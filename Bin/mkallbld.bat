rem This returns the environment error flag as an errorlevel for java script calls
rem We can't do this in mkall.bat because it exits rebuildall.bat
call c:\fwsrc\bin\mkall.bat %1 %2 %3 %4 %5 %6 %7 %8 %9
exit %FW_BUILD_ERROR%
