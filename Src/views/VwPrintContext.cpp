/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwPrintContext.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Provides a default implementation of IVwPrintContext to facilitate views printing.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Methods
//:>********************************************************************************************

VwPrintContext::VwPrintContext()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

VwPrintContext::~VwPrintContext()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP VwPrintContext::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwPrintContext)
		*ppv = static_cast<IVwPrintContext *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwPrintContext);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Views.VwPrintContext"),
	&CLSID_VwPrintContextWin32,
	_T("SIL Print Context"),
	_T("Apartment"),
	&VwPrintContext::CreateCom);


void VwPrintContext::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwPrintContext> qvpc;
	qvpc.Attach(NewObj VwPrintContext());		// ref count initially 1
	CheckHr(qvpc->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	IVwPrintContext methods
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Get a VwGraphics on which to actually draw (or measure things).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::get_Graphics(IVwGraphics ** ppvg)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppvg);
	*ppvg = m_qzvg;
	AddRefObj(*ppvg);
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}
/*----------------------------------------------------------------------------------------------
	Get the number that should be assigned to the first page of the layout.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::get_FirstPageNumber(int * pn)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pn);
	*pn = m_nFirstPageNumber;
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}
/*----------------------------------------------------------------------------------------------
	Return true if the specified page is in the range(s) to be printed.
	Page numbers are relative to the first page number specified with
	FirstPageNumber() above; for example, if the first page is numbered 7,
	then IsPageWanted(8...) requests info on whether to print the second page.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::get_IsPageWanted(int nPageNo, ComBool * pfWanted)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfWanted);
	m_nLastPageTried = nPageNo;
	*pfWanted = nPageNo >= m_nFirstPrintPage && nPageNo <= m_nLastPrintPage;
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}
/*----------------------------------------------------------------------------------------------
	Return true if more pages are wanted after page number specified.
	This is asked only when isPageWanted returns false.
	If AreMorePagesWanted also returns false, printing stops.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::get_AreMorePagesWanted(int nPageNo, ComBool * pfWanted)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfWanted);
	*pfWanted = nPageNo <= m_nLastPrintPage;
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	Answer true if the print process should be aborted (e.g., because the
	end user cancelled). Print code will endeavor to call this as often as possible.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::get_Aborted(ComBool * pfAborted)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfAborted);
	*pfAborted = m_fAborted;
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	The number of copies to print, and whether to collate.
	(If the output device can produce these effects by itself, answer 1 copy,
	in which case, fCollate is irrelevant.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::get_Copies(int * pnCopies)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnCopies);
	*pnCopies = m_nCopies;
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
Answer whether to collate multiple copies. Not used if #copies is 1.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::get_Collate(ComBool * pfCollate)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfCollate);
	*pfCollate = m_fCollate;
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	Return the header that should be printed on page #pn in the postion indicated
	by vhp. Each relevant bit in vhp is turned on; for example, on an odd-numbered
	page the top right header has bits right, outside, odd, and top.
	The intent is that the callee can maintain a list of header strings, each
	with a group of flags indicating where it should be printed. For example,
	a particular string might be marked top, left. The callee masks out unwanted
	bits in vhp (e.g., outside and inside, odd and even, if doing simple left/right
	printing), then looks for an exact match on the remaining bits.
	For mirrored pages, mask out left, right, odd, and even.
	For distinct odd/even headers, mask out left, right, inside, and outside.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::get_HeaderString(VwHeaderPositions grfvhp, int pn,
	ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	// Retrieve wants a reference to a smart pointer.
	ITsStringPtr qtss;
	int grfvhp1 = (int) grfvhp & m_grfvhpMask;
	if (m_hmntssHeaders.Retrieve(grfvhp1, qtss))
	*pptss = qtss.Detach();
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	Get margins, relative to the printable part of the page, in device units
	Arguments:
		pdypHeader           top of page to top of header
		pdypTop              top of page to top of main document
		pdypBottom           bottom of page to bottom of document
		pdypFooter           bottom of page to bottom of footer
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::GetMargins(int * pdxpLeft, int * pdxpRight, int * pdypHeader,
	int * pdypTop, int * pdypBottom, int * pdypFooter)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pdxpLeft);
	ChkComOutPtr(pdxpRight);
	ChkComOutPtr(pdypHeader);
	ChkComOutPtr(pdypTop);
	ChkComOutPtr(pdypBottom);
	ChkComOutPtr(pdypFooter);

	*pdxpLeft = m_dxpLeft;
	*pdxpRight = m_dxpRight;
	*pdypHeader = m_dypHeader;
	*pdypTop = m_dypTop;
	*pdypBottom = m_dypBottom;
	*pdypFooter = m_dypFooter;

	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	Open a page.
	note: Win32 specific, not needed in Gtk implimentation
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::OpenPage()
{
	BEGIN_COM_METHOD;
#if WIN32
	// Inform the driver that the application is about to begin
	// sending data.
	if (!m_qzvg)
		return E_UNEXPECTED;
	int nError;

	VwGraphics * pvgTmp = dynamic_cast<VwGraphics *>(m_qzvg.Ptr());
	nError = StartPage(pvgTmp->DeviceContext());
	if (nError <= 0)
	{
		return E_FAIL;
	}
#else
	printf("VwPrintContext::OpenPage Not Implement\n");
	fflush(stdout);
	// TODO-Linux: Implement
#endif
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	Called at the end of printing each page.
	note: Win32 specific, not needed in Gtk implimentation
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::ClosePage()
{
	BEGIN_COM_METHOD;
#if WIN32
	if (!m_qzvg)
		return E_UNEXPECTED;

	int nError;
	VwGraphics * pvgTmp = dynamic_cast<VwGraphics *>(m_qzvg.Ptr());
	nError = EndPage(pvgTmp->DeviceContext());

	if (nError <= 0)
	{
		return E_FAIL;
	}
#else
	printf("VwPrintContext::ClosePage Not Implemented\n");
	fflush(stdout);
	// TODO-Linux: Implement
#endif
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	Called before the first OpenPage call.
	note: Win32 specific, not needed in Gtk implimentation
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::OpenDoc()
{
	BEGIN_COM_METHOD;
#if WIN32
	// Begin a print job by calling the StartDoc function.
	if (!m_qzvg)
		return E_UNEXPECTED;

	DOCINFO di;

	memset(&di, 0, sizeof(DOCINFO));
	di.cbSize = sizeof(DOCINFO);
	// TODO JohnT: arrange for a better document name. (This is the name
	// that shows up in print queues. So getting the doc title into it somehow
	// would be helpful.)
	di.lpszDocName = _T("Fieldworks View print");
	di.lpszOutput = (LPTSTR) NULL;
	di.lpszDatatype = (LPTSTR) NULL;
	di.fwType = 0;

	VwGraphics * pvgTmp = dynamic_cast<VwGraphics *>(m_qzvg.Ptr());
	int nError = StartDoc(pvgTmp->DeviceContext(), &di);
	if (nError == SP_ERROR)
	{
		return E_FAIL;
	}
#else
	printf("VwPrintContext::OpenDoc Not Implemented\n");
	fflush(stdout);
	// TODO-Linux: Implement
#endif
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	Called after the last ClosePage call (and after testing all subsequent actual pages
	to see if they are wanted, until AreMorePagesWanted returns false).
	note: Win32 specific, not needed in Gtk implimentation
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::CloseDoc()
{
	BEGIN_COM_METHOD;
#if WIN32
	if (!m_qzvg)
		return E_UNEXPECTED;

	VwGraphics * pvgTmp = dynamic_cast<VwGraphics *>(m_qzvg.Ptr());
	int nError = EndDoc(pvgTmp->DeviceContext());

	if (nError <= 0)
	{
		return E_FAIL;
	}
#else
	printf("VwPrintContext::CloseDoc Not Implemented\n");
	fflush(stdout);
	// TODO-Linux: Implement
#endif
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	This returns the last page number requested using IsPageWanted.
	To find out the total number of pages in a document, arrange that
	the first page wanted is some very large number, print the document,
	(nothing is output), then call this method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::get_LastPageNo(int * pnPageNo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnPageNo);
	*pnPageNo = m_nLastPageTried;
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	Set the header mask. Bits masked out do not have to match, in finding
	a header appropriate to a particular page. This also clears all previously
	established headers.
	This and subsequent methods are currently used by clients that use the default
	implementation of IVwPrintContext provided by the view subsystem itself.
	In the future they might be used in other ways; for example, we could create
	a "SectionBox" which occupies no space but can reset the header information,
	margins, and so forth, for subsequent pages when "printed".
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::put_HeaderMask(VwHeaderPositions grfvhp)
{
	BEGIN_COM_METHOD;
	m_grfvhpMask = grfvhp;
	m_hmntssHeaders.Clear();
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	Set a header (or replace an existing one) to be shown in the position indicated
	(and other positions that are equivalent under the current mask).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::SetHeaderString(VwHeaderPositions grfvhp, ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptss);

	int grfvhp1 = (int) grfvhp & m_grfvhpMask;
	m_hmntssHeaders.Insert(grfvhp1, ptss, true);

	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	Set values for various margins.
	Arguments:
		dypHeader            top of page to top of header
		dypTop               top of page to top of main document
		dypBottom            bottom of page to bottom of document
		dypFooter            bottom of page to bottom of footer
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::SetMargins(int dxpLeft, int dxpRight, int dypHeader,
	int dypTop, int dypBottom, int dypFooter)
{
	BEGIN_COM_METHOD;

	m_dxpLeft = dxpLeft;
	m_dxpRight = dxpRight;
	m_dypHeader = dypHeader;
	m_dypTop = dypTop;
	m_dypBottom = dypBottom;
	m_dypFooter = dypFooter;

	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	This will never be called in the course of printing and need not be
	implemented by a callee doing its own implementation. It provides
	only a single range of page numbers, which is enough to implement the
	standard Windows print dialog.
	Arguments:
		nFirstPageNo         returned by FirstPageNumber()
		nFirstPrintPage      relative to FirstPageNo
		nCopies              returned by Copies()
		fCollate             returned by Collate()
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::SetPagePrintInfo(int nFirstPageNo, int nFirstPrintPage,
	int nLastPrintPage, int nCopies, ComBool fCollate)
{
	BEGIN_COM_METHOD;

	m_nFirstPageNumber = nFirstPageNo;
	m_nFirstPrintPage = nFirstPrintPage;
	m_nLastPrintPage = nLastPrintPage;
	m_nCopies = nCopies;
	m_fCollate = (bool)fCollate;

	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}


/*----------------------------------------------------------------------------------------------
	This, likewise, is used only to set up the default print context.
	The print context currently built into the Win32 views subsystem must be
	initialized with a VwGraphics obtained using CoCreateInstance
	on CLSID_VwGraphicsWin32, and then initialized with an appropriate
	print HDC.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::SetGraphics(IVwGraphics * pvg)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvg);

	if (pvg)
		// Get the implementation, so we can extract our hdc from it when we need to.
		// It has to be our own implementation for that reason.
		return pvg->QueryInterface(CLID_VWGRAPHICS_IMPL, (void **)(&m_qzvg));
	m_qzvg = NULL;
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}

/*----------------------------------------------------------------------------------------------
	Call this to cancel the print job as soon as possible (e.g., when the user
	clicks cancel).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::RequestAbort()
{
	m_fAborted = true;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	This is called instead of EndDoc (and possibly replacing a ClosePage call, too)
	when the view code responds to a RequestAbort and actually aborts printing.
	No more calls may be made to the print context after AbortDoc, nor should any
	more drawing be done on the associated IVwGraphics.
	note: Win32 specific, not needed in Gtk implimentation
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPrintContext::AbortDoc()
{
	BEGIN_COM_METHOD;
#if WIN32
	if (!m_qzvg)
		return E_UNEXPECTED;

	VwGraphics * pvgTmp = dynamic_cast<VwGraphics *>(m_qzvg.Ptr());
	int nError = ::AbortDoc(pvgTmp->DeviceContext());

	if (nError <= 0)
	{
		return E_FAIL;
	}
#else
	printf("VwPrintContext::OpenDoc Not Implemented\n");
	fflush(stdout);
	// TODO-Linux: Implement
#endif
	END_COM_METHOD(g_fact, IID_IVwPrintContext);
}


#include "ComHashMap_i.cpp"
template class ComHashMap<int, ITsString>; // IntStringMap;
