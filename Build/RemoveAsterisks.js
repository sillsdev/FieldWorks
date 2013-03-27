// JScript source code to remove all asterisk characters from a specified text file.
// This is used to tidy up the MD5 hash file produced by the overnight Windows build.

if (WScript.Arguments.Length < 1)
	WScript.Quit(-1);

var FilePath = WScript.Arguments.Item(0);

var TempFile = FilePath + ".new";

var fso = new ActiveXObject("Scripting.FileSystemObject");
var tsoRead = fso.OpenTextFile(FilePath, 1, false);
var tsoWrite = fso.CreateTextFile(TempFile, true);
while (!tsoRead.AtEndOfStream)
{
	var line = tsoRead.ReadLine();
	line = line.replace("*", "");
	tsoWrite.WriteLine(line);
}
tsoRead.Close();
tsoWrite.Close();
fso.DeleteFile(FilePath);
fso.MoveFile(TempFile, FilePath);
