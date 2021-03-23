using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FIOSharp
{
	public class RateLimiter : IDisposable
	{
		private readonly SemaphoreSlim semaphore;
		private readonly System.Timers.Timer cycleTimer;

		/// <summary>
		/// If this isntacnce is disposed
		/// </summary>
		public bool IsDisposed { get; private set; }
		private int countCompleted;

		/// <summary>
		/// The maximum number of tasks running concurrently
		/// </summary>
		public int MaxConcurrent { get; }

		/// <summary>
		/// The length of the rate limited time unit in milliseconds
		/// </summary>
		public int TimeUnitMs { get; }

		public RateLimiter(int maxConcurrent, int timeUnitMs)
		{
			if (maxConcurrent <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(maxConcurrent), "Maximum concurrent operations must be at least 1");
			}

			if (timeUnitMs <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(timeUnitMs), "Time unit must be at least 1");
			}

			MaxConcurrent = maxConcurrent;
			TimeUnitMs = timeUnitMs;
			cycleTimer = new System.Timers.Timer(TimeUnitMs);
			cycleTimer.Elapsed += onTimerElapsed;
			cycleTimer.Start();
			semaphore = new SemaphoreSlim(MaxConcurrent);

		}

		private void onTimerElapsed(object sender, ElapsedEventArgs e)
		{
			//semaphore doesn't like releasing "0", so we need to do this conditional. This *can* lead to harder than strictly require rate limits, but ah well.
			int amount = Interlocked.Exchange(ref countCompleted, 0);
			if (amount > 0) semaphore.Release(amount);

		}

		private bool tryEnter(int timeout)
		{
			if (timeout < -1) throw new ArgumentOutOfRangeException(nameof(timeout));
			return semaphore.Wait(timeout);
		}

		private async Task<bool> tryEnterAsync(int timeout)
		{
			if (timeout < -1) throw new ArgumentOutOfRangeException(nameof(timeout));
			return await semaphore.WaitAsync(timeout);
		}

		private void release()
		{
			Interlocked.Increment(ref countCompleted);
		}


		public void Run(Action run, int timeout = -1)
		{
			if (!tryEnter(timeout)) throw new TimeoutException(timeout);
			try
			{
				run();
			}
			finally
			{
				release();
			}
		}

		public T Run<T>(Func<T> run, int timeout = -1)
		{
			if (!tryEnter(timeout)) throw new TimeoutException(timeout);
			T result;
			try
			{
				result = run();
			}
			finally
			{
				release();
			}

			return result;
		}

		public async Task RunAsync(Func<Task> run, int timeout = -1)
		{
			if (!await tryEnterAsync(timeout)) throw new TimeoutException(timeout);
			try
			{
				await run();
			}
			finally
			{
				release();
			}
		}

		public async Task<T> RunAsync<T>(Func<Task<T>> run, int timeout = -1)
		{
			if (!await tryEnterAsync(timeout)) throw new TimeoutException(timeout);
			T result;
			try
			{
				result = await run();
			}
			finally
			{
				semaphore.Release();
			}
			return result;
		}

		public void Dispose()
		{
			if (!IsDisposed)
			{
				semaphore.Dispose();
				cycleTimer.Dispose();
				IsDisposed = true;
			}
		}
	}
}
