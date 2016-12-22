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
            var serverSent = false;
            var clientSent = false;
            var clientReceived = false;
            var connString = "tcp://127.0.0.1:5800";
            using (var server = new Host(connString))
            {
                server.MessageReceivedIntoQueue += (s, e) =>
                {
                    Message msg;
                    if (!server.TryReceive(out msg))
                    {
                        throw new Exception("no message in queue");
                    }
                    var replyData = new List<byte[]>();
                    replyData.Add(Encoding.UTF8.GetBytes("Hello, I'm the server. You sent."));
                    replyData.AddRange(msg.Frames);
                    server.Send(msg.ClientId, replyData);
                    serverSent = true;
                    Assert.True(msg.Frames.Count == 2, "Server received message did not have 2 frames.");
                    Assert.True(replyData.Count == 3, "Server message did not have 3 frames.");
                };

                using (var client = new Client("me", "mykey", connString))
                {
                    client.MessageSent += (s, e) =>
                    {
                        if (null == e.Message)
                        {
                            throw new Exception("message null");
                        }
                        clientSent = true;
                        Assert.True(e.Message.Frames.Count == 2, "Sent message did not have 2 frames.");
                    };

                    client.MessageReceived += (s, e) =>
                    {
                        if (null == e.Message)
                        {
                            throw new Exception("message null");
                        }
                        clientReceived = true;
                        Assert.True(e.Message.Frames.Count == 3, "Received message did not have 3 frames.");
                    };

                    var clientMessageData = new List<byte[]>();
                    clientMessageData.Add(Encoding.UTF8.GetBytes("Hello, I'm the client."));
                    clientMessageData.Add(Encoding.UTF8.GetBytes("This is my second line."));
                    client.Send(clientMessageData);

                    var count = 0;
                    while (count < 20 && (!clientReceived || !clientSent || !serverSent))
                    {
                        Thread.Sleep(20);
                        count++;
                    }
                    Assert.True(count < 100, "Test took too long.");
                    Assert.True(clientSent, "Client failed to sent.");
                    Assert.True(clientReceived, "Client failed to receive.");
                    Assert.True(serverSent, "Server failed to send.");
                }
            }
        }
    }
}
