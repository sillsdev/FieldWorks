/*
JScript to process various WIX fragments, as follows:

1) Remove all features not needed by WorldPad, and their sub-features from Features.wxs.

2) Adjust the UI module: change the yellow bitmaps to green, and remove any special non-WP behavior.

3) Remove the directories not relevant to WP and all their subdirectories from ProcessedFiles.wxs.

4) Remove custom actions not relelvant to WP.

5) Examine every component to see if it is referred to in Features.wxs. If it isn't
then delete it.

6) Change the name of the installer from FieldWorks to WorldPad.

The processed files are written to <filename>_WP.wxs.
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

// 1) Remove DN, FLEX, TE, TLE, and LOC features and all their subfeatures:
RemoveFeature("DN");
RemoveFeature("FLEX");
RemoveFeature("TE");
RemoveFeature("TLE");
RemoveFeature("LOC");


// Set up the files XML parser:
var xmlFiles = GetXmlParser("ProcessedFiles.wxs");

// 2) Adjust the UI module: change the yellow bitmaps to green, and remove any special non-WP behavior.
var xmlUI = GetXmlParser("FwUI.wxs");
var TopBitmap = xmlUI.selectSingleNode("//wix:Binary[@SourceFile='Binary\\FieldWorks.topyellow.bmp']");
TopBitmap.setAttribute("SourceFile", "Binary\\FieldWorks.topgreen.bmp");
var SideBitmap = xmlUI.selectSingleNode("//wix:Binary[@SourceFile='Binary\\FieldWorks.sideyellowfabric.bmp']");
SideBitmap.setAttribute("SourceFile", "Binary\\FieldWorks.sidegreenfabric.bmp");
var FrenchTEEvents = xmlUI.selectNodes("//wix:Publish[@Value='French_TE']");
FrenchTEEvents.removeAll();

// 3) Remove the directories not relevant to WP and all their subdirectories:
RemoveDirectory("Data_Notebook");
RemoveDirectory("DataMigration");
RemoveDirectory("ExportOptions");
RemoveDirectory("fr");
RemoveDirectory("Language_Explorer");
RemoveDirectory("Translation_Editor");
RemoveDirectory("XDE");
RemoveDirectory("Data");
RemoveDirectory("WW_ConceptualIntro");
RemoveDirectory("XLingPap");

// 4) Remove custom action not relelvant to WP:
var xmlActions = GetXmlParser("Actions.wxs");

RemoveCustomAction("SetLocalAppFolderPathSIL");
RemoveCustomAction("InitSqlServer");
RemoveCustomAction("RemoveSampleDBs");
RemoveCustomAction("SetFirewallDataTE");
RemoveCustomAction("SetFirewallDataFlex");
RemoveCustomAction("SetFirewallData");
RemoveCustomAction("SetupFirewall");

// 5) Examine every component to see if it is referred to in Features. If it isn't then delete it:
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

// Do the same for Actions components:
TestAndRemoveComponents(xmlActions);

// Do the same for merge modules:
var xmlMergeModules = GetXmlParser("ProcessedMergeModules.wxs");
TestAndRemoveMergeModules(xmlMergeModules);

// 6) Change the name of the installer from FieldWorks to WorldPad.
var xmlFW = GetXmlParser("FW.wxs");
var Product = xmlFW.selectSingleNode("//wix:Product");
Product.setAttribute("Name", "WorldPad 3.0");


// Save the new XML files:
xmlFW.save("FW_WP.wxs");
xmlFiles.save("ProcessedFiles_WP.wxs");
xmlFeatures.save("Features_WP.wxs");
xmlRegistry.save("Registry_WP.wxs");
xmlCopyFiles.save("CopyFiles_WP.wxs");
xmlShortcuts.save("Shortcuts_WP.wxs");
xmlUI.save("FwUI_WP.wxs");
xmlActions.save("Actions_WP.wxs");
xmlMergeModules.save("ProcessedMergeModules_WP.wxs");


// Removes the named custom action:
function RemoveCustomAction(ActionName)
{
	// Remove CustomAction node(s):
	var CustomActionNode = xmlActions.selectSingleNode("//wix:CustomAction[@Id='" + ActionName + "']");
	CustomActionNode.selectSingleNode("..").removeChild(CustomActionNode);

	// Remove Custom node(s):
	var CustomNode = xmlActions.selectSingleNode("//wix:Custom[@Action='" + ActionName + "']");
	if (CustomNode != null)
		CustomNode.selectSingleNode("..").removeChild(CustomNode);
}

// Deletes the given directory, and all its files and subdirectories.
function RemoveDirectory(DirectoryId)
{
	var Directory = xmlFiles.selectSingleNode("//wix:Directory[@Id='" + DirectoryId + "']");
	Directory.selectSingleNode("..").removeChild(Directory);
}

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

// Tests every Merge node in the given file to see if it is referred to in Features.
// If it isn't then delete it.
function TestAndRemoveMergeModules(xmlFile)
{
	// Get all Merge nodes:
	var MergeNodes = xmlFile.selectNodes("//wix:Merge");

	for (i = 0; i < MergeNodes.length; i++)
	{
		// Get current Merge Id:
		var MergeMod = MergeNodes[i];
		var MergeId = MergeMod.getAttribute("Id");
		// check against Features:
		var FeatureMmRef = xmlFeatures.selectSingleNode("//wix:MergeRef[@Id='" + MergeId + "']");
		if (!FeatureMmRef)
			MergeMod.selectSingleNode("..").removeChild(MergeMod);
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
