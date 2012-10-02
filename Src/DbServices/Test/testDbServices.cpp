/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testDbServices.cpp
Responsibility:
Last reviewed:

	Global initialization/cleanup for unit testing the DbServices DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "testDbServices.h"

namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
		ModuleEntry::DllMain(::GetModuleHandle(NULL), DLL_PROCESS_ATTACH);
		::OleInitialize(NULL);
	}
	void GlobalTeardown()
	{
		::OleUninitialize();
		ModuleEntry::DllMain(::GetModuleHandle(NULL), DLL_PROCESS_DETACH);
	}
}

namespace TestDbServices
{
	StrApp g_strRootDir;

	/*------------------------------------------------------------------------------------------
		Set the root directory for where things are located, which should be the directory
		where this file is located.
	------------------------------------------------------------------------------------------*/
	void SetRootDir()
	{
		if (g_strRootDir.Length())
			return;		// Set this only once!

		// Get the path to the template files.
		HKEY hk;
		if (::RegOpenKeyEx(HKEY_LOCAL_MACHINE, _T("Software\\SIL\\FieldWorks"), 0,
				KEY_QUERY_VALUE, &hk) == ERROR_SUCCESS)
		{
			achar rgch[MAX_PATH];
			DWORD cb = isizeof(rgch);
			DWORD dwT;
			if (::RegQueryValueEx(hk, _T("RootCodeDir"), NULL, &dwT, (BYTE *)rgch, &cb)
				== ERROR_SUCCESS)
			{
				Assert(dwT == REG_SZ);
				StrApp str(rgch);
				int ich = str.FindStrCI(_T("\\distfiles"));
				if (ich >= 0)
					str.Replace(ich, str.Length(), _T(""));
				ich = str.FindCh('\\', str.Length() - 1);
				if (ich >= 0)
					str.Replace(ich, str.Length(), _T(""));
				str.Append(_T("\\Src\\DbServices\\Test\\"));
				g_strRootDir.Assign(str);
			}
			RegCloseKey(hk);
		}
		if (!g_strRootDir.Length())
			g_strRootDir.Assign("C:\\FW");
	}

	/*------------------------------------------------------------------------------------------
		Calculate the local server name.
	------------------------------------------------------------------------------------------*/
	void GetLocalServer(BSTR * pbstr)
	{
		achar rgch[MAX_COMPUTERNAME_LENGTH + 1];
		ulong cch = isizeof(rgch);
		::GetComputerName(rgch, &cch);
		StrUni stuServer;
		stuServer.Format(L"%s\\SILFW", rgch);
		stuServer.GetBstr(pbstr);
	}

	STDMETHODIMP MockBackupDelegates::QueryInterface(REFIID iid, void ** ppv)
	{
		*ppv = NULL;
		if (iid == IID_IUnknown)
			*ppv = static_cast<IUnknown *>(this);
		else if (iid == IID_IBackupDelegates)
			*ppv = static_cast<IBackupDelegates *>(this);
		else
			return E_NOINTERFACE;
		return S_OK;
	}

	STDMETHODIMP_(ULONG) MockBackupDelegates::AddRef(void)
	{
		::InterlockedIncrement(&m_cref);
		return m_cref;
	}

	STDMETHODIMP_(ULONG) MockBackupDelegates::Release(void)
	{
		ulong cref = ::InterlockedDecrement(&m_cref);
		if (!cref)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	STDMETHODIMP MockBackupDelegates::GetLocalServer_Bkupd(BSTR * pbstrSvrName)
	{
		GetLocalServer(pbstrSvrName);
		return S_OK;
	}
	STDMETHODIMP MockBackupDelegates::GetLogPointer_Bkupd(IStream** ppfist)
	{
		return S_OK;
	}
	STDMETHODIMP MockBackupDelegates::SaveAllData_Bkupd(const OLECHAR * pszServer,
		const OLECHAR * pszDbName)
	{
		return S_OK;
	}
	STDMETHODIMP MockBackupDelegates::CloseDbAndWindows_Bkupd(const OLECHAR * pszSvrName,
		const OLECHAR * pszDbName, ComBool fOkToClose, ComBool * pfWindowsClosed)
	{
		*pfWindowsClosed = FALSE;
		return S_OK;
	}
	STDMETHODIMP MockBackupDelegates::IncExportedObjects_Bkupd()
	{
		return S_OK;
	}
	STDMETHODIMP MockBackupDelegates::DecExportedObjects_Bkupd()
	{
		return S_OK;
	}
	STDMETHODIMP MockBackupDelegates::CheckDbVerCompatibility_Bkupd(const OLECHAR * pszSvrName,
		const OLECHAR * pszDbName, ComBool * pfCompatible)
	{
		*pfCompatible = TRUE;
		return S_OK;
	}
	STDMETHODIMP MockBackupDelegates::ReopenDbAndOneWindow_Bkupd(const OLECHAR * pszSvrName,
		const OLECHAR * pszDbName)
	{
		return S_OK;
	}
	STDMETHODIMP MockBackupDelegates::GetDefaultBackupDirectory(BSTR * pbstrDefBackupDir)
	{
		return S_OK;
	}

	STDMETHODIMP MockBackupDelegates::IsDbOpen_Bkupd(const OLECHAR * pszServer,
		const OLECHAR * pszDbName, ComBool * pfIsOpen)
	{
		*pfIsOpen = FALSE;
		for (int i = 0; i < m_vstuOpenDbs.Size(); ++i)
		{
			if (m_vstuOpenDbs[i] == pszDbName)
			{
				*pfIsOpen = TRUE;
				break;
			}
		}
		return S_OK;
	}

	MockBackupDelegates::MockBackupDelegates()
	{
		m_cref = 1;
	}

	void MockBackupDelegates::AddOpenDb(const OLECHAR * pszDbName)
	{
		StrUni stu(pszDbName);
		m_vstuOpenDbs.Push(stu);
	}

	void MockBackupDelegates::ClearOpenDbs()
	{
		m_vstuOpenDbs.Clear();
	}
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkDbSvcs-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
