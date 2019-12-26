using System;
using Universe.CpuUsage.Interop;

namespace Universe.CpuUsage
{
    internal class CpuUsageReader
    {
        public static bool IsSupported => _IsSupported.Value;
        
        public static CpuUsage? GetByProcess()
        {
            return Get(CpuUsageScope.Process);
        }

        public static CpuUsage? GetByThread()
        {
            return Get(CpuUsageScope.Thread);
        }

        public static CpuUsage? SafeGet(CpuUsageScope scope)
        {
            try
            {
                return Get(scope);
            }
            catch
            {
                return null;
            }
        }
        
        public static CpuUsage? Get(CpuUsageScope scope)
        {
            var platform = CrossInfo.ThePlatform;
            if (scope == CpuUsageScope.Process)
            {
                if (platform == CrossInfo.Platform.Linux || platform == CrossInfo.Platform.MacOSX)
                    return LinuxResourceUsageReader.GetByProcess();
                else
                    return WindowsCpuUsage.Get(CpuUsageScope.Process);
            }

            if (platform == CrossInfo.Platform.Linux)
                return LinuxResourceUsageReader.GetByThread();
            
            else if (platform == CrossInfo.Platform.MacOSX)
                return MacOsThreadInfo.GetByThread();
            
            else if (platform == CrossInfo.Platform.Windows)
                // throw new NotImplementedException("CPU Usage in the scope of the thread is not yet implemented for Windows");
                return WindowsCpuUsage.Get(CpuUsageScope.Thread);
            
            throw new InvalidOperationException($"CPU usage in the scope of {scope} is a kind of an unknown on the {platform}");
        }

        private static readonly Lazy<bool> _IsSupported = new Lazy<bool>(() =>
        {
            try
            {
                GetByProcess();
                GetByThread();
                return true;
            }
            catch
            {
                return false;
            }
        });

    }
}