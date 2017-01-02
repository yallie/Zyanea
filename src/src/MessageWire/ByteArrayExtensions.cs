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
using System.Security.Cryptography;
using System.Text;

namespace MessageWire
{
    public static class ByteArrayExtensions
    {
        public static byte[] ConvertToBytes(this string val)
        {
            return Encoding.UTF8.GetBytes(val);
        }

        public static string ConvertToString(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public static bool IsEqualTo(this byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length) return false;
            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i]) return false;
            }
            return true;
        }

        public static byte[] ToBytes(this RSAParameters p)
        {
            var parts = new string[]
            {
                null == p.D ? string.Empty : Convert.ToBase64String(p.D),
                null == p.DP ? string.Empty : Convert.ToBase64String(p.DP),
                null == p.DQ ? string.Empty : Convert.ToBase64String(p.DQ),
                null == p.Exponent ? string.Empty : Convert.ToBase64String(p.Exponent),
                null == p.InverseQ ? string.Empty : Convert.ToBase64String(p.InverseQ),
                null == p.Modulus ? string.Empty : Convert.ToBase64String(p.Modulus),
                null == p.P ? string.Empty : Convert.ToBase64String(p.P),
                null == p.Q ? string.Empty : Convert.ToBase64String(p.Q)
            };
            var data = Encoding.UTF8.GetBytes(string.Join(",", parts));
            return data;
        }

        public static RSAParameters ToRSAParameters(this byte[] data)
        {
            try
            {
                var paramString = Encoding.UTF8.GetString(data);
                var parts = paramString.Split(',');
                if (parts.Length != 8) return default(RSAParameters);
                var result = new RSAParameters();
                result.D = null != parts[0] ? Convert.FromBase64String(parts[0]) : null;
                result.DP = null != parts[1] ? Convert.FromBase64String(parts[1]) : null;
                result.DQ = null != parts[2] ? Convert.FromBase64String(parts[2]) : null;
                result.Exponent = null != parts[3] ? Convert.FromBase64String(parts[3]) : null;
                result.InverseQ = null != parts[4] ? Convert.FromBase64String(parts[4]) : null;
                result.Modulus = null != parts[5] ? Convert.FromBase64String(parts[5]) : null;
                result.P = null != parts[6] ? Convert.FromBase64String(parts[6]) : null;
                result.Q = null != parts[7] ? Convert.FromBase64String(parts[7]) : null;
                return result;
            }
            catch
            {
                return default(RSAParameters);
            }
        }
    }
}