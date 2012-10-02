/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestUniscribeEngine.h
Responsibility:
Last reviewed:

	Unit tests for the UniscribeEngine class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTUNISCRIBEENGINE_H_INCLUDED
#define TESTUNISCRIBEENGINE_H_INCLUDED

#pragma once

#include "testLanguage.h"
#include "RenderEngineTestBase.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for TestUniscribeEngine
	 ******************************************************************************************/
	class TestUniscribeEngine : public RenderEngineTestBase, public unitpp::suite
	{
	public:
		void testNullArgs()
		{
			RenderEngineTestBase::VerifyNullArgs();
		}

		void testBreakPointing()
		{
			RenderEngineTestBase::VerifyBreakPointing();
		}

		TestUniscribeEngine();
		virtual void Setup()
		{
			CreateTestWritingSystemFactory(&m_qwsf);
			CreateTestWritingSystem(m_qwsf, kwsTest, kszTest);
			CreateTestWritingSystem(m_qwsf, kwsTest2, kszTest2);
			IRenderEnginePtr qreneng;
			LgCharRenderProps chrp;
			wcscpy_s(chrp.szFaceName, L"Times New Roman");
			chrp.ws = kwsEng;
			chrp.ttvBold = kttvOff;
			chrp.ttvItalic = kttvOff;
			m_qwsf->get_RendererFromChrp(&chrp, &qreneng);
			m_qre = dynamic_cast<UniscribeEngine *>(qreneng.Ptr());
			m_qre->get_WritingSystemFactory(&m_qwsf);
			RenderEngineTestBase::Setup();
		}
		virtual void Teardown()
		{
			RenderEngineTestBase::Teardown();
		}

	};
}

#endif /*TESTUNISCRIBEENGINE_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
