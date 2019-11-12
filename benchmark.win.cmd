@echo off
pushd Universe.CpuUsage.Banchmark
set fw=netcoreapp2.0
set fw=net47
rd /q /s "bin\benchmark"
dotnet publish -o bin/benchmark -c Release -f %fw%
pushd bin\benchmark
rem dotnet benchmark Universe.CpuUsage.Banchmark.dll --runtimes net47 netcoreapp3.0 Mono --filter *CpuUsageBenchmarks*
dotnet benchmark Universe.CpuUsage.Banchmark.dll --warmupCount 2 --unrollFactor 8 --monoPath "C:\Program Files\Mono\bin\mono.exe" --runtimes Mono --filter *ByThread*
rem --warmupCount 2 --unrollFactor 8
popd
popd

