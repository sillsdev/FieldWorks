/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrEngineDebug.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

FW version synchronized to open-source version on 16 April 2003.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef GR_ENGINEDBG_INCLUDED
#define GR_ENGINEDBG_INCLUDED

//:End Ignore

#ifdef OLD_TEST_STUFF

/*----------------------------------------------------------------------------------------------
	A class of Graphite engine that is used in the test procedures.
----------------------------------------------------------------------------------------------*/
class GrEngineDebug : public IGrEngineDebug
{
public:
	// Constructors/destructors/etc.
	GrEngineDebug(GrEngine * pgreng)
	{
		m_qgreng = pgreng;
		m_cref = 1;
		ModuleEntry::ModuleAddRef();
	}
	virtual ~GrEngineDebug()
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
		if (iid == IID_IGrEngineDebug)
			*ppv = static_cast<IGrEngineDebug *>(this);
		else
			return m_qgreng->QueryInterface(iid, ppv);
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
	STDMETHOD(BreakPointAtChar)(IVwGraphics * pvg,
		IVwTextSource * pts, int ichwMin, int ichwLim,
		ComBool fNeedFinalBreak,
		ComBool fStartLine,
		int cchw,
		LineBrk lbPref, LineBrk lbMax,
		ILgSegment ** ppsegRet,
		int * pichwLimSeg,
		int * pdxWidth, SegEnd * pest,
		int cbPrev, byte * pbPrevSegDat,
		int cbNextMax, byte * pbNextSegDat, int * pcbNextSegDat,
		int * pdichwContext)
	{
		return m_qgreng->FindBreakPointAux(pvg, pts, NULL, ichwMin, ichwLim, ichwLim,
			fNeedFinalBreak, fStartLine, true,
			cchw, true,
			lbPref, lbMax, ktwshAll, false,
			ppsegRet,
			pichwLimSeg,
			pdxWidth, pest,
			cbPrev, pbPrevSegDat, cbNextMax, pbNextSegDat, pcbNextSegDat,
			pdichwContext);
	}

	//	debugging methods


	//	instance variables
	long m_cref;		// standard COM ref count
	GrEnginePtr m_qgreng;
};

#endif // OLD_TEST_STUFF

#endif // !GR_ENGINEDBG_INCLUDED
