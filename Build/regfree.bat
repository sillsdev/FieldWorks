@echo off

REM This file gets called from RegFree.targets. The challenge we face
REM is that we don't know the path to the Win 10 SDK (which contains mt.exe)
REM if we build from the IDE. This batch file sets the environment variables
REM and then calls mt.exe

call %InstallDir%\VC\Auxiliary\Build\vcvarsall.bat x64 > nul

"%WindowsSdkVerBinPath%x64\mt.exe" -nologo %*
