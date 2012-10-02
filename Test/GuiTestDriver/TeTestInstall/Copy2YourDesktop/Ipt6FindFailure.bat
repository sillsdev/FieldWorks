:: Find and report the test case failures

FIND ", Failures: 0," %1 >NUL
IF ERRORLEVEL 1 (
	ECHO.
	ECHO Failure in %1
	ECHO. >> %2
	ECHO Failure in %1 >> %2
)