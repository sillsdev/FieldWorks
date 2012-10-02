@if "%_echo%"=="" echo off

rem ***** Set BUILD_ROOT to the root of the FieldWorks project. *****
call %0\..\..\..\bin\_EnsureRoot.bat

set BUILD_MAKEFILE=%BUILD_ROOT%\Samples\Interlin\InterlinExample.mak

rem ***** Create the version include file *****
%BUILD_ROOT%\bin\mkverrsc.exe %BUILD_ROOT%\Output\Common\bldinc.h 0 3 %BUILD_LEVEL%

call %BUILD_ROOT%\bld\_mkcore.bat %1 %2 %3 %4 %5 %6 %7 %8 %9

set BUILD_MAKEFILE=
