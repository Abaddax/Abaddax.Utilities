using System.Diagnostics.CodeAnalysis;

namespace Abaddax.Utilities.Threading
{
    public static class CancellationTokenExtension
    {

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ownership return")]
        public static CancellationTokenSource WithTimeout(this CancellationToken cancellationToken, TimeSpan timeout)
        {
            var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            return timeoutSource;
        }
    }
}
