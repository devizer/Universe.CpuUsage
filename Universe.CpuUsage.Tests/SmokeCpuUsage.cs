using System;
using NUnit.Framework;
using Tests;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class SmokeCpuUsage: NUnitTestsBase
    {

        [Test]
        public void Does_Not_Fail_ByThread()
        {
            CpuUsage? usage = CpuUsageReader.GetByThread();
            Console.WriteLine($"Thread's CPU Usage: {usage}");
        }

        [Test]
        public void Is_Not_Null_ByThread()
        {
            var usage = CpuUsageReader.GetByThread();
            Console.WriteLine($"Thread's CPU Usage: {usage}");
            Assert.IsTrue(usage.HasValue);
        }

        [Test]
        public void Does_Not_Fail_ByProcess()
        {
            CpuUsage? usage = CpuUsageReader.GetByProcess();
            Console.WriteLine($"Process's CPU Usage: {usage}");
        }

        [Test]
        public void Is_Not_Null_ByProcess()
        {
            var usage = CpuUsageReader.GetByProcess();
            Console.WriteLine($"Process's CPU Usage: {usage}");
            Assert.IsTrue(usage.HasValue);
        }
    }
    
}