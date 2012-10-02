/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2004 by SIL International. All rights reserved.

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
		if (strcmp(pszName, "AStr") == 0 || strcmp(pszName, "Str") == 0)
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
		else if (strcmp(pszName, "Run") == 0)
		{
			m_fInRun = true;
			m_bri.m_ichMin = m_stuChars.Length();
			m_fRunHasChars = false;
			m_rdt = FwXml::krdtChars;
			const char * pszType = FwXml::GetAttributeValue(prgpszAtts, "type");
			if (pszType)
			{
				if (strcmp(pszType, "picture") == 0)
				{
					m_rdt = FwXml::krdtPicture;
				}
				else if (strcmp(pszType, "chars") != 0)
				{
					// "Invalid value in <Run type=\"%s\">: need chars or picture"
					staFmt.Load(kstidXmlErrorMsg073);
					sta.Format(staFmt.Chars(), pszType);
					LogMessage(sta.Chars());
				}
			}
			// Integer-valued properties.
			SetTextColor(prgpszAtts, "backcolor", kscpBackColor);
			SetTextToggle(prgpszAtts, "bold", kscpBold);
			SetTextWs(prgpszAtts, "ws", kscpWs);
			SetTextWs(prgpszAtts, "wsBase", kscpBaseWs);
			SetTextMetric(prgpszAtts, "fontsize", "fontsizeunit", kscpFontSize);
			SetTextColor(prgpszAtts, "forecolor", kscpForeColor);
			SetTextToggle(prgpszAtts, "italic", kscpItalic);
			SetTextMetric(prgpszAtts, "offset", "offsetunit", kscpOffset);
			SetTextSuperscript(prgpszAtts);
			SetTextUnderline(prgpszAtts);
			SetTextColor(prgpszAtts, "undercolor", kscpUnderColor);
			// String-valued properties.
			SetStringProperty(prgpszAtts, "fontFamily", kstpFontFamily);
			SetStringProperty(prgpszAtts, "charStyle", kstpCharStyle);
			SetStringProperty(prgpszAtts, "charStyle", kstpCharStyle);
			SetStringProperty(prgpszAtts, "paraStyle", kstpParaStyle);
			SetStringProperty(prgpszAtts, "tabList", kstpTabList);
			SetStringProperty(prgpszAtts, "tags", kstpTags);
			SetStringProperty(prgpszAtts, "link", kstpObjData, kodtNameGuidHot);
			SetStringProperty(prgpszAtts, "ownlink", kstpObjData, kodtOwnNameGuidHot);
			SetStringProperty(prgpszAtts, "externalLink", kstpObjData, kodtExternalPathName);
			SetStringProperty(prgpszAtts, "contextString", kstpObjData, kodtContextString);
			SetStringProperty(prgpszAtts, "moveableObj", kstpObjData, kodtGuidMoveableObjDisp);
			SetStringProperty(prgpszAtts, "fontVariations", kstpFontVariations);
			SetStringProperty(prgpszAtts, "namedStyle", kstpNamedStyle);
			SetStringProperty(prgpszAtts, "bulNumTxtBef", kstpBulNumTxtBef);
			SetStringProperty(prgpszAtts, "bulNumTxtAft", kstpBulNumTxtAft);
			SetStringProperty(prgpszAtts, "bulNumFontInfo", kstpBulNumFontInfo);
			SetStringProperty(prgpszAtts, "wsStyle", kstpWsStyle);
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

	@param prgpszAtts - Pointer to NULL-terminated array of name / value pairs of strings.
	@param pszAttName - possible XML attribute name for a color property
	@param scp - code for this scalar (integer) valued property
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextColor(const XML_Char ** prgpszAtts, const char * pszAttName,
	int scp)
{
	TextProps::TextIntProp txip;
	const char * pszValueLim;
	const char * pszColor = FwXml::GetAttributeValue(prgpszAtts, pszAttName);
	if (pszColor)
	{
		txip.m_scp = scp;
		txip.m_nVal = FwXml::DecodeTextColor(pszColor, &pszValueLim);
		txip.m_nVar = 0;
		if (*pszValueLim)
		{
			// ERROR
		}
		SetIntegerProperty(txip);
	}
}

/*----------------------------------------------------------------------------------------------
	If the given attribute is present in the XML element, add its value to the current run's
	set of integer values.

	@param prgpszAtts - Pointer to NULL-terminated array of name / value pairs of strings.
	@param pszAttName - possible XML attribute name for a toggle valued property
	@param scp - code for this scalar (integer) valued property
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextToggle(const XML_Char ** prgpszAtts, const char * pszAttName,
	int scp)
{
	TextProps::TextIntProp txip;
	const char * pszValueLim;
	const char * pszToggle = FwXml::GetAttributeValue(prgpszAtts, pszAttName);
	if (pszToggle)
	{
		txip.m_scp = scp;
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
			txip.m_nVar = ktpvEnum;
			SetIntegerProperty(txip);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	If the given attribute is present in the XML element, add its value to the current run's
	set of integer values.

	@param prgpszAtts - Pointer to NULL-terminated array of name / value pairs of strings.
	@param pszAttName - possible XML attribute name for a writing system property
	@param scp - code for this scalar (integer) valued property
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextWs(const XML_Char ** prgpszAtts, const char * pszAttName, int scp)
{
	TextProps::TextIntProp txip;
	const char * pszWs = FwXml::GetAttributeValue(prgpszAtts, pszAttName);
	if (pszWs)
	{
		txip.m_scp = scp;
		// "Cannot convert \"%s\" into a Language Writing system code."
		txip.m_nVal = GetWsFromIcuLocale(pszWs, kstidXmlErrorMsg011);
		txip.m_nVar = 0;
		if (txip.m_nVal)
			SetIntegerProperty(txip);
	}
}

/*----------------------------------------------------------------------------------------------
	If the given attribute is present in the XML element, add its value to the current run's
	set of integer values.

	@param prgpszAtts - Pointer to NULL-terminated array of name / value pairs of strings.
	@param pszAttName - possible XML attribute name for a metric property
	@param pszUnitAttName - related XML attribute name for the unit used for the metric
	@param scp - code for this scalar (integer) valued property
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextMetric(const XML_Char ** prgpszAtts, const char * pszAttName,
	const char * pszUnitAttName, int scp)
{
	TextProps::TextIntProp txip;
	StrAnsi staFmt;
	StrAnsi sta;
	const char * pszVal = FwXml::GetAttributeValue(prgpszAtts, pszAttName);
	const char * pszUnitVal = FwXml::GetAttributeValue(prgpszAtts, pszUnitAttName);
	if (pszVal)
	{
		bool fError = false;
		char * psz;
		unsigned nSize = strtoul(pszVal, &psz, 10);
		if (*psz)
		{
			if (!pszUnitVal)
			{
				if (!strcmp(psz, "mpt"))
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
			if (!strcmp(pszUnitVal, "mpt"))
			{
				tpv = ktpvMilliPoint;
			}
			else
			{
				// REVIEW SteveMc: should we default to an unsigned int like this?
				tpv = strtoul(pszUnitVal, &psz, 10);
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
	else if (pszUnitVal)
	{
		// "Ignoring <Run %s=\"%s\"> in the absence of a %s attribute."
		staFmt.Load(kstidXmlErrorMsg042);
		sta.Format(staFmt.Chars(), pszUnitAttName, pszUnitVal, pszAttName);
		LogMessage(sta.Chars());
	}
}

/*----------------------------------------------------------------------------------------------
	If the superscript attribute is present in the XML element, add its value to the current
	run's set of integer values.

	@param prgpszAtts - Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextSuperscript(const XML_Char ** prgpszAtts)
{
	TextProps::TextIntProp txip;
	const char * pszValueLim;
	const char * pszSuperscript = FwXml::GetAttributeValue(prgpszAtts, "superscript");
	if (pszSuperscript)
	{
		txip.m_scp = kscpSuperscript;
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
			txip.m_nVar = ktpvEnum;
			SetIntegerProperty(txip);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	If the underline attribute is present in the XML element, add its value to the current run's
	set of integer values.

	@param prgpszAtts - Pointer to NULL-terminated array of name / value pairs of strings.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTextUnderline(const XML_Char ** prgpszAtts)
{
	TextProps::TextIntProp txip;
	const char * pszValueLim;
	const char * pszUnderline = FwXml::GetAttributeValue(prgpszAtts, "underline");
	if (pszUnderline)
	{
		txip.m_scp = kscpUnderline;
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
			txip.m_nVar = ktpvEnum;
			SetIntegerProperty(txip);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	If the given attribute is present in the XML element, add its value to the current run's
	set of string values.

	@param prgpszAtts - Pointer to NULL-terminated array of name / value pairs of strings.
	@param pszAttName - possible XML attribute name for an underline property
	@param stp - code for this string valued property
	@param chType - optional (defaults to 0) type flag for the property
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetStringProperty(const XML_Char ** prgpszAtts, const char * pszAttName,
	int stp, wchar chType)
{
	const char * pszVal = FwXml::GetAttributeValue(prgpszAtts, pszAttName);
	if (pszVal)
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
			StrUtil::StoreUtf16FromUtf8(pszVal, strlen(pszVal), txsp.m_stuVal);
			SetStringProperty(txsp);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Add this Tags valued property to the vector of string properties, or set its value if it
	is already in the vector.  NOTE THAT THIS VECTOR MUST BE SORTED BY m_tpt.

	@param stp Code of the string valued property.
	@param pszVal Value of the string valued text property.
----------------------------------------------------------------------------------------------*/
void FwXmlImportData::SetTagsAsStringProp(int stp, const char * pszVal)
{
		FwXml::TextGuidValuedProp tgvp;
		tgvp.m_tpt = stp;

		// convert the IDs in pszVal into GUIDs and store them.
		char szBuffer[400];				// Use stack temp space for smaller amounts.
		Vector<char> vch;
		char * prgch;
		int cch = strlen(pszVal);
		if (cch < 400)
		{
			prgch = szBuffer;
		}
		else
		{
			vch.Resize(cch + 1);		// Too much for stack: use temp heap storage.
			prgch = &vch[0];
		}
		strcpy_s(prgch, cch + 1, pszVal);
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
void FwXmlImportData::SetObjDataAsStringProp(int stp, const char * pszVal, wchar chType)
{
	if (chType == kodtNameGuidHot || chType == kodtOwnNameGuidHot ||
		chType == kodtContextString || chType == kodtGuidMoveableObjDisp)
	{
		// convert the GUID string in pszVal into a GUID and store it.
		FwXml::TextGuidValuedProp tgvp;
		GUID guid;
		if (FwXml::ParseGuid(pszVal, &guid))
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
	}
	else if (chType == kodtExternalPathName)
	{
		TextProps::TextStrProp txsp;
		txsp.m_tpt = stp;
		// Convert the string from UTF-8 to UTF-16 and store the Unicode characters.
		StrUtil::StoreUtf16FromUtf8(pszVal, strlen(pszVal), txsp.m_stuVal);
		// Stick in the marker that this is an external pathname.
		txsp.m_stuVal.Replace(0, 0, &chType, 1);
		SetStringProperty(txsp);
	}
	else
	{
		Assert(false);		// THIS SHOULD NEVER HAPPEN!
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
	if (!strcmp(pszName, "Str") || !strcmp(pszName, "AStr"))
	{
		// Return to the normal processing for storing the string data.
		XML_SetElementHandler(m_parser, m_startOuterHandler, m_endOuterHandler);
		(*m_endOuterHandler)(this, pszName);
		return;
	}
	else if (!strcmp(pszName, "Run"))
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
		// Convert newlines to spaces, unless preceded by a space; in which case,
		// just delete the newline.
		ich = 0;
		while ((ich = m_stuChars.FindStr(L"\r\n", 2, ich)) != -1)
		{
			if (ich > 0 && m_stuChars.GetAt(ich - 1) == ' ')
				m_stuChars.Replace(ich, ich + 2, L"");
			else
				m_stuChars.Replace(ich, ich + 2, L" ");
		}
		ich = 0;
		while ((ich = m_stuChars.FindCh('\r', ich)) != -1)
		{
			if (ich > 0 && m_stuChars.GetAt(ich - 1) == ' ')
				m_stuChars.Replace(ich, ich + 1, L"");
			else
				m_stuChars.SetAt(ich, ' ');
		}
		ich = 0;
		while ((ich = m_stuChars.FindCh('\n', ich)) != -1)
		{
			if (ich > 0 && m_stuChars.GetAt(ich - 1) == ' ')
				m_stuChars.Replace(ich, ich + 1, L"");
			else
				m_stuChars.SetAt(ich, ' ');
		}
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
			*pchData = kodtPictOdd;
		else
			*pchData = kodtPictEven;
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
		prgch = "\r\n";
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
			m_vchHex.Replace(cchHex, cchHex, prgch, cch);
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
			StrUtil::StoreUtf16FromUtf8(prgch, cch, m_stuChars, true);
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
