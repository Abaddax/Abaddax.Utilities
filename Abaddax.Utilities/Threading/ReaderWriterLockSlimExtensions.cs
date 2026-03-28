using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Threading
{
    public static class ReaderWriterLockSlimExtensions
    {
        public static ReaderWriterLockSlimReaderLock ReaderLock(this ReaderWriterLockSlim readerWriterLockSlim)
        {
            readerWriterLockSlim.EnterReadLock();
            return new ReaderWriterLockSlimReaderLock(readerWriterLockSlim);
        }
        public static ReaderWriterLockSlimWriterLock WriterLock(this ReaderWriterLockSlim readerWriterLockSlim)
        {
            readerWriterLockSlim.EnterWriteLock();
            return new ReaderWriterLockSlimWriterLock(readerWriterLockSlim);
        }

        public ref struct ReaderWriterLockSlimReaderLock : IDisposable
        {
            private ReaderWriterLockSlim? _readerWriterLockSlim;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReaderWriterLockSlimReaderLock(ReaderWriterLockSlim readerWriterLockSlim)
            {
                _readerWriterLockSlim = readerWriterLockSlim;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                ReaderWriterLockSlim? readerWriterLockSlim = _readerWriterLockSlim;
                if (readerWriterLockSlim is not null)
                {
                    _readerWriterLockSlim = null;
                    readerWriterLockSlim.ExitReadLock();
                }
            }
        }
        public ref struct ReaderWriterLockSlimWriterLock : IDisposable
        {
            private ReaderWriterLockSlim? _readerWriterLockSlim;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReaderWriterLockSlimWriterLock(ReaderWriterLockSlim readerWriterLockSlim)
            {
                _readerWriterLockSlim = readerWriterLockSlim;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                ReaderWriterLockSlim? readerWriterLockSlim = _readerWriterLockSlim;
                if (readerWriterLockSlim is not null)
                {
                    _readerWriterLockSlim = null;
                    readerWriterLockSlim.ExitWriteLock();
                }
            }
        }


        public static ReaderWriterLockSlimUpgradableReaderLock UpgradeableReaderLock(this ReaderWriterLockSlim readerWriterLockSlim)
        {
            readerWriterLockSlim.EnterUpgradeableReadLock();
            return new ReaderWriterLockSlimUpgradableReaderLock(readerWriterLockSlim);
        }
       
        public ref struct ReaderWriterLockSlimUpgradableReaderLock : IDisposable
        {
            private ReaderWriterLockSlim? _readerWriterLockSlim;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReaderWriterLockSlimUpgradableReaderLock(ReaderWriterLockSlim readerWriterLockSlim)
            {
                _readerWriterLockSlim = readerWriterLockSlim;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly ReaderWriterLockSlimWriterLock UpgradeToWriteLock()
            {
                ObjectDisposedException.ThrowIf(_readerWriterLockSlim == null, typeof(ReaderWriterLockSlimUpgradableReaderLock));
                return new ReaderWriterLockSlimWriterLock(_readerWriterLockSlim);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                ReaderWriterLockSlim? readerWriterLockSlim = _readerWriterLockSlim;
                if (readerWriterLockSlim is not null)
                {
                    _readerWriterLockSlim = null;
                    readerWriterLockSlim.ExitUpgradeableReadLock();
                }
            }
        }
    }
}
