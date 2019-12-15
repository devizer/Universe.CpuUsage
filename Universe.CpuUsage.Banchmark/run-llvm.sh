# work=$HOME/build/devizer; mkdir -p $work; cd $work; git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage; git pull; cd Universe.CpuUsage.Banchmark 

msbuild Universe.CpuUsage.Banchmark.csproj /t:rebuild /p:Configuration=Release
cd bin/Release/net47
mono --llvm Universe.CpuUsage.Banchmark.dll




