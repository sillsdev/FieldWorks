/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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

// If ICU_LINEBREAKING is defined, we use the ICU functions for linebreaking.
// If this is undefined, then we go to JohnT's previous version which doesn't
// use ICU functions.
#ifndef ICU_LINEBREAKING
#define ICU_LINEBREAKING
#endif /*ICU_LINEBREAKING*/

#include "common.h"

//#define kwsLim 0xfffffff9
#include "CellarConstants.h"

#define NO_EXCEPTIONS 1
#if WIN32
#include <mlang.h>
#endif

#if !WIN32
#include <OleStringLiteral.h>
#include "BasicTypes.h"
#include <memory>
#endif

using std::min;
using std::max;

/***********************************************************************************************
	Interfaces.
***********************************************************************************************/
#include "FwKernelTlb.h"

// Special interface mainly used for Graphite engine not defined in an IDH.
#include "../Graphite/GrEngine/ITraceControl.h"
#ifndef ITraceControlPtr // for portability I don't think this header defines this.
	DEFINE_COM_PTR(ITraceControl);
#endif

/***********************************************************************************************
	Implementations.
***********************************************************************************************/
#include "LangResource.h"

// For interfacing with Graphite engines:
namespace gr {
typedef unsigned char utf8;
typedef wchar_t utf16;
typedef unsigned long int utf32;
#define UtfType LgUtfForm
}
// defined in TtSfnt_en.h - but this avoids including all of TtfUtil in the Language.dll
#define tag_Silf 0x666c6953
#include "GrResult.h"
#include "GrUtil.h"
#include "ITextSource.h"
#include "IGrJustifier.h"
#include "FwGr.h"

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
