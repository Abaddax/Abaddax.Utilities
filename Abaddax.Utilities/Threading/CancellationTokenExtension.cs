namespace Abaddax.Utilities.Threading
{
    public static class CancellationTokenExtension
    {
        public static CancellationTokenSource WithTimeout(this CancellationToken cancellationToken, TimeSpan timeout)
        {
            var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            return timeoutSource;
        }
    }
}
