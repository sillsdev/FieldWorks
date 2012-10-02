@if "%_echo%"=="" echo off

rem ***** Set BUILD_ROOT to the root of the FieldWorks project. *****
call %0\..\_EnsureRoot.bat

set BUILD_MAKEFILE=%BUILD_ROOT%\Src\Language\Language.mak

rem ***** Create the version include file *****
%BUILD_ROOT%\bin\nant\bin\nant.exe -buildfile:"%BUILD_ROOT%\bld\Version.build.xml" -D:BUILD_ROOT="%BUILD_ROOT%" -D:BUILD_LEVEL=%BUILD_LEVEL%

call %BUILD_ROOT%\bld\_mkcore.bat dirs %BUILD_ROOT%\Output\Common\LanguageTlb.tlb

set BUILD_MAKEFILE=%BUILD_ROOT%\Src\Kernel\FwKernel.mak
call %BUILD_ROOT%\bld\_mkcore.bat dirs %BUILD_ROOT%\Output\Common\FwKernelTlb.tlb

set BUILD_MAKEFILE=%BUILD_ROOT%\Src\Views\Views.mak
call %BUILD_ROOT%\bld\_mkcore.bat dirs %BUILD_ROOT%\Output\Common\ViewsTlb.tlb

set BUILD_MAKEFILE=%BUILD_ROOT%\Src\Cellar\FwCellar.mak
call %BUILD_ROOT%\bld\_mkcore.bat dirs %BUILD_ROOT%\Output\Common\FwCellarTlb.tlb

set BUILD_MAKEFILE=
