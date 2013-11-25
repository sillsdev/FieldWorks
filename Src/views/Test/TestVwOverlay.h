/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestVwOverlay.h
Responsibility:
Last reviewed:

	Unit tests for the VwOverlay class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTVWOVERLAY_H_INCLUDED
#define TESTVWOVERLAY_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{
	class TestVwOverlay : public unitpp::suite
	{
		IVwOverlayPtr m_qvo;

		void testNullArgs()
		{
			unitpp::assert_true("Non-null m_qvo after setup", m_qvo.Ptr() != 0);
			HRESULT hr;
#ifndef _DEBUG
			try{
				CheckHr(hr = m_qvo->QueryInterface(IID_NULL, NULL));
				unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("QueryInterface(IID_NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
#endif
			try{
				CheckHr(hr = m_qvo->get_Name(NULL));
				unitpp::assert_eq("get_Name(NULL) HRESULT", E_UNEXPECTED, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_Name(NULL) HRESULT", E_UNEXPECTED, thr.Result());
			}
			CheckHr(hr = m_qvo->put_Name(NULL));
			unitpp::assert_eq("put_Name(NULL) HRESULT", S_OK, hr);

			try{
				CheckHr(hr = m_qvo->get_Guid(NULL));
				unitpp::assert_eq("get_Guid(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_Guid(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->put_Guid(NULL));
				unitpp::assert_eq("put_Guid(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("put_Guid(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->get_PossListId(NULL));
				unitpp::assert_eq("get_PossListId(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_PossListId(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->get_Flags(NULL));
				unitpp::assert_eq("get_Flags(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_Flags(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->get_FontName(NULL));
				unitpp::assert_eq("get_FontName(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_FontName(NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qvo->put_FontName(NULL));
			unitpp::assert_eq("put_FontName(NULL) HRESULT", S_OK, hr);
			try{
				CheckHr(hr = m_qvo->FontNameRgch(NULL));
				unitpp::assert_eq("FontNameRgch(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("FontNameRgch(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->get_FontSize(NULL));
				unitpp::assert_eq("get_FontSize(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_FontSize(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->get_MaxShowTags(NULL));
				unitpp::assert_eq("get_MaxShowTags(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_MaxShowTags(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->get_CTags(NULL));
				unitpp::assert_eq("get_CTags(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("get_CTags(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->GetDbTagInfo(0, NULL, NULL, NULL, NULL, NULL, NULL, NULL));
				unitpp::assert_eq(
				"GetDbTagInfo(0, NULL, NULL, NULL, NULL, NULL, NULL, NULL) HRESULT",
				E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq(
				"GetDbTagInfo(0, NULL, NULL, NULL, NULL, NULL, NULL, NULL) HRESULT",
				E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->SetTagInfo(NULL, 0, 0, NULL, NULL, 0, 0, 0, 0, FALSE));
				unitpp::assert_eq("SetTagInfo(NULL, 0, 0, NULL, NULL, 0, 0, 0, 0, FALSE) HRESULT",
				E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("SetTagInfo(NULL, 0, 0, NULL, NULL, 0, 0, 0, 0, FALSE) HRESULT",
				E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->GetTagInfo(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL));
				unitpp::assert_eq(
				"GetTagInfo(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL) HRESULT",
				E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq(
				"GetTagInfo(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL) HRESULT",
				E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->GetDlgTagInfo(0, NULL, NULL, NULL, NULL, NULL, NULL, NULL));
				unitpp::assert_eq(
				"GetDlgTagInfo(0, NULL, NULL, NULL, NULL, NULL, NULL, NULL) HRESULT",
				E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq(
				"GetDlgTagInfo(0, NULL, NULL, NULL, NULL, NULL, NULL, NULL) HRESULT",
				E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->GetDispTagInfo(NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL,
				0, NULL));
				unitpp::assert_eq("GetDispTagInfo(NULL, NULL, NULL, ..., NULL) HRESULT",
				E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("GetDispTagInfo(NULL, NULL, NULL, ..., NULL) HRESULT",
				E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->RemoveTag(NULL));
				unitpp::assert_eq("RemoveTag(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("RemoveTag(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qvo->Merge(NULL, NULL));
				unitpp::assert_eq("Merge(NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr){
				unitpp::assert_eq("Merge(NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}

		}
	public:
		TestVwOverlay();

		virtual void Setup()
		{
			VwOverlay::CreateCom(NULL, IID_IVwOverlay, (void **)&m_qvo);
		}
		virtual void Teardown()
		{
			m_qvo.Clear();
		}
	};
}

#endif /*TESTVWOVERLAY_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
