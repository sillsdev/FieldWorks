/*

JScript to generate a partial WIX source for the ICU data files.

The contents of the output file "PartialICU.wxs" should be copied and pasted into
a suitable full ICU WIX source. If pasted within Visual Studio, proper indentation
will be appiled. (None is generated in the output of this script.)

*/

// Relative folder (from this script's root FW folder):
var IcuDataRelPath = "DistFiles\\Icu40";

var fso = new ActiveXObject("Scripting.FileSystemObject");

// Get script path details:
var iLastBackslash = WScript.ScriptFullName.lastIndexOf("\\");
var ScriptPath = WScript.ScriptFullName.slice(0, iLastBackslash);

// Get FW root path and ICU data folder absolute path:
var FwRootPath = ScriptPath.slice(0, ScriptPath.lastIndexOf("\\"));
var IcuDataFolderPath = fso.BuildPath(FwRootPath, IcuDataRelPath);

var Ids = new Array(); // Used to keep all manufactured IDs unique.
var IdInputs = new Array(); // Used to keep all manufactured IDs unique.
var fOutputFirstDirectory = false;

var IcuFileAndFolderTree = GetFileAndFolderTree(IcuDataFolderPath);

var tso = fso.OpenTextFile("PartialICU.wxs", 2, true, -1);
TreeOutput(IcuFileAndFolderTree, tso);
tso.Close();

// Recursively output a WIX fragment describing the given file and folder tree.
function TreeOutput(Tree, tso)
{
	var FolderPath = Tree.FolderPath;
	var FolderName = FolderPath.slice(FolderPath.lastIndexOf("\\") + 1)
	var DirId = MakeId("", FolderName, false);

	tso.WriteLine('<Directory Id="' + DirId + '" Name="' + FolderName + '">');

	// If this is the first directory in the whole tree, then add a WIX component to set
	// permissions on the folder:
	if (!fOutputFirstDirectory)
	{
		fOutputFirstDirectory = true;
		tso.WriteLine('<Component Id="CreateIcuFolder" Guid="' + MakeGuid() + '">');
		tso.WriteLine('<CreateFolder>');
		tso.WriteLine('<Permission Extended="yes" User="AuthenticatedUser" GenericAll="yes" />');
		tso.WriteLine('</CreateFolder>');
		tso.WriteLine('</Component>');
	}

	// Recurse over subfolders:
	var Subfolders = Tree.SubfolderList;
	var folder;
	for (folder = 0; folder < Subfolders.length; folder++)
		TreeOutput(Subfolders[folder], tso);

	// Output files:
	var Files = Tree.FileList;
	var file;
	for (file = 0; file < Files.length; file++)
	{
		var LongName = Files[file].LongName;
		var ShortName = Files[file].ShortName;
		var Id = MakeId("", LongName, false);
		// Make relative path to source by removing FW root path from absolute file path:
		var RelativeSource = Files[file].Path.slice(1 + FwRootPath.length);
		tso.WriteLine('<Component Id="' + Id + '" Guid="' + MakeGuid() + '">');
		tso.Write('<File Id="' + Id + '" Name="' + ShortName + '" ');
		if (LongName != ShortName)
			tso.Write('LongName="' + LongName + '" ');
		if (FolderName == "tools")
			tso.Write('ReadOnly="yes" ');
		tso.WriteLine('Checksum="yes" KeyPath="yes" Source="' + RelativeSource + '"/>');
		tso.WriteLine('</Component>');
	}

	tso.Writeline('</Directory>');
}

WScript.Echo("Done.");

// Returns a suitable Id based on the given name. (Removes spaces, etc.)
// Identifiers may contain ASCII characters A-Z, a-z, digits, underscores (_), or periods (.).
// Every identifier must begin with either a letter or an underscore.
// Space is limited to 72 chars, so after that, we just truncate the string.
// If MatchInputs is set to true, this function remembers the inputs it deals with, so that if the same
// inputs are presented again, the same output is given, but if unique inputs are given,
// the output will be unique.
function MakeId(Prefix, Name, MatchInputs)
{
	if (MatchInputs)
	{
		// Test inputs to see if we've had them before:
		var i;
		for (i = 0; i < IdInputs.length; i++)
		{
			if (IdInputs[i].Prefix == Prefix && IdInputs[i].Name == Name)
				return IdInputs[i].Output;
		}
	}
	var MaxLen = 72;
	var Id = Name.split("");
	var ValidChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_.";

	for (iChar = 0; iChar < Id.length; iChar++)
		if (ValidChars.indexOf(Id[iChar]) == -1)
			Id[iChar] = "_";

	var Candidate = Prefix + Id.join("");

	if (Candidate.length > MaxLen)
		Candidate = Candidate.slice(0, MaxLen);

	// Test Id to make sure it is unique:
	var fUnique = true;
	for (i = 0; i < Ids.length; i++)
	{
		if (Candidate == Ids[i].Root)
		{
			// Candidate is not unique: it needs a numerical suffix; see what next available one is:
			var CurrentMax = Ids[i].MaxIndex + 1;
			Candidate = Candidate.slice(0, MaxLen - 3) + CurrentMax;
			Ids[i].MaxIndex = CurrentMax;
			fUnique = false;
			break;
		}
	}
	// If Id is unique, register it first, before returning it:
	if (fUnique)
	{
		var NewId = new Object();
		NewId.Root = Candidate;
		NewId.MaxIndex = 1;
		Ids.push(NewId);
	}
	if (MatchInputs)
	{
		var NewIdInput = new Object();
		NewIdInput.Prefix = Prefix;
		NewIdInput.Name = Name;
		NewIdInput.Output = Candidate;
		IdInputs.push(NewIdInput);
	}
	return Candidate;
}

function MakeGuid()
{
	return new ActiveXObject("Scriptlet.Typelib").Guid.substr(1,36);
}

// Recurses given FolderPath and returns an object containing two arrays:
// 1) array of objects containing full names, short names and path strings of files in the folder;
// 2) array of immediate subfolders, which recursively contain their files and subfolders.
// The returned object also includes the folder path for itself.
function GetFileAndFolderTree(FolderPath)
{
	var Results = new Object();
	Results.FolderPath = FolderPath;
	Results.FileList = new Array();
	Results.SubfolderList = new Array();

	// Check if current Folder is a Subversion metadata folder:
	if (FolderPath.slice(-4) == ".svn")
		return Results; // Don't include SVN folders.

	// Add files in current folder:
	var Folder = fso.GetFolder(FolderPath);
	var FileIterator = new Enumerator(Folder.files);
	for (; !FileIterator.atEnd(); FileIterator.moveNext())
	{
		var FileObject = new Object();
		FileObject.Path = FileIterator.item().Path;
		FileObject.LongName = FileIterator.item().Name;
		FileObject.ShortName = FileIterator.item().ShortName;
		Results.FileList.push(FileObject);
	}

	// Now recurse all subfolders:
	var SubfolderIterator = new Enumerator(Folder.SubFolders);
	for (; !SubfolderIterator.atEnd(); SubfolderIterator.moveNext())
		Results.SubfolderList.push(GetFileAndFolderTree(SubfolderIterator.item().Path));

	return Results;
}
