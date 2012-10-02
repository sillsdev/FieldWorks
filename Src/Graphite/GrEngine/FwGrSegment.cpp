/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwGrSegment.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implements a Graphite text segment--a range of text in one writing system, that can be
	rendered with a single font, and that fits on a single line. This is the COM object
	that is generated for FieldWorks.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************

// For error reporting:
static DummyFactory g_fact(_T("SIL.Graphite.FwGrSegment"));


//:>********************************************************************************************
//:>	FwGrSegment overrides
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructors.
----------------------------------------------------------------------------------------------*/
FwGrSegment::FwGrSegment()
{
	m_cref = 1;

	m_pseg = NULL;
	ModuleEntry::ModuleAddRef();

	m_pwsegp = NULL;

	m_dxsStretchNeeded = 0;
	m_fStretched = false;
	m_fChangeLineEnds = false;
	m_psegAltLineEnd = NULL;

	m_pgts = NULL;

//m_prggstrm = NULL;
//m_cgstrm = 0;
//m_prggbb = NULL;
//m_cgbb = 0;

}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwGrSegment::~FwGrSegment()
{
	delete m_pgts;
	delete m_pseg;
	delete m_psegAltLineEnd;
	delete m_pwsegp;


//delete m_prggstrm;
//delete m_prggbb;

	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP FwGrSegment::QueryInterface(REFIID riid, void **ppv)
{
	if (!ppv)
		return E_POINTER;
	AssertPtr(ppv);
	*ppv = NULL;
	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_ILgSegment)
		*ppv = static_cast<ILgSegment *>(this);
	else if (&riid == &CLSID_FwGrSegment)			// trick for engine to get an impl
		*ppv = static_cast<FwGrSegment *>(this);

	else if (riid == IID_ISupportErrorInfo)
	{
		// for error reporting:
		*ppv = NewObj CSupportErrorInfo(this, IID_ILgSegment);
		return NOERROR;
	}

#ifdef OLD_TEST_STUFF
	else if (riid == IID_IGrSegmentDebug)		// trick for test code instrumentation
	{
		GrSegmentDebug * pwrsegd = NewObj GrSegmentDebug(this);
		HRESULT hr = pwrsegd->QueryInterface(riid, ppv);
		pwrsegd->Release();
		return hr;
	}
#endif // OLD_TEST_STUFF

	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   ILgSegment Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Draw the text of the segment at the specified point in the graphics context.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::DrawText(int ichwBase, IVwGraphics * pvg,
	RECT rs, RECT rd, int * pdxdWidth)
{
	BEGIN_COM_METHOD;
	return DrawTextInternal(ichwBase, pvg, rs, rd, pdxdWidth, false);
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Draw the text of the segment at the specified point in the graphics context.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::DrawTextNoBackground(int ichwBase, IVwGraphics * pvg,
	RECT rs, RECT rd, int * pdxdWidth)
{
	BEGIN_COM_METHOD;
	return DrawTextInternal(ichwBase, pvg, rs, rd, pdxdWidth, true);
	END_COM_METHOD(g_fact, IID_ILgSegment);
}
/*----------------------------------------------------------------------------------------------
	Draw the text of the segment at the specified point in the graphics context.
----------------------------------------------------------------------------------------------*/
HRESULT FwGrSegment::DrawTextInternal(int ichwBase, IVwGraphics * pvg,
	RECT rs, RECT rd, int * pdxdWidth, bool fSuppressBackground)
{
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdxdWidth);

	m_qreneng->SetUpGraphics(pvg, m_pgts, ichwBase);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	gr::Rect rsFloat(rs);
	gr::Rect rdFloat(rd);

	if (!m_pwsegp)
		m_pwsegp = new FwGrWinSegmentPainter(m_pseg, hdc);
	((FwGrWinSegmentPainter *)m_pwsegp)->SuppressBackground(fSuppressBackground);

	SetTransformRects(m_pwsegp, rsFloat, rdFloat);

	HRESULT hr = S_OK;
	try {
		m_pwsegp->setDC(hdc);

		m_pwsegp->paint();

		m_pwsegp->setDC(0);
	}
	catch (...)
	{
		hr = E_UNEXPECTED;
	}

	// TODO: figure out why this isn't the same as the active code below:
	//float xsWidth = m_pseg->advanceWidth();
	//*pdxdWidth = rs.left + (int)SegmentPainter::ScaleX(xsWidth - (rd.right - rd.left), rd, rs);

	*pdxdWidth = 0;
	int rsWidth = rs.right - rs.left;
	int rdWidth = rd.right - rd.left;
	if (rsWidth == 0)
		hr = E_INVALIDARG;
	else
		*pdxdWidth = gr::GrEngine::GrIntMulDiv(gr::GrEngine::RoundFloat(
			m_pseg->advanceWidth()), rdWidth, rsWidth);

	return hr;

}

// REVIEW: should all these methods test the input args?

/*----------------------------------------------------------------------------------------------
	If we are wanting measurements with a different device context than when the
	segment was created or than when last calling any measurement routine,
	Recompute() must be called.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::Recompute(int ichwBase, IVwGraphics * pvg)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);

	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	The distance the drawing point should advance after drawing this segment.
	Always positive, even for RtoL segments. This includes any stretch that has
	been requested.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_Width(int ichwBase, IVwGraphics * pvg, int * pxs)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComOutPtr(pxs);

	ChangeLineEnds(ichwBase, pvg);
	JustifyIfNeeded(ichwBase, pvg);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	*pxs = gr::GrEngine::RoundFloat(m_pseg->advanceWidth());

	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Distance that drawing takes place to the right of the rectangle specified by the width
	and drawing origin.
	It is legitimate not to implement this; calling code will typically treat it as zero.
	However, the not-implemented could be used to make sure we allow a few pixels.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_RightOverhang(int ichwBase, IVwGraphics * pvg, int * pxs)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComOutPtr(pxs);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	gr::Rect rectBB = m_pseg->boundingRect();
	float xsWidth = m_pseg->advanceWidth();
	int dxsOver = gr::GrEngine::RoundFloat(rectBB.right - xsWidth);
	*pxs = max(dxsOver, 0);

	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Overhang to the left of the drawing origin. Value returned will be >= 0.
	It is legitimate not to implement this; calling code will typically treat it as zero.
	However, the not-implemented could be used to make sure we allow a few pixels.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_LeftOverhang(int ichwBase, IVwGraphics * pvg, int * pxs)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComOutPtr(pxs);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	gr::Rect rectBB = m_pseg->boundingRect();
	*pxs = min(gr::GrEngine::RoundFloat(rectBB.left), 0) * -1;

	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Height of the drawing rectangle. This is normally the full font height, even if all chars
	in segment are lower case or none have descenders, or even if segment is empty.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_Height(int ichwBase, IVwGraphics * pvg, int * pys)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComOutPtr(pys);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	*pys = gr::GrEngine::RoundFloat(pfont->ascent() + pfont->descent());

	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	The ascent of the font typically used to draw this segment. Basically, it determines the
	baseline used to align it.

	Review: we may need to make more than one baseline explicit in the interface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_Ascent(int ichwBase, IVwGraphics * pvg, int * pdys)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdys);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	*pdys = gr::GrEngine::RoundFloat(pfont->ascent());

	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Obtains height and width in a single call.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::Extent(int ichwBase, IVwGraphics * pvg, int * pdxsWidth, int * pdysHt)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdxsWidth);
	ChkComOutPtr(pdysHt);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));

	ChangeLineEnds(ichwBase, pvg);
	JustifyIfNeeded(ichwBase, pvg);

	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	*pdysHt = gr::GrEngine::RoundFloat(pfont->ascent() + pfont->descent());
	*pdxsWidth  = gr::GrEngine::RoundFloat(m_pseg->advanceWidth());

	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Compute the rectangle in destination coords which contains all the pixels drawn by
	this segment. This should be a sufficient rectangle to invalidate if the segment is
	about to be discarded.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::BoundingRect(int ichwBase, IVwGraphics * pvg, RECT rs, RECT rd,
	RECT * prdBounds)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComArgPtr(prdBounds);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));

	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);
	gr::Rect rectBB = m_pseg->boundingRect();
	// TODO: scale the rectangle using rs and rd

	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Compute the width the segment would occupy if drawn with the specified
	parameters. Don't update the cached measurements.

	This method is used to handle the rounding errors that happen when doing layout in
	one coordinate system and drawing in another. In our case, we don't need to do
	anything too special--the actual width should be the same as the scaled calculated
	width, because of the fact that we postion each glyph explicitly which keeps rounding
	errors from accumulating.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::GetActualWidth(int ichwBase, IVwGraphics * pvg,
	RECT rs, RECT rd, int * pdxdWidth)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdxdWidth);

	ChangeLineEnds(ichwBase, pvg);
	JustifyIfNeeded(ichwBase, pvg);

	int rsWidth = rs.right - rs.left;
	int rdWidth = rd.right - rd.left;

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	float xs = m_pseg->advanceWidth();

	pfont->restoreDC();

	*pdxdWidth = gr::GrEngine::GrIntMulDiv(gr::GrEngine::RoundFloat(xs), rdWidth, rsWidth);

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	The amount that drawing takes place above the official rectangle of the segment (as
	specified by the height).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_AscentOverhang(int ichwBase, IVwGraphics * pvg, int * pdys)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdys);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));

	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);
	float ysAscent = pfont->ascent();

	gr::Rect rectBB = m_pseg->boundingRect();
	int dysOver = gr::GrEngine::RoundFloat(rectBB.top - ysAscent);
	*pdys = max(dysOver, 0);

	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	The amount that drawing takes place below the official rectangle of the segment (as
	specified by the height). Value returned is >= 0.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_DescentOverhang(int ichwBase, IVwGraphics * pvg, int * pdys)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdys);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));

	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);
	float ysNegDescent = pfont->descent() * -1;

	gr::Rect rectBB = m_pseg->boundingRect();
	int dysOver = gr::GrEngine::RoundFloat(ysNegDescent - rectBB.bottom);
	*pdys = max(dysOver, 0);

	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Answer whether the primary direction of the segment is right-to-left. This is based
	on the direction of the writing system, except for white-space-only segments, in
	which case it is based on the main paragraph direction.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_RightToLeft(int ichwBase, ComBool * pfResult)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfResult);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	bool f = m_pseg->rightToLeft();
	*pfResult = (ComBool)f;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the depth of direction embedding used by this segment. It is presumably the same
	for all runs of the segment, otherwise, some of them would use a different writing
	system and therefore be part of a different segment. So just use the first.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_DirectionDepth(int ichwBase, int * pnDepth, ComBool * pfWeak)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnDepth);
	ChkComOutPtr(pfWeak);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	bool fWeakBool;
	*pnDepth = m_pseg->directionDepth(&fWeakBool);
	*pfWeak = (ComBool)fWeakBool;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Change the direction of the segment. This is needed specifically for white-space-only
	segments, which are initially created to be in the direction of the paragraph, but then
	later are discovered to not be line-end after all, and need to be changed to use the
	directionality of the writing system.

	@return E_FAIL for segments that do not have weak directionality and therefore
	cannot be changed.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::SetDirectionDepth(int ichwBase, int nNewDepth)
{
	BEGIN_COM_METHOD;

	if (m_pwsegp)
	{
		delete m_pwsegp;
		m_pwsegp = NULL;
	}

	//HDC hdc;
	//IVwGraphicsWin32Ptr qvg32;
	//CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	//CheckHr(pvg32->GetDeviceContext(&hdc));
	//Font & font = m_pseg->getFont();
	//WinFont * pfont = dynamic_cast<WinFont *>(&font);
	//pfont->replaceDC(hdc);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	Segment * psegPrev = m_pseg;
	m_pseg = Segment::WhiteSpaceSegment(*psegPrev, nNewDepth);

	delete psegPrev;

	//pfont->restoreDC();

	if (m_pseg->directionDepth() != nNewDepth)
		// We weren't able to change it.
		return E_FAIL;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the writing system used by this segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_WritingSystem(int ichwBase, int * pws)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pws);

	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	The logical range of characters covered by the segment, relative to the beginning of
	the segment. These values should be exact at a writing system or string boundary,
	but may be somewhat fuzzy at a line-break, since characters may be re-ordered
	across such boundaries. The renderer is free to	apply any definition it likes of
	where a line-break occurs. This should always be the same value obtained from the
	renderer as pdichLimSeg.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_Lim(int ichwBase, int * pdichw)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pdichw);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));
	*pdichw = m_pgts->GrToVwOffset(m_pseg->stopCharacter() - m_pseg->startCharacter());

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Indicates the last character of interest to the segment, relative to the beginning
	of the segment. The meaning of this is that no behavior of this segment will be
	affected if characters beyond that change. This does not necessarily mean that
	a different line break could not have been obtained by the renderer if characters
	beyond that change, just that a segment with the boundaries of this one would
	not behave differently.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_LimInterest(int ichwBase, int * pdichw)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pdichw);

	ThrowInternalError(E_NOTIMPL);

	//return (HRESULT)m_pseg->get_LimInterest(ichwBase, pdichw);

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Changes the end-of-line status of the segment. This is used after making the last segment
	of a string if we want to attempt to put some more text after it (e.g., in another
	writing system), or if we have reordered due to bidirectionality.

	ENHANCE: the current version handles switching back and forth when we are trying to
	decide whether we can put more stuff on a line. So it assumes we are at the end of
	the contextual run (ie, the next batch of stuff will be in a different ows).
	This will not correctly handle the way this function could or possibly
	should be used for bidirectional reordering.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::put_EndLine(int ichwBase, IVwGraphics * pvg, ComBool fNewVal)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	m_fNewEnd = (bool)fNewVal;
	if (!m_fChangeLineEnds)
		m_fNewStart = m_pseg->startOfLine();
	m_fChangeLineEnds = true;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Changes the start-of-line status of the segment. This is used after making the last segment
	of a string if we want to attempt to put some more text after it (e.g., in another
	writing system), or if we have reordered due to bidirectionality.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::put_StartLine(int ichwBase, IVwGraphics * pvg, ComBool fNewVal)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	m_fNewStart = (bool)fNewVal;
	if (!m_fChangeLineEnds)
		m_fNewEnd = m_pseg->endOfLine();
	m_fChangeLineEnds = true;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the type of break that occurs at the logical start of the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_StartBreakWeight(int ichwBase, IVwGraphics * pvg,
	LgLineBreak * plb)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComArgPtr(plb);

	return m_pseg->startBreakWeight();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the type of break that occurs at the logical end of the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_EndBreakWeight(int ichwBase, IVwGraphics * pvg,
	LgLineBreak * plb)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComArgPtr(plb);

	return m_pseg->endBreakWeight();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Read the amount of stretch that has been set for the segment. This is not included in
	the result returned from width, but the segment gets this much wider when drawn.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::get_Stretch(int ichwBase, int * pdxs)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pdxs);

	return gr::GrEngine::RoundFloat(m_pseg->stretch());

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Set the amount of stretch that has been set for the segment. This is not included in
	the result returned from width, but the segment gets this much wider when drawn.
	Return E_UNEXPECTED if the segment cannot stretch or shrink itself. Return E_FAIL if
	the segment is already stretched.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::put_Stretch(int ichwBase, int dxs)
{
	BEGIN_COM_METHOD;

	if (dxs != 0 && m_fStretched)
		return E_FAIL;

	// The segment will actually be stretched at the point that some metrics are requested,
	// like width.
	m_dxsStretchNeeded = (float)dxs;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Indicate if the character position is a valid place for an insertion point.

	@oaram ich			- position to test
	@param pipvr		- answer: kipvrOK = IP here is valid;
							kipvrBad = IP here no good;
							kipvrUnknown = this seg can't decide, ask the next
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::IsValidInsertionPoint(int ichwBase, IVwGraphics* pvg,
	int ichw, LgIpValidResult * pipvr)
{
	BEGIN_COM_METHOD;
	// Allow this argument to be null, as we don't actually use it, and some clients don't
	// bother to pass it. (Todo JohnT: remove this argument from the interface, NO implementation
	// uses it.)
	ChkComArgPtrN(pvg);
	ChkComArgPtr(pipvr);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	FwGrWinSegmentPainter segp(m_pseg, 0);
	bool badOffset;
	int grIchw = m_pgts->VwToGrOffset(ichw, badOffset);
	*pipvr = badOffset ? kipvrBad : segp.isValidInsertionPoint(grIchw);

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Answer whether the logical and physical boundaries of the string coincide.
	This method is called by the application to handle writing system boundaries, and by
	other segments in a writing-system chain to handle line-breaks.

	@param fBoundaryEnd		- asking about the logical end boundary?
	@param fBoundaryRight	- asking about the physical right boundary?
	@param pfResult			- return value
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::DoBoundariesCoincide(int ichwBase, IVwGraphics* pvg,
	ComBool fBoundaryEnd, ComBool fBoundaryRight,
	ComBool * pfResult)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComOutPtr(pfResult);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	FwGrWinSegmentPainter segp(m_pseg, 0);
	*pfResult = segp.doBoundariesCoincide((bool)fBoundaryEnd, (bool)fBoundaryRight);
	*pfResult = (pfResult != FALSE); // convert back to ComBool

	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Draw an insertion point at an appropriate position.

	@param twx, twy		- same origin used to Draw segment
	@param ich			- character position; must be valid
	@param fAssocPrev	- associated with previous character?
	@param bOn			- turning on or off? Caller should alternate, first turning on
							(ignored in this implementation)
	@param dm			- draw mode:
							kdmNormal = draw complete insertion pt (I-beam or split cursor);
							kdmPrimary = only draw primary half of split cursor;
							kdmSecondary = only draw secondary half of split cursor
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::DrawInsertionPoint(int ichwBase, IVwGraphics * pvg,
	RECT rs, RECT rd, int ichwIP, ComBool fAssocPrev,
	ComBool bOn, LgIPDrawMode dm)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	FwGrWinSegmentPainter segp(m_pseg, hdc);
	SetTransformRects(&segp, rs, rd);
	segp.drawInsertionPoint(m_pgts->VwToGrOffset(ichwIP), fAssocPrev, bOn, (dm != kdmNormal));

	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Fill in bounding rectangles for the (possibly 2-part) IP, in destination device coordinates.
	Return flags indicating which of the 2 parts where rendered here.

	@param rs,rd			- source/destination coordinates, for scaling
	@param ichwIP			- insertion point
	@param fAssocPrev		- is the IP "leaning" backward?
	@param dm				- draw mode:
								kdmNormal = draw complete insertion pt (I-beam or split cursor);
								kdmPrimary = only draw primary half of split cursor;
								kdmSecondary = only draw secondary half of split cursor
	@param prdPrimary		- return location of primary selection, in dest coords
	@param prdSecondary		- return location of secondary selection, in dest coords
	@param pfPrimaryHere	- true if we filled in prdPrimary
	@param pfSecHere		- true if we filled in prdSecondary
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::PositionsOfIP(int ichwBase, IVwGraphics * pvg,
	RECT rs, RECT rd, int ichwIP, ComBool fAssocPrev, LgIPDrawMode dm,
	RECT * prdPrimary, RECT * prdSecondary,
	ComBool * pfPrimaryHere, ComBool * pfSecHere)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComArgPtr(prdPrimary);
	ChkComArgPtr(prdSecondary);
	ChkComOutPtr(pfPrimaryHere);
	ChkComOutPtr(pfSecHere);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	gr::Rect rsFloat(rs);
	gr::Rect rdFloat(rd);

	gr::Rect rdPrimFloat, rdSecFloat;

	//bool fPrimHere = true;
	//bool fSecHere = true;
	FwGrWinSegmentPainter segp(m_pseg, 0);
	SetTransformRects(&segp, rsFloat, rdFloat);
	segp.positionsOfIP(m_pgts->VwToGrOffset(ichwIP), (bool)fAssocPrev, (dm != kdmNormal),
		&rdPrimFloat, &rdSecFloat);

	pfont->restoreDC();

	// NOTE: We don't want to round the float value here because this causes overlapping of
	// two segments' rectangles sometimes. (TE-4497)
	prdPrimary->top      = (int)rdPrimFloat.top;
	prdPrimary->bottom   = (int)rdPrimFloat.bottom;
	prdPrimary->left     = (int)rdPrimFloat.left;
	prdPrimary->right    = (int)rdPrimFloat.right;
	prdSecondary->top    = (int)rdSecFloat.top;
	prdSecondary->bottom = (int)rdSecFloat.bottom;
	prdSecondary->left   = (int)rdSecFloat.left;
	prdSecondary->right  = (int)rdSecFloat.right;

	*pfPrimaryHere = (prdPrimary->top != 0 || prdPrimary->bottom != 0
		|| prdPrimary->left != 0 || prdPrimary->right != 0);
	*pfSecHere = (prdSecondary->top != 0 || prdSecondary->bottom != 0
		|| prdSecondary->left != 0 || prdSecondary->right != 0);

	//*pfPrimaryHere = (ComBool)fPrimHere;
	//*pfSecHere = (ComBool)fSecHere;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Highlight a range of text.

	@param rsArg,rdArg		- source/destination coordinates, for scaling
	@param ichwAnchor/End	- selected range
	@param ydLineTop/Bottom	- top/bottom of area to highlight if whole line height;
								includes half of inter-line spacing.
	@param bOn				- true if we are turning on (ignored in this implementation)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::DrawRange(int ichwBase, IVwGraphics * pvg,
	RECT rs, RECT rd,
	int ichwAnchor, int ichwEnd,
	int ydLineTop, int ydLineBottom,
	ComBool bOn, ComBool fIsLastLineOfSelection, RECT * prsBounds)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComArgPtr(prsBounds);

	HRESULT hr = S_OK;
	ComBool fAnythingToDraw;
	CheckHr(PositionOfRange(ichwBase, pvg, rs, rd, ichwAnchor, ichwEnd, ydLineTop,
		ydLineBottom, fIsLastLineOfSelection, prsBounds, &fAnythingToDraw));
	if (fAnythingToDraw)
	{
		m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));
		int grIchwAnchor = m_pgts->VwToGrOffset(ichwAnchor);
		int grIchwEnd = m_pgts->VwToGrOffset(ichwEnd);
		if ((grIchwAnchor == grIchwEnd && grIchwAnchor == m_pseg->getText().getLength()) ||
			(m_pseg->stopCharacter() - m_pseg->startCharacter() == 0  &&
			 m_pseg->startCharacter() >= grIchwAnchor && m_pseg->startCharacter() <= grIchwEnd))
		{
			CheckHr(pvg->InvertRect(prsBounds->left, prsBounds->top, prsBounds->right,
				prsBounds->bottom));
		}
		else
		{
			HDC hdc;
			IVwGraphicsWin32Ptr qvg32;
			CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
			CheckHr(qvg32->GetDeviceContext(&hdc));
			gr::Font & font = m_pseg->getFont();
			WinFont * pfont = dynamic_cast<WinFont *>(&font);
			pfont->replaceDC(hdc);

			FwGrWinSegmentPainter segp(m_pseg, hdc, 0, 0, fIsLastLineOfSelection);
			SetTransformRects(&segp, rs, rd);
			hr = (HRESULT)segp.drawSelectionRange(grIchwAnchor, grIchwEnd,
				(float)ydLineTop, (float)ydLineBottom, (bool)bOn);

			pfont->restoreDC();
		}
	}

	return hr;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get a bounding rectangle that will contain the area highlighted by this segment
	when drawing the specified range.

	@param rsArg,rdArg		- source/destination coordinates, for scaling
	@param ichwAnchor/End	- selected range
	@param ydLineTop/Bottom	- top/bottom of area to highlight if whole line height;
								includes half of inter-line spacing.
	@param prdBounds		- return location of range
	@param pfAnythingToDraw	- true if the return rectange has a meaningful value
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::PositionOfRange(int ichwBase, IVwGraphics* pvg,
	RECT rs, RECT rd,
	int ichwAnchor, int ichwEnd,
	int ydLineTop, int ydLineBottom, ComBool fIsLastLineOfSelection,
	RECT * prdBounds, ComBool * pfAnythingToDraw)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComArgPtr(prdBounds);
	ChkComOutPtr(pfAnythingToDraw);
	*pfAnythingToDraw = false; // default value

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	gr::Rect rsFloat(rs);
	gr::Rect rdFloat(rd);
	gr::Rect rdBoundsFloat;
	FwGrWinSegmentPainter segp(m_pseg, 0);
	SetTransformRects(&segp, rsFloat, rdFloat);
	int grIchwEnd = m_pgts->VwToGrOffset(ichwEnd);
	bool fAny = segp.positionsOfRange(m_pgts->VwToGrOffset(ichwAnchor), grIchwEnd,
		(float)ydLineTop, (float)ydLineBottom, &rdBoundsFloat);

	if (grIchwEnd == m_pseg->getText().getLength() && !fIsLastLineOfSelection &&
		grIchwEnd >= m_pseg->startCharacter() && grIchwEnd <= m_pseg->stopCharacter())
	{
		if (!fAny)
		{
			// if we didn't have anything, we need to make sure that we start at the right
			// location, so that when we add to the rectangle, we display it at the right place.
			RECT rdPrimary, rdSecondary;
			ComBool fPrimaryHere, fSecHere;
			CheckHr(PositionsOfIP(ichwBase, pvg, rs, rd, ichwEnd, true, kdmNormal,
				&rdPrimary, &rdSecondary, &fPrimaryHere, &fSecHere));
			// we add one pixel to the right to be consistent with a regular range selection.
			// Otherwise the selection would change size when we start with the end of paragraph
			// and then extend the selection to include more characters.
			rdBoundsFloat.left = (float)rdPrimary.left;
			rdBoundsFloat.right = (float)rdPrimary.left + 1;
			rdBoundsFloat.top = (float)ydLineTop;
			rdBoundsFloat.bottom = (float)ydLineBottom;
		}

		int dx, dy;
		CheckHr(pvg->GetTextExtent(1, L"¶", &dx, &dy));
		if (m_pseg->paraRightToLeft())
			rdBoundsFloat.left -= dx;
		else
			rdBoundsFloat.right += dx;
		fAny = true; // make sure we draw it!
	}

	pfont->restoreDC();

	// NOTE: We don't want to round the float value here because this causes overlapping of
	// two segments' rectangles sometimes. (TE-4497)
	prdBounds->top    = (int)rdBoundsFloat.top;
	prdBounds->bottom = (int)rdBoundsFloat.bottom;
	prdBounds->left   = (int)rdBoundsFloat.left;
	prdBounds->right  = (int)rdBoundsFloat.right;

	*pfAnythingToDraw = (ComBool)fAny;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Convert a click position to a character position.

	@param pvg					- graphics object to use in measuring text
	@param rs, rd				- source/destination coordinates, for scaling
	@param zptdClickPosition	- relative to the segment draw origin
	@param pichw				- return character clicked before
	@param pfAssocPrev			- return true if they clicked on the trailing half of
									the previous char, false if click was on the
									leading half of the following
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::PointToChar(int ichwBase, IVwGraphics * pvg,
	RECT rs, RECT rd,
	POINT zptdClickPosition, int * pichw,
	ComBool * pfAssocPrev)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComOutPtr(pichw);
	ChkComOutPtr(pfAssocPrev);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	bool fAP;
	FwGrWinSegmentPainter segp(m_pseg, 0);
	SetTransformRects(&segp, rs, rd);
	segp.pointToChar(zptdClickPosition, pichw, &fAP);
	*pfAssocPrev = (ComBool)fAP;
	*pichw = m_pgts->GrToVwOffset(*pichw);
	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

// Review: may need a way to do this and subsequent methods with one result per method?
/*----------------------------------------------------------------------------------------------
	Indicate what logical position an arrow key should move the IP to.

	@param pichwIP			- initial position and also result
	@param pfAssocPrev		- is the IP "leaning" backwards?
	@param fRight			- direction of desired movement (physical or logical?)
	@param fMovingIn		- to this segment; if so, initial pichwIP meaningless
	@param pfResult			- if false, try next segment or string
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::ArrowKeyPosition(int ichwBase, IVwGraphics * pvg,
	int * pichwIP, ComBool * pfAssocPrev,
	ComBool fRight, ComBool fMovingIn,
	ComBool * pfResult)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComArgPtr(pichwIP);
	ChkComArgPtr(pfAssocPrev);
	ChkComOutPtr(pfResult);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	bool fAP = *pfAssocPrev; // input and output
	bool fInThisSeg = (bool)(!fMovingIn);
	FwGrWinSegmentPainter segp(m_pseg, 0);
	int ichwRet = segp.arrowKeyPosition(m_pgts->VwToGrOffset(*pichwIP), &fAP, (bool)fRight, &fInThisSeg);
	*pfAssocPrev = (ComBool)fAP;
	*pfResult = (ComBool)fInThisSeg;
	*pichwIP = m_pgts->GrToVwOffset(ichwRet);

	pfont->restoreDC();

	//bool fAP = *pfAssocPrev; // input and output
	//bool fRes;
	//FwGrWinSegmentPainter segp(m_pseg, 0);
	//HRESULT hr = (HRESULT)segp.arrowKeyPosition(pichwIP, &fAP,
	//	(bool)fRight, (bool)fMovingIn, &fRes);
	//*pfAssocPrev = (ComBool)fAP;
	//*pfResult = (ComBool)fRes;
	//return hr;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Indicate what logical position a shift-arrow-key combination should move the
	end of the selection to.

	@param pichw			- initial endpoint and also adjusted result
	@param fAssocPrev		- association of the end-point, ie, true if it follows the anchor
	@param ichAnchor		- -1 if anchor is in a different segment
	@param fRight			- direction of desired movement
	@param fMovingIn		- true if we are moving in to this segment;
								if so, initial pichw meaningless
	@param pfRet			- if false try next seg or string
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::ExtendSelectionPosition(int ichwBase, IVwGraphics * pvg,
	int * pichw, ComBool fAssocPrevMatch, ComBool fAssocPrevNeeded, int ichwAnchor,
	ComBool fRight, ComBool fMovingIn,
	ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComArgPtr(pichw);
	ChkComOutPtr(pfRet);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	bool fInThisSeg = (bool)(!fMovingIn);
	FwGrWinSegmentPainter segp(m_pseg, 0);
	int ichwNewEnd = segp.extendSelectionPosition(m_pgts->VwToGrOffset(*pichw),
		(bool)fAssocPrevMatch, (bool)fAssocPrevNeeded,
		ichwAnchor, (bool)fRight, &fInThisSeg);
	*pfRet = (ComBool)fInThisSeg;
	*pichw = m_pgts->GrToVwOffset(ichwNewEnd);

	pfont->restoreDC();

	//bool fRet;
	//FwGrWinSegmentPainter segp(m_pseg, 0);
	//HRESULT hr = (HRESULT)segp.extendSelectionPosition(pichw,
	//	(bool)fAssocPrevMatch, (bool)fAssocPrevNeeded, ichwAnchor, (bool)fRight,
	//	(bool)fMovingIn, &fRet);
	//*pfRet = (ComBool)fRet;
	//return hr;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Used to find out where underlines should be drawn.

	TODO SharonC: handle end-of-line spaces properly--they shouldn't be underlined.

	@param ichwMin/Lim		- range of text of interest
	@param rs, rd			- source/destination coordinates, for scaling
	@param fSkipSpace		- true if white space should not be underlined; some renderers may
								ignore this
	@param crgMax			- number of ranges allowed
	@param pcxd				- number of ranges made
	@param prgxdLefts/Rights/UnderTops
							- arrays of corresponding values indicating where an underline
								should be drawn, in logical order; if a double underline is
								needed prgxdUnderTops indicates the position(s) of the top line.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrSegment::GetCharPlacement(int ichwBase, IVwGraphics * pvg,
	int ichwMin, int ichwLim,
	RECT rs, RECT rd,
	ComBool fSkipSpace,
	int cxdMax,
	int * pcxd, int * prgxdLefts, int * prgxdRights, int * prgydUnderTops)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComArrayArg(pcxd, cxdMax);
	ChkComArrayArg(prgxdLefts, cxdMax);
	ChkComArrayArg(prgxdRights, cxdMax);
	ChkComArrayArg(prgydUnderTops, cxdMax);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	// Calculate the y-position.
	//int rsHeight = rs.bottom - rs.top;
	//int rdHeight = rd.bottom - rd.top;
	//int dydGap = rdHeight / 96;
	//int dydSubscript = 0; // max(0, (-1 * GrEngine::GrMulDiv(m_dysOffset, rsHeight, rdHeight)));
	//int ydBottom = ScaleY((int)pfont->ascent(), rs, rd) + dydGap + dydSubscript;

	gr::Rect rsFloat(rs);
	gr::Rect rdFloat(rd);

	float * prgxysFloat = new float[cxdMax * 3]; // 3 for lefts, rights, tops

	FwGrWinSegmentPainter segp(m_pseg, 0);
	SetTransformRects(&segp, rsFloat, rdFloat);
	*pcxd = segp.getUnderlinePlacement(m_pgts->VwToGrOffset(ichwMin), m_pgts->VwToGrOffset(ichwLim),
		(bool)fSkipSpace, cxdMax,
		prgxysFloat, prgxysFloat + cxdMax, prgxysFloat + (cxdMax*2));

	for (int i = 0; i < cxdMax; i++)
	{
		// NOTE: We don't want to round the float value here because this causes overlapping of
		// two segments' rectangles sometimes. (TE-4497)
		if (prgxdLefts)
			prgxdLefts[i] = (int)prgxysFloat[i];
		if (prgxdRights)
			prgxdRights[i] = (int)prgxysFloat[cxdMax + i];
		if (prgydUnderTops)
			prgydUnderTops[i] = (int)prgxysFloat[(cxdMax*2) + i];
	}

	delete[] prgxysFloat;

	//for (int i = 0; i < min(cxdMax, *pcxd); i++)
	//	prgydUnderTops[i] = ydBottom;

	pfont->restoreDC();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Return the rendered glyphs and their x- and y-coordinates.
----------------------------------------------------------------------------------------------*/
/*
STDMETHODIMP FwGrSegment::GetGlyphsAndPositions(int ichwBase, IVwGraphics * pvg,
	RECT rs, RECT rd, int cchMax, int * pcchRet,
	OLECHAR * prgchGlyphs, int * prgxd, int * prgyd)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pcchRet);
	ChkComArrayArg(prgchGlyphs, cchMax);
	ChkComArrayArg(prgxd, cchMax);
	ChkComArrayArg(prgyd, cchMax);

	m_pseg->setTextSourceOffset(ichwBase);

	return (HRESULT)m_pseg->GetGlyphsAndPositions(ichwBase, rs, rd, cchMax, pcchRet,
		prgchGlyphs, prgxd, prgyd);
	END_COM_METHOD(g_fact, IID_ILgSegment);
}
*/

/*----------------------------------------------------------------------------------------------
	For debugging. Return the characters in this segment.
----------------------------------------------------------------------------------------------*/
/*
STDMETHODIMP FwGrSegment::GetCharData(int ichsBase, int cchMax, OLECHAR * prgch, int * pcchRet)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prgch);
	ChkComOutPtr(pcchRet);

	m_pseg->setTextSourceOffset(ichsBase);

	return (HRESULT)m_pseg->GetCharData(cchMax, prgch, pcchRet);

	END_COM_METHOD(g_fact, IID_ILgSegment);
}
*/

/*----------------------------------------------------------------------------------------------
	Justify the segment.
----------------------------------------------------------------------------------------------*/
HRESULT FwGrSegment::JustifyIfNeeded(int ichwBase, IVwGraphics * pvg)
{
	if (m_dxsStretchNeeded == 0)
		return S_OK;

	Assert(!m_fStretched);

	if (m_pwsegp)	// segment painter cache is no longer appropriate
	{
		delete m_pwsegp;
		m_pwsegp = NULL;
	}

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	LayoutEnvironment & layout = m_pseg->Layout();
	std::ofstream strmLog;
	std::ostream * pstrmLog = m_qreneng->CreateLogFile(strmLog, true);
	layout.setLoggingStream(pstrmLog);

	float xsCurrentWidth = m_pseg->advanceWidth();

	Segment * psegJust = Segment::JustifiedSegment(*m_pseg,
		xsCurrentWidth + m_dxsStretchNeeded);

	Assert(psegJust->segmentTermination() != kestNothingFit);

	Segment * psegPrev = m_pseg;
	m_pseg = psegJust;

	strmLog.close();

	m_fStretched = true; // we did our best
	m_dxsStretchNeeded = 0;

	pfont->restoreDC(); // before deleting the old segment
	delete psegPrev;

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Change the line end flags on the segment. This may or may not require throughly
	regenerating the segment.
----------------------------------------------------------------------------------------------*/
HRESULT FwGrSegment::ChangeLineEnds(int ichwBase, IVwGraphics * pvg)
{
	if (!m_fChangeLineEnds)
		return S_OK;

	if (m_pseg->startOfLine() == m_fNewStart && m_pseg->endOfLine() == m_fNewEnd)
	{
		// No change needed.
		m_fChangeLineEnds = false;
		return S_OK;
	}

	if (m_pwsegp)	// segment painter cache is no longer appropriate
	{
		delete m_pwsegp;
		m_pwsegp = NULL;
	}

	bool fEolOnly = (m_pseg->startOfLine() == m_fNewStart); // we are switching end-of-line

	if (fEolOnly && m_psegAltLineEnd)
	{
		// Switch back to the one we saved.
		Segment * psegTemp = m_pseg;
		m_pseg = m_psegAltLineEnd;
		m_psegAltLineEnd = psegTemp;
		return S_OK;
	}

	//Assert(!m_fStretched);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	gr::Font & font = m_pseg->getFont();
	WinFont * pfont = dynamic_cast<WinFont *>(&font);
	pfont->replaceDC(hdc);

	m_pseg->setTextSourceOffset(m_pgts->VwToGrOffset(ichwBase));

	Segment * psegPrev = m_pseg;
	LayoutEnvironment & layout = psegPrev->Layout();

	std::ofstream strmLog;
	std::ostream * pstrmLog = m_qreneng->CreateLogFile(strmLog, true);
	layout.setLoggingStream(pstrmLog);

	m_pseg = Segment::LineContextSegment(*psegPrev, m_fNewStart, m_fNewEnd);

	strmLog.close();

	// Do this before possibly deleting the old segment which will delete the font.
	pfont->restoreDC();

	if (fEolOnly)
		// The most common change is to just change the end-line flag, and then we fairly
		// often switch back to the original. So save the original.
		m_psegAltLineEnd = psegPrev;
	else
	{
		delete psegPrev;
		delete m_psegAltLineEnd;
		m_psegAltLineEnd = NULL;
	}

	m_fChangeLineEnds = false;

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Set up the transformation and scaling factors on the segment painter.
----------------------------------------------------------------------------------------------*/
void FwGrSegment::SetTransformRects(SegmentPainter * psegp, gr::Rect rs, gr::Rect rd)
{
	psegp->setOrigin(rs.left * -1, rs.top * -1); // ORIGIN
	psegp->setPosition(rd.left, rd.top);
	psegp->setScalingFactors(
		(rd.right - rd.left) / (rs.right - rs.left),
		(rd.top - rd.bottom) / (rs.top - rs.bottom));
}


//:>********************************************************************************************
//:>	   Debugger methods
//:>********************************************************************************************

//:Ignore

/*----------------------------------------------------------------------------------------------
	Return a string containing the glyphs in the output of the final pass. Caller is
	responsible for freeing the BSTR.
----------------------------------------------------------------------------------------------*/
HRESULT FwGrSegment::debug_OutputText(BSTR * pbstrResult)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Return the corresponding underlying association.

	@param islout		- glyph index relative to the beginning of the segment
	@param pichw		- character relative to the beginning of the string
----------------------------------------------------------------------------------------------*/
// TODO AlanW: use above method
HRESULT FwGrSegment::debug_LogicalSurfaceToUnderlying(int ichwBase, int islout, ComBool fBefore,
	int * pichwRet)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Return the corresponding surface association.

	@param ichw			- character relative to the beginning of the string
	@param fBefore		- do we want the before association?
							(Note that ichw = 3, fBefore = false is relevant to the
							selection between characters 3 and 4)
	@param pislout		- glyph index relative to the beginning of the segment
----------------------------------------------------------------------------------------------*/
// TODO AlanW: use above method
HRESULT FwGrSegment::debug_UnderlyingToLogicalSurface(int ichwBase, int ichw, ComBool fBefore,
	int * pisloutRet)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Return the corresponding ligature in the surface, or kNegInfinity if this character
	is not a ligature component, or its ligature is not rendered within this segment.

	Review: should we return kPosInfinity if this character is a component whose ligature is
	rendered in the following segment?

	@param ichw		- character relative to the beginning of the string
	@param pislout		- glyph index relative to the beginning of the segment
----------------------------------------------------------------------------------------------*/
HRESULT FwGrSegment::debug_Ligature(int ichwBase, int ichw, int * pisloutRet)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Return the index of the ligature component, or -1 if this item is not a
	component or its ligature is not rendered within this segment.

	@param ichw			- character relative to the beginning of the string
	@param piCompRet	- index of component
----------------------------------------------------------------------------------------------*/
HRESULT FwGrSegment::debug_LigComponent(int ichwBase, int ichw, int * piCompRet)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Return the index of the underlying character that corresponds to the given component,
	-1 if this slot is not a ligature, -2 if there aren't that many components.

	@param islout		- glyph index relative to the beginning of the segment
	@param iComp		- component
	@param pichwRet		- character relative to the beginning of the string
----------------------------------------------------------------------------------------------*/
HRESULT FwGrSegment::debug_UnderlyingComponent(int ichwBase, int islout, int iComp, int * pichwRet)
{
	return E_NOTIMPL;
}

//:>********************************************************************************************
//:>	FwGrWinSegmentPainter methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwGrWinSegmentPainter::FwGrWinSegmentPainter(Segment * pseg, HDC hdc,
	float xsOrigin, float ysOrigin,
	bool fIsLastLineOfSelection)
		: WinSegmentPainter(pseg, hdc, xsOrigin, ysOrigin)
{
	m_fIsLastLineOfSelection = fIsLastLineOfSelection;
	m_fAppendIndicator = false;
	m_fSuppressBackground = false;
	m_fPaintErase = false;
}

/*----------------------------------------------------------------------------------------------
	Highlight a range of text. The FW version extends the selection the equivalent of the
	paragraph marker character (¶) at the end of a paragraph.
----------------------------------------------------------------------------------------------*/
bool FwGrWinSegmentPainter::drawSelectionRange(int ichwAnchor, int ichwEnd,
	float ydLineTop, float ydLineBottom, bool bOn)
{
	if (ichwEnd == m_pseg->getText().getLength() && !m_fIsLastLineOfSelection &&
		ichwEnd >= m_pseg->startCharacter() && ichwEnd <= m_pseg->stopCharacter())
	{
		m_fAppendIndicator = true;
	}

	return WinSegmentPainter::drawSelectionRange(ichwAnchor, ichwEnd,
		ydLineTop, ydLineBottom, bOn);
}

/*----------------------------------------------------------------------------------------------
	When suppressing background colors, skip this altogether.
----------------------------------------------------------------------------------------------*/
void FwGrWinSegmentPainter::paintBackground(int xs, int ys)
{
	if (m_fSuppressBackground)
		return;
	WinSegmentPainter::paintBackground(xs, ys);
}

// Set font color for paintForeground...isolated for override in special cases.
void FwGrWinSegmentPainter::setForegroundPaintColor(GlyphStrmKey & gsk)
{
	if (m_fPaintErase)
	{
		// We want to erase a previous paint...use the background color, or if that is
		// transparent, use white.
		unsigned long clr = gsk.clrBack;
		if (clr == (unsigned long)kclrTransparent)
			clr = (unsigned long)kclrWhite;
		SetFontProps(clr, (unsigned long)kclrTransparent);
	}
	else
	{
		// normal operation.
		WinSegmentPainter::setForegroundPaintColor(gsk);
	}
}
/*----------------------------------------------------------------------------------------------
	When suppressing background colors, we need to do an extra paint with a forced foreground
	color. This erases the previous text painted, preventing ClearType from making doubly-
	painted text darker than we want.
----------------------------------------------------------------------------------------------*/
void FwGrWinSegmentPainter::paintForeground(int xs, int ys)
{
	if (m_fSuppressBackground)
	{
		// Initial paint in erase mode prevents ClearType making the text bolder if, as is usual
		// when doing suppressBackground, we already drew this once.
		m_fPaintErase = true;
		WinSegmentPainter::paintForeground(xs, ys);
		m_fPaintErase = false;
	}
	WinSegmentPainter::paintForeground(xs, ys);
}
/*----------------------------------------------------------------------------------------------
	When desired, extend the highlight to include the equivalent of the
	paragraph marker character (¶) at the end of a paragraph.
----------------------------------------------------------------------------------------------*/
void FwGrWinSegmentPainter::AdjustHighlightRectangles(float ydLineTop, float ydLineBottom,
	float xdSegLeft, float xdSegRight, std::vector<Rect> & vrd)
{
	if (m_fAppendIndicator)
	{
		SIZE size;
		::GetTextExtentPoint32(m_hdc, L"¶", 1, &size);
		// Scale the size.
		size.cx = (LONG)(size.cx * m_xFactor);

		Rect rectParaMarker;
		rectParaMarker.top = ydLineTop;
		rectParaMarker.bottom = ydLineBottom;
		if (m_pseg->paraRightToLeft())
		{
			rectParaMarker.right = xdSegLeft;
			rectParaMarker.left = xdSegLeft - size.cx;
		}
		else
		{
			rectParaMarker.left = xdSegRight;
			rectParaMarker.right = xdSegRight + size.cx;
		}
		vrd.push_back(rectParaMarker);
	}
}

//:End Ignore
