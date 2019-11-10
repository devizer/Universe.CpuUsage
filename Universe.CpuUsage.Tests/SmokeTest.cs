using KernelManagementJam.ThreadInfo;
using NUnit.Framework;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class SmokeTest
    {

        [Test]
        public void Does_Not_Fail_ByThread()
        {
            KernelManagementJam.ThreadInfo.CpuUsage? kernel = CpuUsageReader.GetByThread();
        }

        [Test]
        public void Is_Not_Null_ByThread()
        {
            Assert.IsTrue(CpuUsageReader.GetByThread().HasValue);
        }

        [Test]
        public void Does_Not_Fail_ByProcess()
        {
            KernelManagementJam.ThreadInfo.CpuUsage? kernel = CpuUsageReader.GetByProcess();
        }

        [Test]
        public void Is_Not_Null_ByProcess()
        {
            Assert.IsTrue(CpuUsageReader.GetByProcess().HasValue);
        }
    }
}