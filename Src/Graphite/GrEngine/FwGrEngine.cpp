/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: FwGrEngine.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description: Contains the implementation of the GrEngine class.
----------------------------------------------------------------------------------------------*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

//:End Ignore

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************

FwGrJustifier * s_pgjus;

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

FwGrEngine::FwGrEngine()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	//BasicInit(); // shouldn't need this, since inherited constructor is automatically called
	m_pfont = NULL;
	m_fLog = false;
	m_pgjus = NULL;
	m_useNFC = false;
	FwSettings::GetBool(_T("Software\\SIL\\Fieldworks"), NULL, _T("GraphiteUseNFC"), &m_useNFC);
}

FwGrEngine::~FwGrEngine()
{
	if (m_pgjus)
	{
		delete m_pgjus;
		s_pgjus = NULL;
		m_pgjus = NULL;
	}
	delete m_pfont;
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Graphite.FwGrEngine"),
	&CLSID_FwGrEngine,
	_T("SIL Graphite renderer"),
	_T("Apartment"),
	&FwGrEngine::CreateCom);


void FwGrEngine::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<FwGrEngine> qgreng;
	qgreng.Attach(NewObj FwGrEngine());		// ref count initialy 1
	CheckHr(qgreng->QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP FwGrEngine::QueryInterface(REFIID riid, void **ppv)
{
	if (!ppv)
		return E_POINTER;
	AssertPtr(ppv);
	*ppv = NULL;
	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IRenderEngine *>(this));

	else if (riid == IID_IRenderEngine)
		*ppv = static_cast<IRenderEngine *>(this);
	else if (riid == IID_IRenderingFeatures)
		*ppv = static_cast<IRenderingFeatures *>(this);
	//else if (riid == IID_IJustifyingRenderer)
	//	*ppv = static_cast<IJustifyingRenderer *>(this);
	else if (riid == IID_ITraceControl)
		*ppv = static_cast<ITraceControl *>(this);
	else if (&riid == &CLSID_FwGrEngine)
		*ppv = static_cast<FwGrEngine *>(this);

	//else if (riid == CLSID_FwGrEngine) // TODO: ask JOHN about this
	//{
	//	Assert(false);
	//	*ppv = static_cast<FwGrEngine *>(this);
	//}

	else if (riid == IID_ISupportErrorInfo)
	{
		// for error reporting:
		*ppv = NewObj CSupportErrorInfo2(static_cast<IRenderEngine *>(this),
			IID_ISimpleInit, IID_IRenderEngine);
		return NOERROR;
	}

#ifdef OLD_TEST_STUFF
	else if (riid == IID_IGrEngineDebug)		// trick for test code instrumentation
	{
		GrEngineDebug * pgrengd = NewObj GrEngineDebug(this);
		HRESULT hr = pgrengd->QueryInterface(riid, ppv);
		pgrengd->Release();
		return hr;
	}
#endif // OLD_TEST_STUFF

	else
		return E_NOINTERFACE;
	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   ITraceControl Interface Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Set the tracing options.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::SetTracing(int n)
{
	BEGIN_COM_METHOD;

	if (!m_pfont)
	{
		Assert(false);
		return E_UNEXPECTED;
	}
	m_fLog = (bool)n;

	END_COM_METHOD(g_fact, IID_ITraceControl);
}

STDMETHODIMP FwGrEngine::GetTracing(int * pnOptions)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnOptions);

	if (!m_pfont)
	{
		Assert(false);
		return E_UNEXPECTED;
	}
	*pnOptions = (int)m_fLog;

	END_COM_METHOD(g_fact, IID_ITraceControl);
}


//:>********************************************************************************************
//:>	   IRenderingFeature methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Return a list of the feature IDs. If cMax is zero, pcfid returns the number of features
	supported. Otherwise it contains the number put into the array.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::GetFeatureIDs(int cMax, int * prgFids, int * pcfid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcfid);
	ChkComArrayArg(prgFids, cMax);

	if (!m_pfont)
	{
		Assert(false);
		return E_UNEXPECTED;
	}

	std::pair<FeatureIterator, FeatureIterator> pairIt = m_pfont->getFeatures();
	int cfid = pairIt.second - pairIt.first;
	FeatureIterator fit = pairIt.first;
	if (cMax == 0)
	{
		*pcfid = cfid;
		return S_OK;
	}

	*pcfid = min(cMax, cfid);
	for (int i = 0;
		fit != pairIt.second;
		++fit, i++)
	{
		if (i >= *pcfid)
			break;
		prgFids[i] = (*fit);
	}

	END_COM_METHOD(g_fact, IID_IRenderingFeatures);
}

/*----------------------------------------------------------------------------------------------
	Return the UI label for the given feature, in the given language.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::GetFeatureLabel(int fid, int nLanguage, BSTR * pbstrLabel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrLabel);

	if (!m_pfont)
	{
		Assert(false);
		return E_UNEXPECTED;
	}

	OLECHAR rgchw[128];
	memset(rgchw, 0, 128);

	FeatureIterator fit = m_pfont->featureWithID(fid);
	if (*fit != kInvalid)
		m_pfont->getFeatureLabel(fit, nLanguage, (gr::utf16*)rgchw);

	int cchw = wcslen(rgchw);

	*pbstrLabel = ::SysAllocStringLen(rgchw, cchw);

	END_COM_METHOD(g_fact, IID_IRenderingFeatures);
}

/*----------------------------------------------------------------------------------------------
	Return a list of recognized values for the given feature. If cfvalMax is zero,
	pcfval returns the total number of values. Otherwise, it returns the number entered into
	the array.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::GetFeatureValues(int fid, int cfvalMax,
	int * prgfval, int * pcfval, int * pfvalDefault)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcfval);
	ChkComOutPtr(pfvalDefault);
	ChkComArrayArg(prgfval, cfvalMax);

	if (!m_pfont)
	{
		Assert(false);
		return E_UNEXPECTED;
	}

	FeatureIterator fit = m_pfont->featureWithID(fid);
	std::pair<FeatureSettingIterator, FeatureSettingIterator> pairIt
		= m_pfont->getFeatureSettings(fit);

	FeatureSettingIterator fsit = pairIt.first;
	FeatureSettingIterator fsitEnd = pairIt.second;
	int cfval = fsitEnd - fsit;
	if (cfvalMax == 0)
	{
		*pcfval = cfval;
		return S_OK;
	}

	*pcfval = min(cfvalMax, cfval);
	for (int i = 0;
		fsit != pairIt.second;
		++fsit, i++)
	{
		if (i >= *pcfval)
			break;
		prgfval[i] = (*fsit);
	}

	FeatureSettingIterator fsitDefault = m_pfont->getDefaultFeatureValue(fit);
	*pfvalDefault = *fsitDefault;
	for (int ifeat = 0; ifeat < m_cfeatWDefaults; ifeat++)
	{
		if (m_rgfsetDefaults[ifeat].id == fid)
		{
			*pfvalDefault = m_rgfsetDefaults[ifeat].value;
			break;
		}
	}

	END_COM_METHOD(g_fact, IID_IRenderingFeatures);
}

/*----------------------------------------------------------------------------------------------
	Return the UI label for the given feature value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::GetFeatureValueLabel(int fid, int fval, int nLanguage,
	BSTR * pbstrLabel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrLabel);

	if (!m_pfont)
	{
		Assert(false);
		return E_UNEXPECTED;
	}

	OLECHAR rgchw[128];
	memset(rgchw, 0, 128);

	FeatureIterator fit = m_pfont->featureWithID(fid);
	if (*fit != kInvalid)
	{
		std::pair<FeatureSettingIterator, FeatureSettingIterator> pairfsit
			= m_pfont->getFeatureSettings(fit);
		FeatureSettingIterator fsit = pairfsit.first;
		for (int i = 0;
			fsit != pairfsit.second;
			++fsit, i++)
		{
			if (*fsit == fval)
				break;
		}
		if (*fsit != kInvalid)
			m_pfont->getFeatureSettingLabel(fsit, nLanguage, (gr::utf16*)rgchw);
	}

	int cchw = wcslen(rgchw);

	*pbstrLabel = ::SysAllocStringLen(rgchw, cchw);

	END_COM_METHOD(g_fact, IID_IRenderingFeatures);
}

//:>********************************************************************************************
//:>	   IRenderEngine methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize the engine. This must be called before any oher methods of the interface.
	How the data is used is implementation dependent. The UniscribeRenderer does not
	use it at all. The Graphite renderer uses font name, bold, and italic settings
	to initialize itself with the proper font tables. For Graphite, bstrData contains
	(optionally) default settings for any font features.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::InitRenderer(IVwGraphics * pvg, BSTR bstrData)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComBstrArgN(bstrData);

	// Make sure we can create a Graphite font.
	try
	{
		HDC hdc;
		IVwGraphicsWin32Ptr qvg32;
		CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
		CheckHr(qvg32->GetDeviceContext(&hdc));
		//FwGrGraphics gg(pvg);
		//gg.GetDeviceContext(&hdc);

		// Remember the font face name.
		LgCharRenderProps chrp;
		pvg->get_FontCharProperties(&chrp);
		memcpy(m_szFaceName, chrp.szFaceName, isizeof(m_szFaceName));

		// Make sure there is a cached font object.
		WinFont * pfontOld = m_pfont;
		m_pfont = new FwWinFont(hdc);
		m_fontSize = gr::GrEngine::RoundFloat(m_pfont->ascent() + m_pfont->descent());

		// Instead a code below, a FontException will be thrown.
		//FontErrorCode ferr = m_pfont->isValidForGraphite();
		//HRESULT hr;
		//std::wstring stuMsgBogus = FontLoadErrorDescription(ferr, 0, 0, &hr);

		// Delete this after creating the new one, so that if there happens to be only one
		// the font cache doesn't get destroyed and recreated!
		delete pfontOld;

		// Store the default feature values, which may be different from the font.
		m_cfeatWDefaults = ParseFeatureString((gr::utf16*)bstrData, BstrLen(bstrData), m_rgfsetDefaults);

		// This is kind of a kludge. The m_pfont may be kind of temporary, because the caller may delete
		// the graphics object and the DC. So call something to get set the FontFace set up while the
		// Font is still valid, so we can at least have access to some basic information from the engine.
		// Ideally we should probably create the WinFont with a private device context.
		int nTemp;
		this->get_ScriptDirection(&nTemp);

		return S_OK;
	}
	catch (FontException & fexptn)
	{
		// There was an error in initializing the font.
		FontErrorCode ferr = fexptn.errorCode;
		//int nVersion = fexptn.version;
		//int nSubVersion = fexptn.subVersion;
		HRESULT hr;
		std::wstring stuMsgBogus = FontLoadErrorDescription(ferr, 0, 0, &hr);
		return hr;
	}
	catch (...)
	{
		return kresUnexpected;
	}

	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Return an indication of whether the font is valid for the renderer.
	S_OK means it is valid, E_FAIL means the font was not available,
	E_UNEXPECTED means the font could not be used to initialize the renderer in the
	expected way (ie, the Graphite tables could not be found).

	Assumes InitNew() has already been called to set the font name.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::FontIsValid()
{
	BEGIN_COM_METHOD;

	if (m_pfont)
		return S_OK; // if the font is present it must be okay!
	else
		return E_FAIL;

	//if (!m_pfont)
	//{
	//	Assert(false);
	//	return E_UNEXPECTED;
	//}

	//int nVersion, nSubVersion;
	//FontErrorCode ferr = m_pfont->isValidForGraphite(&nVersion, &nSubVersion);
	//HRESULT hr;
	//std::wstring stuMsg = FontLoadErrorDescription(ferr, nVersion, nSubVersion, &hr);

	//if (FAILED(hr))
	//{
	//	StackDumper::RecordError(IID_IRenderEngine, stuMsg.data(), L"SIL.Graphite.FwGrEngine", 0, L"");
	//}
	//return hr;

	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Return the maximum size needed for the block of data to pass between segments, that is,
	to reinitialize the engine based on the results of the previously generated segment.
	This value must match what is in GrTableManager::InitializeStreams() and
	InitializeForNextSeg(). 256 is an absolute maximum imposed by the interface.

	In Graphite, what this block of data will contain is information about cross-line
	contextualization and some directionality information.

	Assumes InitNew() has already been called to set the font name.

	OBSOLETE
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::get_SegDatMaxLength(int * pcb)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcb);

	if (!m_pfont)
	{
		Assert(false);
		return E_UNEXPECTED;
	}

	// TODO: reimplement properly if we really need it.
	//return (HRESULT)m_pfont->GraphiteEngine()->get_SegDatMaxLength(pcb);
	*pcb = 256; // absolute maximum

	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	@return The supported script direction(s). If more than one, the application is
	responsible for choosing the most appropriate.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::get_ScriptDirection(int * pgrfsdc)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pgrfsdc);

	if (!m_pfont)
		return E_UNEXPECTED;

	*pgrfsdc = m_pfont->getSupportedScriptDirections();

	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	@return The class ID for the implementation class.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::get_ClassId(GUID * pguid)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pguid);

	memcpy(pguid, &CLSID_FwGrEngine, isizeof(GUID));

	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	The standard method for generating a segment in a paragraph, where a line break might be
	necessary.

	Generally called with fEndLine true; then change segment to make it false if we want to
	try to put something else after.

	TODO SharonC?: try implementing and see whether we can handle all the possible complications relating
	to adjacent segments that can't be separated, such as English parens around Hebrew text.

	@param pgjus		- NULL if no justification will ever be needed for the resulting segment
	@param ichMin/Lim			- part of string to use
	@param ichwLimBacktrack		- when backtracking, where to start looking for a new break
	@param fNeedFinalBreak		- if false, assume it is okay to make a segment ending at
									ichwLim; if true, assume the text source has at least one
									more character at ichwLim, and end the segment at ichwLim
									only if that is a valid break point
	@param fStartLine			- seg is logically first on line? (we assume it is logically last)
	@param dxMaxWidth			- available width in x coords of graphics object
	@param lbPref				- try for longest segment ending with this breakweight
	@param lbMax				- max (last resort) breakweight if no preferred break possible
	@param twsh					- how we are handling trailing white-space
	@param fParaRtl				- overall paragraph direction
	@param pplsegRet			- segment produced, or null if nothing fits
	@param pichwLimSeg			- end of segment produced, beginning of next
	@param pdxWidth				- width of newly-created segment
	@param pest					- what caused the segment to end
	@param cpPrev				- byte size of pbPrevSegDat buffer
	@param pbPrevSegDat			- for initializing from previous segment
	@param cbNextMax			- max size of pbNextSegDat buffer
	@param pbNextSegDat			- for initializing next segment
	@param pcbNextSegDat		- size of pbNextSegDat buffer
	@param pdichwContext		- for the following segment, the index of the first char of
									interest to it; ie, edits before this character will not
									affect how the next segment behaves
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::FindBreakPoint(
	IVwGraphics * pvg, IVwTextSource * pts, IVwJustifier * pvjus,
	int ichwMin, int ichwLim, int ichwLimBacktrack,
	ComBool fNeedFinalBreak,
	ComBool fStartLine,
	int dxMaxWidth,
	LgLineBreak lbPref, LgLineBreak lbMax,
	LgTrailingWsHandling twsh, ComBool fParaRtl,
	ILgSegment ** ppsegRet,
	int * pdichwLimSeg,
	int * pdxWidth, LgEndSegmentType * pest,
	ILgSegment * plgsegPrev)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvg);
	ChkComArgPtr(pts);
	ChkComArgPtrN(pvjus);
	ChkComOutPtr(ppsegRet);
	ChkComOutPtr(pdichwLimSeg);
	ChkComArgPtr(pdxWidth);
	ChkComArgPtr(pest);

	HRESULT hr = S_OK;

	// Use wrappers that fit what is expected by the Graphite implementation code:
	//////FwGrGraphics gg(pvg);

	// Create these on the heap so they can be stored in the segment.
	FwGrTxtSrc * pgts = new FwGrTxtSrc(pts, m_useNFC);
	//FwGrJustifier * pgjus = (pvjus) ? new FwGrJustifier(pvjus) : NULL;
	if (pvjus && !s_pgjus)
	{
		s_pgjus = new FwGrJustifier(pvjus);
		m_pgjus = s_pgjus; // make this engine responsible for deleting it
	}

	FwGrSegmentPtr qfwgrseg;
	if (*ppsegRet)
	{
		return E_INVALIDARG;
		//CheckHr((*ppsegRet)->QueryInterface(CLSID_FwGrSegment, (void**)&qfwgrseg));
		//pgrseg = qfwgrseg->GraphiteSegment();
	}

	Segment * psegPrev = NULL;
	if (plgsegPrev)
	{
		FwGrSegment * pfwgrseg = dynamic_cast<FwGrSegment *>(plgsegPrev);
		if (pfwgrseg)
			psegPrev = pfwgrseg->GraphiteSegment();
		// otherwise, not a Graphite segment
	}

	// Need-final-break is true if and only if we are backtracking.
	Assert((ichwLim != ichwLimBacktrack) == fNeedFinalBreak);

	// Adjust the font in the graphics device for super/subscripting.

	SetUpGraphics(pvg, pgts, ichwMin);

	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));
	////gg.GetDeviceContext(&hdc);

	Segment * pgrseg;
	WinFont * pfontSeg;
	try
	{
		// DON'T use m_pfont, which might be for the wrong size; use a new font
		// specifically for this size font.
		FwWinFont font(hdc);
		int xInch, yInch;
		pvg->get_XUnitsPerInch(&xInch);
		pvg->get_YUnitsPerInch(&yInch);
		font.SetDPI(xInch, yInch);

		std::ofstream strmLog;
		std::ostream * pstrmLog = CreateLogFile(strmLog, (psegPrev != NULL));

		// for testing UTF-8:
		//gr::utf16 prgchw[1000];
		//int cchwLen;
		//pts->get_Length(&cchwLen);
		//pts->Fetch(ichwMin, cchwLen, prgchw);
		//ichwLim = CountUtf8FromUtf16(prgchw, ichwLim);
		//ichwLimBacktrack = ichwLim;
		/////////////////////

		LayoutEnvironment layout;
		layout.setStartOfLine(fStartLine);
		layout.setEndOfLine(true);
		layout.setBestBreak(lbPref);
		layout.setWorstBreak(lbMax);
		layout.setRightToLeft(fParaRtl);
		layout.setTrailingWs(twsh);
		layout.setPrevSegment(psegPrev);
		layout.setJustifier(s_pgjus);
		layout.setLoggingStream(pstrmLog);
		layout.setDumbFallback(true);

		//for allowing hyphen breaks:
		//layout.setBestBreak(max(klbHyphenBreak, lbPref));
		//layout.setWorstBreak(max(klbHyphenBreak, lbMax));

		bool fBacktracking = (ichwLimBacktrack < ichwLim);
		int grIchwMin = pgts->VwToGrOffset(ichwMin);
		int grIchwLim = fBacktracking ? pgts->VwToGrOffset(ichwLimBacktrack) : pgts->VwToGrOffset(ichwLim);

		pgrseg = new LineFillSegment(&font, pgts, &layout,
			grIchwMin, grIchwLim,
			(float)dxMaxWidth, fBacktracking);
		gr::Font & fontSeg = pgrseg->getFont();
		pfontSeg = dynamic_cast<WinFont *>(&fontSeg);
		pfontSeg->replaceDC(hdc);
		*pest = pgrseg->segmentTermination();
		if (*pest != kestNothingFit)
		{
			int grIchwLimSeg = pgrseg->stopCharacter() - pgrseg->startCharacter();
			*pdichwLimSeg = pgts->GrToVwOffset(grIchwLimSeg);
			*pdxWidth = gr::GrEngine::RoundFloat(pgrseg->advanceWidth());
			// there is a limit in the number of pixels (about 2^16) that the ExtTextOut function
			// can render, which is what Graphite uses to render text on Windows. If this
			// segment is over that limit, we reduce the number of characters in this segment
			// so that when it is rendered it is less than the limit. The main place this
			// can happen is in concordance views.
			// TODO (DamienD): This fix removes characters from the end of the segment. If the
			// segment is too long on the left side of the arrow in the concordance view,
			// characters should be removed from the beginning. This case would have to be
			// handled in Views somewhere.
			// TODO (DamienD): If Graphite ever fixes the limit, we can safely remove this
			// hack.
			if (*pdxWidth > SHRT_MAX)
			{
				delete pgrseg;
				int avgCharWidth = *pdxWidth / (grIchwMin + grIchwLimSeg);
				// we use 30000 here, because avgCharWidth is just an estimate,
				// this gives us some padding to ensure that the resulting segment
				// is less than the limit
				grIchwLim = grIchwMin + (30000 / avgCharWidth);
				pgrseg = new LineFillSegment(&font, pgts, &layout,
					grIchwMin, grIchwLim,
					(float)dxMaxWidth, fBacktracking);

				// reset variables for new segment
				gr::Font & fontSeg = pgrseg->getFont();
				pfontSeg = dynamic_cast<WinFont *>(&fontSeg);
				pfontSeg->replaceDC(hdc);

				*pest = pgrseg->segmentTermination();
				if (*pest != kestNothingFit)
				{
					*pdichwLimSeg = pgts->GrToVwOffset(pgrseg->stopCharacter() - pgrseg->startCharacter());
					*pdxWidth = gr::GrEngine::RoundFloat(pgrseg->advanceWidth());
				}
			}
		}

		strmLog.close();
	}
	catch (FontException & fexptn)
	{
		// Error in initializing the font.
		FontErrorCode ferr = fexptn.errorCode;
		LgCharRenderProps chrp;
		int ichwMinBogus, ichwLimBogus;
		pgts->GetCharProps(ichwMin, &chrp, &ichwMinBogus, &ichwLimBogus);
		StrUni stuMsg = L"Error in initializing Graphite font ";
		stuMsg.Append(chrp.szFaceName);
		stuMsg.Append(": ");
		std::wstring stuErrMsg = FontLoadErrorDescription(ferr, 0, 0, &hr);
		stuMsg.Append(stuErrMsg.c_str());
		StackDumper::RecordError(IID_IRenderEngine, stuMsg, L"SIL.Graphite.FwGrEngine", 0, L"");
		return hr;
	}

	// if we are at the end of the requested range, but the text source still has more text in
	// it, Graphite will determine that the break was bad or okay, even though it broke
	// because there was no more text, so we go ahead and change the reason here to no more text
	if (ichwMin + *pdichwLimSeg == ichwLim && (*pest == kestOkayBreak || *pest == kestBadBreak))
		*pest = kestNoMore;

	bool fError = false;
	std::pair<GlyphIterator, GlyphIterator> pairGfit = pgrseg->glyphs();
	GlyphIterator gfit = pairGfit.first;
	GlyphIterator gfitEnd = pairGfit.second;
	for ( ; gfit != gfitEnd ; ++gfit)
	{
		if ((*gfit).erroneous())
		{
			fError = true;
			break;
		}
	}

	if (fError)
	{
		LgCharRenderProps chrp;
		int ichwMinBogus, ichwLimBogus;
		pgts->GetCharProps(ichwMin, &chrp, &ichwMinBogus, &ichwLimBogus);
		StrUni stuMsg = L"Error in Graphite rendering using font ";
		stuMsg.Append(chrp.szFaceName);
		StackDumper::RecordError(IID_IRenderEngine, stuMsg, L"SIL.Graphite.FwGrEngine", 0, L"");
		hr = E_FAIL;
	}

	//// TEMPORARY - for testing
	//pairGfit = pgrseg->glyphs();
	//gfit = pairGfit.first;
	//gfitEnd = pairGfit.second;
	//for ( ; gfit != gfitEnd ; ++gfit)
	//{
	//	GlyphInfo ginf = *gfit;
	//	gid16 gid = ginf.glyphID();
	//	gid = ginf.pseudoGlyphID();

	//	int n = ginf.logicalIndex();
	//	float xy = ginf.origin();
	//	xy = ginf.advanceWidth();
	//	xy = ginf.advanceHeight();
	//	xy = ginf.yOffset();
	//	gr::Rect bb = ginf.bb();
	//	bool f = ginf.isSpace();
	//	f = ginf.insertBefore();
	//	toffset ich = ginf.firstChar();
	//	int ich = ginf.lastChar();
	//	unsigned int dir = ginf.directionality();
	//	dir = ginf.directionLevel();
	//	n = ginf.attachedTo();

	//	n = ginf.numberOfComponents();
	//	for (int i = 0; i < n; i++)
	//	{
	//		bb = ginf.componentBox(i);
	//		ich = ginf.componentFirstChar(i);
	//	}

	//	std::pair<GlyphIterator, GlyphIterator> pairGlyphRange
	//		= pgrseg->charToGlyphs(ginf.firstChar());

	//	for ( ; pairGlyphRange.first != pairGlyphRange.second ; ++(pairGlyphRange.first))
	//	{
	//		ginf = *pairGlyphRange.first;
	//		gid = ginf.glyphID();
	//	}
	//}

	//// for testing:
	//int rgigbb[100];
	//bool rgfClusterStart[100];
	//int cch, cf;
	//pgrseg->getUniscribeClusters(rgigbb, 100, &cch, rgfClusterStart, 100, &cf);

	//// for testing
	//for (int dxWidth = 50; dxWidth < 500; dxWidth += 50)
	//{
	//	int ichBreak;
	//	float dxRetWidth;
	//	ichBreak = pgrseg->findNextBreakPoint(4, klbWsBreak, klbWsBreak, (float)dxWidth, &dxRetWidth, false, false);
	//	int x;
	//	x = 3;
	//}

	pfontSeg->restoreDC();

	// Even if there was an error, if a segment was created, we want to return it.

	if (pgrseg && (*pest != kestNothingFit))
	{
		////qfwgrseg = dynamic_cast<FwGrSegment *>(pgrseg);
		////Assert(qfwgrseg);

		FwGrSegment * psegTmp;
		if (!qfwgrseg)
		{
			psegTmp = NewObj FwGrSegment;
			qfwgrseg = dynamic_cast<FwGrSegment *>(psegTmp);
			Assert(qfwgrseg);
		}
		else
			psegTmp = qfwgrseg.Ptr();

		qfwgrseg->SetGraphiteSegment(pgrseg);

		HRESULT hrTmp;
		CheckHr(hrTmp = qfwgrseg->QueryInterface(IID_ILgSegment, (void **)ppsegRet));
		if (FAILED(hrTmp))
			hr = hrTmp;
		////pgrseg->DecRefCount();
		psegTmp->Release();

		qfwgrseg->SetFwGrEngine(this);
		qfwgrseg->SetTextSource(pgts);

		//pgts->IncRefCount();	// not needed; pgts holds a smart pointer that increments
								// the ref count on the FW text source
	}
	else
	{
		delete pgrseg;
		delete pgts;
		qfwgrseg = NULL;
	}

	return hr;

	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Return the writing system factory for this database (or the registry, as the case may be).

	@param ppwsf Address of the pointer for returning the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(ppwsf);
	*ppwsf = m_qwsf;
	(*ppwsf)->AddRef();
	END_COM_METHOD(g_fact, IID_IRenderEngine)
}

/*----------------------------------------------------------------------------------------------
	Set the writing system factory for this database (or the registry, as the case may be).

	@param pwsf Pointer to the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FwGrEngine::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD
	ChkComArgPtrN(pwsf);

	m_qwsf = pwsf;

	END_COM_METHOD(g_fact, IID_IRenderEngine)
}


//:>********************************************************************************************
//:>	   IJustifyingRenderer methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get an glyph attribute from the engine that will help the IVwJustifier in its work.

	DELETE
----------------------------------------------------------------------------------------------*/
//STDMETHODIMP FwGrEngine::GetGlyphAttribute(int iGlyph, int kjgatId, int nLevel,
//	float * pValueRet)
//{
//	BEGIN_COM_METHOD
//	ChkComArgPtr(pValueRet);
//
//	Assert(false);
//
//	return (HRESULT)m_pfont->GraphiteEngine()->
//		GetGlyphAttribute(iGlyph, kjgatId, nLevel, pValueRet);
//
//	END_COM_METHOD(g_fact, IID_IJustifyingRenderer);
//}

/*----------------------------------------------------------------------------------------------
	Set an glyph attribute in the engine as a result of the decisions made by the
	IVwJustifier.

	DELETE
----------------------------------------------------------------------------------------------*/
//STDMETHODIMP FwGrEngine::SetGlyphAttribute(int iGlyph, int kjgatId, int nLevel, float value)
//{
//	BEGIN_COM_METHOD
//
//	Assert(false);
//
//	return (HRESULT)m_pfont->GraphiteEngine()->
//		SetGlyphAttribute(iGlyph, kjgatId, nLevel, value);
//
//	END_COM_METHOD(g_fact, IID_IJustifyingRenderer);
//}


//:>********************************************************************************************
//:>	Other public methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Create the kind of segment we want.  -- REMOVE
----------------------------------------------------------------------------------------------*/
//void FwGrEngine::NewSegment(Segment ** ppseg)
//{
//	*ppseg = NewObj FwGrSegment;
//}

//:>********************************************************************************************
//:>	Private methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Parse the magic string that indicates the feature values as used by FieldWorks.
	It is of the form:	"123=2,456=-4,..." (ie, <id>=<value>).
	In addition to integer values, it is possible to have a 4-character quoted string
	that is converted to an integer: 1="ENG1"
	Where syntax errors are encountered, simply skip that setting.
----------------------------------------------------------------------------------------------*/
int FwGrEngine::ParseFeatureString(gr::utf16 * strFeatures, int cchLen,
	FeatureSetting * prgfset)
{
	gr::utf16 * pchw = strFeatures;
	gr::utf16 * pchwLim = pchw + cchLen;
	int ifeat = 0;
	while (pchw < pchwLim)
	{
		int nID = 0;
		int nValue = 0;
		bool fNeg = false;

		//	Read the ID.
		while (*pchw != '=' && *pchw != ' ')
		{
			if (*pchw < '0' || *pchw > '9')
				goto LNext;	// syntax error: skip this setting
			nID = nID * 10 + (*pchw - '0');
			pchw++;
		}
		while (*pchw == ' ')
			pchw++;
		Assert(*pchw == '=');
		pchw++;
		while (*pchw == ' ')
			pchw++;

		//	Read the value.
		if (*pchw == '"')
		{
			//	Language ID string--form an integer out of the first four bytes, ignore
			//	the rest.
			pchw++;	// skip quote
			byte b1 = (*pchw != '"') ? *pchw++ : 0;
			byte b2 = (*pchw != '"') ? *pchw++ : 0;
			byte b3 = (*pchw != '"') ? *pchw++ : 0;
			byte b4 = (*pchw != '"') ? *pchw++ : 0;
			while (pchw < pchwLim  && *pchw != '"')	// skip superfluous chars
				pchw++;
			if (pchw >= pchwLim)
				goto LNext;
			pchw++;	// skip quote
			nValue = (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;
		}
		else
		{
			//	Numerical value
			if (*pchw == '-')
			{
				pchw++;
				fNeg = true;
			}
			else if (*pchw == '+')
			{
				pchw++;
				fNeg = false;
			}
			while (*pchw != ',' && *pchw != ' ' && pchw < pchwLim)
			{
				if (*pchw < '0' || *pchw > '9')
					goto LNext;	// syntax error skip this setting
				nValue = nValue * 10 + (*pchw - '0');
				pchw++;
			}
			if (fNeg)
				nValue = nValue * -1;
		}

		//	Set the feature value.
		prgfset[ifeat].id = nID;
		prgfset[ifeat].value = nValue;
		ifeat++;

LNext:
		//	Find the next setting.
		while (pchw < pchwLim && *pchw != ',')
			pchw++;
		while (pchw < pchwLim && (*pchw < '0' || *pchw > '9'))
			pchw++;
	}

	return ifeat;
}

/*----------------------------------------------------------------------------------------------
	Initialize the graphics object with the font and character properties.
----------------------------------------------------------------------------------------------*/
void FwGrEngine::SetUpGraphics(IVwGraphics * pvg, FwGrTxtSrc * pfgts, int ichwMin)
{
	LgCharRenderProps chrp;
	//AdjustFontAndStyles(pvg, pgts, ichwMin, &chrp);
	int ichMinBogus, ichLimBogus;
	pfgts->GetCharProps(ichwMin, &chrp, &ichMinBogus, &ichLimBogus);
	if (m_szFaceName)
		memcpy(chrp.szFaceName, m_szFaceName, isizeof(chrp.szFaceName));

	// Adjust the size for super/subscripting.
	float adjustedSize = pfgts->GetFontSize(ichwMin);
	chrp.dympHeight = (int)(adjustedSize * 1000);

	try
	{
		CheckHr(pvg->SetupGraphics(&chrp));
	}
	catch(...)
	{	// This code is unlikely to execute since even if GrGraphics::SetupGraphics can't
		// find an exact font match it will make its "best guess".
		wcscpy(chrp.szFaceName, L"Arial");
		CheckHr(pvg->SetupGraphics(&chrp));
	}
}

/*----------------------------------------------------------------------------------------------
	Generate the name of the file where the log will be written. It goes in the directory
	defined by either the TEMP or TMP environment variable.
----------------------------------------------------------------------------------------------*/
std::ofstream * FwGrEngine::CreateLogFile(std::ofstream & strmLog, bool fAppend)
{
	if (!m_fLog)
		return NULL;

	std::string staFile;
	if (!LogFileName(staFile))
		return NULL; // can't figure out where to put the log file

	if (fAppend)
		strmLog.open(staFile.c_str(), std::ios::app);	// append
	else
		strmLog.open(staFile.c_str());

	return &strmLog;
}

/*----------------------------------------------------------------------------------------------
	Generate the name of the file where the log will be written. It goes in the directory
	defined by either the TEMP or TMP environment variable.
----------------------------------------------------------------------------------------------*/
bool FwGrEngine::LogFileName(std::string & staFile)
{
	char * pchTmpEnv = getenv("TEMP");
	if (pchTmpEnv == 0)
		pchTmpEnv = getenv("TMP");
	if (pchTmpEnv == 0)
		return false;

	staFile.assign(pchTmpEnv);
	if (staFile[staFile.size() - 1] != '\\')
		staFile.append("\\");
	staFile.append("gr_xductn.log");

	return true;
}

/*----------------------------------------------------------------------------------------------
	Return a string describing the error that occurred in loading the Graphite tables.
	Also return an appropriate HRESULT.
----------------------------------------------------------------------------------------------*/
std::wstring FwGrEngine::FontLoadErrorDescription(FontErrorCode ferr,
	int nVersion, int nSubVersion, HRESULT * phr)
{
	std::wstring stuRet;
	*phr = E_UNEXPECTED;

	switch (ferr)
	{
	case kferrOkay:
		stuRet = L"";
		*phr = S_OK;
		break;
	case kferrUninitialized:
		stuRet = L"Graphite font not initialized";
		break;
	case kferrUnknown:
		stuRet = L"unknown error in initializing Graphite font";
		break;
	case kferrFindHeadTable:
		stuRet = L"could not locate head table for Graphite rendering";
		break;
	case kferrReadDesignUnits:
		stuRet = L"could not read design units for Graphite rendering";
		break;
	case kferrFindCmapTable:
		stuRet = L"could not locate cmap table";
		break;
	case kferrLoadCmapSubtable:
		stuRet = L"failure to load cmap subtable";
		break;
	case kferrCheckCmapSubtable:
		stuRet = L"checking cmap subtable failed";
		break;
	case kferrFindNameTable:
		stuRet = L"could not locate name table";
		break;
	case kferrLoadSilfTable:
		stuRet = L"could not load Silf table for Graphite rendering";
		*phr = E_FAIL;
		break;
	case kferrLoadFeatTable:
		stuRet = L"could not load Feat table for Graphite rendering";
		break;
	case kferrLoadGlatTable:
		stuRet = L"could not load Glat table for Graphite rendering";
		break;
	case kferrLoadGlocTable:
		stuRet = L"could not load Gloc table for Graphite rendering";
		break;
	case kferrBadVersion:
		{
		wchar_t rgch[20];
		swprintf(rgch, 20, L"%d", nVersion);
		stuRet = L"unsupported version (";
		stuRet.append(rgch);
		swprintf(rgch, 20, L"%d", nSubVersion);
		stuRet.append(L".");
		stuRet.append(rgch);
		stuRet.append(L") of Graphite tables");
		break;
		}
	case kferrReadSilfTable:
		stuRet = L"error reading Silf table";
		break;
	case kferrReadGlocGlatTable:
		stuRet = L"error reading Gloc and Glat tables";
		break;
	case kferrReadFeatTable:
		stuRet = L"error reading Feat table";
		break;
	case kferrReadSillTable:
		stuRet = L"error reading Sill table";
		break;
	default:
		stuRet = L"error in initializing Graphite font--invalid error code";
		break;
	}
	return stuRet;
}

/*----------------------------------------------------------------------------------------------
	Set the character properties font to the one this engine is using, regardless of what was
	requested.
----------------------------------------------------------------------------------------------*/
//void FwGrEngine::AdjustFontAndStyles(IVwGraphics * pvg, IVwTextSource * pts,
//	int ichwMin, LgCharRenderProps * pchrp)
//{
//	// TODO: get rid of GrCharProps when graphics object no longer needs it.
//	int ichMinBogus, ichLimBogus;
//	pts->GetCharProps(ichwMin, pchrp, &ichMinBogus, &ichLimBogus);
//
//	////utf16 rgchwFace[32];
//	////for (int ich = 0; ich < 32; ich++)
//	////	rgchwFace[ich] = stuFaceName[ich];
//	////pgts->InterpretChrp(pchrp, rgchwFace);
//
//	pchrp->dympHeight = (int)pts->GetFontSize(ichwMin) * 1000;
//	pchrp->dympOffset = (int)pts->getVerticalOffset(ichwMin) * 1000;
//
//	//////int yInch;
//	//////pgg->get_YUnitsPerInch(&yInch);
//	//////m_dysOffset = GrMulDiv(pchrp->dympOffset, yInch, 72000); // 72000 = millipoints per inch
//
//	// The calling application may very well send an empty string representing an invalid
//	// font. Fall back to something we expect they'll have.
//	////if (stuFaceName.size() == 0)
//	////	stuFaceName = L"Arial";
//
//	////int cchwCopy = min(signed(stuFaceName.size() + 1), 32);
//	////wcsncpy(pchrp->szFaceName, stuFaceName.data(), cchwCopy);
//	////pchrp->szFaceName[31] = 0; // ensure null termination even for long path
//}
