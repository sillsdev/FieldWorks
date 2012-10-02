@echo off
rem -e      = expand (external) entities
rem -V      = validate against the DTD
rem -s      = work "silently": without output other than error reports
rem -f FILE = write error reports to FILE instead of stderr
echo ..\bin\rxp -Vs -f temp.err Lela-Teli 2.xml
..\bin\rxp -Vs -f temp.err "Lela-Teli 2.xml"
more <temp.err
pause