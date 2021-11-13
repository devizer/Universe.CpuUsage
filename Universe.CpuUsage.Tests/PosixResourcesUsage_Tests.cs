using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using KernelManagementJam.Benchmarks;
using NUnit.Framework;
using Tests;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class PosixResourcesUsage_Tests : NUnitTestsBase
    {
        private string FileName = "IO-Metrics-Tests-" + Guid.NewGuid().ToString() + ".tmp";

        // Works at home PC and AppVeyor linux,
        // Does not work on multi arch docker container on travis-ci 
        private bool SkipPosixResourcesUsageAsserts => 
            Environment.GetEnvironmentVariable("SKIP_POSIXRESOURCESUSAGE_ASSERTS") == "True";

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
        public void ContextSwitch_Test(CpuUsageScope scope, int expectedSwitchCount)
        {
            if (!PosixResourceUsage.IsSupported) return;
            if (scope == CpuUsageScope.Thread && CrossInfo.ThePlatform != CrossInfo.Platform.Linux) return;
            // Jit
            var jit = PosixResourceUsage.GetByScope(scope).Value;
            
            // Act
            PosixResourceUsage before = PosixResourceUsage.GetByScope(scope).Value;
            for (int i = 0; i < expectedSwitchCount; i++)
            {
                // CpuLoader.Run(1, 0, true);
                Stopwatch sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 1) ;
                // on some build server sometimes fails (Azure - sometimes fail, AppVeyor - OK)
                Thread.Sleep(1);
            }
            
            PosixResourceUsage after = PosixResourceUsage.GetByScope(scope).Value;
            // var delta = PosixResourceUsage.Substruct(after, before);
            var delta = after - before;
            Console.WriteLine($"delta.InvoluntaryContextSwitches = {delta.InvoluntaryContextSwitches}");
            Console.WriteLine($"delta.VoluntaryContextSwitches = {delta.VoluntaryContextSwitches}");

            // Assert
            if (CrossInfo.ThePlatform != CrossInfo.Platform.Linux) return;
            if (SkipPosixResourcesUsageAsserts) return;
            var expectedMax = Math.Max(expectedSwitchCount + 10, expectedSwitchCount * 2);
            Assert.IsTrue(
                delta.VoluntaryContextSwitches >= expectedSwitchCount && delta.VoluntaryContextSwitches <= expectedMax,
                $"VoluntaryContextSwitches: Expected {expectedSwitchCount} ... {expectedMax+7}; actual {delta.VoluntaryContextSwitches}");
//            if (scope == CpuUsageScope.Thread)
//                Assert.IsTrue(expectedSwitchCount == delta.VoluntaryContextSwitches || expectedSwitchCount == delta.VoluntaryContextSwitches - 1);
//            else
//                Assert.GreaterOrEqual(delta.VoluntaryContextSwitches, expectedSwitchCount);
        }
        
        [Test]
        [TestCase(CpuUsageScope.Thread)]
        [TestCase(CpuUsageScope.Process)]
        // Does not work on drone.io in container
        public void IO_Reads_Test(CpuUsageScope scope)
        {
            if (!PosixResourceUsage.IsSupported) return;
            if (scope == CpuUsageScope.Thread && CrossInfo.ThePlatform != CrossInfo.Platform.Linux) return;

            // Arrange
            var numBytes = 20*1048*1024;
            WriteFile(numBytes);
            LinuxKernelCacheFlusher.Sync();
            
            // Act
            PosixResourceUsage before = PosixResourceUsage.GetByScope(scope).Value;
            ReadFile();
            PosixResourceUsage after = PosixResourceUsage.GetByScope(scope).Value;
            var delta = PosixResourceUsage.Substruct(after, before);
            Console.WriteLine($"Read Operations. Before: [{before.ReadOps}]. After: [{after.ReadOps}]");
            Console.WriteLine($"Operation: Read {numBytes:n0} bytes. ReadOps = {delta.ReadOps}. WriteOps = {delta.WriteOps}");
            
            // Assert
            if (CrossInfo.ThePlatform != CrossInfo.Platform.Linux) return;
            if (SkipPosixResourcesUsageAsserts || !IsIoReadsWritesSupported()) return;
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
            var numBytes = 10*512*1024;
            WriteFile(numBytes);
            PosixResourceUsage after = PosixResourceUsage.GetByScope(scope).Value;
            var delta = PosixResourceUsage.Substruct(after, before);
            Console.WriteLine($"Operation: write {numBytes:n0} bytes. ReadOps = {delta.ReadOps}. WriteOps = {delta.WriteOps}");
            
            // Assert
            if (CrossInfo.ThePlatform != CrossInfo.Platform.Linux) return;
            if (SkipPosixResourcesUsageAsserts || !IsIoReadsWritesSupported()) return;
            Assert.Greater(delta.WriteOps, 0);
        }

        bool IsIoReadsWritesSupported()
        {
            try
            {
                return File.Exists("/proc/self/io");
            }
            catch
            {
                // termux on non-rooted android phone. May be supported but we cant know
                return false;
            }
        }
    }
}