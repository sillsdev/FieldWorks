@if "%_echo%"=="" echo off

set FW_BUILD_CORE_ERROR=0
set OUT_DIR=%1
shift
set COM_OUT_DIR=%1
shift
set OBJ_DIR=%1
shift
rem This returns the environment error flag as an errorlevel for NAnt calls
rem We can't do this in mkall.bat because it exits rebuildall.bat
call %1 %2 %3 %4 %5 %6 %7 %8 %9
exit %FW_BUILD_CORE_ERROR%
