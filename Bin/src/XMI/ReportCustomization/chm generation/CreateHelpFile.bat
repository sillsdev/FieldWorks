cls
REM This file depends on the user first generating the documentation from MagicDraw.
REM It uses new replacement class documentation generated from fw\bin\src\xmi\transforms\createReplaceHelp.bat
REM modifies some HTML to adhere to XHTML standards.
REM It then compiles the files into a chm help file.

call ..\..\..\..\_EnsureRoot.bat

REM========================================================
REM The following lines are remarked out until such time as
REM the old version of Saxon gets jettisoned.
REM echo.
REM echo Generating replacement help files for classes
REM echo.
REM cd %fwroot%\Bin\src\XMI\Transforms
REM call CreateReplaceHelp.bat
REM
REM pause
REM========================================================

pushd .
cd %fwroot%"\Bin\src\XMI\ReportCustomization\chm generation"
echo.
echo Cleaning up untidy html.
echo.
del tempLeft.html
del tempRight.html
del indexOrigLeft.html
del indexOrigRight.html

REM Clean up html > xhtml
tidy -asxhtml indexLeft.html > tempLeft.html
tidy -asxhtml indexRight.html > tempRight.html

REM Rename original file
ren indexLeft.html indexOrigLeft.html
ren indexRight.html indexOrigRight.html

REM Transform xhtml file to include new home page for help file.
REM========================================================
REM The following lines are remarked out until such time as
REM the old version of Saxon gets jettisoned.
REM copy /Y %FWROOT%\Bin\src\XMI\Transforms\XMITempOutputs\ReplaceHelp\*.* %FWROOT%"\Bin\src\XMI\ReportCustomization\chm generation"
REM========================================================
%FWROOT%\bin\msxsl tempLeft.html indexLeft.xsl -o indexLeft.html
%FWROOT%\bin\msxsl tempRight.html indexLeft.xsl -o indexRight.html

echo.
echo Compiling ModelDocumentation.chm file.
call "C:\Program Files\HTML Help Workshop\hhc.exe" ModelDocumentation.chm

copy /Y ModelDocumentation.chm %FWROOT%\Doc\Database
popd
pause