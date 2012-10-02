/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwGraphiteProcess.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Contains the definition of the GrEngine class.
----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GR_PROCESS_INCLUDED
#define GR_PROCESS_INCLUDED

//:End Ignore

namespace gr
{
	class GrEngine;
}

/*----------------------------------------------------------------------------------------------

	Hungarian: fgje
----------------------------------------------------------------------------------------------*/
class FwGraphiteProcess :
	public gr::GraphiteProcess,
	public IJustifyingRenderer
{
public:
	// Constructors & destructors:
	FwGraphiteProcess();

	virtual ~FwGraphiteProcess();

	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// IUnknown methods:
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);

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

	// IJustifyingRenderer methods:
	// These methods return kresInvalidArg if the attribute ID is invalid or inappropriate;
	// kresFail if the engine is not in an appropriate state to return the information.
	STDMETHOD(GetGlyphAttributeFloat)(int iGlyph, int jgat, int nLevel, float * pValueRet);
	STDMETHOD(GetGlyphAttributeInt)(int iGlyph, int jgat, int nLevel, int * pValueRet);
	STDMETHOD(SetGlyphAttributeFloat)(int iGlyph, int jgat, int nLevel, float value);
	STDMETHOD(SetGlyphAttributeInt)(int iGlyph, int jgat, int nLevel, int value);

	void SetProcess(gr::GrEngine * pgproc)
	{
		m_pgproc = pgproc;
	}

	virtual gr::GrResult getGlyphAttribute(int iGlyph, int jgat, int nLevel, float * pValueRet);
	virtual gr::GrResult getGlyphAttribute(int iGlyph, int jgat, int nLevel, int * pValueRet);
	virtual gr::GrResult setGlyphAttribute(int iGlyph, int jgat, int nLevel, float value);
	virtual gr::GrResult setGlyphAttribute(int iGlyph, int jgat, int nLevel, int value);

protected:
	long m_cref;

	gr::GrEngine * m_pgproc;
};

#endif  // !GR_PROCESS_INCLUDED
