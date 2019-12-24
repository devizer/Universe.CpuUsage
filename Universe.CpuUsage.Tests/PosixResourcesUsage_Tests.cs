using System;
using System.Threading;
using NUnit.Framework;
using Tests;
using Universe.CpuUsage;

namespace KernelManagementJam.Tests
{
    [TestFixture]
    public class PosixResourcesUsage_Tests : NUnitTestsBase
    {
        [Test]
        public void Is_Supported()
        {
            Console.WriteLine($"PosixResourceUsage.IsSupported: {PosixResourceUsage.IsSupported}");
        }
        
        [Test]
        [TestCase(CpuUsageScope.Thread)]
        [TestCase(CpuUsageScope.Process)]
        public void Smoke_Test(CpuUsageScope scope)
        {
            Console.WriteLine($"PosixResourceUsage.GetByScope({scope}): {PosixResourceUsage.GetByScope(scope)}");
        }


        [Test]
        [TestCase(CpuUsageScope.Thread)]
        [TestCase(CpuUsageScope.Process)]
        public void ContextSwitch_Test(CpuUsageScope scope)
        {
            PosixResourceUsage before = PosixResourceUsage.GetByScope(scope).Value;
            Thread.Sleep(1);
            PosixResourceUsage after = PosixResourceUsage.GetByScope(scope).Value;
            var delta = PosixResourceUsage.Substruct(after, before);
            Console.WriteLine($"delta.InvoluntaryContextSwitches = {delta.InvoluntaryContextSwitches}");
            Console.WriteLine($"delta.VoluntaryContextSwitches = {delta.VoluntaryContextSwitches}");
        }
    }
}