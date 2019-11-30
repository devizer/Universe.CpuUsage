using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Universe.CpuUsage.Tests
{
    public class CpuLoader
    {
        // contains value of each CpuUsage increment 
        public readonly List<long> Population = new List<long>(64000/8);

        public int IncrementsCount => Population.Count;

        // MILLI seconds
        public static CpuLoader Run(int minDuration = 1, int minCpuUsage = 1, bool needKernelLoad = true)
        {
            CpuLoader ret = new CpuLoader();
            ret.LoadCpu(minDuration, minCpuUsage, needKernelLoad);
            return ret;
        }

        // MILLI seconds
        private long LoadCpu(int minDuration, int minCpuUsage, bool needKernelLoad)
        {
            long ret = 0;
            Stopwatch sw = Stopwatch.StartNew();
            CpuUsage prev = CpuUsage.GetByThread().Value;
            var firstUsage = prev;
            CpuUsage next = prev;
            
            while (true)
            {
                bool isDone = sw.ElapsedMilliseconds >= minDuration
                              && (CpuUsage.Substruct(next, firstUsage).TotalMicroSeconds >= minCpuUsage * 1000L);

                if (isDone) break;
                
                if (needKernelLoad)
                {
                    var ptr = Marshal.AllocHGlobal(512*1024);
                    Marshal.FreeHGlobal(ptr);
                }

                next = CpuUsage.GetByThread().Value;
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