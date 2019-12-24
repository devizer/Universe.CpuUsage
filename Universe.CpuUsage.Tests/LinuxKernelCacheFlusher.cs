using System;
using System.Diagnostics;
using System.IO;

namespace KernelManagementJam.Benchmarks
{
    
    public class LinuxKernelCacheFlusher
    {
        // 60 seconds is an acceptable timeout for a build server
        private static readonly int WriteBuffersFlushingTimout = 60000;
        private static readonly int ReadBuffersFlushTimeout = 60000;

        // skipping is used for integration tests only
        private static bool SkipFlushing => Environment.GetEnvironmentVariable("SKIP_FLUSHING") == "true";
        public static void Sync()
        {
            if (SkipFlushing) return;
            SyncWriteBuffer();
            FlushReadBuffers();
        }

        public static void SyncWriteBuffer()
        {
            StartAndIgnore("sync", "", WriteBuffersFlushingTimout);
        }

        public static void FlushReadBuffers()
        {
            bool isDropOk = false;
            try
            {
                File.WriteAllText("/proc/sys/vm/drop_caches", "3");
                // sudo on Linux: ok
                // non-sudo on Linux: fail
                // macOS - fail
                isDropOk = true;
            }
            catch
            {
            }

            if (!isDropOk)
            {
                StartAndIgnore("sudo", "sh -c \"echo 1 > /proc/sys/vm/drop_caches; purge;\"", ReadBuffersFlushTimeout);
            }
        }
        
        static int StartAndIgnore(string executableName, string args, int timeoutMilliseconds)
        {
            Stopwatch sw = Stopwatch.StartNew(); 
            string info = executableName == "sudo" ? args : executableName;
                
            try
            {
                ProcessStartInfo si = new ProcessStartInfo(executableName, args);
                if (string.IsNullOrEmpty(args)) si = new ProcessStartInfo(executableName);
                si.RedirectStandardError = true;
                si.RedirectStandardOutput = true;
                using (Process p = Process.Start(si))
                {
                    p.Start();
                    bool isOk = p.WaitForExit(timeoutMilliseconds);
                    if (!isOk)
                    {
                        Console.WriteLine($"Warning! Flush command [{info}] is not completed in {sw.ElapsedMilliseconds:n0} milliseconds");
                    }
#if DEBUG
                    if (isOk)
                    {
                        Console.WriteLine($"Info: Flush command [{info}] successfully finished in {sw.ElapsedMilliseconds:n0} milliseconds");
                    }
#endif
                    return p.ExitCode;
                }
            }
            catch(Exception ex)
            {
#if DEBUG
                Console.WriteLine($"Info: Process [{info}] failed in {sw.ElapsedMilliseconds:n0} milliseconds. {ex.GetType().Name} {ex.Message}");
#endif
                return -1;
            }
        }

    }
}