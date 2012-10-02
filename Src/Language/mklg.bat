@if "%_echo%"=="" echo off

rem ***** Set BUILD_ROOT to the root of the FieldWorks project. *****
%0\..\..\..\..\bin\here.exe ".." "set BUILD_ROOT=" > foo.bat
call foo.bat
del foo.bat

set BUILD_MAKEFILE=%BUILD_ROOT%\Src\Services\Language\Language.mak

call %BUILD_ROOT%\bld\_mkcore.bat %1 %2 %3 %4 %5 %6 %7 %8 %9

set BUILD_MAKEFILE=
