import buildfw
from Report import *
#-------------------------- FUNCTION EXITSCRIPT ------------------------------
def exitScript(rptObj):
	#Reset the FWROOT environment variable.
	if (rptObj == 1):
		rptObj.closeLog()
	return

#------------------------------- MAIN PROGRAM --------------------------------
rptObj = Report('t.txt')
try:
	print "Now in main"
	Main()
except Exception, err:
	print "Unhandled exception, check for spelling errors: " + str(err)
	exitScript(0)
