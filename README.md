# Universe.CpuUsage &nbsp;&nbsp;&nbsp; [![Nuget](https://img.shields.io/nuget/v/Universe.CpuUsage?label=nuget.org)](https://www.nuget.org/packages/Universe.CpuUsage/)
CPU Usage for .NET Core, .NET Framework and Mono on Linux, Windows and macOS

## Crossplatform CPU Usage metrics
It receives the amount of time that the current **thread(s)/process** has executed in _**kernel**_ and _**user**_ mode.

Works everywhere: Linux, OSX and Windows.

Targets everywhere: Net Framework 2.0+, Net Standard 1.3+, Net Core 1.0+

## Coverage and supported OS
Minimum OS requirements: Linux Kernel 2.6.26, Mac OS 10.9, Windows XP/2003

Autotests using .NET Core and .NET Framework cover:
- Linux x86_64 and Arm 64-bit on appveyor
- Windows Server 2016 on appveyor
- macOS 10.14 on travis-ci.org
- Raspbian 10 Buster on Azure Piplines using self-hosted agent on Raspberry pi

Autotests using Mono:
- Linux x86_64, Arm 64-bit, Arm-v7 32 bit, i386 using mono on travis-ci.org
- Mac OSX 10.10 using travis-ci.org

Manually tested on:
- Windows 7 x86 (.NET Core), Windows 10 ARM64 (.NET Core)
- FreeBSD 12 (both .NET Core and Mono).
- Debian 8 on armv5 (Mono)


| appveyor                   | travis-ci                                                                                 |
|----------------------------|-------------------------------------------------------------------------------------------|
| .NET Core: **Linux** x64, **Windows** x64. <br>Mono: **Linux** x64. | .NET Core: **macOS** 10.14, **Linux** Arm 64. <br>Mono: **Linux** Arm 64, Arm-v7, i386, **macOS** 10.10. |
| <br><p align="center">[![Build status](https://ci.appveyor.com/api/projects/status/udq3dip23mqxlkjf?svg=true)](https://ci.appveyor.com/project/devizer/universe-cpuusage)</p> | <br><p align="center">[![Build Status](https://travis-ci.org/devizer/Universe.CpuUsage.svg?branch=master)](https://travis-ci.org/devizer/Universe.CpuUsage)</p> |

### Integration tests on exotic platforms
For ***mono only*** platforms (i386, ppc64, mips, arm v5/v6, etc) here is the script for integration tests: [test-on-mono-only-platforms.sh](https://raw.githubusercontent.com/devizer/Universe.CpuUsage/master/test-on-mono-only-platforms.sh)
```
url=https://raw.githubusercontent.com/devizer/Universe.CpuUsage/master/test-on-mono-only-platforms.sh;
(wget -q -nv --no-check-certificate -O - $url 2>/dev/null || curl -ksSL $url) | bash
```

## Implementation
The implementation utilizes platform invocation (P/Invoke) of the corresponding system libraries depending on the OS:

| OS       | per thread implementation  | per process implementation   | library         |
|----------|--------------------------|------------------------|-----------------|
| Linux    | getrusage(RUSAGE_THREAD) | getrusage(RUSAGE_SELF) | libc.so         |
| Windows  | GetThreadTimes()         | GetProcessTimes()      | kernel32.dll    |
| Mac OS X | thread_info()            | getrusage(RUSAGE_SELF) | libSystem.dylib |

## Precision depends on
Here is a summary of CPU usage precision. In general, it depends on OS and version and it does not depend on CPU performance except of FreeBSD

| OS                                                                         | Average Precision |
|----------------------------------------------------------------------------|------------------:|
| Windows Server 2019, Xeon E5-2697 v3                                       |       _16,250 μs_ |
| Linux, i5-10300, 4.0 GHz, kernel 6.8                                       |        _1,000 μs_ |
| Ubuntu, x86-64 QEMU, kernel 6.8                                            |        _1,000 μs_ |
| Debian, x86-64 QEMU, kernel 6.10                                           |        _4,000 μs_ |
| Ubuntu, arm64 QEMU, kernel 6.8                                             |        _1,000 μs_ |
| Debian, arm64 QEMU, kernel 6.10                                            |        _4,000 μs_ |
| Linux, Xeon E5-2697 v3 @ 2.60GHz, kernel 5.0                               |        _4,000 μs_ |
| Linux, ARMv7 H3 CPU, 1.50GHz, kernel 3.4                                   |       _10,000 μs_ |
| Linux, ARMv7 H3 CPU, 1.10GHz, kernel 4.19                                  |        _4,000 μs_ |
| Linux, ARMv7 H3 CPU, 1.30GHz, kernel 6.13                                  |        _4,000 μs_ |
| Mac OS 10.14, Xeon E5-2697 v2 @ 2.70GHz                                    |           14.0 μs |
| Mac OS 15, Apple M1                                                        |            3.0 μs |
| FreeBSD 12, .NET Core 2.0, Xeon E3-1270 v2 @ 3.50GHz, pseudo kernel 2.6.32 |            3.0 μs |
| FreeBSD 12, Mono 5.1, Xeon E3-1270 v2 @ 3.50GHz, native BSD 12             |            1.9 μs |

Detailed histograms of precision are produced by PrecisionTest.cs

## Low level API: class CpuUsage
```csharp
var onStart = CpuUsage.GetByThread();
... 
var onEnd = CpuUsage.GetByThread();
Console.WriteLine("CPU Usage: " + (onEnd-onStart));
```

## High level API: class CpuUsageAsyncWatcher
```csharp
public void Configure(IApplicationBuilder app)
{
    // Here is a "middleware" that displays total CPU usage
    // of all the Tasks executed by ASP.NET Core pipeline during each http request
    app.Use(async (context, next) =>
    {
        CpuUsageAsyncWatcher watcher = new CpuUsageAsyncWatcher();
        await next.Invoke();
        watcher.Stop();
        Console.WriteLine($"Cpu Usage by http request is {watcher.GetSummaryCpuUsage()}");
    });
}
```

## Benchmark 
Just for illustration here is a comparison to well known DateTime.Now and Stopwatch using benchmark.net. All of them are taken using .NET Core 3.0 runtime.

#### Linux x64, kernel 4.15
|      Method |      Mean |    Error |   StdDev | Rank |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------ |----------:|---------:|---------:|-----:|-------:|------:|------:|----------:|
| DateTime.Now() | 150.24 ns | 2.346 ns | 2.194 ns |    2 |      - |     - |     - |         - |
|   Stopwatch |  77.27 ns | 0.473 ns | 0.419 ns |    1 | 0.0095 |     - |     - |      40 B |
| Process CPU Usage | 795.18 ns | 8.000 ns | 6.681 ns |    3 |      - |     - |     - |         - |
| Thread CPU Usage| 834.59 ns | 9.324 ns | 8.722 ns |    4 |      - |     - |     - |         - |

#### Linux 32 bit @ ARM, kernel 3.4 (Orange PI, H3, 1500 MHz)
|      Method |     Mean |     Error |    StdDev | Rank |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------ |---------:|----------:|----------:|-----:|-------:|------:|------:|----------:|
| DateTime.Now | 2.788  μs | 0.0595  μs | 0.0944  μs |    2 |      - |     - |     - |         - |
|   Stopwatch | 1.737  μs | 0.0539  μs | 0.0504  μs |    1 | 0.1945 |     - |     - |      32 B |
| Process CPU Usage | 5.552  μs | 0.1662  μs | 0.4900  μs |    3 |      - |     - |     - |         - |
|    Thread CPU Usage | 5.664  μs | 0.1136  μs | 0.1960  μs |    3 |      - |     - |     - |         - |

#### macOS 10.14
|      Method |        Mean |     Error |    StdDev | Rank |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------ |------------:|----------:|----------:|-----:|-------:|------:|------:|----------:|
| DateTime.Now() |    73.40 ns |  1.506 ns |  2.475 ns |    1 |      - |     - |     - |         - |
|   Stopwatch |    79.36 ns |  1.640 ns |  1.454 ns |    2 | 0.0025 |     - |     - |      40 B |
|   Process CPU Usage | 1,979.10 ns | 29.771 ns | 26.391 ns |    4 |      - |     - |     - |         - |
|    Thread CPU Usage | 1,921.54 ns | 35.869 ns | 33.552 ns |    3 |      - |     - |     - |         - |

#### Windows Server 2016
|      Method |      Mean |    Error |   StdDev | Rank |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------ |----------:|---------:|---------:|-----:|-------:|------:|------:|----------:|
| DateTime.Now() | 217.08 ns | 1.667 ns | 1.559 ns |    4 |      - |     - |     - |         - |
|   Stopwatch |  31.12 ns | 0.203 ns | 0.169 ns |    1 | 0.0095 |     - |     - |      40 B |
|   Process CPU Usage | 200.49 ns | 3.743 ns | 3.501 ns |    2 |      - |     - |     - |         - |
|    Thread CPU Usage | 205.11 ns | 3.970 ns | 4.413 ns |    3 |      - |     - |     - |         - |

##### Legend
- Stopwatch: `var sw = new Stopwatch(); var ticks = sw.ElapsedTicks;`
- Process CPU Usage: `CpuUsageReader.GetByProcess();`
- Thread CPU Usage: `CpuUsageReader.GetByThread();`
- ns - nanosecond,  μs - microsecond
