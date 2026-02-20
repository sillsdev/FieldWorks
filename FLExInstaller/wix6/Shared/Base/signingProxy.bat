@REM Subroutine to sign, if able
@REM Usage (requires %CERTPATH% and %CERTPASS% to be set ahead of time, if needed)
@REM   signingProxy.bat FileToSign
@REM If you set %FILESTOSIGNLATER% to a path the request to sign will append FileToSign
setlocal


@REM Check if the FILESTOSIGNLATER environment variable is set and not empty
if "%FILESTOSIGNLATER%"=="" (
    @REM Check if the "sign" command is available
    where sign >nul 2>nul
    if %errorlevel%==0 (
        sign %*
    ) else (
        @REM Check if signtool.exe is available
        where signtool.exe >nul 2>nul
        if %errorlevel%==0 (
            echo Signing with specified code signing certificate ...
            signtool.exe sign /fd sha256 /f %CERTPATH% /p %CERTPASS% /t http://timestamp.comodoca.com/authenticode %*
        ) else (
            @REM No signing tool found, exit with error
            echo Unable to sign %1; skipping. To build without signing set FILESTOSIGNLATER to a path
            echo and this script will capture the file paths which need signing
            exit /b 1
        )
    )
    @REM If signtool.exe successfully signed, this script will exit with code 0
    @REM otherwise the exit code will be the %errorlevel% that was set by sign or signtool.exe
) else (
    @REM Append the file name to FILESTOSIGNLATER
    echo %~1 >> "%FILESTOSIGNLATER%"
    exit /b 0
)
