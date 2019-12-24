using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using KernelManagementJam.Benchmarks;
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
        private string FileName = "IO-Metrics-Tests-" + Guid.NewGuid().ToString() + ".tmp";

        private void WriteFile(int size)
        {
            Random rnd = new Random(42);
            byte[] content = new byte[size];
            rnd.NextBytes(content);
            using (FileStream fs = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 128, FileOptions.WriteThrough))
            {
                fs.Write(content, 0, content.Length);
            }
        }

        private void ReadFile()
        {
            var bytes = File.ReadAllBytes(FileName);
        }

        [TearDown]
        public void TearDown_IO_Metrics()
        {
            if (File.Exists(FileName)) File.Delete(FileName);
        }
        
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
            if (!PosixResourceUsage.IsSupported) return;
            Console.WriteLine($"PosixResourceUsage.GetByScope({scope}): {PosixResourceUsage.GetByScope(scope)}");
        }


        [Test]
        [TestCase(CpuUsageScope.Thread,1)]
        [TestCase(CpuUsageScope.Process,1)]
        [TestCase(CpuUsageScope.Thread,42)]
        [TestCase(CpuUsageScope.Process,42)]
        public void ContextSwitch_Test(CpuUsageScope scope, int switchCount)
        {
            if (!PosixResourceUsage.IsSupported) return;
            if (scope == CpuUsageScope.Thread && CrossInfo.ThePlatform != CrossInfo.Platform.Linux) return;
            
            PosixResourceUsage before = PosixResourceUsage.GetByScope(scope).Value;
            
            for (int i = 0; i < switchCount; i++)
            {
                // CpuLoader.Run(1, 0, true);
                Stopwatch sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 1) ;
                Thread.Sleep(1);
            }
            
            PosixResourceUsage after = PosixResourceUsage.GetByScope(scope).Value;
            var delta = PosixResourceUsage.Substruct(after, before);
            Console.WriteLine($"delta.InvoluntaryContextSwitches = {delta.InvoluntaryContextSwitches}");
            Console.WriteLine($"delta.VoluntaryContextSwitches = {delta.VoluntaryContextSwitches}");
            if (scope == CpuUsageScope.Thread)
                Assert.AreEqual(switchCount, delta.VoluntaryContextSwitches);
            else
                Assert.GreaterOrEqual(delta.VoluntaryContextSwitches, switchCount);
        }
        
        [Test]
        [TestCase(CpuUsageScope.Thread)]
        [TestCase(CpuUsageScope.Process)]
        public void IO_Reads_Test(CpuUsageScope scope)
        {
            if (!PosixResourceUsage.IsSupported) return;
            if (scope == CpuUsageScope.Thread && CrossInfo.ThePlatform != CrossInfo.Platform.Linux) return;

            // Arrange
            WriteFile(512*1024);
            LinuxKernelCacheFlusher.Sync();
            
            // Act
            PosixResourceUsage before = PosixResourceUsage.GetByScope(scope).Value;
            ReadFile();
            PosixResourceUsage after = PosixResourceUsage.GetByScope(scope).Value;
            var delta = PosixResourceUsage.Substruct(after, before);
            
            // Assert
            Console.WriteLine($"delta.ReadOps = {delta.ReadOps}");
            Console.WriteLine($"delta.WriteOps = {delta.WriteOps}");
            Assert.Greater(delta.ReadOps, 0);
        }
 
        [Test]
        [TestCase(CpuUsageScope.Thread)]
        [TestCase(CpuUsageScope.Process)]
        public void IO_Write_Test(CpuUsageScope scope)
        {
            if (!PosixResourceUsage.IsSupported) return;
            if (scope == CpuUsageScope.Thread && CrossInfo.ThePlatform != CrossInfo.Platform.Linux) return;

            // Arrange: nothing to do
            
            // Act
            PosixResourceUsage before = PosixResourceUsage.GetByScope(scope).Value;
            WriteFile(10*512*1024);
            PosixResourceUsage after = PosixResourceUsage.GetByScope(scope).Value;
            var delta = PosixResourceUsage.Substruct(after, before);
            
            // Assert
            Console.WriteLine($"delta.ReadOps = {delta.ReadOps}");
            Console.WriteLine($"delta.WriteOps = {delta.WriteOps}");
            Assert.Greater(delta.WriteOps, 0);
        }
    }
}