/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestRomRenderEngine.h
Responsibility:
Last reviewed:

	Unit tests for the RomRenderEngine class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTRomRenderEngine_H_INCLUDED
#define TESTRomRenderEngine_H_INCLUDED

#pragma once

#include "testLanguage.h"
#include "RenderEngineTestBase.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for RomRenderEngine
	 ******************************************************************************************/
	class TestRomRenderEngine : public RenderEngineTestBase, public unitpp::suite
	{
		void testNullArgs()
		{
			RenderEngineTestBase::VerifyNullArgs();
		}

		void testBreakPointing()
		{
			RenderEngineTestBase::VerifyBreakPointing();
		}

		virtual IRenderEnginePtr GetRenderer(LgCharRenderProps*)
		{
				return m_qre;
		}



	public:
		TestRomRenderEngine();
		virtual void Setup()
		{
			RenderEngineTestBase::Setup();
			m_qre = NewObj RomRenderEngine;
			m_qre->putref_WritingSystemFactory(m_qwsf);
		}
		virtual void Teardown()
		{
			RenderEngineTestBase::Teardown();
		}
	};
}

#endif /*TESTRomRenderEngine_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
