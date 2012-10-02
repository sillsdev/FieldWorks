/* This script works on the directory where it resides.
	It processes all files.
	It changes:
	HelloWorld\Simple to DllClient,
	HelloWorld to DllClient, HELLOWORLD to DLLCLIENT,
	HwApp to DcApp, HwMainWnd to DcMainWnd, HwClientWnd to DcClientWnd
	HW_SRC to DC_SRC, HW_RES to DC_RES,
	m_qhwcw to m_qdccw
	*/

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
function Echo(strMsg)
{
	WScript.Echo(strMsg);
}

// Create some commonly used objects.
var shellObj = new ActiveXObject("WScript.Shell");
var fso = new ActiveXObject("Scripting.FileSystemObject");

var sRoot = "."; // eventually from command line arg?

// Predefined constants.
ForReading = 1;
FormatASCII = 0;

/*----------------------------------------------------------------------------------------------
	Function to convert a file.
	Arguments:
		name: the file name, sans path but with extension
		path: prefix, from . to the directory containing the file
----------------------------------------------------------------------------------------------*/
function FixFiles(strName, strPath)
{
	var projName = "DllClient";
	var projNameCaps = "DLLCLIENT";
	var projInitials = "dc";
	var projInitialsCaps = "DC";
	var projInitialsOneCap = "Dc";

	var sFileName = strPath + strName ;
	var strNewName = strName.replace("HelloWorld", projName);
	var sFileNameOut = strPath + "output\\" + strNewName;

	var ts = fso.OpenTextFile(sFileName, ForReading, false, FormatASCII);
	var tsout = fso.CreateTextFile(sFileNameOut, true);

	while (!ts.atEndOfStream)
	{
		var str = ts.ReadLine();

		str = str.replace("Hello World", "Dll Client application");
		str = str.replace("HelloWorld\\Simple", projName);
		str = str.replace("HelloWorld", projName);
		str = str.replace("HELLOWORLD", projNameCaps);

		// These ones sometimes occur twice on a line, so we need a global replace
		str = str.replace(RegExp("HwApp","g"), projInitialsOneCap + "App");
		str = str.replace(RegExp("HwMainWnd", "g"), projInitialsOneCap + "MainWnd");
		str = str.replace(RegExp("HwClientWnd", "g"), projInitialsOneCap + "ClientWnd");
		str = str.replace("HW_SRC", projInitialsCaps + "_SRC");
		str = str.replace("HW_RES", projInitialsCaps + "_RES");
		str = str.replace("m_qhwcw", "m_" + projInitials + "cw");

		tsout.WriteLine(str);
	}
	ts.Close();
	tsout.Close();
}

/*----------------------------------------------------------------------------------------------
	Function to process a folder.
		(b) call FixBanner for every file in the folder
----------------------------------------------------------------------------------------------*/
function Process(folder, sPath)
{
	var enumerator = new Enumerator(folder.SubFolders);
	while (!enumerator.atEnd())
	{
		if (!(enumerator.item().Name == "output" || enumerator.item().Name == "Output"))
		{
			Process(enumerator.item(), sPath + enumerator.item().Name + "\\");
		}
		enumerator.moveNext();
	}
	enumerator = new Enumerator(folder.Files);
	while (!enumerator.atEnd())
	{
		var file = enumerator.item();
		if (!file.Name.match(RegExp("\\.ico$")))
			FixFiles(file.Name, sPath);
		enumerator.moveNext();
	}
}

var folRoot = fso.GetFolder(sRoot);
Process(folRoot, sRoot + "\\");
