@if "%_echo%"=="" echo off

rem ***** Set BUILD_ROOT to the root of the FieldWorks project. *****
call %0\..\_EnsureRoot.bat

set BUILD_ONLY_NOTEST=%1
if "%BUILD_ONLY_NOTEST%"=="DONTRUN" shift

set BUILD_MAKEFILE=%BUILD_ROOT%\src\Cellar\Test\testFwCellar.mak

rem ***** Create the version include file *****
%BUILD_ROOT%\bin\nant\bin\nant.exe -buildfile:"%BUILD_ROOT%\bld\Version.build.xml" -D:BUILD_ROOT="%BUILD_ROOT%" -D:BUILD_LEVEL=%BUILD_LEVEL%

call %BUILD_ROOT%\bld\_mkcore.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
set BUILD_MAKEFILE=

if "%FW_BUILD_CORE_ERROR%"=="1" goto done
if "%BUILD_ONLY_NOTEST%"=="DONTRUN" goto done

call %BUILD_ROOT%\Bin\_setLatestBuildConfig.bat
echo .
%BUILD_ROOT%\Output\%BUILD_CONFIG%\testFwCellar.exe
echo .
if not ERRORLEVEL 1 goto done
echo ******** testFwCellar.exe FAILED! ********
set FW_TEST_ERROR=1

:done
set BUILD_CONFIG=
set BUILD_ONLY_NOTEST=

rem Report a meaningful error level.
if "%FW_BUILD_CORE_ERROR%"=="1" exit /b 1
if "%FW_TEST_ERROR%"=="1" exit /b 2
exit /b 0