@echo off
REM VsDevShell.cmd - Initialize Visual Studio Build Tools environment
REM Works with both Docker containers (C:\BuildTools) and developer machines (VS Enterprise/Community)

REM Try VSINSTALLDIR environment variable first (set by Post-Install-Setup.ps1 in containers)
if defined VSINSTALLDIR (
    if exist "%VSINSTALLDIR%VC\Auxiliary\Build\vcvarsall.bat" (
        call "%VSINSTALLDIR%VC\Auxiliary\Build\vcvarsall.bat" x64
        goto :run_command
    )
)

REM Fallback to hardcoded Docker BuildTools path
if exist "C:\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" (
    call "C:\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x64
    goto :run_command
)

REM Fallback: Try to find VS using vswhere
for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath 2^>nul`) do (
    if exist "%%i\VC\Auxiliary\Build\vcvarsall.bat" (
        call "%%i\VC\Auxiliary\Build\vcvarsall.bat" x64
        goto :run_command
    )
)

echo Warning: vcvarsall.bat not found. Visual Studio environment not initialized.

:run_command
REM Execute the command passed as arguments, or start a persistent shell
if "%~1"=="" (
    cmd.exe /k
) else (
    %*
)
