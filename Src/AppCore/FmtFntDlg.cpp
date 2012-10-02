/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FmtFntDlg.cpp
Responsibility: John Landon
Last reviewed: Not yet.

Description:
	Implementation of the Format/Font Dialog class and Format/Styles/Font Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma hdrstop
#include "Main.h"
#undef THIS_FILE
DEFINE_THIS_FILE


const int kdyptMaxOffset = 100000;
const int kdyptMinOffset = -100000;
const int kSpnStpPt = 1000;

const int kmpDefaultSize = 10000; // size 10

/***********************************************************************************************
	Initialization of static const arrays.
***********************************************************************************************/
static const int g_rgdyptSize[] =
{
	kstidFfdUnspecified, // nb: this must be > 120
	8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72
};


// Resource IDs of strings to populate the underline-style combo box (although for most
// of these the text is replaced by actual drawn lines):
static const int g_rgstidUnder[] =
{
	kstidFfdUnspecified,
	kstidFfdNone,
	kstidFfdSingle,
	kstidFfdDouble,
	kstidFfdDotted,
	kstidFfdDashed,
	kstidFfdStrikethrough,
};

// This is parallel to g_rgstidUnder.
static const int g_rguntUnder[] =
{
	FwStyledText::knConflicting,
	kuntNone,
	kuntSingle,
	kuntDouble,
	kuntDotted,
	kuntDashed,
	kuntStrikethrough,
};

// Currently not used:
static const int g_rgstidSuper[] =
{
	kstidFfdUnspecified,
	kstidFfdNone,
	kstidFfdSuperscript,
	kstidFfdSubscript,
};
// This is parallel to g_rgstidSuper; also not used.
static const int g_rgssvSuper[] =
{
	FwStyledText::knConflicting,
	kssvOff,
	kssvSuper,
	kssvSub,
};

// Resource IDs of strings to populate the offset combo box:
static const int g_rgstidOffset[] =
{
	kstidFfdUnspecified,
	kstidFfdNormal,
	kstidFfdRaise,
	kstidFfdLower,
};
// This is parallel to g_rgstidOffset.
static const int g_rgdympOffset[] =
{
	FwStyledText::knConflicting,
	0,
	3000,
	-3000,
};


/***********************************************************************************************
	Global Functions.
***********************************************************************************************/
// Check whether the font size is within the range [6, 96].
// (Microsoft Word limits the font size to 1-1638).
bool IsValidFontSize(int dypt)
{
	return (kdyptMinSize <= dypt && kdyptMaxSize >= dypt);
}

/*----------------------------------------------------------------------------------------------
	Store the new value of the property in the property builder.
----------------------------------------------------------------------------------------------*/
void UpdateProp(ITsTextProps * pttp, int tpt, int varExpected, ITsPropsBldrPtr & qtpb,
	int nOld, int nCur)
{
	// If this property has not changed, do nothing.
	if (nOld == nCur)
		return;
	int nVar, nVal;
	HRESULT hr;
	CheckHr(hr = pttp->GetIntPropValues(tpt, &nVar, &nVal));
	// If this particular ttp already has the correct value, do nothing.
	if (nVar == varExpected && nVal == nCur)
		return;
	// If we don't already have a builder, make one
	if (!qtpb)
		CheckHr(pttp->GetBldr(&qtpb));
	// If the new value is unspecified, delete the prop; otherwise set the new val.
	if (nCur == FwStyledText::knUnspecified)
		CheckHr(qtpb->SetIntPropValues(tpt, -1, -1));
	else
		CheckHr(qtpb->SetIntPropValues(tpt, varExpected, nCur));
}

/*----------------------------------------------------------------------------------------------
	Update an inverting property like Bold or Italic. tpt indicates which.

	nNew indicates the value the user wants. It should be conflicting, kttvOff, or kttvInvert.
	If it is kttvConflicting, the user made no change and we should do nothing.

	Confusingly, if it is kttvInvert, it means the user wants the property ON. That may involve
	setting it in the tpb to either invert or missing, depending on what is currently inherited.

	varExpected is the expected variation in the TsTextProps.
----------------------------------------------------------------------------------------------*/
void UpdateInvertingProp(ITsTextProps * pttp, IVwPropertyStore * pvpsSoft,
	int tpt, int varExpected, ITsPropsBldrPtr & qtpb, int nOld, int nNew)
{
	// If the value was left conflicting, do nothing
	if (FwStyledText::knConflicting == nNew)
		return;

	// Can't change bold or italic from conflicting to unspecified:
	Assert((tpt != ktptBold && tpt != ktptItalic) ||
		nOld != FwStyledText::knConflicting || nNew != FwStyledText::knUnspecified);

	if (FwStyledText::knUnspecified == nNew)
	{
		// Remove.
		if (!qtpb)
			CheckHr(pttp->GetBldr(&qtpb));
		CheckHr(qtpb->SetIntPropValues(tpt, -1, -1));
		return;
	}

	// Figure nValInherit, the inherited value of the property (from styles, etc).
	// The inherited value is either on or off.
	int nValInherit;
	CheckHr(pvpsSoft->get_IntProperty(tpt, &nValInherit));
	if (tpt == ktptBold)
	{
		// The VwPropertyStore uses a special enumeration for bold.
		switch (nValInherit)
		{
		case 400:
			nValInherit = kttvOff;
			break;
		case 700:
			nValInherit = kttvForceOn;
			break;
		default:
			// If something else, approximate.
			nValInherit = (nValInherit >= 550 ? kttvForceOn : kttvOff);
			break;
		}
	}
	// JohnT (10 Sep 2002): I think the prop store may use invert as the 'on' value for italics.
	// Just in case, treat 'invert' as 'on'. Treat anything else as 'off'
	if (nValInherit == kttvInvert)
		nValInherit = kttvForceOn;
	if (nValInherit != kttvForceOn)
		nValInherit = kttvOff;

	// Retrieve nVal, the value set in the TsTextProps.
	int nVar, nVal;
	CheckHr(pttp->GetIntPropValues(tpt, &nVar, &nVal));
	if (nVar != varExpected)
		nVar = nVal = -1; // treat as not specified, a bit kludgy, but shouldn't happen.
	// OK, at this point, nNew indicates the effect the user wants to see, either
	// kttvInvert (meaning on) or kttvOff.
	// nValInherit is either kttvForceOn (indicating that the property is on by inheritance)
	// or kttvOff.
	// nVal indicates the value currently set in the TsTextProps.
	// The value we want in the TsTextProps is either missing (if nNew is the same
	// as nValInherit) or kttvInvert (if nNew is different from nValInherit).
	// It's easier to do the comparision if we replace kttvInvert with kttvForceOn for nNew.
	int temp = nNew == kttvInvert ? kttvForceOn : kttvOff;
	// Now if both are on or both are off, we want missing; if they differ, we need to invert.
	int nValWanted = (nValInherit == temp) ? -1 : kttvInvert;

	// Now nVal is the value in the tpt; nValWanted is what needs to be there.
	// If the same, we have nothing to do.
	if (nVal == nValWanted)
		return; // no change needed.
	// If we don't already have a builder, make one.
	if (!qtpb)
		CheckHr(pttp->GetBldr(&qtpb));
	// If we're clearing the property we have to use a different variation code.
	if (nValWanted == -1)
		varExpected = -1; // erase property.
	// And finally set what is required.
	CheckHr(qtpb->SetIntPropValues(tpt, varExpected, nValWanted));
}


/***********************************************************************************************
	FmtFntDlg Methods.
***********************************************************************************************/

BEGIN_CMD_MAP(FmtFntDlg)
	ON_CID_CHILD(kctidFfdFeatPopup, &FmtFntDlg::CmdFeaturesPopup, NULL)
END_CMD_MAP_NIL()

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FmtFntDlg::FmtFntDlg()
{
	m_rid = kridFmtFntDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/Format/Font.htm");
	FillInts((int *)&m_chrpCur, FwStyledText::knUnspecified, isizeof(m_chrpCur) / isizeof(int));
	m_chrpOld = m_chrpCur;
	m_fBullNum = false;
	m_fBullet = false;
	m_fFeatures = false;
	m_f1DefaultFont = true;
	m_hfontPrvw = NULL;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FmtFntDlg::~FmtFntDlg()
{
	if (m_hfontPrvw)
	{
		AfGdi::DeleteObjectFont(m_hfontPrvw);
		m_hfontPrvw = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Initialize the dialog.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::OnInitDlg(HWND hwnd, LPARAM lp)
{
	StrAppBuf strb;
	HWND hwndT;
	LOGFONT lf;
	HRESULT hr;

	// Center the dialog in the parent window.
	CenterInWindow(::GetParent(m_hwnd));

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	// Initialize the Font selection combo box. Make it be able to show gray text and handle
	// the backspace key the way that is needed for typeahead.
	hwndT = ::GetDlgItem(m_hwnd, kctidFfdFont);

	m_qgecmbFont.Create();
	m_qgecmbFont->SubclassCombo(hwndT);
	m_qgecmbFont->SetMonitor(&(m_chrpi.xFont));

	POINT pt;
	pt.x = 5;
	pt.y = 5;
	HWND hwndFontEdit = ChildWindowFromPoint(hwndT, pt);
	SimpleComboEditPtr qsceFontEdit;
	qsceFontEdit.Create();
	qsceFontEdit->SubclassComboEdit(hwndFontEdit, hwndT);

	ClearItems(&lf, 1);
	lf.lfCharSet = DEFAULT_CHARSET;

	// Get the currently available fonts via the LgFontManager.
	ILgFontManagerPtr qfm;
	SmartBstr bstrNames;

	qfm.CreateInstance(CLSID_LgFontManager);
	hr = qfm->AvailableFonts(&bstrNames);
	static long ipszList = 0; // Returned value from SendMessage.

	StrApp strNameList;
	strNameList.Assign(bstrNames.Bstr(), BstrLen(bstrNames.Bstr())); // Convert BSTR to StrApp.
	int cchLength = strNameList.Length();
	StrApp strName; // Individual font name.
	int ichMin = 0; // Index of the beginning of a font name.
	int ichLim = 0; // Index that is one past the end of a font name.

	// Add the three predefined names to the combo box.
	// Review JohnT: should these come from a resource? Dangerous--they are hard-coded in
	// multiple places.
	Vector<StrUni> vstu;
	FwStyledText::FontUiStrings(m_f1DefaultFont, vstu);
	for (int istu = 0; istu < vstu.Size(); istu++)
	{
		StrApp str(vstu[istu]);
		ipszList = ::SendMessage((HWND)hwndT, CB_ADDSTRING, 0, (LPARAM)str.Chars());
	}

	// Add each font name to the combo box.
	while (ichLim < cchLength)
	{
		ichLim = strNameList.FindCh(L',', ichMin);
		if (ichLim == -1) // i.e., if not found.
		{
			ichLim = cchLength;
		}

		strName.Assign(strNameList.Chars() + ichMin, ichLim - ichMin);
		ipszList = ::SendMessage((HWND)hwndT, CB_ADDSTRING, 0, (LPARAM)strName.Chars());
		// REVIEW JohnT(LarryW): should we check potential error from ipszList?

		ichMin = ichLim + 1;
	}

	// Create color combo boxes. This sets a possibly meaningless initial value,
	// but FillCtls fixes it if need be.
	m_qccmbF.Create();
	m_qccmbF->SubclassButton(::GetDlgItem(m_hwnd, kctidFfdForeClr), &m_clrFore);
	m_clrFore = m_chrpCur.clrFore;
	m_qccmbB.Create();
	m_qccmbB->SubclassButton(::GetDlgItem(m_hwnd, kctidFfdBackClr), &m_clrBack);
	m_clrBack = m_chrpCur.clrBack;
	m_qccmbU.Create();
	m_qccmbU->SubclassButton(::GetDlgItem(m_hwnd, kctidFfdUnderClr), &m_clrUnder);
	m_clrUnder = m_chrpCur.clrUnder;

	// Create a special subclass of an editable combo-box that can show gray text.
	m_qgecmbSize.Create();
	m_qgecmbSize->SubclassCombo(::GetDlgItem(m_hwnd, kctidFfdSize));
	m_qgecmbSize->SetMonitor(&(m_chrpi.xSize));

	// Create a non-editable combo that handles down arrow from no selection.
	m_qbfscOffset.Create();
	m_qbfscOffset->SubclassCombo(::GetDlgItem(m_hwnd, kctidFfdOffset));

	// Fix the size of the underline combo box.
	// ENHANCE JohnT (version 2?) Figure what measurement to use in place of "15" here
	// so it works even when the user does not choose small fonts. (Cf UiColor::SetWindowSize)
	HWND hwndUnder = ::GetDlgItem(m_hwnd, kctidFfdUnder);
	SendMessage(hwndUnder,           // handle to destination window
				CB_SETITEMHEIGHT,    // message to send
				(WPARAM) 0,          // items
				(LPARAM) 15          // empirically determined
	);
	SendMessage(hwndUnder,           // handle to destination window
				CB_SETITEMHEIGHT,    // message to send
				(WPARAM) -1,         // the "selection field" which I presume is the box itself
				(LPARAM) 15          // empirically determined
	);

	// Set the values.
	FillCtls();

	// Disable some controls if called from FmtBulNumDlg with bulleted list selected.
	if (m_fBullNum)
		DisableForBullNum();
	if (m_fBullet)
	{
		// Display the current font item as Wingdings and disable font list box.
		// Not that this is a trick: the real current font is unchanged so that it is still
		// there if the user changes selection to Numbered list.
		hwndT = GetDlgItem(m_hwnd, kctidFfdFont);
		::SetWindowText(hwndT, _T("Wingdings"));
		::EnableWindow(hwndT, false);
	}

	// Hide "Font Features" button if not wanted.
	if (!m_fFeatures)
	{
		hwndT = GetDlgItem(m_hwnd, kctidFfdFeatures);
		::ShowWindow(hwndT, SW_HIDE);
	}
	else
	{
		// Make the "Font Feature" button be a pop-up menu button.
		AfButtonPtr qbtnFeat;
		qbtnFeat.Create();
		qbtnFeat->SubclassButton(m_hwnd, kctidFfdFeatures, kbtPopMenu, NULL, 0);

		// Move the nearby controls down a little.
		Rect rcDlg;
		::GetWindowRect(m_hwnd, &rcDlg);
		POINT ptTL = { 0, 0 };
		::ClientToScreen(m_hwnd, &ptTL);
		int dyTitle = (ptTL.y - rcDlg.top);

		int rgctid[5] = { kctidFfdOffset, kctidFfdPosLabel, kctidFfdByLabel,
			kctidFfdOffsetNum, kctidFfdOffsetSpin };
		for (int ictid = 0; ictid < 5; ictid++)
		{
			Rect rc;
			hwndT = ::GetDlgItem(m_hwnd, rgctid[ictid]);
			::GetWindowRect(hwndT, &rc);
			int w = rc.Width();
			int h = rc.Height();
			rc.left -= rcDlg.left;
			rc.top -= rcDlg.top;
			rc.top -= dyTitle;
			rc.top += 19;
			::MoveWindow(hwndT, rc.left, rc.top, w, h, true);
		}
	}
	EnableFontFeatures();

	return true;
} // FmtFntDlg::OnInitDlg.


/*----------------------------------------------------------------------------------------------
	Initialize the format font dialog with a sequence of TsTextProps (as obtained typically
	from VwSelection::GetSelectionProps). Run the dialog, and adjust vqttp to be the values
	that should be passed to SetSelectionProps if anything is altered. Return true if a change
	is needed.
	Note that TsTextProps that are not changed are therefore set to null. Ones that are
	changed result in a new TsTextProps. The caller becomes responsible for a ref count on
	the new objects.
	@param fBullNum is optional with default 'false'. Disables some controls if 'true'.
	@param fBullet is optional with default 'false'. Displays font as Wingdings & disables list.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::AdjustTsTextProps(HWND hwnd, TtpVec & vqttp, VwPropsVec & vqvpsSoft,
	ILgWritingSystemFactory * pwsf, const achar * pszHelpFile, bool fBullNum, bool fBullet,
	bool fFeatures, bool f1DefaultFont)
{
	AssertPtr(pwsf);

	int cttp = vqttp.Size();
	Assert(cttp > 0);
	Assert(vqvpsSoft.Size() == 0 || cttp == vqvpsSoft.Size());

	LgCharRenderProps chrpHard;
	LgCharRenderProps chrpSoft;
	LgCharRenderProps chrpBogus;
	memset(&chrpHard, 0, sizeof(chrpHard));
	memset(&chrpSoft, 0, sizeof(chrpSoft));
	memset(&chrpBogus, 0, sizeof(chrpBogus));
	ChrpInheritance chrpi;
	SmartBstr sbstrFontFamily;
	StrUni stuFfHard;
	StrUni stuFfSoft;
	SmartBstr sbstrFontVar;
	StrUni stuFvarHard;
	StrUni stuFvarSoft;
	StrUni stuBogus;
	StrUni stuInherit = kstidInherit;
	HRESULT hr;

	chrpi.InitToSoft();

	chrpHard.ttvBold = FwStyledText::knUnspecified;
	chrpHard.ttvItalic = FwStyledText::knUnspecified;
	chrpHard.dympHeight = FwStyledText::knUnspecified;
	chrpHard.dympOffset = FwStyledText::knUnspecified;
	chrpHard.ssv = FwStyledText::knUnspecified;
	chrpHard.unt = FwStyledText::knUnspecified;
	chrpHard.clrFore = (COLORREF)FwStyledText::knUnspecified;
	chrpHard.clrBack = (COLORREF)FwStyledText::knUnspecified;
	chrpHard.clrUnder = (COLORREF)FwStyledText::knUnspecified;

	// Do this loop first for the "inherited" or soft values (i.e., those derived
	// from the styles and/or view contructor) and then for the explicit values.
	// The soft values are in the property store and the hard values are in the ttp.
	// Unfortunately this is kind of convoluted because the MergeIntProp
	// function is designed to mash together the soft and hard values,
	// but we are trying to keep them distinct.
	for (int nHard = 0; nHard <= 1; nHard++)
	{
		bool fHard = (bool)nHard;
		LgCharRenderProps & chrpOrig = (fHard) ? chrpHard : chrpBogus;
		//LgCharRenderProps & chrpCur = chrpSoft;
		StrUni & stuFfOrig = (fHard) ? stuFfHard : stuBogus;
		StrUni & stuFfCur = (fHard) ? stuBogus : stuFfSoft;

		// Load a value for each property for each ttp/vps. If we get a different answer
		// for any of them, change to conflicting.
		int ittp;
		for (ittp = 0; ittp < cttp; ittp++)
		{
			bool fFirst = (ittp == 0);
			// When asking for inherited values, use just the property store.
			ITsTextProps * pttp = (fHard) ? vqttp[ittp] : NULL;
			// When asking for explicit values, use just the text properties.
			IVwPropertyStore * pvps = (fHard) ? NULL : vqvpsSoft[ittp];

			MergeFmtDlgStrProp(pttp, pvps, ktptFontFamily, stuFfOrig, stuFfCur, fFirst,
				chrpi.xFont, fHard);

			// hard values for inverting properties can't be figured out without the
			// inherited information, so pass the appropriate property store on both
			// passes. (JohnT 10 Sep 02: I don't understand in general why we only
			// pass one of them, but this path requires both.)
			MergeFmtDlgIntProp(pttp, vqvpsSoft[ittp], ktptBold, ktpvEnum,
				chrpOrig.ttvBold, chrpSoft.ttvBold, fFirst, chrpi.xBold, fHard, true);
			MergeFmtDlgIntProp(pttp, vqvpsSoft[ittp], ktptItalic, ktpvEnum,
				chrpOrig.ttvItalic, chrpSoft.ttvItalic, fFirst, chrpi.xItalic, fHard, true);

			MergeFmtDlgIntProp(pttp, pvps, ktptSuperscript, ktpvEnum,
				chrpOrig.ssv, chrpSoft.ssv, fFirst, chrpi.xSs, fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptUnderline, ktpvEnum,
				chrpOrig.unt, chrpSoft.unt, fFirst, chrpi.xUnderT, fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptFontSize, ktpvMilliPoint,
				chrpOrig.dympHeight, chrpSoft.dympHeight, fFirst, chrpi.xSize, fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptOffset, ktpvMilliPoint,
				chrpOrig.dympOffset, chrpSoft.dympOffset, fFirst, chrpi.xOffset, fHard);

			MergeFmtDlgIntProp(pttp, pvps, ktptForeColor, ktpvDefault,
				chrpOrig.clrFore, chrpSoft.clrFore, fFirst, chrpi.xFore, fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptBackColor, ktpvDefault,
				chrpOrig.clrBack, chrpSoft.clrBack, fFirst, chrpi.xBack, fHard);
			MergeFmtDlgIntProp(pttp, pvps, ktptUnderColor, ktpvDefault,
				chrpOrig.clrUnder, chrpSoft.clrUnder, fFirst, chrpi.xUnder, fHard);

			if (!fBullet && !fBullNum && fHard)
			{
				MergeIntProp(pttp, pvps, ktptWs, 0, chrpOrig.ws, chrpSoft.ws, fFirst);
			}
		}
	}

	// For drawing the preview we need an ws/ows in the chrp. Take one from the first run.
	ITsTextProps * pttpFirst = vqttp[0];
	int wsFirst, nVar;
	CheckHr(pttpFirst->GetIntPropValues(ktptWs, &nVar, &wsFirst));
	if (fBullet || fBullNum)
	{
		chrpHard.ws = wsFirst;
	}

	// Now make an instance, initialize it, and run it.
	// Note that we need chrpOrig, with 'inherit' as possible values, so that we can tell the
	// difference between two runs where one has a particular value explicitly, and the other
	// inherits the same value.  However, we also need 'Old' and 'Cur', initially identical, so
	// we can tell whether anything changed while running the dialog.  We are actually done with
	// 'Orig' now, except for possible use in the "Remove formatting" dialog
	FmtFntDlgPtr qffd;
	qffd.Create();

	Assert(!fFeatures || (!fBullNum && !fBullet));
	qffd->m_fBullNum = fBullNum;	// Cause OnInitDlg to disable some controls.
	qffd->m_fBullet = fBullet;		// Cause OnInitDlg to set Wingdings and disable font list.
	qffd->m_fFeatures = fFeatures;
	qffd->m_f1DefaultFont = f1DefaultFont;

	stuFfHard = FwStyledText::FontStringMarkupToUi(f1DefaultFont, stuFfHard);
	stuFfSoft = FwStyledText::FontStringMarkupToUi(f1DefaultFont, stuFfSoft);
	FwStyledText::ConvertDefaultFontInput(stuFfHard);
	FwStyledText::ConvertDefaultFontInput(stuFfSoft);
	qffd->SetDlgValues(chrpHard, chrpSoft, stuFfHard, stuFfSoft, chrpi, wsFirst);
	qffd->SetWritingSystemFactory(pwsf);
	qffd->RecordInitialFeatures(vqttp);
	qffd->SetFontVarSettings();
	qffd->SetHelpFile(pszHelpFile);

	// Run the dialog.
	int cidT = qffd->DoModal(hwnd);

	if (cidT == kctidFfdRemove)
	{
		// Obsolete code; Remove Formatting button no longer exists.
		Assert(false);

		RemFmtDlgPtr qrfd;
		qrfd.Create();
		int rgttv[11] = {
			FwStyledText::knUnspecified, FwStyledText::knUnspecified,
			FwStyledText::knUnspecified, FwStyledText::knUnspecified,
			FwStyledText::knUnspecified, FwStyledText::knUnspecified,
			FwStyledText::knUnspecified, FwStyledText::knUnspecified,
			FwStyledText::knUnspecified, FwStyledText::knUnspecified,
			FwStyledText::knUnspecified
		};
		int nVar;
		int nVal;
		for (int ittp = 0; ittp < cttp; ++ittp)
		{
			ITsTextProps * pttp = vqttp[ittp];
			CheckHr(hr = pttp->GetStrPropValue(ktptFontFamily, &sbstrFontFamily));
			if (hr != S_FALSE)
			{
				rgttv[0] = kttvForceOn;
			}
			CheckHr(hr = pttp->GetIntPropValues(ktptFontSize, &nVar, &nVal));
			if (hr != S_FALSE)
			{
				rgttv[1] = kttvForceOn;
			}
			CheckHr(hr = pttp->GetIntPropValues(ktptForeColor, &nVar, &nVal));
			if (hr != S_FALSE)
			{
				rgttv[2] = kttvForceOn;
			}
			CheckHr(hr = pttp->GetIntPropValues(ktptBackColor, &nVar, &nVal));
			if (hr != S_FALSE)
			{
				rgttv[3] = kttvForceOn;
			}
			CheckHr(hr = pttp->GetIntPropValues(ktptUnderline, &nVar, &nVal));
			if (hr != S_FALSE)
			{
				rgttv[4] = kttvForceOn;
			}
			// REVIEW ??(SteveMc): there's no enum value defined for an underline color,
			// but underline color is one of the options in the font/format dialog box
//			CheckHr(hr = pttp->GetIntPropValues(ktptUnderClr, &nVar, &nVal));
//			if (hr != S_FALSE)
//				rgttv[5] = kttvForceOn;
			CheckHr(hr = pttp->GetIntPropValues(ktptBold, &nVar, &nVal));
			if (hr != S_FALSE)
			{
				rgttv[6] = kttvForceOn;
			}
			CheckHr(hr = pttp->GetIntPropValues(ktptItalic, &nVar, &nVal));
			if (hr != S_FALSE)
			{
				rgttv[7] = kttvForceOn;
			}
			CheckHr(hr = pttp->GetIntPropValues(ktptSuperscript, &nVar, &nVal));
			if (hr != S_FALSE)
			{
				switch (nVal)
				{
					case kssvSuper:
						rgttv[8] = kttvForceOn;
						break;
					case kssvSub:
						rgttv[9] = kttvForceOn;
						break;
				}
			}
			CheckHr(hr = pttp->GetIntPropValues(ktptOffset, &nVar, &nVal));
			if (hr != S_FALSE)
			{
				rgttv[10] = kttvForceOn;
			}
		}
		qrfd->SetValue(kctidRfdFont, rgttv[0]);	// Rfd here, as main dialog combo uses FfdFont.
		qrfd->SetValue(kctidRfdSize, rgttv[1]);
		qrfd->SetValue(kctidRfdForeClr, rgttv[2]);
		qrfd->SetValue(kctidRfdBackClr, rgttv[3]);
		qrfd->SetValue(kctidRfdUnder, rgttv[4]);
		qrfd->SetValue(kctidRfdUnderClr, rgttv[5]);
		qrfd->SetValue(kctidRfdBold, rgttv[6]);
		qrfd->SetValue(kctidRfdItalic, rgttv[7]);
		qrfd->SetValue(kctidRfdSuper, rgttv[8]);
		qrfd->SetValue(kctidRfdSub, rgttv[9]);
		qrfd->SetValue(kctidRfdOffset, rgttv[10]);
		if (qrfd->DoModal(hwnd) != kctidOk)
			return false;

		ITsPropsBldrPtr qtpb;
		bool fChanged = false;
		for (int ittp = 0; ittp < cttp; ++ittp)
		{
			ITsTextProps * pttp = vqttp[ittp];
			qtpb = NULL;
			if (qrfd->GetValue(kctidRfdFont) == kttvOff)
			{
				CheckHr(pttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetStrPropValue(ktptFontFamily, NULL));
			}
			if (qrfd->GetValue(kctidRfdSize) == kttvOff)
			{
				if (!qtpb)
					CheckHr(pttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetIntPropValues(ktptFontSize, -1, -1));
			}
			if (qrfd->GetValue(kctidRfdForeClr) == kttvOff)
			{
				if (!qtpb)
					CheckHr(pttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetIntPropValues(ktptForeColor, -1, -1));
			}
			if (qrfd->GetValue(kctidRfdBackClr) == kttvOff)
			{
				if (!qtpb)
					CheckHr(pttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetIntPropValues(ktptBackColor, -1, -1));
			}
			if (qrfd->GetValue(kctidRfdUnder) == kttvOff)
			{
				if (!qtpb)
					CheckHr(pttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetIntPropValues(ktptUnderline, -1, -1));
			}
//			if (qrfd->GetValue(kctidRfdUnderClr) == kttvOff)
//			{
//				if (!qtpb)
//					CheckHr(pttp->GetBldr(&qtpb));
//				CheckHr(qtpb->SetIntPropValues(ktpt, -1, -1));
//			}
			if (qrfd->GetValue(kctidRfdBold) == kttvOff)
			{
				if (!qtpb)
					CheckHr(pttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetIntPropValues(ktptBold, -1, -1));
			}
			if (qrfd->GetValue(kctidRfdItalic) == kttvOff)
			{
				if (!qtpb)
					CheckHr(pttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetIntPropValues(ktptItalic, -1, -1));
			}
			if (qrfd->GetValue(kctidRfdSuper) == kttvOff)
			{
				if (!qtpb)
					CheckHr(pttp->GetBldr(&qtpb));
				CheckHr(hr = pttp->GetIntPropValues(ktptSuperscript, &nVar, &nVal));
				if (nVal == kssvSuper)
					CheckHr(qtpb->SetIntPropValues(ktptSuperscript, -1, -1));
			}
			if (qrfd->GetValue(kctidRfdSub) == kttvOff)
			{
				if (!qtpb)
					CheckHr(pttp->GetBldr(&qtpb));
				CheckHr(hr = pttp->GetIntPropValues(ktptSuperscript, &nVar, &nVal));
				if (nVal == kssvSub)
					CheckHr(qtpb->SetIntPropValues(ktptSuperscript, -1, -1));
			}
			if (qrfd->GetValue(kctidRfdOffset) == kttvOff)
			{
				if (!qtpb)
					CheckHr(pttp->GetBldr(&qtpb));
				CheckHr(qtpb->SetIntPropValues(ktptOffset, -1, -1));
			}
			if (qtpb)
			{
				ITsTextPropsPtr qttp;
				CheckHr(qtpb->GetTextProps(&qttp));
				vqttp[ittp] = qttp;
				fChanged = true;
			}
			else
			{
				vqttp[ittp] = NULL;
			}
		}
		return fChanged;
	}
	else if (cidT != kctidOk)
	{
		return false;
	}
	// --- end of obsolete code ---

	ITsPropsBldrPtr qtpb;

	LgCharRenderProps chrpNew, chrpOld;
	qffd->GetDlgValues(chrpNew, chrpOld);

	bool fChanged = false;
	// Now see what changes we have to deal with.
	for (int ittp = 0; ittp < cttp; ittp++)
	{
		ITsTextProps * pttp = vqttp[ittp];
		qtpb = NULL;
		StrUni stuOldVal;
		StrUni stuNewVal;
		// Font family.
		if (qffd->m_stuFfOld != qffd->m_stuFfCur)
		{
			CheckHr(hr = pttp->GetStrPropValue(ktptFontFamily, &sbstrFontFamily));
			stuOldVal = sbstrFontFamily.Chars();
			// If this particular ttp already has the correct value, do nothing.
			if (stuOldVal != qffd->m_stuFfCur)
			{
				if (!qtpb)
					CheckHr(pttp->GetBldr(&qtpb));
				stuNewVal = qffd->m_stuFfCur;
				stuNewVal = FwStyledText::FontStringUiToMarkup(stuNewVal);
				CheckHr(qtpb->SetStrPropValue(ktptFontFamily, stuNewVal.Bstr()));
			}
		}

		UpdateInvertingProp(pttp, vqvpsSoft[ittp], ktptBold, ktpvEnum, qtpb,
			chrpOld.ttvBold, chrpNew.ttvBold);
		UpdateInvertingProp(pttp, vqvpsSoft[ittp], ktptItalic, ktpvEnum, qtpb,
			chrpOld.ttvItalic, chrpNew.ttvItalic);
		UpdateProp(pttp, ktptSuperscript, ktpvEnum, qtpb, chrpOld.ssv, chrpNew.ssv);
		UpdateProp(pttp, ktptUnderline, ktpvEnum, qtpb, chrpOld.unt, chrpNew.unt);
		UpdateProp(pttp, ktptFontSize, ktpvMilliPoint, qtpb, chrpOld.dympHeight,
			chrpNew.dympHeight);
		UpdateProp(pttp, ktptOffset, ktpvMilliPoint, qtpb, chrpOld.dympOffset,
			chrpNew.dympOffset);
		UpdateProp(pttp, ktptForeColor, ktpvDefault, qtpb, chrpOld.clrFore, chrpNew.clrFore);
		UpdateProp(pttp, ktptBackColor, ktpvDefault, qtpb, chrpOld.clrBack, chrpNew.clrBack);
		UpdateProp(pttp, ktptUnderColor, ktpvDefault, qtpb, chrpOld.clrUnder, chrpNew.clrUnder);

		qffd->UpdateFontVariations(ittp, pttp, qtpb);

		// If anything changed, we now have a props builder that is the new value for this
		// run. Get the new properties.
		if (qtpb)
		{
			ITsTextPropsPtr qttp;
			CheckHr(qtpb->GetTextProps(&qttp));
			vqttp[ittp] = qttp;
			fChanged = true;
		}
		else
		{
			// Nothing changed for this run.
			vqttp[ittp] = NULL;
		}
	}
	return fChanged;
} // FmtFntDlg::AdjustTsTextProps.

/*----------------------------------------------------------------------------------------------
	Set the dialog values.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::SetDlgValues(const LgCharRenderProps & chrp, const LgCharRenderProps & chrpSoft,
	StrUni stuFf, StrUni stuFfSoft, ChrpInheritance & chrpi, int wsPrev)
{
	m_chrpCur = chrp;
	m_chrpOld = chrp;
	m_chrpSoft = chrpSoft;

	m_stuFfOld = stuFf;
	m_stuFfCur = stuFf;
	m_stuFfSoft = stuFfSoft;

	m_chrpi.CopyFrom(chrpi);

	m_wsPrev = wsPrev;

	FillCtls();
}

/*----------------------------------------------------------------------------------------------
	Get the dialog values.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::GetDlgValues(LgCharRenderProps & chrpNew, LgCharRenderProps & chrpOrig)
{
	chrpNew = m_chrpCur;
	chrpOrig = m_chrpOld;
}

/*----------------------------------------------------------------------------------------------
	Fill the controls with their values.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::FillCtls(void)
{
	if (!m_hwnd)
		return;

	HWND hwndT;
	int iv;
	int dypt;
	StrAppBuf strb;

	// Fill the current font item
	hwndT = GetDlgItem(m_hwnd, kctidFfdFont);
	StrApp strT(FontFamily());
	::SetWindowText(hwndT, strT.Chars());

	// Initialise the Size selection combo box.
	hwndT = ::GetDlgItem(m_hwnd, kctidFfdSize);
	::SendMessage(hwndT, CB_RESETCONTENT, 0, 0);

	// Skip the first value in the array (kstidFfdUnspecified).
	m_idyptMinSize = 1;
	Assert(kstidFfdUnspecified > 120); // So the test below will see it as an rid.
	for (iv = m_idyptMinSize; iv < SizeOfArray(g_rgdyptSize); ++iv)
	{
		dypt = g_rgdyptSize[iv];
		if (dypt > g_rgdyptSize[SizeOfArray(g_rgdyptSize) - 1]) // Largest font size listed.
			strb.Clear(); // Load(dypt);
		else
			strb.Format(_T("%d"), dypt);
		::SendMessage(hwndT, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	}

	// Fill the underline type list box.
	hwndT = ::GetDlgItem(m_hwnd, kctidFfdUnder);
	::SendMessage(hwndT, CB_RESETCONTENT, 0, 0);

	//m_istidMinUnder = m_chrpCur.unt != FwStyledText::knConflicting;
	m_istidMinUnder = 0; // yes, include unspecified
	for (iv = m_istidMinUnder; iv < SizeOfArray(g_rgstidUnder); iv++)
	{
		::SendMessage(hwndT, CB_ADDSTRING, 0, (LPARAM)g_rgstidUnder[iv]);
	}

	// Fill one or other (or neither) of the superscript/subscript boxes
	int ttvOn = kttvForceOn; // Set Check won't take a const
	int ttvConf = FwStyledText::knConflicting;
	switch (m_chrpCur.ssv)
	{
	case kssvSuper:
		SetCheck(kctidFfdSuper, ttvOn);
		break;
	case kssvSub:
		SetCheck(kctidFfdSub, ttvOn);
		break;
	case FwStyledText::knConflicting:
	case FwStyledText::knUnspecified:
		SetCheck(kctidFfdSuper, ttvConf);
		SetCheck(kctidFfdSub, ttvConf);
		break;
	default:
		break;
	}

	// Fill the vertical offset list box.
	hwndT = ::GetDlgItem(m_hwnd, kctidFfdOffset);
	::SendMessage(hwndT, CB_RESETCONTENT, 0, 0);

	//m_istidMinOffset = m_chrpCur.dympOffset != FwStyledText::knConflicting;
	m_istidMinOffset = 0;
	for (iv = m_istidMinOffset; iv < SizeOfArray(g_rgstidOffset); iv++)
	{
		strb.Load(g_rgstidOffset[iv]);
		::SendMessage(hwndT, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	}

	// Initialize the spin control.
	UDACCEL udAccel;
	udAccel.nSec = 0;
	udAccel.nInc = kSpnStpPt;

	hwndT = ::GetDlgItem(m_hwnd, kctidFfdOffsetSpin);
	::SendMessage(hwndT, UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(hwndT, UDM_SETRANGE32, (uint) kdyptMinOffset, (int) kdyptMaxOffset);

	SetSize(Size());
	SetCheck(kctidFfdBold, m_chrpCur.ttvBold);
	SetCheck(kctidFfdItalic, m_chrpCur.ttvItalic);
	SetUnder(Underline());
	SetOffset(Offset());

	// Set the colors.
	m_qccmbF->SetColor(ForeColor());
	m_qccmbB->SetColor(BackColor());
	m_qccmbU->SetColor(UnderColor());
} // FmtFntDlg::FillCtls.

/*----------------------------------------------------------------------------------------------
	Disable some of the controls when using this from FmtBulNumDlg.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::DisableForBullNum()
{
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFfdSuper), false);		// Superscript check box.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFfdSub), false);		// Subscript check box.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFfdOffset), false);	// Position drop-down.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFfdOffsetSpin), false);// 'By' spin control.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFfdOffsetNum), false);	// 'By' edit control.
}

/*----------------------------------------------------------------------------------------------
	Set the color of the control based on whether the value is inherited.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::ColorForInheritance(WPARAM wp, LPARAM lp, long & lnRet)
{
	HWND hwndFont = ::GetDlgItem(m_hwnd, kctidFfdFont);
	HWND hwndSize = ::GetDlgItem(m_hwnd, kctidFfdSize);
	HWND hwndUnderT = ::GetDlgItem(m_hwnd, kctidFfdUnder);
	HWND hwndOffsetLst = ::GetDlgItem(m_hwnd, kctidFfdOffset);
	HWND hwndOffsetNum = ::GetDlgItem(m_hwnd, kctidFfdOffsetNum);
//	HWND hwndBold = ::GetDlgItem(m_hwnd, kctidFfdBold);
//	HWND hwndItalic = ::GetDlgItem(m_hwnd, kctidFfdItalic);
//	HWND hwndSuper = ::GetDlgItem(m_hwnd, kctidFfdSuper);
//	HWND hwndSub = ::GetDlgItem(m_hwnd, kctidFfdSub);
	HWND hwndForeC = ::GetDlgItem(m_hwnd, kctidFfdForeClr);
	HWND hwndBackC = ::GetDlgItem(m_hwnd, kctidFfdBackClr);
	HWND hwndUnderC = ::GetDlgItem(m_hwnd, kctidFfdUnderClr);

	HWND hwndArg = (HWND)lp;

	int xExpl;
	if (hwndArg == hwndFont)
		xExpl = m_chrpi.xFont;
	else if (hwndArg == hwndSize)
		xExpl = m_chrpi.xSize;
	else if (hwndArg == hwndUnderT)
		xExpl = m_chrpi.xUnderT;
	else if (hwndArg == hwndOffsetLst || hwndArg == hwndOffsetNum)
		xExpl = m_chrpi.xOffset;
	// Check boxes aren't handled this way.
//	else if (hwndArg == hwndBold)
//		xExpl = m_chrpi.xBold;
//	else if (hwndArg == hwndItalic)
//		xExpl = m_chrpi.xItalic;
//	else if (hwndArg == hwndSuper || hwndArg == hwndSub)
//		xExpl = m_chrpi.xSs;
	else if (hwndArg == hwndForeC)
	{
		xExpl = m_chrpi.xFore;
		if (xExpl == kxHard)
			m_qccmbF->SetLabelColor(::GetSysColor(COLOR_WINDOWTEXT));
		else if (xExpl == kxSoft || xExpl == kxConflicting)
			m_qccmbF->SetLabelColor(::GetSysColor(COLOR_3DSHADOW));
		else
			m_qccmbF->SetLabelColor(kclrGreen); // bug
		// I don't know why we return false here, but when we returned true, the control
		// flashed bizarrely. - SharonC
		return false;
	}
	else if (hwndArg == hwndBackC)
	{
		xExpl = m_chrpi.xBack;
		if (xExpl == kxHard)
			m_qccmbB->SetLabelColor(::GetSysColor(COLOR_WINDOWTEXT));
		else if (xExpl == kxSoft || xExpl == kxConflicting)
			m_qccmbB->SetLabelColor(::GetSysColor(COLOR_3DSHADOW));
		else
			m_qccmbB->SetLabelColor(kclrGreen); // bug
		return false;
	}
	else if (hwndArg == hwndUnderC)
	{
		xExpl = m_chrpi.xUnder;
		if (xExpl == kxHard)
			m_qccmbU->SetLabelColor(::GetSysColor(COLOR_WINDOWTEXT));
		else if (xExpl == kxSoft || xExpl == kxConflicting)
			m_qccmbU->SetLabelColor(::GetSysColor(COLOR_3DSHADOW));
		else
			m_qccmbU->SetLabelColor(kclrGreen); // bug

		return false;
	}
	else
		return false;

	if (xExpl == kxHard)
		::SetTextColor((HDC)wp, ::GetSysColor(COLOR_WINDOWTEXT)); // black
	else if (xExpl == kxSoft || xExpl == kxConflicting)
		::SetTextColor((HDC)wp, ::GetSysColor(COLOR_3DSHADOW)); // gray
	else
		::SetTextColor((HDC)wp, kclrGreen); // bug

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
	If the value has been changed to inherited, update the combo-box to show
	the inherited value. We want to do this after the combo has lost focus so the gray
	color will show; otherwise it could look like the combo isn't working.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::UpdateComboWithInherited(int ctid, NMHDR * pnmh)
{
//	OutputDebugString("Combo lost focus - update\n");

	if (ctid == kctidFfdUnder)
	{
		if (m_chrpi.xUnderT == kxSoft)
			SetUnder(m_chrpSoft.unt);
	}
	if (ctid == kctidFfdOffset || ctid == kctidFfdOffsetNum)
	{
		if (m_chrpi.xOffset == kxSoft)
			SetOffset(m_chrpSoft.dympOffset);
	}
	if (ctid == kctidFfdForeClr)
	{
		if (m_chrpi.xFore == kxSoft)
			m_qccmbF->SetColor(m_chrpSoft.clrFore);
	}
	if (ctid == kctidFfdBackClr)
	{
		if (m_chrpi.xBack == kxSoft)
			m_qccmbB->SetColor(m_chrpSoft.clrBack);
	}
	if (ctid == kctidFfdUnderClr)
	{
		if (m_chrpi.xUnder == kxSoft)
			m_qccmbU->SetColor(m_chrpSoft.clrUnder);
	}
}

/*----------------------------------------------------------------------------------------------
	Obtain a pointer to the renderer for the current font for the first writing system in
	the input text. If it supports IID_IRenderingFeatures, set a pointer to that in
	m_qrenfeat and return true;
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::SetFeatureEngine(IRenderEngine ** ppreneng)
{
	m_qrenfeat.Clear();
	LgCharRenderProps chrpTmp = m_chrpCur;
	// For some reason we don't store the font name in m_chrpCur, but it needs to be
	// copied to the chrp for the factory to find the right renderer.
	wcscpy_s(chrpTmp.szFaceName, m_stuFfCur.Chars());
	chrpTmp.ws = m_wsFl;
	CheckHr(m_qwsf->get_RendererFromChrp(&chrpTmp, ppreneng));
	Assert(*ppreneng);
	return (*ppreneng)->QueryInterface(IID_IRenderingFeatures, (void **)&m_qrenfeat) == S_OK;
}

/*----------------------------------------------------------------------------------------------
	Set up the data structures to handle the Font Features button.
	Note that m_qrenfeat may be left = NULL when the Font Features button is not active.
	Assumes that m_chrpOld.ws has been set to the relevant writing system, or is FwStyledText::knConflicting
	otherwise.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::SetFontVarSettings()
{
	m_qrenfeat = NULL;

	if (!m_fFeatures)
		return;

	// Even if by some strange chance several fonts share features in common, don't
	// allow the Font Features unless there is a uniform font.
	if (m_stuFfCur.Length() == 0) // string is empty => font name conflicting
		// Theoretically, if bold and italic values differ, the fonts could have different
		// features available. But that is a pretty pathological case, so it seems safer and
		// friendlier to allow them to change the features even with a mixture of styles.
//		|| m_chrpCur.ttvBCur == FwStyledText::knConflicting
//		|| m_chrpCur.ttvItalic == FwStyledText::knConflicting)
	{
		m_flCur.MakeConflicting();
		return;
	}

	AssertPtr(m_qwsf);
	IWritingSystemPtr qws;
	CheckHr(m_qwsf->get_EngineOrNull(m_wsFl, &qws));
	if (!qws)
	{
		Assert(false);
		return; // give up in despair
	}

	// Get the renderer that this ws uses for this font
	IRenderEnginePtr qreneng;
	if (!SetFeatureEngine(&qreneng))
		return;

	// Now set up the merged list, which interacts with the menu.
	int cfeat;
	CheckHr(m_qrenfeat->GetFeatureIDs(0, NULL, &cfeat));
	int cttp = m_vfl.Size();
	for (int ifeat = 0; ifeat < cfeat; ifeat++)
	{
		m_flCur.rgn[ifeat] = m_vfl[0].rgn[ifeat];
		for (int ittp = 1; ittp < cttp; ittp++)
		{
			if (m_vfl[ittp].rgn[ifeat] != m_flCur.rgn[ifeat])
			{
				m_flCur.rgn[ifeat] = FwStyledText::knConflicting;
				break;
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the feature data structures.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::RecordInitialFeatures(TtpVec & vqttp)
{
	// Record the first writing system, which we will use to create the
	// right renderer with the selected font, in order to get the features.
	// Strictly speaking, each writing system would have its own renderer for
	// that font, but for the purpose of getting features, it doesn't matter.
	int nBogusVar;
	CheckHr(vqttp[0]->GetIntPropValues(ktptWs, &nBogusVar, &m_wsFl));
	// Try to set m_qrenfeat, which is used below.
	IRenderEnginePtr qreneng;
	SetFeatureEngine(&qreneng);

	if (m_chrpCur.ws != FwStyledText::knConflicting)
	{
		// Record the default features for the writing system.
		IWritingSystemPtr qws;
		CheckHr(m_qwsf->get_EngineOrNull(m_chrpOld.ws, &qws));
		if (qws)
		{
			// Fill in the defaults inherited from the writing system--only
			// if there is exactly one writing system for the selected text.
			SmartBstr sbstrWsDef;
			CheckHr(qws->get_FontVariation(&sbstrWsDef));
			StrUni stuWsDef(sbstrWsDef.Chars());
			m_flWs.Init();
			if (m_qrenfeat)
				ParseFeatureString(m_qrenfeat, stuWsDef, m_flWs.rgn);

			BSTR bstrDefSerif;
			CheckHr(qws->get_DefaultSerif(&bstrDefSerif));
			m_stuFlWs.Assign(bstrDefSerif);
		}
	}
	else
	{
		m_flWs.MakeConflicting();
		m_stuFlWs.Clear();
	}

	// Set up a list of features for each run.
	int cttp = vqttp.Size();
	Assert(cttp >= 1);
	m_vfl.Resize(cttp);
	for (int ittp = 0; ittp < cttp; ittp++)
	{
		SmartBstr sbstrFontVar;
		CheckHr(vqttp[ittp]->GetStrPropValue(ktptFontVariations, &sbstrFontVar));
		StrUni stuFontVar(sbstrFontVar.Chars());
		m_vfl[ittp].Init();
		if (m_qrenfeat)
			ParseFeatureString(m_qrenfeat, stuFontVar, m_vfl[ittp].rgn);
	}

}


/*----------------------------------------------------------------------------------------------
	Set the enabling for the Font Features button.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::EnableFontFeatures()
{
	bool fEnable = false;
	if (m_qrenfeat)
		fEnable = HasFeatures(m_qrenfeat);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFfdFeatures), fEnable);

	// Currently we always enable this control; previously, only if not Graphite.
	bool fEnableFont = true; // (m_qrenfeat.Ptr() == NULL);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFfdFont), fEnableFont);
}

/*----------------------------------------------------------------------------------------------
	Compare the new list of font features with those for the given run. If necessary, update
	the property for this run.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::UpdateFontVariations(int ittp, ITsTextProps * pttp, ITsPropsBldrPtr & qtpb)
{
	if (!m_qrenfeat)
		return;

	// Copy feature settings back in to the lists for each run.
	int cfeat;
	CheckHr(m_qrenfeat->GetFeatureIDs(0, NULL, &cfeat));
	bool fMod = false;
	for (int ifeat = 0; ifeat < cfeat; ifeat++)
	{
		if (m_flCur.rgn[ifeat] != FwStyledText::knConflicting && m_flCur.rgn[ifeat] != INT_MAX)
		{
			m_vfl[ittp].rgn[ifeat] = m_flCur.rgn[ifeat];
			fMod = true;
		}
	}
	if (!fMod)
		return;

	StrUni stuFvarNew = GenerateFeatureString(m_qrenfeat, m_vfl[ittp].rgn);

	SmartBstr sbstrFvarOld;
	CheckHr(pttp->GetStrPropValue(ktptFontVariations, &sbstrFvarOld));
	if (stuFvarNew == sbstrFvarOld.Chars())
		return; // nothing changed

	// Update the property.
	if (!qtpb)
		CheckHr(pttp->GetBldr(&qtpb));
	CheckHr(qtpb->SetStrPropValue(ktptFontVariations, stuFvarNew.Bstr()));
}

/*----------------------------------------------------------------------------------------------
	Create a menu for selecting features. The features are in the main menu, with sub-menus
	containing each of the values.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::CreateFeaturesMenu()
{
	if (!m_qrenfeat || !HasFeatures(m_qrenfeat))
	{
		// Can't get any features, therefore no menu.
		// If HasFeatures answers false, the button should be disabled anyway.
		if (m_hmenuFeatures)
		{
			::DestroyMenu(m_hmenuFeatures);
			m_hmenuFeatures = 0;
		}
		return;
	}

	// For now, always create a new menu, to make sure the check marks are right.
	// ENHANCE: keep the same menu, just update the check marks.
	::DestroyMenu(m_hmenuFeatures);
	m_hmenuFeatures = 0;

	if (!m_hmenuFeatures)
	{
		m_vnFeatMenuIDs.Clear();
		m_hmenuFeatures = ::CreatePopupMenu();
		if (!m_hmenuFeatures)
			ThrowHr(WarnHr(E_FAIL));

		int nLang = 0x00000409;	// for now the UI language is US English

		if (!SetupFeaturesMenu(m_hmenuFeatures, m_qrenfeat, nLang,
			m_vnFeatMenuIDs, m_flCur.rgn, m_flWs.rgn))
		{
			// No valid items for any of the features. Delete the menu.
			::DestroyMenu(m_hmenuFeatures);
			m_hmenuFeatures = 0;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Handle a popup menu command for choosing the desired font features.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::CmdFeaturesPopup(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand);

	// The user selected an expanded menu item, so perform the command now.
	//    m_rgn[1] holds the menu handle.
	//    m_rgn[2] holds the index of the selected item.

	int i = pcmd->m_rgn[2];

	if (m_qrenfeat)
		FmtFntDlg::HandleFeaturesMenu(m_qrenfeat, i, m_vnFeatMenuIDs, m_flCur.rgn);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Set the font size control.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::SetSize(int dymp)
{
	StrAppBuf strb;

	switch (dymp)
	{
	case FwStyledText::knConflicting:
		::SendDlgItemMessage(m_hwnd, kctidFfdSize, CB_SETCURSEL, -m_idyptMinSize, 0);
		break;
	case FwStyledText::knUnspecified:
		::SendDlgItemMessage(m_hwnd, kctidFfdSize, CB_SETCURSEL, 1 - m_idyptMinSize, 0);
		break;
	default:
		strb.Format(_T("%d"), dymp / 1000);
		::SendDlgItemMessage(m_hwnd, kctidFfdSize, WM_SETTEXT, 0, (LPARAM)strb.Chars());
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	Set a toggle check mark. If fAdvance is true, advance the check box to its next state
	(for when clicked). If fAllowConflict is true, the grayed state is allowed.
	Ttv is modified to one of kttvInvert, kttvOff, or (if fAllocConflict is true) FwStyledText::knConflicting.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::SetCheck(int ctid, int & ttv, bool fAdvance, bool fAllowConflict)
{
	int bst;

	if (fAdvance)
	{
		switch (ttv)
		{
		case FwStyledText::knConflicting:
		case FwStyledText::knUnspecified:
			ttv = kttvInvert;
			break;
		case kttvForceOn:
		case kttvInvert:
			ttv = kttvOff;
			break;
		case kttvOff:
		default:
			if (fAllowConflict)
				ttv = FwStyledText::knConflicting;
			else
				ttv = FwStyledText::knUnspecified;
			break;
		}
	}

	switch (ttv)
	{
	case FwStyledText::knConflicting:
	case FwStyledText::knUnspecified:
		bst = BST_INDETERMINATE;
		break;
	case kttvForceOn:
	case kttvInvert:
		bst = BST_CHECKED;
		break;
	case kttvOff:
	default:
		bst = BST_UNCHECKED;
		break;
	}
	::SendDlgItemMessage(m_hwnd, ctid, BM_SETCHECK, bst, 0);
} // FmtFntDlg::SetCheck.

/*----------------------------------------------------------------------------------------------
	Set the underline combo box control.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::SetUnder(int unt)
{
	if (unt == FwStyledText::knConflicting)
	{
		::SendDlgItemMessage(m_hwnd, kctidFfdUnder, CB_SETCURSEL, (WPARAM)-1, 0);
		return;
	}

	int iv;
	for (iv = 0; ; iv++)
	{
		if (iv >= SizeOfArray(g_rguntUnder))
		{
			iv = 1;
			break;
		}
		if (unt == g_rguntUnder[iv])
			break;
	}

	iv -= m_istidMinUnder;

	::SendDlgItemMessage(m_hwnd, kctidFfdUnder, CB_SETCURSEL, iv, 0);
}

/*----------------------------------------------------------------------------------------------
	Set the vertical offset control values.

	WARNING: This method has the side effect of updating the m_esiSel.m_mpOffset and
	m_chrpi.xOffset variables (when it calls SendDlgItemMessage). So any callers who are not
	using those values as the basis of the control need to save the old values of these
	variables and restore them.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::SetOffset(int nValue)
{
	if (nValue == FwStyledText::knConflicting || nValue == FwStyledText::knUnspecified)
	{
		Assert(m_istidMinOffset == 0);
		::SendDlgItemMessage(m_hwnd, kctidFfdOffsetNum, WM_SETTEXT, 0, (LPARAM)_T(""));
		::SendDlgItemMessage(m_hwnd, kctidFfdOffset, CB_SETCURSEL,
			(nValue == FwStyledText::knConflicting ? -1 : 0), 0);
		return;
	}

	// Don't exceed the minimum or maximum values in the spin control.
	StrAppBuf strb;
	strb.SetLength(strb.kcchMaxStr);
	int nRangeMin = 0;
	int nRangeMax = 0;
	int icombo;
	::SendDlgItemMessage(m_hwnd, kctidFfdOffsetSpin, UDM_GETRANGE32, (WPARAM)&nRangeMin,
		(LPARAM)&nRangeMax);
	nValue = NBound(nValue, nRangeMin, nRangeMax);

	// Update the edit box.
	int nVal = Abs(nValue);
	AfUtil::MakeMsrStr (nVal , knpt, &strb);

	::SendDlgItemMessage(m_hwnd, kctidFfdOffsetNum, WM_SETTEXT, 0, (LPARAM)strb.Chars());
	m_chrpCur.dympOffset = nValue;
	if (nValue < 0)
		icombo = 3; // Lowered
	else if (nValue > 0)
		icombo = 2;	// Raised
	else
		icombo = 1;	// Normal

	::SendDlgItemMessage(m_hwnd, kctidFfdOffset, CB_SETCURSEL, icombo - m_istidMinOffset, 0);
}

/*----------------------------------------------------------------------------------------------
	The OK button was pushed.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::OnApply(bool fClose)
{
	int dypt;
	StrAppBuf strb;

	// Get the text from the edit box and convert it to a number.
	strb.SetLength(strb.kcchMaxStr);
	int cch = ::SendDlgItemMessage(m_hwnd, kctidFfdSize, WM_GETTEXT, strb.kcchMaxStr,
		(LPARAM)strb.Chars());
	strb.SetLength(cch);

	if (cch == 0)
	{
		if (m_chrpOld.dympHeight == FwStyledText::knConflicting)
			dypt = FwStyledText::knConflicting;
		else if (m_chrpOld.dympHeight == FwStyledText::knUnspecified)
			dypt = FwStyledText::knUnspecified;
		else
			dypt = m_chrpOld.dympHeight / 1000;
	}
	else
	{
		dypt = StrUtil::ParseInt(strb.Chars());
	}

	if (!IsValidFontSize(dypt) && dypt != FwStyledText::knConflicting &&
		dypt != FwStyledText::knUnspecified)
	{
		StrApp strMessage(kstidFfdRange);
		::MessageBox(m_hwnd, strMessage.Chars(), NULL, 0);

		m_chrpCur.dympHeight = m_chrpOld.dympHeight;

		strb.Format(_T("%d"), m_chrpOld.dympHeight / 1000);
		::SendDlgItemMessage(m_hwnd, kctidFfdSize, WM_SETTEXT, 0, (LPARAM)strb.Chars());

		// Set focus back to font size combobox.
		::SetFocus(::GetDlgItem(m_hwnd, kctidFfdSize));

		// Update preview.
		::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);
		return false;
	}

	return AfDialog::OnApply(fClose);
} // FmtFntDlg::OnApply.

/*----------------------------------------------------------------------------------------------
	A combo box changed (ie, they clicked in the list). Handle it.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::OnComboChange(NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	int dypt; // Font size.

	if (pnmh->idFrom == kctidFfdForeClr)
		return SuperClass::OnNotifyChild(pnmh->idFrom, pnmh, lnRet);
	if (pnmh->idFrom == kctidFfdBackClr)
		return SuperClass::OnNotifyChild(pnmh->idFrom, pnmh, lnRet);
	if (pnmh->idFrom == kctidFfdUnderClr)
		return SuperClass::OnNotifyChild(pnmh->idFrom, pnmh, lnRet);

	// Get the current index from the combo box.
	int iv = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);

	switch (pnmh->idFrom)
	{
	case kctidFfdSize:
		iv += m_idyptMinSize;
		if (iv < 0 || iv >= SizeOfArray(g_rgdyptSize))
		{
			m_chrpCur.dympHeight = FwStyledText::knUnspecified;
			m_chrpi.xSize = kxSoft;
			//m_chrpCur.dympHeight = m_chrpOld.dympHeight;
		}
		else
		{
			dypt = g_rgdyptSize[iv];
			if (dypt == kstidFfdUnspecified)
			{
				m_chrpCur.dympHeight = FwStyledText::knUnspecified;
				m_chrpi.xSize = kxSoft;
			}
			else
			{
				m_chrpCur.dympHeight = dypt * 1000;
				m_chrpi.xSize = kxHard;
			}
		}
		SetSize(Size());
		break;

	case kctidFfdUnder:
		iv += m_istidMinUnder;
		if (iv < 1 || iv >= SizeOfArray(g_rguntUnder))
		{
			// 0 = unspecified
			m_chrpCur.unt = FwStyledText::knUnspecified;
			m_chrpi.xUnderT = kxSoft;
		}
		else
		{
			m_chrpCur.unt = g_rguntUnder[iv];
			m_chrpi.xUnderT = kxHard;
		}
		break;

	case kctidFfdOffset:
		iv += m_istidMinOffset;
		// For now, leave the value explicit, even if they selected unspecified; we'll fix
		// it when the control loses focus.
		if (iv < 0 || iv >= SizeOfArray(g_rgdympOffset))
		{
			m_chrpCur.dympOffset = FwStyledText::knUnspecified;
			m_chrpi.xOffset = kxSoft;
		}
		else
		{
			int dymp = g_rgdympOffset[iv];
			if (dymp == 0 || dymp == FwStyledText::knConflicting)
			{
				m_chrpCur.dympOffset = dymp;
			}
			else if (m_chrpCur.dympOffset == FwStyledText::knConflicting || m_chrpCur.dympOffset == 0)
			{
				m_chrpCur.dympOffset = dymp;  // intialize to our default suggestion
			}
			else if ((m_chrpCur.dympOffset < 0) != (dymp < 0))
			{
				m_chrpCur.dympOffset = -m_chrpCur.dympOffset; // switch directions
			}
			else
			{
				lnRet = 0;
				return true;
			}
			m_chrpi.xOffset = kxHard;
		}
		SetOffset(Offset());
		break;

	case kctidFfdFont:
		{ // BLOCK
			// This message is sent BEFORE the automatic update of the text box, so we
			// can't just read its contents. Instead, read the current item.
			achar rgch[MAX_PATH];
			Vector<achar> vch;
			achar * pszT;
			int cch = ::SendDlgItemMessage(m_hwnd, kctidFfdFont, CB_GETLBTEXTLEN, iv, 0);
			if (cch < MAX_PATH)
			{
				pszT = rgch;
			}
			else
			{
				vch.Resize(cch + 1);
				pszT = vch.Begin();
			}
			cch = ::SendDlgItemMessage(m_hwnd, kctidFfdFont, CB_GETLBTEXT, iv, (LPARAM)pszT);
			if (cch >= 0)
			{	// cch = -1 if user clicks down in list box and then up outside it.
				m_stuFfCur = pszT;
				m_chrpi.xFont = kxHard;
			}
			SetFontVarSettings();
			EnableFontFeatures();
		}
		break;

	default:
		return false;
	}
	// Update preview.
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);
	lnRet = 0;
	return true;
} // FmtFntDlg::OnComboChange.

/*----------------------------------------------------------------------------------------------
	Typing occurred in the text field of a combo box, before the screen is updated.

	TODO: determine how this should really work and implement it. (The current approach
	is probably not right.)
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::OnComboUpdate(NMHDR * pnmh, long & lnRet)
{
	switch (pnmh->idFrom)
	{
	case kctidFfdFont:
		{ // BLOCK (for var init skips)
			// Make sure font name is something legal in the list, ie, truncate it until it
			// matches.
//			int iv; iv = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);

			StrAppBuf strb;
			int cch = ::SendDlgItemMessage(m_hwnd, kctidFfdFont, WM_GETTEXT, strb.kcchMaxStr,
				(LPARAM)strb.Chars());
			strb.SetLength(cch);

			DWORD ichSelMin;
			DWORD ichSelLim;
			::SendDlgItemMessage(m_hwnd, kctidFfdFont, CB_GETEDITSEL,
				(WPARAM)&ichSelMin, (LPARAM)&ichSelLim);

			int cchCmp = (int)ichSelMin;
			int cchOrig = cchCmp;

			// Get the currently available fonts via the LgFontManager.
			ILgFontManagerPtr qfm;
			SmartBstr bstrNames;
			Vector<StrApp> vstrFontNames;
			// Add the three predefined names to the list.
			Vector<StrUni> vstuDefaults;
			FwStyledText::FontUiStrings(m_f1DefaultFont, vstuDefaults);
			for (int istu = 0; istu < vstuDefaults.Size(); istu++)
			{
				StrApp str(vstuDefaults[istu]);
				vstrFontNames.Push(str);
			}
			qfm.CreateInstance(CLSID_LgFontManager);
			CheckHr(qfm->AvailableFonts(&bstrNames));
			static long ipszList = 0; // Returned value from SendMessage.

			StrApp strNameList;
			strNameList.Assign(bstrNames.Bstr(), BstrLen(bstrNames.Bstr()));
			int cchLength = strNameList.Length();
			StrApp strName; // Individual font name.
			int ichMin = 0; // Index of the beginning of a font name.
			int ichLim = 0; // Index that is one past the end of a font name.

			// Add each font name to the list.
			while (ichLim < cchLength)
			{
				ichLim = strNameList.FindCh(L',', ichMin);
				if (ichLim == -1) // i.e., if not found.
				{
					ichLim = cchLength;
				}
				strName.Assign(strNameList.Chars() + ichMin, ichLim - ichMin);
				vstrFontNames.Push(strName);
				ichMin = ichLim + 1;
			}

			int istrMatched = -1;
			while (cchCmp > 0)
			{
				int cbCmp = cchCmp * isizeof(achar);
				for (int istr = 0; istr < vstrFontNames.Size(); istr++)
				{
					if (_tcsnicmp(vstrFontNames[istr].Chars(), strb.Chars(), cbCmp) == 0)
					{
						// Found a match.
						istrMatched = istr;
						break;
					}
				}
				if (istrMatched >= 0)
					break;
				// Try taking off a character.
				cchCmp--;
			}
			if (cchCmp < cchOrig)
			{
				// Had to truncate.
				::MessageBeep(MB_ICONEXCLAMATION);
			}

			if (istrMatched > -1)
			{
				StrApp strGoodName(vstrFontNames[istrMatched]);
				::SendDlgItemMessage(m_hwnd, kctidFfdFont, WM_SETTEXT, NULL,
					(LPARAM)strGoodName.Chars());
			}
			else
			{
				::SendDlgItemMessage(m_hwnd, kctidFfdFont, WM_SETTEXT, NULL, (LPARAM)_T(""));
			}

			// Update the color of the text in the control.
			if (cchCmp > 0)
			{
				m_chrpi.xFont = kxHard;
				long ln;
				// Passing 0 for the first argument seems to work, although I don't see how.
				ColorForInheritance(0, (LPARAM)::GetDlgItem(m_hwnd, kctidFfdFont), ln);
			}

			// Select everything after the matched characters.
			::SendDlgItemMessage(m_hwnd, kctidFfdFont, CB_SETEDITSEL, 0, MAKELONG(cchCmp, -1));

		}
		break;

	case kctidFfdSize:
		{
			// When typing, update the color of the text in the control.
			StrAppBuf strb;
			int cch = ::SendDlgItemMessage(m_hwnd, kctidFfdSize, WM_GETTEXT, strb.kcchMaxStr,
				(LPARAM)strb.Chars());
			strb.SetLength(cch);
			if (cch > 0)
			{
				m_chrpi.xSize = kxHard;
				long ln;
				// Passing 0 for the first argument seems to work, although I don't see how.
				ColorForInheritance(0, (LPARAM)::GetDlgItem(m_hwnd, kctidFfdSize), ln);
			}
		}
		break;

	default:
		return false;
	}
	lnRet = 0;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Moving the focus away from the given control.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::OnKillFocus(NMHDR * pnmh, long & lnRet)
{
	int dypt;
	int iv;
	int cch;
	StrAppBuf strb;

	switch (pnmh->idFrom)
	{
	case kctidFfdSize:
		{ // BLOCK (for var init skips)
			// Get the text from the edit box and convert it to a number.
			cch = ::SendDlgItemMessage(m_hwnd, kctidFfdSize, WM_GETTEXT, strb.kcchMaxStr,
				(LPARAM)strb.Chars());
			strb.SetLength(cch);

			if (cch == 0)
			{
				if (m_chrpOld.dympHeight == FwStyledText::knConflicting)
					// Don't set kxSoft if the original size was 'blank' & is presently blank.
					m_chrpCur.dympHeight = FwStyledText::knConflicting;
				else
				{
					m_chrpCur.dympHeight = FwStyledText::knUnspecified;
					m_chrpi.xSize = kxSoft;
				}
				SetSize(Size());
				// Update preview.
				::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);
				return true;
			}

			dypt = StrUtil::ParseInt(strb.Chars());

			if (dypt > 0)
			{
				// Check whether dypt is within the range of font sizes.
				if (IsValidFontSize(dypt))
				{
					if (m_chrpi.xSize == kxHard // will be if they typed in the ctrl
						|| dypt * 1000 != m_chrpSoft.dympHeight)
					{
						m_chrpCur.dympHeight = dypt * 1000;
						m_chrpi.xSize = kxHard;
					}
					// ...otherwise: perhaps they just pressed the tab key and didn't type
					// anything.
					// Update preview.
					::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);
					lnRet = 0;
				}
				SetSize(Size());
				return true;
			}

			iv = ::SendDlgItemMessage(m_hwnd, kctidFfdSize, CB_FINDSTRING, 0,
				(LPARAM)strb.Chars());
			if (iv == CB_ERR)
			{
				SetSize(m_chrpCur.dympHeight);
				::SendDlgItemMessage(m_hwnd, kctidFfdSize, CB_SETEDITSEL, 0, MAKELONG(0, -1));
				return true;
			}

			//it looks like we get here only if the value entered is <= 0 and that value is
			//found in the size combo box. The size combo box shouldn't contain a values <= 0
			//I'm not quite sure how all this works though, so the assert may not be justified.
			Assert(false);
			iv += m_idyptMinSize;
			if (iv < 0 || iv >= SizeOfArray(g_rgdyptSize))
				m_chrpCur.dympHeight = m_chrpOld.dympHeight;
			else
			{
				dypt = g_rgdyptSize[iv];
				if (dypt == kstidFfdUnspecified)
					m_chrpCur.dympHeight = FwStyledText::knConflicting;
				else
					m_chrpCur.dympHeight = m_chrpOld.dympHeight;
			}
		}
		return true;

	case kctidFfdFont:
		{ // BLOCK
			StrAppBuf strb;
			cch = ::SendDlgItemMessage(m_hwnd, kctidFfdFont, WM_GETTEXT, strb.kcchMaxStr,
				(LPARAM)strb.Chars());
			strb.SetLength(cch);
			if (cch == 0 && m_stuFfCur.Length() == 0)
				return true;	// Was null and still is: do nothing.
			m_stuFfCur = strb.Chars();
			if (m_chrpi.xFont == kxSoft && m_stuFfCur == m_stuFfSoft)
			{
				// They just pressed tab and didn't type anything.
				m_stuFfCur = L"";
			}
			else if (!(m_stuFfOld.Length() == 0 && cch == 0))
			{
				// The selection spanned runs with different fonts originally and the user
				// has returned to this situation, though they may have entered a font in the
				// mean time, without clicking OK.
				m_chrpi.xFont = (cch == 0) ? kxSoft : kxHard;
			}

			StrApp str = FontFamily();
			::SendDlgItemMessage(m_hwnd, kctidFfdFont, WM_SETTEXT, NULL, (LPARAM)str.Chars());

			// Update preview.
			::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);

			// Review JohnT: do we need to do something about scrolling the list?
		}
		return true;
	case kctidFfdOffset:
		{
			//HWND hwndOffset = ::GetDlgItem(m_hwnd, kctidFfdOffset);
			HWND hwndSpin = ::GetDlgItem(m_hwnd, kctidFfdOffsetSpin);
			HWND hwndEdit = (HWND)::SendMessage(hwndSpin, UDM_GETBUDDY, 0, 0);
			StrAppBuf strb;
			int cch = ::SendMessage(hwndEdit, WM_GETTEXT, strb.kcchMaxStr,
				(LPARAM)strb.Chars());
			strb.SetLength(cch);
			int icombo = ::SendDlgItemMessage(m_hwnd, kctidFfdOffset, CB_GETCURSEL, 0, 0)
				+ m_istidMinOffset;
			if (icombo == 0 || cch == 0 || m_chrpCur.dympOffset == FwStyledText::knUnspecified)
			{
				m_chrpCur.dympOffset = FwStyledText::knUnspecified;
				m_chrpi.xOffset = kxSoft;
				SetOffset(Offset());
			}
		}
		return true;
	default:
		UpdateComboWithInherited(pnmh->idFrom, pnmh);
	}

	return false;
} // FmtFntDlg::OnKillFocus.


/*----------------------------------------------------------------------------------------------
	Handles a click on a spin control.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::OnDeltaSpin(NMHDR * pnmh, long & lnRet, bool fKillFocus)
{
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
	}
	else
	{
		// Called from an edit control.
		hwndEdit = pnmh->hwndFrom;
		switch (pnmh->idFrom)
		{
		case kctidFfdOffsetNum:
			hwndSpin = ::GetDlgItem(m_hwnd, kctidFfdOffsetSpin);
			break;
		default:
			Assert(false);
		}
	}

	// Get the text from the edit box and convert it to a number.
	int cch = ::SendMessage(hwndEdit, WM_GETTEXT, strb.kcchMaxStr, (LPARAM)strb.Chars());
	strb.SetLength(cch);
	int nValue;
	int icombo = ::SendDlgItemMessage(m_hwnd, kctidFfdOffset, CB_GETCURSEL, 0, 0) +
		m_istidMinOffset;
	AfUtil::GetStrMsrValue (&strb , knpt, &nValue);
	if (icombo == 3)
		nValue = - nValue;

	if (fKillFocus && cch == 0)
	{
		m_chrpCur.dympOffset = FwStyledText::knUnspecified;
		m_chrpi.xOffset = kxSoft;
		SetOffset(Offset());
		return true;
	}

	if (pnmh->code == UDN_DELTAPOS)
	{
		// If nValue is not already a whole increment of nDelta, then we only increment it
		// enough to make it a whole increment. If already a whole increment, then we go ahead
		// and increment it the entire amount.
		int nDelta = ((NMUPDOWN *)pnmh)->iDelta;
		int nPartialIncrement = nValue % nDelta;
		if (icombo == 3) // if combo is set to lower
		{
			if (nPartialIncrement && nDelta > 0)
				nValue -= nPartialIncrement;
			else if (nPartialIncrement && nDelta < 0)
				nValue += (nDelta - nPartialIncrement);
			else
				nValue += nDelta;
		}
		else // combo set to raise or none
		{
			if (nPartialIncrement && nDelta > 0)
				nValue += (nDelta - nPartialIncrement);
			else if (nPartialIncrement && nDelta < 0)
				nValue -= nPartialIncrement;
			else
				nValue += nDelta;
		}

	}
	m_chrpi.xOffset = kxHard;
	SetOffset(nValue);

	// Update preview window.
	::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);

	lnRet = 0;
	return true;
} // FmtFntDlg::OnDeltaSpin.


/*----------------------------------------------------------------------------------------------
	Handle a notification.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	switch (pnmh->code)
	{
	case CBN_SELCHANGE:
		if (OnComboChange(pnmh, lnRet))
			return true;
		break;

	case CBN_EDITUPDATE:
		if (OnComboUpdate(pnmh, lnRet))
			return true;
		break;

	case CBN_KILLFOCUS:
		if (OnKillFocus(pnmh, lnRet))
			return true;
		break;

	case UDN_DELTAPOS: // Spin control is activated.
		OnDeltaSpin(pnmh, lnRet);
		break;

	case EN_KILLFOCUS: // Edit control modified.
		OnDeltaSpin(pnmh, lnRet, true);
		break;

	case BN_CLICKED:
		switch (pnmh->idFrom)
		{
		case kctidFfdItalic:
			SetCheck(kctidFfdItalic, m_chrpCur.ttvItalic, true,
				m_chrpOld.ttvItalic == FwStyledText::knConflicting);
			m_chrpi.xItalic = (m_chrpCur.ttvItalic == FwStyledText::knUnspecified) ?
				kxSoft : kxHard;
			::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);
			return true;
		case kctidFfdBold:
			SetCheck(kctidFfdBold, m_chrpCur.ttvBold, true,
				m_chrpOld.ttvBold == FwStyledText::knConflicting);
			m_chrpi.xBold = (m_chrpCur.ttvBold == FwStyledText::knUnspecified) ?
				kxSoft : kxHard;
			::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);
			return true;
		case kctidFfdSuper:
			{ // BLOCK (for var inits)
				int ttv;
				switch (m_chrpCur.ssv)
				{
				case FwStyledText::knConflicting:
				case FwStyledText::knUnspecified:
					ttv = m_chrpCur.ssv;
					break;
				case kssvSuper:
					ttv = kttvForceOn;
					break;
				default:
					ttv = kttvOff;
					break;
				}
				SetCheck(kctidFfdSuper, ttv, true,
					m_chrpOld.ssv == FwStyledText::knConflicting);
				m_chrpi.xSs = (m_chrpCur.ssv == FwStyledText::knUnspecified) ? kxSoft : kxHard;
				// Set ssv, and set the value for the complementary checkbox if needed.
				// The possible states are: both off, both conflicting, or one button on.
				// The above call to SetCheck moves it in the order conflicting->Invert->off.
				// If it is now Invert, the other box must be off.
				// If it is now conflicting, the other box must be conflicting.
				// If it is now off, it must previously have been either on or conflicting;
				// in either case, it is safe to ensure the other box is off.
				switch (ttv)
				{
				case kttvInvert:
					m_chrpCur.ssv = kssvSuper;
					ttv = kttvOff;
					m_chrpi.xSs = kxExplicit;
					break;
				case kttvOff:
					m_chrpCur.ssv = kssvOff;
					ttv = kttvOff;
					m_chrpi.xSs = kxExplicit;
					break;
				case FwStyledText::knConflicting:
				case FwStyledText::knUnspecified:
					m_chrpCur.ssv = ttv;
					m_chrpi.xSs = kxInherited;
					break;
				default:
					Assert(false);
					break;
				}
				SetCheck(kctidFfdSub, ttv);
				::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);
			}
			return true;
		case kctidFfdSub:
			{ // BLOCK
				int ttv;
				switch (m_chrpCur.ssv)
				{
				case FwStyledText::knConflicting:
				case FwStyledText::knUnspecified:
					ttv = m_chrpCur.ssv;
					break;
				case kssvSub:
					ttv = kttvForceOn;
					break;
				default:
					ttv = kttvOff;
					break;
				}
				SetCheck(kctidFfdSub, ttv, true,
					m_chrpOld.ssv == FwStyledText::knConflicting);
				m_chrpi.xSs = (m_chrpCur.ssv == FwStyledText::knUnspecified) ? kxSoft : kxHard;
				// Set ssv, and set the value for the complementary checkbox if needed.
				// The possible states are: both off, both conflicting, or one button on.
				// The above call to SetCheck moves it in the order conflicting->Invert->off.
				// If it is now Invert, the other box must be off.
				// If it is now conflicting, the other box must be conflicting.
				// If it is now off, it must previously have been either on or conflicting;
				// in either case, it is safe to ensure the other box is off.
				switch (ttv)
				{
				case kttvInvert:
					m_chrpCur.ssv = kssvSub;
					ttv = kttvOff;
					m_chrpi.xSs = kxExplicit;
					break;
				case kttvOff:
					m_chrpCur.ssv = kssvOff;
					ttv = kttvOff;
					m_chrpi.xSs = kxExplicit;
					break;
				case FwStyledText::knConflicting:
				case FwStyledText::knUnspecified:
					m_chrpCur.ssv = ttv;
					m_chrpi.xSs = kxInherited;
					break;
				default:
					Assert(false);
					break;
				}
				SetCheck(kctidFfdSuper, ttv);
				::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);
			}
			return true;

		case kctidFfdFeatures:
			{ // BLOCK
				// Show the popup menu that allows a user to choose the features.
				Rect rc;
				::GetWindowRect(::GetDlgItem(m_hwnd, kctidFfdFeatures), &rc);
				AfApp::GetMenuMgr(&m_pmum)->SetMenuHandler(kctidFfdFeatPopup);
				CreateFeaturesMenu();
				if (m_hmenuFeatures)
					::TrackPopupMenu(m_hmenuFeatures, TPM_LEFTALIGN | TPM_RIGHTBUTTON,
						rc.left, rc.bottom, 0, m_hwnd, NULL);
				return true;
			}
		}
		break;

	case CBN_SELENDOK:
		// Color "combo box" was modified
		if (pnmh->idFrom == kctidFfdForeClr)
		{
			if (m_clrFore == kclrTransparent)
			{
				m_chrpCur.clrFore = (COLORREF)FwStyledText::knUnspecified;
				m_chrpi.xFore = kxSoft;
				// Do later (UpdateComboWithInherited):
				//m_qccmbF->SetColor(m_chrpSoft.clrFore);
			}
			else
			{
				m_chrpCur.clrFore = m_clrFore;
				m_chrpi.xFore = kxHard;
			}
			::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);
		}
		if (pnmh->idFrom == kctidFfdBackClr)
		{
			if (m_clrBack == kclrTransparent)
			{
				m_chrpCur.clrBack = (COLORREF)FwStyledText::knUnspecified;
				m_chrpi.xBack = kxSoft;
				// Do later (UpdateComboWithInherited):
				//m_qccmbB->SetColor(m_chrpSoft.clrBack);
			}
			else
			{
				m_chrpCur.clrBack = m_clrBack;
				m_chrpi.xBack = kxHard;
			}
			::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);
		}
		if (pnmh->idFrom == kctidFfdUnderClr)
		{
			if (m_clrUnder == kclrTransparent)
			{
				m_chrpCur.clrUnder = (COLORREF)FwStyledText::knUnspecified;
				m_chrpi.xUnder = kxSoft;
				// Do later (UpdateComboWithInherited):
				//m_qccmbU->SetColor(m_chrpSoft.clrUnder);
			}
			else
			{
				m_chrpCur.clrUnder = m_clrUnder;
				m_chrpi.xUnder = kxHard;
			}
			::InvalidateRect(::GetDlgItem(m_hwnd, kctidFfdPreview), NULL, true);
		}
		return true;
	}

	// The Remove button is defined as closing the current dialog box and opening another.
	if (ctidFrom == kctidFfdRemove)
		::EndDialog(m_hwnd, kctidFfdRemove);

	return SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet);
} // FmtFntDlg::OnNotifyChild.

/*----------------------------------------------------------------------------------------------
	Process draw messages.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::OnDrawChildItem(DRAWITEMSTRUCT * pdis)
{
	if (pdis->CtlID == kctidFfdPreview)
	{
		UpdatePreview(pdis);
		return true;
	}
	if (pdis->CtlID == kctidFfdUnder)
	{
		UpdateUnderlineStyle(pdis);
		return true;
	}
	return SuperClass::OnDrawChildItem(pdis);
}

/*----------------------------------------------------------------------------------------------
	Takes care of painting the Preview window, which shows the currently chosen settings in
	the dialog.  We display the font name with all the formatting applied.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::UpdatePreview(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);

	// Setup to paint.
	HDC hdc = pdis->hDC;
	RECT rect = pdis->rcItem;
	SIZE sizeMargins = { ::GetSystemMetrics(SM_CXEDGE), ::GetSystemMetrics(SM_CYEDGE) };
	SmartPalette spal(hdc);

	// Reduce the size of the "client" area of the button to exclude the border.
	rect.left += sizeMargins.cx;
	rect.right -= sizeMargins.cx;
	rect.top += sizeMargins.cy;
	rect.bottom -= sizeMargins.cy;

	StrApp strFontNameUI = FontFamily();
	if (m_fBullet)
		strFontNameUI = "Wingdings";	// Purely cosmetic, to keep user happy.
	::SetBkMode(hdc, OPAQUE);

	COLORREF clrForeGood = ForeColor();
	if (clrForeGood == FwStyledText::knUnspecified ||
		clrForeGood == FwStyledText::knConflicting ||
		clrForeGood == (COLORREF)kclrTransparent)
	{
		clrForeGood = kclrBlack;
	}
	COLORREF clrBackGood = BackColor();
	if (clrBackGood == FwStyledText::knUnspecified ||
		clrBackGood == FwStyledText::knConflicting)
	{
		clrBackGood = (COLORREF)kclrTransparent;
	}
	COLORREF clrUnderGood = UnderColor();
	if (clrUnderGood == FwStyledText::knUnspecified ||
		clrUnderGood == FwStyledText::knConflicting ||
		clrUnderGood == (COLORREF)kclrTransparent)
	{
		clrUnderGood = clrForeGood;
	}

	// Fill in the preview window with windows background color.
	Rect rcT(pdis->rcItem);
	AfGfx::FillSolidRect(hdc, rcT, ::GetSysColor(COLOR_WINDOW));

	if (clrBackGood == (COLORREF)kclrTransparent)
		AfGfx::SetBkColor(hdc, ::GetSysColor(COLOR_WINDOW));
	else
		AfGfx::SetBkColor(hdc, clrBackGood);

	AfGfx::SetTextColor(hdc, clrForeGood);

	int cPixels = ::GetDeviceCaps(hdc, LOGPIXELSY);
	// Use InterpretChrp to make fine adjustments so we get the exact same appearance.
	// Fill a LgCharRenderProps in with current values.
	LgCharRenderProps chrpFix;
	chrpFix.clrFore = clrForeGood;
	chrpFix.clrBack = clrBackGood;
	chrpFix.dympOffset = Offset();
	chrpFix.ws = m_chrpCur.ws;
	chrpFix.fWsRtl = m_chrpCur.fWsRtl;
	chrpFix.nDirDepth = 0; // OK for default
	chrpFix.ssv = (byte)Superscript();
//	chrpFix.ttvBold = (Bold() != kttvOff && Bold() != FwStyledText::knConflicting);
//	chrpFix.ttvItalic = (Italic() != kttvOff && Italic() != FwStyledText::knConflicting);
	chrpFix.ttvBold = Bold() != FwStyledText::knConflicting ? Bold() : kttvOff;
	chrpFix.ttvItalic = Italic() != FwStyledText::knConflicting ? Italic() : kttvOff;
	chrpFix.dympHeight = Size();
	chrpFix.ws = m_wsPrev;

	if (chrpFix.ws == -1) // If old data with ws unspecified substitute def ws.
		chrpFix.ws = 0;

	StrUni stuFfGoodMarkup = FontFamily();
	if (stuFfGoodMarkup.Length() == 0)
		stuFfGoodMarkup = FwStyledText::FontDefaultMarkup();
	stuFfGoodMarkup = FwStyledText::FontStringUiToMarkup(stuFfGoodMarkup);
	wcsncpy_s(chrpFix.szFaceName, stuFfGoodMarkup, isizeof(chrpFix.szFaceName)/ isizeof(OLECHAR));
	if (m_fBullet)
		wcscpy_s(chrpFix.szFaceName, L"Wingdings");	// Cosmetic for user's benefit.

	if (chrpFix.dympHeight == FwStyledText::knConflicting||
		chrpFix.dympHeight == FwStyledText::knUnspecified)
	{
		chrpFix.dympHeight = 12000;
	}
	if (chrpFix.dympOffset == FwStyledText::knConflicting ||
		chrpFix.dympOffset == FwStyledText::knUnspecified)
	{
		chrpFix.dympOffset = 0;
	}
	int dympUnderlineOffset = chrpFix.dympOffset;
	AssertPtr(m_qwsf);
	IWritingSystemPtr qLgWritingSystem;
	CheckHr(m_qwsf->get_EngineOrNull(chrpFix.ws, &qLgWritingSystem));
	AssertPtr(qLgWritingSystem);
	CheckHr(qLgWritingSystem->InterpretChrp(&chrpFix));

	// Underlining should be lowered for subscripts, but not raised for superscripts
	dympUnderlineOffset = min(dympUnderlineOffset, chrpFix.dympOffset);

	// Kludge: we copy everything from m_chrpCur except the ws/ows.
	int encSave = m_chrpCur.ws;
	m_chrpCur.ws = m_wsPrev;

	// If anything has changed from the last preview display, regenerate the font information
	// in the control.
	LgCharRenderProps chrpTmp;
	CopyCurrentTo(chrpTmp);
	if (memcmp(&chrpTmp, &m_chrpPrvw, isizeof(chrpTmp)) || FontFamily() != m_stuFfPrvw)
	{
		LOGFONT lf;
		ClearItems(&lf, 1);

		// For the MM_TEXT mapping mode, you can use the following formula to specify a height
		// for a font with a given point size:
		// lfHeight = -MulDiv(PointSize, GetDeviceCaps(hDC, LOGPIXELSY), 72);

		lf.lfHeight = -MulDiv(chrpFix.dympHeight, cPixels, 72000);
		lf.lfWeight = chrpFix.ttvBold != kttvOff ? FW_BOLD : FW_NORMAL;
		lf.lfItalic = (chrpFix.ttvItalic != kttvOff);
		lf.lfUnderline = false;
		lf.lfCharSet = DEFAULT_CHARSET;
		lf.lfOutPrecision = OUT_TT_ONLY_PRECIS;
		lf.lfClipPrecision = CLIP_DEFAULT_PRECIS;
		lf.lfQuality = DEFAULT_QUALITY;
		lf.lfPitchAndFamily = DEFAULT_PITCH | FF_DONTCARE;
		StrApp strRealFontName = chrpFix.szFaceName;

		// InterpretChrp has mapped trick font names.
		// Now we need a copy that works whether lf.lfFaceName is wide or narrow.

		Assert(isizeof(achar) == isizeof(TCHAR));
		int cch = min(LF_FACESIZE - 1, strRealFontName.Length() + 1); // +1 to copy trailing NUL
		memcpy(lf.lfFaceName, strRealFontName.Chars(), cch * isizeof(achar));

		if (memcmp(&lf, &m_lfPrvw, isizeof(lf)))
		{
			HFONT hfont = AfGdi::CreateFontIndirect(&lf);
			::SendMessage(::GetDlgItem(m_hwnd, kctidFfdPreview), WM_SETFONT, (WPARAM)hfont,
				true);
			m_lfPrvw = lf;
			if (m_hfontPrvw)
				AfGdi::DeleteObjectFont(m_hfontPrvw);
			m_hfontPrvw = hfont;
		}

		CopyCurrentTo(m_chrpPrvw);
		m_stuFfPrvw = FontFamily();
	}
	m_chrpCur.ws = encSave;
	if (strFontNameUI.Length() == 0)
		strFontNameUI = FwStyledText::FontDefaultUi(m_f1DefaultFont);
	// Compute the offset, taking into account superscripting and subscripting.
	int dympOffset = chrpFix.dympOffset;
	int lfOffset = MulDiv(dympOffset, cPixels, 72000);
	// Write the name of the font in the center of the Preview window.
	SIZE size;
	::GetTextExtentPoint32(hdc, strFontNameUI.Chars(), strFontNameUI.Length(), &size);
	int x = (rect.right - rect.left - size.cx) / 2;
	if (x <= rect.left)
		x = rect.left + 10;
	int y = ((rect.bottom - rect.top - size.cy) / 2) - lfOffset;
	if (y <= rect.top)
		y = rect.top + 10;
	::TextOut(hdc, x, y, strFontNameUI.Chars(), strFontNameUI.Length());

	// Graphically display the current offset setting.
	TEXTMETRIC tm;
	int cDescent = 0;
	int cAscent = 0;
	if (::GetTextMetrics(hdc, &tm))
	{
		cDescent = tm.tmDescent;
		cAscent = tm.tmAscent;
	}
	int x1 = x - 20;
	if (x1 < rect.left)
		x1 = rect.left;
	// Set y1 to the baseline of the displayed string + the given offset.
	int y1 = y + size.cy - cDescent + lfOffset;
	::MoveToEx(hdc, x1, y1, NULL);
	::LineTo(hdc, x1 + 20, y1);
	x1 = x + size.cx + 20;
	if (x1 > rect.right)
		x1 = rect.right;
	::MoveToEx(hdc, x1 - 20, y1, NULL);
	::LineTo(hdc, x1, y1);

	// Now, handle the underlining of the text.  (Underline is 1 pixel below baseline.)
	// We lower the underline for subscripts, but don't raise it for superscripts.
	int lfUnderlineOffset = MulDiv(dympUnderlineOffset, cPixels, 72000);
	y1 = y + size.cy - cDescent + 1 + (lfOffset - lfUnderlineOffset);
	int x2;
	int xEnd = x + size.cx;
	// Create a pen of the proper color, and select it.
	HPEN hpen = ::CreatePen(PS_SOLID, 0, clrUnderGood);	// pen is 1 pixel wide.
	HPEN hpenOld = (HPEN)::SelectObject(hdc, hpen);
	switch (Underline())
	{
	case kuntSingle:
		::MoveToEx(hdc, x, y1, NULL);
		::LineTo(hdc, xEnd, y1);
		break;
	case kuntDouble:
		::MoveToEx(hdc, x, y1, NULL);
		::LineTo(hdc, xEnd, y1);
		::MoveToEx(hdc, x, y1 + 2, NULL);
		::LineTo(hdc, xEnd, y1 + 2);
		break;
	case kuntDashed:
		for (x1 = x; x1 < xEnd; x1 += 9)
		{
			::MoveToEx(hdc, x1, y1, NULL);
			x2 = x1 + 6;
			if (x2 > xEnd)
				x2 = xEnd;
			::LineTo(hdc, x2, y1);
		}
		break;
	case kuntDotted:
		for (x1 = x; x1 < xEnd; x1 += 4)
		{
			::MoveToEx(hdc, x1, y1, NULL);
			x2 = x1 + 2;
			if (x2 > xEnd)
				x2 = xEnd;
			::LineTo(hdc, x2, y1);
		}
		break;
	case kuntStrikethrough:
		int nStrikeHeight = y + (3 * cAscent / 4);
		::MoveToEx(hdc, x, nStrikeHeight, NULL);
		::LineTo(hdc, xEnd, nStrikeHeight);
		break;
	}
	// Clean up the pen, restoring the original value.
	::SelectObject(hdc, hpenOld);
	::DeleteObject(hpen);

	// Now draw the preview border. Putting it here at the end makes sure the internal
	// painting doesn't overwrite the border.
	rect = pdis->rcItem;
	::DrawEdge(hdc, &rect, EDGE_SUNKEN, BF_RECT);
} // FmtFntDlg::UpdatePreview.

/*----------------------------------------------------------------------------------------------
	Draw the appropriate string or bitmap in the Underline Style combo box.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::UpdateUnderlineStyle(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);

	if (pdis->itemData == -1)
		// conflicting
		return;

	// Set up to paint.
	// Fill in the window with a solid white background.
	Rect rcT(pdis->rcItem);
	// This is necessary to make items in the menu highlight as the user drags
	// over them. The second condition prevents some very odd appearance in the
	// main edit box after selecting an item.
	//if ((pdis->itemState & ODS_SELECTED) && !(pdis->itemState & ODS_COMBOBOXEDIT))

	HGDIOBJ tmptmp = ::GetStockObject(BLACK_PEN);
	HPEN hpenOld = (HPEN)::SelectObject(pdis->hDC, tmptmp);
//	HGDIOBJ tmptmp = ::GetStockObject(DC_PEN);
//	HPEN hpenOld = (HPEN)::SelectObject(pdis->hDC, ::GetStockObject(DC_PEN));
	::SetBkMode(pdis->hDC, TRANSPARENT);

	AfGfx::FillSolidRect(pdis->hDC, rcT, (pdis->itemState & ODS_SELECTED ?
		::GetSysColor(COLOR_HIGHLIGHT) : ::GetSysColor(COLOR_WINDOW)));

	HPEN hpen;

	if ((pdis->itemState & ODS_COMBOBOXEDIT) && (m_chrpi.xUnderT == kxSoft))
	{
		COLORREF clrGray = ::GetSysColor(COLOR_3DSHADOW);
		// For the line segments, I'm making the gray a little lighter than
		// what's used for the other widgets, because it's hard to tell the difference
		// between gray and black. ENHANCE: if there is a way for this to be some color other
		// than gray, rework.
		if (pdis->itemData != kstidFfdNone && pdis->itemData != kstidFfdUnspecified)
			clrGray = kclrLightGray;

		AfGfx::SetTextColor(pdis->hDC, clrGray);
		hpen = ::CreatePen(PS_SOLID, 0, clrGray);
		::SelectObject(pdis->hDC, hpen);
	}
	else
	{
		AfGfx::SetTextColor(pdis->hDC, (pdis->itemState & ODS_SELECTED ?
			::GetSysColor(COLOR_HIGHLIGHTTEXT) : ::GetSysColor(COLOR_WINDOWTEXT)));

		hpen = ::CreatePen(PS_SOLID, 0, (pdis->itemState & ODS_SELECTED ?
			::GetSysColor(COLOR_HIGHLIGHTTEXT) : ::GetSysColor(COLOR_WINDOWTEXT)));

		::SelectObject(pdis->hDC, hpen);
	}

	int y = pdis->rcItem.top + (pdis->rcItem.bottom - pdis->rcItem.top) / 2;
	int xLeft = pdis->rcItem.left + 2;
	int xRight = pdis->rcItem.right - 2;
	StrApp str;
	int x;
	int x2;
	switch (pdis->itemData)
	{
	case kstidFfdUnspecified:
		str.Load(kstidFfdUnspecified);
		::TextOut(pdis->hDC, pdis->rcItem.left + 2, pdis->rcItem.top + 1,
			str.Chars(), str.Length());
		break;
	case kstidFfdNone:
		str.Load(kstidFfdNone);
		::TextOut(pdis->hDC, pdis->rcItem.left + 2, pdis->rcItem.top + 1,
			str.Chars(), str.Length());
		break;
	case kstidFfdDotted:
		for (x = xLeft; x < xRight; x += 4)
		{
			::MoveToEx(pdis->hDC, x, y, NULL);
			x2 = x + 2;
			if (x2 > xRight)
				x2 = xRight;
			::LineTo(pdis->hDC, x2, y);
		}
		break;
	case kstidFfdDashed:
		for (x = xLeft; x < xRight; x += 9)
		{
			::MoveToEx(pdis->hDC, x, y, NULL);
			x2 = x + 6;
			if (x2 > xRight)
				x2 = xRight;
			::LineTo(pdis->hDC, x2, y);
		}
		break;
	case kstidFfdStrikethrough:
		str.Load(kstidFfdStrikethrough);
		::TextOut(pdis->hDC, pdis->rcItem.left + 2, pdis->rcItem.top + 1,
			str.Chars(), str.Length());
		break;
	case kstidFfdSingle:
		::MoveToEx(pdis->hDC, xLeft, y, NULL);
		::LineTo(pdis->hDC, xRight, y);
		break;
	case kstidFfdDouble:
		::MoveToEx(pdis->hDC, xLeft, y - 1, NULL);
		::LineTo(pdis->hDC, xRight, y - 1);
		::MoveToEx(pdis->hDC, xLeft, y + 1, NULL);
		::LineTo(pdis->hDC, xRight, y + 1);
		break;
	default:
		Assert(false);			// THIS SHOULD NEVER HAPPEN!!
		break;
	}

	::SelectObject(pdis->hDC, hpenOld);
	if (hpen)
		::DeleteObject(hpen);

} // FmtFntDlg::UpdateUnderlineStyle.


/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
#if 0
	// This is supposed to be sent but isn't. Instead, I set the item size in OnInitDlg.
	if (wm == WM_MEASUREITEM)
	{
		// The only owner-draw item is the underline combo box.
		// Just to see if this is working we'll make it very small.
		LPMEASUREITEMSTRUCT lpmis;
		lpmis = (LPMEASUREITEMSTRUCT) lp;

		//if (lpmis->itemHeight < CY_BITMAP + 2)
		lpmis->itemHeight = 5;
	}
#endif
	if (wm == WM_CTLCOLOREDIT || wm == WM_CTLCOLORBTN || wm == WM_CTLCOLORSTATIC
		|| wm == WM_CTLCOLORLISTBOX || wm == WM_CTLCOLORDLG)
	{
		return ColorForInheritance(wp, lp, lnRet);
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Set up a popup menu to use to select font features.
	This method is static so it can be used by functions outside of the Format-Font dialog.

	@param hmenu		Popup menu being set up
	@param prenfeat		Rendering engine from which to get feature information
	@param nLang		Language ID for selecting UI strings
	@param vnMenuMap	feature index/value pairs for each menu item
	@param prgnVal		current values, for setting check marks
	@param prgnDefaults	optional defaults from old writing system, or NULL

	@return				True if there is at least one item in the menu.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::SetupFeaturesMenu(HMENU & hmenu, IRenderingFeatures * prenfeat, int nLang,
	Vector<int> & vnMenuMap, int * prgnVal, int * prgnDefaults)
{
	StrApp strRes;
	int cItems = 0;

	int cfeat;
	int rgfid[kMaxFeature];
	CheckHr(prenfeat->GetFeatureIDs(kMaxFeature, rgfid, &cfeat));
	for (int ifeat = 0; ifeat < cfeat; ifeat++)
	{
		if (rgfid[ifeat] == kGrLangFeature) // don't include 'lang' feature
			continue;

		SmartBstr sbstrFeat;
		CheckHr(prenfeat->GetFeatureLabel(rgfid[ifeat], nLang, &sbstrFeat));
		StrApp strFeat(sbstrFeat.Chars());
		if (strFeat.Length() == 0)
		{
			// Create backup default string, ie, "Feature #1".
			strRes.Load(kstidFeatureLabel);
			strFeat.Format(strRes, rgfid[ifeat]);
		}
		int cn;
		int rgn[kMaxValPerFeat];
		int nDefault;
		CheckHr(prenfeat->GetFeatureValues(rgfid[ifeat], kMaxValPerFeat, rgn,
			&cn, &nDefault));
		if (prgnDefaults && prgnDefaults[ifeat] != INT_MAX)
			// Use the default from the old writing system, rather than the font.
			nDefault = prgnDefaults[ifeat];

		bool fBinary = false;
		if (cn == 2 && (rgn[0] == 0 || rgn[1] == 0) && (rgn[0] + rgn[1] == 1))
		{
			// Figure out whethere a simple binary toggle can be used instead of a
			// sub-menu.
			SmartBstr sbstrT;
			SmartBstr sbstrF;
			CheckHr(prenfeat->GetFeatureValueLabel(rgfid[ifeat], 1, nLang, &sbstrT));
			CheckHr(prenfeat->GetFeatureValueLabel(rgfid[ifeat], 0, nLang, &sbstrF));
			const wchar * pchwT = sbstrT.Chars();
			const wchar * pchwF = sbstrF.Chars();
			if ((wcslen(pchwT) == 0 ||
					!wcscmp(pchwT, L"True") || !wcscmp(pchwT, L"true") ||
					!wcscmp(pchwT, L"Yes")  || !wcscmp(pchwT, L"yes")  ||
					!wcscmp(pchwT, L"On")   || !wcscmp(pchwT, L"on"))
				&& (wcslen(pchwF) == 0 ||
					!wcscmp(pchwF, L"False") || !wcscmp(pchwF, L"false") ||
					!wcscmp(pchwF, L"No")    || !wcscmp(pchwF, L"No")    ||
					!wcscmp(pchwF, L"Off")   || !wcscmp(pchwF, L"off")))
			{
				fBinary = true;
			}
		}

		if (fBinary)
		{
			int cid = kcidMenuItemDynMin + cItems;
			int nChecked = (prgnVal[ifeat] == 1 ||
				(prgnVal[ifeat] == INT_MAX && 1 == nDefault)) ?
					MF_CHECKED :
					MF_UNCHECKED;
			::AppendMenu(hmenu, MF_STRING | nChecked, cid, strFeat.Chars());

			Assert(vnMenuMap.Size() == cItems);
			vnMenuMap.Push((ifeat << 16) | 0x0000FFFF);
			cItems++;
		}
		else if (cn > 0)
		{
			Assert(cn < 0x0000FFFF);
			HMENU hmenuSub = ::CreatePopupMenu();
			::AppendMenu(hmenu, MF_POPUP, (UINT_PTR)hmenuSub, strFeat.Chars());
			for (int in = 0; in < cn; in++)
			{
				SmartBstr sbstrVal;
				CheckHr(prenfeat->GetFeatureValueLabel(rgfid[ifeat], rgn[in], nLang,
					&sbstrVal));
				StrApp strSub(sbstrVal.Chars());
				if (strSub.Length() == 0)
				{
					// Create backup default string.
					strRes.Load(kstidFeatureValueLabel);
					strSub.Format(strRes, rgn[in]);
				}
				int cid = kcidMenuItemDynMin + cItems;
				int nChecked = (prgnVal[ifeat] == rgn[in] ||
					(prgnVal[ifeat] == INT_MAX && rgn[in] == nDefault)) ?
						MF_CHECKED :
						MF_UNCHECKED;
				::AppendMenu(hmenuSub, MF_STRING | nChecked, cid, strSub.Chars());

				Assert(vnMenuMap.Size() == cItems);
				vnMenuMap.Push((ifeat << 16) | in);
				cItems++;
			}
		}
		else
		{
			// Omit. Review: should we create a binary feature out of it?
			//::AppendMenu(hmenu, MF_STRING, kcidMenuItemDynMin + cItems, strFeat.Chars());
		}
	}

	return (cItems > 0);
}

/*----------------------------------------------------------------------------------------------
	The user selected something from the font features menu. Process the selection,
	modifying the appropriate item in the array of feature values.
	This method is static so it can be used by functions outside of the Format-Font dialog.

	@param prenfeat		Rendering engine from which to get feature information
	@param item			Menu item selected
	@param vnMenuMap	feature index/value pairs for each menu item
	@param prgnVal		current values, modified by this method
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::HandleFeaturesMenu(IRenderingFeatures * prenfeat, int item,
	Vector<int> & vnMenuMap, int * prgnVal)
{
	int inVal = vnMenuMap[item] & 0x0000FFFF;
	int ifeat = (vnMenuMap[item] & 0xFFFF0000) >> 16;

	bool fBinary = (inVal == 0x0000FFFF);

	int cfeat;
	int rgfid[kMaxFeature];
	CheckHr(prenfeat->GetFeatureIDs(kMaxFeature, rgfid, &cfeat));
	int cn;
	int rgn[kMaxValPerFeat];
	int nDefault;
	CheckHr(prenfeat->GetFeatureValues(rgfid[ifeat], kMaxValPerFeat, rgn, &cn, &nDefault));
	if (fBinary)
	{
		// Toggle
		if (prgnVal[ifeat] == INT_MAX)
			prgnVal[ifeat] = nDefault;
		else if (prgnVal[ifeat] == knConflicting)
			prgnVal[ifeat] = 0;

		prgnVal[ifeat] = (int)(!prgnVal[ifeat]);
	}
	else
		prgnVal[ifeat] = rgn[inVal];
}

/*----------------------------------------------------------------------------------------------
	Generate a text string holding the given list of feature values. This string is of the
	form <id1>=<val1>,<id2>=<val2>,... Default (unspecified) settings are indicated by INT_MAX.
	This method is static so it can be used by functions outside of the Format-Font dialog.
	TODO (SharonC): Using += on StrAnsi is not very efficient.
----------------------------------------------------------------------------------------------*/
StrUni FmtFntDlg::GenerateFeatureString(IRenderingFeatures * prenfeat, int * prgnVal)
{
	int rgfid[kMaxFeature];
	int cfeat;
	CheckHr(prenfeat->GetFeatureIDs(kMaxFeature, rgfid, &cfeat));
	Assert(cfeat < kMaxFeature);

	StrAnsi staFontVar;
	char rgch[20];
	for (int ifeat = 0; ifeat < cfeat; ifeat++)
	{
		if (prgnVal[ifeat] != knConflicting
			&& prgnVal[ifeat] != INT_MAX) // something other than the default
		{
			if (staFontVar.Length())
				staFontVar += ",";
			_itoa_s(rgfid[ifeat], rgch, 10);
			staFontVar += rgch;
			staFontVar += "=";
			_itoa_s(prgnVal[ifeat], rgch, 10);
			staFontVar += rgch;
		}
	}

	StrUni stuRet = staFontVar;
	return stuRet;
}

/*----------------------------------------------------------------------------------------------
	Parse the text string that holds the font feature values. This string is of the
	form <id1>=<val1>,<id2>=<val2>,... 'prgVal' is assumed to hold kMaxFeature spaces.
	The buffer of values is assumed to be already initialized (thus this method can be used
	to layer values from different sources ie writing system defaults, layers of styles).

	This method is static so it can be used by functions outside of the Format-Font dialog.
----------------------------------------------------------------------------------------------*/
void FmtFntDlg::ParseFeatureString(IRenderingFeatures * prenfeat, StrUni stu, int * prgnVal)
{
	int rgfid[kMaxFeature];
	int cfeat;
	try
	{
		CheckHr(prenfeat->GetFeatureIDs(kMaxFeature, rgfid, &cfeat));
	}
	catch (Throwable& thr)
	{
		WarnHr(thr.Error());
		return;
	}

	Assert(cfeat < kMaxFeature);

	wchar * pchw = const_cast<wchar *>(stu.Chars());
	wchar * pchwLim = pchw + stu.Length();
	while (pchw < pchwLim)
	{
		int fid = 0;
		int nValue = 0;
		bool fNeg = false;

		//	Read the ID.
		while (*pchw != '=' && *pchw != ' ')
		{
			if (*pchw < '0' || *pchw > '9')
				goto LNext;	// syntax error: skip this setting
			fid = fid * 10 + (*pchw - '0');
			pchw++;
		}
		while (*pchw == ' ')
			pchw++;
		Assert(*pchw == '=');
		pchw++;
		while (*pchw == ' ')
			pchw++;

		//	Read the value.
		if (*pchw == '"')
		{
			//	Language ID string--form an integer out of the first four characters, ignore
			//	the rest.
			pchw++;	// skip quote
			byte b1 = 0;
			byte b2 = 0;
			byte b3 = 0;
			byte b4 = 0;
			if (*pchw != '"')
			{
				b1 = (byte)*pchw;
				pchw++;
			}
			if (*pchw != '"')
			{
				b2 = (byte)*pchw;
				pchw++;
			}
			if (*pchw != '"')
			{
				b3 = (byte)*pchw;
				pchw++;
			}
			if (*pchw != '"')
			{
				b4 = (byte)*pchw;
				pchw++;
			}
			while (pchw < pchwLim  && *pchw != '"')	// skip superfluous chars
				pchw++;
			if (pchw >= pchwLim)
				goto LNext;
			pchw++;	// skip quote
			nValue = (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;
		}
		else
		{
			//	Numerical value
			if (*pchw == '-')
			{
				pchw++;
				fNeg = true;
			}
			else if (*pchw == '+')
			{
				pchw++;
				fNeg = false;
			}
			while (*pchw != ',' && *pchw != ' ' && pchw < pchwLim)
			{
				if (*pchw < '0' || *pchw > '9')
					goto LNext;	// syntax error skip this setting
				nValue = nValue * 10 + (*pchw - '0');
				pchw++;
			}
			if (fNeg)
				nValue = nValue * -1;
		}

		if (fid != kGrLangFeature) // ignore 'lang' feature
		{
			// Determine the index of the feature.
			int ifeatFound = -1;
			for (int ifeat = 0; ifeat < cfeat; ifeat++)
			{
				if (rgfid[ifeat] == fid)
				{
					ifeatFound = ifeat;
					break;
				}
			}
			//Assert(ifeatFound > -1);	// this might happen when switching between fonts
										// that have different features
			// Store the value.
			if (ifeatFound > -1)
				prgnVal[ifeatFound] = nValue;
		}

LNext:
		//	Find the next setting.
		while (pchw < pchwLim && *pchw != ',')
			pchw++;
		while (pchw < pchwLim && (*pchw < '0' || *pchw > '9'))
			pchw++;
	}
}

/*----------------------------------------------------------------------------------------------
	Return true if the given rendering engine has any features to be shown in the menu.
	This method is static so it can be used by functions outside of the Format-Font dialog.
----------------------------------------------------------------------------------------------*/
bool FmtFntDlg::HasFeatures(IRenderingFeatures * prenfeat)
{
	if (!prenfeat)
		return false;

	int cfeat;
	int rgfid[3];
	CheckHr(prenfeat->GetFeatureIDs(3, rgfid, &cfeat));
	if (cfeat > 1 || (cfeat == 1 && rgfid[0] != kGrLangFeature))
		return true;
		// Note: there is still the off chance that we have features with no valid settings,
		// in which case the button will be enabled but no menu will appear.
	else
		return false;
}


//:>********************************************************************************************
//:>	SimpleComboEdit methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	In this special class which handles the edit portion of the font combo box, we intercept
	the backspace key to make it act more consistently with our desired typeahead behavior.
----------------------------------------------------------------------------------------------*/
bool SimpleComboEdit::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_CHAR && (wp == '\010')) // backspace
	{
		// If there is a range selection, delete the selected stuff plus one more character.
		DWORD ichMin, ichLim;
		::SendMessage(m_hwnd, EM_GETSEL, (WPARAM)(&ichMin), (LPARAM)(&ichLim));
		if (ichMin < ichLim)
		{
			if (ichMin > 0)
				ichMin--;
			achar rgch[MAX_PATH];
			::SendMessage(m_hwnd, WM_GETTEXT, MAX_PATH, (LPARAM)rgch);
			rgch[ichMin] = 0; // zero-terminate

			// Find the closest match.
			int iitem = ::SendMessage(m_hwndParent, CB_FINDSTRING, (WPARAM)-1, (LPARAM)rgch);
			if (iitem < 0)
				iitem = 0;
			Vector<achar> vch;
			achar * pszT;
			int cch = ::SendMessage(m_hwndParent, CB_GETLBTEXTLEN, (WPARAM)iitem, 0);
			if (cch < MAX_PATH)
			{
				pszT = rgch;
			}
			else
			{
				vch.Resize(cch + 1);
				pszT = vch.Begin();
			}
			cch = ::SendMessage(m_hwndParent, CB_GETLBTEXT, (WPARAM)iitem, (LPARAM)pszT);
			if (cch < 0)
				pszT = _T("");
			::SendMessage(m_hwnd, WM_SETTEXT, 0, (LPARAM)pszT);
			::SendMessage(m_hwndParent, CB_SETCURSEL, (WPARAM)iitem, 0);
			// Select the part of the string that was backspaced over.
			::SendMessage(m_hwndParent, CB_SETEDITSEL, 0, MAKELPARAM(ichMin, -1));
			return true;
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/***********************************************************************************************
	RemFmtDlg methods.
	OBSOLETE, since RemFntDlg is no longer used.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor for the "Remove Formatting" dialog box implementation class.
----------------------------------------------------------------------------------------------*/
RemFmtDlg::RemFmtDlg()
{
	m_rid = kridRemFmtDlg;
	m_pszHelpUrl = _T("DialogFont.htm");
}

/*----------------------------------------------------------------------------------------------
	Set the internal value for the indicated check box (ctid).
----------------------------------------------------------------------------------------------*/
void RemFmtDlg::SetValue(int ctid, int ttv)
{
	int ittv = PropIndex(ctid);
	if (ittv >= 0)
		m_rgttv[ittv] = ttv;
}

/*----------------------------------------------------------------------------------------------
	Return the internal value for the indicated check box (ctid).
----------------------------------------------------------------------------------------------*/
int RemFmtDlg::GetValue(int ctid)
{
	int ittv = PropIndex(ctid);
	if (ittv >= 0)
		return m_rgttv[ittv];
	else
		return FwStyledText::knUnspecified;
}

/*----------------------------------------------------------------------------------------------
	Convert the ctid value to an index into our array of check box values.
----------------------------------------------------------------------------------------------*/
int RemFmtDlg::PropIndex(int ctid)
{
	switch (ctid)
	{
	case kctidRfdFont:		return 0;
	case kctidRfdSize:		return 1;
	case kctidRfdForeClr:	return 2;
	case kctidRfdBackClr:	return 3;
	case kctidRfdUnder:		return 4;
	case kctidRfdUnderClr:	return 5;
	case kctidRfdBold:		return 6;
	case kctidRfdItalic:	return 7;
	case kctidRfdSuper:		return 8;
	case kctidRfdSub:		return 9;
	case kctidRfdOffset:	return 10;
	}
	Assert(false);
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog box, either disabling the check boxes, or displaying checks.
----------------------------------------------------------------------------------------------*/
bool RemFmtDlg::OnInitDlg(HWND hwndCtrl, LPARAM lParam)
{
	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	if (!m_hwnd)
		return true;

	int ttv;
	ttv = GetValue(kctidRfdFont);
	if (ttv == FwStyledText::knUnspecified)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRfdFont), false);
	}
	else
	{
		Assert(ttv == kttvForceOn);
		SetCheck(kctidRfdFont, ttv);
	}
	ttv = GetValue(kctidRfdSize);
	if (ttv == FwStyledText::knUnspecified)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRfdSize), false);
	}
	else
	{
		Assert(ttv == kttvForceOn);
		SetCheck(kctidRfdSize, ttv);
	}
	ttv = GetValue(kctidRfdForeClr);
	if (ttv == FwStyledText::knUnspecified)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRfdForeClr), false);
	}
	else
	{
		Assert(ttv == kttvForceOn);
		SetCheck(kctidRfdForeClr, ttv);
	}
	ttv = GetValue(kctidRfdBackClr);
	if (ttv == FwStyledText::knUnspecified)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRfdBackClr), false);
	}
	else
	{
		Assert(ttv == kttvForceOn);
		SetCheck(kctidRfdBackClr, ttv);
	}
	ttv = GetValue(kctidRfdUnder);
	if (ttv == FwStyledText::knUnspecified)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRfdUnder), false);
	}
	else
	{
		Assert(ttv == kttvForceOn);
		SetCheck(kctidRfdUnder, ttv);
	}
	ttv = GetValue(kctidRfdUnderClr);
	if (ttv == FwStyledText::knUnspecified)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRfdUnderClr), false);
	}
	else
	{
		Assert(ttv == kttvForceOn);
		SetCheck(kctidRfdUnderClr, ttv);
	}
	ttv = GetValue(kctidRfdBold);
	if (ttv == FwStyledText::knUnspecified)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRfdBold), false);
	}
	else
	{
		Assert(ttv == kttvForceOn);
		SetCheck(kctidRfdBold, ttv);
	}
	ttv = GetValue(kctidRfdItalic);
	if (ttv == FwStyledText::knUnspecified)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRfdItalic), false);
	}
	else
	{
		Assert(ttv == kttvForceOn);
		SetCheck(kctidRfdItalic, ttv);
	}
	ttv = GetValue(kctidRfdSuper);
	if (ttv == FwStyledText::knUnspecified)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRfdSuper), false);
	}
	else
	{
		Assert(ttv == kttvForceOn);
		SetCheck(kctidRfdSuper, ttv);
	}
	ttv = GetValue(kctidRfdSub);
	if (ttv == FwStyledText::knUnspecified)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRfdSub), false);
	}
	else
	{
		Assert(ttv == kttvForceOn);
		SetCheck(kctidRfdSub, ttv);
	}
	ttv = GetValue(kctidRfdOffset);
	if (ttv == FwStyledText::knUnspecified)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidRfdOffset), false);
	}
	else
	{
		Assert(ttv == kttvForceOn);
		SetCheck(kctidRfdOffset, ttv);
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle changing the check boxes in response to the mouse clicking around.
----------------------------------------------------------------------------------------------*/
bool RemFmtDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	int ttv;
	switch (ctidFrom)
	{
	case kctidRfdFont:
	case kctidRfdSize:
	case kctidRfdForeClr:
	case kctidRfdBackClr:
	case kctidRfdUnder:
	case kctidRfdUnderClr:
	case kctidRfdBold:
	case kctidRfdItalic:
	case kctidRfdSuper:
	case kctidRfdSub:
	case kctidRfdOffset:
		ttv = GetValue(ctidFrom);
		Assert(ttv == kttvForceOn || ttv == kttvOff);
		ttv = ttv == kttvForceOn ? kttvOff : kttvForceOn;
		SetValue(ctidFrom, ttv);
		SetCheck(ctidFrom, ttv);
		return true;
	default:
		return SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet);
	}
}

/*----------------------------------------------------------------------------------------------
	Set the check box display appropriately.
----------------------------------------------------------------------------------------------*/
void RemFmtDlg::SetCheck(int ctid, int ttv)
{
	int bst;
	switch (ttv)
	{
	case kttvForceOn:
		bst = BST_CHECKED;
		break;
	case kttvOff:
		bst = BST_UNCHECKED;
		break;
	default:
		bst = BST_INDETERMINATE;
		break;
	}
	::SendDlgItemMessage(m_hwnd, ctid, BM_SETCHECK, bst, 0);
}


/***********************************************************************************************
	AfStyleFntDlg Methods.
***********************************************************************************************/

BEGIN_CMD_MAP(AfStyleFntDlg)
	ON_CID_CHILD(kctidFfdFeatPopup, &AfStyleFntDlg::CmdFeaturesPopup, NULL)
END_CMD_MAP_NIL()


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfStyleFntDlg::AfStyleFntDlg(AfStylesDlg * pafsd)
	: AfDialogView()
{
	m_pafsd = pafsd;

	m_rid = kridAfStyleFntDlg;
	m_pszHelpUrl = _T("User_Interface/Menus/Format/Style/Style_Font_tab.htm");
	m_fFfVerifySizeActive = false;
	m_fCanInherit = true;
	m_fFeatures = false;
	m_fFeatInit = false;
	m_f1DefaultFont = pafsd->m_f1DefaultFont; // do this first before initializing the font list
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::OnInitDlg(HWND hwnd, LPARAM lp)
{
	StrAppBuf strb;

	// Set up the two columns in the main list control.
	m_hwndLangList = ::GetDlgItem(m_hwnd, kctidAsfdLangList);

	LVCOLUMN lvc;
	lvc.mask = LVCF_TEXT | LVCF_WIDTH;

	lvc.cx = 100;
	strb.Load(kstidLanguage);
	lvc.pszText = const_cast<achar *>(strb.Chars());
	ListView_InsertColumn(m_hwndLangList, 0, &lvc);

	Rect rcClient;
	::GetClientRect(m_hwndLangList, &rcClient);

	lvc.cx = rcClient.Width() - 100; // give it the rest of the width.
	strb.Load(kstidDescription);
	lvc.pszText = const_cast<achar *>(strb.Chars());
	ListView_InsertColumn(m_hwndLangList, 1, &lvc);

	// Initialize the Font combo box.
	InitFontCtl();

	// Fix the size of the underline combo box.
	// ENHANCE JohnT (version 2?) Figure what measurement to use in place of "15" here
	// so it works even when the user does not choose small fonts. (Cf UiColor::SetWindowSize)
	HWND hwndUnder = ::GetDlgItem(m_hwnd, kctidFfdUnder);
	SendMessage(hwndUnder,				// handle to destination window
				CB_SETITEMHEIGHT,       // message to send
				(WPARAM) 0,				// items
				(LPARAM) 15				// empirically determined
	);
	SendMessage(hwndUnder,				// handle to destination window
				CB_SETITEMHEIGHT,       // message to send
				(WPARAM) -1,			// the "selection field" which I presume is the box itself
				(LPARAM) 15				// empirically determined
	);

	// Create color combo boxes. This sets a possibly meaningless initial value,
	// but FillCtls fixes it if need be.
	m_qccmbF.Create();
	m_clrFore = m_esiSel.m_clrFore;
	m_qccmbF->SubclassButton(::GetDlgItem(m_hwnd, kctidFfdForeClr), &m_clrFore);
	m_qccmbB.Create();
	m_clrBack = m_esiSel.m_clrBack;
	m_qccmbB->SubclassButton(::GetDlgItem(m_hwnd, kctidFfdBackClr), &m_clrBack);
	m_qccmbU.Create();
	m_clrUnder = m_esiSel.m_clrUnder;
	m_qccmbU->SubclassButton(::GetDlgItem(m_hwnd, kctidFfdUnderClr), &m_clrUnder);

	//HWND hwndFfdSz = ::GetDlgItem(m_hwnd, kctidFfdSize);
	//HDC hdc = ::GetDC(hwndFfdSz);
	//::SetBkColor(hdc, ::GetSysColor(COLOR_WINDOW));
	//int iSuccess;
	//iSuccess = ::ReleaseDC(hwndFfdSz, hdc);
	//Assert(iSuccess);

	// Create a special subclass of an editable combo-box that can show gray text.
	m_qgecmbSize.Create();
	m_qgecmbSize->SubclassCombo(::GetDlgItem(m_hwnd, kctidFfdSize));
	m_qgecmbSize->SetMonitor(&(m_chrpi.xSize));

	// Create a non-editable combo that handles down arrow from no selection.
	m_qbfscOffset.Create();
	m_qbfscOffset->SubclassCombo(::GetDlgItem(m_hwnd, kctidFfdOffset));

	// Set the values. Can't do meaningfully until SetDialogValues is called.
	// FillCtls();

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
			(pfn)(m_hwndLangList, L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFfdFont), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFfdFeatures), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFfdSize), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFfdUnder), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFfdOffset), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFfdOffsetNum), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFfdOffsetSpin), L"", L"");
		}

		::FreeLibrary(hmod);
	}

	return true;
} // AfStyleFntDlg::OnInitDlg.

/*----------------------------------------------------------------------------------------------
	Change the member variable that tracks whether the style we are showing inherits from
	some other style (or is a character style and therefore always inherits, at least from
	the paragraph properties). This is currently false only for Normal.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::SetCanInherit(bool fCanInherit)
{
	m_fCanStyleInherit = fCanInherit;
	SetCanWsInherit();
}

/*----------------------------------------------------------------------------------------------
	Change the parameter that says whether or not we are showing inheritance in the dialog.
	Adjust the options offered by certain combo-boxes.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::SetCanWsInherit()
{
	bool fCanInherit = m_fCanStyleInherit || m_esiSel.m_ws != 0;
	if (m_fCanInherit != fCanInherit)
	{
		m_fCanInherit = fCanInherit;

		// Clear out the options that are based on whether or not inheritance can happen,
		// and regenerate them.

		HWND hwndUnderT = ::GetDlgItem(m_hwnd, kctidFfdUnder);

		::SendMessage(hwndUnderT, CB_RESETCONTENT, 0, 0);

		InitUnderlineCtl();
		InitOffsetCtl();
	}
	// Do this unconditionally, as the contents of this control change not only depending
	// on whether inheritance is possible, but also depending on whether m_esiSel.m_ws
	// is zero. (We could save the old value of this, but it doesn't seem worthwhile.)
	InitFontCtl();
}

/*----------------------------------------------------------------------------------------------
	Set the parameter that says whether or not we are showing the Font Features button.
	Hide or adjust controls.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::SetFontFeatures(bool fFeatures)
{
	if (m_fFeatInit)
	{
		// Don't call this method repeatedly every time we initialize a tab, because the
		// adjusted controls keep adjusting!
		Assert(fFeatures == m_fFeatures);
		return;
	}

	m_fFeatures = fFeatures;
	m_fFeatInit = true;

	// Hide "Font Features" button if not wanted.
	if (!m_fFeatures)
	{
		::ShowWindow(GetDlgItem(m_hwnd, kctidFfdFeatures), SW_HIDE);
	}
	else
	{
		// Make the "Font Feature" button be a pop-up menu button.
		AfButtonPtr qbtnFeat;
		qbtnFeat.Create();
		qbtnFeat->SubclassButton(m_hwnd, kctidFfdFeatures, kbtPopMenu, NULL, 0);

		// Move the nearby controls down a little.
		Rect rcDlg;
		::GetWindowRect(m_hwnd, &rcDlg);
		POINT ptTL = { 0, 0 };
		::ClientToScreen(m_hwnd, &ptTL);
		int dyTitle = (ptTL.y - rcDlg.top);

		int rgctid[5] = { kctidFfdOffset, kctidFfdPosLabel, kctidFfdByLabel,
			kctidFfdOffsetNum, kctidFfdOffsetSpin };
		for (int ictid = 0; ictid < 5; ictid++)
		{
			Rect rc;
			HWND hwndT = ::GetDlgItem(m_hwnd, rgctid[ictid]);
			::GetWindowRect(hwndT, &rc);
			int w = rc.Width();
			int h = rc.Height();
			rc.left -= rcDlg.left;
			rc.top -= rcDlg.top;
			rc.top -= dyTitle;
			rc.top += 18;
			::MoveWindow(hwndT, rc.left, rc.top, w, h, true);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the format font dialog with a sequence of TsTextProps (as obtained typically
	from VwSelection::GetSelectionProps). Run the dialog, and adjust vqttp to be the values
	that should be passed to SetSelectionProps if anything is altered. Return true if a change
	is needed.
	Note that TsTextProps that are not changed are therefore set to null. Ones that are
	changed result in a new TsTextProps. The caller becomes responsible for a ref count on
	the new objects. A safe way to deal use this method would be:

	*	if (AfStyleFntDlg::AdjustTsTextProps(m_hwnd, cttp, rgpttp, rgpvps))
	*	{
	*		// some change was made
	*		CheckHr(qvwsel->SetSelectionProps(cttp, rgpttp));
	*	}
	*	// The changed ones are all new, and we have an extra ref count on them.
	*	for (ITsTextProps * pttp = rgpttp; pttp < rgpttp + cttp; ++pttp)
	*		ReleaseObj(*pttp);
----------------------------------------------------------------------------------------------*/
// We don't seem to need this method in this form at present, and it has not been
// fully converted from the FmtFntDlg version, nor updated to use TtpVec etc..
#if 0
//bool AfStyleFntDlg::AdjustTsTextProps(HWND hwnd, int cttp, ITsTextProps ** prgpttp,
//	IVwPropertyStore ** prgpvps)
//{
//	Assert(cttp > 0);
//	AssertArray(prgpttp, cttp);
//	// These two sets are identical except that in places where chrpOld has FwStyledText::knUnspecified,
//	// chrpCur has a value taken from the vps.
//	LgCharRenderProps chrpOrig;
//	LgCharRenderProps chrpCur;
//	memset(&chrpOrig, 0, sizeof(chrpOrig));
//	memset(&chrpCur, 0, sizeof(chrpCur));
//
//	SmartBstr sbstrFontFamily;
//	StrUni stuFfOrig;
//	StrUni stuFfCur;
//	StrUni stuInherit = kstidInherit;
//	HRESULT hr;
//
//	// Load a value for each property for each ttp/vps. If we get a different answer for any
//	// of them, change to conflicting.
//	int ittp;
//	for (ittp = 0; ittp < cttp; ittp++)
//	{
//		bool fFirst = ittp == 0;
//		ITsTextProps * pttp = prgpttp[ittp];
//		IVwPropertyStore * pvps = prgpvps[ittp];
//		MergeStringProp(pttp, pvps, ktptFontFamily, stuFfOrig, stuFfCur, fFirst, L"");
//		MergeIntProp(pttp, pvps, ktptItalic, ktpvEnum, chrpOrig.ttvItalic, chrpCur.ttvItalic,
//			fFirst);
//		MergeIntProp(pttp, pvps, ktptBold, ktpvEnum, chrpOrig.ttvBold, chrpCur.ttvBold, fFirst);
//		MergeIntProp(pttp, pvps, ktptSuperscript, ktpvEnum, chrpOrig.ssv, chrpCur.ssv, fFirst);
//		MergeIntProp(pttp, pvps, ktptUnderline, ktpvEnum, chrpOrig.unt, chrpCur.unt, fFirst);
//		MergeIntProp(pttp, pvps, ktptFontSize, ktpvMilliPoint, chrpOrig.dympHeight,
//			chrpCur.dympHeight, fFirst);
//		MergeIntProp(pttp, pvps, ktptOffset, ktpvMilliPoint, chrpOrig.dympOffset,
//			chrpCur.dympOffset, fFirst);
//		// We can trick it into using the regular Merge for now.
//		Assert(isizeof(uint) == isizeof(int));
//		int orig, cur;
//		orig = (int)(chrpOrig.clrFore);
//		cur = (int)(chrpCur.clrFore);
//		MergeIntProp(pttp, pvps, ktptForeColor, ktpvDefault, orig, cur, fFirst);
//		chrpOrig.clrFore = (uint) orig;
//		chrpCur.clrFore = (uint) cur;
//
//		orig = (int)(chrpOrig.clrBack);
//		cur = (int)(chrpCur.clrBack);
//		MergeIntProp(pttp, pvps, ktptBackColor, ktpvDefault, orig, cur, fFirst);
//		chrpOrig.clrBack = (uint) orig;
//		chrpCur.clrBack = (uint) cur;
//	}
//
//	// Now make an instance, initialize it, and run it.
//	// Note that we need chrpOrig, with 'inherit' as possible values, so that we can tell the
//	// difference between two runs where one has a particular value explicitly, and the other
//	// inherits the same value.  However, we also need 'Old' and 'Cur', initially identical, so
//	// we can tell whether anything changed while running the dialog.  We are actually done with
//	// 'Orig' now, except for possible use in the "Remove formatting" dialog
//	AfStyleFntDlgPtr qasfd;
//	qasfd.Create();
//	qasfd->SetDlgValues(chrpCur);
//	qasfd->m_stuFfOld = stuFfCur;
//	qasfd->m_stuFfCur = stuFfCur;
//	if (qasfd->DoModal(hwnd) != kctidOk)
//		return false;
//
//	ITsPropsBldrPtr qtpb;
//
//	LgCharRenderProps chrpNew;
//	qasfd->GetDlgValues(chrpNew);
//	LgCharRenderProps chrpOld = qasfd->m_chrpOld;
//
//	bool fChanged = false;
//	// Now see what changes we have to deal with.
//	for (ittp = 0; ittp < cttp; ittp++)
//	{
//		ITsTextProps * pttp = prgpttp[ittp];
//		qtpb = NULL;
//		StrUni stuNewVal;
//		// If this property has not changed, do nothing.
//		if (qasfd->m_stuFfOld == qasfd->m_stuFfCur)
//			goto LIntProps;
//		CheckHr(hr = pttp->GetStrPropValue(ktptFontFamily, &sbstrFontFamily));
//		stuNewVal = sbstrFontFamily.Chars();
//		// If this particular ttp already has the correct value, do nothing.
//		if (stuNewVal == qasfd->m_stuFfCur)
//			goto LIntProps;
//		// If we don't already have a builder, make one.
//		if (!qtpb)
//			CheckHr(pttp->GetBldr(&qtpb));
//		// ENHANCE JohnL(?): eventually, we will probably need to remove this kludge when we can fully
//		// handle three kinds of default fonts.
//		StrUni stuSetVal = qasfd->m_stuFfCur;
//		if (stuSetVal == "<default>") // ENHANCE JohnL(?): change this when we fully support 3 default fonts
//			stuSetVal = "<default font>";
//		CheckHr(qtpb->SetStrPropValue(ktptFontFamily, stuSetVal.Bstr()));
//LIntProps:
//		UpdateProp(pttp, ktptItalic, ktpvEnum, qtpb, chrpOld.ttvItalic, chrpNew.ttvItalic);
//		UpdateProp(pttp, ktptBold, ktpvEnum, qtpb, chrpOld.ttvBold, chrpNew.ttvBold);
//		UpdateProp(pttp, ktptSuperscript, ktpvEnum, qtpb, chrpOld.ssv, chrpNew.ssv);
//		UpdateProp(pttp, ktptUnderline, ktpvEnum, qtpb, chrpOld.unt, chrpNew.unt);
//		UpdateProp(pttp, ktptFontSize, ktpvMilliPoint, qtpb, chrpOld.dympHeight,
//			chrpNew.dympHeight);
//		UpdateProp(pttp, ktptOffset, ktpvMilliPoint, qtpb, chrpOld.dympOffset,
//			chrpNew.dympOffset);
//		UpdateProp(pttp, ktptForeColor, ktpvDefault, qtpb, chrpOld.clrFore, chrpNew.clrFore);
//		UpdateProp(pttp, ktptBackColor, ktpvDefault, qtpb, chrpOld.clrBack, chrpNew.clrBack);
//
//		// If anything changed, we now have a props builder that is the new value for this run.
//		if (qtpb)
//		{
//			CheckHr(qtpb->GetTextProps(&prgpttp[ittp]));
//			fChanged = true;
//		}
//		else
//		{
//			prgpttp[ittp] = NULL;
//		}
//	}
//	return fChanged;
//} // AfStyleFntDlg::AdjustTsTextProps. Currently not needed.
#endif

/*----------------------------------------------------------------------------------------------
	Get focus.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::SetActive() // (HWND hwndDialog)
{
	return true;
}

/*----------------------------------------------------------------------------------------------
	Add a string to the stu. If the string is not empty, put ", " before it
----------------------------------------------------------------------------------------------*/
void AddToDesc(StrApp & strDesc, StrApp strItem)
{
	if (strDesc.Length())
	{
		if (strItem.Length())
			strDesc.FormatAppend(_T(", %s"), strItem.Chars());
	}
	else
	{
		strDesc = strItem;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle one property from the TsTextProps. This is used only by GetDefaultSettings.
	It reads property tpt, and if its variation is varExpected, sets ivalRet (typically a field
	in a WsStyleInfo) to val, and xEOrIRet (typically a field in a ChrpInheritance) to xEorI.
	Note that the default ws settings (unlike the named ws settings) are not ordered by the
	XML import so cannot be found by the binary search algorithm. Here we therefore do a linear
	search. This is slower, but the number of default properties is small except for Normal,
	and even that has relatively few.
----------------------------------------------------------------------------------------------*/
void ReadOneSetting(ITsTextProps * pttp, int tpt, int varExpected, int xEorI, int & ivalRet,
	int & xEorIRet)
{
	int var, val, ctip, itip, tptAtIndex;
	CheckHr(pttp->get_IntPropCount(&ctip));
	Assert (ctip >=0);
	if (!ctip)
		return;	// No integer properties.
	for (itip = 0; itip < ctip; ++itip)	// Go through the int properties one by one.
	{
		CheckHr(pttp->GetIntProp(itip, &tptAtIndex, &var, &val));
		if (tptAtIndex == tpt)
			break;
	}
	if (itip < ctip && var == varExpected)
	{
		ivalRet = val;
		xEorIRet = xEorI;
	}
}

/*----------------------------------------------------------------------------------------------
	Same but taking a COLORREF
----------------------------------------------------------------------------------------------*/
void ReadOneSetting(ITsTextProps * pttp, int tpt, int varExpected, int xEorI, COLORREF & ivalRet,
	int & xEorIRet)
{
	int var, val, ctip, itip, tptAtIndex;
	CheckHr(pttp->get_IntPropCount(&ctip));
	Assert (ctip >=0);
	if (!ctip)
		return;	// No integer properties.
	for (itip = 0; itip < ctip; ++itip)	// Go through the int properties one by one.
	{
		CheckHr(pttp->GetIntProp(itip, &tptAtIndex, &var, &val));
		if (tptAtIndex == tpt)
			break;
	}
	if (itip < ctip && var == varExpected)
	{
		ivalRet = val;
		xEorIRet = xEorI;
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the WsStyleInfo from the default settings in the pttp. For each property found,
	set the appropriate field in chrpi to xEorI, which is either kxInherited or kxExplicit.
----------------------------------------------------------------------------------------------*/
void GetDefaultSettings(ITsTextProps * pttp, WsStyleInfo & esi, ChrpInheritance & chrpi,
	int xEorI)
{
	SmartBstr sbstr;
	CheckHr(pttp->GetStrPropValue(ktptFontFamily, &sbstr));
	if (sbstr.Length() > 0)
	{
		esi.m_stuFontFamily.Assign(sbstr.Chars(), sbstr.Length());
		chrpi.xFont = xEorI;
	}
	CheckHr(pttp->GetStrPropValue(ktptFontVariations, &sbstr));
	if (sbstr.Length() > 0)
	{
		esi.m_stuFontVar.Assign(sbstr.Chars(), sbstr.Length());
		chrpi.xFontVar = xEorI;
	}

	ReadOneSetting(pttp, ktptFontSize, ktpvMilliPoint, xEorI, esi.m_mpSize, chrpi.xSize);
	ReadOneSetting(pttp, ktptBold, ktpvEnum, xEorI, esi.m_fBold, chrpi.xBold);
	ReadOneSetting(pttp, ktptItalic, ktpvEnum, xEorI, esi.m_fItalic, chrpi.xItalic);
	ReadOneSetting(pttp, ktptSuperscript, ktpvEnum, xEorI, esi.m_ssv, chrpi.xSs);
	ReadOneSetting(pttp, ktptForeColor, ktpvDefault, xEorI, esi.m_clrFore, chrpi.xFore);
	ReadOneSetting(pttp, ktptBackColor, ktpvDefault, xEorI, esi.m_clrBack, chrpi.xBack);
	ReadOneSetting(pttp, ktptUnderColor, ktpvDefault, xEorI, esi.m_clrUnder, chrpi.xUnder);
	ReadOneSetting(pttp, ktptUnderline, ktpvEnum, xEorI, esi.m_unt, chrpi.xUnderT);
	ReadOneSetting(pttp, ktptOffset, ktpvMilliPoint, xEorI, esi.m_mpOffset, chrpi.xOffset);
}

/*----------------------------------------------------------------------------------------------
	Set the dialog values.
	The kspWsStyle field for a style Ttp stores a complex string indicating what font props
	are overridden for what encodings.
	The information about multiple encodings is stored sorted by writing system code.
	For each writing system we store the writing system number in two characters (least
	significant word first), the the length of the font name, if any, in wchars, and the font
	name itself (no null termination!).
	If the font length is negative additional string props are stored.  See FwStyleText.cpp
	for details.
	After that is a count of integer properties, stored in a single character, followed
	by the integer properties, each stored as four characters: ttp, var, val lsw, val msw.
	The next writing system follows the last integer property of the previous one.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::SetDlgValues(ITsTextProps * pttp, ITsTextProps * pttpInherited,
	bool fCanInherit, bool fFontFeatures, bool f1DefaultFont,
	bool fCharStyle, Vector<int> & vwsProj)
{
	m_fCharStyle = fCharStyle;
	SetCanInherit(fCanInherit);  // false for Normal style
	SetFontFeatures(fFontFeatures);
	m_f1DefaultFont = f1DefaultFont;

	AssertPtr(m_qwsf);
	IWritingSystemPtr qws;

	m_vesi.Clear();
	m_vesiI.Clear();
	m_vchrpi.Clear();
	Vector<int> vwsExtra; // to keep track of encodings with no values specified
	vwsExtra = vwsProj;

	ListView_DeleteAllItems(m_hwndLangList);

	Vector<int> vwsSoFar;

	// Add a record for writing system '0', which holds the default settings pseudo-ws.
	FwStyledText::FindOrAddWsInfo(0, m_vesiI, m_vesi, m_vchrpi, vwsSoFar);
	GetDefaultSettings(pttpInherited, m_vesiI[0], m_vchrpi[0], kxInherited);
	GetDefaultSettings(pttp, m_vesi[0], m_vchrpi[0], kxExplicit);

	// Decode the inherited styles and then the explicit styles.
	SmartBstr sbstrCharStylesI;
	CheckHr(pttpInherited->GetStrPropValue(ktptWsStyle, &sbstrCharStylesI));
	if (sbstrCharStylesI.Length())
	{
		FwStyledText::DecodeFontPropsString(sbstrCharStylesI, false,
			m_vesiI, m_vesi, m_vchrpi, vwsSoFar, vwsExtra);
	}
	SmartBstr sbstrCharStyles;
	CheckHr(pttp->GetStrPropValue(ktptWsStyle, &sbstrCharStyles));
	if (sbstrCharStyles.Length())
	{
		FwStyledText::DecodeFontPropsString(sbstrCharStyles, true,
			m_vesiI, m_vesi, m_vchrpi, vwsSoFar, vwsExtra);
	}

	// OK, we've initialized all the ones for encodings that occur in either of the ttps.
	// If there are some in the language project that are not in the ttp, add
	// lines for them, too.
	for (int iws = 0; iws < vwsExtra.Size(); iws++)
	{
		int ws = vwsExtra[iws];
		FwStyledText::FindOrAddWsInfo(ws, m_vesiI, m_vesi, m_vchrpi, vwsSoFar);
	}

	SetInheritedDefaults();

	// Add to the list control only those writing systems which are in vwsProj, i.e. those which
	// are currently in either the Vernacular or Analysis WS list for this project.
	// Mark as not selected (in m_vesi) any which we are not adding.
	StrApp strT;
	int cws = vwsSoFar.Size();
	int cwsProj = vwsProj.Size();
	for (int iws = 0; iws < cws; iws++)
	{
		int ws = vwsSoFar[iws];

		strT.Clear(); // The string to insert into the list as the name of the ws.
		if (iws == 0)
		{
			// Retreive default settings string.
			strT.Load(kstidDefaultSettings);
		}
		else
		{
			int i;
			for (i = 0; i < cwsProj; ++i)
			{
				if (ws == vwsProj[i])
					break;
			}
			if (i == cwsProj)
			{
				m_vesi[iws].m_fSelected = false;
				continue;	// Ignore this one as it is not in vwsProj.
			}
			CheckHr(m_qwsf->get_EngineOrNull(ws, &qws));
			AssertPtr(qws);
			SmartBstr sbstr;
			CheckHr(qws->get_UiName(m_pafsd->GetUserWs(), &sbstr));
			strT.Assign(sbstr.Chars(), sbstr.Length());
		}

		LVITEM lvi = {0};
		// psztext, state & lParam fields will be used.
		lvi.mask = LVIF_TEXT | LVIF_STATE | LVIF_PARAM;
		lvi.iItem = iws;
		Assert(m_vesi[iws].m_ws == ws);

		Assert(strT.Length());
		lvi.pszText = const_cast<achar *>(strT.Chars());
		lvi.lParam = iws;			// Tag the row with the m_vesi index.
		if (vwsSoFar[iws] == 0)
			lvi.state = LVIS_SELECTED;	// Only 'default settings' is initially selected.
		else
			m_vesi[iws].m_fSelected = false; // So clear the flag on the others.
		ListView_InsertItem(m_hwndLangList, &lvi);
	}

	SetFontVarSettings();
	m_fDirty = false;
	FillCtls();
	UpdateDescrips(); // After FillCtls, because it calls ReadCtls.
}

/*----------------------------------------------------------------------------------------------
	Fill up the dialog with just a list of encodings; leave the properties in some default
	state.
	Review JohnT: should we create a <default settings> row in this case?
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::SetDlgWsValues(Vector<int> & vwsProj)
{
	m_vesi.Clear();
	StrApp strT;
	ListView_DeleteAllItems(m_hwndLangList);

	AssertPtr(m_qwsf);
	IWritingSystemPtr qws;
	for (int iws = 0; iws < vwsProj.Size(); iws++)
	{
		int ws = vwsProj[iws];
		// Figure where to insert it
		int iesi;
		for (iesi = 0; iesi < m_vesi.Size(); iesi++)
		{
			if ((unsigned int)ws < (unsigned int)m_vesi[iesi].m_ws)
				break;
		}
		m_vesi.Insert(iesi, WsStyleInfo()); // New style with everything knNinch
		m_vesi[iesi].m_ws = ws;
		// Now also insert into the list control.
		LVITEM lvi = {0};
		lvi.mask = LVIF_TEXT | LVIF_STATE; // text and state fields will be used
		lvi.iItem = iesi;
		strT.Clear();
		CheckHr(m_qwsf->get_EngineOrNull(ws, &qws));
		AssertPtr(qws);
		SmartBstr sbstr;
		CheckHr(qws->get_UiName(m_pafsd->GetUserWs(), &sbstr));
		strT.Assign(sbstr.Chars(), sbstr.Length());
		Assert(strT.Length());
		lvi.pszText = const_cast<achar *>(strT.Chars());
		if (iesi == 0)
			lvi.state = LVIS_SELECTED; // arbitrarily select the first.
		ListView_InsertItem(m_hwndLangList, &lvi);
	}
	m_fDirty = false;

	int citem = ListView_GetItemCount(m_hwndLangList);
	citem = citem;

	SetFontVarSettings();
	FillCtls();
	UpdateDescrips(); // After FillCtls, because it calls ReadCtls.
}

/*----------------------------------------------------------------------------------------------
	Fill in the bare minimum of defaults. Also, if inheritance is not allowed,
	merge all the explicit and inherited values and pretend that they are all explicit.

	Note: setting default values for the Normal style should not be necessary any more,
	because Normal is always fully initialized.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::SetInheritedDefaults()
{
	Assert(m_vesi.Size() == m_vesiI.Size());
	Assert(m_vesi.Size() == m_vchrpi.Size());

	for (int iesi = 0; iesi < m_vesi.Size(); iesi++)
	{
		WsStyleInfo & esi = m_vesi[iesi];
		WsStyleInfo & esiI = m_vesiI[iesi];
		ChrpInheritance & chrpi = m_vchrpi[iesi];

		if (m_fCanInherit || esi.m_ws)	// We can always inherit if m_ws != 0 (JohnL).
		{
			if (esiI.m_stuFontFamily == L"" && !m_fCharStyle)
				esiI.m_stuFontFamily = FwStyledText::FontDefaultUi(m_f1DefaultFont);
			if (esiI.m_mpSize == 0 || esiI.m_mpSize == FwStyledText::knUnspecified)	// JohnL
				esiI.m_mpSize = (m_fCharStyle) ? FwStyledText::knUnspecified : kmpDefaultSize;
			if (esiI.m_fBold == FwStyledText::knUnspecified)
				esiI.m_fBold = kttvOff;
			if (esiI.m_fItalic == FwStyledText::knUnspecified)
				esiI.m_fItalic = kttvOff;
			if (esiI.m_ssv == FwStyledText::knUnspecified)
				esiI.m_ssv = kttvOff;
			if (esiI.m_unt == FwStyledText::knUnspecified && !m_fCharStyle)
				esiI.m_unt = kuntNone;
			if (esiI.m_mpOffset == FwStyledText::knUnspecified)
				// conflicting makes the control empty
				esiI.m_mpOffset = (m_fCharStyle) ? FwStyledText::knConflicting : 0;
		}
		else
		{
			if (chrpi.xFont == kxInherited)
				esi.m_stuFontFamily = esiI.m_stuFontFamily;
			chrpi.xFont = kxExplicit;
			if (esi.m_stuFontFamily == L"")
				esi.m_stuFontFamily = FwStyledText::FontDefaultUi(m_f1DefaultFont);

			if (chrpi.xSize == kxInherited)
				esi.m_mpSize = esiI.m_mpSize;
			chrpi.xSize = kxExplicit;
			if (esi.m_mpSize == 0 || esi.m_mpSize == FwStyledText::knUnspecified)
				esi.m_mpSize = kmpDefaultSize;

			if (chrpi.xOffset == kxInherited)
				esi.m_mpOffset = esiI.m_mpOffset;
			chrpi.xOffset = kxExplicit;

			if (chrpi.xBold == kxInherited)
				esi.m_fBold = esiI.m_fBold;
			chrpi.xBold = kxExplicit;
			if (esi.m_fBold == FwStyledText::knUnspecified)
				esi.m_fBold = kttvOff;

			if (chrpi.xItalic == kxInherited)
				esi.m_fItalic = esiI.m_fItalic;
			chrpi.xItalic = kxExplicit;
			if (esi.m_fItalic == FwStyledText::knUnspecified)
				esi.m_fItalic = kttvOff;

			if (chrpi.xSs == kxInherited)
				esi.m_ssv = esiI.m_ssv;
			chrpi.xSs = kxExplicit;
			if (esi.m_ssv == FwStyledText::knUnspecified)
				esi.m_ssv = kttvOff;

			if (chrpi.xFore == kxInherited)
				esi.m_clrFore = esiI.m_clrFore;
			chrpi.xFore = kxExplicit;

			if (chrpi.xBack == kxInherited)
				esi.m_clrBack = esiI.m_clrBack;
			chrpi.xBack = kxExplicit;

			if (chrpi.xUnder == kxInherited)
				esi.m_clrUnder = esiI.m_clrUnder;
			chrpi.xUnder = kxExplicit;

			if (chrpi.xUnderT == kxInherited)
				esi.m_unt = esiI.m_unt;
			chrpi.xUnderT = kxExplicit;
			if (esi.m_unt == FwStyledText::knUnspecified)
				esi.m_unt = kuntNone;

			if (chrpi.xFontVar == kxInherited)
				esi.m_stuFontVar = esiI.m_stuFontVar;
			chrpi.xFont = kxExplicit;
		}
	}

	m_fFfConflictI = false;
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_ERASEBKGND)
	{
		// This is needed because of a bug in the list view control that causes
		// it not to be redrawn sometimes.
		::RedrawWindow(::GetDlgItem(m_hwnd, kctidAsfdLangList), NULL, NULL,
			RDW_ERASE | RDW_FRAME | RDW_INVALIDATE);
	}

	if (wm == WM_CTLCOLOREDIT || wm == WM_CTLCOLORBTN || wm == WM_CTLCOLORSTATIC
		|| wm == WM_CTLCOLORLISTBOX || wm == WM_CTLCOLORDLG)
	{
		return ColorForInheritance(wp, lp, lnRet);
	}

	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Set the color of the control based on whether the value is inherited.
	TODO: merge this code with the FmtFntDlg version.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::ColorForInheritance(WPARAM wp, LPARAM lp, long & lnRet)
{
	HWND hwndFont = ::GetDlgItem(m_hwnd, kctidFfdFont);
	HWND hwndSize = ::GetDlgItem(m_hwnd, kctidFfdSize);
	HWND hwndUnderT = ::GetDlgItem(m_hwnd, kctidFfdUnder);
	HWND hwndOffsetLst = ::GetDlgItem(m_hwnd, kctidFfdOffset);
	HWND hwndOffsetNum = ::GetDlgItem(m_hwnd, kctidFfdOffsetNum);
//	HWND hwndBold = ::GetDlgItem(m_hwnd, kctidFfdBold);
//	HWND hwndItalic = ::GetDlgItem(m_hwnd, kctidFfdItalic);
//	HWND hwndSuper = ::GetDlgItem(m_hwnd, kctidFfdSuper);
//	HWND hwndSub = ::GetDlgItem(m_hwnd, kctidFfdSub);
	HWND hwndForeC = ::GetDlgItem(m_hwnd, kctidFfdForeClr);
	HWND hwndBackC = ::GetDlgItem(m_hwnd, kctidFfdBackClr);
	HWND hwndUnderC = ::GetDlgItem(m_hwnd, kctidFfdUnderClr);

	HWND hwndArg = (HWND)lp;

	int xExpl;
	if (hwndArg == hwndFont)
		xExpl = m_chrpi.xFont;
	else if (hwndArg == hwndSize)
		xExpl = m_chrpi.xSize;
	else if (hwndArg == hwndUnderT)
		xExpl = m_chrpi.xUnderT;
	else if (hwndArg == hwndOffsetLst || hwndArg == hwndOffsetNum)
		xExpl = m_chrpi.xOffset;
	// Check boxes aren't handled this way:
//	else if (hwndArg == hwndBold)
//		xExpl = m_chrpi.xBold;
//	else if (hwndArg == hwndItalic)
//		xExpl = m_chrpi.xItalic;
//	else if (hwndArg == hwndSuper || hwndArg == hwndSub)
//		xExpl = m_chrpi.xSs;
	else if (hwndArg == hwndForeC)
	{
		xExpl = m_chrpi.xFore;
		if (xExpl == kxExplicit)
			m_qccmbF->SetLabelColor(::GetSysColor(COLOR_WINDOWTEXT));
		else if (xExpl == kxInherited)
			m_qccmbF->SetLabelColor(::GetSysColor(COLOR_3DSHADOW));
		else
			m_qccmbF->SetLabelColor(kclrGreen); // bug
		// I don't know why we return false here, but when we returned true, the control
		// flashed bizarrely. - SharonC
		return false;
	}
	else if (hwndArg == hwndBackC)
	{
		xExpl = m_chrpi.xBack;
		if (xExpl == kxExplicit)
			m_qccmbB->SetLabelColor(::GetSysColor(COLOR_WINDOWTEXT));
		else if (xExpl == kxInherited)
			m_qccmbB->SetLabelColor(::GetSysColor(COLOR_3DSHADOW));
		else
			m_qccmbB->SetLabelColor(kclrGreen); // bug
		return false;
	}
	else if (hwndArg == hwndUnderC)
	{
		xExpl = m_chrpi.xUnder;
		if (xExpl == kxExplicit)
			m_qccmbU->SetLabelColor(::GetSysColor(COLOR_WINDOWTEXT));
		else if (xExpl == kxInherited)
			m_qccmbU->SetLabelColor(::GetSysColor(COLOR_3DSHADOW));
		else
			m_qccmbU->SetLabelColor(kclrGreen); // bug

		return false;
	}
	else
		return false;

	if (xExpl == kxExplicit)
		::SetTextColor((HDC)wp, ::GetSysColor(COLOR_WINDOWTEXT)); // black
	else if (xExpl == kxInherited)
		::SetTextColor((HDC)wp, ::GetSysColor(COLOR_3DSHADOW)); // gray
	else
		::SetTextColor((HDC)wp, kclrGreen); // bug

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
	Update all description fields.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::UpdateDescrips()
{
	ReadCtls(); // Get the data back into the array.

	// Now go through the list and then update each line according to its corresponding vector
	// index. (Note: the vector may have more entries than the list).
	int cItem = ListView_GetItemCount(m_hwndLangList);
	int iItem;
	int iesi;
	LVITEM lvi;
	lvi.mask = LVIF_PARAM;
	lvi.iSubItem = 0;
	for (iItem = 0; iItem < cItem; ++iItem)
	{
		lvi.iItem = iItem;	// Index of item in list.
		ListView_GetItem(m_hwndLangList, &lvi);
		iesi = lvi.lParam;		// Tag for this item, which is m_vesi index.
		WsStyleInfo & esi = m_vesi[iesi];
		WsStyleInfo & esiI = m_vesiI[iesi];
		ChrpInheritance & chrpi = m_vchrpi[iesi];

		StrApp strDesc; // Build up the description here
		strDesc = FwStyledText::FontStringMarkupToUi(m_f1DefaultFont, esi.m_stuFontFamily);
		StrAppBuf strb; // a temp, used for making strings with units
		StrApp strT;
		if (esi.m_mpSize != FwStyledText::knUnspecified)
		{
			AfUtil::MakeMsrStr (esi.m_mpSize , knpt, &strb);
			strT = strb.Chars();
			AddToDesc(strDesc, strT);
		}
		if (esi.m_fBold == kttvForceOn || esi.m_fBold == kttvInvert)
		{
			strT.Load(kstidBold);
			AddToDesc(strDesc, strT);
		} // review PM(JohnT): should we put something if the style forces it off?
		if (esi.m_fItalic == kttvForceOn || esi.m_fItalic == kttvInvert)
		{
			strT.Load(kstidItalic);
			AddToDesc(strDesc, strT);
		} // review PM(JohnT): should we put something if the style forces it off?
		strT = "";
		if (esi.m_ssv == kssvSuper)
			strT.Load(kstidFfdSuperscript);
		else if (esi.m_ssv == kssvSub)
			strT.Load(kstidFfdSubscript);
		AddToDesc(strDesc, strT);

		// If we have a foreground color but no background display something like "Text is red"
		if (esi.m_clrFore != FwStyledText::knUnspecified &&
			esi.m_clrBack == FwStyledText::knUnspecified)
		{
			StrApp strT2;
			strT2.Load(g_ct.GetColorRid(g_ct.GetIndexFromColor(esi.m_clrFore)));
			StrApp strT3;
			strT3.Load(kstidTextIsFmt);
			strT.Format(strT3.Chars(), strT2.Chars());
			AddToDesc(strDesc, strT);
		}
		else if (esi.m_clrBack != FwStyledText::knUnspecified)
		{
			StrApp strT2;
			strT2.Load(g_ct.GetColorRid(g_ct.GetIndexFromColor(esi.m_clrBack)));
			StrApp strT3;
			strT3.Load(kstidTextOnFmt);
			StrApp strT4;
			if (esi.m_clrFore != FwStyledText::knUnspecified)
				strT4.Load(g_ct.GetColorRid(g_ct.GetIndexFromColor(esi.m_clrFore)));
			strT.Format(strT3.Chars(), strT4.Chars(), strT2.Chars());
			AddToDesc(strDesc, strT);
		}

		// If either the underline type has been explicitly set, or the underline color is set
		// and the underline type is inherited and non-null, include it in the description.
		StrApp strColor;
		COLORREF clr = (COLORREF)FwStyledText::knUnspecified;
		if (esi.m_clrUnder != FwStyledText::knUnspecified)
		{
			clr = esi.m_clrUnder;
		}
		else if (chrpi.xUnder == kxInherited &&
			esiI.m_clrUnder != (COLORREF)FwStyledText::knUnspecified)
		{
			clr = esiI.m_clrUnder;
		}
		if (clr != (COLORREF)FwStyledText::knUnspecified)
			strColor.Load(g_ct.GetColorRid(g_ct.GetIndexFromColor(clr)));
		StrApp strFmt;
		int unt = esi.m_unt;
		if (chrpi.xUnderT == kxInherited && chrpi.xUnder == kxExplicit)
			unt = esiI.m_unt;
		switch (unt)
		{
		case kuntDotted:
			strFmt.Load(kstidDottedUnderFmt);
			break;
		case kuntDashed:
			strFmt.Load(kstidDashedUnderFmt);
			break;
		case kuntStrikethrough:
			strFmt.Load(kstidStrikethroughUnderFmt);
			break;
		case kuntSingle:
			strFmt.Load(kstidSingleUnderFmt);
			break;
		case kuntDouble:
			strFmt.Load(kstidDoubleUnderFmt);
			break;
		default:
			break;
		}
		// Both these lines are harmless if strFmt is still empty.
		strT.Format(strFmt.Chars(), strColor.Chars());
		AddToDesc(strDesc, strT);
		strFmt = "";
		if (esi.m_mpOffset && esi.m_mpOffset != FwStyledText::knUnspecified)
		{
			if (esi.m_mpOffset < 0)
				strFmt.Load(kstidLoweredFmt);
			else
				strFmt.Load(kstidRaisedFmt);
			StrApp strAmt;
			StrAppBuf strb;
			AfUtil::MakeMsrStr (abs(esi.m_mpOffset) , knpt, &strb);
			strT.Format(strFmt.Chars(), strb.Chars());
			AddToDesc(strDesc, strT);
		}

		ListView_SetItemText(m_hwndLangList, iItem, 1, const_cast<achar *>(strDesc.Chars()));
	}
} // AfStyleFntDlg::UpdateDescrips().

/*----------------------------------------------------------------------------------------------
	Draw the appropriate string or bitmap in the Underline Style combo box.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::UpdateUnderlineStyle(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);

	// Set up to paint.

	Rect rcT(pdis->rcItem);
	// This is necessary to make items in the menu highlight as the user drags
	// over them. The second condition prevents some very odd appearance in the
	// main edit box after selecting an item.
	//if ((pdis->itemState & ODS_SELECTED) && !(pdis->itemState & ODS_COMBOBOXEDIT))

	HGDIOBJ tmptmp = ::GetStockObject(BLACK_PEN);
	HPEN hpenOld = (HPEN)::SelectObject(pdis->hDC, tmptmp);
//	HGDIOBJ tmptmp = ::GetStockObject(DC_PEN);
//	HPEN hpenOld = (HPEN)::SelectObject(pdis->hDC, ::GetStockObject(DC_PEN));
	::SetBkMode(pdis->hDC, TRANSPARENT);

	AfGfx::FillSolidRect(pdis->hDC, rcT, (pdis->itemState & ODS_SELECTED ?
		::GetSysColor(COLOR_HIGHLIGHT) : ::GetSysColor(COLOR_WINDOW)));

	HPEN hpen;

	if ((pdis->itemState & ODS_COMBOBOXEDIT) && (m_chrpi.xUnderT == kxInherited))
	{
		COLORREF clrGray = ::GetSysColor(COLOR_3DSHADOW);
		// For the line segments, I'm making the gray a little lighter than
		// what's used for the other widgets, because it's hard to tell the difference
		// between gray and black. ENHANCE: if there is a way for this to be some color other
		// than gray, rework.
		if (pdis->itemData != kstidFfdNone && pdis->itemData != kstidFfdUnspecified)
			clrGray = kclrLightGray;

		AfGfx::SetTextColor(pdis->hDC, clrGray);
		hpen = ::CreatePen(PS_SOLID, 0, clrGray);
		::SelectObject(pdis->hDC, hpen);
	}
	else
	{
		AfGfx::SetTextColor(pdis->hDC, (pdis->itemState & ODS_SELECTED ?
			::GetSysColor(COLOR_HIGHLIGHTTEXT) : ::GetSysColor(COLOR_WINDOWTEXT)));

		hpen = ::CreatePen(PS_SOLID, 0, (pdis->itemState & ODS_SELECTED ?
			::GetSysColor(COLOR_HIGHLIGHTTEXT) : ::GetSysColor(COLOR_WINDOWTEXT)));

		::SelectObject(pdis->hDC, hpen);
	}

	int y = pdis->rcItem.top + (pdis->rcItem.bottom - pdis->rcItem.top) / 2;
	int xLeft = pdis->rcItem.left + 2;
	int xRight = pdis->rcItem.right - 2;
	StrApp str;
	int x;
	int x2;
	switch (pdis->itemData)
	{
	case -1: // Shows up when value is conflicting.
		break;
	case kstidFfdUnspecified:
		str.Load(kstidFfdUnspecified);
		::TextOut(pdis->hDC, pdis->rcItem.left + 2, pdis->rcItem.top + 1,
			str.Chars(), str.Length());
		break;
	case kstidFfdNone:
		str.Load(kstidFfdNone);
		::TextOut(pdis->hDC, pdis->rcItem.left + 2, pdis->rcItem.top + 1,
			str.Chars(), str.Length());
		break;
	case kstidFfdDotted:
		for (x = xLeft; x < xRight; x += 4)
		{
			::MoveToEx(pdis->hDC, x, y, NULL);
			x2 = x + 2;
			if (x2 > xRight)
				x2 = xRight;
			::LineTo(pdis->hDC, x2, y);
		}
		break;
	case kstidFfdDashed:
		for (x = xLeft; x < xRight; x += 9)
		{
			::MoveToEx(pdis->hDC, x, y, NULL);
			x2 = x + 6;
			if (x2 > xRight)
				x2 = xRight;
			::LineTo(pdis->hDC, x2, y);
		}
		break;
	case kstidFfdStrikethrough:
		str.Load(kstidFfdStrikethrough);
		::TextOut(pdis->hDC, pdis->rcItem.left + 2, pdis->rcItem.top + 1,
			str.Chars(), str.Length());
		break;
	case kstidFfdSingle:
		::MoveToEx(pdis->hDC, xLeft, y, NULL);
		::LineTo(pdis->hDC, xRight, y);
		break;
	case kstidFfdDouble:
		::MoveToEx(pdis->hDC, xLeft, y - 1, NULL);
		::LineTo(pdis->hDC, xRight, y - 1);
		::MoveToEx(pdis->hDC, xLeft, y + 1, NULL);
		::LineTo(pdis->hDC, xRight, y + 1);
		break;
	default:
		Assert(false);			// THIS SHOULD NEVER HAPPEN!!
		break;
	}

	::SelectObject(pdis->hDC, hpenOld);

	if (hpen)
		::DeleteObject(hpen);

} // AfStyleFntDlg::UpdateUnderlineStyle.

/*----------------------------------------------------------------------------------------------
	Handle one property from the TsTextProps. This is used only by GetDlgValues.
	It checks ival to see if it is conflicting, and ignores it if so.
	It checks ival to see if it is inherited (unspecified), and removes the value if so.
	Otherwise writes the value as property tpt of the builder, using the specified variation.
	Note: currently I don't think conflicting can occur, but in case we switch back to
	allowing multiple selections I've left it in.
----------------------------------------------------------------------------------------------*/
void WriteOneSetting(int tpt, int var, int nVal, ITsPropsBldr * ptpb)
{
	if (nVal == FwStyledText::knConflicting)
		return;
	if (nVal == FwStyledText::knUnspecified)
	{
		CheckHr(ptpb->SetIntPropValues(tpt, -1, -1));
		return;	// Remove values which are set to Unspecified (JohnL).
	}
	CheckHr(ptpb->SetIntPropValues(tpt, var, nVal));
}

/*----------------------------------------------------------------------------------------------
	Get the dialog values.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::GetDlgValues(ITsTextProps * pttp, ITsTextProps ** ppttp, bool fForPara)
{
	AssertPtr(ppttp);
	Assert(!*ppttp);
	if (!VerifySize())
		return false;
	if (!m_fDirty)
		return true; // No need to make a new ttp
	ReadCtls();
	for (int iesi = 0; iesi < m_vesi.Size(); iesi++)
	{
		m_vesi[iesi].m_stuFontFamily = FwStyledText::FontStringUiToMarkup(m_vesi[iesi].m_stuFontFamily);
	}
	StrUni stuStyle = FwStyledText::EncodeFontPropsString(m_vesi, fForPara);
	ITsPropsBldrPtr qtpb;
	if (pttp)
		CheckHr(pttp->GetBldr(&qtpb));
	else
		qtpb.CreateInstance(CLSID_TsPropsBldr);
	// And add the defaults if any to the props builder.
	WsStyleInfo & esi = m_vesi[0];

	if (esi.m_stuFontFamily.Length() > 0)
	{
		CheckHr(qtpb->SetStrPropValue(ktptFontFamily, esi.m_stuFontFamily.Bstr()));
	}

	WriteOneSetting(ktptFontSize, ktpvMilliPoint, esi.m_mpSize, qtpb);
	WriteOneSetting(ktptBold, ktpvEnum, esi.m_fBold, qtpb);
	WriteOneSetting(ktptItalic, ktpvEnum, esi.m_fItalic, qtpb);
	WriteOneSetting(ktptSuperscript, ktpvEnum, esi.m_ssv, qtpb);
	WriteOneSetting(ktptForeColor, ktpvDefault, esi.m_clrFore, qtpb);
	WriteOneSetting(ktptBackColor, ktpvDefault, esi.m_clrBack, qtpb);
	WriteOneSetting(ktptUnderColor, ktpvDefault, esi.m_clrUnder, qtpb);
	WriteOneSetting(ktptUnderline, ktpvEnum, esi.m_unt, qtpb);
	WriteOneSetting(ktptOffset, ktpvMilliPoint, esi.m_mpOffset, qtpb);

	// Get rid of any 'overrides' that are no different from the <default properties>.
	// Note that we must do this AFTER recording the default properties in the builder.
	stuStyle = FwStyledText::RemoveSpuriousOverrides(stuStyle, qtpb);
	if (stuStyle.Length() == 0)
		CheckHr(qtpb->SetStrPropValue(ktptWsStyle, NULL));	// delete any WsStyle value.
	else
		CheckHr(qtpb->SetStrPropValue(ktptWsStyle, stuStyle.Bstr()));

	CheckHr(qtpb->GetTextProps(ppttp));
	return true;
} // AfStyleFntDlg::GetDlgValues.

/*----------------------------------------------------------------------------------------------
	Decide the actual value to use for a particular property.
	The value is obtained from m_esiSel (passed as actual) if specified there;
	x, taken from m_chrpi, determines whether actual is OK to use;
	xDef, taken from m_vchrpi[0], determines whether to use defProps, taken from m_vesi[0];
	if also unspecified there, use baseVal, taken from m_esiSelI.
----------------------------------------------------------------------------------------------*/
template<typename T>
T GetActualPropValue(T actual, int x, int xDef, T defProps, T baseVal)
{
	if (x == kxInherited)
	{
		// Can't use the m_esiSel value, it's unspecified.
		if (xDef == kxExplicit)
		{
			// However, there is an explicit value in <default properties>, show that as
			// inherited.
			return defProps;
		}
		else
		{
			// Use the value inherited from the base style.
			return baseVal;
		}
	}
	return actual;
}

/*----------------------------------------------------------------------------------------------
	Fill the controls with their values.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::FillCtls(void)
{
	if (!m_hwnd)
		return;

	HWND hwndT;
	int iv;
	StrAppBuf strb;

	StrUni stuFf;
	int iesi;
	m_esiSel.Init(); // Set all to unspecified
	m_esiSelI.Init();
	m_chrpi.Init();

	bool fFoundFirstSel = false;
	m_fFfConflict = false;
	m_fFfConflictI = false;

	// JohnT: currently we only allow one selection, hence only one iteration of this
	// loop does anything, namely, copy the relevant record from vesi to m_esiSel,
	// and similarly for the other variables. We could replace this code with just the
	// lines in the else, but I'm not quite ready to throw away the multi-selection
	// capability (in case the analysts change their minds).
	for (iesi = 0; iesi < m_vesi.Size(); iesi++)
	{
		// Ignore items that are not selected. Use our own notion of what is selected;
		// we may get messages that cause this to be called, during the change process,
		// after the control has changed but before we have changed.
		if (!m_vesi[iesi].m_fSelected)
			continue;

		m_vesi[iesi].m_stuFontFamily = FwStyledText::FontStringMarkupToUi(m_f1DefaultFont,
			m_vesi[iesi].m_stuFontFamily);
		m_vesiI[iesi].m_stuFontFamily = FwStyledText::FontStringMarkupToUi(m_f1DefaultFont,
			m_vesiI[iesi].m_stuFontFamily);

		if (fFoundFirstSel)
		{
			WsStyleInfo & esi = m_vesi[iesi];
			WsStyleInfo & esiI = m_vesiI[iesi];
			ChrpInheritance & chrpi = m_vchrpi[iesi];

			if (esi.m_stuFontFamily != m_esiSel.m_stuFontFamily)
			{
				// Got a conflict--force it to empty.
				m_esiSel.m_stuFontFamily = L"";
				m_fFfConflict = true;
			}
			if (esiI.m_stuFontFamily != m_esiSelI.m_stuFontFamily)
			{
				m_esiSelI.m_stuFontFamily = L"";
				m_fFfConflictI = true;
			}
			if (chrpi.xFont != m_chrpi.xFont)
				m_chrpi.xFont = kxConflicting;

			Assert(isizeof(COLORREF) == isizeof(int));
			if (esi.m_clrBack != m_esiSel.m_clrBack)
				m_esiSel.m_clrBack = (COLORREF)FwStyledText::knConflicting;
			if (esiI.m_clrBack != m_esiSelI.m_clrBack)
				m_esiSelI.m_clrBack = (COLORREF)FwStyledText::knConflicting;
			if (chrpi.xBack != m_chrpi.xBack)
				m_chrpi.xBack = kxConflicting;

			if (esi.m_clrFore != m_esiSel.m_clrFore)
				m_esiSel.m_clrFore = (COLORREF)FwStyledText::knConflicting;
			if (esiI.m_clrFore != m_esiSelI.m_clrFore)
				m_esiSelI.m_clrFore = (COLORREF)FwStyledText::knConflicting;
			if (chrpi.xFore != m_chrpi.xFore)
				m_chrpi.xFore = kxConflicting;

			if (esi.m_clrUnder != m_esiSel.m_clrUnder)
				m_esiSel.m_clrUnder = (COLORREF)FwStyledText::knConflicting;
			if (esiI.m_clrUnder != m_esiSelI.m_clrUnder)
				m_esiSelI.m_clrUnder = (COLORREF)FwStyledText::knConflicting;
			if (chrpi.xUnder != m_chrpi.xUnder)
				m_chrpi.xUnder = kxConflicting;

			if (esi.m_unt != m_esiSel.m_unt)
				m_esiSel.m_unt = FwStyledText::knConflicting;
			if (esiI.m_unt != m_esiSelI.m_unt)
				m_esiSelI.m_unt = kxConflicting;
			if (chrpi.xUnderT != m_chrpi.xUnderT)
				m_chrpi.xUnderT = kxConflicting;

			if (esi.m_fBold != m_esiSel.m_fBold)
				m_esiSel.m_fBold = FwStyledText::knConflicting;
			if (esiI.m_fBold != m_esiSelI.m_fBold)
				m_esiSelI.m_fBold = FwStyledText::knConflicting;
			if (chrpi.xBold != m_chrpi.xBold)
				m_chrpi.xBold = kxConflicting;

			if (esi.m_fItalic != m_esiSel.m_fItalic)
				m_esiSel.m_fItalic = FwStyledText::knConflicting;
			if (esiI.m_fItalic != m_esiSelI.m_fItalic)
				m_esiSelI.m_fItalic = FwStyledText::knConflicting;
			if (chrpi.xItalic != m_chrpi.xItalic)
				m_chrpi.xItalic = kxConflicting;

			if (esi.m_mpOffset != m_esiSel.m_mpOffset)
				m_esiSel.m_mpOffset = FwStyledText::knConflicting;
			if (esiI.m_mpOffset != m_esiSelI.m_mpOffset)
				m_esiSelI.m_mpOffset = FwStyledText::knConflicting;
			if (chrpi.xOffset != m_chrpi.xOffset)
				m_chrpi.xOffset = kxConflicting;

			if (esi.m_mpSize != m_esiSel.m_mpSize)
				m_esiSel.m_mpSize = FwStyledText::knConflicting;
			if (esiI.m_mpSize != m_esiSelI.m_mpSize)
				m_esiSelI.m_mpSize = FwStyledText::knConflicting;
			if (chrpi.xSize != m_chrpi.xSize)
				m_chrpi.xSize = kxConflicting;

			if (esi.m_ssv != m_esiSel.m_ssv)
				m_esiSel.m_ssv = FwStyledText::knConflicting;
			if (esiI.m_ssv != m_esiSelI.m_ssv)
				m_esiSelI.m_ssv = FwStyledText::knConflicting;
			if (chrpi.xSs != m_chrpi.xSs)
				m_chrpi.xSs = kxConflicting;
		}
		else
		{
			// First time. Everything is set to values for this item.
			m_esiSel = m_vesi[iesi];
			m_esiSelI = m_vesiI[iesi];
			m_chrpi = m_vchrpi[iesi];
			fFoundFirstSel = true;
		}
	}
	// Now we've determined which ws it is, use that and information about whether the
	// style can inherit to decide whether this ws can inherit (yes unless it's
	// really <default properties> of Normal).
	SetCanWsInherit();

	// We use the "old" values at this point for resetting controls if the user types
	// something out of range,
	//- and for determining whether conflicting is a valid state.
	m_esiOld = m_esiSel;
	// Now we just set the items based on values in m_esiSel.

	// Fill the current font item.
	hwndT = ::GetDlgItem(m_hwnd, kctidFfdFont);
	StrApp strName = GetActualPropValue(m_esiSel.m_stuFontFamily, m_chrpi.xFont,
		m_vchrpi[0].xFont, m_vesi[0].m_stuFontFamily, m_esiSelI.m_stuFontFamily);

	// Try to select that item. This generates a spurious message telling us the combo
	// changed, which sets m_fFConflict false, so preserve its value.
	bool fSaveConflict = m_fFfConflict;
	bool fSaveConflictI = m_fFfConflictI;
	if (::SendMessage(hwndT, CB_SELECTSTRING, (WPARAM)-1, (LPARAM)strName.Chars()) == CB_ERR)
	{
		// If we can't, typically because it is the empty string, make a null selection
		::SendMessage(hwndT, CB_SETCURSEL, (WPARAM)-1, 0);
	}
	m_fFfConflict = fSaveConflict;
	m_fFfConflictI = fSaveConflictI;

	// Initialize the Size selection combo box.
	hwndT = ::GetDlgItem(m_hwnd, kctidFfdSize);
	::SendMessage(hwndT, CB_RESETCONTENT, 0, 0);

	m_idyptMinSize = 0; //TE-1845: (m_esiSel.m_mpSize == FwStyledText::knConflicting) ? 0 : 1;
	Assert(kstidFfdUnspecified > 120); // So the test below will see it as an rid.
	for (iv = m_idyptMinSize; iv < SizeOfArray(g_rgdyptSize); ++iv)
	{
		int dypt = g_rgdyptSize[iv];
		if (dypt > g_rgdyptSize[SizeOfArray(g_rgdyptSize) - 1]) // Largest font size listed.
			strb.Load(dypt); //TE-1845: strb.Clear()
		else
			strb.Format(_T("%d"), dypt);
		::SendMessage(hwndT, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	}

	SetSize(GetActualPropValue(m_esiSel.m_mpSize, m_chrpi.xSize,
		m_vchrpi[0].xSize, m_vesi[0].m_mpSize, m_esiSelI.m_mpSize));

	// Fill the underline type list box.
	InitUnderlineCtl();
	SetUnder(GetActualPropValue(m_esiSel.m_unt, m_chrpi.xUnderT,
		m_vchrpi[0].xUnderT, m_vesi[0].m_unt, m_esiSelI.m_unt));

	// Fill one or other (or neither) of the superscript/subscript boxes.
	int ttvOn = kttvForceOn; // Set Check won't take a const.
	int ttvConf = FwStyledText::knConflicting;
	int ttvOff = kttvOff;
	int ssvTmp = m_esiSel.m_ssv;
	switch (ssvTmp)
	{
	case kssvSuper:
		SetCheck(kctidFfdSuper, ttvOn);
		SetCheck(kctidFfdSub, ttvOff);
		break;
	case kssvSub:
		SetCheck(kctidFfdSub, ttvOn);
		SetCheck(kctidFfdSuper, ttvOff);
		break;
	case FwStyledText::knUnspecified:
	case FwStyledText::knConflicting:
		SetCheck(kctidFfdSuper, ttvConf);
		SetCheck(kctidFfdSub, ttvConf);
		break;
	default:
		SetCheck(kctidFfdSuper, ttvOff);
		SetCheck(kctidFfdSub, ttvOff);
		break;
	}

	// Fill the vertical offset list box and spin control.
	InitOffsetCtl();

	UDACCEL udAccel;
	udAccel.nSec = 0;
	udAccel.nInc = kSpnStpPt;

	hwndT = ::GetDlgItem(m_hwnd, kctidFfdOffsetSpin);
	::SendMessage(hwndT, UDM_SETACCEL, 1, (long)&udAccel);
	::SendMessage(hwndT, UDM_SETRANGE32, (uint) kdyptMinOffset, (int) kdyptMaxOffset);

	// Since the offset is checked to be within the bounds of the range of the spin control,
	// the spin control needs to be initialized before setting the offset.
	// (And, since SetOffset can correct an out-of-bounds value, it takes a reference
	// to the offset.)
	int offset = GetActualPropValue(m_esiSel.m_mpOffset, m_chrpi.xOffset,
		m_vchrpi[0].xOffset, m_vesi[0].m_mpOffset, m_esiSelI.m_mpOffset);
	SetOffset(offset);

	// Initialize the bold and italic check boxes.
	SetCheck(kctidFfdBold, m_esiSel.m_fBold);
	SetCheck(kctidFfdItalic, m_esiSel.m_fItalic);

	// Set the colors.
	m_qccmbF->SetColor(GetActualPropValue(m_esiSel.m_clrFore, m_chrpi.xFore,
		m_vchrpi[0].xFore, m_vesi[0].m_clrFore, m_esiSelI.m_clrFore));
	m_qccmbB->SetColor(GetActualPropValue(m_esiSel.m_clrBack, m_chrpi.xBack,
		m_vchrpi[0].xBack, m_vesi[0].m_clrBack, m_esiSelI.m_clrBack));
	m_qccmbU->SetColor(GetActualPropValue(m_esiSel.m_clrUnder, m_chrpi.xUnder,
		m_vchrpi[0].xUnder, m_vesi[0].m_clrUnder, m_esiSelI.m_clrUnder));

	UpdateFontFeatState();

} // AfStyleFntDlg::FillCtls.

/*----------------------------------------------------------------------------------------------
	Update the data that is used for the Font Features button.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::SetFeatureEngine(int iws, StrUni stuFaceName)
{
	if (m_vesi[iws].m_ws == 0)
		return false; // Can't get a feature engine for default settings.
	IRenderEnginePtr qreneng;
	LgCharRenderProps chrpTmp;
	if (stuFaceName.Length())
		wcscpy_s(chrpTmp.szFaceName, stuFaceName); // New value in the process of being set.
	else
		wcscpy_s(chrpTmp.szFaceName, m_vesi[iws].m_stuFontFamily.Chars());
	chrpTmp.ttvBold = kttvOff;
	chrpTmp.ttvItalic = kttvOff;
	chrpTmp.ws = m_vesi[iws].m_ws;
	CheckHr(m_qwsf->get_RendererFromChrp(&chrpTmp, &qreneng));
	Assert(qreneng);
	// Don't CheckHr; may legitimately fail.
	return (qreneng->QueryInterface(IID_IRenderingFeatures, (void **)&m_qrenfeat) == S_OK);
}

/*----------------------------------------------------------------------------------------------
	Update the data that is used for the Font Features button.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::UpdateFontFeatState()
{
	if (!m_fFeatures)
		return;
	m_iwsSel = -1;
	for (int iws = 0; iws < m_vesi.Size(); iws++)
	{
		if (m_vesi[iws].m_fSelected)
		{
			m_iwsSel = iws;
			break;
		}
	}

	m_qrenfeat = NULL;
	// Disable if (pathologically) there is nothing selected; if the things selected have
	// different fonts; or if the font is <default font> or <default heading font> since this
	// could produce unpredictable results if the user later changes the default font for one of
	// the writing systems.  Note also that if the font face is inherited, this is stored in
	// m_esiSelI, which we ignore, so the button will be disabled.
	if (m_iwsSel > -1 && m_esiSel.m_stuFontFamily.Length() != 0 &&
		m_esiSel.m_stuFontFamily[0] != L'<')
	{
		// We have a selection and the same font is used for all selected
		// writing systems, so we can consider font features.
		AssertPtr(m_qwsf);
		SetFeatureEngine(m_iwsSel, m_esiSel.m_stuFontFamily);
	}
	EnableFontFeatures();
}

/*----------------------------------------------------------------------------------------------
	Set the enabling for the Font Features button. It is disabled if more than one writing system
	is selected, or if the selected writing system has no features.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::EnableFontFeatures()
{
	bool fEnable = false;
	if (m_fFeatures && m_iwsSel > -1 && m_qrenfeat)
		fEnable = FmtFntDlg::HasFeatures(m_qrenfeat);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFfdFeatures), fEnable);

	// This control is now always enabled.
	bool fEnableFont = true; // was:(m_qrenfeat.Ptr() == NULL);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFfdFont), fEnableFont);
}

/*----------------------------------------------------------------------------------------------
	Select the given string in the TssCombo.
----------------------------------------------------------------------------------------------*/
int AfStyleFntDlg::SelectStringInTssCombo(HWND hwndCombo, StrAnsi sta)
{
	StrUni stu = sta;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	ITsStringPtr qtss;
	qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_pafsd->GetUserWs(), &qtss);
	return ::SendMessage(hwndCombo, FW_CB_SELECTSTRING, 0, (LPARAM)qtss.Ptr());
}

/*----------------------------------------------------------------------------------------------
	Get the current text from the TssCombo.
----------------------------------------------------------------------------------------------*/
int AfStyleFntDlg::GetTextInTssCombo(HWND hwndCombo, StrApp * pstr)
{
	ITsString * ptss = NULL;
	int nRet = ::SendMessage(hwndCombo, FW_CB_GETTEXT, 0, (LPARAM)&ptss);
	SmartBstr sbstr;
	ptss->get_Text(&sbstr);
	StrUni stu(sbstr.Chars());
	pstr->Assign(stu);
	ptss->Release();
	return nRet;
}

/*----------------------------------------------------------------------------------------------
	Get the text of the given item in the TssCombo.
----------------------------------------------------------------------------------------------*/
int AfStyleFntDlg::GetItemTextInTssCombo(HWND hwndCombo, int iitem, StrApp * pstr)
{
	ITsString * ptss = NULL;
	int nRet = ::SendMessage(hwndCombo, FW_CB_GETLBTEXT, iitem, (LPARAM)&ptss);
	SmartBstr sbstr;
	ptss->get_Text(&sbstr);
	StrUni stu(sbstr.Chars());
	pstr->Assign(stu);
	ptss->Release();
	return nRet;
}

/*----------------------------------------------------------------------------------------------
	Used for first initialization and also for resetting things when m_fCanInherit changes,
	or when writing system changes. (Strictly only needs to be called when ws changes between
	zero and something else, but we don't make that optimization.)
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::InitFontCtl()
{
	LOGFONT lf;
	HRESULT hr;

	HWND hwndFont = ::GetDlgItem(m_hwnd, kctidFfdFont);
	::SendMessage(hwndFont, CB_RESETCONTENT, 0, 0);

	StrUni stuName;
	static long ipszList = 0; // Returned value from SendMessage.

	ClearItems(&lf, 1);
	lf.lfCharSet = DEFAULT_CHARSET;

	// Add the four predefined names to the combo box (unspecified only if inheritance is allowed).
	if (m_fCanInherit)
	{
		stuName.Load(kstidUnspec);
		ipszList = ::SendMessage((HWND)hwndFont, CB_ADDSTRING, 0, (LPARAM)stuName.Chars());
	}
	Vector<StrUni> vstuDefaults;
	FwStyledText::FontUiStrings(m_f1DefaultFont, vstuDefaults);
	for (int istu = 0; istu < vstuDefaults.Size(); istu++)
	{
		ipszList = ::SendMessage((HWND)hwndFont, CB_ADDSTRING, 0, (LPARAM)vstuDefaults[istu].Chars());
	}

	// Add real font names only if NOT <default properties>.
	if (m_esiSel.m_ws != 0)
	{
		// Get the currently available fonts via the LgFontManager.
		ILgFontManagerPtr qfm;
		SmartBstr bstrNames;

		qfm.CreateInstance(CLSID_LgFontManager);
		hr = qfm->AvailableFonts(&bstrNames);

		// TODO: just use 16-bit data.
		StrApp strNameList;
		strNameList.Assign(bstrNames.Bstr(), BstrLen(bstrNames.Bstr()));
		int cchLength = strNameList.Length();
		StrApp strName; // Individual font name.
		int ichMin = 0; // Index of the beginning of a font name.
		int ichLim = 0; // Index that is one past the end of a font name.

		// Add each font name to the combo box.
		while (ichLim < cchLength)
		{
			ichLim = strNameList.FindCh(L',', ichMin);
			if (ichLim == -1) // i.e., if not found.
			{
				ichLim = cchLength;
			}

			strName.Assign(strNameList.Chars() + ichMin, ichLim - ichMin);
			ipszList = ::SendMessage((HWND)hwndFont, CB_ADDSTRING, 0, (LPARAM)strName.Chars());
			// REVIEW JohnT(LarryW): should we check potential error from ipszList?

			ichMin = ichLim + 1;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Used for first initialization and also for resetting things when m_fCanInherit changes.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::InitUnderlineCtl()
{
	// Fill the underline type combo-box.
	HWND hwndT = ::GetDlgItem(m_hwnd, kctidFfdUnder);
	::SendMessage(hwndT, CB_RESETCONTENT, 0, 0);

	m_istidMinUnder = (m_fCanInherit) ? 0 : 1;
	for (int iv = m_istidMinUnder; iv < SizeOfArray(g_rgstidUnder); iv++)
	{
		::SendMessage(hwndT, CB_ADDSTRING, 0, (LPARAM)g_rgstidUnder[iv]);
	}
}

void AfStyleFntDlg::InitOffsetCtl()
{
	// Fill the offset combo-box.
	HWND hwndT = ::GetDlgItem(m_hwnd, kctidFfdOffset);
	::SendMessage(hwndT, CB_RESETCONTENT, 0, 0);

	StrAppBuf strb;
	m_istidMinOffset = (m_fCanInherit) ? 0 : 1;
	for (int iv = m_istidMinOffset; iv < SizeOfArray(g_rgstidOffset); iv++)
	{
		strb.Load(g_rgstidOffset[iv]);
		::SendMessage(hwndT, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
	}
}

/*----------------------------------------------------------------------------------------------
	The controls manipulate the values in m_esiSel. When closing, or when the selection
	changes, we need to copy those values back to the selected rows of the ws vector.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::ReadCtls()
{
	int dypt;
	StrAppBuf strb;

	// Get the text from the edit box and convert it to a number.
	int cch = ::SendDlgItemMessage(m_hwnd, kctidFfdSize, WM_GETTEXT, strb.kcchMaxStr,
		(LPARAM)strb.Chars());
	strb.SetLength(cch);

	if (cch == 0 || m_chrpi.xSize == kxInherited)
	{
		if (m_esiSel.m_mpSize != FwStyledText::knConflicting)
			m_esiSel.m_mpSize = FwStyledText::knUnspecified;
	}
	else
	{
		dypt = StrUtil::ParseInt(strb.Chars());

		if (m_chrpi.xSize == kxExplicit)
		{
			if (IsValidFontSize(dypt))
			{
				m_esiSel.m_mpSize = dypt * 1000;
			}
			else
			{
				m_esiSel.m_mpSize = m_esiOld.m_mpSize;
			}
		}
	}

	IRenderingFeaturesPtr qrenengSave = m_qrenfeat;

	Assert(m_vesi.Size() == m_vchrpi.Size());
	for (int iesi = 0; iesi < m_vesi.Size(); iesi++)
	{
		// Any value that is not conflicting should be copied back to the selected
		// writing system's style info. This either makes no change (if the user did
		// nothing to this control) or sets it to the value the user requested.
		WsStyleInfo & esi = m_vesi[iesi];
		ChrpInheritance & chrpi = m_vchrpi[iesi];
		// Ignore items that are not selected. Note that we use the flag in the
		// record, not the actual state of the control, because this method may be
		// called because the state of the control changed; and we want to set the
		// values based on what USED to be selected.
		if (!esi.m_fSelected)
			continue;

		if ((!m_fFfConflict) && esi.m_stuFontFamily != m_esiSel.m_stuFontFamily)
		{
			m_fDirty = true;
			esi.m_stuFontFamily = m_esiSel.m_stuFontFamily;
		}
		m_qrenfeat = NULL;
		SetFeatureEngine(iesi, L"");
		if (m_qrenfeat)
		{
			Assert(m_vesi.Size() == m_vfl.Size());
			StrUni stuFvarNew = FmtFntDlg::GenerateFeatureString(m_qrenfeat, m_vfl[iesi].rgn);
			if (stuFvarNew != m_vesi[iesi].m_stuFontVar)
			{
				m_vesi[iesi].m_stuFontVar = stuFvarNew;
				m_fDirty = true;
			}
		}
		ReadCtl(m_esiSel.m_clrBack, esi.m_clrBack, m_chrpi.xBack);
		ReadCtl(m_esiSel.m_clrFore, esi.m_clrFore, m_chrpi.xFore);
		ReadCtl(m_esiSel.m_clrUnder, esi.m_clrUnder, m_chrpi.xUnder);
		ReadCtl(m_esiSel.m_fBold, esi.m_fBold, m_chrpi.xBold);
		ReadCtl(m_esiSel.m_fItalic, esi.m_fItalic, m_chrpi.xItalic);
		ReadCtl(m_esiSel.m_unt, esi.m_unt, m_chrpi.xUnderT);
		ReadCtl(m_esiSel.m_mpOffset, esi.m_mpOffset, m_chrpi.xOffset);
		ReadCtl(m_esiSel.m_mpSize, esi.m_mpSize, m_chrpi.xSize);
		ReadCtl(m_esiSel.m_ssv, esi.m_ssv, m_chrpi.xSs);

		ReadInheritance(m_chrpi.xFont, chrpi.xFont);
		ReadInheritance(m_chrpi.xFore, chrpi.xFore);
		ReadInheritance(m_chrpi.xBack, chrpi.xBack);
		ReadInheritance(m_chrpi.xUnder, chrpi.xUnder);
		ReadInheritance(m_chrpi.xUnderT, chrpi.xUnderT);
		ReadInheritance(m_chrpi.xSize, chrpi.xSize);
		ReadInheritance(m_chrpi.xBold, chrpi.xBold);
		ReadInheritance(m_chrpi.xItalic, chrpi.xItalic);
		ReadInheritance(m_chrpi.xOffset, chrpi.xOffset);
		ReadInheritance(m_chrpi.xSs, chrpi.xSs);
		ReadInheritance(m_chrpi.xFontVar, chrpi.xFontVar);
	}
	m_qrenfeat = qrenengSave;

} // AfStyleFntDlg::ReadCtls().


void AfStyleFntDlg::ReadCtl(int nNew, int & nReal, int x)
{
	if (x == kxInherited && FwStyledText::knUnspecified != nReal)
	{
		nReal = FwStyledText::knUnspecified;
		m_fDirty = true;
	}
	else if (x == kxExplicit && (nNew != FwStyledText::knConflicting && nNew != nReal))
	{
		nReal = nNew;
		m_fDirty = true;
	}
}

void AfStyleFntDlg::ReadCtl(COLORREF nNew, COLORREF & nReal, int x)
{
	if (x == kxInherited && (COLORREF)FwStyledText::knUnspecified != nReal)
	{
		nReal = (COLORREF)FwStyledText::knUnspecified; // when setting a value back to inherited
		m_fDirty = true;
	}
	else if (x == kxExplicit && (nNew != FwStyledText::knConflicting && nNew != nReal))
	{
		nReal = nNew;
		m_fDirty = true;
	}
}

void AfStyleFntDlg::ReadInheritance(int xNew, int & xReal)
{
	if (xNew != kxConflicting)
		xReal = xNew;
}

/*----------------------------------------------------------------------------------------------
	Set the font size value.
	ENHANCE JohnT: This and many other routines in AfStyleFntDlg are very similar to
	ones in FmtFntDlg. Look for ways to merge the two.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::SetSize(int dymp)
{
	StrAppBuf strb;

	switch (dymp)
	{
	case FwStyledText::knConflicting:
		::SendDlgItemMessage(m_hwnd, kctidFfdSize, CB_SETCURSEL, 0, 0);
		break;
	case FwStyledText::knUnspecified:
		::SendDlgItemMessage(m_hwnd, kctidFfdSize, WM_SETTEXT, 0, (LPARAM)_T(""));
		break;
	default:
		strb.Format(_T("%d"), dymp / 1000);
		::SendDlgItemMessage(m_hwnd, kctidFfdSize, WM_SETTEXT, 0, (LPARAM)strb.Chars());
		break;
	}

//	char rgch[20];
//	itoa(dymp / 1000, rgch, 10);
//	StrApp strName(rgch);
//	if (::SendMessage(::GetDlgItem(m_hwnd, kctidFfdSize), CB_SELECTSTRING, 0,
//		(LPARAM)strName.Chars()) == CB_ERR)
//	{
//		int x;
//		x = 3;
//	}
}

/*----------------------------------------------------------------------------------------------
	Set a toggle check mark. If fAdvance is true, advance the check box to its next state
	(for when clicked). If fAllowConflict is true, the grayed state is allowed.
	Ttv is modified to one of kttvInvert, kttvOff, or (if fAllocConflict is true) FwStyledText::knConflicting.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::SetCheck(int ctid, int & ttv, bool fAdvance, bool fAllowConflict)
{
	int bst;

	if (fAdvance)
	{
		switch (ttv)
		{
		case FwStyledText::knConflicting:
		case FwStyledText::knUnspecified:
			ttv = kttvInvert;
			break;
		case kttvForceOn:
		case kttvInvert:
			ttv = kttvOff;
			break;
		case kttvOff:
		default:
			if (fAllowConflict)
				ttv = FwStyledText::knConflicting;
			else
				ttv = kttvInvert;
			break;
		}
	}

	switch (ttv)
	{
	case FwStyledText::knConflicting:
	case FwStyledText::knUnspecified:
		bst = BST_INDETERMINATE;
		break;
	case kttvForceOn:
	case kttvInvert:
		bst = BST_CHECKED;
		break;
	case kttvOff:
	default:
		bst = BST_UNCHECKED;
		break;
	}
	::SendDlgItemMessage(m_hwnd, ctid, BM_SETCHECK, bst, 0);
} // AfStyleFntDlg::SetCheck.

/*----------------------------------------------------------------------------------------------
	Set the underline combo box value.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::SetUnder(int unt)
{
	int iv;

	for (iv = 0; ; iv++)
	{
		if (iv >= SizeOfArray(g_rguntUnder))
		{
			iv = -1; // makes it blank
			break;
		}
		if (unt == g_rguntUnder[iv])
			break;
	}

	iv -= m_istidMinUnder;

	// Prevent blank display. Instead show "(unspecified)".
	if (iv < 0)
		iv = 0;

	::SendDlgItemMessage(m_hwnd, kctidFfdUnder, CB_SETCURSEL, iv, 0);
}

/*----------------------------------------------------------------------------------------------
	Set the vertical offset control values.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::SetOffset(int & nValue)
{
	// Adjusting the spin control has the side effect of setting the explicit values.
	// So save them and restore them.
	int mpOffsetSave = m_esiSel.m_mpOffset;
	int xOffsetSave = m_chrpi.xOffset;

	bool fSpecial = (nValue == 0 || nValue == FwStyledText::knConflicting ||
		nValue == FwStyledText::knUnspecified);

	StrAppBuf strb;
	strb.SetLength(strb.kcchMaxStr);
	int icombo; // Used to select an item in the combo box.
	int nValShow = Abs(nValue); // used to set the edit box value
	if (fSpecial)
	{
		nValShow = 0;
		if (nValue == FwStyledText::knUnspecified)
			icombo = 0;
		else if (nValue == 0) // None
			icombo = 1;
		else
			icombo = -1;
	}
	else
	{
		// Make sure it is in range.
		int nRangeMin = 0;
		int nRangeMax = 0;
		::SendDlgItemMessage(m_hwnd, kctidFfdOffsetSpin, UDM_GETRANGE32, (long)&nRangeMin,
			(long)&nRangeMax);
		nValShow = NBound(nValShow, nRangeMin, nRangeMax);
		nValue = (nValue < 0) ? -nValShow : nValShow; // copy the adjustment back

		if (nValue > 0)
			icombo = 2;
		else
			icombo = 3;
	}
	//if (m_istidMinOffset)
	//	icombo = max (icombo - m_istidMinOffset, 0); // just in case conflicting gets set

	// Update the edit box.
	if (nValue != FwStyledText::knConflicting && nValue != FwStyledText::knUnspecified)
		AfUtil::MakeMsrStr (nValShow , knpt, &strb);
	// else leave blank
	::SendDlgItemMessage(m_hwnd, kctidFfdOffsetNum, WM_SETTEXT, 0, (LPARAM)strb.Chars());

	// Update the combo box.
	::SendDlgItemMessage(m_hwnd, kctidFfdOffset, CB_SETCURSEL, icombo - m_istidMinOffset, 0);

	m_esiSel.m_mpOffset = mpOffsetSave;
	m_chrpi.xOffset = xOffsetSave;

	::EnableWindow(::GetDlgItem(m_hwnd, kctidFfdOffsetSpin), (m_chrpi.xOffset == kxExplicit));
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFfdOffsetNum), (m_chrpi.xOffset == kxExplicit));

} // AfStyleFntDlg::SetOffset.


/*----------------------------------------------------------------------------------------------
	Handle a popup menu command when we're called from inside a DLL, meaning that there's no
	AfApp to handle command processing up the normal chain.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::OnCommand(int cid, int nc, HWND hctl)
{
	bool fOk = SuperClass::OnCommand(cid, nc, hctl);
	if (!fOk && cid >= kcidMenuItemDynMin && cid < kcidMenuItemDynLim && m_hmenuFeatures != 0)
	{
		Cmd cmd(cid, NULL,
			AfMenuMgr::kmaDoCommand,
			(int)m_hmenuFeatures,
			cid - kcidMenuItemDynMin,
			0);
		AfApp::GetMenuMgr(&m_pmum)->GetLastExpMenuInfo((HMENU *)&cmd.m_rgn[1], &cmd.m_cid);
		CmdFeaturesPopup(&cmd);
	}
	return fOk;
}


/*----------------------------------------------------------------------------------------------
	A combo box changed. Handle it.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::OnComboChange(NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (pnmh->idFrom == kctidFfdForeClr)
	{
		// If we set them here, the description updates continually as we move through the
		// dialog. See the CBN_SELENDOK branch of AfStyleFntDlg::OnNotifyChild().
		//m_esiSel.m_clrFore = m_clrFore;
		return SuperClass::OnNotifyChild(pnmh->idFrom, pnmh, lnRet);
	}
	if (pnmh->idFrom == kctidFfdBackClr)
	{
		//m_esiSel.m_clrBack = m_clrBack;
		return SuperClass::OnNotifyChild(pnmh->idFrom, pnmh, lnRet);
	}
	if (pnmh->idFrom == kctidFfdUnderClr)
	{
		return SuperClass::OnNotifyChild(pnmh->idFrom, pnmh, lnRet);
	}

	// Get the current index from the combo box.
	int iv = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);

	switch (pnmh->idFrom)
	{
	case kctidFfdUnder:
		iv += m_istidMinUnder;
		if (iv < 0 || iv >= SizeOfArray(g_rguntUnder))
			m_esiSel.m_unt = m_esiOld.m_unt;
		else
		{
			if (g_rguntUnder[iv] == FwStyledText::knConflicting)
			{
				if (m_fCanInherit)
				{
					m_esiSel.m_unt = FwStyledText::knUnspecified;
					m_chrpi.xUnderT = kxInherited;
					// Do later (UpdateComboWithInherited):
					//SetUnder(m_esiSelI.m_unt);
				}
				else
				{
					m_esiSel.m_unt = kuntNone;
					m_chrpi.xUnderT = kxExplicit;
					SetUnder(m_esiSel.m_unt);
				}

			}
			else
			{
				m_esiSel.m_unt = g_rguntUnder[iv];
				m_chrpi.xUnderT = kxExplicit;
			}
		}
		return true;

	case kctidFfdSize:
		iv += m_idyptMinSize;
		if (iv < 0 || iv >= SizeOfArray(g_rgdyptSize))
			m_esiSel.m_mpSize = m_esiOld.m_mpSize;
		else
		{
			int dypt = g_rgdyptSize[iv];
			if (dypt == kstidFfdUnspecified)
			{
				if (m_fCanInherit)
				{
					m_esiSel.m_mpSize = FwStyledText::knUnspecified;
					m_chrpi.xSize = kxInherited;
				}
				else
				{
					m_esiSel.m_mpSize = kmpDefaultSize;
					m_chrpi.xSize = kxExplicit;
				}
			}
			else
			{
				m_esiSel.m_mpSize = dypt * 1000;
				m_chrpi.xSize = kxExplicit;
			}
		}
		// This is vital. OnComboChange is called before the window text is updated.
		// Immediately after it, we call UpdateDescrips, which reads the window text.
		// If we don't fix it here, UpdateDescrips reads the wrong value and sets
		// everything back to the old size.
		(m_chrpi.xSize == kxInherited) ?
			SetSize(m_esiSelI.m_mpSize) : // ENHANCE: do this after the combo loses focus
			SetSize(m_esiSel.m_mpSize);
		return true;

	case kctidFfdOffset:
		iv += m_istidMinOffset;
		if (iv < 0 || iv >= SizeOfArray(g_rgdympOffset))
			m_esiSel.m_mpOffset = m_esiOld.m_mpOffset;
		else
		{
			// There are four possible combo box values: FwStyledText::knConflicting, 0, 3000, -3000,
			// meaning conflicting, none, raised, lowered.
			// These are represented by m_esiSel.m_mpOffset being FwStyledText::knConflicting, 0, positive,
			// or negative. If m_mpOffset is not zero or conflicting, it should keep its
			// absolute value.
			int dymp = g_rgdympOffset[iv];
			m_chrpi.xOffset = kxExplicit;
			if (dymp == FwStyledText::knConflicting)
			{
				m_esiSel.m_mpOffset = FwStyledText::knUnspecified;
				m_chrpi.xOffset = kxInherited;
			}
			else if (dymp == 0)
			{
				m_esiSel.m_mpOffset = dymp; // 0
			}
			// otherwise, if it used to be conflicting or zero, set to the  value
			// from m_rgdympOffset, which is arranged to be one spin click in the
			// appropriate direction.
			else if (m_esiSel.m_mpOffset == FwStyledText::knConflicting ||
				m_esiSel.m_mpOffset == 0 ||
				m_esiSel.m_mpOffset == FwStyledText::knUnspecified)
			{
				m_esiSel.m_mpOffset = dymp;
			}
			// Otherwise, if the user has changed from raised to lowered or vice versa,
			// reverse the sign of the offset without changing its magnitude.
			else if ((m_esiSel.m_mpOffset < 0) != (dymp < 0))
			{
				m_esiSel.m_mpOffset = -m_esiSel.m_mpOffset;
			}
			// Otherwise it is not changing at all.
			else
				return true;
		}

		SetOffset(m_esiSel.m_mpOffset); // don't show inherited value until later
		return true;

	case kctidFfdFont:
		{ // BLOCK
			if (iv || !m_fCanInherit)
			{
				// (unspecified) was not selected
				// This message is sent BEFORE the automatic update of the text box, so we
				// can't just read its contents. Instead, read the current item.
				StrApp str;
				StrApp str2;
				TCHAR fontName[300];

				HWND hwndFont = ::GetDlgItem(m_hwnd, kctidFfdFont);
				::GetWindowText(hwndFont, fontName, sizeof(fontName));
				m_esiSel.m_stuFontFamily = fontName;
				m_chrpi.xFont = kxExplicit;
			}
			else
			{
				// (unspecified) was selected
				StrApp strName;
				if (m_fCanInherit)
				{
					m_esiSel.m_stuFontFamily = "";
					m_chrpi.xFont = kxInherited;
					strName = m_esiSelI.m_stuFontFamily;
					// Update the combo box later (UpdateComboWithInherited)
				}
				else
				{
					Assert(false);
					m_esiSel.m_stuFontFamily = FwStyledText::FontDefaultUi(m_f1DefaultFont);
					m_chrpi.xFont = kxExplicit;
					strName = m_esiSel.m_stuFontFamily;
					// Update the combo box right away.
					HWND hwndT = ::GetDlgItem(m_hwnd, kctidFfdFont);
					bool fSaveConflictI = m_fFfConflictI;
					// Try to select that item. This generates a spurious message telling us
					// the combo changed, which sets m_fFConflict false, so preserve its value.
					if (::SendMessage(hwndT, CB_SETCURSEL, iv, 0) == CB_ERR)
					{
						// If we can't, typically because it is the empty string,
						// make a null selection.
						::SendMessage(hwndT, CB_SETCURSEL, (WPARAM)-1, 0);
					}
					m_fFfConflictI = fSaveConflictI;
				}
			}
			m_fFfConflict = false;  // No more conflict once the user set it.
			SetFontVarSettings(true);
			UpdateFontFeatState();
		}
		return true;
	}

	return false;
} // AfStyleFntDlg::OnComboChange.

/*----------------------------------------------------------------------------------------------
	The text field of a combo was edited.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::OnComboUpdate(NMHDR * pnmh, long & lnRet)
{
	StrAppBuf strb;
	HWND hwndT;

	switch (pnmh->idFrom)
	{
	case kctidFfdFont:
		{ // BLOCK
			// This code is currently not used.
			// TODO: convert way to getting text below into using FW_CB_GETTEXT and putting
			// it in an ITsString.
			Assert(false);
			StrAppBuf strb;
			hwndT = ::GetDlgItem(m_hwnd, kctidFfdFont);
			int cch = ::SendDlgItemMessage(m_hwnd, kctidFfdFont, WM_GETTEXT, strb.kcchMaxStr,
				(LPARAM)strb.Chars());
			strb.SetLength(cch);
			if (cch == 0 && m_fCanInherit)
			{
				// Return to the inherited value.
				m_esiSel.m_stuFontFamily = L"";
				m_fFfConflict = true;
				m_chrpi.xFont = kxInherited;
				StrApp strName = m_esiSelI.m_stuFontFamily;
				// Try to select that item. This generates a spurious message telling us the combo
				// changed, which sets m_fFConflict false, so preserve its value.
				bool fSaveConflict = m_fFfConflict;
				bool fSaveConflictI = m_fFfConflictI;
				if (SelectStringInTssCombo(hwndT, strName) == CB_ERR)
				{
					// If we can't, typically because it is the empty string, make a null selection.
					::SendMessage(hwndT, CB_SETCURSEL, (WPARAM)-1, 0);
				}
				m_fFfConflict = fSaveConflict;
				m_fFfConflictI = fSaveConflictI;
			}
			else if (cch == 0 && !m_fCanInherit)
			{
				m_esiSel.m_stuFontFamily = FwStyledText::FontDefaultUi(m_f1DefaultFont);
				m_fFfConflict = false;
			}
			else
			{
				m_esiSel.m_stuFontFamily = strb.Chars();
				m_fFfConflict = false;  // No more conflict once the user set it.
			}
			// Review JohnT: do we need to do something about scrolling the list?
		}
		return true;
	case kctidFfdSize:
		{ // BLOCK
			StrAppBuf strb;
			int cch = ::SendDlgItemMessage(m_hwnd, kctidFfdSize, WM_GETTEXT, strb.kcchMaxStr,
				(LPARAM)strb.Chars());
			strb.SetLength(cch);

			if (cch == 0 && m_fCanInherit)
			{
				m_esiSel.m_mpSize = FwStyledText::knUnspecified;
				m_chrpi.xSize = kxInherited;
				SetSize(m_esiSelI.m_mpSize);
				UpdateDescrips();
			}
			else if (cch == 0 && !m_fCanInherit)
			{
				m_esiSel.m_mpSize = kmpDefaultSize;
				SetSize(m_esiSel.m_mpSize);
				UpdateDescrips();
			}
			else
			{
				int dypt = StrUtil::ParseInt(strb.Chars());

				if (IsValidFontSize(dypt))
				{
					m_esiSel.m_mpSize = dypt * 1000;
					m_chrpi.xSize = kxExplicit;
					UpdateDescrips();
				}
			}
			// If it is not yet a valid size, don't update the description
		}
	}

	return false;
} // AfStyleFntDlg::OnComboUpdate.

/*----------------------------------------------------------------------------------------------
	Handles a click on a spin control.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::OnDeltaSpin(NMHDR * pnmh, long & lnRet)
{
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
	}
	else
	{
		// Called from an edit control.
		hwndEdit = pnmh->hwndFrom;
		switch (pnmh->idFrom)
		{
		case kctidFfdOffsetNum:
			hwndSpin = ::GetDlgItem(m_hwnd, kctidFfdOffsetSpin);
			break;
		default:
			Assert(false);
		}
	}

	// Get the text from the edit box and convert it to a number.
	int cch = ::SendMessage(hwndEdit, WM_GETTEXT, strb.kcchMaxStr, (LPARAM)strb.Chars());
	strb.SetLength(cch);
	int nValue;
	int icombo = ::SendDlgItemMessage(m_hwnd, kctidFfdOffset, CB_GETCURSEL, 0, 0) +
		m_istidMinOffset;
	if (cch == 0)
		nValue = 3; // default
	else
		AfUtil::GetStrMsrValue (&strb , knpt, &nValue);
	if (icombo == 3) // lowered
		nValue = - nValue;

	if (pnmh->code == UDN_DELTAPOS && cch > 0)
	{
		// If nValue is not already a whole increment of nDelta, then we only increment it
		// enough to make it a whole increment. If already a whole increment, then we go ahead
		// and increment it the entire amount.
		int nDelta = ((NMUPDOWN *)pnmh)->iDelta;
		int nPartialIncrement = nValue % nDelta;
		if (icombo == 3) // if combo is set to lower
		{
			if (nPartialIncrement && nDelta > 0)
				nValue -= nPartialIncrement;
			else if (nPartialIncrement && nDelta < 0)
				nValue += (nDelta - nPartialIncrement);
			else
				nValue += nDelta;
		}
		else // combo set to raise or none
		{
			if (nPartialIncrement && nDelta > 0)
				nValue += (nDelta - nPartialIncrement);
			else if (nPartialIncrement && nDelta < 0)
				nValue -= nPartialIncrement;
			else
				nValue += nDelta;
		}

	}
	m_esiSel.m_mpOffset = nValue;
	m_chrpi.xOffset = kxExplicit;
	SetOffset(m_esiSel.m_mpOffset);
	UpdateDescrips();

	lnRet = 0;
	return true;
} // AfStyleFntDlg::OnDeltaSpin.

/*----------------------------------------------------------------------------------------------
	Check the value in the size field is valid. If not, complain.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::VerifySize()
{
	// Check to see if this is a nested call, which can occur after a WM_KILLFOCUS message:
	if (m_fFfVerifySizeActive || m_fCancelInProgress)
		// It is a nested call, so pretend everything is OK this time:
		return true;

	// Protect against nested calls:
	m_fFfVerifySizeActive = true;

	// Validate the size
	StrAppBuf strb;
	int cch = ::SendDlgItemMessage(m_hwnd, kctidFfdSize, WM_GETTEXT, strb.kcchMaxStr,
		(LPARAM)strb.Chars());
	strb.SetLength(cch);

	if (cch > 0)
	{
		int dypt = StrUtil::ParseInt(strb.Chars());

		if (dypt > 0 || m_chrpi.xSize != kxInherited)
		{
			if (!IsValidFontSize(dypt))
			{
				// The following modification was not needed, but if you want the control to
				// force the user's value to be within limits, uncomment the following, and
				// comment out the remainder of this block:
				//// Force value to be within range:
				//if (dypt < kdyptMinSize)
				//	dypt = kdyptMinSize;
				//else if (dypt > kdyptMaxSize)
				//	dypt = kdyptMaxSize;

				//// Form text string from number:
				//StrAppBuf strb;
				//strb.Format("%d", dypt);

				//// Write new value into control:
				//SetSize(1000 * dypt);
				//m_esiSel.m_mpSize = 1000 * dypt;
				//if (m_esiSel.m_mpSize != m_esiOld.m_mpSize)
				//	m_fDirty = true;

				m_esiSel.m_mpSize = m_esiOld.m_mpSize;
				StrApp strMessage(kstidFfdRange);
				::MessageBox(m_hwnd, strMessage.Chars(), NULL, 0);
				::SetFocus(::GetDlgItem(m_hwnd, kctidFfdSize));
				m_fFfVerifySizeActive = false;
				return false;
			}
		}
	}
	m_fFfVerifySizeActive = false;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle a notification.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	bool fAllowConflict; // in check boxes

	switch (pnmh->code)
	{
	case CBN_SELCHANGE:
		{
			bool fRes = OnComboChange(pnmh, lnRet);
			UpdateDescrips();
			if (fRes)
				return true;
		}
		break;
// Don't call VerifySize just because the font size box has lost focus. If you do, you'll
// get error messages for invalid font sizes, even when you wanted to cancel the dialog.
//	case CBN_KILLFOCUS:
//		if (pnmh->idFrom == kctidFfdSize)
//			VerifySize();
//		break;

	case CBN_EDITUPDATE:
		{
			bool fRes = OnComboUpdate(pnmh, lnRet);
			UpdateDescrips();
			if (fRes)
				return true;
		}
		break;

	case CBN_CLOSEUP:
		{
			if (pnmh->idFrom == kctidFfdFont)
			{
				// redraw font combo after closing. Otherwise it might not be visible
				// when the drop down list goes over the edit field if the screen
				// resolution is to small (TE-3133).
				HWND hwnd = ::GetDlgItem(m_hwnd, kctidFfdFont);
				::InvalidateRect(hwnd, NULL, true);
				return true;
			}
		}
		break;
	case UDN_DELTAPOS: // Spin control is activated.
	case EN_KILLFOCUS: // Edit control modified.
		return OnDeltaSpin(pnmh, lnRet);

	case BN_CLICKED:
		switch (pnmh->idFrom)
		{
		case kctidFfdItalic:
//			fAllowConflict = (m_esiOld.m_fItalic == FwStyledText::knConflicting || m_fCanInherit);
			fAllowConflict = m_fCanInherit;
			SetCheck(kctidFfdItalic, m_esiSel.m_fItalic, true, fAllowConflict);
			m_chrpi.xItalic = (m_esiSel.m_fItalic == FwStyledText::knConflicting) ? kxInherited : kxExplicit;
			UpdateDescrips();
			return true;
		case kctidFfdBold:
//			fAllowConflict = (m_esiOld.m_fBold == FwStyledText::knConflicting || m_fCanInherit);
			fAllowConflict = m_fCanInherit;
			SetCheck(kctidFfdBold, m_esiSel.m_fBold, true, fAllowConflict);
			m_chrpi.xBold = (m_esiSel.m_fBold == FwStyledText::knConflicting) ? kxInherited : kxExplicit;
			UpdateDescrips();
			return true;
		case kctidFfdSuper:
			{ // BLOCK (for var inits)
				int ttv;
				switch (m_esiSel.m_ssv)
				{
				case FwStyledText::knConflicting:
					ttv = FwStyledText::knConflicting;
					break;
				case kssvSuper:
					ttv = kttvForceOn;
					break;
				default:
					ttv = kttvOff;
					break;
				}
				fAllowConflict = (m_esiOld.m_ssv == FwStyledText::knConflicting || m_fCanInherit);
				SetCheck(kctidFfdSuper, ttv, true, fAllowConflict);
				// Set ssv, and set the value for the complementary checkbox if needed.
				// The possible states are: both off, both conflicting, or one button on.
				// The above call to SetCheck moves it in the order conflicting->Invert->off.
				// If it is now Invert, the other box must be off.
				// If it is now conflicting, the other box must be conflicting.
				// If it is now off, it must previously have been either on or conflicting;
				// in either case, it is safe to ensure the other box is off.
				switch (ttv)
				{
				case kttvInvert:
					m_esiSel.m_ssv = kssvSuper;
					ttv = kttvOff;
					break;
				case kttvOff:
					m_esiSel.m_ssv = kssvOff;
					ttv = kttvOff;
					break;
				case FwStyledText::knConflicting:
					m_esiSel.m_ssv = FwStyledText::knConflicting;
					ttv = FwStyledText::knConflicting;
					break;
				default:
					Assert(false);
					break;
				}
				SetCheck(kctidFfdSub, ttv);

				m_chrpi.xSs = (m_esiSel.m_ssv == FwStyledText::knConflicting) ? kxInherited : kxExplicit;
				UpdateDescrips();
			}
			return true;

		case kctidFfdSub:
			{ // BLOCK
				int ttv;
				switch (m_esiSel.m_ssv)
				{
				case FwStyledText::knConflicting:
					ttv = FwStyledText::knConflicting;
					break;
				case kssvSub:
					ttv = kttvForceOn;
					break;
				default:
					ttv = kttvOff;
					break;
				}
				fAllowConflict = (m_esiOld.m_ssv == FwStyledText::knConflicting || m_fCanInherit);
				SetCheck(kctidFfdSub, ttv, true, fAllowConflict);
				// Set ssv, and set the value for the complementary checkbox if needed.
				// The possible states are: both off, both conflicting, or one button on.
				// The above call to SetCheck moves it in the order conflicting->Invert->off.
				// If it is now Invert, the other box must be off.
				// If it is now conflicting, the other box must be conflicting.
				// If it is now off, it must previously have been either on or conflicting;
				// in either case, it is safe to ensure the other box is off.
				switch (ttv)
				{
				case kttvInvert:
					m_esiSel.m_ssv = kssvSub;
					ttv = kttvOff;
					break;
				case kttvOff:
					m_esiSel.m_ssv = kssvOff;
					ttv = kttvOff;
					break;
				case FwStyledText::knConflicting:
					m_esiSel.m_ssv = FwStyledText::knConflicting;
					ttv = FwStyledText::knConflicting;
					break;
				default:
					Assert(false);
					break;
				}
				SetCheck(kctidFfdSuper, ttv);

				m_chrpi.xSs = (m_esiSel.m_ssv == FwStyledText::knConflicting) ? kxInherited : kxExplicit;
				UpdateDescrips();
			}
			return true;

		case kctidFfdFeatures:
			{	// BLOCK
				if (!m_fFeatures)
				{
					Assert(false);
					return true;
				}
				// Show the popup menu that allows a user to choose the features.
				Rect rc;
				::GetWindowRect(::GetDlgItem(m_hwnd, kctidFfdFeatures), &rc);
				AfApp::GetMenuMgr(&m_pmum)->SetMenuHandler(kctidFfdFeatPopup);
				CreateFeaturesMenu();
				if (m_hmenuFeatures)
					::TrackPopupMenu(m_hmenuFeatures, TPM_LEFTALIGN | TPM_RIGHTBUTTON,
						rc.left, rc.bottom, 0, m_hwnd, NULL);
				return true;
			}
		}
		break;

	case LVN_ITEMCHANGED:
		{
			switch (pnmh->idFrom)
			{
			case kctidAsfdLangList:
				NMLISTVIEW * qnmlv = (LPNMLISTVIEW) pnmh;
//				int iesi = qnmlv->iItem;
				LVITEM lvi;
				lvi.mask = LVIF_PARAM;
				lvi.iItem = qnmlv->iItem;	// Index of item in list.
				lvi.iSubItem = 0;
				HWND hwndList = GetDlgItem(m_hwnd, kctidAsfdLangList);
				ListView_GetItem(hwndList, &lvi);
				int iesi = lvi.lParam;		// Tag for this item, which is m_vesi index.

				if (bool(qnmlv->uNewState & LVIS_SELECTED) != m_vesi[iesi].m_fSelected)
				{
					ReadCtls();
					m_vesi[iesi].m_fSelected = !m_vesi[iesi].m_fSelected;

					// The following loop is required because the ListView control does not
					// send messages for all changes if a selection is made by holding the
					// shift key down then clicking.
					int iItem = ListView_GetNextItem(hwndList, -1, LVNI_SELECTED);
					while (iItem != -1)
					{
						lvi.iItem = iItem;
						ListView_GetItem(hwndList, &lvi);
						iesi = lvi.lParam;
						m_vesi[iesi].m_fSelected = true;
						iItem = ListView_GetNextItem(hwndList, iItem, LVNI_SELECTED);
					}

					FillCtls();
				}
			}
		}
		break;

	case CBN_SELENDOK:
		// Color "combo box" was modified
		if (pnmh->idFrom == kctidFfdForeClr)
		{
			if (m_clrFore == kclrTransparent)
			{
				m_esiSel.m_clrFore = (COLORREF)FwStyledText::knUnspecified;
				m_chrpi.xFore = kxInherited;
				// Do later (UpdateComboWithInherited):
				//m_qccmbF->SetColor(m_esiSelI.m_clrFore);
			}
			else
			{
				m_esiSel.m_clrFore = m_clrFore;
				m_chrpi.xFore = kxExplicit;
			}
		}
		if (pnmh->idFrom == kctidFfdBackClr)
		{
			if (m_clrBack == kclrTransparent)
			{
				m_esiSel.m_clrBack = (COLORREF)FwStyledText::knUnspecified;
				m_chrpi.xBack = kxInherited;
				// Do later (UpdateComboWithInherited):
				//m_qccmbB->SetColor(m_esiSelI.m_clrBack);
			}
			else
			{
				m_esiSel.m_clrBack = m_clrBack;
				m_chrpi.xBack = kxExplicit;
			}
		}
		if (pnmh->idFrom == kctidFfdUnderClr)
		{
			if (m_clrUnder == kclrTransparent)
			{
				m_esiSel.m_clrUnder = (COLORREF)FwStyledText::knUnspecified;
				m_chrpi.xUnder = kxInherited;
				// Do later (UpdateComboWithInherited):
				//m_qccmbU->SetColor(m_esiSelI.m_clrUnder);
			}
			else
			{
				// If the new value is not "unspecified" and if the Underline style is "unspecified"
				// then set the Underline style to "Single line".
				if (m_fCanInherit)
				{
					int ivUnder = ::SendMessage(::GetDlgItem(m_hwnd, kctidFfdUnder), CB_GETCURSEL, 0, 0);
					if (ivUnder == 0)
					{
						::SendDlgItemMessage(m_hwnd, kctidFfdUnder, CB_SETCURSEL, 2, 0);
						m_esiSel.m_unt = kuntSingle;
						m_chrpi.xUnderT = kxExplicit;
					}
				}
				m_esiSel.m_clrUnder = m_clrUnder;
				m_chrpi.xUnder = kxExplicit;
			}
		}
		UpdateDescrips();
		break;

	case CBN_KILLFOCUS:
		UpdateComboWithInherited(ctidFrom, pnmh);
		break;
	}

	return SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet);
} // AfStyleFntDlg::OnNotifyChild.

/*----------------------------------------------------------------------------------------------
	If the value has been changed to inherited, update the combo-box to show
	the inherited value. We want to do this after the combo has lost focus so the gray
	color will show; otherwise it could look like the combo isn't working.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::UpdateComboWithInherited(int ctid, NMHDR * pnmh)
{
//	OutputDebugString("Combo lost focus - update\n");

	if (!m_fCanInherit)
		return;

	if (pnmh->idFrom == kctidFfdUnder)
	{
		if (m_chrpi.xUnderT == kxInherited)
			SetUnder(m_esiSelI.m_unt);
	}
	if (pnmh->idFrom == kctidFfdOffset)
	{
		if (m_chrpi.xOffset == kxInherited)
			SetOffset(m_esiSelI.m_mpOffset);
	}
	if (pnmh->idFrom == kctidFfdFont)
	{
		if (m_chrpi.xFont == kxInherited)
		{
			StrApp strName = m_esiSelI.m_stuFontFamily;
			HWND hwndT = ::GetDlgItem(m_hwnd, kctidFfdFont);
			bool fSaveConflictI = m_fFfConflictI;
			if (strName == "" || m_fFfConflictI)
				::SendMessage(hwndT, CB_SETCURSEL, (WPARAM)-1, 0);
			else
			{
				// Try to select that item. This generates a spurious message telling us
				// the combo changed, which sets m_fFConflict false, so preserve its value.
				if (SelectStringInTssCombo(hwndT, strName) == CB_ERR)
				{
					// If we can't, typically because it is the empty string,
					// make a null selection.
					::SendMessage(hwndT, CB_SETCURSEL, (WPARAM)-1, 0);
				}
			}
			m_fFfConflictI = fSaveConflictI;
		}
	}
	if (pnmh->idFrom == kctidFfdForeClr)
	{
		if (m_chrpi.xFore == kxInherited)
			m_qccmbF->SetColor(m_esiSelI.m_clrFore);
	}
	if (pnmh->idFrom == kctidFfdBackClr)
	{
		if (m_chrpi.xBack == kxInherited)
			m_qccmbB->SetColor(m_esiSelI.m_clrBack);
	}
	if (pnmh->idFrom == kctidFfdUnderClr)
	{
		if (m_chrpi.xUnder == kxInherited)
			m_qccmbU->SetColor(m_esiSelI.m_clrUnder);
	}
}

/*----------------------------------------------------------------------------------------------
	Process draw messages.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::OnDrawChildItem(DRAWITEMSTRUCT * pdis)
{
#if 0 // Reinstate if we create a preview--at present there is not room
	if (pdis->CtlID == kctidFfdPreview)
	{
		UpdatePreview(pdis);
		return true;
	}
#endif
	if (pdis->CtlID == kctidFfdUnder)
	{
		UpdateUnderlineStyle(pdis);
		return true;
	}
	return SuperClass::OnDrawChildItem(pdis);
}

bool AfStyleFntDlg::QueryClose(QueryCloseType qct)
{
	if (!VerifySize())
		return false;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Set up the data structures to handle the Font Features button.
	Assumes that the strings for each writing system have been read in to m_vesi.
	Note that m_qrenfeat is left = NULL whenever the Font Features button is not active.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::SetFontVarSettings(bool fOnlySelected)
{
	if (!m_fFeatures)
		return;

	m_vfl.Resize(m_vesi.Size());
	m_vflWs.Resize(m_vesi.Size());

	AssertPtr(m_qwsf);
	for (int iws = 0; iws < m_vesi.Size(); iws++)
	{
		if (fOnlySelected && !m_vesi[iws].m_fSelected)
		{
			m_vflWs[iws].MakeConflicting();
			m_vfl[iws].MakeConflicting();
			continue;
		}

		IWritingSystemPtr qws;
		CheckHr(m_qwsf->get_EngineOrNull(m_vesi[iws].m_ws, &qws));
		if (!qws || !SetFeatureEngine(iws, L""))
		{
			m_vflWs[iws].MakeConflicting();
			m_vfl[iws].MakeConflicting();
			continue;
		}

		// Fill in the defaults inherited from the writing system, if the font matches.
		SmartBstr sbstrWsDefFont;
		CheckHr(qws->get_DefaultSerif(&sbstrWsDefFont));
		StrUni stuWsDefFont(sbstrWsDefFont.Chars());
		SmartBstr sbstrWsDefFeat;
		CheckHr(qws->get_FontVariation(&sbstrWsDefFeat));
		StrUni stuWsDefFeat(sbstrWsDefFeat.Chars());
		if (m_vesi[iws].m_stuFontFamily == stuWsDefFont)
		{
			m_vflWs[iws].Init();
			FmtFntDlg::ParseFeatureString(m_qrenfeat, stuWsDefFeat, m_vflWs[iws].rgn);
		}
		else
			m_vflWs[iws].MakeConflicting();

		// Now slap on top of them the defaults inherited from any other style.
		if (m_vesiI[iws].m_stuFontFamily == m_vesi[iws].m_stuFontFamily)
			FmtFntDlg::ParseFeatureString(m_qrenfeat, m_vesiI[iws].m_stuFontVar, m_vflWs[iws].rgn);

		// Set up a list of features set for the current style.
		m_vfl[iws].Init();
		FmtFntDlg::ParseFeatureString(m_qrenfeat, m_vesi[iws].m_stuFontVar, m_vfl[iws].rgn);
	}

	m_qrenfeat = NULL;
}

/*----------------------------------------------------------------------------------------------
	Create a menu for selecting features. The features are in the main menu, with sub-menus
	containing each of the values.
----------------------------------------------------------------------------------------------*/
void AfStyleFntDlg::CreateFeaturesMenu()
{
	if (m_iwsSel == -1 || !m_qrenfeat || !FmtFntDlg::HasFeatures(m_qrenfeat))
	{
		// Can't get any features, therefore no menu.
		// If HasFeatures answers false, the button should be disabled anyway.
		goto LNoMenu;
	}

	// For now, always create a new menu, to make sure the check marks are right.
	// ENHANCE: keep the same menu, just update the check marks.
	::DestroyMenu(m_hmenuFeatures);
	m_hmenuFeatures = 0;

	if (!m_hmenuFeatures)
	{
		m_vnFeatMenuIDs.Clear();
		m_hmenuFeatures = ::CreatePopupMenu();
		if (!m_hmenuFeatures)
			ThrowHr(WarnHr(E_FAIL));

		int nLang = 0x00000409;	// for now the UI language is US English

		if (FmtFntDlg::SetupFeaturesMenu(m_hmenuFeatures, m_qrenfeat, nLang,
			m_vnFeatMenuIDs, m_vfl[m_iwsSel].rgn, m_vflWs[m_iwsSel].rgn))
		{
			return;
		}
	}
LNoMenu:
	if (m_hmenuFeatures)
	{
		::DestroyMenu(m_hmenuFeatures);
		m_hmenuFeatures = 0;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle a popup menu command for choosing the desired font features.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::CmdFeaturesPopup(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand);
	Assert(m_iwsSel > -1);

	// The user selected an expanded menu item, so perform the command now.
	//    m_rgn[1] holds the menu handle.
	//    m_rgn[2] holds the index of the selected item.

	int i = pcmd->m_rgn[2];

	if (m_qrenfeat && m_iwsSel > -1)
	{
		FmtFntDlg::HandleFeaturesMenu(m_qrenfeat, i, m_vnFeatMenuIDs, m_vfl[m_iwsSel].rgn);
		m_chrpi.xFontVar = kxExplicit;
		m_fDirty = true;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle What's This? help.
----------------------------------------------------------------------------------------------*/
bool AfStyleFntDlg::OnHelpInfo(HELPINFO * phi)
{
	return m_pafsd->DoHelpInfo(phi, m_hwnd);
}

//:>********************************************************************************************
//:>	AfFntDlgCombo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfFntDlgCombo::AfFntDlgCombo() : TssComboEx()
{
//	m_iButton = -1;
}


/*----------------------------------------------------------------------------------------------
	Create the child HWND.

	@param hwndPar Handle to the parent toolbar window.
	@param dwStyle Extended window style flag bits.
	@param wid Command ID for the toolbar child button.
	@param iButton Index of the child button within the toolbar.
	@param rc Dimensions of the toolbar child button.
----------------------------------------------------------------------------------------------*/
#if 0
void AfFntDlgCombo::Create(HWND hwndPar, DWORD dwStyle, int wid, int iButton, Rect & rc,
	HWND hwndToolTip, bool fTypeAhead)
{
	Assert(wid);

	SuperClass::Create(hwndPar, wid, hwndToolTip, fTypeAhead);

	WndCreateStruct wcs;
	wcs.InitChild(WC_COMBOBOXEX, hwndPar, wid);
	wcs.style |= dwStyle;
	wcs.SetRect(rc);
	CreateAndSubclassHwnd(wcs);

	::SendMessage(m_hwnd, WM_SETFONT, (WPARAM)::GetStockObject(DEFAULT_GUI_FONT), true);
	::ShowWindow(m_hwnd, SW_SHOW);

	m_iButton = iButton;
}
#endif

/*----------------------------------------------------------------------------------------------
	Return the help string for the toolbar child at the given point.

	@param pt Screen location of a mouse click.
	@param pptss Address of a pointer to an ITsString COM object for returning the help string.

	@return True.
----------------------------------------------------------------------------------------------*/
#if 0
bool AfFntDlgCombo::GetHelpStrFromPt(Point pt, ITsString ** pptss)
{
	AssertPtr(pptss);

	StrApp str;
	if (!AfUtil::GetResourceStr(krstWhatsThisEnabled, ::GetDlgCtrlID(m_hwnd), str))
		str.Load(kstidNoHelpError);
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu(str);
	CheckHr(qtsf->MakeString(stu.Bstr(), m_pafsd->GetUserWs(), pptss));
	return true;
}
#endif


/*----------------------------------------------------------------------------------------------
	Handle window messages.

	@param wm Windows message identifier.
	@param wp First message parameter.
	@param lp Second message parameter.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the message has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfFntDlgCombo::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Handle notifications.

	@param ctid Identifier of the common control sending the message.
	@param pnmh Pointer to an NMHDR structure containing notification code and additional info.
	@param lnRet Value to be returned to system windows send message call.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfFntDlgCombo::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(id, pnmh, lnRet))
		return true;

	// Pass the notification up to the parent window.
//	if (pnmh->code == TTN_GETDISPINFO)
//		lnRet = ::SendMessage(::GetParent(m_hwnd), WM_NOTIFY, id, (LPARAM)pnmh);

	return false;
}


/*----------------------------------------------------------------------------------------------
	Handle the combobox closing.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfFntDlgCombo::OnSelEndOK(int nID, HWND hwndCombo)
{
	// This notification can happen in several circumstances. The special case is when the
	// Alt key is down. When the user hits Alt+Up or Alt+Down to close the combobox, we don't
	// want the action to actually take place. We just want to close the dropdown list. So
	// we return true here, which keeps the TssComboEx from passing the notification on.
	if (::GetKeyState(VK_MENU) < 0)
		return true;
	return false;
}

/*----------------------------------------------------------------------------------------------
	The edit field of the combo box is no longer being edited. Do something with the
	current value.

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfFntDlgCombo::OnEndEdit(FW_NMCBEENDEDIT * pfnmee, long & lnRet)
{
	// This is done while editing is happening now (see OnSelChange below).

//	AfStyleFntDlg * pwnd = dynamic_cast<AfStyleFntDlg *>(Parent());
//	pwnd->OnComboChange(&(pfnmee->hdr), lnRet);
//	pwnd->UpdateDescrips();

//	::SendMessage(::GetParent(m_hwnd), WM_NOTIFY, kctidFfdFont, (LPARAM)&(pfnmee->hdr));
	return false;
}


/*----------------------------------------------------------------------------------------------
	This method is called when the dropdown box closes and also while typeahead is happening.
	In the former case, the OnComboChange call is probably a duplicate, but harmless, but we
	must return false so that, for instance, the updating of the edit field will happen
	normally.

	@param cid The combo-box control ID
	@param hwnd The handle of the TssComboEx; should not be used

	@return True if the notification has been handled, otherwise false.
----------------------------------------------------------------------------------------------*/
bool AfFntDlgCombo::OnSelChange(int cid, HWND hwnd)
{
	AfStyleFntDlg * pwnd = dynamic_cast<AfStyleFntDlg *>(Parent());
	long lnRet;
	NMHDR nmh;
	nmh.code = 0; // not used
	nmh.hwndFrom = hwnd;
	nmh.idFrom = cid;
	pwnd->OnComboChange(&nmh, lnRet);
	pwnd->UpdateDescrips();

	switch (cid)
	{
	case kctidFfdFont:
		pwnd->SetFontVarSettings(true);
		pwnd->UpdateFontFeatState();
		break;
	default:
		break;
	}

	return false;
}


//:>********************************************************************************************
//:>	BlankFirstSelCombo methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	In this special combo class, we intercept down arrow and, if the current selection is -1,
	change it to 1.
----------------------------------------------------------------------------------------------*/
bool BlankFirstSelCombo::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_KEYDOWN)
	{
		if (::GetKeyState(VK_SHIFT) >= 0 && ::GetKeyState(VK_CONTROL) >= 0
			&& wp == VK_DOWN)
		{
			int isel = ::SendMessage(m_hwnd, CB_GETCURSEL, 0, 0);
			if (isel == CB_ERR || isel == 0)
			{
				::SendMessage(m_hwnd, CB_SETCURSEL, 1, 0);
				return true;
			}
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}
