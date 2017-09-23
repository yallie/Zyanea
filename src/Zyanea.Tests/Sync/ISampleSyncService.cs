using System;
using System.Collections.Generic;
using System.Text;

namespace Zyanea.Tests.Sync
{
	public interface ISampleSyncService
	{
		void Void();

		string GetVersion();

		DateTime GetDate(int year, int month, int day);

		void ThrowException();

		string Platform { get; set; }
	}
}
