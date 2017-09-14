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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageWire
{
	public static class IHostExtensions
	{
		public static void Send(this IHost self, Guid clientId, IEnumerable<byte[]> frames)
		{
			self.Send(clientId, frames.ToList());
		}

		public static void Send(this IHost self, Guid clientId, byte[] frame)
		{
			self.Send(clientId, new List<byte[]> { frame });
		}

		public static void Send(this IHost self, Guid clientId, List<string> frames)
		{
			self.Send(clientId, frames.AsEnumerable(), Encoding.UTF8);
		}

		public static void Send(this IHost self, Guid clientId, IEnumerable<string> frames)
		{
			self.Send(clientId, frames, Encoding.UTF8);
		}

		public static void Send(this IHost self, Guid clientId, params string[] frames)
		{
			self.Send(clientId, frames.AsEnumerable(), Encoding.UTF8);
		}

		public static void Send(this IHost self, Guid clientId, string frame)
		{
			self.Send(clientId, Encoding.UTF8, frame);
		}

		public static void Send(this IHost self, Guid clientId, List<string> frames, Encoding encoding)
		{
			self.Send(clientId, frames.AsEnumerable(), encoding);
		}

		public static void Send(this IHost self, Guid clientId, IEnumerable<string> frames, Encoding encoding)
		{
			self.Send(clientId, frames.Select(n => n == null ? null : encoding.GetBytes(n)).ToList());
		}

		public static void Send(this IHost self, Guid clientId, Encoding encoding, params string[] frames)
		{
			self.Send(clientId, frames.AsEnumerable(), encoding);
		}

		public static void Send(this IHost self, Guid clientId, string frame, Encoding encoding)
		{
			self.Send(clientId, encoding, frame);
		}
	}
}
