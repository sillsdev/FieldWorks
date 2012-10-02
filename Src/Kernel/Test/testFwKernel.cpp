/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testFwKernel.cpp
Responsibility:
Last reviewed:

	Global initialization/cleanup for unit testing the FwKernel DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "testFwKernel.h"

namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
		ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
		::OleInitialize(NULL);
	}
	void GlobalTeardown()
	{
		ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
		::OleUninitialize();
	}
}
