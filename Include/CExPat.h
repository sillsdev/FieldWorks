/*-----------------------------------------------------------------------------------------------
File: CExPat.h
Responsibility: Jeff Gayle.
Last reviewed: Not yet.

Owner: Summer Institute of Linguistics, 7500 West Camp Wisdom Road, Dallas,
Texas 75237. (972)708-7400.

Notice: All rights reserved Worldwide. This material contains the valuable properties of the
Summer Institute of Linguistics of Dallas, Texas, United States of America, embodying
substantial creative efforts and confidential information, ideas and expressions, NO PART of
which may be reproduced or transmitted in any form or by any means, electronic, mechanical, or
otherwise, including photographic and recording or in connection with any information storage or
retrieval system without the permission in writing from the Summer Institute of Linguistics.
COPYRIGHT (C) 1998 by the Summer Institute of Linguistics. All rights reserved.

Description : Class to wrap Jim Clark's "C" based expat XML parsing code with a C++. Requires
	helper class CXMLDataRec to implement the callbacks. One notable added capability is that
	more then one of these callback(receiver) objects may be created and then passed to this
	class which stores them in a stack model.  Changing the behavior of the interpreter can
	simply be done by pushing a new receiver object onto the stack and poping it to return to
	previous processing model.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef __CEXPAT_H_
#define __CEXPAT_H_

/***********************************************************************************************
	Include files
***********************************************************************************************/
/***********************************************************************************************
	Forward Declarations
***********************************************************************************************/
class CXMLDataReceiver;

/***********************************************************************************************
	Other Declarations
***********************************************************************************************/
enum ErrorCodes	{E_NO_ERROR = 0, E_XMLPARSER_INIT_FAILED = 101, E_NO_HANDLERS_REGISTERED,
	E_CANNOT_OPEN_FILE, E_NOMEMORY_PARSEBUFFER};
enum EncodingName {None, UTF_8, UTF_16, NONUNICODE};
const int g_VersionStringSize = 20;
/*----------------------------------------------------------------------------------------------
Class: CExPatParser
Description:	This class encapsulates the Expat XML Parser from Jim Clark. It	enforces
				Unicode/WideChar use so all strings passed to receiver objects are wchar_t it is
				also resticted to reading UTF8 and UTF16, (or the other encodings supported buy
				expat, see source), encoded files, arbitrary encoding functionallity is not
				exposed because the call back is not handed the userdata from XML_Parser.

				simple use senario:
					Derive from CXMLDataReceiver, CMyReciever, overriding the call back methods
					of interest.
					Push new receiver onto receiver stack in the XMLExPatParser object.
					Call XMLExPatParser::Parse method with appropiate argument.
					Check for parse errors.

Hungarian: CEPP
----------------------------------------------------------------------------------------------*/
class CExPatParser
{
public:
	// Static Methods
	// methods that will be used by class to register with expat
	static void	ExPat_StartElementHandler(void * pUserData, const XML_Char * szwName,
		const XML_Char ** ppAttribs);
	static void ExPat_EndElementHandler(void * pUserData, const XML_Char * szwName);
	static void ExPat_CharacterDataHandler(void * pUserData, const XML_Char * pchwData,
		int ctnLength);
	static void ExPat_ProcessingInstructionHandler(void * pUserData,
		const XML_Char * szwTarget,	const XML_Char * pchwData);
	static void ExPat_DefaultHandler(void * pUserData, const XML_Char * pchwData,
		int ctnLength);
	static void ExPat_UnparsedEntityDeclHandler(void * pUserData,
		const XML_Char * szwEntityName, const XML_Char * szwBaseAddress,
		const XML_Char * szwSystemID, const XML_Char * szwPublicID,
		const XML_Char * szwNotationName);
	static void ExPat_NotationDeclHandler(void * pUserData, const XML_Char * szwNotationName,
		const XML_Char * szwBaseAddress, const XML_Char * szwSystemID,
		const XML_Char * szwPublicID);
	static int ExPat_ExternalEntityRefHandler(XML_Parser pXMLParser,
		const XML_Char * szwOpenEntityNames, const XML_Char * szwBaseAddress,
		const XML_Char * szwSystemID, const XML_Char * szwPublicID);
	static int ExPat_UnknownEncodingHandler(void * pEncodingHandlerData,
		const XML_Char * szwName, XML_Encoding * pEncodingInfo);

	// Constructors/destructor/assign operators/initializers
	CExPatParser();
	CExPatParser(wchar_t * pszwEncodingName);

	// desctructor
	virtual ~CExPatParser();// calls XML_ParserFree (m_pExPatParser);

	// read/write methogs for member variables
	void SetDataBufferParseIncrement(unsigned int ctuSize);
	EncodingName GetEncodingName(void);

	// copies string from parameter into parse struct
	HRESULT SetAddressBase(const wchar_t * szwBaseAddress);

	// copies from parse struct to the parameter
	const wchar_t *GetAddressBase(void);

	// error handling code
	int GetErrorCode(void);
	void GetErrorString(wchar_t * pszwOutErrorString, int inErrorCode);
	void GetErrorString(wchar_t * pszwOutErrorString);
	int GetCurrentLineNumber(void);
	int GetCurrentColumnNumber(void);

	// Reciever objects call these to set the error string
	void SetCallbackErrorString(wchar_t * pszwOutErrorString);
	void SetEXPatCallBackError(int inCallbackError);

	unsigned long GetCurrentBufferOffset(void);

	// calls XML_ExternalEntityParserCreate(pExPatParser, pszwOpenEntityList, NULL);
	CExPatParser *CreateExternalEntityParser(const wchar_t * szwOpenEntityNames,
		const wchar_t * szwEncoding);

	// receiver stack methods.
	void PopReceiver(void);
	void PushReceiver(CXMLDataReceiver * pReceiver);
	bool IsReceiverEmpty(void);
	CXMLDataReceiver *GetCurrentReceiver(void);

	// methods that actuall start the parse process
	HRESULT Parse(wchar_t * szwFileName);
	HRESULT Parse(char * szsFileName);
	HRESULT Parse(std::istream * pInputStream);
	HRESULT Parse(void * pDataBuffer, int ctnBufSize, bool fIsFinal);

protected:
	// member variables
	XML_Parser m_pExPatParser;	// from Expat code, XML_Parser included the * for the pointer
	bool m_fParserInitialized;
	bool m_fPrologProcessed;
	bool m_fStandalone;
	EncodingName m_Encoding;
	wchar_t	m_szwVersion[g_VersionStringSize];
	ErrorCodes m_enErrorCodes;	// class error codes, not expat internal error codes
	unsigned int m_uDataChunkSize;
	std::vector <CXMLDataReceiver *>	m_ReceiverVec;	// holds receiver object pointers
	// static methods
	// read/write methods for protected member variables
	// Constructors/destructor
	// create new instance passing in XML_Parser, copies instead of calling XML_ParserCreate
	CExPatParser(XML_Parser pXMLParser);	// ************WARNING: untested************
	bool GetPrologParameter(const BSTR pbstrInSource, const wchar_t * pszwParamName,
		wchar_t * pszwOutValue, size_t nSizeOfValueBuf);
	bool ProcessProlog(const XML_Char * pchwData, int ctnLength);
	CComBSTR m_bstrCallBackError;
	};

#endif	// __CEXPAT_H_