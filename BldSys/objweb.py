#----------------------------------------------------------------------------------------------
#   Copyright 2001, SIL International. All rights reserved.
#
#   File: objweb.py
#   Responsibility: JeffG
#   Last reviewed:
#
#   Description: Functions related to process idl files for generating Object Web documentation.
#----------------------------------------------------------------------------------------------
#   Imported Modules
#----------------------------------------------------------------------------------------------
import os, re, file, misc
from Report import *
#----------------------------------------------------------------------------------------------
#   Function to convert an IDH file to a dummy C++ file.
#   Arguments:
#		name: the file name, and any path or extension
#		srcpath: prefix, from . to the directory containing the idh file
#		outpath: prefix, where the newly created files go.
#   Function creates a series of regular expressions and checks file strName.idh
#   against these expressions.  When matches, a message is written to strName.idh_.
#----------------------------------------------------------------------------------------------
def objweb_ConvertIdh(strName, strSrcPath, strOutputPath):
	strFileName = strName + ".idh"
	strFileNameOut = strOutputPath + "\\" + strName + ".idh_"
	if(os.path.exists(strFileName)):
		ts = open(strFileName)
	else:
		raise Exception, "Cannot open file " + sFileName

	tsout = open(strFileNameOut, 'w')
	# Regular Expressions
	reGet = re.compile("\\[propget\\] HRESULT ")
	rePut = re.compile("\\[propput\\] HRESULT ")
	rePutref = re.compile("\\[propputref\\] HRESULT ")
	# Match a "Declare Interface" declaration. $1 is the declared name,
	# $2 is the interface it inherits from.
	reDeclareIntf = re.compile("DeclareInterface\\((\\w+), *(\\w+),.+\\)")
	# Match DeclareDualInterface(2). $1 is "2" or nothing, $2 is the interface name
	reDeclareDual = re.compile("DeclareDualInterface(2?)\\((\\w+),.+\\)")
	# Match "Interface SomeInterface" on a line by itself (typically comment block header)
	reInterface = re.compile("^\\s*Interface I\\w+\\s*$")
	# Match any argument specifier
	reInOut = re.compile("\\[.*\\]")

	strList = ts.readlines()
	istrList = 0
	while istrList < len(strList):
		str = strList[istrList]
		# Look for DeclareInterface type strings
		# If one of these matches, since it can't be immediately follwed
		# by another, we can just continue the loop
		splstr = reDeclareIntf.split(str, reDeclareIntf)
		arr = reDeclareIntf.search(str)
		if(arr != None):
			str = splstr[0] + "class I" + arr.group(1) + ": public I" + arr.group(2)
			tsout.write(str)
			tsout.write("\n")
			istrList = istrList + 1
			str = strList[istrList]
			tsout.write(str)        # typically opening braces
			tsout.write("\tpublic:\n")
			istrList = istrList + 1
			str = strList[istrList]
			continue

		arr = reDeclareDual.search(str)
		splstr = reDeclareDual.split(str, reDeclareDual)
		if(arr != None):
			prefix = "I"
			if (arr.group(1) != "2"):
				prefix = "DI"       # Regular DualInterface declares DI...
			str = splstr[0] + "class " + prefix + arr.group(2) + ": public IDispatch"
			tsout.write(str)
			istrList = istrList + 1
			str = strList[istrList]
			tsout.write(str)
			tsout.write("\tpublic:")
			istrList = istrList + 1
			str = strList[istrList]
			continue

		# Our block comments for DeclareInterface sually start "Interface IX" but
		# we don't want this in the input to Surveyor which makes its own overall header
		# The below three lines are commented out because they cause an endless loop on Interface I in FWkernel.idh
		#arr = reInterface.search(str)
		#if(arr != None):
		#    continue
		# Change "[propget] HRESULT MyFunc" to "HRESULT get_MyFunc", etc
		str = reGet.sub("HRESULT get_", str)
		str = rePut.sub("HRESULT put_", str)
		str = rePutref.sub("HRESULT putref_", str)
		# Remove argument specifiers, also things like [v1_enum].
		str = reInOut.sub("", str)
		tsout.write(str)
		istrList = istrList + 1
		#The following was commented out in original:
		#sOutput = str
		#arr = reInOut.search(str)
		#splstr = reInOut.split(str, reInOut)
		#if (arr != None):
		#    sOutput = splstr[0] + "HRESULT get_" + splstr[1]
		#    tsout.write(sOutput)
		continue

	ts.close()
	tsout.close()
	return
#----------------------------------------------------------------------------------------------
#	Function to process all interface files (*.idh).
#		(a) recursively process subfolders;
#		(b) call ConvertIdh for every idh file in the folder
#----------------------------------------------------------------------------------------------
def objweb_ProcessInterfaceFiles(folder, strSrcPath, strOutputPath):
	strRoot = os.getcwd()
	reIDH = re.compile("\.(idh|IDH|Idh)$", 0)
	if(os.path.exists(strSrcPath)):
		dirList = os.listdir(strSrcPath)
	else:
		raise Exception, "Cannot find path " + strSrcPath

	if(os.path.isdir(strSrcPath)):
		os.chdir(strSrcPath)
		subdir = 0
		for subdir in dirList:
			if (os.path.isdir(subdir)):
				objweb_ProcessInterfaceFiles(folder, subdir, strOutputPath)
			else:
				arr = reIDH.search(subdir)
				if (arr != None):
					file = reIDH.split(subdir, ".")
					print "Converting " + file[0] + " in " + strSrcPath
					objweb_ConvertIdh(file[0], strSrcPath, strOutputPath)
		os.chdir(strRoot)
	else:
		raise Exception, "Cannot find path " + strSrcPath
	return
#----------------------------------------------------------------------------------------------
#   Function to run GtorSur program to build object web..
#   Function creates directory src and src/interfaces; checks that directories
#   were made; and calls functions objweb_ProcessInterfaceFiles misc_RunExtProg.
#----------------------------------------------------------------------------------------------
def objweb_createObjWeb(strBldFldr, rptObj):
	strRoot = os.getcwd()
	if(os.path.exists(strBldFldr)):
		os.chdir(strBldFldr)
	else:
		raise Exception, "Cannot find path " + strBldFldr

	# Process all idh files to produce pseudo *.idh_
	os.system("mkdir src")
	os.chdir('src')
	strSrcRoot = os.getcwd()
	os.system("mkdir interfaces")
	os.chdir('interfaces')
	strOutputFldr = os.getcwd()
	os.chdir(strSrcRoot)
	file.file_MakeSureFolderExists(strOutputFldr)
	os.chdir(strRoot)
	objweb_ProcessInterfaceFiles('src', strSrcRoot, strOutputFldr)
	# Run Surveyor Program
	strSurveyorCommand = "C:\\Progra~1\\Surveyor\\System\\GtorSur.exe"
	try:
		rptObj.echoItVerbose(strSurveyorCommand + " [AutoWeb(\"" + strSrcRoot + "\")] [Quit]")
		misc.misc_RunExtProg(strSurveyorCommand, "[AutoWeb(" + strSrcRoot + ")] [Quit]", None, rptObj, None)
	except Exception, err:
		raise Exception, str(err)
	return
#----------------------------------------------------------------------------------------------
#   Function to copy object web file to server.
#   Function removes everything in strObjectWebDestPath directory;
#   Creates ObjectWeb directory in strOutputFldr and copies everything
#   from that folder to strObjectWebDestPath and then deletes ObjectWeb directory.
#----------------------------------------------------------------------------------------------
def objweb_copyObjWeb(strOutputFldr, strObjectWebDestPath):
	strRoot = os.getcwd()
	# Move the output from Surveyor program to the website
	# Delete the current stuff on the web share
	if(os.path.exists(strObjectWebDestPath)):
		rmDestDir = "rmdir/s/q " + strObjectWebDestPath
		os.system(rmDestDir)
		os.system('mkdir ' + strObjectWebDestPath)
	else:
		raise Exception, "Cannot find path " + strObjectWebDestPath

	if(os.path.exists(strOutputFldr)):
		os.chdir(strOutputFldr)
		os.system('mkdir ObjectWeb')
	else:
		raise Exception, "Cannot find path " + strOutputFldr

	# Copy new files and delete originials
	strObjectWebSrcPath = os.path.join(strOutputFldr, "ObjectWeb")
	os.chdir(strRoot)
	os.system("xcopy " + strObjectWebSrcPath + " " + strObjectWebDestPath + '/s/e/y')

	# Copy to contents of the Doc dir,
	# REVIEW: JeffG - we don't want to copy this here now
	# Some will go into the remote site
	#os.system('mkdir Doc')
	#os.system('xcopy ' + strBldFldr + ' Doc')
	# Delete the originals
	os.system("rmdir/s/q " + strObjectWebSrcPath)
	return
#----------------------------------------------------------------------------------------------
