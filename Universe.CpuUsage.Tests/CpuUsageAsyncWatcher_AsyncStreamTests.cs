using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tests;

namespace Universe.CpuUsage.Tests
{
#if NETCOREAPP3_0
    [TestFixture]
    public class CpuUsageAsyncWatcher_AsyncStreamTests : NUnitTestsBase
    {
        // https://blog.jetbrains.com/dotnet/2019/09/16/async-streams-look-new-language-features-c-8/
        // https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/generate-consume-asynchronous-stream
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/async-streams
        [Test]
        public async Task AwaitForEachTests()
        {
            // Act (durations are for debugging)
            CpuUsageAsyncWatcher watcher = new CpuUsageAsyncWatcher();
            long expectedMicroseconds = 0;
            await foreach (var milliseconds in GetLoadings())
            {
                await Task.Run(() => CpuLoader.Run(minDuration: milliseconds, minCpuUsage: milliseconds, needKernelLoad: true));
                expectedMicroseconds += milliseconds * 1000L;
            }

            var totals = watcher.Totals;
            long actualMicroseconds = totals.GetSummaryCpuUsage().TotalMicroSeconds;
            Console.WriteLine($"Expected usage: {(expectedMicroseconds/1000d):n3}, Actual usage: {(actualMicroseconds/1000d):n3} milliseconds");
            Console.WriteLine(totals.ToHumanString(taskDescription:"ParallelTests()"));
            
            Assert.GreaterOrEqual(totals.Count, 8, "Number of context switches should be 8 at least");
            Assert.AreEqual(expectedMicroseconds, actualMicroseconds, 0.1d * expectedMicroseconds, "Actual CPU Usage should be about as expected."); 
        }
        
        static async IAsyncEnumerable<int> GetLoadings(
            [EnumeratorCancellation] CancellationToken token = default)
        {
            yield return 250;
            yield return 450;
            yield return 650;
            yield return 850;
        }
        
    }
#endif
}