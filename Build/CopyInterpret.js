// JScript source code to copy a specified file to a specified folder, interpreting the
// destination folder path as follows:

// Replace ${date} with the date, in the format yyyy-mm-dd

// This script ultimately calls the DOS copy command, so wildcards etc can be used in the same way.
// This script also ensures that the destination folder path exists, by creating it if need be.
// However, the destination must be a folder path, not a file path.

if (WScript.Arguments.Length != 2)
	WScript.Quit(-1);

var source = WScript.Arguments.Item(0);
var destination = WScript.Arguments.Item(1);

var date = new Date();
var yearString = date.getFullYear();
var month = 1 + date.getMonth();
var monthString = month < 10 ? "0" + month : month;
var day = date.getDate();
var dayString = day < 10 ? "0" + day : day;
var dateString = yearString + "-" + monthString + "-" + dayString;

destination = destination.replace("${date}", dateString);
MakeSureFolderExists(destination);

var shellObj = new ActiveXObject("WScript.Shell");
var Cmd = 'cmd /Q /D /C  copy "' + source + '" "' + destination + '"';
WScript.Echo(Cmd);
if (shellObj.Run(Cmd, 0, true) != 0)
	WScript.Quit(-1);


// Create the specified folder path, if it doesn't already exist.
function MakeSureFolderExists(strDir)
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");

	// See if the dir exists.
	if (!fso.FolderExists(strDir))
	{
		// Handle paths of type \\Berea2\ by temporarily masking the initial double-backslash:
		if (strDir.slice(0, 2) == "\\\\")
			strDir = "%%" + strDir.slice(2);

		var aFolder = new Array();
		aFolder = strDir.split("\\");

		// Unmask any initial double-backslash:
		if (aFolder[0].slice(0, 2) == "%%")
			aFolder[0] = "\\\\" + aFolder[0].slice(2);

		var strNewFolder = fso.BuildPath(aFolder[0], "\\");

		// Loop through each folder name and create if not already created
		var iFolder;
		for (iFolder = 1; iFolder < aFolder.length; iFolder++)
		{
			strNewFolder = fso.BuildPath(strNewFolder, aFolder[iFolder]);
			if (!fso.FolderExists(strNewFolder))
				fso.CreateFolder(strNewFolder);
		}
	}
}
