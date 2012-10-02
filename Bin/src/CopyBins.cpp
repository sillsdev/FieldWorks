/***********************************************************************************************
	This program creates a folder composed of FW and the date formatted as FW_YYYY-MM-DD.
		Example: FW_2000-09-14
	It creates this new folder in the directory specified by root-dir. It then copies the
	entire directory structure from root-dir to the new directory.

	Usage: copybins from-dir root-dir
/**********************************************************************************************/
#include <windows.h>
#include <stdio.h>

int main(int argc, char ** argv)
{
	if (argc != 3)
	{
		// Print a usage message, and pass everything from stdin to stdout without
		// logging anything to a file.
		printf("\n**********************************************************************\n");
		printf("Usage for copybins.exe:\n");
		printf("  copybins.exe from-dir root-dir\n\n");
		printf("  from-dir    the directory that contains the files to copy\n");
		printf("  root-dir    the directory in which to to create the new directory in\n");
		printf("**********************************************************************\n\n\n");
		exit(1);
	}

	SYSTEMTIME st;
	GetLocalTime(&st);
	char szNewPath[MAX_PATH] = {0};
	strcpy(szNewPath, argv[2]);
	if (szNewPath[strlen(szNewPath) - 1] != '\\')
		strcat(szNewPath, "\\");
	sprintf(szNewPath + strlen(szNewPath), "FW_%4d-%02d-%02d", st.wYear, st.wMonth, st.wDay);

	char szCommand[MAX_PATH];
	char szArguments[MAX_PATH];
	OSVERSIONINFO osvi = {sizeof(osvi)};
	GetVersionEx(&osvi);
	if (osvi.dwPlatformId & VER_PLATFORM_WIN32_NT)
	{
		GetSystemDirectory(szCommand, sizeof(szCommand));
		strcat(szCommand, "\\xcopy");
	}
	else
	{
		GetWindowsDirectory(szCommand, sizeof(szCommand));
		strcat(szCommand, "\\command\\xcopy");
	}
	sprintf(szArguments, "%s %s /S /I", argv[1], szNewPath);

	ShellExecute(NULL, "open", szCommand, szArguments, argv[1], 0);
	return 0;
}