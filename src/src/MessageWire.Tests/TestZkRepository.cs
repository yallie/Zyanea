using MessageWire.ZeroKnowledge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MessageWire.Tests
{
    public class TestZkRepository : IZkRepository
    {
        private string password = "....++++....";
        private ZkProtocol _protocol = new ZkProtocol();
        private ZkPasswordHash _hash = null;

        public ZkPasswordHash GetPasswordHashSet(string username)
        {
            if (_hash == null) _hash = _protocol.HashCredentials(username, password);
            return _hash;
        }
    }
}
