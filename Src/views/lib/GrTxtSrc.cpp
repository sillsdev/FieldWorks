/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrTxtSrc.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	A simple text source that shows how to use this interface within Graphite.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructors.
----------------------------------------------------------------------------------------------*/
GrSimpleTextSource::GrSimpleTextSource(gr::utf16 * pszText,
	gr::utf16 * szFaceName, int ichFaceName, int pointSize, bool fBold, bool fItalic, bool fRtl)
	//gr::utf16 * prgchFontVar, int ichFontVar)
{
	//Assert(ichFontVar < isizeof(m_rgchFontVar));

	m_cchLength = u_strlen(pszText);
	m_prgchText = NewObj gr::utf16[m_cchLength + 1];
	memcpy(m_prgchText, pszText, isizeof(gr::utf16) * m_cchLength);
	m_prgchText[m_cchLength] = 0; // zero-terminate

	wcscpy(m_szFaceName, szFaceName);
	m_pointSize = pointSize;
	m_fBold = fBold;
	m_fItalic = fItalic;
	m_fRtl = fRtl;

	//memset(m_rgchFontVar, 0, isizeof(m_rgchFontVar));
	//memcpy(m_rgchFontVar, prgchFontVar, min(ichFontVar, isizeof(m_rgchFontVar)));
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
GrSimpleTextSource::~GrSimpleTextSource()
{
	delete[] m_prgchText;
}

/*----------------------------------------------------------------------------------------------
	Get the specified range of text
----------------------------------------------------------------------------------------------*/
size_t GrSimpleTextSource::fetch(toffset ichMin, size_t cch, gr::utf16 * prgchwBuffer)
{
	size_t ichRet = std::min(cch, (size_t)(m_cchLength - ichMin));
	memcpy(prgchwBuffer, &m_prgchText, isizeof(gr::utf16) * ichRet);
	return ichRet;
}

/*----------------------------------------------------------------------------------------------

	Return true if the text uses a right-to-left writing system.
----------------------------------------------------------------------------------------------*/
bool GrSimpleTextSource::getRightToLeft(toffset ich)
{
	return m_fRtl;
}

/*----------------------------------------------------------------------------------------------
	Return the depth of embedding of the writing system.
----------------------------------------------------------------------------------------------*/
unsigned int GrSimpleTextSource::getDirectionDepth(toffset ich)
{
	if (m_fRtl)
		return 1;
	else;
		return 0;
}

/*----------------------------------------------------------------------------------------------
	Return the vertical offset of the text. This simple implementation provides no
	vertical offset.
----------------------------------------------------------------------------------------------*/
float GrSimpleTextSource::getVerticalOffset(toffset ich)
{
	return 0;
}
