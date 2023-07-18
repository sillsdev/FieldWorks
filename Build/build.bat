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

for /f "usebackq tokens=1* delims=: " %%i in (`vswhere -version "17.0" -requires Microsoft.Component.MSBuild`) do (
  if /i "%%i"=="installationPath" set InstallDir=%%j
  if /i "%%i"=="catalog_productLineVersion" set VSVersion=%%j
)

if "%arch%" == "" set arch=x86

REM run Microsoft's batch file to set all the environment variables and path necessary to build a C++ app
set VcVarsLoc=%InstallDir%\VC\Auxiliary\Build\vcvarsall.bat

if exist "%VcVarsLoc%" (
  call "%VcVarsLoc%" %arch%
) else (
  echo "Could not find: %VcVarsLoc% something is wrong with the Visual Studio installation"
  GOTO End
)


if "%arch%" == "x86" set MsBuild="%InstallDir%\MSBuild\Current\Bin\msbuild.exe"
if "%arch%" == "x64" set MsBuild="%InstallDir%\MSBuild\Current\Bin\amd64\msbuild.exe"

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
	%MsBuild% Src\FwBuildTasks\FwBuildTasks.sln /t:Restore;Build /p:Platform="Any CPU"
) && (
	if "%all_args:disableDownloads=%"=="%all_args%" %MsBuild% FieldWorks.proj /t:RestoreNuGetPackages
) && (
	%MsBuild% FieldWorks.proj /t:CheckDevelopmentPropertiesFile
) && (
	%MsBuild% FieldWorks.proj /t:refreshTargets
) && (
	%MsBuild% FieldWorks.proj %*
)
:END
FOR /F "tokens=*" %%g IN ('date /t') do (SET DATE=%%g)
FOR /F "tokens=*" %%g IN ('time /t') do (SET TIME=%%g)
echo Build completed at %TIME% on %DATE%