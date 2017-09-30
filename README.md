# Zyanea

[![Appveyor build status](https://ci.appveyor.com/api/projects/status/by3vda06qflkufao?svg=true)](https://ci.appveyor.com/project/yallie/zyanea)
[![Travis-CI build status](https://travis-ci.org/yallie/Zyanea.svg?branch=master)](https://travis-ci.org/yallie/Zyanea)

Zyanea is the codename for the next version of [Zyan Communication Framework](http://zyan.com.de).  
It's an easy to use secure distributed application framework for .NET Core.

Goals:

- Support .NET Core
- Decouple from .NET Remoting technology
- Use modern libraries for sockets, IoC, security, serialization, etc
- Support asynchronous interfaces natively: return values of Task/ValueTask
- Provide all Zyan services: events, remote LINQ, authentication, sessions, etc
- Support serialization of POCOs, LINQ expressions, delegates, streams
- More?

Non-goals:

- Heterogeneous environment (like connecting from .NET to Java)
- Obsolete versions of .NET framework
- Transport-level compatibility with Zyan 2.x

## Code sample

```c#
// shared library
public interface IHelloService
{
    Task SayHello(string message);
}

// server
using (var server = new ZyanServer("tcp://127.0.0.1:5800"))
{
    server.Register<IHelloService, HelloService>();
    Console.ReadLine();
}

// client
using (var client = new ZyanClient("tcp://127.0.0.1:5800"))
{
    var proxy = client.CreateProxy<IHelloService>();
    await proxy.SayHello("World");
}
```

# Technology stack currently used:

- NetMQ for transport layer (ZeroMQ protocol)
- MessageWire for zero-knowledge-proof security (SRP v6 protocol)
- Castle.DynamicProxy for the runtime-generated proxies
- DryIoc container for IoC
- Hyperion for polymorphic serialization

## NetMQ

NetMQ is a 100% native C# port of the lightweight messaging library ZeroMQ.

NetMQ extends the standard socket interfaces with features traditionally 
provided by specialised messaging middleware products. NetMQ sockets provide 
an abstraction of asynchronous message queues, multiple messaging patterns,
message filtering (subscriptions), seamless access to multiple transport
protocols, and more.

## MessageWire

MessageWire is a Secure Remote Password Protocol v6 implementation, a kind of 
zero knowledge proof, that enables secure authentication and encryption without 
passing the actual identity key (password) or any other knowledge required 
to crack the encryption for messages exchanged between client (dealer socket) 
and server (router socket) using the NetMQ library, a .NET implementation of ZeroMQ.

Get the [NuGet package](https://www.nuget.org/packages/MessageWire).

Primary sources and references can be found here:

- [Secure Remote Password Protocol](https://en.wikipedia.org/wiki/Secure_Remote_Password_protocol)
- [Zero Knowledge Proof](https://en.wikipedia.org/wiki/Zero-knowledge_proof)

## Castle.DynamicProxy

Castle.DynamicProxy is a lightweight, lightning fast framework for generating 
proxies on the fly, used extensively by multiple projects within Castle (Windsor, 
MonoRail) and outside of it (NHibernate, Rhino Mocks, AutoMapper and many others).

## DryIoc

DryIoc is fast, small, full-featured IoC Container for .NET 
designed for low-ceremony use, performance, and extensibility.

## Hyperion

A high performance polymorphic serializer for the .NET framework, fork of the Wire serializer.

Hyperion was designed to safely transfer messages in distributed systems, 
for example service bus or actor model based systems. In message based systems, 
it is common to receive different types of messages and apply pattern matching 
over those messages. If the messages does not carry over all the relevant type 
information to the receiveing side, the message might no longer match exactly 
what your system expect.
