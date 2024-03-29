using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Tests;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class RisingTest: NUnitTestsBase
    {
        [Test]
        public void Test_Thread()
        {
            for(int i=1; i<=9; i++)
                Load(CpuUsageScope.Thread, 256);
        }

        [Test]
        public void Test_Process()
        {
            for(int i=1; i<=9; i++)
                Load(CpuUsageScope.Process, 256);
        }

        void Load(CpuUsageScope scope, int milliseconds)
        {
            CpuUsage? prev = CpuUsage.Get(scope);
            Assert.IsTrue(prev.HasValue, "Prev should has value");
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds <= milliseconds)
            {
                EatSomeCpu();
                EatSomeMem();
            }
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            CpuUsage? next = CpuUsage.Get(scope);
            double microSeconds = sw.ElapsedTicks * 1000000d / Stopwatch.Frequency; 
            Assert.IsTrue(next.HasValue, "Next should has value");
            

            var delta = CpuUsage.Substruct(next.Value, prev.Value);
            Assert.GreaterOrEqual(next.Value.KernelUsage.TotalMicroSeconds, prev.Value.KernelUsage.TotalMicroSeconds, "Kernel usage should be greater or equal");
            Assert.GreaterOrEqual(next.Value.UserUsage.TotalMicroSeconds, prev.Value.UserUsage.TotalMicroSeconds, "User usage should be greater or equal");

            string message = string.Format("Duration: {0:f3} of {4}, CPU@{3}: {1:f3} = {2}", 
                microSeconds/1000, 
                (delta.KernelUsage.MicroSeconds + delta.UserUsage.MicroSeconds)/1000d,
                delta,
                scope,
                milliseconds);
            
            Console.WriteLine(message);
        }

        void EatSomeMem()
        {
            Stopwatch sw = Stopwatch.StartNew();
            List<IntPtr> list = new List<IntPtr>();
            List<object> saved = new List<object>();

            for (int i = 0; i < 100 && sw.ElapsedMilliseconds < 1000; i++)
            {
                list.Add(Marshal.AllocHGlobal(1 * 1000 * 1000));
                saved.Add(new byte[1*1000*1000]);
            }

            foreach (IntPtr ptr in list)
                Marshal.FreeHGlobal(ptr);
        }

        void EatSomeCpu()
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 11) ;
        }

    }
}