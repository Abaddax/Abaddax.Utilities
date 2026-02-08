using Abaddax.Utilities.Threading.Tasks;

namespace Abaddax.Utilities.Threading
{
    public sealed class CancelableTask : IDisposable
    {
        private enum ThreadState
        {
            NotReady = -1,
            Ready = 0,
            Running,
            RunningDetached,
            Joining,
            Finished
        };

        private readonly Lock _lock = new();
        private readonly Func<CancellationToken, Task> _func;
        private ThreadState _state = ThreadState.NotReady;
        private CancellationTokenSource? _tokenSource = null;
        private Task? _task = null;
        private bool _disposedValue = false;

        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _state == ThreadState.Running ||
                        _state == ThreadState.RunningDetached ||
                        _state == ThreadState.Joining;
                }
            }
        }

        public CancelableTask(Func<CancellationToken, Task> action)
        {
            _func = action ?? throw new ArgumentNullException(nameof(action));
            _state = ThreadState.Ready;
        }

        public void Start()
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            lock (_lock)
            {
                if (_state != ThreadState.Ready &&
                    _state != ThreadState.Finished)
                {
                    return;
                }

                _tokenSource?.Cancel();
                _tokenSource?.Dispose();
                _tokenSource = new CancellationTokenSource();
                _task = Task.Run(async () =>
                {
                    try
                    {
                        await _func(_tokenSource.Token);
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _state = ThreadState.Finished;
                        }
                    }
                });
                _task.Start();
                _state = ThreadState.Running;
            }
        }
        public async Task JoinAsync()
        {
            lock (_lock)
            {
                if (_state == ThreadState.Finished)
                    goto JOIN_THREAD;
                if (_state != ThreadState.Running)
                    return;
                _state = ThreadState.Joining;
            }
        //Thread will aquire lock and change state
        JOIN_THREAD:
            await _task.CompletedIfNull();
        }
        public void RequestStop()
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            _tokenSource?.Cancel();
        }

        #region IDisposable
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                RequestStop();
                if (disposing)
                    _tokenSource?.Dispose();
                _disposedValue = true;
            }
        }
        ~CancelableTask()
        {
            Dispose(disposing: false);
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
