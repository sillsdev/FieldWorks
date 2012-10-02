/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwLazyBox.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Header for lazy boxes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWLAZYBOX_INCLUDED
#define VWLAZYBOX_INCLUDED

#include "OleStringLiteral.h"

class LazinessIncreaser;

/*----------------------------------------------------------------------------------------------
This class holds hvos and estimated heights of items in a VwLazyBox.
@h3{Hungarian: vwlzifghsylsdfgdhlkjhnfghbppd}
----------------------------------------------------------------------------------------------*/
class VwLazyInfo
{
	HvoVec m_vhvo;
	LongVec m_vloEstimatedHeight;

public:

	void Replace(int iHvoMin, int iHvoLim, HVO * prghvoItems, int chvoItems);
	void CopyRange(int ihvoMin, int ihvoLim, VwLazyInfo & vwlzi);
	void Push(HVO hvo);
	void EnsureSpace(int cItems);
	HVO * BeginHvo();
	int Size();
	HVO GetHvo(int iHvo);
	long GetEstimatedHeight(int iEstHeight);
	void SetEstimatedHeight(int iEstHeight, long estHeight);
};

/*----------------------------------------------------------------------------------------------
This class implements a "lazy" box, that is, one that can't be actually drawn, but which
instead gets expanded as part of the process of preparing to draw (or other operations
that need real box data).
@h3{Hungarian: lzb}
----------------------------------------------------------------------------------------------*/
class VwLazyBox : public VwBox
{
	typedef VwBox SuperClass;
	friend class VwBox; // can use protected constructor.
	friend class VwEnv; // can manipulate to initialize.
	friend class VwNotifier; // tweaks when regenerating properties.
	friend class LazinessIncreaser;
protected:
	VwLazyBox(VwLazyBox & lzbOrig, int chvoItems, int ihvoMin);
	VwLazyBox();
public:
	// Static methods

	// Constructors/destructors/etc.
	VwLazyBox(VwPropertyStore * pzvps, HVO * prghvoItems, int chvoItems, int ihvoMin,
		IVwViewConstructor * pvc, int frag, HVO hvoContext);
	virtual ~VwLazyBox();
	/* ENHANCE JohnT: make one up, when we implement.
	virtual short SerializeKey()
	{
		return 12;
	}
	*/
	virtual void DrawBorder(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	virtual void DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst);
	virtual bool IsLazyBox()
	{
		return true;
	}

	// Other public methods
	void GetItemsToExpand(int ydTopClip, int ydBottomClip, Rect rcSrc, Rect rcDst,
		int * pihvoMin, int * pihvoLim);
	int ItemHeight(int iItem) { return m_vwlziItems.GetEstimatedHeight(iItem); }
	int CItems() { return m_vwlziItems.Size(); }
	virtual void DoLayout(IVwGraphics* pvg, int dxsAvailWidth, int dxpAvailOnLine = -1, bool fSyncTops = false);
	virtual bool Relayout(IVwGraphics * pvg, int dxsAvailWidth, VwRootBox * prootb,
		FixupMap * pfixmap, int dxpAvailOnLine = -1, BoxIntMultiMap * pmmbi = NULL);

	virtual bool Expand();
	VwBox * FindBoxClicked(IVwGraphics * pvg, int xd, int yd, Rect rcSrc, Rect rcDst,
		Rect * prcSrc, Rect * prcDst); // override
	void ExpandItems(int ihvoMin, int ihvoLim, bool * pfForcedScroll = NULL);
	void ExpandForDisplay(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int dxsAvailWidth);
	HVO Object() {return m_hvoContext;}
	int MinObjIndex() {return m_ihvoMin;}
	int LimObjIndex() {return m_ihvoMin + m_vwlziItems.Size();}

	VwBox * ExpandItemsNoLayout(int ihvoMin, int ihvoLim,
		VwBox ** ppboxFirstLayout, VwBox ** ppboxLimLayout);
	VwBox * ExpandItemsNoLayout(int ihvoMin, int ihvoLim, VwNotifier * pnoteMyNotifier,
		int ipropBest, int tag, VwBox ** ppboxFirstLayout, VwBox ** ppboxLimLayout);
	static void LayoutExpandedItems(VwBox * pboxFirstLayout, VwBox * pboxLimLayout,
		VwDivBox * pdboxContainer, bool fSyncTops = false);

	virtual OLECHAR * Name()
	{
		static OleStringLiteral name(L"Lazy");
		return name;
	}
	// This state indicates that the box is invisible and not able to work normally.
	// It seems a good thing for a lazy box to indicate.
	virtual int State()
	{
		return STATE_SYSTEM_INVISIBLE;
	}

	// Get the nth item
	HVO NthItem(int i) { return m_vwlziItems.GetHvo(i); }

	// Lazy boxes can't reliably get the next real box.
	VwBox * NextRealBox()
	{
		Assert(false);
		return m_pboxNext;
	}

	// We should never get a real click in a lazy box, so this is a dubious event that I(JT)
	// consider worthy of a warning. However, it CAN happen legitimately, for example,
	// a simulated click to create a selection for scrolling to a particular position,
	// which may be done before the screen is painted and lazy boxes expanded.
	virtual void GetSelection(IVwGraphics * pvg, VwRootBox * prootb, int xd, int yd,
			Rect rcSrcRoot, Rect rcDstRoot, Rect rcSrc, Rect rcDst, VwSelection ** ppvwsel,
			SearchDirection sdir)
	{
		Warn("GetSelection in lazy box\n");
		SuperClass::GetSelection(pvg, prootb, xd, yd, rcSrcRoot, rcDstRoot,
			rcSrc, rcDst, ppvwsel, sdir);
	}
	void ExpandFully();

#ifdef _DEBUG
	void TraceState(const char * pszFilename, int line, const char * pszVarName = NULL);
#endif
protected:
	VwNotifier * FindMyNotifier(int & iprop, int & tag);
	// Member variables
	VwLazyInfo m_vwlziItems; // The items this closure represents.
	int m_ihvoMin;  // The index (in the original list) of the first item in m_hvoItems.
	IVwViewConstructorPtr m_qvc; // View constructor to use for making real displays of items.
	int m_frag;	// Fragment identifier to use for making real displays of items.
	HVO m_hvoContext; // HVO that was current when AddLazyItems was called. Current obj for expand.
	// if non-zero, the constant height estimate for all HVOs determined by previous call to
	// DoLayout() after conversion to pixels.
	int m_dysUniformHeightEstimate;
	bool m_fInLayout;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
};

/*----------------------------------------------------------------------------------------------
This class is responsible for converting regular boxes back into lazy ones.
Note: the lifetime of this class is during the invocation of one method of the root box, so
it doesn't need to retain a reference count on the root box.
@h3{Hungarian: li}
----------------------------------------------------------------------------------------------*/
class LazinessIncreaser
{
public:
	LazinessIncreaser(VwRootBox * prootb);
	~LazinessIncreaser();
	void ConvertAsMuchAsPossible();
	void KeepSequence(VwBox * pboxMinKeep, VwBox * pboxLimKeep);
	void MakeLazy(VwNotifier * pnote, int iprop, int ihvoMin, int ihvoLim);
protected:
	VwRootBox * m_prootb; // the root box we are trying to increase laziness for
	IVwRootSite * m_prs; // Cache of the root site.
	BoxSet m_boxsetKeep;  // Set of boxes not eligible for converting.

	// These variables record what FindSomethingToConvert found: that property
	// m_iprop of notifier m_qnote, which extends from m_pboxFirst to m_pboxLast,
	// can be converted back into a single lazy box.
	VwNotifierPtr m_qnote; // Notifier controlling current display of stuff to convert.
	int m_iprop; // index in m_qnote of property to convert.
	VwBox * m_pboxFirst; // First box to convert (will be replaced by lazy box).
	VwBox * m_pboxLast; // Last box to convert.
	int m_ihvoMin; // First index of objects to convert.
	int m_ihvoLim; // Lim of range to convert.

	bool OkToConvert(VwBox * pbox);
	bool FindSomethingToConvert();
	bool OkToConvertObject(VwBox * pbox, VwNotifier * pnote, int iprop,
		VwBox ** ppboxNext);
	void ConvertIt(bool fSynchronizing = true);
};

#endif  //VWLAZYBOX_INCLUDED
