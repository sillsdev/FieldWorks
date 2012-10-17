rem Completely rebuild FieldWorks (Bounds, Debug, or Release version) and test databases
rem First parameter is build configuration: d (default), b, r
rem Second parameter is build action: build (default), test, clean

@if "%_echo%"=="" echo off
set FW_BUILD_ERROR=0
set FW_BUILD_CORE_ERROR=0
if not "%OS%"=="" setlocal
call %0\..\_EnsureRoot.bat

set CONFIG=
if "%1"=="b" set CONFIG=/property:config=bounds
if "%1"=="r" set CONFIG=/property:config=release

set ACTION=
if "%2"=="test" set ACTION=/property:action=test
if "%2"=="clean" set ACTION=/property:action=clean

rem Delete WhatsThisHelp from GAC
gacutil /u WhatsThisHelp

cd %FWROOT%\Build

msbuild.exe /t:remakefw %CONFIG% %ACTION%