using Abaddax.Utilities.Threading;
using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;

namespace Abaddax.Utilities.Benchmarks.Threading
{
    [MemoryDiagnoser(true)]
    [ThreadingDiagnoser]
    public class SemaphoreBenchmarks
    {
        private readonly Semaphore _semaphore = new Semaphore(1, 1);
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly SemaphoreLite _semaphoreLite = new SemaphoreLite(1, 1);

        private Thread _semaphoreThread;
        private Thread _semaphoreSlimThread;
        private Thread _semaphoreLiteThread;

        private volatile bool _start;

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private void WaitForStart()
        {
            while (!_start)
                ;
            for (int i = 0; i < 100_000; i++)
            {
                ;//Do work
            }
        }

        [IterationSetup(Targets = [nameof(LockSemaphore), nameof(LockSemaphoreAsync)])]
        public void SetupSemaphore()
        {
            _start = false;
            _semaphoreThread = new Thread(() =>
            {
                while (!_semaphore.WaitOne())
                    continue;
                try
                {
                    WaitForStart();
                }
                finally
                {
                    _semaphore.Release();
                }
            });
            _semaphoreThread.Start();
        }
        [IterationCleanup(Targets = [nameof(LockSemaphore), nameof(LockSemaphoreAsync)])]
        public void CleanupSemaphore()
        {
            _semaphoreThread.Join();
        }
        [Benchmark(Baseline = true)]
        public int LockSemaphore()
        {
            _start = true;
            while (!_semaphore.WaitOne())
                continue;
            try
            {
                return 1;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        [Benchmark]
        public async Task<int> LockSemaphoreAsync()
        {
            _start = true;
            await _semaphore.WaitAsync();
            try
            {
                return 1;
            }
            finally
            {
                _semaphore.Release();
            }
        }


        [IterationSetup(Targets = [nameof(LockSemaphoreSlim), nameof(LockSemaphoreSlimAsync)])]
        public void SetupSemaphoreSlim()
        {
            _start = false;
            _semaphoreSlimThread = new Thread(() =>
            {
                _semaphoreSlim.Wait();
                try
                {
                    WaitForStart();
                }
                finally
                {
                    _semaphoreSlim.Release();
                }
            });
            _semaphoreSlimThread.Start();
        }
        [IterationCleanup(Targets = [nameof(LockSemaphoreSlim), nameof(LockSemaphoreSlimAsync)])]
        public void CleanupSemaphoreSlim()
        {
            _semaphoreSlimThread.Join();
        }
        [Benchmark]
        public int LockSemaphoreSlim()
        {
            _start = true;
            _semaphoreSlim.Wait();
            try
            {
                return 1;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
        [Benchmark]
        public async Task<int> LockSemaphoreSlimAsync()
        {
            _start = true;
            await _semaphoreSlim.WaitAsync();
            try
            {
                return 1;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }


        [IterationSetup(Targets = [nameof(LockSemaphoreLite), nameof(LockSemaphoreLiteAsync)])]
        public void SetupSemaphoreLite()
        {
            _start = false;
            _semaphoreLiteThread = new Thread(() =>
            {
                _semaphoreLite.Wait();
                try
                {
                    WaitForStart();
                }
                finally
                {
                    _semaphoreLite.Release();
                }
            });
            _semaphoreLiteThread.Start();
        }
        [IterationCleanup(Targets = [nameof(LockSemaphoreLite), nameof(LockSemaphoreLiteAsync)])]
        public void CleanupSemaphoreLite()
        {
            _semaphoreLiteThread.Join();
        }
        [Benchmark]
        public int LockSemaphoreLite()
        {
            _start = true;
            _semaphoreLite.Wait();
            try
            {
                return 1;
            }
            finally
            {
                _semaphoreLite.Release();
            }
        }
        [Benchmark]
        public async Task<int> LockSemaphoreLiteAsync()
        {
            _start = true;
            await _semaphoreLite.WaitAsync();
            try
            {
                return 1;
            }
            finally
            {
                _semaphoreLite.Release();
            }
        }

    }
}
