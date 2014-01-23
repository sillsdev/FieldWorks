/*----------------------------------------------------------------------------------------------
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: BorderDialog.cpp
Responsibility: John Landon
Last reviewed: Not yet.

Description:
	Implementation of the Paragraph Border Dialog class.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


/***********************************************************************************************
	Static arrays: initialisation
***********************************************************************************************/

const int FmtBorderDlg::krguWidths[kcWidths] =
	{250, 500, 750, 1000, 1500, 2250, 3000, 4500, 6000};	// Units are 0.001 points.
const char * FmtBorderDlg::krgpszWidths[kcWidths] =
	{"\xbc", "\xbd", "\xbe", "1", "1\xbd", "2\xbc", "3", "4\xbd", "6"};
const int FmtBorderDlg::krguBitmapIds[kchBitmaps] =
	{IDB_NONE, IDB_NONE_S, IDB_ALL, IDB_ALL_S, IDB_BOX, IDB_BOX_S, IDB_GRID, IDB_GRID_S,
		IDB_NONET, IDB_NONET_S};

/***********************************************************************************************
	Methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
FmtBorderDlg::FmtBorderDlg(void)
{
	m_clrBorder = RGB(0, 0, 0);
	m_fBorders = 0;
	m_nWidth = 1;

	int i;
	for (i = 0; i < kchBitmaps; ++i)
		m_rghBitmaps[i] = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
FmtBorderDlg::~FmtBorderDlg()
{
	int i;

	for (i = 0; i < kchBitmaps; ++i)
	{
		if (m_rghBitmaps[i])
			::DeleteObject(m_rghBitmaps[i]);
	}
}


bool FmtBorderDlg::FDlgProc(uint wm, WPARAM wp, LPARAM lp)
{
	switch (wm)
	{
	case WM_INITDIALOG:
		return OnInitDlg(wp, lp);
	case WM_COMMAND:
		return OnCommand(LOWORD(wp), HIWORD(wp), (HWND)lp);
	case WM_MEASUREITEM:
		return OnMeasureItem(wp, (MEASUREITEMSTRUCT *)lp);
	case WM_DRAWITEM:
		return OnDrawItem(wp, (DRAWITEMSTRUCT *)lp);
	}

	return false;
}


bool FmtBorderDlg::OnInitDlg(WPARAM wp, LPARAM lp)
{
	// Initialise the Width owner-draw list combo box.
	StrAppBuf strbPt;
	StrAppBuf strb;
	HMODULE hmod = ModuleEntry::GetModuleHandle();

	strb.SetLength(strbPt.kcchMaxStr);

	// Get the " pt" string.
	LoadString(hmod, IDST_PT, &strbPt[0], strbPt.kcchMaxStr);
	strbPt.SetLength(StrLen(strbPt.Chars()));

	HWND hwndWidth = ::GetDlgItem(m_hwnd, IDC_WIDTH);
	int nIndex;

	// Load up each string to the combo box (1/4 pt, 1/2 pt, etc.).
	for (nIndex = 0; nIndex < kcWidths; ++nIndex)
	{
		strb = krgpszWidths[nIndex];
		strb += strbPt;
		::SendMessage(hwndWidth, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	}

	// Load all bitmaps. Note that strictly only four are needed for paragraph border.
	int i;
	for (i = 0; i < kchBitmaps; ++i)
	{
		m_rghBitmaps[i] = LoadImage(hmod, (LPCSTR)krguBitmapIds[i],
			IMAGE_BITMAP, 0, 0, 0);
		if (!m_rghBitmaps[i])
			ThrowHr(WarnHr(E_FAIL));
	}
	return true;
}


void FmtBorderDlg::SetDialogValues(COLORREF crColor, int fBorders, int uWidth)
{
//	int nret = g_ColorTable.GetIndexFromColor(crColor);
	int nret = -1;
	if (nret < 0)
	{
		m_clrBorder = RGB(0, 0, 0);
	}
	else
	{
		m_clrBorder = crColor;
	}
	m_fBorders = fBorders;	// All except low 6 bits are ignored.

	// Set width to index corresponding to value passed if this is one of the ones we use.
	// Else set to the index for 1/2-point (= 1).
	m_nWidth = 1;
	int i;
	for (i = 0; i < kcWidths; ++i)
	{
		if (uWidth == krguWidths[i])
		{
			m_nWidth = i;
			break;
		}
	}
	return;
}


void FmtBorderDlg::GetDialogValues(COLORREF * pcrColor, int * pfBorders, int * puWidth)
{
	AssertPtr(pcrColor);
	AssertPtr(pfBorders);
	AssertPtr(puWidth);
	*pcrColor = m_clrBorder;
	*pfBorders = m_fBorders;
	*puWidth = krguWidths[m_nWidth];
	return;
}


bool FmtBorderDlg::OnMeasureItem(int ctid, MEASUREITEMSTRUCT * pmis)
{
	AssertPtr(pmis);

	Rect rect;
	switch (ctid)
	{
	case IDC_DIAGRAM:
		::GetWindowRect(::GetDlgItem(m_hwnd, IDC_DIAGRAM), &rect);
		pmis->itemWidth = rect.right - rect.left;	// Leave width as designed.
		pmis->itemHeight = rect.top - rect.bottom;	// Leave height as designed.
		return true;
	case IDC_WIDTH:
		::GetWindowRect(::GetDlgItem(m_hwnd, IDC_WIDTH), &rect);
		pmis->itemWidth = rect.right - rect.left;	// Leave width as designed.
		pmis->itemHeight = kcListHeight;
		return true;
	}
	return false;
}


bool FmtBorderDlg::OnDrawItem(int ctid, DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);

	bool fRet = false;

	switch (ctid)
	{
	case IDC_DIAGRAM:
		DrawDiagram(pdis->hDC, pdis->hwndItem, pdis->rcItem);
		return true;
	case IDC_WIDTH:
		RECT rect;
		int nIndex;
		nIndex = pdis->itemID;
		uint uListWidth;
		uint uListHeight;
		int nPenWidth;
		HPEN hOldPen;
		HPEN hPen;
		::GetWindowRect(::GetDlgItem(m_hwnd, IDC_DIAGRAM), &rect);
		uListWidth = pdis->rcItem.right - pdis->rcItem.left;
		uListHeight = pdis->rcItem.bottom - pdis->rcItem.top;
		char pszText[20];


		if (pdis->itemState & ODS_COMBOBOXEDIT)
		{
			// Draw the selection (edit) box with the value currently stored.
			// Make width of pen the same as when drawing the preview diagram.
			nPenWidth = (krguWidths[m_nWidth] *
				(rect.right - rect.left) + knPenFactor/2)/knPenFactor;
			hPen = CreatePen(PS_SOLID, nPenWidth, m_clrBorder);
			hOldPen = (HPEN)SelectObject(pdis->hDC, hPen);
			MoveToEx(pdis->hDC, uListWidth*3/8, uListHeight*5/8, NULL);
			LineTo(pdis->hDC, pdis->rcItem.right - uListWidth/8, uListHeight*5/8);
			SelectObject(pdis->hDC, hOldPen);
			DeleteObject(hPen);
			::SendMessage(pdis->hwndItem, CB_GETLBTEXT, m_nWidth, (LPARAM)pszText);
			TextOut(pdis->hDC, uListWidth/16, uListHeight/3, pszText, strlen(pszText));
			fRet = true;
		}

		// nIndex is index to list.
		if (nIndex >= 0)
		{
			nPenWidth = (krguWidths[nIndex] *
				(rect.right - rect.left) + knPenFactor/2)/knPenFactor;
			hPen = CreatePen(PS_SOLID, nPenWidth, m_clrBorder);
			hOldPen = (HPEN)SelectObject(pdis->hDC, hPen);
			MoveToEx(pdis->hDC, uListWidth*3/8, uListHeight/2 + uListHeight*nIndex, NULL);
			LineTo(pdis->hDC, pdis->rcItem.right - uListWidth/16,
				uListHeight/2 + uListHeight*nIndex);
			SelectObject(pdis->hDC, hOldPen);
			DeleteObject(hPen);
			::SendMessage(pdis->hwndItem, CB_GETLBTEXT, nIndex, (LPARAM)pszText);
			TextOut(pdis->hDC, uListWidth/16, uListHeight/6 + uListHeight*nIndex,
				pszText, strlen(pszText));
			// Draw focus rectangle if required.
			if (pdis->itemState & ODS_SELECTED)
				DrawFocusRect(pdis->hDC, &pdis->rcItem);
			fRet = true;
		}

		// Draw focus rectangle if required.
		if (pdis->itemState & ODS_FOCUS)
			DrawFocusRect(pdis->hDC, &pdis->rcItem);
		return fRet;
	}

	return false;
}


bool FmtBorderDlg::OnCommand(int ctid, int nc, HWND hctl)
{
	if (nc == BN_CLICKED)
	{
		switch (ctid)
		{
		case IDOK:
		case IDCANCEL:
			::EndDialog(m_hwnd, ctid);
			return true;

		case IDC_NONE:
			if (m_fBorders & kfGrid)
			{
				// At least one border exists, so remove it.
				m_fBorders =0;
				SetCheckBoxes();
				SetImages();
			}
			break;
		case IDC_ALL:
		case IDC_BOX:
			if ((m_fBorders & kfBox) != kfBox)
			{
				m_fBorders |= kfBox;
				SetCheckBoxes();
				SetImages();
			}
			break;
		case IDC_GRID:
			if ((m_fBorders & kfGrid) != kfGrid)
			{
				m_fBorders |= kfGrid;
				SetCheckBoxes();
				SetImages();
			}
			break;
		case IDC_TOP:
			if (IsDlgButtonChecked(m_hwnd, IDC_TOP) == BST_CHECKED)
				m_fBorders |= kfTop;
			else
				m_fBorders &= ~kfTop;
			SetImages();
			break;
		case IDC_BOTTOM:
			if (IsDlgButtonChecked(m_hwnd, IDC_BOTTOM) == BST_CHECKED)
				m_fBorders |= kfBottom;
			else
				m_fBorders &= ~kfBottom;
			SetImages();
			break;
		case IDC_LEFT:
			if (IsDlgButtonChecked(m_hwnd, IDC_LEFT) == BST_CHECKED)
				m_fBorders |= kfLeft;
			else
				m_fBorders &= ~kfLeft;
			SetImages();
			break;
		case IDC_RIGHT:
			if (IsDlgButtonChecked(m_hwnd, IDC_RIGHT) == BST_CHECKED)
				m_fBorders |= kfRight;
			else
				m_fBorders &= ~kfRight;
			SetImages();
			break;
		case IDC_COLS:
			if (IsDlgButtonChecked(m_hwnd, IDC_COLS) == BST_CHECKED)
				m_fBorders |= kfColumns;
			else
				m_fBorders &= ~kfColumns;
			SetImages();
			break;
		case IDC_ROWS:
			if (IsDlgButtonChecked(m_hwnd, IDC_ROWS) == BST_CHECKED)
				m_fBorders |= kfRows;
			else
				m_fBorders &= ~kfRows;
			SetImages();
			break;
		default:
			return false; // Button ID not recognized.
		}
		::InvalidateRect(::GetDlgItem(m_hwnd, IDC_DIAGRAM), NULL, false);
		return true;
	}

	if (nc == CBN_SELENDOK)
	{
		switch (ctid)
		{
		case IDC_WIDTH:
			uint uCurSel;
			uCurSel = ::SendMessage(::GetDlgItem(m_hwnd, IDC_WIDTH), CB_GETCURSEL, 0,0);
			if (uCurSel > 0 && uCurSel < kcWidths)
			{
				m_nWidth = ::SendMessage(::GetDlgItem(m_hwnd, IDC_WIDTH), CB_GETCURSEL, 0,0);
				::InvalidateRect(::GetDlgItem(m_hwnd, IDC_DIAGRAM), NULL, false);
			}
			return true;
		}
	}

	return false; // Event not recognized.
}


/*----------------------------------------------------------------------------------------------
	Draws a series of horizontal grey lines of width nWidth and at spacing nSpacing inside
	a bounding rectangle rect. Make first/last lines indented by 1/4 of width.
----------------------------------------------------------------------------------------------*/
void FmtBorderDlg::DrawLines(HDC hDC, int nWidth, int nSpacing, const RECT& rect)
{
	HPEN hOldPen;
	HPEN hPen = ::CreatePen(PS_SOLID, nWidth, RGB(0xC0, 0xC0, 0xC0));
	hOldPen = (HPEN)::SelectObject(hDC, hPen);
	int nPos = rect.top;
	while (nPos < rect.bottom)
	{
		MoveToEx(hDC, rect.left, nPos, NULL);
		if (nPos == rect.top)
			MoveToEx(hDC, rect.left + (rect.right - rect.left)/4, nPos, NULL);	// Frist line.
		if (nPos + nSpacing >= rect.bottom)
			LineTo(hDC, rect.right - (rect.right - rect.left)/4, nPos);	// Last line.
		else
			LineTo(hDC, rect.right, nPos);
		nPos += nSpacing;
	}
	SelectObject(hDC, hOldPen);
	DeleteObject(hPen);
	return;
}


/***********************************************************************************************
	BorderDialogPara implementation
***********************************************************************************************/

FmtBorderParaDlg::FmtBorderParaDlg()
	: FmtBorderDlg()
{
	m_fBorders &= kfBox;	// Ensure spurious grid bits are not set for paragraph.
}

FmtBorderParaDlg::~FmtBorderParaDlg()
{
}


/*----------------------------------------------------------------------------------------------
	Draws the preview diagram for the border paragraph dialog box.
	Note that FmtBorderDlg::DrawLines is called from both derived classes.
----------------------------------------------------------------------------------------------*/
void FmtBorderParaDlg::DrawDiagram(HDC hDC, HWND hwndItem, RECT rcItem)
{
	// First paint the rectangle white.
	int nWidth = rcItem.right - rcItem.left;
	int nHeight = rcItem.bottom - rcItem.top;
	PatBlt(hDC, rcItem.left, rcItem.top, nWidth, nHeight, WHITENESS);

	// Create a rectangle inset a little from what was passed.
	InflateRect(&rcItem, -nWidth/8, -nWidth/8);
	int nTickLen = nWidth/16;

	// First draw the fixed portion (black corner markers).
	MoveToEx(hDC, rcItem.left - nTickLen, rcItem.top, NULL);
	LineTo(hDC, rcItem.left, rcItem.top);
	LineTo(hDC, rcItem.left, rcItem.top - nTickLen);

	MoveToEx(hDC, rcItem.right + nTickLen, rcItem.top, NULL);
	LineTo(hDC, rcItem.right, rcItem.top);
	LineTo(hDC, rcItem.right, rcItem.top - nTickLen);

	MoveToEx(hDC, rcItem.right + nTickLen, rcItem.bottom, NULL);
	LineTo(hDC, rcItem.right, rcItem.bottom);
	LineTo(hDC, rcItem.right, rcItem.bottom + nTickLen);

	MoveToEx(hDC, rcItem.left - nTickLen, rcItem.bottom, NULL);
	LineTo(hDC, rcItem.left, rcItem.bottom);
	LineTo(hDC, rcItem.left, rcItem.bottom + nTickLen);

	int nPenWidth = (krguWidths[m_nWidth] * nWidth + knPenFactor/2)/knPenFactor;
	// Reduce the rectangle by half the width of the pen.
	InflateRect(&rcItem, -nPenWidth/2, -nPenWidth/2);

	// Now draw the selected borders at the width given by krguWidths[m_nWidth]
	// and in the color given by m_clrBorder.
	HPEN hOldPen;
	HPEN hPen = CreatePen(PS_SOLID, nPenWidth, m_clrBorder);
	hOldPen = (HPEN)SelectObject(hDC, hPen);
	if (m_fBorders & kfTop)
	{
		MoveToEx(hDC, rcItem.left, rcItem.top, NULL);
		LineTo(hDC, rcItem.right, rcItem.top);
	}
	if (m_fBorders & kfRight)
	{
		MoveToEx(hDC, rcItem.right, rcItem.top, NULL);
		LineTo(hDC, rcItem.right, rcItem.bottom);
	}
	if (m_fBorders & kfBottom)
	{
		MoveToEx(hDC, rcItem.right, rcItem.bottom, NULL);
		LineTo(hDC, rcItem.left, rcItem.bottom);
	}
	if (m_fBorders & kfLeft)
	{
		MoveToEx(hDC, rcItem.left, rcItem.bottom, NULL);
		LineTo(hDC, rcItem.left, rcItem.top);
	}

	// Decrease the rectangle size by nTickLen/2 + the width of the borders drawn.
	// Do this all round, regardless of which borders have been drawn.
	nPenWidth += nTickLen/2;
	InflateRect(&rcItem, -nPenWidth, -nPenWidth);

	// Draw grey lines of width nTickLen/2, spaced by nTickLen, inside rcItem.
	DrawLines(hDC, nTickLen/2, nTickLen, rcItem);
	SelectObject(hDC, hOldPen);
	DeleteObject(hPen);
	return;
}

/*----------------------------------------------------------------------------------------------
	Sets check boxes according to m_fBorders.
----------------------------------------------------------------------------------------------*/
void FmtBorderParaDlg::SetCheckBoxes()
{
	uint uState;
	uState = (m_fBorders & kfTop) ? BST_CHECKED : BST_UNCHECKED;
	CheckDlgButton(m_hwnd, IDC_TOP, uState);
	uState = (m_fBorders & kfBottom) ? BST_CHECKED : BST_UNCHECKED;
	CheckDlgButton(m_hwnd, IDC_BOTTOM, uState);
	uState = (m_fBorders & kfLeft) ? BST_CHECKED : BST_UNCHECKED;
	CheckDlgButton(m_hwnd, IDC_LEFT, uState);
	uState = (m_fBorders & kfRight) ? BST_CHECKED : BST_UNCHECKED;
	CheckDlgButton(m_hwnd, IDC_RIGHT, uState);
}


/*----------------------------------------------------------------------------------------------
	Sets button images according to m_fBorders.
----------------------------------------------------------------------------------------------*/
void FmtBorderParaDlg::SetImages()
{
	int nImage;
	HWND hButton;
	nImage = (m_fBorders & kfBox) ? NONE : NONE_S;
	hButton = ::GetDlgItem(m_hwnd, IDC_NONE);
	::SendMessage(hButton, STM_SETIMAGE,
		(WPARAM)IMAGE_BITMAP, (LPARAM)m_rghBitmaps[nImage]);
	nImage = ((m_fBorders & kfBox) == kfBox) ? ALL_S : ALL;
	hButton = ::GetDlgItem(m_hwnd, IDC_ALL);
	::SendMessage(hButton, STM_SETIMAGE,
		(WPARAM)IMAGE_BITMAP, (LPARAM)m_rghBitmaps[nImage]);
}
