@if "%_echo%"=="" echo off

time /T

if not "%OS%"=="" setlocal

rem ***** Set FWROOT and BUILD_ROOT to the root of the FieldWorks project. *****
call %0\..\_EnsureRoot.bat

goto CHECK_OPTIONS

:SET_OPTION
set %OPTION%=%OPTION_VALUE%
shift

:CHECK_OPTIONS

set OPTION=BUILD_TYPE
set OPTION_VALUE=%1
if "%1"=="d" goto SET_OPTION
if "%1"=="r" goto SET_OPTION
if "%1"=="p" goto SET_OPTION
if "%1"=="b" goto SET_OPTION

set OPTION=BUILD_ACTION
set OPTION_VALUE=%1
if "%1"=="e" goto SET_OPTION
if "%1"=="c" goto SET_OPTION
if "%1"=="i" goto SET_OPTION
if "%1"=="cc" goto SET_OPTION
if "%1"=="ec" goto SET_OPTION

set OPTION=BUILD_EXTRA
set OPTION_VALUE=%1
if "%1"=="register" goto SET_OPTION
if "%1"=="unregister" goto SET_OPTION

rem === Set up ===
if "%BUILD_TYPE%"=="" set BUILD_TYPE=d
if "%BUILD_ACTION%"=="" set BUILD_ACTION=i

if "%BUILD_TYPE%"=="b" set BUILD_CONFIG=bounds
if "%BUILD_TYPE%"=="d" set BUILD_CONFIG=debug
if "%BUILD_TYPE%"=="r" set BUILD_CONFIG=release
if "%BUILD_TYPE%"=="p" set BUILD_CONFIG=profile

set ACTION=buildtest
if "%BUILD_ACTION%"=="c" set ACTION=buildtest cc
if "%BUILD_ACTION%"=="cc" set ACTION=buildtest cc
if "%BUILD_ACTION%"=="e" set ACTION=clean
if "%BUILD_ACTION%"=="ec" set ACTION=clean

if "%BUILD_EXTRA%"=="register" set ACTION=%ACTION% register
if "%BUILD_EXTRA%"=="unregister" set ACTION=%ACTION% unregister

cd %FWROOT%\Bld

%FWROOT%\bin\nant\bin\nant -t:net-3.5 %BUILD_CONFIG% %ACTION% mkall
