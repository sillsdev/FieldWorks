::CLS
@ECHO OFF

dir %1|find " 0 File(s)" > NUL
if errorlevel 1 goto notempty
dir %1| find " 2 Dir(s)" > NUL
if errorlevel 1 goto notempty
::echo Directory empty
rd %1
goto end

:notempty
::echo Directory not empty

:end

:: DelEmptyDir c:\temp

:: "C:\Documents and Settings\JonesT\Desktop\DelEmptyDir" "C:\Documents and Settings\JonesT\Desktop\Tests Results\090528_101427\GuiTestResults\ImportOtherSF"

:: "C:\Documents and Settings\JonesT\Desktop\DelEmptyDir" "C:\Documents and Settings\JonesT\Desktop\Tests Results\090528_101427\GuiTestResults"