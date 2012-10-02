// Removes duplicate files from a directory hierarchy.
// Run this script in the root of the hierarchy, and any files duplicated in the hierarchy
// will be "promoted" to their common ancestor folder.


var fso = new ActiveXObject("Scripting.FileSystemObject");
var shellObj = new ActiveXObject("WScript.Shell");
var RootFolder = fso.GetFolder(".").Path;

RemoveDuplicates(RootFolder);

function RemoveDuplicates(FolderName)
{
	// Make an array of all files along with their paths:
	var FlatList = new Array();

	// Recursively process child folders first:
	var Folder = fso.GetFolder(FolderName);
	var fc = new Enumerator(Folder.SubFolders);
	for (; !fc.atEnd(); fc.moveNext())
	{
		FlatList = FlatList.concat(RemoveDuplicates(fc.item().Path));
	}

	// Make a list of this folder's files, along with this path:
	var FileList = new Array();
	fc = new Enumerator(Folder.files);
	for (; !fc.atEnd(); fc.moveNext())
	{
		var FileDetails = new Object();
		FileDetails.Name = fc.item().Name;
		FileDetails.Path = FolderName;
		FileList.push(FileDetails);
	}

	// Now see if any files in this folder are duplicated in the FlatList:
	for (iFile = 0; iFile < FileList.length; iFile++)
	{
		for (iFlat = 0; iFlat < FlatList.length; iFlat++)
		{
			if (FlatList[iFlat].Name == (FileList[iFile].Name))
			{
//WScript.Echo("Deleting " + fso.BuildPath(FlatList[iFlat].Path, FlatList[iFlat].Name));
				// There is a match, so delete the one from the subfolder:
				fso.DeleteFile(fso.BuildPath(FlatList[iFlat].Path, FlatList[iFlat].Name), true);
				FlatList.splice(iFlat, 1);
				iFlat--;
			}
		}
	}

	// Now see if any files in the FlatList are duplicated within the FlatList:
	for (i1 = 0; i1 < FlatList.length; i1++)
	{
		for (i2 = i1 + 1; i2 < FlatList.length; i2++)
		{
			if (FlatList[i1].Name == (FlatList[i2].Name))
			{
//WScript.Echo("Moving " + fso.BuildPath(FlatList[i2].Path, FlatList[i2].Name) + " to " + FolderName + "; deleting " + fso.BuildPath(FlatList[i1].Path, FlatList[i1].Name));
				// There is a match, so promote one to this folder, using the DOS move command:
				var Cmd = 'cmd /Q /D /C move "' + fso.BuildPath(FlatList[i2].Path, FlatList[i2].Name) + '" "' + FolderName + '"';
				shellObj.Run(Cmd, 0, true);
				// Delete the other matching file:
				fso.DeleteFile(fso.BuildPath(FlatList[i1].Path, FlatList[i1].Name), true);
				// Now add promoted file to FileList:
				var FileDetails = new Object();
				FileDetails.Name = FlatList[i2].Name;
				FileDetails.Path = FolderName;
				FileList.push(FileDetails);

				// Amend FlatList records. This involves deleting the later entry first,
				// then the newer one, while we have the correct indexes:
				FlatList.splice(i2, 1); // Delete one match
				FlatList.splice(i1, 1); // Delete the other match
				// We will now leave the inner loop, as the file from the outer loop
				// has gone anyway, but first decrement the outer loop counter, so
				// that we will get to the file that has been shunted up into the position
				// (index) we were just looking at:
				i1--;
				break;
			}
		}
	}

	// Return the concatenation of the subfolders' flat list with this folder's file list:
	return FileList.concat(FlatList);
}
