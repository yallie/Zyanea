using System;
using MessageWire;
using Xunit;

namespace ZyanPoC.Tests
{
	public class BasicTests : IDisposable
	{
		public BasicTests()
		{
			Wire.Linger = new TimeSpan(0, 0, 0, 1);
		}

		void IDisposable.Dispose()
		{
			Wire.Cleanup();
		}

		const string BaseUrl = "tcp://127.0.0.1:5800";

		[Fact]
		public void BasicHostConnectionTest()
		{
			using (var host = new ZyanHost(BaseUrl))
			{
			}
		}
	}
}
