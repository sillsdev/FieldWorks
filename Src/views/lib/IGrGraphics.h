/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: IGrGraphics.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef IGRGRAPHICS_INCLUDED
#define IGRGRAPHICS_INCLUDED

#include "GrResult.h"

#ifndef GR_FW
#include "GrData.h"
#endif

#include "GrStructs.h"

namespace gr
{

/*----------------------------------------------------------------------------------------------
Class: IGrGraphics
----------------------------------------------------------------------------------------------*/
class IGrGraphics
{
public:
	// For drawing:
	virtual GrResult InvertRect(int xLeft, int yTop, int xRight, int yBottom) = 0;
	virtual GrResult DrawTextExt(int x, int y, int cgid, const OLECHAR __RPC_FAR * prggid,
		UINT uOptions, const RECT __RPC_FAR * pRect, int __RPC_FAR * prgdx) = 0;
	virtual GrResult get_YUnitsPerInch(int * pyInch) = 0;
	virtual GrResult SetupGraphics(LgCharRenderProps * pchrp) = 0;

	virtual GrResult GetDeviceContext(HDC * phdc) = 0;

	// Other:
	virtual GrResult GetFontEmSquare(int * pxyFontEmSquare) = 0;
	virtual GrResult GetGlyphMetrics(int gid,
		int * psBoundingWidth, int * pyBoundingHeight,
		int * pxBoundingX, int * pyBoundingY, int * pxAdvanceX, int * pyAdvanceY) = 0;
	virtual GrResult GetFontData(int nTableId, int * pcbTableSz, byte * prgb, int cbMax) = 0;
	virtual GrResult XYFromGlyphPoint(int gid, int nPoint, int * pxRet, int * pyRet) = 0;
	virtual GrResult get_FontAscent(int* pdy) = 0;
	virtual GrResult get_FontDescent(int* pdy) = 0;
	virtual GrResult get_FontCharProperties(LgCharRenderProps * pchrp) = 0;
};

} // namespace gr

#if !defined(GR_NAMESPACE)
using namespace gr;
#endif

#endif  // IGRGRAPHICS_INCLUDED
