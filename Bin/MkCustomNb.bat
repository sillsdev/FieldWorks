rem Build AfLib and Notebook

@if "%_echo%"=="" echo off
set FW_BUILD_ERROR=0
set FW_BUILD_CORE_ERROR=0
if not "%OS%"=="" setlocal
call %0\..\_EnsureRoot.bat

rem ********* AfLib ***************
call %FWROOT%\bin\mkAfLib.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
if "%FW_BUILD_CORE_ERROR%"=="1" set FW_BUILD_ERROR=1
if "%FW_BUILD_ERROR%"=="1" goto BuildError

rem ********* Notebook ***************
if exist %FWROOT%\output\debug\FwNotebook.exe del %FWROOT%\output\debug\FwNotebook.exe
call %FWROOT%\bin\mknb %1 %2 %3 %4 %5 %6 %7 %8 %9
if "%FW_BUILD_CORE_ERROR%"=="1" set FW_BUILD_ERROR=1
if "%FW_BUILD_ERROR%"=="1" goto BuildError

if not "%OS%"=="" endlocal
set FW_BUILD_ERROR=0
echo **********************************************************
echo Success!
echo **********************************************************
goto Done

:BuildError
if not "%OS%"=="" endlocal
set FW_BUILD_ERROR=1
echo **********************************************************
echo WARNING: There was at least one error somewhere.
echo **********************************************************

:Done
set FW_BUILD_CORE_ERROR=
