/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgFontManager.cpp
Responsibility: Larry Waswick
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "main.h"
#pragma hdrstop
//:> Any other headers (not precompiled).
#include "Vector_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

static Pcsz g_pszSys = _T("System");

//:>********************************************************************************************
//:>	Forward declarations.
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables.
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Constructor/Destructor.
//:>********************************************************************************************


//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
// Generic factory used to create an instance of LgFontManager with CoCreateInstance.
static GenericFactory g_fact(
	_T("SIL.Language.FontManager"),
	&CLSID_LgFontManager,
	_T("SIL Font Manager"),
	_T("Apartment"),
	&LgFontManager::CreateCom);

// The single global instance of the LgFontManager.
LgFontManager LgFontManager::g_fm;

/*----------------------------------------------------------------------------------------------
	Called by the GenericFactory to "create" an ILgFontManager; it just returns the global one.
----------------------------------------------------------------------------------------------*/
void LgFontManager::CreateCom(IUnknown * punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	CheckHr(g_fm.QueryInterface(riid, ppv));
}


//:>********************************************************************************************
//:>	IUnknown Methods.
//:>********************************************************************************************
// Get a pointer to the interface identified as iid.
STDMETHODIMP LgFontManager::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgFontManager *>(this));
	else if (riid == IID_ILgFontManager)
		*ppv = static_cast<ILgFontManager *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this,IID_ILgFontManager);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	ILgFontManager methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Answer in fAvail whether the font named bstrName is available, i.e., in the font list.

	@h3{Parameters}
	@code{
		bstrName	Name of font.
		pfAvail		Points to true if the font bstrname is available; false if not.
	}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgFontManager::IsFontAvailable(BSTR bstrName, ComBool * pfAvail)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	if (!bstrName)
		return E_INVALIDARG;
	ChkComOutPtr(pfAvail);

	*pfAvail = false;
	for (int i = 0; i < m_vstuFonts.Size(); i++)
	{
		if (m_vstuFonts[i] == bstrName)
		{
			*pfAvail = true;
			return S_OK;
		}
	}

	END_COM_METHOD(g_fact, IID_ILgFontManager);
}

/*----------------------------------------------------------------------------------------------
	Answer in fAvail whether the font, made up of cch OLECHAR's in prgchName, is available,
	i.e., in the font list.

	@h3{Parameters}
	@code{
		cch			Count of characters in the font name.
		prgchName	Range of characters that make up the font name.
		pfAvail		Points to true if the font prgchName is available; false if not.
	}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgFontManager::IsFontAvailableRgch(int cch, OLECHAR * prgchName, ComBool * pfAvail)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchName, cch);
	if (!cch)
		return E_INVALIDARG;
	ChkComOutPtr(pfAvail);

	*pfAvail = false;
	for (int i = 0; i < m_vstuFonts.Size(); i++)
	{
		if (m_vstuFonts[i].Equals(prgchName, cch))
		{
			*pfAvail = true;
			return S_OK;
		}
	}

	END_COM_METHOD(g_fact, IID_ILgFontManager);
}

/*----------------------------------------------------------------------------------------------
	Answer a comma-separated string of font names in pbstrNames.

	@h3{Parameters}
	@code{
		pbstrNames	comma-separated string of font names.
	}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgFontManager::AvailableFonts(BSTR * pbstrNames)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstrNames);

	const wchar * rgchwComma = L",";

	// Get the list of system fonts, if not already done.
	if (0 == m_vstuFonts.Size())
		GetFontNames();

	// Append font names, each followed by a delimiter.
	StrUni stuNames = m_vstuFonts[0];
	for (int i = 1; i < m_vstuFonts.Size(); i++)
	{
		stuNames.Append(rgchwComma);
		stuNames.Append(m_vstuFonts[i]);
	}

	// SysFreeString(*pbstrNames); // Free previous string, if any.
	*pbstrNames = SysAllocString(stuNames.Chars());
	if (!*pbstrNames)
		ThrowOutOfMemory();

	END_COM_METHOD(g_fact, IID_ILgFontManager);
}

/*----------------------------------------------------------------------------------------------
	Call GetFontNames to refresh the list of fonts. This will trigger a call to FontCallBack,
	which will add each font name to the vector stored in m_vstuFonts.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgFontManager::RefreshFontList(void)
{
	BEGIN_COM_METHOD;

	GetFontNames();

	END_COM_METHOD(g_fact, IID_ILgFontManager);
}


//:>********************************************************************************************
//:>	Protected methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	This callback function for EnumFontFamiliesEx, called by GetFontNames, will add each font
	name to the vector stored in m_vstuFonts. Keep the vector sorted.
----------------------------------------------------------------------------------------------*/
int CALLBACK LgFontManager::FontCallBack(ENUMLOGFONTEX * pelfe, NEWTEXTMETRICEX * pntme,
	DWORD ft, LPARAM lp)
{
	const achar * pszFont = pelfe->elfLogFont.lfFaceName;

	// The third condition below eliminates font names which begin with '@'. These are a bit
	// of a mystery, and seem to represent very large Unicode fonts. For example "@Batang".
	// Note that when EnumFontFamiliesEx returns "@Batang" it will also, separately, return
	// "Batang", so "Batang" will still be in the list.
	if (ft & TRUETYPE_FONTTYPE && _tcscmp(pszFont, g_pszSys) && (*pszFont != '@'))
	{
		int i = 0;
		// Search for the font name pszFont in the sorted vector m_vstuFonts.
		for (
			;
			i < g_fm.m_vstuFonts.Size() && g_fm.m_vstuFonts[i] < pszFont;
			i++);

		// Add the font name to the vector m_vstuFonts if it is not there already.
		if (i == g_fm.m_vstuFonts.Size())
			g_fm.m_vstuFonts.Push(pszFont);
		else if (g_fm.m_vstuFonts[i] != pszFont)
			g_fm.m_vstuFonts.Insert(i, pszFont);
	}

	return 1; // Continue enumerating.
}


//:>********************************************************************************************
//:>	Private methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Use an information context to get the available fonts.
----------------------------------------------------------------------------------------------*/
void LgFontManager::GetFontNames(void)
{
	AssertObj(this);

	LOGFONT lf;
	HDC hdc;

	hdc = CreateIC(TEXT("DISPLAY"), NULL, NULL, NULL);
	if (hdc)
	{
		ClearItems(&lf, 1);
		lf.lfCharSet = DEFAULT_CHARSET;

		::EnumFontFamiliesEx(hdc, &lf, (FONTENUMPROC)&FontCallBack, 0, 0);

		BOOL fSuccess;
		fSuccess = ::DeleteDC(hdc);
		Assert(fSuccess);
	}
}
