cd ..\bld
call ..\bin\nant\bin\nant IcuData-nodep
cd "%~p0"
wscript TestWixInstallerIntegrity.js yes