using MessageWire.Logging;
using MessageWire.ZeroKnowledge;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MessageWire
{
    public class Host : IDisposable
    {
        private readonly string _connectionString;
        private readonly ILog _logger;
        private readonly IStats _stats;

        private readonly RouterSocket _routerSocket;
        private readonly NetMQPoller _socketPoller;

        private readonly NetMQPoller _hostPoller;
        private readonly NetMQQueue<NetMQMessage> _sendQueue;
        private readonly NetMQQueue<Message> _receivedQueue;
        private readonly NetMQQueue<MessageFailure> _sendFailureQueue;
        private readonly NetMQTimer _sessionCleanupTimer = null;

        private readonly IZkRepository _authRepository;
        private readonly Dictionary<Guid, ZkProtocolHostSession> _sessions;
        private readonly int _sessionTimeoutMins;

        /// <summary>
        /// Host constructor. Supply an IZkRepository to enable Zero Knowledge authentication and encryption.
        /// </summary>
        /// <param name="connectionString">Valid NetMQ server socket connection string.</param>
        /// <param name="authRepository">External authentication repository. Null creates host with no encryption.</param>
        /// <param name="logger">ILogger implementation for logging operations. Null is replaced with NullLogger.</param>
        /// <param name="stats">IStats implementation for logging perf metrics. Null is replaced with NullStats.</param>
        /// <param name="sessionTimeoutMins">Session timout check interval. If no heartbeats or messages 
        /// received on a given session in this period of time, the session will be removed from memory 
        /// and futher attempts from the client will fail. Default is 20 minutes. Min is 1 and Max is 3600.</param>
        public Host(string connectionString, IZkRepository authRepository = null, 
            ILog logger = null, IStats stats = null, int sessionTimeoutMins = 20)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException("Connection string cannot be null.", nameof(connectionString));
            _connectionString = connectionString;
            _logger = logger ?? new NullLogger();
            _stats = stats ?? new NullStats();

            //enforce min and max session check
            if (sessionTimeoutMins < 1) sessionTimeoutMins = 1;
            else if (sessionTimeoutMins > 3600) sessionTimeoutMins = 3600;
            _sessionTimeoutMins = sessionTimeoutMins;

            _authRepository = authRepository;
            _sessions = new Dictionary<Guid, ZkProtocolHostSession>();
            _sendQueue = new NetMQQueue<NetMQMessage>();

            _routerSocket = new RouterSocket(_connectionString);
            _routerSocket.Options.RouterMandatory = true;
            _sendQueue.ReceiveReady += SendQueue_ReceiveReady;
            _routerSocket.ReceiveReady += RouterSocket_ReceiveReady;
            _socketPoller = new NetMQPoller { _routerSocket, _sendQueue };
            _socketPoller.RunAsync();

            _sendFailureQueue = new NetMQQueue<MessageFailure>();
            _receivedQueue = new NetMQQueue<Message>();
            _sendFailureQueue.ReceiveReady += SendFailureQueue_ReceiveReady;
            _receivedQueue.ReceiveReady += ReceivedQueue_ReceiveReady;

            if (null == _authRepository)
            {
                //no session cleanup required if not secured
                _hostPoller = new NetMQPoller { _receivedQueue, _sendFailureQueue };
            }
            else
            {
                _sessionCleanupTimer = new NetMQTimer(new TimeSpan(0, _sessionTimeoutMins, 0));
                _sessionCleanupTimer.Elapsed += SessionCleanupTimer_Elapsed;
                _hostPoller = new NetMQPoller { _receivedQueue, _sendFailureQueue, _sessionCleanupTimer };
                _sessionCleanupTimer.Enable = true;
            }
            _hostPoller.RunAsync();
        }

        private EventHandler<MessageEventFailureArgs> _sentFailureEvent;
        private EventHandler<MessageEventArgs> _receivedEvent;
        private EventHandler<MessageEventArgs> _sentEvent;
        private EventHandler<MessageEventArgs> _zkClientSessionEstablishedEvent;

        /// <summary>
        /// This event occurs when a message has been received. 
        /// </summary>
        /// <remarks>This handler will run on a different thread than the socket poller and
        /// blocking on this thread will not block sending and receiving.</remarks>
        public event EventHandler<MessageEventArgs> MessageReceived {
            add {
                _receivedEvent += value;
            }
            remove {
                _receivedEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when a message failed to send because the client is no longer connected.
        /// </summary>
        /// <remarks>This handler will run on a different thread than the socket poller and
        /// blocking on this thread will not block sending and receiving.</remarks>
        public event EventHandler<MessageEventFailureArgs> MessageSentFailure {
            add {
                _sentFailureEvent += value;
            }
            remove {
                _sentFailureEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when a new client session has been established over Zero Knowledge protocol.
        /// The Message in the event contains no frames. It is only to signal a new encrypted ClientId.
        /// </summary>
        /// <remarks>This handler will run on a different thread than the socket poller and
        /// blocking on this thread will not block sending and receiving.</remarks>
        public event EventHandler<MessageEventArgs> ZkClientSessionEstablishedEvent {
            add {
                _zkClientSessionEstablishedEvent += value;
            }
            remove {
                _zkClientSessionEstablishedEvent -= value;
            }
        }

        public void Send(Guid clientId, List<byte[]> frames)
        {
            if (_disposed) throw new ObjectDisposedException("Client", "Cannot send on disposed client.");
            if (null == frames || frames.Count == 0)
            {
                throw new ArgumentException("Cannot be null or empty.", nameof(frames));
            }
            var message = new NetMQMessage();
            message.Append(clientId.ToByteArray());
            message.AppendEmptyFrame();
            if (null != _authRepository)
            {
                var session = _sessions.ContainsKey(clientId) ? _sessions[clientId] : null;
                if (null != session && null != session.Crypto)
                {
                    foreach (var frame in frames) message.Append(session.Crypto.Encrypt(frame));
                }
                else
                {
                    throw new OperationCanceledException("Encrypted session not established.");
                }
            }
            else
            {
                foreach (var frame in frames) message.Append(frame);
            }
            _sendQueue.Enqueue(message); //send by message to socket poller
        }

        //occurs on socket polling thread to assure sending and receiving on same thread
        private void SendQueue_ReceiveReady(object sender, NetMQQueueEventArgs<NetMQMessage> e)
        {
            NetMQMessage message;
            if (e.Queue.TryDequeue(out message, new TimeSpan(1000)))
            {
                try
                {
                    _routerSocket.SendMultipartMessage(message);
                }
                catch (Exception ex) //clientId not found or other error, raise event
                {
                    var unreachable = ex as HostUnreachableException;
                    //send by message to host poller
                    _sendFailureQueue.Enqueue(new MessageFailure
                    {
                        Message = message.ToMessageWithClientFrame(),
                        ErrorCode = null != unreachable ? unreachable.ErrorCode.ToString() : "Unknown",
                        ErrorMessage = ex.Message
                    });
                }
            }
        }

        //occurs on socket polling thread to assure sending and receiving on same thread
        private void RouterSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            if (null == msg || msg.FrameCount < 2) return; //ignore this message - nothing in it
            var message = msg.ToMessageWithClientFrame();
            _receivedQueue.Enqueue(message); //sends by message to host poller
        }

        //occurs on host polling thread to allow sending and receiving on a different thread
        private void SendFailureQueue_ReceiveReady(object sender, NetMQQueueEventArgs<MessageFailure> e)
        {
            MessageFailure mf;
            if (e.Queue.TryDequeue(out mf, new TimeSpan(1000)))
            {
                _sentFailureEvent?.Invoke(this, new MessageEventFailureArgs
                {
                    Failure = mf
                });
            }
        }

        //occurs on host polling thread to allow sending and receiving on a different thread
        private void ReceivedQueue_ReceiveReady(object sender, NetMQQueueEventArgs<Message> e)
        {
            Message message;
            if (e.Queue.TryDequeue(out message, new TimeSpan(1000)))
            {
                if (message.Frames[0].IsEqualTo(ZkMessageHeader.HeartBeat))
                {
                    ProcessHeartBeat(message);
                }
                else if (null == _authRepository)
                {
                    ProcessRegularMessage(message);
                }
                else
                {
                    ProcessProtectedMessage(message);
                }
            }
        }

        private void ProcessHeartBeat(Message message)
        {
            var session = _sessions.ContainsKey(message.ClientId) ? _sessions[message.ClientId] : null;
            if (null != session)
            {
                session.RecordHeartBeat();
                var heartBeatResponse = new NetMQMessage();
                heartBeatResponse.Append(message.ClientId.ToByteArray());
                heartBeatResponse.AppendEmptyFrame();
                heartBeatResponse.Append(ZkMessageHeader.HeartBeat);
                _sendQueue.Enqueue(heartBeatResponse);
            }
        }

        private void ProcessRegularMessage(Message message)
        {
            _receivedEvent?.Invoke(this, new MessageEventArgs
            {
                Message = message
            });
        }

        private void ProcessProtectedMessage(Message message)
        {
            var session = _sessions.ContainsKey(message.ClientId) ? _sessions[message.ClientId] : null;
            if (IsHandshakeRequest(message.Frames))
            {
                if (null == session)
                {
                    session = new ZkProtocolHostSession(_authRepository, message.ClientId);
                    _sessions.Add(message.ClientId, session);
                }
                var responseFrames = session.ProcessProtocolRequest(message.Frames);
                var msg = new NetMQMessage();
                msg.Append(message.ClientId.ToByteArray());
                msg.AppendEmptyFrame();
                foreach (var frame in responseFrames) msg.Append(frame);
                _sendQueue.Enqueue(msg); //send by message to socket poller

                //if second reply and success, raise event, new client session?
                if (responseFrames[0].IsEqualTo(ZkMessageHeader.ProofResponseSuccess))
                {
                    _zkClientSessionEstablishedEvent?.Invoke(this, new MessageEventArgs
                    {
                        Message = new Message
                        {
                            ClientId = message.ClientId
                        }
                    });
                }
            }
            else
            {
                if (null != session && null != session.Crypto)
                {
                    for (int i = 0; i < message.Frames.Count; i++)
                    {
                        message.Frames[i] = session.Crypto.Decrypt(message.Frames[i]);
                    }
                }
                _receivedEvent?.Invoke(this, new MessageEventArgs
                {
                    Message = message
                });
            }
        }

        private void SessionCleanupTimer_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            //remove timed out sessions
            var now = DateTime.UtcNow;
            var timedOutKeys = (from n in _sessions
                            where (now - n.Value.LastTouched).TotalMinutes > _sessionTimeoutMins
                            select n.Key).ToArray();
            foreach (var key in timedOutKeys)
            {
                _sessions.Remove(key);
            }
        }

        private bool IsHandshakeRequest(List<byte[]> frames)
        {
            return (null != frames
                && (frames.Count == 2 || frames.Count == 4)
                && frames[0].Length == 4
                && frames[0][0] == ZkMessageHeader.SOH
                && frames[0][1] == ZkMessageHeader.ENQ
                && ((frames[0][2] == ZkMessageHeader.CM0 && frames.Count == 2)
                    || (frames[0][2] == ZkMessageHeader.CM1 && frames.Count == 4)
                    || (frames[0][2] == ZkMessageHeader.CM2 && frames.Count == 2))
                && frames[0][3] == ZkMessageHeader.BEL);
        }


        #region IDisposable Members

        private bool _disposed = false;

        public void Dispose()
        {
            //MS recommended dispose pattern - prevents GC from disposing again
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true; //prevent second call to Dispose
                if (disposing)
                {
                    if (null != _sessionCleanupTimer) _sessionCleanupTimer.Enable = false;
                    if (null != _socketPoller) _socketPoller.Dispose();
                    if (null != _sendQueue) _sendQueue.Dispose();
                    if (null != _routerSocket) _routerSocket.Dispose();

                    if (null != _hostPoller) _hostPoller.Dispose();
                    if (null != _receivedQueue) _receivedQueue.Dispose();
                    if (null != _sendFailureQueue) _sendFailureQueue.Dispose();
                }
            }
        }

        #endregion
    }
}
