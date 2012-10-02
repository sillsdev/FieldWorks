/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestVirtualHandlers.h
Responsibility: John Thomson
Last reviewed:

	Unit tests relating to the use of IVwVirtualHandler to implement virtual properties.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestVirtualHandlers_H_INCLUDED
#define TestVirtualHandlers_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{
	class DummyIntVirtualHandler : public VwBaseVirtualHandler
	{
	public:
		int m_valWritten;
		STDMETHOD(Load)(HVO hvo, PropTag tag, int ws, IVwCacheDa * pcda)
		{
			CheckHr(pcda->CacheIntProp(hvo, tag, 957));
			return S_OK;
		}
		STDMETHOD(WriteInt64)(HVO hvo, PropTag tag, int64 val, ISilDataAccess * psda)
		{
			m_valWritten = (int) val;
			return S_OK;
		}

	};
	DEFINE_COM_PTR(DummyIntVirtualHandler);

	class DummyInt64VirtualHandler : public VwBaseVirtualHandler
	{
	public:
		int64 m_valWritten;

		STDMETHOD(Load)(HVO hvo, PropTag tag, int ws, IVwCacheDa * pcda)
		{
			CheckHr(pcda->CacheInt64Prop(hvo, tag, 1234567890));
			return S_OK;
		}
		STDMETHOD(WriteInt64)(HVO hvo, PropTag tag, int64 val, ISilDataAccess * psda)
		{
			m_valWritten = val;
			return S_OK;
		}
	};
	DEFINE_COM_PTR(DummyInt64VirtualHandler);

	class DummyObjVirtualHandler : public VwBaseVirtualHandler
	{
	public:
		int m_chvo;
		HVO m_hvo;
		int m_ihvoMin, m_ihvoLim;
		STDMETHOD(Load)(HVO hvo, PropTag tag, int ws, IVwCacheDa * pcda)
		{
			CheckHr(pcda->CacheObjProp(hvo, tag, 959));
			return S_OK;
		}
		STDMETHOD(Replace)(HVO hvo, PropTag tag, int ihvoMin, int ihvoLim,
			HVO * prghvo, int chvo, ISilDataAccess * psda)
		{
			m_chvo = chvo;
			if (chvo > 0)
				m_hvo = prghvo[0];
			m_ihvoMin = ihvoMin;
			m_ihvoLim = ihvoLim;

			return S_OK;
		}
	};
	DEFINE_COM_PTR(DummyObjVirtualHandler);

	class DummySeqVirtualHandler : public VwBaseVirtualHandler
	{
	public:
		HVO m_rghvo[100];
		int m_chvo;
		int m_ihvoMin, m_ihvoLim;
		STDMETHOD(Load)(HVO hvo, PropTag tag, int ws, IVwCacheDa * pcda)
		{
			HVO rghvo[] = {345, 356, 376};
			CheckHr(pcda->CacheVecProp(hvo, tag, rghvo, 3));
			return S_OK;
		}
		STDMETHOD(Replace)(HVO hvo, PropTag tag, int ihvoMin, int ihvoLim,
			HVO * prghvo, int chvo, ISilDataAccess * psda)
		{
			m_chvo = chvo;
			for (int i = 0; i < min(chvo, 100); i++)
				m_rghvo[i] = prghvo[i];
			m_ihvoMin = ihvoMin;
			m_ihvoLim = ihvoLim;
		return S_OK;
		}

	};
	DEFINE_COM_PTR(DummySeqVirtualHandler);

	class DummyStrVirtualHandler : public VwBaseVirtualHandler
	{
	public:
		ITsStringPtr m_valWritten;
		STDMETHOD(Load)(HVO hvo, PropTag tag, int ws, IVwCacheDa * pcda)
		{
			ITsStringPtr qtss;
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			ILgWritingSystemFactoryPtr qwsf;
			CheckHr(qtsf->MakeStringRgch(L"abc", 3, g_wsEng, &qtss));
			CheckHr(pcda->CacheStringProp(hvo, tag, qtss));
			return S_OK;
		}
		STDMETHOD(WriteObj)(HVO hvo, PropTag tag, int ws, IUnknown * punk, ISilDataAccess * psda)
		{
			CheckHr(punk->QueryInterface(IID_ITsString, (void **) &m_valWritten));
			return S_OK;
		}
	};
	DEFINE_COM_PTR(DummyStrVirtualHandler);

	class DummyMultiStrVirtualHandler : public VwBaseVirtualHandler
	{
	public:
		ITsStringPtr m_valWritten;
		STDMETHOD(Load)(HVO hvo, PropTag tag, int ws, IVwCacheDa * pcda)
		{
			ITsStringPtr qtss;
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			ILgWritingSystemFactoryPtr qwsf;
			CheckHr(qtsf->MakeStringRgch(L"def", 3, ws, &qtss));
			CheckHr(pcda->CacheStringAlt(hvo, tag, ws, qtss));
			return S_OK;
		}
		STDMETHOD(WriteObj)(HVO hvo, PropTag tag, int ws, IUnknown * punk, ISilDataAccess * psda)
		{
			CheckHr(punk->QueryInterface(IID_ITsString, (void **) &m_valWritten));
			return S_OK;
		}

	};
	DEFINE_COM_PTR(DummyMultiStrVirtualHandler);

	class DummyUniVirtualHandler : public VwBaseVirtualHandler
	{
	public:
		StrUni m_valWritten;

		STDMETHOD(Load)(HVO hvo, PropTag tag, int ws, IVwCacheDa * pcda)
		{
			CheckHr(pcda->CacheUnicodeProp(hvo, tag, L"unicode", 7));
			return S_OK;
		}
		STDMETHOD(WriteUnicode)(HVO hvo, PropTag tag, BSTR bstr, ISilDataAccess * psda)
		{
			m_valWritten = bstr;
			return S_OK;
		}
	};
	DEFINE_COM_PTR(DummyUniVirtualHandler);

	class TestVirtualHandlers : public unitpp::suite
	{
		IVwCacheDaPtr m_qcda;
		ISilDataAccessPtr m_qsda;
		IFwMetaDataCachePtr m_qmdcMem; // used for the memory-only cache.

		HRESULT SetNamesAndInstall(IVwVirtualHandler * pvhi, OLECHAR * pszClass, OLECHAR * pszField, int cpt)
		{
				StrUni stuClass(pszClass);
				CheckHr(pvhi->put_ClassName(stuClass.Bstr()));
				StrUni stuField(pszField);
				CheckHr(pvhi->put_FieldName(stuField.Bstr()));
				CheckHr(pvhi->put_Type(cpt));
				return m_qcda->InstallVirtual(pvhi);
		}

		void testInstallHandler()
		{
			DummyIntVirtualHandlerPtr qvhi;
			qvhi.Attach(new DummyIntVirtualHandler());
			CheckHr(SetNamesAndInstall(qvhi, L"SomeClass", L"IntField", kcptInteger));
			IVwVirtualHandlerPtr qvhiRet;

			// Check we can retrieve it using class name and id;
			StrUni stuClass(L"SomeClass");
			StrUni stuField(L"IntField");
			CheckHr(m_qcda->GetVirtualHandlerName(stuClass.Bstr(), stuField.Bstr(), &qvhiRet));
			unitpp::assert_eq("Retrieved int handler from names OK", qvhi.Ptr(), qvhiRet.Ptr());
			PropTag tag;
			CheckHr(qvhi->get_Tag(&tag));
			unitpp::assert_true("Id assigned", tag != 0);
			CheckHr(m_qcda->GetVirtualHandlerId(tag, &qvhiRet));
			unitpp::assert_eq("Retrieved int handler from ID OK", qvhi.Ptr(), qvhiRet.Ptr());
			// Check the trivial behavior happens;
			int val;
			CheckHr(m_qsda->get_IntProp(257, tag, &val)); // This dummy prop works for any obj.
			unitpp::assert_eq("Got expected int value", 957, val);
			// Test inability to overwrite with alternative handler.
			DummyIntVirtualHandlerPtr qvhi2;
			qvhi2.Attach(new DummyIntVirtualHandler());
			HRESULT hr;
			try{
				CheckHr(hr = SetNamesAndInstall(qvhi2, L"SomeClass", L"IntField", kcptInteger));
				unitpp::assert_eq("Got error overwriting handler", E_INVALIDARG, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("Got error overwriting handler", E_INVALIDARG, thr.Result());
			}
			// Check we still get S_FALSE for unknown prop
			CheckHr(hr = m_qsda->get_IntProp(257, 258, &val));
			unitpp::assert_eq("Got S_FALSE for unknown int prop", S_FALSE, hr);

			// Make it ComputeEveryTime and check that it doesn't get cached.
			CheckHr(qvhi->put_ComputeEveryTime(true));
			CheckHr(m_qsda->get_IntProp(1024, tag, &val));
			unitpp::assert_eq("Got expected CET value", 957, val);
			ComBool fIsInCache;
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptInteger, 0, &fIsInCache));
			unitpp::assert_true("CET prop not in cache", !fIsInCache);

			// Make it writeable and write it.
			CheckHr(qvhi->put_Writeable(true));
			CheckHr(m_qsda->SetInt(1024, tag, 456));
			unitpp::assert_eq("Value was writen", 456, qvhi->m_valWritten);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptInteger, 0, &fIsInCache));
			unitpp::assert_true("CET written val not in cache", !fIsInCache);

			// turn off write every time and write it.
			CheckHr(qvhi->put_ComputeEveryTime(false));
			CheckHr(m_qsda->SetInt(1024, tag, 458));
			unitpp::assert_eq("Value was writen", 458, qvhi->m_valWritten);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptInteger, 0, &fIsInCache));
			unitpp::assert_true("CET written val not in cache", fIsInCache);

			CheckDeletion(1024, tag, kcptInteger);
		}

		void testInt64Virtual()
		{
			DummyInt64VirtualHandlerPtr qvhi;
			qvhi.Attach(new DummyInt64VirtualHandler());
			CheckHr(SetNamesAndInstall(qvhi, L"SomeClass", L"Int64Field", kcptTime));

			PropTag tag;
			CheckHr(qvhi->get_Tag(&tag));

			// Check the trivial behavior happens;
			int64 val;
			CheckHr(m_qsda->get_Int64Prop(257, tag, &val)); // This dummy prop works for any obj.
			unitpp::assert_eq("Got expected int value", 1234567890, val);

			// Check we still get S_FALSE for unknown prop
			HRESULT hr;
			CheckHr(hr = m_qsda->get_Int64Prop(257, 258, &val));
			unitpp::assert_eq("Got S_FALSE for unknown int64 prop", S_FALSE, hr);

			// Make it ComputeEveryTime and check that it doesn't get cached.
			CheckHr(qvhi->put_ComputeEveryTime(true));
			CheckHr(m_qsda->get_Int64Prop(1024, tag, &val));
			unitpp::assert_eq("Got expected CET value", 1234567890, val);
			ComBool fIsInCache;
			// Time is currently stored in the same table as Int64, and int64 does not have a separate kcpt.
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptTime, 0, &fIsInCache));
			unitpp::assert_true("CET prop not in cache", !fIsInCache);

			CheckHr(qvhi->put_Writeable(true));
			CheckHr(m_qsda->SetInt64(1024, tag, 987));
			unitpp::assert_eq("Value was writen", 987, qvhi->m_valWritten);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptTime, 0, &fIsInCache));
			unitpp::assert_true("CET written val not in cache", !fIsInCache);

			// turn off write every time and write it.
			CheckHr(qvhi->put_ComputeEveryTime(false));
			CheckHr(m_qsda->SetInt64(1024, tag, 9876543210));
			unitpp::assert_eq("Value was writen", 9876543210, qvhi->m_valWritten);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptTime, 0, &fIsInCache));
			unitpp::assert_true("CET written val in cache", fIsInCache);
			CheckDeletion(1024, tag, kcptTime);
		}

		void testSeqVirtual()
		{
			DummySeqVirtualHandlerPtr qvhi;
			qvhi.Attach(new DummySeqVirtualHandler());
			CheckHr(SetNamesAndInstall(qvhi, L"SomeClass", L"SeqField", kcptReferenceSequence));
			PropTag tag;
			CheckHr(qvhi->get_Tag(&tag));

			// Check the trivial behavior happens;
			HVO val;
			int chvo;
			HVO rghvo[3];
			CheckHr(m_qsda->get_VecSize(257, tag, &chvo)); // This dummy prop works for any obj.
			unitpp::assert_eq("Got expected num in seq", 3, chvo);
			// For a different object has to be computed again:
			CheckHr(m_qsda->get_VecItem(258, tag, 1, &val));
			unitpp::assert_eq("Got expected value item 1", 356, val);
			// And again for all-at-once
			CheckHr(m_qsda->VecProp(259, tag, 3, &chvo, rghvo));
			unitpp::assert_eq("Got expected # vals from VecProp", 3, chvo);
			unitpp::assert_eq("Got expected value item 2", 376, rghvo[2]);

			// Check that with unknown tag these methods return S_FALSE and simulate empty.
			HRESULT hr;
			CheckHr(hr = m_qsda->get_VecSize(257, 333, &chvo));
			unitpp::assert_eq("Got expected num in unknown seq", 0, chvo);
			unitpp::assert_eq("Got expected S_FALSE", S_FALSE, hr);
			CheckHr(hr = m_qsda->VecProp(259, 333, 3, &chvo, rghvo));
			unitpp::assert_eq("Got expected num in unknown seq", 0, chvo);
			unitpp::assert_eq("Got expected S_FALSE", S_FALSE, hr);

			// Check it still happens when computing every time, but result not saved.
			CheckHr(qvhi->put_ComputeEveryTime(true));

			CheckHr(m_qsda->get_VecSize(1257, tag, &chvo)); // This dummy prop works for any obj.
			unitpp::assert_eq("Got expected num in seq", 3, chvo);
			// For a different object has to be computed again:
			CheckHr(m_qsda->get_VecItem(1258, tag, 1, &val));
			unitpp::assert_eq("Got expected value item 1", 356, val);
			// And again for all-at-once
			CheckHr(m_qsda->VecProp(1259, tag, 3, &chvo, rghvo));
			unitpp::assert_eq("Got expected # vals from VecProp", 3, chvo);
			unitpp::assert_eq("Got expected value item 2", 376, rghvo[2]);
			ComBool fIsInCache;
			CheckHr(m_qsda->get_IsPropInCache(1257, tag, kcptReferenceSequence, 0, &fIsInCache));
			unitpp::assert_true("CET prop not in cache", !fIsInCache);
			CheckHr(m_qsda->get_IsPropInCache(1258, tag, kcptReferenceSequence, 0, &fIsInCache));
			unitpp::assert_true("CET prop not in cache", !fIsInCache);
			CheckHr(m_qsda->get_IsPropInCache(1259, tag, kcptReferenceSequence, 0, &fIsInCache));
			unitpp::assert_true("CET prop not in cache", !fIsInCache);

			// Make it writeable and write it.
			CheckHr(qvhi->put_Writeable(true));

			HVO rghvoW[3] = {987, 654, 321};
			CheckHr(m_qsda->Replace(1024, tag, 0, 0, rghvoW, 3));
			unitpp::assert_eq("Right # vals written", 3, qvhi->m_chvo);
			unitpp::assert_eq("Written at right place", 0, qvhi->m_ihvoMin);
			unitpp::assert_eq("Written at right place", 0, qvhi->m_ihvoLim);
			unitpp::assert_eq("Right vals written", 321, qvhi->m_rghvo[2]);
			unitpp::assert_eq("Right vals written", 654, qvhi->m_rghvo[1]);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptReferenceSequence, 0, &fIsInCache));
			unitpp::assert_true("CET written val not in cache", !fIsInCache);

			// turn off write every time and write it. Note that the previous value was not
			// really written, due to our primitive dummy virtual handler and ComputeEveryTime
			// to have been true.
			CheckHr(qvhi->put_ComputeEveryTime(false));
			CheckHr(m_qsda->Replace(1024, tag, 0, 0, rghvoW, 3));
			unitpp::assert_eq("Right # vals written", 3, qvhi->m_chvo);
			unitpp::assert_eq("Right vals written", 321, qvhi->m_rghvo[2]);
			unitpp::assert_eq("Right vals written", 654, qvhi->m_rghvo[1]);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptReferenceSequence, 0, &fIsInCache));
			unitpp::assert_true("CET written val in cache", fIsInCache);
			HVO item;
			CheckHr(m_qsda->get_VecItem(1024, tag, 0, &item));
			unitpp::assert_eq("Val really in cache", 987, item);

			// Now we have some data in the cache, we can attempt a replace not at
			// position 0.
			HVO rghvo2[3] = {333, 444, 555};
			CheckHr(m_qsda->Replace(1024, tag, 1, 2, rghvo2, 3));
			// We replaced the 654 with 333, 444, 555 giving 987, 333, 444, 555, 321.
			unitpp::assert_eq("Right # vals written", 3, qvhi->m_chvo);
			unitpp::assert_eq("Written at right place(min)", 1, qvhi->m_ihvoMin);
			unitpp::assert_eq("Written at right place(lim)", 2, qvhi->m_ihvoLim);
			unitpp::assert_eq("Right vals written", 555, qvhi->m_rghvo[2]);
			unitpp::assert_eq("Right vals written", 444, qvhi->m_rghvo[1]);
			CheckHr(m_qsda->get_VecItem(1024, tag, 0, &item));
			unitpp::assert_eq("Val really in cache", 987, item);
			CheckHr(m_qsda->get_VecItem(1024, tag, 1, &item));
			unitpp::assert_eq("Val really in cache", 333, item);
			CheckHr(m_qsda->get_VecItem(1024, tag, 3, &item));
			unitpp::assert_eq("Val really in cache", 555, item);
			CheckHr(m_qsda->get_VecItem(1024, tag, 4, &item));
			unitpp::assert_eq("Val really in cache", 321, item);
			int chvo2;
			CheckHr(m_qsda->get_VecSize(1024, tag, &chvo2));
			unitpp::assert_eq("Val really in cache", 5, chvo2);

			CheckDeletion(1024, tag, kcptReferenceSequence);
		}

		void CheckDeletion(HVO hvo, PropTag tag, int cpt)
		{
			// Make sure the specified property is present.
			ComBool fPresent;
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, cpt, 0, &fPresent));
			unitpp::assert_true("prop present", fPresent);
			// Make sure ClearVirtual gets rid of it
			CheckHr(m_qcda->ClearVirtualProperties());
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, cpt, 0, &fPresent));
			unitpp::assert_true("prop cleared", !fPresent);
		}

		void CheckDeletionMs(HVO hvo, PropTag tag, int ws)
		{
			// Make sure the specified property is present.
			ComBool fPresent;
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptMultiString, ws, &fPresent));
			unitpp::assert_true("prop present", fPresent);
			// Make sure ClearVirtual gets rid of it
			CheckHr(m_qcda->ClearVirtualProperties());
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptMultiString, ws, &fPresent));
			unitpp::assert_true("prop cleared", !fPresent);
		}

		void testObjVirtual()
		{
			DummyObjVirtualHandlerPtr qvhi;
			qvhi.Attach(new DummyObjVirtualHandler());
			CheckHr(SetNamesAndInstall(qvhi, L"SomeClass", L"ObjField", kcptReferenceAtom));
			IVwVirtualHandlerPtr qvhiRet;

			PropTag tag;
			CheckHr(qvhi->get_Tag(&tag));

			// Check the trivial behavior happens;
			HVO hvo;
			CheckHr(m_qsda->get_ObjectProp(257, tag, &hvo)); // This dummy prop works for any obj.
			unitpp::assert_eq("Got expected obj value", 959, hvo);

			// Check we still get S_FALSE for unknown prop
			HRESULT hr;
			CheckHr(hr= m_qsda->get_ObjectProp(257, 258, &hvo));
			unitpp::assert_eq("Got S_FALSE for unknown int prop", S_FALSE, hr);

			// Make it ComputeEveryTime and check that it doesn't get cached.
			CheckHr(qvhi->put_ComputeEveryTime(true));
			CheckHr(m_qsda->get_ObjectProp(1024, tag, &hvo));
			unitpp::assert_eq("Got expected CET value", 959, hvo);
			ComBool fIsInCache;
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptOwningAtom, 0, &fIsInCache));
			unitpp::assert_true("CET prop not in cache", !fIsInCache);

			// Make it writeable and write it.
			CheckHr(qvhi->put_Writeable(true));

			CheckHr(m_qsda->SetObjProp(1024, tag, 345));
			unitpp::assert_eq("Value was writen", 345, qvhi->m_hvo);
			unitpp::assert_eq("One val written", 1, qvhi->m_chvo);
			unitpp::assert_eq("no previous val(min)", 0, qvhi->m_ihvoMin);
			unitpp::assert_eq("no previous val(lim)", 0, qvhi->m_ihvoLim);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptReferenceAtom, 0, &fIsInCache));
			unitpp::assert_true("CET written val not in cache", !fIsInCache);

			// turn off write every time and write it.
			CheckHr(qvhi->put_ComputeEveryTime(false));
			CheckHr(m_qsda->SetObjProp(1024, tag, 765));
			unitpp::assert_eq("Value was writen", 765, qvhi->m_hvo);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptReferenceAtom, 0, &fIsInCache));
			unitpp::assert_true("CET written val in cache", fIsInCache);
			// try overwrite back to empty.
			CheckHr(m_qsda->SetObjProp(1024, tag, 0));
			unitpp::assert_eq("Value was writen", 0, qvhi->m_chvo);
			unitpp::assert_eq("prev val overwrite (min)", 0, qvhi->m_ihvoMin);
			unitpp::assert_eq("prev val overwrite (lim)", 1, qvhi->m_ihvoLim);

			CheckDeletion(1024, tag, kcptReferenceAtom);
		}

		void testStringVirtual()
		{
			DummyStrVirtualHandlerPtr qvhi;
			qvhi.Attach(new DummyStrVirtualHandler());
			CheckHr(SetNamesAndInstall(qvhi, L"SomeClass", L"StrField", kcptString));

			PropTag tag;
			CheckHr(qvhi->get_Tag(&tag));

			// Check the trivial behavior happens;
			ITsStringPtr qtss;
			CheckHr(m_qsda->get_StringProp(257, tag, &qtss)); // This dummy prop works for any obj.
			SmartBstr sbstrVal;
			CheckHr(qtss->get_Text(&sbstrVal));
			unitpp::assert_true("Got expected str value", wcscmp(sbstrVal, L"abc") == 0);

			// Check we still get S_FALSE for unknown prop
			HRESULT hr;
			CheckHr(hr= m_qsda->get_StringProp(257, 258, &qtss));
			unitpp::assert_eq("Got S_FALSE for unknown int prop", S_FALSE, hr);

			// Make it ComputeEveryTime and check that it doesn't get cached.
			CheckHr(qvhi->put_ComputeEveryTime(true));
			CheckHr(m_qsda->get_StringProp(1257, tag, &qtss)); // This dummy prop works for any obj.
			CheckHr(qtss->get_Text(&sbstrVal));
			unitpp::assert_true("Got expected str value CET", wcscmp(sbstrVal, L"abc") == 0);
			ComBool fIsInCache;
			CheckHr(m_qsda->get_IsPropInCache(1257, tag, kcptString, 0, &fIsInCache));
			unitpp::assert_true("CET prop not in cache", !fIsInCache);

			// Make it writeable and write it.
			CheckHr(qvhi->put_Writeable(true));
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			ITsStringPtr qtssNew;
			CheckHr(qtsf->MakeStringRgch(L"newStr", 6, g_wsEng, &qtssNew));
			CheckHr(m_qsda->SetString(1024, tag, qtssNew));
			unitpp::assert_eq("Value was writen", qtssNew, qvhi->m_valWritten);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptString, 0, &fIsInCache));
			unitpp::assert_true("CET written val not in cache", !fIsInCache);

			// turn off write every time and write it.
			CheckHr(qvhi->put_ComputeEveryTime(false));
			CheckHr(qtsf->MakeStringRgch(L"newStr2", 7, g_wsEng, &qtssNew));
			CheckHr(m_qsda->SetString(1024, tag, qtssNew));
			unitpp::assert_eq("Value was writen", qtssNew, qvhi->m_valWritten);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptString, 0, &fIsInCache));
			unitpp::assert_true("CET written val in cache", fIsInCache);

			CheckDeletion(1024, tag, kcptString);
		}

		void testMultiStringVirtual()
		{
			DummyMultiStrVirtualHandlerPtr qvhi;
			qvhi.Attach(new DummyMultiStrVirtualHandler());
			CheckHr(SetNamesAndInstall(qvhi, L"SomeClass", L"MsField", kcptMultiString));

			PropTag tag;
			CheckHr(qvhi->get_Tag(&tag));

			// Check the trivial behavior happens;
			ITsStringPtr qtss;
			CheckHr(m_qsda->get_MultiStringAlt(257, tag, g_wsEng, &qtss)); // This dummy prop works for any obj.
			SmartBstr sbstrVal;
			CheckHr(qtss->get_Text(&sbstrVal));
			unitpp::assert_true("Got expected str value", wcscmp(sbstrVal, L"def") == 0);

			// Check we still get S_FALSE for unknown prop
			HRESULT hr;
			CheckHr(hr = m_qsda->get_MultiStringAlt(257, 258, g_wsEng, &qtss));
			unitpp::assert_eq("Got S_FALSE for unknown int prop", S_FALSE, hr);

			// Make it ComputeEveryTime and check that it doesn't get cached.
			CheckHr(qvhi->put_ComputeEveryTime(true));
			CheckHr(m_qsda->get_MultiStringAlt(1257, tag, g_wsEng, &qtss)); // This dummy prop works for any obj.
			CheckHr(qtss->get_Text(&sbstrVal));
			unitpp::assert_true("Got expected str value CET", wcscmp(sbstrVal, L"def") == 0);
			ComBool fIsInCache;
			CheckHr(m_qsda->get_IsPropInCache(1257, tag, kcptMultiString, g_wsEng, &fIsInCache));
			unitpp::assert_true("CET prop not in cache", !fIsInCache);

			// Make it writeable and write it.
			CheckHr(qvhi->put_Writeable(true));
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			ITsStringPtr qtssNew;
			CheckHr(qtsf->MakeStringRgch(L"newStr", 6, g_wsEng, &qtssNew));
			CheckHr(m_qsda->SetMultiStringAlt(1024, tag, g_wsEng, qtssNew));
			unitpp::assert_eq("Value was writen", qtssNew, qvhi->m_valWritten);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptMultiString, g_wsEng, &fIsInCache));
			unitpp::assert_true("CET written val not in cache", !fIsInCache);

			// turn off write every time and write it.
			CheckHr(qvhi->put_ComputeEveryTime(false));
			CheckHr(qtsf->MakeStringRgch(L"newStr2", 7, g_wsEng, &qtssNew));
			CheckHr(m_qsda->SetMultiStringAlt(1024, tag, g_wsEng, qtssNew));
			unitpp::assert_eq("Value was writen", qtssNew, qvhi->m_valWritten);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptMultiString, g_wsEng, &fIsInCache));
			unitpp::assert_true("CET written val in cache", fIsInCache);

			CheckDeletionMs(1024, tag, g_wsEng);
		}

		void testUnicodeVirtual()
		{
			DummyUniVirtualHandlerPtr qvhi;
			qvhi.Attach(new DummyUniVirtualHandler());
			SetNamesAndInstall(qvhi, L"SomeClass", L"UniField", kcptUnicode);

			PropTag tag;
			CheckHr(qvhi->get_Tag(&tag));

			// Check the trivial behavior happens;
			SmartBstr sbstrVal;
			CheckHr(m_qsda->get_UnicodeProp(257, tag, &sbstrVal)); // This dummy prop works for any obj.
			unitpp::assert_true("Got expected uni value", wcscmp(sbstrVal, L"unicode") == 0);

			// Check we still get S_FALSE for unknown prop
			HRESULT hr;
			CheckHr(hr= m_qsda->get_UnicodeProp(257, 258, &sbstrVal));
			unitpp::assert_eq("Got S_FALSE for unknown int prop", S_FALSE, hr);

			// Make it ComputeEveryTime and check that it doesn't get cached.
			CheckHr(qvhi->put_ComputeEveryTime(true));
			CheckHr(m_qsda->get_UnicodeProp(1257, tag, &sbstrVal)); // This dummy prop works for any obj.
			unitpp::assert_true("Got expected uni value CET", wcscmp(sbstrVal, L"unicode") == 0);
			ComBool fIsInCache;
			CheckHr(m_qsda->get_IsPropInCache(1257, tag, kcptUnicode, 0, &fIsInCache));
			unitpp::assert_true("CET prop not in cache", !fIsInCache);

			// Make it writeable and write it.
			CheckHr(qvhi->put_Writeable(true));
			CheckHr(m_qsda->SetUnicode(1024, tag, L"newVal", 6));
			unitpp::assert_true("Value was writen", wcscmp(L"newVal", qvhi->m_valWritten.Chars()) == 0);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptUnicode, 0, &fIsInCache));
			unitpp::assert_true("CET written val not in cache", !fIsInCache);

			// turn off write every time and write it.
			CheckHr(qvhi->put_ComputeEveryTime(false));
			CheckHr(m_qsda->SetUnicode(1024, tag, L"newVal2", 7));
			unitpp::assert_true("Value was writen", wcscmp(L"newVal2", qvhi->m_valWritten.Chars()) == 0);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptUnicode, 0, &fIsInCache));
			unitpp::assert_true("CET written val in cache", fIsInCache);

			CheckDeletion(1024, tag, kcptUnicode);
		}
		// Finish write tests and implementation.
		// Finish final property types: Binary, Unknown, and Guid.
		// Make a VwOleDbDa and test that new virtual property added appears in MetaData (& implement).
		// Test reading and writing virtual props in VwOleDbDa, and also that writing real ones is not affected.
	public:
		TestVirtualHandlers();


		virtual void Setup()
		{
			m_qsda.CreateInstance(CLSID_VwCacheDa);
			CheckHr(m_qsda->QueryInterface(IID_IVwCacheDa, (void **) & m_qcda));
			CreateTestWritingSystemFactory();
			CheckHr(m_qsda->putref_WritingSystemFactory(g_qwsf));
			m_qmdcMem.CreateInstance(CLSID_FwMetaDataCache);
			StrUni stuPath = DirectoryFinder::FwRootCodeDir(); // assume this is something like ...fw\distfiles
			stuPath.Append(L"\\..\\Src\\Views\\Test\\VirtualsCm.xml");
			CheckHr(m_qmdcMem->InitXml(stuPath.Bstr(), true));
			CheckHr(m_qsda->putref_MetaDataCache(m_qmdcMem));
		}
		virtual void Teardown()
		{
			m_qsda.Clear();
			m_qcda.Clear();
			CloseTestWritingSystemFactory();
			m_qmdcMem.Clear();
		}
	};

	class TestVirtualHandlersInMdc : public unitpp::suite
	{
		IVwCacheDaPtr m_qcda;
		ISilDataAccessPtr m_qsda;
		IFwMetaDataCachePtr m_qmdc;
		IOleDbEncapPtr m_qode;

		HRESULT SetNamesAndInstall(IVwVirtualHandler * pvhi, OLECHAR * pszClass, OLECHAR * pszField, int cpt)
		{
				StrUni stuClass(pszClass);
				pvhi->put_ClassName(stuClass.Bstr());
				StrUni stuField(pszField);
				pvhi->put_FieldName(stuField.Bstr());
				pvhi->put_Type(cpt);
				return m_qcda->InstallVirtual(pvhi);
		}

	public:

		TestVirtualHandlersInMdc();
		void testInstallHandlerInDbDa()
		{
			DummyIntVirtualHandlerPtr qvhi;
			qvhi.Attach(new DummyIntVirtualHandler());
			CheckHr(SetNamesAndInstall(qvhi, L"StText", L"IntField", kcptInteger)); // Must be real class for VwOleDbDa
			IVwVirtualHandlerPtr qvhiRet;

			// Check we can retrieve it using class name and id;
			StrUni stuClass(L"StText");
			StrUni stuField(L"IntField");
			CheckHr(m_qcda->GetVirtualHandlerName(stuClass.Bstr(), stuField.Bstr(), &qvhiRet));
			unitpp::assert_eq("Retrieved int handler from names OK", qvhi.Ptr(), qvhiRet.Ptr());
			PropTag tag;
			CheckHr(qvhi->get_Tag(&tag));
			unitpp::assert_true("Id assigned", tag != 0);
			CheckHr(m_qcda->GetVirtualHandlerId(tag, &qvhiRet));
			unitpp::assert_eq("Retrieved int handler from ID OK", qvhi.Ptr(), qvhiRet.Ptr());
			// Check the trivial behavior happens;
			int val;
			CheckHr(m_qsda->get_IntProp(257, tag, &val)); // This dummy prop works for any obj.
			unitpp::assert_eq("Got expected int value", 957, val);
			// Test inability to overwrite with alternative handler.
			DummyIntVirtualHandlerPtr qvhi2;
			qvhi2.Attach(new DummyIntVirtualHandler());
			try{
				HRESULT hr;
				CheckHr(hr = SetNamesAndInstall(qvhi2, L"StText", L"IntField", kcptInteger));
				unitpp::assert_eq("Got error overwriting handler", E_INVALIDARG, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("Got error overwriting handler", E_INVALIDARG, thr.Result());
			}
			// Check we still get S_FALSE for unknown prop
			// (Not currently working for VwOleDbDa.)
			//hr = m_qsda->get_IntProp(257, 258, &val);
			//unitpp::assert_eq("Got S_FALSE for unknown int prop", S_FALSE, hr);

			// Make it ComputeEveryTime and check that it doesn't get cached.
			CheckHr(qvhi->put_ComputeEveryTime(true));
			CheckHr(m_qsda->get_IntProp(1024, tag, &val));
			unitpp::assert_eq("Got expected CET value", 957, val);
			ComBool fIsInCache;
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptInteger, 0, &fIsInCache));
			unitpp::assert_true("CET prop not in cache", !fIsInCache);

			// Make it writeable and write it.
			CheckHr(qvhi->put_Writeable(true));
			CheckHr(m_qsda->SetInt(1024, tag, 456));
			unitpp::assert_eq("Value was writen", 456, qvhi->m_valWritten);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptInteger, 0, &fIsInCache));
			unitpp::assert_true("CET written val not in cache", !fIsInCache);

			// turn off write every time and write it.
			CheckHr(qvhi->put_ComputeEveryTime(false));
			CheckHr(m_qsda->SetInt(1024, tag, 458));
			unitpp::assert_eq("Value was writen", 458, qvhi->m_valWritten);
			CheckHr(m_qsda->get_IsPropInCache(1024, tag, kcptInteger, 0, &fIsInCache));
			unitpp::assert_true("CET written val not in cache", fIsInCache);

			// Check that it got written to the MDC.
			ULONG uflid;
			CheckHr(m_qmdc->GetFieldId(stuClass.Bstr(), stuField.Bstr(), false, &uflid));
			unitpp::assert_eq("Got id from MDC", (ULONG) tag, uflid);
			ULONG uclsid;
			SmartBstr sbstrClass;
			SmartBstr sbstrField;
			CheckHr(m_qmdc->GetOwnClsName((ULONG) tag, &sbstrClass));
			unitpp::assert_true("Retrieved class name from MDC", wcscmp(sbstrClass.Chars(), L"StText") == 0);
			CheckHr(m_qmdc->GetFieldName((ULONG) tag, &sbstrField));
			unitpp::assert_true("Retrieved class name from MDC", wcscmp(sbstrField.Chars(), L"IntField") == 0);
			// Check the Clsid by feeding it back to retrieve the tag again.
			CheckHr(m_qmdc->GetOwnClsId((ULONG) tag, &uclsid));
			CheckHr(m_qmdc->GetFieldId2(uclsid, stuField.Bstr(), false, &uflid));
			unitpp::assert_eq("Got id from MDC", (ULONG) tag, uflid);

			ULONG rgflid[200];
			int cflid;
			CheckHr(m_qmdc->GetFields(uclsid, false, kgrfcptAll, 200, rgflid, &cflid));
			bool fFound = false;
			for (int i = 0; i < cflid && !fFound; ++i)
			{
				fFound = rgflid[i] == (ULONG) tag;
			}
			unitpp::assert_true("Found virtual in field list", fFound);

			int cpt;
			CheckHr(m_qmdc->GetFieldType((ULONG)tag, &cpt));
			unitpp::assert_eq("Got expected field type", kcptInteger, cpt);
			ComBool fVirtual;
			CheckHr(m_qmdc->get_IsVirtual((ULONG)tag, &fVirtual));
			unitpp::assert_true("Virtual prop is virtual", fVirtual);
		}

		virtual void Setup()
		{
			m_qsda.CreateInstance(CLSID_VwOleDbDa);
			CheckHr(m_qsda->QueryInterface(IID_IVwCacheDa, (void **) & m_qcda));
			m_qode.CreateInstance(CLSID_OleDbEncap);
			StrUni stuDBMName(L".\\SILFW");
			StrUni stuDbName(L"TestLangProj");
			CheckHr(m_qode->Init(stuDBMName.Bstr(), stuDbName.Bstr(), NULL, koltReturnError, 1000));
			m_qmdc.CreateInstance(CLSID_FwMetaDataCache);
			CheckHr(m_qmdc->Init(m_qode));
			ILgWritingSystemFactoryBuilderPtr qBuilder;
			qBuilder.CreateInstance(CLSID_LgWritingSystemFactoryBuilder);
			CreateTestWritingSystemFactory();
			ISetupVwOleDbDaPtr qsetup;
			CheckHr(m_qsda->QueryInterface(IID_ISetupVwOleDbDa, (void **)(&qsetup)));
			CheckHr(qsetup->Init(m_qode, m_qmdc, g_qwsf, NULL));
	}
		virtual void Teardown()
		{
			IVwOleDbDaPtr qodd;
			CheckHr(m_qsda->QueryInterface(IID_IVwOleDbDa, (void **)&qodd));
			CheckHr(qodd->Close());
			m_qcda->ClearAllData(); // qodd->Clear();
			m_qmdc.Clear();
			m_qsda.Clear();
			m_qcda.Clear();
			m_qode.Clear();
			CloseTestWritingSystemFactory();
		}
	};
}
#endif /*TestVirtualHandlers_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
