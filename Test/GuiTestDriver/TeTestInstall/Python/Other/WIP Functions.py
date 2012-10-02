

# PRINT_FILE(file):
#   - Print the text in the file.
#   - Test Case: PRINT_FILE(TEST_PROCESSING_MSGS)
def PRINT_FILE(file):
	if os.path.isfile(file):
		in_file = open(file, "r")
		text = in_file.read()
		print text
		in_file.close()

def ECHO_LOG2(logfileText):
	print "\n" + logfileText[1]
	if logfileText[0] != 0:
		log_file = open(logfileText[0], "a") # "a" -> append mode

#   - Delete the file.
def delete_file(file):
	if os.path.isfile(file):
		os.remove(file)
#delete_file(var_log_proc_msgs)
