/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TsPropsFactory.h
Responsibility: Jeff Gayle
Last reviewed:

	Implementation of ITsPropsFactory.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TsPropsFactory_H
#define TsPropsFactory_H 1


/*----------------------------------------------------------------------------------------------
	Implements ITsPropsFactory.
	Hungarian: tpf / ztpf.
----------------------------------------------------------------------------------------------*/
class TsPropsFact : public ITsPropsFactory
{
public:
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void);
	STDMETHOD_(ULONG, Release)(void);

	// ITsPropsFactory methods.
	STDMETHOD(DeserializeProps)(IStream * pstrm, ITsTextProps ** ppttp);
	STDMETHOD(DeserializePropsRgb)(const byte * prgb, int * pcb, ITsTextProps ** ppttp);
	STDMETHOD(DeserializeRgPropsRgb)(int cpttpMax, const BYTE * prgb, int * pcb,
		int * pcpttpRet, ITsTextProps ** rgpttp, int * rgich);

	STDMETHOD(MakeProps)(BSTR bstrStyle, int ws, int ows, ITsTextProps ** ppttp);
	STDMETHOD(MakePropsRgch)(const OLECHAR * prgchStyle, int cch, int ws, int ows,
		ITsTextProps ** ppttp);

	STDMETHOD(GetPropsBldr)(ITsPropsBldr ** pptpb);

private:
	static TsPropsFact g_tpf;

	TsPropsFact(void)
	{
		// Don't call ModuleAddRef since there is a global singleton TsPropsFact. Its
		// AddRef and Release call ModuleAddRef and ModuleRelease.
	}
};

#endif // !TsPropsFactory_H
