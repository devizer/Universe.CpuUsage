using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Console = System.Console;

namespace Universe.CpuUsage.MonoTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var c = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"The Platform: {CrossInfo.ThePlatform}");
            Console.WriteLine($"CPU Usage by Process: {CpuUsageReader.GetByProcess()}");
            Console.WriteLine($"CPU Usage by Thread: {CpuUsageReader.GetByThread()}");
            Console.BackgroundColor = c;
        }
    }
}
