namespace Abaddax.Utilities.Threading
{
    public static class CancellationTokenExtension
    {
        public static CancellationTokenSource WithTimeout(this CancellationToken cancellationToken, TimeSpan timeout)
        {
#pragma warning disable CA2000 //Ownership transfer
            var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                timeoutSource.CancelAfter(timeout);
                return timeoutSource;
            }
            catch (Exception)
            {
                timeoutSource.Dispose();
                throw;
            }
#pragma warning restore CA2000
        }
    }
}
