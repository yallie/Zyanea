using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageWire.ZeroKnowledge
{
    public interface IZkRepository
    {
        ZkIdentityKeyHash GetIdentityKeyHashSet(string identity);
    }

    public class ZkNullRepository : IZkRepository
    {
        public ZkIdentityKeyHash GetIdentityKeyHashSet(string identity)
        {
            return null;
        }
    }
}
