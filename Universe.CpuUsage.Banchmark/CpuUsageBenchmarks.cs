using System;
using System.Diagnostics;
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
            CpuUsageReader.GetByProcess();
        }
        
        [Benchmark]
        public void ByThread()
        {
            CpuUsageReader.GetByProcess();
        }
        
    }
}