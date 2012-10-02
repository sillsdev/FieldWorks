/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestVwOleDbDaUndoRedo.h
Responsibility:
Last reviewed:

	Unit tests for the TestVwOleDbDaUndoRedo class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestVwOleDbDaUndoRedo_H_INCLUDED
#define TestVwOleDbDaUndoRedo_H_INCLUDED

#pragma once

#include "testViews.h"
#include <algorithm>

namespace TestViews
{

	// Now we get to the actual tests.
	class TestVwOleDbDaUndoRedo : public unitpp::suite
	{
	   private:

		IActionHandlerPtr m_qacth; // CLSID_ActionHandler
		ISilDataAccessPtr m_qsda; // CLSID_VwOleDbDa
		IVwCacheDaPtr m_qcda; // IID_IVwCacheDa
		ITsStrFactoryPtr m_qtsf;

		IFwMetaDataCachePtr m_qmdc; // CLSID_FwMetaDataCache
		IOleDbEncapPtr m_qode;	// CLSID_OleDbEncap
		ILgWritingSystemFactoryPtr m_qwsf;

		static const int kclidTester = 9999;
		static const int kclidTester1000 = kclidTester*1000;

		static const int kflidTester_Boolean	= kclidTester1000 + kcptBoolean;
		static const int kflidTester_Integer	= kclidTester1000 + kcptInteger;
		static const int kflidTester_Numeric	= kclidTester1000 + kcptNumeric;
		static const int kflidTester_Float		= kclidTester1000 + kcptFloat;
		static const int kflidTester_Time		= kclidTester1000 + kcptTime;
		static const int kflidTester_Guid		= kclidTester1000 + kcptGuid;
		static const int kflidTester_Image		= kclidTester1000 + kcptImage;
		static const int kflidTester_GenDate	= kclidTester1000 + kcptGenDate;
		static const int kflidTester_Binary		= kclidTester1000 + kcptBinary;

		static const int kflidTester_String			= kclidTester1000 + kcptString;
		static const int kflidTester_MultiString	= kclidTester1000 + kcptMultiString;
		static const int kflidTester_Unicode		= kclidTester1000 + kcptUnicode;
		static const int kflidTester_MultiUnicode	= kclidTester1000 + kcptMultiUnicode;
		static const int kflidTester_BigString		= kclidTester1000 + kcptBigString;
		static const int kflidTester_MultiBigString	= kclidTester1000 + kcptMultiBigString;
		static const int kflidTester_BigUnicode		= kclidTester1000 + kcptBigUnicode;
		static const int kflidTester_MultiBigUnicode= kclidTester1000 + kcptMultiBigUnicode;

		static const int kflidTester_OwningAtom			= kclidTester1000 + kcptOwningAtom;
		static const int kflidTester_ReferenceAtom		= kclidTester1000 + kcptReferenceAtom;
		static const int kflidTester_OwningCollection	= kclidTester1000 + kcptOwningCollection;
		static const int kflidTester_ReferenceCollection= kclidTester1000 + kcptReferenceCollection;
		static const int kflidTester_OwningSequence		= kclidTester1000 + kcptOwningSequence;
		static const int kflidTester_ReferenceSequence	= kclidTester1000 + kcptReferenceSequence;
		static const int kflidTester_OwningSequence2	= kclidTester1000 + 40; // arbitrary extra owning seq for MoveOwningSeq.

		class TestObjectVerifier
		{
			int m_hvo;
			ISilDataAccessPtr m_qsda;
			Vector<int> m_vws; // writing systems to verify
			ITsStrFactoryPtr m_qtsf;

			// These are used to save the current values.
			int m_fBool;
			int m_nInt;
			// Numeric: yagni
			// Float: yagni.
			int64 m_time;
			GUID m_guid;
			Vector<byte> m_vbImage;
			int m_gendate;
			Vector<byte> m_vbBinary;
			ITsStringPtr m_qtssString;
			ComVector<ITsString> m_vtssMultiString;
			SmartBstr m_sbstrUnicode;
			ComVector<ITsString> m_vtssMultiUnicode;
			ITsStringPtr m_qtssBigString;
			ComVector<ITsString> m_vtssMultiBigString;
//			ITsStringPtr m_qtssBigUnicode;
			SmartBstr m_sbstrBigUnicode;
			ComVector<ITsString> m_vtssMultiBigUnicode; // yagni?

			HVO m_hvoRefAtom;
			HVO m_hvoOwnAtom;
			Vector<HVO> m_vhvoRefColl;
			Vector<HVO> m_vhvoOwnColl;
			Vector<HVO> m_vhvoRefSeq;
			Vector<HVO> m_vhvoOwnSeq;
		public:

			// This constructor takes an existing object and saves all its values.
			TestObjectVerifier(int hvo, ISilDataAccess * psda, int * prgws, int cws)
			{
				m_hvo = hvo;
				m_qsda = psda;
				m_vws.Resize(cws);
				::memcpy(m_vws.Begin(), prgws, cws * isizeof(int));
				Load();
			}

			// This one initializes all fields to arbitrary values, based on a seed (which is modified)
			TestObjectVerifier(int hvo, ISilDataAccess * psda, int * prgws, int cws, ITsStrFactory * ptsf, int & val)
			{
				m_hvo = hvo;
				m_qsda = psda;
				m_vws.Resize(cws);
				::memcpy(m_vws.Begin(), prgws, cws * isizeof(int));
				m_qtsf = ptsf;
				CreateState(val);
				// The time value we read back from the database may not be exactly
				// the 64 bit integer we stored there, probably due to rounding
				// errors. By clearing the cache now, we ensure that the values
				// we save as the current values are what we get from reading from
				// the database in its current state.
				ClearCache();
				Load();
			}

			void ClearCache()
			{
				IVwCacheDaPtr qcda;
				CheckHr(m_qsda->QueryInterface(IID_IVwCacheDa, (void **)&qcda));
				CheckHr(qcda->ClearInfoAbout(m_hvo, kciaRemoveObjectAndOwnedInfo)); // Forget anything the cache has about our object.
			}

			int Hvo()
			{
				return m_hvo;
			}


			// Read into vtss the current values of the specified multistring property for
			// the writing systems specified in m_vws
			void GetMultiString(int flid, ComVector<ITsString> & vtss)
			{
				ITsStringPtr qtss;
				for (int i = 0; i < m_vws.Size(); i++)
				{
					CheckHr(m_qsda->get_MultiStringAlt(m_hvo, flid, m_vws[i], &qtss));
					vtss.Push(qtss);
				}
			}

			// Verify that all alternatives of the specified property (except possibly ws)
			// have the expected values.
			void VerifyMultiString(int flid, ComVector<ITsString> & vtss, int flidSkip, int wsSkip)
			{
				if (flidSkip == flid)
					return;
				ITsStringPtr qtss;
				for (int i = 0; i < m_vws.Size(); i++)
				{
					if (m_vws[i] == wsSkip)
						continue;
					CheckHr(m_qsda->get_MultiStringAlt(m_hvo, flid, m_vws[i], &qtss));
					ComBool fEqual;
					CheckHr(qtss->Equals(vtss[i], &fEqual));
					if (!fEqual)
						unitpp::assert_true("multistring alt failed", fEqual);
				}
			}
			// Verify that  the specified property has the expected values.
			void VerifyString(int flid, ITsString * ptss, int flidSkip)
			{
				if (flidSkip == flid)
					return;
				ITsStringPtr qtss;
				CheckHr(m_qsda->get_StringProp(m_hvo, flid, &qtss));
				ComBool fEqual;
				CheckHr(qtss->Equals(ptss, &fEqual));
				if (!fEqual)
				{
					// for debugging
					int cch1, cch2;
					SmartBstr sbstr1, sbstr2;
					CheckHr(ptss->get_Length(&cch1));
					CheckHr(qtss->get_Length(&cch2));
					CheckHr(ptss->get_Text(&sbstr1));
					CheckHr(qtss->get_Text(&sbstr2));
					unitpp::assert_true("string prop failed", fEqual);
				}
			}

			void VerifyUnicode(int flid, BSTR bstr, int flidSkip)
			{
				if (flidSkip == flid)
					return;
				SmartBstr sbstr;
				CheckHr(m_qsda->get_UnicodeProp(m_hvo, flid, &sbstr));
				if (bstr == NULL && sbstr.Length() == 0)
					return; // wcscmp will choke
				unitpp::assert_true("unicode prop failed", bstr != NULL && sbstr.Length() != 0);
				unitpp::assert_true("unicode prop failed", wcscmp(bstr, sbstr.Chars()) == 0);
			}

			static const int kMaxVecSize = 100; // constant, comfortably enough for any vec we use in testing.

			void LoadVec(int flid, Vector<HVO> & vhvo)
			{
				int cobj;
				CheckHr(m_qsda->get_VecSize(m_hvo, flid, &cobj));
				vhvo.Resize(cobj);
				CheckHr(m_qsda->VecProp(m_hvo, flid, cobj, &cobj, vhvo.Begin()));
			}

			void VerifyVec(int flid, Vector<HVO> & vhvo, int flidSkip)
			{
				if (flid == flidSkip)
					return;
				int cobj;
				HVO rghvo[kMaxVecSize];
				CheckHr(m_qsda->VecProp(m_hvo, flid, kMaxVecSize, &cobj, rghvo));
				unitpp::assert_true("manageable #objs", cobj < kMaxVecSize);
				if (cobj != vhvo.Size())
					unitpp::assert_eq("vector size", vhvo.Size(), cobj);
				for (int i = 0; i < cobj; i++)
					if (vhvo[i] != rghvo[i])
						unitpp::assert_eq("vector item", vhvo[i], rghvo[i]);
			}

			void SortVec(HVO * prghvo, int chvo)
			{
				if (chvo > 1)
					std::sort<HVO *>(prghvo, prghvo + chvo);
			}

			void VerifyCollection(int flid, Vector<HVO> & vhvo, int flidSkip)
			{
				if (flid == flidSkip)
					return;
				int cobj;
				HVO rghvo[kMaxVecSize];
				CheckHr(m_qsda->VecProp(m_hvo, flid, kMaxVecSize, &cobj, rghvo));
				unitpp::assert_true("manageable #objs", cobj < kMaxVecSize);
				if (vhvo.Size() != cobj)
					unitpp::assert_eq("collection size", vhvo.Size(), cobj);
				SortVec(rghvo, cobj);
				for (int i = 0; i < cobj; i++)
					if (vhvo[i] !=rghvo[i])
						unitpp::assert_eq("collection item", vhvo[i], rghvo[i]);
			}

			void VerifyObj(int flid, HVO hvoExpected, int flidSkip)
			{
				if (flid == flidSkip)
					return;
				HVO hvo;
				CheckHr(m_qsda->get_ObjectProp(m_hvo, flid, &hvo));
				unitpp::assert_eq("obj prop", hvoExpected, hvo);
			}

			// Save the current values into the member variables.
			void Load()
			{
				CheckHr(m_qsda->get_IntProp(m_hvo, kflidTester_Boolean, &m_fBool));
				CheckHr(m_qsda->get_IntProp(m_hvo, kflidTester_Integer, &m_nInt));
				CheckHr(m_qsda->get_TimeProp(m_hvo, kflidTester_Time, &m_time));
				CheckHr(m_qsda->get_GuidProp(m_hvo, kflidTester_Guid, &m_guid));

				int cb;
				CheckHr(m_qsda->BinaryPropRgb(m_hvo, kflidTester_Image, NULL, 0, &cb));
				m_vbImage.Resize(cb);
				CheckHr(m_qsda->BinaryPropRgb(m_hvo, kflidTester_Image, m_vbImage.Begin(), cb, &cb));

				CheckHr(m_qsda->get_IntProp(m_hvo, kflidTester_GenDate, &m_gendate));

				CheckHr(m_qsda->BinaryPropRgb(m_hvo, kflidTester_Binary, NULL, 0, &cb));
				m_vbBinary.Resize(cb);
				CheckHr(m_qsda->BinaryPropRgb(m_hvo, kflidTester_Binary, m_vbBinary.Begin(), cb, &cb));

				CheckHr(m_qsda->get_StringProp(m_hvo, kflidTester_String, &m_qtssString));
				GetMultiString(kflidTester_MultiString, m_vtssMultiString);
				CheckHr(m_qsda->get_UnicodeProp(m_hvo, kflidTester_Unicode, &m_sbstrUnicode));
				GetMultiString(kflidTester_MultiUnicode, m_vtssMultiUnicode);
				CheckHr(m_qsda->get_StringProp(m_hvo, kflidTester_BigString, &m_qtssBigString));
				GetMultiString(kflidTester_MultiBigString, m_vtssMultiBigString);
				CheckHr(m_qsda->get_UnicodeProp(m_hvo, kflidTester_BigUnicode, &m_sbstrBigUnicode));
				GetMultiString(kflidTester_MultiBigUnicode, m_vtssMultiBigUnicode);

				CheckHr(m_qsda->get_ObjectProp(m_hvo, kflidTester_OwningAtom, &m_hvoOwnAtom));
				CheckHr(m_qsda->get_ObjectProp(m_hvo, kflidTester_ReferenceAtom, &m_hvoRefAtom));
				LoadVec(kflidTester_OwningCollection, m_vhvoOwnColl);
				SortVec(m_vhvoOwnColl.Begin(), m_vhvoOwnColl.Size());
				LoadVec(kflidTester_ReferenceCollection, m_vhvoRefColl);
				SortVec(m_vhvoRefColl.Begin(), m_vhvoRefColl.Size());
				LoadVec(kflidTester_OwningSequence, m_vhvoOwnSeq);
				LoadVec(kflidTester_ReferenceSequence, m_vhvoRefSeq);
			}

			void VerifyInt(int flid, int val, int flidSkip)
			{
				if (flid == flidSkip)
					return;
				int nVal;
				CheckHr(m_qsda->get_IntProp(m_hvo, flid, &nVal));
				if (val != nVal)
					unitpp::assert_eq("verify int val failed", val, nVal);
			}

			void VerifyBinary(int flid, Vector<byte> vbExpected, int flidSkip)
			{
				if (flid == flidSkip)
					return;
				int cb;
				CheckHr(m_qsda->BinaryPropRgb(m_hvo, flid, NULL, 0, &cb));
				unitpp::assert_eq("binary prop size", vbExpected.Size(), cb);
				Vector<byte> vb;
				vb.Resize(cb);
				CheckHr(m_qsda->BinaryPropRgb(m_hvo, flid, vb.Begin(), cb, &cb));
				for (int i = 0; i < cb; ++i)
				{
					unitpp::assert_eq("binary byte", vbExpected[i], vb[i]);
				}
			}

			// Verify that the values are currently as saved.
			// Skip the one with the specified flid (often the one we are directly testing)
			// and, if relevant, ws.
			void Verify(int flidSkip = -1, int wsSkip = -1)
			{
				ClearCache(); // Forget anything the cache has about our object.

				VerifyInt(kflidTester_Boolean, m_fBool, flidSkip);
				VerifyInt(kflidTester_Integer, m_nInt, flidSkip);
				if (flidSkip != kflidTester_Time)
				{
					int64 time;
					CheckHr(m_qsda->get_TimeProp(m_hvo, kflidTester_Time, &time));
					if (m_time != time)
						unitpp::assert_eq("verify time val failed", m_time, time);
				}
				if (flidSkip != kflidTester_Guid)
				{
					GUID guid;
					CheckHr(m_qsda->get_GuidProp(m_hvo, kflidTester_Guid, &guid));
					if (!IsEqualGUID(guid, m_guid))
						unitpp::assert_true("verify guid val failed", ::IsEqualGUID(guid, m_guid));
				}
				VerifyBinary(kflidTester_Image, m_vbImage, flidSkip);
				VerifyInt(kflidTester_GenDate, m_gendate, flidSkip);
				VerifyBinary(kflidTester_Binary, m_vbBinary, flidSkip);

				VerifyString(kflidTester_String, m_qtssString, flidSkip);
				VerifyMultiString(kflidTester_MultiString, m_vtssMultiString, flidSkip, wsSkip);
				VerifyUnicode(kflidTester_Unicode, m_sbstrUnicode, flidSkip);
				VerifyMultiString(kflidTester_MultiUnicode, m_vtssMultiUnicode, flidSkip, wsSkip);
				VerifyString(kflidTester_BigString, m_qtssBigString, flidSkip);
				VerifyMultiString(kflidTester_MultiBigString, m_vtssMultiBigString, flidSkip, wsSkip);
				VerifyUnicode(kflidTester_BigUnicode, m_sbstrBigUnicode, flidSkip);
				VerifyMultiString(kflidTester_MultiBigUnicode, m_vtssMultiBigUnicode, flidSkip, wsSkip);

				VerifyObj(kflidTester_OwningAtom, m_hvoOwnAtom, flidSkip);
				VerifyObj(kflidTester_ReferenceAtom, m_hvoRefAtom, flidSkip);
				VerifyCollection(kflidTester_OwningCollection, m_vhvoOwnColl, flidSkip);
				VerifyCollection(kflidTester_ReferenceCollection, m_vhvoRefColl, flidSkip);
				VerifyVec(kflidTester_OwningSequence, m_vhvoOwnSeq, flidSkip);
				VerifyVec(kflidTester_ReferenceSequence, m_vhvoRefSeq, flidSkip);
			}

			void CreateString(int & val, ITsString ** pptss, int ws)
			{
				StrUni stu;
				stu.Format(L"sample %d", val++);
				CheckHr(m_qtsf->MakeString(stu.Bstr(), ws, pptss));
			}

			void CreateStringVal(int flid, int & val)
			{
				ITsStringPtr qtss;
				CreateString(val, &qtss, m_vws[0]);
				CheckHr(m_qsda->SetString(m_hvo, flid, qtss));
			}

			void CreateStringUnicode(int flid, int & val)
			{
				StrUni stu;
				stu.Format(L"Unicode %d", val++);
				CheckHr(m_qsda->SetUnicode(m_hvo, flid, stu.Bstr(), stu.Length()));
			}

			void CreateStringAlts(int flid, int & val)
			{
				ITsStringPtr qtss;
				for (int i = 0; i < m_vws.Size(); ++i)
				{
					CreateString(val, &qtss, m_vws[i]);
					CheckHr(m_qsda->SetMultiStringAlt(m_hvo, flid, m_vws[i], qtss));
				}
			}

			// Create an initial state for the object, an arbitrary state often based on the integer,
			// which is subsequently incremented (several times).
			void CreateState(int & val)
			{
				CheckHr(m_qsda->SetInt(m_hvo, kflidTester_Boolean, val & 1));
				CheckHr(m_qsda->SetInt(m_hvo, kflidTester_Integer, val));
				SilTime stimNow = SilTime::CurTime() + val; // Need a valid time.
				CheckHr(m_qsda->SetTime(m_hvo, kflidTester_Time, stimNow.AsInt64()));

				GUID guid;
				::CoCreateGuid(&guid);
				CheckHr(m_qsda->SetGuid(m_hvo, kflidTester_Guid, guid));

				StrAnsi sta;
				sta.Format("Nonsence %d%x", val, val + 357);
				CheckHr(m_qsda->SetBinary(m_hvo, kflidTester_Image, (byte *)const_cast<schar *>(sta.Chars()), sta.Length()));

				CheckHr(m_qsda->SetInt(m_hvo, kflidTester_Integer, val - 304));

				sta.Format("%d%x rubbish", val, val + 357);
				CheckHr(m_qsda->SetBinary(m_hvo, kflidTester_Binary, (byte *)const_cast<schar *>(sta.Chars()), sta.Length()));

				CreateStringVal(kflidTester_String, val);
				CreateStringAlts(kflidTester_MultiString, val);
				CreateStringUnicode(kflidTester_Unicode, val);
				CreateStringAlts(kflidTester_MultiUnicode, val);
				CreateStringVal(kflidTester_BigString, val);
				CreateStringAlts(kflidTester_MultiBigString, val);
				CreateStringUnicode(kflidTester_BigUnicode, val);
				CreateStringAlts(kflidTester_MultiBigUnicode, val);
				HVO hvo1, hvo2, hvo3, hvo4;
				CheckHr(m_qsda->MakeNewObject(kclidTester, m_hvo, kflidTester_OwningAtom, -2, &hvo1));
				CheckHr(m_qsda->MakeNewObject(kclidTester, m_hvo, kflidTester_OwningCollection, -1, &hvo2));
				CheckHr(m_qsda->MakeNewObject(kclidTester, m_hvo, kflidTester_OwningSequence, 0, &hvo3));
				CheckHr(m_qsda->MakeNewObject(kclidTester, m_hvo, kflidTester_OwningSequence, 1, &hvo4));
				HVO rghvo[] = {hvo1, hvo3, hvo4, hvo3};
				CheckHr(m_qsda->SetObjProp(m_hvo, kflidTester_ReferenceAtom, hvo3));
				CheckHr(m_qsda->Replace(m_hvo, kflidTester_ReferenceCollection, 0, 0, rghvo, 4));
				CheckHr(m_qsda->Replace(m_hvo, kflidTester_ReferenceSequence, 0, 0, rghvo + 1, 3));

				val ++;
			}
		};

		void VerifyObjSeq(HVO hvoObj, int tag, HVO * prghvoExpected, int chvoExpected)
		{
			HVO rghvo[50];
			int chvo;
			CheckHr(m_qsda->VecProp(hvoObj, tag, 50, &chvo, rghvo));
			unitpp::assert_eq("vec length", chvoExpected, chvo);
			for (int i = 0; i < chvo; i++)
				unitpp::assert_eq("item in vec", prghvoExpected[i], rghvo[i]);
		}


		/*--------------------------------------------------------------------------------------
			Add the new 'Tester' object to the database that has all of the properties so we
			can test with this single object.
		--------------------------------------------------------------------------------------*/
		void CreateTesterObject()
		{
			IOleDbCommandPtr qodc;
			HRESULT hr;
			CheckHr(hr = m_qode->CreateCommand(&qodc));
			unitpp::assert_eq("CreateTesterObject: hr = m_qode->CreateCommand()", S_OK, hr);

			// REVISIT: We should Format this string to use kflidTester_ class, rather than using direct string constants for SQL values().
			StrUni stuSql = L"insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])	values(9999, 6, 0, 0, 'Tester');\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999001, 1, 9999, null, 'Boolean',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999002, 2, 9999, null, 'Integer',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999003, 3, 9999, null, 'Numeric',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999004, 4, 9999, null, 'Float',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999005, 5, 9999, null, 'Time',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999006, 6, 9999, null, 'Guid',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999007, 7, 9999, null, 'Image',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999008, 8, 9999, null, 'GenDate',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999009, 9, 9999, null, 'Binary',0,Null, null, null, 0);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999013, 13, 9999, null, 'String',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999014, 14, 9999, null, 'MultiString',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999015, 15, 9999, null, 'Unicode',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999016, 16, 9999, null, 'MultiUnicode',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999017, 17, 9999, null, 'BigString',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999018, 18, 9999, null, 'MultiBigString',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999019, 19, 9999, null, 'BigUnicode',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999020, 20, 9999, null, 'MultiBigUnicode',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999023, 23, 9999, 9999, 'OwningAtom',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999024, 24, 9999, 9999, 'ReferenceAtom',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999025, 25, 9999, 9999, 'OwningCollection',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999026, 26, 9999, 9999, 'ReferenceCollection',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999027, 27, 9999, 9999, 'OwningSequence',0,Null, null, null, null);\n"
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999028, 28, 9999, 9999, 'ReferenceSequence',0,Null, null, null, null);\n"
				// Need two owning sequence props to test MoveOwnSeq properly.
				L"insert into [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])	values(9999040, 27, 9999, 9999, 'OwningSequence2',0,Null, null, null, null);\n"
				L"exec UpdateClassView$ 9999";

			CheckHr(hr = qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
			unitpp::assert_eq("CreateTesterObject: hr = qodc->ExecCommand(UpdateClassView$)", S_OK, hr);
		}

		void BackupDB()
		{
			HRESULT hr;
			IOleDbCommandPtr qodc;

			CheckHr(hr = m_qode->CreateCommand(&qodc));
			unitpp::assert_eq("BackupDB: hr = m_qode->CreateCommand()", S_OK, hr);
			StrUni stuSql;
			stuSql.Format(L"backup database [TestLangProj] to disk=\'TestLangProj_Test.bak\' with init");
			CheckHr(hr = qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
			unitpp::assert_eq("BackupDB: hr = qodc->ExecCommand()", S_OK, hr);
		}

		void RestoreDB()
		{
			HRESULT hr;
			IOleDbCommandPtr qodc;

			CheckHr(hr = m_qode->CreateCommand(&qodc));
			unitpp::assert_eq("RestoreDB: hr = m_qode->CreateCommand()", S_OK, hr);
			StrUni stuSql;
			stuSql.Format(L"restore database [TestLangProj] from disk=\'TestLangProj_Test.bak\'");
			CheckHr(hr = qodc->ExecCommand(stuSql.Bstr(), knSqlStmtNoResults));
			unitpp::assert_eq("RestoreDB: hr = qodc->ExecCommand()", S_OK, hr);
		}

		// JIRA [LT-1172]
		void jiraDuplicateOwningAtomTest()
		{
			HRESULT hr;
			HVO hvoTest = CreateNewTesterObjectInDB();

			HVO hvoAtom1 = 0;
			HVO hvoAtom2 = 0;
			// Create First OwningAtom
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningAtom, -2, &hvoAtom1));

			// Create "Second" OwningAtom
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningAtom, -2, &hvoAtom2));

			/* // Check to see if there are multiple objects that have the same owner.
			StrUni stuCheckOwning;
			stuCheckOwning.Format(L"select [id] from [%s] where [Owner$]=%d",
					"Tester_", hvoTest);

			IOleDbCommandPtr qodc;
			m_qode->CreateCommand(&qodc);
			hr = qodc->ExecCommand(stuCheckOwning.Bstr(), knSqlStmtSelectWithOneRowset);

			ComBool fMoreRows;
			ComBool fIsNull;
			ULONG cbSpaceTaken;
			qodc->NextRow(&fMoreRows);
			Vector<HVO> vhvoOwned;
			HVO hvoOwned;
			int numOwned = 0;
			while (fMoreRows)
			{
				hr = qodc->GetColValue(1, &hvoOwned, isizeof(hvoOwned), &cbSpaceTaken, &fIsNull, 0);
				if (!fIsNull)
				{
					vhvoOwned.Push(hvoOwned);
				}
				hr = qodc->NextRow(&fMoreRows);
			}
			numOwned = vhvoOwned.Size();
			unitpp::assert_eq("hvoTest should have only one OwningAtom.", 1, numOwned); */
		}

		// JIRA [LT-1173]
		void testJiraOwningAtomUndoTest()
		{
			HRESULT hr;
			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");

			HVO hvoTest = CreateNewTesterObjectInDB();
			HVO hvoNewAtom = 0;
			HVO hvoAtom_beforeMakeNewObject = 0;
			HVO hvoAtom_afterMakeNewObject = 0;
			HVO hvoAtom_afterUndo = 0;

			CheckHr(hr = m_qsda->get_ObjectProp(hvoTest, kflidTester_OwningAtom, &hvoAtom_beforeMakeNewObject));

			// Undoable Task: Make New Object
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningAtom, -2, &hvoNewAtom));
			CheckHr(hr = m_qsda->EndUndoTask());

			CheckHr(hr = m_qsda->get_ObjectProp(hvoTest, kflidTester_OwningAtom, &hvoAtom_afterMakeNewObject));
			unitpp::assert_eq("jiraOwningAtomUndoTest(): OwningAtom set", hvoNewAtom, hvoAtom_afterMakeNewObject);

			UndoResult ures;
			CheckHr(hr = m_qacth->Undo(&ures));

			CheckHr(hr = m_qsda->get_ObjectProp(hvoTest, kflidTester_OwningAtom, &hvoAtom_afterUndo));
			unitpp::assert_eq("jiraOwningAtomUndoTest(): OwningAtom reset", hvoAtom_beforeMakeNewObject, hvoAtom_afterUndo);
		}

		void testUndoRedo()
		{
			HRESULT hr;

			// Initial setup properties
			const int kInt_init = 55;
			StrUni stu(L"init_string");

			HVO hvoTest;
			HVO hvoTest1;
			HVO hvoTest2;
			HVO hvoTest3;
			HVO hvoTest4;

			hvoTest = CreateNewTesterObjectInDB();

			// DEBUG: Tests for some JIRAs

			//jiraDuplicateOwningAtomTest();	// JIRA [LT-1172]
			//testJiraOwningAtomUndoTest();		// JIRA [LT-1173]

			// Create Owned Objects
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningAtom, -2, &hvoTest4));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 0, &hvoTest1));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 1, &hvoTest2));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 2, &hvoTest3));
			HVO rghvoFirst[] = {hvoTest1, hvoTest2, hvoTest3};

			// Setup more initial property values
			int wsEng;
			CheckHr(m_qwsf->get_UserWs(&wsEng));
			ITsStringPtr qtss_init;
			CheckHr(hr = m_qtsf->MakeString(stu.Bstr(), wsEng, &qtss_init));
			CheckHr(hr = m_qsda->SetMultiStringAlt(hvoTest, kflidTester_MultiString, wsEng, qtss_init));
			CheckHr(hr = m_qsda->SetString(hvoTest, kflidTester_String, qtss_init));
			CheckHr(hr = m_qsda->SetInt(hvoTest, kflidTester_Integer, kInt_init));
			CheckHr(hr = m_qsda->SetObjProp(hvoTest, kflidTester_ReferenceAtom, hvoTest1));
			CheckHr(hr = m_qsda->Replace(hvoTest, kflidTester_ReferenceSequence, 0, 0, rghvoFirst, 3));

			// Set some values, in three groups.
			// (First Group)
			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));

			StrUni stuFirst(L"testUndoRedo:UndoTask1");
			ITsStringPtr qtss1;
			CheckHr(hr = m_qtsf->MakeString(stuFirst.Bstr(), wsEng, &qtss1));
			CheckHr(hr = m_qsda->SetString(hvoTest, kflidTester_String, qtss1));
			CheckHr(hr = m_qsda->SetObjProp(hvoTest, kflidTester_ReferenceAtom, hvoTest2));
			CheckHr(hr = m_qsda->SetObjProp(hvoTest2, kflidTester_ReferenceAtom, hvoTest3));
			HVO hvoNewSeq;
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 1, &hvoNewSeq));

			CheckHr(hr = m_qsda->EndUndoTask());

			// DEBUG
			//hr = m_qsda->SetString(hvoTest1, kflidTester_String, qtss1);

			// Second group of actions.
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));

			StrUni stuSecond(L"testUndoRedo:UndoTask2");
			ITsStringPtr qtss2;
			ITsStringPtr qtss2_msa;
			CheckHr(hr = m_qtsf->MakeString(stuSecond.Bstr(), wsEng, &qtss2));
			CheckHr(hr = m_qtsf->MakeString(stuSecond.Bstr(), wsEng, &qtss2_msa));
			CheckHr(hr = m_qsda->SetString(hvoTest, kflidTester_String, qtss2));
			CheckHr(hr = m_qsda->SetMultiStringAlt(hvoTest, kflidTester_MultiString, wsEng, qtss2_msa));

			HVO hvoTemp = hvoTest4;
			CheckHr(hr = m_qsda->Replace(hvoTest, kflidTester_ReferenceSequence, 0, 2, &hvoTemp, 1));
			HVO hvoNewAtom;
			CheckHr(hr = m_qsda->EndUndoTask());

			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr())); // 3rd action
			// This deletes hvoTest4, both from kflidTester_OwningAtom, and ALSO
			// from kflidTester_ReferenceSequence!
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningAtom, -2, &hvoNewAtom));

			CheckHr(hr = m_qsda->EndUndoTask());

			// Verify that deleting the owned object also deleted the reference.
			VerifyObjSeq(hvoTest, kflidTester_ReferenceSequence, &hvoTest3, 1);

			// DEBUG
			//hr = m_qsda->SetString(hvoTest2, kflidTester_String, qtss2);

			// Fourth group.
			const int kInt3 = 99;
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->SetInt(hvoTest, kflidTester_Integer, kInt3));
			CheckHr(hr = m_qsda->EndUndoTask());

			// Now Undo once (group 4).
			UndoResult ures;
			CheckHr(hr = m_qacth->Undo(&ures));

			// Verify a few things not changed by one Undo
			int n;
			ITsStringPtr qtss;
			ComBool fEquals;
			bool equals;

			CheckHr(hr = m_qsda->get_StringProp(hvoTest, kflidTester_String, &qtss));
			// DEBUG
			//hr = m_qsda->SetString(hvoTest3, kflidTester_String, qtss);
			CheckHr(hr = qtss->Equals(qtss2, &fEquals));
			equals = bool(fEquals);
			unitpp::assert_true("1st undo did not affect string", equals);

			CheckHr(hr = m_qsda->get_MultiStringAlt(hvoTest, kflidTester_MultiString, wsEng, &qtss));
			CheckHr(hr = qtss->Equals(qtss2_msa, &fEquals));
			unitpp::assert_true("msa not reset by 1st undo", bool(fEquals));

			// And the one thing that is undone.
			CheckHr(hr = m_qsda->get_IntProp(hvoTest, kflidTester_Integer, &n));
			unitpp::assert_eq("int reset", kInt_init, n);

			CheckHr(hr = m_qacth->Undo(&ures)); // group 3, the insert object that also deletes one

			// The reference should have come back.
			// (But, as yet, we aren't restoring the cache, so clear it.)
			HVO rghvoRefSeqOther[] = {hvoTest4, hvoTest3};
			VerifyObjSeq(hvoTest, kflidTester_ReferenceSequence, rghvoRefSeqOther, 2);

			// Third Undo.
			CheckHr(hr = m_qacth->Undo(&ures));

			// These should be in the state set by the first group of actions.
			CheckHr(hr = m_qsda->get_StringProp(hvoTest, kflidTester_String, &qtss));
			CheckHr(hr = qtss->Equals(qtss1, &fEquals));
			unitpp::assert_true("string 2nd undo", bool(fEquals));

			HVO hvo;
			CheckHr(hr = m_qsda->get_ObjectProp(hvoTest, kflidTester_ReferenceAtom, &hvo));
			unitpp::assert_eq("ref atom redo", hvoTest2, hvo);
			CheckHr(hr = m_qsda->get_ObjectProp(hvoTest2, kflidTester_ReferenceAtom, &hvo));
			unitpp::assert_eq("ref atom redo", hvoTest3, hvo);
			HVO rghvoOwn2[] = {hvoTest1, hvoNewSeq, hvoTest2, hvoTest3};
			VerifyObjSeq(hvoTest, kflidTester_OwningSequence, rghvoOwn2, 4);

			// And these back in their initial states.
			CheckHr(hr = m_qsda->get_MultiStringAlt(hvoTest, kflidTester_MultiString, wsEng, &qtss));
			CheckHr(hr = qtss->Equals(qtss_init, &fEquals));
			unitpp::assert_true("msa reset", bool(fEquals));
			CheckHr(hr = m_qsda->get_ObjectProp(hvoTest, kflidTester_OwningAtom, &hvo));
			// JIRA [LT-1173]
			unitpp::assert_eq("own atom reset", hvoTest4, hvo);

			// Fourth Undo.
			CheckHr(hr = m_qacth->Undo(&ures));
			// All values back in initial state.
			CheckHr(hr = m_qsda->get_StringProp(hvoTest, kflidTester_String, &qtss));
			CheckHr(hr = qtss->Equals(qtss_init, &fEquals));
			unitpp::assert_true("string reset", bool(fEquals));
			CheckHr(hr = m_qsda->get_IntProp(hvoTest, kflidTester_Integer, &n));
			unitpp::assert_eq("int reset", kInt_init, n);
			CheckHr(hr = m_qsda->get_MultiStringAlt(hvoTest, kflidTester_MultiString, wsEng, &qtss));
			CheckHr(hr = qtss->Equals(qtss_init, &fEquals));
			unitpp::assert_true("msa reset", bool(fEquals));
			CheckHr(hr = m_qsda->get_ObjectProp(hvoTest, kflidTester_ReferenceAtom, &hvo));
			unitpp::assert_eq("ref atom reset", hvoTest1, hvo);
			CheckHr(hr = m_qsda->get_ObjectProp(hvoTest2, kflidTester_ReferenceAtom, &hvo));
			unitpp::assert_eq("empty ref atom reset", 0, hvo);		// REVISIT: replace 0 with variable
			CheckHr(hr = m_qsda->get_ObjectProp(hvoTest, kflidTester_OwningAtom, &hvo));
			// JIRA [LT-1173]
			unitpp::assert_eq("own atom reset", hvoTest4, hvo);
			VerifyObjSeq(hvoTest, kflidTester_ReferenceSequence, rghvoFirst, 3);
			VerifyObjSeq(hvoTest, kflidTester_OwningSequence, rghvoFirst, 3);

			// Now Redo. This should put objects in the state after the first group of actions
			CheckHr(hr = m_qacth->Redo(&ures));
			CheckHr(hr = m_qsda->get_StringProp(hvoTest, kflidTester_String, &qtss));
			CheckHr(hr = qtss->Equals(qtss1, &fEquals));
			unitpp::assert_true("string redo", bool(fEquals));
			CheckHr(hr = m_qsda->get_ObjectProp(hvoTest, kflidTester_ReferenceAtom, &hvo));
			unitpp::assert_eq("ref atom redo", hvoTest2, hvo);
			CheckHr(hr = m_qsda->get_ObjectProp(hvoTest2, kflidTester_ReferenceAtom, &hvo));
			unitpp::assert_eq("empty ref atom redo", hvoTest3, hvo);
			VerifyObjSeq(hvoTest, kflidTester_OwningSequence, rghvoOwn2, 4);

			// Redo second group
			CheckHr(hr = m_qacth->Redo(&ures));
			CheckHr(hr = m_qsda->get_StringProp(hvoTest, kflidTester_String, &qtss));
			CheckHr(hr = qtss->Equals(qtss2, &fEquals));
			unitpp::assert_true("2nd string redo", bool(fEquals));
			CheckHr(hr = m_qsda->get_MultiStringAlt(hvoTest, kflidTester_MultiString, wsEng, &qtss));
			CheckHr(hr = qtss->Equals(qtss2_msa, &fEquals));
			unitpp::assert_true("2nd redo msa", bool(fEquals));
			VerifyObjSeq(hvoTest, kflidTester_ReferenceSequence, rghvoRefSeqOther, 2);

			// REdo third (insert/delete object) group
			CheckHr(hr = m_qacth->Redo(&ures));

			CheckHr(hr = m_qsda->get_ObjectProp(hvoTest, kflidTester_OwningAtom, &hvo));
			unitpp::assert_eq("own atom redo", hvoNewAtom, hvo);
			VerifyObjSeq(hvoTest, kflidTester_ReferenceSequence, &hvoTest3, 1);

			// Redo final action.
			CheckHr(hr = m_qacth->Redo(&ures));
			CheckHr(hr = m_qsda->get_StringProp(hvoTest, kflidTester_String, &qtss));
			CheckHr(hr = qtss->Equals(qtss2, &fEquals));
			unitpp::assert_true("3rd redo did not affect string", bool(fEquals));
			CheckHr(hr = m_qsda->get_IntProp(hvoTest, kflidTester_Integer, &n));
			unitpp::assert_eq("int redo", kInt3, n);

		}


		// This method checks the db for the passed in object id and asserts based on the
		// fShouldExis parameter: where true is it should be present and
		// false is where it should NOT be present.
		// If these tests are applied to a non-database cache,
		// this method will also need to change.
		// Also clears info about the object.
		void ConfirmObjectDBExistence(HVO hvo, bool fShouldExist)
		{
			CheckHr(m_qcda->ClearInfoAbout(hvo, kciaRemoveObjectAndOwnedInfo));
			StrUni stuSql;
			stuSql.Format(L"select id from CmObject where id = %d", hvo);
			IOleDbCommandPtr qodc;
			CheckHr(m_qode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuSql.Bstr(), knSqlStmtSelectWithOneRowset));

			// there shouldn't be any rows if the object doesn't exist
			ComBool fMoreRows;
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			if (fShouldExist != (bool) fMoreRows)
				unitpp::assert_eq("ConfirmObjectNonexistence failed", fShouldExist, (bool)fMoreRows);
		}

		// This method is called at points where it would be appropriate to verify that an object is really
		// deleted from the database. Currently there is no straightforward way to verify this through the
		// interface, so we go direct to the database. If these tests are applied to a non-database cache,
		// this method will also need to change.
		// Also clears info about the object.
		void ConfirmObjectNonexistence(HVO hvo)
		{
			ConfirmObjectDBExistence(hvo, false);
		}

		// This method is called at points where it would be appropriate to verify that an object is really
		// in the database. Currently there is no straightforward way to verify this through the
		// interface, so we go direct to the database. If these tests are applied to a non-database cache,
		// this method will also need to change.
		// Also clears info about the object.
		void ConfirmObjectExistence(HVO hvo)
		{
			ConfirmObjectDBExistence(hvo, true);
		}

		// This tests that we can delete an object (by means of Undo) and that after undoing,
		// we get the same ID back and can proceed to restore one of its properties by a further Redo.
		// (It thus guards against the possibility that Redo creates a totally new object with
		// a different ID, in which case, the Redo setting the property, using the old HVO, would
		// have a bad object ID and fail.)
		void testUndoMakeNewObjectAtomic()
		{
			// Create an initial Tester object in the database as it's not owned by anyone...
			HVO hvoTest = CreateNewTesterObjectInDB();

			// Create an initial Tester object in the database as it's not owned by anyone...
			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");
			HVO hvoTest1;
			int oldn, n, newn = 77;

			// first undoable task
			// create an object
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			HRESULT hr;
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningAtom, -2, &hvoTest1));
			int val = 456; // seed for initializing data
			int rgws[2];
			CheckHr(hr = m_qwsf->get_UserWs(&rgws[0]));
			StrUni stuFr(L"fr");
			CheckHr(hr = m_qwsf->GetWsFromStr(stuFr.Bstr(), &rgws[1]));

			TestObjectVerifier tov1(hvoTest1, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->get_IntProp(hvoTest1, kflidTester_Integer, &oldn));
			CheckHr(m_qsda->EndUndoTask());

			// next undoable task
			// set an integer property
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->SetInt(hvoTest1, kflidTester_Integer, newn));
			CheckHr(m_qsda->EndUndoTask());


			// Do two cycles of Undo/Redo
			UndoResult ures;
			for (int i = 0; i < 2; i++)
			{
				// Now Undo both
				CheckHr(m_qacth->Undo(&ures));
				CheckHr(m_qsda->get_IntProp(hvoTest1, kflidTester_Integer, &n));
				unitpp::assert_eq("int reset", oldn, n);
				tov1.Verify(-1, -1); // Everything should be as it was.
				CheckHr(m_qacth->Undo(&ures));

				CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveObjectAndOwnedInfo));

				// Now Redo both
				CheckHr(m_qacth->Redo(&ures));
				tov1.Verify(-1, -1); // Everything should be as it was.
				CheckHr(m_qacth->Redo(&ures));

				CheckHr(m_qsda->get_IntProp(hvoTest1, kflidTester_Integer, &n));
				unitpp::assert_eq("int redo", newn, n);
			}

			// Now try making another object in the same property. The old one should be deleted.
			// This currently fails...need to fix the real code.
			HVO hvoTest2;
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningAtom, -2, &hvoTest2));

			TestObjectVerifier tov2(hvoTest2, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->EndUndoTask());

			tov2.Verify(); // along with all its properties!
			ConfirmObjectNonexistence(hvoTest1); // deleted because we overwrote it
			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectNonexistence(hvoTest2);
			ConfirmObjectExistence(hvoTest1); // it came back!
			CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveObjectInfoOnly));
			tov1.Verify(kflidTester_Integer); // along with all its properties!
			CheckHr(m_qsda->get_IntProp(hvoTest1, kflidTester_Integer, &n));
			unitpp::assert_eq("int redo", newn, n);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectNonexistence(hvoTest1);
			ConfirmObjectExistence(hvoTest2); // it came back!
			CheckHr(m_qcda->ClearInfoAbout(hvoTest2, kciaRemoveObjectInfoOnly));
			tov2.Verify(); // along with all its properties!
		}
		// This one similarly tests that when we Undo deleting an object, Undo re-creates it
		// with the same ID, rather than creating a brand new object.
		void testDeleteObjOwnerAtomic()
		{
			// Create an initial Tester object in the database as it's not owned by anyone...
			HVO hvoTest = CreateNewTesterObjectInDB();

			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");

			// Save the initial (everything null) state of the object.
			int rgws[2];
			HRESULT hr;
			CheckHr(hr= m_qwsf->get_UserWs(&rgws[0]));
			StrUni stuFr(L"fr");
			CheckHr(hr = m_qwsf->GetWsFromStr(stuFr.Bstr(), &rgws[1]));
			int val = 908;
			TestObjectVerifier tovOrig(hvoTest, m_qsda, rgws, 2, m_qtsf, val);
			HVO hvoAtom;
			CheckHr(m_qsda->get_ObjectProp(hvoTest, kflidTester_OwningAtom, &hvoAtom));

			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			// Initialize all properties to arbitrary values.
			TestObjectVerifier tovAtom(hvoAtom, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->EndUndoTask());

			// delete the object
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->DeleteObjOwner(hvoTest, hvoAtom, kflidTester_OwningAtom, -2));
			CheckHr(m_qsda->EndUndoTask());
			// Get rid of all traces.
			CheckHr(m_qcda->ClearInfoAbout(hvoAtom, kciaRemoveAllObjectInfo));

			// Make sure it's really gone.
			ConfirmObjectNonexistence(hvoAtom);

			// Now Undo the first action; the object and the property we set should both come back.
			UndoResult ures;
			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectExistence(hvoAtom);
			tovAtom.Verify(); // Undo delete brought all the props back.
			HVO hvoAtom2;
			CheckHr(m_qsda->get_ObjectProp(hvoTest, kflidTester_OwningAtom, &hvoAtom2));
			unitpp::assert_eq("atomic object restored", hvoAtom, hvoAtom2);

			CheckHr(m_qacth->Redo(&ures)); // Redo the deletion
			CheckHr(m_qcda->ClearInfoAbout(hvoAtom, kciaRemoveAllObjectInfo));
			ConfirmObjectNonexistence(hvoAtom);
			CheckHr(m_qsda->get_ObjectProp(hvoTest, kflidTester_OwningAtom, &hvoAtom2));
			unitpp::assert_eq("atomic object restored", 0, hvoAtom2);
		}

		// This one similarly tests that when we Undo deleting an object, Undo re-creates it
		// with the same ID, rather than creating a brand new object.
		void testDeleteObjectAtomic()
		{
			// Create an initial Tester object in the database as it's not owned by anyone...
			HVO hvoTest = CreateNewTesterObjectInDB();

			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");
			HVO hvoTest1;
			int oldn;

			// create an object
			HRESULT hr;
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningAtom, -2,
				&hvoTest1));
			CheckHr(m_qsda->get_IntProp(hvoTest1, kflidTester_Integer, &oldn));

			// Save the initial (everything null) state of the object.
			int rgws[2];
			CheckHr(hr = m_qwsf->get_UserWs(&rgws[0]));
			StrUni stuFr(L"fr");
			CheckHr(hr = m_qwsf->GetWsFromStr(stuFr.Bstr(), &rgws[1]));
			TestObjectVerifier tovOrig(hvoTest1, m_qsda, rgws, 2);

			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			int val = 654;
			// Initialize all properties to arbitrary values.
			TestObjectVerifier tov1(hvoTest1, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->EndUndoTask());

			HVO hvoAtom;
			CheckHr(m_qsda->get_ObjectProp(hvoTest1, kflidTester_OwningAtom, &hvoAtom));
			HVO hvoSeq1;
			CheckHr(m_qsda->get_VecItem(hvoTest1, kflidTester_OwningSequence, 0, &hvoSeq1));
			HVO hvoCollection1;
			CheckHr(m_qsda->get_VecItem(hvoTest1, kflidTester_OwningCollection, 0, &hvoCollection1));

			// next undoable task
			// delete the object
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->DeleteObj(hvoTest1));
			CheckHr(m_qsda->EndUndoTask());
			// Get rid of all traces.
			CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveAllObjectInfo));

			// Make sure it's really gone.
			// Enhance: possibly try to make sure it's really gone from database.
			ConfirmObjectNonexistence(hvoTest1);
			ConfirmObjectNonexistence(hvoAtom);
			ConfirmObjectNonexistence(hvoSeq1);
			ConfirmObjectNonexistence(hvoCollection1);

//			ComBool fPresent;
//			m_qsda->IsPropInCache(hvoTest1, kflidTester_Integer, kcptInteger, &fPresent);
//			unitpp::assert_eq("object deleted", false, (bool)fPresent);

			// Now Undo the first action; the object and the property we set should both come back.
			UndoResult ures;
			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveObjectAndOwnedInfo));
			ConfirmObjectExistence(hvoTest1);
			ConfirmObjectExistence(hvoAtom);
			ConfirmObjectExistence(hvoSeq1);
			ConfirmObjectExistence(hvoCollection1);

			tov1.Verify(-1, -1); // Undo delete brought all the props back.
			// After undoing the second time (original first action), the integer prop's value is back to 0.
			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveObjectAndOwnedInfo));
			tovOrig.Verify(-1, -1); // and by the way we can undo setting all properties.

			// Now Redo both
			CheckHr(m_qacth->Redo(&ures)); // Redo the property setting.
			CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveObjectAndOwnedInfo));
			tov1.Verify(-1, -1); // Redo delete brought all the props back.
			CheckHr(m_qacth->Redo(&ures)); // Redo the deletion
			CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveAllObjectInfo));
			ConfirmObjectNonexistence(hvoTest1); // and so it's gone.
			ConfirmObjectNonexistence(hvoAtom);
			ConfirmObjectNonexistence(hvoSeq1);
			ConfirmObjectNonexistence(hvoCollection1);
		}

		// Other cases of DeleteObject
		void testDeleteObjOther()
		{
			HVO hvoTest = CreateNewTesterObjectInDB();

			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");

			// Save the initial (everything null) state of the object.
			int rgws[2];
			HRESULT hr;
			CheckHr(hr = m_qwsf->get_UserWs(&rgws[0]));
			StrUni stuFr(L"fr");
			CheckHr(hr = m_qwsf->GetWsFromStr(stuFr.Bstr(), &rgws[1]));
			int val = 629;
			TestObjectVerifier tovOrig(hvoTest, m_qsda, rgws, 2, m_qtsf, val);

			//----------------------------------------------------------
			// Delete an unowned object.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->DeleteObj(hvoTest));
			unitpp::assert_eq("DeleteObj succeeded", S_OK, hr);
			CheckHr(m_qsda->EndUndoTask());
			ConfirmObjectNonexistence(hvoTest); // and so it's gone.

			UndoResult ures;
			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qcda->ClearInfoAbout(hvoTest, kciaRemoveObjectAndOwnedInfo));
			ConfirmObjectExistence(hvoTest);
			tovOrig.Verify(); // check all properties came back

			CheckHr(m_qacth->Redo(&ures)); // Redo the deletion
			CheckHr(m_qcda->ClearInfoAbout(hvoTest, kciaRemoveAllObjectInfo));
			ConfirmObjectNonexistence(hvoTest); // and so it's gone.

			//----------------------------------------------------------
			// Use DeleteObj on a Sequence. This is unusual (it won't clean up
			// anything in the cache about the owner), but do a minimal test
			// to make sure it isn't broken.
			CheckHr(m_qacth->Undo(&ures)); // get it back again to use in other tests.
			HVO hvoTest1;
			CheckHr(m_qsda->get_VecItem(hvoTest, kflidTester_OwningSequence, 0, &hvoTest1));
			TestObjectVerifier tov1(hvoTest1, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->DeleteObj(hvoTest1));
			unitpp::assert_eq("DeleteObj succeeded", S_OK, hr);
			CheckHr(m_qsda->EndUndoTask());
			ConfirmObjectNonexistence(hvoTest1); // and so it's gone.

			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveObjectAndOwnedInfo));
			ConfirmObjectExistence(hvoTest1);
			tov1.Verify(); // check all properties came back

			CheckHr(m_qacth->Redo(&ures)); // Redo the deletion
			CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveAllObjectInfo));
			ConfirmObjectNonexistence(hvoTest1); // and so it's gone.

			CheckHr(m_qacth->Undo(&ures)); // get it back again to use in other tests.

			//----------------------------------------------------------
			// Use DeleteObj on the second (and last) object in a Sequence.
			// This can be a special case because there is no object to insert
			// before on restoring.
			HVO hvoTest3;
			CheckHr(m_qsda->get_VecItem(hvoTest, kflidTester_OwningSequence, 1, &hvoTest3));
			TestObjectVerifier tov3(hvoTest3, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->DeleteObj(hvoTest3));
			unitpp::assert_eq("DeleteObj succeeded", S_OK, hr);
			CheckHr(m_qsda->EndUndoTask());
			ConfirmObjectNonexistence(hvoTest3); // and so it's gone.

			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qcda->ClearInfoAbout(hvoTest3, kciaRemoveObjectAndOwnedInfo));
			ConfirmObjectExistence(hvoTest3);
			tov3.Verify(); // check all properties came back

			CheckHr(m_qacth->Redo(&ures)); // Redo the deletion
			CheckHr(m_qcda->ClearInfoAbout(hvoTest3, kciaRemoveAllObjectInfo));
			ConfirmObjectNonexistence(hvoTest3); // and so it's gone.
			CheckHr(m_qacth->Undo(&ures)); // get it back again to use in other tests.

			//----------------------------------------------------------
			// Use DeleteObj on a Collection. This is unusual (it won't clean up
			// anything in the cache about the owner), but do a minimal test
			// to make sure it isn't broken.
			HVO hvoTest2;
			CheckHr(m_qsda->get_VecItem(hvoTest, kflidTester_OwningCollection, 0, &hvoTest2));
			TestObjectVerifier tov2(hvoTest2, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->DeleteObj(hvoTest2));
			unitpp::assert_eq("DeleteObj succeeded", S_OK, hr);
			CheckHr(m_qsda->EndUndoTask());
			ConfirmObjectNonexistence(hvoTest2); // and so it's gone.

			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qcda->ClearInfoAbout(hvoTest2, kciaRemoveObjectAndOwnedInfo));
			ConfirmObjectExistence(hvoTest2);
			tov2.Verify(); // check all properties came back

			CheckHr(m_qacth->Redo(&ures)); // Redo the deletion
			CheckHr(m_qcda->ClearInfoAbout(hvoTest2, kciaRemoveAllObjectInfo));
			ConfirmObjectNonexistence(hvoTest2); // and so it's gone.

		}

		static const int kMaxVecSize = 100;
		// Verify the expected value of a vector property.
		void VerifyVec(HVO hvoParent, int flid, HVO * prghvoExpected, int chvoExpected)
		{
			// Verify that the cache got updated...
			int chvo;
			HVO rghvo[kMaxVecSize];
			CheckHr(m_qsda->VecProp(hvoParent, flid, kMaxVecSize, &chvo, rghvo));
			unitpp::assert_true("manageable #objs", chvo < kMaxVecSize);
			if (chvoExpected != chvo)
				unitpp::assert_eq("cached vector size", chvoExpected, chvo);
			for (int i = 0; i < chvo; i++)
				if (prghvoExpected[i] != rghvo[i])
					unitpp::assert_eq("cached vector item", prghvoExpected[i], rghvo[i]);
			// and also that the database really did, too.
			CheckHr(m_qcda->ClearInfoAbout(hvoParent, kciaRemoveObjectInfoOnly));
			CheckHr(m_qsda->VecProp(hvoParent, flid, kMaxVecSize, &chvo, rghvo));
			unitpp::assert_true("manageable #objs", chvo < kMaxVecSize);
			if (chvoExpected != chvo)
				unitpp::assert_eq("vector size", chvoExpected, chvo);
			for (int i = 0; i < chvo; i++)
				if (prghvoExpected[i] != rghvo[i])
					unitpp::assert_eq("vector item", prghvoExpected[i], rghvo[i]);
		}

		void SortVec(HVO * prghvo, int chvo)
		{
			if (chvo > 1)
				std::sort<HVO *>(prghvo, prghvo + chvo);
		}

		void VerifyCollection(HVO hvoParent, int flid, HVO * prghvoExpected, int chvoExpected)
		{
			int chvo;
			HVO rghvo[kMaxVecSize];
			CheckHr(m_qsda->VecProp(hvoParent, flid, kMaxVecSize, &chvo, rghvo));
			unitpp::assert_true("manageable #objs", chvo < kMaxVecSize);
			if (chvoExpected != chvo)
				unitpp::assert_eq("collection size", chvoExpected, chvo);
			SortVec(rghvo, chvo);
			for (int i = 0; i < chvo; i++)
				if (prghvoExpected[i] != rghvo[i])
					unitpp::assert_eq("collection item", prghvoExpected[i], rghvo[i]);
		}

		// Try deleting from a collection.
		// Create test owning object.
		// Create three owned objects in a collection.
		// Delete the first, undo, redo, undo, delete second, undo, redo, undo, delete third, undo, redo,
		// Delete remaining two, undo, redo.
		void testInsertDeleteObjectCollection()
		{
			// Create an initial Tester object in the database as it's not owned by anyone...
			HVO hvoTest = CreateNewTesterObjectInDB();

			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");
			HVO hvoTest1, hvoTest2, hvoTest3, hvoTest4;

			// Save the initial (everything null) state of the object.
			int rgws[2];
			HRESULT hr;
			CheckHr(hr= m_qwsf->get_UserWs(&rgws[0]));
			StrUni stuFr(L"fr");
			CheckHr(hr = m_qwsf->GetWsFromStr(stuFr.Bstr(), &rgws[1]));
			TestObjectVerifier tovOrig(hvoTest, m_qsda, rgws, 2);

			// Make one object in the collection, and initialize it.
			int val = 1234;
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningCollection, 0, &hvoTest1));
			TestObjectVerifier tov1(hvoTest1, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->EndUndoTask());

			UndoResult ures;
			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveObjectAndOwnedInfo));

			ConfirmObjectNonexistence(hvoTest1);

			int chvo;
			CheckHr(m_qsda->get_VecSize(hvoTest, kflidTester_OwningCollection, & chvo));
			unitpp::assert_eq("undo create in col removes from col", 0, chvo);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectExistence(hvoTest1);
			tov1.Verify(-1, -1);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, &hvoTest1, 1);

			// Make another at the end.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningCollection, 1, &hvoTest2));
			CheckHr(m_qsda->EndUndoTask());

			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qcda->ClearInfoAbout(hvoTest2, kciaRemoveObjectAndOwnedInfo));

			ConfirmObjectNonexistence(hvoTest2);
			ConfirmObjectExistence(hvoTest1); // But we only got rid of one!
			VerifyCollection(hvoTest, kflidTester_OwningCollection, &hvoTest1, 1);

			CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveObjectAndOwnedInfo));
			tov1.Verify(-1, -1); // Test 1 is still present and uncorrupted.

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectExistence(hvoTest2);
			HVO rghvoExpected12[] = {hvoTest1, hvoTest2};
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected12, 2);

			// Now try inserting at the beginning.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningCollection, 0, &hvoTest3));
			CheckHr(m_qsda->EndUndoTask());

			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectNonexistence(hvoTest3);
			ConfirmObjectExistence(hvoTest1); // But we only got rid of one!
			ConfirmObjectExistence(hvoTest2);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected12, 2);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectExistence(hvoTest3);
			HVO rghvoExpected312[] = {hvoTest1, hvoTest2, hvoTest3}; // nb in order or creation!
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected312, 3);

			// And finally insert in middle.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningCollection, 1, &hvoTest4));
			CheckHr(m_qsda->EndUndoTask());

			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectNonexistence(hvoTest4);
			ConfirmObjectExistence(hvoTest1); // But we only got rid of one!
			ConfirmObjectExistence(hvoTest3);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected312, 3);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectExistence(hvoTest4);
			HVO rghvoExpected3412[] = {hvoTest1, hvoTest2, hvoTest3, hvoTest4};
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected3412, 4);


			// initializing 3 and 4
			// delete from the middle (1)
			TestObjectVerifier tov2(hvoTest2, m_qsda, rgws, 2, m_qtsf, val);
			TestObjectVerifier tov3(hvoTest3, m_qsda, rgws, 2, m_qtsf, val);
			TestObjectVerifier tov4(hvoTest4, m_qsda, rgws, 2, m_qtsf, val);

			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->DeleteObjOwner(hvoTest, hvoTest1, kflidTester_OwningCollection, -1));
			CheckHr(m_qsda->EndUndoTask());
			ConfirmObjectNonexistence(hvoTest1);
			ConfirmObjectExistence(hvoTest2); // But we only got rid of one!
			ConfirmObjectExistence(hvoTest3);
			HVO rghvoExpected342[] = {hvoTest2, hvoTest3, hvoTest4};
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected342, 3);

			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectExistence(hvoTest1);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected3412, 4);
			tov1.Verify(-1, -1);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectNonexistence(hvoTest1);
			ConfirmObjectExistence(hvoTest2); // But we only got rid of one!
			ConfirmObjectExistence(hvoTest4);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected342, 3);

			// now delete from end hvoTest2
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->DeleteObjOwner(hvoTest, hvoTest2, kflidTester_OwningCollection, -1));
			CheckHr(m_qsda->EndUndoTask());
			ConfirmObjectNonexistence(hvoTest2);
			ConfirmObjectExistence(hvoTest3); // But we only got rid of one!
			ConfirmObjectExistence(hvoTest4);
			HVO rghvoExpected34[] = {hvoTest3, hvoTest4};
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected34, 2);

			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectExistence(hvoTest2);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected342, 3);
			tov2.Verify(-1, -1);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectNonexistence(hvoTest2);
			ConfirmObjectExistence(hvoTest3); // But we only got rid of one!
			ConfirmObjectExistence(hvoTest4);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected34, 2);


			// now delete from the begining hvoTest3
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->DeleteObjOwner(hvoTest, hvoTest3, kflidTester_OwningCollection, -1));
			CheckHr(m_qsda->EndUndoTask());
			ConfirmObjectNonexistence(hvoTest3);
			ConfirmObjectExistence(hvoTest4);
			HVO rghvoExpected4[] = {hvoTest4};
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected4, 1);

			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectExistence(hvoTest3);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected34, 2);
			tov3.Verify(-1, -1);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectNonexistence(hvoTest3);
			ConfirmObjectExistence(hvoTest4);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected4, 1);


			// now delete the last one - hvoTest4
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->DeleteObjOwner(hvoTest, hvoTest4, kflidTester_OwningCollection, -1));
			CheckHr(m_qsda->EndUndoTask());
			ConfirmObjectNonexistence(hvoTest4);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, NULL, 0);

			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectExistence(hvoTest4);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected4, 1);
			tov4.Verify(-1, -1);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectNonexistence(hvoTest4);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, NULL, 0);

			// sequence is now empty

			tovOrig.Verify(kflidTester_OwningCollection, -1);
		}


		//-- Move one object to a diffferent property of a different object
		//-- Move one object to the same property of the same object.
		//	-- earlier
		//	-- later
		//-- Move to a different property of the same object
		//-- Move to the same property of a different object.
		//-- Destination
		//	-- start
		//	-- middle
		//	-- end
		//-- Source
		//	-- start
		//	-- middle
		//	-- end
		//-- Move several objects at once
		//-- try a destination property that is empty
		void testMoveOwnSeq()
		{
			// Create an initial Tester object in the database as it's not owned by anyone...
			HVO hvoTest = CreateNewTesterObjectInDB();
			HVO hvoTestOther = CreateNewTesterObjectInDB();

			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");
			HVO hvoTest1, hvoTest2, hvoTest3, hvoTest4;

			// Save the initial (everything null) state of the object.
			int rgws[2];
			HRESULT hr;
			CheckHr(hr= m_qwsf->get_UserWs(&rgws[0]));
			StrUni stuFr(L"fr");
			CheckHr(hr = m_qwsf->GetWsFromStr(stuFr.Bstr(), &rgws[1]));
			TestObjectVerifier tovOrigTest(hvoTest, m_qsda, rgws, 2);
			TestObjectVerifier tovOrigTestOther(hvoTestOther, m_qsda, rgws, 2);

			// Make 4 objects in the sequence - initializing each one along the way
			int val = 21;
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 0, &hvoTest1));
			TestObjectVerifier tov1(hvoTest1, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->EndUndoTask());
			// Make another at the end.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 1, &hvoTest2));
			TestObjectVerifier tov2(hvoTest2, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->EndUndoTask());
			// Now try inserting at the beginning.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 0, &hvoTest3));
			TestObjectVerifier tov3(hvoTest3, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->EndUndoTask());
			// And finally insert in middle.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 1, &hvoTest4));
			TestObjectVerifier tov4(hvoTest4, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->EndUndoTask());
			// order is 3412 in the seq

			// now move seq object from hvoTest to hvoTestOther
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->MoveOwnSeq(hvoTest, kflidTester_OwningSequence, 2, 2, hvoTestOther, kflidTester_OwningSequence, 0));
			CheckHr(m_qsda->EndUndoTask());

			// make sure the previous owner only has three elements
			HVO rghvoExpected342[] = {hvoTest3, hvoTest4, hvoTest2};
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected342, 3);
			tovOrigTest.Verify(kflidTester_OwningSequence, -1);	// all but owning seq the same

			// make sure the new owner only has one element
			HVO rghvoExpected1[] = {hvoTest1};
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, rghvoExpected1, 1);
			tovOrigTestOther.Verify(kflidTester_OwningSequence, -1);	// all but owning seq the same

			// now undo the move seq object and make sure things are good still
			UndoResult ures;
			CheckHr(m_qacth->Undo(&ures));
			HVO rghvoExpected3412[] = {hvoTest3, hvoTest4, hvoTest1, hvoTest2};
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected3412, 4);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, NULL, 0);

			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected342, 3);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, rghvoExpected1, 1);

			//----------------------------------------------------------------------
			// Now move the last object in the source to the end of the destination.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->MoveOwnSeq(hvoTest, kflidTester_OwningSequence, 2, 2, hvoTestOther, kflidTester_OwningSequence, 1));
			CheckHr(m_qsda->EndUndoTask());

			// Check the expected property values and that nothing else changed.
			HVO rghvoExpected34[] = {hvoTest3, hvoTest4};
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected34, 2);
			tovOrigTest.Verify(kflidTester_OwningSequence, -1);	// all but owning seq the same
			HVO rghvoExpected12[] = {hvoTest1, hvoTest2};
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, rghvoExpected12, 2);
			tovOrigTestOther.Verify(kflidTester_OwningSequence, -1);	// all but owning seq the same

			// now undo the move seq object and make sure things are good still
			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected342, 3);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, rghvoExpected1, 1);

			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected34, 2);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, rghvoExpected12, 2);

			//----------------------------------------------------------------------
			// Now move them all back, so we have four in hvoTest to work with.
			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qacth->Undo(&ures));
			// And then try a move to a destination in the same object. Also tests moving the first object.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->MoveOwnSeq(hvoTest, kflidTester_OwningSequence, 0, 1, hvoTest, kflidTester_OwningSequence2, 0));
			CheckHr(m_qsda->EndUndoTask());

			// Check the expected property values and that nothing else changed.
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected12, 2);
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected34, 2);

			// now undo the move seq object and make sure things are good still
			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected3412, 4);
			VerifyVec(hvoTest, kflidTester_OwningSequence2, NULL, 0);

			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected12, 2);
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected34, 2);

			//----------------------------------------------------------------------
			// Move from start to middle; also tests removing everything.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			int chvo;
			CheckHr(m_qsda->get_VecSize(hvoTest, kflidTester_OwningSequence, &chvo)); // make sure prop in cache.
			CheckHr(m_qsda->get_VecSize(hvoTest, kflidTester_OwningSequence2, &chvo)); // make sure prop in cache.
			CheckHr(m_qsda->MoveOwnSeq(hvoTest, kflidTester_OwningSequence, 0, 1, hvoTest, kflidTester_OwningSequence2, 1));
			CheckHr(m_qsda->EndUndoTask());

			// Check the expected property values and that nothing else changed.
			VerifyVec(hvoTest, kflidTester_OwningSequence, NULL, 0);
			HVO rghvoExpected3124[] = {hvoTest3, hvoTest1, hvoTest2, hvoTest4 };
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected3124, 4);

			// now undo the move seq object and make sure things are good still
			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected12, 2);
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected34, 2);

			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, NULL, 0);
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected3124, 4);

			// Good point to verify that the objects themselves haven't been messed up.
			tov1.Verify();
			tov2.Verify();
			tov3.Verify();
			tov4.Verify();
			//----------------------------------------------------------------------
			// Move from ownseq2 to ownseq of other object (obj and prop both diff)
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->get_VecSize(hvoTest, kflidTester_OwningSequence, &chvo)); // make sure prop in cache.
			CheckHr(m_qsda->get_VecSize(hvoTest, kflidTester_OwningSequence2, &chvo)); // make sure prop in cache.
			CheckHr(m_qsda->MoveOwnSeq(hvoTest, kflidTester_OwningSequence2, 0, 0, hvoTestOther, kflidTester_OwningSequence, 0));
			CheckHr(m_qsda->EndUndoTask());

			// Check the expected property values and that nothing else changed.
			HVO rghvoExpected124[] = {hvoTest1, hvoTest2, hvoTest4 };
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected124, 3);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, &hvoTest3, 1);

			// now undo the move seq object and make sure things are good still
			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected3124, 4);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, NULL, 0);

			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected124, 3);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, &hvoTest3, 1);

			//----------------------------------------------------------------------
			// This tries moving to the start of a non-empty property.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->get_VecSize(hvoTestOther, kflidTester_OwningSequence, &chvo)); // make sure prop in cache.
			CheckHr(m_qsda->get_VecSize(hvoTest, kflidTester_OwningSequence2, &chvo)); // make sure prop in cache.
			CheckHr(m_qsda->MoveOwnSeq(hvoTest, kflidTester_OwningSequence2, 2, 2, hvoTestOther, kflidTester_OwningSequence, 0));
			CheckHr(m_qsda->EndUndoTask());

			// Check the expected property values and that nothing else changed.
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected12, 2);
			HVO rghvoExpected43[] = {hvoTest4, hvoTest3 };
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, rghvoExpected43, 2);

			// now undo the move seq object and make sure things are good still
			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected124, 3);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, &hvoTest3, 1);

			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected12, 2);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, rghvoExpected43, 2);

			//----------------------------------------------------------------------
			// Try moving to an earlier position in the same sequence.
			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qacth->Undo(&ures)); // back to seq 2, 3124
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->get_VecSize(hvoTest, kflidTester_OwningSequence2, &chvo)); // make sure prop in cache.
			CheckHr(m_qsda->MoveOwnSeq(hvoTest, kflidTester_OwningSequence2, 2, 2, hvoTest, kflidTester_OwningSequence2, 0));
			CheckHr(m_qsda->EndUndoTask());

			// Check the expected property values.
			HVO rghvoExpected2314[] = {hvoTest2, hvoTest3, hvoTest1, hvoTest4 };
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected2314, 4);

			// now undo the move seq object and make sure things are good still
			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected3124, 4);

			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected2314, 4);
			//----------------------------------------------------------------------
			// Try moving to a later position in the same sequence.
			CheckHr(m_qacth->Undo(&ures)); // back to seq 2, 3124
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected3124, 4);

			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->get_VecSize(hvoTest, kflidTester_OwningSequence2, &chvo)); // make sure prop in cache.
			CheckHr(m_qsda->MoveOwnSeq(hvoTest, kflidTester_OwningSequence2, 1, 1,
				hvoTest, kflidTester_OwningSequence2, 3));
			CheckHr(m_qsda->EndUndoTask());

			// Check the expected property values.
			HVO rghvoExpected3214[] = {hvoTest3, hvoTest2, hvoTest1, hvoTest4 };
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected3214, 4);

			// now undo the move seq object and make sure things are good still
			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected3124, 4);

			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected3214, 4);

			//----------------------------------------------------------------------
			// Try moving everything in a sequence to a completely empty property.
			VerifyVec(hvoTestOther, kflidTester_OwningSequence2, NULL, 0);
			CheckHr(m_qsda->get_VecSize(hvoTestOther, kflidTester_OwningSequence2, &chvo)); // make sure prop in cache.
			CheckHr(m_qsda->MoveOwnSeq(hvoTest, kflidTester_OwningSequence2, 0, 3,
				hvoTestOther, kflidTester_OwningSequence2, 0));

			VerifyVec(hvoTest, kflidTester_OwningSequence2, NULL, 0);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence2, rghvoExpected3214, 4);

			// now undo the move seq object and make sure things are good still
			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence2, rghvoExpected3214, 4);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence2, NULL, 0);

			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence2, NULL, 0);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence2, rghvoExpected3214, 4);
		}

		void VerifyObj(HVO hvoParent, int flid, HVO hvoExpected)
		{
			HVO hvo;
			CheckHr(m_qsda->get_ObjectProp(hvoParent, flid, &hvo));
			unitpp::assert_eq("cached obj prop", hvoExpected, hvo);
			CheckHr(m_qcda->ClearInfoAbout(hvoParent, kciaRemoveObjectInfoOnly));
			CheckHr(m_qsda->get_ObjectProp(hvoParent, flid, &hvo));
			unitpp::assert_eq("obj prop", hvoExpected, hvo);
		}

		// test move ownership from:
		//	-atom to atom
		//	-atom to collection
		//	-collection to atom
		//	-collection to collection
		//	-atom to sequence
		//	-collection to sequence
		//	-sequence to atom
		//	-sequence to collection
		//	-sequence to sequence
		void testMoveOwn()
		{
			// Create an initial Tester object in the database as it's not owned by anyone...
			HVO hvoTest = CreateNewTesterObjectInDB();
			HVO hvoTestOther = CreateNewTesterObjectInDB();

			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");
			HVO hvoTest1, hvoTest2, hvoTest3, hvoTest4;

			// Save the initial (everything null) state of the object.
			int rgws[2];
			HRESULT hr;
			CheckHr(hr= m_qwsf->get_UserWs(&rgws[0]));
			StrUni stuFr(L"fr");
			CheckHr(hr = m_qwsf->GetWsFromStr(stuFr.Bstr(), &rgws[1]));

			// init hvoTest
			int val = 21;
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningAtom, 0, &hvoTest1));
			TestObjectVerifier tov1(hvoTest1, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 0, &hvoTest2));
			TestObjectVerifier tov2(hvoTest2, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningCollection, 0, &hvoTest3));
			TestObjectVerifier tov3(hvoTest3, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningCollection, 0, &hvoTest4));
			TestObjectVerifier tov4(hvoTest4, m_qsda, rgws, 2, m_qtsf, val);

			TestObjectVerifier tovTest(hvoTest, m_qsda, rgws, 2);
			TestObjectVerifier tovTestOther(hvoTestOther, m_qsda, rgws, 2);

			// move atom to atom
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->MoveOwn(hvoTest, kflidTester_OwningAtom, hvoTest1, hvoTestOther, kflidTester_OwningAtom, 0));
			CheckHr(m_qsda->EndUndoTask());

			VerifyObj(hvoTestOther, kflidTester_OwningAtom, hvoTest1);
			tovTest.Verify(kflidTester_OwningAtom);
			VerifyObj(hvoTest, kflidTester_OwningAtom, 0);
			tovTestOther.Verify(kflidTester_OwningAtom);

			UndoResult ures;
			CheckHr(m_qacth->Undo(&ures));
			VerifyObj(hvoTest, kflidTester_OwningAtom, hvoTest1);
			VerifyObj(hvoTestOther, kflidTester_OwningAtom, 0);
			CheckHr(m_qacth->Redo(&ures));
			VerifyObj(hvoTest, kflidTester_OwningAtom, 0);
			VerifyObj(hvoTestOther, kflidTester_OwningAtom, hvoTest1);

			CheckHr(m_qacth->Undo(&ures));
			tovTest.Load();
			tovTestOther.Load();

			// move atom to collection
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->MoveOwn(hvoTest, kflidTester_OwningAtom, hvoTest1, hvoTestOther, kflidTester_OwningCollection, 0));
			CheckHr(m_qsda->EndUndoTask());

			HVO rghvoExpected1[] = {hvoTest1};
			VerifyCollection(hvoTestOther, kflidTester_OwningCollection, rghvoExpected1, 1);
			tovTestOther.Verify(kflidTester_OwningCollection);
			VerifyObj(hvoTest, kflidTester_OwningAtom, 0);
			tovTest.Verify(kflidTester_OwningAtom);

			CheckHr(m_qacth->Undo(&ures));
			VerifyCollection(hvoTestOther, kflidTester_OwningCollection, NULL, 0);
			VerifyObj(hvoTest, kflidTester_OwningAtom, hvoTest1);
			CheckHr(m_qacth->Redo(&ures));
			VerifyCollection(hvoTestOther, kflidTester_OwningCollection, rghvoExpected1, 1);
			VerifyObj(hvoTest, kflidTester_OwningAtom, 0);

			tovTest.Load();
			tovTestOther.Load();

			// move collection to collection
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->MoveOwn(hvoTest, kflidTester_OwningCollection, hvoTest3, hvoTestOther, kflidTester_OwningCollection, 0));
			CheckHr(m_qsda->EndUndoTask());

			HVO rghvoExpected13[] = {hvoTest1, hvoTest3};
			VerifyCollection(hvoTestOther, kflidTester_OwningCollection, rghvoExpected13, 2);
			tovTestOther.Verify(kflidTester_OwningCollection);
			HVO rghvoExpected4[] = {hvoTest4};
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected4, 1);
			tovTest.Verify(kflidTester_OwningCollection);

			CheckHr(m_qacth->Undo(&ures));
			HVO rghvoExpected34[] = {hvoTest3, hvoTest4};
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected34, 2);
			VerifyCollection(hvoTestOther, kflidTester_OwningCollection, rghvoExpected1, 1);
			CheckHr(m_qacth->Redo(&ures));
			VerifyCollection(hvoTestOther, kflidTester_OwningCollection, rghvoExpected13, 2);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected4, 1);

			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qacth->Undo(&ures));
			tov1.Verify();
			tov2.Verify();
			tov3.Verify();
			tov4.Verify();

			tovTest.Load();
			tovTestOther.Load();
			// move collection to atom
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->MoveOwn(hvoTest, kflidTester_OwningCollection, hvoTest4, hvoTestOther, kflidTester_OwningAtom, 0));
			CheckHr(m_qsda->EndUndoTask());

			HVO rghvoExpected3[] = {hvoTest3};
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected3, 1);
			tovTest.Verify(kflidTester_OwningCollection);
			VerifyObj(hvoTestOther, kflidTester_OwningAtom, hvoTest4);
			tovTestOther.Verify(kflidTester_OwningAtom);

			CheckHr(m_qacth->Undo(&ures));
			VerifyObj(hvoTestOther, kflidTester_OwningAtom, 0);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected34, 2);
			CheckHr(m_qacth->Redo(&ures));
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected3, 1);
			VerifyObj(hvoTestOther, kflidTester_OwningAtom, hvoTest4);

			tovTest.Load();
			tovTestOther.Load();
			// move atom to sequence
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->MoveOwn(hvoTestOther, kflidTester_OwningAtom, hvoTest4, hvoTest, kflidTester_OwningSequence, 1));
			CheckHr(m_qsda->EndUndoTask());

			HVO rghvoExpected24[] = {hvoTest2, hvoTest4};
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected24, 2);
			tovTest.Verify(kflidTester_OwningSequence);
			VerifyObj(hvoTestOther, kflidTester_OwningAtom, 0);
			tovTestOther.Verify(kflidTester_OwningAtom);

			CheckHr(m_qacth->Undo(&ures));
			HVO rghvoExpected2[] = {hvoTest2};
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected2, 1);
			VerifyObj(hvoTestOther, kflidTester_OwningAtom, hvoTest4);
			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected24, 2);
			VerifyObj(hvoTestOther, kflidTester_OwningAtom, 0);

			tovTest.Load();
			tovTestOther.Load();
			// move collection to sequence
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->MoveOwn(hvoTest, kflidTester_OwningCollection, hvoTest3, hvoTest, kflidTester_OwningSequence, 1));
			CheckHr(m_qsda->EndUndoTask());

			HVO rghvoExpected234[] = {hvoTest2, hvoTest3, hvoTest4};
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected234, 3);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, NULL, 0);

			CheckHr(m_qacth->Undo(&ures));
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected3, 1);
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected24, 2);
			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected234, 3);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, NULL, 0);

			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qacth->Undo(&ures));
			tov1.Verify();
			tov2.Verify();
			tov3.Verify();
			tov4.Verify();

			tovTest.Load();
			tovTestOther.Load();
			// move sequence to atom
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->MoveOwn(hvoTest, kflidTester_OwningSequence, hvoTest2, hvoTestOther, kflidTester_OwningAtom, 0));
			CheckHr(m_qsda->EndUndoTask());

			VerifyVec(hvoTest, kflidTester_OwningSequence, NULL, 0);
			tovTest.Verify(kflidTester_OwningSequence);
			VerifyObj(hvoTestOther, kflidTester_OwningAtom, hvoTest2);
			tovTestOther.Verify(kflidTester_OwningAtom);

			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected2, 1);
			VerifyObj(hvoTestOther, kflidTester_OwningAtom, 0);
			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, NULL, 0);
			VerifyObj(hvoTestOther, kflidTester_OwningAtom, hvoTest2);

			CheckHr(m_qacth->Undo(&ures));

			tovTest.Load();
			tovTestOther.Load();
			// move sequence to collection
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->MoveOwn(hvoTest, kflidTester_OwningSequence, hvoTest2, hvoTest, kflidTester_OwningCollection, 0));
			CheckHr(m_qsda->EndUndoTask());

			VerifyVec(hvoTest, kflidTester_OwningSequence, NULL, 0);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected234, 3);

			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected2, 1);
			VerifyCollection(hvoTestOther, kflidTester_OwningCollection, NULL, 0);
			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, NULL, 0);
			VerifyCollection(hvoTest, kflidTester_OwningCollection, rghvoExpected234, 3);

			CheckHr(m_qacth->Undo(&ures));

			tovTest.Load();
			tovTestOther.Load();
			// move sequence to sequence
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->MoveOwn(hvoTest, kflidTester_OwningSequence, hvoTest2, hvoTestOther, kflidTester_OwningSequence, 0));
			CheckHr(m_qsda->EndUndoTask());

			VerifyVec(hvoTest, kflidTester_OwningSequence, NULL, 0);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, rghvoExpected2, 1);

			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected2, 1);
			VerifyCollection(hvoTestOther, kflidTester_OwningSequence, NULL, 0);
			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_OwningSequence, NULL, 0);
			VerifyVec(hvoTestOther, kflidTester_OwningSequence, rghvoExpected2, 1);
		}


		// Try deleting from a sequence.
		// Create test owning object.
		// Create three owned objects in a sequence.
		// Delete the first, undo, redo, undo, delete second, undo, redo, undo, delete third, undo, redo,
		// Delete remaining two, undo, redo.
		void testInsertDeleteObjectSeq()
		{
			// Create an initial Tester object in the database as it's not owned by anyone...
			HVO hvoTest = CreateNewTesterObjectInDB();

			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");
			HVO hvoTest1, hvoTest2, hvoTest3, hvoTest4;

			// Save the initial (everything null) state of the object.
			int rgws[2];
			HRESULT hr;
			CheckHr(hr = m_qwsf->get_UserWs(&rgws[0]));
			StrUni stuFr(L"fr");
			CheckHr(hr = m_qwsf->GetWsFromStr(stuFr.Bstr(), &rgws[1]));
			TestObjectVerifier tovOrig(hvoTest, m_qsda, rgws, 2);

			// Make one object in the sequence, and initialize it.
			int val = 999;
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 0, &hvoTest1));
			TestObjectVerifier tov1(hvoTest1, m_qsda, rgws, 2, m_qtsf, val);
			CheckHr(m_qsda->EndUndoTask());

			UndoResult ures;
			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveObjectAndOwnedInfo));

			ConfirmObjectNonexistence(hvoTest1);

			int chvo;
			CheckHr(m_qsda->get_VecSize(hvoTest, kflidTester_OwningSequence, & chvo));
			unitpp::assert_eq("undo create in seq removes from seq", 0, chvo);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectExistence(hvoTest1);
			tov1.Verify(-1, -1);
			VerifyVec(hvoTest, kflidTester_OwningSequence, &hvoTest1, 1);

			// Make another at the end.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 1, &hvoTest2));
			CheckHr(m_qsda->EndUndoTask());

			CheckHr(m_qacth->Undo(&ures));
			CheckHr(m_qcda->ClearInfoAbout(hvoTest2, kciaRemoveObjectAndOwnedInfo));

			ConfirmObjectNonexistence(hvoTest2);
			ConfirmObjectExistence(hvoTest1); // But we only got rid of one!
			VerifyVec(hvoTest, kflidTester_OwningSequence, &hvoTest1, 1);

			CheckHr(m_qcda->ClearInfoAbout(hvoTest1, kciaRemoveObjectAndOwnedInfo));
			tov1.Verify(-1, -1); // Test 1 is still present and uncorrupted.

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectExistence(hvoTest2);
			HVO rghvoExpected12[] = {hvoTest1, hvoTest2};
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected12, 2);

			// Now try inserting at the beginning.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 0, &hvoTest3));
			CheckHr(m_qsda->EndUndoTask());

			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectNonexistence(hvoTest3);
			ConfirmObjectExistence(hvoTest1); // But we only got rid of one!
			ConfirmObjectExistence(hvoTest2);
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected12, 2);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectExistence(hvoTest3);
			HVO rghvoExpected312[] = {hvoTest3, hvoTest1, hvoTest2};
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected312, 3);

			// And finally insert in middle.
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningSequence, 1, &hvoTest4));
			CheckHr(m_qsda->EndUndoTask());

			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectNonexistence(hvoTest4);
			ConfirmObjectExistence(hvoTest1); // But we only got rid of one!
			ConfirmObjectExistence(hvoTest3);
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected312, 3);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectExistence(hvoTest4);
			HVO rghvoExpected3412[] = {hvoTest3, hvoTest4, hvoTest1, hvoTest2};
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected3412, 4);


			// initializing 3 and 4
			// delete from the middle (1)
			TestObjectVerifier tov2(hvoTest2, m_qsda, rgws, 2, m_qtsf, val);
			TestObjectVerifier tov3(hvoTest3, m_qsda, rgws, 2, m_qtsf, val);
			TestObjectVerifier tov4(hvoTest4, m_qsda, rgws, 2, m_qtsf, val);

			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->DeleteObjOwner(hvoTest, hvoTest1, kflidTester_OwningSequence, 2));
			CheckHr(m_qsda->EndUndoTask());
			ConfirmObjectNonexistence(hvoTest1);
			ConfirmObjectExistence(hvoTest2); // But we only got rid of one!
			ConfirmObjectExistence(hvoTest3);
			HVO rghvoExpected342[] = {hvoTest3, hvoTest4, hvoTest2};
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected342, 3);

			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectExistence(hvoTest1);
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected3412, 4);
			tov1.Verify(-1, -1);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectNonexistence(hvoTest1);
			ConfirmObjectExistence(hvoTest2); // But we only got rid of one!
			ConfirmObjectExistence(hvoTest4);
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected342, 3);

			// now delete from end hvoTest2
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->DeleteObjOwner(hvoTest, hvoTest2, kflidTester_OwningSequence, 2));
			CheckHr(m_qsda->EndUndoTask());
			ConfirmObjectNonexistence(hvoTest2);
			ConfirmObjectExistence(hvoTest3); // But we only got rid of one!
			ConfirmObjectExistence(hvoTest4);
			HVO rghvoExpected34[] = {hvoTest3, hvoTest4};
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected34, 2);

			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectExistence(hvoTest2);
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected342, 3);
			tov2.Verify(-1, -1);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectNonexistence(hvoTest2);
			ConfirmObjectExistence(hvoTest3); // But we only got rid of one!
			ConfirmObjectExistence(hvoTest4);
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected34, 2);


			// now delete from the begining hvoTest3
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->DeleteObjOwner(hvoTest, hvoTest3, kflidTester_OwningSequence, 0));
			CheckHr(m_qsda->EndUndoTask());
			ConfirmObjectNonexistence(hvoTest3);
			ConfirmObjectExistence(hvoTest4);
			HVO rghvoExpected4[] = {hvoTest4};
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected4, 1);

			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectExistence(hvoTest3);
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected34, 2);
			tov3.Verify(-1, -1);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectNonexistence(hvoTest3);
			ConfirmObjectExistence(hvoTest4);
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected4, 1);


			// now delete the last one - hvoTest4
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(m_qsda->DeleteObjOwner(hvoTest, hvoTest4, kflidTester_OwningSequence, 0));
			CheckHr(m_qsda->EndUndoTask());
			ConfirmObjectNonexistence(hvoTest4);
			VerifyVec(hvoTest, kflidTester_OwningSequence, NULL, 0);

			CheckHr(m_qacth->Undo(&ures));
			ConfirmObjectExistence(hvoTest4);
			VerifyVec(hvoTest, kflidTester_OwningSequence, rghvoExpected4, 1);
			tov4.Verify(-1, -1);

			CheckHr(m_qacth->Redo(&ures));
			ConfirmObjectNonexistence(hvoTest4);
			VerifyVec(hvoTest, kflidTester_OwningSequence, NULL, 0);

			// sequence is now empty


			tovOrig.Verify(kflidTester_OwningSequence, -1);
		}

		// This test set's data and undoes it and redoes it 'n' times (30)
		void testNUndosAndRedos()
		{
			// Create an initial Tester object in the database as it's not owned by anyone...
			HVO hvoTest = CreateNewTesterObjectInDB();

			// now start the test
			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");
			HVO hvoTest1;
			const int nUndos = 30;
			int n;

			// first undoable task
			// create an object
			CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			HRESULT hr;
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningAtom, -2, &hvoTest1));
//			m_qsda->get_IntProp(hvoTest1, kflidTester_Integer, &oldn);
			CheckHr(m_qsda->EndUndoTask());

			// next undoable task
			// set an integer property 'N' times
			UndoResult ures;
			for (int i=1; i<=nUndos; i++)
			{
				CheckHr(m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
				CheckHr(m_qsda->get_IntProp(hvoTest1, kflidTester_Integer, &n));
				CheckHr(m_qsda->SetInt(hvoTest1, kflidTester_Integer, i));
				CheckHr(m_qsda->EndUndoTask());

				CheckHr(m_qacth->Undo(&ures));
				CheckHr(m_qacth->Redo(&ures));

				CheckHr(m_qsda->get_IntProp(hvoTest1, kflidTester_Integer, &n));
				unitpp::assert_eq("int undo-redo", i, n);
			}

			// Now Undo 'N'/2 times
			for (int i=0; i<nUndos/2; i++)
			{
				CheckHr(m_qacth->Undo(&ures));
			}
			CheckHr(m_qsda->get_IntProp(hvoTest1, kflidTester_Integer, &n));
			unitpp::assert_eq("int reset", nUndos/2, n);
		}

		// This is a simple test to add the basic types through the 'Set' methods.
		void testSetXMethods()
		{
			// Create an initial Tester object in the database as it's not owned by anyone...
			HVO hvoTest = CreateNewTesterObjectInDB();

			// now start the test
			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");
			HVO hvoTest1;
			HRESULT hr;

			// get the writing system
			int wsEng;
			CheckHr(hr = m_qwsf->get_UserWs(&wsEng));

			// first undoable task
			// create an object
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->MakeNewObject(kclidTester, hvoTest, kflidTester_OwningAtom, -2, &hvoTest1));
			CheckHr(hr = m_qsda->EndUndoTask());

			unsigned char cData[] = {"asdf;lkjqwerpoiu"};
			CheckHr(hr = m_qsda->SetBinary(hvoTest1, kflidTester_Binary, cData, sizeof(cData)));
			unitpp::assert_eq("m_qsda->SetBinary()", S_OK, hr);

			const GUID kGuid = { 0x372e0716, 0x974f, 0x40ac, { 0xa0, 0x88, 0x08, 0xcd, 0xc9, 0x2e, 0xbf, 0xbc } };
			CheckHr(hr = m_qsda->SetGuid(hvoTest1, kflidTester_Guid, kGuid));
			unitpp::assert_eq("m_qsda->SetGuid()", S_OK, hr);

			int n = 777;
			CheckHr(hr = m_qsda->SetInt(hvoTest1, kflidTester_Integer, n));
			unitpp::assert_eq("m_qsda->SetInt()", S_OK, hr);

			int64 n64 = 7777777;
			CheckHr(hr = m_qsda->SetInt64(hvoTest1, kflidTester_Numeric, n64));
			unitpp::assert_eq("m_qsda->SetInt64()", S_OK, hr);

			StrUni stuThird(L"three");
			ITsStringPtr qtssThird;
			CheckHr(hr = m_qtsf->MakeString(stuThird.Bstr(), wsEng, &qtssThird));
			CheckHr(hr = m_qsda->SetMultiStringAlt(hvoTest, kflidTester_MultiString, wsEng, qtssThird));
			unitpp::assert_eq("m_qsda->SetMultiStringAlt()", S_OK, hr);

			StrUni stuOther(L"Some other String here...");
			ITsStringPtr qtssOther;
			CheckHr(hr = m_qtsf->MakeString(stuOther.Bstr(), wsEng, &qtssOther));
			CheckHr(hr = m_qsda->SetString(hvoTest, kflidTester_String, qtssOther));
			unitpp::assert_eq("m_qsda->SetString()", S_OK, hr);

			SilTime stimNow = SilTime::CurTime();
			CheckHr(hr = m_qsda->SetTime(hvoTest, kflidTester_Time, stimNow.AsInt64()));
			unitpp::assert_eq("m_qsda->SetTime()", S_OK, hr);

			CheckHr(hr = m_qsda->SetUnicode(hvoTest, kflidTester_Unicode, (OLECHAR *) stuOther.Chars(), stuOther.Length()));
			unitpp::assert_eq("m_qsda->SetUnicode()", S_OK, hr);
		}

		// Test replacing in reference sequences.
		void testReplaceSeq()
		{
			// Create an initial Tester object in the database as it's not owned by anyone...
			HVO hvoTest = CreateNewTesterObjectInDB();
			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");

			// Save the initial (everything null) state of the object.
			int rgws[2];
			HRESULT hr;
			CheckHr(hr = m_qwsf->get_UserWs(&rgws[0]));
			StrUni stuFr(L"fr");
			CheckHr(hr = m_qwsf->GetWsFromStr(stuFr.Bstr(), &rgws[1]));
			int val = 879;

			// Save the initial state, and also get us some other objects.
			TestObjectVerifier tovOrig(hvoTest, m_qsda, rgws, 2, m_qtsf, val);
			// Grab four objects created by the verifier to work with.
			HVO hvoTest1, hvoTest2, hvoTest3, hvoTest4;
			CheckHr(m_qsda->get_ObjectProp(hvoTest, kflidTester_OwningAtom, &hvoTest1));
			CheckHr(m_qsda->get_VecItem(hvoTest, kflidTester_OwningCollection, 0, &hvoTest2));
			CheckHr(m_qsda->get_VecItem(hvoTest, kflidTester_OwningSequence, 0, &hvoTest3));
			CheckHr(m_qsda->get_VecItem(hvoTest, kflidTester_OwningSequence, 1, &hvoTest4));
			// The verifier sets up 1343 in the collection and 343 in the sequence.

			// Try deleting one thing.
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->Replace(hvoTest, kflidTester_ReferenceSequence, 1, 2, NULL, 0));
			CheckHr(hr = m_qsda->EndUndoTask());

			HVO rghvoExpected33[] = {hvoTest3, hvoTest3};
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected33, 2);

			UndoResult ures;
			CheckHr(m_qacth->Undo(&ures));
			HVO rghvoExpected343[] = {hvoTest3, hvoTest4, hvoTest3};
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected343, 3);
			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected33, 2);

			//--------------------------------------------------------------------------
			// Insert one at end
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->Replace(hvoTest, kflidTester_ReferenceSequence, 2, 2, &hvoTest1, 1));
			CheckHr(hr = m_qsda->EndUndoTask());

			HVO rghvoExpected331[] = {hvoTest3, hvoTest3, hvoTest1};
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected331, 3);

			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected33, 2);
			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected331, 3);


			//--------------------------------------------------------------------------
			// Replace 2 at start with 3
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			HVO rghvo214[] = {hvoTest2, hvoTest1, hvoTest4};
			CheckHr(hr = m_qsda->Replace(hvoTest, kflidTester_ReferenceSequence, 0, 2, rghvo214, 3));
			CheckHr(hr = m_qsda->EndUndoTask());

			HVO rghvoExpected2141[] = {hvoTest2, hvoTest1, hvoTest4, hvoTest1};
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected2141, 4);

			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected331, 3);
			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected2141, 4);

			//--------------------------------------------------------------------------
			// Replace 2 at end with 1
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->Replace(hvoTest, kflidTester_ReferenceSequence, 2, 4, &hvoTest3, 1));
			CheckHr(hr = m_qsda->EndUndoTask());

			HVO rghvoExpected213[] = {hvoTest2, hvoTest1, hvoTest3};
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected213, 3);

			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected2141, 4);
			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected213, 3);

			CheckHr(m_qacth->Undo(&ures)); // back to 2141

			//--------------------------------------------------------------------------
			// Replace 2 in middle with 2.
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			HVO rghvo33[] = {hvoTest3, hvoTest3};
			CheckHr(hr = m_qsda->Replace(hvoTest, kflidTester_ReferenceSequence, 1, 3, rghvo33, 2));
			CheckHr(hr = m_qsda->EndUndoTask());

			HVO rghvoExpected2331[] = {hvoTest2, hvoTest3, hvoTest3, hvoTest1};
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected2331, 4);

			CheckHr(m_qacth->Undo(&ures));
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected2141, 4);
			CheckHr(m_qacth->Redo(&ures));
			VerifyVec(hvoTest, kflidTester_ReferenceSequence, rghvoExpected2331, 4);

			tovOrig.Verify(kflidTester_ReferenceSequence);
		}
		// Test replacing in reference sequences and collections.
		void testReplaceColl()
		{
			// Create an initial Tester object in the database as it's not owned by anyone...
			HVO hvoTest = CreateNewTesterObjectInDB();
			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");

			// Save the initial (everything null) state of the object.
			int rgws[2];
			HRESULT hr;
			CheckHr(hr = m_qwsf->get_UserWs(&rgws[0]));
			StrUni stuFr(L"fr");
			CheckHr(hr = m_qwsf->GetWsFromStr(stuFr.Bstr(), &rgws[1]));
			int val = 879;

			// Save the initial state, and also get us some other objects.
			TestObjectVerifier tovOrig(hvoTest, m_qsda, rgws, 2, m_qtsf, val);
			// Grab four objects created by the verifier to work with.
			HVO hvoTest1, hvoTest2, hvoTest3, hvoTest4;
			CheckHr(m_qsda->get_ObjectProp(hvoTest, kflidTester_OwningAtom, &hvoTest1));
			CheckHr(m_qsda->get_VecItem(hvoTest, kflidTester_OwningCollection, 0, &hvoTest2));
			CheckHr(m_qsda->get_VecItem(hvoTest, kflidTester_OwningSequence, 0, &hvoTest3));
			CheckHr(m_qsda->get_VecItem(hvoTest, kflidTester_OwningSequence, 1, &hvoTest4));
			// The verifier sets up (some order of) 1343 in the collection.

			//--------------------------------------------------------------------------
			// Insert another
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->Replace(hvoTest, kflidTester_ReferenceCollection, 0, 0, &hvoTest2, 1));
			CheckHr(hr = m_qsda->EndUndoTask());

			HVO rghvoExpected12334[] = {hvoTest1, hvoTest2, hvoTest3, hvoTest3, hvoTest4};
			VerifyCollection(hvoTest, kflidTester_ReferenceCollection, rghvoExpected12334, 5);

			UndoResult ures;
			CheckHr(m_qacth->Undo(&ures));
			HVO rghvoExpected1334[] = {hvoTest1, hvoTest3, hvoTest3, hvoTest4};
			VerifyVec(hvoTest, kflidTester_ReferenceCollection, rghvoExpected1334, 4);
			CheckHr(m_qacth->Redo(&ures));
			VerifyCollection(hvoTest, kflidTester_ReferenceCollection, rghvoExpected12334, 5);

			//--------------------------------------------------------------------------
			// Delete both 3s and the 2.
			// Since we don't know what order they may be in the cache, change it to one we know.
			CheckHr(m_qcda->CacheVecProp(hvoTest, kflidTester_ReferenceCollection, rghvoExpected12334, 5));
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->Replace(hvoTest, kflidTester_ReferenceCollection, 1, 4, NULL, 0));
			CheckHr(hr = m_qsda->EndUndoTask());

			HVO rghvoExpected14[] = {hvoTest1, hvoTest4};
			VerifyCollection(hvoTest, kflidTester_ReferenceCollection, rghvoExpected14, 2);

			CheckHr(m_qacth->Undo(&ures));
			VerifyCollection(hvoTest, kflidTester_ReferenceCollection, rghvoExpected12334, 5);
			CheckHr(m_qacth->Redo(&ures));
			VerifyCollection(hvoTest, kflidTester_ReferenceCollection, rghvoExpected14, 2);

			//--------------------------------------------------------------------------
			CheckHr(m_qacth->Undo(&ures)); // back to 12334
			// Delete just one 3 and insert another 1 and 2
			// Since we don't know what order they may be in the cache, change it to one we know.
			CheckHr(m_qcda->CacheVecProp(hvoTest, kflidTester_ReferenceCollection, rghvoExpected12334, 5));
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			HVO rghvo12[2] = {hvoTest1, hvoTest2};
			CheckHr(hr = m_qsda->Replace(hvoTest, kflidTester_ReferenceCollection, 2, 3, rghvo12, 2));
			CheckHr(hr = m_qsda->EndUndoTask());

			HVO rghvoExpected112234[] = {hvoTest1, hvoTest1, hvoTest2, hvoTest2, hvoTest3, hvoTest4};
			VerifyCollection(hvoTest, kflidTester_ReferenceCollection, rghvoExpected112234, 6);

			CheckHr(m_qacth->Undo(&ures));
			VerifyCollection(hvoTest, kflidTester_ReferenceCollection, rghvoExpected12334, 5);
			CheckHr(m_qacth->Redo(&ures));
			VerifyCollection(hvoTest, kflidTester_ReferenceCollection, rghvoExpected112234, 6);
		}

		void VerifyStringAlt(ITsString * ptss, HVO hvoTest, int flid, int ws)
		{
			ITsStringPtr qtss;
			CheckHr(m_qsda->get_MultiStringAlt(hvoTest, flid, ws, &qtss));
			ComBool fEqual;
			CheckHr(qtss->Equals(ptss, &fEqual));
			unitpp::assert_true("string alts match", fEqual);
		}

		// Building our standard test object, undoing that, and redoing it, confirms
		// that we can set two alternatives of a multi-string and clear and restore them.
		// Try altering multiple alternatives independently.
		void testSetMultiStringAlt()
		{
			HVO hvoTest = CreateNewTesterObjectInDB();
			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");
			// Set several alternatives.
			ITsStringPtr qtssUser;
			StrUni stuData(L"Hello world");
			int wsUser, wsFr, wsGe;
			HRESULT hr;
			CheckHr(hr = m_qwsf->get_UserWs(&wsUser));
			StrUni stuFr(L"fr");
			CheckHr(hr = m_qwsf->GetWsFromStr(stuFr.Bstr(), &wsFr));
			StrUni stuGe(L"de");
			CheckHr(hr = m_qwsf->GetWsFromStr(stuGe.Bstr(), &wsGe));
			CheckHr(m_qtsf->MakeString(stuData.Bstr(), wsUser, &qtssUser));
			CheckHr(hr = m_qsda->SetMultiStringAlt(hvoTest, kflidTester_MultiString, wsUser, qtssUser));

			stuData.Assign(L"Francais");
			ITsStringPtr qtssFr;
			CheckHr(m_qtsf->MakeString(stuData.Bstr(), wsFr, &qtssFr));
			CheckHr(hr = m_qsda->SetMultiStringAlt(hvoTest, kflidTester_MultiString, wsFr, qtssFr));

			stuData.Assign(L"Deutsch");
			ITsStringPtr qtssGe;
			CheckHr(m_qtsf->MakeString(stuData.Bstr(), wsGe, &qtssGe));
			CheckHr(hr = m_qsda->SetMultiStringAlt(hvoTest, kflidTester_MultiString, wsGe, qtssGe));

			//--------------------------------------------------------------------------
			// Overwrite an existing alternative.
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			stuData.Assign(L"Deutsch2");
			ITsStringPtr qtssGe2;
			CheckHr(m_qtsf->MakeString(stuData.Bstr(), wsGe, &qtssGe2));
			CheckHr(hr = m_qsda->SetMultiStringAlt(hvoTest, kflidTester_MultiString, wsGe, qtssGe2));
			CheckHr(hr = m_qsda->EndUndoTask());

			VerifyStringAlt(qtssGe2, hvoTest, kflidTester_MultiString, wsGe);

			UndoResult ures;
			CheckHr(m_qacth->Undo(&ures));
			VerifyStringAlt(qtssGe, hvoTest, kflidTester_MultiString, wsGe);
			VerifyStringAlt(qtssFr, hvoTest, kflidTester_MultiString, wsFr);
			VerifyStringAlt(qtssUser, hvoTest, kflidTester_MultiString, wsUser);

			CheckHr(m_qacth->Redo(&ures));
			VerifyStringAlt(qtssGe2, hvoTest, kflidTester_MultiString, wsGe);
			VerifyStringAlt(qtssFr, hvoTest, kflidTester_MultiString, wsFr);
			VerifyStringAlt(qtssUser, hvoTest, kflidTester_MultiString, wsUser);

			//--------------------------------------------------------------------------
			// Add a new alternative.
			StrUni stuSp(L"es");
			int wsSp;
			CheckHr(hr = m_qwsf->GetWsFromStr(stuSp.Bstr(), &wsSp));
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			stuData.Assign(L"Espanol");
			ITsStringPtr qtssSp;
			CheckHr(m_qtsf->MakeString(stuData.Bstr(), wsSp, &qtssSp));
			CheckHr(hr = m_qsda->SetMultiStringAlt(hvoTest, kflidTester_MultiString, wsSp, qtssSp));
			CheckHr(hr = m_qsda->EndUndoTask());

			VerifyStringAlt(qtssSp, hvoTest, kflidTester_MultiString, wsSp);

			CheckHr(m_qacth->Undo(&ures));
			ITsStringPtr qtssEmptySp;
			stuData.Assign(L"");
			CheckHr(m_qtsf->MakeString(stuData.Bstr(), wsSp, &qtssEmptySp));

			VerifyStringAlt(qtssEmptySp, hvoTest, kflidTester_MultiString, wsSp);
			VerifyStringAlt(qtssGe2, hvoTest, kflidTester_MultiString, wsGe);
			VerifyStringAlt(qtssFr, hvoTest, kflidTester_MultiString, wsFr);
			VerifyStringAlt(qtssUser, hvoTest, kflidTester_MultiString, wsUser);

			CheckHr(m_qacth->Redo(&ures));
			VerifyStringAlt(qtssSp, hvoTest, kflidTester_MultiString, wsSp);
			VerifyStringAlt(qtssGe2, hvoTest, kflidTester_MultiString, wsGe);
			VerifyStringAlt(qtssFr, hvoTest, kflidTester_MultiString, wsFr);
			VerifyStringAlt(qtssUser, hvoTest, kflidTester_MultiString, wsUser);
		}

		// Test using SetUnknown to store and restore a TsTextProps.
		void testSetUnknown()
		{
			HVO hvoTest = CreateNewTesterObjectInDB();
			StrUni stuUndo(L"Undo");
			StrUni stuRedo(L"Redo");

			ITsTextPropsPtr qttp1;
			ITsTextPropsPtr qttp2;
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, 34));
			CheckHr(qtpb->GetTextProps(&qttp1));
			CheckHr(qtpb->SetIntPropValues(ktptForeColor, ktpvDefault, 999));
			CheckHr(qtpb->GetTextProps(&qttp2));

			CheckHr(m_qsda->SetUnknown(hvoTest, kflidTester_Binary, qttp1));

			//--------------------------------------------------------------------------
			// Overwrite an existing alternative.
			HRESULT hr;
			CheckHr(hr = m_qsda->BeginUndoTask(stuUndo.Bstr(), stuRedo.Bstr()));
			CheckHr(hr = m_qsda->SetUnknown(hvoTest, kflidTester_Binary, qttp2));
			CheckHr(hr = m_qsda->EndUndoTask());

			UndoResult ures;
			CheckHr(m_qacth->Undo(&ures));

			ITsTextPropsPtr qttp;
			IUnknownPtr qunkTtp;
			m_qsda->get_UnknownProp(hvoTest, kflidTester_Binary, &qunkTtp);
			if (qunkTtp)
				CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));
			unitpp::assert_eq("Undo set text props", qttp1.Ptr(), qttp.Ptr());
			CheckHr(m_qacth->Redo(&ures));
			qttp = NULL;
			m_qsda->get_UnknownProp(hvoTest, kflidTester_Binary, &qunkTtp);
			if (qunkTtp)
				CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));
			unitpp::assert_eq("Undo set text props", qttp2.Ptr(), qttp.Ptr());
		}

		// HVO CreateNewTesterObjectInDB()
		// Helper routine to create a 'Tester' object in the DB that isn't owned by anyone
		HVO CreateNewTesterObjectInDB()
		{
			IOleDbCommandPtr qodc;
			ComBool fIsNull;
			StrUni stuSql;
			HVO hvoTest;
			HRESULT hr;
			CheckHr(hr = m_qode->CreateCommand(&qodc));
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *) &hvoTest,
				sizeof(HVO)));
			stuSql.Format(L"exec CreateObject$ %d, ? output, null", kclidTester);
			CheckHr(hr = qodc->ExecCommand(stuSql.Bstr(), knSqlStmtStoredProcedure));
			unitpp::assert_eq("CreateNewTesterObjectInDB: hr = qodc->ExecCommand()", DB_S_NORESULT, hr);

			CheckHr(qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvoTest), sizeof(HVO), &fIsNull));

			unitpp::assert_eq("fIsNull is not NULL", false, bool(fIsNull));
			unitpp::assert_true("hvoTest is not 0", hvoTest);

			return hvoTest;
		}

	public:
		TestVwOleDbDaUndoRedo();

		virtual void SuiteSetup()
		{
			HRESULT hr;
			m_qsda.CreateInstance(CLSID_VwOleDbDa);
			CheckHr(m_qsda->QueryInterface(IID_IVwCacheDa, (void **) & m_qcda));

			m_qode.CreateInstance(CLSID_OleDbEncap);
			StrUni stuDBMName(L".\\SILFW");
			StrUni stuDbName(L"TestLangProj");
			CheckHr(m_qode->Init(stuDBMName.Bstr(), stuDbName.Bstr(), NULL, koltReturnError, 1000));
			// NOTE: Backup before doing anything in the database.
			BackupDB();
			// NOTE: MakeNewObject() uses the metadata cache
			// so, make sure we initialize it with a qode that has the Tester Object class definition.
			CreateTesterObject();
			m_qmdc.CreateInstance(CLSID_FwMetaDataCache);
			CheckHr(m_qmdc->Init(m_qode));
			ILgWritingSystemFactoryBuilderPtr qwsfb;
			qwsfb.CreateInstance(CLSID_LgWritingSystemFactoryBuilder);
			CheckHr(hr = qwsfb->GetWritingSystemFactory(m_qode, NULL, &m_qwsf));
			unitpp::assert_eq("SuiteSetupBackupDB: hr = qwsfb->GetWritingSystemFactory()", S_OK, hr);
			CheckHr(m_qsda->putref_WritingSystemFactory(m_qwsf));

			ISetupVwOleDbDaPtr qsetup;
			CheckHr(m_qsda->QueryInterface(IID_ISetupVwOleDbDa, (void **)(&qsetup)));
			m_qacth.CreateInstance(CLSID_ActionHandler);
			CheckHr(qsetup->Init(m_qode, m_qmdc, m_qwsf, m_qacth));

			m_qtsf.CreateInstance(CLSID_TsStrFactory);
			CheckHr(m_qsda->GetActionHandler(&m_qacth));

		}

		virtual void SuiteTeardown()
		{
			IVwOleDbDaPtr qodd;
			m_qsda->QueryInterface(IID_IVwOleDbDa, (void **)&qodd);
			CheckHr(qodd->Close());
			m_qcda->ClearAllData(); // qodd->Clear();
			m_qacth.Clear();
			// REVISIT: qtsf destructor didn't already clear this object
			// for some reason. Do so now, to avoid an Access Violation
			// Exception when the parent class destructor tries to
			// Release() the qtsf smart pointer.
			m_qtsf.Clear();
			m_qmdc.Clear();
			m_qsda.Clear();
			m_qcda.Clear();
			CheckHr(m_qwsf->Shutdown());
			m_qwsf.Clear();
			m_qode.Clear();

			// now restore the DB
			m_qode.CreateInstance(CLSID_OleDbEncap);
			StrUni stuDBMName(L".\\SILFW");
			StrUni stuDbName(L"Master");
			CheckHr(m_qode->Init(stuDBMName.Bstr(), stuDbName.Bstr(), NULL, koltReturnError, 1000));
			RestoreDB();
			m_qode.Clear();
		}

	};
}

#endif /*TestVwOleDbDaUndoRedo_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
