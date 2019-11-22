@echo off
pushd Universe.CpuUsage.Banchmark
set fw=net47
set fw=netcoreapp2.0
rd /q /s "bin\benchmark"
dotnet publish -o bin/benchmark -c Release -f %fw%
pushd bin\benchmark
rem dotnet benchmark Universe.CpuUsage.Banchmark.dll --runtimes %fw% --filter *CpuUsageBenchmarks*
dotnet benchmark Universe.CpuUsage.Banchmark.dll --filter *CpuUsageBenchmarks*
rem dotnet benchmark Universe.CpuUsage.Banchmark.dll --warmupCount 2 --unrollFactor 8 --monoPath "C:\Program Files\Mono\bin\mono.exe" --runtimes Mono --filter *ByThread*
rem --warmupCount 2 --unrollFactor 8
popd
popd

