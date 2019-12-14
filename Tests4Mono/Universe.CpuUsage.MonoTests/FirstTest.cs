using System;
using Mono.Cecil;
using NUnit.Framework;

namespace Universe.CpuUsage.MonoTests
{
    [TestFixture]
    public class FirstTest
    {
        [Test]
        public void Go()
        {
            CpuUsage? usage = CpuUsage.GetByProcess();
            TestContext.Progress.WriteLine($"Process's CPU Usage: {usage}");

            var cpuUsageVersion = typeof(CpuUsage).Assembly.GetName().Version;
            TestContext.Progress.WriteLine($"CpuUsage Version: {cpuUsageVersion}");

            var fileName = typeof(CpuUsage).Assembly.Location;

            TestContext.Progress.WriteLine($"CpuUsage Location: {fileName}");



            TryAndForget("ModuleDefinition.ReadModule()", () =>
            {
                ModuleDefinition module = ModuleDefinition.ReadModule(fileName);
                ShowProperties(module, 4);
                TestContext.Progress.WriteLine(" ");


                foreach (CustomAttribute moduleCustomAttribute in module.CustomAttributes)
                {
                    TestContext.Progress.WriteLine($"Attribute: {moduleCustomAttribute.GetType()}");
                    ShowProperties(moduleCustomAttribute, 4);
                    TestContext.Progress.WriteLine(" ");
                }


            });
            
        }

        void ShowProperties(object obj, int indent)
        {
            if (obj == null) return;
            string pre = indent > 0 ? new string(' ', indent) : "";
            var props = obj.GetType().GetProperties();
            foreach (var propertyInfo in props)
            {
                TryAndForget(null, () =>
                {
                    TestContext.Progress.WriteLine($"{pre}{propertyInfo.Name}: {propertyInfo.GetValue(obj)}");
                });
            }
        }

        void TryAndForget(string caption, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (caption != null)
                    Console.WriteLine($"Fail. {caption}. {ex.GetType().Name}: {ex.Message}");
            }

        }
    }
}