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
            Job jobCore21 = run.With(CoreRuntime.Core21).WithId("NET-CORE").WithWarmupCount(3);
            Job jobCore50 = run.With(CoreRuntime.Core50).WithId("NET-CORE").WithWarmupCount(3);
            Job jobFW47 = run.With(ClrRuntime.Net47).WithId("NETFW-47").WithWarmupCount(3);
            config = config.With(new[] { jobLlvm, jobNoLlvm, jobCore21, jobCore50});
            Summary summary = BenchmarkRunner.Run<CpuUsageBenchmarks>(config);
        }


    }
}