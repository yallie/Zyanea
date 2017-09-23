using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Zyanea.Tests.Async
{
	public interface ISampleAsyncService
	{
		Task PerformShortOperation();

		Task PerformLongOperation();

		Task ThrowException();

		Task<int> Add(int a, int b);

		Task<string> Concatenate(params string[] strings);

		Task<DateTime> Construct(int y, int m, int d);

		Task<DateTimeOffset> Now { get; }
	}
}
