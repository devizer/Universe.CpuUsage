using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace Universe.CpuUsage.Tests
{
    public class AsyncSchedulerCases
    {
        public static IEnumerable<AsyncSchedulerCase> Schedulers
        {
            get
            {
                var defaultScheduler = TaskScheduler.Default;
                yield return new AsyncSchedulerCase("Default .NET Scheduler", new TaskFactory(defaultScheduler), defaultScheduler);

                // works on OSX 10.10 on mono and windows, doesnt work on dotnet except of ConcurrentTest
                // Fail CpuUsageAsyncWatcher_Tests.ParallelTests and CpuUsageAsyncWatcher_Tests.SimpleTest on Linux
                ThreadPerTaskScheduler tpt = new ThreadPerTaskScheduler();
                yield return new AsyncSchedulerCase("Thread Per Task Scheduler", new TaskFactory(tpt), tpt);

                if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
                {
                    QueuedTaskScheduler s = new QueuedTaskScheduler(1);
                    yield return new AsyncSchedulerCase("QueuedTaskScheduler, Single Thread", new TaskFactory(s), s);

                    // Fails CpuUsageAsyncWatcher_Tests.ParallelTests only
                    QueuedTaskScheduler s2 = new QueuedTaskScheduler();
                    yield return new AsyncSchedulerCase("QueuedTaskScheduler, Unlimited Threads", new TaskFactory(s2), s2);
                }

            }
        } 
    }
    
    public class AsyncSchedulerCase
    {
        public string Title;
        public TaskFactory Factory;
        public TaskScheduler Scheduler;

        public AsyncSchedulerCase(string title, TaskFactory factory, TaskScheduler scheduler)
        {
            Title = title;
            Factory = factory;
            Scheduler = scheduler;
        }

        public override string ToString()
        {
            return Title;
        }
    }

}