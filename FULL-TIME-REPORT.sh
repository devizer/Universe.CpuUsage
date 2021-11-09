export FULL_TIME="
TIME:
Elapsed real time %E
Elapsed real time %e
Total number of CPU-seconds that the process spent in kernel mode %S
Total number of CPU-seconds that the process spent in user mode %U
Percentage of the CPU that this job got %P

MEMORY:
Maximum resident set size of the process during its lifetime (in Kbytes) %M
Average resident set size of the process (in Kbytes) %t
Average total (data+stack+text) memory use of the process (in Kbytes) %K
Average size of the process's unshared data area (in Kbytes) %D
Average size of the process's unshared stack space (in Kbytes) %p
Average size of the process's shared text space (in Kbytes) %X
System's page size, in bytes. This is a per-system constant, but varies between systems. %Z
Number of major page faults. %F
Number of minor page faults. %R
Number of times the process was swapped out of main memory. %W
Number of times the process was context-switched involuntarily (because the time slice expired). %c
Number of waits: times that the program was context-switched voluntarily, for instance while waiting for an I/O operation to complete. %w

I/O:
Number of file system inputs by the process. %I
Number of file system outputs by the process. %O
Number of socket messages received by the process. %r
Number of socket messages sent by the process. %s
Number of signals delivered to the process. %k
Name and command-line arguments of the command being timed. [%C]
Exit status of the command. %x
"
# sudo apt-get install time -y
