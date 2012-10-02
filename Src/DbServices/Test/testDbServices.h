/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testDbServices.h
Responsibility:
Last reviewed:

	Global header for unit testing the DbServices DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTDBSERVICES_H_INCLUDED
#define TESTDBSERVICES_H_INCLUDED

#pragma once
#include "Main.h"
#include <unit++.h>

namespace TestDbServices
{
	extern StrApp g_strRootDir;
	extern void SetRootDir();
	extern void GetLocalServer(BSTR * pbstr);

	class MockBackupDelegates : public IBackupDelegates
	{
	public:
		MockBackupDelegates();

		STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
		STDMETHOD_(ULONG, AddRef)(void);
		STDMETHOD_(ULONG, Release)(void);
		STDMETHOD(GetLocalServer_Bkupd)(BSTR * pbstrSvrName);
		STDMETHOD(GetLogPointer_Bkupd)(IStream** ppfist);
		STDMETHOD(SaveAllData_Bkupd)(const OLECHAR * pszServer, const OLECHAR * pszDbName);
		STDMETHOD(CloseDbAndWindows_Bkupd)(const OLECHAR * pszSvrName,
			const OLECHAR * pszDbName, ComBool fOkToClose, ComBool * pfWindowsClosed);
		STDMETHOD(IncExportedObjects_Bkupd)();
		STDMETHOD(DecExportedObjects_Bkupd)();
		STDMETHOD(CheckDbVerCompatibility_Bkupd)(const OLECHAR * pszSvrName,
			const OLECHAR * pszDbName, ComBool * pfCompatible);
		STDMETHOD(ReopenDbAndOneWindow_Bkupd)(const OLECHAR * pszSvrName,
			const OLECHAR * pszDbName);
		STDMETHOD(GetDefaultBackupDirectory)(BSTR * pbstrDefBackupDir);
		STDMETHOD(IsDbOpen_Bkupd)(const OLECHAR * pszServer, const OLECHAR * pszDbName,
			ComBool * pfIsOpen);

		void AddOpenDb(const OLECHAR * pszDbName);
		void ClearOpenDbs();
	protected:
		long m_cref;
		Vector<StrUni> m_vstuOpenDbs;
	};
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkDbSvcs-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*TESTDBSERVICES_H_INCLUDED*/
