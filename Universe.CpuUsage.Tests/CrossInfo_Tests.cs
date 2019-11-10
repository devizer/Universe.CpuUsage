using System;
using NUnit.Framework;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class CrossInfo_Tests
    {
    
        [Test]
        public void Show_The_Platform()
        {
            Console.WriteLine($"The Platform: {CrossInfo.ThePlatform}");
        }
    }
}