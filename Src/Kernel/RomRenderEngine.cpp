/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: RomRenderEngine.cpp
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

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

RomRenderEngine::RomRenderEngine()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

RomRenderEngine::~RomRenderEngine()
{
	// TODO-Linux FWNX-408:
	// ILgWritingSystemFactory is now a C# object
	// RomRenderEngine isn't explicitly released by C#, meaning there can be close down issues
	m_qwsf.Detach();

	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language1.RomRenderEngine"),
	&CLSID_RomRenderEngine,
	_T("SIL Roman renderer"),
	_T("Apartment"),
	&RomRenderEngine::CreateCom);


void RomRenderEngine::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<RomRenderEngine> qrre;
	qrre.Attach(NewObj RomRenderEngine());		// ref count initialy 1
	CheckHr(qrre->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP RomRenderEngine::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IRenderEngine *>(this));
	else if (riid == IID_IRenderEngine)
		*ppv = static_cast<IRenderEngine *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<IRenderEngine *>(this),
			IID_ISimpleInit, IID_IRenderEngine);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   IRenderEngine methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Initialize the engine. This must be called before any oher methods of the interface.
	How the data is used is implementation dependent. The RomanRenderer does not
	use it at all.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderEngine::InitRenderer(IVwGraphics * pvg, BSTR bstrData)
{
	return S_OK;
}
/*----------------------------------------------------------------------------------------------
	Return an indication of whether the font is valid for the renderer.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderEngine::get_FontIsValid(ComBool * pfValid)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfValid);
	*pfValid = TRUE;
	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Give the maximum length of information that this renderer might want to pass
	from one segment to another in SimpleBreakPoint>>pbNextSegDat.
	RRS never passes info from one segment to another.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderEngine::get_SegDatMaxLength(int * cb)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(cb);
	*cb = 0;
	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Return the support script directions. For the Roman renderer, this is simply horizontal
	left-to-right.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderEngine::get_ScriptDirection(int * pgrfsdc)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pgrfsdc);
	*pgrfsdc = kfsdcHorizLtr;
	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Return the class ID for the implementation class.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderEngine::get_ClassId(GUID * pguid)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pguid);
	memcpy(pguid, &CLSID_RomRenderEngine, isizeof(GUID));
	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Make a segment by finding a suitable break point in the specified range of text.
	Note that it is appropriate for line layout to use this routine even if putting
	text on a single line, because an old writing system may take advantage of line layout
	to handle direction changes and style changes and generate multiple segments
	even on one line. For such layouts, pass a large dxMaxWidth, but still expect
	possibly multiple segments.
	Arguments:
		pgjus				NULL if no justification will ever be needed for the resulting segment
		ichMinNew			index of the first char in the text that is of interest
		ichLimText			index of the last char in the text that is of interest (+ 1)
		ichLimBacktrack		index of last char that may be included in the segment.
							Generally the same as ichLimText unless backtracking.
		fNeedFinalBreak
		fStartLine			seg is logically first on line?
		dxMaxWidth			whatever coords pvg is using
		lbPref				try for longest seg of this weight
		lbMax				max if no preferred break possible
		twsh				how we are handling trailing white-space
		fParaRtoL			overall paragraph direction
		ppsegRet			segment produced, or null if nothing fits
		pdichLimSeg			offset to last char of segment, first of next if any
		pdxWidth			of new segment, if any
		pest				what caused the segment to end?
		cbPrev				(not used)
		pbPrevSegDat		(not used)
		cbNextMax			(not used)
		pbNextSegDat		(not used)
		pcbNextSegDat		(*pcbNextSegDat always set to zero)
		pdichContext		(*pdichContext always set to zero)

	TODO 1441 (SharonC): handle fParaRtoL; specifically, if the paragraph direction is
	right-to-left, trailing white-space characters should be reversed.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderEngine::FindBreakPoint(
	IVwGraphics * pvg, IVwTextSource * pts, IVwJustifier * pvjus,
	int ichMinNew, int ichLimText, int ichLimBacktrack,
	ComBool fNeedFinalBreak, ComBool fStartLine,
	int dxMaxWidth, LgLineBreak lbPref, LgLineBreak lbMax,
	LgTrailingWsHandling twsh, ComBool fParaRtoL,
	ILgSegment ** ppsegRet, int * pdichLimSeg, int * pdxWidth, LgEndSegmentType * pest,
	ILgSegment * psegPrev)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);
	ChkComArgPtr(pts);
	ChkComOutPtr(ppsegRet);
	ChkComOutPtr(pdichLimSeg);
	ChkComArgPtr(pdxWidth);
	ChkComArgPtr(pest);
	ChkComArgPtrN(psegPrev);
#define INIT_BUF_SIZE 1000
	OLECHAR rgchBuf[INIT_BUF_SIZE]; // Unlikely segments are longer than this...
	memset(rgchBuf, 0, sizeof(rgchBuf));
	Vector<OLECHAR> vch; // Use as buffer if 1000 is not enough
	OLECHAR * prgch = rgchBuf; // Use on-stack variable if big enough
	int cchBuf = INIT_BUF_SIZE; // chars available in prgch; INIT_BUF_SIZE or vch.Size().

	int ichForceBreak;
	byte rglbsBuf[INIT_BUF_SIZE]; // line break status
	Vector<byte> vlbs;
	byte * prglbsForString = rglbsBuf; // switch to resized vector if needed.

	RomRenderSegmentPtr qrrs;
	LgCharRenderProps chrp;
	int ichMinRun, ichLimRun;
	CheckHr(pts->GetCharProps(ichMinNew, &chrp, &ichMinRun, &ichLimRun));
	// Ws and old writing system for the segment; don't use chars with different ones.
	int ws = chrp.ws;
	int nDirDepth = chrp.nDirDepth;
	if (fParaRtoL)
		nDirDepth += 2;
	//Assert((nDirDepth % 2) == 0); // left-to-right

	// Get a char props engine
	AssertPtr(m_qwsf);
	ILgCharacterPropertyEnginePtr qcpe;
	CheckHr(m_qwsf->get_CharPropEngine(ws, &qcpe));

// The maximum number of characters we will fetch and measure, beyond what we
// know we need. This should be less than the length of rgchBuf.
#define MAX_MEASURE 100
	EndAvailType eat;
	int ichLimSegCur; // Current proposal for where seg might end
	// We aren't allowed to include characters at or beyond ichLimBacktrack in the
	// segment. But it's worth getting one more, if more are available, so we can
	// check whether an line break at the very end is allowed.
	// Note that this time we're getting at most MAX_MEASURE chars, so we don't
	// have to check for buffer overflow.
	GetAvailChars(pts, ws, ichMinNew, ichLimBacktrack, MAX_MEASURE, twsh, qcpe,
		prgch, &ichLimSegCur, &eat);

	// Measure all the characters we got, or all we are allowed to use, whichever
	// is less.
	int cchMeasure = ichLimSegCur - ichMinNew;
	// Make a segment to use in measuring things; if all goes well use it for the
	// final result.
	qrrs.Attach(NewObj RomRenderSegment(pts, this, cchMeasure,
		klbNoBreak, klbNoBreak, true));
	qrrs->SetDirectionInfo(nDirDepth, (twsh == ktwshOnlyWs));

	// If a forced empty segment, stop here
	if (ichLimBacktrack <= ichMinNew)
	{
		*pest = kestNoMore;
		*pdxWidth = 0;
		*ppsegRet = qrrs.Detach();
		*pdichLimSeg = 0;
		return S_OK;
	}

	int dxWidthMeasure = 0; // width of part of run initially measured.

	CheckHr(qrrs->get_Width(ichMinNew, pvg, &dxWidthMeasure));

	int cchLineEst; // Calculate the estimated number of characters to fill line.
	int cchMaxText = ichLimBacktrack - ichMinNew;
#ifdef ICU_LINEBREAKING
	int ichTempBreak;
	LgLineBreak lbWeight;
	LgGeneralCharCategory cc;
#endif /*ICU_LINEBREAKING*/

	// Now, if it is a short text or already contains all candidate characters,
	// we have the actual width; otherwise, we have
	// a measurement of MAX_MEASURE characters that we can use in guessing how many we
	// need. If we have less than we need, increase the limit until we have more.
	// Note that we need the <= here because, if we have exactly filled the width, we
	// will consider putting a break after the MAX_MEASURE'th character. We need to
	// fetch at least one more character in order to have valid line-break info
	// about the last one that might be put on the line.
	while (eat == keatMax && ichLimSegCur < ichLimBacktrack && dxWidthMeasure <= dxMaxWidth)
	{
		// Compute the number we estimate will fill the line (min the number we have).
		// Don't let this estimate be zero unless we actually have no characters available.
		if (!dxWidthMeasure)
			cchLineEst = min(max(1, dxMaxWidth * cchMeasure), cchMaxText);
		else
			// Use MulDiv to avoid overflow, likely when dxMaxWidth is INT_MAX
			cchLineEst = min(max(1, MulDiv(dxMaxWidth, cchMeasure, dxWidthMeasure)), cchMaxText);
		// Make sure the buffer contains MAX_MEASURE more than that. First make sure
		// there is room for them.
		// We have already loaded out to ichLimSegCur, so use that as a start position.
		// Already in the buffer are pts[ichMinNew...ichLimSegCur] starting at prgch.
		// We need room for cchLineEst + MAX_MEASURE, but add a few more to buffer size
		// so we are always safe adding one or two for end of line testing.
		if (cchLineEst + MAX_MEASURE + 5 >= cchBuf)
		{
			// Allocate memory for the characters in the vector, or resize it.
			// Allocate extra in case we keep adding later.
			cchBuf = cchLineEst + MAX_MEASURE + 500;
			vch.Resize(cchBuf);
			if (prgch == rgchBuf)
				MoveItems(rgchBuf, vch.Begin(), ichLimSegCur - ichMinNew);
			prgch = vch.Begin();
			vlbs.Clear();  // no need to copy, nothing in it yet.
			vlbs.Resize(cchBuf);
			prglbsForString = vlbs.Begin();
		}
		GetAvailChars(pts, ws, ichLimSegCur, ichLimBacktrack,
			ichMinNew + cchLineEst + MAX_MEASURE - ichLimSegCur, twsh, qcpe,
			prgch + ichLimSegCur - ichMinNew, &ichLimSegCur, &eat);
		cchMeasure = ichLimSegCur - ichMinNew;
		qrrs->SetLim(cchMeasure);
		qrrs->SetDirectionInfo(nDirDepth, (twsh == ktwshOnlyWs));
		CheckHr(qrrs->get_Width(ichMinNew, pvg, &dxWidthMeasure));
	}

#ifdef ICU_LINEBREAKING
	//updating the BreakIterator text for calculating line breaks later
	CheckHr(qcpe->put_LineBreakText(prgch, ichLimSegCur-ichMinNew));
#endif /*ICU_LINEBREAKING*/

	// Update this with the results of the latest measurement.
	if (!dxWidthMeasure)
		cchLineEst = min(max(1, dxMaxWidth * cchMeasure), cchMaxText);
	else
		// Use MulDiv to avoid overflow, likely when dxMaxWidth is INT_MAX
		cchLineEst = min(max(1, MulDiv(dxMaxWidth, cchMeasure, dxWidthMeasure)), cchMaxText);
	// Now we have measured either all the characters we are allowed, or enough to
	// fill the line.

	if (dxWidthMeasure <= dxMaxWidth)
	{
		// we will most likely answer the segment we just found
		*pdxWidth = dxWidthMeasure;
		*pdichLimSeg = min(ichLimSegCur - ichMinNew, ichLimBacktrack - ichMinNew);
		if (ichLimSegCur == ichLimText)
		{
			// the whole text we were asked to use fit
			*pest = kestNoMore;
			*ppsegRet = qrrs.Detach();
			return S_OK;
		}
		if (ichLimSegCur == ichLimBacktrack)
		{
			// Everything allowed fits, but, since this is not the real end of the text,
			// we have to consider whether this is a valid line break. We will need one
			// more character in the buffer. We made sure above there is room for it.
			CheckHr(pts->Fetch(ichLimSegCur, ichLimSegCur + 1, prgch + ichLimSegCur - ichMinNew));
			// We want to get line break info for the character at ichLimSegCur - 1, relative to
			// the string as a whole. Offset relative to buffer is less by ichMinNew.
			int ichBuf = ichLimSegCur - ichMinNew - 1;
#ifndef ICU_LINEBREAKING
			CheckHr(qcpe->GetLineBreakInfo(prgch, ichLimSegCur - ichMinNew + 1,
				ichBuf, ichBuf + 1, prglbsForString, &ichForceBreak));
#else
			//updating the BreakIterator text for calculating line breaks
			CheckHr(qcpe->put_LineBreakText(prgch, ichLimSegCur-ichMinNew+1));
			CheckHr(qcpe->LineBreakAfter(ichBuf, &ichTempBreak, &lbWeight));
// Previous code was wrong twice:
// 1. did not offset by ichMinNew
// 2. did not allow GetLineBreakInfo to use info about characters earlier in run.
// Also it got one more result than we need.
//			CheckHr(qcpe->GetLineBreakInfo(prgch + ichLimSegCur - 1, 2, 0, 2, prglbsForString,
//				&ichForceBreak));
			CheckHr(qcpe->get_GeneralCategory(prgch[ichBuf], &cc));
			if ((ichTempBreak == ichBuf+1) && (cc == kccZs))
#endif /*ICU_LINEBREAKING*/
#ifndef ICU_LINEBREAKING
			if (prglbsForString[0] & kflbsBrk)
#endif /*ICU_LINEBREAKING*/
			{
				// Backtrack posn is also a line break
				*pest = kestOkayBreak;
				*ppsegRet = qrrs.Detach();
				return S_OK;
			}
			else if (!fNeedFinalBreak)
			{
				// Backtrack position is not a break, but we may return it anyway
				// This probably never happens but include it for consistency.
				*pest = kestBadBreak;
				*ppsegRet = qrrs.Detach();
				return S_OK;
			}
			// If we get here we must search for an earlier break.
			goto LFindEarlierBreak;
		}
		if (eat == keatBreak || eat == keatOnlyWs)
		{
			*pest = (eat == keatOnlyWs) ? kestOkayBreak : kestHardBreak;
			// a segment up to a hard return or similar fit
			// Return the segment we got (which may be an empty segment)
			*ppsegRet = qrrs.Detach();
			return S_OK;
		}
		// Only one reason remains for us to have stopped while not filling the width
		Assert(eat == keatNewWs);
		// See whether the WS break is also a line break.
		// We want to get line break info for the character at ichLimSegCur - 1, relative to
		// the string as a whole. Offset relative to buffer is less by ichMinNew.
		int ichBuf = ichLimSegCur - ichMinNew - 1;
#ifndef ICU_LINEBREAKING
		CheckHr(qcpe->GetLineBreakInfo(prgch, ichLimSegCur - ichMinNew,
			ichBuf, ichBuf + 1, prglbsForString, &ichForceBreak));
#else
		CheckHr(qcpe->LineBreakAfter(ichBuf, &ichTempBreak, &lbWeight));

		CheckHr(qcpe->get_GeneralCategory(prgch[ichBuf], &cc));
		if ((ichTempBreak == ichBuf+1) && (cc == kccZs))
#endif /*ICU_LINEBREAKING*/
#ifndef ICU_LINEBREAKING
		if (prglbsForString[0] & kflbsBrk)
#endif /*ICU_LINEBREAKING*/
		{
			// WS break is also a line break
			*pest = kestOkayBreak;
			*ppsegRet = qrrs.Detach();
			return S_OK;
		}
		else
		{
			if (!fNeedFinalBreak)
			{
				// Though not a line break, return it and see if we can fit some
				// of next WS on to make things OK.
				*pest = kestWsBreak;
				*ppsegRet = qrrs.Detach();
				return S_OK;
			}
			// otherwise, fall through to find a valid line break earlier.
		}
	}

LFindEarlierBreak:
	// ENHANCE JohnT: arguably we ought to look further back to be absolutely sure
	// of line break props. It makes a difference only if old writing system changes
	// in middle of a run of spaces or combining marks, and even then, the extra spaces
	// will fit so we shouldn't make a spurious break, and the run of CMs should only
	// get broken if the column is too narrow for a whole word. I think we can live
	// wit this.
	// Note: we can ignore ichForceBreak here because we made sure in GetAvailChars
	// that there aren't any break characters before ichLimSegCur.
	CheckHr(qcpe->GetLineBreakInfo(prgch, ichLimSegCur - ichMinNew, 0,
		ichLimSegCur - ichMinNew, prglbsForString, &ichForceBreak));

	// If we got here, we will have to make a line break within the run, somewhere
	// before ichLimSegCur, since stopping there either makes it too wide, or violates
	// fNeedFinalBreak.
	*pest = kestMoreLines;

	// broken segment will certainly end line
	CheckHr(qrrs->put_EndLine(ichMinNew, pvg, true));

	// Get info about line break possibilities

	// Figure the best kind of line break we can do at all,
	// starting at lbPref
	LgLineBreak lbTry;
	lbTry = lbPref;	// sep from decl because of goto
	int ichBreak;	// index in prglbsForString of character after which break is allowed at
					// given level
	int ichDim;		// index in prglbsForString of last character before ichBreak which is
					// not a space,
	ichBreak = -1;	// don't init in decl because of goto
	int dxBreakWidth;
	// loop over possible levels of badness of break, from lbPref to lbMax.
	for (;;)
	{
		if (!(lbTry == klbHyphenBreak && lbTry != lbPref))
		{
			// Look for a break at this level. The condition above means
			// don't try hyphen break if we already tried word break.
			// This engine can't (yet) find a hyphen break that is not
			// a word break.
#ifndef ICU_LINEBREAKING
			FindLineBreak(prglbsForString, 0, ichLimSegCur - ichMinNew, lbTry, false,
				ichBreak, ichDim);
#else
			FindLineBreak(prgch, qcpe, 0, ichLimSegCur - ichMinNew, lbTry, false,
				ichBreak, ichDim);
#endif /*ICU_LINEBREAKING*/
			if (ichBreak >= 0)
			{
				if (ichDim < 0)
					break;		// all characters up to break are spaces
				qrrs->SetLim(ichDim + 1);
				qrrs->SetDirectionInfo(nDirDepth, (twsh == ktwshOnlyWs));
				CheckHr(qrrs->get_Width(ichMinNew, pvg, &dxBreakWidth));
				if (dxBreakWidth > dxMaxWidth && lbTry < klbClipBreak)
				{
					// can't do this kind of break
					ichBreak = -1;
				}
			}
			if (ichBreak >= 0)
				break;		// found a useable break at this level
		}
		lbTry = (LgLineBreak) (lbTry + 1);	// otherwise try next level.
		if (lbTry > lbMax)
		{
			// can't get any valid break. Give up.
			*ppsegRet = NULL;
			*pdxWidth = 0;
			return S_OK;  // this is a perfectly valid result.
		}
		Assert(lbTry <= klbClipBreak); // that level should always succeed
	}
	// OK, we are going to put something on the line. The break type will be lbTry.
	// See if we can find any later breaks of the same type that fit.

	int ichNewBreak;		// in prglbsForString
	int ichNewDim = -1;		// in prglbsForString
	int dxNewBreakWidth;

	// 1. look backward for first break prior to cchLineEst
	// 2. while break doesn't fit, look backward for next prior break
	// 3. once a break fits, quit and use it.
	// 4. while break fits, remember it and look forward for next following break
	// 5. once a break doesn't fit, quit and use the previous break.

	bool fLookAhead = true;
	if (cchLineEst > ichBreak)
#ifndef ICU_LINEBREAKING
		FindLineBreak(prglbsForString, ichBreak + 1, cchLineEst, lbTry, true, ichNewBreak,
			ichNewDim);
#else
		FindLineBreak(prgch, qcpe, ichBreak + 1, cchLineEst, lbTry, true, ichNewBreak,
			ichNewDim);
#endif /*ICU_LINEBREAKING*/
	else
		ichNewBreak = 0;
	if (ichNewBreak >= 0)
	{
		qrrs->SetLim(ichNewDim + 1);
		qrrs->SetDirectionInfo(nDirDepth, (twsh == ktwshOnlyWs));
		CheckHr(qrrs->get_Width(ichMinNew, pvg, &dxNewBreakWidth));
		if (dxNewBreakWidth > dxMaxWidth)
		{
			fLookAhead = false;
			for (;;)
			{
#ifndef ICU_LINEBREAKING
				FindLineBreak(prglbsForString, ichBreak + 1, ichNewDim, lbTry, true,
					ichNewBreak, ichNewDim);
#else
				FindLineBreak(prgch, qcpe, ichBreak + 1, ichNewDim, lbTry, true,
					ichNewBreak, ichNewDim);
#endif /*ICU_LINEBREAKING*/
				if (ichNewBreak < 0)
					break;		// no more breaks possible: use the first one found above
				qrrs->SetLim(ichNewDim + 1);
				qrrs->SetDirectionInfo(nDirDepth, (twsh == ktwshOnlyWs));
				CheckHr(qrrs->get_Width(ichMinNew, pvg, &dxNewBreakWidth));
				if (dxNewBreakWidth <= dxMaxWidth)
				{
					ichBreak = ichNewBreak;
					ichDim = ichNewDim;
					break;
				}
			}
		}
	}
	else
	{
		ichNewBreak = ichBreak;
		ichNewDim = ichDim;
	}
	if (fLookAhead)
	{
		ichBreak = ichNewBreak;
		ichDim = ichNewDim;
		for (;;)
		{
#ifndef ICU_LINEBREAKING
			FindLineBreak(prglbsForString, ichBreak + 1, ichLimSegCur - ichMinNew, lbTry,
				false, ichNewBreak, ichNewDim);
#else
			FindLineBreak(prgch, qcpe, ichBreak + 1, ichLimSegCur - ichMinNew, lbTry,
				false, ichNewBreak, ichNewDim);
#endif /*ICU_LINEBREAKING*/
			if (ichNewBreak < 0)
				break;  // no more breaks possible
			qrrs->SetLim(ichNewDim + 1);
			qrrs->SetDirectionInfo(nDirDepth, (twsh == ktwshOnlyWs));
			CheckHr(qrrs->get_Width(ichMinNew, pvg, &dxNewBreakWidth));
			if (dxNewBreakWidth > dxMaxWidth)
			{
				// can't go any further; reset seg to prev break and stop
				break;
			}
			// OK, the new break works, make it current and try again
			ichBreak = ichNewBreak;
			ichDim = ichNewDim;
		}
	}
//- ORIGINAL CODE THAT ONLY LOOKED FORWARD.
//-		for (;;)
//-		{
//-			FindLineBreak(prglbsForString, ichBreak + 1, ichLimSegCur - ichMinNew, lbTry, false,
//-				ichNewBreak, ichNewDim);
//-			++cOld;
//-			if (ichNewBreak < 0)
//-				break;  // no more breaks possible
//-			qrrs->SetLim(ichNewDim + 1);
//-			qrrs->SetDirectionInfo(nDirDepth, (twsh == ktwshOnlyWs));
//-			CheckHr(qrrs->get_Width(ichMinNew, pvg, &dxNewBreakWidth));
//-			if (dxNewBreakWidth > dxMaxWidth)
//-			{
//-				// can't go any further; reset seg to prev break and stop
//-				break;
//-			}
//-			// OK, the new break works, make it current and try again
//-			ichBreak = ichNewBreak;
//-			ichDim = ichNewDim;
//-		}

	//	If needed, strip off trailing white-space.
	if (twsh == ktwshNoWs)
	{
		while (ichBreak >= 0)
		{
			LgBidiCategory bic;
			CheckHr(qcpe->get_BidiCategory(prgch[ichBreak], &bic));
			if (bic != kbicWS)
				break;
			ichBreak--;
			*pest = kestMoreWhtsp;  // we can fit more white space on the line
		}
		if (ichBreak < 0)
		{	// all we found was white-space
			*pest = kestMoreWhtsp;
			*pdxWidth = 0;
			*ppsegRet = NULL;
			*pdichLimSeg = 0;
			return S_OK;
		}
	}

	qrrs->SetLim(ichBreak + 1);
	qrrs->SetDirectionInfo(nDirDepth, (twsh == ktwshOnlyWs));
	CheckHr(qrrs->get_Width(ichMinNew, pvg, pdxWidth)); // not inc. trail spcs, endline true
	*ppsegRet = qrrs.Detach();
	*pdichLimSeg = ichBreak + 1;

	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Figure a sequence of characters that are available to use in building a segment.
	The first character we have not already tested as OK is at ichMin;
	the last that the client says we can use is ichLimSeg;
	for now we want at most cchMax characters;
	and we don't want any hard break characters (currently \t, \n, \r, 0xfffc (object),
	0x2028 (hard line break)).
	If we are creating a white-space-only segment, stop at the first non-white-space.
	Fetch the actual characters into prgch.
	Set *pichLimSegCur to indicate the lim of chars actually installed.
----------------------------------------------------------------------------------------------*/
void RomRenderEngine::GetAvailChars(IVwTextSource * pts,int ws,
	int ichMin, int ichLimSeg, int cchMax,
	LgTrailingWsHandling twsh, ILgCharacterPropertyEngine * pcpe,
	OLECHAR * prgch, int * pichLimSegCur, EndAvailType * peat)
{
	LgCharRenderProps chrp;
	OLECHAR * pch = prgch;
	int ichMinRun, ichLimRun;
	int ichLimSegCur = ichMin; // starting point: can't use any
	for (; ;)
	{
		// if we have reached the client-defined limit return it
		if (ichLimSegCur == ichLimSeg)
		{
			*peat = keatLim;
			*pichLimSegCur = ichLimSegCur;
			goto LReturn;
		}
		// if we have reached the number of chars we want return
		if (ichLimSegCur == ichMin + cchMax)
		{
			*peat = keatMax;
			*pichLimSegCur = ichLimSegCur;
			goto LReturn;
		}
		CheckHr(pts->GetCharProps(ichLimSegCur, &chrp, &ichMinRun, &ichLimRun));
		Assert(ichLimRun > ichLimSegCur); // ensure some progress
		// We can't use characters in a different old writing system.
		if (chrp.ws != ws)
		{
			*peat = keatNewWs;
			*pichLimSegCur = ichLimSegCur;
			goto LReturn;
		}
		// We aren't interested in characters before ichMin.
		if (ichMinRun < ichMin)
			ichMinRun = ichMin;
		// We can't use characters beyond ichLimSeg.
		if (ichLimRun > ichLimSeg)
			ichLimRun = ichLimSeg;
		// And should not use more than cchMax.
		if (ichLimRun - ichMin > cchMax)
			ichLimRun = ichMin + cchMax;
		// Now check whether any of those characters are hard-breaks. Also, if we are only
		// interested in white-space and we find something else, stop at that point.
		CheckHr(pts->Fetch(ichMinRun, ichLimRun, pch));
		OLECHAR *pchLim = prgch + (ichLimRun - ichMin); // need parens for BoundsChecker
		for (; pch < pchLim; pch++)
		{
			if (*pch == '\n' || *pch == '\t' || *pch == '\r' || *pch == 0xfffc || *pch == 0x2028)
			{
				*peat = keatBreak;
				*pichLimSegCur = ichMin + pch - prgch;
				goto LReturn;
			}
			if (twsh == ktwshOnlyWs)
			{
				// TODO 1441 (SharonC): if the overall paragraph direction is right-to-left,
				// the space characters should be reversed.
				LgBidiCategory bic;
				CheckHr(pcpe->get_BidiCategory(*pch, &bic));
				if (bic != kbicWS)
				{
					*peat = keatOnlyWs;
					*pichLimSegCur = ichMin + pch - prgch;
					goto LReturn;
				}
			}
		}

		// OK, all chars in this run are OK. Update limit of useful chars,
		// and either loop for next seg, or stop with tests at start of loop
		ichLimSegCur = ichLimRun;
	}

LReturn:
	int ichTextLen;
	CheckHr(pts->get_Length(&ichTextLen));

	// The following flags are not currently being used, but they may eventually come in handy:
	if (*pichLimSegCur >= ichTextLen)
	{
		m_fMoreText = false;
		m_fNextIsSameWs = false;
	}
	else
	{
		m_fMoreText = true;
		int ichMinDum, ichLimDum;
		CheckHr(pts->GetCharProps(*pichLimSegCur, &chrp, &ichMinDum, &ichLimDum));
		m_fNextIsSameWs = (chrp.ws == ws);
	}
}

/*----------------------------------------------------------------------------------------------
	Find a line break of the required type within the given range of characters, using the Line
	Break Status array corresponding to the current string.

	Returns the index of the first character after which a line break of the required type is
	possible. Returns -1 if no such break is possible.

	If a break is possible, also returns the index of the last character before the break which
	is not a space (i.e. does not have the Unicode 'SP' line breaking property). This is useful
	for computing the dimensions of the segment up to the line break.

	This is not an interface method, since it is called only within this module.
----------------------------------------------------------------------------------------------*/
void RomRenderEngine::FindLineBreak(

#ifndef ICU_LINEBREAKING
	const byte * prglbs,		// line break status array for string in which range lies
#else
	const OLECHAR * prgch,
	const ILgCharacterPropertyEnginePtr qcpe,
#endif /*ICU_LINEBREAKING*/
	const int ichMin,			// index of first character of string to be considered
	const int ichLim,			// index of (last+1) character of string to be considered
	const LgLineBreak lbrkRequired,	// type of line break required (word, hyphen, letter etc)
	const bool fBackFromEnd,	// flag whether to search backward from the end for line break
	int & ichBreak,		// (out) index of first char after which break is possible (-1 if none)
	int & ichDim)		// (out) index of latest non-space character (-1 if none)
{
#ifndef ICU_LINEBREAKING
	AssertPtrSize(prglbs, ichLim); // (this includes asserting ichLim >= 0)
#else
	AssertPtrSize(prgch, ichLim);
	LgLineBreak lbWeight;
	LgGeneralCharCategory cc;
#endif /*ICU_LINEBREAKING*/
	Assert(ichMin >= 0);
	ichBreak = -1;
	ichDim = -1;
	int ich;
	Assert(ichMin <= ichLim);

	if (ichMin >= ichLim)
	{
		return;
	}
#ifndef ICU_LINEBREAKING
	switch (lbrkRequired)
	{
	case klbWsBreak:
	case klbWordBreak:
	case klbHyphenBreak:
		// Note that at present we do not handle hyphenation, so breaks of this kind are
		// typically between words.
		if (fBackFromEnd)
		{
			for (ich = ichLim; --ich >= ichMin;)
			{
				if (prglbs[ich] & kflbsBrk)
				{
					ichBreak = ich;
					break;
				}
			}
		}
		else
		{
			for (ich = ichMin; ich < ichLim; ++ich)
			{
				if (prglbs[ich] & kflbsBrk)
				{
					ichBreak = ich;
					break;
				}
			}
		}
		break;
	case klbLetterBreak:
		if (fBackFromEnd)
		{
			for (ich = ichLim; --ich >= ichMin;)
			{
				if (prglbs[ich] & (kflbsBrkL | kflbsBrk))
				{
					ichBreak = ich;
					break;
				}
			}
		}
		else
		{
			for (ich = ichMin; ich < ichLim; ++ich)
			{
				if (prglbs[ich] & (kflbsBrkL | kflbsBrk))
				{
					ichBreak = ich;
					break;
				}
			}
		}
		break;
	case klbClipBreak:
		// Simply find the index of the first non-space, or the last character if all spaces.
		// (This is the same for either value of fBackFromEnd.)
		for (ich = ichMin; ich < ichLim; ++ich)
		{
			if (!(prglbs[ich] & kflbsSpace))
			{
				ichBreak = ich;
				break;
			}
		}
		if (ichBreak < 0)
			ichBreak = ichLim - 1;
		break;
	default:
		ThrowInternalError(E_INVALIDARG, "Invalid line break type");
	}
#else
	switch (lbrkRequired)
	{
	case klbNoBreak:
		return;
		break;
	case klbWsBreak:
	case klbWordBreak:
	case klbGoodBreak:
	case klbHyphenBreak:
		//Right now we don't handle automatic hyphenation, so this case will find
		//hyphens already present along with typical word breaks.
		if (fBackFromEnd)
		{
			qcpe->LineBreakBefore(ichLim, &ichBreak, &lbWeight);
			if (--ichBreak < ichMin)
				ichBreak = -1;
		}
		else
		{
			qcpe->LineBreakAfter(ichMin, &ichBreak, &lbWeight);
			if (--ichBreak >= ichLim)
				ichBreak = -1;
		}
		break;
	case klbLetterBreak:
		if (fBackFromEnd)
			ichBreak = ichLim-1;
		else
			ichBreak = ichMin;
		break;
	case klbClipBreak:
		for (ich = ichMin; ich < ichLim; ich++)
		{
			qcpe->get_GeneralCategory(prgch[ich], &cc);
			if (cc != kccZs)
			{
				ichBreak = ich;
				break;
			}
		}
		if (ichBreak < 0)
			ichBreak = ichLim - 1;
		break;
	default:
#if !WIN32
		// as the enums are not numbered sequencialy. given how this method is called
		// we will receive invalid enum vals.
		// possibly because of the use of RomRender
		return;
#else
		ThrowInternalError(E_INVALIDARG, "Invalid line break type");
#endif
	}
#endif /*ICU_LINEBREAKING*/
	if (ichBreak >= 0)
	{
		// a break point was found
#ifndef ICU_LINEBREAKING
		for (ichDim = ichBreak; ichDim >= ichMin; --ichDim)
		{
			if (!(prglbs[ichDim] & kflbsSpace))
				break;
		}
#else
		for (ichDim = ichBreak; ichDim >= ichMin; ichDim--)
		{
			qcpe->get_GeneralCategory(prgch[ichDim], &cc);
			if (cc != kccZs)
				break;
		}
#endif /*ICU_LINEBREAKING*/
		if (ichDim < ichMin)
			ichDim = -1;	// all chars up to and including the break point were spaces
	}
}

/*----------------------------------------------------------------------------------------------
	Return the writing system factory for this database (or the registry, as the case may be).

	@param ppwsf Address of the pointer for returning the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderEngine::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppwsf);

	*ppwsf = m_qwsf;
	if (*ppwsf)
		(*ppwsf)->AddRef();

	END_COM_METHOD(g_fact, IID_IRenderEngine)
}

/*----------------------------------------------------------------------------------------------
	Set the writing system factory for this database (or the registry, as the case may be).

	@param pwsf Pointer to the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RomRenderEngine::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD
	ChkComArgPtrN(pwsf);

	m_qwsf = pwsf;

	END_COM_METHOD(g_fact, IID_IRenderEngine)
}
