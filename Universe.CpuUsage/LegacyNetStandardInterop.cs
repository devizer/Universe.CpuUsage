namespace Universe.CpuUsage
{
    using System;
    using System.Runtime.InteropServices;
    using Universe.CpuUsage.Interop;

    // for netstandard < v2.0 only 
    public class LegacyNetStandardInterop
    {
        public static bool IsSupported => _IsSupported.Value; 
        public static long GetThreadId()
        {
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Linux)
            {
                return pthread_self_onLinux().ToInt64();
            }
            
            else if (CrossInfo.ThePlatform == CrossInfo.Platform.MacOSX)
            {
                return MacOsThreadInfoInterop.mach_thread_self();
            }

            else if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
            {
                return WindowsCpuUsageInterop.GetCurrentThread().ToInt64();
            }

            throw new NotSupportedException($"Platform '{CrossInfo.ThePlatform}' is not supported");
        }

        [DllImport("libc", SetLastError = true, EntryPoint = "pthread_self")]
        static extern IntPtr pthread_self_onLinux();
        
        static Lazy<bool> _IsSupported => new Lazy<bool>(() => {
            try
            {
                return GetThreadId() != 0;
            }
            catch
            {
                return false;
            }

        });
        
    }
}