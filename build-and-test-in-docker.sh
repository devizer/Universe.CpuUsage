#!/usr/bin/env bash
set -e
apt-get update -q; apt-get install -y wget p7zip-full sudo procps gnu-fdisk;
script=https://raw.githubusercontent.com/devizer/test-and-build/master/install-build-tools.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash;
Say "ENVIRONMENT"; printenv | sort
Say "MEMORY INFO"; free -m; 
Say "BLOCK STORAGE DEVICES"; sudo fdisk -l; 
Say "MOUNTS"; df -h -T
Say "7Z BENCHMARK"; 
if [[ $(uname -m) == armv7* ]]; then
    7z -mmt1 b || true
else
    7z b || true
fi

apt-get install -yq locales systemd apt-utils apt-transport-https ca-certificates curl libcurl3 gnupg2 software-properties-common htop mc lsof unzip net-tools bsdutils sudo p7zip-full wget git time ncdu tree procps p7zip-full jq pv; 
apt-get clean;
if [[ $(uname -m) != armv8l ]]; then
  script=https://raw.githubusercontent.com/devizer/test-and-build/master/lab/install-DOTNET.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash;
else
  Say "net core is not yet"
fi
sudo ln -f -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet || true

Say "TESTING Universe.CpuUsage"
dotnet test -f netcoreapp2.2
