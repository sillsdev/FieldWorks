/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: testFwKernel.cpp
Responsibility:
Last reviewed:

	Global initialization/cleanup for unit testing the FwKernel DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "testFwKernel.h"
#include "RedirectHKCU.h"

// NOTE: namespace has to be unitpp, otherwise unit++ won't recognize setup/teardown
// methods
namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
#ifdef WIN32
		ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
#endif
		CheckHr(::OleInitialize(NULL));
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

namespace TestFwKernel
{
	int g_wsEng = 0;
	int g_wsTest = 0;
	int g_wsTest2 = 0;
}
