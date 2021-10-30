using System;
using System.Globalization;

namespace Universe.CpuUsage.Tests
{
    public class Env
    {
        // qemu emulator: 0.2, otherwise 0.1
        public static double CpuUsagePrecision => _CpuUsagePrecision.Value;

        private static Lazy<double> _CpuUsagePrecision = new Lazy<double>(() =>
        {
            double ret = 0.1d;
            var raw = Environment.ExpandEnvironmentVariables("CpuUsagePrecisionForAssert");
            var enUS = new CultureInfo("en-US");
            if (!string.IsNullOrEmpty(raw))
                if (double.TryParse(raw, NumberStyles.AllowDecimalPoint, enUS, out ret))
                {
                }

            ret = ret > 0 ? ret : 0.1d;
            Console.WriteLine($"CpuUsagePrecisionForAssert: {ret}");
            return ret;
        });
    }
}