/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FmtFntDlg.h
Responsibility: John Landon
Last reviewed: Not yet.

Description:
	Header file for the Format/Font Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FMTFNTDLG_H_INCLUDED
#define FMTFNTDLG_H_INCLUDED

// Global function.
bool IsValidFontSize(int dypt);

/*----------------------------------------------------------------------------------------------
	The edit box inside the font combo-box; this is used to intercept backspace characters
	and make them work the way we want. It can also hold gray text.
	Hungarian: sce
----------------------------------------------------------------------------------------------*/
class SimpleComboEdit : public AfWnd
{
	typedef AfWnd SuperClass;
public:
	void SubclassComboEdit(HWND hwndComboEdit, HWND hwndParent)
	{
		m_hwndParent = hwndParent;
		SubclassHwnd(hwndComboEdit);
	}

	void SetMonitor(int * px)
	{
		m_pxMonitor = px;
	}

protected:
	int * m_pxMonitor; // value to monitor whether text should be gray
	HWND m_hwndParent;

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
};
typedef GenSmartPtr<SimpleComboEdit> SimpleComboEditPtr;


/*----------------------------------------------------------------------------------------------
	An editable combo-box that can show gray text, indicating inheritance.
----------------------------------------------------------------------------------------------*/
class GrayableEditCombo : public AfWnd
{
public:
	void SubclassCombo(HWND hwndCombo)
	{
		SubclassHwnd(hwndCombo);
	}

	void SetMonitor(int * px)
	{
		m_pxMonitor = px;
	}

protected:
	int * m_pxMonitor; // value to monitor to indicate whether text should be grayed.

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
	{
		if (WM_CTLCOLOREDIT == wm)
		{
			::SetBkColor((HDC)wp, ::GetSysColor(COLOR_WINDOW));
			::SetTextColor((HDC)wp, (*m_pxMonitor == kxInherited ? kclrGray50 :
				::GetSysColor(COLOR_WINDOWTEXT)));
			return true;
		}
		return AfWnd::FWndProc(wm, wp, lp, lnRet);
	}
};
typedef GenSmartPtr<GrayableEditCombo> GrayableEditComboPtr;


/*----------------------------------------------------------------------------------------------
	An non-editable combo-box that handles arrow keys right when the current selection is -1,
	that is, empty. This allows it to handle having something like "unspecified" as the first
	menu item, but showing the contents of the box as blank when that selection is made; and
	yet still a down arrow takes you to the second item in the menu.
	Hungarian: bfsc
----------------------------------------------------------------------------------------------*/
class BlankFirstSelCombo : public AfWnd
{
	typedef AfWnd SuperClass;
public:
	void SubclassCombo(HWND hwndCombo)
	{
		SubclassHwnd(hwndCombo);
	}
protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
};
typedef GenSmartPtr<BlankFirstSelCombo> BlankFirstSelComboPtr;


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Format/Font Dialog.
	Hungarian: ffd.
----------------------------------------------------------------------------------------------*/
class FmtFntDlg : public AfDialogView
{
	typedef AfDialogView SuperClass;

public:
	FmtFntDlg();
	~FmtFntDlg();

	enum {
		kMaxFeature = 64,
		kMaxValPerFeat = 32,	// arbitrary
		kGrLangFeature = 1,		// Graphite built-in 'lang' feature
	};

	// list of selected font features; hungarian: fl
	struct FeatList
	{
		// This array holds the actual setting values, not the indices into the list of values.
		// However, it is indexed by feature INDEX, not feature ID.
		int rgn[kMaxFeature];

		void Init()
		{
			for (int i = 0; i < kMaxFeature; i++)
				rgn[i] = INT_MAX; // default
		}
		void MakeConflicting()
		{
			for (int i = 0; i < kMaxFeature; i++)
				rgn[i] = knConflicting;
		}
	};

	void SetDlgValues(const LgCharRenderProps & chrp, const LgCharRenderProps & chrpI,
		StrUni stuFfCur, StrUni stuFfI, ChrpInheritance & chrpi, int wsPrev);
	void GetDlgValues(LgCharRenderProps & chrpNew, LgCharRenderProps & chrpOrig);
	static bool AdjustTsTextProps(HWND hwnd, TtpVec & vqttp, VwPropsVec & vqvpsSoft,
		ILgWritingSystemFactory * pwsf, const achar * pszHelpFile, bool fBullNum = false,
		bool fBullet = false, bool fFeatures = false, bool f1DefaultFont = false);
	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	static bool SetupFeaturesMenu(HMENU & hmenu, IRenderingFeatures * prenfeat, int nLang,
		Vector<int> & vnMenuMap, int * prgnVal, int * prgnDefaults = NULL);
	static void HandleFeaturesMenu(IRenderingFeatures * prenfeat, int item,
		Vector<int> & vnMenuMap, int * prgnVal);
	static StrUni GenerateFeatureString(IRenderingFeatures * prenfeat, int * prgnVal);
	static void ParseFeatureString(IRenderingFeatures * prenfeat, StrUni stu, int * prgnVal);
	static bool HasFeatures(IRenderingFeatures * prenfeat);

	void SetWritingSystemFactory(ILgWritingSystemFactory * pwsf)
	{
		m_qwsf = pwsf;
	}

protected:
	static Pcsz s_rgpszSizes[]; // Not currently used.

	LgCharRenderProps m_chrpCur;	// The character properties as changed via the dialog.
	LgCharRenderProps m_chrpOld;	// Original char properties when the view is initialized.
	LgCharRenderProps m_chrpSoft;
	StrUni m_stuFfCur;		// The font family as changed via the dialog.
	StrUni m_stuFfOld;		// Old font family (or <inherit> or blank if unspecified).
	StrUni m_stuFfSoft;
	ChrpInheritance m_chrpi;

	int m_idyptMinSize;		// Index of minimum font size from g_rgdyptSize.
	int m_istidMinUnder;	// Index of minimum underline type from g_rgstidUnder.
	int m_istidMinSsv;		// Not currently used. (Index in g_rgssvSuper.)
	int m_istidMinOffset;	// Index of minimum offset type from g_rgstidOffset.

	UiColorComboPtr m_qccmbF;	// Pointer to the foreground color combo box.
	UiColorComboPtr m_qccmbB;	// Pointer to the background color combo box.
	UiColorComboPtr m_qccmbU;	// Pointer to the underline color combo box.
	GrayableEditComboPtr m_qgecmbFont;
	GrayableEditComboPtr m_qgecmbSize;
	BlankFirstSelComboPtr m_qbfscOffset;

	LgCharRenderProps m_chrpPrvw; // This is used for the Preview window.
	StrUni m_stuFfPrvw;			// This is used for the Preview window.
	HFONT m_hfontPrvw;			// This is used for the Preview window.
	LOGFONT m_lfPrvw;			// This is used for the Preview window.
	int m_wsPrev;

	bool m_fBullNum;	// Set 'true' if being used by FmtBulNumDlg.
	bool m_fBullet;		// Set 'true' if used for bulleted list.
	bool m_fFeatures;	// True if we want a Features button
	bool m_f1DefaultFont; // only one default font (e.g., WorldPad)

	// These three variables get modified continually as the user drags across the
	// color combo. If he actually makes a selection they get copied back to the corresponding
	// variable in m_chrpCur.
	COLORREF m_clrFore;
	COLORREF m_clrBack;
	COLORREF m_clrUnder;

	IRenderingFeaturesPtr m_qrenfeat;	// for the relevant writing system/ows; often NULL
	FeatList m_flCur;				// settings for current font features, if any
	FeatList m_flWs;				// defaults for the relevant writing system
	StrUni m_stuFlWs;				// default font to which m_flWs applies
	int m_wsFl;						// writing system for generating features
	Vector<FeatList> m_vfl;			// feature lists for each run of text
	HMENU m_hmenuFeatures;			// menu for selecting font features
	Vector<int> m_vnFeatMenuIDs; // map between feature menu items and feature index/value pairs

	ILgWritingSystemFactoryPtr m_qwsf;

	// The current values of the dialog, either explicit or inherited.
	StrUni FontFamily()
	{
		return (m_chrpi.xFont == kxInherited) ? m_stuFfSoft : m_stuFfCur;
	}
	int Size()
	{
		return (m_chrpi.xSize == kxInherited) ? m_chrpSoft.dympHeight : m_chrpCur.dympHeight;
	}
	int Italic()
	{
		return (m_chrpi.xItalic == kxExplicit) ? m_chrpCur.ttvItalic : m_chrpSoft.ttvItalic;
	}
	int Bold()
	{
		return (m_chrpi.xBold == kxExplicit) ? m_chrpCur.ttvBold : m_chrpSoft.ttvBold;
	}
	int Superscript()
	{
		return (m_chrpi.xSs == kxExplicit) ? m_chrpCur.ssv : m_chrpSoft.ssv;
	}
	int Offset()
	{
		return (m_chrpi.xOffset == kxExplicit) ? m_chrpCur.dympOffset : m_chrpSoft.dympOffset;
	}
	COLORREF ForeColor()
	{
		return (m_chrpi.xFore == kxExplicit) ? m_chrpCur.clrFore : m_chrpSoft.clrFore;
	}
	COLORREF BackColor()
	{
		return (m_chrpi.xBack == kxExplicit) ? m_chrpCur.clrBack : m_chrpSoft.clrBack;
	}
	COLORREF UnderColor()
	{
		return (m_chrpi.xUnder == kxExplicit) ? m_chrpCur.clrUnder : m_chrpSoft.clrUnder;
	}
	int Underline()
	{
		return (m_chrpi.xUnderT == kxExplicit) ? m_chrpCur.unt : m_chrpSoft.unt;
	}
	void CopyCurrentTo(LgCharRenderProps & chrp)
	{
		chrp.dympHeight = Size();
		chrp.ttvItalic = (byte)Italic();
		chrp.ttvBold = (byte)Bold();
		chrp.ssv = (byte)Superscript();
		chrp.dympOffset = Offset();
		chrp.clrFore = ForeColor();
		chrp.clrBack = BackColor();
		chrp.clrUnder = UnderColor();
		chrp.unt = (byte)Underline();
		chrp.ws = m_chrpCur.ws;
		chrp.fWsRtl = m_chrpCur.fWsRtl;
	}

	void FillCtls(void);
	bool ColorForInheritance(WPARAM wp, LPARAM lp, long & lnRet);
	void UpdateComboWithInherited(int ctid, NMHDR * pnmh);
	void DisableForBullNum();	// Disables some controls for fixed bullets font name etc.
	void SetSize(int dymp);
	void SetCheck(int ctid, int & ttv, bool fAdvance = false, bool fAllowConflict = true);
	void SetUnder(int unt);
	void SetOffset(int dymp);

	void RecordInitialFeatures(TtpVec & vqttp);
	void SetFontVarSettings();
	void EnableFontFeatures();
	void CreateFeaturesMenu();
	bool CmdFeaturesPopup(Cmd * pcmd);
	void UpdateFontVariations(int ittp, ITsTextProps * pttp, ITsPropsBldrPtr & qtpb);
	bool SetFeatureEngine(IRenderEngine ** ppreneng);

	bool OnInitDlg(HWND hwndCtrl, LPARAM lParam);
	virtual bool OnApply(bool fClose); // Intercept click on OK key to verify font size.
	bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	bool OnDeltaSpin(NMHDR * pnmh, long & lnRet, bool fKillFocus = false);
	bool OnComboChange(NMHDR * pnmh, long & lnRet);
	bool OnComboUpdate(NMHDR * pnmh, long & lnRet);
	bool OnKillFocus(NMHDR * pnmh, long & lnRet);
	bool OnDrawChildItem(DRAWITEMSTRUCT * pdis);

	void UpdatePreview(DRAWITEMSTRUCT * pdis);
	void UpdateUnderlineStyle(DRAWITEMSTRUCT * pdis);

	static int CALLBACK FontCallBack(ENUMLOGFONTEX * pelfe, NEWTEXTMETRICEX * pntme,
		DWORD ft, LPARAM lp);

	CMD_MAP_DEC(FmtFntDlg);
};


/*----------------------------------------------------------------------------------------------
	Extended combo box for allowing typeahead.
	Hungarian: afdc
----------------------------------------------------------------------------------------------*/
class AfFntDlgCombo : public TssComboEx
{
	typedef TssComboEx SuperClass;
	friend class AfStyleFntDlg;

public:

	AfFntDlgCombo();

//	virtual void Create(HWND hwndPar, DWORD dwStyle, int wid, int iButton, Rect & rc,
//		HWND hwndToolTip, bool fTypeAhead);

	virtual bool OnEndEdit(FW_NMCBEENDEDIT * pfnmee, long & lnRet);
	virtual bool OnSelChange(int nID, HWND hwnd);

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);
	virtual bool OnSelEndOK(int nID, HWND hwndCombo);
};
typedef GenSmartPtr<AfFntDlgCombo> AfFntDlgComboPtr;


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Format/Style.../Font Dialog.
	Hungarian: asfd.
----------------------------------------------------------------------------------------------*/
class AfStyleFntDlg : public AfDialogView
{
	typedef AfDialogView SuperClass;
	friend class AfFntDlgCombo;
	typedef FmtFntDlg::FeatList FeatList;

public:
	AfStyleFntDlg(AfStylesDlg * pafsd);

	void SetDlgValues(ITsTextProps * pttp, ITsTextProps * pttpInherited,
		bool fCanInherit, bool fFontFeatures, bool f1DefaultFont,
		bool fCharStyle, Vector<int> & vwsProj);
	void SetDlgWsValues(Vector<int> & vwsProj);
	bool GetDlgValues(ITsTextProps * pttp, ITsTextProps ** ppttp, bool fForPara);
#if 0
	static bool AdjustTsTextProps(HWND hwnd, int cttp, ITsTextProps ** prgpttp,
		IVwPropertyStore ** prgpvps);
#endif

	bool SetActive(); // (HWND hwndDialog); // Pass dialog handle to tab.
	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	void UpdateUnderlineStyle(DRAWITEMSTRUCT * pdis);
	bool OnDrawChildItem(DRAWITEMSTRUCT * pdis);
	virtual bool OnCommand(int cid, int nc, HWND hctl);

	virtual bool QueryClose(QueryCloseType qct);

	void SetWritingSystemFactory(ILgWritingSystemFactory * pwsf)
	{
		m_qwsf = pwsf;
	}

protected:
	bool m_fCharStyle;
	bool m_fCanStyleInherit; // false for Normal style (or perhaps some other with no base)
	bool m_fCanInherit; // false only for Normal style <default properties>
	bool m_fFeatures;	// true if we want a Features button
	bool m_fFeatInit;	// true when the features button has been initialized
	bool m_f1DefaultFont;

	// Gives the current (possibly edited) value of each property for each writing system.
	WsStyleVec m_vesi;
	// Gives a composite value for each property, which if all selected rows have
	// the same value, is that value, and otherwise is knConflicting.
	WsStyleInfo m_esiSel;
	WsStyleInfo m_esiOld; // value when FillCtls last called, restored if validation fails.

	// To manage inheritance:
	ChrpInherVec m_vchrpi;
	ChrpInheritance m_chrpi;
	WsStyleVec m_vesiI;	// inherited values
	WsStyleInfo m_esiSelI;

	bool m_fFfConflict; // true if font family in m_esiSel is conflicting
	bool m_fFfConflictI;

	static Pcsz s_rgpszSizes[];
	bool m_fDirty; // true if anything changed.
	HWND m_hwndLangList;
	bool m_fFfVerifySizeActive; // true when inside VerifySize() method, to avoid nested calls

	int m_idyptMinSize;
	int m_istidMinUnder;
	int m_istidMinOffset;
	UiColorComboPtr m_qccmbF;
	UiColorComboPtr m_qccmbB;
	UiColorComboPtr m_qccmbU;
	GrayableEditComboPtr m_qgecmbSize;
	BlankFirstSelComboPtr m_qbfscOffset;

	IRenderingFeaturesPtr m_qrenfeat;	// for the relevant writing system/ows; often NULL
	int m_iwsSel;					// index of writing system selected as basis for font features (1st selected)
	Vector<FeatList> m_vflWs;		// defaults for the relevant writing system/ows
	Vector<FeatList> m_vfl;			// feature lists for each writing system
	HMENU m_hmenuFeatures;			// menu for selecting font features
	Vector<int> m_vnFeatMenuIDs; // map between feature menu items and feature index/value pairs

	// These three variables get modified continually as the user drags across the
	// color combo. If he actually makes a selection they get copied back to the corresponding
	// variable in m_esiSel.
	COLORREF m_clrFore;
	COLORREF m_clrBack;
	COLORREF m_clrUnder;

	AfStylesDlg * m_pafsd; // The parent Format/Styles dialog.

	ILgWritingSystemFactoryPtr m_qwsf;

	bool ColorForInheritance(WPARAM wp, LPARAM lp, long & lnRet);

	void SetInheritedDefaults();
	void SetCanInherit(bool fCanInherit);
	void SetCanWsInherit();
	void InitFontCtl();
	void InitUnderlineCtl();
	void InitOffsetCtl();

	void FillCtls(void);
	void ReadCtls();
	void SetSize(int dymp);
	void SetCheck(int ctid, int & ttv, bool fAdvance = false, bool fAllowConflict = true);
	void SetUnder(int unt);
	void SetOffset(int & dymp);

	bool OnInitDlg(HWND hwndCtrl, LPARAM lParam);
	bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	bool OnDeltaSpin(NMHDR * pnmh, long & lnRet);
	bool OnComboChange(NMHDR * pnmh, long & lnRet);
	bool OnComboUpdate(NMHDR * pnmh, long & lnRet);

	virtual bool OnHelpInfo(HELPINFO * phi);
	void UpdateDescrips();
	static int CALLBACK FontCallBack(ENUMLOGFONTEX * pelfe, NEWTEXTMETRICEX * pntme,
		DWORD ft, LPARAM lp);
	void ReadCtl(int nNew, int & nReal, int x);
	void ReadCtl(COLORREF nNew, COLORREF & nReal, int x);
	void ReadInheritance(int xNew, int & xReal);
	bool VerifySize();
	void UpdateComboWithInherited(int ctid, NMHDR * pnmh);
	int SelectStringInTssCombo(HWND hwndCombo, StrAnsi sta);
	int GetTextInTssCombo(HWND hwndCombo, StrApp * pstr);
	int GetItemTextInTssCombo(HWND hwndCombo, int iitem, StrApp * pstr);

	void SetFontFeatures(bool fFeatures);
	void SetFontVarSettings(bool fOnlySelected = false);
	void EnableFontFeatures();
	void CreateFeaturesMenu();
	bool CmdFeaturesPopup(Cmd * pcmd);
	void UpdateFontFeatState();
	bool SetFeatureEngine(int iws, StrUni stuFaceName);

	CMD_MAP_DEC(AfStyleFntDlg);
};


/*----------------------------------------------------------------------------------------------
	This class provides the functionality of the Remove Formatting Dialog.
	Hungarian: rfd.
	OBSOLETE, since the Remove Formatting dialog is no longer enabled.
----------------------------------------------------------------------------------------------*/
class RemFmtDlg : public AfDialogView
{
public:
	RemFmtDlg();

	typedef AfDialogView SuperClass;

	void SetValue(int ctid, int ttv);
	int GetValue(int ctid);

protected:
	int m_rgttv[11];

	static int PropIndex(int ctid);

	bool OnInitDlg(HWND hwndCtrl, LPARAM lParam);
	bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);

	void SetCheck(int ctid, int ttv);
};


// Smart pointers.
typedef GenSmartPtr<FmtFntDlg> FmtFntDlgPtr;
typedef GenSmartPtr<AfStyleFntDlg> AfStyleFntDlgPtr;
typedef GenSmartPtr<RemFmtDlg> RemFmtDlgPtr;

#endif  /*FMTFNTDLG_H_INCLUDED*/
