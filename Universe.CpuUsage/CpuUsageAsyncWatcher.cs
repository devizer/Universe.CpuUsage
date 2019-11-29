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
        public List<ContextSwitchLogItem> Log
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
            return CpuUsage.Sum(Log.Select(x => x.CpuUsage));
        }  
        
        private List<ContextSwitchLogItem> _Log = new List<ContextSwitchLogItem>(); 
        public class ContextSwitchLogItem
        {
            public double Duration { get; internal set; }
            public CpuUsage CpuUsage { get; internal set; }
        }

#if NETCOREAPP || NETSTANDARD2_0 || NETSTANDARD2_1 || NET_4_8 || NET_4_7_2 || NET_4_7_1 || NET_4_7 || NET_4_6_2 || NET_4_6_1 || NET_4_6

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

        public static bool IsSupported => true;
#else
        public CpuUsageAsyncWatcher()
        {
        }

        public static bool IsSupported => false;
#endif

    }

    public static class CpuUsageAsyncWatcherExtensions
    {
        public static string ToHumanString(this CpuUsageAsyncWatcher watcher, int indent = 2)
        {
            string pre = indent > 0 ? new string(' ', indent) : "";
            StringBuilder ret = new StringBuilder();
            ret.AppendLine($"Total Cpu Usage is {watcher.GetTotalCpuUsage()}. Thread switches are:");
            int n = 0;
            int posLength = watcher.Log.Count.ToString().Length;
            string posFormat = "{0,-" + posLength + "}";
            foreach (var l in watcher.Log)
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
    }  
    
}