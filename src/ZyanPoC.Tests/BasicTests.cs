using System;
using MessageWire;
using Xunit;
using ZyanPoC.Tests.Sync;

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

		const string ServerUrl = "tcp://127.0.0.1:5800";

		[Fact]
		public void ZyanServerCanRegisterAndResolveComponents()
		{
			using (var server = new ZyanServer())
			{
				server.Register<ISampleSyncService, SampleSyncService>();
				var component = server.Resolve<ISampleSyncService>();

				Assert.NotNull(component);
				Assert.IsType<SampleSyncService>(component);
			}
		}

		[Fact]
		public void ZyanServerCanBeConnectedTo()
		{
			using (var server = new ZyanServer(ServerUrl))
			using (var client = new ZyanClient(ServerUrl))
			{
				Assert.True(client.IsConnected);
			}
		}

		[Fact]
		public void ZyanClientDoesntImmediatelyConnect()
		{
			// Assert.DoesNotThrow
			using (var client = new ZyanClient(ServerUrl))
			{
			}
		}

		[Fact]
		public void ZyanClientCanCreateProxies()
		{
			// Assert.DoesNotThrow
			using (var client = new ZyanClient(ServerUrl))
			{
				var proxy = client.CreateProxy<ISampleSyncService>();
				Assert.NotNull(proxy);
			}
		}

		[Fact]
		public void ZyanClientCanCallVoidMethodSynchronously()
		{
			using (var server = new ZyanServer(ServerUrl))
			{
				server.Register<ISampleSyncService, SampleSyncService>();

				using (var client = new ZyanClient(ServerUrl))
				{
					var proxy = client.CreateProxy<ISampleSyncService>();

					// Assert.DoesNotThrow
					proxy.Void();
				}
			}
		}
	}
}
