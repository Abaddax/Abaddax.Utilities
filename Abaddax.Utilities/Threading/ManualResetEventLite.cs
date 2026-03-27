namespace Abaddax.Utilities.Threading
{
    public class ManualResetEventLite
    {
        //Use old lock for Monitor.Wait/Monitor.Pulse
        private readonly object _lock = new();
        private TaskCompletionSource? _asyncWait;
        private int _syncWaiters = 0;

        public bool IsSet { get; private set; }
        public int SpinCount { get; }

        public ManualResetEventLite()
            : this(false) { }
        public ManualResetEventLite(bool initialState)
            : this(initialState, 35) { }
        public ManualResetEventLite(bool initialState, int spinCount)
        {
            IsSet = initialState;
            SpinCount = Environment.ProcessorCount == 1 ? 0 : spinCount;
        }

        public void Wait(CancellationToken cancellationToken = default)
            => Wait(Timeout.InfiniteTimeSpan, cancellationToken);
        public void Wait(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than 0 or 'Timeout.InfiniteTimeSpan'");
            lock (_lock)
            {
                if (IsSet)
                    return;
            }
            Thread.SpinWait(SpinCount);
            lock (_lock)
            {
                //Check again
                if (IsSet)
                    return;
                var start = Environment.TickCount;
                do
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
                    _syncWaiters++;
                    try
                    {
                        Monitor.Wait(_lock, waitTime);
                    }
                    finally
                    {
                        _syncWaiters--;
                    }
                } while (!IsSet);
                return;
            }
        }

        public Task WaitAsync(CancellationToken cancellationToken = default)
            => WaitAsync(Timeout.InfiniteTimeSpan, cancellationToken);
        public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than 0 or 'Timeout.InfiniteTimeSpan'");
            lock (_lock)
            {
                if (IsSet)
                    return;
            }
            Thread.SpinWait(SpinCount);
            TaskCompletionSource tcs;
            lock (_lock)
            {
                //Check again
                if (IsSet)
                    return;
                if (_asyncWait != null)
                {
                    tcs = _asyncWait;
                }
                else
                {
                    _asyncWait = tcs = new TaskCompletionSource();
                }
            }
            await tcs.Task.WaitAsync(timeout, cancellationToken);
        }

        public void Set()
        {
            lock (_lock)
            {
                IsSet = true;
                _asyncWait?.TrySetResult();
                _asyncWait = null;
                if (_syncWaiters > 0)
                    Monitor.PulseAll(_lock);
            }
        }
        public void Reset()
        {
            lock (_lock)
            {
                IsSet = false;
            }
        }

    }
}
