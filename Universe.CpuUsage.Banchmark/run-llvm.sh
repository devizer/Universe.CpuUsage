# work=$HOME/build/devizer; mkdir -p $work; cd $work; git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage; git pull; bash run-llvm.sh  

cd ..
nuget restore || true; dotnet restore || true
cd Universe.CpuUsage.Banchmark
msbuild *.csproj /t:rebuild /p:Configuration=Release /v:q
cd bin/Release/net47
mono --llvm Universe.CpuUsage.Banchmark.exe
cd ..
cd ..



