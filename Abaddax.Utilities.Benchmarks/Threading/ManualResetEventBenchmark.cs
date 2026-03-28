using Abaddax.Utilities.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Abaddax.Utilities.Benchmarks.Threading
{
    [MemoryDiagnoser(true)]
    [ThreadingDiagnoser]
    public class ManualResetEventBenchmark
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        private readonly ManualResetEventSlim _resetEventSlim = new ManualResetEventSlim(false);
        private readonly ManualResetEventLite _resetEventLite = new ManualResetEventLite(false);

        private Thread _setThread;
        private Thread _setSlimThread;
        private Thread _setLiteThread;

        private volatile bool _start;

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private void WaitForStart()
        {
            while (!_start)
                ;
            for (int i = 0; i < 10_000_000; i++)
            {
                ;//Do work
            }
        }

        [IterationSetup(Targets = [nameof(WaitManualResetEvent), nameof(WaitManualResetEventAsync)])]
        public void SetupManualResetEvent()
        {
            _setThread = new Thread(() =>
            {
                WaitForStart();
                _resetEvent.Set();
            });
            _setThread.Start();
        }
        [IterationCleanup(Targets = [nameof(WaitManualResetEvent), nameof(WaitManualResetEventAsync)])]
        public void CleanupManualResetEvent()
        {
            _setThread.Join();
            while (!_resetEvent.Reset())
                continue;
        }
        [Benchmark(Baseline = true)]
        public int WaitManualResetEvent()
        {
            _start = true;
            while (!_resetEvent.WaitOne())
                continue;
            return 1;
        }
        [Benchmark]
        public async Task<int> WaitManualResetEventAsync()
        {
            _start = true;
            await _resetEvent.WaitAsync();
            return 1;
        }


        [IterationSetup(Targets = [nameof(WaitManualResetEventSlim), nameof(WaitManualResetEventSlimAsync)])]
        public void SetupManualResetEventSlim()
        {
            _start = false;
            _setSlimThread = new Thread(() =>
            {
                WaitForStart();
                _resetEventSlim.Set();
            });
            _setSlimThread.Start();
        }
        [IterationCleanup(Targets = [nameof(WaitManualResetEventSlim), nameof(WaitManualResetEventSlimAsync)])]
        public void CleanupManualResetEventSlim()
        {
            _setSlimThread.Join();
            _resetEventSlim.Reset();
        }
        [Benchmark]
        public int WaitManualResetEventSlim()
        {
            _start = true;
            _resetEventSlim.Wait();
            return 1;
        }
        [Benchmark]
        public async Task<int> WaitManualResetEventSlimAsync()
        {
            _start = true;
            await _resetEventSlim.WaitHandle.WaitAsync();
            return 1;
        }


        [IterationSetup(Targets = [nameof(WaitManualResetEventLite), nameof(WaitManualResetEventLiteAsync)])]
        public void SetupManualResetEventLite()
        {
            _start = false;
            _setLiteThread = new Thread(() =>
            {
                WaitForStart();
                _resetEventLite.Set();
            });
            _setLiteThread.Start();
        }
        [IterationCleanup(Targets = [nameof(WaitManualResetEventLite), nameof(WaitManualResetEventLiteAsync)])]
        public void CleanupManualResetEventLite()
        {
            _setLiteThread.Join();
            _resetEventLite.Reset();
        }
        [Benchmark]
        public int WaitManualResetEventLite()
        {
            _start = true;
            _resetEventLite.Wait();
            return 1;
        }
        [Benchmark]
        public async Task<int> WaitManualResetEventLiteAsync()
        {
            _start = true;
            await _resetEventLite.WaitAsync();
            return 1;
        }

    }
}
