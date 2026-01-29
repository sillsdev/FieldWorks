@echo off
REM Usage: regen_midl.cmd [Configuration]
REM   Configuration: Debug or Release (default: Debug)
REM This batch file sets the configuration parameter for the MIDL (Microsoft Interface Definition Language) regeneration process.
REM It expects one argument which represents the build configuration (e.g., Debug, Release) to be used when regenerating MIDL files.
REM The configuration is stored in the CONFIG environment variable for use in subsequent build steps.
REM Usage: regen_midl.cmd <configuration>
setlocal
set CONFIG=%~1
if "%CONFIG%"=="" set CONFIG=Debug

REM Find vcvarsall.bat - try VS 2022 Community, then BuildTools
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
) else if exist "C:\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" (
    call "C:\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64
) else (
    echo ERROR: Cannot find vcvarsall.bat
    exit /b 1
)

REM Navigate to configuration-specific output directory
cd /d "%~dp0Output\%CONFIG%\Common"
if errorlevel 1 (
    echo ERROR: Directory Output\%CONFIG%\Common does not exist
    exit /b 1
)

echo Running MIDL for x64 (%CONFIG% configuration)...
midl /env x64 /Oicf /out Raw /dlldata FwKernelPs_d.c FwKernelPs.idl
echo MIDL exit code: %ERRORLEVEL%
endlocal
