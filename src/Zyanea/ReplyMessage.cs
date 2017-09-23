using System;
using System.Collections.Generic;
using System.Text;

namespace Zyanea
{
	public class ReplyMessage
	{
		public ReplyMessage(RequestMessage msg)
		{
			RequestMessageId = msg.MessageId;
		}

		public Guid RequestMessageId { get; }

		public object Result { get; set; }

		public Exception Exception { get; set; }
	}
}
