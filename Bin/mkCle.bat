@if "%_echo%"=="" echo off

rem ***** Set BUILD_ROOT to the root of the FieldWorks project. *****
call %0\..\_EnsureRoot.bat

set BUILD_MAKEFILE=%BUILD_ROOT%\src\Cle\Cle.mak

rem ***** Create the version include file *****
%BUILD_ROOT%\bin\nant\bin\nant.exe -buildfile:"%BUILD_ROOT%\bld\Version.build.xml" -D:BUILD_ROOT="%BUILD_ROOT%" -D:BUILD_LEVEL=%BUILD_LEVEL%

call %BUILD_ROOT%\bld\_mkcore.bat %1 %2 %3 %4 %5 %6 %7 %8 %9

set BUILD_MAKEFILE=
