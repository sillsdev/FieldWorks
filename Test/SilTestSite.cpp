#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE


/***********************************************************************************************
	SilSeqStream
/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/

SilTestStream::SilTestStream(FILE * pfile)
{
	m_pfile = pfile;
	m_cref = 1;
}

/*----------------------------------------------------------------------------------------------
	Static method to create a SilTestStream.
----------------------------------------------------------------------------------------------*/
HRESULT SilTestStream::Create(FILE * pfile, IStream ** ppstr)
{
	if (!ppstr)
		return WarnHr(E_POINTER);
	*ppstr = NULL;
	try
	{
		ComSmartPtr<SilTestStream> qstr;
		qstr.Attach(NewObj SilTestStream(pfile));
		HRESULT hr = qstr->QueryInterface(IID_IStream, (void **)ppstr);
		return hr;
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
}

/*----------------------------------------------------------------------------------------------
	QueryInterface
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestStream::QueryInterface(REFIID riid, void ** ppv)
{
	if (!ppv)
		return WarnHr(E_POINTER);
	AssertPtr(ppv);
	*ppv = NULL;
	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IStream)
		*ppv = static_cast<IStream *>(this);
	else
		return E_NOINTERFACE;
	AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	AddRef
----------------------------------------------------------------------------------------------*/
ULONG SilTestStream::AddRef(void)
{
	return InterlockedIncrement(&m_cref);
}

/*----------------------------------------------------------------------------------------------
	Release
----------------------------------------------------------------------------------------------*/
ULONG SilTestStream::Release(void)
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
	We do not support reading from the stream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestStream::Read(void * pv, ULONG cb, ULONG * pcbRead)
{
	return WarnHr(E_NOTIMPL);
}

/*----------------------------------------------------------------------------------------------
	Write a buffer to the file.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestStream::Write(void const * pv, ULONG cb, ULONG * pcbWritten)
{
	if (!cb)
		return S_OK;
	if (!pv)
		return WarnHr(E_POINTER);
	if (!m_pfile)
		return WarnHr(E_UNEXPECTED);
	ULONG cbWritten = fwrite(pv, 1, cb, m_pfile);
	if (pcbWritten)
		*pcbWritten = cbWritten;
	if (cbWritten == cb)
		return S_OK;
	return WarnHr(E_FAIL);
}


/***********************************************************************************************
	ISilTestSite
/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
SilTestSite::SilTestSite()
{
	m_pfileLog = NULL;
	m_pfileBsl = NULL;
	Assert(m_pfileLog == NULL);
	Assert(m_pfileBsl == NULL);

	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
SilTestSite::~SilTestSite()
{
	if (m_pfileLog)
	{
		fclose(m_pfileLog);
		m_pfileLog = NULL;
	}
	if (m_pfileBsl)
	{
		fclose(m_pfileBsl);
		m_pfileBsl = NULL;
	}
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Static method to create an SilTestSite.
----------------------------------------------------------------------------------------------*/
HRESULT SilTestSite::Create(ISilTestSite ** pptstsi)
{
	if (!pptstsi)
		return WarnHr(E_POINTER);
	*pptstsi = NULL;
	try
	{
		ComSmartPtr<SilTestSite> qtstsi;
		qtstsi.Attach(NewObj SilTestSite);
		HRESULT hr = qtstsi->QueryInterface(IID_ISilTestSite, (void **)pptstsi);

		GetModuleFileName(ModuleEntry::GetModuleHandle(), qtstsi->m_szLogPath,
			isizeof(qtstsi->m_szLogPath));
		strcat(qtstsi->m_szLogPath, "\\..\\..\\..\\TestLog\\");
		GetFullPathName(qtstsi->m_szLogPath, isizeof(qtstsi->m_szLogPath), qtstsi->m_szLogPath,
			NULL);
		strcpy(qtstsi->m_szBaselinePath, qtstsi->m_szLogPath);
		strcat(qtstsi->m_szLogPath, "Log\\");
		strcat(qtstsi->m_szBaselinePath, "Baseline\\");
		CreateDirectory(qtstsi->m_szLogPath, NULL);

		return hr;
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
}

/*----------------------------------------------------------------------------------------------
	QueryInterface
----------------------------------------------------------------------------------------------*/
HRESULT SilTestSite::QueryInterface(REFIID iid, void ** ppv)
{
	if (!ppv)
		return WarnHr(E_POINTER);
	AssertPtr(ppv);
	*ppv = NULL;
	if (iid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (iid == IID_ISilTestSite)
		*ppv = static_cast<ISilTestSite *>(this);
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	AddRef
----------------------------------------------------------------------------------------------*/
ULONG SilTestSite::AddRef(void)
{
	return InterlockedIncrement(&m_cref);
}

/*----------------------------------------------------------------------------------------------
	Release
----------------------------------------------------------------------------------------------*/
ULONG SilTestSite::Release(void)
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
	Open the requested log file for writing. bstrLogFile should be just a filename (no path).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestSite::SetLogFile(BSTR bstrLogFile)
{
	AssertBstr(bstrLogFile);

	if (m_pfileLog)
	{
		fclose(m_pfileLog);
		m_pfileLog = NULL;
	}
	if (!bstrLogFile)
		return WarnHr(E_INVALIDARG);

	try
	{
		StrAnsi staLogFile = m_szLogPath;
		staLogFile += bstrLogFile;

		m_pfileLog = fopen(staLogFile.Chars(), "wb");
		if (!m_pfileLog)
			return WarnHr(STG_E_SHAREVIOLATION);
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Open the requested baseline file for writing. If bstrBslFile is empty or NULL, S_FALSE is
	returned. bstrBslFile should be just a filename without an extension.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestSite::SetBaselineFile(BSTR bstrBslFile)
{
	AssertBstrN(bstrBslFile);

	if (m_pfileBsl)
	{
		fclose(m_pfileBsl);
		m_pfileBsl = NULL;
	}
	if (!BstrLen(bstrBslFile))
		return S_FALSE;

	try
	{
		m_staBslNew = m_szLogPath;
		m_staBslNew += bstrBslFile;
		m_staBslNew += ".bsn";

		m_staBslOld = m_szBaselinePath;
		m_staBslOld += bstrBslFile;
		m_staBslOld += ".bso";
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}

	m_pfileBsl = fopen(m_staBslNew, "wb");
	if (!m_pfileBsl)
		return WarnHr(STG_E_SHAREVIOLATION);
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Write out starting test information to the log file.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestSite::BeginTest(int itst, BSTR bstrProgId, BSTR bstrTestName)
{
	AssertBstr(bstrProgId);
	AssertBstr(bstrTestName);

	m_cFailure = m_cMs = 0;
	m_stimStart.SetToCurTime();
	StrAnsiBufPath stabp;
	stabp.Format("-------------------------------------------------------------------%n"
		"Starting test %S: %S (%d)%n", bstrProgId, bstrTestName, itst);
	HRESULT hr = Log(stabp.Chars(), stabp.Length());
	if (m_pfileBsl)
	{
		stabp.Format("Baselining to %s%n", m_staBslNew.Chars());
		hr = Log(stabp.Chars(), stabp.Length());
	}
	return hr;
}

/*----------------------------------------------------------------------------------------------
	Write out ending test information to the log file. This includes the amount of time the
	test took to run and the number of errors in the test.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestSite::EndTest(int itst, BSTR bstrProgId, BSTR bstrTestName, HRESULT hr)
{
	AssertBstr(bstrProgId);
	AssertBstr(bstrTestName);

	if (m_pfileBsl)
	{
		// Don't close the log file yet, just the baseline file.
		fclose(m_pfileBsl);
		m_pfileBsl = NULL;
		CompareBslFiles();
		m_staBslOld.Clear();
		m_staBslNew.Clear();
	}

	SilTime stimStop;
	stimStop.SetToCurTime();
	m_cMs = (int)(stimStop - m_stimStart);

	// If the test returned a failure code, add one to the failure count.
	if (FAILED(hr))
		m_cFailure++;

	StrAnsiBufPath stabp;
	stabp.Format("The test completed in %d ms with %d error(s).%n", m_cMs, m_cFailure);
	Log(stabp.Chars(), stabp.Length());
	stabp.Format("End test %S: %S (%d)%n"
		"-------------------------------------------------------------------%n%n",
		bstrProgId, bstrTestName, itst);
	return Log(stabp.Chars(), stabp.Length());
}

/*----------------------------------------------------------------------------------------------
	Write a failure string out to the log file and increment the failure count. The string
	should not contain a newline character at the end.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestSite::Failure(const CHAR * prgch, int cch)
{
	AssertArray(prgch, cch);

	m_cFailure++;
	Log("Failure: ", 9);
	Log(prgch, cch);
	return Log("\r\n", 2);
}

STDMETHODIMP SilTestSite::FailureW(const OLECHAR * prgwch, int cch)
{
	AssertArray(prgwch, cch);

	try
	{
		StrAnsi sta;
		sta.Assign(prgwch, cch);
		return Failure(sta.Chars(), sta.Length());
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}
}

STDMETHODIMP SilTestSite::FailureBstr(BSTR bstr)
{
	AssertBstr(bstr);

	return FailureW(bstr, BstrLen(bstr));
}

/*----------------------------------------------------------------------------------------------
	Write a string out to the log file.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestSite::Log(const CHAR * prgch, int cch)
{
	AssertArray(prgch, cch);
	if (!m_pfileLog)
		return WarnHr(E_UNEXPECTED);

	if (fwrite(prgch, sizeof(char), cch, m_pfileLog) == (uint)cch)
		return S_OK;
	return WarnHr(E_FAIL); // todo: better error code indicating file IO problem?
}

STDMETHODIMP SilTestSite::LogW(const OLECHAR * prgwch, int cch)
{
	AssertArray(prgwch, cch);

	try
	{
		StrAnsi sta;
		sta.Assign(prgwch, cch);
		return Log(sta.Chars(), sta.Length());
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}
}

STDMETHODIMP SilTestSite::LogBstr(BSTR bstr)
{
	AssertBstr(bstr);

	return LogW(bstr, BstrLen(bstr));
}

/*----------------------------------------------------------------------------------------------
	Write a string out to the baseline file.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestSite::Baseline(const CHAR * prgch, int cch)
{
	AssertArray(prgch, cch);

	if (!m_pfileBsl)
	{
		char * pszError = "*** A baseline file must be specified before baselining "
			"will work. ***";
		return Failure(pszError, lstrlen(pszError));
	}
	if (fwrite(prgch, sizeof(char), cch, m_pfileBsl) == (uint)cch)
		return S_OK;
	return WarnHr(E_FAIL); // todo: better error code indicating file IO problem?
}

STDMETHODIMP SilTestSite::BaselineW(const OLECHAR * prgwch, int cch)
{
	AssertArray(prgwch, cch);

	try
	{
		StrAnsi sta;
		sta.Assign(prgwch, cch);
		return Baseline(sta.Chars(), sta.Length());
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}
}

STDMETHODIMP SilTestSite::BaselineBstr(BSTR bstr)
{
	AssertBstr(bstr);

	return BaselineW(bstr, BstrLen(bstr));
}

/*----------------------------------------------------------------------------------------------
	Get a sequential stream. Only the Write method on the stream will work. Writing anything
	to the stream will result in it being written to the log file.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestSite::GetBaselineStream(IStream ** ppstr)
{
	AssertPtrN(ppstr);
	if (!ppstr)
		return WarnHr(E_POINTER);

	return SilTestStream::Create(m_pfileBsl, ppstr);
}

/*----------------------------------------------------------------------------------------------
	This compares the original baseline file with the newly created baseline file and logs
	any differences between them as errors.
----------------------------------------------------------------------------------------------*/
HRESULT SilTestSite::CompareBslFiles(void)
{
	if (!m_staBslOld || !m_staBslNew)
		return WarnHr(E_UNEXPECTED);
	FILE * pfileBslNew = fopen(m_staBslNew, "rb");
	if (!pfileBslNew)
	{
		char * psz = "Could not open the new baseline file.\r\n";
		Failure(psz, lstrlen(psz));
		return WarnHr(STG_E_PATHNOTFOUND);
	}
	FILE * pfileBslOld = fopen(m_staBslOld, "rb");
	if (!pfileBslOld)
	{
		char * psz = "Could not open the old baseline file.\r\n";
		Failure(psz, lstrlen(psz));
		fclose(pfileBslNew);
		return WarnHr(STG_E_PATHNOTFOUND);
	}

	char szLineOld[1024];
	char szLineNew[1024];
	int cch;
	int iLine = 0;
	HRESULT hr = S_OK;
	while (!feof(pfileBslNew))
	{
		if (feof(pfileBslOld))
		{
			char * psz = "The new baseline file is longer than the old one.\r\n";
			Failure(psz, lstrlen(psz));
			hr = S_FALSE;
			break;
		}
		fgets(szLineOld, isizeof(szLineOld), pfileBslOld);
		fgets(szLineNew, isizeof(szLineNew), pfileBslNew);
		cch = lstrlen(szLineOld);
		iLine++;
		if (cch != lstrlen(szLineNew) ||
			memcmp(szLineOld, szLineNew, cch) != 0)
		{
			StrAnsi sta;
			sta.Format("The new baseline file differs from the old one at line %d.%n", iLine);
			Failure(sta.Chars(), sta.Length());
			hr = S_FALSE;
			break;
		}
	}
	if (hr == S_OK && !feof(pfileBslOld))
	{
		char * psz = "The old baseline file is longer than the new one.\r\n";
		Failure(psz, lstrlen(psz));
		hr = S_FALSE;
	}
	if (hr == S_OK)
	{
		char * psz = "The old and new baseline files are identical.\r\n";
		Log(psz, lstrlen(psz));
	}
	fclose(pfileBslOld);
	fclose(pfileBslNew);
	return hr;
}

/*----------------------------------------------------------------------------------------------
	Get the number of errors in the test.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestSite::get_FailureCount(int * pcFailure)
{
	AssertPtrN(pcFailure);
	if (!pcFailure)
		return WarnHr(E_UNEXPECTED);
	*pcFailure = m_cFailure;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Get how long the test took to run (in ms).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestSite::get_MsToRun(int * pcMs)
{
	AssertPtrN(pcMs);
	if (!pcMs)
		return WarnHr(E_UNEXPECTED);
	*pcMs = m_cMs;
	return S_OK;
}
/*----------------------------------------------------------------------------------------------
		Various modified Baselining functions ("Output" style)
----------------------------------------------------------------------------------------------*/
HRESULT SilTestSite::Output(LPCSTR pszs)
{
	return Baseline(pszs, lstrlen(pszs));
}

HRESULT SilTestSite::OutputW(LPCOLESTR pszw)
{
	return BaselineW(pszw, lstrlenW(pszw));
}

HRESULT SilTestSite::OutputFormat(LPCSTR pszs, ...)
{
	StrAnsi sta;
	try
	{
		sta.FormatCore(pszs, StrLen(pszs), (uint *)(&pszs + 1));
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
	return Baseline(sta.Chars(), sta.Length());
}

HRESULT SilTestSite::OutputFormatW(LPCOLESTR pszw, ...)
{
	StrUni stu;
	try
	{
		stu.FormatCore(pszw, StrLen(pszw), (uint *)(&pszw + 1));
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
	return BaselineW(stu.Chars(), stu.Length());
}