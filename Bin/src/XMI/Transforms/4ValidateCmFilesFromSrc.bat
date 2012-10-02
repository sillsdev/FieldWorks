echo off
rem Validate generated cm files.
set fwdir=%FWROOT%
cd %fwdir%\src\cellar\xml
rem -e      = expand (external) entities
rem -V      = validate against the DTD
rem -s      = work "silently": without output other than error reports
rem -f FILE = write error reports to FILE instead of stderr
echo.
echo Validating each .cm file...
echo.
REM echo ..\..\..\bin\rxp -Vs -f temp.err Cellar.cm
..\..\..\bin\rxp -Vs -f temp.err Cellar.cm

cd %fwdir%\src\featsys\xml\
REM echo ..\..\..\bin\rxp -Vs -f temp.err featsys.cm
..\..\..\bin\rxp -Vs -f temp.err featsys.cm

cd %fwdir%\src\notebk\xml\
REM echo ..\..\..\bin\rxp -Vs -f temp.err notebk.cm
..\..\..\bin\rxp -Vs -f temp.err notebk.cm

cd %fwdir%\src\ling\xml\
REM echo ..\..\..\bin\rxp -Vs -f temp.err ling.cm
..\..\..\bin\rxp -Vs -f temp.err ling.cm

cd %fwdir%\src\langproj\xml\
REM echo ..\..\..\bin\rxp -Vs -f temp.err langproj.cm
..\..\..\bin\rxp -Vs -f temp.err langproj.cm

cd %fwdir%\src\scripture\xml\
REM echo ..\..\..\bin\rxp -Vs -f temp.err scripture.cm
..\..\..\bin\rxp -Vs -f temp.err scripture.cm

cd %fwdir%\bin\src\xmi\transforms\