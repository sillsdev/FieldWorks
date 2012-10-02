/*
JScript to process the files.wxs WIX fragment, as follows:

1) Convert relative source paths to absolute. This typically involves prefixing the existing
source path with "C:\FW\", or wherever the root FW folder happens to be.

2) Add registry info. This means COM registration (for non-.NET DLLs), COM interoperability
registration (for .NET DLLs), and COM server registration for a limited number of .EXE files.
Registration is attempted for all DLLs, but the .EXE files to be tested are specified
in this file (hard-coded).

3) Add Assembly attribute info, for .NET assemblies.

4) Make sure each component containing only one file has that file set as the component key.

The processed fragment is written to ProcessedFiles.wxs.
*/

var Build = "release";

// See if the word "debug" appears as the first command line argument:
if (WScript.Arguments.Length < 1)
	WScript.Echo("WARNING - expected 1 argument: defaulting to " + Build + " build.");
else
	Build = WScript.Arguments.Item(0).toLowerCase();

if (Build != "debug" && Build != "release")
{
	WScript.Echo("ERROR - needs 1 argument: debug or release.");
	WScript.Quit(0);
}

var ComExeFiles = new Array();
ComExeFiles.push("FwListEditor.exe");
ComExeFiles.push("FwNotebook.exe");
ComExeFiles.push("WorldPad.exe");

var fso = new ActiveXObject("Scripting.FileSystemObject");
var shellObj = new ActiveXObject("WScript.Shell");

// Get root path details:
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

// Set up the XML parser, including namespaces that are in WIX:
var xmlFiles = new ActiveXObject("Msxml2.DOMDocument.4.0");
xmlFiles.async = false;
xmlFiles.setProperty("SelectionNamespaces", 'xmlns:wix="http://schemas.microsoft.com/wix/2003/01/wi"');
xmlFiles.load("Files.wxs");
xmlFiles.preserveWhiteSpace = true;

if (xmlFiles.parseError.errorCode != 0)
{
	var myErr = xmlFiles.parseError;
	ReportError("XML error in Files.wxs: " + myErr.reason + "\non line " + myErr.line + " at position " + myErr.linepos);
	WScript.Quit(-1);
}

// Get all file nodes:
var FileNodes = xmlFiles.selectNodes("//wix:File");
for (i = 0; i < FileNodes.length; i++)
{
	var FileNode = FileNodes[i];
	// Replace variable ${config} with build type:
	var FilePath = FileNode.getAttribute("Source").replace(/\\\${config}/i, "\\" + Build);

	// Prefix the source with our Root folder, unless the source is already an absolute path:
	if (FilePath.slice(1,2) != ":" && FilePath.slice(0,1) != "\\")
	{
		// Source is relative, so add in the Root:
		FilePath = fso.BuildPath(RootFolder, FilePath);
		FileNode.setAttribute("Source", FilePath);
	}
	// Test if file was sepcified to omit registration info:
	if (FileNode.getAttribute("IgnoreRegistration") == "true")
		FileNode.removeAttribute("IgnoreRegistration");
	else
	{
		// Try running Tallow on qualified files to get any registration data there may be:
		try
		{
			AddRegInfo(FileNode);
		}
		catch (ErrorMsg)
		{
			ReportError("COM registration error - system registry data detected: " + ErrorMsg);
			WScript.Quit(-1);
		}
		// Add assembly data, where relevant:
		AddAssemblyInfo(FileNode);
	}
	// If there is only one file node in the component, make sure its KeyPath attribute is set to "yes":
	var ComponentNode = FileNode.selectSingleNode("..");
	if (ComponentNode.selectNodes("wix:File").length == 1)
		FileNode.setAttribute("KeyPath", "yes");
} // Next file

// Save the new XML file:
xmlFiles.save("ProcessedFiles.wxs");


// Checks if the given File node is a DLL or EXE. If so, it runs the PEParser.exe utility
// to determine if the file is a .NET assembly. If so, three relevant attributes are added.
function AddAssemblyInfo(FileNode)
{
	var FilePath = FileNode.getAttribute("Source");

	// Test if the file actually exists:
	if (!fso.FileExists(FilePath))
		return;

	// Test if we have a DLL or EXE:
	var Extension = FilePath.slice(FilePath.length - 4).toLowerCase();
	if (Extension == ".dll" || Extension == ".exe")
	{
		// Run PEParser on the file:
		if (1 == shellObj.Run('PEParser "' + FilePath + '"', 0, true))
		{
			// The file is a .NET assembly, so add the attributes:
			var FileId = FileNode.getAttribute("Id");
			FileNode.setAttribute("Assembly", ".net");
			FileNode.setAttribute("AssemblyApplication", FileId);
			FileNode.setAttribute("AssemblyManifest", FileId);
		}
	}
}


// Checks if the given File node is a DLL or COM EXE. If so, it calls WriteSpecificRegInfo()
// to produce any registration info there may be and adds it into the given ComponentNode.
function AddRegInfo(FileNode)
{
	var FilePath = FileNode.getAttribute("Source");

	// Test if the file actually exists:
	if (!fso.FileExists(FilePath))
		return;

	// Test if we have a DLL:
	if (FilePath.slice(FilePath.length - 4).toLowerCase() == ".dll")
	{
		// Get regular COM info:
		AddSpecificRegInfo(FileNode, "-s " + FilePath);
		// Get COM Interop info:
		AddSpecificRegInfo(FileNode, "-c " + FilePath);
	}
	else
	{
		// Test if we have a Type Library:
		if (FilePath.slice(FilePath.length - 4).toLowerCase() == ".tlb")
		{
			// Get TLB reg info:
			AddSpecificRegInfo(FileNode, "-t " + FilePath);
		}
		else
		{
			// Test if we have a listed .exe COM server:
			var iLastBackslash = FilePath.lastIndexOf("\\");
			var FileName = FilePath.slice(iLastBackslash + 1);
			var ComExeFile = null;
			for (a = 0; a < ComExeFiles.length; a++)
			{
				if (ComExeFiles[a] == FileName)
				{
					ComExeFile = FileName;
					a = ComExeFiles.length;
				}
			}
			if (ComExeFile)
			{
				AddSpecificRegInfo(FileNode, "-e " + FilePath + " #-RegRedirect");
			}
		}
	}
}

// Calls the WIX Tallow utility to produce specified registration info if it
// exists.
// This then gets written into the given ComponentNode.
function AddSpecificRegInfo(FileNode, TallowCmd)
{
	// Get parent node of current file node - the component node:
	var ComponentNode = FileNode.selectSingleNode("..");
	// Get Component ID:
	var CompId = ComponentNode.getAttribute("Id");
	// Get full path of file:
	var FilePath = FileNode.getAttribute("Source");
	// Get File ID:
	var FileId = FileNode.getAttribute("Id");

	// Set up XML parser for Tallow ouput:
	var TempXmlFile = "temp.xml";
	var xmlTemp = new ActiveXObject("Msxml2.DOMDocument.4.0");
	xmlTemp.async = false;
	xmlTemp.setProperty("SelectionNamespaces", 'xmlns:wix="http://schemas.microsoft.com/wix/2003/01/wi"');

	// Call Tallow to get any COM registry info into a temp file:
	var Cmd = 'cmd /Q /D /C  Tallow -nologo ' + TallowCmd + ' >"' + TempXmlFile + '"';
//WScript.Echo(Cmd);
	if (shellObj.Run(Cmd, 0, true) != 0)
	{
		var NewFragment = new ActiveXObject("Msxml2.DOMDocument.4.0");
		NewFragment.preserveWhiteSpace = true;

		NewFragment.setProperty("SelectionNamespaces", 'xmlns:wix="http://schemas.microsoft.com/wix/2003/01/wi"');
		NewFragment.loadXML('<?xml version="1.0"?><Wix xmlns="http://schemas.microsoft.com/wix/2003/01/wi"><Error GetFilesRegInfo="Error encountered while running Tallow with ' + TallowCmd + '" /></Wix>');

		var NewNode = NewFragment.selectSingleNode("//wix:Error");
		ComponentNode.appendChild(NewNode);
		NewNode.removeAttribute("xmlns");
		ComponentNode.appendChild(NewFragment.createTextNode("\n"));

		return;
	}
//WScript.Echo("Check temp.xml now!");

	xmlTemp.load(TempXmlFile);
	if (xmlTemp.parseError.errorCode != 0)
	{
		var myErr = xmlTemp.parseError;
		WScript.Echo("XML error in " + TempXmlFile + ": " + myErr.reason + "\non line " + myErr.line + " at position " + myErr.linepos);
		return;
	}
	fso.DeleteFile(TempXmlFile);
	var RegNodes = xmlTemp.selectNodes("//wix:Registry");
	var ErrorNodes = xmlTemp.selectNodes("//wix:Error");

	// Get versions of file path and its folder with both backslashes and forward slashes:
	var iLastBackslash = FilePath.lastIndexOf("\\");
	var SourceFolder = FilePath.slice(0, iLastBackslash);
	var FilePathForward = FilePath.replace(/\\/g, "/");
	var SourceFolderForward = FilePath.replace(/\\/g, "/");

	// Graft Registry nodes into ComponentNode:
	for (b = 0; b < RegNodes.length; b++)
	{
		// Get text of reg node:
		var RegText = RegNodes[b].xml;

		// Replace any occurrence of the path with the variable equivalent:
		var iFound = RegText.toLowerCase().indexOf(FilePath.toLowerCase());
		var NewXml = RegText;
		if (iFound != -1)
			NewXml = RegText.slice(0, iFound) + "[#" + FileId + "]" + RegText.slice(iFound + FilePath.length);
		RegText = NewXml;

		iFound = RegText.toLowerCase().indexOf(FilePathForward.toLowerCase());
		if (iFound != -1)
			NewXml = RegText.slice(0, iFound) + "[#" + FileId + "]" + RegText.slice(iFound + FilePathForward.length);
		RegText = NewXml;

		iFound = RegText.toLowerCase().indexOf(SourceFolder.toLowerCase());
		if (iFound != -1)
			NewXml = RegText.slice(0, iFound) + "[$" + CompId + "]" + RegText.slice(iFound + SourceFolder.length);
		RegText = NewXml;

		iFound = RegText.toLowerCase().indexOf(SourceFolderForward.toLowerCase());
		if (iFound != -1)
			NewXml = RegText.slice(0, iFound) + "[$" + CompId + "]" + RegText.slice(iFound + SourceFolderForward.length);
		RegText = NewXml;

		if (FindSystemRegKeys(RegText))
			throw (FileId + " from " + FilePath + ":\n" + RegText);

		var NewFragment = new ActiveXObject("Msxml2.DOMDocument.4.0");
		NewFragment.preserveWhiteSpace = true;

		NewFragment.setProperty("SelectionNamespaces", 'xmlns:wix="http://schemas.microsoft.com/wix/2003/01/wi"');
		NewFragment.loadXML(RegText);

		var NewRegNode = NewFragment.selectSingleNode("//wix:Registry");
		ComponentNode.appendChild(NewRegNode);
		NewRegNode.removeAttribute("xmlns");
		ComponentNode.appendChild(NewFragment.createTextNode("\n"));
	}

	// Graft Errpr nodes into ComponentNode:
	for (b = 0; b < ErrorNodes.length; b++)
		ComponentNode.appendChild(ErrorNodes[b].cloneNode(false));
}

// Searches the given text for registry keys that look like they may be system keys.
// This helps us trap attempts to overwrite system registry keys, like FW 4.0 installer did.
function FindSystemRegKeys(RegText)
{
	if (RegText.search(/CLSID\\\{00000/i) != -1)
		return true;
	if (RegText.search(/Interface\\\{00000/i) != -1)
		return true;

	return false;
}

// Creates error log file:
function ReportError(text)
{
	var tso = fso.OpenTextFile("ProcessFiles.log", 2, true, -1);
	tso.WriteLine(Date() + ": " + text);
	tso.Close();
}
