using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace ZyanPoC
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
			// TODO: handle optional call interception
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
			// TODO: handle optional call interception
			// send the method invocation message
			var msg = new RequestMessage(invocation, ComponentType);
			Client.SendMessage(msg);

			// wait for the reply message asynchronously
			await Client.GetAsyncTask(msg.MessageId);
		}

		public void InterceptAsynchronous<TResult>(IInvocation invocation)
		{
			throw new NotImplementedException();
		}
	}
}
