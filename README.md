# Universe.CpuUsage
CpuUsage for .NET Core and .NET Framework for Linux, Windows and macOS

## Crossplatform CPU Usage metrics
It receives the amount of time that the current **thread/process** has executed in _**kernel**_ and _**user**_ mode.

Works everywhere: Linux, OSX and Windows.

Targets everywhere: Net Framework 2.0+, Net Standard 1.3+, Net Core 1.0+

## Coverage
Minimum OS requirements: Linux Kernel 2.6.26, Mac OS 10.9, Windows XP/2003

Autotests using .NET Core cover:
- Linux on x64 using plenty linux distributions 
- Linux on ARM 64-bit using Debian
- OS X x64 10.13 & 10.14
- Windows 2012 R2 (x64)

ARM 32-bit Linux is manually tested only (because multiarch docker images does not work properly) 

It should be supported on Linux x86 and BSD-like system with linux compatibility layer using mono, but was never tested. 
 
## Implementation
The implementation utilizes platform invocation of the corresponding system libraries depending on the OS:

| OS       | per thread function      | per process function   | library         |
|----------|--------------------------|------------------------|-----------------|
| Linux    | getrusage(RUSAGE_THREAD) | getrusage(RUSAGE_SELF) | libc.so         |
| Windows  | GetThreadTimes()         | GetProcessTimes()      | kernel32.dll    |
| Mac OS X | thread_info()            | getrusage(RUSAGE_SELF) | libSystem.dylib |
