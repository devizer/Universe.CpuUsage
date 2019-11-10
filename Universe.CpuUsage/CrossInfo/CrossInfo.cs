namespace Universe
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    public class CrossInfo
    {

        public static Platform ThePlatform
        {
            get { return _Platform.Value; }
        }

        public enum Platform
        {
            Windows,
            Linux,
            MacOSX,
            FreeBSD,
            Unknown,
        }
        
        static Lazy<Platform> _Platform = new Lazy<Platform>(() =>
        {
#if NETCOREAPP || NETSTANDARD
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Platform.MacOSX;

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Platform.Windows;

            // Community ports Core to FreeBSD
            else
                return GetPlatform_OnLinux_OSX_BSD();
#else
            if (Environment.OSVersion.Platform == PlatformID.MacOSX)
                return Platform.MacOSX;

            else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                return Platform.Windows;

            else if (Environment.OSVersion.Platform == PlatformID.Unix)
                return GetPlatform_OnLinux_OSX_BSD();

            else
                return Platform.Unknown;
#endif
        });
        


        private static Platform GetPlatform_OnLinux_OSX_BSD()
        {
#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_6 
            return IsMacOsX() ? Platform.MacOSX : Platform.Linux;
#else
            var sName = ExecUName("-s");
            if ("Linux".Equals(sName, StringComparison.OrdinalIgnoreCase))
                return Platform.Linux;
            else if ("Darwin".Equals(sName, StringComparison.OrdinalIgnoreCase))
                return Platform.MacOSX;
            else
            {
                // BSD-like with linux compatibility layer
                return Platform.Linux;
            }
#endif
        }

        static bool IsMacOsX()
        {
            const string systemLibPath = "/usr/lib/libSystem.dylib";
            if (!File.Exists(systemLibPath)) return false;
            using (FileStream fs = new FileStream(systemLibPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] buffer = new byte[4];
                int n = fs.Read(buffer, 0, 4);
                if (n == 4)
                {
                    return buffer[0] == 0xCA && buffer[1] == 0xFE && buffer[2] == 0xBA && buffer[3] == 0xBE;
                }

                return false;
            }
        }

        // static readonly StringComparison IgnoreCaseComparision = ;

#if ! (NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_6)
        static string ExecUName(string arg)
        {
            string ret;
            int exitCode;
            HiddenExec("uname", arg, out ret, out exitCode);
            if (ret != null)
                ret = ret.Trim('\r', '\n', '\t', ' ');

            return exitCode == 0 ? ret : ret;
        }

        public static void HiddenExec(string command, string args, out string output, out int exitCode)
        {
                        
            ProcessStartInfo si = new ProcessStartInfo(command, args)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                // WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
            };


            Process p = new Process() {StartInfo = si};
            using (p)
            {
                p.Start();
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                exitCode = p.ExitCode;
            }
        }
#endif

        static void Trace_WriteLine(object message)
        {

#if  NETSTANDARD || NETCOREAPP
            Debug.WriteLine(message);
#else
            Trace.WriteLine(message);
#endif
        }

    }


}

