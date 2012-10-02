CLS
@ECHO OFF

SET STARTTM=%TIME%
SET STARTDT=%DATE%
REM - Debugging Messages
SET Popup_Test_Failures=0
SET Display_EndRun_Messages=0
SET Display_Skip_Messages=0

REM ********** High-Level Testing Options **********
:: 1 => YES  &  0 => NO
SET User_JonesT=1
SET User_Adam=0

SET Fwte_Branch_Bld=1
SET Fwte_Trunk_Bld=0
SET Fwte_Dallas_Bld=0

SET Install_New_TE_Only=0

SET Install_and_Run_All_Tests=1

SET Install_New_TE=0
SET Test_OtherSF=0
SET Test_OtherSF_BT=0
SET Test_PT5=0
SET Test_PT5_BT=0
SET Test_PT6=0
SET Test_PT6_BT=0
SET Test_Roundtrip=0
SET Test_PT6_Restore=0

REM **********  Initialize Variables  **********
SET CreateProjSF=0
SET Run_OtherSF_Test_Cases=0
SET Run_OtherSF_BT_Test_Cases=0
SET CreateProj5=0
SET Run_PT5_Test_Cases=0
SET Run_PT5_BT_Test_Cases=0
SET CreateProj6=0
SET Run_PT6_Test_Cases=0
SET Run_PT6_BT_Test_Cases=0
SET Run_Roundtrip_Sena3_Export_Import_Compare=0
SET Run_PT6_Create_Restore_Project=0
SET Run_PT6_Restore_Opt_Replace_Ver=0
SET Run_PT6_Restore_Opt_Seperate_Database=0

REM ********** Low-Level Testing Options **********

REM =====  Test_OtherSF  =====
IF %Test_OtherSF% EQU 0 (GOTO EndTestOtherSF)
SET CreateProjSF=0
SET Run_OtherSF_Test_Cases=1
SET OtherSF_Test_Cases=1
:EndTestOtherSF


REM =====  Test_OtherSF_BT  =====
IF %Test_OtherSF_BT% EQU 0 (GOTO EndTestOtherSFBT)
SET Run_OtherSF_BT_Test_Cases=1
SET OtherSF_BT_Test_Cases=1
:EndTestOtherSFBT


REM =====  Test_PT5  =====
IF %Test_PT5% EQU 0 (GOTO EndTestPT5)
SET CreateProj5=0
SET Run_PT5_Test_Cases=1
::SET PT5_Test_Cases=4,9
SET PT5_Test_Cases=1
:EndTestPT5


REM =====  Test_PT5_BT  =====
IF %Test_PT5_BT% EQU 0 (GOTO EndTestPT5BT)
SET Run_PT5_BT_Test_Cases=1
SET PT5_BT_Test_Cases=1
:EndTestPT5BT


REM =====  Test_PT6  =====
IF %Test_PT6% EQU 0 (GOTO EndTestPT6)
SET CreateProj6=0
SET Run_PT6_Test_Cases=1
::SET PT6_Test_Cases=2,10,11
SET PT6_Test_Cases=2
::SET PT6_Exp_Failures=10
:EndTestPT6


REM =====  Test_PT6_BT  =====
IF %Test_PT6_BT% EQU 0 (GOTO EndTestPT6BT)
SET Run_PT6_BT_Test_Cases=1
SET PT6_BT_Test_Cases=1
:EndTestPT6BT


REM =====  Test Roundtrip Export Import Compare  =====
IF %Test_Roundtrip% EQU 0 (GOTO EndTestRoundtrip)
SET Run_Roundtrip_Sena3_Export_Import_Compare=1
:EndTestRoundtrip


REM =====  Test Project Backing Up and Restoring Using the UI  =====
IF %Test_PT6_Restore% EQU 0 (GOTO EndTestPT6Restore)
SET Run_PT6_Create_Restore_Project=1
SET Run_PT6_Restore_Opt_Replace_Ver=1
SET Run_PT6_Restore_Opt_Seperate_Database=1
:EndTestPT6Restore


REM ********** Run All Tests **********
IF %Install_and_Run_All_Tests% EQU 0 (GOTO EndInstallAndRunAllTests)

SET Install_New_TE=1

SET CreateProjSF=1
SET Run_OtherSF_Test_Cases=1
SET OtherSF_Test_Cases=1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42

SET Run_OtherSF_BT_Test_Cases=0
SET OtherSF_BT_Test_Cases=1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84

SET CreateProj5=1
SET Run_PT5_Test_Cases=1
SET PT5_Test_Cases=1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42

SET Run_PT5_BT_Test_Cases=1
SET PT5_BT_Test_Cases=1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84

SET CreateProj6=1
SET Run_PT6_Test_Cases=1
SET PT6_Test_Cases=1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42

SET Run_PT6_BT_Test_Cases=1
SET PT6_BT_Test_Cases=1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84

SET Run_Roundtrip_Sena3_Export_Import_Compare=1

SET Run_PT6_Create_Restore_Project=0
SET Run_PT6_Restore_Opt_Replace_Ver=0
SET Run_PT6_Restore_Opt_Seperate_Database=0
:EndInstallAndRunAllTests


REM ********** Install New TE Only **********
IF %Install_New_TE_Only% EQU 0 (GOTO EndInstallNewTEOnly)

SET Install_New_TE=1
SET CreateProjSF=0
SET Run_OtherSF_Test_Cases=0
SET Run_OtherSF_BT_Test_Cases=0
SET CreateProj5=0
SET Run_PT5_Test_Cases=0
SET Run_PT5_BT_Test_Cases=0
SET CreateProj6=0
SET Run_PT6_Test_Cases=0
SET Run_PT6_BT_Test_Cases=0
SET Run_Roundtrip_Sena3_Export_Import_Compare=0
SET Run_PT6_Create_Restore_Project=0
SET Run_PT6_Restore_Opt_Replace_Ver=0
SET Run_PT6_Restore_Opt_Seperate_Database=0

:EndInstallNewTEOnly

REM ********** END Testing Options **********

REM - Test Case Failure Counts
set /a OtherSF_Failures = 0
set /a OtherSF_BT_Failures = 0
set /a PT5_Failures = 0
set /a PT5_BT_Failures = 0
set /a PT6_Failures = 0
set /a PT6_BT_Failures = 0
set /a Roundtrip_Failures = 0

set /a OtherSF_Expected_Failures = 0
set /a OtherSF_BT_Expected_Failures = 0
set /a PT5_Expected_Failures = 0
set /a PT5_BT_Expected_Failures = 0
::set /a PT6_Exp_Failures = 0
::set /a PT6_BT_Expected_Failures = 0
set /a Roundtrip_Expected_Failures = 0

IF %User_JonesT% EQU 1 (set user=JonesT)
IF %User_Adam% EQU 1 (set user=Adam)

REM ===============  CREATE TEST RESULTS DIR  ===================

REM Create sub directory called \yymmdd_hhmmss where yymmdd_hhmmss is a date_time stamp like 030902_134200
set hh=%time:~0,2%

REM Since there is no leading zero for times before 10 am, have to put in a zero when this is run before 10 am.
if "%time:~0,1%"==" " set hh=0%hh:~1,1%

set yymmdd_hhmmss=%date:~12,2%%date:~4,2%%date:~7,2%_%hh%%time:~3,2%%time:~6,2%

md "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

REM ===============================================================

REM Change to the specified test directory
CD "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

SET TEST_PROCESSING_MSGS="C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\TEST_CASE_PROCESSING.txt"

::==============   REPORT HEADINGS   ==================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "*** TE Automated Tests ***"

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "User is %user%"

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Created '%yymmdd_hhmmss%' Test Results directory"

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "--------------------------------------"

REM ====================  INSTALL NEW TE  =====================

IF %Install_New_TE% EQU 0 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip Install New TE"
	GOTO EndInstallNewTE)

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Uninstall FW"

msiexec /x c:\setupfw.msi /qb

IF %User_JonesT% EQU 0 (
	GOTO EndUserJonesT)

IF %Fwte_Branch_Bld% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Copy the latest TE BRANCH build"
	copy \\swd-build\BuildArchive\LatestFwBuild\SetupFW.msi c:\)

IF %Fwte_Trunk_Bld% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Copy the latest TRUNK build"
	copy \\jar-file\SilLangSoft\NightlyBuild\FieldWorks\SetupFW.msi c:\)

IF %Fwte_Dallas_Bld% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Copy the latest DALLAS build"
	copy "\\jar-file\SilLangSoft\Dallas Branch\FieldWorks\SetupFw.msi" c:\)

:EndUserJonesT

IF %User_Adam% EQU 1 (
	copy y:\SetupFW.msi c:\)

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Delete Database Test Data"

del "C:\Documents and Settings\All Users\Application Data\SIL\FieldWorks\Data\MALAY PARATEXT 5 IMPORT TEST*.*"

del "C:\Documents and Settings\All Users\Application Data\SIL\FieldWorks\Data\MALAY PARATEXT 6 IMPORT TEST*.*"

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Install FW"

msiexec /i c:\setupfw.msi /qb

path=%path%;C:\Program Files\Common Files\SIL

:EndInstallNewTE

REM ==============================================================================
REM #########  CONVERT OtherSF TEST CASE FILE INTO XML  ##########

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "--------------------------------------"

cd "C:\fw\Test\GuiTestDriver\TeModel"

REM Convert the spreadsheet comma delineated file of test cases to Xml
CSV2XML ImportOtherSF.CSV

REM Apply a style sheet to the xml file to produce a more readable test case xml file
"C:\Documents and Settings\%user%\Desktop\MSXSL.EXE" XmlFromCsv.xml XmlFromCsv.xsl -o XmlFromCsv2.xml

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Created the ImportOtherSF XML test cases file"

REM =============  CREATE OtherSF BASE PROJECT  =================

IF %CreateProjSF% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip OtherSF Project Creation")
	GOTO EndCreateProjSF)

SET ECHO_MSG="Creating the Import OtherSF Test base project"
CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% %ECHO_MSG%

"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Create OtherSF Project" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\ImportOtherSF_CreateBaseProj.log"

REM ==================

SET SF_Proj_TEST_FAILURE="SF_Proj_TEST_FAILURE.txt"

REM Change to the specified test directory
cd "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

REM Loop thru the test results directory passing all the *.log file names to Ipt6FindFailure.bat
FOR /f %%a IN ('dir /b ImportOtherSF_CreateBaseProj.log') do call "C:\Documents and Settings\%user%\Desktop\Ipt6FindFailure.bat" %%a %SF_Proj_TEST_FAILURE%

REM  Skip to the next test when creating the base project fails
IF EXIST %SF_Proj_TEST_FAILURE% GOTO CreatePT5BasePROJECT

REM ======================   BACKUP PROJECT   ===========================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Backup project MALVI OTHERSF IMPORT TEST"

db backup "MALVI OTHERSF IMPORT TEST" "MALVI OTHERSF IMPORT TEST.bak"

:EndCreateProjSF
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndCreateOtherSFProject"
)

REM ===============================================================

IF %Run_OtherSF_Test_Cases% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip OtherSF Test Cases")
	GOTO EndRunOtherSFTestCases)

md "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\OtherSF"

REM ===============================================================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Looping through the Other SF test cases"

SET OtherSF_TEST_FAILURES="C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\OtherSF_TEST_FAILURES.txt"

REM Do Test Cases Loop
for %%t in (%OtherSF_Test_Cases%) do (

	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Restore project MALVI OTHERSF IMPORT TEST"
	db restore "MALVI OTHERSF IMPORT TEST" "MALVI OTHERSF IMPORT TEST.bak"

REM If Test Case Number is Less Than 10
	IF %%t LSS 10 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Test Case %%t"

		"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Test Case %%t" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\OtherSF\TestCase0%%t.log"

		FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\OtherSF\TestCase0%%t.log" >NUL
		IF ERRORLEVEL 1 (
			CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
			ECHO. >> %OtherSF_TEST_FAILURES%
			ECHO Test Case  %%t >> %OtherSF_TEST_FAILURES%
			set /a OtherSF_Failures = OtherSF_Failures + 1
		)
	)

REM If Test Case Number is Greater Than or Equal to 10
	IF %%t GEQ 10 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Test Case %%t"

		"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Test Case %%t" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\OtherSF\TestCase%%t.log"

		FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\OtherSF\TestCase%%t.log" >NUL
		IF ERRORLEVEL 1 (
			CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
			ECHO. >> %OtherSF_TEST_FAILURES%
			ECHO Test Case %%t >> %OtherSF_TEST_FAILURES%
			set /a OtherSF_Failures = OtherSF_Failures + 1
		)
	)
)

REM ===============================================================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Finished all OtherSF test cases"

REM Delete the test failures file if it has no errors in it
::IF EXIST %OtherSF_TEST_FAILURES% for /F %%A in (%OtherSF_TEST_FAILURES%) do If %%~zA equ 0 del %OtherSF_TEST_FAILURES%

::IF NOT EXIST %OtherSF_TEST_FAILURES% (CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "SUCCESS!"
)

:EndRunOtherSFTestCases
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndRunOtherSFTestCases")

REM =========================================================================
REM ######## CONVERT OtherSF BT TEST CASE FILE INTO XML  #########

IF %Run_OtherSF_BT_Test_Cases% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip OtherSF BT Test Cases")
	GOTO EndRunOtherSFBTTestCases)

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "--------------------------------------"

cd "C:\fw\Test\GuiTestDriver\TeModel"

REM Convert the spreadsheet comma delineated file of test cases to Xml
CSV2XML ImportOtherSF_SepBT.CSV

REM Apply a style sheet to the xml file to produce a more readable test case xml file
"C:\Documents and Settings\%user%\Desktop\MSXSL.EXE" XmlFromCsv.xml XmlFromCsv.xsl -o XmlFromCsv2.xml

::CD "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"
CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Created the OtherSF BT XML test cases file"

REM ===============================================================

REM Change to the specified test directory
cd "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

md "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\OtherSF_BT"

REM ===============================================================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Looping through the OtherSF BT test cases"

SET OtherSF_BT_TEST_FAILURES="C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\OtherSF_BT_TEST_FAILURES.txt"

REM Do Test Cases Loop
for %%t in (%OtherSF_BT_Test_Cases%) do (

	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Restore project MALVI OTHERSF IMPORT TEST"
	db restore "MALVI OTHERSF IMPORT TEST" "MALVI OTHERSF IMPORT TEST.bak"

REM If Test Case Number is Less Than 10
	IF %%t LSS 10 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Test Case %%t"

		"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Test Case %%t" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\OtherSF_BT\TestCase0%%t.log"

		FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\OtherSF_BT\TestCase0%%t.log" >NUL
		IF ERRORLEVEL 1 (
			CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
			ECHO. >> %OtherSF_BT_TEST_FAILURES%
			ECHO Test Case  %%t >> %OtherSF_BT_TEST_FAILURES%
			set /a OtherSF_BT_Failures = OtherSF_BT_Failures + 1
		)
	)

REM If Test Case Number is Greater Than or Equal to 10
	IF %%t GEQ 10 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Test Case %%t"

		"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Test Case %%t" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\OtherSF_BT\TestCase%%t.log"

		FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\OtherSF_BT\TestCase%%t.log" >NUL
		IF ERRORLEVEL 1 (
			CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
			ECHO. >> %OtherSF_BT_TEST_FAILURES%
			ECHO Test Case %%t >> %OtherSF_BT_TEST_FAILURES%
			set /a OtherSF_BT_Failures = OtherSF_BT_Failures + 1
		)
	)
)

REM ===============================================================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Finished all OtherSF_BT test cases"

REM Delete the test failures file if it has no errors in it
::IF EXIST %OtherSF_BT_TEST_FAILURES% for /F %%A in (%OtherSF_BT_TEST_FAILURES%) do If %%~zA equ 0 del %OtherSF_BT_TEST_FAILURES%

::IF NOT EXIST %OtherSF_BT_TEST_FAILURES% (CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "SUCCESS!"
)

:EndRunOtherSFBTTestCases
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndRunOtherSFBTTestCases")

REM =========================================================================
REM ######## CONVERT PT5 TEST CASE FILE INTO XML  ###########
:CreatePT5BasePROJECT

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "--------------------------------------"

cd "C:\fw\Test\GuiTestDriver\TeModel"

REM Convert the spreadsheet comma delineated file of test cases to Xml
CSV2XML IMPORTPARATEXT5.CSV

REM Apply a style sheet to the xml file to produce a more readable test case xml file
"C:\Documents and Settings\%user%\Desktop\MSXSL.EXE" XmlFromCsv.xml XmlFromCsv.xsl -o XmlFromCsv2.xml

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Created the PT5 XML test cases file"

REM =============  CREATE PT5 BASE PROJECT  =================

IF %CreateProj5% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip PT5 Project Creation")
	GOTO EndCreatePT5Project)

SET ECHO_MSG="Creating the Malay Paratext 5 Import Test base project"
CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% %ECHO_MSG%

"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Create PT Project" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\ImportPT5_CreateBaseProject.log"

REM ==================

SET PT5_Proj_TEST_FAILURE="PT5_Proj_TEST_FAILURE.txt"

REM Change to the specified test directory
cd "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

REM Loop thru the test results directory passing all the *.log file names to Ipt6FindFailure.bat
FOR /f %%a IN ('dir /b ImportPT5_CreateBaseProject.log') do call "C:\Documents and Settings\%user%\Desktop\Ipt6FindFailure.bat" %%a %PT5_Proj_TEST_FAILURE%

REM  Skip to the next test when creating the base project fails
IF EXIST %PT5_Proj_TEST_FAILURE% GOTO CreatePT6BasePROJECT

REM ===   BACKUP PROJECT   ===

::SET ECHO_MSG="Backup project MALAY PARATEXT 5 IMPORT TEST"
CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Backup project MALAY PARATEXT 5 IMPORT TEST"

db backup "MALAY PARATEXT 5 IMPORT TEST" "MALAY PARATEXT 5 IMPORT TEST.bak"

:EndCreatePT5Project
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndCreatePT5Project"
)

REM ===============================================================

::REM Change to the specified test directory
::cd "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

IF %Run_PT5_Test_Cases% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip PT5 Test Cases")
	GOTO EndRunPT5TestCases)

md "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt5"

REM ===============================================================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Looping through the Import Paratext 5 test cases"

SET Ipt5_TEST_FAILURES="C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt5_TEST_FAILURES.txt"

REM Do Test Cases Loop
for %%t in (%PT5_Test_Cases%) do (

	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Restore project MALAY PARATEXT 5 IMPORT TEST"
	db restore "MALAY PARATEXT 5 IMPORT TEST" "MALAY PARATEXT 5 IMPORT TEST.bak"

REM If Test Case Number is Less Than 10
	IF %%t LSS 10 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Test Case %%t"

		"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Test Case %%t" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt5\TestCase0%%t.log"

		FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt5\TestCase0%%t.log" >NUL
		IF ERRORLEVEL 1 (
			CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
			ECHO. >> %Ipt5_TEST_FAILURES%
			ECHO Test Case  %%t >> %Ipt5_TEST_FAILURES%
			set /a PT5_Failures = PT5_Failures + 1
		)
	)

REM If Test Case Number is Greater Than or Equal to 10
	IF %%t GEQ 10 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Test Case %%t"

		"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Test Case %%t" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt5\TestCase%%t.log"

		FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt5\TestCase%%t.log" >NUL
		IF ERRORLEVEL 1 (
			CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
			ECHO. >> %Ipt5_TEST_FAILURES%
			ECHO Test Case %%t >> %Ipt5_TEST_FAILURES%
			set /a PT5_Failures = PT5_Failures + 1
		)
	)
)

REM ===============================================================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Finished all Ipt5 test cases"

REM Delete the test failures file if it has no errors in it
::IF EXIST %Ipt5_TEST_FAILURES% for /F %%A in (%Ipt5_TEST_FAILURES%) do If %%~zA equ 0 del %Ipt5_TEST_FAILURES%

::IF NOT EXIST %Ipt5_TEST_FAILURES% (CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "SUCCESS!"
)

:EndRunPT5TestCases
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndRunPT5TestCases")

REM =========================================================================
REM ######## CONVERT PT5 BT TEST CASE FILE INTO XML  #########

IF %Run_PT5_BT_Test_Cases% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip PT5 BT Test Cases")
	GOTO EndRunPT5BTTestCases)

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "--------------------------------------"

cd "C:\fw\Test\GuiTestDriver\TeModel"

REM Convert the spreadsheet comma delineated file of test cases to Xml
CSV2XML IMPORTPARATEXT5_BT.CSV

REM Apply a style sheet to the xml file to produce a more readable test case xml file
"C:\Documents and Settings\%user%\Desktop\MSXSL.EXE" XmlFromCsv.xml XmlFromCsv.xsl -o XmlFromCsv2.xml

::CD "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"
CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Created the PT5 BT XML test cases file"

REM ===============================================================

REM Change to the specified test directory
cd "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

md "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt5BT"

REM ===============================================================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Looping through the Import Paratext 5 BT test cases"

SET Ipt5BT_TEST_FAILURES="C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt5BT_TEST_FAILURES.txt"

REM Do Test Cases Loop
for %%t in (%PT5_BT_Test_Cases%) do (

	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Restore project MALAY PARATEXT 5 IMPORT TEST for BT"
	db restore "MALAY PARATEXT 5 IMPORT TEST" "MALAY PARATEXT 5 IMPORT TEST.bak"

	REM If Test Case Number is Less Than 10
	IF %%t LSS 10 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Test Case %%t"

		"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Test Case %%t" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt5BT\TestCase0%%t.log"

		FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt5BT\TestCase0%%t.log" >NUL
		IF ERRORLEVEL 1 (
			CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
			ECHO. >> %Ipt5BT_TEST_FAILURES%
			ECHO Test Case  %%t >> %Ipt5BT_TEST_FAILURES%
			set /a PT5_BT_Failures = PT5_BT_Failures + 1
		)
	)

REM If Test Case Number is Greater Than or Equal to 10
	IF %%t GEQ 10 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Test Case %%t"

		"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Test Case %%t" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt5BT\TestCase%%t.log"

		FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt5BT\TestCase%%t.log" >NUL
		IF ERRORLEVEL 1 (
			CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
			ECHO. >> %Ipt5BT_TEST_FAILURES%
			ECHO Test Case %%t >> %Ipt5BT_TEST_FAILURES%
			set /a PT5_BT_Failures = PT5_BT_Failures + 1
		)
	)
)

REM ===============================================================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Finished all Ipt5BT test cases"

REM Delete the test failures file if it has no errors in it
::IF EXIST %Ipt5BT_TEST_FAILURES% for /F %%A in (%Ipt5BT_TEST_FAILURES%) do If %%~zA equ 0 del %Ipt5BT_TEST_FAILURES%

::IF NOT EXIST %Ipt5BT_TEST_FAILURES% (CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "SUCCESS!"
)

:EndRunPT5BTTestCases
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndRunPT5BTTestCases"
)

REM =========================================================================
REM ######### CONVERT PT6 TEST CASE FILE INTO XML  ##########
:CreatePT6BasePROJECT

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "--------------------------------------"

cd "C:\fw\Test\GuiTestDriver\TeModel"

REM Convert the spreadsheet comma delineated file of test cases to Xml
CSV2XML IMPORTPARATEXT6.CSV

REM Apply a style sheet to the xml file to produce a more readable test case xml file
"C:\Documents and Settings\%user%\Desktop\MSXSL.EXE" XmlFromCsv.xml XmlFromCsv.xsl -o XmlFromCsv2.xml

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Created the PT6 XML test cases file"

REM =============  CREATE PT6 BASE PROJECT  =================

IF %CreateProj6% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip PT6 Project Creation")
	GOTO EndCreatePT6Project)

::SET ECHO_MSG="Creating the Malay Paratext 6 Import Test base project"
CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Creating the Malay Paratext 6 Import Test base project"

"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Create PT Project" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\ImportPT6_CreateBaseProject.log"

REM ==================

SET PT6_Proj_TEST_FAILURE="PT6_Proj_TEST_FAILURE.txt"

REM Change to the specified test directory
cd "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

REM Loop thru the test results directory passing all the *.log file names to Ipt6FindFailure.bat
FOR /f %%a IN ('dir /b ImportPT6_CreateBaseProject.log') do call "C:\Documents and Settings\%user%\Desktop\Ipt6FindFailure.bat" %%a %PT6_Proj_TEST_FAILURE%

REM  Skip to the next test when creating the base project fails
IF EXIST %PT6_Proj_TEST_FAILURE% GOTO End

REM ========================  BACKUP PROJECT   ============================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Backup project MALAY PARATEXT 6 IMPORT TEST"

db backup "MALAY PARATEXT 6 IMPORT TEST" "MALAY PARATEXT 6 IMPORT TEST.bak"

:EndCreatePT6Project
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndCreatePT6Project"
)

REM ===============================================================

::REM Change to the specified test directory
::cd "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

IF %Run_PT6_Test_Cases% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip PT6 Test Cases")
	GOTO EndRunPT6TestCases)

md "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt6"

REM ===============================================================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Looping through the Import Paratext 6 test cases"

SET Ipt6_TEST_FAILURES="C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt6_TEST_FAILURES.txt"

REM Do Test Cases Loop
for %%t in (%PT6_Test_Cases%) do (

	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Restore project MALAY PARATEXT 6 IMPORT TEST"
	db restore "MALAY PARATEXT 6 IMPORT TEST" "MALAY PARATEXT 6 IMPORT TEST.bak"

	REM - If Test Case Number is Less Than 10
	IF %%t LSS 10 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Test Case %%t"

		"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Test Case %%t" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt6\TestCase0%%t.log"

		FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt6\TestCase0%%t.log" >NUL
		IF ERRORLEVEL 1 (
			CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
			ECHO. >> %Ipt6_TEST_FAILURES%
			ECHO Test Case  %%t >> %Ipt6_TEST_FAILURES%
			set /a PT6_Failures = PT6_Failures + 1
		)
	)

	REM If Test Case Number is Greater Than or Equal to 10
	IF %%t GEQ 10 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Test Case %%t"

		"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Test Case %%t" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt6\TestCase%%t.log"

		FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt6\TestCase%%t.log" >NUL
		IF ERRORLEVEL 1 (
			CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
			ECHO. >> %Ipt6_TEST_FAILURES%
			ECHO Test Case %%t >> %Ipt6_TEST_FAILURES%
			set /a PT6_Failures = PT6_Failures + 1
		)
	)
)

REM ===============================================================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Finished all Ipt6 test cases"

REM Delete the test failures file if it has no errors in it
::IF EXIST %Ipt6_TEST_FAILURES% for /F %%A in (%Ipt6_TEST_FAILURES%) do If %%~zA equ 0 del %Ipt6_TEST_FAILURES%

::IF NOT EXIST %Ipt6_TEST_FAILURES% (CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "SUCCESS!"
)

:EndRunPT6TestCases
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndRunPT6TestCases"
)

REM =========================================================================
REM ####### CONVERT PT6 BT TEST CASE FILE INTO XML  ##########

IF %Run_PT6_BT_Test_Cases% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip PT6 BT Test Cases")
	GOTO EndRunPT6BTTestCases)

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "--------------------------------------"

cd "C:\fw\Test\GuiTestDriver\TeModel"

REM Convert the spreadsheet comma delineated file of test cases to Xml
CSV2XML IMPORTPARATEXT6_BT.CSV

REM Apply a style sheet to the xml file to produce a more readable test case xml file
"C:\Documents and Settings\%user%\Desktop\MSXSL.EXE" XmlFromCsv.xml XmlFromCsv.xsl -o XmlFromCsv2.xml

::CD "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"
CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Created the PT6 BT XML test cases file"

REM ===============================================================

REM Change to the specified test directory
cd "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

md "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt6BT"

REM ===============================================================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Looping through the Import Paratext 6 BT test cases"

SET Ipt6BT_TEST_FAILURES="C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt6BT_TEST_FAILURES.txt"

REM Do Test Cases Loop
for %%t in (%PT6_BT_Test_Cases%) do (

	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Restore project MALAY PARATEXT 6 IMPORT TEST for BT"
	db restore "MALAY PARATEXT 6 IMPORT TEST" "MALAY PARATEXT 6 IMPORT TEST.bak"

	REM - If Test Case Number is Less Than 10
	IF %%t LSS 10 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Test Case %%t"

		"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Test Case %%t" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt6BT\TestCase0%%t.log"

		FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt6BT\TestCase0%%t.log" >NUL
		IF ERRORLEVEL 1 (
			CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
			ECHO. >> %Ipt6BT_TEST_FAILURES%
			ECHO Test Case  %%t >> %Ipt6BT_TEST_FAILURES%
			set /a PT6_BT_Failures = PT6_BT_Failures + 1
		)
	)

	REM - If Test Case Number is Greater Than or Equal to 10
	IF %%t GEQ 10 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Test Case %%t"

		"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Test Case %%t" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt6BT\TestCase%%t.log"

		FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Ipt6BT\TestCase%%t.log" >NUL
		IF ERRORLEVEL 1 (
			CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
			ECHO. >> %Ipt6BT_TEST_FAILURES%
			ECHO Test Case %%t >> %Ipt6BT_TEST_FAILURES%
			set /a PT6_BT_Failures = PT6_BT_Failures + 1
		)
	)
)

REM ===============================================================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Finished all Ipt6BT test cases"

REM Delete the test failures file if it has no errors in it
::IF EXIST %Ipt6BT_TEST_FAILURES% for /F %%A in (%Ipt6BT_TEST_FAILURES%) do If %%~zA equ 0 del %Ipt6BT_TEST_FAILURES%

::IF NOT EXIST %Ipt6BT_TEST_FAILURES% (CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "SUCCESS!"
)

:EndRunPT6BTTestCases
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndRunPT6BTTestCases"
)
::ECHO.
::PAUSE

REM =========================================================================
REM ########### CREATE RESTORE BASE PROJECT  #############

IF %Run_PT6_Create_Restore_Project% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip PT6 Create Restore Project")
	GOTO EndRunPT6CreateRestoreProject)

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "--------------------------------------"

::del "C:\Documents and Settings\All Users\Application Data\SIL\FieldWorks\Data\MALAY PARATEXT 6 IMPORT TEST*.*"

del "C:\GuiTestResults\BackupRestoreUsingUI\*.zip"

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Creating the Malay Paratext 6 Restore Test base project"

"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Create Pt Restore Project" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\RestorePT6_CreateBaseProject.log"

REM ==================

SET Restore_Proj_TEST_FAILURE="Restore_Proj_TEST_FAILURE.txt"

REM Change to the specified test directory
cd "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

REM Loop thru the test results directory passing all the *.log file names to Ipt6FindFailure.bat
FOR /f %%a IN ('dir /b RestorePT6_CreateBaseProject.log') do call "C:\Documents and Settings\%user%\Desktop\Ipt6FindFailure.bat" %%a %Restore_Proj_TEST_FAILURE%

REM  Skip to the next test when creating the base project fails
IF EXIST %Restore_Proj_TEST_FAILURE% GOTO PopupTestFailures

REM ========================  BACKUP PROJECT   ============================

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Backup project MALAY PARATEXT 6 RESTORE TEST"

db backup "MALAY PARATEXT 6 RESTORE TEST" "MALAY PARATEXT 6 RESTORE TEST.bak"

:EndRunPT6CreateRestoreProject
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndRunPT6CreateRestoreProject")

REM ==================  RESTORE OPTIONS REPLACE VERSION TEST  ====================

IF %Run_PT6_Restore_Opt_Replace_Ver% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip PT6 Restore Options Replace Version")
	GOTO EndRunPT6RestoreOptReplaceVer)

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Restore project MALAY PARATEXT 6 RESTORE TEST"

db restore "MALAY PARATEXT 6 RESTORE TEST" "MALAY PARATEXT 6 RESTORE TEST.bak"

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Restore Options Replace Version"

"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Restore Options Replace Version" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Pt6RestoreOptReplaceVer.log"

REM Change to the specified test directory
CD "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

SET Restore_Opt_Replace_Ver_TEST_FAILURE="Restore_Opt_Replace_Ver_TEST_FAILURE.txt"

FOR /f %%a IN ('dir /b Restore_Opt_Replace_Ver_TEST_FAILURE.log') do call "C:\Documents and Settings\%user%\Desktop\Ipt6FindFailure.bat" %%a %Restore_Opt_Replace_Ver_TEST_FAILURE%

:EndRunPT6RestoreOptReplaceVer
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndRunPT6RestoreOptReplaceVer")

REM ==================  RESTORE OPTIONS SEPERATE DATABASE TEST  ====================

IF %Run_PT6_Restore_Opt_Seperate_Database% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip PT6 Restore Options Seperate Database")
	GOTO EndRunPT6RestoreOptSeperateDb)

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Restore project MALAY PARATEXT 6 RESTORE TEST"

db restore "MALAY PARATEXT 6 RESTORE TEST" "MALAY PARATEXT 6 RESTORE TEST.bak"

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Restore Options Seperate Database"

"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Restore Options Seperate Database" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\Pt6RestoreOptSeperateDb.log"

REM Change to the specified test directory
CD "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%"

SET Restore_Opt_Seperate_Db_TEST_FAILURE="Restore_Opt_Seperate_Db_TEST_FAILURE.txt"

FOR /f %%a IN ('dir /b Restore_Opt_Seperate_Db_TEST_FAILURE.log') do call "C:\Documents and Settings\%user%\Desktop\Ipt6FindFailure.bat" %%a %Restore_Opt_Seperate_Db_TEST_FAILURE%

:EndRunPT6RestoreOptSeperateDb
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndRunPT6RestoreOptSeperateDb")

REM ==================  ROUNDTRIP EXPORT IMPORT COMPARE  ====================

IF %Run_Roundtrip_Sena3_Export_Import_Compare% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip PT6 Create Restore Project")
	GOTO EndRunRndtripSena3ExpImpCompare)

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "--------------------------------------"

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Roundtrip Sena3 Export Import Compare"

SET RndTrip_TEST_FAILURES="C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\RndTrip_TEST_FAILURES.txt"

"C:\PROGRAM FILES\NUnit 2.4.5\bin\nunit-console" "C:\PROGRAM FILES\SIL\FieldWorks\TE_Tests.dll" "/include:Roundtrip Export Import Compare" >> "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\RndTripExpImpCompare.log"

FIND ", Failures: 0," "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\RndTripExpImpCompare.log" >NUL
IF ERRORLEVEL 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "* Failed *"
	ECHO. >> %RndTrip_TEST_FAILURES%
	ECHO Failure in Roundtrip Export Import Compare >> %RndTrip_TEST_FAILURES%
	set /a Roundtrip_Failures = Roundtrip_Failures + 1
)

REM Delete the test failures file if it has no errors in it
::IF EXIST %RndTrip_TEST_FAILURES% for /F %%A in (%RndTrip_TEST_FAILURES%) do If %%~zA equ 0 del %RndTrip_TEST_FAILURES%

::IF NOT EXIST %RndTrip_TEST_FAILURES% (CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "SUCCESS!"
)

:EndRunRndtripSena3ExpImpCompare
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndRunRndtripSena3ExpImpCompare")

REM =========================================================================
REM ############## POPUP TEST FAILURES  ################

:PopupTestFailures

IF %Popup_Test_Failures% EQU 0 (
	IF %Display_Skip_Messages% EQU 1 (
		CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Skip Popup Test Failures")
	GOTO EndPopupTestFailures)

REM - Open a maximized Notepad with the test failure files loaded
IF EXIST %PT5_Proj_TEST_FAILURE% START /max NOTEPAD.EXE %PT5_Proj_TEST_FAILURE%
IF EXIST %Ipt5_TEST_FAILURES% START /max NOTEPAD.EXE %Ipt5_TEST_FAILURES%
IF EXIST %Ipt5BT_TEST_FAILURES% START /max NOTEPAD.EXE %Ipt5BT_TEST_FAILURES%
IF EXIST %PT6_Proj_TEST_FAILURE% START /max NOTEPAD.EXE %PT6_Proj_TEST_FAILURE%
IF EXIST %Ipt6_TEST_FAILURES% START /max NOTEPAD.EXE %Ipt6_TEST_FAILURES%
IF EXIST %Ipt6BT_TEST_FAILURES% START /max NOTEPAD.EXE %Ipt6BT_TEST_FAILURES%
IF EXIST %Restore_Proj_TEST_FAILURE% START /max NOTEPAD.EXE %Restore_Proj_TEST_FAILURE%
IF EXIST %OtherSF_TEST_FAILURES% START /max NOTEPAD.EXE %OtherSF_TEST_FAILURES%
IF EXIST %Restore_Proj_TEST_FAILURE% START /max NOTEPAD.EXE %Restore_Proj_TEST_FAILURE%

:EndPopupTestFailures
IF %Display_EndRun_Messages% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "EndPopupTestFailures"
)

REM =========================================================================

:End

REM - Delete the unnedded file, TestResult.xml
IF EXIST "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\TestResult.xml" (
	del "C:\Documents and Settings\%user%\Desktop\Tests Results\%yymmdd_hhmmss%\TestResult.xml")

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "--------------------------------------"

SET TEST_FAIL_COMP="C:\Documents and Settings\%user%\Desktop\Tests Results\TEST_FAILURE_COMPARISON.txt"
ECHO. >> %TEST_FAIL_COMP%

IF %Run_OtherSF_Test_Cases% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "*OtherSF* Test Case Failures:    %OtherSF_Failures%"
	ECHO "*OtherSF* Test Case Failures:    %OtherSF_Failures%" >> %TEST_FAIL_COMP%)
IF %Run_OtherSF_BT_Test_Cases% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "*OtherSF BT* Test Case Failures: %OtherSF_BT_Failures%"
	ECHO "*OtherSF BT* Test Case Failures: %OtherSF_BT_Failures%" >> %TEST_FAIL_COMP%)
IF %Run_PT5_Test_Cases% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "*PT5* Test Case Failures: . . . .%PT5_Failures%"
	ECHO "*PT5* Test Case Failures: . . . .%PT5_Failures%" >> %TEST_FAIL_COMP%)
IF %Run_PT5_BT_Test_Cases% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "*PT5 BT* Test Case Failures:     %PT5_BT_Failures%"
	ECHO "*PT5 BT* Test Case Failures:     %PT5_BT_Failures%" >> %TEST_FAIL_COMP%)
IF %Run_PT6_Test_Cases% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "*PT6* Test Case Failures: . . . .%PT6_Failures%"
	ECHO "*PT6* Test Case Failures: . . . .%PT6_Failures%" >> %TEST_FAIL_COMP%)

::	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "*PT6* NEW Failures: . . . . . . .%PT6_New_Exp_Failures%"

::IF %Run_PT6_Test_Cases% EQU 1 (
::	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "*PT6* NEW Failures: %PT6_New_Exp_Failures%"
::	ECHO "*PT6* NEW Failures: %PT6_New_Exp_Failures%" >> %TEST_FAIL_COMP%)

IF %Run_PT6_BT_Test_Cases% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "*PT6 BT* Test Case Failures:     %PT6_BT_Failures%"
	ECHO "*PT6 BT* Test Case Failures:     %PT6_BT_Failures%" >> %TEST_FAIL_COMP%)
IF %Run_Roundtrip_Sena3_Export_Import_Compare% EQU 1 (
	CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "*Roundtrip* Test Case Failures:  %Roundtrip_Failures%"
	ECHO "*Roundtrip* Test Case Failures:  %Roundtrip_Failures%" >> %TEST_FAIL_COMP%)

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "--------------------------------------"

SET ENDTM=%TIME%
SET ENDDT=%DATE%

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "Start DateTime: %STARTDT% - %STARTTM%"
ECHO. >> %TEST_FAIL_COMP%
ECHO "Start DateTime: %STARTDT% - %STARTTM%" >> %TEST_FAIL_COMP%

CALL "C:\Documents and Settings\%user%\Desktop\ECHO_LOG.bat" %TEST_PROCESSING_MSGS% "End DateTime:   %ENDDT% - %ENDTM%"
ECHO "End DateTime:   %ENDDT% - %ENDTM%" >> %TEST_FAIL_COMP%
ECHO "Test Directory: Tests Results\%yymmdd_hhmmss%" >> %TEST_FAIL_COMP%
ECHO. >> %TEST_FAIL_COMP%
ECHO "--------------------------------------" >> %TEST_FAIL_COMP%

ECHO.
PAUSE