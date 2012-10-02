/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testFwCellar.cpp
Responsibility:
Last reviewed:

	Global initialization/cleanup for unit testing the FwCellar DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "testFwCellar.h"

namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
		ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
		::OleInitialize(NULL);
		TestFwCellar::SetTestDir();
	}
	void GlobalTeardown()
	{
		::OleUninitialize();
		ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
	}
}

namespace TestFwCellar
{
	StrApp g_strTestDir;

	/*------------------------------------------------------------------------------------------
		Set the root directory for where things are located, which should be the directory
		where this file is located.
	------------------------------------------------------------------------------------------*/
	void SetTestDir()
	{
		if (g_strTestDir.Length())
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
				str.Append(_T("\\Src\\Cellar\\Test\\"));
				g_strTestDir.Assign(str);
			}
			RegCloseKey(hk);
		}
		if (!g_strTestDir.Length())
			g_strTestDir.Assign("C:\\FW\\Src\\Cellar\\Test\\");
	}
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkcel-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
