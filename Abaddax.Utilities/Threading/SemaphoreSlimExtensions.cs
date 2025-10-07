namespace Abaddax.Utilities.Threading
{
    public static class SemaphoreSlimExtensions
    {
        public static IDisposable Lock(this SemaphoreSlim semaphore, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            semaphore.Wait(timeout, cancellationToken);
            return new DelegateDisposable(() =>
            {
                semaphore.Release();
            });
        }
        public static IDisposable Lock(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
        {
            return Lock(semaphore, TimeSpan.FromMilliseconds(-1), cancellationToken);
        }

        public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphore, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(timeout, cancellationToken);
            return new DelegateDisposable(() =>
            {
                semaphore.Release();
            });
        }
        public static Task<IDisposable> LockAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
        {
            return LockAsync(semaphore, TimeSpan.FromMilliseconds(-1), cancellationToken);
        }

    }
}
