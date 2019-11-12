#!/usr/bin/env bash
# work=$HOME/build/devizer; rm -rf $work; mkdir -p $work; cd $work; git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage
# dotnet tool install -g BenchmarkDotNet.Tool
pushd Universe.CpuUsage.Banchmark
dotnet publish -o bin/benchmark -c Release -f netcoreapp3.0
pushd bin/benchmark 
dotnet benchmark Universe.CpuUsage.Banchmark.dll --filter *CpuUsageBenchmarks*
popd
popd

