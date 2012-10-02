/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TlsOptView.h
Responsibility: Rand Burgett
Last reviewed: Not yet.

Description:
	Header file for the Views tab in the Tools Options Dialog class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TLSOPTVIEW_H_INCLUDED
#define TLSOPTVIEW_H_INCLUDED

class TlsOptDlg;
class TlsOptDlgVwD;
class TlsOptDlgVwBr;
typedef GenSmartPtr<TlsOptDlgVwD> TlsOptDlgVwDPtr;
typedef GenSmartPtr<TlsOptDlgVwBr> TlsOptDlgVwBrPtr;

/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the Views tab for paragraphs.
	@h3{Hungarian: todv}
----------------------------------------------------------------------------------------------*/
class TlsOptDlgVw : public AfDialogView
{
	typedef AfDialogView SuperClass;

public:
	TlsOptDlgVw(TlsOptDlg * ptod);
	~TlsOptDlgVw();

	enum
	{
		kMoveUp = 0,	// Move Field Up
		kMoveDwn		// Move Field Down
	};

	// CAUTION: This enum corresponds to the kridTlsOptVwBmp bitmap in resources.
	// If either one changes, make sure the other is changed.
	typedef enum
	{
		kimagBrowse,
		kimagDataEntry,
		kimagDocument,
		kimagUpArrow,
		kimagDownArrow,
	};

	void SetDialogValues(UserViewSpecVec & vuvs, Set<int> * psiwndClientDel,
		int ivwInitial = 0);
	bool Apply();
	bool SetActive();

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	bool OnComboChange(NMHDR * pnmh, long & lnRet);
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	virtual bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	virtual bool OnEndLabelEdit(NMLVDISPINFO * plvdi, long & lnRet);
	virtual void OnReleasePtr();

	bool ShowDDlg();
	bool CmdCopy();
	void DeleteView();
	bool CmdModVis(Cmd * pcmd);
	bool ModFldSet();
	void UpdateVwList();
	int UpdateClassifications(int iLvItem, int ifld);
	bool CboChange();
	bool MoveFld(int updwn);
	int MakeNewView(UserViewType vwt, UserViewSpec * puvs);
	bool CmdViewAddMenu(Cmd * pcmd);
	bool CmsViewAddMenu(CmdState & cms);

	// Member variables.
	int m_dxsClient;
	int m_dysClient;
	bool m_fEditLabel;
	bool m_fEnableUpdate;
	TlsOptDlgVwDPtr m_qtodvD;
	TlsOptDlgVwBrPtr m_qtodvB;
	ITsStringPtr m_rgqFldVis[3];	//Field Visiblity
	HWND m_hwndVwList;
	int m_ivw;					//selected view listview item and index to vuvs
	int m_icd;					//selected combo box item
	HIMAGELIST m_himl;
	Set<int> * m_psiwndClientDel;
	TlsOptDlg * m_ptod;

	CMD_MAP_DEC(TlsOptDlgVw);
};

typedef GenSmartPtr<TlsOptDlgVw> TlsOptDlgVwPtr;


/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the Data Entry or DocumentViews on the
	views tab for paragraphs.
	@h3{Hungarian: todvd}
----------------------------------------------------------------------------------------------*/
class TlsOptDlgVwD : public AfDialogView
{
	typedef AfDialogView SuperClass;

public:
	TlsOptDlgVwD(TlsOptDlg * ptod);
	~TlsOptDlgVwD()
	{
	}

	bool UpdateFldList(int iSel);
	int GetSelFldIdx();

	void SetDialogValues(HIMAGELIST himl, UserViewSpecVec * pvuvs, Set<int> * psiwndClientDel);
	void SetViewIndex(int ivw)
		{ m_ivw = ivw; }

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	bool OnComboChange(NMHDR * pnmh, long & lnRet);
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	void OnReleasePtr();

	bool CmdModVis(Cmd * pcmd);
	bool ModFldSet();
	int UpdateClassifications(int iLvItem, int ifld);
	bool CboChange();
	bool MoveFld(int updwn);
	void FindFieldSpec(FldSpec ** ppfsp, RecordSpec ** pprsp);

	// Member variables.
	ITsStringPtr m_rgqFldVis[3];	//Field Visiblity
	ITsStringPtr m_qtssIndent; // Two spaces of indent
	HWND m_hwndFldList;
	int m_icd;					//selected combo box item
	UserViewSpecVec * m_pvuvs;
	HIMAGELIST m_himl;
	int m_ivw;
	Set<int> * m_psiwndClientDel;
	TlsOptDlg * m_ptod;
	int m_wsUser;		// user interface writing system id.

	CMD_MAP_DEC(TlsOptDlgVwD);
};


/*----------------------------------------------------------------------------------------------
	This class provides the functionality particular to the Views tab for Browse view paragraphs.
	@h3{Hungarian: todvb}
----------------------------------------------------------------------------------------------*/
class TlsOptDlgVwBr : public AfDialogView
{
typedef AfDialogView SuperClass;

public:
	TlsOptDlgVwBr(TlsOptDlg * ptod);
	void SetDialogValues(HIMAGELIST himl, UserViewSpecVec * pvuvs, Set<int> * psiwndClientDel);
	void SetViewIndex(int ivw)
		{ m_ivw = ivw; }

	bool UpdateFldList(int iSel);
	bool UpdateDisp(int iSel);
	void UpdateLineCtrls();

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	bool OnDeltaSpin(NMHDR * pnmh, long & lnRet);
	void UpdateEditBox();
//	void OnReleasePtr();
	bool ModFldSet();

	bool MoveFld(int updwn);

	// Member variables.
	HWND m_hwndDispList;
	HWND m_hwndHideList;
	UserViewSpecVec * m_pvuvs;
	HIMAGELIST m_himl;
	int m_nSpinValue;
	int m_ivw;
	Set<int> * m_psiwndClientDel;
	TlsOptDlg * m_ptod;
	int m_wsUser;		// user interface writing system id.
};

/*----------------------------------------------------------------------------------------------
	This class is a dialog to notify the user that he can not make a required field be not
	visible in a data entry view.
	@h3{Hungarian: mfsn}
----------------------------------------------------------------------------------------------*/
class ModFldSetNotice : public AfDialogView
{
public:
	ModFldSetNotice();
	~ModFldSetNotice()
	{
	}
};

typedef GenSmartPtr<ModFldSetNotice> ModFldSetNoticePtr;



/*----------------------------------------------------------------------------------------------
	This class provides common functionality for the Field Settings dialog.
	@h3{Hungarian: mfsd}
----------------------------------------------------------------------------------------------*/
class ModFldSetDlg : public AfDialogView
{
public:
	typedef AfDialogView SuperClass;
	ModFldSetDlg();
	~ModFldSetDlg()
	{
	}

protected:
	// Methods.

	virtual bool OpenFormatStylesDialog(HWND hwnd, bool fCanDoRtl, bool fOuterRtl,
		IVwStylesheet * past, TtpVec & vqttpPara, TtpVec & vqttpChar, bool fCanFormatChar,
		StrUni * pstuStyleName, bool & fStylesChanged, bool & fApply, bool & fReloadDb);
	virtual bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnModifyStyles(NMHDR * pnmh, long & lnRet);
	bool OnChangeCharSty(NMHDR * pnmh, long & lnRet);
	void ChangeCharSty();
	void SetVis(FldVis nvis);
	void MoveCtrl(int cid, int offset);
	void SetCharStylesCombo();

	// Member variables.
	StrUni m_stuSty; // The style selected by the user. (0 for unspecified).
	bool m_fDisableEnChange;
	FldVis m_nVis; // Visibility (fAlways/fIfData/fNever).
	UserViewType m_vwt; // View type (kvwtBrowse/kvwtDE/kvwtDoc).
};


/*----------------------------------------------------------------------------------------------
	This class handles the simplest Field Settings dialog.
	@h3{Hungarian: mbfsdt}
----------------------------------------------------------------------------------------------*/
class ModBrFldSetDlgT : public ModFldSetDlg
{
public:
	typedef ModFldSetDlg SuperClass;
	ModBrFldSetDlgT();

	void SetDialogValues(UserViewType vwt, LPCOLESTR pszSty);
	void GetDialogValues(StrUni & stuSty);

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	// Member variables.
};

typedef GenSmartPtr<ModBrFldSetDlgT> ModBrFldSetDlgTPtr;


/*----------------------------------------------------------------------------------------------
	This class handles the simplest Field Settings dialog.
	@h3{Hungarian: mfsdt}
----------------------------------------------------------------------------------------------*/
class ModFldSetDlgT : public ModFldSetDlg
{
public:
	typedef ModFldSetDlg SuperClass;
	ModFldSetDlgT();

	void SetDialogValues(FldVis vis, UserViewType vwt, LPCOLESTR pszSty);
	void GetDialogValues(FldVis * pvis, StrUni & stuSty);

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	// Member variables.
};

typedef GenSmartPtr<ModFldSetDlgT> ModFldSetDlgTPtr;


/*----------------------------------------------------------------------------------------------
	This class handles the Group Field Settings dialog.
	@h3{Hungarian: mfsdg}

	Note: this class is obsolete, since we no longer allow them to modify settings on
	groups.
----------------------------------------------------------------------------------------------*/
class ModFldSetDlgG : public ModFldSetDlg
{
public:
	typedef ModFldSetDlg SuperClass;
	ModFldSetDlgG();

	void GetDialogValues(FldVis * pvis);

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	// Member variables.
};

typedef GenSmartPtr<ModFldSetDlgG> ModFldSetDlgGPtr;


/*----------------------------------------------------------------------------------------------
	This class handles the Field Settings dialog for chooser list fields.
	@h3{Hungarian: mfsdc}
----------------------------------------------------------------------------------------------*/
class ModFldSetDlgCL : public ModFldSetDlg
{
public:
	typedef ModFldSetDlg SuperClass;
	ModFldSetDlgCL();

	void SetDialogValues(FldVis vis, UserViewType vwt, LPCOLESTR pszSty, PossNameType pnt, bool fHier,
		bool fVert);
	void GetDialogValues(FldVis * pvis, StrUni & stuSty, PossNameType * ppnt, bool * pfHier,
		bool * pfVert);

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	void UpdatePrev();

	// Member variables.
	bool m_fShowAb; // Show abbreviation.
	bool m_fShowNa; // Show name.
	bool m_fShowHi; // Show hierarchy.
	bool m_fListI; // List items in this field vertically.
};

typedef GenSmartPtr<ModFldSetDlgCL> ModFldSetDlgCLPtr;


/*----------------------------------------------------------------------------------------------
	This class handles the Field Settings dialog for chooser list fields for Browse View.
	@h3{Hungarian: mbfsdc}
----------------------------------------------------------------------------------------------*/
class ModBrFldSetDlgCL : public ModFldSetDlg
{
public:
	typedef ModFldSetDlg SuperClass;
	ModBrFldSetDlgCL();

	void SetDialogValues(UserViewType vwt, LPCOLESTR pszSty, PossNameType pnt, bool fHier,
		bool fVert);
	void GetDialogValues(StrUni & stuSty, PossNameType * ppnt, bool * pfHier,
		bool * pfVert);

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	void UpdatePrev();

	// Member variables.
	bool m_fShowAb; // Show abbreviation.
	bool m_fShowNa; // Show name.
	bool m_fShowHi; // Show hierarchy.
	bool m_fListI; // List items in this field vertically.
};

typedef GenSmartPtr<ModBrFldSetDlgCL> ModBrFldSetDlgCLPtr;


/*----------------------------------------------------------------------------------------------
	This class handles the Field Settings dialog for Expandable list fields.
	@h3{Hungarian: mfsde}
----------------------------------------------------------------------------------------------*/
class ModFldSetDlgExp : public ModFldSetDlg
{
public:
	typedef ModFldSetDlg SuperClass;
	ModFldSetDlgExp();

	void SetDialogValues(FldVis vis, UserViewType vwt, LPCOLESTR pszSty, PossNameType pnt,
		bool fHier,	bool fVert, bool fExpand, bool fIsDocVw);
	void GetDialogValues(FldVis * pvis, StrUni & stuSty, PossNameType * ppnt, bool * pfHier,
		bool * pfVert, bool * fExpand);

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	void UpdatePrev();

	// Member variables.
	bool m_fShowAb; // Show abbreviation.
	bool m_fShowNa; // Show name.
	bool m_fShowHi; // Show hierarchy.
	bool m_fListI; // List items in this field vertically.

	bool m_fIsDocVw; //True if this is a document view.
	bool m_fAlEx; // Always expand in this view.
};

typedef GenSmartPtr<ModFldSetDlgExp> ModFldSetDlgExpPtr;


/*----------------------------------------------------------------------------------------------
	This class handles the Field Settings dialog for expandable list fields for Browse View.
	@h3{Hungarian: mbfsde}
----------------------------------------------------------------------------------------------*/
class ModBrFldSetDlgExp : public ModFldSetDlg
{
public:
	typedef ModFldSetDlg SuperClass;
	ModBrFldSetDlgExp();

	void SetDialogValues(UserViewType vwt, LPCOLESTR pszSty, PossNameType pnt, bool fHier,
		bool fVert, bool fExpand, bool fIsDocVw);
	void GetDialogValues(StrUni & stuSty, PossNameType * ppnt, bool * pfHier,
		bool * pfVert, bool * fExpand);

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);
	void UpdatePrev();

	// Member variables.
	bool m_fShowAb; // Show abbreviation.
	bool m_fShowNa; // Show name.
	bool m_fShowHi; // Show hierarchy.
	bool m_fListI; // List items in this field vertically.

	bool m_fIsDocVw; //True if this is a document view.
	bool m_fAlEx; // Always expand in this view.
};

typedef GenSmartPtr<ModBrFldSetDlgExp> ModBrFldSetDlgExpPtr;


/*----------------------------------------------------------------------------------------------
	This class handles the Field Settings dialog for hierarchical items.
	@h3{Hungarian: mfsdh}
----------------------------------------------------------------------------------------------*/
class ModFldSetDlgHi : public ModFldSetDlg
{
public:
	typedef ModFldSetDlg SuperClass;
	ModFldSetDlgHi();

	void SetDialogValues(FldVis vis, UserViewType vwt, LPCOLESTR pszSty, OutlineNumSty ons,
		bool fExpand, bool fIsDocVw);
	void GetDialogValues(FldVis * pvis, StrUni & stuSty, OutlineNumSty * pons, bool * pfExpand);

protected:
	// Methods
	bool OnInitDlg(HWND hwndCtrl, LPARAM lp);
	bool OnNotifyChild(int ctid, NMHDR * pnmh, long & lnRet);

	// Member variables.
	bool m_fIsDocVw; //True if this is a document view.
	bool m_fAlEx; // Always expand in this view.
	bool m_fNS; // Numbering system enabled.
	OutlineNumSty m_ons; // Outline numbering style (knsNone/knsNum/knsNumDot).
};

typedef GenSmartPtr<ModFldSetDlgHi> ModFldSetDlgHiPtr;


#endif  // !TLSOPTVIEW_H_INCLUDED
