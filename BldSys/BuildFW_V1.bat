@if "%_echo%"=="" echo off
if not "%OS%"=="" setlocal


Rem get the latest source.
p4 set P4CLIENT=fwbuilder2-FieldWorks_v1
p4 sync

rem Call the script that actually builds the source.
cscript.exe c:\BuildFW\buildfw.js %FWROOT% d:\FWBuilds -oFw_V1_Master -dnocreatedoc %1 %2 %3 %4 %5 %6 %7
goto exit


:exit
