/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testGeneric.cpp
Responsibility: Dan Hinton (and possibly Alistair Imrie, if Dan has died or something)
Last reviewed:

	Global initialization/cleanup for unit testing the Language DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "testGenericLib.h"

namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
		ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
		::OleInitialize(NULL);
		StrUtil::InitIcuDataDir();
	}
	void GlobalTeardown()
	{
		ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
		::OleUninitialize();
	}
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkGenLib-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
