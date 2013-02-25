echo off

REM cause Environment variable changes to be lost after this process dies:
if not "%OS%"=="" setlocal

REM Add Bin and DistFiles to the PATH:
pushd %~dp0
cd ..
set PATH=%cd%\DistFiles;%cd%\Bin;%PATH%
popd

Set RegQry=HKLM\Hardware\Description\System\CentralProcessor\0

REG.exe Query %RegQry% > checkOS.txt

Find /i "x86" < CheckOS.txt > StringCheck.txt

If %ERRORLEVEL% == 0 (
	set KEY_NAME=HKLM\SOFTWARE\Microsoft\VisualStudio\10.0
) ELSE (
	set KEY_NAME=HKLM\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0
)

del CheckOS.txt
del StringCheck.txt

set VALUE_NAME=InstallDir

REM Check for presence of key first.
reg query %KEY_NAME% /v %VALUE_NAME% 2>nul || (echo Build requires VisualStudio 2010! & exit /b 1)

REM query the value. pipe it through findstr in order to find the matching line that has the value. only grab token 3 and the remainder of the line. %%b is what we are interested in here.
set INSTALL_DIR=
for /f "tokens=2,*" %%a in ('reg query %KEY_NAME% /v %VALUE_NAME% ^| findstr %VALUE_NAME%') do (
	set INSTALL_DIR=%%b
)
call "%INSTALL_DIR%\..\..\VC\vcvarsall.bat"

REM allow typelib registration in redirected registry key even with limited permissions
set OAPERUSERTLIBREG=1

msbuild /t:refreshTargets
msbuild %*