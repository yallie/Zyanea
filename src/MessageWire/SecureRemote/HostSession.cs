/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * 
 *  MessageWire - https://github.com/tylerjensen/MessageWire
 *  
 * The MIT License (MIT)
 * Copyright (C) 2016-2017 Tyler Jensen
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
 * documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
 * TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
 * CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 * DEALINGS IN THE SOFTWARE.
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using MessageWire.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MessageWire.SecureRemote
{
    internal class HostSession : IEquatable<HostSession>
    {
        private readonly Protocol _protocol;
        private readonly IKeyRepository _repository;
        private readonly Guid _clientId;
        private readonly DateTime _created;
        private readonly ILog _logger;

        private string _clientIpAddress = null;
        private string _identity = null;
        private IdentityKeyHash _identityHash = null;
        private byte[] _scramble = null;
        private byte[] _serverSessionKey = null;
        private byte[] _clientEphemeralA = null;
        private byte[] _serverEphemeralB = null;

        private Crypto _zkCrypto = null;
        private DateTime _lastHeartbeatReceived = DateTime.UtcNow;
        private DateTime _lastMessageReceived = DateTime.UtcNow;

        private RSAParameters _serverPublicPrivateKey = default(RSAParameters);
        private RSAParameters _serverPublicKey = default(RSAParameters);
        private RSAParameters _clientPublicKey = default(RSAParameters);


        public HostSession(IKeyRepository repository, Guid clientId, ILog logger)
        {
            _repository = repository;
            _clientId = clientId;
            _logger = logger ?? new NullLogger();
            _created = DateTime.UtcNow;
            _lastMessageReceived = _created;
            _protocol = new Protocol();
        }

        public Guid ClientId { get { return _clientId; } }
        public string ClientIpAddress { get { return _clientIpAddress; } }
        public string ClientIdentity { get { return _identity; } }
        public DateTime Created { get { return _created; } }
        public DateTime LastMessageReceived { get { return _lastMessageReceived; } }
        public DateTime LastHeartbeatReceived { get { return _lastHeartbeatReceived; } }
        public int HeartBeatsReceivedCount { get; private set; }
        public int MessagesReceivedCount { get; private set; }
        public void RecordHeartBeat()
        {
            HeartBeatsReceivedCount++;
            _lastHeartbeatReceived = DateTime.UtcNow;
        }
        public void RecordMessageReceived()
        {
            MessagesReceivedCount++;
            _lastMessageReceived = DateTime.UtcNow;
        }

        public Crypto Crypto 
        {
            get 
            {
                return _zkCrypto;
            }
        }

        public List<byte[]> ProcessProtocolRequest(Message message)
        {
            var frames = message.Frames;
            if (frames[0][2] == MessageHeader.CM0)
                return ProcessInitiationRequest(message);
            else if (frames[0][2] == MessageHeader.CM1)
                return ProcessHandshakeRequest(message);
            else
                return ProcessProofRequest(message);
        }

        private List<byte[]> ProcessInitiationRequest(Message message)
        {
            var frames = message.Frames;
            var list = new List<byte[]>();
            if (frames.Count != 2)
            {
                list.Add(MessageHeader.HandshakeResponseFailure);
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                _logger.Debug("Protocol initiation failed for {0}.", message.ClientId);
            }
            else
            {
                _clientPublicKey = frames[1].ToRSAParameters();
                using (var rsa = RSA.Create())
                {
                    _serverPublicPrivateKey = rsa.ExportParameters(true);
                    _serverPublicKey = rsa.ExportParameters(false);
                }
                list.Add(MessageHeader.InititaionResponseSuccess);
                list.Add(_serverPublicKey.ToBytes());
                _logger.Debug("Protocol initiation completed for {0}.", message.ClientId);
            }
            return list;
        }

        private List<byte[]> ProcessHandshakeRequest(Message message)
        {
            var frames = message.Frames;
            var list = new List<byte[]>();
            if (frames.Count != 4)
            {
                list.Add(MessageHeader.HandshakeResponseFailure);
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                _logger.Debug("Protocol handshake failed for {0}.", message.ClientId);
            }
            else
            {
                using (var rsa = RSA.Create())
                {
                    rsa.ImportParameters(_serverPublicPrivateKey);
                    _identity = Encoding.UTF8.GetString(rsa.Decrypt(frames[1], RSAEncryptionPadding.Pkcs1));
                    _clientEphemeralA = rsa.Decrypt(frames[2], RSAEncryptionPadding.Pkcs1);
                    _clientIpAddress = Encoding.UTF8.GetString(rsa.Decrypt(frames[3], RSAEncryptionPadding.Pkcs1));
                }
                _identityHash = _repository.GetIdentityKeyHashSet(_identity);

                if (null == _identityHash)
                {
                    list.Add(MessageHeader.HandshakeResponseFailure);
                    list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                    list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                    _logger.Debug("Protocol handshake failed for {0}.", message.ClientId);
                }
                else
                {
                    _serverEphemeralB = _protocol.GetServerEphemeralB(_identityHash.Salt,
                    _identityHash.Verifier, _protocol.CryptRand());

                    _scramble = _protocol.CalculateRandomScramble(_clientEphemeralA, _serverEphemeralB);

                    _serverSessionKey = _protocol.ServerComputeSessionKey(_identityHash.Salt, _identityHash.Key,
                        _clientEphemeralA, _serverEphemeralB, _scramble);

                    list.Add(MessageHeader.HandshakeResponseSuccess);
                    using (var rsa = RSA.Create())
                    {
                        rsa.ImportParameters(_clientPublicKey);
                        list.Add(rsa.Encrypt(_identityHash.Salt, RSAEncryptionPadding.Pkcs1));
                        list.Add(rsa.Encrypt(_serverEphemeralB, RSAEncryptionPadding.Pkcs1));
                    }
                    _logger.Debug("Protocol handshake completed for {0}.", message.ClientId);
                }
            }
            return list;
        }

        private List<byte[]> ProcessProofRequest(Message message)
        {
            var frames = message.Frames;
            if (frames.Count != 2) throw new ArgumentException("Invalid frame count.", nameof(frames));

            byte[] clientSessionHash = frames[1];

            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(_serverPublicPrivateKey);
                clientSessionHash = rsa.Decrypt(frames[1], RSAEncryptionPadding.Pkcs1);
            }

            var serverClientSessionHash = _protocol.ClientCreateSessionHash(_identity, _identityHash.Salt,
                _clientEphemeralA, _serverEphemeralB, _serverSessionKey);

            var list = new List<byte[]>();
            if (!clientSessionHash.IsEqualTo(serverClientSessionHash))
            {
                list.Add(MessageHeader.ProofResponseFailure);
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                _logger.Debug("Protocol proof failed for {0}.", message.ClientId);
            }
            else
            {
                var serverSessionHash = _protocol.ServerCreateSessionHash(_clientEphemeralA, 
                    clientSessionHash, _serverSessionKey);
                _zkCrypto = new Crypto(_serverSessionKey, _scramble, _logger);

                list.Add(MessageHeader.ProofResponseSuccess);
                using (var rsa = RSA.Create())
                {
                    rsa.ImportParameters(_clientPublicKey);
                    list.Add(rsa.Encrypt(serverSessionHash, RSAEncryptionPadding.Pkcs1));
                }
                _logger.Debug("Protocol proof completed for {0}.", message.ClientId);
            }
            return list;
        }

        bool IEquatable<HostSession>.Equals(HostSession other)
        {
            return Equals(other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as HostSession);
        }

        public bool Equals(HostSession other)
        {
            return _clientId.Equals(other);
        }

        public override int GetHashCode()
        {
            return _clientId.GetHashCode();
        }
    }
}
