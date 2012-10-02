/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestLgFontManager.h
Responsibility:
Last reviewed:

	Unit tests for the LgFontManager class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLGFONTMANAGER_H_INCLUDED
#define TESTLGFONTMANAGER_H_INCLUDED

#pragma once

#include "testLanguage.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for LgFontManager
	 ******************************************************************************************/
	class TestLgFontManager : public unitpp::suite
	{
		ILgFontManagerPtr m_qfm0;

		void testNullArgs()
		{
			unitpp::assert_true("m_qfm0", m_qfm0.Ptr());
			HRESULT hr;
			try{
				CheckHr(hr = m_qfm0->IsFontAvailable(NULL, NULL));
				unitpp::assert_eq("IsFontAvailable(NULL, NULL) HRESULT", E_INVALIDARG, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("IsFontAvailable(NULL, NULL) HRESULT", E_INVALIDARG, thr.Result());
			}
			try{
				CheckHr(hr = m_qfm0->IsFontAvailableRgch(0, NULL, NULL));
				unitpp::assert_eq("IsFontAvailableRgch(0, NULL, NULL) HRESULT", E_INVALIDARG, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("IsFontAvailableRgch(0, NULL, NULL) HRESULT", E_INVALIDARG, thr.Result());
			}
			try{
				CheckHr(hr = m_qfm0->AvailableFonts(NULL));
				unitpp::assert_eq("AvailableFonts(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("AvailableFonts(NULL) HRESULT", E_POINTER, thr.Result());
			}
		}

	public:
		TestLgFontManager();
		virtual void SuiteSetup()
		{
			LgFontManager::CreateCom(NULL, IID_ILgFontManager, (void **)&m_qfm0);
		}
		virtual void SuiteTeardown()
		{
			m_qfm0.Clear();
		}
	};
}

#endif /*TESTLGFONTMANAGER_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
