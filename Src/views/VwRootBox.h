/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwRootBox.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWROOTBOX_INCLUDED
#define VWROOTBOX_INCLUDED

#include "OleStringLiteral.h"

class VwTextStore;
DEFINE_COM_PTR(VwTextStore);

#ifdef WIN32
#undef ENABLE_TSF
#define ENABLE_TSF
#else /* ! WIN32 */
#undef MANAGED_KEYBOARDING
#define MANAGED_KEYBOARDING
#endif

/*----------------------------------------------------------------------------------------------
These are values that may be passed to VwRootBox::OnExtendedChar.
Hungarian: ec
----------------------------------------------------------------------------------------------*/
typedef enum
{
	// "Extended" key character codes.
	kecPageUpKey = VK_PRIOR,
	kecPageDownKey = VK_NEXT,
	kecEndKey = VK_END,
	kecHomeKey = VK_HOME,
	kecLeftArrowKey = VK_LEFT,
	kecUpArrowKey = VK_UP,
	kecRightArrowKey = VK_RIGHT,
	kecDownArrowKey = VK_DOWN,
	kecInsert = VK_INSERT,
	kecDelete = VK_DELETE,
	kecTabKey = VK_TAB,
	kecEnterKey = VK_RETURN,
	kecF7 = VK_F7,
	kecF8 = VK_F8,
} VwExtendedChars;

typedef Vector<VwSelection *> SelVec; // Hungarian vsel

class LayoutPageMethod;
/*----------------------------------------------------------------------------------------------
Class: VwRootBox
Description:
Hungarian: rootb
----------------------------------------------------------------------------------------------*/
class VwRootBox : public IVwRootBox, public IServiceProvider, public VwDivBox
{
	friend class LayoutPageMethod;
	friend class VwSynchronizer; // ::Reconstruct(); // Reconstruct method uses various protected stuff.
	friend class VwParagraphBox; // just for Assert in destructor.
	typedef VwDivBox SuperClass;
	friend class VwLazyBox;
public:
	// Static methods

	// Constructors/destructors/etc.
	VwRootBox(VwPropertyStore *pzvps);
	virtual ~VwRootBox();
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// IVwNotifyChange methods

	STDMETHOD(PropChanged)(HVO hvo, int tag, int ivMin, int cvIns, int cvDel);

	// IVwRootBox methods

	// Initialization
	STDMETHOD(SetSite)(IVwRootSite * pvrs);
	STDMETHOD(SetRootObjects)(HVO * prghvo, IVwViewConstructor ** ppvwvc, int * prgfrag,
		IVwStylesheet * pss, int chvo);
	STDMETHOD(SetRootObject)(HVO hvo, IVwViewConstructor * pvwvc,
		int frag, IVwStylesheet * pss);
	STDMETHOD(SetRootVariant)(VARIANT v, IVwStylesheet * pss,
		IVwViewConstructor * pvwvc, int frag);
	STDMETHOD(SetRootString)(ITsString * ptss, IVwStylesheet * pss,
		IVwViewConstructor * pvwvc, int frag);
	STDMETHOD(GetRootVariant)(VARIANT * pv);
	STDMETHOD(putref_DataAccess)(ISilDataAccess * psda);
	STDMETHOD(get_DataAccess)(ISilDataAccess ** ppsda);
	STDMETHOD(putref_Overlay)(IVwOverlay * pvo);
	STDMETHOD(get_Overlay)(IVwOverlay ** ppvo);

	// Serialization
	STDMETHOD(Serialize)(IStream * pstrm);
	STDMETHOD(Deserialize)(IStream * pstrm);
	STDMETHOD(WriteWpx)(IStream * pstrm);

	// Selections
	STDMETHOD(get_Selection)(IVwSelection** ppsel);
	STDMETHOD(DestroySelection)();
	STDMETHOD(MakeTextSelection)(int ihvoRoot, int cvlsi, VwSelLevInfo * prgvsli,
		int tagTextProp, int cpropPrevious, int ichAnchor, int ichEnd, int ws,
		ComBool fAssocPrev, int ihvoEnd, ITsTextProps * pttpIns,
		ComBool fInstall, IVwSelection ** ppsel);
	STDMETHOD(MakeRangeSelection)(IVwSelection * pselAnchor, IVwSelection * pselEnd,
		ComBool fInstall, IVwSelection ** ppsel);
	STDMETHOD(MakeSimpleSel)(ComBool fInitial, ComBool fEdit, ComBool fRange,
		ComBool fInstall, IVwSelection ** ppsel);
	STDMETHOD(MakeTextSelInObj)(int ihvoRoot,
		int cvsli, VwSelLevInfo * prgvsli, int cvsliEnd, VwSelLevInfo * prgvsliEnd,
		ComBool fInitial, ComBool fEdit, ComBool fRange, ComBool fWholeObj,
		ComBool fInstall, IVwSelection **ppsel);
	STDMETHOD(MakeSelInObj)(int ihvoRoot, int cvsli, VwSelLevInfo * prgvsli, int tag,
		ComBool fInstall, IVwSelection ** ppsel);
	STDMETHOD(MakeSelAt)(int xd, int yd, RECT rcSrc, RECT rcDst, ComBool fInstall,
		IVwSelection ** ppsel);
	STDMETHOD(MakeSelInBox)(IVwSelection * pselInit, ComBool fEndPoint, int iLevel, int iBox,
		ComBool fInitial, ComBool fRange, ComBool fInstall, IVwSelection ** ppsel);
	STDMETHOD(get_IsClickInText)(int xd, int yd, RECT rcSrc1, RECT rcDst1,
		ComBool * pfInText);
	STDMETHOD(get_IsClickInObject)(int xd, int yd, RECT rcSrc1, RECT rcDst1,
		int * podt, ComBool * pfInHotLink);
	STDMETHOD(get_IsClickInOverlayTag)(int xd, int yd, RECT rcSrc1, RECT rcDst1, int * piGuid,
		BSTR * pbstrGuids, RECT * prcTag, RECT * prcAllTags, ComBool * pfOpeningTag,
		ComBool * pfInOverlayTag);

	// Passing window events to box
	STDMETHOD(OnTyping)(IVwGraphics *pvg, BSTR bstrInput, VwShiftStatus ss, int * pwsPending);
	STDMETHOD(DeleteRangeIfComplex)(IVwGraphics * pvg, ComBool * pfWasComplex);
	STDMETHOD(OnChar)(int chw);
	STDMETHOD(OnSysChar)(int chw);
	STDMETHOD(OnExtendedKey)(int chw, VwShiftStatus ss, int nFlags);
	STDMETHOD(FlashInsertionPoint)();
	STDMETHOD(MouseDown)(int xd, int yd, RECT rcSrc1, RECT rcDst1);
	STDMETHOD(MouseDblClk)(int xd, int yd, RECT rcSrc1, RECT rcDst1);
	STDMETHOD(MouseMoveDrag)(int xd, int yd, RECT rcSrc1, RECT rcDst1);
	STDMETHOD(MouseDownExtended)(int xd, int yd, RECT rcSrc1, RECT rcDst1);
	STDMETHOD(MouseUp)(int xd, int yd, RECT rcSrc1, RECT rcDst1);
	STDMETHOD(Activate)(VwSelectionState vss);
	STDMETHOD(get_SelectionState)(VwSelectionState * pvss);

	// Drawing
	STDMETHOD(PrepareToDraw)(IVwGraphics * pvg, RECT rcSrc, RECT rcDst, VwPrepDrawResult * pxpdr);
	STDMETHOD(DrawRoot)(IVwGraphics* pvg, RECT rcSrc1, RECT rcDst1, ComBool fDrawSel);
	STDMETHOD(Layout)(IVwGraphics* pvg, int dxAvailWidth);
	STDMETHOD(get_Height)(int * ptwHeight);
	STDMETHOD(get_Width)(int * ptwWidth);

	// Store and retrieve containing window.
	STDMETHOD(get_Site)(IVwRootSite ** ppvrs);
	STDMETHOD(put_Site)(IVwRootSite * pvrs);

	// Focus change
	STDMETHOD(LoseFocus)(ComBool * pfOK);

	// Printing
	STDMETHOD(InitializePrinting)(IVwPrintContext * pvpc);
	STDMETHOD(GetTotalPrintPages)(IVwPrintContext * pvpc, int *pcPageTotal);
	STDMETHOD(PrintSinglePage)(IVwPrintContext * pvpc, int nPageNo);

	// Misc
	STDMETHOD(Close)();
	STDMETHOD(Reconstruct)();
	STDMETHOD(OnStylesheetChange)();
	STDMETHOD(DrawingErrors)(IVwGraphics * pvg);
	STDMETHOD(get_Stylesheet)(IVwStylesheet ** ppvss);
	STDMETHOD(SetTableColWidths)(VwLength * prgvlen, int cvlen);
	STDMETHOD(IsDirty)(ComBool * pfDirty);
	STDMETHOD(get_XdPos)(int * pxdPos);
	STDMETHOD(GetRootObject)(HVO * phvo,
	IVwViewConstructor ** ppvwvc, int * pfrag, IVwStylesheet ** ppss);
	STDMETHOD(DrawRoot2)(IVwGraphics * pvg, RECT rcSrcRoot1, RECT rcDstRoot1,
		ComBool fDrawSel, int ysTop, int dysHeight);
	STDMETHOD(SetKeyboardForWs)(ILgWritingSystem * pws, BSTR * pbstrActiveKeymanKbd,
		int * pnActiveLangId, int * phklActive, ComBool * pfSelectLangPending);
	STDMETHOD(get_MaxParasToScan)(int * pcParas);
	STDMETHOD(put_MaxParasToScan)(int cParas);
	STDMETHOD(DoSpellCheckStep)(ComBool * pfComplete);
	STDMETHOD(IsSpellCheckComplete)(ComBool * pfComplete);
	STDMETHOD(get_IsCompositionInProgress)(ComBool * pfInProgress);
	STDMETHOD(get_IsPropChangedInProgress)(ComBool * pfInProgress);
	STDMETHOD(RestartSpellChecking)();

	// IServiceProvider methods
	STDMETHOD(QueryService)(REFGUID guidService, REFIID riid, void ** ppv);

	STDMETHOD(get_Synchronizer)(IVwSynchronizer ** ppsync);

	// Synchronization
	void SetSynchronizer(VwSynchronizer * psync);
	VwSynchronizer * GetSynchronizer();
	void SetActualTopToTop(HVO hvoObj, int dypActualTopToTop);
	void SetActualTopToTopAfter(HVO hvoObj, int dypActualTopToTop);
	int NaturalTopToTop(HVO hvoObj);
	int NaturalTopToTopAfter(HVO hvoObj);
	VwBox * ExpandItemsNoLayout(HVO hvoContext, int tag, int iprop, int ihvoMin, int ihvoLim,
		Rect * prcLazyBoxOld, VwBox ** ppboxFirstLayout, VwBox ** ppboxLimLayout,
		VwDivBox ** ppdboxContainer);
	void AdjustBoxPositions(Rect rcRootOld, VwBox * pboxFirstLayout, VwBox * pboxLimLayout,
		Rect rcThisOld, VwDivBox * pdboxContainer, bool * pfForcedScroll, VwSynchronizer * psync,
		bool fDoLayoutForExpandedItems);
	virtual void Reconstruct(bool fCheckForSync);
#ifdef DEBUG
	void AssertNotifiersValid();
#endif
	// Other public methods
	void SetDirty(bool fDirty);
	void InvalidateRect (Rect * vwrect);
	virtual VwRootBox * Root()
	{
		return this;
	}

	void GetResInfo(VwPrintInfo & vpi, Rect & rcSrc, Rect & rcDst);
	void CreatePrintInfo(IVwPrintContext * pvpc, VwPrintInfo & vpi);
	void DeleteNotifiersFor(VwBox * pbox, int chvoLevel, NotifierVec & vpanoteDel);
	void DeleteNotifierVec(NotifierVec & vpanote);
	void FixSelections(VwBox * pbox, VwBox * pboxReplacement = NULL);
	void AddNotifier(VwAbstractNotifier * panote);
	void AddNotifiers(NotifierVec * pvpanote);
	void GetGraphics(IVwGraphics ** ppvg, Rect *prcSrcRoot, Rect *prcDstRoot);
	void ReleaseGraphics(IVwGraphics * pvg);
	void RelayoutRoot(IVwGraphics * pvg, FixupMap * pfixmap, int dxpAvailOnLine = -1,
		BoxSet * pboxsetDeleted = NULL);
	virtual bool RelayoutCore(IVwGraphics * pvg, int dxpAvailWidth, VwRootBox * prootb,
			FixupMap * pfixmap, int dxpAvailOnLine, BoxIntMultiMap * pmmbi,
			BoxSet * pboxsetDeleted);
	virtual int AvailWidthForChild(int dpiX, VwBox * pboxChild);

	void ChangeNotifierKey(VwAbstractNotifier * panote, VwBox * pboxNewKey);

	void SetSelection(VwSelection * pvwsel, bool fUpdateRootSite = true);
	void ShowSelection();

	void GetNotifierMap(NotifierMap ** ppmmboxqnote, ObjNoteMap ** ppmmhvoqnote = NULL);
	void BuildNotifierMap();
	void ExtractNotifiers(NotifierVec * pvpanote);
	void DeleteNotifier(VwAbstractNotifier * panote);

	VwSelection * Selection()
	{
		return m_qvwsel;
	}

	// Return style sheet: no ref count!
	IVwStylesheet * Stylesheet()
	{
		return m_qss;
	}

	// Directly set the property store, which the root box missed out on through
	// being created with CreateCom. Called only from VwEnv::Initialize.
	void _SetPropStore(VwPropertyStore * pzvps)
	{
		m_qzvps = pzvps;
	}

	ISilDataAccess * GetDataAccess()
	{
		return m_qsda;
	}

	IVwOverlay * Overlay()
	{
		return m_qvo;
	}
	void NotifySelChange(VwSelChangeType nHow, bool fUpdateRootSite = true);

	// This calls Layout with the correct parameters. It also notifies the root site of
	// any size changes in case it needs to update anything.
	void LayoutFull();

	IVwRootSite * Site()
	{
		return m_qvrs;
	}

	VwSelectionState SelectionState()
	{
		return m_vss;
	}

	void RegisterSelection(VwSelection * psel);
	void UnregisterSelection(VwSelection * psel);

	// Record an error in generating a segment.
	void SetSegmentError(HRESULT hr, const wchar * errorMessage)
	{
		m_hrSegmentError = hr;
		m_stuSegmentError.Assign(errorMessage);
	}
	virtual OLECHAR * Name()
	{
		if (m_stuAccessibleName.Length() == 0)
		{
			static OleStringLiteral name(L"Root");
			return name;
		}
		else return const_cast<OLECHAR *>(m_stuAccessibleName.Chars());
	}
	SelVec & ActiveSelections()
	{
		return m_vselInUse;
	}

	void HandleActivate(VwSelectionState vss, bool fSetFocus = false);

#ifdef ENABLE_TSF
	VwTextStore * TextStore() {return m_qtxs;}
#elif defined(MANAGED_KEYBOARDING)
	IViewInputMgr * InputManager() { return m_qvim; }

	void ClearSelectedAnchorPointerTo(VwParagraphBox * pvpbox)
	{
		if (m_pvpboxLastSelectedAnchor == pvpbox)
			m_pvpboxLastSelectedAnchor = NULL;
	}
#endif /* ENABLE_TSF */

	void MaximizeLaziness(VwBox * pboxMinKeep = NULL, VwBox * pboxLimKeep = NULL);
	VwNotifier * NotifierWithKeyAndParent(VwBox * pbox, VwNotifier * pnoteParent);
	void ShowSelectionAfterEdit();

	// The following methods are public only so we can test; don't use them otherwise.
	bool PrivateIsConstructed()
	{
		return m_fConstructed;
	}
	VwNotifier * NotifierForSliArray(int ihvoRoot, int cvsli,
		VwSelLevInfo * prgvsli);
	virtual void SetAccessibleName(BSTR bstrName);
	virtual VwEnv * MakeEnv();

	void Lock() {m_fLocked = true;}
	void Unlock();

protected:
	// Member variables
	long m_cref;
	IVwRootSitePtr m_qvrs;
	// Vector of notifiers that have been added but not yet put in NotifierMap
	// During certain regenerate operations, it is important to keep new ones
	// separate from old until BuildNotifierMap is called.
	NotifierVec m_vpanote;
	// Map from boxes to notifiers. The box in question is the "first covering
	// box" for the notifier. That means all the boxes included in the notifier
	// are either inside that box, or in the chain of boxes that follow it.
	// It is a multimap: several notifiers may share the same first covering box.
	NotifierMap m_mmboxqnote;
	// Parallel map, containing the same notifiers, from object cookie to notifier.
	ObjNoteMap m_mmhvoqnote;

	// The active selection in the pane, if any.
	VwSelectionPtr m_qvwsel;
	// Any other selections that have been created and not yet destroyed involving
	// this root box. Note that we do NOT keep a reference count on them; rather,
	// they remove themselves from the vector when destroyed.
	SelVec m_vselInUse;

	// Stylesheet for view as whole.
	IVwStylesheetPtr m_qss;

	typedef ComVector<IVwViewConstructor> VwVcVec;

	// The top-level view constructors used for top-level objects
	VwVcVec m_vqvwvc;

	// Top-level objects we are displaying
	HvoVec m_vhvo;
	// Fragment identifier for each of them
	IntVec m_vfrag;
	// number of objects and frag ids
	int m_chvoRoot;

	bool m_fConstructed; // true when we have called Construct() successfully.

	ISilDataAccessPtr m_qsda; // data access object, for getting and setting properties

	IVwOverlayPtr m_qvo; // controls overlay/tagging behavior for all text

	// True when a single-click created a new insertion point, or a double-click created a new
	// selection, but don't yet have a mouse-up.
	bool m_fNewSelection;
	// True when the mouse is being dragged with the left button down.
	bool m_fInDrag;

	bool m_fDirty;

	int m_xdPos;		// horizontal position for moving to the next (or previous) field.

	VwSelectionState m_vss; // Current state: vssDisabled, vssOutOfFocus, vssEnabled.

	HRESULT m_hrSegmentError;	// did an error occur in generating a segment?
	StrUni m_stuSegmentError;	// Error message for m_hrSegmentError

	VwSynchronizerPtr m_qsync; // If not null use this to synchronize object display heights.
#ifdef ENABLE_TSF
	VwTextStorePtr m_qtxs;
#elif defined(MANAGED_KEYBOARDING)
	// last selected paragraph box. See comment in VwRootBox::NotifySelChange.
	VwParagraphBox * m_pvpboxLastSelectedAnchor;

	IViewInputMgrPtr m_qvim;
#endif /*ENABLE_TSF*/

	// The top of rcDstRoot the last time DrawRoot was called.
	// When it changes, we try to increase laziness.
	int m_ydTopLastDraw;

	Point m_ptDpiSrc; // x and y resolutions of most recent Layout.

	StrUni m_stuAccessibleName;

	int m_cMaxParasToScan;

	// True while the view is executing a PropChanged or expanding lazy items (or doing the layout resulting from expanding lazy items)
	// Note that this is accessed by VwLazyBox.ExpandItems to make sure it is set during expansion.
	bool m_fIsPropChangedInProgress;

	// The root box is locked when it is in a state where certain operations
	// (notably spell check steps and painting) cannot safely take place, such as
	// during a PropChanged which inserts a temporary box in place of a real one
	// in the root's box tree.
	bool m_fLocked;
	// While the view is locked, if we get paint messages, we must save the
	// invalid areas, and invalidate them when no longer locked.
	Vector<Rect> m_vrectSkippedPaints;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
	void Construct(IVwGraphics * pvg, int dxAvailWidth);

	// Protected default constructor does nothing.
	// After creating with CreateCom, must set everything up from Init
	VwRootBox();
	HRESULT MakeSimpleSelAt(VwBox * pboxStart, int itssStart,
		ComBool fInitial, ComBool fEdit, ComBool fRange,
		ComBool fInstall, IVwSelection ** ppsel, bool fContinueToParents = true);
	void PrintHeaders(IVwPrintContext * pvpc, ISilDataAccess * psda,
		VwPrintInfo * pvpi, bool fFirst);
	void PrintHeader(VwPrintInfo * pvpi, ISilDataAccess * psda,
		ITsString * ptss, int ipos, int dxsAvailWidth);
	void ProcessHeaderSpecials(ITsString *ptss, ITsString ** pptssRet, int nPageNo,
		int nPageTotal);
	void ClearNotifiers();
	void WriteWpxBoxes(IStream * pstrm, VwBox * pbox);
	VwBox * GetBoxDisplaying(HVO hvoObj);
	// Do nothing, FixSync is only relevant for child boxes.
	virtual void FixSync(VwSynchronizer *psync, VwRootBox * prootb){}
	VwNotifier * NotifierForObjPropIndex(HVO hvoContext, int tag,
		int iprop);
	void SetImeWindowLocation(HKL hkl);
	VwBox * FindClosestBox(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
		Rect * prcSrc, Rect * prcDst);
	bool EnsureConstructed(bool fDoLayout = false);
	// next paragraph box to spell-check.
	VwParagraphBox * m_pvpboxNextSpellCheck;
	bool m_fCompletedSpellCheck; // true when we reach the end.
	HashMapStrUni<enchant::Dict *> m_hmDict;
	void FindBreak(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst, int ysStart, int * pysEnd);
	bool OnMouseEvent(int xd, int yd, RECT rcSrc, RECT rcDst, VwMouseEvent me);

public:
	bool FixSelectionsForStringReplacement(VwTxtSrc * psrcModify, int itssMin, int itssLim,
			VwTxtSrc * psrcRep);
	void ContractLazyItems(HVO hvoContext, int tag,
		int iprop, int ihvoMin, int ihvoLim);
	Point DpiSrc() { return m_ptDpiSrc; }
	virtual void SendPageNotifications(VwBox * pbox) {}; // See VwLayoutStream override.
	void ResetSpellCheck();
	virtual enchant::Dict * GetDictionary(const OLECHAR * pszId);
};
DEFINE_COM_PTR(VwRootBox);

/*----------------------------------------------------------------------------------------------
This class is useful when you need to get a VwGraphics and root src/dst transformation from
the root box GetGraphics method. It guarantees to call the necessary ReleaseGraphics when
it goes out of scope.
@h3{Hungarian: hg}
----------------------------------------------------------------------------------------------*/
class HoldGraphics
{
public:
	HoldGraphics(VwRootBox * prootb)
	{
		prootb->GetGraphics(&m_qvg, &m_rcSrcRoot, &m_rcDstRoot);
		m_prootb = prootb;
	}
	~HoldGraphics()
	{
		if (m_prootb)
		{
			m_prootb->ReleaseGraphics(m_qvg);
		}
	}
	IVwGraphicsPtr m_qvg;
	Rect m_rcSrcRoot;
	Rect m_rcDstRoot;

protected:
	VwRootBox * m_prootb;
};

/*----------------------------------------------------------------------------------------------
This class is useful when you need to get a VwGraphics and root src/dst transformation at a
particular point in destination coords. It guarantees to call the necessary ReleaseGraphics when
it goes out of scope.
@h3{Hungarian: hg}
----------------------------------------------------------------------------------------------*/
class HoldGraphicsAtDst
{
public:
	HoldGraphicsAtDst(VwRootBox * prootb, Point pt)
	{
		CheckHr(prootb->Site()->get_ScreenGraphics(prootb, &m_qvg));
		m_prootb = prootb;
		CheckHr(prootb->Site()->GetTransformAtDst(prootb, pt, &m_rcSrcRoot, &m_rcDstRoot));
	}
	~HoldGraphicsAtDst()
	{
		if (m_prootb)
		{
			m_prootb->ReleaseGraphics(m_qvg);
		}
	}
	IVwGraphicsPtr m_qvg;
	Rect m_rcSrcRoot;
	Rect m_rcDstRoot;

protected:
	VwRootBox * m_prootb;
};

/*----------------------------------------------------------------------------------------------
This class is useful when you need to get a VwGraphics and root src/dst transformation at a
particular point in destination coords. It guarantees to call the necessary ReleaseGraphics when
it goes out of scope.
@h3{Hungarian: hg}
----------------------------------------------------------------------------------------------*/
class HoldGraphicsAtSrc
{
public:
	HoldGraphicsAtSrc(VwRootBox * prootb, Point pt)
	{
		CheckHr(prootb->Site()->get_ScreenGraphics(prootb, &m_qvg));
		m_prootb = prootb;
		CheckHr(prootb->Site()->GetTransformAtSrc(prootb, pt, &m_rcSrcRoot, &m_rcDstRoot));
	}
	~HoldGraphicsAtSrc()
	{
		if (m_prootb)
		{
			m_prootb->ReleaseGraphics(m_qvg);
		}
	}
	IVwGraphicsPtr m_qvg;
	Rect m_rcSrcRoot;
	Rect m_rcDstRoot;

protected:
	VwRootBox * m_prootb;
};
/*----------------------------------------------------------------------------------------------
This class is useful when you need to get a layout resolution VwGraphics from
the root box GetLayoutGraphics method. It guarantees to call the necessary ReleaseGraphics when
it goes out of scope.
@h3{Hungarian: hg}
----------------------------------------------------------------------------------------------*/
class HoldLayoutGraphics
{
public:
	HoldLayoutGraphics(VwRootBox * prootb)
	{
		CheckHr(prootb->Site()->get_LayoutGraphics(prootb, &m_qvg));
		m_prootb = prootb;
	}
	~HoldLayoutGraphics()
	{
		if (m_prootb)
		{
			m_prootb->ReleaseGraphics(m_qvg);
		}
	}
	IVwGraphicsPtr m_qvg;

protected:
	VwRootBox * m_prootb;
};

/*----------------------------------------------------------------------------------------------
This class is useful when you need to get a screen resolution VwGraphics from
the root box GetScreenGraphics method. It guarantees to call the necessary ReleaseGraphics when
it goes out of scope.
@h3{Hungarian: hg}
----------------------------------------------------------------------------------------------*/
class HoldScreenGraphics
{
public:
	HoldScreenGraphics(VwRootBox * prootb)
	{
		CheckHr(prootb->Site()->get_ScreenGraphics(prootb, &m_qvg));
		m_prootb = prootb;
	}
	~HoldScreenGraphics()
	{
		if (m_prootb)
		{
			m_prootb->ReleaseGraphics(m_qvg);
		}
	}
	IVwGraphicsPtr m_qvg;

protected:
	VwRootBox * m_prootb;
};

#if WIN32 // In Linux we use a managed implementation
class VwDrawRootBuffered : IVwDrawRootBuffered
{
protected:
	VwDrawRootBuffered();
public:
	~VwDrawRootBuffered();
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}
	STDMETHOD(DrawTheRoot)(IVwRootBox * prootb, HDC hdc, RECT rcpDraw, COLORREF bkclr,
		ComBool fDrawSel, IVwRootSite * pvrs);
	STDMETHOD(ReDrawLastDraw)(HDC hdc, RECT rcpDraw);
	STDMETHOD(DrawTheRootAt)(IVwRootBox * prootb, HDC hdc, RECT rcpDraw,
		COLORREF bkclr, ComBool fDrawSel, IVwGraphics * pvg, RECT rcSrc, RECT rcDst, int ysTop,
	int dysHeight);
	STDMETHOD(DrawTheRootRotated)(IVwRootBox * prootb, HDC hdc, RECT rcpDraw,
		COLORREF bkclr, ComBool fDrawSel, IVwRootSite * pvrs, int nHow);
protected:
	long m_cref;
	HDC m_hdcMem;
};
#endif // WIN32

#endif  //VWROOTBOX_INCLUDED
