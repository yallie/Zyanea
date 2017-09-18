using System;
using System.Threading.Tasks;
using DryIoc;
using MessageWire;
using Xunit;
using ZyanPoC.Tests.Async;
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

		[Fact]
		public void ZyanClientCanCallVoidMethodSynchronouslyAndItsActuallyExecutedOnServer()
		{
			using (var server = new ZyanServer(ServerUrl))
			{
				server.Register<ISampleSyncService, SampleSyncService>(Reuse.Singleton);
				var sync = server.Resolve<ISampleSyncService>() as SampleSyncService;
				Assert.False(sync.VoidExecuted);

				using (var client = new ZyanClient(ServerUrl))
				{
					var proxy = client.CreateProxy<ISampleSyncService>();

					// Assert.DoesNotThrow
					proxy.Void();
					Assert.True(sync.VoidExecuted);
				}
			}
		}

		[Fact]
		public void ZyanClientCanCallMethodSynchronouslyAndGetTheStringResult()
		{
			using (var server = new ZyanServer(ServerUrl))
			{
				server.Register<ISampleSyncService, SampleSyncService>();

				using (var client = new ZyanClient(ServerUrl))
				{
					var proxy = client.CreateProxy<ISampleSyncService>();

					// Assert.DoesNotThrow
					var result = proxy.GetVersion();
					Assert.Equal(SampleSyncService.Version, result);
				}
			}
		}

		[Fact]
		public void ZyanClientCanCallMethodSynchronouslyAndGetTheDateResult()
		{
			using (var server = new ZyanServer(ServerUrl))
			{
				server.Register<ISampleSyncService, SampleSyncService>();

				using (var client = new ZyanClient(ServerUrl))
				{
					var proxy = client.CreateProxy<ISampleSyncService>();

					// Assert.DoesNotThrow
					var result = proxy.GetDate(2017, 09, 17);
					Assert.Equal(new DateTime(2017, 09, 17), result);
				}
			}
		}

		[Fact]
		public void ZyanClientCanCallMethodSynchronouslyAndCatchTheException()
		{
			using (var server = new ZyanServer(ServerUrl))
			{
				server.Register<ISampleSyncService, SampleSyncService>();

				using (var client = new ZyanClient(ServerUrl))
				{
					var proxy = client.CreateProxy<ISampleSyncService>();

					var ex = Assert.Throws<NotImplementedException>(() =>
					{
						proxy.ThrowException();
					});

					Assert.Equal(nameof(ISampleSyncService), ex.Message);
				}
			}
		}

		[Fact]
		public async Task ZyanClientCanCallAsyncTaskMethod()
		{
			using (var server = new ZyanServer(ServerUrl))
			{
				server.Register<ISampleAsyncService, SampleAsyncService>();

				using (var client = new ZyanClient(ServerUrl))
				{
					var proxy = client.CreateProxy<ISampleAsyncService>();

					// Assert.DoesNotThrow
					await proxy.PerformShortOperation();
				}
			}
		}

		[Fact]
		public async Task ZyanClientCanCallShortAsyncTaskMethodAndItsActuallyExecuted()
		{
			using (var server = new ZyanServer(ServerUrl))
			{
				server.Register<ISampleAsyncService, SampleAsyncService>(Reuse.Singleton);
				var async = server.Resolve<ISampleAsyncService>() as SampleAsyncService;
				Assert.False(async.ShortOperationPerformed);

				using (var client = new ZyanClient(ServerUrl))
				{
					var proxy = client.CreateProxy<ISampleAsyncService>();

					// Assert.DoesNotThrow
					await proxy.PerformShortOperation();
					Assert.True(async.ShortOperationPerformed);
				}
			}
		}

		[Fact]
		public async Task ZyanClientCanCallLongAsyncTaskMethodAndItsActuallyExecuted()
		{
			using (var server = new ZyanServer(ServerUrl))
			{
				server.Register<ISampleAsyncService, SampleAsyncService>(Reuse.Singleton);
				var async = server.Resolve<ISampleAsyncService>() as SampleAsyncService;
				Assert.False(async.LongOperationPerformed);

				using (var client = new ZyanClient(ServerUrl))
				{
					var proxy = client.CreateProxy<ISampleAsyncService>();

					// Assert.DoesNotThrow
					await proxy.PerformLongOperation();
					Assert.True(async.LongOperationPerformed);
				}
			}
		}

		[Fact]
		public async Task ZyanClientCanCallAsyncTaskIntMethodWithParameters()
		{
			using (var server = new ZyanServer(ServerUrl))
			{
				server.Register<ISampleAsyncService, SampleAsyncService>();

				using (var client = new ZyanClient(ServerUrl))
				{
					var proxy = client.CreateProxy<ISampleAsyncService>();

					// Assert.DoesNotThrow
					var result = await proxy.Add(12300, 45);
					Assert.Equal(12345, result);
				}
			}
		}

		[Fact]
		public async Task ZyanClientCanCallAsyncTaskStringMethodWithParameters()
		{
			using (var server = new ZyanServer(ServerUrl))
			{
				server.Register<ISampleAsyncService, SampleAsyncService>();

				using (var client = new ZyanClient(ServerUrl))
				{
					var proxy = client.CreateProxy<ISampleAsyncService>();

					// Assert.DoesNotThrow
					var result = await proxy.Concatenate("Get", "Uninitialized", "Object");
					Assert.Equal("GetUninitializedObject", result);
				}
			}
		}

		[Fact]
		public async Task ZyanClientCanCallAsyncTaskDateTimeMethodWithParameters()
		{
			using (var server = new ZyanServer(ServerUrl))
			{
				server.Register<ISampleAsyncService, SampleAsyncService>();

				using (var client = new ZyanClient(ServerUrl))
				{
					var proxy = client.CreateProxy<ISampleAsyncService>();

					// Assert.DoesNotThrow
					var result = await proxy.Construct(2017, 09, 19);
					Assert.Equal(new DateTime(2017, 09, 19), result);
				}
			}
		}
	}
}
