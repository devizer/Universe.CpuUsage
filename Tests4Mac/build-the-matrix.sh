# work=$HOME/build/devizer; rm -rf $work; mkdir -p $work; cd $work; git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage/Tests4Mac; source build-the-matrix.sh; echo $matrix_run

pushd "$(dirname "$0")" >/dev/null; SCRIPT="$(pwd)"; popd >/dev/null
SayScript="$(pwd)/say.include.sh"
source "$SayScript"

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

# Say "LIST OF THE [$(pwd)]"
# find .


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
msbuild /t:Rebuild /p:Configuration=Release /v:q

      errors=0;
      proj=Universe.CpuUsage.MonoTests/Universe.CpuUsage.MonoTests.csproj
      cp -f ${proj} ${proj}-bak
      cat "$SayScript" > $matrix/run.sh
      echo 'errors=0; Say "RUNNING MATRIX. current is [$(pwd)]. Machine is [$(hostname)]"' >> $matrix/run.sh
      matrix_run="cd $matrix && bash run.sh"
      for target_dir in $(ls -d bin/*/); do
        target=$(basename $target_dir)
        echo "pushd job-${target} >/dev/null" >> $matrix/run.sh
        echo 'echo ""; Say "JOB ['${target}'] in [$(pwd)]"' >> $matrix/run.sh


        Say "Mono Tests: msbuild rebuild for [$target]"
        sed_cmd="s/\.\.\\bin\\net20\\Universe/\.\.\\bin\\${target}\\Universe/g"
        sed_cmd="s/net20/${target}/g"
        
        cp -f ${proj}-bak ${proj}
        sed "$sed_cmd" ${proj}-bak > ${proj}
        echo "REF: {$(cat $proj | grep $target)}"
        
        cfg=Debug
        msbuild /noLogo /t:Rebuild /p:Configuration=$cfg /v:q
        echo "mono ./Universe.CpuUsage.MonoTests/bin/$cfg/Universe.CpuUsage.MonoTests.exe" >> $matrix/run.sh
        
        # Say "Mono Tests: Run Tests for [$target]"
        echo '
    pushd packages/NUnit.ConsoleRunner*/tools >/dev/null; runner=$(pwd)/nunit3-console.exe; popd >/dev/null
    echo "Runner for the '$target' target is [$runner]"
    pushd Universe.CpuUsage.MonoTests/bin/'$cfg' >/dev/null
       mono $runner --workers=1 Universe.CpuUsage.MonoTests.exe  || { echo "ERROR: TESTING ['$target']"; errors=$((errors+1)); }
    popd >/dev/null
' >> $matrix/run.sh

        mkdir -p $matrix/job-${target}
        cp -r ./. $matrix/job-${target}

        printf 'echo ""; popd >/dev/null\n\n' >> $matrix/run.sh
      done

echo 'Say "Total Errors: $errors"; exit $errors' >> $matrix/run.sh
chmod +x $matrix/run.sh

