/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FmtParaDlg.cpp
Responsibility: Ken Zook
Last reviewed: Not yet.

Description:
	Implementation of the Format Paragraph Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


/***********************************************************************************************
	Local functions; not class methods.
***********************************************************************************************/

inline int NBoundOrConf(int n, int nMin, int nMax)
{
	if (n == FwStyledText::knConflicting)
		return n;
	else
		return NBound(n, nMin, nMax);
}

/*----------------------------------------------------------------------------------------------
	Converts nVal if it is not knConflicting.
----------------------------------------------------------------------------------------------*/
void ConvertIfSpec(int &nVal, int nMul, int nDiv)
{
	if (nVal != FwStyledText::knConflicting && nVal != FwStyledText::knUnspecified)
		nVal = MulDiv(nVal, nMul, nDiv);
}

/*----------------------------------------------------------------------------------------------
	Return true if the values are considered equal for the purpose of updating the
	properties.
----------------------------------------------------------------------------------------------*/
bool ResultsEq(int nOld, int nNew, bool fConfToUnspec)
{
	if (nOld == nNew)
		return true;
	if (nOld == FwStyledText::knUnspecified && nNew == FwStyledText::knConflicting)
		return true;
	if (fConfToUnspec)	// it make sense to change from conflicting to unspecified--only true
		;				// of combo-boxes; otherwise treat them as equivalent
	else if (nOld == FwStyledText::knConflicting && nNew == FwStyledText::knUnspecified)
		return true;

	return false;
}

/*----------------------------------------------------------------------------------------------
	pttp is an old props value, which may get replaced by the ttp from the props builder.
	If the old and new values and variations are the same, do nothing.
	If the new value is FwStyledText::knUnspecified, delete the property.
	Otherwise, compute the new value of the property as nNew * nMul / nDiv, and (if that is
	not already its value), create a builder if necessary and set the value.
	Note: allow for the possibility that pttp is null.
----------------------------------------------------------------------------------------------*/
void UpdateParaProp(ITsTextProps * pttp, int tpt, ITsPropsBldrPtr & qtpb,
	int nOld, int nNew, int nVarOld, int nVarNew, int nMul, int nDiv,
	bool fConfToUnspec)
{
	AssertPtrN(pttp);
	// If this property has not changed, do nothing
	if (ResultsEq(nOld, nNew, fConfToUnspec))
	{
		if (ResultsEq(nVarOld, nVarNew, false))
			return;
		if (nOld == FwStyledText::knUnspecified && nNew == FwStyledText::knConflicting)
			return;
	}

	int nCur = nNew;
	if (nVarNew == ktpvMilliPoint &&
		nNew != FwStyledText::knUnspecified && nNew != FwStyledText::knConflicting)
	{
		nCur = MulDiv(nNew, nMul, nDiv);
	}
	int nVar = -1; // In case pttp is null, same result as if prop not found
	int nVal = -1;
	HRESULT hr = S_FALSE;
	if (pttp)
		CheckHr(hr = pttp->GetIntPropValues(tpt, &nVar, &nVal));
	// If it was and is unspecified, do nothing.
	if (hr == S_FALSE && (nCur == FwStyledText::knUnspecified ||
			nCur == FwStyledText::knConflicting))
	{
		return;
	}
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
	// If the new value is "inherited", delete the prop; otherwise set the new val.
	if (nCur == FwStyledText::knUnspecified || nCur == FwStyledText::knConflicting)
		CheckHr(qtpb->SetIntPropValues(tpt, -1, -1));
	else
		CheckHr(qtpb->SetIntPropValues(tpt, nVarNew, nCur));
}

/*----------------------------------------------------------------------------------------------
	Return an ITsTextProps corresponding to the given paragraph-property records.
	ENHANCE: JohnT: write a better comment explaining the interaction between the arguments.
----------------------------------------------------------------------------------------------*/
void ComputeParaChanges(ParaPropRec & xprOrig, ParaPropRec & xprNew, ITsTextProps * pttp,
	ITsTextProps ** ppttp)
{
	// ENHANCE JohnT: Explain why shouldn't spindSpIndent and lnspSpaceType be set for xprNew?
	// It seems to work as is.
	Assert(!*ppttp);
	ITsPropsBldrPtr qtpb;
	UpdateParaProp(pttp, ktptRightToLeft, qtpb, xprOrig.nRtl,
		xprNew.nRtl, ktpvEnum, ktpvEnum, 1, 1, false);
	UpdateParaProp(pttp, ktptAlign, qtpb, xprOrig.atAlignType,
		xprNew.atAlignType, ktpvEnum, ktpvEnum, 1, 1, true);
	UpdateParaProp(pttp, ktptLeadingIndent, qtpb, xprOrig.mpLeadInd,
		xprNew.mpLeadInd, ktpvMilliPoint, ktpvMilliPoint, kdzmpInch, kdzptInch * 1000, false);
	UpdateParaProp(pttp, ktptTrailingIndent, qtpb, xprOrig.mpTrailInd,
		xprNew.mpTrailInd, ktpvMilliPoint, ktpvMilliPoint, kdzmpInch, kdzptInch * 1000, false);
	UpdateParaProp(pttp, ktptFirstIndent, qtpb, xprOrig.mpSpIndBy,
		xprNew.mpSpIndBy, ktpvMilliPoint, ktpvMilliPoint, kdzmpInch, kdzptInch * 1000, true);
	UpdateParaProp(pttp, ktptSpaceBefore, qtpb, xprOrig.mpSpacBef,
		xprNew.mpSpacBef, ktpvMilliPoint, ktpvMilliPoint, 1, 1, false);
	UpdateParaProp(pttp, ktptSpaceAfter, qtpb, xprOrig.mpSpacAft,
		xprNew.mpSpacAft, ktpvMilliPoint, ktpvMilliPoint, 1, 1, false);
	UpdateParaProp(pttp, ktptLineHeight, qtpb, xprOrig.mpLineHeightVal,
		xprNew.mpLineHeightVal, xprOrig.nLineHeightVar, xprNew.nLineHeightVar, 1, 1, true);
	UpdateParaProp(pttp, ktptBackColor, qtpb, xprOrig.clrBkgdColor,
		xprNew.clrBkgdColor, ktpvDefault, ktpvDefault, 1, 1, false);
	// If anything changed, we now have a props builder that is the new value for this run
	if (qtpb)
	{
		CheckHr(qtpb->GetTextProps(ppttp));
	}
}


/***********************************************************************************************
	Public class methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructors.
----------------------------------------------------------------------------------------------*/
FmtParaDlg::FmtParaDlg()
{
	BasicInit();
	m_fCanInherit = true;
	m_pszHelpUrl = _T("User_Interface/Menus/Format/Paragraph.htm");
	m_nMsrSys = kninches;
	m_pafsd = NULL;
}

FmtParaDlg::FmtParaDlg(AfStylesDlg * pafsd, MsrSysType nMsrSys)
{
	BasicInit();
	m_pafsd = pafsd;
	if (pafsd)
	{
		SetCanDoRtl(pafsd->CanDoRtl());
		SetOuterRtl(pafsd->OuterRtl());
		m_pszHelpUrl = _T("User_Interface/Menus/Format/Style/Style_Paragraph_tab.htm");
	}
	else
	{
		m_pszHelpUrl = _T("User_Interface/Menus/Format/Paragraph.htm");
	}
	m_nMsrSys = nMsrSys;
	m_fCanInherit = true;
}

void FmtParaDlg::BasicInit()
{
	m_rid = kridFmtParaDlg;
	m_pdvE.clrBkgdColor = kclrYellow; // Default background color.
	m_pdvE.mptFontSize = 10000;	// Default font size used in the paragraph.
	// TODO ???(SteveMc): by version N, pass in the actual value for m_mptFontSize.
	m_fChangingEditCtrl = true; // Change to false during initialization of edittext boxes.
	m_fCanDoRtl = false;
	m_fAlignLeadTrail = m_fCanDoRtl;
	m_fCanInherit = false;

	m_kctidUpdateCtl = 0;
	m_fCurrCtlMod = false;
}

/*----------------------------------------------------------------------------------------------
	Set the flags indicating what version of the dialog we want.
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::SetCanDoRtl(bool fCanDoRtl)
{
	m_fCanDoRtl = fCanDoRtl;
	m_fAlignLeadTrail = m_fCanDoRtl;
	m_rid = (m_fCanDoRtl) ? kridFmtParaDlgRtl : kridFmtParaDlg;
}

/*----------------------------------------------------------------------------------------------
	Initialize the format para dialog with a sequence of TsTextProps (as obtained typically from
	the StyleRules prop of each para object). Run the dialog, and adjust prgpttp to be the new
	new property values, or nulls for those that don't change. Return true if a change is
	is needed.

	vpttp and vpttpHard are identical except that the vpttpHard members have had
	the named style removed. Thus vpttp is what should be used in generating the new version
	of the properties, while vpttpHard is what is used to fill the controls. vqvpsSoft is used
	to show the inherited or soft values. Review (SharonC): Really we shouldn't have to pass
	in vpttpHard, since the only difference is the named style, and we would ignore that
	anyway. We could get by with just vpttp.
----------------------------------------------------------------------------------------------*/
bool FmtParaDlg::AdjustTsTextProps(HWND hwnd, bool fCanDoRtl, bool fOuterRtl, TtpVec & vpttp,
	TtpVec & vpttpHard, VwPropsVec &vqvpsSoft, MsrSysType nMsrSys, int mptFontSize)
{
	int cttp = vpttpHard.Size();
	Assert(cttp == vqvpsSoft.Size());

	// These two sets are identical except that in places where chrpOld has
	// FwStyledText::knUnspecified, xprCur has a value taken from the vps.
	ParaPropRec xprOrig;
	ParaPropRec xprCur;

	xprOrig.nRtl =  FwStyledText::knUnspecified;
	xprOrig.atAlignType = FwStyledText::knUnspecified;
	xprOrig.mpLeadInd = FwStyledText::knUnspecified;
	xprOrig.mpTrailInd =  FwStyledText::knUnspecified;
	xprOrig.mpLineHeightVal = FwStyledText::knUnspecified;
	xprOrig.nLineHeightVar =  FwStyledText::knUnspecified;
	xprOrig.mpSpacBef =  FwStyledText::knUnspecified;
	xprOrig.lnspSpaceType =  FwStyledText::knUnspecified;
	xprOrig.mpSpacAft =  FwStyledText::knUnspecified;
	xprOrig.mpSpIndBy =  FwStyledText::knUnspecified;
	xprOrig.spindSpIndent =  FwStyledText::knUnspecified;
	xprOrig.clrBkgdColor = FwStyledText::knUnspecified;

	xprCur.xRtl = kxInherited;
	xprCur.xAlign = kxInherited;
	xprCur.xLeadInd = kxInherited;
	xprCur.xTrailInd = kxInherited;
	xprCur.xLnSp = kxInherited;
	xprCur.xSpacBef = kxInherited;
	xprCur.xSpacAft = kxInherited;
	xprCur.xSpInd = kxInherited;
	xprCur.xBkgdColor = kxInherited;

	// Load a value for each property for each ttp/vps. If we get a different answer for any
	// of them, change to conflicting.
	for (int nHard = 0; nHard <= 1; nHard++)
	{
		bool fHard = (bool)nHard;
		int ittp;
		for (ittp = 0; ittp < cttp; ittp++)
		{
			// When asking for inherited values, use just the property store.
			ITsTextProps * pttp = (fHard) ? vpttpHard[ittp] : NULL;
			// When asking for explicit values, use just the text properties.
			IVwPropertyStore * pvps = (fHard) ? NULL : vqvpsSoft[ittp];

			// ENHANCE JohnT: Explain: Do you not need to do anything about spindSpIndent
			// and lnspSpaceType?
			bool fFirst = (ittp == 0);

			MergeFmtDlgIntProp(pttp, pvps, ktptRightToLeft, ktpvEnum,
				xprOrig.nRtl, xprCur.nRtl, fFirst, xprCur.xRtl, fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptAlign, ktpvEnum,
				xprOrig.atAlignType, xprCur.atAlignType, fFirst, xprCur.xAlign, fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptLeadingIndent, ktpvMilliPoint,
				xprOrig.mpLeadInd, xprCur.mpLeadInd, fFirst, xprCur.xLeadInd, fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptTrailingIndent, ktpvMilliPoint,
				xprOrig.mpTrailInd, xprCur.mpTrailInd, fFirst, xprCur.xTrailInd, fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptFirstIndent, ktpvMilliPoint,
				xprOrig.mpSpIndBy, xprCur.mpSpIndBy, fFirst, xprCur.xSpInd, fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptSpaceBefore, ktpvMilliPoint,
				xprOrig.mpSpacBef, xprCur.mpSpacBef, fFirst, xprCur.xSpacBef, fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptSpaceAfter, ktpvMilliPoint,
				xprOrig.mpSpacAft, xprCur.mpSpacAft, fFirst, xprCur.xSpacAft, fHard);

			MergeFmtDlgIntProp(pttp, pvps, ktptLineHeight, ktpvDefault,
				xprOrig.mpLineHeightVal, xprCur.mpLineHeightVal,
				xprOrig.nLineHeightVar, xprCur.nLineHeightVar,
				fFirst, xprCur.xLnSp, fHard, false, true);

			MergeFmtDlgIntProp(pttp, pvps, ktptBackColor, ktpvDefault,
				xprOrig.clrBkgdColor, xprCur.clrBkgdColor, fFirst, xprCur.xBkgdColor, fHard);
		}
	}

	// Convert any values that are numbers and not unspecified to the appropriate unit.
	// Convert these dimensions to thousandths of an inch.
	ConvertIfSpec(xprOrig.mpLeadInd, kdzptInch * 1000, kdzmpInch);
	ConvertIfSpec(xprOrig.mpTrailInd, kdzptInch * 1000, kdzmpInch);
	ConvertIfSpec(xprOrig.mpSpIndBy, kdzptInch * 1000, kdzmpInch);

	ConvertIfSpec(xprCur.mpLeadInd, kdzptInch * 1000, kdzmpInch);
	ConvertIfSpec(xprCur.mpTrailInd, kdzptInch * 1000, kdzmpInch);
	ConvertIfSpec(xprCur.mpSpIndBy, kdzptInch * 1000, kdzmpInch);
	// Convert these to points.
#if 0
	// Actually, the routine above takes millipoints.
	ConvertIfSpec(xprCur.mpSpacBef, 1, 1);
	ConvertIfSpec(xprCur.mpSpacAft, 1, 1);
	if (xprCur.nLineHeightVar == ktpvMilliPoint)
		ConvertIfSpec(xprCur.mpLineHeightVal, 1, 1);
#endif

	xprOrig.spindSpIndent = ComputeIndentIn(xprOrig.mpSpIndBy);
	xprCur.spindSpIndent = ComputeIndentIn(xprCur.mpSpIndBy);

	int mpLnHtAdjustedOrig, mpLnHtAdjustedCur; // adjusted to relative, if necessary
	xprOrig.lnspSpaceType = ConvertLineHeightIn(xprOrig.mpLineHeightVal, xprOrig.nLineHeightVar,
		mptFontSize, mpLnHtAdjustedOrig);
	xprCur.lnspSpaceType = ConvertLineHeightIn(xprCur.mpLineHeightVal, xprCur.nLineHeightVar,
		mptFontSize, mpLnHtAdjustedCur);

	// Now make an instance, initialize it, and run it.
	FmtParaDlgPtr qfpar;
	qfpar.Create();
	qfpar->SetCanDoRtl(fCanDoRtl);
	qfpar->SetOuterRtl(fOuterRtl);

	// First set the inherited values and then the explicit values.
	qfpar->SetDialogValues(qfpar->m_pdvI, false,
		xprCur.nRtl, xprCur.atAlignType,
		xprCur.spindSpIndent, xprCur.lnspSpaceType,
		xprCur.mpLeadInd, xprCur.mpTrailInd, xprCur.mpSpIndBy, xprCur.mpSpacBef, xprCur.mpSpacAft,
		mpLnHtAdjustedCur, xprCur.clrBkgdColor, nMsrSys);

	qfpar->SetDialogValues(qfpar->m_pdvE, true,
		xprOrig.nRtl, xprOrig.atAlignType,
		xprOrig.spindSpIndent, xprOrig.lnspSpaceType,
		xprOrig.mpLeadInd, xprOrig.mpTrailInd, xprOrig.mpSpIndBy, xprOrig.mpSpacBef, xprOrig.mpSpacAft,
		mpLnHtAdjustedOrig, xprOrig.clrBkgdColor, nMsrSys);

	qfpar->SetDialogInheritance(xprCur);

	AfDialogShellPtr qdlgs;
	qdlgs.Create();
	StrApp str(kstidParagraph);
	if (qdlgs->CreateDlgShell(qfpar, str.Chars(), hwnd) != kctidOk)
		return false; // We changed nothing.

	ParaPropRec xprNew;

	// Get the output values.
	Assert(sizeof(unsigned long) == sizeof(int));
	qfpar->GetDialogValues(&xprNew.nRtl, &xprNew.atAlignType,
		&xprNew.spindSpIndent, &xprNew.lnspSpaceType,
		&xprNew.mpLeadInd, &xprNew.mpTrailInd, &xprNew.mpSpIndBy, &xprNew.mpSpacBef,
		&xprNew.mpSpacAft, &xprNew.mpLineHeightVal, (unsigned long *)(&xprNew.clrBkgdColor));

	// For special indent, combine the type with the value.
	ComputeIndentOut(xprNew.mpSpIndBy, xprNew.spindSpIndent);

	// Restore original mpSpIndBy for comparison in doing update.
	if (xprOrig.spindSpIndent == kspindHanging)
		xprOrig.mpSpIndBy = - xprOrig.mpSpIndBy;

	ConvertLineHeightOut(xprOrig, xprNew);

	// For each property, if new = current, copy old to new: this ensures there is no spurious
	// change produced by rounding errors in unit conversions. Otherwise, convert new to the
	// appropriate value for a ttp.

	ITsPropsBldrPtr qtpb;

	bool fChanged = false;
	// Now see what changes we have to deal with.
	for (int ittp = 0; ittp < cttp; ittp++)
	{
		ITsTextProps * pttp = vpttp[ittp];
		ComputeParaChanges(xprOrig, xprNew, pttp, &vpttp[ittp]);
		if (vpttp[ittp])
		{
			fChanged = true;
		}
	}
	return fChanged;
} // FmtParaDlg::AdjustTsTextProps.

/*----------------------------------------------------------------------------------------------
	Initialize the format para dialog for editing a style represented as a TsTextProps.
	Any property not specified in the pttp is considered conflicting.
	Fill in all values in xprOrig for use when retrieving values.

	@param fCanInherit - false for the Normal style
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::InitForStyle(ITsTextProps * pttp, ITsTextProps * pttpInherited,
	ParaPropRec & xprOrig, bool fEnable, bool fCanInherit)
{
	SetCanInherit(fCanInherit);

	// Disable the direction control if the selected style is not a paragraph style.
	if (m_fCanDoRtl)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidFpCbDirection), fEnable);
	}
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpCbAlign), fEnable); // Alignment combobox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpCbBkgrnd), fEnable); // Background color combo.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpEdIndLft), fEnable); // Left indent editbox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpSpIndLft), fEnable); // Left indent spin ctrl.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpEdIndRt), fEnable); // Right indent editbox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpSpIndRt), fEnable); // Right indent spin ctrl.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpCbSpec), fEnable); // Special combobox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpEdSpIndBy), fEnable); // By editbox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpSpSpIndBy), fEnable); // By spin control.
	::EnableWindow(::GetDlgItem(m_hwnd, kstidFpEdSpIndBy), fEnable); // B&y text.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpEdSpacBef), fEnable); // Space Before editbox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpSpSpacBef), fEnable); // Space Before spin ctrl.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpEdSpacAft), fEnable); // Space After editbox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpSpSpacAft), fEnable); // Space After spin ctrl.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpCbLineSpace), fEnable); // Line Spacing combo.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpEdLineSpaceAt), fEnable); // Ln space At editbox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpSpLineSpaceAt), fEnable); // Ln space At spin ctlr.
	::EnableWindow(::GetDlgItem(m_hwnd, kstidFpEdLsAt), fEnable); // Ln space A&t text.

	xprOrig.nRtl = FwStyledText::knUnspecified;
	xprOrig.atAlignType = FwStyledText::knUnspecified;
	xprOrig.mpLeadInd = FwStyledText::knUnspecified;
	xprOrig.mpTrailInd = FwStyledText::knUnspecified;
	xprOrig.nLineHeightVar = FwStyledText::knUnspecified;
	xprOrig.mpLineHeightVal = FwStyledText::knUnspecified;
	xprOrig.mpSpacBef = FwStyledText::knUnspecified;
	xprOrig.mpSpacAft = FwStyledText::knUnspecified;
	xprOrig.mpSpIndBy = FwStyledText::knUnspecified;
	xprOrig.clrBkgdColor = FwStyledText::knUnspecified;
	xprOrig.spindSpIndent = FwStyledText::knUnspecified;
	xprOrig.lnspSpaceType = FwStyledText::knUnspecified;

	// Initialize the explicit flags to inherited, unless inheritance is disallowed, in which
	// case we treat all values as explicit.
	xprOrig.xRtl = m_fCanInherit ? kxInherited : kxExplicit;
	xprOrig.xAlign = m_fCanInherit ? kxInherited : kxExplicit;
	xprOrig.xLeadInd = m_fCanInherit ? kxInherited : kxExplicit;
	xprOrig.xTrailInd = m_fCanInherit ? kxInherited : kxExplicit;
	xprOrig.xSpInd = m_fCanInherit ? kxInherited : kxExplicit;
	xprOrig.xLnSp = m_fCanInherit ? kxInherited : kxExplicit;
	xprOrig.xSpacBef = m_fCanInherit ? kxInherited : kxExplicit;
	xprOrig.xSpacAft = m_fCanInherit ? kxInherited : kxExplicit;
	xprOrig.xBkgdColor = m_fCanInherit ? kxInherited : kxExplicit;

	// If this is not a paragraph style, make the control values be blank.
	if (!fEnable)
	{
		SetDialogValues(m_pdvE, true,
			xprOrig.nRtl, xprOrig.atAlignType, kspindNone, xprOrig.lnspSpaceType,
			xprOrig.mpLeadInd, xprOrig.mpTrailInd, xprOrig.mpSpIndBy, xprOrig.mpSpacBef,
			xprOrig.mpSpacAft, xprOrig.mpLineHeightVal, xprOrig.clrBkgdColor, m_nMsrSys);
		return;
	}

	for (int nExplicit = 0; nExplicit < 2; nExplicit++)
	{
		ITsTextProps * pttpTmp = (nExplicit == 0) ? pttpInherited : pttp;

		int x = (nExplicit || !m_fCanInherit) ? kxExplicit : kxInherited;

		int cprop;
		CheckHr(pttpTmp->get_IntPropCount(&cprop));
		for (int iprop = 0; iprop < cprop; iprop++)
		{
			int tpt;
			int nVar;
			int nVal;
			CheckHr(pttpTmp->GetIntProp(iprop, &tpt, &nVar, &nVal));
			switch(tpt)
			{
			default: // property not handled in this dialog.
				break;
			case ktptRightToLeft:
				if (nVar == ktpvEnum && nVal != FwStyledText::knUnspecified)
				{
					xprOrig.nRtl = nVal;
					xprOrig.xRtl = x;
				}
				break;
			case ktptAlign:
				if (nVar == ktpvEnum && nVal != FwStyledText::knUnspecified)
				{
					xprOrig.atAlignType = nVal;
					xprOrig.xAlign = x;
				}
				break;
			case ktptLeadingIndent:
				if (nVar == ktpvMilliPoint && nVal != FwStyledText::knUnspecified)
				{
					xprOrig.mpLeadInd = nVal;
					xprOrig.xLeadInd = x;
				}
				break;
			case ktptTrailingIndent:
				if (nVar == ktpvMilliPoint && nVal != FwStyledText::knUnspecified)
				{
					xprOrig.mpTrailInd = nVal;
					xprOrig.xTrailInd = x;
				}
				break;
			case ktptFirstIndent:
				if (nVar == ktpvMilliPoint && nVal != FwStyledText::knUnspecified)
				{
					xprOrig.mpSpIndBy = nVal;
					xprOrig.xSpInd = x;
				}
				break;
			case ktptSpaceBefore:
				if (nVar == ktpvMilliPoint && nVal != FwStyledText::knUnspecified)
				{
					xprOrig.mpSpacBef = nVal;
					xprOrig.xSpacBef = x;
				}
				break;
			case ktptSpaceAfter:
				if (nVar == ktpvMilliPoint && nVal != FwStyledText::knUnspecified)
				{
					xprOrig.mpSpacAft = nVal;
					xprOrig.xSpacAft = x;
				}
				break;
			case ktptLineHeight:
				if (nVal != FwStyledText::knUnspecified)
				{
					xprOrig.mpLineHeightVal = nVal;
					xprOrig.nLineHeightVar = nVar;
					xprOrig.xLnSp = x;
				}
				break;
			case ktptBackColor:
				if (nVar == ktpvDefault && nVal != FwStyledText::knUnspecified)
				{
					xprOrig.clrBkgdColor = nVal;
					xprOrig.xBkgdColor =
						((nExplicit && nVal != kclrTransparent) || !m_fCanInherit) ?
							kxExplicit : kxInherited;
				}
				break;
			}
		}

		// Convert any values in cur that are numbers and not unspecified to the
		// appropriate unit. Convert these dimensions to thousandths of an inch.

		ConvertIfSpec(xprOrig.mpLeadInd, kdzptInch * 1000, kdzmpInch);
		ConvertIfSpec(xprOrig.mpTrailInd, kdzptInch * 1000, kdzmpInch);
		ConvertIfSpec(xprOrig.mpSpIndBy, kdzptInch * 1000, kdzmpInch);

		xprOrig.spindSpIndent = ComputeIndentIn(xprOrig.mpSpIndBy);
		int mpLineHeightVal;
		xprOrig.lnspSpaceType = ConvertLineHeightIn(xprOrig.mpLineHeightVal,
			xprOrig.nLineHeightVar, m_pdvE.mptFontSize, mpLineHeightVal);

		ParaDlgValues * ppdvToSet = (nExplicit || !m_fCanInherit) ? &m_pdvE : &m_pdvI;

		if (nExplicit)
			SetDialogInheritance(xprOrig);

		SetDialogValues(*ppdvToSet, (bool)nExplicit,
			xprOrig.nRtl, xprOrig.atAlignType,
			xprOrig.spindSpIndent, xprOrig.lnspSpaceType,
			xprOrig.mpLeadInd, xprOrig.mpTrailInd, xprOrig.mpSpIndBy, xprOrig.mpSpacBef,
			xprOrig.mpSpacAft, mpLineHeightVal, xprOrig.clrBkgdColor, m_nMsrSys);

		if (!nExplicit && m_fCanInherit)
		{
			// Reinitialize for next round.
			xprOrig.nRtl = FwStyledText::knUnspecified;
			xprOrig.atAlignType = FwStyledText::knUnspecified;
			xprOrig.mpLeadInd = FwStyledText::knUnspecified;
			xprOrig.mpTrailInd = FwStyledText::knUnspecified;
			xprOrig.nLineHeightVar = FwStyledText::knUnspecified;
			xprOrig.mpLineHeightVal = FwStyledText::knUnspecified;
			xprOrig.mpSpacBef = FwStyledText::knUnspecified;
			xprOrig.mpSpacAft = FwStyledText::knUnspecified;
			xprOrig.mpSpIndBy = FwStyledText::knUnspecified;
			xprOrig.clrBkgdColor = FwStyledText::knUnspecified;
			xprOrig.spindSpIndent = FwStyledText::knUnspecified;
			xprOrig.lnspSpaceType = FwStyledText::knUnspecified;
		}
	}

} // FmtParaDlg::InitForStyle.

/*----------------------------------------------------------------------------------------------
	Change the parameter that says whether or not we are showing inheritance in the dialog.
	Adjust the options offered by certain combo-boxes.
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::SetCanInherit(bool fCanInherit)
{
	if (m_fCanInherit != fCanInherit)
	{
		m_fCanInherit = fCanInherit;

		// Clear out the options that are based on whether or not inheritance can happen,
		// and regenerate them.
		HWND hwndAlign = ::GetDlgItem(m_hwnd, kctidFpCbAlign);
		HWND hwndSpec = ::GetDlgItem(m_hwnd, kctidFpCbSpec);
		HWND hwndLnSpc = ::GetDlgItem(m_hwnd, kctidFpCbLineSpace);

		::SendMessage(hwndAlign, CB_RESETCONTENT, 0, 0);
		::SendMessage(hwndSpec, CB_RESETCONTENT, 0, 0);
		::SendMessage(hwndLnSpc, CB_RESETCONTENT, 0, 0);

		InitAlignmentCtls();
		InitSpecIndentCtls();
		InitLineSpacingCtls();
	}
}

/*----------------------------------------------------------------------------------------------
	Sets the initial values for the dialog controls, prior to displaying the dialog. This
	method should be called after creating, but prior to calling DoModal.
	An indent or spacing value that is out of range will be brought in range without complaint.
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::SetDialogValues(ParaDlgValues & pdv, bool fFillCtls,
	int nRtl, int atAlignType,
	int spindSpIndent, int lnspSpaceType, int thinLeadInd,
	int thinTrailInd, int thinSpIndBy, int mptSpacBef, int mptSpacAft,
	int mptLnSpAt, COLORREF clrBkgdColor, MsrSysType nMsrSys = kninches)
{
	Assert(0 == nRtl || 1 == nRtl || FwStyledText::knConflicting == nRtl ||
		FwStyledText::knUnspecified == nRtl);
	Assert(katLead == atAlignType || katLeft == atAlignType || katCenter == atAlignType ||
		katRight == atAlignType || katTrail == atAlignType || katJust == atAlignType ||
		FwStyledText::knConflicting == atAlignType ||
		FwStyledText::knUnspecified == atAlignType);
	Assert(kspindNone == spindSpIndent || kspindHanging == spindSpIndent ||
		kspindFirstLine == spindSpIndent || FwStyledText::knConflicting == spindSpIndent ||
		FwStyledText::knUnspecified == spindSpIndent);
	Assert(klnspSingle == lnspSpaceType || klnsp15Lines == lnspSpaceType ||
		klnspDouble == lnspSpaceType || klnspAtLeast == lnspSpaceType ||
		klnspExact == lnspSpaceType ||
		FwStyledText::knConflicting == lnspSpaceType ||
		FwStyledText::knUnspecified == lnspSpaceType);
	Assert(clrBkgdColor == FwStyledText::knConflicting ||
		clrBkgdColor == FwStyledText::knUnspecified ||
		-1 != g_ct.GetIndexFromColor(clrBkgdColor));

	m_nMsrSys = nMsrSys;
	pdv.nRtl = nRtl;
	pdv.atAlignType = atAlignType;
	if (!m_fCanDoRtl && !m_fAlignLeadTrail)
	{
		// Convert leading to left and trailing to right.
		if (pdv.atAlignType == katLead)
			pdv.atAlignType = katLeft;
		else if (pdv.atAlignType == katTrail)
			pdv.atAlignType = katRight;
	}
	pdv.spindSpIndent = spindSpIndent;
	pdv.lnspSpaceType = lnspSpaceType;
	// Force these values into range.
	pdv.thinLeadInd = NBoundOrConf(thinLeadInd, kthinIndMin, kthinIndMax);
	pdv.thinTrailInd = NBoundOrConf(thinTrailInd, kthinIndMin, kthinIndMax);
	pdv.thinSpIndBy = NBoundOrConf(thinSpIndBy, kthinIndMin, kthinIndMax);
	pdv.mptSpacBef = NBoundOrConf(mptSpacBef, kmptSpcMin, kmptSpcMax);
	pdv.mptSpacAft = NBoundOrConf(mptSpacAft, kmptSpcMin, kmptSpcMax);
	pdv.mptLnSpAt = NBoundOrConf(abs(mptLnSpAt), kmptSpcAtMin, kmptSpcMax);

	pdv.clrBkgdColor = clrBkgdColor;

	if (fFillCtls)
	{
		// Fill the controls with their values.
		FillCtls();

		// Update any labels that are state-sensitive.
		UpdateLabels();
	}
}

/*----------------------------------------------------------------------------------------------
	Record which values are explicit and which are inherited, and update the
	colors of the controls appropriately.
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::SetDialogInheritance(ParaPropRec & xpr)
{
	m_xprExpl.xRtl = xpr.xRtl;
	m_xprExpl.xBkgdColor = xpr.xBkgdColor;
	m_xprExpl.xAlign = xpr.xAlign;
	m_xprExpl.xLeadInd = xpr.xLeadInd;
	m_xprExpl.xTrailInd = xpr.xTrailInd;
	m_xprExpl.xSpInd = xpr.xSpInd;
	m_xprExpl.xSpacBef = xpr.xSpacBef;
	m_xprExpl.xSpacAft = xpr.xSpacAft;
	m_xprExpl.xLnSp = xpr.xLnSp;

	// Remember the original values of these. This is useful so we know whether a blank
	// edit box is supposed to be conflicting or unspecified.
	m_xprExplOrig.xRtl = xpr.xRtl;
	m_xprExplOrig.xBkgdColor = xpr.xBkgdColor;
	m_xprExplOrig.xAlign = xpr.xAlign;
	m_xprExplOrig.xLeadInd = xpr.xLeadInd;
	m_xprExplOrig.xTrailInd = xpr.xTrailInd;
	m_xprExplOrig.xSpInd = xpr.xSpInd;
	m_xprExplOrig.xSpacBef = xpr.xSpacBef;
	m_xprExplOrig.xSpacAft = xpr.xSpacAft;
	m_xprExplOrig.xLnSp = xpr.xLnSp;
}

/*----------------------------------------------------------------------------------------------
	Handle the messages that cause the controls to be recolored based on their state.
----------------------------------------------------------------------------------------------*/
bool FmtParaDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
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
bool FmtParaDlg::ColorForInheritance(WPARAM wp, LPARAM lp, long & lnRet)
{

	HWND hwndAlign = ::GetDlgItem(m_hwnd, kctidFpCbAlign);
	HWND hwndBkClr = ::GetDlgItem(m_hwnd, kctidFpCbBkgrnd);
	HWND hwndLeftInd = ::GetDlgItem(m_hwnd, kctidFpEdIndLft);
	HWND hwndRightInd = ::GetDlgItem(m_hwnd, kctidFpEdIndRt);
	HWND hwndSpec = ::GetDlgItem(m_hwnd, kctidFpCbSpec);
	HWND hwndSpacBef = ::GetDlgItem(m_hwnd, kctidFpEdSpacBef);
	HWND hwndSpacAft = ::GetDlgItem(m_hwnd, kctidFpEdSpacAft);
	HWND hwndLineSpace = ::GetDlgItem(m_hwnd, kctidFpCbLineSpace);

	HWND hwndArg = (HWND)lp;

	bool x;
	if (hwndArg == hwndAlign)
		x = m_xprExpl.xAlign;
	else if (hwndArg == hwndLeftInd)
		x = m_xprExpl.xLeadInd;
	else if (hwndArg == hwndRightInd)
		x = m_xprExpl.xTrailInd;
	else if (hwndArg == hwndSpec || hwndArg == ::GetDlgItem(m_hwnd, kctidFpEdSpIndBy))
		x = m_xprExpl.xSpInd;
	else if (hwndArg == hwndSpacBef)
		x = m_xprExpl.xSpacBef;
	else if (hwndArg == hwndSpacAft)
		x = m_xprExpl.xSpacAft;
	else if (hwndArg == hwndLineSpace || hwndArg == ::GetDlgItem(m_hwnd, kctidFpEdLineSpaceAt))
		x = m_xprExpl.xLnSp;
	else if (hwndArg == hwndBkClr)
	{
		x = m_xprExpl.xBkgdColor;
		if (x == kxExplicit)
			m_qccmb->SetLabelColor(::GetSysColor(COLOR_WINDOWTEXT));
		else
			m_qccmb->SetLabelColor(kclrGray50);
		// I don't know why we return false here, but when we returned true, the control
		// flashed bizarrely. - SharonC
		return false;
	}
	else
		return false;

	if (x == kxExplicit)
		::SetTextColor((HDC)wp, ::GetSysColor(COLOR_WINDOWTEXT));
	else
		::SetTextColor((HDC)wp, kclrGray50);

	::SetBkColor((HDC)wp, ::GetSysColor(COLOR_WINDOW));

	// Send back a brush with which to color the rest of the background:
//	lnRet = (long)CreateSolidBrush(::GetSysColor(COLOR_WINDOW));
//	The above line was commented out for two reasons:  1) we were experiencing a resource leak
//	and could not find the code that deletes the new brush; 2) we could not see the use for
//	returning the brush to the caller.

	lnRet = (long)::GetSysColorBrush(COLOR_WINDOW);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::GetDialogValues(int * pnRtl, int * patAlignType, int * pspindSpIndent,
	int * plnspSpaceType, int * pthinLeadInd, int * pthinTrailInd,
	int * pthinSpIndBy, int * pmptSpacBef, int * pmptSpacAft, int * pmptLnSpAt,
	COLORREF * pclrBkgdColor)
{
	// Because some of the controls seem to forcibly update these variables, only copy
	// if explicit:

	*pnRtl = (m_xprExpl.xRtl == kxExplicit) ? m_pdvE.nRtl : FwStyledText::knUnspecified;
	if (!m_fCanDoRtl && !m_fAlignLeadTrail)
	{
		// Convert left to leading and right to trailing.
		if (m_pdvE.atAlignType == katLeft)
			m_pdvE.atAlignType = katLead;
		else if (m_pdvE.atAlignType == katRight)
			m_pdvE.atAlignType = katTrail;
	}
	*patAlignType = (m_xprExpl.xAlign == kxExplicit) ?
		m_pdvE.atAlignType : FwStyledText::knUnspecified;
	*pspindSpIndent = (m_xprExpl.xSpInd == kxExplicit) ?
		m_pdvE.spindSpIndent : FwStyledText::knUnspecified;
	*pthinSpIndBy = (m_xprExpl.xSpInd == kxExplicit) ?
		m_pdvE.thinSpIndBy : FwStyledText::knUnspecified;
	*plnspSpaceType = (m_xprExpl.xLnSp == kxExplicit) ?
		m_pdvE.lnspSpaceType : FwStyledText::knUnspecified;
	*pmptLnSpAt = (m_xprExpl.xLnSp == kxExplicit) ?
		m_pdvE.mptLnSpAt : FwStyledText::knUnspecified;
	*pthinLeadInd = (m_xprExpl.xLeadInd == kxExplicit) ?
		m_pdvE.thinLeadInd : FwStyledText::knUnspecified;
	*pthinTrailInd = (m_xprExpl.xTrailInd == kxExplicit) ?
		m_pdvE.thinTrailInd : FwStyledText::knUnspecified;
	*pmptSpacBef = (m_xprExpl.xSpacBef == kxExplicit) ?
		m_pdvE.mptSpacBef : FwStyledText::knUnspecified;
	*pmptSpacAft = (m_xprExpl.xSpacAft == kxExplicit) ?
		m_pdvE.mptSpacAft : FwStyledText::knUnspecified;
	*pclrBkgdColor = (m_xprExpl.xBkgdColor == kxExplicit) ?
		m_pdvE.clrBkgdColor : FwStyledText::knUnspecified;

	*patAlignType = (m_xprExpl.xAlign == kxConflicting) ?
		FwStyledText::knConflicting : *patAlignType;
	*pspindSpIndent = (m_xprExpl.xSpInd == kxConflicting) ?
		FwStyledText::knConflicting : *pspindSpIndent;
	*pthinSpIndBy = (m_xprExpl.xSpInd == kxConflicting) ?
		FwStyledText::knConflicting : *pthinSpIndBy;
	*plnspSpaceType = (m_xprExpl.xLnSp == kxConflicting) ?
		FwStyledText::knConflicting : *plnspSpaceType;
	*pmptLnSpAt = (m_xprExpl.xLnSp == kxConflicting) ?
		FwStyledText::knConflicting : *pmptLnSpAt;
	*pthinLeadInd = (m_xprExpl.xLeadInd == kxConflicting) ?
		FwStyledText::knConflicting : *pthinLeadInd;
	*pthinTrailInd = (m_xprExpl.xTrailInd == kxConflicting) ?
		FwStyledText::knConflicting : *pthinTrailInd;
	*pmptSpacBef = (m_xprExpl.xSpacBef == kxConflicting) ?
		FwStyledText::knConflicting : *pmptSpacBef;
	*pmptSpacAft = (m_xprExpl.xSpacAft == kxConflicting) ?
		FwStyledText::knConflicting : *pmptSpacAft;
	*pclrBkgdColor = (m_xprExpl.xBkgdColor == kxConflicting) ?
		FwStyledText::knConflicting : *pclrBkgdColor;

	// Some controls should never change from conflicting to unspecified.
	if (*patAlignType == FwStyledText::knUnspecified &&
		m_xprExplOrig.xAlign == FwStyledText::knConflicting)
	{
		*patAlignType = FwStyledText::knConflicting;
	}
	if (*pthinLeadInd == FwStyledText::knUnspecified &&
		m_xprExplOrig.xLeadInd == FwStyledText::knConflicting)
	{
		*pthinLeadInd = FwStyledText::knConflicting;
	}
	if (*pthinTrailInd == FwStyledText::knUnspecified &&
		m_xprExplOrig.xTrailInd == FwStyledText::knConflicting)
	{
		*pthinTrailInd = FwStyledText::knConflicting;
	}
	if (*pmptSpacBef == FwStyledText::knUnspecified &&
		m_xprExplOrig.xSpacBef == FwStyledText::knConflicting)
	{
		*pmptSpacBef = FwStyledText::knConflicting;
	}
	if (*pmptSpacAft == FwStyledText::knUnspecified &&
		m_xprExplOrig.xSpacAft == FwStyledText::knConflicting)
	{
		*pmptSpacAft = FwStyledText::knConflicting;
	}
	if (*pclrBkgdColor == FwStyledText::knUnspecified &&
		m_xprExplOrig.xBkgdColor == FwStyledText::knConflicting)
	{
		*pclrBkgdColor = (COLORREF)FwStyledText::knConflicting;
	}
}

/*----------------------------------------------------------------------------------------------
	Obtain a new pttp that is the result of retrieving the current dialog state and applying
	it as a change to the original pttp, whose properties were used in creating xprOrig
	using the code above.
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::GetStyleEffects(ParaPropRec xprOrig, ITsTextProps *pttpOrig,
	ITsTextProps ** ppttp)
{
	// Get the output values.
	ParaPropRec xprNew;
	Assert(sizeof(unsigned long) == sizeof(int));
	GetDialogValues(&xprNew.nRtl, &xprNew.atAlignType,
		&xprNew.spindSpIndent, &xprNew.lnspSpaceType,
		&xprNew.mpLeadInd, &xprNew.mpTrailInd, &xprNew.mpSpIndBy, &xprNew.mpSpacBef,
		&xprNew.mpSpacAft, &xprNew.mpLineHeightVal, (unsigned long *)(&xprNew.clrBkgdColor));
	// Make various adjustments to get them comparable to the originals.
	ComputeIndentOut(xprNew.mpSpIndBy, xprNew.spindSpIndent);
	if (xprOrig.spindSpIndent == kspindHanging)
		xprOrig.mpSpIndBy = - xprOrig.mpSpIndBy;
	ConvertLineHeightOut(xprOrig, xprNew);
	// Compare new with old and make a new ttp if needed.
	ComputeParaChanges(xprOrig, xprNew, pttpOrig, ppttp);
}


/*----------------------------------------------------------------------------------------------
	In our properties we have an mpSpIndBy field which indicates special indents: the actual
	distance, and also, if negative, it is hanging.
	For the dialog, we want a positive distance, and an enumeration member indicating
	whether it is hanging, first line, none, or conflicting.
	This routine computes the enumeration and, if necessary, makes the distance positive.
----------------------------------------------------------------------------------------------*/
int FmtParaDlg::ComputeIndentIn(int & mpSpIndBy)
{
	int spindSpIndent = kspindNone;
	if (mpSpIndBy == FwStyledText::knConflicting)
		spindSpIndent = FwStyledText::knConflicting;
	else if (mpSpIndBy == FwStyledText::knUnspecified)
		spindSpIndent = FwStyledText::knUnspecified;
	else if (mpSpIndBy > 0)
		spindSpIndent = kspindFirstLine;
	else if (mpSpIndBy < 0)
	{
		spindSpIndent = kspindHanging;
		mpSpIndBy = -mpSpIndBy;
	}
	return spindSpIndent;
}

/*----------------------------------------------------------------------------------------------
	Perform the reverse conversion, obtaining a property value from enumeration and val.
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::ComputeIndentOut(int & mpSpIndBy, int spindSpIndentNew)
{
	switch (spindSpIndentNew)
	{
	case kspindNone:
		mpSpIndBy = 0;
		break;
	case kspindHanging:
		mpSpIndBy = -mpSpIndBy;
		break;
	case kspindFirstLine:
		break;
	case FwStyledText::knConflicting:
	case FwStyledText::knUnspecified:
		break;
	default:
		Assert(false);
	}
}

/*----------------------------------------------------------------------------------------------
	In our properties we have a value and a variation for ktptLineHeight.
	Relevant variations are ktpvMilliPoint, ktpvEnum, and ktpvRelative.
	For the dialog, we want an enumeration indicating "at least", "exact",
	single, 1.5, or double spacing, and if "at least" or "exact", a distance.
	This routine generates the enumeration value.
	Some information may possibly be lost.
----------------------------------------------------------------------------------------------*/
int FmtParaDlg::ConvertLineHeightIn(int mpLineHeightVal, int nLineHeightVar,
	int mptFontSize, int & mpLnHtAdjusted)
{
	int spctTypeRet;
	if (nLineHeightVar == ktpvMilliPoint)
	{
		if (mpLineHeightVal < 0)
			spctTypeRet = klnspExact;
		else
			spctTypeRet = klnspAtLeast;
	}
	else if (mpLineHeightVal >= kdenTextPropRel * 2)
		spctTypeRet = klnspDouble;
	else if (mpLineHeightVal >= kdenTextPropRel * 3 / 2)
		spctTypeRet = klnsp15Lines;
	else if (mpLineHeightVal == FwStyledText::knConflicting)
		spctTypeRet = FwStyledText::knConflicting;
	else if (mpLineHeightVal == FwStyledText::knUnspecified)
		spctTypeRet = FwStyledText::knUnspecified;
	else
		spctTypeRet = klnspSingle;

	mpLnHtAdjusted = mpLineHeightVal;
	if (nLineHeightVar == ktpvRelative)
	{
		int mpTmp = mpLnHtAdjusted;
		mpTmp /= 5000;
		Assert(mpTmp == 2 || mpTmp == 3 || mpTmp == 4);
		int nSpace = (mptFontSize * 6) / 5;	// Should be something like 12000.
		if (nSpace == mptFontSize)
			++nSpace;
		mpTmp *= nSpace;	// Should be something like 24000, 36000, or 48000.
		mpTmp /= 2;		// Should be something like 12000, 18000, or 24000.

		mpLnHtAdjusted = mpTmp;
	}

	return spctTypeRet;
}

/*----------------------------------------------------------------------------------------------
	Perform the opposite correction from ConvertLineHeightIn; ie, convert the values used
	by the dialog to what is used by the text properties mechanism.
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::ConvertLineHeightOut(ParaPropRec & xprOrig, ParaPropRec & xprNew)
{
	// If the user didn't change anything we make the two variations match. This helps to
	// avoid the consequences of redundancy: many values of the properties could produce
	// the same dialog settings.
	xprNew.nLineHeightVar = xprOrig.nLineHeightVar;
	if (xprOrig.lnspSpaceType != xprNew.lnspSpaceType ||
		xprNew.mpLineHeightVal != xprOrig.mpLineHeightVal)
	{
		// The user changed one or the other; figure effects.
		switch(xprNew.lnspSpaceType)
		{
		default:
			Assert(false);
			break;
		case klnspSingle:
			xprNew.nLineHeightVar = ktpvRelative;
			xprNew.mpLineHeightVal = kdenTextPropRel;
			break;
		case klnsp15Lines:
			xprNew.nLineHeightVar = ktpvRelative;
			xprNew.mpLineHeightVal = kdenTextPropRel * 3 / 2;
			break;
		case klnspDouble:
			xprNew.nLineHeightVar = ktpvRelative;
			xprNew.mpLineHeightVal = kdenTextPropRel * 2;
			break;
		case klnspAtLeast:
			xprNew.nLineHeightVar = ktpvMilliPoint;
			xprNew.mpLineHeightVal = abs(xprNew.mpLineHeightVal);
			break;
		case klnspExact:
			xprNew.nLineHeightVar = ktpvMilliPoint;
			xprNew.mpLineHeightVal = abs(xprNew.mpLineHeightVal) * -1;
			break;
		case FwStyledText::knConflicting:
		case FwStyledText::knUnspecified:
			break;
		}
	}
}


/***********************************************************************************************
	Protected class methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)

	Assumptions:
	- The Alignment Combo Box is NOT sorted; thus we can call SetCurSel(m_atAlignType) and
		select the correct string.
----------------------------------------------------------------------------------------------*/
bool FmtParaDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Initialize values for the Alignment combo box.
	InitAlignmentCtls();

	if (m_fCanDoRtl)
	{
		StrAppBuf strb;

		// Initialize labels on the Indentation controls.
		HWND hwndLeftInd = ::GetDlgItem(m_hwnd, kctidFpLabelIndLft);
		HWND hwndRightInd = ::GetDlgItem(m_hwnd, kctidFpLabelIndRt);
		if (NRtl() == FwStyledText::knConflicting)
		{
			// Rename Left to Leading and Right to Trailing.
			strb.Load(kstidFpLabelIndLeading);
			::SendMessage(hwndLeftInd, WM_SETTEXT, 0, (LPARAM)strb.Chars());
			strb.Load(kstidFpLabelIndTrailing);
			::SendMessage(hwndRightInd, WM_SETTEXT, 0, (LPARAM)strb.Chars());
		}
		else if (NRtl() == 1)
		{
			// All paragraphs are right-to-left: rename Left to Right and Right to Left.
			strb.Load(kstidFpLabelIndRight);
			::SendMessage(hwndLeftInd, WM_SETTEXT, 0, (LPARAM)strb.Chars());
			strb.Load(kstidFpLabelIndLeft);
			::SendMessage(hwndRightInd, WM_SETTEXT, 0, (LPARAM)strb.Chars());
		}
		// otherwise, leave labels as Left and Right.
	}

	// Initialize values for the Special combo box.
	InitSpecIndentCtls();

	// Initialize values for the Line Spacing combo box.
	InitLineSpacingCtls();

	// Initialize the spin controls.
	UDACCEL udAccel;
	udAccel.nSec = 0;

	switch (m_nMsrSys)
	{
	case kninches:
		udAccel.nInc = kSpnStpIn;
		break;
	case knmm:
		udAccel.nInc = kSpnStpMm;
		break;
	case kncm:
		udAccel.nInc = kSpnStpCm;
		break;
	default:
		Assert(false);	// We should never reach this.
	}
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpSpIndLft), UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpSpIndRt), UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpSpSpIndBy), UDM_SETACCEL, 1, (long)&udAccel);

	// indent is 0 to 3 inches.
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpSpIndLft), UDM_SETRANGE32, kthinIndMin,
		kthinIndMax);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpSpIndRt), UDM_SETRANGE32, kthinIndMin,
		kthinIndMax);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpSpSpIndBy), UDM_SETRANGE32, kthinIndMin,
		kthinIndMax);

	udAccel.nInc = kSpnStpPt;
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpSpSpacBef), UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpSpSpacAft), UDM_SETACCEL, 1, (long)&udAccel);

	// Space is 0 to 50 points.
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpSpSpacBef), UDM_SETRANGE32, kmptSpcMin,
		kmptSpcMax);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpSpSpacAft), UDM_SETRANGE32, kmptSpcMin,
		kmptSpcMax);
	udAccel.nInc = kSpnStpAt;
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpSpLineSpaceAt), UDM_SETACCEL, 1, (long)&udAccel);
	// ENHANCE SteveMc: calculate proper min/max pair?
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpSpLineSpaceAt), UDM_SETRANGE32,
		kmptSpcMin, kmptSpcMax);

	// Setup the color "combo box".
	m_qccmb.Create();
	m_qccmb->SubclassButton(::GetDlgItem(m_hwnd, kctidFpCbBkgrnd), &m_pdvE.clrBkgdColor);

	// Fill the controls with their values.
	FillCtls();

	// Update any labels that are state-sensitive.
	UpdateLabels();

	// Turn off visual styles for these controls until all the controls on the dialog
	// can handle them properly (e.g. custom controls).
	HMODULE hmod = ::LoadLibrary(L"uxtheme.dll");
	if (hmod != NULL)
	{
		typedef bool (__stdcall *themeProc)();
		typedef void (__stdcall *SetWindowThemeProc)(HWND, LPTSTR, LPTSTR);
		themeProc pfnb = (themeProc)::GetProcAddress(hmod, "IsAppThemed");
		bool fAppthemed = (pfnb != NULL ? (pfnb)() : false);
		pfnb = (themeProc)::GetProcAddress(hmod, "IsThemeActive");
		bool fThemeActive = (pfnb != NULL ? (pfnb)() : false);
		SetWindowThemeProc pfn = (SetWindowThemeProc)::GetProcAddress(hmod, "SetWindowTheme");

		if (fAppthemed && fThemeActive && pfn != NULL)
		{
			(pfn)(m_hwnd, L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpCbDirection), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpCbAlign), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpEdIndLft), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpSpIndLft), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpEdIndRt), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpSpIndRt), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpCbSpec), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpEdSpIndBy), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpSpSpIndBy), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpEdSpacBef), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpSpSpacBef), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpEdSpacAft), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpSpSpacAft), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpCbLineSpace), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpEdLineSpaceAt), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFpSpLineSpaceAt), L"", L"");
		}

		::FreeLibrary(hmod);
	}

	return true;
} // FmtParaDlg::OnInitDlg.


/*----------------------------------------------------------------------------------------------
	Used for first initialization and also for resetting things when m_fCanInherit changes.
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::InitAlignmentCtls()
{
	StrAppBuf strb;

	// Initialize values for the Alignment combo box.
	HWND hwndAlign = ::GetDlgItem(m_hwnd, kctidFpCbAlign);

	if (m_fCanInherit)
	{
		strb.Load(kstidFpUnspecified);
		::SendMessage(hwndAlign, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	}
	if (m_fAlignLeadTrail)
	{
		strb.Load(kstidFpAlignLead);
		::SendMessage(hwndAlign, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	}
	else
		m_fAlignLeadTrail = false;
	strb.Load(kstidFpAlignLeft);
	::SendMessage(hwndAlign, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidFpAlignCenter);
	::SendMessage(hwndAlign, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidFpAlignRight);
	::SendMessage(hwndAlign, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	if (m_fAlignLeadTrail)
	{
		strb.Load(kstidFpAlignTrail);
		::SendMessage(hwndAlign, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	}
	strb.Load(kstidFpAlignJustify);
	::SendMessage(hwndAlign, CB_ADDSTRING, 0, (LPARAM)strb.Chars());

}

void FmtParaDlg::InitSpecIndentCtls()
{
	StrAppBuf strb;

	// Initialize the values for the Special Indentation combo box.
	HWND hwndSpec = ::GetDlgItem(m_hwnd, kctidFpCbSpec);

	if (m_fCanInherit)
	{
		strb.Load(kstidFpUnspecified);
		::SendMessage(hwndSpec, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	}
	strb.Load(kstidFpSpecNone);
	::SendMessage(hwndSpec, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidFpSpecFirstLine);
	::SendMessage(hwndSpec, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidFpSpecHang);
	::SendMessage(hwndSpec, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
}

void FmtParaDlg::InitLineSpacingCtls()
{
	StrAppBuf strb;

	// Initialize values for the Line Spacing combo box.
	HWND hwndLnSpc = ::GetDlgItem(m_hwnd, kctidFpCbLineSpace);

	if (m_fCanInherit)
	{
		strb.Load(kstidFpUnspecified);
		::SendMessage(hwndLnSpc, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	}
	strb.Load(kstidFpLsSingle);
	::SendMessage(hwndLnSpc, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidFpLs15Lines);
	::SendMessage(hwndLnSpc, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidFpLsDouble);
	::SendMessage(hwndLnSpc, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidFpLsAtLeast);
	::SendMessage(hwndLnSpc, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	strb.Load(kstidFpLsExact);
	::SendMessage(hwndLnSpc, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
}

/*----------------------------------------------------------------------------------------------
	Fill the controls with their values.
	For the style dialog, these may contain either explicit or inherited values.
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::FillCtls(void)
{
	// If called before OnInitDlg, we can safely ignore, since OnInitDlg calls it again.
	// Also, variables like m_qccmb have not been initialized, and various messages wrongly
	// go to the whole screen, causing flicker.
	if (!m_hwnd)
		return;
	StrAppBuf strb;

	if (m_fCanDoRtl)
	{
		//	Fill direction combo box.
		HWND hDirCombo = GetDlgItem(m_hwnd, kctidFpCbDirection);
		SendMessage(hDirCombo, CB_RESETCONTENT, 0, 0);
		strb.Load(kstidLtrInherited);
		SendMessage(hDirCombo, CB_ADDSTRING, 0, (LPARAM) strb.Chars());
		strb.Load(kstidLtrLeftToRight);
		SendMessage(hDirCombo, CB_ADDSTRING, 0, (LPARAM) strb.Chars());
		strb.Load(kstidLtrRightToLeft);
		SendMessage(hDirCombo, CB_ADDSTRING, 0, (LPARAM) strb.Chars());

		if (m_xprExpl.xRtl == kxInherited)
			SendMessage(hDirCombo, CB_SETCURSEL, 0, 0);
		else
		{
			if (m_pdvE.nRtl == 0)
				SendMessage(hDirCombo, CB_SETCURSEL, 1, 0);
			else
				SendMessage(hDirCombo, CB_SETCURSEL, 2, 0);
		}
	}
	// Fill Alignment and Background controls.
	int icb = AtAlignType();
	if (AtAlignType() == FwStyledText::knConflicting)
		icb = -1;
	else
	{
		if (!m_fAlignLeadTrail)
			icb--; // account for "Leading" which is not offered
		if (m_fCanInherit)
			icb++; // skip "(unspecified)" which is present
	}
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpCbAlign), CB_SETCURSEL, icb, 0);

	m_qccmb->SetColor(ClrBkgdColor());

	// Fill indentation controls.
	m_fChangingEditCtrl = false; // Do not save changes when EN_CHANGE is sent.

	AfUtil::MakeMsrStr(ThinLeadInd(), m_nMsrSys, &strb);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpEdIndLft), WM_SETTEXT, 0, (LPARAM)strb.Chars());
	AfUtil::MakeMsrStr(ThinTrailInd(), m_nMsrSys, &strb);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpEdIndRt), WM_SETTEXT, 0, (LPARAM)strb.Chars());

	icb = SpIndent();
	if (SpIndent() == FwStyledText::knConflicting)
		icb = -1;
	else if (m_fCanInherit)
		icb = SpIndent() + 1; // skip "(unspecified)"
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpCbSpec), CB_SETCURSEL, icb, 0);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpEdSpIndBy),
		(m_xprExpl.xSpInd == kxExplicit && m_pdvE.spindSpIndent != kspindNone));
	::EnableWindow(::GetDlgItem(m_hwnd, kstidFpEdSpIndBy),
		(m_xprExpl.xSpInd == kxExplicit && m_pdvE.spindSpIndent != kspindNone));	// For ALT+y.
	AfUtil::MakeMsrStr(ThinSpIndBy(), m_nMsrSys, &strb);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpEdSpIndBy), WM_SETTEXT, 0, (LPARAM)strb.Chars());

	// Fill line spacing controls.
	AfUtil::MakeMsrStr(MptSpacBef(), knpt, &strb);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpEdSpacBef), WM_SETTEXT, 0, (LPARAM)strb.Chars());
	AfUtil::MakeMsrStr(MptSpacAft(), knpt, &strb);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpEdSpacAft), WM_SETTEXT, 0, (LPARAM)strb.Chars());

	icb = LineSpaceType();
	if (LineSpaceType() == FwStyledText::knConflicting)
		icb = -1;
	else if (m_fCanInherit)
		icb = LineSpaceType() + 1; // skip "(unspecified)"
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpCbLineSpace), CB_SETCURSEL, icb, 0);
	bool fAbsLnSp = (m_pdvE.lnspSpaceType == klnspAtLeast || m_pdvE.lnspSpaceType == klnspExact);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFpEdLineSpaceAt),
		(m_xprExpl.xLnSp == kxExplicit && fAbsLnSp));
	::EnableWindow(::GetDlgItem(m_hwnd, kstidFpEdLsAt),
		(m_xprExpl.xLnSp == kxExplicit && fAbsLnSp));	// En/Dis-able the text also, for ALT+t.
	// TODO SteveMc: check the value being set here.
	AfUtil::MakeMsrStr(MptLnSpAt(), knpt, &strb);
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpEdLineSpaceAt), WM_SETTEXT, 0,
		(LPARAM)strb.Chars());

	m_fChangingEditCtrl = true; // We are done initializing the edittext boxes; save
								// changes when EN_CHANGE is sent.

	// Update preview.
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidFpPreview), NULL, true);

/*	StrApp strInches(kstidFpInchAbbr);
	strb.Format("%d.%03d%s", m_thinLeadInd / 1000, m_thinLeadInd % 1000, strInches.Chars());
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpEdIndLft), WM_SETTEXT, 0, (LPARAM)strb.Chars());
	strb.Format("%d.%03d%s", m_thinTrailInd / 1000, m_thinTrailInd % 1000, strInches.Chars());
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpEdIndRt), WM_SETTEXT, 0, (LPARAM)strb.Chars());
	strb.Format("%d.%03d%s", m_thinSpIndBy / 1000, m_thinSpIndBy % 1000, strInches.Chars());
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpEdSpIndBy), WM_SETTEXT, 0, (LPARAM)strb.Chars());

	StrApp strPts(kstidFbPointsAbbr);
	strb.Format("%d%s", m_mptSpacBef, strPts.Chars());
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpEdSpacBef), WM_SETTEXT, 0, (LPARAM)strb.Chars());
	strb.Format("%d%s", m_mptSpacAft, strPts.Chars());
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpEdSpacAft), WM_SETTEXT, 0, (LPARAM)strb.Chars());
	strb.Format("%d%s", m_mptLnSpAt, strPts.Chars());
	::SendMessage(::GetDlgItem(m_hwnd, kctidFpEdLineSpaceAt), WM_SETTEXT, 0,
		(LPARAM)strb.Chars());
*/
} // FmtParaDlg::FillCtls.


/*----------------------------------------------------------------------------------------------
	Handle a change in a combo box.
----------------------------------------------------------------------------------------------*/
bool FmtParaDlg::OnComboChange(NMHDR * pnmh, long & lnRet)
{
	HWND hwndEdit;
	HWND hwndSpin;
	HWND hwndText;	// Needed to enable/disable the static text associated with the Edit box.
	StrAppBuf strb;

	AssertPtr(pnmh);
	if (pnmh->idFrom == kctidFpCbBkgrnd)
	{
		bool f = AfWnd::OnNotifyChild(pnmh->idFrom, pnmh, lnRet);
		if (m_fCanInherit && m_pdvE.clrBkgdColor == kclrTransparent)
		{
			m_pdvE.clrBkgdColor = (COLORREF)FwStyledText::knUnspecified;
			m_xprExpl.xBkgdColor = kxInherited;
			// Do later (UpdateComboWithInherited):
			//m_qccmb->SetColor(m_pdvI.clrBkgdColor);
		}
		else
			m_xprExpl.xBkgdColor = kxExplicit;
		return f;
	}

	// Get the current index from the combo box.
	int icb = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);

	switch (pnmh->idFrom)
	{
	case kctidFpCbAlign:
		if (m_fCanInherit)
			icb--; // to account for presence of "(unspecified)" option
		if (m_fCanInherit && icb < 0)
		{
			m_pdvE.atAlignType = FwStyledText::knUnspecified;
			m_xprExpl.xAlign = kxInherited;
			// Do later (UpdateComboWithInherited):
			//int icbInherited = m_pdvI.atAlignType + 1; // 1 for (unspecified)
			//::SendMessage(pnmh->hwndFrom, CB_SETCURSEL, icbInherited, 0);
		}
		else
		{
			Assert(icb >= 0);
			if (m_fAlignLeadTrail)
				m_pdvE.atAlignType = (AlignmentType)icb;
			else
				// skip "Leading" which is not offered
				m_pdvE.atAlignType = (AlignmentType)(icb + 1);

			m_xprExpl.xAlign = kxExplicit;
		}
		break;
	case kctidFpCbSpec:
		if (m_fCanInherit)
			icb--; // to account for presence of "(unspecified)" option

		hwndEdit = ::GetDlgItem(m_hwnd, kctidFpEdSpIndBy);
		hwndSpin = ::GetDlgItem(m_hwnd, kctidFpSpSpIndBy);
		hwndText = ::GetDlgItem(m_hwnd, kstidFpEdSpIndBy);

		// The following one line is only here to force the spin ctrl to repaint after it is
		// enabled. It will not paint unless it is first disabled.
		::EnableWindow(hwndSpin, 0);

		m_pdvE.spindSpIndent = (SpecialIndent)icb;
		if (icb > 0)
		{
			// Get the text from the edit box and convert it to a number.
			StrAppBuf strb;
			int cch = ::SendMessage(hwndEdit, WM_GETTEXT, (WPARAM)strb.kcchMaxStr,
				(LPARAM)strb.Chars());
			if (cch > strb.kcchMaxStr)
				cch = strb.kcchMaxStr;
			strb.SetLength(cch);
			int nValue;
			AfUtil::GetStrMsrValue(&strb , m_nMsrSys, &nValue);
			if (nValue == 0)
			{
				// set edit box to .3"
				nValue = 21600;
				AfUtil::MakeMsrStr(nValue , m_nMsrSys, &strb);
				::SendMessage(hwndEdit, WM_SETTEXT, 0, (LPARAM)strb.Chars());
			}
			m_pdvE.thinSpIndBy = nValue;
		}
		if (m_fCanInherit && m_pdvE.spindSpIndent < 0)
		{
			m_pdvE.spindSpIndent = FwStyledText::knUnspecified;
			m_pdvE.thinSpIndBy = FwStyledText::knUnspecified;
			m_xprExpl.xSpInd = kxInherited;
			// Do later (UpdateComboWithInherited):
			//int icbInherited = m_pdvI.spindSpIndent + 1; // 1 for (unspecified)
			//::SendMessage(pnmh->hwndFrom, CB_SETCURSEL, icbInherited, 0);
			AfUtil::MakeMsrStr(m_pdvI.thinSpIndBy, m_nMsrSys, &strb);
			::SendMessage(hwndEdit, WM_SETTEXT, 0, (LPARAM)strb.Chars());
			::EnableWindow(hwndEdit, false);
			::EnableWindow(hwndSpin, false);
			::EnableWindow(hwndText, false);
			// Do this again after updating the spin control:
			m_pdvE.thinSpIndBy = FwStyledText::knUnspecified;
			m_xprExpl.xSpInd = kxInherited;
		}
		else
		{
			m_xprExpl.xSpInd = kxExplicit;
			::EnableWindow(hwndEdit, icb != kspindNone);
			::EnableWindow(hwndSpin, icb != kspindNone);
			::EnableWindow(hwndText, icb != kspindNone);
		}
		break;

	case kctidFpCbLineSpace:
		if (m_fCanInherit)
			icb--; // handle presence of "(unspecified)" option

		hwndEdit = ::GetDlgItem(m_hwnd, kctidFpEdLineSpaceAt);
		hwndSpin = ::GetDlgItem(m_hwnd, kctidFpSpLineSpaceAt);
		hwndText = ::GetDlgItem(m_hwnd, kstidFpEdLsAt);

		// The following one line is only here to force the spin ctrl to repaint after it is
		// enabled. It will not paint unless it is first off.
		::EnableWindow(hwndSpin, 0);

		m_pdvE.lnspSpaceType = (LineSpacing)icb;
		if (m_fCanInherit && m_pdvE.lnspSpaceType < 0)
		{
			m_pdvE.lnspSpaceType = FwStyledText::knUnspecified;
			m_pdvE.mptLnSpAt = FwStyledText::knUnspecified;
			m_xprExpl.xLnSp = kxInherited;
			// Do later (UpdateComboWithInherited):
			//int icbInherited = m_pdvI.lnspSpaceType + 1; // 1 for (unspecified)
			//::SendMessage(pnmh->hwndFrom, CB_SETCURSEL, icbInherited, 0);
			AfUtil::MakeMsrStr(m_pdvI.mptLnSpAt, knpt, &strb);
			::SendMessage(hwndEdit, WM_SETTEXT, 0, (LPARAM)strb.Chars());
			::EnableWindow(hwndEdit, false);
			::EnableWindow(hwndSpin, false);
			::EnableWindow(hwndText, false);
			// Do this again after updating the spin control
			m_pdvE.mptLnSpAt = FwStyledText::knUnspecified;
			m_xprExpl.xLnSp = kxInherited;
		}
		else
		{
			m_xprExpl.xLnSp = kxExplicit;
			bool fAbsLnSp = (icb == klnspAtLeast || icb == klnspExact);
			::EnableWindow(hwndEdit, fAbsLnSp);
			::EnableWindow(hwndSpin, fAbsLnSp);
			::EnableWindow(hwndText, fAbsLnSp);
		}
		break;

	case kctidFpCbDirection:
		switch(icb)
		{
		case 0:	// inherited
			m_xprExpl.xRtl = kxInherited;
			m_pdvE.nRtl = 0;
			break;

		case 1:	// left-to-right
			m_xprExpl.xRtl = kxExplicit;
			m_pdvE.nRtl = 0;
			break;

		case 2:	// right-to-left
			m_xprExpl.xRtl = kxExplicit;
			m_pdvE.nRtl = 1;
			break;
		}
		break;

	default:
		Assert(false);	// We shouldn't get here.
	}

	// Update preview updates.
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidFpPreview), NULL, false);
	lnRet = 0;
	return true;
} // FmtParaDlg::OnComboChange.

/*----------------------------------------------------------------------------------------------
	Handles a click on a button.
	// TODO KenZ(?): handle inheritance
----------------------------------------------------------------------------------------------*/
bool FmtParaDlg::OnButtonClick(NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	//HWND hwndLtr = ::GetDlgItem(m_hwnd, kctidFpLtr);
	//HWND hwndRtl = ::GetDlgItem(m_hwnd, kctidFpRtl);

//	int nLtr, nRtl;

	switch (pnmh->idFrom)
	{
	//case kctidFpLtr:
	//case kctidFpRtl:
	//	if (!m_fCanDoRtl)
	//	{
	//		Assert(false);
	//		return true;
	//	}
	//	m_pdvE.nRtl = (pnmh->idFrom == kctidFpRtl);
	//	m_xprExpl.xRtl = kxExplicit;
	//	nLtr = (m_pdvE.nRtl) ? BST_UNCHECKED : BST_CHECKED;
	//	nRtl = (m_pdvE.nRtl) ? BST_CHECKED : BST_UNCHECKED;
	//	::SendMessage(hwndLtr, BM_SETCHECK, (WPARAM)nLtr, 0);
	//	::SendMessage(hwndRtl, BM_SETCHECK, (WPARAM)nRtl, 0);

	//	UpdateLabels();

	//	break;
	case kctidFpPreview:
		return false;	// ignore
	default:
		Assert(false);
	}

	// Update preview updates.
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidFpPreview), NULL, false);
	lnRet = 0;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Update labels that are state-sensitive, such as the indentation labels.
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::UpdateLabels()
{
	if (!m_fCanDoRtl)
		return;

	StrAppBuf strb;

	HWND hwndLeadInd = ::GetDlgItem(m_hwnd, kctidFpLabelIndLft);
	HWND hwndTrailInd = ::GetDlgItem(m_hwnd, kctidFpLabelIndRt);
	if (NRtl() == FwStyledText::knConflicting || NRtl() == FwStyledText::knUnspecified)
	{
		// Leading / Trailing
		strb.Load(kstidFpLabelIndLeading);
		::SendMessage(hwndLeadInd, WM_SETTEXT, 0, (LPARAM)strb.Chars());
		strb.Load(kstidFpLabelIndTrailing);
		::SendMessage(hwndTrailInd, WM_SETTEXT, 0, (LPARAM)strb.Chars());
	}
	else if (NRtl() == 0) // Review (SharonC): Should the unspecified case go here?
	{
		// Left / Right
		strb.Load(kstidFpLabelIndLeft);
		::SendMessage(hwndLeadInd, WM_SETTEXT, 0, (LPARAM)strb.Chars());
		strb.Load(kstidFpLabelIndRight);
		::SendMessage(hwndTrailInd, WM_SETTEXT, 0, (LPARAM)strb.Chars());
	}
	else if (NRtl() == 1)
	{
		// Right / Left
		strb.Load(kstidFpLabelIndRight);
		::SendMessage(hwndLeadInd, WM_SETTEXT, 0, (LPARAM)strb.Chars());
		strb.Load(kstidFpLabelIndLeft);
		::SendMessage(hwndTrailInd, WM_SETTEXT, 0, (LPARAM)strb.Chars());
	}
	else
		Assert(false);
}

/*----------------------------------------------------------------------------------------------
	Handles a click on a spin control, by doing the appropriate increment.
----------------------------------------------------------------------------------------------*/
bool FmtParaDlg::OnDeltaSpin(NMHDR * pnmh, long & lnRet)
{
//	char rgch[20];
//	itoa(pnmh->idFrom, rgch, 10);
//	OutputDebugString("OnDeltaSpin:  ");
//	OutputDebugString(rgch);
//	if (pnmh->code == UDN_DELTAPOS)
//		OutputDebugString(" UDN_DELTAPOS");
//	else if (pnmh->code == EN_KILLFOCUS)
//		OutputDebugString(" EN_KILLFOCUS");
//	else
//		OutputDebugString(" ????");
//	OutputDebugString("\n");

	// If the edit box has changed and is out of synch with the spin control, this
	// will update the spin's position to correspond to the edit box.
	StrAppBuf strb;
	strb.SetLength(strb.kcchMaxStr);
	HWND hwndEdit;
	HWND hwndSpin;

	// Get handle for the edit and spin controls.
	if (pnmh->code == UDN_DELTAPOS)
	{
		// Called from a spin control.
		hwndSpin = pnmh->hwndFrom;
		hwndEdit = (HWND)::SendMessage(hwndSpin, UDM_GETBUDDY, 0, 0);
		m_fCurrCtlMod = true; // they fiddled with the spinner
//		OutputDebugString("m_fCurrCtlMod = true\n");
	}
	else
	{
		// Called from an edit control.
		Assert(pnmh->code == EN_KILLFOCUS);
		hwndEdit = pnmh->hwndFrom;
		switch (pnmh->idFrom)
		{
		case kctidFpEdIndLft:
			hwndSpin = ::GetDlgItem(m_hwnd, kctidFpSpIndLft);
			break;
		case kctidFpEdIndRt:
			hwndSpin = ::GetDlgItem(m_hwnd, kctidFpSpIndRt);
			break;
		case kctidFpEdSpIndBy:
			hwndSpin = ::GetDlgItem(m_hwnd, kctidFpSpSpIndBy);
			break;
		case kctidFpEdSpacBef:
			hwndSpin = ::GetDlgItem(m_hwnd, kctidFpSpSpacBef);
			break;
		case kctidFpEdSpacAft:
			hwndSpin = ::GetDlgItem(m_hwnd, kctidFpSpSpacAft);
			break;
		case kctidFpEdLineSpaceAt:
			hwndSpin = ::GetDlgItem(m_hwnd, kctidFpSpLineSpaceAt);
			break;
		default:
			Assert(false);
		}
	}

	// Get the text from the edit box and convert it to a number.
	int cch = ::SendMessage(hwndEdit, WM_GETTEXT, (WPARAM)strb.kcchMaxStr,
		(LPARAM)strb.Chars());
	if (cch > strb.kcchMaxStr)
		cch = strb.kcchMaxStr;
	strb.SetLength(cch);
	bool fIndent = pnmh->idFrom == kctidFpSpIndLft || pnmh->idFrom == kctidFpSpIndRt ||
		pnmh->idFrom == kctidFpSpSpIndBy || pnmh->idFrom == kctidFpEdIndLft ||
		pnmh->idFrom == kctidFpEdIndRt || pnmh->idFrom == kctidFpEdSpIndBy;
	int nValue;

	// Remember the current control and whether the value is empty, so that when we update
	// the edit field with the new value, we won't think this is something they typed.
	m_kctidUpdateCtl = pnmh->idFrom;
	m_fUpdateCtlEmpty = (cch == 0);

	if (fIndent)
		AfUtil::GetStrMsrValue(&strb, m_nMsrSys, &nValue);
	else
		AfUtil::GetStrMsrValue(&strb, knpt, &nValue);

	if (pnmh->code == UDN_DELTAPOS)
	{
		// amount of allowed error in millipoints. since values are entered in various units
		// and then converted to millipoints, the delta amount will rarely be exact. So, this
		// is the amount of tolerated error that the increment value can differ from the
		// delta amount and still be considered the same.
		const int kAmountOfError = 50;

		// If nValue is not already a whole increment of nDelta, then we only increment it
		// enough to make it a whole increment. If already a whole increment, then we go ahead
		// and increment it the entire amount. Thus if the increment is 0.25" and the original
		// value was 0.15", the first click on the arrow will bring it to 0.25; the next click
		// to 0.50".
		int nDelta = ((NMUPDOWN *)pnmh)->iDelta;
		int nPartialIncrement = nValue % nDelta;
		if (abs(nDelta - nPartialIncrement) < kAmountOfError)
			nPartialIncrement = 0;
		if (nPartialIncrement && nDelta > 0)
			nValue += (nDelta - nPartialIncrement);
		else if (nPartialIncrement && nDelta < 0)
			nValue -= nPartialIncrement;
		else
			nValue += nDelta;
	}
	else if (pnmh->code == EN_KILLFOCUS && m_fUpdateCtlEmpty)
	{
		// Get the inherited value.
		switch (pnmh->idFrom)
		{
		case kctidFpEdIndLft:
			nValue = (m_xprExplOrig.xLeadInd == kxConflicting) ?
				FwStyledText::knConflicting : m_pdvI.thinLeadInd;
			break;
		case kctidFpEdIndRt:
			nValue = (m_xprExplOrig.xTrailInd == kxConflicting) ?
				FwStyledText::knConflicting : m_pdvI.thinTrailInd;
			break;
		case kctidFpEdSpIndBy:
			nValue = (m_xprExplOrig.xSpInd == kxConflicting) ?
				FwStyledText::knConflicting : m_pdvI.thinSpIndBy;
			break;
		case kctidFpEdSpacBef:
			nValue = (m_xprExplOrig.xSpacBef == kxConflicting) ?
				FwStyledText::knConflicting : m_pdvI.mptSpacBef;
			break;
		case kctidFpEdSpacAft:
			nValue = (m_xprExplOrig.xSpacAft == kxConflicting) ?
				FwStyledText::knConflicting : m_pdvI.mptSpacAft;
			break;
		case kctidFpEdLineSpaceAt:
			nValue = (m_xprExplOrig.xLnSp == kxConflicting) ?
				FwStyledText::knConflicting : m_pdvI.mptLnSpAt;
			break;
		default:
			Assert(false);
		}
	}

	// Don't exceed the minimum or maximum values in the spin control.
	int nRangeMin = 0;
	int nRangeMax = 0;
	::SendMessage(hwndSpin, UDM_GETRANGE32, (long)&nRangeMin, (long)&nRangeMax);
	if (nValue != FwStyledText::knConflicting)
		nValue = NBound(nValue, nRangeMin, nRangeMax);

	// Update the edit box.
	if (nValue == FwStyledText::knConflicting)
		strb.Clear();
	else if (fIndent)
		AfUtil::MakeMsrStr(nValue, m_nMsrSys, &strb);
	else
		AfUtil::MakeMsrStr(nValue, knpt, &strb);
	::SendMessage(hwndEdit, WM_SETTEXT, 0, (LPARAM)strb.Chars());

	bool fUpdate = (cch > 0 || !m_fCanInherit || pnmh->code == UDN_DELTAPOS);
	int x =  (fUpdate) ? kxExplicit : kxInherited;
	if (nValue == FwStyledText::knConflicting)
	{
		fUpdate = true;
		x = kxConflicting;
	}

	if (m_fCurrCtlMod)
	{
		// Update the appropriate member variable.
		switch (pnmh->idFrom)
		{
		case kctidFpSpIndLft:
		case kctidFpEdIndLft:
			if (fUpdate)
				m_pdvE.thinLeadInd = nValue;
			m_xprExpl.xLeadInd = x;
			break;
		case kctidFpSpIndRt:
		case kctidFpEdIndRt:
			if (fUpdate)
				m_pdvE.thinTrailInd = nValue;
			m_xprExpl.xTrailInd = x;
			break;
		case kctidFpSpSpIndBy:
		case kctidFpEdSpIndBy:
			if (fUpdate)
				m_pdvE.thinSpIndBy = nValue;
			m_xprExpl.xSpInd = x;
			break;
		case kctidFpSpSpacBef:
		case kctidFpEdSpacBef:
			if (fUpdate)
				m_pdvE.mptSpacBef = nValue;
			m_xprExpl.xSpacBef = x;
			break;
		case kctidFpSpSpacAft:
		case kctidFpEdSpacAft:
			if (fUpdate)
				m_pdvE.mptSpacAft = nValue;
			m_xprExpl.xSpacAft = x;
			break;
		case kctidFpSpLineSpaceAt:
		case kctidFpEdLineSpaceAt:
			if (fUpdate)
				m_pdvE.mptLnSpAt = nValue;
			m_xprExpl.xLnSp = x;
			break;
		default:
			Assert(false);	// We should never reach this.
		}
	}

	// Update preview window.
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidFpPreview), NULL, false);
	lnRet = 0;

	// The value has been updated to match the control.
	m_fCurrCtlMod = false;
//	OutputDebugString("m_fCurrCtlMod = false\n");

//	OutputDebugString("leaving OnDeltaSpin\n");

	return true;
} // FmtParaDlg::OnDeltaSpin.

/*----------------------------------------------------------------------------------------------
	Process draw messages.
----------------------------------------------------------------------------------------------*/
bool FmtParaDlg::OnDrawChildItem(DRAWITEMSTRUCT * pdis)
{
	if (pdis->CtlID == kctidFpPreview)
	{
		UpdatePreview(pdis);
		return true;
	}
	return AfWnd::OnDrawChildItem(pdis);
}

/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool FmtParaDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	switch(pnmh->code)
	{
	case CBN_SELENDOK:
		if (ctidFrom == kctidFpCbBkgrnd) // Color "combo box" was modified
		{
			::InvalidateRect(::GetDlgItem(m_hwnd, kctidFpPreview), NULL, false);
			return true;
		}
		break;

	case UDN_DELTAPOS: // Spin control is activated.
	case EN_KILLFOCUS: // Edit control modified.
		return OnDeltaSpin(pnmh, lnRet);

	case EN_CHANGE: // Change detected in edittext box.
		// Don't call OnEditCtrlChange is all we are doing is filling controls.
		if (m_fChangingEditCtrl)
		{
			switch(ctidFrom)
			{
			default:
				break;
			case kctidFpEdIndLft:
			case kctidFpEdIndRt:
			case kctidFpEdSpIndBy:
			case kctidFpEdSpacBef:
			case kctidFpEdSpacAft:
			case kctidFpEdLineSpaceAt:
				return OnEditCtrlChange(ctidFrom);
			}
		};
		break;

	case CBN_SELCHANGE: // Combo box item changed.
		return OnComboChange(pnmh, lnRet);

	case BN_CLICKED:
		return OnButtonClick(pnmh, lnRet);

	case CBN_KILLFOCUS:
		UpdateComboWithInherited(ctidFrom, pnmh);
		break;

	// Default is do nothing.
	}

	return AfWnd::OnNotifyChild(ctidFrom, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------
	If the value has been changed to inherited, update the combo-box to show
	the inherited value. We want to do this after the combo has lost focus so the gray
	color will show; otherwise it could look like the combo isn't working.
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::UpdateComboWithInherited(int ctid, NMHDR * pnmh)
{
//	OutputDebugString("Combo lost focus - update\n");

	if (!m_fCanInherit)
		return;

	if (ctid == kctidFpCbAlign)
	{
		if (m_xprExpl.xAlign != kxExplicit)
		{
			int icbInherited = m_pdvI.atAlignType + 1; // 1 for (unspecified)
			if (!m_fAlignLeadTrail)
				icbInherited--;
			::SendMessage(pnmh->hwndFrom, CB_SETCURSEL, icbInherited, 0);
		}
	}
	if (ctid == kctidFpCbSpec)
	{
		if (m_xprExpl.xSpInd != kxExplicit)
		{
			int icbInherited = m_pdvI.spindSpIndent + 1; // 1 for (unspecified)
			::SendMessage(pnmh->hwndFrom, CB_SETCURSEL, icbInherited, 0);
		}
	}
	if (ctid == kctidFpCbLineSpace)
	{
		if (m_xprExpl.xLnSp != kxExplicit)
		{
			int icbInherited = m_pdvI.lnspSpaceType + 1; // 1 for (unspecified)
			::SendMessage(pnmh->hwndFrom, CB_SETCURSEL, icbInherited, 0);
		}
	}
	if (ctid == kctidFpCbBkgrnd)
	{
		if (m_xprExpl.xBkgdColor != kxExplicit)
			m_qccmb->SetColor(m_pdvI.clrBkgdColor);
	}
}

/*----------------------------------------------------------------------------------------------
	Update the appropriate member variable based on changes in an edit control.
----------------------------------------------------------------------------------------------*/
bool FmtParaDlg::OnEditCtrlChange(int ctidFrom)
{
//	char rgch[20];
//	itoa(ctidFrom, rgch, 10);
//	OutputDebugString("OnEditCtrlChange:  ");
//	OutputDebugString(rgch);
//	OutputDebugString("\n");

	HWND hwndEdit;
	int cch;
	int nValue;
	StrAppBuf strb;
	strb.SetLength(strb.kcchMaxStr);

	// Get the value from the edit control.
	hwndEdit = ::GetDlgItem(m_hwnd, ctidFrom);
	cch = ::SendMessage(hwndEdit, WM_GETTEXT, (WPARAM)strb.kcchMaxStr, (LPARAM)strb.Chars());
	if (cch > strb.kcchMaxStr)
		cch = strb.kcchMaxStr;
	strb.SetLength(cch);

	if (cch == 1 && strb[0] == '-')
	{
		strb.Clear();	// Handle special case of user beginning with minus sign: remove it.
		cch = 0;
		::SendMessage(hwndEdit, WM_SETTEXT, 0, (LPARAM)strb.Chars());
	}

	bool fZapped =
		(m_fCanInherit && (cch == 0 || (m_fUpdateCtlEmpty && m_kctidUpdateCtl == ctidFrom)));

	// Convert to the appropriate units.
	switch(ctidFrom)
	{
	default:
		break;
	case kctidFpEdIndLft:
	case kctidFpEdIndRt:
	case kctidFpEdSpIndBy:
		// Convert to thousandths of inches (thin).
		AfUtil::GetStrMsrValue(&strb, m_nMsrSys, &nValue);
		break;
	case kctidFpEdSpacBef:
	case kctidFpEdSpacAft:
	case kctidFpEdLineSpaceAt:
		// Convert to thousandths of points (mpt).
		AfUtil::GetStrMsrValue(&strb, knpt, &nValue);
		break;
	}

	// The value cannot be negative. Update the edit box if necessary.
	if (0 > nValue)
	{
		nValue = 0;
		strb = "0";
		strb.SetLength(1);
		::SendMessage(hwndEdit, WM_SETTEXT, 0, (LPARAM)strb.Chars());
	}

	// The value of the control will be updated by the OnDeltaSpin method.
	if (m_kctidUpdateCtl == ctidFrom)
	{
//		OutputDebugString(" - no value adjustments\n");
		m_kctidUpdateCtl = 0;
		return true;
	}

	m_fCurrCtlMod = true; // they typed something
//	OutputDebugString("m_fCurrCtlMod = true\n");

	// Update the member variable. If cch is zero, i.e., the text in the edittext box has
	// been deleted, set the member variable to FwStyledText::knConflicting.
	// setting.
	int x = (fZapped) ? kxInherited : kxExplicit;
	switch(ctidFrom)
	{
	default:
		break;
	case kctidFpEdIndLft:
		if (!fZapped)
			m_pdvE.thinLeadInd = nValue;
		else
			m_pdvE.thinLeadInd = FwStyledText::knUnspecified;
		m_xprExpl.xLeadInd = x;
		break;
	case kctidFpEdIndRt:
		if (!fZapped)
			m_pdvE.thinTrailInd = nValue;
		else
			m_pdvE.thinTrailInd = FwStyledText::knUnspecified;
		m_xprExpl.xTrailInd = x;
		break;
	case kctidFpEdSpIndBy:
		if (!fZapped)
			m_pdvE.thinSpIndBy = nValue;
		else
			m_pdvE.thinSpIndBy = FwStyledText::knUnspecified;
		m_xprExpl.xSpInd = x;
		break;
	case kctidFpEdSpacBef:
		if (!fZapped)
			m_pdvE.mptSpacBef = nValue;
		else
			m_pdvE.mptSpacBef = FwStyledText::knUnspecified;
		m_xprExpl.xSpacBef = x;
		break;
	case kctidFpEdSpacAft:
		if (!fZapped)
			m_pdvE.mptSpacAft = nValue;
		else
			m_pdvE.mptSpacAft = FwStyledText::knUnspecified;
		m_xprExpl.xSpacAft = x;
		break;
	case kctidFpEdLineSpaceAt:
		// This control is disabled unless the type of spacing is "At Least."
		if (!fZapped)
			m_pdvE.mptLnSpAt = nValue;
		else
			m_pdvE.mptLnSpAt = FwStyledText::knUnspecified;
		m_xprExpl.xLnSp = x;
		break;
	}
//	OutputDebugString(" - value adjustments\n");
	return true;
}

/*----------------------------------------------------------------------------------------------
	Takes care of painting the Preview window, which shows the currently chosen settings in
	the dialog. We display a paragraph with three lines, in between two other paragraphs,
	with visual representations of the various settings.

	Conversions:
		4 points     -> 1 pixel
		0.025 inches -> 1 pixel
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::UpdatePreview(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);

	// Setup to paint
	HDC hdc = pdis->hDC;
	RECT rect = pdis->rcItem;
	SIZE sizeMargins = { ::GetSystemMetrics(SM_CXEDGE), ::GetSystemMetrics(SM_CYEDGE) };
	SmartPalette spal(hdc);

	// Reduce the size of the "client" area of the button to exclude the border.
	rect.left += sizeMargins.cx;
	rect.right -= sizeMargins.cx;
	rect.top += sizeMargins.cy;
	rect.bottom -= sizeMargins.cy;

	::SetBkMode(hdc, TRANSPARENT);

	const int kmptPerPixel = 3000;
	const int kthinPerPixel = 1656;

	const int kysLineHeight = 5;
	const int kysLineSpacing = kysLineHeight + 2;
	const int kxsPageMargin = 5;
	const int kysPageTopMargin = 5;
	int nxsMaxParaWidth = rect.right - rect.left - kxsPageMargin * 2;

	int xs = rect.left + kxsPageMargin;
	int ys = rect.top + kysPageTopMargin;
	int iLine;

	// Background of preview area
	AfGfx::FillSolidRect(hdc, rect, ::GetSysColor(COLOR_WINDOW));

	// Draw two lines representing the paragraph before this one.
	for (iLine = 0; iLine < 2; ++iLine, ys += kysLineSpacing)
	{
		if (m_fOuterRtl)
		{
			RECT rectLine = {xs, ys,
				((iLine == 0) ? xs + nxsMaxParaWidth - 10 : xs + nxsMaxParaWidth),
				ys + kysLineHeight};
			AfGfx::FillSolidRect(hdc, rectLine, kclrLightGray);
		}
		else
		{
			RECT rectLine = {((iLine == 0) ? xs + 10 : xs), ys,
				xs + nxsMaxParaWidth, ys + kysLineHeight};
			AfGfx::FillSolidRect(hdc, rectLine, kclrLightGray);
		}
	}

	// Calculate settings for this paragraph.
	// Make local variables with defaults in place of the real ones, which may be
	// 'conflicting'
	int mptSpacBef = MptSpacBef();
	int mptSpacAft = MptSpacAft();
	int spindSpIndent = SpIndent();
	int thinSpIndBy = ThinSpIndBy();
	int thinLeadInd = ThinLeadInd();
	int thinTrailInd = ThinTrailInd();
	int clrBkgdColor = ClrBkgdColor();
	bool fParaRtl = (NRtl() == 1);

	if (mptSpacBef == FwStyledText::knConflicting || mptSpacBef == FwStyledText::knUnspecified)
		mptSpacBef = 0;
	if (mptSpacAft == FwStyledText::knConflicting || mptSpacAft == FwStyledText::knUnspecified)
		mptSpacAft = 0;
	if (spindSpIndent == FwStyledText::knConflicting ||
		spindSpIndent == FwStyledText::knUnspecified)
	{
		spindSpIndent = kspindNone;
	}
	if (thinSpIndBy == FwStyledText::knConflicting ||
		thinSpIndBy == FwStyledText::knUnspecified)
	{
		thinSpIndBy = 0;
	}
	if (thinLeadInd == FwStyledText::knConflicting ||
		thinLeadInd == FwStyledText::knUnspecified)
	{
		thinLeadInd = 0;
	}
	if (thinTrailInd == FwStyledText::knConflicting ||
		thinTrailInd == FwStyledText::knUnspecified)
	{
		thinTrailInd = 0;
	}
	if (clrBkgdColor == FwStyledText::knConflicting ||
		clrBkgdColor == FwStyledText::knUnspecified)
	{
		clrBkgdColor = ::GetSysColor(COLOR_WINDOW);
	}
	if (NRtl() == FwStyledText::knConflicting || NRtl() == FwStyledText::knUnspecified)
		fParaRtl = m_fOuterRtl;
	else
		fParaRtl = NRtl();

	// If one of these is conflicting they both are, and we default to single space, 12 pt
	int mptLnSpAt = MptLnSpAt();
	int lnspSpaceType = LineSpaceType();

	if (mptLnSpAt == FwStyledText::knConflicting || mptLnSpAt == FwStyledText::knUnspecified)
		mptLnSpAt = 12000;
	if (lnspSpaceType == FwStyledText::knConflicting ||
		lnspSpaceType == FwStyledText::knUnspecified)
	{
		lnspSpaceType = klnspSingle;
	}

	int nValue = NBound(mptSpacBef, kmptSpcMin, kmptSpcMax);
	int ysSpaceBefore = (nValue / kmptPerPixel);
	nValue = NBound(mptSpacAft, kmptSpcMin, kmptSpcMax);
	int ysSpaceAfter  = (nValue / kmptPerPixel);
	int xsTrailIndent = (thinTrailInd / kthinPerPixel);

	nValue = (kspindFirstLine == spindSpIndent) ?
		thinSpIndBy : 0;
	nValue = NBound(nValue + thinLeadInd, kthinIndMin, 240000);
	int xsFirstLineIndent = nValue / kthinPerPixel;

	nValue = (kspindHanging == spindSpIndent) ?
		thinSpIndBy : 0;
	nValue = NBound(nValue + thinLeadInd, kthinIndMin, 240000);
	int xsOtherLineIndent = nValue / kthinPerPixel;

	nValue = NBound(mptLnSpAt, 10000, kmptSpcMax);
	int ysLineSpacing;
	switch (lnspSpaceType)
	{
	case klnspSingle:
		ysLineSpacing = kysLineSpacing;
		break;
	case klnsp15Lines:
		ysLineSpacing = (3 * kysLineSpacing) / 2;
		break;
	case klnspDouble:
		ysLineSpacing = 2 * kysLineSpacing;
		break;
	case klnspAtLeast:
	case klnspExact:
		{
			// TODO KenZ(?): handle inherited font size
			double dmptPerPixel = 1.2 * m_pdvE.mptFontSize / double(kysLineSpacing);
			double dysLineSpacing = double(nValue) / dmptPerPixel;
			// REVIEW SteveMc: should we round off here instead of truncate?
			ysLineSpacing = int(dysLineSpacing);
			if (ysLineSpacing < kysLineSpacing)
				ysLineSpacing = kysLineSpacing;
		}
		break;
	default:
		Assert(false);		// THIS SHOULD NEVER HAPPEN!
		break;
	}
	ysLineSpacing = NBound(ysLineSpacing, 1, 16);

//	int ysParagraphHeight = 2 * ysLineSpacing + kysLineSpacing + ysSpaceBefore + ysSpaceAfter;
	int ysParagraphHeight = 3 * ysLineSpacing + ysSpaceBefore + ysSpaceAfter;
	int dxsShortenBy[3] = {20, 0, 100}; // to make a ragged edge

	// Draw the paragraph's background
	int xsLeft, xsRight;
	if (fParaRtl)
	{
		xsLeft = xs + xsTrailIndent;
		xsRight = xs + nxsMaxParaWidth - min(xsFirstLineIndent, xsOtherLineIndent);
	}
	else
	{
		xsLeft = xs + min(xsFirstLineIndent, xsOtherLineIndent);
		xsRight = xs + nxsMaxParaWidth - xsTrailIndent;
	}
	OnPaintAlignLine(fParaRtl, xsLeft, xsRight, 0);
	RECT rectPara = {xsLeft, ys, xsRight, ys + ysParagraphHeight - 2};
	if (clrBkgdColor == kclrTransparent)
		AfGfx::FillSolidRect(hdc, rectPara, ::GetSysColor(COLOR_WINDOW));
	else
		AfGfx::FillSolidRect(hdc, rectPara, clrBkgdColor);

	// First Line
	ys += ysSpaceBefore;
	if (fParaRtl)
	{
		xsLeft = xs + xsTrailIndent;
		xsRight = xs + nxsMaxParaWidth - xsFirstLineIndent;
	}
	else
	{
		xsLeft = xs + xsFirstLineIndent;
		xsRight = xs + nxsMaxParaWidth - xsTrailIndent;
	}
	OnPaintAlignLine(fParaRtl, xsLeft, xsRight, dxsShortenBy[0]);
	SetRect(&rectPara, xsLeft, ys, xsRight, ys + kysLineHeight);
//	AfGfx::FillSolidRect(hdc, rectPara, kclrBlack);
	AfGfx::FillSolidRect(hdc, rectPara, ::GetSysColor(COLOR_WINDOWTEXT));

	// Second Line
	ys += ysLineSpacing;
	if (fParaRtl)
	{
		xsLeft = xs + xsTrailIndent;
		xsRight = xs + nxsMaxParaWidth - xsOtherLineIndent;
	}
	else
	{
		xsLeft = xs + xsOtherLineIndent;
		xsRight = xs + nxsMaxParaWidth - xsTrailIndent;
	}
	OnPaintAlignLine(fParaRtl, xsLeft, xsRight, dxsShortenBy[1]);
	SetRect(&rectPara, xsLeft, ys, xsRight, ys + kysLineHeight);
//	AfGfx::FillSolidRect(hdc, rectPara, kclrBlack);
	AfGfx::FillSolidRect(hdc, rectPara, ::GetSysColor(COLOR_WINDOWTEXT));

	// Third Line
	ys += ysLineSpacing;
	if (fParaRtl)  // may have been changed by OnPaintAlignLine--recalc.
	{
		xsLeft = xs + xsTrailIndent;
		xsRight = xs + nxsMaxParaWidth - xsOtherLineIndent;
	}
	else
	{
		xsLeft = xs + xsOtherLineIndent;
		xsRight = xs + nxsMaxParaWidth - xsTrailIndent;
	}
	OnPaintAlignLine(fParaRtl, xsLeft, xsRight, dxsShortenBy[2]);
	SetRect(&rectPara, xsLeft, ys, xsRight, ys + kysLineHeight);
//	AfGfx::FillSolidRect(hdc, rectPara, kclrBlack);
	AfGfx::FillSolidRect(hdc, rectPara, ::GetSysColor(COLOR_WINDOWTEXT));
	ys += ysLineSpacing + ysSpaceAfter;

	// Draw three lines representing paragraphs after the modified one.
	for (iLine = 0; iLine < 3; ++iLine, ys += kysLineSpacing)
	{
		if (m_fOuterRtl)
		{
			RECT rectLine = {xs, ys,
				((iLine == 0) ? xs + nxsMaxParaWidth - 10 : xs + nxsMaxParaWidth),
				ys + kysLineHeight};
			AfGfx::FillSolidRect(hdc, rectLine, kclrLightGray);
		}
		else
		{
			RECT rectLine = {((iLine == 0) ? xs + 10 : xs), ys,
				xs + nxsMaxParaWidth, ys + kysLineHeight};
			AfGfx::FillSolidRect(hdc, rectLine, kclrLightGray);
		}
	}

	// Now draw the preview border. Putting it here at the end makes sure the internal
	// painting doesn't overwrite the border.
	rect = pdis->rcItem;
	DrawEdge(hdc, &rect, EDGE_SUNKEN, BF_RECT);
} // FmtParaDlg::UpdatePreview.

/*----------------------------------------------------------------------------------------------
	Assuming we have already calculated the left and right coordinates of a justified
	paragraph (as we will display it on the screen), we now adjust those calculations so as
	to depict alignment. The Right indentation is first decreased by dxsShortenBy (this is
	called with three different values for the three different lines, so as to depict a
	ragged edge. Then depending on the alignment type, we either move the whole line to the
	right, leave it to the left, or move it to the center.
----------------------------------------------------------------------------------------------*/
void FmtParaDlg::OnPaintAlignLine(bool fParaRtl, int & xsLeft, int & xsRight, int dxsShortenBy)
{
	int xsLen = Max(xsRight - dxsShortenBy, 164) - xsLeft;  // Minimum paragraph width
	switch (AtAlignType())
	{
	case FwStyledText::knUnspecified: // treat as left
	case FwStyledText::knConflicting:
	case katLeft:
		xsRight = xsLeft + xsLen;
		break;
	case katRight:
		xsLeft = xsRight - xsLen;
		break;
	case katCenter:
		xsLeft += ((xsRight - xsLeft - xsLen) / 2);
		xsRight = xsLeft + xsLen;
		break;
	case katLead:
		if (fParaRtl)
			xsLeft = xsRight - xsLen;	// align right
		else
			xsRight = xsLeft + xsLen;	// align left
		break;
	case katTrail:
		if (fParaRtl)
			xsRight = xsLeft + xsLen;	// align left
		else
			xsLeft = xsRight - xsLen;	// align right
		break;
	case katJust:
		// no adjustment required
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle What's This? help.
----------------------------------------------------------------------------------------------*/
bool FmtParaDlg::OnHelpInfo(HELPINFO * phi)
{
	if (m_pafsd)
		return m_pafsd->DoHelpInfo(phi, m_hwnd);
	else
		return SuperClass::OnHelpInfo(phi);
}
