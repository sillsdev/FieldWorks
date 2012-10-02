#!/usr/bin/env python

import Constants

#----------------------------------------------------------------------

def print_to_log_file(logText):
	print logText
	# "a" -> append mode
	log_file = open(Constants.log_proc_msgs_file, "a")
	log_file.write(logText + "\n")
	log_file.close()

	#----------------------------------------------------------------------

def print_to_specified_log(logFile, logText):
	print logText
	log_file = open(logFile, "a")
	log_file.write(logText + "\n")
	log_file.close()

	#----------------------------------------------------------------------

def only_print_to_specified_log(logFile, logText):
	log_file = open(logFile, "a")
	log_file.write(logText + "\n")
	log_file.close()

	#----------------------------------------------------------------------

def createReportHeading():
	# Create the "TEST_CASE_PROCESSING.txt" report heading
	print_to_log_file(' ')
	print_to_log_file('*** TE Automated Tests ***')
	print_to_log_file('User is ' + Constants.user)
	print_to_log_file("Created '" + Constants.start_dateTime + "' Test Results directory")
	print_to_log_file('--------------------------------------')

	#----------------------------------------------------------------------

#Takes an amount of seconds and turns it into a human-readable amount of time.
def elapsed_time(seconds, suffixes=['d','h','m','s'], add_s=False, separator=' '):
	time = []
	parts = [(suffixes[0], 60 * 60 * 24),
		(suffixes[1], 60 * 60),
		(suffixes[2], 60),
		(suffixes[3], 1)]

	for suffix, length in parts:
		value = seconds / length
		if value > 1:
			value = int(round(value, 1))
			seconds = seconds % length
			time.append('%s%s' % (str(value),(suffix, (suffix, suffix + 's')[value > 1])[add_s]))
		if seconds < 1:
			break

	return separator.join(time)

#----------------------------------------------------------------------

def createReportEnding(seconds):
	total_time = elapsed_time(
		seconds, [' day',' hour',' minute',' second'], add_s=True, separator=', ')
	print_to_log_file('Total Run Time is -> ' + total_time)
	# Print to the "TEST_FAILURE_COMPARISON.txt" file
	only_print_to_specified_log(Constants.test_fail_comp_file,' ')
	if not Constants.testFailures:
		only_print_to_specified_log(Constants.test_fail_comp_file,'*** NO TEST FAILURES ***')
	only_print_to_specified_log(Constants.test_fail_comp_file,'Total Run Time is -> ' + total_time)
	only_print_to_specified_log(Constants.test_fail_comp_file,'Test Results directory -> ' + Constants.start_dateTime)
	only_print_to_specified_log(Constants.test_fail_comp_file,'--------------------------------------')
