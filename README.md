# Universe.CpuUsage
CPU Usage for .NET Core, .NET Framework and Mono on Linux, Windows and macOS

## Crossplatform CPU Usage metrics
It receives the amount of time that the current **thread/process** has executed in _**kernel**_ and _**user**_ mode.

Works everywhere: Linux, OSX and Windows.

Targets everywhere: Net Framework 2.0+, Net Standard 1.3+, Net Core 1.0+

## Coverage and supported OS
Minimum OS requirements: Linux Kernel 2.6.26, Mac OS 10.9, Windows XP/2003

Autotests using .NET Core cover:
- Windows Server 2016 on appveyor
- Linux x64 and Windows Server 2016 on appveyor
- Linux Arm 64-bit using .net core on travis-ci.org
- Linux Arm 64-bit, Arm-v7 32 bit, i386 using mono on travis-ci.org
- macOS X x64 10.10 (mono only) & 10.14 (both .net core and mono) using travis-ci.org

It should work on BSD-like system with linux compatibility layer using mono, but was never tested. 
 
## Implementation
The implementation utilizes platform invocation of the corresponding system libraries depending on the OS:

| OS       | per thread implementation  | per process implementation   | library         |
|----------|--------------------------|------------------------|-----------------|
| Linux    | getrusage(RUSAGE_THREAD) | getrusage(RUSAGE_SELF) | libc.so         |
| Windows  | GetThreadTimes()         | GetProcessTimes()      | kernel32.dll    |
| Mac OS X | thread_info()            | getrusage(RUSAGE_SELF) | libSystem.dylib |

## Benchmark 
Benchmark below and comparison to well known DateTime.Now and Stopwatch are taken using .NET Core 3.0 runtime.

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
