using System;
using System.Globalization;

namespace Universe.CpuUsage.Tests
{
    public class Env
    {
        // qemu emulator: 0.2, otherwise 0.1
        public static double CpuUsagePrecision = _CpuUsagePrecision.Value;

        private static Lazy<double> _CpuUsagePrecision = new Lazy<double>(() =>
        {
            var raw = Environment.GetEnvironmentVariable("CpuUsagePrecisionForAssert");
            var enUS = new CultureInfo("en-US");
            if (!string.IsNullOrEmpty(raw))
                if (double.TryParse(raw, NumberStyles.AllowDecimalPoint, enUS, out var ret))
                    return ret > 0 ? ret : 0.1d;

            return 0.1d;
        });
    }
}