using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Threading
{
    public class ReaderWriterSemaphoreLite
    {
        private readonly SemaphoreLite _writerSemaphore = new(1);
        private readonly ManualResetEventLite _allowReaders = new(true);
        private readonly ManualResetEventLite _noReaders = new(true);
        private volatile int _readers;

        public int CurrentReadCount => _readers;
        public bool IsReadLockHeld => CurrentReadCount > 0;
        public bool IsWriteLockHeld => _writerSemaphore.CurrentCount == 0;

        public void WaitRead(CancellationToken cancellationToken = default)
            => WaitRead(Timeout.InfiniteTimeSpan, cancellationToken);
        public void WaitRead(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            _allowReaders.Wait(timeout, cancellationToken);
            _noReaders.Reset();
            Interlocked.Increment(ref _readers);
        }
        public Task WaitReadAsync(CancellationToken cancellationToken = default)
            => WaitReadAsync(Timeout.InfiniteTimeSpan, cancellationToken);
        public async Task WaitReadAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (!_allowReaders.IsSet)
                await _allowReaders.WaitAsync(timeout, cancellationToken);
            _noReaders.Reset();
            Interlocked.Increment(ref _readers);
        }
        public void ReleaseRead()
        {
            if (Interlocked.Decrement(ref _readers) == 0)
                _noReaders.Set();
        }

        public void WaitWrite(CancellationToken cancellationToken = default)
            => WaitWrite(Timeout.InfiniteTimeSpan, cancellationToken);
        public void WaitWrite(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            _writerSemaphore.Wait(timeout, cancellationToken);
            try
            {
                _allowReaders.Reset();
                _noReaders.Wait(timeout, cancellationToken);
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
            await _writerSemaphore.WaitAsync(timeout, cancellationToken);
            try
            {
                _allowReaders.Reset();
                await _noReaders.WaitAsync(timeout, cancellationToken);
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
            _allowReaders.Set();
            _writerSemaphore.Release();
        }

        public ReaderWriterSemaphoreLiteReaderLock ReaderLock(CancellationToken cancellationToken = default)
           => ReaderLock(Timeout.InfiniteTimeSpan, cancellationToken);
        public ReaderWriterSemaphoreLiteReaderLock ReaderLock(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            WaitRead(timeout, cancellationToken);
            return new ReaderWriterSemaphoreLiteReaderLock(this);
        }

        public Task<ReaderWriterSemaphoreLiteReaderLock> ReaderLockAsync(CancellationToken cancellationToken = default)
            => ReaderLockAsync(Timeout.InfiniteTimeSpan, cancellationToken);
        public async Task<ReaderWriterSemaphoreLiteReaderLock> ReaderLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            await WaitReadAsync(timeout, cancellationToken);
            return new ReaderWriterSemaphoreLiteReaderLock(this);
        }

        public ReaderWriterSemaphoreLiteWriterLock WriterLock(CancellationToken cancellationToken = default)
          => WriterLock(Timeout.InfiniteTimeSpan, cancellationToken);
        public ReaderWriterSemaphoreLiteWriterLock WriterLock(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            WaitWrite(timeout, cancellationToken);
            return new ReaderWriterSemaphoreLiteWriterLock(this);
        }

        public Task<ReaderWriterSemaphoreLiteWriterLock> WriterLockAsync(CancellationToken cancellationToken = default)
            => WriterLockAsync(Timeout.InfiniteTimeSpan, cancellationToken);
        public async Task<ReaderWriterSemaphoreLiteWriterLock> WriterLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            await WaitWriteAsync(timeout, cancellationToken);
            return new ReaderWriterSemaphoreLiteWriterLock(this);
        }


        public struct ReaderWriterSemaphoreLiteReaderLock : IDisposable
        {
            private ReaderWriterSemaphoreLite? _readerWriterSemaphoreLite;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReaderWriterSemaphoreLiteReaderLock(ReaderWriterSemaphoreLite readerWriterSemaphoreLite)
            {
                _readerWriterSemaphoreLite = readerWriterSemaphoreLite;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                ReaderWriterSemaphoreLite? readerWriterSemaphoreLite = _readerWriterSemaphoreLite;
                if (readerWriterSemaphoreLite is not null)
                {
                    _readerWriterSemaphoreLite = null;
                    readerWriterSemaphoreLite.ReleaseRead();
                }
            }
        }
        public struct ReaderWriterSemaphoreLiteWriterLock : IDisposable
        {
            private ReaderWriterSemaphoreLite? _readerWriterSemaphoreLite;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReaderWriterSemaphoreLiteWriterLock(ReaderWriterSemaphoreLite readerWriterSemaphoreLite)
            {
                _readerWriterSemaphoreLite = readerWriterSemaphoreLite;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                ReaderWriterSemaphoreLite? readerWriterSemaphoreLite = _readerWriterSemaphoreLite;
                if (readerWriterSemaphoreLite is not null)
                {
                    _readerWriterSemaphoreLite = null;
                    readerWriterSemaphoreLite.ReleaseWrite();
                }
            }
        }

    }
}
