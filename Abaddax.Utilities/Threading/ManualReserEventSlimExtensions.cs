namespace Abaddax.Utilities.Threading
{
    public static class ManualReserEventSlimExtensions
    {
        public static Task WaitAsync(this ManualResetEventSlim manualResetEventSlim, CancellationToken cancellationToken = default)
          => WaitAsync(manualResetEventSlim, Timeout.InfiniteTimeSpan, cancellationToken);
        public static async Task WaitAsync(this ManualResetEventSlim manualResetEventSlim, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (manualResetEventSlim.IsSet)
                return;
            await manualResetEventSlim.WaitHandle.WaitAsync(timeout, cancellationToken);
        }
    }
}
