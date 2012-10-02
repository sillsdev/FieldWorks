/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TsStrFactory.h
Responsibility: Jeff Gayle
Last reviewed:

	Implementation of ITsStringFactory.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TsStrFactory_H
#define TsStrFactory_H 1

class TsStrFact : public ITsStrFactory
{
public:
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);
	static void DeserializeStringCore(DataReader * pdrdrTxt,
		DataReader * pdrdrFmt, ITsString ** pptss);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// ITsStrFactory methods.
	STDMETHOD(DeserializeStringStreams)(IStream * pstrmTxt, IStream * pstrmFmt,
		ITsString ** pptss);
	STDMETHOD(DeserializeString)(BSTR bstrTxt, IStream * pstrmFmt,
		ITsString ** pptss);
	STDMETHOD(DeserializeStringRgb)(BSTR bstrTxt,
		const byte * prgbFmt, int cbFmt, ITsString ** pptss);
	STDMETHOD(DeserializeStringRgch)(
		const OLECHAR * prgchTxt, int * pcchTxt, const byte * prgbFmt, int * pcbFmt,
		ITsString ** pptss);

	STDMETHOD(MakeString)(BSTR bstr, int ws, ITsString ** pptss);
	STDMETHOD(MakeStringRgch)(const OLECHAR * prgch, int cch, int ws, ITsString ** pptss);
	STDMETHOD(MakeStringWithPropsRgch)(const OLECHAR * prgch, int cch, ITsTextProps * pttp,
		ITsString ** pptss);

	// Builder methods.
	STDMETHOD(GetBldr)(ITsStrBldr ** pptsb);
	STDMETHOD(GetIncBldr)(ITsIncStrBldr ** pptisb);

	// Run information
	// Fetch the number of runs in the given format prgch.
	STDMETHOD(get_RunCount)(const byte * prgbFmt, int cbfmt, int * pcrun);
	// Fetch the run information for a particular run in for format prgch. irun must be a valid
	// run index.
	STDMETHOD(FetchRunInfo)(const byte * prgbFnt, int cbFmt, int irun,
		TsRunInfo * ptri, ITsTextProps ** ppttp);
	// Fetch the run information at a given character position relative to the format prgch.
	// ich must be a valid character postion.
	STDMETHOD(FetchRunInfoAt)(const byte * prgbFmt, int cbFmt, int ich,
		TsRunInfo * ptri, ITsTextProps ** ppttp);
	STDMETHOD(EmptyString)(int ws, ITsString ** pptss);


private:
	static TsStrFact g_strf;

	TsStrFact(void)
	{
		// Don't call ModuleAddRef since there is a global singleton TsStrFact. Its
		// AddRef and Release call ModuleAddRef and ModuleRelease.
	}
};


#endif // !TsStrFactory_H
