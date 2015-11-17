/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: RomRenderEngine.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef ROMRENDERENGINE_INCLUDED
#define ROMRENDERENGINE_INCLUDED

// Enumerate the possible reasons why only so many characters are available (GetAvailChars)
typedef enum
{
	keatLim,	// Reached limit of chars we are allowed to use
	keatBreak,	// Found a hard break character
	keatNewWs,	// Found a old writing system change
	keatMax,	// Reached max chars we handle in one go
	keatOnlyWs,	// Reached something that was not white space
} EndAvailType;

/*----------------------------------------------------------------------------------------------
Class: RomRenderEngine
Description:
Hungarian: rre
----------------------------------------------------------------------------------------------*/
class RomRenderEngine :
	public IRenderEngine
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	RomRenderEngine();
	virtual ~RomRenderEngine();

	// IUnknown methods
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

	// IRenderEngine methods
	STDMETHOD(InitRenderer)(IVwGraphics * pvg, BSTR bstrData);
	STDMETHOD(get_FontIsValid)(ComBool * pfValid);
	STDMETHOD(get_SegDatMaxLength)(int * cb);
	STDMETHOD(FindBreakPoint)(IVwGraphics * pvg, IVwTextSource * pts, IVwJustifier * pvjus,
		int ichMin, int ichLim, int ichLimBacktrack,
		ComBool fNeedFinalBreak, ComBool fStartLine, int dxMaxWidth,
		LgLineBreak lbPref, LgLineBreak lbMax, LgTrailingWsHandling twsh, ComBool fParaRtoL,
		ILgSegment ** ppsegRet, int * pdichLimSeg, int * pdxWidth, LgEndSegmentType * pest,
		ILgSegment * psegPrev);
	STDMETHOD(get_ScriptDirection)(int * pgrfsdc);
	STDMETHOD(get_ClassId)(GUID * pguid);

	STDMETHOD(get_WritingSystemFactory)(ILgWritingSystemFactory ** ppwsf);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);

	// Other public methods
	void GetAvailChars(IVwTextSource * pts, int ws,
		int ichMin, int ichLimSeg, int cchMax,
		LgTrailingWsHandling twsh, ILgCharacterPropertyEngine * pcpe,
		OLECHAR *prgch, int *pichLimSegMax, EndAvailType *peat);

protected:
	// Member variables
	long m_cref;

	// The following are not currently being used, but they may eventually come in handy:
//	bool m_fRemovedWhtsp;
//	bool m_fExceededSpace;
	bool m_fMoreText;
	bool m_fNextIsSameWs;

	// Writing system factory used by this instance of the rendering engine.
	ILgWritingSystemFactoryPtr m_qwsf;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
//	void AdjustEndForWidth(IVwGraphics * pvg);
	void FindLineBreak(
#ifndef ICU_LINEBREAKING
		const byte * prglbs,
#else
		const OLECHAR * prgch, const ILgCharacterPropertyEnginePtr qcpe,
#endif /*ICU_LINEBREAKING*/
		const int ichMin, const int ichLim,
		const LgLineBreak lbrkRequired, const bool fBackFromEnd, int & ichBreak, int & ichDim);
};

#endif  //ROMRENDERENGINE_INCLUDED
