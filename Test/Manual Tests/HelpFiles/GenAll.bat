@echo off

rem Generate the main html files from xml and create the main helpfile project file leaders.
call GenTopHelp.bat

rem Generate the batch file that generates the html files from xml and creates the helpfile project file segments.

echo Apps
cd Apps
call GenHThis.bat
echo Bugs
cd ..\Bugs
call GenHThis.bat
echo Building
cd ..\Building
call GenHThis.bat
echo DN
cd ..\DN
call GenHThis.bat
echo FW
cd ..\FW
call GenHThis.bat
echo Milestone
cd ..\Milestone
call GenHThis.bat
echo Source
cd ..\Source
call GenHThis.bat
echo Starting
cd ..\Starting
call GenHThis.bat
echo StdCtrls
cd ..\StdCtrls
call GenHThis.bat
echo TCL
cd ..\TCL
call GenHThis.bat
echo TLE
cd ..\TLE
call GenHThis.bat
echo Using
cd ..\Using
call GenHThis.bat
echo WorldPad
cd ..\WorldPad
call GenHThis.bat
echo Writing
cd ..\Writing
call GenHThis.bat
echo XML
cd ..\XML
call GenHThis.bat

rem Use the batch files to generate the html files from xml and create the helpfile project file segments.
echo Apps
cd ..\Apps
call GenAllHelp.bat
echo Bugs
cd ..\Bugs
call GenAllHelp.bat
echo Building
cd ..\Building
call GenAllHelp.bat
echo DN
cd ..\DN
call GenAllHelp.bat
echo FW
cd ..\FW
call GenAllHelp.bat
echo Milestone
cd ..\Milestone
call GenAllHelp.bat
echo Source
cd ..\Source
call GenAllHelp.bat
echo Starting
cd ..\Starting
call GenAllHelp.bat
echo StdCtrls
cd ..\StdCtrls
call GenAllHelp.bat
echo TCL
cd ..\TCL
call GenAllHelp.bat
echo TLE
cd ..\TLE
call GenAllHelp.bat
echo Using
cd ..\Using
call GenAllHelp.bat
echo WorldPad
cd ..\WorldPad
call GenAllHelp.bat
echo Writing
cd ..\Writing
call GenAllHelp.bat
echo XML
cd ..\XML
call GenAllHelp.bat

rem Generate the lists of tests for each library of tests.
echo StdCtrls
cd ..\..\TS\StdCtrls
call TestListGen.bat html
echo DN
cd ..\DN
call TestListGen.bat html
echo FW
cd ..\FW
call TestListGen.bat html
echo TLE
cd ..\TLE
call TestListGen.bat html
echo WorldPad
cd ..\WorldPad
call TestListGen.bat html

rem Generate the lists of possible bugs for each library of tests.
echo StdCtrls
cd ..\StdCtrls
call BugPatGen.bat
echo DN
cd ..\DN
call BugPatGen.bat
echo FW
cd ..\FW
call BugPatGen.bat
echo TLE
cd ..\TLE
call BugPatGen.bat
echo WorldPad
cd ..\WorldPad
call BugPatGen.bat

rem copy the library test lists to the helpfile structures so the helpfile compiler can see them.
cd ..\..\Helpfiles
copy ..\TS\StdCtrls\TestList.htm StdCtrls
copy ..\TS\DN\TestList.htm DN
copy ..\TS\FW\TestList.htm FW
copy ..\TS\TLE\TestList.htm TLE
copy ..\TS\WorldPad\TestList.htm WorldPad

rem copy the library bug lists to the helpfile structures so the helpfile compiler can see them.
copy ..\TS\StdCtrls\PatList.htm StdCtrls
copy ..\TS\DN\PatList.htm DN
copy ..\TS\FW\PatList.htm FW
copy ..\TS\TLE\PatList.htm TLE
copy ..\TS\WorldPad\PatList.htm WorldPad

rem sew together all the helpfile project file segments.
Rem Concatenate the Proj, TOC and Ind files to each other (intelligently) to get full Proj, TOC and Ind files.
echo Concat
call Concat.bat
Rem Run MS HTML Help Workshop and compile.
