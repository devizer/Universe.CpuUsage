using System;
using System.Runtime.InteropServices;
using KernelManagementJam.ThreadInfo;

namespace KernelManagementJam.Tests
{
    public class WindowsCpuUsage
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetThreadTimes(IntPtr hThread, out long lpCreationTime,
            out long lpExitTime, out long lpKernelTime, out long lpUserTime);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentProcess();

        
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetProcessTimes(IntPtr hThread, out long lpCreationTime,
            out long lpExitTime, out long lpKernelTime, out long lpUserTime);

        public static bool GetThreadTimes(out long kernelMicroseconds, out long userMicroseconds)
        {
            long ignored;
            long kernel;
            long user;
            if (GetThreadTimes(GetCurrentThread(), out ignored, out ignored, out kernel, out user))
            {
                // Console.WriteLine($"kernel: {kernel}, user: {user}");
                kernelMicroseconds = kernel / 10L;
                userMicroseconds = user / 10L;
                return true;
            }
            else
            {
                kernelMicroseconds = -1;
                userMicroseconds = -1;
                return false;
            }

        }

        public static bool GetProcessTimes(out long kernelMicroseconds, out long userMicroseconds)
        {
            long ignored;
            long kernel;
            long user;
            if (GetProcessTimes(GetCurrentProcess(), out ignored, out ignored, out kernel, out user))
            {
                // Console.WriteLine($"kernel: {kernel}, user: {user}");
                kernelMicroseconds = kernel / 10L;
                userMicroseconds = user / 10L;
                return true;
            }
            else
            {
                kernelMicroseconds = -1;
                userMicroseconds = -1;
                return false;
            }

        }

        public static CpuUsage? Get(CpuUsageScope scope)
        {
            long kernelMicroseconds;
            long userMicroseconds;
            bool isOk;
            if (scope == CpuUsageScope.Thread)
                isOk = GetThreadTimes(out kernelMicroseconds, out userMicroseconds);
            else 
                isOk = GetProcessTimes(out kernelMicroseconds, out userMicroseconds);
            
            if (!isOk)
                return null;

            const long m = 1000000L;
            return new CpuUsage()
            {
                KernelUsage = new TimeValue() { Seconds = kernelMicroseconds / m, MicroSeconds = kernelMicroseconds % m},
                UserUsage = new TimeValue() { Seconds = userMicroseconds / m, MicroSeconds = userMicroseconds % m},
            };

        }
    }
}