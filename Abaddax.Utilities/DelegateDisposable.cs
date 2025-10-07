using Abaddax.Utilities.Threading.Tasks;

namespace Abaddax.Utilities
{
    public sealed class DelegateDisposable : IDisposable
    {
        private readonly Action _disposeAction;
        private bool disposedValue;

        public DelegateDisposable(Action disposeAction)
        {
            _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
        }

        #region IDisposable
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _disposeAction.Invoke();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    public sealed class AsyncDelegateDisposable : IAsyncDisposable, IDisposable
    {
        private readonly Func<Task> _disposeAction;
        private bool disposedValue;

        public AsyncDelegateDisposable(Func<Task> disposeAction)
        {
            _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
        }

        #region IAsyncDisposable
        private async Task DisposeAsync(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    await _disposeAction.Invoke();
                }
                disposedValue = true;
            }
        }
        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            DisposeAsync(disposing: true).AwaitSync();
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
