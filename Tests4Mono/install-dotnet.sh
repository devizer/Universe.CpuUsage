#!/usr/bin/env bash
# script=https://raw.githubusercontent.com/devizer/Universe.CpuUsage/master/Tests4Mono/install-dotnet.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash 
set -e
OS_X_VER=$(sw_vers 2>/dev/null | grep BuildVer | awk '{print $2}' | cut -c1-2 || true); OS_X_VER=$((OS_X_VER-4))


# OSX?
if [[ "$(uname -s)" == "Darwin" ]]; then
    MONO_Url=https://download.mono-project.com/archive/6.4.0/macos-10-universal/MonoFramework-MDK-6.4.0.198.macos10.xamarin.universal.pkg
    curl -o ~/mono.pkg $MONO_Url
    sudo installer -verbose -pkg ~/mono.pkg -target /
    export PATH="/Library/Frameworks/Mono.framework/Versions/Current/Commands:$PATH"
else

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

    # echo net.ipv6.conf.all.disable_ipv6=1 | sudo tee /etc/sysctl.d/disableipv6.conf || true

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

fi

echo "uname -m: $(uname -m)"

if [[ "$(uname -m)" == "aarch64" || "$(uname -m)" == "x86_64" ]] || [ $OS_X_VER -ge 13 ]; then
    DOTNET_Url=https://dot.net/v1/dotnet-install.sh
    cmd_curl_dotnet_install="curl -ksSL -o /tmp/dotnet-install.sh $DOTNET_Url"
    cmd_dotnet_22="bash /tmp/dotnet-install.sh -c 2.2 -i ~/.dotnet"
    cmd_dotnet_30="bash /tmp/dotnet-install.sh -c 3.0 -i ~/.dotnet"
    for cmd in "$cmd_curl_dotnet_install" "$cmd_dotnet_22" "$cmd_dotnet_30"; do
        echo "|# $cmd"
        time (eval "$cmd" || eval "$cmd" || eval "$cmd")
    done    
     
    echo '#!/usr/bin/env bash
    mkdir -p $HOME/.dotnet/tools $HOME/.dotnet
    export PATH="$HOME/.dotnet:$PATH:$HOME/.dotnet/tools"
    export DOTNET_ROOT="$HOME/.dotnet"
    ' >> ~/.dotnet-env
    chmod +x ~/.dotnet-env
    . ~/.dotnet-env
    echo '
    . ~/.dotnet-env
    ' >> ~/.bashrc
    dotnet --info || true
    dotnet tool install -g BenchmarkDotNet.Tool || true
fi

set +e
