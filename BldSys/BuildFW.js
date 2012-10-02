/*----------------------------------------------------------------------------------------------
Copyright 2001, SIL International. All rights reserved.

File: buildfw.js
Responsibility: Jeff Gayle
Last reviewed: Not yet.

Description: Script for Overnight Builds of FieldWorks

----------------------------------------------------------------------------------------------*/
// TODO: Add a few beeps on error and one beep when done. Play wav files, need sound card/spkrs.
// TODO: Add more try catch blocks around unprotected code.
// TODO: Do a better job partitioning the build options, there is some interaction.
// TODO: Add constants for magic numbers for file open and vss.
// TODO: Test build level and vss label get latest.

// TODO: BIG TODO: - Make the script set the env var FWROOT via WSH, not just command shell.


/*******************************************************************
REVIEW: Always check for bad variable names, typos, etc. They will
cause runtime execptions.
*********************************************************************/
/*	<script language="JScript" src="globconst.js" />
	<script language="JScript" src="misc.js" />
	<script language="JScript" src="rptobj.js" />
	<script language="JScript" src="file.js" />
	<script language="JScript" src="objweb.js" />
	<script language="JScript" src="db.js" />
	<script language="JScript" src="sccs.js" />
	<script language="JScript" src="bld.js" />
	<script language="JScript" src="mail.js" />
*/

// Global Variables
var debug = false;	// If true, verbose progress messages will be emitted.

// Folder/File Consts
var kForRead = 1;
var kForWrite = 2;
var kForAppend = 8;
var kFormatASCII = 0;
var kFormatUnicode = -1;
var kFormatSystemDefault = -2;
var kCreateFile = true;
var kNoCreateFile = false;

// Windows Type Consts
var kappWtHide = 0
var kappWtShowNormal = 1
var kappWtShowMin = 2
var kappWtShowMax = 3
var kappWtShowNoActive = 4
var kappWtMinNoActive = 6

// Special Folders Consts
var ksfWindows = 0;
var ksfWindowsSystem = 1;
var ksfTemp = 2;

//
// Misc functions
//
/*----------------------------------------------------------------------------------------------
	Recursive call back to the original script, a way to change the current active folder.
	From Tom Lavedas <lavedas@pressroom.com>
----------------------------------------------------------------------------------------------*/
/*
function misc_ChdDir(strTargetDir)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	if (fso.GetFolder(".").Path.toUpperCase() != strTargetDir.toUpperCase())
	{
		var shellObj = new ActiveXObject("WScript.Shell");
		var strTempBatName;

		// Create temporary batch file.
		strTempBatName = shellObj.Environment("PROCESS").Item("TEMP") +
			"\~" + fso.GetTempName + ".bat";

		// Open batch file and write commands to it.
		var tsoLogFile = fso.OpenTextFile(strTempBatName, kForWrite, kCreateFile);
		tsoLogFile.WriteLine("cd " + strTargetDir);
		tsoLogFile.Writeline("cscript " + Wscript.ScriptFullName);
		tsoLogFile.Write("del " + strTempBatName);
		tsoLogFile.Close();

		// Exectute temporary batch file.
		shellObj.Run("%comspec% /c " + strTempBatName, 0, True);
		Wscript.Quit();
	}

	// The current active folder is now TargetFolder
	// Add your code here ...
}
*/
/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function misc_GetScriptDir()
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	return (fso.GetParentFolderName(WScript.ScriptFullName));
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function misc_GetCurDir()
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	return(fso.GetFolder(".").Path);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function misc_RunExtProg(strProg, strArgs, strEnvErr, rptObj, strLogFile, bfWaitOnReturn)
{
	rptObj.echoItVerbose("In RunExtProg.");
	var shellObj = new ActiveXObject("WScript.Shell");
	var fso = new ActiveXObject("Scripting.FileSystemObject");
/*
	// Can't use the following because it does not handle string with quotes like
	// "c:\\Program Files\\Wise Installbuilder 8.1\\"
	if (fso.FileExists(strProg) == false)
	{
		var err = new Error;
		err.number = 1;
		err.description += "Cannot find program " + strProg;
		throw(err);
	}

*/
	// Surround the file name and path with double quotes so this won't fail.
	var strQuotedProg;
	strQuotedProg = "\"" + strProg + "\"";
	if (strLogFile && strLogFile.length > 0)
	{
		rptObj.echoItVerbose("Run: " + strQuotedProg + " " + strArgs + " >>" + strLogFile);
		var retValue = shellObj.Run(strQuotedProg + " " + strArgs + " >>" + strLogFile, 0, bfWaitOnReturn)
	}
	else
	{
		rptObj.echoItVerbose("Run: " + strQuotedProg + " " + strArgs);
		var retValue = shellObj.Run(strQuotedProg + " " + strArgs, 0, bfWaitOnReturn);
	}

	// Note: This doesn't work correctly following a batch file because environment variables set in the
	// batch file go away before we get here.
	if (strEnvErr)
	{
		var envVar;
		// Try to find the environment variable in each of the areas.
		if (envVar = shellObj.Environment("Process").Item(strEnvErr))
			retValue=envVar;
		else if (envVar = shellObj.Environment("User").Item(strEnvErr))
			retValue=envVar;
		else if (envVar = shellObj.Environment("System").Item(strEnvErr))
			retValue=envVar;
	}

	rptObj.echoItVerbose("RunExtProg:External program returned " + retValue);
	if (retValue != 0)
	{
		var err = new Error;
		err.number = retValue;
		err.description = "Command " + strProg + " failed. ";
		throw(err);
	}
}

/*----------------------------------------------------------------------------------------------
	Pause execution for specified amount of time.
	TODO:JeffG - Add to the python equivalent.
----------------------------------------------------------------------------------------------*/
function misc_Sleep(iMin, rptObj)
{
	if (rptObj)
		rptObj.reportProgress("Sleeping " + iMin + " minutes...");

	WScript.Sleep(iMin * 60000);
}

/*----------------------------------------------------------------------------------------------
	Uses FTP to upload to the SIL FTP site.
----------------------------------------------------------------------------------------------*/
function misc_UploadSetup(strLocalDir, strRemoteDir, strUploadFile, rptObj)
{
	// Create some commonly used objects.
	var fso = new ActiveXObject("Scripting.FileSystemObject");

	// Check that the file exists
	if (!fso.FileExists(fso.BuildPath(strLocalDir, strUploadFile)))
	{
		var err = new Error;
		err.number = 1;
		err.description = "Unable to open " + fso.BuildPath(strLocalDir, strUploadFile);
		throw(err);
	}

	// Create new ftp command file in temp folder
	var strTempFile = fso.BuildPath(fso.GetSpecialFolder(ksfTemp), fso.GetTempName());
	var tsoTempFile = fso.OpenTextFile(strTempFile, kForWrite, kCreateFile);
	rptObj.echoItVerbose("TmpFile Name:" + strTempFile);

	// Write commands to file
	tsoTempFile.WriteLine("open gamma.sil.org");
	tsoTempFile.WriteLine("user Soft_Admin Cortez");
	tsoTempFile.WriteLine("binary");
	tsoTempFile.WriteLine("lcd " + strLocalDir);
	tsoTempFile.WriteLine("cd " + strRemoteDir);
	tsoTempFile.WriteLine("delete " + strUploadFile);
	tsoTempFile.WriteLine("put " + strUploadFile);
	tsoTempFile.WriteLine("bye");

	// Close file
	tsoTempFile.Close();

	// Execute ftp with command file
	misc_RunExtProg("ftp", "-n -i -s:" + strTempFile, null, rptObj, null, true);

	// Delete temporary command file
	file_DeleteFile(strTempFile);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function misc_DisplayUsage(rptObj)
{
	rptObj.closeLog(); // forget logging the usage message

	rptObj.echoIt("Usage: bldfw fw_source_root output_root [options]");
	rptObj.echoIt("Options:");
	rptObj.echoIt("\t-bbuildlevel, [0-8]");
	rptObj.echoIt("\t-lversion label");
	rptObj.echoIt("\t-ooutput folder override option");
	rptObj.echoIt("\t-dbuild option");
	rptObj.echoIt("Recognized build options :");
	rptObj.echoIt("\tnodebugbld");
	rptObj.echoIt("\tnoreleasebld");
	rptObj.echoIt("\tnoboundsbld");
// TODO: JeffG - create no builds option		rptObj.echoIt("\tnobuilds");
	rptObj.echoIt("\tnoautotest");
	rptObj.echoIt("\tnorefreshsrc");
	rptObj.echoIt("\tnocreatedbs");
	rptObj.echoIt("\tnoinstallbldr");
	rptObj.echoIt("\tdebug");
	rptObj.echoIt("\tnoftpupload");
	rptObj.echoIt("\tnocreatedoc");
	rptObj.echoIt("\tnocopyinst");
	rptObj.echoIt("\tnodeloutput - do not delete output trees.");
	rptObj.echoIt("\ttestrefresh - only run refresh source section");
	rptObj.echoIt("\ttestdoc - only run doc create section");
	rptObj.echoIt("\ttestdb - only run create database section");
	rptObj.echoIt("\ttestinst - only run install builder section");
	rptObj.echoIt("\ttestinstcopy - only copy install builds section");

	WScript.Quit();
}


//
//	Report Functions
//
/*----------------------------------------------------------------------------------------------
//
//	Report Constructor function.
//
----------------------------------------------------------------------------------------------*/
function Report(strLogFileName)
{
	// Folder/File Consts
	var kForWrite = 2;
	var kCreateFile = true;
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	var shellObj = new ActiveXObject("WScript.Shell");

	this.fso = fso;
	this.shellObj = shellObj;
	this.strLogFileName = strLogFileName;
	this.tsoLogFile = fso.OpenTextFile(this.strLogFileName, kForWrite, kCreateFile);
	this.fVerboseMode = false;
	this.fLogIt = true;
	this.sentToList = new Array();
	this.mailList = null;// TODO:JeffG - make this work.
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
Report.prototype.logIt = function (strMsg)
{
	if (this.fLogIt && this.tsoLogFile)
		this.tsoLogFile.WriteLine(strMsg);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
Report.prototype.echoIt = function (strMsg)
{
	WScript.Echo(strMsg);
	this.logIt(strMsg);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
Report.prototype.echoItVerbose = function (strMsg)
{
	if (this.fVerboseMode == true)
		this.echoIt(strMsg);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
Report.prototype.logBanner = function (strMsg)
{
	this.logIt("\n*******************************************************************************");
	this.logIt(strMsg);
	this.logIt("*******************************************************************************\n");
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
Report.prototype.reportProgress = function (strMsg)
{
	var now = new Date();
	var strTime = now.getHours() + ":" + now.getMinutes() + ":" + now.getSeconds();
	var strDate = (now.getMonth() + 1) + "/" + now.getDate() + "/" + now.getYear();

	// store off the status of flogIt and force it to not log
	var fLogItT = this.fLogIt;
	this.fLogIt = false;
	// output the status to the screen
	this.echoIt("<< " + strMsg + "(" + strTime + ") >>", null);
	// output to the log file
	this.fLogIt = fLogItT;
	this.logBanner(strMsg+ "\t(" + strTime + ")");
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
Report.prototype.emailList = function (strMsg)
{

	// Assumes dosmail.exe is somewhere in the path.
	var i;
	for (i = 0; i < this.sentToList.Length; i++)
	{
		// TODO: JeffG - get dosmail moved into the path or add \bin dir to path.
		//  shellObj.Run(binDir + "\\dosmail.exe",
		// "ls-muttley@sil.org " + strArrayEmailList[i] + strMsg, null, null);
		this.reportProgress("Sending mail to: " + this.sentToList[i] + ", msg = " + strMsg);
	}
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
Report.prototype.closeLog = function ()
{
	if (this.tsoLogFile)
	{
		this.tsoLogFile.Close();
		this.tsoLogFile = null;
	}
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
Report.prototype.reopenLog = function ()
{
	// Folder/File Consts
	var kForWrite = 2;
	var kForAppend = 8;

	if (!this.tsoLogFile)
		this.tsoLogFile = this.fso.OpenTextFile(this.strLogFileName, kForAppend);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
Report.prototype.reportFailure = function (strMsg,fAbortScript)
{
	this.fLogIt = true; // force the loging ofthis message.
	if (fAbortScript)
		this.reportProgress("ERROR: " + strMsg +
			" - Examine overnite.bld - Exiting Script");
	else
		this.reportProgress("ERROR:" + strMsg +
			" - Examine overnite.bld.");

	// TODO:JeffG - make this work.
	//if (this.mailList)
	//	this.mailList(strMsg);

	if (fAbortScript == true && this.tsoLogFile)
		this.tsoLogFile.Close();
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
Report.prototype.appendStreamToLogFile = function (tso)
{
	if (tso && this.tsoLogFile)
		while(!tso.AtEndOfStream)
			this.tsoLogFile.WriteLine(tso.ReadLine());
}

//
//	File/Folder I/O
//
/*----------------------------------------------------------------------------------------------

// TODO: JeffG - Add a parse method as well, return a Date Object.
----------------------------------------------------------------------------------------------*/
function file_GetDailyBuildFolderName()
{
	var strDir = "FW_";

	var today = new Date();
	var month = today.getMonth() + 1;
	var day = today.getDate();

	//FW_%4d-%02d-%02d
	strDir += today.getFullYear();
	if (month < 10)
	{
		strDir += "-0" + month;
	}
	else
	{
		strDir += "-" + month;
	}
	if (day < 10)
	{
		strDir += "-0" + day;
	}
	else
	{
		strDir += "-" + day;
	}
	return(strDir);
}

/*----------------------------------------------------------------------------------------------
	Note - Does not handle \\LS-ELMER\ type directory creation.
----------------------------------------------------------------------------------------------*/
function file_MakeSureFolderExists(strDir)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");

	// See if the dir exists.
	if (!fso.FolderExists(strDir))
	{
		var aFolder = new Array();
		aFolder = strDir.split("\\");
		var strNewFolder = fso.BuildPath(aFolder[0], "\\");

		// Loop through each folder name and create if not already created
		var	iFolder;
		for (iFolder = 1; iFolder < aFolder.length; iFolder++)
		{
			strNewFolder = fso.BuildPath(strNewFolder, aFolder[iFolder]);
			if (!fso.FolderExists(strNewFolder))
				fso.CreateFolder(strNewFolder);
		}
	}
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function file_DeleteFolder(strFolderName)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	if (fso.FolderExists(strFolderName))
		fso.DeleteFolder(strFolderName, true);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function file_DeleteFile(strFileName)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	if (fso.FileExists(strFileName))
		fso.DeleteFile(strFileName);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function file_CreateFolder(strFolderName)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	file_DeleteFolder(strFolderName);
	fso.CreateFolder(strFolderName);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function file_ForceRefresh(strFileName, strSourceFolder, strDestFolder)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");

	var strSourceFile = fso.BuildPath(strSourceFolder, "\\" + strFileName);
	var strDestFile = fso.BuildPath(strDestFolder, "\\" + strFileName);

	if (fso.FileExists(strDestFile))
		fso.DeleteFile(strDestFile, true);

	fso.CopyFile(strSourceFile, strDestFolder + "\\");
}

/*----------------------------------------------------------------------------------------------
	Moves contents of source folder to dest folder, deleting contents first
----------------------------------------------------------------------------------------------*/
function file_RefreshFldr(strSrcFldr, strDestFldr)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	try
	{
		// Delete the current stuff	on the web share.
		fso.DeleteFile(fso.BuildPath(strDestFldr, "*.*"));

		// Copy the new files
		fso.CopyFile(fso.BuildPath(strSrcFldr, "*.*"), strDestFldr, true);
		fso.CopyFolder(fso.BuildPath(strSrcFldr, "*.*"), strDestFldr, true);
	} catch (err)
	{
		// Do nothing, files and folders do not exist
	}
}



//
//	Automated Documention Functions
//
/*----------------------------------------------------------------------------------------------
	Function to convert an IDH file to a dummy C++ file.
	Arguments:
		name: the file name, sans any path or extension
		srcpath: prefix, from . to the directory containing the idh file
		outpath: prefix, where the newly created files go.
----------------------------------------------------------------------------------------------*/
function objweb_ConvertIdh(strName, strSrcPath, strOutputPath)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");

	var sFileName = fso.BuildPath(strSrcPath, strName + ".idh");
	var sFileNameOut = fso.BuildPath(strOutputPath, strName + ".idh_");

	var ts = fso.OpenTextFile(sFileName, kForRead, kNoCreateFile, kFormatASCII);
	var tsout = fso.CreateTextFile(sFileNameOut, true);

	var reGet = new RegExp("\\[propget\\] HRESULT ","");
	var rePut = new RegExp("\\[propput\\] HRESULT ","");
	var rePutref = new RegExp("\\[propputref\\] HRESULT ","");
	// Match a "Declare Interface" declaration. $1 is the declared name, $2 is the interface
	// it inherits from.
	var reDeclareIntf = new RegExp("DeclareInterface\\((\\w+), *(\\w+),.+\\)","");

	// Match DeclareDualInterface{2}. $1 is "2" or nothing, $2 is the interface name.
	var reDeclareDual = new RegExp("DeclareDualInterface(2?)\\((\\w+),.+\\)","");

	// Match "Interface ISomeInterface" on a line by itself (typically comment block header)
	var reInterface = new RegExp("^\\s*Interface I\\w+\\s*$");

	// Match any argument specifier
	var reInOut = new RegExp("\\[.*\\]","");

	while (!ts.atEndOfStream)
	{
		var str = ts.ReadLine();

		// Look for DeclareInterface type strings.
		// If one of these matches, since it can't be immediately followed by another, we can just continue
		// the loop.

		var arr = reDeclareIntf.exec(str);
		if (arr != null)
		{
			str = RegExp.leftContext + "class I" + RegExp.$1 + ": public I" + RegExp.$2;
			tsout.WriteLine(str);
			str = ts.ReadLine(); // typically the opening brace
			tsout.WriteLine(str);
			tsout.WriteLine("\tpublic:");
			continue;
		}
		arr = reDeclareDual.exec(str);
		if (arr != null)
		{
			var prefix = "I";
			if (RegExp.$1 != "2")
				prefix = "DI"; // Regular DualInterface declares DI...
			str = RegExp.leftContext + "class " + prefix + RegExp.$2 + ": public IDispatch";
			tsout.WriteLine(str);
			str = ts.ReadLine(); // typically the opening brace
			tsout.WriteLine(str);
			tsout.WriteLine("\tpublic:");
			continue;
		}

		// Our block comments for DeclareInterface usually start "Interface IX" but we don't
		// want this in the input to Surveyor which makes its own overall header.
		arr = reInterface.exec(str);
		if (arr != null)
			continue;

		// Change "[propget] HRESULT MyFunc" to "HRESULT get_MyFunc", etc.
		str = str.replace(reGet, "HRESULT get_");
		str = str.replace(rePut, "HRESULT put_");
		str = str.replace(rePutref, "HRESULT putref_");

		// Remove argument specifiers, also things like [v1_enum].
		str = str.replace(reInOut, "");
		tsout.WriteLine(str);

	   //var sOutput = str;
	   //var arr = re.exec(str);
	   //if (arr != null)
	//		sOutput = RegExp.leftContext + "HRESULT get_" + RegExp.rightContext;
	//	tsout.WriteLine(sOutput);
	}
	ts.Close();
	tsout.Close();
}

/*----------------------------------------------------------------------------------------------
	Function to process all interface files (*.idh).
		(a) recursively process subfolders;
		(b) call ConvertIdh for every idh file in the folder
----------------------------------------------------------------------------------------------*/
function objweb_ProcessInterfaceFiles(folder, strSrcPath, strOutputPath)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	var enumerator = new Enumerator(folder.SubFolders);
	while (!enumerator.atEnd())
	{
		objweb_ProcessInterfaceFiles(enumerator.item(),
			fso.BuildPath(strSrcPath, enumerator.item().Name), strOutputPath);
		enumerator.moveNext();
	}

	var reIDH = new RegExp("\.(idh|IDH|Idh)$", "");
	enumerator = new Enumerator(folder.Files);
	while (!enumerator.atEnd())
	{
		var file = enumerator.item();
		var arr = reIDH.exec(file.Name)
		if (arr != null)
		{
			//Echo("Converting " + RegExp.leftContext + " in " + strSrcPath);
			objweb_ConvertIdh(RegExp.leftContext, strSrcPath, strOutputPath);
		}
		enumerator.moveNext();
	}
}

/*----------------------------------------------------------------------------------------------
	Function to run GtorSur program to build object web..
----------------------------------------------------------------------------------------------*/
function objweb_createObjWeb(strBldFldr, rptObj)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");

	// Process all idh files to produce pseudo (*.idh_) include files
	var strSrcRoot = fso.BuildPath(strBldFldr, "src");
	var strOutputFldr = fso.BuildPath(strBldFldr, "src\\interfaces");
	file_MakeSureFolderExists(strOutputFldr);
	objweb_ProcessInterfaceFiles(fso.GetFolder(strSrcRoot), strSrcRoot, strOutputFldr);

	// Run the Surveyor program
	var strSurveyorCommand = "C:\\Progra~1\\Surveyor\\System\\GtorSur.exe";
	misc_RunExtProg(strSurveyorCommand, "[AutoWeb(\"" + strSrcRoot + "\")][Quit]", null, rptObj, null, true);

}

/*----------------------------------------------------------------------------------------------
	Function to copy object web file to server.
----------------------------------------------------------------------------------------------*/
function objweb_copyObjWeb(strOutputFldr, strObjectWebDestPath, rptObj)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");

	// Move the output from the Surveyor program to the web site.
	// Delete the current stuff	on the web share. Leave the Graphite folder.
	fso.DeleteFile(fso.BuildPath(strObjectWebDestPath, "*.*"), true);
	fso.DeleteFolder(fso.BuildPath(strObjectWebDestPath, "Controls"),true);
	fso.DeleteFolder(fso.BuildPath(strObjectWebDestPath, "Images"),true);
	fso.DeleteFolder(fso.BuildPath(strObjectWebDestPath, "Links"),true);
	fso.DeleteFolder(fso.BuildPath(strObjectWebDestPath, "Objects"),true);
	misc_Sleep(20, rptObj);
	rptObj.reportProgress("Done deleting object web on web site. Copying new files...");

	// Copy the new files
	var strObjectWebSrcPath = fso.BuildPath(strOutputFldr, "ObjectWeb");
	fso.CopyFile(fso.BuildPath(strObjectWebSrcPath, "*.*"), strObjectWebDestPath, true);
	fso.CopyFolder(fso.BuildPath(strObjectWebSrcPath, "*.*"), strObjectWebDestPath, true);
	misc_Sleep(60, rptObj);
	rptObj.reportProgress("Done copying object web to web site. Deleting source files...");

	// Copy to contents of the Doc directory. REVIEW: JeffG - We don't want to copy this here now,
	//		some will go into the remote site.
	//fso.CopyFolder(fso.BuildPath(strBldFldr, "Doc"),
	//fso.BuildPath (strObjectWebDestPath, "Doc"), true);

	//Delete the orginals
	// Purposely leave the files in the Object web dir, default.htm will be overwritten.
	fso.DeleteFolder(fso.BuildPath(strObjectWebSrcPath, "*.*"), true);
	rptObj.reportProgress("Done deleting object web source files.");
}

//
// Datatbase functions
//
/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function db_startServer(sqlSvrBinDir, rptObj)
{
	//TODO:JeffG - this does not support sql server2K

	var shellObj = new ActiveXObject("WScript.Shell");
	var fso = new ActiveXObject("Scripting.FileSystemObject");

	var retValue;

	var sqlSCMCommand = fso.BuildPath(sqlSvrBinDir, "scm.exe");
	// Make sure SQLServer is started
	rptObj.echoItVerbose(sqlSCMCommand + " -Action 3 -Silent 1");
	retValue = shellObj.Run("\""+ sqlSCMCommand + "\"" + " -Action 3", 0, true);
	if (retValue ==  -1)
	{
		rptObj.reportProgress("Starting SQL Server...", null);
		try
		{
			misc_RunExtProg(sqlSCMCommand, "-Action 1", null, rptObj, null, true);
		} catch (err)
		{
			err.number = 1;
			err.description += ": Failed to load SQLServer."
			throw(err);
		}
	}
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function db_detachDB(strOSQLCommand, strDBName, strOSQLStdParams, rptObj)
{
	rptObj.echoItVerbose(strOSQLCommand + strOSQLStdParams + "-dmaster -Q\"sp_detach_db [" +
		strDBName + "]" + "\"");
	try
	{
		misc_RunExtProg(strOSQLCommand, strOSQLStdParams + "-dmaster -n -Q\"sp_detach_db [" +
			strDBName + "]" +"\"", "errorlevel", rptObj, null, true);
	} catch (err)
	{
		err.number = 1;
		err.description += ": Failed to detach newly built database."
		throw(err);
	}
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function db_deleteDB(strOSQLCommand, strDBName, strOSQLStdParams, rptObj)
{
	rptObj.echoItVerbose(strOSQLCommand + strOSQLStdParams + " -dmaster -n -Q\"drop database [" +
		strDBName + "]" + "\"");
	misc_RunExtProg(strOSQLCommand, strOSQLStdParams + "-dmaster -n -Q\"drop database [" +
		strDBName + "]" + "\"", null, rptObj, null, true);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function db_loadSchema(strOSQLCommand, strDBName, strSQLFile, strOSQLStdParams, strOSQLParam2,
	rptObj)
{
	rptObj.echoItVerbose(strOSQLCommand + strOSQLStdParams + "-d\"" + strDBName + "\" -i" +
		strSQLFile + strOSQLParam2);
	rptObj.reportProgress("Loading database schema from " + strSQLFile + "...", null);
	try
	{
		misc_RunExtProg(strOSQLCommand, strOSQLStdParams + "-d\"" + strDBName +
			"\" -i" + strSQLFile +  strOSQLParam2, "errorlevel", rptObj, null, true);
	} catch (err)
	{
		err.number = 1;
		err.description += ": Failed to load database schema."
		throw(err);
	}

	misc_Sleep(2, rptObj);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function db_createDB(strOSQLCommand, strDBFile, strDBName, strOSQLStdParams, strOutputDir, rptObj)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");

	// Create the database
	var strDBOn = " ON (NAME='" + strDBFile + "',FILENAME='" +
		fso.BuildPath(strOutputDir, strDBFile) +
		".mdf',SIZE=10MB,MAXSIZE=200MB,FILEGROWTH=5MB)";
	var strDBLogOn = " LOG ON (NAME='" + strDBFile + "_Log', FILENAME='" +
		fso.BuildPath(strOutputDir, strDBFile) +
		"_Log.ldf',SIZE=5MB,MAXSIZE=25MB,FILEGROWTH=5MB)\"";

	rptObj.echoItVerbose(strOSQLCommand + strOSQLStdParams + " -dmaster -n -Q\"Create database [" +
		strDBName + "]" + strDBOn + strDBLogOn);
	rptObj.reportProgress("Creating database "+ strDBName + " and loading schema....", null);
	try
	{
		misc_RunExtProg(strOSQLCommand, strOSQLStdParams + "-dmaster -n -Q\"Create database [" +
			strDBName + "]" + strDBOn + strDBLogOn, "errorlevel", rptObj, null, true);
	} catch (err)
	{
		err.number=1;
		err.description += ": Failed Create the database."
		throw(err);
	}

	misc_Sleep(2, rptObj);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function db_loadXML(strLoadXMLCmd, strDBFile, strXMLFile, rptObj)
{
	rptObj.reportProgress("Loading data from " + strXMLFile + "...", null);
	rptObj.echoItVerbose(strLoadXMLCmd + " -d \"" + strDBFile + "\" -i \"" + strXMLFile + "\"");
	try
	{
		misc_RunExtProg(strLoadXMLCmd, "-d \"" + strDBFile + "\" -i \"" + strXMLFile + "\"", null,
			rptObj, null, true);
	} catch (err)
	{
		err.number = 1;
		err.description += ": Failed to load XML file, see "+ fso.GetBaseName(strXMLFile) +
			"-import.log.";
		throw(err);
	}

	misc_Sleep(2, rptObj);
}

/*----------------------------------------------------------------------------------------------
	Builds a database by starting the server, dettaching if found, creating the db, loading the
	schema, and loading the data. All this with the help of SQLServer 7.0
	command line utilities.
----------------------------------------------------------------------------------------------*/
function db_BuildDB(strDBName, strDBFile, strSQLFile, strOutputDir, strXMLFile,
	strLoadXMLCmd, strLogFile, strFWRootDir, rptObj, fDetach)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	rptObj.echoItVerbose("In BuildDb.");
	// Check arguments here
	if (strDBName == null)
	{
		err = new Error;
		err.number = 1;
		err.description = "Database name is a required parameter.";
		throw(err);
	}
	// REVIEW: JeffG - Seems like the check for existance of strSQLFile fails
	if (strSQLFile == null || !fso.FileExists(strSQLFile))
	{
		err = new Error;
		err.number = 1;
		err.description = "SQL filename, " + strSQLFile + " was not found.";
		throw(err);
	}

	if (strOutputDir == null)
	{
		err = new Error;
		err.number = 1;
		err.description = "Output directory is a required parameter.";
		throw(err);
	}

	// Should allow for null strDBFile, and use the name of the database for file name
	// if strDFile is null, make it the same name as the db
	if (strDBFile == null)
		strDBFile = strDBName;

	// Get the location of SQLServer 7.0 from registry.
	var shellObj = new ActiveXObject("WScript.Shell");
	var strSvrToolsDir = shellObj.RegRead("HKLM\\Software\\Microsoft\\Microsoft SQL Server\\80\\Tools\\ClientSetup\\SQLPath");

	// Set a few directory strings to use below.
	var strSvrBinDir = fso.BuildPath(strSvrToolsDir, "binn");
	var strOSQLCommand = fso.BuildPath(strSvrBinDir, "osql.exe");
	var strOSQLStdParams = "-S.\\SILFW -Usa -Pinscrutable ";

	// Make sure we can write this somewhere.
	file_MakeSureFolderExists(strOutputDir);

	// Run the detach operation to detach if the db is already attached..
	db_detachDB(strOSQLCommand, strDBName, strOSQLStdParams, rptObj);

	// Create the raw database
	db_createDB(strOSQLCommand, strDBFile, strDBName, strOSQLStdParams, strOutputDir, rptObj);

	// Build schema in database with sql file passed as argument.
	db_loadSchema(strOSQLCommand, strDBName, strSQLFile, strOSQLStdParams, " -a 8192 -n", rptObj);

	// Load data from xml file, should allow for null here.
	if (strXMLFile)
		db_loadXML(strLoadXMLCmd, strDBName, strXMLFile, rptObj);

	// We may be required to detach the DB here:
	if (fDetach)
		db_detachDB(strOSQLCommand, strDBName, strOSQLStdParams, rptObj);

	return;
}

//
// SCCS functions
//
/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function sccs_VSSGetBuildSrc(strDataBase, strProj, strOutDir, strLabel, rptObj)
{
	rptObj.echoItVerbose("In VSSGetBuildSrc.");
	var COM_VSS_WORKS = 0;

	if (COM_VSS_WORKS)
	{
		// Create an instance of the SourceSafe COM object
		var vssDBObj = new ActiveXObject("SourceSafe");

		// Open the database
		try
		{
			vssDBObj.Open(strDataBase, "fwbuilder", "fwbuilder");
		} catch (err)
		{
			err.number = 1;
			err.description = "Could not open "+ strDatabase + " SourceSafe database";
			throw(err);
		}

		// Get the project specified
		try
		{
			var vssItemObj = vssDBObj.VSSItem(strProj);
		} catch (err)
		{
			err.number = 1;
			err.description = "Could not retrieve "+ strProj + " SourceSafe project.";
			throw(err);
		}

		// If there is a label specifed, get the ItemObj for that version.
		// TODO: JeffG - Test this functionallity.
		if (strLabel)
		{
			try
			{
				vssItemObj = vssItemObj.Version(strLabel);
			} catch (err)
			{
				err.number = 1;
				err.description = "Could not retrieve " + strProj + " with " + strLabel + " label.";
				throw(err);
			}
		}

		// Get the source code for all the subprojects and force the output directory.
		try
		{
			vssItemObj.Get(strOutDir, 8192 + 16384);	//32768
		} catch (err)
		{
			if (err.number == -2147166391)
				rptObj.reportProgress("ALERT: VSS " + err.description, null);
			else
				throw(err);
		}
		// TODO: JeffG - Set the objects created to null.
	}
	else	// Use the batch file instead. This does not support labels at this time.
	{
		// Split the strOutDir into drive and directory.
		var fso = new ActiveXObject("Scripting.FileSystemObject");
		try
		{
			misc_RunExtProg("getsrc.bat", fso.GetDriveName(strOutDir) + " " + strOutDir + " " ,
				"errorlevel", rptObj, null, true);
		} catch (err)
		{
			err.number=1;
			err.description += ": Failed to get source code."
			throw(err);
		}

	}
}



//
// Build functions
/*----------------------------------------------------------------------------------------------
	Function to call makeall with appropiate settings..
----------------------------------------------------------------------------------------------*/
function bld_buildFW(strBldFldr, rptObj, strBldType, strFlags)
{
		var fso = new ActiveXObject("Scripting.FileSystemObject");
		var strBldCmdStr = fso.BuildPath(strBldFldr, "bin\\mkallbld.bat");
		rptObj.reportProgress("Starting " + strBldType + " build...");
		rptObj.closeLog();	// Close so the external process can append to the log file.
		try
		{
			misc_RunExtProg(strBldCmdStr, strFlags, null, rptObj, rptObj.strLogFileName, true);
		} catch (err)
		{
			rptObj.reopenLog (); // Re-open the log file for appending.
			rptObj.reportFailure(strBldType + " build failed, " + err.description, true);
			misc_ExitScript();
		}
		rptObj.reopenLog(); // Re-open the log file for appending
		rptObj.reportProgress("Finished building " + strBldType +" build.");
}

/*----------------------------------------------------------------------------------------------
	Function to call mkall-tst with appropiate settings.
	These tests are run as part of C# tests.
----------------------------------------------------------------------------------------------*/
function bld_TestFW(strBldFldr, rptObj, strBldType, strFlags)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	var strBldCmdStr = fso.BuildPath(strBldFldr, "bin\\report_mkall-tst.bat");
	rptObj.reportProgress("Starting " + strBldType + " tests (building and running)...");
	rptObj.closeLog();	// Close so the external process can append to the log file.
	try
	{
		misc_RunExtProg(strBldCmdStr, strFlags, null, rptObj, rptObj.strLogFileName, true);
	} catch (err)
	{
		rptObj.reopenLog (); // Re-open the log file for appending.
		//rptObj.reportFailure(strBldType + " C++ tests returned: " + err.number, false);
		var strMailCmdStr = fso.BuildPath(strBldFldr, "bin\\OverniteFailNotify.bat");
		if (err.number == 1)
		{
			rptObj.reportFailure(strBldType + " tests failed, " + err.description, false);
			var strErr = "1";
		}
		else if (err.number == 2)
		{
			rptObj.reportFailure(strBldType + ": building of tests failed, " + err.description, false);
			var strErr = "2";
		}
		else if (err.number == 3)
		{
			rptObj.reportFailure(strBldType + ": building and running of tests failed, " + err.description, false);
			var strErr = "3";
		}
		else
		{
			rptObj.reportFailure(strBldType + ": unknown test error, " + err.description, false);
			var strErr = "99";
		}
		rptObj.reportProgress("Sending email message " + strErr +".");
		misc_RunExtProg(strMailCmdStr, strErr, null, rptObj, null, true);
	}
	rptObj.reopenLog(); // Re-open the log file for appending
	rptObj.reportProgress("Finished " + strBldType +" C++ tests.");
}

/*----------------------------------------------------------------------------------------------
	Function to run C# tests.
----------------------------------------------------------------------------------------------*/
function run_TestFwCs(strBldFldr, rptObj, strBldType, strFlags)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	var strBldCmdStr = fso.BuildPath(strBldFldr, "bin\\report_cs_tests.bat");
	rptObj.reportProgress("Starting " + strBldType + " C# tests...");
	rptObj.closeLog();	// Close so the external process can append to the log file.
	try
	{
		misc_RunExtProg(strBldCmdStr, strFlags, null, rptObj, rptObj.strLogFileName, true);
	} catch (err)
	{
		rptObj.reopenLog (); // Re-open the log file for appending.
		//rptObj.reportFailure(strBldType + " C# tests returned: " + err.number, false);
		var strMailCmdStr = fso.BuildPath(strBldFldr, "bin\\OverniteFailNotify.bat");
		rptObj.reportFailure(strBldType + " C# tests failed, " + err.description, false);
		var strErr = "4";
		rptObj.reportProgress("Sending email message " + strErr +".");
		misc_RunExtProg(strMailCmdStr, strErr, null, rptObj, null, true);
	}
	rptObj.reopenLog(); // Re-open the log file for appending
	rptObj.reportProgress("Finished " + strBldType +" C# tests.");
}

/*----------------------------------------------------------------------------------------------
	Function to run SQL tests.
	These are now run as part of C# tests.
----------------------------------------------------------------------------------------------*/
function run_TestFwSql(strBldFldr, rptObj, strBldType, strFlags)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	var strBldCmdStr = fso.BuildPath(strBldFldr, "bin\\report_mksql-tst.bat");
	rptObj.reportProgress("Starting " + strBldType + " SQL tests...");
	rptObj.closeLog();	// Close so the external process can append to the log file.
	try
	{
		misc_RunExtProg(strBldCmdStr, strFlags, null, rptObj, rptObj.strLogFileName, true);
	} catch (err)
	{
		rptObj.reopenLog (); // Re-open the log file for appending.
		//rptObj.reportFailure(strBldType + " SQL tests returned: " + err.number, false);
		var strMailCmdStr = fso.BuildPath(strBldFldr, "bin\\OverniteFailNotify.bat");
		rptObj.reportFailure(strBldType + " SQL tests failed, " + err.description, false);
		var strErr = "5";
		rptObj.reportProgress("Sending email message " + strErr +".");
		misc_RunExtProg(strMailCmdStr, strErr, null, rptObj, null, true);
	}
	rptObj.reopenLog(); // Re-open the log file for appending
	rptObj.reportProgress("Finished " + strBldType +" SQL tests.");
}

//
//	MAIN CODE
//
/*------------------------------------------------------------------------------------------

------------------------------------------------------------------------------------------*/
function Main()
{
	// Create some commonly used objects.
	var shellObj = new ActiveXObject("WScript.Shell");
	var fso = new ActiveXObject("Scripting.FileSystemObject");

	//
	// Create the Report object.
	//
	var rptObj = new Report(fso.BuildPath(fso.GetAbsolutePathName(".\\"),
		"overnitebld.log"));

	// Get the command line arguments
	if (WScript.Arguments.Length < 2)
	{
		misc_DisplayUsage(rptObj);
	}

	var nodebugbld = false;
	var forcedebugtests = false;
	var noreleasebld = false;
	var forcereleasetests = false;
	var noboundsbld = false;
	var noautotest = false;
	var norefreshsrc = false;
	var nocreatedbs = false;
	var noinstallbldr = false;
	var noftpupload = false;
	var nocreatedoc = false;
	var testrefresh = false;
	var testdoc = false;
	var testdb = false;
	var testinst = false;
	var nodeloutput = false;
	var nocopyoutput = false;
	var nocopyinst = false;
	var testinstcopy = false;
//TODO:JeffG	var nobuilds = false;

	// Assume the first two arguments are the build root and output root.
	var	strBldFldr, strOutputFldr, bldLevel, strVSSLabel;
	// Default build level is 0 if not overridden with -b in the command line.
	bldLevel = 0;
	strBldFldr = WScript.Arguments.Item(0);
	strOutputFldr = WScript.Arguments.Item(1);
	// TODO: JeffG - add this to the argument list.
	//strFWDeliverablesFldr = "\\\\ls-elmer\\FwDeliverables"

	for (iArgs = 2; iArgs < WScript.Arguments.Length; iArgs++)
	{
		var strArg = WScript.Arguments.Item(iArgs);
		if (strArg.charAt(0) != "-")
		{
			rptObj.echoIt("Options must be preceeded by a \"-\".");
			WScript.Quit();
		}

		var cmd = strArg.charAt(1);
		cmd = cmd.toLowerCase();
		if (cmd == "b")
		{
			bldLevel = parseInt(strArg.substr(2));
			if (bldLevel < 0 || bldLevel > 8)
			{
				rptObj.echoIt("ERROR: Build level must be an integer between 0 and 8.");
				WScript.Quit();
			}

			rptObj.echoItVerbose("Build level set to " + bldLevel);
			//TODO: JeffG - Make sure this is working.
		}
		else if (cmd == "l")
		{
			strVSSLabel = strArg.substr(2);
			rptObj.echoItVerbose("Applying label " + strVSSLabel);
		}
		else if (cmd == "d")
		{
			var strVar = strArg.substr(2);
			strVar = strVar.toLowerCase();
			eval(strVar + "= true;");
			rptObj.echoItVerbose("Defining " + strVar);
		}
		else if (cmd == "o")
		{
			var strOutputFldrOverride = strArg.substr(2);
			rptObj.echoItVerbose("Overriding output directory :" + strOutputFldrOverride);
		}
		else if (cmd == "v")
		{
			rptObj.fVerboseMode = true;
		}
		else
		{
			rptObj.echoIt("ERROR: Invalid argument, \"" + strArg + "\"");
			WScript.Quit();
		}
	}

	// Set special test modes.
	if (testrefresh)
	{
		nodebugbld = true;
		noreleasebld = true;
		noboundsbld = true;
		noautotest = true;
		nodeloutput = true;
		norefreshsrc = false;
		nocreatedbs = true;
		noinstallbldr = true;
		noftpupload = true;
		nocreatedoc = true;
		nocopyoutput = true;
	}

	if (testdoc)
	{
		nodebugbld = true;
		noreleasebld = true;
		noboundsbld = true;
		noautotest = true;
		nodeloutput = true;
		norefreshsrc = true;
		nocreatedbs = true;
		noinstallbldr = true;
		noftpupload = true;
		nocreatedoc = false;
		nocopyoutput = true;
	}

	if (testdb)
	{
		nodebugbld = true;
		noreleasebld = true;
		noboundsbld = true;
		noautotest = true;
		nodeloutput = true;
		norefreshsrc = true;
		nocreatedbs = false;
		noinstallbldr = true;
		noftpupload = true;
		nocreatedoc = true;
		nocopyoutput = true;
	}

	if (testinst)
	{
		nodebugbld = true;
		noreleasebld = true;
		noboundsbld = true;
		noautotest = true;
		nodeloutput = true;
		norefreshsrc = true;
		nocreatedbs = true;
		noinstallbldr = false;
		noftpupload = true;
		nocreatedoc = true;
		nocopyoutput = true;
	}

	if (testinstcopy)
	{
		nodebugbld = true;
		noreleasebld = true;
		noboundsbld = true;
		noautotest = true;
		nodeloutput = true;
		norefreshsrc = true;
		nocreatedbs = true;
		noinstallbldr = true;
		noftpupload = true;
		nocreatedoc = true;
		nocopyoutput = true;
		nocopyinst = false;
	}

	rptObj.echoItVerbose("Setting up build system");

	// Set the build level as an environment variable
	rptObj.echoItVerbose("Setting env vars.");

	var env = shellObj.Environment("Process");
	env.Item("BUILD_LEVEL") = bldLevel;

	// Set the BUILD_ROOT environment variable for the make process.
	env.Item("FWROOT") = strBldFldr;

	var strSvrToolsDir = shellObj.RegRead("HKLM\\Software\\Microsoft\\Microsoft SQL Server\\80\\Tools\\ClientSetup\\SQLPath");
	var strSvrBinDir = fso.BuildPath(strSvrToolsDir, "binn");
	var strOSQLCommand = fso.BuildPath(strSvrBinDir, "osql.exe");
	var strOSQLStdParams = "-S.\\SILFW -Usa -Pinscrutable ";
	//
	// Delete the output directories.
	//
	if (!nodeloutput)
	{
		// Delete the existing system
		rptObj.reportProgress("Removing ouput folders...");
		try
		{
			// Delete the test database in case it is still there. It needs to be deleted
			// before we can clear the directories.
			rptObj.echoItVerbose("Removing the test database...");
			misc_RunExtProg("osql.exe", "-S.\\SILFW -Usa -Pinscrutable -dmaster -n -Q\"drop database TestLangProj\"", "errorlevel", rptObj, null, true);

			// Recreate the output dir.
			rptObj.echoItVerbose("Removing the output folder...");
			file_DeleteFolder(fso.BuildPath(strBldFldr, "output"));
			// Recreate the obj dir.
			rptObj.echoItVerbose("Removing the obj folder...");
			file_DeleteFolder(fso.BuildPath(strBldFldr, "obj"));

			//TODO: JeffG - Move these somewhere appropiate.
			rptObj.echoItVerbose("Removing the overnite files...");
			file_DeleteFile(fso.BuildPath(strOutputFldr, "overnite.tst"));

			rptObj.echoItVerbose("Done clearing out source tree");

		} catch (err)
		{
			rptObj.reportFailure("Unable to recreate source tree, " +
				err.description, true);
			exitScript(rptObj);
		}
	}

	//
	// Get the latest source.
	//
	norefreshsrc = true; // disable this, can delete in new system.
	if (!norefreshsrc)
	{
		rptObj.echoItVerbose("About to check for refresh");
		rptObj.reportProgress("Getting latest source from SourceSafe...");
		try
		{
			sccs_VSSGetBuildSrc("\\\\Elmer\\FW\\VSS\\srcsafe.ini", "$/", strBldFldr,
				strVSSLabel, rptObj)
		} catch (err)
		{
			rptObj.reportFailure("Get latest failed from SourceSafe, " +
				err.description, true);
			exitScript(rptObj);
		}
		rptObj.reportProgress("Finished getting latest source.");
	}

	//
	// Build debug version.
	//
	if (!nodebugbld)
	{
		// cc is needed for .NET
		bld_buildFW(strBldFldr, rptObj, "Debug", "d cc register");
		// The tests have been moved to after the databases have been built.
	}

	//
	// Create databases.	Do before bounds build so the *.tsc file dialog will not
	//						come up on loadxml.exe call. Do before release build as the sql
	//						file generated by the release build is bogus.
	//
	rptObj.echoItVerbose("About to check for createdb");
	if (!nocreatedbs)
	{
		rptObj.reportProgress("Building databases...");
		// Build the TestLangProj database.
		rptObj.reportProgress("Building TestLangProj...");
		rptObj.closeLog();
		try
		{
			zipper = new ActiveXObject("XceedSoftware.XceedZip.4");
			zipper.ZipFilename = fso.BuildPath(strBldFldr, "Test\\TestLangProj.zip");
			zipper.UnzipToFolder = fso.BuildPath(strBldFldr, "Test");
			zipper.UnZip();
			db_BuildDB("TestLangProj", null,
				fso.BuildPath(strBldFldr, "Output\\Common\\NewLangProj.sql"),
				fso.BuildPath(strBldFldr, "Output\\SampleData"),
				fso.BuildPath(strBldFldr, "test\\testlangproj.xml"),
				fso.BuildPath(strBldFldr, "bin\\loadxml.exe"), rptObj.strLogFileName,
					strBldFldr, rptObj, false);
		} catch (err)
		{
			rptObj.reopenLog(); // Re-open the log file for appending.
			rptObj.reportFailure("Failed to build TestLangProj database, " + err.description, false);
		}
		try
		{
			rptObj.reportProgress("Making copy of TestLangProj for testing...");
			// We need to make a copy of TestLangProj to run tests on it.
			// First, detach the existing database:
			// Set a few directory strings to use below.
			rptObj.echoItVerbose("Setting up variables...");
			rptObj.echoItVerbose("Detaching TestLangProj...");
			db_detachDB(strOSQLCommand, "TestLangProj", strOSQLStdParams, rptObj);

			// Now copy the files:
			rptObj.echoItVerbose("Making copies of mdf and ldf files...");
			fso.CopyFile(fso.BuildPath(strBldFldr, "Output\\SampleData\\TestLangProj.mdf"),
				fso.BuildPath(strBldFldr, "Output\\SampleData\\TestLangProj_test.mdf"), true);
			fso.CopyFile(fso.BuildPath(strBldFldr, "Output\\SampleData\\TestLangProj_log.ldf"),
				fso.BuildPath(strBldFldr, "Output\\SampleData\\TestLangProj_test_log.ldf"), true);

			// Now attach the copy:
			rptObj.echoItVerbose("Attaching new copies...");
			misc_RunExtProg(strOSQLCommand, strOSQLStdParams + "-dmaster -n -Q\"sp_attach_db @dbname = N'TestLangProj', @filename1 = N'"
			 + fso.BuildPath(strBldFldr, "Output\\SampleData\\TestLangProj_test.mdf")
			 + "', @filename2 = N'" + fso.BuildPath(strBldFldr, "Output\\SampleData\\TestLangProj_test_log.ldf") + "'",
			 "errorlevel", rptObj, null, true);
			rptObj.echoItVerbose("Finished attaching copy of TestLangProj.");
		} catch (err)
		{
			rptObj.reopenLog(); // Re-open the log file for appending.
			rptObj.reportFailure("Failed to create and attach copy of TestLangProj database, " + err.description, false);
		}

		// Build the NewLangProj database - no load xml.
		rptObj.reportProgress("Building BlankLangProj...");
		rptObj.closeLog();
		try
		{
			// Delete old version of BlankLangProj first:
			file_DeleteFile(fso.BuildPath(strBldFldr, "DistFiles\\Templates\\BlankLangProj.mdf"));
			file_DeleteFile(fso.BuildPath(strBldFldr, "DistFiles\\Templates\\BlankLangProj_Log.ldf"));
			db_BuildDB("BlankLangProj", null,
				fso.BuildPath(strBldFldr, "Output\\Common\\NewLangProj.sql"),
				fso.BuildPath(strBldFldr, "DistFiles\\Templates"),
				null, null, rptObj.strLogFileName, strBldFldr, rptObj, true);
		} catch (err)
		{
			rptObj.reopenLog(); // Re-open the log file for appending.
			rptObj.reportFailure("Failed to build BlankLangProj database, " + err.description, false);
		}

		// Build the Lela Teli 2 database.
		rptObj.reportProgress("Building Lela-Teli 2...");
		rptObj.closeLog();
		try
		{
			db_BuildDB("Lela-Teli 2", null,
				fso.BuildPath(strBldFldr, "Output\\Common\\NewLangProj.sql"),
				fso.BuildPath(strBldFldr, "Output\\SampleData"),
				fso.BuildPath(strBldFldr, "Test\\Lela-Teli 2.xml"),
				fso.BuildPath(strBldFldr, "bin\\loadxml.exe"), rptObj.strLogFileName,
					strBldFldr, rptObj, true);
		} catch (err)
		{
			rptObj.reopenLog(); // Re-open the log file for appending.
			rptObj.reportFailure("Failed to build Lela-Teli 2 database, " + err.description, false);
		}

		// Build the Lela-Teli 3 database.
		rptObj.reportProgress("Building Lela-Teli 3...");
		rptObj.closeLog();
		try
		{
			db_BuildDB("Lela-Teli 3", null,
				fso.BuildPath(strBldFldr, "Output\\Common\\NewLangProj.sql"),
				fso.BuildPath(strBldFldr, "Output\\SampleData"),
				fso.BuildPath(strBldFldr, "Test\\Lela-Teli 3.xml"),
				fso.BuildPath(strBldFldr, "bin\\loadxml.exe"), rptObj.strLogFileName,
					strBldFldr, rptObj, true);
		} catch (err)
		{
			rptObj.reopenLog(); // Re-open the log file for appending.
			rptObj.reportFailure("Failed to build Lela-Teli 3 database, " + err.description, false);
		}

		// Build the Ethnologue database.
		rptObj.reportProgress("Building Ethnologue...");
		rptObj.closeLog();
		try
		{
			// Create the database
			var strBat = fso.BuildPath(strBldFldr, "ethnologue\\CreateEthnologue.bat");
			var strArgs = fso.BuildPath(strBldFldr, "Ethnologue");
			misc_RunExtProg(strBat, strArgs, "errorlevel", rptObj, null, true);
			// Detach the database
			var strOSQLCommand = "osql.exe";
			rptObj.echoItVerbose("Detaching Ethnologue...");
			db_detachDB(strOSQLCommand, "Ethnologue", "-S.\\SILFW -Usa -Pinscrutable ", rptObj);
			// Get the location of SQLServer directory from registry.
			var shellObj = new ActiveXObject("WScript.Shell");
			var strSvrDir = shellObj.RegRead("HKLM\\Software\\Microsoft\\Microsoft SQL Server\\SILFW\\Setup\\SQLPath");
			// Copy the files to output\sampledata
			fso.CopyFile(fso.BuildPath(strSvrDir, "Data\\Ethnologue.mdf"),
				fso.BuildPath(strBldFldr, "Output\\SampleData\\Ethnologue.mdf"), true);
			fso.CopyFile(fso.BuildPath(strSvrDir, "Data\\Ethnologue_log.ldf"),
				fso.BuildPath(strBldFldr, "Output\\SampleData\\Ethnologue_log.ldf"), true);
			// Attach the original Ethnologue database for tests.
			misc_RunExtProg(strOSQLCommand, strOSQLStdParams + "-dmaster -n -Q\"sp_attach_db @dbname = N'Ethnologue', @filename1 = N'"
			 + fso.BuildPath(strSvrDir, "Data\\Ethnologue.mdf")
			 + "', @filename2 = N'" + fso.BuildPath(strSvrDir, "Data\\Ethnologue_log.ldf") + "'",
			 "errorlevel", rptObj, null, true);
		} catch (err)
		{
			rptObj.reopenLog(); // Re-open the log file for appending.
			rptObj.reportFailure("Failed to build Ethnologue database, " + err.description, false);
		}

		// Build the Greek database someday.
		// reopen the log file
		rptObj.reopenLog();
	}

	if (!nodebugbld || forcedebugtests)
	{
		// Run the debug tests.
		rptObj.echoItVerbose("About to check for noautotest and forcedebugtests");
		if (!noautotest || forcedebugtests)
		{
			//bld_TestFW(strBldFldr, rptObj, "Debug", "d cc");
			run_TestFwCs(strBldFldr, rptObj, "Debug", "debug");
		}
	}
	//if (!nocreatedbs || forcedebugtests || forcereleasetests)
	//	run_TestFwSql(strBldFldr, rptObj, "Debug", "");

	//
	// Build release version.
	//
	rptObj.echoItVerbose("About to check for relbld");
	if (!noreleasebld || forcereleasetests)
	{
		if (!noreleasebld)
		{
			// cc is no longer needed for .NET
			bld_buildFW(strBldFldr, rptObj, "Release", "r register");
		}
		// If everything built OK in the release build, run the tests.
		rptObj.echoItVerbose("About to check for noautotest and forcereleasetests");
		if (!noautotest || forcereleasetests)
		{
			//bld_TestFW(strBldFldr, rptObj, "Release", "r");
			run_TestFwCs(strBldFldr, rptObj, "Release", "release");
		}
	}

	//
	// Build Bounds Checker version.
	//
	noboundsbld=true; // We don't have a Bounds Checker version for VS.NET.
	rptObj.echoItVerbose("About to check for boundsbld");
	if (!noboundsbld)
	{
		bld_buildFW(strBldFldr, rptObj, "Bounds Checker", "b register regps");

		// If everything built OK in the release build, run the tests.
		rptObj.echoItVerbose("About to check for noautotest");
		if (!noautotest)
		{
			//bld_TestFW(strBldFldr, rptObj, "Bounds Checker", "b");
		}
	}

	// We are no longer detaching the test database, so that if tests fail,
	// investigations can be carried out without first having to re-attach it.
	/*
	// Detach the test database, now that unit tests have finished:
	if (!nocreatedbs)
	{
		// Get the location of SQLServer 7.0 from registry.
		var shellObj = new ActiveXObject("WScript.Shell");
		var strSvrToolsDir = shellObj.RegRead("HKLM\\Software\\Microsoft\\Microsoft SQL Server\\80\\Tools\\ClientSetup\\SQLPath");

		// Set a few directory strings to use below.
		var strSvrBinDir = fso.BuildPath(strSvrToolsDir, "binn");
		var strOSQLCommand = fso.BuildPath(strSvrBinDir, "osql.exe");
		var strOSQLStdParams = "-S.\\SILFW -Usa -Pinscrutable ";

		db_detachDB(strOSQLCommand, "TestLangProj", strOSQLStdParams, rptObj);
	}*/

	//
	//	If everything built and passed the tests, copy everything to output
	//
	if (!nocopyoutput)
	{
		// Copy the output of the build
		rptObj.reportProgress("Copying build output...");
		// Check for override
		if (strOutputFldrOverride)
		{
			var strDailyBuildFldr = fso.BuildPath(strOutputFldr, strOutputFldrOverride);
		}
		else
		{
			var strDailyBuildFldr = fso.BuildPath(strOutputFldr, file_GetDailyBuildFolderName());
		}

		// Delete the daily build folder if it exists.
		file_DeleteFolder(strDailyBuildFldr);

		// Detach the test database, else we cannot copy it:
		// Get the location of SQLServer from registry.
		var shellObj = new ActiveXObject("WScript.Shell");
		var strSvrToolsDir = shellObj.RegRead("HKLM\\Software\\Microsoft\\Microsoft SQL Server\\80\\Tools\\ClientSetup\\SQLPath");

		// Set a few directory strings to use below.
		var strSvrBinDir = fso.BuildPath(strSvrToolsDir, "binn");
		var strOSQLCommand = fso.BuildPath(strSvrBinDir, "osql.exe");
		var strOSQLStdParams = "-S.\\SILFW -Usa -Pinscrutable ";

		db_detachDB(strOSQLCommand, "TestLangProj", strOSQLStdParams, rptObj);

		rptObj.echoItVerbose("Copying files from " + fso.BuildPath(strBldFldr, "Output") +
			" to " + strDailyBuildFldr);
		fso.CopyFolder(fso.BuildPath(strBldFldr, "Output"), strDailyBuildFldr, true);

		// Copy FW source
		rptObj.reportProgress("Copying source code...");
		var strSrcFolder = fso.BuildPath(strDailyBuildFldr, "Code");
		file_DeleteFolder(strSrcFolder);
		rptObj.echoItVerbose("Copying files from " + fso.BuildPath(strBldFldr, "src") +
			" to " + fso.BuildPath(strDailyBuildFldr, "Code"));
		fso.CopyFolder(fso.BuildPath(strBldFldr, "src"),
			fso.BuildPath(strDailyBuildFldr, "Code"), true);


		// Copy the results of automated testing.
		//TODO: Remove the following assignment when we are running the tests,
		// check the args to copyFolder.
		noautotest = true;
		if (!noautotest)
		{
			// Copy the Test code
			rptObj.reportProgress("Copying test code...", null);
			if (fso.FolderExists(fso.BuildPath(strBldFldr, "Test")))
				fso.CopyFolder(fso.BuildPath(strBldFldr, "Test"),
					fso.BuildPath(strDailyBuildFldr,"Code\\Test"));

			// Copy the Bounds checker files.
			rptObj.reportProgress("Copying Bounds Checker files...", null);
			fso.CopyFile(fso.BuildPath(strBldFldr, "TestLog\\Log\\BoundsChecker.bce"),
				strDailyBuildFldr + "\\");
			fso.CopyFolder(fso.BuildPath(strBldFldr,"TestLog\\Log\\TrueCoverage.tsc"),
				strDailyBuildFldr + "\\");
		}

		// Copy the overnitebld.log file in the root directory.
		rptObj.echoItVerbose("Copying overnitebld.log file to " + strDailyBuildFldr + "\\");
		fso.CopyFile(rptObj.strLogFileName, strDailyBuildFldr + "\\", true);


		//
		// Copy the *.mdf and *.ldf files from the output\sampledata.
		//
		// REVIEW: JeffG - Does this need a try/catch blk around it?
		rptObj.reportProgress("Copying the build databases...");
		var strSampleDataOutDir = fso.BuildPath(strOutputFldr, "SampleData");
		rptObj.echoItVerbose("Deleting " + strSampleDataOutDir);
		file_DeleteFolder(strSampleDataOutDir);
		rptObj.echoItVerbose("Creating " + strSampleDataOutDir);
		fso.CreateFolder(strSampleDataOutDir);
		var strSampleDataSrcDir = fso.BuildPath(strBldFldr, "Output\\SampleData");
		rptObj.echoItVerbose("Copying databases from " + strSampleDataSrcDir +
			" to " + strSampleDataOutDir);
		fso.CopyFile(fso.BuildPath(strSampleDataSrcDir, "*.mdf"), strSampleDataOutDir + "\\");
		fso.CopyFile(fso.BuildPath(strSampleDataSrcDir, "*.ldf"), strSampleDataOutDir + "\\");

		// Re-attach the test database:
		rptObj.echoItVerbose("Attaching new copies...");
		misc_RunExtProg(strOSQLCommand, strOSQLStdParams + "-dmaster -n -Q\"sp_attach_db @dbname = N'TestLangProj', @filename1 = N'"
			+ fso.BuildPath(strBldFldr, "Output\\SampleData\\TestLangProj_test.mdf")
			+ "', @filename2 = N'" + fso.BuildPath(strBldFldr, "Output\\SampleData\\TestLangProj_test_log.ldf") + "'",
			"errorlevel", rptObj, null, true);
		rptObj.echoItVerbose("Finished attaching copy of TestLangProj.");

		// Copy the Template files
		var strTemplateDataOutDir = fso.BuildPath(strDailyBuildFldr, "Templates");
		rptObj.echoItVerbose("Deleting " + strTemplateDataOutDir);
		file_DeleteFolder(strTemplateDataOutDir);
		rptObj.echoItVerbose("Creating " + strTemplateDataOutDir);
		fso.CreateFolder(strTemplateDataOutDir);
		var strTemplatesDataSrcDir = fso.BuildPath(strBldFldr, "Output\\Templates");
		rptObj.echoItVerbose("Copying databases from " + strTemplatesDataSrcDir +
			" to " + strTemplatesDataSrcDir);

/* There are no longer any files in Output\Templates, since BlankLangProj was diverted to
	DistFiles\Templates, so these lines have been removed:
		fso.CopyFile(fso.BuildPath(strTemplatesDataSrcDir, "*.mdf"), strTemplateDataOutDir + "\\");
		fso.CopyFile(fso.BuildPath(strTemplatesDataSrcDir, "*.ldf"), strTemplateDataOutDir + "\\");
*/
		// Copy NewLangProj.xml from the test directory.
		var strDistFilesDir = fso.BuildPath(strBldFldr, "DistFiles\\Templates");
		rptObj.echoItVerbose("Copying XML files from " + strDistFilesDir +
				" to " + strTemplatesDataSrcDir);
		fso.CopyFile(fso.BuildPath(strDistFilesDir, "*.xml"), strTemplateDataOutDir + "\\");
		// Copy other Templates for SIL distribution -- no longer on Elmer. Covered above.
		//fso.CopyFile("\\\\ls-elmer\\FwDeliverables\\Templates\\*.*", strTemplateDataOutDir + "\\");
	}

	//
	// Run the batch file to build the debug and release installers.
	//
	rptObj.echoItVerbose("About to check noinstallbldr");

	if (!noinstallbldr)
	{
		// Set a few directory strings to use below.
		var strSvrToolsDir = shellObj.RegRead("HKLM\\Software\\Microsoft\\Microsoft SQL Server\\80\\Tools\\ClientSetup\\SQLPath");
		var strSvrBinDir = fso.BuildPath(strSvrToolsDir, "binn");
		var strOSQLCommand = fso.BuildPath(strSvrBinDir, "osql.exe");
		var strOSQLStdParams = "-S.\\SILFW -Usa -Pinscrutable ";

		// We want to make sure the installers have a clean copy of the ICU data files, so at this point
		// we delete all icu data files and unzip a clean copy from icu.zip. In order to delete the
		// files, we need to make sure the icu files are freed up.
		misc_RunExtProg(strOSQLCommand, strOSQLStdParams +
			"-dMaster -h-1 -Q\"DBCC DebugProcs (FREE)\"", "errorlevel", rptObj, null, true);
		rptObj.reportProgress("Producing clean ICU data files...");
		file_DeleteFolder(fso.BuildPath(strBldFldr, "DistFiles\\icu\\data"));
		// Note file_DeleteFile fails with wildcards because the if exists returns false so we'll do this directly.
		var fsox = new ActiveXObject("Scripting.FileSystemObject");
		if (fso.FileExists(fso.BuildPath(strBldFldr, "DistFiles\\icu\\icudt26l_root.res")))
			fsox.DeleteFile(fso.BuildPath(strBldFldr, "DistFiles\\icu\\icudt*.*"));
		//file_DeleteFile(fso.BuildPath(strBldFldr, "DistFiles\\icu\\icudt*.*"));
		// We need to touch Unicode Character Database.htm when done and since I don't
		// know how to do this from Java Script, we'll have nant do the unzipping and touching.
		//zipper = new ActiveXObject("XceedSoftware.XceedZip.4");
		//zipper.ZipFilename = fso.BuildPath(strBldFldr, "DistFiles\\icu.zip");
		//zipper.UnzipToFolder = fso.BuildPath(strBldFldr, "DistFiles\\icu");
		//zipper.UnZip();
		var strNant = fso.BuildPath(strBldFldr, "bin\\nant\\bin\\nant.exe");
		var strBld = fso.BuildPath(strBldFldr, "bld\\FieldWorks.build");
		misc_RunExtProg(strNant, " -f:" + strBld + " IcuData", "errorlevel", rptObj, null, true);

		//
		// Build general FW installer
		//
		// Build FW Windows Installer for the debug build version.
		var strIBBatchFile = fso.BuildPath(strBldFldr, "Installer\\MakeInstaller.bat");

		rptObj.reportProgress("Building FieldWorks installer...");
		var strCmpVars = "";

		// Run the batch file
		rptObj.closeLog();	// Close so the external process can append to the log file.
		misc_RunExtProg(strIBBatchFile, strCmpVars, null, rptObj, rptObj.strLogFileName, true);
		rptObj.reopenLog();
	}

	//
	// Now copy built installers.
	//
	if (!nocopyinst)
	{
		// Copy all the installers built to the shared directory
		if (strOutputFldrOverride)
		{
			var strPubInstOutFldr = fso.BuildPath("\\\\ls-elmer\\FwBuilds", strOutputFldrOverride);
		}
		else
		{
			var strPubInstOutFldr = fso.BuildPath("\\\\ls-elmer\\FwBuilds", file_GetDailyBuildFolderName());
		}
		// Recreate the daily build folder if it exists.
		file_CreateFolder(strPubInstOutFldr);
		// Copy FW intaller
		var strPubInstOutFldrFW = fso.BuildPath(strPubInstOutFldr, "FieldWorks");
		file_CreateFolder(strPubInstOutFldrFW);
		// Debug version
		var strInstOutFldrT = fso.BuildPath(strPubInstOutFldrFW, "Debug");
		var strBuiltInstallerPath = fso.BuildPath(strBldFldr, "Installer");
		rptObj.echoItVerbose("Copying FieldWorks debug installer files from " + strBuiltInstallerPath +
			" to " + strInstOutFldrT);
		file_CreateFolder(strInstOutFldrT);
		fso.CopyFile(fso.BuildPath(strBuiltInstallerPath, "SetupFwDebug.exe"), strInstOutFldrT + "\\");
		fso.CopyFile(fso.BuildPath(strBuiltInstallerPath, "SetupFwDebug.ini"), strInstOutFldrT + "\\");
		fso.CopyFile(fso.BuildPath(strBuiltInstallerPath, "SetupFwDebug.msi"), strInstOutFldrT + "\\");
		// Copy the installer for Windows Installer runtime.
		fso.CopyFile(fso.BuildPath(strBuiltInstallerPath, "instmsiw.exe"), strInstOutFldrT + "\\");

		// Release version
		var strInstOutFldrT = fso.BuildPath(strPubInstOutFldrFW, "Release");
		rptObj.echoItVerbose("Copying FieldWorks release installer files from " + strBuiltInstallerPath +
			" to " + strInstOutFldrT);
		file_CreateFolder(strInstOutFldrT);
		fso.CopyFile(fso.BuildPath(strBuiltInstallerPath, "SetupFw.exe"), strInstOutFldrT + "\\");
		fso.CopyFile(fso.BuildPath(strBuiltInstallerPath, "SetupFw.ini"), strInstOutFldrT + "\\");
		fso.CopyFile(fso.BuildPath(strBuiltInstallerPath, "SetupFw.msi"), strInstOutFldrT + "\\");
		// Copy the installers for Windows Installer runtime.
		fso.CopyFile(fso.BuildPath(strBuiltInstallerPath, "instmsiw.exe"), strInstOutFldrT + "\\");

		rptObj.reportProgress("Finished building installers. Copying installers to JAARS...");
		// Copy the debug version to \\jar-file\SILLangSoft\SILInstaller\Debug:
		var strJaarsFolder = "\\\\jar-file\\SILLangSoft\\SILInstaller\\Debug\\FieldWorks\\";
		file_DeleteFile(fso.BuildPath(strJaarsFolder, "SetupFw.msi"));
		fso.CopyFile(fso.BuildPath(strBuiltInstallerPath, "SetupFwDebug India.msi"),
			fso.BuildPath(strJaarsFolder, "SetupFw.msi"));
		rptObj.reportProgress("Done Debug version, doing Release version...");
		// Copy the release version to \\jar-file\SILLangSoft\SILInstaller\Release:
		var strJaarsReleaseFolder = "\\\\jar-file\\SILLangSoft\\SILInstaller\\Release\\FieldWorks\\";
		file_DeleteFile(fso.BuildPath(strJaarsReleaseFolder, "SetupFw.msi"));
		fso.CopyFile(fso.BuildPath(strBuiltInstallerPath, "SetupFW India.msi"),
			fso.BuildPath(strJaarsReleaseFolder, "SetupFw.msi"));
		rptObj.reportProgress("Finished copying installers to JAARS.");
	}
	//
	// Now build the code documentation web site with Surveyor.
	//
	if (!nocreatedoc)
	{
		// Object Web
		rptObj.reportProgress("Begin building ObjectWeb");
		objweb_createObjWeb(strBldFldr, rptObj);
		//TODO: JeffG - make this copy process smarter by checking file dates and lack of
		//	file on target. Delete obsolete files and refresh only those that have changed
		rptObj.reportProgress("Finished building ObjectWeb");
		misc_Sleep(20, rptObj);
		rptObj.reportProgress("Begin copying ObjectWeb");
		objweb_copyObjWeb(strOutputFldr, "\\\\172.21.1.118\\ObjectWeb\\", rptObj);
		rptObj.reportProgress("Finished copying ObjectWeb");

		// MagicDraw
		//TODO: JeffG - should build from MagicDraw and copy output, then remove shared folder
		//file_RefreshFldr("e:\\ModelDoc", "j:\\fieldworks\ModelDoc");
	}

	//
	// Finish up.
	//
	// Reset the FWROOT environment variable.
	env.Item("FWROOT") = "";
	rptObj.reportProgress("Everything Built Correctly, exiting.");
}

/*------------------------------------------------------------------------------------------

------------------------------------------------------------------------------------------*/
function exitScript(rptObj)
{
	// Reset the FWROOT environment variable.
	var shellObj = new ActiveXObject("WScript.Shell");
	var env = shellObj.Environment("Process");
	env.Item("FWROOT") = "";
	if (rptObj)
		rptObj.closeLog();

	WScript.Quit();
}
////////////////////////////////////////////////////////////////////////////////////////////
// Begin the main execution.
////////////////////////////////////////////////////////////////////////////////////////////
try
{
	Main();
	WScript.Quit();
} catch (err)
{
	WScript.Echo("Unhandled exception, check for spelling errors: " +
		err.description, true);
	exitScript(null);

}
//tail junk