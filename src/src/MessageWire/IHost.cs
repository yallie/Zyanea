using System;
using System.Collections.Generic;
using System.Text;

namespace MessageWire
{
    public interface IHost : IDisposable
    {
        event EventHandler<MessageEventArgs> MessageReceived;
        event EventHandler<MessageEventFailureArgs> MessageSentFailure;
        event EventHandler<MessageEventArgs> ZkClientSessionEstablishedEvent;

        Guid[] GetCurrentSessionKeys();
        Session[] GetCurrentSessions();
        Session GetSession(Guid key);
        void Send(Guid clientId, List<byte[]> frames);
        void Send(Guid clientId, IEnumerable<byte[]> frames);
        void Send(Guid clientId, byte[] frame);

        void Send(Guid clientId, List<string> frames);
        void Send(Guid clientId, IEnumerable<string> frames);
        void Send(Guid clientId, params string[] frames);
        void Send(Guid clientId, string frame);

        void Send(Guid clientId, List<string> frames, Encoding encoding);
        void Send(Guid clientId, IEnumerable<string> frames, Encoding encoding);
        void Send(Guid clientId, Encoding encoding, params string[] frames);
        void Send(Guid clientId, string frame, Encoding encoding);
    }
}