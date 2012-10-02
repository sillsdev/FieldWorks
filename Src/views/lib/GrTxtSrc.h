/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrTxtSrc.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:


-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef GRTXTSRC_INCLUDED
#define GRTXTSRC_INCLUDED

//typedef struct tagGrCharProps // TODO: remove
//{
//    unsigned long clrFore;
//    unsigned long clrBack;
//    int dympOffset;
//    int ws;
//    int ows;
//    byte fWsRtl;
//    int nDirDepth;
//    byte ssv;
//    byte ttvBold;
//    byte ttvItalic;
//    int dympHeight;
//    wchar_t szFaceName[ 32 ];
//    wchar_t szFontVar[ 64 ];
//} GrCharProps;

/*----------------------------------------------------------------------------------------------
	Class: GrSimpleTextSource
	This class provides a simple implementation for a text source for the Graphite engine.
	There are no paragraph properties of interest and one set of character properties that
	apply to the entire string.
----------------------------------------------------------------------------------------------*/
class GrSimpleTextSource : public gr::IColorTextSource
{
public:
	// Constructor:
	GrSimpleTextSource(gr::utf16 * pszText,
		gr::utf16 * szFaceName, int ichFaceName, int pointSize,
		bool fBold, bool fItalic, bool fRtl);
		//gr::utf16 * prgchFontVar, int ichFontVar);
	~GrSimpleTextSource();

	virtual UtfType utfEncodingForm()
	{
		return kutf16;
	}
	virtual size_t getLength()
	{
		return m_cchLength;
	}
	virtual size_t fetch(toffset ichMin, size_t cch, gr::utf32 * prgchBuffer)
	{
		throw;
	}
	virtual size_t fetch(toffset ichMin, size_t cch, gr::utf16 * prgchwBuffer);
	virtual size_t fetch(toffset ichMin, size_t cch, gr::utf8  * prgchsBuffer)
	{
		throw;
	};
	virtual bool getRightToLeft(toffset ich);
	virtual unsigned int getDirectionDepth(toffset ich);
	virtual float getVerticalOffset(toffset ich);
	virtual isocode getLanguage(toffset ich)
	{
		isocode lgcode;
		memset(lgcode.rgch, 0, sizeof(lgcode.rgch));
		return lgcode;
	}

	virtual void getRunRange(toffset ich, int * pichMin, int * pichLim)
	{
		*pichMin = 0;
		*pichLim = m_cchLength;
	}
	virtual size_t getFontFeatures(toffset ich, gr::FeatureSetting * prgfset)
	{
		return 0; // no features in this simple implementation
	}
	virtual bool sameSegment(toffset ich1, int ich2)
	{
		return true;
	}

	// Temporary--eventually these properties will be of interest only to SegmentPainter.
	virtual void getColors(toffset ich, int * pclrFore, int * pclrBack)
	{
		*pclrFore = kclrBlack;
		*pclrBack = kclrTransparent;
	}


protected:
	gr::utf16 * m_prgchText;
	int m_cchLength;
	//gr::utf16 m_rgchFontVar[64];

	gr::utf16 m_szFaceName[32];
	int m_pointSize;
	bool m_fBold;
	bool m_fItalic;
	bool m_fRtl;
	bool m_directionDepth;
};


#endif // !GRTXTSRC_INCLUDED
