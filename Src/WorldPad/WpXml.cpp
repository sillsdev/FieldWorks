/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: WpXml.cpp
Responsibility: Sharon Correll
Last reviewed: never.

	This file contains the XML import/export methods for WorldPad.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "../Cellar/FwXml.h"
#include "Vector_i.cpp"
#include "HashMap_i.cpp"
#include "Set_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

//#define LOG_SQL 1

//:End Ignore


//:>********************************************************************************************
//:>	XML EXPORT.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Save the document to the given XML file.

	@param bstrFile				- name of the file
	@param pwpwnd				- main window containing the page setup information
----------------------------------------------------------------------------------------------*/
HRESULT WpDa::SaveXml(BSTR bstrFile, WpMainWnd * pwpwnd, bool fDtd)
{
	AssertPtrN(bstrFile);
	if (!bstrFile)
		ReturnHr(E_INVALIDARG);

	StrApp strWp(kstidAppName);

	AfMainWndPtr qafwTop = AfApp::Papp()->GetCurMainWnd();

	HRESULT hr;
	try
	{
		// Open the output file.
		IStreamPtr qstrm;
		FileStream::Create(bstrFile, STGM_WRITE | STGM_CREATE, &qstrm);
		SaveXmlToStream(qstrm, pwpwnd, fDtd);
		hr = S_OK;
	}
	catch (Throwable & thr)
	{
		StrApp strRes(kstidCantSaveFile);
		StrApp strDiag(thr.Message());
		if (!strDiag.Length())
			strDiag.Load(kstidFileErrUnknown);
		StrApp strMsg;
		strMsg.Format(strRes, strDiag.Chars());
		::MessageBox(qafwTop->Hwnd(), strMsg.Chars(), strWp.Chars(), MB_ICONEXCLAMATION);
		return E_FAIL;
	}
	catch (...)
	{
		StrApp strRes(kstidCantSaveFile);
		StrApp strDiag(kstidFileErrUnknown);
		StrApp strMsg;
		strMsg.Format(strRes, strDiag.Chars());
		::MessageBox(qafwTop->Hwnd(), strMsg.Chars(), strWp.Chars(), MB_ICONEXCLAMATION);
		return E_FAIL;
	}
	return hr;
}

/*----------------------------------------------------------------------------------------------
	Save the contents to a stream.
----------------------------------------------------------------------------------------------*/
void WpDa::SaveXmlToStream(IStream * pstrm, WpMainWnd * pwpwnd, bool fDtd)
{
	// Write the XML header information.
	FormatToStream(pstrm, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>%n");
	if (fDtd)
		FormatToStream(pstrm, "<!DOCTYPE WpDoc SYSTEM \"WorldPad.dtd\">%n");
	FormatToStream(pstrm, "<WpDoc wpxVersion=\"2.0\">%n%n");

	// List of encodings that are actually used by the data.
	Set<int> setws;

	WriteXmlLanguages(pstrm, setws);
	WriteXmlStyles(pstrm, pwpwnd->GetStylesheet(), setws);
	WriteXmlBody(pstrm);
	WriteXmlPageSetup(pstrm, pwpwnd);

	FormatToStream(pstrm, "</WpDoc>%n");
}

/*----------------------------------------------------------------------------------------------
	Write the list of defined encodings in XML format to the given stream.
----------------------------------------------------------------------------------------------*/
void WpDa::WriteXmlLanguages(IStream * pstrm, Set<int> & setws)
{
	//	Get a list of encodings that are actually used by the data.
	GetUsedEncodings(setws);

	FormatToStream(pstrm, "<Languages>%n");

	SetUpWsFactory();

	int cws;
	CheckHr(m_qwsf->get_NumberOfWs(&cws));
	int * prgenc = NewObj int[cws];
	CheckHr(m_qwsf->GetWritingSystems(prgenc, cws));

	for (int iws = 0; iws < cws; iws++)
	{
		if (setws.IsMember(prgenc[iws]))
			WriteXmlEncoding(pstrm, m_qwsf, prgenc[iws]);
	}

	delete[] prgenc;

	FormatToStream(pstrm, "</Languages>%n%n");
}

/*----------------------------------------------------------------------------------------------
	Generate a string containing the XML description of the given writing system.
----------------------------------------------------------------------------------------------*/
void WpDa::GenerateXmlForEncoding(int iws, StrAnsi * psta)
{
	SetUpWsFactory();

	StrAnsiStream * pstas;
	StrAnsiStream::Create(&pstas);
	IStreamPtr qstrm;
	pstas->QueryInterface(IID_IStream, (void **)&qstrm);

	WriteXmlEncoding(qstrm, m_qwsf, iws);

	*psta = pstas->m_sta;

	pstas->Release();
}

/*----------------------------------------------------------------------------------------------
	Write an writing system in XML format to the given stream.
----------------------------------------------------------------------------------------------*/
void WpDa::WriteXmlEncoding(IStream * pstrm, ILgWritingSystemFactory * pwsf, int ws)
{
	IWritingSystemPtr qws;
	SmartBstr sbstr;
	CheckHr(pwsf->GetStrFromWs(ws, &sbstr));
	if (sbstr)
	{
		CheckHr(pwsf->get_Engine(sbstr, &qws));
		if (qws)
			CheckHr(qws->WriteAsXml(pstrm, 2));
	}
}

/*----------------------------------------------------------------------------------------------
	Generate a list of encodings that are actually used by the data.
----------------------------------------------------------------------------------------------*/
void WpDa::GetUsedEncodings(Set<int> & setws)
{
	int ctss;
	CheckHr(get_VecSize(khvoText, kflidStText_Paragraphs, &ctss));

	for (int itss = 0; itss < ctss; itss++)
	{
		HVO hvoPara;
		CheckHr(get_VecItem(khvoText, kflidStText_Paragraphs, itss, &hvoPara));
		ITsStringPtr qtss;
		CheckHr(get_StringProp(hvoPara, kflidStTxtPara_Contents, &qtss));
		// Look at each run.
		int crun;
		CheckHr(qtss->get_RunCount(&crun));

		for (int irun = 0; irun < crun; ++irun)
		{
			ITsTextPropsPtr qttp;
			TsRunInfo tri;
			int ws, nVar;
			CheckHr(qtss->FetchRunInfo(irun, &tri, &qttp));
			CheckHr(qttp->GetIntPropValues(ktptWs, &nVar, &ws));

			if (!setws.IsMember(ws))
				setws.Insert(ws);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Write the defined styles in XML format to the given stream.
	NOTE: This must be kept in sync with AfExportDlg::Export().
----------------------------------------------------------------------------------------------*/
void WpDa::WriteXmlStyles(IStream * pstrm, AfStylesheet * pasts, Set<int> & setws)
{
	SetUpWsFactory();

	FormatToStream(pstrm, "<Styles>%n");

	HvoClsidVec & vhcStyles = pasts->GetStyles();
	for (int ist = 0; ist < vhcStyles.Size(); ist++)
	{
		FormatToStream(pstrm, "  <StStyle>%n");
		HVO hvoStyle = vhcStyles[ist].hvo;

		SmartBstr sbstrName;
		CheckHr(get_UnicodeProp(hvoStyle, kflidStStyle_Name, &sbstrName));
		FormatToStream(pstrm, "    <Name17><Uni>");
		WriteXmlUnicode(pstrm, sbstrName.Chars(), sbstrName.Length());
		FormatToStream(pstrm, "</Uni></Name17>%n");

		int nType;
		CheckHr(get_IntProp(hvoStyle, kflidStStyle_Type, &nType));
		FormatToStream(pstrm, "    <Type17><Integer val=\"%d\"/></Type17>%n", nType);

		SmartBstr sbstrBasedOn;
		HVO hvoBasedOn;
		CheckHr(get_ObjectProp(hvoStyle, kflidStStyle_BasedOn, &hvoBasedOn));
		CheckHr(get_UnicodeProp(hvoBasedOn, kflidStStyle_Name, &sbstrBasedOn));
		FormatToStream(pstrm, "    <BasedOn17><Uni>");
		WriteXmlUnicode(pstrm, sbstrBasedOn.Chars(), sbstrBasedOn.Length());
		FormatToStream(pstrm, "</Uni></BasedOn17>%n");

		SmartBstr sbstrNext;
		HVO hvoNext;
		CheckHr(get_ObjectProp(hvoStyle, kflidStStyle_Next, &hvoNext));
		CheckHr(get_UnicodeProp(hvoNext, kflidStStyle_Name, &sbstrNext));
		FormatToStream(pstrm, "    <Next17><Uni>");
		WriteXmlUnicode(pstrm, sbstrNext.Chars(), sbstrNext.Length());
		FormatToStream(pstrm, "</Uni></Next17>%n");

		IUnknownPtr qunkTtp;
		CheckHr(get_UnknownProp(hvoStyle, kflidStStyle_Rules, &qunkTtp));
		ITsTextPropsPtr qttp;
		CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));
		FormatToStream(pstrm, "    <Rules17>%n");
		qttp->WriteAsXml(pstrm, m_qwsf, 6);
		FormatToStream(pstrm, "    </Rules17>%n");

		FormatToStream(pstrm, "  </StStyle>%n");
	}

	FormatToStream(pstrm, "</Styles>%n%n");
}

/*----------------------------------------------------------------------------------------------
	Write the contents of the document in XML format to the given stream.
----------------------------------------------------------------------------------------------*/
void WpDa::WriteXmlBody(IStream * pstrm)
{
	SetUpWsFactory();

	int nDocRtl = DocRightToLeft();
	FormatToStream(pstrm, "<Body docRightToLeft=\"");
	if (nDocRtl == 0)
		FormatToStream(pstrm, "false");
	else
		FormatToStream(pstrm, "true");
	FormatToStream(pstrm, "\">%n");

	int ctss;
	CheckHr(get_VecSize(1, kflidStText_Paragraphs, &ctss));

	for (int itss = 0; itss < ctss; itss++)
	{
		FormatToStream(pstrm, "  <StTxtPara>%n");

		HVO hvoPara;
		CheckHr(get_VecItem(khvoText, kflidStText_Paragraphs, itss, &hvoPara));
		IUnknownPtr qunkTtp;
		CheckHr(get_UnknownProp(hvoPara, kflidStPara_StyleRules, &qunkTtp));
		ITsTextPropsPtr qttp;
		CheckHr(qunkTtp->QueryInterface(IID_ITsTextProps, (void **) &qttp));
		if (qttp)
		{
			FormatToStream(pstrm, "    <StyleRules15>%n");
			qttp->WriteAsXml(pstrm, m_qwsf, 6);
			FormatToStream(pstrm, "    </StyleRules15>%n");
		}

		FormatToStream(pstrm, "    <Contents16>%n");
		ITsStringPtr qtss;
		CheckHr(get_StringProp(hvoPara, kflidStTxtPara_Contents, &qtss));
		qtss->WriteAsXml(pstrm, m_qwsf, 6, 0, FALSE);
		FormatToStream(pstrm, "    </Contents16>%n");

		FormatToStream(pstrm, "  </StTxtPara>%n");
	}

	FormatToStream(pstrm, "</Body>%n%n");
}

/*----------------------------------------------------------------------------------------------
	Write the page setup information from the window in XML format onto the given stream.
----------------------------------------------------------------------------------------------*/
void WpDa::WriteXmlPageSetup(IStream * pstrm, WpMainWnd * pwpwnd)
{
	SetUpWsFactory();

	FormatToStream(pstrm, "<PageSetup>%n");
	FormatToStream(pstrm, "  <PageInfo>%n");

	FormatToStream(pstrm, "    <TopMargin9999><Integer val=\"%d\"/></TopMargin9999>%n",
		pwpwnd->TopMargin());
	FormatToStream(pstrm, "    <BottomMargin9999><Integer val=\"%d\"/></BottomMargin9999>%n",
		pwpwnd->BottomMargin());
	FormatToStream(pstrm, "    <LeftMargin9999><Integer val=\"%d\"/></LeftMargin9999>%n",
		pwpwnd->LeftMargin());
	FormatToStream(pstrm, "    <RightMargin9999><Integer val=\"%d\"/></RightMargin9999>%n",
		pwpwnd->RightMargin());
	FormatToStream(pstrm, "    <HeaderMargin9999><Integer val=\"%d\"/></HeaderMargin9999>%n",
		pwpwnd->HeaderMargin());
	FormatToStream(pstrm, "    <FooterMargin9999><Integer val=\"%d\"/></FooterMargin9999>%n",
		pwpwnd->FooterMargin());
	FormatToStream(pstrm, "    <PageSize9999><Integer val=\"%d\"/></PageSize9999>%n",
		pwpwnd->PageSize());
	FormatToStream(pstrm, "    <PageHeight9999><Integer val=\"%d\"/></PageHeight9999>%n",
		pwpwnd->PageHeight());
	FormatToStream(pstrm, "    <PageWidth9999><Integer val=\"%d\"/></PageWidth9999>%n",
		pwpwnd->PageWidth());
	FormatToStream(pstrm,
		"    <PageOrientation9999><Integer val=\"%d\"/></PageOrientation9999>%n",
		pwpwnd->PageOrientation());

	ITsStringPtr qtss = pwpwnd->PageHeader();
	if (qtss)
	{
		FormatToStream(pstrm, "    <Header9999>%n");
		qtss->WriteAsXml(pstrm, m_qwsf, 6, 0, FALSE);
		FormatToStream(pstrm, "    </Header9999>%n");
	}
	qtss = pwpwnd->PageFooter();
	if (qtss)
	{
		FormatToStream(pstrm, "    <Footer9999>%n");
		qtss->WriteAsXml(pstrm, m_qwsf, 6, 0, FALSE);
		FormatToStream(pstrm, "    </Footer9999>%n");
	}

	//	TODO SharonC: add header/footer font information

	FormatToStream(pstrm, "  </PageInfo>%n");
	FormatToStream(pstrm, "</PageSetup>%n%n");
}

//:>********************************************************************************************
//:>	XML IMPORT.
//:>********************************************************************************************

#define READ_SIZE 16384

/*----------------------------------------------------------------------------------------------
	Distinguish among the various fundamental kinds of XML elements.
	Hungarian: elty
----------------------------------------------------------------------------------------------*/
typedef enum
{
	keltyDoc = 1,
	keltySection,
	keltyObject,
	keltyPropName,
	keltyBasicProp,
	keltyBad = 0
} ElementType;

/*----------------------------------------------------------------------------------------------
	Store the element type and other basic information for a particular XML tag.
	A stack of these is maintained during parsing, one for each open XML element.
	Hungarian: eti
----------------------------------------------------------------------------------------------*/
struct ElemTypeInfo
{
	ElementType m_elty;
	bool m_fSeq;	// for keltyPropName, true if this is a sequence
	union
	{
		int m_wps;		// for keltySection, section constant
		int m_wpc;		// For keltyObject, class constant
		int m_wpf;		// For keltyPropName, field constant
		int m_cpt;		// For keltyBasicProp, a more exact type.
	};
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
	This serves two purposes: a placeholder for the XML parser callback functions, to make it
	easier for all of them to be "friends" of FwXmlData, and a convenient grouping of the
	various data items used throughout XML import.
	Modelled after SteveM's FwXmlImportData.

	Hungarian: xid
----------------------------------------------------------------------------------------------*/
struct WpXmlImportData
{
	// Constructor and Destructor.
	WpXmlImportData(WpDa * pda, WpMainWnd * pwpwnd, WpMainWnd * pwpwndLauncher,
		WpStylesheet * pwpsts, bool fOverwriteEnc);
	~WpXmlImportData();

	// Other methods.
	void CreateErrorMessage(const char * pszFmt, const char * pszArg = NULL);
	void LogMessage(const char * pszMsg);
	int FinalErrorMessage();
	void AbortErrorMessage();
	void DeleteEmptyLogFile();
	ElemTypeInfo GetElementType(const char * pszElement);

	// Handler (callback) functions for the XML parser.  These must be static methods.

	static int HandleExternalEntityRef(XML_Parser parser, const XML_Char * pszContext,
		const XML_Char * pszBase, const XML_Char * pszSystemId, const XML_Char * pszPublicId);
	static void HandleStartTag(void * pvUser, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	static void HandleEndTag(void * pvUser, const XML_Char * pszName);
	static void HandleStringStartTag(void * pvUser, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	static void HandleStringEndTag(void * pvUser, const XML_Char * pszName);
	static void HandleCharData(void * pvUser, const XML_Char * prgch, int cch);
	static void HandleCharDataAux(void * pvUser, const XML_Char * prgch, int cch);
	static void HandleOldStringData(void * pvUser, const XML_Char * prgch, int cch);
	static bool HandleTextPropStartTag(WpXmlImportData * pxid, const XML_Char * pszName,
		const XML_Char ** prgpszAtts, ITsPropsBldr * ptbp);

	void CreatePropErrorMsg(int stid, const char * pszProp, const char * pszVal = NULL,
		const char * pszProp2 = NULL);

	static StrUni ReadXmlBinaryHexString(WpXmlImportData * pxid, const char * pszHex,
		StrAnsi staAttName);

	static void CheckVersion(WpXmlImportData * pxid, const XML_Char ** prgpszAtts);

	static bool CheckValidField(WpXmlImportData * pxid, int wpc, int wpf);
	static bool CheckValidBasicType(WpXmlImportData * pxid, int wpc, int wpf, int cpt);

	static void SetIntegerField(WpXmlImportData * pxid, int nVal);
	static void SetStringField(WpXmlImportData * pxid, ITsString * ptss);
	static void SetUnicodeField(WpXmlImportData * pxid, BSTR bstr);
	static void SetMultiStringField(WpXmlImportData * pxid, int ws, ITsString * ptss);
	static void SetMultiUnicodeField(WpXmlImportData * pxid, int ws, BSTR bstr);
	static void AddWsStyles(void * pvTextProp, Vector<void *> & vpvWsStyles,
		void ** ppvModified);
	static void AddBulNumFontInfo(void * pvTextProp, Vector<void *> & vpvWsStyles,
		void ** ppvModified);
	static void MakeProp(OLECHAR * & pch, int tpt, int nVar, int nVal, int & cprop);

	// Data used by the handler functions.

	bool m_fOverwriteWs;
	bool m_fError;
	int m_cErrMsg;
	HRESULT m_hr;
	WpDaPtr m_qda;
	WpMainWndPtr m_qwpwnd;			// main window, for storing page-setup information
	WpMainWndPtr m_qwpwndTop;		// current visible window, for giving modal error messages
	WpStylesheetPtr m_qwpsts;
	IStreamPtr m_qstrm;
	StrAnsiBufPath m_stabpFile;		// Used for log file messages.
	StrAnsiBufPath m_stabpLog;
	FILE * m_pfileLog;
	XML_Parser m_parser;
	unsigned m_celemStart;			// Bookkeeping.
	unsigned m_celemEnd;
	bool m_fInString;
	bool m_fBetaXML;		// Flag that we have old-fashioned strings (starts off true).
	bool m_fPropSeen;		// Flag that we have <Prop> inside <Str> (old-fashioned string).
	bool m_fInRun;			// Flag that we're inside <Run> inside <Str> (new-fashioned string).
	bool m_fInUnicode;
	HVO m_hvoText;
	HVO m_hvoNextPara;
	HVO m_hvoNextStyle;
	StrUni m_stuChars;				// Character data for <Uni>
	int m_wsMulti;					// Nonzero for <AStr ws="XXX"> and <AUni ws="XXX">

	bool m_fInOldEncoding;	// WPX version 1

	HashMapChars<ElemTypeInfo> m_hmceti;

	HashMapChars<int> m_hmcws;			// Map writing system string to writing system integer.

	Vector<ElemTypeInfo> m_vetiOpen;	// Stack of currently open XML elements.
	Vector<void *> m_vpvOpen;			// Stack of open objects or HVOs
	Vector<void *> m_vpvClosed;			// Stack of closed objects, waiting to be stored in the
										// appropriate property.
	Vector<int> m_vwpcClosed;			// Class constants for closed objects
	Vector<SeqPropInfo> m_vspiOpen;		// Stack of currently open properties.

	Vector<void *> m_vpvNewWs;			// newly created encodings, whose renderers need
										// to be initalized

	HashMap<HVO, StrUni> m_hmhvostuNextStyle;
	HashMap<HVO, StrUni> m_hmhvostuBasedOn;

	//	For constructing strings:
	ITsStrBldrPtr m_qtsb;
	ITsPropsBldrPtr m_qtpbStr;
	int m_wsUser;
	// True if we should report when trying to load an alternative of a multistring that is
	// not a known writing system.
	bool m_fReportMissingWs;
	// True if, during parsing, we skipped setting an alternative because we could not find a ws.
	bool m_fSkippedMissingWs;
};

/*----------------------------------------------------------------------------------------------
	This data structure stores an XML tag and its associated section type for the sections
	that are valid within WorldPad.
	Hungarian: secel
----------------------------------------------------------------------------------------------*/
struct SectionElem
{
	const char * m_pszName;
	int m_wps;
};

enum {
	kwpsDoc = 1,
	kwpsLang,
	kwpsStyles,
	kwpsBody,
	kwpsPageSetup,
};

// These must be lexically ordered as by strcmp.
static SectionElem g_rgsecel[] =
{
	{ "Body",			kwpsBody },
	{ "Languages",		kwpsLang },
	{ "PageSetup",		kwpsPageSetup },
	{ "Styles",			kwpsStyles },
	{ "WpDoc",			kwpsDoc },
};
static const int g_csecel = isizeof(g_rgsecel) / isizeof(SectionElem);

/*----------------------------------------------------------------------------------------------
	Check whether this element is a valid section name. If so, return an appropriate
	section type value. Otherwise, return -1.
----------------------------------------------------------------------------------------------*/
static inline int ValidSection(const char * pszName)
{
	int iMin = 0;
	int iLim = g_csecel;
	int iMid;
	int ncmp;
	// Perform a binary search
	while (iMin < iLim)
	{
		iMid = (iMin + iLim) >> 1;
		ncmp = strcmp(g_rgsecel[iMid].m_pszName, pszName);
		if (ncmp == 0)
			return g_rgsecel[iMid].m_wps;
		else if (ncmp < 0)
			iMin = iMid + 1;
		else
			iLim = iMid;
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	This data structure stores an XML tag and its associated class type for the classes that
	are valid within WorldPad.
	Hungarian: clel
----------------------------------------------------------------------------------------------*/
struct ClassElem
{
	const char * m_pszName;
	int m_wpc;
};

enum {
	//	language
	kwpcWs = 1,
	kwpcOws,
	kwpcColl,
	//	page setup
	kwpcPageInfo,
	//	body
	kwpcStyle,
	kwpcPara,
	kwpcTextProp,
	kwpcWsProp,

	kwpcBulNumFontInfo,	// semi-bogus value: BulNumFontInfo is half-Class, half-PropName

	kwpcOldEnc, // WPX version 1
};

// These must be lexically ordered as by strcmp.

static ClassElem g_rgclel[] =
{
	{ "LgCollation",			kwpcColl },
	{ "LgEncoding",				kwpcOldEnc },   // WPX version 1
	{ "LgOldWritingSystem",		kwpcOws },
	{ "LgWritingSystem",		kwpcWs },
	{ "PageInfo",				kwpcPageInfo },
	{ "Prop",					kwpcTextProp },
	{ "StStyle",				kwpcStyle },
	{ "StTxtPara",				kwpcPara },
	{ "WsProp",					kwpcWsProp },
};
static const int g_cclel = isizeof(g_rgclel) / isizeof(ClassElem);

/*----------------------------------------------------------------------------------------------
	Check whether this element is a valid class name. If so, return an appropriate
	class type value.
	Otherwise, return -1.
----------------------------------------------------------------------------------------------*/
static inline int ValidClass(const char * pszName)
{
	int iMin = 0;
	int iLim = g_cclel;
	int iMid;
	int ncmp;
	// Perform a binary search
	while (iMin < iLim)
	{
		iMid = (iMin + iLim) >> 1;
		ncmp = strcmp(g_rgclel[iMid].m_pszName, pszName);
		if (ncmp == 0)
			return g_rgclel[iMid].m_wpc;
		else if (ncmp < 0)
			iMin = iMid + 1;
		else
			iLim = iMid;
	}
	return -1;
}
/*----------------------------------------------------------------------------------------------
	This data structure stores an XML tag and its associated field type for the fields that
	are valid within WorldPad.
	NOTE:  The numerical parts of the writing system, old writing system and collation property names
	(24, 25, and 30) must match those defined by the FieldWorks conceptual model.
	Hungarian: fel
----------------------------------------------------------------------------------------------*/
struct FieldElem
{
	const char * m_pszName;
	int m_wpf;
	bool m_fSeq;
};

enum {
	kwpfName = 1,
	// language/old writing system/collation
	kwpfOldWritingSystems,
	kwpfDescr,
	kwpfRendererType,
	kwpfDefSerif,
	kwpfDefSans,
	kwpfDefBodyFont,
	kwpfDefMono,
	kwpfRightToLeft,
	kwpfVertical,
	kwpfFontVar,
	kwpfSansFontVar,
	kwpfBodyFontFeatures, // Features = Var. TODO: data migration to change variation to features to be consistent with the UI.
	kwpfKeyboardType,
	kwpfKeymanKeyboard,
	kwpfLangId,
	kwpfLastModified,
	kwpfKeyManCtrl,
	kwpfAbbr,
	kwpfCapitalizationInfo,
	kwpfCharPropOverrides,
	kwpfCode,
	kwpfCollations,
	kwpfICULocale,
	kwpfICURules,
	kwpfIcuResourceName,
	kwpfIcuResourceText,
	kwpfLocale,
	kwpfLegacyMap,
	kwpfMatchedPairs,
	kwpfPunctuationPatterns,
	kwpfQuotationMarks,
	kwpfWinCollation,
	kwpfWinLCID,
	kwpfRenderer,
	kwpfRendererInit,
	kwpfWritingSystem,
	kwpfValidChars,
	kwpfSpellCheckDictionary,
	//	styles
	kwpfWsStyles,
	kwpfBulNumFontInfo,
	//	body
	kwpfContents,
	kwpfStyleRules,
	kwpfBasedOn,
	kwpfNext,
	kwpfType,
	kwpfRules,
	// The next few are not used by WorldPad, but possibly produced by Data Notebook.
	kwpfParaLabel,
	kwpfStyleName,

	//	page setup
	kwpfTopMargin,
	kwpfBottomMargin,
	kwpfLeftMargin,
	kwpfRightMargin,
	kwpfHeaderMargin,
	kwpfFooterMargin,
	kwpfPageSize,
	kwpfPageHeight,
	kwpfPageWidth,
	kwpfPageOrientation,
	kwpfHeader,
	kwpfFooter,
	kwpfHeaderFont,
};

// These must be lexically ordered as by strcmp.
static FieldElem g_rgfel[] = {
/*
OLD  -> NEW
1002 ->  15
1003 ->  16
1004 ->  17
6002 ->  24
6003 ->  25
 */
//	  XML Element Name			enum value				seq?
	{ "Abbr24",					kwpfAbbr,				false },
	{ "Abbr25",					kwpfAbbr,				false },
	{ "BasedOn1004",			kwpfBasedOn,			false },
	{ "BasedOn17",				kwpfBasedOn,			false },
	{ "BodyFontFeatures24",		kwpfBodyFontFeatures,	false },
	{ "BottomMargin9999",		kwpfBottomMargin,		false },
	{ "BulNumFontInfo",			kwpfBulNumFontInfo,		false },
	{ "CapitalizationInfo24",			kwpfCapitalizationInfo,	false },
	{ "CharPropOverrides25",	kwpfCharPropOverrides,  false },
	{ "Code25",					kwpfCode,				false },
	{ "Code30",					kwpfCode,				false },
	{ "Collations24",			kwpfCollations,			true  },
	{ "Collations25",			kwpfCollations,			true  },
	{ "Contents1003",			kwpfContents,			false },
	{ "Contents16",				kwpfContents,			false },
	{ "DefaultBodyFont24",		kwpfDefBodyFont,		false },
	{ "DefaultMonospace24",		kwpfDefMono,			false },
	{ "DefaultMonospace25",		kwpfDefMono,			false },
	{ "DefaultMonospace6003",	kwpfDefMono,			false },
	{ "DefaultSansSerif24",		kwpfDefSans,			false },
	{ "DefaultSansSerif25",		kwpfDefSans,			false },
	{ "DefaultSansSerif6003",	kwpfDefSans,			false },
	{ "DefaultSerif24",			kwpfDefSerif,			false },
	{ "DefaultSerif25",			kwpfDefSerif,			false },
	{ "DefaultSerif6003",		kwpfDefSerif,			false },
	{ "Description24",			kwpfDescr,				false },
	{ "Description25",			kwpfDescr,				false },
	{ "Description6003",		kwpfDescr,				false },
	{ "FontVariation24",		kwpfFontVar,			false },
	{ "FontVariation25",		kwpfFontVar,			false },
	{ "FontVariation6003",		kwpfFontVar,			false },
	{ "Footer9999",				kwpfFooter,				false },
	{ "FooterMargin9999",		kwpfFooterMargin,		false },
	{ "Header9999",				kwpfHeader,				false },
	{ "HeaderFont9999",			kwpfHeaderFont,			false },
	{ "HeaderMargin9999",		kwpfHeaderMargin,		false },
	{ "ICULocale24",			kwpfICULocale,			false },
	{ "ICURules30",				kwpfICURules,			false },
	{ "IcuResourceName30",		kwpfIcuResourceName,	false },
	{ "IcuResourceText30",		kwpfIcuResourceText,	false },
	{ "KeyManControl25",		kwpfKeyManCtrl,			false },
	{ "KeyManControl6003",		kwpfKeyManCtrl,			false },
	{ "KeyboardType24",			kwpfKeyboardType,		false },
	{ "KeyboardType25",			kwpfKeyboardType,		false },
	{ "KeyboardType6003",		kwpfKeyboardType,		false },
	{ "KeymanKeyboard24",		kwpfKeymanKeyboard,		false },
	{ "Label1003",				kwpfParaLabel,			false },
	{ "Label16",				kwpfParaLabel,			false },
	{ "LangId25",				kwpfLangId,				false },
	{ "LangId6003",				kwpfLangId,				false },
	{ "LastModified24",			kwpfLastModified,		false },
	{ "LeftMargin9999",			kwpfLeftMargin,			false },
	{ "LegacyMapping24",		kwpfLegacyMap,			false },
	{ "Locale24",				kwpfLocale,				false },
	{ "Locale25",				kwpfLocale,				false },
	{ "MatchedPairs24",		kwpfMatchedPairs,		false },
	{ "Name1004",				kwpfName,				false },
	{ "Name17",					kwpfName,				false },
	{ "Name24",					kwpfName,				false },
	{ "Name25",					kwpfName,				false },
	{ "Name30",					kwpfName,				false },
	{ "Name6002",				kwpfName,				false },
	{ "Name6003",				kwpfName,				false },
	{ "Next1004",				kwpfNext,				false },
	{ "Next17",					kwpfNext,				false },
	{ "OldWritingSystems24",	kwpfOldWritingSystems,	true },
	{ "OldWritingSystems6002",	kwpfOldWritingSystems,	true },
	{ "PageHeight9999",			kwpfPageHeight,			false },
	{ "PageOrientation9999",	kwpfPageOrientation,	false },
	{ "PageSize9999",			kwpfPageSize,			false },
	{ "PageWidth9999",			kwpfPageWidth,			false },
	{ "PunctuationPatterns24",	kwpfPunctuationPatterns,	false },
	{ "QuotationMarks24",		kwpfQuotationMarks,		false },
	{ "Renderer24",				kwpfRenderer,			false },
	{ "RendererInit24",			kwpfRendererInit,		false },
	{ "RendererType24",			kwpfRendererType,		false },
	{ "RendererType25",			kwpfRendererType,		false },
	{ "RendererType6003",		kwpfRendererType,		false },
	{ "RightMargin9999",		kwpfRightMargin,		false },
	{ "RightToLeft24",			kwpfRightToLeft,		false },
	{ "RightToLeft25",			kwpfRightToLeft,		false },
	{ "RightToLeft6003",		kwpfRightToLeft,		false },
	{ "Rules1004",				kwpfRules,				false },
	{ "Rules17",				kwpfRules,				false },
	{ "SansFontVariation24",	kwpfSansFontVar,		false },
	{ "SpellCheckDictionary24",	kwpfSpellCheckDictionary,false },
	{ "StyleName1002",			kwpfStyleName,			false },
	{ "StyleName15",			kwpfStyleName,			false },
	{ "StyleRules1002",			kwpfStyleRules,			false },
	{ "StyleRules15",			kwpfStyleRules,			false },
	{ "TopMargin9999",			kwpfTopMargin,			false },
	{ "Type1004",				kwpfType,				false },
	{ "Type17",					kwpfType,				false },
	{ "ValidChars24",			kwpfValidChars,			false },
	{ "Vertical25",				kwpfVertical,			false },
	{ "Vertical6003",			kwpfVertical,			false },
	{ "WinCollation30",			kwpfWinCollation,		false },
	{ "WinLCID30",				kwpfWinLCID,			false },
	{ "WritingSystem24",		kwpfWritingSystem,		false },
	{ "WritingSystems24",		kwpfOldWritingSystems,	true }, // WPX version 1
	{ "WritingSystems6002",		kwpfOldWritingSystems,	true }, // WPX version 1
	{ "WsStyles9999",			kwpfWsStyles,			true }
};
static const int g_cfel = isizeof(g_rgfel) / isizeof(FieldElem);

/*----------------------------------------------------------------------------------------------
	Check whether this element is a valid field. If so, return an appropriate field type value.
	Otherwise, return -1.
----------------------------------------------------------------------------------------------*/
static inline int ValidField(const char * pszName, bool * pfSeq)
{
	int iMin = 0;
	int iLim = g_cfel;
	int iMid;
	int ncmp;
	// Perform a binary search
	while (iMin < iLim)
	{
		iMid = (iMin + iLim) >> 1;
		ncmp = strcmp(g_rgfel[iMid].m_pszName, pszName);
		if (ncmp == 0)
		{
			*pfSeq = g_rgfel[iMid].m_fSeq;
			if (g_rgfel[iMid].m_wpf == kwpfName || g_rgfel[iMid].m_wpf == kwpfDescr)
			{
				int x; x = 3;
			}
			return g_rgfel[iMid].m_wpf;
		}
		else if (ncmp < 0)
			iMin = iMid + 1;
		else
			iLim = iMid;
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Resolve the pszSystemId path.  If pszSystemId contains an absolute path, then use it
	verbatim. Otherwise, append pszSystemId to the directory path part of pszBase.
----------------------------------------------------------------------------------------------*/
static void ResolvePath(const char * pszBase, const char * pszSystemId,
	StrAnsiBufPath & stabpFile)
{
	if (!pszBase ||
		(*pszSystemId == '\\') ||
		(isascii(pszSystemId[0]) && isalpha(pszSystemId[0]) && (pszSystemId[1] == ':')) ||
		(*pszSystemId == '/'))
	{
		stabpFile.Assign(pszSystemId);
	}
	else
	{
		stabpFile.Assign(pszBase);
		int ich1 = stabpFile.ReverseFindCh('/');
		int ich2 = stabpFile.ReverseFindCh('\\');
		if (ich1 == -1)
		{
			if (ich2 == -1)
			{
				// Neither a / or \ in pszBase: it must be a plain filename.
				stabpFile.Clear();
			}
			else
			{
				// One or more \'s in pszBase: truncate to the last one.
				stabpFile.SetLength(ich2 + 1);
			}
		}
		else if (ich2 == -1)
		{
			// One or more /'s in pszBase: truncate to the last one.
			stabpFile.SetLength(ich1 + 1);
		}
		else
		{
			// Both / and \ in pszBase: truncate to the last one.
			if (ich1 < ich2)
				stabpFile.SetLength(ich2 + 1);
			else
				stabpFile.SetLength(ich1 + 1);
		}
		stabpFile.Append(pszSystemId);
	}
}

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
WpXmlImportData::WpXmlImportData(WpDa * pda, WpMainWnd * pwpwnd, WpMainWnd * pwpwndLauncher,
	WpStylesheet * pwpsts, bool fOverwriteWs)
	:	m_qda(pda),
		m_qwpwnd(pwpwnd),
		m_qwpwndTop(pwpwndLauncher),
		m_qwpsts(pwpsts)
{
	m_fOverwriteWs = fOverwriteWs;
	m_pfileLog = NULL;
	m_parser = 0;
	m_celemStart = 0;
	m_celemEnd = 0;
	m_fError = false;
	m_cErrMsg = 0;
	m_hr = S_OK;
	m_fInString = false;
	m_fInRun = false;
	m_fPropSeen = false;
	m_fBetaXML = true;			// Turns false at first <Run> inside <Str> or <AStr>.
	m_fInUnicode = false;
	m_wsMulti = 0;
	m_hvoText = khvoText;
	m_hvoNextPara = khvoParaMin;
	m_hvoNextStyle = ((pwpsts) ? pwpsts->NextStyleHVO() : khvoStyleMin);
	m_qtsb = NULL;
	m_qtpbStr = NULL;
	m_wsUser = 0;
	m_fReportMissingWs = true;
	m_fSkippedMissingWs = false;

	m_fInOldEncoding = false;  // WPX version 1
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
WpXmlImportData::~WpXmlImportData()
{
	if (m_pfileLog)
	{
		fclose(m_pfileLog);
		m_pfileLog = NULL;
	}
	if (m_parser)
	{
		XML_ParserFree(m_parser);
		m_parser = 0;
	}
}

/*----------------------------------------------------------------------------------------------
	Give an error message.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::CreateErrorMessage(const char * pszFmt, const char * pszArg)
{
	StrAnsi staWp(kstidAppName);

	StrAnsi staMsg;
	StrAnsi staInfo(pszFmt);
	if (pszArg)
		staInfo.Format(pszFmt, pszArg);

	LogMessage(staInfo.Chars());

	if (m_cErrMsg == 0)
	{
		// "Error reading file %<0>s at line %<1>d:%n%<2>s."
		StrAnsi staRes(kstidWpXmlErrMsg001);
		staMsg.Format(staRes,
			m_stabpFile.Length() ? m_stabpFile.Chars() : "[Stored in Registry]",
			XML_GetCurrentLineNumber(m_parser), staInfo.Chars());
		::MessageBoxA(m_qwpwndTop ? m_qwpwndTop->Hwnd() : NULL, staMsg.Chars(), staWp.Chars(),
			MB_OK | MB_ICONEXCLAMATION | MB_TOPMOST);	// don't hide behind splash screen!
	}
//	else if (m_cErrMsg == 1)
//	{
//		// "See the following file for a complete list of errors:%n%<0>s"
//		StrAnsi staRes(kstidWpXmlErrMsg002);
//		staMsg.Format(staRes, m_stabpLog.Chars());
//		::MessageBox(m_qwpwndTop->Hwnd(), staMsg.Chars(), staWp.Chars(),
//			MB_OK | MB_ICONINFORMATION);
//	}

	m_cErrMsg++;
}

/*----------------------------------------------------------------------------------------------
	Write a message to the log file (if one is open).
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::LogMessage(const char * pszMsg)
{
	AssertPsz(pszMsg);
	if (m_pfileLog)
	{
		StrAnsi staMsg(pszMsg);
		if (m_parser)
		{
			staMsg.Format("%s:%d: %s%n",
				m_stabpFile.Chars(), XML_GetCurrentLineNumber(m_parser), pszMsg);
		}
		fputs(staMsg.Chars(), m_pfileLog);
		fflush(m_pfileLog);
	}
}

/*----------------------------------------------------------------------------------------------
	If any error occurred during loading, give a general message with the option to abort
	the load.
----------------------------------------------------------------------------------------------*/
int WpXmlImportData::FinalErrorMessage()
{
	int nRet = IDOK;
	StrApp strMsg;
	if (m_cErrMsg > 0)
	{
		StrApp strWp(kstidAppName);
		// "This file contains errors that WorldPad cannot interpret. See the%n%s file for a
		// complete list of errors.%n%nWould you like WorldPad to open this file and show as
		// much as it can interpret?",
		StrApp strRes(kstidWpXmlErrMsg034);
		StrApp strFile;
		strFile.Assign(m_stabpLog.Chars());		// FIX ME FOR PROPER CODE CONVERSION!
		strMsg.Format(strRes.Chars(), strFile.Chars());
		nRet = ::MessageBox(m_qwpwndTop->Hwnd(),  strMsg.Chars(),
			strWp.Chars(), MB_ICONEXCLAMATION | MB_OKCANCEL);
	}
	return nRet;
}

/*----------------------------------------------------------------------------------------------
	If an fatal error occurred during loading, give an error message.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::AbortErrorMessage()
{
	StrAnsi staMsg;
	if (m_cErrMsg > 0)
	{
		StrAnsi staWp(kstidAppName);
		// "This file contains errors and cannot be opened. See the%n%s file for a
		// list of errors.",
		StrAnsi staRes(kstidWpXmlErrMsg035);
		staMsg.Format(staRes, m_stabpLog.Chars());
		::MessageBoxA(m_qwpwndTop->Hwnd(), staMsg.Chars(), staWp.Chars(),
			MB_ICONEXCLAMATION | MB_OK);
	}
}

/*----------------------------------------------------------------------------------------------
	Determine the type of XML element we have here.
----------------------------------------------------------------------------------------------*/
ElemTypeInfo WpXmlImportData::GetElementType(const char * pszElement)
{
	AssertPsz(pszElement);
	ElemTypeInfo eti;
	StrAnsi staRes;
	if (m_hmceti.Retrieve(pszElement, &eti))
	{
		return eti;
	}
	int cpt = FwXml::BasicType(pszElement);
	// Don't recognize "FwDatabase" or "Prop" as basic element types.
	if (cpt == kcptNil || cpt == kcptRuleProp)
		cpt = -1;
	if (cpt != -1)
	{
		if (cpt == kcptNil)
		{
			// "Invalid XML Element: unknown tag '%<0>s'"
			staRes.Load(kstidWpXmlErrMsg003);
			CreateErrorMessage(staRes.Chars(), pszElement);
			eti.m_elty = keltyBad;
			eti.m_wpc = -1;
		}
		else
		{
			eti.m_elty = keltyBasicProp;
			eti.m_cpt = cpt;
		}
	}
	else if (strpbrk(pszElement, "0123456789") != NULL || !strcmp(pszElement, "BulNumFontInfo"))
	{
		//	Field name
		bool fSeq;
		int wpf = ValidField(pszElement, &fSeq);
		if (wpf == -1)
		{
			// "Invalid XML Element: unknown field '%<0>s'"
			staRes.Load(kstidWpXmlErrMsg004);
			CreateErrorMessage(staRes.Chars(), pszElement);
			eti.m_elty = keltyBad;
			eti.m_wpf = -1;
			eti.m_fSeq = false;
		}
		else
		{
			eti.m_elty = keltyPropName;
			eti.m_wpf = wpf;
			eti.m_fSeq = fSeq;
		}
	}
	else
	{
		int wpc = ValidClass(pszElement);
		if (wpc == -1)
		{
			int wps = ValidSection(pszElement);
			if (wps == -1)
			{
				// "Invalid XML Element: unknown class '%<0>s'"
				staRes.Load(kstidWpXmlErrMsg005);
				CreateErrorMessage(staRes.Chars(), pszElement);
				eti.m_elty = keltyBad;
				eti.m_wpc = -1;
			}
			else
			{
				switch (wps)
				{
				case kwpsDoc:
					eti.m_elty = keltyDoc;
					break;
				default:
					eti.m_elty = keltySection;
				}
				eti.m_wps = wps;
			}
		}
		else
		{
			eti.m_elty = keltyObject;
			eti.m_wpc = wpc;
		}
	}
	m_hmceti.Insert(pszElement, eti);
	return eti;
}

/*----------------------------------------------------------------------------------------------
	Handle XML external entity references (during any phase). There should not be any of these
	in a WorldPad file.
----------------------------------------------------------------------------------------------*/
int WpXmlImportData::HandleExternalEntityRef(XML_Parser parser, const XML_Char * pszContext,
	const XML_Char * pszBase, const XML_Char * pszSystemId, const XML_Char * pszPublicId)
{
#if 99
	StrAnsiBuf stab;
	stab.Format("pszContext = '%s', pszBase = '%s', pszSystemId = '%s'",
		pszContext ? pszContext : "{NULL}", pszBase ? pszBase : "{NULL}",
		pszSystemId ? pszSystemId : "{NULL}");
	StrAnsiBuf stabTitle("DEBUG WpXmlImportData::HandleExternalEntityRef()");
	::MessageBoxA(NULL, stab.Chars(), stabTitle.Chars(), MB_OK);
#endif
	ThrowHr(WarnHr(E_UNEXPECTED));
	return 0;
}

/*----------------------------------------------------------------------------------------------
	Handle XML start elements.
	This function is passed to the expat XML parser as a callback function.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::HandleStartTag(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	WpXmlImportData * pxid = reinterpret_cast<WpXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);
	++pxid->m_celemStart;
	const char * pszId;
	const char * pszVal;
	int ws;
	StrAnsiBufSmall stabs;
	char * psz;
	ElemTypeInfo eti;
	ElemTypeInfo etiProp;
	ElemTypeInfo etiObject;
	SeqPropInfo spi;

	IWritingSystemPtr qws;
	ICollationPtr qcoll;
	ITsPropsBldrPtr qtpb;
	ITsTextPropsPtr qttp;
	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.

	StrUni stuWs;
	StrUni stuIsoWs;
	StrAnsi staWs;
	StrAnsi staRes;
	StrAnsi staMsg;
	try
	{
		eti = pxid->GetElementType(pszName);
		switch (eti.m_elty)
		{
		case keltyDoc:
			if (pxid->m_vetiOpen.Size())
			{
				// "<WpDoc> must be the outermost XML element"
				staRes.Load(kstidWpXmlErrMsg006);
				pxid->CreateErrorMessage(staRes.Chars());
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			CheckVersion(pxid, prgpszAtts);
			pxid->m_vetiOpen.Push(eti);
			break;

		case keltySection:
			if (pxid->m_vetiOpen.Size() != 1)
			{
				// "<%<0>s> must be nested directly inside <WpDoc>...</WpDoc>"
				staRes.Load(kstidWpXmlErrMsg007);
				pxid->CreateErrorMessage(staRes.Chars(), pszName);
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			pxid->m_vetiOpen.Push(eti);

			if (eti.m_wps == kwpsBody)
			{
				pszVal = FwXml::GetAttributeValue(prgpszAtts, "docRightToLeft");
				if (pszVal)
				{
					// Ignore for now; this information should be duplicated in the Normal
					// style. Eventually, maybe:
					//CheckHr(pxid->m_qda->CacheIntProp(pxid->m_hvoText,kflidStText_RightToLeft,
					//	((strcmp(pszVal, "true") == 0) ? 1 : 0)));
				}
			}
			break;

		case keltyObject:
			if (!pxid->m_vetiOpen.Size())
			{
				// "<%<0>s> must be nested inside <WpDoc>...</WpDoc>"
				staRes.Load(kstidWpXmlErrMsg008);
				pxid->CreateErrorMessage(staRes.Chars(), pszName);
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			if (pxid->m_vetiOpen.Size() < 2)
			{
				// "<%<0>s> must be nested inside of section"
				staRes.Load(kstidWpXmlErrMsg009);
				pxid->CreateErrorMessage(staRes.Chars(), pszName);
				ThrowHr(WarnHr(E_UNEXPECTED));
			}

			//	Create an object of the right class and push it on the stack of
			//	open objects.

			// Handle WPX version 1:
			if (eti.m_wpc == kwpcOldEnc)
			{
				// LgEncoding--treat as WritingSystem
				pxid->m_fInOldEncoding = true;
				eti.m_wpc = kwpcWs;
			}
			else if (eti.m_wpc == kwpcWs)
			{
				if (pxid->m_fInOldEncoding)
					// <LgWritingSystem> inside <LgEncoding>--treat the same as
					// <LgOldWritingSystem> inside <LgWritingSystem>
					eti.m_wpc = kwpcOws;
				else
					pxid->m_fInOldEncoding = false;
			}

			void * pvObject;
			bool fDirtyWs;
			switch (eti.m_wpc)
			{
			case kwpcWs:
				pszId = FwXml::GetAttributeValue(prgpszAtts, "id");
				fDirtyWs = false;
				pvObject = NULL;
				if (pszId && *pszId)
				{
					if (strcmp(pszId, "___") == 0)
					{
						// Don't ever create a writing system with this bogus id value!!
						break;
					}
					// Assume the id is the ICU Locale, since that's what WorldPad writes.
					staWs.Assign(pszId);
					stuWs.Assign(pszId);
					stuIsoWs = SilUtil::ConvertEthnologueToISO(stuWs.Chars());
					if (stuIsoWs != stuWs)
					{
						fDirtyWs = true;
						staWs = stuIsoWs;
						stuWs = stuIsoWs;
					}
					CheckHr(qwsf->GetWsFromStr(stuWs.Bstr(), &ws));
					if (ws)
					{
						CheckHr(qwsf->get_EngineOrNull(ws, &qws));
						AssertPtr(qws);
						// if the writing system already exists, and the overwrite flag is not
						// set, we don't do anything with the information from the XML file.
						// Otherwise, we fill in the rest of the writing system information as
						// we parse the XML.
						if (!pxid->m_fOverwriteWs)
						{
							// Don't overwrite previous definition.
							qws.Clear();
						}
					}
					else
					{
						// Need to create a new writing system.
						CheckHr(qwsf->get_Engine(stuWs.Bstr(), &qws));
						AssertPtr(qws);
						CheckHr(qws->get_WritingSystem(&ws));
						Assert(ws);
						fDirtyWs = true;
					}
					pxid->m_hmcws.Insert(staWs.Chars(), ws, true);
				}
				else
				{
					// Create a totally blank writing system (unattached to the factory) since
					// no id.
					qws.CreateInstance(CLSID_WritingSystem);
					fDirtyWs = true;
				}
				if (qws)
				{
					// Overwrite previous definition, or create a new one.
					// the open-list is responsible for the ref count.
					if (fDirtyWs)
						CheckHr(qws->put_Dirty(TRUE));
					pvObject = (void *)qws.Detach();
				}
				break;

			case kwpcOws:
				// Get a pointer to the WritingSystem allocated for either <LgWritingSystem>
				// or <LgEncoding> earlier.
				pvObject = *pxid->m_vpvOpen.Top();
				if (pvObject)
				{
					IWritingSystem * pws = reinterpret_cast<IWritingSystem *>(pvObject);
					Assert(pws);
					pws->AddRef();
				}
				break;

			case kwpcColl:
				qcoll.CreateInstance(CLSID_Collation);
				// implicit initialization needed for collations.
				CheckHr(qcoll->putref_WritingSystemFactory(qwsf));
				// the open-list is responsible for the ref count.
				pvObject = (void *)qcoll.Detach();
				break;

			case kwpcPageInfo:
				// Get ready to store the page setup information in the window.
				pvObject = (void *)pxid->m_qwpwnd.Ptr();
				break;

			case kwpcStyle:
				// Assign the style object a row in the table.
				pvObject = (void *)pxid->m_hvoNextStyle++;
				break;

			case kwpcPara:
				// Assign the paragraph object a row in the table.
				pvObject = (void *)pxid->m_hvoNextPara++;
				break;

			case kwpcTextProp:
				// Generate a text-properties object and push it as the value of the property.
				// This does not require a valid writing system attribute value.
				qtpb.CreateInstance(CLSID_TsPropsBldr);
				HandleTextPropStartTag(pxid, pszName, prgpszAtts, qtpb);
				qtpb->GetTextProps(&qttp);
				pvObject = (void *)qttp.Detach();
				break;

			case kwpcWsProp:
				// Generate a text-properties object and push it as the value of the property.
				// This requires a valid writing system attribute value.
				qtpb.CreateInstance(CLSID_TsPropsBldr);
				if (HandleTextPropStartTag(pxid, pszName, prgpszAtts, qtpb))
				{
					qtpb->GetTextProps(&qttp);
					pvObject = (void *)qttp.Detach();
				}
				else
				{
					pvObject = NULL;
				}
				break;

			default:
				Assert(false);
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
			// Check for proper nesting.
			if (!pxid->m_vetiOpen.Size())
			{
				// "<%<0>s> must be nested inside <WpDoc>...</WpDoc>"
				staRes.Load(kstidWpXmlErrMsg008);
				pxid->CreateErrorMessage(staRes.Chars(), pszName);
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
			if (etiObject.m_elty != keltyObject)
			{
				// "<%<0>s> must be nested inside an object element"
				staRes.Load(kstidWpXmlErrMsg011);
				pxid->CreateErrorMessage(staRes.Chars(), pszName);
				ThrowHr(WarnHr(E_UNEXPECTED));
			}

			pxid->m_vetiOpen.Push(eti);
			spi.m_fSeq = eti.m_fSeq;
			spi.m_cobj = 0;
			pxid->m_vspiOpen.Push(spi);
			if (eti.m_wpf == kwpfBulNumFontInfo)
			{
				// We have a subset of normal properties allowed.  Reuse existing code.
				qtpb.CreateInstance(CLSID_TsPropsBldr);
				HandleTextPropStartTag(pxid, pszName, prgpszAtts, qtpb);
				qtpb->GetTextProps(&qttp);
				pvObject = (void *)qttp.Detach();
				pxid->m_vpvClosed.Push(pvObject);
				pxid->m_vwpcClosed.Push(kwpcBulNumFontInfo);
				pxid->m_vspiOpen.Top()->m_cobj++;
			}
			break;

		case keltyBasicProp:
			// Check for proper nesting.
			if (!pxid->m_vetiOpen.Size())
			{
				// "<%<0>s> must be nested inside <WpDoc>...</WpDoc>"
				staRes.Load(kstidWpXmlErrMsg008);
				pxid->CreateErrorMessage(staRes.Chars(), pszName);
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			etiProp = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
			if (etiProp.m_elty != keltyPropName)
			{
				// "<%<0>s> must be nested inside an object attribute element"
				staRes.Load(kstidWpXmlErrMsg012);
				pxid->CreateErrorMessage(staRes.Chars(), pszName);
				ThrowHr(WarnHr(E_UNEXPECTED));
			}
			etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 2];
			if (etiObject.m_elty != keltyObject)
			{
				// "<%<0>s> must be nested inside an object element"
				staRes.Load(kstidWpXmlErrMsg011);
				pxid->CreateErrorMessage(staRes.Chars(), pszName);
				ThrowHr(WarnHr(E_UNEXPECTED));
			}

			pxid->m_vetiOpen.Push(eti);

			if (!CheckValidBasicType(pxid, etiObject.m_wpc, etiProp.m_wpf, eti.m_cpt))
			{
				ThrowHr(WarnHr(E_UNEXPECTED));
			}

			switch (eti.m_cpt)
			{
			case kcptBoolean:
				/*
					<!ELEMENT Boolean EMPTY >
					<!ATTLIST Boolean val (true | false) #REQUIRED >
				 */
				pszVal = FwXml::GetAttributeValue(prgpszAtts, "val");
				int nVal;
				if (!pszVal)
				{
					// "Missing val attribute in Boolean element"
					staRes.Load(kstidWpXmlErrMsg013);
					pxid->CreateErrorMessage(staRes.Chars());
					break;
				}
				if (strcmp(pszVal, "true") == 0)
					nVal = 1;
				else if (strcmp(pszVal, "false") == 0)
					nVal = 0;
				else
				{
					// "Invalid Boolean val attribute value: %<0>s"
					staRes.Load(kstidWpXmlErrMsg014);
					pxid->CreateErrorMessage(staRes.Chars(), pszVal);
					break;
				}
				SetIntegerField(pxid, nVal);
				break;

			case kcptInteger:
				/*
					<!ELEMENT Integer EMPTY >
					<!ATTLIST Integer val CDATA #REQUIRED >
				 */
				pszVal = FwXml::GetAttributeValue(prgpszAtts, "val");
				if (!pszVal)
				{
					// "Missing val attribute in Integer element"
					staRes.Load(kstidWpXmlErrMsg015);
					pxid->CreateErrorMessage(staRes.Chars());
					break;
				}
				nVal = static_cast<int>(strtol(pszVal, &psz, 10));
				if (*psz || !*pszVal)
				{
					// "Invalid Integer val attribute value: %<0>s"
					staRes.Load(kstidWpXmlErrMsg016);
					pxid->CreateErrorMessage(staRes.Chars(), pszVal);
					break;
				}
				SetIntegerField(pxid, nVal);
				break;

			case kcptString:				// May actually be kcptBigString.
				/*
					<!ELEMENT Str (#PCDATA | Run)* >
				 */

				// The next tag will be internal to the string.
				XML_SetElementHandler(pxid->m_parser,
					WpXmlImportData::HandleStringStartTag,
					WpXmlImportData::HandleStringEndTag);
				pxid->m_stuChars.Clear();
				pxid->m_fInString = true;
				pxid->m_fInRun = false;
				pxid->m_fPropSeen = false;
				pxid->m_qtsb.CreateInstance(CLSID_TsStrBldr);
				pxid->m_wsMulti = 0;
				break;

			case kcptUnicode:				// May actually be kcptBigUnicode.
				/*
					<!ELEMENT Uni (#PCDATA) >
				 */
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				pxid->m_wsMulti = 0;
				break;

			case kcptMultiString:			// May actually be kcptMultiBigString.
				/*
					<!ELEMENT AStr (#PCDATA | Run)* >
					<!ATTLIST ws CDATA #REQUIRED>
				 */
				// The next tag will be internal to the string.
				XML_SetElementHandler(pxid->m_parser,
					WpXmlImportData::HandleStringStartTag,
					WpXmlImportData::HandleStringEndTag);
				pxid->m_stuChars.Clear();
				pxid->m_fInString = true;
				pxid->m_fInRun = false;
				pxid->m_fPropSeen = false;
				pszVal = FwXml::GetAttributeValue(prgpszAtts, "ws");
				if (!pszVal || !strlen(pszVal) || strcmp(pszVal, "0") == 0)
					pszVal = FwXml::GetAttributeValue(prgpszAtts, "enc"); // WPX version 1.0
				stuWs.Assign(pszVal);
				stuIsoWs.Clear();
				if (stuWs.Length())
					stuIsoWs = SilUtil::ConvertEthnologueToISO(stuWs.Chars());
				if (stuIsoWs.Length())
					CheckHr(qwsf->GetWsFromStr(stuIsoWs.Bstr(), &pxid->m_wsMulti));
				if (strcmp(pszVal, "___") != 0 && !pxid->m_wsMulti)
				{
					if (pxid->m_fReportMissingWs)
					{
						// "Cannot convert '%<0>s' into a Language Writing system code"
						pxid->CreatePropErrorMsg(kstidWpXmlErrMsg032, pszVal);
					}
					else
					{
						pxid->m_fSkippedMissingWs = true;
					}
				}
				pxid->m_qtsb.CreateInstance(CLSID_TsStrBldr);
				break;

			case kcptMultiUnicode:			// May actually be kcptMultiBigUnicode.
				/*
					<!ELEMENT AUni (#PCDATA) >
					<!ATTLIST ws CDATA #REQUIRED>
				 */
				pxid->m_stuChars.Clear();
				pxid->m_fInUnicode = true;
				pszVal = FwXml::GetAttributeValue(prgpszAtts, "ws");
				if (!pszVal || !strlen(pszVal) || strcmp(pszVal, "0") == 0)
					pszVal = FwXml::GetAttributeValue(prgpszAtts, "enc"); // WPX version 1.0
				stuWs.Assign(pszVal);
				stuIsoWs.Clear();
				if (stuWs.Length())
					stuIsoWs = SilUtil::ConvertEthnologueToISO(stuWs.Chars());
				if (stuIsoWs.Length())
					CheckHr(qwsf->GetWsFromStr(stuIsoWs.Bstr(), &pxid->m_wsMulti));
				if (strcmp(pszVal, "___") != 0 && !pxid->m_wsMulti)
				{
					if (pxid->m_fReportMissingWs)
					{
						// "Cannot convert '%<0>s' into a Language Writing system code"
						pxid->CreatePropErrorMsg(kstidWpXmlErrMsg032, pszVal);
					}
					else
					{
						pxid->m_fSkippedMissingWs = true;
					}
				}
				break;

			case kcptNumeric:
			case kcptFloat:
			case kcptReferenceAtom:
			case kcptTime:
			case kcptGuid:
			case kcptGenDate:
				// "Unsuppported XML start tag: '%<0>s'"
				staRes.Load(kstidWpXmlErrMsg017);
				pxid->CreateErrorMessage(staRes.Chars(), pszName);
				break;
			default:
				Assert(false);
			}

			break;

		default:
			// "Unknown XML start tag: '%<0>s'"
			staRes.Load(kstidWpXmlErrMsg018);
			pxid->CreateErrorMessage(staRes.Chars(), pszName);
			ThrowHr(WarnHr(E_UNEXPECTED));
			break;
		}
	}
	catch (Throwable & thr)
	{
		pxid->m_fError = true;
		pxid->m_hr = thr.Error();
#ifdef DEBUG
		staMsg.Format("ERROR CAUGHT on line %d of %s: %s",
			__LINE__, __FILE__, AsciiHresult(pxid->m_hr));
		pxid->LogMessage(staMsg.Chars());
#endif
	}
	catch (...)
	{
		pxid->m_fError = true;
		pxid->m_hr = E_FAIL;
#ifdef DEBUG
		staMsg.Format("UNKNOWN ERROR CAUGHT on line %d of %s", __LINE__, __FILE__);
		pxid->LogMessage(staMsg.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML end elements during the first pass.
	This function is passed to the expat XML parser as a callback function.
	// REVIEW SteveMc: is there anything else to do here?
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::HandleEndTag(void * pvUser, const XML_Char * pszName)
{
	WpXmlImportData * pxid = reinterpret_cast<WpXmlImportData *>(pvUser);
	StrAnsi staRes;
	AssertPtr(pxid);
	Assert(pxid->m_parser);
	++pxid->m_celemEnd;
	ElemTypeInfo eti;
	ElemTypeInfo etiObject;
	Vector<void *> vpvValue;
	Vector<int> vwpcValue;
	void * pvObject;
	ITsTextPropsPtr qttp;
	IUnknown * punk;
	IWritingSystemPtr qws;
	HVO hvoObject;
	int cpv, ipv, cobj, iobj;
	HVO * prghvoPara;
	HVO * prghvoStyles;
	ITsStringPtr qtss, qtssNormalized;
	int iwpc;

	if (!pxid->m_vetiOpen.Pop(&eti))
	{
		// THIS SHOULD NEVER HAPPEN!
		pxid->LogMessage("Unbalanced XML element stack!?");
		pxid->m_fError = true;
		pxid->m_hr = E_UNEXPECTED;
	}
	switch (eti.m_elty)
	{
	case keltyDoc:
		if (pxid->m_vpvClosed.Size())
		{
			pxid->LogMessage("Left-over objects.");
		}
		break;

	case keltySection:
		switch (eti.m_wps)
		{
		case kwpsLang:
			//	The m_vpvClosed list should contain encodings, which should already
			//	be stored in the writing system factory.
			for (iwpc = 0; iwpc < pxid->m_vwpcClosed.Size(); iwpc++)
			{
				Assert(pxid->m_vwpcClosed[iwpc] == kwpcWs);
				// Keep a list of the newly created encodings, so we can initialize
				// their renderers. The reference count from the closed-list is now
				// owned by this list.
				if (pxid->m_vpvClosed[iwpc])
					pxid->m_vpvNewWs.Push(pxid->m_vpvClosed[iwpc]);
				// otherwise it's a null representing an writing system that's already present
			}
			break;
		case kwpsStyles:
			//	The m_vpvClosed list should contain StStyles.
			//	Store them in the database.
			cpv = pxid->m_vpvClosed.Size();
			prghvoStyles = NewObj HVO[cpv];
			for (ipv = 0; ipv < pxid->m_vwpcClosed.Size(); ipv++)
			{
				Assert(pxid->m_vwpcClosed[ipv] == kwpcStyle);
				prghvoStyles[ipv] = (HVO)pxid->m_vpvClosed[ipv];
			}
			pxid->m_qwpsts->AddLoadedStyles(prghvoStyles, cpv, pxid->m_hvoNextStyle);
			pxid->m_qwpsts->FixStyleReferenceAttrs(kflidStStyle_BasedOn,
				pxid->m_hmhvostuBasedOn);
			pxid->m_qwpsts->FixStyleReferenceAttrs(kflidStStyle_Next,
				pxid->m_hmhvostuNextStyle);
			pxid->m_qwpsts->FinalizeStyles();
			delete[] prghvoStyles;
			break;
		case kwpsBody:
			//	The m_vpvClosed list should contain HVOs for StTxtPara objects.
			//	Store them in the text.
			cpv = pxid->m_vpvClosed.Size();
			prghvoPara = NewObj HVO[cpv];
			for (ipv = 0; ipv < cpv; ipv++)
			{
				Assert(pxid->m_vwpcClosed[ipv] == kwpcPara);
				prghvoPara[ipv] = (HVO)pxid->m_vpvClosed[ipv];
			}
			CheckHr(pxid->m_qda->CacheVecProp(pxid->m_hvoText, kflidStText_Paragraphs,
				prghvoPara, cpv));
			delete[] prghvoPara;
			break;
		case kwpsPageSetup:
			//	The page setup information should be properly stored by now.
			//	The m_vpvClosed list should contain exactly one item, representing the window.
			Assert(pxid->m_vwpcClosed.Size() == 1);
			Assert(pxid->m_vwpcClosed[0] == kwpcPageInfo);
			break;
		default:
			Assert(false);
		}

		pxid->m_vpvClosed.Clear();
		pxid->m_vwpcClosed.Clear();
		break;

	case keltyObject:
		void * pv;
		if (!pxid->m_vpvOpen.Pop(&pv))
		{
			// THIS SHOULD NEVER HAPPEN!
			pxid->LogMessage("Unbalanced object stack!?");
			pxid->m_fError = true;
			pxid->m_hr = E_UNEXPECTED;
		}
		// Now the closed-list is responsible for the ref count (if any).
		pxid->m_vpvClosed.Push(pv);
		pxid->m_vwpcClosed.Push(eti.m_wpc);


		if (eti.m_wpc == kwpcWs) // WPX version 1
			pxid->m_fInOldEncoding = false;

		break;

	case keltyPropName:
		SeqPropInfo spi;
		if (!pxid->m_vspiOpen.Pop(&spi))
		{
			// THIS SHOULD NEVER HAPPEN!
			pxid->LogMessage("Unbalanced property name stack!?");
			pxid->m_fError = true;
			pxid->m_hr = E_UNEXPECTED;
		}
		cobj = spi.m_cobj;
		// Pop cobj closed objects off the stack and set the property to the
		// cumulative value.
		vpvValue.Resize(cobj);
		vwpcValue.Resize(cobj);
		for (iobj = 0; iobj < cobj; iobj++)
		{
			void * pv;
			int wpc;
			if (!pxid->m_vpvClosed.Pop(&pv) || !pxid->m_vwpcClosed.Pop(&wpc))
			{
				// THIS SHOULD NEVER HAPPEN!
				pxid->LogMessage("Unbalanced property value stack!?");
				pxid->m_fError = true;
				pxid->m_hr = E_UNEXPECTED;
			}
			vpvValue[cobj - iobj - 1] = pv;
			vwpcValue[cobj - iobj - 1] = wpc;
		}
		if (spi.m_cobj > 1 && !spi.m_fSeq)
		{
			// "Cannot put multiple objects in an atomic property"
			staRes.Load(kstidWpXmlErrMsg019);
			pxid->CreateErrorMessage(staRes);
			pxid->m_fError = true;
			pxid->m_hr = E_UNEXPECTED;
		}

		switch (eti.m_wpf)
		{
		case kwpfOldWritingSystems:
			break;

		case kwpfCollations:
			etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
			Assert(etiObject.m_elty == keltyObject);
			pvObject = *(pxid->m_vpvOpen.Top());
			if (pvObject != NULL)
			{
				IWritingSystemPtr qws;
				punk = reinterpret_cast<IUnknown *>(pvObject);
				AssertPtr(punk);
				CheckHr(punk->QueryInterface(IID_IWritingSystem, (void **)&qws));
				if (qws)
				{
					ICollationPtr qcoll;
					for (ipv = 0; ipv < vpvValue.Size(); ipv++)
					{
						punk = reinterpret_cast<IUnknown *>(vpvValue[ipv]);
						AssertPtr(punk);
						CheckHr(punk->QueryInterface(IID_ICollation, (void **)&qcoll));
						AssertPtr(qcoll.Ptr());
						CheckHr(qws->putref_Collation(ipv, qcoll));
						//  Clear the reference count--originally from the open-list.
						qcoll.Ptr()->Release();
					}
				}
			}
			else
			{
				// If we are parsing an existing ws, we don't have a pvObject, but
				// we still need to clear memory on the Collations.
				ICollationPtr qcoll;
				for (ipv = 0; ipv < vpvValue.Size(); ipv++)
				{
					punk = reinterpret_cast<IUnknown *>(vpvValue[ipv]);
					AssertPtr(punk);
					CheckHr(punk->QueryInterface(IID_ICollation, (void **)&qcoll));
					AssertPtr(qcoll.Ptr());
					//  Clear the reference count--originally from the open-list.
					qcoll.Ptr()->Release();
				}
			}
			break;

		case kwpfRules:	// of an StStyle
		case kwpfStyleRules: // of a paragraph (StTxtPara)
			etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
			Assert(etiObject.m_elty == keltyObject);
			pvObject = *(pxid->m_vpvOpen.Top());
			hvoObject = (HVO)pvObject;
			if (vpvValue.Size() > 0)
			{
				Assert(vpvValue.Size() == 1);
				punk = reinterpret_cast<IUnknown *>(vpvValue[0]);
				AssertPtrN(punk);
				if (punk)
				{
					CheckHr(pxid->m_qda->CacheUnknown(hvoObject,
						eti.m_wpf == kwpfRules ? kflidStStyle_Rules : kflidStPara_StyleRules,
						punk));
					//	Clear the reference count--originally from the open-list.
					punk->Release();
				}
			}
			else if (eti.m_wpf == kwpfRules)
			{
				//	Every style in the stylesheet needs to have an ITsTextProps,
				//	even if it is empty.
				pxid->m_qwpsts->AddEmptyTextProps(hvoObject);
			}
			break;

		case kwpfWsStyles:	// ws sub-styles for a set of text properties
			etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
			Assert(etiObject.m_elty == keltyObject);
			if (vpvValue.Size() > 0)
			{
				//	Store the text properties from vpvValue into the wsStyles field
				//	of the outer text property object. This generates a new
				//	TsTextProps object which needs to replace the original.
				pvObject = *(pxid->m_vpvOpen.Top());
				void * pvModified;
				AddWsStyles(pvObject, vpvValue, &pvModified);
				pxid->m_vpvOpen.Pop();
				pxid->m_vpvOpen.Push(pvModified);
				for (int iv = 0; iv < vpvValue.Size(); iv++)
				{
					// clear the ref count--originally from the open-list
					punk = reinterpret_cast<IUnknown *>(vpvValue[iv]);
					AssertPtrN(punk);
					// could be NULL for obsolete ws="___" alternative
					if (punk)
						punk->Release();
				}
			}
			break;

		case kwpfBulNumFontInfo:
			etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
			Assert(etiObject.m_elty == keltyObject);
			if (vpvValue.Size() > 0)
			{
				Assert(vpvValue.Size() == 1);
				Assert(vwpcValue.Size() == 1 && vwpcValue[0] == kwpcBulNumFontInfo);
				// Store the text properties from vpvValue into the BulNumFontInfo field of the
				// outer text property object.  This generates a new TsTextProps object which
				// replaces the original.
				pvObject = *(pxid->m_vpvOpen.Top());
				void * pvModified;
				AddBulNumFontInfo(pvObject, vpvValue, &pvModified);
				pxid->m_vpvOpen.Pop();
				pxid->m_vpvOpen.Push(pvModified);
				for (int iv = 0; iv < vpvValue.Size(); iv++)
				{
					// clear the ref count--originally from the open-list
					punk = reinterpret_cast<IUnknown *>(vpvValue[iv]);
					AssertPtr(punk);
					punk->Release();
				}
			}
			break;

		case kwpfName:
		case kwpfDescr:
		case kwpfRendererType:
		case kwpfFontVar:
		case kwpfSansFontVar:
		case kwpfBodyFontFeatures:
		case kwpfDefSerif:
		case kwpfDefSans:
		case kwpfDefBodyFont:
		case kwpfDefMono:
		case kwpfKeymanKeyboard:
		case kwpfAbbr:					// <<Abbr25>: Uni
		case kwpfCharPropOverrides:		// <CharPropOverrides25>: Uni
		case kwpfCode:					// <Code25>, <Code30>: Integer
		case kwpfICULocale:				// <ICULocale24>: Uni
		case kwpfICURules:				// <ICURules30>: Uni
		case kwpfIcuResourceName:		// <IcuResourceName30>: Uni
		case kwpfIcuResourceText:		// <IcuResourceText30>: Uni
		case kwpfLegacyMap:				// <LegacyMapping24>: Uni
		case kwpfLocale:				// <Locale25>: Integer
		case kwpfWinCollation:			// <WinCollation30>: Uni
		case kwpfWinLCID:				// <WinLCID30>: Integer
		case kwpfRightToLeft:
		case kwpfVertical:
		case kwpfKeyboardType:
		case kwpfLangId:
		case kwpfKeyManCtrl:
		case kwpfValidChars:			// <ValidChars24>: Uni
		case kwpfSpellCheckDictionary:	// <SpellCheckDictionary24>: Uni
		case kwpfCapitalizationInfo:			// <CapitalizationInfo24>: Uni
		case kwpfLastModified:			// <LastModified24>: Uni
		case kwpfMatchedPairs:			// <MatchedPairs24>: Uni
		case kwpfQuotationMarks:			// <QuotationMarks24>: Uni
		case kwpfPunctuationPatterns:		// <PunctuationPatterns24>: Uni

		case kwpfBasedOn:
		case kwpfNext:
		case kwpfType:
		case kwpfContents:
		case kwpfParaLabel:
		case kwpfStyleName:

		case kwpfTopMargin:
		case kwpfBottomMargin:
		case kwpfLeftMargin:
		case kwpfRightMargin:
		case kwpfHeaderMargin:
		case kwpfFooterMargin:
		case kwpfPageSize:
		case kwpfPageHeight:
		case kwpfPageWidth:
		case kwpfPageOrientation:
		case kwpfHeader:
		case kwpfFooter:
		case kwpfHeaderFont:
			// already handled by keltyBasicProp below
			Assert(vpvValue.Size() == 0);
			break;
		default:
			Assert(false);
		}

		break;

	case keltyBasicProp:
		switch (eti.m_cpt)
		{
		case kcptBoolean:
		case kcptInteger:
		case kcptNumeric:
			break;	// already handled

		case kcptString:
			// Create a TsString from the current string builder.
			Assert(pxid->m_qtsb);
			pxid->m_qtsb->GetString(&qtss);
			qtss->get_NormalizedForm(knmNFD, &qtssNormalized);
			SetStringField(pxid, qtssNormalized);
			pxid->m_fInString = false;
			pxid->m_qtsb.Clear();
			break;

		case kcptUnicode:
			SetUnicodeField(pxid, pxid->m_stuChars.Bstr());
			pxid->m_fInUnicode = false;
			pxid->m_stuChars.Clear();
			break;

		case kcptMultiString:
			// Create a TsString from the current string builder.
			if (pxid->m_wsMulti)
			{
				Assert(pxid->m_qtsb);
				pxid->m_qtsb->GetString(&qtss);
				qtss->get_NormalizedForm(knmNFD, &qtssNormalized);
				pxid->SetMultiStringField(pxid, pxid->m_wsMulti, qtssNormalized);
				pxid->m_wsMulti = 0;
			}
			pxid->m_fInString = false;
			pxid->m_qtsb.Clear();
			break;

		case kcptMultiUnicode:
			if (pxid->m_wsMulti)
			{
				pxid->SetMultiUnicodeField(pxid, pxid->m_wsMulti, pxid->m_stuChars.Bstr());
				pxid->m_wsMulti = 0;
			}
			pxid->m_fInUnicode = false;
			pxid->m_stuChars.Clear();
			break;

		default:
			Assert(false);
		}
		break;

	default:
		// THIS SHOULD NEVER HAPPEN!
		pxid->LogMessage("INTERNAL XML ELEMENT STACK CORRUPTED!?");
		pxid->m_fError = true;
		pxid->m_hr = E_UNEXPECTED;
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	Throw an error if the version of the WPX file is something we can't handle.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::CheckVersion(WpXmlImportData * pxid, const XML_Char ** prgpszAtts)
{
	const char * pszVersion = FwXml::GetAttributeValue(prgpszAtts, "xmlVersion");
	if (!pszVersion)
		pszVersion = FwXml::GetAttributeValue(prgpszAtts, "wpxVersion");
	if (pszVersion)
	{
		int cch = strlen(pszVersion);
		int nVersion = 0;
		int nSubVersion = 0;
		bool fDecimal = false;
		char * pch = (char *)pszVersion;
		for (int ich = 0; ich < cch; ich++, pch++)
		{
			if (*pch == '.')
			{
				fDecimal = true;
			}
			else if (*pch >= '0' && *pch <= '9')
			{
				if (fDecimal)
					nSubVersion = (nSubVersion * 10) + (*pch - '0');
				else
					nVersion = (nVersion * 10) + (*pch - '0');
			}
			else
				break; // illegal character
		}
		if (nVersion > 2 || nVersion == 2 && nSubVersion > 0)
		{
			// Version we don't know how to handle.
			StrAnsi staRes(kstidWpXmlErrMsg037); // Cannot read WPX version %s
			pxid->CreateErrorMessage(staRes.Chars(), pszVersion);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Throw an error if the field (property) is undefined for the class.
----------------------------------------------------------------------------------------------*/
bool WpXmlImportData::CheckValidField(WpXmlImportData * pxid, int wpc, int wpf)
{

	switch (wpc)
	{
	case kwpcWs:
		switch (wpf)
		{
		case kwpfName:
		case kwpfAbbr:
		case kwpfOldWritingSystems:
		case kwpfRightToLeft:
		case kwpfVertical:
		case kwpfDefSerif:
		case kwpfDefSans:
		case kwpfDefBodyFont:
		case kwpfDefMono:
		case kwpfKeymanKeyboard:
		case kwpfFontVar:
		case kwpfSansFontVar:
		case kwpfBodyFontFeatures:
		case kwpfKeyboardType:
		case kwpfRendererType:
		case kwpfRenderer:
		case kwpfRendererInit:
		case kwpfWritingSystem:
		case kwpfDescr:
		case kwpfCollations:
		case kwpfLocale:
		case kwpfLegacyMap:
		case kwpfICULocale:
		case kwpfValidChars:
		case kwpfSpellCheckDictionary:
		case kwpfCapitalizationInfo:			// <CapitalizationInfo24>: Uni
		case kwpfLastModified:			// <LastModified24>: Uni
		case kwpfMatchedPairs:			// <MatchedPairs24>: Uni
		case kwpfPunctuationPatterns:		// <PunctuationPatterns24>: Uni
		case kwpfQuotationMarks:			// <QuotationMarks24>: Uni
			return true;
		default:
			// fall through to error
			break;
		}
		break;

	case kwpcOws:
		switch (wpf)
		{
		case kwpfName:
		case kwpfDescr:
		case kwpfRendererType:
		case kwpfRightToLeft:
		case kwpfVertical:
		case kwpfDefSerif:
		case kwpfDefSans:
		case kwpfDefBodyFont:
		case kwpfDefMono:
		case kwpfFontVar:
		case kwpfSansFontVar:
		case kwpfBodyFontFeatures:
		case kwpfKeyboardType:
		case kwpfLangId:
		case kwpfKeyManCtrl:
		case kwpfAbbr:
		case kwpfCharPropOverrides:
		case kwpfCode:
		case kwpfCollations:
		case kwpfLocale:
			return true;
		default:
			// fall through to error
			break;
		}
		break;

	case kwpcColl:
		switch (wpf)
		{
		case kwpfCode:
		case kwpfIcuResourceName:
		case kwpfIcuResourceText:
		case kwpfName:
		case kwpfWinCollation:
		case kwpfWinLCID:
		case kwpfICURules:
			return true;
		default:
			// fall through to error
			break;
		}
		break;

	case kwpcPageInfo:
		switch (wpf)
		{
		case kwpfTopMargin:
		case kwpfBottomMargin:
		case kwpfLeftMargin:
		case kwpfRightMargin:
		case kwpfHeaderMargin:
		case kwpfFooterMargin:
		case kwpfPageSize:
		case kwpfPageHeight:
		case kwpfPageWidth:
		case kwpfPageOrientation:
		case kwpfHeader:
		case kwpfFooter:
		case kwpfHeaderFont:
			return true;
		default:
			// fall through to error
			break;
		}
		break;

	case kwpcPara:
		switch (wpf)
		{
		case kwpfStyleRules:
		case kwpfContents:
		case kwpfParaLabel:
		case kwpfStyleName:
			return true;
		default:
			// fall through to error
			break;
		}
		break;

	case kwpcStyle:
		switch (wpf)
		{
		case kwpfName:
		case kwpfBasedOn:
		case kwpfNext:
		case kwpfType:
			return true;
		default:
			// fall through to error
			break;
		}
		break;

	case kwpcTextProp:		// Deprecated: For compatibility with older WorldPad files.
		switch (wpf)
		{
		case kwpfWsStyles:
			return true;
		default:
			// fall through to error
			break;
		}
		break;

	default:
		Assert(false);
	}

	// "Invalid property for class"
	StrAnsi staRes(kstidWpXmlErrMsg020);
	pxid->CreateErrorMessage(staRes.Chars());

	return false;
}

/*----------------------------------------------------------------------------------------------
	Throw an error if the field (property) is undefined for the class or if it does not
	take the given kind of basic object.
----------------------------------------------------------------------------------------------*/
bool WpXmlImportData::CheckValidBasicType(WpXmlImportData * pxid, int wpc, int wpf, int cpt)
{
	if (!CheckValidField(pxid, wpc, wpf))
		return false;

	switch (wpc)
	{
	case kwpcWs:
		switch (wpf)
		{
		case kwpfName:
			return (cpt == kcptString || cpt == kcptUnicode || cpt == kcptMultiUnicode);
		case kwpfAbbr:
			return (cpt == kcptUnicode || cpt == kcptMultiUnicode);
		case kwpfRendererType:
			return (cpt == kcptUnicode);
		case kwpfRenderer:
			return (cpt == kcptGuid);
		case kwpfRendererInit:
			return (cpt == kcptUnicode);
		case kwpfRightToLeft:
			return (cpt == kcptBoolean);
		case kwpfVertical:
			return (cpt == kcptBoolean);
		case kwpfDefSerif:
			return (cpt == kcptUnicode);
		case kwpfDefSans:
			return (cpt == kcptUnicode);
		case kwpfDefBodyFont:
			return (cpt == kcptUnicode);
		case kwpfDefMono:
			return (cpt == kcptUnicode);
		case kwpfKeymanKeyboard:
			return (cpt == kcptUnicode);
		case kwpfFontVar:
			return (cpt == kcptUnicode);
		case kwpfSansFontVar:
			return (cpt == kcptUnicode);
		case kwpfBodyFontFeatures:
			return (cpt == kcptUnicode);
		case kwpfKeyboardType:
			return (cpt == kcptUnicode);
		case kwpfWritingSystem:
			return (cpt == kcptInteger);
		case kwpfDescr:
			return (cpt == kcptString || cpt == kcptMultiString);
		case kwpfLocale:
			return (cpt == kcptInteger);
		case kwpfLegacyMap:
			return (cpt == kcptUnicode);
		case kwpfICULocale:
			return (cpt == kcptUnicode);
		case kwpfValidChars:
			return (cpt == kcptUnicode);
		case kwpfSpellCheckDictionary:
			return (cpt == kcptUnicode);
		case kwpfCapitalizationInfo:
			return (cpt == kcptUnicode);
		case kwpfLastModified:
			return (cpt == kcptTime);
		case kwpfMatchedPairs:
			return (cpt == kcptUnicode);
		case kwpfPunctuationPatterns:
			return (cpt == kcptUnicode);
		case kwpfQuotationMarks:
			return (cpt == kcptUnicode);
		}
		break;

	case kwpcOws:
		switch (wpf)
		{
		case kwpfName:
			return (cpt == kcptString || cpt == kcptUnicode || cpt == kcptMultiUnicode);
		case kwpfDescr:
			return (cpt == kcptString || cpt == kcptMultiString);
		case kwpfRendererType:
			return (cpt == kcptUnicode);
		case kwpfRightToLeft:
			return (cpt == kcptBoolean);
		case kwpfVertical:
			return (cpt == kcptBoolean);
		case kwpfDefSerif:
			return (cpt == kcptUnicode);
		case kwpfDefSans:
			return (cpt == kcptUnicode);
		case kwpfDefBodyFont:
			return (cpt == kcptUnicode);
		case kwpfDefMono:
			return (cpt == kcptUnicode);
		case kwpfFontVar:
			return (cpt == kcptUnicode);
		case kwpfSansFontVar:
			return (cpt == kcptUnicode);
		case kwpfBodyFontFeatures:
			return (cpt == kcptUnicode);
		case kwpfKeyboardType:
			return (cpt == kcptUnicode);
		case kwpfLangId:
			return (cpt == kcptInteger);
		case kwpfKeyManCtrl:
			return (cpt == kcptUnicode);
		case kwpfAbbr:
			return (cpt == kcptUnicode);
		case kwpfCharPropOverrides:
			return (cpt == kcptUnicode);
		case kwpfCode:
			return (cpt == kcptInteger);
		case kwpfLocale:
			return (cpt == kcptInteger);
		}
		break;

	case kwpcColl:
		switch (wpf)
		{
		case kwpfCode:
			return (cpt == kcptInteger);
		case kwpfIcuResourceName:
			return (cpt == kcptUnicode);
		case kwpfIcuResourceText:
			return (cpt == kcptUnicode);
		case kwpfName:
			return (cpt == kcptMultiUnicode);
		case kwpfWinCollation:
			return (cpt == kcptUnicode);
		case kwpfWinLCID:
			return (cpt == kcptInteger);
		case kwpfICURules:
			return (cpt == kcptUnicode);
		}
		break;

	case kwpcStyle:
		if (wpf == kwpfName && cpt == kcptUnicode)
			return true;
		if (wpf == kwpfBasedOn && cpt == kcptUnicode)
			return true;
		if (wpf == kwpfNext && cpt == kcptUnicode)
			return true;
		if (wpf == kwpfType && cpt == kcptInteger)
			return true;
		break;

	case kwpcPara:
		if (wpf == kwpfContents && cpt == kcptString)
			return true;
		if (wpf == kwpfParaLabel && cpt == kcptString)
			return true;
		if (wpf == kwpfStyleName && cpt == kcptUnicode)
			return true;
		break;

	case kwpcPageInfo:
		if ((wpf == kwpfTopMargin || wpf == kwpfBottomMargin || wpf == kwpfLeftMargin ||
				wpf == kwpfRightMargin || wpf == kwpfHeaderMargin || wpf == kwpfFooterMargin ||
				wpf == kwpfPageSize || wpf == kwpfPageHeight || wpf == kwpfPageWidth ||
				wpf == kwpfPageOrientation) &&
			cpt == kcptInteger)
		{
			return true;
		}
		else if (((wpf == kwpfHeader) || (wpf == kwpfFooter)) && cpt == kcptString)
			return true;
		// TODO SharonC: handle kwpfHeaderFont
		break;

	default:
		Assert(false);
	}

	// "Invalid type for property"
	StrAnsi staRes(kstidWpXmlErrMsg021);
	pxid->CreateErrorMessage(staRes.Chars());

	return false;
}

/*----------------------------------------------------------------------------------------------
	Set a property of the top open object to an integer (or boolean) value.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::SetIntegerField(WpXmlImportData * pxid, int nVal)
{
	Assert(pxid->m_vetiOpen.Size() >= 3);
	ElemTypeInfo etiBasicElem;
	etiBasicElem = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
	Assert(etiBasicElem.m_elty == keltyBasicProp);
	ElemTypeInfo etiProp = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 2];
	Assert(etiProp.m_elty == keltyPropName);
	ElemTypeInfo etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 3];
	Assert(etiObject.m_elty == keltyObject);

	void * pvObject = *(pxid->m_vpvOpen.Top());

	IWritingSystemPtr qws;
	ICollationPtr qcoll;
	IUnknown * punk = reinterpret_cast<IUnknown *>(pvObject);
	HVO hvo;
	WpMainWnd * pwpwnd;

	switch (etiObject.m_wpc)
	{
	case kwpcWs:
	case kwpcOws:
		if (!punk)
			// A writing system that already exists, so don't bother setting the attribute.
			return;

		CheckHr(punk->QueryInterface(IID_IWritingSystem, (void**)&qws));
		switch (etiProp.m_wpf)
		{
		case kwpfRightToLeft:
			CheckHr(qws->put_RightToLeft(ComBool(nVal)));
			break;
		case kwpfLocale:
			CheckHr(qws->put_Locale(nVal));
			break;
		case kwpfVertical:
			// ENHANCE: set the vertical property.
			break;
		case kwpfWritingSystem:
			// What do we do, if anything?
			break;

		// Ignore these fields for backwards compatibility.
		case kwpfLangId:
			break;

		default:
			Assert(false);
			break;
		}
		break;

	case kwpcColl:
		CheckHr(punk->QueryInterface(IID_ICollation, (void **)&qcoll));
		switch (etiProp.m_wpf)
		{
		case kwpfCode:
			// What do we do, if anything?
			break;
		case kwpfWinLCID:
			CheckHr(qcoll->put_WinLCID(nVal));
			break;
		default:
			Assert(false);
		}
		break;

	case kwpcStyle:
		hvo = (HVO)pvObject;
		switch (etiProp.m_wpf)
		{
		case kwpfType:
			CheckHr(pxid->m_qda->CacheIntProp(hvo, kflidStStyle_Type, nVal));
			break;
		default:
			Assert(false);
		}
		break;

	case kwpcPageInfo:
		pwpwnd = reinterpret_cast<WpMainWnd *>(pvObject);
		switch (etiProp.m_wpf)
		{
		case kwpfTopMargin:
			pwpwnd->SetTopMargin(nVal);
			break;
		case kwpfBottomMargin:
			pwpwnd->SetBottomMargin(nVal);
			break;
		case kwpfLeftMargin:
			pwpwnd->SetLeftMargin(nVal);
			break;
		case kwpfRightMargin:
			pwpwnd->SetRightMargin(nVal);
			break;
		case kwpfHeaderMargin:
			pwpwnd->SetHeaderMargin(nVal);
			break;
		case kwpfFooterMargin:
			pwpwnd->SetFooterMargin(nVal);
			break;
		case kwpfPageSize:
			pwpwnd->SetPageSize(nVal);
			break;
		case kwpfPageHeight:
			pwpwnd->SetPageHeight(nVal);
			break;
		case kwpfPageWidth:
			pwpwnd->SetPageWidth(nVal);
			break;
		case kwpfPageOrientation:
			pwpwnd->SetPageOrientation(nVal);
			break;
		// TODO SharonC: handle kwpfHeaderFont when it becomes stable
		default:
			Assert(false);
		}
		break;

	case kwpcPara:
		hvo = (HVO)pvObject;
		Assert(false);	// no integer properties
		break;

	case kwpcTextProp:
		Assert(false);	// no integer properties
		break;

	default:
		Assert(false);
	}
}

/*----------------------------------------------------------------------------------------------
	Set a property of the top open object to a String value.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::SetStringField(WpXmlImportData * pxid, ITsString * ptss)
{
	ElemTypeInfo etiProp = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
	Assert(etiProp.m_elty == keltyPropName);
	ElemTypeInfo etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 2];
	Assert(etiObject.m_elty == keltyObject);

	void * pvObject = *(pxid->m_vpvOpen.Top());
	IWritingSystemPtr qws;
	IUnknown * punk = reinterpret_cast<IUnknown *>(pvObject);
	WpMainWnd * pwpwnd;
	HVO hvo;
	SmartBstr sbstr;
	int cch;

	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
	int wsUser;
	CheckHr(qwsf->get_UserWs(&wsUser));

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	AssertPtr(qtsf);
	ITsStringPtr qtssDescr;

	switch (etiObject.m_wpc)
	{
	case kwpcWs:
	case kwpcOws:
		if (!punk)
			// A writing system that already exists, so don't bother setting the attribute.
			return;

		CheckHr(punk->QueryInterface(IID_IWritingSystem, (void**)&qws));
		switch (etiProp.m_wpf)
		{
		case kwpfName:
			// Handle antiquated WPX files.
			CheckHr(ptss->get_Length(&cch));
			CheckHr(ptss->GetChars(0, cch, &sbstr));
			CheckHr(qws->put_Name(wsUser, sbstr));
			break;
		case kwpfDescr:
			CheckHr(qws->put_Description(wsUser, ptss));
			break;
		default:
			Assert(false);
		}
		break;

	case kwpcColl:
		Assert(false);	// no string properties
		break;

	case kwpcStyle:
		hvo = (HVO)pvObject;
		Assert(false);	// no string properties
		break;

	case kwpcPageInfo:
		pwpwnd = reinterpret_cast<WpMainWnd *>(pvObject);
		switch(etiProp.m_wpf)
		{
		case kwpfHeader:
			pwpwnd->SetPageHeader(ptss);
			break;
		case kwpfFooter:
			pwpwnd->SetPageFooter(ptss);
			break;
		default:
			Assert(false);
		}
		break;

	case kwpcPara:
		hvo = (HVO)pvObject;
		switch (etiProp.m_wpf)
		{
		case kwpfContents:
			CheckHr(pxid->m_qda->CacheStringProp(hvo, kflidStTxtPara_Contents, ptss));
			break;
		case kwpfParaLabel:
			// Don't bother storing the paragraph label: WorldPad doesn't use them!
			break;

		default:
			Assert(false);
		}
		break;

	case kwpcTextProp:
		Assert(false);	// no string properties
		break;

	default:
		Assert(false);
	}
}

/*----------------------------------------------------------------------------------------------
	Set a property of the top open object to a Unicode value.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::SetUnicodeField(WpXmlImportData * pxid, BSTR bstr)
{
	ElemTypeInfo etiProp = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
	Assert(etiProp.m_elty == keltyPropName);
	ElemTypeInfo etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 2];
	Assert(etiObject.m_elty == keltyObject);

	void * pvObject = *(pxid->m_vpvOpen.Top());

	IWritingSystemPtr qws;
	ICollationPtr qcoll;

	IRenderEnginePtr qreneng;
	ITsStringPtr qtss;
	IUnknown * punk = reinterpret_cast<IUnknown *>(pvObject);
	WpMainWnd * pwpwnd;
	HVO hvo;
	StrUni stu;
	int ws;

	switch (etiObject.m_wpc)
	{
	case kwpcWs:
	case kwpcOws:
		if (!punk)
			// A writing system that already exists, so don't bother setting the attribute.
			return;

		CheckHr(punk->QueryInterface(IID_IWritingSystem, (void**)&qws));
		switch (etiProp.m_wpf)
		{
		case kwpfName:
			// Handle antiquated WPX files.
			CheckHr(qws->put_Name(pxid->m_wsUser, bstr));
			break;
		case kwpfRendererType:
			// Do nothing (obsolete element type).
			break;
		case kwpfFontVar:
			CheckHr(qws->put_FontVariation(bstr));
			break;
		case kwpfSansFontVar:
			CheckHr(qws->put_SansFontVariation(bstr));
			break;
		case kwpfBodyFontFeatures:
			CheckHr(qws->put_BodyFontFeatures(bstr));
			break;
		case kwpfDefSerif:
			CheckHr(qws->put_DefaultSerif(bstr));
			break;
		case kwpfDefSans:
			CheckHr(qws->put_DefaultSansSerif(bstr));
			break;
		case kwpfDefBodyFont:
			CheckHr(qws->put_DefaultBodyFont(bstr));
			break;
		case kwpfDefMono:
			CheckHr(qws->put_DefaultMonospace(bstr));
			break;
		case kwpfKeymanKeyboard:
			CheckHr(qws->put_KeymanKbdName(bstr));
			break;
		case kwpfKeyboardType:
			if (wcscmp(bstr, L"keyman") == 0)
				CheckHr(qws->put_KeyMan(ComBool(true)));
			else
				CheckHr(qws->put_KeyMan(ComBool(false)));
			break;
		case kwpfLegacyMap:
			CheckHr(qws->put_LegacyMapping(bstr));
			break;
		case kwpfValidChars:
			CheckHr(qws->put_ValidChars(bstr));
			break;
		case kwpfCapitalizationInfo:
			CheckHr(qws->put_CapitalizationInfo(bstr));
			break;
		case kwpfMatchedPairs:
			CheckHr(qws->put_MatchedPairs(bstr));
			break;
		case kwpfPunctuationPatterns:
			CheckHr(qws->put_PunctuationPatterns(bstr));
			break;
		case kwpfQuotationMarks:
			CheckHr(qws->put_QuotationMarks(bstr));
			break;
		case kwpfSpellCheckDictionary:
			if (BstrLen(bstr) > 0)
				CheckHr(qws->put_SpellCheckDictionary(bstr));
			break;
		case kwpfICULocale:
			// NOTE: WorldPad uses the ICU Locale string as the identifier in its XML format.
			CheckHr(qws->get_WritingSystem(&ws));
			if (!ws)
			{
				// There was no id attribute for the <LgWritingSystem> element.
				// Recreate the object, this time in such a way as to get the ws/hvo value
				// assigned to it.
				IWritingSystemPtr qwsNew;
				ILgWritingSystemFactoryPtr qwsf;	// Get the memory-based factory.
				qwsf.CreateInstance(CLSID_LgWritingSystemFactory);
				CheckHr(qwsf->GetWsFromStr(bstr, &ws));
				if (ws)
				{
					if (!pxid->m_fOverwriteWs)
					{
						// The writing system already existed, don't overwrite it.
						*(pxid->m_vpvOpen.Top()) = NULL;
						qws.Clear();		// calls release.
						punk->Release();	// deletes the object.
						break;
					}
					CheckHr(qwsf->get_EngineOrNull(ws, &qwsNew));
				}
				else
				{
					// This generates the ws/hvo value inside the newly created writing
					// system.
					CheckHr(qwsf->get_Engine(bstr, &qwsNew));
					CheckHr(qwsf->GetWsFromStr(bstr, &ws));
					StrAnsi staWs(bstr);
					pxid->m_hmcws.Insert(staWs.Chars(), ws, true);
				}
				AssertPtr(qwsNew);
				// Store the ICU locale and set the dirty bit.
				CheckHr(qwsNew->put_IcuLocale(bstr));
				CheckHr(qwsNew->put_Dirty(TRUE));

				// Work through all the other properties, copying whatever has been parsed
				// in already.  (maybe nothing, maybe everything else)
				int nT;
				ComBool fT;
				SmartBstr sbstrT;
				int ccoll;
				int icoll;
				int cws;
				int iws;
				DATE dat;
				Vector<int> vws;

				CheckHr(qws->get_Locale(&nT));
				if (nT)
					CheckHr(qwsNew->put_Locale(nT));
				CheckHr(qws->get_RightToLeft(&fT));
				if (fT)
					CheckHr(qwsNew->put_RightToLeft(fT));
				CheckHr(qws->get_FontVariation(&sbstrT));
				if (sbstrT.Length())
					CheckHr(qwsNew->put_FontVariation(sbstrT));
				CheckHr(qws->get_SansFontVariation(&sbstrT));
				if (sbstrT.Length())
					CheckHr(qwsNew->put_SansFontVariation(sbstrT));
				CheckHr(qws->get_DefaultSerif(&sbstrT));
				if (sbstrT.Length())
					CheckHr(qwsNew->put_DefaultSerif(sbstrT));
				CheckHr(qws->get_DefaultSansSerif(&sbstrT));
				if (sbstrT.Length())
					CheckHr(qwsNew->put_DefaultSansSerif(sbstrT));
				CheckHr(qws->get_DefaultBodyFont(&sbstrT));
				if (sbstrT.Length())
					CheckHr(qwsNew->put_DefaultBodyFont(sbstrT));
				CheckHr(qws->get_DefaultMonospace(&sbstrT));
				if (sbstrT.Length())
					CheckHr(qwsNew->put_DefaultMonospace(sbstrT));
				CheckHr(qws->get_KeyMan(&fT));
				if (fT)
					CheckHr(qwsNew->put_KeyMan(fT));
				CheckHr(qws->get_LegacyMapping(&sbstrT));
				if (sbstrT.Length())
					CheckHr(qwsNew->put_LegacyMapping(sbstrT));
				CheckHr(qws->get_KeymanKbdName(&sbstrT));
				if (sbstrT.Length())
					CheckHr(qwsNew->put_KeymanKbdName(sbstrT));
				CheckHr(qws->get_LastModified(&dat));
				if (dat)
					CheckHr(qwsNew->put_LastModified(dat));
				CheckHr(qws->get_CollationCount(&ccoll));
				for (icoll = 0; icoll < ccoll; ++icoll)
				{
					ICollationPtr qcoll;
					CheckHr(qws->get_Collation(icoll, &qcoll));
					CheckHr(qwsNew->putref_Collation(icoll, qcoll));
				}
				CheckHr(qws->get_NameWsCount(&cws));
				if (cws)
				{
					vws.Resize(cws);
					CheckHr(qws->get_NameWss(cws, vws.Begin()));
					for (iws = 0; iws < cws; ++iws)
					{
						CheckHr(qws->get_Name(vws[iws], &sbstrT));
						CheckHr(qwsNew->put_Name(vws[iws], sbstrT));
					}
				}
				CheckHr(qws->get_AbbrWsCount(&cws));
				if (cws)
				{
					vws.Resize(cws);
					CheckHr(qws->get_AbbrWss(cws, vws.Begin()));
					for (iws = 0; iws < cws; ++iws)
					{
						CheckHr(qws->get_Abbr(vws[iws], &sbstrT));
						CheckHr(qwsNew->put_Abbr(vws[iws], sbstrT));
					}
				}
				CheckHr(qws->get_DescriptionWsCount(&cws));
				if (cws)
				{
					vws.Resize(cws);
					CheckHr(qws->get_DescriptionWss(cws, vws.Begin()));
					for (iws = 0; iws < cws; ++iws)
					{
						ITsStringPtr qtss;
						CheckHr(qws->get_Description(vws[iws], &qtss));
						CheckHr(qwsNew->put_Description(vws[iws], qtss));
					}
				}

				// Replace the dummy placeholder on the stack with the real writing system.
				*(pxid->m_vpvOpen.Top()) = (void *)qwsNew.Detach();
				qws.Clear();		// calls release.
				punk->Release();	// deletes the object.
			}
			break;

		// Ignore these fields for backwards compatibility
		case kwpfAbbr:
		case kwpfKeyManCtrl:
		case kwpfCharPropOverrides:
			break;
		// Ignore these fields because they've never yet been used.
		case kwpfRendererInit:
			break;
		default:
			Assert(false);
		}
		break;

	case kwpcColl:
		CheckHr(punk->QueryInterface(IID_ICollation, (void **)&qcoll));
		switch (etiProp.m_wpf)
		{
		case kwpfIcuResourceName:
			CheckHr(qcoll->put_IcuResourceName(bstr));
			break;
		case kwpfIcuResourceText:
			CheckHr(qcoll->put_IcuResourceText(bstr));
			break;
		case kwpfWinCollation:
			CheckHr(qcoll->put_WinCollation(bstr));
			break;
		case kwpfICURules:
			CheckHr(qcoll->put_IcuRules(bstr));
			break;
		default:
			Assert(false);
			break;
		}
		break;

	case kwpcStyle:
		hvo = (HVO)pvObject;
		switch (etiProp.m_wpf)
		{
		case kwpfName:
			CheckHr(pxid->m_qda->CacheUnicodeProp(hvo, kflidStStyle_Name, bstr, BstrLen(bstr)));
			break;
		case kwpfBasedOn:
			// Store the BasedOn value in a map that will later be used to set
			// reference attributes.
			stu = bstr;
			pxid->m_hmhvostuBasedOn.Insert(hvo, stu);
			break;
		case kwpfNext:
			// Store the Next value in a map that will later be used to set
			// reference attributes.
			stu = bstr;
			pxid->m_hmhvostuNextStyle.Insert(hvo, stu);
			break;
		default:
			Assert(false);
		}
		break;

	case kwpcPageInfo:
		pwpwnd = reinterpret_cast<WpMainWnd *>(pvObject);
		Assert(false);	// no Unicode properties
		break;

	case kwpcPara:
		if (etiProp.m_wpf != kwpfStyleName)
		{
			hvo = (HVO)pvObject;
			Assert(false);	// no Unicode properties apart from unused StyleName15
		}
		break;

	case kwpcTextProp:
		Assert(false);	// no Unicode properties
		break;

	default:
		Assert(false);
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	Set a property of the top open object to a MultiString value.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::SetMultiStringField(WpXmlImportData * pxid, int ws, ITsString * ptss)
{
	ElemTypeInfo etiProp = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
	Assert(etiProp.m_elty == keltyPropName);
	ElemTypeInfo etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 2];
	Assert(etiObject.m_elty == keltyObject);

	void * pvObject = *(pxid->m_vpvOpen.Top());
	IUnknown * punk = reinterpret_cast<IUnknown *>(pvObject);
	IWritingSystemPtr qws;
	switch (etiObject.m_wpc)
	{
//	If we ever need this case:
//	case kwpcWs:
//		if (!punk)
//			// A writing system that already exists, so don't bother setting the attribute.
//			return;
//		etc...
	case kwpcWs:
	case kwpcOws:
		Assert(etiProp.m_wpf == kwpfDescr);
		if (punk)
		{
			CheckHr(punk->QueryInterface(IID_IWritingSystem, (void**)&qws));
			AssertPtr(qws.Ptr());
			if (etiProp.m_wpf == kwpfDescr)
				CheckHr(qws->put_Description(ws, ptss));
		}
		break;

	default:
		Assert(etiObject.m_wpc == kwpcOws);
	}
}

/*----------------------------------------------------------------------------------------------
	Set a property of the top open object to a MultiUnicode value.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::SetMultiUnicodeField(WpXmlImportData * pxid, int ws, BSTR bstr)
{
	ElemTypeInfo etiProp = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 1];
	Assert(etiProp.m_elty == keltyPropName);
	ElemTypeInfo etiObject = pxid->m_vetiOpen[pxid->m_vetiOpen.Size() - 2];
	Assert(etiObject.m_elty == keltyObject);

	void * pvObject = *(pxid->m_vpvOpen.Top());
	IUnknown * punk = reinterpret_cast<IUnknown *>(pvObject);
	ILgWritingSystemFactoryPtr qwsf;
	qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
	IWritingSystemPtr qws;
	ICollationPtr qcoll;

	switch (etiObject.m_wpc)
	{
	case kwpcWs:
	case kwpcOws:
		if (!punk)
			// A writing system that already exists, so don't bother setting the attribute.
			return;

		Assert(etiProp.m_wpf == kwpfName || etiProp.m_wpf == kwpfAbbr);
		if (punk)
		{
			CheckHr(punk->QueryInterface(IID_IWritingSystem, (void**)&qws));
			AssertPtr(qws);
			if (etiProp.m_wpf == kwpfName)
				CheckHr(qws->put_Name(ws, bstr));
			else if (etiProp.m_wpf == kwpfAbbr)
				CheckHr(qws->put_Abbr(ws, bstr));
		}
		break;

	case kwpcColl:
		Assert(etiProp.m_wpf == kwpfName);
		if (punk)
		{
			CheckHr(punk->QueryInterface(IID_ICollation, (void **)&qcoll));
			AssertPtr(qcoll);
			if (etiProp.m_wpf == kwpfName)
				CheckHr(qcoll->put_Name(ws, bstr));
		}
		break;

	default:
		Assert(etiObject.m_wpc == kwpcWs || etiObject.m_wpc == kwpcOws);
		break;
	}
}


/*----------------------------------------------------------------------------------------------
	Skip over a valid number string, returning a pointer to the first character past the end of
	the number.
----------------------------------------------------------------------------------------------*/
static const char * ScanNumber(const char * pszNum)
{
	const char * pszEnd = pszNum;
	if (*pszEnd == '+' || *pszEnd == '-')
		++pszEnd;
	int cDigits = strspn(pszEnd, "0123456789");
	pszEnd += cDigits;
	if (*pszEnd == '.')
		++pszEnd;
	int cDigitsFrac = strspn(pszEnd, "0123456789");
	pszEnd += cDigitsFrac;
	if (cDigits + cDigitsFrac)		// Must have at least one digit in a number!
		return pszEnd;
	else
		return pszNum;
}

/*----------------------------------------------------------------------------------------------
	Add the WsStyles to the main text properties object.

	This method must be kept in sync with FwStyledText::EncodeFontPropsString().
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::AddWsStyles(void * pvMainTextProp, Vector<void *> & vpvWsStyles,
	void ** ppvModified)
{
	IUnknown * punkMain = reinterpret_cast<IUnknown *>(pvMainTextProp);
	ITsTextProps * pttpMain;
	CheckHr(punkMain->QueryInterface(IID_ITsTextProps, (void**)&pttpMain));
	ITsPropsBldrPtr qtpb;
	CheckHr(pttpMain->GetBldr(&qtpb));

	// Build up a string describing all the font properties for the various encodings,
	// and store it in the wsStyles property.
	// Note: the encodings must be in alphabetical order in order for the style to work.
	Vector<WsStyleInfo> vesi;
	for (int ittp = 0; ittp < vpvWsStyles.Size(); ittp++)
	{
		IUnknown * punkSub = reinterpret_cast<IUnknown *>(vpvWsStyles[ittp]);
		if (!punkSub)
			continue;				// could happen for obsolete ws="___" alternative
		ITsTextPropsPtr qttp;
		CheckHr(punkSub->QueryInterface(IID_ITsTextProps, (void**)&qttp));
		AssertPtr(qttp);
		WsStyleInfo esi;
		int nVar;
		SmartBstr sbstr;
		HRESULT hr;
		// Get the various integer valued properties.
		CheckHr(hr = qttp->GetIntPropValues(ktptWs, &nVar, &esi.m_ws));
		if (hr == S_FALSE)
			continue;		// problem assigning ws (probably antique WPX or WPT file)
		CheckHr(hr = qttp->GetIntPropValues(ktptFontSize, &nVar, &esi.m_mpSize));
		Assert((esi.m_mpSize == -1 && nVar == -1) || nVar == ktpvMilliPoint);
		if (hr == S_FALSE)
			esi.m_mpSize = knNinch;
		CheckHr(hr = qttp->GetIntPropValues(ktptBold, &nVar, &esi.m_fBold));
		if (hr == S_FALSE)
			esi.m_fBold = knNinch;
		CheckHr(hr = qttp->GetIntPropValues(ktptItalic, &nVar, &esi.m_fItalic));
		if (hr == S_FALSE)
			esi.m_fItalic = knNinch;
		CheckHr(hr = qttp->GetIntPropValues(ktptSuperscript, &nVar, &esi.m_ssv));
		if (hr == S_FALSE)
			esi.m_ssv = knNinch;
		CheckHr(hr = qttp->GetIntPropValues(ktptForeColor, &nVar, (int *)&esi.m_clrFore));
		if (hr == S_FALSE)
			esi.m_clrFore = (COLORREF)knNinch;
		CheckHr(hr = qttp->GetIntPropValues(ktptBackColor, &nVar, (int *)&esi.m_clrBack));
		if (hr == S_FALSE)
			esi.m_clrBack = (COLORREF)knNinch;
		CheckHr(hr = qttp->GetIntPropValues(ktptUnderColor, &nVar, (int *)&esi.m_clrUnder));
		if (hr == S_FALSE)
			esi.m_clrUnder = (COLORREF)knNinch;
		CheckHr(hr = qttp->GetIntPropValues(ktptUnderline, &nVar, &esi.m_unt));
		if (hr == S_FALSE)
			esi.m_unt = knNinch;
		CheckHr(hr = qttp->GetIntPropValues(ktptOffset, &nVar, &esi.m_mpOffset));
		Assert((esi.m_mpOffset == -1 && nVar == -1) || nVar == ktpvMilliPoint);
		if (hr == S_FALSE)
			esi.m_mpOffset = knNinch;
		// Get the various string valued properties.
		CheckHr(hr = qttp->GetStrPropValue(ktptFontFamily, &sbstr));
		if (hr == S_OK)
			esi.m_stuFontFamily.Assign(sbstr.Chars(), sbstr.Length());
		else
			esi.m_stuFontFamily.Clear();
		CheckHr(hr = qttp->GetStrPropValue(ktptFontVariations, &sbstr));
		if (hr == S_OK)
			esi.m_stuFontVar.Assign(sbstr.Chars(), sbstr.Length());
		else
			esi.m_stuFontVar.Clear();
		// Store style values sorted by writing system to ensure proper behavior.
		int iv;
		int ivLim;
		for (iv = 0, ivLim = vesi.Size(); iv < ivLim; )
		{
			int ivMid = (iv + ivLim) / 2;
			if ((unsigned)vesi[ivMid].m_ws < (unsigned)esi.m_ws)
				iv = ivMid + 1;
			else
				ivLim = ivMid;
		}
		vesi.Insert(iv, esi);
	}
	StrUni stuStyle = FwStyledText::EncodeFontPropsString(vesi, false);
	CheckHr(qtpb->SetStrPropValue(kspWsStyle, stuStyle.Bstr()));

	//	The new text properties:
	ITsTextProps * pttpModified;
	qtpb->GetTextProps(&pttpModified);		// Reference count = 1.

	pttpMain->Release();		// To balance QueryInterface above.
	punkMain->Release();		// Since we are about to replace this value.

	*ppvModified = (void *)pttpModified;
}

/*----------------------------------------------------------------------------------------------
	Add the BulNumFontInfo to the main text properties object.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::AddBulNumFontInfo(void * pvMainTextProp,
	Vector<void *> & vpvBulNumFontInfo, void ** ppvModified)
{
	Assert(pvMainTextProp);
	AssertPtr(ppvModified);
	if (!vpvBulNumFontInfo.Size())
	{
		*ppvModified = pvMainTextProp;
		return;
	}

	IUnknown * punkSub = reinterpret_cast<IUnknown *>(vpvBulNumFontInfo[0]);
	ITsTextPropsPtr qttpSub;
	CheckHr(punkSub->QueryInterface(IID_ITsTextProps, (void**)&qttpSub));
	Vector<wchar> vchFmt;
	vchFmt.Resize(300);
	int ich = 0;
	int nVar;
	int nVal;
	CheckHr(qttpSub->GetIntPropValues(ktptBackColor, &nVar, &nVal));
	if (nVal != -1)
	{
		vchFmt[ich++] = ktptBackColor;
		vchFmt[ich++] = (wchar)(nVal & 0xFFFF);
		vchFmt[ich++] = (wchar)((nVal >> 16) & 0xFFFF);
	}
	CheckHr(qttpSub->GetIntPropValues(ktptBold, &nVar, &nVal));
	if (nVal != -1)
	{
		vchFmt[ich++] = ktptBold;
		vchFmt[ich++] = (wchar)(nVal & 0xFFFF);
		vchFmt[ich++] = (wchar)((nVal >> 16) & 0xFFFF);
	}
	CheckHr(qttpSub->GetIntPropValues(ktptFontSize, &nVar, &nVal));
	Assert((nVal == -1 && nVar == -1) || nVar == ktpvMilliPoint);
	if (nVal != -1)
	{
		vchFmt[ich++] = ktptFontSize;
		vchFmt[ich++] = (wchar)(nVal & 0xFFFF);
		vchFmt[ich++] = (wchar)((nVal >> 16) & 0xFFFF);
	}
	CheckHr(qttpSub->GetIntPropValues(ktptForeColor, &nVar, &nVal));
	if (nVal != -1)
	{
		vchFmt[ich++] = ktptForeColor;
		vchFmt[ich++] = (wchar)(nVal & 0xFFFF);
		vchFmt[ich++] = (wchar)((nVal >> 16) & 0xFFFF);
	}
	CheckHr(qttpSub->GetIntPropValues(ktptItalic, &nVar, &nVal));
	if (nVal != -1)
	{
		vchFmt[ich++] = ktptItalic;
		vchFmt[ich++] = (wchar)(nVal & 0xFFFF);
		vchFmt[ich++] = (wchar)((nVal >> 16) & 0xFFFF);
	}
	CheckHr(qttpSub->GetIntPropValues(ktptOffset, &nVar, &nVal));
	Assert((nVal == -1 && nVar == -1) || nVar == ktpvMilliPoint);
	if (nVal != -1)
	{
		vchFmt[ich++] = ktptOffset;
		vchFmt[ich++] = (wchar)(nVal & 0xFFFF);
		vchFmt[ich++] = (wchar)((nVal >> 16) & 0xFFFF);
	}
	CheckHr(qttpSub->GetIntPropValues(ktptSuperscript, &nVar, &nVal));
	if (nVal != -1)
	{
		vchFmt[ich++] = ktptSuperscript;
		vchFmt[ich++] = (wchar)(nVal & 0xFFFF);
		vchFmt[ich++] = (wchar)((nVal >> 16) & 0xFFFF);
	}
	CheckHr(qttpSub->GetIntPropValues(ktptUnderColor, &nVar, &nVal));
	if (nVal != -1)
	{
		vchFmt[ich++] = ktptUnderColor;
		vchFmt[ich++] = (wchar)(nVal & 0xFFFF);
		vchFmt[ich++] = (wchar)((nVal >> 16) & 0xFFFF);
	}
	CheckHr(qttpSub->GetIntPropValues(ktptUnderline, &nVar, &nVal));
	if (nVal != -1)
	{
		vchFmt[ich++] = ktptUnderline;
		vchFmt[ich++] = (wchar)(nVal & 0xFFFF);
		vchFmt[ich++] = (wchar)((nVal >> 16) & 0xFFFF);
	}
	SmartBstr sbstrFontFamily;
	CheckHr(qttpSub->GetStrPropValue(ktptFontFamily, &sbstrFontFamily));
	int cchwFontLen = sbstrFontFamily.Length();
	if (cchwFontLen)
	{
		vchFmt[ich++] = ktptFontFamily;
		vchFmt.Resize(ich + cchwFontLen);
		memcpy(&vchFmt[ich], sbstrFontFamily.Chars(), cchwFontLen * sizeof(wchar));
	}
	else
	{
		vchFmt.Resize(ich);
	}

	if (vchFmt.Size())
	{
		IUnknown * punkMain = reinterpret_cast<IUnknown *>(pvMainTextProp);
		AssertPtr(punkMain);
		ITsTextProps * pttpMain;
		CheckHr(punkMain->QueryInterface(IID_ITsTextProps, (void**)&pttpMain));
		AssertPtr(pttpMain);
		ITsPropsBldrPtr qtpb;
		CheckHr(pttpMain->GetBldr(&qtpb));

		// DON'T count on null termination!  Probably has 0's in it.
		StrUni stuBulNumFontInfo(vchFmt.Begin(), vchFmt.Size());
		CheckHr(qtpb->SetStrPropValue(ktptBulNumFontInfo, stuBulNumFontInfo.Bstr()));

		//	The new text properties:
		ITsTextProps * pttpModified;
		qtpb->GetTextProps(&pttpModified);

		//	To balance QueryInterface above:
		pttpMain->Release();

		//	Because we are removing the old one from the open list and adding the modified one:
		pttpMain->Release();

		*ppvModified = (void *)pttpModified;
	}
	else
	{
		*ppvModified = pvMainTextProp;
	}
}

/*----------------------------------------------------------------------------------------------
	Put the value of one ows style item in the buffer.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::MakeProp(OLECHAR * & pch, int tpt, int nVar, int nVal, int & cprop)
{
	// If the value was conflicting and has not changed, or if it is now
	// unspecified, leave it out.
	if (nVal == knConflicting)
		return;
	if (nVal == knNinch)
		return;
	if (nVal == -1)
		return;
	cprop++;
	*pch++ = (OLECHAR) tpt;
	*pch++ = (OLECHAR) nVar;
	*pch++ = (OLECHAR) nVal;
	*pch++ = (OLECHAR) (nVal >> 16);
}

/*----------------------------------------------------------------------------------------------
	Handle XML character data during the second pass.
	This function is passed to the expat XML parser as a callback function.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::HandleCharData(void * pvUser, const XML_Char * prgch, int cch)
{
	StrAnsi staMsg;

	WpXmlImportData * pxid = reinterpret_cast<WpXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);
	if (!prgch || !cch)
		return;
#ifdef WIN32
	// The expat parser reduces "\r\n" to a bare "\n".
	if (*prgch == '\n' && cch == 1)
	{
		prgch = "\r\n";
		cch = 2;
	}
#endif /*WIN32*/
	try
	{
		if ((pxid->m_fInString && pxid->m_fInRun) || pxid->m_fInUnicode)
		{
			HandleCharDataAux(pvUser, prgch, cch);
		}
		else if (pxid->m_fInString && pxid->m_fBetaXML)
		{
			HandleOldStringData(pvUser, prgch, cch);
		}
		else
		{
			// Ignore
		}
	}
	catch (Throwable & thr)
	{
		pxid->m_fError = true;
		pxid->m_hr = thr.Error();
#ifdef DEBUG
		staMsg.Format("ERROR CAUGHT on line %d of %s: %s",
			__LINE__, __FILE__, AsciiHresult(pxid->m_hr));
		pxid->LogMessage(staMsg.Chars());
#endif
	}
	catch (...)
	{
		pxid->m_fError = true;
		pxid->m_hr = E_FAIL;
#ifdef DEBUG
		staMsg.Format("UNKNOWN ERROR CAUGHT on line %d of %s", __LINE__, __FILE__);
		pxid->LogMessage(staMsg.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Process character data (PCDATA) for string and unicode data.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::HandleCharDataAux(void * pvUser, const XML_Char * prgch, int cch)
{
	WpXmlImportData * pxid = reinterpret_cast<WpXmlImportData *>(pvUser);

	// Convert chars to UTF-16 and store them.
	wchar szwBuffer[400];		// Use stack temp space for smaller amounts.
	Vector<wchar> vchw;
	wchar * prgchw;
	int cchw = CountUtf16FromUtf8(prgch, cch);
	if (cchw <= 400)
	{
		prgchw = szwBuffer;
	}
	else
	{
		vchw.Resize(cchw);		// Too much for stack: use temp heap storage.
		prgchw = &vchw[0];
	}
	SetUtf16FromUtf8(prgchw, cchw, prgch, cch);
	// Replace tab, CR, or LF with space.
	for (wchar * pchwTmp = prgchw; pchwTmp < prgchw + cchw; pchwTmp++)
	{
		if (*pchwTmp == 9 || *pchwTmp == 10 || *pchwTmp == 13)
			*pchwTmp = 32;
	}
	pxid->m_stuChars.Append(prgchw, cchw);

	if (pxid->m_fInString)
	{
		Assert(pxid->m_fInRun);
		Assert(pxid->m_qtsb);
		if (!pxid->m_qtpbStr)
		{
			pxid->m_qtpbStr.CreateInstance(CLSID_TsPropsBldr); // with no props
			pxid->m_qtpbStr->SetIntPropValues(ktptWs, ktpvDefault, pxid->m_wsUser);
		}
		Assert(pxid->m_qtpbStr);
		// Create a text-properties object from the most recent text-properties.
		ITsTextPropsPtr qttp;
		pxid->m_qtpbStr->GetTextProps(&qttp);

		int cchwTmp;
		pxid->m_qtsb->get_Length(&cchwTmp);
		pxid->m_qtsb->ReplaceRgch(cchwTmp, cchwTmp, prgchw, cchw, qttp);
	}
	else
	{
		// Not in a string, in a flat Unicode object.
	}
}

/*----------------------------------------------------------------------------------------------
	Process character data (PCDATA) for the old style string elements that used embedded <Prop>
	elements interspersed with PCDATA instead of embedded <Run> elements that contain PCDATA.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::HandleOldStringData(void * pvUser, const XML_Char * prgch, int cch)
{
	WpXmlImportData * pxid = reinterpret_cast<WpXmlImportData *>(pvUser);

	// Convert chars to UTF-16 and store them.
	wchar szwBuffer[400];		// Use stack temp space for smaller amounts.
	Vector<wchar> vchw;
	wchar * prgchw;
	int cchw = CountUtf16FromUtf8(prgch, cch);
	if (cchw <= 400)
	{
		prgchw = szwBuffer;
	}
	else
	{
		vchw.Resize(cchw);		// Too much for stack: use temp heap storage.
		prgchw = &vchw[0];
	}
	SetUtf16FromUtf8(prgchw, cchw, prgch, cch);
	// Replace tab, CR, or LF with space.
	for (wchar * pchwTmp = prgchw; pchwTmp < prgchw + cchw; pchwTmp++)
	{
		if (*pchwTmp == 9 || *pchwTmp == 10 || *pchwTmp == 13)
			*pchwTmp = 32;
	}
	if (!pxid->m_fPropSeen)
	{
		// Create a default text-properties.
		pxid->m_qtpbStr.CreateInstance(CLSID_TsPropsBldr); // with no props
		pxid->m_qtpbStr->SetIntPropValues(ktptWs, ktpvDefault, pxid->m_wsUser);
	}
	Assert(pxid->m_qtpbStr);
	// Create a text-properties object from the most recent text-properties.
	ITsTextPropsPtr qttp;
	pxid->m_qtpbStr->GetTextProps(&qttp);
	int cchwTmp;
	pxid->m_qtsb->get_Length(&cchwTmp);
	pxid->m_qtsb->ReplaceRgch(cchwTmp, cchwTmp, prgchw, cchw, qttp);

	pxid->m_stuChars.Append(prgchw, cchw);
}

/*----------------------------------------------------------------------------------------------
	Handle XML start tags inside <Str> and <AStr> elements during the second pass.
	This function is passed to the expat XML parser as a callback function when the start tag
	for either <Str> or <AStr> is detected.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::HandleStringStartTag(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	StrAnsi staMsg;

	WpXmlImportData * pxid = reinterpret_cast<WpXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);
	try
	{
		if (strcmp(pszName, "AStr") == 0 || strcmp(pszName, "Str") == 0)
		{
			// <AStr ws="ENG">...</AStr>
			// <Str>...</Str>
			//
			// This has already been handled: SHOULD NEVER REACH HERE!
			pxid->CreateErrorMessage(
				"<%s> elements cannot be nested inside either <Str> or <AStr>.", pszName);
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		else if (strcmp(pszName, "Run") == 0)		// Preferred internal string element.
		{
			pxid->m_qtpbStr.CreateInstance(CLSID_TsPropsBldr);
			pxid->m_qtpbStr->SetIntPropValues(ktptWs, ktpvDefault, pxid->m_wsUser);
			if (pxid->m_fBetaXML)
			{
				// Assume file is either all Beta-style XML, or all true version 1.0 XML.
				pxid->m_fBetaXML = false;
				pxid->m_qtsb.CreateInstance(CLSID_TsStrBldr);

			}
			pxid->m_fInRun = true;
			HandleTextPropStartTag(pxid, pszName, prgpszAtts, pxid->m_qtpbStr);
		}
		else if (strcmp(pszName, "Prop") == 0)		// Deprecated: for compatibility.
		{
			pxid->m_qtpbStr.CreateInstance(CLSID_TsPropsBldr);
			pxid->m_qtpbStr->SetIntPropValues(ktptWs, ktpvDefault, pxid->m_wsUser);
			pxid->m_fPropSeen = true;
			HandleTextPropStartTag(pxid, pszName, prgpszAtts, pxid->m_qtpbStr);
		}
		else
		{
			// Do nothing?
		}
	}
	catch (Throwable & thr)
	{
		pxid->m_fError = true;
		pxid->m_hr = thr.Error();
#ifdef DEBUG
		staMsg.Format("ERROR CAUGHT on line %d of %s: %s",
			__LINE__, __FILE__, AsciiHresult(pxid->m_hr));
		pxid->LogMessage(staMsg.Chars());
#endif
	}
	catch (...)
	{
		pxid->m_fError = true;
		pxid->m_hr = E_FAIL;
#ifdef DEBUG
		staMsg.Format("UNKNOWN ERROR CAUGHT on line %d of %s", __LINE__, __FILE__);
		pxid->LogMessage(staMsg.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Handle XML start tags inside <Str>, <StyleRules>, or <StStyle> tags. Fill in a text-prop
	builder with the information.

	TODO: put the messages in a resource?

	@return true if a valid ws property was set, false otherwise.
----------------------------------------------------------------------------------------------*/
bool WpXmlImportData::HandleTextPropStartTag(WpXmlImportData * pxid, const XML_Char * pszName,
	const XML_Char ** prgpszAtts, ITsPropsBldr * ptpb)
{
	/*
		<!-- string property group: if an attribute is missing, that implies
		that it is not set for the following run of characters.

		fontsize and offset contain (unsigned) decimal integers.
		fontsizeUnit is used only if fontsize is set.  It defaults to "mpt".
		offsetUnit is used only if offset is set.  It defaults to "mpt".
			These Unit attributes may have additional values that have not yet
			been defined, and that may not actually be "units".
		underline has the values "none" or "single" at the moment.  Other
			values may be added: an (unsigned) decimal number is allowed.
		forecolor and backcolor can have these values:
			"white" | "black" | "red" | "green" | "blue" | "yellow" | "magenta" |
			"cyan" | "transparent" | <8 digit hexadecimal number>
		ws and wsBase contain valid writing system strings as defined elsewhere.
		fontFamily and charStyle contain arbitrary strings which are stored verbatim.
		-->
		<!ELEMENT Prop EMPTY>
		<!ATTLIST Prop
			ws           CDATA                #IMPLIED
			italic       (off | on | invert)  #IMPLIED
			bold         (off | on | invert)  #IMPLIED
			superscript  (off | super | sub)  #IMPLIED
			underline    CDATA                #IMPLIED
			fontsize     CDATA                #IMPLIED
			fontsizeUnit CDATA                #IMPLIED
			offset       CDATA                #IMPLIED
			offsetUnit   CDATA                #IMPLIED
			forecolor    CDATA                #IMPLIED
			backcolor    CDATA                #IMPLIED
			wsBase       CDATA                #IMPLIED
			fontFamily   CDATA                #IMPLIED
			charStyle    CDATA                #IMPLIED
			externalLink CDATA                #IMPLIED
		>
	 */

	StrAnsi staNeed;
	bool fValidWs = false;

	// Integer-valued properties.
	const char * pszValueLim;
	const char * pszBackcolor = FwXml::GetAttributeValue(prgpszAtts, "backcolor");
	if (pszBackcolor)
	{
		int nVal = FwXml::DecodeTextColor(pszBackcolor, &pszValueLim);
		if (*pszValueLim)
		{
			// ERROR.
		}
		ptpb->SetIntPropValues(ktptBackColor, ktpvDefault, nVal);
	}

	const char * pszBold = FwXml::GetAttributeValue(prgpszAtts, "bold");
	if (pszBold)
	{
		int nVal = FwXml::DecodeTextToggleVal(pszBold, &pszValueLim);
		Assert(kttvOff < kttvForceOn && kttvInvert > kttvForceOn);
		if (*pszValueLim || nVal < kttvOff || nVal > kttvInvert)
		{
			// "off, on, or invert"
			staNeed.Load(kstidWpXmlErrMsg028);
			// "Invalid value in <Prop %<0>s='%<1>s'/>; need %<2>s"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg027, "bold", pszBold, staNeed.Chars());
		}
		else
		{
			ptpb->SetIntPropValues(ktptBold, ktpvEnum, nVal);
		}
	}

	const char * pszOldEnc = FwXml::GetAttributeValue(prgpszAtts, "enc"); // WPX version 1
	const char * pszWs = FwXml::GetAttributeValue(prgpszAtts, "ws");
	const char * pszOws = FwXml::GetAttributeValue(prgpszAtts, "ows");
	StrAnsi staIsoWs;
	if (pszOldEnc) // WPX version 1
	{
		if (strcmp(pszOldEnc, "___") != 0)
		{
			StrUni stuOldEnc(pszOldEnc);
			StrUni stuIsoEnc = SilUtil::ConvertEthnologueToISO(stuOldEnc.Chars());
			staIsoWs = stuIsoEnc;
			int tpt = ktptWs;
			int nVar = pszWs ? strtoul(pszWs, NULL, 10) : 0;
			int nVal;
			fValidWs = pxid->m_hmcws.Retrieve(staIsoWs.Chars(), &nVal);
			if (!fValidWs)
				// "Cannot convert '%<0>s' into a Language Writing system code"
				pxid->CreatePropErrorMsg(kstidWpXmlErrMsg032, pszOldEnc);
			else
				ptpb->SetIntPropValues(tpt, nVar, nVal);
		}
	}
	else if (pszWs)
	{
		if (strcmp(pszWs, "___") != 0)
		{
			StrUni stuWs(pszWs);
			StrUni stuIsoWs = SilUtil::ConvertEthnologueToISO(stuWs.Chars());
			staIsoWs = stuIsoWs;
			int tpt = ktptWs;
			int nVar = pszOws ? strtoul(pszOws, NULL, 10) : 0;
			int nVal;
			fValidWs = pxid->m_hmcws.Retrieve(staIsoWs.Chars(), &nVal);
			if (!fValidWs)
			{
				// We don't need to alert the user about this for WsProp -- old WPX files have
				// the WsProp for every writing system WorldPad knew about, but contain only the
				// writing systems that were actually used in the current document.
				if (strcmp(pszName, "WsProp") != 0)
				{
					// "Cannot convert '%<0>s' into a Language Writing system code"
					pxid->CreatePropErrorMsg(kstidWpXmlErrMsg032, pszWs);
				}
				else
				{
					StrAnsi staFmt(kstidWpXmlErrMsg032);
					StrAnsi staMsg;
					staMsg.Format(staFmt.Chars(), pszWs);
					pxid->LogMessage(staMsg.Chars());
				}
			}
			else
			{
				ptpb->SetIntPropValues(tpt, nVar, nVal);
			}
		}
	}
	else if (pszOws)
	{
		// "Ignoring <Prop %<0>s='%<1>s' in the absence of %<2>s"
		pxid->CreatePropErrorMsg(kstidWpXmlErrMsg031, "ows", pszOws, "ws");
	}

	const char * pszWsBase = FwXml::GetAttributeValue(prgpszAtts, "wsBase");
	const char * pszOwsBase = FwXml::GetAttributeValue(prgpszAtts, "owsBase");
	if (pszWsBase)
	{
		if (strcmp(pszWsBase, "___") != 0)
		{
			StrUni stuWs(pszWsBase);
			StrUni stuIsoWs = SilUtil::ConvertEthnologueToISO(stuWs.Chars());
			staIsoWs = stuIsoWs;
			int tpt = ktptBaseWs;
			int nVar = pszOwsBase ? strtoul(pszOwsBase, NULL, 10) : 0;
			int nVal;
			bool fValidWsBase = pxid->m_hmcws.Retrieve(staIsoWs.Chars(), &nVal);
			if (!fValidWsBase)
				// "Cannot convert '%<0>s' into a Language Writing system code"
				pxid->CreatePropErrorMsg(kstidWpXmlErrMsg032, pszWs);
			else
				ptpb->SetIntPropValues(tpt, nVar, nVal);
		}
	}
	else if (pszOwsBase)
	{
		// "Ignoring <Prop %<0>s='%<1>s' in the absence of %<2>s"
		pxid->CreatePropErrorMsg(kstidWpXmlErrMsg031, "owsBase", pszOwsBase, "wsBase");
	}

	const char * pszFontsize = FwXml::GetAttributeValue(prgpszAtts, "fontsize");
	const char * pszFontsizeUnit = FwXml::GetAttributeValue(prgpszAtts, "fontsizeUnit");
	if (pszFontsize)
	{
		bool fError = false;
		char * psz;
		unsigned nSize = strtoul(pszFontsize, &psz, 10);
		if (*psz)
		{
			if (!pszFontsizeUnit)
			{
				if (strcmp(psz, "mpt") == 0)
					pszFontsizeUnit = psz;
				else
					fError = true;
			}
			else
			{
				fError = true;
			}
		}
		if (psz == pszFontsize || nSize > 0xFFFFFF)
		{
			fError = true;
		}
		if (fError)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "fontsize", pszFontsize);
		int tpv = ktpvDefault;
		if (pszFontsizeUnit)
		{
			if (strcmp(pszFontsizeUnit, "mpt") == 0)
			{
				tpv = ktpvMilliPoint;
			}
			else
			{
				// REVIEW SteveMc: should we default to an unsigned int like this?
				tpv = strtoul(pszFontsizeUnit, &psz, 10);
				if (*psz || tpv > 0xFF)
				{
					// "Invalid value in <Prop %<0>s='%<1>s'/>"
					pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "fontsizeUnit",
						pszFontsizeUnit);
					fError = true;
				}
			}
		}
		if (!fError)
		{
			ptpb->SetIntPropValues(ktptFontSize, tpv, nSize);
		}
	}
	else if (pszFontsizeUnit)
	{
		// "Ignoring <Prop %<0>s='%<1>s' in the absence of %<2>s"
		pxid->CreatePropErrorMsg(kstidWpXmlErrMsg031, "fontsizeUnit", pszFontsizeUnit,
			"fontsize");
	}

	const char * pszForecolor = FwXml::GetAttributeValue(prgpszAtts, "forecolor");
	if (pszForecolor)
	{
		int nVal = FwXml::DecodeTextColor(pszForecolor, &pszValueLim);
		if (*pszValueLim)
		{
			// ERROR
		}
		ptpb->SetIntPropValues(ktptForeColor, ktpvDefault, nVal);
	}

	const char * pszItalic = FwXml::GetAttributeValue(prgpszAtts, "italic");
	if (pszItalic)
	{
		int nVal = FwXml::DecodeTextToggleVal(pszItalic, &pszValueLim);
		Assert(kttvOff < kttvForceOn && kttvInvert > kttvForceOn);
		if (*pszValueLim || nVal < kttvOff || nVal > kttvInvert)
		{
			// "off, on, or invert"
			staNeed.Load(kstidWpXmlErrMsg028);
			// "Invalid value in <Prop %<0>s='%<1>s'/>; need %<2>s"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg027, "italic", pszItalic, staNeed.Chars());
		}
		else
		{
			ptpb->SetIntPropValues(ktptItalic, ktpvEnum, nVal);	// enum was default
		}
	}

	const char * pszOffset = FwXml::GetAttributeValue(prgpszAtts, "offset");
	const char * pszOffsetUnit = FwXml::GetAttributeValue(prgpszAtts, "offsetUnit");
	if (pszOffset)
	{
		bool fError = false;
		char * psz;
		unsigned nSize = strtoul(pszOffset, &psz, 10);
		if (*psz)
		{
			if (!pszOffsetUnit)
			{
				if (strcmp(psz, "mpt") == 0)
					pszOffsetUnit = psz;
				else
					fError = true;
			}
			else
			{
				fError = true;
			}
		}
		if (psz == pszOffset || nSize > 0xFFFFFF)
		{
			fError = true;
		}
		if (fError)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "offset", pszOffset);
		int tpv = ktpvDefault;
		if (pszOffsetUnit)
		{
			if (strcmp(pszOffsetUnit, "mpt") == 0)
			{
				tpv = ktpvMilliPoint;
			}
			else
			{
				// REVIEW SteveMc: should we default to an unsigned int like this?
				tpv = strtoul(pszOffsetUnit, &psz, 10);
				if (*psz || tpv > 0xFF)
				{
					// "Invalid value in <Prop %<0>s='%<1>s'/>"
					pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "offsetUnit", pszOffset);
					fError = true;
				}
			}
		}
		if (!fError)
		{
			ptpb->SetIntPropValues(ktptOffset, tpv, nSize);
		}
	}
	else if (pszOffsetUnit)
	{
		// "Ignoring <Prop %<0>s='%<1>s' in the absence of %<2>s"
		pxid->CreatePropErrorMsg(kstidWpXmlErrMsg031, "offsetUnit", pszOffsetUnit, "offset");
	}

	const char * pszSuperscript = FwXml::GetAttributeValue(prgpszAtts, "superscript");
	if (pszSuperscript)
	{
		int nVal = FwXml::DecodeSuperscriptVal(pszSuperscript, &pszValueLim);
		Assert(kssvOff < kssvSuper && kssvSub > kssvSuper);
		if (*pszValueLim || nVal < kssvOff || nVal > kssvSub)
		{
			// "off, super, or sub"
			staNeed.Load(kstidWpXmlErrMsg029);
			// "Invalid value in <Prop %<0>s='%<1>s'/>; need %<2>s"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg027, "superscript", pszSuperscript,
				staNeed.Chars());
		}
		else
		{
			ptpb->SetIntPropValues(ktptSuperscript, ktpvEnum, nVal);
		}
	}

	const char * pszUndercolor = FwXml::GetAttributeValue(prgpszAtts, "undercolor");
	if (pszUndercolor)
	{
		int nVal = FwXml::DecodeTextColor(pszUndercolor, &pszValueLim);
		if (*pszValueLim)
		{
			// ERROR
		}
		ptpb->SetIntPropValues(ktptUnderColor, ktpvDefault, nVal);
	}

	const char * pszUnderline = FwXml::GetAttributeValue(prgpszAtts, "underline");
	if (pszUnderline)
	{
		int nVal = FwXml::DecodeUnderlineType(pszUnderline, &pszValueLim);
		if (*pszValueLim || nVal < kuntMin || nVal >= kuntLim)
		{
			// "none, single, double, dotted, dashed, squiggle, or strikethrough"
			staNeed.Load(kstidWpXmlErrMsg030);
			// "Invalid value in <Prop %<0>s='%<1>s'/>; need %<2>s"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg027, "underline", pszUnderline,
				staNeed.Chars());
		}
		else
		{
			ptpb->SetIntPropValues(ktptUnderline, ktpvEnum, nVal);
		}
	}

	// String-valued properties.
	const char * pszCharStyle = FwXml::GetAttributeValue(prgpszAtts, "charStyle");
	if (pszCharStyle)
	{
		StrUni stuTmp(pszCharStyle);
		ptpb->SetStrPropValue(ktptCharStyle, stuTmp.Bstr());
	}

	const char * pszNamedStyle = FwXml::GetAttributeValue(prgpszAtts, "namedStyle");
	if (pszNamedStyle)
	{
		StrUni stuTmp(pszNamedStyle);
		ptpb->SetStrPropValue(ktptNamedStyle, stuTmp.Bstr());
	}

	const char * pszFontFamily = FwXml::GetAttributeValue(prgpszAtts, "fontFamily");
	if (pszFontFamily)
	{
		StrUni stuTmp(pszFontFamily);
		ptpb->SetStrPropValue(ktptFontFamily, stuTmp.Bstr());
	}

	const char * pszFontVariations = FwXml::GetAttributeValue(prgpszAtts, "fontVariations");
	if (pszFontVariations)
	{
		StrUni stuTmp(pszFontVariations);
		ptpb->SetStrPropValue(ktptFontVariations, stuTmp.Bstr());
	}

	const char * pszExtLink = FwXml::GetAttributeValue(prgpszAtts, "externalLink");
	if (pszExtLink)
	{
		StrUni stuTmp(pszExtLink);
		stuTmp.ReplaceFill(0, 0, (wchar)kodtExternalPathName, 1);
		ptpb->SetStrPropValue(ktptObjData, stuTmp.Bstr());
	}

	// Paragraph-level properties; they are all integer values.

	const char * pszAlign = FwXml::GetAttributeValue(prgpszAtts, "align");
	if (pszAlign)
	{
		bool fError = false;
		char * psz = NULL;
		unsigned nVal;
		if (!strcmp(pszAlign, "leading"))
			nVal = ktalLeading;
		else if (!strcmp(pszAlign, "left"))
			nVal = ktalLeft;
		else if (!strcmp(pszAlign, "center"))
			nVal = ktalCenter;
		else if (!strcmp(pszAlign, "right"))
			nVal = ktalRight;
		else if (!strcmp(pszAlign, "trailing"))
			nVal = ktalTrailing;
		else if (!strcmp(pszAlign, "justify"))
			nVal = ktalJustify;
		else
		{
			nVal = strtoul(pszAlign, &psz, 10);
			if (*psz || nVal < ktalMin || nVal >= ktalLim)
				fError = true;
		}
		if (psz == pszAlign || nVal > 0xFFFFFF || fError)
		{
			// "leading, trailing, left, center, right, or justify"
			staNeed.Load(kstidWpXmlErrMsg036);
			// "Invalid value in <Prop %<0>s='%<1>s'/>; need %<2>s"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg027, "align", pszAlign,
				staNeed.Chars());
		}
		else
			ptpb->SetIntPropValues(ktptAlign, ktpvEnum, nVal);
	}

	const char * pszFirstIndent = FwXml::GetAttributeValue(prgpszAtts, "firstIndent");
	if (pszFirstIndent)
	{
		char * psz;
		int nVal = strtoul(pszFirstIndent, &psz, 10); // might be a signed value
		if (psz == pszFirstIndent || nVal > 0xFFFFFF || nVal < (0xFFFFFF * -1))
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "firstIndent", pszFirstIndent);
		else
			ptpb->SetIntPropValues(ktptFirstIndent, ktpvMilliPoint, nVal);
	}

	const char * pszLeadingIndent = FwXml::GetAttributeValue(prgpszAtts, "leadingIndent");
	if (pszLeadingIndent)
	{
		char * psz;
		unsigned nVal = strtoul(pszLeadingIndent, &psz, 10);
		if (psz == pszLeadingIndent || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "leadingIndent", pszLeadingIndent);
		else
			ptpb->SetIntPropValues(ktptLeadingIndent, ktpvMilliPoint, nVal);
	}

	const char * pszTrailingIndent = FwXml::GetAttributeValue(prgpszAtts, "trailingIndent");
	if (pszTrailingIndent)
	{
		char * psz;
		unsigned nVal = strtoul(pszTrailingIndent, &psz, 10);
		if (psz == pszTrailingIndent || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "trailingIndent", pszTrailingIndent);
		else
			ptpb->SetIntPropValues(ktptTrailingIndent, ktpvMilliPoint, nVal);
	}

	const char * pszSpaceBefore = FwXml::GetAttributeValue(prgpszAtts, "spaceBefore");
	if (pszSpaceBefore)
	{
		char * psz;
		unsigned nVal = strtoul(pszSpaceBefore, &psz, 10);
		if (psz == pszSpaceBefore || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "spaceBefore", pszSpaceBefore);
		else
			ptpb->SetIntPropValues(ktptSpaceBefore, ktpvMilliPoint, nVal);
	}

	const char * pszSpaceAfter = FwXml::GetAttributeValue(prgpszAtts, "spaceAfter");
	if (pszSpaceAfter)
	{
		char * psz;
		unsigned nVal = strtoul(pszSpaceAfter, &psz, 10);
		if (psz == pszSpaceAfter || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "spaceAfter", pszSpaceAfter);
		else
			ptpb->SetIntPropValues(ktptSpaceAfter, ktpvMilliPoint, nVal);
	}

	const char * pszTabDef = FwXml::GetAttributeValue(prgpszAtts, "tabDef");
	if (pszTabDef)
	{
		char * psz;
		unsigned nVal = strtoul(pszTabDef, &psz, 10);
		if (psz == pszTabDef || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "tabDef", pszTabDef);

		else
			ptpb->SetIntPropValues(ktptTabDef, ktpvMilliPoint, nVal);
	}

	const char * pszLineHeight = FwXml::GetAttributeValue(prgpszAtts, "lineHeight");
	const char * pszLineHeightUnit = FwXml::GetAttributeValue(prgpszAtts, "lineHeightUnit");
	if (pszLineHeight)
	{
		bool fError = false;
		char * psz;
		// Line height may be a signed number (negative means exact).
		int nSize = strtoul(pszLineHeight, &psz, 10);
		if (*psz)
		{
			if (!pszLineHeightUnit)
			{
				if ((strcmp(psz, "mpt") == 0) || (strcmp(psz, "rel") == 0))
				{
					pszLineHeightUnit = psz;
				}
				else
					fError = true;
			}
			else
			{
				fError = true;
			}
		}
		if (psz == pszLineHeight || abs(nSize) > 0xFFFFFF)
		{
			fError = true;
		}
		if (fError)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "lineHeight", pszLineHeight);

		int tpv = ktpvDefault;
		if (pszLineHeightUnit)
		{
			if (strcmp(pszLineHeightUnit, "mpt") == 0)
			{
				tpv = ktpvMilliPoint;
			}
			else if (strcmp(pszLineHeightUnit, "rel") == 0)
			{
				tpv = ktpvRelative;
			}
			else
			{
				// REVIEW SteveMc: should we default to an unsigned int like this?
				tpv = strtoul(pszLineHeightUnit, &psz, 10);
				if (*psz || tpv > 0xFF)
				{
					// "Invalid value in <Prop %<0>s='%<1>s'/>"
					pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "lineHeightUnit",
						pszLineHeightUnit);
					fError = true;
				}
			}
		}
		if (!fError)
		{
			ptpb->SetIntPropValues(ktptLineHeight, tpv, nSize);
		}
	}
	else if (pszLineHeightUnit)
	{
		// "Ignoring <Prop %<0>s='%<1>s' in the absence of %<2>s"
		pxid->CreatePropErrorMsg(kstidWpXmlErrMsg031, "lineHeightUnit", pszLineHeightUnit,
			"lineHeight");
	}

	const char * pszParacolor = FwXml::GetAttributeValue(prgpszAtts, "paracolor");
	if (pszParacolor)
	{
		int nVal = FwXml::DecodeTextColor(pszParacolor, &pszValueLim);
		if (*pszValueLim)
		{
			// ERROR
		}
		ptpb->SetIntPropValues(ktptParaColor, ktpvDefault, nVal);
	}

	//	Properties from thes views subsystem

	const char * pszRightToLeft = FwXml::GetAttributeValue(prgpszAtts, "rightToLeft");
	if (pszRightToLeft)
	{
		char * psz;
		unsigned nVal = strtoul(pszRightToLeft, &psz, 10);
		if (psz == pszRightToLeft || (nVal != 1 && nVal != 0))
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "rightToLeft", pszRightToLeft);

		else
			ptpb->SetIntPropValues(ktptRightToLeft, ktpvEnum, nVal);
	}

	const char * pszBorderTop = FwXml::GetAttributeValue(prgpszAtts, "borderTop");
	if (pszBorderTop)
	{
		char * psz;
		unsigned nVal = strtoul(pszBorderTop, &psz, 10);
		if (psz == pszBorderTop || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "borderTop", pszBorderTop);
		else
			ptpb->SetIntPropValues(ktptBorderTop, ktpvMilliPoint, nVal);
	}

	const char * pszBorderBottom = FwXml::GetAttributeValue(prgpszAtts, "borderBottom");
	if (pszBorderBottom)
	{
		char * psz;
		unsigned nVal = strtoul(pszBorderBottom, &psz, 10);
		if (psz == pszBorderBottom || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "borderBottom", pszBorderBottom);
		else
			ptpb->SetIntPropValues(ktptBorderBottom, ktpvMilliPoint, nVal);
	}

	const char * pszBorderLeading = FwXml::GetAttributeValue(prgpszAtts, "borderLeading");
	if (pszBorderLeading)
	{
		char * psz;
		unsigned nVal = strtoul(pszBorderLeading, &psz, 10);
		if (psz == pszBorderLeading || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "borderLeading", pszBorderLeading);
		else
			ptpb->SetIntPropValues(ktptBorderLeading, ktpvMilliPoint, nVal);
	}

	const char * pszBorderTrailing = FwXml::GetAttributeValue(prgpszAtts, "borderTrailing");
	if (pszBorderTrailing)
	{
		char * psz;
		unsigned nVal = strtoul(pszBorderTrailing, &psz, 10);
		if (psz == pszBorderTrailing || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "borderTrailing", pszBorderTrailing);
		else
			ptpb->SetIntPropValues(ktptBorderTrailing, ktpvMilliPoint, nVal);
	}

	const char * pszBorderColor = FwXml::GetAttributeValue(prgpszAtts, "borderColor");
	if (pszBorderColor)
	{
		int nVal = FwXml::DecodeTextColor(pszBorderColor, &pszValueLim);
		if (*pszValueLim)
		{
			// ERROR
		}
		ptpb->SetIntPropValues(ktptBorderColor, ktpvDefault, nVal);
	}

	const char * pszBulNumScheme = FwXml::GetAttributeValue(prgpszAtts, "bulNumScheme");
	if (pszBulNumScheme)
	{
		char * psz;
		unsigned nVal = strtoul(pszBulNumScheme, &psz, 10);
		if (psz == pszBulNumScheme || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "bulNumScheme", pszBulNumScheme);
		else
			ptpb->SetIntPropValues(ktptBulNumScheme, ktpvEnum, nVal);
	}

	const char * pszBulNumStartAt = FwXml::GetAttributeValue(prgpszAtts, "bulNumStartAt");
	if (pszBulNumStartAt)
	{
		char * psz;
		unsigned nVal = strtoul(pszBulNumStartAt, &psz, 10);
		if (psz == pszBulNumStartAt || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "bulNumStartAt", pszBulNumStartAt);
		else
			ptpb->SetIntPropValues(ktptBulNumStartAt, ktpvDefault, nVal);
	}

	const char * pszBulNumTxtBef = FwXml::GetAttributeValue(prgpszAtts, "bulNumTxtBef");
	if (pszBulNumTxtBef)
	{
		StrUni stuTmp(pszBulNumTxtBef);
		ptpb->SetStrPropValue(ktptBulNumTxtBef, stuTmp.Bstr());
	}

	const char * pszBulNumTxtAft = FwXml::GetAttributeValue(prgpszAtts, "bulNumTxtAft");
	if (pszBulNumTxtAft)
	{
		StrUni stuTmp(pszBulNumTxtAft);
		ptpb->SetStrPropValue(ktptBulNumTxtAft, stuTmp.Bstr());
	}

	const char * pszBulNumFontInfo = FwXml::GetAttributeValue(prgpszAtts, "bulNumFontInfo");
	if (pszBulNumFontInfo)
	{
		StrUni stuTmp = ReadXmlBinaryHexString(pxid, pszBulNumFontInfo, "bulNumFontInfo");
		ptpb->SetStrPropValue(ktptBulNumFontInfo, stuTmp.Bstr());
	}

	// pad values are automatically generated for borders.
	const char * pszPadLeading = FwXml::GetAttributeValue(prgpszAtts, "padLeading");
	if (pszPadLeading)
	{
		char * psz;
		unsigned nVal = strtoul(pszPadLeading, &psz, 10);
		if (psz == pszPadLeading || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "padLeading", pszPadLeading);
		else
			ptpb->SetIntPropValues(ktptPadLeading, ktpvMilliPoint, nVal);
	}

	const char * pszPadTrailing = FwXml::GetAttributeValue(prgpszAtts, "padTrailing");
	if (pszPadTrailing)
	{
		char * psz;
		unsigned nVal = strtoul(pszPadTrailing, &psz, 10);
		if (psz == pszPadTrailing || nVal > 0xFFFFFF)
			// "Invalid value in <Prop %<0>s='%<1>s'/>"
			pxid->CreatePropErrorMsg(kstidWpXmlErrMsg026, "padTrailing", pszPadTrailing);
		else
			ptpb->SetIntPropValues(ktptPadTrailing, ktpvMilliPoint, nVal);
	}

	const char * pszKeepWithNext = FwXml::GetAttributeValue(prgpszAtts, "keepWithNext");
	if (pszKeepWithNext)
	{
		char * psz;
		unsigned nVal = strtoul(pszKeepWithNext, &psz, 10);
		if (psz != pszKeepWithNext)
			ptpb->SetIntPropValues(ktptKeepWithNext, ktpvEnum, nVal);
	}

	const char * pszKeepTogether = FwXml::GetAttributeValue(prgpszAtts, "keepTogether");
	if (pszKeepTogether)
	{
		char * psz;
		unsigned nVal = strtoul(pszKeepTogether, &psz, 10);
		if (psz != pszKeepTogether)
			ptpb->SetIntPropValues(ktptKeepTogether, ktpvEnum, nVal);
	}

	const char * pszWidowOrphanControl = FwXml::GetAttributeValue(prgpszAtts, "widowOrphanremakefw");
	if (pszWidowOrphanControl)
	{
		char * psz;
		unsigned nVal = strtoul(pszWidowOrphanControl, &psz, 10);
		if (psz != pszWidowOrphanControl)
			ptpb->SetIntPropValues(ktptWidowOrphanControl, ktpvEnum, nVal);
	}

	return fValidWs;
}

/*----------------------------------------------------------------------------------------------
	Set up an error message that has to do with setting string properties.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::CreatePropErrorMsg(int stid, const char * pszProp, const char * pszVal,
	const char * pszProp2)
{
	StrAnsi staRes(stid);
	StrAnsi staMsg;

	if (pszProp2)
		staMsg.Format(staRes.Chars(), pszProp, pszVal, pszProp2);
	else if (pszVal)
		staMsg.Format(staRes.Chars(), pszProp, pszVal);
	else
		staMsg.Format(staRes.Chars(), pszProp ? pszProp : "");

	CreateErrorMessage(staMsg.Chars());
}

/*----------------------------------------------------------------------------------------------
	Convert a hex string into a Unicode string that actually contains binary data.
----------------------------------------------------------------------------------------------*/
StrUni WpXmlImportData::ReadXmlBinaryHexString(WpXmlImportData * pxid, const char * pszHex,
	StrAnsi staAttName)
{
	Vector<byte> vbBin;
	byte rgbBin[1024];
	byte ch;
	byte bT;

	int cch = strlen(pszHex);
	int cbBin = cch / 2;
	byte * prgbBin;
	if (cbBin <= isizeof(rgbBin))
	{
		prgbBin = rgbBin;
	}
	else
	{
		vbBin.Resize(cbBin);
		prgbBin = vbBin.Begin();
	}

	for (int ib = 0, ich = 0; ich < cch; )
	{
		// Read the first Hex digit of the byte.  It may be preceded by one or
		// more whitespace characters.
		do
		{
			ch = pszHex[ich];
			++ich;
			if (!isascii(ch))
				ThrowHr(WarnHr(E_UNEXPECTED));
		} while (isspace(ch) && ich < cch);
		if (ich == cch)
		{
			if (!isspace(ch))
			{
				// "Warning: ignoring extra character at the end of %<0>s data"
				StrAnsi staMsg(kstidWpXmlErrMsg033);
				pxid->CreateErrorMessage(staMsg.Chars(), staAttName.Chars());
			}
			break;
		}
		if (!isxdigit(ch))
			ThrowHr(WarnHr(E_UNEXPECTED));

		if (isdigit(ch))
			bT = static_cast<byte>((ch & 0xF) << 4);
		else
			bT = static_cast<byte>(((ch & 0xF) + 9) << 4);

		// Read the second Hex digit of the byte.
		ch = pszHex[ich];
		++ich;
		if (!isascii(ch) || !isxdigit(ch))
			ThrowHr(WarnHr(E_UNEXPECTED));

		if (isdigit(ch))
			bT |= ch & 0xF;
		else
			bT |= (ch & 0xF) + 9;
		prgbBin[ib] = bT;
		++ib;
	}

	OLECHAR * pcchw = reinterpret_cast<OLECHAR *>(prgbBin);
	StrUni stuRet(pcchw, (cbBin / isizeof(OLECHAR)));
	return stuRet;
}

/*----------------------------------------------------------------------------------------------
	Handle XML end tags inside <Str> and <AStr> elements during the second pass.
	This function is passed to the expat XML parser as a callback function when the start tag
	for either <Str> or <AStr> is detected.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::HandleStringEndTag(void * pvUser, const XML_Char * pszName)
{
	WpXmlImportData * pxid = reinterpret_cast<WpXmlImportData *>(pvUser);
	AssertPtr(pxid);
	Assert(pxid->m_parser);
	if (strcmp(pszName, "Str") == 0 || strcmp(pszName, "AStr") == 0)
	{
		if (pxid->m_stuChars.Length() == 0 && pxid->m_qtpbStr)
		{
			// Create empty string with specific properties set.
			ITsTextPropsPtr qttp;
			pxid->m_qtpbStr->GetTextProps(&qttp);
			ITsStringPtr qtss;
			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			qtsf->MakeStringWithPropsRgch(L"", 0, qttp, &qtss);
			qtss->GetBldr(&(pxid->m_qtsb));
		}

		//	Clear the current text properties.
		pxid->m_qtpbStr = NULL;
		// Return to the normal processing.
		XML_SetElementHandler(pxid->m_parser,
			WpXmlImportData::HandleStartTag,
			WpXmlImportData::HandleEndTag);
		HandleEndTag(pvUser, pszName);
		return;
	}
	else if (strcmp(pszName, "Run") == 0)			// Preferred internal string element.
	{
		pxid->m_fInRun = false;
	}
	else if (strcmp(pszName, "Prop") == 0)	// Deprecated: for compatibility.
	{
		// Do nothing: <Prop> inside <Str> was an empty element used to set properties for the
		// following PCDATA characters for our first pass at writing system strings within XML.
	}
	else
	{
		// We should have already complained about this invalid element.
	}
}

/*----------------------------------------------------------------------------------------------
	If the log file is empty, delete it.
----------------------------------------------------------------------------------------------*/
void WpXmlImportData::DeleteEmptyLogFile()
{
	if (m_cErrMsg == 0 && m_pfileLog)
	{
		fclose(m_pfileLog);
		::DeleteFileA(m_stabpLog.Chars());
		m_pfileLog = NULL;
	}
}

/*----------------------------------------------------------------------------------------------
	Load the document from the given XML file.  The document must be initially empty.
----------------------------------------------------------------------------------------------*/
HRESULT WpDa::LoadXml(BSTR bstrFile, WpMainWnd * pwpwnd, WpMainWnd * pwpwndLauncher, int * pfls)
{
	AssertPtrN(bstrFile);
	if (!bstrFile)
	{
		*pfls = kflsAborted;
		ReturnHr(E_INVALIDARG);
	}

	StrAnsi staRes;
	StrAnsi staMsg;
	StrApp strWp(kstidAppName);

	WpStylesheetPtr qwpsts = dynamic_cast<WpStylesheet *>(pwpwnd->GetStylesheet());
	WpXmlImportData xid(this, pwpwnd, pwpwndLauncher, qwpsts, false);
	const LARGE_INTEGER libMove = {0,0};
	ULARGE_INTEGER ulibPos;
	ulong cbRead;
	void * pBuffer;
	const XML_Char * pszWritingSystem = NULL;
	try
	{
		// Open the input file and create the log file.
		FileStream::Create(bstrFile, STGM_READ, &xid.m_qstrm);
		STATSTG statFile;
		CheckHr(xid.m_qstrm->Stat(&statFile, STATFLAG_NONAME));
		xid.m_stabpFile = bstrFile;
		xid.m_stabpLog = bstrFile;
		int ich = xid.m_stabpLog.ReverseFindCh('.');
		if (ich != -1)
			xid.m_stabpLog.SetLength(ich);
		xid.m_stabpLog.Append("-Import.log");
		fopen_s(&xid.m_pfileLog, xid.m_stabpLog.Chars(), "w");

		//  Since there are no reference attributes, we only need one pass through the file
		//	(unlike the standard XML import routine which uses two passes).

		//  Create a parser to scan over the file to create the basic objects/ids.
		xid.m_parser = XML_ParserCreate(pszWritingSystem);
		XML_SetUserData(xid.m_parser, &xid);
		if (!XML_SetBase(xid.m_parser, xid.m_stabpFile.Chars()))
		{
			// "Out of memory before parsing anything"
			StrAnsi staRes(kstidWpXmlErrMsg022);
			xid.CreateErrorMessage(staRes.Chars());
			ThrowHr(WarnHr(E_OUTOFMEMORY));
		}
		XML_SetExternalEntityRefHandler(xid.m_parser, WpXmlImportData::HandleExternalEntityRef);
		XML_SetElementHandler(xid.m_parser,
			WpXmlImportData::HandleStartTag, WpXmlImportData::HandleEndTag);
		XML_SetCharacterDataHandler(xid.m_parser, WpXmlImportData::HandleCharData);

		// Store the default (user interface) writing system id.
		ILgWritingSystemFactoryPtr qwsf;
		qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
		CheckHr(qwsf->get_UserWs(&xid.m_wsUser));

		// Initialize the writing system hashmap with the user interface writing system.
		// If the UI ever allows true multilingual names, abbreviations, and/or descriptions for
		// writing systems, we'll have to go to a 2-pass parse of the file, the first pass to
		// collect and create all of the writing systems, filling in our hashmap as it goes, and
		// the second pass to do what the single pass does now.
		////StrAnsi staUserWs(kstidUserWs);
		////xid.m_hmcws.Insert(staUserWs.Chars(), xid.m_wsUser);

		// Actually this needs to be done for all the existing writing systems!
		int cwsTotal;
		CheckHr(qwsf->get_NumberOfWs(&cwsTotal));
		int * prgws = NewObj int[cwsTotal];
		CheckHr(qwsf->GetWritingSystems(prgws, cwsTotal));
		for (int iws = 0; iws < cwsTotal; iws++)
		{
			SmartBstr sbstr;
			HRESULT hr;
			CheckHr(hr = qwsf->GetStrFromWs(prgws[iws], &sbstr));

			StrUni stuWs(sbstr.Chars());
			StrAnsi staWs(stuWs);
			xid.m_hmcws.Insert(staWs.Chars(), prgws[iws], true);
		}
		delete[] prgws;

		// Process the XML file.
		for (;;)
		{
			pBuffer = XML_GetBuffer(xid.m_parser, READ_SIZE);
			if (!pBuffer)
			{
				xid.CreateErrorMessage(
					"Cannot get buffer from the XML parser! (Out of memory?)");
				*pfls = kflsAborted;
				return E_UNEXPECTED;
			}
			CheckHr(xid.m_qstrm->Read(pBuffer, READ_SIZE, &cbRead));
			char * pch = (char *)pBuffer;
			if (*pch == 0xFFFFFFEF && *(pch + 1) == 0xFFFFFFBB &&
				*(pch + 2) == 0xFFFFFFBF)
			{
				// We need to skip the UTF marker. I don't think there's any way to adjust
				// the pointer into the buffer, so instead move the contents of the buffer
				// so that what we want is in the expected starting position.
				memmove(pch, pch + 3, cbRead - 3);
				memset(pch + cbRead - 3, 0, 3);
				cbRead -= 3;
			}
			if (!XML_ParseBuffer(xid.m_parser, cbRead, cbRead == 0))
			{
				// "XML parser detected an XML syntax error"
				staRes.Load(kstidWpXmlErrMsg023);
				xid.CreateErrorMessage(staRes);
				break;
			}
			if (xid.m_fError)
			{
				// "Error detected while parsing XML file"
				staRes.Load(kstidWpXmlErrMsg024);
				xid.CreateErrorMessage(staRes);
				break;
			}
			CheckHr(xid.m_qstrm->Seek(libMove, STREAM_SEEK_CUR, &ulibPos));
			if ((ulibPos.HighPart == statFile.cbSize.HighPart) &&
				(ulibPos.LowPart == statFile.cbSize.LowPart))
			{
				// Successfully processed the XML file.
				Assert(xid.m_celemEnd <= xid.m_celemStart);
				if (xid.m_celemStart != xid.m_celemEnd)
				{
					// "Error in termination of file"
					staRes.Load(kstidWpXmlErrMsg025);
					xid.CreateErrorMessage(staRes);
				}
				break;
			}
		}
		CleanupWritingSystems(xid.m_vpvNewWs);

		int nRet = xid.FinalErrorMessage();
		if (nRet == IDCANCEL)
			*pfls = kflsAborted;
		else if (xid.m_cErrMsg > 0)
			*pfls = kflsPartial;
		else
			*pfls = kflsOkay;

		XML_ParserFree(xid.m_parser);
		xid.m_parser = 0;

		xid.DeleteEmptyLogFile();

		if (xid.m_cErrMsg > 0)
			return E_UNEXPECTED;
		else
			return S_OK;
	}
	catch (Throwable & thr)
	{
#ifdef DEBUG
		StrAnsi staMsg;
		staMsg.Format("ERROR CAUGHT on line %d of %s: %s",
			__LINE__, __FILE__, AsciiHresult(thr.Error()));
		xid.LogMessage(staMsg.Chars());
#endif
		xid.AbortErrorMessage();
		*pfls = kflsAborted;
		return thr.Error();
	}
	catch (...)
	{
#ifdef DEBUG
		StrAnsi staMsg;
		staMsg.Format("UNKNOWN ERROR CAUGHT on line %d of %s", __LINE__, __FILE__);
		xid.LogMessage(staMsg.Chars());
#endif
		xid.AbortErrorMessage();
		*pfls = kflsAborted;
		return WarnHr(E_FAIL);
	}
}

/*----------------------------------------------------------------------------------------------
	NB (JohnT): this routine is responsible to do a Release on each object in vpvNewEnc
	(which are actually pointers to writing systems).
----------------------------------------------------------------------------------------------*/
void WpDa::CleanupWritingSystems(Vector<void *> & vpvNewWs)
{
	for (int iws = 0; iws < vpvNewWs.Size(); iws++)
	{
		IUnknown * punk = reinterpret_cast<IUnknown *>(vpvNewWs[iws]);
		//	Clean up the reference count (originally from the open-object list of void *'s).
		punk->Release();
	}
}

/*----------------------------------------------------------------------------------------------
	Create an writing system by parsing an XML string, which was read from the registry or a file.
	Return true if we skipped reading an alternative of a multistring (without reporting it,
	because fReportMissingWs is true).
----------------------------------------------------------------------------------------------*/
bool WpDa::ParseXmlForWritingSystem(StrUni & stuInput, StrApp * pstrFile, bool fReportMissingWs)
{
	if (!stuInput.Length())
		return false;
	// The input is actually UTF-8 with NUL bytes between each character byte, not UTF-16.
	// Repack the data properly, and call the companion method.
	Vector<char> vch;
	vch.Resize(stuInput.Length() + 1);
	for (int ich = 0; ich < stuInput.Length(); ++ich)
	{
		Assert((unsigned)stuInput[ich] < 256);
		vch[ich] = static_cast<char>(stuInput[ich]);
	}
	StrAnsi staInput(vch.Begin(), stuInput.Length());
	return ParseXmlForWritingSystem(staInput, pstrFile, fReportMissingWs);
}
bool WpDa::ParseXmlForWritingSystem(StrAnsi & staInput, StrApp * pstrFile, bool fReportMissingWs)
{
	StrAnsi staRes;
	StrAnsi staMsg;

	if (!staInput.Length())
		return false;					// Nothing to parse??

	WpXmlImportData xid(this, NULL, NULL, NULL, true);
	xid.m_fReportMissingWs = fReportMissingWs;
	if (pstrFile != NULL)
		xid.m_stabpFile.Assign((*pstrFile).Chars());	// Store file name for possible error message.
	const LARGE_INTEGER libMove = {0,0};
	ULARGE_INTEGER ulibPos;
	ulong cbRead;
	void * pBuffer;
	const XML_Char * pszWritingSystem = NULL;
	try
	{
		// Create a stream on the input and create the log file.
		StrAnsiStream * pstas;
		StrAnsiStream::Create(&pstas);
		// Kludge to pretend like we are in a complete XML file.
		pstas->m_sta.Format("<WpDoc>%n<Languages>%n%s%n</Languages>%n</WpDoc>%n",
			staInput.Chars());
		ulong cbEncoding = pstas->m_sta.Length();
		pstas->QueryInterface(IID_IStream, (void **)&xid.m_qstrm);

		//  Since there are no reference attributes, we only need one pass through the file
		//	(unlike the standard XML import routine which uses two passes).

		//  Create a parser to scan over the file to create the basic objects/ids.
		xid.m_parser = XML_ParserCreate(pszWritingSystem);
		XML_SetUserData(xid.m_parser, &xid);
		if (!XML_SetBase(xid.m_parser, xid.m_stabpFile.Chars()))
		{
			// // "Out of memory before parsing anything"
			staRes.Load(kstidWpXmlErrMsg022);
			xid.CreateErrorMessage(staRes);
			return false; // E_OUTOFMEMORY;
		}
		XML_SetExternalEntityRefHandler(xid.m_parser, WpXmlImportData::HandleExternalEntityRef);
		XML_SetElementHandler(xid.m_parser,
			WpXmlImportData::HandleStartTag,
			WpXmlImportData::HandleEndTag);
		XML_SetCharacterDataHandler(xid.m_parser, WpXmlImportData::HandleCharData);

		// Store the default (user interface) writing system id.
		ILgWritingSystemFactoryPtr qwsf;
		qwsf.CreateInstance(CLSID_LgWritingSystemFactory);	// Get the memory-based factory.
		CheckHr(qwsf->get_UserWs(&xid.m_wsUser));

		// Initialize the writing system hashmap with the user interface writing system.
		// If the UI ever allows true multilingual names, abbreviations, and/or descriptions for
		// writing systems, we'll have to insert all of the writing systems already known to
		// qwsf.
		StrAnsi staUserWs(kstidUserWs);
		xid.m_hmcws.Insert(staUserWs.Chars(), xid.m_wsUser);

		// Process the XML string.
		for (;;)
		{
			pBuffer = XML_GetBuffer(xid.m_parser, READ_SIZE);
			if (!pBuffer)
			{
				xid.CreateErrorMessage(
					"Cannot get buffer from the XML parser! (Out of memory?)");
				return false; // E_UNEXPECTED;
			}
			CheckHr(xid.m_qstrm->Read(pBuffer, READ_SIZE, &cbRead));
			if (!XML_ParseBuffer(xid.m_parser, cbRead, cbRead == 0))
			{
				// "XML parser detected an XML syntax error"
				staRes.Load(kstidWpXmlErrMsg023);
				xid.CreateErrorMessage(staRes);
				return false; // E_FAIL;
			}
			if (xid.m_fError)
			{
				// "Error detected while parsing XML file"
				staRes.Load(kstidWpXmlErrMsg024);
				xid.CreateErrorMessage(staRes);
				return false; // xid.m_hr;
			}
			CheckHr(xid.m_qstrm->Seek(libMove, STREAM_SEEK_CUR, &ulibPos));
			if ((ulibPos.HighPart == 0L) && (ulibPos.LowPart == cbEncoding))
			{
				// Successfully processed the XML file.
				Assert(xid.m_celemEnd <= xid.m_celemStart);
				if (xid.m_celemStart != xid.m_celemEnd)
				{
					// "Error in termination of file"
					staRes.Load(kstidWpXmlErrMsg025);
					xid.CreateErrorMessage(staRes);
				}
				break;
			}
		}

		// Clean up. The closed-list should contain encodings, which have already
		// been stored in the writing system factory. Put them on the new-encoding list (which
		// now owns the reference).
		Assert(xid.m_vpvClosed.Size() == xid.m_vwpcClosed.Size());
		for (int iws = 0; iws < xid.m_vpvClosed.Size(); iws++)
		{
			IUnknown * punk = reinterpret_cast<IUnknown *>(xid.m_vpvClosed[iws]);
			IWritingSystemPtr qws;
			CheckHr(punk->QueryInterface(IID_IWritingSystem, (void**)&qws));
			xid.m_vpvNewWs.Push(xid.m_vpvClosed[iws]);
		}
		xid.m_vpvClosed.Clear();
		xid.m_vwpcClosed.Clear();

		XML_ParserFree(xid.m_parser);
		xid.m_parser = 0;

		// pass NULL so we don't bother with error messages.
		CleanupWritingSystems(xid.m_vpvNewWs);

		pstas->Release();
		return xid.m_fSkippedMissingWs;
	}
	catch (Throwable & thr)
	{
#ifdef DEBUG
		staMsg.Format("ERROR CAUGHT on line %d of %s: %s", __LINE__, AsciiHresult(thr.Error()));
		xid.LogMessage(staMsg.Chars());
#else
		thr.Error();
#endif
	}
	catch (...)
	{
#ifdef DEBUG
		staMsg.Format("UNKNOWN ERROR CAUGHT on line %d of %s", __LINE__, __FILE__);
		xid.LogMessage(staMsg.Chars());
#endif
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Create an writing system by parsing a Language Definition (XML) file.
	Return true if we suppressed a warning about a string alternative we could not save
	because of an unknown writing system.
----------------------------------------------------------------------------------------------*/
bool WpDa::ParseLanguageDefinitionFile(StrApp & strFile, bool fReportMissingWs)
{
	// 1. Load the Language Definition file in its entirety.

	HANDLE hFile = ::CreateFile(strFile.Chars(), GENERIC_READ, FILE_SHARE_READ, NULL,
		OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	DWORD cchFileHigh = 0;
	DWORD cchFile = ::GetFileSize(hFile, &cchFileHigh);
	if (cchFileHigh)
		return false;				// The file is way too big!!
	if (!cchFile)
		return false;				// The file is too small!
	Vector<char> vch;
	vch.Resize(cchFile + 1);
	DWORD cchRead = 0;
	BOOL fOk = ::ReadFile(hFile, vch.Begin(), cchFile, &cchRead, NULL);
	if (fOk)
		fOk = ::CloseHandle(hFile);
	if (!fOk || cchFile != cchRead)
		return false;				// We had trouble reading/closing the file.
	vch[cchFile] = 0;		// NUL terminate the input buffer.

	// 2. Extract the <LgWritingSystem>...</LgWritingSystem>

	const char kszBegin1[] = "<LgWritingSystem ";	// allow attributes
	const char kszBegin2[] = "<LgWritingSystem>";	// allow no attributes
	const char kszEnd[] = "</LgWritingSystem>";
	char * pszBegin = strstr(vch.Begin(), kszBegin1);
	if (!pszBegin)
		pszBegin = strstr(vch.Begin(), kszBegin2);
	if (!pszBegin)
		return false;
	char * pszEnd = strstr(pszBegin, kszEnd);
	if (!pszEnd)
		return false;
	int cch = pszEnd - pszBegin + strlen(kszEnd);
	StrAnsi staInput(pszBegin, cch);

	// 3. Parse this writing system. Pass the file name for the sake of possible error message.

	return ParseXmlForWritingSystem(staInput, &strFile, fReportMissingWs);
}
