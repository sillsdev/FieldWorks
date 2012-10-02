@echo off
if not exist XMITempOutputs md XMITempOutputs
echo.
echo Beginning to create .cm files from the FieldWorks.xml.zip (MagicDraw native format).
echo.

echo Unzipping the zipped MagicDraw xml file...
unzip95 -o ..\FieldWorks.xml.zip -d ..\
echo.
echo Creating XMI2cellar3.xml for further transformations...
%fwroot%\Bin\msxsl ..\FieldWorks.xml MagicToCellarStage1.xsl -o XMITempOutputs\xmi2cellar1.xml
echo.
%fwroot%\Bin\msxsl XMITempOutputs\xmi2cellar1.xml MagicToCellarStage2.xsl -o XMITempOutputs\xmi2cellar2.xml
%fwroot%\Bin\msxsl XMITempOutputs\xmi2cellar2.xml MagicToCellarStage3.xsl -o XMITempOutputs\xmi2cellar3.xml
echo.
echo Performing further transformations...