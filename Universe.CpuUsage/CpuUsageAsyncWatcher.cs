// ReSharper disable PossibleInvalidOperationException

namespace Universe.CpuUsage
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using System.Linq;

    public class CpuUsageAsyncWatcher
    {
        private volatile bool IsRunning = true;
        public ICollection<ContextSwitchMetrics> Totals
        {
            get
            {
                lock (_Log)
                {
                    return new List<ContextSwitchMetrics>(_Log);
                }
            }
        }

        public CpuUsage GetSummaryCpuUsage()
        {
            return Totals.GetSummaryCpuUsage();
        }  
        
        private List<ContextSwitchMetrics> _Log = new List<ContextSwitchMetrics>(); 
        public class ContextSwitchMetrics
        {
            public double Duration { get; internal set; }
            public CpuUsage CpuUsage { get; internal set; }
        }
        
        public void Stop()
        {
            IsRunning = false;
        }
        
#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
        public static bool IsSupported => CpuUsageReader.IsSupported && LegacyNetStandardInterop.IsSupported && IsFrameworkSupported;
        static long GetThreadId() => LegacyNetStandardInterop.GetThreadId();        
#else
        static long GetThreadId() => Thread.CurrentThread.ManagedThreadId;
        public static bool IsSupported => CpuUsageReader.IsSupported && IsFrameworkSupported;
#endif        


// legacy net framework [2.0 ... 4.6) is not supported 
#if NETCOREAPP || NETSTANDARD2_0 || NETSTANDARD2_1 || NET48 || NET472 || NET471 || NET47 || NET462 || NET461 || NET46 || NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6

        const bool IsFrameworkSupported = true;

        private class ContextSwitchInfo
        {
            public long ThreadId;
            public Stopwatch StartAt;
            public CpuUsage UsageOnStart;
        }

        private static ThreadLocal<ContextSwitchInfo> ContextItem = new ThreadLocal<ContextSwitchInfo>();

        private AsyncLocal<object> _ContextSwitchListener;

        private void ContextChangedHandler(AsyncLocalValueChangedArgs<object> args)
        {
            // if (!args.ThreadContextChanged) return;
            if (!IsRunning) return;
            
            long tid = GetThreadId();
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
                ContextSwitchMetrics logRow = new ContextSwitchMetrics()
                {
                    Duration = duration, 
                    CpuUsage = cpuUsage
                };

                lock (_Log) _Log.Add(logRow);
            }

#if DEBUG
            Console.ForegroundColor = ConsoleColor.Cyan;
            string AsString(object value) => value == null ? "off" : Convert.ToString(value); 
            Console.WriteLine($"Value Changed {(args.ThreadContextChanged ? $"WITH context #{tid}" : $"WITHOUT context #{tid}")}: {AsString(args.PreviousValue)} => {AsString(args.CurrentValue)}");
#endif            
        }

        public CpuUsageAsyncWatcher()
        {
            _ContextSwitchListener = new AsyncLocal<object>(ContextChangedHandler);
            _ContextSwitchListener.Value = "Online";
        }
        
#else
        const bool IsFrameworkSupported = false;
        public CpuUsageAsyncWatcher()
        {
        }
        
#endif

    }

    public static class CpuUsageAsyncWatcherExtensions
    {
        public static CpuUsage GetSummaryCpuUsage(this IEnumerable<CpuUsageAsyncWatcher.ContextSwitchMetrics> log)
        {
            return log == null 
                ? new CpuUsage() 
                : CpuUsage.Sum(log.Select(x => x.CpuUsage));
        }  
        
        public static string ToHumanString(this ICollection<CpuUsageAsyncWatcher.ContextSwitchMetrics> log, int indent = 2, string taskDescription = "")
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (indent < 0) throw new ArgumentException("indent should be zero or positive number", nameof(indent));
            taskDescription = taskDescription ?? string.Empty;
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
            if (watcher == null) throw new ArgumentNullException(nameof(watcher));
            return ToHumanString(watcher.Totals, indent, taskDescription);
        }
    }  
}
