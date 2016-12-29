using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

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
        private RSAParameters _clientPublicPrivateKey = default(RSAParameters);
        private RSAParameters _clientPublicKey = default(RSAParameters);
        private RSAParameters _serverPublicKey = default(RSAParameters);

        public ZkProtocolClientSession(string id, string key)
        {
            _id = id;
            _key = key;
            _protocol = new ZkProtocol();
        }

        public ZkCrypto Crypto { get { return _zkCrypto; } }

        public List<byte[]> CreateInitiationRequest()
        {
            var list = new List<byte[]>();
            list.Add(ZkMessageHeader.InitiationRequest);
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
                || initiationResponseFrames[0].IsEqualTo(ZkMessageHeader.InititaionResponseFailure)
                || !initiationResponseFrames[0].IsEqualTo(ZkMessageHeader.InititaionResponseSuccess))
            {
                return null;
            }

            _serverPublicKey = initiationResponseFrames[1].ToRSAParameters();

            _clientEphemeralA = _protocol.GetClientEphemeralA(_protocol.CryptRand());

            var list = new List<byte[]>();
            list.Add(ZkMessageHeader.HandshakeRequest);
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(_serverPublicKey);
                list.Add(rsa.Encrypt(Encoding.UTF8.GetBytes(identity), RSAEncryptionPadding.Pkcs1));
                list.Add(rsa.Encrypt(_clientEphemeralA, RSAEncryptionPadding.Pkcs1));
            }
            return list;
        }

        public List<byte[]> CreateProofRequest(List<byte[]> handshakeResponseFrames)
        {
            if (handshakeResponseFrames.Count != 3
                || handshakeResponseFrames[0].IsEqualTo(ZkMessageHeader.HandshakeResponseFailure)
                || !handshakeResponseFrames[0].IsEqualTo(ZkMessageHeader.HandshakeResponseSuccess))
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
                _id,
                _key,
                _clientEphemeralA,
                bServerEphemeral,
                _scramble);

            _clientSessionHash = _protocol.ClientCreateSessionHash(
                _id,
                salt,
                _clientEphemeralA,
                bServerEphemeral,
                _clientSessionKey);

            var list = new List<byte[]>();
            list.Add(ZkMessageHeader.ProofRequest);
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
                || proofResponseFrames[0].IsEqualTo(ZkMessageHeader.ProofResponseFailure)
                || !proofResponseFrames[0].IsEqualTo(ZkMessageHeader.ProofResponseSuccess))
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
            _zkCrypto = new ZkCrypto(_clientSessionKey, _scramble);
            return true;
        }
    }
}
