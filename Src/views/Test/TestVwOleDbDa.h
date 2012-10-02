/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestVwOleDbDa.h
Responsibility:
Last reviewed:

	Unit tests for the DbColSpec class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTVWOLEDBDA_H_INCLUDED
#define TESTVWOLEDBDA_H_INCLUDED

#pragma once

#include "testViews.h"
#include "..\\..\\DbAccess\\OleDbEncap.h"

#define MY_FAVORITE_REF_SEQ 1001004
#define MY_FAVORITE_OWN_SEQ 1001005

namespace TestViews
{
	static SmartBstr s_sbstrLastSqlCommand;
	static int s_nLastStatementType;

	// Dummy MetaDataCache to return dummy names needed in Set methods in VwOleDbDa.
	class MockFwMetaDataCache : public FwMetaDataCache
	{
		typedef FwMetaDataCache SuperClass;
	public:
		MockFwMetaDataCache()
		{
		}
		STDMETHODIMP GetFieldName(ULONG luFlid, BSTR * pbstrFieldName)
		{
			StrUni stu = L"FldName";
			stu.GetBstr(pbstrFieldName);
			return S_OK;
		}
		STDMETHODIMP GetOwnClsName(ULONG luFlid, BSTR * pbstrOwnClsName)
		{
			StrUni stu = L"ClsName";
			stu.GetBstr(pbstrOwnClsName);
			return S_OK;
		}
		STDMETHODIMP GetFieldType(ULONG luFlid, int * piType)
		{
			switch(luFlid)
			{
			case MY_FAVORITE_REF_SEQ:
				*piType = kcptReferenceSequence;
				break;
			case MY_FAVORITE_OWN_SEQ:
				*piType = kcptOwningSequence;
				break;
			default:
				*piType = kcptMultiBigString;
				break;
			}
			return S_OK;
		}
	};
	typedef ComSmartPtr<MockFwMetaDataCache> MockFwMetaDataCachePtr;


	// Dummy OleDbAccess to ignore commands used in VwOleDbDa.
	class MockOleDbCommand : public OleDbCommand
	{
		typedef OleDbCommand SuperClass;
	public:

		MockOleDbCommand()
		{
		}
		STDMETHODIMP SetParameter(ULONG iluParamIndex, DWORD dwFlags, BSTR bstrParamName,
			WORD nDataType, ULONG * prgluDataBuffer, ULONG cluBufferLength)
		{
			return S_OK;
		}
		STDMETHODIMP ExecCommand(BSTR bstrSqlStatement, int nStatementType)
		{
			s_sbstrLastSqlCommand = bstrSqlStatement;
			s_nLastStatementType = nStatementType;
			return S_OK;
		}
	};
	typedef ComSmartPtr<MockOleDbCommand> MockOleDbCommandPtr;


	// Dummy OleDbEncap to return a dummy OleDbCommand, needed in Set methods in VwOleDbDa.
	class MockOleDbEncap : public OleDbEncap
	{
		typedef OleDbEncap SuperClass;
	public:
		MockOleDbEncap()
		{
		}
		STDMETHODIMP CreateCommand(IOleDbCommand ** ppodc)
		{
			MockOleDbCommandPtr qodc;
			qodc.Attach(NewObj MockOleDbCommand);
			*ppodc = qodc.Detach();
			return S_OK;
		}
	};
	typedef ComSmartPtr<MockOleDbEncap> MockOleDbEncapPtr;


	// Dummy VwOleDbDa to use for testing Set methods. It bypasses database I/O
	// but allows the cache to function normally.
	class MockVwOleDbDa : public VwOleDbDa
	{
		typedef VwOleDbDa SuperClass;
	public:
		MockVwOleDbDa()
		{
			m_qmdc.Attach(NewObj MockFwMetaDataCache);
			m_qode.Attach(NewObj MockOleDbEncap);
		}
		STDMETHODIMP CacheCurrTimeStamp(HVO hvo)
		{
			return S_OK;
		}

		STDMETHODIMP MakeNewObject(int clid, HVO hvoOwner, PropTag tag, int ord, HVO * phvoNew)
		{
			*phvoNew = m_hvoNext++;
			DoInsert(hvoOwner, *phvoNew, tag, ord);

			return S_OK;
		}

		STDMETHODIMP Load(BSTR bstrSqlStmt, IDbColSpec * pdcs, HVO hvoBase, int crowMax,
			IAdvInd * padvi, ComBool fNotifyChange)
		{
			return S_OK;
		}

		Vector<PropChangedInfo>& GetPropChangedQueue()
		{
			return m_vPropChangeds;
		}
	};
	typedef ComSmartPtr<MockVwOleDbDa> MockVwOleDbDaPtr;



	// Now we get to the actual tests.
	class TestVwOleDbDa : public unitpp::suite
	{
		IVwOleDbDaPtr m_qodde;

		/*--------------------------------------------------------------------------------------
			Test string save methods for proper handling of normalizion.
		--------------------------------------------------------------------------------------*/
		void testSaveNormalization()
		{
			HRESULT hr;
			MockVwOleDbDaPtr qvcd;
			qvcd.Attach(NewObj MockVwOleDbDa);
			HVO hvoDummy = 100;
			int flidDummy = 1001001;
			StrUni stuIn = L"T\x00e9sting";
			StrUni stuNFD = L"Te\x0301sting";
			ITsStringPtr qtssIn;
			ITsStringPtr qtssOut;
			ITsStrFactoryPtr qtsf;

			qtsf.CreateInstance(CLSID_TsStrFactory);
			qtsf->MakeStringRgch(stuIn.Chars(), stuIn.Length(), 1, &qtssIn);

			// Test SetUnicode method.
			SmartBstr sbstrOut;
			hr = qvcd->SetUnicode(hvoDummy, flidDummy, const_cast<OLECHAR *>(stuIn.Chars()),
				stuIn.Length());
			hr = qvcd->get_UnicodeProp(hvoDummy, flidDummy, &sbstrOut);
			unitpp::assert_true("SetUnicode normalized correctly to NFD",
				sbstrOut.Equals(stuNFD.Chars(), stuNFD.Length()));

			// Test SetString method.
			hr = qvcd->SetString(hvoDummy, flidDummy, qtssIn);
			hr = qvcd->get_StringProp(hvoDummy, flidDummy, &qtssOut);
			hr = qtssOut->get_Text(&sbstrOut);
			unitpp::assert_true("SetString normalized correctly to NFD",
				sbstrOut.Equals(stuNFD.Chars(), stuNFD.Length()));

			// Test SetMultiString method.
			hr = qvcd->SetMultiStringAlt(hvoDummy, flidDummy, 1, qtssIn);
			hr = qvcd->get_MultiStringAlt(hvoDummy, flidDummy, 1, &qtssOut);
			hr = qtssOut->get_Text(&sbstrOut);
			unitpp::assert_true("SetMultiString normalized correctly to NFD",
				sbstrOut.Equals(stuNFD.Chars(), stuNFD.Length()));
		}

		/*--------------------------------------------------------------------------------------
			Test the COM methods that fetch the underlying run information.
		--------------------------------------------------------------------------------------*/
		void testNullArgs()
		{
			unitpp::assert_true("Non-null m_qodde after setup", m_qodde.Ptr() != 0);
#ifndef _DEBUG
			HRESULT hr;
			try{
				CheckHr(hr = m_qodde->QueryInterface(IID_NULL, NULL));
				unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
#endif
//			hr = m_qodde->Push(0, 0, 0, 0);		// Prevent assertions in methods called below.
//			unitpp::assert_eq("Push(0, 0, 0, 0) HRESULT", S_OK, hr);
		}

		/*--------------------------------------------------------------------------------------
			Test the Replace method when used to insert a new ref at the beginning of a
			sequence.
		--------------------------------------------------------------------------------------*/
		void testReplaceRefSeqAtBeginning()
		{
			HRESULT hr;
			MockVwOleDbDaPtr qvcd;
			qvcd.Attach(NewObj MockVwOleDbDa);
			HVO hvoDummy = 108;
			int flidDummyRS = MY_FAVORITE_REF_SEQ;
			HVO hvoExistingRef = 456;
			CheckHr(qvcd->CacheVecProp(hvoDummy, flidDummyRS, &hvoExistingRef, 1));
			HVO hvoItemToInsert = 956;
			CheckHr(hr = qvcd->Replace(hvoDummy, flidDummyRS, 0, 0,	&hvoItemToInsert, 1));
			wchar rgchExpectedSql[700];
			wsprintf(rgchExpectedSql,
				L"declare @hdoc int;\r\n"
	L"exec sp_xml_preparedocument @hdoc output, '<root><Obj Id=\"956\" Ord=\"0\"/></root>';\r\n"
				L"exec ReplaceRefSeq$ %d, 108, NULL, @hdoc, 456, 1",
				MY_FAVORITE_REF_SEQ);
			unitpp::assert_true("Bogus SQL generated",
				!wcscmp(rgchExpectedSql, s_sbstrLastSqlCommand.Chars()));
			unitpp::assert_eq("SQL Statement type", knSqlStmtStoredProcedure,
				s_nLastStatementType);
			int chvo;
			CheckHr(qvcd->get_VecSize(hvoDummy, flidDummyRS, &chvo));
			unitpp::assert_eq("Incorrect number of references in sequence", 2, chvo);
			HVO hvo;
			CheckHr(qvcd->get_VecItem(hvoDummy, flidDummyRS, 0, &hvo));
			unitpp::assert_eq("Item passed to Replace method should be first in sequence",
				hvoItemToInsert, hvo);
			CheckHr(qvcd->get_VecItem(hvoDummy, flidDummyRS, 1, &hvo));
			unitpp::assert_eq("Item originally inserted in vector should be second in sequence",
				hvoExistingRef, hvo);
		}

		/*--------------------------------------------------------------------------------------
			Test the Replace method when used to insert a new ref at the end of a sequence.
		--------------------------------------------------------------------------------------*/
		void testReplaceRefSeqAtEnd()
		{
			HRESULT hr;
			MockVwOleDbDaPtr qvcd;
			qvcd.Attach(NewObj MockVwOleDbDa);
			HVO hvoDummy = 108;
			int flidDummyRS = MY_FAVORITE_REF_SEQ;
			HVO hvoExistingRef = 456;
			CheckHr(qvcd->CacheVecProp(hvoDummy, flidDummyRS, &hvoExistingRef, 1));
			HVO hvoItemToInsert = 956;
			CheckHr(hr = qvcd->Replace(hvoDummy, flidDummyRS, 1, 1,	&hvoItemToInsert, 1));
			wchar rgchExpectedSql[700];
			wsprintf(rgchExpectedSql,
				L"declare @hdoc int;\r\n"
	L"exec sp_xml_preparedocument @hdoc output, '<root><Obj Id=\"956\" Ord=\"0\"/></root>';\r\n"
				L"exec ReplaceRefSeq$ %d, 108, NULL, @hdoc",
				MY_FAVORITE_REF_SEQ);
			unitpp::assert_true("Bogus SQL generated",
				!wcscmp(rgchExpectedSql, s_sbstrLastSqlCommand.Chars()));
			unitpp::assert_eq("SQL Statement type", knSqlStmtStoredProcedure,
				s_nLastStatementType);
			int chvo;
			CheckHr(qvcd->get_VecSize(hvoDummy, flidDummyRS, &chvo));
			unitpp::assert_eq("Incorrect number of references in sequence", 2, chvo);
			HVO hvo;
			CheckHr(qvcd->get_VecItem(hvoDummy, flidDummyRS, 1, &hvo));
			unitpp::assert_eq("Item passed to Replace method should be second in sequence",
				hvoItemToInsert, hvo);
			CheckHr(qvcd->get_VecItem(hvoDummy, flidDummyRS, 0, &hvo));
			unitpp::assert_eq("Item originally inserted in vector should be first in sequence",
				hvoExistingRef, hvo);
		}

		/*--------------------------------------------------------------------------------------
			Test the Replace method when used to insert a new ref in the middle of a sequence.
		--------------------------------------------------------------------------------------*/
		void testReplaceRefSeqInsertInMiddle()
		{
			HRESULT hr;
			MockVwOleDbDaPtr qvcd;
			qvcd.Attach(NewObj MockVwOleDbDa);
			HVO hvoDummy = 108;
			int flidDummyRS = MY_FAVORITE_REF_SEQ;
			HVO rghvoExistingRefs[2];
			rghvoExistingRefs[0] = 456;
			rghvoExistingRefs[1] = 456;
			CheckHr(qvcd->CacheVecProp(hvoDummy, flidDummyRS, rghvoExistingRefs, 2));
			HVO hvoItemToInsert = 956;
			CheckHr(hr = qvcd->Replace(hvoDummy, flidDummyRS, 1, 1,	&hvoItemToInsert, 1));
			wchar rgchExpectedSql[700];
			wsprintf(rgchExpectedSql,
				L"declare @hdoc int;\r\n"
	L"exec sp_xml_preparedocument @hdoc output, '<root><Obj Id=\"956\" Ord=\"0\"/></root>';\r\n"
				L"exec ReplaceRefSeq$ %d, 108, NULL, @hdoc, 456, 2",
				MY_FAVORITE_REF_SEQ);
			unitpp::assert_true("Bogus SQL generated",
				!wcscmp(rgchExpectedSql, s_sbstrLastSqlCommand.Chars()));
			unitpp::assert_eq("SQL Statement type", knSqlStmtStoredProcedure,
				s_nLastStatementType);
			int chvo;
			CheckHr(qvcd->get_VecSize(hvoDummy, flidDummyRS, &chvo));
			unitpp::assert_eq("Incorrect number of references in sequence", 3, chvo);
			HVO hvo;
			CheckHr(qvcd->get_VecItem(hvoDummy, flidDummyRS, 0, &hvo));
			unitpp::assert_eq(
				"First item originally inserted in vector should be first in sequence",
				rghvoExistingRefs[0], hvo);
			CheckHr(qvcd->get_VecItem(hvoDummy, flidDummyRS, 1, &hvo));
			unitpp::assert_eq("Item passed to Replace method should be second in sequence",
				hvoItemToInsert, hvo);
			CheckHr(qvcd->get_VecItem(hvoDummy, flidDummyRS, 2, &hvo));
			unitpp::assert_eq(
				"Second item originally inserted in vector should be third in sequence",
				rghvoExistingRefs[1], hvo);
		}

		/*--------------------------------------------------------------------------------------
			Test the Replace method when used to replace the second ocurrence of a referenced
			object with a a new ref.
		--------------------------------------------------------------------------------------*/
		void testReplaceRefSeqReplaceItem2()
		{
			HRESULT hr;
			MockVwOleDbDaPtr qvcd;
			qvcd.Attach(NewObj MockVwOleDbDa);
			HVO hvoDummy = 108;
			int flidDummyRS = MY_FAVORITE_REF_SEQ;
			HVO rghvoExistingRefs[3];
			rghvoExistingRefs[0] = 456;
			rghvoExistingRefs[1] = 456;
			rghvoExistingRefs[2] = 789;
			CheckHr(qvcd->CacheVecProp(hvoDummy, flidDummyRS, rghvoExistingRefs, 3));
			HVO hvoItemToInsert = 956;
			CheckHr(hr = qvcd->Replace(hvoDummy, flidDummyRS, 1, 2,	&hvoItemToInsert, 1));
			wchar rgchExpectedSql[700];
			wsprintf(rgchExpectedSql,
				L"declare @hdoc int;\r\n"
	L"exec sp_xml_preparedocument @hdoc output, '<root><Obj Id=\"956\" Ord=\"0\"/></root>';\r\n"
				L"exec ReplaceRefSeq$ %d, 108, NULL, @hdoc, 456, 2, 456, 2",
				MY_FAVORITE_REF_SEQ);
			unitpp::assert_true("Bogus SQL generated",
				!wcscmp(rgchExpectedSql, s_sbstrLastSqlCommand.Chars()));
			unitpp::assert_eq("SQL Statement type", knSqlStmtStoredProcedure,
				s_nLastStatementType);
			int chvo;
			CheckHr(qvcd->get_VecSize(hvoDummy, flidDummyRS, &chvo));
			unitpp::assert_eq("Incorrect number of references in sequence", 3, chvo);
			HVO hvo;
			CheckHr(qvcd->get_VecItem(hvoDummy, flidDummyRS, 0, &hvo));
			unitpp::assert_eq("First item originally inserted in vector should be first in sequence",
				rghvoExistingRefs[0], hvo);
			CheckHr(qvcd->get_VecItem(hvoDummy, flidDummyRS, 1, &hvo));
			unitpp::assert_eq("Item passed to Replace method should be second in sequence",
				hvoItemToInsert, hvo);
			CheckHr(qvcd->get_VecItem(hvoDummy, flidDummyRS, 2, &hvo));
			unitpp::assert_eq("Lasst item originally inserted in vector should be last in sequence",
				rghvoExistingRefs[2], hvo);
		}

		/*--------------------------------------------------------------------------------------
			Tests queuing PropChanged calls where all the PropChanged calls have the same ivMin
		--------------------------------------------------------------------------------------*/
		void testQueuePropChanges_SameIvMin()
		{
			MockVwOleDbDaPtr qvcd;
			qvcd.Attach(NewObj MockVwOleDbDa);

			CheckHr(qvcd->SuppressPropChanges());
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 5, 1, 0));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 5, 2, 1));

			Vector<PropChangedInfo>& queue = qvcd->GetPropChangedQueue();
			unitpp::assert_eq("1. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("1. test:", kpctNotifyAll, 4711, ktagSection_Content, 5, 2, 0, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 8, 1, 2));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 8, 0, 1));
			unitpp::assert_eq("2. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("2. test:", kpctNotifyAll, 4711, ktagSection_Content, 8, 0, 2, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 2, 1, 2));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 2, 2, 1));
			unitpp::assert_eq("3. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("3. test:", kpctNotifyAll, 4711, ktagSection_Content, 2, 2, 2, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 5, 2, 1));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 5, 1, 2));
			unitpp::assert_eq("4. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("4. test:", kpctNotifyAll, 4711, ktagSection_Content, 5, 1, 1, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 4, 0, 1));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 4, 1, 2));
			unitpp::assert_eq("5. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("5. test:", kpctNotifyAll, 4711, ktagSection_Content, 4, 1, 3, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 1, 6, 7));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 1, 5, 6));
			unitpp::assert_eq("6. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("6. test:", kpctNotifyAll, 4711, ktagSection_Content, 1, 5, 7, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 10, 6, 7));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 10, 5, 6));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 10, 4, 5));
			unitpp::assert_eq("7. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("7. test:", kpctNotifyAll, 4711, ktagSection_Content, 10, 4, 7, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 8, 6, 7));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 8, 7, 2));
			unitpp::assert_eq("8. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("8. test:", kpctNotifyAll, 4711, ktagSection_Content, 8, 11, 7, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 5, 6, 7));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 5, 7, 2));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 5, 3, 3));
			unitpp::assert_eq("9. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("9. test:", kpctNotifyAll, 4711, ktagSection_Content, 5, 11, 7, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 3, 7, 2));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 3, 6, 7));
			unitpp::assert_eq("10. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("10. test:", kpctNotifyAll, 4711, ktagSection_Content, 3, 6, 2, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 2, 7, 6));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 2, 2, 7));
			unitpp::assert_eq("11. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("11. test:", kpctNotifyAll, 4711, ktagSection_Content, 2, 2, 6, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 2, 0, 1));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 2, 0, 1));
			unitpp::assert_eq("12. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("12. test:", kpctNotifyAll, 4711, ktagSection_Content, 2, 0, 2, queue[0]);
		}

		/*--------------------------------------------------------------------------------------
			Tests queuing PropChanged calls with different ivMin values
		--------------------------------------------------------------------------------------*/
		void testQueuePropChanges_DifferentIvMin()
		{
			MockVwOleDbDaPtr qvcd;
			qvcd.Attach(NewObj MockVwOleDbDa);

			Vector<PropChangedInfo>& queue = qvcd->GetPropChangedQueue();
			CheckHr(qvcd->SuppressPropChanges());

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 0, 10, 5));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 5,  2, 5));
			unitpp::assert_eq("1. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("1. test:", kpctNotifyAll, 4711, ktagSection_Content, 0, 7, 5, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 0,  2, 5));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 5, 10, 5));
			unitpp::assert_eq("2. test: Queue has wrong size", 2, queue.Size());
			VerifyQueuedPropChanges("2. test, 1st PropChanged:",
				kpctNotifyAll, 4711, ktagSection_Content, 0, 2, 5, queue[0]);
			VerifyQueuedPropChanges("2. test, 2nd PropChanged:",
				kpctNotifyAll, 4711, ktagSection_Content, 5, 10, 5, queue[1]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 0, 5, 2));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 4, 5, 10));
			unitpp::assert_eq("3. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("3. test:", kpctNotifyAll, 4711, ktagSection_Content, 0, 9, 11, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 5, 5, 2));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 9, 5, 10));
			unitpp::assert_eq("4. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("4. test:", kpctNotifyAll, 4711, ktagSection_Content, 5, 9, 11, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 3, 5, 2));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, 4711, ktagSection_Content, 6, 9, 6));
			unitpp::assert_eq("5. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("5. test:", kpctNotifyAll, 4711, ktagSection_Content, 3, 12, 6, queue[0]);
		}

		/*--------------------------------------------------------------------------------------
			Tests queuing PropChanged calls where a subsequent propchanged call is for an object
			that is owned by an object that is already queued up.
		--------------------------------------------------------------------------------------*/
		void testQueuePropChanges_SuppressOwnedObjects()
		{
			MockVwOleDbDaPtr qvcd;
			qvcd.Attach(NewObj MockVwOleDbDa);

			CheckHr(qvcd->SuppressPropChanges());
			Vector<PropChangedInfo>& queue = qvcd->GetPropChangedQueue();

			// 4711 is the object that owns an object whose prop-changed we want to ignore.
			HVO hvoScrBook = 4711;
			HVO hvoNewFootnote;
			CheckHr(qvcd->MakeNewObject(345, hvoScrBook, ktagBook_Footnotes, 0, &hvoNewFootnote));
			CheckHr(qvcd->CacheObjProp(hvoNewFootnote, kflidCmObject_Owner, hvoScrBook));
			CheckHr(qvcd->CacheIntProp(hvoNewFootnote, kflidCmObject_OwnFlid, ktagBook_Footnotes));

			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, hvoScrBook, ktagBook_Footnotes, 0, 16, 15));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, hvoNewFootnote, ktagFootnote_Paragraphs, 0, 1, 0));
			unitpp::assert_eq("1. test: Queue has wrong size", 1, queue.Size());
			VerifyQueuedPropChanges("1. test:", kpctNotifyAll, hvoScrBook, ktagBook_Footnotes, 0, 16, 15, queue[0]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, hvoScrBook, ktagBook_Footnotes, 5, 16, 15));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, hvoNewFootnote, ktagFootnote_Paragraphs, 0, 1, 0));
			unitpp::assert_eq("2. test: Queue has wrong size", 2, queue.Size());
			VerifyQueuedPropChanges("2. test:", kpctNotifyAll, hvoScrBook, ktagBook_Footnotes, 5, 16, 15, queue[0]);
			VerifyQueuedPropChanges("2. test:", kpctNotifyAll, hvoNewFootnote, ktagFootnote_Paragraphs, 0, 1, 0, queue[1]);

			queue.Clear();
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, hvoScrBook, ktagBook_Footnotes, 0, 0, 15));
			CheckHr(qvcd->PropChanged(NULL, kpctNotifyAll, hvoNewFootnote, ktagFootnote_Paragraphs, 0, 1, 0));
			unitpp::assert_eq("3. test: Queue has wrong size", 2, queue.Size());
			VerifyQueuedPropChanges("3. test:", kpctNotifyAll, hvoScrBook, ktagBook_Footnotes, 0, 0, 15, queue[0]);
			VerifyQueuedPropChanges("3. test:", kpctNotifyAll, hvoNewFootnote, ktagFootnote_Paragraphs, 0, 1, 0, queue[1]);
		}

		void VerifyQueuedPropChanges(const std::string& sTest, int pct, int hvo, int tag, int ivMin,
			int cvIns, int cvDel, PropChangedInfo pci)
		{
			unitpp::assert_eq(sTest + " Unexpected pct", pct, pci.pct);
			unitpp::assert_eq(sTest + " Unexpected hvo", hvo, pci.hvo);
			unitpp::assert_eq(sTest + " Unexpected tag", tag, pci.tag);
			unitpp::assert_eq(sTest + " Unexpected ivMin", ivMin, pci.ivMin);
			unitpp::assert_eq(sTest + " Unexpected cvIns", cvIns, pci.cvIns);
			unitpp::assert_eq(sTest + " Unexpected cvDel", cvDel, pci.cvDel);
		}

		void testVwCacheDaMoveOwnSeq()
		{
			MockVwOleDbDaPtr qvcd;
			qvcd.Attach(NewObj MockVwOleDbDa);

			HVO hvoDummy = 1081;
			int flidDummyOS = MY_FAVORITE_OWN_SEQ;
			HVO rghvoExistingObjs[5] = {4561, 4562, 4563, 4564, 4565 };
			HRESULT hr = qvcd->CacheVecProp(hvoDummy, flidDummyOS, rghvoExistingObjs, 5);
			unitpp::assert_eq("caching owning sequence bad HRESULT", hr, S_OK);
			VwCacheDa * pcda = qvcd.Ptr();
			hr = pcda->MoveOwnSeq(hvoDummy, flidDummyOS, 4, 4, hvoDummy, flidDummyOS, 3);
			unitpp::assert_eq("MoveOwnSeq(4,4,3) bad HRESULT", hr, S_OK);
			hr = pcda->MoveOwnSeq(hvoDummy, flidDummyOS, 3, 3, hvoDummy, flidDummyOS, 2);
			unitpp::assert_eq("MoveOwnSeq(3,3,2) bad HRESULT", hr, S_OK);
			hr = pcda->MoveOwnSeq(hvoDummy, flidDummyOS, 2, 2, hvoDummy, flidDummyOS, 1);
			unitpp::assert_eq("MoveOwnSeq(2,2,1) bad HRESULT", hr, S_OK);
			hr = pcda->MoveOwnSeq(hvoDummy, flidDummyOS, 1, 1, hvoDummy, flidDummyOS, 0);
			unitpp::assert_eq("MoveOwnSeq(1,1,0) bad HRESULT", hr, S_OK);

			HVO rghvoT[5];
			int chvo;
			hr = qvcd->VecProp(hvoDummy, flidDummyOS, 5, &chvo, rghvoT);
			unitpp::assert_eq("VecProp() bad HRESULT [A]", hr, S_OK);
			unitpp::assert_eq("wrong number of objects [A]", chvo, 5);
			unitpp::assert_eq("wrong rghvoT[0] [A]", rghvoExistingObjs[4], rghvoT[0]);
			unitpp::assert_eq("wrong rghvoT[1] [A]", rghvoExistingObjs[0], rghvoT[1]);
			unitpp::assert_eq("wrong rghvoT[2] [A]", rghvoExistingObjs[1], rghvoT[2]);
			unitpp::assert_eq("wrong rghvoT[3] [A]", rghvoExistingObjs[2], rghvoT[3]);
			unitpp::assert_eq("wrong rghvoT[4] [A]", rghvoExistingObjs[3], rghvoT[4]);

			hr = pcda->MoveOwnSeq(hvoDummy, flidDummyOS, 1, 1, hvoDummy, flidDummyOS, 3);
			unitpp::assert_eq("MoveOwnSeq(1,1,3) bad HRESULT", hr, S_OK);
			hr = pcda->MoveOwnSeq(hvoDummy, flidDummyOS, 2, 2, hvoDummy, flidDummyOS, 4);
			unitpp::assert_eq("MoveOwnSeq(2,2,4) bad HRESULT", hr, S_OK);
			hr = pcda->MoveOwnSeq(hvoDummy, flidDummyOS, 3, 3, hvoDummy, flidDummyOS, 5);
			unitpp::assert_eq("MoveOwnSeq(3,3,5) bad HRESULT", hr, S_OK);

			hr = qvcd->VecProp(hvoDummy, flidDummyOS, 5, &chvo, rghvoT);
			unitpp::assert_eq("VecProp() bad HRESULT [B]", hr, S_OK);
			unitpp::assert_eq("wrong number of objects [B]", chvo, 5);
			unitpp::assert_eq("wrong rghvoT[0] [B]", rghvoExistingObjs[4], rghvoT[0]);
			unitpp::assert_eq("wrong rghvoT[1] [B]", rghvoExistingObjs[1], rghvoT[1]);
			unitpp::assert_eq("wrong rghvoT[2] [B]", rghvoExistingObjs[2], rghvoT[2]);
			unitpp::assert_eq("wrong rghvoT[3] [B]", rghvoExistingObjs[3], rghvoT[3]);
			unitpp::assert_eq("wrong rghvoT[4] [B]", rghvoExistingObjs[0], rghvoT[4]);
		}



	public:
		TestVwOleDbDa();

		virtual void Setup()
		{
			VwOleDbDa::CreateCom(NULL, IID_IVwOleDbDa, (void **)&m_qodde);
			StrUtil::InitIcuDataDir();
		}

		virtual void Teardown()
		{
			m_qodde.Clear();
		}
	};
}

#endif /*TESTVWOLEDBDA_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
