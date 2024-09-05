#!/bin/bash

if [ "$(id -u)" -ne 0 ]; then echo "Please run as root." >&2; exit 1; fi
download() {
    [[ $downloadspage =~ $1 ]]
    linkpage=$(wget -qO - https://dotnet.microsoft.com${BASH_REMATCH[1]})

    matchdl='id="directLink" href="([^"]*)"'
    [[ $linkpage =~ $matchdl ]]
    wget -O $2 "${BASH_REMATCH[1]}"
}

detectArch() {
  arch=arm32

  if command -v uname > /dev/null; then
      # machineCpu=$(uname -m)-$(uname -p)
    machineCpu=$(getconf LONG_BIT)

    if [[ $machineCpu == *64* ]]; then
      arch=arm64
    fi
  fi
}

RUNTIMES=$(echo $(su - $USER && otnet --list-runtimes))

if grep -q "dotnet: command not found" <<< "RUNTIMES"  
then
    
  username=$(logname)
  dotnetver=8.0

  sdkfile=/tmp/dotnetsdk.tar.gz
  aspnetfile=/tmp/aspnetcore.tar.gz

  if [[ $EUID -ne 0 ]]; then
    echo -e "\e[1;31mThis script must be run as root (sudo $0)" 
    exit 1
  fi

  apt-get -y install libunwind8 gettext

  rm -f $sdkfile
  rm -f $aspnetfile

  [[ "$dotnetver" > "5" ]] && dotnettype="dotnet" || dotnettype="dotnet-core"
  downloadspage=$(wget -qO - https://dotnet.microsoft.com/download/$dotnettype/$dotnetver)

  detectArch

  download 'href="([^"]*sdk-[^"/]*linux-'$arch'-binaries)"' $sdkfile

  download 'href="([^"]*aspnetcore-[^"/]*linux-'$arch'-binaries)"' $aspnetfile

  if [[ -d /opt/dotnet ]]; then
    echo "/opt/dotnet already  exists on your filesystem."
  else
    echo "Creating Main Directory"
    echo ""
    mkdir /opt/dotnet
  fi

  tar -xvf $sdkfile -C /opt/dotnet/

  tar -xvf $aspnetfile -C /opt/dotnet/

  ln -s /opt/dotnet/dotnet /usr/local/bin

  if grep -q 'export DOTNET_ROOT=' /home/$username/.bashrc;  then
    echo 'Already added link to .bashrc'
  else
    echo 'Adding Link to .bashrc'
    echo 'export DOTNET_ROOT=/opt/dotnet' >> /home/$username/.bashrc
  fi

    
fi



