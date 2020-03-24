using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Universe.CpuUsage.Tests
{
    public class CpuLoader
    {
        // contains a value of each CpuUsage increment 
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
        private void LoadCpu(int minDuration, int minCpuUsage, bool needKernelLoad)
        {
            Stopwatch sw = Stopwatch.StartNew();
            CpuUsage firstUsage = CpuUsage.GetByThread().Value;
            CpuUsage prev = firstUsage;
            bool isFirstChange = true;
            bool isDone = false;
            while (!isDone)
            {
                if (needKernelLoad)
                {
                    var ptr = Marshal.AllocHGlobal(2*1024);
                    Marshal.FreeHGlobal(ptr);
                }

                CpuUsage nextUsage = CpuUsage.GetByThread().Value;
                if (nextUsage.TotalMicroSeconds != prev.TotalMicroSeconds)
                {
                    var increment = CpuUsage.Substruct(nextUsage, prev).TotalMicroSeconds;
                    if (!isFirstChange) Population.Add(increment);
                    isFirstChange = false;
                }
                
                prev = nextUsage;

                isDone = 
                    sw.ElapsedMilliseconds >= minDuration 
                    && (nextUsage - firstUsage).TotalMicroSeconds >= minCpuUsage * 1000L;
            }
        }

    }
}