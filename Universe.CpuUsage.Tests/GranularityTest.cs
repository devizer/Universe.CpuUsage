using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using Tests;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class GranularityTest: NUnitTestsBase
    {
        [Test]
        [TestCase(ProcessPriorityClass.BelowNormal, "WITH kernel load")]
        [TestCase(ProcessPriorityClass.Normal, "WITH kernel load")]
        [TestCase(ProcessPriorityClass.AboveNormal, "WITH kernel load")]
        [TestCase(ProcessPriorityClass.BelowNormal, "without kernel load")]
        [TestCase(ProcessPriorityClass.Normal, "without kernel load")]
        [TestCase(ProcessPriorityClass.AboveNormal, "without kernel load")]
        public void ShowGranularity(ProcessPriorityClass priority, string includeKernelLoad)
        {
            bool needKernelLoad = includeKernelLoad.IndexOf("WITHOUT", StringComparison.InvariantCultureIgnoreCase) < 0;
            ApplyPriority(priority);
            long preJit = LoadCpu(111, true);
            Console.WriteLine($"OS: {CrossFullInfo.OsDisplayName}");
            Console.WriteLine($"CPU: {CrossFullInfo.ProcessorName}");
            Console.WriteLine($"Granularity[{Process.GetCurrentProcess().PriorityClass},{includeKernelLoad}] (it may vary if Intel SpeedStep, TorboBoost, etc are active):");
            int count = CrossFullInfo.IsMono ? 1 : 9;
            for (int i = 1; i <= count; i++)
            {
                long granularity = LoadCpu(1000, needKernelLoad);
                double microSeconds = 1000000d / granularity;
                Console.WriteLine($" #{i}: {granularity} increments a second, eg {microSeconds:n1} microseconds in average");

                Statistica<long> stat = new Statistica<long>(Population, x => (double) x, x => x, x => x.ToString("n3"));
                var histogram = stat.BuildReport(12, 3);
                Console.WriteLine(histogram.ToConsole("CPU Usage increments (microseconds)", 42));
            }

        }

        private List<long> Population;
        private long LoadCpu(int milliseconds, bool needKernelLoad)
        {
            Population = new List<long>(7000);
            long ret = 0;
            Stopwatch sw = Stopwatch.StartNew();
            CpuUsage prev = CpuUsageReader.GetByThread().Value;
            while (sw.ElapsedMilliseconds <= milliseconds)
            {
                if (needKernelLoad)
                {
                    var ptr = Marshal.AllocHGlobal(1024);
                    Marshal.FreeHGlobal(ptr);
                }

                CpuUsage next = CpuUsageReader.GetByThread().Value;
                if (next.TotalMicroSeconds != prev.TotalMicroSeconds)
                {
                    Population.Add(CpuUsage.Substruct(next,prev).TotalMicroSeconds);
                    ret++;
                }

                prev = next;
            }

            return ret;
        }

        private void ApplyPriority(ProcessPriorityClass priority)
        {
            try
            {
                Process.GetCurrentProcess().PriorityClass = priority;
            }
            catch (Win32Exception ex)
            {
                Console.WriteLine($"Win32Exception.NativeErrorCode: {ex.NativeErrorCode}. {ex.Message}" );
            }

            if (priority == ProcessPriorityClass.AboveNormal)
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            else if (priority == ProcessPriorityClass.Normal)
                Thread.CurrentThread.Priority = ThreadPriority.Normal;
            else if (priority == ProcessPriorityClass.BelowNormal)
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            else
                throw new NotImplementedException($"Priority {priority} is not implemented for a thread");
        }
    }
}
