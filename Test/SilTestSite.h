/*
		Implementation of ISilTestSite taken from the TestHarness in order to test
		view overlays. Modified somewhat to provide "hijacked" testing without going through
		Testman or RunTest
*/

#ifndef VIEW_SILTESTSITE
#define VIEW_SILTESTSITE 1

/***********************************************************************************************
	SilTestStream
***********************************************************************************************/

class SilTestStream : public IStream
{
public:
	// Static methods
	static HRESULT Create(FILE * pfile, IStream ** ppstr);

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// ISequentialStream methods
	STDMETHOD(Read)(void * pv, ULONG cb, ULONG * pcbRead);
	STDMETHOD(Write)(void const * pv, ULONG cb, ULONG * pcbWritten);

	// IStream methods
	STDMETHODIMP Seek(LARGE_INTEGER dlibMove, DWORD dwOrigin, ULARGE_INTEGER * plibNewPosition)
	{
		return WarnHr(E_NOTIMPL);
	}
	STDMETHODIMP SetSize(ULARGE_INTEGER libNewSize)
	{
		return WarnHr(E_NOTIMPL);
	}
	STDMETHODIMP CopyTo(IStream * pstm, ULARGE_INTEGER cb, ULARGE_INTEGER * pcbRead,
		ULARGE_INTEGER * pcbWritten)
	{
		return WarnHr(E_NOTIMPL);
	}
	STDMETHODIMP Commit(DWORD grfCommitFlags)
	{
		return WarnHr(E_NOTIMPL);
	}
	STDMETHODIMP Revert(void)
	{
		return WarnHr(E_NOTIMPL);
	}
	STDMETHODIMP LockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType)
	{
		return WarnHr(E_NOTIMPL);
	}
	STDMETHODIMP UnlockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType)
	{
		return WarnHr(E_NOTIMPL);
	}
	STDMETHODIMP Stat(STATSTG * pstatstg, DWORD grfStatFlag)
	{
		return WarnHr(E_NOTIMPL);
	}
	STDMETHODIMP Clone(IStream ** ppstm)
	{
		return WarnHr(E_NOTIMPL);
	}

protected:
	SilTestStream(FILE * pfile);

	// Member variables
	long m_cref;
	FILE * m_pfile;
};


/***********************************************************************************************
	SilTestSite
/**********************************************************************************************/

class SilTestSite : public ISilTestSite
{
public:
	static HRESULT Create(ISilTestSite ** pptstsi);

	// constructors/destructors
	SilTestSite();
	~SilTestSite();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// ISilTestSite methods.
	STDMETHOD(SetLogFile)(BSTR bstrLogFile);
	STDMETHOD(SetBaselineFile)(BSTR bstrBslFile);
	STDMETHOD(BeginTest)(int itst, BSTR bstrProgId, BSTR bstrTestName);
	STDMETHOD(EndTest)(int itst, BSTR bstrProgId, BSTR bstrTestName, HRESULT hr);
	STDMETHOD(Failure)(const CHAR * prgch, int cch);
	STDMETHOD(FailureW)(const OLECHAR * prgwch, int cch);
	STDMETHOD(FailureBstr)(BSTR bstr);
	STDMETHOD(Log)(const CHAR * prgch, int cch);
	STDMETHOD(LogW)(const OLECHAR * prgwch, int cch);
	STDMETHOD(LogBstr)(BSTR bstr);
	STDMETHOD(Baseline)(const CHAR * prgch, int cch);
	STDMETHOD(BaselineW)(const OLECHAR * prgwch, int cch);
	STDMETHOD(BaselineBstr)(BSTR bstr);
	STDMETHOD(GetBaselineStream)(IStream ** ppstr);
	STDMETHOD(get_FailureCount)(int * pcFailure);
	STDMETHOD(get_MsToRun)(int * pcMs);

	HRESULT Output(LPCSTR pszs);
	HRESULT OutputW(LPCOLESTR pszw);
	HRESULT OutputFormat(LPCSTR pszs, ...);
	HRESULT OutputFormatW(LPCOLESTR pszw, ...);
protected:
	HRESULT CompareBslFiles(void);

	long m_cref;
	int m_cFailure;
	int m_cMs;
	FILE * m_pfileLog;
	FILE * m_pfileBsl;
	StrAnsi m_staBslNew;
	StrAnsi m_staBslOld;
	char m_szLogPath[MAX_PATH];
	char m_szBaselinePath[MAX_PATH];
	SilTime m_stimStart;
};


#endif