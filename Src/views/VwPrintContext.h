/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwPrintContext.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Provides a default implementation of IVwPrintContext to facilitate views printing.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWPRINT_CONTEXT_INCLUDED
#define VWPRINT_CONTEXT_INCLUDED

typedef ComHashMap<int, ITsString> IntStringMap;

class VwPrintInfo
{
public:
	IVwPrintContext * m_pvpc;
	IVwGraphics * m_pvg;
	Rect m_rcPage; // The whole area we can print on
	Rect m_rcDoc; // The part inside the margins
	int m_dxpInch; // printer resolution
	int m_dypInch;
	int m_nPageNo; // page we are printing
	int m_nPageTotal; // total count of pages.
};

class VwPrintContext : public IVwPrintContext
{
public:
	// Static methods

	// Constructors/destructors/etc.
	VwPrintContext();
	virtual ~VwPrintContext();
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
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}
	// IVwPrintContext methods
	STDMETHOD(get_Graphics)(IVwGraphics ** ppvg);
	STDMETHOD(get_FirstPageNumber)(int * pn);
	STDMETHOD(get_IsPageWanted)(int nPageNo, ComBool * pfWanted);
	STDMETHOD(get_AreMorePagesWanted)(int nPageNo, ComBool * pfWanted);
	STDMETHOD(get_Aborted)(ComBool * pfAborted);
	STDMETHOD(get_Copies)(int * pnCopies);
	STDMETHOD(get_Collate)(ComBool * pfCollate);
	STDMETHOD(get_HeaderString)(VwHeaderPositions grfvhp, int pn, ITsString ** pptss);
	STDMETHOD(GetMargins)(int * pdxpLeft, int * pdxpRight, int * pdypHeader,
		int * pdypTop, int * pdypBottom, int * pdypFooter);
	STDMETHOD(OpenPage)();
	STDMETHOD(ClosePage)();
	STDMETHOD(OpenDoc)();
	STDMETHOD(CloseDoc)();
	STDMETHOD(get_LastPageNo)(int * pnPageNo);
	STDMETHOD(put_HeaderMask)(VwHeaderPositions grfvhp);
	STDMETHOD(SetHeaderString)(VwHeaderPositions grfvhp, ITsString * ptss);
	STDMETHOD(SetMargins)(int dxpLeft, int dxpRight, int dypHeader, int dypTop, int dypBottom, int dypFooter);
	STDMETHOD(SetPagePrintInfo)(int nFirstPageNo, int nFirstPrintPage, int nLastPrintPage, int nCopies,
		ComBool fCollate);
	STDMETHOD(SetGraphics)(IVwGraphics * pvg);
	STDMETHOD(RequestAbort)();
	STDMETHOD(AbortDoc)();
protected:
	// member variables
	long m_cref;
	VwGraphicsPtr m_qzvg;
	int m_nFirstPageNumber; // Number assigned first physical page
	int m_nFirstPrintPage; // relative to FirstPageNumber
	int m_nLastPrintPage;
	int m_nCopies;
	bool m_fCollate;
	IntStringMap m_hmntssHeaders;
	// Mask to be applied to input grfvhp values before seeking them in hmntssHeaders.
	VwHeaderPositions m_grfvhpMask;
	// Margins.
	int m_dxpLeft;
	int m_dxpRight;
	int m_dypHeader; // top of page to top of header
	int m_dypTop; // top of page to top of main document
	int m_dypBottom; // bottom of page to bottom of document
	int m_dypFooter; // bottom of page to bottom of footer
	bool m_fAborted; // Set true if Abort() is called.

	int m_nLastPageTried; // Most recent number passed to IsPageWanted().
};

#endif // !VWPRINT_CONTEXT_INCLUDED
