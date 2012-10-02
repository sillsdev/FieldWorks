#include <windows.h>
#include <stdio.h>
#include <msi.h>
#include <msiquery.h>

// Adds a line to the log file of the installer.
void WriteMsiLogEntry(MSIHANDLE hInstall, const char * pszMessage)
{
#ifdef TEST_HARNESS
	if (!hInstall)
	{
		MessageBox(NULL, pszMessage, "Test harness message", 0);
		return;
	}
#endif

	PMSIHANDLE hRecord = MsiCreateRecord(1);
	// field 0 is the template
	MsiRecordSetString(hRecord, 0, "Delete ICU files: [1]");
	// field 1, to be placed in [1] placeholder
	MsiRecordSetString(hRecord, 1, pszMessage);
	// send message to running installer
	MsiProcessMessage(hInstall, INSTALLMESSAGE_INFO, hRecord);
}

// Recursively deletes all files and subfolders in the specified folder,
// which must have a trailing backslash.
// If fDeleteFolderToo is true, the specified folder is deleted too.
void DeleteAllFilesInFolder(char * pszFolderpath, bool fDeleteFolderToo, MSIHANDLE hInstall)
{
	char Msg[500];
	WIN32_FIND_DATA wfd;
	HANDLE hFile;
	DWORD dwFileAttr;
	char * pszFile;
	size_t cchSearchSpec = 1 + strlen(pszFolderpath) + 3;
	char * pszSearchSpec = new char [cchSearchSpec];
	sprintf_s(pszSearchSpec, cchSearchSpec, "%s*.*", pszFolderpath);
	char szPathFile[MAX_PATH];

	sprintf_s(Msg, 500, "folder path is '%s'.", pszFolderpath);
	WriteMsiLogEntry(hInstall, Msg);

	// Find the first file
	hFile = FindFirstFile(pszSearchSpec, &wfd);

	if (hFile != INVALID_HANDLE_VALUE)
	{
		do
		{
			pszFile = wfd.cFileName;
			sprintf_s(szPathFile, MAX_PATH, "%s%s", pszFolderpath, pszFile);

			// Get the file attributes:
			dwFileAttr = GetFileAttributes(szPathFile);

			// See if file is read-only : if so unset read-only:
			if (dwFileAttr & FILE_ATTRIBUTE_READONLY)
			{
				dwFileAttr &= ~FILE_ATTRIBUTE_READONLY;
				SetFileAttributes(szPathFile, dwFileAttr);
			}

			// See if the file is a directory:
			if (wfd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
			{
				// Make sure it isn't current or parent directory:
				if (strcmp(pszFile, ".") != 0 && strcmp(pszFile, "..") != 0)
				{
					// Recursively delete all files in this folder:
					strcat_s(szPathFile, MAX_PATH, "\\");
					DeleteAllFilesInFolder(szPathFile, true, hInstall);
				}
			}
			else // Not a directory
			{
				// Delete the file:
				if (0 == DeleteFile(szPathFile))
				{
					sprintf_s(Msg, 500, "error %d: could not delete file %s.", GetLastError(), szPathFile);
					WriteMsiLogEntry(hInstall, Msg);
				}
				else
				{
					sprintf_s(Msg, 500, "deleted file %s.", szPathFile);
					WriteMsiLogEntry(hInstall, Msg);
				}
			}
		} while (FindNextFile(hFile, &wfd));
	}
	// Close handle to file search:
	FindClose(hFile);

	// Delete directory too if needed:
	if (fDeleteFolderToo)
	{
		if (0 == RemoveDirectory(pszFolderpath))
		{
			sprintf_s(Msg, 500, "error %d: could not delete folder %s.", GetLastError(), pszFolderpath);
			WriteMsiLogEntry(hInstall, Msg);
		}
		else
		{
			sprintf_s(Msg, 500, "deleted folder %s.", pszFolderpath);
			WriteMsiLogEntry(hInstall, Msg);
		}
	}
	delete[] pszSearchSpec;
}

void DeleteMsiSpecFolderFiles(const char * MsiFolderName, bool fDeleteFolderToo, MSIHANDLE hInstall)
{
	char Msg[500];
	char * pszFolderPath = NULL;
	DWORD cchFolderPath = 0;

	sprintf_s(Msg, 500, "Attempting to delete files in MSI folder %s.", MsiFolderName);
	WriteMsiLogEntry(hInstall, Msg);

	// Determine the required buffer size for the full file path:
	UINT uiStat = MsiGetTargetPath(hInstall, MsiFolderName, "", &cchFolderPath);
	if (ERROR_MORE_DATA == uiStat)
	{
		++cchFolderPath; // on output does not include terminating null, so add 1
		pszFolderPath = new char[cchFolderPath];
		if (pszFolderPath)
			uiStat = MsiGetTargetPath(hInstall, MsiFolderName, pszFolderPath, &cchFolderPath);
	}

	if (pszFolderPath)
		DeleteAllFilesInFolder(pszFolderPath, true, hInstall);
	else
		WriteMsiLogEntry(hInstall, "error: could not find full path.");

	delete[] pszFolderPath;
}

// Deletes any pre-existing ICU files from the Icu40 and icudt40l folders.
// Returns 0 in all cases, but errors are logged to the MSI error output.
extern "C" __declspec(dllexport) UINT CleanupICU(MSIHANDLE hInstall)
{
	DeleteMsiSpecFolderFiles("data.F001DE50_84CE_44C8_A065_297102C05A95", true, hInstall);
	DeleteMsiSpecFolderFiles("icudt40l.F001DE50_84CE_44C8_A065_297102C05A95", true, hInstall);
	DeleteMsiSpecFolderFiles("Icu40.F001DE50_84CE_44C8_A065_297102C05A95", false, hInstall);

	return 0;
}
