/*
JScript to process various WIX fragments, as follows:

1) Remove all TE-only features their sub-features from Features.wxs.

2) Adjust the UI module: change the yellow bitmaps to green, and remove any special TE behavior.

3) Remove the Translation Editor directory and all its subdirectories from ProcessedFiles.wxs.

4) Examine every component to see if it is referred to in Features.wxs. If it isn't
then delete it.

The processed files are written to <filename>_No_TE.wxs.
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

// Set up the features XML parser:
var xmlFeatures = GetXmlParser("Features.wxs");

// 1) Remove TE feature and all subfeatures:
RemoveFeature("TE");
RemoveFeature("French_TE");

// Set up the files XML parser:
var xmlFiles = GetXmlParser("ProcessedFiles.wxs");

// 2) Adjust the UI module: change the yellow bitmaps to green, and remove any special TE behavior.
var xmlUI = GetXmlParser("FwUI.wxs");
var TopBitmap = xmlUI.selectSingleNode("//wix:Binary[@SourceFile='Binary\\FieldWorks.topyellow.bmp']");
TopBitmap.setAttribute("SourceFile", "Binary\\FieldWorks.topgreen.bmp");
var SideBitmap = xmlUI.selectSingleNode("//wix:Binary[@SourceFile='Binary\\FieldWorks.sideyellowfabric.bmp']");
SideBitmap.setAttribute("SourceFile", "Binary\\FieldWorks.sidegreenfabric.bmp");
var FrenchTEEvents = xmlUI.selectNodes("//wix:Publish[@Value='French_TE']");
FrenchTEEvents.removeAll();

// 3) Remove the Translation Editor directory and all its subdirectories:
var TeDirectory = xmlFiles.selectSingleNode("//wix:Directory[@Id='Translation_Editor']");
TeDirectory.selectSingleNode("..").removeChild(TeDirectory);

// 4) Examine every component to see if it is referred to in Features. If it isn't then delete it:
TestAndRemoveComponents(xmlFiles);

// Do the same for Registry components:
var xmlRegistry = GetXmlParser("Registry.wxs");
TestAndRemoveComponents(xmlRegistry);

// Do the same for CopyFiles components:
var xmlCopyFiles = GetXmlParser("CopyFiles.wxs");
TestAndRemoveComponents(xmlCopyFiles);

// Do the same for Shortcuts components:
var xmlShortcuts = GetXmlParser("Shortcuts.wxs");
TestAndRemoveComponents(xmlShortcuts);
DeleteNode(xmlShortcuts, "//wix:Directory[@Id='TEMenu']");

// Save the new XML files:
xmlFiles.save("ProcessedFiles_No_TE.wxs");
xmlFeatures.save("Features_No_TE.wxs");
xmlRegistry.save("Registry_No_TE.wxs");
xmlCopyFiles.save("CopyFiles_No_TE.wxs");
xmlShortcuts.save("Shortcuts_No_TE.wxs");
xmlUI.save("FwUI_No_TE.wxs");


// Deletes from the features list the given feature and any sub-features.
function RemoveFeature(FeatureName)
{
	var Feature = xmlFeatures.selectSingleNode("//wix:Feature[@Id='" + FeatureName + "']");
	Feature.selectSingleNode("..").removeChild(Feature);
}

// Tests every component in the given file to see if it is referred to in Features.
// If it isn't then delete it.
function TestAndRemoveComponents(xmlFile)
{
	// Get all component nodes:
	var ComponentNodes = xmlFile.selectNodes("//wix:Component");

	for (i = 0; i < ComponentNodes.length; i++)
	{
		// Get current component Id:
		var Component = ComponentNodes[i];
		var ComponentId = Component.getAttribute("Id");
		// check against Features:
		var FeatureCompRef = xmlFeatures.selectSingleNode("//wix:ComponentRef[@Id='" + ComponentId + "']");
		if (!FeatureCompRef)
			Component.selectSingleNode("..").removeChild(Component);
	}
}

// Set up an XML parser from the given XML file, including namespaces that are in WIX:
function GetXmlParser(FileName)
{
	var xmlFile = new ActiveXObject("Msxml2.DOMDocument.4.0");
	xmlFile.async = false;
	xmlFile.setProperty("SelectionNamespaces", 'xmlns:wix="http://schemas.microsoft.com/wix/2003/01/wi"');
	xmlFile.load(FileName);
	xmlFile.preserveWhiteSpace = true;

	if (xmlFile.parseError.errorCode != 0)
	{
		var myErr = xmlFile.parseError;
		ReportError("XML error in " + FileName + ": " + myErr.reason + "\non line " + myErr.line + " at position " + myErr.linepos);
		WScript.Quit(-1);
	}
	return xmlFile;
}

function DeleteNode(xmlFile, XPath)
{
	var CondemnedNode = xmlFile.selectSingleNode(XPath);
	CondemnedNode.selectSingleNode("..").removeChild(CondemnedNode);
}
