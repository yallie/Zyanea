using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
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

        private string _id = null;
        private ZkIdentityKeyHash _zkPwdHash = null;
        private byte[] _scramble = null;
        private byte[] _serverSessionKey = null;
        private byte[] _clientEphemeralA = null;
        private byte[] _serverEphemeralB = null;

        private ZkCrypto _zkCrypto = null;
        private DateTime _lastTouched = DateTime.UtcNow;

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

        public List<byte[]> RouteHandshakeRequest(List<byte[]> frames)
        {
            if (frames[0][2] == ZkMessageHeader.CM1)
                return CreateHandshakeReply1(frames);
            else
                return CreateHandshakeReply2(frames);
        }

        private List<byte[]> CreateHandshakeReply1(List<byte[]> frames)
        {
            var list = new List<byte[]>();
            if (frames.Count != 3)
            {
                list.Add(ZkMessageHeader.HandshakeReply1Failure);
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
            }
            else
            {
                _id = Encoding.UTF8.GetString(frames[1]);
                _clientEphemeralA = frames[2];
                _zkPwdHash = _repository.GetIdentityKeyHashSet(_id);
                if (null == _zkPwdHash)
                {
                    list.Add(ZkMessageHeader.HandshakeReply1Failure);
                    list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                    list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
                }
                else
                {
                    list.Add(ZkMessageHeader.HandshakeReply1Success);
                    _serverEphemeralB = _protocol.GetServerEphemeralB(_zkPwdHash.Salt,
                        _zkPwdHash.Verifier, _protocol.CryptRand());

                    _scramble = _protocol.CalculateRandomScramble(_clientEphemeralA, _serverEphemeralB);

                    _serverSessionKey = _protocol.ServerComputeSessionKey(_zkPwdHash.Salt, _zkPwdHash.Key,
                        _clientEphemeralA, _serverEphemeralB, _scramble);

                    list.Add(_zkPwdHash.Salt);
                    list.Add(_serverEphemeralB);
                }
            }
            return list;
        }

        private List<byte[]> CreateHandshakeReply2(List<byte[]> frames)
        {
            if (frames.Count != 2) throw new ArgumentException("Invalid frame count.", nameof(frames));

            var clientSessionHash = frames[1];
            var serverClientSessionHash = _protocol.ClientCreateSessionHash(_id, _zkPwdHash.Salt,
                _clientEphemeralA, _serverEphemeralB, _serverSessionKey);

            var list = new List<byte[]>();
            if (!clientSessionHash.IsEqualTo(serverClientSessionHash))
            {
                list.Add(ZkMessageHeader.HandshakeReply2Failure);
                list.Add(_protocol.ComputeHash(_protocol.CryptRand()));
            }
            else
            {
                list.Add(ZkMessageHeader.HandshakeReply2Success);
                var serverSessionHash = _protocol.ServerCreateSessionHash(_clientEphemeralA, 
                    clientSessionHash, _serverSessionKey);
                _zkCrypto = new ZkCrypto(_serverSessionKey, _scramble);
                list.Add(serverSessionHash);
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
