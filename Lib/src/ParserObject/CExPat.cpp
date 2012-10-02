/*----------------------------------------------------------------------------------------------
File: CExPat.cpp
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
/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Forward Declarations
***********************************************************************************************/

/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

/***********************************************************************************************
	Methods
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Default constuctor for class
	Arguments: None
----------------------------------------------------------------------------------------------*/
CExPatParser::CExPatParser()
{
	m_fParserInitialized = false;
	m_fPrologProcessed = false;
	m_fStandalone = true;
	m_Encoding = UTF_8;
	wcscpy (m_szwVersion, L"1.0");
	m_enErrorCodes = E_NO_ERROR;
	m_uDataChunkSize = 1024*8;	// 8K
	m_pExPatParser = XML_ParserCreate(NULL);

	if (m_pExPatParser) {
		// set the userdata in XML_Parser structure to point to the object
		XML_SetUserData(m_pExPatParser, this);
		m_fParserInitialized = true;
	}
	else {
		m_enErrorCodes = E_XMLPARSER_INIT_FAILED;
	}

	// set up default call back functions
	m_ReceiverVec.push_back(new CXMLDataReceiver);

	// register the static call backs with expat
	XML_SetElementHandler(m_pExPatParser, ExPat_StartElementHandler, ExPat_EndElementHandler);
	XML_SetCharacterDataHandler(m_pExPatParser, ExPat_CharacterDataHandler);
	XML_SetProcessingInstructionHandler(m_pExPatParser, ExPat_ProcessingInstructionHandler);
	XML_SetDefaultHandler(m_pExPatParser, ExPat_DefaultHandler);
	XML_SetUnparsedEntityDeclHandler(m_pExPatParser, ExPat_UnparsedEntityDeclHandler);
	XML_SetNotationDeclHandler(m_pExPatParser, ExPat_NotationDeclHandler);
	XML_SetExternalEntityRefHandler(m_pExPatParser, ExPat_ExternalEntityRefHandler);
	XML_SetUnknownEncodingHandler(m_pExPatParser, ExPat_UnknownEncodingHandler, NULL);
}

/*----------------------------------------------------------------------------------------------
	Constructor for class
	Arguments:	pszwEncodingName - initialize with this encoding string.
----------------------------------------------------------------------------------------------*/
CExPatParser::CExPatParser(wchar_t *pszwEncodingName)
{
	m_fParserInitialized = false;
	m_fPrologProcessed = false;
	m_fStandalone = true;
	m_Encoding = UTF_8;
	wcscpy (m_szwVersion, L"1.0");
	m_enErrorCodes = E_NO_ERROR;
	m_uDataChunkSize = 1024*8;	// 8K
	m_pExPatParser = XML_ParserCreate(pszwEncodingName);

	if (m_pExPatParser)
	{
		// set the userdata in XML_Parser structure to point to the object
		XML_SetUserData(m_pExPatParser, this);
		m_fParserInitialized = true;
	}
	else
	{
		m_enErrorCodes = E_XMLPARSER_INIT_FAILED;
	}

	// set up default call back functions
	m_ReceiverVec.push_back(new CXMLDataReceiver);

	// register the static call backs with expat
	XML_SetElementHandler(m_pExPatParser, ExPat_StartElementHandler, ExPat_EndElementHandler);
	XML_SetCharacterDataHandler(m_pExPatParser, ExPat_CharacterDataHandler);
	XML_SetProcessingInstructionHandler(m_pExPatParser, ExPat_ProcessingInstructionHandler);
	XML_SetDefaultHandler(m_pExPatParser, ExPat_DefaultHandler);
	XML_SetUnparsedEntityDeclHandler(m_pExPatParser, ExPat_UnparsedEntityDeclHandler);
	XML_SetNotationDeclHandler(m_pExPatParser, ExPat_NotationDeclHandler);
	XML_SetExternalEntityRefHandler(m_pExPatParser, ExPat_ExternalEntityRefHandler);
	XML_SetUnknownEncodingHandler(m_pExPatParser, ExPat_UnknownEncodingHandler, NULL);
}

/*----------------------------------------------------------------------------------------------
	************UNTESTED***********
	Constuctor for this class
	Arguments: XML Parser to clone
----------------------------------------------------------------------------------------------*/
CExPatParser::CExPatParser(XML_Parser pXMLParser)
{
	m_fParserInitialized = true;
	m_fPrologProcessed = false;
	m_fStandalone = true;
	m_Encoding = UTF_8;
	wcscpy (m_szwVersion, L"1.0");
	m_enErrorCodes = E_NO_ERROR;
	m_uDataChunkSize = 1024*8;	// 8K
	m_pExPatParser = pXMLParser;

	// set the userdata in XML_Parser structure to point to the object
	XML_SetUserData(m_pExPatParser, this);

	// set up default call back functions
	m_ReceiverVec.push_back(new CXMLDataReceiver);

	// register the static call backs with expat
	XML_SetElementHandler(m_pExPatParser, ExPat_StartElementHandler, ExPat_EndElementHandler);
	XML_SetCharacterDataHandler(m_pExPatParser, ExPat_CharacterDataHandler);
	XML_SetProcessingInstructionHandler(m_pExPatParser, ExPat_ProcessingInstructionHandler);
	XML_SetDefaultHandler(m_pExPatParser, ExPat_DefaultHandler);
	XML_SetUnparsedEntityDeclHandler(m_pExPatParser, ExPat_UnparsedEntityDeclHandler);
	XML_SetNotationDeclHandler(m_pExPatParser, ExPat_NotationDeclHandler);
	XML_SetExternalEntityRefHandler(m_pExPatParser, ExPat_ExternalEntityRefHandler);
	XML_SetUnknownEncodingHandler(m_pExPatParser, ExPat_UnknownEncodingHandler, NULL);
}

/*----------------------------------------------------------------------------------------------
	Desctuctor for this class
	Arguments: None
----------------------------------------------------------------------------------------------*/
CExPatParser::~CExPatParser ()
{
	XML_ParserFree(m_pExPatParser);

	// remove all the receiver pointers
	while (false == m_ReceiverVec.empty())
		m_ReceiverVec.pop_back();
}

/***********************************************************************************************
	Static methods registered with with Expat
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Start element handler.
	Arguments:	pUserData - user data passed, already used
				szwName - tag name
				ppAttribs - array of strings, attrib name value pairs.
----------------------------------------------------------------------------------------------*/
void CExPatParser::ExPat_StartElementHandler(void * pUserData, const XML_Char * szwName,
	const XML_Char ** ppAttribs)
{
	CExPatParser *pCExPatParser = reinterpret_cast <CExPatParser *> (pUserData);
	pCExPatParser->m_ReceiverVec.back()->StartElementHandler(pCExPatParser,
		szwName, ppAttribs);
}

/*----------------------------------------------------------------------------------------------
	End element handler
	Arguments:	pUserData - user data passed, already used
				szwName - tag name
----------------------------------------------------------------------------------------------*/
void CExPatParser::ExPat_EndElementHandler(void * pUserData, const XML_Char * szwName)
{
	CExPatParser * pCExPatParser = reinterpret_cast <CExPatParser *> (pUserData);
	pCExPatParser->m_ReceiverVec.back()->EndElementHandler(pCExPatParser, szwName);
}

/*----------------------------------------------------------------------------------------------
	Character data handler.
	Arguments:	pUserData - user data passed, already used
				pchwData - data found between tags.
				ctnLength - amount of data found between tags.
----------------------------------------------------------------------------------------------*/
void CExPatParser::ExPat_CharacterDataHandler(void * pUserData, const XML_Char *pchwData,
	int ctnLength)
{
	CExPatParser * pCExPatParser = reinterpret_cast <CExPatParser *> (pUserData);
	pCExPatParser->m_ReceiverVec.back()->CharacterDataHandler(pCExPatParser,
		pchwData, ctnLength);
}

/*----------------------------------------------------------------------------------------------
	Instruction handler.
	Arguments:	pUserData - user data passed, already used
				szwTarget - ???
				pchwData - data found between tags.
----------------------------------------------------------------------------------------------*/
void CExPatParser::ExPat_ProcessingInstructionHandler(void * pUserData,
	const XML_Char * szwTarget, const XML_Char * pchwData)
{
	CExPatParser * pCExPatParser = reinterpret_cast <CExPatParser *> (pUserData);
	pCExPatParser->m_ReceiverVec.back()->ProcesingInstructionHandler(pCExPatParser,
		szwTarget, pchwData);
}

/*----------------------------------------------------------------------------------------------
	Called when no other handler is registered.
	Arguments:	pUserData - user data passed, already used
				pchwData - data found between tags.
				ctnLength - amount of data found between tags.
----------------------------------------------------------------------------------------------*/
void CExPatParser::ExPat_DefaultHandler(void * pUserData, const XML_Char * pchwData,
	int ctnLength)
{
	CExPatParser * pCExPatParser = reinterpret_cast <CExPatParser *> (pUserData);
	if (!pCExPatParser->m_fPrologProcessed && !wcsncmp (pchwData, L"<?xml", 5))
		pCExPatParser->m_fPrologProcessed = pCExPatParser->ProcessProlog (pchwData, ctnLength);

	pCExPatParser->m_ReceiverVec.back()->DefaultHandler(pCExPatParser, pchwData, ctnLength);
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
void CExPatParser::ExPat_UnparsedEntityDeclHandler(void * pUserData,
	const XML_Char * szwEntityName, const XML_Char * szwBaseAddress, const XML_Char * szwSystemID,
	const XML_Char * szwPublicID, const XML_Char * szwNotationName)
{
	CExPatParser * pCExPatParser = reinterpret_cast <CExPatParser *> (pUserData);
	pCExPatParser->m_ReceiverVec.back()->UnparsedEntityDeclHandler(pCExPatParser, szwEntityName,
		szwBaseAddress,	szwSystemID, szwPublicID, szwNotationName);
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
void CExPatParser::ExPat_NotationDeclHandler(void * pUserData, const XML_Char * szwNotationName,
	const XML_Char * szwBaseAddress, const XML_Char * szwSystemID, const XML_Char * szwPublicID)
{
	CExPatParser * pCExPatParser = reinterpret_cast <CExPatParser *> (pUserData);
	pCExPatParser->m_ReceiverVec.back()->NotationDeclHandler(pCExPatParser, szwNotationName,
		szwBaseAddress, szwSystemID, szwPublicID);
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
int CExPatParser::ExPat_ExternalEntityRefHandler(XML_Parser pXMLParser,
	const XML_Char * szwOpenEntityNames, const XML_Char * szwBaseAddress,
	const XML_Char * szwSystemID, const XML_Char * szwPublicID)
{
	CExPatParser * pCExPatParser = reinterpret_cast <CExPatParser *> (XML_GetUserData (pXMLParser));
	return pCExPatParser->m_ReceiverVec.back()->ExternalEntityRefHandler(pCExPatParser,
		szwOpenEntityNames, szwBaseAddress, szwSystemID, szwPublicID);
}

/*----------------------------------------------------------------------------------------------
	Called for unknown encodings. NONUNICODE support is provided here.
	Arguments:
----------------------------------------------------------------------------------------------*/
int CExPatParser::ExPat_UnknownEncodingHandler(void * pEncodingHandlerData,
	const XML_Char * szwName, XML_Encoding * pEncodingInfo)
{
	// make sure the name is NONUNICODE
	if (!_wcsicmp (szwName, L"NONUNICODE"))
	{
		 // set the map to pass the value through as the unicode offset
		 for (int i = 0; i< 256; i++)
			pEncodingInfo->map[i] = i;

		return(1);
	}
	return(0);
}


/***********************************************************************************************
	Methods that start the parse process
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Parses the file
	Arguments: File name to parse
----------------------------------------------------------------------------------------------*/
HRESULT CExPatParser::Parse(wchar_t * szwInFileName)
{
	char * pszsFileName;

	USES_CONVERSION;
	pszsFileName = W2A(szwInFileName);
	return Parse(pszsFileName);
}

/*----------------------------------------------------------------------------------------------
	Parses the file
	Arguments: File name to parse
----------------------------------------------------------------------------------------------*/
HRESULT CExPatParser::Parse(char * szsFileName)
{

	std::ifstream inStream;
	HRESULT parseStatus;

	// open the file as a stream
	inStream.open(szsFileName, std::ios::binary);
	if (inStream.fail()) {
		m_enErrorCodes = E_CANNOT_OPEN_FILE;
		return E_FAIL;
	}

	// Parse the stream
	parseStatus = Parse(&inStream);

	// close stream
	inStream.close();

	// return status from Parse
	return parseStatus;
}

/*----------------------------------------------------------------------------------------------
	******************UNTESTED*********************
	Parses the buffer
	Arguments:	pDataBuffer - buffer to parse
				ctnBufSize - size of the buffer
				fIsFinal - true if this is the last chunk to process
----------------------------------------------------------------------------------------------*/
HRESULT CExPatParser::Parse(void * pDataBuffer, int ctnBufSize, bool fIsFinal)
{
	int	nParseReturnCode;

	// check for initialized parser
	if (m_enErrorCodes)
		return E_FAIL;

	nParseReturnCode = XML_Parse(m_pExPatParser, reinterpret_cast <const char *>(pDataBuffer),
		ctnBufSize, fIsFinal);
	if (!nParseReturnCode)
		return E_FAIL;

	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Parses the stream
	Arguments:	pInputStream - stream to read from
----------------------------------------------------------------------------------------------*/
HRESULT CExPatParser::Parse(std::istream * pInputStream)
{
	char	* pbDataBuffer = NULL;

	// check for initialized parser
	if (m_enErrorCodes)
		return E_FAIL;

	// get chunk of data to write to
	pbDataBuffer = new char[m_uDataChunkSize];
	if (pbDataBuffer == 0)
	{
		m_enErrorCodes = E_NOMEMORY_PARSEBUFFER;
		return E_FAIL;
	}

	while (!pInputStream->eof())
	{
		bool fIsFinal;
		int	 nParseReturnCode;
		size_t ctluBytesRead;

		// get chunk of data to parse from the stream, ChunkSize is in bytes, convert to wc
		pInputStream->read(pbDataBuffer, m_uDataChunkSize);
		ctluBytesRead = pInputStream->gcount();
		fIsFinal = ctluBytesRead < m_uDataChunkSize;
		nParseReturnCode = XML_Parse(m_pExPatParser, pbDataBuffer, ctluBytesRead, (int) fIsFinal);
		if (!nParseReturnCode)
			return E_FAIL;
	}
	delete [] pbDataBuffer;
	return S_OK;
}


/***********************************************************************************************
	General Utility Methods
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Sets the base address for all subsequent external references
	Arguments:	szwBaseAddress - directory base path
----------------------------------------------------------------------------------------------*/
HRESULT CExPatParser::SetAddressBase(const wchar_t * szwBaseAddress)
{
	// copies string from parameter into parse struct
	if (XML_SetBase(m_pExPatParser, szwBaseAddress))
		return(S_OK);

	return(E_FAIL);
}

/*----------------------------------------------------------------------------------------------
	Gets the base address for all subsequent external references
	Arguments: None
----------------------------------------------------------------------------------------------*/
const wchar_t * CExPatParser::GetAddressBase(void)
{
	// copies from parse struct to the parameter
	return (XML_GetBase(m_pExPatParser));
}

/*----------------------------------------------------------------------------------------------
	Gets the last error code
	Arguments:	None
----------------------------------------------------------------------------------------------*/
int CExPatParser::GetErrorCode(void)
{
	if (m_enErrorCodes)	// get internal error code
		return(m_enErrorCodes);
	else
		return(XML_GetErrorCode(m_pExPatParser));
}

/*----------------------------------------------------------------------------------------------
	Gets the error string based on error code
	Arguments:	pszwOutErrorString -  the error string
				inErrorCode - the code to look up
----------------------------------------------------------------------------------------------*/
void CExPatParser::GetErrorString(wchar_t * pszwOutErrorString, int inErrorCode)
{
	if (pszwOutErrorString == NULL)
		return;

#ifdef XML_ERROR_CALLBACK_SET_ERROR
	if (inErrorCode == XML_ERROR_CALLBACK_SET_ERROR)	// get error code from Receiver
	{
		if (m_bstrCallBackError.Length())
			wcsncpy(pszwOutErrorString, reinterpret_cast<wchar_t *>(BSTR(m_bstrCallBackError)),
				wcslen(pszwOutErrorString));
	}
	else
#endif
	if (inErrorCode >= E_XMLPARSER_INIT_FAILED)
	{
		int inCode = (inErrorCode % 100);
		static const wchar_t *message[] =
		{
			0,
			(L"Out of memory while trying to initialize XML Parser"),
			(L"No handlers registered"),
			(L"Input file cannot be opened"),
			(L"Out of memory allocating parse buffer" ),
		};

		if (inCode > 0 && inCode < sizeof(message)/sizeof(message[0]))
			wcsncpy(pszwOutErrorString, message[inCode], wcslen(pszwOutErrorString));
	}
	else
	{
		wcsncpy (pszwOutErrorString, XML_ErrorString(inErrorCode),
			wcslen(pszwOutErrorString));
	}
}

/*----------------------------------------------------------------------------------------------
	Get the last error.
	Arguments:	pszwOutErrorString - current error string
----------------------------------------------------------------------------------------------*/
void CExPatParser::GetErrorString(wchar_t * pszwOutErrorString)
{
	if (m_enErrorCodes >= E_XMLPARSER_INIT_FAILED)
		GetErrorString(pszwOutErrorString, m_enErrorCodes);
	else
		GetErrorString(pszwOutErrorString, XML_GetErrorCode(m_pExPatParser));
}

/*----------------------------------------------------------------------------------------------
	Sets the error sting when the error is generated in callback.
	Arguments:	pszwOutErrorString - error string
----------------------------------------------------------------------------------------------*/
void CExPatParser::SetCallbackErrorString(wchar_t * pszwOutErrorString)
{
	if (pszwOutErrorString == NULL)
		return;

	m_bstrCallBackError = pszwOutErrorString;
}

/*----------------------------------------------------------------------------------------------
	Sets the numeric error code when error is generated in the callback
	Arguments:	inCallbackError - error to set
----------------------------------------------------------------------------------------------*/
void CExPatParser::SetEXPatCallBackError(int inCallbackError)
{
#ifdef XML_ERROR_CALLBACK_SET_ERROR
	XML_SetErrorCode(m_pExPatParser, XML_ERROR_CALLBACK_SET_ERROR);
	XML_SetCallBackError (m_pExPatParser, inCallbackError);
#endif
}

/*----------------------------------------------------------------------------------------------
	Returns the line number where parser stopped parsing
	Arguments: None
----------------------------------------------------------------------------------------------*/
int CExPatParser::GetCurrentLineNumber (void)
{
	return(XML_GetCurrentLineNumber(m_pExPatParser));
}

/*----------------------------------------------------------------------------------------------
	Returns the column number where parser stopped parsing
	Arguments: None
----------------------------------------------------------------------------------------------*/
int CExPatParser::GetCurrentColumnNumber (void)
{
	return (XML_GetCurrentColumnNumber(m_pExPatParser));
}

/*----------------------------------------------------------------------------------------------
	Returns the byte offset in buffer where parser stopped parsing.
	Arguments: None
----------------------------------------------------------------------------------------------*/
unsigned long CExPatParser::GetCurrentBufferOffset()
{
	return (XML_GetCurrentByteIndex(m_pExPatParser));
}

/*----------------------------------------------------------------------------------------------
	********************Not tested***************************
	Creates new parser object base on current one, used to parse external entities.
	Arguments:	szwOpenEntityNames - List of open entity names?
				szwEncoding - encoding to open with.
----------------------------------------------------------------------------------------------*/
CExPatParser * CExPatParser::CreateExternalEntityParser(const wchar_t * szwOpenEntityNames,
	const wchar_t * szwEncoding)
{
	XML_Parser pNewXMLParser;

	pNewXMLParser = XML_ExternalEntityParserCreate(m_pExPatParser, szwOpenEntityNames,
		szwEncoding);
	if (!pNewXMLParser)
		return (NULL);

	return (new CExPatParser(pNewXMLParser));
}

/*----------------------------------------------------------------------------------------------

	Arguments: IN: the new size of the buffer increment
----------------------------------------------------------------------------------------------*/
void CExPatParser::SetDataBufferParseIncrement(unsigned int ctuSize)
{
	m_uDataChunkSize = ctuSize;
}

/*----------------------------------------------------------------------------------------------
	Removes the top receiver object.
	Arguments: None
----------------------------------------------------------------------------------------------*/
void CExPatParser::PopReceiver(void)
{
	delete (m_ReceiverVec.back());	// call delete explicitly as STL container don't do pointers
	m_ReceiverVec.pop_back();
}

/*----------------------------------------------------------------------------------------------
	Adds the new receiver object to the top of the list
	Arguments:	pReceiver - points to new handler methods
----------------------------------------------------------------------------------------------*/
void CExPatParser::PushReceiver(CXMLDataReceiver * pReceiver)
{
	m_ReceiverVec.push_back (pReceiver);
}

/*----------------------------------------------------------------------------------------------
	Determines if there is a receiver object in the list.
	Arguments: None
----------------------------------------------------------------------------------------------*/
bool CExPatParser::IsReceiverEmpty(void)
{
	return (m_ReceiverVec.empty());
}

/*----------------------------------------------------------------------------------------------
	Returns the current receiver object.
	Arguments: None
----------------------------------------------------------------------------------------------*/
CXMLDataReceiver * CExPatParser::GetCurrentReceiver(void)
{
	return (m_ReceiverVec.back());
}

/*----------------------------------------------------------------------------------------------
	Returns the encoding enumeration
	Arguments: None
----------------------------------------------------------------------------------------------*/
EncodingName CExPatParser::GetEncodingName(void)
{
	return(m_Encoding);
}

/*----------------------------------------------------------------------------------------------
	Returns the value of the prolog parameter passed in.
	Arguments:	pbstrSource	- string to scan
				pszwParamName - parameter name to look for
				pszwOutValue - value of the paramter is stored here
				nSizeOfValueBuf - size of the output buffer
----------------------------------------------------------------------------------------------*/
bool CExPatParser::GetPrologParameter(const BSTR pbstrSource, const wchar_t * pszwParamName,
	wchar_t * pszwOutValue, size_t nSizeOfValueBuf)
{
	int	i, nStringSize;
	wchar_t * pszwStartStr,	* pszwEndStr;

	if (NULL != pszwOutValue && nSizeOfValueBuf > 0)
		pszwOutValue[0] = '\0';	// NULL terminate the string

	if (NULL != (pszwStartStr = wcsstr (pbstrSource, pszwParamName)))
	{
		pszwStartStr = wcsstr (pszwStartStr, L"\"");	// get past the keyword
		pszwStartStr++;	// get past the double quote
		pszwEndStr = wcsstr (pszwStartStr, L"\"");	// get the double quote after the value
		// copy the string to the value
		nStringSize = pszwEndStr - pszwStartStr;
		for (i = 0; (i < nStringSize && (size_t)i < (nSizeOfValueBuf - 1)); i++)
			pszwOutValue[i] = *pszwStartStr++;

		pszwOutValue[i] = '\0';	// NULL terminate the string
		return(true);
	}
	return(false);
}

/*----------------------------------------------------------------------------------------------
	Processes the prolog for encoding and version information
	Arguments:	pchwData - data found in the XML data stream
				ctnLength - length of the data buffer passed in
----------------------------------------------------------------------------------------------*/
bool CExPatParser::ProcessProlog(const XML_Char * pchwData, int ctnLength)
{
	CComBSTR bstrTmp = CComBSTR (ctnLength, pchwData);
	wchar_t szwTmp[30];
	bool fFoundParameter;

	// version string
	fFoundParameter = GetPrologParameter (BSTR(bstrTmp), L"version", m_szwVersion,
		g_VersionStringSize);

	//  encoding string
	if (GetPrologParameter (BSTR(bstrTmp), L"encoding", szwTmp, 30))
		if (!wcscmp (szwTmp, L"NONUNICODE"))
			m_Encoding = NONUNICODE;
		else if (!wcscmp (szwTmp, L"UTF-16"))
			m_Encoding = UTF_16;
		else
			m_Encoding = UTF_8;

	// standalone string if it exists
	if (GetPrologParameter (BSTR(bstrTmp), L"standalone", szwTmp, 30))
		if (!wcscmp (szwTmp, L"no"))
			m_fStandalone = false;
		else
			m_fStandalone = true;

	return(fFoundParameter);
}
