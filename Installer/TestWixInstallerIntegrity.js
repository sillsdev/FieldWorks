/*

JScript to examine the integrity of the WIX source files, in the light of a new build of FW.

The WIX installer is compromised if any file it is expecting no longer exists.
The installer can also be compromised if a new file is created but not added
to the installer when it is needed.

This script checks for those situations. It creates a ChangedFiles.xml file detailing new and
deleted files, and returns 0 if no files have changed, 1 if there are new files, 2 if there
are deleted files, and 3 if there are both types.
If there is an error, -1 is returned, and a file TestWixInstallerIntegrity.log is created.

Comparison is made with Files.wxs WIX installer source, and with FileLibrary.xml - a record of
all known files.

*/

// Get script path details:
var iLastBackslash = WScript.ScriptFullName.lastIndexOf("\\");
var ScriptPath = WScript.ScriptFullName.slice(0, iLastBackslash);

var fso = new ActiveXObject("Scripting.FileSystemObject");

// Because this test is not enforced on all developers, we look for the file "TestWixInstallerIntegrity.yes"
// in the same folder as this script, or the second command line argument being "yes" and if neither are there,
// we quit:
var Continue = false;
if (fso.FileExists(fso.BuildPath(ScriptPath, "TestWixInstallerIntegrity.yes")))
	Continue = true;
else
{
	if (WScript.Arguments.Length >= 2)
	{
		if (WScript.Arguments.Item(0).toLowerCase() == "yes" || WScript.Arguments.Item(1).toLowerCase() == "yes")
			Continue = true;
	}
	else if (WScript.Arguments.Length >= 1)
	{
		if (WScript.Arguments.Item(0).toLowerCase() == "yes")
			Continue = true;
	}
}
if (!Continue)
	WScript.Quit(0);

var Build = "debug";

// See if the word "debug" appears as the first command line argument:
if (WScript.Arguments.Length < 1)
	WScript.Echo("WARNING - expected 1 argument: defaulting to " + Build + " build.");
else
{
	if (WScript.Arguments.Item(0).toLowerCase() != "yes")
		Build = WScript.Arguments.Item(0).toLowerCase();
}

if (Build != "debug" && Build != "release")
{
	WScript.Echo("ERROR - needs 1 argument: debug or release.");
	WScript.Quit();
}

// Get path details:
var RootFolder = ScriptPath;
// If the script is in a subfolder called Installer, then set the root folder back one notch:
iLastBackslash = ScriptPath.lastIndexOf("\\");
if (iLastBackslash > 0)
{
	if (ScriptPath.slice(iLastBackslash + 1) == "Installer")
		RootFolder = ScriptPath.slice(0, iLastBackslash + 1).toLowerCase();
}

var xmlFileLibrary = new ActiveXObject("Msxml2.DOMDocument.4.0");
xmlFileLibrary.async = false;
xmlFileLibrary.load("FileLibrary.xml");
if (xmlFileLibrary.parseError.errorCode != 0)
{
	var myErr = xmlFileLibrary.parseError;
	ReportError("XML error in FileLibrary.xml: " + myErr.reason + "\non line " + myErr.line + " at position " + myErr.linepos);
	WScript.Quit(-1);
}

var xmlComponents = new ActiveXObject("Msxml2.DOMDocument.4.0");
xmlComponents.async = false;
xmlComponents.setProperty("SelectionNamespaces", 'xmlns:wix="http://schemas.microsoft.com/wix/2003/01/wi"');
xmlComponents.load("Files.wxs");
if (xmlComponents.parseError.errorCode != 0)
{
	var myErr = xmlComponents.parseError;
	ReportError("XML error in Files.wxs: " + myErr.reason + "\non line " + myErr.line + " at position " + myErr.linepos);
	WScript.Quit(-1);
}

var NewFiles = new Array();
var DeletedFiles = new Array();

// Read principle list of paths to include and exclude in search:
var xmlDirectoryScan = new ActiveXObject("Msxml2.DOMDocument.4.0");
xmlDirectoryScan.async = false;
xmlDirectoryScan.load("DirectoryScan.xml");
if (xmlDirectoryScan.parseError.errorCode != 0)
{
	var myErr = xmlDirectoryScan.parseError;
	ReportError("XML error in DirectoryScan.xml: " + myErr.reason + "\non line " + myErr.line + " at position " + myErr.linepos);
	WScript.Quit(-1);
}
var InstallerDirectoryScanNode = xmlDirectoryScan.selectSingleNode("/InstallerDirectoryScan");

// Add in any exclude paths from the user's private file:
var xmlDirectoryScanPrivate = null;
if (fso.FileExists(fso.BuildPath(ScriptPath, "DirectoryScanPrivate.xml")))
{
	xmlDirectoryScanPrivate = new ActiveXObject("Msxml2.DOMDocument.4.0");
	xmlDirectoryScanPrivate.async = false;
	xmlDirectoryScanPrivate.load("DirectoryScanPrivate.xml");
	if (xmlDirectoryScanPrivate.parseError.errorCode != 0)
	{
		var myErr = xmlDirectoryScanPrivate.parseError;
		ReportError("XML error in DirectoryScanPrivate.xml: " + myErr.reason + "\non line " + myErr.line + " at position " + myErr.linepos);
		WScript.Quit(-1);
	}
	var DirectoryScanPrivateExcludeNodes = xmlDirectoryScanPrivate.selectNodes("//Exclude");
	for (n = 0; n < DirectoryScanPrivateExcludeNodes.length; n++)
		InstallerDirectoryScanNode.appendChild(DirectoryScanPrivateExcludeNodes[n]);
}
var DirectoryScanIncludeNodes = xmlDirectoryScan.selectNodes("//Include");
var DirectoryScanExcludeNodes = xmlDirectoryScan.selectNodes("//Exclude");

// consolidate list of ExcludedFolders and expand to to full paths:
ExcludedFolders = new Array();
ExcludedFiles = new Array();
for (l = 0; l < DirectoryScanExcludeNodes.length; l++)
{
	var CurrentFolder = DirectoryScanExcludeNodes[l].getAttribute("Folder");
	if (CurrentFolder)
		ExcludedFolders.push(fso.BuildPath(RootFolder, CurrentFolder).replace(/\\\${config}/i, "\\" + Build));

	var CurrentFile = DirectoryScanExcludeNodes[l].getAttribute("File");
	if (CurrentFile)
		ExcludedFiles.push(fso.BuildPath(RootFolder, CurrentFile).replace(/\\\${config}/i, "\\" + Build));
}

// Get total list of candidate files:
var TotalFileList = new Array();
var CurrentIncludePath;
var SubstitutePath;
try
{
	for (l = 0; l < DirectoryScanIncludeNodes.length; l++)
	{
		CurrentIncludePath = DirectoryScanIncludeNodes[l].getAttribute("Folder");
		SubstitutePath = CurrentIncludePath.replace(/\\\${config}/i, "\\" + Build);
		TotalFileList = TotalFileList.concat(GetFileList(fso.BuildPath(RootFolder, SubstitutePath)));
	}
}
catch (e)
{
	ReportError(e.description + ", CurrentIncludePath = " + CurrentIncludePath + ", SubstitutePath = " + SubstitutePath);
	WScript.Quit(-1);
}

// Get total list of files in Files.wxs:
var WixFileNodes = xmlComponents.selectNodes("//wix:File");
var FilesInInstaller = new Array();
for (i = 0; i <  WixFileNodes.length; i++)
{
	var Details = new Object();
	var FilePath = WixFileNodes[i].getAttribute("Source");

	FilePath = FilePath.replace(/\\\${config}\\/i, "\\" + Build + "\\");

	// Prefix the source with our Root folder, unless the source is an absolute path:
	if (FilePath.slice(1,2) != ":" && FilePath.slice(0,1) != "\\")
	{
		// Source is relative, so add in the Root:
		FilePath = fso.BuildPath(RootFolder, FilePath);
	}

	// Check if current file is in the ExcludedFiles array or part of the ExcludedFolders array:
	// TODO - make this more efficient, maybe by checking the folder part of an Excluded File first...
	var fExcludeFile = false;
	var iFile;
	for (iFile = 0; iFile < ExcludedFiles.length && !fExcludeFile; iFile++)
	{
		if (ExcludedFiles[iFile].toLowerCase() == FilePath.toLowerCase())
			fExcludeFile = true;
	}
	if (!fExcludeFile)
	{
		// Get folder path of current file:
		var iLastBackslash = FilePath.lastIndexOf("\\");
		if (iLastBackslash != -1)
		{
			var CurrentFolder = FilePath.slice(0, iLastBackslash).toLowerCase();
			var iFolder;
			// Check current file's folder against list of excluded folders:
			for (iFolder = 0; iFolder < ExcludedFolders.length; iFolder++)
			{
				if (CurrentFolder == ExcludedFolders[iFolder].toLowerCase())
				{
					fExcludeFile = true;
					continue;
				}
			}
		}
	}
	if (fExcludeFile)
		continue;

	Details.Source = FilePath;
	Details.StillExists = fso.FileExists(Details.Source);
	if (!Details.StillExists)
	{
		DeletedFiles.push(Details.Source);
	}
	FilesInInstaller.push(Details);
}

// Get total list of files in FileLibrary.xml:
var LibraryFileNodes = xmlFileLibrary.selectNodes("//File");
var FilesInLibrary = new Array();
for (i = 0; i <  LibraryFileNodes.length; i++)
{
	var f = fso.BuildPath(RootFolder, LibraryFileNodes[i].getAttribute("Path"));
	f = f.replace(/\\\${config}\\/i, "\\" + Build + "\\");
	FilesInLibrary.push(f);
}

// Iterate through all candidate files:
for (i = 0; i < TotalFileList.length; i++)
{
	var CurrentFile = TotalFileList[i];
	var fKnown = false;
	var fInInstaller = false;

	// See if current candidate file is in the WIX installer components:
	for (iFile = 0; iFile < FilesInInstaller.length; iFile++)
	{
		if (FilesInInstaller[iFile].Source.toLowerCase() == CurrentFile.toLowerCase())
		{
			fInInstaller = true;
			iFile = FilesInInstaller.length;
		}
	}

	// See if current candidate file is in the File Library:
	for (iFile = 0; iFile < FilesInLibrary.length; iFile++)
	{
		if (FilesInLibrary[iFile].toLowerCase() == CurrentFile.toLowerCase())
		{
			fKnown = true;
			iFile = FilesInLibrary.length;
		}
	}

	if (!fKnown && !fInInstaller)
		NewFiles.push(CurrentFile);
}

// Now create a ChangedFiles.xml file with the details:
var tso = fso.OpenTextFile("ChangedFiles.xml", 2, true, -1);
tso.WriteLine("<?xml version='1.0'?>");
tso.WriteLine("<ChangedFiles>");
tso.WriteLine("\t<Build Type='" + Build + "' />");

// Set up search regular expression:
var re = new RegExp("\\\\" + Build + "\\\\", "i"); // Quadruple backslashes because regular expressions require \ to be doubled, and so does JScript.

for (i = 0; i < NewFiles.length; i++)
	tso.WriteLine("\t<New Path='" + NewFiles[i].replace(re, "\\\${config}\\") + "' />");
for (i = 0; i < DeletedFiles.length; i++)
	tso.WriteLine("\t<Deleted Path='" + DeletedFiles[i].replace(re, "\\\${config}\\") + "' />");
tso.WriteLine("</ChangedFiles>");
tso.Close();

var Result = 0;
if (NewFiles.length > 0)
	Result += 1;
if (DeletedFiles.length > 0)
	Result += 2;

WScript.Quit(Result);


// Recurses given FolderPath and returns an array of path strings of files in the folder and its subfolders.
// Omits any files specified in ExcludedFiles, and any whose folders are in the global ExcludedFolders list.
function GetFileList(FolderPath)
{
	var ResultList = new Array();

	// Check if current Folder is a Subversion metadata folder:
	if (FolderPath.slice(-4) == ".svn")
		return ResultList; // Don't include SVN folders.

	// Check if current Folder is excluded:
	for (ex = 0; ex < ExcludedFolders.length; ex++)
		if (FolderPath.toLowerCase() == ExcludedFolders[ex].toLowerCase())
			return ResultList;

	// Add files in current folder:
	var Folder = fso.GetFolder(FolderPath);
	var FileIterator = new Enumerator(Folder.files);
	for (; !FileIterator.atEnd(); FileIterator.moveNext())
	{
		var CurrentFile = FileIterator.item().Path;

		// Check if current file is in the ExcludedFiles array:
		// TODO - make this more efficient, maybe by checking the folder part of an Excluded File first...
		var iFile;
		var fExcludeFile = false;
		for (iFile = 0; iFile < ExcludedFiles.length && !fExcludeFile; iFile++)
		{
			if (ExcludedFiles[iFile].toLowerCase() == CurrentFile.toLowerCase())
				fExcludeFile = true;
		}
		if (!fExcludeFile)
			ResultList.push(CurrentFile);
	}

	// Now recurse all subfolders:
	var SubfolderIterator = new Enumerator(Folder.SubFolders);
	for (; !SubfolderIterator.atEnd(); SubfolderIterator.moveNext())
		ResultList = ResultList.concat(GetFileList(SubfolderIterator.item().Path));

	return ResultList;
}

// Creates error log file:
function ReportError(text)
{
	var tso = fso.OpenTextFile("TestWixInstallerIntegrity.log", 2, true, -1);
	tso.WriteLine(text);
	tso.Close();
}
