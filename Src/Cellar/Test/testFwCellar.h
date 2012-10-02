/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testFwCellar.h
Responsibility:
Last reviewed:

	Global header for unit testing the FwCellar DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTFWCELLAR_H_INCLUDED
#define TESTFWCELLAR_H_INCLUDED

#pragma once
#include "Main.h"
//#include "DbAccessTlb.h"
#include <unit++.h>

namespace TestFwCellar
{
	extern StrApp g_strTestDir;
	void SetTestDir();
}


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkcel-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*TESTFWCELLAR_H_INCLUDED*/
