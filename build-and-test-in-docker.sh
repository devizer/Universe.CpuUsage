set -e
apt-get update -q; 
apt-get install -yq locales systemd apt-utils apt-transport-https ca-certificates curl libcurl3 gnupg2 software-properties-common htop mc lsof unzip net-tools bsdutils sudo p7zip-full wget git time ncdu tree procps p7zip-full jq pv; 
apt-get clean;

script=https://raw.githubusercontent.com/devizer/test-and-build/master/install-build-tools.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash;
Say "SYSTEM INFO"; free -m; sudo fdisk -l; df -h -T
Say "7Z BENCHMARK"; 7z b
script=https://raw.githubusercontent.com/devizer/test-and-build/master/lab/install-DOTNET.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash;
dotnet test -f netcoreapp2.2
