/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright (c) 2004-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: FwXmlString.cpp
Responsibility: Steve McConnel
Last reviewed:

VERY IMPORTANT NOTE:
	This file implements the namespace FwXml methods used to parse strings.  It is designed to
	be #included in a master C++ file, since it's exact implementation depends on the definition
	of the FwXmlImportData class, which can vary according to what specific type of XML data is
	being read.  That is why there are no #include statements in this file!!
----------------------------------------------------------------------------------------------*/

/*----------------------------------------------------------------------------------------------
	Check that the data writer has enough room to store the string property.  If it doesn't,
	reallocate the vector used for the data writer's storage.  This should be needed only for
	"Big Strings", and then rarely.
----------------------------------------------------------------------------------------------*/
static void VerifyDataLength(DataWriterRgb & dwr, TextProps::TextStrProp & txsp,
	Vector<byte> & vbProps)
{
	int ibCur = dwr.IbCur();
	Assert(vbProps.Size() >= ibCur);
	int cbNeed = ibCur + 9 + txsp.m_stuVal.Length() * isizeof(wchar);
	if (cbNeed >= vbProps.Size())
	{
		vbProps.Resize(cbNeed + 4000);
		dwr.Init(vbProps.Begin(), vbProps.Size());
		dwr.SeekAbs(ibCur);
	}
}


/*----------------------------------------------------------------------------------------------
	Add this integer valued property to the vector of integer properties, or set its value if it
	is already in the vector.  NOTE THAT THIS VECTOR MUST BE SORTED BY m_scp.

	@param txip Reference to a data structure used for storing the integer valued text property.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetIntegerProperty(TextProps::TextIntProp & txip)
{
	// Perform a binary search for inserting/changing.
	int iMin = 0;
	int iLim = m_vtxip.Size();
	while (iMin < iLim)
	{
		int iT = (iMin + iLim) >> 1;
		if (m_vtxip[iT].m_scp < txip.m_scp)
			iMin = iT + 1;
		else
			iLim = iT;
	}
	if (iMin < m_vtxip.Size() && m_vtxip[iMin].m_scp == txip.m_scp)
	{
		m_vtxip[iMin].m_nVal = txip.m_nVal;
		m_vtxip[iMin].m_nVar = txip.m_nVar;
	}
	else
	{
		m_vtxip.Insert(iMin, txip);
	}
}

/*----------------------------------------------------------------------------------------------
	Add this string valued property to the vector of string properties, or set its value if it
	is already in the vector.  NOTE THAT THIS VECTOR MUST BE SORTED BY m_tpt.

	@param txsp Reference to a data structure used for storing the string valued text property.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetStringProperty(TextProps::TextStrProp & txsp)
{
	// Perform a binary search for inserting/changing.
	int iMin = 0;
	int iLim = m_vtxsp.Size();
	while (iMin < iLim)
	{
		int iT = (iMin + iLim) >> 1;
		if (m_vtxsp[iT].m_tpt < txsp.m_tpt)
			iMin = iT + 1;
		else
			iLim = iT;
	}
	if (iMin < m_vtxsp.Size() && m_vtxsp[iMin].m_tpt == txsp.m_tpt)
	{
		m_vtxsp[iMin].m_stuVal = txsp.m_stuVal;
	}
	else
	{
		m_vtxsp.Insert(iMin, txsp);
	}
}


/*----------------------------------------------------------------------------------------------
	Add this guid valued property to the vector of string properties, or set its value if it
	is already in the vector.  NOTE THAT THIS VECTOR MUST BE SORTED BY m_tpt.

	@param tgvp Reference to a data structure used for storing the guid valued text property.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetStringProperty(FwXml::TextGuidValuedProp & tgvp)
{
	TextProps::TextStrProp txsp;
	wchar * prgch;
	txsp.m_tpt = tgvp.m_tpt;
	if (tgvp.m_tpt == kstpTags)
	{
		// Store list of GUIDS derived from the IDREFS.
		txsp.m_stuVal.Clear();
		int cchTags = tgvp.m_vguid.Size() * isizeof(GUID) / isizeof(wchar);
		txsp.m_stuVal.SetSize(cchTags, &prgch);
		for (int iguid = 0; iguid < tgvp.m_vguid.Size(); ++iguid)
		{
			memcpy(prgch, &tgvp.m_vguid[iguid], sizeof(GUID));
			prgch += isizeof(GUID) / isizeof(wchar);
		}
	}
	else if (tgvp.m_tpt == kstpObjData)
	{
		// Store GUID derived from the link attribute value.
		Assert(tgvp.m_vguid.Size() == 1);
		txsp.m_stuVal.Clear();
		txsp.m_stuVal.SetSize((isizeof(GUID) / isizeof(wchar)) + 1, &prgch);
		prgch[0] = tgvp.m_chType;
		memcpy(prgch + 1, &tgvp.m_vguid[0], isizeof(GUID));
	}
	else
	{
		// THIS SHOULD NEVER HAPPEN!
		Assert(tgvp.m_tpt == kstpTags || tgvp.m_tpt == kstpObjData);
		return;				// Don't store anything.
	}
	SetStringProperty(txsp);
}

#ifdef XML_UNICODE
#define CompareXml wcscmp
#define LitXml(x) L ## x
#define StrXmlChars StrUni
#else
#define CompareXml strcmp
#define LitXml(x) x
#define StrXmlChars StrAnsi
#endif

/*----------------------------------------------------------------------------------------------
	Handle XML start tags inside <Str> and <AStr> elements.  For FwXmlData::LoadXml(), this is
	used for the second pass.

	This static method is passed to the expat XML parser as a callback function.  It is used
	when the start tag for either <Str> or <AStr> is detected.  See the comments in xmlparse.h
	for the XML_StartElementHandler typedef for the documentation such as it is.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXml::HandleStringStartTag(void * pvUser, const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);

	pxid->ProcessStringStartTag(pszName, prgpszAtts);
}

/*----------------------------------------------------------------------------------------------
	The following data and static method are used to look up attribute names found in a <Run>
	element.  The array must be kept sorted because the method uses a binary search algorithm.
----------------------------------------------------------------------------------------------*/
struct AttributeInfo
{
	const XML_Char * m_pszName;
	int m_nCode;
	wchar m_chType;
	bool m_fIsStringProp;
};
#ifdef XML_UNICODE
	#define STRTYPE OleStringLiteral
#else
	#define STRTYPE char *
#endif
	static const STRTYPE backcolor(LitXml("backcolor"));
	static const STRTYPE bold(LitXml("bold"));
	static const STRTYPE bulNumFontInfo(LitXml("bulNumFontInfo"));
	static const STRTYPE bulNumTxtAft(LitXml("bulNumTxtAft"));
	static const STRTYPE bulNumTxtBef(LitXml("bulNumTxtBef"));
	static const STRTYPE charStyle(LitXml("charStyle"));
	static const STRTYPE contextString(LitXml("contextString"));
	static const STRTYPE embedded(LitXml("embedded"));
	static const STRTYPE externalLink(LitXml("externalLink"));
	static const STRTYPE fontFamily(LitXml("fontFamily"));
	static const STRTYPE fontVariations(LitXml("fontVariations"));
	static const STRTYPE fontsize(LitXml("fontsize"));
	static const STRTYPE fontsizeunit(LitXml("fontsizeunit"));
	static const STRTYPE forecolor(LitXml("forecolor"));
	static const STRTYPE italic(LitXml("italic"));
	static const STRTYPE link_(LitXml("link"));
	static const STRTYPE moveableObj(LitXml("moveableObj"));
	static const STRTYPE namedStyle(LitXml("namedStyle"));
	static const STRTYPE offset(LitXml("offset"));
	static const STRTYPE offsetunit(LitXml("offsetunit"));
	static const STRTYPE ownlink(LitXml("ownlink"));
	static const STRTYPE paraStyle(LitXml("paraStyle"));
	static const STRTYPE superscript(LitXml("superscript"));
	static const STRTYPE tabList(LitXml("tabList"));
	static const STRTYPE tags(LitXml("tags"));
	static const STRTYPE type(LitXml("type"));
	static const STRTYPE undercolor(LitXml("undercolor"));
	static const STRTYPE underline(LitXml("underline"));
	static const STRTYPE ws_(LitXml("ws"));
	static const STRTYPE wsBase(LitXml("wsBase"));
	static const STRTYPE wsStyle(LitXml("wsStyle"));

static AttributeInfo g_rgatti[] = {
	{ backcolor,		kscpBackColor,		0,	false},
	{ bold,				kscpBold,			0,	false},
	{ bulNumFontInfo,	kstpBulNumFontInfo,	0,						true},
	{ bulNumTxtAft,		kstpBulNumTxtAft,	0,						true},
	{ bulNumTxtBef,		kstpBulNumTxtBef,	0,						true},
	{ charStyle,		kstpCharStyle,		0,						true},
	{ contextString,	kstpObjData,		kodtContextString,		true},
	{ embedded,			kstpObjData,		kodtEmbeddedObjectData,	true},
	{ externalLink,		kstpObjData,		kodtExternalPathName,	true},
	{ fontFamily,		kstpFontFamily,		0,						true},
	{ fontVariations,	kstpFontVariations,	0,						true},
	{ fontsize,			kscpFontSize,		0,	false},
	{ fontsizeunit,		0,					0,	false},	// ignored if found
	{ forecolor,		kscpForeColor,		0,	false},
	{ italic,			kscpItalic,			0,	false},
	{ link_,				kstpObjData,		kodtNameGuidHot,		true},
	{ moveableObj,		kstpObjData,		kodtGuidMoveableObjDisp,true},
	{ namedStyle,		kstpNamedStyle,		0,						true},
	{ offset,			kscpOffset,			0,	false},
	{ offsetunit,		0,					0,	false},	// ignored if found
	{ ownlink,			kstpObjData,		kodtOwnNameGuidHot,		true},
	{ paraStyle,		kstpParaStyle,		0,						true},
	{ superscript,		kscpSuperscript,	0,	false},
	{ tabList,			kstpTabList,		0,						true},
	{ tags,				kstpTags,			0,						true},
	{ type,				0,					0,						true},	// special case
	{ undercolor,		kscpUnderColor,		0,	false},
	{ underline,		kscpUnderline,		0,	false},
	{ ws_,				kscpWs,				0,	false},
	{ wsBase,			kscpBaseWs,			0,	false},
	{ wsStyle,			kstpWsStyle,		0,						true},
};
static const int g_crgatti = isizeof(g_rgatti) / isizeof(AttributeInfo);
static AttributeInfo g_aiBad = { NULL, -1, NULL, false };
static AttributeInfo FindAttributeInfo(const XML_Char * pszAttName)
{
	int iMin = 0;
	int iLim = g_crgatti;
	int iMid;
	int ncmp;
	// Perform a binary search
	while (iMin < iLim)
	{
		iMid = (iMin + iLim) >> 1;
		ncmp = CompareXml(g_rgatti[iMid].m_pszName, pszAttName);
		if (ncmp == 0)
		{
			return g_rgatti[iMid];
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
	return g_aiBad;
}

/*----------------------------------------------------------------------------------------------
	Handle XML start tags inside <Str> and <AStr> elements.  For FwXmlData::LoadXml(), this is
	used for the second pass.

	The following attributes are possible for these elements:

		type         - defines how to interpret the PCDATA.  This list will probably be
						extended in the future.  The default is "chars".  The other
						attributes may not be meaningful if type is not equal to "chars",
						but it may be useful to carry them along for use when inserting a
						new run of character data following the special run.

	If any of the other attributes is missing, that implies that it is not set for this run of
	characters.

		backcolor      - contains one of these values:
						 "white" | "black" | "red" | "green" | "blue" | "yellow" |
						 "magenta" | "cyan" | "transparent" | <8 digit hexadecimal number>
		bold           - contains one of the listed values.
		charStyle      - contains an arbitrary string which is stored verbatim.
		ws             - contains a valid writing system string as defined elsewhere, or an
						 empty string to indicate that it is not defined.
		wsBase         - contains a valid writing system string as defined elsewhere, or an
						 empty string to indicate that it is not defined.
		fontFamily     - contains an arbitrary string which is stored verbatim.
		fontsize       - contains an unsigned decimal integer.
		fontsizeUnit   - is used only if fontsize is set.  It defaults to "mpt".
		forecolor      - contains one of these values:
						 "white" | "black" | "red" | "green" | "blue" | "yellow" |
						 "magenta" | "cyan" | "transparent" | <8 digit hexadecimal number>
		italic         - contains one of the listed values.
		link           - contains a GUID that references an object elsewhere, most often
						 in this same database.
		offset         - contains an unsigned decimal integer.
		offsetUnit     - is used only if offset is set.  It defaults to "mpt".
		paraStyle      - contains an arbitrary string which is stored verbatim.
		superscript    - contains one of the listed values.
		tabList        - contains a comma-delimited list of unsigned decimal numbers.
		tags           - contains one or more ID values assigned to other objects in the
						 database.
		undercolor     - contains one of these values:
						 "white" | "black" | "red" | "green" | "blue" | "yellow" |
						 "magenta" | "cyan" | "transparent" | <8 digit hexadecimal number>
		underline      - contains one of the listed values.
		fontVariations - contains an arbitrary string which is stored verbatim.
		namedStyle     - contains an arbitrary string which is stored verbatim.
		bulNumTxtBef   - contains an arbitrary string which is stored verbatim.
		bulNumTxtAft   - contains an arbitrary string which is stored verbatim.
		bulNumFontInfo - contains special encoded property information (not really a
						 "string" as such).
		wsStyle        - contains an arbitrary string which is stored verbatim.

	Note that the unit attributes may have additional values that have not yet been defined, and
	that may not actually be "units".

	@param pszName XML element name read from the input file.
	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ProcessStringStartTag(const XML_Char * pszName,
	const XML_Char ** prgpszAtts)
{
	StrAnsi sta;
	StrAnsi staFmt;
	try
	{
		static const STRTYPE run(LitXml("Run"));
		static const STRTYPE picture(LitXml("picture"));
		static const STRTYPE chars(LitXml("chars"));
		static const STRTYPE aStr(LitXml("AStr"));
		static const STRTYPE str(LitXml("Str"));
		if (CompareXml(pszName, run) == 0)
		{
			m_fInRun = true;
			m_bri.m_ichMin = m_stuChars.Length();
			m_fRunHasChars = false;
			m_rdt = FwXml::krdtChars;
			if (prgpszAtts == NULL)
				return;
			for (int i = 0; prgpszAtts[i]; i += 2)
			{
				const XML_Char * pszAttr = prgpszAtts[i];
				const XML_Char * pszValue = prgpszAtts[i+1];
				if (pszValue == NULL)
					continue;	// SHOULD NEVER HAPPEN!
				// ws is by far the most common attribute, so check it first before
				// trying the binary search.
				if (CompareXml(pszAttr, ws_) == 0)
				{
					SetTextWs(kscpWs, pszValue);
					continue;
				}
				AttributeInfo atti = FindAttributeInfo(pszAttr);
				if (atti.m_nCode < 0)
					continue;	// error???
				if (atti.m_fIsStringProp)
				{
					if (atti.m_nCode == 0)
					{
						Assert(CompareXml(pszAttr, type) == 0);
						if (CompareXml(pszValue, picture) == 0)
						{
							m_rdt = FwXml::krdtPicture;
						}
						else if (CompareXml(pszValue, chars) != 0)
						{
							// "Invalid value in <Run type=\"%s\">: need chars or picture"
							staFmt.Load(kstidXmlErrorMsg073);
							sta.Format(staFmt.Chars(), pszValue);
							LogMessage(sta.Chars());
						}
					}
					else
					{
						SetStringProperty(atti.m_nCode, pszValue, atti.m_chType);
					}
				}
				else
				{
					switch (atti.m_nCode)
					{
					case kscpWs:
					case kscpBaseWs:
						SetTextWs(atti.m_nCode, pszValue);
						break;
					case kscpBackColor:
					case kscpForeColor:
					case kscpUnderColor:
						SetTextColor(atti.m_nCode, pszValue);
						break;
					case kscpBold:
					case kscpItalic:
						SetTextToggle(atti.m_nCode, pszValue, pszAttr);
						break;
					case kscpFontSize:
						SetTextMetric(kscpFontSize, pszValue, prgpszAtts, fontsizeunit, pszAttr);
						break;
					case kscpOffset:
						SetTextMetric(kscpOffset, pszValue, prgpszAtts, offsetunit, pszAttr);
						break;
					case kscpSuperscript:
						SetTextSuperscript(pszValue);
						break;
					case kscpUnderline:
						SetTextUnderline(pszValue);
						break;
					}
				}
			}
		}
		else if (CompareXml(pszName, aStr) == 0 || CompareXml(pszName, str) == 0)
		{
			// <AStr ws="ENG">...</AStr>
			// <Str>...</Str>
			// This has already been handled: SHOULD NEVER REACH HERE!
			// "<%s> elements cannot be nested inside either <Str> or <AStr>!"
			staFmt.Load(kstidXmlErrorMsg001);
			sta.Format(staFmt.Chars(), pszName);
			LogMessage(sta.Chars());
			ThrowHr(WarnHr(E_UNEXPECTED));
		}
		else
		{			// Do nothing?
		}
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		staFmt.Load(kstidXmlDebugMsg003);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		staFmt.Load(kstidXmlDebugMsg005);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	If the given attribute is present in the XML element, add its value to the current run's
	set of integer values.

	@param scp - code for this scalar (integer) valued property
	@param pszColor - color property value
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextColor(int scp, const XML_Char * pszColor)
{
	TextProps::TextIntProp txip;
	const XML_Char * pszValueLim;
	txip.m_nVal = FwXml::DecodeTextColor(pszColor, &pszValueLim);
	if (*pszValueLim)
	{
		// ERROR
	}
	txip.m_scp = scp;
	txip.m_nVar = 0;
	SetIntegerProperty(txip);
}

/*----------------------------------------------------------------------------------------------
	If the given attribute is present in the XML element, add its value to the current run's
	set of integer values.

	@param scp - code for this scalar (integer) valued property
	@param pszToggle - toggle valued property value
	@param pszAttName - attribute name (used for error message)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextToggle(int scp, const XML_Char * pszToggle, const XML_Char * pszAttName)
{
	TextProps::TextIntProp txip;
	const XML_Char * pszValueLim;
	txip.m_nVal = FwXml::DecodeTextToggleVal(pszToggle, &pszValueLim);
	Assert(kttvOff < kttvForceOn && kttvInvert > kttvForceOn);
	if (*pszValueLim || txip.m_nVal < kttvOff || txip.m_nVal > kttvInvert)
	{
		// "Invalid value in <Run %s=\"%s\">: need on, off or invert"
		StrAnsi staFmt(kstidXmlErrorMsg066);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), pszAttName, pszToggle);
		LogMessage(sta.Chars());
	}
	else
	{
		txip.m_scp = scp;
		txip.m_nVar = ktpvEnum;
		SetIntegerProperty(txip);
	}
}

/*----------------------------------------------------------------------------------------------
	If the given attribute is present in the XML element, add its value to the current run's
	set of integer values.

	@param scp - code for this scalar (integer) valued property
	@param pszWs - writing system property value
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextWs(int scp, const XML_Char * pszWs)
{
	TextProps::TextIntProp txip;
	// "Cannot convert \"%s\" into a Language Writing system code."
	txip.m_nVal = GetWsFromIcuLocale(pszWs, kstidXmlErrorMsg011);
	if (txip.m_nVal)
	{
		txip.m_scp = scp;
		txip.m_nVar = 0;
		SetIntegerProperty(txip);
	}
}

/*----------------------------------------------------------------------------------------------
	If the given attribute is present in the XML element, add its value to the current run's
	set of integer values.

	@param scp - code for this scalar (integer) valued property
	@param pszVal - metric property value
	@param prgpszAtts - Pointer to NULL-terminated array of name / value pairs of strings.
	@param pszUnitAttName - related XML attribute name for the unit used for the metric
	@param pszAttName - primary attribute name (used for error message)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextMetric(int scp, const XML_Char * pszVal, const XML_Char ** prgpszAtts,
	const XML_Char * pszUnitAttName, const XML_Char * pszAttName)
{
	TextProps::TextIntProp txip;
	StrAnsi staFmt;
	StrAnsi sta;
	const XML_Char * pszUnitVal = FwXml::GetAttributeValue(prgpszAtts, pszUnitAttName);
	bool fError = false;
	XML_Char * psz;
#ifdef XML_UNICODE
	unsigned nSize = Utf16StrToUL(pszVal, const_cast<const XML_Char **>(&psz), 10);
#else
	unsigned nSize = strtoul(pszVal, &psz, 10);
#endif
	static const STRTYPE mpt(LitXml("mpt"));
	if (*psz)
	{
		if (!pszUnitVal)
		{
			if (!CompareXml(psz, mpt))
				pszUnitVal = psz;
			else
				fError = true;
		}
		else
		{
			fError = true;
		}
	}
	if (psz == pszVal || nSize > 0xFFFFFF)
	{
		fError = true;
	}
	if (fError)
	{
		// "Invalid value in <Run %s=\"%s\">."
		staFmt.Load(kstidXmlErrorMsg067);
		sta.Format(staFmt.Chars(), pszAttName, pszVal);
		LogMessage(sta.Chars());
	}
	int tpv = ktpvDefault;
	if (pszUnitVal)
	{
		if (!CompareXml(pszUnitVal, mpt))
		{
			tpv = ktpvMilliPoint;
		}
		else
		{
			// REVIEW SteveMc: should we default to an unsigned int like this?
#ifdef XML_UNICODE
			tpv = Utf16StrToUL(pszUnitVal, const_cast<const XML_Char **>(&psz), 10);
#else
			tpv = strtoul(pszUnitVal, &psz, 10);
#endif
			if (*psz || tpv > 0xFF)
			{
				// "Invalid value in <Run %s=\"%s\">."
				staFmt.Load(kstidXmlErrorMsg068);
				sta.Format(staFmt.Chars(), pszUnitAttName, pszUnitVal);
				LogMessage(sta.Chars());
				fError = true;
			}
		}
	}
	if (!fError)
	{
		txip.m_scp = scp;
		txip.m_nVal = nSize;
		txip.m_nVar = tpv;
		SetIntegerProperty(txip);
	}
}

/*----------------------------------------------------------------------------------------------
	If the superscript attribute is present in the XML element, add its value to the current
	run's set of integer values.

	@param pszSuperscript - superscript property value.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextSuperscript(const XML_Char * pszSuperscript)
{
	TextProps::TextIntProp txip;
	const XML_Char * pszValueLim;
	txip.m_nVal = FwXml::DecodeSuperscriptVal(pszSuperscript, &pszValueLim);
	Assert(kssvOff < kssvSuper && kssvSub > kssvSuper);
	if (*pszValueLim || txip.m_nVal < kssvOff || txip.m_nVal > kssvSub)
	{
		// "Invalid value in <Run superscript=\"%s\">: need off, super, or sub"
		StrAnsi staFmt(kstidXmlErrorMsg072);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), pszSuperscript);
		LogMessage(sta.Chars());
	}
	else
	{
		txip.m_scp = kscpSuperscript;
		txip.m_nVar = ktpvEnum;
		SetIntegerProperty(txip);
	}
}

/*----------------------------------------------------------------------------------------------
	If the underline attribute is present in the XML element, add its value to the current run's
	set of integer values.

	@param pszUnderline - underline property value
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextUnderline(const XML_Char * pszUnderline)
{
	TextProps::TextIntProp txip;
	const XML_Char * pszValueLim;
	txip.m_nVal = FwXml::DecodeUnderlineType(pszUnderline, &pszValueLim);
	if (*pszValueLim || txip.m_nVal < kuntMin || txip.m_nVal >= kuntLim)
	{
		// "Invalid value in <Run underline=\"%s\">: need none, single, double, dotted, dashed, strikethrough"
		StrAnsi staFmt(kstidXmlErrorMsg074);
		StrAnsi sta;
		sta.Format(staFmt.Chars(), pszUnderline);
		LogMessage(sta.Chars());
	}
	else
	{
		txip.m_scp = kscpUnderline;
		txip.m_nVar = ktpvEnum;
		SetIntegerProperty(txip);
	}
}

/*----------------------------------------------------------------------------------------------
	If the given attribute is present in the XML element, add its value to the current run's
	set of string values.

	@param stp - code for this string valued property
	@param pszVal - value of this string valued property
	@param chType - optional (defaults to 0) type flag for the property
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetStringProperty(int stp, const XML_Char * pszVal, wchar chType)
{
	StrAnsi sta;
	StrAnsi staFmt;
	if (stp == kstpTags)
	{
		SetTagsAsStringProp(stp, pszVal);
	}
	else if (stp == kstpObjData)
	{
		SetObjDataAsStringProp(stp, pszVal, chType);
	}
	else if (stp == kstpBulNumFontInfo)
	{
		// FIXME?
		Assert(stp != kstpBulNumFontInfo);		// This shouldn't appear in a <Run> element!
	}
	else
	{
		TextProps::TextStrProp txsp;
		txsp.m_tpt = stp;

		// Convert the string from UTF-8 to UTF-16 and store the Unicode characters.
#ifdef XML_UNICODE
		txsp.m_stuVal = pszVal;
#else
		StrUtil::StoreUtf16FromUtf8(pszVal, strlen(pszVal), txsp.m_stuVal);
#endif
		SetStringProperty(txsp);
	}
}


/*----------------------------------------------------------------------------------------------
	Add this Tags valued property to the vector of string properties, or set its value if it
	is already in the vector.  NOTE THAT THIS VECTOR MUST BE SORTED BY m_tpt.

	@param stp Code of the string valued property.
	@param pszVal Value of the string valued text property.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTagsAsStringProp(int stp, const XML_Char * pszVal)
{
		FwXml::TextGuidValuedProp tgvp;
		tgvp.m_tpt = stp;

		// convert the IDs in pszVal into GUIDs and store them.
		char szBuffer[400];				// Use stack temp space for smaller amounts.
		Vector<char> vch;
		char * prgch;
#ifdef XML_UNICODE
		int cchW = wcslen(pszVal);
		int cch = ::WideCharToMultiByte(CP_UTF8, 0, pszVal, cchW, NULL, 0, NULL, NULL);
#else
		int cch = strlen(pszVal);
#endif
		if (cch < 400)
		{
			prgch = szBuffer;
		}
		else
		{
			vch.Resize(cch + 1);		// Too much for stack: use temp heap storage.
			prgch = &vch[0];
		}
#ifdef XML_UNICODE
		::WideCharToMultiByte(CP_UTF8, 0, pszVal, cchW, prgch, cch, NULL, NULL);
#else
		strcpy_s(prgch, cch + 1, pszVal);
#endif
		char * pszId;
		char * pszEnd;
		GUID guid;
		for (pszId = prgch; pszId && *pszId; pszId = pszEnd)
		{
			pszEnd = strpbrk(pszId, " \t\r\n");
			if (pszEnd)
			{
				*pszEnd = '\0';
				++pszEnd;
			}
			else
			{
				pszEnd = pszId + strlen(pszId);
			}
			if (FwXml::ParseGuid(pszId + 1, &guid))
			{
				tgvp.m_vguid.Push(guid);
			}
			else
			{
				int hobj;
				if (m_hmcidhobj.Retrieve(pszId, &hobj))
				{
					// Need to convert hobj to GUID.
					Assert(false);
					// TODO SteveMc: implement this!
				}
			}
		}
		SetStringProperty(tgvp);
}

/*----------------------------------------------------------------------------------------------
	Add this Obj reference valued property to the vector of string properties, or set its value
	if it is already in the vector.  NOTE THAT THIS VECTOR MUST BE SORTED BY m_tpt.

	@param stp Code of the string valued property.
	@param pszVal Value of the string valued text property.
	@param chType Subtype for the string property (usually default to 0, or none)
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetObjDataAsStringProp(int stp, const XML_Char * pszVal, wchar chType)
{
	switch (chType)
	{
		case kodtNameGuidHot:
		case kodtOwnNameGuidHot:
		case kodtContextString:
		case kodtGuidMoveableObjDisp:
		{
			// convert the GUID string in pszVal into a GUID and store it.
			FwXml::TextGuidValuedProp tgvp;
			GUID guid;
#ifdef XML_UNICODE
			char pszGuid[100];
			int cch = ::WideCharToMultiByte(CP_UTF8, 0, pszVal, wcslen(pszVal), pszGuid, 100, NULL, NULL);
			pszGuid[cch] = NULL;
#else
			const char * pszGuid = pszVal;
#endif
			if (FwXml::ParseGuid(pszGuid, &guid))
			{
				tgvp.m_tpt = stp;
				tgvp.m_chType = chType;
				tgvp.m_vguid.Push(guid);
				SetStringProperty(tgvp);
			}
			else
			{
				// "Invalid GUID value in <Run %s=\"%s\"> element!"
				const char * pszAttr = "???";
				switch (chType)
				{
				case kodtNameGuidHot:			pszAttr = "link";			break;
				case kodtOwnNameGuidHot:		pszAttr = "ownlink";		break;
				case kodtContextString:			pszAttr = "contextString";	break;
				case kodtGuidMoveableObjDisp:	pszAttr = "moveableObj";	break;
				}
				StrAnsi staFmt(kstidXmlErrorMsg049);
				StrAnsi sta;
				sta.Format(staFmt.Chars(), pszAttr, pszVal);
				LogMessage(sta.Chars());
			}
			break;
		}
		case kodtExternalPathName:
		case kodtEmbeddedObjectData:
		{
			TextProps::TextStrProp txsp;
			txsp.m_tpt = stp;
			// Convert the string from UTF-8 to UTF-16 and store the Unicode characters.
#ifdef XML_UNICODE
			txsp.m_stuVal = pszVal;
#else
			StrUtil::StoreUtf16FromUtf8(pszVal, strlen(pszVal), txsp.m_stuVal);
#endif
			// Stick in the type marker
			txsp.m_stuVal.Replace(0, 0, &chType, 1);
			SetStringProperty(txsp);
			break;
		}
		default:
		{
			Assert(false);		// THIS SHOULD NEVER HAPPEN!
			break;
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Handle XML end tags inside <Str> and <AStr> elements.  For FwXmlData::LoadXml(), this is
	used for the second pass.

	This static method is passed to the expat XML parser as a callback function.  It is used
	when the start tag for either <Str> or <AStr> is detected until the corresponding end tag
	(</Str> or </AStr>) is detected.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param pszName XML element name read from the input file.
----------------------------------------------------------------------------------------------*/
void FwXml::HandleStringEndTag(void * pvUser, const XML_Char * pszName)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);

	pxid->ProcessStringEndTag(pszName);
}

void FwXmlImportData::ProcessStringEndTag(const XML_Char * pszName)
{
	StrAnsi sta;
	StrAnsi staFmt;
	static const STRTYPE str(LitXml("Str"));
	static const STRTYPE aStr(LitXml("AStr"));
	static const STRTYPE run(LitXml("Run"));
	if (!CompareXml(pszName, str) || !CompareXml(pszName, aStr))
	{
		// Return to the normal processing for storing the string data.
		XML_SetElementHandler(m_parser, m_startOuterHandler, m_endOuterHandler);
		(*m_endOuterHandler)(this, pszName);
		return;
	}
	else if (!CompareXml(pszName, run))
	{
		if (m_rdt == FwXml::krdtChars)
		{
			SaveCharDataInRun();
		}
		else if (m_rdt == FwXml::krdtPicture)
		{
			SavePictureDataInRun();
		}
		else
		{
			Assert(false);	// We should have already complained about this invalid element.
		}
		// Check for an empty run (except the first, which we want to keep, e.g., so
		// we know the correct writing system for an empty string).
		if (m_vbri.Size() != 0 && !m_fRunHasChars)
		{
			// KenZ thinks this is too much information.
			// "Ignoring an empty <Run>."
			//sta.Load(kstidXmlErrorMsg155);
			//LogMessage(sta.Chars());
			m_fInRun = false;
			return;
		}
		if (m_vbri.Size() == 1 && m_bri.m_ichMin == 0)
		{
			// KenZ thinks this is too much information.
			//sta.Load(kstidXmlErrorMsg155);
			//LogMessage(sta.Chars());

			// We saved an initial empty run, but we found another run, so discard the initial
			// empty run information.
			m_vbri.Pop();
			if (m_vrpi.Size())
				m_vrpi.Pop();	// Keep these in sync.
		}
		bool fMergeRun = StoreRunInformation();
#if 99-99
		// "run[%d]: ichMin = %d, ibProp = %d; distinct = %d, fMerge = %s"
		staFmt.Load(kstidXmlDebugMsg008);
		sta.Format(staFmt.Chars(),
			m_vbri.Size(), m_bri.m_ichMin, m_bri.m_ibProp,
			m_vrpi.Size(), fMergeRun ? "true" : "false");
		LogMessage(sta.Chars());
#endif
		// Hand-crafted (or LinguaLinks exported) XML is likely to have consecutive runs with
		// identical properties: these must be merged.
		if (!fMergeRun)
		{
			m_vbri.Push(m_bri);
		}
		else
		{
			// KenZ thinks this is too much information.
	// "Found a <Run> with identical properties to preceding <Run>: these have been merged."
			//sta.Load(kstidXmlErrorMsg039);
			//LogMessage(sta.Chars());
		}
		m_fInRun = false;
	}
	else
	{		// We should have already complained about this invalid element.
	}
}

/*----------------------------------------------------------------------------------------------
	Handle any accumulated character data for the </Run> element end marker.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SaveCharDataInRun()
{
	if (!m_fRunHasChars)
	{
		bool fLinkFound = false;
		for (int i = 0; i < m_vtxsp.Size(); ++i)
		{
			if (m_vtxsp[i].m_tpt == kstpObjData)
			{
				fLinkFound = true;
				break;
			}
		}
		if (fLinkFound)
		{
			wchar rgchw[2];
			rgchw[0] = kchObject;
			rgchw[1] = 0;
			m_stuChars.Append(rgchw, 1);
			m_fRunHasChars = true;
		}
	}
	else
	{
		// Convert tabs to spaces.
		int ich = 0;
		while ((ich = m_stuChars.FindCh('\t', ich)) != -1)
			m_stuChars.SetAt(ich, ' ');

	}
}

/*----------------------------------------------------------------------------------------------
	Handle the character data in a <Run> element if it was storing a picture bitmap.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SavePictureDataInRun()
{
	// Store this picture data in the properties.
	int cch = m_vchHex.Size();
	const char * prgchHex = m_vchHex.Begin();
#define BIN_SIZE 8000
	byte rgbBin[BIN_SIZE];
	Vector<byte> vbBin;
	byte * prgbBin;
	// Conservative approximation, ignoring possible whitespace.
	int cbBin = cch / 2;
	if (cbBin <= isizeof(rgbBin))
	{
		prgbBin = rgbBin;
	}
	else
	{
		vbBin.Resize(cbBin);
		prgbBin = vbBin.Begin();
	}
	cbBin = ConvertPictureToBitmap(prgchHex, cch, prgbBin);
	if (!cbBin)
	{
		// "Empty <Run type=\"picture\"> element?"
		StrAnsi sta(kstidXmlErrorMsg033);
		LogMessage(sta.Chars());
	}
	else
	{
		// The number of characters need to hold the bytes plus 1 for the kodtPict
		// constant.
		TextProps::TextStrProp txsp;
		txsp.m_tpt = kstpObjData;
		int cchData;
		OLECHAR * pchData;
		cchData = (cbBin + 1 + isizeof(OLECHAR)) / isizeof(OLECHAR);
		txsp.m_stuVal.SetSize(cchData, &pchData);
		if (cbBin & 1)
			*pchData = kodtPictOddHot;
		else
			*pchData = kodtPictEvenHot;
		++pchData;
		memcpy(pchData, prgbBin, cbBin);
		m_vtxsp.Push(txsp);
		wchar rgchw[2];
		rgchw[0] = kchObject;
		rgchw[1] = 0;
		m_stuChars.Append(rgchw, 1);
		m_fRunHasChars = true;
	}
	m_vchHex.Clear();
}

/*----------------------------------------------------------------------------------------------
	Store the run information for future use.

	@return true if the run information is empty or shared with an earlier run.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::StoreRunInformation()
{
	bool fMergeRun = false;
	// Store the run information.
	if (!m_vtxip.Size() && !m_vtxsp.Size())
	{
		m_bri.m_ibProp = -1;			// Signal no properties.
		if (m_vbri.Top() && m_vbri.Top()->m_ibProp == -1)
			fMergeRun = true;
	}
	else
	{
		Assert(m_vtxip.Size() < 256);
		Assert(m_vtxsp.Size() < 256);

		fMergeRun = StoreRawPropertyBytes();
	}
	return fMergeRun;
}

/*----------------------------------------------------------------------------------------------
	Store the run information in raw binary form, unless an earlier run has exactly the same
	properties, then just record the earlier index into the string's property array.

	@return true if the run information is shared with an earlier run.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::StoreRawPropertyBytes()
{
	bool fMergeRun = false;
	FwXml::RunPropInfo rpi;
	ConvertToRawBytes(rpi);

	// Calculate the offset into the raw property bytes, and check whether
	// this set of properties has already been stored.
	bool fFound = false;
	m_bri.m_ibProp = 0;
	for (int i = 0; i < m_vrpi.Size(); ++i)
	{
		int cb = m_vrpi[i].m_vbRawProps.Size();
		if (rpi.m_ctip == m_vrpi[i].m_ctip &&
			rpi.m_ctsp == m_vrpi[i].m_ctsp &&
			rpi.m_vbRawProps.Size() == cb &&
			!memcmp(rpi.m_vbRawProps.Begin(), m_vrpi[i].m_vbRawProps.Begin(), cb))
		{
			fFound = true;
			break;
		}
		m_bri.m_ibProp += cb + 2;
	}
	if (!fFound)
	{
#if 99-99
		// "rpi[%d] = %d, %d, 0x"
		staFmt.Load(kstidXmlDebugMsg007);
		sta.Format(staFmt.Chars(), m_vrpi.Size(), rpi.m_ctip, rpi.m_ctsp);
		if (m_pfileLog)
		{
			fputs(sta.Chars(), m_pfileLog);
			for (int ib = 0; ib < rpi.m_vbRawProps.Size(); ++ib)
				fprintf(m_pfileLog, "%02x", rpi.m_vbRawProps[ib] & 0xFF);
			fputs("\n", m_pfileLog);
		}
#endif
		m_vrpi.Push(rpi);
	}
	else
	{
#if 99-99
		// "DEBUG: Repeated run properties found: ibProp = %d, Top()->ibProp = %d"
		staFmt.Load(kstidXmlDebugMsg002);
		sta.Format(staFmt.Chars(),
			m_bri.m_ibProp,
			m_vbri.Top() ? m_vbri.Top()->m_ibProp : -1);
		LogMessage(sta.Chars());
#endif
		if (m_vbri.Top() &&
			m_vbri.Top()->m_ibProp == m_bri.m_ibProp)
		{
			fMergeRun = true;
		}
	}
	return fMergeRun;
}

/*----------------------------------------------------------------------------------------------
	Convert the run property information to the canonical raw binary form.

	@param rpi - reference to a RunPropInfo struct for storing the raw bytes.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::ConvertToRawBytes(FwXml::RunPropInfo & rpi)
{
	rpi.m_vbRawProps.Resize(8000);
	DataWriterRgb dwr(rpi.m_vbRawProps.Begin(), rpi.m_vbRawProps.Size());
	int i;
	rpi.m_ctip = static_cast<byte>(m_vtxip.Size());
	if (rpi.m_ctip)
	{
		TextProps::TextIntProp txip;
		for (i = 0; i < m_vtxip.Size(); ++i)
		{
			txip.m_scp = m_vtxip[i].m_scp;
			txip.m_nVal = m_vtxip[i].m_nVal;
			txip.m_nVar = m_vtxip[i].m_nVar;
			TextProps::WriteTextIntProp(&dwr, &txip);
		}
		m_vtxip.Clear();
	}
	rpi.m_ctsp = static_cast<byte>(m_vtxsp.Size());
	if (rpi.m_ctsp)
	{
		TextProps::TextStrProp txsp;
		for (i = 0; i < m_vtxsp.Size(); ++i)
		{
			txsp.m_tpt = m_vtxsp[i].m_tpt;
			txsp.m_stuVal = m_vtxsp[i].m_stuVal;
			// Check that we have enough room to store the property.
			VerifyDataLength(dwr, txsp, rpi.m_vbRawProps);
			TextProps::WriteTextStrProp(&dwr, &txsp);
		}
		m_vtxsp.Clear();
	}
	rpi.m_vbRawProps.Resize(dwr.IbCur());		// Shrink to fit.
}

/*----------------------------------------------------------------------------------------------
	Convert the picture bitmap data from hexadecimal characters to binary bytes.

	@prgbBin - pointer to byte array for storing picture bitmap
	@return - number of bytes actually stored in prgbBin
----------------------------------------------------------------------------------------------*/
int FwXmlImportData::ConvertPictureToBitmap(const char * prgchHex, int cch, byte * prgbBin)
{
	int ib;
	int ich;
	byte ch;
	byte bT;
	for (ib = 0, ich = 0; ich < cch; )
	{
		// Read the first Hex digit of the byte.  It may be preceded by one or
		// more whitespace characters.
		do
		{
			ch = prgchHex[ich];
			++ich;
			if (!isascii(ch))
				ThrowHr(WarnHr(E_UNEXPECTED));
		} while (isspace(ch) && ich < cch);
		if (ich == cch)
		{
			if (!isspace(ch))
			{
				// "Warning: ignoring extra character at the end of %s data."
				StrAnsi staFmt(kstidXmlErrorMsg133);
				StrAnsi sta;
				sta.Format(staFmt.Chars(), "Run");
				LogMessage(sta.Chars());
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
		ch = prgchHex[ich];
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
	return ib;
}

/*----------------------------------------------------------------------------------------------
	Handle XML character data.  For FwXmlData::LoadXml(), this is used for the second pass.

	This static method is passed to the expat XML parser as a callback function.  See the
	comments in xmlparse.h for the XML_CharacterDataHandler typedef for the documentation
	such as it is.

	@param pvUser Pointer to generic user data (always XML import data in this case).
	@param prgch Pointer to an array of character data; not NUL-terminated.
	@param cch Number of characters (bytes) in prgch.
----------------------------------------------------------------------------------------------*/
void FwXml::HandleCharData(void * pvUser, const XML_Char * prgch, int cch)
{
	FwXmlImportData * pxid = reinterpret_cast<FwXmlImportData *>(pvUser);
	AssertPtr(pxid);

	pxid->ProcessCharData(prgch, cch);
}

void FwXmlImportData::ProcessCharData(const XML_Char * prgch, int cch)
{
	if (!prgch || !cch)
		return;
#ifdef WIN32
	// The expat parser reduces "\r\n" to a bare "\n".
	if (*prgch == '\n' && cch == 1)
	{
		prgch = LitXml("\r\n");
		cch = 2;
	}
#endif /*WIN32*/
	StrAnsi sta;
	StrAnsi staFmt;
	try
	{
		if (m_fInBinary || (m_fInString && m_fInRun && m_rdt == FwXml::krdtPicture))
		{
			// Store these Hex digit characters.
			int cchHex = m_vchHex.Size();
#ifdef XML_UNICODE
			StrAnsi staHex(prgch); // hex digits will work fine with default conversion
			const char * prgchSrc = staHex.Chars();
#else
			const char * prgchSrc = prgch;
#endif
			m_vchHex.Replace(cchHex, cchHex, prgchSrc, cch);
			return;
		}
		bool fIgnore = SetWsIfNeeded(prgch, cch);

		if (fIgnore)
		{
			// Check that all characters are valid XML whitespace characters.
			for (int ich = 0; ich < cch; ++ich)
			{
				if (prgch[ich] != '\r' && prgch[ich] != '\n' && prgch[ich] != '\t' &&
					prgch[ich] != ' ')
				{
					if (m_pfileLog)
					{
						// "Invalid character data found between runs: \""
						sta.Load(kstidXmlErrorMsg056);
						LogMessage(sta.Chars());
						for (int i = 0; i < cch; ++i)
							fprintf(m_pfileLog, "%c", prgch[i]);
						fprintf(m_pfileLog, "\"\n");
					}
					break;
				}
			}
		}
		else
		{
			// Append these characters to the string.
#ifdef XML_UNICODE
			m_stuChars.Append(prgch, cch);
#else
			StrUtil::StoreUtf16FromUtf8(prgch, cch, m_stuChars, true);
#endif
			if (m_fInRun)
				m_fRunHasChars = true;
		}
	}
	catch (Throwable & thr)
	{
		m_fError = true;
		m_hr = thr.Error();
#ifdef DEBUG
		// "ERROR CAUGHT on line %d of %s: %s"
		staFmt.Load(kstidXmlDebugMsg003);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__, AsciiHresult(m_hr));
		LogMessage(sta.Chars());
#endif
	}
	catch (...)
	{
		m_fError = true;
		m_hr = E_FAIL;
#ifdef DEBUG
		// "UNKNOWN ERROR CAUGHT on line %d of %s"
		staFmt.Load(kstidXmlDebugMsg005);
		sta.Format(staFmt.Chars(), __LINE__, __FILE__);
		LogMessage(sta.Chars());
#endif
	}
}

/*----------------------------------------------------------------------------------------------
	Set the writing system for string data if it is needed.

	@param prgch Pointer to an array of character data; not NUL-terminated.
	@param cch Number of characters (bytes) in prgch.

	@return true if the character data should be ignored, false if it should be stored.
----------------------------------------------------------------------------------------------*/
bool FwXmlImportData::SetWsIfNeeded(const XML_Char * prgch, int cch)
{
	bool fIgnore = false;
	if (m_fInString)
	{
		if (m_fInRun)
		{
			if (m_rdt == FwXml::krdtChars)
			{
				if (!m_stuChars.Length())
				{
					// Check for writing system being set at the beginning of the string.
					bool fHaveWs = false;
					for (int i = 0; i < m_vtxip.Size(); ++i)
					{
						if (m_vtxip[i].m_scp == kscpWs)
						{
							fHaveWs = true;
							break;
						}
					}
					if (!fHaveWs)
					{
						if (m_ws)
						{
							TextProps::TextIntProp txip;
							txip.m_scp = kscpWs;
							txip.m_nVal = m_ws;
							txip.m_nVar = 0;
							SetIntegerProperty(txip);
						}
						else
						{
							// "Warning: String does not have an writing system!"
							StrAnsi sta(kstidXmlErrorMsg132);
							LogMessage(sta.Chars());
						}
					}
				}
			}
			else if (m_rdt == FwXml::krdtPicture)
			{
				// This should have been handled already!
				Assert(m_rdt != FwXml::krdtPicture);
			}
		}
		else
		{
			// Ignore whitespace inside <Str> or <AStr> but outside <Run> elements.
			fIgnore = true;
		}
	}
	else
	{
		// Assume that we want character data if it's outside string elements.  This stores a
		// lot of temporary whitespace between elements, but so what?
	}
	return fIgnore;
}
