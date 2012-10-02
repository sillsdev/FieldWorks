#include <windows.h>

#include "StringFunctions.h"
#include "StatusDialog.h"
#include "Services.h"

const _TCHAR * pszSqlServerService = _T("MSSQL$SILFW");

/*----------------------------------------------------------------------------------------------
	Forms a full path by appending the file name to the folder, adding a backslash if necessary.
	Assumes pszFolder is dynamically allocated, and replaces it with the new path.
----------------------------------------------------------------------------------------------*/
void MakePath(_TCHAR * & pszFolder, const _TCHAR * pszFile)
{
	const _TCHAR * pszSlash = _T("\\");

	if (pszFolder[_tcslen(pszFolder) - 1] != '\\')
		new_sprintf_concat(pszFolder, 0, pszSlash);
	new_sprintf_concat(pszFolder, 0, pszFile);
}

/*----------------------------------------------------------------------------------------------
	Tests specified folder to see if it is compressed or encrypted. If so, an attempt is made to
	decompress and decrypt it.
	@param pszDataDir Folder where the test is to take place.
	@return true if we stopped SQL Server service.
----------------------------------------------------------------------------------------------*/
bool TestFolder(const _TCHAR * pszDataDir)
{
	bool fWeStoppedSqlService = false; // Set to true if we stop SQL Server.

	DWORD dwAttr = ::GetFileAttributes(pszDataDir);
	if (dwAttr & FILE_ATTRIBUTE_COMPRESSED)
	{
		AppendStatusText(_T("Folder '%s' is compressed.\r\n"), pszDataDir);

		// Make sure SQL Server is stopped:
		if (!ServiceManager.StopService(pszSqlServerService, true, 60000, fWeStoppedSqlService))
			LogError(_T("Could not stop SQL Server service: attempt to decompress folder may fail. Attempting anyway."));

		AppendStatusText(_T("Attempting to decompress folder..."), pszDataDir);

		// Get a handle to the folder:
		HANDLE hFolder = CreateFile(pszDataDir, FILE_ALL_ACCESS,
			FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL, OPEN_EXISTING,
			FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OVERLAPPED, NULL);

		// If we couldn't open it, report the error:
		if (hFolder == INVALID_HANDLE_VALUE)
		{
			LogError(_T("Error %d while obtaining handle to folder '%s'."),
				GetLastError(), pszDataDir);
		}
		else
		{
			// Attempt decompression:
			USHORT usNotCompressed = COMPRESSION_FORMAT_NONE;
			DWORD dwMeaninglessBytesReturned;

			if (0 == DeviceIoControl(hFolder, FSCTL_SET_COMPRESSION, &usNotCompressed,
				sizeof(usNotCompressed), NULL, 0, &dwMeaninglessBytesReturned, NULL))
			{
				LogError(_T("Error %d while decompressing %s."), GetLastError(),
					pszDataDir);
			}
			else
				AppendStatusText(_T(" Done.\r\n"));

			CloseHandle(hFolder);
			hFolder = NULL;
		} // End else file handle was valid
	}
	if (dwAttr & FILE_ATTRIBUTE_ENCRYPTED)
	{
		AppendStatusText(_T("Folder '%s' is encrypted.\r\n"), pszDataDir);

		// Make sure SQL Server is stopped:
		if (!ServiceManager.StopService(pszSqlServerService, true, 60000, fWeStoppedSqlService))
			LogError(_T("Could not stop SQL Server service: attempt to decrypt folder may fail. Attempting anyway."));

		AppendStatusText(_T("Attempting to decrypt folder..."), pszDataDir);

		if (DecryptFile(pszDataDir, 0) == 0)
		{
			// We failed, so report error:
			LogError(_T("Error %d while decrypting %s."), GetLastError(), pszDataDir);
			// Note that error 32 (ERROR_SHARING_VIOLATION - the process cannot access the file
			// because it is being used by another process) can be given simply because Windows
			// Explorer is open on the folder.
		}
		else
			AppendStatusText(_T(" Done.\r\n"));
	}

	// Try to prevent this folder or its files ever being encrypted again:
	EncryptionDisable(pszDataDir, TRUE);

	// Final sanity check:
	dwAttr = ::GetFileAttributes(pszDataDir);
	if ((dwAttr & FILE_ATTRIBUTE_COMPRESSED) || (dwAttr & FILE_ATTRIBUTE_ENCRYPTED))
		LogError(_T("Folder '%s' is still bad."), pszDataDir);
	else
		AppendStatusText(_T("Folder '%s' is OK.\r\n"), pszDataDir);

	return fWeStoppedSqlService;
}

/*----------------------------------------------------------------------------------------------
	Searches for all files matching the given mask, and tests to see if any are compressed or
	encrypted. If they are, an attempt is made to decompress and decrypt them.
	@param pszMask The file expression to match; can contain DOS ? and * wildcards.
	@param pszDataDir Folder where the search is to take place.
	@return true if we stopped SQL Server service.
----------------------------------------------------------------------------------------------*/
bool SearchForBadData(const _TCHAR * pszMask, const _TCHAR * pszDataDir)
{
	bool fWeStoppedSqlService = false; // Set to true if we stop SQL Server.

	WIN32_FIND_DATA wfd;
	HANDLE hFind;

	// Make a full path out of the file search pattern:
	_TCHAR * pszFullMask = my_strdup(pszDataDir);
	MakePath(pszFullMask, pszMask);

	// Begin searching for matching files:
	hFind = ::FindFirstFile(pszFullMask, &wfd);
	if (hFind != INVALID_HANDLE_VALUE)
	{
		do
		{
			// Get full path to bad file:
			_TCHAR * pszFilePath = my_strdup(pszDataDir);
			MakePath(pszFilePath, wfd.cFileName);

			if (wfd.dwFileAttributes & FILE_ATTRIBUTE_COMPRESSED)
			{
				AppendStatusText(_T("File '%s' is compressed.\r\n"), wfd.cFileName);

				// Make sure SQL Server is stopped:
				if (!ServiceManager.StopService(pszSqlServerService, true, 60000, fWeStoppedSqlService))
				{
					LogError(_T("Could not stop SQL Server service: attempt to decompress %s may fail. Attempting anyway."),
						wfd.cFileName);
				}

				AppendStatusText(_T("Attempting to decompress file..."), wfd.cFileName);

				// Get a handle to the file:
				HANDLE hFile = CreateFile(pszFilePath, FILE_ALL_ACCESS, 0, NULL, OPEN_EXISTING,
					FILE_ATTRIBUTE_NORMAL, NULL);

				// If we couldn't open it, report the error:
				if (hFile == INVALID_HANDLE_VALUE)
				{
					LogError(_T("Error %d while obtaining handle to file '%s'."),
						GetLastError(), pszFilePath);
				}
				else
				{
					// Attempt decompresion:
					USHORT usNotCompressed = COMPRESSION_FORMAT_NONE;
					DWORD dwMeaninglessBytesReturned;

					if (0 == DeviceIoControl(hFile, FSCTL_SET_COMPRESSION, &usNotCompressed,
						sizeof(usNotCompressed), NULL, 0, &dwMeaninglessBytesReturned, NULL))
					{
						LogError(_T("Error %d while decompressing %s."), GetLastError(),
							pszFilePath);
					}
					else
						AppendStatusText(_T(" Done.\r\n"));

					CloseHandle(hFile);
					hFile = NULL;
				} // End else file handle was valid
			} // End if file attributes showed compressed

			if (wfd.dwFileAttributes & FILE_ATTRIBUTE_ENCRYPTED)
			{
				AppendStatusText(_T("File '%s' is encrypted.\r\n"), wfd.cFileName);

				// Make sure SQL Server is stopped:
				if (!ServiceManager.StopService(pszSqlServerService, true, 60000, fWeStoppedSqlService))
				{
					LogError(_T("Could not stop SQL Server service: attempt to decrypt %s may fail. Attempting anyway."),
						wfd.cFileName);
				}

				AppendStatusText(_T("Attempting to decrypt file..."), wfd.cFileName);

				if (DecryptFile(pszFilePath, 0) == 0)
				{
					// We failed, so report error:
					LogError(_T("Error %d while decrypting %s."), GetLastError(), pszFilePath);
				}
				else
					AppendStatusText(_T(" Done.\r\n"));
			} // End if file attributes showed encrypted

			// One last sanity check:
			DWORD dwAttr = ::GetFileAttributes(pszFilePath);
			if ((dwAttr & FILE_ATTRIBUTE_COMPRESSED) || (dwAttr & FILE_ATTRIBUTE_ENCRYPTED))
				LogError(_T("File '%s' is still bad"), wfd.cFileName);
			else
				AppendStatusText(_T("File '%s' is OK.\r\n"), wfd.cFileName);

			delete[] pszFilePath;
			pszFilePath = NULL;

		} while (::FindNextFile(hFind, &wfd) && !IfStopRequested());
		::FindClose(hFind);
	}
	delete[] pszFullMask;
	pszFullMask = NULL;

	return fWeStoppedSqlService;
}

/*----------------------------------------------------------------------------------------------
	If any the .mdf or .ldf files in the FW Data folder are compressed or encrypted, attempts to
	undo the compressed and encrytped status. If this is not possible, returns a text list
	of the compressed/encrypted items.
	This test is necessary because SQL Server fails with compressed or encrypted data files.
----------------------------------------------------------------------------------------------*/
void DecompressAndDecrypt()
{
	bool fWeStoppedSqlService = false; // Set to true if we stop SQL Server.

	_TCHAR * pszDataDir = NULL;
	DWORD cb = 0;
	DWORD dwT;
	HKEY hk;
	const _TCHAR * pszRegKey = _T("RootDataDir");
	_TCHAR * pszMsg = NULL;

	// Retrieve the path to FW data folder:
	long lRet = ::RegCreateKeyEx(HKEY_LOCAL_MACHINE, _T("Software\\SIL\\FieldWorks"), 0,
		_T(""), REG_OPTION_NON_VOLATILE, KEY_QUERY_VALUE, NULL, &hk, NULL);
	if (lRet == ERROR_SUCCESS)
	{
		// Get size of needed buffer:
		if (::RegQueryValueEx(hk, pszRegKey, NULL, &dwT, (BYTE *)pszDataDir, &cb)
			== ERROR_SUCCESS)
		{
			// Create buffer to receive registry value:
			pszDataDir = new _TCHAR [cb];

			// Retrieve folder string:
			if (::RegQueryValueEx(hk, pszRegKey, NULL, &dwT, (BYTE *)pszDataDir, &cb)
				== ERROR_SUCCESS)
			{
				// Extend to form path to database files:
				MakePath(pszDataDir, _T("Data"));

				// Write status message:
				AppendStatusText(_T("Examining FieldWorks database folder '%s'\r\n"),
					pszDataDir);

				// Test database data folder:
				if (TestFolder(pszDataDir))
					fWeStoppedSqlService = true;

				// Test database files in the Data folder:
				if (SearchForBadData(_T("*.mdf"), pszDataDir))
					fWeStoppedSqlService = true;
				if (SearchForBadData(_T("*.ldf"), pszDataDir))
					fWeStoppedSqlService = true;

				if (fWeStoppedSqlService)
				{
					AppendStatusText(_T("Attempting to restart SQL Server service..."));

					// Attempt to restart SQL Server service:
					if (ServiceManager.StartServiceW(pszSqlServerService))
						AppendStatusText(_T(" Done.\r\n"));
					else
						LogError(_T("Could not restart SQL Server service."));
				}
			}
			else
			{
				LogError(_T("Could not retrieve FieldWorks database folder path from registry."),
					pszDataDir);
			}
			delete[] pszDataDir;
			pszDataDir = NULL;
		}
		else
			LogError(_T("Could not determine length of FieldWorks database folder path from registry."));
	}
	else
		LogError(_T("Could not open registry."));

	CopyErrorsToClipboard();
}
