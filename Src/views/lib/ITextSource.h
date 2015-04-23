/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: ITextSource.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	An interface for a text source that is used by the Graphite engine.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef IGRTXTSRC_INCLUDED
#define IGRTXTSRC_INCLUDED


#include "GrAppData.h"
#include "GrStructs.h"
#include "GrResult.h"

namespace gr
{

/*----------------------------------------------------------------------------------------------
	Class: ITextSource
	This class provides an interface for a text source for the Graphite engine.
----------------------------------------------------------------------------------------------*/
class ITextSource
{
public:
	virtual ~ITextSource() {};
	virtual UtfType utfEncodingForm() = 0;
	virtual size_t getLength() = 0;
	virtual size_t fetch(toffset ichMin, size_t cch, utf32 * prgchBuffer) = 0;
	virtual size_t fetch(toffset ichMin, size_t cch, utf16 * prgchwBuffer) = 0;
	virtual size_t fetch(toffset ichMin, size_t cch, utf8  * prgchsBuffer) = 0;
	virtual bool getRightToLeft(toffset ich) = 0;
	virtual unsigned int getDirectionDepth(toffset ich) = 0;
	virtual float getVerticalOffset(toffset ich) = 0;
	virtual isocode getLanguage(toffset ich) = 0;

	virtual std::pair<toffset, toffset> propertyRange(toffset ich) = 0;
	virtual size_t getFontFeatures(toffset ich, FeatureSetting * prgfset) = 0;
	virtual bool sameSegment(toffset ich1, toffset ich2) = 0;
};


/*----------------------------------------------------------------------------------------------
	Class: IColorTextSource
	Defines a method to get the color informtion from the text source.
	It is needed for SegmentPainters that want to handle color.
----------------------------------------------------------------------------------------------*/
class IColorTextSource : public ITextSource
{
public:
	virtual void getColors(toffset ich, int * pclrFore, int * pclrBack) = 0;
};

} // namespace gr

#if defined(GR_NO_NAMESPACE)
using namespace gr;
#endif

#endif // !IGRTXTSRC_INCLUDED
