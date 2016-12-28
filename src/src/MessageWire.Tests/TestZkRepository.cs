using MessageWire.ZeroKnowledge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MessageWire.Tests
{
    public class TestZkRepository : IZkRepository
    {
        private string identityKey = "....++++....";
        private ZkProtocol _protocol = new ZkProtocol();
        private ZkIdentityKeyHash _hash = null;

        public ZkIdentityKeyHash GetIdentityKeyHashSet(string identity)
        {
            if (_hash == null) _hash = _protocol.HashCredentials(identity, identityKey);
            return _hash;
        }
    }
}
