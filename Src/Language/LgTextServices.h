/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgTextServices.h
Responsibility: Steve McConnel
Last reviewed: Not yet.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LgTextServices_H
#define LgTextServices_H

/*----------------------------------------------------------------------------------------------
	Provide access to TSF functionality wrapped in easy-to-call methods.

	@h3{Hungarian: lts}
----------------------------------------------------------------------------------------------*/
class LgTextServices : public ILgTextServices
{
public:
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	//:>****************************************************************************************
	//:>	IUnknown methods.
	//:>****************************************************************************************
	// Get a pointer to the interface identified as iid.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);

	// Add a reference by calling addref on the module, since this is a singleton.
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return ModuleEntry::ModuleAddRef();
	}
	// Release a reference by calling release on the module, since this is a singleton.
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		return ModuleEntry::ModuleRelease();
	}

	//:>****************************************************************************************
	//:>	ILgTextServices methods.
	//:>****************************************************************************************
	STDMETHOD(SetKeyboard)(int lcid, BSTR bstrKeymanKbd, int * pnActiveLangId,
		BSTR * pbstrActiveKeymanKbd, ComBool * pfSelectLangPending);

#ifdef DEBUG
	// Check to make certain we have a valid internal state for debugging purposes.
	bool AssertValid(void)
	{
		AssertPtr(this);
		return true;
	}
#endif // DEBUG

protected:
	/*------------------------------------------------------------------------------------------
		This generic constructor does nothing.
	------------------------------------------------------------------------------------------*/
	LgTextServices(void)
	{
	}

	friend class LanguageGlobals;
};

DEFINE_COM_PTR(LgTextServices);

#endif //!LgTextServices_H
