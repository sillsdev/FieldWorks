/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2004 by SIL International. All rights reserved.

File: FwXml.cpp
Responsibility: Steve McConnel
Last reviewed:

VERY IMPORTANT NOTE:
	This file implements the most basic namespace FwXml methods.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop

#include "FwXml.h"

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	This data structure stores an XML tag and its associated field type for basic type elements.
	Hungarian: bel
----------------------------------------------------------------------------------------------*/
struct BasicElem
{
	const char * m_pszName;		// Name of an XML element.
	int m_cpt;					// Data type associated with that element.
};

static const BasicElem g_rgbel[] =
{
	// These must be lexically ordered as by strcmp so that a binary search may be used.
	{ "AStr",		kcptMultiString },
	{ "AUni",		kcptMultiUnicode },
	{ "Binary",		kcptBinary },
	{ "Boolean",	kcptBoolean },
	{ "Float",		kcptFloat },
	{ "FwDatabase",	kcptNil },
	{ "GenDate",	kcptGenDate },
	{ "Guid",		kcptGuid },
	{ "Image",		kcptImage },
	{ "Integer",	kcptInteger },
	{ "Link",		kcptReferenceAtom },
	{ "Numeric",	kcptNumeric },
	{ "Prop",		kcptRuleProp },
	{ "Str",		kcptString },
	{ "Time",		kcptTime },
	{ "Uni",		kcptUnicode },
};
static const int g_cbel = isizeof(g_rgbel) / isizeof(BasicElem);

/*----------------------------------------------------------------------------------------------
	Check whether this element is a basic type.

	@param pszName XML element name read from the input file.

	@return The appropriate field type value for a basic type element, or -1 if the element
	is not a basic type.
----------------------------------------------------------------------------------------------*/
int FwXml::BasicType(const char * pszName)
{
	int iMin = 0;
	int iLim = g_cbel;
	int iMid;
	int ncmp;
	// Perform a binary search
	while (iMin < iLim)
	{
		iMid = (iMin + iLim) >> 1;
		ncmp = strcmp(g_rgbel[iMid].m_pszName, pszName);
		if (ncmp == 0)
			return g_rgbel[iMid].m_cpt;
		else if (ncmp < 0)
			iMin = iMid + 1;
		else
			iLim = iMid;
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Return a pointer to the character string containing the value of the given XML attribute,
	or NULL if that attribute is not defined for this XML element.

	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param pszName Name to search for in prgpszAtts.
----------------------------------------------------------------------------------------------*/
const OLECHAR * FwXml::GetAttributeValue(const OLECHAR ** prgpszAtts, const OLECHAR * pszName)
{
	if (!prgpszAtts)
		return NULL;
	for (int i = 0; prgpszAtts[i]; i += 2)
	{
		if (wcscmp(prgpszAtts[i], pszName) == 0)
			return prgpszAtts[i+1];
	}
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Return a pointer to the character string containing the value of the given XML attribute,
	or NULL if that attribute is not defined for this XML element.

	@param prgpszAtts Pointer to NULL-terminated array of name / value pairs of strings.
	@param pszName Name to search for in prgpszAtts.
----------------------------------------------------------------------------------------------*/
const char * FwXml::GetAttributeValue(const char ** prgpszAtts, const char * pszName)
{
	if (!prgpszAtts)
		return NULL;
	for (int i = 0; prgpszAtts[i]; i += 2)
	{
		if (strcmp(prgpszAtts[i], pszName) == 0)
			return prgpszAtts[i+1];
	}
	return NULL;
}


static const char g_szHexDigits[23] = "0123456789ABCDEFabcdef";

/*----------------------------------------------------------------------------------------------
	Try to parse a string as a guid.
	@null{	REVIEW SteveMc: make this a general purpose utility function?	}
	@null{	REVIEW SteveMc: should we handle other formats?	}

	@param pszGuid String ostensibly containing a GUID.
	@param pguidRet Pointer to a GUID structure that receives the decoded result.

	@return True if successful, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FwXml::ParseGuid(const char * pszGuid, GUID * pguidRet)
{
	AssertPsz(pszGuid);
	AssertPtr(pguidRet);
	int cSect = 0;
	char * psz = NULL;
	char szBuffer[4];
	GUID guid;
	if (strlen(pszGuid) != 36)		// (For example, "9261D3B0-F064-11d3-9041-00400541F6D3".)
		return false;
	memset(szBuffer, 0, isizeof(szBuffer));
	memset(&guid, 0, isizeof(GUID));
	if (*pszGuid)
	{
		guid.Data1 = strtoul(pszGuid, &psz, 16);
		++cSect;
	}
	if (psz && *psz == '-')
	{
		guid.Data2 = static_cast<ushort>(strtol(psz+1, &psz, 16));
		++cSect;
	}
	if (psz && *psz == '-')
	{
		guid.Data3 = static_cast<ushort>(strtol(psz+1, &psz, 16));
		++cSect;
	}
	if (psz && *psz == '-')
	{
		++psz;
		if (strspn(psz, g_szHexDigits) == 4)
		{
			memcpy(szBuffer, psz, 2);
			guid.Data4[0] = static_cast<uchar>(strtol(szBuffer, NULL, 16));
			++cSect;
			psz += 2;
			guid.Data4[1] = static_cast<uchar>(strtol(psz, &psz, 16));
			++cSect;
		}
		else
			psz = NULL;
	}
	if (psz && *psz == '-')
	{
		++psz;
		if (strspn(psz, g_szHexDigits) == 12)
		{
			for (int i = 2; i < 7; ++i)
			{
				memcpy(szBuffer, psz, 2);
				guid.Data4[i] = static_cast<uchar>(strtol(szBuffer, NULL, 16));
				++cSect;
				psz += 2;
			}
			guid.Data4[7] = static_cast<uchar>(strtol(psz, &psz, 16));
			++cSect;
		}
	}
	if (cSect == 11)
	{
		memcpy(pguidRet, &guid, isizeof(GUID));
		return true;
	}
	else
	{
		return false;
	}
}

/*----------------------------------------------------------------------------------------------
	Collect any error information together in a string.
----------------------------------------------------------------------------------------------*/
void FwXml::XmlErrorDetails(XML_Parser parser, const char * pszFile, StrAnsi & staMsg)
{
	enum XML_Error xerr = XML_GetErrorCode(parser);
	const char * pszErr = NULL;
	char rgchErr[100];
	switch (xerr)
	{
	case XML_ERROR_NONE:
		pszErr = "XML_ERROR_NONE";
		break;
	case XML_ERROR_NO_MEMORY:
		pszErr = "XML_ERROR_NO_MEMORY";
		break;
	case XML_ERROR_SYNTAX:
		pszErr = "XML_ERROR_SYNTAX";
		break;
	case XML_ERROR_NO_ELEMENTS:
		pszErr = "XML_ERROR_NO_ELEMENTS";
		break;
	case XML_ERROR_INVALID_TOKEN:
		pszErr = "XML_ERROR_INVALID_TOKEN";
		break;
	case XML_ERROR_UNCLOSED_TOKEN:
		pszErr = "XML_ERROR_UNCLOSED_TOKEN";
		break;
	case XML_ERROR_PARTIAL_CHAR:
		pszErr = "XML_ERROR_PARTIAL_CHAR";
		break;
	case XML_ERROR_TAG_MISMATCH:
		pszErr = "XML_ERROR_TAG_MISMATCH";
		break;
	case XML_ERROR_DUPLICATE_ATTRIBUTE:
		pszErr = "XML_ERROR_DUPLICATE_ATTRIBUTE";
		break;
	case XML_ERROR_JUNK_AFTER_DOC_ELEMENT:
		pszErr = "XML_ERROR_JUNK_AFTER_DOC_ELEMENT";
		break;
	case XML_ERROR_PARAM_ENTITY_REF:
		pszErr = "XML_ERROR_PARAM_ENTITY_REF";
		break;
	case XML_ERROR_UNDEFINED_ENTITY:
		pszErr = "XML_ERROR_UNDEFINED_ENTITY";
		break;
	case XML_ERROR_RECURSIVE_ENTITY_REF:
		pszErr = "XML_ERROR_RECURSIVE_ENTITY_REF";
		break;
	case XML_ERROR_ASYNC_ENTITY:
		pszErr = "XML_ERROR_ASYNC_ENTITY";
		break;
	case XML_ERROR_BAD_CHAR_REF:
		pszErr = "XML_ERROR_BAD_CHAR_REF";
		break;
	case XML_ERROR_BINARY_ENTITY_REF:
		pszErr = "XML_ERROR_BINARY_ENTITY_REF";
		break;
	case XML_ERROR_ATTRIBUTE_EXTERNAL_ENTITY_REF:
		pszErr = "XML_ERROR_ATTRIBUTE_EXTERNAL_ENTITY_REF";
		break;
	case XML_ERROR_MISPLACED_XML_PI:
		pszErr = "XML_ERROR_MISPLACED_XML_PI";
		break;
	case XML_ERROR_UNKNOWN_ENCODING:
		pszErr = "XML_ERROR_UNKNOWN_ENCODING";
		break;
	case XML_ERROR_INCORRECT_ENCODING:
		pszErr = "XML_ERROR_INCORRECT_ENCODING";
		break;
	case XML_ERROR_UNCLOSED_CDATA_SECTION:
		pszErr = "XML_ERROR_UNCLOSED_CDATA_SECTION";
		break;
	case XML_ERROR_EXTERNAL_ENTITY_HANDLING:
		pszErr = "XML_ERROR_EXTERNAL_ENTITY_HANDLING";
		break;
	case XML_ERROR_NOT_STANDALONE:
		pszErr = "XML_ERROR_NOT_STANDALONE";
		break;
	default:
		sprintf_s(rgchErr, sizeof(rgchErr), "Unknown: %d", xerr);
		pszErr = rgchErr;
		break;
	}
	int cline = XML_GetCurrentLineNumber(parser);
	int ccol = XML_GetCurrentColumnNumber(parser);
	long cbyte = XML_GetCurrentByteIndex(parser);
	if (pszFile && *pszFile)
	{
		staMsg.Format("XML error: %s (%s; line %d, column %d, byte %ld)\n",
			pszErr, pszFile, cline, ccol, cbyte);
	}
	else
	{
		staMsg.Format("XML error: %s (line %d, column %d, byte %ld)\n",
			pszErr, cline, ccol, cbyte);
	}
}
