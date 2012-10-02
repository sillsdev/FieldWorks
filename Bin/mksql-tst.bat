@if "%_echo%"=="" echo off

time /T

rem ***** Set BUILD_ROOT to the root of the FieldWorks project. *****
call %0\..\_EnsureRoot.bat

echo =====================================================================
echo Running %BUILD_ROOT%\Test\Python23\python fw_sqlunit.py ...
echo =====================================================================
pushd %BUILD_ROOT%\Test\tsqlunit
%BUILD_ROOT%\Test\Python23\python fw_sqlunit.py

set save_errorlevel=%errorlevel%
rem python sets the errorlevel environment variable
popd

time /T

set errorlevel=%save_errorlevel%
