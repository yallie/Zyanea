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

using System;
using MessageWire.SecureRemote;

namespace MessageWire
{
    public class Session
    {
        private readonly HostSession _session;

        internal Session(HostSession session)
        {
            _session = session;
        }

        public Guid ClientId {
            get {
                return _session.ClientId;
            }
        }
        public string ClientIpAddress {
            get {
                return _session.ClientIpAddress;
            }
        }
        public string ClientIdentity {
            get {
                return _session.ClientIdentity;
            }
        }

        public DateTime Created {
            get {
                return _session.Created;
            }
        }
        public DateTime LastMessageReceived {
            get {
                return _session.LastMessageReceived;
            }
        }
        public DateTime LastHeartbeatReceived {
            get {
                return _session.LastHeartbeatReceived;
            }
        }
        public int HeartBeatsReceivedCount {
            get {
                return _session.HeartBeatsReceivedCount;
            }
        }
        public int MessagesReceivedCount {
            get {
                return _session.MessagesReceivedCount;
            }
        }
    }
}
