@if "%_echo%"=="" echo off
if NOT "%BUILD_BSC%"=="Y" goto ALL_DONE

set ERROR_MESSAGE=You must set BUILD_ROOT.
if "%BUILD_ROOT%"=="" goto PRINT_ERROR

set BUILD_CONFIG=
set NUL=
set DEL_BSC=
if "%BUILD_OS%"=="win95" set NUL=nul

:CHECK_FOR_CC
if "%1"=="cc" set DEL_BSC=Y
if "%1"=="d" set BUILD_CONFIG=Debug
if "%1"=="" goto NO_MORE_PARMS
shift
goto CHECK_FOR_CC

:NO_MORE_PARMS
::: Delete the SrcBrwsr directory if this is a cleancom debug build
if NOT "%DEL_BSC%"=="Y" goto END
if NOT "%BUILD_CONFIG%"=="Debug" goto END
if "%BSC_INT_DIR%"=="" goto DEFAULT_BSC_INT_DIR
if exist "%BSC_INT_DIR%/%NUL%" %BUILD_ROOT%\bin\delnode.exe /q "%BSC_INT_DIR%"
goto END

:DEFAULT_BSC_INT_DIR
if exist "%BUILD_ROOT%\Obj\SrcBrwsr/%NUL%" %BUILD_ROOT%\bin\delnode.exe /q "%BUILD_ROOT%\Obj\SrcBrwsr"
goto END

:PRINT_ERROR
echo ERROR: %ERROR_MESSAGE%
goto ALL_DONE

:END
set BUILD_CONFIG=
set NUL=
set DEL_BSC=

:ALL_DONE
