using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FIOSharp
{
	/// <summary>
	/// An async friendly read/write lock that can still be used in a synchronous context
	/// </summary>
	public sealed class FlexibleReadWriteLock : IDisposable
	{
		private readonly SemaphoreSlim readSemaphore =  new SemaphoreSlim(1, 1);
		private readonly SemaphoreSlim writeSemaphore = new SemaphoreSlim(1, 1);
		private int readerCount = 0;



		public async Task RunInReadAsync(Func<Task> func)
		{
			await EnterReadLockAsync();
			try
			{
				await func();
			}
			finally
			{
				ExitReadLock();
			}
		}

		public async Task<T> RunInReadAsync<T>(Func<Task<T>> func)
		{

			await EnterReadLockAsync();
			try
			{
				return await func();
			}
			finally
			{
				ExitReadLock();
			}
		}

		public void RunInRead(Action func)
		{
			EnterReadLock();
			try
			{
				func();
			}
			finally
			{
				ExitReadLock();
			}
		}

		public T RunInRead<T>(Func<T> func)
		{
			EnterReadLock();
			try
			{
				return func();
			}
			finally
			{
				ExitReadLock();
			}
		}

		public async Task RunInWriteAsync(Func<Task> func)
		{
			await EnterWriteLockAsync();
			try
			{
				await func();
			}
			finally
			{
				ExitWriteLock();
			}
		}

		public async Task<T> RunInWriteAsync<T>(Func<Task<T>> func)
		{

			await EnterWriteLockAsync();
			try
			{
				return await func();
			}
			finally
			{
				ExitWriteLock();
			}
		}

		public void RunInWrite(Action func)
		{
			EnterWriteLock();
			try
			{
				func();
			}
			finally
			{
				ExitWriteLock();
			}
		}

		public T RunInWrite<T>(Func<T> func)
		{
			EnterWriteLock();
			try
			{
				return func();
			}
			finally
			{
				ExitWriteLock();
			}
		}


		private async Task EnterWriteLockAsync()
		{
			await writeSemaphore.WaitAsync().ConfigureAwait(false);
			await SafeWaitReadSempahoreAsync().ConfigureAwait(false);
		}

		private void EnterWriteLock()
		{
			writeSemaphore.Wait();
			SafeWaitReadSempahore();
		}

		private void ExitWriteLock()
		{
			readSemaphore.Release();
			writeSemaphore.Release();
		}


		private async Task EnterReadLockAsync()
		{
			await writeSemaphore.WaitAsync().ConfigureAwait(false);
			if (Interlocked.Increment(ref readerCount) == 1)
			{
				await SafeWaitReadSempahoreAsync().ConfigureAwait(false);
			}
			writeSemaphore.Release();
		}

		private void EnterReadLock()
		{
			writeSemaphore.Wait();
			if (Interlocked.Increment(ref readerCount) == 1)
			{
				SafeWaitReadSempahore();
			}
			writeSemaphore.Release();
		}

		private void ExitReadLock()
		{
			if (Interlocked.Decrement(ref readerCount) == 0)
			{
				readSemaphore.Release();
			}
		}

		private async Task SafeWaitReadSempahoreAsync()
		{
			try
			{
				await readSemaphore.WaitAsync().ConfigureAwait(false);
			}
			catch
			{
				writeSemaphore.Release();
				throw;
			}
		}

		private void SafeWaitReadSempahore()
		{
			try
			{
				readSemaphore.Wait();
			}
			catch
			{
				writeSemaphore.Release();
				throw;
			}
		}

		public void Dispose()
		{
			writeSemaphore.Dispose();
			readSemaphore.Dispose();
		}
	}
}
