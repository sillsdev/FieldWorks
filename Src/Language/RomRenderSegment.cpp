/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: RomRenderSegment.cpp
Responsibility: John Thomson
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
static DummyFactory g_fact(_T("SIL.Language1.RomRenderSeg"));

// Added from UniscribeSegment.cpp
#define kchwHardLineBreak (wchar)0x2028

//:>********************************************************************************************
//:>	   Methods
//:>********************************************************************************************

RomRenderSegment::RomRenderSegment()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

RomRenderSegment::RomRenderSegment(IVwTextSource * pts, RomRenderEngine * prre, int dichLim,
	LgLineBreak lbrkStart, LgLineBreak lbrkEnd,
	ComBool fEndLine)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_qts = pts;
	m_qrre = prre;
	m_dichLim = dichLim;
	m_lbrkStart = lbrkStart;
	m_lbrkEnd = lbrkEnd;
	m_dxsStretch = 0;
	m_dxsWidth = -1; // so we know it is not initialized
	m_dxsTotalWidth = -1;
	m_fEndLine = (bool)fEndLine;
	m_fReversed = false;
	// To properly init m_dysAscent and m_dxsWidth, ComputeDimensions() must be called.
}

RomRenderSegment::~RomRenderSegment()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP RomRenderSegment::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_ILgSegment)
		*ppv = static_cast<ILgSegment *>(this);
	else if (&riid == &CLSID_RomRenderSegment)			// trick one for engine to get an impl
		*ppv = static_cast<RomRenderSegment *>(this);
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
	Functor for DoAllRuns for drawing segment
----------------------------------------------------------------------------------------------*/
class DrawBinderRoman
{
	int m_ydTop;
	int m_dydAscent;

public:
	int m_xdRight;

	DrawBinderRoman(int ydTop, int dydAscent)
	{
		m_ydTop = ydTop;
		m_dydAscent = dydAscent;
	}
	bool operator() (IVwGraphics* pvg, const OLECHAR *prgch, int cch, bool fLast,
		int xd, int dxdStretch, Rect rcSrc, Rect rcDst, LgCharRenderProps * pchrp)
	{
		int dydAscent;

		CheckHr(pvg->get_FontAscent(&dydAscent));
		CheckHr(pvg->DrawText(xd,
			m_ydTop + m_dydAscent - dydAscent -
				MulDiv(pchrp->dympOffset, rcDst.Height(), kdzmpInch),
			cch, prgch, dxdStretch));
		if (fLast)
		{
			int dxdWidth, dydHeight;
			CheckHr(pvg->GetTextExtent(cch, prgch, &dxdWidth, &dydHeight));
			m_xdRight = xd + dxdWidth;
		}
		return true;
	}
};


/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::DrawText(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int * pdxdWidth)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdxdWidth);

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	int dydAscent;
	if (m_dxsWidth == -1)
	{
		ComputeDimensions(ichBase, pvg, rcSrc, rcDst, false);
		dydAscent = m_dysAscent;
	}
	else
		dydAscent = ScaleInt(m_dysAscent, rcDst.Height(), rcSrc.Height());

	if (m_fReversed && m_fEndLine)
	{
		// Right-to-left trailing white-space: draw position is assumed to be just to the
		// left of the visible stuff, so scoot invisible stuff to the left.
		Assert(m_dxsTotalWidth != -1);
		rcSrc.left += (m_dxsTotalWidth - m_dxsWidth);
		rcSrc.right += (m_dxsTotalWidth - m_dxsWidth);
	}
	DrawBinderRoman db(rcSrc.MapYTo(0, rcDst), dydAscent);
	DoAllRuns(ichBase, pvg, rcSrc, rcDst, db);
	if (m_fReversed)
	{
		// Return visible width, not total width.
		*pdxdWidth = ScaleInt(m_dxsWidth, rcSrc.Width(), rcDst.Width());
	}
	else
		*pdxdWidth = db.m_xdRight - rcSrc.MapXTo(0, rcDst);

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

STDMETHODIMP RomRenderSegment::DrawTextNoBackground(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int * pdxdWidth)
{
#if WIN32
	return E_NOTIMPL;
#else
	// TODO-Linux: This is a really dumb implementation; we should not be using the Roman Renderer.
	BEGIN_COM_METHOD
	pvg->put_BackColor(kclrTransparent);
	DrawText(ichBase, pvg, rcSrc1, rcDst1, pdxdWidth);
	END_COM_METHOD(g_fact, IID_ILgSegment);
#endif
}

/*----------------------------------------------------------------------------------------------
	Tell the segment its measurements are invalid, because we are going to start asking
	questions using a different VwGraphics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::Recompute(int ichBase, IVwGraphics * pvg)
{
	m_dxsWidth = -1;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	The distance the drawing point should advance after drawing this segment.
	Always positive, even for RtoL segments. This includes any stretch that has
	been requested.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_Width(int ichBase, IVwGraphics * pvg, int * pdxs)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdxs);
	if (m_dxsWidth == -1)
	{
		int dxInch, dyInch;
		CheckHr(pvg->get_XUnitsPerInch(&dxInch));
		CheckHr(pvg->get_YUnitsPerInch(&dyInch));
		Rect rcSrc(0, 0, dxInch, dyInch);
		ComputeDimensions(ichBase, pvg, rcSrc, rcSrc);
	}
	*pdxs = m_dxsWidth;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Distance that drawing takes place to the right of the rectangle specified by the width
	and drawing origin.
	It is legitimate not to implement this; calling code will typically treat it as zero.
	However, the not-implemented could be used to make sure we allow a few pixels.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_RightOverhang(int ichBase, IVwGraphics * pvg, int * px)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Overhang to the left of the drawing origin
	It is legitimate not to implement this; calling code will typically treat it as zero.
	However, the not-implemented could be used to make sure we allow a few pixels.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_LeftOverhang(int ichBase, IVwGraphics * pvg, int * px)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Height of the drawing rectangle. This is normally the full font height, even if all chars
	in segment are lower case or none have descenders, or even if segment is empty.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_Height(int ichBase, IVwGraphics * pvg, int * pdys)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdys);
	if (m_dxsWidth == -1)
	{
		int dxInch, dyInch;
		CheckHr(pvg->get_XUnitsPerInch(&dxInch));
		CheckHr(pvg->get_YUnitsPerInch(&dyInch));
		Rect rcSrc(0, 0, dxInch, dyInch);
		ComputeDimensions(ichBase, pvg, rcSrc, rcSrc);
	}
	*pdys = m_dysHeight;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	The ascent of the font typically used to draw this segment. Basically, it determines the
	baseline used to align it.

	To do: we may need to make more than one baseline explicit in the interface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_Ascent(int ichBase, IVwGraphics * pvg, int * pdys)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdys);
	if (m_dxsWidth == -1)
	{
		int dxInch, dyInch;
		CheckHr(pvg->get_XUnitsPerInch(&dxInch));
		CheckHr(pvg->get_YUnitsPerInch(&dyInch));
		Rect rcSrc(0, 0, dxInch, dyInch);
		ComputeDimensions(ichBase, pvg, rcSrc, rcSrc);
	}
	*pdys = m_dysAscent;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Obtains height and width in a single call.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::Extent(int ichBase, IVwGraphics * pvg, int * pdxs, int * pdys)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdxs);
	ChkComOutPtr(pdys);
	if (m_dxsWidth == -1)
	{
		int dxInch, dyInch;
		CheckHr(pvg->get_XUnitsPerInch(&dxInch));
		CheckHr(pvg->get_YUnitsPerInch(&dyInch));
		Rect rcSrc(0, 0, dxInch, dyInch);
		ComputeDimensions(ichBase, pvg, rcSrc, rcSrc);
	}
	*pdxs = m_dxsWidth;
	*pdys = m_dysHeight;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Compute the rectangle in destination coords which contains all the pixels
	drawn by this segment. This should be a sufficient rectangle to invalidate
	if this segment is about to be discarded and replaced by another.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::BoundingRect(int ichBase, IVwGraphics * pvg, RECT rcSrc1,
	RECT rcDst1, RECT * prcBounds)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(prcBounds);

	int dxsWidth = m_dxsWidth;
	int dysHeight = m_dysHeight;
	int dysAscent = m_dysAscent;
	m_dxsWidth = -1; // force recompute
	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	ComputeDimensions(ichBase, pvg, rcSrc, rcDst);
	int dxdWidth = m_dxsWidth;
	int dydHeight = m_dysHeight;
	m_dxsWidth = dxsWidth;
	m_dysHeight = dysHeight;
	m_dysAscent = dysAscent;

	// Compute origin, fill in *prcBounds
	int xdLeft = rcSrc.MapXTo(0, rcDst);
	int ydTop = rcSrc.MapYTo(0, rcDst);
	// Broaden it by the height. This should catch any reasonable overlap.
	// ENHANCE JohnT: if we figure out a proper implementation of the overhang methods,
	// use them.
	int dxdHeight = ScaleInt(dydHeight, rcDst.Width(), rcDst.Height());
	xdLeft -= dxdHeight;
	dxdWidth += 2* dxdHeight;
	prcBounds->left = xdLeft;
	prcBounds->right = xdLeft + dxdWidth;
	prcBounds->top = ydTop;
	prcBounds->bottom = ydTop + dydHeight;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Distance that drawing takes place above the rectangle specified by the ascent
	and drawing origin.
	It is legitimate not to implement this; calling code will typically treat it as zero.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_AscentOverhang(int ichBase, IVwGraphics * pvg, int * py)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Distance that drawing takes place below the rectangle specified by the descent
	and drawing origin.
	It is legitimate not to implement this; calling code will typically treat it as zero.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_DescentOverhang(int ichBase, IVwGraphics * pvg, int * py)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Functor for DoAllRuns for computing actual drawn width of segment
----------------------------------------------------------------------------------------------*/
class WidthBinderRoman
{
public:
	int m_xdRight;

	WidthBinderRoman()
	{
	}
	bool operator() (IVwGraphics* pvg, const OLECHAR *prgch, int cch, bool fLast,
		int xd, int dxdStretch, Rect rcSrc, Rect rcDst, LgCharRenderProps * pchrp)
	{
		if (fLast)
		{
			int dxdWidth, dydHeight;
			CheckHr(pvg->GetTextExtent(cch, prgch, &dxdWidth, &dydHeight));
			m_xdRight = xd + dxdWidth;
		}
		return true;
	}
};

/*----------------------------------------------------------------------------------------------
	Compute the width the segment would occupy if drawn with the specified
	parameters. Don't update cached width.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::GetActualWidth(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int * pdxdWidth)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdxdWidth);
	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	WidthBinderRoman wbFunc;
	DoAllRuns(ichBase, pvg, rcSrc, rcDst, wbFunc);
	if (m_fReversed)
	{
		// Return visible width, not total width. Note that text (ie, space) is assumed
		// to be to the left of the position of the segment.
		*pdxdWidth = ScaleInt(m_dxsWidth, rcSrc.Width(), rcDst.Width());
	}
	else
		*pdxdWidth = wbFunc.m_xdRight - rcSrc.MapXTo(0, rcDst);
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Answer whether the segment is right to left. RRS normally isn't, unless it is trailing
	white-space in a right-to-left paragraph (eg, Roman text embedded in Arabic or Hebrew).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_RightToLeft(int ichBase,
	ComBool * pfResult)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfResult);
	*pfResult = (m_nDirDepth % 2);
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Answer how deeply embedded this segment is in relative to the paragraph direction--
	how many changes of direction have to happen to get this in the right direction.
	Also indicate if this has weak directionality, eg, a white-space-only segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_DirectionDepth(int ichBase, int * pnDepth, ComBool * pfWeak)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pnDepth);
	ChkComOutPtr(pfWeak);
//	LgCharRenderProps chrp;
//	int ichMinRun, ichLimRun;
//	CheckHr(m_qts->GetCharProps(ichBase, &chrp, &ichMinRun, &ichLimRun));
//	*pnDepth = chrp.nDirDepth;

	*pnDepth = m_nDirDepth;
	*pfWeak = m_fWsOnly;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Set the direction of the segment.
	ENHANCE SharonC (JohnT): explain the logic here, especially the %2s and the m_fWsOnly

	TODO 1441 (SharonC): Ideally, the characters should be reversed for Roman trailing
	white-space-only segments embedded in Arabic or Hebrew.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::SetDirectionDepth(int ichBase, int nNewDepth)
{
	BEGIN_COM_METHOD
	if (nNewDepth == m_nDirDepth)
		return S_OK;
	else if ((nNewDepth % 2) == (m_nDirDepth % 2))
	{
		m_nDirDepth = nNewDepth;
		return S_OK;
	}
	else if (!m_fWsOnly)
		// ENHANCE SharonC (JohnT): Add an argument briefly explaining what is wrong
		ThrowInternalError(E_INVALIDARG);
	else
	{
		m_nDirDepth = nNewDepth;
		m_fReversed = (bool)(m_nDirDepth % 2);
		return S_OK; // E_NOTIMPL;
	}
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the cookies which identify the old writing system of the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_WritingSystem(int ichBase, int * pws)
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
STDMETHODIMP RomRenderSegment::get_Lim(int ichBase, int * pdich)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pdich);
	*pdich = m_dichLim;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the last character of interest to the segment. RRS never renders any characters
	beyond its logical end.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_LimInterest(int ichBase, int * pdich)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pdich);
	*pdich = m_dichLim;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Changes the end-of-line status of the segment. This is used after making the last segment
	of a string if we want to attempt to put some more text after it (e.g., in another
	old writing system).
	RomRenderSegment uses this information only to decide whether to include trailing
	white space in the segment width.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::put_EndLine(int ichBase, IVwGraphics* pvg, ComBool fNewVal)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	if (m_fEndLine != fNewVal)
	{
		m_fEndLine = (bool)fNewVal;
		m_dxsWidth = -1;
		m_dxsTotalWidth = -1;
	}
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Changes the start-of-line status of the segment. This is used after making the last segment
	of a string if we want to attempt to put some more text after it (e.g., in another
	old writing system).
	RomRenderSegment ignores this; it does not handle start-line contextuals.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::put_StartLine(int ichBase, IVwGraphics * pvg, ComBool fNewVal)
{
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Get the type of break that occurs at the logical start of the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_StartBreakWeight(int ichBase, IVwGraphics * pvg,
	LgLineBreak * plbrk)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(plbrk);
	*plbrk = m_lbrkStart;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the type of break that occurs at the logical end of the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_EndBreakWeight(int ichBase, IVwGraphics * pvg,
	LgLineBreak * plbrk)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(plbrk);
	*plbrk = m_lbrkEnd;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Read the amount of stretch that has been set for the segment. This is not included in
	the result returned from width, but the segment gets this much wider when drawn.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::get_Stretch(int ichBase, int * pdxs)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pdxs);
	*pdxs = m_dxsStretch;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Set the amount of stretch that has been set for the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::put_Stretch(int ichBase, int dxs)
{
	BEGIN_COM_METHOD
	m_dxsStretch = dxs;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	// Test whether IP at specified position is valid.
	// If position is not in this segment, and there are following segments, may
	// answer kipvrUnknown; client should then try subsequent segments.
	// Note: This routine gives the finest possible granularity (where IPs can be
	// rendered). Particular input methods may not support all the positions that
	// the renderer says are valid. Compare ILgInputMethodEditor>>IsValidInsertionPoint.
	// Clients should generally check both.
	// Upgraded this method to more closly align with UniscribeSegment Implementation. 10 March 2010.
	// The only difference being LgIcuCharPropEngine is created each time rather then
	// getting it from the Uniscribe Render.
	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::IsValidInsertionPoint(int ichBase, IVwGraphics * pvg, int ich,
	LgIpValidResult * pipvr)
{
BEGIN_COM_METHOD
	// Allow this argument to be null, as we don't actually use it, and some local methods
	// do in fact pass null.
	ChkComArgPtrN(pvg);
	ChkComArgPtr(pipvr);
	*pipvr = kipvrOK;
	int cch;
	CheckHr(m_qts->get_Length(&cch));
	if (ich < ichBase || ich >= ichBase + m_dichLim)
	{
		// out of range is quite valid, just means IP is really in another segment.
		// But, if this is an empty segment, we'd better count it valid: it's the only position.
		// In fact, we need to ensure that the last position in any paragraph is always valid.
		// This test catches the empty and end of paragraph cases.
		if (ich == cch)
		{
			return S_OK; // end of paragraph (or empty paragraph) is valid.
		}
		// If this is BiDi text and this segment is going the opposite direction from the
		// overall text direction, we consider the point at the end of this segment as a
		// valid place for the IP.
		else if (ich != ichBase + m_dichLim || m_nDirDepth % 2 == 0)
		{
			// If we're pointing at a hard line break character (Unicode 2028), then we know
			// that it's a good insertion point in spite of missing the other conditions.
			OLECHAR ch;
			m_qts->Fetch(ich, ich + 1, &ch);
			if (ch != kchwHardLineBreak)
				*pipvr = kipvrUnknown; // Definitely in a different segment of the paragraph.
			return S_OK;
		}
	}
	if (ich < ichBase + cch)
	{
		// Handle surrogate pairs properly.
		OLECHAR rgch[2] = {0, 0};
		CheckHr(m_qts->Fetch(ich, ich + 2, rgch));
		if (IsLowSurrogate(rgch[0]))
		{
			*pipvr = kipvrBad;
			return S_OK;
		}
		else
		{
			// Make sure we have a LgCharPropEngine that we can use
			if (!m_qcpe)
			{
				// We can't get a WritingSystemFactory so just create a
				// LgIcuCharPropEngine to use
				m_qcpe.CreateInstance(CLSID_LgIcuCharPropEngine);
			}

			// Handle (diacritic) marks properly.
			UChar32 uch32;
			LgGeneralCharCategory gcc;
			bool fSurrogate = FromSurrogate(rgch[0], rgch[1], (uint *)&uch32);
			if (!fSurrogate)
				uch32 = (unsigned)rgch[0];
			CheckHr(m_qcpe->get_GeneralCategory(uch32, &gcc));
			if (gcc >= kccMn && gcc <= kccMe)
			{
				*pipvr = kipvrBad;
				return S_OK;
			}
		}
	}
	// A valid position should not be in the middle of something that will compose in NFC.
	// For example, if characters at index 3, 4, and 5 compose to a single character,
	// 3 is a valid position (before the composite), and so is 6 (after it),
	// but 4 and 5 are not.
	// One way to detect this is that (in the absence of other compositions),
	// chars the NFC composition of characters 0..3 has 3 characters, 0...4 is 4 characters, and
	// 0...5 is also 4, so is 0...6, while 0...7 has 5. In other words, invalid positions are
	// those that have the same NFC offset as one more character.
	if (ich > ichBase &&
		ich < ichBase + m_dichLim &&
		OffsetInNfc(ich, ichBase, m_qts) == OffsetInNfc(ich +1, ichBase, m_qts))
	{
		*pipvr = kipvrBad;
	}
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

// This method Added from UniscribeSegment.cpp
// ich is an offset into the whole paragraph; ichBase is the start of this segment, both in
// original (typically NFD) characters. Compute the offset of ich from the start of the
// segment that corresponds to ich (in NFC, if using NFC).
int RomRenderSegment::OffsetInNfc(int ich, int ichBase, IVwTextSource * pts)
{
	Assert(ich >= ichBase);
#ifdef UNISCRIBE_NFC
	if (ich == ichBase)
		return 0;
	StrUni stuBuf;
	OLECHAR * pch;
	stuBuf.SetSize(ich - ichBase, &pch);
	CheckHr(pts->Fetch(ichBase, ich, pch));
	// This can return an error code, but it is very unlikely.
	StrUtil::NormalizeStrUni(stuBuf, UNORM_NFC);
	return stuBuf.Length();
#else
	return ich - ichBase;
#endif
}

/*----------------------------------------------------------------------------------------------

	Arguments:
		fBoundaryEnd		 asking about the logical end boundary?
		fBoundaryRight		 asking about the physical right boundary?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::DoBoundariesCoincide(int ichBase, IVwGraphics * pvg,
	ComBool fBoundaryEnd, ComBool fBoundaryRight, ComBool * pfResult)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pfResult);
	*pfResult = TRUE;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	DoAllRuns binder for drawing IP and figuring out where it goes.
----------------------------------------------------------------------------------------------*/
class DrawIPBinderRoman
{
	int m_dydAscent;
	int m_ich;
	int m_cch; // count of chars in previous runs
	int m_xdLeft;
	int m_ydTop;
	int m_xdRight;
	int m_ydBottom;

public:
	DrawIPBinderRoman(int dydAscent, int ich, int ichMin)
	{
		m_dydAscent = dydAscent;
		m_ich = ich;
		m_cch = ichMin; // characters before first run in string
	}
	bool operator() (IVwGraphics* pvg, const OLECHAR *prgch, int cch, bool fLast,
		int xd, int dxdStretch, Rect rcSrc, Rect rcDst, LgCharRenderProps * pchrp)
	{
		m_cch += cch; // now count of chars including this segment
		if (m_cch < m_ich)
			return true;  // subsequent run will actually draw

		// Chars in this run before the IP: chars in run, minus difference
		// between ip and end of run
		int cchRunToIP = cch - (m_cch - m_ich);

		int dxdRunToIP;
		CheckHr(pvg->GetTextLeadWidth(cch, prgch, cchRunToIP, dxdStretch,
			&dxdRunToIP));
		int dydAscent;
		CheckHr(pvg->get_FontAscent(&dydAscent));
		int dxdWidth;
		int dydHeight;
		CheckHr(pvg->GetTextExtent(0, prgch, &dxdWidth, &dydHeight));
		// make width two pixels.
		// ENHANCE JohnT: if we have really high-res screens one day we may need more.
		m_xdLeft = xd + dxdRunToIP - 1;
		m_ydTop = rcSrc.MapYTo(0, rcDst) + m_dydAscent - dydAscent -
			MulDiv(pchrp->dympOffset, rcDst.Height(), kdzmpInch);
		m_xdRight = m_xdLeft + 2;
		m_ydBottom = m_ydTop + dydHeight;
		return false; // stop loop, we computed it successfully.
	}
	void GetResults(int * pxdLeft, int * pydTop, int * pxdRight, int * pydBottom)
	{
		*pxdLeft = m_xdLeft;
		*pydTop = m_ydTop;
		*pxdRight = m_xdRight;
		*pydBottom = m_ydBottom;
	}
};

// Check whether the character at ich is an object replacement character
bool CheckForORC(IVwTextSource * pts, int ich)
{
	OLECHAR ch;
	CheckHr(pts->Fetch(ich, ich + 1, &ch));
	return ch == 0xfffc;
}
/*----------------------------------------------------------------------------------------------
	Draw an insertion point at an appropriate position.
	Arguments:
		rcSrc				 as for DrawText
		ich					 must be valid
		fAssocPrev			 primary associated with preceding character?
		fOn					 turning on or off? Caller should alternate, on first.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::DrawInsertionPoint(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int ich, ComBool fAssocPrev, ComBool fOn, LgIPDrawMode dm)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	// out of range is quite valid, just means IP is really in another segment.
	if (ich < ichBase || ich > ichBase + m_dichLim)
		return S_OK;
	// Draw at start of segment only if associated with following char
	// (or if seg empty) (or at very start of paragraph--in case someone
	// set fAssocPrev inappropriately, don't want IP to disappear)
	if (ich && ich == ichBase && fAssocPrev && m_dichLim)
	{
		// Also, the previous character must not be an object one. If it is,
		// there is no previous segment to draw the IP.
		if (!CheckForORC(m_qts, ich - 1))
			return S_OK;
	}
	// Draw at end of segment only if associated with preceding char
	// (or if segment empty) (or if at very end of paragraph--in case
	// fAssocPrev set inappropriately)
	if (ich == ichBase + m_dichLim && (!fAssocPrev) && m_dichLim)
	{
		// if there is anything following in the para, let that draw it.
		int cchPara;
		CheckHr(m_qts->get_Length(&cchPara));
		if (cchPara > ich && !CheckForORC(m_qts, ich))
			return S_OK;
	}
	int dydAscent;
	if (m_dxsWidth == -1)
	{
		ComputeDimensions(ichBase, pvg, rcSrc, rcDst, false);
		dydAscent = m_dysAscent;
	}
	else
		dydAscent = ScaleInt(m_dysAscent, rcDst.Height(), rcSrc.Height());

	if (m_fReversed && m_fEndLine)
	{
		// Right-to-left trailing white-space: draw position is assumed to be just to the
		// left of the visible stuff, so scoot invisible stuff to the left.
		Assert(m_dxsTotalWidth != -1);
		rcSrc.left += (m_dxsTotalWidth - m_dxsWidth);
		rcSrc.right += (m_dxsTotalWidth - m_dxsWidth);
	}
	DrawIPBinderRoman dipb(dydAscent, ich, ichBase);
	DoAllRuns(ichBase, pvg, rcSrc, rcDst, dipb);
	int xdLeft;
	int ydTop;
	int xdRight;
	int ydBottom;
	dipb.GetResults(&xdLeft, &ydTop, &xdRight, &ydBottom);
	CheckHr(pvg->InvertRect(xdLeft, ydTop, xdRight, ydBottom));

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
STDMETHODIMP RomRenderSegment::PositionsOfIP(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int ich, ComBool fAssocPrev, LgIPDrawMode dm,
	RECT * prectPrimary, RECT * prectSecondary,
	ComBool * pfPrimaryHere, ComBool * pfSecHere)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(prectPrimary);
	ChkComArgPtr(prectSecondary);
	ChkComOutPtr(pfPrimaryHere);
	ChkComOutPtr(pfSecHere);

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	*pfSecHere = false; // Roman renderer never draws secondary
	*pfPrimaryHere = false; // default
	// out of range is quite valid, just means IP is really in another segment.
	if (ich < ichBase || ich > ichBase + m_dichLim)
		return S_OK;
	// Draw at start of segment only if associated with following char (or if seg empty)
	// Don't draw at start of segment if associated with prev char (unless seg empty)
	if (ich == ichBase && fAssocPrev && m_dichLim)
		return S_OK;
	// Draw at end of segment only if associated with preceding char (or if seg empty)
	// Don't draw at end of segment if associated with following char (unless seg empty)
	if (ich == ichBase + m_dichLim && !fAssocPrev && m_dichLim)
		return S_OK;
	int dydAscent;
	if (m_dxsWidth == -1)
	{
		ComputeDimensions(ichBase, pvg, rcSrc, rcDst, false);
		dydAscent = m_dysAscent;
	}
	else
		dydAscent = ScaleInt(m_dysAscent, rcDst.Height(), rcSrc.Height());

	if (m_fReversed && m_fEndLine)
	{
		// Right-to-left trailing white-space: draw position is assumed to be just to the
		// left of the visible stuff, so scoot invisible stuff to the left.
		Assert(m_dxsTotalWidth != -1);
		rcSrc.left += (m_dxsTotalWidth - m_dxsWidth);
		rcSrc.right += (m_dxsTotalWidth - m_dxsWidth);
	}
	DrawIPBinderRoman dipb(dydAscent, ich, ichBase);
	DoAllRuns(ichBase, pvg, rcSrc, rcDst, dipb);
	Rect rcd;
	int xdLeft, ydTop, xdRight, ydBottom;
	dipb.GetResults(&xdLeft, &ydTop,
		&xdRight, &ydBottom);
	rcd.left = xdLeft;
	rcd.top = ydTop;
	rcd.right = xdRight;
	rcd.bottom = ydBottom;
	*prectPrimary = rcd;
	*pfPrimaryHere = true;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	DoAllRuns functor; computes x and y limits of area to invert for given range.
----------------------------------------------------------------------------------------------*/
class DrawRangeBinderRoman {
	int m_ichMin;
	int m_ichLim;
	int m_cch; // count of chars in previous runs (and current)
	bool fGotMin;

	// results
	int m_xdLeft;
	int m_xdRight;

public:
	DrawRangeBinderRoman(int ichMin, int ichLim, int cchPrev)
	{
		m_ichMin = ichMin;
		m_ichLim = ichLim;
		m_cch = cchPrev;
		fGotMin = false;
	}
	bool operator() (IVwGraphics* pvg, const OLECHAR *prgch, int cch, bool fLast,
		int xd, int dxdStretch, Rect rcSrc, Rect rcDst, LgCharRenderProps * pchrp)
	{
		m_cch += cch; // now count of chars including this segment
		if (m_cch < m_ichMin)
			return true;  // not yet to start of range, keep going
		if (!fGotMin)
		{
			// Chars in this run before the IP: chars in run, minus difference
			// between ip and end of run
			int cchRunToMin = cch - (m_cch - m_ichMin);

			int dxdRunToMin;
			CheckHr(pvg->GetTextLeadWidth(cch, prgch, cchRunToMin, dxdStretch,
				&dxdRunToMin));
			m_xdLeft = xd + dxdRunToMin;
			fGotMin = true;
		}
		if (m_cch < m_ichLim)
			return true; // go on to later run for Lim
		// Chars in this run before the IP: chars in run, minus difference
		// between ip and end of run
		int cchRunToLim = cch - (m_cch - m_ichLim);

		int dxdRunToLim;
		CheckHr(pvg->GetTextLeadWidth(cch, prgch, cchRunToLim, dxdStretch,
			&dxdRunToLim));
		m_xdRight = xd + dxdRunToLim;
		return false; // stop loop, we got all we want.
	}
	void GetResults(int * pxdLeft, int * pxdRight)
	{
		Assert(fGotMin);
		*pxdLeft = m_xdLeft;
		*pxdRight = m_xdRight;
	}
};

/*----------------------------------------------------------------------------------------------
	Highlight a range of text.
	Arguments:
		rcSrc				 as for DrawText
		ichMin				 must be valid
		ydTop				 of area to highlight if whole line height;
		ydBottom			 includes half of inter-line spacing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::DrawRange(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int ichMin, int ichLim, int ydTop, int ydBottom, ComBool bOn,
	ComBool fIsLastLineOfSelection, RECT * prsBounds)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(prsBounds);

	ComBool fAnythingToDraw;
	CheckHr(PositionOfRange(ichBase, pvg, rcSrc1, rcDst1, ichMin, ichLim, ydTop, ydBottom,
		fIsLastLineOfSelection, prsBounds, &fAnythingToDraw));

	if (fAnythingToDraw)
	{
		CheckHr(pvg->InvertRect(prsBounds->left, prsBounds->top, prsBounds->right,
		prsBounds->bottom));
	}

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
STDMETHODIMP RomRenderSegment::PositionOfRange(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int ichMin, int ichLim, int ydTop, int ydBottom,
	ComBool fIsLastLineOfSelection, RECT * prsBounds, ComBool * pfAnythingToDraw)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(prsBounds);
	ChkComOutPtr(pfAnythingToDraw);

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	Rect rcBounds;
	*pfAnythingToDraw = false;
	if (ichMin < 0  || ichLim <= ichMin)
	{
		Warn("invalid or out-of-order indices");
		return S_OK;
	}
	// If the indices cover a larger range than the segment,
	// adjust them
	if (ichMin < ichBase)
		ichMin = ichBase;
	if (ichLim > ichBase + m_dichLim)
		ichLim = ichBase + m_dichLim;
	// If that leaves nothing to draw, do nothing
	if (ichMin >= ichLim)
		return S_OK;
	// OK, we will draw something.
	*pfAnythingToDraw = true;
	if (m_fReversed && m_fEndLine)
	{
		// Right-to-left trailing white-space: draw position is assumed to be just to the
		// left of the visible stuff, so scoot invisible stuff to the left.
		Assert(m_dxsTotalWidth != -1);
		rcSrc.left += (m_dxsTotalWidth - m_dxsWidth);
		rcSrc.right += (m_dxsTotalWidth - m_dxsWidth);
	}
	DrawRangeBinderRoman drb(ichMin, ichLim, ichBase);
	DoAllRuns(ichBase, pvg, rcSrc, rcDst, drb);
	int xdLeft, xdRight;
	drb.GetResults(&xdLeft, &xdRight);
	rcBounds.left = xdLeft;
	rcBounds.right = xdRight;
	rcBounds.top = ydTop;
	rcBounds.bottom = ydBottom;
	// Actually we may draw higher or lower than this due to the
	// int nTopTwips, int nBottomTwips arguments passed to DrawRange,
	// but this is the part that really needs to be visible, and
	// in any case this routine does not get those arguments.
	*prsBounds = rcBounds;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Convert a click position (in dst coords) to a character position.
	Arguments:
		pfBefore			 true if click was logically before indicated position
----------------------------------------------------------------------------------------------*/
class PointCharBinderRoman
{
	int m_xdClick;
	int m_ichLimRun; // start index of current run

	int m_dydAscent; // used to accumulate height in dest coords
	int m_dydDescent;
	int m_ich; // result
	int m_fBefore; // result
	bool m_fGotIt; // true when we find it.

public:
	PointCharBinderRoman(int xdClick, int ichBase)
	{
		m_ichLimRun = ichBase; // limit of previous run if any
		m_xdClick = xdClick;
		m_dydAscent = 0;
		m_dydDescent = 0;
		m_fGotIt = false;
	}
	bool operator() (IVwGraphics* pvg, const OLECHAR *prgch, int cch, bool fLast,
		int xd, int dxdStretch, Rect rcSrc, Rect rcDst, LgCharRenderProps * pchrp)
	{
		m_ichLimRun += cch; // includes chars of this run
		int dydAscent;

		CheckHr(pvg->get_FontAscent(&dydAscent));
		int dydThisAscent = dydAscent - MulDiv(pchrp->dympOffset, rcDst.Height(), kdzmpInch);
		if (dydThisAscent > m_dydAscent)
			m_dydAscent = dydThisAscent;
		int dydHeight;
		int dxdWidth;
		CheckHr(pvg->GetTextExtent(cch, prgch, &dxdWidth, &dydHeight));
		int dydThisDescent = dydHeight - dydThisAscent;
		if (dydThisDescent > m_dydDescent)
			m_dydDescent = dydThisDescent;
		if (m_fGotIt)
			return true; // continue looping, but only to get overall segment height.

		if(xd + dxdWidth + dxdStretch < m_xdClick)
		{
			if (fLast)
			{
				// The IP is right of this whole segment
				m_ich = m_ichLimRun;
				m_fBefore = true;
				return false;
			}
			// the IP is in a later run
			return true;
		}
		if (0 == cch)
		{
			// The only zero-length run is if the whole segment (and string) is empty.
			m_ich = m_ichLimRun;
			m_fBefore = false; // rather arbitrary
			return false;
		}

		//OK, it is in this run. We find it by approximation, then iteration.
		int dxdRun = m_xdClick - xd; // distance into this run
		if (dxdRun >= dxdWidth + dxdStretch)
		{
			// should only be possible that it is equal. We consider this to be
			// a click at the end of the last character of this run
			m_ich = m_ichLimRun;
			m_fBefore = true;
			m_fGotIt = true;
			return true; // continue the main loop, we need the segment height as well.
		}
		else if (dxdRun <= 0)
		{
			// Should not be possible, I think, but just in case put it at start of
			// segment. This is defensive programming, since negative dxdRun could
			// lead to trying to measure a negative character count below.
			m_ich = m_ichLimRun - cch;
			m_fBefore = true; // rather arbitrary
			m_fGotIt = true;
			return true;
		}
		// roughly it should be a char position proportional to the physical position.

		// ensure that ichRunLow is the index of a character such that the width
		// of all characters up to but not including it is <= dxdRun.
		int ichRunLow = MulDiv(cch, dxdRun, dxdWidth + dxdStretch) + 1;
		int dxdWidthLow = dxdRun + 1; // ensure one iteration unless ichRunMin == 0
		while (ichRunLow > 0 && dxdWidthLow > dxdRun)
		{
			ichRunLow--;
			CheckHr(pvg->GetTextLeadWidth(cch, prgch, ichRunLow, dxdStretch,
				&dxdWidthLow));
		}
		if (ichRunLow == 0)
			dxdWidthLow = 0;
		// now, measure one character more, and advance ichRunLow until we have
		// the width bracketed. We have ensured that dxdRun is less than
		// width plus stretch, so ichRunLow should be strictly less than cch.
		int dxdWidthHigh;
		for(;;)
		{
			CheckHr(pvg->GetTextLeadWidth(cch, prgch, ichRunLow + 1, dxdStretch,
				&dxdWidthHigh));
			if (dxdWidthHigh > dxdRun)
				break;
			if (ichRunLow >= cch)
			{ // run overflow: select at end of run
				m_ich = m_ichLimRun;
				m_fBefore = true;
				return false;
			}
			// otherwise, scan forward.
			ichRunLow ++;
			dxdWidthLow = dxdWidthHigh;
		}
		// now width up to (but not including) ichRunLow is <= dxdRun,
		// and width up to and including ichRunLow is > dxdRun.
		// Therefore ichRunLow indexes the character the user clicked on.
		// We just have to decide which side of it.
		int dxdChWidth = dxdWidthHigh - dxdWidthLow;
		int dxdChClick = dxdRun - dxdWidthLow;
		m_ich = ichRunLow + (m_ichLimRun - cch);
		m_fBefore = false;
		if (dxdChClick > dxdChWidth / 2)
		{
			// click in right half of char; IP goes after it
			m_ich ++;
			m_fBefore = true;
		}

		m_fGotIt = true;
		return true; // continue the loop to get height
	}
	void GetResults(int * pich, ComBool * pfBefore, int * pdydHeight)
	{
		*pfBefore = m_fBefore;
		*pich = m_ich;
		// This is only approximate, if we stopped the loop early, but should be good
		// enough for the purpose.
		*pdydHeight = m_dydAscent + m_dydDescent;
	}
};

/*----------------------------------------------------------------------------------------------
	Given a point in or near the segment, determine what character position it corresponds to.
	Arguments:
		 rcSrc, rcDst		as for DrawText
		 ptClickPosition	 dest coords
		 pfAssocPrev		 true if click was logically before indicated position
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::PointToChar(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst, POINT ptdClickPosition, int * pich, ComBool * pfAssocPrev)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(pich);
	ChkComArgPtr(pfAssocPrev);

	Rect rcSrc(rcSrc1);
	if (m_fReversed && m_fEndLine)
	{
		// Right-to-left trailing white-space: draw position is assumed to be just to the
		// left of the visible stuff, so invisible stuff has been scooted to the left.
		Assert(m_dxsTotalWidth != -1);
		rcSrc.left += (m_dxsTotalWidth - m_dxsWidth);
		rcSrc.right += (m_dxsTotalWidth - m_dxsWidth);
	}
	int dydPt15 = MulDiv(15, rcDst.bottom - rcDst.top, kdzptInch);
	// If the point is right outside our rectangle, answer one of our bounds
	// If it is much above us (15 points, a line or so), treat as at the start
	if (ptdClickPosition.y < rcSrc.MapYTo(0, rcDst) - dydPt15)
	{
		*pich = ichBase;
		*pfAssocPrev = false;
		return S_OK;
	}
	// If left of us, answer the start
	// ENHANCE JohnT: if we decide to use RRS for right-to-left, this may need fixing.
	if (ptdClickPosition.x < rcSrc.MapXTo(0, rcDst))
	{
		*pich = ichBase;
		*pfAssocPrev = false;
		return S_OK;
	}
	PointCharBinderRoman pcb(ptdClickPosition.x, ichBase);
	DoAllRuns(ichBase, pvg, rcSrc, rcDst, pcb);
	int dydHeight;
	pcb.GetResults(pich, pfAssocPrev, &dydHeight);
	// If the point is much below, treat as end, whatever x position.
	// Mapping 0 to dest coords gives us the Y position of the top of the segment.
	if (ptdClickPosition.y > rcSrc.MapYTo(0, rcDst) + dydHeight + dydPt15)
	{
		*pich = ichBase + m_dichLim;
		*pfAssocPrev = true;
	}
	else
	{
		// Handle surrogate pairs properly:
		//		DC00;<Low Surrogate, First>;Cs;0;L;;;;;N;;;;;
		//		DFFF;<Low Surrogate, Last>;Cs;0;L;;;;;N;;;;;
		OLECHAR rgch[2] = {0, 0};
		CheckHr(m_qts->Fetch(*pich, *pich + 1, rgch));
		if (0xDC00 <= rgch[0] && rgch[0] <= 0xDFFF)
		{
			if (*pich > ichBase)
				--(*pich);
		}
	}
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
STDMETHODIMP RomRenderSegment::ArrowKeyPosition(int ichBase, IVwGraphics * pvg, int * pich,
	ComBool * pfAssocPrev, ComBool fRight, ComBool fMovingIn, ComBool * pfResult)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(pich);
	ChkComArgPtr(pfAssocPrev);
	ChkComOutPtr(pfResult);

	*pfResult = TRUE;
	if (fRight != RightToLeft())
	{
		// Moving forward.

		if (!fMovingIn && *pich >= ichBase + m_dichLim)
		{
			*pfResult = FALSE;
		}
		else
		{
			if (fMovingIn)
				*pich = ichBase;
			if (*pich >= ichBase + m_dichLim)
			{
				// It's just possible to be moving in to an empty segment (e.g., an empty string
				// following a grey box in a list).
				*pfResult = FALSE;
			}
			else
			{
				++*pich;
				FindValidIPForward(ichBase, pich);
				// Since we are moving forward, we want to associate the selection with the
				// previous character.
				*pfAssocPrev = true;
			}
		}
	}
	else	// Moving backward
	{
		if (!fMovingIn && *pich <= ichBase)
		{
			*pfResult = FALSE;
		}
		else
		{
			if (fMovingIn)
				*pich = ichBase + m_dichLim;
			--*pich;
			FindValidIPBackward(ichBase, pich);
			// Since we are moving backward, we want to associate the selection with the
			// following character.
			*pfAssocPrev = false;
		}
	}
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

// Added from UniscribeSegment.cpp
/*----------------------------------------------------------------------------------------------
	If the character position given by *pich is not a valid insertion point, adjust *pich
	backwards until it is (or until the beginning of the segment, which should be a valid
	insertion point).

	@param ichBase offset within the string of the current segment
	@param pich current offset within the string
----------------------------------------------------------------------------------------------*/
void RomRenderSegment::FindValidIPBackward(int ichBase, int * pich)
{
	while (*pich > ichBase)
	{
		LgIpValidResult ipvr;
		CheckHr(IsValidInsertionPoint(ichBase, NULL, *pich, &ipvr));
		if (ipvr == kipvrOK)
			return;
		(*pich)--;
	}
}

// Added from UniscribeSegment.cpp
/*----------------------------------------------------------------------------------------------
	If the character position given by *pich is not a valid insertion point, adjust *pich
	forwards until it is (or until the end of the segment, which should be a valid insertion
	point).

	@param ichBase offset within the string of the current segment
	@param pich current offset within the string
----------------------------------------------------------------------------------------------*/
void RomRenderSegment::FindValidIPForward(int ichBase, int * pich)
{
	while (*pich < ichBase + m_dichLim)
	{
		LgIpValidResult ipvr;
		CheckHr(IsValidInsertionPoint(ichBase, NULL, *pich, &ipvr));
		if (ipvr == kipvrOK)
			return;
		(*pich)++;
	}
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
STDMETHODIMP RomRenderSegment::ExtendSelectionPosition(int ichBase, IVwGraphics * pvg,
	int * pich, ComBool fAssocPrevMatch, ComBool fAssocPrevNeeded, int ichAnchor,
	ComBool fRight, ComBool fMovingIn, ComBool * pfRet)
{
	// Argument checking done in ArrowKeyPosition.

	// Make the same adjustment as for an insertion point.
	ComBool fDummy;
	return ArrowKeyPosition(ichBase, pvg, pich, &fDummy, fRight, fMovingIn, pfRet);
}

class CPBinderRoman
{
	int m_dydAscent;
	int m_cxdMax;
	int * m_prgxdLefts;
	int * m_prgxdRights;
	int * m_prgydTops; // ie, the top of the underline
	int m_dydGap;
	int m_ichMin; // relative to segment!
	int m_ichLim;
	int m_cch; // processed in previous runs
	int m_ydTopPrev; // value for previous run, if any
	bool m_fStarted;
public:
	int m_cxd;
	CPBinderRoman(int cxdMax, int * prgxdLefts, int * prgxdRights, int * prgydTops,
		int ichMin, int ichLim)
	{
		m_dydAscent = 0;
		m_cxd = 0;
		m_cxdMax = cxdMax;
		m_prgxdLefts = prgxdLefts;
		m_prgxdRights = prgxdRights;
		m_prgydTops = prgydTops;
		m_ichMin = ichMin;
		m_ichLim = ichLim;
		m_cch = 0;
		m_fStarted = false;
	}

	// The basic idea is to make an entry in the array each time we are called,
	// specifying how to underline that segment.
	// However, we don't know the baseline of the segment until the last interval,
	// so until then we just record the offset from the baseline in m_prgydTops.
	// Also, if two adjacent segments have the same ydTop, we can merge the line
	// segments.
	// Furthermore, we don't make our first entry until we reach m_ichMin, and we
	// stop when we get to m_ichLim.
	bool operator() (IVwGraphics* pvg, const OLECHAR *prgch, int cch, bool fLast,
		int xd, int dxdStretch, Rect rcSrc, Rect rcDst, LgCharRenderProps * pchrp)
	{
		m_cch += cch; // From here on it includes the current run!
		int dydAscent;
		int dydHeight;
		int dxdWidth;
		CheckHr(pvg->get_FontAscent(&dydAscent));
		int dydOffset = MulDiv(pchrp->dympOffset, rcDst.Height(), kdzmpInch);
		int dydThisAscent = dydAscent + dydOffset;
		if (dydThisAscent > m_dydAscent)
			m_dydAscent = dydThisAscent;

		// For computing the vertical position of the underline, only negative offsets
		// count. We lower the underline for subscripts, but don't raise it for superscripts.
		if (dydOffset > 0)
			dydOffset = 0;

		if (!m_fStarted)
		{
			// We haven't started yet. See if this run extends past the start position.
			if (m_cch > m_ichMin)
			{
				// Yes, we will make a start.
				m_fStarted = true;
				// The gap between the baseline and underlining is one pixel, on a normal screen
				// at standard magnification,
				// and as near as we can get to the equivalent in any special case.
				m_dydGap = rcDst.Height() / 96;

				// Now compute how far over m_ichMin is
				int cchPrev = m_ichMin - (m_cch - cch);
				int xdLeft = xd;
				if (cchPrev)
				{
					CheckHr(pvg->GetTextExtent(cchPrev, prgch, &dxdWidth, &dydHeight));
					xdLeft += dxdWidth;
				}
				m_cxd = 1;
				if (m_cxd < m_cxdMax)
				{
					m_prgxdLefts[0] = xdLeft;
					m_ydTopPrev = m_dydGap - dydOffset;
					m_prgydTops[0] = m_ydTopPrev;
				}
			}
		}
		else
		{
			// A subsequent run. If it is at a different height,
			// and we are interested in characters from it, we must make a new
			// line segment. Otherwise, we can just extend the current one.
			if (m_ichLim > m_cch - cch)
			{
				int ydTop = m_dydGap - dydOffset;
				if (ydTop != m_ydTopPrev)
				{
					// Left of this run is the end of the previous underline seg and the
					// start of a new one.

					m_cxd++;
					if (m_cxd < m_cxdMax)
					{
						m_prgxdRights[m_cxd - 2] = xd;
						m_prgxdLefts[m_cxd - 1] = xd;
						m_ydTopPrev = ydTop;
						m_prgydTops[m_cxd - 1] = m_ydTopPrev;
					}
				}
			}
		}
		// Ignore any request for chars beyond end of segment. This could legitimately
		// happen if the segment includes trailing blanks, but they are invisible
		// at a line boundary.
		if (fLast && m_ichLim > m_cch)
			m_ichLim = m_cch;

		// If this is the last run that we are being asked about, or if the chars of
		// interest ended exactly at the end of the previous run, record the final
		// end-point.
		// A special case is if underlining extends to the segment end: since there
		// will be no subsequent run, we must compute the width here.
		if (m_ichLim >= m_cch - cch && // underlining doesn't stop before this run
				(m_ichLim < m_cch || fLast)) // it does stop in this run
		{
			int cchRun = m_ichLim - (m_cch - cch); // from start this run to lim
			dxdWidth = 0;
			if (cchRun)
				CheckHr(pvg->GetTextExtent(cchRun, prgch, &dxdWidth, &dydHeight));
			m_prgxdRights[m_cxd - 1] = xd + dxdWidth;
		}

		if (!fLast)
			return true;

		// In the last segment we can compute the actual overall ascent of the segment.
		// Go through and add this to all the yTop values.
		int ixdLim = min(m_cxd, m_cxdMax);
		int ydBaseline = m_dydAscent + rcSrc.MapYTo(0, rcDst);
		for (int i = 0; i < ixdLim; i++)
			m_prgydTops[i] += ydBaseline;

		return true;
	}
};

/*----------------------------------------------------------------------------------------------
	Used to find where underlines should be drawn.
	As usual, if cxdMax is zero, it just answers the number of slots needed
	to return the information.
	Arguments:
		rcSrc				 as for DrawText
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderSegment::GetCharPlacement(int ichBase, IVwGraphics * pvg, int ichMin,
	int ichLim, RECT rcSrc1, RECT rcDst1, ComBool fSkipSpace, int cxdMax, int * pcxd,
	int * prgxdLefts, int * prgxdRights, int * prgydUnderTops)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArrayArg(pcxd, cxdMax);
	ChkComArrayArg(prgxdLefts, cxdMax);
	ChkComArrayArg(prgxdRights, cxdMax);
	ChkComArrayArg(prgydUnderTops, cxdMax);

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	if (m_fReversed && m_fEndLine)
	{
		// Right-to-left trailing white-space: draw position is assumed to be just to the
		// left of the visible stuff, so scoot invisible stuff to the left.
		Assert(m_dxsTotalWidth != -1);
		rcSrc.left += (m_dxsTotalWidth - m_dxsWidth);
		rcSrc.right += (m_dxsTotalWidth - m_dxsWidth);
	}
	CPBinderRoman cpb(cxdMax, prgxdLefts, prgxdRights, prgydUnderTops, ichMin - ichBase,
		ichLim - ichBase);
	DoAllRuns(ichBase, pvg, rcSrc, rcDst, cpb, true);
	* pcxd = cpb.m_cxd;
	if (cxdMax != 0 && *pcxd > cxdMax)
		return E_FAIL;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Return the rendered glyphs and their x- and y-coordinates.
----------------------------------------------------------------------------------------------*/
/*
STDMETHODIMP RomRenderSegment::GetGlyphsAndPositions(int ichwBase, IVwGraphics * pvg,
	RECT rsArg, RECT rdArg,	int cchMax, int * pcchRet,
	OLECHAR * prgchGlyphs, int * prgxd, int * prgyd)
{
	BEGIN_COM_METHOD
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}
*/

/*----------------------------------------------------------------------------------------------
	For debugging. Return the characters in this segment.
----------------------------------------------------------------------------------------------*/
/*
STDMETHODIMP RomRenderSegment::GetCharData(int ichBase, int cchMax,
	OLECHAR * prgch, int * pcchRet)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgch, cchMax-ichBase);
	ChkComOutPtr(pcchRet);

	int ichLimTmp = ichBase + min(m_dichLim, cchMax);
	CheckHr(m_qts->Fetch(ichBase, ichLimTmp, prgch));
	*pcchRet = ichLimTmp - ichBase;

	END_COM_METHOD(g_fact, IID_ILgSegment);
}
*/

//:>********************************************************************************************
//:>	   Other public methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Compute the ascent, height, and width of the segment.
----------------------------------------------------------------------------------------------*/
class AscentBinderRoman
{
	int m_dydAscent;
	int m_xdRight;
	int m_dydDescent;
	bool m_fNeedWidth;

public:
	AscentBinderRoman(bool fNeedWidth)
	{
		m_dydAscent = 0;
		m_dydDescent = 0;
		m_fNeedWidth = fNeedWidth;
	}
	bool operator() (IVwGraphics* pvg, const OLECHAR *prgch, int cch, bool fLast,
		int xd, int dxdStretch, Rect rcSrc, Rect rcDst, LgCharRenderProps * pchrp)
	{
		int dydAscent;

		CheckHr(pvg->get_FontAscent(&dydAscent));
		int dydThisAscent = dydAscent + MulDiv(pchrp->dympOffset, rcDst.Height(), kdzmpInch);
		if (dydThisAscent > m_dydAscent)
			m_dydAscent = dydThisAscent;
		int dydHeight;
		int dxdWidth;
		if (fLast && m_fNeedWidth)
		{
			CheckHr(pvg->GetTextExtent(cch, prgch, &dxdWidth, &dydHeight));
			m_xdRight = xd + dxdWidth;
		} else {
			// We only need height, so measure trivial seg
			CheckHr(pvg->GetTextExtent(0, prgch, &dxdWidth, &dydHeight));
		}
		int dydThisDescent = dydHeight - dydThisAscent;
		if (dydThisDescent > m_dydDescent)
			m_dydDescent = dydThisDescent;
		return true;
	}
	void GetResults(int * xdRight, int * dydHeight, int * dydAscent)
	{
		*xdRight = m_xdRight;
		*dydHeight = m_dydAscent + m_dydDescent;
		*dydAscent = m_dydAscent;
	}
};

void RomRenderSegment::ComputeDimensions(int ichBase, IVwGraphics * pvg,
	Rect rcSrc, Rect rcDst, bool fNeedWidth)
{
	AscentBinderRoman ab(fNeedWidth);
	int dichLimOrig = m_dichLim;
	AdjustEndForWidth(ichBase, pvg);
	// Both source and dest rectangles just indicate the pixels per inch.
	DoAllRuns(ichBase, pvg, rcSrc, rcSrc, ab, fNeedWidth);
	int xsRight;
	ab.GetResults(&xsRight, &m_dysHeight, &m_dysAscent);
	if (fNeedWidth)
		m_dxsWidth = xsRight - rcSrc.MapXTo(0, rcDst);
	else
		m_dxsWidth = -1;
	if (dichLimOrig == m_dichLim)
	{
		// Total and visible width are the same.
		m_dxsTotalWidth = m_dxsWidth;
	}
	else
	{
		m_dichLim = dichLimOrig;
		DoAllRuns(ichBase, pvg, rcSrc, rcSrc, ab, fNeedWidth);
		// Use the height and ascent of the entire line, including trailing ows.
		ab.GetResults(&xsRight, &m_dysHeight, &m_dysAscent);
		m_dxsTotalWidth = xsRight - rcSrc.MapXTo(0, rcDst);
	}
	m_dichLim = dichLimOrig;
}

/*----------------------------------------------------------------------------------------------
	Adjust the endpoint of the segment to exclude trailing white space if m_fEndLine is true.
	Caller should record and restore real end point.
----------------------------------------------------------------------------------------------*/
#define MAX_WS_GROUP 20
void RomRenderSegment::AdjustEndForWidth(int ichBase, IVwGraphics * pvg)
{
	int ichLimWidth = ichBase + m_dichLim; // limit for figuring width
	if (m_fEndLine)
	{
		// don't include trailing white space in width.
		// For now, we just drop spaces. In Unicode, this is the only character
		// that gets dropped at line end.
		while (ichLimWidth > ichBase)
		{
			LgCharRenderProps chrp;
			int ichMinRun, ichLimRun;
			CheckHr(m_qts->GetCharProps(ichLimWidth - 1, &chrp, &ichMinRun, &ichLimRun));
			OLECHAR rgch[MAX_WS_GROUP];
			// Arbitrarily limit ourselves to getting MAX_WS_GROUP chars at a time.
			// The run could be long, and we usually only need one or two.
			if (ichLimRun > ichLimWidth)
				ichLimRun = ichLimWidth;
			if (ichLimRun - MAX_WS_GROUP > ichMinRun)
				ichMinRun = ichLimRun - MAX_WS_GROUP;
			if (ichMinRun < ichBase)
				ichMinRun = ichBase;
			CheckHr(m_qts->Fetch(ichMinRun, ichLimRun, rgch));
			// This loop continues until we find a non-space or get out of the run
			// Note that we are going to test the character BEFORE the one
			// indexed by ichLimWidth, so we want strict inequalities
			// in both the following tests.
			while (ichLimWidth > ichMinRun)
			{
				if (rgch[ichLimWidth - ichMinRun - 1] != ' ')
				{
					goto ExitBothLoops;
				}
				ichLimWidth--;
			}
			Assert(ichLimWidth == ichMinRun);
			// if we get here ichLimWidth is down to the start of the run.
			// allow the outer loop to make a new run
		}
		// If we drop out of both loops, we will measure an empty segment.
ExitBothLoops:
		m_dichLim = ichLimWidth - ichBase;  // temporarily exclude the trailing spaces
		// TODO 1438 (JohnT): this approach assumes that a segment is private to a thread.
	}
}
#undef MAX_WS_GROUP

/*----------------------------------------------------------------------------------------------
	Invoke the functor for every (non-empty) run of the segment. f(pvg, pch, cch, fLast, xd,
	dxdStretch, Rect & rcSrc, Rect rcDst) is called with the chars of each run covered
	by the segment.
	The fLast flag indicates whether the run is the last of the segment.
	The functor may break out of the loop prematurely (e.g., having found the run of
	interest) by returning a false. Before invoking the functor,
	DoAllRuns ensures that the VwGraphics is in the correct state for working with text in that
	run. The xd variable indicates the position of the left of this run.
	dxdStretch indicates what amount of the stretch of the whole
	segment is assigned to this run.
	rcSrc and rcDst indicate what transformation, if any, is in effect.
	If the segment is empty the functor is invoked once with cch 0.
	If fNeedWidth is false, the xd passed to the functor is meaningless,
	and no characters are actually put in prgch
----------------------------------------------------------------------------------------------*/
template<class Op> void RomRenderSegment::DoAllRuns(int ichBase, IVwGraphics * pvg,
	Rect rcSrc, Rect rcDst, Op & f, bool fNeedWidth)
{
	if (!m_qts)
		ThrowHr(WarnHr(E_UNEXPECTED));

	LgCharRenderProps chrp;
#define INIT_BUF_SIZE 1000
	OLECHAR rgchBuf[INIT_BUF_SIZE]; // Unlikely segments are longer than this...
	Vector<OLECHAR> vch; // Use as buffer if 1000 is not enough
	OLECHAR * prgch = rgchBuf; // Use on-stack variable if big enough
	int cchBuf = INIT_BUF_SIZE; // chars available in prgch; 1000 or vch.Size().

	int ichLim;  // start first run at start of segment
	int xd = rcSrc.MapXTo(0, rcDst);
	int dxdStretchRemaining = ScaleInt(m_dxsStretch, rcDst.Width(), rcSrc.Width());
	int dxdStretch = dxdStretchRemaining;
	if (0 == m_dxsWidth)
	{
		// can't handle stretch; probably ComputeDimensions call...
		dxdStretchRemaining = dxdStretch = 0;
	}

	// Each iteration handles one run, or part of a run if it is cut off
	// by ichBase or m_dichLim
	for(ichLim = ichBase;;)
	{
		int ichMin = ichLim; // start new seg at base, or end prev seg
		int ichMinDum; // for GetCharProps to return

		CheckHr(m_qts->GetCharProps(ichMin, &chrp, &ichMinDum, &ichLim));
		if (ichLim - ichBase > m_dichLim)
			ichLim = ichBase + m_dichLim;
		// Figure out what the chrp means and set up the VwGraphics
		{
			ILgWritingSystemPtr qLgWritingSystem;
			ILgWritingSystemFactoryPtr qLgWritingSystemFactory;
			CheckHr(m_qrre->get_WritingSystemFactory(&qLgWritingSystemFactory));
			AssertPtr(qLgWritingSystemFactory);
			CheckHr(qLgWritingSystemFactory->get_EngineOrNull(chrp.ws, &qLgWritingSystem));
			AssertPtr(qLgWritingSystem);
			CheckHr(qLgWritingSystem->InterpretChrp(&chrp));
		}
		CheckHr(pvg->SetupGraphics(&chrp));

		// Get the characters of the run, if any
		if (ichLim > ichMin && fNeedWidth)
		{
			if (ichLim - ichMin > cchBuf)
			{
				cchBuf = ichLim - ichMin + 1000; // probably enough for later runs too
				vch.Clear(); // don't need to copy old chars
				vch.Resize(cchBuf);
				prgch = vch.Begin();
			}
			CheckHr(m_qts->Fetch(ichMin, ichLim, prgch));
		}

		int dxdThisStretch = dxdStretchRemaining; // default for last seg
		int cch = ichLim - ichMin;
		bool fLast = ichLim - ichBase == m_dichLim;
		int dydHeight = 0; // Don't care for last seg
		int dxdWidth = 0;

		if (!fLast && fNeedWidth)
		{
			CheckHr(pvg->GetTextExtent(cch, prgch, &dxdWidth, &dydHeight));
			if (dxdThisStretch) // will be 0 if m_dxsWidth is zero
				dxdThisStretch = dxdWidth * dxdStretch / m_dxsWidth;
			dxdStretchRemaining -= dxdThisStretch;
		}
		// Pass the run to the functor. It is the last run if its limit is the segment's.
		// False from functor signals to break out of the loop.
		if (!f(pvg, prgch, cch, fLast, xd, dxdThisStretch, rcSrc, rcDst, &chrp))
			break;
		xd += dxdThisStretch + dxdWidth;
		// Exit the loop if we have drawn everything.
		// Don't put this test in the loop because it will prevent the one iteration
		// we need if 0 characters.
		if (ichLim - ichBase >= m_dichLim)
			break;
	}
}

#include "Vector_i.cpp"
template class Vector<OLECHAR>;
