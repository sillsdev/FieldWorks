REM Perform test on one file
@echo off
echo Testing %1
..\bin\debug\idlimp /x 0 /n SIL.Fieldworks.Test /c ..\IDLImporter\IDLImp.xml /u LanguageLib /u FwKernelLib /u Views %1
if ERRORLEVEL 1 goto ERROR
cmp %2 %2.ok > NUL
if ERRORLEVEL 1 goto ERROR
:OK
echo OK
goto END
:ERROR
echo FAILED
:END
