using System;
using System.Collections.Generic;

namespace MessageWire
{
	public interface IClient : IDisposable
	{
		bool CanSend { get; }
		Guid ClientId { get; }
		int HeartBeatsReceivedCount { get; }
		int HeartBeatsSentCount { get; }
		bool IsHostAlive { get; }
		DateTime? LastHeartBeatReceivedFromHost { get; }

		event EventHandler<EventArgs> EcryptionProtocolEstablished;
		event EventHandler<ProtocolFailureEventArgs> EcryptionProtocolFailed;
		event EventHandler<MessageEventArgs> InvalidMessageReceived;
		event EventHandler<MessageEventArgs> MessageReceived;

		bool SecureConnection(bool blockUntilComplete = true, int timeoutMs = 500);
		void Send(List<byte[]> frames);
	}
}