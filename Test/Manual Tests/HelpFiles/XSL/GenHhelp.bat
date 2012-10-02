@echo off
rem generate GenAllHelp.bat file from an xml modularbook using a style sheet
..\..\User\Tools\msxsl.exe ..\..\%3\%1\%2 GenHhelp.xsl -o ..\..\HelpFiles\%1\GenAllHelp.bat srcdir=..\..\%3\%1\ srcmbk=%2
