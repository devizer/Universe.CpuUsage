using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Tests;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class GranularityTest: NUnitTestsBase
    {
        [Test]
        public void ShowGranularity()
        {
            long preJit = LoadCpu(111);
            Console.WriteLine($"OS: {CrossFullInfo.OsDisplayName}");
            Console.WriteLine($"CPU: {CrossFullInfo.ProcessorName}");
            Console.WriteLine("Granularity (it may vary if Intel SpeedStep, TorboBoost, etc are active):");
            int count = CrossFullInfo.IsMono ? 1 : 9;
            for (int i = 1; i <= count; i++)
            {
                long granularity = LoadCpu(1000);
                double microSeconds = 1000000d / granularity;
                Console.WriteLine($" #{i}: {granularity} a second, eg {microSeconds:n1} microseconds");
            }

        }

        private long LoadCpu(int milliseconds)
        {
            long ret = 0;
            Stopwatch sw = Stopwatch.StartNew();
            CpuUsage prev = CpuUsageReader.GetByThread().Value;
            while (sw.ElapsedMilliseconds <= milliseconds)
            {
                var ptr = Marshal.AllocHGlobal(1024);
                Marshal.FreeHGlobal(ptr);

                CpuUsage next = CpuUsageReader.GetByThread().Value;
                // Console.WriteLine(next);
                if (next.TotalMicroSeconds != prev.TotalMicroSeconds)
                    ret++;

                prev = next;
            }

            return ret;
        }
    }
}
