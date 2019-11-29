using System;
using NUnit.Framework;

namespace Universe.CpuUsage.MonoTests
{
    [TestFixture]
    public class FirstTest
    {
        [Test]
        public void Go()
        {
            CpuUsage? usage = CpuUsage.GetByProcess();
            TestContext.Progress.WriteLine($"Process's CPU Usage: {usage}");

            TestContext.Progress.WriteLine("CpuUsageReader Version: " + typeof(CpuUsage).Assembly.GetName().Version);

        }
    }
}