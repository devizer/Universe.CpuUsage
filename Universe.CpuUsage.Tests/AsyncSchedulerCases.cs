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

                // works on OSX 10.10 on mono and windows, may not work on dotnet except of ConcurrentTest
                // May fail CpuUsageAsyncWatcher_Tests.ParallelTests and CpuUsageAsyncWatcher_Tests.SimpleTest on Linux
                ThreadPerTaskScheduler tpt = new ThreadPerTaskScheduler();
                yield return new AsyncSchedulerCase("Thread Per Task Scheduler", new TaskFactory(tpt), tpt);
                
                LimitedConcurrencyLevelTaskScheduler lc = new LimitedConcurrencyLevelTaskScheduler(16);
                yield return new AsyncSchedulerCase("Limited Concurrency Scheduler, up to 16 threads", new TaskFactory(lc), lc);

                LimitedConcurrencyLevelTaskScheduler lc1 = new LimitedConcurrencyLevelTaskScheduler(1);
                yield return new AsyncSchedulerCase("Limited Concurrency Scheduler. Single Thread", new TaskFactory(lc1), lc1);

                if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
                {
                    QueuedTaskScheduler s = new QueuedTaskScheduler(1);
                    yield return new AsyncSchedulerCase("QueuedTaskScheduler, Single Thread", new TaskFactory(s), s);

                    // May fail CpuUsageAsyncWatcher_Tests.ParallelTests only
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