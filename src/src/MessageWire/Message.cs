using NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageWire
{
    public class Message
    {
        public string ClientId { get; set; }
        public List<byte[]> Frames { get; set; }
    }

    internal static class MessageExtensions
    {
        public static Message ToMessageWithoutClientFrame(this NetMQMessage msg, string clientId)
        {
            if (msg == null || msg.FrameCount == 0) return null;
            List<byte[]> frames = new List<byte[]>();
            if (msg.FrameCount > 0)
            {
                frames = (from n in msg where !n.IsEmpty select n.Buffer).ToList();
            }
            return new Message
            {
                ClientId = clientId,
                Frames = frames
            };
        }

        public static Message ToMessageWithClientFrame(this NetMQMessage msg)
        {
            if (msg == null || msg.FrameCount == 0) return null;
            var clientId = msg[0].ConvertToString(Encoding.UTF8);
            List<byte[]> frames = new List<byte[]>();
            if (msg.FrameCount > 1)
            {
                frames = (from n in msg where !n.IsEmpty select n.Buffer).Skip(1).ToList();
            }
            return new Message
            {
                ClientId = clientId,
                Frames = frames
            };
        }

        public static NetMQMessage ToNetMQMessage(this Message msg)
        {
            var message = new NetMQMessage();
            message.Append(Encoding.UTF8.GetBytes(msg.ClientId));
            message.AppendEmptyFrame();
            if (null != msg.Frames)
            {
                foreach (var frame in msg.Frames)
                {
                    message.Append(frame);
                }
            }
            else
            {
                message.AppendEmptyFrame();
            }
            return message;
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public Message Message { get; set; }
    }

    public class MessageEventFailureArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public Message Message { get; set; }
    }

}
