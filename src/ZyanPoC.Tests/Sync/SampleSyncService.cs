﻿using System;

namespace ZyanPoC.Tests.Sync
{
	internal class SampleSyncService : ISampleSyncService
	{
		public DateTime GetDate(int year, int month, int day)
		{
			return new DateTime(year, month, day);
		}

		public string GetVersion()
		{
			return "0.0.1 alpha";
		}

		public void Void()
		{
			// do nothing
		}

		public void ThrowException()
		{
			throw new NotImplementedException();
		}
	}
}