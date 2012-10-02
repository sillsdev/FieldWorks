/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: ConvertCallback.cpp
Responsibility: Darrell Zook
Last reviewed:

	Implementation file for the CsCallback classes.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


const char * kpszierr[] = {
	"Operation completed successfully.\n",
	"The argument is missing from line %d.\n",
	"The argument on line %d is illegal.\n",
	"The argument on line %d has already been defined above.\n",
	"The replacement is missing from line %d.\n",
	"The replacement on line %d is illegal.\n",
	"The syntax of line %d is incorrect.\n",
	"The initialization file could not be found.\n",
	"The initialization file could not be read.\n",
	"Out of memory.\n"
};
const char * kpszperr[] = {
	"Operation completed successfully.",
};


/***********************************************************************************************
	CsCallbackAToW methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CsCallbackAToW::CsCallbackAToW(ConvertProcess * pcp)
{
	m_cref = 1;
	m_pcp = pcp;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CsCallbackAToW::~CsCallbackAToW()
{
}

/*----------------------------------------------------------------------------------------------
	AddRef.
----------------------------------------------------------------------------------------------*/
ULONG CsCallbackAToW::AddRef(void)
{
	return InterlockedIncrement(&m_cref);
}

/*----------------------------------------------------------------------------------------------
	Release.
----------------------------------------------------------------------------------------------*/
ULONG CsCallbackAToW::Release(void)
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
	Static method to create a CsCallbackAToW.
----------------------------------------------------------------------------------------------*/
HRESULT CsCallbackAToW::Create(ConvertProcess * pcp, ICsCallbackAToW ** ppccaw)
{
	AssertPtr(pcp);
	AssertPtr(ppccaw);
	*ppccaw = NULL;
	CsCallbackAToW * pccaw = NewObj CsCallbackAToW(pcp);
	if (!pccaw)
		return WarnHr(E_OUTOFMEMORY);
	HRESULT hr = pccaw->QueryInterface(IID_ICsCallbackAToW, (void **)ppccaw);
	pccaw->Release();
	return hr;
}

/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsCallbackAToW::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtrN(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_ICsCallbackAToW)
		*ppv = static_cast<ICsCallbackAToW *>(this);
	else
		return E_NOINTERFACE;
	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Text from a call to ConvertCallback. This is not used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsCallbackAToW::HaveText(OLECHAR * prgch, int cch, int cbCompleted)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Write an error message to the log file if there was an initialization error.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsCallbackAToW::InitError(InitErrorCode iec, int iInvalidLine,
	BSTR bstrInvalidLine, BOOL * pfContinue)
{
	AssertPtr(m_pcp);
	HANDLE hLogFile = m_pcp->GetLogFile();
	if (!hLogFile)
		return WarnHr(E_UNEXPECTED);

	char szBuffer[MAX_PATH];
	sprintf(szBuffer, kpszierr[iec], iInvalidLine);
	DWORD dwT;
	WriteFile(hLogFile, szBuffer, strlen(szBuffer), &dwT, NULL);
	if (pfContinue)
	{
		if (m_pcp->LogError())
			*pfContinue = TRUE;
		else
			*pfContinue = FALSE;
	}
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Write an error message to the log file if there was a processing error.
	Currently a processing error means that a rule was not found.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsCallbackAToW::ProcessError(int ichInput, BOOL * pfContinue)
{
	AssertPtrN(pfContinue);
	if (!pfContinue)
		return WarnHr(E_POINTER);
	AssertPtr(m_pcp);
	HANDLE hLogFile = m_pcp->GetLogFile();
	if (!hLogFile)
		return WarnHr(E_UNEXPECTED);
	*pfContinue = FALSE;

	char szBuffer[MAX_PATH];
	char ch = ((char *)m_pcp->GetFileStart())[ichInput];
	wsprintf(szBuffer, "Warning: The character \'%c\' (0x%x) in the \"%s\" encoding starting "
		"on line %d did not match any rule.\n", ch, ch, m_pcp->GetTopEncoding(),
		m_pcp->GetLine());
	DWORD dwT;
	WriteFile(hLogFile, szBuffer, strlen(szBuffer), &dwT, NULL);

	if (m_pcp->LogError())
		*pfContinue = TRUE;
	return S_OK;
}


/***********************************************************************************************
	CsCallbackWToA methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
CsCallbackWToA::CsCallbackWToA(ConvertProcess * pcp)
{
	m_cref = 1;
	m_pcp = pcp;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
CsCallbackWToA::~CsCallbackWToA()
{
}

/*----------------------------------------------------------------------------------------------
	AddRef.
----------------------------------------------------------------------------------------------*/
ULONG CsCallbackWToA::AddRef(void)
{
	return InterlockedIncrement(&m_cref);
}

/*----------------------------------------------------------------------------------------------
	Release.
----------------------------------------------------------------------------------------------*/
ULONG CsCallbackWToA::Release(void)
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
	Static method to create a CsCallbackWToA.
----------------------------------------------------------------------------------------------*/
HRESULT CsCallbackWToA::Create(ConvertProcess * pcp, ICsCallbackWToA ** ppccwa)
{
	AssertPtr(pcp);
	AssertPtr(ppccwa);
	CsCallbackWToA * pccwa = NewObj CsCallbackWToA(pcp);
	if (!pccwa)
		return WarnHr(E_OUTOFMEMORY);
	HRESULT hr = pccwa->QueryInterface(IID_ICsCallbackWToA, (void **)ppccwa);
	pccwa->Release();
	return hr;
}

/*----------------------------------------------------------------------------------------------
	QueryInterface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsCallbackWToA::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtrN(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_ICsCallbackWToA)
		*ppv = static_cast<ICsCallbackWToA *>(this);
	else
		return E_NOINTERFACE;
	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Text from a call to ConvertCallback. This is not used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsCallbackWToA::HaveText(byte * prgch, int cch, int cchCompleted)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Write an error message to the log file if there was an initialization error.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsCallbackWToA::InitError(InitErrorCode iec, int iInvalidLine,
	BSTR bstrInvalidLine, BOOL * pfContinue)
{
	AssertPtr(m_pcp);
	HANDLE hLogFile = m_pcp->GetLogFile();
	if (!hLogFile)
		return WarnHr(E_UNEXPECTED);

	char szBuffer[MAX_PATH];
	sprintf(szBuffer, kpszierr[iec], iInvalidLine);
	DWORD dwT;
	WriteFile(hLogFile, szBuffer, strlen(szBuffer), &dwT, NULL);
	if (pfContinue)
	{
		if (m_pcp->LogError())
			*pfContinue = TRUE;
		else
			*pfContinue = FALSE;
	}
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Write an error message to the log file if there was a processing error.
	Currently a processing error means that a rule was not found.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP CsCallbackWToA::ProcessError(int ichInput, BOOL * pfContinue)
{
	AssertPtrN(pfContinue);
	if (!pfContinue)
		return WarnHr(E_POINTER);
	AssertPtr(m_pcp);
	HANDLE hLogFile = m_pcp->GetLogFile();
	if (!hLogFile)
		return WarnHr(E_UNEXPECTED);
	*pfContinue = FALSE;

	const void * pv = m_pcp->GetTopEncoding();
	if (!pv)
		return WarnHr(E_UNEXPECTED);

	char * prgch;
	if (*((OLECHAR *)pv) <= 0xFF)
	{
		int cch = wcslen((OLECHAR *)pv) + 1;
		prgch = NewObj char[cch];
		if (!prgch)
			return WarnHr(E_OUTOFMEMORY);

		OLECHAR * prgchSrc = (OLECHAR *)pv;
		OLECHAR * prgchLim = (OLECHAR *)pv + cch;
		char * prgchDst = prgch;
		while (prgchSrc < prgchLim)
			*prgchDst++ = (char)*prgchSrc++;
	}
	else
	{
		prgch = (char *)pv;
	}

	char szBuffer[MAX_PATH];
	char ch = ((char *)m_pcp->GetFileStart())[ichInput];
	sprintf(szBuffer, "Warning: The character \'%c\' (0x%x) in the \"%s\" encoding starting "
		"on line %d did not match any rule.\n", ch, ch, prgch, m_pcp->GetLine());
	DWORD dwT;
	WriteFile(hLogFile, szBuffer, strlen(szBuffer), &dwT, NULL);
	if (prgch != (char *)pv)
		delete prgch;

	if (m_pcp->LogError())
		*pfContinue = TRUE;
	return S_OK;
}