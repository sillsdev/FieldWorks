@if "%_echo%"=="" echo off

rem ***** Set BUILD_ROOT to the root of the FieldWorks project. *****
call %0\..\_EnsureRoot.bat

set BUILD_MAKEFILE=%BUILD_ROOT%\src\Kernel\FwKernel.mak

call %BUILD_ROOT%\bld\_mkcore.bat %1 %2 %3 %4 %5 %6 %7 %8 %9

set BUILD_MAKEFILE=
