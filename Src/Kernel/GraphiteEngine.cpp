/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GraphiteEngine.cpp
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

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

GraphiteEngine::GraphiteEngine()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_face = NULL;
	m_featureValues = NULL;
	m_defaultFeatureValues = NULL;
}

GraphiteEngine::~GraphiteEngine()
{
	if (m_featureValues != NULL)
		gr_featureval_destroy(m_featureValues);
	if (m_defaultFeatureValues != NULL)
		gr_featureval_destroy(m_defaultFeatureValues);
	if (m_face != NULL)
		gr_face_destroy(m_face);
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language1.GraphiteEngine"),
	&CLSID_GraphiteEngine,
	_T("SIL Graphite wrapper"),
	_T("Apartment"),
	&GraphiteEngine::CreateCom);


void GraphiteEngine::CreateCom(IUnknown* punkCtl, REFIID riid, void** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<GraphiteEngine> qrre;
	qrre.Attach(NewObj GraphiteEngine());		// ref count initialy 1
	CheckHr(qrre->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP GraphiteEngine::QueryInterface(REFIID riid, void** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown*>(static_cast<IRenderEngine*>(this));
	else if (riid == IID_IRenderEngine)
		*ppv = static_cast<IRenderEngine*>(this);
	else if (riid == IID_IRenderingFeatures)
		*ppv = static_cast<IRenderingFeatures*>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<IRenderEngine*>(this),
			IID_ISimpleInit, IID_IRenderEngine);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	   IRenderingFeature methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Return a list of the feature IDs. If cMax is zero, pcfid returns the number of features
	supported. Otherwise it contains the number put into the array.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteEngine::GetFeatureIDs(int cMax, int* prgFids, int* pcfid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcfid);
	ChkComArrayArg(prgFids, cMax);

	if (m_face == NULL)
	{
		Assert(false);
		return E_UNEXPECTED;
	}

	int numFeatures = gr_face_n_fref(m_face);
	if (cMax == 0)
	{
		*pcfid = numFeatures;
		return S_OK;
	}

	*pcfid = min(cMax, numFeatures);
	for (gr_uint16 i = 0; i < *pcfid; i++)
		prgFids[i] = gr_fref_id(gr_face_fref(m_face, i));

	END_COM_METHOD(g_fact, IID_IRenderingFeatures);
}

/*----------------------------------------------------------------------------------------------
	Return the UI label for the given feature, in the given language.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteEngine::GetFeatureLabel(int fid, int nLanguage, BSTR* pbstrLabel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrLabel);

	if (m_face == NULL)
	{
		Assert(false);
		return E_UNEXPECTED;
	}

	const gr_feature_ref* feature = gr_face_find_fref(m_face, fid);
	gr_uint16 lang = gr_uint16(nLanguage);
	gr_uint32 len;
	void* label = gr_fref_label(feature, &lang, gr_utf16, &len);

	*pbstrLabel = ::SysAllocStringLen((OLECHAR*) label, len);

	gr_label_destroy(label);

	END_COM_METHOD(g_fact, IID_IRenderingFeatures);
}

/*----------------------------------------------------------------------------------------------
	Return a list of recognized values for the given feature. If cfvalMax is zero,
	pcfval returns the total number of values. Otherwise, it returns the number entered into
	the array.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteEngine::GetFeatureValues(int fid, int cfvalMax,
	int* prgfval, int* pcfval, int* pfvalDefault)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcfval);
	ChkComOutPtr(pfvalDefault);
	ChkComArrayArg(prgfval, cfvalMax);

	if (m_face == NULL)
	{
		Assert(false);
		return E_UNEXPECTED;
	}

	const gr_feature_ref* feature = gr_face_find_fref(m_face, fid);
	int numValues = gr_fref_n_values(feature);
	if (cfvalMax == 0)
	{
		*pcfval = numValues;
		return S_OK;
	}

	*pcfval = min(cfvalMax, numValues);
	for (gr_uint16 i = 0; i < *pcfval; i++)
		prgfval[i] = gr_fref_value(feature, i);

	if (m_defaultFeatureValues == NULL)
		m_defaultFeatureValues = gr_face_featureval_for_lang(m_face, 0);
	*pfvalDefault = gr_fref_feature_value(feature, m_defaultFeatureValues);

	END_COM_METHOD(g_fact, IID_IRenderingFeatures);
}

/*----------------------------------------------------------------------------------------------
	Return the UI label for the given feature value.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteEngine::GetFeatureValueLabel(int fid, int fval, int nLanguage,
	BSTR* pbstrLabel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrLabel);

	if (m_face == NULL)
	{
		Assert(false);
		return E_UNEXPECTED;
	}

	const gr_feature_ref* feature = gr_face_find_fref(m_face, fid);
	int numValues = gr_fref_n_values(feature);
	for (gr_uint16 i = 0; i < numValues; i++)
	{
		if (gr_fref_value(feature, i) == fval)
		{
			gr_uint16 lang = gr_uint16(nLanguage);
			gr_uint32 len;
			void* label = gr_fref_value_label(feature, i, &lang, gr_utf16, &len);

			*pbstrLabel = ::SysAllocStringLen((OLECHAR*) label, len);

			gr_label_destroy(label);
			break;
		}
	}

	END_COM_METHOD(g_fact, IID_IRenderingFeatures);
}

//:>********************************************************************************************
//:>	   IRenderEngine methods
//:>********************************************************************************************

const void* GetFontTable(const void* appFaceHandle, unsigned int name, size_t *len)
{
	IVwGraphics* pvg = (IVwGraphics*) appFaceHandle;
	int bufLen = 0;
	CheckHr(pvg->GetFontData(name, &bufLen, NULL));
	BYTE* tableBuffer = new BYTE[bufLen];
	CheckHr(pvg->GetFontData(name, &bufLen, tableBuffer));
	*len = bufLen;
	return tableBuffer;
}

void ReleaseFontTable(const void* appFaceHandle, const void* tableBuffer)
{
	delete[] (BYTE*) tableBuffer;
}

/*----------------------------------------------------------------------------------------------
	Initialize the engine. This must be called before any oher methods of the interface.
	How the data is used is implementation dependent. The UniscribeRenderer does not
	use it at all.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteEngine::InitRenderer(IVwGraphics * pvg, BSTR bstrData)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvg);

	gr_face_ops faceOps;
	faceOps.size = sizeof(gr_face_ops);
	faceOps.get_table = &GetFontTable;
	faceOps.release_table = &ReleaseFontTable;

	m_face = gr_make_face_with_ops(pvg, &faceOps, gr_face_preloadAll);
	if (m_face != NULL && bstrData != NULL)
		ParseFeatureString(bstrData);

	END_COM_METHOD(g_fact, IID_IRenderEngine);
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
STDMETHODIMP GraphiteEngine::FontIsValid()
{
	BEGIN_COM_METHOD
	if (m_face == NULL)
		return E_UNEXPECTED;
	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Give the maximum length of information that this renderer might want to pass
	from one segment to another in SimpleBreakPoint>>pbNextSegDat.
	UniSeg never passes info from one segment to another.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteEngine::get_SegDatMaxLength(int * cb)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(cb);
	*cb = 0;
	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Return the support script directions. The GraphiteRenderer can do horizontal in either
	direction.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteEngine::get_ScriptDirection(int * pgrfsdc)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pgrfsdc);
	*pgrfsdc = kfsdcHorizLtr | kfsdcHorizRtl;
	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Return the class ID for the implementation class.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteEngine::get_ClassId(GUID * pguid)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pguid);
	memcpy(pguid, &CLSID_GraphiteEngine, isizeof(GUID));
	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Make a segment by finding a suitable break point in the specified range of text.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteEngine::FindBreakPoint(
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

	int ichInterestLim = min(ichLimBacktrack + 1, ichLimText);
	int interestLen = ichInterestLim - ichMinSeg;
	StrUni segStr;
	OLECHAR* pchNfd;
	segStr.SetSize(interestLen + 1, &pchNfd);
	CheckHr(pts->Fetch(ichMinSeg, ichInterestLim, pchNfd));
	pchNfd[interestLen] = '\0';

	LgCharRenderProps chrp;
	int ichMinRun, ichLimRun;
	CheckHr(pts->GetCharProps(ichMinSeg, &chrp, &ichMinRun, &ichLimRun));
	InterpretChrp(chrp);
	int ws = chrp.ws;

	ILgCharacterPropertyEnginePtr qcpe;
	CheckHr(m_qwsf->get_CharPropEngine(ws, &qcpe));

	int segmentLen = ichLimBacktrack - ichMinSeg;
	LgEndSegmentType est = kestNoMore;
	int i = 0;
	bool extraSlot = false;
	while (i < segmentLen)
	{
		LgCharRenderProps runChrp;
		CheckHr(pts->GetCharProps(ichMinSeg + i, &runChrp, &ichMinRun, &ichLimRun));
		if (runChrp.ws != ws)
		{
			// change in writing system so break
			est = kestWsBreak;
			segmentLen = i;
			break;
		}
		else if (i != 0)
		{
			// change in chrp so break
			est = kestOkayBreak;
			segmentLen = i;
			// we need to check if this is good break
			extraSlot = true;
			break;
		}

		for (; i < min(ichLimRun, ichLimBacktrack) - ichMinSeg; i++)
		{
			if (segStr[i] == '\n' || segStr[i] == '\t' || segStr[i] == '\r' || segStr[i] == 0xfffc || segStr[i] == 0x2028)
			{
				est = kestHardBreak;
				segmentLen = i;
				break;
			}
			if (twsh == ktwshOnlyWs)
			{
				// break if we hit non-whitespace
				LgBidiCategory bic;
				CheckHr(qcpe->get_BidiCategory(segStr[i], &bic));
				if (bic != kbicWS)
				{
					est = kestOkayBreak;
					segmentLen = i;
					break;
				}
			}
		}

		if (est != kestNoMore)
			break;
	}

	// if we are backtracking, we need to check if this is a good break, since we are not at the end of the line
	if (est == kestNoMore && segmentLen < interestLen)
		extraSlot = true;

	int dpiY;
	CheckHr(pvg->get_YUnitsPerInch(&dpiY));

	gr_font* font = gr_make_font(float(MulDiv(chrp.dympHeight, dpiY, kdzmpInch)), m_face);
	gr_segment* segment;
	if (font != NULL)
		segment = gr_make_seg(font, m_face, 0, m_featureValues, gr_utf16, segStr, segmentLen + (extraSlot ? 1 : 0), fParaRtoL ? gr_rtl : 0);
	if (segment == NULL && font != NULL)
	{
		gr_font_destroy(font);
		font = NULL;
	}
	if (font == NULL)
	{
		printf("FieldWorks has encountered an unusual text rendering problem and may be unable to continue.\n");
		fflush(stdout);
		ThrowHr(WarnHr(E_FAIL));
	}

	const gr_slot* end = NULL;
	const gr_slot* breakSlot = NULL;
	if (extraSlot)
	{
		end = gr_seg_last_slot(segment);
		int breakWeight = BreakWeightBefore(end, segment);
		if (breakWeight <= lbMax)
		{
			// okay place to break
			est = kestOkayBreak;
		}
		else
		{
			if (est == kestNoMore && fNeedFinalBreak)
			{
				// we are backtracking and this is bad place to break so search for a better place
				breakSlot = end;
			}
			else
			{
				est = kestBadBreak;
			}
		}
	}

	int width = 0;
	for (const gr_slot* s = gr_seg_first_slot(segment); s != end; s = gr_slot_next_in_segment(s))
	{
		if (width > dxMaxWidth)
		{
			breakSlot = s;
			break;
		}
		width += Round(gr_slot_advance_X(s, m_face, font));
	}

	if (breakSlot != NULL)
	{
		const gr_slot* fallbackSlot = NULL;
		int fallbackWeight = lbMax;
		const gr_slot* s = breakSlot;
		while (s != NULL)
		{
			int breakWeight = BreakWeightBefore(s, segment);
			if (breakWeight <= lbPref)
			{
				break;
			}
			else if (breakWeight <= lbMax && breakWeight < fallbackWeight)
			{
				fallbackSlot = s;
				fallbackWeight = breakWeight;
			}
			s = gr_slot_prev_in_segment(s);
		}

		if (s != NULL)
			// preferred break
			breakSlot = s;
		else if (fallbackSlot != NULL)
			// acceptable break
			breakSlot = fallbackSlot;
		else
			// nothing fit
			breakSlot = gr_seg_first_slot(segment);
		est = kestMoreLines;
	}

	if (twsh == ktwshNoWs)
	{
		// attempt to strip off trailing whitespace
		const gr_slot* wsSlot = NULL;
		const gr_slot* s;
		if (breakSlot == NULL)
		{
			s = end == NULL ? gr_seg_last_slot(segment) : gr_slot_prev_in_segment(end);
		}
		else
		{
			s = gr_slot_prev_in_segment(breakSlot);
		}
		while (s != NULL)
		{
			int ich = gr_cinfo_base(gr_seg_cinfo(segment, gr_slot_before(s)));
			LgBidiCategory bic;
			CheckHr(qcpe->get_BidiCategory(segStr[ich], &bic));
			if (bic != kbicWS)
				break;

			wsSlot = s;
			s = gr_slot_prev_in_segment(s);
		}

		if (wsSlot != NULL)
		{
			breakSlot = wsSlot;
			// we can fit more white space on the line
			est = kestMoreWhtsp;
		}
	}

	if (breakSlot != NULL)
	{
		// we had to break somewhere before the end so recalculate width and segment length
		width = 0;
		for (const gr_slot* s = gr_seg_first_slot(segment); s != breakSlot; s = gr_slot_next_in_segment(s))
			width += Round(gr_slot_advance_X(s, m_face, font));
		segmentLen = gr_cinfo_base(gr_seg_cinfo(segment, gr_slot_before(breakSlot)));
	}

	*pdxWidth = width;
	*pdichLimSeg = segmentLen;
	*pest = est;

	if (ichMinSeg < ichLimBacktrack && breakSlot == gr_seg_first_slot(segment))
	{
		// Views expects a NULL segment when a non-zero length segment was requested and nothing fit
		*ppsegRet = NULL;
	}
	else
	{
		int dirDepth = chrp.nDirDepth;
		if (fParaRtoL && dirDepth == 0)
			dirDepth = 2;

		bool wsOnly = segmentLen > 0;
		if (twsh != ktwshOnlyWs)
		{
			// check if this is a whitespace only segment
			for (i = 0; i < segmentLen; i++)
			{
				LgBidiCategory bic;
				CheckHr(qcpe->get_BidiCategory(segStr[i], &bic));
				if (bic != kbicWS)
				{
					wsOnly = false;
					break;
				}
			}
		}

		*ppsegRet = NewObj GraphiteSegment(pts, this, ichMinSeg, ichMinSeg + segmentLen, dirDepth, fParaRtoL, wsOnly);
	}

	gr_seg_destroy(segment);
	gr_font_destroy(font);

	END_COM_METHOD(g_fact, IID_IRenderEngine);
}

/*----------------------------------------------------------------------------------------------
	Return the writing system factory for this database (or the registry, as the case may be).

	@param ppwsf Address of the pointer for returning the writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP GraphiteEngine::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
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
STDMETHODIMP GraphiteEngine::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD
	ChkComArgPtrN(pwsf);

	m_qwsf = pwsf;

	END_COM_METHOD(g_fact, IID_IRenderEngine)
}

void GraphiteEngine::InterpretChrp(LgCharRenderProps& chrp)
{
	ILgWritingSystemPtr qLgWritingSystem;
	CheckHr(m_qwsf->get_EngineOrNull(chrp.ws, &qLgWritingSystem));
	if (!qLgWritingSystem)
		ThrowHr(WarnHr(E_UNEXPECTED));
	CheckHr(qLgWritingSystem->InterpretChrp(&chrp));
}

/*----------------------------------------------------------------------------------------------
	Parse the magic string that indicates the feature values as used by FieldWorks.
	It is of the form:	"123=2,456=-4,..." (ie, <id>=<value>).
	In addition to integer values, it is possible to have a 4-character quoted string
	that is converted to an integer: 1="ENG1"
	Where syntax errors are encountered, simply skip that setting.
----------------------------------------------------------------------------------------------*/
void GraphiteEngine::ParseFeatureString(BSTR strFeatures)
{
	m_featureValues = gr_face_featureval_for_lang(m_face, 0);
	OLECHAR* pchw = strFeatures;
	OLECHAR* pchwLim = pchw + BstrLen(strFeatures);
	while (pchw < pchwLim)
	{
		int fid = 0;
		//	Read the ID.
		while (*pchw != '=' && *pchw != ' ')
		{
			if (*pchw < '0' || *pchw > '9')
			{
				fid = -1;
				break;	// syntax error: skip this setting
			}
			fid = fid * 10 + (*pchw - '0');
			pchw++;
		}

		if (fid != -1)
		{
			while (*pchw == ' ')
				pchw++;
			Assert(*pchw == '=');
			pchw++;
			while (*pchw == ' ')
				pchw++;

			int value;
			//	Read the value.
			if (*pchw == '"')
			{
				//	Language ID string--form an integer out of the first four bytes, ignore
				//	the rest.
				pchw++;	// skip quote
				char idtag[5];
				idtag[0] = (*pchw != '"') ? char(*pchw++) : '\0';
				idtag[1] = (*pchw != '"') ? char(*pchw++) : '\0';
				idtag[2] = (*pchw != '"') ? char(*pchw++) : '\0';
				idtag[3] = (*pchw != '"') ? char(*pchw++) : '\0';
				idtag[4] = '\0';
				while (pchw < pchwLim  && *pchw != '"')	// skip superfluous chars
					pchw++;
				pchw++;
				if (pchw < pchwLim)
				{
					pchw++;	// skip quote
					value = gr_str_to_tag(idtag);
				}
				else
				{
					value = -1;
				}
			}
			else
			{
				value = 0;
				//	Numerical value
				while (*pchw != ',' && *pchw != ' ' && pchw < pchwLim)
				{
					if (*pchw < '0' || *pchw > '9')
					{
						value = -1;
						break;	// syntax error skip this setting
					}
					value = value * 10 + (*pchw - '0');
					pchw++;
				}
			}

			if (value != -1)
			{
				//	Set the feature value.
				const gr_feature_ref* feature = gr_face_find_fref(m_face, fid);
				gr_fref_set_feature_value(feature, gr_uint16(value), m_featureValues);
			}
		}

		//	Find the next setting.
		while (pchw < pchwLim && *pchw != ',')
			pchw++;
		while (pchw < pchwLim && (*pchw < '0' || *pchw > '9'))
			pchw++;
	}
}

int GraphiteEngine::BreakWeightBefore(const gr_slot* s, const gr_segment* seg)
{
	const gr_slot* prevSlot = gr_slot_prev_in_segment(s);
	int bbefore = prevSlot != NULL ? gr_cinfo_break_weight(gr_seg_cinfo(seg, gr_slot_after(prevSlot))) : 50;
	int bafter = gr_cinfo_break_weight(gr_seg_cinfo(seg, gr_slot_before(s)));
	if (!gr_slot_can_insert_before(s))
		return 50;
	if (bbefore < 0)
		bbefore = 0;
	if (bafter > 0)
		bafter = 0;
	return abs((bbefore > bafter) ? bbefore : bafter);
}
