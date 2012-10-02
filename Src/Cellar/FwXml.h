/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, 2004 by SIL International. All rights reserved.

File: FwXml.h
Responsibility: Steve McConnel
Last reviewed:
	This file declares functions and data structures used to parse the FieldWorks XML
	representation of string data.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef FwXml_h
#define FwXml_h 1
//:End Ignore

#include "xmlparse.h"

namespace FwXml
{
	/*------------------------------------------------------------------------------------------
		This contains one entry for the array of basic run information stored for formatted
		strings.
		Hungarian: bri
	------------------------------------------------------------------------------------------*/
	struct BasicRunInfo
	{
		int m_ichMin;		// Starting offset of the run within the character array.
		int m_ibProp;		// Starting offset of the run within the formatting data.
	};

	/*------------------------------------------------------------------------------------------
		TextGuidValuedProp contains one GUID-valued text property stored for formatted strings.
		Hungarian: tgvp
	------------------------------------------------------------------------------------------*/
	struct TextGuidValuedProp
	{
		int m_tpt;		// String-valued property code: must be either kstpTags or kstpObjData.
		OLECHAR m_chType; // kodt{Own}NameGuidHot, gives subtype for kstpObjData.
		Vector<GUID> m_vguid;	// Value of this property.
	};

	/*------------------------------------------------------------------------------------------
		RunPropInfo contains the property information for one run.
		Hungarian: rpi
	------------------------------------------------------------------------------------------*/
	struct RunPropInfo
	{
		byte m_ctip;				// Number of integer-valued properties.
		byte m_ctsp;				// Number of string-valued properties.
		Vector<byte> m_vbRawProps;	// Binary data representing the properties.
	};

	/*------------------------------------------------------------------------------------------
		Distinguish among the various kinds of data stored in a Run element.
		Hungarian: rdt
	------------------------------------------------------------------------------------------*/
	typedef enum
	{
		krdtChars = 1,
		krdtPicture,
		krdtBad = 0
	} RunDataType;


	const char * GetAttributeValue(const XML_Char ** prgpszAtts, const char * pszName);
	bool ParseGuid(const char * pszGuid, GUID * pguidRet);

#define kcptRuleProp 100			// Must differ from any in CmTypes.h
	int BasicType(const char * pszName);

	void HandleStringStartTag(void * pvUser, const XML_Char * pszName,
		const XML_Char ** prgpszAtts);
	void HandleStringEndTag(void * pvUser, const XML_Char * pszName);
	void HandleCharData(void * pvUser, const XML_Char * prgch, int cch);

	void XmlErrorDetails(XML_Parser parser, const char * pszFile, StrAnsi & staMsg);
};


// The next three lines are useful for Steve McConnel's editing with Emacs.
// Local Variables:
// mode:C++
// End:

#endif /*FwXml_h*/
