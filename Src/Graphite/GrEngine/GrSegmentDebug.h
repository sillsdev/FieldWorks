/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrSegmentDebug.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Defines the class for a Graphite text segment that is used in the test procedures.
----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GR_SEGMENTDBG_INCLUDED
#define GR_SEGMENTDBG_INCLUDED

//:End Ignore

namespace gr
{

#ifdef OLD_TEST_STUFF

class GrSegmentDebug : public IGrSegmentDebug
{
public:
	// Constructors/destructors/etc.
	GrSegmentDebug(Segment * pzgrseg)
	{
		m_qzgrseg = pzgrseg;
		m_cref = 1;
		ModuleEntry::ModuleAddRef();
	}
	virtual ~GrSegmentDebug()
	{
		ModuleEntry::ModuleRelease();
	}

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv)
	{
		if (!ppv)
			return E_POINTER;
		AssertPtr(ppv);
		*ppv = NULL;
		if (iid == IID_IGrSegmentDebug)
			*ppv = static_cast<IGrSegmentDebug *>(this);
		else
			return m_qzgrseg->QueryInterface(iid, ppv);
		AddRef();
		return NOERROR;
	}
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

	//	standard methods
	STDMETHOD(get_Lim)(int ichwBase, int * pichw)
	{
		m_qzgrseg->setTextSourceOffset(ichwBase);
		*pichw = m_qzgrseg->stopCharacter() - m_qzgrseg->startCharacter();
	}
	STDMETHOD(get_Width)(int ichwBase, IVwGraphics * pvg, int * pxs)
	{
		return m_qzgrseg->advanceWidth(pvg, pxs);
	}

	//	debugging methods
	STDMETHOD(get_OutputText)(BSTR * pbstrResult)
	{
		return m_qzgrseg->debug_OutputText(pbstrResult);
	}
	STDMETHOD(get_UnderlyingToSurface)(int ichwBase, int ichw, ComBool fBefore, int * pislout)
	{
		return m_qzgrseg->debug_UnderlyingToLogicalSurface(ichwBase, ichw, fBefore, pislout);
	}
	STDMETHOD(get_SurfaceToUnderlying)(int ichwBase, int islout, ComBool fBefore, int * pichw)
	{
		return m_qzgrseg->debug_LogicalSurfaceToUnderlying(ichwBase, islout, fBefore, pichw);
	}

	STDMETHOD(get_Ligature)(int ichwBase, int ichw, int * pislout)
	{
		return m_qzgrseg->debug_Ligature(ichwBase, ichw, pislout);
	}
	STDMETHOD(get_LigComponent)(int ichwBase, int ichw, int  * piComp)
	{
		return m_qzgrseg->debug_LigComponent(ichwBase, ichw, piComp);
	}
	STDMETHOD(get_UnderlyingComponent)(int ichwBase, int islout, int iComp, int * pichw)
	{
		return m_qzgrseg->debug_UnderlyingComponent(ichwBase, islout, iComp, pichw);
	}


	//	instance variables
	long			m_cref;		// standard COM ref count
	GrSegmentPtr	m_qzgrseg;
};

} // namespace gr

#endif // OLD_TEST_STUFF

#endif // !GR_SEGMENTDBG_INCLUDED
