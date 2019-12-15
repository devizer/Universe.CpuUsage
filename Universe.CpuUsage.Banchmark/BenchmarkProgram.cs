using System;
using System.Collections.Generic;
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

            List<Job> jobs = new List<Job>();
            if (IsMono())
            {
                Job jobLlvm = new Job("LLVM", RunMode.Medium).WithWarmupCount(1).With(Jit.Llvm);
                Job jobNoLlvm = new Job("NO LLVM", RunMode.Medium).WithWarmupCount(1);
                jobs.AddRange(new[] { jobLlvm,jobNoLlvm });
            }
            else
                jobs.Add(new Job(".NET Core", RunMode.Medium).WithWarmupCount(1));

            // https://benchmarkdotnet.org/articles/guides/customizing-runtime.html
            IConfig config = ManualConfig.Create(DefaultConfig.Instance);
            foreach (var job in jobs) config.With(job);
            Summary summary = BenchmarkRunner.Run<CpuUsageBenchmarks>(config);
        }
        
        static bool IsMono()
        {
            return Type.GetType("Mono.Runtime", false) != null;
        }

    }
}