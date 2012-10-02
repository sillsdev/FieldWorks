#-----------------------------------------------------------------------------------------------
#   Copyright 2001, SIL International. All rights reserved.
#
#   File: misc.py
#   Responsibility: JeffG
#   Last reviewed:
#
#
#
#-----------------------------------------------------------------------------------------------
#-----------------------------------------------------------------------------------------------
# Imported modules
#-----------------------------------------------------------------------------------------------
import os
#-----------------------------------------------------------------------------------------------
#	Reporting Class
#	Hungarian: rpt
#-----------------------------------------------------------------------------------------------
class Report:

#-----------------------------------------------------------------------------------------------
#	Initialize a Report object with a file to log messages to
#
#	Parameters:
#		filename - the name of the text file to log messages to
#-----------------------------------------------------------------------------------------------
	def __init__(this, filename):
		# Member variables beginning with __ are private (for the most part, anyway)
		this.__filename = filename
		try:
			this.__outFile = open(this.__filename,'w')
		except Exception, err:
			print 'Error opening log file: ' + this.__filename
			raise Exception, str(err) + ': Error opening log file: ' + this.__filename
		except:
			print 'Error opening log file: ' + this.__filename
			raise
		this.__fVerboseMode = 0
		this.__fLogIt = 1

#-----------------------------------------------------------------------------------------------
#	Write a message to the log file
#
#	Parameters:
#		strMessage - the message to log to the file
#-----------------------------------------------------------------------------------------------
	def __logIt(this, strMessage):
		try:
			if this.__fLogIt and not this.__outFile.closed:
				this.__outFile.write('\n' + strMessage)
		except Exception, err:
			print 'Error writing to log file: ' + this.__filename
			raise Exception, str(err) + ': Error writing to log file: ' + this.__filename
		except:
			print 'Error writing to log file: ' + this.__filename
			raise

#-----------------------------------------------------------------------------------------------
#	Write a message to standard output, then call __logIt() to log the message to the log file
#
#	Parameters:
#		strMessage - the message to display and log to the file
#-----------------------------------------------------------------------------------------------
	def echoIt(this, strMessage):
		#print '\n' + strMessage
		print strMessage
		this.__logIt(strMessage)

#-----------------------------------------------------------------------------------------------
#	If verbose mode has been enabled, call echoIt() to display and log the message
#
#	Parameters:
#		strMessage - the message to display and log to the file (if verbose mode is enabled)
#-----------------------------------------------------------------------------------------------
	def echoItVerbose(this, strMessage):
		if this.__fVerboseMode:
			this.echoIt(strMessage)

#-----------------------------------------------------------------------------------------------
#	Write a line of asterisks, a message, then another line of asterisks to the log file
#	(Calls __logIt() to do the actual writing to the file)
#
#	Parameters:
#		strMessage - the message to write to the file
#-----------------------------------------------------------------------------------------------
	def logBanner(this, strMessage):
		this.__logIt('\n****************************************' \
		'****************************************')
		this.__logIt(strMessage)
		this.__logIt('****************************************' \
		'****************************************\n')

#-----------------------------------------------------------------------------------------------
#	Output a message to standard output, and write the same message to the log file using
#		__logBanner()
#
#	Parameters:
#		strMessage - the message to display and log to the file
#-----------------------------------------------------------------------------------------------
	def reportProgress(this, strMessage):
		fLogIt = this.__fLogIt
		this.__fLogIt = 0
		this.echoIt('<< ' + strMessage + ' >>')
		this.__fLogIt = fLogIt
		this.logBanner(strMessage)

#-----------------------------------------------------------------------------------------------
#	Write an error message to standard output, and log the message to the log file
#
#	Parameters:
#		strMessage - the message to display and log to the file
#		isFatal - boolean value indicating whether the failure is fatal
#-----------------------------------------------------------------------------------------------
	def reportFailure(this, strMessage, isFatal):
		# Force logging to file
		this.__fLogIt = 1;
		if isFatal:
			this.reportProgress('ERROR: ' + strMessage + ' - Examine overnitebld.log - ' \
			+ 'Exiting Script')
		else:
			this.reportProgress('ERROR: ' + strMessage + ' - Examine overnitebld.log')

#-----------------------------------------------------------------------------------------------
#	Use dosmail2.exe to send an email message
#
#	Parameters:
#		strFrom - address the email should be sent from
#		strTo - address the email should be sent to (e.g., lsadmin@lsdev.sil.org)
#		strHostName - should be host name of To address (e.g., lsdev.sil.org)
#		strSubject - subject line of the email message
#		strMessage - the email message
#-----------------------------------------------------------------------------------------------
	def emailList(this, strFrom, strTo, strHostName, strSubject, strMessage):
		if strFrom == None:
			strFrom = 'fwbuilder@lsdev.sil.org'
		if strTo == None:
			strTo = 'lsadmin@lsdev.sil.org'
		if strHostName == None:
			strHostName = 'lsdev.sil.org'
		if strSubject == None:
			strSubject = 'Build Failure'
		if strMessage == None:
			strMessage = 'There was build failure.  Please check the log file.'

		os.system('dosmail2 -f' + strFrom + ' -t' + strTo + ' -h' + strHostName + ' -s"' \
		+ strSubject + '" "' + strMessage + '"')

#-----------------------------------------------------------------------------------------------
#	Close the log file, raises an exception if there was an error
#-----------------------------------------------------------------------------------------------
	def closeLog(this):
		try:
			this.__outFile.close()
		except Exception, err:
			print 'Could not close log file: ' + this.__filename
			raise Exception, str(err) + ': Could not close log file: ' + this.__filename
		except:
			print 'Could not close log file: ' + this.__filename
			raise

#-----------------------------------------------------------------------------------------------
#	Opens the log file, raises an exception if there was an error
#-----------------------------------------------------------------------------------------------
	def reopenLog(this):
		try:
			this.__outFile = open(this.__filename, 'a')
		except Exception, err:
			print 'Could not reopen log file: ' + this.__filename
			raise Exception, str(err) + ': Could not reopen log file: ' + this.__filename
		except:
			print 'Could not reopen log file: ' + this.__filename
			raise

#-----------------------------------------------------------------------------------------------
#	Turns verbose mode on by setting __fVerboseMode to true
#-----------------------------------------------------------------------------------------------
	def verboseModeOn(this):
		this.__fVerboseMode = 1

#-----------------------------------------------------------------------------------------------
#	Turns verbose mode off by setting __fVerboseMode to false
#-----------------------------------------------------------------------------------------------
	def verboseModeOff(this):
		this.__fVerboseMode = 0

#-----------------------------------------------------------------------------------------------
#	Returns the name of the log file
#-----------------------------------------------------------------------------------------------
	def getLogFile(this):
		return this.__filename

#-----------------------------------------------------------------------------------------------
#	Closes the log file, then opens a new log file with a different name
#
#	Parameters:
#		filename - the name of the new log file
#-----------------------------------------------------------------------------------------------
	def setLogFile(this, filename):
		this.closeLog()
		this.__filename = filename
		this.reopenLog()