/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeSplitChild.h
Responsibility: Ken Zook
Last reviewed: never

Description:
	Defines the base for data entry functions.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDESPLITCHILD_INCLUDED
#define AFDESPLITCHILD_INCLUDED 1

class AfDeFeRefs;
enum {kPopupMenu = 2}; // Flag to indicate popup menu is being executed.

/*----------------------------------------------------------------------------------------------
	The base data entry window for an application. This maintains a vector of AfDeFieldEditor.
	It draws an expandable tree view directly in the DC	on the left side with labels for each
	editor. In the right side, subclasses of AfDeFieldEditor draw their contents directly on
	the DC. A draggable vertical divider line is drawn between the label and contents.

	When a user clicks on a field on the right of the divider, the field editor creates a child
	window over that field to allow the user to edit the data. Only one field ever has an
	active editor with a child window. Prior to opening a new editor, the existing editor
	should first be closed. m_pdfe points to the active editor.

	This class should always be subclassed. This provides common code, but the subclass
	is responsible for loading the vector of field editors, and adding or subtracting field
	editors when the user expands or contracts a tree node. It is responsible for maintaining
	the displayed information in tree nodes, and for parts of the drag and drop operation. It
	is also responsible for adding and deleting subitems, loading and saving data, etc.

	@h3{Hungarian: adsc}
----------------------------------------------------------------------------------------------*/
class AfDeSplitChild : public AfSplitChild
{
	friend class AfClientRecWnd;
	typedef AfSplitChild SuperClass;
public:

	AfDeSplitChild(bool fAlignFieldsToTree = false);
	virtual ~AfDeSplitChild();

	// Return the pixel height of the font used in the tree view.
	int GetTreeFontHeight()
	{
		return m_dypTreeFont;
	}

	// Return the default pixel height of a field.
	int GetDefFieldHeight()
	{
		return m_dypDefFieldHeight;
	}

	// Return the minimum pixel height of a field. The minimum height cannot
	// be less than the +/- box height or the tree font height.
	int GetMinFieldHeight()
	{
		return Max((int)kdypBoxHeight, m_dypTreeFont);
	}

	// Return the pixel width of the tree portion (left of the divider line).
	// If this returns 0, the there is no divider line and the width is
	// variable. Use GetBranchWidth to get the width for a specific field editor.
	int GetTreeWidth()
	{
		return m_dxpTreeWidth;
	}

	// Return the pixel width of the tree portion for a specific field editor.
	virtual int GetBranchWidth(AfDeFieldEditor * pdfe);

	// Return true if tree is being separated from data by a vertical line.
	bool HasVerticalTreeSeparatorLine()
	{
		return !m_fAlignFieldsToTree;
	}

	// Return a pointer to the language project information.
	AfLpInfo * GetLpInfo()
	{
		Assert(m_qlpi);
		return m_qlpi;
	}

	// Set the pixel width of the tree ortion (left of the divider line)
	void SetTreeWidth(int dxp)
	{
		m_dxpTreeWidth = dxp;
	}

	// Return the vector of field editors.
	DeEditorVec & GetEditors()
	{
		return m_vdfe;
	}

	// Return the active field editor.
	// @return A pointer to the active editor, or NULL if there is none.
	AfDeFieldEditor * GetActiveFieldEditor()
	{
		return m_pdfe;
	}

	// Insert a field editor at the specified index.
	void AddFieldDirect(int idfe, AfDeFieldEditor * pdfe)
	{
		m_vdfe.Insert(idfe, pdfe);
		pdfe->AddRef();
	}

	// Scroll the data entry window up or down one page.
	// @param wp Virtual key code. Must be VK_PRIOR or VK_NEXT
	// @param lp Key data: scan code, repeat info, flags.
	bool ScrollKey(WPARAM wp, LPARAM lp)
	{
		Assert(wp == VK_PRIOR || wp == VK_NEXT);
		return OnKeyDown(wp, lp);
	}

	// Set the language project information pointer. This is normally set automatically,
	// derived from the appropriate main window during initializatin. If there is no main
	// window, the client should set it directly.
	// @param plpi Pointer to the language project information.
	void SetLpInfo(AfLpInfo * plpi)
	{
		AssertPtr(plpi);

		m_qlpi = plpi;
		AfDbInfo * pdbi = plpi->GetDbInfo();
		AssertPtr(pdbi);
		ILgWritingSystemFactoryPtr qwsf;
		pdbi->GetLgWritingSystemFactory(&qwsf);
		AssertPtr(qwsf);
		CheckHr(qwsf->get_UserWs(&m_wsUser));
		Assert(m_wsUser);
	}

	// Set the text heading for the specified tree node.
	// This is a stub to be overwritten by the subclass.
	// @param pdetn Pointer to the tree node we want to update.
	virtual void SetTreeHeader(AfDeFeNode * pden)
	{}

	// A field editor can call this method to jump (replace current record in current
	// window) to a new record with the indicated HVO. If the hvo isn't in the current
	// filter, this should use LaunchItem().
	// This is a stub to be overwritten by the subclass.
	// @param Non-null id of the object we want to make the main item in the current window.
	virtual void JumpItem(HVO hvo)
	{}

	// A field editor can call this method to launch (open in a new window) a new
	// object with the indicated HVO.
	// This is a stub to be overwritten by the subclass.
	// @param Non-null id of the object we want to display in a new window.
	virtual void LaunchItem(HVO hvo)
	{}

	// This is called whenever a field editor is opened to begin editing. It can be
	// defined in subclasses to do things such as conditionally saving the modification date.
	virtual void BeginEdit(AfDeFieldEditor * pdfe)
	{}

	int GetField(int ypIn, int * dypStart = NULL, int * dypExtra = NULL);
	int GetEditorIndex(int * pdyp);
	virtual bool OpenPreviousEditor(int dxpCursor = 0, bool fTopCursor = true);
	virtual bool OpenNextEditor(int dxpCursor = 0);
	virtual bool OpenEditor(int idfe, bool fSearch);
	void CloseAllEditors(bool fForce = false);
	void EditorResizing();
	void FieldSizeChanged(bool fNoPaint = false);
	int NextFieldAtIndent(int nInd, int idfe);
	int LastFieldAtSameIndent(int idfe);
	int FirstFieldOfTreeNode(int idfe);
	int DeleteTreeNode(HVO hvo, int idfeLim = 0, AfDeFieldEditor * pdfe = NULL);
	int EndOfNodes(HVO hvo, int flid, RecordSpec * prsp);
	void SetHeight(bool fNoScroll = false, bool fNoPaint = false);
	int GetCurNodeIndex(AfDeFieldEditor * pdfe = NULL);
	void AddTreeNode(AfDeFeTreeNode * pdetnSrc, int iOrd, RecordSpec * prsp);
	int FindInsertionIndex(HVO hvoOwn, int flid, int iOrd, HVO hvoNode, RecordSpec * prsp,
		int * pnInd);

	virtual bool GetHelpStrFromPt(Point pt, ITsString ** pptss);
	virtual void UpdateAllDEWindows(HVO hvoOwn, int flid, HVO hvoNode = 0);
	virtual void UpdateField(HVO hvoOwn, int flid, HVO hvoNode);
	virtual void CheckTreeFld(HVO hvoOwn, int flid, HVO hvoNode);
	virtual void ResetNodeHeaders();
	virtual bool Save();
	virtual HVO LaunchRefChooser(AfDeFeRefs * pdfr);
	virtual HVO GetDragObject(AfDeFieldEditor * pdfe);
	int TopOfField(int idfe);
	int TopOfField(AfDeFieldEditor * pdfeTarget);
	virtual void OnStylesheetChange();
	virtual void OnPreActivate(bool fActivating);
	virtual bool CloseEditor();
	// Possible generic deletion method, which, if it is right should be renamed to CmdDelete.
	// In this case the subclasses should call it, and set their maps to it.
	virtual void MoveRecord(HVO hvo, int clid, int idfe, int drp) {};
	virtual bool CloseProj();
	// This method is called prior to displaying the window, and is a convenient place to
	// add command handlers, etc.
	virtual void PrepareToShow()
	{
		AfApp::Papp()->AddCmdHandler(this, 1, kgrfcmmAll);
	}
	virtual bool IsOkToChange(bool fChkReq = false);
	virtual void PrepareToHide();
	virtual int GetLocation(Vector<HVO> & vhvo, Vector<int> & vflid);
	virtual void OpenPath(Vector<HVO> & vhvoPath, Vector<int> & vflidPath, int ichCur);
	void SwitchFocusHere();
	virtual bool Synchronize(SyncInfo & sync);
	virtual bool InsertSubItem(int clsidOwner, int flidOwner, int flidOwnerModified,
			int flidType, HVO hvoOwner,
			int clsidNew, int stidUndoRedo, int stidLabel, int nIndent);

protected:

	int GetBranchWidth(int idfe);
	int GetBranchWidthAt(int yp);

	virtual void OnLButtonDownInEditor(int idfe, AfDeFieldEditor * pdfe, int dypFieldTop,
		uint grfmk, int xp, int yp);
	virtual void OnRButtonDownInEditor(int idfe, AfDeFieldEditor * pdfe, int dypFieldTop,
		uint grfmk, int xp, int yp);
	virtual void CreateActiveEditorWindow(AfDeFieldEditor * pdfe, int dypFieldTop);
	virtual RecordSpec * GetRecordSpec(int idfe, FldSpec ** ppfsp = NULL);
	int ShowField(RecordSpec * prsp, BlockSpec * pbsp, int idfe);
	bool CloseToTreeSeparator(int xp)
	{
		return (!m_fAlignFieldsToTree &&
		m_dxpTreeWidth - kdxpActiveTreeBorder < xp &&
		xp <= m_dxpTreeWidth);
	}

	// If a drop target is present, this method should set m_idfeDst, m_drp,
	// and m_dxpTopDst, and return true. Otherwise it should return false.
	virtual bool GetDropTarget(int yp);

	// This needs to return a valid flid where we can place an object of the specified
	// class in the object with the specified id.
	// @param hvoDstOwner The object id of the object holding flid.
	// @param clid The class id of the object we want to insert into flid.
	// @param pptss Pointer to receive the string label name for this field.
	// @return The flid in hvoDstOwner where we are inserting the new object.
	virtual int GetDstFlidAndLabel(HVO hvoDstOwner, int clid, ITsString ** pptss)
	{
		Assert(false); // This needs to be overwritten.
		*pptss = NULL;
		return 0;
	}

	// Overrides PreCreateHwnd from the superclass to change the window style.
	virtual void PreCreateHwnd(CREATESTRUCT & cs)
	{
		SuperClass::PreCreateHwnd(cs);
		cs.style |= WS_CHILD | WS_CLIPCHILDREN;
	}

	virtual int AddFields(HVO hvoRoot, ClsLevel & clev, CustViewDa *pcvd, int idfe, int nInd);

	// Add a field editor for a given field at the location and indent specified.
	// This needs to be overridden to do something useful.
	// @param hvoRoot Id of the root object that holds the fields we want to display.
	// @param clid Class of the root object.
	// @param nLev Level (main/sub) of the root object in the window.
	// @param pbsp The FldSpec that defines this field.
	// @param pcvd Pointer to the CustViewDa specifying what fields to display.
	// @param idfe Index where the new fields are to be inserted. On return it contains
	//    an index to the field following any inserted fields.
	// @param nInd Indent of the fields to be added.
	// @param fAlwaysVisible If true, show field regardless of pbsp->m_eVisibility
	virtual void AddField(HVO hvoRoot, int clid, int nLev, FldSpec * pfsp,
		CustViewDa * pcvd, int & idfe, int nInd = 0, bool fAlwaysVisible = false);

	virtual void ActivateField(int idfe);

	// Override this if your subclass wants to add anything to the context-sensitive
	// right-click Insert menu in the tree view.
	// @param hmenu Handle to the menu in which to add the items.
	virtual void AddContextInsertItems(HMENU & hmenu)
	{}

	// Override this if your subclass wants to add anything to the context-sensitive
	// right-click Show menu in the tree view.
	// @param hmenu Handle to the menu in which to add the items.
	virtual void AddContextShowItems(HMENU & hmenu)
	{}

	// Added virtual functions to allow derived classes to specify different colors.
	virtual const COLORREF GetLineColor()
	{
		return ::GetSysColor(COLOR_3DSHADOW);
	}

	virtual const COLORREF GetTreeBackgroundColor()
	{
		return ::GetSysColor(COLOR_3DFACE);
	}

	virtual const COLORREF GetSeparatorLineColor()
	{
		return ::GetSysColor(COLOR_WINDOWTEXT);
	}

	// This is used when drawing the separator line between fields. It returns the gap
	// (in pixels). That is, how many pixels of the background color that are fall
	// between that pixels of the line color returned by GetSeparatorLineColor. This
	// allows derived classes to create lines ranging from solid to very sparsely
	// dotted. Zero is the default which yields a solid line.
	virtual const int GetSeparatorLineDotSpacing()
	{
		return 0;
	}

	bool ToggleExpansionAndScroll(int idfe, int dypFieldTop);
	void Draw(HDC hdc, const Rect & rcpClient, const Rect & rcpClip);
	void GetObjOwnerAndProp(int idfe, HVO * phvoOwner, int * pflid);
	int IndexOfField(AfDeFieldEditor * pdfe);
	virtual bool HasRequiredData();
	virtual bool GetIndexAndIndent(bool fPopupMenu, int iflid, bool &fFlid,
			int & idfe, int & nInd, HVO & hvoOwner);
	// Set the path/flid list in the main window to the hvo entry that was promoted.
	// This needs to be defined in subclasses to do something meaningful.
	virtual void PromoteSetup(HVO hvo)
	{}


	//:>****************************************************************************************
	//:> IDropTarget methods.
	//:>****************************************************************************************
	STDMETHOD(DragOver)(DWORD grfKeyState, POINTL pt, DWORD * pdwEffect);
	STDMETHOD(Drop)(IDataObject * pDataObject, DWORD grfKeyState, POINTL pt, DWORD * pdwEffect);
	STDMETHOD(DragLeave)(void);
	STDMETHOD(DragEnter)(IDataObject * pDataObject, DWORD grfKeyState, POINTL pt,
		DWORD * pdwEffect);

	virtual void ToggleExpansion(int idfe);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnKeyDown(WPARAM wp, LPARAM lp);
	virtual bool OnMouseMove(uint grfmk, int xp, int yp);
	virtual bool OnLButtonDown(uint grfmk, int xp, int yp);
	virtual bool OnRButtonDown(uint grfmk, int xp, int yp);
	virtual bool OnLButtonDblClk(uint grfmk, int xp, int yp);
	virtual bool OnLButtonUp(uint grfmk, int xp, int yp);
	virtual bool OnVScroll(int wst, int yp, HWND hwndSbar);
	virtual bool OnSize(int nId, int dxp, int dyp);
	virtual bool OnPaint(HDC hdc);
	virtual void PostAttach(void);
	virtual bool OnCommand(int cid, int nc, HWND hctl);
	virtual bool OnSetFocus();
	virtual bool OnTimer(int nId);
	virtual bool OnDragOverTree(int xp, int yp);
	virtual bool OnBeginDragDrop(AfDeFieldEditor * pdfe, int xp, int yp);
	virtual void ClearTarget(int xp, int yp);
	virtual void OnReleasePtr();
	virtual void OnDestroy();
	virtual int DefaultFieldHeight();
	virtual bool OnContextMenu(HWND hwnd, Point pt);
	// This is overridden in AfLazyDeWnd where not all slots need be filled.
	virtual AfDeFieldEditor * FieldAt(int idfe)
	{
		return m_vdfe[idfe];
	}
	virtual void AddExtraContextMenuItems(HMENU hmenuPopup)
	{
	}

	CMD_MAP_DEC(AfDeSplitChild);
	virtual bool CmdExpContextMenu(Cmd * pcmd);
	virtual bool CmsExpContextMenu(CmdState & cms);

	//:>****************************************************************************************
	//:> Constants used for drag and drop operations.
	//:>****************************************************************************************
	enum
	{
		kdrpAbove,	// Target moves above m_pdfeDst.
		kdrpOn,		// Target moves at end of fields for m_pdfeDst.
		kdrpBelow,	// Target moves below m_pdfeDst.
		kdrpAtEnd,	// Target moves at end of fields. Used when there are no valid targets. In
					// this case, m_pdfeDst contains the final field, to give access to
					// AfDeSplitChild.
		kdrpLim,
	};

	//:>****************************************************************************************
	//:> Member variables.
	//:>****************************************************************************************

	// Vector of all field editors currently instantiated. This expands and contracts as
	// outlines are opened and closed.
	DeEditorVec m_vdfe;
	// Pointer to the active field editor. The active editor is the only one that has an
	// actual window. Only one editor can be active at a time.
	AfDeFieldEditor * m_pdfe;
	int m_dxpTreeWidth; // The width (pixels) of the tree pane. Ignored if m_fAlignFieldsToTree

	// Allows a jagged boundary between tree and field editors. Also suppresses drawing the
	// border line between the tree and the field editors (hence the user may not adjust the
	// tree's width). A field editor's left edge will be aligned just beyond the width of its
	// adjacent tree branch or leaf but not necessarily aligned with the field editors above
	// and below.
	bool m_fAlignFieldsToTree;

	int m_dypTreeFont; // Height (pixels) of the tree font.
	int m_dypDefFieldHeight; // Default height (pixels) of a field.
	int m_dypEditors; // The total height (pixels) of all editors.
	bool m_fChangingTreeWid; // The user is dragging the tree border.
	// A handle to the active/inactive tool tip window. This window is always available, but
	// is hidden when it is not needed.
	HWND m_hwndToolTip;
	AfLpInfoPtr m_qlpi; // Pointer to the current language project info.
	int m_wsUser;		// user interface writing system id.
	int m_idfe; // Index of editor where user right-clicked.
	Vector<BlockSpec *> m_vbsp; // Vector of blockspecs used in Show right-click menu.
	UserViewSpecPtr m_quvs; // This is set up to specify the fields to be displayed.

	//:>****************************************************************************************
	//:> Member variables used for drag and drop.
	//:>****************************************************************************************
	AfDeFieldEditor * m_pdfeDst; // The destination field for a field dragging operation.
	int m_idfeDst; // The index for the target editor.
	int m_drp; // Location of the drop relative to the target field (kdrpAbove, kdrpOn, etc.)
	int m_ypTopDst; // The top of the destination field. (pixels from top of window).
	// Left side of target label/bar highlight (pixels from left of window).
	// This also acts as a flag indicating we have a target marked.
	int m_xpMrkLeft;
	int m_xpMrkRight; // Right side of target label/bar highlight (pixels from left of window).
	int m_ypMrkTop; // Top of target label/bar highlight (pixels from top of window).
	int m_ypMrkBottom; // Bottom of target label/bar highlight (pixels from top of window).
};

/*----------------------------------------------------------------------------------------------
	This class is used for a "data entry" window (often fake) such as a concordance, where
	all items are much the same, and it is not desirable to create all the field editors
	ahead of time. Instead, they are created as they become visible.

	@h3{Hungarian: aldw}
----------------------------------------------------------------------------------------------*/
class AfLazyDeWnd : public AfDeSplitChild
{
	typedef AfDeSplitChild SuperClass;
public:
	AfLazyDeWnd(bool fAlignFieldsToTree = false);

	virtual AfDeFieldEditor * FieldAt(int idfe);

	// A subclass must provide this method if it creates virtual slots, which is the
	// main reason for using this class.
	virtual AfDeFieldEditor * MakeEditorAt(int idfe) = 0;
};

/*----------------------------------------------------------------------------------------------
	This class is used for a "data entry" window (often fake) such as a concordance, where
	all items are much the same, and it is not desirable to create all the field editors
	ahead of time. Instead, they are created as they become visible.

	@h3{Hungarian: arsc}
----------------------------------------------------------------------------------------------*/
class AfDeRecSplitChild : public AfDeSplitChild
{
	typedef AfDeSplitChild SuperClass;
public:
	AfDeRecSplitChild(bool fAlignFieldsToTree = false);
	virtual ~AfDeRecSplitChild();

	virtual void MoveRecord(HVO hvo, int clid, int idfe, int drp);
	virtual void MoveRecordFromMainItems(HVO hvo);
	virtual void MoveRecordToMainItems(HVO hvo, int clid);
	virtual bool CmdDeleteObject(Cmd * pcmd);
	virtual bool CmdPromote(Cmd * pcmd);
	virtual bool CmsPromote(CmdState & cms);
	virtual void SetRootObj(HvoClsid & hcRoot, bool fNeedToReadData);
	virtual void GetCurClsLevel(int * pclsid, int * pnLevel);

protected:
	virtual bool CmsHaveRecord(CmdState & cms);
	virtual bool CmsInsertSubentry(CmdState & cms);
	virtual bool ConfirmDeletion(int flid, bool & fAtomic, bool & fTopLevelObj,
			int & kstid, bool & fHasRefs);
	virtual void LoadOtherData(IOleDbEncap * pode, CustViewDa * pcvd, HvoClsid & hcRoot) {}
	// Subclasses should override this,
	// if the object of class clid also has a modifed date.
	// 'clid' comes from the owning object.
	virtual int GetOwnerModifiedFlid(int clid)
	{ return 0; }
	virtual bool IsSubitemFlid(int flid)
	{ return false; }

	CMD_MAP_DEC(AfDeRecSplitChild);
};

#endif // AFDESPLITCHILD_INCLUDED
