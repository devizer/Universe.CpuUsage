# work=$HOME/build/devizer; rm -rf $work; mkdir -p $work; cd $work; git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage/Tests4Mac; source build-the-matrix.sh; echo ""; echo matrix_run

    function header() {
      if [[ $(uname -s) != Darwin ]]; then
        startAt=${startAt:-$(date +%s)}; elapsed=$(date +%s); elapsed=$((elapsed-startAt)); elapsed=$(TZ=UTC date -d "@${elapsed}" "+%_H:%M:%S");
      fi
      LightGreen='\033[1;32m'; Yellow='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'; LightGray='\033[1;2m';
      printf "${LightGray}${elapsed:-}${NC} ${LightGreen}$1${NC} ${Yellow}$2${NC}\n"; 
    }
    counter=0; function Say() { echo ""; counter=$((counter+1)); header "STEP $counter" "$1"; }; Say "" >/dev/null


echo '<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Universe.CpuUsage" value="https://ci.appveyor.com/nuget/Universe.CpuUsage" />
  </packageSources>
</configuration>
' > nuget.config


mkdir -p bin matrix ../Tests4Linux/bin
matrix=$(pwd)/matrix
pushd bin
Say "Loading Universe.CpuUsage to [$(pwd)]"
nuget install Universe.CpuUsage || nuget install Universe.CpuUsage || true
pushd Universe.CpuUsage*/lib
copyto=$(pwd)
popd
RAOT_VER=3.0.2
Say "Loading Theraot.Core ${RAOT_VER} to $(pwd)"
nuget install Theraot.Core -version $RAOT_VER || true
cd Theraot.Core*/lib
for subdir in $(ls -1); do
  Say "TRY ${copyto}/${subdir}"
  if [[ -d "${copyto}/${subdir}" ]]; then
    Say "COPY [Theraot.Core $RAOT_VER] @ ${subdir} to ${copyto}/${subdir}"
    cp -r ${subdir}/. "${copyto}/${subdir}"
  fi
done
# popd

# cd Universe.CpuUsage*/lib 
cd $copyto
Say "CpuUsage libraries: {$(pwd)}"
rm -rf net47 net472 net48 netcoreapp3.0 netstandard2.1
mkdir -p  ../../../../Tests4Linux/bin
cp -r ./. ../../../../Tests4Linux/bin/
popd

cd ../Tests4Linux
Say "RESTORE for [$(pwd)]"
nuget restore
# pushd ~/build/devizer/Universe.CpuUsage; find . ; popd
msbuild /t:Rebuild /p:Configuration=Release

      errors=0;
      proj=Universe.CpuUsage.MonoTests/Universe.CpuUsage.MonoTests.csproj
      cp -f ${proj} ${proj}-bak
      echo "errors=0" >> $matrix/run.sh
      matrix_run="cd $matrix; bash run.sh"
      for target_dir in $(ls -d bin/*/); do
        target=$(basename $target_dir)
        echo "pushd job-${target}" >> $matrix/run.sh

        Say "Mono Tests: msbuild rebuild for [$target]"
        sed_cmd="s/\.\.\\bin\\net20\\Universe/\.\.\\bin\\${target}\\Universe/g"
        sed_cmd="s/net20/${target}/g"
        
        cp -f ${proj}-bak ${proj}
        sed "$sed_cmd" ${proj}-bak > ${proj}
        echo "REF: {$(cat $proj | grep $target)}"
        
        cfg=Debug
        msbuild /noLogo /t:Rebuild /p:Configuration=$cfg /v:q
        echo "mono ./Universe.CpuUsage.MonoTests/bin/$cfg/Universe.CpuUsage.MonoTests.exe" >> $matrix/run.sh
        
        pushd packages/NUnit.ConsoleRunner*/tools
        runner=$(pwd)/nunit3-console.exe
        popd

        Say "Mono Tests: Run Tests for [$target]"
        echo "
    pushd Universe.CpuUsage.MonoTests/bin/$cfg
    mono $runner --workers=1 Universe.CpuUsage.MonoTests.exe  || (echo "ERROR: TESTING [$target]"; errors=\$((errors+1)))
    popd
" >> $matrix/run.sh

        mkdir -p $matrix/job-${target}
        cp -r ./. $matrix/job-${target}

        printf "popd\n\n" >> $matrix/run.sh
      done

echo 'exit $errors' >> $matrix/run.sh
exit;

if [[ $errors == "0" ]]; then
    Say "Mono Tests: Done"
else
    Say "Mono Tests: FAIL. Total $errors error(s)"
    exit 666
fi
