using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Tests;
using Universe.CpuUsage.Tests.Statistica;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class PrecisionTest : NUnitTestsBase
    {
        public class PrecisionCase
        {
            public ProcessPriorityClass Priority;
            public bool IncludeKernelLoad;

            public PrecisionCase(ProcessPriorityClass priority, bool includeKernelLoad)
            {
                Priority = priority;
                IncludeKernelLoad = includeKernelLoad;
            }

            public override string ToString()
            {
                return $"{Priority} priority, {(IncludeKernelLoad ? "WITH KERNEL Load" : "without kernel load")}";
            }
        }

        public static PrecisionCase[] GetGranularityCases()
        {
            var fullCases = new[]
            {
                new PrecisionCase(ProcessPriorityClass.AboveNormal, false),
                new PrecisionCase(ProcessPriorityClass.AboveNormal, true),
                new PrecisionCase(ProcessPriorityClass.Normal, false),
                new PrecisionCase(ProcessPriorityClass.Normal, true),
                new PrecisionCase(ProcessPriorityClass.BelowNormal, false),
                new PrecisionCase(ProcessPriorityClass.BelowNormal, true),
            };

            if (CrossFullInfo.IsMono)
                return new[]
                {
                    new PrecisionCase(ProcessPriorityClass.AboveNormal, false),
                    new PrecisionCase(ProcessPriorityClass.AboveNormal, true),
                };

            return fullCases;
        }


        [Test]
        [TestCaseSource(typeof(PrecisionTest), nameof(GetGranularityCases))]
        public void Show_Precision_Histogram(PrecisionCase precisionCase)
        {
            PosixProcessPriority.ApplyMyPriority(precisionCase.Priority);
            var preJit = CpuLoader.Run(11, 1, true).IncrementsCount;
            Console.WriteLine($"OS: {CrossFullInfo.OsDisplayName}");
            Console.WriteLine($"CPU: {CrossFullInfo.ProcessorName}");
            var actualCase = new PrecisionCase(Process.GetCurrentProcess().PriorityClass, precisionCase.IncludeKernelLoad);
            Console.WriteLine($"Granularity[{actualCase}] (it may vary if Intel SpeedStep, TurboBoost, etc are active):");
            int count = CrossFullInfo.IsMono ? 2 : 9;
            for (int i = 1; i <= count; i++)
            {
                var cpuLoadResult = CpuLoader.Run(minDuration: 1000, needKernelLoad: precisionCase.IncludeKernelLoad);
                long granularity = cpuLoadResult.IncrementsCount;
                double microSeconds = 1000000d / granularity;
                Console.WriteLine($" #{i}: {granularity} increments a second, eg {microSeconds:n1} microseconds in average");
                
                // Percentile report
                PercentileCalc<long> percentileCalc = new PercentileCalc<long>(cpuLoadResult.Population, x => x, x => x.ToString("n3"));
                double[] percents = new[] {1d, 5, 10, 20, 30, 40, 50, 60, 70, 80, 90, 95, 99};
                // double[] percents = new[] {1d, 5, 10, 90, 95, 99}.Reverse().ToArray();
                StringBuilder pcReport = new StringBuilder();
                for(int p=0; p<percents.Length; p++)
                {
                    pcReport.Append($"  {percents[p].ToString("f0").PadLeft(3)}%: {percentileCalc[percents[p]].ToString("n3"),-12}");
                    if ((p+1) % 5 == 0) pcReport.AppendLine();
                }
                
                Console.WriteLine(pcReport);

                // Histogram report
                Statistica<long> stat = new Statistica<long>(cpuLoadResult.Population, x => (double) x, x => x, x => x.ToString("n3"));
                var histogram = stat.BuildReport(12, 3);
                Console.WriteLine(histogram.ToConsole("CPU Usage increments (microseconds)", 42));
            }

        }


    }
}

