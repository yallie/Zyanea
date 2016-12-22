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
        protected bool _isOpen = false;
        private readonly string _connectionString;
        private RouterSocket _socket = null;
        private NetMQPoller _poller = null;
        private readonly ConcurrentQueue<NetMQMessage> _sendQueue;
        private readonly ConcurrentQueue<NetMQMessage> _receivedQueue;

        /// <summary>
        /// Host constructor.
        /// </summary>
        /// <param name="connectionString">Valid NetMQ server socket connection string.</param>
        public Host(string connectionString)
        {
            _connectionString = connectionString;
            _sendQueue = new ConcurrentQueue<NetMQMessage>();
            _receivedQueue = new ConcurrentQueue<NetMQMessage>();
            _isOpen = true;
            _socket = new RouterSocket(_connectionString);
            _socket.Options.RouterMandatory = true;
            _poller = new NetMQPoller { _socket };
            _socket.ReceiveReady += _socket_ReceiveReady;
            _socket.SendReady += _socket_SendReady;
            _poller.RunAsync();
        }

        public int InBoxCount { get { return _receivedQueue.Count; } }
        public int OutBoxCount { get { return _sendQueue.Count; } }

        private EventHandler<MessageEventFailureArgs> _sentFailureEvent;
        private EventHandler<MessageEventArgs> _receivedEvent;
        private EventHandler<MessageEventArgs> _sentEvent;
        private EventHandler<EventArgs> _receivedIntoQueueEvent;

        /// <summary>
        /// This event occurs when a message has been received into the received queue.
        /// </summary>
        /// <remarks>To get the event, call the TryReceive method. This handler will run on the same thread as 
        /// the thread handling the IO. Care should be taken to make processing very quick or offload the work 
        /// onto another thread or task.</remarks>
        public event EventHandler<EventArgs> MessageReceivedIntoQueue {
            add {
                _receivedIntoQueueEvent += value;
            }
            remove {
                _receivedIntoQueueEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when a message has been received. 
        /// </summary>
        /// <remarks>This handler will run on the same thread as the thread handling the IO. Care should be taken to make 
        /// processing very quick or offload the work onto another thread or task. Any subscription to this event will prevent incoming 
        /// messages from being enqued and the TryReceive method will always return false and output a null Message. 
        /// Rather the message will be included in the event arguments and must be handled there. Once the event handler
        /// returns, the message will no longer be accessible.</remarks>
        public event EventHandler<MessageEventArgs> MessageReceived {
            add {
                _receivedEvent += value;
            }
            remove {
                _receivedEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when a message has been sent.
        /// </summary>
        /// <remarks>This handler will run on the same thread as the thread handling the IO. Care should be taken to make 
        /// processing very quick or offload the work onto another thread or task.</remarks>
        public event EventHandler<MessageEventArgs> MessageSent {
            add {
                _sentEvent += value;
            }
            remove {
                _sentEvent -= value;
            }
        }

        /// <summary>
        /// This event occurs when a message failed to send because the client is no longer connected.
        /// </summary>
        /// <remarks>This handler will run on the same thread as the thread handling the IO. Care should be taken to make 
        /// processing very quick or offload the work onto another thread or task.</remarks>
        public event EventHandler<MessageEventFailureArgs> MessageSentFailure {
            add {
                _sentFailureEvent += value;
            }
            remove {
                _sentFailureEvent -= value;
            }
        }

        public void Send(string clientId, List<byte[]> frames)
        {
            if (null == clientId) throw new ArgumentNullException(nameof(clientId), "Cannot be null.");
            if (null == frames || frames.Count == 0) throw new ArgumentException("Cannot be null or empty.", nameof(frames));
            var clientIdBytes = Encoding.UTF8.GetBytes(clientId);
            var message = new NetMQMessage();
            message.Append(clientIdBytes);
            message.AppendEmptyFrame();
            if (null != frames && frames.Count > 0)
            {
                foreach (var frame in frames) message.Append(frame);
            }
            else
            {
                message.AppendEmptyFrame();
            }
            _sendQueue.Enqueue(message);
        }

        public bool TryReceive(out Message message)
        {
            NetMQMessage msg;
            if (_receivedQueue.TryDequeue(out msg))
            {
                message = msg.ToMessageWithClientFrame();
                return true;
            }
            message = null;
            return false;
        }

        //occurs on the poller thread
        private void _socket_SendReady(object sender, NetMQSocketEventArgs e)
        {
            NetMQMessage message;
            if (_sendQueue.TryDequeue(out message))
            {
                try
                {
                    e.Socket.SendMultipartMessage(message);
                }
                catch (HostUnreachableException ex) //clientId not found or other error, raise event
                {
                    _sentFailureEvent?.Invoke(this, new MessageEventFailureArgs
                    {
                        Message = message.ToMessageWithClientFrame(),
                        ErrorCode = ex.ErrorCode.ToString(),
                        ErrorMessage = ex.Message
                    });
                }
                _sentEvent?.Invoke(this, new MessageEventArgs { Message = message.ToMessageWithClientFrame() });
            }
        }

        //occurs on the poller thread
        private void _socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMultipartMessage();
            //do something with message - put it on a queue and raise event if subscribed
            _receivedQueue.Enqueue(message);
            //raise event only or add to queue and raise
            if (null != _receivedEvent)
            {
                _receivedEvent.Invoke(this, new MessageEventArgs { Message = message.ToMessageWithClientFrame() });
            }
            else
            {
                _receivedQueue.Enqueue(message);
                _receivedIntoQueueEvent?.Invoke(this, new EventArgs());
            }

            //NetworkOrderBitsConverter.GetBytes()
            //NetworkOrderBitsConverter.ToInt16(buf)
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
                    if (null != _poller) _poller.Dispose();
                    if (null == _socket) _socket.Dispose();
                    _isOpen = false;
                }
            }
        }

        #endregion
    }
}
