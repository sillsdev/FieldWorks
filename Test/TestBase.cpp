/*----------------------------------------------------------------------------------------------
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestBase.cpp
Responsibility: Darrell Zook
Last reviewed: Not yet.
----------------------------------------------------------------------------------------------*/

#include "main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

// Don't auto initialize this! It will be set to 0 automatically before any
// constructor code is run. If you auto-initialize it we run the risk of some
// constructor code accessing it before it has been set.
TestBase * TestBase::s_ptstFirst;


/***********************************************************************************************
	TestBase
/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
TestBase::TestBase(wchar * pwszName, int nCookie)
	: LLBase<TestBase>(&s_ptstFirst)
{
	AssertPtr(pwszName);
	m_stuName = pwszName;
	m_nCookie = nCookie;
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
TestBase::~TestBase()
{
}

/*----------------------------------------------------------------------------------------------
	Static method to find out the number of tests stored in the DLL
----------------------------------------------------------------------------------------------*/
int TestBase::NumberTests()
{
	int ctst = 0;
	TestBase * ptst = s_ptstFirst;
	for ( ; ptst; ptst = ptst->m_pobjNext)
		ctst++;
	return ctst;
}

/*----------------------------------------------------------------------------------------------
	Static method to retrieve the TestBase pointer to the requested test
----------------------------------------------------------------------------------------------*/
TestBase * TestBase::GetTestFromCookie(int nCookie)
{
	for (TestBase * ptst = s_ptstFirst; ptst && ptst->m_nCookie != nCookie; )
		ptst = ptst->m_pobjNext;
	return ptst;
}

/*----------------------------------------------------------------------------------------------
	Static method to retrieve the TestBase pointer to the requested test
----------------------------------------------------------------------------------------------*/
TestBase * TestBase::GetTest(int itstb)
{
	for (TestBase * ptst = s_ptstFirst; ptst && itstb--; )
		ptst = ptst->m_pobjNext;
	return ptst;
}

/*----------------------------------------------------------------------------------------------
	Instance method to return the name of the test
----------------------------------------------------------------------------------------------*/
HRESULT TestBase::Name(StrUni & stuName)
{
	try
	{
		stuName = m_stuName;
	}
	catch (...)
	{
		return WarnHr(E_UNEXPECTED);
	}
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Instance method to return the name of the test
----------------------------------------------------------------------------------------------*/
HRESULT TestBase::Name(StrAnsi & staName)
{
	try
	{
		staName = m_stuName;
	}
	catch (...)
	{
		return WarnHr(E_UNEXPECTED);
	}
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Instance method to return the filename of the baseline file for the test
----------------------------------------------------------------------------------------------*/
HRESULT TestBase::BslFile(StrUni & stuBsl)
{
	try
	{
		stuBsl = m_stuBsl;
	}
	catch (...)
	{
		return WarnHr(E_UNEXPECTED);
	}
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Instance method to return the filename of the baseline file for the test
----------------------------------------------------------------------------------------------*/
HRESULT TestBase::BslFile(StrAnsi & staBsl)
{
	try
	{
		staBsl = m_stuBsl;
	}
	catch (...)
	{
		return WarnHr(E_UNEXPECTED);
	}
	return S_OK;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
HRESULT TestBase::RunTest(ISilTestSite * ptstsi)
{
	AssertPtr(ptstsi);

	m_ptstsi = ptstsi;
	HRESULT hr = Run();
	if (FAILED(hr))
		FailureFormat(L"Test %s failed with code %08x", m_stuName, hr);
	m_ptstsi = NULL;

	return hr;
}

/*----------------------------------------------------------------------------------------------
	If the hr passed as arg 1 represents a failure, report it using the comment.
----------------------------------------------------------------------------------------------*/
HRESULT TestBase::TestHrFail(HRESULT hr, LPCSTR pszs)
{
	if (FAILED(hr))
		FailureFormat("Test %S failed with code %08x at step %s.", m_stuName, hr, pszs);
	return WarnHr(hr);
}

HRESULT TestBase::TestHrFail(HRESULT hr, LPCOLESTR pszw)
{
	if (FAILED(hr))
		FailureFormat(L"Test %s failed with code %08x at step %s.", m_stuName, hr, pszw);
	return WarnHr(hr);
}

HRESULT TestBase::TestHrFail(HRESULT hr, const CHAR * prgch, int cch)
{
	if (FAILED(hr))
		FailureFormat("Test %S failed with code %08x at step %r.", m_stuName, hr, prgch, cch);
	return WarnHr(hr);
}

HRESULT TestBase::TestHrFail(HRESULT hr, const OLECHAR * prgwch, int cch)
{
	if (FAILED(hr))
		FailureFormat(L"Test %S failed with code %08x at step %r.", m_stuName, hr, prgwch, cch);
	return WarnHr(hr);
}


HRESULT TestBase::TestHrFailFormat(HRESULT hr, LPCSTR pszs, ...)
{
	if (FAILED(hr))
	{
		StrAnsi sta;
		// conditional expression of type 'void' is illegal
		try
		{
			sta.FormatCore(pszs, StrLen(pszs), (uint *)(&pszs + 1));
		}
		catch (...)
		{
			FailureFormat("Test %S failed with code %08x at step %s.", m_stuName, hr, pszs);
		}
	}
	return WarnHr(hr);
}

HRESULT TestBase::TestHrFailFormat(HRESULT hr, LPCOLESTR pszw, ...)
{
	if (FAILED(hr))
	{
		StrUni stu;
		// conditional expression of type 'void' is illegal
		try
		{
			stu.FormatCore(pszw, StrLen(pszw), (uint *)(&pszw + 1));
		}
		catch (...)
		{
			FailureFormat(L"Test %S failed with code %08x at step %s.", m_stuName, hr, pszw);
		}
	}
	return WarnHr(hr);
}

/*----------------------------------------------------------------------------------------------
	Write the string out to the baseline file.
----------------------------------------------------------------------------------------------*/
HRESULT TestBase::Baseline(LPCSTR pszs)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	return m_ptstsi->Baseline(pszs, lstrlen(pszs));
}

HRESULT TestBase::Baseline(LPCOLESTR pszw)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	return m_ptstsi->BaselineW(pszw, lstrlenW(pszw));
}

HRESULT TestBase::Baseline(const CHAR * prgch, int cch)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	return m_ptstsi->Baseline(prgch, cch);
}

HRESULT TestBase::Baseline(const OLECHAR * prgwch, int cch)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	return m_ptstsi->BaselineW(prgwch, cch);
}

HRESULT TestBase::BaselineFormat(LPCSTR pszs, ...)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	StrAnsi sta;
	try
	{
		sta.FormatCore(pszs, StrLen(pszs), (uint *)(&pszs + 1));
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
	return m_ptstsi->Baseline(sta.Chars(), sta.Length());
}

HRESULT TestBase::BaselineFormat(LPCOLESTR pszw, ...)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	StrUni stu;
	try
	{
		stu.FormatCore(pszw, StrLen(pszw), (uint *)(&pszw + 1));
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
	return m_ptstsi->BaselineW(stu.Chars(), stu.Length());
}

/*----------------------------------------------------------------------------------------------
	Write the string out to the log file.
----------------------------------------------------------------------------------------------*/
HRESULT TestBase::Log(LPCSTR pszs)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	return m_ptstsi->Log(pszs, lstrlen(pszs));
}

HRESULT TestBase::Log(LPCOLESTR pszw)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	return m_ptstsi->LogW(pszw, lstrlenW(pszw));
}

HRESULT TestBase::Log(const CHAR * prgch, int cch)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	return m_ptstsi->Log(prgch, cch);
}

HRESULT TestBase::Log(const OLECHAR * prgwch, int cch)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	return m_ptstsi->LogW(prgwch, cch);
}

HRESULT TestBase::LogFormat(LPCSTR pszs, ...)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	StrAnsi sta;
	try
	{
		sta.FormatCore(pszs, StrLen(pszs), (uint *)(&pszs + 1));
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
	return m_ptstsi->Log(sta.Chars(), sta.Length());
}

HRESULT TestBase::LogFormat(LPCOLESTR pszw, ...)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	StrUni stu;
	try
	{
		stu.FormatCore(pszw, StrLen(pszw), (uint *)(&pszw + 1));
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
	return m_ptstsi->LogW(stu.Chars(), stu.Length());
}

/*----------------------------------------------------------------------------------------------
	Write the string out to the log file and increment the failure count.
----------------------------------------------------------------------------------------------*/
HRESULT TestBase::Failure(LPCSTR pszs)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	return m_ptstsi->Failure(pszs, lstrlen(pszs));
}

HRESULT TestBase::Failure(LPCOLESTR pszw)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	return m_ptstsi->FailureW(pszw, lstrlenW(pszw));
}

HRESULT TestBase::Failure(const CHAR * prgch, int cch)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	return m_ptstsi->Failure(prgch, cch);
}

HRESULT TestBase::Failure(const OLECHAR * prgwch, int cch)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	return m_ptstsi->FailureW(prgwch, cch);
}

HRESULT TestBase::FailureFormat(LPCSTR pszs, ...)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	StrAnsi sta;
	try
	{
		sta.FormatCore(pszs, StrLen(pszs), (uint *)(&pszs + 1));
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
	return m_ptstsi->Failure(sta.Chars(), sta.Length());
}

HRESULT TestBase::FailureFormat(LPCOLESTR pszw, ...)
{
	if (!m_ptstsi)
		return E_UNEXPECTED;
	StrUni stu;
	try
	{
		stu.FormatCore(pszw, StrLen(pszw), (uint *)(&pszw + 1));
	}
	catch (...)
	{
		return WarnHr(E_OUTOFMEMORY);
	}
	return m_ptstsi->FailureW(stu.Chars(), stu.Length());
}


/***********************************************************************************************
	ModuleTestBaseEntry
/**********************************************************************************************/

const char * g_pszRootKey = "Software\\SIL\\Fieldworks\\Test";

class ModuleTestBaseEntry : public ModuleEntry
{
public:
	/* overriding virtual function differs from 'ModuleEntry::RegisterServer' only by return
	   type or calling convention. Same for UnregisterServer(). */
	virtual void RegisterServer(void);
	virtual void UnregisterServer(void);
};

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void ModuleTestBaseEntry::RegisterServer()
{
	RegKey hkey;
	RegKey hkeySub;
	HRESULT hr;
	DWORD dwT;
	TestBase * ptstb;
	int ctstb = TestBase::NumberTests();
	StrAnsi staSubKey;
	StrAnsi staTestName;
	StrAnsi staRootKey;
	char szFilename[MAX_PATH];

	GetModuleFileName(s_hmod, szFilename, sizeof(szFilename));
	strcat(szFilename, "..\\..\\..\\..\\TestLog\\Log");
	GetFullPathName(szFilename, sizeof(szFilename), szFilename, NULL);

	// Open the Software\SIL\Fieldworks\Test key
	if (0 != RegCreateKeyEx(HKEY_LOCAL_MACHINE, g_pszRootKey, 0, REG_NONE,
		REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, NULL, &hkey, &dwT))
	{
		ThrowHr(WarnHr(E_FAIL));
	}

	// Write the path to the log file in Software\SIL\Fieldworks\Test
	if (0 != RegSetValueEx(hkey, "Log Path", 0, REG_SZ, (BYTE *)szFilename,
		lstrlen(szFilename) + 1))
	{
		ThrowHr(WarnHr(E_FAIL));
	}

	// Open the Software\SIL\Fieldworks\Test\<ProgId> key
	staRootKey.Format("%s\\%s", g_pszRootKey, TESTPROGID);
	if (0 != RegCreateKeyEx(HKEY_LOCAL_MACHINE, staRootKey.Chars(), 0, REG_NONE,
		REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, NULL, &hkey, &dwT))
	{
		ThrowHr(WarnHr(E_FAIL));
	}

	for (int itstb = 0; itstb < ctstb; itstb++)
	{
		// Get information about the test
		ptstb = TestBase::GetTest(itstb);
		if (!ptstb)
			ThrowHr(WarnHr(E_UNEXPECTED));
		if (FAILED(hr = ptstb->Name(staTestName)))
			ThrowHr(WarnHr(hr));

		// Create a subkey for the test (HKLM\Software\SIL\Fieldworks\Test\<Name>)
		staSubKey.Format("%s\\%s", staRootKey.Chars(), staTestName.Chars());
		if (0 != RegCreateKeyEx(HKEY_LOCAL_MACHINE, staSubKey.Chars(), 0, REG_NONE,
			REG_OPTION_NON_VOLATILE, KEY_READ | KEY_WRITE, NULL, &hkeySub, &dwT))
		{
			ThrowHr(WarnHr(E_FAIL));
		}

		// Write the cookie of the test (in HKLM\Software\SIL\Fieldworks\Test\<Name>)
		dwT = ptstb->Cookie();
		if (0 != RegSetValueEx(hkeySub, "Cookie", 0, REG_DWORD, (BYTE *)&dwT, sizeof(DWORD)))
			ThrowHr(WarnHr(E_FAIL));
	}
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void ModuleTestBaseEntry::UnregisterServer()
{
	RegKey hkey;

	// Delete the Name subkey from HKLM\Software\SIL\Fieldworks\Test
	LONG lResult = RegOpenKeyEx(HKEY_LOCAL_MACHINE, g_pszRootKey, 0, KEY_READ | KEY_WRITE,
		&hkey);
	if (ERROR_SUCCESS == lResult)
		lResult = DeleteSubKey(hkey, TESTPROGID);
	if (ERROR_SUCCESS != lResult && ERROR_FILE_NOT_FOUND != lResult)
		ThrowHr(WarnHr(E_FAIL));
}

ModuleTestBaseEntry g_mtbe;


/***********************************************************************************************
	SilTest
***********************************************************************************************/

static GenericFactory g_fact(
	TESTPROGID,					// must be defined elsewhere, e.g., in main.h
	&CLSID_Test,					// likewise
	"Test interface",
	"Apartment",
	&SilTest::CreateCom);

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
SilTest::SilTest()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
SilTest::~SilTest()
{
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Static method to create an SilTest
----------------------------------------------------------------------------------------------*/
void SilTest::CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv)
{
	if (!ppv)
		ThrowHr(WarnHr(E_POINTER));
	*ppv = NULL;
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	SilTest * ptstb = NewObj SilTest();
	CheckHr(ptstb->QueryInterface(riid, ppv));
	ptstb->Release();
}

/*----------------------------------------------------------------------------------------------
	AddRef
----------------------------------------------------------------------------------------------*/
ULONG SilTest::AddRef(void)
{
	return InterlockedIncrement(&m_cref);
}

/*----------------------------------------------------------------------------------------------
	Release
----------------------------------------------------------------------------------------------*/
ULONG SilTest::Release(void)
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
	QueryInterface
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTest::QueryInterface(REFIID riid, void ** ppv)
{
	if (!ppv)
		return WarnHr(E_POINTER);
	AssertPtr(ppv);
	*ppv = NULL;
	if (IID_IUnknown == riid)
		*ppv = static_cast<IUnknown *>(this);
	else if (IID_ISilTest == riid)
		*ppv = static_cast<ISilTest *>(this);
	else
		return WarnHr(E_NOINTERFACE);
	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTest::Initialize(ISilTestSite * ptstsi)
{
	if (!ptstsi)
		return WarnHr(E_POINTER);
	AssertPtr(ptstsi);
	m_qtstsi = ptstsi;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTest::Close()
{
	m_qtstsi.Clear();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTest::RunTest(int nCookie)
{
	TestBase * ptstb = TestBase::GetTestFromCookie(nCookie);
	if (!ptstb)
		return WarnHr(E_INVALIDARG);
	AssertPtr(ptstb);
	return ptstb->RunTest(m_qtstsi);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTest::BaselineFile(int nCookie, BSTR * pbstr)
{
	AssertPtrN(pbstr);
	if (!pbstr)
		return WarnHr(E_POINTER);
	*pbstr = NULL;

	HRESULT hr;
	StrUni stuBsl;
	TestBase * ptstb = TestBase::GetTestFromCookie(nCookie);
	if (!ptstb)
		return WarnHr(E_INVALIDARG);
	AssertPtr(ptstb);
	if (FAILED(hr = ptstb->BslFile(stuBsl)))
		return WarnHr(hr);
	stuBsl.GetBstr(pbstr);
	return S_OK;
}