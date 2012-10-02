/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
NU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwGr.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Wrapper classes that are used by the FieldWorks Graphite implementation.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FWGR_INCLUDED
#define FWGR_INCLUDED

/////class IGrEngine;

// We want to use std::min (for portability), so we have to undef min which is defined
// in WinDef.h
#undef min

/*----------------------------------------------------------------------------------------------
Class: FwGrTxtSrc
Description: Wrapper class to provide access to non-COM version of the text source,
	ITextSource.
----------------------------------------------------------------------------------------------*/
class FwGrTxtSrc : public gr::IColorTextSource
{
public:

	FwGrTxtSrc(IVwTextSource * pts, bool useNFC)
	{
		m_qts = pts;
		m_useNFC = useNFC;
	}

	~FwGrTxtSrc()
	{
	}

	// Wrapper methods.
	virtual long IncRefCount()
	{
		return m_qts.Ptr()->AddRef();
	}
	virtual long DecRefCount()
	{
		return m_qts.Ptr()->Release();
	}

	// ITextSource interface methods:

	virtual UtfType utfEncodingForm()
	{
		return kutf16;
	}
	virtual size_t getLength()
	{
		int cch;
		m_qts->get_Length(&cch);

		// for testing UTF-8:
		//gr::utf16 prgchwBuffer[1000];
		//m_qts->Fetch(0, cch, prgchwBuffer);
		//int cch8 = CountUtf8FromUtf16(prgchwBuffer, cch);
		//cch = cch8;
		///////////

		return (size_t)VwToGrOffset(cch);
	}
	virtual size_t fetch(toffset ichMin, size_t cch, gr::utf32 * prgchBuffer)
	{
		throw;
	}

	virtual size_t fetch(toffset ichMin, size_t cch, gr::utf16 * prgchwBuffer);
	virtual size_t fetch(toffset ichMin, size_t cch, gr::utf8 * prgchsBuffer)
	{
		// for testing UTF-8:
		//int cchLen;
		//HRESULT hr = (GrResult)m_qts->get_Length(&cchLen);
		//if (FAILED(hr))
		//	throw;
		//gr::utf16 prgchwBuffer[1000];
		//hr = (GrResult)m_qts->Fetch(0, cchLen, prgchwBuffer);

		//char prgchsTmp[4000];
		//int cchsLen = ConvertUtf16ToUtf8(prgchsTmp, 4000, prgchwBuffer, cchLen);

		//int cchsRet = min(cch, cchsLen - ichMin);
		//memcpy(prgchsBuffer, prgchsTmp + ichMin, cchsRet);
		//return cchsRet;

		throw;
	};
	virtual bool getRightToLeft(toffset ich);
	virtual unsigned int getDirectionDepth(toffset ich);
	virtual float getVerticalOffset(toffset ich);
	virtual isocode getLanguage(toffset ich);

	virtual std::pair<toffset, toffset> propertyRange(toffset ich);
	virtual size_t getFontFeatures(toffset ich, gr::FeatureSetting * prgfset);
	virtual bool sameSegment(toffset ich1, toffset ich2);

	// IColorTextSource interface:
	virtual void getColors(toffset ich, int * pclrFore, int * pclrBack);

	// Specific to FwGrTextSrc
	virtual float GetFontSize(int ich);
	gr::GrResult GetCharProps(int ich, LgCharRenderProps * pchrp,
		int * pichMin, int * pichLim);

	int VwToGrOffset(int vwOffset, bool& badOffset);
	int VwToGrOffset(int vwOffset);
	int GrToVwOffset(int grOffset);

protected:
	void ParseFeatureString(std::wstring stuFeat, int cMax,
		gr::FeatureSetting * prgfset, int * pcFeat);

protected:
	static const int kNFDBufferSize = 64;
	IVwTextSourcePtr m_qts;
	bool m_useNFC;
};

/*----------------------------------------------------------------------------------------------
Class: FwGrJustifier
Description: Wrapper class to provide access to non-COM version of the graphics object,
	IGrJustifier.
----------------------------------------------------------------------------------------------*/
class FwGrJustifier : public gr::IGrJustifier
{
public:
	FwGrJustifier(IVwJustifier * pjus)
	{
		m_qjus = pjus;
		m_cref = 0;
	}

	virtual gr::GrResult adjustGlyphWidths(gr::GraphiteProcess * pfgjwe,
		int iGlyphMin, int iGlyphLim,
		float dxCurrentWidth, float dxDesiredWidth);

	void IncRefCount()
	{
		m_cref++;
	}
	void DecRefCount()
	{
		m_cref--;
		if (m_cref <= 0)
			delete this;
	}

	//virtual GrResult suggestShrinkAndBreak(GraphiteProcess * pfgjwe,
	//	int iGlyphMin, int iGlyphLim, float dxsWidth, LgLineBreak lbPref, LgLineBreak lbMax,
	//	float * pdxShrink, LgLineBreak * plbToTry);

	// Return a Graphite-compatible justifier that can be stored in a Graphite segment.
	// In this case, create a permanent wrapper.
	// TODO: remove
	//virtual void JustifierObject(IGrJustifier ** ppgjus)
	//{
	//	FwGrJustifier * pfgjusNew = NewObj FwGrJustifier(m_qjus);
	//	*ppgjus = pfgjusNew;
	//}

	// When a segment is being destroyed, delete this object, which is the wrapper for the
	// justifier inside of it.
	// TODO: remove
	//virtual void DeleteJustifierPtr()
	//{
	//	m_qjus = NULL;
	//	delete this;
	//}

protected:
	long m_cref;
	IVwJustifierPtr m_qjus;
};


#endif // !FWGR_INCLUDED
