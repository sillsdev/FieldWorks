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

#ifdef WIN32
#define GUID_TYPE GUID
#else
#define GUID_TYPE PlainGUID
#endif
#define LOCAL_DEFINE_GUID(name, l, w1, w2, b1, b2, b3, b4, b5, b6, b7, b8) \
		EXTERN_C const GUID_TYPE name \
				= { l, w1, w2, {b1, b2,  b3,  b4,  b5,  b6,  b7,  b8 } }

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
#include "DummyRootsite.h"
#include "DummyBaseVc.h"
#include "MockLgWritingSystemFactory.h"

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*TESTVIEWS_H_INCLUDED*/
