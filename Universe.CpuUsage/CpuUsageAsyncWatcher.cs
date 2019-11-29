using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Linq;
// ReSharper disable PossibleInvalidOperationException

namespace Universe.CpuUsage
{
    public class CpuUsageAsyncWatcher
    {
        public List<ContextSwitchLogItem> Totals
        {
            get
            {
                lock (_Log)
                {
                    List<ContextSwitchLogItem> copy = new List<ContextSwitchLogItem>(_Log);
                    return copy;
                }
            }
        }

        public CpuUsage GetTotalCpuUsage()
        {
            return Totals.GetSummaryCpuUsage();
        }  
        
        private List<ContextSwitchLogItem> _Log = new List<ContextSwitchLogItem>(); 
        public class ContextSwitchLogItem
        {
            public double Duration { get; internal set; }
            public CpuUsage CpuUsage { get; internal set; }
        }

#if NETCOREAPP || NETSTANDARD2_0 || NETSTANDARD2_1 || NET48 || NET472 || NET471 || NET47 || NET462 || NET461 || NET46

        private class ContextSwitchInfo
        {
            public int ThreadId;
            public Stopwatch StartAt;
            public CpuUsage UsageOnStart;
        }

        private static ThreadLocal<ContextSwitchInfo> ContextItem = new ThreadLocal<ContextSwitchInfo>();

        private AsyncLocal<object> _ContextSwitchListener;

        private void ContextChangedHandler(AsyncLocalValueChangedArgs<object> args)
        {
            if (!args.ThreadContextChanged) return;
            
            int tid = Thread.CurrentThread.ManagedThreadId;
            if (args.PreviousValue == null)
            {
                ContextItem.Value = new ContextSwitchInfo()
                {
                    ThreadId = tid, 
                    StartAt = Stopwatch.StartNew(), 
                    UsageOnStart = CpuUsageReader.GetByThread().Value,
                };
            }
            else if (args.CurrentValue == null)
            {
                var contextOnStart = ContextItem.Value;
                ContextItem.Value = null;
                if (contextOnStart == null) throw new InvalidOperationException(
                    "CpuUsageAsyncWatcher.OnEnd: Missing contextOnStart. Please report");

                if (tid != contextOnStart.ThreadId) 
                    throw new InvalidOperationException(
                        $"CpuUsageAsyncWatcher.OnEnd: ContextItem.Value.ThreadId is not as expected." 
                        + $"Thread.CurrentThread.ManagedThreadId is {tid}. " 
                        + $"contextOnStart.ThreadId is {contextOnStart.ThreadId}. Please report");

                var ticks = contextOnStart.StartAt.ElapsedTicks;
                var duration = ticks / (double) Stopwatch.Frequency;
                var usageOnEnd = CpuUsageReader.GetByThread().Value;
                var cpuUsage = CpuUsage.Substruct(usageOnEnd, contextOnStart.UsageOnStart);
                ContextSwitchLogItem logRow = new ContextSwitchLogItem()
                {
                    Duration = duration, 
                    CpuUsage = cpuUsage
                };

                lock (_Log) _Log.Add(logRow);
            }

#if DEBUG
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Value Changed {(args.ThreadContextChanged ? $"WITH context #{tid}" : $"WITHOUT context #{tid}")}: {args.PreviousValue} => {args.CurrentValue}");
#endif            
        }

        public CpuUsageAsyncWatcher()
        {
            _ContextSwitchListener = new AsyncLocal<object>(ContextChangedHandler);
            _ContextSwitchListener.Value = "online";
        }

        public static bool IsSupported => true && CpuUsageReader.IsSupported;
#else
        public CpuUsageAsyncWatcher()
        {
        }

        public static bool IsSupported => false && CpuUsageReader.IsSupported;
#endif

    }

    public static class CpuUsageAsyncWatcherExtensions
    {
        public static CpuUsage GetSummaryCpuUsage(this IEnumerable<CpuUsageAsyncWatcher.ContextSwitchLogItem> log)
        {
            return CpuUsage.Sum(log.Select(x => x.CpuUsage));
        }  
        
        public static string ToHumanString(this ICollection<CpuUsageAsyncWatcher.ContextSwitchLogItem> log, int indent = 2, string taskDescription = "")
        {
            string pre = indent > 0 ? new string(' ', indent) : "";
            StringBuilder ret = new StringBuilder();
            ret.AppendLine($"Total Cpu Usage {(taskDescription?.Length > 0 ? $"of {taskDescription} " : "")}is {log.GetSummaryCpuUsage()}. Thread switches are:");
            int n = 0;
            int posLength = log.Count.ToString().Length;
            string posFormat = "{0,-" + posLength + "}";
            foreach (var l in log)
            {
                var delta = l.CpuUsage;
                double elapsed = l.Duration;
                double user = delta.UserUsage.TotalMicroSeconds / 1000d;
                double kernel = delta.KernelUsage.TotalMicroSeconds / 1000d;
                double perCents = (user + kernel) / 1000d / elapsed; 
                var cpuUsageInfo = $"{(elapsed*1000):n3} (cpu: {(perCents*100):f0}%, {(user+kernel):n3} = {user:n3} [user] + {kernel:n3} [kernel] milliseconds)";
                string posInfo = string.Format(posFormat, ++n);
                ret.AppendLine($"{pre}{posInfo}: {cpuUsageInfo}");
            }

            return ret.ToString();
        }

        public static string ToHumanString(this CpuUsageAsyncWatcher watcher, int indent = 2, string taskDescription = "")
        {
            return ToHumanString(watcher.Totals, indent, taskDescription);
        }
    }  
}
