using System;
using MessageWire;

namespace ZyanPoC
{
    public class ZyanHost : IDisposable
    {
		public ZyanHost(string baseUrl)
		{
			BaseUrl = baseUrl;
			Host = new Host(BaseUrl);
		}

		public void Dispose()
		{
			Host.Dispose();
		}

		public string BaseUrl { get; }

		public IHost Host { get; }
	}
}
