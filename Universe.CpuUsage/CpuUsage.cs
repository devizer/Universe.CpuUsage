using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Universe.CpuUsage
{
    // Supported by kernel 2.6.26+, mac OS 10.9+, Windows XP/2003 and above
    [StructLayout(LayoutKind.Sequential)]
    public struct CpuUsage
    {
        
        public TimeValue UserUsage { get; set; }
        public TimeValue KernelUsage { get; set; }
        
        public static CpuUsage? GetByProcess()
        {
            return Get(CpuUsageScope.Process);
        }

        public static bool IsSupported => CpuUsageReader.IsSupported; 

        // for intellisense
        public static CpuUsage? GetByThread()
        {
            return Get(CpuUsageScope.Thread);
        }

        // for intellisense
        public static CpuUsage? SafeGet(CpuUsageScope scope)
        {
            return CpuUsageReader.SafeGet(scope);
        }

        // for intellisense
        public static CpuUsage? Get(CpuUsageScope scope)
        {
            return CpuUsageReader.Get(scope);
        }


        public CpuUsage(long userMicroseconds, long kernelMicroseconds)
        {
            const long _1M = 1000000L;
            UserUsage = new TimeValue() {Seconds = userMicroseconds / _1M, MicroSeconds = userMicroseconds % _1M};
            KernelUsage = new TimeValue() {Seconds = kernelMicroseconds / _1M, MicroSeconds = kernelMicroseconds % _1M};
        }


        public long TotalMicroSeconds => UserUsage.TotalMicroSeconds + KernelUsage.TotalMicroSeconds;

        public override string ToString()
        {
            var user = UserUsage.TotalMicroSeconds;
            var kernel = KernelUsage.TotalMicroSeconds;
            return $"{{{(user + kernel) / 1000d:n3} = {user / 1000d:n3} [user] + {kernel / 1000d:n3} [kernel] milliseconds}}";
            // return $"{{User: {UserUsage}, Kernel: {KernelUsage}}}";
        }

        public static CpuUsage Substruct(CpuUsage onEnd, CpuUsage onStart)
        {
            var user = onEnd.UserUsage.TotalMicroSeconds - onStart.UserUsage.TotalMicroSeconds;
            var system = onEnd.KernelUsage.TotalMicroSeconds - onStart.KernelUsage.TotalMicroSeconds;
            return new CpuUsage(user, system);
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
            
            return new CpuUsage(user, system);
        }
        
        public static CpuUsage Add(CpuUsage one, CpuUsage two)
        {
            long user = one.UserUsage.TotalMicroSeconds + two.UserUsage.TotalMicroSeconds;
            long system = one.KernelUsage.TotalMicroSeconds + two.KernelUsage.TotalMicroSeconds;
            return new CpuUsage(user, system);
        }
    }
    
    // replacing it to long will limit usage by 3,170,979 years
    [StructLayout(LayoutKind.Sequential)] 
    public struct TimeValue
    {
        public long Seconds { get; set; }
        public long MicroSeconds { get; set; }

        public long TotalMicroSeconds => Seconds * 1000000 + MicroSeconds;
        public double TotalSeconds => Seconds + MicroSeconds / 1000000d;

        public TimeValue(long totalMicroseconds)
        {
            const long _1M = 1000000L;
            Seconds = totalMicroseconds / _1M;
            MicroSeconds = totalMicroseconds % _1M;
        }


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