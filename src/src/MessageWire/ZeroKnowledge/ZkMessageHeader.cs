using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MessageWire.ZeroKnowledge
{
    internal sealed class ZkMessageHeader
    {
        internal const byte SOH = 0x01; //start of header
        internal const byte ENQ = 0x05; //enquiry -- signal protocol handshake message
        internal const byte ACK = 0x06; //acknowledgment -- signal protocol handshake reply
        internal const byte BEL = 0x07; //bell -- signal regular message 

        internal const byte CM1 = 0x08;
        internal const byte CM2 = 0x09;
        internal const byte SM1 = 0x0A;
        internal const byte SF1 = 0x0B;
        internal const byte SM2 = 0x0C;
        internal const byte SF2 = 0x0D;
        internal const byte FF0 = 0x0E;

        internal static byte[] HandshakeRequest1 = new byte[] { SOH, ENQ, CM1, BEL };
        internal static byte[] HandshakeRequest2 = new byte[] { SOH, ENQ, CM2, BEL };

        internal static byte[] HandshakeReply0Failure = new byte[] { SOH, ACK, FF0, BEL };

        internal static byte[] HandshakeReply1Success = new byte[] { SOH, ACK, SM1, BEL };
        internal static byte[] HandshakeReply1Failure = new byte[] { SOH, ACK, SF1, BEL };

        internal static byte[] HandshakeReply2Success = new byte[] { SOH, ACK, SM2, BEL };
        internal static byte[] HandshakeReply2Failure = new byte[] { SOH, ACK, SF2, BEL };
    }
}
