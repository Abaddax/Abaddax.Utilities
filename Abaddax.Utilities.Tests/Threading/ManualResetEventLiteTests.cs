using Abaddax.Utilities.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Abaddax.Utilities.Tests.Threading
{
    public class ManualResetEventLiteTests
    {
        [Test]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public void ShouldWaitForSetSync(int concurrency)
        {
            var resetEvent = new ManualResetEventLite();
            ConcurrentBag<DateTime> setTimes = new();

            var threads = Enumerable.Range(0, concurrency).Select(x =>
            {
                return new Thread(() =>
                {
                    resetEvent.Wait();
                    setTimes.Add(DateTime.UtcNow);
                });
            }).ToArray();
            foreach (var thread in threads)
            {
                thread.Start();
            }
            Thread.Sleep(1000);
            DateTime setTime = DateTime.UtcNow;
            resetEvent.Set();            
            Assert.That(resetEvent.IsSet, Is.True);
            foreach (var thread in threads)
            {
                thread.Join();
            }

            resetEvent.Reset();
            Assert.That(resetEvent.IsSet, Is.False);

            Assert.That(setTimes, Has.Count.EqualTo(concurrency));

            foreach(var threadSetTime in setTimes)
            {
                Assert.That(threadSetTime, Is.GreaterThan(setTime));
                Assert.That(threadSetTime, Is.EqualTo(setTime).Within(TimeSpan.FromMilliseconds(50)));
            }
        }
        [Test]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public async Task ShouldWaitForSetAync(int concurrency)
        {
            var resetEvent = new ManualResetEventLite();
            ConcurrentBag<DateTime> setTimes = new();

            var tasks = Enumerable.Range(0, concurrency).Select(x =>
            {
                return Task.Run(async () =>
                {
                    await resetEvent.WaitAsync();
                    setTimes.Add(DateTime.UtcNow);
                });
            }).ToArray();
            await Task.Delay(1000);
            DateTime setTime = DateTime.UtcNow;
            resetEvent.Set();
            Assert.That(resetEvent.IsSet, Is.True);
            foreach (var task in tasks)
            {
                await task;
            }

            resetEvent.Reset();
            Assert.That(resetEvent.IsSet, Is.False);

            Assert.That(setTimes, Has.Count.EqualTo(concurrency));

            foreach (var threadSetTime in setTimes)
            {
                Assert.That(threadSetTime, Is.GreaterThan(setTime));
                Assert.That(threadSetTime, Is.EqualTo(setTime).Within(TimeSpan.FromMilliseconds(50)));
            }
        }
        [Test]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public async Task ShouldWaitForSetSyncAndAync(int concurrency)
        {
            var resetEvent = new ManualResetEventLite();
            ConcurrentBag<DateTime> setTimes = new();
            
            var threads = Enumerable.Range(0, concurrency).Select(x =>
            {
                return new Thread(() =>
                {
                    resetEvent.Wait();
                    setTimes.Add(DateTime.UtcNow);
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
                    await resetEvent.WaitAsync();
                    setTimes.Add(DateTime.UtcNow);
                });
            }).ToArray();
            await Task.Delay(1000);
            DateTime setTime = DateTime.UtcNow;
            resetEvent.Set();
            Assert.That(resetEvent.IsSet, Is.True);
            foreach (var thread in threads)
            {
                thread.Join();
            }
            foreach (var task in tasks)
            {
                await task;
            }

            resetEvent.Reset();
            Assert.That(resetEvent.IsSet, Is.False);

            Assert.That(setTimes, Has.Count.EqualTo(concurrency*2));

            foreach (var threadSetTime in setTimes)
            {
                Assert.That(threadSetTime, Is.GreaterThan(setTime));
                Assert.That(threadSetTime, Is.EqualTo(setTime).Within(TimeSpan.FromMilliseconds(50)));
            }
        }

    }
}
