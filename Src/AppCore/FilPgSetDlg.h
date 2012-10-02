/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FilPgSetDlg.h
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Header file for the PageSetUp Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FILPGSETDLG_H_INCLUDED
#define FILPGSETDLG_H_INCLUDED


class FilPgSetDlg;

/*----------------------------------------------------------------------------------------------
This class is used in place of TssEdit so we can receive notifications of changes to the text
in the control.
Hungarian: fpste.
----------------------------------------------------------------------------------------------*/

class FilPgSetTssEdit : public TssEdit
{
	typedef TssEdit SuperClass;
public:
	FilPgSetTssEdit()
	{
		m_nEditable = ktptIsEditable;
		m_fShowTags = true;
	}

	void SetParent(FilPgSetDlg * pfrdlg);
	virtual bool OnChange();
	virtual bool OnSetFocus(HWND hwndOld, bool fTbControl = false);
//	void GetOverlay(IVwOverlay ** ppvo);
	virtual void HandleSelectionChange(IVwSelection * pvwsel);

protected:
	FilPgSetDlg * m_pfpsdlg;

//	virtual void AdjustForOverlays(IVwOverlay * pvo);

};
typedef GenSmartPtr<FilPgSetTssEdit> FilPgSetTssEditPtr;


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Page Setup Dialog. It is an abstract class.
	Derived classes are FmtBdrDlgPara and FmtBdrDlgTable.
	Hungarian: psd.
----------------------------------------------------------------------------------------------*/

class FilPgSetDlg : public AfDialogView
{
	typedef AfDialogView SuperClass;

public:
	FilPgSetDlg();
	~FilPgSetDlg();

	// Constants.
	enum
	{
		kPgWLtr = 612000,		//width of letter page
		kPgHLtr = 792000,		//height of letter page
		kPgWLgl = 612000,		//width of legal page
		kPgHLgl = 1008000,		//heigth of legal page
		kPgWA4 = 595274,		//width of A4 page
		kPgHA4 = 841888,		//height of A4 page
		kPgMin = 216000,		//min custom page size
		kPgMax = 2448000,		//max custom page size
		kMarMin = 7200,			//min margin
		kMarMax = 216000,		//max margin
		kMinTxt = 144000,		//min width of text column
		kMinHdrTxtHth = 7200,	//min text height of header to top margin
		kMinFtrTxtHth = 7200,	//min text height of footer to bottom margin
		kEdgeMin = 7200,		//min height to edge of header or footer
		kSpnStpIn = 7200,		//spin control step for inches .1"
		kSpnStpMm = 2835,		//spin control step for mm 1mm
		kSpnStpCm = 2835,		//spin control step for cm	.1cm
		kSpnStpPt = 600,		//spin control step for pt 6pts

		// Default Constants
		kDefnLMarg = 90000,		// Width of Left Margin in units of 1/72000"
		kDefnRMarg = 90000,		// Width of Right Margin in units of 1/72000"
		kDefnTMarg = 72000,		// Height of Top Margin in units of 1/72000"
		kDefnBMarg = 72000,		// Height of Bottom Margin in units of 1/72000"
		kDefnHEdge = 36000,		// Height of Header from edge in units of 1/72000"
		kDefnFEdge = 36000,		// Height of Footer from edge in units of 1/72000"
	};

	void SetDialogValues(int nLMarg, int nRMarg, int nTMarg, int nBMarg, int nHEdge, int nFEdge,
		int nOrient, ITsString * ptssHeader, ITsString * ptssFooter, bool fHeaderOnFirstPage,
		int nPgH, int nPgW, PgSizeType sPgSize, MsrSysType nMsrSys, ITsString * ptssTitle);

	void GetDialogValues(int * pLMarg, int * pRMarg, int * pTMarg, int * pBMarg, int * pHEdge,
		int * pFEdge, POrientType * pOrient, ITsString ** pptssHeader, ITsString ** pptssFooter,
		bool * pfHeaderOnFirstPage, int * pPgH, int * pPgW, PgSizeType * pPgSize);

//todo	virtual void EditBoxChanged(FilPgSetTssEdit * fpste);
	virtual void EditBoxFocus(FilPgSetTssEdit * fpste);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

protected:
	// Member variables.
	PgSizeType m_sPgSize;	// Size of the page.
	MsrSysType m_nMsrSys;	// Measurement system
	int		m_nPgH;			// Height of page in units of 1/72000"
	int		m_nPgW;			// Width page in units of 1/72000"
	int		m_nLMarg;		// Width of Left Margin in units of 1/72000"
	int		m_nRMarg;		// Width of Right Margin in units of 1/72000"
	int		m_nTMarg;		// Height of Top Margin in units of 1/72000"
	int		m_nBMarg;		// Height of Bottom Margin in units of 1/72000"
	int		m_nHEdge;		// Height of Header from edge in units of 1/72000"
	int		m_nFEdge;		// Height of Footer from edge in units of 1/72000"
	int		m_nOrient;		// Orientation of page

	ILgWritingSystemFactoryPtr m_qwsf;

	ITsStringPtr m_qtssTitle;

	ITsStringPtr m_qtssHeader;
	ITsStringPtr m_qtssFooter;
	bool m_fHeaderOnFirstPage; // true if the header should be shown on the first page
	bool m_bLastEditFt;	// true if last edit was footer, false if header

	// The control window for the find what box.
	FilPgSetTssEditPtr m_qteHeader;
	// The control window for the replace with box.
	FilPgSetTssEditPtr m_qteFooter;
	// Whichever of them was last in focus
	FilPgSetTssEditPtr m_qteLastFocus;

	HFONT m_hfontNumber;
	COLORREF m_clrNumber;
	HANDLE m_hndFilPgSetFont;
	HANDLE m_hndFilPgSetPgNum;
	HANDLE m_hndFilPgSetTotPg;
	HANDLE m_hndFilPgSetDate;
	HANDLE m_hndFilPgSetTime;
	HANDLE m_hndFilPgSetTitle;

	bool FDlgProc(uint wm, WPARAM wp, LPARAM lp);

	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	bool OnApply(bool fClose);
	bool OnCancel();
	bool OnFontChange(NMHDR * pnmh, long & lnRet);

	bool OnDeltaSpin(NMHDR * pnmh, long & lnRet);
	bool OnComboChange(NMHDR * pnmh, long & lnRet);
	bool UpdateCtrls();
	void UpdateEditBox(HWND hwndSpin, HWND hwndEdit, int nValue);
	bool ChkCstSize();
	void SetImages();
	void InsertButtonId(HWND hwndct, int stid);
//	void InsertButtonText(HWND hwndct, const achar * psz);
	void InsertButtonText(HWND hwndct, ITsString * ptssText);
};

typedef GenSmartPtr<FilPgSetDlg> FilPgSetDlgPtr;

#endif  // !FILPGSETDLG_H
