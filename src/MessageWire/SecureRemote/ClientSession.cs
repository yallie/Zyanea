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

using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using MessageWire.Logging;

namespace MessageWire.SecureRemote
{
    internal class ClientSession
    {
        private readonly Protocol _protocol;
        private readonly string _identity;
        private readonly string _identityKey;
        private readonly ILog _logger;

        private byte[] _clientEphemeralA = null;
        private byte[] _clientSessionHash = null;
        private byte[] _clientSessionKey = null;
        private byte[] _scramble = null;
        private Crypto _zkCrypto = null;
        private RSAParameters _clientPublicPrivateKey = default(RSAParameters);
        private RSAParameters _clientPublicKey = default(RSAParameters);
        private RSAParameters _serverPublicKey = default(RSAParameters);

        private DateTime _lastHeartBeatResponse = DateTime.UtcNow;

        public ClientSession(string identity, string identityKey, ILog logger)
        {
            _identity = identity;
            _identityKey = identityKey;
            _logger = logger ?? new NullLogger();
            _protocol = new Protocol();
        }

        public DateTime LastHeartBeat { get { return _lastHeartBeatResponse; } }
        public void RecordHeartBeat()
        {
            _lastHeartBeatResponse = DateTime.UtcNow;
        }

        public Crypto Crypto { get { return _zkCrypto; } }

        public List<byte[]> CreateInitiationRequest()
        {
            var list = new List<byte[]>();
            list.Add(MessageHeader.InitiationRequest);
            using (var rsa = RSA.Create())
            {
                _clientPublicPrivateKey = rsa.ExportParameters(true);
                _clientPublicKey = rsa.ExportParameters(false);
            }
            list.Add(_clientPublicKey.ToBytes());
            return list;
        }

        public List<byte[]> CreateHandshakeRequest(string identity, List<byte[]> initiationResponseFrames)
        {
            if (initiationResponseFrames.Count != 2
                || initiationResponseFrames[0].IsEqualTo(MessageHeader.InititaionResponseFailure)
                || !initiationResponseFrames[0].IsEqualTo(MessageHeader.InititaionResponseSuccess))
            {
                return null;
            }

            _serverPublicKey = initiationResponseFrames[1].ToRSAParameters();
            _clientEphemeralA = _protocol.GetClientEphemeralA(_protocol.CryptRand());


            var list = new List<byte[]>();
            list.Add(MessageHeader.HandshakeRequest);
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(_serverPublicKey);
                list.Add(rsa.Encrypt(Encoding.UTF8.GetBytes(identity), RSAEncryptionPadding.Pkcs1));
                list.Add(rsa.Encrypt(_clientEphemeralA, RSAEncryptionPadding.Pkcs1));
                list.Add(rsa.Encrypt(Encoding.UTF8.GetBytes(Wire.PublicIpAddress), RSAEncryptionPadding.Pkcs1));
            }
            return list;
        }

        public List<byte[]> CreateProofRequest(List<byte[]> handshakeResponseFrames)
        {
            if (handshakeResponseFrames.Count != 3
                || handshakeResponseFrames[0].IsEqualTo(MessageHeader.HandshakeResponseFailure)
                || !handshakeResponseFrames[0].IsEqualTo(MessageHeader.HandshakeResponseSuccess))
            {
                return null;
            }

            byte[] salt = null;
            byte[] bServerEphemeral = null;
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(_clientPublicPrivateKey);
                salt = rsa.Decrypt(handshakeResponseFrames[1], RSAEncryptionPadding.Pkcs1);
                bServerEphemeral = rsa.Decrypt(handshakeResponseFrames[2], RSAEncryptionPadding.Pkcs1);
            }

            _scramble = _protocol.CalculateRandomScramble(_clientEphemeralA, bServerEphemeral);
            _clientSessionKey = _protocol.ClientComputeSessionKey(
                salt,
                _identity,
                _identityKey,
                _clientEphemeralA,
                bServerEphemeral,
                _scramble);

            _clientSessionHash = _protocol.ClientCreateSessionHash(
                _identity,
                salt,
                _clientEphemeralA,
                bServerEphemeral,
                _clientSessionKey);

            var list = new List<byte[]>();
            list.Add(MessageHeader.ProofRequest);
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(_serverPublicKey);
                list.Add(rsa.Encrypt(_clientSessionHash, RSAEncryptionPadding.Pkcs1));
            }
            return list;
        }

        public bool ProcessProofReply(List<byte[]> proofResponseFrames)
        {
            if (proofResponseFrames.Count != 2
                || proofResponseFrames[0].IsEqualTo(MessageHeader.ProofResponseFailure)
                || !proofResponseFrames[0].IsEqualTo(MessageHeader.ProofResponseSuccess))
            {
                return false;
            }

            byte[] serverSessionHash = null;
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(_clientPublicPrivateKey);
                serverSessionHash = rsa.Decrypt(proofResponseFrames[1], RSAEncryptionPadding.Pkcs1);
            }
            byte[] clientServerSessionHash = _protocol.ServerCreateSessionHash(
                _clientEphemeralA,
                _clientSessionHash, 
                _clientSessionKey);

            if (!serverSessionHash.IsEqualTo(clientServerSessionHash))
            {
                return false;
            }
            _zkCrypto = new Crypto(_clientSessionKey, _scramble, _logger);
            return true;
        }
    }
}
