namespace Abaddax.Utilities.Threading
{
    public static class WaitHandleExtensions
    {
        public static Task WaitAsync(this WaitHandle waitHandle, CancellationToken cancellationToken = default)
           => WaitAsync(waitHandle, Timeout.InfiniteTimeSpan, cancellationToken);
        public static async Task WaitAsync(this WaitHandle waitHandle, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource();
            var handle = ThreadPool.RegisterWaitForSingleObject(
                waitObject: waitHandle,
                callBack: (o, timeout) =>
                {
                    if (timeout)
                        tcs.TrySetException(new TimeoutException());
                    tcs.TrySetResult();
                },
                state: null,
                timeout: timeout,
                executeOnlyOnce: true);

            using var cancellationRegistration = cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled(cancellationToken);
            });
            try
            {
                await tcs.Task;
                return;
            }
            finally
            {
                handle.Unregister(null);
            }
        }
    }
}
