using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Universe.CpuUsage.Banchmark
{
    class BenchmarkProgram
    {
        public static void Main(string[] args)
        {

            if (IsMono())
            {
                
            }
            
            Job jobLlvm = Job.ShortRun.WithWarmupCount(1).With(Jit.Llvm);
            // https://benchmarkdotnet.org/articles/guides/customizing-runtime.html
            IConfig config = ManualConfig.Create(DefaultConfig.Instance).With(jobLlvm);
            Summary summary = BenchmarkRunner.Run<CpuUsageBenchmarks>(config);
        }
        
        static bool IsMono()
        {
            return Type.GetType("Mono.Runtime", false) != null;
        }

    }
}