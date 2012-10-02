#!/usr/bin/env python

import datetime

#----------------------------------------------------------------------

user = "JonesT"

start_dateTime = datetime.datetime.today().strftime("%Y-%m-%d  %H.%M.%S")

test_directory='C:\\"Documents and Settings"\\' + user + '\\Desktop\\"Tests Results"\\"' + start_dateTime + '"\\'

test_directory_fmt2='C:\\Documents and Settings\\' + user + '\\Desktop\\Tests Results\\' + start_dateTime + '\\'

desktop_directory='C:\\"Documents and Settings"\\' + user + '\\Desktop\\"AutoTests Scripts"\\'

desktop_directory_fmt2='C:\Documents and Settings\JonesT\Desktop\AutoTests Scripts\\'

fw_data_directory='C:\Documents and Settings\All Users\Application Data\SIL\FieldWorks\Data'

kdiff3_path='"C:\Program Files\KDiff3\kdiff3.exe" '

compare_files_log='C:\\GuiTestResults\\CompareFiles.txt'

log_proc_msgs_file = test_directory_fmt2 + "TEST_CASE_PROCESSING.txt"

test_fail_comp_file = 'C:\\Documents and Settings\\' + user + '\\Desktop\\Tests Results\\' + "TEST_FAILURE_COMPARISON.txt"

testFailures = False

all_Tests_Failure_Log = test_directory_fmt2 + "Failures_ALL_Tests.txt"
