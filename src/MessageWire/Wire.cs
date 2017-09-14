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

using NetMQ;
using System;
using System.Net.Http;

namespace MessageWire
{
    /// <summary>
    /// Critical pass through to key NetMQConfig methods.
    /// </summary>
    public static class Wire
    {
        /// <summary>
        /// Cleanup library resources, call this method when your process is shutting-down.
        /// </summary>
        /// <param name="block">Set to true when you want to make sure sockets send all pending messages</param>
        public static void Cleanup(bool block = true)
        {
            _publicIpAddress = string.Empty;
            NetMQConfig.Cleanup(block);
        }

        /// <summary>
        /// Get or set the default linger period for the all sockets,
        /// which determines how long pending messages which have yet to be sent to a peer
        /// shall linger in memory after a socket is closed.
        /// </summary>
        /// <remarks>
        /// This also affects the termination of the socket's context.
        /// -1: Specifies infinite linger period. Pending messages shall not be discarded after the socket is closed;
        /// attempting to terminate the socket's context shall block until all pending messages have been sent to a peer.
        /// 0: The default value of 0 specifies an no linger period. Pending messages shall be discarded immediately when the socket is closed.
        /// Positive values specify an upper bound for the linger period. Pending messages shall not be discarded after the socket is closed;
        /// attempting to terminate the socket's context shall block until either all pending messages have been sent to a peer,
        /// or the linger period expires, after which any pending messages shall be discarded.
        /// </remarks>
        public static TimeSpan Linger {
            get {
                return NetMQConfig.Linger;
            }
            set {
                NetMQConfig.Linger = value;
            }
        }

        /// <summary>
        /// Get or set the number of IO Threads NetMQ will create, default is 1.
        /// 1 is good for most cases.
        /// </summary>
        public static int ThreadPoolSize {
            get {
                return NetMQConfig.ThreadPoolSize;
            }
            set {
                NetMQConfig.ThreadPoolSize = value;
            }
        }

        /// <summary>
        /// Get or set the maximum number of sockets.
        /// </summary>
        public static int MaxSockets {
            get {
                return NetMQConfig.MaxSockets;
            }
            set {
                NetMQConfig.MaxSockets = value;
            }
        }

        private static object _syncRoot = new object();
        private static string _publicIpAddress = string.Empty;

        public static string PublicIpAddress 
        {
            get 
            {
                if (string.IsNullOrEmpty(_publicIpAddress))
                {
                    lock (_syncRoot)
                    {
                        if (string.IsNullOrEmpty(_publicIpAddress))
                        {
                            try
                            {
                                using (var client = new HttpClient())
                                {
                                    _publicIpAddress = client.GetAsync("http://checkip.amazonaws.com/")
                                        .Result
                                        .Content
                                        .ReadAsStringAsync()
                                        .Result;
                                }
                            }
                            catch
                            {
                                _publicIpAddress = "127.0.0.1";
                            }
                        }
                    }
                }
                return _publicIpAddress;
            }
        }
    }
}
