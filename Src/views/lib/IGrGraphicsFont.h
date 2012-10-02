/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.
Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.
File: IGrGraphicsFont.h
Responsibility: Sharon Correll
Last reviewed: Not yet.
Description:
	Declaration of graphics methods used for set-up and getting metric information.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef IGRGRAPHICSFONT_INCLUDED
#define IGRGRAPHICSFONT_INCLUDED

#include "IGrGraphics.h"

namespace gr
{


/*----------------------------------------------------------------------------------------------
Class: IGrGraphicsFont
----------------------------------------------------------------------------------------------*/
class IGrGraphicsFont : public virtual IGrGraphics
{
public:
	virtual GrResult GetFontEmSquare(int * pxyFontEmSquare) = 0;
	virtual GrResult GetGlyphMetrics(int gid,
		int * pxBoundingWidth, int * pyBoundingHeight,
		int * pxBoundingX, int * pyBoundingY, int * pxAdvanceX, int * pyAdvanceY) = 0;
	virtual GrResult GetFontData(int nTableId, int * pcbTableSz, byte * prgb, int cbMax) = 0;
	virtual GrResult XYFromGlyphPoint(int gid, int nPoint, int * pxRet, int * pyRet) = 0;
	virtual GrResult get_FontAscent(int* pdy) = 0;
	virtual GrResult get_FontDescent(int* pdy) = 0;

	// should this one be moved to IGrGraphicsDrawing?
	virtual GrResult get_FontCharProperties(LgCharRenderProps * pchrp) = 0;
};

}

#if !defined(GR_NAMESPACE)
using namespace gr;
#endif

#endif  // IGRGRAPHICSFONT_INCLUDED
