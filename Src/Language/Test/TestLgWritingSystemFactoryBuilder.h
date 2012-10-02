/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestLgWritingSystemFactoryBuilder.h
Responsibility:
Last reviewed:

	Unit tests for the LgWritingSystemFactoryBuilder class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLGWRITINGSYSTEMFACTORYBUILDER_H_INCLUDED
#define TESTLGWRITINGSYSTEMFACTORYBUILDER_H_INCLUDED

#pragma once

#include "testLanguage.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for LgWritingSystemFactoryBuilder
	 ******************************************************************************************/
	class TestLgWritingSystemFactoryBuilder : public unitpp::suite
	{
		ILgWritingSystemFactoryBuilderPtr m_qwsfb;

		void testNullArgs()
		{
			unitpp::assert_true("m_qwsfb", m_qwsfb.Ptr());
			HRESULT hr;
			try{
				CheckHr(hr = m_qwsfb->GetWritingSystemFactory(NULL, NULL, NULL));
				unitpp::assert_eq("GetWritingSystemFactory(NULL, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetWritingSystemFactory(NULL, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qwsfb->GetWritingSystemFactoryNew(NULL, NULL, NULL, NULL));
				unitpp::assert_eq("GetWritingSystemFactoryNew(NULL, NULL, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetWritingSystemFactoryNew(NULL, NULL, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qwsfb->Deserialize(NULL, NULL));
				unitpp::assert_eq("Deserialize(NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Deserialize(NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
		}

	public:
		TestLgWritingSystemFactoryBuilder();
		virtual void SuiteSetup()
		{
			LgWritingSystemFactoryBuilder::CreateCom(NULL,
				IID_ILgWritingSystemFactoryBuilder, (void **)&m_qwsfb);
		}
		virtual void SuiteTeardown()
		{
			m_qwsfb.Clear();
		}
	};
}

#endif /*TESTLGWRITINGSYSTEMFACTORYBUILDER_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)
