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
        [OneTimeSetUp]
        public void SetUp()
        {
            PreJit().Wait();
        }
        
        [Test]
        public async Task SimpleTests()
        {
            if (!IsSupported()) return;

            // Act (durations are for debugging)
            CpuUsageAsyncWatcher watch = new CpuUsageAsyncWatcher();
            await Task.Run(() => LoadCpu(milliseconds: 200));
            await Task.Run(() => LoadCpu(milliseconds: 500));
            await Task.Run(() => LoadCpu(milliseconds: 800));
            watch.Stop();
            var totals = watch.Totals;
            
            // Assert
            long actualMicroseconds = totals.GetSummaryCpuUsage().TotalMicroSeconds;
            long expectedMicroseconds = 1000L * (200 + 500 + 800);
            Console.WriteLine($"Expected usage: {(expectedMicroseconds/1000d):n3}, Actual usage: {(actualMicroseconds/1000d):n3} milliseconds");            
            Console.WriteLine(watch.ToHumanString(taskDescription:"SimpleTests()"));
            
            Assert.GreaterOrEqual(totals.Count, 6, "Number of context switches should be 6 at least");
            Assert.GreaterOrEqual(actualMicroseconds, expectedMicroseconds, "Actual CPU Usage should be about as expected.");
        }

        [Test]
        public async Task ParallelTests()
        {
            if (!IsSupported()) return;
            
            // Act (durations are for debugging)
            CpuUsageAsyncWatcher watcher = new CpuUsageAsyncWatcher();
            var task4 = Task.Run(() => LoadCpu(milliseconds: 2400));
            var task3 = Task.Run(() => LoadCpu(milliseconds: 2100));
            var task2 = Task.Run(() => LoadCpu(milliseconds: 1800));
            var task1 = Task.Run(() => LoadCpu(milliseconds: 1500));
            Task.WaitAll(task1, task2, task3, task4);
            await NotifyFinishedTasks();
            watcher.Stop();
            var totals = watcher.Totals;
            
            // Assert
            long actualMicroseconds = totals.GetSummaryCpuUsage().TotalMicroSeconds;
            long expected = 1000L * (2400 + 2100 + 1800 + 1500);
            Console.WriteLine($"Expected usage: {(expected/1000d):n3}, Actual usage: {(actualMicroseconds/1000d):n3} milliseconds");
            Console.WriteLine(totals.ToHumanString(taskDescription:"ParallelTests()"));
            // 7 for windows 8 cores, rarely 6 for slow 2 core machine
            Assert.GreaterOrEqual(totals.Count, 6, "Number of context switches should be 6 at least");
            Assert.GreaterOrEqual(actualMicroseconds, expected, "Actual CPU Usage should be about as expected.");
        }

        [Test]
        public async Task ConcurrentTest()
        {
            if (!IsSupported()) return;
            
            System.Collections.Generic.List<Task> tasks = new System.Collections.Generic.List<Task>();
            int errors = 0;
            int maxThreads = Environment.ProcessorCount + 9;
            for (int i = 1; i <= maxThreads; i++)
            {
                var iCopy = i;
                tasks.Add( Task.Factory.StartNew(async () =>
                {
                    var expectedMilliseconds = iCopy * 400;
                    if (!await CheckExpectedCpuUsage(expectedMilliseconds)) 
                        Interlocked.Increment(ref errors);
                    
                }, TaskCreationOptions.LongRunning).Unwrap());
            }

            Task.WaitAll(tasks.ToArray());
            Assert.IsTrue(errors == 0, "Concurrent CpuUsageAsyncWatcher should not infer on each other. See details above");
        }

        async Task<bool> CheckExpectedCpuUsage(int expectedMilliseconds)
        {
            // Act
            CpuUsageAsyncWatcher watcher = new CpuUsageAsyncWatcher();
            await Task.Run(() => LoadCpu(expectedMilliseconds));
            watcher.Stop();
            Console.WriteLine(watcher.ToHumanString(taskDescription: $"'Expected CPU Load is {expectedMilliseconds} milli-seconds'"));
                    
            // Assert: CpuUsage should be expectedMilliseconds
            var actualSeconds = watcher.GetSummaryCpuUsage().TotalMicroSeconds / 1000000d;
            bool isOk = Math.Abs(actualSeconds - expectedMilliseconds / 1000d) < 0.2d;
            return isOk;
        }

        // Load CPU Usage at least number of milliseconds 
        private void LoadCpu(int milliseconds = 42) => CpuLoader.Run(minDuration: milliseconds, minCpuUsage: milliseconds, needKernelLoad: true);

        private async Task PreJit()
        {
            Console.WriteLine("Pre-Jitting CpuUsageAsyncWatcher class");
            CpuUsageAsyncWatcher watch = new CpuUsageAsyncWatcher();
            await Task.Run(() => LoadCpu(1));
            await NotifyFinishedTasks();
            watch.Stop();
            watch.ToHumanString();
            Console.WriteLine("Pre-Jitted CpuUsageAsyncWatcher class");
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