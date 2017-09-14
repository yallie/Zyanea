using System;
using DryIoc;
using DryIoc.MefAttributedModel;
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

		public IContainer Container { get; } = new Container().WithMef();
	}
}
