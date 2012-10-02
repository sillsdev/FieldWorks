#include "stdafx.h"
#include <stdio.h>
#include <Windows.h>
#include "LogFile.h"

extern const char * BaseDirectory;

//:>********************************************************************************************
//:>	LogFile methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
LogFile::LogFile()
{
	m_fileLog = NULL;
	Initiate();
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
LogFile::~LogFile()
{
	Terminate();
}

/*----------------------------------------------------------------------------------------------
	Initiates the log file. File will be created if it doesn't already exist.
----------------------------------------------------------------------------------------------*/
void LogFile::Initiate()
{
	char FilePath[200];
	strcpy(FilePath, BaseDirectory);
	strcat(FilePath, "\\");
	strcat(FilePath, "AutoSurveyor.log");

	// Creat/open the log file:
	m_fileLog = fopen(FilePath, "a");
}


/*----------------------------------------------------------------------------------------------
	Writes initial text to log file.
----------------------------------------------------------------------------------------------*/
void LogFile::Start()
{
	TimeStamp();
	Write(" AutoSurveyor started.");
	Write(".\n");
}


/*----------------------------------------------------------------------------------------------
	Writes given text to log file.
----------------------------------------------------------------------------------------------*/
void LogFile::Write(const char * szText)
{
	if (m_fileLog)
		fputs(szText, m_fileLog);

	if (szText[strlen(szText) - 1] == '\n')
		fflush(m_fileLog);
}


/*----------------------------------------------------------------------------------------------
	Writes the current date and time to the log file.
----------------------------------------------------------------------------------------------*/
void LogFile::TimeStamp()
{
	if (m_fileLog)
	{
		SYSTEMTIME syst;
		GetLocalTime(&syst);

		char str[100];
		sprintf(str, "%4d-%02d-%02d %02d:%02d:%02d", syst.wYear, syst.wMonth, syst.wDay,
			syst.wHour, syst.wMinute, syst.wSecond);
		fputs(str, m_fileLog);
	}
}


/*----------------------------------------------------------------------------------------------
	Closes down the log file, in case we wish to restart before object goes out of scope.
----------------------------------------------------------------------------------------------*/
void LogFile::Terminate()
{
	if (m_fileLog)
	{
		Write("AutoSurveyor ended at ");
		TimeStamp();
		Write(".\n\n");
		fclose(m_fileLog);
		m_fileLog = NULL;
	}
}
