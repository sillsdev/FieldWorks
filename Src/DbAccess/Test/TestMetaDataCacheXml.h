/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestMetaDataCacheXml.h
Responsibility:
Last reviewed:

	Global header for unit testing the FwMetaDataCache class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTMETADATACACHEXML_H_INCLUDED
#define TESTMETADATACACHEXML_H_INCLUDED

#pragma once

#include "testDbAccess.h"

namespace TestDbAccess
{
	class TestMetaDataCacheXml : public unitpp::suite
	{
		IFwMetaDataCachePtr m_qmdc;

		void CheckOccurrence(ULONG *prgflid, int cflid, int target)
		{
			for(int i = 0; i < cflid; i++)
				if (prgflid[i] == (ULONG) target)
					return;
			unitpp::assert_true("expected flid not found", false);
		}

		void testAbstractClasses()
		{
			StrUni stuPath = DirectoryFinder::FwRootCodeDir(); // assume this is something like ...fw\distfiles
			stuPath.Append(L"\\..\\Src\\DbAccess\\Test\\SampleCm.xml");
			FwMetaDataCache::CreateCom(NULL, IID_IFwMetaDataCache, (void **)&m_qmdc);
			m_qmdc->InitXml(stuPath.Bstr(), true);

			ComBool fIsAbstract;
			m_qmdc->GetAbstract(0, &fIsAbstract); // kclidCmObject
			unitpp::assert_true("CmObject not abstract.", fIsAbstract);
			m_qmdc->GetAbstract(7, &fIsAbstract); // kclidCmPossibility
			unitpp::assert_true("CmPossibility is abstract", !fIsAbstract);
			SmartBstr bstr;
			m_qmdc->GetClassName(7, &bstr);
			unitpp::assert_true("Got name of CmPossibility", wcscmp(L"CmPossibility", bstr.Chars()) == 0);
			ULONG clid;
			m_qmdc->GetBaseClsId(7, &clid);
			unitpp::assert_eq("got base of CmPossibility", (ULONG)0, clid);
			m_qmdc->GetBaseClsId(5, &clid);
			unitpp::assert_eq("got base of LexMajorEntry", (ULONG)2, clid);

			ULONG rgflid[50];
			int cflid;
			m_qmdc->GetFields(5, false, kgrfcptAll, 50, rgflid, &cflid);
			unitpp::assert_eq("got one field on LexMajorEntry", 1, cflid);
			unitpp::assert_eq("got correct field on LexMajorEntry", (ULONG)5001, rgflid[0]);
			m_qmdc->GetFields(5, true, kgrfcptAll, 50, rgflid, &cflid);
			unitpp::assert_eq("got 17 fields on LexMajorEntry", 17, cflid);
			CheckOccurrence(rgflid, cflid, 5001);
			CheckOccurrence(rgflid, cflid, 2009);
			CheckOccurrence(rgflid, cflid, 103);

			StrUni stuClass(L"LexEntry");
			m_qmdc->GetClassId(stuClass.Bstr(), &clid);
			unitpp::assert_eq("got id of LexEntry", (ULONG)2, clid);

			m_qmdc->GetFieldName(2003, &bstr);
			unitpp::assert_true("Got name of CitationForm", wcscmp(L"CitationForm", bstr.Chars()) == 0);

			StrUni stuField(L"CitationForm");
			stuClass = L"LexMajorEntry";
			ULONG flid;
			m_qmdc->GetFieldId(stuClass.Bstr(), stuField.Bstr(), false, &flid);
			unitpp::assert_eq("base field not found", (ULONG)0, flid);
			m_qmdc->GetFieldId(stuClass.Bstr(), stuField.Bstr(), true, &flid);
			unitpp::assert_eq("base field found", (ULONG)2003, flid);

			int cpt;
			m_qmdc->GetFieldType(2003, &cpt);
			unitpp::assert_eq("field type of CitationForm", kcptMultiUnicode, cpt);

			m_qmdc->GetFieldType(2001, &cpt);
			unitpp::assert_eq("field type of HomographNumber", kcptInteger, cpt);

			m_qmdc->GetFieldType(2008, &cpt);
			unitpp::assert_eq("field type of Allomorphs", kcptOwningSequence, cpt);

			m_qmdc->GetFieldType(2016, &cpt);
			unitpp::assert_eq("field type of Pronunciation", kcptReferenceAtom, cpt);
		}

		void testAbstractClassesWithNumSet()
		{
			StrUni stuPath = DirectoryFinder::FwRootCodeDir(); // assume this is something like ...fw\distfiles
			stuPath.Append(L"\\..\\Src\\DbAccess\\Test\\SampleCmWithNum.xml");
			FwMetaDataCache::CreateCom(NULL, IID_IFwMetaDataCache, (void **)&m_qmdc);
			m_qmdc->InitXml(stuPath.Bstr(), true);

			ComBool fIsAbstract;
			m_qmdc->GetAbstract(2007, &fIsAbstract); // kclidCmPossibility
			unitpp::assert_true("CmPossibility is abstract", !fIsAbstract);
			SmartBstr bstr;
			m_qmdc->GetClassName(2007, &bstr);
			unitpp::assert_true("Got name of CmPossibility", wcscmp(L"CmPossibility", bstr.Chars()) == 0);
			ULONG clid;
			m_qmdc->GetBaseClsId(2007, &clid);
			unitpp::assert_eq("got base of CmPossibility", (ULONG)0, clid);
			m_qmdc->GetBaseClsId(2005, &clid);
			unitpp::assert_eq("got base of LexMajorEntry", (ULONG)2002, clid);

			ULONG rgflid[50];
			int cflid;
			m_qmdc->GetFields(2005, false, kgrfcptAll, 50, rgflid, &cflid);
			unitpp::assert_eq("got one field on LexMajorEntry", 1, cflid);
			unitpp::assert_eq("got correct field on LexMajorEntry", (ULONG)2005001, rgflid[0]);

			StrUni stuClass(L"LexEntry");
			m_qmdc->GetClassId(stuClass.Bstr(), &clid);
			unitpp::assert_eq("got id of LexEntry", (ULONG)2002, clid);

			m_qmdc->GetFieldName(2002003, &bstr);
			unitpp::assert_true("Got name of CitationForm", wcscmp(L"CitationForm", bstr.Chars()) == 0);

			int cpt;
			m_qmdc->GetFieldType(2002003, &cpt);
			unitpp::assert_eq("field type of CitationForm", kcptMultiUnicode, cpt);

			m_qmdc->GetFieldType(2002001, &cpt);
			unitpp::assert_eq("field type of HomographNumber", kcptInteger, cpt);

			m_qmdc->GetFieldType(2002008, &cpt);
			unitpp::assert_eq("field type of Allomorphs", kcptOwningSequence, cpt);

			m_qmdc->GetFieldType(2002016, &cpt);
			unitpp::assert_eq("field type of Pronunciation", kcptReferenceAtom, cpt);
		}

	public:
		TestMetaDataCacheXml();

		virtual void Setup()
		{
		}
		virtual void Teardown()
		{
			m_qmdc.Clear();
		}
	};
}

#endif /*TESTMETADATACACHEXML_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkdba-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
