echo '<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Universe.CpuUsage" value="https://ci.appveyor.com/nuget/Universe.CpuUsage" />
  </packageSources>
</configuration>
' > nuget.config

mkdir -p bin
pushd bin
nuget install Universe.CpuUsage || nuget install Universe.CpuUsage || true
cd Universe.CpuUsage*/lib 
rm -rf net47 net472 net48 netcoreapp3.0 netstandard2.1
mkdir -p  ../../../../../Test4Linux/bin
cp -r ./. ../../../../../Test4Linux/bin/
popd

cd ../Tests4Linux
nuget restore
pushd ~/build/devizer/Universe.CpuUsage; find . ; popd
msbuild /t:Rebuild /p:Configuration=Release
