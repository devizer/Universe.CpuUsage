work=$HOME/build/devizer; rm -rf $work; mkdir -p $work; cd $work; git clone https://github.com/devizer/Universe.CpuUsage; 
cd Universe.CpuUsage
dotnet build -c Release
dotnet test -f netcoreapp3.0


