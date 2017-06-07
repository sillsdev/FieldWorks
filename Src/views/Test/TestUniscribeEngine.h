/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestUniscribeEngine.h
Responsibility:
Last reviewed:

	Unit tests for the UniscribeEngine class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTUNISCRIBEENGINE_H_INCLUDED
#define TESTUNISCRIBEENGINE_H_INCLUDED

#pragma once

#include "testViews.h"
#include "RenderEngineTestBase.h"

namespace TestViews
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
			RenderEngineTestBase::Setup();
			IRenderEnginePtr qreneng;
			LgCharRenderProps chrp;
			wcscpy_s(chrp.szFaceName, L"Times New Roman");
			chrp.ws = g_wsEng;
			chrp.ttvBold = kttvOff;
			chrp.ttvItalic = kttvOff;
			chrp.dympHeight = 0;

			HDC hdc;
#ifdef WIN32
			int dxMax = 600;
			hdc = ::CreateCompatibleDC(::GetDC(::GetDesktopWindow()));
			HBITMAP hbm = ::CreateCompatibleBitmap(hdc, dxMax, dxMax);
			::SelectObject(hdc, hbm);
			::SetMapMode(hdc, MM_TEXT);
#else
			hdc = 0;
#endif
			IVwGraphicsWin32Ptr qvg;
			qvg.CreateInstance(CLSID_VwGraphicsWin32);
			qvg->Initialize(hdc);

			qvg->SetupGraphics(&chrp);
			ILgWritingSystemPtr qws;
			g_qwsf->get_EngineOrNull(g_wsEng, &qws);
			m_qref->get_Renderer(qws, qvg, &m_qre);

			qvg.Clear();
#ifdef WIN32
			::DeleteObject(hbm);
			::DeleteDC(hdc);
#endif
		}
		virtual void Teardown()
		{
			m_qre.Clear();
			RenderEngineTestBase::Teardown();
		}

	};
}

#endif /*TESTUNISCRIBEENGINE_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
