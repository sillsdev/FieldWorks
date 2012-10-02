/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgFontManager.h
Responsibility: Larry Waswick
Last reviewed: Not yet.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LgFontManager_H
#define LgFontManager_H

/*----------------------------------------------------------------------------------------------
	Obtain a list of the (TrueType) fonts that are available on the system, and handle simple
	queries about font availability.

	Note: the font manager may load the available fonts once, and use this cached list to answer
	subsequent queries. To be sure the answer to a query is current, call RefreshFontList.

	@h3{Data Structures}
	@code{
		m_vstuFonts - Vector<StrUni> to hold font names.
	}

	@h3{Hungarian: fm}
----------------------------------------------------------------------------------------------*/
class LgFontManager : public ILgFontManager
{
public:
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	virtual ~LgFontManager();

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
	//:>	ILgFontManager methods.
	//:>****************************************************************************************
	STDMETHOD(IsFontAvailable)(BSTR bstrName, ComBool * pfAvail);
	STDMETHOD(IsFontAvailableRgch)(int cch, OLECHAR * prgchName, ComBool * pfAvail);
	STDMETHOD(AvailableFonts)(BSTR * pbstrNames);
	STDMETHOD(RefreshFontList)(void);

#ifdef DEBUG
	// Check to make certain we have a valid internal state for debugging purposes.
	bool AssertValid(void)
	{
		AssertPtr(this);
		AssertObj(&m_vstuFonts);
		return true;
	}
#endif // DEBUG

protected:
	/*------------------------------------------------------------------------------------------
		This generic constructor simply gets the list of the (TrueType) fonts for the global
		variable g_fm.
	------------------------------------------------------------------------------------------*/
	LgFontManager(void);

#if WIN32
	static int CALLBACK FontCallBack(ENUMLOGFONTEX * pelfe, NEWTEXTMETRICEX * pntme,
		DWORD ft, LPARAM lp);
#endif

	friend class LanguageGlobals;

	//:>****************************************************************************************
	//:>	Member variables.
	//:>****************************************************************************************
	// Vector<StrUni> to hold font names.
	Vector<StrUni> m_vstuFonts;

private:
	// Get the available fonts.
	void GetFontNames(void);
};

DEFINE_COM_PTR(LgFontManager);

#endif //!LgFontManager_H
