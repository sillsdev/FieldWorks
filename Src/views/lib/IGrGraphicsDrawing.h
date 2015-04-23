/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
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
