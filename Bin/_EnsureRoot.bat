if not "%FWROOT%"=="" goto :LSetBuildRoot

if exist "%~dp0\_setroot.bat" goto :LCall

call :CreateSetRoot "%~dp0\.."

:LCall
call %~dp0\_setroot.bat

:LSetBuildRoot
set BUILD_ROOT=%FWROOT%

goto :EOF

:CreateSetRoot
if "%~1" EQU "" goto :EOF
echo set FWROOT=%~f1> "%~dp0\_setroot.bat"

:EOF
