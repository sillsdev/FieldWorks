/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestAutoLoad.h
Responsibility: John Thomson
Last reviewed:

	Unit tests relating to the .
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestAutoLoad_H_INCLUDED
#define TestAutoLoad_H_INCLUDED

#pragma once

#include "testViews.h"

typedef enum ViewTestDefns
{
	#define CMCG_SQL_ENUM 1
	#include "Ling.sqh" // Need kflidLexSense_Glosss, etc.
	#undef CMCG_SQL_ENUM
} ViewTestDefns;

namespace TestViews
{
	class TestAutoLoad : public unitpp::suite
	{
		IVwCacheDaPtr m_qcda;
		ISilDataAccessPtr m_qsda;
		IFwMetaDataCachePtr m_qmdc;
		IOleDbEncapPtr m_qode;
		ILgWritingSystemFactoryPtr m_qwsf;
		IVwOleDbDaPtr m_qodde;

	public:

		TestAutoLoad();

		void GetIntsFromSql(StrUni & stuQuery, Vector<int> & results)
		{
			IOleDbCommandPtr qodc;
			ULONG cbData;
			ComBool fIsNull;
			ComBool fMoreRows;

			results.Clear();

			CheckHr(m_qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			while (fMoreRows)
			{
				int val;
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&val),
					sizeof(int), &cbData, &fIsNull, 0));
				results.Push(val);

				CheckHr(qodc->NextRow(&fMoreRows));
			}
		}

		void testAutoLoadStrings()
		{
			Vector<int> vids;
			StrUni stuSql1(L"Select top 2 id from StTxtPara where Contents is not null");
			GetIntsFromSql(stuSql1, vids);
			HVO hvoPara1 = vids[0];
			HVO hvoPara2 = vids[1];
			StrUni stuSql2(L"Select top 1 id from StTxtPara where Contents is null");
			GetIntsFromSql(stuSql2, vids);
			HVO hvoNullPara = vids[0];
			ComBool fInCache;
			m_qsda->get_IsPropInCache(hvoPara1, kflidStTxtPara_Contents, kcptBigString, 0, &fInCache);
			unitpp::assert_true("String prop should not be cached before autoload", !fInCache);

			ITsStringPtr qtss1;
			m_qsda->get_StringProp(hvoPara1, kflidStTxtPara_Contents, &qtss1);

			m_qsda->get_IsPropInCache(hvoPara2, kflidStTxtPara_Contents, kcptBigString, 0, &fInCache);
			unitpp::assert_true("String prop should not be cached by autoload other prop", !fInCache);

			ITsStringPtr qtss2;
			m_qsda->get_StringProp(hvoPara2, kflidStTxtPara_Contents, &qtss2);

			m_qcda->ClearInfoAbout(hvoPara1, kciaRemoveObjectInfoOnly);
			m_qcda->ClearInfoAbout(hvoPara2, kciaRemoveObjectInfoOnly);
			m_qsda->get_IsPropInCache(hvoPara2, kflidStTxtPara_Contents, kcptBigString, 0, &fInCache);
			unitpp::assert_true("String prop should not be cached before autoload", !fInCache);

			m_qodde->put_AutoloadPolicy(kalpLoadForAllOfObjectClass);
			ITsStringPtr qtss1A;
			m_qsda->get_StringProp(hvoPara1, kflidStTxtPara_Contents, &qtss1A);
			m_qsda->get_IsPropInCache(hvoPara2, kflidStTxtPara_Contents, kcptBigString, 0, &fInCache);
			unitpp::assert_true("autoload all of class should load both props", fInCache);

			m_qsda->get_IsPropInCache(hvoNullPara, kflidStTxtPara_Contents, kcptBigString, 0, &fInCache);
			unitpp::assert_true("autoload all of class should load even null string props", fInCache);

			ComBool fEqual;
			qtss1->Equals(qtss1A, &fEqual);
			unitpp::assert_true("autoload all should get same result as one at a time", fEqual);

			ITsStringPtr qtss2A;
			m_qsda->get_StringProp(hvoPara2, kflidStTxtPara_Contents, &qtss2A);
			qtss2->Equals(qtss2A, &fEqual);
			unitpp::assert_true("autoload all should get same result as one at a time", fEqual);
			m_qodde->put_AutoloadPolicy(kalpLoadForThisObject);
		}

		void TryMultiStrings(OLECHAR * pchClass, OLECHAR *pchField, int cpt, int flid)
		{
			Vector<int> vids;
			StrUni stuSql;
			stuSql.Format(L"Select top 1 ws from %s_%s", pchClass, pchField);
			GetIntsFromSql(stuSql, vids);
			int ws = vids[0];
			stuSql.Format(L"Select top 2 obj from %s_%s where Txt is not null", pchClass, pchField);
			GetIntsFromSql(stuSql, vids);
			HVO hvo1 = vids[0];
			HVO hvo2 = vids[1];

			ComBool fInCache;
			m_qsda->get_IsPropInCache(hvo1, flid, cpt, 0, &fInCache);
			unitpp::assert_true("Multistring prop should not be cached before autoload", !fInCache);

			ITsStringPtr qtss1;
			m_qsda->get_MultiStringAlt(hvo1, flid, ws, &qtss1);

			m_qsda->get_IsPropInCache(hvo2, flid, cpt, 0, &fInCache);
			unitpp::assert_true("MultiString prop should not be cached by autoload other prop", !fInCache);

			ITsStringPtr qtss2;
			m_qsda->get_MultiStringAlt(hvo2, flid, ws, &qtss2);

			m_qcda->ClearAllData();
			m_qsda->get_IsPropInCache(hvo2, flid, cpt, ws, &fInCache);
			unitpp::assert_true("Multistring prop should not be cached before autoload", !fInCache);

			m_qodde->put_AutoloadPolicy(kalpLoadForAllOfObjectClass);
			ITsStringPtr qtss1A;
			m_qsda->get_MultiStringAlt(hvo1, flid, ws, &qtss1A);
			m_qsda->get_IsPropInCache(hvo2, flid, cpt, ws, &fInCache);
			unitpp::assert_true("autoload all of class should load both props", fInCache);

			ComBool fEqual;
			qtss1->Equals(qtss1A, &fEqual);
			unitpp::assert_true("autoload all should get same result as one at a time", fEqual);

			ITsStringPtr qtss2A;
			m_qsda->get_MultiStringAlt(hvo2, flid, ws, &qtss2A);
			qtss2->Equals(qtss2A, &fEqual);
			unitpp::assert_true("autoload all should get same result as one at a time", fEqual);

			// Autoloading a large and therefore invalid writing system should produce two empty strings cached.
			m_qsda->get_MultiStringAlt(hvo2, flid, 10000000, &qtss1);
			m_qsda->get_IsPropInCache(hvo2, flid, cpt, 10000000, &fInCache);
			unitpp::assert_true("autoload all of class should load even null string props", fInCache);
			m_qodde->put_AutoloadPolicy(kalpLoadForThisObject);
		}

		void testAtomicObject()
		{
			Vector<int> vids;
			StrUni stuSql(L"Select distinct top 2 le.id from LexEntry le join MoForm_ mf on le.id = mf.owner$ where MorphType is not null "
				L"union all Select distinct top 2 mf.id from LexEntry le join MoForm_ mf on le.id = mf.owner$ where MorphType is not null");
			GetIntsFromSql(stuSql, vids);
			HVO hvoe1 = vids[0];
			HVO hvoe2 = vids[1];
			HVO hvom3 = vids[2];
			HVO hvom4 = vids[3];

			ComBool fInCache;
			m_qsda->get_IsPropInCache(hvoe1, kflidLexEntry_LexemeForm, kcptOwningAtom, 0, &fInCache);
			unitpp::assert_true("owning atomic prop should not be cached before autoload", !fInCache);
			m_qsda->get_IsPropInCache(hvom3, kflidMoForm_MorphType, kcptReferenceAtom, 0, &fInCache);
			unitpp::assert_true("ref atomic prop should not be cached before autoload", !fInCache);

			HVO hvoLf1, hvoMf3;
			m_qsda->get_ObjectProp(hvoe1, kflidLexEntry_LexemeForm, &hvoLf1);
			m_qsda->get_ObjectProp(hvom3, kflidMoForm_MorphType, &hvoMf3);
			m_qsda->get_IsPropInCache(hvoe2, kflidLexEntry_LexemeForm, kcptOwningAtom, 0, &fInCache);
			unitpp::assert_true("owning atomic prop should not be cached by loading another prop", !fInCache);
			m_qsda->get_IsPropInCache(hvom4, kflidMoForm_MorphType, kcptOwningAtom, 0, &fInCache);
			unitpp::assert_true("ref atomic prop should not be cached by loading another prop", !fInCache);

			HVO hvoLf2, hvoMf4;
			m_qsda->get_ObjectProp(hvoe2, kflidLexEntry_LexemeForm, &hvoLf2);
			m_qsda->get_ObjectProp(hvom4, kflidMoForm_MorphType, &hvoMf4);

			m_qodde->put_AutoloadPolicy(kalpLoadForAllOfObjectClass);
			m_qcda->ClearAllData();

			HVO hvoLf1A, hvoMf3A;
			m_qsda->get_ObjectProp(hvoe1, kflidLexEntry_LexemeForm, &hvoLf1A);
			m_qsda->get_ObjectProp(hvom3, kflidMoForm_MorphType, &hvoMf3A);
			unitpp::assert_eq("owning atomic prop should load same value in all of object class mode", hvoLf1, hvoLf1A);
			unitpp::assert_eq("ref atomic prop should load same value in all of object class mode", hvoMf3, hvoMf3A);

			m_qsda->get_IsPropInCache(hvoe2, kflidLexEntry_LexemeForm, kcptOwningAtom, 0, &fInCache);
			unitpp::assert_true("owning atomic prop should be cached by loading another prop in all of object class mode", fInCache);
			m_qsda->get_IsPropInCache(hvom4, kflidMoForm_MorphType, kcptOwningAtom, 0, &fInCache);
			unitpp::assert_true("ref atomic prop should be cached by loading another prop in all of object class mode", fInCache);

			HVO hvoLf2A, hvoMf4A;
			m_qsda->get_ObjectProp(hvoe2, kflidLexEntry_LexemeForm, &hvoLf2A);
			m_qsda->get_ObjectProp(hvom4, kflidMoForm_MorphType, &hvoMf4A);
			unitpp::assert_eq("2nd owning atomic prop should load same value in all of object class mode", hvoLf2, hvoLf2A);
			unitpp::assert_eq("2nd ref atomic prop should load same value in all of object class mode", hvoMf4, hvoMf4A);

			m_qodde->put_AutoloadPolicy(kalpLoadForThisObject);

		}

		void testAutoLoadMultiStrings()
		{
			TryMultiStrings(L"LexSense", L"Gloss", kcptMultiUnicode, kflidLexSense_Gloss);
			TryMultiStrings(L"LexSense", L"Definition", kcptMultiString, kflidLexSense_Definition);
		}


		virtual void Setup()
		{
			m_qsda.CreateInstance(CLSID_VwOleDbDa);
			CheckHr(m_qsda->QueryInterface(IID_IVwCacheDa, (void **) & m_qcda));
			CheckHr(m_qsda->QueryInterface(IID_IVwOleDbDa, (void **) & m_qodde));

			m_qode.CreateInstance(CLSID_OleDbEncap);
			StrUni stuDBMName(L".\\SILFW");
			StrUni stuDbName(L"TestLangProj");
			CheckHr(m_qode->Init(stuDBMName.Bstr(), stuDbName.Bstr(), NULL, koltReturnError, 1000));
			m_qmdc.CreateInstance(CLSID_FwMetaDataCache);
			CheckHr(m_qmdc->Init(m_qode));
			ILgWritingSystemFactoryBuilderPtr qBuilder;
			qBuilder.CreateInstance(CLSID_LgWritingSystemFactoryBuilder);
			CheckHr(qBuilder->GetWritingSystemFactory(m_qode, NULL, &m_qwsf));
			ISetupVwOleDbDaPtr qsetup;
			CheckHr(m_qsda->QueryInterface(IID_ISetupVwOleDbDa, (void **)(&qsetup)));
			CheckHr(qsetup->Init(m_qode, m_qmdc, m_qwsf, NULL));
		}
		virtual void Teardown()
		{
			CheckHr(m_qodde->Close());
			m_qcda->ClearAllData();
			m_qwsf->Shutdown();;
			m_qwsf.Clear();
			m_qmdc.Clear();
			m_qsda.Clear();
			m_qcda.Clear();
			m_qodde.Clear();
			m_qode.Clear();
		}
	};
}
#endif /*TestAutoLoad_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
