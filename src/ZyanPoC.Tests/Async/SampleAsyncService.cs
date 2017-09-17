using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZyanPoC.Tests.Async
{
	internal class SampleAsyncService : ISampleAsyncService
	{
		public bool ShortOperationPerformed { get; private set; }

		public Task PerformShortOperation()
		{
			ShortOperationPerformed = true;
			return Task.CompletedTask;
		}
	}
}
