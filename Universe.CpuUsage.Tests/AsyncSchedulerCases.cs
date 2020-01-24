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

                if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
                {
                    // works on OSX 10.10 on mono, doesnt work on dotnet 
                    ThreadPerTaskScheduler tpt = new ThreadPerTaskScheduler();
                    yield return new AsyncSchedulerCase("Thread Per Task Scheduler", new TaskFactory(tpt), tpt);

                    QueuedTaskScheduler s = new QueuedTaskScheduler(1);
                    yield return new AsyncSchedulerCase("Single Threaded QueuedTaskScheduler", new TaskFactory(s), s);

                    QueuedTaskScheduler s2 = new QueuedTaskScheduler();
                    yield return new AsyncSchedulerCase("QueuedTaskScheduler, unlimited threads", new TaskFactory(s2), s2);
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