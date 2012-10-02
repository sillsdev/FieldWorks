@if "%_echo%"=="" echo off
rem ***** Set BUILD_ROOT to the root of the FieldWorks project. *****
call %0\..\_EnsureRoot.bat
cd %BUILD_ROOT%
cvs update -d >cvsupdate.log
if not errorlevel 1 goto done
echo ERROR in cvs update!!  look at %BUILD_ROOT%\cvsupdate.log for details.
:done
cd bin
pause