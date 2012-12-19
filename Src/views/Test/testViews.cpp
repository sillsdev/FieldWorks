/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: testViews.cpp
Responsibility:
Last reviewed:

	Global initialization/cleanup for unit testing the Views DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef WIN32
#define INITGUID
#endif
#include "testViews.h"
#include "RedirectHKCU.h"

#ifndef WIN32
// These define GUIDs that we need to define globally somewhere
#include "TestVwTxtSrc.h"
#include "TestLayoutPage.h"
#endif

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
		::OleUninitialize();
#ifdef WIN32
		ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
#endif
	}
}

namespace TestViews
{
	ILgWritingSystemFactoryPtr g_qwsf;
	int g_wsEng = 0;
	int g_wsFrn = 0;
	int g_wsGer = 0;

	// Create a dummy writing system factory with English and French.
	void CreateTestWritingSystemFactory()
	{
		StrUni stuWs;
		ILgWritingSystemPtr qws;
		g_qwsf.Attach(NewObj MockLgWritingSystemFactory);

		// Add a writing system for English.
		stuWs.Assign(L"en");
		CheckHr(g_qwsf->get_Engine(stuWs.Bstr(), &qws));
		StrUni stuTimesNewRoman(L"Times New Roman");
		qws->put_DefaultFontName(stuTimesNewRoman.Bstr());
		CheckHr(qws->get_Handle(&g_wsEng));
		CheckHr(g_qwsf->put_UserWs(g_wsEng));

		// Add a writing system for French.
		stuWs.Assign(L"fr");
		CheckHr(g_qwsf->get_Engine(stuWs.Bstr(), &qws));
		qws->put_DefaultFontName(stuTimesNewRoman.Bstr());
		CheckHr(qws->get_Handle(&g_wsFrn));

		// Add a writing system for German.
		stuWs.Assign(L"de");
		CheckHr(g_qwsf->get_Engine(stuWs.Bstr(), &qws));
		qws->put_DefaultFontName(stuTimesNewRoman.Bstr());
		CheckHr(qws->get_Handle(&g_wsGer));
	}

	// Free the dummy writing system factory.
	void CloseTestWritingSystemFactory()
	{
		if (g_qwsf)
		{
			g_qwsf.Clear();
		}
	}
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
