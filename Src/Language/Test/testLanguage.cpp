/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: testLanguage.cpp
Responsibility:
Last reviewed:

	Global initialization/cleanup for unit testing the Language DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "testLanguage.h"
#include "RedirectHKCU.h"

#ifndef WIN32
#include <fstream>
#include <WinBase.h>
#endif

namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
#ifdef WIN32
		ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
#endif
		CheckHr(::OleInitialize(NULL));
		RedirectRegistry();
		StrUtil::InitIcuDataDir();	// needed for the normalize routines (ICU)
	}
	void GlobalTeardown()
	{
#ifdef WIN32
		ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
#endif
		::OleUninitialize();
	}
}

namespace TestLanguage
{
	int g_wsEng = 0;
	int g_wsTest = 0;
	int g_wsTest2 = 0;
}

#include "Vector_i.cpp"
template class Vector<bool>;

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
