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
        [SetUp]
        public void SetUp()
        {
            Console.WriteLine("Pre-Jitting CpuUsageAsyncWatcher class");
            PreJit().Wait();
        }
        
        [Test]
        public async Task SimpleTests()
        {
            if (!IsSupported()) return;

            CpuUsageAsyncWatcher watch = new CpuUsageAsyncWatcher();
            await Task.Run(() => LoadCpu(milliseconds: 111));
            await Task.Run(() => LoadCpu(milliseconds: 222));
            await Task.Run(() => LoadCpu(milliseconds: 333));
            var totals = watch.Totals;
            
            // Assert
            long actualMicroseconds = totals.GetSummaryCpuUsage().TotalMicroSeconds;
            long expectedMicroseconds = 1000L * (111 + 222 + 333);
            Console.WriteLine($"Expected usage: {(expectedMicroseconds/1000):n3}, Actual usage: {(actualMicroseconds/1000):n3} milliseconds");            
            Console.WriteLine(watch.ToHumanString(taskDescription:"SimpleTests()"));
            
            Assert.GreaterOrEqual(totals.Count, 6, "Number of context switches should be 6 at least");
            // the 0.95 multiplier is need to compensate granularity and precision
            if (actualMicroseconds < 0.95d * expectedMicroseconds)
            {
                Console.WriteLine("Warning! 2 cores is not yet enough. Actual CPU Usage should be about as expected.");
            }
        }

        [Test]
        public async Task ParallelTests()
        {
            if (!IsSupported()) return;
            
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
            Console.WriteLine($"Expected usage: {(expected/1000):n3}, Actual usage: {(actualMicroseconds/1000):n3} milliseconds");
            Console.WriteLine(totals.ToHumanString(taskDescription:"ParallelTests()"));
            // 5 for windows 8 cores, 6 for linux 2 cores
            Assert.GreaterOrEqual(totals.Count, 5, "Number of context switches should be 5 at least");
            // the 0.95 multiplier is need to compensate granularity and precision
            if (actualMicroseconds < 0.95d * expected)
            {
                Console.WriteLine("Warning! 2 cores is not yet enough. Actual CPU Usage should be about as expected.");
            }
        }

        // Load CPU Usage at least number of milliseconds 
        private void LoadCpu(int milliseconds = 42) => CpuLoader.Run(minDuration: milliseconds, minCpuUsage: milliseconds, needKernelLoad: true);

        private async Task PreJit()
        {
            CpuUsageAsyncWatcher watch = new CpuUsageAsyncWatcher();
            await Task.Run(() => LoadCpu(1));
            await NotifyFinishedTasks();
            var _ = watch.ToHumanString();
            watch.Stop();
            
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