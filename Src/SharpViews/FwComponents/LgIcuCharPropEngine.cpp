/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgIcuCharPropEngine.cpp
Responsibility: Charley Wesley
Last reviewed: Not yet.

Description:
	The character property engine provides character properties from the Unicode character
	property tables, using ICU to do so.  A solution will be implemented that allows the user
	to add custom characters; at present they can be added only by editing the XML file.

	This is a thread-safe, "agile" component.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)
#include <limits.h>
#include "Set_i.cpp"
#include "Vector_i.cpp"
#include "xmlparse.h"
#if WIN32
#include <io.h>
#endif
#include "FwXml.h"
#include "FwCellarRes.h"
#include "FwStyledText.h"
#undef THIS_FILE
DEFINE_THIS_FILE
// Doesn't seem to be used. #define ICU_2_2_BREAKING

#if !WIN32
#include "LocaleIndex.h"
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <cstdio>
#include <iostream>
#include <fstream>
using namespace std;
#endif

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables, etc.
//:>********************************************************************************************
template class Vector<byte>;

// This table now maps from the return values of ICU's u_charType to numbers that are
// consistent with the LgGeneralCharCategory constants defined in LanguageTlb.h.  Used
// in ConvertCharCategory.
static LgGeneralCharCategory g_rggcc[] = {
	kccCn,
	kccLu,
	kccLl,
	kccLt,
	kccLm,
	kccLo,
	kccMn,
	kccMe,
	kccMc,
	kccNd,
	kccNl,
	kccNo,
	kccZs,
	kccZl,
	kccZp,
	kccCc,
	kccCf,
	kccCo,
	kccCs,
	kccPd,
	kccPs,
	kccPe,
	kccPc,
	kccPo,
	kccSm,
	kccSc,
	kccSk,
	kccSo,
	kccPi,
	kccPf
};	//hungarian gcc

typedef enum
{
	kmccLetter,
	kmccMark,
	kmccNumber,
	kmccSeparator,
	kmccOther,
	kmccPunctuation,
	kmccSymbol
} LgMainCharCategory; // Hungarian mcc

// Table to map from full unicode general category as defined in LgGeneralCharCategory to
// main category as defined in LgMainCharCategory. The entries in this table should
// exactly parallel the definition of LgGeneralCharCategory
static LgMainCharCategory g_rgmcc[] = {
	kmccLetter, // = Letter, Uppercase
	kmccLetter, // = Letter, Lowercase
	kmccLetter, // = Letter, Titlecase
	kmccLetter, // = Letter, Modifier
	kmccLetter, // = Letter, Other

	kmccMark, // = Mark, Non-Spacing
	kmccMark, // = Mark, Spacing Combining
	kmccMark, // = Mark, Enclosing

	kmccNumber, // = Number, Decimal Digit
	kmccNumber, // = Number, Letter
	kmccNumber, // = Number, Other

	kmccSeparator, // = Separator, Space
	kmccSeparator, // = Separator, Line
	kmccSeparator, // = Separator, Paragraph

	kmccOther, // = Other, Control
	kmccOther, // = Other, Format
	kmccOther, // = Other, Surrogate
	kmccOther, // = Other, Private Use
	kmccOther, // = Other, Not Assigned

	kmccPunctuation, // = Punctuation, Connector
	kmccPunctuation, // = Punctuation, Dash
	kmccPunctuation, // = Punctuation, Open
	kmccPunctuation, // = Punctuation, Close
	kmccPunctuation, // = Punctuation, Initial quote
	kmccPunctuation, // = Punctuation, Final quote
	kmccPunctuation, // = Punctuation, Other

	kmccSymbol, // = Symbol, Math
	kmccSymbol, // = Symbol, Currency
	kmccSymbol, // = Symbol, Modifier
	kmccSymbol, // = Symbol, Other
};

// This table maps from the return values of ICU's u_charDirection to numbers that are
// consistent with the LgBidiCategory constants defined in LanguageTlb.h.  Used in
// get_BidiCategory.
static LgBidiCategory g_rgbic[] = {
	kbicL,
	kbicR,
	kbicEN,
	kbicES,
	kbicET,
	kbicAN,
	kbicCS,
	kbicB,
	kbicS,
	kbicWS,
	kbicON,
	kbicLRE,
	kbicLRO,
	kbicAL,
	kbicRLE,
	kbicRLO,
	kbicPDF,
	kbicNSM,
	kbicBN
};  //hungarian bic

//This table maps from the return values of ICU's u_getIntPropertyValue((int),
//UCHAR_LINE_BREAK) to numbers consistent with John's original LgLBP enumeration.
//Used in GetLineBreakInfo.
static LgLBP g_rglbp[] = {
	klbpXX,
	klbpAI,
	klbpAL,
	klbpB2,
	klbpBA,
	klbpBB,
	klbpBK,
	klbpCB,
	klbpCL,
	klbpCM,
	klbpCR,
	klbpEX,
	klbpGL,
	klbpHY,
	klbpID,
	klbpIN,
	klbpIS,
	klbpLF,
	klbpNS,
	klbpNU,
	klbpOP,
	klbpPO,
	klbpPR,
	klbpQU,
	klbpSA,
	klbpSG,
	klbpSP,
	klbpSY,
	klbpZW
};  //hungarian lbp

// Codes for whole-number values: used to interpret the nv field of LgCharPropsFlds.
// Values up to 20 mean themselves. Higher values map onto these special values.
// This allows us to represent all the numberic digit values actually used in Unicode
// using just 6 bits.
typedef enum {
	klcpfNoNumericVal = 63, // Digit field when no numeric value
	klcpfFraction = 62,     // Special case fractional value.
	klcpfFifty = 21,        // Values 0-20 occur; also the values here...
	klcpfHundred = 22,
	klcpfFiveHundred = 23,
	klcpfThousand = 24,
	klcpfFiveThousand = 25,
	klcpfTenThousand = 26,
	klcpfThirty = 27,
	klcpfForty = 28,
	klcpfSixty = 29,
	klcpfSeventy = 30,
	klcpfEighty = 31,
	klcpfNinety = 32
} LgChPrNumVals;    // Hungarian lcpv

// The three types of case recognized in Unicode.
//enum {
//	kcaseUpper,
//	kcaseLower,
//	kcaseTitle
//} LgCase; // Hungarian case

//:>********************************************************************************************
//:>	Constructor/Destructor
//:>********************************************************************************************

LgIcuCharPropEngine::LgIcuCharPropEngine()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_pLocale = NULL;
	m_pBrkit = NULL;
#if WIN32
	CoCreateFreeThreadedMarshaler(static_cast<IUnknown *>(static_cast<ILgCharacterPropertyEngine *>(this)),
		&m_qunkMarshaler);
#endif
	StrUni stuUserWs(kstidUserWs);
	// We at least need to initialize the ICU data directory...
	CheckHr(Initialize(stuUserWs.Bstr(), NULL, NULL, NULL));
}

LgIcuCharPropEngine::LgIcuCharPropEngine(BSTR bstrLanguage, BSTR bstrScript, BSTR bstrCountry, BSTR bstrVariant)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_pLocale = NULL;
	m_pBrkit = NULL;
#if WIN32
	CoCreateFreeThreadedMarshaler(static_cast<IUnknown *>(static_cast<ILgCharacterPropertyEngine *>(this)),
		&m_qunkMarshaler);
#endif
	CheckHr(Initialize(bstrLanguage, bstrScript, bstrCountry, bstrVariant));
}

LgIcuCharPropEngine::~LgIcuCharPropEngine()
{
	ModuleEntry::ModuleRelease();
	if (m_pLocale)
	{
		delete m_pLocale;
		m_pLocale = NULL;
	}
	CleanupBreakIterator();
	// Be extra paranoid...
	if (m_pBrkit != NULL)
	{
		delete m_pBrkit;
		m_pBrkit = NULL;
	}
}

// Set up a break iterator if we don't currently have one.
void LgIcuCharPropEngine::SetupBreakIterator()
{
	if (m_pBrkit)
		return;
	//setting up our breakiterator as well so it will always be ready
	UErrorCode uerr = U_ZERO_ERROR;
	// Getting a BreakIterator locks icudt28l_uprops.icu until the BreakIterator memory is
	// freed and u_cleanup() is called. To allow it to be freed as needed we make a callback.
	m_pBrkit = BreakIterator::createLineInstance(*m_pLocale, uerr);
	if (!U_SUCCESS(uerr))
	{
		if (m_pBrkit)
			delete m_pBrkit;
		ThrowNice(E_FAIL, kstidICUBrkInit);
	}
}

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language1.LgIcuCharPropEngine"),
	&CLSID_LgIcuCharPropEngine,
	_T("SIL char properties"),
	_T("Both"),
	&LgIcuCharPropEngine::CreateCom);


void LgIcuCharPropEngine::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgIcuCharPropEngine> qzpropeng;

	qzpropeng.Attach(NewObj LgIcuCharPropEngine());		// ref count initially 1
	CheckHr(qzpropeng->QueryInterface(riid, ppv));
}

/*----------------------------------------------------------------------------------------------
	Return a singleton character property engine for pure Unicode character manipulation.
----------------------------------------------------------------------------------------------*/
HRESULT LgIcuCharPropEngine::GetUnicodeCharProps(ILgCharacterPropertyEngine ** pplcpe)
{
	AssertPtr(pplcpe);

	ILgCharacterPropertyEnginePtr qlcpeUnicode;
	StrUtil::InitIcuDataDir();
	ISimpleInitPtr qsimi;
	// Make an instance; initially get the interface we need to initialize it.
	LgIcuCharPropEngine::CreateCom(NULL, IID_ISimpleInit, (void **)&qsimi);
	if (!qsimi)
		ThrowHr(WarnHr(E_UNEXPECTED));
	// This engine does not need any init data.
	CheckHr(qsimi->InitNew(NULL, 0));
	// If initialization succeeds, get the requested interface.
	CheckHr(qsimi->QueryInterface(IID_ILgCharacterPropertyEngine, (void **)&qlcpeUnicode));
	AssertPtr(qlcpeUnicode);
	*pplcpe = qlcpeUnicode.Detach();

	return S_OK;
}

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgIcuCharPropEngine::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgCharacterPropertyEngine *>(this));
	else if (riid == IID_ILgCharacterPropertyEngine)
		*ppv = static_cast<ILgCharacterPropertyEngine *>(this);
	else if (riid == IID_ISimpleInit)
		*ppv = static_cast<ISimpleInit *>(this);
	else if (riid == IID_ILgIcuCharPropEngine)
		*ppv = static_cast<ILgIcuCharPropEngine *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<ILgCharacterPropertyEngine *>(this),
			IID_ISimpleInit, IID_ILgCharacterPropertyEngine);
		return S_OK;
	}
#if WIN32
	else if (riid == IID_IMarshal)
		return m_qunkMarshaler->QueryInterface(riid, ppv);
#endif
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	ISimpleInit Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize an instance. This method is supported to allow creating instances using
	ClassInitMoniker, which may become the standard way we initialize Language engines.
	However, this class does not actually need initialization.

	If you do want to create a moniker, you can do it like this:

	@code{
	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	hr = qcim->InitNew(CLSID_LgSystemCollater, NULL, 0);
	}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::InitNew(const BYTE * prgb, int cb)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgb, cb);
	if (cb != 0)
		ThrowInternalError(E_INVALIDARG, "Expected empty init string");

	END_COM_METHOD(g_fact, IID_ISimpleInit);
}

/*----------------------------------------------------------------------------------------------
	Return the initialization value previously set by InitNew.

	@param pbstr Pointer to a BSTR for returning the initialization data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_InitializationData(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComArgPtr (pbstr)
	*pbstr = NULL;
	return S_OK;
	END_COM_METHOD(g_fact, IID_ISimpleInit);
}

//:>********************************************************************************************
//:>	ILgCharacterPropertyEngine Methods
//:>********************************************************************************************

// ENHANCE JohnT: if asked about a character not defined in the standard, should we give
// some sort of default answer, or report an error? The current implementation does
// the former.

/*----------------------------------------------------------------------------------------------
	Check a unicode character is valid and throw an exception if not. This is broken out into
	a separate routine in case we want to change to some milder form of failure than an
	internal error.
----------------------------------------------------------------------------------------------*/
void LgIcuCharPropEngine::CheckUnicodeChar(int ch)
{
	if (!IsPlausibleUnicodeCh(ch))
		ThrowNice(E_FAIL, kstidInvalidUnicode);
}

/*----------------------------------------------------------------------------------------------
	The general character category, as defined by Unicode.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_GeneralCategory(int ch, LgGeneralCharCategory * pcc)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pcc);
	CheckUnicodeChar(ch);

	*pcc = GenCategory(ch);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Get the bidi category info for the specified character.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_BidiCategory(int ch, LgBidiCategory * pbic)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pbic);
	CheckUnicodeChar(ch);
	LOCK(m_mutex)
	{
		CharacterPropertyObject * pcpoOverride;
		pcpoOverride = GetOverrideChar((UChar32)ch);
		if (pcpoOverride)
		{
			*pbic = pcpoOverride->bicBidiCategory;
			return S_OK;
		}
	}
	*pbic = LgBidiCategory(int(u_charDirection(ch)));
	*pbic = ConvertBidiCategory(int(*pbic));

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Is it a letter (Unicode category L)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsLetter(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (g_rgmcc[GenCategory(ch)] == kmccLetter);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Is it a word-forming character (Unicode categories L*, Mn, or Mc)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsWordForming(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	LOCK(m_mutex)
	{
		if( m_siWordformingOverrides.IsMember(ch))
		{
			*pfRet = true;
			return S_OK;
		}
	}

	switch (GenCategory(ch))
	{
	case kccLu:
	case kccLl:
	case kccLt:
	case kccLm:
	case kccLo:
	case kccMn:
	case kccMc:
	case kccSk:		// per Martin Hosken's reading of UAX#29.  See LT-5518.
		*pfRet = true;
		break;
	default:
		*pfRet = false;
		break;
	}

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Unicode general category P
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsPunctuation(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (g_rgmcc[GenCategory(ch)] == kmccPunctuation);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Unicode general category N
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsNumber(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (g_rgmcc[GenCategory(ch)] == kmccNumber);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}


/*----------------------------------------------------------------------------------------------
	Unicode general category Z
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsSeparator(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (g_rgmcc[GenCategory(ch)] == kmccSeparator);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Unicode general category S
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsSymbol(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);

	*pfRet = (g_rgmcc[GenCategory(ch)] == kmccSymbol);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Unicode general category M
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsMark(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);

	*pfRet = (g_rgmcc[GenCategory(ch)] == kmccMark);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Unicode general category C
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsOther(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (g_rgmcc[GenCategory(ch)] == kmccOther);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Unicode general category Lu
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsUpper(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (GenCategory(ch) == kccLu);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Unicode general category Ll
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsLower(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (GenCategory(ch) == kccLl);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}


/*----------------------------------------------------------------------------------------------
	Unicode general category Lt, typically digraph with first upper
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsTitle(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (GenCategory(ch) == kccLt);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}


/*----------------------------------------------------------------------------------------------
	Unicode general category Lm
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsModifier(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (GenCategory(ch) == kccLm);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}


/*----------------------------------------------------------------------------------------------
	Unicode general category Lo
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsOtherLetter(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (GenCategory(ch) == kccLo);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Unicode general category Ps, opening punctuation, like left paren
	ENHANCE JohnT: Should opening include Pi, Initial quote?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsOpen(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (GenCategory(ch) == kccPs);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Unicode general category Pe, closing punctuation, like right paren
	ENHANCE JohnT: Should closing include Pf, final quote?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsClose(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (GenCategory(ch) == kccPe);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Unicode general category Pc, middle of a word, like hyphen
	ENHANCE JohnT: should word medial include Pd (Punctuation, dash)?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsWordMedial(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (GenCategory(ch) == kccPc);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Unicode general category Cc
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsControl(int ch, ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = (GenCategory(ch) == kccCc);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Convert character to lower case
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_ToLowerCh(int ch, int * pch)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pch);
	CheckUnicodeChar(ch);
	LOCK(m_mutex)
	{
		CharacterPropertyObject *pcpoOverride;
		pcpoOverride = GetOverrideChar((UChar32)ch);
		if (pcpoOverride)
		{
			if (pcpoOverride->uch32Lowercase != 0)
				*pch = pcpoOverride->uch32Lowercase;
			else
				*pch = ch;

			return S_OK;
		}
	}
	*pch = u_tolower(ch);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Convert character to upper case
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_ToUpperCh(int ch, int * pch)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pch);
	CheckUnicodeChar(ch);
	LOCK(m_mutex)
	{
		CharacterPropertyObject *pcpoOverride;
		pcpoOverride = GetOverrideChar((UChar32)ch);
		if (pcpoOverride)
		{
			if (pcpoOverride->uch32Uppercase != 0)
				*pch = pcpoOverride->uch32Uppercase;
			else
				*pch = ch;

			return S_OK;
		}
	}
	*pch = u_toupper(ch);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Convert character to title case
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_ToTitleCh(int ch, int * pch)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pch);
	CheckUnicodeChar(ch);
	LOCK(m_mutex)
	{
		CharacterPropertyObject *pcpoOverride;
		pcpoOverride = GetOverrideChar((UChar32)ch);
		if (pcpoOverride)
		{
			if (pcpoOverride->uch32Titlecase != 0)
				*pch = pcpoOverride->uch32Titlecase;
			else
				*pch = ch;

			return S_OK;
		}
	}
	*pch = u_totitle(ch);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Convert string to lower case
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::ToLower(BSTR bstr, BSTR * pbstr)
{
	BEGIN_COM_METHOD

	// Arg checking is in ConvertCase.
	ConvertCase(bstr, pbstr, kccLl);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Convert string to upper case
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::ToUpper(BSTR bstr, BSTR * pbstr)
{
	BEGIN_COM_METHOD

	// Arg checking is in ConvertCase.
	ConvertCase(bstr, pbstr, kccLu);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}


/*----------------------------------------------------------------------------------------------
	Convert string to title case
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::ToTitle(BSTR bstr, BSTR * pbstr)
{
	BEGIN_COM_METHOD

	// Arg checking is in ConvertCase.
	ConvertCase(bstr, pbstr, kccLt);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Convert to lower case
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::ToLowerRgch(OLECHAR * prgchIn, int cchIn,
	OLECHAR * prgchOut, int cchOut, int * pcchRet)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgchIn, cchIn);
	ChkComArrayArg(prgchOut, cchOut);
	ChkComArgPtr(pcchRet);

	ConvertCaseRgch(prgchIn, cchIn, prgchOut, cchOut, pcchRet, kccLl);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Convert to upper case.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::ToUpperRgch(OLECHAR * prgchIn, int cchIn,
	OLECHAR * prgchOut, int cchOut, int * pcchRet)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgchIn, cchIn);
	ChkComArrayArg(prgchOut, cchOut);
	ChkComArgPtr(pcchRet);

	ConvertCaseRgch(prgchIn, cchIn, prgchOut, cchOut, pcchRet, kccLu);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Convert to title case
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::ToTitleRgch(OLECHAR * prgchIn, int cchIn,
	OLECHAR * prgchOut, int cchOut, int * pcchRet)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgchIn, cchIn);
	ChkComArrayArg(prgchOut, cchOut);
	ChkComArgPtr(pcchRet);

	ConvertCaseRgch(prgchIn, cchIn, prgchOut, cchOut, pcchRet, kccLt);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Returns true if the specified code point is a member of the specified user defined
	class of code points. Nathan thinks we should restrict class names to a single
	character to make patterns containing them more readable. Example \C\Vxyz for all
	words containing a consonant, followed by a vowel, followed by xyz.
	This initial Unicode engine just says no, it isn't.
		TENHANCE: there should almost definitely be methods for setting up user-defined
		character classes. Or, should that be on a separate class, instantiated
		per-old writing system, and this pure Unicode engine is shared somehow by them all?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_IsUserDefinedClass(int ch, int chClass,
	ComBool * pfRet)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pfRet);
	CheckUnicodeChar(ch);

	*pfRet = false;

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Converts a string into another string in which all characters are replaced with their
	sound alike equivalent. For example, if for this language s and z where specified to
	sound alike, we could take s as the generic form and convert all z's to this form.
	Note that we need to support the possibility that 0 (empty code point) and x and y
	sound alike which means that all x's and 's and y's will be ignored when testing for
	sound alikeness.
	ENHANCE JohnT: Does this need a separate engine? Is it part of the character classification
	engine if any? Is it part of the search engine if any?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_SoundAlikeKey(BSTR bstrValue, BSTR * pbstrKey)
{
	BEGIN_COM_METHOD
	ChkComBstrArgN(bstrValue);
	ChkComArgPtr(pbstrKey);

	*pbstrKey = NULL;

	//ENHANCE: figure some way to implement, if we decide to keep method
	Assert(false);
	ThrowInternalError(E_NOTIMPL, "SoundAlikeKey");

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Official Unicode character name (character database field 1).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_CharacterName(int ch, BSTR * pbstrName)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrName);

	OLECHAR rgOLEch[256]; //longer than longest standard name
	OLECHAR * pchw = rgOLEch;
	char rgch[256];
	memset(rgch, 0, sizeof(rgch));
	char * prgch = rgch;
	UErrorCode uerr = U_ZERO_ERROR;
	int cch=0;

	int cChars = 0;
	cChars = u_charName(ch, U_UNICODE_CHAR_NAME, rgch, 255, &uerr);

	if (!U_SUCCESS(uerr))
		ThrowNice(E_FAIL, kstidICUCharName);

	for (;*prgch;)
	{
		*pchw++ = *prgch++;
		cch++;
	}
	*prgch = 0;  // When the "for" loop terminated, this was already 0 !
	*pbstrName = SysAllocStringLen(rgOLEch, cch);
	if (!*pbstrName)
		ThrowOutOfMemory();

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

//	TODO: Decide what we want if it doesn't decompose
/*----------------------------------------------------------------------------------------------
	How this character decomposes (Unicode char database field 5).
	(May be recursive)
	Empty string if it does not decompose; otherwise at least one character.
	(Currently returns the given string if it doesn't decompose)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_Decomposition(int ch, BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pbstr);
	*pbstr = NULL;

	// Quick validity check for ch.
	if (!IsPlausibleUnicodeCh((UCHAR)ch))
		ThrowInternalError(E_INVALIDARG);

	int decompType = u_getIntPropertyValue(ch, UCHAR_DECOMPOSITION_TYPE);

	UErrorCode uerr = U_ZERO_ERROR;
	UNormalizationMode normMode = UNORM_NFD;

	// Convert input character to UTF-16 string.
	const int cchSrc = 3;
	wchar rgchwSrc[3];
	u_strFromUTF32(rgchwSrc, cchSrc, NULL, (UChar32 *)&ch, 1, &uerr);
	if (U_FAILURE(uerr))
		ThrowInternalError(E_FAIL);

	// Note: it may be safe to always do NFKD since it performs compatability decomposition
	// then canonical, but it is not always safe to do NFD since it will only do the canonical
	// decomposition. This may be recursive, i.e. not just returning the first decomposition,
	// but decomposing that, if possible.

	// This uses compatability decomposition if the type is not canonical
	// See http://www.unicode.org/Public/UNIDATA/UCD.html#Character_Decomposition_Mappings
	if (decompType != U_DT_NONE)
		normMode = UNORM_NFKD;

	// Since we are only decomposing 1 character, it seems highly unlikely
	// that is will decompose into more than 10 characters
	const int cchBuff = 16;
	wchar chwBuff[cchBuff];

	// http://oss.software.ibm.com/icu/apiref/unorm_8h.html#a17
	int cchResultLength = unorm_normalize(rgchwSrc, -1, normMode, 0, chwBuff, cchBuff, &uerr);

	// If we didn't allocate enough memory try again.
	if (uerr == U_BUFFER_OVERFLOW_ERROR)
	{
		wchar * pchwBuff = new wchar[cchResultLength +1 ];
		// Reset the error code so that unorm_normalize will function correctly.
		uerr = U_ZERO_ERROR;
		unorm_normalize(rgchwSrc, -1, normMode, 0, pchwBuff, cchResultLength + 1, &uerr);
		if (U_SUCCESS(uerr))
		{
			*pbstr = SysAllocStringLen(pchwBuff, cchResultLength);
			delete [] pchwBuff;
		}
		else
		{
			delete [] pchwBuff;
			ThrowInternalError(E_FAIL);
		}
	}
	else if (U_SUCCESS(uerr))
		*pbstr = SysAllocStringLen(chwBuff, cchResultLength);
	else
	{
		ThrowInternalError(E_FAIL);
	}

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}


/*----------------------------------------------------------------------------------------------
	How this character decomposes (Unicode char database field 5).
	Empty string if it does not decompose; otherwise at least one character.
	If cchMax is zero, just compute the required length.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::DecompositionRgch(int ch, int cchMax, OLECHAR * prgch,
	int * pcch, ComBool * pfHasDecomp)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pfHasDecomp);
	ChkComArrayArg(prgch, cchMax);
	ChkComArgPtr(pcch);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Get the recursive canonical decomposition of a character. Empty string if it does not
	decompose at all.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_FullDecomp(int ch, BSTR * pbstrOut)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstrOut);
	CheckUnicodeChar(ch);

	int cch;
	ComBool fHasDecomp = false;

	//passing 0 and null just produces a length
	FullDecompRgch(ch, 0, NULL, &cch, &fHasDecomp);
	if (!fHasDecomp) //the char doesn't have a decomposition, return an empty string
		return S_OK;

	//SmartBstr bstrOut;
	//bstrOut = SysAllocStringLen(NULL, cch);
	//if (!bstrOut)
	//	ThrowOutOfMemory();
	OLECHAR *prgchOut = new OLECHAR[cch];

	FullDecompRgch(ch, cch, prgchOut, &cch, &fHasDecomp);
	//const OLECHAR *temp = bstrOut.Chars();
	//cch = int(temp[0]);
	//*pbstrOut = bstrOut.Detach();
	//bstrOut.Copy(pbstrOut);
	//ReleaseBstr(bstrOut);

	*pbstrOut = SysAllocStringLen(prgchOut, cch);

	delete prgchOut;

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Get the recursive canonical decomposition of a character. Returns the character itself if it
	does not decompose at all.
	Passing in 0 and NULL for cchMax and prgch will return the number of characters needed
	in pcch.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::FullDecompRgch(int ch, int cchMax1, OLECHAR * prgch,
	int * pcch, ComBool * pfHasDecomp)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgch, cchMax1);
	ChkComOutPtr(pcch);
	ChkComOutPtr(pfHasDecomp);
	CheckUnicodeChar(ch);

	UChar32 uch = ch;
	UnicodeString ustrSrc = uch;
	//ustrSrc[1] = 0;
	UnicodeString ustrResult;
	UErrorCode uerr = U_ZERO_ERROR;
	Normalizer norm(ustrSrc, UNORM_NFD);

	norm.normalize(ustrSrc, UNORM_NFD, 0, ustrResult, uerr);

	if (!U_SUCCESS(uerr))
		ThrowNice(E_FAIL, kstidICUDecomp);

	if (ustrSrc == ustrResult) //meaning that the character has no decomposition
	{
		if (prgch)
			*prgch = (OLECHAR) ch;
		*pcch = 1;
		return S_OK;
	}

	*pfHasDecomp = true;
	if ((cchMax1 < ustrResult.length()) && (cchMax1 > 0))
		ThrowNice(E_FAIL, kstidBufferTooSmall);

	*pcch = ustrResult.length();
	if (cchMax1 > 0)
		ustrResult.extract(0, ustrResult.length(), prgch);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}


/*----------------------------------------------------------------------------------------------
	Character's value as a digit. Reports E_FAIL if the character is not
	one of the Nd, Nl, or No types.
	ENHANCE: embed the character's hex value in the error message.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_NumericValue(int ch, int * pn)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pn);
	CheckUnicodeChar(ch);
	LgGeneralCharCategory cc = GenCategory(ch);
	if (cc != kccNd && cc != kccNl && cc != kccNo)
		ThrowNice(E_FAIL, kstidNoNumeric);

	*pn = u_charDigitValue(ch);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Character's combining class (Unicode char database field 3)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_CombiningClass(int ch, int * pn)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pn);
	CheckUnicodeChar(ch);
	LOCK(m_mutex)
	{
		CharacterPropertyObject * cpoOverride = GetOverrideChar((UChar32)ch);
		if (cpoOverride)
		{
			*pn = cpoOverride->nCombiningClass;
			return S_OK;
		}
	}
	*pn = u_getCombiningClass(ch);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Comment about this character (Unicode char database field 11).
	ENHANCE: implement if it seems worth it. Currently, I'm inclined to think this info
	is probably only really useful in connection with overridden characters, and therefore
	only of concern to the override implementation of this interface. If we decide to go with
	that definition, change this implementation to return S_OK and an empty string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_Comment(int ch, BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);

	OLECHAR rgOLEch[256]; //longer than longest standard name
	OLECHAR * pchw = rgOLEch;
	char rgch[256];
	memset(rgch, 0, sizeof(rgch));
	char * prgch = rgch;
	UErrorCode uerr = U_ZERO_ERROR;
	int cch=0;

	int cChars = 0;
	cChars = u_getISOComment(ch, rgch, 255, &uerr);

	if (!U_SUCCESS(uerr))
		ThrowNice(E_FAIL, kstidICUCharName);

	for (;*prgch;)
	{
		*pchw++ = *prgch++;
		cch++;
	}
	*prgch = 0;  // When the "for" loop terminated, this was already 0 !
	*pbstr = SysAllocStringLen(rgOLEch, cch);
	if (!*pbstr)
		ThrowOutOfMemory();

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Get the line breaking properties for a range of characters.
	The output bytes have the following format:
	Lower 5 bits - actual line breaking property from table.
	High bit     - set if the general character property is "Zs" , i.e. space of some kind.
	This added bit is used later on to tell the renderer that characters so marked can be
	ignored if they are at the end of a line.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::GetLineBreakProps(const OLECHAR * prgchIn, int cchIn,
	byte * prglbpOut)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgchIn, cchIn);
	ChkComArrayArg(prglbpOut, cchIn);

	LOCK(m_mutex)
	{
		int ich;
		int ch;
		CharacterPropertyObject * cpoOverride;
		for (ich = 0; ich < cchIn; ++ich)
		{
			ch = *prgchIn++;

			cpoOverride = GetOverrideChar((UChar32)ch);
			if (cpoOverride)
				*prglbpOut = (byte)cpoOverride->lbpLineBreak;
			else
			*prglbpOut = (byte)g_rglbp[u_getIntPropertyValue(ch, UCHAR_LINE_BREAK)];
			if (GenCategory(ch) == kccZs)
				*prglbpOut |= 0x80;
			++prglbpOut;
		}
	}

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

//:Ignore (no reason to have this table or the comments about it in the web page)
const byte LgIcuCharPropEngine::s_rglbs[32][32] = {
//    AI AL B2 BA BB BK CB CL CM CR EX GL HY ID IN IS LF NS NU OP PO PR QU SA SG SP SY XX ZW
/*AI*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 3, 3, 4, 3, 3, 2, 3, 2, 3, 3, 3, 0, 1, 3, 1},
/*AL*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 3, 3, 4, 3, 3, 2, 3, 2, 3, 3, 3, 0, 1, 3, 1},
/*B2*/{2, 2, 1, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 2, 2, 2, 2, 3, 2, 2, 0, 1, 2, 1},
/*BA*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 2, 2, 2, 2, 3, 2, 2, 0, 1, 2, 1},
/*BB*/{1, 1, 1, 3, 2, 4, 1, 1, 0, 4, 1, 1, 3, 1, 1, 1, 4, 3, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1},
/*BK*/{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
/*CB*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 2, 2, 2, 2, 3, 2, 2, 0, 1, 2, 1},
/*CL*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 2, 2, 1, 2, 3, 2, 2, 0, 1, 2, 1},
/*CM*/{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
/*CR*/{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
/*EX*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 2, 2, 3, 2, 3, 2, 2, 0, 1, 2, 1},
/*GL*/{3, 3, 3, 3, 3, 4, 3, 1, 0, 4, 1, 3, 3, 3, 3, 3, 4, 3, 3, 3, 3, 3, 3, 3, 3, 0, 1, 3, 1},
/*HY*/{2, 2, 2, 2, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 2, 3, 2, 2, 2, 2, 2, 2, 0, 1, 2, 1},
/*ID*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 3, 3, 4, 3, 2, 2, 3, 2, 3, 2, 2, 0, 1, 2, 1},
/*IN*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 1, 3, 4, 3, 2, 2, 3, 2, 3, 2, 2, 0, 1, 2, 1},
/*IS*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 3, 2, 2, 2, 3, 2, 2, 0, 1, 2, 1},
/*LF*/{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
/*NS*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 3, 2, 3, 2, 3, 3, 3, 0, 1, 3, 1},
/*NU*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 3, 3, 4, 3, 3, 2, 1, 2, 3, 3, 3, 0, 1, 3, 1},
/*OP*/{1, 1, 1, 1, 1, 4, 1, 1, 0, 4, 1, 1, 1, 1, 1, 1, 4, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1},
/*PO*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 3, 2, 3, 2, 3, 3, 3, 0, 1, 3, 1},
/*PR*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 3, 2, 3, 4, 3, 1, 1, 2, 2, 3, 2, 2, 0, 1, 2, 1},
/*QU*/{3, 3, 3, 3, 2, 4, 3, 1, 0, 4, 1, 3, 3, 3, 3, 3, 4, 3, 3, 1, 3, 3, 3, 3, 3, 0, 1, 3, 1},
/*SA*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 3, 3, 4, 3, 3, 2, 3, 2, 3, 3, 3, 0, 1, 3, 1},
/*SG*/{4, 4, 4, 4, 4, 4, 4, 4, 0, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 0, 4, 4, 4},
/*SP*/{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
/*SY*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 3, 2, 3, 2, 3, 2, 2, 0, 1, 2, 1},
/*XX*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 3, 3, 4, 3, 3, 2, 3, 2, 3, 3, 3, 0, 1, 3, 1},
/*ZW*/{2, 2, 2, 2, 2, 4, 2, 1, 0, 4, 1, 2, 2, 2, 2, 2, 4, 2, 2, 2, 2, 2, 2, 2, 2, 0, 1, 2, 1}
};
// Notes on the table:
//	1. Rows BK, CM, CR, LF and SP are not accessed by the line breaking algorithm.
//  2. Columns CM and SP are not accessed by the algorithm.
//  3. XX (character unassigned in Unicode) is represented in the table with the same
//     properties as AL. Also AI and SA. The default option of CB same as B2 has been chosen.
//  4. Table values: 0: invalid                     1: word break prohibited between pair
//                   2: break allowed between pair  3: break allowed when spaces separate pair
//                   4: word break and letter break both prohibited between pair
//  5. Rows are labelled according to the "Before" value, columns according to the "After"
//     value.
//  6. The ordering of the rows and columns of this table must be kept in line with the
//     definitions of klbpAI, etc in the LgLBP enum.
//  7. The size of the table (32x32) is chosen in the hope of greater access efficiency.
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Get the line break status array corresponding to an array of line break properties.
	Size of arrays is cb bytes.
	Assumes that a break is always allowed after the last character.
	Unknown or unhandled lbps (XX, CB, SA, AI) are handled as AL.
	The enum LgLineBreakStatus {kflbsBrk, kflbsSpace, kflbsBrkL} is defined in Render.idh. These
	values are boolean flags which may be combined. However, kflbsBrkL is never set unless the
	other two flags are not set.
	The algorithm adopted here is a Pair table based algorithm similar to that described
	in Unicode Technical Report #14. A careful study of that TR is recommended before you try
	to understand this code fully.
	Mandatory breaking lbps (BK, CR, LF) are not expected but generate kflbsBrk except for CR
	when followed by LF which generates !klbsBrk for the CR property itself.
	The table has some values set to 0. If one of these is returned it indicates an error,
	since it should not be possible to access them except with illegal values of lbp.

	The concept of a "letter break" is not in the TR14 algorith. A letter break is a break
	between characters which is forced because, for example, a single word is longer than
	the line width. Note that even letter breaks are not allowed before CM or after SG.

	"Break" in the notes on this algorithm means "word break" unless indicated otherwise.

	Special note on "space" characters:
		All types of space (17 different characters in Unicode 3) have General Character
		Property Zs, whereas the Line Break Property SP applies only to U+0020. In the input
		array for this algorithm the high bit (0x80) is set for each byte with the Zs property.
		This in turn causes all these characters to be marked as spaces in the output array.
		The only exception is when one is followed by a CM (Combining Mark). As such a space
		can never be at the end of a line, not marking it as a space does not matter.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::GetLineBreakStatus(const byte * prglbpIn,
	int cb, byte * prglbsOut)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prglbpIn, cb);
	ChkComArrayArg(prglbsOut, cb);

	if (!cb) return S_FALSE;

	const byte * pIn;              // pointer to current position in input array
	byte * pOut;                   // pointer to current position in output array
	const byte * pInLast = prglbpIn + cb - 1; // pointer to last element of input array
	int lbpCurrent;                // value of lpb currently considered as first of pair
	int lbpNext;                   // value of lbp currently considered as second of pair
	const byte klbsBS = kflbsBrk | kflbsSpace; // status is space with break allowed after
	const byte klbsN = 0;          // status is non-space with no break allowed after

	int i;
	for (i = 0; i < cb; ++i)
		*(prglbsOut + i) = kflbsBrkL;	// Initialise output array to kflbsBrkL; has the
										// effect of allowing only letter breaks as default.
	pIn = prglbpIn;
	pOut = prglbsOut;

	// assume break is allowed after last character. Also mark it a space, if it is.
	*(pOut + cb -1) = (*pInLast & 0x80) ? klbsBS : (byte)kflbsBrk;
	if (cb == 1)
		return S_OK; // if only one character, we have finished

	lbpCurrent = *pIn & 0x7f;	// clear high bit if set
	if (lbpCurrent == klbpSP || lbpCurrent == klbpCM)
		lbpCurrent = klbpAL;	// make as if a leading SP or CM follows an alphabetic character

	byte lbs;
	for (pIn = prglbpIn; pIn < pInLast; ++pIn) // last character already handled
	{
		lbpNext = *(pIn + 1) & 0x7f;	// clear high bit if set
		if (lbpCurrent == klbpBK || lbpCurrent == klbpLF)
		{
			// Just mark hard breaks as break allowed
			*pOut++ = kflbsBrk;
			// if next line begins with CM or SP we make as if they follow an AL
			lbpCurrent = (lbpNext == klbpSP || lbpNext == klbpCM) ? klbpAL : lbpNext;
			continue;
		}
		if (lbpCurrent == klbpCR)
		{
			// Break allowed (actually mandatory) unless NL follows
			*pOut++ = (lbpNext == klbpLF) ? klbsN : (byte)kflbsBrk;
			// if next line begins with CM or SP we make as if they follow an AL
			lbpCurrent = (lbpNext == klbpSP || lbpNext == klbpCM) ? klbpAL : lbpNext;
			continue;
		}
		if (lbpNext == klbpSP)
		{
			// prevent break before space; do not reset lbpCurrent
			*pOut++ = ((*pIn & 0x7f) == klbpSP) ? (byte)kflbsSpace : klbsN;
			continue;
		}
		if (lbpNext == klbpCM)
		{
			// prevent letter break before CM; do not reset lbpCurrent
			*pOut++ = klbsN;	// (side effect of space before CM not marked as space)
			continue;
		}

		Assert(lbpCurrent >= 0 && lbpCurrent <= klbpZW);
		Assert(lbpNext >= 0 && lbpNext <= klbpZW);
		lbs = s_rglbs[lbpCurrent][lbpNext]; // look up break status in table
		Assert(lbs > 0 && lbs < 5);  // values other than 1 to 4 indicate an internal error

		if (lbs == 2)
			*pOut = kflbsBrk;
		if (lbs == 3 && (*pIn & 0x7f) == klbpSP)
			*pOut = kflbsBrk;
		if (lbs == 4)
			*pOut = klbsN;
		if (*pIn & 0x80)
			*pOut |= kflbsSpace;
		// Note that for some reason low surrogates (as well as high surrogates) are listed
		// with SG line breaking property in Unicode 3.0. This means that letter break is
		// prevented after a surrogate pair, not just after the high part of the pair. The
		// character following a surrogate pair could presumably be a space.
		// If none of these conditions is met, *pOut is left as intialised (kflbsBrkL)
		// which implies "not a space" and "letter break allowed after".

		lbpCurrent = lbpNext;
		++pOut;
	}
	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Get the line break status array for a subrange of characters. This is a shortcut for
	calling ${#GetLineBreakProps} followed by ${#GetLineBreakStatus} on the whole string.
	The logic of the method is roughly a combination of that of the other two.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::GetLineBreakInfo(const OLECHAR * prgchIn, int cchIn,
	int ichMin, int ichLim, byte * prglbsOut, int * pichBreak)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgchIn,ichLim);
	ChkComArrayArg(prglbsOut, ichLim - ichMin);
	ChkComOutPtr(pichBreak);
	if (cchIn < ichLim)
		ThrowInternalError(E_INVALIDARG, "Range beyond end of string");
	if (ichLim < ichMin)
		ThrowInternalError(E_INVALIDARG, "Range limits out of order");
	*pichBreak = -1;	// This will be set to something else if we come to a "break/stop".
	if (ichLim == ichMin)
		return S_FALSE;

	// Intermediate array for line break properties of interest. Make it of size ichLim + 1
	// so that we can examine properties before the output range and (as long as cchIn > ichLim)
	// one position past the end of the range to obtain the status of the last element.
	byte rglbp[1000];
	Vector<byte> vlbp;
	byte * prglbpBuf = rglbp;	// Pointer to start of intermediate buffer.
	if (ichLim > 999)
	{
		vlbp.Resize(ichLim + 1);
		prglbpBuf = vlbp.Begin();
	}
	byte * prglbp = prglbpBuf;	// Initial value of pointer to intermediate buffer.

	// Count of characters in whose properties we are interested.
	int cb = ichLim;
	if (cchIn > ichLim)
		++cb;

	LOCK(m_mutex)
	{
		// Fill intermediate array of line break properties.
		int ich;
		int ch;
		CharacterPropertyObject * cpoOverride;
		// Trap this character and trigger a stop of the process when reached.
		const int kchTAB = 0x0009;
		for (ich = 0; ich < cb; ++ich)
		{
			ch = *prgchIn++;
			cpoOverride = GetOverrideChar((UChar32)ch);
			if (cpoOverride)
				*prglbp = (byte)cpoOverride->lbpLineBreak;
			else
			{
				int lpbVal = u_getIntPropertyValue(ch, UCHAR_LINE_BREAK);
				if (lpbVal > klbpZW) // klbpZW is max enum < icu 2.6
				{
					// TODO-Linux FWNX-207: can't handle icu ULineBreak >= 2.6.
					lpbVal = klbpXX; // unknown line break
				}
				*prglbp = (byte)g_rglbp[lpbVal];
			}
			if (GenCategory(ch) == kccZs)
				*prglbp |= 0x80;
			if (ch == kchTAB)
				*prglbp |= 0x40;	// This bit causes process to be stopped if found.
			++prglbp;
		}
	}
	byte * plbsOut;                   // pointer to current position in output array
	const byte * plbpLast = prglbpBuf + cb - 1; // pointer to last relevant element of lbp array
	int lbpCurrent;                // value of lpb currently considered as first of pair
	int lbpNext;                   // value of lbp currently considered as second of pair
	const byte klbsBS = kflbsBrk | kflbsSpace; // status is space with break allowed after
	const byte klbsN = 0;          // status is non-space with no break allowed after

	int i;
	int cbOut = ichLim - ichMin; // count of bytes to output
	for (i = 0; i < cbOut; ++i)
		*(prglbsOut + i) = kflbsBrkL;	// Initialise output array to kflbsBrkL; has the
										// effect of allowing only letter breaks as default.
	prglbp = prglbpBuf + ichMin;
	plbsOut = prglbsOut;

	// If there are no characters beyond ichLim, assume break is allowed after last character.
	// -- No, that is not a good default.
	if (cchIn == ichLim)
	{
//		*(plbsOut + cbOut - 1) = (*plbpLast & 0x80) ? klbsBS : (byte)kflbsBrk;
		if (*plbpLast & 0x80)
			*(plbsOut + cbOut - 1) = klbsBS;
		if (cbOut == 1)
			return S_OK; // If only one character, we have finished.
	}

	// Now search backwards to see if there is a line break property which is not SP or CM.
	// If there is, make this the value of lbpCurrent.
	i = ichMin;
	while (i >= 0)
	{
		lbpCurrent = *prglbp & 0x3f;	// Clear high bits if set.
		if (lbpCurrent != klbpSP && lbpCurrent != klbpCM)
			i = -2;
		else
		{
			--i;
			--prglbp;
		}
	}
	if (i != -2)
		// We didn't find a property at or before ichMin which was not SP or CM.
		lbpCurrent = klbpAL;  // Make as if a leading SP or CM follows an alphabetic character.

	byte lbs;
	// Last character has already been handled if necessary.
	for (prglbp = prglbpBuf + ichMin; prglbp < plbpLast; ++prglbp)
	{
		if (*pichBreak >= 0)
			break; // Exit from the loop if we found a reason to stop during the last iteration.
		if (*prglbp & 0x40)
		{
			// If current character is a TAB, set *pichBreak. This will stop the loop next time.
			// Note that we want the TAB to be processed "normally" for its line breaking
			// properties, so we continue through this iteration.
			*pichBreak = prglbp - prglbpBuf;
		}
		lbpNext = *(prglbp + 1) & 0x3f;	// Clear high bits if set.
		if (lbpCurrent == klbpBK || lbpCurrent == klbpLF)
		{
			// Mark hard breaks as break allowed and stop looking for more properties.
			*plbsOut++ = kflbsBrk;
			*pichBreak = prglbp - prglbpBuf;
			break;
		}
		if (lbpCurrent == klbpCR)
		{
			// Break allowed (actually mandatory) unless NL follows
			*plbsOut++ = (lbpNext == klbpLF) ? klbsN : (byte)kflbsBrk;
			if (lbpNext == klbpLF)
			{
				*pichBreak = prglbp - prglbpBuf;
				break;
			}
			// If next line begins with CM or SP we make as if they follow an AL.
			lbpCurrent = (lbpNext == klbpSP || lbpNext == klbpCM) ? klbpAL : lbpNext;
			continue;
		}
		if (lbpNext == klbpSP)
		{
			// prevent break before space; do not reset lbpCurrent
			*plbsOut++ = ((*prglbp & 0x7f) == klbpSP) ? (byte)kflbsSpace : klbsN;
			continue;
		}
		if (lbpNext == klbpCM)
		{
			// prevent letter break before CM; do not reset lbpCurrent
			*plbsOut++ = klbsN;	// (side effect of space before CM not marked as space)
			continue;
		}

		Assert(lbpCurrent >= 0 && lbpCurrent <= klbpZW);
		Assert(lbpNext >= 0 && lbpNext <= klbpZW);
		lbs = s_rglbs[lbpCurrent][lbpNext]; // look up break status in table
		Assert(lbs > 0 && lbs < 5);  // values other than 1 to 4 indicate an internal error

		if (lbs == 2)
			*plbsOut = kflbsBrk;
		if (lbs == 3 && (*prglbp & 0x3f) == klbpSP)
			*plbsOut = kflbsBrk;
		if (lbs == 4)
			*plbsOut = klbsN;
		if (*prglbp & 0x80)
			*plbsOut |= kflbsSpace;
		// Note that for some reason low surrogates (as well as high surrogates) are listed
		// with SG line breaking property in Unicode 3.0. This means that letter break is
		// prevented after a surrogate pair, not just after the high part of the pair. The
		// character following a surrogate pair could presumably be a space.
		// If none of these conditions is met, *pOut is left as intialised (kflbsBrkL)
		// which implies "not a space" and "letter break allowed after".

		lbpCurrent = lbpNext;
		++plbsOut;
	}
	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

///*----------------------------------------------------------------------------------------------
//	Perform compatibility normalization of the input string, that is, every character which
//	has a compatibility decomposition is decomposed (recursively). This is Normalization Form
//	KD (NFKD) as defined by Unicode TR 15.
//----------------------------------------------------------------------------------------------*/
//STDMETHODIMP LgIcuCharPropEngine::NormalizeKd(BSTR bstr, BSTR * pbstr)
//{
//	BEGIN_COM_METHOD;
//	ChkComBstrArgN(bstr);
//	ChkComOutPtr(pbstr);
//
//	Normalize(UNORM_NFKD, bstr, pbstr);
//
//	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
//}

/*----------------------------------------------------------------------------------------------
	Perform normalized decomposition of the input string, that is, every character which has
	a decomposition is decomposed (recursively). This is Normalization Form D (NFD) as defined
	by Unicode TR 15.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::NormalizeD(BSTR bstr, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);
	ChkComOutPtr(pbstr);

	Normalize(UNORM_NFD, bstr, pbstr);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Perform the specified type of normalized decomposition of the input string, that is, every
	character which has a decomposition is decomposed (recursively).
----------------------------------------------------------------------------------------------*/
void LgIcuCharPropEngine::Normalize(UNormalizationMode mode, BSTR bstr, BSTR * pbstr)
{
	// Treat NULL pointer as empty string.
	if (!bstr)
		return;

	if (!IsPlausibleUnicodeRgch(bstr, BstrLen(bstr)))
		ThrowNice(E_FAIL, kstidInvalidUnicode);

	int cchRet;

	// find length of converted string
	NormalizeRgch(mode, bstr, BstrLen(bstr), NULL, 0, &cchRet);

	SmartBstr sbstr;

	// Note: we can't assign it direct to the smart BSTR, because passing a NULL like
	// this does not fill in the final null, and trying to get the Chars() of a smart
	// BSTR in debug mode calls AssertValid, which fails if the null is not present.
	{ // Block, to limit scope of bstrT
		BSTR bstrT = NULL; // The NULL is needed for release builds.
		AllocBstr(&bstrT, cchRet);
		bstrT[cchRet] = 0; // provide null (before making smart, to satisfy AssertValid)
		sbstr.Attach(bstrT); // ensure freed even if exception occurs.
	}

	NormalizeRgch(mode, bstr, BstrLen(bstr), (OLECHAR *)sbstr.Chars(), cchRet, &cchRet);
	// Make sure the string length is as promised.
	Assert(cchRet == BstrLen(sbstr));
	*pbstr = sbstr.Detach();
}

/*----------------------------------------------------------------------------------------------
	Perform compatibility normalization of the input string, that is, every character which
	has a compatibility decomposition is decomposed (recursively). This is Normalization Form
	KD (NFKD) as defined by Unicode TR 15.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::NormalizeKdRgch(OLECHAR * prgchIn, int cchIn,
	OLECHAR * prgchOut, int cchMaxOut, int * pcchOut)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchIn, cchIn);
	ChkComArrayArg(prgchOut, cchMaxOut);
	ChkComOutPtr(pcchOut);

	NormalizeRgch(UNORM_NFKD, prgchIn, cchIn, prgchOut, cchMaxOut, pcchOut);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Perform normalization of the input string, that is, every character which has a
	decomposition is decomposed (recursively). This is Normalization Form D (NFD) as defined
	by Unicode TR 15.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::NormalizeDRgch(OLECHAR * prgchIn, int cchIn,
	OLECHAR * prgchOut, int cchMaxOut, int * pcchOut)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchIn, cchIn);
	ChkComArrayArg(prgchOut, cchMaxOut);
	ChkComOutPtr(pcchOut);

	NormalizeRgch(UNORM_NFD, prgchIn, cchIn, prgchOut, cchMaxOut, pcchOut);

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Perform the specified type of normalization of the input string, that is, every character
	which has a decomposition is decomposed (recursively).
----------------------------------------------------------------------------------------------*/
void LgIcuCharPropEngine::NormalizeRgch(UNormalizationMode mode, OLECHAR * prgchIn, int cchIn,
	OLECHAR * prgchOut, int cchMaxOut, int * pcchOut)
{
	UnicodeString ustIn;
	for (int cch = 0; cch < cchIn; cch++)
		ustIn += prgchIn[cch];
	UnicodeString ustOut;

	UChar uch = 18;	//these two lines put a cap that ICU code recognizes on the
	ustIn += uch;	//string we're dealing with

	Normalizer norm(ustIn, mode);
	UErrorCode uerr = U_ZERO_ERROR;
	int ch = 0;

	norm.normalize(ustIn, mode, 0, ustOut, uerr);

	if (!U_SUCCESS(uerr))
		ThrowNice(E_FAIL, kstidICUNormalize);

	if (cchMaxOut > 0) //only if the user actually wants an answer
	{
		for (; ustOut[ch] != 18; ch++) //checking for the cap we added earlier
		{
			if (cchMaxOut > ch)
				prgchOut[ch] = ustOut[ch];
			else
				ThrowNice(E_FAIL, kstidBufferTooSmall);
		}
	}
	else
	{
		while(ustOut[ch] != 18)
			ch++;
	}

	*pcchOut = ch;
}

/*----------------------------------------------------------------------------------------------
	Strip diacritics. Specifically, removes all characters that have the property Lm.or Mn
	Note that this will not comvert a single code point that includes a diacritic to
	its unmodified equivalent. It is usually desireable to first perform normalization
	(form D or KD) before stripping diacritics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::StripDiacritics(BSTR bstr, BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);
	ChkComOutPtr(pbstr);

	// Treat NULL pointer as empty string.
	if (!bstr)
		return S_OK;

	if (!IsPlausibleUnicodeRgch(bstr, BstrLen(bstr)))
		ThrowNice(E_FAIL, kstidInvalidUnicode);

	int cchRet;

	// find length of converted string
	StripDiacriticsRgch(bstr, BstrLen(bstr), NULL, 0, &cchRet);

	if (cchRet == 0)
		return S_OK; // nothing left but diacritics

	SmartBstr sbstr;
	// Note: we can't assign it direct to the smart BSTR, because passing a NULL like
	// this does not fill in the final null, and trying to get the Chars() of a smart
	// BSTR in debug mode calls AssertValid, which fails if the null is not present.
	{ // Block, to limit scope of bstrT
		BSTR bstrT = NULL; // The NULL is needed for release build.
		AllocBstr(&bstrT, cchRet);
		bstrT[cchRet] = 0; // provide null (before making smart, to satisfy AssertValid)
		sbstr.Attach(bstrT); // ensure freed even if exception occurs.
	}

	StripDiacriticsRgch(bstr, BstrLen(bstr), (OLECHAR *)sbstr.Chars(), cchRet, &cchRet);
	// Make sure the string length is as promised.
	Assert(cchRet == BstrLen(sbstr));
	*pbstr = sbstr.Detach();
	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Strip diacritics. Specifically, removes all characters that have the property Lm.or Mn
	Note that this will not comvert a single code point that includes a diacritic to
	its unmodified equivalent. It is usually desireable to first perform normalization
	(form D or KD) before stripping diacritics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::StripDiacriticsRgch(OLECHAR * prgchIn, int cchIn,
	OLECHAR * prgchOut, int cchMaxOut, int * pcchOut)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchIn, cchIn);
	ChkComArrayArg(prgchOut, cchMaxOut);
	ChkComOutPtr(pcchOut);
	int cchOut = 0;
	OLECHAR *pchOut = prgchOut;
	for (OLECHAR *pch = prgchIn; pch < prgchIn + cchIn; pch++)
	{
		int cc = GenCategory(*pch);
		if (cc != kccLm && cc != kccMn)
		{
			cchOut++;
			if (cchMaxOut)
			{
				if (cchOut > cchMaxOut)
					ThrowNice(E_FAIL, kstidBufferTooSmall);
				*pchOut++ = *pch;
			}
		}
		// Otherwise just skip it.
	}
	*pcchOut = cchOut;
	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Sets the current locale--usually this is done at the time of initialization and then not
	touched for the remainder of the instance's existence.  The locale is needed for
	case changes and line breaking.

	NOTE: Both the put_Locale and the get_Locale are known to change LCIDs slightly on about 10%
	of all LCIDs passed into them.  It shouldn't be a problem, but we don't know that for sure.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::put_Locale(int nLocale)
{
	BEGIN_COM_METHOD;

#if WIN32
	DWORD dwLCID = MAKELCID(nLocale, SORT_DEFAULT);
#else
	DWORD dwLCID = nLocale; // SORT_DEFAULT = 0
#endif

	LOCK(m_mutex)
	{
#if WIN32
		char rgch1[8] = {0, 0, 0, 0, 0, 0, 0, 0};
		char rgch2[8] = {0, 0, 0, 0, 0, 0, 0, 0};

		int iTmp;
		iTmp = ::GetLocaleInfoA(dwLCID, LOCALE_SISO639LANGNAME, rgch1, 7);
		iTmp = ::GetLocaleInfoA(dwLCID, LOCALE_SISO3166CTRYNAME, rgch2, 7);

		if (m_pLocale)			// Because the ICU functions provide no way of reassigning the
			delete m_pLocale;	// locale, we have to blow it away and start over.
		m_pLocale = new Locale(rgch1, rgch2);
#else
		std::string language = LocaleIndex::Instance().GetLanguage(dwLCID);
		std::string country = LocaleIndex::Instance().GetCountry(dwLCID);

		if (m_pLocale)			// Because the ICU functions provide no way of reassigning the
			delete m_pLocale;	// locale, we have to blow it away and start over.
		m_pLocale = new Locale(language.c_str(), country.c_str());
#endif //WIN32

		CleanupBreakIterator();
		SetupBreakIterator();
	}
	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Gets the current locale.  This function is included for completion.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::get_Locale(int * pnLocale)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnLocale);

	LOCK(m_mutex)
	{
		Assert(m_pLocale);
		if (!m_pLocale)
			return E_UNEXPECTED;

		*pnLocale = m_pLocale->getLCID();
	}

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Sets the text for the LineBreakBefore and the LineBreakAfter functions to use.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::put_LineBreakText(OLECHAR * prgchIn, int cch)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prgchIn);
	ChkComArrayArg(prgchIn, cch);
	LOCK(m_mutex)
	{
		Assert(m_pLocale);
		if (!m_pLocale)
			return E_UNEXPECTED;
		// Check that first the character is not a low surrogate.
	//	Assert(!IsLowSurrogate(*prgchIn));
		SetupBreakIterator(); //make sure we have one.

		m_cchBrkMax = cch;
		m_pBrkit->setText(m_usBrkIt.setTo(prgchIn, cch));
	}

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Gets the text that the LineBreakBefore and LineBreakAfter functions are using.  This
	function is included for completion.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::GetLineBreakText(int cchMax, OLECHAR * prgchOut,
	int * pcchOut)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchOut, cchMax);
	ChkComOutPtr(pcchOut);
	LOCK(m_mutex)
	{
		Assert(m_pLocale);
		if (!m_pLocale)
			return E_UNEXPECTED;
		SetupBreakIterator(); //make sure we have one.

		UnicodeString ustrAns;

		const CharacterIterator & chIter = m_pBrkit->getText();
		const_cast<CharacterIterator &>(chIter).getText(ustrAns);

		if ((cchMax < ustrAns.length()) && (cchMax > 0))
			ThrowNice(E_FAIL, kstidBufferTooSmall);

		*pcchOut = ustrAns.length();

		if (cchMax > 0)
		{
			for (int ch = 0; ch < ustrAns.length(); ch++)
				prgchOut[ch] = ustrAns[ch];
		}
	}

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Finds the nearest line break immediately before the given index (ichIn).  The function
	returns not only a location but the weight of the line break (currently not implemented).
	See http://www.unicode.org/unicode/reports/tr14/ for more information on line breaking
	properties.  The third parameter is not currently used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::LineBreakBefore(int ichIn, int * pichOut,
	LgLineBreak * plbWeight)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichOut);
	ChkComArgPtr(plbWeight);
	LOCK(m_mutex)
	{
		Assert(m_pLocale);
		if (!m_pLocale)
			return E_UNEXPECTED;
		SetupBreakIterator(); //make sure we have one.

		if ((ichIn < 0) || (ichIn >= m_cchBrkMax))
			ThrowNice(E_FAIL, kstidICUBrkRange);

		*pichOut = m_pBrkit->preceding(ichIn);
	}

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

/*----------------------------------------------------------------------------------------------
	Finds the nearest line break immediately after the given index (ichIn).  The function
	returns not only a location but the weight of the line break (currently not implemented).
	See http://www.unicode.org/unicode/reports/tr14/ for more information on line breaking
	properties.  The third parameter is not currently used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::LineBreakAfter(int ichIn, int * pichOut,
	LgLineBreak * plbWeight)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pichOut);
	ChkComArgPtr(plbWeight);
	LOCK(m_mutex)
	{
		Assert(m_pLocale);
		if (!m_pLocale)
			return E_UNEXPECTED;
		SetupBreakIterator(); //make sure we have one.

		if ((ichIn < 0) || (ichIn >= m_cchBrkMax))
			ThrowNice(E_FAIL, kstidICUBrkRange);

		*pichOut = m_pBrkit->following(ichIn);
	}

	END_COM_METHOD(g_fact, IID_ILgCharacterPropertyEngine);
}

//:>********************************************************************************************
//:>	ILgIcuCharPropEngine Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize an instance with the proper Locale values.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgIcuCharPropEngine::Initialize(BSTR bstrLanguage, BSTR bstrScript, BSTR bstrCountry, BSTR bstrVariant)
{
	BEGIN_COM_METHOD
	StrAnsi staLanguage(bstrLanguage);
	StrAnsi staCountry(bstrCountry);
	StrAnsi staVariant(bstrVariant);
	Assert(staLanguage.Length());

	LOCK(m_mutex)
	{
		m_pLocale = new Locale(staLanguage.Chars(), staCountry.Chars(), staVariant.Chars());

		SetupBreakIterator();
	}

	END_COM_METHOD(g_fact, IID_ILgIcuCharPropEngine);
}

// Enhance JohnT: possibly handle surrogates? DavidO says dialog does not allow them to be added currently.
STDMETHODIMP LgIcuCharPropEngine::InitCharOverrides(BSTR bstrWsCharsList)
{
	BEGIN_COM_METHOD
	// Todo JohnT: implement; wsCharsList is delimted by \xfffc; between each delimiter if there
	// is exactly one character and it is not already known to be wordforming add it to m_siWordformingOverrides.

	StrUni wsCharsList(bstrWsCharsList);
	OLECHAR chPrev = L'\xfffc'; // treat first char as following delimiter.
	OLECHAR chBefore = L'a'; // already wordforming, not interesting.
	LOCK(m_mutex)
	{
		for (const OLECHAR * pch = wsCharsList.Chars(); pch < wsCharsList.Chars() + wsCharsList.Length(); pch++)
		{
			ConsiderAdd(chBefore, chPrev, *pch);
			chBefore = chPrev;
			chPrev = *pch;
		}
		ConsiderAdd(chBefore, chPrev, L'\xfffc'); // treat end as another delimiter.
	}

	END_COM_METHOD(g_fact, IID_ILgIcuCharPropEngine);
}

//:>********************************************************************************************
//:>	Utility methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Determine whether specified character is in the range of a valid Unicode character.
	(There are many codepoints within this range that are not yet assigned.)
	Note that this implementation will accept surrogates, though they are not really valid
	as stand-along Unicode characters.
----------------------------------------------------------------------------------------------*/
bool LgIcuCharPropEngine::IsPlausibleUnicodeCh(int ch)
{
	return (ch >= 0 && ch <= 0x10ffff);
}

/*----------------------------------------------------------------------------------------------
	Determine whether specified string is composed of characters within the range
	of valid Unicode characters.
	Treat well-formed surrogate pairs as valid.
	ENHANCE JohnT: Currently, this can only fail on malformed surrogate pairs, since no
	single OLECHAR can be < 0 or > 0xffff.  But we could get more intelligent about what
	are really valid Unicode chars.
----------------------------------------------------------------------------------------------*/
bool LgIcuCharPropEngine::IsPlausibleUnicodeRgch(OLECHAR * prgch, int cch)
{
	AssertPtr(prgch);
	Assert(cch >= 0);

	for (; cch > 0; cch--, prgch++)
	{
		if (prgch[0] >= 0xd800 && prgch[0] < 0xdc00)
		{
			if  (cch > 1 && prgch[1] >= 0xdc00 && prgch[1] < 0xe000)
			{
				// A surrogate pair.
				prgch++;
				cch--;
				continue;
			}
			else
				return false;
		}
		else if (prgch[0] >= 0xdc00 && prgch[0] < 0xe000) // Low surrogate with no high before.
		{
			return false;
		}
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the general category info for the specified character.
----------------------------------------------------------------------------------------------*/
LgGeneralCharCategory LgIcuCharPropEngine::GenCategory(int ch)
{
	Assert(IsPlausibleUnicodeCh(ch));

	int nAns = u_charType(ch);
	return ConvertCharCategory(nAns);
}

/*----------------------------------------------------------------------------------------------
	Do the work for interface methods ToLower, ToUpper, and ToTitle().
	Therefore this method checks its arguments as if it were an interface method.
	Use ConvertCaseRgch() to do for a bstr what it does for a rgch.
----------------------------------------------------------------------------------------------*/
void LgIcuCharPropEngine::ConvertCase(BSTR bstr, BSTR * pbstr, LgGeneralCharCategory ccTo)
{
	ChkComBstrArgN(bstr);
	ChkComArgPtr(pbstr);

	*pbstr = NULL;
	// Treat NULL pointer as empty string.
	if (!bstr)
		return;

	if (!IsPlausibleUnicodeRgch(bstr, BstrLen(bstr)))
		ThrowNice(E_FAIL, kstidInvalidUnicode);

	int cchRet;

	// find length of converted string
	ConvertCaseRgch(bstr, BstrLen(bstr), NULL, 0, &cchRet, ccTo);

	Vector<OLECHAR> vch;
	vch.Resize(cchRet);

	ConvertCaseRgch(bstr, BstrLen(bstr), vch.Begin(), cchRet, &cchRet, ccTo);
	Assert(vch.Size() == cchRet);

	SmartBstr sbstr(vch.Begin(), cchRet);

	*pbstr = sbstr.Detach();
}

/*----------------------------------------------------------------------------------------------
	Convert a string to given case.
	If cchOut is zero, do not return a string but just calculate how long it would be.
	If cchOut is positive but not big enough, throw E_FAIL.
	Do arg checking for interface methods To{Lower,Upper,Title}Rgch().
----------------------------------------------------------------------------------------------*/
void LgIcuCharPropEngine::ConvertCaseRgch(OLECHAR * prgchIn, int cchIn,
	OLECHAR * prgchOut, int cchOut, int * pcchRet, LgGeneralCharCategory ccTo)
{
	ChkComArrayArg(prgchIn, cchIn);
	ChkComArrayArg(prgchOut, cchOut);
	ChkComOutPtr(pcchRet);
	Assert(ccTo == kccLl || ccTo == kccLu || ccTo == kccLt);
	Assert(sizeof(OLECHAR) == sizeof(UChar));
	Assert(sizeof(OLECHAR) == sizeof(wchar));
	if (!IsPlausibleUnicodeRgch(prgchIn, cchIn))
		ThrowNice(E_FAIL, kstidInvalidUnicode);

	StrUni stuOut;
	int cchNeed = 0;
	LOCK(m_mutex)
	{
		Assert(m_pLocale);
		if (!m_pLocale)
			ThrowHr(E_UNEXPECTED);

		UChar rgchT[8];
		int ich;
		UChar32 uch32;
		UChar uch1;
		UChar uch2;
		bool fSurrogate;
		CharacterPropertyObject * pcpoOverride;
		UErrorCode uErr = U_ZERO_ERROR;

		for (ich = 0; ich < cchIn; ++ich)
		{
			if (ich + 1 < cchIn)
				fSurrogate = FromSurrogate(prgchIn[ich], prgchIn[ich+1], (uint*)&uch32);
			else
				fSurrogate = false;

			if (fSurrogate)
				pcpoOverride = GetOverrideChar(uch32);
			else
				pcpoOverride = GetOverrideChar(prgchIn[ich]);

			if (cchOut)
			{
				switch (ccTo)
				{
				case kccLl:
					if (pcpoOverride)
					{
						if (ToSurrogate(pcpoOverride->uch32Lowercase, (wchar*)&uch1, (wchar*)&uch2))
						{
							stuOut.Append(&uch1, 1);
							stuOut.Append(&uch2, 1);
						}
						else
						{
							stuOut.Append(&uch1, 1);
						}
					}
					else
					{
						int cch = u_strToLower(rgchT, 8, prgchIn + ich, fSurrogate ? 2 : 1,
							m_pLocale->getName(), &uErr);
						stuOut.Append(rgchT, cch);
					}
					break;
				case kccLu:
					if (pcpoOverride)
					{
						if (ToSurrogate(pcpoOverride->uch32Uppercase, (wchar*)&uch1, (wchar*)&uch2))
						{
							stuOut.Append(&uch1, 1);
							stuOut.Append(&uch2, 1);
						}
						else
						{
							stuOut.Append(&uch1, 1);
						}
					}
					else
					{
						int cch = u_strToUpper(rgchT, 8, prgchIn + ich, fSurrogate ? 2 : 1,
							m_pLocale->getName(), &uErr);
						stuOut.Append(rgchT, cch);
					}
					break;
				case kccLt:
					if (pcpoOverride)
					{
						if (ToSurrogate(pcpoOverride->uch32Titlecase, (wchar*)&uch1, (wchar*)&uch2))
						{
							stuOut.Append(&uch1, 1);
							stuOut.Append(&uch2, 1);
						}
						else
						{
							stuOut.Append(&uch1, 1);
						}
					}
					else
					{
						int cch = u_strToTitle(rgchT, 8, prgchIn + ich, fSurrogate ? 2 : 1,
							NULL, m_pLocale->getName(), &uErr);
						stuOut.Append(rgchT, cch);
					}
					break;
				default:
					Assert(ccTo == kccLl || ccTo == kccLu || ccTo == kccLt);
					break;
				}
			}
			else
			{
				uErr = U_ZERO_ERROR;		// Ignore buffer overflow messages.
				switch (ccTo)
				{
				case kccLl:
					if (pcpoOverride)
					{
						if (ToSurrogate(pcpoOverride->uch32Lowercase, (wchar*)&uch1, (wchar*)&uch2))
							cchNeed += 2;
						else
							++cchNeed;
					}
					else
					{
						cchNeed += u_strToLower(NULL, 0, prgchIn + ich, fSurrogate ? 2 : 1,
							m_pLocale->getName(), &uErr);
					}
					break;
				case kccLu:
					if (pcpoOverride)
					{
						if (ToSurrogate(pcpoOverride->uch32Uppercase, (wchar*)&uch1, (wchar*)&uch2))
							cchNeed += 2;
						else
							++cchNeed;
					}
					else
					{
						cchNeed += u_strToUpper(NULL, 0, prgchIn + ich, fSurrogate ? 2 : 1,
							m_pLocale->getName(), &uErr);
					}
					break;
				case kccLt:
					if (pcpoOverride)
					{
						if (ToSurrogate(pcpoOverride->uch32Titlecase, (wchar*)&uch1, (wchar*)&uch2))
							cchNeed += 2;
						else
							++cchNeed;
					}
					else
					{
						cchNeed += u_strToTitle(NULL, 0, prgchIn + ich, fSurrogate ? 2 : 1,
							NULL, m_pLocale->getName(), &uErr);
					}
					break;
				default:
					Assert(ccTo == kccLl || ccTo == kccLu || ccTo == kccLt);
					break;
				}
			}

			if (fSurrogate)
				++ich;
		}
	}

	if (cchOut)
	{
		if (cchOut < stuOut.Length())
			ThrowNice(E_FAIL, kstidBufferTooSmall);		// if the buffer was too small
		memcpy(prgchOut, stuOut.Chars(), stuOut.Length() * sizeof(OLECHAR));
		*pcchRet = stuOut.Length();
	}
	else
	{
		*pcchRet = cchNeed;
	}
}

/*----------------------------------------------------------------------------------------------
	Just a simple little utility to convert the integer ICU returns in its getType function to
	an LgGeneralCharCategory that other functions can actually use.
----------------------------------------------------------------------------------------------*/
LgGeneralCharCategory LgIcuCharPropEngine::ConvertCharCategory(int nICUCat)
{
	return g_rggcc[nICUCat];
}

/*----------------------------------------------------------------------------------------------
	Another simple utility, this time converting the Bidi values given by the ICU function into
	something that FieldWorks understands.
----------------------------------------------------------------------------------------------*/
LgBidiCategory LgIcuCharPropEngine::ConvertBidiCategory(int nICUCat)
{
	return g_rgbic[nICUCat];
}

/*----------------------------------------------------------------------------------------------
	This function will take the pointer to the OLECHAR array (prgchIn) and length (cchLength)
	passed into it and convert it to the UChar array passed in (prgchOut).
----------------------------------------------------------------------------------------------*/
void LgIcuCharPropEngine::OLECHARToUChar(OLECHAR *prgchIn, UChar *prgchOut, int cchLength)
{
	Assert(sizeof(UChar) == sizeof(OLECHAR));		// So why this function?

	ChkComArrayArg(prgchIn, cchLength);
	ChkComArrayArg(prgchOut, cchLength+1);
	for (int count = 0; count < cchLength; count++)
	{
		*prgchOut = *prgchIn;
		prgchOut++;
		prgchIn++;
	}
	*prgchOut = 0;  //capping off the string with a null
}

/*----------------------------------------------------------------------------------------------
	This function will take the pointer to the UChar array (prgchIn) and length (cchLength)
	passed into it and convert it to the OLECHAR array passed in (prgchOut).
----------------------------------------------------------------------------------------------*/
void LgIcuCharPropEngine::UCharToOLECHAR(UChar *prgchIn, OLECHAR *prgchOut, int cchLength)
{
	Assert(sizeof(UChar) == sizeof(OLECHAR));		// So why this function?

	ChkComArrayArg(prgchIn, cchLength);
	ChkComArrayArg(prgchOut, cchLength);
	for (int count = 0; count < cchLength; count++)
	{
		*prgchOut = *prgchIn;
		prgchIn++;
		prgchOut++;
	}
}

/*----------------------------------------------------------------------------------------------
	This function merely sets m_pocpData to point to the same item as the input value pocpData.
----------------------------------------------------------------------------------------------*/
void LgIcuCharPropEngine::SetCharOverrideTables(OverriddenCharProps * pocpData)
{
	LOCK(m_mutex)
	{
		m_pocpData = pocpData;
	}
}

/*----------------------------------------------------------------------------------------------
	This function checks the character override tables to see if a certain character has been
	overridden.  It will return a pointer to the character property object if the character has
	been overridden; otherwise, it will return 0.
----------------------------------------------------------------------------------------------*/
CharacterPropertyObject * LgIcuCharPropEngine::GetOverrideChar(UChar32 chIn)
{
	if (!m_pocpData)
		return 0;

	// If the character is not within the outer minimum and maximum...
	if ((chIn >= m_pocpData->iLim) || (chIn < m_pocpData->iMin))
		return 0;

	// TODO: make this more efficient if there are many ranges.
	CharPropRange * cprIter;
	int chIndex;

	// Loop through the vRanges.
	for (cprIter = m_pocpData->pvcprOverride1->Begin();
		cprIter != m_pocpData->pvcprOverride1->End(); cprIter++)
	{
		// If the input value is between the minimum and maximum for that vRange...
		if ((chIn < cprIter->iLim) && (chIn >= cprIter->iMin))
			break;
	}

	// If the search came up empty (iterator pointing at pvcprOverride1->End())...
	if (cprIter == m_pocpData->pvcprOverride1->End())
		return 0;
	else
	{
		// Get index.
		chIndex = cprIter->vRange[chIn - cprIter->iMin];

		//Return 0 if chIndex is 0; else return pointer to the index in pvcpoOverride2.
		return (chIndex) ? &((*(m_pocpData->pvcpoOverride2))[chIndex-1]) : 0;
	}
}

/*----------------------------------------------------------------------------------------------
	Takes an array of wchars and converts it into an array of UChars.
----------------------------------------------------------------------------------------------*/
void LgIcuCharPropEngine::WCharToUChar(const wchar *wchIn, UChar *uchOut, int cchIn)
{
	ChkComArrayArg(wchIn, cchIn);
	ChkComArrayArg(uchOut, cchIn+1);
	int count;
	for (count = 0; count < cchIn; count++)
		uchOut[count] = wchIn[count];

	uchOut[count] = 0; //capping off the string with a null
}


/*----------------------------------------------------------------------------------------------
	Cleanup the callback object and the associated break iterator, if any.
----------------------------------------------------------------------------------------------*/
void LgIcuCharPropEngine::CleanupBreakIterator()
{
	Assert(m_pBrkit != NULL);
	delete m_pBrkit;
	m_pBrkit = NULL;
}

void LgIcuCharPropEngine::ConsiderAdd(OLECHAR chFirst, OLECHAR chSecond, OLECHAR chThird)
{
	if (chFirst == 0xfffc && chThird == 0xfffc && chSecond != 0xfffc)
	{
		// It's wordforming. Did we already know that?
		ComBool wf;
		CheckHr(get_IsWordForming(chSecond, &wf));

		if(!wf)
		{
			int newChar = (int)chSecond;
			m_siWordformingOverrides.Insert(newChar);
		}
	}
}
