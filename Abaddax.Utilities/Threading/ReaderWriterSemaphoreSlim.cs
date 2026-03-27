using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Threading
{
    public class ReaderWriterSemaphoreSlim : IDisposable
    {
        private readonly SemaphoreSlim _writerSemaphore = new(1);
        private readonly ManualResetEventSlim _allowReaders = new(true);
        private readonly ManualResetEventSlim _noReaders = new(true);
        private volatile int _readers;
        private bool _disposedValue;

        public int CurrentReadCount => _readers;
        public bool IsReadLockHeld => CurrentReadCount > 0;
        public bool IsWriteLockHeld => _writerSemaphore.CurrentCount == 0;

        public void WaitRead(CancellationToken cancellationToken = default)
           => WaitRead(Timeout.InfiniteTimeSpan, cancellationToken);
        public void WaitRead(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            if (!_allowReaders.Wait(timeout, cancellationToken))
                throw new TimeoutException();
            _noReaders.Reset();
            Interlocked.Increment(ref _readers);
        }
        public Task WaitReadAsync(CancellationToken cancellationToken = default)
            => WaitReadAsync(Timeout.InfiniteTimeSpan, cancellationToken);
        public async Task WaitReadAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            if (!_allowReaders.IsSet)
                await _allowReaders.WaitHandle.WaitAsync(timeout, cancellationToken);
            _noReaders.Reset();
            Interlocked.Increment(ref _readers);
        }
        public void ReleaseRead()
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            if (Interlocked.Decrement(ref _readers) == 0)
                _noReaders.Set();
        }

        public void WaitWrite(CancellationToken cancellationToken = default)
            => WaitWrite(Timeout.InfiniteTimeSpan, cancellationToken);
        public void WaitWrite(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            if (!_writerSemaphore.Wait(timeout, cancellationToken))
                throw new TimeoutException();
            try
            {
                _allowReaders.Reset();
                if (!_noReaders.Wait(timeout, cancellationToken))
                    throw new TimeoutException();
            }
            catch (Exception)
            {
                //"Lock" was taken, but failed (timeout) -> Reset state
                _allowReaders.Set();
                _writerSemaphore.Release();
                throw;
            }
        }
        public Task WaitWriteAsync(CancellationToken cancellationToken = default)
            => WaitWriteAsync(Timeout.InfiniteTimeSpan, cancellationToken);
        public async Task WaitWriteAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            if (!await _writerSemaphore.WaitAsync(timeout, cancellationToken))
                throw new TimeoutException();
            try
            {
                _allowReaders.Reset();
                if (!_noReaders.IsSet)
                    await _noReaders.WaitHandle.WaitAsync(timeout, cancellationToken);
            }
            catch (Exception)
            {
                //"Lock" was taken, but failed (timeout) -> Reset state
                _allowReaders.Set();
                _writerSemaphore.Release();
                throw;
            }
        }
        public void ReleaseWrite()
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            _allowReaders.Set();
            _writerSemaphore.Release();
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _allowReaders.Dispose();
                    _noReaders.Dispose();
                    _writerSemaphore.Dispose();
                }
                _disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        public ReaderWriterSemaphoreSlimReaderLock ReaderLock(CancellationToken cancellationToken = default)
           => ReaderLock(Timeout.InfiniteTimeSpan, cancellationToken);
        public ReaderWriterSemaphoreSlimReaderLock ReaderLock(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            WaitRead(timeout, cancellationToken);
            return new ReaderWriterSemaphoreSlimReaderLock(this);
        }

        public Task<ReaderWriterSemaphoreSlimReaderLock> ReaderLockAsync(CancellationToken cancellationToken = default)
            => ReaderLockAsync(Timeout.InfiniteTimeSpan, cancellationToken);
        public async Task<ReaderWriterSemaphoreSlimReaderLock> ReaderLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            await WaitReadAsync(timeout, cancellationToken);
            return new ReaderWriterSemaphoreSlimReaderLock(this);
        }

        public ReaderWriterSemaphoreSlimWriterLock WriterLock(CancellationToken cancellationToken = default)
          => WriterLock(Timeout.InfiniteTimeSpan, cancellationToken);
        public ReaderWriterSemaphoreSlimWriterLock WriterLock(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            WaitWrite(timeout, cancellationToken);
            return new ReaderWriterSemaphoreSlimWriterLock(this);
        }

        public Task<ReaderWriterSemaphoreSlimWriterLock> WriterLockAsync(CancellationToken cancellationToken = default)
            => WriterLockAsync(Timeout.InfiniteTimeSpan, cancellationToken);
        public async Task<ReaderWriterSemaphoreSlimWriterLock> WriterLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            await WaitWriteAsync(timeout, cancellationToken);
            return new ReaderWriterSemaphoreSlimWriterLock(this);
        }


        public struct ReaderWriterSemaphoreSlimReaderLock : IDisposable
        {
            private ReaderWriterSemaphoreSlim? _readerWriterSemaphoreSlim;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReaderWriterSemaphoreSlimReaderLock(ReaderWriterSemaphoreSlim readerWriterSemaphoreSlim)
            {
                _readerWriterSemaphoreSlim = readerWriterSemaphoreSlim;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                ReaderWriterSemaphoreSlim? readerWriterSemaphoreSlim = _readerWriterSemaphoreSlim;
                if (readerWriterSemaphoreSlim is not null)
                {
                    _readerWriterSemaphoreSlim = null;
                    readerWriterSemaphoreSlim.ReleaseRead();
                }
            }
        }
        public struct ReaderWriterSemaphoreSlimWriterLock : IDisposable
        {
            private ReaderWriterSemaphoreSlim? _readerWriterSemaphoreSlim;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReaderWriterSemaphoreSlimWriterLock(ReaderWriterSemaphoreSlim readerWriterSemaphoreSlim)
            {
                _readerWriterSemaphoreSlim = readerWriterSemaphoreSlim;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                ReaderWriterSemaphoreSlim? readerWriterSemaphoreSlim = _readerWriterSemaphoreSlim;
                if (readerWriterSemaphoreSlim is not null)
                {
                    _readerWriterSemaphoreSlim = null;
                    readerWriterSemaphoreSlim.ReleaseWrite();
                }
            }
        }

    }
}
