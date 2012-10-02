@if "%_echo%"=="" echo off

call %FWROOT%\bin\nant\bin\NAnt.exe -buildfile:%FWROOT%\bld\Fieldworks.build test %1 %2 %3 %4 %5 %6 %7 %8 %9 all
exit %errorlevel%
