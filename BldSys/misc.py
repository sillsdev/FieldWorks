#----------------------------------------------------------------------------------------------
#   Copyright 2001, SIL International. All rights reserved.
#
#   File: misc.py
#   Responsibility: JeffG
#   Last reviewed:
#
#   Misc Functions
#----------------------------------------------------------------------------------------------
#   Imported Modules
#----------------------------------------------------------------------------------------------
import os, re
from Report import *
#----------------------------------------------------------------------------------------------
#   Function runs external program.
#----------------------------------------------------------------------------------------------
def misc_RunExtProg(strProg, strArgs, strEnvErr, rptObj, strLogFile):
	rptObj.reopenLog()
	rptObj.echoItVerbose("In RunExtProg. ")

	# This works.  Couldn't use in Javascript because it does not handle string with quotes like
	#   "c:\\Program Files\\Wise Installbuilder 8.1\\"
	if(os.path.exists(strProg) == 0):
		strError = "Cannot find program " + strProg
		raise Exception, strError

	# Don't surround the file name and path with double quotes because
	# DOS won't accept the pathname in double quotes if a parameter is also in quotes (db_functions).
	# Have used short form of all pathnames instead (c:\progra~1 instead of "c:\program files").
	# strQuotedProg = "\"" + strProg + "\""
	strQuotedProg = strProg
	if(strLogFile and len(strLogFile) > 0):
		rptObj.echoItVerbose("Run: " + strQuotedProg + " " + strArgs + " >>" + strLogFile)
		rptObj.closeLog()
		retValue = os.system(strQuotedProg + " " + strArgs + " >>" + strLogFile)
	else:
		rptObj.echoItVerbose("Run: " + strQuotedProg + " " + strArgs)
		rptObj.closeLog()
		retValue = os.system(strQuotedProg + " " + strArgs)
	rptObj.reopenLog()
	if(strEnvErr):
	   retValue = os.environ.get(strEnvErr)

	rptObj.echoItVerbose("RunExtProg:External program returned " + str(retValue))
	if((retValue != None) and (retValue != 0)):
		raise Exception, "Command " + strProg + " failed, with return value: " + str(retValue)
	return
#----------------------------------------------------------------------------------------------
# Function prints Usage message to screen.
#----------------------------------------------------------------------------------------------
def misc_DisplayUsage(rptObj):
	rptObj.closeLog() # forget logging the usage message
	print "Usage: bldfw fw_source_root output_root [options]"
	print "Options:"
	print "\t-bbuildlevel, [0-8]"
	print "\t-lversion label"
	print "\t-dbuild option"
	print "Recognized build options :"
	print "\tnodebugbld"
	print "\tnoreleasebld"
	print "\tnoboundsbld"
	# TODO: JeffG - create no builds option
	print "\tnobuilds"
	print "\tnoautotest"
	print "\tnorefreshsrc"
	print "\tnocreatedbs"
	print "\tnoinstallblder"
	print "\tdebug"
	print "\tnoftpupload"
	print "\tnocreatedoc"
	print "\tnodeloutput - do not delete output trees."
	print "\ttestrefresh - only run refresh source section"
	print "\ttestdoc - only run doc create section"
	print "\ttestdb - only run create database section"
	print "\ttestinst - only run install builder section"
	return
#----------------------------------------------------------------------------------------------
