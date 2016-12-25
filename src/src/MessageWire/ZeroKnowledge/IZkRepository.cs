using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageWire.ZeroKnowledge
{
    public interface IZkRepository
    {
        ZkPasswordHash GetPasswordHashSet(string username);
    }

    public class ZkNullRepository : IZkRepository
    {
        public ZkPasswordHash GetPasswordHashSet(string username)
        {
            return null;
        }
    }
}
