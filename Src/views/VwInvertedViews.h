/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwInvertedViews.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description: Headers for various subclasses to support inverted views.

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWINVERTEDVIEWS
#define VWINVERTEDVIEWS


/*----------------------------------------------------------------------------------------------
Class: VwInvertedRootBox
Description: A root box where everything vertical is laid out from bottom to top (usually used
for vertical mongolian, where the view is rotated 90 degrees clockwise, so the inversion makes
lines advance left to right instead of right to left).
Hungarian: rootb
----------------------------------------------------------------------------------------------*/
class VwInvertedRootBox : public VwRootBox
{
	friend class VwInvertedDivMethods;
	typedef VwRootBox SuperClass;
public:
	// Static methods

	// Constructors/destructors/etc.
	VwInvertedRootBox(VwPropertyStore *pzvps);
	VwInvertedRootBox();
	virtual ~VwInvertedRootBox();
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	virtual VwEnv * MakeEnv();

	// common overrides (should be the same list as VwInvertedDivBox)
	virtual void AdjustInnerBoxes(IVwGraphics* pvg, VwSynchronizer * psync = NULL,
		BoxIntMultiMap * pmmbi = NULL, VwBox * pboxFirstNeedingInvalidate = NULL,
		VwBox * pboxLastNeedingInvalidate = NULL, bool fDoInvalidate = false);
	void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTopOfPage,
		int dysPageHeight, bool fDisplayPartialLines = false);
	// The (vertical) edge which comes first in the order the boxes are arranged.
	virtual int LeadingEdge(VwBox * pbox)
	{
		return pbox->Bottom();
	}
	// The (vertical) edge which comes last in the order the boxes are arranged.
	virtual int TrailingEdge(VwBox * pbox)
	{
		return pbox->Top();
	}
	// The top or bottom of a rectangle, whichever is in the direction of end of a page.
	virtual int TrailingEdge(fwutil::Rect rc)
	{
		return rc.top;
	}
	// The vertical edge which comes first in the order the boxes are arranged, as used for fitting boxes
	// to print on a page.
	virtual int LeadingPrintEdge(VwBox * pbox, int dypInch)
	{
		return pbox->Top() + pbox->PrintHeight(dypInch);
	}
	// The vertical edge which comes last in the order the boxes are arranged, as used for fitting boxes
	// to print on a page.
	virtual int TrailingPrintEdge(VwBox * pbox, int dypInch)
	{
		return pbox->Top();
	}
	// True if ys2 is further than or the same as ys1 in the direction the boxes are arranged.
	virtual bool IsVerticallySameOrAfter(int ys1, int ys2)
	{
		return ys1 <= ys2;
	}
	// True if ys1 is further than ys2 in the direction the boxes are arranged.
	virtual bool IsVerticallyAfter(int ys1, int ys2)
	{
		return ys1 < ys2;
	}
	virtual int ChooseSecondIfInverted(int normal, int inverted)
	{
		return inverted;
	}
};
DEFINE_COM_PTR(VwInvertedRootBox);

/*----------------------------------------------------------------------------------------------
Class: VwInvertedParaBox
Description: A paragraph box where everything vertical is laid out from bottom to top (usually used
for vertical mongolian, where the view is rotated 90 degrees clockwise, so the inversion makes
lines advance left to right instead of right to left).
Hungarian: ipb
----------------------------------------------------------------------------------------------*/
class VwInvertedParaBox : public VwParagraphBox
{
	friend class ParaBuilder;
	typedef VwParagraphBox SuperClass;
public:
	VwInvertedParaBox(VwPropertyStore * pzvps, VwSourceType vst = kvstNormal);
	virtual ~VwInvertedParaBox();
	virtual void SetHeightAndAdjustChildren(IVwGraphics * pvg, ParaBuilder * pzpb);
	virtual void DoPartialLayout(IVwGraphics * pvg, VwBox * pboxStart, int cLinesToSave,
		int dyStart, int dyPrevDescent,
		int ichMinDiff, int ichLimDiff, int cchLenDiff);
	virtual void RunParaBuilder(IVwGraphics * pvg, int dxAvailWidth, int cch);
	virtual void RunParaReBuilder(IVwGraphics * pvg, int dxAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int cch);

	// True if ys2 is further than or the same as ys1 in the direction the boxes are arranged.
	virtual bool IsVerticallySameOrAfter(int ys1, int ys2)
	{
		return ys1 <= ys2;
	}
	// True if ys1 is further than ys2 in the direction the lines are arranged.
	virtual bool IsVerticallyAfter(int ys1, int ys2)
	{
		return ys1 < ys2;
	}
	// Answer whichever of the arguments is further in the direction the lines are advancing.
	virtual int VerticallyLast(int ys1, int ys2)
	{
		return std::min(ys1, ys2);
	}
	virtual void AdjustLineBoundary(bool fExactLineHeight, int & dyBottomCurr, int & dyDescentCurr,
		VwBox * pboxTmp, int dypLineHeight, int dypInch);
	virtual int InitialBoundary();
	virtual int ChooseSecondIfInverted(int normal, int inverted)
	{
		return inverted;
	}
	// The top or bottom of a rectangle, whichever is in the direction of end of a page.
	virtual int TrailingEdge(fwutil::Rect rc)
	{
		return rc.top;
	}
};

/*----------------------------------------------------------------------------------------------
Class: VwInvertedDivBox
Description: A div box where everything vertical is laid out from bottom to top (usually used
for vertical mongolian, where the view is rotated 90 degrees clockwise, so the inversion makes
lines advance left to right instead of right to left).
Hungarian: idb
----------------------------------------------------------------------------------------------*/
class VwInvertedDivBox : public VwDivBox
{
	friend class VwInvertedDivMethods;
	typedef VwDivBox SuperClass;
public:
	VwInvertedDivBox(VwPropertyStore * pzvps);
	~VwInvertedDivBox();

	// common overrides (should be the same list as VwInvertedRootBox)
	virtual void AdjustInnerBoxes(IVwGraphics* pvg, VwSynchronizer * psync = NULL,
		BoxIntMultiMap * pmmbi = NULL, VwBox * pboxFirstNeedingInvalidate = NULL,
		VwBox * pboxLastNeedingInvalidate = NULL, bool fDoInvalidate = false);
	void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTopOfPage,
		int dysPageHeight, bool fDisplayPartialLines = false);
	// The (vertical) edge which comes first in the order the boxes are arranged.
	virtual int LeadingEdge(VwBox * pbox)
	{
		return pbox->Bottom();
	}
	// The (vertical) edge which comes last in the order the boxes are arranged.
	virtual int TrailingEdge(VwBox * pbox)
	{
		return pbox->Top();
	}
	// The top or bottom of a rectangle, whichever is in the direction of end of a page.
	virtual int TrailingEdge(fwutil::Rect rc)
	{
		return rc.top;
	}
	// The vertical edge which comes first in the order the boxes are arranged, as used for fitting boxes
	// to print on a page.
	virtual int LeadingPrintEdge(VwBox * pbox, int dypInch)
	{
		return pbox->Top() + pbox->PrintHeight(dypInch);
	}
	// The vertical edge which comes last in the order the boxes are arranged, as used for fitting boxes
	// to print on a page.
	virtual int TrailingPrintEdge(VwBox * pbox, int dypInch)
	{
		return pbox->Top();
	}
	// True if ys2 is further than or the same as ys1 in the direction the boxes are arranged.
	virtual bool IsVerticallySameOrAfter(int ys1, int ys2)
	{
		return ys1 <= ys2;
	}
	// True if ys1 is further than ys2 in the direction the boxes are arranged.
	virtual bool IsVerticallyAfter(int ys1, int ys2)
	{
		return ys1 < ys2;
	}
	virtual int ChooseSecondIfInverted(int normal, int inverted)
	{
		return inverted;
	}
};

/*----------------------------------------------------------------------------------------------
Class: VwInvertedDivMethods
Description: The methods that both VwInvertedDivBox and VwInvertedRootBox should share, but
can't because they don't have the same ancestor. This could be a mixin multiple inheritance
class, but there's only a few and that seems messy.
Hungarian: idm
----------------------------------------------------------------------------------------------*/
class VwInvertedDivMethods
{
public:
	VwDivBox * m_pdbox; // actually an inverted div or root box, but this is the common base
	VwInvertedDivMethods(VwDivBox * pdbox);
	int ComputeTopOfBoxAfter(VwBox * pboxPrev, int dypInch);
	int FirstBoxTopY(int dypInch);

	void AdjustInnerBoxes(IVwGraphics* pvg, VwSynchronizer * psync = NULL,
		BoxIntMultiMap * pmmbi = NULL, VwBox * pboxFirstNeedingInvalidate = NULL,
		VwBox * pboxLastNeedingInvalidate = NULL, bool fDoInvalidate = false);
	void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int ysTopOfPage,
		int dysPageHeight, bool fDisplayPartialLines = false);
	//bool IsVerticallyAfter(int ys, VwBox * pbox); too simple to bother with pattern.
};

/*----------------------------------------------------------------------------------------------
Class: VwInvertedPropertyStore
Description: A property store which reverses the meaning of top and bottom margins, borders,
and pad.
(Note that MswTopMargin cannot be reversed, because there is no corresponding bottom Msw margin.
Instead this is handled specially in the pile layout routines.)
Hungarian: ienv
----------------------------------------------------------------------------------------------*/
class VwInvertedPropertyStore : public VwPropertyStore
{
public:
	VwInvertedPropertyStore();
	virtual ~VwInvertedPropertyStore();
	virtual int PadTop()
	{
		return m_mpPadBottom;
	}
	virtual int PadBottom()
	{
		return m_mpPadTop;
	}
	virtual int BorderTop()
	{
		return m_mpBorderBottom;
	}
	virtual int BorderBottom()
	{
		return m_mpBorderTop;
	}
	virtual int MarginTop()
	{
		return m_mpMarginBottom;
	}
	virtual int MarginBottom()
	{
		return m_mpMarginTop;
	}
};

/*----------------------------------------------------------------------------------------------
Class: VwInvertedParaBox
Description: A VwEnv which creates the appropriate classes of inverted boxes.
Hungarian: ienv
----------------------------------------------------------------------------------------------*/
class VwInvertedEnv : public VwEnv
{
public:
	// Constructors/destructors/etc.
	VwInvertedEnv();
	virtual ~VwInvertedEnv();

	// overrides to warn of invalid operations
	//STDMETHOD(OpenDiv)();
	STDMETHOD(OpenMappedPara)();
	STDMETHOD(OpenConcPara)(int ichMinItem, int ichLimItem, VwConcParaOpts cpoFlags, int dmpAlign);
	STDMETHOD(OpenOverridePara)(int cOverrideProperties, DispPropOverride *prgOverrideProperties);
	STDMETHOD(OpenInnerPile)();
	STDMETHOD(OpenTable)(int cCols, VwLength vwlenWidth, int twBorder,
		VwAlignment vaAlign, VwFramePosition vfpFrame, VwRule vrlRule,
		int twSpacing, int twPadding, ComBool fSelectOneCol);
	STDMETHOD(AddLazyVecItems)(int tag, IVwViewConstructor * pvwvc, int frag);

	// These are the reason for the subclass!
	virtual VwParagraphBox * MakeParagraphBox(VwSourceType vst = kvstNormal);
	virtual VwDivBox * MakeDivBox();
	virtual VwPropertyStore * MakePropertyStore();
};
#endif  //VWINVERTEDVIEWS
