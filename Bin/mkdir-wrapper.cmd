@echo off
REM mkdir-wrapper.cmd - Cross-platform compatible directory creation
REM Replaces Unix-style mkdir.exe with native Windows command
REM Usage: mkdir-wrapper.cmd -p "path"
REM The -p flag is accepted but ignored (md creates parent dirs if needed via loop)

setlocal enabledelayedexpansion

REM Skip the -p flag if present
set "ARG=%~1"
if "%ARG%"=="-p" (
    set "DIRPATH=%~2"
) else (
    set "DIRPATH=%~1"
)

REM Normalize the path (replace forward slashes with backslashes)
set "DIRPATH=%DIRPATH:/=\%"

REM Create the directory if it doesn't exist
if not exist "%DIRPATH%" (
    md "%DIRPATH%" 2>nul
    if errorlevel 1 (
        REM Try creating parent directories one by one
        set "CURRENT="
        for %%P in ("%DIRPATH:\=" "%") do (
            if "!CURRENT!"=="" (
                set "CURRENT=%%~P"
            ) else (
                set "CURRENT=!CURRENT!\%%~P"
            )
            if not exist "!CURRENT!" md "!CURRENT!" 2>nul
        )
    )
)

REM Verify the directory was created
if exist "%DIRPATH%" (
    exit /b 0
) else (
    echo Error: Failed to create directory: %DIRPATH% >&2
    exit /b 1
)
