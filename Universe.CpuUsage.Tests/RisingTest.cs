using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using NUnit.Framework;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class RisingTest
    {
        [Test]
        public void Test_Thread()
        {
            Load(CpuUsageScope.Thread, 420);
        }

        [Test]
        public void Test_Process()
        {
            Load(CpuUsageScope.Process, 420);
        }

        void Load(CpuUsageScope scope, int milliseconds)
        {
            CpuUsage? prev = CpuUsageReader.Get(scope);
            Assert.IsTrue(prev.HasValue, "Prev should has value");
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds <= milliseconds)
            {
                EatSomeCpu();
                EatSomeMem();
            }
            
            CpuUsage? next = CpuUsageReader.Get(scope);
            double microSeconds = sw.ElapsedTicks * 1000000d / Stopwatch.Frequency; 
            Assert.IsTrue(next.HasValue, "Next should has value");
            

            var delta = CpuUsage.Substruct(next.Value, prev.Value);
            Assert.GreaterOrEqual(next.Value.KernelUsage.TotalMicroSeconds, prev.Value.KernelUsage.TotalMicroSeconds, "Kernel usage should be greater or equal zero");
            Assert.Greater(next.Value.UserUsage.TotalMicroSeconds, prev.Value.UserUsage.TotalMicroSeconds, "User usage should be greater or equal zero");

            string message = string.Format("Duration: {0:f3}, CPU: {1:f3} = {2}", 
                microSeconds/1000, 
                (delta.KernelUsage.MicroSeconds + delta.UserUsage.MicroSeconds)/1000d,
                delta);
            TestContext.Progress.WriteLine(message);
        }

        void EatSomeMem()
        {
            List<object> list = new List<object>();
            for (int i = 0; i < 10; i++)
                list.Add(new byte[10 * 1024 * 1024]);
        }

        void EatSomeCpu()
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 11) ;
        }

    }
}