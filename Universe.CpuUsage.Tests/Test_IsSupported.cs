using NUnit.Framework;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class Test_IsSupported
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

        
    }
}