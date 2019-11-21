using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class GranularityTest
    {
        [Test]
        public void ShowGranularity()
        {
            long preJit = LoadCpu(111);
            Console.WriteLine("Granularity (it may vary if Intel SpeedStep, TorboBoost, etc are active):");
            for (int i = 1; i <= 9; i++)
            {
                long granularity = LoadCpu(1000);
                Console.WriteLine($" #{i}: {granularity} a second. ");
            }

        }

        private long LoadCpu(int milliseconds)
        {
            long ret = 0;
            Stopwatch sw = Stopwatch.StartNew();
            CpuUsage prev = CpuUsageReader.GetByThread().Value;
            while (sw.ElapsedMilliseconds <= milliseconds)
            {
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
