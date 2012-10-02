#!/usr/bin/env python

import os
import subprocess
import Constants
import Reporter
import fileinput
import TestCase
import shutil
import time

#----------------------------------------------------------------------

class TestPlan:
	def type(self, string):
		self.type = string
		self.createProject(False)
		self.createProjectSucceeded(True)
	def testCategory(self, string):
		self.testCategory = string
	def projectLogFile(self, string):
		self.projectLogFile = string
	def backupFile(self, string):
		self.backupFile = string
	def testFailureLog(self, string):
		self.testFailureLog = string
	def timesPerfDbFile(self, string):
		self.timesPerfDbFile = string
	def times1PerfDbFile(self, string):
		self.times1PerfDbFile = string
	def updateTimes1PerfFile(self, string):
		self.updateTimes1PerfFile = string
	def perfChartFile(self, string):
		self.perfChartFile = string
	def dbPerfChartFile(self, string):
		self.dbPerfChartFile = string
	def resultFilesDirectory(self, string):
		self.resultFilesDirectory = string
	def testCases(self, list):
		self.testCases = list
	def createProject(self, bool):
		self.createProject = bool
	def createProjectSucceeded(self, bool):
		self.createProjectSucceeded = bool
	def runTestCases(self, bool):
		self.runTestCases = bool
	def initTimesSena3Db(self, bool):
		self.initTimesSena3Db = bool

	#----------------------------------------------------------------------

	def run(self):

		self.createProjectSucceeded = True
		# Create the Xml Test File
		self.createXmlTestFile()

		Reporter.print_to_log_file('Checking on the existence of the base project')
		backupFilePath = 'C:\\Documents and Settings\\All Users\\Application Data\\SIL\\FieldWorks\\Data\\' + self.backupFile + ' IMPORT TEST.bak'
		backupFilePathExists = os.path.isfile(backupFilePath)

		# Create the base project
		if self.createProject == True or backupFilePathExists == False:
			Reporter.print_to_log_file('Creating the ' + self.type + ' Test base project')
			self.createBaseProject()

		if self.createProjectSucceeded:
			# Run the test cases
			if self.runTestCases:
				# Create the test directory
				subprocess.check_call(["md", Constants.test_directory_fmt2 + self.type], shell=True)

				#Delete the old result files
				os.system('del/q "C:\\GuiTestResults\\' + self.resultFilesDirectory + '\\*.*"')

				testFailuresCount = 0
				# Loop thru the test cases
				for test_case_nbr in self.testCases:
					aTestcase = TestCase.TestCase(test_case_nbr)
					aTestcase.type = self.type
					aTestcase.resultFilesDirectory = self.resultFilesDirectory
					aTestcase.backupFile = self.backupFile
					aTestcase.testFailureLog = self.testFailureLog
					if aTestcase.run():
						os.chdir(Constants.desktop_directory_fmt2)
						# Apply a style sheet to the xml file to produce a more readable test case xml file
						p = subprocess.check_call(self.updateTimes1PerfFile, shell=True)
						# Copy 'Times1.xml' to 'Times.xml'
						shutil.copy(self.times1PerfDbFile, self.timesPerfDbFile)

						# Checking on the existence of the Performance directory
						performDirPath = Constants.test_directory_fmt2 + 'Performance'
						performDirPathExists = os.path.isdir(performDirPath)
						# Create the Performance directory if it doesn't exist
						if performDirPathExists == False:
							subprocess.check_call(["md", performDirPath], shell=True)
						os.chdir(performDirPath)
						shutil.copy((Constants.desktop_directory_fmt2 + self.timesPerfDbFile), self.timesPerfDbFile)
						shutil.copy((Constants.desktop_directory_fmt2 + self.perfChartFile), self.perfChartFile)
						shutil.copy((Constants.desktop_directory_fmt2 + self.dbPerfChartFile), self.dbPerfChartFile)
					else:
						testFailuresCount += 1

				sourceDirectory = 'C:\\GuiTestResults\\' + self.resultFilesDirectory
				file_count = len(os.walk(sourceDirectory).next()[2])
				if file_count > 0:
					# Copy all the test result files from the generic GuiTestResults directory to
					# the new GuiTestResults folder for permanently storing the export output files.
					targetDirectory = Constants.test_directory_fmt2 + 'GuiTestResults\\' + self.resultFilesDirectory
					shutil.copytree(sourceDirectory,targetDirectory)

				Reporter.print_to_log_file('   ')
				if testFailuresCount > 0:
					Constants.testFailures = True
					testCaseFailuresStr = "%i" %testFailuresCount
					Reporter.print_to_log_file('!!! ' + self.type + ' Test Failures = ' + testCaseFailuresStr + ' !!!')
					Reporter.only_print_to_specified_log(Constants.test_fail_comp_file,self.type + ' Test Failures = ' + testCaseFailuresStr)
					Reporter.only_print_to_specified_log(Constants.all_Tests_Failure_Log,'=======================')
				else:
					Reporter.print_to_log_file('!!! SUCCESS -> ' + self.type + ' Test Failures = 0 !!!')

				Reporter.print_to_log_file('====================================================')

	#----------------------------------------------------------------------

	def runSingleTest(self):

		if self.runTestCases == True:

			if self.initTimesSena3Db == True:
				# Initialize / clean-up the file named TimesSena3Db.xml
				os.chdir(Constants.desktop_directory_fmt2)
				shutil.copy('Original_TimesSena3Db.xml', 'TimesSena3Db.xml')
				shutil.copy('Original_TimesSena3Db.xml', 'Times1Sena3Db.xml')

			startTime = time.clock()

			self.createProject == False
			# Create the test directory
			subprocess.check_call(["md", Constants.test_directory_fmt2 + self.type], shell=True)

			#Delete the old result files
			os.system('del/q "C:\\GuiTestResults\\' + self.resultFilesDirectory + '\\*.*"')

			testFailuresCount = 0

			log_file = Constants.test_directory + self.type + '\\TestCase' + '.log'
			log_file_no_quotes = Constants.test_directory_fmt2 + self.type + '\\TestCase' + '.log'

			# Prints '*** RoundTrip_Oxes'
			test_case_type_nbr = '*** ' + self.type
			Reporter.print_to_log_file(test_case_type_nbr)

			# Format the command specifying the test-category to run and the output log file for the results
			command = ('%sRunTeAutoTests.bat %s >> %s') % (Constants.desktop_directory,self.testCategory,log_file)
			p = subprocess.call(command, shell=True)

			self.testPassed = False
			# The test passed if there are no Failures
			for line in fileinput.FileInput(log_file_no_quotes):
				if ", Failures: 0," in line:
					self.testPassed = True

			if self.testPassed == True or self.testPassed == False:
				os.chdir(Constants.desktop_directory_fmt2)
				# Apply a style sheet to the xml file to produce a more readable test case xml file
				p = subprocess.check_call(self.updateTimes1PerfFile, shell=True)
				# Copy 'Times1Sena3Db.xml' to 'TimesSena3Db.xml'
				shutil.copy(self.times1PerfDbFile, self.timesPerfDbFile)

				# Checking on the existence of the Performance directory
				performDirPath = Constants.test_directory_fmt2 + 'Performance'
				performDirPathExists = os.path.isdir(performDirPath)
				# Create the Performance directory if it doesn't exist
				if performDirPathExists == False:
					subprocess.check_call(["md", performDirPath], shell=True)
				os.chdir(performDirPath)
				shutil.copy((Constants.desktop_directory_fmt2 + self.timesPerfDbFile), self.timesPerfDbFile)
				shutil.copy((Constants.desktop_directory_fmt2 + self.perfChartFile), self.perfChartFile)
				shutil.copy((Constants.desktop_directory_fmt2 + self.dbPerfChartFile), self.dbPerfChartFile)

			if self.testPassed == False:
				Reporter.print_to_log_file('*** Failed ***')
				test_case_name = '*** ' + self.testCategory
				Reporter.only_print_to_specified_log(self.testFailureLog,test_case_name)
				Reporter.only_print_to_specified_log(Constants.all_Tests_Failure_Log,test_case_type_nbr)

			sourceDirectory = 'C:\\GuiTestResults\\' + self.resultFilesDirectory
			file_count = len(os.walk(sourceDirectory).next()[2])
			if file_count > 0:
				# Copy all the test result files from the generic GuiTestResults directory to
				# the new GuiTestResults folder for permanently storing the export output files.
				targetDirectory = Constants.test_directory_fmt2 + 'GuiTestResults\\' + self.resultFilesDirectory
				shutil.copytree(sourceDirectory,targetDirectory)

			Reporter.print_to_log_file('   ')
			if self.testPassed == False:
				Constants.testFailures = True
				testCaseFailuresStr = "%i" %testFailuresCount
				Reporter.print_to_log_file('!!! ' + self.type + ' Test Failure !!!')
				Reporter.only_print_to_specified_log(Constants.test_fail_comp_file,self.type + ' Test Failures = ' + testCaseFailuresStr)
				Reporter.only_print_to_specified_log(Constants.all_Tests_Failure_Log,'=======================')
			else:
				Reporter.print_to_log_file('!!! SUCCESS -> ' + self.type + ' Test Failures = 0 !!!')

			Reporter.print_to_log_file('====================================================')

	#----------------------------------------------------------------------

	def createBaseProject(self):

		# Format the command specifying the test-category to run and the output log file for the results
		test_category = self.testCategory
		log_file = self.projectLogFile
		command = ('%sRunTeAutoTests.bat %s >> %s%s') % (Constants.desktop_directory,test_category,Constants.test_directory,log_file)

		try:
			subprocess.check_call(command, shell=True)
		except: # catch *all* exceptions
			self.createProjectSucceeded = False
			Reporter.print_to_log_file('Creating "' + self.backupFile + '" base project FAILED')
		else:
			# Backup the project
			Reporter.print_to_log_file('Backing up project "' + self.backupFile + ' IMPORT TEST"')
			#db backup "MALAY PARATEXT 5 IMPORT TEST" "MALAY PARATEXT 5 IMPORT TEST.bak"
			command = ('db backup "' + self.backupFile + ' IMPORT TEST" "' + self.backupFile + ' IMPORT TEST.bak"')
			p = subprocess.check_call(command, shell=True)
			Reporter.print_to_log_file('====================================================')

	#----------------------------------------------------------------------

	def createXmlTestFile(self):
		os.chdir('C:\\fw\\Test\\GuiTestDriver\\TeModel')

		# Convert the spreadsheet comma delineated file of test cases to Xml
		os.system('CSV2XML Import' + self.type + '.CSV')

		# Apply a style sheet to the xml file to produce a more readable test case xml file
		command = ('%sXmlFromCsvStylesheet.bat %s') % (Constants.desktop_directory,Constants.user)
		p = subprocess.check_call(command, shell=True)
		Reporter.print_to_log_file('Created the ' + self.type + ' XML test cases file')
		Reporter.print_to_log_file(' ')

	#----------------------------------------------------------------------

class RoundTripOxes(TestPlan):
	def __init__(self):
		self.type = 'RoundTrip_Oxes'
		self.testCategory = 'Roundtrip_Export_Import_Compare'
		self.projectLogFile = 'CreateBaseProject_ImportPT5.log'
		self.backupFile = 'SENA 3'
		self.testFailureLog = Constants.test_directory_fmt2 + "Failures_Oxes.txt"
		self.resultFilesDirectory = 'RoundtripCompare'
		self.timesPerfDbFile = 'TimesSena3Db.xml'
		self.times1PerfDbFile = 'Times1Sena3Db.xml'
		self.updateTimes1PerfFile = 'UpdateTimes1Sena3Db.bat'
		self.perfChartFile = 'PerfChart_Sena3.xsl'
		self.dbPerfChartFile = 'DbSena3PerfChart.xsl'
		self.testCases = [1]

	#----------------------------------------------------------------------

#class TestPlanMalayDb(TestPlan):
#    def __init__(self):
#        self.timesPerfDbFile = 'TimesMalayDb.xml'
#        self.times1PerfDbFile = 'Times1MalayDb.xml'
#        #self.updateTimes1PerfFile = 'UpdateTimes1MalayDb.bat'
#        self.perfChartFile = 'PerfChart_Malay.xsl'
#        self.dbPerfChartFile = 'DbMalayPerfChart.xsl'

	#----------------------------------------------------------------------

class TestPlanSF(TestPlan):
	def __init__(self):
		self.type = 'OTHERSF'
		self.testCategory = 'Create_OtherSF_Project'
		self.projectLogFile = 'CreateBaseProj_ImportOtherSF.log'
		self.backupFile = 'MALVI OTHERSF'
		self.testFailureLog = Constants.test_directory_fmt2 + "Failures_OtherSF.txt"
		self.resultFilesDirectory = 'ImportOtherSF'
		self.timesPerfDbFile = 'TimesMalayDb.xml'
		self.times1PerfDbFile = 'Times1MalayDb.xml'
		self.updateTimes1PerfFile = 'UpdateTimes1MalayDb.bat'
		self.perfChartFile = 'PerfChart_Malay.xsl'
		self.dbPerfChartFile = 'DbMalayPerfChart.xsl'
		self.testCases([1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50])

	#----------------------------------------------------------------------

class TestPlanSF_SepBT(TestPlanSF, object):
	def __init__(self):
		# Call the super class __init__() function
		super(TestPlanSF_SepBT, self).__init__()
		self.type = 'OTHERSF_SepBT'
		self.testFailureLog = Constants.test_directory_fmt2 + "Failures_OtherSF_SepBT.txt"
		self.resultFilesDirectory = 'ImportOtherSF_SepBT'
		self.testCases = [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100]

	#----------------------------------------------------------------------

class TestPlanPT5(TestPlan):
	def __init__(self):
		self.type = 'PARATEXT5'
		self.testCategory = 'Create_PT_Project'
		self.projectLogFile = 'CreateBaseProject_ImportPT5.log'
		self.backupFile = 'MALAY PARATEXT 5'
		self.testFailureLog = Constants.test_directory_fmt2 + "Failures_PT5.txt"
		self.resultFilesDirectory = 'ImportParatext5'
		self.timesPerfDbFile = 'TimesMalayDb.xml'
		self.times1PerfDbFile = 'Times1MalayDb.xml'
		self.updateTimes1PerfFile = 'UpdateTimes1MalayDb.bat'
		self.perfChartFile = 'PerfChart_Malay.xsl'
		self.dbPerfChartFile = 'DbMalayPerfChart.xsl'
		self.testCases = [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42]

	#----------------------------------------------------------------------

class TestPlanPT5BT(TestPlanPT5, object):
	def __init__(self):
		# Call the super class __init__() function
		super(TestPlanPT5BT, self).__init__()
		self.type = 'PARATEXT5_BT'
		self.testFailureLog = Constants.test_directory_fmt2 + "Failures_PT5BT.txt"
		self.resultFilesDirectory = 'ImportParatext5BT'
		self.testCases = [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87]

	#----------------------------------------------------------------------

class TestPlanPT6(TestPlan):
	def __init__(self):
		self.type = 'PARATEXT6'
		self.testCategory = 'Create_PT_Project'
		self.projectLogFile = 'CreateBaseProject_ImportPT6.log'
		self.backupFile = 'MALAY PARATEXT 6'
		self.testFailureLog = Constants.test_directory_fmt2 + "Failures_PT6.txt"
		self.resultFilesDirectory = 'ImportParatext6'
		self.timesPerfDbFile = 'TimesMalayDb.xml'
		self.times1PerfDbFile = 'Times1MalayDb.xml'
		self.updateTimes1PerfFile = 'UpdateTimes1MalayDb.bat'
		self.perfChartFile = 'PerfChart_Malay.xsl'
		self.dbPerfChartFile = 'DbMalayPerfChart.xsl'
		self.testCases = [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42]

	#----------------------------------------------------------------------

class TestPlanPT6BT(TestPlanPT6, object):
	def __init__(self):
		# Call the super class __init__() function
		super(TestPlanPT6BT, self).__init__()
		self.type = 'PARATEXT6_BT'
		self.testFailureLog = Constants.test_directory_fmt2 + "Failures_PT6BT.txt"
		self.resultFilesDirectory = 'ImportParatext6BT'
		self.testCases = [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87]

	#----------------------------------------------------------------------
