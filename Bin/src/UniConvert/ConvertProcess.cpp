/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: ConvertProcess.cpp
Responsibility: Darrell Zook
Last reviewed:

	Implementation file for ConvertProcess.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


const char * kpszWhiteSpace = " \t\r\n\v";
const char * kpszWedgeSpace = " \t\r\n\v>";
const OLECHAR * kpwszWedgeSpace = L" \t\r\n\v>";
const int knEncodingChunk = 5;
const int knStackChunk = 4;
const int knProgessChunk = 1024;

const char * kpszOutOfMemory = "Out of memory.\n";
const char * kpszMultipleConverts = "More than one convert statement was found in the control file (line %d).\n";
const char * kpszInvalidConvert = "Invalid convert statement at line %d.\n";
const char * kpszNoConvert = "The convert statement must come before encoding statements (line %d).\n";
const char * kpszInvalidEncoding = "Invalid encoding statement at line %d.\n";
const char * kpszComCreateError = "Could not create COM object at line %d. Make sure the ConvertString.dll is registered.\n";
const char * kpszComError = "A COM error has occurred at line %d.\n";
const char * kpszInvalidLine = "Invalid statement at line %d.\n";
const char * kpszEmptyControlFile = "The control file did not contain anything.\n";
const char * kpszControlReadError = "There was an error when reading the control file.\n";
const char * kpszNoOpenTagClose = "The end of the file occurred before the final String open tag was closed.\n";
const char * kpszExtraEndTag = "An extra end String tag was found on line %d.\n";
const char * kpszInvalidChar = "The invalid character 0x%x on line %d was converted to \'%c\'.\n";


const ULONG kReplacementCharacter = 0x0000FFFDUL;
const ULONG kMaximumSimpleUniChar = 0x0000FFFFUL;
const ULONG kMaximumUniChar = 0x0010FFFFUL;
const ULONG kMaximumUCS4 = 0x7FFFFFFFUL;

const int halfShift = 10;
const ULONG halfBase = 0x0010000UL;
const ULONG halfMask = 0x3FFUL;
const ULONG kSurrogateHighStart = 0xD800UL;
const ULONG kSurrogateHighEnd = 0xDBFFUL;
const ULONG kSurrogateLowStart = 0xDC00UL;
const ULONG kSurrogateLowEnd = 0xDFFFUL;

ULONG offsetsFromUTF8[6] = {
	0x00000000UL, 0x00003080UL, 0x000E2080UL,
	0x03C82080UL, 0xFA082080UL, 0x82082080UL
};

char bytesFromUTF8[256] = {
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
	2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2, 3,3,3,3,3,3,3,3,4,4,4,4,5,5,5,5
};

BYTE firstByteMark[7] = {0x00, 0x00, 0xC0, 0xE0, 0xF0, 0xF8, 0xFC};

//////////////////////////////////////////////////////////////////////
// Callback function for the progress dialog
//////////////////////////////////////////////////////////////////////

BOOL CALLBACK ProgressDlgProc(HWND hwndDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	if (uMsg == WM_INITDIALOG)
	{
		RECT rc, rcP;
		HWND hwndParent = GetParent(hwndDlg);
		GetWindowRect(hwndParent, &rcP);
		GetWindowRect(hwndDlg, &rc);
		OffsetRect(&rc,
			rcP.left + ((rcP.right - rcP.left) >> 1) - ((rc.right - rc.left) >> 1) - rc.left,
			rcP.top + ((rcP.bottom - rcP.top) >> 1) - ((rc.bottom - rc.top) >> 1) - rc.top);
		SetWindowPos(hwndDlg, NULL, rc.left, rc.top, 0, 0, SWP_NOZORDER | SWP_NOSIZE);
	}
	return 0;
}


///////////////////////////////////////////////////////////////////////////
// ConvertProcess functions
///////////////////////////////////////////////////////////////////////////

ConvertProcess::ConvertProcess(HWND hwndParent, HINSTANCE hInstance)
{
	m_prgei = NULL;
	m_nDefaultEncOffset = m_nCurrentEncOffset = -1;
	m_cei = m_cErrors = 0;
	m_hFileMap = 0;
	m_pFileStart = NULL;
	m_hwndParent = hwndParent;
	m_hInstance = hInstance;
}

ConvertProcess::~ConvertProcess()
{
}

const void * ConvertProcess::GetTopEncoding()
{
	if (m_nCurrentEncOffset > -1 &&
		m_nCurrentEncOffset < m_cei && m_prgei)
	{
		return m_prgei[m_nCurrentEncOffset]->pxxxCode;
	}
	return NULL;
}

bool ConvertProcess::WriteError(const char * pszError, int nParam1, int nParam2, int nParam3)
{
	if (pszError && m_cErrors < ERROR_LIMIT)
	{
		DWORD dwT;
		char szMessage[MAX_PATH];
		wsprintf(szMessage, pszError, nParam1, nParam2, nParam3);
		WriteFile(m_hLogFile, szMessage, lstrlen(szMessage), &dwT, NULL);
		m_cErrors++;
	}
	return false;
}

bool ConvertProcess::Process(char szFiles[NUM_COMBOS][MAX_PATH], bool fSilent)
{
	bool fSuccess = true;
	if ((m_hLogFile = CreateFile(szFiles[kfoErrorLog], GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, 0, 0)) == INVALID_HANDLE_VALUE)
	{
		MessageBox(NULL, "The log file could not be opened.", "UniConvert", MB_OK);
		return false;
	}

	time_t ltime;
	time(&ltime);
	char szBuffer[MAX_PATH];
	wsprintf(szBuffer, "Starting UniConvert process on %s", ctime(&ltime));
	DWORD dwT;
	WriteFile(m_hLogFile, szBuffer, strlen(szBuffer), &dwT, NULL);
	m_cErrors = 0;

	if ((m_hControlFile = CreateFile(szFiles[kfoControlFile], GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, 0)) == INVALID_HANDLE_VALUE)
		fSuccess = WriteError("\nThe control file could not be opened.\n");
	if ((m_hInputFile = CreateFile(szFiles[kfoInputFile], GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, 0)) == INVALID_HANDLE_VALUE)
		fSuccess = WriteError("\nThe input file could not be opened.\n");
	if ((m_hOutputFile = CreateFile(szFiles[kfoOutputFile], GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, 0, 0)) == INVALID_HANDLE_VALUE)
		fSuccess = WriteError("\nThe output file could not be created.\n");
	strcpy(m_szOutputFile, szFiles[kfoOutputFile]);

	if (fSuccess)
	{
		fSuccess = false;
		HWND hwndProgressDlg = NULL;
		HWND hwndProgress = NULL;
		if (!fSilent)
		{
			hwndProgressDlg = CreateDialog(m_hInstance, MAKEINTRESOURCE(IDD_PROGRESS), m_hwndParent, ProgressDlgProc);
			hwndProgress = GetDlgItem(hwndProgressDlg, IDC_PROGRESS);
			SendMessage(hwndProgress, PBM_SETPOS, 0, 0);
		}
		if (ParseControlFile())
		{
			IUnknown * punk;
			if (m_ct == kctUTF8toANSI || m_ct == kctUTF16toANSI)
				CsCallbackWToA::Create(this, (ICsCallbackWToA **)&punk);
			else
				CsCallbackAToW::Create(this, (ICsCallbackAToW **)&punk);
			if (punk)
			{
				if (m_ct == kctANSItoUTF16)
				{
					// Write 0xFEFF to the UTF-16 file.
					DWORD dwT;
					WriteFile(m_hOutputFile, "ÿþ", 2, &dwT, NULL);
				}
				if (ParseInputFile(punk, hwndProgress))
					fSuccess = true;
				punk->Release();
			}
		}
		if (!fSilent)
			DestroyWindow(hwndProgressDlg);
	}
	if (m_cErrors == ERROR_LIMIT)
		WriteError("Error logging was stopped after %d errors.\n", (--m_cErrors) + 1);
	if (m_cErrors && !fSilent)
		MessageBox(m_hwndParent, "There were some errors during the conversion. Look at the log file for more information.", "UniConvert", MB_OK);
	CloseHandle(m_hControlFile);
	CloseHandle(m_hInputFile);
	CloseHandle(m_hOutputFile);
	CloseHandle(m_hLogFile);
	if (m_prgei)
	{
		for (int iei = 0; iei < m_cei; iei++)
		{
			if (m_prgei[iei]->pxxxCode)
				delete m_prgei[iei]->pxxxCode;
			delete m_prgei[iei];
		}
		delete m_prgei;
		m_prgei = NULL;
		m_cei = 0;
	}
	return fSuccess;
}

bool ConvertProcess::ParseControlFile()
{
	DWORD dwT;
	WriteFile(m_hLogFile, "\nParsing control file...\n", 25, &dwT, NULL);
	int iLine = 0;
	DWORD dwBytesRead;
	DWORD dwFileSize = GetFileSize(m_hControlFile, NULL);
	if (!dwFileSize)
		return WriteError(kpszEmptyControlFile);
	char * pFileText = NewObj char[dwFileSize + 2]; // end with 2 NUL characters
	if (!pFileText)
		return WriteError(kpszOutOfMemory);
	pFileText[dwFileSize] = pFileText[dwFileSize + 1] = 0;
	ReadFile(m_hControlFile, pFileText, dwFileSize, &dwBytesRead, NULL);

	ICsCallbackAToWPtr qccaw;
	if (FAILED(CsCallbackAToW::Create(this, &qccaw)))
		return WriteError(kpszOutOfMemory);
	ICsCallbackWToAPtr qccwa;
	if (FAILED(CsCallbackWToA::Create(this, &qccwa)))
		return WriteError(kpszOutOfMemory);

	bool fSuccess = true;
	bool fFoundConvert = false;
	if (dwFileSize == dwBytesRead)
	{
		char * pPos = pFileText;
		char * pStop;
		do
		{
			iLine++;
			pPos += strspn(pPos, "\r\n");
			pStop = pPos + strcspn(pPos, "\r\n");
			if (pStop && pPos != pStop)
				*pStop = 0;
			// Parse the line if it is not empty and it isn't a comment
			if (*pPos != '#' && *pPos)
			{
				if (!ParseControlLine(&fFoundConvert, pPos, iLine, qccaw, qccwa))
					fSuccess = false;
			}
		} while (*(pPos = pStop + 1));
	}
	else
	{
		fSuccess = WriteError(kpszControlReadError);
	}
	delete pFileText;
	if (!fSuccess || m_cErrors > 0)
		return false;

	if (!m_cErrors)
		WriteFile(m_hLogFile, "Successful!\n", 12, &dwT, NULL);
	return fSuccess;
}

bool ConvertProcess::ParseControlLine(bool * pfFoundConvert, char * pPos, int iLine,
	ICsCallbackAToW * pccaw, ICsCallbackWToA * pccwa)
{
	if (!pfFoundConvert || !pPos)
		return WriteError("Initialization error!\n");

	char * pToken1 = strtok(pPos, kpszWhiteSpace);
	if (!pToken1)
		return WriteError("Initialization error!\n");

	if (stricmp(pToken1, "convert") == 0)
	{
		char * pToken2 = strtok(NULL, kpszWhiteSpace);
		char * pToken3 = strtok(NULL, kpszWhiteSpace);
		char * pToken4 = strtok(NULL, kpszWhiteSpace);
		char * pToken5 = strtok(NULL, kpszWhiteSpace);
		if (!pToken2 || !pToken3 || !pToken4 || !pToken5 || stricmp(pToken2, "from") || stricmp(pToken4, "to"))
			return WriteError(kpszInvalidConvert, iLine);
		if (*pfFoundConvert)
			return WriteError(kpszMultipleConverts, iLine);
		m_ct = (ConversionType) -1;
		if (stricmp(pToken3, "ANSI") == 0)
		{
			if (stricmp(pToken5, "UTF-8") == 0)
				m_ct = kctANSItoUTF8;
			else if (stricmp(pToken5, "UTF-16") == 0)
				m_ct = kctANSItoUTF16;
		}
		else if (stricmp(pToken3, "UTF-8") == 0)
		{
			if (stricmp(pToken5, "ANSI") == 0)
				m_ct = kctUTF8toANSI;
		}
		else if (stricmp(pToken3, "UTF-16") == 0)
		{
			if (stricmp(pToken5, "ANSI") == 0)
				m_ct = kctUTF16toANSI;
		}
		if (m_ct == (ConversionType)-1)
			return WriteError(kpszInvalidConvert, iLine);
		*pfFoundConvert = true;
	}
	else
	{
		if (!*pfFoundConvert)
			return WriteError(kpszNoConvert, iLine);
		char * pToken2 = strtok(NULL, " \t\r\n\"");
		char * pToken3 = strtok(NULL, "\r\n\"");
		if (!pToken2 || !pToken3)
			return WriteError(kpszInvalidEncoding, iLine);
		if (m_cei % knEncodingChunk == 0)
		{
			m_prgei = (EncodingInfo **)realloc(m_prgei, (m_cei + knEncodingChunk) *
				sizeof(EncodingInfo *));
		}
		if (!m_prgei)
			return WriteError(kpszOutOfMemory, iLine);
		EncodingInfo * peiNew = m_prgei[m_cei++] = NewObj EncodingInfo;
		if (!peiNew)
			return WriteError(kpszOutOfMemory, iLine);
		int cch = strlen(pToken1);
		if (m_ct == kctUTF16toANSI)
			peiNew->pxxxCode = NewObj OLECHAR[cch + 1];
		else
			peiNew->pxxxCode = NewObj char[cch + 1];
		if (!peiNew->pxxxCode)
		{
			delete peiNew;
			return WriteError(kpszOutOfMemory, iLine);
		}
		if (m_ct == kctUTF16toANSI)
		{
			OLECHAR * prgchDst = (OLECHAR *)(peiNew->pxxxCode);
			char * prgchSrc = pToken1;
			char * prgchLim = prgchSrc + cch;
			while (prgchSrc < prgchLim)
				*prgchDst++ = *prgchSrc++;
			*prgchDst = 0;
		}
		else
		{
			memmove(peiNew->pxxxCode, pToken1, cch);
			((char *)(peiNew->pxxxCode))[cch] = 0;
		}
		GetFullPathName(pToken3, MAX_PATH, peiNew->szFileName, NULL);
		CLSID clsid;
		if (stricmp(pToken2, "TableLookup") == 0)
		{
			char szBuffer[MAX_PATH];
			DWORD dwBytesWritten;
			wsprintf(szBuffer, "Initializing: %s\n", peiNew->szFileName);
			WriteFile(m_hLogFile, szBuffer, strlen(szBuffer), &dwBytesWritten, NULL);
			peiNew->et = ketTableLookup;
			StrUni stubp = peiNew->szFileName;
			if (m_ct == kctUTF8toANSI || m_ct == kctUTF16toANSI)
			{
				if (FAILED(CLSIDFromProgID(L"SIL.CS.ConvertWToA", &clsid)))
					return WriteError(kpszComCreateError, iLine);
				if (FAILED(CoCreateInstance(clsid, NULL, CLSCTX_ALL, IID_ICsConvertWToA,
					(void **)&(peiNew->qunk))))
				{
					return WriteError(kpszComCreateError, iLine);
				}
				if (FAILED(((ICsConvertWToA *)(peiNew->qunk.Ptr()))->Initialize(stubp.Bstr(),
					true, pccwa)))
				{
					return WriteError("Initialization error!\n");
				}
			}
			else
			{
				if (FAILED(CLSIDFromProgID(L"SIL.CS.ConvertAToW", &clsid)))
					return WriteError(kpszComCreateError, iLine);
				if (FAILED(CoCreateInstance(clsid, NULL, CLSCTX_ALL, IID_ICsConvertAToW,
					(void **)&(peiNew->qunk))))
				{
					return WriteError(kpszComCreateError, iLine);
				}
				if (FAILED(((ICsConvertAToW *)(peiNew->qunk.Ptr()))->Initialize(stubp.Bstr(),
					true, pccaw)))
				{
					return WriteError("Initialization error!\n");
				}
			}
		}
		else
		{
			// TODO: Add more possibilities here. (CC, PERL, etc.)
			//		 If something is added, a corresponding entry for
			//		 it should be made in the EncodingType enum.
			return WriteError(kpszInvalidLine, iLine);
		}
	}
	return true;
}

bool ConvertProcess::ParseInputFile(IUnknown * punk, HWND hwndProgress)
{
	DWORD dwBytesWritten;
	WriteFile(m_hLogFile, "\nParsing input file...\n", 23, &dwBytesWritten, NULL);
	m_hFileMap = CreateFileMapping(m_hInputFile, NULL, PAGE_READONLY, 0, 0, NULL);;
	if (!m_hFileMap)
		return false;
	m_pFileStart = MapViewOfFile(m_hFileMap, FILE_MAP_READ, 0, 0, 0);
	if (!m_pFileStart)
	{
		CloseHandle(m_hFileMap);
		m_hFileMap = NULL;
		return false;
	}
	bool fSuccess = true;
	DWORD dwFileSize = GetFileSize(m_hInputFile, NULL);
	m_nDefaultEncOffset = -1;
	m_iLine = 1;
	SendMessage(hwndProgress, PBM_SETRANGE32, 0, dwFileSize);

	__try
	{
		if (m_ct != kctUTF16toANSI)
		{
			char * pFileStop = (char *)m_pFileStart + dwFileSize;
			while (strnicmp((char *)m_prgei[++m_nDefaultEncOffset]->pxxxCode, "default", 7));
			m_nCurrentEncOffset = m_nDefaultEncOffset;
			char * pPos = (char *)m_pFileStart;
			char * pRunStart = (char *)m_pFileStart;
			char * pChangeProgress = pPos + knProgessChunk;

			while (pPos < pFileStop)
			{
				if (pPos > pChangeProgress)
				{
					pChangeProgress = pPos + knProgessChunk;
					SendMessage(hwndProgress, PBM_SETPOS, pChangeProgress - (char*)m_pFileStart, 0);
				}
				if (*pPos == '<')
				{
					// "<Uni>", "<Str>", "<Run ...>", "<AStr ...>", or "<AUni ...>"
					if ((pPos + 4 < pFileStop && strnicmp(pPos + 1, "Uni>", 4) == 0) ||
						(pPos + 4 < pFileStop && strnicmp(pPos + 1, "Str>", 4) == 0) ||
						(pPos + 4 < pFileStop && strnicmp(pPos + 1, "Run",  3) == 0 &&
							strchr(kpszWedgeSpace, *(pPos + 4))) ||
						(pPos + 5 < pFileStop && strnicmp(pPos + 1, "AStr", 4) == 0 &&
							strchr(kpszWedgeSpace, *(pPos + 5))) ||
						(pPos + 5 < pFileStop && strnicmp(pPos + 1, "AUni", 4) == 0 &&
							strchr(kpszWedgeSpace, *(pPos + 5))))
					{
						m_nCurrentEncOffset = m_nDefaultEncOffset;
						if (Write8ToFile(pRunStart, pPos - pRunStart) &&
							ParseTsString8(&pPos, pFileStop, punk))
						{
							pRunStart = pPos;
							continue;
						}
						fSuccess = false;
						break;
					}
				}
				else if (*pPos == 10)
					m_iLine++;
				pPos++;
			}
			if (!Write8ToFile(pRunStart, pPos - pRunStart))
				fSuccess = false;
		}
		else
		{
			OLECHAR * pFileStop = (OLECHAR *)m_pFileStart + (dwFileSize >> 1);
			while (wcsnicmp((OLECHAR *)m_prgei[++m_nDefaultEncOffset]->pxxxCode, L"default", 7));
			m_nCurrentEncOffset = m_nDefaultEncOffset;
			OLECHAR * pPos = (OLECHAR *)m_pFileStart;
			OLECHAR * pRunStart = (OLECHAR *)m_pFileStart;
			OLECHAR * pChangeProgress = pPos + knProgessChunk;

			while (pPos < pFileStop)
			{
				if (pPos > pChangeProgress)
				{
					pChangeProgress = pPos + knProgessChunk;
					SendMessage(hwndProgress, PBM_SETPOS, (char*)pChangeProgress - (char*)m_pFileStart, 0);
				}
				if (*pPos == '<')
				{
					// "<Uni>", "<Str>", "<Run ...>", "<AStr ...>", or "<AUni ...>"
					if ((pPos + 4 < pFileStop && wcsnicmp(pPos + 1, L"Uni>", 4) == 0) ||
						(pPos + 4 < pFileStop && wcsnicmp(pPos + 1, L"Str>", 4) == 0) ||
						(pPos + 4 < pFileStop && wcsnicmp(pPos + 1, L"Run",  3) == 0 &&
							wcschr(kpwszWedgeSpace, *(pPos + 4))) ||
						(pPos + 5 < pFileStop && wcsnicmp(pPos + 1, L"AStr", 4) == 0 &&
							wcschr(kpwszWedgeSpace, *(pPos + 5))) ||
						(pPos + 5 < pFileStop && wcsnicmp(pPos + 1, L"AUni", 4) == 0 &&
							wcschr(kpwszWedgeSpace, *(pPos + 5))))
					{
						m_nCurrentEncOffset = m_nDefaultEncOffset;
						if (Write16ToFile(pRunStart, pPos - pRunStart) &&
							ParseTsString16(&pPos, pFileStop, punk))
						{
							pRunStart = pPos;
							continue;
						}
						fSuccess = false;
						break;
					}
				}
				else if (*pPos == 10)
					m_iLine++;
				pPos++;
			}
			if (!Write16ToFile(pRunStart, pPos - pRunStart))
				fSuccess = false;
		}
	}
	__except(EXCEPTION_EXECUTE_HANDLER)
	{
		fSuccess = WriteError("An exception has occurred while reading the input file.\n");
	}
	UnmapViewOfFile(m_pFileStart);
	m_pFileStart = NULL;
	CloseHandle(m_hFileMap);
	m_hFileMap = NULL;
	if (!m_cErrors)
		WriteFile(m_hLogFile, "Successful!\n", 12, &dwBytesWritten, NULL);
	return fSuccess;
}

bool ConvertProcess::ParseTsString8(char ** ppPos, char * pFileStop, IUnknown * punk)
{
	if (!ppPos)
		return false;

	// When in doubt, use the default.
	m_nCurrentEncOffset = m_nDefaultEncOffset;
	char * pTagStart = *ppPos;

	// Only process tags that have string data in them.
	if ((pTagStart + 5 < pFileStop && strnicmp(pTagStart, "<Uni>", 5) == 0) ||
		(pTagStart + 5 < pFileStop && strnicmp(pTagStart, "<Str>", 5) == 0) ||
		(pTagStart + 5 < pFileStop && strnicmp(pTagStart, "<Run",  4) == 0 &&
			strchr(kpszWedgeSpace, pTagStart[4])) ||
		(pTagStart + 6 < pFileStop && strnicmp(pTagStart, "<AStr", 5) == 0 &&
			strchr(kpszWedgeSpace, pTagStart[5])) ||
		(pTagStart + 6 < pFileStop && strnicmp(pTagStart, "<AUni", 5) == 0 &&
			strchr(kpszWedgeSpace, pTagStart[5])))
	{
		char * pTagStop = pTagStart;
		char ch;
		while (++pTagStop < pFileStop && (ch = *pTagStop) != '>')
		{
			if (ch == 10)
				m_iLine++;
		}

		// Check some tags for 'ws', as they can change the encoding from the default.
		if (strnicmp(pTagStart, "<AUni", 5) == 0 ||
			strnicmp(pTagStart, "<AStr", 5) == 0 ||
			strnicmp(pTagStart, "<Run",  4) == 0)

		{
			// Find out which encoding is to be used.
			char * pEncodingStart = pTagStart;
			while (++pEncodingStart < pTagStop)
			{
				if (isspace(*pEncodingStart) && strnicmp(pEncodingStart + 1, "ws", 2) == 0)
				{
					pEncodingStart += 2;
					while (++pEncodingStart < pTagStop && isspace(ch = *pEncodingStart))
					{
						if (ch == 10)
							m_iLine++;
					}
					if (pEncodingStart < pTagStop && *pEncodingStart == '=')
					{
						while (++pEncodingStart < pTagStop && isspace(ch = *pEncodingStart))
						{
							if (ch == 10)
								m_iLine++;
						}
						if (pEncodingStart < pTagStop && (*pEncodingStart == '\'' || *pEncodingStart == '\"'))
						{
							char * pEncodingStop = pEncodingStart;
							while (++pEncodingStop < pTagStop && *pEncodingStop != *pEncodingStart);
							int nLength = pEncodingStop - (++pEncodingStart);
							if (pEncodingStop < pTagStop && nLength > 0)
							{
								m_nCurrentEncOffset = -1;
								while (++m_nCurrentEncOffset < m_cei &&
									strnicmp((char*)m_prgei[m_nCurrentEncOffset]->pxxxCode, pEncodingStart, nLength));
								if (m_nCurrentEncOffset == m_cei)
								{
									char szBuffer[MAX_PATH] = "Warning: An unknown encoding (";
									strncat(szBuffer, pEncodingStart, nLength);
									strcat(szBuffer, ") was found on line %d. The default encoding was used.\n");
									WriteError(szBuffer, m_iLine);
									m_nCurrentEncOffset = m_nDefaultEncOffset;
								}
							break;
							}
						}
					}
				}
			}
		}

		if (pTagStop == pFileStop)
			return WriteError(kpszNoOpenTagClose, m_iLine);

		char * pStringStart = pTagStop + 1;
		char * pStringStop = pTagStop;
		while (++pStringStop < pFileStop && (ch = *pStringStop) != '<')
		{
			if (ch == 10)
				m_iLine++;
		}
		// Write out start tag.
		if (!Write8ToFile(*ppPos, pStringStart - *ppPos))
			return false;
		*ppPos = pStringStart;
		// Convert FW string.
		if (!ConvertText8(pStringStart, pStringStop - pStringStart, punk))
			return false;
		*ppPos = pStringStop;
		return true;
	}
	else return false;
}

bool ConvertProcess::ParseTsString16(OLECHAR ** ppPos, OLECHAR * pFileStop, IUnknown * punk)
{
	if (!ppPos)
		return false;

	// When in doubt, use the default.
	m_nCurrentEncOffset = m_nDefaultEncOffset;
	OLECHAR * pTagStart = *ppPos;

	// Only process tags that have string data in them.
	if ((pTagStart + 5 < pFileStop && wcsnicmp(pTagStart, L"<Uni>", 5) == 0) ||
		(pTagStart + 5 < pFileStop && wcsnicmp(pTagStart, L"<Str>", 5) == 0) ||
		(pTagStart + 5 < pFileStop && wcsnicmp(pTagStart, L"<Run",  4) == 0 &&
			wcschr(kpwszWedgeSpace, pTagStart[4])) ||
		(pTagStart + 6 < pFileStop && wcsnicmp(pTagStart, L"<AStr", 5) == 0 &&
			wcschr(kpwszWedgeSpace, pTagStart[5])) ||
		(pTagStart + 6 < pFileStop && wcsnicmp(pTagStart, L"<AUni", 5) == 0 &&
			wcschr(kpwszWedgeSpace, pTagStart[5])))
	{
		OLECHAR * pTagStop = pTagStart;
		OLECHAR ch;
		while (++pTagStop < pFileStop && (ch = *pTagStop) != '>');
		{
			if (ch == 10)
				m_iLine++;
		}

		// Check some tags for 'ws', as they can change the encoding from the default.
		if (wcsnicmp(pTagStart, L"<AUni", 5) == 0 ||
			wcsnicmp(pTagStart, L"<AStr", 5) == 0 ||
			wcsnicmp(pTagStart, L"<Run",  4) == 0)

		{
			// Find out which encoding is to be used.
			OLECHAR * pEncodingStart = pTagStart;
			while (++pEncodingStart < pTagStop)
			{
				if (iswspace(*pEncodingStart) && wcsnicmp(pEncodingStart + 1, L"ws", 2) == 0)
				{
					pEncodingStart += 2;
					while (++pEncodingStart < pTagStop && iswspace(ch = *pEncodingStart))
					{
						if (ch == 10)
							m_iLine++;
					}
					if (pEncodingStart < pTagStop && *pEncodingStart == '=')
					{
						while (++pEncodingStart < pTagStop && iswspace(ch = *pEncodingStart))
						{
							if (ch == 10)
								m_iLine++;
						}
						if (pEncodingStart < pTagStop && (*pEncodingStart == '\'' || *pEncodingStart == '\"'))
						{
							OLECHAR * pEncodingStop = pEncodingStart;
							while (++pEncodingStop < pTagStop && *pEncodingStop != *pEncodingStart);
							int nLength = pEncodingStop - (++pEncodingStart);
							if (pEncodingStop < pTagStop && nLength > 0)
							{
								m_nCurrentEncOffset = -1;
								while (++m_nCurrentEncOffset < m_cei &&
									wcsnicmp((OLECHAR *)m_prgei[m_nCurrentEncOffset]->pxxxCode, pEncodingStart, nLength));
								if (m_nCurrentEncOffset == m_cei)
								{
									char szBuffer[MAX_PATH] = "Warning: An unknown encoding (";
									int nStartLength = strlen(szBuffer);
									WideCharToMultiByte(CP_ACP, 0, pEncodingStart, nLength, szBuffer + nStartLength, MAX_PATH - nStartLength, NULL, NULL);
									szBuffer[nStartLength + nLength] = 0;
									strcat(szBuffer, ") was found on line %d. The default encoding was used.\n");
									WriteError(szBuffer, m_iLine);
									m_nCurrentEncOffset = m_nDefaultEncOffset;
								}
							break;
							}
						}
					}
				}
			}
		}

		if (pTagStop == pFileStop)
			return WriteError(kpszNoOpenTagClose, m_iLine);

		OLECHAR * pStringStart = pTagStop + 1;
		OLECHAR * pStringStop = pTagStop;
		while (++pStringStop < pFileStop && (ch = *pStringStop) != '<');
		{
			if (ch == 10)
				m_iLine++;
		}
		if (!Write16ToFile(*ppPos, (pStringStart - *ppPos)))
			return false;
		*ppPos = pStringStart;
		// Convert FW string.
		if (!ConvertText16(pStringStart, pStringStop - pStringStart, punk))
			return false;
		*ppPos = pStringStop;
		return true;
	}
	else return false;
}

bool ConvertProcess::ConvertText8(char * prgch, int cch, IUnknown * punk)
{
	char * pBuffer = NewObj char[cch + 1];
	if (!pBuffer)
		return WriteError(kpszOutOfMemory);
	char * pSrc;
	char * pDst;
	char * pStop;
	char ch;
	int cLines = 0;

	// Convert coded characters
	for (pSrc = prgch, pDst = pBuffer, pStop = pSrc + cch; pSrc < pStop; pSrc++)
	{
		if ((ch = *pSrc) != '&')
		{
			if (ch == 10)
				cLines++;
			*pDst++ = ch;
		}
		else
		{
			if (*(++pSrc) == '#')
			{
				char * pStop;
				int nChar = strtol(pSrc + 1, &pStop, 10);
				if (*pStop == ';')
				{
					*pDst++ = (char)nChar;
					pSrc = pStop;
				}
				else
				{
					*pDst++ = '&';
					*pDst++ = '#';
				}
			}
			else
			{
				if (strnicmp(pSrc, "amp;", 4) == 0)
				{
					*pDst++= '&';
					pSrc += 3;
				}
				else if (strnicmp(pSrc, "lt;", 3) == 0)
				{
					*pDst++= '<';
					pSrc += 2;
				}
				else if (strnicmp(pSrc, "gt;", 3) == 0)
				{
					*pDst++= '>';
					pSrc += 2;
				}
				else
				{
					*pDst++ = '&';
					*pDst++ = *pSrc;
				}
			}
		}
	}
	*pDst = 0;
	cch = strlen(pBuffer);
	int cchNeeded = 0;
	bool fSuccess = true;
	if (m_ct == kctUTF8toANSI)
	{
		OLECHAR * pwBuffer = NewObj OLECHAR[cch + 1];
		if (pwBuffer)
		{
			OLECHAR * pDst = pwBuffer;
			char * pSrc = pBuffer;
			char * pStop = pBuffer + cch;
			while (pSrc < pStop)
				*pDst++ = *pSrc++;
			if (SUCCEEDED(((ICsConvertWToA *)m_prgei[m_nCurrentEncOffset]->qunk.Ptr())->ConvertRgch(pwBuffer,
				cch, m_rgchOutput, BUFFER_SIZE, (ICsCallbackWToA *)punk, &cchNeeded)))
			{
				// Find the needed size.
				if (cchNeeded <= BUFFER_SIZE)
				{
					fSuccess = WriteStringToFile8((char *)m_rgchOutput, cchNeeded);
				}
				else
				{
					char * prgchOutput = NewObj char[cchNeeded + 1];
					if (prgchOutput)
					{
						if (SUCCEEDED(((ICsConvertWToA *)m_prgei[m_nCurrentEncOffset]->qunk.Ptr())->ConvertRgch(
							pwBuffer, cch, (byte *)prgchOutput, cchNeeded + 1, (ICsCallbackWToA *)punk,
							&cchNeeded)))
						{
							fSuccess = WriteStringToFile8(prgchOutput, cchNeeded);
							delete prgchOutput;
						}
						else
						{
							fSuccess = WriteError(kpszComError, m_iLine);
						}
					}
					else
					{
						fSuccess = WriteError(kpszOutOfMemory);
					}
				}
			}
			else
			{
				fSuccess = WriteError(kpszComError, m_iLine);
			}
			delete pwBuffer;
		}
		else
		{
			fSuccess = WriteError(kpszOutOfMemory);
		}
	}
	else
	{
		if (SUCCEEDED(((ICsConvertAToW *)m_prgei[m_nCurrentEncOffset]->qunk.Ptr())->ConvertRgch((byte *)pBuffer,
			cch, m_rgwchOutput, BUFFER_SIZE, (ICsCallbackAToW *)punk, &cchNeeded)))
		{
			OLECHAR * prgchOutput = m_rgwchOutput;
			if (cchNeeded > BUFFER_SIZE)
			{
				prgchOutput = NewObj OLECHAR[cchNeeded + 1];
				if (prgchOutput)
				{
					if (FAILED(((ICsConvertAToW *)m_prgei[m_nCurrentEncOffset]->qunk.Ptr())->ConvertRgch((byte *)pBuffer, cch,
						prgchOutput, cchNeeded + 1, (ICsCallbackAToW *)punk, &cchNeeded)))
					{
						fSuccess = false;
					}
				}
				else
				{
					fSuccess = WriteError(kpszOutOfMemory);
				}
			}
			if (fSuccess)
			{
				if (m_ct == kctANSItoUTF8)
					fSuccess = WriteUTF16ToUTF8File(prgchOutput, cchNeeded);
				else
					fSuccess = WriteStringToFile16(prgchOutput, cchNeeded);
			}
			if (prgchOutput != m_rgwchOutput)
				delete prgchOutput;
		}
		else
		{
			fSuccess = WriteError(kpszComError, m_iLine);
		}
	}
	delete pBuffer;
	m_iLine += cLines;
	return fSuccess;
}

bool ConvertProcess::ConvertText16(OLECHAR * prgch, int cch, IUnknown * punk)
{
	OLECHAR * pwBuffer = NewObj OLECHAR[cch + 1];
	if (!pwBuffer)
		return WriteError(kpszOutOfMemory);
	OLECHAR * pSrc;
	OLECHAR * pDst;
	OLECHAR * pStop;
	OLECHAR ch;
	int cLines = 0;

	// Convert coded characters
	for (pSrc = prgch, pDst = pwBuffer, pStop = pSrc + cch; pSrc < pStop; pSrc++)
	{
		if ((ch = *pSrc) != '&')
		{
			if (ch == 10)
				cLines++;
			*pDst++ = *pSrc;
		}
		else
		{
			if (*(++pSrc) == '#')
			{
				OLECHAR *pStop;
				int nChar = wcstol(pSrc + 1, &pStop, 10);
				if (*pStop == ';')
				{
					*pDst++ = (OLECHAR)nChar;
					pSrc = pStop;
				}
				else
				{
					*pDst++ = '&';
					*pDst++ = '#';
				}
			}
			else
			{
				if (wcsnicmp(pSrc, L"amp;", 4) == 0)
				{
					*pDst++= '&';
					pSrc += 3;
				}
				else if (wcsnicmp(pSrc, L"lt;", 3) == 0)
				{
					*pDst++= '<';
					pSrc += 2;
				}
				else if (wcsnicmp(pSrc, L"gt;", 3) == 0)
				{
					*pDst++= '>';
					pSrc += 2;
				}
				else
				{
					*pDst++ = '&';
					*pDst++ = *pSrc;
				}
			}
		}
	}
	*pDst = 0;
	cch = wcslen(pwBuffer);
	int cchNeeded = 0;
	bool fSuccess = true;
	if (SUCCEEDED(((ICsConvertWToA *)m_prgei[m_nCurrentEncOffset]->qunk.Ptr())->ConvertRgch(pwBuffer,
		cch, m_rgchOutput, BUFFER_SIZE, (ICsCallbackWToA *)punk, &cchNeeded)))
	{
		if (cchNeeded <= BUFFER_SIZE)
		{
			WriteStringToFile8((char *)m_rgchOutput, cchNeeded);
		}
		else
		{
			char * prgchOutput = NewObj char[cchNeeded + 1];
			if (prgchOutput)
			{
				if (SUCCEEDED(((ICsConvertWToA *)m_prgei[m_nCurrentEncOffset]->qunk.Ptr())->ConvertRgch(
					pwBuffer, cch, (byte *)prgchOutput, cchNeeded + 1, (ICsCallbackWToA *)punk, &cchNeeded)))
				{
					WriteStringToFile8(prgchOutput, cchNeeded);
					delete prgchOutput;
				}
			}
			else
			{
				fSuccess = WriteError(kpszOutOfMemory);
			}
		}
	}
	else
	{
		fSuccess = WriteError(kpszComError, m_iLine);
	}
	delete pwBuffer;
	m_iLine += cLines;
	return true;
}

bool ConvertProcess::Write8ToFile(char * prgch, int cch)
{
	AssertArray(prgch, cch);
	if (!cch)
		return true;

	DWORD dwT;
	switch (m_ct)
	{
		case kctANSItoUTF8:
			return WriteANSIToUTF8File(prgch, cch);

		case kctANSItoUTF16:
		{
			OLECHAR * prgchBuffer = NewObj OLECHAR[cch];
			if (!prgchBuffer)
				return WriteError(kpszOutOfMemory);
			OLECHAR * prgchDst = prgchBuffer;
			char * prgchSrc = prgch;
			char * prgchLim = prgch + cch;
			OLECHAR ch;
			while (prgchSrc < prgchLim)
			{
				if ((ch = *prgchSrc++) == 10)
					m_iLine++;
				*prgchDst++ = ch;
			}
			WriteFile(m_hOutputFile, prgchBuffer, cch << 1, &dwT, NULL);
			delete prgchBuffer;
			return true;
		}

		case kctUTF8toANSI:
			return WriteUTF8ToANSIFile(prgch, cch);
	}
	return true;
}

bool ConvertProcess::Write16ToFile(OLECHAR * prgch, int cch)
{
	AssertArray(prgch, cch);
	if (!cch)
		return true;

	char * prgchBuffer = NewObj char[cch];
	if (!prgchBuffer)
		return WriteError(kpszOutOfMemory);
	char * prgchDst = prgchBuffer;
	OLECHAR * prgchSrc = prgch;
	OLECHAR ch;
	for (int i = 0; i < cch; i++)
	{
		if ((ch = *prgchSrc++) == 10)
			m_iLine++;
		else if (ch > 0xFF)
			WriteError(kpszInvalidChar, ch, m_iLine, ch);
		*prgchDst++ = (char)ch;
	}
	DWORD dwT;
	WriteFile(m_hOutputFile, prgchBuffer, cch, &dwT, NULL);
	delete prgchBuffer;
	return true;
}

bool ConvertProcess::WriteStringToFile8(char * prgch, int cch)
{
	AssertArray(prgch, cch);
	if (!cch)
		return true;

	DWORD dwT;
	char * prgchBuffer = NewObj char[cch * 5];
	if (!prgchBuffer)
		return WriteError(kpszOutOfMemory);
	char * prgchSrc = prgch;
	char * prgchDst = prgchBuffer;

	for (int i = 0; i < cch; i++, prgchSrc++)
	{
		switch (*prgchSrc)
		{
		case '<':
			strcpy(prgchDst, "&lt;");
			prgchDst += 4;
			break;

		case '&':
			strcpy(prgchDst, "&amp;");
			prgchDst += 5;
			break;

		case '>':
			strcpy(prgchDst, "&gt;");
			prgchDst += 4;
			break;

		default:
			*prgchDst++ = *prgchSrc;
		}
	}
	WriteFile(m_hOutputFile, prgchBuffer, prgchDst - prgchBuffer, &dwT, NULL);
	delete prgchBuffer;
	return true;
}

bool ConvertProcess::WriteStringToFile16(OLECHAR * prgch, int cch)
{
	AssertArray(prgch, cch);
	if (!cch)
		return true;

	DWORD dwT;
	OLECHAR * prgchBuffer = NewObj OLECHAR[cch * 5];
	if (!prgchBuffer)
		return WriteError(kpszOutOfMemory);
	OLECHAR * prgchSrc = prgch;
	OLECHAR * prgchDst = prgchBuffer;

	for (int i = 0; i < cch; i++, prgchSrc++)
	{
		switch (*prgchSrc)
		{
			case '<':
				wcscpy(prgchDst, L"&lt;");
				prgchDst += 4;
				break;

			case '&':
				wcscpy(prgchDst, L"&amp;");
				prgchDst += 5;
				break;

			case '>':
				wcscpy(prgchDst, L"&gt;");
				prgchDst += 4;
				break;

			default:
				*prgchDst++ = *prgchSrc;
		}
	}
	WriteFile(m_hOutputFile, prgchBuffer, (prgchDst - prgchBuffer) << 1, &dwT, NULL);
	delete prgchBuffer;
	return true;
}

bool ConvertProcess::WriteANSIToUTF8File(char * prgch, int cch)
{
	AssertArray(prgch, cch);
	if (!cch)
		return true;

	char * prgchSrc = prgch;
	char * prgchLim = prgchSrc + cch;
	char * prgchBuffer = NewObj char[cch << 1];
	if (!prgchBuffer)
		return WriteError(kpszOutOfMemory);

	char * prgchDst = prgchBuffer;
	DWORD dwT;
	char ch;

	while (prgchSrc < prgchLim)
	{
		ch = (char)*prgchSrc++;
		if (ch < 0x80)
		{
			if (ch == 10)
				m_iLine++;
			*prgchDst++ = ch;
		}
		else
		{
			*prgchDst++ = (char)((ch >> 6) | firstByteMark[2]);
			*prgchDst++ = (char)((ch | 0x80) & 0xBF);
		}
	}
	WriteFile(m_hOutputFile, prgchBuffer, prgchDst - prgchBuffer, &dwT, NULL);
	delete prgchBuffer;
	return true;
}

bool ConvertProcess::WriteUTF16ToUTF8File(OLECHAR * prgch, int cch)
{
	AssertArray(prgch, cch);
	if (!cch)
		return true;

	OLECHAR * prgchSrc = prgch;
	OLECHAR * prgchLim = prgchSrc + cch;
	char * prgchBuffer = NewObj char[cch * 6];
	if (!prgchBuffer)
		return WriteError(kpszOutOfMemory);

	char * prgchDst = prgchBuffer;
	DWORD dwT;
	ULONG ch;
	ULONG ch2;
	unsigned short bytesToWrite = 0;
	const ULONG byteMask = 0xBF;
	const ULONG byteMark = 0x80;

	while (prgchSrc < prgchLim)
	{
		ch = *prgchSrc++;
		if (ch >= kSurrogateHighStart && ch <= kSurrogateHighEnd && prgchSrc < prgchLim)
		{
			ch2 = *prgchSrc;
			if (ch2 >= kSurrogateLowStart && ch2 <= kSurrogateLowEnd)
			{
				ch = ((ch - kSurrogateHighStart) << halfShift) +
					 (ch2 - kSurrogateLowStart) + halfBase;
				++prgchSrc;
			}
		}
		if (ch < 0x80)
		{
			switch (ch)
			{
				case '<':
					strcpy(prgchDst, "&lt;");
					prgchDst += 4;
					break;

				case '&':
					strcpy(prgchDst, "&amp;");
					prgchDst += 5;
					break;

				case '>':
					strcpy(prgchDst, "&gt;");
					prgchDst += 4;
					break;

				default:
					*prgchDst++ = (char)ch;
			}
		}
		else
		{
			if (ch < 0x800)
				bytesToWrite = 2;
			else if (ch < 0x10000)
				bytesToWrite = 3;
			else if (ch < 0x200000)
				bytesToWrite = 4;
			else if (ch < 0x4000000)
				bytesToWrite = 5;
			else if (ch <= kMaximumUCS4)
				bytesToWrite = 6;
			else
			{
				bytesToWrite = 2;
				ch = kReplacementCharacter;
			}

			prgchDst += bytesToWrite;
			switch (bytesToWrite)
			{
				case 6: *--prgchDst = (BYTE) ((ch | byteMark) & byteMask); ch >>= 6;
				case 5: *--prgchDst = (BYTE) ((ch | byteMark) & byteMask); ch >>= 6;
				case 4: *--prgchDst = (BYTE) ((ch | byteMark) & byteMask); ch >>= 6;
				case 3: *--prgchDst = (BYTE) ((ch | byteMark) & byteMask); ch >>= 6;
				case 2: *--prgchDst = (BYTE) ((ch | byteMark) & byteMask); ch >>= 6;
				case 1: *--prgchDst = (BYTE) (ch | firstByteMark[bytesToWrite]);
			}
			prgchDst += bytesToWrite;
		}
	}
	WriteFile(m_hOutputFile, prgchBuffer, prgchDst - prgchBuffer, &dwT, NULL);
	delete prgchBuffer;
	return true;
}

bool ConvertProcess::WriteUTF8ToANSIFile(char * prgch, int cch)
{
	AssertArray(prgch, cch);
	if (!cch)
		return true;

	char * prgchSrc = prgch;
	char * prgchBuffer = NewObj char[cch];
	if (!prgchBuffer)
		return WriteError(kpszOutOfMemory);

	char * prgchDst = prgchBuffer;
	char * prgchLim = prgchSrc + cch;
	ULONG ch;
	unsigned short extraBytesToWrite;

	while (prgchSrc < prgchLim)
	{
		ch = 0;
		extraBytesToWrite = bytesFromUTF8[(BYTE)*prgchSrc];
		switch (extraBytesToWrite)
		{
			case 5: ch += *prgchSrc++; ch <<= 6;
			case 4: ch += *prgchSrc++; ch <<= 6;
			case 3: ch += *prgchSrc++; ch <<= 6;
			case 2: ch += *prgchSrc++; ch <<= 6;
			case 1: ch += *prgchSrc++; ch <<= 6;
			case 0: ch += *prgchSrc++;
		}
		ch -= offsetsFromUTF8[extraBytesToWrite];

		if (ch == 10)
			m_iLine++;
		else if (ch > 0xFF)
			WriteError(kpszInvalidChar, ch, m_iLine, ch);
		*prgchDst++ = (BYTE)ch;
	}
	DWORD dwT;
	WriteFile(m_hOutputFile, prgchBuffer, prgchDst - prgchBuffer, &dwT, NULL);
	delete prgchBuffer;
	return true;
}