#----------------------------------------------------------------------------------------------
#   Copyright 2001, SIL International. All rights reserved.
#
#   File: file.py
#   Responsibility: JeffG
#   Last reviewed:
#
#   Description: File and Folder related functions.
#----------------------------------------------------------------------------------------------
#   Imported Modules
#----------------------------------------------------------------------------------------------
import time
import os
#----------------------------------------------------------------------------------------------
#   Function returns string FW_ plus the current date that will be used as a folder name.
#   TODO: JeffG - Add a parse method as well, return a Date Object.
#----------------------------------------------------------------------------------------------
def file_GetDalyBuildFolderName():
	strDir = "FW_"
	(year, month, day, hour, min, sec, wday, yday, isdst) = time.localtime()
	#TODO: JEFFG - make single digit month and days have leading zero.
	strDir = strDir + str(year)
	strDir = strDir + "-" + str(month)
	strDir = strDir + "-" + str(day)
	print strDir
	return strDir
#----------------------------------------------------------------------------------------------
#   Function makes the passed directory and its path if it does not already exist.
#----------------------------------------------------------------------------------------------
def file_MakeSureFolderExists(strDir):
	# See if the directory exists
	if not os.path.exists(strDir):
		aFolder = strDir.split("\\")
		strNewFolder = aFolder[0]
		iFolder = 1
		# Loop through each folder name and create if it doesn't exist
		while iFolder < len(aFolder):
			strNewFolder = strNewFolder + "\\" + aFolder[iFolder]
			iFolder = iFolder + 1
			if not os.path.exists(strNewFolder):
				os.system('mkdir ' + strNewFolder)
	return
#----------------------------------------------------------------------------------------------
#   Function deletes passed directory and all files and subfolders in it.
#----------------------------------------------------------------------------------------------
def file_DeleteFolder(strFolderName):
	remdir = "rmdir/s/q " + strFolderName
	if os.path.exists(strFolderName):
		os.system(remdir)
	return
#----------------------------------------------------------------------------------------------
#   Function deletes passed file.
#----------------------------------------------------------------------------------------------
def file_DeleteFile(strFileName):
	if(os.path.exists(strFileName) == 1):
		os.unlink(strFileName)
	return
#----------------------------------------------------------------------------------------------
