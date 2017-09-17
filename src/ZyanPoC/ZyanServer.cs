using System;
using System.IO;
using DryIoc;
using DryIoc.MefAttributedModel;
using Hyperion;
using MessageWire;

namespace ZyanPoC
{
	public class ZyanServer : IDisposable
	{
		public ZyanServer(string serverUrl = null)
		{
			if (!string.IsNullOrWhiteSpace(serverUrl))
			{
				Start(serverUrl);
			}
		}

		public void Start(string serverUrl)
		{
			ServerUrl = serverUrl;
			Host = new Host(ServerUrl);
			Host.MessageReceived += HandleReceivedMessage;
		}

		public void Dispose()
		{
			if (IsDisposed)
			{
				return;
			}

			IsDisposed = true;
			Host?.Dispose();
			Container?.Dispose();
		}

		public bool IsDisposed { get; private set; }

		public string ServerUrl { get; private set; }

		public IHost Host { get; private set; }

		public IContainer Container { get; } =
			new Container().WithMef().With(r => r.WithDefaultReuse(Reuse.Transient));

		private Serializer Serializer { get; } =
			new Serializer(new SerializerOptions(preserveObjectReferences: true));

		private void HandleReceivedMessage(object sender, MessageEventArgs e)
		{
			using (var ms = new MemoryStream(e.Message.Frames[0]))
			{
				// deserialize the request message and prepare the reply
				var requestMessage = Serializer.Deserialize<RequestMessage>(ms);
				var replyMessage = new ReplyMessage(requestMessage);

				try
				{
					// invoke the request message and get the result
					var component = Container.Resolve(requestMessage.ComponentType);
					var result = requestMessage.Method.Invoke(component, requestMessage.Arguments);
					replyMessage.Result = result;
				}
				catch (Exception ex)
				{
					// wrap the exception to send back to the client
					replyMessage.Exception = ex;
				}
				finally
				{
					// serialize the reply message
					ms.SetLength(0);
					Serializer.Serialize(replyMessage, ms);
					var serialized = ms.ToArray();

					// send the reply
					Host.Send(e.Message.ClientId, serialized);
				}
			}
		}
	}
}
