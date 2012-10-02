/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: IGrDebug.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Defines interfaces for the Graphite engine and text segment that is used in
	the test procedures.

	Keep the version in the test procedure project in sync with this one.
----------------------------------------------------------------------------------------------*/
#ifdef _DEBUG

#ifdef OLD_TEST_STUFF

#pragma once

#ifndef GR_INTFDBG_INCLUDED
#define GR_INTFDBG_INCLUDED

//:End Ignore

interface __declspec(uuid("F0C462A3-3258-11d4-9273-00400543A57C")) IGrEngineDebug;

#define IID_IGrEngineDebug __uuidof(IGrEngineDebug)

interface IGrEngineDebug : public IUnknown
{
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
		int * pdichwContext) = 0;
};



interface __declspec(uuid("F0C462A4-3258-11d4-9273-00400543A57C")) IGrSegmentDebug;

#define IID_IGrSegmentDebug __uuidof(IGrSegmentDebug)

interface IGrSegmentDebug : public IUnknown
{
	//	standard methods
	STDMETHOD(get_Lim)(int ichwBase, int * pich) = 0;
	STDMETHOD(get_Width)(int ichwBase, IVwGraphics* pvg, int* pxs) = 0;

	//	debugging methods
	STDMETHOD(get_OutputText)(BSTR * pbstr) = 0;
	STDMETHOD(get_UnderlyingToSurface)(int ichwbase, int ichw, ComBool fBefore, int * pislout) = 0;
	STDMETHOD(get_SurfaceToUnderlying)(int ichwbase, int islout, ComBool fBefore, int * pichw) = 0;

	STDMETHOD(get_Ligature)(int ichwbase, int ichw, int * pislout) = 0;
	STDMETHOD(get_LigComponent)(int ichwbase, int ichw, int * pi) = 0;
	STDMETHOD(get_UnderlyingComponent)(int ichwbase, int islout, int iComp, int * pichw) = 0;
};

#endif // !GR_INTFDBG_INCLUDED

#endif // OLD_TEST_STUFF

#endif // _DEBUG
