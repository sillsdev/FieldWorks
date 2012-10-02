/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrUtil.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.


Description:
-------------------------------------------------------------------------------*//*:End Ignore*/

#ifdef _WIN32

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#ifdef GR_FW

#include "Main.h"

#else //!GR_FW

// Headers to use for portability to non FieldWorks app
#ifdef _WIN32
#include <windows.h>
#endif // _WIN32

#ifdef GR_FW
#include "debug.h"
#else
#include "GrDebug.h"
#endif

#include <stdio.h>
#include "GrUtil.h"
#include "UtilRegistry.h"

///#include "string.h" // NULL
///#include "limits.h" // INT_MAX, INT_MIN

#endif // !GR_FW

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

namespace gr
{

//:>********************************************************************************************
//:>	Methods
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Open a key, which is the name of a Graphite font, and return a handle to it.

	@param pszFontKey		- the name of the font
	@param pszStyle			- regular, bold, italic, bolditalic;
								ignored here--caller opens string value
	@param at				- access
	@param phkey			- handle to return
----------------------------------------------------------------------------------------------*/
bool GrUtil::OpenFontKey(const utf16 * pszFontKey, const utf16 * pszStyle,
		AccessType at, HKEY * phkey)
{
	AssertPsz((const wchar_t*)pszFontKey);
	AssertPsz((const wchar_t*)pszStyle);
	AssertPtr(phkey);

//#ifdef GR_FW
//	StrApp str;
//	str.Format(_T("Software\\SIL\\GraphiteFonts\\%s"), pszFontKey);
//#else
	OLECHAR str[260];
	_stprintf_s(str, _T("Software\\SIL\\GraphiteFonts\\%s"), (wchar_t*)pszFontKey);
//#endif

	if (at == katRead)
		return ::RegOpenKeyEx(HKEY_LOCAL_MACHINE, str, 0, at, phkey) == ERROR_SUCCESS;
	return ::RegCreateKeyEx(HKEY_LOCAL_MACHINE, str, 0, NULL, 0, at, NULL, phkey,
		NULL) == ERROR_SUCCESS;
}

/*----------------------------------------------------------------------------------------------
	Fill in the vector with the names of all the Graphite fonts in the registry.
	These are not guaranteed to come back in any particular order (eg sorted).
	Note that we now support using Graphite fonts that are NOT registered, and this is likely
	to become the most common case except for font developers, so using this method
	is questionable.
----------------------------------------------------------------------------------------------*/
bool GrUtil::GetAllRegisteredGraphiteFonts(std::vector<std::wstring> & vstr)
{
	//	Open main key:
	std::wstring str(L"Software\\SIL\\GraphiteFonts");
	bool f;
	RegKey hkey;
	f = ::RegOpenKeyEx(HKEY_LOCAL_MACHINE, str.data(), 0, katRead, &hkey);
	if (f != ERROR_SUCCESS)
		return false;

	DWORD dwIndex = 0;
	for ( ; ; )
	{
		OLECHAR rgch[256];
		DWORD cb = isizeof(rgch);
		LONG l = ::RegEnumKeyEx(hkey, dwIndex, rgch, &cb, NULL, NULL, NULL, NULL);
		if (l == ERROR_NO_MORE_ITEMS)
			return true;
		else if (l != ERROR_SUCCESS)
			return false;

		std::wstring strTmp(rgch);
		vstr.push_back(strTmp);
		dwIndex++;
	}

	Assert(false);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Return the file name (including path) of the given Graphite font.

	@param pszFontKey			- name of font
	@param strStyle				- regular, bold, italic, bolditalic
	@param strRetFile			- file name to return
----------------------------------------------------------------------------------------------*/
bool GrUtil::GetFontFile(const utf16 * pszFontKey, const utf16 * pszStyle,
	std::wstring & strRetFile)
{
	AssertPsz((const wchar_t*)pszFontKey);
	AssertPsz((const wchar_t*)pszStyle);

	RegKey hkey;
	if (OpenFontKey(pszFontKey, pszStyle, katRead, &hkey))
	{
		OLECHAR rgch[MAX_PATH];
		DWORD cb = isizeof(rgch);
		DWORD dwT;
		if (::RegQueryValueEx(hkey, (LPCWSTR)pszStyle, NULL, &dwT, (byte *)rgch, &cb) == ERROR_SUCCESS)
		{
			Assert(dwT == REG_SZ);
			strRetFile = rgch;
			return true;
		}
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Return true if there is a Graphite font in the registry with the given name.

	@param pszFontKey			- name of font
----------------------------------------------------------------------------------------------*/
bool GrUtil::HasGraphiteRegistryEntry(const utf16 * pszFontKey)
{
	//	Open main key:
	std::wstring str(L"Software\\SIL\\GraphiteFonts");
	bool f;
	RegKey hkey;
	f = ::RegOpenKeyEx(HKEY_LOCAL_MACHINE, str.data(), 0, katRead, &hkey);
	if (f != ERROR_SUCCESS)
		return false;

	DWORD dwIndex = 0;
	for ( ; ; )
	{
		OLECHAR rgch[256];
		DWORD cb = isizeof(rgch);
		LONG l = ::RegEnumKeyEx(hkey, dwIndex, rgch, &cb, NULL, NULL, NULL, NULL);
		if (_tcscmp((const wchar_t*)pszFontKey, rgch) == 0)
			return true;
		else if (l == ERROR_NO_MORE_ITEMS)
			return false;
		else if (l != ERROR_SUCCESS)
			return false;
		dwIndex++;
	}

	Assert(false);
	return false;
}

/*----------------------------------------------------------------------------------------------
	Return true if the font selected into the graphics object stands at least
	a good chance of being a Graphite font.
----------------------------------------------------------------------------------------------*/
bool GrUtil::FontHasGraphiteTables(IVwGraphics * pvg)
{
	HDC hdc;
	IVwGraphicsWin32Ptr qvg32;
	CheckHr(pvg->QueryInterface(IID_IVwGraphicsWin32, (void **)&qvg32));
	CheckHr(qvg32->GetDeviceContext(&hdc));

	// Unfortunately this duplicates code in the Graphite Font class, but that's easier
	// than including Font everywhere this is needed.
	DWORD cbTableSz = ::GetFontData(hdc, tag_Silf, 0, NULL, 0);
	if (cbTableSz == GDI_ERROR)
		return false;
	else if (cbTableSz == 0)
		return false;
	return true;
}

/**
bool GrUtil::FontHasGraphiteTables(IGrGraphics * pgg)
{
	// If there is an Silf table, there is a good chance this is a valid Graphite font.
	int silf_tbl_sz = 0;
	bool fOk = false;
	if (pgg->GetFontData(tag_Silf, &silf_tbl_sz, NULL, 0) == kresFalse)
	{
		//	silf_tbl_sz will be -1 if the client manages the table buffer memory
		//  and not the engine, in this case the buffer will hold a pointer to the
		//  table rather than a copy of the table.
		//silf_tbl_sz = max(int(silf_tbl_sz), int(tmp)); // SC removed std::max

		// Tired of trying to get the above to compile, sorry:
		if (sizeof(byte**) > silf_tbl_sz)
			silf_tbl_sz = sizeof(byte**);

		byte *silf_tbl = new byte[silf_tbl_sz];
		fOk = pgg->GetFontData(tag_Silf, &silf_tbl_sz, silf_tbl, silf_tbl_sz) == kresOk;
		delete [] silf_tbl;
	}
	// Review: do we need to check for the presence of the Glat table as well?


	if (!fOk)
	{
		// If no tables in the font, look in the registry; there might be another
		// version of the font somewhere with the Graphite tables.
		LgCharRenderProps chrp;
		pgg->get_FontCharProperties(&chrp);
//		utf16 rgchStyle[25];
//		if (chrp.ttvBold == kttvOff)
//			rgchStyle = (chrp.ttvItalic == kttvOff) ? _T("regular") : _T("italic");
//		else
//			rgchStyle = (chrp.ttvItalic == kttvOff) ? _T("bold") : _T("bolditalic");

		fOk = GrUtil::HasGraphiteRegistryEntry(chrp.szFaceName);
		// For now, disable the registry mechanism:
		fOk = false;
	}

	return fOk;
}
**/

#ifdef GR_FW
/*----------------------------------------------------------------------------------------------
	See if there are Graphite tables in the font, or failing that, an indication of the
	Graphite font in the registry.
----------------------------------------------------------------------------------------------*/
bool GrUtil::FontHasGraphiteTables(const OLECHAR * pszFace, bool fBold, bool fItalic)
{
	bool fGraphite = false;
	LgCharRenderProps chrp;
	wcscpy_s(chrp.szFaceName, pszFace);
	chrp.ttvBold = fBold ? kttvForceOn : kttvOff;
	chrp.ttvItalic = fItalic ? kttvForceOn : kttvOff;
	chrp.dympHeight = 12000; // arbitrary to make it something reasonable (12 points)
	IVwGraphicsWin32Ptr qvg;
	qvg.CreateInstance(CLSID_VwGraphicsWin32);
	HDC hdc = ::CreateDC(TEXT("DISPLAY"), NULL, NULL, NULL);
	try {
		qvg->Initialize(hdc); // puts the DC in the right state
		qvg->SetupGraphics(&chrp);
		//FwGrGraphics gg(qvg.Ptr());
		//fGraphite = GrUtil::FontHasGraphiteTables(&gg);
		fGraphite = GrUtil::FontHasGraphiteTables(qvg);
	}
	catch (...)
	{
	}
	qvg.Clear();
	::DeleteDC(hdc);
	return fGraphite;
}

/*----------------------------------------------------------------------------------------------
	A convenient way to initialize a graphite renderer when we don't have an IVwGraphics
	around. When one is readily available, just call InitRenderer directly.
----------------------------------------------------------------------------------------------*/
void GrUtil::InitGraphiteRenderer(IRenderEngine * preneng, const OLECHAR * pszFace,
		bool fBold, bool fItalic, BSTR bstrFontVar, int nTrace)
{
	LgCharRenderProps chrp;
	wcscpy_s(chrp.szFaceName, pszFace);
	chrp.ttvBold = fBold ? kttvForceOn : kttvOff;
	chrp.ttvItalic = fItalic ? kttvForceOn : kttvOff;
	chrp.dympHeight = 12000; // arbitrary to make it something reasonable (12 points)
	IVwGraphicsWin32Ptr qvg;
	qvg.CreateInstance(CLSID_VwGraphicsWin32);
	HDC hdc = ::CreateDC(TEXT("DISPLAY"), NULL, NULL, NULL);
	try {
		qvg->Initialize(hdc); // puts the DC in the right state
		qvg->SetupGraphics(&chrp);
		CheckHr(preneng->InitRenderer(qvg, bstrFontVar));

		// Set the trace options (e.g. logging).
		ComSmartPtr<ITraceControl> qtreng;
		CheckHr(preneng->QueryInterface(IID_ITraceControl, (void**)&qtreng));
		CheckHr(qtreng->SetTracing(nTrace));
	}
	catch (...)
	{
	}
	qvg.Clear();
	::DeleteDC(hdc);
}

#endif // GR_FW

} // namespace gr

#endif // _WIN32
