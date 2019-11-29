using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tests;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class CpuUsageAsyncWatcher_Tests : NUnitTestsBase
    {
        [Test]
        public async Task SimpleTests()
        {
            if (!IsSupported()) return;

            await PreJit();
            CpuUsageAsyncWatcher watch = new CpuUsageAsyncWatcher();
            await Task.Run(() => LoadCpu(milliseconds: 111));
            await Task.Run(() => LoadCpu(milliseconds: 222));
            await Task.Run(() => LoadCpu(milliseconds: 333));
            var totals = watch.Totals;
            
            // Assert
            long actualMicroseconds = totals.GetSummaryCpuUsage().TotalMicroSeconds;
            long expectedMicroseconds = 1000L * (111 + 222 + 333);
            Console.WriteLine(watch.ToHumanString(taskDescription:"SimpleTests()"));
            Assert.AreEqual(6, totals.Count, "Number of context switches should be 6");
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
            {
                // the 0.95 multiplier is need to compensate granularity and precision
                Assert.Greater( actualMicroseconds, expectedMicroseconds * 0.95d , "Cpu Usage in multi threaded scenario should be fully caught");
            }
        }

        [Test]
        public async Task ParallelTests()
        {
            if (!IsSupported()) return;
            
            await PreJit();
            CpuUsageAsyncWatcher watcher = new CpuUsageAsyncWatcher();
            var task4 = Task.Run(() => LoadCpu(milliseconds: 555));
            var task3 = Task.Run(() => LoadCpu(milliseconds: 444));
            var task2 = Task.Run(() => LoadCpu(milliseconds: 333));
            var task1 = Task.Run(() => LoadCpu(milliseconds: 222));
            Task.WaitAll(task1, task2, task3, task4);
            await NotifyFinishedTasks();
            var totals = watcher.Totals;
            
            // Assert
            long actualMicroseconds = totals.GetSummaryCpuUsage().TotalMicroSeconds;
            long expected = 1000L * (555 + 444 + 333 + 222);
            Console.WriteLine(totals.ToHumanString(taskDescription:"ParallelTests()"));
            Assert.AreEqual(5, totals.Count, "Number of context switches should be 5");
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
            {
                // the 0.95 multiplier is need to compensate granularity and precision
                Assert.Greater(actualMicroseconds, expected * 0.95d, "Cpu Usage in multi threaded scenario should be caught");
            }
        }

        private void LoadCpu(int milliseconds = 42) => CpuLoader.Run(milliseconds, true);

        private async Task PreJit()
        {
            CpuUsageAsyncWatcher watch = new CpuUsageAsyncWatcher();
            await Task.Run(() => LoadCpu(1));
            var _ = watch.ToHumanString();
        }

        async Task NotifyFinishedTasks()
        {
            // v1
            // await Task.Run(() => "Nothing to do");
            
            // v2 
            // await Task.Delay(0);
            
            // v3
            await Task.Run(() => Thread.Sleep(0));
        }

        bool IsSupported()
        {
            if (!CpuUsageAsyncWatcher.IsSupported)
            {
                Console.WriteLine("Skipping. The CpuUsageAsyncWatcher class requires NET Framework 4.6+, Net Core 1.0+ or NetStandard 2.0+");
            }

            return CpuUsageAsyncWatcher.IsSupported;
        }
        
        
    }
}