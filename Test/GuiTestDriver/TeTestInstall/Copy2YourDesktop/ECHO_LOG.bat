
REM ********** Echo Messages to the Screen and a Log File **********

SET LOG_FILE=%1
SET ECHO_MSG=%2

ECHO.
ECHO %ECHO_MSG%

IF %LOG_FILE% NEQ 0 (
ECHO. >> %LOG_FILE%
ECHO %ECHO_MSG% >> %LOG_FILE%
)