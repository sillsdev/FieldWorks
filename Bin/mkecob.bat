@if "%_echo%"=="" echo off

rem ***** Set BUILD_ROOT to the root of the FieldWorks project. *****
call %0\..\_EnsureRoot.bat

set BUILD_PROJ=ECObjects
set BUILD_MAKEFILE=%BUILD_ROOT%\Src\%BUILD_PROJ%\%BUILD_PROJ%.mak
pushd %BUILD_ROOT%\Src\%BUILD_PROJ%

rem call %BUILD_ROOT%\bld\_mkcore.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
rem         type: d - Debug, r - Release, b - Bounds checking (debug)
rem       action: c - Clean build, i - Incremental build, e - Erase
rem               cc - Complete Clean build, ec - Complete Erase
rem               (Complete includes the common targets)
goto CHECK_OPTIONS

:SET_OPTION
set %OPTION%=%OPTION_VALUE%
shift

:CHECK_OPTIONS
set OPTION=BUILD_TYPE
set OPTION_VALUE=%1
if "%1"=="d" goto SET_OPTION
if "%1"=="r" goto SET_OPTION
if "%1"=="p" goto SET_OPTION
if "%1"=="b" goto SET_OPTION

set OPTION=BUILD_ACTION
set OPTION_VALUE=%1
if "%1"=="e" goto SET_OPTION
if "%1"=="c" goto SET_OPTION
if "%1"=="i" goto SET_OPTION
if "%1"=="cc" goto SET_OPTION
if "%1"=="ec" goto SET_OPTION

rem === Set up ===
if "%BUILD_TYPE%"=="" set BUILD_TYPE=d
if "%BUILD_ACTION%"=="" set BUILD_ACTION=i

if "%BUILD_TYPE%"=="b" set BUILD_CONFIG=Bounds
if "%BUILD_TYPE%"=="d" set BUILD_CONFIG=Debug
if "%BUILD_TYPE%"=="r" set BUILD_CONFIG=Release
if "%BUILD_TYPE%"=="p" set BUILD_CONFIG=Profile
if "%BUILD_CONFIG%"=="" set BUILD_CONFIG=Debug
rem the next line exports BUILD_CONFIG for use by unit test batch files.
echo set BUILD_CONFIG=%BUILD_CONFIG%>%BUILD_ROOT%\Bin\_setLatestBuildConfig.bat

rem ** The Nant build system may put in another batch file parameter before the tlb, so check
rem ** several places for it.
if "%1"=="tlb" goto RUNMIDL
if "%2"=="tlb" goto RUNMIDL
if "%3"=="tlb" goto RUNMIDL
if "%4"=="tlb" goto RUNMIDL
if "%5"=="tlb" goto RUNMIDL

if "%BUILD_ACTION%"=="e" set BUILD_COMMAND=clean
if "%BUILD_ACTION%"=="c" set BUILD_COMMAND=/a
if "%BUILD_ACTION%"=="i" set BUILD_COMMAND=
if "%BUILD_ACTION%"=="cc" set BUILD_COMMAND=/a
if "%BUILD_ACTION%"=="ec" set BUILD_COMMAND=clean

if "%BUILD_ACTION%"=="cc" goto ERASE_COM1
if "%BUILD_ACTION%"=="ec" goto ERASE_COM1
goto BUILD_DLL

:ERASE_COM1
del /q "%BUILD_OUTPUT%\Common\%BUILD_PROJ%.*"
del /q "%BUILD_OUTPUT%\Common\%BUILD_PROJ%_*.*"
del /q "%BUILD_OUTPUT%\Common\%BUILD_PROJ%-*.*"

:BUILD_DLL
echo nmake -f %BUILD_MAKEFILE% CFG="%BUILD_CONFIG%" %BUILD_COMMAND%
nmake -f %BUILD_MAKEFILE% CFG="%BUILD_CONFIG%" %BUILD_COMMAND%
goto DONE

:RUNMIDL
rem we want to generate the TLB/H files for the interface without compiling the whole thing

if "%BUILD_ACTION%"=="e" goto ERASE
if "%BUILD_ACTION%"=="ec" goto ERASE_COM2
if "%BUILD_ACTION%"=="c" goto ERASE
if "%BUILD_ACTION%"=="cc" goto ERASE_COM2
goto BUILD_TLB

:ERASE_COM2
del /q "%BUILD_OUTPUT%\Common\%BUILD_PROJ%.*"
del /q "%BUILD_OUTPUT%\Common\%BUILD_PROJ%_*.*"
del /q "%BUILD_OUTPUT%\Common\%BUILD_PROJ%-*.*"
:ERASE

if "%BUILD_ACTION%"=="e" goto DONE
if "%BUILD_ACTION%"=="ec" goto DONE

:BUILD_TLB

echo nmake -f %BUILD_MAKEFILE% CFG="%BUILD_CONFIG%" ..\..\Output\Common\%BUILD_PROJ%.tlb
nmake -f %BUILD_MAKEFILE% CFG="%BUILD_CONFIG%" ..\..\Output\Common\%BUILD_PROJ%.tlb

:DONE
popd
set BUILD_PROJ=
set BUILD_ACTION=
set BUILD_TYPE=
set BUILD_MAKEFILE=
