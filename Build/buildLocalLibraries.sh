#!/bin/bash
# script for building libpalaso, liblcm and chorus libraries locally for debugging FLEx
# You must also indicate that you are using local libraries or edit the LibraryDevelopment.properties file
#    with the path to the library outputs (i.e. C:/libpalaso/output/Debug)

########## Parameters ############
# Edit these parameters according to the configurations on your machine
buildcommand="C:/Program Files (x86)/MSBuild/14.0/Bin/MSBuild.exe"
BUILD_CONFIG=Debug
########### End Parameters #############

set -e -o pipefail
PROGRAM="$(basename "$0")"

copy_curl() {
	echo "curl $2 <= $1"
	curl -# -L -o $2 $1
}

printUsage() {
	echo "buildLocalLibraries x86|x64 [PALASOROOT] [LIBLCMROOT] [CHORUSROOT] [BUILDCOMMAND]"
}

osName=`uname -s`

if [ "$5" != "" ]
then
	buildcommand="$5"
fi

if [ "$1" == "x86" ]
then
	libpalasoPlatform="Mixed Platforms"
	liblcmPlatform="x86"
	ICUBuildType="Win32"
elif [ "$1" == "x64" ]
then
	libpalasoPlatform="x64"
	liblcmPlatform="x64"
	ICUBuildType="Win64"
else
	printUsage
	exit
fi

# Get the path to the libpalaso, chorus and LCModel cloned repositories on your machine
# Repositories are available at github.com/sillsdev
if [ "$2" == "" ]
then
	read -p "Enter the full path to your local libpalaso repo: " libpalasoRepo
else
	libpalasoRepo="$2"
fi
if [ "$3" == "" ]
then
	read -p "Enter the full path to your local liblcm repo: " liblcmRepo
else
	liblcmRepo="$3"
fi
if [ "$4" == "" ]
then
	read -p "Enter the full path to your local chorus repo: " chorusRepo
else
	chorusRepo="$4"
fi

############### build libpalaso #############
cd ${libpalasoRepo}/build
if [[ ${osName} == "Linux" ]]
then
	./buildupdate.mono.sh
	MONO=Mono
	(. ../environ && "${buildcommand}" /target:build /verbosity:quiet /property:Configuration=$BUILD_CONFIG$MONO /property:Platform="${libpalasoPlatform}" Palaso.proj)
else
	./buildupdate.win.sh
	MONO=
	"${buildcommand}" /target:build /verbosity:quiet /property:Configuration=$BUILD_CONFIG /property:Platform="${libpalasoPlatform}" Palaso.proj
fi

copy_curl http://build.palaso.org/guestAuth/repository/download/Libraries_Icu4c${ICUBuildType}FieldWorksContinuous/latest.lastSuccessful/icudt54.dll ../output/${BUILD_CONFIG}$MONO/${Platform}/icudt54.dll
copy_curl http://build.palaso.org/guestAuth/repository/download/Libraries_Icu4c${ICUBuildType}FieldWorksContinuous/latest.lastSuccessful/icuin54.dll ../output/${BUILD_CONFIG}$MONO/${Platform}/icuin54.dll
copy_curl http://build.palaso.org/guestAuth/repository/download/Libraries_Icu4c${ICUBuildType}FieldWorksContinuous/latest.lastSuccessful/icuuc54.dll ../output/${BUILD_CONFIG}$MONO/${Platform}/icuuc54.dll
copy_curl http://build.palaso.org/guestAuth/repository/download/Libraries_Icu4c${ICUBuildType}FieldWorksContinuous/latest.lastSuccessful/icutu54.dll ../output/${BUILD_CONFIG}$MONO/${Platform}/icutu54.dll
copy_curl http://build.palaso.org/guestAuth/repository/download/Libraries_Icu4c${ICUBuildType}FieldWorksContinuous/latest.lastSuccessful/gennorm2.exe ../output/${BUILD_CONFIG}$MONO/${Platform}/gennorm2.exe


############### build liblcm ##############
cd $liblcmRepo
mkdir -p ${liblcmRepo}/lib/downloads
cp -r ${libpalasoRepo}/output/${BUILD_CONFIG}/* lib/downloads
if [[ ${osName} == "Linux" ]]
then
	(. environ && "${buildcommand}" /target:Build /property:Configuration=$BUILD_CONFIG /property:Platform="${liblcmPlatform}" /property:UseLocalFiles=True LCM.sln)
else
	"${buildcommand}" /target:Build /property:Configuration=$BUILD_CONFIG /property:Platform="${liblcmPlatform}" /property:UseLocalFiles=True LCM.sln
fi



############### build chorus ##############
cd ${chorusRepo}/build
cp -a ${libpalasoRepo}/output/${BUILD_CONFIG}$MONO/* ../lib/${BUILD_CONFIG}$MONO
cp -a ${libpalasoRepo}/output/${BUILD_CONFIG}$MONO/${Platform}/* ../lib/${BUILD_CONFIG}$MONO
if [[ ${osName} == "Linux" ]]
then
	./TestBuild.sh $BUILD_CONFIG
else
	./buildupdate.win.sh
	"${buildcommand}" /target:Compile /verbosity:quiet /property:Configuration=$BUILD_CONFIG Chorus.proj
fi



echo $(date +"%F %T") $PROGRAM: "Finished"

#End Script
