@echo off

REM build the ONLINE EXE bundle.
(
) && (
	echo.
	echo [ERROR] Legacy WiX 3 batch build script.
	echo This repository builds bundles with WiX Toolset v6 using MSBuild (.wixproj).
	echo Use one of these instead:
	echo   - .\build.ps1
	echo   - msbuild Build\Orchestrator.proj /t:BuildInstaller
	echo.
	exit /b 1
)