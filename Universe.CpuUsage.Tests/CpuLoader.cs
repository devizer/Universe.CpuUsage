using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Universe.CpuUsage.Tests
{
    public class CpuLoader
    {
        // contains value of each CpuUsage increment 
        public readonly List<long> Population = new List<long>(10000);

        public int IncrementsCount => Population.Count;

        public static CpuLoader Run(int minDuration, bool needKernelLoad)
        {
            CpuLoader ret = new CpuLoader();
            ret.LoadCpu(minDuration, needKernelLoad);
            return ret;
        }

        public long LoadCpu(int milliseconds, bool needKernelLoad)
        {
            long ret = 0;
            Stopwatch sw = Stopwatch.StartNew();
            CpuUsage prev = CpuUsage.GetByThread().Value;
            while (sw.ElapsedMilliseconds <= milliseconds)
            {
                if (needKernelLoad)
                {
                    var ptr = Marshal.AllocHGlobal(1024);
                    Marshal.FreeHGlobal(ptr);
                }

                CpuUsage next = CpuUsage.GetByThread().Value;
                if (next.TotalMicroSeconds != prev.TotalMicroSeconds)
                {
                    Population.Add(CpuUsage.Substruct(next, prev).TotalMicroSeconds);
                    ret++;
                }

                prev = next;
            }

            return ret;
        }

    }
}