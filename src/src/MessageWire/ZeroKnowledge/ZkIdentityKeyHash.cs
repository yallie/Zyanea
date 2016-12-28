namespace MessageWire.ZeroKnowledge
{
    public class ZkIdentityKeyHash
    {
        public byte[] Salt { get; set; }
        public byte[] Key { get; set; }
        public byte[] Verifier { get; set; }
    }
}