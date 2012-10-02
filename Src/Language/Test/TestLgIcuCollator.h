/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2008 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestLgIcuCollator.h
Responsibility:
Last reviewed:

	Unit tests for the LgIcuCharPropEngine class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLGICUCOLLATOR_H_INCLUDED
#define TESTLGICUCOLLATOR_H_INCLUDED

#pragma once

#include "testLanguage.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for LgIcuCollator (ICU based implementation)
	 ******************************************************************************************/
	class TestLgIcuCollator : public unitpp::suite
	{
		ILgCollatingEnginePtr m_qCollator;

		// This tests the ILgCollatingEngine::Compare method (TE-6264)
		void testCompare()
		{
			SmartBstr wsStr(L"fr");
			m_qCollator->Open(wsStr);

			SmartBstr str1(L"something");
			SmartBstr str2(L"frére");

			int nVal;
			m_qCollator->Compare(str1, str2, fcoDefault, &nVal);
			m_qCollator->Close();

			unitpp::assert_true("Compare returned wrong value", nVal > 0);
		}

	public:
		TestLgIcuCollator();
		virtual void SuiteSetup()
		{
			LgIcuCollator::CreateCom(NULL,
				IID_ILgCollatingEngine, (void **)&m_qCollator);
		}
		virtual void SuiteTeardown()
		{
			m_qCollator.Clear();
		}
	};
}
#endif