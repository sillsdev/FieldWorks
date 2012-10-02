/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UniscribeSegment.cpp
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

#ifndef kchwHardLineBreak
#define kchwHardLineBreak (wchar)0x2028
#endif

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************
static DummyFactory g_fact(_T("SIL.Language1.UniscribeSeg"));
// Vector to hold UniscribeRunInfos in DoAllRuns()
static Vector<UniscribeRunInfo> g_vuri;

// cache of SCRIPT_CACHE values accessed by LgCharRenderProps.
UniscribeSegment::FwScriptCache UniscribeSegment::g_fsc;

ScrItemVec UniscribeSegment::g_vscri; // vector of script items from ScriptItemize.
int UniscribeSegment::g_cscri; // number of valid items in ScriptItemize.

#if !WIN32
template<> const GUID __uuidof(UniscribeSegment)("61299C3B-54D6-4c46-ACE5-72B9128F2048");
#endif

//:>********************************************************************************************
//:>	   UniscribeRunInfo
//:>********************************************************************************************
UniscribeRunInfo::UniscribeRunInfo(int cglyphMax, int cClusterMax)
{
	pvg = NULL;
	hdc = 0;
	prgch = NULL;
	cch = 0;
	fLast = false;
	m_fFromCopy = false;
	xd = 0;
	dxdStretch = 0;
	rcSrc = Rect();
	rcDst = Rect();
	pchrp = NULL;
	psa = NULL;
	sc = NULL;
	dxdWidth = 0;
	fScriptPlaceFailed = false;
	m_cglyphMax = 0;
	cglyph = 0;
	prgAdvance = NULL;
	prgcst = NULL;
	prgJustAdv = NULL;
	prgGlyph = NULL;
	prgoff = NULL;
	prgsva = NULL;
	m_cClusterMax = 0;
	prgCluster = NULL;

	if (cglyphMax > 0)
		UpdateGlyphSize(cglyphMax);
	if (cClusterMax > 0)
		UpdateClusterSize(cClusterMax);
}

UniscribeRunInfo::UniscribeRunInfo(const UniscribeRunInfo& oriUri)
{
	m_fFromCopy = true;
	pvg = oriUri.pvg;
	hdc = oriUri.hdc;
	prgch = oriUri.prgch;
	cch = oriUri.cch;
	fLast = oriUri.fLast;
	xd = oriUri.xd;
	dxdStretch = oriUri.dxdStretch;
	rcSrc = oriUri.rcSrc;
	rcDst = oriUri.rcDst;
	psa = oriUri.psa;
	sc = oriUri.sc;
	dxdWidth = oriUri.dxdWidth;
	fScriptPlaceFailed = oriUri.fScriptPlaceFailed;

	// The LgCharRenderProps need to be copied as well.
	pchrp = NewObj LgCharRenderProps;
	CopyBytes(oriUri.pchrp, pchrp, sizeof(LgCharRenderProps));

	// Now assign everything we allocated
	m_cglyphMax = oriUri.m_cglyphMax;
	cglyph = oriUri.cglyph;
	prgAdvance = oriUri.prgAdvance;
	prgcst = oriUri.prgcst;
	prgJustAdv = oriUri.prgJustAdv;
	prgGlyph = oriUri.prgGlyph;
	prgoff = oriUri.prgoff;
	prgsva = oriUri.prgsva;
	m_cClusterMax = oriUri.m_cClusterMax;
	prgCluster = oriUri.prgCluster;
}

UniscribeRunInfo::~UniscribeRunInfo()
{
	free(prgAdvance);
	free(prgcst);
	free(prgJustAdv);
#if !WIN32
	FreeGlyphs(prgGlyph, cglyph);
#endif
	free(prgGlyph);
	free(prgoff);
	free(prgsva);
	free(prgCluster);

	// Need to delete the pchrp if we created a new one from a copy, otherwise it will be
	// pointing to an object that should get deleted somewhere else (or NULL).
	if (m_fFromCopy)
		delete pchrp;

	m_cglyphMax = 0;
	cglyph = 0;
	prgAdvance = NULL;
	prgcst = NULL;
	prgJustAdv = NULL;
	prgGlyph = NULL;
	prgoff = NULL;
	prgsva = NULL;
	m_cClusterMax = 0;
	prgCluster = NULL;
	pchrp = NULL;
}

/*----------------------------------------------------------------------------------------------
	Detach the vectors we allocated so that they can be used with a different instance.
----------------------------------------------------------------------------------------------*/
void UniscribeRunInfo::Detach()
{
	m_cglyphMax = 0;
	cglyph = 0;
	prgAdvance = NULL;
	prgcst = NULL;
	prgJustAdv = NULL;
	prgGlyph = NULL;
	prgoff = NULL;
	prgsva = NULL;
	m_cClusterMax = 0;
	prgCluster = NULL;
	pchrp = NULL;
}

void UniscribeRunInfo::UpdateGlyphSize(int cglyphMax)
{
	Assert(cglyphMax > 0);
	prgAdvance = (int*)realloc(prgAdvance, cglyphMax * isizeof(int));
	prgcst = (int*)realloc(prgcst, cglyphMax * isizeof(int));
	prgJustAdv = (int*)realloc(prgJustAdv, cglyphMax * isizeof(int));
#if WIN32
	prgGlyph = (WORD*)realloc(prgGlyph, cglyphMax * isizeof(WORD));
#else
	prgGlyph = (WORD*)realloc(prgGlyph, cglyphMax * isizeof(void*));
#endif
	prgoff = (GOFFSET*)realloc(prgoff, cglyphMax * isizeof(GOFFSET));
	prgsva = (SCRIPT_VISATTR*)realloc(prgsva, cglyphMax * isizeof(SCRIPT_VISATTR));
	m_cglyphMax = cglyphMax;
}

void UniscribeRunInfo::UpdateClusterSize(int cClusterMax)
{
	Assert(cClusterMax > 0);
	prgCluster = (WORD*)realloc(prgCluster, cClusterMax * isizeof(WORD));
	m_cClusterMax = cClusterMax;
}

//:>********************************************************************************************
//:>	   Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructors.
----------------------------------------------------------------------------------------------*/
UniscribeSegment::UniscribeSegment()
{
	m_cref = 1;
	m_pdxsAvailStretch = NULL;
	ModuleEntry::ModuleAddRef();
}

UniscribeSegment::UniscribeSegment(IVwTextSource * pts, UniscribeEngine * pure, int dichLim,
	LgLineBreak lbrkStart, LgLineBreak lbrkEnd, ComBool fEndLine, ComBool fParaRTL)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_qts = pts;
	m_qure = pure;
	m_dichLim = dichLim;
	m_lbrkStart = lbrkStart;
	m_lbrkEnd = lbrkEnd;
	m_dxsStretch = 0;
	m_dxsWidth = -1; // so we know it is not initialized
	m_dxsTotalWidth = -1;
	m_fEndLine = (bool)fEndLine;
	m_fParaRTL = (bool)fParaRTL;
	m_pdxsAvailStretch = NULL;

//	m_fReversed = false;
	// To properly init m_dysAscent and m_dxsWidth, ComputeDimensions() must be called.
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
UniscribeSegment::~UniscribeSegment()
{
	if (m_pdxsAvailStretch)
		delete[] m_pdxsAvailStretch;

	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP UniscribeSegment::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_ILgSegment)
		*ppv = static_cast<ILgSegment *>(this);
	else if (&riid == &CLSID_UniscribeSegment)			// trick one for engine to get an impl
		*ppv = static_cast<UniscribeSegment *>(this);
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
	Call ScriptShape and ScriptPlace, putting the results in uri.prgGlyph etc.
	Increase buffer size as needed.
	Keep the relevant variables of uri pointing to the vector contents.
	Set the actual number of glyphs (usually less than uri.CGlyphMax())
	and the total advance width for the run (pixels in hdc) into uri.

	Side effects: modifies uri.psa->eScript if it is impossible to render the given
	characters using the requested font. Sets sc to a state useful for other Script
	calls on this hdc in this state (but which must eventually be freed).

	Inputs: uri.pchrp, uri.cch, prgch, hdc, psa, sc.
	Outputs: uri.prgCluster, prgAdvance, prgcst, prgJustAdv, prgGlyph, prgoff, prgsva,
		cglyph, (sc, psa).

	TODO: pass an argument indicating context (are we creating a segment, or using
	it to draw), as an optimization.
----------------------------------------------------------------------------------------------*/
void UniscribeSegment::ShapePlaceRun(UniscribeRunInfo& uri, bool fCreatingSeg)
{
	HRESULT hr;
	// Enhance JohnT: (multithread) lock static buffers.
	// Make sure buffers are big enough.
	int cglyphMax = uri.CGlyphMax();
	if (cglyphMax < uri.cch * 3 / 2 + 1)
	{
		// MS Doc says 1.5 * cch is enough for all current scripts but may not always be.
		// Hence the call-again if not enough.
		cglyphMax = uri.cch * 3 / 2 + 1;
		uri.UpdateGlyphSize(cglyphMax);
	}
	if (uri.CClusterMax() < uri.cch)
	{
		uri.UpdateClusterSize(uri.cch + 100); // reduce # of resize calls
	}
	SCRIPT_CACHE sc = uri.sc = g_fsc.FindScriptCache(/**uri.pchrp*/uri);

#if !WIN32
		// Associate VwGraphics with the cache as Linux uniscribe implementation needs it.
		IVwGraphicsWin32Ptr qvg32;
		CheckHr(uri.pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
		SetCachesVwGraphics(&uri.sc, qvg32);
#endif

	// loop to try ScriptShape multiple times
	while (true)
	{
		DISABLE_MULTISCRIBE
		{
			IgnoreHr(hr = ::ScriptShape(uri.hdc, &uri.sc, uri.prgch, uri.cch, cglyphMax, uri.psa,
				uri.prgGlyph, uri.prgCluster, uri.prgsva, &uri.cglyph));
		}

		if (hr == E_OUTOFMEMORY)
		{
			// Increase buffer size if necessary until we have enough.
			// Note: if the system is really out of memory, the resize operaions will fail.
			cglyphMax *= 2;
			uri.UpdateGlyphSize(cglyphMax);
			continue;
		}

		if (hr == USP_E_SCRIPT_NOT_IN_FONT) {

			// The user's selected font does not support the
			// script of this run, so force the script to 'SCRIPT_UNDEFINED'
			// This will display the characters directly through the font's
			// CMAP table, probably showing the text as square boxes (the
			// missing glyph)

			uri.psa->eScript = SCRIPT_UNDEFINED;
			continue;
		}
		if (FAILED(hr))
		{
#if WIN32
			StrUni stuErr;
			OLECHAR rgchErr[201];
			wcsncpy_s(rgchErr, uri.prgch, min(200, uri.cch));
			rgchErr[200] = L'\0';
			OLECHAR rgchFont[201];
			::GetTextFace(uri.hdc, 200, rgchFont);
			stuErr.Format(L"FieldWorks has encountered an unusual text rendering problem and may be unable to continue.\n"
				L"After you close this dialog FieldWorks may display the usual program failure dialog; if so please report the problem as usual.\n"
				L"This problem is sometimes caused by trying to use a font that is not installed; the font FieldWorks is trying to use is %s.\n"
				L"If you see this problem and cannot correct it by installing the proper font, please report it to FlexErrors@sil.org\n"
				L"It is also sometimes caused by very long strings, usually from some kind of data corruption; the current text has %d characters.\n"
				L"This problem is documented in the FieldWorks bug tracking system as TE-8046 and LT-9823.\n"
				L"This is (the start of) the text that cannot be rendered: %s.",
				rgchFont, uri.cch, rgchErr);
			::MessageBox(NULL, stuErr.Chars(), L"Error", MB_OK);
			ThrowHr(WarnHr(hr), stuErr);
#else
			printf("FieldWorks has encountered an unusual text rendering problem and may be unable to continue.\n");
			fflush(stdout);
			ThrowHr(WarnHr(hr));
#endif
		}
		break;
	}

	// Having generated glyphs, now generate advance widths and combining
	// offsets.
	ABC abc;          // Run combined ABC
	DISABLE_MULTISCRIBE
	{
		IgnoreHr(hr = ::ScriptPlace(uri.hdc, &uri.sc, uri.prgGlyph, uri.cglyph, uri.prgsva,
			uri.psa, uri.prgAdvance, uri.prgoff, &abc));
	}
	uri.fScriptPlaceFailed = FAILED(hr);
	if (FAILED(hr))
	{
		// Apparently we need to somehow recover from this, it can happen when asked to render
		// characters a font does not have. We're basically going to draw a one-pixel box with a 1 pixel border 1/20 inch wide.
		// We'll have one glyph per code point. Correct cglyph if necessary.
		uri.cglyph = 0;
		for (int ich = 0; ich < uri.cch; NextCodePoint(ich, uri.prgch, uri.cch))
			uri.cglyph++;

		int dpiX;
		CheckHr(uri.pvg->get_XUnitsPerInch(&dpiX));
		uri.dxdWidth = (dpiX/20 + 4) * uri.cglyph;
		// Treat them all as letters for justification purposes.
		for (int iglyph = 0; iglyph < uri.cglyph; iglyph++)
		{
			uri.prgcst[iglyph] = kcstLetter;
		}
	}
	else
	{
		uri.dxdWidth = abc.abcA + abc.abcB + abc.abcC;

		// Record the stretch possibilities for each glyph.
		if (fCreatingSeg)
		{
			SCRIPT_VISATTR * psva;
			for (int iglyph = 0; iglyph < uri.cglyph; iglyph++)
			{
				// The glyphs are in visual order (always LTR), but we want to generate the
				// stretch types in logical order.
				int iglyphLog = (uri.pchrp->fWsRtl) ? uri.cglyph - iglyph - 1 : iglyph;

				psva = &(uri.prgsva[iglyph]);
				if (psva->uJustification == SCRIPT_JUSTIFY_BLANK ||
					psva->uJustification == SCRIPT_JUSTIFY_ARABIC_BLANK)
				{
					uri.prgcst[iglyphLog] = kcstWhiteSpace;
				}
				else if (psva->fDiacritic)
					uri.prgcst[iglyphLog] = kcstDiac;
				else
					uri.prgcst[iglyphLog] = kcstLetter;
			}
		}
	}

	if (uri.sc && uri.sc != sc)
	{
		g_fsc.StoreScriptCache(/**uri.pchrp, uri.sc*/uri);
	}
}

/*----------------------------------------------------------------------------------------------
	Functor for DoAllRuns for drawing segment
----------------------------------------------------------------------------------------------*/
class DrawBinder
{
	int m_ydTop;
	int m_dydAscent;

public:
	DrawBinder(int ydTop, int dydAscent)
	{
		m_ydTop = ydTop;
		m_dydAscent = dydAscent;
	}
	bool operator() (UniscribeRunInfo & uri)
	{
		if (uri.cglyph == 0)
			return true;
		int dydAscent;

		CheckHr(uri.pvg->get_FontAscent(&dydAscent));

		int ydTop = m_ydTop + m_dydAscent - dydAscent -
				MulDiv(uri.pchrp->dympOffset, uri.rcDst.Height(), kdzmpInch);
		if (uri.fScriptPlaceFailed)
		{
			int glyphWidth = uri.dxdWidth / uri.cglyph;
			for (int i = 0; i < uri.cglyph; i++)
			{
				Rect rect;
				rect.top = ydTop + 1;
				rect.bottom = ydTop + m_dydAscent;
				rect.left = uri.xd + (i * glyphWidth) + 1;
				rect.right = rect.left + glyphWidth - 2;
				uri.pvg->DrawLine(rect.left, rect.top, rect.right, rect.top);
				uri.pvg->DrawLine(rect.left, rect.top, rect.left, rect.bottom);
				uri.pvg->DrawLine(rect.right, rect.top, rect.right, rect.bottom);
				uri.pvg->DrawLine(rect.left, rect.bottom, rect.right, rect.bottom);

				// This was another approach to making hollow boxes, but it fails in the usual case where
				// the default background color is transparent. Using the first three lines only produces
				// solid boxes.
				//uri.pvg->put_BackColor(uri.pchrp->clrFore);
				//uri.pvg->DrawRectangle(rect.left, rect.top, rect.right, rect.bottom);
				//uri.pvg->put_BackColor(uri.pchrp->clrBack);
				//uri.pvg->DrawRectangle(rect.left + 1, rect.top + 1, rect.right - 1, rect.bottom - 1);
			}
		}
		else
		{
			int dydAscent;

			CheckHr(uri.pvg->get_FontAscent(&dydAscent));

			int ydTop = m_ydTop + m_dydAscent - dydAscent -
					MulDiv(uri.pchrp->dympOffset, uri.rcDst.Height(), kdzmpInch);

			SCRIPT_CACHE sc = uri.sc = UniscribeSegment::FindScriptCache(/**uri.pchrp*/uri);

			DISABLE_MULTISCRIBE
			{
#if WIN32
				CheckHr(::ScriptTextOut(
					uri.hdc,
					&uri.sc,
					uri.xd,
					ydTop,
					0, // no extra clipping or opaque
					NULL, // no extra clip rect
					uri.psa,
					NULL, // Two reserved values must be zero
					0,
					uri.prgGlyph,
					uri.cglyph,
					uri.prgAdvance,
					uri.prgJustAdv,
					uri.prgoff));
#else
				// TODO-Linux: possibly implement ScriptTextOut and draw with that if needed.
				uri.pvg->DrawText(uri.xd, ydTop, uri.cch, uri.prgch, 0);
#endif
			}

			if (uri.sc && uri.sc != sc)
			{
				UniscribeSegment::StoreScriptCache(/**uri.pchrp, uri.sc*/uri);
			}
		}

		return true;
	}
};


/*----------------------------------------------------------------------------------------------
	Identical to DrawText, except passes true to DoAllRuns as final argument.
	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::DrawTextNoBackground(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int * pdxdWidth)
{
	BEGIN_COM_METHOD;
	return DrawTextInternal(ichBase, pvg, rcSrc1, rcDst1, pdxdWidth, true);
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

HRESULT UniscribeSegment::DrawTextInternal(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int * pdxdWidth, bool fSuppressBackground)
{
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
		dydAscent = ScaleIntY(m_dysAscent, rcDst, rcSrc);

	AdjustForRtlWhiteSpace(rcSrc);

	DrawBinder db(rcSrc.MapYTo(0, rcDst), dydAscent);
	*pdxdWidth = DoAllRuns(ichBase, pvg, rcSrc, rcDst, db, fSuppressBackground);
	return S_OK;
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::DrawText(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int * pdxdWidth)
{
	BEGIN_COM_METHOD;
	return DrawTextInternal(ichBase, pvg, rcSrc1, rcDst1, pdxdWidth, false);
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	In a right-to-left segment, the draw position is assumed to be just to the left of the
	visible stuff, so scoot the draw rectangle left to account for invisible white space.
----------------------------------------------------------------------------------------------*/
void UniscribeSegment::AdjustForRtlWhiteSpace(Rect & rcSrc)
{
	if (RightToLeft() && m_fEndLine)
	{
		Assert(m_dxsTotalWidth != -1);
		rcSrc.left += (m_dxsTotalWidth - m_dxsWidth);
		rcSrc.right += (m_dxsTotalWidth - m_dxsWidth);
	}
}

/*----------------------------------------------------------------------------------------------
	Tell the segment its measurements are invalid, because we are going to start asking
	questions using a different VwGraphics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::Recompute(int ichBase, IVwGraphics * pvg)
{
	m_dxsWidth = -1;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	The distance the drawing point should advance after drawing this segment.
	Always positive, even for RtoL segments. This includes any stretch that has
	been requested.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::get_Width(int ichBase, IVwGraphics * pvg, int * pdxs)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdxs);
	EnsureDefaultDimensions(ichBase, pvg);
	*pdxs = m_dxsWidth;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Distance that drawing takes place to the right of the rectangle specified by the width
	and drawing origin.
	It is legitimate not to implement this; calling code will typically treat it as zero.
	However, the not-implemented could be used to make sure we allow a few pixels.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::get_RightOverhang(int ichBase, IVwGraphics * pvg, int * px)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Overhang to the left of the drawing origin. Value returned will be >= 0.
	It is legitimate not to implement this; calling code will typically treat it as zero.
	However, the not-implemented could be used to make sure we allow a few pixels.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::get_LeftOverhang(int ichBase, IVwGraphics * pvg, int * px)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Height of the drawing rectangle. This is normally the full font height, even if all chars
	in segment are lower case or none have descenders, or even if segment is empty.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::get_Height(int ichBase, IVwGraphics * pvg, int * pdys)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdys);
	EnsureDefaultDimensions(ichBase, pvg);
	*pdys = m_dysHeight;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	The ascent of the font typically used to draw this segment. Basically, it determines the
	baseline used to align it.

	ENHANCE JohnT: we may need to make more than one baseline explicit in the interface.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::get_Ascent(int ichBase, IVwGraphics * pvg, int * pdys)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdys);
	EnsureDefaultDimensions(ichBase, pvg);
	*pdys = m_dysAscent;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Obtains height and width in a single call.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::Extent(int ichBase, IVwGraphics * pvg, int * pdxs, int * pdys)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdxs);
	ChkComOutPtr(pdys);
	EnsureDefaultDimensions(ichBase, pvg);
	*pdxs = m_dxsWidth;
	*pdys = m_dysHeight;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Compute the rectangle in destination coords which contains all the pixels
	drawn by this segment. This should be a sufficient rectangle to invalidate
	if this segment is about to be discarded and replaced by another.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::BoundingRect(int ichBase, IVwGraphics * pvg, RECT rcSrc1,
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
	int dxdHeight;
	if (rcDst.Height())
		dxdHeight = MulDiv(dydHeight, rcDst.Width(), rcDst.Height());
	else
		dxdHeight = dydHeight;
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
STDMETHODIMP UniscribeSegment::get_AscentOverhang(int ichBase, IVwGraphics * pvg, int * py)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Distance that drawing takes place below the rectangle specified by the descent
	and drawing origin. Value returned will be >= 0.
	It is legitimate not to implement this; calling code will typically treat it as zero.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::get_DescentOverhang(int ichBase, IVwGraphics * pvg, int * py)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Functor for DoAllRuns for computing actual drawn width of segment.

	We don't actually need to do anything here, DoAllRuns computes the width but just needs
	a functor that always returns true.
----------------------------------------------------------------------------------------------*/
class WidthBinder
{
public:
	WidthBinder()
	{
	}
	bool operator() (const UniscribeRunInfo & uri)
	{
		return true;
	}
};

/*----------------------------------------------------------------------------------------------
	Compute the width the segment would occupy if drawn with the specified
	parameters. Don't update cached width.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::GetActualWidth(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int * pdxdWidth)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pdxdWidth);
	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	WidthBinder wbFunc;
	*pdxdWidth = DoAllRuns(ichBase, pvg, rcSrc, rcDst, wbFunc);
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Answer whether the segment is right to left.
	REVIEW JohnT: is this good enough?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::get_RightToLeft(int ichBase,
	ComBool * pfResult)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfResult);
	*pfResult = (m_nDirDepth % 2);
	return S_OK;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Answer how deeply embedded this segment is in relative to the paragraph direction--
	how many changes of direction have to happen to get this in the right direction.
	Also indicate if this has weak directionality, eg, a white-space-only segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::get_DirectionDepth(int ichBase, int * pnDepth,
	ComBool * pfWeak)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pnDepth);
	ChkComOutPtr(pfWeak);
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
STDMETHODIMP UniscribeSegment::SetDirectionDepth(int ichBase, int nNewDepth)
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
//		m_fReversed = (bool)(m_nDirDepth % 2);
		return S_OK; // E_NOTIMPL;
	}
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the cookies which identify the old writing system of the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::get_WritingSystem(int ichBase, int * pws)
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
STDMETHODIMP UniscribeSegment::get_Lim(int ichBase, int * pdich)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pdich);
	*pdich = m_dichLim;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Get the last character of interest to the segment. UnicodeSeg never renders any characters
	beyond its logical end.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::get_LimInterest(int ichBase, int * pdich)
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
	UniscribeSegment uses this information only to decide whether to include trailing
	white space in the segment width.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::put_EndLine(int ichBase, IVwGraphics* pvg, ComBool fNewVal)
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
	UniscribeSegment ignores this; it does not handle start-line contextuals.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::put_StartLine(int ichBase, IVwGraphics* pvg, ComBool fNewVal)
{
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Get the type of break that occurs at the logical start of the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::get_StartBreakWeight(int ichBase, IVwGraphics * pvg,
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
STDMETHODIMP UniscribeSegment::get_EndBreakWeight(int ichBase, IVwGraphics * pvg,
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
STDMETHODIMP UniscribeSegment::get_Stretch(int ichBase, int * pdxs)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pdxs);
	*pdxs = m_dxsStretch;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Set the amount of stretch that has been set for the segment.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::put_Stretch(int ichBase, int dxs)
{
	BEGIN_COM_METHOD
	m_dxsStretch = dxs;
	m_dxsWidth = -1; // recompute it
	return S_OK;
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
STDMETHODIMP UniscribeSegment::IsValidInsertionPoint(int ichBase, IVwGraphics * pvg, int ich,
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
				CheckHr(LgIcuCharPropEngine::GetUnicodeCharProps(&m_qcpe));

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

/*----------------------------------------------------------------------------------------------

	Arguments:
		fBoundaryEnd		 asking about the logical end boundary?
		fBoundaryRight		 asking about the physical right boundary?
	REVIEW JohnT: do we need more complex answer here?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::DoBoundariesCoincide(int ichBase, IVwGraphics * pvg,
	ComBool fBoundaryEnd, ComBool fBoundaryRight, ComBool * pfResult)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComOutPtr(pfResult);
	*pfResult = TRUE;
	END_COM_METHOD(g_fact, IID_ILgSegment);
}

// ich is an offset into the whole paragraph; ichBase is the start of this segment, both in
// original (typically NFD) characters. Compute the offset of ich from the start of the
// segment that corresponds to ich (in NFC, if using NFC).
int UniscribeSegment::OffsetInNfc(int ich, int ichBase, IVwTextSource * pts)
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

// ich is an offset into the (NFC normalized) characters of this segment.
// convert it into a (typically NFD) position in the original paragraph.
// This is complicated because it isn't absolutely guaranteed that the original is
// normalized at all. Fortunately this is not called with high frequency.
// We look for the longest string that normalizes to ich characters.
int UniscribeSegment::OffsetToOrig(int ich, int ichBase, IVwTextSource * pts)
{
#ifdef UNISCRIBE_NFC
	if (ich == 0)
		return ichBase;
	StrUni stuInput;
	int ichNfd = ich; // first guess is same as input
	int cch;
	CheckHr(pts->get_Length(&cch));

	for (; ichNfd + ichBase <= cch; ichNfd++)
	{
		OLECHAR * pch;
		int cchGood = stuInput.Length();
		stuInput.SetSize(ichNfd, &pch);
		CheckHr(pts->Fetch(ichBase+cchGood, ichBase + ichNfd, pch + cchGood));
		StrUni stu1(stuInput);
		// This can return an error code, but it is very unlikely.
		StrUtil::NormalizeStrUni(stu1, UNORM_NFC);

		// The first time we get an NFC string longer than ich, one less must have
		// produced ich, so use that.
		if (stu1.Length() > ich)
			return ichNfd - 1 + ichBase;
	}
	// Reached the end of the input, use that.
	Assert(cch >= ichBase);
	return cch;
#else
	return ich + ichBase;
#endif
}

int OffsetInRun(UniscribeRunInfo & uri, int ichRun, bool fTrailing)
{
	int dxdRunToIP;

	if (uri.fScriptPlaceFailed)
	{
		int glyphWidth = uri.dxdWidth / uri.cglyph;
		dxdRunToIP = glyphWidth * ichRun;
	}
	else
	{
		// We can't get an offset for characters behind this run. ::ScriptCPtoX()
		// doesn't seem to check if ichRun is behind uri.cch, but sometimes it
		// notices it and returns a failure. Othertimes it just returns the end
		// of the run. So be safe here...
		if (ichRun >= uri.cch)
		{
			ichRun = uri.cch > 0 ? uri.cch - 1 : 0;
			fTrailing = true;
		}

		// There's not much we can do if this fails. We do output a warning, in case
		// it happens to developers and they can do something.
		// It seems that sometimes (for reasons I haven't yet figured out) this call
		// fails if the run is empty. In this case we just assume a 0 position offset.
		// If it fails on a non-empty run we're in trouble and better report it.
		DISABLE_MULTISCRIBE
		{
			HRESULT hr;
			IgnoreHr(hr = ::ScriptCPtoX(
				ichRun,
				fTrailing,
				uri.cch,
				uri.cglyph,
				uri.prgCluster,
				uri.prgsva,
				uri.prgJustAdv,
				uri.psa,
				&dxdRunToIP));

			if (FAILED(hr))
			{
				if (ichRun == 0)
				{
				//	WarnHr(hr);
					dxdRunToIP = 0; // I think ScriptCptoX leaves it unchanged, but make sure.
				}
				else
					ThrowHr(hr);
			}
		}
	}
	return dxdRunToIP;
}

/*----------------------------------------------------------------------------------------------
	Used in functions that are passed in to DoAllRuns() to determine the correct placement
	of the x value. This should be called in the case where we are calculating the last
	uniscribe segment, but haven't hit our ich position. This can happen because we strip off
	trailing spaces in the DoAllRuns() method which we need back for calculating IP positions,
	etc.
	@param cch The number of characters that have been processed so far
	@param ich The ich location we are looking for
	@param pvg The IvwGraphics object used to draw the run
	@param fTrailing True if we are looking for the location at the trailing edge of ich, false
					 if we are looking for the location at the leading edge of ich
----------------------------------------------------------------------------------------------*/
int GetXAdjustForMissingSpaces(int cch, int ich, IVwGraphics * pvg, bool fTrailing,
	bool fRightToLeft)
{
	if (cch <= ich)
	{
		static OleStringLiteral singleSpace(L" ");
		int dxdWidth, dydHeight;
		CheckHr(pvg->GetTextExtent(1, singleSpace, &dxdWidth, &dydHeight));
		return (ich - cch + (fTrailing ? 1 : 0)) * (fRightToLeft ? -dxdWidth : dxdWidth);
	}
	return 0;
}

/*----------------------------------------------------------------------------------------------
	DoAllRuns binder for drawing IP and figuring out where it goes.
----------------------------------------------------------------------------------------------*/
class DrawIPBinder
{
	int m_dydAscent;
	int m_ich; // index where we want to draw IP, relative to base of this segment.
	int m_cch; // count of (NFC) chars in previous runs of this segment
	int m_xdLeft;
	int m_ydTop;
	int m_xdRight;
	int m_ydBottom;
	bool m_fTrailing;
	bool m_fParaRTL;

public:
	// Construct one. Pass in ich (NFC char offset from start of segment)
	DrawIPBinder(int dydAscent, int ich, bool fTrailing, bool fParaRTL)
	{
		m_fParaRTL = fParaRTL;
		m_dydAscent = dydAscent;
		m_ich = ich;
		m_cch = 0;
		m_xdLeft = 0;
		m_ydTop = 0;
		m_xdRight = 0;
		m_ydBottom = 0;
		m_fTrailing = fTrailing;
	}
	bool operator() (UniscribeRunInfo & uri)
	{
		m_cch += uri.cch; // m_cch now has the number of chars to the end of this run.
		// If we haven't processed enough characters to get to the IP position, we
		// definitely don't want to draw here and do want to keep looping.
		if (m_cch < m_ich && !uri.fLast)
			return true;
		// If the IP is exactly at the end of the run, we generally don't want
		// to have this run draw it. (If fAssocPrev is true, we decrement m_ich
		// and set fTrailing true, so the run that really draws it will have
		// m_cch > m_ich.) We leave it to be drawn by another run of this segment,
		// or a subsequent segment.

		// However, if uri.cch is zero, there is nothing in the run,
		// which only happens if there is nothing in the whole segment, which in
		// turn only happens if the segment is not adjacent to any other text segments.
		// For example, the paragraph may be empty; or the segment may be between two
		// picture boxes; or it may be between two hard line breaks.
		// In any of these cases, we definitely DO want to draw the IP if m_cch is
		// exactly m_ich, as nothing else will draw one at this position.

		// If we return here, we are expecting a later run to draw the IP, so
		// return true to continue the loop.
		if (m_cch == m_ich && uri.cch > 0 && !uri.fLast)
			return true;

		// Chars in this run before the IP: chars in run, minus difference
		// between ip and end of run
		int cchRunToIP = uri.cch - (m_cch - m_ich);

		int dxdRunToIP = OffsetInRun(uri, cchRunToIP, m_fTrailing);

		int dydAscent;
		CheckHr(uri.pvg->get_FontAscent(&dydAscent));
		int dxdWidth;
		int dydHeight;
		CheckHr(uri.pvg->GetTextExtent(0, NULL, &dxdWidth, &dydHeight));
		// Make width two pixels.
		// ENHANCE JohnT: if we have really high-res screens one day we may need more.
		m_xdLeft = uri.xd + dxdRunToIP - 1;
		m_ydTop = uri.rcSrc.MapYTo(0, uri.rcDst) + m_dydAscent - dydAscent -
			MulDiv(uri.pchrp->dympOffset, uri.rcDst.Height(), kdzmpInch);
		m_xdRight = m_xdLeft + 2;
		m_ydBottom = m_ydTop + dydHeight;

		// If we are in the trailing whitespace that got stripped off of the segment
		// we want to draw the IP, so we need to adjust the position.
		int xdAdjust = GetXAdjustForMissingSpaces(m_cch, m_ich, uri.pvg, m_fTrailing, m_fParaRTL);
		m_xdLeft += xdAdjust;
		m_xdRight += xdAdjust;

		return false; // Stop loop, we computed it successfully.
	}
	void GetResults(int * pxdLeft, int * pydTop, int * pxdRight, int * pydBottom)
	{
		*pxdLeft = m_xdLeft;
		*pydTop = m_ydTop;
		*pxdRight = m_xdRight;
		*pydBottom = m_ydBottom;
	}
};

// Check whether the character at ich is an object replacement character.
bool CheckForOrcUs(IVwTextSource * pts, int ich)
{
	OLECHAR ch;
	CheckHr(pts->Fetch(ich, ich + 1, &ch));
	return ch == 0xfffc;
}

/*----------------------------------------------------------------------------------------------
	Answer true if the IP should be drawn by this segment.
----------------------------------------------------------------------------------------------*/
bool DrawIpHere(int ichBase, int dichLim, IVwTextSource * pts, int ich, ComBool fAssocPrev)
{
	// out of range is quite valid, just means IP is really in another segment.
	if (ich < ichBase || ich > ichBase + dichLim)
		return false;
	// Draw at start of segment only if associated with following char
	// (or if seg empty) (or at very start of paragraph--in case someone
	// set fAssocPrev inappropriately, don't want IP to disappear)
	if (ich && ich == ichBase && fAssocPrev && dichLim)
	{
		// Also, the previous character must not be an object one. If it is,
		// there is no previous segment to draw the IP.
		if (!CheckForOrcUs(pts, ich - 1))
			return false;
	}
	// Draw at end of segment only if associated with preceding char
	// (or if segment empty) (or if at very end of paragraph--in case
	// fAssocPrev set inappropriately)
	if (ich == ichBase + dichLim && (!fAssocPrev) && dichLim)
	{
		// if there is anything following in the para, let that draw it.
		int cchPara;
		CheckHr(pts->get_Length(&cchPara));
		if (cchPara > ich && !CheckForOrcUs(pts, ich))
			return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Draw an insertion point at an appropriate position.
	Arguments:
		rcSrc				 as for DrawText
		ich					 must be valid
		fAssocPrev			 primary associated with preceding character?
		fOn					 turning on or off? Caller should alternate, on first.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::DrawInsertionPoint(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int ich, ComBool fAssocPrev, ComBool fOn, LgIPDrawMode dm)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);
	if (!DrawIpHere(ichBase, m_dichLim, m_qts, ich, fAssocPrev))
		return S_OK;
	int dydAscent;
	if (m_dxsWidth == -1)
	{
		ComputeDimensions(ichBase, pvg, rcSrc, rcDst, false);
		dydAscent = m_dysAscent;
	}
	else
		dydAscent = ScaleIntY(m_dysAscent, rcDst, rcSrc);

	AdjustForRtlWhiteSpace(rcSrc);

	// By default we want to draw on the leading edge of character ich.
	// if fAssocPrev is true, and it is feasible, we want to draw on the trailing
	// edge of the previous character. Note that most of the infeasible cases are
	// handled above by not having this segment draw the IP at all.
	bool fTrailing = false;
	int ichNfc = OffsetInNfc(ich, ichBase, m_qts);
	if (fAssocPrev && ichNfc > 0)
	{
		ichNfc--;
		fTrailing = true;
	}
	DrawIPBinder dipb(dydAscent, ichNfc, fTrailing, m_fParaRTL);
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
STDMETHODIMP UniscribeSegment::PositionsOfIP(int ichBase, IVwGraphics * pvg,
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
	*pfSecHere = false; // Uniscribe renderer never draws secondary
	*pfPrimaryHere = false; // default
	if (!DrawIpHere(ichBase, m_dichLim, m_qts, ich, fAssocPrev))
		return S_OK;
	int dydAscent;
	if (m_dxsWidth == -1)
	{
		ComputeDimensions(ichBase, pvg, rcSrc, rcDst, false);
		dydAscent = m_dysAscent;
	}
	else
		dydAscent = ScaleIntY(m_dysAscent, rcDst, rcSrc);

	AdjustForRtlWhiteSpace(rcSrc);

	// By default we want to draw on the leading edge of character ich.
	// if fAssocPrev is true, and it is feasible, we want to draw on the trailing
	// edge of the previous character. Note that most of the infeasible cases are
	// handled above by not having this segment draw the IP at all.
	bool fTrailing = false;
	int ichNfc = OffsetInNfc(ich, ichBase, m_qts);
	if (fAssocPrev && ichNfc > 0)
	{
		ichNfc--;
		fTrailing = true;
	}
	DrawIPBinder dipb(dydAscent, ichNfc, fTrailing, m_fParaRTL);
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
	ENHANCE JohnT: more strictly correct to allow Uniscribe to redraw whole string in special
	mode.
----------------------------------------------------------------------------------------------*/
class DrawRangeBinder {
	int m_ichMin; // NFC offset from start of segment to start of selection.
	int m_ichLim; // NFC offset from start of segment to lim of selection.
	int m_cch; // count of chars in previous runs (and current) in this segment.
	bool m_fGotMin;
	bool m_fParaRTL;

	// Segment values: assume final segment should be the same as the first segment, even if
	// Uniscribe processing disagrees.
	bool m_fRTL;
	bool m_fLayoutRTL;
	unsigned m_uBidiLevel;

	// results
	int m_xdLeft;
	int m_xdRight;

public:

	// Pass the min and lim relative to the segment in NFC, and the count of characters before this
	// (ichBase=cchPrev).
	DrawRangeBinder(int ichMin, int ichLim, bool fParaRTL)
	{
		m_fParaRTL = fParaRTL;
		m_ichMin = ichMin;
		m_ichLim = ichLim;
		m_cch = 0;
		m_fGotMin = false;
		m_xdLeft = 0;
		m_xdRight = 0;
	}
	bool operator() (UniscribeRunInfo & uri)
	{
		m_cch += uri.cch; // now count of chars including this segment
		if (m_cch < m_ichMin && !uri.fLast)
			return true;  // not yet to start of range, keep going

		// Calculate the amount we have to add to left/right position if we are
		// in the trailing whitespace that got stripped off of the segment.
		int xdAdjust = GetXAdjustForMissingSpaces(m_cch, m_ichMin, uri.pvg, false, m_fParaRTL);

		// Shape (generate glyphs)
		if (!m_fGotMin)
		{
			// Chars in this run before the sel: chars in run, minus difference
			// between ip and end of run
			int cchRunToMin = uri.cch - (m_cch - m_ichMin);
			int dxdRunToMin = OffsetInRun(uri, cchRunToMin, false);
			m_xdLeft = xdAdjust + uri.xd + dxdRunToMin;
			m_fGotMin = true;
			m_fRTL = uri.psa->fRTL;
			m_fLayoutRTL = uri.psa->fLayoutRTL;
			m_uBidiLevel = uri.psa->s.uBidiLevel;
		}
		if (m_cch < m_ichLim && !uri.fLast)
			return true; // go on to later run for Lim

		xdAdjust = GetXAdjustForMissingSpaces(m_cch, m_ichLim, uri.pvg, false, m_fParaRTL);

		// Chars in this run before the sel end: chars in run, minus difference
		// between ip and end of run
		int cchRunToLim = uri.cch - (m_cch - m_ichLim);
		int dxdRunToLim;
		// Save values to restore in case they're needed for more processing.
		bool fSaveRTL = uri.psa->fRTL;
		bool fSaveLayoutRTL = uri.psa->fLayoutRTL;
		unsigned uSaveBidiLevel = uri.psa->s.uBidiLevel;
		// Make sure final segment directionality agrees with the first segment!
		uri.psa->fRTL = m_fRTL;
		uri.psa->fLayoutRTL = m_fLayoutRTL;
		uri.psa->s.uBidiLevel = m_uBidiLevel;
		dxdRunToLim = OffsetInRun(uri, cchRunToLim, false);
		m_xdRight = xdAdjust + uri.xd + dxdRunToLim;
		// Restore values in case they're needed for more processing.
		uri.psa->fRTL = fSaveRTL;
		uri.psa->fLayoutRTL = fSaveLayoutRTL;
		uri.psa->s.uBidiLevel = uSaveBidiLevel;
		return false; // stop loop, we got all we want.
	}
	void GetResults(int * pxdLeft, int * pxdRight)
	{
		Assert(m_fGotMin);
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
	ENHANCE JohnT: it would be better to use the proper Uniscribe approach of redrawing
	the selected text in a special mode.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::DrawRange(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, int ichMin, int ichLim, int ydTop, int ydBottom, ComBool bOn,
	ComBool fIsLastLineOfSelection, RECT * prsBounds)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(prsBounds);

	Rect rcBounds;
	ComBool fAnythingToDraw;
	CheckHr(PositionOfRange(ichBase, pvg, rcSrc1, rcDst1, ichMin, ichLim, ydTop, ydBottom,
		fIsLastLineOfSelection, &rcBounds, &fAnythingToDraw));

	// If we ever change this code to use some other means of drawing the selection than
	// inverting the rectangle, we need to move the code that inverts the whole segment
	// from VwStringBox::DrawRange() to here and also implement it in the Graphite segment
	// implementation.
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
STDMETHODIMP UniscribeSegment::PositionOfRange(int ichBase, IVwGraphics* pvg,
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
	int xdLeft;
	int xdRight;
	if (ichMin == 0 && ichLim == 0)
	{
		// Special case for drawing empty paragraph range. Invert the region that would be
		// occupied by one paragraph character in the font.
		AdjustForRtlWhiteSpace(rcSrc);
		xdLeft = rcSrc.MapXTo(0, rcDst);
		int dx, dy;
#pragma warning(disable: 4428)
		static OleStringLiteral paraMark(L"\u00B6"); // L"¶"
#pragma warning(default: 4428)
		CheckHr(pvg->GetTextExtent(1, paraMark, &dx, &dy));
		if (m_fParaRTL)
		{
			xdRight = xdLeft;
			xdLeft = xdRight - dx;
		}
		else
		{
			xdRight = xdLeft + dx;
		}
		*pfAnythingToDraw = true;
	}
	else
	{
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

		AdjustForRtlWhiteSpace(rcSrc);

		DrawRangeBinder drb(OffsetInNfc(ichMin, ichBase, m_qts),
			OffsetInNfc(ichLim, ichBase, m_qts), m_fParaRTL);
		DoAllRuns(ichBase, pvg, rcSrc, rcDst, drb);
		drb.GetResults(&xdLeft, &xdRight);
	}
	// In RTL scripts, may return the coords out of order. Make sure we return a
	// valid rectangle.
	rcBounds.left = min(xdLeft, xdRight);
	rcBounds.right = max(xdLeft, xdRight);
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
class PointCharBinder
{
	int m_xdClick;
	int m_ichLimRun; // limit of current run (previous run at very start of operator method) (non NFC)
	int m_ichLimWhiteSpace; // limit of current run including trailing spaces (non NFC)
	bool m_fParaRTL;

	int m_dydAscent; // used to accumulate height in dest coords
	int m_dydDescent;
	int m_ich; // result, NFC characters from start of segment.
	int m_fBefore; // result
	bool m_fGotIt; // true when we find it.

public:
	PointCharBinder(int xdClick, int ichBase, int ichLimWhiteSpace, bool fParaRTL)
	{
		m_fParaRTL = fParaRTL;
		m_ichLimWhiteSpace = ichLimWhiteSpace;
		m_ichLimRun = 0; // limit of previous run if any
		m_xdClick = xdClick;
		m_dydAscent = 0;
		m_dydDescent = 0;
		m_fGotIt = false;
	}
	bool operator() (const UniscribeRunInfo & uri)
	{
		m_ichLimRun += uri.cch; // includes chars of this run

		int offset = MulDiv(uri.pchrp->dympOffset, uri.rcDst.Height(), kdzmpInch);
		int dydAscent;
		CheckHr(uri.pvg->get_FontAscent(&dydAscent));
		int dydThisAscent = dydAscent - offset;
		if (dydThisAscent > m_dydAscent)
			m_dydAscent = dydThisAscent;
		int dydDescent;
		CheckHr(uri.pvg->get_FontDescent(&dydDescent));
		int dydThisDescent = dydDescent - offset;
		if (dydThisDescent > m_dydDescent)
			m_dydDescent = dydThisDescent;
		if (m_fGotIt)
			return true; // continue looping, but only to get overall segment height.

		int dichAdjust = 0;
		if(uri.xd + uri.dxdWidth + uri.dxdStretch < m_xdClick)
		{
			// to the right of our segment
			if (uri.fLast)
			{
				// we're the last segment, so we better handle it!
				if (m_ichLimRun < m_ichLimWhiteSpace && !m_fParaRTL)
				{
					// We are behind the current segment, but possibly in the
					// trailing whitespace. Figure the number of whitespace characters
					// and the space they take.
					int cchWhiteSpace = m_ichLimWhiteSpace - m_ichLimRun;
					int dxWhiteSpace = GetXAdjustForMissingSpaces(0, cchWhiteSpace,
						uri.pvg, true, m_fParaRTL);
					if (uri.xd + uri.dxdWidth + uri.dxdStretch + dxWhiteSpace < m_xdClick)
					{
						// The IP is to the right of the whitespace characters. Put IP
						// at the end of the whitespace characters.
						dichAdjust = cchWhiteSpace;
					}
					else
					{
						// The IP is somewhere in the trailing whitespace.
						// Calculate how many characters we are in the trailing whitespace.
						int x = m_xdClick - (uri.xd + uri.dxdWidth + uri.dxdStretch);
						int dxdWidth, dydHeight;
						static OleStringLiteral singleSpace(L" ");
						CheckHr(uri.pvg->GetTextExtent(1, singleSpace, &dxdWidth, &dydHeight));
						dichAdjust = (int)((x / (double)dxdWidth) + 0.5);
					}
				}
				// The IP is right of this whole segment. Treat as at right.
				m_xdClick = uri.xd + uri.dxdWidth + uri.dxdStretch;
			}
			else
			{
				// there are following segments that can handle it!
				return true;
			}
		}
		else if (m_xdClick < uri.xd)
		{
			// to the left of our segment
			if (uri.fLast)
			{
				// we're the last segment, so we have to handle it
				if (m_ichLimRun < m_ichLimWhiteSpace && m_fParaRTL)
				{
					// We are behind the current segment, but possibly in the
					// trailing whitespace. Figure the number of whitespace characters
					// and the space they take.
					int cchWhiteSpace = m_ichLimWhiteSpace - m_ichLimRun;
					int dxWhiteSpace = GetXAdjustForMissingSpaces(0, cchWhiteSpace,
						uri.pvg, true, m_fParaRTL); // returns a value < 0
					if (m_xdClick < uri.xd + dxWhiteSpace)
					{
						// The IP is to the left of the whitespace characters. Put IP
						// at the end of the whitespace characters.
						dichAdjust = cchWhiteSpace;
					}
					else
					{
						// The IP is somewhere in the trailing whitespace.
						// Calculate how many characters we are in the trailing whitespace.
						int x = uri.xd - m_xdClick;
						int dxdWidth, dydHeight;
						static OleStringLiteral singleSpace(L" ");
						CheckHr(uri.pvg->GetTextExtent(1, singleSpace, &dxdWidth, &dydHeight));
						dichAdjust = (int)((x / (double)dxdWidth) + 0.5);
					}
				}
				// The IP is left of this whole segment. Treat as at left.
				m_xdClick = uri.xd;
			}
			else
			{
				// there are more segments that can handle it
				return true;
			}
		}

		if (0 == m_ichLimWhiteSpace)
		{
			// The only zero-length run is if the whole segment (and string) is empty.
			m_ich = m_ichLimRun;
			m_fBefore = false; // rather arbitrary
			return false;
		}

		int ichChar, ichTrailing;
		if (uri.fScriptPlaceFailed)
		{
			// This is a slightly awkward way to get the info we want, but it makes the special-case
			// logic most like the normal case below.
			int glyphWidth = uri.dxdWidth / uri.cglyph;
			int offset = m_xdClick - uri.xd;
			int iglyph = offset / glyphWidth;
			bool fTrailing = offset - (iglyph * glyphWidth) > glyphWidth / 2;
			// ichChar is normally iglyph. But if there are surrogate pairs we have to adjust.
			// Advance ichChar one or two positions for each glyph.
			for (ichChar = 0; iglyph > 0; NextCodePoint(ichChar, uri.prgch, uri.cch))
			{
				iglyph--;
			}
			ichTrailing = 0;
			if (fTrailing)
			{
				int ichNext = ichChar;
				NextCodePoint(ichNext, uri.prgch, uri.cch);
				ichTrailing = ichNext - ichChar;
			}
		}
		else
		{
			DISABLE_MULTISCRIBE
			{
				HRESULT hr;
				IgnoreHr(hr = ::ScriptXtoCP(
					m_xdClick - uri.xd,  // ENHANCE JohnT: this neglects stretch.
					uri.cch,
					uri.cglyph,
					uri.prgCluster,
					uri.prgsva,
					uri.prgJustAdv,
					uri.psa,
					&ichChar,
					&ichTrailing));

				if (FAILED(hr))
				{
					ichChar = 0;
					ichTrailing = 0;
				}
			}
		}
		m_ich = ichChar + ichTrailing + (m_ichLimRun - uri.cch) + dichAdjust;
		m_fBefore = ichTrailing != 0 || ichChar == uri.cch;
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
	If the character position given by *pich is not a valid insertion point, adjust *pich
	backwards until it is (or until the beginning of the segment, which should be a valid
	insertion point).

	@param ichBase offset within the string of the current segment
	@param pich current offset within the string
----------------------------------------------------------------------------------------------*/
void UniscribeSegment::FindValidIPBackward(int ichBase, int * pich)
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

/*----------------------------------------------------------------------------------------------
	If the character position given by *pich is not a valid insertion point, adjust *pich
	forwards until it is (or until the end of the segment, which should be a valid insertion
	point).

	@param ichBase offset within the string of the current segment
	@param pich current offset within the string
----------------------------------------------------------------------------------------------*/
void UniscribeSegment::FindValidIPForward(int ichBase, int * pich)
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
	Given a point in or near the segment, determine what character position it corresponds to.
	Arguments:
		 rcSrc, rcDst		as for DrawText
		 ptClickPosition	 dest coords
		 pfAssocPrev		 true if click was logically before indicated position
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::PointToChar(int ichBase, IVwGraphics * pvg,
	RECT rcSrc1, RECT rcDst1, POINT ptdClickPosition, int * pich, ComBool * pfAssocPrev)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(pich);
	ChkComArgPtr(pfAssocPrev);

	Rect rcSrc(rcSrc1);
	Rect rcDst(rcDst1);

	AdjustForRtlWhiteSpace(rcSrc);

	int dydPt15 = MulDiv(15, rcDst.Height(), kdzptInch);
	// If the point is right outside our rectangle, answer one of our bounds
	// If it is much above us (15 points, a line or so), treat as at the start
	if (ptdClickPosition.y < rcSrc.MapYTo(0, rcDst) - dydPt15)
	{
		*pich = ichBase;
		*pfAssocPrev = false;
		return S_OK;
	}
	if (m_fParaRTL)
	{
		// If right of us, treat as exactly at the right
	}
	else
	{
		// If left of us, treat as exactly at the left
		if (ptdClickPosition.x < rcSrc.MapXTo(0, rcDst))
			ptdClickPosition.x = rcSrc.MapXTo(0, rcDst);
	}
	PointCharBinder pcb(ptdClickPosition.x, ichBase, m_dichLim, m_fParaRTL);
	DoAllRuns(ichBase, pvg, rcSrc, rcDst, pcb);
	int dydHeight;
	int ichNfc;
	pcb.GetResults(&ichNfc, pfAssocPrev, &dydHeight);
	*pich = OffsetToOrig(ichNfc, ichBase, m_qts);

	// If the point is much below, treat as end, whatever x position.
	// Mapping 0 to dest coords gives us the Y position of the top of the segment.
	if (ptdClickPosition.y > rcSrc.MapYTo(0, rcDst) + dydHeight + dydPt15)
	{
		*pich = ichBase + m_dichLim;
		*pfAssocPrev = true;
	}
	else
	{
		FindValidIPBackward(ichBase, pich);
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
STDMETHODIMP UniscribeSegment::ArrowKeyPosition(int ichBase, IVwGraphics * pvg, int * pich,
	ComBool * pfAssocPrev, ComBool fRight, ComBool fMovingIn, ComBool * pfResult)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(pich);
	ChkComArgPtr(pfAssocPrev);
	ChkComOutPtr(pfResult);
#if 0
	// We will use this function.
	// The tricky thing is getting the script analyis for the right run. It may take
	// implementing a full DoAllRuns functor. That means doing a wasteful amount of computation,
	// but it doesn't matter for the frequency of use of this function.
HRESULT WINAPI ScriptBreak(
  const WCHAR *pwcChars,
  int cChars,
  const SCRIPT_ANALYSIS *psa,
  SCRIPT_LOGATTR *psla
);
#endif
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

/*----------------------------------------------------------------------------------------------
	Indicate what logical position a shift-arrow-key combination should move the
	end of the selection to.
	Arguments:
		ichAnchor			needed? What if moving in?
		fRight				direction of desired movement
		fMovingIn			to this segment? If so, initial pich meaningless
		pfRet				if false if false, try next seg or string
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeSegment::ExtendSelectionPosition(int ichBase, IVwGraphics * pvg,
	int * pich, ComBool fAssocPrevMatch, ComBool fAssocPrevNeeded, int ichAnchor,
	ComBool fRight, ComBool fMovingIn, ComBool * pfRet)
{
	// Argument checking is done in ArrowKeyPosition.

	// Make the same adjustment as for an insertion point.
	ComBool fDummy;
	return ArrowKeyPosition(ichBase, pvg, pich, &fDummy, fRight, fMovingIn, pfRet);
}

/*----------------------------------------------------------------------------------------------
	CPBinder is a class whose sole purpose is to implement part of
	UniscribeSegment::GetCharPlacement, by having its operator method called once for each run
	in the segment. The end result is a sequence m_prgxdLefts/m_prgxdRights of 'xd' values
	(x coords in drawing pixels) that specify where an underline should go.
----------------------------------------------------------------------------------------------*/
class CPBinder
{
	int m_dydAscent;
	int m_cxdMax;
	int * m_prgxdLefts;
	int * m_prgxdRights;
	int * m_prgydTops; // ie, the top of the underline
	int m_dydGap;
	int m_ichMin; // relative to segment!
	int m_ichLim;
	int m_cch; // Number of characters processed in previous runs
	int m_ydTopPrev; // value for previous run, if any
	bool m_fStarted; // true if we started processing underline segments
	bool m_fRightToLeft;
public:
	int m_cxd;
	CPBinder(int cxdMax, int * prgxdLefts, int * prgxdRights, int * prgydTops,
		int ichMin, int ichLim, bool fRightToLeft)
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
		m_fRightToLeft = fRightToLeft;
	}

	// The basic idea is to make an entry in the array each time we are called,
	// specifying how to underline that segment.
	// However, we don't know the baseline of the segment until the last interval,
	// so until then we just record the offset from the baseline in m_prgydTops.
	// Also, if two adjacent segments have the same ydTop, we can merge the line
	// segments.
	// Furthermore, we don't make our first entry until we reach m_ichMin, and we
	// stop when we get to m_ichLim.
	bool operator() (UniscribeRunInfo & uri)
	{
		int cch = uri.cch;
		IVwGraphics * pvg = uri.pvg;
		m_cch += cch; // From here on it includes the current run!
		int dydAscent;
		int dxdWidth;
		CheckHr(pvg->get_FontAscent(&dydAscent));
		int dydOffset = MulDiv(uri.pchrp->dympOffset, uri.rcDst.Height(), kdzmpInch);
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
				// REVIEW (EberhardB): why do we hard-code 96? Is this correct? What if the user
				// has a different DPI setting like 120?
				m_dydGap = uri.rcDst.Height() / 96;

				// Now compute how far over m_ichMin is
				int cchPrev = m_ichMin - (m_cch - cch);
				int xdLeft = uri.xd;
				xdLeft += OffsetInRun(uri, cchPrev, FALSE);
				m_cxd = 1;
				if (0 < m_cxdMax)
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
			// For RTL segments, we always make a new one, since there could be bidi stuff
			// going on within the segment.
			if (m_ichLim > m_cch - cch)
			{
				int ydTop = m_dydGap - dydOffset;
				if (m_fRightToLeft)
				{
					m_cxd++;
					if (m_cxd < m_cxdMax)
					{
						int xdRight = uri.xd + OffsetInRun(uri, 0, FALSE);
						m_prgxdRights[m_cxd - 2] = xdRight; // previous bit of underlining ends at start of this
						m_prgxdLefts[m_cxd - 1] = xdRight; // our own bit starts there too
						m_ydTopPrev = ydTop;
						m_prgydTops[m_cxd - 1] = m_ydTopPrev;
					}
				}
				else if (ydTop != m_ydTopPrev)
				{
					// Left of this run is the end of the previous underline seg and the
					// start of a new one.

					m_cxd++;
					if (m_cxd < m_cxdMax)
					{
						m_prgxdRights[m_cxd - 2] = uri.xd;
						m_prgxdLefts[m_cxd - 1] = uri.xd;
						m_ydTopPrev = ydTop;
						m_prgydTops[m_cxd - 1] = m_ydTopPrev;
					}
				}
			}
		}
		// Ignore any request for chars beyond end of segment. This could legitimately
		// happen if the segment includes trailing blanks, but they are invisible
		// at a line boundary.
		if (uri.fLast && m_ichLim > m_cch)
			m_ichLim = m_cch;

		// If this is the last run that we are being asked about, or if the chars of
		// interest ended exactly at the end of the previous run, record the final
		// end-point.
		// A special case is if underlining extends to the segment end: since there
		// will be no subsequent run, we must compute the width here.
		if (m_ichLim >= m_cch - cch && // underlining doesn't stop before this run
				(m_ichLim < m_cch || uri.fLast) && // it does stop in this run
				m_cxd > 0) // we have started underlining
		{
			int cchRun = m_ichLim - (m_cch - cch); // from start this run to lim
			dxdWidth = 0;
			dxdWidth = OffsetInRun(uri, cchRun, FALSE);
			if (m_cxd - 1 < m_cxdMax)
				m_prgxdRights[m_cxd - 1] = uri.xd + dxdWidth;
		}

		if (!uri.fLast)
			return true;

		// In the last segment we can compute the actual overall ascent of the segment.
		// Go through and add this to all the yTop values.
		int ixdLim = min(m_cxd, m_cxdMax);
		int ydBaseline = m_dydAscent + uri.rcSrc.MapYTo(0, uri.rcDst);
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
STDMETHODIMP UniscribeSegment::GetCharPlacement(int ichBase, IVwGraphics * pvg, int ichMin,
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
	if (RightToLeft() && m_fEndLine)
	{
		// Right-to-left segment with possible trailing white-space: draw position is
		// assumed to be just to the left of the visible stuff, so scoot invisible stuff
		// to the left.
		Assert(m_dxsTotalWidth != -1);
		rcSrc.left += (m_dxsTotalWidth - m_dxsWidth);
		rcSrc.right += (m_dxsTotalWidth - m_dxsWidth);
	}
	CPBinder cpb(cxdMax, prgxdLefts, prgxdRights, prgydUnderTops, OffsetInNfc(ichMin, ichBase, m_qts),
		OffsetInNfc(ichLim, ichBase, m_qts), RightToLeft());
	DoAllRuns(ichBase, pvg, rcSrc, rcDst, cpb);
	* pcxd = cpb.m_cxd;
	if (cxdMax == 0)
		return S_OK;
	if (*pcxd > cxdMax)
		return E_FAIL;
	// Make sure the left ends are less than the right ends. This typically flips the pairs we got
	// for RTL segments.
	for (int i = 0; i < *pcxd; i++)
	{
		if (prgxdLefts[i] > prgxdRights[i])
		{
			int temp = prgxdRights[i];
			prgxdRights[i] = prgxdLefts[i];
			prgxdLefts[i] = temp;
		}
	}

	END_COM_METHOD(g_fact, IID_ILgSegment);
}

/*----------------------------------------------------------------------------------------------
	Return the rendered glyphs and their x- and y-coordinates.
----------------------------------------------------------------------------------------------*/
/*
STDMETHODIMP UniscribeSegment::GetGlyphsAndPositions(int ichwBase, IVwGraphics * pvg,
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
STDMETHODIMP UniscribeSegment::GetCharData(int ichBase, int cchMax,
	OLECHAR * prgch, int * pcchRet)
{
	BEGIN_COM_METHOD
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
	Compute the ascent, and height of the segment.
----------------------------------------------------------------------------------------------*/
class AscentBinder
{
	int m_dydAscent;
	int m_dydDescent;

public:
	AscentBinder()
	{
		m_dydAscent = 0;
		m_dydDescent = 0;
	}
	bool operator() (const UniscribeRunInfo & uri)
	{
		int dydAscent;

		CheckHr(uri.pvg->get_FontAscent(&dydAscent));
		int dydThisAscent =
			dydAscent + MulDiv(uri.pchrp->dympOffset, uri.rcDst.Height(), kdzmpInch);
		if (dydThisAscent > m_dydAscent)
			m_dydAscent = dydThisAscent;
		int dydHeight;
		int dxdWidth;
		CheckHr(uri.pvg->GetTextExtent(0, NULL, &dxdWidth, &dydHeight));
		int dydThisDescent = dydHeight - dydThisAscent;
		if (dydThisDescent > m_dydDescent)
			m_dydDescent = dydThisDescent;
		return true;
	}
	void GetResults(int * pdydHeight, int * pdydAscent)
	{
		*pdydHeight = m_dydAscent + m_dydDescent;
		*pdydAscent = m_dydAscent;
	}
};

/*----------------------------------------------------------------------------------------------
	TODO: write a comment.
----------------------------------------------------------------------------------------------*/
void UniscribeSegment::EnsureDefaultDimensions(int ichBase, IVwGraphics * pvg)
{
	if (m_dxsWidth == -1)
	{
		int dxInch, dyInch;
		CheckHr(pvg->get_XUnitsPerInch(&dxInch));
		CheckHr(pvg->get_YUnitsPerInch(&dyInch));
		Rect rcSrc(0, 0, dxInch, dyInch);
		ComputeDimensions(ichBase, pvg, rcSrc, rcSrc);
	}
}

/*----------------------------------------------------------------------------------------------
	Compute the ascent, height, and width of the segment.
----------------------------------------------------------------------------------------------*/
void UniscribeSegment::ComputeDimensions(int ichBase, IVwGraphics * pvg,
	Rect rcSrc, Rect rcDst, bool fNeedWidth)
{
	AscentBinder asbd;
	int dichLimOrig = m_dichLim;
	AdjustEndForWidth(ichBase, pvg);
	// Both source and dest rectangles just indicate the pixels per inch.
	int xsRight = DoAllRuns(ichBase, pvg, rcSrc, rcSrc, asbd);
	asbd.GetResults(&m_dysHeight, &m_dysAscent);
	if (fNeedWidth)
		m_dxsWidth = xsRight - rcSrc.MapXTo(0, rcDst);
	else
		m_dxsWidth = -1;
	if (dichLimOrig == m_dichLim)
	{
		// Total and visible width are the same.
		m_dxsTotalWidth = m_dxsWidth;
	}
	else if (fNeedWidth)
	{
		m_dichLim = dichLimOrig;
		xsRight = DoAllRuns(ichBase, pvg, rcSrc, rcSrc, asbd);
		int dysHeightTmp, dysAscentTmp;
		asbd.GetResults(&dysHeightTmp, &dysAscentTmp);
		m_dxsTotalWidth = xsRight - rcSrc.MapXTo(0, rcDst);
	}
	m_dichLim = dichLimOrig;
}

/*----------------------------------------------------------------------------------------------
	Adjust the endpoint of the segment to exclude trailing white space if m_fEndLine is true.
	Caller should record and restore real end point.
----------------------------------------------------------------------------------------------*/
#define MAX_WS_GROUP 20
void UniscribeSegment::AdjustEndForWidth(int ichBase, IVwGraphics * pvg)
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

/*----------------------------------------------------------------------------------------------
	This is a chunk of common code used by DoAllRuns and UniscribeEngine::FindBreakPoint.
	Typically prgchDefBuf is a fixed-size on-stack buffer that is used unless cch is too large,
	in which case vch is resized and used. *pprgchBuf gets set to the actual place the chars
	are put.
	In this buffer are placed the cch characters starting at ichMin in pts, which are then
	converted to NFC (if UNISCRIBE_NFC is true).
	Then, the code calls ScriptItemize to break those characters into 'items', which
	are placed into the first citem slots in g_vscri.
----------------------------------------------------------------------------------------------*/
int UniscribeSegment::CallScriptItemize(OLECHAR * prgchDefBuf, int cchBuf,
	Vector<OLECHAR> & vch, IVwTextSource * pts, int ichMin, int cch, OLECHAR ** pprgchBuf,
	int & citem, bool fParaRTL)
{
	* pprgchBuf = prgchDefBuf; // Use on-stack variable if big enough

#ifdef UNISCRIBE_NFC
	if (cch)
	{
		// We should be able to do something more efficient than this...
		StrUni stu;
		OLECHAR * pch;
		stu.SetSize(cch, &pch);
		CheckHr(pts->Fetch(ichMin, ichMin + cch, pch));
		StrUtil::NormalizeStrUni(stu,  UNORM_NFC);
		if (cch != stu.Length())
			cch = stu.Length();
		if (cch > cchBuf)
		{
			cchBuf = cch;
			vch.Resize(cch);
			* pprgchBuf = vch.Begin();
		}
		::memcpy(*pprgchBuf, stu.Chars(), isizeof(OLECHAR) * cch);
	}
#else
	if (cch > cchBuf)
	{
		cchBuf = cch;
		vch.Resize(cch);
		* pprgchBuf = vch.Begin();
	}
	// Get the characters
	CheckHr(pts->Fetch(ichMin, ichMin + cch, *pprgchBuf));
#endif

	int citemMax = g_vscri.Size();
	if (citemMax < 2)
	{
		citemMax = 100; // default starting size
		g_vscri.Resize(citemMax);
	}

//	ComBool fBaseRtl;
//	CheckHr(pts->GetBaseRtl(&fBaseRtl));
	int ichMin1, ichLim1;
	LgCharRenderProps chrp;
	CheckHr(pts->GetCharProps(ichMin, &chrp, &ichMin1, &ichLim1));
	bool fwsRtl = chrp.fWsRtl;
	bool fIsArabic = false;
/*
typedef struct tag_SCRIPT_CONTROL {
  DWORD uDefaultLanguage :16;
  DWORD fContextDigits :1;
  DWORD fInvertPreBoundDir :1;
  DWORD fInvertPostBoundDir :1;
  DWORD fLinkStringBefore :1;
  DWORD fLinkStringAfter :1;
  DWORD fNeutralOverride :1;
  DWORD fNumericOverride :1;
  DWORD fLegacyBidiClass :1;
  DWORD fReserved :8;
} SCRIPT_CONTROL;
*/
	SCRIPT_CONTROL scon = {
		0, false, false, false, false, false, false, false, false, 0, 0
	};
/*
typedef struct tag_SCRIPT_STATE {
  WORD uBidiLevel :5;
  WORD fOverrideDirection :1;
  WORD fInhibitSymSwap :1;
  WORD fCharShape :1;
  WORD fDigitSubstitute :1;
  WORD fInhibitLigate :1;
  WORD fDisplayZWG :1;
  WORD fArabicNumContext :1;
  WORD fGcpClusters :1;
  WORD fReserved :1;
  WORD fEngineReserved :2;
} SCRIPT_STATE;
*/
	// JohnT: an earlier version used fParaRTL instead of fwsRtl. However, this produces incorrect
	// mirroring of bracket-like characters in upstream text.
	SCRIPT_STATE ss = {
		fwsRtl ? 1 : 0,
		false, false, false, false, false, false, fIsArabic, false, false, 0
		};

	if (cch) // Can only call if at least one char.
	{
		DISABLE_MULTISCRIBE
		{
			while (true)
			{
				HRESULT hr;
				IgnoreHr(hr = ::ScriptItemize(*pprgchBuf, cch, citemMax,
					&scon, //NULL, // default SCRIPT_CONTROL
					&ss,
					g_vscri.Begin(),
					&citem));

				if (hr == E_OUTOFMEMORY)
				{
					citemMax *= 2; // try twice as much
					g_vscri.Resize(citemMax); // will fail if really out of memory
					continue;
				}
				if (FAILED(hr))
				{
					ThrowHr(WarnHr(hr), L"ScriptItemize failed");
				}
				break;
			}
		}
	}
	else
	{
		citem = 0;
		g_vscri[0].iCharPos = 0;
		g_vscri[1].iCharPos = 0;
	}
	return cch;
}
#undef MAX_WS_GROUP

// Figures out what the chrp means
void UniscribeSegment::InterpretChrp(LgCharRenderProps &chrp)
{
	ILgWritingSystemPtr qLgWritingSystem;
	ILgWritingSystemFactoryPtr qLgWritingSystemFactory;
	CheckHr(m_qure->get_WritingSystemFactory(&qLgWritingSystemFactory));
	if(qLgWritingSystemFactory)
	{
		CheckHr(qLgWritingSystemFactory->get_EngineOrNull(chrp.ws, &qLgWritingSystem));
		if (!qLgWritingSystem)
			ThrowHr(WarnHr(E_UNEXPECTED));
		CheckHr(qLgWritingSystem->InterpretChrp(&chrp));
	}
}

/*----------------------------------------------------------------------------------------------
	Invoke the functor for every (non-empty) run of the segment. f(uri) is called with the
	chars of each run covered by the segment.
	The fLast flag indicates whether the run is the last of the segment.
	The functor may break out of the loop prematurely (e.g., having found the run of
	interest) by returning a false. Before invoking the functor,
	DoAllRuns ensures that the VwGraphics is in the correct state for working with text in that
	run. The xd variable indicates the position of the left of this run.
	dxdStretch indicates what amount of the stretch of the whole
	segment is assigned to this run (currently always zero).
	uri.rcSrc and uri.rcDst indicate what transformation, if any, is in effect.
	If the segment is empty the functor is invoked once with cch 0. In this case uri.psa
	is not valid and should not be used.
	Returns the overall width of the segment, in dest coords. If the functor returns false,
	it will only include runs up to (but not including) the one that returned false.
----------------------------------------------------------------------------------------------*/
template<class Op> int UniscribeSegment::DoAllRuns(int ichBase, IVwGraphics * pvg,
	Rect rcSrc, Rect rcDst, Op & f, bool fSuppressBackgroundColor)
{
	if (!m_qts)
		ThrowHr(WarnHr(E_UNEXPECTED));

	int dichLimOrig = m_dichLim;
	AdjustEndForWidth(ichBase, pvg);

	// Get the characters
#define INIT_BUF_SIZE 1000
	OLECHAR rgchBuf[INIT_BUF_SIZE]; // Unlikely segments are longer than this...
	Vector<OLECHAR> vch; // Use as buffer if 1000 is not enough
	int citem; // actual number of items obtained.
	OLECHAR * prgchBuf; // Where text actually goes.
	int cchNfc = CallScriptItemize(rgchBuf, INIT_BUF_SIZE, vch, m_qts, ichBase, m_dichLim, &prgchBuf,
		citem, m_fParaRTL);

	// If dxdExpectedWidth is not 0, then the segment will try its best to stretch to the
	// specified size.
	int dxdExpectedWidth = 0;
	if (m_dxsWidth > 0)
		dxdExpectedWidth = ScaleIntX(m_dxsWidth, rcDst, rcSrc);

	int xsOrig = rcSrc.MapXTo(0, rcDst);
	int dxdStretchRemaining = ScaleIntX(m_dxsStretch, rcDst, rcSrc);
	if (0 == m_dxsWidth)
	{
		// can't handle stretch; probably ComputeDimensions call...
		dxdStretchRemaining = 0;
	}

	// Vector to store all uniscribe run infos. We calculate them first (so that we
	// get the width right), then go through all of them again to draw them (or whatever
	// we want to do)
	g_vuri.EnsureSpace(g_vscri.Size());
	int dxdWidth = 0;
	int cStretchable = NumStretchableGlyphs();

	// This outermost loop deals with rounding errors because we layed out in printer
	// resolution and might now try to draw in screen resolution - we loop until the
	// width we will draw matches (or is as close as possible) the width we calculated
	// previously
	for (;;)
	{
		int dxdStretchPrevious = dxdStretchRemaining;
		int dxdLastWidth = dxdWidth;
		int dxdOffset = xsOrig; // we have at least left margin
		dxdWidth = 0;
		g_vuri.Delete(0, g_vuri.Size());

		int cStretched = 0;
		int iglyphSeg = 0;

		// The current one we are processing;
		SCRIPT_ITEM * pscri = g_vscri.Begin();
		// This middle loop handles runs of characters with the same properties.
		// This variable is the limit of the NFC characters in this segment corresponding
		// to each ichLim position. We start it at zero so that we can correctly set
		// ichMinNfc to the old ichLimNfc at the same time we update ichMin to the old ichLim.
		int ichLimNfc = 0;
		for(int ichLim = ichBase;;)
		{
			int ichMin = ichLim; // start new seg at base, or end prev run
			int ichMinNfc = ichLimNfc;

			LgCharRenderProps chrp;
			int ichMinDum; // for GetCharProps to return
			CheckHr(m_qts->GetCharProps(ichMin, &chrp, &ichMinDum, &ichLim));
			InterpretChrp(chrp);
			CheckHr(pvg->SetupGraphics(&chrp));

			if (ichLim - ichBase > m_dichLim)
				ichLim = ichBase + m_dichLim;
			ichLimNfc = OffsetInNfc(ichLim, ichBase, m_qts);
			if (ichLimNfc == ichMinNfc && m_dichLim > 0)
			{
				// This can happen pathologically where later characters in a composition have different
				// properties from the first one and following characters. (This can actually happen
				// quite easily, for example, in certain intermediate states of typing Korean using
				// the TSF IME.) If we don't do something special, we will do all the following for an
				// effectively empty run, and probably advance to the next pscri twice, with disastrous
				// consequences. It is better to ignore the offending run that maps to no surface chars.
				// We also need in this case to break out of the loop if we've processed all the data.
				// However, if we have a completely empty segment, we need to call our function at least once,
				// so we don't do any of this if m_dichLim is zero.
				if (ichLimNfc >= cchNfc)
					break;
				continue;
			}
			// This inner loop handles the way Uniscribe breaks up the run.
			for (int ichLimRun = ichMinNfc ; ; )
			{
				int ichMinRun = ichLimRun;
				ichLimRun = ichLimNfc;
				if (ichLimRun > (pscri + 1)->iCharPos)
					ichLimRun = (pscri + 1)->iCharPos;

				// Create a new uri for this uniscribe run
				int cch = ichLimRun - ichMinRun;
				UniscribeRunInfo uri(cch * 3 / 2 + 1, cch);

				IVwGraphicsWin32Ptr qvg32;
				CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **) &qvg32));
				CheckHr(qvg32->GetDeviceContext(&uri.hdc));

				uri.pvg = pvg;
				uri.rcSrc = rcSrc;
				uri.rcDst = rcDst;
				uri.pchrp = &chrp;
				uri.xd = dxdOffset;
				uri.prgch = prgchBuf + ichMinRun; // Get the characters of the run, if any
				uri.cch = cch;
				uri.psa = &pscri->a;
				uri.dxdStretch = dxdStretchRemaining; // default for last seg
				uri.fLast = ichLimRun == ichLimNfc && ichLimNfc == cchNfc; // This is the last char group if it reaches the last surface character.

				ShapePlaceRun(uri);

				int cStretchableThisRun =
					StretchGlyphs(uri, cStretchable - cStretched, &dxdStretchRemaining, iglyphSeg);
				cStretched += cStretchableThisRun;

				iglyphSeg += uri.cglyph;
				dxdOffset += uri.dxdStretch + uri.dxdWidth;
				dxdWidth += uri.dxdStretch + uri.dxdWidth;

				if (ichLimNfc >= (pscri + 1)->iCharPos)
					pscri++;

				g_vuri.Push(uri);
				// We created a copy of uri that will be deleted when the vector vuri goes out of
				// scope. Make sure we don't try to delete uri's data.
				uri.Detach();

				// Exit the inner loop if we have processed as far as ichLimNfc.
				// Don't put this condition in the for because we want to ensure at least one call to f.
				if (ichLimRun >= ichLimNfc)
					break;
			} // inner loop - uniscribe runs
			// Exit the loop if we have drawn everything.
			// Don't put this test in the for stmt because it will prevent the one iteration
			// we need if 0 characters.
			if (ichLim - ichBase >= m_dichLim)
				break;
		} // middle loop - runs of characters with same properties
		// Don't put this condition in the for because we want to ensure at least one call to f.
		if (dxdWidth == dxdExpectedWidth || dxdLastWidth == dxdWidth || !dxdExpectedWidth)
			break;
		dxdStretchRemaining = dxdStretchPrevious + dxdExpectedWidth - dxdWidth;
	} // outermost loop


	// Now process all the uniscribe runs
	dxdWidth = 0;
	for (int iuri = 0; iuri < g_vuri.Size(); iuri++)
	{
		UniscribeRunInfo& uri = g_vuri[iuri];
		if (fSuppressBackgroundColor)
		{
			COLORREF temp = uri.pchrp->clrFore;
			uri.pchrp->clrFore = uri.pchrp->clrBack;
			if (uri.pchrp->clrFore == (COLORREF)kclrTransparent)
				uri.pchrp->clrFore = kclrWhite;
			uri.pchrp->clrBack = (COLORREF)kclrTransparent;
			CheckHr(pvg->SetupGraphics(uri.pchrp));
			// Do the operation (this is only used for actual painting) with a white/same as BG
			// foreground color first to erase a previous draw that has typically occurred.
			// Without this, ClearType produces extra boldness when we write the same thing
			// a second time in transparent mode.
			f(uri);
			// And the main, original draw will take place with the specified foreground color and
			// transparent background.
			uri.pchrp->clrFore = temp;
			uri.pchrp->clrBack = (COLORREF)kclrTransparent;
		}
		CheckHr(pvg->SetupGraphics(uri.pchrp));

		// Pass the run to the functor. It is the last run if its limit is the segment's.
		// False from functor signals to break out of the loop.
		if (!f(uri))
			break;

		dxdWidth += uri.dxdStretch + uri.dxdWidth;
	}
	g_vuri.Delete(0, g_vuri.Size()); // We are done with the vector
	m_dichLim = dichLimOrig; // restore original value
	return dxdWidth;
}

/*----------------------------------------------------------------------------------------------
	Destructor: free all the stored SCRIPT_CACHE values before the internal HashMap itself goes
	away.
----------------------------------------------------------------------------------------------*/
UniscribeSegment::FwScriptCache::~FwScriptCache()
{
	ResetScriptCacheMap(m_hmchrpsc);
	ResetScriptCacheMap(m_hmchrpscOther);
}

/*----------------------------------------------------------------------------------------------
	Store a SCRIPT_CACHE value if the LgCharRenderProps isn't already stored.

	@param chrp Character properties associated with the SCRIPT_CACHE value.
	@param sc Magic SCRIPT_CACHE value used by the Uniscribe system calls, associated.
----------------------------------------------------------------------------------------------*/
void UniscribeSegment::FwScriptCache::StoreScriptCache(UniscribeRunInfo & uri)
{
	Assert(uri.sc);
	if (uri.sc == NULL)
		return;			// Don't bother storing NULLs.

	int nType = ::GetDeviceCaps(uri.hdc, TECHNOLOGY);
	if (nType == DT_RASDISPLAY)
	{
		// When we're back to the video display, forget everything stored for another device.
		if (m_hmchrpscOther.Size())
			ResetScriptCacheMap(m_hmchrpscOther);

		SCRIPT_CACHE sc0;
		if (m_hmchrpsc.Retrieve(*uri.pchrp, &sc0))
		{
			// QUESTION: ThrowNice instead of Assert()ing and then storing?
			Assert(sc0 == uri.sc);
			if (sc0 != uri.sc) // For release build handle with most hopeful approach.
			{
				DISABLE_MULTISCRIBE
				{
					::ScriptFreeCache(&sc0);
				}
				m_hmchrpsc.Insert(*uri.pchrp, uri.sc, true);
			}
		}
		else
		{
			m_hmchrpsc.Insert(*uri.pchrp, uri.sc);
		}
	}
	else
	{
		SCRIPT_CACHE sc0;
		if (m_hmchrpscOther.Retrieve(*uri.pchrp, &sc0))
		{
			// QUESTION: ThrowNice instead of Assert()ing and then storing?
			Assert(sc0 == uri.sc);
			if (sc0 != uri.sc) // For release build handle with most hopeful approach.
			{
				DISABLE_MULTISCRIBE
				{
					::ScriptFreeCache(&sc0);
				}
				m_hmchrpscOther.Insert(*uri.pchrp, uri.sc, true);
			}
		}
		else
		{
			m_hmchrpscOther.Insert(*uri.pchrp, uri.sc);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Return the SCRIPT_CACHE value associated with the LgCharRenderProps, or NULL if it's
	not found.

	@param chrp Character properties possibly associated with a SCRIPT_CACHE value.

	@return Magic SCRIPT_CACHE value used by the Uniscribe system calls, or NULL if chrp is
				not found.
----------------------------------------------------------------------------------------------*/
SCRIPT_CACHE UniscribeSegment::FwScriptCache::FindScriptCache(UniscribeRunInfo & uri)
{
	SCRIPT_CACHE sc;
	static Rect g_storeDpiSrc;
	static Rect g_storeDpiDst;
	static HDC g_hdc;

	int nType = ::GetDeviceCaps(uri.hdc, TECHNOLOGY);
	if (nType == DT_RASDISPLAY)
	{
		// When we're back to the video display, forget everything stored for another device.
		if (m_hmchrpscOther.Size())
			ResetScriptCacheMap(m_hmchrpscOther);

		// If the resolution has changed then reset the cache. It also seems to be necessary to
		// reset it for a different DC...check TE's Print Layout view, or something else that
		// makes extensive use of two resolutions...when it goes wrong, text typically starts
		// drawing with very large spacing between letters. Failures are common if we only check
		// the source resolution, fairly rare if we check both resolutions but not the DC, and
		// seem not to occur if we check all three.
		if (g_storeDpiSrc.Size() != uri.rcSrc.Size() || g_storeDpiDst.Size() != uri.rcDst.Size() ||
			g_hdc != uri.hdc)
		{
			ResetScriptCacheMap(m_hmchrpsc);
			g_storeDpiSrc = uri.rcSrc;
			g_storeDpiDst = uri.rcDst;
			g_hdc = uri.hdc;
			return NULL; // no point in retrieving from empty cache
		}

		if (m_hmchrpsc.Retrieve(*uri.pchrp, &sc))
			return sc;
		else
		{
			CheckDuplicateMapping(uri, m_hmchrpsc);
			return NULL;
		}
	}
	else
	{
		if (m_hmchrpscOther.Retrieve(*uri.pchrp, &sc))
			return sc;
		else
		{
			CheckDuplicateMapping(uri, m_hmchrpscOther);
			return NULL;
		}
	}
}

/*--------------------------------------------------------------------------------------
	Delete all the stored SCRIPT_CACHE values for the video or "other" display.
--------------------------------------------------------------------------------------*/
void UniscribeSegment::FwScriptCache::ResetScriptCacheMap(
	HashMap<LgCharRenderProps, SCRIPT_CACHE>& hmchrpsc)
{
	DISABLE_MULTISCRIBE
	{
		HashMap<LgCharRenderProps, SCRIPT_CACHE>::iterator it;
		for (it = hmchrpsc.Begin(); it != hmchrpsc.End(); ++it)
		{
			SCRIPT_CACHE sc = it->GetValue();
			::ScriptFreeCache(&sc);
		}
	}
	hmchrpsc.Clear();
}

/*----------------------------------------------------------------------------------------------
	Check if a different hash entry has the same SCRIPT_CACHE value. If it has, we delete the
	script cache entry for that font.

	@param uri Run information with HDC and SCRIPT_CACHE
	@param hmchrpsc Hash map to check
----------------------------------------------------------------------------------------------*/
void UniscribeSegment::FwScriptCache::CheckDuplicateMapping(UniscribeRunInfo& uri,
	HashMap<LgCharRenderProps, SCRIPT_CACHE>& hmchrpsc)
{
	// Workaround for Uniscribe bug:
	// Some fonts sizes map to the same SCRIPT_CACHE value (e.g. Times New Roman 9+10 pt),
	// but we do the layout with the specified font sizes. To fix display problems we have to
	// reset the script cache for this sc before we render a run with this font.
	// This means we have to look if any other SCRIPT_CACHE value in our hash table
	// maps to the same uri.sc and delete the SCRIPT_CACHE value.
	// This fixes TE-3297.

	DISABLE_MULTISCRIBE
	{
		// Get SCRIPT_CACHE value. If it is not in the cache, it will be added now!
		long height;
		::ScriptCacheGetHeight(uri.hdc, &uri.sc, &height);

		// Loop through our cache and check if anything maps to the same SCRIPT_CACHE value
		// we just got.
		HashMap<LgCharRenderProps, SCRIPT_CACHE>::iterator it;
		for (it = hmchrpsc.Begin(); it != hmchrpsc.End(); ++it)
		{
			if (it->GetValue() == uri.sc)
			{
				::ScriptFreeCache(&uri.sc);
				hmchrpsc.Delete(it->GetKey());
				break;
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Return the number of glyphs that can be stretched. For now we assume that each
	stretchable glyph is basically stretched a consistent amount.
----------------------------------------------------------------------------------------------*/
int UniscribeSegment::NumStretchableGlyphs()
{
	int cStretchable = 0;
	for (int iglyph = 0; iglyph < m_cGlyphsInSeg; iglyph++)
	{
		if (m_pdxsAvailStretch[iglyph] > 0)
			cStretchable++;
	}
	return cStretchable;
}

/*----------------------------------------------------------------------------------------------
	Stretch the glyphs for a single run. Return the number of stretchable glyphs in this run.

	This method must be called whether we want justification or not (in order to get
	values into uri.prgJustAdv which is used for the Uniscribe API calls).

	Note that for now we ignore the actual (max) stretch value on the glyph, we just assume
	we can divide the needed amount among all the glyphs that are stretchable.

	TODO: See GrJustifier::DistributeRemainder for a better approach to handling rounding
	errors. The current approach makes them clump up rather than be distributed evenly.
----------------------------------------------------------------------------------------------*/
int UniscribeSegment::StretchGlyphs(UniscribeRunInfo & uri,
	int cStretchableGlyphs, int * pdxdStretchRemaining, int iglyphSeg)
{
	if (cStretchableGlyphs == 0 || *pdxdStretchRemaining == 0 || !m_pdxsAvailStretch)
	{
		memcpy(uri.prgJustAdv, uri.prgAdvance, (isizeof(int) * uri.cglyph));
		uri.dxdStretch = 0;
		return 0;
	}

	int cStillLeft = cStretchableGlyphs;

	int dxdThisRun = 0;
	for (int iglyphRun = 0; iglyphRun < uri.cglyph; iglyphRun++)
	{
		// The stretch types are in logical order, but the glyphs are in visual order.
		int iglyphVis = (uri.pchrp->fWsRtl) ? uri.cglyph - iglyphRun - 1 : iglyphRun;

		int dxd = 0;
		if (*pdxdStretchRemaining && m_pdxsAvailStretch[iglyphSeg + iglyphRun])
		{
			Assert(cStillLeft != 0);
			// It could be bad to stop stretching like this, but it almost certainly
			// beats dividing by 0.
			if (cStillLeft > 0)
			{
				dxd = (int) (((double)*pdxdStretchRemaining / (double)cStillLeft) + 0.5);
				*pdxdStretchRemaining -= dxd;
				dxdThisRun += dxd;
				cStillLeft--;
			}
		}
		uri.prgJustAdv[iglyphVis] = uri.prgAdvance[iglyphVis] + dxd;
	}
	uri.dxdStretch = dxdThisRun;
	return cStretchableGlyphs - cStillLeft;
}


#include "Vector_i.cpp"
template class Vector<OLECHAR>;
#if WIN32
template class Vector<WORD>;
#endif
template class Vector<SCRIPT_VISATTR>;
template class Vector<int>;
template class Vector<GOFFSET>;
template class Vector<SCRIPT_ITEM>; // ScrItemVec; // Hungarian vscri;
template class Vector<SCRIPT_LOGATTR>; // ScrLogAttrVec; // Hungarian vsla.

#include "HashMap_i.cpp"
template class HashMap<LgCharRenderProps, SCRIPT_CACHE>;
