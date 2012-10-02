#!/usr/bin/env python

import os
import subprocess
import Constants
import Reporter
import TestPlan
import FieldWorks
import shutil
import time

#----------------------------------------------------------------------

def runTests(testCases):

	if not ONLY_Install_Fieldworks and not ONLY_Reinstall_Fieldworks:
		if Force_Create_Base_Projects:
			aTestPlan.createProject(True)

		if Run_ALL_Test_Cases:
			aTestPlan.runTestCases = True
			aTestPlan.run()
		else:
			aTestPlan.testCases[0:] = testCases
			if aTestPlan.runTestCases:
				aTestPlan.run()

#----------------------------------------------------------------------
startTime = time.clock()

# Prepare for the timing performance tests
os.chdir(Constants.desktop_directory_fmt2)
shutil.copy('Original_TimesMalayDb.xml', 'TimesMalayDb.xml')
shutil.copy('Original_TimesMalayDb.xml', 'Times1MalayDb.xml')

# Create the test directory
subprocess.check_call(["md", Constants.test_directory_fmt2], shell=True)
Reporter.createReportHeading()

#------  RUN TESTS  --------
Reinstall_Fieldworks = False
ONLY_Install_Fieldworks = False
ONLY_Reinstall_Fieldworks = False
Force_Create_Base_Projects = False
Run_ALL_Test_Cases = False

if ONLY_Install_Fieldworks:
	FieldWorks.FieldWorks().install()
elif Reinstall_Fieldworks or ONLY_Reinstall_Fieldworks:
	FieldWorks.FieldWorks().reinstall()
# ===============

aTestPlan = TestPlan.RoundTripOxes()
aTestPlan.initTimesSena3Db(False)
aTestPlan.runTestCases(False)
if not ONLY_Install_Fieldworks and not ONLY_Reinstall_Fieldworks:
	aTestPlan.runSingleTest()
# ===============

aTestPlan = TestPlan.TestPlanSF()
aTestPlan.runTestCases(False)
runTests([1])
# ===============

aTestPlan = TestPlan.TestPlanSF_SepBT()
aTestPlan.runTestCases(True)
runTests([1])
# ===============

aTestPlan = TestPlan.TestPlanPT5()
aTestPlan.runTestCases(False)
runTests([1,2,3,4,5,6,7])
#runTests([7,8,22,36])
# ===============

aTestPlan = TestPlan.TestPlanPT5BT()
aTestPlan.runTestCases(False)
#runTests([1,2,3,4])
#runTests([6,21])
runTests([1])
# ===============

aTestPlan = TestPlan.TestPlanPT6()
aTestPlan.runTestCases(False)
runTests([1,2,3,4,5,6,7])
# ===============

aTestPlan = TestPlan.TestPlanPT6BT()
aTestPlan.runTestCases(False)
runTests([1])
# ===============

#if ONLY_Reinstall_Fieldworks == False:
if not ONLY_Reinstall_Fieldworks:
	Reporter.createReportEnding(time.clock() - startTime)
