#!/usr/bin/env bash

echo Configure apt
echo 'Acquire::Check-Valid-Until "0";' |  tee /etc/apt/apt.conf.d/10no--check-valid-until 
echo 'APT::Get::Assume-Yes "true";' |  tee /etc/apt/apt.conf.d/11assume-yes               
echo 'APT::Get::AllowUnauthenticated "true";' |  tee /etc/apt/apt.conf.d/12allow-unauth   

time (apt -qq update >/dev/null ;  apt install -y -qq git sudo jq tar bzip2 gzip curl lsb-release procps gnupg)

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
  sudo apt-get update; time sudo apt-get install mono-complete nuget msbuild -y -q
  # --allow-unauthenticated ?
fi
set -e
mono --version
