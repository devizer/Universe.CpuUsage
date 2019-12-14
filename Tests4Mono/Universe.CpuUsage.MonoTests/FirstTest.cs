using System;
using System.Linq;
using Mono.Cecil;
using NUnit.Framework;

namespace Universe.CpuUsage.MonoTests
{
    [TestFixture]
    public class FirstTest
    {
        [Test]
        public void Smoke_Test()
        {
            Show_CpuUsage_Version();
            CpuUsage? byThread = CpuUsage.GetByThread();
            CpuUsage? byProcess = CpuUsage.GetByProcess();
            TestContext.Progress.WriteLine($"Process's CPU Usage  : {byProcess}");
            TestContext.Progress.WriteLine($"Thread's CPU Usage   : {byThread}");
        }

        [Test]
        public void Show_CpuUsage_Version()
        {
            var asm = typeof(CpuUsage).Assembly;
            TestContext.Progress.WriteLine($"CpuUsage Version     : {asm.GetName().Version}");
            TestContext.Progress.WriteLine($"Its Target Framework : '{GetTargetFramework(asm.Location)}'");
        }



        [Test/*, Ignore("Experimental")*/]
        public void Show_TargetFramework_ByCecil()
        {
            var fileName = typeof(CpuUsage).Assembly.Location;

            TryAndForget("AssemblyDefinition.ReadAssembly()", () =>
            {
                var asmDef = AssemblyDefinition.ReadAssembly(fileName);
                ShowProperties(asmDef, 3);
                TestContext.Progress.WriteLine(" ");

                foreach (var attr in asmDef.CustomAttributes)
                {
                    TestContext.Progress.WriteLine($"Attribute {attr.AttributeType}");
                    ShowProperties(attr, 6);
                    foreach (var prop in attr.Properties)
                    {
                        TestContext.Progress.WriteLine($"      Property [{prop.Argument.Type}] {prop.Name}: '{prop.Argument.Value}'");
                    }
                    TestContext.Progress.WriteLine(" ");
                }


            });
        }

        [Test, Ignore("Experimental")]
        public void Show_Metatdata_ByCecil()
        {
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

        string GetTargetFramework(string fileName)
        {
            var asmDef = AssemblyDefinition.ReadAssembly(fileName);
            const string attrName = "System.Runtime.Versioning.TargetFrameworkAttribute";
            var tfa = asmDef.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == attrName);
            return tfa?.Properties.FirstOrDefault().Argument.Value?.ToString();
        }
    }
}