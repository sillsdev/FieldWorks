/*----------------------------------------------------------------------------------------------
File: CXMLDefRec.cpp
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

Description :
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
	virtual destructor
	Arguments:
----------------------------------------------------------------------------------------------*/
CXMLDataReceiver::~CXMLDataReceiver ()
{
}

/***********************************************************************************************
	Default behavior is to do nothing in the call back functions
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Start element handler.
	Arguments:	pUserData - user data passed, already used
				szwName - tag name
				ppAttribs - array of strings, attrib name value pairs.
----------------------------------------------------------------------------------------------*/
void CXMLDataReceiver::StartElementHandler (CExPatParser *pParser,
							  const wchar_t * szwInName, const wchar_t ** pszwInAttribs)
{
//	ATLTRACE(_T("Warning:Calling Baseclass handler: %ls \n"), _T("StartElementHandler"));
	return;
}

/*----------------------------------------------------------------------------------------------
	End element handler
	Arguments:	pUserData - user data passed, already used
				szwName - tag name
----------------------------------------------------------------------------------------------*/
void CXMLDataReceiver::EndElementHandler (CExPatParser *pParser, const wchar_t * szwInName)
{
//	ATLTRACE(_T("Warning:Calling Baseclass handler: %ls \n"), _T("EndElementHandler"));
	return;
}

/*----------------------------------------------------------------------------------------------
	Character data handler.
	Arguments:	pUserData - user data passed, already used
				pchwData - data found between tags.
				ctnLength - amount of data found between tags.
----------------------------------------------------------------------------------------------*/
void CXMLDataReceiver::CharacterDataHandler (CExPatParser *pParser,
											const wchar_t *pchwInData,
											int ctnInLength)
{
//	ATLTRACE(_T("Warning:Calling Baseclass handler: %ls \n"), _T("CharacterDataHandler"));
	return;
}

/*----------------------------------------------------------------------------------------------
	Instruction handler.
	Arguments:	pUserData - user data passed, already used
				szwTarget - ???
				pchwData - data found between tags.
----------------------------------------------------------------------------------------------*/
void CXMLDataReceiver::ProcesingInstructionHandler (CExPatParser *pParser,
													const wchar_t *szwInTarget,
													const wchar_t *pchwInData)
{
//	ATLTRACE(_T("Warning:Calling Baseclass handler: %ls \n"), _T("ProcesingInstructionHandler"));
	return;
}

/*----------------------------------------------------------------------------------------------
	Called when no other handler is registered.
	Arguments:	pUserData - user data passed, already used
				pchwData - data found between tags.
				ctnLength - amount of data found between tags.
----------------------------------------------------------------------------------------------*/
void CXMLDataReceiver::DefaultHandler (CExPatParser *pParser,
										const wchar_t *pchwInData,
										int ctnInLength)
{
//	ATLTRACE(_T("Warning:Calling Baseclass handler: %ls \n"), _T("DefaultHandler"));
	return;
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
void CXMLDataReceiver::UnparsedEntityDeclHandler (CExPatParser *pParser,
													const wchar_t *szwInEntityName,
													const wchar_t *szwInBaseAddress,
													const wchar_t *szwInSystemID,
													const wchar_t *szwInPublicID,
													const wchar_t *szwInNotationName)
{
//	ATLTRACE(_T("Warning:Calling Baseclass handler: %ls \n"), _T("UnparsedEntityDeclHandler"));
	return;
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
void CXMLDataReceiver::NotationDeclHandler (CExPatParser *pParser,
											const wchar_t *szwInNotationName,
											const wchar_t *szwInBaseAddress,
											const wchar_t *szwInSystemID,
											const wchar_t *szwInPublicID)
{
//	ATLTRACE(_T("Warning:Calling Baseclass handler: %ls \n"), _T("NotationDeclHandler"));
	return;
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
int	CXMLDataReceiver::ExternalEntityRefHandler (CExPatParser *pParser,
												const wchar_t *szwInOpenEntityNames,
												const wchar_t *szwInBaseAddress,
												const wchar_t *szwInSystemID,
												const wchar_t *szwInPublicID)
{
//	ATLTRACE(_T("Warning:Calling Baseclass handler: %ls \n"), _T("ExternalEntityRefHandler"));
	return 1;
}
