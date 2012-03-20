rem Completely rebuild FieldWorks (Bounds, Debug, or Release version) and test databases
rem First parameter is build configuration: d (default), b, r
rem Second parameter is build action: buildtest (default), build, test, clean, register, unregister, forcetests, cc, i

@if "%_echo%"=="" echo off
set FW_BUILD_ERROR=0
set FW_BUILD_CORE_ERROR=0
if not "%OS%"=="" setlocal
call %0\..\_EnsureRoot.bat

set CONFIG=
if "%1"=="" set CONFIG=debug
if "%1"=="b" set CONFIG=bounds
if "%1"=="d" set CONFIG=debug
if "%1"=="r" set CONFIG=release

set ACTION=%2
if "%2"=="" set ACTION=buildtest

rem Can't call vcvars32.bat because it gives wrong order for include files.
rem call vcvars32.bat

rem Delete WhatsThisHelp from GAC
gacutil /u WhatsThisHelp

cd %FWROOT%\Bld

%FWROOT%\bin\nant\bin\nant %CONFIG% %ACTION% remakefw
