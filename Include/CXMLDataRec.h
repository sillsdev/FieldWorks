/***********************************************************************************************
File: CXMLDataRec.h

Owner: Summer Institute of Linguistics, 7500 West Camp Wisdom Road, Dallas,
Texas 75237. (972)708-7400.

Notice: All rights reserved Worldwide. This material contains the valuable
properties of the Summer Institute of Linguistics of Dallas, Texas,
United States of America, embodying substantial creative efforts and
confidential information, ideas and expressions, NO PART of which may be
reproduced or transmitted in any form or by any means, electronic,
mechanical, or otherwise, including photographic and recording or in
connection with any information storage or retrieval system without the
permission in writing from the Summer Institute of Linguistics.

COPYRIGHT (C) 1998 by the Summer Institute of Linguistics. All rights reserved.
Insert your summary of the file contents and purpose here.

Responsibility: Jeff Gayle.
Last reviewed: Not yet.
Creation Date: 03/01/1999
***********************************************************************************************/
#ifndef __CXMLDataRec_H_
#define __CXMLDataRec_H_

/***********************************************************************************************
	Include files
***********************************************************************************************/
/***********************************************************************************************
	Forward Declarations
***********************************************************************************************/
class CExPatParser;
/***********************************************************************************************
	Other Declarations
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
Class: CXMLDataReceiver
Description:
Comments:
----------------------------------------------------------------------------------------------*/
class CXMLDataReceiver
{
public:
	// Static Methods
	// Constructors/destructor/assign operators/initializers
	virtual ~CXMLDataReceiver();

	// Read/write methods for member variables
	// methods that will be optionally overridden in a derived class
	virtual void StartElementHandler(CExPatParser *pParser,	const wchar_t * szwInName,
		const wchar_t ** pszwInAttribs);

	virtual void EndElementHandler(CExPatParser *pParser, const wchar_t * szwInName);

	virtual void CharacterDataHandler(CExPatParser *pParser, const wchar_t *pchwInData,
		int ctnInLength);

	virtual void ProcesingInstructionHandler(CExPatParser *pParser, const wchar_t *szwInTarget,
		const wchar_t *pchwInData);

	virtual void DefaultHandler(CExPatParser *pParser, const wchar_t *pchwInData,
		int ctnInLength);

	virtual void UnparsedEntityDeclHandler(CExPatParser *pParser,
		const wchar_t *szwInEntityName, const wchar_t *szwInBaseAddress,
		const wchar_t *szwInSystemID, const wchar_t *szwInPublicID,
		const wchar_t *szwInNotationName);

	virtual void NotationDeclHandler(CExPatParser *pParser, const wchar_t *szwInNotationName,
		const wchar_t *szwInBaseAddress, const wchar_t *szwInSystemID,
		const wchar_t *szwInPublicID);

	virtual	int	ExternalEntityRefHandler(CExPatParser *pParser,
		const wchar_t *szwInOpenEntityNames, const wchar_t *szwInBaseAddress,
		const wchar_t *szwInSystemID, const wchar_t *szwInPublicID);
};
#endif	// __CXMLDataRec_H_