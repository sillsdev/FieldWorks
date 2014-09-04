/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GraphiteEngine.h
Responsibility: Damien Daspit
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef GRAPHITEENGINE_INCLUDED
#define GRAPHITEENGINE_INCLUDED

#include <graphite2/Segment.h>

/*----------------------------------------------------------------------------------------------
Class: GraphiteEngine
Description:
Hungarian: gre
----------------------------------------------------------------------------------------------*/
class GraphiteEngine : public IRenderEngine, public IRenderingFeatures
{
public:
	// Static methods
	static void CreateCom(IUnknown* punkOuter, REFIID iid, void** ppv);

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// IRenderingFeatures methods:
	STDMETHOD(GetFeatureIDs)(int cMax, int* prgFids, int* pcfid);
	STDMETHOD(GetFeatureLabel)(int fid, int nLanguage, BSTR* pbstrLabel);
	STDMETHOD(GetFeatureValues)(int fid, int cfvalMax,
			int* prgfval, int* pcfval, int* pfvalDefault);
	STDMETHOD(GetFeatureValueLabel)(int fid, int fval, int nLanguage, BSTR* pbstrLabel);

	// IRenderEngine methods
	STDMETHOD(InitRenderer)(IVwGraphics* pvg, BSTR bstrData);
	STDMETHOD(FontIsValid)();
	STDMETHOD(get_SegDatMaxLength)(int* cb);
	STDMETHOD(FindBreakPoint)(IVwGraphics* pvg, IVwTextSource* pts, IVwJustifier* pvjus,
		int ichMin, int ichLim, int ichLimBacktrack,
		ComBool fNeedFinalBreak, ComBool fStartLine, int dxMaxWidth,
		LgLineBreak lbPref, LgLineBreak lbMax, LgTrailingWsHandling twsh, ComBool fParaRtoL,
		ILgSegment** ppsegRet, int* pdichLimSeg, int* pdxWidth, LgEndSegmentType* pest,
		ILgSegment* psegPrev);
	STDMETHOD(get_ScriptDirection)(int* pgrfsdc);
	STDMETHOD(get_ClassId)(GUID* pguid);
	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory** ppwsf);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory* pwsf);

	// Other public methods
	const gr_face* Face()
	{
		return m_face;
	}

	const gr_feature_val* FeatureValues()
	{
		return m_featureValues;
	}

protected:
	static int Round(const float n)
	{
		return int(n < 0 ? n - 0.5 : n + 0.5);
	}
	void InterpretChrp(LgCharRenderProps& chrp);
	void ParseFeatureString(BSTR strFeatures);
	static int BreakWeightBefore(const gr_slot* s, const gr_segment* seg);

	// Member variables
	long m_cref;

	// Writing system factory used by this rendering engine.
	ILgWritingSystemFactoryPtr m_qwsf;

	gr_face* m_face;
	gr_feature_val* m_featureValues;
	gr_feature_val* m_defaultFeatureValues;

	// Static methods

	// Constructors/destructors/etc.
	GraphiteEngine();
	virtual ~GraphiteEngine();
};

#endif  //GRAPHITEENGINE_INCLUDED
