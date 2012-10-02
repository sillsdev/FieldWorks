@if "%_echo%"=="" echo off

set FW_BUILD_CORE_ERROR=0
rem This returns the environment error flag as an errorlevel for NAnt calls
rem The test batch files use "exit /b x", but this doesn't come through to NAnt
call %1 %2 %3 %4 %5 %6 %7 %8 %9

rem Report a meaningful error level.
if "%FW_BUILD_CORE_ERROR%"=="1" exit 1
if "%FW_TEST_ERROR%"=="1" exit 2
exit 0
