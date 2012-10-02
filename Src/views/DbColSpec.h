/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: DbColSpec.h
Responsibility: John Thomson
Last reviewed: never

Description:
	This file contains class declarations for the following class:
		DbColSpec
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef DbColSpec_INCLUDED
#define DbColSpec_INCLUDED


/*----------------------------------------------------------------------------------------------
	Cross-Reference: ${IDbColSpec}

	@h3{Hungarian: dcs}
----------------------------------------------------------------------------------------------*/
class DbColSpec : public IDbColSpec
{
public:
	STDMETHOD_(ULONG, AddRef)(void);
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, Release)(void);

	STDMETHOD(Clear)();
	STDMETHOD(Push)(int oct, int iBaseCol, PropTag tag, int ws);
	STDMETHOD(Size)(int * pc);
	STDMETHOD(GetColInfo)(int iIndex, int * poct, int * piBaseCol, PropTag * ptag, int * pws);
	STDMETHOD(GetDbColType)(int iIndex, int * poct);
	STDMETHOD(GetBaseCol)(int iIndex, int * piBaseCol);
	STDMETHOD(GetTag)(int iIndex, PropTag * ptag);
	STDMETHOD(GetWs)(int iIndex, int * pws);

protected:
	DbColSpec();
	~DbColSpec();

	int m_cref;
	Vector<int> voct;
	Vector<int> viBaseCol;
	Vector<PropTag> vtag;
	Vector<int> vws;
};
DEFINE_COM_PTR(DbColSpec);



#endif // DbColSpec_INCLUDED
