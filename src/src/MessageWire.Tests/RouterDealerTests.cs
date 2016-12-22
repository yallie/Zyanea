using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessageWire;
using System.Text;

namespace MessageWire.Tests
{
    public class RouterDealerTests
    {
        public void TestSomething()
        {
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
                };

                using (var client = new Client("me", "mykey", connString))
                {
                    client.MessageSent += (s, e) =>
                    {
                        if (null == e.Message)
                        {
                            throw new Exception("message null");
                        }
                    };

                    client.MessageReceived += (s, e) =>
                    {
                        if (null == e.Message)
                        {
                            throw new Exception("message null");
                        }
                    };

                    var clientMessageData = new List<byte[]>();
                    clientMessageData.Add(Encoding.UTF8.GetBytes("Hello, I'm the client."));
                    clientMessageData.Add(Encoding.UTF8.GetBytes("This is my second line."));
                    client.Send(clientMessageData);

                    Console.WriteLine("hit enter to quit waiting");
                    Console.ReadLine();
                }
            }
        }
    }
}
