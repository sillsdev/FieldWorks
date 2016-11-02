/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestRomRenderEngine.h
Responsibility:
Last reviewed:

	Unit tests for the RomRenderEngine class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTRomRenderEngine_H_INCLUDED
#define TESTRomRenderEngine_H_INCLUDED

#pragma once

#include "testViews.h"
#include "RenderEngineTestBase.h"

namespace TestViews
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

	public:
		TestRomRenderEngine();
		virtual void Setup()
		{
			RenderEngineTestBase::Setup();
			m_qre = NewObj RomRenderEngine;
			m_qre->putref_WritingSystemFactory(g_qwsf);
			m_qre->putref_RenderEngineFactory(m_qref);
		}
		virtual void Teardown()
		{
			m_qre.Clear();
			RenderEngineTestBase::Teardown();
		}
	};
}

#endif /*TESTRomRenderEngine_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
