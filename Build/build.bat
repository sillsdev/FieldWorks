echo off

SETLOCAL ENABLEDELAYEDEXPANSION
SET DELIMITER=-
SET ABSOLUTESTARTDATESTRING=%date:~-4,4%%DELIMITER%%date:~-7,2%%DELIMITER%%date:~-10,2%
SET ABSOLUTESTARTTIMESTRING=%TIME%
::TRIM OFF the LAST 3 characters of ABSOLUTESTARTTIMESTRING, which is the decimal point and hundredths of a second
set ABSOLUTESTARTTIMESTRING=%ABSOLUTESTARTTIMESTRING:~0,-3%
:: Replace colons from ABSOLUTESTARTTIMESTRING with DELIMITER
SET ABSOLUTESTARTTIMESTRING=%ABSOLUTESTARTTIMESTRING::=!DELIMITER!%

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
	set KEY_NAME=HKLM\SOFTWARE\Microsoft\VisualStudio\14.0
) ELSE (
	set KEY_NAME=HKLM\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\14.0
)
set KEY_NAME=%KEY_NAME%\Setup\VS

del CheckOS.txt
del StringCheck.txt

set VALUE_NAME=ProductDir

REM Check for presence of key first.
reg query %KEY_NAME% /v %VALUE_NAME% 2>nul || (echo Build requires VisualStudio 2015! & exit /b 1)

REM query the value. pipe it through findstr in order to find the matching line that has the value. only grab token 3 and the remainder of the line. %%b is what we are interested in here.
set INSTALL_DIR=
for /f "tokens=2,*" %%a in ('reg query %KEY_NAME% /v %VALUE_NAME% ^| findstr %VALUE_NAME%') do (
	set PRODUCT_DIR=%%b
)
call "%PRODUCT_DIR%\VC\vcvarsall.bat" x86 8.1

REM allow typelib registration in redirected registry key even with limited permissions
set OAPERUSERTLIBREG=1

msbuild /t:refreshTargets

SET STARTDATESTRING=%date:~-4,4%%DELIMITER%%date:~-7,2%%DELIMITER%%date:~-10,2%
SET STARTTIMESTRING=%TIME%
::TRIM OFF the LAST 3 characters of STARTTIMESTRING, which is the decimal point and hundredths of a second
set STARTTIMESTRING=%STARTTIMESTRING:~0,-3%
:: Replace colons from STARTTIMESTRING with DELIMITER
SET STARTTIMESTRING=%STARTTIMESTRING::=!DELIMITER!%

msbuild %*

SET ENDDATESTRING=%date:~-4,4%%DELIMITER%%date:~-7,2%%DELIMITER%%date:~-10,2%
SET ENDTIMESTRING=%TIME%
::TRIM OFF the LAST 3 characters of ENDTIMESTRING, which is the decimal point and hundredths of a second
set ENDTIMESTRING=%ENDTIMESTRING:~0,-3%
:: Replace colons from ENDTIMESTRING with DELIMITER
SET ENDTIMESTRING=%ENDTIMESTRING::=!DELIMITER!%
echo ABSOLUTESTARTTIMESTRING %ABSOLUTESTARTDATESTRING%_%ABSOLUTESTARTTIMESTRING: =0%
echo START %STARTDATESTRING%_%STARTTIMESTRING: =0%
echo END %ENDDATESTRING%_%ENDTIMESTRING: =0%
