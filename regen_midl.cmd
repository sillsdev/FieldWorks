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

REM Locate the newest supported Visual Studio via vswhere.
REM The version range mirrors FwVisualStudioVersionRange in Build\FieldWorks.Toolchain.props.
REM The result goes through a temp file because cmd's for /f parsing cannot safely
REM carry both the "(x86)" path parenthesis and the version-range parenthesis.
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" set "VSWHERE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo ERROR: Cannot find vswhere.exe - install Visual Studio 2026 or 2022
    exit /b 1
)
set "VSROOT="
set "VSROOT_FILE=%TEMP%\fw_vsroot_%RANDOM%.txt"
"%VSWHERE%" -latest -products * -version "[17.0,19.0)" -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath > "%VSROOT_FILE%"
set /p VSROOT=<"%VSROOT_FILE%"
del "%VSROOT_FILE%" >nul 2>&1
if not defined VSROOT (
    echo ERROR: No Visual Studio 2026/2022 installation with C++ build tools was found
    exit /b 1
)
call "%VSROOT%\VC\Auxiliary\Build\vcvarsall.bat" x64

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
