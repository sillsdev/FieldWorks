@if "%_echo%"=="" echo off
if not "%OS%"=="" setlocal

Rem get the latest source.
p4 set P4CLIENT=fwbuilder-fieldworks
rem p4 sync

rem Call the script that actually builds the source.
rem define norefreshsrc because of flakey problems with getting source with SS COM interface.
cscript.exe //D //X BuildFW.js %FWROOT% d:\FwBuilds  -dnorefreshsrc %1 %2 %3 %4 %5 %6 %7 %8 %9
goto exit

:exit
