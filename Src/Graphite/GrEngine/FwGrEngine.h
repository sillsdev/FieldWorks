/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwGrEngine.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Contains the definition of the GrEngine class.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef FWGRENGINE_INCLUDED
#define FWGRENGINE_INCLUDED

//:End Ignore

/*----------------------------------------------------------------------------------------------
	The GrEngine serves as the top level object that knows how to run Graphite tables
	and generate Graphite segments.

	Primarily, this class implements IRenderEngine, which allows it to serve as a FW
	rendering engine. It also implements ISimpleInit, a general interface for initializing
	using a string. Finally, it implements ITraceControl, a very simple interface which
	allows a client to flip a flag indicating whether or not we want to output a log of
	the Graphite transduction process.

	Hungarian: greng
----------------------------------------------------------------------------------------------*/
class FwGrEngine :
	public IRenderEngine,
	public IRenderingFeatures,
	//public IJustifyingRenderer,
	public ITraceControl
{
public:
	// Static methods:
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructor/destructor:
	FwGrEngine();
	~FwGrEngine();

	// IUnknown methods:
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// ITraceControl methods:
	STDMETHOD(SetTracing)(int n);
	STDMETHOD(GetTracing)(int * pnOptions);

	// IRenderingFeatures methods:
	STDMETHOD(GetFeatureIDs)(int cMax, int * prgFids, int * pcfid);

	STDMETHOD(GetFeatureLabel)(int fid, int nLanguage, BSTR * pbstrLabel);

	STDMETHOD(GetFeatureValues)(int fid, int cfvalMax,
			int * prgfval, int * pcfval, int * pfvalDefault);

	STDMETHOD(GetFeatureValueLabel)(int fid, int fval, int nLanguage, BSTR * pbstrLabel);

	// IRenderEngine methods:
	STDMETHOD(InitRenderer)(IVwGraphics * pvg, BSTR bstrData);
	STDMETHOD(FontIsValid)();

	STDMETHOD(get_SegDatMaxLength)(int * pcb);

	STDMETHOD(FindBreakPoint)(IVwGraphics * pvg, IVwTextSource * pts, IVwJustifier * pvjus,
		int ichMin, int ichLim, int ichLimBacktrack,
		ComBool fNeedFinalBreak,
		ComBool fStartLine,
		int dxMaxWidth,
		LgLineBreak lbPref, LgLineBreak lbMax,
		LgTrailingWsHandling twsh, ComBool fParaRtoL,
		ILgSegment ** ppsegRet,
		int * pdichwLimSeg, int * pdxWidth, LgEndSegmentType * pest,
		ILgSegment * psegPrev);

	STDMETHOD(get_ScriptDirection)(int * pgrfsdc);

	STDMETHOD(get_ClassId)(GUID * pguid);

	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory ** ppencf);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);

	// IJustifyingRenderer methods:
	//STDMETHOD(GetGlyphAttribute)(int iGlyph, int kjgatId, int nLevel, float * pValueRet);
	//STDMETHOD(SetGlyphAttribute)(int iGlyph, int kjgatId, int nLevel, float value);

	// Other public methods:
	////virtual void NewSegment(Segment ** ppseg);

	// temporary
/***
	HRESULT GetGlyphAttribute(int iGlyph, int kjgatId, int nLevel, float *pValueRet)
	{
		return (HRESULT)m_pfont->GraphiteEngine()->getGlyphAttribute(iGlyph, kjgatId, nLevel, pValueRet);
	}
	HRESULT GetGlyphAttribute(int iGlyph, int kjgatId, int nLevel, int *pValueRet)
	{
		return (HRESULT)m_pfont->GraphiteEngine()->getGlyphAttribute(iGlyph, kjgatId, nLevel, pValueRet);
	}
	HRESULT SetGlyphAttribute(int iGlyph, int kjgatId, int nLevel, float value)
	{
		return (HRESULT)m_pfont->GraphiteEngine()->setGlyphAttribute(iGlyph, kjgatId, nLevel, value);
	}
	HRESULT SetGlyphAttribute(int iGlyph, int kjgatId, int nLevel, int value)
	{
		return (HRESULT)m_pfont->GraphiteEngine()->setGlyphAttribute(iGlyph, kjgatId, nLevel, value);
	}
***/

	// Other methods:
	void SetUpGraphics(IVwGraphics * pvg, FwGrTxtSrc * pfgts, int ichwMin);
	//static void AdjustFontAndStyles(IVwGraphics * pvg, IVwTextSource * pgts,
	//	int ichwMin, LgCharRenderProps * pchrp);

	wchar * FaceName()
	{
		return m_szFaceName;
	}

	std::ofstream * CreateLogFile(std::ofstream & strmLog, bool fAppend);

protected:
	static int ParseFeatureString(gr::utf16 * strFeatures, int cchLen,
		FeatureSetting * prgfset);

	bool LogFileName(std::string & staFile);

	std::wstring FontLoadErrorDescription(FontErrorCode ferr, int nVersion, int nSubVersion,
		HRESULT * phr);

protected:
	long m_cref;

	// Writing system factory used by this rendering engine.
	ILgWritingSystemFactoryPtr m_qwsf;

	// Note that this font is not fully functional unless the caller of FwGrEngine supplies
	// a device context; it basically serves as a pointer into the Graphite engine.
	// Specifically it should NOT be used for any methods that care about the size of the
	// font.
	WinFont * m_pfont;
	int m_fontSize;
	wchar m_szFaceName[32];
	FeatureSetting m_rgfsetDefaults[kMaxFeatures];
	int m_cfeatWDefaults;	// number of features with non-standard defaults
	bool m_fLog;

	// Static variable:
	//static FwGrJustifier * s_pgjus;
	// The static justifier object is stored in exactly one engine, which is responsible for
	// deleting it.
	FwGrJustifier * m_pgjus;

	bool m_useNFC;
};

DEFINE_COM_PTR(FwGrEngine);


/*----------------------------------------------------------------------------------------------
	Fieldworks-specific font.
----------------------------------------------------------------------------------------------*/
class FwWinFont : public WinFont
{
public:
	FwWinFont(HDC hdc) : WinFont(hdc)
	{
	}
	void SetDPI(int dpiX, int dpiY)
	{
		m_dpiX = dpiX;
		m_dpiY = dpiY;
	}
	virtual unsigned int getDPIy()
	{
		return m_dpiY;
	}
	virtual unsigned int getDPIx()
	{
		return m_dpiX;
	}
	virtual float fakeItalicRatio()
	{
		return (float)0.50;
	}

protected:
	int m_dpiY;
	int m_dpiX;
};


#endif  // !FWGRENGINE_INCLUDED
