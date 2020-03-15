#!/usr/bin/env bash
set -e
apt-get update -q; apt-get install -y wget p7zip-full sudo procps bsdutils util-linux lshw;
script=https://raw.githubusercontent.com/devizer/test-and-build/master/install-build-tools.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash;
Say "ENVIRONMENT"; printenv | sort
Say "MEMORY INFO"; free -m; 
Say "BLOCK STORAGE DEVICES"; sudo fdisk -l || true; 
Say "MOUNTS"; df -h -T
Say "LS CPU"; lscpu || true
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
  Say "net core for arm?"
      DOTNET_Url=https://dot.net/v1/dotnet-install.sh; 
      try-and-retry curl -o /tmp/_dotnet-install.sh -ksSL $DOTNET_Url
      for v in 2.1 2.2 3.0 3.1; do
        time try-and-retry timeout 666 sudo -E bash /tmp/_dotnet-install.sh -c 2.1 -i /usr/share/dotnet --architecture arm
      done
fi
sudo ln -f -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet || true

Say "TESTING Universe.CpuUsage"
dotnet test -f netcoreapp2.2
