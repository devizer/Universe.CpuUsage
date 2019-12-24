using System;
using System.Threading;
using NUnit.Framework;
using Tests;
using Universe;
using Universe.CpuUsage;
using Universe.CpuUsage.Tests;

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
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows) return;
            Console.WriteLine($"PosixResourceUsage.GetByScope({scope}): {PosixResourceUsage.GetByScope(scope)}");
        }


        [Test]
        [TestCase(CpuUsageScope.Thread,1)]
        [TestCase(CpuUsageScope.Process,1)]
        [TestCase(CpuUsageScope.Thread,42)]
        [TestCase(CpuUsageScope.Process,42)]
        public void ContextSwitch_Test(CpuUsageScope scope, int switchCount)
        {
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows) return;
            if (scope == CpuUsageScope.Thread && CrossInfo.ThePlatform != CrossInfo.Platform.Linux) return;
            
            PosixResourceUsage before = PosixResourceUsage.GetByScope(scope).Value;
            for (int i = 0; i < switchCount; i++)
            {
                CpuLoader.Run(1, 0, true);
                Thread.Sleep(1);
            }
            
            PosixResourceUsage after = PosixResourceUsage.GetByScope(scope).Value;
            var delta = PosixResourceUsage.Substruct(after, before);
            Console.WriteLine($"delta.InvoluntaryContextSwitches = {delta.InvoluntaryContextSwitches}");
            Console.WriteLine($"delta.VoluntaryContextSwitches = {delta.VoluntaryContextSwitches}");
        }
    }
}