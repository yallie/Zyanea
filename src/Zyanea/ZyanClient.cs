using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Hyperion;
using MessageWire;

namespace Zyanea
{
	public class ZyanClient : IDisposable
	{
		public ZyanClient(string serverUrl)
		{
			ServerUrl = serverUrl;
			Client = new Client(serverUrl);
			Client.MessageReceived += HandleReceivedMessage;
		}

		public void Dispose()
		{
			if (IsDisposed)
			{
				return;
			}

			IsDisposed = true;
			Client?.Dispose();
		}

		public bool IsDisposed { get; private set; }

		public string ServerUrl { get; }

		public IClient Client { get; }

		public bool IsConnected => Client.IsHostAlive;

		private ProxyGenerator ProxyGenerator { get; } =
			new ProxyGenerator(disableSignedModule: true);

		private Serializer Serializer { get; } =
			new Serializer(new SerializerOptions(preserveObjectReferences: true));

		public IService CreateProxy<IService>()
			where IService: class
		{
			var interceptor = new AsyncInterceptor(this, typeof(IService));
			return ProxyGenerator.CreateInterfaceProxyWithTargetInterface<IService>(null, interceptor);
		}

		private ConcurrentDictionary<Guid, TaskCompletionSource<object>> PendingMessages { get; } =
			new ConcurrentDictionary<Guid, TaskCompletionSource<object>>();

		internal void SendMessage(RequestMessage zyanMessage)
		{
			// serialize and send the message to the remote host
			using (var ms = new MemoryStream())
			{
				PendingMessages[zyanMessage.MessageId] = new TaskCompletionSource<object>();
				Serializer.Serialize(zyanMessage, ms);
				var serialized = ms.ToArray();
				Client.Send(serialized);
			}
		}

		private void HandleReceivedMessage(object sender, MessageEventArgs e)
		{
			// deserialize the reply message
			using (var ms = new MemoryStream(e.Message.Frames[0]))
			{
				var replyMessage = Serializer.Deserialize<ReplyMessage>(ms);
				var tcs = PendingMessages[replyMessage.RequestMessageId];

				// signal the remote exception
				if (replyMessage.Exception != null)
				{
					tcs.SetException(replyMessage.Exception);
					return;
				}

				// signal the result
				tcs.SetResult(replyMessage.Result);
			}
		}

		private Task<object> GetResultTask(Guid messageId)
		{
			if (PendingMessages.TryGetValue(messageId, out var tcs))
			{
				return tcs.Task;
			}

			throw new InvalidOperationException($"Message {messageId} already handled");
		}

		internal object GetSyncResult(Guid messageId)
		{
			// task.Result wraps the exception in AggregateException
			// task.GetAwaiter().GetResult() does not
			return GetResultTask(messageId).GetAwaiter().GetResult();
		}

		internal Task GetAsyncResult(Guid messageId)
		{
			return GetResultTask(messageId);
		}

		internal async Task<TResult> GetAsyncResult<TResult>(Guid messageId)
		{
			var result = await GetResultTask(messageId);
			return (TResult)result;
		}
	}
}
