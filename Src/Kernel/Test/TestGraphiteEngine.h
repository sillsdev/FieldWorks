/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestGraphiteEngine.h
Responsibility:
Last reviewed:

	Unit tests for the GraphiteEngine class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTGRAPHITEENGINE_H_INCLUDED
#define TESTGRAPHITEENGINE_H_INCLUDED

#pragma once

#include "testFwKernel.h"
#include "RenderEngineTestBase.h"

namespace TestFwKernel
{
	/*******************************************************************************************
		Tests for TestGraphiteEngine
	 ******************************************************************************************/
	class TestGraphiteEngine : public RenderEngineTestBase, public unitpp::suite
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

		TestGraphiteEngine();
		virtual void Setup()
		{
			RenderEngineTestBase::Setup();
			ILgWritingSystemPtr qws;
			SmartBstr sbstr;

			SmartBstr fontStr(L"Charis SIL");
			sbstr.Assign(kszEng);
			m_qwsf->get_Engine(sbstr, &qws);
			qws->put_DefaultFontName(fontStr);

			sbstr.Assign(kszTest);
			m_qwsf->get_Engine(sbstr, &qws);
			qws->put_DefaultFontName(fontStr);

			sbstr.Assign(kszTest2);
			m_qwsf->get_Engine(sbstr, &qws);
			qws->put_DefaultFontName(fontStr);

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
			LgCharRenderProps chrp;
			chrp.dympHeight = 0;
			wcscpy_s(chrp.szFaceName, 32, StrUni(L"Charis SIL").Chars());
			qvg->SetupGraphics(&chrp);

			m_qre.CreateInstance(CLSID_GraphiteEngine);
			m_qre->InitRenderer(qvg, NULL);
			m_qre->putref_WritingSystemFactory(m_qwsf);

			qvg.Clear();
#ifdef WIN32
			::DeleteObject(hbm);
			::DeleteDC(hdc);
#endif
		}
		virtual void Teardown()
		{
			RenderEngineTestBase::Teardown();
		}

	};
}

#endif /*TESTGRAPHITEENGINE_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
