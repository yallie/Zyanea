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

using MessageWire.Logging;
using System;
using System.Security.Cryptography;

namespace MessageWire.SecureRemote
{
    /// <summary>
    /// Easy to use encapsulation of Rijndael encryption.
    /// </summary>
    internal class Crypto
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly ILog _logger;

        public Crypto(byte[] key, byte[] iv, ILog logger)
        {
            if (key.Length != 32) throw new ArgumentException("key must be 256 bits", "key");
            if (iv.Length != 16) throw new ArgumentException("iv must be 128 bits", "iv");
            _key = key;
            _iv = iv;
            _logger = logger ?? new NullLogger();
        }

        public byte[] Encrypt(byte[] data)
        {
            try
            {
                using (var crypto = System.Security.Cryptography.Aes.Create())
                {
                    crypto.Mode = CipherMode.CBC;
                    crypto.BlockSize = 128; // 256;
                    crypto.KeySize = 256;
                    crypto.Padding = PaddingMode.PKCS7;
                    using (var encryptor = crypto.CreateEncryptor(_key, _iv))
                    {
                        return encryptor.TransformFinalBlock(data, 0, data.Length);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error("Encryption error {0}", e);
                return data;
            }
        }

        public byte[] Decrypt(byte[] encrypted)
        {
            try
            {
                using (var crypto = Aes.Create())
                {
                    crypto.Mode = CipherMode.CBC;
                    crypto.BlockSize = 128; // 256;
                    crypto.KeySize = 256;
                    crypto.Padding = PaddingMode.PKCS7;
                    using (var dencryptor = crypto.CreateDecryptor(_key, _iv))
                    {
                        return dencryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error("Decryption error {0}", e);
                return encrypted;
            }
        }

    }
}