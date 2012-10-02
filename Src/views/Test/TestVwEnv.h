/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestVwEnv.h
Responsibility:
Last reviewed:

	Unit tests for the VwEnv class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTVWENV_H_INCLUDED
#define TESTVWENV_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{
	class TestVwEnv : public unitpp::suite
	{
		VwEnvPtr m_qvwenv;

		void testNullArgs()
		{
			unitpp::assert_true("Non-null m_qvwenv after setup", m_qvwenv.Ptr() != 0);
			HRESULT hr;
			try{
				CheckHr(hr = m_qvwenv->AddObjProp(0, NULL, 0));
				unitpp::assert_eq("AddObjProp(0, NULL, 0) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("AddObjProp(0, NULL, 0) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvwenv->AddObjVec(0, NULL, 0));
				unitpp::assert_eq("AddObjVec(0, NULL, 0) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("AddObjVec(0, NULL, 0) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvwenv->AddObjVecItems(0, NULL, 0));
				unitpp::assert_eq("AddObjVecItems(0, NULL, 0) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("AddObjVecItems(0, NULL, 0) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvwenv->AddObj(0, NULL, 0));
				unitpp::assert_eq("AddObj(0, NULL, 0) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("AddObj(0, NULL, 0) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvwenv->AddLazyVecItems(0, NULL, 0));
				unitpp::assert_eq("AddLazyVecItems(0, NULL, 0) HRESULT", E_UNEXPECTED, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("AddLazyVecItems(0, NULL, 0) HRESULT", E_UNEXPECTED, thr.Result());
			}
			try{
				CheckHr(hr = m_qvwenv->AddLazyItems(NULL, 0, NULL, 0));
				unitpp::assert_eq("AddLazyItems(NULL, 0, NULL, 0) HRESULT", E_UNEXPECTED, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("AddLazyItems(NULL, 0, NULL, 0) HRESULT", E_UNEXPECTED, thr.Result());
			}
			try{
				CheckHr(hr = m_qvwenv->VwEnv::AddProp(0, NULL, 0));
				unitpp::assert_eq("VwEnv::AddProp(0, NULL, 0) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("VwEnv::AddProp(0, NULL, 0) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvwenv->CurrentObject(NULL));
				unitpp::assert_eq("CurrentObject(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("CurrentObject(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvwenv->get_OpenObject(NULL));
				unitpp::assert_eq("get_OpenObject(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_OpenObject(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvwenv->get_EmbeddingLevel(NULL));
				unitpp::assert_eq("get_EmbeddingLevel(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_EmbeddingLevel(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvwenv->GetOuterObject(0, NULL, NULL, NULL));
				unitpp::assert_eq("GetOuterObject(0, NULL, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("GetOuterObject(0, NULL, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvwenv->get_DataAccess(NULL));
				unitpp::assert_eq("get_DataAccess(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_DataAccess(NULL) HRESULT", E_POINTER, thr.Result());
			}

			CheckHr(hr = m_qvwenv->AddStringAltSeq(0, NULL, 0));
			unitpp::assert_eq("AddStringAltSeq(0, NULL, 0) HRESULT", S_OK, hr);

			try{
				CheckHr(hr = m_qvwenv->AddString(NULL));
				unitpp::assert_eq("AddString(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("AddString(NULL) HRESULT", E_POINTER, thr.Result());
			}
			// This causes a memory leak if executed, since the VwEnv is not initialized.
			//hr = m_qvwenv->AddPicture(NULL);
			//unitpp::assert_eq("AddPicture(NULL) HRESULT", E_UNEXPECTED, hr);

			try{
				CheckHr(hr = m_qvwenv->put_StringProperty(0, NULL));
				unitpp::assert_eq("put_StringProperty(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("put_StringProperty(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvwenv->get_StringWidth(NULL, NULL, NULL, NULL));
				unitpp::assert_eq("get_StringWidth(NULL, NULL, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_StringWidth(NULL, NULL, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
	//		hr = m_qvwenv->AddDerivedProp(NULL, 0, NULL, 0);		// Not implemented yet.
	//		unitpp::assert_eq("AddDerivedProp(NULL, 0, NULL, 0) HRESULT", E_POINTER, hr);
	//		hr = m_qvwenv->AddMultiProp(NULL, NULL, 0, NULL, 0);	// Not implemented yet.
	//		unitpp::assert_eq("AddMultiProp(NULL, NULL, 0, NULL, 0) HRESULT", E_POINTER, hr);
	//		hr = m_qvwenv->StartDependency(NULL, NULL, 0);			// Not implemented yet.
	//		unitpp::assert_eq("StartDependency(NULL, NULL, 0) HRESULT", E_POINTER, hr);
	//		hr = m_qvwenv->AddWindow(NULL, 0, FALSE, FALSE);		// Not implemented yet.
	//		unitpp::assert_eq("AddWindow(NULL, 0, FALSE, FALSE) HRESULT", E_POINTER, hr);

	//		hr = m_qvwenv->NoteDependency(NULL, NULL, 0);		// requires valid m_pgboxCurr
	//		unitpp::assert_eq("NoteDependency(NULL, NULL, 0) HRESULT", E_POINTER, hr);
	//		hr = m_qvwenv->AddStringProp(0, NULL);				// requires valid m_pgboxCurr
	//		unitpp::assert_eq("AddStringProp(0, NULL) HRESULT", E_POINTER, hr);
	//		hr = m_qvwenv->AddStringAltMember(0, 0, NULL);		// requires valid m_pgboxCurr
	//		unitpp::assert_eq("AddStringAltMember(0, 0, NULL) HRESULT", E_POINTER, hr);

	//		hr = m_qvwenv->put_Props(NULL);						// requires valid m_qzvps
	//		unitpp::assert_eq("put_Props(NULL) HRESULT", E_POINTER, hr);
		}
	public:
		TestVwEnv();

		virtual void Setup()
		{
			m_qvwenv.Attach(NewObj VwEnv);
		}
		virtual void Teardown()
		{
			m_qvwenv.Clear();
		}
	};
}

#endif /*TESTVWENV_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
