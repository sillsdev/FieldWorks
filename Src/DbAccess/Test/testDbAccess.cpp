/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testDbAccess.cpp
Responsibility:
Last reviewed:

	Global initialization/cleanup for unit testing the DbAccess DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "testDbAccess.h"

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
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkdba-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
