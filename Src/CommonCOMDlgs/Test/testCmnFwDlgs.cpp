/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: testCmnFwDlgs.cpp
Responsibility:
Last reviewed:

	Global initialization/cleanup for unit testing the CmnFwDlgs DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "testCmnFwDlgs.h"

namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
		ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
		::OleInitialize(NULL);
	}
	void GlobalTeardown()
	{
		::OleUninitialize();
		ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
	}
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkComFWDlgs-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
