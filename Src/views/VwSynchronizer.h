/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwSynchronizer.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VwSynchronizer_INCLUDED
#define VwSynchronizer_INCLUDED

// Things would be somewhat easier if this were a vector of VwRootBox instead of IVwRootBox,
// But ComVector can't handle objects that implement two COm interfaces because casts to
// IUnknown are ambiguous.
typedef ComVector<IVwRootBox> RootBoxVec; // Hungarian vrootb

/*----------------------------------------------------------------------------------------------
Struct: ExpandLazyItemsInfo
Description: Holds information about lazy items to expand after current expand is done
Hungarian: elii
----------------------------------------------------------------------------------------------*/
struct ExpandLazyItemsInfo
{
public:
	VwRootBox * m_prootbSrc;
	HVO m_hvoContext;
	int m_tag;
	int m_iprop;
	int m_ihvoMin;
	int m_ihvoLim;

	ExpandLazyItemsInfo():
		m_prootbSrc(NULL), m_hvoContext(0), m_tag(0),
			m_iprop(0), m_ihvoMin(0), m_ihvoLim(0)
	{
	}

	ExpandLazyItemsInfo(VwRootBox * prootbSrc, HVO hvoContext, int tag,
		int iprop, int ihvoMin, int ihvoLim):
		m_prootbSrc(prootbSrc), m_hvoContext(hvoContext), m_tag(tag),
			m_iprop(iprop), m_ihvoMin(ihvoMin), m_ihvoLim(ihvoLim)
	{
	}
};

typedef Vector<ExpandLazyItemsInfo> LazyItemsInfoVec;

/*----------------------------------------------------------------------------------------------
Class: VwSynchronizer
Description:
Hungarian: rootb
----------------------------------------------------------------------------------------------*/
class VwSynchronizer : public IVwSynchronizer
{
public:
	// Static methods

	// Constructors/destructors/etc.
	VwSynchronizer();
	virtual ~VwSynchronizer();
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
	STDMETHOD(AddRoot)(IVwRootBox * prootb);
	int SyncNaturalTopToTop(VwRootBox * prootbSrc, HVO hvoObj,
		int dypTopToTopNatural);
	int SyncNaturalTopToTopAfter(VwRootBox * prootbSrc, HVO hvoObj,
		int dypTopToTopNatural);
	VwBox* ExpandLazyItems(VwRootBox * prootbSrc, HVO hvoContext, int tag,
		int iprop, int ihvoMin, int ihvoLim, bool * pfForcedScroll,
		VwBox** ppboxFirstLayout, VwBox** ppboxLimLayout);
	void ContractLazyItems(VwRootBox * prootbSrc, HVO hvoContext, int tag,
		int iprop, int ihvoMin, int ihvoLim);
	bool AnotherRootHasSelection(VwRootBox * prootbSrc);
	void Reconstruct();
	bool VerifyCorrespondence(VwRootBox * prootbSrc, HVO hvoObj,
		VwBox * pboxSrc);
	bool OkToNotifyOfSizeChange();
	void AdjustSyncedBoxHeights(VwBox * pbox, int dypInch);

	// Returns true if we're in the middle of expanding lazy items
	bool IsExpandingLazyItems()
	{
		return m_fAlreadyExpandingItems;
	}

	// Allow access to IsExpandingLazyItems via COM - added for Managed Implementation of VwDrawRootBuffered
	STDMETHOD(get_IsExpandingLazyItems)(ComBool * fAlreadyExpandingItems);

private:
	VwBox* ExpandLazyItemsNoLayoutOnRootb(VwRootBox * prootb, HVO hvoContext, int tag,
		int iprop, int ihvoMin, int ihvoLim, int irootb, VwBox** ppboxFirstLayout,
		VwBox** ppboxLimLayout);

protected:
	// Member variables
	long m_cref;
	RootBoxVec m_vrootb; // List of things to synchronize.
	bool m_fStartedExpanding;
	bool m_fAlreadyExpandingItems;
	bool m_fSyncingTops;
	LazyItemsInfoVec m_vLazyItemsInfo;
	Vector<VwBox*> m_vboxFirstLayout;
	Vector<VwBox*> m_vboxLimLayout;
	Vector<VwDivBox*> m_vboxContainer;
	Vector<Rect> m_vTopBottomExpandedBoxes;
	Rect m_rcAllExpandedBoxes;
};

#endif  //VwSynchronizer_INCLUDED
