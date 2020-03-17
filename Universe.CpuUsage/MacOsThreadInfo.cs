namespace Universe.CpuUsage
{
    using System;
    using System.Runtime.InteropServices;
    using Universe.CpuUsage.Interop;

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

                var ret = MacOsThreadInfoInterop.GetThreadCpuUsageInfo(threadId);
                return ret;
            }
            finally
            {
                int? self = null;
                int kResult2 = -424242;
                int kResult1 = MacOsThreadInfoInterop.mach_port_deallocate(threadId, threadId);

                // https://opensource.apple.com/source/xnu/xnu-792/osfmk/mach/kern_return.h
                // KERN_INVALID_TASK 16: target task isn't an active task.
                if (kResult1 != 0)
                {
                    self = MacOsThreadInfoInterop.mach_thread_self();
                    kResult2 = MacOsThreadInfoInterop.mach_port_deallocate(self.Value, threadId);
                }
#if DEBUG 
                if (kResult1 != 0 && kResult2 != 0)
                {
                    Console.WriteLine($@"{(kResult1 == 0 || kResult2 == 0 ? "Info" : "Warning!!!!!")} 
    mach_port_deallocate({threadId}, {threadId}) returned {kResult1}
    mach_port_deallocate(mach_thread_self() == {self}, {threadId}) returned {kResult2}");
                }
#endif
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

    namespace Interop
    {
        public class MacOsThreadInfoInterop
        {
            private const int THREAD_BASIC_INFO_FIELDS_COUNT = 10;
            private const int THREAD_BASIC_INFO_SIZE = THREAD_BASIC_INFO_FIELDS_COUNT * 4;
            private const int THREAD_BASIC_INFO = 3;

            [DllImport("libc", SetLastError = false, EntryPoint = "mach_thread_self")]
            public static extern int mach_thread_self();

            // mach_port_deallocate
            [DllImport("libc", SetLastError = false, EntryPoint = "mach_port_deallocate")]
            public static extern int mach_port_deallocate(int threadId, int materializedThreadId);

            [DllImport("libc", SetLastError = true, EntryPoint = "thread_info")]
            public static extern int thread_info_custom(int threadId, int flavor, IntPtr threadInfo, ref int count);

            public static unsafe CpuUsage? GetThreadCpuUsageInfo(int threadId)
            {
                int* ptr = stackalloc int[THREAD_BASIC_INFO_FIELDS_COUNT];
                {
                    int count = THREAD_BASIC_INFO_SIZE;
                    IntPtr threadInfo = new IntPtr(ptr);
                    int result = thread_info_custom(threadId, THREAD_BASIC_INFO, threadInfo, ref count);
                    if (result != 0) return null;
                    return new CpuUsage()
                    {
                        UserUsage = new TimeValue() {Seconds = *ptr, MicroSeconds = *(ptr + 1)},
                        KernelUsage = new TimeValue() {Seconds = *(ptr + 2), MicroSeconds = *(ptr + 3)},
                    };
                }
            }

            public static unsafe int[] GetRawThreadInfo_ForTests(int threadId)
            {
                int[] raw = new int[THREAD_BASIC_INFO_FIELDS_COUNT];
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