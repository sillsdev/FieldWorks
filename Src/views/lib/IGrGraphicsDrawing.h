/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.
Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.
File: IGrGraphicsDrawing.h
Responsibility: Sharon Correll
Last reviewed: Not yet.
Description:
	Declaration of graphics methods used in drawing operations.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef IGRGRAPHICSDRAWING_INCLUDED
#define IGRGRAPHICSDRAWING_INCLUDED

#include "IGrGraphics.h"

namespace gr
{


/*----------------------------------------------------------------------------------------------
Class: IGrGraphicsDrawing
----------------------------------------------------------------------------------------------*/
class IGrGraphicsDrawing : public virtual IGrGraphics
{
public:
	virtual GrResult InvertRect(int twLeft, int twTop, int twRight, int twBottom) = 0;
	virtual GrResult DrawTextExt(int x, int y, int cgid, const OLECHAR __RPC_FAR * prggid,
		UINT uOptions, const RECT __RPC_FAR * pRect, int __RPC_FAR * prgdx) = 0;
	virtual GrResult get_YUnitsPerInch(int * pyInch) = 0;
	virtual GrResult SetupGraphics(LgCharRenderProps * pchrp) = 0;

	virtual GrResult GetDeviceContext(HDC * phdc) = 0;
};

}

#if !defined(GR_NAMESPACE)
using namespace gr;
#endif

#endif  // IGRGRAPHICSDRAWING_INCLUDED
