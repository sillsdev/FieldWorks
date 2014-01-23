/*----------------------------------------------------------------------------------------------
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: SilTestHarness.h
Responsibility: Darrell Zook
Last reviewed: Not yet.

Usage:
	See TestHarnessTlb.idl for a description of the interfaces supported by the Test Harness
----------------------------------------------------------------------------------------------*/

#pragma once
#ifndef TESTHARNESS_H
#define TESTHARNESS_H 1


/*----------------------------------------------------------------------------------------------
	Trivial subclass of exception allows us to catch this specific one.
----------------------------------------------------------------------------------------------*/
class SilAssertException : public _exception
{
};


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
	SilTestHarness
/**********************************************************************************************/

// Hungarian: sti
typedef struct
{
	char szTestName[MAX_PATH];
	int itdi;
	int nCookie;
} SingleTestInfo;

// Hungarian: tdi
typedef struct
{
	char szProgId[100];
	ISilTestPtr qtst;
	int itstMin;
	int itstLim;
	int ctst;
} TestDllInfo;

typedef struct
{
	char szName[100];
	bool fPublic;
	Vector<int> vitst;
} TestGroupInfo;


class SilTestHarness : public ISilTestHarness
{
public:
	//static methods
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);

	// constructors/destructors
	SilTestHarness();
	~SilTestHarness();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// The ISilTestHarness methods.
	STDMETHOD(get_TestCount)(int * pctst);
	STDMETHOD(get_TestName)(int itst, BSTR * pbstrName);

	STDMETHOD(get_DllCount)(int * pcdll);
	STDMETHOD(get_DllProgId)(int idll, BSTR * pbstrProgId);
	STDMETHOD(GetTestRangeInDll)(int idll, int * pitstMin, int * pitstLim);

	STDMETHOD(get_GroupCount)(int * pctstg);
	STDMETHOD(get_GroupName)(int itstg, BSTR * pbstrName);
	STDMETHOD(get_GroupSize)(int itstg, int * pctst);
	STDMETHOD(get_IsGroupPublic)(int itstg, BOOL * pfPublic);
	STDMETHOD(get_CanModifyPublicGroups)(BOOL * pfCanModify);
	STDMETHOD(GetGroupTestIndex)(int itstg, int itst, int * pitstInTotal);
	STDMETHOD(AddTestToGroup)(int itstg, int itst);
	STDMETHOD(DeleteTestFromGroup)(int itstg, int itst);
	STDMETHOD(CreateNewGroup)(BSTR bstrName, int itstg, BOOL fPublic, int * pitstg);
	STDMETHOD(DeleteGroup)(int itstg);

	STDMETHOD(Initialize)();
	STDMETHOD(RunTests)(int * prgitst, int ctst, BSTR bstrLog,
		ISilTestHarnessSite * ptsthsi);
	STDMETHOD(RunSingle)(BSTR bstrProgId, int itstInDll, BSTR bstrLog,
		ISilTestHarnessSite * ptsthsi);
	STDMETHOD(RunGroup)(int itstg, BSTR bstrLog, ISilTestHarnessSite * ptsthsi);

protected:
	HRESULT GetTestFromIndex(int itst, ISilTest ** pptst, int * pnCookie);
	void SaveGroups(bool fPublic);

	long m_cref;
	Vector<SingleTestInfo> m_vsti;
	Vector<TestDllInfo> m_vtdi;
	Vector<TestGroupInfo> m_vtgi;
	ulong m_ctgi;
	TestGroupInfo * m_prgtgi;
	bool m_fInitialized;
	bool m_fNoPublicChanges;
	bool m_fDirtyPublic;
	bool m_fDirtyPrivate;
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


/***********************************************************************************************
	SilDebugProcs
/**********************************************************************************************/

class SilDebugProcs : public ISilDebugProcs
{
public:
	//static methods
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);

	// constructors/destructors
	SilDebugProcs();
	~SilDebugProcs();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// The ISilDebugProcs methods.
	STDMETHOD(WarnProc)(BSTR bstrExplanation, BSTR bstrSource);
	STDMETHOD(AssertProc)(BSTR bstrExplanation, BSTR bstrSource);
	STDMETHOD(HideWarnings)(BOOL fHide);
	STDMETHOD(HideAsserts)(BOOL fHide);
	STDMETHOD(HideErrors)(BOOL fHide);

protected:
	long m_cref;
};

#endif // !TESTHARNESS_H