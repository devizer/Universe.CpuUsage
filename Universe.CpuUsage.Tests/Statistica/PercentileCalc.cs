using System;
using System.Collections.Generic;
using System.Linq;

namespace Universe.CpuUsage.Tests.Statistica
{
    public class PercentileCalc<T>
    {
        public readonly ICollection<T> Source;
        public readonly Func<T, double> AsNumber;
        public readonly Func<double, string> HistogramIntervalAsString;
        
        private List<double> Sorted = new List<double>();

        public PercentileCalc(ICollection<T> source, Func<T, double> asNumber, Func<double, string> histogramIntervalAsString)
        {
            Source = source;
            AsNumber = asNumber;
            HistogramIntervalAsString = histogramIntervalAsString;
            Sorted = source.Select(x => AsNumber(x)).OrderBy(x => x).ToList();
        }

        public double this[double percentile]
        {
            get
            {
                if (percentile < 0 || percentile>100) throw new ArgumentException();
                int pos = (int) (Sorted.Count / 100d * percentile);
                pos = Math.Min(pos, Sorted.Count - 1);
                return Sorted[pos];
            }
        }
    }
}