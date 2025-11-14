@echo off
REM VsDevShell.cmd - Initialize Visual Studio Build Tools environment for Docker container

REM Set the Visual Studio installation path
set "VSINSTALLDIR=C:\BuildTools\"
set "VCINSTALLDIR=C:\BuildTools\VC\"

REM Call vcvarsall.bat to set up the build environment
if exist "C:\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" (
    call "C:\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64
) else (
    echo Warning: vcvarsall.bat not found at expected location
)

REM Execute the command passed as arguments, or start a persistent shell
if "%~1"=="" (
    cmd.exe /k
) else (
    %*
)
