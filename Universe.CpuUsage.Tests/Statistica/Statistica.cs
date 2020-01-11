using System;
using System.Collections.Generic;
using System.Linq;

namespace Universe.CpuUsage.Tests.Statistica
{
    public class Statistica<T>
    {
        public readonly ICollection<T> Source;
        public readonly Func<T, double> AsNumber;
        public readonly Func<double, double> RoundHistogramInterval;
        public readonly Func<double, string> HistogramIntervalAsString;

        // public static Statistica<T> Create()

        public Statistica(ICollection<T> source, Func<T, double> asNumber, Func<double, double> roundHistogramInterval, Func<double, string> histogramIntervalAsString)
        {
            Source = source;
            AsNumber = asNumber;
            RoundHistogramInterval = roundHistogramInterval;
            HistogramIntervalAsString = histogramIntervalAsString;
        }

        public HistogramReport<T> BuildReport(int intervalCount = 15, double variances = 3)
        {
            var doubles = Source.Select(x => AsNumber(x));
            var count = doubles.Count();
            var min = doubles.Min();
            var max = doubles.Max();
            var avg = doubles.Average();
            var variance = doubles.Aggregate(0d, (result, item) => result + Math.Pow(item - avg, 2), result => Math.Sqrt(result) / count);
            var filteredMin = min;
            var filteredMax = max;
            var filteredDistance = filteredMax - filteredMin;

            var lowerOutliersCount = doubles.Count(x => (x - filteredMin) <= -Double.Epsilon);
            var highOutliersCount = doubles.Count(x => (x - filteredMax) >= Double.Epsilon);

            var filteredDoubles = doubles.Where(x => (x - filteredMin) >= -Double.Epsilon && (x - filteredMax) <= Double.Epsilon).ToArray();
            List<HistogramReport<T>.RangePart> intervals = new List<HistogramReport<T>.RangePart>();
            for (int i = 0; i < intervalCount; i++)
            {
                double from = filteredMin + i * filteredDistance / intervalCount;
                double to = filteredMin + (i + 1) * filteredDistance / intervalCount;
                // var filtered4interval = filteredDoubles.Where(x => (x - from) >= -Double.Epsilon && (x - to) <= Double.Epsilon).ToArray();
                // long perIntervalCount = filtered4interval.Length;
                // double perCents = intervalCount * 100d / filteredDoubles.Length;
                // var rangePart = new RangePart(i, @from, to, perIntervalCount);
                var rangePart = new HistogramReport<T>.RangePart(i, @from, to, 0);
                intervals.Add(rangePart);
            }

            foreach (var filteredDouble in filteredDoubles)
            {
                int intervalIndex = 0;
                var i = 0;
                foreach (var rangePart in intervals)
                {
                    if (filteredDouble - rangePart.From >= -Double.Epsilon && filteredDouble - rangePart.To <= 0d)
                    {
                        intervalIndex = i;
                        break;
                    }

                    i++;
                }

                intervals[intervalIndex].Count = intervals[intervalIndex].Count + 1;
            }

            int keepFrom = 0;
            while (keepFrom < intervals.Count && intervals[keepFrom].Count == 0)
                keepFrom++;

            int keepTo = intervals.Count - 1;
            while (keepTo >= 0 && intervals[keepTo].Count == 0)
                keepTo--;

            var ret = intervals.AsEnumerable();
            if (keepFrom > 0) ret.Skip(keepFrom);
            ret = ret.Take(keepTo - keepFrom + 1);

            return new HistogramReport<T>(
                HistogramIntervalAsString,
                min,
                max,
                avg,
                variance,
                filteredMin,
                filteredMax,
                lowerOutliersCount,
                ret.ToArray(),
                highOutliersCount);
        }
    }
}
