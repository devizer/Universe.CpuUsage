# work=$HOME/build/devizer; mkdir -p $work; cd $work; git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage; git pull; cd Universe.CpuUsage.Banchmark; bash run-llvm.sh  

pushd ..
nuget restore || true; dotnet restore || true
popd
msbuild *.csproj /t:rebuild /p:Configuration=Release /v:q
cd bin/Release/net47
mono --llvm Universe.CpuUsage.Banchmark.exe
cd ..
cd ..



