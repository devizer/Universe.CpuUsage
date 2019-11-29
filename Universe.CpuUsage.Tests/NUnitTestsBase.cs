using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Universe.CpuUsage;

namespace Tests
{
    public class NUnitTestsBase
    {
        public static bool IsTravis => Environment.GetEnvironmentVariable("TRAVIS") == "true";

        protected static TextWriter OUT;
        private Stopwatch StartAt;
        private CpuUsage? _CpuUsage_OnStart;
        private int TestCounter = 0;
        private static int GlobalTestCounter = 0;
        private int GlobalThisTestCounter = 0;


        [SetUp]
        public void BaseSetUp()
        {
            TestConsole.Setup();
            Environment.SetEnvironmentVariable("SKIP_FLUSHING", null);
            StartAt = Stopwatch.StartNew();
            _CpuUsage_OnStart = GetCpuUsage();
            Interlocked.Increment(ref TestCounter);
            GlobalThisTestCounter = Interlocked.Increment(ref GlobalTestCounter);

            var testClassName = TestContext.CurrentContext.Test.ClassName;
            testClassName = testClassName.Split('.').LastOrDefault();
            Console.WriteLine($"#{GlobalThisTestCounter}.{TestCounter} {{{TestContext.CurrentContext.Test.Name}}} @ {testClassName} starting...");
        }

        CpuUsage? GetCpuUsage()
        {
            try
            {
                // return LinuxResourceUsage.GetByThread();
                return CpuUsage.Get(CpuUsageScope.Thread);
            }
            catch
            {
            }

            return null;
        }

        [TearDown]
        public void BaseTearDown()
        {
            TimeSpan elapsed = StartAt.Elapsed;
            string cpuUsage = "";
            if (_CpuUsage_OnStart.HasValue)
            {
                var onEnd = GetCpuUsage();
                if (onEnd != null)
                {
                    var delta = CpuUsage.Substruct(onEnd.Value, _CpuUsage_OnStart.Value);
                    double user = delta.UserUsage.TotalMicroSeconds / 1000d;
                    double kernel = delta.KernelUsage.TotalMicroSeconds / 1000d;
                    double perCents = (user + kernel) / 1000d / elapsed.TotalSeconds; 
                    cpuUsage = $" (cpu: {(perCents*100):f0}%, {(user+kernel):n3} = {user:n3} [user] + {kernel:n3} [kernel] milliseconds)";
                }
            }

            Console.WriteLine($"#{GlobalThisTestCounter}.{TestCounter} {{{TestContext.CurrentContext.Test.Name}}} >{TestContext.CurrentContext.Result.Outcome.Status.ToString().ToUpper()}< in {elapsed}{cpuUsage}{Environment.NewLine}");
        }

        [OneTimeSetUp]
        public void BaseOneTimeSetUp()
        {
            TestConsole.Setup();
        }

        [OneTimeTearDown]
        public void BaseOneTimeTearDown()
        {
            // nothing todo
        }
        
        protected static bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }
        
        public class TestConsole
        {
            static bool Done = false;
            public static void Setup()
            {
                if (!Done)
                {
                    Done = true;
                    Console.SetOut(new TW());
                }
            }

            class TW : TextWriter
            {
                public override Encoding Encoding { get; }

                public override void WriteLine(string value)
                {
//                    TestContext.Progress.Write(string.Join(",", value.Select(x => ((int)x).ToString("X2"))) );
//                    if (value.Length > Environment.NewLine.Length && value.EndsWith(Environment.NewLine))
//                        value = value.Substring(0, value.Length - Environment.NewLine.Length);
                    
                    
                    TestContext.Progress.WriteLine(value);
                    try
                    {
                        // TestContext.Error.WriteLine(value); // .WriteLine();
                    }
                    catch
                    {
                    }
                }

            }
        }

    }
}