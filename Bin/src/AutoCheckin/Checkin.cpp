/*
	Auto-Checkin - a utility to perform the entire Perforce check-in process
	unattended. The steps taken are:
		Waiting for the CheckIn_History.txt file to become available;
		Acquiring the CheckIn_History.txt file (no longer locking token file);
		Re-syncing all files to the local machine;
		Rebuilding FW with tests;
		Appending a comment to the CheckIn_History.txt file
		Submitting files to Perforce.

	The program accepts the user's check-in comment passed on the command
	line or, if not supplied, will pop up a dialog asking for it.

	Possible gotchas that you need to be aware of:
		If the Perforce sync results in inability to "clobber writable
			files", the auto-checkin will abort;
		If any files need resolving, the auto-checkin will abort;
		Subversion files are not synchronized;
		Any build or test failure will prevent a check-in.
*/

#include <windows.h>
#include <stdio.h>

#include "Dialogs.h"
#include "resource.h"
#include "Globals.h"

const char * pszWorkSpaceFileName = "C:\\__AutoCheckinWorkspace.txt";
const char * pszWorkSpace2FileName = "C:\\__AutoCheckinWorkspace2.txt";
const char * pszDepotFw = NULL;		// "//depot/fw/";
const char * pszCheckinTokenFile = "CheckIn_History.txt";
const char * pszChagelistSpecFileName = "C:\\__AutoCheckinChagelist.txt";

static bool fTimedOut = false;

void RevertTokenFile();

void FatalError(const char * Msg)
{
	static bool fRevertingOnError = false;

	printf("Fatal error: %s\r\n", Msg);

	if (!fRevertingOnError)
	{
		fRevertingOnError = true;
		RevertTokenFile();
		fRevertingOnError = false;
	}

	MessageBox(NULL, Msg, "Auto-checkin: fatal error", MB_ICONSTOP | MB_OK);
	exit(1);
}


/* This function has been replaced with a call to system(), because redirection
of standard output to a file was not possible with ExecCmd().
However, it has not been rigorously established whether the return values from
system() are the same as those from ExecCmd(). Basically, we are assuming zero
means success, and non-zero means an error of some sort.

// Executes the command in the given string and waits for the launched process to exit.
DWORD ExecCmd(LPCTSTR pszCmd)
{
	// Set up data for creating new process:
	BOOL bReturnVal = false;
	STARTUPINFO si;
	DWORD dwExitCode =  0;
	PROCESS_INFORMATION process_info;

	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);

	// Launch new process:
	bReturnVal = CreateProcess(NULL, (LPTSTR)pszCmd, NULL, NULL, false, 0, NULL,
							   NULL, &si, &process_info);

	if (bReturnVal)
	{
		CloseHandle(process_info.hThread);
		WaitForSingleObject(process_info.hProcess, INFINITE);
		GetExitCodeProcess(process_info.hProcess, &dwExitCode);
		CloseHandle(process_info.hProcess);
	}
	else
	{
		return (DWORD)-1;
	}
	return dwExitCode;
}
*/

// Advance character pointer beyond tabs and spaces:
const char * SkipWhiteSpace(const char * pch)
{
	if (!pch)
		return NULL;

	while (*pch == ' ' || *pch == '\t')
		pch++;

	return pch;
}

// Remove whitespace and newlines from end of string:
void ChopWhiteSpaceAndNewlines(char * psz)
{
	if (!psz)
		return;
	if (*psz == 0)
		return;

	psz += strlen(psz) - 1;

	while (*psz == ' ' || *psz == '\t' || *psz == '\n' || *psz == '\r')
		*(psz--) = 0;
}

bool FileIsEmpty(const char * pszFilePath)
{
	FILE * file = fopen(pszFilePath, "rt");
	if (!file)
		return true;

	char szLine[2];
	bool fEmpty = false;
	if (fgets(szLine, 2, file) == NULL)
		fEmpty = true;
	fclose(file);
	return fEmpty;
}

// Get Perforce to give us the Client name, User name and Root path:
void GetClientAndUser()
{
	gpszClient = NULL;
	gpszUser = NULL;

	char szCmd[100];
	sprintf(szCmd, "p4 client -o >%s", pszWorkSpaceFileName);
	int nResult = system(szCmd);

	if (nResult != 0)
		FatalError("Could not get client details from Perforce.");

	// We are looking for text after "Client:", text after "Owner:" and text after "Root:"
	FILE * file = fopen(pszWorkSpaceFileName, "rt");
	if (!file)
		FatalError("Could not open workspace file for client details.");

	char szLine[200];
	while (!gpszClient || !gpszUser || !pszDepotFw)
	{
		if (fgets(szLine, 200, file) == NULL)
			FatalError("Workspace file does not contain full client details.");

		if (strncmp(szLine, "Client:", 7) == 0)
		{
			// We have the client name:
			gpszClient = strdup(SkipWhiteSpace(&szLine[7]));
			ChopWhiteSpaceAndNewlines(gpszClient);
			continue;
		}
		if (strncmp(szLine, "Owner:", 6) == 0)
		{
			// We have the user name:
			gpszUser = strdup(SkipWhiteSpace(&szLine[6]));
			ChopWhiteSpaceAndNewlines(gpszUser);
			continue;
		}
		if (strncmp(szLine, "View:", 5) == 0)
		{
			while (!pszDepotFw)
			{
				if (fgets(szLine, 200, file) == NULL)
					FatalError("Workspace file does not contain full client details.");
				char * psz = strstr(szLine, gpszClient);
				if (psz != NULL)
				{
					psz = strstr(szLine, "//depot/");
					if (psz != NULL)
					{
						char * psz2 = strstr(psz, "/... //");
						if (psz != NULL)
						{
							*(psz2+1) = 0;
							pszDepotFw = strdup(psz);
						}
					}
				}
			}
		}
	}
	fclose(file);
}

// Make a list of changelists that the user has pending.
// The default list is always implicit.
void GetAvailableChangeLists()
{
	gpnChangeLists = NULL;
	gppszChangeListsComments = NULL;
	gcclChangeLists = 0;

	char szCmd[200];
	sprintf(szCmd, "p4 changes -s pending -u %s >%s", gpszUser, pszWorkSpaceFileName);
	int nResult = system(szCmd);
	if (nResult != 0)
		FatalError("Could not see which changelists are pending.");

	// Collect changelist numbers:
	FILE * file = fopen(pszWorkSpaceFileName, "rt");
	if (!file)
		return;

	char szLine[400];
	while (fgets(szLine, 400, file) != NULL)
	{
		// Find start of changelist number:
		char * pszStartNumber = szLine;
		while (!isdigit(*pszStartNumber))
		{
			pszStartNumber++;
			if (*pszStartNumber == 0)
				break;
		}
		// Find end of changelist number:
		char * pszEndNumber = strstr(pszStartNumber, " on");
		if (pszEndNumber)
		{
			*pszEndNumber = 0;
		}
		if (pszStartNumber && pszEndNumber)
		{
			// Add changelist number to list:
			int * nTemp = new int [1 + gcclChangeLists];
			char ** pszTemp = new char * [1 + gcclChangeLists];
			for (int i = 0; i < gcclChangeLists; i++)
			{
				nTemp[i] = gpnChangeLists[i];
				pszTemp[i] = gppszChangeListsComments[i];
			}
			delete[] gpnChangeLists;
			gpnChangeLists = nTemp;
			gpnChangeLists[gcclChangeLists++] = atoi(pszStartNumber);
			delete[] gppszChangeListsComments;
			gppszChangeListsComments = pszTemp;

			// Collect comment for this changelist:
			pszEndNumber++;
			char *pszCommentStart = strchr(pszEndNumber, '\'');
			if (pszCommentStart)
			{
				pszCommentStart++;
				char * pszCommentEnd = strrchr(pszCommentStart, '\'');
				if (pszCommentEnd)
				{
					*pszCommentEnd = 0;
					ChopWhiteSpaceAndNewlines(pszCommentStart);
					gppszChangeListsComments[gcclChangeLists - 1] = strdup(pszCommentStart);
				}
			}
		}
	}
	fclose(file);
}

// See if anyone has the token file.
bool TokenFileIsFree()
{
	char szCmd[200];
	sprintf(szCmd, "p4 opened -a %s%s >%s", pszDepotFw, pszCheckinTokenFile,
		pszWorkSpaceFileName);
	int nResult = system(szCmd);
	if (nResult != 0)
		FatalError("Could not see if token file is checked out.");

	// Looking for empty file:
	return FileIsEmpty(pszWorkSpaceFileName);
}


// Get up to date token file:
void SyncTokenFile()
{
	char szCmd[200];
	sprintf(szCmd, "p4 sync %s%s", pszDepotFw, pszCheckinTokenFile);
	int nResult = system(szCmd);
	if (nResult != 0)
		FatalError("Could not sync token file from Perforce.");
}

// Open token file for edit.
void OpenTokenFileForEdit()
{
	char szCmd[200];
	if (!gnCheckinChangeList)
		sprintf(szCmd, "p4 edit %s%s", pszDepotFw, pszCheckinTokenFile); // Default changelist
	else
		sprintf(szCmd, "p4 edit -c %d %s%s", gnCheckinChangeList, pszDepotFw, pszCheckinTokenFile);
	int nResult = system(szCmd);
	if (nResult != 0)
		FatalError("Could not open token file for edit from Perforce.");
}

// Lock token file:
/* No longer used.
void LockTokenFile()
{
	char szCmd[200];
	sprintf(szCmd, "p4 lock %s%s", pszDepotFw, pszCheckinTokenFile);
	int nResult = system(szCmd);
	if (nResult != 0)
		FatalError("Could not lock token file on Perforce.");
}*/

// Relinquish the token file.
void RevertTokenFile()
{
	char szCmd[200];
	sprintf(szCmd, "p4 revert %s%s", pszDepotFw, pszCheckinTokenFile);
	int nResult = system(szCmd);
	if (nResult != 0)
		FatalError("Could not revert token file on Perforce.");
}

// See if we have the token file exclusively:
bool TokenFileExclusivelyMine()
{
	char szCmd[200];
	sprintf(szCmd, "p4 opened -a %s%s >%s", pszDepotFw, pszCheckinTokenFile,
		pszWorkSpaceFileName);
	int nResult = system(szCmd);
	if (nResult != 0)
		FatalError("Could not see if token file is checked out.");

	// Looking for our client name in first line, and only one line:
	FILE * file = fopen(pszWorkSpaceFileName, "rt");
	if (!file)
		return false;

	char szLine[400];
	if (fgets(szLine, 400, file) == NULL)
	{
		// Nobody has the file! Could be an error...
		fclose(file);
		return false;
	}

	char * pszClientInLine = strstr(szLine, gpszClient);
	if (pszClientInLine == NULL)
	{
		// Somebody else has the file!
		fclose(file);
		return false;
	}
/*	// Make sure our client has the file locked:
	char * pszRestOfLine = pszClientInLine + strlen(gpszClient);
	if (strncmp(pszRestOfLine, " *locked*", 9) != 0)
	{
		// It wasn't really us!
		fclose(file);
		return false;
	}
*/
	// Make sure no other lines exist in report:
	if (fgets(szLine, 400, file) != NULL)
	{
		// Somebody else has the file!
		fclose(file);
		return false;
	}

	fclose(file);
	return true;
}

// Sync all files.
void SyncAllFiles()
{
	char szCmd[200];

	// Perforce:
	sprintf(szCmd, "p4 sync %s...", pszDepotFw);
	int nResult = system(szCmd);
	if (nResult != 0)
		FatalError("Could not completely sync to files on Perforce.");

	// Subversion:
	//TODO...
}

// Return true if any files in the selected changelist need resolving.
bool FilesNeedResolving()
{
	// Firstly, use the p4 opened command to get a list of files in the selected changelist:
	char szCmd[200];
	if (!gnCheckinChangeList)
		sprintf(szCmd, "p4 opened -c default >%s", pszWorkSpaceFileName); // Default changelist
	else
		sprintf(szCmd, "p4 opened -c %d >%s", gnCheckinChangeList, pszWorkSpaceFileName);
	int nResult = system(szCmd);
	if (nResult != 0)
		FatalError("Could not check if files need resolving.");

	// Now examine listed files:
	FILE * file = fopen(pszWorkSpaceFileName, "rt");
	if (!file)
		FatalError("Could not open workspace file for files in selected changelist.");

	char szLine[200];
	bool fFilesNeedResolving = false;
	while (fgets(szLine, 200, file) != NULL)
	{
		// Get current file:
		char * pszFile = strdup(szLine);
		// Remove revision and status description:
		char * pszStatus = strrchr(pszFile, '#');
		if (pszStatus)
			*pszStatus = 0;
		if (strlen(pszFile) == 0)
			break; // End of list reached.

		// See if current file needs resolving. If user opted for auto-resolve, try that:
		if (gfAutoResolve)
			sprintf(szCmd, "p4 resolve -am \"%s\" >%s", pszFile, pszWorkSpace2FileName);
		else
			sprintf(szCmd, "p4 resolve -n \"%s\" >%s", pszFile, pszWorkSpace2FileName);
		int nResult = system(szCmd);
		if (nResult != 0)
			FatalError("Could not see if file needed resolving.");

		// Look for empty file:
		if (!FileIsEmpty(pszWorkSpace2FileName))
		{
			// File is not empty, but if auto-resolve was attempted, we can see if it worked:
			if (gfAutoResolve)
			{
				// If the resolve failed, the phrase "resolve skipped" will appear in the report:
				FILE * file2 = fopen(pszWorkSpace2FileName, "rt");
				if (file2)
				{
					char szLine2[400];
					while (fgets(szLine2, 400, file2) != NULL)
					{
						if (strstr(szLine2, "resolve skipped"))
						{
							printf("File '%s' has conflicts.\n", pszFile);
							fFilesNeedResolving = true;
							break;
						}
					}
					fclose(file2);
					remove(pszWorkSpace2FileName);
				}
			}
			else
			{
				printf("File '%s' needs resolving.\n", pszFile);
				fFilesNeedResolving = true;
			}
		}
	}
	fclose(file);

	return fFilesNeedResolving;
}

// Rebuild all.
void RebuildAll()
{
	char szCmd[300];
	sprintf(szCmd, "%s\\bin\\nant\\bin\\nant.exe -buildfile:%s\\bld\\FieldWorks.build remakefw "
		"remakefw-failOnError", gpszRoot, gpszRoot);
	int nResult = system(szCmd);
	if (nResult != 0)
		FatalError("Build failed.");

	// just run tests:
	// c:\FW\bin\nant\bin\nant.exe -buildfile:c:\FW\bld\FieldWorks.build test all remakefw-failOnError
}


// Prepare changelist specification.
void MakeChangelistSpec()
{
	// Get current changelist specification:
	char szCmd[200];
	if (!gnCheckinChangeList)
		sprintf(szCmd, "p4 change -o >%s", pszWorkSpaceFileName); // Default changelist
	else
		sprintf(szCmd, "p4 change -o %d >%s", gnCheckinChangeList, pszWorkSpaceFileName);
	int nResult = system(szCmd);
	if (nResult != 0)
		FatalError("Cannot get Changelist specification from Perforce.");

	// Make copy of file. If any line contains "<enter description here>",
	// replace it with user's comment:
	FILE * input = fopen(pszWorkSpaceFileName, "rt");
	if (!input)
		FatalError("Cannot open original Changelist specification file.");
	FILE * output = fopen(pszChagelistSpecFileName, "wt");
	if (!output)
	{
		fclose(input);
		FatalError("Cannot open new Changelist specification file.");
	}
	char szLine[400];
	bool fDescriptionActive = false;
	while (fgets(szLine, 400, input) != NULL)
	{
		if (fDescriptionActive)
		{
			// Replace current line with comment:
			fprintf(output, "\t%s\r\n", gpszCheckinComment);
			fDescriptionActive = false;
		}
		else
		{
			fprintf(output, szLine);

			if (strncmp(szLine, "Description:", 12) == 0)
				fDescriptionActive = true;
		}
	}
	fclose(input);
	fclose(output);
}

// Edit token file to add user's comment of changes.
void EditTokenFile()
{
	char szTokenFileName[300];
	sprintf(szTokenFileName, "%s\\%s", gpszRoot, pszCheckinTokenFile);

	// Check if last comment has a newline at the end:
	FILE * fileToken = fopen(szTokenFileName, "rt");
	if (!fileToken)
		FatalError("Cannot open token file.");
	char szLine[400];
	while (fgets(szLine, 400, fileToken) != NULL)
		;
	fclose(fileToken);
	fileToken = NULL;

	bool fNewLineFound = false;
	int nLen = strlen(szLine);
	if (nLen == 0)
		fNewLineFound = true;
	else
	{
		int iEnd = nLen - 1;
		if (szLine[iEnd] == '\n' || szLine[iEnd] == '\r')
			fNewLineFound = true;
	}

	// Append user's check-in details:
	fileToken = fopen(szTokenFileName, "at");
	if (!fileToken)
		FatalError("Cannot append to token file.");

	if (!fNewLineFound)
		fprintf(fileToken, "\r\n");

	fprintf(fileToken, "%s (auto), %s. %s\r\n", gpszCheckinUser, gpszCheckinDate,
		gpszCheckinComment);

	fclose(fileToken);
}

// Submit changes:
void P4Submit()
{
	char szCmd[200];
	if (!gnCheckinChangeList)
		sprintf(szCmd, "p4 submit -i <%s", pszChagelistSpecFileName); // Default changelist
	else
		sprintf(szCmd, "p4 submit -c %d -i <%s", gnCheckinChangeList, pszChagelistSpecFileName);
	int nResult = system(szCmd);
	if (nResult != 0)
		FatalError("Cannot submit changes to Perforce.");
}

DWORD WINAPI TimeOutCheck(LPVOID)
{
	// Sleep for 2 hours before signalling time-out:
	Sleep(2 * 60 * 60 * 1000);
	fTimedOut = true;
	FatalError("Timed out. Two hours have passed, and I don't know why I haven't finished.");
	return 0;
}

int main(int argc, char * argv[])
{
	printf("Please wait while initial details are downloaded from Perforce server...\n");

	gpszClient = NULL;
	gpszUser = NULL;
	gpszRoot = NULL;
	gpszCheckinUser = NULL;
	gpszCheckinDate = NULL;
	gpszCheckinComment = NULL;
	gnCheckinChangeList = 0;
	pszDepotFw = NULL;

	// Get client name and user name (and root path):
	GetClientAndUser();
	printf("client = '%s'; user = '%s'...\n", gpszClient, gpszUser);

	// Get available changelist numbers:
	GetAvailableChangeLists();

	// Get root path:
	const int kcchBuf = 100;
	char szBuf[kcchBuf] = "";
	GetEnvironmentVariable("fwroot", szBuf, kcchBuf);
	gpszRoot = strdup(szBuf);

	// Get default value for date:
	SYSTEMTIME SystemTime;
	GetSystemTime(&SystemTime);

	sprintf(szBuf, "%d-%02d-%02d", SystemTime.wYear, SystemTime.wMonth, SystemTime.wDay);
	gpszCheckinDate = strdup(szBuf);

	printf("...Done.\n");

	// There are two ways to run the program:
	// 1) from the command line, with free text (as the checkin comment) forming the rest of the
	//    command line;
	// 2) with no command line arguments, in which case all details are collected in a dialog
	//    box.
	// In the case of (1), all other variables take default values: Default changelist, Perforce
	// user name, date format etc.
	// Check if user entered text:
	if (argc < 2)
	{
		if (DialogBox(GetModuleHandle(NULL), MAKEINTRESOURCE(IDD_DIALOG_USER_INFO), NULL,
			DlgProcUserInfo) == 0)
		{
			exit(0);
		}
	}
	else
	{
		// Get user's checkin comment from the command line:
		const char * pszCommandLine = GetCommandLine();
		// The position of the what the system thinks is the first of the command line arguments
		// is the start of the comment:
		gpszCheckinComment = strdup(strstr(pszCommandLine, argv[1]));

		// Use Perforce user name as default checkin name:
		gpszCheckinUser = strdup(gpszUser);
	}

	printf("Attempting to acquire check-in token file...\n");

	// Loop until we get exclusive access to token file:
	while (true)
	{
		// See if anyone has the token file:
		if (TokenFileIsFree())
		{
			// Get up to date token file:
			SyncTokenFile();

			// Open token file for edit:
			OpenTokenFileForEdit();

			// Lock token file:
//			LockTokenFile();

			// See if anyone else has the token file as well as us:
			if (TokenFileExclusivelyMine())
			{
				// We have the token file exclusively, so we can proceed:
				break;
			}
			else
			{
				// Someone else got it at the same time as us! We'll revert:
				RevertTokenFile();
			}
		}
		// Sleep for 10 seconds before trying again:
		Sleep(10000);
	}

	printf("...Done.\n");

	// Now that we have the token file, create a new thread to monitor if we take too long
	// to complete the task:
	DWORD nThreadId; // MSDN says you can pass NULL instead of this, but you can't on Win98.
	HANDLE hThread = CreateThread(NULL, 0, TimeOutCheck, NULL, 0, &nThreadId);

	printf("Synchronizing files with Perforce server...\n");

	// Sync all files:
	SyncAllFiles();

	if (fTimedOut)
		Sleep(INFINITE);

	printf("...Done.\n");
	printf("Checking if any files need resolving...\n");

	// See if any files need resolving:
	if (FilesNeedResolving())
		FatalError("One or more files need resolving. No point in attempting build, as check-in will fail anyway.");

	if (fTimedOut)
		Sleep(INFINITE);

	printf("...Done.\n");
	printf("\n");
	printf("******************************************************************************\n");
	printf("Initialization complete. About to start build. You can leave your machine now!\n");
	printf("******************************************************************************\n");
	printf("\n");
	Sleep(4000);

	// Rebuild all - does not return if build fails:
	RebuildAll();

	if (fTimedOut)
		Sleep(INFINITE);

	// Prepare Chagelist spec file:
	MakeChangelistSpec();

	if (fTimedOut)
		Sleep(INFINITE);

	// Edit CheckIn_History.txt file to add user's description of changes.
	EditTokenFile();

	if (fTimedOut)
		Sleep(INFINITE);

	// Submit changes:
	P4Submit();

	printf("Auto-checkin completed!\r\n");

	TerminateThread(hThread, 0);

	delete[] pszDepotFw;
	delete[] gpszClient;
	delete[] gpszUser;
	delete[] gpszRoot;
	delete[] gpszCheckinUser;
	delete[] gpszCheckinDate;
	delete[] gpszCheckinComment;
	for (int i = 0; i < gcclChangeLists; i++)
		delete[] gppszChangeListsComments[i];
	delete[] gppszChangeListsComments;
	delete[] gpnChangeLists;

	remove(pszWorkSpaceFileName);
	remove(pszChagelistSpecFileName);

	return 0;
}
