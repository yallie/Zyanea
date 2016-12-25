using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessageWire;
using System.Text;
using Xunit;
using System.Threading;

namespace MessageWire.Tests
{
    public class RouterDealerTests
    {
        [Fact]
        public void TestSomething()
        {
            var serverReceived = false;
            var clientReceived = false;
            var connString = "tcp://127.0.0.1:5800";
            using (var server = new Host(connString))
            {
                server.MessageReceived += (s, e) =>
                {
                    var replyData = new List<byte[]>();
                    replyData.Add(Encoding.UTF8.GetBytes("Hello, I'm the server. You sent."));
                    replyData.AddRange(e.Message.Frames);
                    server.Send(e.Message.ClientId, replyData);
                    serverReceived = true;
                    Assert.True(e.Message.Frames.Count == 2, "Server received message did not have 2 frames.");
                    Assert.True(replyData.Count == 3, "Server message did not have 3 frames.");
                };

                using (var client = new Client(connString))
                {
                    client.MessageReceived += (s, e) =>
                    {
                        clientReceived = true;
                        Assert.True(e.Message.Frames.Count == 3, "Received message did not have 3 frames.");
                    };

                    var clientMessageData = new List<byte[]>();
                    clientMessageData.Add(Encoding.UTF8.GetBytes("Hello, I'm the client."));
                    clientMessageData.Add(Encoding.UTF8.GetBytes("This is my second line."));
                    client.Send(clientMessageData);

                    var count = 0;
                    while (count < 20 && (!clientReceived || !serverReceived))
                    {
                        Thread.Sleep(20);
                        count++;
                    }
                    Assert.True(count < 100, "Test took too long.");
                    Assert.True(serverReceived, "Server failed to receive and send.");
                    Assert.True(clientReceived, "Client failed to receive.");
                }
            }
        }
    }
}
