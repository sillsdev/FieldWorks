rem Make a manual test html file from an accil script.

rem name the script file without an extension.
set ScriptName=tlsDelete

c:\MSXSL.EXE %ScriptName%.xml MakeManual.xsl -o %ScriptName%.htm