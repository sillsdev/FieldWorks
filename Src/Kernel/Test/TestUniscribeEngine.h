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

#include "testFwKernel.h"
#include "RenderEngineTestBase.h"

namespace TestFwKernel
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

			m_qwsf->get_RendererFromChrp(qvg, &chrp, &qreneng);

			qvg.Clear();
#ifdef WIN32
			::DeleteObject(hbm);
			::DeleteDC(hdc);
#endif

			m_qre = dynamic_cast<UniscribeEngine *>(qreneng.Ptr());
			m_qre->get_WritingSystemFactory(&m_qwsf);
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
