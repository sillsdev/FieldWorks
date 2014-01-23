/*----------------------------------------------------------------------------------------------
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestBase.h
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	Provides a base class for test DLLs.

Usage:
	Look at FieldWorks\Test\Harness\TestHarnessTlb.idl for usage information.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef TestBase_H
#define TestBase_H 1

/*----------------------------------------------------------------------------------------------
	TestBase
	Hungarian: tstb
----------------------------------------------------------------------------------------------*/
class TestBase : public LLBase<TestBase>
{
public:
	// Static methods
	static int NumberTests();
	static TestBase * GetTestFromCookie(int nCookie);
	static TestBase * GetTest(int itstb);

	// Constructors, destructors, etc.
	TestBase(wchar * pwszName, int nCookie);
	virtual ~TestBase();

	// Methods the derived classes must override. Fill this in to implement test
	virtual HRESULT Run() = 0;

	// Appears in log and test manager window
	HRESULT Name(StrUni & stuName);
	HRESULT Name(StrAnsi & staName);
	HRESULT BslFile(StrUni & stuBsl);
	HRESULT BslFile(StrAnsi & staBsl);
	int Cookie()
		{ return m_nCookie; }

	// other methods.
	HRESULT RunTest(ISilTestSite * ptstsi);

	// Methods to support logging
	HRESULT TestHrFail(HRESULT hr, LPCSTR pszs);
	HRESULT TestHrFail(HRESULT hr, LPCOLESTR pszw);
	HRESULT TestHrFail(HRESULT hr, const CHAR * prgch, int cch);
	HRESULT TestHrFail(HRESULT hr, const OLECHAR * prgwch, int cch);
	HRESULT TestHrFailFormat(HRESULT hr, LPCSTR pszs, ...);
	HRESULT TestHrFailFormat(HRESULT hr, LPCOLESTR pszw, ...);

	HRESULT Baseline(LPCSTR pszs);
	HRESULT Baseline(LPCOLESTR pszw);
	HRESULT Baseline(const CHAR * prgch, int cch);
	HRESULT Baseline(const OLECHAR * prgwch, int cch);
	HRESULT BaselineFormat(LPCSTR pszs, ...);
	HRESULT BaselineFormat(LPCOLESTR pszw, ...);

	HRESULT Log(LPCSTR pszs);
	HRESULT Log(LPCOLESTR pszw);
	HRESULT Log(const CHAR * prgch, int cch);
	HRESULT Log(const OLECHAR * prgwch, int cch);
	HRESULT LogFormat(LPCSTR pszs, ...);
	HRESULT LogFormat(LPCOLESTR pszw, ...);

	HRESULT Failure(LPCSTR pszs);
	HRESULT Failure(LPCOLESTR pszw);
	HRESULT Failure(const CHAR * prgch, int cch);
	HRESULT Failure(const OLECHAR * prgwch, int cch);
	HRESULT FailureFormat(LPCSTR pszs, ...);
	HRESULT FailureFormat(LPCOLESTR pszw, ...);

private:
	static TestBase * s_ptstFirst;

protected:
	ISilTestSite * m_ptstsi;

	// The next two values must be filled in by each test class in its constructor.
	StrUni m_stuName;
	int m_nCookie;
	// This value should be filled in by each test class in its constructor if it wants to
	// create and use a baseline file.
	StrUni m_stuBsl;
};


/*----------------------------------------------------------------------------------------------
	SilTest implements the ISilTest interface.
	Hungarian: tst
----------------------------------------------------------------------------------------------*/
class SilTest : public ISilTest
{
public:
	//static methods
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);

	// constructors/destructors
	SilTest();
	virtual ~SilTest();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// The actual ISilTest methods.
	STDMETHOD(Initialize)(ISilTestSite * ptstsi);
	STDMETHOD(Close)();
	STDMETHOD(RunTest)(int nCookie);
	STDMETHOD(BaselineFile)(int nCookie, BSTR * pbstr);

protected:
	long m_cref;
	ISilTestSitePtr m_qtstsi;
};


#endif // !TestBase_H