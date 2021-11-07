#!/usr/bin/env bash
set -e; #v11

if [[ "$(command -v Reset-Target-Framework)" == "" ]]; then
  script=https://raw.githubusercontent.com/devizer/test-and-build/master/install-build-tools-bundle.sh; 
  (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash
fi
Say --Reset-Stopwatch

if [[ "$(command -v nunit3-console)" == "" ]]; then
  export XFW_VER=net47 NET_TEST_RUNNERS_INSTALL_DIR=/opt/net-test-runners; 
  script=https://raw.githubusercontent.com/devizer/test-and-build/master/lab/NET-TEST-RUNNERS-build.sh;
  cmd="(wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | sudo -E bash"
  eval "$cmd" || eval "$cmd" || eval "$cmd"
fi

NETFW=net461
work=/transient-builds/src
mkdir -p $work && cd $work
Say "git clone|pull"
test ! -d Universe.CpuUsage && git clone https://github.com/devizer/Universe.CpuUsage || true
cd Universe.CpuUsage
git reset --hard; git pull
Say "Reset target framework to [$NETFW]"
Reset-Target-Framework --framework $NETFW --language latest
Say "Restore Dependencies"
cd Universe.CpuUsage.Tests
time msbuild /t:Restore -v:m
Say "Build Release for Universe.CpuUsage.Tests"
time msbuild /t:Build /p:Configuration=Release -v:m
cd bin/Release/$NETFW
Say "Run Integration Tests"
time nunit3-console --workers=1 Universe.CpuUsage.Tests.dll
Say "Successfully completed"
