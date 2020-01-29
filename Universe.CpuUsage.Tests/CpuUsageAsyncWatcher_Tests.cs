using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
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
        
        [Test, TestCaseSource(typeof(AsyncSchedulerCases), nameof(AsyncSchedulerCases.Schedulers))]
        public async Task SimpleTests(AsyncSchedulerCase testEnvironment)
        {
            if (!IsSupported()) return;

            // Act (durations are for debugging)
            CpuUsageAsyncWatcher watch = new CpuUsageAsyncWatcher();
            await testEnvironment.Factory.StartNew(() => LoadCpu(milliseconds: 200));
            await testEnvironment.Factory.StartNew(() => LoadCpu(milliseconds: 500));
            await testEnvironment.Factory.StartNew(() => LoadCpu(milliseconds: 800));
            await NotifyFinishedTasks();
            watch.Stop();
            var totals = watch.Totals;
            
            // Assert
            long actualMicroseconds = totals.GetSummaryCpuUsage().TotalMicroSeconds;
            long expectedMicroseconds = 1000L * (200 + 500 + 800);
            Console.WriteLine($"Expected usage: {(expectedMicroseconds/1000d):n3}, Actual usage: {(actualMicroseconds/1000d):n3} milliseconds");            
            Console.WriteLine(watch.ToHumanString(taskDescription:"SimpleTests()"));
            
            Assert.GreaterOrEqual(totals.Count, 6, "Number of context switches should be 6 at least");
            Assert.AreEqual(expectedMicroseconds, actualMicroseconds, 0.1d * expectedMicroseconds, "Actual CPU Usage should be about as expected.");
        }

        [Test, TestCaseSource(typeof(AsyncSchedulerCases), nameof(AsyncSchedulerCases.Schedulers))]
        public async Task ParallelTests(AsyncSchedulerCase testEnvironment)
        {
            if (!IsSupported()) return;
            
            // Act (durations are for debugging)
            CpuUsageAsyncWatcher watcher = new CpuUsageAsyncWatcher();
            TaskFactory tf = new TaskFactory();
            var task4 = testEnvironment.Factory.StartNew(() => LoadCpu(milliseconds: 2400));
            var task3 = testEnvironment.Factory.StartNew(() => LoadCpu(milliseconds: 2100));
            var task2 = testEnvironment.Factory.StartNew(() => LoadCpu(milliseconds: 1800));
            var task1 = testEnvironment.Factory.StartNew(() => LoadCpu(milliseconds: 1500));
            await Task.WhenAll(task1, task2, task3, task4);
            await NotifyFinishedTasks();
            watcher.Stop();
            var totals = watcher.Totals;
            
            // Assert
            long actualMicroseconds = totals.GetSummaryCpuUsage().TotalMicroSeconds;
            long expectedMicroseconds = 1000L * (2400 + 2100 + 1800 + 1500);
            Console.WriteLine($"Expected usage: {(expectedMicroseconds/1000d):n3}, Actual usage: {(actualMicroseconds/1000d):n3} milliseconds");
            Console.WriteLine(totals.ToHumanString(taskDescription:"ParallelTests()"));
            // 7 for windows 8 cores, rarely 6 for slow 2 core machine, but 7 for single core armv5
            Assert.GreaterOrEqual(totals.Count, 6, "Number of context switches should be 6 at least");
            Assert.AreEqual(expectedMicroseconds, actualMicroseconds, 0.1d * expectedMicroseconds, "Actual CPU Usage should be about as expected."); 
        }

        [Test, TestCaseSource(typeof(AsyncSchedulerCases), nameof(AsyncSchedulerCases.Schedulers))]
        public async Task ConcurrentTest(AsyncSchedulerCase testEnvironment)
        {
            if (!IsSupported()) return;
            // second context switch is lost for ThreadPerTaskScheduler
            if (testEnvironment.Scheduler is ThreadPerTaskScheduler) return;
            
            IList<Task> tasks = new List<Task>();
            int errors = 0;
            int maxThreads = Environment.ProcessorCount + 9;
            // hypothesis: each thread load should be exponential   
            for (int i = 1; i <= maxThreads; i++)
            {
                var iCopy = i;
                tasks.Add( testEnvironment.Factory.StartNew(async () =>
                {
                    var expectedMilliseconds = iCopy * 400;
                    if (!await CheckExpectedCpuUsage(expectedMilliseconds, testEnvironment.Factory)) 
                        Interlocked.Increment(ref errors);
                    
                }, TaskCreationOptions.LongRunning).Unwrap());
            }

            await Task.WhenAll(tasks);
            Assert.IsTrue(errors == 0, "Concurrent CpuUsageAsyncWatchers should not infer on each other. See details above");
        }

        async Task<bool> CheckExpectedCpuUsage(int expectedMilliseconds, TaskFactory factory)
        {
            // Act
            CpuUsageAsyncWatcher watcher = new CpuUsageAsyncWatcher();
            await factory.StartNew(() => LoadCpu(expectedMilliseconds));
            watcher.Stop();
            Console.WriteLine(watcher.ToHumanString(taskDescription: $"'Expected CPU Load is {expectedMilliseconds} milli-seconds'"));
                    
            Assert.AreEqual(2, watcher.Totals.Count, "The CheckExpectedCpuUsage should produce exact 2 context switches");
            // Assert: CpuUsage should be expectedMilliseconds
            var actualSeconds = watcher.GetSummaryCpuUsage().TotalMicroSeconds / 1000000d;
            bool isOk = actualSeconds >= expectedMilliseconds / 1000d;
            return isOk;
        }

        // Load CPU by number of milliseconds 
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
            for (int i = 0; i < 7; i++)
            {
                await Task.Run(() => Thread.Sleep(1));
                await Task.Delay(1);
            }
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