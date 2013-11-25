/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: testGeneric.cpp
Responsibility: Dan Hinton (and possibly Alistair Imrie, if Dan has died or something)
Last reviewed:

	Global initialization/cleanup for unit testing the Language DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "testGenericLib.h"
#include "RedirectHKCU.h"

namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
#ifdef WIN32
		ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
#endif
		::OleInitialize(NULL);
		RedirectRegistry();
		StrUtil::InitIcuDataDir();
	}
	void GlobalTeardown()
	{
#ifdef WIN32
		ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
#endif
		::OleUninitialize();
	}
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkGenLib-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
