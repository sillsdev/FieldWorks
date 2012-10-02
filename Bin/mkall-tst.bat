@if "%_echo%"=="" echo off

time /T

rem ***** Set FWROOT and BUILD_ROOT to the root of the FieldWorks project. *****
call %0\..\_EnsureRoot.bat


set FW_TEST_ERROR=
rem FW_TEST_BUILD_ERROR incremented after each failed test build
set FW_TEST_BUILD_ERROR=0
set FW_BUILD_CORE_ERROR=0

call %FWROOT%\bin\mkGenLib-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
call %FWROOT%\bin\_setTstBuildError mkGenLib-tst

call %FWROOT%\bin\mkaflib-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
call %FWROOT%\bin\_setTstBuildError mkaflib-tst

call %FWROOT%\bin\mkcel-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
call %FWROOT%\bin\_setTstBuildError mkcel-tst

call %FWROOT%\bin\mkfwk-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
call %FWROOT%\bin\_setTstBuildError mkfwk-tst

call %FWROOT%\bin\mklg-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
call %FWROOT%\bin\_setTstBuildError mklg-tst

call %FWROOT%\bin\mkvw-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
call %FWROOT%\bin\_setTstBuildError mkvw-tst

call %FWROOT%\bin\mkdba-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
call %FWROOT%\bin\_setTstBuildError mkdba-tst

call %FWROOT%\bin\mkComFWDlgs-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
call %FWROOT%\bin\_setTstBuildError mkComFWDlgs-tst

call %FWROOT%\bin\mkDbSvcs-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
call %FWROOT%\bin\_setTstBuildError mkDbSvcs-tst

call %FWROOT%\bin\mkmig-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
call %FWROOT%\bin\_setTstBuildError mkmig-tst

call %FWROOT%\bin\mkcle-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
call %FWROOT%\bin\_setTstBuildError mkcle-tst

rem call %FWROOT%\bin\mkteso-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
rem call %FWROOT%\bin\_setTstBuildError mkteso-tst

rem echo FW_TEST_ERROR= %FW_TEST_ERROR%
rem echo FW_TEST_BUILD_ERROR= %FW_TEST_BUILD_ERROR%
rem echo FW_BUILD_CORE_ERROR= %FW_BUILD_CORE_ERROR%

set FW_OVNITE_REPORT=0
if "%FW_TEST_ERROR%" == "1" set /A FW_OVNITE_REPORT += 1
if /i "%FW_TEST_BUILD_ERROR%" NEQ "0" set /A FW_OVNITE_REPORT += 2

if "%FW_TEST_ERROR%" == "1" goto showbar
if /i "%FW_TEST_BUILD_ERROR%" NEQ "0" set FW_TEST_ERROR=2
:showbar
greenbar.exe FW_TEST_ERROR

if "%FW_BUILD_CORE_ERROR%"=="1" set FW_BUILD_ERROR=1
if "%FW_TEST_ERROR%"=="1" set FW_BUILD_ERROR=1

time /T

set FW_TEST_ERROR=
rem echo FW_OVNITE_REPORT= %FW_OVNITE_REPORT%
set errorlevel=%FW_OVNITE_REPORT%
