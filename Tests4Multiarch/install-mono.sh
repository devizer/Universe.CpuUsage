#!/usr/bin/env bash

cmd1="apt-get -qq update >/dev/null"
cmd2="apt-get install -y -qq git sudo jq tar bzip2 gzip curl lsb-release procps gnupg apt-transport-https dirmngr ca-certificates >/dev/null"
for cmd in "$cmd1" "$cmd2"; do
    echo "eval [$cmd]"
    sudo true >/dev/null 2>&1 && time eval "sudo $cmd" || time eval "$cmd"
done

echo Configure apt
echo 'Acquire::Check-Valid-Until "0";' | sudo tee /etc/apt/apt.conf.d/10no--check-valid-until
echo 'APT::Get::Assume-Yes "true";' | sudo tee /etc/apt/apt.conf.d/11assume-yes
echo 'APT::Get::AllowUnauthenticated "true";' | sudo tee /etc/apt/apt.conf.d/12allow-unauth

echo net.ipv6.conf.all.disable_ipv6=1 | sudo tee /etc/sysctl.d/disableipv6.conf || true

if [[ "$(command -v mono)" == "" ]]; then 
  # export MONO_ENV_OPTIONS=-O=-aot
  export MONO_USE_LLVM=0
  
  sudo apt-key adv --keyserver keyserver.ubuntu.com --recv-keys A6A19B38D3D831EF
  # sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
  source /etc/os-release
  def="deb https://download.mono-project.com/repo/$ID stable-$(lsb_release -s -c) main"
  if [[ "$ID" == "raspbian" ]]; then def="deb https://download.mono-project.com/repo/debian stable-raspbian$(lsb_release -cs) main"; fi
  echo "$def" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list >/dev/null
  echo "Official mono repo: /etc/apt/sources.list.d/mono-official-stable.list"
  echo $def
  sudo apt -qq update; time sudo apt -qq install mono-complete nuget msbuild -y
  # --allow-unauthenticated ?
fi
set -e
mono --version

  if [[ "$(uname -m)" == "aarch64" ]]; then
      url=https://raw.githubusercontent.com/devizer/glist/master/install-dotnet-dependencies.sh; (wget -q -nv --no-check-certificate -O - $url 2>/dev/null || curl -ksSL $url) | bash
      DOTNET_Url=https://dot.net/v1/dotnet-install.sh; 
      mkdir -p ~/.dotnet/tools; export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
      time (curl -ksSL $DOTNET_Url | bash /dev/stdin -c 2.2 -i ~/.dotnet)
      time (curl -ksSL $DOTNET_Url | bash /dev/stdin -c 3.0 -i ~/.dotnet)
      export DOTNET_ROOT="$HOME/.dotnet"
      time dotnet tool install -g BenchmarkDotNet.Tool || true
      time dotnet --info || true
  fi
