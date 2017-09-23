using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zyanea.Tests.Async
{
	internal class SampleAsyncService : ISampleAsyncService
	{
		public bool ShortOperationPerformed { get; private set; }

		public Task PerformShortOperation()
		{
			ShortOperationPerformed = true;
			return Task.CompletedTask;
		}

		public bool LongOperationPerformed { get; private set; }

		public async Task PerformLongOperation()
		{
			await Task.Delay(300);
			LongOperationPerformed = true;
		}

		public async Task ThrowException()
		{
			await Task.Delay(1);
			throw new NotImplementedException(nameof(ISampleAsyncService));
		}

		public async Task<int> Add(int a, int b)
		{
			await Task.Delay(1);
			return a + b;
		}

		public async Task<string> Concatenate(params string[] strings)
		{
			await Task.Delay(1);
			return string.Concat(strings);
		}

		public async Task<DateTime> Construct(int y, int m, int d)
		{
			await Task.Delay(1);
			return new DateTime(y, m, d);
		}

		public Task<DateTimeOffset> Now => GetNow();

		private async Task<DateTimeOffset> GetNow()
		{
			await Task.Delay(1);
			return DateTimeOffset.Now;
		}
	}
}
