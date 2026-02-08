namespace Abaddax.Utilities.Threading
{
    public sealed class CancelableThread : IDisposable
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
        private readonly Action<CancellationToken> _func;
        private ThreadState _state = ThreadState.NotReady;
        private CancellationTokenSource? _tokenSource = null;
        private Thread? _thread = null;
        private Exception? _threadEx = null;
        private bool _disposedValue = false;

        public bool IsBackground { get; set; }
        public ThreadPriority Priority { get; set; }
        public string? Name { get; set; }
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

        public CancelableThread(Action<CancellationToken> action)
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
                _threadEx = null;
                _thread = new Thread(() =>
                {
                    try
                    {
                        _func(_tokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        _threadEx = ex;
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _state = ThreadState.Finished;
                        }
                    }
                })
                {
                    IsBackground = IsBackground,
                    Priority = Priority,
                    Name = Name
                };
                _thread.Start();
                _state = ThreadState.Running;
            }
        }
        public void Join()
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
            _thread?.Join();
            if (_threadEx != null)
                throw new Exception("Error in thread-execution", _threadEx);
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
        ~CancelableThread()
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
