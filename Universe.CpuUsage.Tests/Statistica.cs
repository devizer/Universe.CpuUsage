using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Universe.CpuUsage.Tests
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

        public Report BuildReport_Wrong(int intervalCount = 15, double variances = 3)
        {
            var doubles = Source.Select(x => AsNumber(x));
            var count = doubles.Count();
            var min = doubles.Min();
            var max = doubles.Max();
            var avg = doubles.Average();
            var variance = doubles.Aggregate(0d, (result, item) => result + Math.Pow(item - avg, 2), result => Math.Sqrt(result) / count);
            var filteredMin = avg - variance * variances;
            var filteredMax = avg + variance * variances;
            var filteredDistance = filteredMax - filteredMin;

            var lowerOutliersCount = doubles.Count(x => (x - filteredMin) <= -Double.Epsilon);
            var highOutliersCount = doubles.Count(x => (x - filteredMax) >= Double.Epsilon);

            var filteredDoubles = doubles.Where(x => (x - filteredMin) >= -Double.Epsilon && (x - filteredMax) <= Double.Epsilon).ToArray();
            List<RangePart> intervals = new List<RangePart>();
            for (int i = 0; i < intervalCount; i++)
            {
                double from = filteredMin + i * filteredDistance / intervalCount;
                double to = filteredMin + (i + 1) * filteredDistance / intervalCount;
                // var filtered4interval = filteredDoubles.Where(x => (x - from) >= -Double.Epsilon && (x - to) <= Double.Epsilon).ToArray();
                // long perIntervalCount = filtered4interval.Length;
                // double perCents = intervalCount * 100d / filteredDoubles.Length;
                // var rangePart = new RangePart(i, @from, to, perIntervalCount);
                var rangePart = new RangePart(i, @from, to, 0);
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

            return new Report(
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
        public Report BuildReport(int intervalCount = 15, double variances = 3)
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
            List<RangePart> intervals = new List<RangePart>();
            for (int i = 0; i < intervalCount; i++)
            {
                double from = filteredMin + i * filteredDistance / intervalCount;
                double to = filteredMin + (i + 1) * filteredDistance / intervalCount;
                // var filtered4interval = filteredDoubles.Where(x => (x - from) >= -Double.Epsilon && (x - to) <= Double.Epsilon).ToArray();
                // long perIntervalCount = filtered4interval.Length;
                // double perCents = intervalCount * 100d / filteredDoubles.Length;
                // var rangePart = new RangePart(i, @from, to, perIntervalCount);
                var rangePart = new RangePart(i, @from, to, 0);
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

            return new Report(
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

        public class Report
        {
            public readonly Func<double, string> IntervalAsString;

            public double Min { get; private set; }
            public double Max { get; private set; }
            public double Mean { get; private set; }
            public double Variance { get; private set; }
            public double FilteredMin { get; private set; }
            public double FilteredMax { get; private set; }

            public long LowerOutliers { get; private set; }
            public ICollection<RangePart> Histogram { get; private set; }
            public long HighOutliers { get; private set; }

            public Report(Func<double, string> intervalAsString, double min, double max, double mean, double variance, double filteredMin, double filteredMax, long lowerOutliers, ICollection<RangePart> histogram, long highOutliers)
            {
                IntervalAsString = intervalAsString;
                Min = min;
                Max = max;
                Mean = mean;
                Variance = variance;
                FilteredMin = filteredMin;
                FilteredMax = filteredMax;
                LowerOutliers = lowerOutliers;
                Histogram = histogram;
                HighOutliers = highOutliers;
            }

            public string ToConsole(string caption, int barWidth = 42, int indent = 5)
            {
                string pre = indent > 0 ? new string(' ', indent) : "";
                var maxCount = Math.Max(LowerOutliers, HighOutliers);
                maxCount = Math.Max(maxCount, Histogram.Max(x => x.Count));

                var sumCount = LowerOutliers + HighOutliers + Histogram.Sum(x => x.Count);

                var allFormattedValues = Histogram.Select(x => IntervalAsString(x.From)).Concat(Histogram.Select(x => IntervalAsString(x.To)));
                var maxFormattedValueLength = allFormattedValues.Max(x => x.Length);


                List<string> captions = new List<string>();
                List<string> bars = new List<string>();

                Action<long> addBar = count =>
                {
                    var lenD = count * 1d * barWidth / maxCount;
                    var len = (int) lenD;
                    var perCents = count * 100d / sumCount;
                    var perCentsAsString = count == 0 ? "--" : (" " + perCents.ToString("f0") + "%");
                    bars.Add((len > 0 ? new string('@', len) : "") + perCentsAsString);
                };

                if (LowerOutliers > 0)
                {
                    captions.Add($"Lower Outliers");
                    addBar(LowerOutliers);
                }

                foreach (var rangePart in Histogram)
                {
                    Func<double, string> valueToString = v => string.Format("{0,-" + maxFormattedValueLength + "}", IntervalAsString(v));
                    captions.Add($"{valueToString(rangePart.From)} ... {valueToString(rangePart.To)}");
                    addBar(rangePart.Count);
                }

                if (LowerOutliers > 0)
                {
                    captions.Add($"Higher Outliers");
                    addBar(HighOutliers);
                }

                // var maxFormattedValue = 
                var maxCaptionWidth = captions.Max(x => x.Length);
                maxCaptionWidth = Math.Max(2, maxCaptionWidth);

                StringBuilder ret = new StringBuilder();
                ret.AppendLine($"{pre}Histogram '{caption}'");

                for (int i = 0; i < captions.Count; i++)
                {
                    ret.AppendFormat("{2}{0,-" + maxCaptionWidth + "} {1}", captions[i], bars[i], pre).AppendLine();
                }

                return ret.ToString();
            }


        }

        public class RangePart
        {
            public int No { get; private set; }
            public double From { get; private set; }
            public double To { get; private set; }
            public long Count { get; internal set; }

            public RangePart(int no, double @from, double to, long count)
            {
                No = no;
                From = @from;
                To = to;
                Count = count;
            }
        }

    }
}
