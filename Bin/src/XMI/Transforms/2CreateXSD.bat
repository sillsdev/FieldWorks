echo.
echo Creating FwDatabase.xsd (used for validating data for import - e.g. TestLangProj.xml)
echo.
echo Creating FwDatabaseA.xsd from xmi2cellar3.xml
echo.
%fwroot%\Bin\msxsl XMITempOutputs\xmi2cellar3.xml CreateFWDataXSDStage1.xsl -o XMITempOutputs\FwDatabaseA.xsd
echo.
echo Adding Prop element:refs to Rules17 and StyleRules15 elements
echo.
%fwroot%\Bin\msxsl XMITempOutputs\FwDatabaseA.xsd CreateFWDataXSDStage1b.xsl -o XMITempOutputs\FwDatabaseB.xsd
echo.
echo Sorting elements and attributes and outputting to FwDatabase.xsd
echo.
%fwroot%\Bin\msxsl XMITempOutputs\FwDatabaseB.xsd CreateFWDataXSDStage2.xsl -o XMITempOutputs\FwDatabase.xsd

REM del FwDatabase1.xsd
REM copy FwDatabase.xsd ..\..\..\test