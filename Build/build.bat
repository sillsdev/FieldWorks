echo off

echo.
echo.
echo NOTE: If you are building from a clean repository, you will need to answer a few questions after restoring NuGet packages before the build can continue.
echo.
echo.

REM cause Environment variable changes to be lost after this process dies:
if not "%OS%"=="" setlocal

REM Add Bin and DistFiles to the PATH:
pushd %~dp0
cd ..
set PATH=%cd%\DistFiles;%cd%\Bin;%WIX%\bin;%PATH%
popd

for /f "usebackq tokens=1* delims=: " %%i in (`vswhere -latest -requires Microsoft.Component.MSBuild`) do (
  if /i "%%i"=="installationPath" set InstallDir=%%j
  if /i "%%i"=="catalog_productLineVersion" set VSVersion=%%j
)

if "%arch%" == "" set arch=x86
call "%InstallDir%\VC\Auxiliary\Build\vcvarsall.bat" %arch% 8.1

if "%arch%" == "x86" IF "%VSVersion%" GEQ "2019" (set MsBuild="%InstallDir%\MSBuild\Current\Bin\msbuild.exe") else (set MsBuild="%InstallDir%\MSBuild\15.0\Bin\msbuild.exe")
if "%arch%" == "x64" if "%VSVersion%" GEQ "2019" (set MsBuild="%InstallDir%\MSBuild\Current\Bin\amd64\msbuild.exe") else (set MsBuild="%InstallDir%\MSBuild\15.0\Bin\amd64\msbuild.exe")

set KEY_NAME="HKLM\SOFTWARE\WOW6432Node\Microsoft\Microsoft SDKs\Windows\v10.0"
set VALUE_NAME=InstallationFolder

REG QUERY %KEY_NAME% /S /v %VALUE_NAME%
FOR /F "tokens=2* delims= " %%1 IN (
  'REG QUERY %KEY_NAME% /v InstallationFolder') DO SET pInstallDir=%%2bin\%arch%;
SET PATH=%PATH%;%pInstallDir%
REM allow typelib registration in redirected registry key even with limited permissions
set OAPERUSERTLIBREG=1

echo "Feedback" %MsBuild%
REM Run the next target only if the previous target succeeded
(
	%MsBuild% /t:RestoreNuGetPackages
) && (
	%MsBuild% /t:CheckDevelopmentPropertiesFile
) && (
	%MsBuild% /t:refreshTargets
) && (
	%MsBuild% %*
)
FOR /F "tokens=*" %%g IN ('date /t') do (SET DATE=%%g)
FOR /F "tokens=*" %%g IN ('time /t') do (SET TIME=%%g)
echo Build completed at %TIME% on %DATE%