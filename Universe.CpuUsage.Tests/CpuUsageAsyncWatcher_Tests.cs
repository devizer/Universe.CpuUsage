using System;
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
            await Task.Run(() => LoadCpu(111));
            await Task.Run(() => LoadCpu(222));
            await Task.Run(() => LoadCpu(333));
            var actual = watch.GetTotalCpuUsage().TotalMicroSeconds;
            var expected = 1000 * (111 + 222 + 333);
            Console.WriteLine(watch.ToHumanString(taskDescription:"SimpleTests()"));
            // the 0.9 multiplier is need to compensate granularity and precision
            Assert.Greater( actual / 0.9d, expected, "Cpu Usage in multi threaded scenario should be fully caught");
        }

        [Test]
        public async Task ParallelTests()
        {
            if (!IsSupported()) return;
            
            await PreJit();
            CpuUsageAsyncWatcher watcher = new CpuUsageAsyncWatcher();
            var task4 = Task.Run(() => LoadCpu(555));
            var task3 = Task.Run(() => LoadCpu(444));
            var task2 = Task.Run(() => LoadCpu(333));
            var task1 = Task.Run(() => LoadCpu(222));
            Task.WaitAll(task1, task2, task3, task4);
            await NotifyFinishedTasks();
            var totalCpuUsage = watcher.Totals;
            var actual = totalCpuUsage.GetSummaryCpuUsage().TotalMicroSeconds;
            var expected = 1000 * (555 + 444 + 333 + 222);
            Console.WriteLine(totalCpuUsage.ToHumanString(taskDescription:"ParallelTests()"));
            // the 0.9 multiplier is need to compensate granularity and precision
            Assert.Greater( actual / 0.9d, expected, "Cpu Usage in multi threaded scenario should be caught");
        }

        private void LoadCpu(int milliseconds) => CpuLoader.Run(milliseconds, true);

        private async Task PreJit()
        {
            CpuUsageAsyncWatcher watch = new CpuUsageAsyncWatcher();
            await Task.Run(() => LoadCpu(1));
            var _ = watch.ToHumanString();
        }

        async Task NotifyFinishedTasks()
        {
            // await Task.Run(() => Thread.Sleep(0));
            await Task.Run(() => "Nothing to do" );
        }

        bool IsSupported()
        {
            if (!CpuUsageAsyncWatcher.IsSupported)
            {
                Console.WriteLine("Skipping. CpuUsageAsyncWatcher requires NET 4.6+, Net Core 1.0+ or NetStandard 2.0+");
            }

            return CpuUsageAsyncWatcher.IsSupported;
        }
        
        
    }
}