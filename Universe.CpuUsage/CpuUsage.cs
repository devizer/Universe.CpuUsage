using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Universe.CpuUsage
{
    // Supported by kernel 2.6.26+ and mac os 10.9+, Windows XP/2003 and above
    [StructLayout(LayoutKind.Sequential)]
    public struct CpuUsage
    {
        
        public TimeValue UserUsage { get; set; }
        public TimeValue KernelUsage { get; set; }

        public long TotalMicroSeconds => UserUsage.TotalMicroSeconds + KernelUsage.TotalMicroSeconds;

        public override string ToString()
        {
            return $"{{User: {UserUsage}, Kernel: {KernelUsage}}}";
        }

        public static CpuUsage Substruct(CpuUsage onEnd, CpuUsage onStart)
        {
            var user = onEnd.UserUsage.TotalMicroSeconds - onStart.UserUsage.TotalMicroSeconds;
            var system = onEnd.KernelUsage.TotalMicroSeconds - onStart.KernelUsage.TotalMicroSeconds;
            const long _1M = 1000000L;
            return new CpuUsage()
            {
                UserUsage = new TimeValue() {Seconds = user / _1M, MicroSeconds = user % _1M},
                KernelUsage = new TimeValue() {Seconds = system / _1M, MicroSeconds = system % _1M},
            };
        }

        public static CpuUsage Sum(IEnumerable<CpuUsage> list)
        {
            long user = 0;
            long system = 0;
            foreach (var item in list)
            {
                user += item.UserUsage.TotalMicroSeconds;
                system += item.KernelUsage.TotalMicroSeconds;
            }
            
            const long _1M = 1000000L;
            return new CpuUsage()
            {
                UserUsage = new TimeValue() {Seconds = user / _1M, MicroSeconds = user % _1M},
                KernelUsage = new TimeValue() {Seconds = system / _1M, MicroSeconds = system % _1M},
            };
        }
        
        public static CpuUsage Add(CpuUsage one, CpuUsage two)
        {
            long user = one.UserUsage.TotalMicroSeconds + two.UserUsage.TotalMicroSeconds;
            long system = one.KernelUsage.TotalMicroSeconds + two.KernelUsage.TotalMicroSeconds;
            const long _1M = 1000000L;
            return new CpuUsage()
            {
                UserUsage = new TimeValue() {Seconds = user / _1M, MicroSeconds = user % _1M},
                KernelUsage = new TimeValue() {Seconds = system / _1M, MicroSeconds = system % _1M},
            };
        }
    }
    
    // replacing it to long will limit usage by 3,170,979
    [StructLayout(LayoutKind.Sequential)] 
    public struct TimeValue
    {
        public long Seconds { get; set; }
        public long MicroSeconds { get; set; }

        public long TotalMicroSeconds => Seconds * 1000000 + MicroSeconds;
        public double TotalSeconds => Seconds + MicroSeconds / 1000000d;


        public override string ToString()
        {
            return $"{TotalMicroSeconds / 1000d:n3} milliseconds";
        }
    }
    
    public enum CpuUsageScope
    {
        Thread, 
        Process,  
    }

}