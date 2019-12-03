#!/usr/bin/env bash
# work=$HOME/build/devizer; mkdir -p $work; cd $work; rm -rf Universe.CpuUsage; git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage/Tests4Mono; source build-the-matrix.sh; echo $matrix_run; bash -c "$matrix_run" 

# set -e

# pushd "$(dirname "$0")" >/dev/null; SCRIPT="$(pwd)"; popd >/dev/null
SCRIPT="$(pwd)"
SayScript="$(pwd)/say.include.sh"
pushd ..; StartFrom=$(pwd); popd
source "$SayScript"

echo '<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Universe.CpuUsage" value="https://ci.appveyor.com/nuget/Universe.CpuUsage" />
  </packageSources>
</configuration>
' > nuget.config


matrix=~/build/devizer/MATRIX-Universe.CpuUsage
mkdir -p bin $matrix
bin_path=$(pwd)/bin
restore_path=$(pwd)/obj/RESTORE
mkdir -p $restore_path; rm -rf $restore_path/* || true
Say "Matrix Path: $matrix"
Say "Library path: $bin_path"

cd $restore_path
Say "Nuget Restore Universe.CpuUsage to [$(pwd)]"
nuget install Universe.CpuUsage -verbosity quiet || nuget install Universe.CpuUsage -verbosity quiet || true
pushd Universe.CpuUsage*/lib
copyto=$(pwd)
popd
RAOT_VER=3.0.2
Say "Nuget Restore Theraot.Core ${RAOT_VER} to $(pwd)"
nuget install Theraot.Core -version $RAOT_VER -verbosity quiet || nuget install Theraot.Core -version $RAOT_VER -verbosity quiet || true
cd Theraot.Core*/lib
for subdir in $(ls -1); do
  # Say "TRY ${copyto}/${subdir}"
  if [[ -d "${copyto}/${subdir}" ]]; then
    echo -e "COPY [Theraot.Core $RAOT_VER] @ ${subdir} to\n     [${copyto}/${subdir}]"
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
# mkdir -p  ../../../../Tests4Linux/bin
cp -r ./. $bin_path/

cd $SCRIPT
Say "RESTORE for [$(pwd)] Universe.CpuUsage.MonoTests.sln"
nuget restore Universe.CpuUsage.MonoTests.sln -verbosity quiet || nuget restore Universe.CpuUsage.MonoTests.sln -verbosity quiet
# pushd ~/build/devizer/Universe.CpuUsage; find . ; popd
msbuild /t:Rebuild /p:Configuration=Release /v:q

      errors=0;
      proj=Universe.CpuUsage.MonoTests/Universe.CpuUsage.MonoTests.csproj
      cp -f ${proj} ${proj}-bak
      cat "$SayScript" > $matrix/run.sh
      echo 'success=0; errors=0; Say "RUNNING MATRIX. current is [$(pwd)]. Machine is [$(uname -m)-$(uname -s)]"' >> $matrix/run.sh
      matrix_run="cd $matrix && bash run.sh"
      for target_dir in $(ls -d bin/*/); do
        target=$(basename $target_dir)
        echo "pushd job-${target} >/dev/null" >> $matrix/run.sh
        echo 'echo ""; Say "JOB ['${target}'] for [$(uname -m)-$(uname -s)] in [$(pwd)]"' >> $matrix/run.sh


        Say "Mono Tests: msbuild rebuild for [$target]"
        sed_cmd="s/\.\.\\bin\\net46\\Universe/\.\.\\bin\\${target}\\Universe/g"
        sed_cmd="s/net46/${target}/g"
        
        cp -f ${proj}-bak ${proj}
        sed "$sed_cmd" ${proj}-bak > ${proj}
        echo "REF: {$(cat $proj | grep $target | grep HintPath)}"
        
        cfg=Debug
        msbuild /noLogo /t:Rebuild /p:Configuration=$cfg /v:q
        echo "mono ./Universe.CpuUsage.MonoTests/bin/$cfg/Universe.CpuUsage.MonoTests.exe" >> $matrix/run.sh
        
        # Say "Mono Tests: Run Tests for [$target]"
        echo '
    pushd packages/NUnit.ConsoleRunner*/tools >/dev/null; runner=$(pwd)/nunit3-console.exe; popd >/dev/null
    echo "Runner for the '$target' target is [$runner]"
    pushd Universe.CpuUsage.MonoTests/bin/'$cfg' >/dev/null
       mono $runner --workers=1 Universe.CpuUsage.MonoTests.exe && { Say "Success ['$target'] for [$(uname -m)-$(uname -s)]"; success=$((success+1)); } || { Say "ERROR: TESTING ['$target'] for [$(uname -m)-$(uname -s)]"; errors=$((errors+1)); }
    popd >/dev/null
' >> $matrix/run.sh

        mkdir -p $matrix/job-${target}
        cp -r ./. $matrix/job-${target}

        printf 'echo ""; popd >/dev/null\n\n' >> $matrix/run.sh
      done

echo 'Say "Target Summary. Success: $success, Errors: $errors"; exit $errors' >> $matrix/run.sh
chmod +x $matrix/run.sh


has_dot_net="no"; command -v dotnet >/dev/null 2>&1 && { has_dot_net=yes; } || true
if [[ $has_dot_net == yes && "$(uname -m)" == "aarch64" ]]; then
  pushd $StartFrom; 
    cd ..
    Say "dotnet RESTORE for [$(pwd)]"
    time dotnet restore --disable-parallel || true
    pushd Universe.CpuUsage.Tests
      Say "dotnet TEST -f netcoreapp2.2 for [$(pwd)]"
      (dotnet test -f netcoreapp2.2 -c Release || exit 1) | cat
    popd
  popd
fi

# set +e