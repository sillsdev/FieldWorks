/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Main.h
Responsibility:
Last reviewed:

	Main header file for the Language component.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LANGUAGE_H
#define LANGUAGE_H 1

// If ICU_LINEBREAKING is defined, we use the ICU functions for linebreaking.
// If this is undefined, then we go to JohnT's previous version which doesn't
// use ICU functions.
#ifndef ICU_LINEBREAKING
#define ICU_LINEBREAKING
#endif /*ICU_LINEBREAKING*/

#define NO_EXCEPTIONS 1
#include "Common.h" // Most of generic.
#include <mlang.h>

//:>**********************************************************************************
//:>	Interfaces.
//:>**********************************************************************************
//#include "FwKernelTlb.h"
//#include "DbAccessTlb.h"		// Provides Database access for persistence.
#include "LanguageTlb.h"
#include "FwCellarTlb.h"
#ifdef LANG_MODEL
#include "LanguageModelTlb.h"
#endif // LANG_MODEL
// Special interface mainly used for Graphite engine not defined in an IDH.
#include "..\Graphite\GrEngine\ITraceControl.h"
#ifndef ITraceControlPtr // for portability I don't think this header defines this.
	DEFINE_COM_PTR(ITraceControl);
#endif

using namespace fwutil;  // Rect and Point classes

//:>**********************************************************************************
//:>	Implementations.
//:>**********************************************************************************

// Forward declarations for unit test friends.
namespace TestLanguage
{
	class TestLgWritingSystem;
	class TestLgWritingSystemFactory;
	extern void CreateTestWritingSystemFactory(ILgWritingSystemFactory ** ppwsf);
	extern HRESULT CreateTestWritingSystem(ILgWritingSystemFactory * pwsf, int ws,
		const wchar * pszWs);
};

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

// Conceptual model of an writing system
#ifdef LANG_MODEL
#include "LgSpec.h"
#include "LgCharOverride.h"
#include "LgCharPropSpec.h"
#include "LgCharSpec.h"
#include "LgCollaterSpec.h"
#include "LgComponent.h"
#include "LgConverterSpec.h"
#include "LgConvertStringSpec.h"
#include "LgExternalSpecComponent.h"
#include "LgLineBreakSpec.h"
#include "LgLineBreakSpaceSpec.h"
#include "LgOwnedSpecComponent.h"
#include "LgNumericConverterSpec.h"
#include "LgRenderSpec.h"
#include "LgRomanRendererSpec.h"
#include "LgSpellCheckSpec.h"
#include "LgSysCollaterSpec.h"
#include "LgTokenizerSpec.h"
#include "LgUnicodeCollaterSpec.h"
#include "LgUserClassSpec.h"
#include "LgWfiCheckerSpec.h"
#include "LgWinRendSpec.h"
#include "LgWordBreakSpaceSpec.h"
#endif // LANG_MODEL

// these are a gray area, including aspects of both model and engine
#include "LgCollation.h"
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

#include "LgWritingSystem.h"

// Engines
#include "LgIcuCharPropEngine.h"
#include "LgUnicodeCollater.h"
#include "LgIcuCollator.h"
class RomRenderEngine;
DEFINE_COM_PTR(RomRenderEngine);
class UniscribeEngine;
DEFINE_COM_PTR(UniscribeEngine);
#include "RomRenderSegment.h"
#include "RomRenderEngine.h"
#include "LgSimpleEngines.h"
#include "LgCPWordTokenizer.h"
#include "LgNumericEngine.h"
#include "LgInputMethodEditor.h"
#include "UniscribeSegment.h"
#include "UniscribeEngine.h"

// Other tools
#include "LgFontManager.h"
#include "LgTsStringPlus.h"
#include "LgTsDataObject.h"
#include "LgWritingSystemFactory.h"
#include "FwStyledText.h"
#include "WriteXml.h"		// From AppCore.
#include "xmlparse.h"
#include "LgKeymanHandler.h"
#include "LgCodePageEnumerator.h"
#include "LgIcuWrappers.h"
#include "LangDef.h"
#include "LgTextServices.h"

#include "LgIcuLocking.h"
#include "RegexMatcherWrapper.h"

#endif //!LANGUAGE_H
