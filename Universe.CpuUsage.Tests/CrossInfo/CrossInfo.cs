#pragma warning disable CS0162

namespace Universe
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using SysGZip = System.IO.Compression;

    public class CrossFullInfo
    {

        public static String HumanReadableEnvironment(int intend)
        {
            string pre = intend > 0 ? new string(' ', intend) : string.Empty;
            long workingSet64 = Process.GetCurrentProcess().WorkingSet64;
            StringBuilder ret = new StringBuilder();
            var endianless = (BitConverter.IsLittleEndian ? "little-endian" : "big-endian");
            // Try(ret, delegate { return pre + "Platform .......... " + CrossInfo.ThePlatform + ", " + endianless; });
            // Try(ret, delegate { return pre + "Runtime ........... " + CrossInfo.RuntimeDisplayName; });
            Try(ret, delegate { return pre + "OS ................ " + OsDisplayName; });
            Try(ret, delegate { return pre + "CPU ............... " + ProcessorName; });
            Try(ret, delegate
            {
                var ws = workingSet64 == 0 ? "n/a" : ((workingSet64/1024L/1024).ToString("n0") + " Mb");
                var totalMem = TotalMemory == null ? "n/a" : string.Format("{0:n0} Mb", TotalMemory/1024);
                return pre + "Memory ............ " + totalMem + "; Working Set: " + ws;
            });

            return ret.ToString();
        }

        private delegate T Func<T>();
        static void Try(StringBuilder b, Func<string> action)
        {
            try
            {
                b.Append(action() + Environment.NewLine);
            }
            catch (Exception)
            {
            }
        }


        private static bool? _isSystemGZipSupported = null;
        static readonly object SyncGZipInfo = new object();
        static readonly string _GZipNotSupportedMessage = "System.IO.Compression.GZipStream is not supported.";

        static readonly StringComparison IgnoreCaseComparision = StringComparison.OrdinalIgnoreCase;

        public static bool IsSystemGZipSupported
        {
            get
            {
                if (!_isSystemGZipSupported.HasValue)
                    lock (SyncGZipInfo)
                        if (!_isSystemGZipSupported.HasValue)
                        {
                            // plain is {5,4,3,2,1}
                            byte[] gzipped = {
                                0x1f, 0x8b, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x63, 0x65,
                                0x61, 0x66, 0x62, 0x04, 0x00, 0x77, 0x03, 0xd7, 0xc6, 0x05, 0x00, 0x00, 0x00
                            };

                            try
                            {
                                MemoryStream mem = new MemoryStream(gzipped);
                                using (SysGZip.GZipStream s = new SysGZip.GZipStream(mem, SysGZip.CompressionMode.Decompress))
                                {
                                    byte[] plain = new byte[5 + 1];
                                    var n = s.Read(plain, 0, plain.Length);
                                    if (n != 5)
                                        throw new NotSupportedException(_GZipNotSupportedMessage);

                                    if (plain[0] != 5 || plain[1] != 4 || plain[2] != 3 || plain[3] != 2 || plain[4] != 1)
                                    {
                                        throw new NotSupportedException(_GZipNotSupportedMessage);
                                    }

                                    _isSystemGZipSupported = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                _isSystemGZipSupported = false;
                                // on mono without additional dlls exception is 'System.EntryPointNotFoundException: CreateZStream'
                                Trace_WriteLine(_GZipNotSupportedMessage + " [" + ex.GetType().Name + "] " + ex.Message);
                            }
                        }

                return _isSystemGZipSupported.Value;
            }
        }

        public static bool IsLinux
        {
            get
            {
                return ThePlatform == Platform.Linux;
            }
        }

        
        static Lazy<bool> _IsLinuxOnArm = new Lazy<bool>(() =>
        {
            if (!IsLinux) return false;
            string model;
            GetLinuxCpuInfo(out model);
            bool ret = model != null && model.ToUpper().Contains("ARM");
            if (ret)
                Trace_WriteLine("Workaround activated: Math.Round(decimal...) on ARM");

            return ret;
        });
        
        public static bool IsLinuxOnArm
        {
            get { return _IsLinuxOnArm.Value;  }
        }

#if NET20 || UNSAFE
        unsafe static int SizeOfPtr
        {
            get { return sizeof (IntPtr); }
        }

#elif NETCORE
        static int SizeOfPtr
        {
            get
            {
                var pa = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
                return (pa == System.Runtime.InteropServices.Architecture.Arm64
                        || pa == System.Runtime.InteropServices.Architecture.X64)
                    ? 8 : 4;
            }
        }
#else
        static int SizeOfPtr
        {
            get { return Environment.Is64BitOperatingSystem ? 8 : 4; }
        }
#endif

        static Lazy<string> _ProcessorName = new Lazy<string>(() =>
        {
            var ret = SizeOfPtr == 4 ? "32-bit" : "64-bit";
            if (ThePlatform == Platform.Linux)
                ret = Linux_ProcName();

            else if (ThePlatform == Platform.MacOSX)
                ret = MacOs_ProcName();

            else if (ThePlatform == Platform.Windows)
                ret = Windows_ProcName();

            else if (ThePlatform == Platform.FreeBSD)
                ret = FreeBSD_ProcName();

            var cores = Environment.ProcessorCount;
            return ret 
                + (cores > 1 ? ", " + cores + " Cores" : ", Single Core");
        });

        public static string ProcessorName
        {
            get { return _ProcessorName.Value; }
        }

        private static readonly Lazy<int?> _TotalMemory = new Lazy<int?>(() =>
        {
            if (ThePlatform == Platform.Linux)
                return Linux_TotalMemory();

            if (ThePlatform == Platform.MacOSX)
                return MacOs_TotalMemory();

            if (ThePlatform == Platform.Windows)
                return Windows_TotalMemory();

            if (ThePlatform == Platform.FreeBSD)
                return FreeBSD_TotalMemory();

            return null;
        });

        public static int? TotalMemory
        {
            get { return _TotalMemory.Value; }
        }

        public static decimal Round(decimal arg, int digits)
        {
            if (!IsLinuxOnArm) return Math.Round(arg, digits);

            long d = 10;
            switch (digits)
            {
                case 2:
                    d = 100;
                    break;
                case 3:
                    d = 1000;
                    break;
                case 4:
                    d = 10000;
                    break;
                case 5:
                    d = 100000;
                    break;
                case 6:
                    d = 1000000;
                    break;

                default:
                    for (int i = 0; i < digits; i++) d = d*10;
                    break;
            }

            // return ((long)(d * arg)) / (decimal)d;
            return Math.Floor(arg*d + 0.5m)/d;
        }

        static Lazy<bool> _IsMono = new Lazy<bool>(() =>
        {
            return Type.GetType("Mono.Runtime", false) != null;
        });

        public static bool IsMono
        {
            get { return _IsMono.Value; }
        }

        private static void GetLinuxCpuInfo(out string model)
        {
            model = Linux_ProcName();
            return;

#if !NETCORE
            model = null;
            string procCpuinfo = "/proc/cpuinfo";
            if (!File.Exists(procCpuinfo)) return;
            foreach (var s in EnumLines(procCpuinfo))
            {
                string key;
                string value;
                TrySplit(s, ':', out key, out value);
                if (StringComparer.InvariantCultureIgnoreCase.Equals("model name", (key ?? "").Trim()))
                {
                    if (value.Length > 0)
                    {
                        model = value;
                        return;
                    }
                }
            }
#endif
        }

        public static void HiddenExec
            (
            string command, string args, 
            out string output, out Exception outputException,
            out string error, out Exception errorException,
            out int exitCode
            )
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

            Process p = new Process()
            {
                StartInfo = si,
            };

            // error is written to output xml
            ManualResetEvent outputDone = new ManualResetEvent(false);
            ManualResetEvent errorDone = new ManualResetEvent(false);

            string my_output = null;
            string my_error = null;
            Exception my_outputException = null;
            Exception my_errorException = null;

            Thread t1 = new Thread(() =>
            {
                try
                {
                    my_error = p.StandardError.ReadToEnd();
                    // my_error = DumpToEnd(p.StandardError).ToString();
                }
                catch (Exception ex)
                {
                    my_errorException = ex;
                }
                finally
                {
                    errorDone.Set();
                }
            }
#if !NETCORE
            , 64 * 1024
#endif
            ) { IsBackground = true };

            Thread t2 = new Thread(() =>
            {
                try
                {
                    my_output = p.StandardOutput.ReadToEnd();
                    // my_output = DumpToEnd(p.StandardOutput).ToString();  
                }
                catch (Exception ex)
                {
                    my_outputException = ex;
                }
                finally
                {
                    outputDone.Set();
                }
            }
#if !NETCORE
            , 64 * 1024
#endif
            )
            { IsBackground = true };

            using (p)
            {
                p.Start();
                t2.Start();
                t1.Start();
                errorDone.WaitOne();
                outputDone.WaitOne();
                p.WaitForExit();
                exitCode = p.ExitCode;
            }

            output = my_output;
            error = my_error;
            outputException = my_outputException;
            errorException = my_errorException;

        }

        private static StringBuilder DumpToEnd(StreamReader rdr)
        {
            StringBuilder b = new StringBuilder();
            while (true)
            {
                string s = rdr.ReadLine();
                if (s == null)
                    break;

                // Console.WriteLine(s);
                b.AppendLine(s);
            }
            return b;
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

        public static void HiddenExec(string command, string args, string input, out string output, out int exitCode)
        {

            ProcessStartInfo si = new ProcessStartInfo(command, args)
            {
                CreateNoWindow = true,
                RedirectStandardError = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                // StandardErrorEncoding = Encoding.UTF8,
#if NETCORE
                StandardOutputEncoding = Encoding.UTF8,
#endif
                // WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
            };


            Process p = new Process() { StartInfo = si };
            using (p)
            {
                p.Start();
                ManualResetEvent written = new ManualResetEvent(false);
                Exception ex1 = null;
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        p.StandardInput.Write(input);
                        p.StandardInput.Dispose();
                    }
                    catch (Exception ex)
                    {
                        ex1 = ex;
                    }
                    written.Set();
                });

                string o = null;
                ManualResetEvent readed = new ManualResetEvent(false);
                Exception ex2 = null;
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        o = p.StandardOutput.ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                        ex2 = ex;
                    }
                    readed.Set();
                });
                WaitHandle.WaitAll(new WaitHandle[] {readed, written,});
                output = o;
                if (ex1 != null) throw new InvalidOperationException("Unable to write into pipe of a " + command);
                if (ex2 != null) throw new InvalidOperationException("Unable to read output of a " + command);
                p.WaitForExit();
                exitCode = p.ExitCode;
            }
        }


        // -s: Linux | Darwin
        static string ExecUName(string arg)
        {
            string ret;
            int exitCode;
            HiddenExec("uname", arg, out ret, out exitCode);
            if (ret != null)
                ret = ret.Trim('\r', '\n', '\t', ' ');

            return exitCode == 0 ? ret : ret;
        }

        static string ExecSysCtl(string arg)
        {
            string ret;
            int exitCode;
            HiddenExec("sysctl", arg, out ret, out exitCode);
            if (ret != null)
                ret = ret.Trim("\r\n\t ".ToCharArray());

            return exitCode == 0 ? ret : ret;
        }

#if !NETCORE
        static List<T> ExecWmi<T>(string query, string propertyName)
        {
            List<T> ret = new List<T>();
            string typeName = string.Format(
                    "System.Management.ManagementObjectSearcher, System.Management, Version={0}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                    Environment.Version.Major);

            Type t = Type.GetType(typeName, false);
            if (t == null)
                return ret;

            var search = Activator.CreateInstance(t, query);
            var resultSet = search.GetType().GetMethod("Get", new Type[0]).Invoke(search, new object[0]);
            var list = resultSet as IEnumerable;
            foreach (var item in list)
            {
                var val = item.GetType().GetMethod("get_Item").Invoke(item, new object[] { propertyName });
                if (val != null)
                {
                    var otherVal = TypeDescriptor.GetConverter(val).ConvertTo(val, typeof(T));
                    ret.Add((T)otherVal);
                }
            }

            if (resultSet is IDisposable)
                ((IDisposable)resultSet).Dispose();

            return ret;
        }
#endif


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
#if NETCORE
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
            var sName = ExecUName("-s");
            if ("Darwin".Equals(sName, IgnoreCaseComparision))
                return Platform.MacOSX;
            else if ("FreeBSD".Equals(sName, IgnoreCaseComparision))
                return Platform.FreeBSD;
            else
                return Platform.Linux;
        }

        public static Platform ThePlatform
        {
            get { return _Platform.Value; }
        }

        private static string MacOs_ProcName()
        {
            var name = StripDoubleWhitespace(ExecSysCtl("-n machdep.cpu.brand_string") ?? "");
            var cache = ExecSysCtl("-n machdep.cpu.cache.size");
            return name
                + (cache != null && cache.Trim() != "" ? "; Cache " + cache.Trim() : "");
        }

        private static string FreeBSD_ProcName()
        {
            var name = StripDoubleWhitespace(ExecSysCtl("-n hw.model") ?? "");
            if (string.IsNullOrEmpty(name))
                StripDoubleWhitespace(ExecSysCtl("-n hw.machine") ?? "");

            return name;
        }

        private static string FreeBSD_OsName()
        {
            var name = StripDoubleWhitespace(ExecSysCtl("-n kern.version") ?? "");
            if (string.IsNullOrEmpty(name))
            {
                var type = StripDoubleWhitespace(ExecSysCtl("-n kern.ostype") ?? "");
                var ver = StripDoubleWhitespace(ExecSysCtl("-n kern.osrelease") ?? "");
                name = (type + " " + ver).Trim();
                if (string.IsNullOrEmpty(name))
                    name = ThePlatform.ToString();
            }

            string rev = StripDoubleWhitespace(ExecSysCtl("-n kern.osrevision") ?? "");
            if (!string.IsNullOrEmpty(rev))
                name += " Rev " + rev;

            return name;
        }



        private static int? MacOs_TotalMemory()
        {
            var raw = ExecSysCtl("-n hw.memsize");
            long bytes;
            if (long.TryParse(raw, out bytes))
                return (int) (bytes/1024L);

            return null;
        }

        static int? FreeBSD_TotalMemory()
        {
            var raw = ExecSysCtl("-n hw.physmem");
            long bytes;
            if (long.TryParse(raw, out bytes))
                return (int)(bytes / 1024L);

            return null;
        }

        private static int? Linux_TotalMemory()
        {
            string fileName = "/proc/meminfo";
            if (!File.Exists(fileName))
                return null;

            string raw = null;
            var fileContent = File.ReadAllText(fileName);
            foreach (var line in EnumLines(new StringReader(fileContent)))
            // foreach (var line in EnumLines(fileName))
            {
                string key, value;
                TrySplit(line, ':', out key, out value);
                if (key != null)
                {
                    key = key.Trim();
                    if ("memtotal".Equals(key, IgnoreCaseComparision))
                        if (raw == null && value != null)
                            raw = value.ToLower().Trim();
                }
            }

            if (raw != null)
            {
                raw = raw.Trim(' ', 'k', 'b');
                int ret;
                if (int.TryParse(raw, out ret))
                    return ret;
            }

            return null;
        }

/*
ProductName:    Mac OS X
ProductVersion: 10.10.1
BuildVersion:   14B25
*/
    private static Dictionary<string,string> Exec_Sw_Vers()
        {
            Dictionary<string,string> ret = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            string output;
            int exitCode;
            HiddenExec("sw_vers", string.Empty, out output, out exitCode);
            if (!string.IsNullOrEmpty(output))
            {
                StringReader rdr = new StringReader(output);
                string s = null;
                while ((s = rdr.ReadLine()) != null)
                {
                    string key, value;
                    TrySplit(s, ':', out key, out value);
                    if (key != null && value != null)
                    {
                        key = key.Trim();
                        value = value.Trim();
                        ret[key] = value;
                    }
                    
                }
            }

            return ret;
        }

    private static string Linux_ProcName_Bit_Working_OnTravis_Ubuntu_12_04()
    {
        String name = null, name2 = null, cache = null, processor = null, hardware = null;
        string fileName = "/proc/cpuinfo";
        if (!File.Exists(fileName))
            return (ExecUName("-m") ?? "").Trim();

        StringComparison comp = IgnoreCaseComparision;
        foreach (var line in EnumLines(fileName, Encoding.ASCII))
        {
            string key, value;
            TrySplit(line, ':', out key, out value);
            if (key != null)
            {
                if ("model name".Equals(key, comp))
                    name = value;
                else if ("cpu model".Equals(key, comp))
                    name2 = value;
                else if ("Processor".Equals(key, comp))
                    processor = value;
                else if ("Hardware".Equals(key, comp))
                    hardware = value;

                else if ("cache size".Equals(key, comp))
                    cache = value;
            }
        }

        name = name ?? name2;
        if (string.IsNullOrEmpty(name))
            name = ExecUName("-m");

        if (string.IsNullOrEmpty(name))
            name = processor + (string.IsNullOrEmpty(hardware) ? "" : (", " + hardware));

        name = StripDoubleWhitespace(name.Trim());
        cache = (cache ?? "").Trim();

        return name + (cache.Length == 0 ? "" : (", Cache " + cache));
    }

    private static string Linux_ProcName()
    {
        String model_name = null, cpu_model = null, cache = null, processor = null, hardware = null;
        string fileName = "/proc/cpuinfo";
        if (!File.Exists(fileName))
            return (ExecUName("-m") ?? "").Trim();

        var comp = IgnoreCaseComparision;
        var procCpuInfo = File.ReadAllText("/proc/cpuinfo");
            
        foreach (var line in EnumLines(new StringReader(procCpuInfo)))
        {
            string key, value;
            TrySplit(line, ':', out key, out value);
            if (key != null)
            {
                key = key.Trim();
                if ("model name".Equals(key, comp))
                    model_name = value;
                else if ("cpu model".Equals(key, comp))
                    cpu_model = value;
                else if ("Processor".Equals(key, StringComparison.Ordinal))
                    processor = value;
                else if ("Hardware".Equals(key, StringComparison.Ordinal))
                    hardware = value;

                else if ("cache size".Equals(key, comp))
                    cache = value;
            }
        }

        model_name = model_name ?? cpu_model;

        if (string.IsNullOrEmpty(model_name) && !string.IsNullOrEmpty(processor))
            model_name = processor;

        model_name = model_name + (string.IsNullOrEmpty(hardware) ? "" : (( !string.IsNullOrEmpty(model_name) ? ", " : "") + hardware));

        if (string.IsNullOrEmpty(model_name))
            model_name = ExecUName("-m");

        model_name = StripDoubleWhitespace(model_name.Trim());
        cache = (cache ?? "").Trim();

        return model_name + (cache.Length == 0 ? "" : (", Cache " + cache));
    }

        private static string Windows_ProcName()
        {
#if NETCORE
            return WindowsSystemInfo.Default.ProcName;
#else
            // var d = WindowsSystemInfo.Default.AsStringDictionary;
            if (IsMono && ThePlatform == Platform.Windows)
                return SizeOfPtr == 4 ? "32-bit" : "64-bit";

            List<string> names = ExecWmi<string>("SELECT * FROM Win32_Processor", "Name");
            if (names.Count > 0)
            {
                var name = names[0];
                if (!string.IsNullOrEmpty(name))
                {
                    return
                        (names.Count > 1 ? names.Count + " * " : "")
                        + StripDoubleWhitespace(name);
                }
            }

            return SizeOfPtr == 4 ? "x86" : "x64";
#endif
        }

        static int? Windows_TotalMemory()
        {
#if NETCORE
            return WindowsSystemInfo.Default.TotalMemory;
#else
            if (IsMono && ThePlatform == Platform.Windows)
                return WindowsSystemInfo.Default.TotalMemory;

            List<long> mems = ExecWmi<long>("SELECT * FROM Win32_ComputerSystem", "TotalPhysicalMemory");
            foreach (var mem in mems)
                if (mem > 0)
                    return (int?) (mem/1024L);

            return null;
#endif
        }

        static string Windows_OsVersion()
        {
#if NETCORE
            var retCore = WindowsSystemInfo.Default.OsDescription;
            if (string.IsNullOrEmpty(retCore))
                retCore = RuntimeInformation.OSDescription;

            return retCore;
#else
            if (IsMono && ThePlatform == Platform.Windows)
            {
                var retMono = WindowsSystemInfo.Default.OsDescription;
                if (string.IsNullOrEmpty(retMono))
                    retMono = Environment.OSVersion.ToString();

                return retMono;
            }

            // OSArchitecture is unavailable on XP
            string[] names = new[] {"Caption", "CSDVersion", "OSArchitecture"};
            StringBuilder ret = new StringBuilder();
            foreach (var par in names)
            {
                try
                {
                    List<string> vals = ExecWmi<string>("SELECT * FROM Win32_OperatingSystem", par);
                    if (vals.Count > 0 && vals[0] != null && vals[0].Trim() != "")
                        ret.Append(ret.Length == 0 ? "" : ", ").Append(vals[0].Trim());
                }
                catch
                {
                }
            }

            return ret.ToString();
#endif
        }

        private static string MacOs_OsName()
        {

            string ver = "MAC OS X";
            var sw = Exec_Sw_Vers();
            string osxVersion;
            if (sw.TryGetValue("ProductVersion", out osxVersion) && !string.IsNullOrEmpty(osxVersion))
                ver = "MAC OS X " + osxVersion;
#if !NETCORE
            else
                ver = "MAC OS X v10." + (Environment.OSVersion.Version.Major - 4);
#endif

            string build;
            if (!sw.TryGetValue("BuildVersion", out build) || string.IsNullOrEmpty(build))
                build = ExecSysCtl("-n kern.osversion");
                      
            return
                ver
                + (!string.IsNullOrEmpty(build) ? (", Build " + build) : "")
                ;
        }

        private static string MacOS_AndLinux_AndFreeBSD_OsArch()
        {
            var arch = ExecUName("-m");
            return string.IsNullOrEmpty(arch) ? null : arch;
        }

        private static string Linux_OsName()
        {
            string firstLine = null;
            if (!Directory.Exists("/etc"))
                return 
                    "Linux "
                    + (SizeOfPtr == 4 ? "32-bit" : "64-bit");


            var files = new DirectoryInfo("/etc").GetFiles();
            foreach (FileInfo fileInfo in files)
            {
                if (fileInfo.FullName.ToLower().EndsWith("release"))
                {
                    var fileContent = File.ReadAllText(fileInfo.FullName);
                    // foreach (var line in EnumLines(fileInfo.FullName))
                    foreach (var line in EnumLines(new StringReader(fileContent)))
                    {
                        string key, value;
                        TrySplit(line, '=', out key, out value);
                        if ("pretty_name".Equals(key, IgnoreCaseComparision))
                        {
                            string pretty = value.Trim().Trim(new[] {'"'});
                            if (pretty.Length > 0)
                                return pretty;
                        }

                        // On SUSE, pretty_name=... is absent
                        if (line.IndexOf('=') == -1 && firstLine == null)
                            firstLine = line;
                    }
                }
            }

            return firstLine == null ? null : (firstLine);
        }

        static string StripDoubleWhitespace(string arg)
        {
            arg = (arg ?? "").Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
            while (arg.IndexOf("  ") > 0)
                arg = arg.Replace("  ", " ");

            return arg;
        }
        
        static Lazy<string> _OsDisplayName = new Lazy<string>(() =>
        {
            if (ThePlatform == Platform.Linux)
            {
                var arch = MacOS_AndLinux_AndFreeBSD_OsArch();
                var archSuffix = string.IsNullOrEmpty(arch) ? "" : " (" + arch + ")";
                var s1 = Linux_OsName();
                var s2 = ExecUName("-r");
                return s1 + (!string.IsNullOrEmpty(s2) ? (", Kernel " + s2) : "") + archSuffix;
            }

            else if (ThePlatform == Platform.MacOSX)
            {
                var arch = MacOS_AndLinux_AndFreeBSD_OsArch();
                var archSuffix = string.IsNullOrEmpty(arch) ? "" : " (" + arch + ")";
                return MacOs_OsName() + archSuffix;
            }

            else if (ThePlatform == Platform.FreeBSD)
            {
                var arch = MacOS_AndLinux_AndFreeBSD_OsArch();
                var archSuffix = string.IsNullOrEmpty(arch) ? "" : " (" + arch + ")";
                return FreeBSD_OsName() + archSuffix;
            }

            else
            {
                var ret = Windows_OsVersion();
                string genericOsName =
#if !NETCORE
                    Environment.OSVersion.VersionString;
#else
                    RuntimeInformation.OSDescription;
#endif

                return
                    string.IsNullOrEmpty(ret)
                        ? genericOsName
                        : ret;
            }

        });

        public static string OsDisplayName
        {
            get { return _OsDisplayName.Value; }
        }

        private static void TrySplit(string line, char separator, out string key, out string value)
        {
            key = value = null;
            int pos = line.IndexOf(separator);
            if (pos > 0)
            {
                key = line.Substring(0, pos);
                value = pos < line.Length - 1 ? line.Substring(pos + 1, line.Length - pos - 1) : "";
                key = key.Trim(' ').Trim('\t');
                value = value.Trim(' ').Trim('\t');
            }
        }

        static Lazy<string> _RuntimeDisplayName = new Lazy<string>(() =>
        {
#if NETCORE
            return RuntimeInformation.FrameworkDescription;
#else
            if (IsMono)
            {
                MethodInfo method = Type.GetType("Mono.Runtime", false).GetMethod("GetDisplayName", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.ExactBinding);
                if (method != null)
                    return 
                        "CLR " + Environment.Version + "; "
                        + "Mono " + method.Invoke(null, new object[0]);
            }

            return "Net " + Environment.Version;
#endif
        });

        public static string RuntimeDisplayName
        {
            get { return _RuntimeDisplayName.Value; }
        }


#region Iterator of lines by fileName, Stream, StreamReader or TextReader

        static readonly Encoding EnumLines_UTF8 = new UTF8Encoding(false);

        static IEnumerable<string> EnumLines(string fileName)
        {
            return EnumLines(fileName, EnumLines_UTF8);
        }

        static IEnumerable<string> EnumLines(string fileName, Encoding encoding)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader rdr = new StreamReader(fs, encoding))
            {
                return EnumLines(rdr);
            }
        }

        static IEnumerable<string> EnumLines(Stream stream, Encoding encoding)
        {
            using (StreamReader rdr = new StreamReader(stream, encoding))
            {
                return EnumLines(rdr);
            }
        }

        static IEnumerable<string> EnumLines(Stream stream)
        {
            return EnumLines(stream, EnumLines_UTF8);
        }

        static IEnumerable<string> EnumLines(StreamReader input)
        {
            string s;
            while ((s = input.ReadLine()) != null)
                yield return s;
        }

        static IEnumerable<string> EnumLines(TextReader input)
        {
            string line;
            while ((line = input.ReadLine()) != null)
                yield return line;
        }

#endregion


        private class Lazy<T>
        {
            public delegate T Function();

            private Function _Function;

            public Lazy(Function function)
            {
                _Function = function;
            }

            private T _Value;
            private /*static*/ readonly object Sync = new object();
            private bool _Ready = false;

            public T Value
            {
                get
                {
                    if (!_Ready)
                        lock(Sync)
                            if (!_Ready)
                            {
                                _Value = _Function();
                                _Ready = true;
                            }

                    return _Value;
                }
            }
        }

        public static void AttachUnitTrace(string consoleCaption)
        {
            if ("true".Equals(Environment.GetEnvironmentVariable("TRAVIS")))
                goto skipChangeTitle;

            _ConsoleTitle = consoleCaption;
            try
            {
                bool isFirst = _OriginalConsoleTitle == null;
                if (isFirst) 
                    _OriginalConsoleTitle = Console.Title;

                Console.Title = consoleCaption;
#if !NETCORE
                if (isFirst)
                    AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
                    {
                        try
                        {
                            Console.Title = _OriginalConsoleTitle;
                        }
                        catch
                        {
                        }
                    };
#endif
            }
            catch (Exception)
            {
            }

            skipChangeTitle:
            AttachUnitTrace();
        }

        public static void NextTest()
        {
            AttachUnitTrace();
            var i = Interlocked.Increment(ref _TestIndex);
            try
            {
                Console.Title = i + ":" + (_ConsoleTitle ?? "Tests");
            }
            catch (Exception)
            {
            }
        }

        private static string _ConsoleTitle, _OriginalConsoleTitle;
        private static int _TestIndex;
        private static bool _UnitTraceAttached;
        static readonly object SyncAttachUnitTrace = new object();

        public static void AttachUnitTrace()
        {
#if !NETCORE
            lock (SyncAttachUnitTrace)
            {
                if (!HasDefaultTraceListener())
                {
                    Trace.Listeners.Add(new DefaultTraceListener());
                }

                bool isNunitConsoleOnWindows = false;
                if (ThePlatform == Platform.Windows)
                    if (Process.GetCurrentProcess().ProcessName == "nunit-agent")
                    {
                        try
                        {
                            var c = Console.ForegroundColor;
                            Console.ForegroundColor = c == ConsoleColor.White ? ConsoleColor.Gray : ConsoleColor.White;
                            Console.ForegroundColor = c;
                            isNunitConsoleOnWindows = true;
                        }
                        catch (Exception)
                        {
                        }
                    }

                

                if (IsMono || Environment.OSVersion.Platform == PlatformID.Unix || isNunitConsoleOnWindows)
                {
                    // mac os x is also goes here
                    ConfigConsoleOnUnixTraceListener();
                }

                if (!_UnitTraceAttached)
                {
                    _UnitTraceAttached = true;
                    IsLinuxOnArm.ToString();
                    Trace.WriteLine(Environment.NewLine + HumanReadableEnvironment(3) + Environment.NewLine);
                }
            }
#endif
        }

        static bool HasDefaultTraceListener()
        {
#if !NETCORE
            foreach (var listener in Trace.Listeners)
                if (listener is DefaultTraceListener)
                    return true;
#endif
            return false;
        }

        private static void ConfigConsoleOnUnixTraceListener()
        {
#if !NETCORE
            string consoleUnix = "Console@Unix";
            bool isPresent = false;
            foreach (var listener in Trace.Listeners)
            {
                TextWriterTraceListener l = listener as TextWriterTraceListener;
                if (l != null && l.Name == consoleUnix)
                    isPresent = true;
            }

            if (!isPresent)
            {
                TextWriterTraceListener tl = new TextWriterTraceListener(Console.Out, consoleUnix);
                Trace.Listeners.Add(tl);
            }
#endif
        }

        public static List<ProcessInfo> GetProcessInfoByName(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            var c = IgnoreCaseComparision;
            List<ProcessInfo> ret = new List<ProcessInfo>();
            if (ThePlatform == Platform.Windows)
            {
                var all = Process.GetProcesses();
                foreach (var process in all)
                {
                    if (name.Equals(process.ProcessName, c))
                        ret.Add(new ProcessInfo()
                        {
                            Id = process.Id,
                            Rss = process.WorkingSet64 / 1024L ,
                            Vsz = process.PagedMemorySize64 / 1024L 
                        });
                }
            }

            else if (ThePlatform == Platform.Linux)
            {
                string pidsRaw;
                int exitCode;
                HiddenExec("pidof", '"' + name + '"', out pidsRaw, out exitCode);
                if (pidsRaw != null)
                    pidsRaw = pidsRaw.Trim('\n', '\t', '\r', ' ');

                if (!string.IsNullOrEmpty(pidsRaw))
                {
                    string[] arr = pidsRaw.Split(' ');
                    List<int> pids = new List<int>();
                    foreach (var s in arr)
                    {
                        int i;
                        if (int.TryParse(s, out i))
                            pids.Add(i);
                    }

                    ret.AddRange(Exec_Ps(pids));
                }
            }

            else if (ThePlatform == Platform.MacOSX)
            {
                var all = Exec_Ps_Axc();
                List<int> pids = new List<int>();
                foreach (var proc1Info in all)
                {
                    if (name.Equals(proc1Info.Name, c))
                        pids.Add(proc1Info.Id);
                }

                ret.AddRange(Exec_Ps(pids));
            }


            return ret;
        }

        private static List<ProcessInfo> Exec_Ps(IEnumerable<int> pidList)
        {
            List<ProcessInfo> ret = new List<ProcessInfo>();
            StringBuilder pids = new StringBuilder();
            foreach (var i in pidList)
                pids.Append(pids.Length == 0 ? "" : ",").Append(i.ToString("0"));

            string arg = string.Format("-p {0} -o pid,rss,vsz", pids);
            string psOutput;
            int psExitCode;
            HiddenExec("ps", arg, out psOutput, out psExitCode);
            if (psOutput != null)
            {
                StringReader rdr = new StringReader(psOutput);
                string psLine;
                while ((psLine = rdr.ReadLine()) != null)
                {
                    string[] psArr = psLine.Split(' ');
                    List<int> values = new List<int>();
                    foreach (var s in psArr)
                    {
                        int intVal;
                        if (int.TryParse(s, out intVal))
                            values.Add(intVal);
                    }

                    if (values.Count == 3)
                    {
                        ret.Add(new ProcessInfo()
                        {
                            Id = values[0],
                            Rss = values[1],
                            Vsz = values[2],
                        });
                    }
                }
            }

            return ret;
        }

        
        private class Proc1Info
        {
            public int Id;
            public string Name;
        }

        static List<Proc1Info> Exec_Ps_Axc()
        {
            List<Proc1Info> ret = new List<Proc1Info>();
            string output;
            int exitCode;
            HiddenExec("ps", "axc", out output, out exitCode);
            if (output != null)
            {
                StringReader rdr = new StringReader(output);
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    List<string> words = new List<string>();
                    string[] arr = line.Split(' ', '\t');
                    foreach (var s in arr)
                    {
                        if (s.Trim().Length > 0)
                        {
                            words.Add(s);
                        }
                    }

                    if (words.Count >= 5)
                    {
                        int id;
                        if (int.TryParse(words[0], out id))
                        {
                            ret.Add(new Proc1Info() { Id = id, Name = words[4] });
                        }
                    }
                }
            }

            return ret;
        }

        public class ProcessInfo
        {
            public int Id;
            public long Rss; // WorkingSet
            public long Vsz; // Paged
        }

        static Lazy<GdiPlusSupport> _GdiPlus = new Lazy<GdiPlusSupport>(() =>
        {
#if NETCORE
            return new GdiPlusSupport() {IsPresent = false};
#else
            var bitmapTypeName = string.Format(
                "System.Drawing.Bitmap, System.Drawing, Version={0}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                Environment.Version.Major);

            var imageFromatTypeName = string.Format(
                "System.Drawing.Imaging.ImageFormat, System.Drawing, Version={0}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                Environment.Version.Major);

            var bitmapType = Type.GetType(bitmapTypeName, false);
            var imageFromatType = Type.GetType(imageFromatTypeName, false);
            if (bitmapType == null || imageFromatType == null)
                return new GdiPlusSupport() {IsPresent = false};

            GdiPlusSupport ret = new GdiPlusSupport() {IsPresent = true};
            ret.Exif = ExifDiag.HasExifSupport;
            try
            {
                var bitmapCtor = bitmapType.GetConstructor(new Type[] {typeof (int), typeof (int),});
                var bmp = bitmapCtor.Invoke(new object[] {1, 1});

                var saveMethod = bitmapType.GetMethod("Save", new[] {typeof (Stream), imageFromatType});
                string[] formatNames = new[] {"Jpeg", "Gif", "Png", "Tiff"};

                foreach (var formatName in formatNames)
                {
                    try
                    {
                        var format = imageFromatType.GetProperty(formatName, BindingFlags.Static | BindingFlags.Public).GetValue(null, new object[0]);
                        MemoryStream mem = new MemoryStream();
                        saveMethod.Invoke(bmp, new object[] {mem, format});
                        // Console.WriteLine(formatName + ": " + mem.Length + " bytes");
                        ret.GetType().GetProperty(formatName).SetValue(ret, true, new object[0]);
                    }
                    catch
                    {
                    }
                }

                if (bmp is IDisposable)
                    ((IDisposable) bmp).Dispose();
            }
            catch
            {
            }

            return ret;
#endif
        });

        public static GdiPlusSupport GdiPlus
        {
            get { return _GdiPlus.Value.Clone(); }
        }

        public class GdiPlusSupport
        {
            public bool IsPresent { get; set; }

            // Image formats below are optional on mono
            public bool Jpeg { get; set; }
            public bool Png { get; set; }
            public bool Gif { get; set; }
            public bool Tiff { get; set; }
            public bool Exif { get; set; }

            internal GdiPlusSupport Clone()
            {
                return (GdiPlusSupport) this.MemberwiseClone();
            }

            public override string ToString()
            {
                if (!IsPresent)
                    return "Unavailable";

                List<string> codecs = new List<string>();
                List<string> nocodecs = new List<string>();
                if (Jpeg) codecs.Add("jpeg"); else nocodecs.Add("jpeg");
                if (Png) codecs.Add("png"); else nocodecs.Add("png");
                if (Gif) codecs.Add("gif"); else nocodecs.Add("gif");
                if (Tiff) codecs.Add("tiff"); else nocodecs.Add("tiff");
                if (Exif) codecs.Add("exif"); else nocodecs.Add("exif");

                return
                    "Available"
                    + (codecs.Count > 0 ? "; supported features are " + string.Join(", ", codecs.ToArray()) : "")
                    + (nocodecs.Count > 0 ? "; unsupported are " + string.Join(", ", nocodecs.ToArray()) : "");
            }
        }


#if !NETCORE
        class ExifDiag
        {
            // 271: Type=2, SAMPLE Camera 42\0
            private static bool? _HasExifSupport;
            static readonly object _Sync = new object();

            public static bool HasExifSupport
            {
                get
                {
                    lock (_Sync)
                        if (!_HasExifSupport.HasValue)
                            _HasExifSupport = HasExifSupport_Impl();

                    return _HasExifSupport.Value;
                }
            }

            private static bool HasExifSupport_Impl()
            {
                var bitmapTypeName = string.Format(
                    "System.Drawing.Bitmap, System.Drawing, Version={0}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                    Environment.Version.Major);

                MemoryStream stream;
                IDisposable bmp;
                try
                {
                    var bitmapType = Type.GetType(bitmapTypeName, false);
                    var bitmapCtor = bitmapType.GetConstructor(new Type[] { typeof(Stream), });
                    stream = TheImage;
                    bmp = bitmapCtor.Invoke(new object[] { stream, }) as IDisposable;
                }
                catch (Exception)
                {
                    return false;
                }

                using (stream)
                using (bmp)
                {
                    var items = GetPro(bmp, "PropertyItems") as ICollection;
                    foreach (object item in items)
                    {
                        try
                        {
                            var id = Convert.ToInt32(GetPro(item, "Id"));
                            var type = Convert.ToInt16(GetPro(item, "Type"));
                            var valueArr = GetPro(item, "Value") as byte[];
                            if (id == 271 && type == 2 && valueArr != null)
                            {
                                var camera = Encoding.UTF8.GetString(valueArr);
                                bool isIt = camera == "SAMPLE Camera 42\0";
                                if (isIt) return true;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                return false;

            }

            static object GetPro(object obj, string proName)
            {
                var pro = obj.GetType().GetProperty(proName);
                return pro.GetValue(obj, new object[0]);
            }

            private static MemoryStream TheImage
            {
                get
                {
                    MemoryStream mem = new MemoryStream();
                    StringBuilder b = new StringBuilder();
                    foreach (char c in RawStr)
                    {
                        if (c >= '0') b.Append(c);
                        if (b.Length == 2)
                        {
                            int i = int.Parse(b.ToString(), NumberStyles.HexNumber);
                            // Console.WriteLine("*** " + i);
                            mem.WriteByte((byte)i);
                            b.Length = 0;
                        }
                    }

                    mem.Position = 0;
                    return mem;
                }
            }

            private static readonly string RawStr = @"
FFD8FFE100CD45786966000049492A000800000006000F0102001100000056000000120103000100
000001000000310102001C0000006700000032010200140000008300000013020300010000000100
00006987040001000000970000000000000053414D504C452043616D657261203432004143442053
797374656D73204469676974616C20496D6167696E6700323031353A31313A31392031303A32393A
313000030090920200040000003436390002A00400010000000100000003A0040001000000010000
000000000000030000FFE11374687474703A2F2F6E732E61646F62652E636F6D2F7861702F312E30
2F003C3F787061636B657420626567696E3D22EFBBBF222069643D2257354D304D7043656869487A
7265537A4E54637A6B633964223F3E0A3C783A786D706D65746120786D6C6E733A783D2261646F62
653A6E733A6D6574612F2220783A786D70746B3D225075626C696320584D5020546F6F6C6B697420
436F726520332E35223E0A203C7264663A52444620786D6C6E733A7264663D22687474703A2F2F77
77772E77332E6F72672F313939392F30322F32322D7264662D73796E7461782D6E7323223E0A2020
3C7264663A4465736372697074696F6E207264663A61626F75743D22220A20202020786D6C6E733A
746966663D22687474703A2F2F6E732E61646F62652E636F6D2F746966662F312E302F223E0A2020
203C746966663A4D616B653E53414D504C452043616D6572612034323C2F746966663A4D616B653E
0A2020203C746966663A5943624372506F736974696F6E696E673E313C2F746966663A5943624372
506F736974696F6E696E673E0A2020203C746966663A4F7269656E746174696F6E3E313C2F746966
663A4F7269656E746174696F6E3E0A20203C2F7264663A4465736372697074696F6E3E0A20203C72
64663A4465736372697074696F6E207264663A61626F75743D22220A20202020786D6C6E733A6578
69663D22687474703A2F2F6E732E61646F62652E636F6D2F657869662F312E302F223E0A2020203C
657869663A506978656C5944696D656E73696F6E3E313C2F657869663A506978656C5944696D656E
73696F6E3E0A2020203C657869663A506978656C5844696D656E73696F6E3E313C2F657869663A50
6978656C5844696D656E73696F6E3E0A20203C2F7264663A4465736372697074696F6E3E0A20203C
7264663A4465736372697074696F6E207264663A61626F75743D22220A20202020786D6C6E733A78
61703D22687474703A2F2F6E732E61646F62652E636F6D2F7861702F312E302F223E0A2020203C78
61703A43726561746F72546F6F6C3E4143442053797374656D73204469676974616C20496D616769
6E673C2F7861703A43726561746F72546F6F6C3E0A2020203C7861703A4D6F64696679446174653E
323031352D31312D31395431303A32393A31302E3436392B323A30303C2F7861703A4D6F64696679
446174653E0A20203C2F7264663A4465736372697074696F6E3E0A203C2F7264663A5244463E0A3C
2F783A786D706D6574613E0A20202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020200A20202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
202020202020202020202020200A2020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
202020202020202020202020202020202020202020202020202020202020202020200A2020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020202020202020202020202020200A202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020202020202020202020202020202020202020202020202020202020202020202020200A202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020200A20202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020200A20
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
202020202020202020202020202020202020200A2020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
0A202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020202020202020202020202020202020202020200A202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20200A20202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020200A20202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
202020200A2020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
202020202020202020202020202020202020202020202020200A2020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020202020200A202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020202020202020202020202020202020202020202020202020200A202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020200A20202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020200A20202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
202020202020202020200A2020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
202020202020202020202020202020202020202020202020202020202020200A2020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020202020202020202020200A202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020202020202020202020202020202020202020202020202020202020202020200A202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020200A20202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020200A20202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
202020202020202020202020202020200A2020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
202020202020202020202020202020202020202020202020202020202020202020202020200A2020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020202020202020202020202020202020200A202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020202020202020202020202020202020202020202020202020202020202020202020202020200A
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020200A20202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
200A2020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
202020202020202020202020202020202020202020200A2020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020200A202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020202020202020202020202020202020202020202020200A202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020200A20202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020200A20202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
202020202020200A2020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
202020202020202020202020202020202020202020202020202020200A2020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020202020202020200A202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
2020202020202020202020202020202020202020202020202020202020200A202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020202020202020202020202020202020202020202020202020202020202020
20202020202020202020200A3C3F787061636B657420656E643D2277223F3EFFDB00430002010101
01010201010102020202030503030202030604040305070607070706070608090B0908080A080607
0A0D0A0A0B0C0C0D0C07090E0F0E0C0F0B0C0C0CFFDB00430103030304030408040408120C0A0C12
12121212121212121212121212121212121212121212121212121212121212121212121212121212
121212121212121212FFC00011080001000103012100021101031101FFC4001F0000010501010101
010100000000000000000102030405060708090A0BFFC400B5100002010303020403050504040000
017D01020300041105122131410613516107227114328191A1082342B1C11552D1F0243362728209
0A161718191A25262728292A3435363738393A434445464748494A535455565758595A6364656667
68696A737475767778797A838485868788898A92939495969798999AA2A3A4A5A6A7A8A9AAB2B3B4
B5B6B7B8B9BAC2C3C4C5C6C7C8C9CAD2D3D4D5D6D7D8D9DAE1E2E3E4E5E6E7E8E9EAF1F2F3F4F5F6
F7F8F9FAFFC4001F0100030101010101010101010000000000000102030405060708090A0BFFC400
B5110002010204040304070504040001027700010203110405213106124151076171132232810814
4291A1B1C109233352F0156272D10A162434E125F11718191A262728292A35363738393A43444546
4748494A535455565758595A636465666768696A737475767778797A82838485868788898A929394
95969798999AA2A3A4A5A6A7A8A9AAB2B3B4B5B6B7B8B9BAC2C3C4C5C6C7C8C9CAD2D3D4D5D6D7D8
D9DAE2E3E4E5E6E7E8E9EAF2F3F4F5F6F7F8F9FAFFDA000C03010002110311003F00F70A2BE8CF92
3FFFD9";


        }
#endif


        public class WindowsSystemInfo
        {
            private static readonly string Script = @"
Write-Host 'group-header: Processor'
Get-WmiObject -Class Win32_Processor | Format-List Name,L2CacheSize,L3CacheSize,MaxClockSpeed
Write-Host 'group-header: ComputerSystem'
Get-WmiObject -Class Win32_ComputerSystem | Format-List Manufacturer,Model,TotalPhysicalMemory,TotalVisibleMemorySize
Write-Host 'group-header: OperatingSystem'
Get-WmiObject -Class Win32_OperatingSystem | Format-List Caption,CSDVersion,Version,ServicePackMajorVersion,ServicePackMinorVersion,OSArchitecture,TotalVirtualMemorySize,TotalVisibleMemorySize,FreePhysicalMemory,FreeVirtualMemory,FreeSpaceInPagingFiles,SystemDrive,SystemDirectory
";

            public static readonly WindowsSystemInfo Default = new WindowsSystemInfo();
            private readonly object Sync = new object();
            private Dictionary<string, string> _AsStringDictionary;

            public string OsDescription
            {
                get
                {
                    var d = AsStringDictionary;
                    string caption;
                    string sp;
                    d.TryGetValue("OperatingSystem.Caption", out caption);
                    d.TryGetValue("OperatingSystem.CSDVersion", out sp);
                    return caption + (!string.IsNullOrEmpty(caption) && !string.IsNullOrEmpty(sp) ? ", " : "") + sp;
                }
            }

            public Dictionary<string, string> AsStringDictionary
            {
                get
                {
                    if (_AsStringDictionary == null)
                        lock (Sync)
                            if (_AsStringDictionary == null)
                            {
                                _AsStringDictionary = GetDictionary();
                                foreach (var key in _AsStringDictionary.Keys)
                                {
                                    // Console.WriteLine("[{0}]: '{1}'", key, _AsStringDictionary[key]);
                                }
                            }

                    return _AsStringDictionary;
                }
            }

            public int? TotalMemory
            {
                get
                {
                    string rawBytes;
                    AsStringDictionary.TryGetValue("OperatingSystem.TotalVisibleMemorySize", out rawBytes);
                    long ret;
                    Debugger.Break();
                    if (long.TryParse(rawBytes, out ret))
                        return (int?) ret;
                    else
                        return null;
                }
            }

            public string ProcName
            {
                get
                {
                    string name;
                    string l3Cache;
                    string l2Cache;
                    AsStringDictionary.TryGetValue("Processor.Name", out name);
                    name = name == null ? null : StripDoubleWhitespace(name);
                    AsStringDictionary.TryGetValue("Processor.L2CacheSize", out l2Cache);
                    AsStringDictionary.TryGetValue("Processor.L3CacheSize", out l3Cache);
                    if (l2Cache == "0") l2Cache = "";
                    if (l3Cache == "0") l3Cache = "";
                    string cache = l2Cache + (!string.IsNullOrEmpty(l3Cache) && !string.IsNullOrEmpty(l2Cache) ? "+" : "") + l3Cache;
                    var ret = name + (cache.Length > 0 ? ", Cache " + cache + " Mb" : "");
                    return ret;
                }
            }


            private Dictionary<string, string> GetDictionary()
            {
                string output;
                int code;
                HiddenExec("powershell.exe", "-OutputFormat Text -Command -", Script, out output, out code);
                StringReader rdr = new StringReader(output);
                string line;
                string group = null;
                Dictionary<string, string> ret = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
                while ((line = rdr.ReadLine()) != null)
                {
                    var p = line.IndexOf(":");
                    if (p < 0) continue;
                    string key = p > 0 ? line.Substring(0, p).Trim() : "";
                    string value = p < line.Length - 1 ? line.Substring(p + 1).Trim() : "";
                    if (string.IsNullOrEmpty(key)) continue;
                    if (key == "group-header")
                        group = value;
                    else
                        ret[group + "." + key] = value;
                }

                return ret;
            }
        }

        static void Trace_WriteLine(object message)
        {

#if NETCORE
            Debug.WriteLine(message);
#else
            Trace.WriteLine(message);
#endif
        }

    }


}

