/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeVw.h
Responsibility: John Thomson
Last reviewed: never

Description:
	A superclass for field editors that contain a view: that is, the field editor creates a
	root box and displays it; when active, it creates an appropriate view window to handle
	editing.
	A base class for the view window is also implemented here: AfDeVw.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AfDeFeVw_INCLUDED
#define AfDeFeVw_INCLUDED 1

class AfDeFeVw;

/*----------------------------------------------------------------------------------------------
	A superclass for views embedded in field editors. In simple cases, it may be useable as is.
	Hungarian: dvw.
----------------------------------------------------------------------------------------------*/
class AfDeVwWnd : public AfVwWnd
{
	friend class AfDeFeVw;
public:
	AfDeVwWnd();
	AfDeVwWnd(AfDeFeVw * pdvw);
	typedef AfVwWnd SuperClass;
	// The root box gets made in the field editor. But this method has to exist because
	// the superclass expects it.
	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb)
	{
	}

//-	virtual void HandleSelectionChange(IVwSelection * pvwsel);
//-	virtual void HandleKeyboardChange(IVwSelection * pvwsel, int nLangID);

	void SetRootBox(IVwRootBox * prootb)
	{
		m_qrootb = prootb;
		// Assume it has been laid out correctly.
		m_dxdLayoutWidth = LayoutWidth();
	}
	virtual void RemoveRootRegistration();
	STDMETHOD(RootBoxSizeChanged)(IVwRootBox * prootb);
	STDMETHOD(AdjustScrollRange)(IVwRootBox * prootb, int dxdSize, int dxdPosition,
		int dydSize, int dydPosition, ComBool * pfForcedScroll);
	void SwitchFocusHere();
protected:
	// We must NOT close the root box because it is still in use by the field editor.
	virtual void CloseRootBox()
	{
	}
	bool OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);
	AfDeFeVw * m_pdfv;

	// Return the messages to use for the Find or Replace operation. Here we are searching
	// a single field at a time, so use messages indicating that.
	virtual void GetFindReplMsgs(int * pstidNoMatches, int * pstidReplaceN)
	{
		*pstidNoMatches = kstidNoMatchesField;
		*pstidReplaceN = kstidReplaceNField;
	}
};
DEFINE_COM_PTR(AfDeVwWnd);

/*----------------------------------------------------------------------------------------------
	A superclass for field editors based on a view.
	Hungarian: dfv.
----------------------------------------------------------------------------------------------*/
class AfDeFeVw : public AfDeFieldEditor, public AfInactiveRootSite
{
	friend class AfDeVwWnd;
public:
	typedef AfDeFieldEditor SuperClass; // Warning: this is only one of two superclasses!

	AfDeFeVw();
	virtual ~AfDeFeVw();
	virtual bool IsTextSelected();
	virtual void DeleteSelectedText();
	// This is required because the call to OnMouseMove comes through a pointer to an
	// AfDeFieldEditor. So unless we override in this class, it goes to the field editor
	// base class implementation, which does nothing.
	virtual bool OnMouseMove(uint grfmk, int xp, int yp)
	{
		return AfInactiveRootSite::OnMouseMove(grfmk, xp, yp);
	}

	// Override this method in your subclass.
	// It should make a root box and initialize it with appropriate data and
	// view constructor, etc.
	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb) = 0;

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);

	// We have to be tricky here. There is an inherited reference count from
	// AfDeFieldEditor. We want to go on existing as long as there are pointers
	// to either interface, so we must use the same reference count for both.
	STDMETHOD_(ULONG, AddRef)(void)
	{
		AfDeFieldEditor::AddRef();
		return m_cref;
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		// AfDeFieldEditor::Release might delete this object, so we need to get the reference
		// count before calling it and subtract one.
		long cref = m_cref;
		AfDeFieldEditor::Release();
		return ::InterlockedDecrement(&cref);
	}

	// IVwRootSite Methods
	STDMETHOD(InvalidateRect)(IVwRootBox* pRoot, int twLeft, int twTop, int twWidth,
		int twHeight);
	STDMETHOD(GetGraphics)(IVwRootBox * prootb, IVwGraphics ** ppvg,
		RECT * prcSrcRoot, RECT * prcDstRoot);
	STDMETHOD(get_LayoutGraphics)(IVwRootBox * prootb, IVwGraphics ** ppvg);
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
	STDMETHOD(SelectionChanged)(IVwRootBox * prootb, IVwSelection * pvwsel);
	STDMETHOD(OverlayChanged)(IVwRootBox * prootb, IVwOverlay * pvo);
	STDMETHOD(get_SemiTagging)(IVwRootBox * prootb, ComBool * pf);
	STDMETHOD(ScreenToClient)(IVwRootBox * prootb, POINT * ppnt);
	STDMETHOD(ClientToScreen)(IVwRootBox * prootb, POINT * ppnt);
	STDMETHOD(GetAndClearPendingWs)(IVwRootBox * prootb, int * pws);
	STDMETHOD(IsOkToMakeLazy)(IVwRootBox * prootb, int ydTop, int ydBottom, ComBool * pfOK);
	STDMETHOD(OnProblemDeletion)(IVwSelection * psel, VwDelProbType dpt,
			VwDelProbResponse * pdpr);
	STDMETHOD(get_RootBox)(IVwRootBox ** pprootb);
	STDMETHOD(get_Hwnd)(DWORD * phwnd);
	STDMETHOD(ScrollSelectionIntoView)(IVwSelection * psel, VwScrollSelOpts ssoFlag)
		{return E_NOTIMPL;}
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

	// This is required by OnMouseMove, just for the parent of any tooltips.
	virtual AfWnd * Window()
	{
		return GetDeWnd();
	}

	// Clear pointers back to the Window and break any other cycles.
	virtual void OnReleasePtr();

	virtual bool CmdFindInDictionary(Cmd * pcmd);
	virtual bool CmsFindInDictionary(CmdState & cms);

	/*------------------------------------------------------------------------------------------
		Required overrides from AfDeFieldEditor.
	------------------------------------------------------------------------------------------*/

	// Calculates the height for a given width, storing both, and returning the height.
	virtual int SetHeightAt(int dxpWidth);

	// Draw the field contents.
	virtual void Draw(HDC hdc, const Rect & rcClip);

	/*------------------------------------------------------------------------------------------
		Overrides to support editing. Note that to actually get editing, a subclass must
		override IsEditable to return true.
	------------------------------------------------------------------------------------------*/

	virtual bool IsDirty();
	// Create an editing window, set m_hwnd to the hwnd of the editor, and return true.
	// Return false if it failed. hwnd is the parent window, and rc is the rect for the
	// window based on parent window coordinates.
	virtual bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true);

	// Saves changes and returns true.
	// If the data is illegal, this should return false without saving the changes.
	// In this case, it should also raise an error for the user so they know what to fix.
	virtual bool SaveEdit();

	// Saves changes, destroys the window, and sets m_hwnd to 0.
	// If it is possible to have bad data, the user should call IsOkToClose() first.
	virtual void EndEdit(bool fForce = false);

	// Move the window to the new location bounded by rcClip. It is assumed that the
	// caller has already called SetHeightAt() so that it knows how much room is needed.
	virtual void MoveWnd(const Rect & rcClip);

	virtual void UpdateField();

	/*------------------------------------------------------------------------------------------
		New overrideable functions introducted by this class.
	------------------------------------------------------------------------------------------*/

	// Create and initialize an instance of the appropriate kind of window when editing is
	// to take place. The default version creates an AfVwWnd, and initializes it with this
	// window's root box. The arguments give its parent window and position relative to
	// that window.
	virtual AfDeVwWnd * CreateEditWnd(HWND hwndParent, Rect & rcBounds);

	IVwRootBox * GetRootBox() { return m_qrootb; }
	void UpdatePosition();
	virtual void SaveCursorInfo();
	virtual void RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur);
	// This is just here because the shared OnMouseMoved calls it. But this class, since it
	// is always inactive, will never experience a mouse move with the button pressed,
	// so it doesn't need to do anything.
	virtual void CallMouseMoveDrag(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot) {};

protected:
	// The width available for laying out the root box, as passed to SetHeightAt(),
	// or a large negative number if it has never been successfully laid out.
	int m_dxpLayoutWidth;

	// Other protected methods
	virtual void InitGraphics();
	virtual void UninitGraphics();
	int LayoutWidth();
	virtual void GetCoordRects(IVwGraphics * pvg, RECT * prcSrcRoot, RECT * prcDstRoot);
	bool Layout();

	// The embedded editor window, if editing is in progress.
	AfDeVwWndPtr m_qdvw;
	// Stores the clip rectangle most recently used to draw the field.
	// This provides the basis for the origin of the transformation used
	// in GetCoordRects.
	Rect m_rcClip;
	bool m_fDirty; // The field contents have been changed.
};


#endif // AfDeFeVw_INCLUDED
