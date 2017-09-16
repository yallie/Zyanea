using System;
using System.Collections.Generic;
using System.Text;
using Castle.DynamicProxy;

namespace ZyanPoC
{
	internal class AsyncInterceptor : IAsyncInterceptor
	{
		public AsyncInterceptor(ZyanClient client)
		{
			Client = client;
		}

		public ZyanClient Client { get; }

		public void InterceptSynchronous(IInvocation invocation)
		{
			// TODO: invoke the remote method
			//throw new NotImplementedException();
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
