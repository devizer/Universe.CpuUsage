using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tests;

namespace Universe.CpuUsage.Tests
{
#if NETCOREAPP3_0 || NETCOREAPP3_1 || NETCOREAPP5_0 
    [TestFixture]
    public class CpuUsageAsyncWatcher_AsyncStreamTests : NUnitTestsBase
    {
        // https://blog.jetbrains.com/dotnet/2019/09/16/async-streams-look-new-language-features-c-8/
        // https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/generate-consume-asynchronous-stream
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/async-streams
        [Test, TestCaseSource(typeof(AsyncSchedulerCases), nameof(AsyncSchedulerCases.Schedulers))]
        public async Task AwaitForEachTests(AsyncSchedulerCase testEnvironment)
        {
            // Pre JIT
            await testEnvironment.Factory.StartNew(() => CpuLoader.Run(minDuration: 1, minCpuUsage: 1, needKernelLoad: true));
            // Act (durations are for debugging)
            CpuUsageAsyncWatcher watcher = new CpuUsageAsyncWatcher();
            long expectedMicroseconds = 0;
            await foreach (var milliseconds in GetLoadings())
            {
                await testEnvironment.Factory.StartNew(() => CpuLoader.Run(minDuration: milliseconds, minCpuUsage: milliseconds, needKernelLoad: true));
                expectedMicroseconds += milliseconds * 1000L;
            }

            var totals = watcher.Totals;
            long actualMicroseconds = totals.GetSummaryCpuUsage().TotalMicroSeconds;
            Console.WriteLine($"Expected usage: {(expectedMicroseconds/1000d):n3}, Actual usage: {(actualMicroseconds/1000d):n3} milliseconds");
            Console.WriteLine(totals.ToHumanString(taskDescription:"ParallelTests()"));
            
            // Assert
            Assert.GreaterOrEqual(totals.Count, 8, "Number of context switches should be 8 at least");
            // 0.15 for qemu armhf, less for rest 
            Assert.AreEqual(expectedMicroseconds, actualMicroseconds, 0.15d * expectedMicroseconds, "Actual CPU Usage should be about as expected."); 
        }
        
        static async IAsyncEnumerable<int> GetLoadings([EnumeratorCancellation] CancellationToken token = default)
        {
            foreach (var ret in new[] {250, 450, 650, 850})
            {
                yield return ret;
                await Task.Delay(10);
            }
        }
        
    }
#endif
}