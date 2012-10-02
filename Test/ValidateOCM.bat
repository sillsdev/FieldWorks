@echo off
rem -e      = expand (external) entities
rem -V      = validate against the DTD
rem -s      = work "silently": without output other than error reports
rem -f FILE = write error reports to FILE instead of stderr
rem NOTE: for this to work, FWDatabase.dtd must be copied to the DistfFiles\Templates directory.
echo ..\bin\rxp -Vs -f temp.err NewLangProj-OCM.xml in Distfiles\Templates
..\bin\rxp -Vs -f temp.err "..\DistFiles\Templates\NewLangProj-OCM.xml"
more <temp.err
