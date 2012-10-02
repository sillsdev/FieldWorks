/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfDeFeTags.h
Responsibility: Ken Zook
Last reviewed: never

These classes all work together to provide a field editor for CmPossibility references.
Defines
	TagsVc: The view constructor that determines how the tags view appears.
	AfDeFeTags: A field editor to display one or more lists of CmPossibility references.
	AfDeFeTags::AfDeSelListener: Keeps track of the selection.
	AfDeFeTags::DftButton: Displays and processes the button that calls up the list chooser.
	AfDeTagsWnd: A window allowing the tags to be edited.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef AFDEFE_TAGS_INCLUDED
#define AFDEFE_TAGS_INCLUDED 1

enum
{
	// These are dummy properties used to store information in VwCacheDa.
	kflidPssIds, // Stores a list of the CmPossibility ids.
	kflidPssName, // Stores a list of the corresponding name/abbreviation.
	kflidPsslIds, // Stores a list of the corresponding CmPossibilityList ids.
	kflidPsslAbbr, // Stores a list of the corresponding list abbreviations.
};

// Enumeration to provide fragment identifiers for the view constructor.
enum
{
	kfrMultiList, // Top level for table of lists.
	kfrSingleList, // Top level to process a single list.
	kfrList, // List of items as one paragraph with separator boxes.
	kfrListInTable, // Same as kfrList, but in a table.
	kfrListVec, // For DisplayVec, the vector of items.
	kfrItem, // One item.
};


/*----------------------------------------------------------------------------------------------
	The view constructor that determines how the tags view appears. If the possibilities come
	from a single list, they are simply displayed in one paragraph. If the possibilities come
	from more than one list, the items are sorted by list, and the view uses a table to display
	each list as a row in the table. The left cell in each row shows the list abbreviation,
	and the right cell shows the paragraph of possibility names/abbreviations.

	@h3{Hungarian: tgvc}
----------------------------------------------------------------------------------------------*/
class TagsVc : public VwBaseVc
{
public:
	typedef VwBaseVc SuperClass;

	TagsVc(LPCOLESTR pszSty, COLORREF clrBkg = -1);
	~TagsVc();

	STDMETHOD(Display)(IVwEnv * pvwenv, HVO hvo, int frag);
	STDMETHOD(DisplayVec)(IVwEnv * pvwenv, HVO hvo, int tag, int frag);

	void SetWritingSystemInfo(int ws, ILgWritingSystemFactory * pwsf);

protected:
	StrUni m_stuSty; // Text properties.
	COLORREF m_clrBkg; // Color for paragraph background.

	int m_ws;
	ILgWritingSystemFactoryPtr m_qwsf;
	ComBool m_fRTL;
};
typedef GenSmartPtr<TagsVc> TagsVcPtr;


/*----------------------------------------------------------------------------------------------
	A field editor for multiple references to CmPossibilities. The references are displayed
	in a paragraph using the desired combination of name and abbreviation of the possibilities.
	Multiple references are separated by a gray separater bar. There is always one additional
	bar at the end allowing the user to add a new item.

	If the possibilities come from a single list, they are simply displayed in one paragraph.
	If the possibilities come from more than one list, the items are sorted by list, and the
	view uses a table to display each list as a row in the table. The left cell in each row
	shows the list abbreviation, and the right cell shows the paragraph of possibility
	names/abbreviations.

	When the user clicks in the field, a new window is opened to allow editing the contents.
	This includes a button to the right that allows the user to invoke a list chooser to
	make selections. A user can also select a new item by typing the name (and/or abbreviation)
	and the editor will find and display the next item that matches what has been typed so far.

	@h3{Hungarian: dft}
----------------------------------------------------------------------------------------------*/
class AfDeFeTags : public AfDeFeVw, public PossListNotify
{
public:
	friend class AfDeTagsWnd;
	typedef AfDeFeVw SuperClass;

	AfDeFeTags();
	virtual ~AfDeFeTags();

	virtual void Init(Vector<HVO> & vpssl, bool fMultiList, bool fHier,
		PossNameType pnt = kpntName);

	STDMETHOD(SelectionChanged)(IVwRootBox * prootb, IVwSelection * pvwsel);

	virtual void MakeRoot(IVwGraphics * pvg, ILgWritingSystemFactory * pwsf,
		IVwRootBox ** pprootb);

	// This field is always editable.
	// @return True indicating it is editable.
	virtual bool IsEditable()
	{
		return true;
	}

	void SaveCursor(int iFrom = 0);
	void FindAndSetPss(int ihvoPss, int ihvoPssl, uint nChar, int * pichAnchor, int * pichEnd);
	AfDeVwWnd * CreateEditWnd(HWND hwndParent, Rect & rcBounds);
	void StoreItemsByList();
	void PlaceButton();
	virtual FldReq HasRequiredData();

	/*------------------------------------------------------------------------------------------
		This class implements IEventListener to keep track of the selection. Whenever the
		selection changes, the view	code calls the Notify method. We use this to place the
		chooser button in the right position for multiple lists.
		@h3{Hungarian: adsl}
	------------------------------------------------------------------------------------------*/
	class AfDeSelListener : public IEventListener
	{
	public:
		friend class AfDeFeTags;

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
		AfDeFeTags * m_pdft; // Pointer to the field editor we are monitoring.
	};
	friend class AfDeFeTags::AfDeSelListener;
	DEFINE_COM_PTR(AfDeSelListener);

	/*------------------------------------------------------------------------------------------
		This is the button window that allows the user to call up the list chooser. It always
		appears at the right side of the field when a field is active for editing.
		@h3{Hungarian: dftb}
	------------------------------------------------------------------------------------------*/
	class DftButton : public AfWnd
	{
	public:
		friend class AfDeFeTags;
		typedef AfWnd SuperClass;
	protected:
		bool OnDrawThisItem(DRAWITEMSTRUCT * pdis);
		bool GetHelpStrFromPt(Point pt, ITsString ** pptss);

		AfDeFeTags * m_pdft; // Pointer to the field editor using the button.
		bool m_fClicked; // Display should show a depressed button.
	};

	virtual bool IsDirty();
	virtual bool BeginEdit(HWND hwnd, Rect & rc, int dxpCursor = 0, bool fTopCursor = true);
	virtual bool SaveEdit();
	virtual void EndEdit(bool fForce = false);
	virtual void MoveWnd(const Rect & rcClip);
	virtual void UpdateField();
	virtual void SaveCursorInfo();
	virtual void RestoreCursor(Vector<HVO> & vhvo, Vector<int> & vflid, int ichCur);
	void FillDisplayCache();
	void ProcessChooser();
	void AddNames(HVO hvoPssl);
	void PrevItem();
	void NextItem();
	void GetCurrentItem(int * pipss, int * pipssl);
	inline void SetSuggestedTextFlag(bool flag)
	{
		m_fSuggestedText = flag;
	};
	virtual void ListChanged(int nAction, HVO hvoPssl, HVO hvoSrc, HVO hvoDst, int ipssSrc,
		int ipssDst);
	virtual void OnReleasePtr();
	virtual void ChooserApplied(PossChsrDlg * pplc);

protected:

	HWND m_hwndButton; // Windows handle for the chooser button.
	TagsVcPtr m_qtgvc; // View constructor used to display the tags.
	// A temporary data cache used just within this property that allows us to create
	// dummy objects for displaying the possibilities in a table.
	IVwCacheDaPtr m_qvcd;
	bool m_fHier; // True if names are supposed to show hierarchy.
	HVO m_hvoPssl; // Id of the current possibility list.
	Vector<HVO> m_vpssl; // Vector of lists that can be used for this property.
	// True for multiple lists, even though only one may be active, false for single list.
	bool m_fMultiList;
	ComSmartPtr<AfDeSelListener> m_qadsl; // Listener to catch selection changes.
	int m_ichCurs; // The position of the cursor as an index into the current item.
	PossNameType m_pnt; // Determines whether we show name, abbr, or both for poss items.
	bool m_fSuggestedText; // Is text currently display a "type-ahead" string?  True means yes.
	bool m_fIgnoreNext_3;
	bool m_fTypeAhead; // True if suggested text is to be displayed.
	HVO m_kidNew; // Stores next "available" object handle (HVO) for ?
	// This flag is used to skip notifications from PossListInfo when we are in the
	// middle of adding new items. Without this, we load the temporary cache with the
	// old list while we are in the update process, and we end up trying to access beyond
	// the end of the vector.
	bool m_fSaving;
	Vector<HVO> m_vpssOld; // Vector of items for this list before calling up the chooser.
};
typedef GenSmartPtr<AfDeFeTags::DftButton> DftButtonPtr;


/*----------------------------------------------------------------------------------------------
	This window is created when the user activates the editor by clicking in it or moving the
	cursor into the field via the keyboard. The window allows the view to be edited (e.g., new
	references can be set, or old ones removed). We need to override AfDeVwWnd to handle the
	floating chooser button and to process type-ahead actions.

	@h3{Hungarian: dtw}
----------------------------------------------------------------------------------------------*/
class AfDeTagsWnd : public AfDeVwWnd
{
	friend class AfDeFeTags;
	typedef AfDeVwWnd SuperClass;

public:
	virtual bool CmsCharFmt(CmdState & cms);
	virtual bool CmdEdit(Cmd * pcmd);
	virtual bool CmdEditUndo(Cmd * pcmd);
	virtual bool CmdEditRedo(Cmd * pcmd);
	virtual bool CmsEditUndo(CmdState & cms);
	virtual bool CmsEditRedo(CmdState & cms);

protected:
	virtual void OnChar(UINT nChar, UINT nRepCnt, UINT nFlags);
	virtual void CallMouseUp(int xp, int yp, RECT rcSrcRoot, RECT rcDstRoot);
	bool OnCommand(int cid, int nc, HWND hctl);
	bool FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet);
	virtual bool OnCut();

	CMD_MAP_DEC(AfDeTagsWnd); // Command map for processing edit buttons/menus.

};
DEFINE_COM_PTR(AfDeTagsWnd);

#endif // AFDEFE_TAGS_INCLUDED
