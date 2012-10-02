::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: CleanOutputDirs.bat
:: -------------------
:: Cleans all of the bin and obj directories for the Votive projects. This is needed whenever
:: switching from the VS 2003 and VS 2005 projects since they both build to the same directories.
:: If you don't clean the directories, then the project won't build correctly.
::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

@echo off
setlocal

:: Directories
set SCONCE_ROOT=%~dp0..\..\sconce
set VOTIVE_ROOT=%~dp0..\Core
set VOTIVEUI_ROOT=%~dp0..\Satellite\1033

echo Cleaning sconce
if exist %SCONCE_ROOT%\bin rd /s /q %SCONCE_ROOT%\bin
if exist %SCONCE_ROOT%\obj rd /s /q %SCONCE_ROOT%\obj

echo Cleaning votive
if exist %VOTIVE_ROOT%\bin rd /s /q %VOTIVE_ROOT%\bin
if exist %VOTIVE_ROOT%\obj rd /s /q %VOTIVE_ROOT%\obj

echo Cleaning votiveui
if exist %VOTIVEUI_ROOT%\Debug rd /s /q %VOTIVEUI_ROOT%\Debug
if exist %VOTIVEUI_ROOT%\Release rd /s /q %VOTIVEUI_ROOT%\Release

:End
endlocal
exit /b 0
