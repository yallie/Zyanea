using System;
using System.Collections.Generic;
using System.Text;
using Castle.DynamicProxy;
using MessageWire;

namespace ZyanPoC
{
	public class ZyanClient : IDisposable
	{
		public ZyanClient(string serverUrl)
		{
			ServerUrl = serverUrl;
			Client = new Client(serverUrl);
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

		private ProxyGenerator ProxyGenerator { get; } = new ProxyGenerator(disableSignedModule: true);

		public IService CreateProxy<IService>()
			where IService: class
		{
			var interceptor = new AsyncInterceptor(this);
			return ProxyGenerator.CreateInterfaceProxyWithTargetInterface<IService>(null, interceptor);
		}

		internal void SendMessage(RequestMessage zyanMessage)
		{
			// TODO: serialize and send the message
		}
	}
}
