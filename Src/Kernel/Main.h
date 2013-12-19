/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Main.h
Responsibility: Jeff Gayle
Last reviewed:

	Main header file for the text services dll.
-------------------------------------------------------------------------------*//*:End Ignore*/
#if _MSC_VER
#pragma once
#endif
#ifndef Main_H
#define Main_H 1

#include "common.h"

//#define kwsLim 0xfffffff9
#include "CellarConstants.h"

#if WIN32
#include <mlang.h>
#endif

using std::min;
using std::max;

/***********************************************************************************************
	Interfaces.
***********************************************************************************************/
#include "FwKernelTlb.h"

/***********************************************************************************************
	Implementations.
***********************************************************************************************/
#include "LangResource.h"
using namespace fwutil;	// Rect and Point classes
// these are a gray area, including aspects of both model and engine
// Todo JohnT: These structs are part of an obsolete approach to overriding character properties.
// Get rid of them and whatever uses them. (Taken from OldLgWritingSystem file.)
/*----------------------------------------------------------------------------------------------
	The CharacterPropertyObject stores all of the data that we allow to be overriden for a
	single character (right now, pretty much everything except the Unicode 1.0 name).
----------------------------------------------------------------------------------------------*/
struct CharacterPropertyObject
{
	UChar32 uch32CodePoint;
	StrUni stuCharName;
	LgGeneralCharCategory ccGenCategory;
	unsigned int nCombiningClass : 8;
	LgBidiCategory bicBidiCategory;
	LgDecompMapTag dtDecompMapTag;
	Vector <UChar32> vuch32Decomp;
	unsigned int nDecDigit : 4;
	unsigned int nDigit : 4;
	int nNumericValue;	//numerator stored in the top 16 bits, denominator in the bottom 16
	bool fMirrored : 1;
	StrUni stuISOComment;
	UChar32 uch32Uppercase;
	UChar32 uch32Lowercase;
	UChar32 uch32Titlecase;
	LgLBP lbpLineBreak;

	void Clear()
	{
		uch32CodePoint = 0;
		stuCharName.Clear();
		ccGenCategory = kccLu;  //0
		nCombiningClass = 0;
		bicBidiCategory = kbicL;  //0
		dtDecompMapTag = kdtNoTag;  //0
		vuch32Decomp.Clear();
		nDecDigit = 0;
		nDigit = 0;
		nNumericValue = 0;
		fMirrored = false;
		stuISOComment.Clear();
		uch32Uppercase = 0;
		uch32Lowercase = 0;
		uch32Titlecase = 0;
		lbpLineBreak = klbpAI;  //0
	}
}; //hungarian cpo
struct CharPropRange
{
	UChar32 iMin;
	UChar32 iLim;
	Vector <unsigned short> vRange;
}; //hungarian cpr
struct OverriddenCharProps
{
	UChar32 iMin;
	UChar32 iLim;
	Vector <CharPropRange> * pvcprOverride1;
	Vector <CharacterPropertyObject> * pvcpoOverride2;
}; //hungarian ocp

#include "KernelGlobals.h"
#include "TsString.h"
#include "TsTextProps.h"
#include "TsStrFactory.h"
#include "TsPropsFactory.h"
#include "TextServ.h"
#include "TsMultiStr.h"
#include "ActionHandler.h"
// Engines
#include "LgIcuCharPropEngine.h"
#include "LgUnicodeCollater.h"
class RomRenderEngine;
DEFINE_COM_PTR(RomRenderEngine);
class UniscribeEngine;
DEFINE_COM_PTR(UniscribeEngine);
#include "RomRenderSegment.h"
#include "RomRenderEngine.h"
#include "LgSimpleEngines.h"
#include "LgNumericEngine.h"
#if !WIN32
#include "UniscribeLinux.h"
#endif
#include "UniscribeSegment.h"
#include "UniscribeEngine.h"
#include "RegexMatcherWrapper.h"

// Other tools
#include "FwStyledText.h"
#include "StringToNumHelpers.h"
#include "WriteXml.h"		// From AppCore.
#include "xmlparse.h"
#include "LgKeymanHandler.h"
#include "LgIcuWrappers.h"

#if WIN32
// for parsing XML files; in this DLL, we want the parser to work with wide characters,
// since we always parse BSTRs.
#define XML_UNICODE_WCHAR_T
#else
// XML_UNICODE_WCHAR_T causes XML_Char to be wchar_t
#ifdef XML_UNICODE
	#error "Don't define XML_UNICODE as this causes XML_CHAR to be UTF-16 which expat on Linux can't handle"
#endif
#endif
#include "StringToNumHelpers.h"
#include "../Cellar/FwXml.h"
#include "WriteXml.h"		// From AppCore.

#endif // !Main_H
