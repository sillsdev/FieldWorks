call ..\..\..\_EnsureRoot.bat
call 1XMI2Cellar.bat
call 2CreateXSD.bat
call 2bCreateDTD.bat
call 3CreateCmFIles.bat
call 4ValidateCmFilesFromSrc.bat
call 5CreateClassList.bat
REM collect and copy the files from src\xml and type them here.
copy ..\..\..\..\src\cellar\xml\temp.err + ..\..\..\..\src\featsys\xml\temp.err + ..\..\..\..\src\langproj\xml\temp.err + ..\..\..\..\src\ling\xml\temp.err + ..\..\..\..\src\notebk\xml\temp.err + ..\..\..\..\src\scripture\xml\temp.err XMITempOutputs\temp.err
echo.
echo Here are the errors found in the .cm files:
echo.
echo ***********************************
type XMITempOutputs\temp.err
echo.
echo End of errors.
echo ***********************************
echo.
echo Fix errors if any.
echo.
echo Be sure to convert FwDatabase.xsd to FwDatabase.dtd.
echo Update TestLangProj.xml to reflect any model changes.
echo.
echo Check in changes when ready.
echo.
echo .cm file generation COMPLETE.
pause
