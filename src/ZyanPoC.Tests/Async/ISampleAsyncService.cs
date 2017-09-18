using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZyanPoC.Tests.Async
{
	public interface ISampleAsyncService
	{
		Task PerformShortOperation();

		Task PerformLongOperation();
	}
}
