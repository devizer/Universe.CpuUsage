﻿using System;
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
            // var run = Job.ShortRun;
            var run = Job.MediumRun;
            IConfig config = ManualConfig.Create(DefaultConfig.Instance);
            // Job jobLlvm = Job.InProcess;
            Job jobLlvm = run.With(Jit.Llvm).With(MonoRuntime.Default).WithId("LLVM-ON").WithWarmupCount(3);
            Job jobNoLlvm = run.With(MonoRuntime.Default).WithId("LLVM-OFF").WithWarmupCount(3);
            Job jobCore = run.With(CoreRuntime.Core22).WithId("NET-CORE").WithWarmupCount(3);
            config = config.With(new[] { jobLlvm, jobNoLlvm, jobCore});
            Summary summary = BenchmarkRunner.Run<CpuUsageBenchmarks>(config);
        }

        public static void Main_Always_NOLLVM(string[] args)
        {
            // Job jobLlvm = Job.ShortRun.WithWarmupCount(1).With(Jit.Llvm);

            // https://benchmarkdotnet.org/articles/guides/customizing-runtime.html
            IConfig config = ManualConfig.Create(DefaultConfig.Instance);
            Job jobLlvm = Job.ShortRun.With(Jit.Llvm).With(MonoRuntime.Default);
            config.With(jobLlvm);
            Summary summary = BenchmarkRunner.Run<CpuUsageBenchmarks>(config);
        }

/*
        public static void Main_PREV(string[] args)
        {
            
            // bool isLlvm = 

            if (IsMono())
            {
                
            }

            List<Job> jobs = new List<Job>();
            if (IsMono())
            {
                // Job jobLlvm = new Job("LLVM", RunMode.Short).WithWarmupCount(1).With(Jit.Llvm);
                Job jobLlvm = Job.ShortRun.WithWarmupCount(1).With(Jit.Llvm);
                Job jobNoLlvm = new Job("NO LLVM", RunMode.Short).WithWarmupCount(1);
                jobs.AddRange(new[] { jobLlvm/*, jobNoLlvm#1# });
            }
            else
            {
                // jobs.Add(new Job(".NET Core", RunMode.Short).WithWarmupCount(1));
            }

            // https://benchmarkdotnet.org/articles/guides/customizing-runtime.html
            IConfig config = ManualConfig.Create(DefaultConfig.Instance);
            config.With(jobs.ToArray());
            Summary summary = BenchmarkRunner.Run<CpuUsageBenchmarks>(config);
        }
        
        static bool IsMono()
        {
            return Type.GetType("Mono.Runtime", false) != null;
        }
*/

    }
}