using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
        
        private List<ContextSwitchLogItem> _Log = new List<ContextSwitchLogItem>(); 
        public class ContextSwitchLogItem
        {
            public double Duration { get; internal set; }
            public CpuUsage CpuUsage { get; internal set; }
        }

#if NETSTANDARD2_0 || NETSTANDARD2_1 || NET_4_8 || NET_4_7_2 || NET_4_7_1 || NET_4_7 || NET_4_6_2 || NET_4_6_1 || NET_4_6

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
                    "CpuUsageAsyncWatcher.OnEnd: Missing contextOnStart");

                if (tid != contextOnStart.ThreadId) 
                    throw new InvalidOperationException(
                        $"CpuUsageAsyncWatcher.OnEnd: ContextItem.Value.ThreadId is not as expected." 
                        + $"Thread.CurrentThread.ManagedThreadId is {tid}. " 
                        + $"contextOnStart.ThreadId is {contextOnStart.ThreadId}.");

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

        public bool IsSupported => true;
#else
        public CpuUsageAsyncWatcher()
        {
        }

        public bool IsSupported => false;
#endif

    }
    
}