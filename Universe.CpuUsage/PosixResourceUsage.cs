using System;
using System.Collections.Generic;
using Universe.CpuUsage.Interop;

namespace Universe.CpuUsage
{
    // for linux and mac os only
    public struct PosixResourceUsage
    {
        
        public static bool IsSupported => LinuxResourceUsageReader.IsSupported;

        public TimeValue UserUsage { get; set; }
        public TimeValue KernelUsage { get; set; }
        
        public long MaxRss { get; set; }
        
        public long SoftPageFaults { get; set; }
        public long HardPageFaults { get; set; }
        public long Swaps { get; set; }
        public long ReadOps { get; set; }
        public long WriteOps { get; set; }
        // mac os only
        public long SentIpcMessages { get; set; }
        // mac os only
        public long ReceivedIpcMessages { get; set; }
        // mac os only
        public long ReceivedSignals { get; set; }
        
        // usually to await availability of a resource 
        public long VoluntaryContextSwitches { get; set; }
        // a higher priority process becoming runnable or because the current process exceeded its time slice.
        public long InvoluntaryContextSwitches { get; set; }

        public static PosixResourceUsage? GetByScope(CpuUsageScope scope)
        {
            int kernelScope = scope == CpuUsageScope.Process ? LinuxResourceUsageInterop.RUSAGE_SELF : LinuxResourceUsageInterop.RUSAGE_THREAD;
            return LinuxResourceUsageReader.GetResourceUsageByScope(kernelScope);
        }
        
        public static PosixResourceUsage Substruct(PosixResourceUsage onEnd, PosixResourceUsage onStart)
        {
            var user = onEnd.UserUsage.TotalMicroSeconds - onStart.UserUsage.TotalMicroSeconds;
            var system = onEnd.KernelUsage.TotalMicroSeconds - onStart.KernelUsage.TotalMicroSeconds;
            return new PosixResourceUsage()
            {
                UserUsage = new TimeValue(user),
                KernelUsage = new TimeValue(system),
                MaxRss = Math.Max(onEnd.MaxRss, onStart.MaxRss),
                SoftPageFaults = onEnd.SoftPageFaults - onStart.SoftPageFaults,
                HardPageFaults = onEnd.HardPageFaults - onStart.HardPageFaults,
                Swaps = onEnd.Swaps - onStart.Swaps,
                ReadOps = onEnd.ReadOps - onStart.ReadOps,
                WriteOps = onEnd.WriteOps - onStart.WriteOps,
                SentIpcMessages = onEnd.SentIpcMessages - onStart.SentIpcMessages,
                ReceivedIpcMessages = onEnd.InvoluntaryContextSwitches - onStart.InvoluntaryContextSwitches,
                ReceivedSignals = onEnd.ReceivedSignals - onStart.ReceivedSignals,
                VoluntaryContextSwitches = onEnd.VoluntaryContextSwitches - onStart.VoluntaryContextSwitches,
                InvoluntaryContextSwitches = onEnd.InvoluntaryContextSwitches - onStart.InvoluntaryContextSwitches,
            };
        }

        public static PosixResourceUsage Add(PosixResourceUsage one, PosixResourceUsage two)
        {
            long user = one.UserUsage.TotalMicroSeconds + two.UserUsage.TotalMicroSeconds;
            long system = one.KernelUsage.TotalMicroSeconds + two.KernelUsage.TotalMicroSeconds;
            return new PosixResourceUsage()
            {
                UserUsage = new TimeValue(user),
                KernelUsage = new TimeValue(system),
                MaxRss = Math.Max(two.MaxRss, one.MaxRss),
                SoftPageFaults = two.SoftPageFaults + one.SoftPageFaults,
                HardPageFaults = two.HardPageFaults + one.HardPageFaults,
                Swaps = two.Swaps + one.Swaps,
                ReadOps = two.ReadOps + one.ReadOps,
                WriteOps = two.WriteOps + one.WriteOps,
                SentIpcMessages = two.SentIpcMessages + one.SentIpcMessages,
                ReceivedIpcMessages = two.InvoluntaryContextSwitches + one.InvoluntaryContextSwitches,
                ReceivedSignals = two.ReceivedSignals + one.ReceivedSignals,
                VoluntaryContextSwitches = two.VoluntaryContextSwitches + one.VoluntaryContextSwitches,
                InvoluntaryContextSwitches = two.InvoluntaryContextSwitches + one.InvoluntaryContextSwitches,
            };
        }
        
        public static PosixResourceUsage Sum(IEnumerable<PosixResourceUsage> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            PosixResourceUsage ret = new PosixResourceUsage();
            foreach (var item in list)
                ret = Add(ret, item);
            
            return ret;
        }

        public static PosixResourceUsage? Sum(IEnumerable<PosixResourceUsage?> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            PosixResourceUsage? ret = null;
            foreach (var item in list)
            {
                if (ret.HasValue || item.HasValue)
                    ret = Add(ret.GetValueOrDefault(), item.GetValueOrDefault());
            }
            
            return ret;
        }

        public static PosixResourceUsage operator -(PosixResourceUsage onEnd, PosixResourceUsage onStart)
        {
            return Substruct(onEnd, onStart);
        }

        public static PosixResourceUsage? operator -(PosixResourceUsage? onEnd, PosixResourceUsage? onStart)
        {
            if (onEnd.HasValue || onStart.HasValue)
                return Substruct(onEnd.GetValueOrDefault(), onStart.GetValueOrDefault());
            
            return null;
        }

        public static PosixResourceUsage operator +(PosixResourceUsage one, PosixResourceUsage two)
        {
            return Add(one, two);
        }

        public static PosixResourceUsage? operator +(PosixResourceUsage? one, PosixResourceUsage? two)
        {
            if (one.HasValue || two.HasValue)
                return Add(one.GetValueOrDefault(), two.GetValueOrDefault());

            return null;
        }

    }
}