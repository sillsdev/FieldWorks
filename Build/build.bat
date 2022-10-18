echo off

REM cause Environment variable changes to be lost after this process dies:
if not "%OS%"=="" setlocal

REM Add Bin and DistFiles to the PATH:
pushd %~dp0
cd ..
set PATH=%cd%\DistFiles;%cd%\Bin;%WIX%\bin;%PATH%
popd

<<<<<<< HEAD
for /f "usebackq tokens=1* delims=: " %%i in (`vswhere -version "[15.0,16.999)" -latest -requires Microsoft.Component.MSBuild`) do (
||||||| f013144d5
for /f "usebackq tokens=1* delims=: " %%i in (`vswhere -version "[15.0,15.999)" -requires Microsoft.Component.MSBuild`) do (
=======
for /f "usebackq tokens=1* delims=: " %%i in (`vswhere -version "15.0" -requires Microsoft.Component.MSBuild`) do (
>>>>>>> develop
  if /i "%%i"=="installationPath" set InstallDir=%%j
  if /i "%%i"=="catalog_productLineVersion" set VSVersion=%%j
)

if "%arch%" == "" set arch=x86
<<<<<<< HEAD

call "%InstallDir%\VC\Auxiliary\Build\vcvarsall.bat" %arch% 8.1
||||||| f013144d5
call "%InstallDir%\VC\Auxiliary\Build\vcvarsall.bat" %arch% 8.1
=======

REM run Microsoft's batch file to set all the environment variables and path necessary to build a C++ app
set VcVarsLoc=%InstallDir%\VC\Auxiliary\Build\vcvarsall.bat

if exist "%VcVarsLoc%" (
  call "%VcVarsLoc%" %arch% 8.1
) else (
  echo "Could not find: %VcVarsLoc% something is wrong with the Visual Studio installation"
  GOTO End
)
>>>>>>> develop


if "%arch%" == "x86" IF "%VSVersion%" GEQ "2019" (set MsBuild="%InstallDir%\MSBuild\Current\Bin\msbuild.exe") else (set MsBuild="%InstallDir%\MSBuild\15.0\Bin\msbuild.exe")
if "%arch%" == "x64" if "%VSVersion%" GEQ "2019" (set MsBuild="%InstallDir%\MSBuild\Current\Bin\amd64\msbuild.exe") else (set MsBuild="%InstallDir%\MSBuild\15.0\Bin\amd64\msbuild.exe")

set KEY_NAME="HKLM\SOFTWARE\WOW6432Node\Microsoft\Microsoft SDKs\Windows\v10.0"
set VALUE_NAME=InstallationFolder

REG QUERY %KEY_NAME% /S /v %VALUE_NAME%
FOR /F "tokens=2* delims= " %%1 IN (
  'REG QUERY %KEY_NAME% /v %VALUE_NAME%') DO SET pInstallDir=%%2
SET PATH=%PATH%;%pInstallDir%bin\%arch%;

set VALUE_NAME=ProductVersion
REG QUERY %KEY_NAME% /S /v %VALUE_NAME%
FOR /F "tokens=2* delims= " %%1 IN (
  'REG QUERY %KEY_NAME% /v %VALUE_NAME%') DO SET Win10SdkUcrtPath=%pInstallDir%Include\%%2.0\ucrt

REM allow typelib registration in redirected registry key even with limited permissions
set OAPERUSERTLIBREG=1

echo Building using `%MsBuild%`
set all_args=%*
REM Run the next target only if the previous target succeeded
(
<<<<<<< HEAD
	%MsBuild% /t:refreshTargets
||||||| f013144d5
	%MsBuild% /t:RestoreNuGetPackages
) && (
	%MsBuild% /t:CheckDevelopmentPropertiesFile
) && (
	%MsBuild% /t:refreshTargets
=======
	if "%all_args:disableDownloads=%"=="%all_args%" %MsBuild% FieldWorks.proj /t:RestoreNuGetPackages
) && (
	%MsBuild% FieldWorks.proj /t:CheckDevelopmentPropertiesFile
) && (
	%MsBuild% FieldWorks.proj /t:refreshTargets
>>>>>>> develop
) && (
	%MsBuild% FieldWorks.proj %*
)
:END
FOR /F "tokens=*" %%g IN ('date /t') do (SET DATE=%%g)
FOR /F "tokens=*" %%g IN ('time /t') do (SET TIME=%%g)
echo Build completed at %TIME% on %DATE%