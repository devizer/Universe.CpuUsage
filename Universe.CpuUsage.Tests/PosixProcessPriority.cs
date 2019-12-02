using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Universe.CpuUsage.Tests
{
    class PosixProcessPriority
    {
        public static int WAIT_FOR_TIMEOUT = 5000;
        public static void ApplyMyPriority(ProcessPriorityClass priority)
        {
            // AppVeyor: Windows - OK, linux: need sudo
            // Travis: OSX 10.10 & 10.14 - need sudo, Ubuntu - ?!
            bool isOk = SetMyProcessPriority(priority);
            if (!isOk)
            {
                Console.WriteLine(
                    $"WARNING! Process priority can not be changed. Permission is required");
            }

            if (priority == ProcessPriorityClass.AboveNormal)
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            else if (priority == ProcessPriorityClass.Normal)
                Thread.CurrentThread.Priority = ThreadPriority.Normal;
            else if (priority == ProcessPriorityClass.BelowNormal)
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            else if (priority == ProcessPriorityClass.High)
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            else if (priority == ProcessPriorityClass.RealTime)
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            else if (priority == ProcessPriorityClass.Idle)
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            else
                throw new NotSupportedException($"Priority {priority} is not supported for a thread");
        }

        // For consistency with build-in Process.GetCurrentProcess().PriorityClass
        // Niceness is the same as by Net Core and Mono
        private static readonly Dictionary<ProcessPriorityClass, int> map = new Dictionary<ProcessPriorityClass, int>
        {
            {ProcessPriorityClass.AboveNormal, -6},
            {ProcessPriorityClass.Normal, 0},
            {ProcessPriorityClass.BelowNormal, 10},
            {ProcessPriorityClass.High, -11},
            // Bad idea - process may be frozen
            {ProcessPriorityClass.Idle, 19},
            // Actually the is a separated API for RealTime priority management,
            // but it is not used by 
            {ProcessPriorityClass.RealTime, -19},
        };


        public static bool SetMyProcessPriority(ProcessPriorityClass priority)
        {
            

            try
            {
                Process.GetCurrentProcess().PriorityClass = priority;
                return true;
            }
            catch (Win32Exception)
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    // on windows we cant increase priority
                    return false;
                }

                // Either Linux, OSX, FreeBSD, etc - all is ok
                // the package name is bsdutils
                map.TryGetValue(priority, out var niceness);
                ProcessStartInfo si = new ProcessStartInfo("sudo", $"renice -n {niceness} -p {Process.GetCurrentProcess().Id}");
                // we need to mute output of both streams to console
                si.RedirectStandardError = true;
                si.RedirectStandardOutput = true;
                Process p;
                try
                {
                    p = Process.Start(si);
                }
                catch
                {
                    // Not a case for build servers
                    Debug.WriteLine("It seems sudo is not installed or current user is not authorized");
                    return false;
                }

                using (p)
                {
                    // sudo may require input of password
                    // 5 seconds if for ARM under a high load
                    bool isExited = p.WaitForExit(WAIT_FOR_TIMEOUT);
                    if (!isExited)
                    {
                        try
                        {
                            p.Kill();
                        }
                        catch
                        {
                        }
                    }

                    return p.ExitCode == 0;
                }
            }
        }

        public static int? GetNicenessOfCurrentProcess()
        {
            var pid = Process.GetCurrentProcess().Id.ToString();
            ProcessStartInfo si = new ProcessStartInfo("ps", $"ax ax -o pid,ni");
            // we need to mute output of both streams to console
            si.RedirectStandardError = true;
            si.RedirectStandardOutput = true;
            Process p = Process.Start(si);
            var rawOutput = p.StandardOutput.ReadToEnd();
            var rawRows = rawOutput.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawRow in rawRows)
            {
                var rawArr = rawRow.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (rawArr.Length >= 2 && rawArr[0] == pid)
                {
                    if (int.TryParse(rawArr[1], out var ret))
                        return ret;
                }
            }

            return null;
        }
        
    }
}