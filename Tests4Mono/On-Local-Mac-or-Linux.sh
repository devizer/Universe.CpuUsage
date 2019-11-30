work=$HOME/build/devizer; mkdir -p $work; cd $work; rm -rf $work/Universe.CpuUsage; git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage
cd Tests4Mono; set +e; source build-the-matrix.sh; echo $matrix_run
bash -e -c "$matrix_run"

