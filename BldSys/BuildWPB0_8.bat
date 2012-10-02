@if "%_echo%"=="" echo off
if not "%OS%"=="" setlocal


Rem get the latest source.
p4 set P4CLIENT=fwbuilder-wpbeta0_8
p4 sync

rem Call the script that actually builds the source.
rem define norefreshsrc because of flakey problems with getting source with SS COM interface.
cscript.exe e:\BuildFW\buildfw.js KEN UPDATED SRC DIR from CLIENT SPEC HERE d:\fw -oWPBeta0_8 -dnocreatedbs -dnocreatedoc -dnoautotest -dnoboundsbld
goto exit


:exit
