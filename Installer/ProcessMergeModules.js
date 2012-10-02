/*
JScript to process the MergeModules.wxs WIX fragment, as follows:

1) Convert relative source paths to absolute. This typically involves prefixing the existing
source path with "C:\FW\", or wherever the root FW folder happens to be.

The processed fragment is written to ProcessedMergeModules.wxs.
*/

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
var xmlMergeModules = new ActiveXObject("Msxml2.DOMDocument.4.0");
xmlMergeModules.async = false;
xmlMergeModules.setProperty("SelectionNamespaces", 'xmlns:wix="http://schemas.microsoft.com/wix/2003/01/wi"');
xmlMergeModules.load("MergeModules.wxs");
xmlMergeModules.preserveWhiteSpace = true;

if (xmlMergeModules.parseError.errorCode != 0)
{
	var myErr = xmlMergeModules.parseError;
	WScript.Echo("XML error in MergeModules.wxs: " + myErr.reason + "\non line " + myErr.line + " at position " + myErr.linepos);
	WScript.Quit();
}

// Get all file nodes:
var MergeNodes = xmlMergeModules.selectNodes("//wix:Merge");
for (i = 0; i < MergeNodes.length; i++)
{
	var MergeNode = MergeNodes[i];
	var MergePath = MergeNode.getAttribute("SourceFile");

	// Prefix the source with our Root folder, unless the source is already an absolute path:
	if (MergePath.slice(1,2) != ":" && MergePath.slice(0,1) != "\\")
	{
		// Source is relative, so add in the Root:
		MergePath = fso.BuildPath(RootFolder, MergePath);
		MergeNode.setAttribute("SourceFile", MergePath);
	}
} // Next merge module

// Save the new XML file:
xmlMergeModules.save("ProcessedMergeModules.wxs");
