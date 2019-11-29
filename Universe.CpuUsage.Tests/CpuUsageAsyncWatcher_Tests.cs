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
        public async Task Simple()
        {
            if (!IsSupported()) return;

            await PreJit();
            CpuUsageAsyncWatcher watch = new CpuUsageAsyncWatcher();
            await Task.Run(() => LoadCpu(111));
            await Task.Run(() => LoadCpu(222));
            await Task.Run(() => LoadCpu(333));
            var actual = watch.GetTotalCpuUsage().TotalMicroSeconds;
            var expected = 1000 * (111 + 222 + 333);
            Assert.IsTrue( actual >= 0.9d*expected, "Cpu Usage in multi threaded scenario should be caught");
            Console.WriteLine(watch.ToHumanString());
        }

        private void LoadCpu(int milliseconds) => CpuLoader.Run(milliseconds, true);

        private async Task PreJit()
        {
            CpuUsageAsyncWatcher watch = new CpuUsageAsyncWatcher();
            await Task.Run(() => LoadCpu(1));
            var _ = watch.ToHumanString();
        }

        bool IsSupported()
        {
            if (!CpuUsageAsyncWatcher.IsSupported)
            {
                Console.WriteLine("CpuUsageAsyncWatcher requires NET 4.6+, Net Core 1.0+ or NetStandard 2.0+");
            }

            return CpuUsageAsyncWatcher.IsSupported;
        }
        
        
    }
}