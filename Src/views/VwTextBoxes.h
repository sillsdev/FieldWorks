/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TextBoxes.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Contains the classes VwAbstractStringBox (and subclasses) which handle displaying strings,
	plus the VwParagraphBox class which lays them (and other stuff) out.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TEXTBOXES_INCLUDED
#define TEXTBOXES_INCLUDED

#include "OleStringLiteral.h"

#ifndef WIN32
// TODO Review - Assuming the MSVC++ compiler adds a forward declare ref when "friend class ParaBuilder;" is used. Gcc doesn't do this.
class ParaBuilder;
#endif

/*----------------------------------------------------------------------------------------------
Class: VwStringBox
Description: These are temporary boxes created as part of the paragraph layout process.
Hungarian: sbox
----------------------------------------------------------------------------------------------*/
class VwStringBox : public VwLeafBox
{
	friend class ParaBuilder;
protected:

	// member variables
	int m_ichMin; // first char in para's VwTxtSrc considered part of this
	ILgSegmentPtr m_qlseg;
	int m_dysAscent;

	// constructors, destructor
	VwStringBox()
		:VwLeafBox()
	{
#ifdef _DEBUG
		// acts as a flag for validity of the box.
		rgchDebugStr[0] = 0xffff;
#endif
	}

public:
	VwStringBox(VwPropertyStore * pzvps, int ichMin)
		:VwLeafBox(pzvps)
	{
		m_ichMin = ichMin;
#ifdef _DEBUG
		// acts as a flag for validity of the box.
		rgchDebugStr[0] = 0xffff;
#endif
	}

	virtual ~VwStringBox();

	virtual short SerializeKey() {return 8;}

	// drawing
	virtual void DrawForeground(IVwGraphics *pvg, Rect rcSrc, Rect rcDst);

	// overridden to also invalidate possible overhang
	virtual Rect GetInvalidateRect();

	// override DoLayout (but not Relayout--there is never anything to change about
	// a string box's size because of other boxes changing, unless the paragraph
	// changes the text it contains, which forces a recalculation directly.
	virtual void DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);

	virtual void GetDependentObjects(Vector<GUID> & vguid);
	virtual int Ascent();

	// member variable access

	// return a pointer (no ref count) to the embedded segment.
	ILgSegment * Segment()
	{
		return m_qlseg;
	}

	void SetSegment(ILgSegment *plseg)
	{
		m_qlseg = plseg;
	}

	// These boxes are string boxes
	virtual bool IsStringBox()
	{
		return true;
	}

	// They are generated directly from the strings in the text source.
	virtual bool IsBoxFromTsString()
	{
		return true;
	}

	// Rendered char position where the segment starts.
	int IchMin()
	{
		return m_ichMin;
		//Assert(Container());
		//return m_dichMin + Container()->Source()->IchStartString(m_itss);
	}

	// Increment ichMin by the specified amount (or decrement, if negative)
	void _IncIchMin(int dichMin)
	{
		m_ichMin += dichMin;
	}

	void _SetIchMin(int ichMin)
	{
		m_ichMin = ichMin;
	}

	void GetSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
		Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, VwSelection ** ppvwsel,
		SearchDirection sdir = ksdirBoth);
	void GetPointOffset(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
		int * pich, bool * pfAssocPrevious);
	void GetExtendedClickOffset(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
		VwParagraphBox * pvpboxAnchor, int ichAnchor,
		int * pich);
	virtual MakeSelResult MakeSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
		Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, bool forceReadOnly=false);

	void Reset();  // clean up ready to be reused in new layout
	virtual OLECHAR * Name()
	{
		static OleStringLiteral name(L"String");
		return name;
	}
	virtual int Role()
	{
		return ROLE_SYSTEM_TEXT;
	}
	virtual StrUni Description();
	virtual Rect DrawRange(IVwGraphics * pvg,
		RECT rcSrc1, RECT rcDst1, int ichMin, int ichLim, int ydTop, int ydBottom, ComBool bOn,
		Rect rcSrcRoot, Rect rcDstRoot, bool fIsLastParaOfSelection);
	virtual void PositionOfRange(IVwGraphics* pvg,
		RECT rcSrc1, RECT rcDst1, int ichMin, int ichLim, int ydTop, int ydBottom,
		RECT * prsBounds, ComBool * pfAnythingToDraw, Rect rcSrcRoot, Rect rcDstRoot,
		bool fIsLastParaOfSelection);

	virtual void DeleteAndCleanup(NotifierVec &vpanoteDel);

	void DoHotLink(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
		Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst);

#ifdef _DEBUG
#define MAX_DEBUG_STR_LEN 20
	OLECHAR rgchDebugStr[MAX_DEBUG_STR_LEN + 1];
	OLECHAR * DebugStr();
#endif /* _DEBUG */
};

/*----------------------------------------------------------------------------------------------
Class: VwStringBox
Description: This subclass exists, so far, just to identify the box as containing a drop cap.
Hungarian: sbox
----------------------------------------------------------------------------------------------*/
class VwDropCapStringBox : public VwStringBox
{
	typedef VwStringBox SuperClass;
	friend class ParaBuilder;
public:
	VwDropCapStringBox(VwPropertyStore * pzvps, int ichMin)
		:VwStringBox(pzvps, ichMin)
	{
	}
	virtual bool IsDropCapBox()
	{
		return true;
	}
	// This routine returns an adjusted box descent used in laying out lines that
	// might contain a DropCapsStringBox. It is used only by the paragraph layout.
	// DropCapsStringBox overrides.
	virtual int LineSepDescent(bool fAloneOnLine)
	{
		// If it's the only box on the line, we want the line to have its full descent.
		// Otherwise we return zero, allowing the max descent to be determined by
		// the other boxes on the line.
		if (fAloneOnLine)
			return Descent();
		else
			return 0;
	}
	virtual Rect DrawRange(IVwGraphics * pvg,
		RECT rcSrc1, RECT rcDst1, int ichMin, int ichLim, int ydTop, int ydBottom, ComBool bOn,
		Rect rcSrcRoot, Rect rcDstRoot, bool fIsLastParaOfSelection); // override
	virtual void PositionOfRange(IVwGraphics* pvg,
		RECT rcSrc1, RECT rcDst1, int ichMin, int ichLim, int ydTop, int ydBottom,
		RECT * prsBounds, ComBool * pfAnythingToDraw, Rect rcSrcRoot, Rect rcDstRoot,
		bool fIsLastParaOfSelection);
	int AdjustSelBottom(IVwGraphics * pvg, int ydLineBottom, Rect rcSrcRoot, Rect rcDstRoot);
	virtual int Ascent();
protected:
	// Rather than just fiddle with the inherited m_dysAscent, which can easily be broken
	// by another call to DoLayout, we figure the appropriate adjustment once and save it.
	int m_dysSubFromAscent;
};

/*----------------------------------------------------------------------------------------------
Class: VwParagraphBox
Description: This class represents a paragraph. It can contain both fixed-size boxes like
Interlinear bundles, buttons, and pictures; or text, which can be more extensively
rearranged. The paragraph box is responsible for laying all this out in lines.
Hungarian: vpbox
----------------------------------------------------------------------------------------------*/
class VwParagraphBox : public VwGroupBox
{
	friend class ParaBuilder;
	typedef VwGroupBox SuperClass;
	friend class SpellCheckMethod;
	friend class DrawForegroundMethod;
	// Information about a tag used in drawing one above or below the text.
	struct TagInfo
	{
		COLORREF clrFore;
		COLORREF clrBack;
		COLORREF clrUnder;
		int unt;
		StrUni stuAbbr;
		GUID uid;
	}; // Hungarian ti

	typedef Vector<TagInfo> VecTagInfo; // Hungarian vti

	// Information about a tag used in displaying a popup menu or hover information.
	/*struct MenuInfo
	{
		OLECHAR rgchGuid[kcchGuidRepLength];
		StrUni stuAbbr;
		StrUni stuName;
	}; // Hungarian mi

	typedef Vector<MenuInfo> VecMenuInfo; // Hungarian vmi*/

	struct TagSelectInfo
	{
		int xd;		// Point we are inquiring about.
		int yd;
		bool fTagClicked; // Set true if (xd, yd) is in a tag list
		// Remaining info filled in only if fTagClicked set true.
		//VecMenuInfo vmi; // Full list of changes for tag list
		int ich;		// char index where changes occur
		bool fOpening; // click on opening tag list or closing one
		Vector<GUID> vguid; // Full list of guids for this group of tags
		int iguid; // which tag in vguid was clicked; or -1, if ... clicked
		RECT rc;
		RECT rcAllTags;
		//int iguid; // which tag in vmi was clicked; or -1, if ... clicked
	}; // Hungarian tsi

protected:
	friend class VwBox; //allows deserialization to use zero-argument constructor
	VwParagraphBox()
		:VwGroupBox()
	{
		m_dympExactAscent = -1;
	}

	void GetWritingSystemFactory(ILgWritingSystemFactory ** ppwsf);

	//	member variables:
	VwTxtSrcPtr m_qts;
	bool m_fParaRtl;
	int m_dxsRightEdge;		// rt edge of para, for drawing bullets when para is RTL
	int m_dympExactAscent;	// when doing exact line spacing; -1 otherwise. In millipoints.
	bool m_fSemiTagging;
	VwBoundaryMark m_BoundaryMark; // enumeration used to represent the paragraph or section mark
	int m_dxdBoundaryMark; // the width of the boundary mark character

	// A little struct used to pass info between DrawForeground and its overlay
	// sub-methods.
	struct FobData
	{
		BoxVec vboxLine; // boxes on line
		int ibox; // position of this one in it
		Rect rcSrcPara; // rcSrc and rcDst with para top left as (0,0)
		Rect rcDst;
		FobData(BoxVec vboxLineA, int iboxA, Rect rcSrcParaA, Rect rcDstA)
			:vboxLine(vboxLineA), ibox(iboxA), rcSrcPara(rcSrcParaA), rcDst(rcDstA)
		{
		}

	}; // Hungarian fobd

public:
	VwParagraphBox(VwPropertyStore * pzvps, VwSourceType vst = kvstNormal);
	virtual ~VwParagraphBox();
	virtual short SerializeKey() {return 10;}

	// drawing
	virtual void DrawForeground(IVwGraphics *pvg, Rect rcSrc, Rect rcDst);
	void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTopOfPage,
		int dysPageHeight, bool fDisplayPartialLines = false);
	void DrawLastLine(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTopOfPage,
		int dysPageHeight, bool fDisplayPartialLines);
	virtual void DrawBorder(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	virtual void PrintPage(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst, int ysStart, int ysEnd);
	virtual bool FindNiceBreak(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst,
		int ysStart, int * pysEnd, bool fDisregardKeeps);

	virtual void DoLayout(IVwGraphics * pvg, int dxAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);
	virtual VwBox * FindNonPileChildAtOffset (int dysPosition, int dpiY, int * pdysOffsetIntoBox = NULL);

	virtual bool StretchToPileWidth(int dxpInch, int dxpTargetWidth, int tal);

	virtual int Ascent();

	virtual void Add(VwBox * pbox);

	virtual bool IsParagraphBox()
	{
		return true;
	}

	// Gets the paragraph box if this is a paragraph, or if the only thing
	// it contains is a paragraph box (e.g. a paragraph box inside of a single
	// table cell inside of a single table row inside of a table).
	virtual VwParagraphBox * GetOnlyContainedPara() // override
	{
		return this;
	}
	// Gets the paragraph box if this is a paragraph, or if one of our child boxes
	// contains a paragraph box.
	virtual VwParagraphBox * GetAnyContainedPara()
	{
		return this;
	}

	VwTxtSrc * Source()
	{
		return m_qts;
	}

	// Caller is responsible to update layout etc. as necessary! Use with caution!
	void SetSource(VwTxtSrc * pts)
	{
		m_qts = pts;
	}

	bool RightToLeft()
	{
		return m_fParaRtl;
	}
	void SetRightToLeft(bool f)
	{
		m_fParaRtl = f;
	}

	void SetRightEdge(int dx)
	{
		m_dxsRightEdge = dx;
	}

	int ExactAscent()
	{
		return m_dympExactAscent;
	}
	void SetExactAscent(int dy)
	{
		m_dympExactAscent = dy;
	}

	bool SemiTagging()
	{
		return m_fSemiTagging;
	}
	void SetSemiTagging(bool f)
	{
		m_fSemiTagging = f;
	}

	virtual int PadLeading();
	virtual int BorderTop();
	virtual int BorderBottom();

	VwBox * FindBoxClicked(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
		Rect * prcSrc, Rect * prcDst);
	void GetLineTopAndBottom(IVwGraphics * pvg, VwBox * pboxTarget,
		int * pyTop, int * pyBottom, Rect rcSrcRoot, Rect rcDstRoot, bool fRelativeToRoot = true,
		VwBox ** ppboxLastOnLine = NULL);
	virtual VwBox * RelinkBoxes(VwBox * pboxPrev, VwBox * pboxLim, VwBox * pboxFirst, VwBox * pboxLast);
	VwBox * GetALine(VwBox * pboxFirst, BoxVec & vbox);
	void GetBoxesInPhysicalOrder(BoxVec & vbox);
	VwBox * GetNextPhysicalBox(VwBox * pbox);
	VwBox * GetPrevPhysicalBox(VwBox * pbox);
	VwBox * GetAdjPhysicalBox(VwBox * pbox, BoxVec & vbox, int nDir);
	void CoordTransForChild(IVwGraphics * pvg, VwBox * pboxChild, Rect rcSrc, Rect rcDst,
		Rect * prcSrc, Rect * prcDst);
	void ReplaceStrings(IVwGraphics * pvg, int itssMin, int itssLim, VwParagraphBox * pvpboxRep,
		VwNotifier * pnote = NULL, NotifierVec * pvpanoteDel = NULL,BoxSet * pboxsetDeleted = NULL);
	Rect DrawSelection(IVwGraphics * pvg, int ichMin, int ichLim, bool fAssocPrev, bool fOn,
		Rect rcSrcRoot, Rect rcDstRoot, bool isInsertionPoint, bool fIsLastParaOfSelection,
		int ysTop, int dysHeight, bool fDisplayPartialLines = false);
	bool FindOverlayTagAt(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int xd, int yd,
		int * piGuid, BSTR * pbstrGuids, RECT * prc, RECT * prcAllTags,
		ComBool * pfOpeningTag);
	//void StringAndOffsetAt(int ich, ITsString ** pptss, int * pitssSub, int * pichSub);

	VwEditPropVal EditableSubstringAt(int ichMin, int ichLim, bool fAssocBefore,
		HVO * phvo, int * ptag, int * pichMin, int * pichLim,
		IVwViewConstructor ** ppvvc, int * pfrag, VwAbstractNotifier ** ppanote, int * piprop,
		VwNoteProps * pvnp, int * pitssProp, ITsString ** pptssProp);

	virtual void HiliteAll(IVwGraphics * pvg, bool fOn, Rect rcSrcRoot, Rect rcDstRoot);
	void LocOfSelection(IVwGraphics * pvg, int ichMin, int ichLim, bool fAssocPrev,
		Rect rcSrcRoot, Rect rcDstRoot, RECT * prdPrimary, RECT * prdSecondary,
		ComBool * pfSplit, bool fIsInsertionPoint, bool fIsLastParaOfSelection = true);
	void ComputeTagHeights(IVwGraphics * pvg, IVwOverlay * pxvo, int dyInch,
		int & dyTagAbove, int & dyTagBelow);
	int MaxLines();
	virtual int CLines();
	bool IsSelectionTruncated(int ich);
	// overridden to also invalidate possible overhang
	virtual Rect GetInvalidateRect();
	StrUni GetBulNumString(IVwGraphics * pvg, COLORREF * pclrUnder, int * punt);

	virtual void Search(VwPattern * ppat, IVwSearchKiller * pxserkl = NULL);
	void MakeSourceNfd();

	void WriteWpxText(IStream * pstrm);
	virtual OLECHAR * Name()
	{
		static OleStringLiteral name(L"Paragraph");
		return name;
	}
	virtual int Role()
	{
		return ROLE_SYSTEM_TEXT;
	}
	virtual StrUni Description();
	// Seems useful to indicate that paragraphs can be selected; but the IAccessible interface
	// really only allows for selecting the whole thing...
	// Enhance JohnT: should really test for the current selection being in this para, but
	// what should the response be if it is only part of the paragraph, or several paragraphs
	// including all or part of this? If we do this OR in STATE_SYSTEM_SELECTED if appropriate.
	// STATE_SYSTEM_FOCUSABLE and STATE_SYSTEM_FOCUSED might also be OR'd in if appropriate.
	virtual int State()
	{
		return STATE_SYSTEM_SELECTABLE;
	}
	// Enhance JohnT: possibly we could try to implement up and down??
	virtual VwBox * NavLeft(VwBox * pboxStart)
	{
		if (m_fParaRtl)
			return pboxStart->NextOrLazy();
		else
			return BoxBefore(pboxStart);
	}
	virtual VwBox * NavRight(VwBox * pboxStart)
	{
		if (m_fParaRtl)
			return BoxBefore(pboxStart);
		else
			return pboxStart->NextOrLazy();
	}
	// In a paragraph, don't need to redo layout of neighbor boxes. The paragraph's text will all get
	// relaid out, and embedded boxes were laid out in the full width to begin with.
	virtual void AddPrevAndFollowingToFixMap(FixupMap & fixmap, VwBox * pboxNext, VwBox * pboxPrev)
	{}
	virtual bool Relayout(IVwGraphics * pvg, int dxAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine = -1, BoxIntMultiMap * pmmbi = NULL);
	VwBox * ChildAtStringIndex(int itssTarget);
	int StringIndexOfChildBox(VwBox * pbox);

	virtual int ComputeInnerPileBaselines(VwParagraphBox * pvpboxAligner, IntVec & vdyBaselines,
		int dyBottomPrevious, int & irow, bool & fNeedAdjust);
	virtual void AdjustInnerPileBaseline(VwParagraphBox * pvpboxAligner, IntVec & vdyBaselines,
		int & irow);

	inline void SetParagraphMark(VwBoundaryMark boundaryMark)
	{
		m_BoundaryMark = boundaryMark;
		m_dxdBoundaryMark = 0;
	}
	virtual int ExtraHeightIfNotFollowedByPara(bool fIgnoreNextBox = false); // override
	virtual bool KeepWithNext(); // override
	virtual bool KeepTogether(); // override
	virtual bool ControlWidowsAndOrphans(); // override
	virtual void GetPageLines(IVwGraphics * pvg, PageLineVec & vln);
	// Next in a sequence we use for determining clipboard contents. It is like NextInRootSeq,
	// except we don't dig down inside paragraphs. Thus, we override here to pass false
	// as the last argument to NextInRootSeq.
	virtual VwBox * NextInClipSeq()
	{
		return NextInRootSeq(true, NULL, false);
	}

	// Directly set the property store, which we need to do only in one special case:
	// hiding a box that has no content.
	void _SetPropStore(VwPropertyStore * pzvps)
	{
		m_qzvps = pzvps;
	}

protected:
	void DiscardLayoutBoxes();

	static void CompareSourceStrings(VwTxtSrc * pts1, VwTxtSrc * pts2, int itss,
		int * pichwMinDiff, int * pichwLimDiff);
	int FindFirstBoxOnLine(IVwGraphics *, int ichw,
		VwBox ** ppboxStartLine, int * pdyTop, int * pdyPrevDescent,
		VwBox ** ppboxPrevLine, int * pdyPrevTop, int * pdyPrevPrevDescent);
	virtual void DoPartialLayout(IVwGraphics * pvg, VwBox * pboxStart, int cLinesToSave,
		int dyStart, int dyPrevDescent,
		int ichMinDiff, int ichLimDiff, int cchLenDiff);
	virtual void DoLayoutAux(IVwGraphics * pvg, void * pv);
	bool WsNearStartOfLine(IVwTextSource * pts, VwBox * pboxStartOfLine,
		int ichChange);

	// a group of internal methods used in drawing tags.
	void DrawOverlayTags(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, VwStringBox * psbox,
		VwOverlayFlags vof, int ydTop, int ydBottom, TagSelectInfo * ptsi, FobData & fobd);
	void DrawTags(IVwGraphics * pvg, bool fOpening, VwOverlayFlags vof,
		VecTagInfo vti, int ichMin, int ichLim, Rect rcSrc, Rect rcDst, VwStringBox * psbox,
		int ydTopText, int ydBottomText, TagSelectInfo * ptsi, FobData & fobd);
	void DrawUnderline(IVwGraphics * pvg, int xdLeft, int xdRight, int ydTop, int dxScreenPix,
		int dyScreenPix, int clrUnder, int unt, int xOffset);
	void FindGuidDiffs(ITsTextProps * pttpThis, ITsTextProps * pttpOther,
		IVwOverlay * pvo, VecTagInfo & vtiDiffs, TagSelectInfo * ptsi);
	VwBox * GetALineTB(VwBox * pboxFirst, BoxVec &vbox, int dpiY,
		int * pysTop, int * pysBottom);
	int ParaNumber();
	virtual void SetHeightAndAdjustChildren(IVwGraphics * pvg, ParaBuilder * pzpb);
#ifdef DEBUG
	bool AssertValid(void) const
	{
		return IsValid();
	}
	bool IsValid() const;
#endif

	int FindOverlayBoundary(BoxVec vbox, int ibox, bool fOpening, bool fRightToLeft,
		Rect rcSrc, Rect rcDst, IVwGraphics * pvg);
	bool FindOverlayBoundary(VwStringBox * psbox, bool fOpening, bool fRightToLeft,
		int * pxd, Rect rcSrc, Rect rcDst, IVwGraphics * pvg);
	bool NoSignificantSizeChange(int dysHeight, int dxsWidth);

public:
	int ComputeOuterWidth();
protected:
	virtual void RunParaBuilder(IVwGraphics * pvg, int dxAvailWidth, int cch);
	virtual void RunParaReBuilder(IVwGraphics * pvg, int dxAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int cch);

	// True if ys1 is further than ys2 in the direction the lines are arranged.
	virtual bool IsVerticallySameOrAfter(int ys1, int ys2)
	{
		return ys1 >= ys2;
	}

	// True if ys1 is further than ys2 in the direction the lines are arranged.
	virtual bool IsVerticallyAfter(int ys1, int ys2)
	{
		return ys1 > ys2;
	}
	// Answer whichever of the arguments is further in the direction the lines are advancing.
	virtual int VerticallyLast(int ys1, int ys2)
	{
		return std::max(ys1, ys2);
	}
	virtual void AdjustLineBoundary(bool fExactLineHeight, int & dyBottomCurr, int & dyDescentCurr,
		VwBox * pboxTmp, int dypLineHeight, int dypInch);
	virtual int InitialBoundary();
	OLECHAR GetBoundaryMarkChar();


public:
	bool AssertValid(void);
	Rect GetOuterBoundsRect(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);
	void SpellCheck();
	virtual void CountColumnsAndLines(int * pcCol, int * pcLines);
};

/*----------------------------------------------------------------------------------------------
This represents a modified paragraph that occupies only a single line, and aligns a word in
the middle of the paragraph rather than one of the margins.
Hungarian: cpb
----------------------------------------------------------------------------------------------*/
class VwConcParaBox : public VwParagraphBox
{
	typedef VwParagraphBox SuperClass;
public:
	VwConcParaBox(VwPropertyStore * pzvps, VwSourceType vst = kvstConc)
		: VwParagraphBox(pzvps, vst)
	{
	}
	void Init(int ichMinItem, int ichLimItem, int dmpAlign, VwConcParaOpts cpo);
	virtual void DoPartialLayout(IVwGraphics * pvg, VwBox * pboxStart, int cLinesToSave,
		int dyStart, int dyPrevDescent,
		int ichMinDiff, int ichLimDiff, int cchLenDiff);
	virtual void DoLayout(IVwGraphics * pvg, int dxAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);
	virtual bool Relayout(IVwGraphics * pvg, int dxAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine = -1, BoxIntMultiMap * pmmbi = NULL);
protected:
	void DoSpecialAlignment(IVwGraphics * pvg);
	int m_dmpAlign; // Original alignment position set by creator
	VwConcParaOpts m_cpo; // options
	int m_ichMinItem; // item to align and bold
	int m_ichLimItem;
};

#endif  //TEXTBOXES_INCLUDED
