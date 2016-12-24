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

        private readonly Guid _clientId;
        private readonly byte[] _clientIdBytes;

        private DealerSocket _dealerSocket = null;
        private NetMQPoller _socketPoller = null;
        private NetMQPoller _clientPoller = null;
        private readonly NetMQQueue<List<byte[]>> _sendQueue;
        private readonly NetMQQueue<List<byte[]>> _receiveQueue;

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
        }

        private void _receivedQueue_ReceiveReady(object sender, NetMQQueueEventArgs<List<byte[]>> e)
        {
            List<byte[]> frames;
            if (e.Queue.TryDequeue(out frames, new TimeSpan(1000)))
            {
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

        public Guid ClientId { get { return _clientId; } }

        private EventHandler<MessageEventArgs> _receivedEvent;

        /// <summary>
        /// This event occurs when a message has been received. 
        /// </summary>
        /// <remarks>This handler is thread safe occuring on a thread other 
        /// than the thread sendign and receiving messages over the wire.</remarks>
        public event EventHandler<MessageEventArgs> MessageReceived {
            add {
                _receivedEvent += value;
            }
            remove {
                _receivedEvent -= value;
            }
        }

        public void Send(List<byte[]> frames)
        {
            if (_disposed) throw new ObjectDisposedException("Client", "Cannot send on disposed client.");
            if (null == frames || frames.Count == 0)
            {
                throw new ArgumentException("Cannot be null or empty.", nameof(frames));
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
                foreach (var frame in frames)
                {
                    message.Append(frame);
                }
                _dealerSocket.SendMultipartMessage(message);
            }
        }

        //Executes on same poller thread as dealer socket, so we enqueue to the received queue
        //and raise the event on the poller thread for received queue
        private void _socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            var message = msg.ToMessageWithoutClientFrame(_clientId);
            _receiveQueue.Enqueue(message.Frames);
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
