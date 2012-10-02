/*----------------------------------------------------------------------------------------------
Copyright 2001, SIL International. All rights reserved.

File: SurveyorProcessIdhFiles.js
Responsibility: Paul Panek
Last reviewed: Not yet.

Description:
Script for converting .IDH files into .IDH_ files which can then be given to the AutoSurveyor
program to generate web-page documentation (i.e. the program that runs on ls-muttley that
checks every 10 seconds for .cpp, .h, and .idh_ files in certain directories).  Basically, this
is just a clipped down version of the script that Jeff Gayle wrote to do the daily FieldWorks
build process.

How To Use:
Copy the .idh files that you want to have processed along with this script to the same
directory.  Then run this script.  After 1-2 seconds, it should be done and you can then move
the .idh_ files (along with the other .cpp and .h files you want processed) to the the
ls-muttley\autosurveyor\YourName directory.  AutoSurveyor will then "digest" them and make
web page documentation.
----------------------------------------------------------------------------------------------*/

/*******************************************************************
REVIEW: Always check for bad variable names, typos, etc. They will
cause runtime execptions.
*********************************************************************/

// Global Variables
var debug = false;	// If true, verbose progress messages will be emitted.

// Folder/File Consts
var kForRead = 1;
var kForWrite = 2;
var kForAppend = 8;
var kFormatASCII = 0;
var kFormatUnicode = -1;
var kNoCreateFile = false;



//
//	Reporting functions.
//
/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function EchoLog(strMsg, tsoLogFile)
{
	tsoLogFile.WriteLine(strMsg);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function Echo(strMsg, tsoLogFile)
{
	WScript.Echo(strMsg);
	if (tsoLogFile)
		EchoLog(strMsg, tsoLogFile);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function EchoDbg(strMsg)
{
	if (debug)
		Echo(">> Debug - " + strMsg + " <<");
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function LogBanner(strMsg, tsoLogFile)
{
	EchoLog("*******************************************************************************",
		tsoLogFile);
	EchoLog(strMsg, tsoLogFile);
	EchoLog("*******************************************************************************",
		tsoLogFile);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function EmailList(strArrayEmailList, strMsg)
{
	var shellObj = new ActiveXObject("WScript.Shell");

	// Assumes dosmail.exe is somewhere in the path.
	var i;
	for (i = 0; i < strArrayEmailList.Length; i++)
	{
		// TODO: JeffG - get dosmail moved into the path or add \bin dir to path.
		//  shellObj.Run(binDir + "\\dosmail.exe",
		// "ls-muttley@sil.org " + strArrayEmailList[i] + strMsg, null, null);
		ReportProgress("Sending mail to: " + strArrayEmailList[i] + ", msg = " + strMsg, null);
	}
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function ReportFailure(strMsg, strArrayEmailList, tsoLogFile, fAbortScript)
{
	if (fAbortScript)
		ReportProgress("ERROR: " + strMsg + " - Examine overnite.bld - Exiting Script",
			tsoLogFile);
	else
		ReportProgress("ERROR:" + strMsg + " - Examine overnite.bld.", tsoLogFile);

	if (strArrayEmailList)
		EmailList(strArrayEmailList);

	if (fAbortScript==true)
	{
		if (tsoLogFile)
		{
			tsoLogFile.Close();
			tsoLogFile = null;
		}

		// Reset the FWROOT environment variable.
		var shellObj = new ActiveXObject("WScript.Shell");
		var env = shellObj.Environment("Process");
		env.Item("FWROOT") = "";
		WScript.Quit();
	}
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function ReportProgress(strMsg, tsoLogFile)
{
	Echo("<< " + strMsg + " >>", null);
	if (tsoLogFile)
		LogBanner(strMsg, tsoLogFile);

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
function ConvertIdh(strName, strSrcPath, strOutputPath)
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
function ProcessInterfaceFiles(folder, strSrcPath, strOutputPath)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	var enumerator = new Enumerator(folder.SubFolders);
	while (!enumerator.atEnd())
	{
		ProcessInterfaceFiles(enumerator.item(),
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
			ConvertIdh(RegExp.leftContext, strSrcPath, strOutputPath);
		}
		enumerator.moveNext();
	}
}


//
//	MAIN CODE
//
/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function Main()
{
	//
	// Process all idh files to produce pseudo (*.idh_) include files
	//
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	var srcRoot = fso.BuildPath(".", "");
	ProcessInterfaceFiles(fso.GetFolder(srcRoot), ".", ".");
}

///////////////////////////////////////////////////////////////////////////////////////////////
// Begin the main execution.
///////////////////////////////////////////////////////////////////////////////////////////////
try
{
	Main();
	WScript.Quit();
} catch (err)
{
	ReportFailure("Unhandled exception, check for spelling errors: " + err.description, null,
		null, true);
}
