@if "%_echo%"=="" echo off
REM Usage: CollectUnit++Tests.cmd <module> <filename1> <filename2>...

setlocal

set MODULE=%1
shift

set BUILD_ROOT=%~dp0

REM we can't use a for loop here because shift doesn't modify %*

:LOOP
	if "%1"=="" goto END
	%BUILD_ROOT%\gawk -v module=%MODULE% -v SHORTFILENAME="%~nx1" -f %BUILD_ROOT%CollectUnit++Tests.awk %1
	shift
goto LOOP

:END
endlocal
