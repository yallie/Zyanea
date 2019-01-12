using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Zyanea.Tests.Async;

namespace Zyanea.Tests
{
    public class PerformanceTests : IDisposable
    {
        private const string ServerUrl = "tcp://127.0.0.1:5800";
        private readonly ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public void Dispose()
        {
            Thread.Sleep(100);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(20)]
        [InlineData(50)]
        public async Task AddRateTest(int msgQty)
        {
            using (var server = new ZyanServer(ServerUrl))
            {
                server.Register<ISampleAsyncService, SampleAsyncService>();

                using (var client = new ZyanClient(ServerUrl))
                {
                    var proxy = client.CreateProxy<ISampleAsyncService>();

                    var reply = await proxy.Add(1, 2);

                    var result = new long[3];

                    for (var i = 0; i < 3; i++)
                    {
                        var watch = Stopwatch.StartNew();

                        for (var k = 0; k < msgQty; k++)
                            reply = await proxy.Add(1, 2);

                        watch.Stop();

                        Assert.Equal(3, reply);

                        result[i] = watch.ElapsedMilliseconds;
                    }

                    _output.WriteLine($"{msgQty} messages: {string.Join(" ms, ", result)} ms");
                }
            }
        }
    }
}
