# Zyan vNext PoC

[![Build status](https://ci.appveyor.com/api/projects/status/3pctff9644pdxx3i/branch/master?svg=true)](https://ci.appveyor.com/project/yallie/messagewire/branch/master)

Zyan vNext PoC is a (surprise!) proof-of concept for the next version of Zyan Communication Framework.

Goals:

- Support .NET Core
- Use modern libraries for sockets, IoC, security, etc
- Decouple from .NET Remoting technology
- Support asynchronous interfaces natively
- More?

Non-goals:

- Geterogeneous environment (like connecting from .NET to Java)
- Old versions of .NET framework
- Backwards compatibility with Zyan 2.x

Technology stack currently used:

- NetMQ for transport layer (ZeroMQ protocol)
- MessageWire for zero-knowledge-proof security (SRP v6 protocol)
- Castle.DynamicProxy for the runtime-generated proxies
- DryIoc container for IoC
- Hyperion for polymorphic serialization

# MessageWire
MessageWire is a Secure Remote Password Protocol v6 implementation, a kind of zero Knowledge proof, that enables secure authentication and encryption without passing the actual identity key (password) or any other knowledge required to crack the encryption for messages exchanged between client (dealer socket) and server (router socket) using the NetMQ library, a .NET implementation of ZeroMQ.

Get the [NuGet package](https://www.nuget.org/packages/MessageWire).

Primary sources and references can be found here:

- [Secure Remote Password Protocol](https://en.wikipedia.org/wiki/Secure_Remote_Password_protocol)
- [Zero Knowledge Proof](https://en.wikipedia.org/wiki/Zero-knowledge_proof)
