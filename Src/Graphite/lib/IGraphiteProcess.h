/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GraphiteProcess.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	The interface that is needed for the Justifier object to call back to the Graphite engine.
----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef IGR_JENGINE_INCLUDED
#define IGR_JENGINE_INCLUDED

#include "GrResult.h"
//:End Ignore
namespace gr
{

/*----------------------------------------------------------------------------------------------

	Hungarian: fgje
----------------------------------------------------------------------------------------------*/
class GraphiteProcess
{
public:
	virtual GrResult GetGlyphAttribute(int iGlyph, int jgat, int nLevel, float * pValueRet) = 0;
	virtual GrResult GetGlyphAttribute(int iGlyph, int jgat, int nLevel, int * pValueRet) = 0;
	virtual GrResult SetGlyphAttribute(int iGlyph, int jgat, int nLevel, float value) = 0;
	virtual GrResult SetGlyphAttribute(int iGlyph, int jgat, int nLevel, int value) = 0;
};

} // namespace gr

#if !defined(GR_NAMESPACE)
using namespace gr;
#endif

#endif  // !IGR_JENGINE_INCLUDED
