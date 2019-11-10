using System;
using NUnit.Framework;

namespace Universe.CpuUsage.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            Console.WriteLine("I'm SETUP'");
        }

        [Test]
        public void Test1()
        {
            Console.WriteLine("I'm TEST");
            Assert.Pass();
        }
    }
}