using System;

namespace ZyanPoC.Tests.Sync
{
	internal class SampleSyncService : ISampleSyncService
	{
		public const string Version = "0.0.1 alpha";

		public DateTime GetDate(int year, int month, int day)
		{
			return new DateTime(year, month, day);
		}

		public string GetVersion()
		{
			return Version;
		}

		public void Void()
		{
			VoidExecuted = true;
		}

		public bool VoidExecuted { get; private set; }

		public void ThrowException()
		{
			throw new NotImplementedException(nameof(ISampleSyncService));
		}

		public string Platform { get; set; }
	}
}