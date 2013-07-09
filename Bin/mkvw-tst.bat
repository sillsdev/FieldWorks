@if "%_echo%"=="" echo off

rem ***** Set BUILD_ROOT to the root of the FieldWorks project. *****
call %0\..\_EnsureRoot.bat

set BUILD_ONLY_NOTEST=%1
if "%BUILD_ONLY_NOTEST%"=="DONTRUN" shift

set BUILD_MAKEFILE=%BUILD_ROOT%\src\Views\Test\testViews.mak

call %BUILD_ROOT%\bld\_mkcore.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
set BUILD_MAKEFILE=

if "%FW_BUILD_CORE_ERROR%"=="1" goto done
if "%BUILD_ONLY_NOTEST%"=="DONTRUN" goto done

call %BUILD_ROOT%\Bin\_setLatestBuildConfig.bat
echo .
%BUILD_ROOT%\Output\%BUILD_CONFIG%\testViews.exe
echo .
rem NOTE: Windows batch files always implicitly compare errorlevels >=, so the following line
rem will ONLY go to done if errorlevel == 0 (or rather, <= 0). The echo command doesn't set errorlevel.
if NOT ERRORLEVEL 1 goto done
echo ******** testViews.exe FAILED! ********
set FW_TEST_ERROR=1

:done
set BUILD_CONFIG=
set BUILD_ONLY_NOTEST=

rem Report a meaningful error level.
if "%FW_BUILD_CORE_ERROR%"=="1" exit /b 1
if "%FW_TEST_ERROR%"=="1" exit /b 2
exit /b 0
