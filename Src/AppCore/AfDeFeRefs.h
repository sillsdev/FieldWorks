/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeRefs.h
Responsibility: Ken Zook
Last reviewed: never

These classes all work together to provide a field editor for references to non-CmPossibilities.
Defines
	RefsVc: The view constructor that determines how the reference view appears.
	AfDeFeRefs: A field editor to display atomic or sequence references.
	AfDeFeRefs::AfDeSelListener: Keeps track of the selection.
	AfDeFeRefs::DfrButton: Displays and processes the button that calls up the ref chooser.
	AfDeRefsWnd: A window allowing references to be added and deleted.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDEFE_REFS_INCLUDED
#define AFDEFE_REFS_INCLUDED 1


// Enumeration to provide fragment identifiers for the view constructor.
enum
{
	kfrRefName, // Display a name for a referenced object.
	kfrListName, // Display a name to be used in a list of objects.
};


/*----------------------------------------------------------------------------------------------
	A view constructor that displays a single item in a reference view. It may also be used
	to provide views in list choosers, etc. This defines a generic view of any object.
	It should be subclassed to provide more meaningful views of most objects.

	Depending on fLoadData, the view constructor needs to load anything it uses, since the
	desired information may not be in the cache.

	@h3{Hungarian: ovc}
----------------------------------------------------------------------------------------------*/
class ObjVc : public VwBaseVc
{
public:
	typedef VwBaseVc SuperClass;

	ObjVc(bool fLoadData = true);
	virtual ~ObjVc();

	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(LoadDataFor)(IVwEnv * pvwenv, HVO hvo, int frag);

	void SetDbInfo(AfDbInfo * pdbi)
	{
		AssertPtr(pdbi);
		m_qdbi = pdbi;
	}

protected:
	bool m_fLoadData; // If true, the VC should load any data it needs to use.

	AfDbInfoPtr m_qdbi;
};
typedef GenSmartPtr<ObjVc> ObjVcPtr;



// Enumeration to provide fragment identifiers for the view constructor.
enum
{
	kfrMultiRefs, // Top level to process multiple references.
	kfrSingleRef, // Top level to process a single reference.
	kfrObjName, // Display the name for an object reference.
};


/*----------------------------------------------------------------------------------------------
	The view constructor that determines how the reference view appears. If the property is
	a sequence, it produces a paragraph of references with a gray bar following each reference.
	If the property is atomic, it just shows the reference.

	@h3{Hungarian: rvc}
----------------------------------------------------------------------------------------------*/
class RefsVc : public VwBaseVc
{
public:
	typedef VwBaseVc SuperClass;

	RefsVc(int flid, ObjVc * povc, LPCOLESTR pszSty, COLORREF clrBkg = -1);
	virtual ~RefsVc();

	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(DisplayVec)(IVwEnv * pvwenv, HVO hvo, int tag, int frag);
	STDMETHOD(GetStrForGuid)(BSTR bstrGuid, ITsString ** pptss);
	STDMETHOD(DoHotLinkAction)(BSTR bstrData, HVO hvoOwner, PropTag tag, ITsString * ptss,
		int ichObj);

	void SetDbInfo(AfDbInfo * pdbi)
	{
		AssertPtr(pdbi);
		AssertPtr(m_qovc);

		m_qdbi = pdbi;
		m_qovc->SetDbInfo(pdbi);
	}

protected:
	StrUni m_stuSty; // Text properties.
	COLORREF m_clrBkg; // Color for paragraph background.
	int m_flid; // The id of the field from which we are displaying references.
	ObjVcPtr m_qovc; // View constructor used to display each item.
	AfDbInfoPtr m_qdbi;
};
typedef GenSmartPtr<RefsVc> RefsVcPtr;


/*----------------------------------------------------------------------------------------------
	A field editor for multiple references to non-CmPossibility objects. If the property is
	a sequence, it displays a paragraph of references with a gray bar following each reference.
	If the property is atomic, it just shows the reference.

	When the user clicks in the field, a new window is opened to allow changing the contents.
	This includes a button to the right that allows the user to invoke a chooser to
	make selections. The user cannot edit an individual reference. The cursor will skip over
	any reference. A reference can be deleted by pressing Delete or Backspace on either side
	of the reference. New items can be added by dragging an appropriate item. The insertion
	location depends on where the drop occurs. New items can also be added using a right-click
	menu paste item. Clicking on a reference causes a jump to an appropriate window to display
	that item.

	For jumping to new records to work, you must implement JumpItem and LaunchItem methods
	on your subclass of AfDeSplitChild.

	@h3{Supported functions: (ref = the view for a single reference)}
	@list References shown with Internal Link style
	@list Multiple references shown with gray bar following each ref.
	@list Click to left of ref places IP at left of ref.
	@list Click gray bar places IP to right of gray bar.
	@list Click to right of ref places IP at right of ref.
	@list Arrows skip over ref.
	@list Shift + arrow selects ref.
	@list If ref highlighted, Del/Bksp deletes ref.
	@list ENHANCE KenZ: If ref highlighted, Enter jumps to ref in same window. (currently launches new
		window)
	@list If ref highlighted, Shift+Enter launches ref in new window.
	@list If IP at left of ref, Del deletes ref with bar to right, and puts IP to left of next
		ref. Bksp ignored.
	@list If IP at right of ref, Bksp deletes ref and bar to right, and puts IP to right of
		previous ref. Del ignored.
	@list ENHANCE KenZ: Click on ref jumps to ref in same window. (currently launches new window).
	@list Shift+Click on ref launches ref in new window.
	@list ENHANCE KenZ: If ref highlighted, Ctrl+X copies ref hvo to clipboard and deletes ref.
	@list ENHANCE KenZ: If ref highlighted, Ctrl+C copies ref hvo to clipboard.
	@list ENHANCE KenZ: If ref highlighted, Ctrl+V replaces ref with hvo from clipboard (or error dialog).
	@list ENHANCE KenZ: If IP, Ctrl+V adds hvo from clipboard at IP (or error dialog)
	@list ENHANCE KenZ: If ref highlighted, dropping on ref replaces ref.
	@list ENHANCE KenZ: If ref not highlighted, dropping on ref inserts hvo at closest side. (drop not
		allowed if dragged object can't be dropped here).
	@list ENHANCE KenZ: For now, button brings up instructional dialog with choice to open new window.
	@list ENHANCE KenZ: Drag across ref highlights ref.
	@list ENHANCE KenZ: If it is not too difficult, it would be desirable to support above operations with
		multiple refs.
	@list Moving mouse turns cursor to pointing finger when jump is activated and arrow when
		selection is activated.

	@h3{Hungarian: dfr}
----------------------------------------------------------------------------------------------*/
class AfDeFeRefs : public AfDeFeVw
{
public:
	typedef AfDeFeVw SuperClass;
	friend class AfDeRefsWnd;

	AfDeFeRefs();
	virtual ~AfDeFeRefs();

	void Init(ObjVc * povc, bool fMultiRefs);

	STDMETHOD(SelectionChanged)(IVwRootBox * prootb, IVwSelection * pvwsel);

	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf, IVwRootBox ** pprootb);

	// This field is always editable.
	// @return True indicating it is editable.
	virtual bool IsEditable()
	{
		return true;
	}

	AfDeVwWnd * CreateEditWnd(HWND hwndParent, Rect & rcBounds);
	void PlaceButton();

	/*------------------------------------------------------------------------------------------
		This class implements IEventListener to keep track of the selection. Whenever the
		selection changes, the view	code calls the Notify method. We use this to place the
		chooser button in the right position for multiple lists.
		@h3{Hungarian: adsl}
	------------------------------------------------------------------------------------------*/
	class AfDeSelListener : public IEventListener
	{
	public:
		friend class AfDeFeRefs;

		STDMETHOD(QueryInterface)(REFIID riid, void **ppv);

		/*--------------------------------------------------------------------------------------
			Increment the reference count.
		--------------------------------------------------------------------------------------*/
		STDMETHOD_(ULONG, AddRef)(void)
		{
			return InterlockedIncrement(&m_cref);
		}

		/*--------------------------------------------------------------------------------------
			Decrement the reference count and delete the object if 0.
		--------------------------------------------------------------------------------------*/
		STDMETHOD_(ULONG, Release)(void)
		{
			long cref = InterlockedDecrement(&m_cref);
			if (cref == 0) {
				m_cref = 1;
				delete this;
			}
			return cref;
		}

		STDMETHOD(Notify)(int nArg1, int nArg2);

	protected:
		long m_cref; // COM reference count.
		AfDeFeRefs * m_pdfr; // Pointer to the field editor we are monitoring.
	};
	friend class AfDeFeRefs::AfDeSelListener;
	DEFINE_COM_PTR(AfDeSelListener);

	/*------------------------------------------------------------------------------------------
		This is the button window that allows the user to call up the list chooser. It always
		appears at the right side of the field when a field is active for editing.
		@h3{Hungarian: dfrb}
	------------------------------------------------------------------------------------------*/
	class DfrButton : public AfWnd
	{
	public:
		friend class AfDeFeRefs;
		typedef AfWnd SuperClass;
	protected:
		bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);
		bool GetHelpStrFromPt(Point pt, ITsString ** pptss);

		AfDeFeRefs * m_pdfr; // Pointer to the field editor using the button.
		bool m_fClicked; // Display should show a depressed button.
	};

	virtual bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true);
	virtual bool SaveEdit();
	virtual void EndEdit(bool fForce = false);
	virtual void MoveWnd(const Rect & rcClip);
/*TODO TimP	virtual bool OnMouseMove(uint grfmk, int xp, int yp);*/
	virtual void DropObject(HVO hvo, int clid, POINTL pt);
	void ProcessChooser();
	void PrevItem();
	void NextItem();
	virtual FldReq HasRequiredData();

protected:
	HWND m_hwndButton; // Windows handle for the chooser button.
	RefsVcPtr m_qrvc; // View constructor used to display the references.
	// True for sequence/collection properties, false for atomic properties.
	bool m_fMultiRefs;
	ComSmartPtr<AfDeSelListener> m_qadsl; // Listener to catch selection changes.
};
typedef GenSmartPtr<AfDeFeRefs::DfrButton> DfrButtonPtr;


/*----------------------------------------------------------------------------------------------
	This window is created when the user activates the editor by clicking in it or moving the
	cursor into the field via the keyboard. The window allows the view to be edited (e.g., new
	references can be set, or old ones removed). We need to override AfDeVwWnd to handle the
	floating chooser button and to process type-ahead actions.

	@h3{Hungarian: dtw}
----------------------------------------------------------------------------------------------*/
class AfDeRefsWnd : public AfDeVwWnd
{
public:
	friend class AfDeFeRefs;
	typedef AfDeVwWnd SuperClass;

	virtual bool CmsCharFmt(CmdState & cms);
	virtual bool CmdEditCut(Cmd * pcmd);
	virtual bool CmdEditCopy(Cmd * pcmd);
	virtual bool CmdEditPaste(Cmd * pcmd);
	virtual bool CmsEdit(CmdState & cms);

protected:
	virtual bool OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);
	virtual bool OnCommand(int cid, int nc, HWND hctl);
	virtual bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual void CallMouseUp(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot);

	CMD_MAP_DEC(AfDeRefsWnd); // Command map for processing edit buttons/menus.

};
DEFINE_COM_PTR(AfDeRefsWnd);

#endif // AFDEFE_REFS_INCLUDED
