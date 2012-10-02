#-----------------------------------------------------------------------------------------------
#   Copyright 2001, SIL International. All rights reserved.
#
#   File: db_functions.py
#   Responsibility: JeffG
#   Last reviewed:
#
#	Functions related to database operations.
#
#-----------------------------------------------------------------------------------------------
#-----------------------------------------------------------------------------------------------
# Imported modules
#-----------------------------------------------------------------------------------------------
import re, os, os.path, _winreg, sys, file, misc, time
#-----------------------------------------------------------------------------------------------
#-----------------------------------------------------------------------------------------------
#	Start SQL Server
#
#	Parameters:
#		sqlSvrBinDir - the path to the directory that contains the SQL server executable files
#		rptObj - a Report object to log output to
#-----------------------------------------------------------------------------------------------
def db_startServer(sqlSvrBinDir, rptObj):

	# TODO:JEffG - this does not support sql server2K
	# Make sure the directory name string ends with a \ (backslash)
	sqlSvrBinDir = re.sub('\\\\*$', '\\\\', sqlSvrBinDir, 1)

	sqlSCMCommand = os.path.join(sqlSvrBinDir, 'scm.exe')
	rptObj.reopenLog()
	# Make sure SQLServer is started
	rptObj.echoItVerbose(sqlSCMCommand + ' -Action 3 -Silent 1')
	retValue = os.system('"' + sqlSCMCommand + '"' + ' -Action 3')
	print sqlSCMCommand + ' -Action 3'
	if retValue == -1:
		rptObj.reportProgress('Starting SQL Server...')
		try:
			misc.misc_RunExtProg(sqlSCMCommand, ' -Action 1', None, rptObj, None)

		# Handle exceptions of type "Exception"
		# "err" is a name for the Exception object
		except Exception, err:
			raise Exception, str(err) + ': Failed to load SQlServer'

		# Handle any other exceptions by simply raising them again
		except:
			raise



#
#TODO: JeffG - Surround all strDBName uses with quotes or brackets for names with spaces, Lela Teli.
#TODO: JeffG - Add Lela Teli build support.
#

#-----------------------------------------------------------------------------------------------
#	Detach the database
#
#	Parameters:
#		strOSQLCommand - SQL command to detach a database
#		strDBName - name of the database to detach
#		strOSQLStdParams - command line parameters to pass to the detaching command
#		rptObj - a Report object to log output to
#-----------------------------------------------------------------------------------------------
def db_detachDB(strOSQLCommand, strDBName, strOSQLStdParams, rptObj):
	rptObj.reopenLog()
	rptObj.echoItVerbose(strOSQLCommand + strOSQLStdParams + '-dmaster -Q"sp_detach_db ' \
	+ strDBName + '"')
	try:
		misc.misc_RunExtProg(strOSQLCommand, strOSQLStdParams + '-dmaster -n -Q"sp_detach_db ' \
		+ strDBName + '"', 'errorlevel', rptObj, None)
	except Exception, err:
		raise Exception, str(err) + ': Failed to detach newly built database'
	except:
		raise

#-----------------------------------------------------------------------------------------------
#	Delete a database
#
#	Parameters:
#		strOSQLCommand - SQL command to delete a database
#		strDBName - name of the database to delete
#		strOSQLStdParams - command line parameters to pass to the deleting command
#		rptObj - a Report object to log output to
#-----------------------------------------------------------------------------------------------
def db_deleteDB(strOSQLCommand, strDBName, strOSQLStdParams, rptObj):
	rptObj.reopenLog()
	rptObj.echoItVerbose(strOSQLCommand + strOSQLparams + ' -dmaster -n -Q"drop database ' \
	+ strDBName + '"')
	try:
		misc.misc_RunExtProg(strOSQLCommand, strOSQLStdParams + '-dmaster -n -Q"drop database ' \
		+ strDBName + '"', None, rptObj, None)
	except Exception, err:
		raise Exception, str(err) + ': Failed to delete database'
	except:
		raise

#-----------------------------------------------------------------------------------------------
#	Load a schema
#
#	Parameters:
#		strOSQLCommand - SQL command to load a new schema
#		strDBName - name of the database
#		strSQLFile - file containing database schema to load
#		strOSQLStdParams - command line parameters to pass to the loading command
#		strOSQLParam2 - additional command line parameters
#		rptObj - a Report object to log output to
#-----------------------------------------------------------------------------------------------
def db_loadSchema(strOSQLCommand, strDBName, strSQLFile, strOSQLStdParams, strOSQLParam2, \
rptObj):
	rptObj.reopenLog()
	rptObj.echoItVerbose(strOSQLCommand + strOSQLStdParams + '-d' + strDBName + ' -i ' \
	+ strSQLFile + strOSQLParam2)
	rptObj.reportProgress('Loading database schema from ' + strSQLFile + '...')
	try:
		misc.misc_RunExtProg(strOSQLCommand, strOSQLStdParams + '-d' + strDBName + ' -i ' \
		+ strSQLFile + strOSQLParam2, 'errorlevel', rptObj, None)
	except Exception, err:
		raise Exception, str(err) + ': Failed to load database schema'
	except:
		raise
	time.sleep(120)

#-----------------------------------------------------------------------------------------------
#	Create a new database
#
#	Parameters:
#		strOSQLCommand - SQL command to create a new database
#		strDBFile - name of the database file
#		strDBName - name of the database to detach
#		strOSQLStdParams - command line parameters to pass to the creating command
#		strOutputDir - directory where the database will reside
#		rptObj - a Report object to log output to
#-----------------------------------------------------------------------------------------------
def db_createDB(strOSQLCommand, strDBFile, strDBName, strOSQLStdParams, strOutputDir, rptObj):
	rptObj.reopenLog()

	# Make sure the directory name string ends with a \ (backslash)
	strOutputDir = re.sub('\\\\*$', '\\\\', strOutputDir, 1)
	# Create the database
	strDBOn = " ON (NAME='" + strDBFile + "',FILENAME='" + strOutputDir + strDBFile \
	+ ".mdf',SIZE=10MB,MAXSIZE=200MB,FILEGROWTH=5MB)"
	strDBLogOn = " LOG ON (NAME='" + strDBFile + "_Log', FILENAME='" + strOutputDir \
	+ strDBFile + "_Log.ldf',SIZE=5MB,MAXSIZE=25MB,FILEGROWTH=5MB)\""
	rptObj.echoItVerbose(strOSQLCommand + strOSQLStdParams \
	+ ' -dmaster -n -Q"Create database ' + strDBName + strDBOn + strDBLogOn)
	rptObj.reportProgress('Creating database ' + strDBName + ' and loading schema....')
	try:
		misc.misc_RunExtProg(strOSQLCommand, strOSQLStdParams + '-dmaster -n -Q"Create database ' \
		+ strDBName + strDBOn + strDBLogOn, 'errorlevel', rptObj, 0)
	except Exception, err:											#=dmaster
		raise Exc.eption, str(err) + ': Failed to create the database'
	except:
		raise
	time.sleep(60)

#-----------------------------------------------------------------------------------------------
#	Load an XML file
#
#	Parameters:
#		strLoadXMLCmd - SQL command to load XML data
#		strDBFile - name of the database file
#		strXMLfile - name of the XML data file
#		rptObj - a Report object to log output to
#-----------------------------------------------------------------------------------------------
def db_loadXML(strLoadXMLCmd, strDBFile, strXMLFile, rptObj):
	rptObj.reopenLog()
	rptObj.reportProgress('Loading data from ' + strXMLFile + '...')
	rptObj.echoItVerbose(strLoadXMLCmd + ' -d ' + strDBFile + ' -i ' + strXMLFile)
	try:
		misc.misc_RunExtProg(strLoadXMLCmd, '-d ' + strDBFile + ' -i ' + strXMLFile, 0, rptObj, 0)
	except Exception, err:
		raise Exception, str(err) + ': Failed to load XML file, see ' + strXMLFile \
		+ '-import.log'
	except:
		raise
	time.sleep(120)

#-----------------------------------------------------------------------------------------------
#	Detach the old database, create a new database, load schema, load XML (if required), then
#		detach the new database
#
#	Parameters:
#		strDBName - name of the database that will be detached and recreated
#		strDBFile - name of the database file
#		strSQLFile - file containing a database schema to load
#		strSQLFile2 - another file containing another database schema to load
#		strOutputDir - directory where the database will reside
#		strXMLFile - file containing XML data to load
#		strLoadXMLCmd - command to load the XML data
#		strLogFile - name of log file to (possibly) log data to
#		strFWRootDir - Fieldworks root directory
#		rptObj - a Report object to log output to
#-----------------------------------------------------------------------------------------------
def db_BuildDB(strDBName, strDBFile, strSQLFile, strSQLFile2, strOutputDir, strXMLFile, \
strLoadXMLCmd, strLogFile, strFWRootDir, rptObj):
	rptObj.reopenLog()
	rptObj.echoItVerbose('In db_BuildDB.')
	if not strDBName:
		raise Exception, 'Database name is a required parameter.'
	if not strSQLFile or not os.path.isfile(strSQLFile):
		raise Exception, 'SQL filename, ' + strSQLFile + ' was not found.'
	if not strOutputDir:
		raise Exception, 'Output directory is a required parameter.'
	if not strDBFile:
		strDBFile = strDBName

	# TODO:MartensK - figure out the correct path to the key in the registry - it probably is
	#	different for SQL Server 2000.  Maybe it is:
	# HKEY_LOCAL_MACHINE\Software\Microsoft\Microsoft SQL Server\80\Tools\ClientSetup\SQLPath
	key = _winreg.OpenKey(_winreg.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\MSSQLServer\\Setup")
	strSvrToolsDir = _winreg.QueryValueEx(key, "SQLPath")[0]
	# This is hard coded due to DOS command error with both path and parameters in quotes
	strSvrToolsDir = 'c:\\progra~1\\mssql7\\'
	# Make sure the directory name string ends with a \ (backslash)
	strSvrToolsDir = re.sub('\\\\*$', '\\\\', strSvrToolsDir, 1)
	strSvrBinDir = os.path.join(strSvrToolsDir, 'Binn')
	strOSQLCommand = os.path.join(strSvrBinDir, 'osql.exe')
	strOSQLStdParams = '-S. -Usa -P '

	file.file_MakeSureFolderExists(strOutputDir)
	rptObj.closeLog()
	db_detachDB(strOSQLCommand, strDBName, strOSQLStdParams, rptObj)
	db_createDB(strOSQLCommand, strDBFile, strDBName, strOSQLStdParams, strOutputDir, rptObj)
	#db_loadSchema(strOSQLCommand, strDBName, strSQLFile, strOSQLStdParams, '-a 8192 -n', rptObj)
	# Osql.exe says -a 8192 is not an option. In any case, it is already the default size used by NT.
	db_loadSchema(strOSQLCommand, " " + strDBName, strSQLFile, strOSQLStdParams, ' -n', rptObj)
	db_loadSchema(strOSQLCommand, 'master', strSQLFile2, strOSQLStdParams, '', rptObj)
	if strXMLFile:
		db_loadXML(strLoadXMLCmd, strDBName, strXMLFile, rptObj)
	db_detachDB(strOSQLCommand, strDBName, strOSQLStdParams, rptObj)
	return
#----------------------------------------------------------------------------------------------