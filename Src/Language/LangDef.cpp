/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2004 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LangDef.cpp
Responsibility: Steve McConnel
Last reviewed:

	This file implements classes for reading a language definition XML file.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#include "xmlparse.h"
#include "../Cellar/FwXml.h"
#include "../Cellar/FwCellarRes.h"
#include "FwStyledText.h"
#include <io.h>

#undef THIS_FILE
DEFINE_THIS_FILE


//:>********************************************************************************************
//:>	LanguageDefinitionFactory methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Distinguish among the various fundamental kinds of XML elements.
	Hungarian: elty
----------------------------------------------------------------------------------------------*/
typedef enum
{
	keltyLangDef = 1,	// LanguageDefinition: outermost element of the XML file.
	keltyObject,		// WritingSystem or Collation.  Results in separate COM objects.
	keltyPropName,		// subelement of WritingSystem or Collation.
	keltyBasicProp,		// value for subelement of WritingSystem or Collation.
	keltyPropValue,		// basic untyped string data for other language definition information.
	keltyBad = 0
} ElementType;

/*----------------------------------------------------------------------------------------------
	Store the element type and other basic information for a particular XML tag.
	A stack of these is maintained during parsing, one for each open XML element.
	Hungarian: eti
----------------------------------------------------------------------------------------------*/
struct ElemTypeInfo
{
	const char * m_pszName;
	ElementType m_elty;
	int m_nExact;		// more exact type / field specifier.
	bool m_fSeq;		// for keltyPropName, true if this is a sequence.
};
template Vector<ElemTypeInfo>;

/*----------------------------------------------------------------------------------------------
	This contains the information needed to handle missing ord attributes for sequence
	properties.  A stack of these is maintained, one for each open property.
	Hungarian: spi
----------------------------------------------------------------------------------------------*/
struct SeqPropInfo
{
	bool m_fSeq;		// Flag whether this is a sequence attribute.
	int m_cobj;			// Number of objects loaded for this sequence.
};
template Vector<SeqPropInfo>;

/*----------------------------------------------------------------------------------------------
	Data structure for information processed during XML parsing.  Much of this is identical to
	that used by FwXmlData::LoadXml().

	Hungarian: xid.
----------------------------------------------------------------------------------------------*/
class FwXmlImportData
{
public:
	FwXmlImportData();
	~FwXmlImportData();

	// The following data elements and methods are common to that used by FwXmlData::LoadXml(),
	// and used by the string / character data handlers defined in the FwXml namespace.
	XML_Parser m_parser;	// The open Expat XML parser.
	bool m_fError;			// Flag that an error has occurred: this will terminate the parse.
	HRESULT m_hr;			// COM return code: S_OK or specific error code.
	bool m_fIcuLocale;	// Flag to store <Uni> data in pass 1: <LgWritingSystem><ICULocale24>.
	StrUni m_stuChars;		// Character data for <Uni>, <AUni>, <Str>, or <AStr>.
	bool m_fInRun;			// Flag that we're parsing a <Run> element.
	bool m_fRunHasChars;	// Flag that this <Run> element contains text data.
	FwXml::RunDataType m_rdt;					// Type of this run (Characters / Picture)
	FwXml::BasicRunInfo m_bri;					// Offsets of this run into the string data.
	Vector<TextProps::TextIntProp> m_vtxip;		// Scalar-valued properties for this run.
	Vector<TextProps::TextStrProp> m_vtxsp;		// Text-valued properties for this run
	Vector<FwXml::TextGuidValuedProp> m_vtgvp;	// Guid-valued properties for this run.
	Vector<FwXml::BasicRunInfo> m_vbri;			// Offsets of all the runs into the string data.
	XML_StartElementHandler m_startOuterHandler;	// Outermost Start Element Handler.
	XML_EndElementHandler m_endOuterHandler;		// Outermost End Element Handler.
	HashMapChars<int> m_hmcidhobj;
	Vector<char> m_vchHex;			// Character data for <Binary> or <Run type="picture">.
	Vector<FwXml::RunPropInfo> m_vrpi;	// Raw binary formatting data for all the runs.
	bool m_fInBinary;				// Flag that we're parsing a <Binary> element.
	bool m_fInString;				// Flag that we're parsing a <Str> (or <AStr>) element.
	int m_ws;						// Writing system for <AStr> or <AUni>.
	FILE * m_pfileLog;				// Old fashioned C FILE pointer for log output file.
	StrAnsiBufPath m_stabpFile;		// Name of the input file: used for log file messages.
	StrAnsiBufPath m_stabpLog;		// Name of the log output file.
	void LogMessage(const char * pszMsg);
	int GetWsFromIcuLocale(const char * pszWs, int stidErrMsg);
	HashMapChars<int> m_hmcws;			// Map writing system string to writing system integer.
	int m_celemStart;
	int m_celemEnd;
	Vector<ElemTypeInfo> m_vetiOpen;
	Vector<SeqPropInfo> m_vspiOpen;		// Stack of currently open properties.
	Vector<void *> m_vpvOpen;			// Stack of open objects or HVOs
	Vector<void *> m_vpvClosed;			// Stack of closed objects, waiting to be stored in the
										// appropriate property.
	Vector<int> m_vldcClosed;			// Class constants for closed objects

	HashMapChars<ElemTypeInfo> m_hmceti;

	// These data elements and methods are specific to LanguageDefinitionFactory.
	IWritingSystemPtr m_qws;
	Vector<LanguageDefinition::CharDef> m_vcdPua;
	Vector<StrUni> m_vstuFonts;
	StrUni m_stuEncConvInstall;
	StrUni m_stuEncConvFile;
	StrUni m_stuKeyboard;
	StrUni m_stuBaseLocale;
	StrUni m_stuCollationElements;
	StrUni m_stuEthnoCode;
	StrUni m_stuLocaleScript;
	StrUni m_stuLocaleCountry;
	StrUni m_stuLocaleName;
	StrUni m_stuLocaleResources;
	StrUni m_stuLocaleVariant;
	StrUni m_stuLocaleWinLCID;
	StrUni m_stuNewLocale;

	bool m_fInUnicode;
	int m_wsMulti;					// Nonzero for <AStr ws="XXX"> and <AUni ws="XXX">
	ITsStrFactoryPtr m_qtsf;

	void SetIntegerField(int nVal);
	void CreateTsString(ITsString ** pptss);
	void SetStringField();
	void SetUnicodeField();
	void SetMultiStringField();
	void SetMultiUnicodeField();

	ElemTypeInfo GetElementType(const char * pszElement);
	bool CheckValidBasicType(int nClass, int nField, int cpt);

	static void HandleStartTag(void * pvUser, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	static void HandleEndTag(void * pvUser, const XML_Char * pszName);

	//:> These methods are implemented in FwXmlString.cpp.
	void SetIntegerProperty(TextProps::TextIntProp & txip);
	void SetStringProperty(TextProps::TextStrProp & txsp);
	void SetStringProperty(FwXml::TextGuidValuedProp & tgvp);
	void ProcessStringStartTag(const XML_Char * pszName, const XML_Char ** prgpszAtts);
	void ProcessStringEndTag(const XML_Char * pszName);
	void ProcessCharData(const XML_Char * prgch, int cch);
	void SetTextColor(const XML_Char ** prgpszAtts, const char * pszAttName, int scp);
	void SetTextToggle(const XML_Char ** prgpszAtts, const char * pszAttName, int scp);
	void SetTextWs(const XML_Char ** prgpszAtts, const char * pszAttName, int scp);
	void SetTextMetric(const XML_Char ** prgpszAtts, const char * pszAttName,
		const char * pszUnitAttName, int scp);
	void SetTextSuperscript(const XML_Char ** prgpszAtts);
	void SetTextUnderline(const XML_Char ** prgpszAtts);
	void SetStringProperty(const XML_Char ** prgpszAtts, const char * pszAttName, int stp,
		wchar chType = 0);
	void SetTagsAsStringProp(int stp, const char * pszVal);
	void SetObjDataAsStringProp(int stp, const char * pszVal, wchar chType);
	void SaveCharDataInRun();
	void SavePictureDataInRun();
	bool StoreRunInformation();
	bool StoreRawPropertyBytes();
	void ConvertToRawBytes(FwXml::RunPropInfo & rpi);
	int ConvertPictureToBitmap(const char * prgchHex, int cch, byte * prgbBin);
	bool SetWsIfNeeded(const XML_Char * prgch, int cch);
};

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
FwXmlImportData::FwXmlImportData()
{
	m_parser = NULL;
	m_fError = false;
	m_hr = S_OK;
	m_fIcuLocale = false;
	m_fInRun = false;
	m_fRunHasChars = false;
	m_startOuterHandler = NULL;
	m_endOuterHandler = NULL;
	m_fInBinary = false;
	m_fInString = false;
	m_ws = 0;
	m_pfileLog = NULL;
	m_celemStart = 0;
	m_celemEnd = 0;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
FwXmlImportData::~FwXmlImportData()
{
	if (m_parser)
	{
		XML_ParserFree(m_parser);
		m_parser = NULL;
	}
	if (m_pfileLog)
	{
		fclose(m_pfileLog);
		m_pfileLog = NULL;
		// No errors, no need for log file!
		if (!m_fError)
			::DeleteFileA(m_stabpLog.Chars());
	}
}

/*----------------------------------------------------------------------------------------------
	Log a message to the log file, if one exists.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::LogMessage(const char * pszMsg)
{
	AssertPsz(pszMsg);
	if (m_pfileLog)
	{
		if (m_parser)
		{
			fprintf(m_pfileLog, "%s:%d: ",
				m_stabpFile.Chars(), XML_GetCurrentLineNumber(m_parser));
		}
		fputs(pszMsg, m_pfileLog);
	}
}

/*----------------------------------------------------------------------------------------------
	Map writing system string to writing system integer.
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::GetWsFromIcuLocale(const char * pszWs, int stidErrMsg)
{
	int ws;
	if (m_hmcws.Retrieve(pszWs, &ws))
		return ws;

	StrAnsi staFmt(stidErrMsg);
	StrAnsi sta;
	sta.Format(staFmt.Chars(), pszWs);
	LogMessage(sta.Chars());
	return 0;
}


// This enum must be updated for any new elements added to LanguageDefinition.dtd!
enum {
	kldcColl = 1,
	kldcWs,
	kldfAbbr,
	kldfBodyFontFeatures,
	kldfCapitalizationInfo,
	kldfCollations,
	kldfDefaultBodyFont,
	kldfDefaultMonospace,
	kldfDefaultSansSerif,
	kldfDefaultSerif,
	kldfDescription,
	kldfFontVariation,
	kldfIcuLocale,
	kldfIcuResourceName,
	kldfIcuResourceText,
	kldfIcuRules,
	kldfKeyboardType,
	kldfKeymanKeyboard,
	kldfLastModified,
	kldfLegacyMapping,
	kldfLocale,
	kldfMatchedPairs,
	kldfName,
	kldfPunctuationPatterns,
	kldfQuotationMarks,
	kldfRightToLeft,
	kldfSansFontVariation,
	kldfSpellCheckDictionary,
	kldfValidChars,
	kldfWinCollation,
	kldfWinLCID,
	kldpBaseLocale,
	kldpCharDef,
	kldpCollationElements,
	kldpEncodingConverter,
	kldpEthnoCode,
	kldpFont,
	kldpFonts,
	kldpKeyboard,
	kldpLocaleCountry,
	kldpLocaleName,
	kldpLocaleResources,
	kldpLocaleScript,
	kldpLocaleVariant,
	kldpLocaleWinLCID,
	kldpNewLocale,
	kldpPuaDefinitions,
};

/*----------------------------------------------------------------------------------------------
	Determine the type of XML element we have.  This table must be kept in sync with
	LanguageDefinition.dtd!  The table must be sorted in strcmp collation order for the binary
	search in CheckValidElement() to work.

	NOTE: FieldWorks conceptual model classes are type keltyObject.  FieldWorks conceptual model
			fields are type keltyPropName.  Other language definition file elements are type
			keltyPropValue, except for <LanguageDefinition>, which is type keltyLangDef.
----------------------------------------------------------------------------------------------*/
static ElemTypeInfo g_rgeti[] = {
//	  XML Element Name			element type		enum value				seq?
	{ "Abbr24",					keltyPropName,		kldfAbbr,				false },
	{ "BaseLocale",				keltyPropValue,		kldpBaseLocale,			false },
	{ "BodyFontFeatures24",		keltyPropName,		kldfBodyFontFeatures,	false },
	{ "CapitalizationInfo24",	keltyPropName,		kldfCapitalizationInfo,	false },
	{ "CharDef",				keltyPropValue,		kldpCharDef,			false },
	{ "CollationElements",		keltyPropValue,		kldpCollationElements,	false },
	{ "Collations24",			keltyPropName,		kldfCollations,			true  },
	{ "DefaultBodyFont24",		keltyPropName,		kldfDefaultBodyFont,	false },
	{ "DefaultMonospace24",		keltyPropName,		kldfDefaultMonospace,	false },
	{ "DefaultSansSerif24",		keltyPropName,		kldfDefaultSansSerif,	false },
	{ "DefaultSerif24",			keltyPropName,		kldfDefaultSerif,		false },
	{ "Description24",			keltyPropName,		kldfDescription,		false },
	{ "EncodingConverter",		keltyPropValue,		kldpEncodingConverter,	false },
	{ "EthnoCode",				keltyPropValue,		kldpEthnoCode,			false },
	{ "Font",					keltyPropValue,		kldpFont,				false },
	{ "FontVariation24",		keltyPropName,		kldfFontVariation,		false },
	{ "Fonts",					keltyPropValue,		kldpFonts,				true  },
	{ "ICULocale24",			keltyPropName,		kldfIcuLocale,			false },
	{ "ICURules30",				keltyPropName,		kldfIcuRules,			false },
	{ "IcuLocale24",			keltyPropName,		kldfIcuLocale,			false },
	{ "IcuResourceName30",		keltyPropName,		kldfIcuResourceName,	false },
	{ "IcuResourceText30",		keltyPropName,		kldfIcuResourceText,	false },
	{ "IcuRules30",				keltyPropName,		kldfIcuRules,			false },
	{ "Keyboard",				keltyPropValue,		kldpKeyboard,			false },
	{ "KeyboardType24",			keltyPropName,		kldfKeyboardType,		false },
	{ "KeymanKeyboard24",		keltyPropName,		kldfKeymanKeyboard,		false },
	{ "LanguageDefinition",		keltyLangDef,		0,						false },
	{ "LastModified24",			keltyPropName,		kldfLastModified,		false },
	{ "LegacyMapping24",		keltyPropName,		kldfLegacyMapping,		false },
	{ "LgCollation",			keltyObject,		kldcColl,				false },
	{ "LgWritingSystem",		keltyObject,		kldcWs,					false },
	{ "Locale24",				keltyPropName,		kldfLocale,				false },
	{ "LocaleCountry",			keltyPropValue,		kldpLocaleCountry,		false },
	{ "LocaleName",				keltyPropValue,		kldpLocaleName,			false },
	{ "LocaleResources",		keltyPropValue,		kldpLocaleResources,	false },
	{ "LocaleScript",			keltyPropValue,		kldpLocaleScript,		false },
	{ "LocaleVariant",			keltyPropValue,		kldpLocaleVariant,		false },
	{ "LocaleWinLCID",			keltyPropValue,		kldpLocaleWinLCID,		false },
	{ "MatchedPairs24",			keltyPropName,		kldfMatchedPairs,		false },
	{ "Name24",					keltyPropName,		kldfName,				false },
	{ "Name30",					keltyPropName,		kldfName,				false },
	{ "NewLocale",				keltyPropValue,		kldpNewLocale,			false },
	{ "PuaDefinitions",			keltyPropValue,		kldpPuaDefinitions,		true  },
	{ "PunctuationPatterns24",	keltyPropName,		kldfPunctuationPatterns,false },
	{ "QuotationMarks24",		keltyPropName,		kldfQuotationMarks,		false },
	{ "RightToLeft24",			keltyPropName,		kldfRightToLeft,		false },
	{ "SansFontVariation24",	keltyPropName,		kldfSansFontVariation,	false },
	{ "SpellCheckDictionary24",	keltyPropName,		kldfSpellCheckDictionary,false },
	{ "ValidChars24",			keltyPropName,		kldfValidChars,			false },
	{ "WinCollation30",			keltyPropName,		kldfWinCollation,		false },
	{ "WinLCID30",				keltyPropName,		kldfWinLCID,			false },
};
static const int g_crgeti = isizeof(g_rgeti) / isizeof(ElemTypeInfo);
static ElemTypeInfo CheckValidElement(const char * pszElement)
{
	int iMin = 0;
	int iLim = g_crgeti;
	int iMid;
	int ncmp;
	// Perform a binary search
	while (iMin < iLim)
	{
		iMid = (iMin + iLim) >> 1;
		ncmp = strcmp(g_rgeti[iMid].m_pszName, pszElement);
		if (ncmp == 0)
		{
			return g_rgeti[iMid];
		}
		else if (ncmp < 0)
		{
			iMin = iMid + 1;
		}
		else
		{
			iLim = iMid;
		}
	}
	ElemTypeInfo etiBad = { NULL, keltyBad, -1, false };
	return etiBad;
}

/*----------------------------------------------------------------------------------------------
	Return the element type information for the given XML element name.
----------------------------------------------------------------------------------------------*/
ElemTypeInfo FwXmlImportData::GetElementType(const char * pszElement)
{
	AssertPsz(pszElement);
	ElemTypeInfo eti;
	StrAnsi staRes;
	if (m_hmceti.Retrieve(pszElement, &eti))
	{
		return eti;
	}
	eti.m_nExact = FwXml::BasicType(pszElement);
	// Don't recognize "FwDatabase" or "Prop" as basic element types.
	if (eti.m_nExact == kcptNil || eti.m_nExact == kcptRuleProp)
		eti.m_nExact = -1;
	if (eti.m_nExact != -1)
	{
		eti.m_elty = keltyBasicProp;
		eti.m_fSeq = false;
		eti.m_pszName = NULL;
	}
	else
	{
		eti = CheckValidElement(pszElement);
	}
	m_hmceti.Insert(pszElement, eti);
	return eti;
}

/*----------------------------------------------------------------------------------------------
	Return false if the field (property) is undefined for the class or if it does not
	take the given kind of basic object.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::CheckValidBasicType(int nClass, int nField, int cpt)
{
	switch (nClass)
	{
	case kldcWs:
		switch (nField)
		{
		case kldfName:
			return (cpt == kcptMultiUnicode);
		case kldfAbbr:
			return (cpt == kcptMultiUnicode);
		case kldfRightToLeft:
			return (cpt == kcptBoolean);
		case kldfDefaultSerif:
			return (cpt == kcptUnicode);
		case kldfDefaultSansSerif:
			return (cpt == kcptUnicode);
		case kldfDefaultBodyFont:
			return (cpt == kcptUnicode);
		case kldfDefaultMonospace:
			return (cpt == kcptUnicode);
		case kldfKeymanKeyboard:
			return (cpt == kcptUnicode);
		case kldfFontVariation:
			return (cpt == kcptUnicode);
		case kldfSansFontVariation:
			return (cpt == kcptUnicode);
		case kldfBodyFontFeatures:
			return (cpt == kcptUnicode);
		case kldfKeyboardType:
			return (cpt == kcptUnicode);
		case kldfDescription:
			return (cpt == kcptMultiString);
		case kldfLocale:
			return (cpt == kcptInteger);
		case kldfIcuLocale:
			return (cpt == kcptUnicode);
		case kldfLegacyMapping:
			return (cpt == kcptUnicode);
		case kldfValidChars:
			return (cpt == kcptUnicode);
		case kldfSpellCheckDictionary:
			return (cpt == kcptUnicode);
		case kldfMatchedPairs:
			return (cpt == kcptUnicode);
		case kldfPunctuationPatterns:
			return (cpt == kcptUnicode);
		case kldfQuotationMarks:
			return (cpt == kcptUnicode);
		case kldfLastModified:
			return (cpt == kcptTime);
		case kldfCapitalizationInfo:
			return (cpt == kcptUnicode);
		}
		break;
	case kldcColl:
		switch (nField)
		{
		case kldfIcuResourceName:
			return (cpt == kcptUnicode);
		case kldfIcuResourceText:
			return (cpt == kcptUnicode);
		case kldfIcuRules:
			return (cpt == kcptUnicode);
		case kldfName:
			return (cpt == kcptMultiUnicode);
		case kldfWinCollation:
			return (cpt == kcptUnicode);
		case kldfWinLCID:
			return (cpt == kcptInteger);
		}
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements.
	This function is passed to the expat XML parser as a callback function.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandleStartTag(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);
	++pxid->m_celemStart;
	LanguageDefinition::CharDef cd;

	StrAnsi sta;
	StrAnsi staFmt;
	try
	{
		IWritingSystemPtr qws;
		ICollationPtr qcoll;
		void * pvObject;
		ElemTypeInfo eti = pxid->GetElementType(pszName);
		ElemTypeInfo etiProp;
		ElemTypeInfo etiObject;
		SeqPropInfo spi;
		int nVal;
		const char * pszVal;
		char * psz;
		const char * pszCode;
		const char * pszData;
		const char * pszInstall;
		const char * pszFile;
		StrUni stu;

		switch (eti.m_elty)
		{
		case keltyLangDef:
			if (pxid->m_vetiOpen.Size())
			{
				// This must be the outermost element.
				// "<%<0>s> must be the outermost XML element!?"
				staFmt.Load(kstidXmlErrorMsg010);
				sta.Format(staFmt.Chars(), "LanguageDefinition");
				pxid->LogMessage(sta.Chars());
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			pxid->m_vetiOpen.Push(eti);
			break;
		case keltyObject:
			if (!pxid->m_vetiOpen.Size())
			{
				// This must not be the outermost element!
				// "<%<0>s> must be nested inside <%<1>s>...</%<1>s>!"
				staFmt.Load(kstidXmlErrorMsg004);
				sta.Format(staFmt.Chars(),
					pszName, eti.m_nExact == kldcWs ? "LanguageDefinition" : "Collations24");
				pxid->LogMessage(sta.Chars());
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			switch (eti.m_nExact)
			{
			case kldcWs:
				WritingSystem::CreateCom(NULL, IID_IWritingSystem, (void **)&qws);
				//qws.CreateInstance(CLSID_WritingSystem);
				pvObject = (void *)qws.Detach();
				break;
			case kldcColl:
				Collation::CreateCom(NULL, IID_ICollation, (void **)&qcoll);
				//qcoll.CreateInstance(CLSID_Collation);
				pvObject = (void *)qcoll.Detach();
				break;
			default:
				pvObject = NULL;
				break;
			}
			if (pxid->m_vspiOpen.Size())
			{
				SeqPropInfo * pspiTmp = pxid->m_vspiOpen.Top();
				pspiTmp->m_cobj++;
			}
			pxid->m_vetiOpen.Push(eti);
			pxid->m_vpvOpen.Push(pvObject);
			break;
		case keltyPropName:
			if (pxid->m_vetiOpen.Size() < 2)
			{
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
			if (etiObject.m_elty != keltyObject)
			{
				//"<%<0>s> must be nested inside an object element!"
				staFmt.Load(kstidXmlErrorMsg006);
				sta.Format(staFmt.Chars(), pszName);
				pxid->LogMessage(sta.Chars());
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			spi.m_fSeq = eti.m_fSeq;
			spi.m_cobj = 0;
			pxid->m_vspiOpen.Push(spi);
			pxid->m_vetiOpen.Push(eti);
			break;
		case keltyBasicProp:
			if (pxid->m_vetiOpen.Size() < 3)
			{
				// "<%<0>s> must be nested inside an object attribute element!"
				staFmt.Load(kstidXmlErrorMsg005);
				sta.Format(staFmt.Chars(), pszName);
				pxid->LogMessage(sta.Chars());
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			etiProp = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
			if (etiProp.m_elty != keltyPropName)
			{
				// "<%<0>s> must be nested inside an object attribute element!"
				staFmt.Load(kstidXmlErrorMsg005);
				sta.Format(staFmt.Chars(), pszName);
				pxid->LogMessage(sta.Chars());
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 2];
			if (etiObject.m_elty != keltyObject)
			{
				//"<%<0>s> must be nested inside an object element!"
				staFmt.Load(kstidXmlErrorMsg006);
				sta.Format(staFmt.Chars(), pszName);
				pxid->LogMessage(sta.Chars());
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			if (!pxid->CheckValidBasicType(etiObject.m_nExact, etiProp.m_nExact, eti.m_nExact))
			{
				// Invalid basic property in this context: "<%<0>s> is improperly nested!"
				staFmt.Load(kstidXmlErrorMsg002);
				sta.Format(staFmt.Chars(), pszName);
				pxid->LogMessage(sta.Chars());
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			pxid->m_vetiOpen.Push(eti);
			switch (eti.m_nExact)
			{
			case kcptBoolean:
				/*
					<!ELEMENT Boolean EMPTY >
					<!ATTLIST Boolean val (true | false) #REQUIRED >
				 */
				pszVal = FwXml::GetAttributeValue(prgpszAtts, "val");
				nVal = 0;
				if (!pszVal)
				{
					// "Missing val attribute in Boolean element"
					break;
				}
				if (strcmp(pszVal, "true") == 0)
					nVal = 1;
				else if (strcmp(pszVal, "false") == 0)
					nVal = 0;
				else
				{
					// "Invalid Boolean val attribute value: %<0>s"
					break;
				}
				pxid->SetIntegerField(nVal);
				break;

			case kcptInteger:
				/*
					<!ELEMENT Integer EMPTY >
					<!ATTLIST Integer val CDATA #REQUIRED >
				 */
				nVal = 0;
				pszVal = FwXml::GetAttributeValue(prgpszAtts, "val");
				if (!pszVal)
				{
					// "Missing val attribute in Integer element"
					break;
				}
				nVal = static_cast<int>(strtol(pszVal, &psz, 10));
				if (*psz || !*pszVal)
				{
					// "Invalid Integer val attribute value: %<0>s"
					break;
				}
				pxid->SetIntegerField(nVal);
				break;

			case kcptString:				// May actually be kcptBigString.
				/*
					<!ELEMENT Str (#PCDATA | Run)* >
				 */
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = false;
				pxid->m_fInString = true;
				pxid->m_fInRun = false;
				pxid->m_wsMulti = 0;
				// The next tag will be internal to the string.
				XML_SetElementHandler(pxid->m_parser,
					FwXml::HandleStringStartTag, FwXml::HandleStringEndTag);
				break;

			case kcptUnicode:				// May actually be kcptBigUnicode.
				/*
					<!ELEMENT Uni (#PCDATA) >
				 */
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				pxid->m_fInString = false;
				pxid->m_fInRun = false;
				pxid->m_wsMulti = 0;
				break;

			case kcptMultiString:			// May actually be kcptMultiBigString.
				/*
					<!ELEMENT AStr (#PCDATA | Run)* >
					<!ATTLIST ws CDATA #REQUIRED>
				 */
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = false;
				pxid->m_fInString = true;
				pxid->m_fInRun = false;
				pszVal = FwXml::GetAttributeValue(prgpszAtts, "ws");
				if (pszVal)
					pxid->m_wsMulti = pxid->GetWsFromIcuLocale(pszVal, 0);
				else
					pxid->m_wsMulti = 0;
				// The next tag will be internal to the string.
				XML_SetElementHandler(pxid->m_parser,
					FwXml::HandleStringStartTag, FwXml::HandleStringEndTag);
				break;

			case kcptMultiUnicode:			// May actually be kcptMultiBigUnicode.
				/*
					<!ELEMENT AUni (#PCDATA) >
					<!ATTLIST ws CDATA #REQUIRED>
				 */
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				pxid->m_fInString = false;
				pxid->m_fInRun = false;
				pszVal = FwXml::GetAttributeValue(prgpszAtts, "ws");
				if (pszVal)
					pxid->m_wsMulti = pxid->GetWsFromIcuLocale(pszVal, 0);
				else
					pxid->m_wsMulti = 0;
				break;
			}
			break;
		case keltyPropValue:
			if (!pxid->m_vetiOpen.Size())
			{
				// "<%<0>s> must be nested inside <%<1>s>...</%<1>s>!"
				staFmt.Load(kstidXmlErrorMsg004);
				sta.Format(staFmt.Chars(), pszName, "LanguageDefinition");
				pxid->LogMessage(sta.Chars());
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			pxid->m_vetiOpen.Push(eti);
			switch (eti.m_nExact)
			{
			case kldpBaseLocale:
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				break;
			case kldpCharDef:
				pszCode = FwXml::GetAttributeValue(prgpszAtts, "code");
				pszData = FwXml::GetAttributeValue(prgpszAtts, "data");
				if (!pszCode || !*pszCode)
				{
					// "Missing CharDef code attribute value."
					sta.Load(kstidLangDefXmlMsg001);
					pxid->LogMessage(sta.Chars());
					break;		// ThrowHr(WarnHr(E_UNEXPECTED));
				}
				if (!pszData || !*pszData)
				{
					// "Missing CharDef data attribute value."
					sta.Load(kstidLangDefXmlMsg002);
					pxid->LogMessage(sta.Chars());
					break;		// ThrowHr(WarnHr(E_UNEXPECTED));
				}
				// Add this character definition to the internal list.
				cd.m_code = static_cast<int>(strtol(pszCode, &psz, 16));
				if (*psz)
				{
					// "Invalid CharDef code attribute value: ""%<0>s""."
					staFmt.Load(kstidLangDefXmlMsg003);
					sta.Format(staFmt.Chars(), pszCode);
					pxid->LogMessage(sta.Chars());
					break;		// ThrowHr(WarnHr(E_UNEXPECTED));
				}
				StrUtil::StoreUtf16FromUtf8(pszData, strlen(pszData), cd.m_stuData, false);
				pxid->m_vcdPua.Push(cd);
				break;
			case kldpCollationElements:
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				break;
			case kldpEncodingConverter:
				// install CDATA #REQUIRED
				// file CDATA #IMPLIED
				pszInstall = FwXml::GetAttributeValue(prgpszAtts, "install");
				if (!pszInstall || !*pszInstall)
				{
					// "Missing EncodingCOnverter install attribute value"
					sta.Load(kstidLangDefXmlMsg013);
					pxid->LogMessage(sta.Chars());
					break;			// ThrowHr(WarnHr(E_UNEXPECTED));
				}
				StrUtil::StoreUtf16FromUtf8(pszInstall, strlen(pszInstall),
					pxid->m_stuEncConvInstall, false);
				pszFile = FwXml::GetAttributeValue(prgpszAtts, "file");
				if (pszFile && *pszFile)
				{
					StrUtil::StoreUtf16FromUtf8(pszFile, strlen(pszFile),
						pxid->m_stuEncConvFile, false);
				}
				break;
			case kldpEthnoCode:
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				break;
			case kldpFont:
				pszVal = FwXml::GetAttributeValue(prgpszAtts, "file");
				if (!pszVal || !*pszVal)
				{
					// "Missing Font file attribute value."
					sta.Load(kstidLangDefXmlMsg004);
					pxid->LogMessage(sta.Chars());
					break;			// ThrowHr(WarnHr(E_UNEXPECTED));
				}
				// Add the font filename to the list of fonts.
				StrUtil::StoreUtf16FromUtf8(pszVal, strlen(pszVal), stu, false);
				pxid->m_vstuFonts.Push(stu);
				break;
			case kldpFonts:
				// <!ELEMENT Fonts (Font)*>
				break;
			case kldpKeyboard:
				pszVal = FwXml::GetAttributeValue(prgpszAtts, "file");
				if (pszVal && *pszVal)
				{
					// Store the keyboard filename.
					StrUtil::StoreUtf16FromUtf8(pszVal, strlen(pszVal), pxid->m_stuKeyboard,
						false);
				}
				break;
			case kldpLocaleScript:
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				break;
			case kldpLocaleCountry:
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				break;
			case kldpLocaleName:
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				break;
			case kldpLocaleResources:
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				break;
			case kldpLocaleVariant:
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				break;
			case kldpLocaleWinLCID:
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				break;
			case kldpNewLocale:
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				break;
			case kldpPuaDefinitions:
				// <!ELEMENT PuaDefinitions (CharDef)*>
				break;
			}
			break;
		default:
			// Should we just ignore things we don't recognize?
			break;
		}
	}
	catch (Throwable & thr)
	{
		pxid->m_fError = true;
		pxid->m_hr = thr.Error();
#ifdef DEBUG
		StrAnsi staMsg;
		staMsg.Format("ERROR CAUGHT on line %d of %s: %s\n",
			__LINE__, __FILE__, AsciiHresult(pxid->m_hr));
		pxid->LogMessage(staMsg.Chars());
#endif
	}
	catch (...)
	{
		pxid->m_fError = true;
		pxid->m_hr = E_FAIL;
#ifdef DEBUG
		StrAnsi staMsg;
		staMsg.Format("UNKNOWN ERROR CAUGHT on line %d of %s\n", __LINE__, __FILE__);
		pxid->LogMessage(staMsg.Chars());
#endif
	}
}


/*----------------------------------------------------------------------------------------------
	Set a property of the top open object to an integer (or boolean) value.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetIntegerField(int nVal)
{
	if (m_vetiOpen.Size() < 3)
		return;
	ElemTypeInfo etiBasicElem;
	etiBasicElem = m_vetiOpen[m_vetiOpen.Size() - 1];
	if (etiBasicElem.m_elty != keltyBasicProp)
		return;
	ElemTypeInfo etiProp = m_vetiOpen[m_vetiOpen.Size() - 2];
	if (etiProp.m_elty != keltyPropName)
		return;
	ElemTypeInfo etiObject = m_vetiOpen[m_vetiOpen.Size() - 3];
	if (etiObject.m_elty != keltyObject)
		return;

	void * pvObject = *(m_vpvOpen.Top());
	IUnknown * punk = reinterpret_cast<IUnknown *>(pvObject);
	if (!punk)
		return;
	IWritingSystemPtr qws;
	ICollationPtr qcoll;
	switch (etiObject.m_nExact)
	{
	case kldcWs:
		CheckHr(punk->QueryInterface(IID_IWritingSystem, (void**)&qws));
		switch (etiProp.m_nExact)
		{
		case kldfRightToLeft:
			CheckHr(qws->put_RightToLeft(ComBool(nVal)));
			break;
		case kldfLocale:
			CheckHr(qws->put_Locale(nVal));
			break;
		}
		break;

	case kldcColl:
		CheckHr(punk->QueryInterface(IID_ICollation, (void **)&qcoll));
		switch (etiProp.m_nExact)
		{
		case kldfWinLCID:
			CheckHr(qcoll->put_WinLCID(nVal));
			break;
		}
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	Create a TsString from the currently stored string data in m_stuChars, m_vbri, and m_vrpi.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::CreateTsString(ITsString ** pptss)
{
	AssertPtr(pptss);
	*pptss = NULL;
	long cbtext;
	Vector<byte> vbBin;
	int cbBin;
	StrAnsi sta;
	byte * prgbBin;

	cbtext = BstrSize(m_stuChars.Bstr());
	if (!cbtext)
	{
		// "Empty String element? (cbtext = 0)"
		sta.Load(kstidXmlErrorMsg036);
		LogMessage(sta.Chars());
	}
	else
	{
		// Calculate the number of bytes needed for run information, and allocate the output
		// buffer.
		int crun = m_vbri.Size();
		Assert(crun >= m_vrpi.Size());
		cbBin = isizeof(crun);
		if (crun)
		{
			cbBin += crun * isizeof(FwXml::BasicRunInfo);
			for (int i = 0; i < m_vrpi.Size(); ++i)
			{
				cbBin += 2;
				cbBin += m_vrpi[i].m_vbRawProps.Size();
			}
		}
		vbBin.Resize(cbBin);
		prgbBin = vbBin.Begin();
		// Copy the run information to the output buffer.
		cbBin = isizeof(crun);
		memcpy(prgbBin, &crun, cbBin);
		if (crun)
		{
			memcpy(prgbBin + cbBin, m_vbri.Begin(),
				crun * isizeof(FwXml::BasicRunInfo));
			cbBin += crun * isizeof(FwXml::BasicRunInfo);
			for (int i = 0; i < m_vrpi.Size(); ++i)
			{
				prgbBin[cbBin] = m_vrpi[i].m_ctip;
				++cbBin;
				prgbBin[cbBin] = m_vrpi[i].m_ctsp;
				++cbBin;
				memcpy(prgbBin + cbBin, m_vrpi[i].m_vbRawProps.Begin(),
					m_vrpi[i].m_vbRawProps.Size());
				cbBin += m_vrpi[i].m_vbRawProps.Size();
			}
		}
		// We now have the character data in m_stuChars and the raw formatting data in vbBin.
		// Create a string from these!
		if (!m_qtsf)
			m_qtsf.CreateInstance(CLSID_TsStrFactory);
		ITsStringPtr qtss;
		CheckHr(m_qtsf->DeserializeStringRgb(m_stuChars.Bstr(), vbBin.Begin(), vbBin.Size(),
			&qtss));
		ComBool fNfd;
		CheckHr(qtss->get_IsNormalizedForm(knmNFD, &fNfd));
		if (fNfd)
			*pptss = qtss.Detach();
		else
			CheckHr(qtss->get_NormalizedForm(knmNFD, pptss));
	}
	// Final cleanup.
	m_vbri.Clear();
	m_vrpi.Clear();
	m_vtxip.Clear();
	m_vtxsp.Clear();
	m_vtgvp.Clear();
	m_fInString = false;
	m_stuChars.Clear();
}


/*----------------------------------------------------------------------------------------------
	Set a property of the top open object to a TsString value.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetStringField()
{
	// Create a TsString from the current string data.
	ITsStringPtr qtss;
	CreateTsString(&qtss);
	// None of the current elements of LanguageDefinition use a plain TsString!
}

/*----------------------------------------------------------------------------------------------
	Set a property of the top open object to a Unicode value.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetUnicodeField()
{
	ElemTypeInfo etiProp = m_vetiOpen[m_vetiOpen.Size() - 1];
	Assert(etiProp.m_elty == keltyPropName);
	ElemTypeInfo etiObject = m_vetiOpen[m_vetiOpen.Size() - 2];
	Assert(etiObject.m_elty == keltyObject);

	void * pvObject = *(m_vpvOpen.Top());
	IUnknown * punk = reinterpret_cast<IUnknown *>(pvObject);
	if (!punk)
		return;

	IWritingSystemPtr qws;
	ICollationPtr qcoll;
	if (m_stuChars.Length() > 0)
		StrUtil::NormalizeStrUni(m_stuChars, UNORM_NFD);
	switch (etiObject.m_nExact)
	{
	case kldcWs:
		CheckHr(punk->QueryInterface(IID_IWritingSystem, (void**)&qws));
		switch (etiProp.m_nExact)
		{
		case kldfDefaultSerif:
			CheckHr(qws->put_DefaultSerif(m_stuChars.Bstr()));
			break;
		case kldfDefaultSansSerif:
			CheckHr(qws->put_DefaultSansSerif(m_stuChars.Bstr()));
			break;
		case kldfDefaultBodyFont:
			CheckHr(qws->put_DefaultBodyFont(m_stuChars.Bstr()));
			break;
		case kldfDefaultMonospace:
			CheckHr(qws->put_DefaultMonospace(m_stuChars.Bstr()));
			break;
		case kldfKeymanKeyboard:
			CheckHr(qws->put_KeymanKbdName(m_stuChars.Bstr()));
			break;
		case kldfFontVariation:
			CheckHr(qws->put_FontVariation(m_stuChars.Bstr()));
			break;
		case kldfSansFontVariation:
			CheckHr(qws->put_SansFontVariation(m_stuChars.Bstr()));
			break;
		case kldfBodyFontFeatures:
			CheckHr(qws->put_BodyFontFeatures(m_stuChars.Bstr()));
			break;
		case kldfKeyboardType:
			if (m_stuChars == L"keyman")
				CheckHr(qws->put_KeyMan(ComBool(true)));
			else
				CheckHr(qws->put_KeyMan(ComBool(false)));
			break;
		case kldfIcuLocale:
			CheckHr(qws->put_IcuLocale(m_stuChars.Bstr()));
			break;
		case kldfLegacyMapping:
			CheckHr(qws->put_LegacyMapping(m_stuChars.Bstr()));
			break;
		case kldfValidChars:
			CheckHr(qws->put_ValidChars(m_stuChars.Bstr()));
			break;
		case kldfCapitalizationInfo:
			CheckHr(qws->put_CapitalizationInfo(m_stuChars.Bstr()));
			break;
		case kldfMatchedPairs:
			CheckHr(qws->put_MatchedPairs(m_stuChars.Bstr()));
			break;
		case kldfPunctuationPatterns:
			CheckHr(qws->put_PunctuationPatterns(m_stuChars.Bstr()));
			break;
		case kldfQuotationMarks:
			CheckHr(qws->put_QuotationMarks(m_stuChars.Bstr()));
			break;
		case kldfSpellCheckDictionary:
			CheckHr(qws->put_SpellCheckDictionary(m_stuChars.Bstr()));
			break;
		}
		break;
	case kldcColl:
		CheckHr(punk->QueryInterface(IID_ICollation, (void **)&qcoll));
		switch (etiProp.m_nExact)
		{
		case kldfIcuResourceName:
			CheckHr(qcoll->put_IcuResourceName(m_stuChars.Bstr()));
			break;
		case kldfIcuResourceText:
			CheckHr(qcoll->put_IcuResourceText(m_stuChars.Bstr()));
			break;
		case kldfIcuRules:
			CheckHr(qcoll->put_IcuRules(m_stuChars.Bstr()));
			break;
		case kldfWinCollation:
			CheckHr(qcoll->put_WinCollation(m_stuChars.Bstr()));
			break;
		}
		break;
	}

	// Final cleanup.
	m_fInUnicode = false;
	m_stuChars.Clear();
}

/*----------------------------------------------------------------------------------------------
	Set a property of the top open object to a multilingual TsString value.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetMultiStringField()
{
	ElemTypeInfo etiProp = m_vetiOpen[m_vetiOpen.Size() - 1];
	Assert(etiProp.m_elty == keltyPropName);
	ElemTypeInfo etiObject = m_vetiOpen[m_vetiOpen.Size() - 2];
	Assert(etiObject.m_elty == keltyObject);
	void * pvObject = *(m_vpvOpen.Top());
	IUnknown * punk = reinterpret_cast<IUnknown *>(pvObject);
	if (punk && m_wsMulti)
	{
		// Create a TsString from the current string data.
		ITsStringPtr qtss;
		CreateTsString(&qtss);
		IWritingSystemPtr qws;
		ICollationPtr qcoll;
		switch (etiObject.m_nExact)
		{
		case kldcWs:
			CheckHr(punk->QueryInterface(IID_IWritingSystem, (void**)&qws));
			switch (etiProp.m_nExact)
			{
			case kldfDescription:
				CheckHr(qws->put_Description(m_wsMulti, qtss));
				break;
			default:
				break;
			}
		case kldcColl:
			//CheckHr(punk->QueryInterface(IID_ICollation, (void **)&qcoll));
			break;
		}
	}
	// Final cleanup.
	m_wsMulti = 0;
	m_fInString = false;
	m_fInRun = false;
}

/*----------------------------------------------------------------------------------------------
	Set a property of the top open object to a multilingual Unicode value.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetMultiUnicodeField()
{
	ElemTypeInfo etiProp = m_vetiOpen[m_vetiOpen.Size() - 1];
	Assert(etiProp.m_elty == keltyPropName);
	ElemTypeInfo etiObject = m_vetiOpen[m_vetiOpen.Size() - 2];
	Assert(etiObject.m_elty == keltyObject);
	void * pvObject = *(m_vpvOpen.Top());
	IUnknown * punk = reinterpret_cast<IUnknown *>(pvObject);
	if (punk && m_wsMulti)
	{
		IWritingSystemPtr qws;
		ICollationPtr qcoll;
		if (m_stuChars.Length() > 0)
			StrUtil::NormalizeStrUni(m_stuChars, UNORM_NFD);
		switch (etiObject.m_nExact)
		{
		case kldcWs:
			CheckHr(punk->QueryInterface(IID_IWritingSystem, (void**)&qws));
			switch (etiProp.m_nExact)
			{
			case kldfName:
				CheckHr(qws->put_Name(m_wsMulti, m_stuChars.Bstr()));
				break;
			case kldfAbbr:
				CheckHr(qws->put_Abbr(m_wsMulti, m_stuChars.Bstr()));
				break;
			}
			break;
		case kldcColl:
			CheckHr(punk->QueryInterface(IID_ICollation, (void **)&qcoll));
			switch (etiProp.m_nExact)
			{
			case kldfName:
				CheckHr(qcoll->put_Name(m_wsMulti, m_stuChars.Bstr()));
				break;
			default:
				break;
			}
			break;
		}
	}
	// Final cleanup.
	m_wsMulti = 0;
	m_fInUnicode = false;
	m_stuChars.Clear();
}


/*----------------------------------------------------------------------------------------------
	Handle XML end elements.
	This function is passed to the expat XML parser as a callback function.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::HandleEndTag(void * pvUser, const XML_Char * pszName)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);
	++pxid->m_celemEnd;
	StrAnsi staFmt;
	StrAnsi sta;

	ElemTypeInfo eti;
	if (!pxid->m_vetiOpen.Pop(&eti))
	{
		// THIS SHOULD NEVER HAPPEN! - "Unbalanced XML element stack!?"
		sta.Load(kstidXmlErrorMsg123);
		pxid->LogMessage(sta.Chars());
		pxid->m_fError = true;
		pxid->m_hr = E_UNEXPECTED;
		return;
	}

	ElemTypeInfo etiObject;
	Vector<void *> vpvValue;
	Vector<int> vldcValue;
	void * pvObject;
	IUnknown * punk;
	int ldc;
	int ipv;
	int cobj;
	int iobj;
	IWritingSystemPtr qws;

	switch (eti.m_elty)
	{
	case keltyLangDef:
		// Store the IWritingSystem object -- it better exist!
		if (!pxid->m_vpvClosed.Pop(&pvObject) || !pxid->m_vldcClosed.Pop(&ldc))
		{
			// THIS SHOULD NEVER HAPPEN! - "Missing WritingSystem definition!?"
			sta.Load(kstidLangDefXmlMsg005);
			pxid->LogMessage(sta.Chars());
			pxid->m_fError = true;
			pxid->m_hr = E_UNEXPECTED;
			return;
		}
		if (pxid->m_vpvClosed.Size() || pxid->m_vldcClosed.Size())
		{
			// THIS SHOULD NEVER HAPPEN! - "Unbalanced object stack!?"
			sta.Load(kstidLangDefXmlMsg006);
			pxid->LogMessage(sta.Chars());
			pxid->m_fError = true;
			pxid->m_hr = E_UNEXPECTED;
			return;
		}
		if (pvObject == NULL)
		{
			// THIS SHOULD NEVER HAPPEN! - "Missing WritingSystem definition!?"
			sta.Load(kstidLangDefXmlMsg005);
			pxid->LogMessage(sta.Chars());
			pxid->m_fError = true;
			pxid->m_hr = E_UNEXPECTED;
			return;
		}
		punk = reinterpret_cast<IUnknown *>(pvObject);
		AssertPtr(punk);
		punk->QueryInterface(IID_IWritingSystem, (void **)&pxid->m_qws);
		//  Clear the reference count--originally from the closed-list.
		punk->Release();
		if (!pxid->m_qws)
		{
			// THIS SHOULD NEVER HAPPEN! - "Missing WritingSystem definition!?"
			sta.Load(kstidLangDefXmlMsg005);
			pxid->LogMessage(sta.Chars());
			pxid->m_fError = true;
			pxid->m_hr = E_UNEXPECTED;
			return;
		}
		break;
	case keltyObject:
		void * pv;
		if (!pxid->m_vpvOpen.Pop(&pv))
		{
			// THIS SHOULD NEVER HAPPEN! - "Unbalanced object stack!?"
			sta.Load(kstidLangDefXmlMsg006);
			pxid->LogMessage(sta.Chars());
			pxid->m_fError = true;
			pxid->m_hr = E_UNEXPECTED;
			return;
		}
		if (eti.m_nExact == kldcColl)
		{
			// Verify that this is not an empty definition.  If it is, ignore it.
			IUnknown * punk = reinterpret_cast<IUnknown *>(pv);
			AssertPtr(punk);
			ICollationPtr qcoll;
			CheckHr(punk->QueryInterface(IID_ICollation, (void **)&qcoll));
			AssertPtr(qcoll.Ptr());
			SmartBstr sbstrWinColl;
			SmartBstr sbstrIcuName;
			CheckHr(qcoll->get_WinCollation(&sbstrWinColl));
			CheckHr(qcoll->get_IcuResourceName(&sbstrIcuName));
			if (!sbstrWinColl.Length() && !sbstrIcuName.Length())
			{
				// Don't store this object since it doesn't have a valid content!
				punk->Release();
				break;
			}
		}
		// Now the closed-list is responsible for the ref count (if any).
		pxid->m_vpvClosed.Push(pv);
		pxid->m_vldcClosed.Push(eti.m_nExact);
		break;
	case keltyPropName:
		SeqPropInfo spi;
		if (!pxid->m_vspiOpen.Pop(&spi))
		{
			// THIS SHOULD NEVER HAPPEN! - "Unbalanced property name stack!?"
			sta.Load(kstidXmlErrorMsg126);
			pxid->LogMessage(sta.Chars());
			pxid->m_fError = true;
			pxid->m_hr = E_UNEXPECTED;
			return;
		}
		cobj = spi.m_cobj;
		// Pop cobj closed objects off the stack and set the property to the
		// cumulative value.
		vpvValue.Resize(cobj);
		vldcValue.Resize(cobj);
		for (iobj = 0; iobj < cobj; iobj++)
		{
			void * pv;
			if (!pxid->m_vpvClosed.Pop(&pv) || !pxid->m_vldcClosed.Pop(&ldc))
			{
				// THIS SHOULD NEVER HAPPEN! - "Unbalanced property value stack!?"
				sta.Load(kstidLangDefXmlMsg007);
				pxid->LogMessage(sta.Chars());
				pxid->m_fError = true;
				pxid->m_hr = E_UNEXPECTED;
				return;
			}
			vpvValue[cobj - iobj - 1] = pv;
			vldcValue[cobj - iobj - 1] = ldc;
		}
		if (spi.m_cobj > 1 && !spi.m_fSeq)
		{
			// "Cannot put multiple objects in an atomic property."
			sta.Load(kstidLangDefXmlMsg008);
			pxid->LogMessage(sta.Chars());
			pxid->m_fError = true;
			pxid->m_hr = E_UNEXPECTED;
			return;
		}
		switch (eti.m_nExact)
		{
		case kldfCollations:
			etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
			Assert(etiObject.m_elty == keltyObject);
			pvObject = *(pxid->m_vpvOpen.Top());
			if (pvObject != NULL)
			{
				punk = reinterpret_cast<IUnknown *>(pvObject);
				AssertPtr(punk);
				punk->QueryInterface(IID_IWritingSystem, (void **)&qws);
			}
			if (qws)
			{
				for (ipv = 0; ipv < vpvValue.Size(); ipv++)
				{
					ICollationPtr qcoll;
					punk = reinterpret_cast<IUnknown *>(vpvValue[ipv]);
					AssertPtr(punk);
					CheckHr(punk->QueryInterface(IID_ICollation, (void **)&qcoll));
					AssertPtr(qcoll.Ptr());
					CheckHr(qws->putref_Collation(ipv, qcoll));
					//  Clear the reference count--originally from the open-list.
					punk->Release();
				}
				qws.Clear();
			}
			else
			{
				// we still need to clear memory on the Collations.
				for (ipv = 0; ipv < vpvValue.Size(); ipv++)
				{
					punk = reinterpret_cast<IUnknown *>(vpvValue[ipv]);
					AssertPtr(punk);
					punk->Release();
				}
			}
			break;
		default:
			break;
		}
		break;
	case keltyBasicProp:
		switch (eti.m_nExact)
		{
		case kcptString:
			pxid->SetStringField();
			break;

		case kcptUnicode:
			pxid->SetUnicodeField();
			break;

		case kcptMultiString:
			pxid->SetMultiStringField();
			break;

		case kcptMultiUnicode:
			pxid->SetMultiUnicodeField();
			break;
		default:
			// All other basic property data is stored in start tag attribute values.
			break;
		}
		break;

	case keltyPropValue:
		if (pxid->m_stuChars.Length() > 0)
			StrUtil::NormalizeStrUni(pxid->m_stuChars, UNORM_NFD);
		switch (eti.m_nExact)
		{
		case kldpBaseLocale:
			pxid->m_stuBaseLocale = pxid->m_stuChars;
			pxid->m_stuChars.Clear();
			break;
		case kldpCollationElements:
			pxid->m_stuCollationElements = pxid->m_stuChars;
			pxid->m_stuChars.Clear();
			break;
		case kldpEthnoCode:
			pxid->m_stuEthnoCode = pxid->m_stuChars;
			pxid->m_stuChars.Clear();
			break;
		case kldpLocaleScript:
			pxid->m_stuLocaleScript = pxid->m_stuChars;
			pxid->m_stuChars.Clear();
			break;
		case kldpLocaleCountry:
			pxid->m_stuLocaleCountry = pxid->m_stuChars;
			pxid->m_stuChars.Clear();
			break;
		case kldpLocaleName:
			pxid->m_stuLocaleName = pxid->m_stuChars;
			pxid->m_stuChars.Clear();
			break;
		case kldpLocaleResources:
			pxid->m_stuLocaleResources = pxid->m_stuChars;
			pxid->m_stuChars.Clear();
			break;
		case kldpLocaleVariant:
			pxid->m_stuLocaleVariant = pxid->m_stuChars;
			pxid->m_stuChars.Clear();
			break;
		case kldpLocaleWinLCID:
			pxid->m_stuLocaleWinLCID = pxid->m_stuChars;
			pxid->m_stuChars.Clear();
			break;
		case kldpNewLocale:
			pxid->m_stuNewLocale = pxid->m_stuChars;
			pxid->m_stuChars.Clear();
			break;
		default:
			// All other language property elements are already handled.
			break;
		}
		break;
	default:
		// Should we just ignore things we don't recognize?
		break;
	}
}

//:>********************************************************************************************
//:>	LanguageDefinitionFactory methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
LanguageDefinitionFactory::LanguageDefinitionFactory()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
LanguageDefinitionFactory::~LanguageDefinitionFactory()
{
}


/*----------------------------------------------------------------------------------------------
	Initialize the language definition with the given IWritingSystem object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinitionFactory::Initialize(IWritingSystem * pws,
	LanguageDefinition ** ppld)
{
	AssertPtrN(pws);
	AssertPtrN(ppld);
	if (!ppld)
		return E_POINTER;

	m_qld.Create();
	m_qld->put_WritingSystem(pws);
	*ppld = m_qld;
	(*ppld)->AddRef();

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Check for a UTF-8 Byte Order Mark at the beginning of the buffer.  If present, erase it by
	shifting the contents of the buffer and subtracting 3 from cbRead.
----------------------------------------------------------------------------------------------*/
static void CheckForBOM(void * pBuffer, ulong & cbRead)
{
	unsigned char * pch = (unsigned char *)pBuffer;
	if (pch[0] == 0xEF && pch[1] == 0xBB && pch[2] == 0xBF)
	{
		// We have a BOM which is totally unnecessary, since the data is a byte stream.
		// Erase it.  For some reason, expat isn't ignoring the BOM as it should.
		cbRead -= 3;
		memmove(pch, pch + 3, cbRead);
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the language definition from the XML file stored on the disk.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinitionFactory::InitializeFromXml(ILgWritingSystemFactory * pwsf,
	BSTR bstrIcuLocale, LanguageDefinition ** ppld)
{
	AssertPtrN(ppld);
	if (!ppld)
		return E_POINTER;
	*ppld = NULL;
	AssertPtrN(pwsf);
	if (!pwsf)
		return E_INVALIDARG;
	AssertBstrN(bstrIcuLocale);
	if (!BstrLen(bstrIcuLocale))
		return E_INVALIDARG;

	// Build the filename from the ICU locale string.
	StrUni stuFile(DirectoryFinder::FwRootDataDir());
	stuFile.FormatAppend(L"\\Languages\\%b.xml", bstrIcuLocale);
	StrAnsi staFile(stuFile);
	StrAnsi staFmt;
	StrAnsi sta;

	FwXmlImportData xid;
	const XML_Char * pszWritingSystem = NULL;
	try
	{
		xid.m_stabpLog.Format("%S\\Temp\\%B-Import.log",
			DirectoryFinder::FwRootDataDir().Chars(), bstrIcuLocale);
		fopen_s(&xid.m_pfileLog, xid.m_stabpLog.Chars(), "w");

		FILE * fp;
		if (fopen_s(&fp, staFile.Chars(), "rb"))
		{
			// Try another location before giving up: some standard writing systems have
			// default values stored over in Templates for bootstrapping.  Copy the file over
			// and make it writable.
			StrUni stuFile2(DirectoryFinder::FwRootCodeDir());
			stuFile2.FormatAppend(L"\\Templates\\%b.xml", bstrIcuLocale);
			if (::CopyFileW(stuFile2.Chars(), stuFile.Chars(), TRUE))
			{
				DWORD dwAttrs = ::GetFileAttributesW(stuFile.Chars());
				if (dwAttrs & FILE_ATTRIBUTE_READONLY)
					::SetFileAttributesW(stuFile.Chars(), dwAttrs & ~FILE_ATTRIBUTE_READONLY);
				fopen_s(&fp, staFile.Chars(), "rb");
			}
			if (!fp)
			{
				// "Cannot open language definition file ""%<0>s""!?"
				staFmt.Load(kstidLangDefXmlMsg009);
				sta.Format(staFmt.Chars(), staFile.Chars());
				xid.LogMessage(sta.Chars());
				xid.m_fError = true;
				return E_FAIL;
			}
			xid.m_stabpFile = staFile.Chars();
		}

		ulong cbFile = _filelength(_fileno(fp));
		if (cbFile == (ulong)-1L)
		{
			// "Error accessing language definition file ""%<0>s""!?"
			staFmt.Load(kstidLangDefXmlMsg010);
			sta.Format(staFmt.Chars(), xid.m_stabpFile.Chars());
			xid.LogMessage(sta.Chars());
			xid.m_fError = true;
			fclose(fp);
			return E_FAIL;
		}
		//  Create a parser to scan over the file to create the basic objects/ids.
		xid.m_parser = XML_ParserCreate(pszWritingSystem);
		xid.m_startOuterHandler = FwXmlImportData::HandleStartTag;
		xid.m_endOuterHandler = FwXmlImportData::HandleEndTag;
		void * pBuffer = XML_GetBuffer(xid.m_parser, cbFile);
		if (!pBuffer)
		{
			fclose(fp);
			// "Out of memory before parsing anything!"
			sta.Load(kstidXmlErrorMsg095);
			xid.LogMessage(sta.Chars());
			xid.m_fError = true;
			return E_OUTOFMEMORY;
		}
		ulong cbRead = fread(pBuffer, 1, cbFile, fp);
		fclose(fp);
		if (cbRead != cbFile)
		{
			// "Error accessing language definition file ""%<0>s""!?"
			staFmt.Load(kstidLangDefXmlMsg010);
			sta.Format(staFmt.Chars(), xid.m_stabpFile.Chars());
			xid.LogMessage(sta.Chars());
			xid.m_fError = true;
			return E_FAIL;
		}
		CheckForBOM(pBuffer, cbRead);

		// Build hashmap of ICU Locales to writing system ids from the LgWritingSystemFactory
		// we received.
		int cws;
		CheckHr(pwsf->get_NumberOfWs(&cws));
		Vector<int> vws;
		vws.Resize(cws);
		CheckHr(pwsf->GetWritingSystems(vws.Begin(), cws));
		for (int iws = 0; iws < cws; ++iws)
		{
			SmartBstr sbstr;
			CheckHr(pwsf->GetStrFromWs(vws[iws], &sbstr));
			StrAnsi staWs(sbstr.Chars());
			xid.m_hmcws.Insert(staWs.Chars(), vws[iws]);
		}
		XML_SetUserData(xid.m_parser, &xid);
		if (!XML_SetBase(xid.m_parser, staFile.Chars()))
		{
			XML_ParserFree(xid.m_parser);
			xid.m_parser = 0;
			// "Out of memory before parsing anything!"
			sta.Load(kstidXmlErrorMsg095);
			xid.LogMessage(sta.Chars());
			xid.m_fError = true;
			return E_OUTOFMEMORY;
		}
		XML_SetElementHandler(xid.m_parser, FwXmlImportData::HandleStartTag,
			FwXmlImportData::HandleEndTag);
		XML_SetCharacterDataHandler(xid.m_parser, FwXml::HandleCharData);

		if (!XML_ParseBuffer(xid.m_parser, cbRead, true))
		{
			// "XML parser detected an XML syntax error!"
			sta.Load(kstidLangDefXmlMsg011);
			xid.LogMessage(sta.Chars());
			xid.m_fError = true;
			return E_FAIL;
		}
		if (xid.m_fError)
		{
			// "Error detected while parsing XML file"
			sta.Load(kstidLangDefXmlMsg012);
			xid.LogMessage(sta.Chars());
			xid.m_fError = true;
			return E_FAIL;
		}
		// Successfully processed the XML file.
		Assert(xid.m_celemEnd <= xid.m_celemStart);
		if (xid.m_celemStart != xid.m_celemEnd)
		{
			// "Error detected while parsing XML file"
			sta.Load(kstidLangDefXmlMsg012);
			xid.LogMessage(sta.Chars());
			xid.m_fError = true;
			return E_FAIL;
		}
		// Turn the collected data into a language definition object.
		// It helps to be a friend!
		m_qld.Create();
		m_qld->m_qws = xid.m_qws;
		for (int ipua = 0; ipua < xid.m_vcdPua.Size(); ++ipua)
			m_qld->m_vcdPua.Push(xid.m_vcdPua[ipua]);
		for (int istu = 0; istu < xid.m_vstuFonts.Size(); ++istu)
			m_qld->m_vstuFonts.Push(xid.m_vstuFonts[istu]);
		m_qld->m_stuEncConvInstall = xid.m_stuEncConvInstall;
		m_qld->m_stuEncConvFile = xid.m_stuEncConvFile;
		m_qld->m_stuKeyboard = xid.m_stuKeyboard;
		m_qld->m_stuBaseLocale = xid.m_stuBaseLocale;
		m_qld->m_stuCollationElements = xid.m_stuCollationElements;
		m_qld->m_stuEthnoCode = xid.m_stuEthnoCode;
		m_qld->m_stuLocaleScript = xid.m_stuLocaleScript;
		m_qld->m_stuLocaleCountry = xid.m_stuLocaleCountry;
		m_qld->m_stuLocaleName = xid.m_stuLocaleName;
		m_qld->m_stuLocaleResources = xid.m_stuLocaleResources;
		m_qld->m_stuLocaleVariant = xid.m_stuLocaleVariant;
		m_qld->m_stuLocaleWinLCID = xid.m_stuLocaleWinLCID;
		m_qld->m_stuNewLocale = xid.m_stuNewLocale;
		*ppld = m_qld;
		(*ppld)->AddRef();
	}
	catch (...)
	{
		// "Error detected while parsing XML file"
		sta.Load(kstidLangDefXmlMsg012);
		xid.LogMessage(sta.Chars());
		xid.m_fError = true;
		return E_FAIL;
	}
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Retrieve the language definition from the factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinitionFactory::get_LanguageDefinition(LanguageDefinition ** ppld)
{
	AssertPtrN(ppld);
	if (!ppld)
		return E_POINTER;

	*ppld = m_qld;
	if (*ppld)
		(*ppld)->AddRef();

	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Store the language definition in the factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinitionFactory::put_LanguageDefinition(LanguageDefinition * pld)
{
	AssertPtrN(pld);

	m_qld = pld;

	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Uninstall the given language definition, delete that language definition file, and remove
	any references to it from other language definition files.
----------------------------------------------------------------------------------------------*/
void LanguageDefinitionFactory::RemoveLanguage(BSTR bstrIcuLocale)
{
	// Build the filename from the ICU locale string.
	StrApp strLocale(bstrIcuLocale);

	StrApp strCmd(ModuleEntry::GetModulePathName());
	int iSlash = strCmd.ReverseFindCh('\\');
	Assert(iSlash > 0);
	strCmd.Replace(iSlash + 1, strCmd.Length(), _T("InstallLanguage.exe"));
	strCmd.FormatAppend(_T(" -r \"%s\""), strLocale.Chars());
	StrUtil::RestartIcu();		// Re-initialize ICU.			// Get rid of any memory-mapping of files by ICU.

	DWORD dwRes;
	bool fOk;
	fOk = SilUtil::ExecCmd(strCmd.Chars(), true, true, &dwRes);

	StrUtil::InitIcuDataDir();		// Re-initialize ICU.

	StrUni stuFile;
	StrUni stuLangDir(DirectoryFinder::FwRootDataDir());
	stuLangDir.Append(L"\\Languages");
	stuFile.Format(L"%s\\%s.xml", stuLangDir.Chars(), bstrIcuLocale);
	::DeleteFileW(stuFile.Chars());		// needed in case it's a builtin language/locale

	// Process each of the remaining language definition XML files to remove any references to
	// the deleted writing system.
	stuFile.Format(L"%s\\*.xml", stuLangDir.Chars());
	StrApp strFile(stuFile);
	WIN32_FIND_DATA ffd;
	HANDLE hFind = ::FindFirstFile(strFile.Chars(), &ffd);
	if (hFind == INVALID_HANDLE_VALUE)
		return;
	do
	{
		strFile.Assign(stuLangDir);
		strFile.FormatAppend(_T("\\%s"), ffd.cFileName);
		RemoveWsReferences(strFile.Chars(), bstrIcuLocale);
	} while (::FindNextFile(hFind, &ffd));
	::FindClose(hFind);
}


/*----------------------------------------------------------------------------------------------
	Hold the data for parsing the XML files and removing a given writing system.
	Hungarian: rwxd.
----------------------------------------------------------------------------------------------*/
struct RemoveWsXmlData
{
	bool m_fDirty;			// flag that we did remove some data from this file.
	bool m_fIgnore;			// flag to ignore char data in this element.
	StrAnsi m_staXml;		// reconstructed XML data (minus removed ws data).
	StrAnsi m_staWs;		// The writing system that we're removing references to.
	StrAnsi m_staElem;		// The element we're deleting.
	XML_Parser m_parser;	// The open Expat XML parser.

	// constructor (allows the object to be on the stack).
	RemoveWsXmlData()
	{
		m_fIgnore = false;
		m_fDirty = false;
	}
	static void HandleStartTag(void * pvUser, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	static void HandleEndTag(void * pvUser, const XML_Char * pszName);
	static void HandleCharData(void * pvUser, const XML_Char * prgch, int cch);
};


/*----------------------------------------------------------------------------------------------
	Append the given characters to the StrAnsi object, but convert the magic XML chars < > and &
	to character references before storing.
----------------------------------------------------------------------------------------------*/
static void AppendXmlUtf8(StrAnsi & staXml, const char * prgch, int cch)
{
	for (int ich = 0; ich < cch; ++ich)
	{
		switch (prgch[ich])
		{
		case '<':
			staXml.Append("&lt;");
			break;
		case '>':
			staXml.Append("&gt;");
			break;
		case '&':
			staXml.Append("&amp;");
			break;
		default:
			staXml.Append(&prgch[ich], 1);
			break;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Handle XML start elements.
	This function is passed to the expat XML parser as a callback function.
----------------------------------------------------------------------------------------------*/
void RemoveWsXmlData::HandleStartTag(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	RemoveWsXmlData * prwxd = reinterpret_cast<RemoveWsXmlData *>(pvUser);
	AssertPtr(prwxd);

	if (prwxd->m_fIgnore)
		return;			// <Run> inside deleted <Astr>

	const char * pszAtt;
	const char * pszVal;
	if (!strcmp(pszName, "AStr") || !strcmp(pszName, "AUni") || !strcmp(pszName, "Run"))
	{
		pszVal = FwXml::GetAttributeValue(prgpszAtts, "ws");
		if (prwxd->m_staWs == pszVal)
		{
			prwxd->m_fIgnore = true;
			prwxd->m_fDirty = true;
			prwxd->m_staElem = pszName;		// remember the deleted element.
			return;
		}
	}
	prwxd->m_staXml.FormatAppend("<%s", pszName);
	for (int i = 0; prgpszAtts[i]; i += 2)
	{
		pszAtt = prgpszAtts[i];
		pszVal = prgpszAtts[i+1];
		AssertPsz(pszAtt);
		AssertPsz(pszVal);
		prwxd->m_staXml.FormatAppend(" %s=\"", pszAtt);
		AppendXmlUtf8(prwxd->m_staXml, pszVal, strlen(pszVal));
		prwxd->m_staXml.Append("\"");
	}
	prwxd->m_staXml.Append(">");
}

/*----------------------------------------------------------------------------------------------
	Handle XML end elements.
	This function is passed to the expat XML parser as a callback function.
----------------------------------------------------------------------------------------------*/
void RemoveWsXmlData::HandleEndTag(void * pvUser, const XML_Char * pszName)
{
	RemoveWsXmlData * prwxd = reinterpret_cast<RemoveWsXmlData *>(pvUser);
	AssertPtr(prwxd);

	if (prwxd->m_fIgnore)
	{
		if (prwxd->m_staElem == pszName)
		{
			prwxd->m_fIgnore = false;
			prwxd->m_staElem.Clear();
		}
		return;
	}
	prwxd->m_staXml.FormatAppend("</%s>", pszName);
}

/*----------------------------------------------------------------------------------------------
	Handle XML character data. This static method is passed to the expat XML parser as a
	callback function.  See the comments in xmlparse.h for the XML_CharacterDataHandler typedef
	for the documentation such as it is.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param prgch Pointer to an array of character data; not NUL-terminated.
	@param cch Number of characters (bytes) in prgch.
----------------------------------------------------------------------------------------------*/
void RemoveWsXmlData::HandleCharData(void * pvUser, const XML_Char * prgch, int cch)
{
	RemoveWsXmlData * prwxd = reinterpret_cast<RemoveWsXmlData *>(pvUser);
	AssertPtr(prwxd);

	if (prwxd->m_fIgnore)
		return;

	AppendXmlUtf8(prwxd->m_staXml, prgch, cch);
}


/*----------------------------------------------------------------------------------------------
	Remove any references to the given writing system (ICU Locale) from the given language
	definition file.
----------------------------------------------------------------------------------------------*/
void LanguageDefinitionFactory::RemoveWsReferences(const achar * pszFile, BSTR bstrIcuLocale)
{
	// AStr, AUni, Run are the only elements of interest.  Everything else passes through as is.
/*
 <AStr ws="...">					remove element if ws = bstrIcuLocale
 <AUni ws="...">					remove element if ws = bstrIcuLocale
 <Run ws="..." wsBase="..." ...>	remove element if ws = bstrIcuLocale,
									remove attribute if wsBase = bstrIcuLocale
 */
	RemoveWsXmlData rwxd;
	const XML_Char * pszWritingSystem = NULL;
	try
	{
		FILE * fp;
		if (_tfopen_s(&fp, pszFile, _T("rb")))
			return;
		ulong cbFile = _filelength(_fileno(fp));
		if (cbFile == (ulong)-1L)
		{
			fclose(fp);
			return;
		}
		Vector<char> vch;
		try
		{
			vch.Resize(cbFile);
		}
		catch (...)
		{
			// Out of memory??
			fclose(fp);
			return;
		}
		//  Create a parser to scan over the file to create the basic objects/ids.
		rwxd.m_parser = XML_ParserCreate(pszWritingSystem);
		ulong cbRead = fread(vch.Begin(), 1, cbFile, fp);
		fclose(fp);
		fp = NULL;
		if (cbRead != cbFile)
			return;

		rwxd.m_staWs.Assign(bstrIcuLocale);
		XML_SetUserData(rwxd.m_parser, &rwxd);
		XML_SetElementHandler(rwxd.m_parser, RemoveWsXmlData::HandleStartTag,
			RemoveWsXmlData::HandleEndTag);
		XML_SetCharacterDataHandler(rwxd.m_parser, RemoveWsXmlData::HandleCharData);
		int nRet = XML_Parse(rwxd.m_parser, vch.Begin(), cbRead, true);
		if (!nRet)
		{
			// XML parser itself detected an error!
			StrAnsi staFile(pszFile);
			StrAnsi staErr;
			FwXml::XmlErrorDetails(rwxd.m_parser, staFile.Chars(), staErr);
			::OutputDebugStringA(staErr.Chars());
			return;
		}
	}
	catch (...)
	{
		// Error detected while parsing XML file
		return;
	}
	if (rwxd.m_fDirty)
	{
		// Something changed, so dump the revised XML file and install the ICU Locale again.
		FILE * fp;
		_tfopen_s(&fp, pszFile, _T("w"));
		AssertPtr(fp);
		if (!fp)
			return;
		fputs("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
			"<!DOCTYPE LanguageDefinition SYSTEM \"LanguageDefinition.dtd\">\n", fp);
		fputs(rwxd.m_staXml.Chars(), fp);
		fputs("\n", fp);	// Final trailing whitespace is not processed by the char handler.
		fclose(fp);

		StrApp strCmd(ModuleEntry::GetModulePathName());
		int iSlash = strCmd.ReverseFindCh('\\');
		Assert(iSlash > 0);
		strCmd.Replace(iSlash + 1, strCmd.Length(), _T("InstallLanguage.exe"));
		// Argument needs to be surrounded by double quotes to cover spaces in path.
		// TODO: DanH: Consider if this should also have the "-c" argument for installing PUA data
		//       This should be reviewed in the future.
		strCmd.FormatAppend(_T(" -i \"%s\""), pszFile);

		StrUtil::RestartIcu(); // Re-initialize ICU.				// Get rid of any memory-mapping of files by ICU.

		DWORD dwRes;
		bool fOk;
		fOk = SilUtil::ExecCmd(strCmd.Chars(), true, true, &dwRes);

		StrUtil::InitIcuDataDir();		// Re-initialize ICU.
	}
}



//:>********************************************************************************************
//:>	LanguageDefinition methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
LanguageDefinition::LanguageDefinition()
{
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
LanguageDefinition::~LanguageDefinition()
{
}


/*----------------------------------------------------------------------------------------------
	Retrieve the writing system from the language definition object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_WritingSystem(IWritingSystem ** ppws)
{
	AssertPtrN(ppws);
	if (!ppws)
		return E_POINTER;

	*ppws = m_qws;
	if (*ppws)
		(*ppws)->AddRef();
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Store the writing system in the language definition object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::put_WritingSystem(IWritingSystem * pws)
{
	AssertPtrN(pws);

	m_qws = pws;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the ICU base locale.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_BaseLocale(BSTR * pbstr)
{
	AssertPtrN(pbstr);
	if (!pbstr)
		return E_POINTER;

	m_stuBaseLocale.GetBstr(pbstr);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Store the ICU base locale.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::put_BaseLocale(BSTR bstr)
{
	AssertBstrN(bstr);

	m_stuBaseLocale = bstr;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the Ethnologue code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_EthnoCode(BSTR * pbstr)
{
	AssertPtrN(pbstr);
	if (!pbstr)
		return E_POINTER;

	m_stuEthnoCode.GetBstr(pbstr);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Store the Ethnologue code.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::put_EthnoCode(BSTR bstr)
{
	AssertBstrN(bstr);

	m_stuEthnoCode = bstr;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the ICU locale name
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_LocaleName(BSTR * pbstr)
{
	AssertPtrN(pbstr);
	if (!pbstr)
		return E_POINTER;

	m_stuLocaleName.GetBstr(pbstr);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Store the ICU locale name
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::put_LocaleName(BSTR bstr)
{
	AssertBstrN(bstr);

	m_stuLocaleName = bstr;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the ICU locale script
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_LocaleScript(BSTR * pbstr)
{
	AssertPtrN(pbstr);
	if (!pbstr)
		return E_POINTER;

	m_stuLocaleScript.GetBstr(pbstr);

	return S_OK;
}
/*----------------------------------------------------------------------------------------------
	Retrieve the ICU locale country
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_LocaleCountry(BSTR * pbstr)
{
	AssertPtrN(pbstr);
	if (!pbstr)
		return E_POINTER;

	m_stuLocaleCountry.GetBstr(pbstr);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Store the ICU locale script
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::put_LocaleScript(BSTR bstr)
{
	AssertBstrN(bstr);

	m_stuLocaleScript = bstr;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Store the ICU locale country
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::put_LocaleCountry(BSTR bstr)
{
	AssertBstrN(bstr);

	m_stuLocaleCountry = bstr;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the ICU locale variant
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_LocaleVariant(BSTR * pbstr)
{
	AssertPtrN(pbstr);
	if (!pbstr)
		return E_POINTER;

	m_stuLocaleVariant.GetBstr(pbstr);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Store the ICU locale variant
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::put_LocaleVariant(BSTR bstr)
{
	AssertBstrN(bstr);

	m_stuLocaleVariant = bstr;
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Gets the displayable name for the ICU code, built from LocaleName, LocaleScript,
	LocaleCountry, and LocaleVariant.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_DisplayName(BSTR * pbstr)
{
	AssertPtrN(pbstr);
	if (!pbstr)
		return E_POINTER;
	*pbstr = NULL;

	if (!m_stuLocaleName.Length())
		return E_UNEXPECTED;

	StrUni stu(m_stuLocaleScript);
	if (stu.Length() && m_stuLocaleCountry.Length())
		stu.Append(L", ");
	stu.Append(m_stuLocaleCountry);
	if (stu.Length() && m_stuLocaleVariant.Length())
		stu.Append(L", ");
	stu.Append(m_stuLocaleVariant);
	if (stu.Length())
	{
		StrUni stuT;
		stuT.Format(L"%s (%s)", m_stuLocaleName.Chars(), stu.Chars());
		stuT.GetBstr(pbstr);
	}
	else
	{
		m_stuLocaleName.GetBstr(pbstr);
	}
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve collation elements
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_CollationElements(BSTR * pbstr)
{
	AssertPtrN(pbstr);
	if (!pbstr)
		return E_POINTER;

	m_stuCollationElements.GetBstr(pbstr);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Store collation elements
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::put_CollationElements(BSTR bstr)
{
	AssertBstrN(bstr);

	m_stuCollationElements = bstr;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve locale resources
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_LocaleResources(BSTR * pbstr)
{
	AssertPtrN(pbstr);
	if (!pbstr)
		return E_POINTER;

	m_stuLocaleResources.GetBstr(pbstr);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Store locale resources
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::put_LocaleResources(BSTR bstr)
{
	AssertBstrN(bstr);

	m_stuLocaleResources = bstr;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the number of PUA definitions
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_PuaDefinitionCount(int * pcPUA)
{
	AssertPtrN(pcPUA);
	if (!pcPUA)
		return E_POINTER;

	*pcPUA = m_vcdPua.Size();

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve a PUA character definition.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::GetPuaDefinition(int i, int * pnCode, BSTR * pbstrData)
{
	AssertPtrN(pnCode);
	if (!pnCode)
		return E_POINTER;
	*pnCode = 0;
	AssertPtrN(pbstrData);
	if (!pbstrData)
		return E_POINTER;
	*pbstrData = 0;
	if ((unsigned)i >= (unsigned)m_vcdPua.Size())
		return E_INVALIDARG;

	*pnCode = m_vcdPua[i].m_code;
	m_vcdPua[i].m_stuData.GetBstr(pbstrData);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Update a PUA character definition.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::UpdatePuaDefinition(int i, int nCode, BSTR bstrData)
{
	AssertBstrN(bstrData);
	if (!BstrLen(bstrData))
		return E_INVALIDARG;
	if ((unsigned)i >= (unsigned)m_vcdPua.Size())
		return E_INVALIDARG;
	// TODO: validate nCode for being in one of the Private Use Areas.
	// TODO: verify that nCode is not already existing in m_vcdPua.

	m_vcdPua[i].m_code = nCode;
	m_vcdPua[i].m_stuData = bstrData;

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Add a PUA character definition.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::AddPuaDefinition(int nCode, BSTR bstrData)
{
	AssertBstrN(bstrData);
	if (!BstrLen(bstrData))
		return E_INVALIDARG;
	// TODO: validate nCode for being in one of the Private Use Areas.
	// TODO: verify that nCode is not already existing in m_vcdPua.

	CharDef cd;
	cd.m_code = nCode;
	cd.m_stuData = bstrData;
	m_vcdPua.Push(cd);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Remove a PUA character definition.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::RemovePuaDefinition(int i)
{
	if ((unsigned)i >= (unsigned)m_vcdPua.Size())
		return E_INVALIDARG;

	m_vcdPua.Delete(i);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the number of fonts.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_FontCount(int * pcFont)
{
	AssertPtrN(pcFont);
	if (!pcFont)
		return E_POINTER;

	*pcFont = m_vstuFonts.Size();

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve a Font
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::GetFont(int i, BSTR * pbstrFilename)
{
	AssertPtrN(pbstrFilename);
	if (!pbstrFilename)
		return E_POINTER;
	if ((unsigned)i >= (unsigned)m_vstuFonts.Size())
		return E_INVALIDARG;

	m_vstuFonts[i].GetBstr(pbstrFilename);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Updates a font
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::UpdateFont(int i, BSTR bstrFilename)
{
	AssertBstrN(bstrFilename);
	if (!BstrLen(bstrFilename))
		return E_INVALIDARG;
	if ((unsigned)i >= (unsigned)m_vstuFonts.Size())
		return E_INVALIDARG;

	m_vstuFonts[i] = bstrFilename;

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Adds a font
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::AddFont(BSTR bstrFilename)
{
	AssertBstrN(bstrFilename);
	if (!BstrLen(bstrFilename))
		return E_INVALIDARG;

	StrUni stu(bstrFilename);
	m_vstuFonts.Push(stu);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Removes a font
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::RemoveFont(int i)
{
	if ((unsigned)i >= (unsigned)m_vstuFonts.Size())
		return E_INVALIDARG;

	m_vstuFonts.Delete(i);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the keyman keyboard
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_Keyboard(BSTR * pbstr)
{
	AssertPtrN(pbstr);
	if (!pbstr)
		return E_POINTER;

	m_stuKeyboard.GetBstr(pbstr);

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Store the keyman keyboard
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::put_Keyboard(BSTR bstr)
{
	AssertBstrN(bstr);

	m_stuKeyboard = bstr;

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the encoding converter
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::GetEncodingConverter(BSTR * pbstrInstall, BSTR * pbstrFile)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Sets the encoding converter
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::SetEncodingConverter(BSTR bstrInstall, BSTR bstrFile)
{
	AssertBstrN(bstrInstall);
	AssertBstrN(bstrFile);

	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the number of collations
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::get_CollationCount(int * pcColl)
{
	AssertPtrN(pcColl);
	if (!pcColl)
		return E_POINTER;

	if (m_qws)
	{
		return m_qws->get_CollationCount(pcColl);
	}
	else
	{
		*pcColl = 0;
		return S_OK;
	}
}

/*----------------------------------------------------------------------------------------------
	Retrieve a collation
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::GetCollation(int i, ICollation ** ppcoll)
{
	AssertPtrN(ppcoll);
	if (!ppcoll)
		return E_POINTER;
	if (m_qws)
	{
		return m_qws->get_Collation(i, ppcoll);
	}
	else
	{
		*ppcoll = 0;
		return E_INVALIDARG;
	}
}

/*----------------------------------------------------------------------------------------------
	Serializes this LanguageDefinition to an XML file.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::Serialize()
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Save in writing system factory and to database, if any.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LanguageDefinition::SaveWritingSystem(BSTR bstrOldIcuLocale)
{
	AssertBstrN(bstrOldIcuLocale);

	return E_NOTIMPL;
}


// Implement the FwXml methods for our FwXmlImportData struct.
#include "../Cellar/FwXmlString.cpp"

// Explicit instantiation.
#include "Vector_i.cpp"
#include "HashMap_i.cpp"
//#include "Set_i.cpp"

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg.bat"
// End: (These 4 lines are useful to Steve McConnel.)
