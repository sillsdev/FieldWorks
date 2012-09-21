echo off

REM cause Environment variable changes to be lost after this process dies:
if not "%OS%"=="" setlocal

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
reg query %KEY_NAME% /v %VALUE_NAME% 2>nul || (echo No theme name present! & exit /b 1)

REM query the value. pipe it through findstr in order to find the matching line that has the value. only grab token 3 and the remainder of the line. %%b is what we are interested in here.
set INSTALL_DIR=
for /f "tokens=2,*" %%a in ('reg query %KEY_NAME% /v %VALUE_NAME% ^| findstr %VALUE_NAME%') do (
	set INSTALL_DIR=%%b
)
call "%INSTALL_DIR%\..\..\VC\vcvarsall.bat"

msbuild %*