@if "%_echo%"=="" echo off

call %FWROOT%\bin\mksql-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
exit %errorlevel%
