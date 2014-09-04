/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GraphiteSegment.cpp
Responsibility: Damien Daspit
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "Main.h"
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
static DummyFactory g_fact(_T("SIL.Language1.GraphiteSeg"));

//:>********************************************************************************************
//:>	   Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructors.
----------------------------------------------------------------------------------------------*/
GraphiteSegment::GraphiteSegment()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_qts = NULL;
	m_qgre = NULL;
	m_ichMin = 0;
	m_ichLim = 0;
	m_dirDepth = 0;
	m_paraRtl = false;
	m_width = -1;
	m_stretch = 0;
	m_wsOnly = false;
}

GraphiteSegment::GraphiteSegment(IVwTextSource* pts, GraphiteEngine* pgre, int ichMin, int ichLim,
	int dirDepth, bool paraRtl, bool wsOnly)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_qts = pts;
	m_qgre = pgre;
	m_ichMin = ichMin;
	m_ichLim = ichLim;
	m_dirDepth = dirDepth;
	m_paraRtl = paraRtl;
	m_width = -1;
	m_stretch = 0;
	m_wsOnly = wsOnly;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
GraphiteSegment::~GraphiteSegment()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP GraphiteSegment::QueryInterface(REFIID riid, void** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown*>(this);
	else if (riid == IID_ILgSegment)
		*ppv = static_cast<ILgSegment*>(this);
	else if (&riid == &CLSID_GraphiteSegment)			// trick one for engine to get an impl
		*ppv = static_cast<GraphiteSegment*>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_ILgSegment);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   ILgSegment Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Identical to DrawText, except passes true to DoAllRuns as final argument.
	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::DrawTextNoBackground(int ichBase, IVwGraphics* pvg,
	RECT rcSrc, RECT rcDst, int* pdxdWidth)
{
	BEGIN_COM_METHOD;

	Rect srcRect(rcSrc);
	Rect dstRect(rcDst);

	Assert(srcRect.Width() == dstRect.Width());
	Assert(srcRect.Height() == dstRect.Height());

	if (m_width < 0)
		Compute(ichBase, pvg);

	if (m_glyphs.size() > 0)
	{
		LgCharRenderProps chrp;
		int ichMinDum, ichLim;
		CheckHr(m_qts->GetCharProps(m_ichMin, &chrp, &ichMinDum, &ichLim));
		InterpretChrp(chrp);

		int x = srcRect.MapXTo(0, dstRect);
		int y = srcRect.MapYTo(0, dstRect) - MulDiv(chrp.dympOffset, dstRect.Height(), kdzmpInch);

		COLORREF temp = chrp.clrFore;
		chrp.clrFore = chrp.clrBack;
		if (chrp.clrFore == (COLORREF) kclrTransparent)
			chrp.clrFore = kclrWhite;
		chrp.clrBack = (COLORREF) kclrTransparent;
		CheckHr(pvg->SetupGraphics(&chrp));
		CheckHr(pvg->DrawGlyphs(x, y, m_glyphs.size(), &m_glyphs[0]));

		chrp.clrFore = temp;
		CheckHr(pvg->SetupGraphics(&chrp));
		CheckHr(pvg->DrawGlyphs(x, y, m_glyphs.size(), &m_glyphs[0]));
	}

	*pdxdWidth = m_width;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::DrawText(int ichBase, IVwGraphics* pvg,
	RECT rcSrc, RECT rcDst, int* pdxdWidth)
{
	BEGIN_COM_METHOD;

	Rect srcRect(rcSrc);
	Rect dstRect(rcDst);

	Assert(srcRect.Width() == dstRect.Width());
	Assert(srcRect.Height() == dstRect.Height());

	if (m_width < 0)
		Compute(ichBase, pvg);

	if (m_glyphs.size() > 0)
	{
		LgCharRenderProps chrp;
		int ichMinDum, ichLim;
		CheckHr(m_qts->GetCharProps(m_ichMin, &chrp, &ichMinDum, &ichLim));
		InterpretChrp(chrp);

		int x = srcRect.MapXTo(0, dstRect);
		int y = srcRect.MapYTo(0, dstRect) - MulDiv(chrp.dympOffset, dstRect.Height(), kdzmpInch);

		CheckHr(pvg->SetupGraphics(&chrp));
		CheckHr(pvg->DrawGlyphs(x, y, m_glyphs.size(), &m_glyphs[0]));
	}

	*pdxdWidth = m_width;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Tell the segment its measurements are invalid, because we are going to start asking
	questions using a different VwGraphics.
	This method is not used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::Recompute(int ichBase, IVwGraphics* pvg)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	The distance the drawing point should advance after drawing this segment.
	Always positive, even for RtoL segments. This includes any stretch that has
	been requested.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_Width(int ichBase, IVwGraphics* pvg, int* pdxs)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdxs);

	if (m_width < 0)
		Compute(ichBase, pvg);
	*pdxs = m_width;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Distance that drawing takes place to the right of the rectangle specified by the width
	and drawing origin.
	It is legitimate not to implement this; calling code will typically treat it as zero.
	However, the not-implemented could be used to make sure we allow a few pixels.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_RightOverhang(int ichBase, IVwGraphics* pvg, int* px)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Overhang to the left of the drawing origin. Value returned will be >= 0.
	It is legitimate not to implement this; calling code will typically treat it as zero.
	However, the not-implemented could be used to make sure we allow a few pixels.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_LeftOverhang(int ichBase, IVwGraphics* pvg, int* px)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Height of the drawing rectangle. This is normally the full font height, even if all chars
	in segment are lower case or none have descenders, or even if segment is empty.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_Height(int ichBase, IVwGraphics* pvg, int* pdys)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdys);

	if (m_width < 0)
		Compute(ichBase, pvg);
	*pdys = m_fontAscent + m_fontDescent;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	The ascent of the font typically used to draw this segment. Basically, it determines the
	baseline used to align it.

	ENHANCE JohnT: we may need to make more than one baseline explicit in the interface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_Ascent(int ichBase, IVwGraphics* pvg, int* pdys)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdys);

	if (m_width < 0)
		Compute(ichBase, pvg);
	*pdys = m_fontAscent;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Obtains height and width in a single call.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::Extent(int ichBase, IVwGraphics* pvg, int* pdxs, int* pdys)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdxs);
	ChkComOutPtr(pdys);

	if (m_width < 0)
		Compute(ichBase, pvg);
	*pdxs = m_width;
	*pdys = m_fontAscent + m_fontDescent;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Compute the rectangle in destination coords which contains all the pixels
	drawn by this segment. This should be a sufficient rectangle to invalidate
	if this segment is about to be discarded and replaced by another.
	This method is not used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::BoundingRect(int ichBase, IVwGraphics* pvg, RECT rcSrc,
	RECT rcDst, RECT* prcBounds)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Distance that drawing takes place above the rectangle specified by the ascent
	and drawing origin.
	It is legitimate not to implement this; calling code will typically treat it as zero.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_AscentOverhang(int ichBase, IVwGraphics* pvg, int* py)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Distance that drawing takes place below the rectangle specified by the descent
	and drawing origin. Value returned will be >= 0.
	It is legitimate not to implement this; calling code will typically treat it as zero.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_DescentOverhang(int ichBase, IVwGraphics* pvg, int* py)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Compute the width the segment would occupy if drawn with the specified
	parameters. Don't update cached width.
	This method is not used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::GetActualWidth(int ichBase, IVwGraphics* pvg,
	RECT rcSrc, RECT rcDst, int* pdxdWidth)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Answer whether the segment is right to left.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_RightToLeft(int ichBase, ComBool* pfResult)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfResult);

	*pfResult = IsRtl();

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Answer how deeply embedded this segment is in relative to the paragraph direction--
	how many changes of direction have to happen to get this in the right direction.
	Also indicate if this has weak directionality, eg, a white-space-only segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_DirectionDepth(int ichBase, int* pnDepth, ComBool* pfWeak)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pnDepth);
	ChkComOutPtr(pfWeak);

	*pnDepth = m_dirDepth;
	*pfWeak = m_wsOnly;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Set the direction of the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::SetDirectionDepth(int ichBase, int nNewDepth)
{
	BEGIN_COM_METHOD

	if (nNewDepth == m_dirDepth)
	{
		return S_OK;
	}
	else if ((nNewDepth % 2) == (m_dirDepth % 2))
	{
		m_dirDepth = nNewDepth;
		return S_OK;
	}
	else if (!m_wsOnly)
	{
		return E_FAIL;
	}
	else
	{
		m_dirDepth = nNewDepth;
		return S_OK;
	}

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the cookies which identify the old writing system of the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_WritingSystem(int ichBase, int* pws)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pws);

	LgCharRenderProps chrp;
	int ichMinRun, ichLimRun;
	CheckHr(m_qts->GetCharProps(ichBase, &chrp, &ichMinRun, &ichLimRun));
	*pws = chrp.ws;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the last character that is logically part of the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_Lim(int ichBase, int* pdich)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pdich);

	*pdich = m_ichLim - m_ichMin;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the last character of interest to the segment. UnicodeSeg never renders any characters
	beyond its logical end.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_LimInterest(int ichBase, int* pdich)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pdich);

	*pdich = m_ichLim - m_ichMin;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Changes the end-of-line status of the segment. This is used after making the last segment
	of a string if we want to attempt to put some more text after it (e.g., in another
	old writing system).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::put_EndLine(int ichBase, IVwGraphics* pvg, ComBool fNewVal)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Changes the start-of-line status of the segment. This is used after making the last segment
	of a string if we want to attempt to put some more text after it (e.g., in another
	old writing system).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::put_StartLine(int ichBase, IVwGraphics* pvg, ComBool fNewVal)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the type of break that occurs at the logical start of the segment.
	This method is not used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_StartBreakWeight(int ichBase, IVwGraphics* pvg,
	LgLineBreak* plbrk)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Get the type of break that occurs at the logical end of the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_EndBreakWeight(int ichBase, IVwGraphics* pvg,
	LgLineBreak* plbrk)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(plbrk);

	*plbrk = klbNoBreak;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Read the amount of stretch that has been set for the segment. This is not included in
	the result returned from width, but the segment gets this much wider when drawn.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::get_Stretch(int ichBase, int* pdxs)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pdxs);

	*pdxs = m_stretch;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Set the amount of stretch that has been set for the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::put_Stretch(int ichBase, int dxs)
{
	BEGIN_COM_METHOD

	m_stretch = dxs;
	m_width = -1;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Test whether IP at specified position is valid.
	If position is not in this segment, and there are following segments, may answer
	kipvrUnknown; client should then try subsequent segments.
	Note: This routine gives the finest possible granularity (where IPs can be rendered).
	Particular input methods may not support all the positions that the renderer says are valid.
	Compare ILgInputMethodEditor>>IsValidInsertionPoint.  Clients should generally check both.

	@param ichBase
	@param pvg
	@param ich
	@param pipvr
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::IsValidInsertionPoint(int ichBase, IVwGraphics* pvg, int ich,
	LgIpValidResult* pipvr)
{
	BEGIN_COM_METHOD
	// Allow this argument to be null, as we don't actually use it, and some local methods
	// do in fact pass null.
	ChkComArgPtrN(pvg);
	ChkComArgPtr(pipvr);

	if (m_width < 0)
		Compute(ichBase, pvg);

	if (ich < m_ichMin || ich > m_ichLim)
	{
		*pipvr = kipvrUnknown;
	}
	else if (ich == m_ichLim)
	{
		*pipvr = kipvrOK;
	}
	else
	{
		*pipvr = binary_search(m_clusters.begin(), m_clusters.end(), Cluster(ich)) ? kipvrOK : kipvrBad;
	}

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------

	Arguments:
		fBoundaryEnd		 asking about the logical end boundary?
		fBoundaryRight		 asking about the physical right boundary?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::DoBoundariesCoincide(int ichBase, IVwGraphics* pvg,
	ComBool fBoundaryEnd, ComBool fBoundaryRight, ComBool* pfResult)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pfResult);

	if (IsRtl())
		*pfResult = fBoundaryEnd != fBoundaryRight;
	else
		*pfResult = fBoundaryEnd == fBoundaryRight;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Draw an insertion point at an appropriate position.
	Arguments:
		rcSrc				 as for DrawText
		ich					 must be valid
		fAssocPrev			 primary associated with preceding character?
		fOn					 turning on or off? Caller should alternate, on first.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::DrawInsertionPoint(int ichBase, IVwGraphics* pvg,
	RECT rcSrc, RECT rcDst, int ich, ComBool fAssocPrev, ComBool fOn, LgIPDrawMode dm)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);

	Rect primary, secondary;
	ComBool primaryHere, secondaryHere;
	CheckHr(PositionsOfIP(ichBase, pvg, rcSrc, rcDst, ich, fAssocPrev, dm, &primary, &secondary,
		&primaryHere, &secondaryHere));
	if (primaryHere)
		CheckHr(pvg->InvertRect(primary.left, primary.top, primary.right, primary.bottom));

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Give bounding rectangles for the (possibly 2-part) IP. If the IP has only one part,
	pfSecHere will never be set true for any segment.
	Arguments:
		rcSrc				 as for DrawText
		fAssocPrev			 primary associated with preceding character?
		 rectPrimary		 source coords
		 rectSecondary		 source coords
		 pfPrimaryHere		 set true if this segment renders the primary IP
		 pfSecHere			 set true if this segment renders the secondary IP.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::PositionsOfIP(int ichBase, IVwGraphics* pvg,
	RECT rcSrc, RECT rcDst, int ich, ComBool fAssocPrev, LgIPDrawMode dm,
	RECT* prectPrimary, RECT* prectSecondary,
	ComBool* pfPrimaryHere, ComBool* pfSecHere)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(prectPrimary);
	ChkComArgPtr(prectSecondary);
	ChkComOutPtr(pfPrimaryHere);
	ChkComOutPtr(pfSecHere);

	Rect srcRect(rcSrc);
	Rect dstRect(rcDst);

	Assert(srcRect.Width() == dstRect.Width());
	Assert(srcRect.Height() == dstRect.Height());

	if (m_width < 0)
		Compute(ichBase, pvg);

	if (!CanDrawIP(ich, fAssocPrev))
	{
		*pfPrimaryHere = false;
		return S_OK;
	}

	LgCharRenderProps chrp;
	int ichMinDum, ichLim;
	CheckHr(m_qts->GetCharProps(m_ichMin, &chrp, &ichMinDum, &ichLim));
	InterpretChrp(chrp);

	prectPrimary->left = GetSelectionX(ich, srcRect, dstRect) - 1;
	prectPrimary->top = srcRect.MapYTo(0, dstRect) - MulDiv(chrp.dympOffset, dstRect.Height(), kdzmpInch);
	prectPrimary->right = prectPrimary->left + 2;
	prectPrimary->bottom = prectPrimary->top + m_fontAscent + m_fontDescent;
	*pfPrimaryHere = true;

	*pfSecHere = false;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Highlight a range of text.
	Arguments:
		rcSrc				 as for DrawText
		ichMin				 must be valid
		ydTop				 of area to highlight if whole line height;
		ydBottom			 includes half of inter-line spacing.
	ENHANCE JohnT: it would be better to use the proper Uniscribe approach of redrawing
	the selected text in a special mode.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::DrawRange(int ichBase, IVwGraphics* pvg,
	RECT rcSrc, RECT rcDst, int ichMin, int ichLim, int ydTop, int ydBottom, ComBool bOn,
	ComBool fIsLastLineOfSelection, RECT* prsBounds)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(prsBounds);

	Rect rcBounds;
	ComBool fAnythingToDraw;
	CheckHr(PositionOfRange(ichBase, pvg, rcSrc, rcDst, ichMin, ichLim, ydTop, ydBottom,
		fIsLastLineOfSelection, &rcBounds, &fAnythingToDraw));

	if (fAnythingToDraw)
		CheckHr(pvg->InvertRect(rcBounds.left, rcBounds.top, rcBounds.right, rcBounds.bottom));

	*prsBounds = rcBounds;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get a bounding rectangle that will contain the area highlighted by this segment
	when drawing the specified range.
	Arguments:
		rcSrc				 as for DrawText
		 rsBounds			 source coords; used to return the result
		 pfAnythingToDraw	true if any part of range drawn by segment
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::PositionOfRange(int ichBase, IVwGraphics* pvg,
	RECT rcSrc, RECT rcDst, int ichMin, int ichLim, int ydTop, int ydBottom,
	ComBool fIsLastLineOfSelection, RECT* prsBounds, ComBool* pfAnythingToDraw)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(prsBounds);
	ChkComOutPtr(pfAnythingToDraw);

	Rect srcRect(rcSrc);
	Rect dstRect(rcDst);

	Assert(srcRect.Width() == dstRect.Width());
	Assert(srcRect.Height() == dstRect.Height());

	if (m_width < 0)
		Compute(ichBase, pvg);

	int left, right;
	if (ichMin == 0 && ichLim == 0)
	{
		// Special case for drawing empty paragraph range. Invert the region that would be
		// occupied by one paragraph character in the font.
		left = srcRect.MapXTo(0, dstRect);
		int dx, dy;
#pragma warning(disable: 4428)
		static OleStringLiteral paraMark(L"\u00B6");
#pragma warning(default: 4428)
		CheckHr(pvg->GetTextExtent(1, paraMark, &dx, &dy));
		if (m_paraRtl)
		{
			right = left;
			left = right - dx;
		}
		else
		{
			right = left + dx;
		}
		*pfAnythingToDraw = true;
	}
	else
	{
		// If the indices cover a larger range than the segment,
		// adjust them
		if (ichMin < m_ichMin)
			ichMin = m_ichMin;
		if (ichLim > m_ichLim)
			ichLim = m_ichLim;
		// If that leaves nothing to draw, do nothing
		if (ichMin >= ichLim)
			return S_OK;
		// OK, we will draw something.

		if (IsRtl())
		{
			left = GetSelectionX(ichLim, srcRect, dstRect);
			right = GetSelectionX(ichMin, srcRect, dstRect);
		}
		else
		{
			left = GetSelectionX(ichMin, srcRect, dstRect);
			right = GetSelectionX(ichLim, srcRect, dstRect);
		}

		*pfAnythingToDraw = true;
	}

	prsBounds->left = left;
	prsBounds->right = right;
	prsBounds->top = ydTop;
	prsBounds->bottom = ydBottom;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Given a point in or near the segment, determine what character position it corresponds to.
	Arguments:
		 rcSrc, rcDst		as for DrawText
		 ptClickPosition	 dest coords
		 pfAssocPrev		 true if click was logically before indicated position
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::PointToChar(int ichBase, IVwGraphics* pvg,
	RECT rcSrc, RECT rcDst, POINT ptdClickPosition, int* pich, ComBool* pfAssocPrev)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(pich);
	ChkComArgPtr(pfAssocPrev);

	Rect srcRect(rcSrc);
	Rect dstRect(rcDst);

	Assert(srcRect.Width() == dstRect.Width());
	Assert(srcRect.Height() == dstRect.Height());

	if (m_width < 0)
		Compute(ichBase, pvg);

	int point15 = MulDiv(15, dstRect.Height(), kdzptInch);
	int top = srcRect.MapYTo(0, dstRect);
	// If the point is right outside our rectangle, answer one of our bounds
	// If it is much above us (15 points, a line or so), treat as at the start
	if (ptdClickPosition.y < top - point15)
	{
		*pich = m_ichMin;
		*pfAssocPrev = false;
		return S_OK;
	}

	// If the point is much below, treat as end, whatever x position.
	// Mapping 0 to dest coords gives us the Y position of the top of the segment.
	if (ptdClickPosition.y > top + m_fontAscent + m_fontDescent + point15)
	{
		*pich = m_ichLim;
		*pfAssocPrev = true;
		return S_OK;
	}

	// check if the click position is before this segment
	if (ptdClickPosition.x <= srcRect.MapXTo(-1, dstRect))
	{
		if (IsRtl())
		{
			*pich = m_ichLim;
			*pfAssocPrev = true;
		}
		else
		{
			*pich = m_ichMin;
			*pfAssocPrev = false;
		}
		return S_OK;
	}

	// check if the click position is after this segment
	if (ptdClickPosition.x >= srcRect.MapXTo(m_width - 1, dstRect))
	{
		if (IsRtl())
		{
			*pich = m_ichMin;
			*pfAssocPrev = false;
		}
		else
		{
			*pich = m_ichLim;
			*pfAssocPrev = true;
		}
		return S_OK;
	}

	// find the closest IP to the click position
	int minDistance = 0;
	int minIch = m_ichMin;
	vector<Cluster>::iterator it = m_clusters.end();
	do
	{
		if (it == m_clusters.end())
			it = m_clusters.begin();
		else
			it++;

		int x, ich;
		if (it == m_clusters.end())
		{
			x = (IsRtl() ? 0 : m_width) - 1;
			ich = m_ichLim;
		}
		else
		{
			x = it->beforeX - 1;
			ich = it->ichBase;
		}
		int distance = srcRect.MapXTo(x, dstRect) - ptdClickPosition.x;
		if (it == m_clusters.begin() || abs(distance) < abs(minDistance))
		{
			minIch = ich;
			minDistance = distance;
		}
		else
		{
			// we have passed the click position, so we don't need to compare any more IPs
			// they will all be farther away than the current closest IP
			break;
		}
	} while (it != m_clusters.end());

	*pich = minIch;
	if (IsRtl())
		*pfAssocPrev = minDistance < 0;
	else
		*pfAssocPrev = minDistance > 0;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Indicate what logical position an arrow key should move the IP to.
	Arguments:
		pfAssocPrev			primary associated with preceding character?
		fRight				direction of desired movement (physical)
		fMovingIn			to this segment; if so, initial pich meaningless
		pfResult			if false, try next segment or string (output)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::ArrowKeyPosition(int ichBase, IVwGraphics* pvg, int* pich,
	ComBool* pfAssocPrev, ComBool fRight, ComBool fMovingIn, ComBool* pfResult)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(pich);
	ChkComArgPtr(pfAssocPrev);
	ChkComOutPtr(pfResult);

	if (m_width < 0)
		Compute(ichBase, pvg);

	*pfResult = TRUE;
	if (fRight != IsRtl())
	{
		// Moving forward.
		if (!fMovingIn && *pich >= m_ichLim)
		{
			*pfResult = FALSE;
		}
		else
		{
			if (fMovingIn)
				*pich = m_ichMin;
			if (*pich >= m_ichLim)
			{
				// It's just possible to be moving in to an empty segment (e.g., an empty string
				// following a grey box in a list).
				*pfResult = FALSE;
			}
			else
			{
				vector<Cluster>::iterator it = upper_bound(m_clusters.begin(), m_clusters.end(), Cluster(*pich));
				*pich = it->ichBase;
				// Since we are moving forward, we want to associate the selection with the
				// previous character.
				*pfAssocPrev = true;
			}
		}
	}
	else
	{
		// Moving backward
		if (!fMovingIn && *pich <= m_ichMin)
		{
			*pfResult = FALSE;
		}
		else
		{
			if (fMovingIn)
				*pich = m_ichLim;
			vector<Cluster>::iterator it = lower_bound(m_clusters.begin(), m_clusters.end(), Cluster(*pich));
			it--;
			*pich = it->ichBase;
			// Since we are moving backward, we want to associate the selection with the
			// following character.
			*pfAssocPrev = false;
		}
	}

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Indicate what logical position a shift-arrow-key combination should move the
	end of the selection to.
	Arguments:
		ichAnchor			needed? What if moving in?
		fRight				direction of desired movement
		fMovingIn			to this segment? If so, initial pich meaningless
		pfRet				if false if false, try next seg or string
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::ExtendSelectionPosition(int ichBase, IVwGraphics* pvg,
	int* pich, ComBool fAssocPrevMatch, ComBool fAssocPrevNeeded, int ichAnchor,
	ComBool fRight, ComBool fMovingIn, ComBool* pfRet)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(pich);
	ChkComOutPtr(pfRet);

	ComBool assocPrev;
	CheckHr(ArrowKeyPosition(ichBase, pvg, pich, &assocPrev, fRight, fMovingIn, pfRet));

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Used to find where underlines should be drawn.
	As usual, if cxdMax is zero, it just answers the number of slots needed
	to return the information.
	Arguments:
		rcSrc				 as for DrawText
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteSegment::GetCharPlacement(int ichBase, IVwGraphics* pvg, int ichMin,
	int ichLim, RECT rcSrc, RECT rcDst, ComBool fSkipSpace, int cxdMax, int* pcxd,
	int* prgxdLefts, int* prgxdRights, int* prgydUnderTops)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArrayArg(pcxd, cxdMax);
	ChkComArrayArg(prgxdLefts, cxdMax);
	ChkComArrayArg(prgxdRights, cxdMax);
	ChkComArrayArg(prgydUnderTops, cxdMax);

	Rect srcRect(rcSrc);
	Rect dstRect(rcDst);

	Assert(srcRect.Width() == dstRect.Width());
	Assert(srcRect.Height() == dstRect.Height());

	if (m_width < 0)
		Compute(ichBase, pvg);

	// If the indices cover a larger range than the segment,
	// adjust them
	if (ichMin < m_ichMin)
		ichMin = m_ichMin;
	if (ichLim > m_ichLim)
		ichLim = m_ichLim;
	// If that leaves nothing to draw, do nothing
	if (ichMin >= ichLim)
	{
		*pcxd = 0;
		return S_OK;
	}

	*pcxd = 1;
	if (cxdMax > 0)
	{
		int left, right;
		if (IsRtl())
		{
			left = GetSelectionX(ichLim, srcRect, dstRect);
			right = GetSelectionX(ichMin, srcRect, dstRect);
		}
		else
		{
			left = GetSelectionX(ichMin, srcRect, dstRect);
			right = GetSelectionX(ichLim, srcRect, dstRect);
		}
		prgxdLefts[0] = left;
		prgxdRights[0] = right;

		LgCharRenderProps chrp;
		int ichMinDum, ichLimDum; // for GetCharProps to return
		CheckHr(m_qts->GetCharProps(m_ichMin, &chrp, &ichMinDum, &ichLimDum));
		InterpretChrp(chrp);

		int top = srcRect.MapYTo(0, dstRect) - MulDiv(chrp.dympOffset, dstRect.Height(), kdzmpInch);
		prgydUnderTops[0] = top + m_fontAscent + 1;
	}

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

//:>********************************************************************************************
//:>	   Other public methods
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Protected Methods
//:>********************************************************************************************

void GraphiteSegment::Compute(int ichBase, IVwGraphics* pvg)
{
	m_ichMin = ichBase;

	LgCharRenderProps chrp;
	int ichMinDum, ichLim; // for GetCharProps to return
	CheckHr(m_qts->GetCharProps(m_ichMin, &chrp, &ichMinDum, &ichLim));
	InterpretChrp(chrp);
	CheckHr(pvg->SetupGraphics(&chrp));

	int dpiY;
	CheckHr(pvg->get_YUnitsPerInch(&dpiY));

	gr_font* font = gr_make_font((float) MulDiv(chrp.dympHeight, dpiY, kdzmpInch), m_qgre->Face());

	int segmentLen = m_ichLim - m_ichMin;
	StrUni segStr;
	OLECHAR* pchNfd;
	segStr.SetSize(segmentLen + 1, &pchNfd);
	CheckHr(m_qts->Fetch(m_ichMin, m_ichLim, pchNfd));
	pchNfd[segmentLen] = '\0';

	gr_segment* segment = gr_make_seg(font, m_qgre->Face(), 0, m_qgre->FeatureValues(), gr_utf16, segStr, segmentLen, m_paraRtl ? gr_rtl : 0);
	if (m_stretch > 0)
	{
		int width = 0;
		for (const gr_slot* s = gr_seg_first_slot(segment); s != NULL; s = gr_slot_next_in_segment(s))
			width += Round(gr_slot_advance_X(s, m_qgre->Face(), font));
		gr_seg_justify(segment, gr_seg_first_slot(segment), font, width + m_stretch, gr_justCompleteLine, NULL, NULL);
	}
	InitializeGlyphs(segment, font);

	int fontAscent, fontDescent;
	CheckHr(pvg->get_FontAscent(&fontAscent));
	CheckHr(pvg->get_FontDescent(&fontDescent));
	m_fontAscent = fontAscent;
	m_fontDescent = fontDescent;

	gr_seg_destroy(segment);
	gr_font_destroy(font);
}

/*----------------------------------------------------------------------------------------------
	Retrieves glyph positioning information from the Graphite2 segment and identifies clusters.
	The clustering algorithm used below is adapted from the algorithm in the Graphite2 manual.
----------------------------------------------------------------------------------------------*/
void GraphiteSegment::InitializeGlyphs(gr_segment* segment, gr_font* font)
{
	m_glyphs.clear();
	m_clusters.clear();
	m_width = 0;
	if (m_ichMin == m_ichLim)
		return;

	// Graphite2 slots are returned in logical order. We want them in visual order.
	const gr_slot* s = m_paraRtl ? gr_seg_last_slot(segment) : gr_seg_first_slot(segment);
	while (s != NULL)
	{
		GlyphInfo g;
		g.glyphIndex = gr_slot_gid(s);
		g.x = m_width;
		g.y = Round(gr_slot_origin_Y(s));
		m_width += Round(gr_slot_advance_X(s, m_qgre->Face(), font));
		m_glyphs.push_back(g);
		s = m_paraRtl ? gr_slot_prev_in_segment(s) : gr_slot_next_in_segment(s);
	}

	unsigned int gi;
	int beforeX;
	if (m_paraRtl)
	{
		gi = m_glyphs.size() - 1;
		beforeX = m_width;
	}
	else
	{
		gi = 0;
		beforeX = 0;
	}
	m_clusters.push_back(Cluster(m_ichMin, 0, gi, 0, beforeX));
	for (s = gr_seg_first_slot(segment); s != NULL; s = gr_slot_next_in_segment(s))
	{
		int before = m_ichMin + gr_cinfo_base(gr_seg_cinfo(segment, gr_slot_before(s)));
		int after = m_ichMin + gr_cinfo_base(gr_seg_cinfo(segment, gr_slot_after(s)));

		while (m_clusters.size() > 1 && m_clusters.back().ichBase > before)
		{
			Cluster& last = m_clusters.back();
			Cluster& newLast = m_clusters[m_clusters.size() - 2];
			newLast.length += last.length;
			newLast.glyphCount += last.glyphCount;
			m_clusters.pop_back();
		}

		if (gr_slot_can_insert_before(s) && m_clusters.back().length > 0
			&& before >= m_clusters.back().ichBase + m_clusters.back().length)
		{
			Cluster& last = m_clusters.back();
			int ichBase = last.ichBase + last.length;
			int x;
			if (!m_paraRtl)
			{
				x = m_glyphs[gi].x;
			}
			else if (gi < m_glyphs.size() - 1)
			{
				x = m_glyphs[gi + 1].x;
			}
			else
			{
				x = m_width;
			}
			m_clusters.push_back(Cluster(ichBase, before - ichBase, gi, 0, x));
		}
		m_clusters.back().glyphCount++;

		if (m_clusters.back().ichBase + m_clusters.back().length < after + 1)
			m_clusters.back().length = after + 1 - m_clusters.back().ichBase;

		if (m_paraRtl)
			gi--;
		else
			gi++;
	}
}

void GraphiteSegment::InterpretChrp(LgCharRenderProps& chrp)
{
	ILgWritingSystemPtr qLgWritingSystem;
	ILgWritingSystemFactoryPtr qLgWritingSystemFactory;
	CheckHr(m_qgre->get_WritingSystemFactory(&qLgWritingSystemFactory));
	if (qLgWritingSystemFactory)
	{
		CheckHr(qLgWritingSystemFactory->get_EngineOrNull(chrp.ws, &qLgWritingSystem));
		if (!qLgWritingSystem)
			ThrowHr(WarnHr(E_UNEXPECTED));
		CheckHr(qLgWritingSystem->InterpretChrp(&chrp));
	}
}

// Check whether the character at ich is an object replacement character.
bool GraphiteSegment::CheckForOrcUs(int ich)
{
	OLECHAR ch;
	CheckHr(m_qts->Fetch(ich, ich + 1, &ch));
	return ch == 0xfffc;
}

/*----------------------------------------------------------------------------------------------
	Answer true if the IP should be drawn by this segment.
----------------------------------------------------------------------------------------------*/
bool GraphiteSegment::CanDrawIP(int ich, ComBool fAssocPrev)
{
	// out of range is quite valid, just means IP is really in another segment.
	if (ich < m_ichMin || ich > m_ichLim)
		return false;
	// Draw at start of segment only if associated with following char
	// (or if seg empty) (or at very start of paragraph--in case someone
	// set fAssocPrev inappropriately, don't want IP to disappear)
	if (ich > 0 && ich == m_ichMin && fAssocPrev && m_ichMin != m_ichLim)
	{
		// Also, the previous character must not be an object one. If it is,
		// there is no previous segment to draw the IP.
		if (!CheckForOrcUs(ich - 1))
			return false;
	}
	// Draw at end of segment only if associated with preceding char
	// (or if segment empty) (or if at very end of paragraph--in case
	// fAssocPrev set inappropriately)
	if (ich == m_ichLim && !fAssocPrev && m_ichMin != m_ichLim)
	{
		// if there is anything following in the para, let that draw it.
		int cchPara;
		CheckHr(m_qts->get_Length(&cchPara));
		if (cchPara > ich && !CheckForOrcUs(ich))
			return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Returns the x coordinate at the beginning cluster that contains the specified offset.
----------------------------------------------------------------------------------------------*/
int GraphiteSegment::GetSelectionX(int ich, const Rect& srcRect, const Rect& dstRect)
{
	vector<Cluster>::iterator it = lower_bound(m_clusters.begin(), m_clusters.end(), Cluster(ich));

	if (it == m_clusters.end())
		return srcRect.MapXTo(IsRtl() ? 0 : m_width, dstRect);

	return srcRect.MapXTo(it->beforeX, dstRect);
}
