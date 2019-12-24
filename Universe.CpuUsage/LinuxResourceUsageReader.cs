using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Universe.CpuUsage
{

    public class LinuxResourceUsageReader
    {
        
        public const int RESOURCE_USAGE_FIELDS_COUNT = 18;

        public static bool IsSupported => _IsSupported.Value;
        public static CpuUsage? GetByScope(CpuUsageScope scope)
        {
            var s = scope == CpuUsageScope.Process ? LinuxResourceUsageInterop.RUSAGE_SELF : LinuxResourceUsageInterop.RUSAGE_THREAD; 
            return GetLinuxCpuUsageByScope(s);
        }
        
        public static CpuUsage? GetByProcess()
        {
            return GetLinuxCpuUsageByScope(LinuxResourceUsageInterop.RUSAGE_SELF);
        }

        // returns null on mac os x
        public static CpuUsage? GetByThread()
        {
            return GetLinuxCpuUsageByScope(LinuxResourceUsageInterop.RUSAGE_THREAD);
        }

        static Lazy<bool> _IsSupported = new Lazy<bool>(() =>
        {
            try
            {
                GetByScope(CpuUsageScope.Process);
                GetByScope(CpuUsageScope.Thread);
                return true;
            }
            catch
            {
                return false;
            }
        });

        internal static unsafe PosixResourceUsage? GetResourceUsageByScope(int scope)
        {
            if (IntPtr.Size == 4)
            {
                int* rawResourceUsage = stackalloc int[RESOURCE_USAGE_FIELDS_COUNT];
                int result = LinuxResourceUsageInterop.getrusage_heapless(scope, new IntPtr(rawResourceUsage));
                if (result != 0) return null;
                return new PosixResourceUsage()
                {
                    UserUsage = new TimeValue() {Seconds = *rawResourceUsage, MicroSeconds = rawResourceUsage[1]},
                    KernelUsage = new TimeValue() {Seconds = rawResourceUsage[2], MicroSeconds = rawResourceUsage[3]},
                    MaxRss = rawResourceUsage[4],
                    SoftPageFaults = rawResourceUsage[8],
                    HardPageFaults = rawResourceUsage[9],
                    Swaps = rawResourceUsage[10],
                    ReadOps = rawResourceUsage[11],
                    WriteOps = rawResourceUsage[12],
                    SentIpcMessages = rawResourceUsage[13],
                    ReceivedIpcMessages = rawResourceUsage[14],
                    ReceivedSignals = rawResourceUsage[15],
                    VoluntaryContextSwitches = rawResourceUsage[16],
                    InvoluntaryContextSwitches = rawResourceUsage[17],
                };
            }
            else
            {
                long* rawResourceUsage = stackalloc long[RESOURCE_USAGE_FIELDS_COUNT];
                int result = LinuxResourceUsageInterop.getrusage_heapless(scope, new IntPtr(rawResourceUsage));
                if (result != 0) return null;
                // microseconds are 4 bytes length on mac os and 8 bytes on linux
                return new PosixResourceUsage()
                {
                    UserUsage = new TimeValue() {Seconds = *rawResourceUsage, MicroSeconds = rawResourceUsage[1] & 0xFFFFFFFF},
                    KernelUsage = new TimeValue() {Seconds = rawResourceUsage[2], MicroSeconds = rawResourceUsage[3] & 0xFFFFFFFF},
                    MaxRss = rawResourceUsage[4],
                    SoftPageFaults = rawResourceUsage[8],
                    HardPageFaults = rawResourceUsage[9],
                    Swaps = rawResourceUsage[10],
                    ReadOps = rawResourceUsage[11],
                    WriteOps = rawResourceUsage[12],
                    SentIpcMessages = rawResourceUsage[13],
                    ReceivedIpcMessages = rawResourceUsage[14],
                    ReceivedSignals = rawResourceUsage[15],
                    VoluntaryContextSwitches = rawResourceUsage[16],
                    InvoluntaryContextSwitches = rawResourceUsage[17],
                };
            }
        }
        
        private static unsafe CpuUsage? GetLinuxCpuUsageByScope(int scope)
        {
            if (IntPtr.Size == 4)
            {
                int* rawResourceUsage = stackalloc int[RESOURCE_USAGE_FIELDS_COUNT];
                int result = LinuxResourceUsageInterop.getrusage_heapless(scope, new IntPtr(rawResourceUsage));
                if (result != 0) return null;
                return new CpuUsage()
                {
                    UserUsage = new TimeValue() {Seconds = *rawResourceUsage, MicroSeconds = rawResourceUsage[1]},
                    KernelUsage = new TimeValue() {Seconds = rawResourceUsage[2], MicroSeconds = rawResourceUsage[3]},
                };
            }
            else
            {
                long* rawResourceUsage = stackalloc long[RESOURCE_USAGE_FIELDS_COUNT];
                int result = LinuxResourceUsageInterop.getrusage_heapless(scope, new IntPtr(rawResourceUsage));
                if (result != 0) return null;
                // microseconds are 4 bytes length on mac os and 8 bytes on linux
                return new CpuUsage()
                {
                    UserUsage = new TimeValue() {Seconds = *rawResourceUsage, MicroSeconds = rawResourceUsage[1] & 0xFFFFFFFF},
                    KernelUsage = new TimeValue() {Seconds = rawResourceUsage[2], MicroSeconds = rawResourceUsage[3] & 0xFFFFFFFF},
                };
            }
        }
    }
    
    public class LinuxResourceUsageInterop
    {

        // For integration tests only
        public static IList GetRawUsageResources(int scope = RUSAGE_THREAD)
        {
            if (IntPtr.Size == 4)
            {
                RawLinuxResourceUsage_32 ret = new RawLinuxResourceUsage_32();
                ret.Raw = new int[18];
                int result = getrusage32(scope, ref ret);
                if (result != 0) return null;
                Console.WriteLine($"getrusage returns {result}");
                return ret.Raw;
            }
            else
            {
                RawLinuxResourceUsage_64 ret = new RawLinuxResourceUsage_64();
                ret.Raw = new long[18];
                int result = getrusage64(scope, ref ret);
                if (result != 0) return null;
                Console.WriteLine($"getrusage returns {result}");
                return ret.Raw;
            }
        }

        public const int RUSAGE_SELF = 0;
        public const int RUSAGE_CHILDREN = -1;
        public const int RUSAGE_BOTH = -2;         /* sys_wait4() uses this */
        public const int RUSAGE_THREAD = 1;        /* only the calling thread */
        
        
        [DllImport("libc", SetLastError = true, EntryPoint = "getrusage")]
        public static extern int getrusage32(int who, ref RawLinuxResourceUsage_32 resourceUsage);
        [DllImport("libc", SetLastError = true, EntryPoint = "getrusage")]
        public static extern int getrusage32_heapless(int who, IntPtr resourceUsage);

        [DllImport("libc", SetLastError = true, EntryPoint = "getrusage")]
        public static extern int getrusage64(int who, ref RawLinuxResourceUsage_64 resourceUsage);
        [DllImport("libc", SetLastError = true, EntryPoint = "getrusage")]

        public static extern int getrusage64_heapless(int who, IntPtr resourceUsage);
        
        [DllImport("libc", SetLastError = true, EntryPoint = "getrusage")]
        public static extern int getrusage_heapless(int who, IntPtr resourceUsage);
    }

    // https://github.com/mono/mono/issues?utf8=%E2%9C%93&q=getrusage
    // https://elixir.bootlin.com/linux/v2.6.24/source/include/linux/time.h#L19
    // https://stackoverflow.com/questions/1468443/per-thread-cpu-statistics-in-linux
    // ! http://man7.org/linux/man-pages/man2/getrusage.2.html
    // 

    
    [StructLayout(LayoutKind.Sequential)] 
    public struct RawLinuxResourceUsage_64
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I8, SizeConst = 18)]
        public long[] Raw;
    }

    [StructLayout(LayoutKind.Sequential)] 
    public struct RawLinuxResourceUsage_32
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I4, SizeConst = 18)]
        public int[] Raw;
    }
}