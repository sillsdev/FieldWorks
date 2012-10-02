@if "%_echo%"=="" echo off
if not "%OS%"=="" setlocal


Rem get the latest source.
p4 set P4CLIENT=fwbuilder-fieldworks
p4 sync

rem Add to the path so that the test harness can find all the icu dlls:
SET PATH=%PATH%;%FWROOT%\output\debug

rem Call the script that actually builds the source.
rem define norefreshsrc because of flakey problems with getting source with SS COM interface.
cscript.exe e:\BuildFW\buildfw.js %1 %2 %3 %4 %5 %6 %7 %8 %9
goto exit


:exit
