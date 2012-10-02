@echo off
rem Transform each xml page into an html help topic page
..\User\Tools\msxsl.exe ..\User\TitlePage.xml ..\XSL\HelpFile.xsl -o TitlePage.htm target=help
..\User\Tools\msxsl.exe ..\User\Welcome.xml ..\XSL\HelpFile.xsl -o Welcome.htm target=help
..\User\Tools\msxsl.exe ..\User\nolink.xml ..\XSL\HelpFile.xsl -o nolink.htm target=help

rem a table of contents, index and project file are in ..\User\ and maintained by hand
copy ..\User\all.hhp .
copy ..\User\all.hhc .
copy ..\User\all.hhk .
copy ..\User\end.hh .
