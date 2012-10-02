/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: RecMainWndSupportWnds.h
Responsibility: Darrell Zook
Last reviewed:

Description:
	This file contains class declarations for the following classes:
		AfViewBarBase : A base class exposing methods common to AfListBar and AfTreeBar
		AfListBar : AfWnd, AfViewBarBase - This window contains the actual view bar items and
			is responsible for drawing them. It can handle both large and small icons.
		AfRecListBar : AfListBar - This window contains view bar items used by record-
			based apps.
		AfTreeBar : TssTreeView, AfViewBarBase - This window is a tree control that is embedded
			inside an AfViewBar.
		AfOverlayListBar : AfListBar - This window supports multiple selections, and should be
			used when overlay support is required.
		AfViewBar : AfWnd - This window contains multiple AfListBar windows. It is in charge of
			switching between AfListBar windows and communicating with them. It is embedded
			inside an AfViewBarShell.
		AfViewBarShell : AfWnd - This window handles drawing the different groups (i.e.buttons).
			It contains an AfViewBar inside of it.
		AfCaptionBar : AfWnd - This window is used for the 'information bar' at the top of the
			MDI client window. It provides support for the caption text as well as multiple
			icons on the right side of the window that can provide some action when the user
			right-clicks on them.
		AfMdiClientWnd : AfWnd - This window embeds multiple AfClientWnd windows inside of it.
			It provides support for switching between child windows and communicating with them.
			It also provides methods to manipulate the caption bar embedded inside. Client
			windows are not created until the first time they are shown. Once they have been
			created, they stay around for the life of the application.
		AfClientWnd : AfWnd - This is an abstract base class that should be derived off
			of to create windows that show up in the MDI client window.
		AfSplitterClientWnd : AfClientWnd An abstract class for a client window that contains
			a splitter.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RECMAINWNDSUPPORTWNDS_H
#define RECMAINWNDSUPPORTWNDS_H 1

class RecMainWnd;
class AfMdiClientWnd;
class AfClientWnd;
class AfSplitterClientWnd;
class AfViewBarBase;
class AfListBar;
class AfRecListBar;
class AfTreeBar;
class AfViewBar;
class AfViewBarShell;
class AfCaptionBar;
class AfRecCaptionBar;
class AfDeSplitChild;

typedef GenSmartPtr<RecMainWnd> RecMainWndPtr;
typedef GenSmartPtr<AfMdiClientWnd> AfMdiClientWndPtr;
typedef GenSmartPtr<AfClientWnd> AfClientWndPtr;
typedef GenSmartPtr<AfSplitterClientWnd> AfSplitterClientWndPtr;
typedef GenSmartPtr<AfListBar> AfListBarPtr;
typedef GenSmartPtr<AfRecListBar> AfRecListBarPtr;
typedef GenSmartPtr<AfTreeBar> AfTreeBarPtr;
typedef GenSmartPtr<AfViewBar> AfViewBarPtr;
typedef GenSmartPtr<AfViewBarShell> AfViewBarShellPtr;
typedef GenSmartPtr<AfCaptionBar> AfCaptionBarPtr;
typedef GenSmartPtr<AfRecCaptionBar> AfRecCaptionBarPtr;

/*----------------------------------------------------------------------------------------------
	Base class to expose methods common to AfListBar and AfTreeBar

	@h3{Hungarian: vbb)
----------------------------------------------------------------------------------------------*/
class AfViewBarBase
{
public:
	const achar * GetName()
	{
		return m_staName.Chars();
	}
	virtual int GetSize() = 0;
	virtual int GetGeneralWhatsThisHelpId() { return 0; }
	virtual int GetItemWhatsThisHelpId(int iItem) { return 0; }
	virtual void GetSelection(Set<int> & sisel)
	{
		_CopySet(m_sisel, sisel);
	}
	virtual void SetSelection(Set<int> & sisel);
	virtual int GetContextSelection(void) { return 0; }

	virtual HWND GetHwnd()
	{
		AfWnd * pwnd = dynamic_cast<AfWnd *>(this);
		AssertPtr(pwnd);
		return pwnd->Hwnd();
	}

protected:
	StrApp m_staName;
	AfViewBarShellPtr m_qvwbrs;
	Set<int> m_sisel;

	// If your context menu should have a 'Configure' option in it, return the appropriate
	// CID for the current list. Return 0 for no Configure menu item.
	// The text and corresponding context help for your configure item should be in a
	// resource string organized for AfUtil::GetResourceStr.
	virtual int GetConfigureCid() { return 0; }

	void _CopySet(Set<int> & siselFrom, Set<int> & siselTo);
};


/*----------------------------------------------------------------------------------------------
	List bar (used in View bar). This class manages drawing the items in the list bar embedded
	within an AfViewBar. A flag on this class allows multiple selections. By default, only one
	selection is allowed. It supports two icon views: small and large.

	@h3{Hungarian: lstbr)
----------------------------------------------------------------------------------------------*/
class AfListBar : public AfWnd, public AfViewBarBase
{
	typedef AfWnd SuperClass;

public:
	AfListBar();

	virtual void Create(HWND hwndPar, int wid, const achar * pszName, HIMAGELIST himlLarge,
		HIMAGELIST himlSmall, bool fMultiple, AfViewBarShell * pvwbrs);
	virtual void AddItem(const achar * pszName, int imag);
	virtual void DeleteItem(const achar * pszName);

	// Size of icons in the display
	bool IsShowingLargeIcons()
	{
		return m_fShowLargeIcons;
	}
	virtual void ChangeIconSize(bool fLarge);
	int GetContextSelection(void)
	{
		return m_iContextSel;
	}
	int CalcSize()
	{
		return (m_fShowLargeIcons ? kdypLargeImages : kdypSmallImages) *
			::SendMessage(m_hwnd, LB_GETCOUNT, 0, 0) + 10;
	}
	int GetSize()
	{
		return ::SendMessage(m_hwnd, LB_GETCOUNT, 0, 0);
	}
	bool GetHelpStrFromPt(Point pt, ITsString ** pptss);
	virtual int GetGeneralWhatsThisHelpId();
	virtual int GetItemWhatsThisHelpId(int iItem);

	enum
	{
		kdypLargeImages = 70,
		kdypSmallImages = 30,
	};

protected:
	// Member variables
	HIMAGELIST m_himlLarge;
	HIMAGELIST m_himlSmall;
	bool m_fHilight; // true if the mouse is hovering over
	bool m_fLBDown; // true if the view btn should appear depressed
	HFONT m_hfont; // Font for captions in the bar
	bool m_fShowLargeIcons; // true if large icons, false if small
	bool m_fMultipleSel;
	int m_iCurSel;
	int m_iContextSel;
	bool m_fContextMenuVisible;

	int GetItemFromPoint(POINT pt);

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	// Message handlers
	virtual bool OnContextMenu(HWND hwnd, Point pt);
	virtual bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);
	virtual bool OnMeasureThisItem(MEASUREITEMSTRUCT * pmis);
	virtual void OnLButtonDown(UINT nFlags, POINT pt);
	virtual void OnLButtonUp(UINT nFlags, POINT pt);
	virtual void OnMouseMove(UINT nFlags, POINT pt);
	virtual void OnTimer(UINT nIDEvent);
	virtual bool OnPaint(HDC hdc);
	virtual void OnReleasePtr();

	// *****************************************************************************************
	//	Command functions.
	// *****************************************************************************************
	virtual bool CmdVbChangeSize(Cmd * pcmd);
	virtual bool CmsVbUpdateSize(CmdState & cms);
	virtual bool CmdHideVb(Cmd * pcmd);

	CMD_MAP_DEC(AfListBar);
};

/*----------------------------------------------------------------------------------------------
	Class used for record-based list bars.

	@h3{Hungarian: rlb}
----------------------------------------------------------------------------------------------*/
class AfRecListBar : public AfListBar
{
	typedef AfListBar SuperClass;

public:

	void Init(int itype)
	{
		m_itype = itype;
	}

protected:
	int m_itype;

	virtual int GetConfigureCid();
	virtual int GetGeneralWhatsThisHelpId();
	virtual int GetItemWhatsThisHelpId(int iItem);

	CMD_MAP_DEC(AfRecListBar);
};


/*----------------------------------------------------------------------------------------------
	Tree bar (used in View bar). This class manages drawing the items in the list bar embedded
	within an AfViewBar. A flag on this class allows multiple selections. By default, only one
	selection is allowed. It supports two icon views: small and large.

	@h3{Hungarian: lstbr)
----------------------------------------------------------------------------------------------*/
class AfTreeBar : public TssTreeView, public AfViewBarBase
{
	typedef TssTreeView SuperClass;

public:
	AfTreeBar();
	virtual void Create(HWND hwndPar, int wid, const achar * pszName, HIMAGELIST himl,
		DWORD dwStyle, AfViewBarShell * pvwbrs);
	virtual int GetGeneralWhatsThisHelpId();
	virtual int GetItemWhatsThisHelpId(int iItem);
	int GetSize()
		{ return GetCount(); }

protected:
	HIMAGELIST m_himl;

	virtual bool CmdHideVb(Cmd * pcmd);

	CMD_MAP_DEC(AfTreeBar);
};


/*----------------------------------------------------------------------------------------------
	Overlay listbar class for use in the view bar.
	This is needed to allow the overlay tool window to inform the list bar that
	it is being closed.

	@h3{Hungarian: olb)
----------------------------------------------------------------------------------------------*/
class AfOverlayListBar : public AfListBar
{
	typedef AfListBar SuperClass;

public:
	virtual void CreateOverlayTool(AfTagOverlayTool ** pprtot)
	{
		AssertPtr(pprtot);
		*pprtot = NewObj AfTagOverlayTool;
	}
	virtual void SetSelection(Set<int> & sisel);
	virtual void AddItem(const achar * pszName, int imag);

	virtual void HideOverlay(AfTagOverlayTool * ptot);

	virtual void HideAllOverlays();
	virtual void ShowAllOverlays();

	virtual int GetGeneralWhatsThisHelpId();
	virtual int GetItemWhatsThisHelpId(int iItem);

protected:
	Vector<Rect> m_vrc;
	HWND m_hwndOwner;
	AfLpInfoPtr m_qlpi;


};


/*----------------------------------------------------------------------------------------------
	View bar.

	@h3{Hungarian: vwbr)
----------------------------------------------------------------------------------------------*/
class AfViewBar : public AfWnd
{
	typedef AfWnd SuperClass;

public:
	AfViewBar();

	virtual void Create(HWND hwndPar, int style, int wid, AfViewBarShell * pvwbrs);
	virtual void AddList(const achar * pszName, HIMAGELIST himlLarge, HIMAGELIST himlSmall,
		bool fMultiple, AfListBar * plstbr = NULL);
	virtual int AddTree(const achar * pszName, HIMAGELIST himl, DWORD dwStyle,
		AfTreeBar * ptrbr = NULL);
	virtual void AddListItem(int ilist, const achar * pszName, int imag);
	virtual void DeleteListItem(int ilist, const achar * pszName);

	void GetSelection(int ilist, Set<int> & sisel)
	{
		Assert((uint)ilist < (uint)m_vpvbb.Size());
		m_vpvbb[ilist]->GetSelection(sisel);
	}
	int GetContextSelection(int ilist)
	{
		Assert((uint)ilist < (uint)m_vpvbb.Size());
		return m_vpvbb[ilist]->GetContextSelection();
	}
	void SetSelection(int ilist, Set<int> & sisel)
	{
		Assert((uint)ilist < (uint)m_vpvbb.Size());
		m_vpvbb[ilist]->SetSelection(sisel);
	}
	int GetListCount()
	{
		return m_vpvbb.Size();
	}
	AfViewBarBase * GetList(int ilist)
	{
		Assert((uint)ilist < (uint)m_vpvbb.Size());
		return m_vpvbb[ilist];
	}
	int GetCurrentList()
	{
		return m_ilistCur;
	}
	void SetCurrentList(int ilist);

	virtual bool OnSelChanged(AfViewBarBase * pvbb, Set<int> & siselOld, Set<int> & siselNew);
	virtual void Clear();

	// These two methods should only be called when you're sure ilist points to
	// an AfListBar window.
	void ChangeIconSize(int ilist, bool fLarge)
	{
		Assert((uint)ilist < (uint)m_vpvbb.Size());
		AfListBar * plstbr = dynamic_cast<AfListBar *>(m_vpvbb[ilist]);
		AssertPtr(plstbr);
		plstbr->ChangeIconSize(fLarge);
	}
	bool IsShowingLargeIcons(int ilist)
	{
		Assert((uint)ilist < (uint)m_vpvbb.Size());
		AfListBar * plstbr = dynamic_cast<AfListBar *>(m_vpvbb[ilist]);
		AssertPtr(plstbr);
		return plstbr->IsShowingLargeIcons();
	}

protected:
	AfViewBarShellPtr m_qvwbrs;
	Vector<AfViewBarBase *> m_vpvbb; // formerly m_vqlstbr
	Vector<int> m_viScrollPos;
	int m_ilistCur;

	virtual bool OnSize(int wst, int dxp, int dyp);
	virtual bool OnNotifyThis(int ctid, NMHDR * pnmh, long & lnRet);
	virtual void OnReleasePtr();
};


/*----------------------------------------------------------------------------------------------
	View bar shell.

	@h3{Hungarian: vwbrs)
----------------------------------------------------------------------------------------------*/
class AfViewBarShell : public AfWnd
{
	typedef AfWnd SuperClass;

public:
	AfViewBarShell();

	void Create(HWND hwndPar, int wid, AfViewBar * pvwbr = NULL);
	void AddList(const achar * pszName, HIMAGELIST himlLarge, HIMAGELIST himlSmall,
		bool fMultiple = false, AfListBar * plstbr = NULL)
	{
		m_qvwbr->AddList(pszName, himlLarge, himlSmall, fMultiple, plstbr);
	}
	int AddTree(const achar * pszName, HIMAGELIST himl, DWORD dwStyle,
		AfTreeBar * ptrbr = NULL)
	{
		return m_qvwbr->AddTree(pszName, himl, dwStyle, ptrbr);
	}
	void AddListItem(int ilist, const achar * pszName, int imag)
	{
		m_qvwbr->AddListItem(ilist, pszName, imag);
	}
	void DeleteListItem(int ilist, const achar * pszName)
	{
		m_qvwbr->DeleteListItem(ilist, pszName);
	}
	void GetSelection(int ilist, Set<int> & sisel)
	{
		m_qvwbr->GetSelection(ilist, sisel);
	}
	int GetContextSelection(int ilist)
	{
		return m_qvwbr->GetContextSelection(ilist);
	}
	void SetSelection(int ilist, Set<int> & sisel)
	{
		m_qvwbr->SetSelection(ilist, sisel);
	}
	void ChangeIconSize(int ilist, bool fLarge)
	{
		m_qvwbr->ChangeIconSize(ilist, fLarge);
	}
	bool IsShowingLargeIcons(int ilist)
	{
		return m_qvwbr->IsShowingLargeIcons(ilist);
	}
	int GetCurrentList()
	{
		return m_qvwbr->GetCurrentList();
	}
	void SetCurrentList(int ilist);

	AfViewBar * GetViewBar()
	{
		return m_qvwbr;
	}

	void ShowViewBar(bool fShow);
	bool GetHelpStrFromPt(Point pt, ITsString ** pptss);

protected:
	AfViewBarPtr m_qvwbr;
	int m_ibtn;
	bool m_fLbDown;

	enum
	{
		kdypButton = 20,
	};

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	virtual bool OnLButtonDown(uint grfmk, Point pt);
	virtual bool OnLButtonUp(uint grfmk, Point pt);
	virtual bool OnMouseMove(uint grfmk, Point pt);
	virtual bool OnSize(int wst, int dxp, int dyp);
	virtual bool OnPaint(HDC hdcDef);
	virtual void OnReleasePtr();
	int GetListIndexFromPoint(Point pt);
};

const int kdypStdCaptionBarHeight = 30;
const int kwidCaption = 1002;

/*----------------------------------------------------------------------------------------------
	Bitmask flags for passing to the AfCaptionBar Create function.

	@h3{Hungarian: fcbc)
----------------------------------------------------------------------------------------------*/
enum CaptionBarCreateFlags
{
	kfcbcEnhance3DTopCaptionBrdr = 1,
	kfcbcAllowCaptionToSizeParent = 2,
	kfcbcAllowCaptionToMoveParent = 4,
	kfcbcNotifyParentOnClick = 8,
};

/*----------------------------------------------------------------------------------------------
	Caption bar. A window that displays a caption and small icon.

	@h3{Hungarian: cpbr)
----------------------------------------------------------------------------------------------*/
class AfCaptionBar : public AfWnd
{
	typedef AfWnd SuperClass;

public:
	AfCaptionBar();
	~AfCaptionBar();

	virtual void Create(HWND hwndPar, int wid, HIMAGELIST himl, DWORD dwFlags = 0);

	int AddIcon(int imag);
	void SetCaptionText(Pcsz pszCaption);
	void SetIconImage(int iicon, int imag);

	int ButtonFromPoint(POINT pt, POINT * pptTopLeft = NULL);

	// This allows the implementor to override the default font which
	// is set in the caption bar's constructor.
	HFONT const SetCaptionFont(LOGFONT * plf);

	// This allows the implementor to override the default background color.
	void SetCaptionBackColor(COLORREF clr, bool fRedraw = false);

	// This allows the implementor to override the default border thickness.
	void SetCaptionBorderWidth(int dzsWidth, bool fRedraw = false);

protected:
	enum
	{
		kdzsBorderThickness = 4,
		kdzpIcon = 18,
		kdxpOffset = 8,
		kdxpMargin = 3,
		kdzpBorderWidthForSizingCursor = 5,
	};

	// Override this method to show a popup menu when an icon is pressed.
	virtual void ShowContextMenu(int ibtn, Point pt)
		{ }; // Do nothing.
	// Override this method to have a tooltip popup when the user hovers over an icon.
	virtual void GetIconName(int ibtn, StrApp & str)
		{ }; // Do nothing.

	void RecalcToolTip();

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnSize(int wst, int dxp, int dyp);
	virtual bool OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet);
	virtual bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);

	StrApp m_strCaption;
	HFONT m_hfont; // Font for the caption text.
	Vector<int> m_vimag; // When this is empty, we don't display any images.
	// This holds the tooltip text for each of the icons.
	// The strings in this vector are actually empty until the first time the tooltip is
	// required. Then it stores the tooltip text for later in case it needs it again.
	Vector<StrApp> m_vstaText;
	HIMAGELIST m_himl; // The image list from which we get our icons.
	HWND m_hwndToolTip;
	bool m_fNeedToRecalc;
	RecMainWnd * m_prmwMain;

	// True will paint a button face-colored line at the very top of the caption bar to
	// enhance it's top border's 3D appearance. This is usually done when the caption is
	// not tucked up against a dark border of it's parent or neighbor.
	bool m_fEnhanced3DTop;

	// True will show the NS arrow mouse cursor when moving over the caption's top border.
	// It will cause the caption's parent window to be informed when the user hold's down
	// the primary mouse button when hovering over the caption's top border. The idea is
	// the parent will use that information to begin capturing mouse messages to allow
	// the parent window to size itself as the user drags the mouse up and down. It's
	// important to note, it's the parent's responsibility to begin the capturing,
	// subsequent mouse message handling and release of capture.
	bool m_fCaptionSizesParent;

	// True will cause the caption's parent window to be informed when the user hold's
	// down the primary mouse button when hovering over the caption bar (except when
	// over the caption's top border and m_fCaptionSizesParent is true). The idea is
	// the parent window will move itself as the user drags the mouse. It's the parent's
	// responsibility to begin the capturing, subsequent mouse message handling and
	// release of capture.
	// ENHANCE DavidO: CaptionMovesParent is not implemented yet.
	bool m_fCaptionMovesParent;

	// True will cause the caption bar to notify the caption bar's parent when the
	// user clicks on the caption bar.
	bool m_fNotifyParentOnClick;

	// There may be times when the implementor wants to change the caption color (e.g.
	// to indicate focus). Therefore, it's stored in a variable and changed via
	// SetCaptionBackColor.
	COLORREF m_clrCaptionBackColor;

	// This is the border thickness, in pixels, of the caption bar (i.e. the area around the
	// caption text that's usually darker than the border). By default this value is set to
	// kdzsBorderThickness, but it can be changed via the SetCaptionBorderWidth access
	// function.
	int m_dzsBorderThickness;
};

/*----------------------------------------------------------------------------------------------
	Caption bar for recorde based apps. A window that displays a caption and small icon.

	@h3{Hungarian: rcpbr)
----------------------------------------------------------------------------------------------*/
class AfRecCaptionBar : public AfCaptionBar
{
	typedef AfCaptionBar SuperClass;

	virtual void Create(HWND hwndPar, int wid, HIMAGELIST himl, DWORD dwFlags = 0);

public:
	AfRecCaptionBar(RecMainWnd * prmwMain);

protected:
	virtual void GetIconName(int ibtn, StrApp & str);
	virtual bool GetHelpStrFromPt(Point pt, ITsString ** pptss);
};

/*----------------------------------------------------------------------------------------------
	MDI Client window that holds multiple AfClientWnd windows. It also controls the caption bar.

	@h3{Hungarian: mdic)
----------------------------------------------------------------------------------------------*/
class AfMdiClientWnd : public AfWnd
{
public:
	typedef AfWnd SuperClass;

	int AddChild(AfClientWnd * pafcw);
	void DelChildWnd(int wid);

	int GetChildCount()
	{
		return m_vqafcw.Size();
	}
	void RemoveAllChildren()
	{
		m_vqafcw.Clear();
	}

	int GetChildIndexFromWid(int wid);
	AfClientWnd * GetChildFromIndex(int iwnd);
	AfClientWnd * GetChildFromWid(int wid);
	void DelAllChildWnds();
	bool SetCurChildFromIndex(int iwnd);
	bool SetCurChildFromWid(int wid);
	AfClientWnd * GetCurChild(void)
	{
		return m_qafcwCur;
	}

	void SetCaptionBar(AfCaptionBar * pcpbr, DWORD dwFlags = 0);
	// NOTE: This doesn't return a reference count.
	AfCaptionBar * GetCaptionBar()
	{
		return m_qcpbr;
	}
	void ShowCaptionBar(bool fShow)
	{
		::ShowWindow(m_qcpbr->Hwnd(), fShow ? SW_SHOW : SW_HIDE);
	}
	bool IsCaptionBarVisible()
	{
		AssertObj(m_qcpbr);
		return ::IsWindowVisible(m_qcpbr->Hwnd());
	}

	int GetClientIndexLim()
	{
		return m_ClientIndexLim;
	}

	void SetClientIndexLim(int iClientIndexLim)
	{
		m_ClientIndexLim = iClientIndexLim;
	}

	virtual void Refresh();

protected:
	AfCaptionBarPtr m_qcpbr;
	Vector<AfClientWndPtr> m_vqafcw;
	AfClientWndPtr m_qafcwCur;
	RecMainWndPtr m_qrmw;
	int m_ClientIndexLim;

	virtual void PreCreateHwnd(CREATESTRUCT & cs);

	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnSize(int wst, int dxp, int dyp);
	virtual void OnReleasePtr();
};


/*----------------------------------------------------------------------------------------------
	Client window.

	@h3{Hungarian: afcw)
----------------------------------------------------------------------------------------------*/
class AfClientWnd : public AfWnd
{
	typedef AfWnd SuperClass;
public:

	virtual ~AfClientWnd() // Need for subclasses.
	{}

	virtual void Create(Pcsz pszView, int imag, int wid);

	// Called when framework is about to hide this client and show another.
	// If there is a possibility of failure, IsOkToChange() should be called first.
	virtual void PrepareToHide()
	{}

	// This can be used by the client to do something special, such as setting up menus for
	// subclasses of AfDeSplitChild, when the window is about to be displayed.
	virtual void PrepareToShow()
	{}

	virtual bool IsOkToChange(bool fChkReq = false)
		{return true;}

	Pcsz GetViewName()
		{ return m_strName.Chars(); }
	int GetImageIndex()
		{ return m_imag; }
	int GetWindowId()
		{ return m_wid; }
	void SetViewName(StrApp & strName)
		{ m_strName = strName; }
	// Any window that CAN be refreshed needs to provide validimplementation.
	virtual void RefreshDisplay()
		{ Assert(false); }

	// Called when the frame window is gaining or losing activation.
	// @param fActivating true if gaining activation, false if losing activation.
	virtual void OnPreActivate(bool fActivating)
	{}

	// Get/Put the split and scroll info for the window
	virtual void GetVwSpInfo(WndSettings * pwndSet)
	{}
	virtual void RestoreVwInfo(WndSettings wndSet)
	{}
	virtual bool Synchronize(SyncInfo & sync);
	virtual bool FullRefresh();
	virtual bool PreSynchronize(SyncInfo & sync)
	{
		return true;
	}

protected:
	StrApp m_strName;
	int m_imag;
	int m_wid;
};

class AfSplitterClientWnd;

/*----------------------------------------------------------------------------------------------
	This is a split frame subclass that exists solely to implement AfSplitterClientWnd.

	@h3{Hungarian: afscw}
----------------------------------------------------------------------------------------------*/
class AfSubClientSplitterWnd : public AfSplitFrame
{
	friend class AfSplitterClientWnd;
	typedef AfSplitFrame SuperClass;
public:
	virtual void CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew);
	virtual bool IsOkToChange(bool fChkReq = false);
protected:
	AfSplitterClientWnd * m_pafscwParent;
	// Protected constructor prevents anyone not a friend from making one.
	AfSubClientSplitterWnd(AfSplitterClientWnd * pafscw)
	{
		m_pafscwParent = pafscw;
	}
	void EnableHScroll()
		{m_fScrollHoriz = true;}

};
typedef GenSmartPtr<AfSubClientSplitterWnd> AfSubClientSplitterWndPtr;

/*----------------------------------------------------------------------------------------------
	Client window which embeds a splitter window. Subclasses must override CreateChild.
	This window is intended to replace the old AfClientWnd, which used to inherit from
	AfSplitFrame. It therefore implements many of the methods of AfSplitFrame, by delegating
	to the AfSplitFrame.

	@h3{Hungarian: afscw}
----------------------------------------------------------------------------------------------*/
class AfSplitterClientWnd : public AfClientWnd
{
	typedef AfClientWnd SuperClass;
public:
	// Subclasses must implement this! Note one difference between this and a regular
	// CreateChild method: use SplitterHwnd() to get the parent HWND for the new child.
	virtual void CreateChild(AfSplitChild * psplcCopy, AfSplitChild ** psplcNew) = 0;

	// These are all the methods that AfSplitFrame had when this class was separated from
	// AfClientWnd. They are implemented by delegating to the contained split frame.
	bool GetScrollInfo(AfSplitChild * psplc, int nBar, SCROLLINFO * psi)
		{ return m_qsplf->GetScrollInfo(psplc, nBar, psi); }
	int SetScrollInfo(AfSplitChild * psplc, int nBar, SCROLLINFO * psi, bool fRedraw)
		{ return m_qsplf->SetScrollInfo(psplc, nBar, psi, fRedraw); }
	void GetScrollOffsets(AfSplitChild * psplc, int * pdxd, int * pdyd)
		{ m_qsplf->GetScrollOffsets(psplc, pdxd, pdyd); }

	void SetCurrentPane(AfSplitChild * psplc)
		{ m_qsplf->SetCurrentPane(psplc); }
	HWND GetScrollBarFromPane(AfSplitChild * psplc)
		{ return m_qsplf->GetScrollBarFromPane(psplc);}

	AfSplitChild * GetPane(int iPane)
		{ return m_qsplf->GetPane(iPane); }
	AfSplitChild * CurrentPane()
		{ return m_qsplf->CurrentPane(); }
	void SplitWindow(int ypSplit)
		{ m_qsplf->SplitWindow(ypSplit); }
	void UnsplitWindow(bool fCloseBottom)
		{ m_qsplf->UnsplitWindow(fCloseBottom); }
	bool CanGetPane()
		{ return (m_qsplf.Ptr() != NULL); }
	virtual void PostAttach(void);
	virtual bool OnSize(int wst, int dxp, int dyp);
	HWND SplitterHwnd()
	{
		return m_qsplf->Hwnd();
	}
	int TopPaneHeight()
		{ return m_qsplf->TopPaneHeight(); }
	void EnableHScroll()
	{
		m_fScrollHoriz = true;
		if (m_qsplf)
			m_qsplf->EnableHScroll();
	}
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void OnPreActivate(bool fActivating);

	// the split and scroll info for the window.
	virtual void GetVwSpInfo(WndSettings * pwndSet);
	virtual void RestoreVwInfo(WndSettings wndSet);
	virtual bool Synchronize(SyncInfo & sync);

protected:
	AfSubClientSplitterWndPtr m_qsplf;
	bool m_fScrollHoriz;
};


#endif // !RECMAINWNDSUPPORTWNDS_H
