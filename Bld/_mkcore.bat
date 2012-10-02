@if "%_echo%"=="" echo off
if not "%OS%"=="" setlocal
set BUILD_TYPE=
set BUILD_ACTION=

echo In _mkcore.bat 1=%1 2="%2"; 3="%3".
set ERROR_MESSAGE=You must set BUILD_ROOT and BUILD_MAKEFILE.
if "%BUILD_ROOT%"=="" goto PRINT_ERROR
if "%BUILD_MAKEFILE%"=="" goto PRINT_ERROR

rem BUILD_TYPE - one of { b (bounds), d (for debug), r (for release), p (profile)}
rem BUILD_ACTION - one of { e (for erase), c (for clean), i (for incremental), ec (for erase complete), cc (for clean complete) }
rem BUILD_OS - one of { winnt, win95 }


goto CHECK_OPTIONS

:SET_OPTION
set %OPTION%=%OPTION_VALUE%
shift

:CHECK_OPTIONS
if "%1"=="/?" goto USAGE
if "%1"=="-?" goto USAGE

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

rem ANAL_TYPE is used by the Bounds build to turn on performance analysis instrumentation
set OPTION=ANAL_TYPE
set OPTION_VALUE=%1
if "%1"=="performance" goto SET_OPTION

rem === Set up ===
if "%BUILD_TYPE%"=="" set BUILD_TYPE=d
if "%BUILD_ACTION%"=="" set BUILD_ACTION=i

set BUILD_OS=winnt
if "%OS%"=="" set BUILD_OS=win95

if "%BUILD_TYPE%"=="b" set BUILD_CONFIG=Bounds
if "%BUILD_TYPE%"=="d" set BUILD_CONFIG=Debug
if "%BUILD_TYPE%"=="r" set BUILD_CONFIG=Release
if "%BUILD_TYPE%"=="p" set BUILD_CONFIG=Profile
rem the next line exports BUILD_CONFIG for use by unit test batch files.
echo set BUILD_CONFIG=%BUILD_CONFIG%>%BUILD_ROOT%\Bin\_setLatestBuildConfig.bat

rem == Execute ===
set ERROR_MESSAGE=Cannot find makefile %BUILD_MAKEFILE%.
if not exist %BUILD_MAKEFILE% goto PRINT_ERROR

echo =====================================================================
echo === Performing %BUILD_ACTION% %BUILD_CONFIG% on %BUILD_MAKEFILE%. ===
echo =====================================================================
if "%BUILD_ACTION%"=="i" goto BUILD

echo on
nmake /nologo %BUILD_NMAKEFLAGS% /f %BUILD_MAKEFILE% clean
@if "%_echo%"=="" echo off

if "%BUILD_ACTION%"=="e" goto DONE
if "%BUILD_ACTION%"=="c" goto BUILD

echo on
nmake /nologo %BUILD_NMAKEFLAGS% /f %BUILD_MAKEFILE% cleancom
@if "%_echo%"=="" echo off

if "%BUILD_ACTION%"=="ec" goto DONE

:BUILD
echo on
@set CORE_ERROR=0
nmake /nologo %BUILD_NMAKEFLAGS% /f %BUILD_MAKEFILE% %1 %2 %3 %4 %5 %6
@if errorlevel 1 set CORE_ERROR=1
@if "%_echo%"=="" echo off
goto DONE





:USAGE
echo.
echo Usage: mk [options] nmake_options...
echo.
echo         type: d - Debug, r - Retail, b - Bounds checking (debug)
echo       action: c - Clean build, i - Incremental build, e - Erase
echo               cc - Complete Clean build, ec - Complete Erase
echo               (Complete includes the common targets)
echo.
goto DONE


:PRINT_ERROR
echo ERROR: %ERROR_MESSAGE%


:DONE
if "%CORE_ERROR%"=="1" goto BuildError
if not "%OS%"=="" endlocal
goto ReallyDone

:BuildError
if not "%OS%"=="" endlocal
set FW_BUILD_CORE_ERROR=1

:ReallyDone
