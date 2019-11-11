using System;
using System.Runtime.InteropServices;
using Universe.CpuUsage.Interop;

namespace Universe.CpuUsage
{
    public class MacOsThreadInfo
    {
        public static bool IsSupported => _IsSupported.Value;

        public static CpuUsage? GetByThread()
        {
            return Get();
        }

        static CpuUsage? Get()
        {
            int threadId = MacOsThreadInfoInterop.mach_thread_self();
            try
            {
                if (threadId == 0) return null;

                var ret = MacOsThreadInfoInterop.GetThreadInfo(threadId);
                return ret;
            }
            finally
            {
                // var arg1 = MacOsThreadInfoInterop.mach_thread_self(); // crash
                var arg1 = threadId; // waiting for test result
                int resDeallocate =
                    MacOsThreadInfoInterop.mach_port_deallocate(arg1, threadId);
            }

        }

        private static Lazy<bool> _IsSupported = new Lazy<bool>(() =>
        {
            try
            {
                GetByThread();
                return true;
            }
            catch
            {
                return false;
            }
        });

    }

    namespace Interop {
        public class MacOsThreadInfoInterop
        {
            private const int THREAD_BASIC_INFO_COUNT = 10;
            private const int THREAD_BASIC_INFO_SIZE = THREAD_BASIC_INFO_COUNT * 4;
            private const int THREAD_BASIC_INFO = 3;

            [DllImport("libc", SetLastError = false, EntryPoint = "mach_thread_self")]
            public static extern int mach_thread_self();

            // mach_port_deallocate
            [DllImport("libc", SetLastError = false, EntryPoint = "mach_port_deallocate")]
            public static extern int mach_port_deallocate(int threadId, int materializedThreadId);

            [DllImport("libc", SetLastError = true)]
            public static extern int thread_info(int threadId, int flavor, ref ThreadInfo info, ref int count);


            public class ThreadInfo
            {
                [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I4, SizeConst = THREAD_BASIC_INFO_COUNT)]
                public int[] Raw;
            }

            // Doesnt work
            public static int[] GetRawThreadInfo_Malformed(int threadId)
            {
                ThreadInfo info = new ThreadInfo() {Raw = new int[THREAD_BASIC_INFO_COUNT]};
                int count = THREAD_BASIC_INFO_SIZE;
                int result = thread_info(threadId, THREAD_BASIC_INFO, ref info, ref count);
                Console.WriteLine($"thread_info return value:${result}");
                return info.Raw;
            }

            [DllImport("libc", SetLastError = true, EntryPoint = "thread_info")]
            public static extern int thread_info_custom(int threadId, int flavor, IntPtr threadInfo, ref int count);

            public static unsafe CpuUsage? GetThreadInfo(int threadId)
            {
#if NETCOREAPP || NETSTANDARD
            int* ptr = stackalloc int[THREAD_BASIC_INFO_COUNT];
            {
                int count = THREAD_BASIC_INFO_SIZE;
                IntPtr threadInfo = new IntPtr(ptr);
                int result = thread_info_custom(threadId, THREAD_BASIC_INFO, threadInfo, ref count);
                if (result != 0) return null;
                return new CpuUsage()
                {
                    UserUsage = new TimeValue() {Seconds = *ptr, MicroSeconds = *(ptr+1)},
                    KernelUsage = new TimeValue() {Seconds = *(ptr+2), MicroSeconds = *(ptr+3)},
                };
            }
#else
                int[] raw = new int[THREAD_BASIC_INFO_COUNT];
                fixed (int* ptr = &raw[0])
                {
                    int count = THREAD_BASIC_INFO_SIZE;
                    IntPtr threadInfo = new IntPtr(ptr);
                    int result = thread_info_custom(threadId, THREAD_BASIC_INFO, threadInfo, ref count);
                    if (result != 0) return null;
                    return new CpuUsage()
                    {
                        UserUsage = new TimeValue() {Seconds = raw[0], MicroSeconds = raw[1]},
                        KernelUsage = new TimeValue() {Seconds = raw[2], MicroSeconds = raw[3]},
                    };
                }
#endif
            }

            public static unsafe int[] GetRawThreadInfo_ForTests(int threadId)
            {
                int[] raw = new int[THREAD_BASIC_INFO_COUNT];
                fixed (int* ptr = &raw[0])
                {
                    int count = THREAD_BASIC_INFO_SIZE;
                    IntPtr threadInfo = new IntPtr(ptr);
                    int result = thread_info_custom(threadId, THREAD_BASIC_INFO, threadInfo, ref count);
                    Console.WriteLine($"thread_info return value: {result}");
                    return raw;
                }
            }
        }

    }
}