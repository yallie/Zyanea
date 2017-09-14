using System;
using System.Collections.Generic;
using System.Text;
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
	}
}
