/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: ConvertString.cpp
Responsibility: Darrell Zook
Last reviewed:

	Implementation file for ConvertString.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


const int kcchChunk = 4096; // 4 Kb

/***********************************************************************************************
	Global factory instances.
***********************************************************************************************/

GenericFactory g_factCsConvertAToW(
	"SIL.CS.ConvertAToW",
	&CLSID_CsConvertAToW,
	"ConvertAToW (8-bit to 16-bit)",
	"Apartment",
	&CsConvertAToW::CreateCom);

GenericFactory g_factCsConvertWToA(
	"SIL.CS.ConvertWToA",
	&CLSID_CsConvertWToA,
	"ConvertWToA (16-bit to 8-bit)",
	"Apartment",
	&CsConvertWToA::CreateCom);

GenericFactory g_factCsConvertWToW(
	"SIL.CS.ConvertWToW",
	&CLSID_CsConvertWToW,
	"ConvertWToW (16-bit to 16-bit)",
	"Apartment",
	&CsConvertWToW::CreateCom);


/***********************************************************************************************
	Global utility functions.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	HexToString converts a string of hex numbers (pchSrc) to a string containing the hex
	characters represented by the numbers in pchSrc. pprgchDst will point to the newly
	allocated string that the caller is responsible to free.
----------------------------------------------------------------------------------------------*/
InitErrorCode HexToString(OLECHAR * prgchSrc, OLECHAR * prgchStop, OLECHAR ** pprgchDst,
	int * pcchDst)
{
	AssertPtr(prgchSrc);
	AssertPtr(prgchStop);
	AssertPtr(pprgchDst);
	AssertPtr(pcchDst);

	*pprgchDst = NULL;
	*pcchDst = 0;

	if (prgchSrc > prgchStop)
		return kiecNoReplacement;

	ULONG ch = 0;
	int cchDst = 0;
	OLECHAR * prgchNumEnd;
	OLECHAR * prgchOldNumEnd = NULL;
	try
	{
		*pprgchDst = NewObj OLECHAR[prgchStop - prgchSrc + 1];
	}
	catch (...)
	{
		return kiecOutOfMemory;
	}
	OLECHAR * prgchDst = *pprgchDst;

	if (prgchSrc == prgchStop)
	{
		*prgchDst = 0;
		return kiecNoError;
	}

	InitErrorCode iec = kiecNoError;
	ch = (ULONG)wcstol(prgchSrc, &prgchNumEnd, 16);
	while (prgchNumEnd <= prgchStop && prgchNumEnd != prgchOldNumEnd)
	{
		if (*prgchNumEnd && !iswspace(*prgchNumEnd))
		{
			iec = kiecIllegalRule;
			break;
		}
		cchDst++;
		if (ch > 0xFFFF)
		{
			if (ch > 0x10FFFF)
			{
				iec = kiecIllegalRule;
				break;
			}
			cchDst++;
			ch -= 0x10000;
			*prgchDst++ = (OLECHAR)((ch >> 10) + 0xD800);
			*prgchDst++ = (OLECHAR)(ch % 0x400 + 0xDC00);
		}
		else
		{
			*prgchDst++ = (OLECHAR)ch;
		}
		prgchOldNumEnd = prgchNumEnd;
		ch = (OLECHAR)wcstol(prgchNumEnd, &prgchNumEnd, 16);
	}

	if (kiecNoError == iec)
	{
		*pcchDst = cchDst;
	}
	else
	{
		delete *pprgchDst;
		*pprgchDst = NULL;
		*pcchDst = 0;
	}
	return iec;
}

/*----------------------------------------------------------------------------------------------
	ParseLine takes one line from the initialization string and parses the rule and the
	replacement from the line. ppszRule and ppszReplace will be newly allocated strings that
	the caller is responsible to free.
----------------------------------------------------------------------------------------------*/
InitErrorCode ParseLine(OLECHAR * prgchLine, OLECHAR * prgchLineLim, OLECHAR ** ppszRule,
	OLECHAR ** ppszReplace)
{
	AssertPtr(prgchLine);
	AssertPtr(prgchLineLim);
	AssertPtr(ppszRule);
	AssertPtr(ppszReplace);

	*ppszRule = NULL;
	*ppszReplace = NULL;

	OLECHAR *& pszRule = *ppszRule;
	OLECHAR *& pszOutput = *ppszReplace;
	InitErrorCode iec = kiecNoError;

	OLECHAR * prgchLim = wcschr(prgchLine, '#');
	if (!prgchLim || prgchLim > prgchLineLim)
		prgchLim = prgchLineLim;
	OLECHAR * prgchWedge = wcschr(prgchLine, '>');
	if (prgchWedge && prgchWedge < prgchLineLim)
	{
		int cch;
		if (HexToString(prgchLine, prgchWedge, &pszRule, &cch) == kiecNoError)
		{
			if (cch)
			{
				if (HexToString(prgchWedge + 1, prgchLim, &pszOutput, &cch) == kiecNoError)
					return kiecNoError;
				else
					iec = kiecIllegalReplacement;
			}
			else
			{
				iec = kiecNoArgument;
			}
			delete pszRule;
			pszRule = NULL;
		}
		else
		{
			iec = kiecIllegalArgument;
		}
	}
	else
	{
		iec = kiecNoReplacement;
	}
	return iec;
}

/*----------------------------------------------------------------------------------------------
	Initialize creates a trie from either a string or a file. if fFilename is true, bstrTable
	should contain the name of a file. Otherwise, bstrTable should contain the table text. If
	there are any errors and punk is not NULL, the InitError method will be called using the
	interface specified by ct. If there are any errors and punk is NULL, an error code will
	be returned from this function.
----------------------------------------------------------------------------------------------*/
HRESULT Initialize(BSTR bstrTable, bool fFilename, TrieLevel * ptl, CallbackType ct,
	IUnknown * punk)
{
	AssertBstr(bstrTable);
	AssertPtr(ptl);
	AssertPtrN(punk);

	OLECHAR * prgchBuffer = bstrTable;
	BOOL fContinue = true;
	HRESULT hr;
	IUnknownPtr qunk;
	if (punk)
	{
		if (kctAToW == ct)
			hr = punk->QueryInterface(IID_ICsCallbackAToW, (void **)&qunk);
		else if (kctWToA == ct)
			hr = punk->QueryInterface(IID_ICsCallbackWToA, (void **)&qunk);
		else
			hr = punk->QueryInterface(IID_ICsCallbackWToW, (void **)&qunk);
		if (FAILED(hr))
			return WarnHr(hr);
	}

	if (fFilename)
	{
		InitErrorCode iec = kiecNoError;
		StrAnsi staFile = bstrTable;
		HANDLE hFile = CreateFile(staFile.Chars(), GENERIC_READ, FILE_SHARE_READ, NULL,
			OPEN_EXISTING, 0, NULL);
		if (hFile != INVALID_HANDLE_VALUE)
		{
			DWORD cb = GetFileSize(hFile, NULL);
			if (cb % 2)
			{
				CloseHandle(hFile);
				return WarnHr(E_UNEXPECTED);
			}
			try
			{
				prgchBuffer = NewObj OLECHAR[(cb >> 1) + 1];
			}
			catch (...)
			{
				CloseHandle(hFile);
				return WarnHr(E_OUTOFMEMORY);
			}
			prgchBuffer[cb >> 1] = 0;
			DWORD dwRead = 0;
			if (!ReadFile(hFile, prgchBuffer, cb, &dwRead, NULL) || dwRead != cb)
				iec = kiecFileReadError;
			CloseHandle(hFile);
		}
		else
		{
			iec = kiecFileNotFound;
		}
		if (iec != kiecNoError)
		{
			if (punk)
			{
				if (kctAToW == ct)
					((ICsCallbackAToW *)qunk.Ptr())->InitError(iec, -1, NULL, &fContinue);
				else if (kctWToA == ct)
					((ICsCallbackWToA *)qunk.Ptr())->InitError(iec, -1, NULL, &fContinue);
				else
					((ICsCallbackWToW *)qunk.Ptr())->InitError(iec, -1, NULL, &fContinue);
			}
			return WarnHr(E_FAIL);
		}
	}
	else
	{
		prgchBuffer = bstrTable;
	}

	int iLine = 0;
	InitErrorCode iec = kiecNoError;
	OLECHAR * prgch = prgchBuffer;
	OLECHAR * prgchEOL = NULL;
	OLECHAR * prgchRule = NULL;
	OLECHAR * prgchReplace;

	if (fFilename && 0xFEFF == prgch[0])
	{
		// Move past the 0xFEFF at the beginning of the file if it exists.
		prgch++;
	}

	try
	{
		while (fContinue && prgch && *prgch)
		{
			iLine++;
			prgchEOL = prgch + wcscspn(prgch, L"\r\n");

			if (*prgch != '#' && *prgch != 10 && *prgch != 13)
			{
				iec = ParseLine(prgch, prgchEOL, &prgchRule, &prgchReplace);
				if (kiecNoError == iec)
				{
					if (kctWToA != ct)
					{
						hr = ptl->AddKey(prgchRule, prgchReplace);
					}
					else
					{
						int cch = wcslen(prgchReplace);
						char * prgchNewReplace = NewObj char[cch + 1];
						prgchNewReplace[cch] = '\0';
						while (--cch >= 0)
							prgchNewReplace[cch] = (char)prgchReplace[cch];
						hr = ptl->AddKey(prgchRule, prgchNewReplace);
						// Delete the 16-bit string.
						delete prgchReplace;
						prgchReplace = NULL;
					}
					if (E_UNEXPECTED == hr)
					{
						iec = kiecDuplicateArgument;
						if (prgchReplace)
						{
							delete prgchReplace;
							prgchReplace = NULL;
						}
					}
				}
				else
				{
					StrUni stu;
					stu.Assign(prgch, prgchEOL - prgch);
					if (punk)
					{
						if (kctAToW == ct)
						{
							((ICsCallbackAToW *)qunk.Ptr())->InitError(iec, iLine, stu.Bstr(),
								&fContinue);
						}
						else if (kctWToA == ct)
						{
							((ICsCallbackWToA *)qunk.Ptr())->InitError(iec, iLine, stu.Bstr(),
								&fContinue);
						}
						else
						{
							((ICsCallbackWToW *)qunk.Ptr())->InitError(iec, iLine, stu.Bstr(),
								&fContinue);
						}
					}
					else
					{
						if (prgchRule)
						{
							delete prgchRule;
							prgchRule = NULL;
						}
						hr = WarnHr(E_FAIL);
						break;
					}
				}
				if (prgchRule)
				{
					delete prgchRule;
					prgchRule = NULL;
				}
			}
			if (!*prgchEOL)
				break;
			prgch = prgchEOL + 1;
		}
	}
	catch (...)
	{
		if (prgchRule)
		{
			delete prgchRule;
			prgchRule = NULL;
		}
	}
	if (fFilename)
		delete prgchBuffer;
	return hr;
}


/***********************************************************************************************
	CsConvertAToW methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CsConvertAToW::CsConvertAToW()
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CsConvertAToW::~CsConvertAToW()
{
	if (m_ptl)
		delete m_ptl;
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Static method to create a CsConvertAToW.
----------------------------------------------------------------------------------------------*/
void CsConvertAToW::CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<CsConvertAToW> qcsaw;
	qcsaw.Attach(NewObj CsConvertAToW());
	CheckHr(qcsaw->QueryInterface(riid, ppv));
}

/*----------------------------------------------------------------------------------------------
	AddRef.
----------------------------------------------------------------------------------------------*/
ULONG CsConvertAToW::AddRef(void)
{
	return InterlockedIncrement(&m_cref);
}

/*----------------------------------------------------------------------------------------------
	Release.
----------------------------------------------------------------------------------------------*/
ULONG CsConvertAToW::Release(void)
{
	long lw = InterlockedDecrement(&m_cref);
	if (0 == lw)
	{
		m_cref = 1;
		delete this;
	}
	return lw;
}

/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertAToW::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtrN(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_ICsConvertAToW)
		*ppv = static_cast<ICsConvertAToW *>(this);
	else
		return E_NOINTERFACE;
	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	This must be called before any of the other methods can be called.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertAToW::Initialize(BSTR bstrTable, BOOL fFilename,
	ICsCallbackAToW * pccaw)
{
	AssertBstrN(bstrTable);
	AssertPtrN(pccaw);
	if (!bstrTable)
		return WarnHr(E_POINTER);

	if (m_ptl)
		delete m_ptl;
	try
	{
		m_ptl = NewObj TrieLevel;
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
	return ::Initialize(bstrTable, fFilename, m_ptl, kctAToW, pccaw);
}

/*----------------------------------------------------------------------------------------------
	Convert takes an array of characters and returns the converted output (using the trie
	built in the initialization step) in a newly allocated BSTR that the caller is
	responsible to free.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertAToW::Convert(byte * prgchSrc, int cchSrc, BSTR * pbstrDst)
{
	AssertArray(prgchSrc, cchSrc);
	AssertPtrN(pbstrDst);
	if (!pbstrDst)
		return WarnHr(E_POINTER);
	Assert(!*pbstrDst);
	*pbstrDst = NULL;
	if (!prgchSrc)
		return WarnHr(E_POINTER);
	if (!m_ptl)
		return WarnHr(E_UNEXPECTED);

	if (!cchSrc)
		return S_OK;

	int cchNeed;
	// Allocate extra space for the BSTR size count and the NULL at the end.
	OLECHAR * prgchDst = (OLECHAR *)CoTaskMemAlloc((kcchChunk + 3) * sizeof(OLECHAR));
	HRESULT hr = ConvertCore(prgchSrc, cchSrc, &prgchDst, kcchChunk, &cchNeed,
		kctConvert, NULL);
	if (SUCCEEDED(hr))
	{
		*((int *)prgchDst) = cchNeed << 1;
		prgchDst[cchNeed + 2] = 0;
		OLECHAR * prgchT = (OLECHAR *)CoTaskMemRealloc(prgchDst,
			(cchNeed + 3) * sizeof(OLECHAR));
		if (!prgchT)
			hr = WarnHr(E_OUTOFMEMORY);
		*pbstrDst = (BSTR)(prgchT + 2);
	}
	else
	{
		delete prgchDst;
	}
	return hr;
}

/*----------------------------------------------------------------------------------------------
	ConvertRgch takes an array of characters and stores the output in the buffer specified by
	prgchDst. If there is not enough room in the buffer to store all of the converted text,
	this function will return an error code and the values in prgchDst and pcchNeed should be
	ignored. If a character does not have a corresponding rule in the table, the ProcessError
	method is called on pccaw. If pccaw is NULL, the missing rule is ignored and the character
	is placed in prgchDst without any conversion.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertAToW::ConvertRgch(byte * prgchSrc, int cchSrc, OLECHAR * prgchDst,
	int cchDst, ICsCallbackAToW * pccaw, int * pcchNeed)
{
	AssertArray(prgchSrc, cchSrc);
	AssertArray(prgchDst, cchDst);
	AssertPtrN(pccaw);
	AssertPtrN(pcchNeed);
	if (!prgchSrc || (!prgchDst && cchDst) || (!prgchDst && !cchDst && !pcchNeed))
		return WarnHr(E_POINTER);
	if (!m_ptl)
		return WarnHr(E_UNEXPECTED);

	if (!cchSrc)
		return S_OK;
	return ConvertCore(prgchSrc, cchSrc, &prgchDst, cchDst, pcchNeed, kctConvertArray, pccaw);
}

/*----------------------------------------------------------------------------------------------
	ConvertCallback takes an array of characters and calls the HaveText method on pccaw
	every time a maximum of cchChunk characters is converted. cchChunk refers to the length
	of the converted text, not the length of the source text.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertAToW::ConvertCallback(byte * prgchSrc, int cchSrc, int cchChunk,
	ICsCallbackAToW * pccaw)
{
	AssertArray(prgchSrc, cchSrc);
	AssertPtrN(pccaw);
	if (!pccaw || !prgchSrc)
		return WarnHr(E_POINTER);
	if (!m_ptl)
		return WarnHr(E_UNEXPECTED);

	if (!cchSrc)
		return S_OK;
	OLECHAR * prgchDst = (OLECHAR *)CoTaskMemAlloc(cchChunk << 1);
	HRESULT hr = ConvertCore(prgchSrc, cchSrc, &prgchDst, cchChunk, NULL, kctConvertCB, pccaw);
	CoTaskMemFree(prgchDst);
	return hr;
}

/*----------------------------------------------------------------------------------------------
	ConvertCore gets called by the other three Convert methods; ct specifies which one called
	it.
----------------------------------------------------------------------------------------------*/
HRESULT CsConvertAToW::ConvertCore(byte * prgchSrc, int cchSrc, OLECHAR ** pprgchDst,
	int cchDst, int * pcchNeed, ConvertType ct, ICsCallbackAToW * pccaw)
{
	AssertArray(prgchSrc, cchSrc);
	AssertPtr(pprgchDst);
	AssertArray(*pprgchDst, cchDst);
	AssertPtrN(pcchNeed);
	AssertPtrN(pccaw);
	Assert(pccaw || ct != kctConvertCB);

	OLECHAR * prgchDst = *pprgchDst;
	OLECHAR * prgchBuffer = NULL;
	int cchSrcProcessed = 0;
	int cchDstProcessed = 0;

	if (kctConvert == ct)
		prgchDst += 2; // This skips past the size count for the BSTR.
	if (kctConvertArray == ct && 0 == cchDst)
	{
		AssertPtr(pcchNeed);
		*pcchNeed = 0;
		cchDst = kcchChunk;
		try
		{
			prgchBuffer = NewObj OLECHAR[kcchChunk];
		}
		catch (...)
		{
			return WarnHr(E_OUTOFMEMORY);
		}
		prgchDst = prgchBuffer;
	}

	HRESULT hr = S_OK;
	while (cchSrcProcessed < cchSrc)
	{
		int cchSrcT = cchSrc - cchSrcProcessed;
		int cchDstT = cchDst;
		if (!ConvertChunk(prgchSrc, &cchSrcT, prgchDst, &cchDstT, cchSrcProcessed, pccaw))
		{
			// The process was cancelled because of a missing rule.
			hr = S_FALSE;
			break;
		}
		cchSrcProcessed += cchSrcT;
		prgchSrc += cchSrcT;
		cchDstProcessed += cchDstT;
		if (kctConvert == ct) // from Convert
		{
			if (cchSrcProcessed < cchSrc)
			{
				// Double the size of memory for the destination buffer.
				// Add space for the BSTR size count and the NULL at the end.
				int cchDstNew = (cchDstProcessed << 1) + 3;
				int cchOffset = prgchDst - *pprgchDst + cchDstT;
				*pprgchDst = (OLECHAR *)CoTaskMemRealloc(*pprgchDst,
					cchDstNew * sizeof(OLECHAR));
				if (!*pprgchDst)
				{
					hr = WarnHr(E_OUTOFMEMORY);
					break;
				}
				prgchDst = *pprgchDst + cchOffset;
				// Subtract space for the BSTR size count and the NULL at the end.
				cchDst = cchDstNew - cchDstProcessed - 3;
			}
		}
		else if (kctConvertCB == ct) // from ConvertChunk
		{
			pccaw->HaveText(*pprgchDst, cchDstT, cchSrcProcessed);
		}
		else // from ConvertRgch
		{
			Assert(kctConvertArray == ct);
			if (cchSrcProcessed != cchSrc && !prgchBuffer)
			{
				hr = WarnHr(E_FAIL);
				break;
			}
		}
	}

	if (prgchBuffer)
		delete prgchBuffer;
	if (pcchNeed)
		*pcchNeed = cchDstProcessed;
	return hr;
}

/*----------------------------------------------------------------------------------------------
	pcchSrc -> This contains the number of characters to process from prgchSrc. On exit, this
		contains the number of characters that were actually processed from prgchSrc.
	pcchDst -> This contains the size of prgchSDst. On exit, this contains the number of
		characters that were actually placed in prgchDst.

	This method returns false if a rule is not found and fContinue is set to false in the
		ProcessError callback method.
----------------------------------------------------------------------------------------------*/
bool CsConvertAToW::ConvertChunk(byte * prgchSrc, int * pcchSrc, OLECHAR * prgchDst,
	int * pcchDst, int cchSrcProcessed, ICsCallbackAToW * pccaw)
{
	AssertPtr(pcchSrc);
	AssertPtr(pcchDst);
	AssertArray(prgchSrc, *pcchSrc);
	AssertArray(prgchDst, *pcchDst);
	AssertPtrN(pccaw);

	void * pv = NULL;
	int cchInput = *pcchSrc;
	int cchOutput = *pcchDst;
	int cchKey;
	int cch;
	int cchSrc = 0;
	int cchDst = 0;
	BOOL fContinue = true;

	// If there is not enough room in the destination buffer, return false.
	while (cchSrc < cchInput && cchDst < cchOutput)
	{
		cchKey = 0;
		if (S_OK == m_ptl->FindKey((char *)prgchSrc, &cchKey, &pv))
		{
			cch = wcslen((OLECHAR *)pv);
			if (cchDst + cch <= cchOutput)
			{
				memmove(prgchDst, pv, cch << 1);
				prgchDst += cch;
				cchDst += cch;
			}
			else
			{
				// Not enough room in output buffer.
				break;
			}
		}
		else
		{
			cchKey = 1;
			if (pccaw)
				pccaw->ProcessError(cchSrcProcessed + cchSrc, &fContinue);
			if (!fContinue)
				break;
			if (cchDst + 1 <= cchOutput)
			{
				*prgchDst++ = *prgchSrc;
				cchDst++;
			}
			else
			{
				// Not enough room in output buffer.
				break;
			}
		}
		prgchSrc += cchKey;
		cchSrc += cchKey;
	}
	*pcchSrc = cchSrc;
	*pcchDst = cchDst;
	return fContinue;
}


/***********************************************************************************************
	CsConvertWToA methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CsConvertWToA::CsConvertWToA()
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CsConvertWToA::~CsConvertWToA()
{
	if (m_ptl)
		delete m_ptl;
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Static method to create a CsConvertWToA.
----------------------------------------------------------------------------------------------*/
void CsConvertWToA::CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<CsConvertWToA> qcswa;
	qcswa.Attach(NewObj CsConvertWToA());
	CheckHr(qcswa->QueryInterface(riid, ppv));
}

/*----------------------------------------------------------------------------------------------
	AddRef.
----------------------------------------------------------------------------------------------*/
ULONG CsConvertWToA::AddRef(void)
{
	return InterlockedIncrement(&m_cref);
}

/*----------------------------------------------------------------------------------------------
	Release.
----------------------------------------------------------------------------------------------*/
ULONG CsConvertWToA::Release(void)
{
	long lw = InterlockedDecrement(&m_cref);
	if (lw == 0)
	{
		m_cref = 1;
		delete this;
	}
	return lw;
}

/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertWToA::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtrN(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_ICsConvertWToA)
		*ppv = static_cast<ICsConvertWToA *>(this);
	else
		return E_NOINTERFACE;
	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	This must be called before any of the other methods can be called.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertWToA::Initialize(BSTR bstrTable, BOOL fFilename,
	ICsCallbackWToA * pccwa)
{
	AssertBstrN(bstrTable);
	AssertPtrN(pccwa);
	if (!bstrTable)
		return WarnHr(E_POINTER);

	if (m_ptl)
		delete m_ptl;
	try
	{
		m_ptl = NewObj TrieLevel;
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
	return ::Initialize(bstrTable, fFilename, m_ptl, kctWToA, pccwa);
}

/*----------------------------------------------------------------------------------------------
	Convert takes a BSTR and returns the converted output (using the trie built in the
	initialization step) in a newly allocated array of characters that the caller is
	responsible to free.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertWToA::Convert(BSTR bstrSrc, byte ** pprgchDst, int * pcchNeed)
{
	AssertBstrN(bstrSrc);
	AssertPtrN(pprgchDst);
	if (!pprgchDst)
		return WarnHr(E_POINTER);
	*pprgchDst = NULL;
	if (!m_ptl)
		return WarnHr(E_UNEXPECTED);
	if (BstrLen(bstrSrc) == 0)
		return S_OK;

	*pprgchDst = (byte *)CoTaskMemAlloc(kcchChunk);
	if (!*pprgchDst)
		return WarnHr(E_OUTOFMEMORY);
	return ConvertCore(bstrSrc, BstrLen(bstrSrc), pprgchDst, kcchChunk, pcchNeed,
		kctConvert, NULL);
}

/*----------------------------------------------------------------------------------------------
	ConvertRgch takes an array of characters and stores the output in the buffer specified by
	prgchDst. If there is not enough room in the buffer to store all of the converted text,
	this function will return an error code and the values in prgchDst and pcchNeed should be
	ignored. If a character does not have a corresponding rule in the table, the ProcessError
	method is called on pccwa. If pccwa is NULL, the missing rule is ignored and the character
	is placed in prgchDst without any conversion.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertWToA::ConvertRgch(OLECHAR * prgchSrc, int cchSrc, byte * prgchDst,
	int cchDst, ICsCallbackWToA * pccwa, int * pcchNeed)
{
	AssertArray(prgchSrc, cchSrc);
	AssertArray(prgchDst, cchDst);
	AssertPtrN(pccwa);
	AssertPtrN(pcchNeed);
	if (!prgchSrc || (!prgchDst && cchDst) || (!prgchDst && !cchDst && !pcchNeed))
		return WarnHr(E_POINTER);
	if (!m_ptl)
		return WarnHr(E_UNEXPECTED);

	if (!cchSrc)
		return S_OK;
	return ConvertCore(prgchSrc, cchSrc, &prgchDst, cchDst, pcchNeed, kctConvertArray, pccwa);
}

/*----------------------------------------------------------------------------------------------
	ConvertCallback takes an array of characters and calls the HaveText method on pccwa
	every time a maximum of cchChunk characters is converted. cchChunk refers to the length
	of the converted text, not the length of the source text.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertWToA::ConvertCallback(OLECHAR * prgchSrc, int cchSrc, int cchChunk,
	ICsCallbackWToA * pccwa)
{
	AssertArray(prgchSrc, cchSrc);
	AssertPtrN(pccwa);
	if (!pccwa || !prgchSrc)
		return WarnHr(E_POINTER);
	if (!m_ptl)
		return WarnHr(E_UNEXPECTED);

	if (!cchSrc)
		return S_OK;
	byte * prgchDst = (byte *)CoTaskMemAlloc(cchChunk);
	HRESULT hr = ConvertCore(prgchSrc, cchSrc, &prgchDst, cchChunk, NULL, kctConvertCB, pccwa);
	CoTaskMemFree(prgchDst);
	return hr;
}

/*----------------------------------------------------------------------------------------------
	ConvertCore gets called by the other three Convert methods; ct specifies which one called
	it.
----------------------------------------------------------------------------------------------*/
HRESULT CsConvertWToA::ConvertCore(OLECHAR * prgchSrc, int cchSrc, byte ** pprgchDst,
	int cchDst, int * pcchNeed, ConvertType ct, ICsCallbackWToA * pccwa)
{
	AssertArray(prgchSrc, cchSrc);
	AssertPtr(pprgchDst);
	AssertArray(*pprgchDst, cchDst);
	AssertPtrN(pcchNeed);
	AssertPtrN(pccwa);
	Assert(pccwa || ct != kctConvertCB);

	byte * prgchDst = *pprgchDst;
	byte * prgchBuffer = NULL;
	int cchSrcProcessed = 0;
	int cchDstProcessed = 0;

	if (kctConvertArray == ct && 0 == cchDst)
	{
		AssertPtr(pcchNeed);
		*pcchNeed = 0;
		cchDst = kcchChunk;
		try
		{
			prgchBuffer = NewObj byte[kcchChunk];
		}
		catch (...)
		{
			return WarnHr(E_OUTOFMEMORY);
		}
		prgchDst = prgchBuffer;
	}

	HRESULT hr = S_OK;
	while (cchSrcProcessed < cchSrc)
	{
		int cchSrcT = cchSrc - cchSrcProcessed;
		int cchDstT = cchDst;
		if (!ConvertChunk(prgchSrc, &cchSrcT, prgchDst, &cchDstT, cchSrcProcessed, pccwa))
		{
			// The process was cancelled because of a missing rule.
			hr = S_FALSE;
			break;
		}
		cchSrcProcessed += cchSrcT;
		prgchSrc += cchSrcT;
		cchDstProcessed += cchDstT;
		if (kctConvert == ct) // from Convert
		{
			if (cchSrcProcessed < cchSrc)
			{
				// Double the size of memory for the destination buffer.
				int cchDstNew = cchDstProcessed << 1;
				int cchOffset = prgchDst - *pprgchDst + cchDstT;
				*pprgchDst = (byte *)CoTaskMemRealloc(*pprgchDst, cchDstNew);
				if (!pprgchDst)
				{
					hr = WarnHr(E_OUTOFMEMORY);
					break;
				}
				prgchDst = *pprgchDst + cchOffset;
				cchDst = cchDstNew - cchDstProcessed;
			}
		}
		else if (kctConvertCB == ct) // from ConvertChunk
		{
			pccwa->HaveText(*pprgchDst, cchDstT, cchSrcProcessed);
		}
		else // from ConvertRgch
		{
			Assert(kctConvertArray == ct);
			if (cchSrcProcessed != cchSrc && !prgchBuffer)
			{
				hr = WarnHr(E_FAIL);
				break;
			}
		}
	}

	if (prgchBuffer)
		delete prgchBuffer;
	if (pcchNeed)
		*pcchNeed = cchDstProcessed;
	return hr;
}

/*----------------------------------------------------------------------------------------------
	pcchSrc -> This contains the number of characters to process from prgchSrc. On exit, this
		contains the number of characters that were actually processed from prgchSrc.
	pcchDst -> This contains the size of prgchSDst. On exit, this contains the number of
		characters that were actually placed in prgchDst.

	This method returns false if a rule was not found and fContinue was set to false in the
		ProcessError callback method.
----------------------------------------------------------------------------------------------*/
bool CsConvertWToA::ConvertChunk(OLECHAR * prgchSrc, int * pcchSrc, byte * prgchDst,
	int * pcchDst, int cchSrcProcessed, ICsCallbackWToA * pccwa)
{
	AssertPtr(pcchSrc);
	AssertPtr(pcchDst);
	AssertArray(prgchSrc, *pcchSrc);
	AssertArray(prgchDst, *pcchDst);
	AssertPtrN(pccwa);

	void * pv = NULL;
	int cchInput = *pcchSrc;
	int cchOutput = *pcchDst;
	int cchKey;
	int cch;
	int cchSrc = 0;
	int cchDst = 0;
	BOOL fContinue = true;

	// If there is not enough room in the destination buffer, return false.
	while (cchSrc < cchInput && cchDst < cchOutput)
	{
		cchKey = 0;
		if (S_OK == m_ptl->FindKey(prgchSrc, &cchKey, &pv))
		{
			cch = strlen((char *)pv);
			if (cchDst + cch <= cchOutput)
			{
				memmove(prgchDst, pv, cch);
				prgchDst += cch;
				cchDst += cch;
			}
			else
			{
				// Not enough room in output buffer.
				break;
			}
		}
		else
		{
			cchKey = 1;
			if (pccwa)
				pccwa->ProcessError(cchSrcProcessed + cchSrc, &fContinue);
			if (!fContinue)
				break;
			if (cchDst + 1 <= cchOutput)
			{
				*prgchDst++ = (BYTE)*prgchSrc;
				cchDst++;
			}
			else
			{
				// Not enough room in output buffer.
				break;
			}
		}
		prgchSrc += cchKey;
		cchSrc += cchKey;
	}
	*pcchSrc = cchSrc;
	*pcchDst = cchDst;
	return fContinue;
}


/***********************************************************************************************
	CsConvertWToW methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CsConvertWToW::CsConvertWToW()
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CsConvertWToW::~CsConvertWToW()
{
	if (m_ptl)
		delete m_ptl;
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Static method to create a CsConvertWToW.
----------------------------------------------------------------------------------------------*/
void CsConvertWToW::CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<CsConvertWToW> qcsww;
	qcsww.Attach(NewObj CsConvertWToW());
	CheckHr(qcsww->QueryInterface(riid, ppv));
}

/*----------------------------------------------------------------------------------------------
	AddRef.
----------------------------------------------------------------------------------------------*/
ULONG CsConvertWToW::AddRef(void)
{
	return InterlockedIncrement(&m_cref);
}

/*----------------------------------------------------------------------------------------------
	Release.
----------------------------------------------------------------------------------------------*/
ULONG CsConvertWToW::Release(void)
{
	long lw = InterlockedDecrement(&m_cref);
	if (lw == 0)
	{
		m_cref = 1;
		delete this;
	}
	return lw;
}

/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertWToW::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtrN(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_ICsConvertWToW)
		*ppv = static_cast<ICsConvertWToW *>(this);
	else
		return E_NOINTERFACE;
	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	This must be called before any of the other methods can be called.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertWToW::Initialize(BSTR bstrTable, BOOL fFilename,
	ICsCallbackWToW * pccww)
{
	AssertBstrN(bstrTable);
	AssertPtrN(pccww);
	if (!bstrTable)
		return WarnHr(E_POINTER);

	if (m_ptl)
		delete m_ptl;
	try
	{
		m_ptl = NewObj TrieLevel;
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
	return ::Initialize(bstrTable, fFilename, m_ptl, kctWToW, pccww);
}

/*----------------------------------------------------------------------------------------------
	Convert takes a BSTR and returns the converted output (using the trie built in the
	initialization step) in a newly allocated BSTR that the caller is responsible to free.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertWToW::Convert(BSTR bstrSrc, BSTR * pbstrDst)
{
	AssertBstrN(bstrSrc);
	AssertPtrN(pbstrDst);
	if (!pbstrDst)
		return WarnHr(E_POINTER);
	Assert(!*pbstrDst);
	*pbstrDst = NULL;
	if (!m_ptl)
		return WarnHr(E_UNEXPECTED);

	if (BstrLen(bstrSrc) == 0)
		return S_OK;

	int cchNeed;
	// Allocate extra space for the BSTR size count and the NULL at the end.
	OLECHAR * prgchDst = (OLECHAR *)CoTaskMemAlloc((kcchChunk + 3) * sizeof(OLECHAR));
	HRESULT hr = ConvertCore(bstrSrc, BstrLen(bstrSrc), &prgchDst, kcchChunk, &cchNeed,
		kctConvert, NULL);
	if (SUCCEEDED(hr))
	{
		*((int *)prgchDst) = cchNeed << 1;
		prgchDst[cchNeed + 2] = 0;
		OLECHAR * prgchT = (OLECHAR *)CoTaskMemRealloc(prgchDst,
			(cchNeed + 3) * sizeof(OLECHAR));
		if (!prgchT)
			hr = WarnHr(E_OUTOFMEMORY);
		*pbstrDst = (BSTR)(prgchT + 2);
	}
	else
	{
		delete prgchDst;
	}
	return hr;
}

/*----------------------------------------------------------------------------------------------
	ConvertRgch takes an array of characters and stores the output in the buffer specified by
	prgchDst. If there is not enough room in the buffer to store all of the converted text,
	this function will return an error code and the values in prgchDst and pcchNeed should be
	ignored. If a character does not have a corresponding rule in the table, the ProcessError
	method is called on pccww. If pccww is NULL, the missing rule is ignored and the character
	is placed in prgchDst without any conversion.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertWToW::ConvertRgch(OLECHAR * prgchSrc, int cchSrc,
	OLECHAR * prgchDst, int cchDst, ICsCallbackWToW * pccww, int * pcchNeed)
{
	AssertArray(prgchSrc, cchSrc);
	AssertArray(prgchDst, cchDst);
	AssertPtrN(pccww);
	AssertPtrN(pcchNeed);
	if (!prgchSrc || (!prgchDst && cchDst) || (!prgchDst && !cchDst && !pcchNeed))
		return WarnHr(E_POINTER);
	if (!m_ptl)
		return WarnHr(E_UNEXPECTED);

	if (!cchSrc)
		return S_OK;
	return ConvertCore(prgchSrc, cchSrc, &prgchDst, cchDst, pcchNeed, kctConvertArray, pccww);
}

/*----------------------------------------------------------------------------------------------
	ConvertCallback takes an array of characters and calls the HaveText method on pccww
	every time a maximum of cchChunk characters is converted. cchChunk refers to the length
	of the converted text, not the length of the source text.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsConvertWToW::ConvertCallback(OLECHAR * prgchSrc, int cchSrc, int cchChunk,
	ICsCallbackWToW * pccww)
{
	AssertArray(prgchSrc, cchSrc);
	AssertPtrN(pccww);
	if (!pccww || !prgchSrc)
		return WarnHr(E_POINTER);
	if (!m_ptl)
		return WarnHr(E_UNEXPECTED);

	if (!cchSrc)
		return S_OK;
	OLECHAR * prgchDst = (OLECHAR *)CoTaskMemAlloc(cchChunk << 1);
	HRESULT hr = ConvertCore(prgchSrc, cchSrc, &prgchDst, cchChunk, NULL, kctConvertCB, pccww);
	CoTaskMemFree(prgchDst);
	return hr;
}

/*----------------------------------------------------------------------------------------------
	ConvertCore gets called by the other three Convert methods; ct specifies which one called
	it.
----------------------------------------------------------------------------------------------*/
HRESULT CsConvertWToW::ConvertCore(OLECHAR * prgchSrc, int cchSrc, OLECHAR ** pprgchDst,
	int cchDst, int * pcchNeed, ConvertType ct, ICsCallbackWToW * pccww)
{
	AssertArray(prgchSrc, cchSrc);
	AssertPtr(pprgchDst);
	AssertArray(*pprgchDst, cchDst);
	AssertPtrN(pcchNeed);
	AssertPtrN(pccww);
	Assert(pccww || ct != kctConvertCB);

	OLECHAR * prgchDst = *pprgchDst;
	OLECHAR * prgchBuffer = NULL;
	int cchSrcProcessed = 0;
	int cchDstProcessed = 0;

	if (kctConvert == ct)
		prgchDst += 2; // This skips past the size count for the BSTR.
	if (kctConvertArray == ct && 0 == cchDst)
	{
		AssertPtr(pcchNeed);
		*pcchNeed = 0;
		cchDst = kcchChunk;
		try
		{
			prgchBuffer = NewObj OLECHAR[kcchChunk];
		}
		catch (...)
		{
			return WarnHr(E_OUTOFMEMORY);
		}
		prgchDst = prgchBuffer;
	}

	HRESULT hr = S_OK;
	while (cchSrcProcessed < cchSrc)
	{
		int cchSrcT = cchSrc - cchSrcProcessed;
		int cchDstT = cchDst;
		if (!ConvertChunk(prgchSrc, &cchSrcT, prgchDst, &cchDstT, cchSrcProcessed, pccww))
		{
			// The process was cancelled because of a missing rule.
			hr = S_FALSE;
			break;
		}
		cchSrcProcessed += cchSrcT;
		prgchSrc += cchSrcT;
		cchDstProcessed += cchDstT;
		if (kctConvert == ct) // from Convert
		{
			if (cchSrcProcessed < cchSrc)
			{
				// Double the size of memory for the destination buffer.
				// Add space for the BSTR size count and the NULL at the end.
				int cchDstNew = (cchDstProcessed << 1) + 3;
				int cchOffset = prgchDst - *pprgchDst + cchDstT;
				*pprgchDst = (OLECHAR *)CoTaskMemRealloc(*pprgchDst,
					cchDstNew * sizeof(OLECHAR));
				if (!*pprgchDst)
				{
					hr = WarnHr(E_OUTOFMEMORY);
					break;
				}
				prgchDst = *pprgchDst + cchOffset;
				// Subtract space for the BSTR size count and the NULL at the end.
				cchDst = cchDstNew - cchDstProcessed - 3;
			}
		}
		else if (kctConvertCB == ct) // from ConvertChunk
		{
			pccww->HaveText(*pprgchDst, cchDstT, cchSrcProcessed);
		}
		else // from ConvertRgch
		{
			Assert(kctConvertArray == ct);
			if (cchSrcProcessed != cchSrc && !prgchBuffer)
			{
				hr = WarnHr(E_FAIL);
				break;
			}
		}
	}

	if (prgchBuffer)
		delete prgchBuffer;
	if (pcchNeed)
		*pcchNeed = cchDstProcessed;
	return hr;
}

/*----------------------------------------------------------------------------------------------
	pcchSrc -> This contains the number of characters to process from prgchSrc. On exit, this
		contains the number of characters that were actually processed from prgchSrc.
	pcchDst -> This contains the size of prgchSDst. On exit, this contains the number of
		characters that were actually placed in prgchDst.

	This method returns false if a rule was not found and fContinue was set to false in the
		ProcessError callback method.
----------------------------------------------------------------------------------------------*/
bool CsConvertWToW::ConvertChunk(OLECHAR * prgchSrc, int * pcchSrc, OLECHAR * prgchDst,
	int * pcchDst, int cchSrcProcessed, ICsCallbackWToW * pccww)
{
	AssertPtr(pcchSrc);
	AssertPtr(pcchDst);
	AssertArray(prgchSrc, *pcchSrc);
	AssertArray(prgchDst, *pcchDst);
	AssertPtrN(pccww);

	void * pv = NULL;
	int cchInput = *pcchSrc;
	int cchOutput = *pcchDst;
	int cchKey;
	int cch;
	int cchSrc = 0;
	int cchDst = 0;
	BOOL fContinue = true;

	// If there is not enough room in the destination buffer, return false.
	while (cchSrc < cchInput && cchDst < cchOutput)
	{
		cchKey = 0;
		if (S_OK == m_ptl->FindKey(prgchSrc, &cchKey, &pv))
		{
			cch = wcslen((OLECHAR *)pv);
			if (cchDst + cch <= cchOutput)
			{
				memmove(prgchDst, pv, cch << 1);
				prgchDst += cch;
				cchDst += cch;
			}
			else
			{
				// Not enough room in output buffer.
				break;
			}
		}
		else
		{
			cchKey = 1;
			if (pccww)
				pccww->ProcessError(cchSrcProcessed + cchSrc, &fContinue);
			if (!fContinue)
				break;
			if (cchDst + 1 <= cchOutput)
			{
				*prgchDst++ = *prgchSrc;
				cchDst++;
			}
			else
			{
				// Not enough room in output buffer.
				break;
			}
		}
		prgchSrc += cchKey;
		cchSrc += cchKey;
	}
	*pcchSrc = cchSrc;
	*pcchDst = cchDst;
	return fContinue;
}