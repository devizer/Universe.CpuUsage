using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Universe.CpuUsage.Banchmark
{
    class BenchmarkProgram
    {
        static void Main(string[] args)
        {
            IConfig config = ManualConfig.Create(DefaultConfig.Instance).With(Job.ShortRun.WithWarmupCount(1));
            // config.Add(DefaultConfig.Instance.GetExporters());
            // c.Add(DefaultConfig.Instance.GetLoggers());
            Summary summary = BenchmarkRunner.Run<CpuUsageBenchmarks>(config);

        }
    }
}