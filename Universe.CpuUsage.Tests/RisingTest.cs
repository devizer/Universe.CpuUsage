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
            for(int i=1; i<5; i++)
                Load(CpuUsageScope.Thread, 123);
        }

        [Test]
        public void Test_Process()
        {
            for(int i=1; i<5; i++)
                Load(CpuUsageScope.Process, 123);
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

            string message = string.Format("Duration: {0:f3}, CPU@{3}: {1:f3} = {2}", 
                microSeconds/1000, 
                (delta.KernelUsage.MicroSeconds + delta.UserUsage.MicroSeconds)/1000d,
                delta,
                scope);
            
            TestContext.Progress.WriteLine(message);
        }

        void EatSomeMem()
        {
            Stopwatch sw = Stopwatch.StartNew();
            List<object> list = new List<object>();
            for (int i = 0; i < 10 && sw.ElapsedMilliseconds < 1000; i++)
                list.Add(new byte[10 * 1024 * 1024]);
        }

        void EatSomeCpu()
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 11) ;
        }

    }
}