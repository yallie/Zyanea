using System;
using System.Collections.Generic;
using System.Text;
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

			// wait for the reply message
			invocation.ReturnValue = Client.GetResult(msg.MessageId);
		}

		public void InterceptAsynchronous(IInvocation invocation)
		{
			throw new NotImplementedException();
		}

		public void InterceptAsynchronous<TResult>(IInvocation invocation)
		{
			throw new NotImplementedException();
		}
	}
}
