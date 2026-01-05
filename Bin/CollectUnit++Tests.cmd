@echo off
REM Usage: CollectUnit++Tests.cmd <module> <filename1> <filename2>...<outputfile>
REM
REM This script invokes CollectCppUnitTests.exe to generate Collection.cpp
REM which contains the test suite registration for unit++ tests.

set BUILD_ROOT=%~dp0..
"%~dp0CollectCppUnitTests.exe" %*
