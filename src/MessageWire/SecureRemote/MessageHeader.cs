/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * 
 *  MessageWire - https://github.com/tylerjensen/MessageWire
 *  
 * The MIT License (MIT)
 * Copyright (C) 2016-2017 Tyler Jensen
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
 * documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
 * TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
 * CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 * DEALINGS IN THE SOFTWARE.
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

namespace MessageWire.SecureRemote
{
    internal sealed class MessageHeader
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

        internal const byte CM0 = 0x10; //initiation request
        internal const byte SM0 = 0x11; //initiation response success
        internal const byte SF0 = 0x12; //initiation response failure

        internal static byte[] InitiationRequest = new byte[] { SOH, ENQ, CM0, BEL };
        internal static byte[] HandshakeRequest = new byte[] { SOH, ENQ, CM1, BEL };
        internal static byte[] ProofRequest = new byte[] { SOH, ENQ, CM2, BEL };

        internal static byte[] ProtocolResponseFailure = new byte[] { SOH, ACK, FF0, BEL };

        internal static byte[] InititaionResponseSuccess = new byte[] { SOH, ACK, SM0, BEL };
        internal static byte[] InititaionResponseFailure = new byte[] { SOH, ACK, SF0, BEL };

        internal static byte[] HandshakeResponseSuccess = new byte[] { SOH, ACK, SM1, BEL };
        internal static byte[] HandshakeResponseFailure = new byte[] { SOH, ACK, SF1, BEL };

        internal static byte[] ProofResponseSuccess = new byte[] { SOH, ACK, SM2, BEL };
        internal static byte[] ProofResponseFailure = new byte[] { SOH, ACK, SF2, BEL };

        internal static byte[] HeartBeat = new byte[] { SOH, ACK, BEL, BEL };
    }
}
