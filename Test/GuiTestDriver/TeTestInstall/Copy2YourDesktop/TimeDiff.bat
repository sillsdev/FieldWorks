::CLS
@ECHO OFF

::ECHO.
::ECHO *** Time Difference ***

REM Save begin time that is passed in as a variable
SET TM=%1
SET hour1=%TM:~0,2%
SET min1=%TM:~3,2%
SET sec1=%TM:~6,2%

::ECHO %TM% >> %2
::ECHO %hour1% >> %2
::ECHO %min1% >> %2
::ECHO %sec1% >> %2

REM Save end time
SET TM=%TIME%
SET hour2=%TM:~0,2%
SET min2=%TM:~3,2%
SET sec2=%TM:~6,2%

REM Calculate hour difference
SET hour_diff=0
IF %hour2% LSS %hour1% (
SET /a hour_diff=12-%hour1%
SET /a hour_diff=hour_diff + %hour2%)
IF %hour_diff% EQU 0 (
SET /a hour_diff=hour2 - hour1 >nul)

REM Calculate minute difference
SET min_diff=0
IF %min2% LSS %min1% (
SET /a min_diff=60-%min1%
SET /a min_diff=min_diff + %min2%
SET /a hour_diff=hour_diff - 1)
IF %min_diff% EQU 0 (
SET /a min_diff=min2 - min1 >nul)

REM Calculate second difference
SET sec_diff=0
IF %sec2% LSS %sec1% (
SET /a sec_diff=60-%sec1%
SET /a sec_diff=sec_diff + %sec2%
SET /a min_diff=min_diff - 1)
IF %sec_diff% EQU 0 (
SET /a sec_diff=sec2 - sec1 >nul)

IF %hour_diff% LSS 10 (SET hour_diff=0%hour_diff%)
IF %min_diff% LSS 10 (SET min_diff=0%min_diff%)
IF %sec_diff% LSS 10 (SET sec_diff=0%sec_diff%)

ECHO.
ECHO Begin Time: %hour1%:%min1%:%sec1%
ECHO End   Time: %hour2%:%min2%:%sec2%
ECHO.
ECHO Total Time: %hour_diff%:%min_diff%:%sec_diff%

ECHO. >> %2
ECHO Begin Time: %hour1%:%min1%:%sec1% >> %2
ECHO End   Time: %hour2%:%min2%:%sec2% >> %2
ECHO. >> %2
ECHO Total Time: %hour_diff%:%min_diff%:%sec_diff% >> %2

::ECHO.
::PAUSE