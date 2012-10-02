echo off
echo.
echo Creating FwDatabase.dtd (used for validating data for import - e.g. TestLangProj.xml)
%fwroot%\Bin\msxsl XMITempOutputs\FwDatabase.xsd XSD2DTD.xsl -o %fwroot%\Test\FwDatabase.dtd