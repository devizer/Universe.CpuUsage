using System;
using System.Collections;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Tests;
using Universe;
using Universe.CpuUsage;
using Universe.CpuUsage.Interop;
using Universe.CpuUsage.Tests;

namespace KernelManagementJam.Tests
{
    // git pull; time dotnet test --filter LinuxResourcesUsage
    [TestFixture]
    public class LinuxResourcesUsage_Reader_Tests : NUnitTestsBase
    {
        [Test]
        public void Is_Supported()
        {
            Console.WriteLine($"LinuxResourceUsageReader.IsSupported: {LinuxResourceUsageReader.IsSupported}");

            bool expectedSupported = CrossInfo.ThePlatform == CrossInfo.Platform.Linux ||
                                     CrossInfo.ThePlatform == CrossInfo.Platform.MacOSX;
            
            if (expectedSupported && !LinuxResourceUsageReader.IsSupported)
                Assert.Fail("On Linux 2.6.26+ the value of LinuxResourceUsageReader.IsSupported should be true");
        }

        [Test]
        public void Grow_Usage()
        {
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows) return;
            
            CpuLoader.Run(1, 1, true);
            CpuUsageScope scope = CrossInfo.ThePlatform == CrossInfo.Platform.MacOSX
                ? CpuUsageScope.Process
                : CpuUsageScope.Thread;

            Console.WriteLine($"Supported scope: {scope}");
            LinuxResourceUsageReader.GetByScope(scope);
            var prev = LinuxResourceUsageReader.GetByScope(scope);
            for (int i = 0; i < 10; i++)
            {
                CpuLoader.Run(1, 1, true);
                CpuUsage? next = LinuxResourceUsageReader.GetByScope(scope);
                Console.WriteLine($" {i} -> {next}");
                Assert.GreaterOrEqual(next.Value.KernelUsage.TotalMicroSeconds, prev.Value.KernelUsage.TotalMicroSeconds);
                Assert.GreaterOrEqual(next.Value.UserUsage.TotalMicroSeconds, prev.Value.UserUsage.TotalMicroSeconds);
                prev = next;
            }
        }
        
        [Test]
        public void Get_Process_Usage()
        {
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
            {
                Console.WriteLine($"LinuxResourceUsageReader is not supported on platform {CrossInfo.ThePlatform}");
                return;
            }
            
            var usage = LinuxResourceUsageReader.GetByProcess();
            Console.WriteLine($"LinuxResourceUsageReader.GetByProcess(): {usage}");
        }
        

        [Test]
        public void Get_Thread_Usage()
        {
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
            {
                Console.WriteLine($"LinuxResourceUsageReader is not supported on platform {CrossInfo.ThePlatform}");
                return;
            }
            
            var usage = LinuxResourceUsageReader.GetByThread();
            Console.WriteLine($"LinuxResourceUsageReader.GetByThread(): {usage}");
        }
        
        [Test]
        public void Show_Raw_Thread_Usage()
        {
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
            {
                Console.WriteLine($"LinuxResourceUsageReader is not supported on platform {CrossInfo.ThePlatform}");
                return;
            }
            
            var resources = LinuxResourceUsageInterop.GetRawUsageResources(LinuxResourceUsageInterop.RUSAGE_THREAD);
            Console.WriteLine($"GetRawUsageResources(RUSAGE_THREAD):{Environment.NewLine}{AsString(resources)}");
        }

        [Test]
        public void Show_Raw_Process_Usage()
        {
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
            {
                Console.WriteLine($"LinuxResourceUsageReader is not supported on platform {CrossInfo.ThePlatform}");
                return;
            }
            
            var resources = LinuxResourceUsageInterop.GetRawUsageResources(LinuxResourceUsageInterop.RUSAGE_SELF);
            Console.WriteLine($"GetRawUsageResources(RUSAGE_SELF):{Environment.NewLine}{AsString(resources)}");
        }
        
        static string AsString(IEnumerable arr)
        {
            if (arr == null) return "<null>";
            
            if (IntPtr.Size == 8 && CrossInfo.ThePlatform == CrossInfo.Platform.MacOSX)
            {
                var copy = arr.OfType<object>().ToArray();
                // microseconds on mac os are 4 bytes integers
                copy[1] = Convert.ToInt64(copy[1]) & 0xFFFFFFFF;
                copy[3] = Convert.ToInt64(copy[3]) & 0xFFFFFFFF;
                arr = copy;
            }
            
            const string rawNames = "u_sec u_usec k_sec k_usec maxrss ixrss idrss isrss minflt majflt nswap inblock oublock msgsnd msgrcv nsignals nvcsw nivcsw";
            var names = rawNames.Split(' ');
            
            var count = arr.OfType<object>().Count();
            StringBuilder b = new StringBuilder();
            var vLength = arr.OfType<object>().Select(x => Convert.ToString(x).Length).Max();
            int n = 0;
            foreach (var v in arr)
            {
                var comma = n + 1 < count ? "," : "";
                var name = n < names.Length ? (names[n] + " ") : "";
                name += n;
                name = name.PadLeft(11);
                b.AppendFormat("{0,-" + (11+1+vLength+2) + "}", $"{name}:{v}{comma}");
                n++;
                b.Append(" ");
                if (n % 4 == 0) b.Append(Environment.NewLine);
            }

            return b.ToString();
        }

        
    }
}