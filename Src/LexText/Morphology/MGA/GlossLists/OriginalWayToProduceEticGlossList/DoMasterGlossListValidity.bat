rem @echo off
set FWROOT=C:\fw
cd %FWROOT%\Src\LexText\Morphology\MGA\GlossLists
%FWROOT%\bin\msxsl.exe MasterGlossListValidityConstraints.xml schematron-report.xsl -o temp.xsl
%FWROOT%\bin\msxsl.exe MasterGlossList.xml temp.xsl -o schematron-errors.html
%FWROOT%\bin\msxsl.exe MasterGlossList.xml verbid.xsl -o schematron-out.html
start "C:\Program Files\Internet Explorer\IEXPLORE.EXE" schematron-errors.html
