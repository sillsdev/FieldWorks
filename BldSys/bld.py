#----------------------------------------------------------------------------------------------
#   Copyright 2001, SIL International. All rights reserved.
#
#   File: bld.py
#   Responsibility: JeffG
#   Last reviewed:
#
#   Description: Functions related to building FieldWorks.
#----------------------------------------------------------------------------------------------
#   Imported Modules
#----------------------------------------------------------------------------------------------
import time, os, misc, re
from Report import *
#----------------------------------------------------------------------------------------------
#   Function to call mkall.bat with appropiate settings.
#----------------------------------------------------------------------------------------------
def bld_buildFW(strBldFldr, rptObj, strBldType, strFlags):
	strBldFldr = re.sub('\\\\*$', '\\\\', strBldFldr, 1)
	strBldCmdStr = os.path.join(strBldFldr, "bin\\mkall.bat")
	if(os.path.exists(strBldCmdStr)):
		rptObj.reportProgress("Starting " + strBldType + " build...")
		rptObj.closeLog()           # Close so the external process can append to the log file
		try:
			misc.misc_RunExtProg(strBldCmdStr, strFlags, "FW_BUILD_ERROR", rptObj, rptObj.getLogFile())
		except Exception, err:
			rptObj.reopenLog()
			rptObj.reportFailure(strBldType + " build failed, " + str(err), 1)
			rptObj.closeLog()
			raise Exception, str(err) + ":error running " + strBldCmdStr
		rptObj.reopenLog()          # Re-open the log file for appending
		rptObj.reportProgress("Finished building " + strBldType + " build.")
	else:
		raise Exception, ":error " + strBldCmdStr + " does not exist."
	return
#----------------------------------------------------------------------------------------------
#	Function to call Wise InstallBuilder 8.1 with appropiate settings..
#----------------------------------------------------------------------------------------------
def bld_installer(strBldFldr, strOutputFldr, rptObj, strWiseFile, strBldType, strProduct):
	# TODO: JeffG - Make the install script use the data in daly output folder so
	# installer can build from the daly copy of the source.
	#strInstBldrExe = "c:\\Program Files\\Wise InstallBuilder 8.1\\installbuilder.exe"
	# Must use short form for all path names because won't work if path and parameters are both in double quotes
	strInstBldrExe = 'c:\\Progra~1\\WiseIn~1.1\\installbuilder.exe'
	strIBScriptFile = os.path.join(strBldFldr, strWiseFile)
	# Build installer for the build type.
	rptObj.reportProgress("Building " + strProduct + " " + strBldType + " version installer...")
	strCmpVars = "/d _BUILD_TYPE_=" + strBldType + " /d _OUTPUT_DIR_=" + \
				 strOutputFldr + " /d _FW_ROOT_DIR_=" + strBldFldr + " /c /s "
	try:
		misc.misc_RunExtProg(strInstBldrExe, strCmpVars + strIBScriptFile, None, rptObj, None)
	except Exception, err:
		raise Exception, str(err)
	# Sleep to let the dumb InstallBuilder cool down
	rptObj.reportProgress("Sleeping 1 minute to let dumb InstallBuilder cool down...")
	time.sleep(60)
	return
#----------------------------------------------------------------------------------------------
