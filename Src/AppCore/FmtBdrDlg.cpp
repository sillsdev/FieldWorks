/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FmtBdrDlg.cpp
Responsibility: John Landon
Last reviewed: Not yet.

Description:
	Implementation of the Paragraph and Table Border Dialog classes.

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma hdrstop
#include "main.h"

#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Static arrays: initialisation
***********************************************************************************************/
#define TRACE0(msg) _CrtDbgReport(_CRT_WARN, NULL, 0, NULL, msg)
#define TRACE1(msg,arg1) _CrtDbgReport(_CRT_WARN, NULL, 0, NULL, msg, arg1)

const int FmtBdrDlg::krgmpWidths[kcWidths] =
	{ 0, 250, 500, 750, 1000, 1500, 2250, 3000, 4500, 6000 };	// Units are 0.001 points.
// The string "(unspecified)" must be localizable:
static const int knUnspecLen = 20;
static achar rgchUnspec[knUnspecLen];
const achar * FmtBdrDlg::krgpszWidths[kcWidths] = {rgchUnspec,
	_T("\xbc"), _T("\xbd"), _T("\xbe"), _T("1"), _T("1\xbd"), _T("2\xbc"), _T("3"), _T("4\xbd"),
	_T("6")
};
const int FmtBdrDlg::krgridBitmapIds[kchbmp] =
	{
		kridFmtBdrDlgNoneP, kridFmtBdrDlgNonePSel,
		kridFmtBdrDlgAll,   kridFmtBdrDlgAllSel,
		kridFmtBdrDlgBox,   kridFmtBdrDlgBoxSel,
		kridFmtBdrDlgGrid,  kridFmtBdrDlgGridSel,
		kridFmtBdrDlgNoneT, kridFmtBdrDlgNoneTSel
	};

const int kimpDefaultWidth = 2;	// 1/2 pt

// Set a check box
#define SCB(side) ::CheckDlgButton (m_hwnd, kctidFmtBdrDlg##side, \
		(m_grfBorders & kf##side) ? BST_CHECKED : BST_UNCHECKED)

/***********************************************************************************************
	Methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
FmtBdrDlg::FmtBdrDlg(int rid)
{
	m_rid = rid;
	BasicInit();
	m_fCanInherit = true;
	m_pszHelpUrl = _T("User_Interface/Menus/Format/Border.htm");
	m_pafsd = NULL;
}

FmtBdrDlg::FmtBdrDlg(AfStylesDlg * pafsd, int rid)
{
	m_rid = rid;
	BasicInit();
	m_pafsd = pafsd;
	if (pafsd)
	{
		SetCanDoRtl(pafsd->CanDoRtl());
		SetOuterRtl(pafsd->OuterRtl());
		m_pszHelpUrl = _T("User_Interface/Menus/Format/Style/Style_Border_tab.htm");
	}
	else
	{
		m_pszHelpUrl = _T("User_Interface/Menus/Format/Border.htm");
	}
	m_fCanInherit = true;
}

void FmtBdrDlg::BasicInit()
{
	m_clrBorder = kclrBlack;
	m_grfBorders = 0;
	m_fSwitchSides = false;
	m_impWidth = kimpDefaultWidth;
	for (int ibmp = 0; ibmp < kchbmp; ++ibmp)
		m_rghbmp[ibmp] = NULL;
	m_fCanInherit = false;
	m_xColor = kxExplicit;
	m_xBorders = kxExplicit;
	StrApp str(kstidUnspec);
	_tcsncpy_s(rgchUnspec, str.Chars(), knUnspecLen);
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
FmtBdrDlg::~FmtBdrDlg()
{
	for (int ibmp = 0; ibmp < kchbmp; ++ibmp)
	{
		if (m_rghbmp[ibmp])
		{
			AfGdi::DeleteObjectBitmap(m_rghbmp[ibmp]);
			m_rghbmp[ibmp] = NULL;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Set the flags indicating what version of the dialog we want.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::SetCanDoRtl(bool fCanDoRtl)
{
	m_fCanDoRtl = fCanDoRtl;
}

/*----------------------------------------------------------------------------------------------
	Handle the messages that cause the controls to be recolored based on their state.
----------------------------------------------------------------------------------------------*/
bool FmtBdrDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_CTLCOLOREDIT || wm == WM_CTLCOLORBTN)
	{
		return ColorForInheritance(wp, lp, lnRet);
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Set the color of the control based on whether the value is inherited.
----------------------------------------------------------------------------------------------*/
bool FmtBdrDlg::ColorForInheritance(WPARAM wp, LPARAM lp, long & lnRet)
{
	HWND hwndArg = (HWND)lp;

	if (hwndArg == ::GetDlgItem(m_hwnd, kctidColor) && m_qccmb)
	{
		if (m_xColor == kxExplicit)
			m_qccmb->SetLabelColor(::GetSysColor(COLOR_WINDOWTEXT));
		else
			m_qccmb->SetLabelColor(kclrGray50);
		// I don't know why we return false here, but when we returned true, the control
		// flashed bizarrely. - SharonC
		return false;
	}
	else
		return false;

}
/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool FmtBdrDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	InitWidthCombo();

	HMODULE hmod = ModuleEntry::GetModuleHandle();

	// Load all bitmaps. Note that strictly only four are needed for paragraph border.
	int i;
	for (i = 0; i < kchbmp; ++i)
	{
		m_rghbmp[i] = AfGdi::LoadImageBitmap(hmod, (LPCTSTR)krgridBitmapIds[i],
			IMAGE_BITMAP, 0, 0, 0);
		if (!m_rghbmp[i])
			break;
	}
	if (i != kchbmp)
	{
		// Error loading a bitmap: delete ones already loaded.
		for (; --i >= 0; )
		{
			if (m_rghbmp[i])
			{
				AfGdi::DeleteObjectBitmap(m_rghbmp[i]);
				m_rghbmp[i] = NULL;
			}
		}
		return false;
	}

	// Create color combo box.
	m_qccmb.Create();
	m_qccmb->SubclassButton(::GetDlgItem(m_hwnd, kctidColor), &m_clrMod);

	// Adjust the Left and Right check-boxes, if needed.
	UpdateLabels(m_nRtl);

	FillCtls();

	// Turn off visual styles for these controls until all the controls on the dialog
	// can handle them properly (e.g. custom controls).
	HMODULE hmodtheme = ::LoadLibrary(L"uxtheme.dll");
	if (hmodtheme != NULL)
	{
		typedef bool (__stdcall *themeProc)();
		typedef void (__stdcall *SetWindowThemeProc)(HWND, LPTSTR, LPTSTR);
		themeProc pfnb = (themeProc)::GetProcAddress(hmodtheme, "IsAppThemed");
		bool fAppthemed = (pfnb != NULL ? (pfnb)() : false);
		pfnb = (themeProc)::GetProcAddress(hmodtheme, "IsThemeActive");
		bool fThemeActive = (pfnb != NULL ? (pfnb)() : false);
		SetWindowThemeProc pfn = (SetWindowThemeProc)::GetProcAddress(hmodtheme, "SetWindowTheme");

		if (fAppthemed && fThemeActive && pfn != NULL)
		{
			(pfn)(m_hwnd, L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFmtBdrDlgWidth), L"", L"");
		}

		::FreeLibrary(hmodtheme);
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Initialise the Width owner-draw list combo box.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::InitWidthCombo()
{
	achar rgch[20];						// Temp array.
	achar rgchPt[6];
	// Get the " pt" string.
	HMODULE hmod = ModuleEntry::GetModuleHandle();
	LoadString(hmod, kstidFmtBdrDlgPt, rgchPt, kcchPt);
	HWND hwndWidth = ::GetDlgItem(m_hwnd, kctidFmtBdrDlgWidth);
	// Load up each string to the combo box (1/4 pt, 1/2 pt, etc.).
	for (int nIndex = 0; nIndex < kcWidths; ++nIndex)
	{
		if (!m_fCanInherit && nIndex == 0)
			continue;

		_tcscpy_s(rgch, krgpszWidths[nIndex]);
		if (nIndex > 0) // don't add "pt" after (unspecified)
			_tcscat_s(rgch, rgchPt);
		::SendMessage(hwndWidth, CB_ADDSTRING, 0, (LPARAM)rgch);
	}
}

/*----------------------------------------------------------------------------------------------
	Fill the dialog in with its values, and update the controls.
	Assumes that inherited values are set before explicit ones.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::SetDialogValues(COLORREF clrBorder, int grfBorders, int mpWidth, int nRtl,
	bool fExplicit)
{
	UpdateLabels(nRtl);

	if (fExplicit)
		m_clrBorder = clrBorder;
	else
		m_clrBorderI = clrBorder;

	int grfBordersTmp = grfBorders & kfGrid;	// All except low 6 bits are ignored.
	int impWidthTmp;

	// Set width to index corresponding to value passed if this is one of the ones we use.
	// Else set to the inherited value or the index for default width (1/2 point).
	impWidthTmp = (fExplicit && m_fCanInherit) ? m_impWidthI : kimpDefaultWidth;
	for (int i = 1; i < kcWidths; ++i)
	{
		if (mpWidth == krgmpWidths[i])
		{
			impWidthTmp = i;
			break;
		}
	}
	if (mpWidth == FwStyledText::knConflicting)
		impWidthTmp = -1;
	else if (mpWidth == FwStyledText::knUnspecified)
		impWidthTmp = 0;

	m_nRtl = nRtl;

	if (m_fSwitchSides)
	{
		// Switch leading and trailing.
		int fL = grfBordersTmp & kfLeading;
		int fT = grfBordersTmp & kfTrailing;
		grfBordersTmp = ((grfBordersTmp & ~kfLeading) & ~kfTrailing);
		if (fL != 0)
			grfBordersTmp |= kfTrailing;
		if (fT != 0)
			grfBordersTmp |= kfLeading;
	}

	if (fExplicit)
	{
		m_grfBorders = grfBordersTmp;
		m_impWidth = impWidthTmp;
	}
	else
	{
		m_grfBordersI = grfBordersTmp;
		m_impWidthI = impWidthTmp;
	}
}

/*----------------------------------------------------------------------------------------------
	Fill the controls with their values.
	For the style dialog, these may contain either explicit or inherited values.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::FillCtls()
{
	m_clrMod = ClrBorder();
	m_qccmb->SetColor(m_clrMod);

	int impWidth = ImpWidth();
	if (!m_fCanInherit)
		impWidth--; // to account for unspecified which isn't there
	::SendMessage(::GetDlgItem(m_hwnd, kctidFmtBdrDlgWidth), CB_SETCURSEL, (WPARAM)impWidth, 0);

	SetImages();
	SetCheckBoxes();
}

/*----------------------------------------------------------------------------------------------
	Adjust the labels on the check boxes as needed, based on the directionality we are
	working with.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::UpdateLabels(int nRtl)
{
	HWND hwndLeading = ::GetDlgItem(m_hwnd, kctidFmtBdrDlgLeading);
	HWND hwndTrailing = ::GetDlgItem(m_hwnd, kctidFmtBdrDlgTrailing);
	StrAppBuf strb;
	if (nRtl == 1 || nRtl == 0)
	{
		// Standard labels.
		strb.Load(kstidFmtBdrLabelLeft);
		::SendMessage(hwndLeading, WM_SETTEXT, 0, (LPARAM)strb.Chars());
		strb.Load(kstidFmtBdrLabelRight);
		::SendMessage(hwndTrailing, WM_SETTEXT, 0, (LPARAM)strb.Chars());

		m_fSwitchSides = (nRtl == 1);
	}
	else
	{
		// Change labels to Leading and Trailing.
		if (m_fOuterRtl)
		{
			m_fSwitchSides = true;
			strb.Load(kstidFmtBdrLabelTrailing);
			::SendMessage(hwndLeading, WM_SETTEXT, 0, (LPARAM)strb.Chars());
			strb.Load(kstidFmtBdrLabelLeading);
			::SendMessage(hwndTrailing, WM_SETTEXT, 0, (LPARAM)strb.Chars());
		}
		else
		{
			m_fSwitchSides = false;
			strb.Load(kstidFmtBdrLabelLeading);
			::SendMessage(hwndLeading, WM_SETTEXT, 0, (LPARAM)strb.Chars());
			strb.Load(kstidFmtBdrLabelTrailing);
			::SendMessage(hwndTrailing, WM_SETTEXT, 0, (LPARAM)strb.Chars());
		}
	}
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::GetDialogValues(COLORREF * pclrBorder, int * pgrfBorders, int * pmpWidth)
{
	AssertPtr(pclrBorder);
	AssertPtr(pgrfBorders);
	AssertPtr(pmpWidth);

	if (m_xColor == kxExplicit)
		*pclrBorder = m_clrBorder;
	else
		*pclrBorder = (COLORREF)FwStyledText::knUnspecified;

	if (m_xBorders == kxExplicit)
	{
		*pgrfBorders = m_grfBorders;
		*pmpWidth = krgmpWidths[m_impWidth];
		if (m_fSwitchSides)
		{
			// Switch leading and trailing.
			int fL = m_grfBorders & kfLeading;
			int fT = m_grfBorders & kfTrailing;
			*pgrfBorders = ((*pgrfBorders & ~kfLeading) & ~kfTrailing);
			if (fL != 0)
				*pgrfBorders |= kfTrailing;
			if (fT != 0)
				*pgrfBorders |= kfLeading;
		}
	}
	else
	{
		*pgrfBorders = FwStyledText::knUnspecified;
		*pmpWidth = FwStyledText::knUnspecified;
	}

	return;
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool FmtBdrDlg::OnMeasureChildItem(MEASUREITEMSTRUCT * pmis)
{
	AssertPtr(pmis);

	Rect rc;
	switch (pmis->CtlID)
	{
	case kctidFmtBdrDlgDiag:
		::GetWindowRect(::GetDlgItem(m_hwnd, kctidFmtBdrDlgDiag), &rc);
		pmis->itemWidth = rc.Width(); // Leave width as designed.
		pmis->itemHeight = rc.Height(); // Leave height as designed.
		return true;
	case kctidFmtBdrDlgWidth:
		::GetWindowRect(::GetDlgItem(m_hwnd, kctidFmtBdrDlgWidth), &rc);
		pmis->itemWidth = rc.Width(); // Leave width as designed.
		pmis->itemHeight = kdzpListItem;
		return true;
	default:
		return AfWnd::OnMeasureChildItem(pmis);
	}
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool FmtBdrDlg::OnDrawChildItem(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);

	switch (pdis->CtlID)
	{
	case kctidFmtBdrDlgDiag:
		DrawDiagram(pdis->hDC, pdis->hwndItem, pdis->rcItem);
		return true;
	case kctidFmtBdrDlgWidth:
		DrawBorderWidth(pdis);
		return true;

	case kctidFmtBdrDlgNoneP:
	case kctidFmtBdrDlgAll:
		/* REVIEW JohnT/JohnW(SteveMc):
			The code here, and in OnNotifyChild() below more or less works in conjuction with
			the matched LTEXT and CONTROL definitions in FmtBdrDlg.rc.  It would be nicer if we
			defined a button class (AfImageButton?) which could use a single CONTROL definition
			in the .rc file.  This would (probably?) allow us to correctly handle the hot key,
			and also using space bar to toggle the state between All and None when the focus is
			on All or None.  Is this sufficiently unclear?
		*/
		{
			int ibmp;
			if (pdis->CtlID == kctidFmtBdrDlgNoneP)
				ibmp = (m_xBorders != kxExplicit || (m_grfBorders & kfBox)) ? NONE : NONE_S;
			else
				ibmp = (m_xBorders != kxExplicit && ((m_grfBorders & kfBox) == kfBox)) ? ALL_S : ALL;
			AfGfx::FillSolidRect(pdis->hDC, pdis->rcItem, ::GetSysColor(COLOR_WINDOW));
			HDC hdcMem = AfGdi::CreateCompatibleDC(pdis->hDC);
			HBITMAP hbmOld = AfGdi::SelectObjectBitmap(hdcMem, m_rghbmp[ibmp]);

			BITMAP bm;
			::GetObject(m_rghbmp[ibmp], sizeof (bm), &bm);
			::BitBlt(pdis->hDC, pdis->rcItem.left, pdis->rcItem.top, bm.bmWidth, bm.bmHeight,
				hdcMem, 0, 0, SRCCOPY);

			AfGdi::SelectObjectBitmap(hdcMem, hbmOld, AfGdi::OLD);
			BOOL fSuccess;
			fSuccess = AfGdi::DeleteDC(hdcMem);
			Assert(fSuccess);

			int nState = ::SendMessage(pdis->hwndItem, BM_GETSTATE, 0, 0);
			if (nState & BST_FOCUS)
			{
				RECT rcFocus;
				rcFocus.top = pdis->rcItem.top + 1;
				rcFocus.left = pdis->rcItem.left + 1;
				rcFocus.bottom = pdis->rcItem.bottom - 1;
				rcFocus.right = pdis->rcItem.right - 1;
				HPEN hpen = ::CreatePen(PS_DOT, 0, ::GetSysColor(COLOR_WINDOW));
				HPEN hpenOld = (HPEN)::SelectObject(pdis->hDC, hpen);
				::DrawEdge(pdis->hDC, &rcFocus, EDGE_ETCHED, BF_RECT);
				::SelectObject(pdis->hDC, hpenOld);
				::DeleteObject(hpen);
			}
		}
		return true;

	default:
		return AfWnd::OnDrawChildItem(pdis);
	}
}

/*----------------------------------------------------------------------------------------------
	Draw an item indicating a border width.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::DrawBorderWidth(DRAWITEMSTRUCT * pdis)
{
	SmartPalette spal(pdis->hDC);
	Rect rc;
	Rect rcLine;
	int nIndex = pdis->itemID;
	int dxpListItem;
	int dypListItem;
	int dzpPenWidth;
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidFmtBdrDlgDiag), &rc);
	dxpListItem = pdis->rcItem.right - pdis->rcItem.left;
	dypListItem = pdis->rcItem.bottom - pdis->rcItem.top;
	achar rgch[MAX_PATH];
	Vector<achar> vch;
	achar * pszT;
	int ypLineTop; // Top of filled rectangle which represents the border line.

	HPEN hpen = NULL;
	HPEN hpenOld;
	hpenOld = NULL;
	Rect rcT(pdis->rcItem);
	::SetBkMode(pdis->hDC, TRANSPARENT);
	COLORREF clrLine = ClrBorder();
	if (pdis->itemState & ODS_SELECTED)
	{
		AfGfx::FillSolidRect(pdis->hDC, rcT, ::GetSysColor(COLOR_HIGHLIGHT));
		AfGfx::SetTextColor(pdis->hDC, ::GetSysColor(COLOR_HIGHLIGHTTEXT));
		hpen = ::CreatePen(PS_SOLID, 0, ::GetSysColor(COLOR_WINDOWTEXT));
		hpenOld = (HPEN)::SelectObject(pdis->hDC, hpen);
		clrLine = ::GetSysColor(COLOR_HIGHLIGHTTEXT);
	}
	else
	{
		AfGfx::FillSolidRect(pdis->hDC, rcT, ::GetSysColor(COLOR_WINDOW));
		if ((pdis->itemState & ODS_COMBOBOXEDIT) && m_xBorders != kxExplicit)
		{
			AfGfx::SetTextColor(pdis->hDC, kclrGray50);
			// Don't really have to do this, because the lines are drawn with the selected
			// color, not gray.
//			HPEN hpenGray = ::CreatePen(PS_SOLID, 0, clrGray);
//			fGrayPen = true;
//			hpenOld = (HPEN)::SelectObject(pdis->hDC, hpenGray);
		}
		else
		{
			AfGfx::SetTextColor(pdis->hDC, ::GetSysColor(COLOR_WINDOWTEXT));
			hpen = ::CreatePen(PS_SOLID, 0, ::GetSysColor(COLOR_WINDOWTEXT));
			hpenOld = (HPEN)::SelectObject(pdis->hDC, hpen);
		}
	}

	if (pdis->itemState & ODS_COMBOBOXEDIT)
	{
		// Draw the selection (edit) box with the value currently stored.
		// Make width of pen the same as when drawing the preview diagram.
		int imp = ImpWidth();
		if (imp > 0)
		{
			dzpPenWidth = krgmpWidths[imp] * rc.Width() / knPenFactor + 1;
			ypLineTop = dypListItem * 5 / 8 - dzpPenWidth / 2;
			rcLine.Set(dxpListItem * 7 / 15, ypLineTop, dxpListItem * 15 / 16,
				ypLineTop + dzpPenWidth);
			AfGfx::FillSolidRect(pdis->hDC, rcLine, clrLine);
		}

		int icb = (m_fCanInherit) ? imp : imp - 1; // to account for "(unspecified)"

		int cch = 0;
		if (icb > -1)
		{
			cch = ::SendMessage(pdis->hwndItem, CB_GETLBTEXTLEN, icb, (LPARAM)0);
			if (cch < MAX_PATH)
			{
				pszT = rgch;
			}
			else
			{
				vch.Resize(cch + 1);
				pszT = vch.Begin();
			}
			cch = ::SendMessage(pdis->hwndItem, CB_GETLBTEXT, icb, (LPARAM)pszT);
			if (cch < 0)
			{
				pszT = _T("");
				cch = 0;
			}
		}
		else
		{
			pszT = _T("");
		}
		::TextOut(pdis->hDC, dxpListItem / 16, dypListItem / 3, pszT, cch);
	}
	else if (nIndex >= 0)
	{
		// nIndex is index into list.
		int imp = (m_fCanInherit) ? nIndex : nIndex + 1; // "skip (unspecified)"
		if (imp > 0)
		{
			dzpPenWidth = krgmpWidths[imp] * rc.Width() / knPenFactor + 1;
			ypLineTop = dypListItem / 2 + dypListItem * nIndex - dzpPenWidth / 2;
			rcLine.Set(dxpListItem * 3 / 8, ypLineTop, dxpListItem * 7 / 8,
				ypLineTop + dzpPenWidth);
			AfGfx::FillSolidRect(pdis->hDC, rcLine, m_clrBorder);
		}
		int cch = ::SendMessage(pdis->hwndItem, CB_GETLBTEXTLEN, nIndex, (LPARAM)0);
		if (cch < MAX_PATH)
		{
			pszT = rgch;
		}
		else
		{
			vch.Resize(cch + 1);
			pszT = vch.Begin();
		}
		cch = ::SendMessage(pdis->hwndItem, CB_GETLBTEXT, nIndex, (LPARAM)pszT);
		if (cch < 0)
		{
			pszT = _T("");
			cch = 0;
		}
		::TextOut(pdis->hDC, dxpListItem / 16, dypListItem / 6 + dypListItem * nIndex,
			pszT, cch);
		// Draw focus rectangle if required.
		if (pdis->itemState & ODS_SELECTED)
			::DrawFocusRect(pdis->hDC, &pdis->rcItem);
	}
	// Draw focus rectangle if required. OPTIMIZE: is this needed anymore?
	if (pdis->itemState & ODS_FOCUS)
		DrawFocusRect(pdis->hDC, &pdis->rcItem);

	if (hpenOld)
		::SelectObject(pdis->hDC, hpenOld);
	if (hpen)
		::DeleteObject(hpen);
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
bool FmtBdrDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (pnmh->code == BN_SETFOCUS)
	{
		switch (ctidFrom)
		{
		case kctidFmtBdrDlgNoneP:
			{
				StrAppBuf strb;
				achar chAmp = '&';
				int cch = ::GetDlgItemText(m_hwnd, kctidFmtBdrDlgNoneP,
					const_cast<achar *>(strb.Chars()), strb.kcchMaxStr);
				strb.SetLength(cch);
				int ich = strb.FindCh(chAmp) + 1;
				if (cch && ich && ich < cch)
				{
					achar chHot = strb.GetAt(ich);
					int vkey;
					if (chHot >= 'A' && chHot <= 'Z')
					{
						vkey = chHot;
					}
					else if (chHot >= 'a' && chHot <= 'z')
					{
						vkey = toupper(chHot);
					}
					else
					{
						// TODO ???(SteveMc): Convert this character to the corresponding
						// virtual key.
						vkey = '?';			// I HAVE NO IDEA WHAT TO DO!?
					}
					SHORT nKeyState = ::GetAsyncKeyState(vkey);
					if (nKeyState & 0x8000 && nKeyState & 0x1)
					{
						::SendMessage(::GetDlgItem(m_hwnd, kctidFmtBdrDlgNoneP), BM_CLICK, 0,
							0);
					}
				}
			}
			break;
		case kctidFmtBdrDlgAll:
			{
				StrAppBuf strb;
				achar chAmp = '&';
				int cch = ::GetDlgItemText(m_hwnd, kctidFmtBdrDlgAll,
					const_cast<achar *>(strb.Chars()), strb.kcchMaxStr);
				strb.SetLength(cch);
				int ich = strb.FindCh(chAmp) + 1;
				if (cch && ich && ich < cch)
				{
					achar chHot = strb.GetAt(ich);
					int vkey;
					if (chHot >= 'A' && chHot <= 'Z')
					{
						vkey = chHot;
					}
					else if (chHot >= 'a' && chHot <= 'z')
					{
						vkey = toupper(chHot);
					}
					else
					{
						// TODO ???(SteveMc): Convert this character to the corresponding
						// virtual key.
						vkey = '?';			// I HAVE NO IDEA WHAT TO DO!?
					}
					SHORT nKeyState = ::GetAsyncKeyState(vkey);
					if (nKeyState & 0x8000 && nKeyState & 0x1)
					{
						::SendMessage(::GetDlgItem(m_hwnd, kctidFmtBdrDlgAll), BM_CLICK, 0, 0);
					}
				}
			}
			break;
		default:
			return false;		// Button ID not recognized.
		}
		return true;			// Setting focus to button handled.
	}
	if (pnmh->code == BN_KILLFOCUS)
	{
		switch (ctidFrom)
		{
		case kctidFmtBdrDlgNoneP:
		case kctidFmtBdrDlgAll:
			SetImages();
			return false;
		default:
			return false;		// Button ID not recognized.
		}
	}
	if (pnmh->code == BN_CLICKED)
	{
		switch (ctidFrom)
		{
		case kctidFmtBdrDlgNoneP:
		case kctidFmtBdrDlgNoneT:
			if (m_grfBorders & kfGrid || m_xBorders != kxExplicit)
			{
				// At least one border exists, so remove it.
				m_grfBorders = 0;
				SetExplicitWidth();
				m_xBorders = kxExplicit;
				SetCheckBoxes();
				SetImages();
				if (m_impWidth == 0)
					m_impWidth = (m_fCanInherit) ? m_impWidthI : kimpDefaultWidth;
				UpdateWidthCombo();
			}
			break;
		case kctidFmtBdrDlgAll:
		case kctidFmtBdrDlgBox:
			if ((m_grfBorders & kfGrid) != kfBox || m_xBorders != kxExplicit)
			{
				m_grfBorders = kfBox;
				SetExplicitWidth();
				m_xBorders = kxExplicit;
				SetCheckBoxes();
				SetImages();
				if (m_impWidth == 0)
					m_impWidth = (m_fCanInherit) ? m_impWidthI : kimpDefaultWidth;
				UpdateWidthCombo();
			}
			break;
		case kctidFmtBdrDlgGrid:
			if ((m_grfBorders & kfGrid) != kfGrid || m_xBorders != kxExplicit)
			{
				m_grfBorders = kfGrid;
				SetExplicitWidth();
				m_xBorders = kxExplicit;
				SetCheckBoxes();
				SetImages();
				if (m_impWidth == 0)
					m_impWidth = (m_fCanInherit) ? m_impWidthI : kimpDefaultWidth;
				UpdateWidthCombo();
			}
			break;

		case kctidFmtBdrDlgTop:
		case kctidFmtBdrDlgBottom:
		case kctidFmtBdrDlgLeading:
		case kctidFmtBdrDlgTrailing:
			CheckBoxChanged(ctidFrom);
			break;

		case kctidFmtBdrDlgCols:
			if (IsDlgButtonChecked(m_hwnd, kctidFmtBdrDlgCols) == BST_CHECKED)
				m_grfBorders |= kfCols;
			else
				m_grfBorders &= ~kfCols;
			SetImages();
			break;
		case kctidFmtBdrDlgRows:
			if (IsDlgButtonChecked(m_hwnd, kctidFmtBdrDlgRows) == BST_CHECKED)
				m_grfBorders |= kfRows;
			else
				m_grfBorders &= ~kfRows;
			SetImages();
			break;
		default:
			return false;	// Button ID not recognized.
		}
		::InvalidateRect(::GetDlgItem(m_hwnd, kctidFmtBdrDlgDiag), NULL, false);
		return true;	// Button click handled.
	}
	if (pnmh->code == CBN_SELENDOK)
	{
		switch (ctidFrom)
		{
		case kctidColor:
			m_clrBorder = m_clrMod;
			if (m_clrBorder == kclrTransparent)
			{
				m_xColor = kxInherited;
				m_clrMod = m_clrBorderI;
			}
			else
				m_xColor = kxExplicit;

			::InvalidateRect(::GetDlgItem(m_hwnd, kctidFmtBdrDlgDiag), NULL, false);
			::InvalidateRect(::GetDlgItem(m_hwnd, kctidFmtBdrDlgWidth), NULL, true);
			return true;

		case kctidFmtBdrDlgWidth:
			int iCurSel = ::SendMessage(::GetDlgItem(m_hwnd, kctidFmtBdrDlgWidth),
				CB_GETCURSEL, 0,0);
			if (!m_fCanInherit)
				iCurSel++; // to account for (unspecified) which is not there
			if (iCurSel == 0)
			{
				// unspecified
				if (!m_fCanInherit)
					return false;
				m_xBorders = kxInherited;
				m_impWidth = 0;
				UpdateWidthCombo();
				::InvalidateRect(::GetDlgItem(m_hwnd, kctidFmtBdrDlgDiag), NULL, false);
				SetCheckBoxes();
				SetImages();
			}
			else if (iCurSel >= 1 && iCurSel < kcWidths)
			{
				m_impWidth = iCurSel;
				if (m_xBorders != kxExplicit)
				{
					m_grfBorders = m_grfBordersI;
					m_xBorders = kxExplicit;
					SetCheckBoxes();
					SetImages();
				}
				UpdateWidthCombo();
				::InvalidateRect(::GetDlgItem(m_hwnd, kctidFmtBdrDlgDiag), NULL, false);
			}
			return true;
		}
	}

	if (pnmh->code == CBN_KILLFOCUS)
	{
		UpdateComboWithInherited(ctidFrom, pnmh);
	}

	return false;	// Event not recognized.
}

/*----------------------------------------------------------------------------------------------
	When switching from inherited or conflicting to explicit, set the border width to
	something valid.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::SetExplicitWidth()
{
	if (m_xBorders == kxExplicit)
		return; // should already be something valid
	else if (!m_fCanInherit)
		m_impWidth = kimpDefaultWidth;
	else if (m_impWidthI <= 0) // conflicting or unspecified
		m_impWidth = kimpDefaultWidth;
	else
		m_impWidth = m_impWidthI;
}

/*----------------------------------------------------------------------------------------------
	Draws the preview diagram for the border paragraph dialog box.
	Note that FmtBdrDlg::DrawLines is called from both derived classes.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::DrawDiagramInit(HDC hdc, HWND hwndItem, RECT & rcItem,
	int & nTickLen, int & dzpPenWidth,
	int & dxpHalfWidth, int & dxpHalfHeight)
{
	SmartPalette spal(hdc);

	// First paint the rectangle white.
	int dxpWidth = rcItem.right - rcItem.left;
	int dypHeight = rcItem.bottom - rcItem.top;
	::PatBlt(hdc, rcItem.left, rcItem.top, dxpWidth, dypHeight, WHITENESS);

	// Create a rectangle inset a little from what was passed.
	::InflateRect(&rcItem, -dxpWidth / 8, -dxpWidth / 8);
	nTickLen = dxpWidth / 16;

	// First draw the fixed portion (black corner markers).
	::MoveToEx(hdc, rcItem.left - nTickLen, rcItem.top, NULL);
	::LineTo(hdc, rcItem.left, rcItem.top);
	::LineTo(hdc, rcItem.left, rcItem.top - nTickLen);

	::MoveToEx(hdc, rcItem.right + nTickLen, rcItem.top, NULL);
	::LineTo(hdc, rcItem.right, rcItem.top);
	::LineTo(hdc, rcItem.right, rcItem.top - nTickLen);

	::MoveToEx(hdc, rcItem.right + nTickLen, rcItem.bottom, NULL);
	::LineTo(hdc, rcItem.right, rcItem.bottom);
	::LineTo(hdc, rcItem.right, rcItem.bottom + nTickLen);

	::MoveToEx(hdc, rcItem.left - nTickLen, rcItem.bottom, NULL);
	::LineTo(hdc, rcItem.left, rcItem.bottom);
	::LineTo(hdc, rcItem.left, rcItem.bottom + nTickLen);

	Rect rcLine;
	int impWidth = ImpWidth();
	if (impWidth == -1 || impWidth == -1)
		impWidth = kimpDefaultWidth;
	dzpPenWidth = krgmpWidths[impWidth] * dxpWidth / knPenFactor + 1;

	// Store these values for later use in drawing gray lines
	dxpHalfWidth = (rcItem.right - rcItem.left) / 2;
	dxpHalfHeight = (rcItem.bottom - rcItem.top) / 2;

	int grfToDraw = GrfBorders();
	if (grfToDraw == FwStyledText::knConflicting || grfToDraw == FwStyledText::knUnspecified)
		grfToDraw = 0;
	COLORREF clr = ClrBorder();
	if (clr == (COLORREF)FwStyledText::knConflicting ||
		clr == (COLORREF)FwStyledText::knUnspecified)
	{
		clr = kclrBlack;
	}

	// Now draw the selected borders at the width given by krgmpWidths[m_impWidth]
	// and in the color given by m_clrBorder.
	if (grfToDraw & kfTop)
	{
		rcLine.Set(rcItem.left, rcItem.top, rcItem.right, rcItem.top + dzpPenWidth);
		AfGfx::FillSolidRect(hdc, rcLine, clr);
	}
	if (grfToDraw & kfTrailing)
	{
		rcLine.Set(rcItem.right - dzpPenWidth + 1, rcItem.top, rcItem.right + 1, rcItem.bottom);
		AfGfx::FillSolidRect(hdc, rcLine, clr);
	}
	if (grfToDraw & kfBottom)
	{
		rcLine.Set(rcItem.left, rcItem.bottom - dzpPenWidth + 1,
			rcItem.right, rcItem.bottom + 1);
		AfGfx::FillSolidRect(hdc, rcLine, clr);
	}
	if (grfToDraw & kfLeading)
	{
		rcLine.Set(rcItem.left, rcItem.top, rcItem.left + dzpPenWidth, rcItem.bottom);
		AfGfx::FillSolidRect(hdc, rcLine, clr);
	}
}

/*----------------------------------------------------------------------------------------------
	Update the values of the border flags based on the fact that a check box changed.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::CheckBoxChanged(int ctid)
{
	int nChecked;
	int iside;
	int fSide;
	if (ctid == kctidFmtBdrDlgTop)
	{
		iside = 0;
		fSide = kfTop;
	}
	else if (ctid == kctidFmtBdrDlgBottom)
	{
		iside = 1;
		fSide = kfBottom;
	}
	else if (ctid == kctidFmtBdrDlgLeading)
	{
		iside = 2;
		fSide = kfLeading;
	}
	else if (ctid == kctidFmtBdrDlgTrailing)
	{
		iside = 3;
		fSide = kfTrailing;
	}

	nChecked = IsDlgButtonChecked(m_hwnd, ctid);
	if (nChecked == BST_CHECKED)
	{
		m_grfBorders |= fSide;
		SetExplicitWidth();
		m_xBorders = kxExplicit;
	}
	else if (nChecked == BST_INDETERMINATE)
	{
		// Can't set to indeterminate by clicking--this can only happen by
		// choosing (unspecified) in the width combo. Toggle instead.
		if (m_grfBorders & fSide)
			m_grfBorders &= ~fSide; // turn off
		else
			m_grfBorders |= fSide;	// turn on
	}
	else if (nChecked == BST_UNCHECKED)
	{
		m_grfBorders &= ~fSide;
		SetExplicitWidth();
		m_xBorders = kxExplicit;
	}

	SetCheckBoxes();
	SetImages();
}

/*----------------------------------------------------------------------------------------------
	pttp is an old props value, which may get replaced by the ttp from the props builder.
	If the old and new values and variations are the same, do nothing.
	If the new value is FwStyledText::knUnspecified, delete the property.
	Otherwise, compute the new value of the property as nNew * nMul / nDiv, and (if that is
	not already its value), create a builder if necessary and set the value.
	Also, set pad to zero if border is zero, otherwise to the appropriate value.
	Note: allow for the possibility that pttp is null.

	Ideally, I think, if the new value of the border were the same as produced by the current
	style, we would change the border and pad to "unspecified." But currently we don't have
	access to the VwPropertyStore that would let us determine this.
----------------------------------------------------------------------------------------------*/
void UpdateBdrProp(ITsTextProps * pttp, int tpt, ITsPropsBldrPtr & qtpb,
	int nOld, int nNew, int nVarOld, int nVarNew, int nMul, int nDiv)
{
	AssertPtrN(pttp);
	// If this property has not changed, do nothing
	if (nOld == nNew && nVarOld == nVarNew)
		return;
	int nCur = nNew;
	if (nVarNew == ktpvMilliPoint && nNew != FwStyledText::knUnspecified)
		nCur = MulDiv(nNew, nMul, nDiv);
	int nVar = -1; // In case pttp is null, same result as if prop not found
	int nVal = -1;
	HRESULT hr = S_FALSE;
	if (pttp)
		CheckHr(hr = pttp->GetIntPropValues(tpt, &nVar, &nVal));
	// If it was and is unspecified, do nothing.
	if (hr == S_FALSE && nCur == FwStyledText::knUnspecified)
		return;
	// If this particular ttp already has the correct value, do nothing.
	if (nVar == nVarNew && nVal == nCur)
		return;
	// If we don't already have a builder, make one
	if (!qtpb)
	{
		if (pttp)
			CheckHr(pttp->GetBldr(&qtpb));
		else
			qtpb.CreateInstance(CLSID_TsPropsBldr);
	}
	int tptPad = 0; // corresponding pad direction
	int dzmpPad = 0; // corresponding pad amount
	FmtBdrDlg::GetPadInfo(tpt, tptPad, dzmpPad);
	// No padding if no border.
	if (!nCur)
		dzmpPad = 0;

	// If the new value is "inherited", delete the prop and corresponding pad; otherwise set the new val.
	if (nCur == FwStyledText::knUnspecified)
	{
		CheckHr(qtpb->SetIntPropValues(tpt, -1, -1));
		if (tptPad)
			CheckHr(qtpb->SetIntPropValues(tpt, -1, -1));
	}
	else
	{
		CheckHr(qtpb->SetIntPropValues(tpt, nVarNew, nCur));
		if (tptPad)
			CheckHr(qtpb->SetIntPropValues(tptPad, ktpvMilliPoint, dzmpPad));
	}
}

/*----------------------------------------------------------------------------------------------
	Given a border tpt, compute the corresponding pad tpt and standard amount.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::GetPadInfo(int tpt, int & tptPad, int & dzmpPad)
{
	tptPad = 0;
	dzmpPad = 0;
	switch (tpt)
	{
	case ktptBorderLeading:
		tptPad = ktptPadLeading;
		dzmpPad = 4000; // 4 pts
		break;
	case ktptBorderTrailing:
		tptPad = ktptPadTrailing;
		dzmpPad = 4000; // 4 pts
		break;
	case ktptBorderTop:
		tptPad = ktptPadTop;
		dzmpPad = 1000; // 1 pt
		break;
	case ktptBorderBottom:
		tptPad = ktptPadBottom;
		dzmpPad = 1000; // 1 pts
		break;
	default:
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the format border dialog for editing a style represented as a TsTextProps.
	Any property not specified in the pttp is considered conflicting.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::InitForStyle(ITsTextProps * pttp, ITsTextProps * pttpInherited,
	ParaPropRec & xprOrig, bool fEnable, bool fCanInherit)
{
	SetCanInherit(fCanInherit);

	HRESULT hr;
	COLORREF clr;
	int nVar, nVal;

	m_xColor = kxInherited;
	Assert (sizeof(int) == sizeof(COLORREF));
	CheckHr(hr = pttp->GetIntPropValues(ktptBorderColor, &nVar, (int *)(&clr)));
	if (hr == S_FALSE)
		clr = (COLORREF)FwStyledText::knUnspecified;
	else
		m_xColor = kxExplicit;

	COLORREF clrI = (COLORREF)FwStyledText::knUnspecified;
	if (m_fCanInherit)
	{
		CheckHr(hr = pttpInherited->GetIntPropValues(ktptBorderColor, &nVar, (int *)(&clrI)));
		if (hr == S_FALSE)
			clrI = (COLORREF)FwStyledText::knUnspecified;
	}

	static const int bits[4] = { kfTop, kfBottom, kfLeading, kfTrailing };
	int grfBorders = 0;
	int mpWidth = FwStyledText::knUnspecified;
	int nRtl = (m_fCanDoRtl) ? FwStyledText::knUnspecified : 0;
	m_xBorders = kxInherited;
	m_impWidth = 0;

	// Disable the controls if the selected style is not a paragraph style.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidColor), fEnable); // Color combobox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFmtBdrDlgWidth), fEnable); // Width combobox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFmtBdrDlgNoneP), fEnable); // None button.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFmtBdrDlgAll), fEnable); // All button.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFmtBdrDlgTop), fEnable); // Top button.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFmtBdrDlgTrailing), fEnable); // Right button.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFmtBdrDlgBottom), fEnable); // Bottom button.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFmtBdrDlgLeading), fEnable); // Left button.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFmtBdrDlgDiag), fEnable); // Diagram button.

	// If this is a paragraph style, get the properties associated with the Border dialog.
	if (fEnable)
	{
		for (int iside = 0; iside < 4; iside++)
		{
			CheckHr(hr = pttp->GetIntPropValues(ktptBorderTop + iside, &nVar, &nVal));
			if (nVal != 0 && hr != S_FALSE)
			{
				grfBorders |= bits[iside];
				mpWidth = nVal;
				m_xBorders = kxExplicit;
			}
			else if (hr != S_FALSE)
				m_xBorders = kxExplicit;
		}

		// Get the style direction.
		CheckHr(hr = pttp->GetIntPropValues(ktptRightToLeft, &nVar, &nVal));
		if (nVal != -1 && hr != S_FALSE)
			nRtl = nVal;

		// Also get the inherited values.
		int grfBordersI = 0;
		int mpWidthI = FwStyledText::knUnspecified;
		int nRtlI = (m_fCanDoRtl) ? FwStyledText::knUnspecified : 0;
		for (int iside = 0; iside < 4; iside++)
		{
			CheckHr(hr = pttpInherited->GetIntPropValues(ktptBorderTop + iside, &nVar, &nVal));
			if (nVal != 0 && hr != S_FALSE)
			{
				grfBordersI |= bits[iside];
				mpWidthI = nVal;
			}
		}
		CheckHr(hr = pttp->GetIntPropValues(ktptRightToLeft, &nVar, &nVal));

		if (nRtl == FwStyledText::knUnspecified)
			nRtl = nRtlI;

		SetDialogValues(clrI, grfBordersI, mpWidthI, nRtl, false);
	}

	SetDialogValues(clr, grfBorders, mpWidth, nRtl, true);

	if (!m_fCanInherit)
	{
		// Merge explicit and inherited values, and treat them like they were all explicit.
		if (m_xColor != kxExplicit && m_clrBorderI != (COLORREF)FwStyledText::knUnspecified)
			m_clrBorder = m_clrBorderI;
		m_xColor = kxExplicit;

		if (m_xBorders != kxExplicit)
		{
			m_grfBorders &= m_grfBordersI;
		}
		m_xBorders = kxExplicit;
	}

	FillCtls();
}

/*----------------------------------------------------------------------------------------------
	Change the parameter that says whether or not we are showing inheritance in the dialog.
	Adjust the options offered by certain combo-boxes.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::SetCanInherit(bool fCanInherit)
{
	if (m_fCanInherit != fCanInherit)
	{
		m_fCanInherit = fCanInherit;

		// Clear out the options that are based on whether or not inheritance can happen,
		// and regenerate them.
		HWND hwndWidth = ::GetDlgItem(m_hwnd, kctidFmtBdrDlgWidth);
		::SendMessage(hwndWidth, CB_RESETCONTENT, 0, 0);
		InitWidthCombo();
	}
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::GetStyleEffects(ITsTextProps *pttpOrig, ITsTextProps ** ppttp)
{
	Assert(!*ppttp);
	HRESULT hr;
	COLORREF clrNew;
	int grfBordersNew;
	int mpWidthNew;
	GetDialogValues(&clrNew, &grfBordersNew, &mpWidthNew);
	ITsPropsBldrPtr qtpb;
	int nVar, nValOrig;

	// color
	COLORREF clrOld;
	CheckHr(hr = pttpOrig->GetIntPropValues(ktptBorderColor, &nVar, (int *)(&clrOld)));
	if (hr == S_FALSE)
	{
		clrOld = (COLORREF)FwStyledText::knUnspecified;
		nVar = ktpvDefault;
	}
	UpdateBdrProp(pttpOrig, ktptBorderColor, qtpb, clrOld, clrNew, nVar, nVar, 1, 1);

	// sides
	static const int bits[4] = { kfTop, kfBottom, kfLeading, kfTrailing };
	for (int iside = 0; iside < 4; iside++)
	{
		CheckHr(hr = pttpOrig->GetIntPropValues(ktptBorderTop + iside, &nVar, &nValOrig));
		if (hr == S_FALSE)
		{
			nValOrig = FwStyledText::knUnspecified;
			nVar = ktpvMilliPoint; // The only reasonable variation at present.
		}
		int nValNew = FwStyledText::knUnspecified;
		if (grfBordersNew == FwStyledText::knUnspecified)
		{}
		else if (!(grfBordersNew & bits[iside]))
		{
			// turned off in the dialog
			if (m_fCanInherit && m_grfBordersI & bits[iside])
				nValNew = 0; // differs from inherited value--definitely turn it off
			else if (nValOrig != FwStyledText::knUnspecified)
				nValNew = 0; // value has been changed
			// else leave unspecified
		}
		else
			nValNew = mpWidthNew;
		UpdateBdrProp(pttpOrig, ktptBorderTop + iside, qtpb, nValOrig, nValNew, nVar, nVar, 1, 1);
	}
	if (qtpb)
		CheckHr(qtpb->GetTextProps(ppttp));
}

/*----------------------------------------------------------------------------------------------
	Generate new properties by bringing up the dialog and letting the user manipulate it.

	vpttp and vpttpHard are identical except that the vpttpHard members have had
	the named style removed. Thus vpttp is what should be used in generating the new version
	of the properties, while vpttpHard is what is used to fill the controls. vqvpsSoft is used
	to show the inherited or soft values. Review (SharonC): Really we shouldn't have to pass
	in vpttpHard, since the only difference is the named style, and we would ignore that
	anyway. We could get by with just vpttp.
----------------------------------------------------------------------------------------------*/
bool FmtBdrDlg::AdjustTsTextProps(HWND hwnd, bool fCanDoRtl, bool fOuterRtl,
	TtpVec & vpttp, TtpVec & vpttpHard, VwPropsVec &vqvpsSoft)
{
	Assert (sizeof(int) == sizeof(COLORREF));
	static const int fBdrbit[4] = { kfTop, kfBottom, kfLeading, kfTrailing };
	struct BdrStuff {
		COLORREF color;
		int side[4];
		int borders;
		int width;
		int rtl;
	};

	BdrStuff hard, soft, znew;

	int cttp = vpttpHard.Size();
	Assert(cttp == vqvpsSoft.Size());

	memset(&hard, 0, sizeof(hard));
	memset(&soft, 0, sizeof(soft));

	int xSide[4];
	xSide[0] = kxInherited;
	xSide[1] = kxInherited;
	xSide[2] = kxInherited;
	xSide[3] = kxInherited;
	int xColor = kxInherited;
	int xBorders = kxInherited;

	// Get the stored border properties.
	for (int nHard = 0; nHard <= 1; nHard++)
	{
		bool fHard = (bool)nHard;

		for (int ittp = 0; ittp < cttp; ittp++)
		{
			// When asking for inherited values, just use the property store.
			ITsTextProps * pttp = (fHard) ? vpttpHard[ittp] : NULL;
			// When asking for explicit values, just use the text properties.
			IVwPropertyStore * pvps = (fHard) ? NULL : vqvpsSoft[ittp];

			bool fFirst = (ittp == 0);

			MergeFmtDlgIntProp(pttp, pvps, ktptBorderTop, ktpvMilliPoint,
				hard.side[0], soft.side[0], fFirst, xSide[0], fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptBorderBottom, ktpvMilliPoint,
				hard.side[1], soft.side[1], fFirst, xSide[1], fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptBorderLeading, ktpvMilliPoint,
				hard.side[2], soft.side[2], fFirst, xSide[2], fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptBorderTrailing, ktpvMilliPoint,
				hard.side[3], soft.side[3], fFirst, xSide[3], fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptBorderColor, ktpvDefault,
				hard.color, soft.color, fFirst, xColor, fHard);

			// Also figure out the paragraph direction, We don't care about inheritance,
			// we just want a valid value to use for labeling the dialog.
			if (fHard)
			{
				MergeIntProp(vpttpHard[ittp], vqvpsSoft[ittp],
					ktptRightToLeft, ktpvEnum,
					hard.rtl, soft.rtl, fFirst);
			}
		}
	}

	if (hard.rtl == FwStyledText::knUnspecified)
		hard.rtl = soft.rtl;

	if (xSide[0] == kxConflicting || xSide[1] == kxConflicting ||
		xSide[2] == kxConflicting || xSide[3] == kxConflicting)
	{
		hard.borders = FwStyledText::knConflicting;
		hard.width = FwStyledText::knConflicting;
		int nVal, nVar;
		HRESULT hr;
		//check to see if the first paragraph has properties assigned
		if ((void *)vpttp[0] != NULL)
			hr = vpttp[0]->GetIntPropValues(ktptBorderTop, &nVar, &nVal);
		else
			hr = S_FALSE;
		for (int ittp = 0; ittp < vpttp.Size(); ittp++)
		{
			int rgtpt[4] = { ktptBorderTop, ktptBorderBottom, ktptBorderLeading, ktptBorderTrailing };
			for (int itpt = 0; itpt < 4; itpt++)
			{
				HRESULT hr2;
				if ((void *)vpttp[ittp] != NULL)
					hr2 = vpttp[ittp]->GetIntPropValues(rgtpt[itpt], &nVar, &nVal);
				else
					hr2 = S_FALSE;
				if (hr != hr2)
				{
					xBorders = kxConflicting;
					goto LBreak;
				}
				else if (hr == S_OK)
					xBorders = kxExplicit;
				else
					xBorders = kxInherited;
			}
		}
	}
	else if (xSide[0] == kxExplicit || xSide[1] == kxExplicit ||
		xSide[2] == kxExplicit || xSide[3] == kxExplicit)
	{
		xBorders = kxExplicit;
	}
	else
	{
		xBorders = kxInherited;
		hard.borders = FwStyledText::knUnspecified;
		hard.width = FwStyledText::knUnspecified;
	}
LBreak:

	// Convert to the form needed by the dialog:
	for (int nHard = 0; nHard <= 1; nHard++)
	{
		BdrStuff & tmp = (nHard == 1) ? hard : soft;
		if (nHard == 1 && xBorders == kxInherited)
			break;
		tmp.borders = 0;
		for (int i = 0; i < 4; i++)
		{
			/*
			if (tmp.side[i] == FwStyledText::knUnspecified)
			{
				tmp.width = FwStyledText::knUnspecified;
				tmp.borders = 0;
				break;
			}
			else if (tmp.side[i] == FwStyledText::knConflicting)
			{
				tmp.width = FwStyledText::knConflicting;
				tmp.borders = 0;
				break;
			}
			else
			*/
			if (!(tmp.side[i] == 0 ||
				  tmp.side[i] == FwStyledText::knConflicting ||
				  tmp.side[i] == FwStyledText::knUnspecified))
			{
				tmp.width = tmp.side[i];
				tmp.borders |= fBdrbit[i];
			}
		}
	}

	// Execute the dialog.
	GenSmartPtr<FmtBdrDlgPara> dlg;
	dlg.Create();
	dlg->SetCanDoRtl(fCanDoRtl);
	dlg->SetOuterRtl(fOuterRtl);
	dlg->SetDialogValues(soft.color, soft.borders, soft.width, soft.rtl, false);
	dlg->SetDialogValues(hard.color, hard.borders, hard.width, hard.rtl, true);
	dlg->SetInheritance(xColor, xBorders);
	AfDialogShellPtr qdlgs;
	qdlgs.Create();
	if (qdlgs->CreateDlgShell(dlg, _T("Border"), hwnd) != kctidOk)
		return false;

	// Get new values from dialog.
	dlg->GetDialogValues (&znew.color, &znew.borders, &znew.width);
	// Convert to form needed by text properties.
	if (znew.borders == FwStyledText::knUnspecified ||
		znew.borders == FwStyledText::knConflicting)
	{
		znew.side[0] = znew.side[1] = znew.side[2] = znew.side[3] = znew.borders;
	}
	else
	{
		for (int i = 0; i < 4; i++)
			znew.side[i] = (znew.borders & fBdrbit[i]) ? znew.width : 0;
	}

	// Store any new values.
	ITsPropsBldrPtr qtpb;
	bool fChanged = false;
	// Now see what changes we have to deal with.
	for (int ittp = 0; ittp < cttp; ittp++)
	{
		ITsTextProps * pttp = vpttp[ittp];
		qtpb = NULL;

		if (znew.borders != FwStyledText::knUnspecified &&
			znew.borders != FwStyledText::knConflicting)
		{
			// If one changed they all changed.
			UpdateBdrProp(pttp, ktptBorderTop, qtpb, -1, znew.side[0],
				ktpvMilliPoint, ktpvMilliPoint, 1, 1);
			UpdateBdrProp(pttp, ktptBorderBottom, qtpb, -1, znew.side[1],
				ktpvMilliPoint, ktpvMilliPoint, 1, 1);
			UpdateBdrProp(pttp, ktptBorderLeading, qtpb, -1, znew.side[2],
				ktpvMilliPoint, ktpvMilliPoint, 1, 1);
			UpdateBdrProp(pttp, ktptBorderTrailing, qtpb, -1, znew.side[3],
				ktpvMilliPoint, ktpvMilliPoint, 1, 1);
		}
		else if (znew.borders == FwStyledText::knUnspecified && znew.borders != hard.borders)
		{
			UpdateBdrProp(pttp, ktptBorderTop, qtpb, hard.side[0], znew.side[0],
				ktpvMilliPoint, ktpvMilliPoint, 1, 1);
			UpdateBdrProp(pttp, ktptBorderBottom, qtpb, hard.side[1], znew.side[1],
				ktpvMilliPoint, ktpvMilliPoint, 1, 1);
			UpdateBdrProp(pttp, ktptBorderLeading, qtpb, hard.side[2], znew.side[2],
				ktpvMilliPoint, ktpvMilliPoint, 1, 1);
			UpdateBdrProp(pttp, ktptBorderTrailing, qtpb, hard.side[3], znew.side[3],
				ktpvMilliPoint, ktpvMilliPoint, 1, 1);
		}
		UpdateBdrProp(pttp, ktptBorderColor, qtpb, hard.color, znew.color,
			ktpvDefault, ktpvDefault, 1, 1);

		ITsTextPropsPtr qttpNew;
		if (qtpb) // If any changes, we now have a props builder with new value(s)
		{
			CheckHr(qtpb->GetTextProps(&qttpNew));
			fChanged = true;
		}
		vpttp[ittp] = qttpNew;
	}
	return fChanged;
}

/*----------------------------------------------------------------------------------------------
	Draws a series of horizontal gray lines of width dxpWidth and at spacing nSpacing inside
	a bounding rectangle rect. Make first/last lines indented by 1/4 of width.
----------------------------------------------------------------------------------------------*/
void DrawLines(HDC hdc, int dxpWidth, int nSpacing, const RECT & rect)
{
	HPEN hpenOld;
	HPEN hpen = ::CreatePen(PS_SOLID, dxpWidth, kclrLightGray);
	hpenOld = (HPEN)::SelectObject(hdc, hpen);
	int nPos = rect.top;
	while (nPos < rect.bottom)
	{
		MoveToEx(hdc, rect.left, nPos, NULL);
		if (nPos == rect.top) // First line.
			::MoveToEx(hdc, rect.left + (rect.right - rect.left) / 4, nPos, NULL);
		if (nPos + nSpacing >= rect.bottom) // Last line/
			::LineTo(hdc, rect.right - (rect.right - rect.left) / 4, nPos);
		else
			LineTo(hdc, rect.right, nPos);
		nPos += nSpacing;
	}
	::SelectObject(hdc, hpenOld);
	::DeleteObject(hpen);
}

/*----------------------------------------------------------------------------------------------
	If the value has been changed to inherited, update the combo-box to show
	the inherited value.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::UpdateComboWithInherited(int ctid, NMHDR * pnmh)
{
	if (pnmh->idFrom == kctidFmtBdrDlgWidth)
	{
		if (m_xBorders == kxExplicit && m_fCanInherit)
		{
			UpdateWidthCombo();
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Reset the width combo to reflect the current state. Needed when something about
	inheritance has changed.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlg::UpdateWidthCombo()
{
	int impWidth = ImpWidth();
	::SendMessage(::GetDlgItem(m_hwnd, kctidFmtBdrDlgWidth), CB_SETCURSEL, impWidth, 0);
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidFmtBdrDlgWidth), NULL, false);
}


/*----------------------------------------------------------------------------------------------
	Handle What's This? help.
----------------------------------------------------------------------------------------------*/
bool FmtBdrDlg::OnHelpInfo(HELPINFO * phi)
{
	if (m_pafsd)
		return m_pafsd->DoHelpInfo(phi, m_hwnd);
	else
		return SuperClass::OnHelpInfo(phi);
}


/***********************************************************************************************
	FmtBdrDlgPara implementation
***********************************************************************************************/

FmtBdrDlgPara::FmtBdrDlgPara()
	: FmtBdrDlg(kridFmtBdrDlgP)
{
	m_grfBorders &= kfBox;	// Ensure spurious grid bits are not set for paragraph.
}

FmtBdrDlgPara::FmtBdrDlgPara(AfStylesDlg * pafsd)
	: FmtBdrDlg(pafsd, kridFmtBdrDlgP)
{
	m_grfBorders &= kfBox;
}

/*----------------------------------------------------------------------------------------------
	Draws the preview diagram for the border paragraph dialog box.
	Note that FmtBdrDlg::DrawLines is called from both derived classes.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlgPara::DrawDiagram(HDC hdc, HWND hwndItem, RECT rcItem)
{
	SmartPalette spal(hdc);

	int nTickLen, dxpHalfWidth, dxpHalfHeight, dzpPenWidth;
	DrawDiagramInit (hdc, hwndItem, rcItem, nTickLen, dzpPenWidth, dxpHalfWidth, dxpHalfHeight);

	// Decrease the rectangle size by nTickLen / 2 + the width of the borders drawn.
	// Do this all round, regardless of which borders have been drawn.
	int dzp = dzpPenWidth + nTickLen / 2;
	::InflateRect(&rcItem, -dzp, -dzp);

	// Draw gray lines of width nTickLen / 2, spaced by nTickLen, inside rcItem.
	DrawLines(hdc, nTickLen / 2, nTickLen, rcItem);
	return;
}

/*----------------------------------------------------------------------------------------------
	Sets check boxes according to m_grfBorders.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlgPara::SetCheckBoxes()
{
	::CheckDlgButton(m_hwnd, kctidFmtBdrDlgTop, CheckForSide(0));
	::CheckDlgButton(m_hwnd, kctidFmtBdrDlgBottom, CheckForSide(1));
	::CheckDlgButton(m_hwnd, kctidFmtBdrDlgLeading, CheckForSide(2));
	::CheckDlgButton(m_hwnd, kctidFmtBdrDlgTrailing, CheckForSide(3));
//	SCB (Top);
//	SCB (Bottom);
//	SCB (Leading);
//	SCB (Trailing);
}

/*----------------------------------------------------------------------------------------------
	Sets button images according to m_grfBorders.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlgPara::SetImages()
{
	if (m_hwnd)
	{
		::InvalidateRect(::GetDlgItem(m_hwnd, kctidFmtBdrDlgNoneP), NULL, false);
		::InvalidateRect(::GetDlgItem(m_hwnd, kctidFmtBdrDlgAll), NULL, false);
		::InvalidateRect(::GetDlgItem(m_hwnd, kctidFmtBdrDlgDiag), NULL, false);
	}
}

/***********************************************************************************************
	FmtBdrDlgTable implementation
***********************************************************************************************/

FmtBdrDlgTable::FmtBdrDlgTable()
	: FmtBdrDlg(kridFmtBdrDlgT)
{
	m_grfBorders &= kfGrid;	// Ensure spurious bits are not set.
}

FmtBdrDlgTable::FmtBdrDlgTable(AfStylesDlg * pafsd)
	: FmtBdrDlg(pafsd, kridFmtBdrDlgT)
{
	m_grfBorders &= kfGrid;
}

/*----------------------------------------------------------------------------------------------
	Draws the preview diagram for the border paragraph dialog box.
	Note that FmtBdrDlg::DrawLines is called from both derived classes.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlgTable::DrawDiagram(HDC hdc, HWND hwndItem, RECT rcItem)
{
	int grfToDraw = GrfBorders();
	if (grfToDraw == FwStyledText::knConflicting || grfToDraw == FwStyledText::knUnspecified)
		grfToDraw = 0;
	COLORREF clr = ClrBorder();
	if (clr == (COLORREF)FwStyledText::knConflicting ||
		grfToDraw == (COLORREF)FwStyledText::knUnspecified)
	{
		clr = kclrBlack;
	}

	SmartPalette spal(hdc);

	int nTickLen, dxpHalfWidth, dxpHalfHeight, dzpPenWidth;
	DrawDiagramInit (hdc, hwndItem, rcItem, nTickLen, dzpPenWidth, dxpHalfWidth, dxpHalfHeight);

	Rect rcLine;
	if (grfToDraw & kfRows)
	{
		int ypLineTop = (rcItem.top + rcItem.bottom - dzpPenWidth) / 2;
		rcLine.Set(rcItem.left, ypLineTop, rcItem.right, ypLineTop + dzpPenWidth);
		AfGfx::FillSolidRect(hdc, rcLine, clr);
	}
	if (grfToDraw & kfCols)
	{
		int xpLineLeft = (rcItem.left + rcItem.right - dzpPenWidth) / 2;
		rcLine.Set(xpLineLeft, rcItem.top, xpLineLeft + dzpPenWidth, rcItem.bottom);
		AfGfx::FillSolidRect(hdc, rcLine, clr);
	}

	// Decrease the overall bounding rectangle size by nTickLen / 2 + the width
	// of the borders drawn.
	// Do this all round, regardless of which borders have been drawn.
	int dzp = dzpPenWidth + nTickLen / 2;
	::InflateRect(&rcItem, -dzp, -dzp);

	// Now work out the width and height of each of the four cells to be drawn.
	int dxpCellWidth = (rcItem.right - rcItem.left) / 2 - dzp;
	int dypCellHeight = (rcItem.bottom - rcItem.top) / 2 - dzp;

	Rect rcCell(rcItem.left, rcItem.top, rcItem.left + dxpCellWidth,
		rcItem.top + dypCellHeight);

	// Draw gray lines of width nTickLen/2, spaced by nTickLen, inside rcCell.
	DrawLines(hdc, nTickLen / 2, nTickLen, rcCell);

	// Move rcCell to other corners and draw.
	rcCell.Offset(dxpHalfWidth, 0); // Top right.
	DrawLines(hdc, nTickLen / 2, nTickLen, rcCell);
	rcCell.Offset(0, dxpHalfHeight); // Bottom right.
	DrawLines(hdc, nTickLen / 2, nTickLen, rcCell);
	rcCell.Offset(-dxpHalfWidth, 0); // Bottom left.
	DrawLines(hdc, nTickLen / 2, nTickLen, rcCell);
}

/*----------------------------------------------------------------------------------------------
	Sets check boxes according to m_grfBorders.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlgTable::SetCheckBoxes()
{
	SCB (Top);
	SCB (Bottom);
	SCB (Leading);
	SCB (Trailing);
	SCB (Rows);
	SCB (Cols);
}

/*----------------------------------------------------------------------------------------------
	Sets images according to m_grfBorders.
----------------------------------------------------------------------------------------------*/
void FmtBdrDlgTable::SetImages()
{
	int imag;
	HWND hwndButton;
	imag = (m_grfBorders & kfGrid) ? NONET : NONET_S;
	hwndButton = GetDlgItem(m_hwnd, kctidFmtBdrDlgNoneT);
	::SendMessage(hwndButton, STM_SETIMAGE, IMAGE_BITMAP, (LPARAM)m_rghbmp[imag]);
	imag = ((m_grfBorders & kfGrid) == kfBox) ? BOX_S : BOX;
	hwndButton = GetDlgItem(m_hwnd, kctidFmtBdrDlgBox);
	::SendMessage(hwndButton, STM_SETIMAGE, IMAGE_BITMAP, (LPARAM)m_rghbmp[imag]);
	imag = ((m_grfBorders & kfGrid) == kfGrid) ? GRID_S : GRID;
	hwndButton = GetDlgItem(m_hwnd, kctidFmtBdrDlgGrid);
	::SendMessage(hwndButton, STM_SETIMAGE, IMAGE_BITMAP, (LPARAM)m_rghbmp[imag]);
}
