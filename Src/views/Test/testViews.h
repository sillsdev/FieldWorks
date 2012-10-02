/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testViews.h
Responsibility:
Last reviewed:

	Global header for unit testing the Views DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTVIEWS_H_INCLUDED
#define TESTVIEWS_H_INCLUDED

#pragma once
#include "Main.h"

namespace TestViews
{
	extern ILgWritingSystemFactoryPtr g_qwsf;
	extern int g_wsEng;
	extern int g_wsFrn;
	extern int g_wsGer;

	void CreateTestWritingSystemFactory();
	void CloseTestWritingSystemFactory();
};

#include <unit++.h>
#include "DummyRootSite.h"
#include "DummyBaseVc.h"

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*TESTVIEWS_H_INCLUDED*/
