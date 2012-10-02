#include "stdafx.h"
#include "LogFile.h"
#include <io.h>
#include <sys/stat.h>

const char * BaseDirectory = "D:\\AutoSurveyor";
const char * SurveyorExe = "C:\\Program files\\Surveyor\\System\\GtorSur.exe";
LogFile Log;


int GetNumFiles(const char * FileSpec)
// Counts the number of files found to match the given spec, as per FindFirstFile()
{
	WIN32_FIND_DATA FileInfo;
	HANDLE hFile;
	int Tally = 0;

	hFile = FindFirstFile(FileSpec, &FileInfo);
	if (hFile != INVALID_HANDLE_VALUE)
	{
		do
		{
			Tally++;
		} while (FindNextFile(hFile, &FileInfo) != 0);
		FindClose(hFile);
	}
	return Tally;
}

void DelTree(const char * CurrentDirectory)
// Recursively deletes all files and subfolders in given folder.
{
	WIN32_FIND_DATA FileInfo;
	HANDLE hFile;
	char *mask = new char [strlen(CurrentDirectory) + 10]; // a few extras for safety
	strcpy(mask, CurrentDirectory);
	strcat(mask, "\\*.*");
	hFile = FindFirstFile(mask, &FileInfo);
	delete[] mask;
	mask = NULL;
	if (hFile != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (FileInfo.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
			{
				if (strcmp(FileInfo.cFileName, ".") != 0 && strcmp(FileInfo.cFileName, "..") != 0)
				{
					char *NextDirectory = new char [strlen(CurrentDirectory) + strlen(FileInfo.cFileName) + 10]; // a few extras for safety
					strcpy(NextDirectory, CurrentDirectory);
					strcat(NextDirectory, "\\");
					strcat(NextDirectory, FileInfo.cFileName);
					DelTree(NextDirectory);
					char cmd[500];
					sprintf(cmd, "rmdir %s", NextDirectory);
					system(cmd);
					delete[] NextDirectory;
				}
			}
			else
			{
				char * CondemnedFile = new char [strlen(CurrentDirectory) + strlen(FileInfo.cFileName) + 10]; // a few extras for safety
				strcpy(CondemnedFile, CurrentDirectory);
				strcat(CondemnedFile, "\\");
				strcat(CondemnedFile, FileInfo.cFileName);
				// Make sure we have write access:
				_chmod(CondemnedFile, _S_IWRITE);
				DeleteFile(CondemnedFile);
				delete[] CondemnedFile;
			}
		} while (FindNextFile(hFile, &FileInfo) != 0);
		FindClose(hFile);
	}
}

void MoveWebTree(const char * CurrentSourceDirectory, const char * CurrentDestDirectory)
// Recursively copies all files and subfolders in given source folder to dest folder.
{
	WIN32_FIND_DATA FileInfo;
	HANDLE hFile;
	char *mask = new char [strlen(CurrentSourceDirectory) + 10]; // a few extras for safety
	strcpy(mask, CurrentSourceDirectory);
	strcat(mask, "\\*.*");
	hFile = FindFirstFile(mask, &FileInfo);
	delete[] mask;
	mask = NULL;
	if (hFile != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (FileInfo.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
			{
				if (strcmp(FileInfo.cFileName, ".") != 0 && strcmp(FileInfo.cFileName, "..") != 0)
				{
					char *NextSourceDirectory = new char [strlen(CurrentSourceDirectory) + strlen(FileInfo.cFileName) + 10]; // a few extras for safety
					strcpy(NextSourceDirectory, CurrentSourceDirectory);
					strcat(NextSourceDirectory, "\\");
					strcat(NextSourceDirectory, FileInfo.cFileName);

					char *NextDestDirectory = new char [strlen(CurrentDestDirectory) + strlen(FileInfo.cFileName) + 10]; // a few extras for safety
					strcpy(NextDestDirectory, CurrentDestDirectory);
					strcat(NextDestDirectory, "\\");
					strcat(NextDestDirectory, FileInfo.cFileName);

					char cmd[500];
					sprintf(cmd, "mkdir %s", NextDestDirectory);
					system(cmd);

					MoveWebTree(NextSourceDirectory, NextDestDirectory);
					sprintf(cmd, "rmdir %s", NextSourceDirectory);
					system(cmd);

					delete[] NextSourceDirectory;
					delete[] NextDestDirectory;
				}
			}
			else
			{
				// Don't copy the .gcp file or the .log file, or any source file:
#define LastChars(s, n) &s[strlen(s)-n]
				if (strcmp(LastChars(FileInfo.cFileName, 4), ".gcp") != 0
					&& strcmp(LastChars(FileInfo.cFileName, 4), ".log") != 0
					&& strcmp(LastChars(FileInfo.cFileName, 4), ".cpp") != 0
					&& strcmp(LastChars(FileInfo.cFileName, 2), ".h") != 0
					&& strcmp(LastChars(FileInfo.cFileName, 4), ".idl") != 0
					&& strcmp(LastChars(FileInfo.cFileName, 4), ".idh") != 0
					&& strcmp(LastChars(FileInfo.cFileName, 3), ".rc") != 0
					&& strcmp(LastChars(FileInfo.cFileName, 4), ".def") != 0
					&& strcmp(LastChars(FileInfo.cFileName, 4), ".mak") != 0
					&& strcmp(LastChars(FileInfo.cFileName, 4), ".bmp") != 0)
				{
					char * SourceFile = new char [strlen(CurrentSourceDirectory) + strlen(FileInfo.cFileName) + 10]; // a few extras for safety
					strcpy(SourceFile, CurrentSourceDirectory);
					strcat(SourceFile, "\\");
					strcat(SourceFile, FileInfo.cFileName);

					char * DestFile = new char [strlen(CurrentDestDirectory) + strlen(FileInfo.cFileName) + 10]; // a few extras for safety
					strcpy(DestFile, CurrentDestDirectory);
					strcat(DestFile, "\\");
					strcat(DestFile, FileInfo.cFileName);

					CopyFile(SourceFile, DestFile, false);
					DeleteFile(SourceFile);
					delete[] SourceFile;
					delete[] DestFile;
				}
			}
		} while (FindNextFile(hFile, &FileInfo) != 0);
		FindClose(hFile);
	}
}

// This scheduler currently scans the AutoSurveyor folders repeatedly, invoking Surveyor if new data is found.
void RunScheduler()
{
	Log.TimeStamp();
	Log.Write(" Polling thread entered.\r\n");

	// Enter loop:
	while (!fStopRequested)
	{
		// Scan through all subdirectories, just to one level deep:
		char *DirectorySpec = new char [strlen(BaseDirectory) + 100]; // a few extras for safety
		strcpy(DirectorySpec, BaseDirectory);
		strcat(DirectorySpec, "\\*.");
		WIN32_FIND_DATA DirectoryInfo;
		HANDLE hDirectory;
		hDirectory = FindFirstFile(DirectorySpec, &DirectoryInfo);
		if (hDirectory != INVALID_HANDLE_VALUE)
		{
			do
			{
				if (DirectoryInfo.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY
					&& strcmp(DirectoryInfo.cFileName, ".") != 0
					&& strcmp(DirectoryInfo.cFileName, "..") != 0)
				{
					char *CurrentDirectory = new char [strlen(BaseDirectory) + strlen(DirectoryInfo.cFileName) + 100]; // a few extras for safety
					strcpy(CurrentDirectory, BaseDirectory);
					strcat(CurrentDirectory, "\\");
					strcat(CurrentDirectory, DirectoryInfo.cFileName);

					WIN32_FIND_DATA FileInfo;
					HANDLE hFile;
					char * FileSpecCpp = new char [strlen(CurrentDirectory) + 10]; // a few extras for safety
					strcpy(FileSpecCpp, CurrentDirectory);
					strcat(FileSpecCpp, "\\*.cpp");
					char * FileSpecH = new char [strlen(CurrentDirectory) + 10]; // a few extras for safety
					strcpy(FileSpecH, CurrentDirectory);
					strcat(FileSpecH, "\\*.h");
					char * FileSpecIdh_ = new char [strlen(CurrentDirectory) + 10]; // a few extras for safety
					strcpy(FileSpecIdh_, CurrentDirectory);
					strcat(FileSpecIdh_, "\\*.idh_");

					// We will have to count the numbers of .h, .cpp and .idh_ files, because if any more
					// have appeared after we've finished processing, we can assume that a user
					// was in the middle of copying files over when we started, and thus we don't
					// have a complete set, and will have to start again:
					int NumHFiles = GetNumFiles(FileSpecH);
					int NumCppFiles = GetNumFiles(FileSpecCpp);
					int NumIdh_Files = GetNumFiles(FileSpecIdh_);

					hFile = FindFirstFile(FileSpecCpp, &FileInfo);
					if (hFile == INVALID_HANDLE_VALUE)
						hFile = FindFirstFile(FileSpecH, &FileInfo);
					if (hFile == INVALID_HANDLE_VALUE)
						hFile = FindFirstFile(FileSpecIdh_, &FileInfo);
					if (hFile != INVALID_HANDLE_VALUE)
					{
						// We have found some .cpp, .h or idh_ files, so invoke Surveyor, in a loop,
						// to allow repeated invocations if the number of source files changes:
						int NewNumHFiles = NumHFiles;
						int NewNumCppFiles = NumCppFiles;
						int NewNumIdh_Files = NumIdh_Files;
						do
						{
							NumHFiles = NewNumHFiles;
							NumCppFiles = NewNumCppFiles;
							NumIdh_Files = NewNumIdh_Files;
							char *CommandLine = new char [strlen(CurrentDirectory) + 100]; // a few extras for safety
							strcpy(CommandLine, " [AutoWeb(\"");
							strcat(CommandLine, CurrentDirectory);
							strcat(CommandLine, "\")][Quit]");
							PROCESS_INFORMATION ProcessInformation; // This will be filled in for us
							STARTUPINFO StartupInformation;
							StartupInformation.cb = sizeof(STARTUPINFO);
							StartupInformation.lpReserved = NULL;
							StartupInformation.lpDesktop = "";
							StartupInformation.lpTitle = NULL;
							StartupInformation.cbReserved2 = 0;
							StartupInformation.lpReserved2 = NULL;;
							StartupInformation.dwFlags = 0;

							Log.TimeStamp();
							Log.Write(" Found files in ");
							Log.Write(CurrentDirectory);
							Log.Write(". Starting Surveyor.\r\n");

							if (0 == CreateProcess(SurveyorExe,
												   CommandLine,
												   NULL,
												   NULL,
												   false,
												   CREATE_DEFAULT_ERROR_MODE | NORMAL_PRIORITY_CLASS,
												   NULL,
												   CurrentDirectory,
												   &StartupInformation,
												   &ProcessInformation))
							{
								Log.Write("Could not launch Surveyor - process quitting.\r\n");
								return;
							}
							delete[] CommandLine;

							// Wait till Surveyor quits:
							WaitForSingleObject(ProcessInformation.hProcess, INFINITE);
							FindClose(hFile);

							Log.TimeStamp();
							Log.Write(" Surveyor finished.\r\n");

							// See how many source files we have now:
							NewNumHFiles = GetNumFiles(FileSpecH);
							NewNumCppFiles = GetNumFiles(FileSpecCpp);
							NewNumIdh_Files = GetNumFiles(FileSpecIdh_);
						// Repeat if number of source files has changed:
						} while (NumHFiles != NewNumHFiles || NumCppFiles != NewNumCppFiles || NumIdh_Files != NewNumIdh_Files);

						// Now delete those .cpp, .h and .idh_ files:
						hFile = FindFirstFile(FileSpecCpp, &FileInfo);
						if (hFile != INVALID_HANDLE_VALUE)
						{
							do
							{
								char * CondemnedFile = new char [strlen(CurrentDirectory) + strlen(FileInfo.cFileName) + 10]; // a few extras for safety
								strcpy(CondemnedFile, CurrentDirectory);
								strcat(CondemnedFile, "\\");
								strcat(CondemnedFile, FileInfo.cFileName);
								// Make sure we have write access:
								_chmod(CondemnedFile, _S_IWRITE);
								DeleteFile(CondemnedFile);
								delete[] CondemnedFile;
							} while (FindNextFile(hFile, &FileInfo) != 0);
							FindClose(hFile);
						}
						hFile = FindFirstFile(FileSpecH, &FileInfo);
						if (hFile != INVALID_HANDLE_VALUE)
						{
							do
							{
								char * CondemnedFile = new char [strlen(CurrentDirectory) + strlen(FileInfo.cFileName) + 10]; // a few extras for safety
								strcpy(CondemnedFile, CurrentDirectory);
								strcat(CondemnedFile, "\\");
								strcat(CondemnedFile, FileInfo.cFileName);
								// Make sure we have write access:
								_chmod(CondemnedFile, _S_IWRITE);
								DeleteFile(CondemnedFile);
								delete[] CondemnedFile;
							} while (FindNextFile(hFile, &FileInfo) != 0);
							FindClose(hFile);
						}
						hFile = FindFirstFile(FileSpecIdh_, &FileInfo);
						if (hFile != INVALID_HANDLE_VALUE)
						{
							do
							{
								char * CondemnedFile = new char [strlen(CurrentDirectory) + strlen(FileInfo.cFileName) + 10]; // a few extras for safety
								strcpy(CondemnedFile, CurrentDirectory);
								strcat(CondemnedFile, "\\");
								strcat(CondemnedFile, FileInfo.cFileName);
								// Make sure we have write access:
								_chmod(CondemnedFile, _S_IWRITE);
								DeleteFile(CondemnedFile);
								delete[] CondemnedFile;
							} while (FindNextFile(hFile, &FileInfo) != 0);
							FindClose(hFile);
						}
						// If the source folder happens to be "Graphite", then the Surveyor output is to be published
						// on the FieldWorks web site: \\172.21.1.118\ObjectWeb\Graphite
						if (strcmp(DirectoryInfo.cFileName, "Graphite") == 0)
						{
							const char * GraphiteWebURL = "\\\\172.21.1.118\\ObjectWeb\\Graphite";
							DelTree(GraphiteWebURL);
							MoveWebTree(CurrentDirectory, GraphiteWebURL);
						}
					}
					delete[] FileSpecH;
					delete[] FileSpecCpp;
					delete[] FileSpecIdh_;
					delete[] CurrentDirectory;
				} // End if found directory

			} while (FindNextFile(hDirectory, &DirectoryInfo) != 0);

			FindClose(hDirectory);

		}
		delete[] DirectorySpec;

		// Wait up to 10 seconds before trying again or quitting:
		::WaitForSingleObject(hEventStopper, 10000);

		if (fStopRequested)
		{
			::CloseHandle(hEventStopper);
			hEventStopper = NULL;
		}
	} // End while
}