@echo off
rem generate htm files from the xml using a style sheet
..\..\User\Tools\msxsl.exe %1\%2.xml ..\..\XSL\HelpFile.xsl -o %3 target=help
