using Abaddax.Utilities.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Collections.Concurrent
{
    public abstract class ConcurrentCollectionBase
    {
        private static readonly TimeSpan _Timeout = TimeSpan.FromSeconds(30);
        private readonly ReaderWriterSemaphoreLite _semaphore = new();

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void WithReadLock(Action action)
        {
            using (_semaphore.ReaderLock(_Timeout))
            {
                action.Invoke();
            }
        }
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T WithReadLock<T>(Func<T> action)
        {
            using (_semaphore.ReaderLock(_Timeout))
            {
                return action.Invoke();
            }
        }
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void WithWriteLock(Action action)
        {
            using (_semaphore.WriterLock(_Timeout))
            {
                action.Invoke();
            }
        }
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T WithWriteLock<T>(Func<T> action)
        {
            using (_semaphore.WriterLock(_Timeout))
            {
                return action.Invoke();
            }
        }

        protected IEnumerable<T> WithReadLock<T>(IEnumerable<T> enumerable)
        {
            using (_semaphore.ReaderLock(_Timeout))
            {
                foreach (var item in enumerable)
                {
                    yield return item;
                }
            }
        }
    }
}
