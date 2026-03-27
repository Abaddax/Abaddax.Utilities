using Abaddax.Utilities.Threading;
using Abaddax.Utilities.Threading.Tasks;

namespace Abaddax.Utilities.Tests.Threading
{
    public class ReaderWriterSemaphoreSlimTests
    {
        private readonly ReaderWriterSemaphoreSlim _readerWriterSemaphore = new();

        [Test]
        public void ShouldAllowMultipleReaders()
        {
            for(int i = 0; i<10; i++)
            {
                _readerWriterSemaphore.WaitRead();
            }
            for (int i = 0; i < 10; i++)
            {
                _readerWriterSemaphore.ReleaseRead();
            }
            Assert.Pass();
        }
        [Test]
        public void ShouldNotAllowMultipleWriters()
        {
            _readerWriterSemaphore.WaitWrite();
            Assert.Throws<TimeoutException>(() =>
            {
                _readerWriterSemaphore.WaitWrite(TimeSpan.FromMilliseconds(1000));
            });
            _readerWriterSemaphore.ReleaseWrite();

            //Check again, just in case the 2nd wait altered something it shouldnt
            _readerWriterSemaphore.WaitWrite();
            _readerWriterSemaphore.ReleaseWrite();

            Assert.Pass();
        }
        [Test]
        public void ShouldNotAllowWriteWhenStillReading()
        {
            _readerWriterSemaphore.WaitRead();
            Assert.Throws<TimeoutException>(() =>
            {
                _readerWriterSemaphore.WaitWrite(TimeSpan.FromMilliseconds(1000));
            });
            _readerWriterSemaphore.ReleaseRead();

            //Check again, just in case the 2nd wait altered something it shouldnt
            _readerWriterSemaphore.WaitRead();
            _readerWriterSemaphore.ReleaseRead();

            _readerWriterSemaphore.WaitWrite();
            _readerWriterSemaphore.ReleaseWrite();

            Assert.Pass();
        }
        [Test]
        public void ShouldNotAllowReadWhenStillWriting()
        {
            _readerWriterSemaphore.WaitWrite();
            Assert.Throws<TimeoutException>(() =>
            {
                _readerWriterSemaphore.WaitRead(TimeSpan.FromMilliseconds(1000));
            });
            _readerWriterSemaphore.ReleaseWrite();

            //Check again, just in case the 2nd wait altered something it shouldnt
            _readerWriterSemaphore.WaitWrite();
            _readerWriterSemaphore.ReleaseWrite();

            _readerWriterSemaphore.WaitRead();
            _readerWriterSemaphore.ReleaseRead();

            Assert.Pass();
        }
        [Test]
        public void ShouldNotAllowNewReaderWhenWriterPending()
        {
            _readerWriterSemaphore.WaitRead();
            //Write
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var writerTask = _readerWriterSemaphore.WaitWriteAsync(cancellationTokenSource.Token);
                Assert.Throws<TimeoutException>(() =>
                {
                    _readerWriterSemaphore.WaitRead(TimeSpan.FromMilliseconds(1000));
                });
                cancellationTokenSource.Cancel();
                Assert.Throws<TaskCanceledException>(() =>
                {
                    writerTask.AwaitSync();
                });
            }
            _readerWriterSemaphore.ReleaseRead();

            //Check again, just in case the 2nd wait altered something it shouldnt
            _readerWriterSemaphore.WaitWrite();
            _readerWriterSemaphore.ReleaseWrite();

            _readerWriterSemaphore.WaitRead();
            _readerWriterSemaphore.ReleaseRead();

            Assert.Pass();
        }

    }
}
