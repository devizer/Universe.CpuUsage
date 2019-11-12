@echo off
pushd Universe.CpuUsage.Banchmark
dotnet publish -o bin/benchmark -c Release
pushd bin\benchmark 
dotnet benchmark Universe.CpuUsage.Banchmark.dll --filter *CpuUsageBenchmarks*
popd
popd

