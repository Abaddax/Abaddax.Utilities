using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
