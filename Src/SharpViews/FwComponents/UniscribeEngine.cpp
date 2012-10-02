/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UniscribeEngine.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

POSSIBLE OPTIMIZATION HINT:
	One thing you may want to consider in your optimization efforts, if you haven't already, is
	making use of accurate information (if you can find it) about the bounding rectangle that
	really needs to be drawn.  I think that both when AfVwWnd::OnPaint constructs an off-screen
	bitmap, and when VwGraphics figures the default clipping rectangle, they are just using the
	window size.  In many cases, especially while typing, it should be possible to do much
	better than this.  (Beware, though, that the default clipping rectangle that Windows gives
	when our code has not asked explicitly for clipping is either empty or huge...I forget
	which...but anyway not useful.  However I eventually found, I think, another function that
	gives an accurate boundary of the part of the window being redrawn, as limited by what is
	invalid and what is covered by other windows.)  - JohnT
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
static UniscribeRunInfo g_uri; // information about a run.

DisableMultiscribe::SetMultiscribeEnabledFunc DisableMultiscribe::s_setMultiscribeEnabled = NULL;
bool DisableMultiscribe::s_multiscribeHandleRetrieved = false;

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

UniscribeEngine::UniscribeEngine()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	StrUtil::InitIcuDataDir();
}

UniscribeEngine::~UniscribeEngine()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language1.UniscribeEngine"),
	&CLSID_UniscribeEngine,
	_T("SIL Uniscribe wrapper"),
	_T("Apartment"),
	&UniscribeEngine::CreateCom);


void UniscribeEngine::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<UniscribeEngine> qrre;
	qrre.Attach(NewObj UniscribeEngine());		// ref count initialy 1
	CheckHr(qrre->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP UniscribeEngine::QueryInterface(REFIID riid, void **ppv)
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
	How the data is used is implementation dependent. The UniscribeRenderer does not
	use it at all.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeEngine::InitRenderer(IVwGraphics * pvg, BSTR bstrData)
{
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Return an indication of whether the font is valid for the renderer.
	S_OK means it is valid, E_FAIL means the font was not available,
	E_UNEXPECTED means the font could not be used to initialize the renderer in the
	expected way (eg, the Graphite tables could not be found).
	Assumes InitRenderer() has already been called to set the font name.
	ENHANCE: Do we possibly need to return an error code for an invalid font name?
	ENHANCE: This is not a standard use of E_UNEXPECTED, we may want to have the method return
	an enumeration member.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeEngine::FontIsValid()
{
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Give the maximum length of information that this renderer might want to pass
	from one segment to another in SimpleBreakPoint>>pbNextSegDat.
	UniSeg never passes info from one segment to another.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeEngine::get_SegDatMaxLength(int * cb)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(cb);
	*cb = 0;
	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Return the support script directions. The UniscribeRenderer can do horizontal in either
	direction.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeEngine::get_ScriptDirection(int * pgrfsdc)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pgrfsdc);
	*pgrfsdc = kfsdcHorizLtr | kfsdcHorizRtl;
	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Return the class ID for the implementation class.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeEngine::get_ClassId(GUID * pguid)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pguid);
	memcpy(pguid, &CLSID_UniscribeEngine, isizeof(GUID));
	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Calculate stretch potential for each glyph
----------------------------------------------------------------------------------------------*/
int * UniscribeEngine::CalculateStretchValues(int cglyph, const Vector<int>& vGlyphStretchTypes)
{
	int * prgdxsStretch = NewObj int[cglyph];
	int cglyphWhiteSpace = 0;
	for (int iglyphLp = 0; iglyphLp < cglyph; iglyphLp++)
	{
		if (vGlyphStretchTypes[iglyphLp] == kcstWhiteSpace)
		{
			prgdxsStretch[iglyphLp] = 1000;
			cglyphWhiteSpace++;
		}
		else
			prgdxsStretch[iglyphLp] = 0;
	}
	if (cglyphWhiteSpace)
	{
		// Remove stretch from trailing white-space.
		for (int iglyphLp = cglyph - 1; iglyphLp >= 0; iglyphLp--)
		{
			if (vGlyphStretchTypes[iglyphLp] != kcstWhiteSpace)
				break;
			prgdxsStretch[iglyphLp] = 0;
			cglyphWhiteSpace--;
		}
	}
	if (cglyphWhiteSpace == 0)
	{
		// No white space; give some stretch to base characters.
		for (int iglyphLp = 0; iglyphLp < cglyph; iglyphLp++)
		{
			if (vGlyphStretchTypes[iglyphLp] == kcstLetter)
				prgdxsStretch[iglyphLp] = 1000;
		}
	}

	return prgdxsStretch;
}


/*----------------------------------------------------------------------------------------------
	Make a segment by finding a suitable break point in the specified range of text.
	Note that it is appropriate for line layout to use this routine even if putting
	text on a single line, because an old writing system may take advantage of line layout
	to handle direction changes and style changes and generate multiple segments
	even on one line. For such layouts, pass a large dxMaxWidth, but still expect
	possibly multiple segments.
	Arguments:
	[in]	pgjus				NULL if no justification will ever be needed for the resulting segment
	[in]	pvg					Pointer to graphics interface.
	[in]	pts					Pointer to text source interface.
	[in]	ichMinSeg			Index of the first char in the text that is of interest.
	[in]	ichLimText			Index of the last char in the text that is of interest (+ 1).
	[in]	ichLimBacktrack		Index of last char that may be included in the segment;
								generally the same as ichLimText unless backtracking.
	[in]	fNeedFinalBreak		(Not used.)
	[in]	fStartLine			True if the segment is logically first on the line.
	[in]	dxMaxWidth			Whatever coordinates pvg is using.
	[in]	lbPref				Try for longest seg of this weight.
	[in]	lbMax				Max if no preferred break possible.
	[in]	twsh				How we are handling trailing white-space.
	[in]	fParaRtoL			Overall paragraph direction.
	[out]	ppsegRet			Segment produced, or null if nothing fits.
	[out]	pdichLimSeg			Offset to last char of segment, first of next if any.
	[out]	pdxWidth			Width of new segment, if any.
	[out]	pest				What caused the segment to end?
	[in]	cbPrev				(Not used.)
	[in]	pbPrevSegDat		(Not used.)
	[in]	cbNextMax			(Not used.)
	[out]	pbNextSegDat		(Not used.)
	[out]	pcbNextSegDat		(*pcbNextSegDat always set to zero.)
	[out]	pdichContext		(*pdichContext always set to zero.)

	TODO 1441 (SharonC): handle fParaRtoL; specifically, if the paragraph direction is
	right-to-left, trailing white-space characters should be reversed.

	TODO (SharonC): handle trailing white space.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeEngine::FindBreakPoint(
	IVwGraphics * pvg, IVwTextSource * pts, IVwJustifier * pvjus,
	int ichMinSeg, int ichLimText, int ichLimBacktrack,
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
	Vector<OLECHAR> vch;	// Use as buffer if rgchBuf is not big enough
	OLECHAR * prgchBuf;		// will point to either rgchBuf or vch.Begin().
	int citem;
	LgCharRenderProps chrpThis;
	LgCharRenderProps chrp;
	UniscribeRunInfo & uri = g_uri;
	int ichMinUri = -1; // JT: this is apparently set to ichMinNfc each iteration and never otherwise changed.
	uri.pchrp = &chrp;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **) &qvg32));
	CheckHr(qvg32->GetDeviceContext(&uri.hdc));

	int ichMinDum; // for GetCharProps to return (used later also).
	int ichLim;  // Used later as limit of successive runs.
	CheckHr(pts->GetCharProps(ichMinSeg, &chrpThis, &ichMinDum, &ichLim));
	int nDirDepth = chrpThis.nDirDepth;
	// chrpThis.nDirDepth will be 0 if the WS is LTR and 1 if the WS is RTL.
	// If we're putting LTR text (nDirDepth == 0) into an RTL paragraph,
	// we want to make this an upstream segment with a direction depth
	// greater than 1. Since it is LTR text, that means it has to be 2.
	// If this seems to be behaving wrongly, check that the direction of all
	// writing systems involved and the paragraph itself are all correct.
	if (fParaRtoL && nDirDepth == 0)
		nDirDepth = 2;

	// If a forced empty segment, stop here
	UniscribeSegmentPtr qus;
	if (ichLimBacktrack <= ichMinSeg)
	{
LEmptySeg:
		*pest = kestNoMore;
		*pdxWidth = 0;
		qus.Attach(NewObj UniscribeSegment(pts, this, 0,
			klbNoBreak, klbNoBreak, true, fParaRtoL));
		qus->SetDirectionInfo(nDirDepth, (twsh == ktwshOnlyWs));
		Vector<int> dummy;
		qus->SetStretchValues(0, NULL, dummy);
		*ppsegRet = qus.Detach();
		*pdichLimSeg = 0;
		return S_OK;
	}

	// Now check whether any characters are hard-breaks.
	bool fGotHardBreak = false;
	{ // (local block)
		// Get the characters
		int cch = ichLimText - ichMinSeg;
		prgchBuf = rgchBuf;
		if (cch > INIT_BUF_SIZE)
		{
			vch.Resize(cch);
			prgchBuf = vch.Begin();
		}
		CheckHr(pts->Fetch(ichMinSeg, ichLimText, prgchBuf));
		wchar *pch = prgchBuf;
		OLECHAR *pchLim = pch + cch;
		for (; pch < pchLim; pch++)
		{
			wchar ch = *pch;
			if (ch == '\n' || ch == '\t' || ch == '\r' || ch == 0xfffc || ch == 0x2028)
			{
				ichLimText = ichMinSeg + pch - prgchBuf;
				if (ichLimBacktrack > ichLimText)
					ichLimBacktrack = ichLimText;
				fGotHardBreak = true;
				break;
			}
		}
	}
	// If the last character before the limit is the start of a surrogate pair,
	// decrease the limit by one.  We should never split a surrogate pair!
	int cchNFD = ichLimBacktrack - ichMinSeg;
	if (IsHighSurrogate(prgchBuf[cchNFD - 1]))
	{
		--ichLimBacktrack;
		if (ichLimBacktrack <= ichMinSeg)
			goto LEmptySeg;
		--cchNFD;
	}
	// Ensure that ichLimBacktrack points to either the end of the available text or to a
	// base character.
	if (ichLimBacktrack < ichLimText)
	{
		// Get one character past the limit, normalize the substring to NFC.
		if (ichLimBacktrack + 1 < ichLimText && IsHighSurrogate(prgchBuf[cchNFD]))
			++cchNFD;		// include both halves of a surrogate pair.
		StrUni stuT(prgchBuf, cchNFD + 1);
		StrUtil::NormalizeStrUni(stuT, UNORM_NFC);
		// If the length shrinks, then we have to worry about the limit being in the
		// middle of a composition sequence.
		if (stuT.Length() < cchNFD + 1)
		{
			int cchTooManyNFC = stuT.Length();
			do
			{
				cchNFD = ichLimBacktrack - ichMinSeg;
				if (IsHighSurrogate(prgchBuf[cchNFD - 1]))
				{
					// can't split a surrogate pair!
					--ichLimBacktrack;
					if (ichLimBacktrack <= ichMinSeg)
						goto LEmptySeg;
					--cchNFD;
				}
				stuT.Assign(prgchBuf, cchNFD);
				StrUtil::NormalizeStrUni(stuT, UNORM_NFC);
				if (stuT.Length() == cchTooManyNFC)
					--ichLimBacktrack;
			} while (ichLimBacktrack > ichMinSeg && stuT.Length() == cchTooManyNFC);
			if (ichLimBacktrack <= ichMinSeg)
				goto LEmptySeg;
		}
	}

	// Having at least one character we can call this.
	// Note that there may be several Uniscribe items in one fw run, and that an item may not
	// end at a line break opportunity. In particular if a run contains sequences of PUA
	// characters from plane 0 Uniscribe creates new items for these for reasons which are not
	// clear.
	int cchNfc = UniscribeSegment::CallScriptItemize(rgchBuf, INIT_BUF_SIZE, vch, pts, ichMinSeg,
		ichLimText - ichMinSeg, &prgchBuf, citem, (bool)fParaRtoL);

	Vector<int> vichBreak;
	ILgCharacterPropertyEnginePtr qcpe;

	int dxSegWidth = 0; // Segment total width.
	SCRIPT_ITEM * pscri = UniscribeSegment::g_vscri.Begin(); // The current Uniscribe item.

	int irun = 0;
	Vector<int> vdxRun; // values of dxSegWidth, to restore on backtracking.
	Vector<int> vichRun; // values of ichMin, hence in original characters.
	Vector<int> viglyphRun; // values of cglyph to restore on backtracking.

	bool fFixWs = false;
	bool fHitNextWs = false;
	bool fStoppedForRtoL = false; // also used for upstream LTR text
	bool fOkBreak = false;
	bool fRemovedWs = false;
	bool fBacktracking = (ichLimBacktrack < ichLimText);
	bool fRemovedWsSeg = false;
	int ichLimBT2 = ichLimBacktrack; // An internal backtracking limit: can't use more than this number of (original) characters.
	bool fLimBT2Ok = false; // when true, okay to stop right at ichLimBT2
	int cglyph = 0; // number of glyphs in runs up to this point

	Vector<int> vcst; // stretch-types: cumulative for entire segment

	int ichLimNfc = 0; // offset in NFC chars of this segment of runs so far included in segment.
	// Each iteration handles one fw run, or one Uniscribe item if that is shorter.
	// Specifically, at the start of each iteration, ichLim-ichMinSeg original characters, which is
	// ichLimNfc normalized characters, have been 'put' in the segment, resulting in cglyph glyphs.
	// irun runs have been 'put' in the segment, and the width of each of them are saved in vdxRun,
	// their starting ichMin values in vichRun, and their starting glyph indexes (?) in vglyphRun.
	// The typical task for one iteration is to add another run, from the old ichLim up to the end
	// of the TsString run or script item, to the segment, updating all relevant variables.
	// Several other things can happen:
	//  - we may find that the next run is wider than the available space.
	//		- we may find an acceptable break point within the run that doesn't fit, and terminate
	//			the segment there.
	//		- we may find that there is no acceptable break point in the run, and 'backtrack',
	//			setting everything back to the previous run except that ichLimBT2 indicates that
	//			we can't go beyond that run. This results in trying to find a line break position
	//			in the previous run.
	// - If we're in the last run, we have to deal with trailing whitespace. That may also lead
	// to backtracking.
	// - If we're in (or backtracked to) the first run, we may conclude that nothing fits, and
	//	the whole method fails to make a segment.
	// - We may find that we hit a different writing system, and terminate this segment at the boundary.
	// - We may find we hit a font which requires a different renderer, and terminate this segment
	//	at the boundary.
	// - We may decide we want only one run in the segment, because it is RTL text, or upstream LTR text,
	//	or a whitespace-only segment, and stop after adding one run.
	for (ichLim = ichMinSeg; ;)
	{
		bool fQuit = false;

		int ichMin = ichLim; // Begin new run at start (first iteration), or end prev run. Original characters.
		int ichMinNfc = ichLimNfc;  // Min of this run is lim of previous (or 0 for first run).

		IRenderEnginePtr qreneng;

		// This updates ichLim to the end of the next run that has the same character
		// properties as the one at ichMin. Then we proceed to reduce this if necessary
		// so we have a run that does not go past the characters we are allowed to use,
		// nor past the end of the current script item.
		int ichLimNext;
		CheckHr(pts->GetCharProps(ichMin, &chrp, &ichMinDum, &ichLimNext));
		if (chrp.ws != chrpThis.ws)
		{
			fHitNextWs = true;
			fQuit = true;
			goto LQuit;
		}
		// Figure out what the chrp means and set up the VwGraphics.
		{
			ILgWritingSystemPtr qLgWritingSystem;
			AssertPtr(m_qwsf);
			CheckHr(m_qwsf->get_EngineOrNull(chrp.ws, &qLgWritingSystem));
			AssertPtr(qLgWritingSystem);
			CheckHr(qLgWritingSystem->InterpretChrp(&chrp));
		}
		CheckHr(pvg->SetupGraphics(&chrp));

		CheckHr(m_qwsf->get_Renderer(chrp.ws, pvg, &qreneng));
		if (qreneng.Ptr() != this)
		{
			// Not actually another ws, but it requires a different (probably Graphite)
			// rendering engine.
			fHitNextWs = true;
			fQuit = true;
			goto LQuit;
		}
		ichLim = min(ichLimNext, ichLimBT2);
		// Optimize JohnT: if ichLim==ichBase+m_dichLim, can use cchNfc.
		ichLimNfc = UniscribeSegment::OffsetInNfc(ichLim, ichMinSeg, pts);
		if (ichLimNfc == ichMinNfc)
		{
			// This can happen if later characters in a composite have different properties than the first.
			// We must detect this, otherwise, we may use up an extra scri by repeating processing at
			// the same index, and get badly out of sync. It seems we must just ignore any differing
			// properties on non-initial parts of a composite.

			// We must make a further check that it didn't happen at the very end of the input,
			// otherwise, we can get into a closed loop.
			if (ichLim >= ichLimBT2)
			{
				fQuit = true;
				goto LQuit;
			}

			// Otherwise just skip the disappearing run that doesn't correspond to
			// any NFC characters and go on.
			continue;
		}
		if (ichLimNfc > (pscri + 1)->iCharPos)
		{
			// Script item is smaller than run; shorten the amount we treat as a 'run'.
			ichLimNfc = (pscri + 1)->iCharPos;
			ichLim = UniscribeSegment::OffsetToOrig(ichLimNfc, ichMinSeg, pts);
		}

		// Set up the characters of the run, if any.
		uri.prgch = prgchBuf + ichMinNfc;
		uri.cch = ichLimNfc - ichMinNfc;
		uri.psa = &pscri->a;
		uri.pvg = pvg;
		ichMinUri = ichMinNfc;

		UniscribeSegment::ShapePlaceRun(uri, true);

		if (dxSegWidth + uri.dxdWidth <= dxMaxWidth &&
			// If backtracking, can't assume that last char is a good break point...
			(!fBacktracking || ichLim < ichLimBT2 ||
				// ... unless it was in the middle of deleted white-space
				ichLim == ichLimBT2 && fLimBT2Ok))
		{
			// Whole of this run will go into the segment.
			// If we've inserted the whole script item go on to the next.
			if (ichLimNfc >= (pscri + 1)->iCharPos)
				pscri++;

			irun++;
			vdxRun.Push(dxSegWidth); // Remember for local backtracking.
			vichRun.Push(ichMin);
			viglyphRun.Push(cglyph);
			Assert(vdxRun.Size() == irun);
			Assert(vichRun.Size() == irun);
			Assert(viglyphRun.Size() == irun);

			dxSegWidth += uri.dxdWidth;
			cglyph += uri.cglyph;
			// ichMin gets updated to ichLim at start of next iteration.

			// Copy the stretch types into the segment-wide vector.
			vcst.Resize(max(vcst.Size(), cglyph));
			for (int iglyphLp = 0; iglyphLp < uri.cglyph; iglyphLp++)
			{
				int iglyphSeg = iglyphLp + viglyphRun[irun - 1];
				vcst[iglyphSeg] = uri.prgcst[iglyphLp];
			}

			// Exit the loop if we have fit everything. Don't put this test in the 'for' stmt
			// because it will prevent the one iteration we need if 0 characters.
			if (ichLim >= ichLimBT2)
			{
				fQuit = true;
			}

			// I (JohnT) think is is probably more efficient to make separate segments
			// for each item/run than to handle multiple ones in a single segment
			// for right-to-left. The problem is that to get the placement of the runs,
			// we have to do the layout of the whole segment before we start the main loop
			// in DoAllRuns, then do it again as we work through them. On the other hand,
			// the paragraph layout stuff already knows how to re-order the segments
			// properly for right-to-left.

			// So if it's an RTL segment, stop after the first run. Also do this
			// for upstream (LTR) segments; this is convenient for white-space handling.
//			int nParaRtoL = (int)(fParaRtoL == true);
			if ((nDirDepth % 2)) // || (nDirDepth % 2) != nParaRtoL && twsh == ktwshOnlyWs)
			{
				fHitNextWs = false;
				fStoppedForRtoL = true;
				if (!fOkBreak)
				{
					// Get a char props engine and initialize it for finding line breaks;
					// Check for the last line break in the current segment.
					// If the last line break is at the end of the segment, the overall line break
					// is okay.
					ILgCharacterPropertyEnginePtr qcpe;
					AssertPtr(m_qwsf);
					CheckHr(m_qwsf->get_CharPropEngine(chrpThis.ws, &qcpe));
					AssertPtr(qcpe.Ptr());
					CheckHr(qcpe->put_LineBreakText(prgchBuf, cchNfc));
					int ichBreakPoint = ichLimNfc - 1; // Nfc Chars.
					int ichNextBreak; // Nfc characters.
					LgLineBreak lb;
					CheckHr(qcpe->LineBreakAfter(ichBreakPoint, &ichNextBreak, &lb));
					if (ichNextBreak == ichLimNfc) // Hopefully the next possible break is the one we're proposing to use.
						fOkBreak = true;
				}
				fQuit = true;
			}
			else if (twsh == ktwshOnlyWs)
			{
				fStoppedForRtoL = true;  // sort of
				fOkBreak = true;
				fQuit = true;
			}
		}
		else
		{
			// Part of this run should go into the segment; not all of it fits.
			// First, see how much would fit.
			int ichRun = 0; // iterates through NFC characters in run.
			SCRIPT_LOGATTR * prgsla = NULL; // used later in method, only if ScriptPlace succeeded.
			int dxRunWidth = 0;
			int rgwidth[INIT_BUF_SIZE];
			IntVec vwidth; // Used if rgwidth not big enough.
			int * prgwidth = rgwidth; // character advance widths.
			if (uri.cch > INIT_BUF_SIZE)
			{
				vwidth.Resize(uri.cch);
				prgwidth = vwidth.Begin();
			}

			if (uri.fScriptPlaceFailed)
			{
				int glyphWidth = uri.dxdWidth / uri.cglyph;

				for (; ichRun < uri.cch;)
				{
					int ichNext = ichRun;
					NextCodePoint(ichNext, uri.prgch, uri.cch);
					if (ichNext == ichRun + 1)
					{
						// one code point per char, gets all width
						prgwidth[ichRun] = glyphWidth;
					}
					else
					{
						// surrogate pair, allocate half width to each
						prgwidth[ichRun] = glyphWidth / 2;
						prgwidth[ichRun + 1] = glyphWidth - prgwidth[ichRun];
					}
					if (glyphWidth + dxRunWidth + dxSegWidth > dxMaxWidth)
						break;	// This character will not fit, so don't include it.
					dxRunWidth += glyphWidth;
					ichRun = ichNext;
				}// loop adding characters.
			}
			else
			{
				// Make prgwidth point at a buffer of uri.cch ints (if not too many, do without
				// memory allocation).
				SCRIPT_LOGATTR rgsla[INIT_BUF_SIZE];
				ScrLogAttrVec vsla; // Likewise.
				prgsla = rgsla;
				if (uri.cch > INIT_BUF_SIZE)
				{
					vsla.Resize(uri.cch);
					prgsla = vsla.Begin();
				}

				DISABLE_MULTISCRIBE
				{
					HRESULT hr;
					IgnoreHr(hr = ::ScriptGetLogicalWidths(
						uri.psa,
						uri.cch,
						uri.cglyph,
						uri.prgAdvance,
						uri.prgCluster,
						uri.prgsva,
						prgwidth));

					if (FAILED(hr))
						ThrowHr(WarnHr(hr), L"ScriptGetLogicalWidths failed");

					// Obtain logical attributes including whitespace and wordstart flags.
					IgnoreHr(hr = ::ScriptBreak(
						uri.prgch,
						uri.cch,
						uri.psa,
						prgsla));

					if (FAILED(hr))
						ThrowHr(WarnHr(hr), L"ScriptBreak failed");
				}

				// Add as many characters as possible. Iterate by complete code points.
				// Note that, for surrogate pairs, ScriptGetLogicalWidths divides the total width
				// between the high and low surrogates, assigning half the width to the high
				// surrogate and half (or half + 1 if width is odd) to the low surrogate. Spaces are
				// "free" (counted as fitting come what may) until a non-space is encountered, when
				// testing against dxMaxWidth resumes. Thus the total width can be more than
				// dxMaxWidth provided that the last non-space will fit into dxMaxWidth. Assume that
				// a Unicode run doesn't end between spaces.
				for (; ichRun < uri.cch;
					NextCodePoint(ichRun, uri.prgch, uri.cch))
				{
					int dxwidth = prgwidth[ichRun];
					if (prgsla[ichRun].fWhiteSpace)
					{
						dxRunWidth += dxwidth;	// Assume white space characters are not surrogates.
						continue;
					}
					if (dxwidth + dxRunWidth + dxSegWidth > dxMaxWidth)
						break;	// This character will not fit, so don't include it.
					if (twsh == ktwshOnlyWs && !prgsla[ichRun].fWhiteSpace)
						break; // something other than whitespace
					if (IsHighSurrogate(*(uri.prgch + ichRun)) && ichRun < uri.cch - 1)
					{
						// If the current character is a high surrogate we need to make sure that
						// the width assigned to the low surrogate will also fit.
						Assert(IsLowSurrogate(*(uri.prgch + ichRun + 1)));
						int dxWidthLow = prgwidth[ichRun + 1];
						if (dxwidth + dxWidthLow + dxRunWidth + dxSegWidth > dxMaxWidth)
							break;	// Surrogate pair character won't fit.
						dxRunWidth += dxWidthLow;
					}
					dxRunWidth += dxwidth;
				}// loop adding characters.
			} // normal case where ScriptLayout worked.
			if (ichRun == 0 && irun > 0)
			{
				// Ooops, nothing will fit. Backtrack and test end of previous run.
				irun--;
				dxSegWidth = *(vdxRun.Top());
				vdxRun.Pop();
				ichLimBT2 = ichMin;
				ichLim = *(vichRun.Top());
				ichLimNfc = UniscribeSegment::OffsetInNfc(ichLim, ichMinSeg, pts);
				vichRun.Pop();
				cglyph = *(viglyphRun.Top());
				viglyphRun.Pop();
				fRemovedWs = false;
				fBacktracking = true;
				continue;
			}

			int ichRunFit = ichRun; // NFC
			int dxWidthFit = dxSegWidth + dxRunWidth;

			// If there is a suitable break point after the start of this run (or Uniscribe
			// item) and it is in that part which we can fit, then this run will be the last.
			// If the previous suitable break point is in (including at the end of) a previous
			// run then we will need to go back to that run. Note that vichBreak's zeroth
			// element contains 0 and that ICU line breaks will not be followed by whitespace.

			if (!qcpe)
			{
				// Get a char props engine
				AssertPtr(m_qwsf);
				CheckHr(m_qwsf->get_CharPropEngine(chrpThis.ws, &qcpe));
				AssertPtr(qcpe.Ptr());
				CheckHr(qcpe->put_LineBreakText(prgchBuf, cchNfc));
				Assert(vichBreak.Size() == 0);
			}

			// This block of code has the aim of figuring out a line break position that is <= ichRunFit
			// and is a valid line break position and includes as much text as possible. Initially we
			// compute an offset into the Uniscribe (NFC) buffer.
			int ichLineBreakNfc = 0; // original, and default.
			int ichNextBreak; // This is set to the next break point, copied to ichBreakPoint if not the reserved Done value.
			int iich = 0;
			int cich = vichBreak.Size();
			int ichBreakPoint = 0; // NFC offset into segment, iterates through the possible line breaks in the segment.
			// The purpose of this loop is to set ichLineBreak to a possible line break.
			// The break iterator requires that we start at the beginning of the segment.
			// We loop until we run out of breaks or until we find one that is beyond what
			// will fit (and then use the previous one).
			// (One way of running out is to reach the end of the array of characters we put
			// into the cpe. It doesn't work to ask it for the line break at the limit.
			while (ichBreakPoint < cchNfc )
			{
				if (iich < cich)
				{
					// cached value from previous pass through the outer loop.
					ichNextBreak = vichBreak[iich];
					++iich;
				}
				else
				{
					// calculate new value and cache it for possible future use.
					LgLineBreak lb;
					CheckHr(qcpe->LineBreakAfter(ichBreakPoint, &ichNextBreak, &lb));
					vichBreak.Push(ichNextBreak);
				}
				if (ichNextBreak == BreakIterator::DONE)
				{
					// There are no more valid line breaks in the allowed range...but, if
					// everything fit, the very end of the text is a valid line break.
					if (ichLimText <= ichRunFit + ichMin)
						ichLineBreakNfc = cchNfc;
					break;
				}
				ichBreakPoint = ichNextBreak;
				if (ichBreakPoint <= ichRunFit + ichMinNfc)
				{
					ichLineBreakNfc = ichBreakPoint; // Save this as the best found so far
				}
				else
				{
					break; // Use the last one we saved, this one doesn't fit.
				}
			}

			if (twsh == ktwshNoWs && !uri.fScriptPlaceFailed)
			{
				// Remove trailing white space.
				fFixWs = false; // we're handling it here.
				int ichEndRun = ichLineBreakNfc - ichMinUri;
				while (ichEndRun > 0 && prgsla[ichEndRun - 1].fWhiteSpace)
				{
					ichEndRun--;
					fRemovedWs = true;
				}
				if (ichEndRun == 0)
				{
					// Nothing available but maybe white space. Backtrack if possible, or fail.
					// (It's also normal to come here after we've found trailing ws and the app
					// is (rather stupidly) trying to make another non-ws segment.)
					if (irun > 0)
					{
						irun--;
						dxSegWidth = *(vdxRun.Top());
						vdxRun.Pop();
						ichLimBT2 = ichMin;
						ichLim = *(vichRun.Top());
						ichLimNfc = UniscribeSegment::OffsetInNfc(ichLim, ichMinSeg, pts);
						vichRun.Pop();
						cglyph = *(viglyphRun.Top());
						viglyphRun.Pop();
						fBacktracking = true;
						continue;
					}
					else if (lbMax < klbLetterBreak) // don't absolutely have to put something in segment
					{
						*ppsegRet = NULL;
						return S_OK;
					}
				}
				ichLineBreakNfc = ichEndRun + ichMinUri;
			}

			// Recompute the segment width to match the new line break.
			dxRunWidth = 0;
			int ichRunLim = ichLineBreakNfc - ichMinUri;
			for (ichRun = 0; ichRun < ichRunLim; ++ichRun)
				dxRunWidth += prgwidth[ichRun];
			// If the trailing characters of the segment are whitespace, don't include
			// their width. (If ScriptPlace failed, treat all as letters.)
			if (!uri.fScriptPlaceFailed)
			{
				for (ichRun = ichRunLim - 1; ichRun >= 0; --ichRun)
				{
					if (prgsla[ichRun].fWhiteSpace)
						dxRunWidth -= prgwidth[ichRun];
					else
						break;
				}
			}
			dxSegWidth += dxRunWidth;

			// ichLineBreakNfc is now the latest suitable break point, relative to the start of the segment.

			if (ichLineBreakNfc == 0)
			{
				// No good line break is available.

				if (lbMax < klbLetterBreak)
					// Return without making a segment. Apparently *pest does not matter.
					return S_OK;

				if (twsh == ktwshOnlyWs)
					return S_OK; // assumes ws would have been a good break!

				// Letter or clip break is acceptable. Work backwards from what will fit.
				// Assume that the start of the latest run (if not the firtst) provides
				// a good letter break, if there is no subsequent one.
				// Decrement ichRun (and subtract corresponding width) until ichRun is the
				// start of a cluster.
				// The first character of a cluster is one with a different cluster
				// from the previous one.
				ichRun = ichRunFit;
				dxSegWidth = dxWidthFit;
				while (ichRun > 0 && uri.prgCluster[ichRun] == uri.prgCluster[ichRun - 1])
				{
					// Note that, for surrogate pairs, ScriptGetLogicalWidths divides the
					// total width between the high and low surrogates, assigning half the
					// width to the high surrogate and half (or half + 1 if width is odd) to
					// the low surrogate.  On this basis, taking it for granted that two
					// halves of a surrogate pair will always be in the same cluster, we can
					// back up by code units rather than code points.
					ichRun--;
					dxSegWidth -= prgwidth[ichRun];
				}
				if (!ichRun && !irun)
				{
					// Could not fit one cluster of the first run.
					if (lbMax == klbClipBreak)
					{
						// REVIEW JohnT: is this the right way to detect that we must put
						// something? Ensure at least one cluster is returned. Add the
						// first character...
						dxSegWidth += uri.prgAdvance[uri.prgCluster[0]];
						ichRun = 1;

						// ...and any others in the same cluster.
						while (ichRun < uri.cch
							&& uri.prgCluster[ichRun] == uri.prgCluster[0])
						{
							// REVIEW JohnT: this is more-or-less copied from an MS example,
							// but I don't understand it. Since prgcluster[ichRun] is the
							// same as prgcluster[0], we are effectively adding the width of
							// the first character repeatedly. Shouldn't it just be
							// += uri.prgAdvance[ichRun];??
							// In all other code prgAdvance is indexed simply by character.
							dxSegWidth += uri.prgAdvance[uri.prgCluster[ichRun]];
							ichRun++;
						}
					}
				}
				// Now there is an acceptable break after at least one character.

				fRemovedWs = false;
				if (!uri.fScriptPlaceFailed)
				{
					if (twsh == ktwshNoWs)
					{
						if (ichRun < uri.cch && prgsla[ichRun].fWhiteSpace)
							fRemovedWs = true; // left off trailing ws
					}
					else
					{
						// Include any subsequent whitespace characters.
						while (ichRun < uri.cch && prgsla[ichRun].fWhiteSpace)
						{
							dxSegWidth += uri.prgAdvance[ichRun];
							ichRun++;
						}
					}
				}
				ichLim = UniscribeSegment::OffsetToOrig(ichMinUri + ichRun, ichMinSeg, pts);
				break;
			}

			int ichLineBreak = UniscribeSegment::OffsetToOrig(ichLineBreakNfc, ichMinSeg, pts);

			if (ichLineBreak <= ichMin)
			{
				// Break point was in a previous run: go back. (Each iteration decreases ichMin by popping the stack.)
				while (ichLineBreak <= ichMin && irun > 0)
				{
					// ichBreak is at or before the start of the latest run (or Uniscribeitem).
					irun--;
					dxSegWidth = *(vdxRun.Top());
					vdxRun.Pop();
					ichMin = *(vichRun.Top());
					vichRun.Pop();
					cglyph = *(viglyphRun.Top());
					viglyphRun.Pop();
				}
				ichLim = ichMin;	// Required to get correct values at start of loop.
				ichLimNfc = UniscribeSegment::OffsetInNfc(ichLim, ichMinSeg, pts);
				ichLimBT2 = ichLineBreak;
				fRemovedWs = false;
				fBacktracking = true;
				continue;
			}
			ichLim = ichLineBreak; // We limit the segment to not exceed the latest line break point.
			Assert(ichLim <= ichLimBacktrack);
			ichLimNfc = UniscribeSegment::OffsetInNfc(ichLim, ichMinSeg, pts);
			fOkBreak = true;	// Means we have a good line break.

			// Store the glyph-specific information: stretch values.
			int cchRunTotalTmp = uri.cch;
			uri.cch = UniscribeSegment::OffsetInNfc(ichLim, ichMinSeg, pts) - ichMinNfc;
			UniscribeSegment::ShapePlaceRun(uri, true);
			viglyphRun.Push(cglyph);
			cglyph += uri.cglyph;
			vcst.Resize(max(vcst.Size(), cglyph));
			for (int iglyphLp = 0; iglyphLp < uri.cglyph; iglyphLp++)
			{
				int iglyphSeg = iglyphLp + viglyphRun[irun];
				vcst[iglyphSeg] = uri.prgcst[iglyphLp];
			}
			uri.cch = cchRunTotalTmp; // restore it

			break;	// We are done, because we know we are at a line break.
			// Note: this assumes that word or hyphen break == line break.
			// Note also that we choose to break at a line break if it exists (as being best)
			// even if the preferred break type was letter or clip.
		}

		// We get here when we have included an entire run, but are quitting for some reason
		// (hit the end of the input or new writing system, bidi issues).
LQuit:
		if (fQuit)
		{
			if (twsh == ktwshNoWs)
			{
				fRemovedWs = RemoveTrailingWhiteSpace(ichMinUri, &ichLimNfc, uri);
				ichLim = UniscribeSegment::OffsetToOrig(ichLimNfc, ichMinSeg, pts);
				// Usually the worst case is that ichLimNfc == ichMinUri, indicating that the whole run is
				// white space. However, in at least one pathological case, we have observed uniscribe
				// strip of more than one run of white space. Hence the <=.
				if (ichLimNfc <= ichMinUri)
				{
					// This entire run was white space, and has been removed. Backtrack
					// into the previous run and remove the white-space from it.
					if (irun > 1)
					{
						// First handle the fact that we totally deleted a run.
						irun--;
						vdxRun.Pop();
						ichLimBT2 = *(vichRun.Top());	// don't go beyond the beginning of the
														// run we just deleted
						fLimBT2Ok = true;
						vichRun.Pop();
						viglyphRun.Pop();

						// ...and now the backtracking.
						irun--;
						dxSegWidth = *(vdxRun.Top());
						vdxRun.Pop();
						ichLim = *(vichRun.Top());
						ichLimNfc = UniscribeSegment::OffsetInNfc(ichLim, ichMinSeg, pts);
						vichRun.Pop();
						cglyph = *(viglyphRun.Top());
						viglyphRun.Pop();
						Assert(irun == vdxRun.Size());
						Assert(irun == vichRun.Size());
						Assert(irun == viglyphRun.Size());
						fBacktracking = true;
						fRemovedWsSeg = true;
						if (pscri > UniscribeSegment::g_vscri.Begin())
							--pscri;
						continue;
					}
					else
					{
						*ppsegRet = NULL;
						return S_OK;
					}
				}
			}
			else if (twsh == ktwshOnlyWs)
			{
				// Possibly this is not really needed, because Uniscribe apparently creates a
				// separate script-item out of the leading white-space, and if it is upstream
				// we will have terminated the segment after the first run. But just in case...
				Assert(irun == 1);
				Assert(ichMinUri == 0);
				RemoveNonWhiteSpace(ichMinUri, &ichLimNfc, uri);
				ichLim = UniscribeSegment::OffsetToOrig(ichLimNfc, ichMinSeg, pts);
				if (ichLim == ichMinSeg)
					return S_OK; // failure to create a valid segment
				fOkBreak = true;
			}

			break; // out of main loop
		}
	}

	// At this point we will definitely make a segment with these characters.
	// Still keep it in a smart pointer for now just in case some error occurs.
	qus.Attach(NewObj UniscribeSegment(pts, this, ichLim - ichMinSeg,
		klbNoBreak, klbNoBreak, true, fParaRtoL));
	qus->SetDirectionInfo(nDirDepth, (twsh == ktwshOnlyWs));
	*pdichLimSeg = ichLim - ichMinSeg;
	*pdxWidth = dxSegWidth;

	// Calculate the stretch potential for each glyph.
	qus->SetStretchValues(cglyph, CalculateStretchValues(cglyph, vcst), vcst);

	// Actually make the segment. TODO JohnT: this needs much more work...
	*pest = kestNoMore; // Default, change if needed.
	if (fRemovedWs)
	{
		*pest = kestMoreWhtsp;
	}
	else if (fHitNextWs)
	{
		// Todo JohnT: it would be nice to detect that this is an OK break if it is...
		*pest = kestWsBreak;
	}
	// Review JohnT (SharonC): Can be this simplified to (ichLim <= ichLimBacktrack)?
	else if (ichLim < ichLimBacktrack || (ichLim <= ichLimBT2 && fBacktracking))
	{
		if (fStoppedForRtoL)
		{
			*pest = fOkBreak ? kestOkayBreak : kestBadBreak;
		}
		else if (fBacktracking && fRemovedWsSeg)
		{
			// We removed a segment with only whitespace, but may have room for more segments
			// on the line.  This can happen when hitting a space while inserting LTR text into
			// a RTL paragraph.
			*pest = fOkBreak ? kestOkayBreak : kestBadBreak;
		}
		else
		{
			// We made a break in the middle of our ws run, so it must be a valid
			// line break and we will need more lines for our segment.
			*pest = kestMoreLines;
		}
	}
	else
	{
		// We broke at the end of our ws run, or earlier because of forced
		// backtracking from a later segment--or got the whole segment.
		int cchSource;
		pts->get_Length(&cchSource);
		if (cchSource < ichLim)
		{
			*pest = fOkBreak ? kestOkayBreak : kestBadBreak;
		}
		// else we are at the end of the para, leave as kestNoMore.
	}
	if (ichLim == ichLimText && fGotHardBreak)
	{
		*pest = kestHardBreak;
	}

	*ppsegRet = qus.Detach();

	// TODO JohnT: handle checking whether break is OK at run boundary (use regular Unicode
	// properties).

	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Remove trailing white-space. The 'uri' structure contains information for the final run.
	Return true if anything was removed. *pichLimSeg is an in/out parameter, the nfc offset
	of the end of the segment.
----------------------------------------------------------------------------------------------*/
bool UniscribeEngine::RemoveTrailingWhiteSpace(int ichMinRun, int * pichLimSeg,
	UniscribeRunInfo & uri)
{
	if (uri.fScriptPlaceFailed)
		return false;
	SCRIPT_LOGATTR rgsla[INIT_BUF_SIZE];
	ScrLogAttrVec vsla;
	SCRIPT_LOGATTR * prgsla = rgsla;
	if (uri.cch > INIT_BUF_SIZE)
	{
		vsla.Resize(uri.cch);
		prgsla = vsla.Begin();
	}

	DISABLE_MULTISCRIBE
	{
		HRESULT hr;
		IgnoreHr(hr = ::ScriptBreak(
			uri.prgch,
			uri.cch,
			uri.psa,
			prgsla)); // get whitespace info
		if (FAILED(hr))
			ThrowHr(WarnHr(hr), L"ScriptBreak failed");
	}

	int ichLimTmp = *pichLimSeg;
	bool fRemovedWs = false;
	while (ichLimTmp > 0 && prgsla[ichLimTmp - 1].fWhiteSpace)
	{
		ichLimTmp--;
		fRemovedWs = true;
	}
	*pichLimSeg = ichLimTmp;
	return fRemovedWs;
}

/*----------------------------------------------------------------------------------------------
	Remove everything from the run but leading white-space. The 'uri' structure contains
	information for the final (and only) run. Sets *pichLimSeg to the offset of the first
	NFC non-space character in the segment.
----------------------------------------------------------------------------------------------*/
void UniscribeEngine::RemoveNonWhiteSpace(int ichMinRun, int * pichLimSeg,
	UniscribeRunInfo & uri)
{
	if (uri.fScriptPlaceFailed)
	{
		*pichLimSeg = 0;
		return;
	}
	SCRIPT_LOGATTR rgsla[INIT_BUF_SIZE];
	ScrLogAttrVec vsla;
	SCRIPT_LOGATTR * prgsla = rgsla;
	if (uri.cch > INIT_BUF_SIZE)
	{
		vsla.Resize(uri.cch);
		prgsla = vsla.Begin();
	}
	DISABLE_MULTISCRIBE
	{
		HRESULT hr;
		IgnoreHr(hr = ::ScriptBreak(
			uri.prgch,
			uri.cch,
			uri.psa,
			prgsla)); // get whitespace info
		if (FAILED(hr))
			ThrowHr(WarnHr(hr), L"ScriptBreak failed");
	}

	int ichLimTmp = 0;
	while (ichLimTmp < *pichLimSeg && prgsla[ichLimTmp].fWhiteSpace)
		ichLimTmp++;
	*pichLimSeg = ichLimTmp;
}



/*----------------------------------------------------------------------------------------------
	Return the writing system factory for this database (or the registry, as the case may be).

	@param ppwsf Address of the pointer for returning the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP UniscribeEngine::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
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
STDMETHODIMP UniscribeEngine::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD
	ChkComArgPtrN(pwsf);

	m_qwsf = pwsf;

	END_COM_METHOD(g_fact, IID_IRenderEngine)
}
