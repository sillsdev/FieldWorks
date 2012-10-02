// JScript to produce WIX source fragments from Excel spreadsheet of a FieldWorks WSI
// installer source.
// The source Excel spreadsheet is specified in the global SourceXls variable below.
// The spreadsheet must be in XML, and of the form produced by the InstallerFileList.exe
// utility (At least 4 columns: Source Path, Destination path, File name, Merge Module).
// There will be three output files: Components.xml, Features.xml and FileLibrary.xml.
// The first two will define the components and features, respectively, needed by WIX
// to build the installer. FileLibrary.xml will contain a list of all the files in the
// Root Folder, along with a component guid in cases where the file is included in the
// installer. This file is used to detect future new files.
// Also defined near the start of this script:
// 1) the SourceFolders array: subfolders (from Root Folder) to check for new files;

// The source spreadsheet:
var SourceXls = "File List FW (Auto).xls";

// List of subfolders to check for new files:
var SourceFolders = new Array();
SourceFolders.push("DistFiles");
SourceFolders.push("Output\\Release");
SourceFolders.push("Output\\SampleData");
SourceFolders.push("Doc");
SourceFolders.push("Output\\Common");
SourceFolders.push("Lib\\release");

var TabSpaces = 2;

// Get path details:
var iLastBackslash = WScript.ScriptFullName.lastIndexOf("\\");
var ScriptPath = WScript.ScriptFullName.slice(0, iLastBackslash);
var RootFolder = ScriptPath;
// If the script is in a subfolder called Installer, then set the root folder back one notch:
iLastBackslash = ScriptPath.lastIndexOf("\\");
if (iLastBackslash > 0)
{
	if (ScriptPath.slice(iLastBackslash + 1) == "Installer")
		RootFolder = ScriptPath.slice(0, iLastBackslash + 1).toLowerCase();
}

var fso = new ActiveXObject("Scripting.FileSystemObject");
var shellObj = new ActiveXObject("WScript.Shell");

InitErrorLog();

// Set up the XML parser, including namespaces that are in the XLS spreadsheet:
var xmlSpreadsheet = new ActiveXObject("Msxml2.DOMDocument.4.0");
xmlSpreadsheet.async = false;
xmlSpreadsheet.setProperty("SelectionNamespaces", "xmlns:xls='urn:schemas-microsoft-com:office:spreadsheet' xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:x='urn:schemas-microsoft-com:office:excel' xmlns:ss='urn:schemas-microsoft-com:office:spreadsheet' xmlns:html='http://www.w3.org/TR/REC-html40'");

// Load and check the XLS spreadsheet:
xmlSpreadsheet.load(SourceXls);
if (xmlSpreadsheet.parseError.errorCode != 0)
{
	var myErr = xmlSpreadsheet.parseError;
	WScript.Echo("XML error in " + SourceXls + ": " + myErr.reason + "\non line " + myErr.line + " at position " + myErr.linepos);
	WScript.Quit();
}

// Convert SourceFolders to lower case:
for (i = 0; i < SourceFolders.length; i++)
	SourceFolders[i] = SourceFolders[i].toLowerCase();

// We're going to be specifying files in WIX according to their destination folders, so first
// we'll build a list of unique destination folders:
var DestFolders = new Array();
var xlsDestNodes = xmlSpreadsheet.selectNodes("//xls:Row/xls:Cell[2]/xls:Data[not(.=../../preceding-sibling::xls:Row/xls:Cell[2]/xls:Data)]");
for (i = 1; i < xlsDestNodes.length; i++) // Omit header
	DestFolders.push(xlsDestNodes[i].text);
DestFolders.sort();
// Generate short paths for the same folders:
var DestShortFolders = GetShortFolderNames(DestFolders);

// Now read in all the info from the spreadsheet, assigining GUIDs to files:
var FileData = new Array();
var Matches = new Array();
var xlsRowNodes = xmlSpreadsheet.selectNodes("//xls:Row");
for (iFile = 1; iFile < xlsRowNodes.length; iFile++) // Omit header
{
	var CellNodes = xlsRowNodes[iFile].selectNodes("xls:Cell");
	var File = new Object();
	File.Source = CellNodes[0].selectSingleNode("xls:Data").text.toLowerCase();
	File.Dest = CellNodes[1].selectSingleNode("xls:Data").text;
	File.Name = CellNodes[2].selectSingleNode("xls:Data").text;
	File.Module = CellNodes[3].selectSingleNode("xls:Data").text;
	File.guid = CreatGuid();
	File.fDN = false;
	File.fFlex = false;
	File.fTE = false;
	File.fTLE = false;
	File.fWP = false;
	File.fFlexMovies = false;
	File.fTEMovies = false;

	// Use the merge module to assign a file/component to a feature:
	if (File.Module == "CC DLL" ||
		File.Module == "PerlEC" ||
		File.Module == "PythonEC" ||
		File.Module == "EncConverters Core GAC" ||
		File.Module == "TECkit DLLs" ||
		File.Module == "SetPath" ||
		File.Module == "Old C++ and MFC DLLs" ||
		File.Module == "ICU036" ||
		File.Module == "Inner Core Release" ||
		File.Module == "Inner Core Common" ||
		File.Module == "Core Nonexec" ||
		File.Module == "Core Release" ||
		File.Module == "Core Automatic Release")
	{
		File.fDN = true;
		File.fFlex = true;
		File.fTE = true;
		File.fTLE = true;
		File.fWP = true;
	}
	if (File.Module == "Core DB Release" ||
		File.Module == "Core DB Common")
	{
		File.fDN = true;
		File.fFlex = true;
		File.fTE = true;
		File.fTLE = true;
	}
	if (File.Module == "DN Nonexec" ||
		File.Module == "DN Release")
	{
		File.fDN = true;
	}
	if (File.Module == "LexText Release" ||
		File.Module == "LexText Nonexec")
	{
		File.fFlex = true;
	}
	if (File.Module == "Flex Movies")
	{
		File.fFlexMovies = true;
	}
	if (File.Module == "TE Nonexec" ||
		File.Module == "TE Release")
	{
		File.fTE = true;
	}
	if (File.Module == "TE Movies")
	{
		File.fTEMovies = true;
	}
	if (File.Module == "TLE Release" ||
		File.Module == "TLE Nonexec")
	{
		File.fTLE = true;
	}
	if (File.Module == "WP Release" ||
		File.Module == "WP Nonexec")
	{
		File.fWP = true;
	}

	// Test if match is already known:
	var LowerCaseName = File.Name.toLowerCase();
	var fKnown = false;
	var MatchNumber = 0;
	for (iMatch = 0; iMatch < Matches.length; iMatch++)
	{
		if (Matches[iMatch].Name.toLowerCase() == LowerCaseName)
		{
			fKnown = true;
			var Other = new Object();
			Other.Source = File.Source;
			Other.Dest = File.Dest;
			Other.guid = File.guid;
			Matches[iMatch].Set.push(Other);
			MatchNumber = Matches[iMatch].Set.length;
			iMatch = Matches.length;
		}
	}
	if (!fKnown)
	{
		// Test if File has a name that matches another file:
		for (iTest = 0; iTest < iFile - 1; iTest++)
		{
			if (FileData[iTest].Name.toLowerCase() == LowerCaseName)
			{
				var Match = new Object();
				Match.Name = File.Name;
				Match.Set = new Array();
				var First = new Object();
				First.Source = FileData[iTest].Source;
				First.Dest = FileData[iTest].Dest;
				First.guid = FileData[iTest].guid;
				Match.Set.push(First);
				var Other = new Object();
				Other.Source = File.Source;
				Other.Dest = File.Dest;
				Other.guid = File.guid;
				Match.Set.push(Other);
				MatchNumber = Match.Set.length;
				Matches.push(Match);
				iTest = iFile - 1;
			}
		}
	} // Next existing file

	// Make a unique Id for file and components, based on file name:
	if (MatchNumber == 0)
		File.Id = MakeId(File.Name);
	else
		File.Id = MakeId(File.Name + MatchNumber);

	FileData.push(File);
}

// Report matches:
for (iMatch = 0; iMatch < Matches.length; iMatch++)
{
	var Match = Matches[iMatch];
	var Set = Match.Set;
	LogError("Warning: Possible duplicate file: " + Match.Name + " appears " + Set.length + " times:");
	for (iSet = 0; iSet < Set.length; iSet++)
		LogError("         " + (1 + iSet) + ") Source = " + Set[iSet].Source + ";   Dest = " + Set[iSet].Dest + ";   GUID = " + Set[iSet].guid);
}

// Generate list of all files that could be included in installer:
var TotalFileList = new Array();
for (i = 0; i < SourceFolders.length; i++)
{
	var FullPath = fso.BuildPath(RootFolder, SourceFolders[i]);
	var NewList = GetFileList(FullPath, true)
	TotalFileList = TotalFileList.concat(NewList);
}
// Remove root folder from each of the files in this list:
for (iTotal = 0; iTotal < TotalFileList.length; iTotal++)
	TotalFileList[iTotal] = TotalFileList[iTotal].replace(RootFolder, "");

// Open and initialize the Component and File Library output files:
// Components:
var tsoComp = fso.OpenTextFile("Components.xml", 2, true, -1);
WriteXmlLine(tsoComp, 0, '<?xml version="1.0"?>');
WriteXmlLine(tsoComp, 0, '<Wix xmlns="http://schemas.microsoft.com/wix/2003/01/wi">');
WriteXmlLine(tsoComp, 1, '<Fragment Id="ComponentsFragment">');
WriteXmlLine(tsoComp, 2, '<Directory Id="TARGETDIR" Name="SourceDir">');

// File library:
var tsoFileLibrary = fso.OpenTextFile("FileLibrary.xml", 2, true, -1);
WriteXmlLine(tsoFileLibrary, 0, '<?xml version="1.0"?>');
WriteXmlLine(tsoFileLibrary, 0, '<FileLibrary>');


// Iterate through every Destination folder:
var LastDestinationPath = "";
var CurrentDirIndent = 0;
var DirIdMatches = new Array();
for (iDest = 0; iDest < DestFolders.length; iDest++)
{
	var DestinationPath = DestFolders[iDest];
	var DestinationShortPath = DestShortFolders[iDest];

	// Navigate Directory nodes in Components file:
	CurrentDirData = WriteFoldersDelta(tsoComp, LastDestinationPath, DestinationPath, DestinationShortPath);
	CurrentDirIndent = CurrentDirData.Indent;
	CurrentDirId = CurrentDirData.Id;

	// Iterate over every file which should go in the current Destination:
	for (iFile = 0; iFile < FileData.length; iFile++)
	{
		var File = FileData[iFile];
		if (File.Dest == DestinationPath)
		{
			var SourcePath = File.Source;
			var FileName = File.Name;
			var Id = File.Id;
//WScript.Echo(iFile + "/" + xlsCurrentDestNodes.length + "  SourcePath: " + SourcePath + "; FileName: " + FileName + "; DestinationPath: " + DestinationPath);

			// Make a few custom changes to the SourcePath:
			SourcePath = FixSourcePath(SourcePath);

			// Write file and new component guid to File Library:
			WriteXmlLine(tsoFileLibrary, 1, '<File Path="' + SourcePath + FileName + '" ComponentGuid="' + File.guid + '"/>');

			// Remove current file from TotalFileList:
			for (iTotal = 0; iTotal < TotalFileList.length; iTotal++)
			{
/*var echo = 0;
if (TotalFileList[iTotal].toLowerCase().indexOf("backup.js") != -1 && FileName.toLowerCase().indexOf("backup.js") != -1)
	echo = 1;
if (echo == 1)
	WScript.Echo("Comparing " + TotalFileList[iTotal].toLowerCase() + " with " + fso.BuildPath(RootFolder, fso.BuildPath(SourcePath, FileName)).toLowerCase());
*/
				if (TotalFileList[iTotal].toLowerCase() == fso.BuildPath(SourcePath, FileName).toLowerCase())
				{
//if (echo == 1)
//	WScript.Echo("Removing " + TotalFileList[iTotal]);
					TotalFileList.splice(iTotal, 1);
					iTotal = TotalFileList.length;
				}
			}

			var FullSource = fso.BuildPath(fso.BuildPath(RootFolder, SourcePath), FileName);

			// Write Component:
			WriteXmlLine(tsoComp, CurrentDirIndent + 3, '<Component Id="' + Id + '" Guid="' + File.guid + '">');
			var ShortName = GetShortFileName(fso.BuildPath(SourcePath, FileName));
			if (ShortName == FileName)
				WriteXmlLine(tsoComp, CurrentDirIndent + 4, '<File Id="' + Id + '" Name="' + GetShortFileName(fso.BuildPath(SourcePath, FileName)) + '" ReadOnly="yes" Source="' + FullSource + '" DiskId="1" />');
			else
				WriteXmlLine(tsoComp, CurrentDirIndent + 4, '<File Id="' + Id + '" Name="' + GetShortFileName(fso.BuildPath(SourcePath, FileName)) + '" LongName="' + FileName + '" ReadOnly="yes" Source="' + FullSource + '" DiskId="1" />');

			// If current file is a DLL, then try running Tallow on it to get any registration data there may be:
			WriteRegInfo(FullSource, Id, CurrentDirId, tsoComp, CurrentDirIndent + 4);

			WriteXmlLine(tsoComp, CurrentDirIndent + 3, '</Component>');
		} // End if (File.Dest == DestinationPath)
	} // Next file
	LastDestinationPath = DestinationPath;
} // Next destination folder

// Add unused files to File Library:
for (iTotal = 0; iTotal < TotalFileList.length; iTotal++)
	WriteXmlLine(tsoFileLibrary, 1, '<File Path="' + TotalFileList[iTotal].replace("&", "&amp;") + '"/>');

// Close down File Library file:
WriteXmlLine(tsoFileLibrary, 0, '</FileLibrary>');
tsoFileLibrary.Close();

// Close down components file:
WriteFoldersDelta(tsoComp, LastDestinationPath, "", "");
WriteXmlLine(tsoComp, 2, '</Directory>');
WriteXmlLine(tsoComp, 1, '</Fragment>');
WriteXmlLine(tsoComp, 0, '</Wix>');
tsoComp.Close();


// Open and initialize the Features output file:
var tsoFeat = fso.OpenTextFile("Features.xml", 2, true, -1);
WriteXmlLine(tsoFeat, 0, '<?xml version="1.0"?>');
WriteXmlLine(tsoFeat, 0, '<Wix xmlns="http://schemas.microsoft.com/wix/2003/01/wi">');
WriteXmlLine(tsoFeat, 1, '<Fragment Id="FeaturesFragment">');

// Data Notebook:
WriteXmlLine(tsoFeat, 2, '<Feature Id="DN" Title="Data Notebook" Description="A data management tool for language and cultural fieldwork. For documenting, categorizing, retrieving, and analyzing fieldnotes." Display="expand" Level="3" AllowAdvertise="no">');
for (i = 0; i < FileData.length; i++)
{
	if (FileData[i].fDN)
		WriteXmlLine(tsoFeat, 3, '<ComponentRef Id="' + FileData[i].Id + '" />');
}
WriteXmlLine(tsoFeat, 2, '</Feature>');

// FLEx:
WriteXmlLine(tsoFeat, 2, '<Feature Id="FLEX" Title="Language Explorer" Description="FieldWorks Language Explorer" Display="expand" Level="3" AllowAdvertise="no">');
for (i = 0; i < FileData.length; i++)
{
	if (FileData[i].fFlex)
		WriteXmlLine(tsoFeat, 3, '<ComponentRef Id="' + FileData[i].Id + '" />');
}
// FLEx Movies:
WriteXmlLine(tsoFeat, 3, '<Feature Id="FlexMovies" Title="Demo Movies" Description="Language Explorer Demo Movies" Display="expand" Level="3" AllowAdvertise="no">');
for (i = 0; i < FileData.length; i++)
{
	if (FileData[i].fFlexMovies)
		WriteXmlLine(tsoFeat, 4, '<ComponentRef Id="' + FileData[i].Id + '" />');
}
WriteXmlLine(tsoFeat, 3, '</Feature>');
WriteXmlLine(tsoFeat, 2, '</Feature>');

// Translation Editor:
WriteXmlLine(tsoFeat, 2, '<Feature Id="TE" Title="Translation Editor" Description="FieldWorks Translation Editor" Display="expand" Level="3" AllowAdvertise="no">');
for (i = 0; i < FileData.length; i++)
{
	if (FileData[i].fTE)
		WriteXmlLine(tsoFeat, 3, '<ComponentRef Id="' + FileData[i].Id + '" />');
}
// TE Tutorials:
WriteXmlLine(tsoFeat, 3, '<Feature Id="TEMovies" Title="Tutorial Movies" Description="Translation Editor tutorial movies" Display="expand" Level="3" AllowAdvertise="no">');
for (i = 0; i < FileData.length; i++)
{
	if (FileData[i].fTEMovies)
		WriteXmlLine(tsoFeat, 4, '<ComponentRef Id="' + FileData[i].Id + '" />');
}
WriteXmlLine(tsoFeat, 3, '</Feature>');
WriteXmlLine(tsoFeat, 2, '</Feature>');

// Topics List Editor:
WriteXmlLine(tsoFeat, 2, '<Feature Id="TLE" Title="Topics List Editor" Description="For entering and managing lists of related data used repeatedly in various Data Notebook entries. Essential for most uses of Data Notebook." Display="expand" Level="3" AllowAdvertise="no">');
for (i = 0; i < FileData.length; i++)
{
	if (FileData[i].fTLE)
		WriteXmlLine(tsoFeat, 3, '<ComponentRef Id="' + FileData[i].Id + '" />');
}
WriteXmlLine(tsoFeat, 2, '</Feature>');

// WorldPad:
WriteXmlLine(tsoFeat, 2, '<Feature Id="WP" Title="WorldPad" Description="A simple word processor that can use Graphite to display complex scripts." Display="expand" Level="3" AllowAdvertise="no">');
for (i = 0; i < FileData.length; i++)
{
	if (FileData[i].fWP)
		WriteXmlLine(tsoFeat, 3, '<ComponentRef Id="' + FileData[i].Id + '" />');
}
WriteXmlLine(tsoFeat, 2, '</Feature>');

WriteXmlLine(tsoFeat, 1, '</Fragment>');
WriteXmlLine(tsoFeat, 0, '</Wix>');
tsoFeat.Close();

// Manipulate specified source path to meet our needs.
function FixSourcePath(SourcePath)
{
	// Change absolute path to relative path, if within our local file set:
	SourcePath = SourcePath.replace(RootFolder, "");

	// In some cases, files used in Wise installer were copies placed in temporary folders.
	// We will revert these back to their originals:
	SourcePath = SourcePath.replace(/output\\install\\release\\common\\flex/i, "output\\release");
	SourcePath = SourcePath.replace(/output\\install\\release\\common\\te/i, "output\\release");
	SourcePath = SourcePath.replace(/output\\install\\release\\common/i,"output\\release");
	SourcePath = SourcePath.replace("&", "&amp;");

	return SourcePath;
}

// Writes to the given XML file the navigation route between the given directory paths.
// Returns the current indentation level in the Directory nodes.
function WriteFoldersDelta(tso, StartPath, EndPath, EndShortPath)
{
	// Split each path into an array of folders:
	var aStart = StartPath.split("\\");
	if (StartPath == "")
		aStart = new Array();

	var aEnd = EndPath.split("\\");
	if (EndPath == "")
		aEnd = new Array();

	var aEndShort = EndShortPath.split("\\");
	if (EndShortPath == "")
		aEndShort = new Array();

//WScript.Echo(aStart + "(" + aStart.length + ") to " + aEnd + "(" + aEnd.length + ")");
	// Determine at what point the two paths diverge:
	var iDiverge = 0;
	if (aStart.length != 0 && aEnd.length != 0)
	{
		while (aStart[iDiverge] == aEnd[iDiverge])
			iDiverge++;
	}
//WScript.Echo("aStart.length = " + aStart.length + "; iDiverge = " + iDiverge + "; aEnd.length = " + aEnd.length);
	// Terminate directory nodes from start path:
	for (i = aStart.length; i > iDiverge; i--)
	{
//	WScript.Echo(i);
		WriteXmlLine(tso, 2 + i, '</Directory>');
	}
	// Add end path nodes:
	for (i = iDiverge; i < aEnd.length; i++)
	{
		var ShortName = aEndShort[i];
		var LongName = aEnd[i];
		var Id = MakeId(aEnd[i]);

		// Check if this directory Id has been used before:
		var fFoundDirId = false;
		for (iDir = 0; iDir < DirIdMatches.length; iDir++)
		{
			if (DirIdMatches[iDir].Id == Id)
			{
				var Index = DirIdMatches[iDir].Index + 1;
				Id = Id + Index;
				DirIdMatches[iDir].Index = Index;
				fFoundDirId = true;
				iDir = DirIdMatches.length;
			}
		}
		if (!fFoundDirId)
		{
			var Dir = new Object();
			Dir.Id = Id;
			Dir.Index = 1;
			DirIdMatches.push(Dir);
		}

		if (LongName == ShortName)
			WriteXmlLine(tso, 3 + i, '<Directory Id="' + Id + '" SourceName="' + aEndShort[i] + '">');
		else
			WriteXmlLine(tso, 3 + i, '<Directory Id="' + Id + '" SourceName="' + aEndShort[i] + '" LongSource="' + aEnd[i] + '">');
	}

	var RetObj = new Object();
	RetObj.Indent = aEnd.length;
	if (aEnd.length > 0)
		RetObj.Id = aEnd[aEnd.length - 1];
	return RetObj;
}

function GetShortFileName(LongPath)
{
	var FileSpec = "C:\\FW\\" + LongPath;
//	WScript.Echo(FileSpec);
	try
	{
		var f = fso.GetFile(FileSpec);
	}
	catch (e)
	{
//WScript.Echo(FileSpec);
		return "TODO";
	}
	return f.ShortName;
}

// Returns a suitable Id based on the given name. (Removes spaces, etc.)
// Identifiers may contain ASCII characters A-Z, a-z, digits, underscores (_), or periods (.).
// Every identifier must begin with either a letter or an underscore.
function MakeId(Name)
{
	var Id = Name.split("");
	var ValidChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_.";
	var ValidStartChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_";

	for (iChar = 0; iChar < Id.length; iChar++)
		if (ValidChars.indexOf(Id[iChar]) == -1)
			Id[iChar] = "_";

	if (ValidStartChars.slice(0, ValidChars.length - 1).indexOf(Id[0]) == -1)
		return "_" + Id.join("");
	return Id.join("");
}

// Creates 8.3 names for given folder names. This has to be a little different from the
// GetShortFileName() function, because it is anticipated that the folders do not yet
// exist.
function GetShortFolderNames(DestFolders)
{
	// Create each folder path in a temporary parent folder:
	var ParentFolder = "__TEMP__";
	fso.CreateFolder(ParentFolder);

	for (i = 0; i < DestFolders.length; i++)
		MakeSureFolderExists(ParentFolder + "\\" + DestFolders[i]);

	var ShortDestFolders = new Array();

	for (i = 0; i < DestFolders.length; i++)
	{
		var FolderSpec = ParentFolder + "\\" + DestFolders[i];
		var ShortName;
//		WScript.Echo(FolderSpec);
		try
		{
			var f = fso.GetFolder(FolderSpec);
			// Get the full path of the current folder:
			ShortName = f.ShortPath;
			// Remove the path up to and including the ParentFolder:
			var iParent = ShortName.indexOf(ParentFolder);
			if (iParent > -1)
				ShortName = ShortName.slice(iParent + 1 + ParentFolder.length);
		}
		catch (e)
		{
			ShortName = "TODO";
		}
//WScript.Echo(FolderSpec + ", " + ShortName);
		ShortDestFolders.push(ShortName);
	}

	fso.DeleteFolder(ParentFolder);
	return ShortDestFolders;
}

// Create the specified folder path, if it doesn't already exist.
// Thanks, Jeff!
//	Note - Does not handle \\LS-ELMER\ type directory creation.
function MakeSureFolderExists(strDir)
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

// Generates a list of files using the given DOS file specification, which may include
// wildcards. Returns an array of strings listing full path to each file.
// DOS file attributes also can be specified.
// Works by using the DOS dir command, redirecting output to a temp file, then
// reading in the file.
// Filters out any .svn folders (Subversion metadata).
function GetFileList(FileSpec, RecurseSubfolders, Attributes)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	if (Attributes)
		Attributes += '-D'; // Force removal of folders from list
	else
		Attributes = '-D';

	// Get the root folder at the base of the search:
	var LocalRootFolder;
	if (fso.FolderExists(FileSpec))
		LocalRootFolder = FileSpec;
	else
	{
		var iLastBackslash = FileSpec.lastIndexOf("\\");
		LocalRootFolder = FileSpec.substr(0, iLastBackslash);
		if (!fso.FolderExists(LocalRootFolder))
		{
			Exception = new Object();
			Exception.description = "Source specification '" + FileSpec + "' does not refer to a valid, accessible folder.";
			throw(Exception);
		}
	}
	// Build DOS dir command:
	Cmd = 'cmd /Q /D /C dir "' + FileSpec + '" /B';
	if (RecurseSubfolders)
		Cmd += ' /S';
	if (Attributes)
		Cmd += ' /A:' + Attributes;

	// Get path to temp file:
	var TempFolderName = fso.GetSpecialFolder(2);
	var TempFileName = fso.GetTempName();
	var TempFilePath = fso.BuildPath(TempFolderName, TempFileName);

	// Specify redirection to temp file in the DOS command:
	Cmd += ' >"' + TempFilePath + '"';

	// Run DOS command:
	shellObj.Run(Cmd, 0, true);

	// Read resulting file:
	var File = fso.OpenTextFile(TempFilePath, 1);
	var FileList = new Array();
	var Index = 0;
	while (!File.AtEndOfStream)
	{
		var CurrentFile;
		// If we were recursing folders, the full path will be given already:
		if (RecurseSubfolders)
			CurrentFile = File.ReadLine();
		else // we have to add the root folder to the file name
			CurrentFile = fso.BuildPath(LocalRootFolder, File.ReadLine());

		// Make sure there is nothing from Subversion in the path:
		if (CurrentFile.search(".svn") < 0)
		{
			FileList[Index] = CurrentFile;
			Index++;
		}
	}
	File.Close();
	fso.DeleteFile(TempFilePath);

	return FileList;
}

// Checks if the given Source File is a DLL. If so, it calls WriteSpecificRegInfo()
// to produce any registration info there may be and write it into the given
// text stream.
function WriteRegInfo(FullSource, FileId, DirId, tso, Indent)
{
	// Test if we have a DLL:
	if (FullSource.slice(FullSource.length - 4).toLowerCase() != ".dll")
		return;
	// Test if the file actually exists:
	if (!fso.FileExists(FullSource))
		return;

//WScript.Echo(FullSource + " is a DLL");

	// Get regular COM info:
	WriteSpecificRegInfo('-s "' + FullSource + '"', FullSource, FileId, DirId, tso, Indent);
	// Get COM Interop info:
	WriteSpecificRegInfo('-c "' + FullSource + '"', FullSource, FileId, DirId, tso, Indent);
}

// Calls the WIX Tallow utility to produce specified registration info if it
// exists.
// This then gets written into the given tso text stream, using the specified
// indentation level.
// The parameters FileId and DirId are used to refer to the Component file and
// directory, should any reg info refer to the full path as it is on our machine.
function WriteSpecificRegInfo(Options, FullSource, FileId, DirId, tso, Indent)
{
	// Call Tallow to get any COM registry info into a temp file:
	var TempXmlFile = "temp.xml";
	var Cmd = 'cmd /Q /D /C  Tallow -nologo ' + Options + ' >"' + TempXmlFile + '"';
//WScript.Echo(Cmd);
	shellObj.Run(Cmd, 0, true);
//WScript.Echo("Check temp.xml now!");

	var xmlTemp = new ActiveXObject("Msxml2.DOMDocument.4.0");
	xmlTemp.async = false;
	xmlTemp.setProperty("SelectionNamespaces", 'xmlns:wix="http://schemas.microsoft.com/wix/2003/01/wi"');
	xmlTemp.load(TempXmlFile);
	if (xmlTemp.parseError.errorCode != 0)
	{
		var myErr = xmlTemp.parseError;
		WScript.Echo("XML error in " + TempXmlFile + ": " + myErr.reason + "\non line " + myErr.line + " at position " + myErr.linepos);
		return;
	}
	var RegNodes = xmlTemp.selectNodes("//wix:Registry");
	if (RegNodes.length > 0)
	{
		// Get versions of full source and its folder with both backslashes and forward slashes:
		var iLastBackslash = FullSource.lastIndexOf("\\");
		var SourceFolder = FullSource.slice(0, iLastBackslash);
		var FullSourceForward = FullSource.replace(/\\/g, "/");
		var SourceFolderForward = SourceFolder.replace(/\\/g, "/");

		for (iReg = 0; iReg < RegNodes.length; iReg++)
		{
			var Xml = RegNodes[iReg].xml;

			// Remove xmlns attribute:
			Xml = Xml.replace('xmlns="http://schemas.microsoft.com/wix/2003/01/wi"',"");

			// Replace any occurrence of the path with the variable equivalent:
			var iFound = Xml.toLowerCase().indexOf(FullSource.toLowerCase());
			var NewXml = Xml;
			if (iFound != -1)
				NewXml = Xml.slice(0, iFound) + "[#" + FileId + "]" + Xml.slice(iFound + FullSource.length);
			Xml = NewXml;

			iFound = Xml.toLowerCase().indexOf(FullSourceForward.toLowerCase());
			if (iFound != -1)
				NewXml = Xml.slice(0, iFound) + "[#" + FileId + "]" + Xml.slice(iFound + FullSourceForward.length);
			Xml = NewXml;

			iFound = Xml.toLowerCase().indexOf(SourceFolder.toLowerCase());
			if (iFound != -1)
				NewXml = Xml.slice(0, iFound) + "[#" + FileId + "]" + Xml.slice(iFound + SourceFolder.length);
			Xml = NewXml;

			iFound = Xml.toLowerCase().indexOf(SourceFolderForward.toLowerCase());
			if (iFound != -1)
				NewXml = Xml.slice(0, iFound) + "[#" + FileId + "]" + Xml.slice(iFound + SourceFolderForward.length);
			Xml = NewXml;

			WriteXmlLine(tso, Indent, Xml);
		}
	}
	if (fso.FileExists(TempXmlFile))
		fso.DeleteFile(TempXmlFile);
}

// Writes the given line of text to the given XML file, using the given number of indentation tabs.
function WriteXmlLine(tso, Indent, Line)
{
	var iIndent;
	for (iIndent = 0; iIndent < Indent * TabSpaces; iIndent++)
		tso.Write(' ');
	tso.WriteLine(Line);
}

function CreatGuid()
{
	return new ActiveXObject("Scriptlet.Typelib").Guid.substr(1,36);
}

function LogError(Msg)
{
	var tsoLog = fso.OpenTextFile("Errors.txt", 8, true, -1);
	tsoLog.WriteLine(Msg);
	tsoLog.Close();
}

function InitErrorLog()
{
	var tsoLog = fso.OpenTextFile("Errors.txt", 2, true, -1);
	tsoLog.Close();
}
