using System;
using NUnit.Framework;
using Tests;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class Test_IsSupported: NUnitTestsBase
    {

        [Test]
        public void OnWindows()
        {
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
            {
                Assert.IsTrue(WindowsCpuUsage.IsSupported);
            }
        }
        
        [Test]
        public void OnMacOS()
        {
            if (CrossInfo.ThePlatform == CrossInfo.Platform.MacOSX)
            {
                Assert.IsTrue(MacOsThreadInfo.IsSupported);
            }
        }

        [Test]
        public void OnLinux()
        {
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Linux)
            {
                Assert.IsTrue(LinuxResourceUsage.IsSupported);
            }
        }

        [Test]
        public void Show_Is_Supported()
        {
            Console.WriteLine($"CpuUsage.IsSupported ............... : {CpuUsage.IsSupported}");
            Console.WriteLine($"CpuUsageAsyncWatcher.IsSupported ... : {CpuUsageAsyncWatcher.IsSupported}");
            Console.WriteLine($"LinuxResourceUsage.IsSupported ..... : {LinuxResourceUsage.IsSupported}");
            Console.WriteLine($"MacOsThreadInfo.IsSupported ........ : {MacOsThreadInfo.IsSupported}");
            Console.WriteLine($"WindowsCpuUsage.IsSupported ........ : {WindowsCpuUsage.IsSupported}");
        }
        
    }
}