# work=$HOME/build/devizer; rm -rf $work; mkdir -p $work; cd $work; git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage/Tests4Mac

    function header() {
      if [[ $(uname -s) != Darwin ]]; then
        startAt=${startAt:-$(date +%s)}; elapsed=$(date +%s); elapsed=$((elapsed-startAt)); elapsed=$(TZ=UTC date -d "@${elapsed}" "+%_H:%M:%S");
      fi
      LightGreen='\033[1;32m'; Yellow='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'; LightGray='\033[1;2m';
      printf "${LightGray}${elapsed:-}${NC} ${LightGreen}$1${NC} ${Yellow}$2${NC}\n"; 
    }
    counter=0; function Say() { echo ""; counter=$((counter+1)); header "STEP $counter" "$1"; }; Say "" >/dev/null

if [[ "$(uname -m)" == "aarch64" ]]; then
  pushd ..
    dotnet test -f netcoreapp3.0 -c Release || exit 1
  popd
fi

echo '<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Universe.CpuUsage" value="https://ci.appveyor.com/nuget/Universe.CpuUsage" />
  </packageSources>
</configuration>
' > nuget.config


mkdir -p bin ../Tests4Linux/bin
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

Say "LIST OF THE [$(pwd)]"
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
msbuild /t:Rebuild /p:Configuration=Release

      errors=0;
      proj=Universe.CpuUsage.MonoTests/Universe.CpuUsage.MonoTests.csproj
      cp -f ${proj} ${proj}-bak
      for target_dir in $(ls -d bin/*/); do
        target=$(basename $target_dir)

        Say "Mono Tests: msbuild rebuild for [$target]"
        sed_cmd="s/\.\.\\bin\\net20\\Universe/\.\.\\bin\\${target}\\Universe/g"
        sed_cmd="s/net20/${target}/g"
        
        cp -f ${proj}-bak ${proj}
        sed "$sed_cmd" ${proj}-bak > ${proj}
        echo "REF: {$(cat $proj | grep $target)}"
        
        cfg=Debug
        msbuild /t:Rebuild /p:Configuration=$cfg /v:q
        mono ./Universe.CpuUsage.MonoTests/bin/$cfg/Universe.CpuUsage.MonoTests.exe
        
        pushd packages/NUnit.ConsoleRunner*/tools
        runner=$(pwd)/nunit3-console.exe
        popd

        Say "Mono Tests: Run Tests for [$target]"
        pushd Universe.CpuUsage.MonoTests/bin/$cfg
        mono $runner --workers=1 Universe.CpuUsage.MonoTests.exe  || (Say "TESTING [$target] ERROR"; errors=$((errors+1)))
        popd
      
      done

if [[ $errors == "0" ]]; then
    Say "Mono Tests: Done"
else
    Say "Mono Tests: FAIL. Total $errors error(s)"
    exit 666
fi
