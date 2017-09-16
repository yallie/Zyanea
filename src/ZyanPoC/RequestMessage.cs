using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Castle.DynamicProxy;

namespace ZyanPoC
{
	internal class RequestMessage
	{
		public RequestMessage(IInvocation invocation)
		{
			MessageId = Guid.NewGuid();
			ComponentType = invocation.TargetType;
			Method = invocation.Method;
			GenericArguments = invocation.GenericArguments;
			Arguments = invocation.Arguments;
		}

		public Guid MessageId { get; }

		public Type ComponentType { get; }

		public MethodInfo Method { get; }

		public Type[] GenericArguments { get; }

		public object[] Arguments { get; }
	}
}
