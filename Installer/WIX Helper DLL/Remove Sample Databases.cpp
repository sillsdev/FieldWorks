#define _WIN32_WINNT 0x0501

#include <windows.h>
#include <stdio.h>
#include <msi.h>
#include <msiquery.h>
#include <oledb.h>
#include <comdef.h>

#include "..\..\Output\Common\Raw\DbAccessTlb.h"
#include "..\..\Output\Common\Raw\DbAccessTlb_i.c"


// Define names of FW databases we will be detaching, in wide character format, as we
// will be passing them to a Unicode-only function:
wchar_t * pszDbNames[] =
{
	L"Sena 2",
	L"Sena 3",
	L"Lela-Teli 2",
	L"Lela-Teli 3",
	L"Ethnologue",
};

// Define the database file names, no need for wide characters:
char * pszFileNames[] =
{
	"Sena 2.mdf",
	"Sena 2_Log.ldf",
	"Sena 3.mdf",
	"Sena 3_Log.ldf",
	"Lela-Teli 2.mdf",
	"Lela-Teli 2_Log.ldf",
	"Lela-Teli 3.mdf",
	"Lela-Teli 3_Log.ldf",
	"Ethnologue.mdf",
	"Ethnologue_Log.ldf",
};


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
	MsiRecordSetString(hRecord, 0, "RemoveSampleDatabases: [1]");
	// field 1, to be placed in [1] placeholder
	MsiRecordSetString(hRecord, 1, pszMessage);
	// send message to running installer
	MsiProcessMessage(hInstall, INSTALLMESSAGE_INFO, hRecord);
}

// Adds a line of wide text to the log file of the installer.
void WriteMsiLogEntry(MSIHANDLE hInstall, const wchar_t * pszMessage)
{
#ifdef TEST_HARNESS
	if (!hInstall)
	{
		MessageBoxW(NULL, pszMessage, L"Test harness message", 0);
		return;
	}
#endif

	PMSIHANDLE hRecord = MsiCreateRecord(1);
	// field 0 is the template
	MsiRecordSetStringW(hRecord, 0, L"RemoveSampleDatabases: [1]");
	// field 1, to be placed in [1] placeholder
	MsiRecordSetStringW(hRecord, 1, pszMessage);
	// send message to running installer
	MsiProcessMessage(hInstall, INSTALLMESSAGE_INFO, hRecord);
}

// Determines the path for the sample FW databases, detaches them from SQL server,
// then deletes the files.
// Returns 0 in all cases, but errors are logged to the MSI error output.
extern "C" __declspec(dllexport) UINT RemoveSampleDatabases(MSIHANDLE hInstall)
{
	// Initialize COM client:
	CoInitialize(NULL);
	IOleDbEncap * pOleDbEncap = NULL;

	// Connect to COM Server for FW Databases:
	HRESULT hr = CoCreateInstance(CLSID_OleDbEncap, NULL, CLSCTX_INPROC_SERVER,
						IID_IOleDbEncap, reinterpret_cast <void **> (&pOleDbEncap));
	if (FAILED(hr))
	{
		WriteMsiLogEntry(hInstall, "error: CoCreateInstance failed.");
		CoUninitialize();
		return 0;
	}

	// Initialize connection to master database on local machine:
	wchar_t szComputerName[MAX_COMPUTERNAME_LENGTH + 1];
	DWORD cch = sizeof(szComputerName);
	if (0 == GetComputerNameW(szComputerName, &cch))
	{
		WriteMsiLogEntry(hInstall, "error: GetComputerName failed.");
		pOleDbEncap->Release();
		CoUninitialize();
		return 0;
	}

	wchar_t szServerName[MAX_COMPUTERNAME_LENGTH + 7];
	swprintf_s(szServerName, MAX_COMPUTERNAME_LENGTH + 7, L"%s\\SILFW", szComputerName);

	hr = pOleDbEncap->Init(_bstr_t(szServerName), _bstr_t(L"master"), NULL, koltMsgBox, koltvFwDefault);
	if (FAILED(hr))
	{
		WriteMsiLogEntry(hInstall, "error: Initialization of connection to master database failed.");
		pOleDbEncap->Release();
		CoUninitialize();
		return 0;
	}

	// Create a command interface from the FW database connection:
	IOleDbCommand * podc = NULL;
	hr = pOleDbEncap->CreateCommand(&podc);
	if (FAILED(hr))
	{
		WriteMsiLogEntry(hInstall, "error: CreateCommand failed.");
		pOleDbEncap->Release();
		CoUninitialize();
		return 0;
	}

	// Detach each database:
	for (int iDb = 0; iDb < sizeof(pszDbNames) / sizeof(pszDbNames[0]); iDb++)
	{
		// Set the DB name as the parameter:
		hr = podc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)pszDbNames[iDb], ULONG(wcslen(pszDbNames[iDb]) * sizeof(wchar_t)));

		try
		{
			// Run the "detach" stored procedure:
			hr = podc->ExecCommand(L"EXEC sp_detach_db ?", knSqlStmtNoResults);
			wchar_t szError[200];
			swprintf_s(szError, 200, L"detached %s.", pszDbNames[iDb]);
			WriteMsiLogEntry(hInstall, szError);
		}
		catch (...)
		{
			wchar_t szError[200];
			swprintf_s(szError, 200, L"error: failed to detach %s.", pszDbNames[iDb]);
			WriteMsiLogEntry(hInstall, szError);
		}
	}

	// Get the full path of the folder where the database files are:
	char * pszFolderPath = NULL;
	DWORD cchFolderPath = 0;

	if (!hInstall)
	{
#ifdef TEST_HARNESS
		pszFolderPath = _strdup(DATA_FOLDER);
#else
		podc->Release();
		pOleDbEncap->Release();
		CoUninitialize();
		return 0;
#endif
	}
	else
	{
		// Determine the required buffer size for the full file path:
		UINT uiStat = MsiGetTargetPath(hInstall, "Data", "", &cchFolderPath);
		if (ERROR_MORE_DATA == uiStat)
		{
			++cchFolderPath; // on output does not include terminating null, so add 1
			pszFolderPath = new char[cchFolderPath];
			if (pszFolderPath)
				uiStat = MsiGetTargetPath(hInstall, "Data", pszFolderPath, &cchFolderPath);
		}
	}

	char szMsg[500];
	if (pszFolderPath)
	{
		sprintf_s(szMsg, 500, "database folder path is %s.", pszFolderPath);
		WriteMsiLogEntry(hInstall, szMsg);
	}
	else
	{
		WriteMsiLogEntry(hInstall, "error: could not determine database folder path.");
		podc->Release();
		pOleDbEncap->Release();
		CoUninitialize();
		return 0;
	}

	// Delete the detached database files:
	for (int iFile = 0; iFile < sizeof(pszFileNames) / sizeof(pszFileNames[0]); iFile++)
	{
		size_t cchFilePath = strlen(pszFolderPath) + strlen(pszFileNames[iFile]) + 1;
		char * pszFilePath = new char [cchFilePath];

		if (pszFilePath)
		{
			sprintf_s(pszFilePath, cchFilePath, "%s%s", pszFolderPath, pszFileNames[iFile]);

			sprintf_s(szMsg, 500, "Deleting file %s.", pszFilePath);
			WriteMsiLogEntry(hInstall, szMsg);

			// Remove the read-only attribute:
			DWORD dwAttrs = GetFileAttributes(pszFilePath);
			if (dwAttrs != INVALID_FILE_ATTRIBUTES)
			{
				if (dwAttrs & FILE_ATTRIBUTE_READONLY) // Only attempt removal if attribute is set
				{
					if (0 == SetFileAttributes(pszFilePath, dwAttrs & ~FILE_ATTRIBUTE_READONLY))
					{
						sprintf_s(szMsg, 500, "Error %d: could not remove read-only attributes.", GetLastError());
						WriteMsiLogEntry(hInstall, szMsg);
					}
				}
			}
			else
				WriteMsiLogEntry(hInstall, "Error: could not retrieve file attributes.");

			// Delete the file:
			if (0 == DeleteFile(pszFilePath))
			{
				sprintf_s(szMsg, 500, "Error %d: could not delete file.", GetLastError());
				WriteMsiLogEntry(hInstall, szMsg);
			}
		}
	}

	podc->Release();
	// For some reason, the process would hang here if we tried to release pOleDbEncap.
//	pOleDbEncap->Release();

	return 0;
}
