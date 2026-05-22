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
			RenderEngineTestBase::VerifyBreakPointing(35);
		}

		TestUniscribeEngine();
		int MeasureTextWithFeatures(const wchar_t * pszText, const wchar_t * pszFontVar)
		{
			int dxWidth = 0;
#if defined(WIN32) || defined(_M_X64)
			int dxMax = 4000;
			HDC hdc = ::CreateCompatibleDC(::GetDC(::GetDesktopWindow()));
			HBITMAP hbm = ::CreateCompatibleBitmap(hdc, dxMax, dxMax);
			::SelectObject(hdc, hbm);
			::SetMapMode(hdc, MM_TEXT);

			IVwGraphicsWin32Ptr qvg;
			qvg.CreateInstance(CLSID_VwGraphicsWin32);
			qvg->Initialize(hdc);

			LgCharRenderProps chrp;
			ZeroMemory(&chrp, isizeof(chrp));
			wcscpy_s(chrp.szFaceName, _countof(chrp.szFaceName), L"Charis SIL");
			wcscpy_s(chrp.szFontVar, _countof(chrp.szFontVar), pszFontVar);
			chrp.ws = g_wsEng;
			chrp.ttvBold = kttvOff;
			chrp.ttvItalic = kttvOff;
			chrp.dympHeight = 14000;
			qvg->SetupGraphics(&chrp);

			ILgWritingSystemFactoryPtr qwsf;
			m_qre->get_WritingSystemFactory(&qwsf);

			IVwTextSourcePtr qts;
			TxtSrc ts(pszText, qwsf, pszFontVar);
			ts.QueryInterface(IID_IVwTextSource, (void **)&qts);
			int cch;
			CheckHr(qts->get_Length(&cch));

			ILgSegmentPtr qseg;
			int dichLimSeg;
			LgEndSegmentType est;
			CheckHr(m_qre->FindBreakPoint(qvg, qts, NULL, 0, cch, cch, TRUE, TRUE, dxMax,
				klbWordBreak, klbLetterBreak, ktwshAll, FALSE, &qseg, &dichLimSeg, &dxWidth,
				&est, NULL));
			unitpp::assert_true("OpenType feature test should produce a segment", qseg);
			CheckHr(qseg->get_Width(0, qvg, &dxWidth));

			qvg.Clear();
			::DeleteObject(hbm);
			::DeleteDC(hdc);
#endif
			return dxWidth;
		}

#if defined(WIN32) || defined(_M_X64)
		struct RenderedFeatureText
		{
			int dxWidth;
			int cNonWhitePixels;
			Vector<DWORD> vPixels;
		};

		class ScopedPrivateFont
		{
		public:
			ScopedPrivateFont(const wchar_t * pszPath)
				: m_stuPath(pszPath), m_cFonts(0)
			{
				m_cFonts = ::AddFontResourceExW(m_stuPath.Chars(), FR_PRIVATE, 0);
			}

			~ScopedPrivateFont()
			{
				if (m_cFonts > 0)
					::RemoveFontResourceExW(m_stuPath.Chars(), FR_PRIVATE, 0);
			}

			bool Loaded() const
			{
				return m_cFonts > 0;
			}

		private:
			StrUni m_stuPath;
			int m_cFonts;
		};

		class BitmapRenderTarget
		{
		public:
			BitmapRenderTarget(int dxWidth, int dyHeight)
				: m_dxWidth(dxWidth), m_dyHeight(dyHeight), m_hdc(NULL), m_hbm(NULL),
				m_hbmOld(NULL), m_prgbBits(NULL)
			{
				m_hdc = ::CreateCompatibleDC(NULL);
				unitpp::assert_true("CreateCompatibleDC should return a memory DC", m_hdc != NULL);

				BITMAPINFO bmi;
				ZeroMemory(&bmi, isizeof(bmi));
				bmi.bmiHeader.biSize = isizeof(BITMAPINFOHEADER);
				bmi.bmiHeader.biWidth = m_dxWidth;
				bmi.bmiHeader.biHeight = -m_dyHeight;
				bmi.bmiHeader.biPlanes = 1;
				bmi.bmiHeader.biBitCount = 32;
				bmi.bmiHeader.biCompression = BI_RGB;
				m_hbm = ::CreateDIBSection(m_hdc, &bmi, DIB_RGB_COLORS,
					reinterpret_cast<void **>(&m_prgbBits), NULL, 0);
				unitpp::assert_true("CreateDIBSection should return a bitmap", m_hbm != NULL);

				m_hbmOld = ::SelectObject(m_hdc, m_hbm);
				unitpp::assert_true("SelectObject should select the render bitmap", m_hbmOld != NULL);
				::SetMapMode(m_hdc, MM_TEXT);

				RECT rcFill = {0, 0, m_dxWidth, m_dyHeight};
				HBRUSH hbrWhite = ::CreateSolidBrush(RGB(255, 255, 255));
				unitpp::assert_true("CreateSolidBrush should return a white brush", hbrWhite != NULL);
				::FillRect(m_hdc, &rcFill, hbrWhite);
				::DeleteObject(hbrWhite);
			}

			~BitmapRenderTarget()
			{
				if (m_hdc && m_hbmOld)
					::SelectObject(m_hdc, m_hbmOld);
				if (m_hbm)
					::DeleteObject(m_hbm);
				if (m_hdc)
					::DeleteDC(m_hdc);
			}

			HDC DeviceContext() const
			{
				return m_hdc;
			}

			void CopyPixels(Vector<DWORD> & vpixels) const
			{
				vpixels.Resize(m_dxWidth * m_dyHeight);
				memcpy(vpixels.Begin(), m_prgbBits, m_dxWidth * m_dyHeight * isizeof(DWORD));
			}

		private:
			int m_dxWidth;
			int m_dyHeight;
			HDC m_hdc;
			HBITMAP m_hbm;
			HGDIOBJ m_hbmOld;
			DWORD * m_prgbBits;
		};

		StrUni GetCharisFontPath()
		{
			wchar_t rgchPath[MAX_PATH];
			DWORD cchPath = ::GetModuleFileNameW(NULL, rgchPath, _countof(rgchPath));
			unitpp::assert_true("GetModuleFileNameW should return the test executable path",
				cchPath > 0 && cchPath < _countof(rgchPath));

			wchar_t * pchLastSlash = wcsrchr(rgchPath, L'\\');
			unitpp::assert_true("Test executable path should contain a directory separator",
				pchLastSlash != NULL);
			*(pchLastSlash + 1) = 0;
			wcscat_s(rgchPath, _countof(rgchPath),
				L"TestData\\Fonts\\CharisSIL-5.000\\CharisSIL-R.ttf");

			DWORD dwAttributes = ::GetFileAttributesW(rgchPath);
			unitpp::assert_true("Charis SIL test font should be copied beside TestViews.exe",
				dwAttributes != INVALID_FILE_ATTRIBUTES &&
				(dwAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0);
			return StrUni(rgchPath);
		}

		void SetDefaultFontForTest(const wchar_t * pszFontName)
		{
			ILgWritingSystemPtr qws;
			CheckHr(g_qwsf->get_EngineOrNull(g_wsEng, &qws));
			MockLgWritingSystem * pws = dynamic_cast<MockLgWritingSystem *>(qws.Ptr());
			unitpp::assert_true("English test writing system should be a mock writing system",
				pws != NULL);
			StrUni stuFont(pszFontName);
			CheckHr(pws->put_DefaultFontName(stuFont.Bstr()));
		}

		RenderedFeatureText RenderTextWithFeatures(const wchar_t * pszText, const wchar_t * pszFontVar)
		{
			const int kdxBitmap = 640;
			const int kdyBitmap = 180;
			const int kdxMax = 4000;
			BitmapRenderTarget target(kdxBitmap, kdyBitmap);

			IVwGraphicsWin32Ptr qvg;
			qvg.CreateInstance(CLSID_VwGraphicsWin32);
			qvg->Initialize(target.DeviceContext());

			LgCharRenderProps chrp;
			ZeroMemory(&chrp, isizeof(chrp));
			wcscpy_s(chrp.szFaceName, _countof(chrp.szFaceName), L"Charis SIL");
			wcscpy_s(chrp.szFontVar, _countof(chrp.szFontVar), pszFontVar);
			chrp.clrFore = kclrBlack;
			chrp.clrBack = kclrWhite;
			chrp.clrUnder = kclrRed;
			chrp.ws = g_wsEng;
			chrp.ttvBold = kttvOff;
			chrp.ttvItalic = kttvOff;
			chrp.dympHeight = 26000;
			qvg->SetupGraphics(&chrp);

			ILgWritingSystemFactoryPtr qwsf;
			m_qre->get_WritingSystemFactory(&qwsf);

			IVwTextSourcePtr qts;
			TxtSrc ts(pszText, qwsf, pszFontVar);
			ts.QueryInterface(IID_IVwTextSource, (void **)&qts);
			int cch;
			CheckHr(qts->get_Length(&cch));

			ILgSegmentPtr qseg;
			int dichLimSeg;
			int dxWidth;
			LgEndSegmentType est;
			CheckHr(m_qre->FindBreakPoint(qvg, qts, NULL, 0, cch, cch, TRUE, TRUE, kdxMax,
				klbWordBreak, klbLetterBreak, ktwshAll, FALSE, &qseg, &dichLimSeg, &dxWidth,
				&est, NULL));
			unitpp::assert_true("OpenType render test should produce a segment", qseg);

			RECT rcSrc = {0, 0, kdzmpInch, kdzmpInch};
			RECT rcDst = {10, 10, kdzmpInch + 10, kdzmpInch + 10};
			RenderedFeatureText rendered;
			CheckHr(qseg->DrawText(0, qvg, rcSrc, rcDst, &rendered.dxWidth));
			::GdiFlush();

			target.CopyPixels(rendered.vPixels);
			rendered.cNonWhitePixels = CountNonWhitePixels(rendered);
			qvg.Clear();
			return rendered;
		}

		int CountNonWhitePixels(const RenderedFeatureText & rendered)
		{
			int cNonWhitePixels = 0;
			for (int i = 0; i < rendered.vPixels.Size(); ++i)
			{
				if ((rendered.vPixels[i] & 0x00FFFFFF) != 0x00FFFFFF)
					++cNonWhitePixels;
			}
			return cNonWhitePixels;
		}

		int CountDifferentPixels(const RenderedFeatureText & first, const RenderedFeatureText & second)
		{
			unitpp::assert_eq("Rendered bitmaps should have the same pixel count",
				first.vPixels.Size(), second.vPixels.Size());
			int cDifferentPixels = 0;
			for (int i = 0; i < first.vPixels.Size(); ++i)
			{
				if ((first.vPixels[i] & 0x00FFFFFF) != (second.vPixels[i] & 0x00FFFFFF))
					++cDifferentPixels;
			}
			return cDifferentPixels;
		}
#endif

		void testOpenTypeFeatureMetrics()
		{
#if defined(WIN32) || defined(_M_X64)
			ScopedPrivateFont font(GetCharisFontPath().Chars());
			unitpp::assert_true("Charis SIL test font should load", font.Loaded());
			SetDefaultFontForTest(L"Charis SIL");

			int dxWithoutLigatures = MeasureTextWithFeatures(L"office official affinity", L"liga=0");
			int dxWithLigatures = MeasureTextWithFeatures(L"office official affinity", L"liga=1");

			unitpp::assert_true("OpenType feature-off segment width should be positive",
				dxWithoutLigatures > 0);
			unitpp::assert_true("OpenType feature-on segment width should be positive",
				dxWithLigatures > 0);
			unitpp::assert_true("Charis SIL liga feature should change segment metrics",
				dxWithoutLigatures != dxWithLigatures);
#endif
		}

		void testOpenTypeFeatureRenderedPixels()
		{
#if defined(WIN32) || defined(_M_X64)
			ScopedPrivateFont font(GetCharisFontPath().Chars());
			unitpp::assert_true("Charis SIL test font should load", font.Loaded());
			SetDefaultFontForTest(L"Charis SIL");

			RenderedFeatureText regular = RenderTextWithFeatures(L"small caps verify", L"smcp=0");
			RenderedFeatureText smallCaps = RenderTextWithFeatures(L"small caps verify", L"smcp=1");

			unitpp::assert_true("OpenType feature-off render should draw text",
				regular.cNonWhitePixels > 0);
			unitpp::assert_true("OpenType feature-on render should draw text",
				smallCaps.cNonWhitePixels > 0);
			unitpp::assert_true("Charis SIL smcp feature should change rendered pixels",
				CountDifferentPixels(regular, smallCaps) > 0);
#endif
		}

		void testOpenTypeFeatureRenderedPixelsSwitchState()
		{
#if defined(WIN32) || defined(_M_X64)
			ScopedPrivateFont font(GetCharisFontPath().Chars());
			unitpp::assert_true("Charis SIL test font should load", font.Loaded());
			SetDefaultFontForTest(L"Charis SIL");

			RenderedFeatureText featureOnFirst = RenderTextWithFeatures(L"small caps verify", L"smcp=1");
			RenderedFeatureText featureOff = RenderTextWithFeatures(L"small caps verify", L"smcp=0");
			RenderedFeatureText featureOnAgain = RenderTextWithFeatures(L"small caps verify", L"smcp=1");

			unitpp::assert_true("Feature-on render should differ from feature-off render",
				CountDifferentPixels(featureOnFirst, featureOff) > 0);
			unitpp::assert_eq("Feature-on render should be stable after switching off and back on",
				0, CountDifferentPixels(featureOnFirst, featureOnAgain));
#endif
		}

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
#if defined(WIN32) || defined(_M_X64)
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
#if defined(WIN32) || defined(_M_X64)
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
