/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestMetaDataCache.h
Responsibility:
Last reviewed:

	Global header for unit testing the FwMetaDataCache class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTMETADATACACHE_H_INCLUDED
#define TESTMETADATACACHE_H_INCLUDED

#pragma once

#include "testDbAccess.h"

namespace TestDbAccess
{
	class TestMetaDataCache : public unitpp::suite
	{
		IFwMetaDataCachePtr m_qmdc;

		void testAbstractClasses()
		{
			ComBool fIsAbstract;
			m_qmdc->GetAbstract(0, &fIsAbstract); // kclidCmObject
			unitpp::assert_true("CmObject not abstract.", fIsAbstract);
			m_qmdc->GetAbstract(7, &fIsAbstract); // kclidCmPossibility
			unitpp::assert_true("CmPossibility is abstract", !fIsAbstract);
		}

	public:
		TestMetaDataCache();

		virtual void Setup()
		{
			StrUni stuSvrName(L".\\SILFW");
			StrUni stuDbName(L"TestLangProj");
			IOleDbEncapPtr qode;
			OleDbEncap::CreateCom(NULL, IID_IOleDbEncap, (void **)&qode);
			CheckHr(qode->Init(stuSvrName.Bstr(), stuDbName.Bstr(), NULL, koltNone, 0));
			FwMetaDataCache::CreateCom(NULL, IID_IFwMetaDataCache, (void **)&m_qmdc);
			CheckHr(m_qmdc->Init(qode));
		}
		virtual void Teardown()
		{
			m_qmdc.Clear();
		}
	};
}

#endif /*TESTMETADATACACHE_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkdba-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
