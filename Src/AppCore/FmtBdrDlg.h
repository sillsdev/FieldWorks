/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FmtBdrDlg.h
Responsibility: John Landon
Last reviewed: Not yet.

Description:
	Header file for the Paragraph Border Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FMTBDRDLG_H_INCLUDED
#define FMTBDRDLG_H_INCLUDED

class AfStylesDlg;

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Border Dialog. It is an abstract class.
	Derived classes are FmtBdrDlgPara and FmtBdrDlgTable.
	Hungarian: brd.
----------------------------------------------------------------------------------------------*/

class FmtBdrDlg : public AfDialogView
{
	typedef AfDialogView SuperClass;

public:
	FmtBdrDlg(int rid);
	FmtBdrDlg(AfStylesDlg * pafsd, int rid);
	void BasicInit();
	~FmtBdrDlg();

	void SetCanDoRtl(bool f);
	void SetOuterRtl(bool f)
	{
		m_fOuterRtl = f;
	}

	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	void SetDialogValues(COLORREF clrBorder, int fBorders, int mpWidth, int nRtl,
		bool fExplicit = true);
	void GetDialogValues(COLORREF * pclrBorder, int * pfBorders, int * pmpWidth);

	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnDrawChildItem(DRAWITEMSTRUCT * pdis);
	bool OnMeasureChildItem(MEASUREITEMSTRUCT * pmis);
	bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	static bool AdjustTsTextProps(HWND hwnd, bool fCanDoRtl, bool fOuterRtl,
		TtpVec & vpttp, TtpVec & vpttpHard, VwPropsVec &vqvpsSoft);

	// Constants.
	enum
	{
		kfTop = 0x1, kfBottom = 0x2, kfLeading = 0x4, kfTrailing = 0x8,
		kfRows = 0x10, kfCols = 0x20,
		kfBox = kfTop | kfBottom | kfLeading | kfTrailing,
		kfGrid = kfBox | kfRows | kfCols
	};
	enum
	{
		kcWidths = 10,			// Number of different widths of borders.
		knPenFactor = 128000,	// Scaling factor for pen width when drawing borders:
								//   p = width of pen in pixels
								//   m = width of preview diagram in millipoints
								//   b = width of border in points
								//   p = m * b / knPenFactorPen
		kdzpListItem = 24,		// Height of each selection in Width combo box.
		kchbmp = 10,			// Number of bitmaps which may be required.
		kcchPt = 6				// Max length of resource " pt".
	};

	// Bitmap indices.
	enum { NONE, NONE_S, ALL, ALL_S, BOX, BOX_S, GRID, GRID_S, NONET, NONET_S };

	static const int krgmpWidths[kcWidths];
	static const int krgridBitmapIds[kchbmp];
	// Names of width choices as character strings.
	static const achar * krgpszWidths[kcWidths];
	void InitForStyle(ITsTextProps * pttp, ITsTextProps * pttpInherited,
		ParaPropRec & xprOrig, bool fEnable, bool fCanInherit);
	void GetStyleEffects(ITsTextProps *pttpOrig, ITsTextProps ** ppttp);
	static void GetPadInfo(int tpt, int & tptPad, int & dzmpPad);

	void SetInheritance(int xColor, int xBorders)
	{
		m_xColor = xColor;
		m_xBorders = xBorders;
	}

protected:

	// Member variables.
	bool m_fCanDoRtl;		// true if this dialog should deal with the possibility of RTL stuff
	bool m_fOuterRtl;		// true if outer document has an overall right-to-left direction
	AfStylesDlg * m_pafsd;

	COLORREF m_clrBorder;	// The color of the borders.
	int m_grfBorders;		// The borders currently selected.
	int m_impWidth;			// Index (within krguWidths[]) of width of borders; -1 if unspecified
							// or conflicting

	UiColorComboPtr m_qccmb;
	COLORREF m_clrMod;	// for modifying by the combo

	// For inheritance:
	bool m_fCanInherit;
	COLORREF m_clrBorderI;
	int m_grfBordersI;
	int m_impWidthI;
	int m_xColor;
	int m_xBorders;

	int m_nRtl;				// direction of selected paragraphs
	bool m_fSwitchSides;	// if true, right->leading and left->trailing;
							// normally, left->leading and right->trailing

	HBITMAP m_rghbmp[kchbmp];	// Bitmap handles.

	void DrawDiagramInit(HDC hdc, HWND hwndItem, RECT & rcItem,
			int & nTickeLen, int & dzpPenWidth,
			int & dxpHalfWidth, int & dxpHalfHeight);

	// Pure virtual functions.
	virtual void DrawDiagram(HDC hdc, HWND hwndItem, RECT rcItem) = 0;
	virtual void SetCheckBoxes() = 0;
	virtual void SetImages() = 0;

	virtual bool OnHelpInfo(HELPINFO * phi);

	void InitWidthCombo();
	void FillCtls();
	bool ColorForInheritance(WPARAM wp, LPARAM lp, long & lnRet);
	void DrawBorderWidth(DRAWITEMSTRUCT * pdis);
	void SetCanInherit(bool f);
	void InitWidthComboBox();
	void UpdateComboWithInherited(int ctid, NMHDR * pnmh);
	void UpdateWidthCombo();
	void CheckBoxChanged(int ctid);
	void SetExplicitWidth();

	void UpdateLabels(int nRtl);

	int CheckForSide(int iside)
	{
		static const int bits[4] = { kfTop, kfBottom, kfLeading, kfTrailing };
		if (m_xBorders == kxExplicit)
		{
			if (m_grfBorders & bits[iside])
				return BST_CHECKED;
			else
				return BST_UNCHECKED;
		}
		else
			return BST_INDETERMINATE;
	}
	int ImpWidth()
	{
		switch (m_xBorders)
		{
		case kxConflicting:	return -1;
		case kxExplicit:	return m_impWidth;
		case kxInherited:	return m_impWidthI;
		}
		Assert(false);
		return m_impWidth;
	}

	int GrfBorders()
	{
		switch (m_xBorders)
		{
		case kxConflicting:	return 0;
		case kxExplicit:	return m_grfBorders;
		case kxInherited:	return m_grfBordersI;
		}
		Assert(false);
		return m_grfBorders;
	}

	COLORREF ClrBorder()
	{
		switch (this->m_xColor)
		{
		case kxConflicting:	return (COLORREF)knConflicting;
		case kxExplicit:	return m_clrBorder;
		case kxInherited:	return m_clrBorderI;
		}
		Assert(false);
		return m_clrBorder;
	}
};

// Helper functions
void DrawLines(HDC hdc, int dxpWidth, int nSpacing, const RECT & rect);
void UpdateBdrProp(ITsTextProps * pttp, int tpt, ITsPropsBldrPtr & qtpb,
	int nOld, int nNew, int nVarOld, int nVarNew, int nMul, int nDiv);


/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the Border Dialog for paragraphs.
	Hungarian: brdp.
----------------------------------------------------------------------------------------------*/
class FmtBdrDlgPara : public FmtBdrDlg
{
public:
	FmtBdrDlgPara();
	FmtBdrDlgPara(AfStylesDlg * pafsd);

	void DrawDiagram(HDC hdc, HWND hwndItem, RECT rcItem);
	void SetCheckBoxes();
	void SetImages();
};

typedef GenSmartPtr<FmtBdrDlgPara> FmtBdrDlgParaPtr;


/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the Border Dialog for tables.
	Hungarian: brdt
----------------------------------------------------------------------------------------------*/
class FmtBdrDlgTable : public FmtBdrDlg
{
public:
	FmtBdrDlgTable();
	FmtBdrDlgTable(AfStylesDlg * pafsd);

	void DrawDiagram(HDC hdc, HWND hwndItem, RECT rcItem);
	void SetCheckBoxes();
	void SetImages();
};

typedef GenSmartPtr<FmtBdrDlgTable> FmtBdrDlgTablePtr;

#endif  /*FMTBDRDLG_H_INCLUDED*/
