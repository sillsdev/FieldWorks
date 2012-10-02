/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FmtParaDlg.h
Responsibility: Ken Zook
Last reviewed: Not yet.

Description:
Header file for the Format Paragraph Dialog class.

 *	// Sample method for presetting values, calling dialog, and getting results back.
 *	bool RnMainWnd::CmdFmtPara(Cmd * pcmd)
 *	{
 *		AssertObj(pcmd);
 *
 *		int nRtl = false;
 *		FmtParaDlg::AlignmentType atAlignType = FmtParaDlg::katCenter; // Alignment type.
 *																	// Special indentation type.
 *		FmtParaDlg::SpecialIndent spindSpecType = FmtParaDlg::kspindHanging;
 *		FmtParaDlg::LineSpacing lnspSpaceType = FmtParaDlg::klnsp15Lines; // Line spacing type.
 *		COLORREF clrBkgdColor = kclrBrightGreen; // The paragraph's background color.
 *		int thinLeadInd = 250; // (in thousandths of inches).
 *		int thinTrailInd = 250; // (in thousandths of inches).
 *		int thinSpIndBy = 250; // (in thousandths of inches).
 *		int ptSpacBef = 2; // (in points).
 *		int ptSpacAft = 2; // (in points).
 *		int ptLnSpAt = 2; // (in points).
 *
 *		FmtParaDlgPtr qfpar;
 *		qfpar.Create();
 *		// Test some input values.
 *		qfpar->SetDialogValues(nRtl, atAlignType, spindSpIndent, lnspSpaceType,
 *			thinLeadInd, thinTrailInd,
 *			thinSpIndBy, ptSpacBef, ptSpacAft, ptLnSpAt, clrBkgdColor, nMsrSys);
 *
 *		AfDialogShellPtr qdlgs;
 *		qdlgs.Create();
 *		if (qdlgs->CreateDlgShell(qfpar, "Paragraph", m_hwnd) == kctidOk)
 *		{
 *			// Get the output values.
 *			qfpar->GetDialogValues(&nRtl, &atAlignType, &spindSpIndent, &lnspSpaceType,
 *				&thinLeadInd, &thinTrailInd, &thinSpIndBy, &ptSpacBef, &ptSpacAft, &ptLnSpAt,
 *				&clrBkgdColor);
 *		}
 *		return true;
 *	}

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FMT_PARA_DLG_H
#define FMT_PARA_DLG_H 1

typedef ComVector<ITsTextProps> TtpVec;

class AfStylesDlg;

/*----------------------------------------------------------------------------------------------
	Values of the controls in the Paragraph Dialog. It uses two of these: one for the explicit
	values, the other for the inherited ones.
	Hungarian: fpar.
----------------------------------------------------------------------------------------------*/
struct ParaDlgValues
{
	// These have to be ints rather than members of an enumeration, because
	// they could store FmtFntDlg::knConflicting.
	int nRtl;				// paragraph direction
	int atAlignType;		// setting for the alignment type.
	int spindSpIndent;		// special indentation type.
	int lnspSpaceType;		// line spacing type.

	COLORREF clrBkgdColor;	// The paragraph's background color.

	// Properties in spin controls.
	int thinLeadInd;	// leading indent (in thousandths of inches).
	int thinTrailInd;	// trailing indent (in thousandths of inches).
	int thinSpIndBy;		// special indentation (in thousandths of inches).
	int mptSpacBef;		// space before (in millipoints).
	int mptSpacAft;		// space after (in millipoints).
	int mptLnSpAt;		// (in millipoints).
	int mptFontSize;	// (in millipoints).
};

/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Paragraph Dialog. It is an abstract class.
	Derived classes are BorderDialogPara and ParagraphDialogTable.
	Hungarian: fpar.
----------------------------------------------------------------------------------------------*/

class FmtParaDlg : public AfDialogView
{
	typedef AfDialogView SuperClass;

public:
	FmtParaDlg();
	FmtParaDlg(AfStylesDlg * pafsd, MsrSysType nMsrSys);
	void BasicInit();
	void SetCanDoRtl(bool fCanDoRtl);
	void SetOuterRtl(bool f)
	{
		m_fOuterRtl = f;
	}
	void SetMsrSys(MsrSysType nMsrSys)
	{
		m_nMsrSys = nMsrSys;
	}

	// A paragraph may be any one of three possible alignment types.
	enum AlignmentType {katLead=0, katLeft, katCenter, katRight, katTrail, katJust};

	// Possible special indentation types.
	enum SpecialIndent {kspindNone=0, kspindFirstLine, kspindHanging};

	// Possible line spacing types.
	enum LineSpacing {klnspSingle=0, klnsp15Lines, klnspDouble, klnspAtLeast, klnspExact};

	// Indent range (0 to 3 inches)
	enum {kthinIndMin = 0, kthinIndMax = 216000};

	// Spacing range (0 to 500 points, except spacing at 0 to 50 points)
	enum {kmptSpcMin = 0, kmptSpcMax = 50000, kmptSpcAtMin = 0};

		// Spin control step sizes(for inches .1",for mm 1mm,for cm	.1cm,for pt 6pt,for at 1pt)
	enum {kSpnStpIn = 7200, kSpnStpMm = 2835, kSpnStpCm = 2835, kSpnStpPt = 6000,
		kSpnStpAt = 1000};

	// Sets initial values for the dialog controls, prior to displaying the dialog.
	void SetDialogValues(ParaDlgValues & pdv, bool fFillCtls,
		int nRtl, int atAlignType, int spindSpIndent,
		int lnspSpaceType, int thinLeadInd, int thinTrailInd,
		int thinSpIndBy, int mptSpacBef, int mptSpacAft, int mptLnSpAt,
		COLORREF clrBkgdColor, MsrSysType nMsrSys);

	// Retrieve values.
	void GetDialogValues(int * pnRtl, int * patAlignType, int * pspindSpIndent,
		int * plnspSpaceType, int * pthinLeadInd, int * pthinTrailInd,
		int * pthinSpIndBy, int * pmptSpacBef, int * pmptSpacAft, int * pmptLnSpAt,
		COLORREF * pclrBkgdColor);

	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	// TODO ???(SteveMc): by version N, pass in the actual value for mptFontSize, remove
	// the default value.
	static bool AdjustTsTextProps(HWND hwnd, bool fCanDoRtl, bool fOuterRtl,
		TtpVec & vpttp, TtpVec & vpttpHard, VwPropsVec & vqvpsSoft, MsrSysType nMsrSys,
		int mptFontSize = 10000);
	void GetStyleEffects(ParaPropRec xprOrig, ITsTextProps *pttpOrig,
		ITsTextProps ** ppttp);
	void InitForStyle(ITsTextProps * pttp, ITsTextProps * pttpInherited,
		ParaPropRec & xprOrig, bool fEnable, bool fCanInherit);
	static int ComputeIndentIn(int & mpSpIndBy);
	static void ComputeIndentOut(int & mpSpIndBy, int spindSpIndentNew);
	static int ConvertLineHeightIn(int nLineHeightVal, int nLineHeightVar,
		int mpFontSize, int & mpLnHtAdjusted);
	static void ConvertLineHeightOut(ParaPropRec & xprCur, ParaPropRec & xprNew);

protected:
	MsrSysType m_nMsrSys;	// Measurement system to use in the dialog.
	bool m_fCanDoRtl;		// true if this is the kind of dialog that offers right-to-left
	bool m_fAlignLeadTrail;	// true if Leading and Trailing are offered in the Alignment box
	bool m_fOuterRtl;		// true if outer document has an overall right-to-left direction
	AfStylesDlg * m_pafsd;

	// Attributes.
	// These variables have to be ints rather than the appropriate enumeration,
	// because they could store FmtFntDlg::knConflicting.
//	int m_nRtl;				// Current paragraph direction
//	int m_atAlignType;		// Current setting for the alignment type.
//	int m_spindSpIndent;	// Current special indentation type.
//	int m_lnspSpaceType;		// Current line spacing type.
//	COLORREF m_clrBkgdColor;	// The paragraph's background color.

	// Properties in spin controls.
//	int m_thinLeadInd; // (in thousandths of inches).
//	int m_thinTrailInd; // (in thousandths of inches).
//	int m_thinSpIndBy; // (in thousandths of inches).
//	int m_mptSpacBef; // (in millipoints).
//	int m_mptSpacAft; // (in millipoints).
//	int m_mptLnSpAt; // (in millipoints).
//	int m_mptFontSize; // (in millipoints).

	// True if inherited values should show up as gray, and setting to empty reverts to
	// inherited value:
	bool m_fCanInherit;

	// For storing whether or not the values are inherited or explicit.
	ParaPropRec m_xprExpl;
	// For renembering whether to use knConflicting or knUnspecified for edit boxes.
	ParaPropRec m_xprExplOrig;

	ParaDlgValues m_pdvE;	// explicit
	ParaDlgValues m_pdvI;	// inherited

	UiColorComboPtr m_qccmb;	// The Background color combobox.

	// For updating edit-boxes and spin controls:
	bool m_fCurrCtlMod;		// has the current value been modified?
	int m_kctidUpdateCtl;	// control we are in the process of updating
	bool m_fUpdateCtlEmpty;	// true if kctidUpdateCtl has been cleared (returned to inherited value)

	// The message EN_CHANGE traps changes in an edittext box. When an edittext box is
	// being initialized, this message is received by this dialog. However, the text in the
	// edittext box is not really being changed. To differentiate initialization from actual
	// changes in the text of an edittext box, the member variable fChangingEditCtrl is used.
	// The member variable associated with the edittext box should be changed only when
	// fChangingEditCtrl is true. (See FillCtls() and OnEditCtrlChange()).
	bool m_fChangingEditCtrl;

	// Initialize the various dialog controls prior to displaying the dialog.
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	void InitAlignmentCtls();
	void InitSpecIndentCtls();
	void InitLineSpacingCtls();
	void SetCanInherit(bool f);
	bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	bool OnDeltaSpin(NMHDR * pnmh, long & lnRet);
	bool OnComboChange(NMHDR * pnmh, long & lnRet);
	bool OnButtonClick(NMHDR * pnmh, long & lnRet);
	bool OnEditCtrlChange(int ctidFrom);
	bool OnDrawChildItem(DRAWITEMSTRUCT * pdis);
	void OnPaintAlignLine(bool fRtl, int &xsLeft, int &xsRight, int dxsShortenBy);
	void SetDialogInheritance(ParaPropRec & xpr);
	bool ColorForInheritance(WPARAM wp, LPARAM lp, long & lnRet);
	void UpdateComboWithInherited(int ctid, NMHDR * pnmh);
	void FillCtls(void);
	void UpdateLabels();

	void UpdatePreview(DRAWITEMSTRUCT * pdis);

	virtual bool OnHelpInfo(HELPINFO * phi);

	// The current values of the dialog, either explicit or inherited.
	int NRtl()
	{
		switch (m_xprExpl.xRtl)
		{
		case kxConflicting: return knConflicting;
		case kxExplicit:	return m_pdvE.nRtl;
		case kxInherited:	return m_pdvI.nRtl;
		}
		Assert(false);
		return m_pdvE.nRtl;
	}
	int AtAlignType()
	{
		switch (m_xprExpl.xAlign)
		{
		case kxConflicting: return knConflicting;
		case kxExplicit:	return m_pdvE.atAlignType;
		case kxInherited:	return m_pdvI.atAlignType;
		}
		Assert(false);
		return m_pdvE.atAlignType;
	}
	COLORREF ClrBkgdColor()
	{
		COLORREF clrRet;
		switch (m_xprExpl.xBkgdColor)
		{
		case kxConflicting:
			clrRet = (COLORREF)knConflicting;
			break;
		case kxExplicit:
			clrRet = m_pdvE.clrBkgdColor;
			break;
		case kxInherited:
			clrRet = m_pdvI.clrBkgdColor;
			break;
		default:
			Assert(false);
			clrRet = m_pdvE.clrBkgdColor;
		}
		return clrRet;
	}
	int ThinLeadInd()
	{
		switch (m_xprExpl.xLeadInd)
		{
		case kxConflicting: return knConflicting;
		case kxExplicit:	return m_pdvE.thinLeadInd;
		case kxInherited:	return m_pdvI.thinLeadInd;
		}
		Assert(false);
		return m_pdvE.thinLeadInd;
	}
	int ThinTrailInd()
	{
		switch (m_xprExpl.xTrailInd)
		{
		case kxConflicting: return knConflicting;
		case kxExplicit:	return m_pdvE.thinTrailInd;
		case kxInherited:	return m_pdvI.thinTrailInd;
		}
		Assert(false);
		return m_pdvE.thinTrailInd;
	}
	int ThinSpIndBy()
	{
		switch (m_xprExpl.xSpInd)
		{
		case kxConflicting: return knConflicting;
		case kxExplicit:	return m_pdvE.thinSpIndBy;
		case kxInherited:	return m_pdvI.thinSpIndBy;
		}
		Assert(false);
		return m_pdvE.thinSpIndBy;
	}
	int SpIndent()
	{
		switch (m_xprExpl.xSpInd)
		{
		case kxConflicting: return knConflicting;
		case kxExplicit:	return m_pdvE.spindSpIndent;
		case kxInherited:	return m_pdvI.spindSpIndent;
		}
		Assert(false);
		return m_pdvE.spindSpIndent;
	}
	int MptSpacBef()
	{
		switch (m_xprExpl.xSpacBef)
		{
		case kxConflicting: return knConflicting;
		case kxExplicit:	return m_pdvE.mptSpacBef;
		case kxInherited:	return m_pdvI.mptSpacBef;
		}
		Assert(false);
		return m_pdvE.mptSpacBef;
	}
	int MptSpacAft()
	{
		switch (m_xprExpl.xSpacAft)
		{
		case kxConflicting: return knConflicting;
		case kxExplicit:	return m_pdvE.mptSpacAft;
		case kxInherited:	return m_pdvI.mptSpacAft;
		}
		Assert(false);
		return m_pdvE.mptSpacAft;
	}
	int MptLnSpAt()
	{
		switch (m_xprExpl.xLnSp)
		{
		case kxConflicting: return knConflicting;
		case kxExplicit:	return m_pdvE.mptLnSpAt;
		case kxInherited:	return m_pdvI.mptLnSpAt;
		}
		Assert(false);
		return m_pdvE.mptLnSpAt;
	}
	int LineSpaceType()
	{
		switch (m_xprExpl.xLnSp)
		{
		case kxConflicting: return knConflicting;
		case kxExplicit:	return m_pdvE.lnspSpaceType;
		case kxInherited:	return m_pdvI.lnspSpaceType;
		}
		Assert(false);
		return m_pdvE.lnspSpaceType;
	}
//	int MptFontSize()
//	{
//		switch (m_xprExpl.x??)
//		{
//		case kxConflicting: return knConflicting;
//		case kxExplicit:	return m_pdvE.mptFontSize;
//		case kxInherited:	return m_pdvI.mptFontSize;
//		}
//		Assert(false);
//		return m_pdvE.mptFontSize;
//	}
};

typedef GenSmartPtr<FmtParaDlg> FmtParaDlgPtr;

#endif // !FMT_PARA_DLG_H
