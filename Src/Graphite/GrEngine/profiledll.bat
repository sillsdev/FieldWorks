echo on
rem   TO DO EXECUTION TIMING PROFILE OF CODE IN A DLL
rem   * recompile DLL with Project/Settings/Link Enable Profiling option selected
rem   * run this batch file, arg1 = DLL name, arg2 = test driver name
rem        eg ProfileDll ScriptureObjects Tokenize_pc
rem   * the warning is expected and ok
rem   * results are in %1.txt file

rem  -- Set up:
prep /OM /FT %1.dll
copy %1._ll %1.dll
regsvr32 /s %1.dll

rem -- Run the program with profiling on:
profile /I %1 /O %1 %2.exe

rem -- Massage output:
prep /M %1

rem -- Generate output file:
plist %1 >%1.txt
%1.txt
