/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TssEdit.h
Responsibility: Rand Burgett
Last reviewed:

	This is the base Sdk class of an editbox designed for TsStrings.
	This class is used for Sdk applications, and is also the base for an ActiveX control.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TSSEDIT_H
#define TSSEDIT_H 1

// The reason we are using WM_APP here instead of WM_USER is because on some machines, using
// WM_USER as the base caused messages to be converted to another message somehow. I
// (DarrellZ) don't understand how the messages were getting converted, but using WM_APP
// seems to work.
enum
{
	FW_EM_GETLINE = WM_APP + 1,	// wp = int iLine, lp = ITsString ** pptss.
	FW_EM_REPLACESEL,			// wp = Ignored, lp = ITsString * ptss.
	FW_EM_GETTEXT,				// wp = Ignored, lp = ITsString ** pptss.
	FW_EM_SETTEXT,				// wp = Ignored, lp = ITsString * ptss.
	FW_EM_GETSTYLE,				// wp = COLORREF * pclrBack, lp = StrUni * pstu.
	FW_EM_SETSTYLE,				// wp = COLORREF clrBack, lp = StrUni * pstu.
};


class TssEdit;
class TssEditVc;
typedef GenSmartPtr<TssEdit> TssEditPtr;
typedef GenSmartPtr<TssEditVc> TssEditVcPtr;


/*----------------------------------------------------------------------------------------------
	This class supports editing a TsString.
----------------------------------------------------------------------------------------------*/
class TssEdit : public AfVwScrollWnd
{
	friend TssEditVc;

	typedef AfVwScrollWnd SuperClass;

public:
	TssEdit();

	void SubclassEdit(HWND hwndDlg, int cid, ILgWritingSystemFactory * pwsf, int ws,
		DWORD dwStyleExtra);
	void Create(HWND hwndPar, int cid, DWORD dwStyle, HWND hwndToolTip, const achar * psz,
		ILgWritingSystemFactory * pwsf, int ws, IActionHandler * pacth = NULL);
	void Create(HWND hwndPar, int cid, DWORD dwStyle, HWND hwndToolTip, ITsString * ptss,
		ILgWritingSystemFactory * pwsf, int ws, IActionHandler * pacth = NULL);
	virtual void PreCreate(ILgWritingSystemFactory * pwsf, int ws, ITsString * ptss = NULL,
		IActionHandler * pacth = NULL);
	virtual void PostCreate(ITsString * ptss = NULL);

	bool HasToolTip()
	{
		return (m_hwndToolTip != NULL);
	}
	int Cid()
	{
		return m_cid;
	}

	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb);

	int GetTextLength();
	uint GetLimitText();
	int GetLine(int iLine, ITsString ** pptss);
	int GetLineCount();
	uint GetMargins();
	bool GetSel(int * pichAnchor, int * pichEnd, bool * pfAssocPrev = NULL);
	void GetStyle(StrUni * pstu, COLORREF * pclrBack);
	int GetText(ITsString ** pptss);
	int LineFromChar(int iLine = -1);
	int LineIndex(int iLine = -1);
	int LineLength(int iLine = -1);
	bool LineScroll(int cLines, int cch = 0);
	void ReplaceSel(ITsString * ptss);
	void ScrollCaret();
	void SetLimitText(uint nMax);
	void SetMargins(int fwMargin, int dxpLeft, int dxpRight);
	void SetReadOnly(bool fReadOnly = true);
	void SetSel(int ichAnchor, int ichEnd);
	void SetStyle(StrUni * pstu, COLORREF clrBack);
	void SetText(ITsString * ptss);
	void SetEditable(TptEditable nEditable)
	{
		m_nEditable = nEditable;
	}
	void SetShowTags(bool fShowTags)
	{
		m_fShowTags = fShowTags;
	}

	// Clipboard operations
	void Copy();
	void Cut();
	void Paste();

	/*------------------------------------------------------------------------------------------
		Notification message handlers.
		NOTE: Override these in the derived class to get the events.
	------------------------------------------------------------------------------------------*/
	virtual bool OnChange()
	{
		return false;
	}
	virtual bool OnKillFocus(HWND hwndNew)
	{
		SuperClass::OnKillFocus(hwndNew);
		return false;
	}
	virtual bool OnSetFocus(HWND hwndOld, bool fTbControl = false);
	virtual bool OnUpdate()
	{
		return false;
	}
	virtual bool OnVScroll(int wst, int yp, HWND hwndSbar)
	{
		SuperClass::OnVScroll(wst, yp, hwndSbar);
		return false;
	}

	// scrolling
	virtual void GetScrollOffsets(int * pdxd, int * pdyd);
	virtual int LayoutWidth();
	virtual bool OnHScroll(int nSBCode, int nPos, HWND hwndSbar);
	virtual void MakeSelectionVisible1(IVwSelection * psel);
	virtual void ScrollSelectionNearTop1(IVwSelection * psel);

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void OnChar(UINT nChar, UINT nRepCnt, UINT nFlags);
	virtual int GetHorizMargin()
		{ return 0; }
	virtual void MakeSelectionVisible1()
		{ } // Do nothing.
	virtual void ScrollSelectionNearTop1()
		{ }

	virtual bool OnCharTab()
		{ return false; }
	virtual bool OnCharEnter()
		{ return false; }
	virtual bool OnCharEscape()
		{ return false; }

	virtual bool CmdEditCopy1(Cmd * pcmd);
	virtual bool CmdEditCut1(Cmd * pcmd);
	virtual bool CmdEditPaste1(Cmd * pcmd);

	virtual bool OnNotifyChild(int id, NMHDR * pnmh, long & lnRet);

	virtual COLORREF GetWindowColor()
	{
		return m_clrBack;
	}

	enum
	{
		khvoString = 1,
		ktagString = 2,
		kfrString  = 3,
	};

	int m_cid;
	int m_dxpMarginLeft;
	TptEditable m_nEditable;
	int m_dxpMarginRight;
	int m_dypMarginTop;
	int m_dxpScrollOffset;
	VwCacheDaPtr m_qcda;
	StrUni m_stuStyle;
	COLORREF m_clrBack;
	bool m_fInDialog; // true if control is in a dialog (SubclassEdit was called).
	HWND m_hwndToolTip;
	bool m_fShowTags; // True if we are to show overlay tags. False otherwise.

	int m_wsBase;
	ILgWritingSystemFactoryPtr m_qwsf;
};


/*----------------------------------------------------------------------------------------------
	The main view constructor for the TssEdit window.
	Hungarian: tevc.
----------------------------------------------------------------------------------------------*/
class TssEditVc : public VwBaseVc
{
	typedef VwBaseVc SuperClass;

public:
	TssEditVc(TssEdit * pte, TptEditable tpt, bool fShowTags, ComBool fRtl)
	{
		AssertPtr(pte);
		m_pte = pte;
		m_tptEditable = tpt;
		m_fShowTags = fShowTags;
		m_fRtl = fRtl;
	}

	// IVwViewConstructor methods.
	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);

protected:
	TssEdit * m_pte;
	TptEditable m_tptEditable;
	bool m_fShowTags; // True if we are to show overlay tags. False otherwise.
	ComBool m_fRtl;		// True if the base writing system is Right-To-Left.
};


#endif //!TSSEDIT_H
