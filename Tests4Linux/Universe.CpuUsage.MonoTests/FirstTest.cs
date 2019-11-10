using NUnit.Framework;

namespace Universe.CpuUsage.MonoTests
{
    [TestFixture]
    public class FirstTest
    {
        [Test]
        public void Go()
        {
            CpuUsage? usage = CpuUsageReader.GetByProcess();
            TestContext.Progress.WriteLine($"Process's CPU Usage: {usage}");

        }
    }
}