#!/usr/bin/env python

import subprocess
import Constants
import Reporter
import fileinput
import linecache
import time

#----------------------------------------------------------------------

class TestCase:
	# name is a interger like 14 (e.g. test case 14)
	def __init__(self, int):
		self.name = int
		self.testPassed = False
	def type(self, string):
		self.type = string
	def resultFilesDirectory(self, string):
		self.type = resultFilesDirectory
	def backupFile(self, string):
		self.backupFile = string
	def testFailureLog(self, string):
		self.testFailureLog = string

	#----------------------------------------------------------------------

	def run(self):
		startTime = time.clock()

		# Convert the test case number int to a string
		testCase_Nbr = "%i" %self.name

		test_category = 'Test_Case_' + testCase_Nbr
		if self.name < 10:
			log_file = Constants.test_directory + self.type + '\\TestCase0' + testCase_Nbr + '.log'
			log_file_no_quotes = Constants.test_directory_fmt2 + self.type + '\\TestCase0' + testCase_Nbr + '.log'
		else:
			log_file = Constants.test_directory + self.type + '\\TestCase' + testCase_Nbr + '.log'
			log_file_no_quotes = Constants.test_directory_fmt2 + self.type + '\\TestCase' + testCase_Nbr + '.log'

		Reporter.print_to_log_file('Restoring base project')
		#db restore "MALAY PARATEXT 5 IMPORT TEST" "MALAY PARATEXT 5 IMPORT TEST.bak"
		command = ('db restore "' + self.backupFile + ' IMPORT TEST" "' + self.backupFile + ' IMPORT TEST.bak"')
		p = subprocess.check_call(command, shell=True)

		# Prints '*** PARATEXT5_BT / Test_Case_1'
		test_case_type_nbr = '*** ' + self.type + ' / ' + test_category
		Reporter.print_to_log_file(test_case_type_nbr)

		# Format the command specifying the test-category to run and the output log file for the results
		command = ('%sRunTeAutoTests.bat %s >> %s') % (Constants.desktop_directory,test_category,log_file)
		p = subprocess.call(command, shell=True)

		# The test passed if there are no Failures
		for line in fileinput.FileInput(log_file_no_quotes):
			if ", Failures: 0," in line:
				self.testPassed = True

		if not self.testPassed:
			Reporter.print_to_log_file('*** Failed ***')
			test_case_name = '*** ' + test_category
			Reporter.only_print_to_specified_log(self.testFailureLog,test_case_name)
			Reporter.only_print_to_specified_log(Constants.all_Tests_Failure_Log,test_case_type_nbr)

			for line in fileinput.FileInput(log_file_no_quotes):
				if "1) GuiTestDriver.ImportParatext6.ImportCase" in line:
					Reporter.print_to_log_file(line)
					Reporter.only_print_to_specified_log(self.testFailureLog,line)
					Reporter.only_print_to_specified_log(Constants.all_Tests_Failure_Log,line)

			for line in fileinput.FileInput(log_file_no_quotes):
				if " File-Comp Assert:" in line:
					# Clear the linecache because 'CompareFiles.txt' has changed
					linecache.clearcache()
					mrfPathName = linecache.getline(Constants.compare_files_log, 1)
					testCaseResultFile = linecache.getline(Constants.compare_files_log, 2)
					exportScriptureFilePath = linecache.getline(Constants.compare_files_log, 3)

					#example-> C:\Documents and Settings\JonesT\Desktop\Tests Results\2009-07-30  13.27.39\GuiTestResults\ImportParatext6\Test Case 4 Scripture.sf
					exportFilePathName = ' \"' + Constants.test_directory_fmt2 + 'GuiTestResults\\' + self.resultFilesDirectory + '\\' + testCaseResultFile[:-1] + '\"'
					kdiff3_formatted_line = Constants.kdiff3_path + mrfPathName[:-1] + exportFilePathName
					kdiff3_formatted_line2 = Constants.kdiff3_path + mrfPathName[:-1] + ' ' + exportScriptureFilePath

					instruction = 'NOTE - Paste this line into a cmd window to compare the export file with the MRF'
					instruction2 = 'NOTE - Use this line for compare when test cases are stopped before their completion'

					Reporter.only_print_to_specified_log(self.testFailureLog,instruction2)
					Reporter.only_print_to_specified_log(Constants.all_Tests_Failure_Log,instruction2)
					Reporter.only_print_to_specified_log(self.testFailureLog,kdiff3_formatted_line2)
					Reporter.only_print_to_specified_log(Constants.all_Tests_Failure_Log,kdiff3_formatted_line2)
					#Reporter.only_print_to_specified_log(self.testFailureLog,' ')
					#Reporter.only_print_to_specified_log(Constants.all_Tests_Failure_Log,' ')
					Reporter.only_print_to_specified_log(self.testFailureLog,instruction)
					Reporter.only_print_to_specified_log(Constants.all_Tests_Failure_Log,instruction)
					Reporter.only_print_to_specified_log(self.testFailureLog,kdiff3_formatted_line)
					Reporter.only_print_to_specified_log(Constants.all_Tests_Failure_Log,kdiff3_formatted_line)

			Reporter.only_print_to_specified_log(self.testFailureLog,'---------------')
			Reporter.only_print_to_specified_log(Constants.all_Tests_Failure_Log,'---------------')

		seconds = time.clock() - startTime
		total_time = Reporter.elapsed_time(
			seconds, [' day',' hour',' minute',' second'], add_s=True, separator=', ')
		Reporter.print_to_log_file(total_time)
		Reporter.print_to_log_file('---------------')

		if self.testPassed:
			return True
		else:
			return False

	#----------------------------------------------------------------------
