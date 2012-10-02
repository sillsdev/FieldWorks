/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FmtBulNumDlg.h
Responsibility: Ken Zook
Last reviewed: Not yet.

Description:
	Handles the Format-List command. This command brings up a dialog by which direct list
	formatting can be applied to the selected paragraph (or paragrahs). List formatting
	includes the following characteristics:

		- For a bulleted list,
			+ the symbols used for the bullets.

		- For a numbered list,
			+ the number scheme (see s_rgszNumberOptions for possibilities)
			+ Text to go before or after the number
			+ The font for the text
			+ The number to start the paragraph at.

	When the user clicks on OK in the dialog, the selected text is formatted according to
	the settings in the dialog.

// Sample method for presetting values, calling dialog, and getting results back.
bool RnMainWnd::CmdFmtBulNum(Cmd * pcmd)
{
	AssertObj(pcmd);

	FmtBulNumDlg::ListType ltListType = FmtBulNumDlg::kltNumber; // Numbered paragraphs.
	unsigned int iBullet = 15; // Cross for bullet.
	FmtBulNumDlg::NumberOption noNumberOption = FmtBulNumDlg::knoi; // LC Roman numerals.
	bool fParaStartAt = true; // Allow paragraph renumbering.
	int nParaStartAt = 13; // Starting paragraph number.
	achar pszTxtBef[20] = "("; // Text before number.
	achar pszTxtAft[20] = ")"; // Text after number.
	COLORREF clrNumber = kclrBrightGreen; // Color of number.
	HFONT hfontNumber = AfGdi::CreateFont(15, 0, 0, 0, FW_DONTCARE, 0, 0, 0, ANSI_CHARSET,
		OUT_TT_PRECIS, CLIP_TT_ALWAYS, DEFAULT_QUALITY, VARIABLE_PITCH | TMPF_TRUETYPE,
		"Times New Roman");

	FmtBulNumDlgPtr qfpar;
	qfpar.Create();
	// Test some input values.
	qfpar->SetDialogValues(ltListType, iBullet, noNumberOption, pszTxtBef,
		pszTxtAft, fParaStartAt, nParaStartAt, hfontNumber, clrNumber);

	AfDialogShellPtr qdlgs;
	qdlgs.Create();
	if (qdlgs->CreateDlgShell(qfpar, "Format Bullets and Numbering", m_hwnd) == kctidOk)
	{
		qfpar->GetDialogValues(&ltListType, &iBullet, &noNumberOption, &pszTxtBef[0],
			&pszTxtAft[0], &fParaStartAt, &nParaStartAt, &hfontNumber, &clrNumber);
	}

	if (hfontNumber)
		AfGdi::DeleteObjectFont(hfontNumber);
	return true;
}

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FMT_LIST_DLG_H
#define FMT_LIST_DLG_H 1

/***********************************************************************************************
	Dialog: FmtBulNumDlg.
***********************************************************************************************/

// These are the bullet characters supported, rendered in the WingDings font.
static achar * s_rgszBulletOptions[] = {
	_T("\xf09e"), _T("\xf09f"), _T("l"),  _T("m"),    _T("\xa7"), _T("n"),    _T("\xfa"),
	_T("o"),    _T("q"),    _T("r"),    _T("s"),    _T("t"),    _T("u"),    _T("v"),
	_T("z"),    _T("F"),    _T("U"),    _T("V"),    _T("\xb6"), _T("\xd8"), _T("\xdc"),
	_T("\xe0"), _T("\xe8"), _T("\xf0"), _T("\xfc")
};

static const int s_cBulletOptions = isizeof(s_rgszBulletOptions) / isizeof(achar *);

// These are the numbering systems supported.
static achar * s_rgszNumberOptions[] = {
	_T("1, 2, 3, ..."),
	_T("I, II, III, ..."),
	_T("i, ii, iii, ..."),
	_T("A, B, C, ..."),
	_T("a, b, c, ..."),
	_T("01, 02, 03, ...")
};
static const int s_cNumberOptions = isizeof(s_rgszNumberOptions) / isizeof(achar *);

/*----------------------------------------------------------------------------------------------
	Implements the view constructor for the preview window
	@h3{Hungarian: fbnpvc}
----------------------------------------------------------------------------------------------*/
class FmtBulNumPreviewVc : public VwBaseVc
{
public:
	void SetProps(ITsTextProps * pttpFirst, ITsTextProps * pttpOther)
	{
		m_pttpFirst = pttpFirst;
		m_pttpOther = pttpOther;
	}
	STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag);
protected:
	ITsTextProps * m_pttpFirst; // TsTextProps for the first numbered para (may have start-at)
	ITsTextProps * m_pttpOther; // TsTextProps for other paragraphs (should not have start-at)
};

DEFINE_COM_PTR(FmtBulNumPreviewVc);

/*----------------------------------------------------------------------------------------------
	Implements the preview window for the Format Bullets and Numbers dialog
	@h3{Hungarian: fbnp}
----------------------------------------------------------------------------------------------*/
class FmtBulNumPreview : public AfVwWnd
{
public:
	void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf, IVwRootBox ** pprootb);
	void SetProps(ITsTextProps * pttpFirst, ITsTextProps * pttpOther);
protected:
	ITsTextProps * m_pttpFirst; // TsTextProps for the first numbered para (may have start-at)
	ITsTextProps * m_pttpOther; // TsTextProps for other paragraphs (should not have start-at)
	FmtBulNumPreviewVcPtr m_qfbnpvc;
};

DEFINE_COM_PTR(FmtBulNumPreview);

/*----------------------------------------------------------------------------------------------
	Provides the user interface for looking at the current list formatting of a paragraph (or
	sequence of paragraphs), and of making changes to those paragraphs.
	Hungarian: flst
----------------------------------------------------------------------------------------------*/
class FmtBulNumDlg : public AfDialogView
{
	typedef AfDialogView SuperClass;
public:
	// A paragraph may be any one of three possible list types.
	enum ListType { kltUnspecified = FwStyledText::knUnspecified,
		kltNotAList = 0, kltBullet, kltNumber, kltLim };

//	// If a paragraph's StartAt has this value, we algorithimcally figure out the number. If
//	// the preceeding paragraph is of the same style, then our number is one greater. Otherwise,
//	// our number is '1'.
//	enum { kParaStartAtNext = INT_MIN + 1 };

	enum { kStartAtMin = 1, kStartAtMax = 14999, };

	// Enumeration of the numbering options currently supported.
	enum NumberOption
	{
		kno1 = 0, // 1, 2, 3, ...
		knoI, // I, II, III, ...
		knoi, // i, ii, iii, ...

		knoA, // A, B, C, ...
		knoa, // a, b, c, ...
		kno01, // 01, 02, 03, ...
		knoLim
	};

	// Constructors
	FmtBulNumDlg(ILgWritingSystemFactory * pwsf);
	FmtBulNumDlg(AfStylesDlg * pafsd, ILgWritingSystemFactory * pwsf);
	void BasicInit();
	void SetCanDoRtl(bool fCanDoRtl);
	void SetOuterRtl(bool f)
	{
		m_fOuterRtl = f;
	}

	// Destructor.
	~FmtBulNumDlg()
	{
		if (m_hfontBullet)
		{
			AfGdi::DeleteObjectFont(m_hfontBullet);
			m_hfontBullet = NULL;
		}
	}

	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	static bool AdjustTsTextProps(HWND hwnd, bool fCanDoRtl, bool fOuterRtl,
		TtpVec & vpttpOrig, TtpVec & vpttpHard, VwPropsVec &vqvps,
		ILgWritingSystemFactory * pwsf, const achar * pszHelpFile);
	void InitForStyle(ITsTextProps * pttp, ITsTextProps * pttpInherited,
		ParaPropRec & xprOrig, bool fEnable, bool fCanInherit);
	void GetStyleEffects(ITsTextProps *pttpOrig, ITsTextProps ** ppttp);

	/*------------------------------------------------------------------------------------------
		Fill in the given text-properties builder with the equivalent font information
		string.

		This method is static and public so it can be called from other places in the system
		that need to be able to interpret this data.
	------------------------------------------------------------------------------------------*/
	static void DecodeFontInfo(StrUni stu, ITsPropsBldr * ptpb)
	{
		const OLECHAR *pchProps = stu.Chars();
		const OLECHAR *pchPropsLim = pchProps + stu.Length();
		while (pchProps < pchPropsLim)
		{
			int tpt = *pchProps++;
			if (tpt == ktptFontFamily)
			{
				StrUni stuFontFamily(pchProps);
				CheckHr(ptpb->SetStrPropValue(ktptFontFamily, stuFontFamily.Bstr()));
				break;
			}
			int nVal = *pchProps + ((*(pchProps + 1)) << 16);
			pchProps += 2;
			int var;
			switch (tpt)
			{
			default:
				var = ktpvDefault;
				break;
			case ktptBold:
			case ktptItalic:
			case ktptSuperscript:
			case ktptUnderline:
				var = ktpvEnum;
				break;
			case ktptFontSize:
			case ktptOffset:
				var = ktpvMilliPoint;
				break;
			}
			CheckHr(ptpb->SetIntPropValues(tpt, var, nVal));
		}
	}

	/*------------------------------------------------------------------------------------------
		Fill in the given string with the equivalent information of the text properties
		object that contains the bullet-and-number font information.

		This method is static and public so it can be called from other places in the system
		that need to be able to generate this data.
	------------------------------------------------------------------------------------------*/
	static StrUni EncodeFontInfo(ITsTextProps * pttp)
	{
		OLECHAR rgchFontInfo[256]; // should be plenty
		OLECHAR * pchFontInfo = rgchFontInfo;

		int nNew, varNew;
		int rgtpt[] = { ktptItalic, ktptBold, ktptSuperscript, ktptUnderline,
						ktptFontSize, ktptOffset, ktptForeColor, ktptBackColor,
						ktptUnderColor };
		for (int itpt = 0; itpt < isizeof(rgtpt)/isizeof(int); itpt++)
		{
			int tpt = rgtpt[itpt];
			CheckHr(pttp->GetIntPropValues(tpt, &varNew, &nNew));
			if (varNew != -1)
			{
				*pchFontInfo++ = (OLECHAR) tpt;
				*pchFontInfo++ = (OLECHAR)(nNew);
				*pchFontInfo++ = (OLECHAR)(nNew >> 16);
			}
		}
		SmartBstr sbstrNew;
		CheckHr(pttp->GetStrPropValue(ktptFontFamily, &sbstrNew));
		if (sbstrNew.Length())
		{
			*pchFontInfo++ = (OLECHAR) ktptFontFamily;
			int nLen = SizeOfArray(rgchFontInfo) - (pchFontInfo - rgchFontInfo);
			wcscpy_s(pchFontInfo, nLen, sbstrNew.Chars());
			pchFontInfo += wcslen(pchFontInfo);
		}

		StrUni stuRet(rgchFontInfo, (pchFontInfo - rgchFontInfo));
		return stuRet;
	}

protected:
	bool m_fCanDoRtl;
	bool m_fOuterRtl;
	int m_nRtl;				// direction of selected paragraphs
	AfStylesDlg * m_pafsd;

	// Values directly displayed in dialog
	ListType m_ltListType;			// Current radio button setting for list type.
	unsigned int m_iCbBullet;		// Current bullet option (as displayed in combo).
	unsigned int m_iCbNumber;		// Current number option (as displayed in combo).
	bool m_fCxStartAt;				// Flag indicating whether m_nEdStartAt is meaningful.
	int m_nEdStartAt;				// Number of paragraph that was typed/passed in
	StrUni m_stuEdTxtBef;			// Text prior to the number (numbered list).
	enum {kcchEdTxtMax = 20};
	StrUni m_stuEdTxtAft;			// Text following the number (numbered list).
	StrUni m_stuFontInfo;
//	bool m_fGotFontInfo;			// True if a font is specified.
	// Derived and helper values
	HFONT m_hfontBullet;			// Font for Bullet Option combo (WingDings).
	int m_nStartAtMin;				// Minimum value of start-at (0 or 1)
	int m_nStartAtMax;				// Maximum value of start-at ()
	StrApp m_strEdStartAt;			// Last valid displayed value
	int m_icchEdStartAtSelMin;		// Start of last valid selection
	int m_icchEdStartAtSelLim;		// End of last valid selection
	int m_cbTxtAft;					// Size of buffer for text before and text after.
	// Other variables
	IVwPropertyStorePtr m_qvps; // The context in which we are editing.
	ILgWritingSystemFactoryPtr m_qwsf;

	// Inheritance
	bool m_fCanInherit;
	int m_xEorI;	// explicit, inherited, or conflicting
	ListType m_ltListTypeI;
	unsigned int m_iCbBulletI;
	unsigned int m_iCbNumberI;
	bool m_fCxStartAtI;
	int m_nEdStartAtI;
	StrUni m_stuEdTxtBefI;
	StrUni m_stuEdTxtAftI;
	StrUni m_stuFontInfoI;

	// For preview; m_qttpFirst also records current number font settings
	ITsTextPropsPtr m_qttpFirst; // TsTextProps for the first numbered para (may have start-at)
	ITsTextPropsPtr m_qttpOther; // TsTextProps for other paragraphs (should not have start-at)
	FmtBulNumPreviewPtr m_qfbnp; // Actual preview window.

	class CPropBulNum {
	public:
		CPropBulNum() // constructor
		{
			ltListType = kltNotAList;
			nEdStartAt = 1;
			stuEdTxtBef.Clear();
			stuEdTxtAft.Clear();
			stuFontInfo.Clear();
		}
		int ltListType;
		int nEdStartAt;
		StrUni stuEdTxtBef;
		StrUni stuEdTxtAft;
		StrUni stuFontInfo;			// (contents of this are UFontTemp (binary data))
		};
	CPropBulNum m_curr; // Current properties
	int m_wsDefault;
	bool m_fDisableEnChange;		// Disable change notification to prevent endless recursion.

	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	bool OnComboChange(NMHDR * pnmh, long & lnRet);
	bool OnFontChange(NMHDR * pnmh, long & lnRet);
	// Handle What's This? help.
	virtual bool OnHelpInfo(HELPINFO * phi)
	{
		if (m_pafsd)
			return m_pafsd->DoHelpInfo(phi, m_hwnd);
		else
			return SuperClass::OnHelpInfo(phi);
	}

	void UpdatePreview(DRAWITEMSTRUCT * pdis);
	void SetListType(ListType eNewType);
	void SetEdStartAt(int start_at, bool is_keyin);
	void FormatListNumber(int nNum1, int nNum2, int nOffset, StrAppBuf & strb);
	int ConvStrToNum(int number_option, const achar * str);

	bool ColorForInheritance(WPARAM wp, LPARAM lp, long & lnRet);

	void GetProps(ITsTextProps * pttp, CPropBulNum & cpt, int & nRtl);
	bool AdjustTsTextPropsDo (HWND hwnd, TtpVec & vqttpOrig, TtpVec & vpttpHard,
		VwPropsVec &vqvps);
	void DecodeValues(CPropBulNum &prop, int nRtl, bool fExplicit, bool fFillCtls);
	void EncodeValues(CPropBulNum &prop);
	void MergeExplicitAndInherited(CPropBulNum & cptE, CPropBulNum & cptI);
	void SetExplicit(int x);
//	void DecodeFontInfo(StrUni &str, HFONT &hfont, COLORREF &clr);
//	void EncodeFontInfo(StrUni &str, HFONT &hfont, COLORREF &clr);
	void UpdateIntProp(
		ITsTextProps * pttp,		// text properties
		int tpt,					// storage id of the integer property
		ITsPropsBldrPtr & qtpb,		// builder for any new text properties
		int nOld,					// old value
		int nNew,					// new value
		int nVarOld,				// old variation
		int nVarNew,				// new variation
		int nMul, int nDiv);		// scaling to be applied
	void UpdateStringProp(
		ITsTextProps * pttp,		// text properties
		int tpt,					// storage id of the string property
		ITsPropsBldrPtr & ptpb,		// property builder (new or NULL)
		StrUni &strOld,				// old string
		StrUni &strNew);			// new string
	void AdjustPreview();
	virtual void PostAttach(void);
	bool OnDrawChildItem(DRAWITEMSTRUCT * pdis);

	ListType LtListType()
	{
		switch (m_xEorI)
		{
		case kxConflicting:	return (ListType)FwStyledText::knConflicting;
		case kxExplicit:	return m_ltListType;
		case kxInherited:	return m_ltListTypeI;
		}
		Assert(false);
		return m_ltListType;
	}
	int ICbBullet()
	{
		switch (m_xEorI)
		{
		case kxConflicting:	return -1;
		case kxExplicit:	return m_iCbBullet;
		case kxInherited:	return m_iCbBulletI;
		}
		Assert(false);
		return m_iCbBullet;
	}
	int ICbNumber()
	{
		switch (m_xEorI)
		{
		case kxConflicting:	return -1;
		case kxExplicit:	return m_iCbNumber;
		case kxInherited:	return m_iCbNumberI;
		}
		Assert(false);
		return m_iCbNumber;
	}
	int FCxStartAt()
	{
		switch (m_xEorI)
		{
		case kxConflicting:	return FwStyledText::knConflicting;
		case kxExplicit:	return (int)m_fCxStartAt;
		case kxInherited:	return (int)m_fCxStartAtI;
		}
		Assert(false);
		return m_fCxStartAt;
	}
	int NEdStartAt()
	{
		switch (m_xEorI)
		{
		case kxConflicting:	return FwStyledText::knConflicting;
		case kxExplicit:	return m_nEdStartAt;
		case kxInherited:	return m_nEdStartAtI;
		}
		Assert(false);
		return m_nEdStartAt;
	}
	StrUni StuEdTxtBef()
	{
		switch (m_xEorI)
		{
		case kxConflicting:	return L"";
		case kxExplicit:	return m_stuEdTxtBef;
		case kxInherited:	return m_stuEdTxtBefI;
		}
		Assert(false);
		return m_stuEdTxtBef;
	}
	StrUni StuEdTxtAft()
	{
		switch (m_xEorI)
		{
		case kxConflicting:	return L"";
		case kxExplicit:	return m_stuEdTxtAft;
		case kxInherited:	return m_stuEdTxtAftI;
		}
		Assert(false);
		return m_stuEdTxtAft;
	}
	StrUni StuFontInfo()
	{
		switch (m_xEorI)
		{
		case kxConflicting:	return L"";
		case kxExplicit:	return m_stuFontInfo;
		case kxInherited:	return m_stuFontInfoI;
		}
		Assert(false);
		return m_stuFontInfo;
	}
};

typedef GenSmartPtr<FmtBulNumDlg> FmtBulNumDlgPtr;

#endif // !FMT_LIST_DLG_H
