using System;

namespace FIOSharp
{
	public class TimeoutException : Exception
	{
		public int TimeoutDuration { get; }

		public TimeoutException(int timeoutDuration, string message = null) : base(message)
		{
			TimeoutDuration = timeoutDuration;
		}

	}
}
