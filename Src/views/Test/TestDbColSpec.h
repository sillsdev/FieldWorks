/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestDbColSpec.h
Responsibility:
Last reviewed:

	Unit tests for the DbColSpec class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTDBCOLSPEC_H_INCLUDED
#define TESTDBCOLSPEC_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{
	class TestDbColSpec : public unitpp::suite
	{
		IDbColSpecPtr m_qdcs;

		void testNullArgs()
		{
			unitpp::assert_true("Non-null m_qdcs after setup", m_qdcs.Ptr() != 0);
			HRESULT hr;
#ifndef _DEBUG
			try{
				CheckHr(hr = m_qdcs->QueryInterface(IID_NULL, NULL));
				unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
#endif
			CheckHr(hr = m_qdcs->Push(0, 0, 0, 0));		// Prevent assertions in methods called below.
			unitpp::assert_eq("Push(0, 0, 0, 0) HRESULT", S_OK, hr);
			try{
				CheckHr(hr = m_qdcs->Size(NULL));
				unitpp::assert_eq("Size(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("Size(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qdcs->GetColInfo(0, NULL, NULL, NULL, NULL));
				unitpp::assert_eq("GetColInfo(0, NULL, NULL, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("GetColInfo(0, NULL, NULL, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qdcs->GetDbColType(0, NULL));
				unitpp::assert_eq("GetDbColType(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("GetDbColType(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qdcs->GetBaseCol(0, NULL));
				unitpp::assert_eq("GetBaseCol(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("GetBaseCol(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qdcs->GetTag(0, NULL));
				unitpp::assert_eq("GetTag(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("GetTag(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qdcs->GetWs(0, NULL));
				unitpp::assert_eq("GetWs(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("GetWs(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
		}
	public:
		TestDbColSpec();

		virtual void Setup()
		{
			DbColSpec::CreateCom(NULL, IID_IDbColSpec, (void **)&m_qdcs);
		}
		virtual void Teardown()
		{
			m_qdcs.Clear();
		}
	};
}

#endif /*TESTDBCOLSPEC_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
