using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
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

		private async void HandleReceivedMessage(object sender, MessageEventArgs e)
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

					// handle task results
					var task = result as Task;
					if (task != null)
					{
						await task;

						// handle Task<TResult>
						var taskType = task.GetType().GetTypeInfo();
						if (taskType.IsGenericType)
						{
							// TODO: cache resultProperty and convert it to a delegate
							var resultProperty = taskType.GetProperty(nameof(Task<bool>.Result));
							replyMessage.Result = resultProperty.GetValue(task);
						}
						else
						{
							replyMessage.Result = null;
						}
					}
					else
					{
						replyMessage.Result = result;
					}
				}
				catch (Exception ex)
				{
					// skip the useless TargetInvocationException
					// also, it's not supported by the serializer
					if (ex is TargetInvocationException)
					{
						ex = ex.InnerException;
					}

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
