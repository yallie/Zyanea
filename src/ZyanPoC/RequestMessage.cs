using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Castle.DynamicProxy;

namespace ZyanPoC
{
	public class RequestMessage
	{
		public RequestMessage(IInvocation invocation, Type componentType)
		{
			MessageId = Guid.NewGuid();
			ComponentType = componentType;
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
