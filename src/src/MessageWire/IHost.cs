using System;
using System.Collections.Generic;

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
    }
}