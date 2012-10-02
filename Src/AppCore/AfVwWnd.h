/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfVwWnd.h
Responsibility: John Thomson
Last reviewed: never

Description:
	The most interesting class in this file is AfVwRootSite, the class that does most of the
	interesting work relating to hosting a FieldWorks View represented as an IVwRootBox.
	It provides an implmentation of IVwRootSite, passes interesting events to the root box,
	and handles various common menu commands and toolbar functions.

	Because this functionality may be needed by a variety of window classes, it is implemented
	as a mixin, designed for multiple inheritance in a hierarchy where some subclass of AfWnd
	is the main superclass.

	A subclass, AfVwScrollWndBase, provides extended mixin functions useful when the view will
	have an associated scroll bar.

	The file also contains several concrete view-hosting window classes:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFVWWND_INCLUDED
#define AFVWWND_INCLUDED 1

extern int GetCodePageForLangId(int nLangID);

typedef ComVector<ITsTextProps> TtpVec;
typedef ComVector<IVwPropertyStore> VwPropsVec;
class AfVwRootSite;
class AfInactiveRootSite;
typedef GenSmartPtr<AfVwRootSite> AfVwRootSitePtr;
typedef GenSmartPtr<AfInactiveRootSite> AfInactiveRootSitePtr;

class AfVwWnd;
class AfVwSplitChild;
class AfVwRootSite;
class AfVwRecSplitChild;

typedef GenSmartPtr<AfVwSplitChild> AfVwSplitChildPtr;
typedef GenSmartPtr<AfVwRecSplitChild> AfVwRecSplitChildPtr;
typedef Vector<VwSelLevInfo> VecSelInfo;

class VwCustomVc;
typedef GenSmartPtr<VwCustomVc> VwCustomVcPtr;

/*----------------------------------------------------------------------------------------------
	Class to represent the logical info about a selection, typically for restoring position
	after turning on tags or similar.
	Hungarian: avsi
----------------------------------------------------------------------------------------------*/
class AfVwSelInfo
{
public:
	int m_ihvoRoot;
	PropTag m_tagTextProp;
	int m_cpropPrevious; // or -1 to indicate load failed.
	int m_ws;
	int m_ichAnchor;
	int m_ichEnd;
	int m_ihvoEnd;
	VecSelInfo m_vvsli;
	// Relevant for insertion points:
	ITsTextPropsPtr m_qttp;
	ComBool m_fAssocPrev;
	// Relevant for ranges--this isn't needed because we figure it out in the process
	// of restoring the selecion:
	//ComBool m_fEndBeforeAnchor;
	// Primary rectangle where selection was visible when loaded; current only set
	// by LoadTopLeft:
	Rect m_rdPrimary;
	// For when the selection spans multiple root objects:
	PropTag m_tagTextPropEnd;
	VecSelInfo m_vvsliEnd;
	int m_cpropPreviousEnd;

	void Load(IVwRootBox * prootb, IVwSelection * psel = NULL);
	bool LoadVisible(IVwRootBox * prootb);
	bool Set(IVwRootBox * prootb, bool fInstall, IVwSelection ** ppsel);
	bool MakeVisible(IVwRootBox * prootb, bool fInstall,
		IVwSelection ** ppsel);
	bool MakeBest(IVwRootBox * prootb, bool fInstall, IVwSelection ** ppsel);
	bool LoadTopLeft(AfVwRootSite * pavrs);
	void RestorePosition(AfVwRootSite * pavrs);
};

/*----------------------------------------------------------------------------------------------
	Class to represent the position of a selection at the top left of a root site,
	so it can be restored, typically after some operation in the other pane.
	Hungarian: spi
----------------------------------------------------------------------------------------------*/
class SelPositionInfo
{
public:
	SelPositionInfo(AfVwRootSite * pavrs);
	void Restore();

	AfVwRootSite * m_pavrs;	// The root site we are working with.
	Rect m_rdPrimaryOld;	// The position of the top-left selection to be restored.
	IVwSelectionPtr m_qsel;	// the selection.
};

/*----------------------------------------------------------------------------------------------
	A class to manage initializing and uninitializing the graphics object. Create an instance
	where you need m_qvg to be ready to use
	Hungarian: avrs
----------------------------------------------------------------------------------------------*/
class HoldGraphicsBase
{
protected:
	HoldGraphicsBase() {}
	void InitWithOrigHdc();
	virtual ~HoldGraphicsBase() {}

public:
	AfInactiveRootSite * m_pvrs;
	HDC m_hdcOld;
	Rect m_rcSrcRoot;
	Rect m_rcDstRoot;
	HDC m_hdc; // what the user passed
	IVwGraphicsPtr m_qvg;
};

/*----------------------------------------------------------------------------------------------
	A class to manage initializing and uninitializing the graphics object. Create an instance
	where you need m_qvg to be ready to use. Use this variant if you want it to use information
	from the current pane, even if it is not the one you pass to the constructor.
	Hungarian: avrs
----------------------------------------------------------------------------------------------*/
class HoldGraphics : public HoldGraphicsBase
{
public:
	HoldGraphics(AfInactiveRootSite * pvrs, HDC hdc = 0);
	virtual ~HoldGraphics();
};

/*----------------------------------------------------------------------------------------------
	A class to manage initializing and uninitializing the graphics object. Create an instance
	where you need m_qvg to be ready to use. This differs from HoldGraphics in that it uses
	GetGraphicsRaw, and hence, holds a graphics object that is definitely for the root site
	it was passed, neve for a complementary active pane that contains the same root.
	Hungarian: avrs
----------------------------------------------------------------------------------------------*/
class HoldGraphicsRaw : public HoldGraphicsBase
{
public:
	HoldGraphicsRaw(AfVwRootSite * pvrs, HDC hdc = 0);
	virtual ~HoldGraphicsRaw();
};


ATTACH_GUID_TO_CLASS(class, 66832BB8-E47D-43d9-9D53-57C0F55A385C, AfVwRootSite);
// Trick GUID for getting the actual implementation of the AfVwRootSite object from QI.
#define CLSID_AfVwRootSite __uuidof(AfVwRootSite)

/*----------------------------------------------------------------------------------------------
	An Inactive root site holds all the behavior that is common to root sites that are
	inactive (don't have the focus). See AfDeFeVw for another subclass that is never active.

	At this point the main shared thing is OnMouseMove. The things it calls are declared
	pure virtual. Investigation may uncover other methods that are really the same or from
	which common parts could be refactored.
	Hungarian: irs
----------------------------------------------------------------------------------------------*/
class AfInactiveRootSite : public IVwRootSite
{
	friend AfVwWnd;
	friend HoldGraphicsBase;
	friend HoldGraphics;
	friend HoldGraphicsRaw;
	friend class OrientationManager;
	friend class VerticalOrientationManager;
public:
	virtual bool OnMouseMove(uint grfmk, int xp, int yp);
	virtual AfWnd * Window() = 0;
	bool GetExternalLinkSel(bool & fFoundLinkStyle, IVwSelection ** ppvwsel,
		StrAppBuf & strbFile, POINT * ppt = NULL);
protected:
	AfInactiveRootSite();
	virtual ~AfInactiveRootSite() {}
	IVwRootBoxPtr m_qrootb;

	// This is used for the External Link tooltip.
	HWND m_hwndExtLinkTool;

	// This is used for the Overlay tag tooltip.
	HWND m_hwndOverlayTool;

#ifdef BASELINE
	VwGraphicsPtr m_qvg;
#else
	IVwGraphicsWin32Ptr m_qvg;
#endif

	int m_cactInitGraphics; // count of number of times we have called InitGraphics
							// more than we have called ReleaseGraphics

	virtual void PixelToView(int & xp, int & yp);
	virtual void InitGraphics() = 0;
	virtual void GetCoordRects(IVwGraphics * pvg, RECT * prcSrcRoot, RECT * prcDstRoot) = 0;
	virtual void CallMouseMoveDrag(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot) = 0;
	virtual void UninitGraphics() = 0;
	void GetGraphicsRaw(IVwGraphics ** ppvg, RECT * prcSrcRoot = NULL, RECT * prcDstRoot = NULL);
	void ReleaseGraphicsRaw(IVwGraphics * pvg);
};

/*----------------------------------------------------------------------------------------------
	This is the main class of root site that is commonly used.
	Hungarian: avrs
----------------------------------------------------------------------------------------------*/

class AfVwRootSite : public AfInactiveRootSite
{
	friend class OrientationManager;
	friend class VerticalOrientationManager;
public:
	friend AfVwWnd;
	friend HoldGraphicsBase;
	friend HoldGraphics;
	friend HoldGraphicsRaw;

	// Format commands:
	enum
	{
		knFormatNormal = -1, // Normal dialog.
		knFormatJustify = -2, // Justification: left, center, or right.
		knFormatIndent = -3, // Increase or decrease indentation.
		knFormatBorder = -4, // Border dialog
		knFormatStyle = -5, // Paragraph named style
		knFormatBulNum = -6, // Bullets & Numbers
		knFormatBulletList = -7, // Bullet toolbar button
		knFormatNumListButton = -8, // Numbered list button
		knFormatBorderButton = -9, // Border button
	};

	AfVwRootSite(AfWnd * pwnd, bool fScrollVert = false, bool fScrollHoriz = false);
	~AfVwRootSite();

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);

	// IVwRootSite methods.
	STDMETHOD(InvalidateRect)(IVwRootBox * pRoot, int twLeft, int twTop, int twWidth,
		int twHeight);
	STDMETHOD(GetGraphics)(IVwRootBox * pRoot, IVwGraphics ** ppvg,
		RECT * prcSrcRoot, RECT * prcDstRoot);
	STDMETHOD(get_LayoutGraphics)(IVwRootBox * pRoot, IVwGraphics ** ppvg);
	STDMETHOD(get_ScreenGraphics)(IVwRootBox * prootb, IVwGraphics ** ppvg);
	STDMETHOD(GetTransformAtDst)(IVwRootBox * pRoot,  POINT pt,
		RECT * prcSrcRoot, RECT * prcDstRoot);
	STDMETHOD(GetTransformAtSrc)(IVwRootBox * pRoot,  POINT pt,
		RECT * prcSrcRoot, RECT * prcDstRoot);
	STDMETHOD(ReleaseGraphics)(IVwRootBox * prootb, IVwGraphics * pvg);
	STDMETHOD(GetAvailWidth)(IVwRootBox * prootb, int * ptwWidth);
	STDMETHOD(RootBoxSizeChanged)(IVwRootBox * prootb);
	STDMETHOD(AdjustScrollRange)(IVwRootBox * prootb, int dxdSize, int dxdPosition,
		int dydSize, int dydPosition, ComBool * pfForcedScroll);
	STDMETHOD(AdjustScrollRange1)(int dxdSize, int dxdPosition, int dydSize, int dydPosition,
		ComBool * pfForcedScroll);
	STDMETHOD(DoUpdates)(IVwRootBox * prootb);
	STDMETHOD(SelectionChanged)(IVwRootBox * prootb, IVwSelection * pvwselNew);
	STDMETHOD(OverlayChanged)(IVwRootBox * prootb, IVwOverlay * pvo);
	STDMETHOD(get_SemiTagging)(IVwRootBox * prootb, ComBool * pf);
	STDMETHOD(ScreenToClient)(IVwRootBox * prootb, POINT * ppnt);
	STDMETHOD(ClientToScreen)(IVwRootBox * prootb, POINT * ppnt);
	STDMETHOD(GetAndClearPendingWs)(IVwRootBox * prootb, int * pws);
	STDMETHOD(IsOkToMakeLazy)(IVwRootBox * prootb, int ydTop, int ydBottom, ComBool * pfOK);
	STDMETHOD(OnProblemDeletion)(IVwSelection * psel, VwDelProbType dpt,
		VwDelProbResponse * pdpr);
	STDMETHOD(ScrollSelectionIntoView)(IVwSelection * psel, VwScrollSelOpts ssoFlag);
	STDMETHOD(OnInsertDiffParas)(IVwRootBox * prootb, ITsTextProps * pttpDest, int cPara,
		ITsTextProps ** prgpttpSrc, ITsString ** prgptssSrc,  ITsString * ptssTrailing,
		VwInsertDiffParaResponse * pidpr);
	STDMETHOD(get_TextRepOfObj)(GUID * pguid, BSTR * pbstrRep)
	{
		*pbstrRep = NULL;
		return S_OK;
	}
	STDMETHOD(get_MakeObjFromText)(BSTR bstrText, IVwSelection * pselDst,
		int * podt, GUID * pGuid)
	{
		*pGuid = GUID_NULL;
		return S_OK;
	}
	STDMETHOD(get_RootBox)(IVwRootBox ** pprootb);
	STDMETHOD(get_Hwnd)(DWORD * phwnd);

	// Override this method in your subclass.
	// It should make a root box and initialize it with appropriate data and
	// view constructor, etc.
	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb) = 0;
	// Override in classes that do something special with printing.
	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb, bool fPrinting)
	{
		MakeRoot(pvg, pwsf, pprootb);
	}
	bool EnsureRootBox();
	virtual bool IsSelectionVisible(IVwSelection * psel = NULL);
	virtual void HandleSelectionChange(IVwSelection * pvwsel);
	virtual void HandleKeyboardChange(IVwSelection * pvwsel, int nLangID);
	void ActivateKeyboard(HKL hkl, UINT nFlags);
	void SetKeyboardForSelection(IVwSelection * pvwsel);
	virtual bool UpdateParaDirection(IVwSelection * pvwsel);

	void SetCodePageForLangId(int nLangId);

	virtual void MakeSelectionVisible1(IVwSelection * psel = NULL) = 0;
	virtual void ScrollSelectionNearTop1(IVwSelection * psel = NULL) = 0;

	// Does this kind of view support printing just the selection?
	virtual bool CanPrintOnlySelection()
	{
		return false;
	}
	virtual void CacheSelectedObjects()
	{
		Assert(false);	// should only be called when CanPrintOnlySelection is true
	}
	virtual void GetSelectedObjects(Vector<HVO> & vhvo)
	{
		Assert(false);
	}

	// Is the overall direction of the window right-to-left?
	virtual bool OuterRightToLeft()
	{
		return false;
	}

	// Get the border buttons that are pressed, based upon the properites of the current
	// paragraph. Return false if no selection, true if successful.
	bool GetFmtBdrPressed(bool * pavrs);


	virtual AfWnd * Window()
	{
		return m_pwndSubclass;
	}

	// Override to add extra menu items to the default context menu.
	virtual void AddExtraContextMenuItems(HMENU hmenuPopup) {}

	void ModifyOverlay(bool fApplyTag, IVwOverlay * pvo, int itag);

	//	Possibly these should be static methods:
	bool WsWithUiName(StrApp strUiName, int * pws);
	void UiNameOfWs(int ws, StrApp * pstr);

	/*******************************************************************************************
		Command handlers.
	*******************************************************************************************/
	bool CmdFmtFnt1(Cmd * pcmd);
	bool CmdFmtWrtSys1(Cmd * pcmd);
	bool CmdFmtPara1(Cmd * pcmd);
	virtual bool CmsFmtPara1(CmdState & cms);
	bool CmdFmtStyles1(Cmd * pcmd);
	virtual bool CmsFmtStyles1(CmdState & cms);
	bool CmdApplyNormalStyle1(Cmd * pcmd);
	bool CmdFmtBulNum1(Cmd * pcmd);
	bool CmsFmtBulNum1(CmdState & cms);
	bool CmdFmtBdr1(Cmd * pcmd);
	bool CmsFmtBdr1(CmdState & cms);
	bool CmsFmtX(CmdState & cms);
	bool CmdCharFmt1(Cmd * pcmd);
	bool CmsCharFmt1(CmdState & cms);
	bool CmdInsertPic1(Cmd * pcmd);
	bool CmdFilePrint(Cmd * pcmd);
	bool CmdFilePrint1(Cmd * pcmd);
	bool CmdEditFind1(Cmd * pcmd = NULL);
	bool CmdEditSrchQuick1(Cmd * pcmd = NULL);
	bool CmsEditSrchQuick1(CmdState & cms);
	bool CmdEditNextMatch1(Cmd * pcmd);
	bool CmdEditRepl1(Cmd * pcmd);
	bool CmdViewRefresh1(Cmd * pcmd);

	virtual bool CmdEditCut1(Cmd * pcmd);
	virtual bool CmdEditCopy1(Cmd * pcmd);
	virtual bool CmdEditPaste1(Cmd * pcmd);
	bool CmdEditDel1(Cmd * pcmd);
	bool CmdEditSelAll1(Cmd * pcmd);
	virtual bool CmsEditCut1(CmdState & cms);
	virtual bool CmsEditCopy1(CmdState & cms);
	virtual bool CmsEditPaste1(CmdState & cms);
	virtual bool CmsEditDel1(CmdState & cms);
	bool CmsEditSelAll1(CmdState & cms);

	virtual bool ApplyFormatting(int cid, HWND hwnd);
	virtual int GetFormatting(int cid, StrApp & strValue);
	void UpdateDefaultFontNames(void * pv);
	int AfVwRootSite::GetParaFormatting(int cid, StrApp & strValue);
	virtual bool IsCurrentPane();
	virtual void SetFocusToRootSite();
	void Activate(VwSelectionState vss);
	virtual void SelectAll();
	virtual bool NextMatch(bool fForward = true, bool fDialogIfEmpty = true,
		bool fScrollAndMsg = true, bool fReplace = false, HWND hwndFrom = NULL,
		IVwSearchKiller * pxserkl = NULL);
	void ApplyWritingSystem(TtpVec & vqttp, int wsNew,  IVwSelection * pvwsel);

	static bool GetCharacterProps(IVwRootBox * prootb, IVwSelection ** ppvwsel,
		TtpVec & vqttp, VwPropsVec & vqvps);
	bool GetCharacterProps(IVwSelection ** ppvwsel, TtpVec & vqttp,
		VwPropsVec & vqvps);
	// Subclasses should override if they can scroll.
	virtual void ScrollDown(int dy) {};

	virtual bool OpenFormatStylesDialog(HWND hwnd, bool fCanDoRtl, bool fOuterRtl,
		IVwStylesheet * past, TtpVec & vqttpPara, TtpVec & vqttpChar, bool fCanFormatChar,
		StrUni * pstuStyleName, bool & fStylesChanged, bool & fApply, bool & fReloadDb);

	// The following method is public so it can be used by the Find/Replace dialog.
	void RemoveCharFormatting(IVwSelection * pvwsel, TtpVec & vqttp,
		BSTR bstrStyle = NULL);

	bool GetOverlayTagSel(OLECHAR * prgchGuid, IVwSelection ** ppvwsel, POINT * ppt = NULL);

	virtual void OnChar(UINT nChar, UINT nRepCnt, UINT nFlags);

	void SetupUndoSelection(ISilDataAccess * psda, bool fForUndo);

	bool RemoveRedundantHardFormatting(ISilDataAccess * psda,
		IVwPropertyStore * pvpsSoft, ITsTextProps * pttpHard, bool fParaStyle, HVO hvoPara,
		ITsTextProps ** ppttpRet);

	// Overridden by windows like Rn/CleDocSplitChild that allow the selection to extend
	// over multiple fields.
	virtual bool SelectionInOneField()
	{
		return true;
	}

	int GetSelectionWs();

protected:
	// A pointer, set in the constructor, to the window class which is a subclass of this.
	AfWnd * m_pwndSubclass;

	virtual void InitGraphics();
	virtual void UninitGraphics();
	bool Layout();

	bool m_fVScrollEnabled;
	bool m_fHScrollEnabled;
	// Set when we issue a keyman keyboard change request, so we can ignore the
	// resulting notification.
	bool m_fSelectLangPending;
	// Keeps track of the last language we tried to set. If this changes,
	// even if the keyboard didn't, we update the language bar.
	LANGID m_langIDCurrent;


	// The width returned by LayoutWidth() when the root box was last laid out,
	// or a large negative number if it has never been successfully laid out.
	int m_dxdLayoutWidth;

	// The following are set when there was a switch in the system keyboard,
	// and at that point there was no insertion point on which to change the writing system.
	// We store the information here until either they get an IP and start typing
	// (the writing system is set using these) or the selection changes (throw the
	// informtion away and reset the keyboard).
	int m_wsPending;
	int m_nCodePage;		// codepage to use when receiving 8-bit keyboard data
	HKL m_hklCurr;			// Current keyboard.
	StrUni m_stuActiveKeymanKbd; // Current keyman setting.

	bool m_fCanDoRtl;

	int m_dyHeader;		// height of an optional fixed header at the top of the client window.

	static Vector<StrUni> s_vstuDrawErrMsgs;	// list of drawing err messages that have been
												// shown to the user

	bool m_f1DefaultFont;

	// This variable is used to keep a more precise idea than ::GetFocus() can give us
	// of whether this window has focus. The issue is that during OnKillFocus(), we
	// switch the keyboard to the default UI language. That can produce a WM_INPUTLANGCHANGED,
	// but we must NOT process it, even though (during OnKillFocus) we are be the focus window.
	// We don't want THIS window set to use a different input language, we just want any dialog
	// we may be switching to do so.
	bool m_fIAmFocussed;

	// This variable prevents painting while printing, a potential re-entrant use of
	// UniscribeSegment.  See DN-841.
	static bool s_fBusyPrinting;
	static Vector<AfVwRootSite *> s_vrsDraw;

	void SetOneDefaultFont(bool f = true)
	{
		m_f1DefaultFont = f;
	}
	virtual bool OneDefaultFont()
	{
		return m_f1DefaultFont;
	}

	// Check for presence of proper paragraph properties. Return false if neither
	// selection nor paragraph property. Otherwise return true.
	bool IsParagraphProps(IVwSelection ** ppvwsel, HVO & hvoText, int & tagText,
		VwPropsVec &vqvps, int & ivhoAnchor, int & ivhoEnd);

	// Get the view selection and paragraph properties. Return false if there is neither a
	// selection nor a paragraph property. Otherwise return true.
	bool GetParagraphProps(IVwSelection ** pqvwsel, HVO & hvoText, int & tagText,
		VwPropsVec &vqvps, int & ihvoFirst, int & ihvoLast, ISilDataAccess ** ppsda,
		TtpVec & vpttp);

	bool CmdApplyNormalStyle(Cmd * pcmd);

	virtual bool IsSelectionVisible1(IVwSelection * psel = NULL) = 0;

	bool GetWsOfSelection(IVwSelection * pvwsel, int * pwsSel);
	virtual void CloseRootBox();
	virtual int LayoutWidth();
	virtual void GetCoordRects(IVwGraphics * pvg, RECT * prcSrcRoot, RECT * prcDstRoot);
	// Allow two pixels to keep the data clear of the window border.
	virtual int GetHorizMargin()
		{ return 2; }
	void UpdateScrollRange();
	virtual void Draw(HDC hdc, const Rect & rcpClip);

	void CollectTypedInput(wchar chwFirst, wchar * prgchwBuffer, int cchMax,
		int * pcchBackspace, int * pcchDelForward);

	void Invalidate(bool fErase = true)
	{
		::InvalidateRect(m_pwndSubclass->Hwnd(), NULL, fErase);
	}

	// The name distinguishes from the one defined on a sister class.
	virtual int SetRootSiteScrollInfo(int nBar, SCROLLINFO * psi, bool fRedraw)
	{
		return m_pwndSubclass->SetScrollInfo(nBar, psi, fRedraw);
	}

	// Window message handling.
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void OnUniChar(UINT nChar, UINT nReserved, UINT nFlags);
	virtual int OnCreate(CREATESTRUCT * pcs);
	virtual bool OnLButtonDown(uint grfmk, int xp, int yp);
	virtual bool OnLButtonDblClk(uint grfmk, int xp, int yp);
	virtual bool OnLButtonUp(uint grfmk, int xp, int yp);
	virtual bool OnRButtonDown(uint grfmk, int xp, int yp);
	virtual void OnSysChar(UINT nChar, UINT nRepCnt, UINT nFlags);
	virtual bool OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);
	virtual void OnTimer(UINT nIDEvent);
	virtual bool OnSetFocus(HWND hwndOld, bool fTbControl = false);
	virtual bool OnKillFocus(HWND hwndNew);
	virtual void GetScrollOffsets(int * pdxd, int * pdyd) = 0;
	virtual bool OnInputLangChangeRequest(UINT wpFlags, UINT lpHKL);
	virtual void OnInputLangChange(UINT wpFlags, UINT lpHKL);
	virtual bool OnContextMenu(HWND hwnd, Point pt);
	virtual void OnKeymanKeyboardChange(UINT wpFlags, UINT lpHKL);
	bool GetWsListCurrentFirst(IVwSelection * pvwsel, Vector<int> & vws,
		ILgWritingSystemFactory ** ppwsf);


	void OnReleasePtr();
	virtual void RemoveRootRegistration();
	virtual void SwitchFocusHere();

	// called from OnChar and OnUniChar:
	virtual void OnCharAux(UINT nChar, UINT nFlags, StrUni strInput,
		int cchBackspace, int cchDelForward, VwShiftStatus ss);

	// Test functions:
#ifdef BASELINE
	virtual void CallOnTyping(VwGraphicsPtr qvg, BSTR bstr,
		int cchBackspace, int cchDelForward, OLECHAR chFirst,
		RECT rcSrcRoot, RECT rcDstRoot);
#else
	virtual void CallOnTyping(IVwGraphicsWin32Ptr qvg, BSTR bstr,
		int cchBackspace, int cchDelForward, OLECHAR chFirst,
		RECT rcSrcRoot, RECT rcDstRoot);
#endif
	virtual void CallMouseDown(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot);
	virtual void CallMouseDownExtended(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot);
	virtual void CallMouseUp(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot);
	virtual void CallMouseMoveDrag(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot);
	virtual void CallOnSysChar(int chw);
	virtual bool CallOnExtendedKey(int chw, VwShiftStatus ss);
	// If this pane is one of two panes sharing the same root box in a split window, answer the other one.
	virtual AfVwRootSite * OtherPane()
	{
		return NULL;
	}
	bool CheckBorderButton(TtpVec &vqttp);
	bool HandleBorderButton(TtpVec &vqttp);

	virtual int ComplexKeyBehavior(int chw, VwShiftStatus ss)
	{
		return 1; // Return 0 for visual (physical) arrow key behavior, 1 for logical.
		// (Note that WorldPad currently overrides.)
	}

	// Called by the menu enabling methods as well as the commands themselves:
	virtual bool CanCut();
	virtual bool CanPaste();

	void BeginUndoTask(int cid, ISilDataAccess ** ppsda);
	void EndUndoTask(ISilDataAccess * psda);
	int UndoRedoLabelRes(int cid);

	bool FormatParas(int format, bool fCanDoRtl, bool fOuterRtl,
		int var1, BSTR bstrNewVal = NULL);

	void GetDefaultFontNames(TtpVec & vqttp, VwPropsVec & vqvps, StrUni & stuDefSerif,
		StrUni & stuDefSans, StrUni & stuDefMono, StrUni & stuDefBodyFont);
	void StripNameFromDefaultFont(StrUni & stuDefault);

	bool ApplyFormattingAux(int cid, HWND hwnd);
	bool DoFindReplace(bool fReplace);
	virtual bool DoViewRefresh();
	virtual void GetFindReplMsgs(int * pstidNoMatches, int * pstidReplaceN);

	virtual void AdjustForOverlays(IVwOverlay * pvo)
	{
	}
	virtual bool DoesSemiTagging()
	{
		return false;
	}
	virtual bool AllowFontFeatures() // in the Format-Font dialog
	{
		return true;
	}

	void GiveDrawErrMsg(bool fFatal);
	void GetLgWritingSystemFactory(ILgWritingSystemFactory ** ppwsf);
	bool OkToConvert(int ydTop, int ydBottom);
	void SetKeyboardForUI();
	void SetKeyboardForWs(int ws);
	virtual RECT RotateRectDstToPaint(RECT rect);
	virtual bool IsVertical();
};

class OrientationManager;

/*----------------------------------------------------------------------------------------------
	This class manages common functionality for view windows that scroll.
	Hungarian: avswb.
----------------------------------------------------------------------------------------------*/
class AfVwScrollWndBase : public AfVwRootSite
{
	friend AfVwRootSite;
	friend OrientationManager;

	typedef AfVwRootSite SuperClass;

public:
	AfVwScrollWndBase(AfWnd * pwnd, bool fScrollHoriz, bool fVerticalOrientation = false);
	virtual ~AfVwScrollWndBase();

	// Window message handling.
	virtual bool OnVScroll(int wst, int yp, HWND hwndSbar);
	virtual bool OnHScroll(int wst, int yp, HWND hwndSbar);
	virtual bool OnPaint(HDC hdc);
	virtual bool OnKillFocus(HWND hwndNew);
	virtual bool OnSize(int nId, int dxp, int dyp);
	virtual bool OnMouseMove(uint grfmk, int xp, int yp);

	virtual void MakeSelectionVisible1(IVwSelection * psel = NULL);
	virtual bool IsSelectionVisible1(IVwSelection * psel = NULL);
	virtual void ScrollSelectionNearTop1(IVwSelection * psel = NULL);
	virtual void ScrollToTop();
	virtual void ScrollToEnd();
	STDMETHOD(AdjustScrollRange1)(int dxdSize, int dxdPosition, int dydSize, int dydPosition,
		ComBool * pfForcedScroll);
	virtual void MakeSelVisAfterResize(bool fSelVis);

	// Set the width last used to lay out the root box. This is useful when two objects are
	// sharing the root box, such as an AfDeFeVw and an active view window.
	void SetLastLayoutWidth(int dxp)
	{
		m_dxdLayoutWidth = dxp;
	}

	virtual int LayoutWidth();
	virtual void GetScrollOffsets(int * pdxd, int * pdyd);
	virtual void ScrollDown(int dy);
	virtual void ScrollBy(int dxdOffset, int dydOffset) = 0;
	virtual void GetScrollRect(int dx, int dy, Rect & rc)
	{
		m_pwndSubclass->GetClientRect(rc);
	}
	virtual COLORREF GetWindowColor()
	{
		return ::GetSysColor(COLOR_WINDOW);
	}

	virtual void PixelToView(int & xp, int & yp);

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	OrientationManager * m_pomgr;
	virtual OrientationManager * CreateOrientationManager(bool fVerticalOrientation);
	virtual RECT RotateRectDstToPaint(RECT rect);
	virtual void GetCoordRects(IVwGraphics * pvg, RECT * prcSrcRoot, RECT * prcDstRoot);
	virtual bool IsVertical();
};


/*----------------------------------------------------------------------------------------------
	A view which scrolls as one half of a splitter window (or the whole of it, if not currently
	split). The two halves share the same root box, which improves efficiency but introduces
	some complications in working out which tasks should be done only in the active pane (like
	drawing the selection) and which in both (like invalidating outdated boxes).
	Hungarian: avsc.
----------------------------------------------------------------------------------------------*/
class AfVwSplitChild : public AfSplitChild, public AfVwScrollWndBase
{
	typedef AfSplitChild SuperClass;

protected:
	typedef AfVwScrollWndBase ScrollSuperClass;
	bool CmsHaveRecord1(CmdState & cms);

public:
	AfVwSplitChild(bool fScrollHoriz = false, bool fVerticalOrientation = false) : AfVwScrollWndBase(this, fScrollHoriz, fVerticalOrientation)
	{
	}

	// We have to be tricky here. There is an inherited reference count from
	// AfClientWnd. We want to go on existing as long as there are pointers
	// to either interface.
	STDMETHOD_(ULONG, AddRef)(void)
	{
		AfWnd::AddRef();
		return m_cref;
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		// AfDeFieldEditor::Release might delete this object, so we need to get the reference
		// count before calling it and subtract one.
		long cref = m_cref;
		AfWnd::Release();
		return ::InterlockedDecrement(&cref);
	}
	STDMETHOD(GetGraphics)(IVwRootBox * pRoot, IVwGraphics ** ppvg,
		RECT * prcSrcRoot, RECT * prcDstRoot);
	STDMETHOD(get_LayoutGraphics)(IVwRootBox * pRoot, IVwGraphics ** ppvg);
	STDMETHOD(get_ScreenGraphics)(IVwRootBox * prootb, IVwGraphics ** ppvg);
	STDMETHOD(GetTransformAtDst)(IVwRootBox * pRoot,  POINT pt,
		RECT * prcSrcRoot, RECT * prcDstRoot);
	STDMETHOD(GetTransformAtSrc)(IVwRootBox * pRoot,  POINT pt,
		RECT * prcSrcRoot, RECT * prcDstRoot);
	STDMETHOD(ReleaseGraphics)(IVwRootBox * prootb, IVwGraphics * pvg);

	virtual void PreCreateHwnd(CREATESTRUCT & cs)
	{
		SuperClass::PreCreateHwnd(cs);
		cs.style |= WS_CHILD | WS_CLIPCHILDREN;
	}

	// This method should be overrided to clear all smart pointer member variables
	// in the subclass. It gets called during the WM_NCDESTROY message.
	virtual void OnReleasePtr()
	{
		AfVwRootSite::OnReleasePtr();
		SuperClass::OnReleasePtr();
	}

	virtual void ScrollBy(int dxdOffset, int dydOffset);
	virtual bool OnSetFocus(HWND hwndOld, bool fTbControl = false);
	virtual void SwitchFocusHere();
	virtual bool IsCurrentPane();
	virtual void SetFocusToRootSite();

	virtual void MakeSelectionVisible1(IVwSelection * psel = NULL);
	virtual void ScrollSelectionNearTop1(IVwSelection * psel = NULL);

	virtual void CacheSelectedObjects();
	virtual void GetSelectedObjects(Vector<HVO> & vhvo);
	virtual int GetLocation(Vector<HVO> & vhvo, Vector<int> & vflid);
	virtual void CopyRootTo(AfVwSplitChild * pvswOther);

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);

	virtual bool OnSize(int nId, int dxp, int dyp);

	/*******************************************************************************************
		Command handlers.

		The functionality is all implemented on the mixin baseclass AfVwRootSite, but it doesn't
		work to put pointers to those functions directly in the command map, so we need
		functions that are directly implemented on this class.
		It also doesn't work (this appears to be a compiler bug) if the baseclass methods are
		virtual.
	*******************************************************************************************/
	virtual bool CmdFmtFnt(Cmd * pcmd)
		{ return CmdFmtFnt1(pcmd); }
	virtual bool CmdFmtWrtSys(Cmd * pcmd)
		{ return CmdFmtWrtSys1(pcmd); }
	virtual bool CmdFmtPara(Cmd * pcmd)
		{ return CmdFmtPara1(pcmd); }
	virtual bool CmsFmtPara(CmdState & cms)
		{ return CmsFmtPara1(cms); }
	virtual bool CmdFmtStyles(Cmd * pcmd)
		{ return CmdFmtStyles1(pcmd); }
	virtual bool CmsFmtStyles(CmdState & cms)
		{ return CmsFmtStyles1(cms); }
	virtual bool CmdApplyNormalStyle(Cmd * pcmd)
		{ return CmdApplyNormalStyle1(pcmd); }
	virtual bool CmdFmtBulNum(Cmd * pcmd)
		{ return CmdFmtBulNum1(pcmd); }
	virtual bool CmsFmtBulNum(CmdState & cms)
		{ return CmsFmtBulNum1(cms); }
	virtual bool CmdFmtBdr(Cmd * pcmd)
		{ return CmdFmtBdr1(pcmd); }
	virtual bool CmsFmtBdr(CmdState & cms)
		{ return CmsFmtBdr1(cms); }
	virtual bool CmdCharFmt(Cmd * pcmd)
		{ return CmdCharFmt1(pcmd); }
	virtual bool CmsCharFmt(CmdState & cms)
		{ return CmsCharFmt1(cms); }
	virtual bool CmdInsertPic(Cmd * pcmd)
		{ return CmdInsertPic1(pcmd); }
	virtual bool CmdFilePrint(Cmd * pcmd)
		{ return CmdFilePrint1(pcmd); }
	virtual bool CmdEditFind(Cmd * pcmd)
		{ return CmdEditFind1(pcmd); }
	virtual bool CmdEditSrchQuick(Cmd * pcmd)
		{ return CmdEditSrchQuick1(pcmd); }
	virtual bool CmsEditSrchQuick(CmdState & cms)
		{ return CmsEditSrchQuick1(cms); }
	virtual bool CmdEditNextMatch(Cmd * pcmd)
		{ return CmdEditNextMatch1(pcmd); }
	virtual bool CmdEditRepl(Cmd * pcmd)
		{ return CmdEditRepl1(pcmd); }
	virtual bool CmdViewRefresh(Cmd * pcmd)
		{ return CmdViewRefresh1(pcmd); }

	virtual bool CmdEditCut(Cmd * pcmd)
		{ return CmdEditCut1(pcmd); }
	virtual bool CmdEditCopy(Cmd * pcmd)
		{ return CmdEditCopy1(pcmd); }
	virtual bool CmdEditPaste(Cmd * pcmd)
		{ return CmdEditPaste1(pcmd); }
	virtual bool CmdEditDel(Cmd * pcmd)
		{ return CmdEditDel1(pcmd); }
	virtual bool CmdEditSelAll(Cmd * pcmd)
		{ return CmdEditSelAll1(pcmd); }
	virtual bool CmsEditCut(CmdState & cms)
		{ return CmsEditCut1(cms); }
	virtual bool CmsEditCopy(CmdState & cms)
		{ return CmsEditCopy1(cms); }
	virtual bool CmsEditPaste(CmdState & cms)
		{ return CmsEditPaste1(cms); }
	virtual bool CmsEditDel(CmdState & cms)
		{ return CmsEditDel1(cms); }
	virtual bool CmsEditSelAll(CmdState & cms)
		{ return CmsEditSelAll1(cms); }
	virtual bool CmsHaveRecord(CmdState & cms)
		{ return CmsHaveRecord1(cms); }

	virtual AfVwRootSite * OtherPane();

	CMD_MAP_DEC(AfVwSplitChild);
};


/*----------------------------------------------------------------------------------------------
	A view window which scrolls using its own scroll bar. It cannot be split in two, so for
	main document windows AfVwSplitChild is usually more desirable.
	Hungarian: avsw.
----------------------------------------------------------------------------------------------*/
class AfVwScrollWnd : public AfWnd, public AfVwScrollWndBase
{
	typedef AfWnd SuperClass;

protected:
	typedef AfVwScrollWndBase ScrollSuperClass;

public:
	AfVwScrollWnd() : AfVwScrollWndBase(this, true)
	{
	}

	// We have to be tricky here. There is an inherited reference count from
	// AfClientWnd. We want to go on existing as long as there are pointers
	// to either interface.
	STDMETHOD_(ULONG, AddRef)(void)
	{
		AfWnd::AddRef();
		return m_cref;
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		// AfDeFieldEditor::Release might delete this object, so we need to get the reference
		// count before calling it and subtract one.
		long cref = m_cref;
		AfWnd::Release();
		return ::InterlockedDecrement(&cref);
	}

	virtual void PreCreateHwnd(CREATESTRUCT & cs)
	{
		SuperClass::PreCreateHwnd(cs);
		cs.style |= WS_CHILD | WS_CLIPCHILDREN;
	}

	// This method should be overrided to clear all smart pointer member variables
	// in the subclass. It gets called during the WM_NCDESTROY message.
	virtual void OnReleasePtr()
	{
		AfVwRootSite::OnReleasePtr();
		SuperClass::OnReleasePtr();
	}

	virtual void ScrollBy(int dxdOffset, int dydOffset);

protected:
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnSize(int nId, int dxp, int dyp);

	/*******************************************************************************************
		Command handlers.

		The functionality is all implemented on the mixin baseclass AfVwRootSite, but it doesn't
		work to put pointers to those functions directly in the command map, so we need
		functions that are directly implemented on this class.
		It also doesn't work (this appears to be a compiler bug) if the baseclass methods are
		virtual.
	*******************************************************************************************/
	virtual bool CmdFmtFnt(Cmd * pcmd)
		{ return CmdFmtFnt1(pcmd); }
	virtual bool CmdFmtWrtSys(Cmd * pcmd)
		{ return CmdFmtWrtSys1(pcmd); }
	virtual bool CmdFmtPara(Cmd * pcmd)
		{ return CmdFmtPara1(pcmd); }
	virtual bool CmsFmtPara(CmdState & cms)
		{ return CmsFmtPara1(cms); }
	virtual bool CmdFmtStyles(Cmd * pcmd)
		{ return CmdFmtStyles1(pcmd); }
	virtual bool CmsFmtStyles(CmdState & cms)
		{ return CmsFmtStyles1(cms); }
	virtual bool CmdApplyNormalStyle(Cmd * pcmd)
		{ return CmdApplyNormalStyle1(pcmd); }
	virtual bool CmdFmtBulNum(Cmd * pcmd)
		{ return CmdFmtBulNum1(pcmd); }
	virtual bool CmsFmtBulNum(CmdState & cms)
		{ return CmsFmtBulNum1(cms); }
	virtual bool CmdFmtBdr(Cmd * pcmd)
		{ return CmdFmtBdr1(pcmd); }
	virtual bool CmsFmtBdr(CmdState & cms)
		{ return CmsFmtBdr1(cms); }
	virtual bool CmdCharFmt(Cmd * pcmd)
		{ return CmdCharFmt1(pcmd); }
	virtual bool CmsCharFmt(CmdState & cms)
		{ return CmsCharFmt1(cms); }
	virtual bool CmdInsertPic(Cmd * pcmd)
		{ return CmdInsertPic1(pcmd); }
	virtual bool CmdEditFind(Cmd * pcmd)
		{return CmdEditFind1(pcmd); }
	// Method below is not currently used: the execution of the Quick Search control is handled by
	// intercepting the Enter key.
	virtual bool CmdEditSrchQuick(Cmd * pcmd)
		{return CmdEditSrchQuick1(pcmd); }
	virtual bool CmsEditSrchQuick(CmdState & cms)
		{return CmsEditSrchQuick1(cms); }
	virtual bool CmdEditNextMatch(Cmd * pcmd)
		{return CmdEditNextMatch1(pcmd); }
	virtual bool CmdEditRepl(Cmd * pcmd)
		{return CmdEditRepl1(pcmd); }
	virtual bool CmdViewRefresh(Cmd * pcmd)
		{return CmdViewRefresh1(pcmd); }

	virtual bool CmdEditCut(Cmd * pcmd)
		{ return CmdEditCut1(pcmd); }
	virtual bool CmdEditCopy(Cmd * pcmd)
		{ return CmdEditCopy1(pcmd); }
	virtual bool CmdEditPaste(Cmd * pcmd)
		{ return CmdEditPaste1(pcmd); }
	virtual bool CmdEditDel(Cmd * pcmd)
		{ return CmdEditDel1(pcmd); }
	virtual bool CmdEditSelAll(Cmd * pcmd)
		{ return CmdEditSelAll1(pcmd); }
	virtual bool CmsEditCut(CmdState & cms)
		{ return CmsEditCut1(cms); }
	virtual bool CmsEditCopy(CmdState & cms)
		{ return CmsEditCopy1(cms); }
	virtual bool CmsEditPaste(CmdState & cms)
		{ return CmsEditPaste1(cms); }
	virtual bool CmsEditDel(CmdState & cms)
		{ return CmsEditDel1(cms); }
	virtual bool CmsEditSelAll(CmdState & cms)
		{ return CmsEditSelAll1(cms); }

	CMD_MAP_DEC(AfVwScrollWnd);
};


/*----------------------------------------------------------------------------------------------
	A base class for views that have no scroll bar (e.g., a preview window, or one field of
	a data entry view). The parent window might, of course, scroll, taking the view along as
	a whole.
	Hungarian: avw.
----------------------------------------------------------------------------------------------*/
class AfVwWnd : public AfWnd, public AfVwRootSite
{
	typedef AfWnd SuperClass;
	typedef AfVwRootSite RootSiteSuperClass;
public:

	AfVwWnd();
	virtual ~AfVwWnd();

	// We have to be tricky here. There is an inherited reference count from
	// AfClientWnd. We want to go on existing as long as there are pointers
	// to either interface.
	STDMETHOD_(ULONG, AddRef)(void)
	{
		AfWnd::AddRef();
		return m_cref;
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		// AfDeFieldEditor::Release might delete this object, so we need to get the reference
		// count before calling it and subtract one.
		long cref = m_cref;
		AfWnd::Release();
		return ::InterlockedDecrement(&cref);
	}

	// Window message handling.
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnPaint(HDC hdc);
	virtual bool OnKillFocus(HWND hwndNew);
	virtual bool OnSize(int nId, int dxp, int dyp);
	virtual bool OnMouseMove(uint grfmk, int xp, int yp);

	virtual bool IsSelectionVisible1(IVwSelection * psel = NULL);
	virtual void ScrollSelectionNearTop1(IVwSelection * psel = NULL);

	virtual void PreCreateHwnd(CREATESTRUCT & cs)
	{
		SuperClass::PreCreateHwnd(cs);
		cs.style |= WS_CHILD | WS_CLIPCHILDREN;
	}

	// Set the width last used to lay out the root box. This is useful when two objects are
	// sharing the root box, such as an AfDeFeVw and an active view window.
	void SetLastLayoutWidth(int dxp)
	{
		m_dxdLayoutWidth = dxp;
	}

	// This method should be overrided to clear all smart pointer member variables
	// in the subclass. It gets called during the WM_NCDESTROY message.
	virtual void OnReleasePtr()
	{
		AfVwRootSite::OnReleasePtr();
		SuperClass::OnReleasePtr();
	}

	/*******************************************************************************************
		Command handlers.

		The functionality is all implemented on the mixin baseclass AfVwRootSite, but it doesn't
		work to put pointers to those functions directly in the command map, so we need
		functions that are directly implemented on this class.
		It also doesn't work (this appears to be a compiler bug) if the baseclass methods are
		virtual.
	*******************************************************************************************/
	virtual bool CmdFmtFnt(Cmd * pcmd)
		{ return CmdFmtFnt1(pcmd); }
	virtual bool CmdFmtWrtSys(Cmd * pcmd)
		{ return CmdFmtWrtSys1(pcmd); }
	virtual bool CmdFmtPara(Cmd * pcmd)
		{ return CmdFmtPara1(pcmd); }
	virtual bool CmsFmtPara(CmdState & cms)
		{ return CmsFmtPara1(cms); }
	virtual bool CmdFmtStyles(Cmd * pcmd)
		{ return CmdFmtStyles1(pcmd); }
	virtual bool CmsFmtStyles(CmdState & cms)
		{ return CmsFmtStyles1(cms); }
	virtual bool CmdApplyNormalStyle(Cmd * pcmd)
		{ return CmdApplyNormalStyle1(pcmd); }
	virtual bool CmdFmtBulNum(Cmd * pcmd)
		{ return CmdFmtBulNum1(pcmd); }
	virtual bool CmsFmtBulNum(CmdState & cms)
		{ return CmsFmtBulNum1(cms); }
	virtual bool CmdFmtBdr(Cmd * pcmd)
		{ return CmdFmtBdr1(pcmd); }
	virtual bool CmsFmtBdr(CmdState & cms)
		{ return CmsFmtBdr1(cms); }
	virtual bool CmdCharFmt(Cmd * pcmd)
		{ return CmdCharFmt1(pcmd); }
	virtual bool CmsCharFmt(CmdState & cms)
		{ return CmsCharFmt1(cms); }
	virtual bool CmdInsertPic(Cmd * pcmd)
		{ return CmdInsertPic1(pcmd); }
	virtual bool CmdEditFind(Cmd * pcmd)
		{return CmdEditFind1(pcmd); }
	virtual bool CmdEditSrchQuick(Cmd * pcmd)
		{return CmdEditSrchQuick1(pcmd); }
	virtual bool CmsEditSrchQuick(CmdState & cms)
		{return CmsEditSrchQuick1(cms); }
	virtual bool CmdEditNextMatch(Cmd * pcmd)
		{return CmdEditNextMatch1(pcmd); }
	virtual bool CmdEditRepl(Cmd * pcmd)
		{return CmdEditRepl1(pcmd); }
	virtual bool CmdViewRefresh(Cmd * pcmd)
		{return CmdViewRefresh1(pcmd); }

	virtual bool CmdEditCut(Cmd * pcmd)
		{ return CmdEditCut1(pcmd); }
	virtual bool CmdEditCopy(Cmd * pcmd)
		{ return CmdEditCopy1(pcmd); }
	virtual bool CmdEditPaste(Cmd * pcmd)
		{ return CmdEditPaste1(pcmd); }
	virtual bool CmdEditDel(Cmd * pcmd)
		{ return CmdEditDel1(pcmd); }
	virtual bool CmdEditSelAll(Cmd * pcmd)
		{ return CmdEditSelAll1(pcmd); }
	virtual bool CmsEditCut(CmdState & cms)
		{ return CmsEditCut1(cms); }
	virtual bool CmsEditCopy(CmdState & cms)
		{ return CmsEditCopy1(cms); }
	virtual bool CmsEditPaste(CmdState & cms)
		{ return CmsEditPaste1(cms); }
	virtual bool CmsEditDel(CmdState & cms)
		{ return CmsEditDel1(cms); }
	virtual bool CmsEditSelAll(CmdState & cms)
		{ return CmsEditSelAll1(cms); }

	void GetLgWritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
	{
		AssertPtr(ppwsf);
		Assert(*ppwsf == NULL);
		*ppwsf = m_qwsf;
		AddRefObj(*ppwsf);
	}
	void SetLgWritingSystemFactory(ILgWritingSystemFactory * pwsf) { m_qwsf = pwsf; }

protected:
	virtual void MakeSelectionVisible1(IVwSelection * psel = NULL);

	// For replacing the selection after resizing:
	bool m_fResizeMakeSelVis;
	ILgWritingSystemFactoryPtr m_qwsf; // Needed for AfLib-based dialogs used by C# code.

	virtual void GetScrollOffsets(int * pdxd, int * pdyd)
	{
		*pdxd = 0;
		*pdyd = 0;
	}

	CMD_MAP_DEC(AfVwWnd);
};

/*----------------------------------------------------------------------------------------------
	A dummy root site for printing. This is needed so that if the view being printed contains
	lazy boxes, they can get the information they need to expand and lay themselves out.
	Most of the methods have dummy implementations.
	Hungarian: avrs
----------------------------------------------------------------------------------------------*/
class AfPrintRootSite : public IVwRootSite
{
public:
	AfPrintRootSite(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int dxpAvailWidth);
	~AfPrintRootSite();

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// IVwRootSite methods.
	STDMETHOD(InvalidateRect)(IVwRootBox * pRoot, int twLeft, int twTop, int twWidth,
		int twHeight);
	STDMETHOD(GetGraphics)(IVwRootBox * pRoot, IVwGraphics ** ppvg,
		RECT * prcSrcRoot, RECT * prcDstRoot);
	STDMETHOD(get_LayoutGraphics)(IVwRootBox * pRoot, IVwGraphics ** ppvg);
	STDMETHOD(get_ScreenGraphics)(IVwRootBox * prootb, IVwGraphics ** ppvg);
	STDMETHOD(GetTransformAtDst)(IVwRootBox * pRoot,  POINT pt,
		RECT * prcSrcRoot, RECT * prcDstRoot);
	STDMETHOD(GetTransformAtSrc)(IVwRootBox * pRoot,  POINT pt,
		RECT * prcSrcRoot, RECT * prcDstRoot);
	STDMETHOD(ReleaseGraphics)(IVwRootBox * prootb, IVwGraphics * pvg);
	STDMETHOD(GetAvailWidth)(IVwRootBox * prootb, int * ptwWidth);
	STDMETHOD(RootBoxSizeChanged)(IVwRootBox * prootb);
	STDMETHOD(AdjustScrollRange)(IVwRootBox * prootb, int dxdSize, int dxdPosition,
		int dydSize, int dydPosition, ComBool * pfForcedScroll);
	STDMETHOD(DoUpdates)(IVwRootBox * prootb);
	STDMETHOD(SelectionChanged)(IVwRootBox * prootb, IVwSelection * pvwselNew);
	STDMETHOD(OverlayChanged)(IVwRootBox * prootb, IVwOverlay * pvo);
	STDMETHOD(get_SemiTagging)(IVwRootBox * prootb, ComBool * pf);
	// These are meaningless for a print site, do nothing.
	STDMETHOD(ScreenToClient)(IVwRootBox * prootb, POINT * ppnt) {return E_NOTIMPL;}
	STDMETHOD(ClientToScreen)(IVwRootBox * prootb, POINT * ppnt) {return E_NOTIMPL;}
	STDMETHOD(GetAndClearPendingWs)(IVwRootBox * prootb, int * pws);
	STDMETHOD(IsOkToMakeLazy)(IVwRootBox * prootb, int ydTop, int ydBottom, ComBool * pfOK);
	STDMETHOD(OnProblemDeletion)(IVwSelection * psel, VwDelProbType dpt,
		VwDelProbResponse * pdpr);
	STDMETHOD(OnInsertDiffParas)(IVwRootBox * prootb, ITsTextProps * pttpDest, int cPara,
		ITsTextProps ** prgpttpSrc, ITsString ** prgptssSrc,  ITsString * ptssTrailing,
		VwInsertDiffParaResponse * pidpr)
	{
		return E_NOTIMPL;
	}
	STDMETHOD(get_TextRepOfObj)(GUID * pguid, BSTR * pbstrText)
	{
		return E_NOTIMPL;
	}
	STDMETHOD(get_MakeObjFromText)(BSTR bstrText, IVwSelection * pselDst,
		int * podt, GUID * pGuid)
	{
		return E_NOTIMPL;
	}
	STDMETHOD(ScrollSelectionIntoView)(IVwSelection * psel, VwScrollSelOpts ssoFlag)
		{return E_NOTIMPL;}
	STDMETHOD(get_RootBox)(IVwRootBox ** pprootb) {return E_NOTIMPL;}
	STDMETHOD(get_Hwnd)(DWORD * phwnd) {return E_NOTIMPL;}

protected:

	long m_cref;
	IVwGraphicsPtr m_qvg;
	Rect m_rcSrc;
	Rect m_rcDst;
	int m_dxpAvailWidth;
};

DEFINE_COM_PTR(AfPrintRootSite);


/*----------------------------------------------------------------------------------------------
	An implementation of IAdvInd3 used to receive print progress notifications.

	Hungarian: ppr
----------------------------------------------------------------------------------------------*/
class PrintProgressReceiver : public IAdvInd3
{
protected:
	HWND m_hwndDlg; // dialog in which to report progress.
	StrUni m_stuFmt;
	int m_nProgress;
	int m_nRangeMax;
	long m_cref;

public:
	PrintProgressReceiver(HWND hwndDlg);

	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	STDMETHOD(Step)(int nStepAmt);
	STDMETHOD(NextStage)();

	STDMETHOD(put_Message)(BSTR bstrMessage);
	STDMETHOD(put_Position)(int nPos);
	STDMETHOD(put_StepSize)(int nStepInc);
	STDMETHOD(put_Title)(BSTR bstrTitle);
	STDMETHOD(SetRange)(int nMin, int nMax);
};

DEFINE_COM_PTR(PrintProgressReceiver);


/*----------------------------------------------------------------------------------------------
	The basic document and browse split child window.
	@h3{Hungarian: avrsc}
----------------------------------------------------------------------------------------------*/
class AfVwRecSplitChild : public AfVwSplitChild
{
public:
	typedef AfVwSplitChild SuperClass;

	AfVwRecSplitChild(bool fScrollHoriz = false);
	virtual ~AfVwRecSplitChild();

	virtual bool FullRefresh();
	virtual bool PreSynchronize(SyncInfo & sync);
	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf, IVwRootBox ** pprootb)
	{
		MakeRoot(pvg, pwsf, pprootb, false);
	}
	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf, IVwRootBox ** pprootb,
		bool fPrintingSel) = 0;
	virtual bool SelectionInOneField();
	virtual void GetCurClsLevel(int * pclsid, int * pnLevel);

protected:
	VwCustomVcPtr m_qvcvc;

	virtual void CallMouseUp(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot);
	virtual bool CmsEditDel1(CmdState & cms);
	virtual bool CanCut();
	virtual bool CanPaste();
#ifdef BASELINE
	virtual void CallOnTyping(VwGraphicsPtr qvg, BSTR bstr,
		int cchBackspace, int cchDelForward, OLECHAR chFirst,
		RECT rcSrcRoot, RECT rcDstRoot);
#else
	virtual void CallOnTyping(IVwGraphicsWin32Ptr qvg, BSTR bstr,
		int cchBackspace, int cchDelForward, OLECHAR chFirst,
		RECT rcSrcRoot, RECT rcDstRoot);
#endif
};

#endif // AFVWWND_INCLUDED
