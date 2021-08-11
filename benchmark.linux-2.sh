#!/usr/bin/env bash
# work=$HOME/build/devizer; mkdir -p $work; cd $work; git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage; git pull
# dotnet tool install -g BenchmarkDotNet.Tool
pushd Universe.CpuUsage.Banchmark
rm -rf bin/benchmark
dotnet publish -o bin/benchmark -c Release -f net5.0 --self-contained -r linux-arm64
pushd bin/benchmark 
./Universe.CpuUsage.Banchmark
popd
popd

