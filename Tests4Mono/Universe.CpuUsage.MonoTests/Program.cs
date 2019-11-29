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
            Console.WriteLine($"The Platform: {CrossInfo.ThePlatform}");
            Console.WriteLine($"Process: {IntPtr.Size * 8} bits");
            Console.WriteLine($"CPU Usage by Process: {CpuUsage.GetByProcess()}");
            Console.WriteLine($"CPU Usage by Thread: {CpuUsage.GetByThread()}");
        }
    }
}
