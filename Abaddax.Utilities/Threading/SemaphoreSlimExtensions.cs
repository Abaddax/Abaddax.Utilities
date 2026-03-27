using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Threading
{
    public static class SemaphoreSlimExtensions
    {
        public static SemaphoreSlimLock Lock(this SemaphoreSlim semaphoreSlim, CancellationToken cancellationToken = default)
            => Lock(semaphoreSlim, Timeout.InfiniteTimeSpan, cancellationToken);
        public static SemaphoreSlimLock Lock(this SemaphoreSlim semaphoreSlim, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            semaphoreSlim.Wait(timeout, cancellationToken);
            return new SemaphoreSlimLock(semaphoreSlim);
        }

        public static Task<SemaphoreSlimLock> LockAsync(this SemaphoreSlim semaphoreSlim, CancellationToken cancellationToken = default)
            => LockAsync(semaphoreSlim, Timeout.InfiniteTimeSpan, cancellationToken);
        public static async Task<SemaphoreSlimLock> LockAsync(this SemaphoreSlim semaphoreSlim, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            await semaphoreSlim.WaitAsync(timeout, cancellationToken);
            return new SemaphoreSlimLock(semaphoreSlim);
        }


        public struct SemaphoreSlimLock : IDisposable
        {
            private SemaphoreSlim? _semaphoreSlim;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal SemaphoreSlimLock(SemaphoreSlim semaphoreLite)
            {
                _semaphoreSlim = semaphoreLite;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                SemaphoreSlim? semaphoreSlim = _semaphoreSlim;
                if (semaphoreSlim is not null)
                {
                    _semaphoreSlim = null;
                    semaphoreSlim.Release();
                }
            }
        }

    }
}
