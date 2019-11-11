# work=$HOME/build/devizer; rm -rf $work mkdir -p $work; cd $work; git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage/Tests4Mac

echo '<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Universe.CpuUsage" value="https://ci.appveyor.com/nuget/Universe.CpuUsage" />
  </packageSources>
</configuration>
' > nuget.config


mkdir -p bin ../Tests4Linux/bin
pushd bin
nuget install Universe.CpuUsage || nuget install Universe.CpuUsage || true
cd Universe.CpuUsage*/lib 
rm -rf net47 net472 net48 netcoreapp3.0 netstandard2.1
mkdir -p  ../../../../Tests4Linux/bin
cp -r ./. ../../../../Tests4Linux/bin/
popd

cd ../Tests4Linux
nuget restore
pushd ~/build/devizer/Universe.CpuUsage; find . ; popd
msbuild /t:Rebuild /p:Configuration=Release
