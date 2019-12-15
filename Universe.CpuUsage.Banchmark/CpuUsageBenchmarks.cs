using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Universe.CpuUsage.Banchmark
{
    /*[SimpleJob(RuntimeMoniker.NetCoreApp30)]*/
    [RankColumn]
    [MemoryDiagnoser]
    public class CpuUsageBenchmarks
    {
        [Benchmark]
        public void DateTimeNow()
        {
            var now = DateTime.Now;
        }

        [Benchmark]
        public void Stopwatch()
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            var ticks = sw.ElapsedTicks;
        }

        [Benchmark]
        public void ByProcess()
        {
            CpuUsage.GetByProcess();
        }
        
        [Benchmark]
        public void ByThread()
        {
            CpuUsage.GetByProcess();
        }

        [Benchmark]
        public async Task CpuUsageAsyncMinimal()
        {
            CpuUsageAsyncWatcher watcher = new CpuUsageAsyncWatcher();
            await Task.Run(() => "nothing to do");
            var totals = watcher.Totals.GetSummaryCpuUsage();
        }
        
    }
}