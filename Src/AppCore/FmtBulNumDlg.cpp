/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FmtBulNumDlg.cpp
Responsibility: Ken Zook
Last reviewed: Not yet.

Description:
	Implementation of the Format List Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE


// Send message to window identified by id
#define SM2ID(kctid,Msg,wParam,lParam) ::SendMessage( \
	::GetDlgItem(m_hwnd, kctid), Msg, (WPARAM)wParam, (LPARAM)lParam)

#define SET_CHECK(kctid,f) SM2ID(kctid, BM_SETCHECK, (f)?BST_CHECKED:BST_UNCHECKED, 0)

/***********************************************************************************************
	Methods.
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructors.
----------------------------------------------------------------------------------------------*/
FmtBulNumDlg::FmtBulNumDlg(ILgWritingSystemFactory * pwsf)
{
	AssertPtr(pwsf);
	BasicInit();
	m_qwsf = pwsf;
	m_pszHelpUrl = _T("User_Interface/Menus/Format/Bullets_and_Numbering.htm");
	m_wsDefault = 0;
	m_pafsd = NULL;
}

FmtBulNumDlg::FmtBulNumDlg(AfStylesDlg * pafsd, ILgWritingSystemFactory * pwsf)
{
	AssertPtrN(pafsd);
	AssertPtr(pwsf);

	BasicInit();
	m_pafsd = pafsd;
	if (pafsd)
	{
		SetCanDoRtl(pafsd->CanDoRtl());
		SetOuterRtl(pafsd->OuterRtl());
		m_pszHelpUrl = _T("User_Interface/Menus/Format/Style/Style_Bullets_and_Numbering_tab.htm");
	}
	else
	{
		m_pszHelpUrl = _T("User_Interface/Menus/Format/Bullets_and_Numbering.htm");
	}
	m_qwsf = pwsf;
	m_wsDefault = 0;
}


void FmtBulNumDlg::BasicInit()
{
	m_rid = kridFmtBulNumDlg;
	m_ltListType = kltNotAList;
	m_iCbBullet = 1; // medium circle for bullet.
	m_iCbNumber = kno1;
	m_fCxStartAt = false;
	m_nEdStartAt = 1; // Default number to start at is 1.
	m_strEdStartAt = "1";
	m_icchEdStartAtSelMin = 1;
	m_icchEdStartAtSelLim = 1;
	m_nStartAtMin = 0;
	m_nStartAtMax = kStartAtMax;
	m_stuEdTxtBef = "";
	m_stuEdTxtAft = "";
	m_cbTxtAft = 200;
}

void FmtBulNumDlg::SetCanDoRtl(bool fCanDoRtl)
{
	m_fCanDoRtl = fCanDoRtl;
}

/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)
----------------------------------------------------------------------------------------------*/
bool FmtBulNumDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	ListType orig_ltListType = m_ltListType;

	// Subclass the Font button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton (m_hwnd, kctidFbnPbFont, kbtFont, NULL, 0);

	// Set the Bullet Option combo box font to WingDings; and fill with bullet options.
	m_hfontBullet = AfGdi::CreateFont(15, 0, 0, 0, FW_DONTCARE, 0, 0, 0, SYMBOL_CHARSET,
		OUT_TT_PRECIS, CLIP_TT_ALWAYS, DEFAULT_QUALITY, VARIABLE_PITCH | TMPF_TRUETYPE,
		_T("WingDings"));
	HWND hwndBullet = ::GetDlgItem(m_hwnd, kctidFbnCbBullet);
	::SendMessage(hwndBullet, WM_SETFONT, (WPARAM)m_hfontBullet, 0);
	for (int i = 0; i < s_cBulletOptions; i++)
		::SendMessage(hwndBullet, CB_ADDSTRING, 0, (LPARAM)s_rgszBulletOptions[i]);
	::SendMessage(hwndBullet, CB_LIMITTEXT, 1, 0);

	// Fill combo box with numbering options.
	HWND hwndNum = ::GetDlgItem (m_hwnd, kctidFbnCbNumber);
	for (int i = 0; i < s_cNumberOptions; i++)
		::SendMessage (hwndNum, CB_ADDSTRING, 0, (LPARAM)s_rgszNumberOptions[i]);

	// Set the list type.
	SM2ID(kctidFbnCbBullet, CB_SETCURSEL, ICbBullet(), 0);
	SM2ID(kctidFbnCbNumber, CB_SETCURSEL, ICbNumber(), 0);

	// Setup StartAt edit box.
	SetEdStartAt(NEdStartAt(), false);

	// Setup the StartAt spin control.
	UDACCEL uda; uda.nSec = 0; uda.nInc = 1;
	SM2ID(kctidFbnSpStartAt, UDM_SETACCEL, 1, &uda);
	SM2ID(kctidFbnSpStartAt, UDM_SETRANGE, 0, MAKELONG(kStartAtMax-1,kStartAtMin));

	// Text before and after.
	StrApp str = StuEdTxtBef();
	SM2ID(kctidFbnEdTxtBef, WM_SETTEXT, 0, str.Chars());
	str = StuEdTxtAft();
	SM2ID(kctidFbnEdTxtAft, WM_SETTEXT, 0, str.Chars());

	::CheckDlgButton (m_hwnd, kctidFbnCxStartAt,
		(FCxStartAt() == 1) ? BST_CHECKED : BST_UNCHECKED);

	SetListType (orig_ltListType); // Do this last because the above can affect it.

	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnRbUnspecified), m_fCanInherit);
//		(m_fCanInherit && m_xEorI == kxExplicit));

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
			(pfn)(::GetDlgItem(m_hwnd, kctidFbnRbNotAList), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFbnRbBullet), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFbnRbNumber), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFbnRbUnspecified), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFbnCbBullet), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFbnCbNumber), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFbnEdStartAt), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFbnSpStartAt), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFbnEdTxtBef), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFbnEdTxtAft), L"", L"");
		}

		::FreeLibrary(hmod);
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle the messages that cause the controls to be recolored based on their state.
----------------------------------------------------------------------------------------------*/
bool FmtBulNumDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
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
bool FmtBulNumDlg::ColorForInheritance(WPARAM wp, LPARAM lp, long & lnRet)
{
	HWND hwndBullet = ::GetDlgItem(m_hwnd, kctidFbnCbBullet);
	HWND hwndNumber = ::GetDlgItem(m_hwnd, kctidFbnCbNumber);
	HWND hwndStartAt = ::GetDlgItem(m_hwnd, kctidFbnCxStartAt);
	HWND hwndEdStartAt = ::GetDlgItem(m_hwnd, kctidFbnEdStartAt);
	HWND hwndTxtBef = ::GetDlgItem(m_hwnd, kctidFbnEdTxtBef);
	HWND hwndTxtAft = ::GetDlgItem(m_hwnd, kctidFbnEdTxtAft);

	HWND hwndArg = (HWND)lp;

	if (hwndArg != hwndBullet && hwndArg != hwndNumber &&
		hwndArg != hwndStartAt && hwndArg != hwndEdStartAt &&
		hwndArg != hwndTxtBef && hwndArg != hwndTxtAft)
	{
		return false;
	}

	::SetTextColor((HDC)wp, (m_xEorI == kxExplicit ? ::GetSysColor(COLOR_WINDOWTEXT) : kclrGray50));
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
	Initialize the format bullets and numbering dialog for editing a style represented as a
	TsTextProps.
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::InitForStyle(ITsTextProps * pttp, ITsTextProps * pttpInherited,
	ParaPropRec & xprOrig, bool fEnable, bool fCanInherit)
{
	m_fCanInherit = fCanInherit;

	CPropBulNum cpt;
	CPropBulNum cptI;

	cpt.ltListType = kltUnspecified;
	cpt.nEdStartAt = FwStyledText::knUnspecified;

	// Disable the controls if the selected style is not a paragraph style.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnRbNotAList), fEnable); // Not a List button.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnRbBullet), fEnable); // Bullet button.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnCbBullet), fEnable); // Bullet Scheme combobox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnRbNumber), fEnable); // Number button.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnCbNumber), fEnable); // Number Scheme combobox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnCxStartAt), fEnable); // Start At button.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnEdStartAt), fEnable); // Start At editbox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnSpStartAt), fEnable); // Start At spin ctrl.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnEdTxtBef), fEnable); // Text Before combobox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnEdTxtAft), fEnable); // Text After combobox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnPbFont), fEnable); // Font button.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnBulSch), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnNumSch), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnNumTB), fEnable);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnNumTA), fEnable);

	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnRbUnspecified), fEnable && m_fCanInherit);

	// If this is a paragraph style, get the properties associated with the Bullets
	// and Numbering dialog and load them into the dialog.
	if (fEnable)
	{
		m_nRtl = FwStyledText::knUnspecified;
		GetProps(pttpInherited, cptI, m_nRtl);
		GetProps(pttp, cpt, m_nRtl);

		if (!m_fCanInherit)
			MergeExplicitAndInherited(cpt, cptI);

		m_xEorI = (cpt.ltListType != kltUnspecified && cpt.ltListType != -1) ?
			kxExplicit : kxInherited;

		DecodeValues(cptI, m_nRtl, false, false);
		DecodeValues(cpt, m_nRtl, true, true);	// Put the properties into the dialog box class.

		m_qttpFirst = NULL;
		AdjustPreview();
		int nVar;
		CheckHr(pttp->GetIntPropValues(ktptWs, &nVar, &m_wsDefault));
		if (m_wsDefault == -1 && m_fCanInherit)
			CheckHr(pttpInherited->GetIntPropValues(ktptWs, &nVar, &m_wsDefault));
	}
}

/*----------------------------------------------------------------------------------------------
	Merge all the explicit and inherited values and pretend that they are all explicit.
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::MergeExplicitAndInherited(CPropBulNum & cptE, CPropBulNum & cptI)
{
	if (cptE.ltListType == kltUnspecified)
		cptE.ltListType = cptI.ltListType;
	if (cptE.nEdStartAt == FwStyledText::knUnspecified)
		cptE.nEdStartAt = cptI.nEdStartAt;
	if (cptE.stuEdTxtBef == L"")
		cptE.stuEdTxtBef = cptI.stuEdTxtBef;
	if (cptE.stuEdTxtAft == L"")
		cptE.stuEdTxtAft = cptI.stuEdTxtAft;
	if (cptE.stuFontInfo == L"")
		cptE.stuFontInfo = cptI.stuFontInfo;
}

/*----------------------------------------------------------------------------------------------
	Populate a CPropBulNum structure from TsTextProps accessed via pttp.
	CPropBulNum has all the properties relevant to Format Bullets and Numbering.
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::GetProps(ITsTextProps * pttp, CPropBulNum & cpt, int & nRtl)
{
	int cprop;
	int tpt;
	int nVar;
	int nVal;
	int iprop;
	SmartBstr sbstr;

	CheckHr(pttp->get_IntPropCount(&cprop));
	for (iprop = 0; iprop < cprop; ++iprop)
	{
		CheckHr(pttp->GetIntProp(iprop, &tpt, &nVar, &nVal));
		switch (tpt)
		{
		default: // property not handled in this dialog.
			break;
		case ktptBulNumScheme:
			cpt.ltListType = nVal;
			break;
		case ktptBulNumStartAt:
			cpt.nEdStartAt = nVal;
			break;
		case ktptRightToLeft:
			nRtl = nVal;
			break;
		}
	}

	CheckHr(pttp->get_StrPropCount(&cprop));
	for (iprop = 0; iprop < cprop; ++iprop)
	{
		CheckHr(pttp->GetStrProp(iprop, &tpt, &sbstr));
		switch (tpt)
		{
		default: // property not handled in this dialog.
			break;
		case ktptBulNumTxtBef:
			cpt.stuEdTxtBef.Assign(sbstr.Chars(), sbstr.Length());
			break;
		case ktptBulNumTxtAft:
			cpt.stuEdTxtAft.Assign(sbstr.Chars(), sbstr.Length());
			break;
		case ktptBulNumFontInfo:
			cpt.stuFontInfo.Assign(sbstr.Chars(), sbstr.Length());
			break;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Retrieve the settings from the Bullets and Numbering tab and set the properties into a text
	prop which is intiallly built from the originial one (if this exists) but which has relevant
	values overwritten from the tab. We take the view that it doesn't matter whether some of
	these properties were the same before. However, if none has changed then *ppttp is returned
	as NULL to avoid unneccesary updates being triggered.
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::GetStyleEffects(ITsTextProps *pttpOrig, ITsTextProps ** ppttp)
{
	Assert(!*ppttp);
	CPropBulNum cptOrig;
	CPropBulNum cptNew;
	int nRtlBogus;
	ITsPropsBldrPtr qtpb;

	// First get the values out of the dialog controls.
	EncodeValues(cptNew);

	// Populate another CPropBulNum with the old values (if any). Note that old values
	// are assumed to be those constructed by CPropBulNum() if pttpOrig is NULL.
	if (pttpOrig)
		GetProps(pttpOrig, cptOrig, nRtlBogus);

	// If the new and original properties are the same, do nothing.
	if (cptNew.ltListType == cptOrig.ltListType && cptNew.nEdStartAt == cptOrig.nEdStartAt
		&& cptNew.stuEdTxtAft == cptOrig.stuEdTxtAft && cptNew.stuEdTxtBef == cptOrig.stuEdTxtBef
		&& cptNew.stuFontInfo == cptOrig.stuFontInfo)
	{
		return;	// Leaves *ppttp NULL.
	}

	// Make a text props builder.
	if (pttpOrig)
		CheckHr(pttpOrig->GetBldr(&qtpb));	// Builder starts with original values.
	else
		qtpb.CreateInstance(CLSID_TsPropsBldr);	// Builder starts empty.

	// Now put values into the builder.
	if (cptNew.ltListType == -1)
		CheckHr(qtpb->SetIntPropValues(ktptBulNumScheme, -1, -1));
	else
		CheckHr(qtpb->SetIntPropValues(ktptBulNumScheme, ktpvEnum, cptNew.ltListType));

	if (cptNew.nEdStartAt == FwStyledText::knConflicting ||
		cptNew.nEdStartAt == FwStyledText::knUnspecified)
	{
		CheckHr(qtpb->SetIntPropValues(ktptBulNumStartAt, -1, -1));
	}
	else
	{
		CheckHr(qtpb->SetIntPropValues(ktptBulNumStartAt, ktpvDefault, cptNew.nEdStartAt));
	}
	CheckHr(qtpb->SetStrPropValue(ktptBulNumTxtBef, cptNew.stuEdTxtBef.Bstr()));
	CheckHr(qtpb->SetStrPropValue(ktptBulNumTxtAft, cptNew.stuEdTxtAft.Bstr()));
	CheckHr(qtpb->SetStrPropValue(ktptBulNumFontInfo, cptNew.stuFontInfo.Bstr()));

	// Finally, make a text prop from the builder.
	CheckHr(qtpb->GetTextProps(ppttp));

	return;
}

/*----------------------------------------------------------------------------------------------
	This method should be called in order to set m_ltListType. (DO NOT set m_ltListType
	directly!) In addition to setting m_ltListType, this method does the following:
	- the appropriate radio button is checked.
	- the other radio buttons are unchecked.
	- any controls corresponding to unchecked radio buttons are disabled.
	- any controls corresponding to the checked radio button are enabled.
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::SetListType(ListType eNewType)
{
	Assert(kltUnspecified == eNewType || kltNotAList == eNewType ||
		kltBullet == eNewType || kltNumber == eNewType);

	m_ltListType = eNewType;		// Set the new type.

	bool fNotAList = (m_xEorI == kxExplicit && kltNotAList == m_ltListType);
	bool fBullet = (m_xEorI == kxExplicit && kltBullet == m_ltListType);
	bool fNumber = (m_xEorI == kxExplicit && kltNumber == m_ltListType);

	// Set the correct radio button and clear the others.
	SET_CHECK(kctidFbnRbNotAList, fNotAList);
	SET_CHECK(kctidFbnRbBullet, fBullet);
	SET_CHECK(kctidFbnRbNumber, fNumber);
	SET_CHECK(kctidFbnRbUnspecified, m_xEorI == kxInherited);

	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnBulSch), fBullet); // Bullet Scheme label.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnCbBullet), fBullet); // Bullet Scheme combobox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnCbNumber), fNumber); // Number Scheme combobox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnNumSch), fNumber); // Number Scheme label.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnNumTB), fNumber); // Number text before label.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnNumTA), fNumber); // Number text after label.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnCxStartAt),fNumber); // Start At button.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnEdStartAt),fNumber); // Start At editbox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnSpStartAt),fNumber); // Start At spin ctrl.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnEdTxtBef), fNumber); // Text Before combobox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnEdTxtAft), fNumber); // Text After combobox.
	::EnableWindow(::GetDlgItem(m_hwnd, kctidFbnPbFont), fNumber | fBullet); // Font button.

	// Redraw the preview window.
	AdjustPreview();
}

/*----------------------------------------------------------------------------------------------
	Process notifications from user.
----------------------------------------------------------------------------------------------*/
bool FmtBulNumDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr (pnmh);
	StrAppBuf strb;
	int cch;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidFbnRbNotAList:
			SetExplicit(kxExplicit);
			SetListType(kltNotAList);
			break;
		case kctidFbnRbBullet:
			SetExplicit(kxExplicit);
			SetListType(kltBullet);
			break;
		case kctidFbnRbNumber:
			SetExplicit(kxExplicit);
			SetListType(kltNumber);
			break;
		case kctidFbnRbUnspecified:
			SetExplicit(kxInherited);
			SetListType(kltUnspecified);
			//AdjustPreview();
			break;
		case kctidFbnCxStartAt:
			if (m_xEorI != kxExplicit)
				return false;
			SetListType(kltNumber);
			m_fCxStartAt = (BST_CHECKED == SM2ID(kctidFbnCxStartAt,BM_GETCHECK,0,0));
			if (!m_fCxStartAt)
			{
				SetEdStartAt(1, false);	// Start at 1 if "Start At" is unchecked...
				AdjustPreview();		// ...and update preview.
			}
			break;
		case kctidFbnPbFont:
			return OnFontChange(pnmh, lnRet);
		}
		return true;
	case EN_CHANGE: // only edit controls

		if (m_fDisableEnChange) // prevent recursion
			return true;
		if (m_xEorI != kxExplicit)
			return true;
		cch = ::SendMessage (pnmh->hwndFrom, WM_GETTEXT, strb.kcchMaxStr, (LPARAM)strb.Chars());
		strb.SetLength(cch);
		switch (pnmh->idFrom)
		{
		case kctidFbnEdStartAt:
			{
//			SetListType(kltNumber);
			int nMax(ConvStrToNum(m_iCbNumber, strb.Chars()));
			if (nMax > m_nStartAtMax)
			{
				StrApp strM(kctidFbnSAMsg);
				StrApp strT(kctidFbnSATitle);
				StrApp strFmt;
				strFmt.Format(strM.Chars(), m_nStartAtMin, m_nStartAtMax);
				::MessageBox(m_hwnd, strFmt.Chars(), strT.Chars(), MB_OK | MB_ICONWARNING);
				nMax = m_nStartAtMax;
			}
			SetEdStartAt(nMax, true);
			break;
			}
		case kctidFbnEdTxtBef:
//			SetListType(kltNumber);
			if (strb.Length() > kcchEdTxtMax)
			{
				::MessageBeep(MB_ICONEXCLAMATION);
				strb.SetLength(kcchEdTxtMax);
				m_stuEdTxtBef.Assign(strb.Chars(), strb.Length());
				::SendMessage(pnmh->hwndFrom, WM_SETTEXT, 0, (LPARAM)strb.Chars());
				::SendMessage(pnmh->hwndFrom, EM_SETSEL, kcchEdTxtMax, kcchEdTxtMax);
				StrApp strTitle(kctidFbnTxtBATitle);
				StrApp strMsgFmt(kctidFbnTxtBAMsg);
				StrApp strMsg;
				strMsg.Format(strMsgFmt.Chars(), kcchEdTxtMax);
				::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
			}
			else
				m_stuEdTxtBef.Assign(strb.Chars(), strb.Length());
			break;
		case kctidFbnEdTxtAft:
//			SetListType(kltNumber);
			if (strb.Length() > kcchEdTxtMax)
			{
				::MessageBeep(MB_ICONEXCLAMATION);
				strb.SetLength(kcchEdTxtMax);
				m_stuEdTxtAft.Assign(strb.Chars(), strb.Length());
				::SendMessage(pnmh->hwndFrom, WM_SETTEXT, 0, (LPARAM)strb.Chars());
				::SendMessage(pnmh->hwndFrom, EM_SETSEL, kcchEdTxtMax, kcchEdTxtMax);
				StrApp strTitle(kctidFbnTxtBATitle);
				StrApp strMsgFmt(kctidFbnTxtBAMsg);
				StrApp strMsg;
				strMsg.Format(strMsgFmt.Chars(), kcchEdTxtMax);
				::MessageBox(m_hwnd, strMsg.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
			}
			else
				m_stuEdTxtAft.Assign(strb.Chars(), strb.Length());
			break;
		}
		AdjustPreview();
		return true;

	case UDN_DELTAPOS: // Spin control is activated.
		if (m_xEorI != kxExplicit)
			return true;
//		SetListType(kltNumber);
		SetEdStartAt(m_nEdStartAt + ((NMUPDOWN*)pnmh)->iDelta, false);
		AdjustPreview();
		return true;
	case CBN_SELCHANGE: // Combo box item changed.
		if (m_xEorI != kxExplicit)
			return true;
		return OnComboChange (pnmh, lnRet);
	case EN_KILLFOCUS: // Edit control modified.
		if (m_xEorI != kxExplicit)
			return true;
		if (pnmh->idFrom == kctidFbnEdStartAt)
		{
			cch = ::SendMessage (pnmh->hwndFrom, WM_GETTEXT, strb.kcchMaxStr,
				(LPARAM)strb.Chars());
			strb.SetLength(cch);
			if (cch == 0)
			{
				SetEdStartAt(kStartAtMin, false); // Text is empty, set it to 1
				AdjustPreview();
				return true;
			}
		}
	}

	return SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Handle a change in a combo box.
----------------------------------------------------------------------------------------------*/
bool FmtBulNumDlg::OnComboChange (NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	int icb = ::SendMessage (pnmh->hwndFrom, CB_GETCURSEL, 0, 0); // Get combo box index.

	switch (pnmh->idFrom)
	{
	case kctidFbnCbBullet:
		SetListType(kltBullet);
		m_iCbBullet = icb;
		break;
	case kctidFbnCbNumber:
		SetListType(kltNumber);
		m_iCbNumber = icb;
		// Std European numbers can start at 0, all others at 1
		m_nStartAtMin = kStartAtMin - (icb == kno1 || icb == kno01);
		switch (m_iCbNumber)
		{
		case knoA:
		case knoa:
			m_nStartAtMax = 780;
			break;
		default:
			m_nStartAtMax = 14999;
			break;
		}

		SM2ID(kctidFbnCbNumber, UDM_SETRANGE, 0, MAKELONG(m_nStartAtMax-1,m_nStartAtMin));
		SetEdStartAt(m_nEdStartAt, false);
		break;
	default:
		Assert(false); // We shouldn't get here.
	}

	AdjustPreview();
	lnRet = 0;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Make all inherited values be explicit.
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::SetExplicit(int x)
{
	if (!m_fCanInherit)
	{
		Assert(m_xEorI == kxExplicit);
		return;
	}

	if (x == kxExplicit && m_xEorI != kxExplicit)
	{
		m_ltListType = m_ltListTypeI;
		m_iCbBullet = m_iCbBulletI;
		m_iCbNumber = m_iCbNumberI;
		m_fCxStartAt = m_fCxStartAtI;
		m_nEdStartAt = m_nEdStartAtI;
		m_stuEdTxtBef = m_stuEdTxtBefI;
		m_stuEdTxtAft = m_stuEdTxtAftI;
		if (m_hwnd)
			::InvalidateRect(m_hwnd, NULL, false);
	}
	else
	{
		m_ltListType = kltUnspecified;
	}
	m_xEorI = x;
}

/*----------------------------------------------------------------------------------------------
	Supports the user choosing a different font and color for displaying the label.
	We call the FieldWorks font dialog to manipulate the TsTextProps used
	to control the preview.
----------------------------------------------------------------------------------------------*/
bool FmtBulNumDlg::OnFontChange(NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	SmartBstr sbstrFontInfo;
	ITsTextPropsPtr qttp = m_qttpFirst;
	CheckHr(m_qttpFirst->GetStrPropValue(ktptBulNumFontInfo, &sbstrFontInfo));

	if (sbstrFontInfo.Length())
	{
		StrUni stuFont(sbstrFontInfo.Chars(), sbstrFontInfo.Length());
		ITsPropsBldrPtr qtpb;
		m_qttpFirst->GetBldr(&qtpb);
		DecodeFontInfo(stuFont, qtpb);
		CheckHr(qtpb->GetTextProps(&qttp));
	}

	ITsTextProps * pttp = qttp;
	if (!m_qvps)
	{
		// Make up a bogus property store. This is needed within the styles dialog, which
		// doesn't have one from the outer view.
		m_qvps.CreateInstance(CLSID_VwPropertyStore);
		m_qvps->putref_WritingSystemFactory(m_qwsf);
	}

	IVwPropertyStorePtr qvps;
	qvps.CreateInstance(CLSID_VwPropertyStore);
	m_qvps->get_DerivedPropertiesForTtp(pttp, &qvps);

	// Add a writing system to the properties, if they don't already include one.
	int ws;
	int nVar;
	CheckHr(pttp->GetIntPropValues(ktptWs, &nVar, &ws));
	if (ws == -1)
	{
		if (m_wsDefault == -1 || m_wsDefault == 0)
			CheckHr(m_qwsf->get_UserWs(&m_wsDefault));
		ITsPropsBldrPtr qtpb;
		CheckHr(pttp->GetBldr(&qtpb));
		CheckHr(qtpb->SetIntPropValues(ktptWs, 0, m_wsDefault));
		CheckHr(qtpb->GetTextProps(&qttp));
		pttp = qttp;
	}

	TtpVec vqttp;
	vqttp.Push(pttp);
	VwPropsVec vqvps;
	vqvps.Push(qvps);

	// Get adjustments from Font dialog, inhibiting some controls.
	// TODO (SharonC): Is it adequate to just send vqvps, or do we have to do something
	// more sophisticated to separate the hard- and soft-formatting?
	if (FmtFntDlg::AdjustTsTextProps(m_hwnd, vqttp, vqvps, m_qwsf, m_pszHelpFile,
		(m_ltListType == kltBullet || m_ltListType == kltNumber), m_ltListType == kltBullet))
	{
		pttp = vqttp[0];
		if (pttp)
		{
			// Pack the font info back into m_qttpFirst's ktptBulNumFontInfo property.
			// Make qttp responsible for the reference count on the new ttp.
			qttp = pttp;
			m_stuFontInfo = EncodeFontInfo(qttp);
		}
	}
	AdjustPreview();
	lnRet = 0;
	return true;
}


/*----------------------------------------------------------------------------------------------
	Set m_nEdStartAt, display value and synchronize spinner with m_nEdStartAt.

	Note: this has the side effect of setting m_nEdStartAt to the inherited value if
	inheritance is happening, but that should be harmless.
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::SetEdStartAt(int nStartAt, bool fIsKeyIn)
{
// can't blank field for non-European numbers

	int icchLim, icchMin; // sel_beg, sel_end;
	int cchKeyIn, cchNew;
	StrAppBuf strKeyIn, strNew;

	m_fDisableEnChange = true; // prevent endless recursion

	// Get keyed text and selection
	cchKeyIn = SM2ID(kctidFbnEdStartAt, WM_GETTEXT, strKeyIn.kcchMaxStr, strKeyIn.Chars());
	strKeyIn.SetLength(cchKeyIn);
	SM2ID(kctidFbnEdStartAt, EM_GETSEL, &icchMin, &icchLim);

	// Check for invalid keyin - number conversions return 0 for error
	int i;
	bool fOk = true;
	if (fIsKeyIn && nStartAt == 0)
	{
		switch (m_iCbNumber)
		{
		case kno1: // 0 valid only for std European numbers
		case kno01:
			for (i = 0; i < cchKeyIn; i++) // blank or all zeros ok
				fOk = fOk && (strKeyIn.GetAt(i) == '0');
			break;
		default: // blank ok for non-European numbers
			fOk = (cchKeyIn == 0);
			nStartAt = kStartAtMin; // if ok, use this
		}
	}

	// Compute new values
	if (!fOk) // Not ok, just plug in previous values instead of new ones
	{
		nStartAt = NEdStartAt();
		strNew = m_strEdStartAt.Chars();
		cchKeyIn = strNew.Length();
		icchLim = m_icchEdStartAtSelMin;
		icchMin = m_icchEdStartAtSelLim;
	}
	else // Ok, compute new values. Note that start_at can be knNinch.
	{
		if (nStartAt == knNinch)
		{
			// At present, starting at 1 always goes with not checking the "Start at" box.
			nStartAt = 1;
		}
		if (nStartAt > m_nStartAtMax)
		{
			StrApp strM(kctidFbnSAMsg);
			StrApp strT(kctidFbnSATitle);
			StrApp strFmt;
			strFmt.Format(strM.Chars(), m_nStartAtMin, m_nStartAtMax);
			::MessageBox(m_hwnd, strFmt.Chars(), strT.Chars(), MB_OK | MB_ICONWARNING);
		}

		m_nEdStartAt = NBound(nStartAt, m_nStartAtMin, m_nStartAtMax);
		if (m_fCanInherit && m_xEorI != kxExplicit)
			m_nEdStartAtI = m_nEdStartAt;
		if (cchKeyIn == 0 && fIsKeyIn)
			strNew.Clear();
		else
			FormatListNumber(m_nEdStartAt, m_nEdStartAt, 0, strNew);
	}

	// Display new values and synchronize spinner
	SM2ID(kctidFbnEdStartAt, WM_SETTEXT, 0, strNew.Chars());
	SM2ID(kctidFbnSpStartAt, UDM_SETPOS, 0, m_nEdStartAt);
	m_strEdStartAt = strNew.Chars();
	if (NEdStartAt() != 1)
	{
		(m_fCanInherit && m_xEorI != kxExplicit) ? m_fCxStartAtI = true : m_fCxStartAt = true;
	}
	SET_CHECK(kctidFbnCxStartAt, (FCxStartAt() == 1));

	// Restore selection as best we can
	cchNew = strNew.Length();
	if (fIsKeyIn)
	{	// keep selection as close to original as possible
		int cchDiff = cchNew - cchKeyIn;
		m_icchEdStartAtSelMin = NBound(icchMin + cchDiff, 0, cchNew);
		m_icchEdStartAtSelLim = NBound(icchLim + cchDiff, 0, cchNew);
	}
	else
	{	// select all for easy keyin of something different
		m_icchEdStartAtSelMin = 0;
		m_icchEdStartAtSelLim = cchNew;
	}
	SM2ID(kctidFbnEdStartAt, EM_SETSEL, m_icchEdStartAtSelMin, m_icchEdStartAtSelLim);

	m_fDisableEnChange = false;
}

/*----------------------------------------------------------------------------------------------
	Given a number, appends it in the appropriate format (e.g., "A, B, C, ...", etc.) to strb.
	If the number cannot be handled, a "?" is returned. The format is determined by the value
	currently in m_iCbNumber.
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::FormatListNumber(int nMin, int nNum, int nOffset, StrAppBuf & strb)
{
	StrAppBuf strbTmp;
	if (nNum < nMin)
		nNum = nMin;  // For robustness; %M dies on negative numbers.
	int nNumber = ((FCxStartAt() == 1) ? nNum : nMin) + nOffset;
	// Switch between the supported numbering options.
	switch (ICbNumber()) {
	case kno1:
		strbTmp.Format(_T("%d"), nNumber);
		break;
	case knoI:
		strbTmp.Format(_T("%M"), nNumber);
		break;
	case knoi:
		strbTmp.Format(_T("%m"), nNumber);
		break;
	case knoA:
		strbTmp.Format(_T("%O"), nNumber);
		break;
	case knoa:
		strbTmp.Format(_T("%o"), nNumber);
		break;
	case kno01:
		strbTmp.Format(_T("%02d"), nNumber);
		break;
	case -1:
		// Conflicting
		strbTmp.Clear();
		break;
	default:
		Assert(false);
	}

	strb.Append(strbTmp);
}

/*----------------------------------------------------------------------------------------------
	Convert the string to a number.
----------------------------------------------------------------------------------------------*/
int FmtBulNumDlg::ConvStrToNum(int number_option, const achar * str)
{
	int nVal;
	switch (number_option)
	{
	case kno1:
	case kno01:
		nVal = StrUtil::ParseInt(str);
		break;
	case knoI:
	case knoi:
		nVal = StrUtil::ParseRomanNumeral(str);
		break;
	case knoA:
	case knoa:
		nVal = StrUtil::ParseAlphaOutline(str);
		break;
	default:
		Assert(false); // This is illegal.
	}
	return nVal;
}

/*----------------------------------------------------------------------------------------------
	Process draw messages.
----------------------------------------------------------------------------------------------*/
bool FmtBulNumDlg::OnDrawChildItem(DRAWITEMSTRUCT * pdis)
{
	if (pdis->CtlID == kctidFbnPreview)
	{
		UpdatePreview(pdis);
		return true;
	}
	return SuperClass::OnDrawChildItem(pdis);
}

/*----------------------------------------------------------------------------------------------
	Takes care of painting the button behind the preview window. This window exists only to
	create the border and allow the preview position to be specified by the resource editor.
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::UpdatePreview(DRAWITEMSTRUCT * pdis)
{
	AssertPtr(pdis);
	HDC hdc = pdis->hDC;
	DrawEdge(hdc, &pdis->rcItem, EDGE_SUNKEN, BF_RECT);
}

/*----------------------------------------------------------------------------------------------
	This is a static method, the main starting point for using the class. It is typically called
	from AfVwRootSite::FormatParas.

	It creates an instance of the class and passes the parameters on to it.
----------------------------------------------------------------------------------------------*/
bool FmtBulNumDlg::AdjustTsTextProps(HWND hwnd, bool fCanDoRtl, bool fOuterRtl,
	TtpVec & vpttpOrig, TtpVec & vpttpHard, VwPropsVec &vqvpsSoft,
	ILgWritingSystemFactory * pwsf, const achar * pszHelpFile)
{
	AssertPtr(pwsf);

	FmtBulNumDlgPtr qdlg;
	qdlg.Attach(NewObj FmtBulNumDlg(pwsf));
	qdlg->SetCanDoRtl(fCanDoRtl);
	qdlg->SetOuterRtl(fOuterRtl);
	qdlg->SetHelpFile(pszHelpFile);
	return qdlg->AdjustTsTextPropsDo (hwnd, vpttpOrig, vpttpHard, vqvpsSoft);
}

/*----------------------------------------------------------------------------------------------
	Run the dialog to edit the TsTextProps in TtpVec, which are typically the style objects
	for one or more paragraphs. If context is needed, pvps should be the containing properties
	for the whole paragraph sequence.

	vpttpOrig and vpttpHard are identical except that the vpttpHard members have had
	the named style removed. Thus vpttpOrig is what should be used in generating the new
	version of the properties, while vpttpHard is what is used to fill the controls.
	vqvpsSoft is used to show the inherited or soft values.
----------------------------------------------------------------------------------------------*/
bool FmtBulNumDlg::AdjustTsTextPropsDo(HWND hwnd, TtpVec & vpttp,
	TtpVec & vpttpHard, VwPropsVec & vqvpsSoft)
{
	m_qvps = vqvpsSoft[0];
	m_qvps->putref_WritingSystemFactory(m_qwsf);

	CPropBulNum hard, soft, znew;

	int nRtlHard, nRtlSoft;

	m_fCanInherit = true;
	m_xEorI = kxInherited;
	int cttp = vpttp.Size();
	Assert(vqvpsSoft.Size() == cttp);

	// Get the stored bullet/numbering properties.
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

			MergeFmtDlgIntProp(pttp, pvps, ktptBulNumScheme, ktpvEnum,
				hard.ltListType, soft.ltListType, fFirst, m_xEorI, fHard);

			// The only property that really counts as far as determining whether we are
			// using hard or soft values is ktptBulNumScheme. Ignore all the others.
			int xBogus;

			// StartAt is special. We only consider the first ttp as input.
			if (fFirst)
			{
				MergeFmtDlgIntProp(pttp, pvps, ktptBulNumStartAt, ktpvDefault,
					hard.nEdStartAt, soft.nEdStartAt, fFirst, xBogus, fHard);
			}

			MergeFmtDlgStrProp(pttp, pvps, ktptBulNumTxtBef, hard.stuEdTxtBef, soft.stuEdTxtBef,
				fFirst, xBogus, fHard);
			MergeFmtDlgStrProp(pttp, pvps, ktptBulNumTxtAft, hard.stuEdTxtAft, soft.stuEdTxtAft,
				fFirst, xBogus, fHard);
			MergeFmtDlgStrProp(pttp, pvps, ktptBulNumFontInfo, hard.stuFontInfo,
				soft.stuFontInfo, fFirst, xBogus, fHard);

			// Also get the direction of the selected paragraphs, for showing the preview.
			if (fHard)
			{
				MergeIntProp(vpttpHard[ittp], vqvpsSoft[ittp],
					ktptRightToLeft, ktpvEnum,
					nRtlHard, nRtlSoft, fFirst);
			}
		}
	}

	if (nRtlHard == FwStyledText::knUnspecified)
		nRtlHard = nRtlSoft;

	StrUni stuInherit = kstidInherit;
	if (hard.stuEdTxtBef == stuInherit)
		hard.stuEdTxtBef = soft.stuEdTxtBef;
	if (hard.stuEdTxtAft == stuInherit)
		hard.stuEdTxtAft = soft.stuEdTxtAft;
	if (hard.stuFontInfo == stuInherit)
		hard.stuFontInfo = soft.stuFontInfo;

	// Unpack any number font info into m_qttpFirst
	m_qttpFirst = vpttp[0];
	if (!m_qttpFirst)
	{
		ITsPropsBldrPtr qtpb;
		qtpb.CreateInstance(CLSID_TsPropsBldr);
		CheckHr(qtpb->GetTextProps(&m_qttpFirst));
	}
	AdjustPreview();

	// Run the dialog.
	DecodeValues(soft, nRtlSoft, false, false);
	DecodeValues(hard, nRtlHard, true, true);
	AfDialogShellPtr qdlgShell;
	qdlgShell.Create();
	StrApp str(kstidStyBullNum);
	if (qdlgShell->CreateDlgShell (this, str.Chars(), hwnd) != kctidOk)
		return false;
	EncodeValues(znew);
	if (znew.ltListType == -1)
		znew.ltListType = FwStyledText::knUnspecified;

	// Store any new values.
	bool fChanged = false;
	ITsPropsBldrPtr qtpb;
	// Now see what changes we have to deal with.
	for (int ittp = 0; ittp < cttp; ittp++)
	{
		ITsTextProps * pttp = vpttp[ittp];
		qtpb = NULL;

		// Either set everything, or don't set anything.
		if (znew.ltListType == FwStyledText::knUnspecified &&
			hard.ltListType == FwStyledText::knUnspecified)
		{
			continue;
		}

		UpdateIntProp(pttp, ktptBulNumScheme, qtpb, hard.ltListType, znew.ltListType,
			ktpvEnum, ktpvEnum, 1, 1);

		if (znew.ltListType == FwStyledText::knUnspecified)
		{

			UpdateIntProp(pttp, ktptBulNumStartAt, qtpb, -1, znew.nEdStartAt,
				ktpvDefault, ktpvDefault, 1, 1);
			StrUni stuEmpty;
			stuEmpty.Clear();
			UpdateStringProp(pttp, ktptBulNumTxtBef, qtpb, stuEmpty, stuEmpty);
			UpdateStringProp(pttp, ktptBulNumTxtAft, qtpb, stuEmpty, stuEmpty);
			UpdateStringProp(pttp, ktptBulNumFontInfo, qtpb, stuEmpty, stuEmpty);
		}
		else
		{
			UpdateIntProp(pttp, ktptBulNumStartAt, qtpb, hard.nEdStartAt, znew.nEdStartAt,
				ktpvDefault, ktpvDefault, 1, 1);
			UpdateStringProp(pttp, ktptBulNumTxtBef, qtpb, hard.stuEdTxtBef, znew.stuEdTxtBef);
			UpdateStringProp(pttp, ktptBulNumTxtAft, qtpb, hard.stuEdTxtAft, znew.stuEdTxtAft);
			UpdateStringProp(pttp, ktptBulNumFontInfo, qtpb, hard.stuFontInfo,
				znew.stuFontInfo);
		}

		int dxmpStdHangIndent = - kdzmpInch / 4;
		int dxmpHangInd = -1;
		int tpvHangInd = -1;
		if (pttp)
			CheckHr(pttp->GetIntPropValues(ktptFirstIndent, &tpvHangInd, &dxmpHangInd));
		if (znew.ltListType == kltNotAList || znew.ltListType == FwStyledText::knUnspecified)
		{
			// If the indentation is the standard hanging indentation that is set by the
			// toolbar button, assume we want to clear it.
			if (qtpb && tpvHangInd == ktpvMilliPoint && dxmpHangInd == dxmpStdHangIndent)
				CheckHr(qtpb->SetIntPropValues(ktptFirstIndent, -1, -1));
		}
		else
		{
			// If no indentation specified, set it to hanging.
			if (qtpb && (dxmpHangInd == 0 || dxmpHangInd == -1))
			{
				CheckHr(qtpb->SetIntPropValues(ktptFirstIndent, ktpvMilliPoint,
					dxmpStdHangIndent));
			}
		}

		ITsTextPropsPtr qttpNew;
		if (qtpb) // If any changes, we now have a props builder with new value(s)
		{
			CheckHr(qtpb->GetTextProps (&qttpNew));
			fChanged = true;
		}
		vpttp[ittp] = qttpNew;

		// Reinstate to force the first ttp's start at to be the one we got from the dialog,
		// and all the others to be missing. At present we prefer to make all the same,
		// because this allows an explicit list to override a style. The views code
		// is now smart enough to increment when it sees several paragraphs with the
		// same explicit start-at value.
		//xnew.nEdStartAt = knNinch; // after first pass force all to Ninch.
	}
	return fChanged;
}

/*----------------------------------------------------------------------------------------------
	Encode dialog values into stored values
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::EncodeValues(CPropBulNum & prop)
{
	prop.nEdStartAt = 1;
	switch (m_ltListType)
	{
	case kltNotAList:
		prop.ltListType = kvbnNone;
		break;
	case kltBullet:
		prop.ltListType = kvbnBulletBase + m_iCbBullet;
		break;
	case kltNumber:
		prop.ltListType = kvbnNumberBase + m_iCbNumber;
		prop.nEdStartAt = m_fCxStartAt ? m_nEdStartAt : knNinch;
		break;
	case kltUnspecified:
	default:
		prop.ltListType = (ListType)-1;
		break;
	}
	prop.stuEdTxtBef = m_stuEdTxtBef;
	prop.stuEdTxtAft = m_stuEdTxtAft;
	SmartBstr sbstr;
	CheckHr(m_qttpFirst->GetStrPropValue(ktptBulNumFontInfo, &sbstr));
	prop.stuFontInfo.Assign(sbstr.Chars(), sbstr.Length());
}

/*----------------------------------------------------------------------------------------------
	Decode stored values into values needed by dialog
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::DecodeValues(CPropBulNum & prop, int nRtl, bool fExplicit, bool fFillCtls)
{
	unsigned int & iCbBullet = (fExplicit) ? m_iCbBullet : m_iCbBulletI;
	unsigned int & iCbNumber = (fExplicit) ? m_iCbNumber : m_iCbNumberI;
	bool & fCxStartAt = (fExplicit) ? m_fCxStartAt : m_fCxStartAtI;
	int & nEdStartAt = (fExplicit) ? m_nEdStartAt : m_nEdStartAtI;
	StrUni * pstuEdTxtBef = (fExplicit) ? &m_stuEdTxtBef : &m_stuEdTxtBefI;
	StrUni * pstuEdTxtAft = (fExplicit) ? &m_stuEdTxtAft : &m_stuEdTxtAftI;
	StrUni * pstuFontInfo = (fExplicit) ? &m_stuFontInfo : &m_stuFontInfoI;

	StrAppBuf str;
	ListType lt = kltNotAList;	// Will be parameter for SetListType();
	// To the extent possible, remember the previous settings of the combo-boxes.
	if (iCbBullet <= 0 || iCbBullet > kvbnBulletMax - kvbnBulletBase)
		iCbBullet = 1;
	if (iCbNumber < kno1 || iCbNumber > knoLim)
		iCbNumber = kno1;
	fCxStartAt = false;

	m_nRtl = nRtl;

	if (prop.ltListType >= kvbnBulletBase && prop.ltListType < kvbnBulletMax)
	{
		Assert(!fExplicit || m_xEorI == kxExplicit);
		lt = kltBullet;
		iCbBullet = prop.ltListType - kvbnBulletBase;
	}
	else if (prop.ltListType >= kvbnNumberBase && prop.ltListType < kvbnNumberMax)
	{
		Assert(!fExplicit || m_xEorI == kxExplicit);
		lt = kltNumber;
		iCbNumber = prop.ltListType - kvbnNumberBase;
		nEdStartAt = prop.nEdStartAt;
		fCxStartAt = ((uint)nEdStartAt != (uint)knNinch);
	}
	else if (fExplicit && m_xEorI == kxInherited)
	{
		lt = kltUnspecified;
	}
	*pstuEdTxtBef = prop.stuEdTxtBef;
	*pstuEdTxtAft = prop.stuEdTxtAft;
	*pstuFontInfo = prop.stuFontInfo;
	// Derive further values from retrieved values
	m_nStartAtMin = kStartAtMin - (ICbNumber() == kno1 || ICbNumber() == kno01);
	str.Clear();
	FormatListNumber(m_nStartAtMin, nEdStartAt, 0, str);
	m_strEdStartAt.Assign(str.Chars());
	m_icchEdStartAtSelMin = 0;
	m_icchEdStartAtSelLim = m_strEdStartAt.Length();

	switch (m_iCbNumber)
	{
		case knoA:
		case knoa:
			m_nStartAtMax = 780;
			break;
		default:
			m_nStartAtMax = 14999;
			break;
	}

	if (fFillCtls)
	{
		SM2ID(kctidFbnCbBullet, CB_SETCURSEL, ICbBullet(), 0);
		SM2ID(kctidFbnCbNumber, CB_SETCURSEL, ICbNumber(), 0);

		SetListType(lt);

		// Do the following regardless of the scheme, just to make things look as
		// consistent as possible.
		SetEdStartAt(nEdStartAt, false);	// Number was not typed in, so false here.
		// Text before and after.

		StrApp str = StuEdTxtBef();
		SM2ID(kctidFbnEdTxtBef, WM_SETTEXT, 0, str.Chars());

		str = StuEdTxtAft();
		SM2ID(kctidFbnEdTxtAft, WM_SETTEXT, 0, str.Chars());
		AdjustPreview();

		if (m_hwnd)
			::InvalidateRect(m_hwnd, NULL, false);
	}
	else
	{
		m_ltListTypeI = lt;
	}
}

/*----------------------------------------------------------------------------------------------
	Update Integer Property
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::UpdateIntProp(
	ITsTextProps * pttp,		// the old text properties
	int tpt,					// the identifier of the integer property
	ITsPropsBldrPtr & qtpb,		// builder for any new text properties
	int nOld,					// the old value
	int nNew,					// the new value
	int nVarOld,				// the old variation
	int nVarNew,				// the new variation
	int nMul, int nDiv)			// scaling to be applied
{
	AssertPtrN (pttp);
	if (nOld == nNew && nVarOld == nVarNew)
		return;					// no change, do nothing
	int nCur = nNew;
	if (nVarNew == ktpvMilliPoint && nNew != FwStyledText::knUnspecified)
		nCur = MulDiv(nNew, nMul, nDiv);
	int nVar = -1;				// if pttp null, same as if property not found
	int nVal = -1;
	HRESULT hr = S_FALSE;
	if (pttp)
		CheckHr(hr = pttp->GetIntPropValues (tpt, &nVar, &nVal));
	if (hr == S_FALSE && nCur == FwStyledText::knUnspecified)
		return;					// it was and is unspecified, do nothing.
	if (nVar == nVarNew && nVal == nCur)
		return;					// ttp already has the correct value, do nothing.
	if (!qtpb)
	{							// if no builder yet, make one
		if (pttp)
			CheckHr(pttp->GetBldr(&qtpb));
		else
			qtpb.CreateInstance(CLSID_TsPropsBldr);
	}
	if (nCur == FwStyledText::knUnspecified)
		CheckHr(qtpb->SetIntPropValues(tpt, -1, -1)); // new property is "inherited"
	else
		CheckHr(qtpb->SetIntPropValues(tpt, nVarNew, nCur));
}

/*----------------------------------------------------------------------------------------------
	Update String Property
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::UpdateStringProp(
	ITsTextProps * pttp,		// text properties
	int tpt,					// storage id of the string property
	ITsPropsBldrPtr & ptpb,		// property builder (new or NULL)
	StrUni &stuOld,				// old string
	StrUni &stuNew)				// new string
{
	// By removing these two lines, this method has the effect that even if nothing was changed,
	// we will enforce the final state of the dialog on the selected paragraphs. This means
	// that any controls that were empty due to a conflict will have that emptiness imposed on
	// the data.
//	if (stuOld == stuNew)
//		return; // no change, do nothing

	if (!ptpb)
		CheckHr(pttp->GetBldr (&ptpb)); // must allocate new builder
	SmartBstr sbstr(stuNew.Chars(), stuNew.Length()); // data may be binary, must use length
	Assert(sbstr.Length() == stuNew.Length());
	CheckHr(ptpb->SetStrPropValue (tpt, sbstr)); // set the new property
}

/*----------------------------------------------------------------------------------------------
	Something changed that may affect the preview window. Figure the new TsTextProps that
	should be used to display it, and update it.
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::AdjustPreview()
{
	ITsPropsBldrPtr qtpb;
	if (!m_qttpFirst)
	{
		// In the normal dialog, m_qttpFirst should be initialized from the view, but for
		// the styles dialog, it must be initialized from the styles properties.
		ITsPropsBldrPtr qtpb;
		qtpb.CreateInstance(CLSID_TsPropsBldr);
		SmartBstr sbstr;
		StuFontInfo().GetBstr(&sbstr);
		CheckHr(qtpb->SetStrPropValue(ktptBulNumFontInfo, sbstr));
		CheckHr(qtpb->GetTextProps(&m_qttpFirst));
	}
	CheckHr(m_qttpFirst->GetBldr(&qtpb));

	int nRtl = m_nRtl;
	if (nRtl == FwStyledText::knConflicting || nRtl == FwStyledText::knUnspecified)
		nRtl = m_fOuterRtl;
	CheckHr(qtpb->SetIntPropValues(ktptRightToLeft, ktpvEnum, nRtl));

	switch (LtListType())
	{
	case kltBullet:
		CheckHr(qtpb->SetIntPropValues(ktptBulNumScheme, ktpvEnum,
			kvbnBulletBase + ICbBullet()));
		CheckHr(qtpb->SetIntPropValues(ktptBulNumStartAt, -1, -1));
		CheckHr(qtpb->SetStrPropValue(ktptBulNumTxtBef, NULL));
		CheckHr(qtpb->SetStrPropValue(ktptBulNumTxtAft, NULL));
		CheckHr(qtpb->SetStrPropValue(ktptBulNumFontInfo, StuFontInfo().Bstr()));
		break;
	case kltNumber:
		CheckHr(qtpb->SetIntPropValues(ktptBulNumScheme, ktpvEnum,
			kvbnNumberBase + ICbNumber()));
		CheckHr(qtpb->SetIntPropValues(ktptBulNumStartAt, ktpvDefault, NEdStartAt()));
		CheckHr(qtpb->SetStrPropValue(ktptBulNumTxtBef, StuEdTxtBef().Bstr()));
		CheckHr(qtpb->SetStrPropValue(ktptBulNumTxtAft, StuEdTxtAft().Bstr()));
		CheckHr(qtpb->SetStrPropValue(ktptBulNumFontInfo, StuFontInfo().Bstr()));
		break;
	default: // not a list
		CheckHr(qtpb->SetIntPropValues(ktptBulNumScheme, ktpvEnum, kvbnNone));
		CheckHr(qtpb->SetIntPropValues(ktptBulNumStartAt, -1, -1));
		CheckHr(qtpb->SetStrPropValue(ktptBulNumTxtBef, NULL));
		CheckHr(qtpb->SetStrPropValue(ktptBulNumTxtAft, NULL));
		break;
	}
	CheckHr(qtpb->SetIntPropValues(ktptMarginTop, ktpvMilliPoint, 6000));
	CheckHr(qtpb->GetTextProps(&m_qttpFirst));
	CheckHr(qtpb->SetIntPropValues(ktptBulNumStartAt, -1, -1));
	CheckHr(qtpb->GetTextProps(&m_qttpOther));
	if (m_qfbnp)
		m_qfbnp->SetProps(m_qttpFirst, m_qttpOther);

	if (m_hwnd) // Don't redraw if the window isn't open yet.
		::InvalidateRect(m_hwnd, NULL, true);
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog window by creating a child window for the preview.
----------------------------------------------------------------------------------------------*/
void FmtBulNumDlg::PostAttach(void)
{
	SuperClass::PostAttach();

	WndCreateStruct wcs;
	wcs.InitChild(_T("AfVwWnd"), m_hwnd, 0);
	wcs.style |=  WS_VISIBLE;
	// Since the preview is placed inside the owner draw button, it must not clip siblings or
	// it won't show up at all.
	wcs.style &= ~WS_CLIPSIBLINGS;
	Rect rcBounds; // where to display it in parent: on top of the dummy user-defined control
	Rect rcMyBounds;

	// Get rectangle for child window, in pixels relative to parent
	::GetWindowRect(::GetDlgItem(m_hwnd, kctidFbnPreview), &rcBounds);
	::GetWindowRect(m_hwnd, &rcMyBounds);
	rcBounds.Offset(-rcMyBounds.left, -rcMyBounds.top);
	// Reduce the size of the view to exclude the border.
	SIZE sizeMargins = { ::GetSystemMetrics(SM_CXEDGE), ::GetSystemMetrics(SM_CYEDGE) };

	rcBounds.left += sizeMargins.cx;
	rcBounds.right -= sizeMargins.cx;
	rcBounds.top += sizeMargins.cy;
	rcBounds.bottom -= sizeMargins.cy;

	wcs.SetRect(rcBounds);

	m_qfbnp.Attach(NewObj FmtBulNumPreview);
	m_qfbnp->SetLgWritingSystemFactory(m_qwsf);

	m_qfbnp->CreateHwnd(wcs);

}

/*----------------------------------------------------------------------------------------------
	Make the root box.
----------------------------------------------------------------------------------------------*/
void FmtBulNumPreview::MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
	IVwRootBox ** pprootb)
{
	AssertPtrN(pwsf);
	*pprootb = NULL;

	IVwRootBoxPtr qrootb;
	qrootb.CreateInstance(CLSID_VwRootBox);
	CheckHr(qrootb->SetSite(this));

	// Make an arbitrary ID for a dummy root object
	HVO hvo = 1;
	int frag = 1;

	// Set up a new view constructor with the given record specs.
	m_qfbnpvc.Attach(NewObj FmtBulNumPreviewVc());
	m_qfbnpvc->SetProps(m_pttpFirst, m_pttpOther);

	// Make a trivial, empty data access object.
	ISilDataAccessPtr qsda;
	qsda.CreateInstance(CLSID_VwCacheDa);
	Assert(qsda);

	IVwViewConstructor * pvvc = m_qfbnpvc;

	if (pwsf)
		CheckHr(qsda->putref_WritingSystemFactory(pwsf));
	CheckHr(qrootb->putref_DataAccess(qsda));
	// Pass phony vectors of one item each (the last argument) by taking the address of
	// each single item.
	CheckHr(qrootb->SetRootObjects(&hvo, &pvvc, &frag, NULL, 1));

	*pprootb = qrootb.Detach();
}

/*----------------------------------------------------------------------------------------------
	Set the properties that define the preview.
	If it is already showing, update it.
----------------------------------------------------------------------------------------------*/
void FmtBulNumPreview::SetProps(ITsTextProps * pttpFirst, ITsTextProps * pttpOther)
{
	m_pttpFirst = pttpFirst;
	m_pttpOther = pttpOther;
	if (m_qfbnpvc)
	{
		// we're up and running: update the view
		m_qfbnpvc->SetProps(pttpFirst, pttpOther);
		CheckHr(m_qrootb->Reconstruct());
	}
}

static DummyFactory g_fact(_T("SIL.AppCore.FmtBulNumPreviewVc"));

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them. Construct
	the complete contents of the preview (it doesn't have any interesting internal structure).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FmtBulNumPreviewVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvwenv);

	int dmpFakeHeight = 5000; // height for the "fake text" rectangles
	// (width is -1, meaning "use the rest of the line")

	// Make a "context" paragraph before the numbering starts.
	CheckHr(pvwenv->put_IntProperty(ktptSpaceBefore, ktpvMilliPoint, 10000));
	CheckHr(pvwenv->OpenParagraph());
	CheckHr(pvwenv->AddSimpleRect(kclrLightGray, -1, dmpFakeHeight, 0));
	CheckHr(pvwenv->CloseParagraph());

	// Make the first numbered paragraph.
	// (It's not much use if we don't have properties, but that may happen while we're starting
	// up so we need to cover it.)
	if (m_pttpFirst)
		CheckHr(pvwenv->put_Props(m_pttpFirst));
	CheckHr(pvwenv->OpenParagraph());
	CheckHr(pvwenv->AddSimpleRect(kclrLightGray, -1, dmpFakeHeight, 0));
	CheckHr(pvwenv->CloseParagraph());

	// Make two more numbered paragraphs.
	if (m_pttpOther)
		CheckHr(pvwenv->put_Props(m_pttpOther));
	CheckHr(pvwenv->OpenParagraph());
	CheckHr(pvwenv->AddSimpleRect(kclrLightGray, -1, dmpFakeHeight, 0));
	CheckHr(pvwenv->CloseParagraph());
	if (m_pttpOther)
		CheckHr(pvwenv->put_Props(m_pttpOther));
	CheckHr(pvwenv->OpenParagraph());
	CheckHr(pvwenv->AddSimpleRect(kclrLightGray, -1, dmpFakeHeight, 0));
	CheckHr(pvwenv->CloseParagraph());

	// Make a "context" paragraph after the numbering ends.
	CheckHr(pvwenv->put_IntProperty(ktptSpaceBefore, ktpvMilliPoint, 6000));
	CheckHr(pvwenv->OpenParagraph());
	CheckHr(pvwenv->AddSimpleRect(kclrLightGray, -1, dmpFakeHeight, 0));
	CheckHr(pvwenv->CloseParagraph());

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwViewConstructor)
}
