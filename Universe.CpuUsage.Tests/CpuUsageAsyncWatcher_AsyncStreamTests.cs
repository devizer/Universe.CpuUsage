using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Tests;

namespace Universe.CpuUsage.Tests
{
    [TestFixture]
    public class CpuUsageAsyncWatcher_AsyncStreamTests : NUnitTestsBase
    {
        // https://blog.jetbrains.com/dotnet/2019/09/16/async-streams-look-new-language-features-c-8/
        // https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/generate-consume-asynchronous-stream
        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/async-streams
        [Test]
        public async Task AwaitForEachTests()
        {
            await Task.Run(() => "nothing to do");
        }
        
    }
}