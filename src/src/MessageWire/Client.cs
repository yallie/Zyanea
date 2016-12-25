using MessageWire.ZeroKnowledge;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessageWire
{
    public class Client : IDisposable
    {
        private readonly string _id;
        private readonly string _key;
        private readonly string _connectionString;

        private readonly Guid _clientId;
        private readonly byte[] _clientIdBytes;

        private DealerSocket _dealerSocket = null;
        private NetMQPoller _socketPoller = null;
        private NetMQPoller _clientPoller = null;
        private readonly NetMQQueue<List<byte[]>> _sendQueue;
        private readonly NetMQQueue<List<byte[]>> _receiveQueue;

        private ZkProtocolClientSession _session = null;
        private bool _throwOnSend = false;

        /// <summary>
        /// Client constructor.
        /// </summary>
        /// <param name="connectionString">Valid NetMQ client socket connection string.</param>
        /// <param name="id">Client identifier passed to the server in Zero Knowledge authentication. 
        ///                  Null for unencrypted hosts.</param>
        /// <param name="key">Secret key used by NOT passed to the server in Zero Knowledge authentication 
        ///                   but used in memory to validate authentication of the server. Null for 
        ///                   unencrypted hosts</param>
        public Client(string connectionString, string id = null, string key = null)
        {
            _id = id;
            _key = key;
            _connectionString = connectionString;

            _clientId = Guid.NewGuid();
            _clientIdBytes = _clientId.ToByteArray();

            _sendQueue = new NetMQQueue<List<byte[]>>();
            _dealerSocket = new DealerSocket(_connectionString);
            _dealerSocket.Options.Identity = _clientIdBytes;
            _sendQueue.ReceiveReady += _sendQueue_ReceiveReady;
            _dealerSocket.ReceiveReady += _socket_ReceiveReady;
            _socketPoller = new NetMQPoller { _dealerSocket, _sendQueue };
            _socketPoller.RunAsync();

            _receiveQueue = new NetMQQueue<List<byte[]>>();
            _receiveQueue.ReceiveReady += _receivedQueue_ReceiveReady;
            _clientPoller = new NetMQPoller { _receiveQueue };
            _clientPoller.RunAsync();

            if (null != _id && null != _key)
            {
                _throwOnSend = true;
            }
        }

        private ManualResetEvent _securedSignal = null;

        /// <summary>
        /// Executes the Zero Knowledge protocol and blocks until it is complete of has failed.
        /// Allows client to be hooked up to protocol events.
        /// </summary>
        /// <param name="blockUntilComplete">If true (default) method blocks until protocol is established.</param>
        /// <param name="timeoutMs">If blocking, will block for timeoutMs.</param>
        /// <returns>Returns true if connection has been secured. False if non-blocking or if protocol fails.</returns>
        public bool SecureConnection(bool blockUntilComplete = true, int timeoutMs = 500)
        {
            if (null == _id || null == _key) return false;
            if (null != _session && null != _session.Crypto) return true; //in case it's called twice

            _securedSignal = new ManualResetEvent(false);
            _session = new ZkProtocolClientSession(_id, _key);
            _sendQueue.Enqueue(_session.CreateHandshakeMessage1(_id));

            if (blockUntilComplete)
            {
                if (_securedSignal.WaitOne(timeoutMs))
                {
                    if (!_throwOnSend) return true; //success
                }
            }
            return false;
        }

        public Guid ClientId { get { return _clientId; } }

        private EventHandler<MessageEventArgs> _receivedEvent;
        private EventHandler<EventArgs> _ecryptionProtocolEstablishedEvent;
        private EventHandler<EventArgs> _ecryptionProtocolFailedEvent;

        /// <summary>
        /// This event occurs when a message has been received. 
        /// </summary>
        /// <remarks>This handler is thread safe occuring on a thread other 
        /// than the thread sending and receiving messages over the wire.</remarks>
        public event EventHandler<MessageEventArgs> MessageReceived {
            add {
                _receivedEvent += value;
            }
            remove {
                _receivedEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when the client has established a secure connection and
        /// messages may be sent without throwing an operation cancelled exception. 
        /// </summary>
        /// <remarks>This handler is thread safe occuring on a thread other 
        /// than the thread sending and receiving messages over the wire.</remarks>
        public event EventHandler<EventArgs> EcryptionProtocolEstablished {
            add {
                _ecryptionProtocolEstablishedEvent += value;
            }
            remove {
                _ecryptionProtocolEstablishedEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when the client failes to establish a secure connection and
        /// messages may be sent without throwing an operation cancelled exception. 
        /// </summary>
        /// <remarks>This handler is thread safe occuring on a thread other 
        /// than the thread sending and receiving messages over the wire.</remarks>
        public event EventHandler<EventArgs> EcryptionProtocolFailed {
            add {
                _ecryptionProtocolFailedEvent += value;
            }
            remove {
                _ecryptionProtocolFailedEvent -= value;
            }
        }

        public bool CanSend { get { return !_throwOnSend; } }

        public void Send(List<byte[]> frames)
        {
            if (_disposed) throw new ObjectDisposedException("Client", "Cannot send on disposed client.");
            if (null == frames || frames.Count == 0)
            {
                throw new ArgumentException("Cannot be null or empty.", nameof(frames));
            }
            if (_throwOnSend)
            {
                throw new OperationCanceledException("Encryption protocol not established.");
            }
            _sendQueue.Enqueue(frames);
        }

        //Executes on same poller thread as dealer socket, so we can send directly
        private void _sendQueue_ReceiveReady(object sender, NetMQQueueEventArgs<List<byte[]>> e)
        {
            var message = new NetMQMessage();
            message.AppendEmptyFrame();
            List<byte[]> frames;
            if (e.Queue.TryDequeue(out frames, new TimeSpan(1000)))
            {
                if (null != _session && null != _session.Crypto)
                {
                    //encrypt message frames
                    for (int i = 0; i < frames.Count; i++)
                    {
                        frames[i] = _session.Crypto.Encrypt(frames[i]);
                    }
                }

                foreach (var frame in frames)
                {
                    message.Append(frame);
                }
                _dealerSocket.SendMultipartMessage(message);
            }
        }

        //Executes on same poller thread as dealer socket, so we enqueue to the received queue
        //and raise the event on the client poller thread for received queue
        private void _socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            var message = msg.ToMessageWithoutClientFrame(_clientId);
            _receiveQueue.Enqueue(message.Frames);
        }

        //Executes on client poller thread to avoid tying up the dealer socket poller thread
        private void _receivedQueue_ReceiveReady(object sender, NetMQQueueEventArgs<List<byte[]>> e)
        {
            List<byte[]> frames;
            if (e.Queue.TryDequeue(out frames, new TimeSpan(1000)))
            {
                //check for ZK protocol
                if (null != _session)
                {
                    if (null == _session.Crypto)
                    {
                        if (IsHandshakeReply(frames))
                        {
                            if (frames[0][2] == ZkMessageHeader.SM1)
                            {
                                //send proof
                                var frames2 = _session.CreateHandshakeMessage2(frames);
                                if (null != frames2)
                                {
                                    _sendQueue.Enqueue(frames2);
                                }
                                else
                                {
                                    _throwOnSend = true;
                                }
                            }
                            else if (frames[0][2] == ZkMessageHeader.SM2)
                            {
                                //complete proof
                                if (_session.ProcessHandshakeReply2(frames))
                                {
                                    _throwOnSend = false;
                                    if (null != _securedSignal) _securedSignal.Set(); //signal if waiting
                                    _ecryptionProtocolEstablishedEvent?.Invoke(this, new EventArgs());
                                }
                                else
                                {
                                    _throwOnSend = true;
                                    _ecryptionProtocolFailedEvent?.Invoke(this, new EventArgs());
                                }
                            }
                            else
                            {
                                _throwOnSend = true;
                                _ecryptionProtocolFailedEvent?.Invoke(this, new EventArgs());
                            }
                        }
                    }
                    else
                    {
                        //decrypt message frames
                        for (int i = 0; i < frames.Count; i++)
                        {
                            frames[i] = _session.Crypto.Decrypt(frames[i]);
                        }
                    }
                }

                _receivedEvent?.Invoke(this, new MessageEventArgs
                {
                    Message = new Message
                    {
                        ClientId = _clientId,
                        Frames = frames
                    }
                });
            }
        }

        private bool IsHandshakeReply(List<byte[]> frames)
        {
            return (null != frames
                && frames.Count == 3
                && frames[0].Length == 4
                && frames[0][0] == ZkMessageHeader.SOH
                && frames[0][1] == ZkMessageHeader.ACK
                && (frames[0][2] == ZkMessageHeader.FF0
                   || frames[0][2] == ZkMessageHeader.SM1
                   || frames[0][2] == ZkMessageHeader.SF1
                   || frames[0][2] == ZkMessageHeader.SM2
                   || frames[0][2] == ZkMessageHeader.SF2)
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
                    if (null != _socketPoller) _socketPoller.Dispose();
                    if (null != _sendQueue) _sendQueue.Dispose();
                    if (null != _dealerSocket) _dealerSocket.Dispose();

                    if (null != _clientPoller) _clientPoller.Dispose();
                    if (null != _receiveQueue) _receiveQueue.Dispose();
                }
            }
        }

        #endregion
    }
}
