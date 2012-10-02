/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: ConvertProcess.h
Responsibility: Darrell Zook
Last reviewed:

	Header file for ConvertProcess.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef _CONVERTPROCESS_H_
#define _CONVERTPROCESS_H_


typedef enum {
	ketTableLookup
} EncodingType;

typedef struct {
	void * pxxxCode;
	char szFileName[MAX_PATH];
	EncodingType et;
	IUnknownPtr qunk;
} EncodingInfo;

typedef enum {
	kctUTF8toANSI = 0,
	kctUTF16toANSI,
	kctANSItoUTF8,
	kctANSItoUTF16,
} ConversionType;


class ConvertProcess
{
public:
	ConvertProcess(HWND hwndParent, HINSTANCE hInstance);
	~ConvertProcess();
	bool Process(char szFiles[NUM_COMBOS][MAX_PATH], bool fSilent);
	void * GetFileStart()
		{ return m_pFileStart; }
	HANDLE GetLogFile()
		{ return m_hLogFile; }
	DWORD GetLine()
		{ return m_iLine; }
	bool LogError()
		{ return (++m_cErrors != ERROR_LIMIT); }
	const void * GetTopEncoding();

protected:
	bool WriteError(const char * pszError, int nParam1 = -1, int nParam2 = -1, int nParam3 = -1);
	bool ParseControlFile();
	bool ParseControlLine(bool * pfFoundConvert, char * prgchPos, int nLine,
		ICsCallbackAToW * pccaw, ICsCallbackWToA * pccwa);
	bool ParseInputFile(IUnknown * punk, HWND hwndProgress);
	bool ParseTsString8(char ** ppPos, char * prgchFileLim, IUnknown * punk);
	bool ParseTsString16(OLECHAR ** ppPos, OLECHAR * prgchFileLim, IUnknown * punk);
	bool ConvertText8(char * prgch, int cch, IUnknown * punk);
	bool ConvertText16(OLECHAR * prgch, int cch, IUnknown * punk);
	bool Write8ToFile(char * prgch, int cch);
	bool Write16ToFile(OLECHAR * prgch, int cch);
	bool WriteStringToFile8(char * prgch, int cch);
	bool WriteStringToFile16(OLECHAR * prgch, int cch);
	bool WriteANSIToUTF8File(char * prgch, int cch);
	bool WriteUTF16ToUTF8File(OLECHAR * prgch, int cch);
	bool WriteUTF8ToANSIFile(char * prgch, int cch);

	HWND m_hwndParent;
	HINSTANCE m_hInstance;
	HANDLE m_hControlFile;
	HANDLE m_hInputFile;
	HANDLE m_hOutputFile;
	HANDLE m_hLogFile;
	EncodingInfo ** m_prgei;
	int m_cei;
	ConversionType m_ct;
	int m_nDefaultEncOffset;
	int m_nCurrentEncOffset;
	byte m_rgchOutput[BUFFER_SIZE];
	OLECHAR m_rgwchOutput[BUFFER_SIZE];
	UINT m_iLine;
	HANDLE m_hFileMap;
	void * m_pFileStart;
	char m_szOutputFile[MAX_PATH];
	int m_cErrors;
};

#endif // !_CONVERTPROCESS_H_