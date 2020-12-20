docker run -it --rm ubuntu bash -c 'export DEBIAN_FRONTEND=noninteractive; apt-get update; apt-get install git curl sudo mc -y;
  git clone https://github.com/devizer/Universe.CpuUsage; cd Universe.CpuUsage;
  cd Universe.CpuUsage/Universe.CpuUsage.Tests;
  script=https://raw.githubusercontent.com/devizer/test-and-build/master/lab/install-DOTNET.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash;
  dotnet test -f netcoreapp3.1;
  bash'

