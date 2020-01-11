using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Universe.CpuUsage.Tests.Statistica
{
    public class HistogramReport<T>
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

        public HistogramReport(Func<double, string> intervalAsString, double min, double max, double mean, double variance, double filteredMin, double filteredMax, long lowerOutliers, ICollection<RangePart> histogram, long highOutliers)
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
                // var perCentsAsString = count == 0 ? "--" : (" " + perCents.ToString("f1") + "%");
                var perCentsAsString = count == 0 ? "" : perCents.ToString("f1") + "%";

                bars.Add(String.Format(" {0,6} | {1}",
                    perCentsAsString,
                    len > 0 ? new string('@', len) : ""
                ));
            };

            if (LowerOutliers > 0)
            {
                captions.Add($"Lower Outliers");
                addBar(LowerOutliers);
            }

            foreach (var rangePart in Histogram)
            {
                Func<double, string> valueToString = v => String.Format("{0,-" + maxFormattedValueLength + "}", IntervalAsString(v));
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