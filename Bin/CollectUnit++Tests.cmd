@if "%_echo%"=="" echo off
REM Usage: CollectUnit++Tests.cmd <module> <filename1> <filename2>...<outputFileName>

set BUILD_ROOT=%~dp0

%BUILD_ROOT%CollectCppUnitTests %*
