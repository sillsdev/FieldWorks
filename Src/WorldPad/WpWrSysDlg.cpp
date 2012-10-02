/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpWrSysDlg.cpp
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Implements the behavior of the old writing system set-up dialog.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

//:End Ignore

//:>********************************************************************************************
//:>	System-defined language identifiers
//:>********************************************************************************************

struct LangIDMapping
{
	const achar * m_pszName;
	int m_n;
};
static LangIDMapping g_rglangid[] =
{
	{ _T("<default>"),						0x0000 },	// will be treated as US English
	{ _T("Other"),							0x05FE },
	{ _T("Afrikaans"),						0x0436 },
	{ _T("Albanian"),						0x041c },
	{ _T("Amharic"),						0x045e },
	{ _T("Arabic (Algeria)"),				0x1401 },
	{ _T("Arabic (Bahrain)"),				0x3c01 },
	{ _T("Arabic (Egypt)"),					0x0c01 },
	{ _T("Arabic (Iraq)"),					0x0801 },
	{ _T("Arabic (Jordan)"),				0x2c01 },
	{ _T("Arabic (Kuwait)"),				0x3401 },
	{ _T("Arabic (Lebanon)"),				0x3001 },
	{ _T("Arabic (Libya)"),					0x1001 },
	{ _T("Arabic (Morocco)"),				0x1801 },
	{ _T("Arabic (Oman)"),					0x2001 },
	{ _T("Arabic (Qatar)"),					0x4001 },
	{ _T("Arabic (Saudi Arabia)"),			0x0401 },
	{ _T("Arabic (Syria)"),					0x2801 },
	{ _T("Arabic (Tunisia)"),				0x1c01 },
	{ _T("Arabic (U.A.E.)"),				0x3801 },
	{ _T("Arabic (Yemen)"),					0x2401 },
	{ _T("Armenian"),						0x042b },
	{ _T("Assamese"),						0x044d },
	{ _T("Azeri (Cyrillic)"),				0x082c },
	{ _T("Azeri (Latin)"),					0x042c },
	{ _T("Basque"),							0x042d },
	{ _T("Belarusian"),						0x0423 },
	{ _T("Bengali (Bangladesh)"),			0x0845 },
	{ _T("Bengali (India)"),				0x0445 },
	{ _T("Bulgarian"),						0x0402 },
	{ _T("Burmese"),						0x0455 },
	{ _T("Catalan"),						0x0403 },
	{ _T("Cherokee"),						0x045c },
	{ _T("Chinese (Hong Kong)"),			0x0c04 },
	{ _T("Chinese (Macau SAR)"),			0x1404 },
	{ _T("Chinese (PRC)"),					0x0804 },
	{ _T("Chinese (Singapore)"),			0x1004 },
	{ _T("Chinese (Taiwan)"),				0x0404 },
	{ _T("Croatian (Bosnia/Herzegovina)"),	0x101a },
	{ _T("Croatian (Croatia)"),				0x041a },
	{ _T("Czech"),							0x0405 },
	{ _T("Danish"),							0x0406 },
	{ _T("Divehi"),							0x0465 },
	{ _T("Dutch (Belgium)"),				0x0813 },
	{ _T("Dutch (Netherlands)"),			0x0413 },
	{ _T("Dzongkha (Bhutan)"),				0x0851 },
	{ _T("Edo"),							0x0466 },
	{ _T("English (Australia)"),			0x0c09 },
	{ _T("English (Belize)"),				0x2809 },
	{ _T("English (Canada)"),				0x1009 },
	{ _T("English (Caribbean)"),			0x2409 },
	{ _T("English (Hong Kong)"),			0x3c09 },
	{ _T("English (India)"),				0x4009 },
	{ _T("English (Indonesia)"),			0x3809 },
	{ _T("English (Ireland)"),				0x1809 },
	{ _T("English (Jamaica)"),				0x2009 },
	{ _T("English (Malaysia)"),				0x4409 },
	{ _T("English (New Zealand)"),			0x1409 },
	{ _T("English (Philippines)"),			0x3409 },
	{ _T("English (Singapore)"),			0x4809 },
	{ _T("English (South Africa)"),			0x1c09 },
	{ _T("English (Trinidad & Tobago)"),	0x2c09 },
	{ _T("English (United Kingdom)"),		0x0809 },
	{ _T("English (United States)"),		0x0409 },
	{ _T("English (Zimbabwe)"),				0x3009 },
	{ _T("Estonian"),						0x0425 },
	{ _T("Faeroese"),						0x0438 },
	{ _T("Farsi"),							0x0429 },
	{ _T("Filipino"),						0x0464 },
	{ _T("Finnish"),						0x040b },
	{ _T("French (Belgium)"),				0x080c },
	{ _T("French (Cameroon)"),				0x2c0c },
	{ _T("French (Canada)"),				0x0c0c },
	{ _T("French (Cote d'Ivoire)"),			0x300c },
	{ _T("French (France)"),				0x040c },
	{ _T("French (Haiti)"),					0x3c0c },
	{ _T("French (Luxembourg)"),			0x140c },
	{ _T("French (Mali)"),					0x340c },
	{ _T("French (Morocco)"),				0x380c },
	{ _T("French (Monaco)"),				0x180c },
	{ _T("French (Reunion)"),				0x200c },
	{ _T("French (Senegal)"),				0x280c },
	{ _T("French (Switzerland)"),			0x100c },
	{ _T("French (West Indies)"),			0x1c0c },
	{ _T("French (Zaire)"),					0x240c },
	{ _T("Frisian"),						0x0462 },
	{ _T("Fulfulde"),						0x0467 },
	{ _T("Gaelic (Irish)"),					0x083c },
	{ _T("Gaelic (Scots)"),					0x043c },
	{ _T("Galician"),						0x0456 },
	{ _T("Georgian"),						0x0437 },
	{ _T("German (Austria)"),				0x0c07 },
	{ _T("German (Germany)"),				0x0407 },
	{ _T("German (Liechtenstein)"),			0x1407 },
	{ _T("German (Luxembourg)"),			0x1007 },
	{ _T("German (Switzerland)"),			0x0807 },
	{ _T("Greek"),							0x0408 },
	{ _T("Guarani"),						0x0474 },
	{ _T("Gujarati"),						0x0447 },
	{ _T("Hausa"),							0x0468 },
	{ _T("Hawaiian"),						0x0475 },
	{ _T("Hebrew"),							0x040d },
	{ _T("Hindi"),							0x0439 },
	{ _T("Hungarian"),						0x040e },
	{ _T("Ibibio"),							0x0469 },
	{ _T("Icelandic"),						0x040f },
	{ _T("Igbo"),							0x0470 },
	{ _T("Indonesian"),						0x0421 },
	{ _T("Inuktitut"),						0x045d },
	{ _T("Italian (Italy)"),				0x0410 },
	{ _T("Italian (Switzerland)"),			0x0810 },
	{ _T("Japanese"),						0x0411 },
	{ _T("Kannada"),						0x044b },
	{ _T("Kanuri"),							0x0471 },
	{ _T("Kashmiri (India)"),				0x0860 },
	{ _T("Kashmiri (Kashmir)"),				0x0460 },
	{ _T("Kazakh"),							0x043f },
	{ _T("Khmer"),							0x0453 },
	{ _T("Konkani"),						0x0457 },
	{ _T("Korean"),							0x0412 },
	{ _T("Lao"),							0x0454 },
	{ _T("Latin"),							0x0476 },
	{ _T("Latvian"),						0x0426 },
	{ _T("Lithuanian"),						0x0427 },
	{ _T("Macedonian"),						0x042f },
	{ _T("Malay (Brunei Darussalam)"),		0x083e },
	{ _T("Malay (Malaysia)"),				0x043e },
	{ _T("Malayalam"),						0x044c },
	{ _T("Maltese"),						0x043a },
	{ _T("Manipuri"),						0x0458 },
	{ _T("Marathi"),						0x044e },
	{ _T("Mongolian (Cyrillic)"),			0x0450 },
	{ _T("Mongolian (Mongolian)"),			0x0850 },
	{ _T("Nepali (India)"),					0x0861 },
	{ _T("Nepali (Nepal)"),					0x0461 },
	{ _T("Norwegian (Bokmål)"),				0x0414 },
	{ _T("Norwegian (Nynorsk)"),			0x0814 },
	{ _T("Oriya"),							0x0448 },
	{ _T("Oromo "),							0x0472 },
	{ _T("Pashto"),							0x0463 },
	{ _T("Polish"),							0x0415 },
	{ _T("Portuguese (Brazil)"),			0x0416 },
	{ _T("Portuguese (Portugal)"),			0x0816 },
	{ _T("Punjabi (Arabic)"),				0x0846 },
	{ _T("Punjabi (Gurmukhi)"),				0x0446 },
	{ _T("Rhaeto-Romanic"),					0x0417 },
	{ _T("Romanian (Moldava)"),				0x0818 },
	{ _T("Romanian (Romania)"),				0x0418 },
	{ _T("Russian (Moldava)"),				0x0819 },
	{ _T("Russian (Russia)"),				0x0419 },
	{ _T("Sami"),							0x043b },
	{ _T("Sanskrit"),						0x044f },
	{ _T("Serbian (Cyrillic)"),				0x0c1a },
	{ _T("Serbian (Latin)"),				0x081a },
	{ _T("Sindhi (India)"),					0x0459 },
	{ _T("Sindhi (Pakistan)"),				0x0859 },
	{ _T("Sinhalese"),						0x045b },
	{ _T("Slovak"),							0x041b },
	{ _T("Slovenian"),						0x0424 },
	{ _T("Somali"),							0x0477 },
	{ _T("Sorbian"),						0x042e },
	{ _T("Spanish (Argentina)"),			0x2c0a },
	{ _T("Spanish (Bolivia)"),				0x400a },
	{ _T("Spanish (Chile)"),				0x340a },
	{ _T("Spanish (Colombia)"),				0x240a },
	{ _T("Spanish (Costa Rica)"),			0x140a },
	{ _T("Spanish (Dominican Republic)"),	0x1c0a },
	{ _T("Spanish (Ecuador)"),				0x300a },
	{ _T("Spanish (El Salvador)"),			0x440a },
	{ _T("Spanish (Guatemala)"),			0x100a },
	{ _T("Spanish (Honduras)"),				0x480a },
	{ _T("Spanish (Mexico)"),				0x080a },
	{ _T("Spanish (Nicaragua)"),			0x4c0a },
	{ _T("Spanish (Panama)"),				0x180a },
	{ _T("Spanish (Paraguay)"),				0x3c0a },
	{ _T("Spanish (Peru)"),					0x280a },
	{ _T("Spanish (Puerto Rico)"),			0x500a },
	{ _T("Spanish (Modern Sort)"),			0x0c0a },
	{ _T("Spanish (Traditional Sort)"),		0x040a },
	{ _T("Spanish (Uruguay)"),				0x380a },
	{ _T("Spanish (Venezuela)"),			0x200a },
	{ _T("Sutu"),							0x0430 },
	{ _T("Swahili"),						0x0441 },
	{ _T("Swedish (Finland)"),				0x081d },
	{ _T("Swedish (Sweden)"),				0x041d },
	{ _T("Syriac"),							0x045a },
	{ _T("Tajik"),							0x0428 },
	{ _T("Tamazight (Berber/Arabic)"),		0x045f },
	{ _T("Tamazight (Latin)"),				0x085f },
	{ _T("Tamil"),							0x0449 },
	{ _T("Tatar"),							0x0444 },
	{ _T("Telugu"),							0x044a },
	{ _T("Thai"),							0x041e },
	{ _T("Tibetan (Tibet)"),				0x0451 },
	{ _T("Tigrigna (Eritrea)"),				0x0873 },
	{ _T("Tigrigna (Ethiopia)"),			0x0473 },
	{ _T("Tsonga"),							0x0431 },
	{ _T("Tswana"),							0x0432 },
	{ _T("Turkish"),						0x041f },
	{ _T("Turkmen"),						0x0442 },
	{ _T("Ukrainian"),						0x0422 },
	{ _T("Urdu (India)"),					0x0820 },
	{ _T("Urdu (Pakistan)"),				0x0420 },
	{ _T("Uzbek (Cyrillic)"),				0x0843 },
	{ _T("Uzbek (Latin)"),					0x0443 },
	{ _T("Venda"),							0x0433 },
	{ _T("Vietnamese"),						0x042a },
	{ _T("Welsh"),							0x0452 },
	{ _T("Xhosa"),							0x0434 },
	{ _T("Yi"),								0x0478 },
	{ _T("Yiddish"),						0x043d },
	{ _T("Yoruba"),							0x046a },
	{ _T("Zulu"),							0x0435 },
};
static const int g_clangid = isizeof(g_rglangid) / isizeof(LangIDMapping);


//:>********************************************************************************************
//:>	Writing Systems dialog
//:>********************************************************************************************

// For putting the focus back on combo-boxes after an error:
BEGIN_CMD_MAP(WpWrSysDlg)
	ON_CID_CHILD(kctidFocusFonts, &WpWrSysDlg::CmdFocusFonts, NULL)
	ON_CID_CHILD(kctidFocusKeyboard, &WpWrSysDlg::CmdFocusKeyboardType, NULL)
	ON_CID_CHILD(kctidFocusLangId, &WpWrSysDlg::CmdFocusLangId, NULL)

	ON_CID_CHILD(kctidFeaturesPopup, &WpWrSysDlg::CmdFeaturesPopup, NULL)
END_CMD_MAP_NIL()

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
WpWrSysDlg::WpWrSysDlg()
{
	m_rid = kridOldWritingSystemsDlg;
	m_rgencdat = NULL;
	m_fRendChngd = false;
	m_pszHelpUrl = _T("User_Interface\\Menus\\Tools\\Writing_System_Properties.htm");
	m_hmenuFeatures = NULL;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
WpWrSysDlg::~WpWrSysDlg()
{
	if (m_rgencdat)
		delete[] m_rgencdat;
	if (m_hmenuFeatures)
	{
		::DestroyMenu(m_hmenuFeatures);
		m_hmenuFeatures = NULL;
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize dialog, and its controls.
----------------------------------------------------------------------------------------------*/
bool WpWrSysDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	GetAvailableFonts();

	StrApp strRes;

	// Subclass the Help button.
	AfButtonPtr qbtnHelp;
	qbtnHelp.Create();
	qbtnHelp->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	InitEncList();
	HWND hwndEncs = ::GetDlgItem(m_hwnd, kctidWritingSystems);
	// Fill the list up with writing system strings.
	StrAppBuf strb;
	for (int iws = 0; iws < m_cws; ++iws)
	{
		strb.Assign(m_rgencdat[iws].m_strID.Chars());
		::SendMessage(hwndEncs, LB_ADDSTRING, 0, (LPARAM)strb.Chars());
	}

	//	Name
	HWND hwndName = ::GetDlgItem(m_hwnd, kctidWsName);
	::SendMessage(hwndName, EM_LIMITTEXT, (WPARAM)128, 0);
	achar rgchName[128];
	memset(rgchName, 0, 128 * isizeof(achar));
	::SetDlgItemText(m_hwnd, kctidWsName, rgchName);

	//	Code
	HWND hwndCode = ::GetDlgItem(m_hwnd, kctidWsCode);
	::SendMessage(hwndCode, EM_LIMITTEXT, (WPARAM)128, 0);
	achar rgchCode[128];
	memset(rgchCode, 0, 128 * isizeof(achar));
	::SetDlgItemText(m_hwnd, kctidWsCode, rgchCode);

	//	Description
	HWND hwndDescr = ::GetDlgItem(m_hwnd, kctidWsDescrip);
	::SendMessage(hwndDescr, EM_LIMITTEXT, (WPARAM)128, 0);
	achar rgchDescr[128];
	memset(rgchDescr, 0, 128 * isizeof(achar));
	::SetDlgItemText(m_hwnd, kctidWsDescrip, rgchDescr);

	//	Keyboard types
	HWND hwndKType = ::GetDlgItem(m_hwnd, kctidKeyboardType);
	for (int ikt = 0; ikt <kktLim; ikt++)
	{
		if (ikt == kktStandard)
			strRes.Load(kstidStandard);
		else if (ikt == kktKeyMan)
			strRes.Load(kstidKeyman);
		else
			strRes.Load(kstidOther);
		::SendMessage(hwndKType, CB_ADDSTRING, 0, (LPARAM)strRes.Chars());
	}

	//	Language ID
	HWND hwndLangId = ::GetDlgItem(m_hwnd, kctidLangId);
	for (int ilangid = 0; ilangid < g_clangid; ilangid++)
	{
		::SendMessage(hwndLangId, CB_ADDSTRING, 0, (LPARAM)g_rglangid[ilangid].m_pszName);
	}

	//	KeyMan control
	HWND hwndKmCtrl = ::GetDlgItem(m_hwnd, kctidKeymanKeyboard);
	ILgKeymanHandlerPtr qkh;
	qkh.CreateInstance(CLSID_LgKeymanHandler);
	int cKeyboards;
	CheckHr(qkh->get_NLayout(&cKeyboards));
	::SendMessage(hwndKmCtrl, CB_ADDSTRING, 0, (LPARAM)L"None");
	for (int ikb = 0; ikb <cKeyboards; ikb++)
	{
		SmartBstr sbstrKbdName;
		CheckHr(qkh->get_Name(ikb, &sbstrKbdName));
		::SendMessageW(hwndKmCtrl, CB_ADDSTRING, 0, (LPARAM)sbstrKbdName.Chars());
	}

	//	Font list
	HWND hwndFonts = ::GetDlgItem(m_hwnd, kctidFont);
	::SendMessage(hwndFonts, CB_RESETCONTENT, 0, 0);
	for (int iFont = 0; iFont < m_vstrAllFonts.Size(); iFont++)
	{
		StrApp str = m_vstrAllFonts[iFont];
		::SendMessage(hwndFonts, CB_ADDSTRING, 0, (LPARAM)str.Chars());
	}

	//	Right to left
	HWND hwndRtl = ::GetDlgItem(m_hwnd, kctidRightToLeft);
	::SendMessage(hwndRtl, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);

	//	Special info
#if 0
	HWND hwndSpecInfo = ::GetDlgItem(m_hwnd, kctidSpecInfo);
	::SendMessage(hwndSpecInfo, EM_LIMITTEXT, (WPARAM)128, 0);
	achar rgchSpecInfo[128];
	memset(rgchSpecInfo, 0, 128 * isizeof(achar));
	::SetDlgItemText(m_hwnd, kctidSpecInfo, rgchSpecInfo);
#endif

	// Font Features button: subclass so it looks like a popup button.
	AfButtonPtr qbtFeat;
	qbtFeat.Create();
	qbtFeat->SubclassButton(m_hwnd, kctidFeatures, kbtPopMenu, NULL, 0);

	//	Select an encoding--the one from the selection, or the first in the list.
	if (m_cws > 0)
	{
		int iwsToSel = 0; // default to first in list
		if (m_wsInit != 0)
		{
			for (int iws = 0; iws < m_cws; iws++)
			{
				if (m_rgencdat[iws].m_ws == m_wsInit)
				{
					iwsToSel = iws;
					break;
				}
			}
		}

		::SendMessage(hwndEncs, LB_SETCURSEL, (WPARAM)iwsToSel, 0);
		// I would expect OnNotifyChild to be called with the appropriate message to
		// handle this, but it isn't:
		InitCurrentRenderer(iwsToSel);
		UpdateControlsForSel(iwsToSel);
	}

	m_hwndParent = ::GetParent(m_hwnd);		// Get the parent (or owner).

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle the OK button.
----------------------------------------------------------------------------------------------*/
bool WpWrSysDlg::OnApply(bool fClose)
{
	int iTmp;

	if (!KeyboardComboOk(0, &iTmp))
	{
		Assert(false); // because we currently don't let them type
		return true;  // don't close
	}

	if (!FontsComboOk(0, &iTmp))
	{
		Assert(false);
		return true;
	}

	if (!LangIdComboOk(0, &iTmp))
	{
		Assert(false);
		return true;
	}

	return SuperClass::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Handle the Cancel button.
----------------------------------------------------------------------------------------------*/
bool WpWrSysDlg::OnCancel()
{
	return SuperClass::OnCancel();
}

/*----------------------------------------------------------------------------------------------
	Handle the behavior of various controls.
----------------------------------------------------------------------------------------------*/
bool WpWrSysDlg::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	HWND hwndEncs = ::GetDlgItem(m_hwnd, kctidWritingSystems);
//	HWND hwndAdd = ::GetDlgItem(m_hwnd, kctidAddWs);
//	HWND hwndDel = ::GetDlgItem(m_hwnd, kctidDeleteWs);
//	HWND hwndName = ::GetDlgItem(m_hwnd, kctidWsName);
//	HWND hwndDescr = ::GetDlgItem(m_hwnd, kctidWsDescrip);
	HWND hwndKType = ::GetDlgItem(m_hwnd, kctidKeyboardType);
	HWND hwndLangId = ::GetDlgItem(m_hwnd, kctidLangId);
	HWND hwndKmCtrl = ::GetDlgItem(m_hwnd, kctidKeymanKeyboard);
	HWND hwndFonts = ::GetDlgItem(m_hwnd, kctidFont);
	HWND hwndRtl = ::GetDlgItem(m_hwnd, kctidRightToLeft);
//	HWND hwndSpecInfo = ::GetDlgItem(m_hwnd, kctidSpecInfo);
//	HWND hwndOk = ::GetDlgItem(m_hwnd, kctidOk);
//	HWND hwndCancel = ::GetDlgItem(m_hwnd, kctidCancel);
	int iwsSel = ::SendMessage(hwndEncs, LB_GETCURSEL, 0, 0);

	WsData * pwsdat = m_rgencdat + iwsSel;

	StrApp strWp(kstidAppName);

	int nChecked;
	int rt;	// renderer type
	int kt, ktPrev;	// keyboard type
	int iLangId;	// lang id index
	int iFont;		// font index

	achar rgch[128];
	memset(rgch, 0, 128 * isizeof(achar));

	bool fChanged = false;

	switch (id)
	{
	case kctidWritingSystems:
		switch (pnmh->code)
		{
		case LBN_SELCHANGE:
			// Initialize the copy of the selected renderer.
			InitCurrentRenderer(iwsSel);
			MakeCurrFeatSettingsDefault(iwsSel, true);
			// Update the dialog controls to reflect the newly selected writing system.
			UpdateControlsForSel(iwsSel);
			break;
		default:
			break;
		}
		break;

	case kctidWsName:
		if (iwsSel >= 0 && pnmh->code == EN_CHANGE)
		{
			::GetDlgItemText(m_hwnd, kctidWsName, rgch, 128);
			pwsdat->m_stuName = rgch;		// FIX ME FOR PROPER CODE CONVERSION!
		}
		break;

	case kctidWsDescrip:
		if (iwsSel >= 0 && pnmh->code == EN_CHANGE)
		{
			::GetDlgItemText(m_hwnd, kctidWsDescrip, rgch, 128);
			pwsdat->m_stuDescr = rgch;		// FIX ME FOR PROPER CODE CONVERSION!
		}
		break;

	case kctidKeyboardType:
		fChanged = false;
		if (iwsSel >= 0 && pnmh->code == CBN_SELCHANGE)
		{
			ktPrev = m_rgencdat[iwsSel].m_kt;
			kt = ::SendMessage(hwndKType, CB_GETCURSEL, 0, 0);
			m_rgencdat[iwsSel].m_kt = kt;
			fChanged = (ktPrev != kt);
		}
#if 0	// Don't need to check here because should always be legit (they can't type)
		// and case above should handle changes to the value.
		else if (pnmh->code == CBN_KILLFOCUS)
		{
			HWND hwndFocus = ::GetFocus();
			bool fOkay = KeyboardComboOk(hwndFocus, &kt);
			if (fOkay)
			{
				ktPrev = m_rgencdat[iwsSel].m_kt;
				m_rgencdat[iwsSel].m_kt = kt;
				fChanged = (ktPrev != kt);
			}
			else
				Assert(false); // because we don't let them type
		}
#endif
		if (fChanged)
		{
			UpdateKeyboardControls(iwsSel, kt);
			//	Review: should we clear the KeyMan control string and/or the lang id?
		}
		break;

	case kctidLangId:
		if (iwsSel >= 0 && pnmh->code == CBN_SELCHANGE)
		{
			iLangId = ::SendMessage(hwndLangId, CB_GETCURSEL, 0, 0);
			m_rgencdat[iwsSel].m_iLangId = iLangId;
		}
#if 0	// Don't need to check here because should always be legit (they can't type)
		// and case above should handle changes to the value.
		else if (pnmh->code == CBN_KILLFOCUS)
		{
			HWND hwndFocus = ::GetFocus();
			bool fOkay = LangIdComboOk(hwndFocus, &iLangId);
			if (fOkay)
			{
				pwsdat->m_iLangId = iLangId;
			}
			else
			{
				if (hwndFocus == hwndKType) // currently doesn't work
				{
					// Switching back to the keyboard type.
					pwsdat->m_iLangId = -1;
				}
			}
		}
#endif
		break;

	case kctidKeymanKeyboard:
		if (iwsSel >= 0 && pnmh->code == CBN_SELCHANGE)
		{
			int ikb = ::SendMessage(hwndKmCtrl, CB_GETCURSEL, 0, 0);
			if (ikb == 0)
			{
				pwsdat->m_stuKeymanKeyboard.Clear();
			}
			else
			{
				int cch = ::SendMessage(hwndKmCtrl, CB_GETLBTEXTLEN, ikb, 0);
				OLECHAR * pch;
				pwsdat->m_stuKeymanKeyboard.SetSize(cch, &pch);
				::SendMessage(hwndKmCtrl, CB_GETLBTEXT, ikb, (LPARAM)pch);
			}
		}
		break;

	case kctidFont:
		if (iwsSel >= 0 && pnmh->code == CBN_SELCHANGE)
		{
			iFont = ::SendMessage(hwndFonts, CB_GETCURSEL, 0, 0);
			if (pwsdat->m_iFont != iFont)
			{
				pwsdat->m_iFont = iFont;
				m_fRendChngd = true;
			}

			InitCurrentRenderer(iwsSel);
			int grfsdc;
			UpdateRenderingControls(iwsSel, &grfsdc);

			if (grfsdc & kfsdcHorizLtr && !(grfsdc & kfsdcHorizRtl))
				pwsdat->m_fRtl = false;
			else if (grfsdc & kfsdcHorizRtl && !(grfsdc & kfsdcHorizLtr))
				pwsdat->m_fRtl = true;

			// Copy the default features for this font into the old writing system (if Graphite).
			// Disabled; see MakeCurrFeatSettingsDefault.
			//if (pwsdat->m_rt == krtGraphite)
			//{
			//	for (int ifeat = 0; ifeat < kMaxFeature; ifeat++)
			//		pwsdat->m_fl.rgn[ifeat] = m_vflGrDefFeats[pwsdat->m_iFont].rgn[ifeat];
			//}
		}
#if 0	// Don't need to check here because should always be legit (they can't type)
		// and case above should handle changes to the value.
		else if (pnmh->code == CBN_KILLFOCUS)
		{
			HWND hwndFocus = ::GetFocus();
			bool fOkay = FontsComboOk(hwndFocus, &iFont);
			if (fOkay)
			{
				if (pwsdat->m_iFont != iFont)
					m_fRendChngd = true;
				pwsdat->m_iFont = iFont;
			}
		}
#endif
		break;

	case kctidRightToLeft:
		if (iwsSel >= 0 && pnmh->code == BN_CLICKED)
		{
			nChecked = ::SendMessage(hwndRtl, BM_GETCHECK, 0, 0);
			if (nChecked == BST_CHECKED)
			{
				if (!pwsdat->m_fRtl)
					m_fRendChngd = true;
				pwsdat->m_fRtl = true;
			}
			else if (nChecked == BST_UNCHECKED)
			{
				if (pwsdat->m_fRtl)
					m_fRendChngd = true;
				pwsdat->m_fRtl = false;
			}
		}
		break;

	case kctidFeatures:
		if (pnmh->code == BN_CLICKED)
		{
			// Show the popup menu that allows a user to choose the features.
			Rect rc;
			::GetWindowRect(::GetDlgItem(m_hwnd, kctidFeatures), &rc);
			AfApp::GetMenuMgr()->SetMenuHandler(kctidFeaturesPopup);
			CreateFeaturesMenu(pwsdat); //  pwsdat->m_iFont);
			if (m_hmenuFeatures)
			::TrackPopupMenu(m_hmenuFeatures, TPM_LEFTALIGN | TPM_RIGHTBUTTON,
				rc.left, rc.bottom, 0, m_hwnd, NULL);
			return true;
		}

	case kctidAddWs:
		AddNewEncoding();
		break;

	case kctidDeleteWs:
		if (pwsdat->m_ws == 0)
		{
			StrApp strRes(kstidCantDeleteDefWs);
			::MessageBox(m_hwnd, strRes.Chars(), strWp.Chars(), MB_OK | MB_ICONEXCLAMATION);
		}
		else
			DeleteEncoding(iwsSel);
		break;

	case kctidCancel:
		rt = -1;
		break;

	case kctidOk:
		break;

	default:
		break;
	}

	return SuperClass::OnNotifyChild(id, pnmh, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Check the value of the fonts combo-box. Give an message and return false if it is not
	okay.
	TODO SharonC: remove this method if we decide to never let them type in the combo.
----------------------------------------------------------------------------------------------*/
bool WpWrSysDlg::FontsComboOk(HWND hwndFocus, int * piFont)
{
	HWND hwndFonts = ::GetDlgItem(m_hwnd, kctidFont);
	HWND hwndCancel = ::GetDlgItem(m_hwnd, kctidCancel);

	achar rgch[128];
	memset(rgch, 0, 128 * isizeof(achar));

	StrApp strWp(kstidAppName);

	::GetDlgItemText(m_hwnd, kctidFont, rgch, 128);
	if (_tcslen(rgch) == 0)
	{
		*piFont = -1;
	}
	else
	{
		*piFont = ::SendMessage(hwndFonts, CB_FINDSTRINGEXACT, (WPARAM)-1, (LPARAM)rgch);
		if (*piFont == CB_ERR)
		{
			if (hwndFocus == hwndCancel)
			{
				// Don't give a message.
				*piFont = -1;
			}
			else
			{
				StrApp strErr;
				StrApp strRes;
				if (_tcslen(rgch) == 0)
				{
					strErr.Load(kstidPlsEnterFont);
				}
				else
				{
					strRes.Load(kstidInvalidFont);
					strErr.Format(strRes, rgch);

				}
				::MessageBox(m_hwnd, strErr.Chars(), strWp.Chars(), MB_OK | MB_ICONEXCLAMATION);
				//::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(kctidFocusFonts, 0), NULL);
				::SetFocus(hwndFonts);
			}
			return false;
		}
	}

	return true; // okay
}

/*----------------------------------------------------------------------------------------------
	Check the value of the keyboard-type combo-box. Give an message and return false
	if it is not okay.
	TODO SharonC: remove this method if we decide to never let them type in the combo.
----------------------------------------------------------------------------------------------*/
bool WpWrSysDlg::KeyboardComboOk(HWND hwndFocus, int * pkt)
{
	HWND hwndKType = ::GetDlgItem(m_hwnd, kctidKeyboardType);
	HWND hwndCancel = ::GetDlgItem(m_hwnd, kctidCancel);

	achar rgch[128];
	memset(rgch, 0, 128 * isizeof(achar));

	StrApp strWp(kstidAppName);

	::GetDlgItemText(m_hwnd, kctidKeyboardType, rgch, 128);
	*pkt = ::SendMessage(hwndKType, CB_FINDSTRINGEXACT, (WPARAM)-1, (LPARAM)rgch);
	if (*pkt == CB_ERR)
	{
		if (hwndFocus == hwndCancel)
		{
			// Don't give a message, since their change will be ignored anyway.
		}
		else
		{
			StrApp strErr;
			StrApp strRes(kstidInvalidKeybType);
			strErr.Format(strRes, rgch);
			::MessageBox(m_hwnd, strErr.Chars(), strWp.Chars(), MB_OK | MB_ICONEXCLAMATION);
			::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(kctidFocusKeyboard, 0), NULL);
			//::SetFocus(hwndKType);
		}
		return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Check the value of the lang-id combo-box. Give an message and return false if it is not
	okay.
	TODO SharonC: remove this method if we decide to never let them type in the combo.
----------------------------------------------------------------------------------------------*/
bool WpWrSysDlg::LangIdComboOk(HWND hwndFocus, int * piLangId)
{
	HWND hwndKType = ::GetDlgItem(m_hwnd, kctidKeyboardType);
	HWND hwndLangId = ::GetDlgItem(m_hwnd, kctidLangId);
	HWND hwndCancel = ::GetDlgItem(m_hwnd, kctidCancel);

	achar rgch[128];
	memset(rgch, 0, 128 * isizeof(achar));

	StrApp strWp(kstidAppName);

	::GetDlgItemText(m_hwnd, kctidLangId, rgch, 128);
	if (_tcslen(rgch) == 0)
	{
		*piLangId = -1;
	}
	else
	{
		*piLangId = ::SendMessage(hwndLangId, CB_FINDSTRINGEXACT, (WPARAM)-1, (LPARAM)rgch);
		if (*piLangId == CB_ERR)
		{
			if (hwndFocus == hwndCancel || hwndFocus == hwndKType)
			{
				// Don't give a message.
				*piLangId = -1;
			}
			else
			{
				StrApp strErr;
				StrApp strRes(kstidInvalidLangId);
				strErr.Format(strRes, rgch);
				::MessageBox(m_hwnd, strErr.Chars(), strWp.Chars(), MB_OK | MB_ICONEXCLAMATION);
				//::PostMessage(m_hwnd, WM_COMMAND, MAKEWPARAM(kctidFocusLangId, 0), NULL);
				::SetFocus(hwndLangId);
			}
			return false;
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Initialize the list of writing systems from the writing system factory.
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::InitEncList()
{
	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
	int cwsTotal;
	CheckHr(qwsf->get_NumberOfWs(&cwsTotal));
	int * prgenc = NewObj int[cwsTotal];
	CheckHr(qwsf->GetWritingSystems(prgenc, cwsTotal));

	// Leave out the default writing system.
	m_cws = cwsTotal;
	for (int iws = 0; iws < m_cws; iws++)
	{
		if (prgenc[iws] == 0)
		{
			m_cws--;
			break;
		}
	}

	m_rgencdat = NewObj WsData[m_cws];

	// TODO SharonC (SteveMc): Name and Description are now multilingual -- do you need to handle
	// saving/restoring multiple encodings?
	// Also, Description is a TsString, not a Unicode string -- do you need to handle formatting
	// information?
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));

	int iwsdat, iws;
	for (iws = 0, iwsdat = 0; iws < cwsTotal; iws++, iwsdat++)
	{
		if (prgenc[iws] == 0)
		{
			iwsdat--;
			continue;
		}

		Assert(iwsdat < m_cws);
		WsData * pwsdat = &(m_rgencdat[iwsdat]);
		pwsdat->m_ws = prgenc[iws];

		IWritingSystemPtr qws;
		CheckHr(qwsf->get_EngineOrNull(prgenc[iws], &qws));
		AssertPtr(qws);

		//	Generate the writing system ID string.
		if (prgenc[iws] == 0)
		{
			Assert(prgenc[iws]);
			pwsdat->m_staWs = "<unknown>";
		}
		else
		{
			SmartBstr sbstr;
			HRESULT hr;
			CheckHr(hr = qwsf->GetStrFromWs(prgenc[iws], &sbstr));
			if (hr == S_FALSE || !sbstr.Length())
			{
				Assert(hr == S_OK && sbstr.Length());
				pwsdat->m_staWs.Assign("<unknown>");
			}
			else
			{
				pwsdat->m_staWs.Assign(sbstr.Chars(), sbstr.Length());
			}
		}

		ComBool fRtl;
		CheckHr(qws->get_RightToLeft(&fRtl));
		pwsdat->m_fRtl = (bool)fRtl;

		SmartBstr sbstrName = NULL;
		CheckHr(qws->get_Name(wsUser, &sbstrName));
		if (sbstrName)
		{
			StrUni stu(sbstrName.Bstr());
			pwsdat->m_stuName = stu;
			pwsdat->m_strID = stu;
		}
		else
		{	// no name
			StrApp strWs = pwsdat->m_staWs;
			pwsdat->m_strID.Format(_T("-%s-"), strWs.Chars());
		}

		ITsStringPtr qtssDescr;
		CheckHr(qws->get_Description(wsUser, &qtssDescr));
		if (qtssDescr)
		{
			SmartBstr sbstrDescr = NULL;
			CheckHr(qtssDescr->get_Text(&sbstrDescr));
			if (sbstrDescr)
			{
				StrUni stu(sbstrDescr.Bstr());
				pwsdat->m_stuDescr = stu;
			}
		}

		ComBool fKeyMan;
		CheckHr(qws->get_KeyMan(&fKeyMan));
		pwsdat->m_kt = (fKeyMan) ? kktKeyMan : kktStandard;

		//if (fKeyMan)
		//{
			SmartBstr sbstrKeymanKeyboard;
			CheckHr(qws->get_KeymanKbdName(&sbstrKeymanKeyboard));
			pwsdat->m_stuKeymanKeyboard = sbstrKeymanKeyboard.Chars();
		//}

		int lcid;
		CheckHr(qws->get_Locale(&lcid));
		int langid = LANGIDFROMLCID(lcid);
		int ilangid;
		for (ilangid = 0; ilangid < g_clangid; ilangid++)
		{
			if (g_rglangid[ilangid].m_n == langid)
				break;
		}
		if (ilangid >= g_clangid)
			pwsdat->m_iLangId = -1;
		else
			pwsdat->m_iLangId = ilangid;

		pwsdat->m_iFont = -1;
		SmartBstr sbstrDefSerif = NULL;
		CheckHr(qws->get_DefaultSerif(&sbstrDefSerif));
		if (sbstrDefSerif)
		{
			pwsdat->m_iFont = AllFontsIndex(sbstrDefSerif);
		}
		SmartBstr sbstrDefSans = NULL;
		qws->get_DefaultSansSerif(&sbstrDefSans);
		if (pwsdat->m_iFont == -1 && sbstrDefSans)
		{
			pwsdat->m_iFont = AllFontsIndex(sbstrDefSans);
		}
		SmartBstr sbstrDefBodyFont = NULL;
		qws->get_DefaultBodyFont(&sbstrDefBodyFont);
		if (pwsdat->m_iFont == -1 && sbstrDefBodyFont)
		{
			pwsdat->m_iFont = AllFontsIndex(sbstrDefBodyFont);
		}
		SmartBstr sbstrDefMono = NULL;
		qws->get_DefaultMonospace(&sbstrDefMono);
		if (pwsdat->m_iFont == -1 && sbstrDefMono)
		{
			pwsdat->m_iFont = AllFontsIndex(sbstrDefMono);
		}

		// AFTER we initialize the font info in pwsdat, we can figure the default
		// renderer type and, if it is Graphite, interpret its features.
		IRenderEnginePtr qreneng;
		InitRenderer(pwsdat, qreneng);

		SmartBstr sbstrFontVar = NULL;
		CheckHr(qws->get_FontVariation(&sbstrFontVar));
		if (sbstrFontVar)
		{
			StrUni stu(sbstrFontVar.Bstr());
			pwsdat->m_stuSpecInfo = stu;

			if (pwsdat->m_rt == krtGraphite)
				ParseFeatureString(stu, qreneng, pwsdat);
		}
	}

	delete[] prgenc;

	//	Sort the encodings by id string.
	for (int iws = 0; iws < m_cws - 1; iws++)
	{
		for (int iws2 = iws + 1; iws2 < m_cws; iws2++)
		{
			if (m_rgencdat[iws2].m_strID < m_rgencdat[iws].m_strID)
			{
				char rgb[isizeof(WsData)];
				memcpy(&rgb, &(m_rgencdat[iws2]), isizeof(WsData));
				memcpy(&(m_rgencdat[iws2]), &(m_rgencdat[iws]), isizeof(WsData));
				memcpy(&(m_rgencdat[iws]), &rgb, isizeof(WsData));
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Get the list of all available fonts, and the list of Graphite fonts from the registry.
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::GetAvailableFonts()
{
	// Get the currently available fonts via the LgFontManager.

	ILgFontManagerPtr qfm;
	SmartBstr bstrNames;

	qfm.CreateInstance(CLSID_LgFontManager);
	CheckHr(qfm->AvailableFonts(&bstrNames));
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
//	strName.Assign("<default serif>");
//	ipszList = ::SendMessage((HWND)hwndT, CB_ADDSTRING, 0, (LPARAM)strName.Chars());
//	strName.Assign("<default sans serif>");
//	ipszList = ::SendMessage((HWND)hwndT, CB_ADDSTRING, 0, (LPARAM)strName.Chars());
//	strName.Assign("<default fixed>");
//	ipszList = ::SendMessage((HWND)hwndT, CB_ADDSTRING, 0, (LPARAM)strName.Chars());

	// Add each font name to the combo box.
	while (ichLim < cchLength)
	{
		ichLim = strNameList.FindCh(L',', ichMin);
		if (ichLim == -1) // i.e., if not found.
		{
			ichLim = cchLength;
		}

		strName.Assign(strNameList.Chars() + ichMin, ichLim - ichMin);
		m_vstrAllFonts.Push(strName);

		ichMin = ichLim + 1;
	}
}

/*----------------------------------------------------------------------------------------------
	Find the index of the given font in the all-fonts list, or -1 if not found.
	Review: do we need a binary search?
---------------------------------------------------------------------------------------------*/
int WpWrSysDlg::AllFontsIndex(SmartBstr sbstr)
{
	StrApp str = sbstr.Bstr();
	for (int i = 0; i < m_vstrAllFonts.Size(); i++)
	{
		if (str == m_vstrAllFonts[i])
			return i;
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Reinitialize the temporary renderer to reflect the selections in the controls
	(font, consequent rendering type, right-to-left, font features).
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::InitCurrentRenderer(int iwsSel)
{
	if (iwsSel < 0 || iwsSel >= m_cws)
	{
		Assert(!m_qrenengSel);
		return;
	}
	WsData * pwsdat = m_rgencdat + iwsSel;
	InitRenderer(pwsdat, m_qrenengSel);
}

/*----------------------------------------------------------------------------------------------
	Reinitialize the temporary renderer to reflect the selections in the controls
	(font, consequent rendering type, right-to-left, font features).
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::InitRenderer(WsData * pwsdat, IRenderEnginePtr & qreneng)
{
	if (pwsdat->m_iFont == -1)
	{
		qreneng.Clear();
		return;
	}

	SmartBstr sbstrFontVar = pwsdat->m_stuSpecInfo.Bstr();

	StrUni stuDefFont;
	stuDefFont = m_vstrAllFonts[pwsdat->m_iFont];
	SmartBstr sbstrFont = stuDefFont.Bstr();

	bool fGraphite = gr::GrUtil::FontHasGraphiteTables(sbstrFont, false, false);

	if (qreneng)
	{
		// A rendering engine exists; make sure it is the right type.
		GUID guidEngine;
		HRESULT hr = qreneng->get_ClassId(&guidEngine);
		if (SUCCEEDED(hr))
		{
			if (guidEngine != CLSID_FwGrEngine && fGraphite)
				//	Change to a Graphite renderer
				qreneng.CreateInstance(CLSID_FwGrEngine);
			else if (guidEngine != CLSID_UniscribeEngine && !fGraphite)
				//	Change to a Uniscribe renderer
				qreneng.CreateInstance(CLSID_UniscribeEngine);
		}
	}
	else
	{ // don't have one to begin with
		if (fGraphite)
			//	Create a Graphite renderer
			qreneng.CreateInstance(CLSID_FwGrEngine);
		else
			//	Create a Uniscribe renderer
			qreneng.CreateInstance(CLSID_UniscribeEngine);
	}

	if (fGraphite)
	{
		int nTrace = (m_fGrLog) ? 1 : 0;	// Least significant option bit is for logging.
		//	Reinitialize the renderer. Note: last param sets trace options (e.g. logging).
		gr::GrUtil::InitGraphiteRenderer(qreneng, sbstrFont, false, false, NULL, nTrace);
	}
	pwsdat->m_rt = fGraphite ? krtGraphite : krtUniscribe;
}

/*----------------------------------------------------------------------------------------------
	Copy the current feature settings into the default list.
	Only do this if we are dealing with a Graphite font.
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::MakeCurrFeatSettingsDefault(int iwsSel, bool fInit)
{
	// This feature is disabled because now we don't maintain separate lists of Graphite fonts,
	// and the total list of fonts is too long to keep a features structure for each of them.
	// We could reimplement making the features structure smaller or using a hash table, but
	// at present the feature does not seem valuable enough to warrant it.

	//Assert(m_qrenengSel);
	//if (fInit)
	//{
	//	for (int i = 0; i < this->m_vflGrDefFeats.Size(); i++)
	//		m_vflGrDefFeats[i].Init();
	//}

	//WsData * pwsdat = m_rgencdat + iwsSel;
	//if (pwsdat->m_rt == krtGraphite && pwsdat->m_iFont > -1)
	//{
	//	int iGrFont = pwsdat->m_iFont;
	//	for (int ifeat = 0; ifeat < kMaxFeature; ifeat++)
	//		m_vflGrDefFeats[iGrFont].rgn[ifeat] = pwsdat->m_fl.rgn[ifeat];
	//}
}

/*----------------------------------------------------------------------------------------------
	Create a menu for selecting features. The features are in the main menu, with sub-menus
	containing each of the values.
	Assumes that the current renderer has been set up to match the given Graphite font.
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::CreateFeaturesMenu(WsData * pwsdat)
{
	IRenderingFeaturesPtr qrenfeat = NULL;
	HRESULT hr = E_FAIL;
	if (m_qrenengSel)
	{
		// Don't CheckHr; may very well fail.
		IgnoreHr(hr = m_qrenengSel->QueryInterface(IID_IRenderingFeatures, (void **)&qrenfeat));
	}
	if (!qrenfeat || FAILED(hr))
	{
		// Can't get any features, therefore no menu.
		if (m_hmenuFeatures)
		{
			::DestroyMenu(m_hmenuFeatures);
			m_hmenuFeatures = NULL;
		}
		return;
	}

	// For now, always create a new menu, to make sure the check marks are right.
	// ENHANCE: keep the same menu, just update the check marks.
	if (true)
	{
		::DestroyMenu(m_hmenuFeatures);
		m_hmenuFeatures = NULL;
	}

	if (!m_hmenuFeatures)
	{
		m_vnFeatMenuIDs.Clear();
		m_hmenuFeatures = ::CreatePopupMenu();
		if (!m_hmenuFeatures)
			ThrowHr(WarnHr(E_FAIL));

		int nLang = 0x00000409;	// for now the UI language is US English

		if (!FmtFntDlg::SetupFeaturesMenu(m_hmenuFeatures, qrenfeat, nLang,
			m_vnFeatMenuIDs, pwsdat->m_fl.rgn))
		{
			// No valid items for any of the features. Delete the menu.
			::DestroyMenu(m_hmenuFeatures);
			m_hmenuFeatures = NULL;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Handle a popup menu command for choosing the desired font features (this is only used
	for Graphite fonts currently).
----------------------------------------------------------------------------------------------*/
bool WpWrSysDlg::CmdFeaturesPopup(Cmd * pcmd)
{
	AssertPtr(pcmd);
	Assert(pcmd->m_rgn[0] == AfMenuMgr::kmaDoCommand);

	// The user selected an expanded menu item, so perform the command now.
	//    m_rgn[1] holds the menu handle.
	//    m_rgn[2] holds the index of the selected item.

	int i = pcmd->m_rgn[2];

	int iwsSel = ::SendMessage(::GetDlgItem(m_hwnd, kctidWritingSystems), LB_GETCURSEL, 0, 0);
	WsData * pwsdat = m_rgencdat + iwsSel;

	IRenderingFeaturesPtr qrenfeat;
	CheckHr(m_qrenengSel->QueryInterface(IID_IRenderingFeatures, (void **)&qrenfeat));

	FmtFntDlg::HandleFeaturesMenu(qrenfeat, i, m_vnFeatMenuIDs, pwsdat->m_fl.rgn);

	m_fRendChngd = true;

	MakeCurrFeatSettingsDefault(iwsSel, false);

	return true;
}


/*----------------------------------------------------------------------------------------------
	Generate the font variation string from the list of selected features.
	Only do this if we are dealing with a Graphite font. This string of the form
	<id1>=<val1>,<id2>=<val2>,...
----------------------------------------------------------------------------------------------*/
StrUni WpWrSysDlg::GenerateFeatureString(IRenderEngine * preneng, WsData * pwsdat)
{
	Assert(pwsdat->m_rt == krtGraphite);

	IRenderingFeaturesPtr qrenfeat;
	CheckHr(preneng->QueryInterface(IID_IRenderingFeatures, (void **)&qrenfeat));
	return FmtFntDlg::GenerateFeatureString(qrenfeat, pwsdat->m_fl.rgn);
}

/*----------------------------------------------------------------------------------------------
	Parse the string that describes the font's features, and store the information in the
	given data structure. This string of the form <id1>=<val1>,<id2>=<val2>,...
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::ParseFeatureString(StrUni stu, IRenderEngine * preneng, WsData * pwsdat)
{
	Assert(pwsdat->m_rt == krtGraphite);

	IRenderingFeaturesPtr qrenfeat;
	CheckHr(preneng->QueryInterface(IID_IRenderingFeatures, (void **)&qrenfeat));
	pwsdat->m_fl.Init();
	if (!qrenfeat)
		return;
	FmtFntDlg::ParseFeatureString(qrenfeat, stu, pwsdat->m_fl.rgn);
}

/*----------------------------------------------------------------------------------------------
	Update the dialog controls to reflect the new selection in the writing systems list.
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::UpdateControlsForSel(int iwsSel)
{
	WsData * pwsdat;
	WsData encdatEmpty;
	if (iwsSel < 0 || iwsSel >= m_cws)
	{
		pwsdat = &encdatEmpty;
		pwsdat->m_kt = -1;
		pwsdat->m_rt = -1;
	}
	else
		pwsdat = m_rgencdat + iwsSel;

	StrApp strName;
	strName.Assign(pwsdat->m_stuName);		// FIX ME FOR PROPER CODE CONVERSION!
	::SetDlgItemText(m_hwnd, kctidWsName, strName.Chars());

	StrApp strCode;
	strCode.Assign(pwsdat->m_staWs);
	::SetDlgItemText(m_hwnd, kctidWsCode, strCode.Chars());
	// Control is defined to be read-only by the resource file.

	StrApp strDescr;
	strDescr.Assign(pwsdat->m_stuDescr);	// FIX ME FOR PROPER CODE CONVERSION!
	::SetDlgItemText(m_hwnd, kctidWsDescrip, strDescr.Chars());

	HWND hwndKType = ::GetDlgItem(m_hwnd, kctidKeyboardType);
	::SendMessage(hwndKType, CB_SETCURSEL, (WPARAM)pwsdat->m_kt, 0);
	UpdateKeyboardControls(iwsSel, pwsdat->m_kt);

//	// Set the keyman keyboard...don't worry about StrApp, we're no longer supporting
//	// non-Unicode.
//	HWND hwndKeymanCtl = ::GetDlgItem(m_hwnd, kctidKeymanKeyboard);
//	::SendMessageW(hwndKeymanCtl, CB_SELECTSTRING, (WPARAM)-1,
//		(LPARAM)(pwsdat->m_stuKeymanKeyboard.Chars()));

	HWND hwndLangId = ::GetDlgItem(m_hwnd, kctidLangId);
	::SendMessage(hwndLangId, CB_SETCURSEL, (WPARAM)pwsdat->m_iLangId, 0);

	UpdateRenderingControls(iwsSel);

	HWND hwndFonts = ::GetDlgItem(m_hwnd, kctidFont);
	::SendMessage(hwndFonts, CB_SETCURSEL, (WPARAM)pwsdat->m_iFont, 0);

	HWND hwndRtl = ::GetDlgItem(m_hwnd, kctidRightToLeft);
	if (pwsdat->m_fRtl)
		::SendMessage(hwndRtl, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
	else
		::SendMessage(hwndRtl, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);

#if 0
	StrApp str3(pwsdat->m_stuSpecInfo);
	::SetDlgItemText(m_hwnd, kctidSpecInfo, str3.Chars());
#endif
}

/*----------------------------------------------------------------------------------------------
	Update the various controls based on a change to the renderer type.
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::UpdateRenderingControls(int iwsSel, int * pgrfsdcRet)
{
	HWND hwndRtl = ::GetDlgItem(m_hwnd, kctidRightToLeft);
	HWND hwndFeat = ::GetDlgItem(m_hwnd, kctidFeatures);

	// Enable or disable the right-to-left check box based on what is permitted in the font.
	int grfsdc;
	HRESULT hr = (m_qrenengSel) ? m_qrenengSel->get_ScriptDirection(&grfsdc) : E_FAIL;
	if (SUCCEEDED(hr))
	{
		if ((grfsdc & kfsdcHorizLtr) && (grfsdc & kfsdcHorizRtl))
		{
			// Both directions are permitted; enable the check box.
			::EnableWindow(hwndRtl, true);
		}
		else if (grfsdc & kfsdcHorizLtr)
		{
			// Only LTR: uncheck and disable.
			::SendMessage(hwndRtl, BM_SETCHECK, (WPARAM)BST_UNCHECKED, 0);
			::EnableWindow(hwndRtl, false);
		}
		else if (grfsdc & kfsdcHorizRtl)
		{
			// Only RTL: check and disable.
			::SendMessage(hwndRtl, BM_SETCHECK, (WPARAM)BST_CHECKED, 0);
			::EnableWindow(hwndRtl, false);
		}
		else
		{
			// No directions permitted? Whatever; enable.
			Assert(false);
			::EnableWindow(hwndRtl, true);
		}
		if (pgrfsdcRet)
			*pgrfsdcRet = grfsdc;
	}
	else
	{
		//	We don't know what's permitted; enable it.
		::EnableWindow(hwndRtl, true);
		if (pgrfsdcRet)
			*pgrfsdcRet = (kfsdcHorizLtr | kfsdcHorizRtl);
	}

	// Enable or disable the Font Features button depending on whether there are any features.
	bool fHasFeatures = false;
	IRenderingFeaturesPtr qrenfeat = NULL;
	hr = E_FAIL;
	if (m_qrenengSel)
	{
		// Don't CheckHr; may very well fail.
		hr = m_qrenengSel->QueryInterface(IID_IRenderingFeatures, (void **)&qrenfeat);
	}
	if (!qrenfeat || FAILED(hr))
		fHasFeatures = false;
	else
		fHasFeatures = FmtFntDlg::HasFeatures(qrenfeat);
	::EnableWindow(hwndFeat, fHasFeatures);
}

/*----------------------------------------------------------------------------------------------
	Update the various controls based on a change to the keyboard type.
	Currently we never disable the keyboard control, because even "standard" will allow us
	to use Keyman keyboards.
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::UpdateKeyboardControls(int iwsSel, int kt)
{
	HWND hwndKmCtrl = ::GetDlgItem(m_hwnd, kctidKeymanKeyboard);
	WsData * pwsdat = m_rgencdat + iwsSel;
	const wchar * pszKbdName = pwsdat->m_stuKeymanKeyboard.Chars();
	LRESULT lResult;
	//switch (kt)
	//{
	//case kktKeyMan:
	//	// Enable the KeyMan control field, and set the proper default.
	//	::EnableWindow(hwndKmCtrl, true);

		lResult = ::SendMessageW(hwndKmCtrl, CB_SELECTSTRING, (WPARAM)-1, (LPARAM)pszKbdName);
		if (lResult == CB_ERR)
			lResult = ::SendMessage(hwndKmCtrl, CB_SETCURSEL, (WPARAM)-1, (LPARAM)0);

	//	break;

	//case kktStandard:
	//	// Disable the KeyMan control field, and set the proper default (nothing).
	//	lResult = ::SendMessage(hwndKmCtrl, CB_SETCURSEL, (WPARAM)-1, (LPARAM)0);
	//	::EnableWindow(hwndKmCtrl, false);
	//	break;

	//default:
	//	// do nothing
	//	break;
	//}
}

/*----------------------------------------------------------------------------------------------
	Modify the actual writing system objects based on the changes in the dialog.
	This happens when the dialog closes with an OK.
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::ModifyEncodings(WpDa * pda)
{
	AssertPtr(pda);
	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
	int cwsObj;
	CheckHr(qwsf->get_NumberOfWs(&cwsObj));
	Vector<int> vwsObj;
	vwsObj.Resize(cwsObj);
	CheckHr(qwsf->GetWritingSystems(vwsObj.Begin(), cwsObj));

	int cwsInDlg = m_cws;
	int iwsInDlg;

	WaitCursor wc;		// This is potentially slow as molasses in wintertime.  :-)
	// Count the number of ws being deleted and added.
	int cDel = 0;
	int cAdd = 0;
	for (int iwsObj = 0; iwsObj < cwsObj; iwsObj++)
	{
		int wsDel = vwsObj[iwsObj];
		if (wsDel != 0)
		{
			for (iwsInDlg = 0; iwsInDlg < cwsInDlg; iwsInDlg++)
			{
				if (m_rgencdat[iwsInDlg].m_ws == wsDel)
					break;
			}
			if (iwsInDlg >= cwsInDlg)
			{
				++cDel;
			}
		}
	}
	for (iwsInDlg = 0; iwsInDlg < cwsInDlg; iwsInDlg++)
	{
		if (m_rgencdat[iwsInDlg].m_ws == 0)
		{
			++cAdd;
		}
	}
	AfProgressDlgPtr qprog;
	if (cDel + cAdd)
	{
		qprog.Create();
		qprog->DoModeless(m_hwndParent);
		StrApp strMsg(kstidSavingWsMsg);
		qprog->SetMessage(strMsg.Chars());
		StrApp strTitle(kstidSavingWsTitle);
		qprog->SetTitle(strTitle.Chars());
		qprog->SetRange(0, cDel + cwsObj + 2 * cAdd);
		qprog->SetStep(1);
	}

	//	First of all, if there are any encodings that have been deleted, delete them for real.
	for (int iwsObj = 0; iwsObj < cwsObj; iwsObj++)
	{
		int wsDel = vwsObj[iwsObj];
		if (wsDel == 0)
			continue; // don't delete the default writing system.
		for (iwsInDlg = 0; iwsInDlg < cwsInDlg; iwsInDlg++)
		{
			if (m_rgencdat[iwsInDlg].m_ws == wsDel)
				break;
		}
		if (iwsInDlg >= cwsInDlg)
		{
			//	Not found in dialog list--delete it.
			SmartBstr sbstrWs;
			CheckHr(qwsf->GetStrFromWs(wsDel, &sbstrWs));
			qwsf->RemoveEngine(wsDel);
			vwsObj[iwsObj] = 0;
			if (qprog)
				qprog->StepIt();
			// This is a convenient spot to delete it from the registry as well.
			// This is safe, but probably not needed anymore.
			pda->DeleteWsFromRegistry(wsDel);

			// We must also delete any style information specific to this writing system.
			Vector<AfMainWndPtr> vqafw = AfApp::Papp()->GetMainWindows();
			for (int iwnd = 0; iwnd < vqafw.Size(); iwnd++)
			{
				WpMainWnd * pwpwndTmp = dynamic_cast<WpMainWnd *>(vqafw[iwnd].Ptr());
				if (pwpwndTmp && pwpwndTmp->DataAccess() && pwpwndTmp->GetStylesheet())
				{
					WpDa * pwda = dynamic_cast<WpDa *>(pwpwndTmp->DataAccess());
					AfStylesheet * pasts = pwpwndTmp->GetStylesheet();
					AssertPtr(pwda);
					AssertPtr(pasts);
					HvoClsidVec & vhcStyles = pasts->GetStyles();
					for (int ist = 0; ist < vhcStyles.Size(); ist++)
					{
						HVO hvoStyle = vhcStyles[ist].hvo;
						IUnknownPtr qunkTtp;
						CheckHr(pwda->get_UnknownProp(hvoStyle, kflidStStyle_Rules, &qunkTtp));
						ITsTextPropsPtr qttp;
						CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));
						// Check whether this property has a WsStyle/WsProp for the writing
						// system being deleted.  If so, remove it!
						SmartBstr sbstr;
						CheckHr(qttp->GetStrPropValue(ktptWsStyle, &sbstr));
						if (!sbstr.Length())
							continue;
						Vector<WsStyleInfo> vesi;
						Vector<int> vws;
						FwStyledText::DecodeFontPropsString(sbstr, vesi, vws);
						bool fDel = false;
						for (int ie = 0; ie < vesi.Size(); ++ie)
						{
							if (vesi[ie].m_ws == wsDel)
							{
								vesi.Delete(ie);
								fDel = true;
								break;
							}
						}
						if (fDel)
						{
							int nType;
							CheckHr(pwda->get_IntProp(hvoStyle, kflidStStyle_Type, &nType));
							StrUni stuWsStyle(FwStyledText::EncodeFontPropsString(vesi,
								nType == kstParagraph));
							ITsPropsBldrPtr qtpb;
							CheckHr(qttp->GetBldr(&qtpb));
							CheckHr(qtpb->SetStrPropValue(ktptWsStyle, stuWsStyle.Bstr()));
							ITsTextPropsPtr qttpNew;
							CheckHr(qtpb->GetTextProps(&qttpNew));
							IUnknownPtr qunk;
							CheckHr(qttpNew->QueryInterface(IID_IUnknown, (void **)&qunk));
							CheckHr(pwda->SetUnknown(hvoStyle, kflidStStyle_Rules, qunk));
						}
					}
				}
			}
			if (qprog)
				qprog->StepIt();
		}
	}

	//	Then, if there are any new encodings, add them for real.
	for (iwsInDlg = 0; iwsInDlg < cwsInDlg; iwsInDlg++)
	{
		if (m_rgencdat[iwsInDlg].m_ws == 0)
		{
			// No corresponding writing system engine -- create it.
			IWritingSystemPtr qws;
			StrUni stuWs(m_rgencdat[iwsInDlg].m_staWs);
			CheckHr(qwsf->get_Engine(stuWs.Bstr(), &qws));
			AssertPtr(qws);
			CheckHr(qws->get_WritingSystem(&m_rgencdat[iwsInDlg].m_ws));
			if (qprog)
				qprog->StepIt();
		}
	}

	//	Update the list of writing system objects.

	CheckHr(qwsf->get_NumberOfWs(&cwsObj));
	vwsObj.Resize(cwsObj);
	CheckHr(qwsf->GetWritingSystems(vwsObj.Begin(), cwsObj));

	// TODO SharonC (SteveMc): Name and Description are now multilingual -- do you need to
	// handle saving/restoring the alternatives?
	// Also, Description is a TsString, not a Unicode string -- do you need to handle formatting
	// information?
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	AssertPtr(qtsf);
	ITsStringPtr qtssDescr;

	//	Now update the existing encodings with the information from the dialog.

	for (iwsInDlg = 0; iwsInDlg < cwsInDlg; iwsInDlg++)
	{
		WsData * pwsdat = &(m_rgencdat[iwsInDlg]);

		int iwsObj;
		for (iwsObj = 0; iwsObj < cwsObj; iwsObj++)
		{
			if (vwsObj[iwsObj] == pwsdat->m_ws)
				break;
		}
		Assert(iwsObj < cwsObj);

		IWritingSystemPtr qws;
		CheckHr(qwsf->get_EngineOrNull(vwsObj[iwsObj], &qws));
		AssertPtr(qws);

		//	name
		CheckHr(qws->put_Name(wsUser, pwsdat->m_stuName.Bstr()));

		//	description
		if (pwsdat->m_stuDescr.Length())
			CheckHr(qtsf->MakeString(pwsdat->m_stuDescr.Bstr(), wsUser, &qtssDescr));
		else
			qtssDescr.Clear();
		CheckHr(qws->put_Description(wsUser, qtssDescr));

		//	keyboarding
		CheckHr(qws->put_KeyMan(pwsdat->m_kt == kktKeyMan));

		if (pwsdat->m_iLangId > -1)
		{
			LCID lcid = MAKELCID(g_rglangid[pwsdat->m_iLangId].m_n, SORT_DEFAULT);
			CheckHr(qws->put_Locale(lcid));
		}

		SmartBstr sbstrKeymanKeyboard = pwsdat->m_stuKeymanKeyboard.Bstr();
		CheckHr(qws->put_KeymanKbdName(sbstrKeymanKeyboard));

		//	Rendering.
		ComBool fRtl = (ComBool)pwsdat->m_fRtl;
		CheckHr(qws->put_RightToLeft(fRtl));

		SmartBstr sbstrFontVar = pwsdat->m_stuSpecInfo.Bstr();
		CheckHr(qws->put_FontVariation(sbstrFontVar));

		// Set tracing on or off for this writing system.
		int nTrace = (m_fGrLog) ? 1 : 0;	// Least significant option bit is for logging.
		qws->SetTracing(nTrace);


		if (pwsdat->m_iFont > -1)
		{
			StrUni stuDefFont;
			stuDefFont = m_vstrAllFonts[pwsdat->m_iFont];
			SmartBstr sbstrFont = stuDefFont.Bstr();
			CheckHr(qws->put_DefaultSerif(sbstrFont));	// Sans and Monospace are not chaged.

			//	Reinitialize the renderer.

			StrUni stuInit;
			StrUni stuFontVarGen; // null unless Graphite renderer with features.
			if (pwsdat->m_rt == krtGraphite)
			{
				IRenderEnginePtr qreneng;
				InitRenderer(pwsdat, qreneng);
				stuFontVarGen = GenerateFeatureString(qreneng, pwsdat);
			}
			CheckHr(qws->put_FontVariation(stuFontVarGen.Bstr()));
		}
		if (qprog)
			qprog->StepIt();
	}
	if (qprog)
	{
		qprog->DestroyHwnd();
		qprog.Clear();
	}
}

/*----------------------------------------------------------------------------------------------
	Add a new writing system.
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::AddNewEncoding()
{
	WpNewWsDlgPtr qdlg;
	qdlg.Create();
	qdlg->SetWsData(m_rgencdat, m_cws);
	if (qdlg->DoModal(m_hwnd) == kctidOk)
	{
		//	Create the new writing system item in the list. (Only when the OK button is hit do
		//	we create the writing system object itself.)

		StrAnsi staWsNew(qdlg->NewEncStr());

		if (!staWsNew.Length())
			return;
		char rgchLang[100];
		UErrorCode err = U_ZERO_ERROR;
		uloc_getLanguage(staWsNew.Chars(), rgchLang, 100, &err);
		int cchLang = strlen(rgchLang);
		if (cchLang > 0)
		{
			int cchNew = strlen(staWsNew.Chars());
			char next = '_';
			if (cchNew > cchLang)
				next = staWsNew.Chars()[cchLang];
			if (strncmp(staWsNew.Chars(), rgchLang, cchLang)|| next != '_')
			{
				// We got a different code; this is an error. Typically it is because rgchLang is
				// a new 2-letter one and the staWsNew is the old 3-letter one.
				StrUni stuMsg;
				stuMsg.Format(L"The code '%S' cannot be used, because it starts with a code ICU will not accept.\nUsually this is because it starts with a 3-letter language code that has been replaced with a 2-letter one.\nTo see the 2-letter code, click on the 3-letter code on the Ethnologue web page: ethnologue.com.",
					staWsNew.Chars());
				StrUni stuCaption("Cannot use code");
				MessageBox(m_hwnd, stuMsg.Chars(), stuCaption.Chars(), MB_OK);
				return;
			}
		}
		WsData * pwsdatOldMin = m_rgencdat;
		m_rgencdat = NewObj WsData[m_cws + 1];
		char * bTmp = NewObj char[isizeof(WsData) * m_cws];

		//	Swap the old buffer and the new one (this way the old buffer owns the ref counts
		//	for the bogus empty strings that were created in the NewObj WsData statement above).
		memmove(bTmp, m_rgencdat, isizeof(WsData) * m_cws);
		memmove(m_rgencdat, pwsdatOldMin, isizeof(WsData) * m_cws);
		memmove(pwsdatOldMin, bTmp, isizeof(WsData) * m_cws);

		delete[] pwsdatOldMin;

		StrApp strWsNew = staWsNew;
		StrApp strIDNew;
		strIDNew.Format(_T("-%s-"), strWsNew.Chars());

		m_rgencdat[m_cws].m_ws = 0;				// Unknown, flag as uncreated.
		m_rgencdat[m_cws].m_staWs = staWsNew;
		m_rgencdat[m_cws].m_strID = strIDNew;
		m_rgencdat[m_cws].m_kt = kktStandard;
		m_rgencdat[m_cws].m_rt = krtUniscribe;
		m_rgencdat[m_cws].m_iFont = -1;
		m_rgencdat[m_cws].m_fRtl = false;

		m_cws++;

		HWND hwndEncs = ::GetDlgItem(m_hwnd, kctidWritingSystems);

		//	Move the new writing system to its proper sorted place.
		int iwsSel = -1;
		StrApp strb;
		for (int iwsdat = 0; iwsdat < m_cws - 1; iwsdat++)
		{
			if (m_rgencdat[iwsdat].m_strID > strIDNew)
			{
				//	Move the new writing system forward.
				memmove(bTmp, m_rgencdat + m_cws - 1, isizeof(WsData));
				memmove(m_rgencdat + iwsdat + 1, m_rgencdat + iwsdat,
					isizeof(WsData) * (m_cws - iwsdat - 1));
				memmove(m_rgencdat + iwsdat, bTmp, isizeof(WsData));

				//	Add the new string to the list box.
				strb.Assign(strIDNew.Chars());
				::SendMessage(hwndEncs, LB_INSERTSTRING, (WPARAM)iwsdat, (LPARAM)strb.Chars());
				iwsSel = iwsdat;

				break;
			}
		}
		if (iwsSel == -1)
		{
			strb.Assign(strIDNew.Chars());
			::SendMessage(hwndEncs, LB_ADDSTRING, (WPARAM)0, (LPARAM)strb.Chars());
			iwsSel = m_cws - 1;
		}

		::SendMessage(hwndEncs, LB_SETCURSEL, (WPARAM)iwsSel, 0);
		InitCurrentRenderer(iwsSel);
		MakeCurrFeatSettingsDefault(iwsSel, true);
		UpdateControlsForSel(iwsSel);

		delete[] bTmp;
	}
}

/*----------------------------------------------------------------------------------------------
	Delete the selected writing system.
----------------------------------------------------------------------------------------------*/
void WpWrSysDlg::DeleteEncoding(int iwsSel)
{
	int encToDelete = m_rgencdat[iwsSel].m_ws;
	bool fEncUsed = false;
	Vector<AfMainWndPtr> vqafw = AfApp::Papp()->GetMainWindows();
	for (int iwnd = 0; iwnd < vqafw.Size(); iwnd++)
	{
		WpMainWnd * pwpwndTmp = dynamic_cast<WpMainWnd *>(vqafw[iwnd].Ptr());
		if (pwpwndTmp && pwpwndTmp->DataAccess())
		{
			if (pwpwndTmp->DataAccess()->AnyStringWithWs(encToDelete))
				fEncUsed = true;
		}
	}
	if (fEncUsed)
	{
		StrApp strRes;
		if (vqafw.Size() <= 1)
			strRes.Load(kstidWsInUse);
		else
			strRes.Load(kstidWsInUseMultDoc);
		StrApp strWp(kstidAppName);
		::MessageBox(m_hwnd, strRes.Chars(), strWp.Chars(), MB_OK | MB_ICONEXCLAMATION);
		return;
	}

	StrApp strToDelete;
	StrApp strName = m_rgencdat[iwsSel].m_stuName;
	StrApp strCode = m_rgencdat[iwsSel].m_staWs;
	if (strName.Length())
		strToDelete.Format(_T("%s  [-%s-]"), strName.Chars(), strCode.Chars());
	else
		strToDelete.Format(_T("-%s-"), strCode.Chars());


	WpDelEncDlgPtr qdlg;
	qdlg.Create();
	qdlg->SetDelEnc(iwsSel, strToDelete);
	if (qdlg->DoModal(m_hwnd) == kctidOk)
	{
		//	Do the deletion.

		// TODO: Use a Vector instead of explicit allocations?
		WsData * pwsdatOldMin = m_rgencdat;
		m_rgencdat = NewObj WsData[m_cws - 1];
		char * bTmp = NewObj char[isizeof(WsData) * m_cws];

		//	Swap the old buffer and the new one (this way the old buffer owns the ref counts
		//	for the bogus empty strings that were created in the NewObj WsData statement above).

		int cb1 = isizeof(WsData) * iwsSel;
		int cb2 = isizeof(WsData) * (m_cws - iwsSel - 1);

		memmove(bTmp, m_rgencdat, cb1);
		memmove(m_rgencdat, pwsdatOldMin, cb1);
		memmove(pwsdatOldMin, bTmp, cb1);

		memmove(bTmp, m_rgencdat + iwsSel, cb2);
		memmove(m_rgencdat + iwsSel, pwsdatOldMin + iwsSel + 1, cb2);
		memmove(pwsdatOldMin + iwsSel + 1, bTmp, cb2);

		delete[] pwsdatOldMin;
		delete[] bTmp;

		m_cws--;

		//	Update the list box.
		HWND hwndEncs = ::GetDlgItem(m_hwnd, kctidWritingSystems);
		::SendMessage(hwndEncs, LB_DELETESTRING, (WPARAM)iwsSel, 0);
		//	Update the other controls to match the new selection.
		int iwsNext = (iwsSel >= m_cws) ? m_cws - 1 : iwsSel;
		::SendMessage(hwndEncs, LB_SETCURSEL, (WPARAM)iwsNext, 0);
		InitCurrentRenderer(iwsNext);
		UpdateControlsForSel(iwsNext);
	}
}

//:>********************************************************************************************
//:>	New Writing system dialog
//:>********************************************************************************************

bool WpNewWsDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

bool WpNewWsDlg::OnApply(bool fClose)
{
	StrApp strWp(kstidAppName);
	StrApp strErr;
	StrApp strRes;

	achar rgch[32];
	memset(rgch, 0, 32 * isizeof(achar));

	// Validate the Writing System id from the "Add a Writing System" dialog

	StrAnsi staWsNew1(m_strEnc);
	if (!m_strEnc.Length())
	{
		// Invalid:  zero length string
		strRes.Load(kstidInvalidWs);
		strErr.Format(strRes, m_strEnc.Chars());
		::MessageBox(m_hwnd, strErr.Chars(), strWp.Chars(), MB_OK | MB_ICONEXCLAMATION);
		return true;
	}

	// Is the "new" id / code already in the list?
	for (int iws = 0; iws < m_cwsdat; iws++)
	{
		if (m_prgencdat[iws].m_staWs == staWsNew1)
		{
			// Invalid:  id / code already in the list
			strRes.Load(kstidDupWs);
			strErr.Format(strRes, m_strEnc.Chars());
			::MessageBox(m_hwnd, strErr.Chars(), strWp.Chars(), MB_OK | MB_ICONEXCLAMATION);
			return true;
		}
	}

	int iPos;
	bool fToLower = true;

	// Force first segment to lowercase and remaining segments to uppercase
	for (iPos = 0; staWsNew1[iPos]; iPos++)
	{
		if (fToLower)
		{
			rgch[iPos] = ::ToLower(staWsNew1[iPos]);
			if (staWsNew1[iPos] == '_')
				fToLower = false;
		}
		else
			rgch[iPos] = ::ToUpper(staWsNew1[iPos]);
	}

	StrAnsi staWsNew(rgch);
	memset(rgch, 0, 32 * isizeof(achar));
	int iRgchPos = 0;

	bool fModified = false;
	// Validate the format of id / code

	// First part of id must be 2 to 11 lowercase english letters
	int iLen = 0;
	for (iPos = 0; staWsNew[iPos] && iLen < 11; iLen++, iPos++)
	{
		if (staWsNew[iPos] == '_')
		{
			rgch[iRgchPos++] = '_';
			break;  // first segment is valid
		}

		if (staWsNew[iPos] < 'a' || staWsNew[iPos] > 'z')
		{
			// Just remove these charaters

			fModified = true;
			iLen--; // so thrown out chars do not count toward segment length
		}
		else
		{
			rgch[iRgchPos++] = staWsNew[iPos];
		}
	}

	if ((iRgchPos < 2) ||
		(iRgchPos < 3 && rgch[iRgchPos-1] == '_'))
	{
		// Invalid:  first segment too short

		strRes.Load(kstidInvalidWs);
		strErr.Format(strRes, m_strEnc.Chars());
		::MessageBox(m_hwnd, strErr.Chars(), strWp.Chars(), MB_OK | MB_ICONEXCLAMATION);
		return true;
	}

	if (staWsNew[iPos] && staWsNew[iPos] != '_')
	{
		// Invalid:  first segment too long
		// Truncate first segment
		while (staWsNew[iPos] && staWsNew[iPos] != '_')
		{
			iPos++;
			fModified = true;
		}
		if (staWsNew[iPos] && staWsNew[iPos] == '_')
			rgch[iRgchPos++] = '_';
	}

	while (staWsNew[iPos])
	{
		// remaining segments of id must be 0 to 3 uppercase letters

		// move iPos past '_'
		++iPos;
		for (iLen=0; staWsNew[iPos] && iLen<3; iLen++, iPos++)
		{
			if (staWsNew[iPos] == '_')
			{
				rgch[iRgchPos++] = '_';
				break;  // current segment is valid or zero length
			}

			if (staWsNew[iPos] < 'A' || staWsNew[iPos] > 'Z')
			{
				// Just remove these charaters

				fModified = true;
				iLen--; // so thrown out chars do not count toward segment length
			}
			else
			{
				rgch[iRgchPos++] = staWsNew[iPos];
			}
		}
		if (staWsNew[iPos] && staWsNew[iPos] != '_')
		{
			// Invalid:  current segment too long
			// Truncate the rest of this segment.

			// Find the next '_'
			while (staWsNew[iPos] && staWsNew[iPos] != '_')
			{
				fModified = true;
				iPos++;
			}
			if (staWsNew[iPos] && staWsNew[iPos] == '_')
				rgch[iRgchPos++] = '_';
		}
	}

	// Remove all trailing '_' characters.
	while (rgch[--iRgchPos] == '_')
	{
		rgch[iRgchPos] = 0;
		fModified = true;
	}

	if (fModified)
	{
		strRes.Load(kstidCorrectedWs);
		strErr.Format(strRes, m_strEnc.Chars(), rgch);
		::MessageBox(m_hwnd, strErr.Chars(), strWp.Chars(), MB_OK | MB_ICONEXCLAMATION);

		::SetDlgItemText(m_hwnd, kctidWs, rgch);

		return true;
	}

	::SetDlgItemText(m_hwnd, kctidWs, rgch);

	return SuperClass::OnApply(fClose);
}

bool WpNewWsDlg::OnNotifyChild(int id, NMHDR * pnmh, long & lnRet)
{
	if (pnmh->code == EN_CHANGE)
	{
		achar rgch[32];
		memset(rgch, 0, 32 * isizeof(achar));
		::GetDlgItemText(m_hwnd, kctidWs, rgch, 32);
		m_strEnc = rgch;
	}
	return SuperClass::OnNotifyChild(id, pnmh, lnRet);
}

//:>********************************************************************************************
//:>	Delete Writing system dialog
//:>********************************************************************************************

bool WpDelEncDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	//	Put the name of the writing system to delete in the list box.
	::SetDlgItemText(m_hwnd, kctidWsToDel, (LPCWSTR)m_str.Chars());
	//HWND hwndEnc = ::GetDlgItem(m_hwnd, kctidWsToDel);
	//::SendMessage(hwndEnc, LB_ADDSTRING, 0, (LPARAM)m_str.Chars());

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}
