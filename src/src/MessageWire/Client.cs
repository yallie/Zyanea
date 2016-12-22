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
    public class Client : IDisposable
    {
        private readonly string _id;
        private readonly string _key;
        private readonly string _connectionString;

        private readonly string _instanceId;
        private readonly string _clientId;
        private readonly byte[] _clientIdBytes;

        protected bool _isOpen = false;
        private DealerSocket _socket = null;
        private NetMQPoller _poller = null;
        private readonly ConcurrentQueue<NetMQMessage> _sendQueue;
        private readonly ConcurrentQueue<NetMQMessage> _receivedQueue;

        /// <summary>
        /// Client constructor.
        /// </summary>
        /// <param name="id">Client identifier passed to the server in Zero Knowledge authentication.</param>
        /// <param name="key">Secret key used by NOT passed to the server in Zero Knowledge authentication 
        ///                   but used in memory to validate authentication of the server.</param>
        /// <param name="connectionString">Valid NetMQ client socket connection string.</param>
        public Client(string id, string key, string connectionString)
        {
            _id = id ?? Dns.GetHostName();
            _key = key ?? ".";
            _connectionString = connectionString;
            _sendQueue = new ConcurrentQueue<NetMQMessage>();
            _receivedQueue = new ConcurrentQueue<NetMQMessage>();

            _instanceId = Guid.NewGuid().ToString("N");
            _clientId = $"{_id}|{_instanceId}";
            _clientIdBytes = Encoding.UTF8.GetBytes(_clientId);

            _isOpen = true;
            _socket = new DealerSocket(_connectionString);
            _socket.Options.Identity = _clientIdBytes;
            _poller = new NetMQPoller { _socket };
            _socket.ReceiveReady += _socket_ReceiveReady;
            _socket.SendReady += _socket_SendReady;
            _poller.RunAsync();
        }

        public int InBoxCount { get { return _receivedQueue.Count; } }
        public int OutBoxCount { get { return _sendQueue.Count; } }

        public string InstanceId { get { return _instanceId; } }
        public string ClientId { get { return _clientId; } }

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


        public void Send(List<byte[]> frames)
        {
            if (null == frames || frames.Count == 0) throw new ArgumentException("Cannot be null or empty.", nameof(frames));
            var message = new NetMQMessage();
            //this may be automagically added by DealerSocket
            //message.Append(_clientIdBytes);
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
                message = msg.ToMessageWithoutClientFrame(_clientId);
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
                e.Socket.SendMultipartMessage(message);
                _sentEvent?.Invoke(this, new MessageEventArgs { Message = message.ToMessageWithClientFrame() });
            }
        }

        //occurs on the poller thread
        private void _socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMultipartMessage();
            //do something with message - put it on a queue and raise event if subscribed
            //raise event only or add to queue and raise
            if (null != _receivedEvent)
            {
                _receivedEvent.Invoke(this, new MessageEventArgs { Message = message.ToMessageWithoutClientFrame(_clientId) });
            }
            else
            {
                _receivedQueue.Enqueue(message);
                _receivedIntoQueueEvent?.Invoke(this, new EventArgs());
            }
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
