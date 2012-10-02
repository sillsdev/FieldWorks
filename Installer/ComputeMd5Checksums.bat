@rem Compute the MD5 checksums of the relevant files, and adjust the output for WinMD5.
..\bin\md5sums.exe -u Flex_M.cab TE_M.cab SetupFW.msi | ..\bin\sed.exe -e 's/\*/ /' > MD5SUM.md5
..\bin\md5sums.exe -u Flex_M.cab SetupFW_No_TE.msi | ..\bin\sed.exe -e 's/\*/ /' -e s/_No_TE// > MD5SUM_NO_TE.md5
..\bin\md5sums.exe -u SetupEC.msi | ..\bin\sed.exe -e 's/\*/ /' > MD5SUM_EC.md5