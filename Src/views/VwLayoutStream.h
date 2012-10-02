/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwLayoutStream.h
Responsibility: John Thomson

Description:
	Implementation of IVwLayoutStream, conceptually an additional interface on a VwRootBox,
	though if it gets a lot of data we may make it a separate object.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VwLayoutStream_INCLUDED
#define VwLayoutStream_INCLUDED

DEFINE_COM_PTR(IVwLayoutManager);

/*----------------------------------------------------------------------------------------------
Class: VwPage
Description: A VwPage represents one page in a page layout stream.
Hungarian: page
----------------------------------------------------------------------------------------------*/
class VwPage : public GenRefObj
{
public:

	VwPage(int hPage = 0);

	int m_hPage; // handle identifying the page.
	// This is basically the first box on the page. All of its parents are piles, but it is
	// not itself a pile; currently, this means it is a paragraph, table row, or some sort of
	// leaf box such as a picture or separator. It will never be a string box or paragraph
	// that is nested inside interlinear text. (This is designed to ensure that adjusting
	// paragraph layout will never delete a box pointed to by a VwPage.)
	// If this is null, the page is empty, and will probably not have a meaningful position.
	VwBox * m_pboxStart;
	//// If m_pboxStart is a paragraph, this gives the character offset at which the page
	//// actually starts. It should correspond to the IchMin of a string box, or the ich
	//// of the character that corresponds to an embedded box. (Otherwise, layout changes
	//// have broken the page and it needs to be recomputed.) Not currently used when
	//// m_poxStart is not a paragraph. (Review: should we set it to -1 when not used?)
	//int m_ichMin;

	// The offset from the top of m_pboxStart to the first thing shown on the page.
	// This position is inside any top margins that are being suppressed at the top of the page.
	int m_dysStart;
	// This is similarly the last box on the page.
	VwBox * m_pboxEnd;
	//// And, if it is a paragraph box, the position after the last character on the page.
	//int m_ichLim;
	// The offset from the top of m_pboxStart (yes, I mean pboxStart, NOT pboxEnd!)
	// to the bottom of  the last thing displayed on the page.
	// This is inside any bottom margins of the last thing on the page.
	int m_dysEnd;
	// This flag is set during Relayout() operations, so at the end of the Relayout()
	// we can determine which pages are broken and report them.
	bool m_fPageBroken;
};
typedef GenSmartPtr<VwPage> VwPagePtr;


class LayoutPageMethod;
class AddDependentObjectsMethod;
/*----------------------------------------------------------------------------------------------
Class: VwLayoutStream
Description: A Layout stream is a refinement of rootbox useful for print layout views.
Hungarian: lay
----------------------------------------------------------------------------------------------*/
class VwLayoutStream : public IVwLayoutStream, public VwRootBox
{
	friend class LayoutPageMethod;
	friend class AddDependentObjectsMethod;
	typedef VwRootBox SuperClass;
public:
	// Static methods

	// Constructors/destructors/etc.
	VwLayoutStream(VwPropertyStore *pzvps);
	VwLayoutStream();
	virtual ~VwLayoutStream();
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);

	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return SuperClass::AddRef();
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		return SuperClass::Release();
	}

	// IVwLayoutStream methods
	STDMETHOD(SetManager)(IVwLayoutManager * plm);
	STDMETHOD(LayoutObj)(IVwGraphics * pvg, int dxsAvailWidth,
		int ihvoRoot, int cvsli, VwSelLevInfo * prgvsli, int hPage);
	STDMETHOD(LayoutPage)(IVwGraphics * pvg, int dxsAvailWidth,
		int dysAvailHeight, int * pysStartThisPageBoundary, int hPage, int nColumns,
		int * pdysUsedHeight, int * pysStartNextPageBoundary);
	STDMETHOD(DiscardPage)(int hPage);
	STDMETHOD(PageBoundary)(int hPage, ComBool fEnd, IVwSelection ** ppsel);
	STDMETHOD(PageHeight)(int hPage, int * pdysHeight);
	STDMETHOD(PagePostion)(int hPage, int * pysPosition);
	STDMETHOD(RollbackLayoutObjects)(int hPage);
	STDMETHOD(CommitLayoutObjects)(int hPage);
	STDMETHOD(Layout)(IVwGraphics* pvg, int dxAvailWidth);
	STDMETHOD(ColumnHeight)(int iColumn, int * pdysHeight);
	STDMETHOD(ColumnOverlapWithPrevious)(int iColumn, int * pdysHeight);
	STDMETHOD(IsInPageAbove)(int dxs, int dys, int ysBottomOfPage, IVwGraphics * pvg,
		int * pxsLeft, int * pxsRight, ComBool * pfInLineAbove);
	void ConstructAndLayout(IVwGraphics* pvg, int dxsAvailWidth);

protected:

	// Data members
	IVwLayoutManagerPtr m_qlm;
	int m_dxsLayoutWidth; // Width most recently used to call Layout().
	GpHashMap<int, VwPage> m_hmhpagePages; // Map from Handle to Pages.

	void AddPage(VwPage * ppage);
	VwPage * CreatePage(int hPage);
	VwPage m_pageRollBack; // Copy of state of page we can roll back.
	Vector<int> m_vColumnHeights; // Heights of individual columns
	Vector<int> m_vColumnOverlaps; // Overlaps of individual columns

public:
	IVwLayoutManager * Manager() { return m_qlm; }
	VwPage * FindPage(int hPage);
	virtual bool RelayoutCore(IVwGraphics * pvg, int dxpAvailWidth, VwRootBox * prootb,
			FixupMap * pfixmap, int dxpAvailOnLine, BoxIntMultiMap * pmmbi,
			BoxSet * pboxsetDeleted);
	virtual void SendPageNotifications(VwBox * pbox);
	virtual void Reconstruct(bool fCheckForSync);
};
DEFINE_COM_PTR(VwLayoutStream);

// Class to implement the algorithm of the VwLayoutStream::LayoutPage method.
// This is in the header only to facilitate testing; it is a private class for the
// implementation of the method.
class LayoutPageMethod
{
	VwLayoutStream * m_play;
	IVwGraphics * m_pvg;
	int m_dxsAvailWidth;
	int m_dysAvailHeight;	// The available height on the page. Each column might not be higher than this value.
	int * m_pysStartPageBoundary; // The start of this page in the rootbox.
	int m_hPage;
	int m_nColumns;
	Vector<int> * m_pvColHeights;
	Vector<int> * m_pvColOverlaps;
	int * m_pdysUsedHeight;	// The height that the tallest column occupies.
	int * m_pysStartNextPageBoundary;
	int m_dpiY;

	VwBox * m_pboxBeingAdded; // the top-level non-pile box we are adding to the page.
	// If m_pboxBeingAdded is divisible into multiple lines, here is a list of them,
	// sorted by bottom of line.
	// If not this will have a single item containing the box itself.
	Vector<PageLine> m_vlnLinesOfBoxBeingAdded;
	int m_ilnBeingConsideredMin; // index into m_vplLinesOfBoxBeingAdded, the first we are currently investigating.
	int m_ilnBeingConsideredLim; // end of group of lines being considered (which have the same bottom).

public:
	LayoutPageMethod(VwLayoutStream * play, IVwGraphics * pvg, int dxsAvailWidth,
		int dysAvailHeight, int * pysStartThisPageBoundary, int hPage, int nColumns, Vector<int> * pvColHeights,
		Vector<int> * pvColOverlaps, int * pdysUsedHeight, int * pysStartNextPageBoundary);

	void FindFirstLineOnPage(int ysPosition);
	void FindFirstLineOnPage(int dysPosition, VwBox ** ppboxFirst, VwBox ** ppboxLast,
		int * pysTopOfLine, int * pysBottomOfLine);
	void Run();
	VwBox * StartOfNextLine(VwBox * pbox);
	int PrintableBottomOfLine(VwBox * pboxStartLine, VwBox ** ppboxEndLine);

	void GetDependentObjectsInChunk(VwBox * pboxFirst, VwBox * pboxLast, Vector<GUID> & vguid);

private:
	template<class Op> int LayoutFullPage(int nColumns, int dysInitialAvailableHeightPerColumn,
		int dysAdditionalAvailableHeight, Op f, VwBox** ppboxFirst,
		VwBox** ppboxLastThatFit, int * pysBottomOfLastLineThatFit, int * pdysMinAvailHeight);
	int GetExtraSpace();
	bool CanBreakAfter(VwBox * pbox);
	bool MoreStuffToAdd();
	void AdvanceToNextLine();
	int TopOfCurrentLine();
	void SetLineGroupLimit();
	bool CanBreakAfterCurrentGroup();

};

#endif  //VwLayoutStream_INCLUDED
