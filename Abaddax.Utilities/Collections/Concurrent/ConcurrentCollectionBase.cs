using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Collections.Concurrent
{
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Collections should not implement IDisposable")]
    public abstract class ConcurrentCollectionBase
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void WithReadLock(Action action)
        {
            _lock.EnterReadLock();
            try
            {
                action.Invoke();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T WithReadLock<T>(Func<T> action)
        {
            _lock.EnterReadLock();
            try
            {
                return action.Invoke();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void WithWriteLock(Action action)
        {
            _lock.EnterWriteLock();
            try
            {
                action.Invoke();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected T WithWriteLock<T>(Func<T> action)
        {
            _lock.EnterWriteLock();
            try
            {
                return action.Invoke();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

    }
}
