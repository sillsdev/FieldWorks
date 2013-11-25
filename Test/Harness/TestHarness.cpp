/*----------------------------------------------------------------------------------------------
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: SilTestHarness.cpp
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	ISequentialStream, ISilTestHarness, and ISilTestSite implementations
----------------------------------------------------------------------------------------------*/
#include "main.h"
#pragma hdrstop

#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

const char * g_pszTestRootKey = "Software\\SIL\\Fieldworks\\Test";

/***********************************************************************************************
	Functions that replace the default error handling for Assert and Warn/WarnHr.
/**********************************************************************************************/

// This is set to the current test site while a test is running. It gets set back
// to NULL when the test is finisihed.
ISilTestSite * g_ptstsi;

// g_fInException is used as an ugly hack to avoid problems when a destructor (i.e. the
// destructor for a document calls Assert or Warn. Since the throw statements in the
// following two functions cause the stack to unwind, calling Assert or Warn in a destructor
// of an object on the stack sends it back into this function, where SilAssertException is
// not caught. This causes the test harness/manager to die an unpleasant death.
bool g_fInException = false;

void LogError(const char * pszMsg, const char * pszExp, const char * pszFile, int nLine,
	HMODULE hmod)
{
	// This function should not allocate any memory on the heap.
	bool fThrowException = !g_fInException;
	g_fInException = true;

	if (g_ptstsi)
	{
		char szBuffer[2000];
		StrAnsi strMsg = pszMsg;
		StrAnsi strExp = pszExp;
		StrAnsi strFile = pszFile;
		int cch = sprintf_s(szBuffer, strMsg.Chars(), strExp.Chars(), nLine, strFile.Chars());
		::GetModuleFileNameA(hmod, szBuffer + cch, isizeof(szBuffer) - cch - 1);
		strcat_s(szBuffer, ")");
		g_ptstsi->Failure(szBuffer, strlen(szBuffer));
	}
	if (fThrowException)
		throw SilAssertException();
}

void WINAPI PutAssertInLog(const char * pszExp, const char * pszFile, int nLine, HMODULE hmod)
{
	LogError("Assert(%s) occurred in line %d of %s (", pszExp, pszFile, nLine, hmod);
}

void WINAPI PutWarnInLog(const char * pszExp, const char * pszFile, int nLine, HMODULE hmod)
{
	LogError("Warning(%s) occurred in line %d of %s (", pszExp, pszFile, nLine, hmod);
}


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
	SilTestHarness
/**********************************************************************************************/

static GenericFactory g_factTestHarness(
	_T("SIL.SilTestHarness"),
	&CLSID_SilTestHarness,
	_T("Test Harness"),
	_T("Apartment"),
	&SilTestHarness::CreateCom);


/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
SilTestHarness::SilTestHarness()
{
	m_cref = 1;
	m_prgtgi = NULL;
	m_ctgi = 0;
	m_fInitialized = m_fNoPublicChanges = m_fDirtyPublic = m_fDirtyPrivate = false;
	ModuleEntry::ModuleAddRef();
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
SilTestHarness::~SilTestHarness()
{
	if (m_fDirtyPublic)
		SaveGroups(true);
	if (m_fDirtyPrivate)
		SaveGroups(false);
	if (m_prgtgi)
		delete [] m_prgtgi;
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Static method to create a SilTestHarness.
----------------------------------------------------------------------------------------------*/
void SilTestHarness::CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<SilTestHarness> qtst;

	qtst.Attach(NewObj SilTestHarness);
	CheckHr(qtst->QueryInterface(riid, ppv));
}

/*----------------------------------------------------------------------------------------------
	QueryInterface
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::QueryInterface(REFIID riid, void ** ppv)
{
	if (!ppv)
		return WarnHr(E_POINTER);
	AssertPtr(ppv);
	*ppv = NULL;
	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_ISilTestHarness)
		*ppv = static_cast<ISilTestHarness *>(this);
	else
		return E_NOINTERFACE;
	AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	AddRef
----------------------------------------------------------------------------------------------*/
ULONG SilTestHarness::AddRef(void)
{
	return InterlockedIncrement(&m_cref);
}

/*----------------------------------------------------------------------------------------------
	Release
----------------------------------------------------------------------------------------------*/
ULONG SilTestHarness::Release(void)
{
	long lw = InterlockedDecrement(&m_cref);
	if (lw == 0)
	{
		m_cref = 1;
		delete this;
	}
	return lw;
}

/***********************************************************************************************
	ISilTestHarness methods.
/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Read information about the registered tests and test groups from the registry. This must be
	called before any of the other methods. It also reads information about registered VB tests
	from the VBTests.txt file.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::Initialize()
{
	ulong cb;
	RegKey hkeyRoot;
	RegKey hkeyDll;
	RegKey hkeyTest;
	DWORD dwT;
	int cdll = 0;
	int ctst = 0;

	// Open the test root key.
	if (0 != RegOpenKeyExA(HKEY_LOCAL_MACHINE, g_pszTestRootKey, 0, KEY_READ, &hkeyRoot))
		return WarnHr(E_FAIL);

	// Enumerate all the test DLLs stored in the test root key
	try
	{
		TestDllInfo tdi;
		SingleTestInfo sti;
		cb = isizeof(tdi.szProgId);
		while (0 == RegEnumKeyExA(hkeyRoot, cdll, tdi.szProgId, &cb, NULL, NULL, NULL, NULL))
		{
			int ctstb = 0;

			tdi.itstMin = ctst;
			m_vtdi.Push(tdi);

			// Open the ProgID subkey.
			if (0 != RegOpenKeyExA(hkeyRoot, tdi.szProgId, 0, KEY_READ, &hkeyDll))
				return WarnHr(E_FAIL);

			// Enumerate all the tests stored in the ProgID subkey.
			cb = isizeof(sti.szTestName);
			while (0 == RegEnumKeyExA(hkeyDll, ctstb, sti.szTestName, &cb, NULL, NULL, NULL, NULL))
			{
				// Open the TestName subkey
				if (0 != RegOpenKeyExA(hkeyDll, sti.szTestName, 0, KEY_READ, &hkeyTest))
					return WarnHr(E_FAIL);

				cb = isizeof(dwT);
				if (0 != RegQueryValueExA(hkeyTest, "Cookie", NULL, NULL, (BYTE *)&dwT, &cb))
					return WarnHr(E_FAIL);

				sti.itdi = cdll;
				sti.nCookie = (int)dwT;
				m_vsti.Push(sti);

				cb = isizeof(sti.szTestName);
				ctstb++;
			}

			ctst += ctstb;
			m_vtdi[cdll].itstLim = ctst;
			m_vtdi[cdll].ctst = ctstb;

			cdll++;
			cb = isizeof(tdi.szProgId);
		}
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}

	FILE * pfile;
	char szLine[4096];
	try
	{
		if (!fopen_s(&pfile, "VBTests.txt", "r"))
		{
			int ctstb = 0;
			bool fEndOfDll = false;
			int cchLine;
			while (!feof(pfile))
			{
				fgets(szLine, isizeof(szLine), pfile);
				cchLine = strlen(szLine);
				if (!cchLine || *szLine == '\n' || *szLine == ';')
					continue;
				if (szLine[cchLine - 1] == '\n')
					szLine[cchLine - 1] = 0;
				if (*szLine != '\t')
				{
					// This line represents a new DLL
					if (fEndOfDll)
					{
						ctst += ctstb;
						m_vtdi[cdll].itstLim = ctst;
						m_vtdi[cdll].ctst = ctstb;
						cdll++;
					}
					TestDllInfo tdi;
					strncpy_s(tdi.szProgId, szLine, sizeof(tdi.szProgId) - 1);
					tdi.itstMin = ctst;
					m_vtdi.Push(tdi);
					ctstb = 0;
				}
				else
				{
					// This line represents a test within the current DLL
					SingleTestInfo sti;
					char * ptab = strchr(szLine + 1, '\t');
					if (!ptab)
						ThrowHr(WarnHr(E_UNEXPECTED));
					*ptab = 0;
					strncpy_s(sti.szTestName, szLine + 1, sizeof(sti.szTestName) - 1);
					sti.nCookie = strtol(ptab + 1, NULL, 10);
					sti.itdi = cdll;
					m_vsti.Push(sti);
					ctstb++;
					fEndOfDll = true;
				}
			}
			if (fEndOfDll)
			{
				ctst += ctstb;
				m_vtdi[cdll].itstLim = ctst;
				m_vtdi[cdll].ctst = ctstb;
				cdll++;
			}
			fclose(pfile);
		}

		// See if TH_Public.txt is checked out (read-only).
		if (!fopen_s(&pfile, "TH_Public.txt", "r+"))
			fclose(pfile);
		else
			m_fNoPublicChanges = true;

		// Read the public groups from TH_Public.txt and the private groups from TH_Private.txt.
		int itgi = -1;
		for (int ifile = 0; ifile < 2; ifile++)
		{
			errno_t err;
			if (ifile == 0)
				err = fopen_s(&pfile, "TH_Public.txt", "r");
			else
				err = fopen_s(&pfile, "TH_Private.txt", "r");
			if (!err)
			{
				int cchLine;
				while (!feof(pfile))
				{
					fgets(szLine, isizeof(szLine), pfile);
					cchLine = strlen(szLine);
					if (!cchLine || *szLine == '\n' || *szLine == ';')
						continue;
					if (szLine[cchLine - 1] == '\n')
						szLine[cchLine - 1] = 0;
					if (*szLine != '\t')
					{
						if (strncmp(szLine, "#Count: ", 8) == 0)
						{
							if (ifile == 0)
							{
								m_ctgi = strtol(szLine + 8, NULL, 10);
								m_prgtgi = NewObj TestGroupInfo[m_ctgi];
							}
							else
							{
								int ctgiNew = m_ctgi + strtol(szLine + 8, NULL, 10);
								TestGroupInfo * prgtgi = NewObj TestGroupInfo[ctgiNew];

								// Make a copy of the old vector.
								TestGroupInfo * prgtgiOld = m_prgtgi;
								TestGroupInfo * prgtgiNew = prgtgi;
								TestGroupInfo * prgtgiOldStop = prgtgiOld + m_ctgi;
								for ( ; prgtgiOld < prgtgiOldStop; prgtgiOld++, prgtgiNew++)
								{
									prgtgiNew->vitst.Resize(prgtgiOld->vitst.Size());
									for (int itst = prgtgiOld->vitst.Size(); --itst >= 0; )
										prgtgiNew->vitst[itst] = prgtgiOld->vitst[itst];
									strcpy_s(prgtgiNew->szName, prgtgiOld->szName);
									prgtgiNew->fPublic = prgtgiOld->fPublic;
								}
								delete [] m_prgtgi;
								m_prgtgi = prgtgi;
								m_ctgi = ctgiNew;
							}
						}
						else
						{
							// This line represents a new group
							AssertArray(m_prgtgi, m_ctgi);
							itgi++;
							Assert((ulong)itgi < m_ctgi);
							strncpy_s(m_prgtgi[itgi].szName, szLine, isizeof(m_prgtgi[itgi].szName));
							m_prgtgi[itgi].fPublic = ifile == 0;
						}
					}
					else
					{
						// This line represents a test within the current group
						// Find the TestDllInfo for this dll.
						int itdi;
						for (itdi = 0; itdi < cdll; itdi++)
						{
							if (strncmp(m_vtdi[itdi].szProgId, szLine + 1, strlen(m_vtdi[itdi].szProgId)) == 0)
								break;
						}

						// If a DLL isn't registered anymore, ignore it.
						if (itdi < cdll)
						{
							char * psz = szLine + strlen(m_vtdi[itdi].szProgId) + 2;
							int nCookie;
							while ((nCookie = strtol(psz, &psz, 10)) != 0)
							{
								int itst;
								for (itst = 0; itst < ctst; itst++)
								{
									// Try to find the test given its ProgID and cookie
									if (m_vsti[itst].itdi == itdi &&
										m_vsti[itst].nCookie == nCookie)
									{
										break;
									}
								}
								// If an item in the group does not exist (i.e. the test it's
								// pointing to doesn't exist any more), ignore it.
								if (itst < ctst)
									m_prgtgi[itgi].vitst.Push(itst);
								if (*psz++ == 0)
									break;
							}
						}
					}
				}
				fclose(pfile);
			}
		}
	}
	catch (...)
	{
		if (pfile)
			fclose(pfile);
		return WarnHr(E_FAIL);
	}

	m_fInitialized = true;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Get the number of tests in all the test DLLs found in the registry.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::get_TestCount(int * pctst)
{
	AssertPtrN(pctst);
	if (!pctst)
		return WarnHr(E_POINTER);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	*pctst = m_vsti.Size();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Get the name of a test. itst specifies the index of the test within all the
	registered tests.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::get_TestName(int itst, BSTR * pbstrName)
{
	AssertPtrN(pbstrName);
	if (!pbstrName)
		return WarnHr(E_POINTER);
	if ((uint)itst >= (uint)m_vsti.Size())
		return WarnHr(E_INVALIDARG);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	try
	{
		StrUni stuName(m_vsti[itst].szTestName);
		stuName.GetBstr(pbstrName);
		return S_OK;
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}
}

/*----------------------------------------------------------------------------------------------
	Get the total number of registered test DLLs.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::get_DllCount(int * pcdll)
{
	AssertPtrN(pcdll);
	if (!pcdll)
		return WarnHr(E_POINTER);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	*pcdll = m_vtdi.Size();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Get the ProgID of a DLL. idll is the index of the DLL within all the registered DLLs.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::get_DllProgId(int idll, BSTR * pbstrProgId)
{
	AssertPtrN(pbstrProgId);
	if (!pbstrProgId)
		return WarnHr(E_POINTER);
	if ((ulong)idll >= (ulong)m_vtdi.Size())
		return WarnHr(E_INVALIDARG);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	try
	{
		StrUni stuProgId(m_vtdi[idll].szProgId);
		stuProgId.GetBstr(pbstrProgId);
		return S_OK;
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}
}

/*----------------------------------------------------------------------------------------------
	Get the range of tests within a DLL.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::GetTestRangeInDll(int idll, int * pitstMin, int * pitstLim)
{
	AssertPtrN(pitstMin);
	AssertPtrN(pitstLim);
	if (!pitstMin || !pitstLim)
		return WarnHr(E_POINTER);
	if ((ulong)idll >= (ulong)m_vtdi.Size())
		return WarnHr(E_INVALIDARG);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	*pitstMin = m_vtdi[idll].itstMin;
	*pitstLim = m_vtdi[idll].itstLim;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Get the number of groups that were found in the registry.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::get_GroupCount(int * pctstg)
{
	AssertPtrN(pctstg);
	if (!pctstg)
		return WarnHr(E_POINTER);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	*pctstg = (int)m_ctgi;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Get the name of a test group.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::get_GroupName(int itstg, BSTR * pbstrName)
{
	AssertPtrN(pbstrName);
	if (!pbstrName)
		return WarnHr(E_POINTER);
	if ((ulong)itstg >= m_ctgi)
		return WarnHr(E_INVALIDARG);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	try
	{
		StrUni stuName(m_prgtgi[itstg].szName);
		stuName.GetBstr(pbstrName);
		return S_OK;
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}
}

/*----------------------------------------------------------------------------------------------
	Get the size (number of tests) of a group.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::get_GroupSize(int itstg, int * pctst)
{
	AssertPtrN(pctst);
	if (!pctst)
		return WarnHr(E_POINTER);
	if ((ulong)itstg >= m_ctgi)
		return WarnHr(E_INVALIDARG);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	*pctst = m_prgtgi[itstg].vitst.Size();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Get the type of a group (Private or Public).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::get_IsGroupPublic(int itstg, BOOL * pfPublic)
{
	AssertPtrN(pfPublic);
	if (!pfPublic)
		return WarnHr(E_POINTER);
	if ((ulong)itstg >= m_ctgi)
		return WarnHr(E_INVALIDARG);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	*pfPublic = m_prgtgi[itstg].fPublic;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Returns TRUE if public groups can be modified; otherwise returns FALSE.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::get_CanModifyPublicGroups(BOOL * pfCanModify)
{
	AssertPtrN(pfCanModify);
	if (!pfCanModify)
		return WarnHr(E_POINTER);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	*pfCanModify = m_fNoPublicChanges == false;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Get the index of a test given a test group and the index of the test within that group.
	pitstInTotal will contain the index of the test within all registered tests.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::GetGroupTestIndex(int itstg, int itst, int * pitstInTotal)
{
	AssertPtrN(pitstInTotal);
	if (!pitstInTotal)
		return WarnHr(E_POINTER);
	if ((ulong)itstg >= m_ctgi)
		return WarnHr(E_INVALIDARG);
	if ((ulong)itst >= (ulong)m_prgtgi[itstg].vitst.Size())
		return WarnHr(E_INVALIDARG);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	*pitstInTotal = m_prgtgi[itstg].vitst[itst];
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Add a test to a test group. itst is the index of the test within all registered tests.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::AddTestToGroup(int itstg, int itst)
{
	if ((ulong)itstg >= m_ctgi)
		return WarnHr(E_INVALIDARG);
	if ((ulong)itst >= (ulong)m_vsti.Size())
		return WarnHr(E_INVALIDARG);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);
	if (m_prgtgi[itstg].fPublic && m_fNoPublicChanges)
		return E_ACCESSDENIED;

	if (m_prgtgi[itstg].fPublic)
		m_fDirtyPublic = true;
	else
		m_fDirtyPrivate = true;

	// See if it is already in the list of groups.
	for (int itstT = m_prgtgi[itstg].vitst.Size(); --itstT >= 0; )
	{
		if (m_prgtgi[itstg].vitst[itstT] == itst)
			return S_OK;
	}

	// Add it to the list of groups.
	try
	{
		m_prgtgi[itstg].vitst.Push(itst);
		return S_OK;
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}
}

/*----------------------------------------------------------------------------------------------
	Delete a test from a group. itstInGroup is the index of the test within the group, not the
	index of the test within all the tests.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::DeleteTestFromGroup(int itstg, int itstInGroup)
{
	if ((ulong)itstg >= m_ctgi)
		return WarnHr(E_INVALIDARG);
	if ((ulong)itstInGroup >= (ulong)m_prgtgi[itstg].vitst.Size())
		return WarnHr(E_INVALIDARG);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);
	if (m_prgtgi[itstg].fPublic && m_fNoPublicChanges)
		return E_ACCESSDENIED;

	if (m_prgtgi[itstg].fPublic)
		m_fDirtyPublic = true;
	else
		m_fDirtyPrivate = true;

	// Get the index of the test within all of the tests.
	m_prgtgi[itstg].vitst.Delete(itstInGroup);
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Create a new group with the given name. The index of the new group is returned in pitstg.
	If itstg is -1, an empty group will be created. Otherwise, a copy of the group
	indicated by itstg will be created with bstrName as its name.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::CreateNewGroup(BSTR bstrName, int itstg, BOOL fPublic, int * pitstg)
{
	AssertBstr(bstrName);
	AssertPtrN(pitstg);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);
	if (!pitstg)
		return WarnHr(E_POINTER);
	if ((ulong)itstg >= (ulong)m_ctgi && itstg != -1)
		return WarnHr(E_INVALIDARG);
	if (fPublic && m_fNoPublicChanges)
		return E_ACCESSDENIED;

	try
	{
		StrAnsi staName = bstrName;

		// Add it to the list of groups.
		TestGroupInfo * prgtgi = NewObj TestGroupInfo[m_ctgi + 1];

		// Make a copy of the old vector.
		TestGroupInfo * prgtgiOld = m_prgtgi;
		TestGroupInfo * prgtgiNew = prgtgi;
		TestGroupInfo * prgtgiOldStop = prgtgiOld + m_ctgi;
		for ( ; prgtgiOld < prgtgiOldStop; prgtgiOld++, prgtgiNew++)
		{
			prgtgiNew->vitst.Resize(prgtgiOld->vitst.Size());
			for (int itst = prgtgiOld->vitst.Size(); --itst >= 0; )
				prgtgiNew->vitst[itst] = prgtgiOld->vitst[itst];
			strcpy_s(prgtgiNew->szName, prgtgiOld->szName);
			prgtgiNew->fPublic = prgtgiOld->fPublic;
		}
		delete [] m_prgtgi;
		m_prgtgi = prgtgi;
		strncpy_s(m_prgtgi[m_ctgi].szName, staName.Chars(), sizeof(m_prgtgi[m_ctgi].szName));
		m_prgtgi[m_ctgi].fPublic = fPublic;
		*pitstg = m_ctgi++;

		// If requested, copy the items from the given group to the new one.
		if (itstg != -1)
		{
			prgtgiNew = m_prgtgi + m_ctgi - 1;
			prgtgiOld = m_prgtgi + itstg;
			prgtgiNew->vitst.Resize(prgtgiOld->vitst.Size());
			for (int itst = prgtgiOld->vitst.Size(); --itst >= 0; )
				prgtgiNew->vitst[itst] = prgtgiOld->vitst[itst];
		}
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}

	if (fPublic)
		m_fDirtyPublic = true;
	else
		m_fDirtyPrivate = true;

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Delete a test group.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::DeleteGroup(int itstg)
{
	if ((ulong)itstg >= m_ctgi)
		return WarnHr(E_INVALIDARG);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);
	bool fPublic = m_prgtgi[itstg].fPublic;
	if (fPublic && m_fNoPublicChanges)
		return E_ACCESSDENIED;

	try
	{
		// Remove it from the list of groups.
		Assert(m_ctgi > 0);
		TestGroupInfo * prgtgi = NewObj TestGroupInfo[m_ctgi - 1];

		// Make a copy of the old vector (without the deleted item).
		TestGroupInfo * prgtgiOld = m_prgtgi;
		TestGroupInfo * prgtgiNew = prgtgi;
		for (ulong itgi = 0; itgi < m_ctgi; itgi++, prgtgiOld++)
		{
			if (itgi == (ulong)itstg)
				continue;
			prgtgiNew->vitst.Resize(prgtgiOld->vitst.Size());
			for (int itst = prgtgiOld->vitst.Size(); --itst >= 0; )
				prgtgiNew->vitst[itst] = prgtgiOld->vitst[itst];
			strcpy_s(prgtgiNew->szName, prgtgiOld->szName);
			prgtgiNew->fPublic = prgtgiOld->fPublic;
			prgtgiNew++;
		}
		delete [] m_prgtgi;
		m_prgtgi = prgtgi;
		m_ctgi--;
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}

	if (fPublic)
		m_fDirtyPublic = true;
	else
		m_fDirtyPrivate = true;

	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Run the tests given by the array of indexes (prgitst). Log everything to bstrLog.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::RunTests(int * prgitst, int ctst, BSTR bstrLog,
	ISilTestHarnessSite * ptsthsi)
{
	AssertArray(prgitst, ctst);
	AssertBstr(bstrLog);
	AssertPtrN(ptsthsi);
	if (!prgitst || !ptsthsi)
		return WarnHr(E_INVALIDARG);
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);
	if (!ctst)
		return S_OK;

	// Create a test site.
	ISilTestSitePtr qtstsi;
	HRESULT hr;
	if (FAILED(hr = SilTestSite::Create(&qtstsi)))
		return WarnHr(hr);
	if (FAILED(hr = qtstsi->SetLogFile(bstrLog)))
		return WarnHr(hr);

	ISilTestPtr qtst;
	int itstTotal;
	SmartBstr sbstrBsl;
	BOOL fCancel;
	StrAnsiBufPath stabp;
	int cFailure;
	int cMs;
	int cTotalFailures = 0;
	int cTotalTime = 0;
	int nCookie;

	try
	{
		SilTime stim;
		stim.SetToCurTime();
		stabp.Format("*******************************************************************%n"
			"Beginning tests at %d:%d:%d on %d-%d-%d.%n"
			"*******************************************************************%n%n",
			stim.Hour(), stim.Minute(), stim.Second(), stim.Month(), stim.WeekDay(), stim.Year());
		qtstsi->Log(stabp.Chars(), stabp.Length());

		for (int itst = 0; itst < ctst; itst++, prgitst++)
		{
			itstTotal = *prgitst;
			if ((uint)itstTotal >= (uint)m_vsti.Size())
				continue; // Skip this invalid test index

			StrUni stuProgId(m_vtdi[m_vsti[itstTotal].itdi].szProgId);
			StrUni stuName(m_vsti[itstTotal].szTestName);

			if (FAILED(hr = GetTestFromIndex(itstTotal, &qtst, &nCookie)))
			{
				WarnHr(hr);
				if (hr == REGDB_E_CLASSNOTREG)
				{
					stabp.Format("The dll (ProgId = %S) containing \"%S\" has not been registered "
						"correctly.", stuProgId.Chars(), stuName.Chars());
					qtstsi->Failure(stabp.Chars(), stabp.Length());
				}
			}
			if (SUCCEEDED(hr) &&
				SUCCEEDED(hr = qtst->Initialize(qtstsi)) &&
				SUCCEEDED(hr = qtst->BaselineFile(nCookie, &sbstrBsl)))
			{
				hr = qtstsi->SetBaselineFile(sbstrBsl);
			}
			cFailure = cMs = 0;
			if (SUCCEEDED(hr))
			{
				qtstsi->BeginTest(itstTotal, stuProgId.Bstr(), stuName.Bstr());

				// Override normal assert and warning behavior to report problems.
				g_ptstsi = qtstsi;
				Pfn_Assert pfnOldAssert = SetAssertProc(&PutAssertInLog);
				Pfn_Assert pfnOldWarn = SetWarnProc(&PutWarnInLog);
				g_fInException = false;
				try
				{
					hr = qtst->RunTest(nCookie);
				}
				// If it is an SilAssertException it has already been logged.
				catch (SilAssertException)
				{
					hr = E_FAIL;
				}
				// Otherwise log it now.
				catch (std::exception * e)
				{
					StrAnsi sta;
					sta.Format("Test %S caused an exception (%s)", stuName.Chars(), e->what());
					qtstsi->Failure(sta.Chars(), sta.Length());
					hr = E_FAIL;
				}
				catch (std::exception & e)
				{
					StrAnsi sta;
					sta.Format("Test %S caused an exception (%s)", stuName.Chars(), e.what());
					qtstsi->Failure(sta.Chars(), sta.Length());
					hr = E_FAIL;
				}
				catch (...)
				{
					StrAnsi sta;
					sta.Format("Test %S caused a non-standard exception.", stuName.Chars());
					qtstsi->Failure(sta.Chars(), sta.Length());
					hr = E_FAIL;
				}
				g_fInException = true;

				// Restore previous assert and warning behavior.
				if (pfnOldAssert)
					SetAssertProc(pfnOldAssert);
				if (pfnOldWarn)
					SetWarnProc(pfnOldWarn);

				qtst->Close(); // Ignore failure.

				qtstsi->EndTest(itstTotal, stuProgId.Bstr(), stuName.Bstr(), hr);
				qtstsi->get_FailureCount(&cFailure);
				qtstsi->get_MsToRun(&cMs);
			}
			else
			{
				cFailure++;
			}
			g_ptstsi = NULL;

			ptsthsi->TestResult(itstTotal, cFailure, hr, cMs, &fCancel);

			cTotalFailures += cFailure;
			cTotalTime += cMs;

			if (fCancel)
				break;
		}

		stabp.Format(
			"%n*******************************************************************%n"
			"%d test(s) were completed with %d error(s) in %d ms.%n"
			"*******************************************************************%n",
			ctst, cTotalFailures, cTotalTime);
		qtstsi->Log(stabp.Chars(), stabp.Length());

		// Release all the ISilTest COM objects.
		for (int itdi = m_vtdi.Size(); --itdi >= 0; )
			m_vtdi[itdi].qtst.Clear();
		qtst.Clear();
		CoFreeUnusedLibraries();
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}

	return fCancel ? S_FALSE : S_OK;
}

/*----------------------------------------------------------------------------------------------
	Run a single test from the DLL specified by bstrProgId. Case does not matter in bstrProgId.
	If itstInDll is -1, all tests in the DLL are run.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::RunSingle(BSTR bstrProgId, int itstInDll, BSTR bstrLog,
	ISilTestHarnessSite * ptsthsi)
{
	AssertBstr(bstrProgId);
	AssertBstr(bstrLog);
	AssertPtrN(ptsthsi);
	if (!ptsthsi)
		return E_INVALIDARG;
	if (itstInDll != -1 && (ulong)itstInDll >= (ulong)m_vsti.Size())
		return E_INVALIDARG;
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	// Get the index of the DLL corresponding to bstrProgId
	try
	{
		StrAnsi staProgId = bstrProgId;
		int cdll = m_vtdi.Size();
		int idll;
		for (idll = 0; idll < cdll; idll++)
		{
			if (_stricmp(m_vtdi[idll].szProgId, staProgId.Chars()) == 0)
				break;
		}
		if (idll >= cdll)
			return WarnHr(E_INVALIDARG);

		if (itstInDll != -1)
		{
			int itst = m_vtdi[idll].itstMin + itstInDll;
			return RunTests(&itst, 1, bstrLog, ptsthsi);
		}

		// Add all the tests that belong to the DLL to a vector
		Vector<int> vitst;
		for (int itst = m_vtdi[idll].itstMin; itst < m_vtdi[idll].itstLim; itst++)
			vitst.Push(itst);
		return RunTests(&vitst[0], vitst.Size(), bstrLog, ptsthsi);
	}
	catch (...)
	{
		return WarnHr(E_FAIL);
	}
}

/*----------------------------------------------------------------------------------------------
	Run all the tests within a group.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilTestHarness::RunGroup(int itstg, BSTR bstrLog, ISilTestHarnessSite * ptsthsi)
{
	AssertPtrN(ptsthsi);
	if (!ptsthsi)
		return E_POINTER;
	if ((ulong)itstg >= m_ctgi)
		return E_INVALIDARG;
	if (!m_fInitialized)
		return WarnHr(E_UNEXPECTED);

	return RunTests(&m_prgtgi[itstg].vitst[0], m_prgtgi[itstg].vitst.Size(), bstrLog, ptsthsi);
}

/*----------------------------------------------------------------------------------------------
	Return a pointer to the SilTest interface and the cookie of the test in that interface.
	itst is an index of the test within all the registered tests.
----------------------------------------------------------------------------------------------*/
HRESULT SilTestHarness::GetTestFromIndex(int itst, ISilTest ** pptst, int * pnCookie)
{
	AssertPtr(pptst);
	AssertPtr(pnCookie);
	Assert((uint)itst < (uint)m_vsti.Size());
	*pptst = NULL;

	int idll = m_vsti[itst].itdi;
	*pnCookie = m_vsti[itst].nCookie;
	*pptst = m_vtdi[idll].qtst;
	if (!*pptst)
	{
		// The test has not been created yet, so create an instance of it now.
		try
		{
			CLSID clsid;
			StrUni stuProgId(m_vtdi[idll].szProgId);
			CLSIDFromProgID(stuProgId.Chars(), &clsid);
			ISilTestPtr qtst;
			HRESULT hr = CoCreateInstance(clsid, NULL, CLSCTX_ALL, IID_ISilTest, (void **)&qtst);
			if (FAILED(hr))
				return WarnHr(hr);
			*pptst = m_vtdi[idll].qtst = qtst;
		}
		catch (...)
		{
			return WarnHr(E_FAIL);
		}
	}
	(*pptst)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void SilTestHarness::SaveGroups(bool fPublic)
{
	FILE * pfile;
	errno_t err;
	if (fPublic)
		err = fopen_s(&pfile, "TH_Public.txt", "w");
	else
		err = fopen_s(&pfile, "TH_Private.txt", "w");
	if (err)
	{
		if (fPublic)
			::MessageBoxA(NULL, "The public group settings could not be saved.", "Error",
				MB_OK | MB_ICONWARNING);
		else
			::MessageBoxA(NULL, "The private group settings could not be saved.", "Error",
				MB_OK | MB_ICONWARNING);
		return;
	}

	// Get the number of test groups to write out to the file.
	int ctstg = 0;
	ulong itgi;
	for (itgi = 0; itgi < m_ctgi; itgi++)
	{
		if (m_prgtgi[itgi].fPublic == fPublic)
			ctstg++;
	}

	// Write the banner lines to the file.
	fprintf(pfile, "; ---------------------------------------------------------------\n");
	fprintf(pfile, "; | WARNING: This is a generated file, so don't edit it.        |\n");
	fprintf(pfile, "; ---------------------------------------------------------------\n\n");
	fprintf(pfile, "; The next line contains the number of test groups in this file.\n");
	fprintf(pfile, "#Count: %d\n\n", ctstg);

	int cdll = m_vtdi.Size();
	for (itgi = 0; itgi < m_ctgi; itgi++)
	{
		if (m_prgtgi[itgi].fPublic == fPublic)
		{
			// Write the information for this test to the file.
			fprintf(pfile, "%s\n", m_prgtgi[itgi].szName);
			int ctst = m_prgtgi[itgi].vitst.Size();
			for (int idll = 0; idll < cdll; idll++)
			{
				int ctstInDll = 0;
				for (int itst = 0; itst < ctst; itst++)
				{
					if (m_vsti[m_prgtgi[itgi].vitst[itst]].itdi == idll)
					{
						// This test belongs in the dll given by idll.
						int nCookie = m_vsti[m_prgtgi[itgi].vitst[itst]].nCookie;
						if (ctstInDll++)
						{
							// This is not the first test for this dll.
							fprintf(pfile, " %d", nCookie);
						}
						else
						{
							// This is the first test for this dll.
							fprintf(pfile, "\t%s %d",
								m_vtdi[m_vsti[m_prgtgi[itgi].vitst[itst]].itdi].szProgId,
								nCookie);
						}
					}
				}
				if (ctstInDll)
					fprintf(pfile, "\n");
			}
			fprintf(pfile, "\n");
		}
	}
}


/***********************************************************************************************
	ISilTestSite
/**********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
SilTestSite::SilTestSite()
{
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

		::GetModuleFileNameA(ModuleEntry::GetModuleHandle(), qtstsi->m_szLogPath,
			isizeof(qtstsi->m_szLogPath));
		strcat_s(qtstsi->m_szLogPath, "\\..\\..\\..\\TestLog\\");
		::GetFullPathNameA(qtstsi->m_szLogPath, isizeof(qtstsi->m_szLogPath),
			qtstsi->m_szLogPath, NULL);
		strcpy_s(qtstsi->m_szBaselinePath, qtstsi->m_szLogPath);
		strcat_s(qtstsi->m_szLogPath, "Log\\");
		strcat_s(qtstsi->m_szBaselinePath, "Baseline\\");
		::CreateDirectoryA(qtstsi->m_szLogPath, NULL);

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

		if (fopen_s(&m_pfileLog, staLogFile.Chars(), "wb"))
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

	if (fopen_s(&m_pfileBsl, m_staBslNew, "wb"))
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
		return Failure(pszError, strlen(pszError));
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
	FILE * pfileBslNew;
	if (fopen_s(&pfileBslNew, m_staBslNew, "rb"))
	{
		char * psz = "Could not open the new baseline file.\r\n";
		Failure(psz, strlen(psz));
		return WarnHr(STG_E_PATHNOTFOUND);
	}
	FILE * pfileBslOld;
	if (fopen_s(&pfileBslOld, m_staBslOld, "rb"))
	{
		char * psz = "Could not open the old baseline file.\r\n";
		Failure(psz, strlen(psz));
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
			Failure(psz, strlen(psz));
			hr = S_FALSE;
			break;
		}
		fgets(szLineOld, isizeof(szLineOld), pfileBslOld);
		fgets(szLineNew, isizeof(szLineNew), pfileBslNew);
		cch = strlen(szLineOld);
		iLine++;
		if ((unsigned)cch != strlen(szLineNew) ||
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
		Failure(psz, strlen(psz));
		hr = S_FALSE;
	}
	if (hr == S_OK)
	{
		char * psz = "The old and new baseline files are identical.\r\n";
		Log(psz, strlen(psz));
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


/***********************************************************************************************
	SilDebugProcs
/**********************************************************************************************/

static GenericFactory g_factSilDebugProcs(
	_T("SIL.DebugProcs"),
	&CLSID_SilDebugProcs,
	_T("SIL Debug Procedures"),
	_T("Apartment"),
	&SilDebugProcs::CreateCom);


/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
SilDebugProcs::SilDebugProcs()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
SilDebugProcs::~SilDebugProcs()
{
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Static method to create a SilDebugProcs.
----------------------------------------------------------------------------------------------*/
void SilDebugProcs::CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<SilDebugProcs> qtstdp;

	qtstdp.Attach(NewObj SilDebugProcs);
	CheckHr(qtstdp->QueryInterface(riid, ppv));
}

/*----------------------------------------------------------------------------------------------
	QueryInterface
----------------------------------------------------------------------------------------------*/
STDMETHODIMP SilDebugProcs::QueryInterface(REFIID riid, void ** ppv)
{
	if (!ppv)
		return WarnHr(E_POINTER);
	AssertPtr(ppv);
	*ppv = NULL;
	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_ISilDebugProcs)
		*ppv = static_cast<ISilDebugProcs *>(this);
	else
		return E_NOINTERFACE;
	AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	AddRef
----------------------------------------------------------------------------------------------*/
ULONG SilDebugProcs::AddRef(void)
{
	return InterlockedIncrement(&m_cref);
}

/*----------------------------------------------------------------------------------------------
	Release
----------------------------------------------------------------------------------------------*/
ULONG SilDebugProcs::Release(void)
{
	long lw = InterlockedDecrement(&m_cref);
	if (lw == 0)
	{
		m_cref = 1;
		delete this;
	}
	return lw;
}

/***********************************************************************************************
	ISilDebugProcs methods.

	These basically just call the corresponding global assert/warn functions defined in debug.h.
/**********************************************************************************************/

STDMETHODIMP SilDebugProcs::WarnProc(BSTR bstrExplanation, BSTR bstrSource)
{
	StrAnsi staExplanation = bstrExplanation;
	StrAnsi staSource = bstrSource;
	::WarnProc(staExplanation.Chars(), staSource.Chars(), 0, FALSE, 0);
	return S_OK;
}

STDMETHODIMP SilDebugProcs::AssertProc(BSTR bstrExplanation, BSTR bstrSource)
{
	StrAnsi staExplanation = bstrExplanation;
	StrAnsi staSource = bstrSource;
	::AssertProc(staExplanation.Chars(), staSource.Chars(), 0, FALSE, 0);
	return S_OK;
}

STDMETHODIMP SilDebugProcs::HideWarnings(BOOL fHide)
{
	::HideWarnings(fHide);
	return S_OK;
}

STDMETHODIMP SilDebugProcs::HideAsserts(BOOL fHide)
{
	::HideAsserts(fHide);
	return S_OK;
}

STDMETHODIMP SilDebugProcs::HideErrors(BOOL fHide)
{
	::HideErrors(fHide);
	return S_OK;
}
