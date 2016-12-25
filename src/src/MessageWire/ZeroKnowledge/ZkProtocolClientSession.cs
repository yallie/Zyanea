using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace MessageWire.ZeroKnowledge
{
    internal class ZkProtocolClientSession
    {
        private readonly ZkProtocol _protocol;
        private readonly string _id;
        private readonly string _key;

        private byte[] _clientEphemeralA = null;
        private byte[] _clientSessionHash = null;
        private byte[] _clientSessionKey = null;
        private byte[] _scramble = null;
        private ZkCrypto _zkCrypto = null;

        public ZkProtocolClientSession(string id, string key)
        {
            _id = id;
            _key = key;
            _protocol = new ZkProtocol();
        }

        public ZkCrypto Crypto { get { return _zkCrypto; } }

        /// <summary>
        /// Create first set of message frames for initiating the ZkProtocol.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<byte[]> CreateHandshakeMessage1(string id)
        {
            var list = new List<byte[]>();
            list.Add(ZkMessageHeader.HandshakeRequest1);
            list.Add(Encoding.UTF8.GetBytes(id));

            _clientEphemeralA = _protocol.GetClientEphemeralA(_protocol.CryptRand());
            list.Add(_clientEphemeralA);
            return list;
        }

        public List<byte[]> CreateHandshakeMessage2(List<byte[]> frames)
        {
            if (frames.Count != 3
                || frames[0].IsEqualTo(ZkMessageHeader.HandshakeReply1Failure)
                || !frames[0].IsEqualTo(ZkMessageHeader.HandshakeReply1Success))
            {
                return null;
            }

            var salt = frames[1];
            var bServerEphemeral = frames[2];

            var list = new List<byte[]>();
            list.Add(ZkMessageHeader.HandshakeRequest2);

            _scramble = _protocol.CalculateRandomScramble(_clientEphemeralA, bServerEphemeral);
            _clientSessionKey = _protocol.ClientComputeSessionKey(salt, _id, _key,
                _clientEphemeralA, bServerEphemeral, _scramble);

            _clientSessionHash = _protocol.ClientCreateSessionHash(_id, salt, _clientEphemeralA,
                bServerEphemeral, _clientSessionKey);
            list.Add(_clientSessionHash);
            return list;
        }

        public bool ProcessHandshakeReply2(List<byte[]> frames)
        {
            if (frames.Count != 2
                || frames[0].IsEqualTo(ZkMessageHeader.HandshakeReply2Failure)
                || !frames[0].IsEqualTo(ZkMessageHeader.HandshakeReply2Success))
            {
                return false;
            }

            var serverSessionHash = frames[1];
            var clientServerSessionHash = _protocol.ServerCreateSessionHash(_clientEphemeralA,
                _clientSessionHash, _clientSessionKey);
            if (!serverSessionHash.IsEqualTo(clientServerSessionHash))
            {
                return false;
            }
            _zkCrypto = new ZkCrypto(_clientSessionKey, _scramble);
            return true;
        }
    }
}
