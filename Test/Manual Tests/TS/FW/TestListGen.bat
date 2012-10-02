rem generate htm files from the xml using a style sheet
if not "%1"=="" goto OtherMode
..\..\User\Tools\msxsl.exe FwBasics.xml ..\XSL\TCLlist.xsl -o TestList.htm
goto END

:OtherMode
..\..\User\Tools\msxsl.exe FwBasics.xml ..\XSL\TCLlist.xsl -o TestList.htm target=%1
:END
