/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwSelection.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	The various kinds of selection (currently only text) that can be made in views.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWSELECTION_INCLUDED
#define VWSELECTION_INCLUDED

// forward declarations
class LazinessIncreaser;
namespace TestViews
{
	class TestVwPattern;
	class TestVwTextSelection;
};

typedef enum
{
	ksctNoChange = -1, // Selection did not change
	ksctSamePara = 1, // Selection changed but stayed in same paragraph
	ksctDiffPara = 2, // Selection moved to a different paragraph
	ksctUnknown = 3, // Selection changed, it is not known whether it moved paragraph...maybe no previous sel.
	ksctDeleted = 4, // Selection removed altogether, there is now no current selection.
} VwSelChangeType;

class VwRootBox;
/*----------------------------------------------------------------------------------------------
Class: VwSelection
Description: This is the abstract class that captures the common functionality of selections.
Most of its methods are stubs. A few MUST be implemented by any subclass.
Hungarian: vwsel
----------------------------------------------------------------------------------------------*/
class VwSelection :
#if WIN32
	public SilDispatchImpl<IVwSelection, &IID_IVwSelection, &LIBID_Views>
#else
	public IVwSelection
#endif
{
	friend class VwRootBox;
	friend class VwParagraphBox;

public:
	// Static methods

	// Constructors/destructors/etc.
	VwSelection();
	virtual ~VwSelection();

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
	// IVwSelection methods
	STDMETHOD(get_IsRange)(ComBool* pfRet);
	STDMETHOD(GetSelectionProps)(int cttpMax, ITsTextProps ** prgpttp,
		IVwPropertyStore ** prgpvps, int * pcttp);
	STDMETHOD(GetHardAndSoftCharProps)(int cttpMax, ITsTextProps ** prgpttpSel,
		IVwPropertyStore ** prgpvpsSoft, int * pcttp);
	STDMETHOD(GetParaProps)(int cttpMax, IVwPropertyStore ** prgpvps, int * pcttp);
	STDMETHOD(GetHardAndSoftParaProps)(int cttpMax, ITsTextProps ** prgpttpPara,
		ITsTextProps ** prgpttpHard, IVwPropertyStore ** prgpvpsSoft, int * pcttp);
	STDMETHOD(SetSelectionProps)(int cttp, ITsTextProps ** prgpttp);
	STDMETHOD(TextSelInfo)(ComBool fEndPoint, ITsString ** pptss, int * pich,
		ComBool * pfAssocPrev, HVO * phvoObj, PropTag * ptag, int * pws);
	STDMETHOD(CLevels)(ComBool fEndPoint, int * pclev);
	STDMETHOD(PropInfo)(ComBool fEndPoint, int ilev, HVO * phvoObj, PropTag * ptag, int * pihvo,
		int * pcpropPrevious, IVwPropertyStore ** ppvps);
	STDMETHOD(CompleteEdits)(VwChangeInfo * pci, ComBool * pfOk);
	STDMETHOD(ExtendToStringBoundaries)();
	STDMETHOD(AllTextSelInfo)(int * pihvoRoot, int cvlsi, VwSelLevInfo * prgvsli,
		PropTag * ptagTextProp, int * pcpropPrevious, int * pichAnchor, int * pichEnd,
		int * pws, ComBool * pfAssocPrev, int * pihvoEnd, ITsTextProps ** ppttpIns);
	STDMETHOD(AllSelEndInfo)(ComBool fEndPoint, int * pihvoRoot,
		int cvlsi, VwSelLevInfo * prgvsli, PropTag * ptagTextProp, int * pcpropPrevious,
		int * pich, int * pws, ComBool * pfAssocPrev, ITsTextProps ** ppttpIns);
	STDMETHOD(get_EndBeforeAnchor)(ComBool* pfRet);
	STDMETHOD(Location)(IVwGraphics * pvg, RECT rcSrc, RECT rcDst, RECT * prdPrimary,
		RECT * prdSecondary, ComBool * pfSplit, ComBool * pfEndBeforeAnchor);
	STDMETHOD(GetParaLocation)(RECT * prdLoc);
	STDMETHOD(ReplaceWithTsString)(ITsString * ptss);
	STDMETHOD(GetSelectionString)(ITsString ** pptss, BSTR bstr);
	STDMETHOD(GetFirstParaString)(ITsString ** pptss, BSTR bstr, ComBool * pfGotItAll);
	STDMETHOD(SetIPLocation)(ComBool fTopLine, int xdPos);
	STDMETHOD(get_CanFormatPara)(ComBool* pfRet);
	STDMETHOD(get_CanFormatChar)(ComBool* pfRet);
	STDMETHOD(get_CanFormatOverlay)(ComBool* pfRet);
	STDMETHOD(Install)();
	STDMETHOD(get_Follows)(IVwSelection * psel, ComBool * pfFollows);
	STDMETHOD(get_IsValid)(ComBool * pfValid);
	STDMETHOD(get_AssocPrev)(ComBool * pfValue);
	STDMETHOD(put_AssocPrev)(ComBool fValue);
	STDMETHOD(get_ParagraphOffset)(ComBool fEndPoint, int * pich);
	STDMETHOD(get_SelType)(VwSelType * pstType);
	STDMETHOD(get_RootBox)(IVwRootBox ** pprootb);
	STDMETHOD(GrowToWord)(IVwSelection ** ppsel);
	STDMETHOD(EndPoint)(ComBool fEndPoint, IVwSelection ** ppsel);
	STDMETHOD(SetTypingProps)(ITsTextProps * pttp);

	STDMETHOD(get_BoxDepth)(ComBool fEndPoint, int * pcDepth);
	STDMETHOD(get_BoxIndex)(ComBool fEndPoint, int iLevel, int * piAtLevel);
	STDMETHOD(get_BoxCount)(ComBool fEndPoint, int iLevel, int * pcAtLevel);
	STDMETHOD(get_BoxType)(ComBool fEndPoint, int iLevel, VwBoxType * pvbt);
	STDMETHOD(get_IsEditable)(ComBool * pfEditable);
	STDMETHOD(get_IsEnabled)(ComBool * pfEnabled);

	void MarkInvalid() {m_qrootb.Clear();}

	virtual VwRootBox * RootBox();
	bool IsValid() { return (bool)m_qrootb; }

	// Return the selection state of the selection.
	virtual VwSelectionState SelectionState() = 0;

	// Member variable access.

	bool Showing()
	{
		return m_fShowing;
	}

	// virtual methods typically overridden by concrete subclasses

	// Return true if the selection needs to be deleted when pbox is deleted or
	// replaced by the given box. If possible patch things up. It is not necessary to
	// handle the possibility that the selection points at a box inside pbox.
	virtual bool RuinedByDeleting(VwBox * pbox, VwBox * pboxReplacement=0)
	{
		return false;
	}
	// See description in IDH of VwRootBox.OnTyping.
	virtual void OnTyping(IVwGraphics *pvg, const wchar * pchInput, int cchInput, VwShiftStatus ss, int * pwsPending)
	{
		return; // by default ignore happily
	}

	// Focus Handling
	virtual void LoseFocus(IVwSelection * pvwselNew, ComBool * pfOk)
	{
		return; // By default ignore happily.
	}

	// Move selection appropriately for arrow keys, if possible
	virtual bool LeftArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping = false)
	{
		return true;
	}
	virtual bool RightArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping = false)
	{
		return true;
	}
	virtual bool UpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, int * pxdPos)
	{
		return true;
	}
	virtual bool DownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, int * pxdPos)
	{
		return true;
	}
	virtual void EndKey(IVwGraphics * pvg, bool fLogical)
	{
	}
	virtual void HomeKey(IVwGraphics * pvg, bool fLogical)
	{
	}

	virtual void ShiftLeftArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping = false)
	{
	}
	virtual void ShiftRightArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping = false)
	{
	}
	virtual void ShiftUpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
	{
	}
	virtual void ShiftDownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
	{
	}
	virtual void ShiftEndKey(IVwGraphics * pvg, bool fLogical)
	{
	}
	virtual void ShiftHomeKey(IVwGraphics * pvg, bool fLogical)
	{
	}

	virtual bool ControlLeftArrow(IVwGraphics * pvg, bool fLogical)
	{
		return true;
	}
	virtual bool ControlRightArrow(IVwGraphics * pvg, bool fLogical)
	{
		return true;
	}
	virtual bool ControlUpArrow(IVwGraphics * pvg)
	{
		return true;
	}
	virtual bool ControlDownArrow(IVwGraphics * pvg)
	{
		return true;
	}
	virtual void ControlEndKey(IVwGraphics * pvg, bool fLogical)
	{
	}
	virtual void ControlHomeKey(IVwGraphics * pvg, bool fLogical)
	{
	}

	virtual void ControlShiftLeftArrow(IVwGraphics * pvg, bool fLogical)
	{
	}
	virtual void ControlShiftRightArrow(IVwGraphics * pvg, bool fLogical)
	{
	}
	virtual void ControlShiftUpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
	{
	}
	virtual void ControlShiftDownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
	{
	}
	virtual void ControlShiftEndKey(IVwGraphics * pvg, bool fLogical)
	{
	}
	virtual void ControlShiftHomeKey(IVwGraphics * pvg, bool fLogical)
	{
	}

	/*----------------------------------------------------------------------------------------------
		If we are not a range, just leave the insertion point where it is.
		If we are a range, lose the selection, leaving us with an Insertion Point at one end
		position.
		Whether we move the anchor or end point is controlled by fCollapseToStart. If it is true,
		we collapse to whichever end comes first; if false, to whichever comes last.
	----------------------------------------------------------------------------------------------*/
	virtual void LoseSelection(bool fCollapseToStart)
	{
	}
	virtual COMINT32 VisiblePageHeight(IVwGraphics * pvg, Rect rcDocumentCoord, Rect rcClientCoord);
	void MoveIpVerticalByAmount(IVwGraphics * pvg, int lYAdjustment, bool fPutSelAtEdge,
		bool fIsExtendedSelection);

	virtual void PageUpKey(IVwGraphics * pvg);
	virtual void PageDownKey(IVwGraphics * pvg);

	virtual void ShiftPageUpKey(IVwGraphics * pvg);
	virtual void ShiftPageDownKey(IVwGraphics * pvg);

	virtual void ControlPageUpKey(IVwGraphics * pvg);
	virtual void ControlPageDownKey(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);

	virtual void ControlShiftPageUpKey(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	virtual void ControlShiftPageDownKey(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);

	virtual bool TabKey()
	{
		return true;
	}
	virtual bool ShiftTabKey()
	{
		return true;
	}

	//Extend the selection, given that the user made a shift-click or drag to
	//(x,y) relative to the root box, which would result in an insertion point
	//in clickBox. (relX, relY) give the postion of the click relative to clickBox.
	virtual void ExtendTo(IVwGraphics *pvg, VwRootBox* prootb, VwBox * pboxClick,
		int xd, int yd, Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst)
	{
		//default is to do nothing, that is, by default selections can't be extended.
	}


	// Other public methods

	// Called as part of root box draw; if currently visible,
	// draw it in the clipped region being redrawn. This has just
	// had the main text drawn under it, so it is in the
	// non-inverted state.
	void Draw(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot,
		int ysTop, int dysHeight, bool fDisplayPartialLines = false)
	{
		if (m_fShowing)
			Draw(pvg, true, rcSrcRoot, rcDstRoot, ysTop, dysHeight, fDisplayPartialLines);
	}
	void DrawIfShowing(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot,
		int ysTop, int dysHeight, bool fDisplayPartialLines = false);
	// Ensure turned on, unless disabled
	void Show();
	void ReallyShow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	void InvalidateSel();
	// Ensure turned off
	void Hide();
	// Toggle on/off, unless disabled
	void Invert();
	// Typically the opposite of IsRange, this is used specifically for flashing.
	virtual bool IsInsertionPoint()
	{
		return false;
	}
	virtual void DoPageUpDown(IVwGraphics * pvg, bool fIsPageUp, bool fIsExtendedSelection);
	virtual void DoCtrlPageUpDown(IVwGraphics * pvg, Rect rcDocumentCoord, Rect rcClientCoord,
		bool fIsPageUp, bool fIsExtendedSelection);

	virtual bool IsEditable(int ichIP, VwParagraphBox * pvpBoxIP, bool fAssocPrevIP);
	virtual bool FindClosestEditableIP(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	// Note any boxes that should not be deleted while this selection exists.
	virtual void AddToKeepList(LazinessIncreaser *pli)
	{
	}
	virtual bool AdjustForStringReplacement(VwTxtSrc * psrcModify, int itssMin, int itssLim,
		VwTxtSrc * psrcRep);
	virtual bool IsComplexSelection()
	{
		return false;
	}

protected:
	// Member variables
	long m_cref;
	bool m_fShowing;  // true if it has been drawn
	ComSmartPtr<VwRootBox> m_qrootb;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
	bool CommitAndNotify(VwSelChangeType nHowChanged, VwRootBox * prootb);

	// Draw the selection. If fOn is true it is being turned on;
	// otherwise, off.
	virtual void Draw(IVwGraphics * pvg, bool fOn, Rect rcSrcRoot, Rect rcDstRoot,
		int ysTop, int dysHeight, bool fDisplayPartialLines = false) = 0;
	virtual HRESULT AllTextSelInfoAux(int * pihvoRoot, int cvsli, VwSelLevInfo * prgvsli,
		PropTag * ptagTextProp, int * pcpropPrevious, int * pichAnchor, int * pichEnd, int * pws,
		ComBool * pfAssocPrev, int * pihvoEnd, ITsTextProps ** ppttpIns,
		ComBool fEndPoint);
	void BuildVsli(VwNotifier * pnoteInner, int cvsli,  VwSelLevInfo * prgvsli,
		int * pihvoRoot);

	// Same as get_IsEnabled, but directly returns the value instead of passing it as parameter
	bool IsEnabled();

	// Amount by which to adjust the end of the selection (e.g. half a line)
	virtual int AdjustEndLocation(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot)
	{
		return 0;
	}
};

DEFINE_COM_PTR(VwSelection);
#if 0
// Because we inherit twice from IUnknown, we have to make one of
// them primary.
IUnknown* operator()(VwSelection * psel)
{
	return (IUnknown*)(IVwSelection*)(psel);
}
#endif

//:>********************************************************************************************
//:>	Classes used internally by VwTextSelection::SetSelectionProps.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	We need to keep a map from (HVO, tag) to string builder. This keeps track of all the
	properties that have changed.
	ENHANCE JohnT: an equivalent class is defined if VwCacheDa.h. Could we put the defn
	somewhere it could be shared?
----------------------------------------------------------------------------------------------*/
class HvoTagRec
{
public:
	HVO m_hvo;
	PropTag m_tag;

	HvoTagRec() // needs  default constructor to be a key
	{
	}

	HvoTagRec(HVO hvo, PropTag tag)
	{
		m_hvo = hvo;
		m_tag = tag;
	}
};

/*----------------------------------------------------------------------------------------------
	A class that stores the information about how to update one property.
----------------------------------------------------------------------------------------------*/
class UpdateInfo
{
public:
	UpdateInfo()
	{
	}
	UpdateInfo(IVwViewConstructor * pvvc, int frag, VwNoteProps vnp, ITsStrBldr * ptsb)
	{
		m_qvvcEdit = pvvc;
		m_fragEdit = frag;
		m_vnp = vnp;
		m_qtsb = ptsb;
	}
	IVwViewConstructorPtr m_qvvcEdit;
	int m_fragEdit; // The fragment identifier which the VC needs for the edited property.
	VwNoteProps m_vnp; // Notifier Property attributes.
	ITsStrBldrPtr m_qtsb;
};

typedef enum
{
	kcsNormal = 0, // nothing special going on
	kcsWorking = 1, // We're working on something, getting a Commit request moves to next state
	kcsCommitRequest = 2, // While working on something, got a commit request
	kcsInCommit = 3, // Commit is in progress.
} CommitState;

class OnTypingMethod;
class GetSelectionStringMethod;

typedef HashMap<HvoTagRec, UpdateInfo> HvoTagUpdateMap; // Hungarian hmhtru
class __declspec(uuid("102AACD1-AE7E-11d3-9BAF-00400541F9E9")) VwTextSelection;
#define CLSID_VwTextSelection __uuidof(VwTextSelection)
/*----------------------------------------------------------------------------------------------
Class: VwTextSelection
Hungarian: vwtsel
----------------------------------------------------------------------------------------------*/
class VwTextSelection : public VwSelection
{
	friend class VwStringBox;
	friend class VwParagraphBox;
	friend class VwRootBox;
	friend class VwTextStore;
	friend class TestVwPattern;
	friend class OnTypingMethod;
	friend class GetSelectionStringMethod;
	friend class TestViews::TestVwPattern;
	friend class TestViews::TestVwTextSelection;
public:
	// IUnknown
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	// IVwSelection
	STDMETHOD(GetSelectionProps)(int cttpMax, ITsTextProps ** prgpttp,
		IVwPropertyStore ** prgpvps, int * pcttp);
	STDMETHOD(GetHardAndSoftCharProps)(int cttpMax, ITsTextProps ** prgpttpSel,
		IVwPropertyStore ** prgpvpsSoft, int * pcttp);
	STDMETHOD(GetParaProps)(int cttpMax, IVwPropertyStore ** prgpvps, int * pcttp);
	STDMETHOD(GetHardAndSoftParaProps)(int cttpMax, ITsTextProps ** prgpttpPara,
		ITsTextProps ** prgpttpHard, IVwPropertyStore ** prgpvpsSoft, int * pcttp);
	STDMETHOD(SetSelectionProps)(int cttp, ITsTextProps ** prgpttp);
	STDMETHOD(TextSelInfo)(ComBool fEndPoint, ITsString ** pptss, int * pich,
		ComBool * pfAssocPrev, HVO * phvoObj, PropTag * ptag, int * pws);
	STDMETHOD(CLevels)(ComBool fEndPoint, int * pclev);
	STDMETHOD(PropInfo)(ComBool fEndPoint, int ilev, HVO * phvoObj, PropTag * ptag, int * pihvo,
		int * pcpropPrevious, IVwPropertyStore ** ppvps);
	STDMETHOD(CompleteEdits)(VwChangeInfo * pci, ComBool * pfOk);
	STDMETHOD(ExtendToStringBoundaries)();
	STDMETHOD(get_EndBeforeAnchor)(ComBool* pfRet);
	STDMETHOD(Location)(IVwGraphics * pvg, RECT rcSrc, RECT rcDst, RECT * prdPrimary,
		RECT * prdSecondary, ComBool * pfSplit, ComBool * pfEndBeforeAnchor);
	STDMETHOD(GetParaLocation)(RECT * prdLoc);
	STDMETHOD(ReplaceWithTsString)(ITsString * ptss);
	STDMETHOD(GetSelectionString)(ITsString ** pptss, BSTR bstr);
	STDMETHOD(GetFirstParaString)(ITsString ** pptss, BSTR bstr, ComBool * pfGotItAll);
	STDMETHOD(SetIPLocation)(ComBool fTopLine, int xdPos);
	STDMETHOD(get_CanFormatPara)(ComBool* pfRet);
	STDMETHOD(get_CanFormatChar)(ComBool* pfRet);
	STDMETHOD(get_CanFormatOverlay)(ComBool* pfRet);
	STDMETHOD(Install)();
	STDMETHOD(get_Follows)(IVwSelection * psel, ComBool * pfFollows);
	STDMETHOD(get_ParagraphOffset)(ComBool fEndPoint, int * pich);
	STDMETHOD(get_SelType)(VwSelType * pstType);
	STDMETHOD(GrowToWord)(IVwSelection ** ppsel);
	STDMETHOD(EndPoint)(ComBool fEndPoint, IVwSelection ** ppsel);
	STDMETHOD(SetTypingProps)(ITsTextProps * pttp);

	STDMETHOD(get_BoxDepth)(ComBool fEndPoint, int * pcDepth);
	STDMETHOD(get_BoxIndex)(ComBool fEndPoint, int iLevel, int * piAtLevel);
	STDMETHOD(get_BoxCount)(ComBool fEndPoint, int iLevel, int * pcAtLevel);
	STDMETHOD(get_BoxType)(ComBool fEndPoint, int iLevel, VwBoxType * pvbt);
	STDMETHOD(get_IsEditable)(ComBool * pfEditable);
	STDMETHOD(get_AssocPrev)(ComBool * pfValue);
	STDMETHOD(put_AssocPrev)(ComBool fValue);

	// Constructors/destructors/etc.
	VwTextSelection();
	VwTextSelection(VwParagraphBox * pvpbox, int ichAnchor, int ichEnd,
		bool fAssocPrevious);
	VwTextSelection(VwParagraphBox * pvpbox, int ichAnchor, int ichEnd,
		bool fAssocPrevious, VwParagraphBox * pvpboxEnd);
	virtual bool IsInsertionPoint() // override
	{
		return m_ichAnchor == m_ichEnd && !m_pvpboxEnd;
	}

	VwStringBox * GetClosestStringBox(VwBox * pBox, bool * fFoundAfter = NULL);

	void ExtendTo(IVwGraphics * pvg, VwRootBox * prootb, VwBox * pboxClick,
		int xd, int yd, Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst);

	virtual VwSelectionState SelectionState();

	// Methods used in editing the selection.
	virtual void StartEditing();
	virtual VwDelProbType IsProblemSelection();
	virtual bool IsComplexSelection();
	virtual void OnTyping(IVwGraphics * pvg, const wchar * pchInput, int cchInput, VwShiftStatus ss, int * pwsPending);
	void DoUpdateProp(VwRootBox * prootb, HVO hvo, PropTag tag, VwNoteProps vnp,
		IVwViewConstructor * pvvcEdit, int fragEdit, ITsStrBldr * ptsb, bool * pfOk);
	virtual bool RuinedByDeleting(VwBox * pbox, VwBox * pboxReplacement=0);
	void MakeSubString(ITsString * ptss, int ichMin, int ichLim, ITsString ** pptssSub);

	void SetInsertionProps(ITsTextProps * pttp)
	{
		if (IsInsertionPoint())
			m_qttp = pttp;
	}

	// Focus Handling
	virtual void LoseFocus(IVwSelection * pvwselNew, ComBool * pfOk);

protected:
	// This computes the baseline of the lowest-level box clicked in, relative to
	// the whole document in layout coordinates. Note that expanding lazy boxes
	// will render this value meaningless unless corrected.
	int CalculateBaseline(VwBox * pboxOrig)
	{
		int yBaselineOrig = pboxOrig->Ascent();
		for (VwBox * pboxT = pboxOrig; pboxT; pboxT = pboxT->Container())
			yBaselineOrig += pboxT->Top();
		return yBaselineOrig;
	}

	// Insertion point movement helpers
	virtual void LoseSelection(bool fCollapseToStart);

	// Move insertion point or selection appropriately for arrow keys, if possible
	virtual bool LeftArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping = false);
	virtual bool RightArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping = false);
	virtual bool UpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, int * pxdPos);
	virtual bool DownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, int * pxdPos);
	virtual void EndKey(IVwGraphics * pvg, bool fLogical);
	virtual void HomeKey(IVwGraphics * pvg, bool fLogical);

	virtual void ShiftLeftArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping = false);
	virtual void ShiftRightArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping = false);
	virtual void ShiftUpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	virtual void ShiftDownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	virtual void ShiftEndKey(IVwGraphics * pvg, bool fLogical);
	virtual void ShiftHomeKey(IVwGraphics * pvg, bool fLogical);

	virtual bool ControlLeftArrow(IVwGraphics * pvg, bool fLogical);
	virtual bool ControlRightArrow(IVwGraphics * pvg, bool fLogical);
	virtual bool ControlUpArrow(IVwGraphics * pvg);
	virtual bool ControlDownArrow(IVwGraphics * pvg);
	virtual void ControlEndKey(IVwGraphics * pvg, bool fLogical);
	virtual void ControlHomeKey(IVwGraphics * pvg, bool fLogical);

	virtual void ControlShiftLeftArrow(IVwGraphics * pvg, bool fLogical);
	virtual void ControlShiftRightArrow(IVwGraphics * pvg, bool fLogical);
	virtual void ControlShiftUpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	virtual void ControlShiftDownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	virtual void ControlShiftEndKey(IVwGraphics * pvg, bool fLogical);
	virtual void ControlShiftHomeKey(IVwGraphics * pvg, bool fLogical);

	virtual bool TabKey();
	virtual bool ShiftTabKey();

	bool ForwardArrow(IVwGraphics * pvg, bool fSuppressClumping = false);
	bool BackwardArrow(IVwGraphics * pvg, bool fSuppressClumping = false);
	bool CalcAssocPrevForBackArrow(int ich, VwParagraphBox *pvpbox,	IVwGraphics *pvg);
	bool PhysicalArrow(IVwGraphics * pvg, bool fRight, bool fSuppressClumping = false);
	void PhysicalHomeOrEnd(IVwGraphics * pvg, bool fEnd);
	void ShiftForwardArrow(IVwGraphics * pvg, bool fSuppressClumping = false);
	void ShiftBackwardArrow(IVwGraphics * pvg, bool fSuppressClumping = false);
	void ShiftPhysicalArrow(IVwGraphics * pvg, bool fRight, bool fSuppressClumping = false);
	void ShiftPhysicalHomeOrEnd(IVwGraphics * pvg, bool fEnd);
	bool ControlForwardArrow(IVwGraphics * pvg);
	bool ControlBackwardArrow(IVwGraphics * pvg);
	bool ControlPhysicalArrow(IVwGraphics * pvg, bool fRight);
	void ControlShiftForwardArrow(IVwGraphics * pvg);
	void ControlShiftBackwardArrow(IVwGraphics * pvg);
	void ControlShiftPhysicalArrow(IVwGraphics * pvg, bool fRight);

public:
	bool ExpandToWord(VwTextSelection * pselNew);
	virtual bool IsEditable(int ichIP, VwParagraphBox * pvpBoxIP, bool fAssocPrevIP);
	bool IsEditable(int ichIP, VwParagraphBox * pvpBoxIP);
	virtual bool FindClosestEditableIP(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	VwParagraphBox * AnchorBox() {return m_pvpbox;}
	VwParagraphBox * EndBox() {return m_pvpboxEnd;} // NULL if same as anchor
	int AnchorOffset() {return m_ichAnchor;} // Logical (not rendered) offset
	int EndOffset() {return m_ichEnd;} // Logical (not rendered) offset
	void GetLimit(bool fEnd, VwParagraphBox ** ppvpbox, int * pich);
	void ExtendEndTo(VwTextSelection * psel);
	void ContractToEnd();
	virtual void AddToKeepList(LazinessIncreaser *pli);
	bool AssocPrevious() {return m_fAssocPrevious;}

	int FindWordBoundary(int ichLogIP, VwParagraphBox * pvpboxIP, IVwGraphics * pvg,
									  bool fForward);

protected:
	// Member variables
	VwParagraphBox * m_pvpbox; // Selected text is generally all in this box...
	VwParagraphBox * m_pvpboxEnd; // Unless this is non-null, in which case end point is here...
	bool m_fEndBeforeAnchor;
	// If anchor == end, the selection is an IP just before the indicated character, and
	// m_fAssocPrevious is true if the IP is primarily associated with the character logically
	// before the IP. Otherwise, min(anchor, end) gives the Min, and max(anchor, end) the Lim,
	// of the range of selected characters, relative to the start of the box.
	// These are offsets in logical (as opposed to physical or rendered) characters.
	int m_ichAnchor;
	int m_ichEnd;

	Rect m_rcBounds; // The bounds of the selection when drawn.

	static const int kcchTemp = 50;

protected:
	bool m_fAssocPrevious;
	// This is set equal to m_ichEnd by double-clicking to select an entire word.  It is needed
	// to allow dragging to extend the selection while the left button is still depressed for
	// the second click.  A single click or moving the selection with an arrow key resets this
	// to -1.
	int m_ichAnchor2;

	// A TsTextProps for the next character to be inserted. Usually set to null when the selection
	// changes. It is useful when a formatting command is issued while the selection is an IP,
	// or when typing over a selection should not produce text with the properties of the first
	// character, as sometimes when typing around verse numbers.
	ITsTextPropsPtr m_qttp;

	// NOTE that new variables added here should normally be copied if this selection is
	// replaced by a new one in LoseFocus.

	// A string builder for the property being edited. If we have a multi-paragraph selection,
	// it is initially a builder for the property in the last paragraph.
	ITsStrBldrPtr m_qtsbProp;
	// The object and property being edited.
	HVO m_hvoEdit;
	int m_tagEdit;
	// While editing, the range of the whole paragraph that corresponds to the property
	// being edited. Meaningless if m_qtsbProp is null.
	int m_ichMinEditProp;
	int m_ichLimEditProp;
	// The view constructor, if any, responsible for the edited property.
	IVwViewConstructorPtr m_qvvcEdit;
	int m_fragEdit; // The fragment identifier which the VC needs for the edited property.
	VwAbstractNotifierPtr m_qanote; // The notifier for the property.
	int m_iprop; // the index of the property within that notifier.
	int m_itssProp; // index of edited string in list for this para

	VwNoteProps m_vnp; // Notifier Property attributes.

	// These variables are relevant where a selection is part of an editable sequence
	// of paragraphs. The are set up by StartEditing and not valid unless m_qtsbProp is.
	HVO m_hvoParaOwner; // hvo of the object that has the following property
	PropTag m_tagParaProp; // property that holds one object per paragraph
	VwNotifier * m_pnoteParaOwner; // Notifier governing this display of the paragraphs prop
	int m_ipropPara; // Index of (relevant occurrence of) the prop in the notifier
	int m_ihvoFirstPara; // index in that prop of first partly-selected object.
	int m_ihvoLastPara; // index in that prop of last partly-selected object.
	// (The above two indexes are always in ascending order; they are not tied to anchor/end.)
	// (They are the same if we have a one-paragraph selection.)

	// Save the screen X location of the insertion point for the Up and Down Arrow keys.
	// This should be invalidated by any operation that changes the insertion point other than
	// an UpArrow or DownArrow key.  (invalid indicated by -1)
	int m_xdIP;

	// Manages multiple commit requests and commit requests during other operations.
	// See enumeration for details.
	CommitState m_csCommitState;

	// other protected methods
	bool CommitAndNotify(VwSelChangeType nHowChanged, VwRootBox * prootb = NULL);
	virtual void Draw(IVwGraphics * pvg, bool fOn, Rect rcSrcRoot, Rect rcDstRoot,
		int ysTop, int dysHeight, bool fDisplayPartialLines = false);
	void FixStringVars(int ichEditNew);

	bool FindEditablePlaceOnLine(int * pich, VwParagraphBox ** ppvpbox, bool * pfAssocPrev,
		IVwGraphics * pvg);
	int ForwardOneChar(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
		IVwGraphics * pvg, VwParagraphBox ** ppvpbox, bool fSuppressClumping = false,
		bool fLimitToCurrentPara = false);
	int ForwardOneWord(int ichLogIP, VwParagraphBox * pvpboxIP, IVwGraphics * pvg,
		VwParagraphBox ** ppvpbox);
	int EndOfLine(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev, IVwGraphics * pvg);
	int DownOneLine(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev, IVwGraphics * pvg,
		Rect rcSrcRoot, Rect rcDstRoot, VwParagraphBox ** ppvpbox, int * pichLogHome);
	int EndOfView(IVwGraphics * pvg, VwParagraphBox ** ppvpbox);

	int BackOneChar(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev, IVwGraphics * pvg,
		VwParagraphBox ** ppvpbox, bool fSuppressClumping = false);
	int BackOneWord(int ichLogIP, VwParagraphBox * pvpboxIP, IVwGraphics * pvg,
		VwParagraphBox ** ppvpbox);
	int BeginningOfLine(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
		IVwGraphics * pvg);
	int UpOneLine(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev, IVwGraphics * pvg,
		Rect rcSrcRoot, Rect rcDstRoot, VwParagraphBox ** ppvpbox, int * pichLogHome);
	int BeginningOfView(IVwGraphics * pvg, VwParagraphBox ** ppvpbox);

	int ForwardOneCharInLine(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
		IVwGraphics * pvg);
	int BackOneCharInLine(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
		IVwGraphics * pvg);

	int PositionOfIP(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev,
		IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, int * pxdSec);
	bool CheckCommit(int ichLogIP, VwParagraphBox * pvpboxIP);
	void UnprotectedCommit(bool * pfOk);
	VwStringBox * GetStringBox(int ichLogIP, VwParagraphBox * pvpboxIP, bool fAssocPrev);
	bool AdjustForRep(int & ichLog, VwParagraphBox * pvpbox, int itssMin, int itssLim,
		VpsTssVec & vpst);

	// Test whether the given location is at the beginning (or end) of a line on the display.
	bool IsBeginningOfLine(int ichLogIP, VwParagraphBox * pvpboxIP, IVwGraphics * pvg)
	{
		return ichLogIP == BeginningOfLine(ichLogIP, pvpboxIP, false, pvg);
	}
	bool IsEndOfLine(int ichLogIP, VwParagraphBox * pvpboxIP, IVwGraphics * pvg)
	{
		return ichLogIP == EndOfLine(ichLogIP, pvpboxIP, true, pvg);
	}

	void CommitAndContinue(bool * pfOk, VwChangeInfo * pci = NULL);
	VwParagraphBox * MakeDummyPara(ITsString * ptss, VwPropertyStore * pzvps);
	bool SetEnd(VwParagraphBox * pvpboxNew, VwParagraphBox * pvpboxEnd, int ichEnd,
		bool fEndBeforeAnchor, VwSelChangeType & nHowChanged);

	// Amount by which to adjust the end of the selection (i.e. half a line)
	virtual int AdjustEndLocation(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot); // override

public:
	void GetInsertionProps(int ichAnchor, ITsTextProps * pttp, ITsTextProps ** ppttp);
	virtual bool AdjustForStringReplacement(VwTxtSrc * psrcModify, int itssMin, int itssLim,
		VwTxtSrc * psrcRep);
protected:
	int UpdatePropsMap(ITsString * ptss, int ich, int ichMinTss,
		int ichLimTss, int ichStart, int ichEnd, ITsTextProps * pttp,
		VwParagraphBox * pvpboxCurr, HvoTagUpdateMap & hmhtru);

	void BeginUndoTask(ISilDataAccess * psda, int stid);

	virtual HRESULT AllTextSelInfoAux(int * pihvoRoot, int cvsli, VwSelLevInfo * prgvsli,
		PropTag * ptagTextProp, int * pcpropPrevious, int * pichAnchor, int * pichEnd, int * pws,
		ComBool * pfAssocPrev, int * pihvoEnd, ITsTextProps ** ppttpIns,
		ComBool fEndPoint);

	void GetHardAndSoftPropsOneRun(ITsTextProps * pttp, IVwPropertyStore * pvpsParent,
		IVwPropertyStore ** ppvpsRet);
	void GetFirstAndLast(VwParagraphBox ** ppvpboxFirst,
		VwParagraphBox ** ppvpboxLast, int * pichFirst, int * pichLast);
	void ShrinkSelection();
	bool DeleteRangeAndPrepareToInsert();
	void FindWordBoundaries(int & ichMin, int & ichLim);
	void GetEndInfo(bool fEndPoint, VwParagraphBox ** ppvpbox, int & ichTarget,
		bool & fAssocPrevious);
	void StartProtectMethod();
	void EndProtectMethod();
	void CleanPropertiesForTyping();
	void RequestSelectionAfterUow(int ihvoPara, int ich, bool fAssocPrev);

};
DEFINE_COM_PTR(VwTextSelection);

class __declspec(uuid("6AFD893B-6336-48a8-953A-3A6C2879F721")) VwPictureSelection;
#define CLSID_VwPictureSelection __uuidof(VwPictureSelection)
/*----------------------------------------------------------------------------------------------
Class: VwPictureSelection
Description: Picture selections are used when we want to select something...typically a picture,
but other kinds of leaf boxes (except VwStringBox) are also possible...that is not text.
Hungarian: vwpsel
----------------------------------------------------------------------------------------------*/
class VwPictureSelection : public VwSelection
{
	friend class VwStringBox;
	friend class VwRootBox;

public:
	// IUnknown
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	VwPictureSelection();
	VwPictureSelection(VwLeafBox * ppicbox, int ipicAnchor, int ipicEnd,
		bool fAssocPrevious);

	STDMETHOD(PropInfo)(ComBool fEndPoint, int ilev, HVO * phvoObj, PropTag * ptag, int * pihvo,
		int * pcpropPrevious, IVwPropertyStore ** ppvps);

	STDMETHODIMP get_CanFormatPara(ComBool * pfRet);
	STDMETHODIMP get_CanFormatChar(ComBool * pfRet);
	STDMETHOD(get_EndBeforeAnchor)(ComBool* pfRet);
	STDMETHOD(Location)(IVwGraphics * pvg, RECT rcSrc, RECT rcDst, RECT * prdPrimary,
		RECT * prdSecondary, ComBool * pfSplit, ComBool * pfEndBeforeAnchor);
	STDMETHOD(CLevels)(ComBool fEndPoint, int * pclev);
	STDMETHOD(TextSelInfo)(ComBool fEndPoint, ITsString ** pptss, int * pich,
		ComBool * pfAssocPrev, HVO * phvoObj, PropTag * ptag, int * pws);
	STDMETHOD(GetSelectionString)(ITsString ** pptss, BSTR bstr);
	STDMETHOD(GetFirstParaString)(ITsString ** pptss, BSTR bstr, ComBool * pfGotItAll);
	STDMETHOD(GetParaLocation)(RECT * prdLoc);
	STDMETHOD(get_SelType)(VwSelType * pstType);
	STDMETHOD(EndPoint)(ComBool fEndPoint, IVwSelection ** ppsel);
	STDMETHOD(get_BoxDepth)(ComBool fEndPoint, int * pcDepth);
	STDMETHOD(get_BoxIndex)(ComBool fEndPoint, int iLevel, int * piAtLevel);
	STDMETHOD(get_BoxCount)(ComBool fEndPoint, int iLevel, int * pcAtLevel);
	STDMETHOD(get_BoxType)(ComBool fEndPoint, int iLevel, VwBoxType * pvbt);
	STDMETHOD(get_ParagraphOffset)(ComBool fEndPoint, int * pich);

	// Return the selection state of the selection.
	virtual VwSelectionState SelectionState()
	{
		return vssEnabled;
	}
	VwLeafBox * LeafBox() {return m_plbox;}

	virtual void LoseFocus(IVwSelection * pvwselNew, ComBool * pfOk);
	virtual void AddToKeepList(LazinessIncreaser *pli);

	virtual bool LeftArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping = false);
	virtual bool RightArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping = false);
	virtual bool UpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, int * pxdPos);
	virtual bool DownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, int * pxdPos);
	virtual void EndKey(IVwGraphics * pvg, bool fLogical);
	virtual void HomeKey(IVwGraphics * pvg, bool fLogical);

	virtual void ShiftLeftArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping = false);
	virtual void ShiftRightArrow(IVwGraphics * pvg, bool fLogical, bool fSuppressClumping = false);
	virtual void ShiftUpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	virtual void ShiftDownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	virtual void ShiftEndKey(IVwGraphics * pvg, bool fLogical);
	virtual void ShiftHomeKey(IVwGraphics * pvg, bool fLogical);

	virtual bool ControlLeftArrow(IVwGraphics * pvg, bool fLogical);
	virtual bool ControlRightArrow(IVwGraphics * pvg, bool fLogical);
	virtual bool ControlUpArrow(IVwGraphics * pvg);
	virtual bool ControlDownArrow(IVwGraphics * pvg);
	virtual void ControlEndKey(IVwGraphics * pvg, bool fLogical);
	virtual void ControlHomeKey(IVwGraphics * pvg, bool fLogical);

	virtual void ControlShiftLeftArrow(IVwGraphics * pvg, bool fLogical);
	virtual void ControlShiftRightArrow(IVwGraphics * pvg, bool fLogical);
	virtual void ControlShiftUpArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	virtual void ControlShiftDownArrow(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	virtual void ControlShiftEndKey(IVwGraphics * pvg, bool fLogical);
	virtual void ControlShiftHomeKey(IVwGraphics * pvg, bool fLogical);

//	void Invert();
	// Typically the opposite of IsRange, this is used specifically for flashing.
protected:
	// Member variables
	VwLeafBox * m_plbox;
	bool m_fEndBeforeAnchor;

	// If anchor == end, the selection is an IP just before the indicated picture, and
	// m_fAssocPrevious is true if the IP is primarily associated with the picture logically
	// before the IP. Otherwise, min(anchor, end) gives the Min, and max(anchor, end) the Lim,
	// of the range of selected pictures, relative to the start of the box.
	int m_ichAnchorLog;
	int m_ichEndLog;
	bool m_fAssocPrevious;

	// The object and property being edited.
	HVO m_hvoEdit;
	int m_tagEdit;

	// Save the screen X location of the insertion point for the Up and Down Arrow keys.
	// This should be invalidated by any operation that changes the insertion point other than
	// an UpArrow or DownArrow key.  (invalid indicated by -1)
	int m_xdIP;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods

	// Draw the selection. If fOn is true it is being turned on;
	// otherwise, off.
	virtual void Draw(IVwGraphics * pvg, bool fOn, Rect rcSrcRoot, Rect rcDstRoot,
		int ysTop, int dysHeight, bool fDisplayPartialLines = false);
	virtual HRESULT AllTextSelInfoAux(int * pihvoRoot, int cvsli, VwSelLevInfo * prgvsli,
		PropTag * ptagTextProp, int * pcpropPrevious, int * pichAnchor, int * pichEnd, int * pws,
		ComBool * pfAssocPrev, int * pihvoEnd, ITsTextProps ** ppttpIns,
		ComBool fEndPoint);
	VwNotifier * GetNotifier(int * piprop = NULL);
	int GetCharPosition();
	virtual bool RuinedByDeleting(VwBox * pbox, VwBox * pboxReplacement=0);
};

DEFINE_COM_PTR(VwPictureSelection);

#endif  //VWSELECTION_INCLUDED
