/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrUtil.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Various Graphite-related utilities that may be needed by the Graphite engine
	as well as FieldWorks or other applications.
---------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GRUTIL_INCLUDED
#define GRUTIL_INCLUDED 1

#include <vector>

#ifdef GR_FW
#ifndef __IVwGraphics_FWD_DEFINED__
#include "LanguageTlb.h" // for IVwGraphics
#endif
#else // !GR_FW
//////#include "UtilVector.h" // was Vector.h
//////#include "UtilString.h"
#endif // !GR_FW
//:End Ignore

// To avoid bringing in all of TtfUtil, just define a little bit here.
//#include "TtfUtil.h"
// #include "Tt.h" // Language currently can't find this.
#define PC_OS
// copied from TtSfnt_en.h:
#ifdef PC_OS
#define tag_Silf 0x666c6953
#else
#define tag_Silf 0x53696c66
#endif

//#define tag_Feat				0x74616546
//#define tag_Glat				0x74616c47
//#define tag_Gloc				0x636f6c47
//#define tag_CharToIndexMap      0x70616d63        /* 'cmap' */
//#define tag_OS_2                0x322f534f        /* 'OS/2' */
//#define tag_Postscript          0x74736f70        /* 'post' */

/////#include "IGrGraphics.h"
#include "GrResult.h"


struct IRenderEngine;

namespace gr
{

class GrUtil
{
public:

#if defined(_WIN32)
#if defined(GR_FW)
private:
#endif

	typedef enum
	{
		katRead = KEY_READ,
		katWrite = KEY_WRITE,
		katBoth = katRead | katWrite,
	} AccessType;

	static bool OpenFontKey(const utf16 * pszFontKey, const utf16 * pszStyle, AccessType at,
		HKEY * phkey);

#if defined(GR_FW)
public:
#endif
	// used by WorldPad:
	static bool GrUtil::FontHasGraphiteTables(const OLECHAR * pszFace, bool fBold, bool fItalic);
	static void GrUtil::InitGraphiteRenderer(IRenderEngine * preneng, const OLECHAR * pszFace,
			bool fBold, bool fItalic, BSTR bstrFontVar, int nTrace);

	static bool GetAllRegisteredGraphiteFonts(std::vector<std::wstring> & vstr);
	static bool GetFontFile(const utf16 * pszFontKey, const utf16 * pszStyle, std::wstring & strFile);
	static bool HasGraphiteRegistryEntry(const utf16 * pszFontKey);
	static bool FontHasGraphiteTables(IVwGraphics * pvg);
	//static bool FontHasGraphiteTables(IGrGraphics * pgg);
#endif // _WIN32
};

} // namespace gr

#endif // GRUTIL_INCLUDED
