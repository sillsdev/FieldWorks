/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfStylesDlg.cpp
Responsibility: LarryW
Last reviewed: never

Description:
	This file contains the class definition for the Format/Styles dialog.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

// This is the string "Normal", to be used wherever the normal style's name is needed. Do not
// use a resource or hard-code for this name, as it is used in the database, and any change
// should be possible from as few places as possible.
const wchar * g_pszwStyleNormal = L"Normal";
const wchar * g_pszDefaultSerif = L"<default serif>";


/*----------------------------------------------------------------------------------------------
	The command map for the generic AfStylesDlg window.
----------------------------------------------------------------------------------------------*/
BEGIN_CMD_MAP(AfStylesDlg)
	ON_CID_GEN(kcidAfsdAddPara, &AfStylesDlg::CmdAdd, NULL)
	ON_CID_GEN(kcidAfsdAddChar, &AfStylesDlg::CmdAdd, NULL)
END_CMD_MAP_NIL()


//:> Constants.

// Dummy HVO for default paragraph characters. By using zero we make old char styles that are
// based on nothing show up as based on default paragraph characters, and also ensure that
// we'll get all kinds of errors if we try to store it as a real style.
#define khvoDefChars 0

// Identifier of normal style
const int AfStylesDlg::kiNormalStyle = 0;

//:>********************************************************************************************
//:>	Construction, destruction, initialization.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Generic constructor.
----------------------------------------------------------------------------------------------*/
AfStylesDlg::AfStylesDlg()
{
	m_rid = kridAfStyleDlg;
	// m_pszHelpUrl = "User_Interface/Menus/Format/Style/Style_General_tab.htm";
	m_itabCurrent = -1;
	m_itabInitial = 0; // 0 is the "General" tab.
	m_hwndTab = NULL; // Handle to tab control.
	m_hwndStylesList = NULL; // Handle to styles list view control.
	m_dxsClient = 0;
	m_dysClient = 0;
	m_ihvoNextNewStyi = -1000; // Temp hvo for a new style. Decrement for each additional style.
	m_nCharStyles = 0; // Number of new character styles.
	m_nParaStyles = 0; // Number of new paragraph styles.
	m_istyiSelected = -1; // None selected.
	m_nCustomStyleLevel = 9999;

	m_stuDefParaChars = L""; // Dummy style name for "no character style at all"
	m_stuDefParaChars.Load(kstidDefParaChars);

	m_nMsrSys = kninches;		// Should be overridden.
	m_wsUser = 0;				// MUST be overridden.
	m_hvoRootObj = 0;
	m_pclsidApp = NULL;
	m_fInLabelEdit = false;

	m_pszHelpFile = NULL;
	for (int iTabNum = 0; iTabNum < kcdlgv; iTabNum++)
		m_rgpszTabDlgHelpUrl[iTabNum] = NULL;
}

/*----------------------------------------------------------------------------------------------
	Generic destructor.
----------------------------------------------------------------------------------------------*/
AfStylesDlg::~AfStylesDlg()
{
}

/*----------------------------------------------------------------------------------------------
	Sets the initial values when created to modify char styles.

	@param stuCharStyle The Char style to be selected when dialog is opened.
----------------------------------------------------------------------------------------------*/
void AfStylesDlg::SetChrStyle(StrUni stuCharStyle)
{
	m_stuCharStyleNameOrig = stuCharStyle;
	m_fStyleChanged = false;
	m_stuStyleSelected = stuCharStyle;
}

/*----------------------------------------------------------------------------------------------
	Initialization. This is called by AdjustTsTextProps(...).

	@param pasts Stylesheet which controls the appearance of things
----------------------------------------------------------------------------------------------*/
void AfStylesDlg::Initialize(IVwStylesheet * pasts, bool fCanDoRtl, bool fOuterRtl,
	bool fFontFeatures, bool f1DefaultFont, bool * pfReloadDb)
{
	m_pasts = pasts;

	// Make a fresh VwPropertyStore and obtain from it a TsTextProps which thus
	// should contain the system default properties. Save this in m_qttpDefault.
	IVwPropertyStorePtr qvps;
	qvps.CreateInstance(CLSID_VwPropertyStore);
	CheckHr(qvps->get_TextProps(&m_qttpDefault));

	// We really want to display "unspecified" for the Underline style if there is none, so
	// remove it from m_qttpDefault.
	ITsTextPropsPtr qttp;
	ITsPropsBldrPtr qtpb;
	CheckHr(m_qttpDefault->GetBldr(&qtpb));
	CheckHr(qtpb->SetIntPropValues(ktptUnderline, -1, -1));
	CheckHr(qtpb->GetTextProps(&m_qttpDefault));

	m_fCanDoRtl = fCanDoRtl;
	m_fOuterRtl = fOuterRtl;
	m_fFontFeatures = fFontFeatures;
	m_f1DefaultFont = f1DefaultFont;
	m_pfReloadDb = pfReloadDb;
	*m_pfReloadDb = false;
}

/*----------------------------------------------------------------------------------------------
	First section of AdjustTsTextProps(), split off to facilitate testing.
----------------------------------------------------------------------------------------------*/
void AfStylesDlg::SetupForAdjustTsTextProps(bool fCanDoRtl, bool fOuterRtl, bool fFontFeatures,
	bool f1DefaultFont, IVwStylesheet * past, TtpVec & vqttpPara, TtpVec & vqttpChar,
	bool fCanFormatChar,
	bool & fReloadDb, Vector<int> & vwsAvailable, int hvoRootObj, IStream * pstrmLog,
	StrUni stuStyleName, bool fOnlyCharStyles)
{
	m_fOnlyCharStyles = fOnlyCharStyles;
	int cttpChar = vqttpChar.Size();
	AssertPtr(past);
	AssertArray(vqttpPara.Begin(), vqttpPara.Size());
	AssertArray(vqttpChar.Begin(), cttpChar);

	// Tell caller whether it needs to reload the DB (due to styles being renamed or deleted).
	bool * pfReloadDb = &fReloadDb;

	StrUni stuCharStyle = L"";
	StrUni stuParaStyle = L"";
	HRESULT hr;
	int ittp;

	FullyInitializeNormalStyle(past);

	// Determine the paragraph style name.
	int cttpPara = vqttpPara.Size();
	bool fParaConflict = false; // true if only one paragraph style was found for the selection.
	bool fNameFound = false; // Set to true once the first non-empty name is found.
	SmartBstr sbstrParaStyle;
	for (ittp = 0; ittp < cttpPara; ittp++)
	{
		ITsTextProps * pttp = vqttpPara[ittp];

		// In some cases pttp may be null, i.e., no paragraph properties were set.
		if (pttp)
		{
			CheckHr(hr = pttp->GetStrPropValue(kspNamedStyle, &sbstrParaStyle));
			if (hr != S_FALSE)
			{
				if (0 < sbstrParaStyle.Length())
				{
					if (fNameFound)
					{
						if (!stuParaStyle.Equals(sbstrParaStyle.Chars(),
							sbstrParaStyle.Length()))
						{
							stuParaStyle.Assign(g_pszwStyleNormal);
							fParaConflict = true;
							break;
						}
						else
							continue;
					}
					else
					{
						fNameFound = true;
						stuParaStyle = sbstrParaStyle.Chars();
						if (0 < ittp)
							fParaConflict = true;
						continue;
					}
				}
				if (fNameFound)
					fParaConflict = true;
			}
		}
		else if (fNameFound)
			fParaConflict = true;
	}

	// Use the "Normal" style if no non-empty name was found.
	if (stuParaStyle.Equals(L""))
	{
		stuParaStyle.Assign(g_pszwStyleNormal);
		// Treating this like a conflict prevents the arrow from showing up on the styles list.
		//fParaConflict = true;
	}

	// Determine the character style name.
	bool fCharConflict = false; // true if only one character style was found for the selection.
	fNameFound = false; // Set to false once the first name (even if "") has been found.
	SmartBstr sbstrCharStyle;
	for (ittp = 0; ittp < cttpChar; ittp++)
	{
		ITsTextProps * pttp = vqttpChar[ittp];

		CheckHr(hr = pttp->GetStrPropValue(kspNamedStyle, &sbstrCharStyle));
		if (hr != S_FALSE)
		{
			if (0 < sbstrCharStyle.Length())
			{
				if (fNameFound)
				{
					if (!stuCharStyle.Equals(sbstrCharStyle.Chars(), sbstrCharStyle.Length()))
					{
						stuCharStyle.Load(kstidDefParaCharacters);
						fCharConflict = true;
						break;
					}
					else
						continue;
				}
				else
				{
					fNameFound = true;
					stuCharStyle = sbstrCharStyle.Chars();
					if (0 < ittp)
						fCharConflict = true;
					continue;
				}
			}
			if (fNameFound)
				fCharConflict = true;
		}
		else if (fNameFound)
			fCharConflict = true;
	}

	// Use the "Default Paragraph Characters" style if no non-empty name was found.
	if (stuCharStyle.Equals(L""))
	{
		stuCharStyle.Load(kstidDefParaCharacters);
		// Treating this like a conflict prevents the arrow from showing up on the styles list.
		//fCharConflict = true;
	}

	Initialize(past, fCanDoRtl, fOuterRtl, fFontFeatures, f1DefaultFont, pfReloadDb);

	// Initialize member variables before running the dialog.
	m_stuParaStyleNameOrig = stuParaStyle;
	if(m_fOnlyCharStyles)
		m_stuCharStyleNameOrig = stuStyleName;
	else
		m_stuCharStyleNameOrig = stuCharStyle;
	m_fParaStyleFound = !fParaConflict;
	m_fCharStyleFound = !fCharConflict;
	m_fStyleChanged = false;
	m_stuStyleSelected = m_stuParaStyleNameOrig;

	m_vwsAvailable = vwsAvailable;
	m_hvoRootObj = hvoRootObj;
	m_qstrmLog = pstrmLog;

	// If vqttpPara is empty disable the Apply button.
	if (vqttpPara.Size())
		m_nEnableApply = kStyleEnableApply;
	else if (fCanFormatChar)
		m_nEnableApply = kStyleEnChrApply;
	else
		m_nEnableApply = kStyleDisableApply;
}

/*----------------------------------------------------------------------------------------------
	Middle section of AdjustTsTextProps(), split off to facilitate testing.
----------------------------------------------------------------------------------------------*/
int AfStylesDlg::DoModalForAdjustTsTextProps(HWND hwnd)
{
	// Run the format styles dialog.
	return DoModal(hwnd);
}

/*----------------------------------------------------------------------------------------------
	Final section of AdjustTsTextProps(), split off to facilitate testing.
// ncid takes on the following values based on how the styles dialog returns:
//   Close box in upper right corner - 2 (same as Cancel button)
//   Cancel button                   - 2
//   Close button                    - 1404
//   Apply button                    - 1
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::ResultsForAdjustTsTextProps(int ncid, StrUni * pstuStyleName,
	bool & fStylesChanged, bool & fApply)
{
	AssertPtr(pstuStyleName);

	*pstuStyleName = L"";
	fApply = false; // default
	fStylesChanged = m_fStyleChanged;

	switch (ncid)
	{
	case kctidCancel:
		// Either the Cancel button or the close box was pressed.
		return false; // No changes.

	case kctidOk: // Believe it or not, actually the Apply button
		{
			// Set the stylename string and return true.
			int stType = kstCharacter; // default for "default paragraph characters"
			bool fDefCharStyle = !wcscmp(m_stuStyleSelected.Bstr(), m_stuDefParaChars.Chars());
			if (!fDefCharStyle)
				CheckHr(m_pasts->GetType(m_stuStyleSelected.Bstr(), & stType));
			switch (stType)
			{
			default:
				Assert(false); // This should not happen.
				break;
			case kstParagraph:
				//if (fParaConflict || stuParaStyle != m_stuStyleSelected)
				// Return some valid style name regardless of whether it is the same as
				// what's already set - SharonC, July 31, 2001 (bug #1532)
				*pstuStyleName = m_stuStyleSelected;
				break;
			case kstCharacter:
				//if (fCharConflict || stuCharStyle != m_stuStyleSelected)
				*pstuStyleName = fDefCharStyle ? L"" : m_stuStyleSelected;
				break;
			}
			fApply = true;
		}
		// Fall through.

	case kctidClose: // Believe it or not, the OK button.
		if (fStylesChanged || fApply)
			return true;
		break;

	default:
		break;
	}

	return false; // No changes.
}

/*----------------------------------------------------------------------------------------------
	AfVwRootSite calls this method to initialize the format styles dialog.

	@h3{Algorithm}
	AfVwRootSite uses the selection to create the vector of paragraph properties, vqttpPara,
	and the range of TsTextProps, vqttpChar, that contain character properties.

	AdjustTsTextProps determines whether the paragraphs have the same named style. If so, the
	styles dialog will be initialized to select this style. Otherwise, the "Normal" style will
	be selected. Likewise, AdjustTsTextProps determines whether the runs of characters have the
	same named style. If so, the styles dialog will identify (with an icon) this character
	style.

	Next, run the styles dialog. If the user presses the Apply button, the name of the selected
	style will be returned in the parameter pstuStyleName if it is different from the style
	common to the selected paragraphs/texts, and fApply is true. Also, if the user modifies
	any of the styles, the boolean parameter fStylesChanged will be set to true.
	AdjustTsTextProps will return true if either (or both) of these conditions occur.

	If the user selects "Default Paragraph Characters", fApply is set true, and pstuStyleName
	is made empty.

	AfVwRootSite is then responsible to change the selected text and any other views that are
	affected by modifications of the styles.

	Note: This implementation of AdjustTsTextProps does not return property changes via the
	TsTextProps, although the implementations for the other format dialogs do.

	@h3{Parameters}
	@code{
		hwnd -- window handle passed by AfVwRootSite, Window()->Hwnd().
		fCanDoRtl -- ???
		fOuterRtl -- ???
		past -- pointer to the IVwStylesheet for a particular language project.
		vqttpPara -- vector of TsTextProps for paragraph properties.
		vqttpChar -- vector of TsTextProps for character properties.
		fCanFormatChar -- if no paragraph styles are in vqttpPara, true decides if character
			styles can be applied. If vqttpPara contains styles, fCanFormatChar is ignored.
		pstuStyleName -- name of selected style, when AdjustTsTextProps returns.
		fStylesChanged -- true if any of the styles have been changed, when AdjustTsTextProps
			returns.
		fApply -- true on return if user closed the dialog with the Apply button
		fReloadDb - true on return if all the view data must be reloaded from the DB; this
			is needed when styles are renamed or deleted.
		fOnlyCharStyles - if true than no paragrah styles are to used in the dialog.
	}
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::AdjustTsTextProps(HWND hwnd,
	bool fCanDoRtl, bool fOuterRtl, bool fFontFeatures, bool f1DefaultFont,
	IVwStylesheet * past, TtpVec & vqttpPara, TtpVec & vqttpChar, bool fCanFormatChar,
	Vector<int> & vwsAvailable, int hvoRootObj, IStream * pstrmLog,
	StrUni * pstuStyleName, bool & fStylesChanged, bool & fApply, bool & fReloadDb)
{
	bool fOnlyCharStyles = false;
	SetupForAdjustTsTextProps(fCanDoRtl, fOuterRtl, fFontFeatures, f1DefaultFont,
		past, vqttpPara, vqttpChar,
		fCanFormatChar, fReloadDb, vwsAvailable, hvoRootObj, pstrmLog, *pstuStyleName,
		fOnlyCharStyles);
	int ncid = DoModalForAdjustTsTextProps(hwnd);
	return ResultsForAdjustTsTextProps(ncid, pstuStyleName, fStylesChanged, fApply);
}

/*----------------------------------------------------------------------------------------------
	The Shoebox import wizard calls this method to initialize the format styles dialog in order
	to allow the user to add more styles to fit data being imported.

	@h3{Parameters}
	@code{
		hwnd -- window handle passed by AfVwRootSite, Window()->Hwnd().
		past -- pointer to the IVwStylesheet for a particular root box.
		stuStyleName -- name of selected style, before and after EditImportStyles.
		fStylesChanged -- true if any of the styles have been changed, when EditImportStyles
			returns.
	}
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::EditImportStyles(HWND hwnd, bool fCanDoRtl, bool fOuterRtl, bool fFontFeat,
	bool f1DefaultFont, IVwStylesheet * past, ILgWritingSystemFactory * pwsf, int stType,
	StrUni & stuStyleName, bool & fStylesChanged, const GUID * pclsidApp, int hvoRootObj)
{
	AssertPtr(past);
	AssertPtr(pwsf);

	// Use the "Normal" style if no non-empty name was found.
	StrUni stuParaStyle;
	if (stType == kstParagraph)
		stuParaStyle = stuStyleName;
	if (stuParaStyle.Equals(L""))
		stuParaStyle.Assign(g_pszwStyleNormal);

	// Use the "<default font>" style for the character style name. (was "<default serif>")
	StrUni stuCharStyle;
	if (stType == kstCharacter)
		stuCharStyle = stuStyleName;
	if (stuCharStyle.Equals(L""))
		stuCharStyle.Load(kstidDefaultSerif);

	fStylesChanged = false;

	GenSmartPtr<AfStylesDlg> qafsd;
	bool fReloadDb;
	qafsd.Create();
	qafsd->Initialize(past, fCanDoRtl, fOuterRtl, fFontFeat, f1DefaultFont, &fReloadDb);
	qafsd->SetAppClsid(pclsidApp);
	qafsd->SetRootObj(hvoRootObj);
	int wsUser;
	CheckHr(pwsf->get_UserWs(&wsUser));
	qafsd->SetUserWs(wsUser);
	qafsd->SetLgWritingSystemFactory(pwsf);
	// Review (SharonC): is there anything that has to be done if styles were deleted or
	// renamed (ie fReloadDb == true)?
	// Initialize member variables before running the dialog.
	qafsd->m_stuParaStyleNameOrig = stuParaStyle;
	qafsd->m_stuCharStyleNameOrig = stuCharStyle;
	qafsd->m_fStyleChanged = false;
	if (stType == kstParagraph)
	{
		qafsd->m_fParaStyleFound = true;
		qafsd->m_fCharStyleFound = false;
		qafsd->m_stuStyleSelected = qafsd->m_stuParaStyleNameOrig;
	}
	else
	{
		qafsd->m_fParaStyleFound = false;
		qafsd->m_fCharStyleFound = true;
		qafsd->m_stuStyleSelected = qafsd->m_stuCharStyleNameOrig;
	}
	qafsd->m_fOnlyCharStyles = false;
	qafsd->m_nEnableApply = kStyleDisableApply;		// Disable the Apply button.

	//------------------------------------------------------------------------------------------
	// Run the format styles dialog.

	int ncid = qafsd->DoModal(hwnd);
	// ncid takes on the following values based on how the styles dialog returns:
	//   Close box in upper right corner - 2 (same as Cancel button)
	//   Cancel button                   - 2
	//   Close button                    - 1404
	//   Apply button                    - 1

	fStylesChanged = qafsd->m_fStyleChanged;
	if (ncid == kctidClose)
	{
		int stType2;
		// Set the stylename string and return true.
		CheckHr(qafsd->m_pasts->GetType(qafsd->m_stuStyleSelected.Bstr(), &stType2));
		if (stType2 == stType)
		{
			if (stuStyleName != qafsd->m_stuStyleSelected)
				stuStyleName = qafsd->m_stuStyleSelected;
		}
		return true;
	}
	return false; // No changes.
}



//:>********************************************************************************************
//:>	Access methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get the selected style.

	@return Style Info of the selected style
----------------------------------------------------------------------------------------------*/
StyleInfo & AfStylesDlg::SelectedStyle()
{
	Assert(StyleIsSelected());
	return m_vstyi[m_istyiSelected];
}

/*----------------------------------------------------------------------------------------------
	Get the name of the selected style.

	@return name of the selected style
----------------------------------------------------------------------------------------------*/
StrUni AfStylesDlg::GetNameOfSelectedStyle()
{
	Assert(StyleIsSelected());
	return m_vstyi[m_istyiSelected].m_stuName;
}

/*----------------------------------------------------------------------------------------------
	Get the name of the style in m_vstyi whose HVO is hvoStyle.

	@param hvoStyle The style to find it's name.
	@return Name of this syle
----------------------------------------------------------------------------------------------*/
StrUni AfStylesDlg::GetNameOfStyle(HVO hvoStyle)
{
	for (int istyi = 0; istyi < m_vstyi.Size(); istyi++)
	{
		if ((!m_vstyi[istyi].m_fDeleted) && hvoStyle == m_vstyi[istyi].m_hvoStyle)
			return m_vstyi[istyi].m_stuName;
	}
	return StrUni(L"");
}

/*----------------------------------------------------------------------------------------------
	Get the index of the style in m_vstyi whose HVO is hvoStyle.

	@param hvoStyle The style to find it's index.
	@return Index of this syle
----------------------------------------------------------------------------------------------*/
int AfStylesDlg::GetIndexFromHVO(HVO hvoStyle) const
{
	for (int istyi = 0; istyi < m_vstyi.Size(); istyi++)
	{
		if ((!m_vstyi[istyi].m_fDeleted) && hvoStyle == m_vstyi[istyi].m_hvoStyle)
			return istyi;
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Get the index of the style in m_vstyi whose name is stuName
	@param stuName The style to find it's index.
	@return Index of this syle or -1 if not found
----------------------------------------------------------------------------------------------*/
int AfStylesDlg::GetIndexFromName(const StrUni& stuName) const
{
	for (int istyi = 0; istyi < m_vstyi.Size(); istyi++)
	{
		if (m_vstyi[istyi].m_stuName == stuName)
			return istyi;
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Get the BasedOn HVO of the style in m_vstyi whose HVO is hvoStyle.

	@param hvoStyle The style to find it's BasedOn HVO.
	@return BasedOn HVO
----------------------------------------------------------------------------------------------*/
HVO AfStylesDlg::GetBasedOnHvoOfStyle(HVO hvoStyle)
{
	for (int istyi = 0; istyi < m_vstyi.Size(); istyi++)
	{
		if ((!m_vstyi[istyi].m_fDeleted) && hvoStyle == m_vstyi[istyi].m_hvoStyle)
			return m_vstyi[istyi].m_hvoBasedOn;
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Get the hvo of the style in m_vstyi whose name is stuName.

	@param stuName The name of the style to find.
	@return HVO of the style
----------------------------------------------------------------------------------------------*/
HVO AfStylesDlg::GetHvoOfStyleNamed(StrUni stuName)
{
	for (int istyi = 0; istyi < m_vstyi.Size(); istyi++)
	{
		if ((!m_vstyi[istyi].m_fDeleted) && stuName == m_vstyi[istyi].m_stuName)
			return m_vstyi[istyi].m_hvoStyle;
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Return true if styi is directly or indirectly based on hvoNewBasedOn.

	@param pstyi The style to search for.
	@param hvoNewBasedOn HVO
	@return true if styi is directly or indirectly based on hvoNewBasedOn
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::IsBasedOn(StyleInfo * pstyi, HVO & hvoNewBasedOn)
{
	// If styi is based on hvoNewBasedOn, return true.
	if (pstyi->m_hvoBasedOn == hvoNewBasedOn)
		return true;

	// Otherwise, return true if styi is indirectly based on hvoNewBasedOn.
	HVO hvoBasedOn = pstyi->m_hvoBasedOn;
	while (hvoBasedOn)
	{
		if (GetBasedOnHvoOfStyle(hvoBasedOn) == hvoNewBasedOn)
			return true;
		hvoBasedOn = GetBasedOnHvoOfStyle(hvoBasedOn);
	}

	return false;
}


//:>********************************************************************************************
//:>	Set methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Give the style styi the suggested name if that name is not used and if it is different
	from the current name. Set m_fDirty to true if the name of style styi is changed.

	@return true if successful; false if an error is detected. The error has already been
	reported to the user if it returns false.

	@param styi The style to be named.
	@param stuNewName The requested name for the style.
	@return false if another style already has the suggested name.
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::SetName(StyleInfo & styi, const StrUni & stuNewName)
{
	int iItem; // Index of new style name in list view.
	LVFINDINFO lvfi;
	StrApp strMessage; // Message for alerting the user.
	StrApp strMessageTitle(kstidStyles); // Title of message box.
//	StrApp strOldName(styi.m_stuName); // Old name of selected style.
	StrApp strNewName; // New name entered by the user.

	// find out if listview has focus. We have to get the parent, because the listview
	// still has the edit window open.
	HWND hwnd = ::GetParent(::GetFocus());
	bool fListHasFocus = (hwnd == m_hwndStylesList); // true if listview has focus

	// Get the index of the selected style in the listview.
	// Search by index, because name may be invalid (empty or duplicated)!
	lvfi.flags = LVFI_PARAM;
	lvfi.lParam = m_istyiSelected;
	iItem = ListView_FindItem(m_hwndStylesList, -1, &lvfi);

	// If the new name is an empty string, alert the user and return false.
	if (0 == stuNewName.Length())
	{
		strMessage.Load(kstidAfsdEmptyNameMsg);
		::MessageBox(m_hwnd, strMessage.Chars(), strMessageTitle.Chars(),
			MB_OK | MB_ICONINFORMATION);
		// Allow the user to edit the style name again.
		if (fListHasFocus)
		{
			::SetFocus(m_hwndStylesList); // control must have focus!
			ListView_EditLabel(m_hwndStylesList, iItem);
		}
		return false;
	}

	// If this style already has the name stuNewName, return true.
	if (styi.m_stuName.Equals(stuNewName))
		return true;

	// Check that this name is not already used. If it is, return false.
	for (int i = 0; i < m_vstyi.Size(); i++)
	{
		if (m_vstyi[i].m_stuName.Equals(stuNewName) && !m_vstyi[i].m_fDeleted)
		{
			strMessage.Load(kstidAfsdSameNameMsg);
			::MessageBox(m_hwnd, strMessage.Chars(), strMessageTitle.Chars(),
				MB_OK | MB_ICONINFORMATION);
			// Allow the user to edit the style name again.
			if (fListHasFocus)
			{
				::SetFocus(m_hwndStylesList); // control must have focus!
				ListView_EditLabel(m_hwndStylesList, iItem);
			}
			return false;
		}
	}

	styi.m_stuName.Assign(stuNewName);
	styi.m_fDirty = true;
	if (styi.m_fBuiltIn)
		styi.m_fModified = true;

	// Update the text of the appropriate list view item.
	strNewName.Assign(stuNewName.Chars(), stuNewName.Length()); // New name.
	lvfi.psz = strNewName.Chars();
	ListView_SetItemText(m_hwndStylesList, iItem, 0, const_cast<achar *>(strNewName.Chars()));

//	StrUni stuOldName(strOldName);

	// TODO (EberhardB):
	// deleting or renaming styles causes problems when the user presses the Apply button.
	// DataNotebook pops up a message in that case that the selection was lost and ignores
	// the apply, TE tries to apply it but messes up previous instances of that style in
	// the current paragraph. Analysts are thinking of separating the Apply functionality
	// from the styles dialog, so as workaround we disable the Apply button in case of delete or
	// rename (TE-3781).
	m_nEnableApply = kStyleDisableApply;
	::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), false);

	return true;
} //:> AfStylesDlg::SetName.

/*----------------------------------------------------------------------------------------------
	This is called by the FmtGenDlg to inform us that the user has changed the based on style
	name. This allows us to determine whether or not the apply button should be enabled.

	@param hvoNewBasedOn The HVO of the new based on style.
----------------------------------------------------------------------------------------------*/
void AfStylesDlg::BasedOnStyleChangeNotification(HVO hvoNewBasedOn)
{
	int istyi = GetIndexFromHVO(hvoNewBasedOn);
	::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), CanApplyStyle(istyi));
} //:> AfStylesDlg::BasedOnStyleChangeNotification.

/*----------------------------------------------------------------------------------------------
	See if basing this style on the style identified as hvoNewBasedOn results in a
	circular route. If it does not, then make this style be based on hvoNewBasedOn, if it is
	not already. Set m_fDirty to true if this style is based on a different style.

	Note: The caller needs to handle a return of false, e.g., by raising a messagebox.

	@param styi The style whose BasedOn is to be changed.
	@param hvoNewBasedOn What the style (styi) is to be BasedOn.
	@return false if basing this style on the style identified as hvoNewBasedOn results in a
	circular route.
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::SetBasedOn(StyleInfo & styi, HVO & hvoNewBasedOn)
{
	// You cannot change BasedOn for a protected style.
	Assert(!IsStyleProtected(styi.m_stuName));

	HVO hvoBasedOn;

	// If this style is already based on hvoNewBasedOn, return true.
	if (styi.m_hvoBasedOn == hvoNewBasedOn)
		return true;

	// Otherwise, check that there will be no circular route.
	hvoBasedOn = hvoNewBasedOn;
	while (hvoBasedOn)
	{
		if (hvoBasedOn == styi.m_hvoStyle)
			return false;

		hvoBasedOn = GetBasedOnHvoOfStyle(hvoBasedOn);
	}

	styi.m_hvoBasedOn = hvoNewBasedOn;

	// Set the context, structure and function of the style to the values of the based on style.
	int istyi = GetIndexFromHVO(hvoNewBasedOn);
	if (istyi > -1)
	{
		styi.m_nContext = m_vstyi[istyi].m_nContext;
		styi.m_nStructure = m_vstyi[istyi].m_nStructure;
		styi.m_nFunction = m_vstyi[istyi].m_nFunction;
	}

	styi.m_fDirty = true;
	if (styi.m_fBuiltIn)
		styi.m_fModified = true;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Set the next style for this paragraph style to be hvoNewNext, if it is not already the next
	style. Set m_fDirty to true if this style now has a different next style. Return true.

	@param styi The style to be changed.
	@param hvoNewNext The style to change styi.m_hvoNext to.
	@return true
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::SetNext(StyleInfo & styi, HVO & hvoNewNext)
{
	// You cannot change Next for a protected style.
	Assert(!IsStyleProtected(styi.m_stuName));
	// The Next button of the General tab should be disabled for character styles.
	Assert(kstCharacter != styi.m_st);

	if (styi.m_hvoNext != hvoNewNext)
	{
		styi.m_hvoNext = hvoNewNext;
		styi.m_fDirty = true;
		if (styi.m_fBuiltIn)
			styi.m_fModified = true;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Set m_qttp to the pttp passed as an argument. Set m_fDirty to true.

	@param styi The style to be changed.
	@param pttp TextProps to be copied into styi
	@return true
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::AttachPttp(StyleInfo & styi, ITsTextProps * pttp)
{
	AssertPtr(pttp);
	styi.m_qttp.Attach(pttp);
	styi.m_fDirty = true;
	if (styi.m_fBuiltIn)
		styi.m_fModified = true;
	return true;
}


//:>********************************************************************************************
//:>	Message handling.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	The Apply button was pushed, so save data to DataBase.

	@param fClose Flag whether to close the dialog: essentially ignored in this case.
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::OnApply(bool fClose)
{
	if (m_fInLabelEdit) // we are in the midst of editing a label - very likely a rename
	{
		// Set focus to OK button so that label editing ends
		::SetFocus(::GetDlgItem(m_hwnd, kctidClose));
		return false;
	}

	if (fClose)
	{
		int itab;
		for (itab=0; itab < kcdlgv; itab++)
		{
			AssertPtr(m_rgdlgv[itab]);
			if (m_rgdlgv[itab]->QueryClose(AfDialogView::kqctOk) == false)
				return false;
		}
	}
	if (!UpdateTabCtrl(-1, m_istyiSelected))
		return false;

	// check if there are any deleted or renamed styles. If there are any, we shouldn't
	// apply the style, so just quit.
	for (int istyi = 0; istyi < m_vstyi.Size(); istyi++)
	{
		if ((m_vstyi[istyi].m_fDeleted && !m_vstyi[istyi].m_fJustCreated) ||
			(m_vstyi[istyi].m_hvoStyle >= 0 &&
			 m_vstyi[istyi].m_stuName != m_vstyi[istyi].m_stuNameOrig))
		{
			return false;
		}
	}

	if (!CopyToDb())
		return false;
	// This is important if the user was actually editing the name when he clicked Apply.
	// This apparently bypasses OnEndLabelEdit (no LVN_ENDLABELEDIT is sent by Windows).
	// TODO JohnT: this (and perhaps other cases?) bypasses the code in OnEndLabelEdit
	// that trims spaces from the name. Figure a way to share this fix.
	// (This was postponed in order to get a safe, minimal fix for the WorldPad release.)
	if (m_istyiSelected < m_vstyi.Size())		// May have deleted the selected style...
		m_stuStyleSelected = m_vstyi[m_istyiSelected].m_stuName;
	return AfDialog::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Close the dialog in response to the Cancel button being pressed. Also let the parent window
	know that we are closing.

	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::OnCancel()
{
	int itab;
	for (itab=0; itab < kcdlgv; itab++)
	{
		AssertPtr(m_rgdlgv[itab]);
		m_rgdlgv[itab]->SetCancelInProgress();
	}
	return AfDialog::OnCancel();
}


//:>********************************************************************************************
//:>	Protected methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Switch to showing tab itab and style istyi. If either is different from current, save the
	current values into m_vstyi. Initialize the current tab for the selected style. If fSave is
	true, save the values from the current style/tab combination. (This is not needed when
	deleting a style.) If some kind of validation fails when saving the current style/tab info,
	return false.

	@param itab Which tab is being updated; -1 if closing the dialog.
	@param istyi Style to update for; -1 if none selected or relevant.
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::UpdateTabCtrl(int itab, int istyi, bool fSave)
{
	if (itab == m_itabCurrent && istyi == m_istyiSelected)
	{
		return true; // no problem
	}

	// Disable the Delete button for protected styles.
	if (-1 != istyi)
	{
		::EnableWindow(::GetDlgItem(m_hwnd, kctidAfsdBtnDel),
			!IsStyleProtected(m_vstyi[istyi].m_stuName));
	}

	// Says whether General tab changed "Based On" value. When loading values into the chosen
	// tabbed dialog box (later on in this method after saving from the current one) the
	// inherited properties need to be calculated if either: a different style has been chosen
	// or: if the "based on" property of the current style has been changed in the General tab
	// and a different tab is then selected.
	bool fBasedOnChanged = false;
	// Text props that are inherited from parent styles and system defaults.
	ITsTextPropsPtr qttpInherited;

	// Obtain a pttp for the properties being inherited by the currently selected style.
	// The pttp is always obtained here, since we are not sure what may have happened to
	// the inheritance structure, and maybe to m_istyiSelected, since we were here last.
	// The inherited properties include the system default properties.
	GetInheritedProperties(m_istyiSelected, &qttpInherited);

	// Save the current info. Use m_itabCurrent and m_istyiSelected.
	// Return false if some validation error.
	if (fSave)
	{

		switch (m_itabCurrent)
		{
		default:
			Assert(false);
			break;
		case -1:
			// starting up, nothing previous selected. Do nothing about saving.
			break;
		case 0:
			{ // BLOCK
				// General tab.
				FmtGenDlg * pfgd = dynamic_cast<FmtGenDlg *>(m_rgdlgv[0].Ptr());
				Assert(pfgd);
				if (m_istyiSelected > -1)
					pfgd->GetDialogValues(m_vstyi[m_istyiSelected], fBasedOnChanged);
			}
			break;
		case 1:
			{ // BLOCK
				// Font tab.
				AfStyleFntDlg * pasfd = dynamic_cast<AfStyleFntDlg *>(m_rgdlgv[1].Ptr());
				Assert (pasfd);
				if (m_istyiSelected > -1)
				{
					ITsTextPropsPtr qttp;
					if (!pasfd->GetDlgValues(m_vstyi[m_istyiSelected].m_qttp, &qttp,
						m_vstyi[m_istyiSelected].m_st == kstParagraph))
					{
						return false;
					}
					if (qttp)
					{
						ITsTextProps * pttp = qttp.Detach();
						AttachPttp(m_vstyi[m_istyiSelected], pttp); // Sets m_fDirty.
					}
				}
			}
			break;
		case 2:
			{ // BLOCK
				// Paragraph tab.
				FmtParaDlg * pfpd = dynamic_cast<FmtParaDlg *>(m_rgdlgv[2].Ptr());
				Assert (pfpd);
				if (m_istyiSelected > -1)
				{
					ITsTextPropsPtr qttp;
					pfpd->GetStyleEffects(m_xprOrig, m_vstyi[m_istyiSelected].m_qttp, &qttp);
					if (qttp)
					{
						ITsTextProps * pttp = qttp.Detach();
						AttachPttp(m_vstyi[m_istyiSelected], pttp); // Sets m_fDirty.
					}
				}
			}
			break;
		case 3:
			{ // BLOCK
				// Bullets and Numbering tab.
				FmtBulNumDlg * pbnd = dynamic_cast<FmtBulNumDlg *>(m_rgdlgv[3].Ptr());
				Assert (pbnd);
				ITsTextProps * pttp = NULL;

				// We don't want the style info from this tab if the style is not a para style.
				if (m_istyiSelected > -1)
				{
					if (kstParagraph == m_vstyi[m_istyiSelected].m_st)
						pbnd->GetStyleEffects(m_vstyi[m_istyiSelected].m_qttp, &pttp);
					if (pttp)
						AttachPttp(m_vstyi[m_istyiSelected], pttp); // Sets m_fDirty.
				}
			}
			break;
		case 4:
			{	// Border tab.
				FmtBdrDlgPara * pfbdp = dynamic_cast<FmtBdrDlgPara *>(m_rgdlgv[4].Ptr());
				Assert (pfbdp);
				if (m_istyiSelected > -1)
				{
					ITsTextPropsPtr qttp;
					pfbdp->GetStyleEffects(m_vstyi[m_istyiSelected].m_qttp, &qttp);
					if (qttp)
					{
						ITsTextProps * pttp = qttp.Detach();
						AttachPttp(m_vstyi[m_istyiSelected], pttp); // Sets m_fDirty.
					}
				}
			}
			break;
		}
	}

	// Install new info. Use itab and istyi.
	// If fBasedOnChanged is true or a different style has been chosen, re-compute the
	// inherited properties. Note that istyi can be -1 when the dialog is closed.
	if (istyi >= 0 && (fBasedOnChanged || istyi != m_istyiSelected))
		GetInheritedProperties(istyi, &qttpInherited);
	switch (itab)
	{
	default:
		Assert(false);
		break;
	case -1:
		// Closing the dialog, no new pane or selection. Do nothing.
		break;
	case 0:
		{ // BLOCK
			// General tab.
			FmtGenDlg * pfgd = dynamic_cast<FmtGenDlg *>(m_rgdlgv[0].Ptr());
			Assert(pfgd);
			if (istyi > -1)
				pfgd->SetDialogValues(m_vstyi[istyi], m_vwsAvailable);
		}
		break;
	case 1:
		{ // BLOCK
			// Font tab.
			AfStyleFntDlg * pafsd = dynamic_cast<AfStyleFntDlg *>(m_rgdlgv[1].Ptr());
			Assert (pafsd);

			pafsd->SetWritingSystemFactory(m_qwsf);

			if (istyi > -1)
			{
				bool fCanInherit = (istyi != AfStylesDlg::kiNormalStyle);
				pafsd->SetDlgValues(m_vstyi[istyi].m_qttp, qttpInherited,
					fCanInherit, m_fFontFeatures, m_f1DefaultFont,
					m_vstyi[istyi].m_st == kstCharacter, m_vwsAvailable);
			}
			else
			{
				pafsd->SetDlgWsValues(m_vwsAvailable); // Just show a list of encodings.
			}
		}
		break;
	case 2:
		{ // BLOCK
			// Paragraph tab.
			FmtParaDlg * pfpd = dynamic_cast<FmtParaDlg *>(m_rgdlgv[2].Ptr());
			Assert (pfpd);
			if (istyi > -1)
			{
				if (kstParagraph == m_vstyi[istyi].m_st)
				{
					// Enable the Paragraph dialog controls for a paragraph style.
					bool fCanInherit = (istyi != AfStylesDlg::kiNormalStyle);
					pfpd->InitForStyle(m_vstyi[istyi].m_qttp, qttpInherited, m_xprOrig,
						true, fCanInherit);
				}
				else
					// Disable the Paragraph dialog controls for other styles.
					pfpd->InitForStyle(m_vstyi[istyi].m_qttp, qttpInherited, m_xprOrig,
						false, false);
			}
		}
		break;
	case 3:
		{ // BLOCK
			// Bullets and Numbering tab.
			FmtBulNumDlg * pbnd = dynamic_cast<FmtBulNumDlg *>(m_rgdlgv[3].Ptr());
			Assert (pbnd);
			if (istyi > -1)
			{
				if (kstParagraph == m_vstyi[istyi].m_st)
				{
					// Enable the Bullets and Numbering dialog controls for a paragraph style.
					bool fCanInherit = (istyi != AfStylesDlg::kiNormalStyle);
					pbnd->InitForStyle(m_vstyi[istyi].m_qttp, qttpInherited, m_xprOrig,
						true, fCanInherit);
				}
				else
					// Disable the Bullets and Numbering dialog controls for other styles.
					pbnd->InitForStyle(m_vstyi[istyi].m_qttp, qttpInherited, m_xprOrig,
						false, false);
			}
		}
		break;
	case 4:
		{ // BLOCK
			// Border tab.
			FmtBdrDlgPara * pbrd = dynamic_cast<FmtBdrDlgPara *>(m_rgdlgv[4].Ptr());
			Assert (pbrd);
			if (istyi > -1)
			{
				if (kstParagraph == m_vstyi[istyi].m_st)
				{
					// Enable the Border dialog controls for a paragraph style.
					bool fCanInherit = (istyi != AfStylesDlg::kiNormalStyle);
					pbrd->InitForStyle(m_vstyi[istyi].m_qttp, qttpInherited, m_xprOrig, true,
						fCanInherit);
				}
				else
					// Disable the Border dialog controls for other styles.
					pbrd->InitForStyle(m_vstyi[istyi].m_qttp, qttpInherited, m_xprOrig, false,
						false);
			}
		}
		break;
	}

	if (-1 != itab)
	{
		m_itabCurrent = itab;
		m_istyiSelected = istyi;
	}
	// Make sure we show the correct tabs
	int ctabWanted; // number of tabs we want
	StyleInfo & styi = m_vstyi[m_istyiSelected];
	if (styi.m_st == kstCharacter)
	{
		if (khvoDefChars == styi.m_hvoStyle)
			ctabWanted = 1; // general tab only, can't edit def char style
		else
			ctabWanted = 2; // General and font only
	}
	else
		ctabWanted = 5; // All of them
	if (ctabWanted != m_ctabVisible)
	{
		int itabT;
		for (itabT = m_ctabVisible; --itabT >= ctabWanted; )
			TabCtrl_DeleteItem(m_hwndTab, itabT);
		TCITEM tci = { TCIF_TEXT };
		for (itabT = m_ctabVisible; itabT < ctabWanted; itabT++)
		{
			StrApp str;
			switch (itabT)
			{
			case 1:
				str.Load(kstidStyFont);
				break;
			case 2:
				str.Load(kstidStyParagraph);
				break;
			case 3:
				str.Load(kstidStyBullNum);
				break;
			case 4:
				str.Load(kstidStyBorder);
				break;
			}
			tci.pszText = const_cast<achar *>(str.Chars());
			TabCtrl_InsertItem(m_hwndTab, itabT, &tci);
		}

		m_ctabVisible = ctabWanted;
		// Inserting and deleting things erases the pane contents but does not
		// redraw.
		::InvalidateRect(m_rgdlgv[m_itabCurrent]->Hwnd(), NULL, false);

		// If we've changed style, and the new style doesn't allow this many tabs,
		// switch to tab 0. We've already saved, so needn't do that again (and mustn't,
		// because the values in this tab don't apply to this kind of style).
		if (m_itabCurrent >= m_ctabVisible)
			ShowChildDlg(0, false);
		TabCtrl_SetCurSel(m_hwndTab, m_itabCurrent);
	}


	return true; // All went well with save, if any.
} //:> AfStylesDlg::UpdateTabCtrl.

/*----------------------------------------------------------------------------------------------
	Gets a style to replace the given deleted style

	@param styiDeletedStyle Info about the style being deleted.
	@return Name of replacement style to use.
----------------------------------------------------------------------------------------------*/
StrUni AfStylesDlg::GetStyleForDeletedStyle(StyleInfo styiDeletedStyle)
{
	BSTR bstrDefaultStyleName;
	CheckHr(m_pasts->GetDefaultStyleForContext(styiDeletedStyle.m_nContext, false,
		&bstrDefaultStyleName));
	return StrUni(bstrDefaultStyleName);
}

/*----------------------------------------------------------------------------------------------
	Handle window messages. Process window messages for the listview.

	@param wm windows message
	@param wp WPARAM
	@param lp LPARAM
	@param lnRet Value to be returned to the windows.
	@return true
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	if (wm == WM_ERASEBKGND)
		// This is required to prevent the listview from not painting when selected then covered
		// by another window, then uncovered.  Without this the listview will not repaint.
		RedrawWindow(m_hwndStylesList, NULL , NULL,
			RDW_ERASE | RDW_FRAME | RDW_INTERNALPAINT | RDW_INVALIDATE);
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}


/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they need
	initial values.)

	See ${AfDialog#FWndProc}
	@param hwndCtrl (not used)
	@param lp (not used)
	@return true
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Subclass the Add buttons.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidAfsdBtnAdd, kbtPopMenu, NULL, 0);

	// Load the bitmaps used in the list view of styles.
	m_hwndStylesList = GetDlgItem(m_hwnd, kctidLstStyles);
	HIMAGELIST himlStyles; // Image list.

	// I changed the last argument to CLR_DEFAULT because the images weren't coming in
	// with a transparent background (CLR_DEFAULT treats the top, left pixel in the bmp
	// as the transparent color). I don't have a bright white background on my monitor
	// so I noticed this. I'm not sure why RGB(0x0, 0x80, 0x80) was used. DavidO.
	himlStyles = ImageList_LoadBitmap(ModuleEntry::GetModuleHandle(),
		MAKEINTRESOURCE(kridAfStylesList), 16, 0, CLR_DEFAULT); //RGB(0x00, 0x80, 0x80));

	// Assign the image list to the list view control.
	HIMAGELIST himlOld = ListView_SetImageList(m_hwndStylesList, himlStyles, LVSIL_SMALL);
	if (himlOld)
		if (himlOld != himlStyles)
			AfGdi::ImageList_Destroy(himlOld);

	// Insert the list view in the dialog control.
	LVCOLUMN lvc = { LVCF_TEXT | LVCF_WIDTH };
	lvc.cx = 170; // Even though the width of control from AfStylesDlg.rc is 130.
	ListView_InsertColumn(m_hwndStylesList, 0, &lvc);

	CopyToLocal(); // Copy info from m_pasts to our local variable, m_vstyi, and set
					// m_istyiSelected.
	// Set up the tabs.
	SetupTabs();

	// setup the help files for each of the tabs
	for (int iTabNum = 0; iTabNum < kcdlgv; iTabNum++)
	{
		if (m_rgpszTabDlgHelpUrl[iTabNum] != NULL)
		{
			m_rgdlgv[iTabNum]->SetHelpUrl(m_rgpszTabDlgHelpUrl[iTabNum]);
		}
		else if (m_qhtprov)
		{
			SmartBstr sbstrHelpUrl;
			StrUni stu;
			stu.Format(L"kstidStylesDialogTab%d", iTabNum + 1);
			HRESULT hr = m_qhtprov->GetHelpString(stu.Bstr(), 0, &sbstrHelpUrl);
			if (SUCCEEDED(hr) && sbstrHelpUrl.Length())
			{
				m_rgstrTabHelpUrls[iTabNum].Assign(sbstrHelpUrl.Chars());
				m_rgdlgv[iTabNum]->SetHelpUrl(m_rgstrTabHelpUrls[iTabNum].Chars());
			}
		}
		if (m_pszHelpFile != NULL)
		{
			m_rgdlgv[iTabNum]->SetHelpFile(m_pszHelpFile);
		}
		else if (m_qhtprov)
		{
			SmartBstr sbstrHelpFile;
			StrUni stu(L"UserHelpFile");
			HRESULT hr = m_qhtprov->GetHelpString(stu.Bstr(), 0, &sbstrHelpFile);
			if (SUCCEEDED(hr) && sbstrHelpFile.Length())
			{
				StrApp strHelpFile(sbstrHelpFile.Chars());
				m_rgdlgv[iTabNum]->SetHelpFile(strHelpFile.Chars());
			}
		}
	}

	// Insert the title of the base tab.
	TCITEM tci = { TCIF_TEXT };
	StrApp str(kstidStyGeneral);
	tci.pszText = const_cast<achar *>(str.Chars());
	TabCtrl_InsertItem(m_hwndTab, 0, &tci);
	m_ctabVisible = 1;

	// This section must be after at least one tab gets added to the tab control.
	RECT rcTab;
	GetWindowRect(m_hwndTab, &rcTab);
	TabCtrl_AdjustRect(m_hwndTab, false, &rcTab);
	POINT pt = { rcTab.left, rcTab.top };
	ScreenToClient(m_hwnd, &pt);
	m_dxsClient = pt.x;
	m_dysClient = pt.y;

	SetDialogValues();

	// Show the first tab, the "General" tab, by default.
	ShowChildDlg(m_itabInitial);

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
			(pfn)(m_hwndTab, L"", L"");
			(pfn)(m_hwndStylesList, L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidAfsdBtnAdd), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidAfsdBtnCopy), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidAfsdBtnDel), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidInsert), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidClose), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidOk), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidCancel), L"", L"");
			(pfn)(::GetDlgItem(m_hwnd, kctidHelp), L"", L"");
		}

		::FreeLibrary(hmod);
	}

	// (EberhardB/TomB): our customer wants the list box to have focus therefore we changed
	// the following line. OnInitDlg has to return false so that our set focus has success.
	// Old code:
	// return AfDialog::OnInitDlg(hwndCtrl, lp);

	AfDialog::OnInitDlg(hwndCtrl, lp);
	return false;
} //:> AfStylesDlg::OnInitDlg.

/*----------------------------------------------------------------------------------------------
	Process notifications for this dialog from some event on a control.  This method is called
	by the framework.

	@param ctid Id of the control that issued the windows command.
	@param pnmh Windows command that is being passed.
	@param lnRet return value to be returned to the windows command.
	@return true if command is handled.
	See ${AfWnd#OnNotifyChild}
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	Rect rc;

	if (SuperClass::OnNotifyChild(ctid, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctid)
		{
		case kctidClose:	// Okay button
			if (!UpdateTabCtrl(-1, -1)) // Cache and validate the dialog values.
				return true;
			if (!CopyToDb())
				return true; // don't quit
			// fall through:
		case kctidCancel:
			::EndDialog(m_hwnd, ctid);
			return true;

		case kctidAfsdBtnAdd:
			{
				HMENU hmenu = ::LoadMenu(ModuleEntry::GetModuleHandle(),
					MAKEINTRESOURCE(kctidAfsdAddMnu));
				HMENU hmenuPopup = ::GetSubMenu(hmenu, 0);
				::GetWindowRect(pnmh->hwndFrom, &rc);
				int cid = ::TrackPopupMenu(hmenuPopup,
					TPM_LEFTALIGN | TPM_RIGHTBUTTON | TPM_RETURNCMD,
					rc.left, rc.bottom, 0, m_hwnd, NULL);
				::DestroyMenu(hmenu);
				// No command handler to direct these menu commands properly.
				Cmd cmd(cid);
				switch (cid)
				{
				case kcidAfsdAddPara:
					// Fall through -- treat these two the same.
				case kcidAfsdAddChar:
					CmdAdd(&cmd);
					break;
				default:
					Assert(cid == 0 || cid == kcidAfsdAddPara || cid == kcidAfsdAddChar);
					break;
				}
			}
			return true;
		case kctidAfsdBtnCopy:
			CmdCopyStyle();
			break;

		case kctidAfsdBtnDel:
			CmdDel();
			return true;
		}

		break;

	case LVN_ITEMCHANGED:
		{
			switch (pnmh->idFrom)
			{
			case kctidLstStyles: // A selection was made in the Styles list view.
				NMLISTVIEW * qnmlv = (LPNMLISTVIEW) pnmh;
				if (qnmlv->uNewState & LVIS_SELECTED)
				{
					// Retrieve the index of the currently selected item.
					int iitem = qnmlv->lParam;

					// Get its text
					int istyi = iitem; // Don't alter m_istyiSelected till we save old data.
					// Also must verify first that current values are OK.
					if (!UpdateTabCtrl(m_itabCurrent, istyi))
						break;
#if 0
// I (JohnT) thought this would be necessary but apparently the list identifies the items
// by the index with which they were inserted, not their current position.
					achar rgchText[1024];
					ListView_GetItemText(m_hwndStylesList, iitem, 0, rgchText, 1024);
					// If the user clicks Apply now, this will be the style that gets applied.
					m_stuStyleSelected = rgchText;
					// Figure which item it is.
					for (istyi = 0; istyi < m_vstyi.Size(); istyi++)
					{
							StrUni stuT = rgchText;
						if (!wcscmp(stuT.Chars(), m_vstyi[istyi].m_stuName.Chars()))
							break;
					}
#else
					m_stuStyleSelected = m_vstyi[istyi].m_stuName;
#endif
					Assert(istyi < m_vstyi.Size());

					::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), CanApplyStyle(istyi));
				}
				break;
			}
			return true;
		}

	case LVN_ITEMCHANGING:
		{
			// If the user clicked on an empty part of the list view, keep the selection
			// on the current item.
			NMLISTVIEW * pnmlv = (NMLISTVIEW *)pnmh;
			if (pnmlv->uChanged & LVIF_STATE && !(pnmlv->uNewState & LVIS_SELECTED))
			{
				// NOTE: This can also be called when the keyboard is used to select a different
				// item. In this case, we don't want to cancel the new selection.
				if (::GetKeyState(VK_LBUTTON) < 0 || ::GetKeyState(VK_RBUTTON) < 0)
				{
					LVHITTESTINFO lvhti;
					::GetCursorPos(&lvhti.pt);
					::ScreenToClient(pnmh->hwndFrom, &lvhti.pt);
					if (ListView_HitTest(pnmh->hwndFrom, &lvhti) == -1)
					{
						lnRet = true;
						return true;
					}
					// Ask the General tab, if it is showing, to redraw the name.
					if (m_itabCurrent == m_itabInitial)
					{
						FmtGenDlg * pfgd = dynamic_cast<FmtGenDlg *>(m_rgdlgv[0].Ptr());
						// Get the name from the General tab name editbox.
						StrUni stuNewName(pfgd->GetName());
						// Check that this name is not already used. If it is, return false.
						for (int i = 0; i < m_vstyi.Size(); i++)
						{
							if (i == m_istyiSelected)
								continue;
							if (m_vstyi[i].m_stuName.Equals(stuNewName) && !m_vstyi[i].m_fDeleted)
							{
								lnRet = true;
								return true;
							}
						}
					}
				}
			}
		}
		break;

	case NM_RCLICK:
		// If there's a callback for getting the help topic, use it to obtain help.
		if (m_qhtprov && pnmh->idFrom == kctidLstStyles)
		{
			Point pt;
			::GetCursorPos(&pt);
			HMENU hmenu = ::LoadMenu(ModuleEntry::GetModuleHandle(),
				MAKEINTRESOURCE(kctidStylePopup));
			HMENU hmenuPopup = ::GetSubMenu(hmenu, 0);
			int cid = ::TrackPopupMenu(hmenuPopup,
				TPM_LEFTALIGN | TPM_RIGHTBUTTON | TPM_RETURNCMD, pt.x, pt.y, 0, m_hwnd, NULL);
			::DestroyMenu(hmenu);
			if (cid == kcidStylePopupHelp)
			{
				LPNMITEMACTIVATE pnmitem = (LPNMITEMACTIVATE)pnmh;
				achar rgch[1000];
				LVITEM lvi;
				lvi.mask = LVIF_TEXT;
				lvi.iItem = pnmitem->iItem;
				lvi.iSubItem = pnmitem->iSubItem;
				lvi.pszText = rgch;
				lvi.cchTextMax = sizeof(rgch) / sizeof(achar);
				LRESULT lResult = ::SendMessage(pnmitem->hdr.hwndFrom, LVM_GETITEMTEXT,
					(WPARAM)pnmitem->iItem, (LPARAM)&lvi);
				if (lResult < 999)
					rgch[lResult] = 0;
				else
					rgch[999] = 0;		// be paranoid.
				StrUni stuTopic(L"style:");
				stuTopic.Append(rgch);
				if (m_qhtprov)
				{
					SmartBstr sbstrHelpUrl;
					HRESULT hr = m_qhtprov->GetHelpString(stuTopic.Bstr(), 0, &sbstrHelpUrl);
					if (SUCCEEDED(hr))
					{
						AssertPsz(m_pszHelpFile);
						AssertBstr(sbstrHelpUrl);
						StrApp strHelpUrl(sbstrHelpUrl.Chars());
						AfUtil::ShowHelpFile(m_pszHelpFile, strHelpUrl.Chars());
					}
				}
			}
		}
		break;

	case TCN_SELCHANGE:
		{
			// Make sure we can move to the current tab.
			int itab = TabCtrl_GetCurSel(m_hwndTab);
			Assert((uint)itab < (uint)kcdlgv);
			if (!ShowChildDlg(itab))
				TabCtrl_SetCurSel(m_hwndTab, m_itabCurrent); // Move back to the old tab.
			return true;
		}

	case TCN_SELCHANGING:
		{
			// Make sure that we can move off of the current tab.
			int itab = TabCtrl_GetCurSel(m_hwndTab);
			Assert((uint)itab < (uint)kcdlgv);
			lnRet = !m_rgdlgv[itab]->QueryClose(AfDialogView::kqctChange);
			return true;
		}

	case LVN_KEYDOWN:
		{
			NMLVKEYDOWN * pnmlvkd = (NMLVKEYDOWN *)pnmh;
			if (pnmlvkd->wVKey == VK_F2)
			{
				int iitem = ListView_GetNextItem(pnmh->hwndFrom, -1, LVNI_SELECTED);
				if (iitem != -1)
					ListView_EditLabel(pnmh->hwndFrom, iitem);
			}
			break;
		}

	case LVN_BEGINLABELEDIT:
		{
			NMLVDISPINFO * pdi = (NMLVDISPINFO *) pnmh;
			if (IsStyleProtected(m_vstyi[pdi->item.lParam].m_stuName))
			{
				// don't allow renaming of a protected style
				lnRet = true;
			}
			else
			{
				lnRet = false;
				m_fInLabelEdit = true;
			}
			return true;
		}

	case LVN_ENDLABELEDIT:
		{
			Assert(pnmh->idFrom == kctidLstStyles);
			m_fInLabelEdit = false;
			return OnEndLabelEdit((NMLVDISPINFO *)pnmh, lnRet);
		}

	// Default is to do nothing.
	}

	return false;
} //:> AfStylesDlg::OnNotifyChild.

/*----------------------------------------------------------------------------------------------
	Determine if apply button should be enabled or disabled

	@return true if apply button should be enabled, otherwise false
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::CanApplyStyle(int istyi)
{
	// Hard code for the DN styles "Internal Link" and "External Link".
	// These names can't be changed so hard code is OK. Disable "Apply".
	if (m_stuStyleSelected == L"Internal Link" ||
		m_stuStyleSelected == L"External Link")
	{
		return false;
	}
	else
	{
		bool fEnable = false;
		switch (m_nEnableApply)
		{
			case kStyleDisableApply:
				return false;
			case kStyleEnChrApply:
				fEnable = (m_vstyi[istyi].m_st == kstCharacter);
				break;
			case kStyleEnableApply:
				fEnable = true;
				break;
			default:
				Assert("Unknown value for m_nEnableApply");
				return false;
		}

		if (fEnable && m_vApplicableContexts.Size() && m_vstyi.Size())
		{
			for (int i = 0; i < m_vApplicableContexts.Size(); i++)
			{
				if (m_vApplicableContexts[i] == m_vstyi[istyi].m_nContext)
					return true;
			}
			return false;
		}

		return fEnable;
	}
}

/*----------------------------------------------------------------------------------------------
	Pass the message on to the current sub dialog.

	@return true
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::OnHelp()
{
	Assert((uint)m_itabCurrent < kcdlgv);
	AssertPtr(m_rgdlgv[m_itabCurrent]);
	AssertPsz(m_pszHelpFile);
	AfUtil::ShowHelpFile(m_pszHelpFile, m_rgdlgv[m_itabCurrent]->GetHelpUrl());
	return true;
}

/*----------------------------------------------------------------------------------------------
	Show help information for the requested control.
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::OnHelpInfo(HELPINFO * phi)
{
	return DoHelpInfo(phi, m_hwnd);
}

/*----------------------------------------------------------------------------------------------
	Show help information for the requested control.  This handles controls on the various tab
	subdialogs as well as the main dialog.
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::DoHelpInfo(HELPINFO * phi, HWND hwnd)
{
	AssertPtr(phi);

	// Get the coordinates of the control and center the tooltip underneath the control.
	Rect rc;
	::GetWindowRect((HWND)phi->hItemHandle, &rc);
	phi->MousePos.x = rc.left + (rc.Width() / 2);
	phi->MousePos.y = rc.bottom + 1;

	AfContextHelpWndPtr qchw;
	qchw.Attach(NewObj AfContextHelpWnd);
	if (m_qwsf)
		qchw->SetLgWritingSystemFactory(m_qwsf);
	qchw->Create(hwnd, phi);
	return true;
}

/*----------------------------------------------------------------------------------------------
	See if the given style name is one that is protected from fundamental changes by the user.

	@param stuStyleName style to be checked.
	@return true if the given style name is one that is protected from fundamental changes
	by the user.
----------------------------------------------------------------------------------------------*/
// TODO ToddJ: Use style id's when JohnT has a plan
bool AfStylesDlg::IsStyleProtected(const StrUni stuStyleName)
{
	// Check if this style is "Default Paragraph Characters", the dummy entry provided
	// within AfStylesDlg
	if (!wcscmp(stuStyleName, m_stuDefParaChars.Chars()))
		return true;

	// Check if this style is considered protected by the stylesheet implementation.
	// "Normal" should be one of those the stylesheet protects.
	ComBool fStyleProtected;
	CheckHr(m_pasts->get_IsStyleProtected(stuStyleName.Bstr(), & fStyleProtected));

	return (bool)fStyleProtected;
}

/*----------------------------------------------------------------------------------------------
	Add a new style.

	@param pcmd Ptr to menu command
	@return true successful
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::CmdAdd(Cmd * pcmd)
{
	AssertObj(pcmd);
	::SetFocus(m_hwndStylesList);

	bool fSucceeded = true; // true if MakeNewStyi succeeded.

	// Create the new style.
	StyleInfo styiNew;
	styiNew.m_fJustCreated = true;
	switch (pcmd->m_cid)
	{
		case kcidAfsdAddPara:
			fSucceeded = MakeNewStyi(kstParagraph, &styiNew);
			break;

		case kcidAfsdAddChar:
			fSucceeded = MakeNewStyi(kstCharacter, &styiNew);
			break;

		default:
			Assert(false);
	}

	// Add it to the list of styles.
	if (fSucceeded)
		InstallStyle(styiNew);
	return fSucceeded;
} //:> AfStylesDlg::CmdAdd.


/*----------------------------------------------------------------------------------------------
	Copy a style.

	@return true
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::CmdCopyStyle()
{
	try
	{
		::SetFocus(m_hwndStylesList);

		// Create the new style.
		StyleInfo styiNew;
		if (!CopyStyi(m_vstyi[m_istyiSelected], styiNew))
			return true;
		styiNew.m_fJustCreated = true;

		// Add the style name to the list of styles in the dialog.
		int iStyle = m_vstyi.Size();
		LVITEM lvi;
		lvi.mask = LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM;
		lvi.iImage = (int)styiNew.m_st;
		lvi.iItem = iStyle;
		lvi.iSubItem = 0;
		lvi.lParam = (LPARAM) iStyle;

		StrApp strName(styiNew.m_stuName);
		lvi.pszText = const_cast<achar *>(strName.Chars());
		ListView_InsertItem(m_hwndStylesList, &lvi);

		// Add it to the list of styles.
		InstallStyle(styiNew);
	}
	catch (...)
	{
		// Nothing much we can do...it is a command so can't fail...and still no one else
		// can implement it...
		return true;
	}

	return true;
} //:> AfStylesDlg::CmdCopyStyle().


/*----------------------------------------------------------------------------------------------
	Delete a style.
	@return true
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::CmdDel()
{
	LVFINDINFO lvfi;
	int iItem; // Index of item in list view.
	int iStyle; // Index of style in m_vstyi.
	StrApp str; // Used for message and style name.

	// The Delete button should be disabled for styles originally provided by FieldWorks.
	Assert(!IsStyleProtected(m_vstyi[m_istyiSelected].m_stuName));
	str.Assign(m_vstyi[m_istyiSelected].m_stuName);

	// Change any references to the style that is to be deleted.
	StyleInfo * pstyiStyleToDelete = &m_vstyi[m_istyiSelected];
	for (iStyle = 0; iStyle < m_vstyi.Size(); iStyle++)
	{
		StyleInfo * pstyiThisStyle = &m_vstyi[iStyle];
		// Is this style based on the style-to-delete?
		if ((*pstyiThisStyle).m_hvoBasedOn == (*pstyiStyleToDelete).m_hvoStyle)
		{
			bool f;
			f =	SetBasedOn(*pstyiThisStyle, (*pstyiStyleToDelete).m_hvoBasedOn); // Sets m_fDirty.
			Assert(f);
			// Recalculate the TsTextProps before deleting the style.
			for (int i = 0; i < m_vstyi.Size(); i++)
			{
				ITsTextPropsPtr qttpDerived;
				FwStyledText::ComputeInheritance(
					(*pstyiStyleToDelete).m_qttp, // Base ttp.
					(*pstyiThisStyle).m_qttp, // Current ttp.
					&qttpDerived);  // Recalculated ttp.
				if (qttpDerived)
				{
					ITsTextProps * pttp = qttpDerived.Detach();
					AttachPttp(*pstyiThisStyle, pttp); // Sets m_fDirty.
				}
				else
					Assert(false); // Something went wrong here.
				break;
			}

		}
		if (kstCharacter != (*pstyiThisStyle).m_st &&
			(*pstyiThisStyle).m_hvoNext == (*pstyiStyleToDelete).m_hvoStyle)
		{
			SetNext(*pstyiThisStyle, (*pstyiStyleToDelete).m_hvoBasedOn); // Sets m_fDirty.
		}
	}

	m_vstyi[m_istyiSelected].m_fDeleted = true;

	// Delete the style from the list view.
	lvfi.flags = LVFI_STRING;
	lvfi.psz = str.Chars();
	iItem = ListView_FindItem(m_hwndStylesList, -1, &lvfi);
	if (iItem > -1)
		ListView_DeleteItem(m_hwndStylesList, iItem);
	else
	{
		// this means that the user probably changed the name of the style, and so the style
		// is not in sort-order, which prevents the listview from finding it.

		// force styles listbox to be refilled
		UpdateStyleList();
	}


	// Set m_istyiSelected to BasedOn;
	for (iStyle = 0; iStyle < m_vstyi.Size(); iStyle++)
	{
		if (m_vstyi[iStyle].m_hvoStyle == (*pstyiStyleToDelete).m_hvoBasedOn)
		{
			break;
		}
	}
	// This is mainly for robustness. It could happen if we some day allow users to have
	// additional styles that are not based on anything.
	if (iStyle >= m_vstyi.Size())
		iStyle = 0; // Will usually be Normal; at least, it is safe.
	// Don't check old values, we will delete.
	UpdateTabCtrl(m_itabCurrent, iStyle, false); // Actually sets m_istyiSelected.

	// Select the BasedOn style.
	str.Assign(m_vstyi[m_istyiSelected].m_stuName);
	lvfi.psz = str.Chars();
	iItem = ListView_FindItem(m_hwndStylesList, -1, &lvfi);
	ListView_SetItemState(m_hwndStylesList, iItem, LVIS_SELECTED | LVIS_FOCUSED,
		LVIS_SELECTED | LVIS_FOCUSED);
	ListView_EnsureVisible(m_hwndStylesList, iItem, false);
	::SetFocus(m_hwndStylesList);

	// TODO (EberhardB):
	// deleting or renaming styles causes problems when the user presses the Apply button.
	// DataNotebook pops up a message in that case that the selection was lost and ignores
	// the apply, TE tries to apply it but messes up previous instances of that style in
	// the current paragraph. Analysts are thinking of separating the Apply functionality
	// from the styles dialog, so as workaround we disable the Apply button in case of delete or
	// rename (TE-3781).
	m_nEnableApply = kStyleDisableApply;
	::EnableWindow(::GetDlgItem(m_hwnd, kctidOk), false);

	// How do we redraw text that used to be based on the deleted style? If a paragraph/text
	// was based on a deleted style, should it now be drawn using the default style? Or will
	// its ttp be sufficient to contain the information?

	return true;
} //:> AfStylesDlg::CmdDel.


/*----------------------------------------------------------------------------------------------
	Assign values to the member variables of pstyi based on the type "stype", then add the
	style name to the list of styles in the dialog.

	@param stype Style type to be copied
	@param pstyi Style to be changed.
	@return true
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::MakeNewStyi(const StyleType stype, StyleInfo * pstyi)
{
	AssertPtr(pstyi);

	HVO hvoDefault;
	StrAnsi staStyleName;
	int iStyle = m_vstyi.Size(); // Index of new style in the vector of styles.

	// Be sure that the Dirty flag gets set.
	pstyi->m_fDeleted = false;
	pstyi->m_fDirty = true;

	if (pstyi->m_fBuiltIn) // this should never happen, but it doesn't hurt
		pstyi->m_fModified = true;

	// Give the newly created style a temporary, unique hvo until it is added to the database.
	pstyi->m_hvoStyle = m_ihvoNextNewStyi--;

	// Set the style type.
	pstyi->m_st = stype;

	// Get new style name
	switch (stype)
	{
	case kstParagraph:
			staStyleName.Load(kstidNewParaStyleName);
		break;

	case kstCharacter:
			staStyleName.Load(kstidNewCharStyleName);
		break;

	default:
		Assert(false); // Should not happen.
		break;
	}

	StrAnsi staOld;
	staOld = staStyleName;

	// add number to name if name is used. We can't use the list box for finding the name,
	// because it may not show all values
	int iv = 2;
	while (GetIndexFromName(staStyleName) > -1)
	{
		staStyleName.Format("%s %d", staOld.Chars(), iv++); // Start at 2.
	}

	StrUni stuStyle;
	switch (stype)
	{
	case kstParagraph:
		{// Local block for SmartBstr
			// Get the HVO of the Default paragraph style to use as the base for new styles.
			SmartBstr sbstrNormal;
			CheckHr(m_pasts->GetDefaultBasedOnStyleName(&sbstrNormal));
			stuStyle.Assign(sbstrNormal.Chars());
		}
		hvoDefault = GetHvoOfStyleNamed(stuStyle);

		m_nParaStyles++;
		pstyi->m_stuName = staStyleName.Chars();
		SetNext(*pstyi, pstyi->m_hvoStyle); // Sets m_fDirty.
		break;

	case kstCharacter:
		// Get the HVO of the Default Paragraph Characters character style.
		stuStyle.Load(kstidDefParaCharacters);
		hvoDefault = GetHvoOfStyleNamed(stuStyle);

		pstyi->m_hvoNext = 0;
		m_nCharStyles++;
		pstyi->m_stuName = staStyleName.Chars();
		break;

	default:
		Assert(false); // Should not happen.
		break;
	}

	// Create an empty ttp.
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->GetTextProps(&pstyi->m_qttp));

	// Set BasedOn to the default style. Use the SetBasedOn method for integrity checks.
	SetBasedOn(*pstyi, hvoDefault); // Sets m_fDirty.

	// ENHANCE LarryW: Implement m_stuShortcut.

	// Add the style name to the list of styles in the dialog.
	LVITEM lvi;
	lvi.mask = LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM;
	lvi.iImage = (int)stype;
	lvi.iItem = iStyle;
	lvi.iSubItem = 0;
	lvi.lParam = (LPARAM) iStyle;

	StrApp strName(pstyi->m_stuName);
	lvi.pszText = const_cast<achar *>(strName.Chars());
	ListView_InsertItem(m_hwndStylesList, &lvi);

	return true;
} //:> AfStylesDlg::MakeNewStyi.


/*----------------------------------------------------------------------------------------------
	Copy a style to a new one and find right values

	@param pstyiSrc - Source style
	@param pstyiDest - new style
	@return true
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::CopyStyi(const StyleInfo & styiSrc, StyleInfo & styiDest)
{
	// Copy everything that should be copied, initialize the rest appropriately.
	styiDest.m_fDeleted = false;
	styiDest.m_fDirty = true; // ensure it gets saved
	// Usually, give it the same base as the original. If the original paragraph style has
	// no base (i.e., Normal), base it on the original (that is, on Normal). (Note that it is
	// common and acceptable for character styles to be based on no other style.)
	styiDest.m_hvoBasedOn = (styiSrc.m_st == kstCharacter || styiSrc.m_hvoBasedOn) ?
		styiSrc.m_hvoBasedOn :
		styiSrc.m_hvoStyle;
	// Give the newly created style a temporary, unique hvo until it is added to the
	// database.
	styiDest.m_hvoStyle = m_ihvoNextNewStyi--;
	// if next style is the same style, than we have to change it to refer to our new style
	if (styiSrc.m_hvoNext == styiSrc.m_hvoStyle && styiSrc.m_st != kstCharacter)
		styiDest.m_hvoNext = styiDest.m_hvoStyle;
	else
		styiDest.m_hvoNext = styiSrc.m_hvoNext;
	styiDest.m_qttp = styiSrc.m_qttp;
	styiDest.m_st = styiSrc.m_st;
	styiDest.m_fBuiltIn = false;
	styiDest.m_fModified = false;
	styiDest.m_nContext = styiSrc.m_nContext;
	styiDest.m_nStructure = styiSrc.m_nStructure;
	styiDest.m_nFunction = styiSrc.m_nFunction;

	int ccopy = 0; // How many names like "copy of styiSrc.m_stuName" do we have?
	bool fUnique;
	do
	{
		ccopy++;
		if (ccopy == 1)
		{
			StrUni stuFmt(kstidCopyOf); // "Copy of %s".
			styiDest.m_stuName.Format(stuFmt.Chars(), styiSrc.m_stuName.Chars());
		}
		else
		{
			StrUni stuFmt(kstidCopyNOf); // "Copy %d of %s".
			styiDest.m_stuName.Format(stuFmt.Chars(), ccopy, styiSrc.m_stuName.Chars());
		}
		fUnique = (GetIndexFromName(styiDest.m_stuName) == -1);
	} while (!fUnique);

	// styiSrc.m_stuShortcut =""; // Leave shortcut empty (don't make it the same!).

	return true;
}

/*----------------------------------------------------------------------------------------------
	Copy info from m_pasts to our local vector, m_vstyi.
----------------------------------------------------------------------------------------------*/
void AfStylesDlg::CopyToLocal()
{
	int cstyles;
	CheckHr(m_pasts->get_CStyles(&cstyles));
	m_vstyi.Resize(cstyles + 1);

	SmartBstr sbstr;
	StrAnsi staName;
	ISilDataAccessPtr qsda;
	m_pasts->get_DataAccess(&qsda);

	m_istyiSelected = cstyles ? 0 : -1; // Initialize to the first style, if any.
	for (int iStyle = 0; iStyle < cstyles; iStyle++)
	{
		StyleInfo & styi = m_vstyi[iStyle];

		// Get info about the style.
		HVO hvoStyle;
		CheckHr(m_pasts->get_NthStyle(iStyle, &hvoStyle));
		styi.m_hvoStyle = hvoStyle;
		CheckHr(qsda->get_UnicodeProp(hvoStyle, kflidStStyle_Name, &sbstr));
		styi.m_stuName = sbstr.Chars();
		styi.m_stuNameOrig = sbstr.Chars();

		ITsStringPtr qtss;
		CheckHr(qsda->get_MultiStringAlt(hvoStyle, kflidStStyle_Usage, m_wsUser, &qtss));
		SmartBstr sbstrUsage;
		CheckHr(qtss->get_Text(&sbstrUsage));
		styi.m_stuUsage = sbstrUsage.Chars();

		// Set m_istyiSelected if this is the first style to select.
		if ((0 < m_stuParaStyleNameOrig.Length()) && !m_fOnlyCharStyles)
		{
			if (styi.m_stuName == m_stuParaStyleNameOrig)
				m_istyiSelected = iStyle;
		}
		else if (0 < m_stuCharStyleNameOrig.Length())
		{
			if (styi.m_stuName == m_stuCharStyleNameOrig)
				m_istyiSelected = iStyle;
		}

		// Get more info about the style.
		IUnknownPtr qunkTtp;
		CheckHr(qsda->get_UnknownProp(hvoStyle, kflidStStyle_Rules, &qunkTtp));
		CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &styi.m_qttp));
		int ntmp;
		CheckHr(qsda->get_IntProp(hvoStyle, kflidStStyle_Type, & ntmp));
		Assert((unsigned) ntmp < kstLim);
		styi.m_st = (StyleType)ntmp;
		CheckHr(qsda->get_ObjectProp(hvoStyle, kflidStStyle_BasedOn, &styi.m_hvoBasedOn));
		// Check for a possible db problem.
		// Assert(styi.m_stuName.Equals(g_pszwStyleNormal) ||
		//	styi.m_stuName.Equals(kstidDefParaCharacters) ||
		//	styi.m_hvoBasedOn != NULL);
		CheckHr(qsda->get_ObjectProp(hvoStyle, kflidStStyle_Next, &styi.m_hvoNext));
		// ENHANCE LarryW(JohnT): add shortcut to the conceptual model and load it here.
		CheckHr(qsda->get_IntProp(hvoStyle, kflidStStyle_IsBuiltIn, &ntmp));
		styi.m_fBuiltIn = static_cast<bool>(ntmp);
		CheckHr(qsda->get_IntProp(hvoStyle, kflidStStyle_IsModified, &ntmp));
		styi.m_fModified = static_cast<bool>(ntmp);
		CheckHr(qsda->get_IntProp(hvoStyle, kflidStStyle_Context, &ntmp));
		styi.m_nContext = ntmp;
		CheckHr(qsda->get_IntProp(hvoStyle, kflidStStyle_UserLevel, &ntmp));
		styi.m_nUserLevel = ntmp;
		CheckHr(qsda->get_IntProp(hvoStyle, kflidStStyle_Structure, &ntmp));
		styi.m_nStructure = ntmp;
		CheckHr(qsda->get_IntProp(hvoStyle, kflidStStyle_Function, &ntmp));
		styi.m_nFunction = ntmp;

		styi.m_fDirty = false;
		styi.m_fDeleted = false;
	};
	// Make a dummy entry for "Default Paragraph Characters"
	// Because khvoDefChars is zero, styles with no based-on value will automatically
	// appear to be based on this.
	StyleInfo & styi = m_vstyi[cstyles];
	styi.m_hvoStyle = khvoDefChars;
	styi.m_stuName = m_stuDefParaChars;
	styi.m_stuNameOrig = m_stuDefParaChars;
	styi.m_hvoBasedOn = 0; //Based on nothing.
	styi.m_hvoNext = 0; // N/A
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->GetTextProps(&styi.m_qttp)); // no props set at all
	styi.m_st = kstCharacter;
	styi.m_fBuiltIn = true;
	styi.m_fModified = false;
	styi.m_nContext = 0;
	styi.m_nFunction = 0;
	styi.m_nStructure = 0;
	// TODO EberhardB: Add usage
	styi.m_fDirty = false; // Don't let it look as if it should be saved!
	styi.m_fDeleted = false;
	// styi.m_stuShortcut = ; Not implemented.
} //:> AfStylesDlg::CopyToLocal.


/*----------------------------------------------------------------------------------------------
	If anything changed, copy the changes back to the database and style sheet. If there are
	any new styles, give them real hvo's.
	Return false if they decide they don't want to make the changes after all, specifically
	because they've changed the name of a style and don't want to wait to crawl through
	the database.
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::CopyToDb()
{
	// First see if any styles have had their names changed, or if any have been deleted.
	// If so, give the user a chance to back out of saving.
	int istyi;
	Vector<StrUni> vstuDelNames;
	Vector<StrUni> vstuOldNames;
	Vector<StrUni> vstuNewNames;

	Vector<HVO> vhvoRenStyles; // HVOs of styles to rename

	for (istyi = 0; istyi < m_vstyi.Size(); istyi++)
	{
		if (m_vstyi[istyi].m_fDeleted && !m_vstyi[istyi].m_fJustCreated)
		{
			if (m_vstyi[istyi].m_st == kstParagraph)
			{
				// replace with normal style for context;
				StrUni stuRepl = GetStyleForDeletedStyle(m_vstyi[istyi]);
				vstuOldNames.Push(m_vstyi[istyi].m_stuNameOrig);
				vstuNewNames.Push(stuRepl);
			}
			else
			{
				vstuDelNames.Push(m_vstyi[istyi].m_stuNameOrig);
			}
		}
		else if (m_vstyi[istyi].m_hvoStyle >= 0 &&
			m_vstyi[istyi].m_stuName != m_vstyi[istyi].m_stuNameOrig)
		{
			vhvoRenStyles.Push(m_vstyi[istyi].m_hvoStyle);
		}
	}

	// at this point, vstuOldNames contains the paragraph styles that are marked for
	// deletion!
	Assert(vstuOldNames.Size() == vstuNewNames.Size());
	if (!AfStylesWarningDlg::WarnUser(vstuDelNames.Size() + vstuOldNames.Size(),
			vhvoRenStyles.Size(), m_hwnd, m_pszHelpFile))
	{
		return false;
	}
/*	if (vstuOldNames.Size() > 0 || vstuDelNames.Size() > 0)
	{
		StrApp strTitle;
		StrApp strMsg;
		if (vstuOldNames.Size() > 0 && vstuDelNames.Size() > 0)
			strMsg.Load(kstidChgAndDelStyleQuestion);
		else if (vstuDelNames.Size() > 0)
			strMsg.Load(kstidDelStyleQuestion);
		else
			strMsg.Load(kstidChgStyleQuestion);

		int nT = ::MessageBox(NULL, strMsg.Chars(), strTitle.Chars(),
			MB_YESNO | MB_ICONEXCLAMATION | MB_APPLMODAL);
		if (nT != IDYES)
			return false;
	}*/

	// Handle deletions before any other changes. That way, if the user has subsequently
	// inserted a new style with the same name, we won't get into trouble. The second check
	// in the 'if' makes sure we don't try to delete a newly created style.
	for (istyi = 0; istyi < m_vstyi.Size(); istyi++)
	{
		if (m_vstyi[istyi].m_fDeleted && 0 <= m_vstyi[istyi].m_hvoStyle)
		{
			m_pasts->Delete(m_vstyi[istyi].m_hvoStyle);
			m_vstyi.Delete(istyi);
			istyi--; // Consider the style that has now moved into this position.

			// Deletion, even if the only change, requires display update: there may be
			// something visible that is using the deleted style and needs to go back to
			// a default.
			m_fStyleChanged = true;
		}
	}

	// Give new styles real hvo's -- except for any that may have been deleted.
	int nhvoTemp; // Temporary HVO of new style. This is -1000 or less.
	for (istyi = 0; istyi < m_vstyi.Size(); istyi++)
	{
		if (m_vstyi[istyi].m_hvoStyle < 0 && !m_vstyi[istyi].m_fDeleted)
		{
			Assert(m_vstyi[istyi].m_fDirty);

			// Give the new style a real HVO and correct any references.
			nhvoTemp = m_vstyi[istyi].m_hvoStyle;
			// Create the object and obtain a new object ID from the database.
			HVO hvoNewStyle;
			CheckHr(m_pasts->MakeNewStyle(&hvoNewStyle));
			m_vstyi[istyi].m_hvoStyle = hvoNewStyle;

			// If any other style is refering to this new style via Next and/or BasedOn, make
			// the appropriate change.
			for (int i = 0; i < m_vstyi.Size(); i++)
			{
				if (nhvoTemp == m_vstyi[i].m_hvoBasedOn)
				{
					SetBasedOn(m_vstyi[i], hvoNewStyle); // Sets m_fDirty.
				}
				if (nhvoTemp == m_vstyi[i].m_hvoNext)
				{
					SetNext(m_vstyi[i], hvoNewStyle); // Sets m_fDirty.
				}
			}
		}
	}

	// Insert new (and replace modified) styles into the stylesheet.
	for (istyi = 0; istyi < m_vstyi.Size(); istyi++)
	{
		StyleInfo &styi = m_vstyi[istyi];
		// If deleted or not dirty, nothing to do (Deleted should never be the case here)
		if (m_vstyi[istyi].m_fDeleted || !m_vstyi[istyi].m_fDirty)
			continue;
		// Otherwise something here needs saving.
		m_fStyleChanged = true;
		SmartBstr sbstrName(styi.m_stuName);
		SmartBstr sbstrUsage(styi.m_stuUsage);
		CheckHr(m_pasts->PutStyle(
			sbstrName,
			sbstrUsage,
			styi.m_hvoStyle,
			styi.m_hvoBasedOn,
			styi.m_hvoNext,
			styi.m_st,
			styi.m_fBuiltIn,
			styi.m_fModified,
			styi.m_qttp));

		// Get the normalized form of the stylename
		int cStyles;
		CheckHr(m_pasts->get_CStyles(&cStyles));
		for (int i = 0; i < cStyles; i++)
		{
			HVO hvoOfNthStyle;
			CheckHr(m_pasts->get_NthStyle(i, &hvoOfNthStyle));
			if (hvoOfNthStyle == styi.m_hvoStyle)
			{
				bool fSelectedStyle = (m_stuStyleSelected.Equals(m_vstyi[istyi].m_stuName));
				CheckHr(m_pasts->get_NthStyleName(i, &sbstrName));
				m_vstyi[istyi].m_stuName.Assign(sbstrName.Chars());
				if (fSelectedStyle)
					m_stuStyleSelected = m_vstyi[istyi].m_stuName;

				for (int j = 0; j < vhvoRenStyles.Size(); j++)
				{
					if (vhvoRenStyles[j] == styi.m_hvoStyle)
					{
						vstuOldNames.Push(m_vstyi[istyi].m_stuNameOrig);
						vstuNewNames.Push(m_vstyi[istyi].m_stuName);
						break;
					}
				}
				break;
			}
			//if (m_vstyi[istyi].m_fDeleted && !m_vstyi[istyi].m_fJustCreated)
			//{
			//	if (m_vstyi[istyi].m_st == kstParagraph)
			//	{
			//		// replace with normal style for context;
			//		StrUni stuRepl = GetStyleForDeletedStyle(m_vstyi[istyi]);
			//		vstuOldNames.Push(m_vstyi[istyi].m_stuNameOrig);
			//		vstuNewNames.Push(stuRepl);
			//	}
			//	else
			//	{
			//		vstuDelNames.Push(m_vstyi[istyi].m_stuNameOrig);
			//	}
			//}
			//else if (m_vstyi[istyi].m_hvoStyle >= 0 &&
			//	m_vstyi[istyi].m_stuName != m_vstyi[istyi].m_stuNameOrig)
			//{
			//	vstuOldNames.Push(m_vstyi[istyi].m_stuNameOrig);
			//	vstuNewNames.Push(m_vstyi[istyi].m_stuName);
			//}
		}
	}

	Assert(vstuOldNames.Size() == vstuNewNames.Size());
	if (vstuOldNames.Size() > 0 || vstuDelNames.Size() > 0)
	{
		*m_pfReloadDb = true;

		if (m_hvoRootObj)
		{
			// A database type program must use an IVwOleDbDa object underneath the
			// IVwStylesheet, and must provide us with the root object id.
			ISilDataAccessPtr qsda;
			m_pasts->get_DataAccess(&qsda);
			AssertPtr(qsda);
			HRESULT hr;
			IVwOleDbDaPtr qodde;
			hr = qsda->QueryInterface(IID_IVwOleDbDa, (void **)&qodde);
			Assert(hr == S_OK);
			if (SUCCEEDED(hr))
			{
				CheckHr(qodde->Save());
				ISetupVwOleDbDaPtr qods;
				CheckHr(qodde->QueryInterface(IID_ISetupVwOleDbDa, (void **)&qods));
				IUnknownPtr qunk;
				CheckHr(qods->GetOleDbEncap(&qunk));
				IOleDbEncapPtr qode;
				CheckHr(qunk->QueryInterface(IID_IOleDbEncap, (void **)&qode));
				SmartBstr sbstrServer;
				SmartBstr sbstrDatabase;
				CheckHr(qode->get_Server(&sbstrServer));
				CheckHr(qode->get_Database(&sbstrDatabase));

				IFwDbMergeStylesPtr qfdmsStyleMerger;
				qfdmsStyleMerger.CreateInstance(CLSID_FwDbMergeStyles);
				CheckHr(qfdmsStyleMerger->Initialize(sbstrServer, sbstrDatabase, m_qstrmLog,
					m_hvoRootObj, m_pclsidApp));
				// Populate change and deleted lists from contents of vstuOldNames, vstuNewNames, vstuDelNames
				Assert(vstuOldNames.Size() == vstuNewNames.Size());
				for (int i = 0; i < vstuOldNames.Size(); i++)
					CheckHr(qfdmsStyleMerger->AddStyleReplacement(vstuOldNames[i].Bstr(), vstuNewNames[i].Bstr()));
				for (int i = 0; i < vstuDelNames.Size(); i++)
				{
					CheckHr(qfdmsStyleMerger->AddStyleDeletion(vstuDelNames[i].Bstr()));
				}
				CheckHr(qfdmsStyleMerger->Process((DWORD)Hwnd()));
			}
			else
			{
				// warn user??
			}
		}
		else
		{
			// A non-database program (ie, WorldPad) provides its own method to rename and/or
			// delete styles, accessible through its main window subclass.
			AfMainWnd * pafw = MainWindow();
			AssertPtr(pafw);
			if (pafw)
			{
				pafw->RenameAndDeleteStyles(vstuOldNames, vstuNewNames, vstuDelNames);
			}
			else
			{
				// warn user??
			}
		}
	}

	return true;

#ifndef NO_DATABASE_SUPPORT
	// Todo SharonC(JohnT): figure how to save styles in WorldPad.
///	CheckHr(qsda->Save());
#endif
} //:> AfStylesDlg::CopyToDb.


/*----------------------------------------------------------------------------------------------
	Compute the inherited properties for the style in m_vstyi[istyi] based on ppttp.

	@param istyi Index of the style
	@param ppttp The returned TsTextProps of inherited properties.
----------------------------------------------------------------------------------------------*/
void AfStylesDlg::GetInheritedProperties(int istyi, ITsTextProps ** ppttp)
{
	if (istyi < 0)
		return;

	AssertPtr(ppttp);
	Assert(istyi >= 0 && istyi < m_vstyi.Size());
	// First work out the parentage of the currently selected style.
	Vector <int> vistyiParents;
	HVO hvo = m_vstyi[istyi].m_hvoBasedOn;
	while (hvo)
	{
		istyi = GetIndexFromHVO(hvo);
		Assert(istyi >= 0);
		vistyiParents.Push(istyi);
		hvo = m_vstyi[istyi].m_hvoBasedOn;
	}
	// Now build up a pttp using the static IVwStylesheet:: ComputeInheritance method.
	ITsTextPropsPtr qttpInherited = m_qttpDefault; // Begin with the system defaults.
	int istyiParent = vistyiParents.Size() - 1;
	while (istyiParent >= 0)
	{
		ITsTextPropsPtr qttpTemp; // Intermediate qttp for use in loop.
		FwStyledText::ComputeInheritance(qttpInherited,
			m_vstyi[vistyiParents[istyiParent]].m_qttp, &qttpTemp);
		qttpInherited = qttpTemp;
		--istyiParent;
	}
	*ppttp = qttpInherited.Detach();
	return;
}


/*----------------------------------------------------------------------------------------------
	Update Dialog changes. In particular, update the selected tab.

	@param itab Dialog tab index
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::ShowChildDlg(int itab, bool fSave)
{
	Assert((uint)itab < (uint)kcdlgv);
	AssertPtr(m_rgdlgv[itab]);

	if (m_itabCurrent == itab)
		return true;

	if (!m_rgdlgv[itab]->Hwnd())
	{
		HWND hwnd = ::GetFocus();
		m_rgdlgv[itab]->DoModeless(m_hwnd);
		::SetWindowPos(m_rgdlgv[itab]->Hwnd(), NULL, m_dxsClient, m_dysClient, 0, 0,
			SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

		// If this is the first time we are creating the internal dialog and the focus was
		// on the tab control or the style list, Windows moves the focus to the dialog,
		// so set it back.
		if (hwnd == m_hwndTab || hwnd == m_hwndStylesList)
			::SetFocus(hwnd);
	}

	// This needs to come after creating the tab, in case we switch successfully,
	// but before we set the active tab, in case we fail.
	int itabCurrent = m_itabCurrent; // Before UpdateTabCtrl changes it.
	if (!UpdateTabCtrl(itab, m_istyiSelected, fSave) || !m_rgdlgv[itab]->SetActive())
	{
		static bool s_fRecursive = false;
		if (s_fRecursive)
		{
			// This is to keep us from getting into an infinite loop if the user tries to
			// select a new tab and can't, but it fails when we try to set the selection back
			// to the previously selected tab.
			// REVIEW DarrellZ: Can we do something a little more intelligent here?
			Assert(false);
			return false;
		}
		s_fRecursive = true;
		TabCtrl_SetCurSel(m_hwndTab, itabCurrent);
		ShowChildDlg(itabCurrent, false);
		s_fRecursive = false;
		return true;
	}

	::ShowWindow(m_rgdlgv[itab]->Hwnd(), SW_SHOW);

	if (itabCurrent != -1)
		::ShowWindow(m_rgdlgv[itabCurrent]->Hwnd(), SW_HIDE);

#if 0
	if (!UpdateTabCtrl(itab, m_istyiSelected))
	{
		if (m_itabCurrent != -1)
			::ShowWindow(m_rgdlgv[m_itabCurrent]->Hwnd(), SW_SHOW);

		if (itab != -1)
			::ShowWindow(m_rgdlgv[itab]->Hwnd(), SW_HIDE);

		return false;
	}
#endif
	return true;
} //:> AfStylesDlg::ShowChildDlg.


/*----------------------------------------------------------------------------------------------
	Initialize Styles listview control using m_vstyi.

	@return true
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::UpdateStyleList()
{
	::SendMessage(m_hwndStylesList, LVM_DELETEALLITEMS, 0, 0); // Only to repopulate the list.

	LVITEM lvi;
	lvi.mask = LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM;

	int cstyles = m_vstyi.Size();

	StrApp strName;

	int nType;
	int iAddedStyle = 0;
	for (int iStyle = 0; iStyle < cstyles; iStyle++)
	{
		if (SkipStyle(iStyle))
		{
			// don't show this style (e.g. advanced style when we show only
			// basic styles; needed for TE)
			continue;
		}

		StyleInfo & styi = m_vstyi[iStyle];

		// Don't add styles marked as deleted!
		if (styi.m_fDeleted)
			continue;

		// The image is to be based on the type of style.
		nType = styi.m_st;

		if (m_fOnlyCharStyles && (StyleType)nType != kstCharacter)
		{
			continue;
		}

		// Adjust the type by 2 if it is the current selected type.
		if ((StyleType)nType == kstParagraph)
		{
			if (m_fParaStyleFound && m_stuParaStyleNameOrig == styi.m_stuName.Chars())
				nType = nType + 2;
		}
		else if ((StyleType)nType == kstCharacter)
		{
			if (m_fCharStyleFound && m_stuCharStyleNameOrig == styi.m_stuName.Chars())
				nType = nType + 2;
		}
		else
			Assert(false); // This shouldn't happen.

		lvi.iImage = nType;
		lvi.iItem = iAddedStyle; // This may be modified by the control due to sorting.
		iAddedStyle++;
		lvi.iSubItem = 0;
		lvi.lParam = (LPARAM) iStyle; // Keep this index into the styles in the sheet.

		// Insert the style name into the list view.
		strName = styi.m_stuName.Chars(); // Convert to App text.
		lvi.pszText = const_cast<achar *>(strName.Chars());
		ListView_InsertItem(m_hwndStylesList, &lvi);
	}

	return true;
} //:> AfStylesDlg::UpdateStyleList.


/*----------------------------------------------------------------------------------------------
	Add styiNew to the list of styles.  Select the new style then find the new style in the
	listview and select it there.

	@param styiNew The style to be added.
----------------------------------------------------------------------------------------------*/
void AfStylesDlg::InstallStyle(StyleInfo & styiNew)
{
	int iStyle = m_vstyi.Size();
	m_vstyi.EnsureSpace(iStyle + 1);
	m_vstyi.Push(styiNew);

	// Select the new style, assuming we can validate any changes to the old one.
	if (!UpdateTabCtrl(m_itabCurrent, iStyle))
		return;

	// Find the new style in the listview and select it.
	StrApp str(m_vstyi[iStyle].m_stuName);
	LVFINDINFO lvfi;
	lvfi.flags = LVFI_STRING;
	lvfi.psz = str.Chars();
	int iItem = ListView_FindItem(m_hwndStylesList, -1, &lvfi);
	ListView_SetItemState(m_hwndStylesList, iItem, LVIS_SELECTED | LVIS_FOCUSED,
		LVIS_SELECTED | LVIS_FOCUSED);
	ListView_EnsureVisible(m_hwndStylesList, iItem, false);
	::SendMessage(m_hwnd, WM_NEXTDLGCTL, (WPARAM)m_hwndStylesList, true);
	ListView_EditLabel(m_hwndStylesList, iItem);
}


/*----------------------------------------------------------------------------------------------
	Update the Style name.

	@param plvdi ListView Display Info that has the new name in it.
	@param lnRet return value to be returned to the windows command.
	@return true if successful.
----------------------------------------------------------------------------------------------*/
bool AfStylesDlg::OnEndLabelEdit(NMLVDISPINFO * plvdi, long & lnRet)
{
	AssertPtr(plvdi);
	StrUni stuName(plvdi->item.pszText);

	if (stuName.Equals(m_vstyi[m_istyiSelected].m_stuName))
		return true;

	if (plvdi->item.pszText)
	{
		AssertPsz(plvdi->item.pszText);

		// Trim leading and trailing space characters.
		StrUtil::TrimWhiteSpace(plvdi->item.pszText, stuName);

		if (SetName(m_vstyi[m_istyiSelected], stuName))
		{
			// Save the new name of the newly added style.
			m_stuStyleSelected = m_vstyi[m_istyiSelected].m_stuName;
			lnRet = true;

			// Ask the General tab, if it is showing, to redraw the name.
			StrApp strName(stuName);
			if (m_itabCurrent == m_itabInitial)
			{
				FmtGenDlg * pfgd = dynamic_cast<FmtGenDlg *>(m_rgdlgv[0].Ptr());
				pfgd->SetName(strName); // Set the name in the General tab.
			}

			// This was Sharon's unsuccessful attempt to get it to update the list box
			// in case blanks had trimmed:
			//ListView_SetItemText(m_hwndStylesList, plvdi->item.iItem, 0,
			//	const_cast<achar *>(strName.Chars()));
			//ListView_RedrawItems(m_hwndStylesList, plvdi->item.iItem, plvdi->item.iItem);
			//::UpdateWindow(m_hwndStylesList);

			return true;
		}
	}
	return false;
} //:> AfStylesDlg::OnEndLabelEdit.

/*----------------------------------------------------------------------------------------------
	Make sure the Normal style has a value set for every property that will show up in the
	dialog.
----------------------------------------------------------------------------------------------*/
void AfStylesDlg::FullyInitializeNormalStyle(IVwStylesheet * past)
{
	int cstyles;
	CheckHr(past->get_CStyles(&cstyles));
	ISilDataAccessPtr qsda;
	past->get_DataAccess(&qsda);

	HVO hvoNormal;
	HVO hvoBasedOn;
	HVO hvoNext;
	ITsTextPropsPtr qttpNormal;
	int st;
	int ntmp = 0;
	bool fBuiltIn = true;
	bool fModified = false;
	StrUni stuNormal(g_pszwStyleNormal);
	int istyle;
	for (istyle = 0; istyle < cstyles; istyle++)
	{
		HVO hvoStyle;
		CheckHr(past->get_NthStyle(istyle, &hvoStyle));
		SmartBstr sbstrName;
		CheckHr(qsda->get_UnicodeProp(hvoStyle, kflidStStyle_Name, &sbstrName));

		if (sbstrName == stuNormal)
		{
			hvoNormal = hvoStyle;
			IUnknownPtr qunkTtp;
			CheckHr(qsda->get_UnknownProp(hvoStyle, kflidStStyle_Rules, &qunkTtp));
			CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttpNormal));
			CheckHr(qsda->get_IntProp(hvoStyle, kflidStStyle_Type, &st));
			Assert(st == kstParagraph);
			CheckHr(qsda->get_ObjectProp(hvoStyle, kflidStStyle_BasedOn, &hvoBasedOn));
			CheckHr(qsda->get_ObjectProp(hvoStyle, kflidStStyle_Next, &hvoNext));

			CheckHr(qsda->get_IntProp(hvoStyle, kflidStStyle_IsBuiltIn, &ntmp));
			fBuiltIn = static_cast<bool>(ntmp);
			CheckHr(qsda->get_IntProp(hvoStyle, kflidStStyle_IsModified, &ntmp));
			fModified = static_cast<bool>(ntmp);
			// TODO EberhardB: Add usage
			break;
		}
	}
	if (istyle >= cstyles)
	{
		// TODO: create a Normal style
		Warn("No Normal style");
		return;
	}

	COLORREF clrDefaultBack = ::GetSysColor(COLOR_WINDOW);
	COLORREF clrDefaultFore = ::GetSysColor(COLOR_WINDOWTEXT);

	ITsPropsBldrPtr qtpb;
	CheckHr(qttpNormal->GetBldr(&qtpb));
	SetIntPropIfBlank(qtpb, ktptRightToLeft, ktpvEnum, 0);
	SetIntPropIfBlank(qtpb, ktptAlign, ktpvEnum, ktalLeading);
	SetIntPropIfBlank(qtpb, ktptFirstIndent, ktpvMilliPoint, 0);
	SetIntPropIfBlank(qtpb, ktptLeadingIndent, ktpvMilliPoint, 0);
	SetIntPropIfBlank(qtpb, ktptTrailingIndent, ktpvMilliPoint, 0);
	SetIntPropIfBlank(qtpb, ktptSpaceBefore, ktpvMilliPoint, 0);
	SetIntPropIfBlank(qtpb, ktptSpaceAfter, ktpvMilliPoint, 0);
	SetIntPropIfBlank(qtpb, ktptLineHeight, ktpvMilliPoint,
		10000);  // should match default text size in FmtFntDlg
	SetIntPropIfBlank(qtpb, ktptBackColor, ktpvDefault, clrDefaultBack);
	SetIntPropIfBlank(qtpb, ktptBorderTop, ktpvMilliPoint, 0);
	SetIntPropIfBlank(qtpb, ktptBorderBottom, ktpvMilliPoint, 0);
	SetIntPropIfBlank(qtpb, ktptBorderLeading, ktpvMilliPoint, 0);
	SetIntPropIfBlank(qtpb, ktptBorderTrailing, ktpvMilliPoint, 0);
	SetIntPropIfBlank(qtpb, ktptBorderColor, ktpvDefault, clrDefaultFore);
	SetIntPropIfBlank(qtpb, ktptBulNumScheme, ktpvEnum, 0);
	SetIntPropIfBlank(qtpb, ktptBulNumStartAt, ktpvDefault, 1);

	// Set up the font information for bullets and numbers.
	SmartBstr sbstrVal;
	CheckHr(qttpNormal->GetStrPropValue(ktptBulNumFontInfo, &sbstrVal));
	StrUni stuFont(sbstrVal.Chars(), sbstrVal.Length());
	ITsPropsBldrPtr qtpbBulNumFont;
	qtpbBulNumFont.CreateInstance(CLSID_TsPropsBldr);
	FmtBulNumDlg::DecodeFontInfo(stuFont, qtpbBulNumFont);

	SetIntPropIfBlank(qtpbBulNumFont, ktptFontSize, ktpvMilliPoint, 10000);
	SetIntPropIfBlank(qtpbBulNumFont, ktptBold, ktpvEnum, kttvOff);
	SetIntPropIfBlank(qtpbBulNumFont, ktptItalic, ktpvEnum, kttvOff);
	SetIntPropIfBlank(qtpbBulNumFont, ktptSuperscript, ktpvEnum, kssvOff);
	SetIntPropIfBlank(qtpbBulNumFont, ktptForeColor, ktpvDefault, clrDefaultFore);
	SetIntPropIfBlank(qtpbBulNumFont, ktptBackColor, ktpvDefault, clrDefaultBack);
	SetIntPropIfBlank(qtpbBulNumFont, ktptUnderColor, ktpvDefault, clrDefaultFore);
	SetIntPropIfBlank(qtpbBulNumFont, ktptUnderline, ktpvEnum, kuntNone);
	SetIntPropIfBlank(qtpbBulNumFont, ktptOffset, ktpvMilliPoint, 0);

	HRESULT hr;
	CheckHr(hr = qtpbBulNumFont->GetStrPropValue(ktptFontFamily, &sbstrVal));
	if (hr == S_FALSE)
	{
		SmartBstr sbstrTnr(L"Times New Roman");
		CheckHr(qtpbBulNumFont->SetStrPropValue(ktptFontFamily, sbstrTnr));
	}

	ITsTextPropsPtr qttpBulNumFont;
	CheckHr(qtpbBulNumFont->GetTextProps(&qttpBulNumFont));
	StrUni stuBulNumFont = FmtBulNumDlg::EncodeFontInfo(qttpBulNumFont);
	CheckHr(qtpb->SetStrPropValue(ktptBulNumFontInfo, stuBulNumFont.Bstr()));

	// No point in setting any other string values, because setting them to an
	// empty string has no effect.

	// Set defaults for font properties for each writing system.
	SetIntPropIfBlank(qtpb, ktptFontSize, ktpvMilliPoint, 10000);
	SetIntPropIfBlank(qtpb, ktptBold, ktpvEnum, kttvOff);
	SetIntPropIfBlank(qtpb, ktptItalic, ktpvEnum, kttvOff);
	SetIntPropIfBlank(qtpb, ktptSuperscript, ktpvEnum, kssvOff);
	SetIntPropIfBlank(qtpb, ktptOffset, ktpvMilliPoint, 0);
	SetIntPropIfBlank(qtpb, ktptForeColor, ktpvDefault, kclrBlack);
	SetIntPropIfBlank(qtpb, ktptBackColor, ktpvDefault, kclrTransparent);
	SetIntPropIfBlank(qtpb, ktptUnderColor, ktpvDefault, kclrBlack);
	SetIntPropIfBlank(qtpb, ktptUnderline, ktpvEnum, kuntNone);
	SmartBstr sbstrFF;
	CheckHr(qtpb->GetStrPropValue(ktptFontFamily, &sbstrFF));
	if (sbstrFF.Length() == 0)
	{
		StrUni stuFFDef(g_pszDefaultSerif);
		CheckHr(qtpb->SetStrPropValue(ktptFontFamily, stuFFDef.Bstr()));
	}

	// Copy any WsStyle info that isn't a duplicate of the defaults.
	// It isn't necessary for any values to be stored for particular writing systems because
	// they use the defaults we just set.
	SmartBstr sbstrWsStyle;
	CheckHr(qttpNormal->GetStrPropValue(ktptWsStyle, &sbstrWsStyle));
	if (sbstrWsStyle.Length())
	{
		StrUni stuWsStyle(sbstrWsStyle.Chars(), sbstrWsStyle.Length());
		stuWsStyle = FwStyledText::RemoveSpuriousOverrides(stuWsStyle, qtpb);
		if (stuWsStyle.Length() == 0)
			CheckHr(qtpb->SetStrPropValue(ktptWsStyle, NULL));	// delete any WsStyle value.
		else
			CheckHr(qtpb->SetStrPropValue(ktptWsStyle, stuWsStyle.Bstr()));
	}

	// Store the new set of properties in the style. Use this special method so we don't
	// generate an Undo action or database changes needing saving.
	CheckHr(qtpb->GetTextProps(&qttpNormal));
	CheckHr(past->CacheProps(stuNormal.Length(), const_cast<OLECHAR *>(stuNormal.Chars()),
		hvoNormal, qttpNormal));

} // FullyInitializeNormalStyle

/*----------------------------------------------------------------------------------------------
	Set a property to the given default if there is not already a value recorded.
----------------------------------------------------------------------------------------------*/
void AfStylesDlg::SetIntPropIfBlank(ITsPropsBldr * ptpb, int tpt, int nVar, int nVal)
{
	int nVarOld, nValOld;
	CheckHr(ptpb->GetIntPropValues(tpt, &nVarOld, &nValOld));
	if (nValOld == -1)
		ptpb->SetIntPropValues(tpt, nVar, nVal);
}

/*----------------------------------------------------------------------------------------------
	Insert the sub dialogs in vector. This can be overridden to create application specific
	tabs, e.g. in TE.
----------------------------------------------------------------------------------------------*/
void AfStylesDlg::SetupTabs()
{
	Assert(kcdlgv == 5); // Ensure that the number of dialogs is what we expect.
	m_hwndTab = ::GetDlgItem(m_hwnd, kctidTabDlgs);

	m_rgdlgv[0].Attach(NewObj FmtGenDlg(this, m_nMsrSys));
	m_rgdlgv[1].Attach(NewObj AfStyleFntDlg(this));
	m_rgdlgv[2].Attach(NewObj FmtParaDlg(this, m_nMsrSys));
	AssertPtr(m_qwsf);
	m_rgdlgv[3].Attach(NewObj FmtBulNumDlg(this, m_qwsf));
	m_rgdlgv[4].Attach(NewObj FmtBdrDlgPara(this));
}

/*----------------------------------------------------------------------------------------------
	Set the values for the dialog controls based
----------------------------------------------------------------------------------------------*/
void AfStylesDlg::SetDialogValues()
{
	UpdateStyleList(); // Initialize Styles listview control using m_vstyi.

	// Select the current style in the list view control.
	LVFINDINFO plvfi;
	DWORD dwT = LVIS_SELECTED | LVIS_FOCUSED;
	if (m_vstyi.Size())
	{
		if (m_istyiSelected >= 0)
		{
			StrApp strName(m_vstyi[m_istyiSelected].m_stuName);
			plvfi.psz = strName.Chars();
			plvfi.flags = LVFI_STRING;
			int iListItem; // Index of strName in the listview.
			iListItem = ListView_FindItem(m_hwndStylesList, -1, &plvfi);
			ListView_SetItemState(m_hwndStylesList, iListItem, dwT, dwT);
			ListView_EnsureVisible(m_hwndStylesList, iListItem, false);
		}
		::SetFocus(m_hwndStylesList);
	}

}

/*----------------------------------------------------------------------------------------------
	Write a font property to the given string.
----------------------------------------------------------------------------------------------*/
//void AfStylesDlg::WriteFntProp(OLECHAR * & pch, int tpt, int nVar, int nVal, int & cprop)
//{
//	cprop++;
//	*pch++ = (OLECHAR) tpt;
//	*pch++ = (OLECHAR) nVar;
//	*pch++ = (OLECHAR) nVal;
//	*pch++ = (OLECHAR) (nVal >> 16);
//}


//template Vector<StyleInfo>; // StyleInfoVec. Hungarian vstyi.

// TODO JohnT: For the Font tab, make sure all root boxes are initialized to use the stylesheet.
// Figure why the heading 2 right border is half an inch from the text, and fix.
// Style can be brought up even if no text selection (but Apply is disabled).
// *** If it is a paragraph style it can be Applied without text being selected (JohnL).



//:>********************************************************************************************
//:>	AfStylesWarningDlg Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Starting point for executing dialog.
	@param nDeletes Number of character styles marked for deletion.
	@param nRenames Number of styles marked for renaming.
	@param hwnd handle of window to be used as dialog parent
	@return True if OK to proceed with changes.
----------------------------------------------------------------------------------------------*/
bool AfStylesWarningDlg::WarnUser(int nDeletes, int nRenames, HWND hwnd,
	const achar * pszHelpFile)
{
	if (!nDeletes && !nRenames)
		return true;

	AfStylesWarningDlg afswd;
	afswd.Initialize(nDeletes, nRenames, pszHelpFile);
	switch (afswd.DoModal(hwnd))
	{
	case 2:
		// Either the No button or the close box was pressed.
		return false; // No further action.

	case 1:
		// OK button was pressed.
		return true;

	default:
		Assert(false);
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
AfStylesWarningDlg::AfStylesWarningDlg()
{
	m_rid = kridDelAndChgStylesWarningDlg;
	m_pszHelpUrl = _T("Advanced_Tasks/Customizing_Styles/Delete_a_style.htm");

	m_nDeletes = 0;
	m_nRenames = 0;
}

void AfStylesWarningDlg::Initialize(int nDeletes, int nRenames, const achar * pszHelpFile)
{
	m_nDeletes = nDeletes;
	m_nRenames = nRenames;
	m_pszHelpFile = pszHelpFile;
}

/*----------------------------------------------------------------------------------------------
	Handle window messages. Process message for the warning text, to color it red.
----------------------------------------------------------------------------------------------*/
bool AfStylesWarningDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);
	Assert(m_hwnd);

	if (wm == WM_CTLCOLORSTATIC)
	{
		if ((HWND)lp == ::GetDlgItem(m_hwnd, kctidDelAndChgStylesWarning))
		{
			// This enables us to set the color of the warning message.
			::SetTextColor((HDC)wp, kclrRed);
			::SetBkColor((HDC)wp, GetSysColor(COLOR_3DFACE));
			// This next line signals to Windows that we've altered the device context,
			// as well as telling it to stick with the dialog color for unused space within
			// static control.
			lnRet = (long)GetSysColorBrush(COLOR_3DFACE);
			return true;
		}
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

bool AfStylesWarningDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	Assert(m_nDeletes > 0 || m_nRenames > 0);

	StrApp strTitle;
	StrApp strMessage;
	StrApp strMsgFmt;

	if (m_nDeletes == 1 && m_nRenames == 0)
	{
		strTitle.Load(kstidDelStyleTitle);
		strMessage.Load(kstidDelStyleQuestion);
	}
	else if (m_nDeletes == 0 && m_nRenames == 1)
	{
		strTitle.Load(kstidChgStyleTitle);
		strMessage.Load(kstidChgStyleQuestion);
	}
	else if (m_nDeletes > 1 && m_nRenames == 0)
	{
		strTitle.Load(kstidDelStylesTitle);
		strMsgFmt.Load(kstidDelStylesQuestion);
		strMessage.Format(strMsgFmt.Chars(), m_nDeletes);
	}
	else if (m_nDeletes == 0 && m_nRenames > 1)
	{
		strTitle.Load(kstidChgStylesTitle);
		strMsgFmt.Load(kstidChgStylesQuestion);
		strMessage.Format(strMsgFmt.Chars(), m_nRenames);
	}
	else
	{
		strTitle.Load(kstidDelAndChgStylesTitle);
		strMsgFmt.Load(kstidDelAndChgStylesQuestion);
		strMessage.Format(strMsgFmt.Chars(), m_nRenames + m_nDeletes);
	}

	// Set up help info for the button on this dialog box. (I don't know what I'm doing, so
	// if it doesn't look right it's probably wrong.) -MarkS, 2004.10.19
//	SmartBstr sbstrHelpUrl;
//	StrUni stu;
//	stu.Format(L"khtpDeletingStyle");
//	HRESULT hr = m_qhtprov->GetHelpString(stu.Bstr(), 0, &sbstrHelpUrl);
//	if (SUCCEEDED(hr) && sbstrHelpUrl.Length())
//	{
//		StrApp strHelpUrl(sbstrHelpUrl.Chars());
//		this->SetHelpUrl(strHelpUrl.Chars());
//	}

	::SendMessage(m_hwnd, WM_SETTEXT, 0, (long)strTitle.Chars());
	::SendDlgItemMessage(m_hwnd, kctidDelAndChgStylesText, WM_SETTEXT, 0,
		(long)strMessage.Chars());

	HICON hicon = ::LoadIcon(NULL, IDI_EXCLAMATION);
	if (hicon)
	{
		HWND hwnd = ::GetDlgItem(m_hwnd, kctidDelAndChgStylesIcon);
		::SendMessage(hwnd, STM_SETICON, (WPARAM)hicon, (LPARAM)0);
	}

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}
/*----------------------------------------------------------------------------------------------
	Show the proper help page from the help file.
	@return true
----------------------------------------------------------------------------------------------*/
bool AfStylesWarningDlg::OnHelp()
{
	AssertPsz(m_pszHelpFile);
	AfUtil::ShowHelpFile(m_pszHelpFile, m_pszHelpUrl);
	return true;
}
