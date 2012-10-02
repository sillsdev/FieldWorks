/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FmtGenDlg.cpp
Responsibility: Larry Waswick
Last reviewed: Not yet.

Description:
	Implementation of the Format General Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

// These two contexts must match the values of ContextValues.Internal and
// ContextValues.InternalMappable, respectively, in FDO\StStyle.cs.
const int knContextInternal = 5;
const int knContextInternalMappable = 6;

//:>********************************************************************************************
//:>	Methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get the final values for the dialog controls, after the dialog has been closed.
----------------------------------------------------------------------------------------------*/
void FmtGenDlg::GetDialogValues(StyleInfo & styi, bool & fBasedOnChanged)
{
	StrUni stuName = GetName();
	StrApp strName = stuName;
	achar rgcha[1024];
	Vector<achar> vch;
	achar * pszT;
	memcpy(rgcha, strName.Chars(), strName.Length() * isizeof(achar));
	if (stuName != styi.m_stuName)
	{
		StrApp strOldName(styi.m_stuName);
		StrApp strNewName(stuName);
		if (!m_pafsd->SetName(styi, stuName))
		{
			// Error: restore the old name.
			::SetDlgItemText(m_hwnd, kctidFgEdName, strOldName.Chars());
		}
		// Otherwise update the comboboxes for BasedOn and Next.
		// This allows the later code which reads from them to work correctly,
		// so it is worth doing even here in a read routine.
		// Also update the edit box to handle stripping of leading/trailing blanks.
		else
		{
			::SetDlgItemText(m_hwnd, kctidFgEdName, strNewName.Chars());
			UpdateComboboxes(strOldName, strNewName);
		}
	}

	int iitem = ::SendMessage(m_hwndBasedOn, CB_GETCURSEL, 0,0);
	// <0 means the item is blank (e.g., Normal is not based on anything).
	// But if we pass -1 to CB_GETLBTEXT, we get the text of the first item.
	// This can change Normal to be based on Normal, with bad consequences.
	//
	if (iitem < 0)
	{
		pszT = _T("");
	}
	else
	{
		int cch = ::SendMessage(m_hwndBasedOn, CB_GETLBTEXTLEN, (WPARAM)iitem, 0);
		if (cch < 1024)
		{
			pszT = rgcha;
		}
		else
		{
			vch.Resize(cch + 1);
			pszT = vch.Begin();
		}
		cch = ::SendMessage(m_hwndBasedOn, CB_GETLBTEXT, iitem, (long)pszT);
		if (cch < 0)
			pszT = _T("");
	}
	stuName = pszT;
	HVO hvoBasedOn = m_pafsd->GetHvoOfStyleNamed(stuName);
	if (styi.m_hvoBasedOn != hvoBasedOn)
	{
		m_pafsd->SetBasedOn(styi, hvoBasedOn); // Sets m_fDirty.

		// Note that this is initialised to false in AfStylesDlg::UpdateTabCtrl.
		fBasedOnChanged = true;
	}

	iitem = ::SendMessage(m_hwndNext, CB_GETCURSEL, 0,0);
	if (iitem < 0)
	{
		pszT = _T("");
	}
	else
	{
		int cch = ::SendMessage(m_hwndNext, CB_GETLBTEXTLEN, (WPARAM)iitem, 0);
		if (cch < 1024)
		{
			pszT = rgcha;
		}
		else
		{
			vch.Resize(cch + 1);
			pszT = vch.Begin();
		}
		cch = ::SendMessage(m_hwndNext, CB_GETLBTEXT, iitem, (long)pszT);
		if (cch < 0)
			pszT = _T("");
	}
	stuName = pszT;
	HVO hvoNext = m_pafsd->GetHvoOfStyleNamed(stuName);
	if ((styi.m_hvoNext != hvoNext) && (styi.m_st != kstCharacter))
	{
		m_pafsd->SetNext(styi, hvoNext); // Sets m_fDirty.
	}
} //:> FmtGenDlg::GetDialogValues.

/*----------------------------------------------------------------------------------------------
	The app framework calls this to initialize the dialog. All one-time initialization should
	be done here (that is, all controls have been created and have valid hwnd's, but they
	need initial values.)
----------------------------------------------------------------------------------------------*/
bool FmtGenDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Get the window handles for our controls.
	m_hwndName = ::GetDlgItem(m_hwnd, kctidFgEdName); // Name edit control
	m_hwndType = ::GetDlgItem(m_hwnd, kctidFgStyleType); // Type static text.
	m_hwndBasedOn = ::GetDlgItem(m_hwnd, kctidFgCbBasedOn); // Based On combobox.
	m_hwndNext = ::GetDlgItem(m_hwnd, kctidFgCbParaNextStyle); // Next combobox.
//	m_hwndShortcut = ::GetDlgItem(m_hwnd, kctidFgEdShortcut); // Shortcut edit control.
	//:> ENHANCE LarryW: Implement shortcut.
	m_hwndDescription = ::GetDlgItem(m_hwnd, kctidFgDescription); // Description static text.

	// Fix static controls that contain shortcuts so the shortcuts won't do anything if
	// the controls are disabled.
	AfStaticText::FixEnabling(m_hwnd, kctidFgEdName);
	AfStaticText::FixEnabling(m_hwnd, kctidFgCbBasedOn);
	AfStaticText::FixEnabling(m_hwnd, kctidFgCbParaNextStyle);
//	AfStaticText::FixEnabling(m_hwnd, kctidFgEdShortcut);

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
			(pfn)(m_hwndName, L"", L"");
			(pfn)(m_hwndType, L"", L"");
			(pfn)(m_hwndBasedOn, L"", L"");
			(pfn)(m_hwndNext, L"", L"");
			(pfn)(m_hwndDescription, L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidFgEdShortcut), L"", L"");
		}

		::FreeLibrary(hmod);
	}

	return true;
}

// Add the part to the description (preceded by a comma if it isn't the first thing added).
void AppendDescPart(StrApp & strDesc, const achar * pszPart, bool & fFirst)
{
	if (*pszPart == 0)
		return;
	if (fFirst)
		fFirst = false; // Subsequent calls will add a comma.
	else
		strDesc.Append (",");
	strDesc.Append(pszPart);
}

void AppendDescPart(StrApp & strDesc, StrUni stuPart, bool & fFirst)
{
	AppendDescPart(strDesc, StrApp(stuPart).Chars(), fFirst);
}

void AppendDescPart(StrApp & strDesc, int stidPart, bool & fFirst)
{
	AppendDescPart(strDesc, StrApp(stidPart).Chars(), fFirst);
}

void AppendDescPart(StrApp & strDesc, StrAppBuf strbPart, bool & fFirst)
{
	AppendDescPart(strDesc, strbPart.Chars(), fFirst);
}

void AppendDescPart(StrApp & strDesc, BSTR bstr, bool & fFirst)
{
	AppendDescPart(strDesc, StrApp(bstr).Chars(), fFirst);
}

// Append to strDesc appropriate info about underline if any.
void AppendUnderlineInfo(StrApp & strDesc, COLORREF clrUnder, int unt, bool & fFirst)
{
	StrApp strColor;
	if (clrUnder != (COLORREF)knNinch)
		strColor.Load(g_ct.GetColorRid(g_ct.GetIndexFromColor(clrUnder)));
	StrApp strFmt;
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
		return;
	}
	StrApp strT;
	strT.Format(strFmt.Chars(), strColor.Chars());
	AppendDescPart(strDesc, strT, fFirst);
}

// Append something appropriate to strDesc for foreground and background color, depending on whether
// either or both is unspecified (knNinch).
void AppendTextIs(StrApp & strDesc, COLORREF clrFore, COLORREF clrBack, bool & fFirst)
{
	StrApp strT;
	StrApp strT2;
	StrApp strT3;
	if (clrBack == (COLORREF)kclrTransparent)
		clrBack = (COLORREF)knNinch; // treat transparent as no background color.
	// If we have a foreground color but no background display something like "Text is red"
	if (clrFore != knNinch && clrBack == knNinch)
	{
		strT2.Load(g_ct.GetColorRid(g_ct.GetIndexFromColor(clrFore)));
		strT3.Load(kstidTextIsFmt);
		strT.Format(strT3.Chars(), strT2.Chars());
	}
	else if (clrBack != knNinch)
	{
		strT2.Load(g_ct.GetColorRid(g_ct.GetIndexFromColor(clrBack)));
		strT3.Load(kstidTextOnFmt);
		StrApp strT4;
		if (clrFore != knNinch)
			strT4.Load(g_ct.GetColorRid(g_ct.GetIndexFromColor(clrFore)));
		strT.Format(strT3.Chars(), strT4.Chars(), strT2.Chars());
	}
	AppendDescPart(strDesc, strT, fFirst);
}

void AppendOffsetInfo(StrApp & strDesc, int mpOffset, bool & fFirst)
{
	if (mpOffset && mpOffset != knNinch)
	{
		StrApp strFmt;
		if (mpOffset < 0)
			strFmt.Load(kstidLoweredFmt);
		else
			strFmt.Load(kstidRaisedFmt);
		StrApp strAmt;
		StrAppBuf strb;
		AfUtil::MakeMsrStr (abs(mpOffset) , knpt, &strb);
		StrApp strT;
		strT.Format(strFmt.Chars(), strb.Chars());
		AppendDescPart(strDesc, strT, fFirst);
	}
}

// Read a value from a TsTextProps; if not present, return Ninch, if present return the value.
int ReadValOrNinch(ITsTextProps * pttp, int tpt)
{
	int var, val;
	CheckHr(pttp->GetIntPropValues(tpt, &var, &val));
	if (var == -1)
		return knNinch;
	else
		return val;
}

// Add the indicated resource string if fAdd is true
void AddIf(StrApp & strDesc, bool fAdd, int stid, bool & fFirst)
{
	if (!fAdd)
		return;
	AppendDescPart(strDesc, stid, fFirst);
}

struct Keyval
{
	int key;
	int val;
};


// Add to strDesc the item from rgkeyvals.val where rgkeyvals.key matches key;
// or nothing if no key matches. (rgkeyvals is terminated by a val of 0).
void AddFromList(StrApp & strDesc, int key, const Keyval rgkeyvals[], bool & fFirst)
{
	for (int i = 0; rgkeyvals[i].val != 0; i++)
	{
		if (rgkeyvals[i].key == key)
		{
			AppendDescPart(strDesc, rgkeyvals[i].val, fFirst);
			return;
		}
	}
}

// Append information about one border, if it is defined
void AppendBorderInfo(StrApp & strDesc, int mp, int stid, bool & fFirst)
{
	if (mp == knNinch)
		return;
	StrAppBuf strb;
	AfUtil::MakeMsrStr (mp , knpt, &strb);
	StrApp strFmt(stid);
	StrApp strT;
	strT.Format(strFmt.Chars(), strb.Chars());
	AppendDescPart(strDesc, strT, fFirst);
}

// Append a measurement, never with preceding comma. stid contains a %s which is replaced
// with the nVal (initially mp) converted to the appropriate display units.
void AppendMeasurement(StrApp & strDesc, int nVal, MsrSysType nMsrSys, int stid)
{
	if (nVal == knNinch)
		return;
	StrAppBuf strb;
	AfUtil::MakeMsrStr(nVal , nMsrSys, &strb);
	StrApp strFmt(stid);
	strDesc.FormatAppend(strFmt.Chars(), strb.Chars());
}

/*----------------------------------------------------------------------------------------------
	Set the values for the dialog controls based on the style styi.
	@param vwsProj Union of the current Vernacular and Analysis encodings for the project.
----------------------------------------------------------------------------------------------*/
void FmtGenDlg::SetDialogValues(StyleInfo & styi, Vector<int> & vwsProj)
{
	// During SetDialogValues we never need to handle loss of focus in the name control,
	// because if anything needed to be done about updating that, it was done during
	// GetDialogValues, which is always called before SetDialogValues. We need to suppress it
	// because during this call the current style index and the current value of the control
	// may not agree: the purpose of this method is to update the control from the new style.
	// The normal loss-of-focus code is trying to synchronize in the opposite direction, and
	// can cause problems, for example, when we are disabling the control. For example:
	//  - Disabling the window causes it to lose focus if it previously had focus.
	//  - There is a kill focus event that tries to make the contents of the box agree with
	//		the name of the current style.
	//	- when this method is called, the old style is still the current one, so if we
	//		already changed the text in the box, we wind up trying to rename the old style
	//		to the name of the 'provided' style and produce a name conflict error.

	try
	{
		m_vwsProj = vwsProj;

		StrApp strDesc;
		m_fSuppressLossOfFocus = true;
		StrApp strName = styi.m_stuName;
		StrApp strTemp; // Temporary string.
		bool fProtectedStyle = m_pafsd->IsStyleProtected(styi.m_stuName);

		// Name edit box.
		::SetDlgItemText(m_hwnd, kctidFgEdName, strName.Chars());
		// Diable the control for styles originally provided by FieldWorks.
		::EnableWindow(m_hwndName, !fProtectedStyle);

		// Style type ltext.
		switch (styi.m_st)
		{
		case kstParagraph:
			strTemp.Load(kstidParagraph);
			break;
		case kstCharacter:
			strTemp.Load(kstidCharacter);
			break;
		default:
			Assert(false); // Should not happen.
		}
		::SetWindowText(m_hwndType, strTemp.Chars());

		// Update the "next" and "basedOn" comboboxes
		LoadNextStyleCombobox(styi);
		SetNextStyleComboboxValue(styi);
		LoadBasedOnStyleCombobox(styi);
		SetBasedOnStyleComboboxValue(styi);

		// ENHANCE LarryW(JohnT): When shortcut is implemented initialize the value instead.
		::EnableWindow(m_hwndShortcut, false); // Disables it.

		// **************************************
		m_vesi.Clear();
		strDesc = m_pafsd->GetNameOfStyle(styi.m_hvoBasedOn);
		strDesc.Append(" + ");

		ITsTextProps * pttp = styi.m_qttp;
		if (pttp)
		{
			StrApp strT;
			StrAppBuf strb; // a temp, used for making strings with units

			// Add default font info
			bool fFirstPart = true;
			SmartBstr sbstr;
			CheckHr(pttp->GetStrPropValue(ktptFontFamily, &sbstr));
			StrUni stuFont(sbstr.Chars());
			AppendDescPart(strDesc, FwStyledText::FontStringMarkupToUi(false, stuFont), fFirstPart);
			int val, var;
			CheckHr(pttp->GetIntPropValues(ktptFontSize, &var, &val));
			if (var != -1)
			{
				AfUtil::MakeMsrStr (val , knpt, &strb);
				AppendDescPart(strDesc, strb, fFirstPart);
			}
			CheckHr(pttp->GetIntPropValues(ktptBold, &var, &val));
			if (val == kttvForceOn || val == kttvInvert)
				AppendDescPart(strDesc, kstidBold, fFirstPart);
			CheckHr(pttp->GetIntPropValues(ktptItalic, &var, &val));
			if (val == kttvForceOn || val == kttvInvert)
				AppendDescPart(strDesc, kstidItalic, fFirstPart);
			CheckHr(pttp->GetIntPropValues(ktptSuperscript, &var, &val));
			if (val == kssvSuper)
				AppendDescPart(strDesc, kstidFfdSuperscript, fFirstPart);
			else if (val == kssvSub)
				AppendDescPart(strDesc, kstidFfdSubscript, fFirstPart);

			AppendTextIs(strDesc, (COLORREF)ReadValOrNinch(pttp, ktptForeColor),
				(COLORREF)ReadValOrNinch(pttp, ktptBackColor), fFirstPart);

			AppendUnderlineInfo(strDesc, (COLORREF)ReadValOrNinch(pttp, ktptUnderColor),
				ReadValOrNinch(pttp, ktptUnderline), fFirstPart);

			AppendOffsetInfo(strDesc, ReadValOrNinch(pttp, ktptOffset), fFirstPart);

			// Add info about other tabs
			int nDir = ReadValOrNinch(pttp, ktptRightToLeft);
			AddIf(strDesc, nDir == 0, kstidLeftToRight, fFirstPart);
			AddIf(strDesc, nDir == 1, kstidRightToLeft, fFirstPart);

			static const Keyval rgkeyvals[] =
			{
				{ktalLeading, kstidFpAlignLead},
				{ktalLeft, kstidFpAlignLeft},
				{ktalCenter, kstidFpAlignCenter},
				{ktalRight, kstidFpAlignRight},
				{ktalTrailing, kstidFpAlignTrail},
				{ktalJustify, kstidFpAlignJustify}, //TODO: support when in use.
				{0, 0}
			};
			AddFromList(strDesc, ReadValOrNinch(pttp, ktptAlign), rgkeyvals, fFirstPart);

			int nLeadIndent = ReadValOrNinch(pttp, ktptLeadingIndent);
			int nTrailIndent = ReadValOrNinch(pttp, ktptTrailingIndent);
			int nFirstIndent = ReadValOrNinch(pttp, ktptFirstIndent);
			if (nLeadIndent != knNinch || nTrailIndent != knNinch || nFirstIndent != knNinch)
			{
				AppendDescPart(strDesc, kstidIndentColon, fFirstPart);
				AppendMeasurement(strDesc, nLeadIndent, m_nMsrSys, kstidLeadingFmt);
				if (nFirstIndent != knNinch)
				{
					if (nFirstIndent < 0)
						AppendMeasurement(strDesc, -nFirstIndent, m_nMsrSys, kstidHangingFmt);
					else
						AppendMeasurement(strDesc, nFirstIndent, m_nMsrSys, kstidFirstLineFmt);
				}
				AppendMeasurement(strDesc, nTrailIndent, m_nMsrSys, kstidTrailingFmt);
			}
			// line spacing
			CheckHr(pttp->GetIntPropValues(ktptLineHeight, &var, &val));
			StrApp strSpacing;
			StrApp strFmt;
			if (var == ktpvMilliPoint)
			{
				if (val < 0)
					strFmt.Load(kstidFpLsExactFmt);
				else
					strFmt.Load(kstidFpLsAtLeastFmt);
				StrAppBuf strb;
				AfUtil::MakeMsrStr (val , knpt, &strb);
				strSpacing.Format(strFmt.Chars(), strb.Chars());
			}
			else if (var == ktpvRelative)
			{
				if (val >= kdenTextPropRel * 2)
					strSpacing.Load(kstidFpLsDouble);
				else if (val >= kdenTextPropRel * 3 / 2)
					strSpacing.Load(kstidFpLs15Lines);
				else
					strSpacing.Load(kstidFpLsSingle);
			}
			if (strSpacing.Length() != 0)
			{
				strFmt.Load(kstidLineSpacingFmt);
				strT.Format(strFmt.Chars(), strSpacing.Chars());
				AppendDescPart(strDesc, strT, fFirstPart);
			}

			int mpBefore = ReadValOrNinch(pttp, ktptSpaceBefore);
			int mpAfter = ReadValOrNinch(pttp, ktptSpaceAfter);
			if (mpBefore != knNinch || mpAfter != knNinch)
			{
				AppendDescPart(strDesc, kstidSpace, fFirstPart);
				AppendMeasurement(strDesc, mpBefore, knpt, kstidBeforeFmt);
				AppendMeasurement(strDesc, mpAfter, knpt, kstidAfterFmt);
			}

			int bulnum = ReadValOrNinch(pttp, ktptBulNumScheme);
			AddIf(strDesc, bulnum >= kvbnBulletBase && bulnum < kvbnBulletMax,
				kstidBulleted, fFirstPart);
			AddIf(strDesc, bulnum >= kvbnNumberBase && bulnum < kvbnNumberMax,
				kstidNumbered, fFirstPart);

			int mpBTop = ReadValOrNinch(pttp, ktptBorderTop);
			int mpBB = ReadValOrNinch(pttp, ktptBorderBottom);
			int mpBL = ReadValOrNinch(pttp, ktptBorderLeading);
			int mpBTr = ReadValOrNinch(pttp, ktptBorderTrailing);

			if (mpBTop != knNinch || mpBB != knNinch || mpBL != knNinch || mpBTr != knNinch)
			{
				AppendDescPart(strDesc, kstidBorderColon, fFirstPart);
				int clrBorder = ReadValOrNinch(pttp, ktptBorderColor);
				bool fFirstBorder = true;
				if (clrBorder != knNinch)
				{
					StrApp strClr;
					strClr.Load(g_ct.GetColorRid(g_ct.GetIndexFromColor(clrBorder)));
					strDesc.Append(strClr.Chars());
					fFirstBorder = false;
				}
				AppendBorderInfo(strDesc, mpBTop, kstidTopBdrFmt, fFirstBorder);
				AppendBorderInfo(strDesc, mpBB, kstidBottomBdrFmt, fFirstBorder);
				AppendBorderInfo(strDesc, mpBL, kstidLeadingBdrFmt, fFirstBorder);
				AppendBorderInfo(strDesc, mpBTr, kstidTrailingBdrFmt, fFirstBorder);
			}


			// Add info about writing-system overrides of font info.
			Vector<int> vwsSoFar;


			// Get the appropropriate writing system factory.
			ILgWritingSystemFactoryPtr qwsf;
			AssertPtr(m_pafsd);
			m_pafsd->GetLgWritingSystemFactory(&qwsf);
			AssertPtr(qwsf);
		//-		IWritingSystemPtr qws;

			// Each iteration of this loop processes information about one writing system.
			SmartBstr sbstrCharStyles;
			CheckHr(pttp->GetStrPropValue(kspWsStyle, &sbstrCharStyles));
			if (sbstrCharStyles.Length())
			{
				FwStyledText::DecodeFontPropsString(sbstrCharStyles, m_vesi, vwsSoFar);

				SmartBstr sbstrWs;
				StrApp strWs;
				int wsUser;
				CheckHr(qwsf->get_UserWs(&wsUser));
				SmartBstr sbstrAbbr;
				for (int iesi = 0; iesi < m_vesi.Size(); iesi++)
				{
					WsStyleInfo & esi = m_vesi[iesi];
					if (vwsSoFar[iesi] == 0)
						continue;		// Ignore writing system set to 0.
					fFirstPart = true;
					StrApp strT;
					StrApp strT2;
					strT.Format(_T("\n"));
					// Use the abbreviation in the user ws if it exists.
					// else try for an abbreviation in each ws in m_vwsProj in turn,
					// else use the ICU locale name as a last resort.
					IWritingSystemPtr qws;
					CheckHr(qwsf->GetStrFromWs(vwsSoFar[iesi], &sbstrWs));
					CheckHr(qwsf->get_Engine(sbstrWs, &qws));
					CheckHr(qws->get_Abbr(wsUser, &sbstrAbbr));
					if (sbstrAbbr.Length() == 0)
					{
						for (int iws = 0; iws < m_vwsProj.Size(); ++iws)
						{
							CheckHr(qws->get_Abbr(m_vwsProj[iws], &sbstrAbbr));
							if (sbstrAbbr.Length() != 0)
								break;
						}
					}
					if (sbstrAbbr.Length() == 0)
						strWs.Assign(sbstrWs.Chars(), sbstrWs.Length());
					else
						strWs.Assign(sbstrAbbr.Chars(), sbstrAbbr.Length());
					strT2.Format(_T("%s: "), strWs.Chars());
					if (strT2 == _T("___: "))
						strT2.Load(kctidFgUnknown);
					strT.Append(strT2);
					strDesc.Append (strT);
					AppendDescPart(strDesc,
						FwStyledText::FontStringMarkupToUi(false, esi.m_stuFontFamily),
						fFirstPart);
					if (esi.m_mpSize != knNinch)
					{
						AfUtil::MakeMsrStr (esi.m_mpSize , knpt, &strb);
						AppendDescPart(strDesc, strb, fFirstPart);
					}
					if (esi.m_fBold == kttvForceOn || esi.m_fBold == kttvInvert)
						AppendDescPart(strDesc, kstidBold, fFirstPart);
					if (esi.m_fItalic == kttvForceOn || esi.m_fItalic == kttvInvert)
						AppendDescPart(strDesc, kstidItalic, fFirstPart);
					if (esi.m_ssv == kssvSuper)
						AppendDescPart(strDesc, kstidFfdSuperscript, fFirstPart);
					else if (esi.m_ssv == kssvSub)
						AppendDescPart(strDesc, kstidFfdSubscript, fFirstPart);
					AppendTextIs(strDesc, esi.m_clrFore, esi.m_clrBack, fFirstPart);

					AppendUnderlineInfo(strDesc, esi.m_clrUnder, esi.m_unt, fFirstPart);

					AppendOffsetInfo(strDesc, esi.m_mpOffset, fFirstPart);
				} // 'for' loop
			}
		}
		::SetDlgItemText(m_hwnd, kctidFgDescription, strDesc.Chars());
// **************************************
	}
	catch(...)
	{
		m_fSuppressLossOfFocus = false;
		throw;
	}
	m_fSuppressLossOfFocus = false;
} //:> FmtGenDlg::SetDialogValues.

/*----------------------------------------------------------------------------------------------
	Loads the "next" combobox for the specified style
----------------------------------------------------------------------------------------------*/
void FmtGenDlg::LoadNextStyleCombobox(StyleInfo & styi)
{
	StrApp strTemp; // Temporary string.

	// "Next Style" combobox.
	::SendMessage(m_hwndNext, CB_RESETCONTENT, 0, 0);
	for (int istyi = 0; istyi < m_pafsd->m_vstyi.Size(); istyi++)
	{
		StyleInfo * pstyiTemp = &m_pafsd->m_vstyi[istyi];
		if (pstyiTemp->m_fDeleted)
			continue;
		strTemp = pstyiTemp->m_stuName;
		if (pstyiTemp->m_st == styi.m_st)
			::SendMessage(m_hwndNext, CB_ADDSTRING, 0, (LPARAM)strTemp.Chars());
	}
}

/*----------------------------------------------------------------------------------------------
	Selects the "next" style of the specified style in the "next" combobox
----------------------------------------------------------------------------------------------*/
void FmtGenDlg::SetNextStyleComboboxValue(StyleInfo & styi)
{
	StrApp strTemp; // Temporary string.
	int icombobox;
	bool fProtectedStyle = m_pafsd->IsStyleProtected(styi.m_stuName);
	// Select the "Next Style" values from styi for the combobox.
	if (styi.m_st == kstCharacter)
	{
		icombobox = -1; // Makes it blank.
		::EnableWindow(m_hwndNext, false); // And disables it.
	}
	else
	{
		strTemp = m_pafsd->GetNameOfStyle(styi.m_hvoNext);
		icombobox = ::SendMessage(m_hwndNext, CB_FINDSTRINGEXACT, 0,
			(LPARAM)strTemp.Chars());
		// Disable the control for styles originally provided by FieldWorks.
		::EnableWindow(m_hwndNext, !fProtectedStyle);
	}
	::SendMessage(m_hwndNext, CB_SETCURSEL, icombobox, 0);
}

/*----------------------------------------------------------------------------------------------
	Loads the "basedOn" combox for the specified style
----------------------------------------------------------------------------------------------*/
void FmtGenDlg::LoadBasedOnStyleCombobox(StyleInfo & styi)
{
	StrApp strName = styi.m_stuName;
	StrApp strTemp; // Temporary string.
	// "Based On" combobox.
	::SendMessage(m_hwndBasedOn, CB_RESETCONTENT, 0, 0);
	for (int istyi = 0; istyi < m_pafsd->m_vstyi.Size(); istyi++)
	{
		StyleInfo * pstyiTemp = &m_pafsd->m_vstyi[istyi];
		if (pstyiTemp->m_fDeleted)
			continue;
		if ((pstyiTemp->m_nContext == knContextInternal ||
			pstyiTemp->m_nContext == knContextInternalMappable) && !styi.m_fBuiltIn)
			continue;
		strTemp = pstyiTemp->m_stuName;
		if (pstyiTemp->m_st == styi.m_st)
		{
			// The based-on list should not include any style that is already based on the
			// newly selected one, even indirectly.
			if (strName != strTemp && !m_pafsd->IsBasedOn(pstyiTemp, styi.m_hvoStyle))
				::SendMessage(m_hwndBasedOn, CB_ADDSTRING, 0, (LPARAM)strTemp.Chars());
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Selects the "basedOn" style of the specified style in the "basedOn" combobox
----------------------------------------------------------------------------------------------*/
void FmtGenDlg::SetBasedOnStyleComboboxValue(StyleInfo & styi)
{
	StrApp strTemp; // Temporary string.
	bool fProtectedStyle = m_pafsd->IsStyleProtected(styi.m_stuName);

	// Select the "Based On" value from styi for the combobox.
	strTemp = m_pafsd->GetNameOfStyle(styi.m_hvoBasedOn);
	int icombobox = ::SendMessage(m_hwndBasedOn, CB_FINDSTRINGEXACT, 0,
		(LPARAM)strTemp.Chars());
	::SendMessage(m_hwndBasedOn, CB_SETCURSEL, icombobox, 0);
	// Disable the control for styles originally provided by FieldWorks.
	::EnableWindow(m_hwndBasedOn, !fProtectedStyle);
}

/*----------------------------------------------------------------------------------------------
	The AfStylesDlg calls this when the user edits the name of a style.
----------------------------------------------------------------------------------------------*/
void FmtGenDlg::SetName(StrApp & strNewName)
{
	StrApp strOldName;

	strOldName = GetName(); // Get the name from the editbox.

	// If the name has not changed, there is nothing to do.
	if (strNewName.Equals(strOldName))
		return;

	::SetDlgItemText(m_hwnd, kctidFgEdName, strNewName.Chars());

	UpdateComboboxes(strOldName, strNewName); // Update the names in the comboboxes.
}

/*----------------------------------------------------------------------------------------------
	Retrieve the name from the editbox.
----------------------------------------------------------------------------------------------*/
StrApp FmtGenDlg::GetName()
{
	achar rgchaName[1024]; // Name in the editbox.

	::GetDlgItemText(m_hwnd, kctidFgEdName, rgchaName, 1024);

	// Trim leading and trailing space characters.
	StrApp strName;
	StrUtil::TrimWhiteSpace(rgchaName, strName);

	return strName;
}

/*----------------------------------------------------------------------------------------------
	Replace strOldName with strNewName in the BasedOn and Next comboboxes.
----------------------------------------------------------------------------------------------*/
void FmtGenDlg::UpdateComboboxes(StrApp & strOldName, StrApp & strNewName)
{
	if (!m_pafsd->StyleIsSelected())
		return;

	StyleInfo styiSelected = m_pafsd->SelectedStyle();
	StrApp strTemp;
	int icombobox;

	// Update the name in the BasedOn combobox if it is there.
	icombobox = ::SendMessage(m_hwndBasedOn, CB_FINDSTRINGEXACT, 0, (LPARAM)strOldName.Chars());
	if (CB_ERR != icombobox)
	{
		::SendMessage(m_hwndBasedOn, CB_DELETESTRING, icombobox, 0);
		::SendMessage(m_hwndBasedOn, CB_ADDSTRING, 0, (LPARAM)strNewName.Chars());
		// Select the BasedOn value.
		strTemp = m_pafsd->GetNameOfStyle(styiSelected.m_hvoBasedOn);
		icombobox = ::SendMessage(m_hwndBasedOn, CB_FINDSTRINGEXACT, 0,
			(LPARAM)strTemp.Chars());
		if (CB_ERR != icombobox)
			::SendMessage(m_hwndBasedOn, CB_SETCURSEL, icombobox, 0);
	}

	// Update the name in the Next combobox, but only for a paragraph style.
	if (kstParagraph == styiSelected.m_st)
	{
		icombobox = ::SendMessage(m_hwndNext, CB_FINDSTRINGEXACT, 0,
			(LPARAM)strOldName.Chars());
		if (CB_ERR != icombobox)
		{
			::SendMessage(m_hwndNext, CB_DELETESTRING, icombobox, 0);
			::SendMessage(m_hwndNext, CB_ADDSTRING, 0, (LPARAM)strNewName.Chars());
			// Select the "Next Style" value.
			strTemp = m_pafsd->GetNameOfStyle(styiSelected.m_hvoNext);
			icombobox = ::SendMessage(m_hwndNext, CB_FINDSTRINGEXACT, 0,
				(LPARAM)strTemp.Chars());
			if (CB_ERR != icombobox)
				::SendMessage(m_hwndNext, CB_SETCURSEL, icombobox, 0);
		}
	}
	return;
}

/*----------------------------------------------------------------------------------------------
	Called when the dialog becomes active.
----------------------------------------------------------------------------------------------*/
bool FmtGenDlg::SetActive() // (HWND hwndDialog)
{
	// Get the stylesheet selected in the dialog styles list.
	// HWND m_hwndStylesList = GetDlgItem(hwndDialog, kctidLstStyles);
	// int iStyle = ListView_GetNextItem(m_hwndStylesList, -1, LVNI_SELECTED);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Handle a change in a combo box.
----------------------------------------------------------------------------------------------*/
bool FmtGenDlg::OnComboChange(NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	// Do nothing if we're not handling a change in the based on style name combo.
	if (pnmh->idFrom != kctidFgCbBasedOn)
	{
		lnRet = 0;
		return true;
	}

	achar rgchaName[1024]; // Name in the combobox.
	Vector<achar> vch;
	achar * pszT;
	bool fKeepChange = true;

	// Get the name from the combobox.
	int index = ::SendMessage(m_hwndBasedOn, CB_GETCURSEL, 0, 0);
	int cch = ::SendMessage(m_hwndBasedOn, CB_GETLBTEXTLEN, index, (LPARAM)0);
	if (cch < 1024)
		pszT = rgchaName;
	else
	{
		vch.Resize(cch + 1);
		pszT = vch.Begin();
	}

	cch = ::SendMessage(m_hwndBasedOn, CB_GETLBTEXT, index, (long)pszT);
	if (cch < 0)
		pszT = _T("");

	// Trim leading and trailing space characters.
	StrApp strNewBasedOnName;
	StrUtil::TrimWhiteSpace(pszT, strNewBasedOnName);

	// Get the name of the style whose based on style is being changed.
	StrUni stuCurrStyleName(m_pafsd->GetNameOfSelectedStyle());

	// Get the HVO of the new based on style.
	StrUni stuName;
	stuName.Assign(strNewBasedOnName);
	HVO hvoNewBasedOn = m_pafsd->GetHvoOfStyleNamed(stuName);

	// Check that the selected style will not be based on itself.
	if (stuCurrStyleName.Equals(strNewBasedOnName))
		fKeepChange = false;
	else
	{
		int hvo = m_pafsd->GetBasedOnHvoOfStyle(hvoNewBasedOn);

		// Loop through the inheritance chain to make sure none of the based on
		// styles are the same as the currently selected style.
		while (hvo && fKeepChange)
		{
			if (stuCurrStyleName.Equals(m_pafsd->GetNameOfStyle(hvo)))
				fKeepChange = false;
			else
				hvo = m_pafsd->GetBasedOnHvoOfStyle(hvo);
		}
	}

	// If keeping the new based on style, then notify the styles dialog of its change.
	if (fKeepChange)
	{
		StyleInfo styi = m_pafsd->SelectedStyle();
		m_pafsd->SetBasedOn(styi, hvoNewBasedOn);
		m_pafsd->BasedOnStyleChangeNotification(hvoNewBasedOn);
		LoadNextStyleCombobox(styi);
		SetNextStyleComboboxValue(styi);
	}
	else
	{
		StrApp strMsg(kstidAfsdSameBasedOnMsg);
		StrApp strMsgTitle(kstidStyles);
		// Raise a message alerting the user to his mistake.
		::MessageBox(m_hwnd, strMsg.Chars(), strMsgTitle.Chars(), MB_OK | MB_ICONINFORMATION);

		// Return the combobox to its original value.
		strNewBasedOnName = m_pafsd->GetNameOfStyle(m_pafsd->SelectedStyle().m_hvoBasedOn);
		int index = ::SendMessage(m_hwndBasedOn, CB_FINDSTRINGEXACT, 0,
			(LPARAM)strNewBasedOnName.Chars());

		::SendMessage(m_hwndBasedOn, CB_SETCURSEL, index, 0);
	}

	lnRet = 0;
	return true;
} //:> FmtGenDlg::OnComboChange.


/*----------------------------------------------------------------------------------------------
	Process notifications from the user.
----------------------------------------------------------------------------------------------*/
bool FmtGenDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	switch (pnmh->code)
	{
	case EN_KILLFOCUS: // Edit control modified.
		// Suppress updating the style from the control when we are trying to update the
		// control from the style.
		if (m_fSuppressLossOfFocus)
			break;

		if (ctidFrom == kctidFgEdName)
		{
			if (!m_pafsd->StyleIsSelected())
				return true;
			StyleInfo & styiSelected = m_pafsd->SelectedStyle();
			StrApp strOldName(styiSelected.m_stuName); // Save old name.
			StrApp strNewName;
			StrUni stuName;

			strNewName = GetName();

			// If the name has not changed, there is nothing to do.
			if (strNewName.Equals(strOldName))
				return true;

			stuName.Assign(strNewName);

			// If the style cannot be named stuName, put the old name back into the control.
			if (!m_pafsd->SetName(styiSelected, stuName))
			{
				::SetDlgItemText(m_hwnd, kctidFgEdName, strOldName.Chars());
				return true;
			}
			// Otherwise update the comboboxes for BasedOn and Next. Also update the edit box
			// to handle stripping of leading/trailing blanks.
			else
			{
				::SetDlgItemText(m_hwnd, kctidFgEdName, strNewName.Chars());
				UpdateComboboxes(strOldName, strNewName);
			}
		}
		return true;

	case CBN_SELCHANGE: // Combo box item changed.
		return OnComboChange(pnmh, lnRet);

	default:
		break;
	}

	return AfWnd::OnNotifyChild(ctidFrom, pnmh, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Process draw messages.
----------------------------------------------------------------------------------------------*/
bool FmtGenDlg::OnDrawChildItem(DRAWITEMSTRUCT * pdis)
{
	return AfWnd::OnDrawChildItem(pdis);
}

/*----------------------------------------------------------------------------------------------
	Handle What's This? help.
----------------------------------------------------------------------------------------------*/
bool FmtGenDlg::OnHelpInfo(HELPINFO * phi)
{
	return m_pafsd->DoHelpInfo(phi, m_hwnd);
}
