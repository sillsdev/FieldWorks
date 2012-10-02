/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2005 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestVwGraphics.h

	Unit tests for the VwGraphics class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestVwGraphics_H_INCLUDED
#define TestVwGraphics_H_INCLUDED

#pragma once

#include "testViews.h"
#include <set>
#include <string>

template<class T>
bool within(const T tolerance, const T x, const T y)
{
	return std::abs(x - y) <= tolerance;
}

class FontsOnSystem {
public:
	void Initialize(){
		m_sFontFullNames.clear();

		HDC hdc = ::CreateDC(TEXT("DISPLAY"),NULL,NULL,NULL);
		Assert(hdc);
		LOGFONT lf;
		lf.lfCharSet = DEFAULT_CHARSET;
		lf.lfFaceName[0] = '\0';
		lf.lfPitchAndFamily = 0;
		EnumFontFamiliesEx(hdc, &lf, (FONTENUMPROC)&EnumFontFamExProc, (LPARAM)this, 0);
		::DeleteDC(hdc);
	}
	bool IsFontInstalledOnSystem(const TCHAR * szFontFullName) const {

		return m_sFontFullNames.find(szFontFullName) != m_sFontFullNames.end();
	}
protected:
	static int CALLBACK EnumFontFamExProc(
		ENUMLOGFONTEX *lpelfe,    // logical-font data
		NEWTEXTMETRICEX *lpntme,  // physical-font data
		DWORD FontType,           // type of font
		LPARAM lParam             // application-defined data
	){
		FontsOnSystem* pThis = reinterpret_cast<FontsOnSystem*>(lParam);
		pThis->AddFont((TCHAR *)&lpelfe->elfFullName);
		return 1;
	}
	void AddFont(const TCHAR * szFontFullName)
	{
		std::basic_string<TCHAR> strFontFullName(szFontFullName);
		m_sFontFullNames.insert(strFontFullName);
	}

	std::set<std::basic_string<TCHAR> > m_sFontFullNames;
};

namespace TestViews
{

	// Now we get to the actual tests.
	class TestVwGraphicSuperscript : public unitpp::suite
	{
		void testSuperscriptGraphite()
		{
			unitpp::assert_true("SILDoulos PigLatinDemo font must be installed",
				m_FOS.IsFontInstalledOnSystem(L"SILDoulos PigLatinDemo"));

			int dSuperscriptSize9ptNumerator;
			int dSuperscriptSize9ptDenominator;
			int dSuperscriptYOffset9ptNumerator;
			int dSuperscriptYOffset9ptDenominator;
			int dSuperscriptSize70ptNumerator;
			int dSuperscriptSize70ptDenominator;
			int dSuperscriptYOffset70ptNumerator;
			int dSuperscriptYOffset70ptDenominator;

			wcscpy_s(m_chrp.szFaceName, L"SILDoulos PigLatinDemo");

			m_chrp.dympHeight = 9000;
			CheckHr(m_qvg->SetupGraphics(&m_chrp));

			CheckHr(m_qvg->GetSuperscriptHeightRatio(&dSuperscriptSize9ptNumerator, &dSuperscriptSize9ptDenominator));
			unitpp::assert_true("Graphite Size of superscript less than 100% of normal",
				((double)dSuperscriptSize9ptNumerator / dSuperscriptSize9ptDenominator) < 1);
			unitpp::assert_true("Graphite Size of superscript greater than 0% of normal",
				((double)dSuperscriptSize9ptNumerator / dSuperscriptSize9ptDenominator) > 0);

			CheckHr(m_qvg->GetSuperscriptYOffsetRatio(&dSuperscriptYOffset9ptNumerator, &dSuperscriptYOffset9ptDenominator));
			unitpp::assert_true("Graphite offset of superscript less than 100% of height",
				((double)dSuperscriptYOffset9ptNumerator / dSuperscriptYOffset9ptDenominator) < 1);
			unitpp::assert_true("Graphite offset of superscript greater than 0% of height",
				((double)dSuperscriptYOffset9ptNumerator / dSuperscriptYOffset9ptDenominator) > 0);

			m_chrp.dympHeight = 70000;
			CheckHr(m_qvg->SetupGraphics(&m_chrp));
			CheckHr(m_qvg->GetSuperscriptHeightRatio(&dSuperscriptSize70ptNumerator, &dSuperscriptSize70ptDenominator));
			unitpp::assert_eq("Graphite Ratio of superscript same for different size fonts",
				((double)dSuperscriptSize9ptNumerator / dSuperscriptSize9ptDenominator),
				((double)dSuperscriptSize70ptNumerator / dSuperscriptSize70ptDenominator));

			unitpp::assert_eq("Graphite Numerator of 70pt superscript", 650, dSuperscriptSize70ptNumerator);
			unitpp::assert_eq("Graphite Denominator of 70pt superscript", 1207, dSuperscriptSize70ptDenominator);

			CheckHr(m_qvg->GetSuperscriptYOffsetRatio(&dSuperscriptYOffset70ptNumerator, &dSuperscriptYOffset70ptDenominator));
			unitpp::assert_eq("Graphite Numerator of 70pt superscript y offset", 453, dSuperscriptYOffset70ptNumerator);
			unitpp::assert_eq("Graphite Denominator of 70pt superscript y offset", 1207, dSuperscriptYOffset70ptDenominator);

			uint cbOtm = ::GetOutlineTextMetrics(m_hdc, 0, NULL);
			OUTLINETEXTMETRIC * pOtm = reinterpret_cast<OUTLINETEXTMETRIC*>(new BYTE[cbOtm]);
			cbOtm = ::GetOutlineTextMetrics(m_hdc, cbOtm, pOtm);
			unitpp::assert_true("Graphite Size of superscript approximately OTM value",
								within<double>(2, pOtm->otmptSuperscriptSize.y,
											MulDiv(pOtm->otmTextMetrics.tmHeight, dSuperscriptSize70ptNumerator, dSuperscriptSize70ptDenominator)));
			unitpp::assert_true("Graphite Offset of superscript approximately OTM value",
								within<double>(1, pOtm->otmptSuperscriptOffset.y,
											MulDiv(pOtm->otmTextMetrics.tmHeight, dSuperscriptYOffset70ptNumerator, dSuperscriptYOffset70ptDenominator)));
			delete pOtm;
		}
		void testSuperscriptUniscribe()
		{
			unitpp::assert_true("Times New Roman font must be installed",
				m_FOS.IsFontInstalledOnSystem(L"Times New Roman"));

			int dSuperscriptSize9ptNumerator;
			int dSuperscriptSize9ptDenominator;
			int dSuperscriptYOffset9ptNumerator;
			int dSuperscriptYOffset9ptDenominator;
			int dSuperscriptSize70ptNumerator;
			int dSuperscriptSize70ptDenominator;
			int dSuperscriptYOffset70ptNumerator;
			int dSuperscriptYOffset70ptDenominator;

			wcscpy_s(m_chrp.szFaceName, L"Times New Roman");

			m_chrp.dympHeight = 9000;
			CheckHr(m_qvg->SetupGraphics(&m_chrp));
			CheckHr(m_qvg->GetSuperscriptHeightRatio(&dSuperscriptSize9ptNumerator, &dSuperscriptSize9ptDenominator));
			unitpp::assert_true("Uniscribe Size of superscript less than 100% of normal",
				((double)dSuperscriptSize9ptNumerator / dSuperscriptSize9ptDenominator) < 1);
			unitpp::assert_true("Uniscribe Size of superscript greater than 0% of normal",
				((double)dSuperscriptSize9ptNumerator / dSuperscriptSize9ptDenominator) > 0);

			CheckHr(m_qvg->GetSuperscriptYOffsetRatio(&dSuperscriptYOffset9ptNumerator, &dSuperscriptYOffset9ptDenominator));
			unitpp::assert_true("Uniscribe offset of superscript less than 100% of height",
				((double)dSuperscriptYOffset9ptNumerator / dSuperscriptYOffset9ptDenominator) < 1);
			unitpp::assert_true("Uniscribe offset of superscript greater than 0% of height",
				((double)dSuperscriptYOffset9ptNumerator / dSuperscriptYOffset9ptDenominator) > 0);


			m_chrp.dympHeight = 70000;
			CheckHr(m_qvg->SetupGraphics(&m_chrp));
			CheckHr(m_qvg->GetSuperscriptHeightRatio(&dSuperscriptSize70ptNumerator, &dSuperscriptSize70ptDenominator));
			unitpp::assert_eq("Uniscribe Ratio of superscript same for different size fonts",
				((double)dSuperscriptSize9ptNumerator / dSuperscriptSize9ptDenominator),
				((double)dSuperscriptSize70ptNumerator / dSuperscriptSize70ptDenominator));

			unitpp::assert_eq("Uniscribe Numerator of 70pt superscript", 1331, dSuperscriptSize70ptNumerator);
			unitpp::assert_eq("Uniscribe Denominator of 70pt superscript", 2355, dSuperscriptSize70ptDenominator);

			CheckHr(m_qvg->GetSuperscriptYOffsetRatio(&dSuperscriptYOffset70ptNumerator, &dSuperscriptYOffset70ptDenominator));
			unitpp::assert_eq("Uniscribe Numerator of 70pt superscript y offset", 928, dSuperscriptYOffset70ptNumerator);
			unitpp::assert_eq("Uniscribe Denominator of 70pt superscript y offset", 2355, dSuperscriptYOffset70ptDenominator);

			uint cbOtm = ::GetOutlineTextMetrics(m_hdc, 0, NULL);
			OUTLINETEXTMETRIC * pOtm = reinterpret_cast<OUTLINETEXTMETRIC*>(new BYTE[cbOtm]);
			cbOtm = ::GetOutlineTextMetrics(m_hdc, cbOtm, pOtm);
			unitpp::assert_true("Uniscribe Size of superscript approximately OTM value",
								within<double>(2, pOtm->otmptSuperscriptSize.y,
											MulDiv(pOtm->otmTextMetrics.tmHeight, dSuperscriptSize70ptNumerator, dSuperscriptSize70ptDenominator)));
			unitpp::assert_true("Uniscribe Offset of superscript approximately OTM value",
								within<double>(1, pOtm->otmptSuperscriptOffset.y,
											MulDiv(pOtm->otmTextMetrics.tmHeight, dSuperscriptYOffset70ptNumerator, dSuperscriptYOffset70ptDenominator)));
			delete pOtm;
		}
		void testSuperscriptSystem()
		{
			unitpp::assert_true("Marlett font must be installed",
				m_FOS.IsFontInstalledOnSystem(L"Marlett"));

			int dSuperscriptSize9ptNumerator;
			int dSuperscriptSize9ptDenominator;
			int dSuperscriptYOffset9ptNumerator;
			int dSuperscriptYOffset9ptDenominator;
			int dSuperscriptSize70ptNumerator;
			int dSuperscriptSize70ptDenominator;
			int dSuperscriptYOffset70ptNumerator;
			int dSuperscriptYOffset70ptDenominator;

			wcscpy_s(m_chrp.szFaceName, L"Marlett");

			m_chrp.dympHeight = 9000;
			CheckHr(m_qvg->SetupGraphics(&m_chrp));
			CheckHr(m_qvg->GetSuperscriptHeightRatio(&dSuperscriptSize9ptNumerator, &dSuperscriptSize9ptDenominator));
			unitpp::assert_true("System Size of superscript less than 100% of normal",
				((double)dSuperscriptSize9ptNumerator / dSuperscriptSize9ptDenominator) < 1);
			unitpp::assert_true("System Size of superscript greater than 0% of normal",
				((double)dSuperscriptSize9ptNumerator / dSuperscriptSize9ptDenominator) > 0);

			CheckHr(m_qvg->GetSuperscriptYOffsetRatio(&dSuperscriptYOffset9ptNumerator, &dSuperscriptYOffset9ptDenominator));
			unitpp::assert_true("System offset of superscript less than 100% of height",
				((double)dSuperscriptYOffset9ptNumerator / dSuperscriptYOffset9ptDenominator) < 1);
			unitpp::assert_true("System offset of superscript greater than 0% of height",
				((double)dSuperscriptYOffset9ptNumerator / dSuperscriptYOffset9ptDenominator) > 0);


			m_chrp.dympHeight = 70000;
			CheckHr(m_qvg->SetupGraphics(&m_chrp));
			CheckHr(m_qvg->GetSuperscriptHeightRatio(&dSuperscriptSize70ptNumerator, &dSuperscriptSize70ptDenominator));
			unitpp::assert_eq("System Ratio of superscript same for different size fonts",
				((double)dSuperscriptSize9ptNumerator / dSuperscriptSize9ptDenominator),
				((double)dSuperscriptSize70ptNumerator / dSuperscriptSize70ptDenominator));

			unitpp::assert_eq("System Numerator of 70pt superscript", 2, dSuperscriptSize70ptNumerator);
			unitpp::assert_eq("System Denominator of 70pt superscript", 3, dSuperscriptSize70ptDenominator);

			CheckHr(m_qvg->GetSuperscriptYOffsetRatio(&dSuperscriptYOffset70ptNumerator, &dSuperscriptYOffset70ptDenominator));
			unitpp::assert_eq("System Numerator of 70pt superscript y offset", 1, dSuperscriptYOffset70ptNumerator);
			unitpp::assert_eq("System Denominator of 70pt superscript y offset", 3, dSuperscriptYOffset70ptDenominator);
		}


	public:
		TestVwGraphicSuperscript();

		virtual void Setup()
		{
			m_FOS.Initialize();
			m_hdc = 0;
			m_qvgW.CreateInstance(CLSID_VwGraphicsWin32);
			m_hdc = ::CreateDC(TEXT("DISPLAY"),NULL,NULL,NULL);
			m_qvgW->Initialize(m_hdc);
			CheckHr(m_qvgW->QueryInterface(IID_IVwGraphics, (void **) &m_qvg));

			memset(&m_chrp,0,sizeof(m_chrp)); // clear m_chrp;

			wcscpy_s(m_chrp.szFaceName, L"Times New Roman");
			m_chrp.ttvBold = kttvOff;
			m_chrp.ttvItalic = kttvOff;
			m_chrp.ssv = kssvOff; // set no superscript
		}

		virtual void Teardown()
		{
			m_qvgW.Clear();
			m_qvg.Clear();
			::DeleteDC(m_hdc);
		}
	protected:
		IVwGraphicsPtr m_qvg;
		IVwGraphicsWin32Ptr m_qvgW;
		FontsOnSystem m_FOS;

		HDC m_hdc;
		LgCharRenderProps m_chrp;

	};

	class TestVwGraphicSubscript : public unitpp::suite
	{
		void testSubscriptGraphite()
		{
			unitpp::assert_true("SILDoulos PigLatinDemo font must be installed",
				m_FOS.IsFontInstalledOnSystem(L"SILDoulos PigLatinDemo"));

			int dSubscriptSize9ptNumerator;
			int dSubscriptSize9ptDenominator;
			int dSubscriptYOffset9ptNumerator;
			int dSubscriptYOffset9ptDenominator;
			int dSubscriptSize70ptNumerator;
			int dSubscriptSize70ptDenominator;
			int dSubscriptYOffset70ptNumerator;
			int dSubscriptYOffset70ptDenominator;

			wcscpy_s(m_chrp.szFaceName, L"SILDoulos PigLatinDemo");

			m_chrp.dympHeight = 9000;
			CheckHr(m_qvg->SetupGraphics(&m_chrp));
			CheckHr(m_qvg->GetSubscriptHeightRatio(&dSubscriptSize9ptNumerator, &dSubscriptSize9ptDenominator));
			unitpp::assert_true("Graphite Size of subscript less than 100% of normal",
				((double)dSubscriptSize9ptNumerator / dSubscriptSize9ptDenominator) < 1);
			unitpp::assert_true("Graphite Size of subscript greater than 0% of normal",
				((double)dSubscriptSize9ptNumerator / dSubscriptSize9ptDenominator) > 0);

			CheckHr(m_qvg->GetSubscriptYOffsetRatio(&dSubscriptYOffset9ptNumerator, &dSubscriptYOffset9ptDenominator));
			unitpp::assert_true("Graphite offset of subscript less than 100% of height",
				((double)dSubscriptYOffset9ptNumerator / dSubscriptYOffset9ptDenominator) < 1);
			unitpp::assert_true("Graphite offset of subscript greater than 0% of height",
				((double)dSubscriptYOffset9ptNumerator / dSubscriptYOffset9ptDenominator) > 0);


			m_chrp.dympHeight = 70000;
			CheckHr(m_qvg->SetupGraphics(&m_chrp));
			CheckHr(m_qvg->GetSubscriptHeightRatio(&dSubscriptSize70ptNumerator, &dSubscriptSize70ptDenominator));
			unitpp::assert_eq("Graphite Ratio of subscript same for different size fonts",
				((double)dSubscriptSize9ptNumerator / dSubscriptSize9ptDenominator),
				((double)dSubscriptSize70ptNumerator / dSubscriptSize70ptDenominator));

			unitpp::assert_eq("Graphite Numerator of 70pt subscript", 650, dSubscriptSize70ptNumerator);
			unitpp::assert_eq("Graphite Denominator of 70pt subscript", 1207, dSubscriptSize70ptDenominator);

			CheckHr(m_qvg->GetSubscriptYOffsetRatio(&dSubscriptYOffset70ptNumerator, &dSubscriptYOffset70ptDenominator));
			unitpp::assert_eq("Graphite Numerator of 70pt subscript y offset", 143, dSubscriptYOffset70ptNumerator);
			unitpp::assert_eq("Graphite Denominator of 70pt subscript y offset", 1207, dSubscriptYOffset70ptDenominator);

		}
		void testSubscriptUniscribe()
		{
			unitpp::assert_true("Times New Roman font must be installed",
				m_FOS.IsFontInstalledOnSystem(L"Times New Roman"));

			int dSubscriptSize9ptNumerator;
			int dSubscriptSize9ptDenominator;
			int dSubscriptYOffset9ptNumerator;
			int dSubscriptYOffset9ptDenominator;
			int dSubscriptSize70ptNumerator;
			int dSubscriptSize70ptDenominator;
			int dSubscriptYOffset70ptNumerator;
			int dSubscriptYOffset70ptDenominator;

			wcscpy_s(m_chrp.szFaceName, L"Times New Roman");

			m_chrp.dympHeight = 9000;
			CheckHr(m_qvg->SetupGraphics(&m_chrp));
			CheckHr(m_qvg->GetSubscriptHeightRatio(&dSubscriptSize9ptNumerator, &dSubscriptSize9ptDenominator));
			unitpp::assert_true("Uniscribe Size of subscript less than 100% of normal",
				((double)dSubscriptSize9ptNumerator / dSubscriptSize9ptDenominator) < 1);
			unitpp::assert_true("Uniscribe Size of subscript greater than 0% of normal",
				((double)dSubscriptSize9ptNumerator / dSubscriptSize9ptDenominator) > 0);

			CheckHr(m_qvg->GetSubscriptYOffsetRatio(&dSubscriptYOffset9ptNumerator, &dSubscriptYOffset9ptDenominator));
			unitpp::assert_true("Uniscribe offset of subscript less than 100% of height",
				((double)dSubscriptYOffset9ptNumerator / dSubscriptYOffset9ptDenominator) < 1);
			unitpp::assert_true("Uniscribe offset of subscript greater than 0% of height",
				((double)dSubscriptYOffset9ptNumerator / dSubscriptYOffset9ptDenominator) > 0);


			m_chrp.dympHeight = 70000;
			CheckHr(m_qvg->SetupGraphics(&m_chrp));
			CheckHr(m_qvg->GetSubscriptHeightRatio(&dSubscriptSize70ptNumerator, &dSubscriptSize70ptDenominator));
			unitpp::assert_eq("Uniscribe Ratio of subscript same for different size fonts",
				((double)dSubscriptSize9ptNumerator / dSubscriptSize9ptDenominator),
				((double)dSubscriptSize70ptNumerator / dSubscriptSize70ptDenominator));

			unitpp::assert_eq("Uniscribe Numerator of 70pt subscript", 1331, dSubscriptSize70ptNumerator);
			unitpp::assert_eq("Uniscribe Denominator of 70pt subscript", 2355, dSubscriptSize70ptDenominator);

			CheckHr(m_qvg->GetSubscriptYOffsetRatio(&dSubscriptYOffset70ptNumerator, &dSubscriptYOffset70ptDenominator));
			unitpp::assert_eq("Uniscribe Numerator of 70pt subscript y offset", 293, dSubscriptYOffset70ptNumerator);
			unitpp::assert_eq("Uniscribe Denominator of 70pt subscript y offset", 2355, dSubscriptYOffset70ptDenominator);
		}
		void testSubscriptSystem()
		{
			unitpp::assert_true("Marlett font must be installed",
				m_FOS.IsFontInstalledOnSystem(L"Marlett"));

			int dSubscriptSize9ptNumerator;
			int dSubscriptSize9ptDenominator;
			int dSubscriptYOffset9ptNumerator;
			int dSubscriptYOffset9ptDenominator;
			int dSubscriptSize70ptNumerator;
			int dSubscriptSize70ptDenominator;
			int dSubscriptYOffset70ptNumerator;
			int dSubscriptYOffset70ptDenominator;

			wcscpy_s(m_chrp.szFaceName, L"Marlett");

			m_chrp.dympHeight = 9000;
			CheckHr(m_qvg->SetupGraphics(&m_chrp));
			CheckHr(m_qvg->GetSubscriptHeightRatio(&dSubscriptSize9ptNumerator, &dSubscriptSize9ptDenominator));
			unitpp::assert_true("System Size of subscript less than 100% of normal",
				((double)dSubscriptSize9ptNumerator / dSubscriptSize9ptDenominator) < 1);
			unitpp::assert_true("System Size of subscript greater than 0% of normal",
				((double)dSubscriptSize9ptNumerator / dSubscriptSize9ptDenominator) > 0);

			CheckHr(m_qvg->GetSubscriptYOffsetRatio(&dSubscriptYOffset9ptNumerator, &dSubscriptYOffset9ptDenominator));
			unitpp::assert_true("System offset of subscript less than 100% of height",
				((double)dSubscriptYOffset9ptNumerator / dSubscriptYOffset9ptDenominator) < 1);
			unitpp::assert_true("System offset of subscript greater than 0% of height",
				((double)dSubscriptYOffset9ptNumerator / dSubscriptYOffset9ptDenominator) > 0);


			m_chrp.dympHeight = 70000;
			CheckHr(m_qvg->SetupGraphics(&m_chrp));
			CheckHr(m_qvg->GetSubscriptHeightRatio(&dSubscriptSize70ptNumerator, &dSubscriptSize70ptDenominator));
			unitpp::assert_eq("System Ratio of subscript same for different size fonts",
				((double)dSubscriptSize9ptNumerator / dSubscriptSize9ptDenominator),
				((double)dSubscriptSize70ptNumerator / dSubscriptSize70ptDenominator));

			unitpp::assert_eq("System Numerator of 70pt subscript", 2, dSubscriptSize70ptNumerator);
			unitpp::assert_eq("System Denominator of 70pt subscript", 3, dSubscriptSize70ptDenominator);

			CheckHr(m_qvg->GetSubscriptYOffsetRatio(&dSubscriptYOffset70ptNumerator, &dSubscriptYOffset70ptDenominator));
			unitpp::assert_eq("System Numerator of 70pt subscript y offset", 2, dSubscriptYOffset70ptNumerator);
			unitpp::assert_eq("System Denominator of 70pt subscript y offset", 15, dSubscriptYOffset70ptDenominator);
		}


	public:
		TestVwGraphicSubscript();

		virtual void Setup()
		{
			m_FOS.Initialize();

			m_hdc = 0;
			m_qvgW.CreateInstance(CLSID_VwGraphicsWin32);
			m_hdc = ::CreateDC(TEXT("DISPLAY"),NULL,NULL,NULL);
			m_qvgW->Initialize(m_hdc);
			CheckHr(m_qvgW->QueryInterface(IID_IVwGraphics, (void **) &m_qvg));

			memset(&m_chrp,0,sizeof(m_chrp)); // clear m_chrp;

			wcscpy_s(m_chrp.szFaceName, L"Times New Roman");
			m_chrp.ttvBold = kttvOff;
			m_chrp.ttvItalic = kttvOff;
			m_chrp.ssv = kssvOff; // set no subscript
		}

		virtual void Teardown()
		{
			m_qvgW.Clear();
			m_qvg.Clear();
			::DeleteDC(m_hdc);
		}
	protected:
		IVwGraphicsPtr m_qvg;
		IVwGraphicsWin32Ptr m_qvgW;
		FontsOnSystem m_FOS;

		HDC m_hdc;
		LgCharRenderProps m_chrp;

	};

}

#endif /*TestVwGraphics_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
