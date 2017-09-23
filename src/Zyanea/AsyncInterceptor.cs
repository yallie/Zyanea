using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Zyanea
{
	internal class AsyncInterceptor : IAsyncInterceptor
	{
		public AsyncInterceptor(ZyanClient client, Type componentType)
		{
			Client = client;
			ComponentType = componentType;
		}

		public ZyanClient Client { get; }

		public Type ComponentType { get; }

		public void InterceptSynchronous(IInvocation invocation)
		{
			// send the method invocation message
			var msg = new RequestMessage(invocation, ComponentType);
			Client.SendMessage(msg);

			// wait for the reply message synchronously
			invocation.ReturnValue = Client.GetSyncResult(msg.MessageId);
		}

		public void InterceptAsynchronous(IInvocation invocation)
		{
			invocation.ReturnValue = InternalInterceptAsynchronous(invocation);
		}

		private async Task InternalInterceptAsynchronous(IInvocation invocation)
		{
			// send the method invocation message
			var msg = new RequestMessage(invocation, ComponentType);
			Client.SendMessage(msg);

			// wait for the reply message asynchronously
			await Client.GetAsyncResult(msg.MessageId);
		}

		public void InterceptAsynchronous<TResult>(IInvocation invocation)
		{
			invocation.ReturnValue = InternalInterceptAsynchronous<TResult>(invocation);
		}

		private async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
		{
			// send the method invocation message
			var msg = new RequestMessage(invocation, ComponentType);
			Client.SendMessage(msg);

			// wait for the reply message asynchronously
			return await Client.GetAsyncResult<TResult>(msg.MessageId);
		}
	}
}
