/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: UniConvert.cpp
Responsibility: Darrell Zook
Last reviewed:

	Implementation file for UniConvert.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


extern const char * kpszWhiteSpace;


int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
	if (FAILED(CoInitialize(NULL)))
		return 1;
	UniConvert * puc = new UniConvert(lpCmdLine);
	puc->DoConversion(hInstance);
	delete puc;
	CoUninitialize();
	return 0;
}


UniConvert::UniConvert(char * pszCmdLine)
{
	AssertPsz(pszCmdLine);
	memset(m_szFiles, 0, sizeof(m_szFiles));
	m_fSilent = false;
	ParseCommandLine(pszCmdLine);
}

UniConvert::~UniConvert()
{
}

UniConvert::ParseCommandLine(char * pszCmdLine)
{
	AssertPsz(pszCmdLine);
	char * prgchPos = strtok(pszCmdLine, kpszWhiteSpace);

	// Set default error log
	strcpy(m_szFiles[kfoErrorLog], "UniConvert.log");

	while (prgchPos)
	{
		if (*prgchPos == '-')
		{
			char ch = *(++prgchPos);
			prgchPos = strtok(NULL, kpszWhiteSpace);
			switch (ch)
			{
			case 'c':
				strncpy(m_szFiles[kfoControlFile], prgchPos, MAX_PATH);
				break;

			case 'e':
				strncpy(m_szFiles[kfoErrorLog], prgchPos, MAX_PATH);
				break;

			case 'o':
				strncpy(m_szFiles[kfoOutputFile], prgchPos, MAX_PATH);
				break;

			case 's':
				m_fSilent = true;
				continue;

			default:
				{
					char szMessage[] =
						 "Usage: UniConvert -c control.txt [-e error.txt] -o output.txt input.txt\n\n"
						 "   -c\tGives a control file that specifies input and output formats and\n"
						 "\tcontains a list of encodings with corresponding mapping files.\n"
						 "   -e\tOptionally specifies a file for error reports during conversion.\n"
						 "\tIt defaults to UniConvert.log.\n"
						 "   -o\tSpecifies the output file.\n";
					MessageBox(NULL, szMessage, "Unicovert", MB_OK | MB_ICONINFORMATION);
					ExitProcess(0);
				}
			}
		}
		else
		{
			strncpy(m_szFiles[kfoInputFile], prgchPos, MAX_PATH);
		}
		prgchPos = strtok(NULL, kpszWhiteSpace);
	}
}

UniConvert::DoConversion(HINSTANCE hInstance)
{
	GetCurrentDirectory(MAX_PATH, m_szFiles[0]);
	MainDlg * m_pmd = new MainDlg(m_szFiles, m_fSilent, hInstance);
	if (m_pmd)
	{
		m_pmd->Create();
		delete m_pmd;
	}
}