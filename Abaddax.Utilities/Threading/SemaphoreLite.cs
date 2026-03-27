using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Threading
{
    public class SemaphoreLite
    {
        //Use old lock for Monitor.Wait/Monitor.Pulse
        private readonly object _lock = new();
        private readonly Queue<TaskCompletionSource> _asyncWaiters = new();
        private int _syncWaiters = 0;
        private readonly int _maxCount;
        private volatile int _counter;
        private byte _resetCounter = 0;

        public int CurrentCount => _counter;
        public int SpinCount { get; } = 35;

        public SemaphoreLite(int initialCount)
            : this(initialCount, int.MaxValue) { }
        public SemaphoreLite(int initialCount, int maxCount)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(initialCount, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxCount, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCount, maxCount);

            _counter = initialCount;
            _maxCount = maxCount;
        }

        private bool TryLock()
        {
            while (true)
            {
                var counter = _counter;
                //Semaphore full
                if (counter == 0)
                    return false;
                //Try decrement
                if (Interlocked.CompareExchange(ref _counter, counter - 1, counter) == counter)
                    return true;
            }
        }
        private bool TryUnlock()
        {
            while (true)
            {
                var counter = _counter;
                //Maxcount reached
                if (counter >= _maxCount)
                    return false;
                //Try increment
                if (Interlocked.CompareExchange(ref _counter, counter + 1, counter) == counter)
                    return true;
            }
        }

        public void Wait(CancellationToken cancellationToken = default)
            => Wait(Timeout.InfiniteTimeSpan, cancellationToken);
        public void Wait(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than 0 or 'Timeout.InfiniteTimeSpan'");
            if (TryLock())
            {
                return;
            }
            Thread.SpinWait(SpinCount);
            //Check again
            if (TryLock())
            {
                return;
            }
            lock (_lock)
            {
                var start = Environment.TickCount;
                //Check need to happen inside the lock and before actually waiting, to avoid invalid states
                while (!TryLock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    const int maxWaitTime = 10000;
                    int waitTime = maxWaitTime;
                    if (timeout != Timeout.InfiniteTimeSpan)
                    {
                        int elapsed = Environment.TickCount - start;
                        waitTime = Math.Clamp((int)timeout.TotalMilliseconds - elapsed, 0, maxWaitTime);
                        if (waitTime == 0)
                            throw new TimeoutException();
                    }
                    Interlocked.Increment(ref _syncWaiters);
                    try
                    {
                        Monitor.Wait(_lock, waitTime);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _syncWaiters);
                    }
                }
                return;
            }
        }

        public Task WaitAsync(CancellationToken cancellationToken = default)
            => WaitAsync(Timeout.InfiniteTimeSpan, cancellationToken);
        public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than 0 or 'Timeout.InfiniteTimeSpan'");
            if (TryLock())
            {
                return;
            }
            Thread.SpinWait(SpinCount);
            //Check again
            if (TryLock())
            {
                return;
            }
            {
                var start = Environment.TickCount;
                cancellationToken.ThrowIfCancellationRequested();

                TaskCompletionSource tcs;
                lock (_lock)
                {
                    tcs = new TaskCompletionSource();
                    _asyncWaiters.Enqueue(tcs);
                }
                try
                {
                    //Check need to happen after the completion got added and before actually waiting, to avoid invalid states
                    while (!TryLock())
                    {
                        const int maxWaitTime = 10000;
                        int waitTime = maxWaitTime;
                        if (timeout != Timeout.InfiniteTimeSpan)
                        {
                            int elapsed = Environment.TickCount - start;
                            waitTime = Math.Clamp((int)timeout.TotalMilliseconds - elapsed, 0, maxWaitTime);
                            if (waitTime == 0)
                                throw new TimeoutException();
                        }
                        try
                        {
                            await tcs.Task.WaitAsync(timeout, cancellationToken);
                        }
                        catch (TimeoutException)
                        {
                            continue;
                        }
                    }
                    return;
                }
                finally
                {
                    tcs.TrySetCanceled(new CancellationToken(true));
                }
            }
        }

        public void Release()
        {
            if (!TryUnlock())
                throw new SemaphoreFullException($"Maximum count of {_maxCount} reached");

            //Alternate for fairness between sync/async waiters
            lock (_lock)
            {
                if (_resetCounter++ % 2 == 0)
                {
                    if (NotifySyncWaiter())
                        return;
                    NotifyAsyncWaiter();
                    return;
                }
                else
                {
                    if (NotifyAsyncWaiter())
                        return;
                    NotifySyncWaiter();
                    return;
                }
            }

            bool NotifySyncWaiter()
            {
                //Notify sync waiter
                if (Interlocked.CompareExchange(ref _syncWaiters, 0, 0) != 0)
                {
                    Monitor.Pulse(_lock);
                    return true;
                }
                return false;
            }
            bool NotifyAsyncWaiter()
            {
                //Notify async waiter
                while (_asyncWaiters.TryDequeue(out var asyncWaiter))
                {
                    if (asyncWaiter.TrySetResult())
                        return true;
                }
                return false;
            }
        }


        public SemaphoreLiteLock Lock(CancellationToken cancellationToken = default)
            => Lock(Timeout.InfiniteTimeSpan, cancellationToken);
        public SemaphoreLiteLock Lock(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            Wait(timeout, cancellationToken);
            return new SemaphoreLiteLock(this);
        }
        public Task<SemaphoreLiteLock> LockAsync(CancellationToken cancellationToken = default)
            => LockAsync(Timeout.InfiniteTimeSpan, cancellationToken);
        public async Task<SemaphoreLiteLock> LockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            await WaitAsync(timeout, cancellationToken);
            return new SemaphoreLiteLock(this);
        }

        public struct SemaphoreLiteLock : IDisposable
        {
            private SemaphoreLite? _semaphoreLite;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal SemaphoreLiteLock(SemaphoreLite semaphoreLite)
            {
                _semaphoreLite = semaphoreLite;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                SemaphoreLite? semaphoreLite = _semaphoreLite;
                if (semaphoreLite is not null)
                {
                    _semaphoreLite = null;
                    semaphoreLite.Release();
                }
            }
        }
    }
}
