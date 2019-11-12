work=$HOME/build/devizer; rm -rf $work; mkdir -p $work; cd $work; git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage
cd Tests4Mac
bash -e test-using-mono-only.sh 


