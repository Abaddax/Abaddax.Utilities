using Abaddax.Utilities.Threading;

namespace Abaddax.Utilities.Tests.Threading
{
    public class SemaphoreLiteTests
    {
        [Test]
        [TestCase(1, 1)]
        [TestCase(10, 1)]
        [TestCase(100, 1)]
        [TestCase(100, 10)]
        [TestCase(1000, 1)]
        public void ShouldWaitReleaseSync(int concurrency, int maxCount)
        {
            var semaphore = new SemaphoreLite(maxCount, maxCount);

            using ManualResetEventSlim startEvent = new(false);
            const int counterFactor = 10000;
            int unlockedCounter = 0;
            int counter = 0;
            int threadSafeCounter = 0;

            var threads = Enumerable.Range(0, concurrency).Select(x =>
            {
                return new Thread(() =>
                {
                    startEvent.Wait();
                    for (int i = 0; i < counterFactor; i++)
                        unlockedCounter++;
                    semaphore.Wait();
                    try
                    {
                        //Do work
                        for (int i = 0; i < counterFactor; i++)
                            counter++;
                        for (int i = 0; i < counterFactor; i++)
                            Interlocked.Increment(ref threadSafeCounter);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            }).ToArray();
            foreach (var thread in threads)
            {
                thread.Start();
            }
            startEvent.Set();
            foreach (var thread in threads)
            {
                thread.Join();
            }

            Assert.That(semaphore.CurrentCount, Is.EqualTo(maxCount));

            Assert.That(threadSafeCounter, Is.EqualTo(concurrency * counterFactor));
            if (maxCount == 1)
                Assert.That(counter, Is.EqualTo(concurrency * counterFactor));
            else
                Warn.If(counter, Is.Not.LessThan(threadSafeCounter), "No concurrency errors occured in 'counter'");
            Warn.If(unlockedCounter, Is.Not.LessThan(threadSafeCounter), "No concurrency errors occured in 'unlockedCounter'");
        }
        [Test]
        [TestCase(1, 1)]
        [TestCase(10, 1)]
        [TestCase(100, 1)]
        [TestCase(100, 10)]
        [TestCase(1000, 1)]
        public async Task ShouldWaitReleaseAsync(int concurrency, int maxCount)
        {
            var semaphore = new Utilities.Threading.SemaphoreLite(maxCount, maxCount);

            using ManualResetEventSlim startEvent = new(false);
            const int counterFactor = 10000;
            int unlockedCounter = 0;
            int counter = 0;
            int threadSafeCounter = 0;

            var tasks = Enumerable.Range(0, concurrency).Select(x =>
            {
                return Task.Run(async () =>
                {
                    await startEvent.WaitHandle.WaitAsync();
                    for (int i = 0; i < counterFactor; i++)
                        unlockedCounter++;
                    await semaphore.WaitAsync();
                    try
                    {
                        //Do work
                        for (int i = 0; i < counterFactor; i++)
                            counter++;
                        for (int i = 0; i < counterFactor; i++)
                            Interlocked.Increment(ref threadSafeCounter);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            }).ToArray();
            startEvent.Set();
            foreach (var task in tasks)
            {
                await task;
            }

            Assert.That(semaphore.CurrentCount, Is.EqualTo(maxCount));

            Assert.That(threadSafeCounter, Is.EqualTo(concurrency * counterFactor));
            if (maxCount == 1)
                Assert.That(counter, Is.EqualTo(concurrency * counterFactor));
            else
                Warn.If(counter, Is.Not.LessThan(threadSafeCounter), "No concurrency errors occured in 'counter'");
            Warn.If(unlockedCounter, Is.Not.LessThan(threadSafeCounter), "No concurrency errors occured in 'unlockedCounter'");
        }
        [Test]
        [TestCase(1, 1)]
        [TestCase(10, 1)]
        [TestCase(100, 1)]
        [TestCase(100, 10)]
        [TestCase(1000, 1)]
        public async Task ShouldWaitReleaseSyncAndAsync(int concurrency, int maxCount)
        {
            var semaphore = new Utilities.Threading.SemaphoreLite(maxCount, maxCount);

            using ManualResetEventSlim startEvent = new(false);
            const int counterFactor = 10000;
            int unlockedCounter = 0;
            int counter = 0;
            int threadSafeCounter = 0;

            var threads = Enumerable.Range(0, concurrency).Select(x =>
            {
                return new Thread(() =>
                {
                    startEvent.Wait();
                    for (int i = 0; i < counterFactor; i++)
                        unlockedCounter++;
                    semaphore.Wait();
                    try
                    {
                        //Do work
                        for (int i = 0; i < counterFactor; i++)
                            counter++;
                        for (int i = 0; i < counterFactor; i++)
                            Interlocked.Increment(ref threadSafeCounter);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            }).ToArray();
            foreach (var thread in threads)
            {
                thread.Start();
            }
            var tasks = Enumerable.Range(0, concurrency).Select(x =>
            {
                return Task.Run(async () =>
                {
                    await startEvent.WaitHandle.WaitAsync();
                    for (int i = 0; i < counterFactor; i++)
                        unlockedCounter++;
                    await semaphore.WaitAsync();
                    try
                    {
                        //Do work
                        for (int i = 0; i < counterFactor; i++)
                            counter++;
                        for (int i = 0; i < counterFactor; i++)
                            Interlocked.Increment(ref threadSafeCounter);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            }).ToArray();
            startEvent.Set();
            foreach (var thread in threads)
            {
                thread.Join();
            }
            foreach (var task in tasks)
            {
                await task;
            }

            Assert.That(semaphore.CurrentCount, Is.EqualTo(maxCount));

            Assert.That(threadSafeCounter, Is.EqualTo(concurrency * counterFactor * 2));
            if (maxCount == 1)
                Assert.That(counter, Is.EqualTo(concurrency * counterFactor * 2));
            else
                Warn.If(counter, Is.Not.LessThan(threadSafeCounter), "No concurrency errors occured in 'counter'");
            Warn.If(unlockedCounter, Is.Not.LessThan(threadSafeCounter), "No concurrency errors occured in 'unlockedCounter'");
        }

    }
}
