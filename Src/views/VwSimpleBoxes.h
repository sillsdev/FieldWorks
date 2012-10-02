/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwSimpleBoxes.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	This provides the header for the boxes used for ordinary textual and interlinear
	layouts: VwBox, VwGroupBox, VwPileBox, VwDivBox, VwLeafBox.

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWSIMPLEBOX_INCLUDED
#define VWSIMPLEBOX_INCLUDED

#include "MultiMap.h"
#include "OleStringLiteral.h"

// This function is the same as MulDiv, except that if n is not zero, neither is the result.
// It is mainly used for line widths, to make sure that a line is not
// reduced to zero pixels wide. The initial test for zero often lets us skip the multiplication,
// as most box borders are zero wide.
inline int MulDivFixZero(int n, int nMul, int nDiv)
{
	if (n == 0)
		return 0;
	int result = MulDiv(n, nMul, nDiv);
	if (result)
		return result;
	return n > 0 ? 1 : -1;
}

const int knTruncated = -100000; // used for Top() of box that is not to be laid out or drawn

typedef enum
{
	kmsrMadeSel,		// MakeSelection made a new selection, show it.
	kmsrNoSel,			// Did nothing, try an adjacent box.
	kmsrAction,			// Performed some action, such as following a hot link; do nothing more.
} MakeSelResult; // Hungarian msr

typedef enum
{
	ksdirDown = 1,
	ksdirUp = 2,
	ksdirBoth = 3,
} SearchDirection; // Hungarian sdir

// A special MultiMap used in Relayout methods.
typedef MultiMap<VwBox *, int> BoxIntMultiMap; // Hungarian mmbi;

// Forward declarations
class VwLazyBox;
class VwBox;
class VwPictureSelection;

// Struct representing one line within a divisible box being added to a page.
// It is always one row of boxes out of a paragraph having the same baseline,
// or else a single indivisible box.
// Hungarian ln.
struct PageLine
{
	VwBox * pboxFirst;
	VwBox * pboxLast;
	// for boxes in a paragraph, the top and bottom relative to the very top of the view,
	// as determined by the containing paragraph's GetLineTopAndBottom (with identity
	// coordinate transform).
	//
	int ypTopOfLine;
	int ypBottomOfLine;
	// bool fBreakAllowedAfter; // currently not supported.
};

typedef Vector<PageLine> PageLineVec; // Hungarian vln;

/*----------------------------------------------------------------------------------------------
Class: VwBox
Description:
Hungarian: box
----------------------------------------------------------------------------------------------*/
class VwBox
{
public:
	friend class VwInvertedDivMethods;
	// Static methods

	// Constructors/destructors/etc.
	VwBox(VwPropertyStore * pzvps);
	virtual ~VwBox();
protected:
	VwBox() // For use only in deserialization.
	{
	}
public:

	// Member variable access

	// Note that this does not give a reference count. If you want to keep it beyond
	// the life of this box, AddRef it.
	VwPropertyStore * Style()
	{
		return m_qzvps;
	}

	/*----------------------------------------------------------------------------------------*/
	// Serialization-related (ENHANCE JohnT: much more...)

	//every non-abstract subclass must override this and assign a new integer;
	//add a corresponding line to VwBox::makeBox().
	virtual short SerializeKey()
	{
		return 1;
	}

	/*----------------------------------------------------------------------------------------*/
	// Methods related to margin, border, padding...
	virtual int BorderLeading();
	virtual int BorderTrailing();
	virtual int BorderBottom();
	virtual int BorderTop();
	virtual int MarginLeading()
	{
		return m_qzvps->MarginLeading();
	}
	virtual int MarginTop()
	{
		return m_qzvps->MarginTop();
	}
	// Todo: write overrides for these two methods as needed.
	virtual int TotalTopMargin(int dpiY);
	virtual int TotalBottomMargin(int dpiY);

	virtual int MarginBottom()
	{
		return m_qzvps->MarginBottom();
	}
	virtual int MarginTrailing()
	{
		return m_qzvps->MarginTrailing();
	}

	virtual int PadLeading()
	{
		return m_qzvps->PadLeading();
	}

	// These take either a dyInch or dxInch and produce a result in pixels.
	// This is necessary because of the need to round the border up to force it
	// to at least one pixel (unless exactly zero). Also, to produce exact
	// consistency, each margin should be separately converted to pixels.
	// sum of margin, border, padding
	int GapTop(int dyInch)
	{
		return MulDiv(m_qzvps->MarginTop(), dyInch, kdzmpInch) +
			MulDiv(m_qzvps->PadTop(), dyInch, kdzmpInch) +
			MulDivFixZero(BorderTop(), dyInch, kdzmpInch);
	}
	// The gap at the leading edge (left normally, right in RTL boxes).
	int GapLeading(int dxInch)
	{
		return MulDiv(MarginLeading(), dxInch, kdzmpInch) +
			MulDiv(PadLeading(), dxInch, kdzmpInch) +
			MulDivFixZero(BorderLeading(), dxInch, kdzmpInch);
	}
	// The Gap strictly on the right (right even in RTL text).
	int GapRight(int dxInch)
	{
		if (Style()->RightToLeft())
			return GapLeading(dxInch);
		else
			return GapTrailing(dxInch);
	}
	// The gap strictly on the left (left even in RTL text).
	int GapLeft(int dxInch)
	{
		if (Style()->RightToLeft())
			return GapTrailing(dxInch);
		else
			return GapLeading(dxInch);
	}
	int GapBottom(int dyInch)
	{
		return MulDiv(MarginBottom(), dyInch, kdzmpInch) +
			MulDiv(m_qzvps->PadBottom(), dyInch, kdzmpInch) +
			MulDivFixZero(BorderBottom(), dyInch, kdzmpInch);
	}
	int GapTrailing(int dxInch)
	{
		return MulDiv(MarginTrailing(), dxInch, kdzmpInch) +
			MulDiv(m_qzvps->PadTrailing(), dxInch, kdzmpInch) +
			MulDivFixZero(BorderTrailing(), dxInch, kdzmpInch);
	}
	int SurroundWidth(int dxInch)
	{
		return GapLeading(dxInch) + GapTrailing(dxInch);
	}
	int SurroundHeight(int dyInch)
	{
		return GapTop(dyInch) + GapBottom(dyInch);
	}

	/*----------------------------------------------------------------------------------------*/
	// Methods for testing box types and other properties overridden by other box classes
	virtual bool IsParagraphBox()
	{
		return false;
	}
	virtual bool IsPileBox()
	{
		return false;
	}
	virtual bool IsInnerPileBox()
	{
		return false;
	}

	// True for boxes created by the layout routine, typically by breaking strings into lines.
	// Such boxes may be destroyed and created by layout() and relayout(), and are never
	// the targets of holders.
	virtual bool IsBoxFromTsString()
	{
		return false;
	}
	virtual bool IsStringBox()
	{
		return false;
	}
	virtual bool IsLazyBox()
	{
		return false;
	}
	virtual bool IsDropCapBox()
	{
		return false;
	}
	virtual bool IsMoveablePile() { return false; }
	virtual bool IsAnchor() { return false; }

	bool IsInMoveablePile()
	{
		return (IsMoveablePile() || (Container() && ((VwBox *)Container())->IsInMoveablePile()));
	}

	// True if the box cannot be logically followed by a line break.
	// Currently not true for any box, but we may decide to make it depend on
	// a style or something similar.
	// (Note however that paragraph layout uses other means to decide that
	// some text boxes can't end lines in a particular state.)
	virtual bool CannotEndLine()
	{
		return false;
	}
	// True if it is undesirable to put a page break between this box and the next.
	// Currently this just depends on the style by default unless it's the last box, except that
	// VwParagraphBox overrides to prevent breaks between a one-line drop-cap paragraph and the
	// following, overlapping paragraph.
	virtual bool KeepWithNext()
	{
		return NextOrLazy() && Style()->KeepWithNext();
	}

	// True if it is undesirable to put a page break in the middle of this box.
	// VwParagraphBox overrides to prevent breaks between lines of the paragraph.
	virtual bool KeepTogether()
	{
		return false;
	}

	// True to control single lines of this box at the top or bottom of a page. The default
	// always returns false. Override in VwParagraphBox.
	virtual bool ControlWidowsAndOrphans()
	{
		return false;
	}

	/*----------------------------------------------------------------------------------------*/
	// Methods related to box positioning and size
	int Top()
	{
		return m_ysTop;
	}
	int TopToTopOfDocument();
	int Left()
	{
		return m_xsLeft;
	}
	int LeftToLeftOfDocument();
	// Total width in source coords occupied by the box, including its left and right margins.
	int Width()
	{
		return m_dxsWidth;
	}
	int Height()
	{
		return m_dysHeight;
	}
	// Return the extra space that should be added to the height of the box.
	// This is usually 0, but might be non-zero if the box has drop-caps and is not followed by
	// a paragraph (that can wrap around part of the drop cap, e.g., if it is the last thing in
	// its pile or followed by a table).
	// If fIgnoreNextBox is false (the default), it returns non-zero only if it is indeed not
	// followed by a paragraph. If it is true, it will not make that test.
	virtual int ExtraHeightIfNotFollowedByPara(bool fIgnoreNextBox = false) { return 0; }
	int PrintHeight(int dypInch);
	virtual int FieldHeight();

	// Return the bottom of the box (used e.g. for hit testing). Compare VisibleBottom!
	int Bottom()
	{
		return m_ysTop + m_dysHeight;
	}
	// Return the visible bottom of the box. Usually the same as Bottom(), but might be different
	// from Bottom() if we display drop caps in this box and it is the last box in the pile
	// or it is not followed by a paragraph. This bottom has to do with the lowest thing in the
	// paragraph that can be seen, and should be considered when calculating clipping areas and
	// similar things: something might be drawn as far down as VisibleBottom(), even though it
	// may be larger than Bottom().
	int VisibleBottom()
	{
		return Bottom() + ExtraHeightIfNotFollowedByPara();
	}
	int Right()
	{
		return m_xsLeft + m_dxsWidth;
	}

	void Top(int ysVal)
	{
		m_ysTop = ysVal;
	}
	void Left(int xsVal)
	{
		m_xsLeft = xsVal;
	}

	//height and width are normally set by calling DoLayout(VwEnv*,int), but
	//sometimes a class dong fancy layout has the information directly
	//available for a sub-box and would rather just set it.
	void _Width(int dxsVal)
	{
		m_dxsWidth = dxsVal;
	}
	void _Height(int dysVal)
	{
		m_dysHeight = dysVal;
	}
	virtual void ForceAscentForExactLineSpacing(int dyAscent)
	{
		// Base implementation is a no-op
	}

	int Descent()
	{
		return this->Height() - this->Ascent();
	}
	// This routine returns an adjusted box descent used in figuring out line separation.
	// Most boxes return their normal descent; DropCapsStringBox overrides so the line
	// after a drop cap is not pushed down below the drop cap.
	virtual int LineSepDescent(bool fAloneOnLine)
	{
		return Descent();
	}
	virtual int Ascent();

	int Baseline()
	{
		return Top() + Ascent();
	}

	// Given the coordinate transformation for the root box, compute the one for this.
	void CoordTransFromRoot(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot, Rect * prcSrc, Rect *prcDst);

	// a full rect in the rootbox coordinate system that contains the box,
	// giving the area to invalidate if it moves or changes.
	// Subclasses which draw outside their box should override.
	virtual Rect GetInvalidateRect();

	// a full rect in the rootbox coordinate system that contains the box.
	virtual Rect GetBoundsRect(IVwGraphics * pvg, Rect rcSrcRoot, Rect rcDstRoot);

	// square of distance from box to point (0 if inside)
	int DsqToPoint(int x, int y, Rect rcSrc);

	/*----------------------------------------------------------------------------------------*/
	//:> Methods related to linking boxes to container and in a sequence

	// Use this when you know the next box is not lazy (e.g., because container is not
	// a DivBox, or you already went through the loop once).
	VwBox * Next();

	// Use this when you are prepared to handle getting back a lazy box. This is the most
	// efficient form of Next.
	VwBox * NextOrLazy()
	{
		return m_pboxNext;
	}

	// Use this to get the next real box, expanding lazy ones as necessary.
	// Try not to use this in ways that might expand everything!
	// Also, do not send this message to a box that is (or might be) a lazy box itself.
	VwBox * NextRealBox()
	{
		while (m_pboxNext && m_pboxNext->Expand())
			;
		return m_pboxNext;
	}

	// Gets the paragraph box if this is a paragraph, or if the only thing
	// it contains is a paragraph box (e.g. a paragraph box inside of a single
	// table cell inside of a single table row inside of a table).
	virtual VwParagraphBox * GetOnlyContainedPara()
	{
		return NULL;
	}

	// Gets the paragraph box if this is a paragraph, or if one of our child boxes
	// contains a paragraph box.
	virtual VwParagraphBox * GetAnyContainedPara()
	{
		return NULL;
	}

	// This is overridden by VwLazyBox; it returns false except for lazy boxes,
	// which get converted into real ones and return true.
	virtual bool Expand()
	{
		return false;
	}

	void SetNext(VwBox * pbox)
	{
		m_pboxNext = pbox;
	}
	VwGroupBox * Container()
	{
		return m_pgboxContainer;
	}
	void Container(VwGroupBox * pgbox)
	{
		m_pgboxContainer = pgbox;
	}
	VwBox * EndOfChain();

	//Fill in a set of your following boxes
	void Followers(BoxSet & boxset);

	// get a pointer (without reference count) to the root box. RootBox overrides.
	virtual VwRootBox * Root();

	//answer a set of all your containers (not including this)
	void Containers(BoxSet * pboxset);

	virtual void AddAllChildrenToSet(BoxSet & boxset) {}

	// Next box in a traversal of the boxes of the root, top down depth first.
	VwBox * NextInRootSeq(bool fReal = true, IVwSearchKiller * pxserkl = NULL,
		bool fIncludeChildren = true);
	// Similar, but processes children last to first.
	VwBox * NextInReverseRootSeq(bool fReal = true, IVwSearchKiller * pxserkl = NULL);
	// Similar, but used for selection. Virtual so that we can implement selecting only
	// one column in tables
	virtual VwBox * NextBoxForSelection(VwBox ** ppStartSearch, bool fReal = true,
		bool fIncludeChildren = true);
	// Next in a sequence we use for determining clipboard contents. It is like NextInRootSeq,
	// except we don't dig down inside paragraphs.
	virtual VwBox * NextInClipSeq()
	{
		return NextInRootSeq();
	}

	// Next in root sequence not embedded in this.
	VwBox * NextBoxAfter();

	int ComputeTopOfoxAfter(VwBox * pboxPrev);
	/*----------------------------------------------------------------------------------------*/
	// Methods related to the layout (and relayout) process. Many are stubs in VwBox.

	// Answer true if this box must be the last on its line in a paragraph.
	// The default answers false; some subclasses store a value.
	virtual bool MustEndLine();
	virtual void SetMustEndLine(bool f);

	// Lay this box out, computing positions of any sub-boxes, and the height
	// and width of this one. If its layout is flexible (e.g., paragraph), make it
	// fit in the specified width.
	virtual void DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);

	// Redo layout after changes to certain boxes which are keys in fixmap, and
	// possibly adding other boxes. Recipient may be one of the changed or added
	// boxes. If so, redo its layout.
	virtual bool Relayout(IVwGraphics * pvg, int dxAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine = -1, BoxIntMultiMap * pmmbi = NULL);

	// Pile and paragraph boxes do something more complex. The default is just to
	// answer *this and return the offset passed.
	virtual VwBox * FindNonPileChildAtOffset (int dysPosition, int dpiY, int * pdysOffsetIntoBox = NULL)
	{
		if (pdysOffsetIntoBox)
			*pdysOffsetIntoBox = dysPosition;
		return this;
	}

	virtual void GetPageLines(IVwGraphics * pvg, PageLineVec & vln);

	virtual bool StretchToPileWidth(int dxpInch, int dxpTargetWidth, int tal)
	{
		return false;
	}

	void CheckBoxMap(BoxIntMultiMap * pmmbi, VwRootBox * prootb);

	// Delete embedded boxes (overridden in VwGroupBox to delete children)
	// Also delete any notifiers for which those boxes are keys in the root.
	virtual void DeleteContents(VwRootBox * prootb, NotifierVec & vpanoteDel,
		BoxSet * pboxsetDeleted = NULL);

	void DeleteAndCleanupAndDeleteNotifiers();
	virtual void DeleteAndCleanup(NotifierVec &vec);

	// The number of lines contained in the box. Currently, only paragraphs override.
	// Arguably, piles should do, but we don't need that yet.
	virtual int CLines()
	{
		return 1;
	}

	// This is used for interlinear (and thus inner pile and paragraph override, but
	// table-related boxes do not, though plausibly they could). It is primarily
	// used for inner piles, but it's convenient to have a default to override.
	// It indicates how many rows and columns, in total, the box occupies.
	virtual void CountColumnsAndLines(int * pcCol, int * pcLines)
	{
		*pcCol = 1;
		*pcLines = 1;
	}


	// Check for owning links to dependent objects. For each such object, add it's GUID to the
	// vector. Note that this is NOT recursive, so if this box contains boxes that have dependent
	// objects, this method needs to be called directly on those boxes if desired.
	virtual void GetDependentObjects(Vector<GUID> & vguid)
	{
	}

	/*----------------------------------------------------------------------------------------*/
	// Methods related to drawing and printing the box

	// See the interesting implementation at {$VwDivBox#PrepareToDraw}
	virtual VwPrepDrawResult PrepareToDraw(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
	{
		return kxpdrNormal;
	}

	// The method usually overridden which draws the box contents. Called from draw.
	// The rectangles give the drawing transformation from the coord system of the VwGraphics
	// last used to lay the box out to the one now being used to draw it. Typically, the height
	// and width of the rectangles give units per inch (may cheat to achieve zoon). The top
	// left of rcSrc indicates the distance from the top left of the whole drawing area to
	// the top left of the container of this box. The top left of rcDst indicates how the
	// view as a whole is scrolled.
	// This routines assumes the background
	// has been drawn and also the borders.
	virtual void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	virtual void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTop,
		int dysHeight, bool fDisplayPartialLines = false);

	// Rarely overridden, this draws any border, then calls drawForeground.
	virtual void Draw(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);

	// this draws any border, then calls the drawForeground overload with the three extra arguments.
	void Draw(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTop, int dysHeight,
		bool fDisplayPartialLines = false);

	// And this actually draws the border.
	virtual void DrawBorder(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	void DebugDrawBorder(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);

	// Invalidate the part of the screen occupied by the box.
	// Subclasses needing special behavior should override GetInvalidateRect();
	void Invalidate();

	// Highlight everything in the box. Default inverts the whole box.
	virtual void HiliteAll(IVwGraphics * pvg, bool fOn, Rect rcSrcRoot, Rect rcDstRoot);

	virtual void PrintPage(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst, int ysStart, int ysEnd);
	virtual bool FindNiceBreak(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst,
		int ysStart, int * pysEnd, bool fDisregardKeeps);

	/*----------------------------------------------------------------------------------------*/
	// Methods related to mouse activity and editing.

	// Find the box that should handle a mouse event at xd, yd, given that the recipient
	// is being drawn using the coordinate transformation indicated by the rectangles.
	// Also return the source and dest rectangles that would be passed to that box
	// to draw it.
	virtual VwBox * FindBoxClicked(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
		Rect * prcSrc, Rect * prcDst);
	bool IsPointInside(int xd, int yd, Rect rcSrc, Rect rcDst);
	// Try to make a selection within yourself. Usually just calls GetSelection, then installs
	virtual MakeSelResult MakeSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
		Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, bool forceReadonly = false);
	virtual void GetSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
		Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, VwSelection ** ppvwsel,
		SearchDirection sdir = ksdirBoth);

	/*----------------------------------------------------------------------------------------*/
	//:> Miscellaneous methods.

	// Perform a search for the specified pattern in your contents. This does not have to
	// include embedded boxes for groups. VwParagraphBox and VwLazyBox override.
	// The default implementation does nothing.
	virtual void Search(VwPattern * ppat, IVwSearchKiller * pxserkl = NULL) {}

	bool IsOrFollows(VwBox * pbox);

	// Find the first box contained in the next cell of the table, or NULL if not in a table
	// cell, or in the last cell of the table.  (This obscure functionality is needed in some
	// methods of VwSelection.)
	virtual VwBox * FirstBoxInNextTableCell()
	{
		return NULL;
	}
	void PrepareFixupMap(FixupMap & fixmap, bool fIncludeThis);
	void SizeChanged();
	void SetActualTopToTop(int dyp);
	int NaturalTopToTop();
	int NaturalTopToTopAfter();
	virtual void FixSync(VwSynchronizer *psync, VwRootBox * prootb);

	// IAccessible support (called from VwAccessRoot)

	// A Name for the object. Typically derived from the class name. Mainly used for accessibility.
	virtual OLECHAR * Name()
	{
		static OleStringLiteral name(L"Box");
		return name;
	}
	// Return a count of the IAccessible children of this box.
	virtual int ChildCount()
	{
		return 0;
	}
	// Return the IAccessible nth child. Although IAccessible usually indexes from 1,
	// this method uses the usual C++ convention of accessing from 0.
	virtual VwBox * ChildAt(int index)
	{
		Assert(false);
		return NULL;
	}
	// Return the IAccessible Role of the object. Defalt just says we're part of the client
	// area of the window.
	virtual int Role()
	{
		return ROLE_SYSTEM_CLIENT;
	}
	// Return the IAccessible State of the object.
	// I can't find a generally useful default state; since it is a bit field
	// zero presumably means none of the states apply.
	virtual int State()
	{
		return 0;
	}
	// Return a description of the box.
	// Review JohnT: one possible algorithm for coming up with something more useful is to
	// default to the Description of the next box, override for groups to use the Description
	// of the first box. Paragraph already overrides to answer the contents.
	virtual StrUni Description()
	{
		StrUni stuResult;
		return stuResult; // by default boxes have no description.
	}
	// Point is in destination coords...same system as used by GetBoundsRect...
	// Return NULL if ptDst is outside bounds rect of this, otherwise,
	// return the most local direct child that contains ptDst, or this if no child
	// contains it.
	virtual VwBox * FindBoxContaining(Point ptDst, IVwGraphics * pvg, Rect rcSrcRoot,
		Rect rcDstRoot)
	{
		if (GetBoundsRect(pvg, rcSrcRoot, rcDstRoot).Contains(ptDst))
			return this;
		else
			return NULL;
	}

	// Most boxes can't yet save an accessible name. Rootbox overrides.
	virtual void SetAccessibleName(BSTR bstrName)
	{
	}

	virtual int ComputeInnerPileBaselines(VwParagraphBox * pvpboxAligner, IntVec & vdyBaselines,
		int dyBottomPrevious, int & irow, bool & fNeedAdjust);
	virtual void AdjustInnerPileBaseline(VwParagraphBox * pvpboxAligner, IntVec & vdyBaselines,
		int & irow);
	// The top or bottom of a rectangle, whichever is in the direction of end of a page.
	virtual int TrailingEdge(fwutil::Rect rc)
	{
		return rc.bottom;
	}
	// Choose the first value in the pair for normal operation, the second in an inverted paragraph.
	// (Various kinds of inverted box override to choose the second value.)
	virtual int ChooseSecondIfInverted(int normal, int inverted)
	{
		return normal;
	}

protected:
	// Member variables
	VwPropertyStorePtr m_qzvps;   //smart pointer to style info
	int m_ysTop;		// relative to container
	int m_xsLeft;		// relative to container
	int m_dxsWidth;		// source coords
	int m_dysHeight;	// source coords
	VwBox * m_pboxNext;				//links chain of boxes in container
	VwGroupBox * m_pgboxContainer;	//link up hierarchy

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods

	// Debug-only:
#ifdef DEBUG
public:

	virtual bool AssertValid(void)
	{
		AssertPtr(m_qzvps.Ptr());
		AssertPtrN(m_pboxNext);
		AssertPtrN(m_pgboxContainer);
		return true;
	}
#endif // DEBUG
};

/*----------------------------------------------------------------------------------------------
Class: VwGroupBox
Description:
Hungarian: gbox
----------------------------------------------------------------------------------------------*/
class VwGroupBox : public VwBox
{
	friend class VwInvertedDivMethods;
	typedef VwBox SuperClass;
protected:
	VwGroupBox()
		:VwBox()
	{
		m_pboxFirst = NULL;
		m_pboxLast = NULL;
	}
public:
	VwGroupBox(VwPropertyStore * pzvps)
		:VwBox(pzvps), m_pboxFirst(0), m_pboxLast(0)
	{
	}
	virtual ~VwGroupBox();

	virtual short serializeKey() {return 6;}

	// Apply the function to each box in the group. Lazy ones are not expanded.
	// Do NOT use this for operations that might delete the boxes!
	template<class Op> void ForEachChild(Op& f)
	{
		for (VwBox * pbox = m_pboxFirst; pbox; pbox = pbox->NextOrLazy())
		{
			f(pbox);
		}
	}

	// Add a box to the group during initial construction.
	// A different method should be used once the box is
	// laid out and may need to be recomputed and redrawn
	virtual void Add(VwBox * pbox);

	// Return the last box in the group. May return a lazy box or null.
	VwBox * LastBox() {return m_pboxLast;}
	// Return the last real box in the group. May not return lazy box. May return null.
	// Override for classes that could have lazy boxes.
	virtual VwBox * LastRealBox() {return m_pboxLast;}
	// Return the first box in the group. May return lazy box or null.
	VwBox * FirstBox() {return m_pboxFirst;}
	// Return the first real box in the group. May not return lazy box. May return null.
	// Override for classes that could have lazy boxes.
	virtual VwBox * FirstRealBox() {return m_pboxFirst;}
	// Gets the paragraph box if this is a paragraph, or if the only thing
	// it contains is a paragraph box (e.g. a paragraph box inside of a single
	// table cell inside of a single table row inside of a table).
	virtual VwParagraphBox * GetOnlyContainedPara(); // override
	// Gets the paragraph box if this is a paragraph, or if one of our child boxes
	// contains a paragraph box.
	virtual VwParagraphBox * GetAnyContainedPara(); // override

	// Set the first and last box directly: only used in regeneration code, this bypasses
	// important normal side effects of inserting boxes.
	void _SetFirstBox(VwBox * pbox) {m_pboxFirst = pbox;}
	void _SetLastBox(VwBox * pbox) {m_pboxLast = pbox;}

	virtual void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	virtual void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTop,
		int dysHeight, bool fDisplayPartialLines = false);

	// methods related to notifiers and regenerating the display
	void GetNotifiers(VwBox * pboxChild, NotifierVec& notevec);
	virtual void DeleteContents(VwRootBox * prootb, NotifierVec & vpanoteDel,
		BoxSet * pboxsetDeleted = NULL);

	virtual void DeleteAndCleanup(NotifierVec &vpanoteDel);

	VwBox * RelinkBoxes(VwBox * pboxPrev, VwBox * pboxLim, VwBox * pboxFirst, VwBox * pboxLast);

	// other methods
	virtual void GetSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
		Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, VwSelection ** ppvwsel,
		SearchDirection sdir = ksdirBoth);
	VwBox * RemoveAllBoxes();
	void RestoreBoxes(VwBox * pboxFirst, VwBox * pboxLast);
	bool Contains(VwBox * pbox, VwBox ** ppboxSub = NULL);
	// Box before this one in chain, or null if pboxSub is the first. Assert if not found.
	// May return lazy box or null.
	VwBox * BoxBefore(VwBox * pboxSub);
	// Box before this one in chain which is NOT lazy. Override on group boxes that can
	// contain lazy ones.
	virtual VwBox * RealBoxBefore(VwBox * pboxSub) {return BoxBefore(pboxSub);}
	virtual VwBox * NextBoxForSelection(VwBox ** ppStartSearch, bool fReal = true,
		bool fIncludeChildren = true); // override

	// remove any following extensions; subclasses which can be followed by extension boxes
	// should override.
	virtual void RemoveExtensions()
	{
	}
	virtual VwBox * FindBoxClicked(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
		Rect * prcSrc, Rect * prcDst);

	// Given a coordinate transformation that would be used to draw the recipient,
	// figure the one for the specified child.
	virtual void CoordTransForChild(IVwGraphics * pvg, VwBox * pboxChild, Rect rcSrc, Rect rcDst,
		Rect * prcSrc, Rect * prcDst);

	virtual void HiliteAllChildren(IVwGraphics * pvg, bool fOn, Rect rcSrcRoot, Rect rcDstRoot);
	VwNotifier * GetLowestNotifier(VwBox * pboxChild);
	virtual OLECHAR * Name()
	{
		static OleStringLiteral name(L"Group");
		return name;
	}
	virtual int ChildCount();
	virtual VwBox * ChildAt(int index);
	virtual int Role()
	{
		return ROLE_SYSTEM_GROUPING;
	}
	// By default we don't know how to navigate.
	// These are currently implemented to support up and down in piles,
	// left and right in paragraphs (in the simple-minded sense that
	// right means the next box in a left-to-right para, even if it is on
	// the next line).
	// Enhance JohnT: there are obvious interpretations for the various
	// Table classes. Paragraph could implement up and down. Pile could
	// implement left and right by testing to see whether it is in a table.
	virtual VwBox * NavDown(VwBox * pboxStart)
	{
		return NULL;
	}
	virtual VwBox * NavLeft(VwBox * pboxStart)
	{
		return NULL;
	}
	virtual VwBox * NavRight(VwBox * pboxStart)
	{
		return NULL;
	}
	virtual VwBox * NavUp(VwBox * pboxStart)
	{
		return NULL;
	}
	virtual VwBox * FindBoxContaining(Point ptDst, IVwGraphics * pvg, Rect rcSrcRoot,
		Rect rcDstRoot);

	virtual void AddAllChildrenToSet(BoxSet & boxset);

	static VwGroupBox * CommonContainer(VwBox * pboxFirst, VwBox * pboxLast);

	virtual void AddPrevAndFollowingToFixMap(FixupMap & fixmap, VwBox * pboxNext, VwBox * pboxPrev);

	virtual int AvailWidthForChild(int dpiX, VwBox * pboxChild);

protected:
	VwBox * m_pboxFirst;  //start of linked list of contained boxes
	VwBox* m_pboxLast;    //last one in the chain (but see special case in VwParagraphBox)

#ifdef DEBUG
public:

	virtual bool AssertValid(void)
	{
		VwBox * pboxPrev = NULL;
		for(VwBox * pbox = m_pboxFirst; pbox; pbox = pbox->NextOrLazy())
		{
			AssertObj(pbox);
			Assert(pbox->Container() == this);
			pboxPrev = pbox;
		}
		Assert(m_pboxLast == pboxPrev);
		return true;
	}
#endif // DEBUG

};

/*----------------------------------------------------------------------------------------------
Class: VwPileBox
Description:
Hungarian: boxp
----------------------------------------------------------------------------------------------*/
class VwPileBox : public VwGroupBox
{
	typedef VwGroupBox SuperClass;
	friend class VwBox; //can use protected constructor
	friend class VwLazyBox;  // can adjust positions of things directly when expanding

	// constructors, destructor
protected:
	VwPileBox()
		:VwGroupBox()
	{
	}

	virtual void AdjustInnerBoxes(IVwGraphics* pvg, VwSynchronizer * psync = NULL,
		BoxIntMultiMap * pmmbi = NULL, VwBox * pboxFirstNeedingInvalidate = NULL,
		VwBox * pboxLastNeedingInvalidate = NULL, bool fDoInvalidate = false);
public:
	VwPileBox(VwPropertyStore * pzvps)
		:VwGroupBox(pzvps)
	{
	}

	virtual short SerializeKey() {return 5;}

	// layout overrides
	virtual void DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);

	// other methods
	virtual void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTopOfPage,
		int dysPageHeight, bool fDisplayPartialLines = false);
	int FirstBoxTopY(int dypInch);
	virtual int SyncedComputeTopOfBoxAfter(VwBox * pboxCurr, int dypInch,
		VwRootBox * prootb, VwSynchronizer * psync);
	virtual int SyncedFirstBoxTopY(int dypInch, VwRootBox * prootb, VwSynchronizer * psync);
	virtual bool IsPileBox() { return true; }
	virtual VwBox * FindBoxClicked(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
		Rect * prcSrc, Rect * prcDst);
	virtual int FieldHeight();
	int ComputeTopOfBoxAfter(VwBox * pboxPrev, int dypInch);
	bool SetActualTopToTop(VwBox * pboxFix, int dyp);
	bool SetActualTopToTopAfter(VwBox * pboxFix, int dyp);
	virtual OLECHAR * Name()
	{
		static OleStringLiteral name(L"Pile");
		return name;
	}
	virtual VwBox * NavDown(VwBox * pboxStart)
	{
		return pboxStart->NextOrLazy();
	}
	virtual VwBox * NavUp(VwBox * pboxStart)
	{
		return BoxBefore(pboxStart);
	}
	virtual bool FindNiceBreak(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst,
		int ysStart, int * pysEnd, bool fDisregardKeeps);
	virtual VwBox * FindNonPileChildAtOffset (int dysPosition, int dpiY, int * pdysOffsetIntoBox = NULL);
	void AdjustLeft(VwBox * pbox, int dxLeft, int dxpInnerAvailWidth);
	// The (vertical) edge which comes first in the order the boxes are arranged.
	virtual int LeadingEdge(VwBox * pbox)
	{
		return pbox->Top();
	}
	// The (vertical) edge which comes last in the order the boxes are arranged.
	virtual int TrailingEdge(VwBox * pbox)
	{
		return pbox->Bottom();
	}
	// Seems to be necessary to repeat this here? Though inherited from VwBox?? Otherwise compiler complains
	// about converting fwutil::Rect to VwBox for the other overload.
	virtual int TrailingEdge(fwutil::Rect rc)
	{
		return rc.bottom;
	}

	// The vertical edge which comes first in the order the boxes are arranged, as used for fitting boxes
	// to print on a page.
	virtual int LeadingPrintEdge(VwBox * pbox, int dysInch)
	{
		return pbox->Top();
	}
	// The vertical edge which comes last in the order the boxes are arranged, as used for fitting boxes
	// to print on a page.
	virtual int TrailingPrintEdge(VwBox * pbox, int dypInch)
	{
		return pbox->Top() + pbox->PrintHeight(dypInch);
	}
	// True if ys1 is further than or the same as ys2 in the direction the boxes are arranged.
	virtual bool IsVerticallySameOrAfter(int ys1, int ys2)
	{
		return ys1 >= ys2;
	}
	// True if ys1 is further than ys2 in the direction the boxes are arranged.
	virtual bool IsVerticallyAfter(int ys1, int ys2)
	{
		return ys1 > ys2;
	}
	// True if ys is after the trailing edge of the (child) box.
	bool IsVerticallyAfter(int ys, VwBox * pbox)
	{
		return IsVerticallyAfter(ys, TrailingEdge(pbox));
	}
	virtual void GetPageLines(IVwGraphics * pvg, PageLineVec & vln);
};

/*----------------------------------------------------------------------------------------------
Class: VwDivBox
Description: VwDivBox is currently no different from VwPileBox.
Hungarian: dbox
----------------------------------------------------------------------------------------------*/
class VwDivBox : public VwPileBox
{
	friend class VwBox; //can use protected constructor
	friend class VwLazyBox;   // can adjust positions of things directly when expanding

	// constructors, destructor
protected:
	VwDivBox() :VwPileBox()
	{
	}
public:
	VwDivBox(VwPropertyStore * pzvps)
		:VwPileBox(pzvps)
	{
	}
	virtual short SerializeKey() {return 4;}
	virtual bool Relayout(IVwGraphics * pvg, int dxAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine = -1, BoxIntMultiMap * pmmbi = NULL);
	virtual void PrintPage(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst, int ysStart, int ysEnd);
	virtual VwPrepDrawResult PrepareToDraw(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	virtual VwBox * LastRealBox();
	virtual VwBox * FirstRealBox();
	virtual VwBox * RealBoxBefore(VwBox * pboxSub);
	void ExpandFully();
	virtual OLECHAR * Name()
	{
		static OleStringLiteral name(L"Div");
		return name;
	}
	virtual void AddPrevAndFollowingToFixMap(FixupMap & fixmap, VwBox * pboxNext, VwBox * pboxPrev);
};

/*----------------------------------------------------------------------------------------------
Class: VwInnerPileBox
Description: VwInnerPileBox is currently little different from regular piles
Hungarian: boxpin
----------------------------------------------------------------------------------------------*/
class VwInnerPileBox : public VwPileBox
{
	typedef VwPileBox SuperClass;

protected:
	// Redo layout after changes to certain boxes which are keys in fixmap, and
	// possibly adding other boxes. Recipient may be one of the changed or added
	// boxes. If so, or if m_fRedoRequiresLayout is true, redo its layout.
	virtual bool Relayout(IVwGraphics * pvg, int dxAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine = -1, BoxIntMultiMap * pmmbi = NULL);

	bool m_fRelayoutRequiresLayout;

	friend class VwBox; //allows deserialization to use zero-argument constructor
	VwInnerPileBox() :VwPileBox()
	{
	}
	void AdjustBorderedParas(IVwGraphics * pvg);
public:
	virtual short SerializeKey() {return 7;}
	VwInnerPileBox(VwPropertyStore * pzvps)
		: VwPileBox(pzvps)
	{
	}
	void ComputeAscent()
	{
		m_dysAscent = (m_pboxFirst) ? (m_pboxFirst->Ascent() + m_pboxFirst->Top()) : VwPileBox::Ascent();
	}
	int Ascent()
	{
		return m_dysAscent;
	}
	virtual void ForceAscentForExactLineSpacing(int dyAscent)
	{
		m_dysAscent = dyAscent;
	}

	//	Convenient for debugging:
	virtual bool IsInnerPileBox()
	{
		return true;
	}
	virtual OLECHAR * Name()
	{
		static OleStringLiteral name(L"InnerPile");
		return name;
	}
	// Don't try to break inner piles across pages.
	virtual bool FindNiceBreak(VwPrintInfo * pvpi, Rect rcSrc, Rect rcDst,
		int ysStart, int * pysEnd, bool fDisregardKeeps)
	{return false;}

	void SetRelayoutRequiresLayout()
	{
		m_fRelayoutRequiresLayout = true;
	}
	virtual int ComputeInnerPileBaselines(VwParagraphBox * pvpboxAligner, IntVec & vdyBaselines,
		int dyBottomPrevious, int & irow, bool & fNeedAdjust);
	virtual void AdjustInnerPileBaseline(VwParagraphBox * pvpboxAligner, IntVec & vdyBaselines,
		int & irow);
	virtual void DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);
	virtual bool StretchToPileWidth(int dxpInch, int dxpTargetWidth, int tal);
	virtual int SyncedComputeTopOfBoxAfter(VwBox * pboxCurr, int dypInch,
		VwRootBox * prootb, VwSynchronizer * psync);
	virtual int SyncedFirstBoxTopY(int dypInch, VwRootBox * prootb, VwSynchronizer * psync);
	void AdjustBoxHorizontalPositions(int dxpInch);
	// The ascent of the inner pile. This allows paragraphs with exact line spacing to override.
	// In other paragraphs, it basically doesn't matter, and can remain zero.
	int m_dysAscent;
	virtual void CountColumnsAndLines(int * pcCol, int * pcRow);
};

class IVwNotifier;
/*----------------------------------------------------------------------------------------------
Class: VwMoveablePileBox
Description: This class represents an embedded object with kodtGuidMoveableObjDisp. It contains
whatever was produced by the DisplayEmbeddedObject call. Unlike an inner pile, it always
occupies a complete line in the paragraph, initially immediately following the line that
contains the ORC that functions as its 'anchor'. However, it may be moved down a number of
lines in order to make better page breaks. Eventually we may also wrap stuff around it.
Hungarian: mpbox
----------------------------------------------------------------------------------------------*/
class VwMoveablePileBox : public VwInnerPileBox
{
	friend class VwBox; //allows deserialization to use zero-argument constructor
	VwMoveablePileBox() : VwInnerPileBox()
	{
	}
public:
	virtual bool IsBoxFromTsString() { return true; }
	virtual bool IsMoveablePile() { return true; }
	virtual short SerializeKey() {return 77;}
	VwMoveablePileBox(VwPropertyStore * pzvps)
		: VwInnerPileBox(pzvps)
	{
	}
	GUID * Guid() { return &m_guid; }
	void SetGuid(GUID * pguid) { m_guid = *pguid; }
	VwParagraphBox * OriginalContainer() { return m_pvpboxOriginalContainer; }
	void SetOriginalContainer(VwParagraphBox * pvpbox) { m_pvpboxOriginalContainer = pvpbox; }
	IVwViewConstructor * Vc() { return m_pvc; }
	void SetVc(IVwViewConstructor * pvc) { m_pvc = pvc; }
	VwNotifier * Notifier() { return m_pnote; }
	void SetNotifier(VwNotifier * pnote) { m_pnote = pnote; }
	int ParentPropIndex() { return m_iprop; }
	void SetParentPropIndex(int iprop) { m_iprop = iprop; }
	int Alternative() { return m_ws; }
	void SetAlternative(int ws) { m_ws = ws; }
	int CharIndex() { return m_ich; }
	void SetCharIndex(int ich) { m_ich = ich; }

protected:
	GUID m_guid; // The GUID from which it was created.
	// The paragraph box that it was part of, before being moved to improve page breaks.
	VwParagraphBox * m_pvpboxOriginalContainer;
	// The view constructor that created the mp box. We never use this pointer to call methods,
	// just to compare with the one we want for reusing it, so we don't need a smart pointer.
	IVwViewConstructor * m_pvc;
	// The notifier that is the parent for anything contained. Again, this is only used
	// to determine whether the box can be reused, so it doesn't matter if the target is
	// deleted.
	VwNotifier * m_pnote;
	// Index of the property within m_pnote that contains the string having the ORC.
	int m_iprop;
	// If the ORC is in a multistring alternative, indicates the alternative, otherwise 0.
	int m_ws;
	// LOGICAL (not rendering) index of the character within the STRING (not paragraph).
	int m_ich;
};

/*----------------------------------------------------------------------------------------------
Class: VwLeafBox
Description:
Hungarian: lbox
----------------------------------------------------------------------------------------------*/

class VwLeafBox : public VwBox
{
protected:
	friend class VwBox; //allows deserialization to use zero-argument constructor
	VwLeafBox()
		:VwBox()
	{
	}

public:
	VwLeafBox(VwPropertyStore * pzvps)
		:VwBox(pzvps)
	{
	}
	virtual short SerializeKey() {return 2;}
	virtual OLECHAR * Name()
	{
		static OleStringLiteral name(L"Leaf");
		return name;
	}
	int GetCharPosition();
};

/*----------------------------------------------------------------------------------------------
Class: VwAnchorBox
Description: This special class is used to 'anchor' a VwMappedPileBox. It is inserted into the
paragraph at the character position where the ORC would be displayed. Currently its main
purpose is to keep track of the existence of the mpb; eventually, we may also use it to display
an anchor.
Hungarian: abox
----------------------------------------------------------------------------------------------*/
class VwAnchorBox : public VwLeafBox
{
	friend class VwBox;
	VwAnchorBox() : VwLeafBox() {}
public:
	VwAnchorBox(VwPropertyStore * pzvps, VwMoveablePileBox * pmpbox) : VwLeafBox(pzvps)
	{
		m_pmpbox = pmpbox;
	}
	~VwAnchorBox() {}
	virtual short SerializeKey() {return 78;}
	virtual bool IsBoxFromTsString() { return true; }
	virtual bool IsAnchor() { return true; }
	VwMoveablePileBox * MoveablePile() { return m_pmpbox; }
	// No need to override DoLayout, deliberately leave height and width zero; ascent is
	// also zero, as it defaults to height.

protected:
	VwMoveablePileBox * m_pmpbox;
};

class VwSeparatorBox : public VwLeafBox
{
protected:
	friend class VwBox; //allows deserialization to use zero-argument constructor
	VwSeparatorBox() :VwLeafBox()
	{
	}
public:
	VwSeparatorBox(VwPropertyStore * pzvps)
		:VwLeafBox(pzvps)
	{
	}
	virtual ~VwSeparatorBox();
	virtual void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	virtual void DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);
	virtual int Ascent()
	{
		return m_dysAscent;
	}
	virtual OLECHAR * Name()
	{
		static OleStringLiteral name(L"Separator");
		return name;
	}
	virtual int Role()
	{
		return ROLE_SYSTEM_SEPARATOR;
	}
protected:
	int m_dysAscent;
};

/*----------------------------------------------------------------------------------------------
Class: VwBarBox
Description: An arbitrary rectangular box, allowing the color to be specified, also height,
width, and baseline offset. Currently used in format previews.
A baseline offset of zero aligns the bottom of the colored rectangle with the current text
baseline. Positive values raise it, negative lower it.
Hungarian: bbox
----------------------------------------------------------------------------------------------*/
class VwBarBox : public VwLeafBox
{
protected:
	friend class VwBox; //allows deserialization to use zero-argument constructor
	VwBarBox() :VwLeafBox()
	{
	}
public:
	VwBarBox(VwPropertyStore * pzvps, COLORREF rgb, int dmpWidth, int dmpHeight, int dmpBaselineOffset)
		:VwLeafBox(pzvps)
	{
		m_rgbColor = rgb;
		m_dmpBaselineOffset = dmpBaselineOffset;
		m_dmpHeight = dmpHeight;
		m_dmpWidth = dmpWidth;
	}
	virtual ~VwBarBox();
	virtual void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	virtual void DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);
	virtual int Ascent()
	{
		return m_dysAscent;
	}
	virtual OLECHAR * Name()
	{
		static OleStringLiteral name(L"Bar");
		return name;
	}
	virtual int Role()
	{
		return ROLE_SYSTEM_SEPARATOR;
	}
protected:
	COLORREF m_rgbColor;
	int m_dmpBaselineOffset;
	int m_dmpHeight;
	int m_dmpWidth;
	int m_dysAscent;

};


DEFINE_COM_PTR(IPicture);
DEFINE_COM_PTR(IComDisposable);

class VwPictureBox : public VwLeafBox
{
protected:
	friend class VwBox; //allows deserialization to use zero-argument constructor
	VwPictureBox() :VwLeafBox()
	{
	}
public:
	VwPictureBox(VwPropertyStore * pzvps, IPicture * ppic, int dxmpWidth = 0,
		int dympHeight = 0, VwParagraphBox * pvpbox = NULL, SmartBstr sbstrObjData = (const wchar_t*)NULL)
		:VwLeafBox(pzvps)
	{
		m_qpic = ppic;
		Assert(m_qpic);
		m_dxmpWidth = dxmpWidth;
		m_dympHeight = dympHeight;
		m_pvpbox = pvpbox;
		m_sbstrObjData = sbstrObjData;
	}
	virtual ~VwPictureBox();
	virtual void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	virtual void DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);
	virtual int Ascent() {return m_dysAscent;}

	void DoHotLink(VwPictureSelection * pPicSel);
	void GetSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
		Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, VwSelection ** ppvwsel,
		SearchDirection sdir = ksdirBoth);

	SmartBstr ObjectData()
	{
		return m_sbstrObjData;
	}

	VwParagraphBox * Paragraph()
	{
		return m_pvpbox;
	}

	virtual void ForceAscentForExactLineSpacing(int dyAscent)
	{
		m_dysAscent = dyAscent;
	}
	// ENHANCE JohnT: when we implement direct insertion of pictures into the view,
	// so they are not part of a TsString, we will need a subclass
	// or member variable to handle the possibility they are not "from TsString".
	virtual bool IsBoxFromTsString()
	{
		return true;
	}

	// Derived classes should override this method if their picture box should be
	// centered or right aligned within the available width.
	virtual int AdjustedLeft()
	{
		return 0;
	}
	virtual void Search(VwPattern * ppat, IVwSearchKiller * pxserkl = NULL);
	virtual OLECHAR * Name()
	{
		static OleStringLiteral name(L"Picture");
		return name;
	}
	virtual int Role()
	{
		return ROLE_SYSTEM_GRAPHIC;
	}

	// Check for owning links to dependent objects. For each such object, add it's GUID to the
	// vector. Note that this is NOT recursive, so if this box contains boxes that have dependent
	// objects, this method needs to be called directly on those boxes if desired. Picture boxes
	// can contain dependent objects because they can be "hot".
	virtual void GetDependentObjects(Vector<GUID> & vguid);

protected:
	int m_dysAscent;
	IPicturePtr m_qpic;
	// height and width of the actual bitmap in pixels.
	int m_cpixWidth;
	int m_cpixHeight;
	// Width adjusted to be Dword-aligned for Bitmap
	int m_widthDW;
	// If negative, these are absolute; if positive, they are maximums.
	// If both are zero, use the natural size of the picture.
	// Do not specify one positive and one negative.
	// If both are negative, the picture may be distorted.
	// If both are positive, aspect ratio is preserved.
	// The available display width functions as an additional bound on width.
	int m_dxmpWidth;
	int m_dympHeight;
	SmartBstr m_sbstrObjData;
	VwParagraphBox * m_pvpbox;
};

// A picture that ISN'T part of a string.
class VwIndepPictureBox : public VwPictureBox
{
	typedef VwPictureBox SuperClass;

public:
	VwIndepPictureBox()	:VwPictureBox()
	{
		m_dxsOffset = 0;
	}

	VwIndepPictureBox(VwPropertyStore * pzvps, IPicture * ppic, int dxmpWidth = 0, int dympHeight = 0)
		:VwPictureBox(pzvps, ppic, dxmpWidth, dympHeight)
	{
		m_dxsOffset = 0;
	}

	virtual void DoLayout(IVwGraphics* pvg, int dxAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);
	virtual bool IsBoxFromTsString()
	{
		return false;
	}

	virtual int AdjustedLeft()
	{
		return m_dxsOffset;
	}

protected:
	int m_dxsOffset;	// How far to offset the left drawing position for centering or
						// right aligning.
};

// A picture that is used to implement AddIntPropPic.
class VwIntegerPictureBox : public VwIndepPictureBox
{
	typedef VwIndepPictureBox SuperClass;

public:
	VwIntegerPictureBox()	:VwIndepPictureBox()
	{
	}

	VwIntegerPictureBox(VwPropertyStore * pzvps, IPicture * ppic, HVO hvo, PropTag tag,
		int nValMin, int nValMax)
		:VwIndepPictureBox(pzvps, ppic)
	{
		m_dxsOffset = 0;
		m_nValMin = nValMin;
		m_nValMax = nValMax;
		m_hvo = hvo;
		m_tag = tag;
	}

	virtual MakeSelResult MakeSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
		Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, bool forceReadonly = false);

	int MinVal() { return m_nValMin; }
	int MaxVal() { return m_nValMax; }


protected:
	int m_nValMin; // min of range to cycle through
	int m_nValMax;
	HVO m_hvo;
	PropTag m_tag;
};

#endif  //VWSIMPLEBOX_INCLUDED
