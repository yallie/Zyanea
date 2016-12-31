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
    public class RouterDealerTests : IDisposable
    {
        public RouterDealerTests()
        {
            Wire.Linger = new TimeSpan(0, 0, 0, 1);
        }

        void IDisposable.Dispose()
        {
            Wire.Cleanup();
        }

        [Fact]
        public void BasicSendReceiveTest()
        {
            var serverReceived = false;
            var clientReceived = false;
            var connString = "tcp://127.0.0.1:5800";
            using (var server = new Host(connString))
            {
                server.MessageReceived += (s, e) =>
                {
                    Assert.Equal("Hello, I'm the client.", e.Message.Frames[0].ConvertToString());
                    Assert.Equal("This is my second line.", e.Message.Frames[1].ConvertToString());

                    var replyData = new List<byte[]>();
                    replyData.Add("Hello, I'm the server. You sent.".ConvertToBytes());
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
                        Assert.Equal("Hello, I'm the server. You sent.", e.Message.Frames[0].ConvertToString());
                        Assert.Equal("Hello, I'm the client.", e.Message.Frames[1].ConvertToString());
                        Assert.Equal("This is my second line.", e.Message.Frames[2].ConvertToString());
                    };

                    var clientMessageData = new List<byte[]>();
                    clientMessageData.Add("Hello, I'm the client.".ConvertToBytes());
                    clientMessageData.Add("This is my second line.".ConvertToBytes());
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

        [Fact]
        public void EncryptedSendReceiveTest()
        {
            var serverReceived = false;
            var clientReceived = false;
            var connString = "tcp://127.0.0.1:5800";
            using (var server = new Host(connString, new TestZkRepository()))
            {
                server.MessageReceived += (s, e) =>
                {
                    Assert.Equal("Hello, I'm the client.", e.Message.Frames[0].ConvertToString());
                    Assert.Equal("This is my second line.", e.Message.Frames[1].ConvertToString());

                    var replyData = new List<byte[]>();
                    replyData.Add("Hello, I'm the server. You sent.".ConvertToBytes());
                    replyData.AddRange(e.Message.Frames);
                    server.Send(e.Message.ClientId, replyData);
                    serverReceived = true;
                    Assert.True(e.Message.Frames.Count == 2, "Server received message did not have 2 frames.");
                    Assert.True(replyData.Count == 3, "Server message did not have 3 frames.");
                };

                using (var client = new Client(connString, "testid", "....++++...."))
                {
                    var established = false;
                    client.EcryptionProtocolEstablished += (s, e) =>
                    {
                        established = true;
                    };

                    client.EcryptionProtocolFailed += (s, e) =>
                    {
                        Assert.True(false, "Protocol failed.");
                    };

                    client.MessageReceived += (s, e) =>
                    {
                        clientReceived = true;
                        Assert.True(e.Message.Frames.Count == 3, "Received message did not have 3 frames.");
                        Assert.Equal("Hello, I'm the server. You sent.", e.Message.Frames[0].ConvertToString());
                        Assert.Equal("Hello, I'm the client.", e.Message.Frames[1].ConvertToString());
                        Assert.Equal("This is my second line.", e.Message.Frames[2].ConvertToString());
                    };

                    client.SecureConnection(blockUntilComplete: false);

                    var count = 0;
                    while (count < 20000 && !established)
                    {
                        Thread.Sleep(20);
                        count++;
                    }
                    Assert.True(count < 20000, "SecureConnection took too long.");
                    Assert.True(established, "SecureConnection not established.");

                    var clientMessageData = new List<byte[]>();
                    clientMessageData.Add("Hello, I'm the client.".ConvertToBytes());
                    clientMessageData.Add("This is my second line.".ConvertToBytes());
                    client.Send(clientMessageData);

                    count = 0;
                    while (count < 20 && (!established || !clientReceived || !serverReceived))
                    {
                        Thread.Sleep(20);
                        count++;
                    }
                    Assert.True(count < 20, "Test took too long.");
                    Assert.True(serverReceived, "Server failed to receive and send.");
                    Assert.True(clientReceived, "Client failed to receive.");
                }
            }
        }


        [Fact]
        public void HeartBeatTest()
        {
            var serverReceived = false;
            var clientReceived = false;
            var connString = "tcp://127.0.0.1:5800";
            using (var server = new Host(connString, new TestZkRepository()))
            {
                server.MessageReceived += (s, e) =>
                {
                    Assert.Equal("Hello, I'm the client.", e.Message.Frames[0].ConvertToString());
                    Assert.Equal("This is my second line.", e.Message.Frames[1].ConvertToString());

                    var replyData = new List<byte[]>();
                    replyData.Add("Hello, I'm the server. You sent.".ConvertToBytes());
                    replyData.AddRange(e.Message.Frames);
                    server.Send(e.Message.ClientId, replyData);
                    serverReceived = true;
                    Assert.True(e.Message.Frames.Count == 2, "Server received message did not have 2 frames.");
                    Assert.True(replyData.Count == 3, "Server message did not have 3 frames.");
                };

                using (var client = new Client(connString, "testid", "....++++....", 
                    heartBeatIntervalMs: 1000, maxSkippedHeartBeatReplies: 4))
                {
                    var established = false;
                    client.EcryptionProtocolEstablished += (s, e) =>
                    {
                        established = true;
                    };

                    client.EcryptionProtocolFailed += (s, e) =>
                    {
                        Assert.True(false, "Protocol failed.");
                    };

                    client.MessageReceived += (s, e) =>
                    {
                        clientReceived = true;
                        Assert.True(e.Message.Frames.Count == 3, "Received message did not have 3 frames.");
                        Assert.Equal("Hello, I'm the server. You sent.", e.Message.Frames[0].ConvertToString());
                        Assert.Equal("Hello, I'm the client.", e.Message.Frames[1].ConvertToString());
                        Assert.Equal("This is my second line.", e.Message.Frames[2].ConvertToString());
                    };

                    client.SecureConnection(blockUntilComplete: false);

                    var count = 0;
                    while (count < 200 && !established)
                    {
                        Thread.Sleep(20);
                        count++;
                    }
                    Assert.True(count < 200, "SecureConnection took too long.");
                    Assert.True(established, "SecureConnection not established.");

                    var clientMessageData = new List<byte[]>();
                    clientMessageData.Add("Hello, I'm the client.".ConvertToBytes());
                    clientMessageData.Add("This is my second line.".ConvertToBytes());
                    client.Send(clientMessageData);

                    count = 0;
                    while (count < 20 && (!established || !clientReceived || !serverReceived))
                    {
                        Thread.Sleep(20);
                        count++;
                    }
                    Assert.True(count < 20, "Test took too long.");
                    Assert.True(serverReceived, "Server failed to receive and send.");
                    Assert.True(clientReceived, "Client failed to receive.");

                    count = 0;
                    while (count < 200 && client.HeartBeatsSentCount < 2 && client.HeartBeatsReceivedCount < 2)
                    {
                        Thread.Sleep(500);
                        count++;
                    }

                    Assert.True(count < 200, "Test took too long.");
                    var serverKeys = server.GetCurrentSessionKeys();
                    Assert.Equal(1, serverKeys.Length);
                    var session = server.GetSession(serverKeys[0]);
                    Assert.NotNull(session);
                    Assert.True("testid" == session.ClientIdentity, "Client identity does not match.");
                    Assert.True(client.ClientId == session.ClientId, "ClientId does not match.");
                    Assert.True(session.HeartBeatsReceivedCount > 1, "Server failed to receive heartbeats.");
                }
            }
        }
    }
}
