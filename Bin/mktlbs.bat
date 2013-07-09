@if "%_echo%"=="" echo off

rem ***** Set BUILD_ROOT to the root of the FieldWorks project. *****
call %0\..\_EnsureRoot.bat

set BUILD_MAKEFILE=%BUILD_ROOT%\Src\Language\Language.mak

call %BUILD_ROOT%\bld\_mkcore.bat dirs %BUILD_OUTPUT%\Common\LanguageTlb.tlb

set BUILD_MAKEFILE=%BUILD_ROOT%\Src\Kernel\FwKernel.mak
call %BUILD_ROOT%\bld\_mkcore.bat dirs %BUILD_OUTPUT%\Common\FwKernelTlb.tlb

set BUILD_MAKEFILE=%BUILD_ROOT%\Src\Views\Views.mak
call %BUILD_ROOT%\bld\_mkcore.bat dirs %BUILD_OUTPUT%\Common\ViewsTlb.tlb

set BUILD_MAKEFILE=
