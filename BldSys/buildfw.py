#-----------------------------------------------------------------------------------------------
#   Copyright 2001, SIL International. All rights reserved.
#
#   File: buildfw.py
#   Responsibility: JeffG
#   Last reviewed:
#
#	Build program. Calls bld.py, misc.py, file.py, Report.py, db_functions.py, objweb.py
#
#-----------------------------------------------------------------------------------------------
#-----------------------------------------------------------------------------------------------
# Imported modules
#-----------------------------------------------------------------------------------------------
import os, re, sys, string, Report, misc, db_functions, file, bld, objweb
from misc import *
from db_functions import *
from Report import *
from file import *
from bld import *
from objweb import *
#-----------------------------------------------------------------------------------------------
# Global Variables
#-----------------------------------------------------------------------------------------------
false = 0;
true = 1;
Debug = false;
#-----------------------------------------------------------------------------------------------
def Main():
	rptObj = Report('c:\\overnitebld.log');

	# TODO: When debugging is complete, remove the following line of code to turn
	#	verbose mode off
	rptObj.verboseModeOn()

	# Get command line arguments, the first of which is this script's name
	arguments = sys.argv

	# Name of script + two parameters = 3 parameters (we just don't care about the first one)
	if len(arguments) < 3:
		misc_DisplayUsage(rptObj)
		raise Exception, 'Too few command line parameters'

	nodebugbld = false
	noreleasebld = false
	noboundsbld = false
	noautotest = false
	norefreshsrc = false
	nocreatedbs = false
	noinstallbldr = false
	nocreatedoc = false
	testrefresh = false
	testdoc = false
	testdb = false
	testdb = false
	testinst = false
	nodeloutput = false

	# Assume the first two arguments are the build root and output root, and ensure they
	#	end with \ (backslashes)
	strBldFldr = arguments[1]
	strBldFldr = re.sub('\\\\*$', '\\\\', strBldFldr, 1)
	strOutputFldr = arguments[2]
	strOutputFldr = re.sub('\\\\*$', '\\\\', strOutputFldr, 1)
	bldLevel = ''

	TODO: Add strFWDeliverables variable and retrieve off the command line.

	# Loop through and process the command line arguments
	for iArgs in range(3, len(arguments)):
		strArg = arguments[iArgs]

		if strArg[0] != '-':
			rptObj.echoIt('Options must be preceeded by a "-".')
			raise Exception, 'Option not preceeded by a "-"'

		cmd = string.lower(strArg[1])
		if cmd == 'b':
			bldLevel = long(strArg[2])
			if bldLevel < 0 or bldLevel > 8:
				rpt.echoIt('ERROR: Buildlevel must be an integer between 0 and 8.')
				raise Exception, 'Invalid Buildlevel--must be an integer between 0 and 8'
			rptObj.echoItVerbose('Build level set to ' + strArg[2])
		elif cmd == 'l':
			strVSLabel = strArg[2]
			rptObj.echoItVerbose('Applying label ' + strVSSLabel)
		# The command dVariableName results in the the code "VariableName = true" being executed
		elif cmd == 'd':
			strVar = strArg[2:len(strArg)]
			strVar = string.lower(strVar)
			try:
				exec strVar + '= true'
			except:
				raise 'Could not assign variable ' + strVar
			rptObj.echoItVerbose('Defining ' + strVar)
		elif cmd == 'o':
			strOutputFldrOverride = strArg[2:len(strArg)]
			rptObj.echoItVerbose('Overriding output directory ' + strVar)

		else:
			rptObj.echoIt('ERROR: Invalid argument, "' + strArg + '"')
			raise Exception, 'Invalid argument, "' + strArg + '"'

	if testrefresh:
		nodebugbld = true
		noreleasebld = true
		noboundsbld = true
		noautotest = true
		nodeloutput = true
		norefreshsrc = false
		nocreatedbs = true
		noinstallbldr = true
		nocreatedoc = true
	if testdoc:
		nodebugbld = true
		noreleasebld = true
		noboundsbld = true
		noautotest = true
		nodeloutput = true
		norefreshsrc = true
		nocreatedbs = true
		noinstallbldr = true
		nocreatedoc = false
	if testdb:
		nodebugbld = true
		noreleasebld = true
		noboundsbld = true
		noautotest = true
		nodeloutput = true
		norefreshsrc = true
		nocreatedbs = false
		noinstallbldr = true
		nocreatedoc = true
	if testinst:
		nodebugbld = true
		noreleasebld = true
		noboundsbld = true
		noautotest = true
		nodeloutput = true
		norefreshsrc = true
		nocreatedbs = true
		noinstallbldr = false
		nocreatedoc = true
	rptObj.echoItVerbose('Setting up build system')
	rptObj.echoItVerbose('Setting env vars.')

	os.environ['BUILD_LEVEL'] = str(bldLevel)
	#os.environ['FWROOT'] = strBldFldr
	# Set FWROOT and BUILD_ROOT to the source file directory (used w/mkall.bat)
	os.environ['FWROOT'] = 'c:\fwsrc'
	os.environ['BUILD_ROOT'] = 'c:\fwsrc'

	# Delete output directories
	if not nodeloutput:
		rptObj.echoItVerbose('Removing the output folders...')
		try:
			rptObj.echoItVerbose('Removing the output folder...')
			file_DeleteFolder(os.path.join(strBldFldr, 'output'))
			rptObj.echoItVerbose('Removing the obj folder...')
			file_DeleteFolder(os.path.join(strBldFldr, 'obj'))
			rptObj.echoItVerbose('Removing the overnite files...')
			file_DeleteFile(os.path.join(strOutputFldr, 'overnite.tst'))
			rptObj.echoItVerbose('Done clearing out source tree')
		except Exception, err:
			rptObj.reportFailure('Unable to recreate source tree, ' + str(err), 1)
			raise

	#TODO - Find out how to refresh the source, or if it is even necessary
	#if not norefreshsrc:
	#	rptObj.echoItVerbose('About to check for refresh')
	#	rptObj.reportProgress('Getting latest source from SourceSafe...')

	os.system('mkdir c:\\build\\Output')
	os.system('xcopy c:\\fwsrc\\Output C:\\build\\Output\\ /s/q/Y')
	os.system('mkdir c:\\build\\src')
	os.system('xcopy c:\\fwsrc\\src C:\\build\\src\\ /s/q/Y')

	# Build debug version
	if not nodebugbld:
		bld_buildFW(strBldFldr, rptObj, 'Debug', 'd register')
		rptObj.echoItVerbose('About to check for noautotest')

		# If everything built OK in the debug build, run the tests without TrueCoverage
		# REVIEW: JeffG - Shouldn't this use RunProg and use exceptions
		if not noautotest:
			# Create a directory for log files
			strLogFolder = os.path.join(os.path.join(strBldFldr, 'TestLog'), 'Log')
			file_MakeSureFolderExists(strLogFolder)
			rptObj.reportProgress('Running automated test without TrueCoverage...')
			os.system(os.path.join(os.path.join(strBldFldr, 'bin'), 'runtest.exe overnite.tst'))

	# Create databases.  Do before bounds build to the *.tsc file dialog will not come up on
	#	loadxml.exe call.  Do before release build as the sql file generated by the release
	#	build is bogus
	rptObj.echoItVerbose('About to check for createdb')
	if not nocreatedbs:
		rptObj.reportProgress('Building databases...')
		rptObj.reportProgress('Building TestLangProj...')
		rptObj.closeLog()
		try:
			# Build the TestLangProj database
			db_BuildDB('TestLangProj', None, \
					   os.path.join(strBldFldr, 'Output\\Common\\NewLangProj.sql'), \
					   os.path.join(strBldFldr, 'bin\\GetFWDBs.sql'), \
					   os.path.join(strBldFldr, 'Output\\SampleData'), \
					   os.path.join(strBldFldr, 'test\\testlangproj.xml'), \
					   os.path.join(strBldFldr, 'bin\\loadxml.exe'), \
					   rptObj.getLogFile(), strBldFldr, rptObj)
		except Exception, err:
			rptObj.reopenLog()
			rptObj.reportFailure('Failed to build TestLangProj database, ' + str(err), false)
		except:
			rptObj.reopenLog()
			rptObj.reportFailure('Failed to build TestLangProj database, ' + 'unknown error', false)

		# Build the NewLangProj database - no load xml
		rptObj.reportProgress('Building BlankLangProj...')
		rptObj.closeLog()
		try:
			db_BuildDB('BlankLangProj', None, strBldFldr \
					   + 'Output\\Common\\NewLangProj.sql', strBldFldr \
					   + 'bin\\GetFWDBs.sql', strBldFldr \
					   + 'Output\\Templates', None, None, rptObj.getLogFile(), strBldFldr, rptObj)
			db_BuildDB('BlankLangProj', None, \
					   os.path.join(strBldFldr, 'Output\\Common\\NewLangProj.sql'), \
					   os.path.join(strBldFldr, 'bin\\GetFWDBs.sql'), \
					   os.path.join(strBldFldr, 'Output\\Templates'), \
					   None, \
					   None, \
					   rptObj.getLogFile(), strBldFldr, rptObj)

		except Exception, err:
			rptObj.reopenLog()
			rptObj.reportFailure('Failed to build BlankLangProj database, ' + str(err), false)
		except:
			rptObj.reopenLog()
			rptObj.reportFailure('Failed to build BlankLangProj database, ' + 'unknown error', false)

		# Build the Tuwali database someday
		# Build the Greek database someday
		# reopen the log file
		rptObj.reopenLog()

	# Build release version
	rptObj.echoItVerbose('About to check for relbld')
	if not noreleasebld:
		bld_buildFW(strBldFldr, rptObj, 'Release', 'r')

	# Build bounds checker version
	rptObj.echoItVerbose('About to check for boundsbld')
	if not noboundsbld:
		bld_buildFW(strBldFldr, rptObj, 'Bounds Checker', 'b register regps')

		# TODO: Remove the following assignment when ready to start the tests again
		noautotest = true
		if not noautotest:
			# If everything built OK, run the tests using TrueCoverage
			# REVIEW: JeffG - Could make this somehow include all files found in a particular directory
			#	so this script would not need editing to add a new test
			rptObj.reportProgress('Running tests using TrueCoverage...')
			rptObj.closeLog()
			# Create the install output folders
			strArgStr = '/B /S ' + os.path.join(rptObj.getLogFile(), 'TrueCoverage.tcs') + ' ' \
						+ os.path.join(strBldFldr, 'bin\\runtest.exe overnite.tst')
			try:
				misc_RunExtProg('tcdev', strArgStr, 'errorLevel', rptObj, None)
			except:
				rptObj.reopenLog()
				rptObj.reportFailure('TrueCoverage tests failed.', false)
			rptObj.reopenLog()

	# If everything built passed the tests, copy everything to output
	# Copy the output of the build
	rptObj.reportProgress("Copying build output...")
	strDalyBldFldr = os.path.join(strOutputFldr, file_GetDalyBuildFolderName())
	file_DeleteFolder(strDalyBldFldr)
	file_MakeSureFolderExists(strDalyBldFldr)
	rptObj.echoItVerbose("Copying files from " + os.path.join(strBldFldr, 'Output') + " to " + strDalyBldFldr)
	os.system("xcopy " + os.path.join(strBldFldr, 'Output') + " " + strDalyBldFldr + ' /s/e/q/y')
	# Copy FWSource
	rptObj.reportProgress("Copying source code...")
	strSrcFldr = os.path.join(strDalyBldFldr, 'Code')
	file_DeleteFolder(strSrcFldr)
	file_MakeSureFolderExists(strSrcFldr)
	rptObj.echoItVerbose("Copying files from " + os.path.join(strBldFldr, 'src') + " to " \
						 + os.path.join(strDalyBldFldr, 'Code'))
	os.system("xcopy " + os.path.join(strBldFldr, 'src') + " " + os.path.join(strDalyBldFldr, 'Code') + ' /s/e/q/y')
	# Copying the results of automated testing
	# TODO: Remove the following assignment when we are running the tests
	noautotest = true
	if not noautotest:
		# Copy the test code
		rptObj.reportProgress("Copying test code...")
		if os.path.exists(os.path.join(strBldFldr, 'Test')):
			os.system("copy " + os.path.join(strBldFldr, 'Test') \
					  + " " + os.path.join(strDalyBldFldr, 'Code\\Test'))
		rptObj.reportProgress("Copying Bounds Checker File...")
		os.system("copy " + os.path.join(strBldFldr, 'TestLog\\Log\\BoundsChecker.bce') \
				  + " " + os.path.join(strDalyBldFldr, ""))
		os.system("copy " + os.path.join(strBldFldr, 'TestLog\\Log\\TrueCoverage.tsc') \
				  + " " + os.path.join(strDalyBldFldr, ""))

	# Copy the overnightbld.log file in the root directory.
	rptObj.echoItVerbose("Copying overnightbld.log file to " + os.path.join(strDalyBldFldr, ''))
	os.system("copy " + rptObj.getLogFile() + " " + os.path.join(strDalyBldFldr, ""))

	# Copy the *.mdf and *.ldf files from the Output\SampleData
	rptObj.reportProgress("Copying the Build Databases...")
	strSampleDataOutDir = os.path.join(strOutputFldr, "SampleData")
	rptObj.echoItVerbose("Deleting " + strSampleDataOutDir)
	file_DeleteFolder(strSampleDataOutDir)
	rptObj.echoItVerbose("Creating space " + strSampleDataOutDir)
	os.system("mkdir " + strSampleDataOutDir)
	strSampleDataSrcDir = os.path.join(strBldFldr, "Output\\SampleData")
	rptObj.echoItVerbose("Copying Databases from " + strSampleDataSrcDir + " to " \
						 + strSampleDataOutDir)
	os.system("copy " + os.path.join(strSampleDataSrcDir, "*.mdf ") \
			  + os.path.join(strSampleDataOutDir, ""))
	os.system("copy " + os.path.join(strSampleDataSrcDir, "*.ldf ") \
			  + os.path.join(strSampleDataOutDir, ""))

	# Copy the template files
	strTemplatesDataOutDir = os.path.join(strOutputFldr, "Templates")
	rptObj.echoItVerbose("Deleting " + strTemplatesDataOutDir)
	file_DeleteFolder(strTemplatesDataOutDir)
	rptObj.echoItVerbose("Creating " + strTemplatesDataOutDir)
	os.system("mkdir " + strTemplatesDataOutDir)
	strTemplatesDataSrcDir = os.path.join(strBldFldr, "Output\\Templates")
	rptObj.echoItVerbose("Copying Databases from " + strTemplatesDataSrcDir + " to " \
						 + strTemplatesDataOutDir)
	os.system("copy " + os.path.join(strTemplatesDataSrcDir, "*.mdf ") \
			  + os.path.join(strTemplatesDataOutDir, ""))
	os.system("copy " + os.path.join(strTemplatesDataSrcDir, "*.ldf ") \
			  + os.path.join(strTemplatesDataOutDir, ""))

	# Copy NewLangProj.xml from the Test directory
	strTestDir = os.path.join(strOutputFldr, "Test")
	rptObj.echoItVerbose("Copying XML file from " + strTestDir + " " + strTemplatesDataSrcDir)
	os.system("copy " + os.path.join(strTestDir, "NewLangProj.xml ") + os.path.join(strTemplatesDataOutDir, ""))

	# Create the install directories.  These directories are used by Install Builder to
	#	create the distribution CDROM
	rptObj.reportProgress("Creating install directories...")
	strDbgInstFldr = os.path.join(strDalyBldFldr, "Install_Debug")
	strRelInstFldr = os.path.join(strDalyBldFldr, "Install_Release")
	# The following needed just for WP
	strWPDbgInstFldr = os.path.join(strDalyBldFldr, "WPInstall_Debug")
	strWPRelInstFldr = os.path.join(strDalyBldFldr, "WPInstall_Release")

	try:
		file_DeleteFolder(strDbgInstFldr)
		os.system("mkdir " + strDbgInstFldr)
		file_DeleteFolder(strRelInstFldr)
		os.system("mkdir " + strRelInstFldr)
		file_DeleteFolder(strWPDbgInstFldr)
		os.system("mkdir " + strWPDbgInstFldr)
		file_DeleteFolder(strWPRelInstFldr)
		os.system("mkdir " + strWPRelInstFldr)
	except Exception, err:
		rptObj.reportFailure("Unable to create install folders " + str(err), true)
		rptObj.closeLog()
		raise

	# Copy files to be added to the root of the distributing CDROM
	rptObj.reportProgress("Copying files to be delievered to CDROM")
	strDelFileSrcDir = os.path.join(strBldFldr, "DelFiles")
	rptObj.echoItVerbose("Copying file from " + strDelFileSrcDir + " to " + strDbgInstFldr)
	os.system("copy " + os.path.join(strDelFileSrcDir, "*.*") \
			  + " " + os.path.join(strDbgInstFldr, ""))
	rptObj.echoItVerbose("Copying file from " + strDelFileSrcDir + " to " + strRelInstFldr)
	os.system("copy " + os.path.join(strDelFileSrcDir, "*.*") \
			  + " " + os.path.join(strRelInstFldr, ""))

	# Run the install builder program for the debug and release version
	rptObj.echoItVerbose("About to check noinstallbuilder")
	if not noinstallbldr:
		# TODO: JeffG - Make the install script use the data in daly output folder so installer can
		#	build from the daly copy of the source.
		# Build FW installer for the debug build version.
		bld_installer(strBldFldr, strDbgInstFldr, rptObj, "FWInstaller\\Fieldworks.wse", \
					  "debug", "FieldWorks")

		# Build FW installer for the release build version.
		bld_installer(strBldFldr, strRelInstFldr, rptObj, "FWInstaller\\Fieldworks.wse", \
					  "release", "FieldWorks")

		# Build WP installer for the debug build version.
		bld_installer(strBldFldr, strWPDbgInstFldr, rptObj, "WPInstaller\\worldpad.wse", \
					  "debug", "WorldPad")

		# Build WP installer for the debug build version.
		bld_installer(strBldFldr, strWPRelInstFldr, rptObj, "WPInstaller\\worldpad.wse", \
					  "release", "WorldPad")

		# Add calls to new windows installer function.
		if not nocreatedoc:
			rptObj.echoItVerbose('Skipping create objectweb')
			objweb_createObjWeb(strBldFldr, rptObj)
			objweb_copyObjWeb(strOutputFldr, "j:\\fieldworks\\objectweb\\")

		# Finish up
		# Reset FWROOT environment variable.
		os.environ['FWROOT'] = ""
		rptObj.reportProgress("Everything Built Correctly, exiting.")
		return
#-----------------------------------------------------------------------------------------------
try:
	print "Now in main.";

	Main();
except Exception, err:
	print str(err) + ': Could not finish the script.  Check for spelling errors'
except:
	print 'Unhandled exception, check for spelling errors'
#-----------------------------------------------------------------------------------------------