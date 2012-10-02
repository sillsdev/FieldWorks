@if "%_echo%"=="" echo off

call %FWROOT%\bin\mkall-tst.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
exit %errorlevel%
