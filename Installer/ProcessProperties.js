/*
JScript to process the Properties.wxs WIX fragment, as follows:

1) Change the BUILD_DATE property to today's date

The processed fragment is written to ProcessedProperties.wxs.
*/

var fso = new ActiveXObject("Scripting.FileSystemObject");
var shellObj = new ActiveXObject("WScript.Shell");

// Set up the XML parser, including namespaces that are in WIX:
var xmlProperties = new ActiveXObject("Msxml2.DOMDocument.4.0");
xmlProperties.async = false;
xmlProperties.setProperty("SelectionNamespaces", 'xmlns:wix="http://schemas.microsoft.com/wix/2003/01/wi"');
xmlProperties.load("Properties.wxs");
xmlProperties.preserveWhiteSpace = true;

if (xmlProperties.parseError.errorCode != 0)
{
	var myErr = xmlProperties.parseError;
	WScript.Echo("XML error in Properties.xml: " + myErr.reason + "\non line " + myErr.line + " at position " + myErr.linepos);
	WScript.Quit();
}

// Get BUILD_DATE Property node:
var BuildDateNode = xmlProperties.selectSingleNode("//wix:Property[@Id='BUILD_DATE']");
if (BuildDateNode)
{
	// Get today's date:
	var Date = new Date();
	var Year = Date.getFullYear();
	var Month = 1 + Date.getMonth();
	var DayOfMonth = Date.getDate();
	var DateString = Year + "-";
	if (Month < 10)
		DateString += "0";
	DateString += Month + "-";
	if (DayOfMonth < 10)
		DateString += "0";
	DateString += DayOfMonth;

	BuildDateNode.text = DateString;
}

// Save the new XML file:
xmlProperties.save("ProcessedProperties.wxs");
