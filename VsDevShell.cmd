@echo off
REM VsDevShell.cmd - Initialize Visual Studio Build Tools environment for Docker container

REM Call vcvarsall.bat to set up the build environment
REM This sets up the complex VC++ environment variables that are hard to replicate manually
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
