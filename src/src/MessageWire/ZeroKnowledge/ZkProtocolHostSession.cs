using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MessageWire.ZeroKnowledge
{
    internal class ZkProtocolHostSession : IEquatable<ZkProtocolHostSession>
    {
        private readonly ZkProtocol _protocol;
        private readonly IZkRepository _repository;
        private readonly Guid _clientId;
        private readonly DateTime _created;

        private string _clientIpAddress = null;
        private string _identity = null;
        private ZkIdentityKeyHash _identityHash = null;
        private byte[] _scramble = null;
        private byte[] _serverSessionKey = null;
        private byte[] _clientEphemeralA = null;
        private byte[] _serverEphemeralB = null;

        private ZkCrypto _zkCrypto = null;
        private DateTime _lastTouched = DateTime.UtcNow;

        private RSAParameters _serverPublicPrivateKey = default(RSAParameters);
        private RSAParameters _serverPublicKey = default(RSAParameters);
        private RSAParameters _clientPublicKey = default(RSAParameters);


        public ZkProtocolHostSession(IZkRepository repository, Guid clientId)
        {
            _repository = repository;
            _clientId = clientId;
            _created = DateTime.UtcNow;
            _lastTouched = _created;
            _protocol = new ZkProtocol();
        }

        public DateTime Created { get { return _created; } }
        public DateTime LastTouched { get { return _lastTouched; } }
        public ZkCrypto Crypto 
        {
            get 
            {
                _lastTouched = DateTime.UtcNow;
                return _zkCrypto;
            }
        }

        public List<byte[]> ProcessProtocolRequest(List<byte[]> frames)
        {
            if (frames[0][2] == ZkMessageHeader.CM0)
                return ProcessInitiationRequest(frames);
            else if (frames[0][2] == ZkMessageHeader.CM1)
                return ProcessHandshakeRequest(frames);
            else
                return ProcessProofRequest(frames);
        }

        private List<byte[]> ProcessInitiationRequest(List<byte[]> frames)
        {
            var list = new List<byte[]>();
            if (frames.Count != 2)
            {
                list.Add(ZkMessageHeader.HandshakeResponseFailure);
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
            }
            else
            {
                _clientPublicKey = frames[1].ToRSAParameters();
                using (var rsa = RSA.Create())
                {
                    _serverPublicPrivateKey = rsa.ExportParameters(true);
                    _serverPublicKey = rsa.ExportParameters(false);
                }
                list.Add(ZkMessageHeader.InititaionResponseSuccess);
                list.Add(_serverPublicKey.ToBytes());
            }
            return list;
        }

        private List<byte[]> ProcessHandshakeRequest(List<byte[]> frames)
        {
            var list = new List<byte[]>();
            if (frames.Count != 4)
            {
                list.Add(ZkMessageHeader.HandshakeResponseFailure);
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
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
                    list.Add(ZkMessageHeader.HandshakeResponseFailure);
                    list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                    list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                }
                else
                {
                    _serverEphemeralB = _protocol.GetServerEphemeralB(_identityHash.Salt,
                    _identityHash.Verifier, _protocol.CryptRand());

                    _scramble = _protocol.CalculateRandomScramble(_clientEphemeralA, _serverEphemeralB);

                    _serverSessionKey = _protocol.ServerComputeSessionKey(_identityHash.Salt, _identityHash.Key,
                        _clientEphemeralA, _serverEphemeralB, _scramble);

                    list.Add(ZkMessageHeader.HandshakeResponseSuccess);
                    using (var rsa = RSA.Create())
                    {
                        rsa.ImportParameters(_clientPublicKey);
                        list.Add(rsa.Encrypt(_identityHash.Salt, RSAEncryptionPadding.Pkcs1));
                        list.Add(rsa.Encrypt(_serverEphemeralB, RSAEncryptionPadding.Pkcs1));
                    }
                }
            }
            return list;
        }

        private List<byte[]> ProcessProofRequest(List<byte[]> frames)
        {
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
                list.Add(ZkMessageHeader.ProofResponseFailure);
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
            }
            else
            {
                var serverSessionHash = _protocol.ServerCreateSessionHash(_clientEphemeralA, 
                    clientSessionHash, _serverSessionKey);
                _zkCrypto = new ZkCrypto(_serverSessionKey, _scramble);

                list.Add(ZkMessageHeader.ProofResponseSuccess);
                using (var rsa = RSA.Create())
                {
                    rsa.ImportParameters(_clientPublicKey);
                    list.Add(rsa.Encrypt(serverSessionHash, RSAEncryptionPadding.Pkcs1));
                }
            }
            return list;
        }

        bool IEquatable<ZkProtocolHostSession>.Equals(ZkProtocolHostSession other)
        {
            return Equals(other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ZkProtocolHostSession);
        }

        public bool Equals(ZkProtocolHostSession other)
        {
            return _clientId.Equals(other);
        }

        public override int GetHashCode()
        {
            return _clientId.GetHashCode();
        }
    }
}
