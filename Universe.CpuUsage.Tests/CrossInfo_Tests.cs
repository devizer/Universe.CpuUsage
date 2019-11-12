using System;
using NUnit.Framework;
using Tests;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class CrossInfo_Tests : NUnitTestsBase
    {
    
        [Test]
        public void Show_The_Platform()
        {
            Console.WriteLine($"The Platform: {CrossInfo.ThePlatform}");
            Console.WriteLine($"Process: {IntPtr.Size * 4} bits");
        }
    }
}