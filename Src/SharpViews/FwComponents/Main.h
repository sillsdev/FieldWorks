/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Main.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Main header file for the views component.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef FWCOMPONENTS_H
#define FWCOMPONENTS_H 1

#define NO_EXCEPTIONS 1
#include "common.h"
#if WIN32
#include <shlobj.h> // for one call to SHGetSpecialFolderPath
#endif

// We want to use std::min and std::max(for portability), so we have to undef min/max which
// are defined in WinDef.h
#undef min
#undef max
using namespace std;

// We want to use std::min and std::max(for portability), so we have to undef min/max which
// are defined in WinDef.h
#undef min
#undef max
using namespace std;

/* ---------------------
If you want to show colored boxes around the boxes uncomment the following define:
----------------------*/
//#define _DEBUG_SHOW_BOX
/*----------------------*/
#define kdzptInch 72
#define kdzmpInch 72000

#define kchwHardLineBreak (wchar)0x2028

#if WIN32
#include "..\..\..\Src\AppCore\Res\AfAppRes.h"
#else
#include <Res/AfAppRes.h> // from AppCore
#endif

// This is needed now by FwGr.h which uses std::wstring
#include <string>

// This is needed for the min and max methods
#include <algorithm>

using namespace fwutil;

//:>**********************************************************************************
//:>	Forward declarations
//:>**********************************************************************************

//:>**********************************************************************************
//:>	Classes we have to include before we can do typedefs
//:>**********************************************************************************
#include "HashMap.h"
#include "Vector.h"

//:>**********************************************************************************
//:>	Other classes we have to include.
//:>**********************************************************************************

//:>**********************************************************************************
//:>	Interfaces.
//:>**********************************************************************************
#include "FwKernelTlb.h"

#include "FwComponentsTlb.h"

//:>**********************************************************************************
//:>	Implementations.
//:>**********************************************************************************
// for interfacing with Graphite:
namespace gr {
typedef unsigned char utf8;
#ifdef WIN32
typedef wchar_t utf16;
#else
typedef wchar utf16;
#endif
typedef unsigned long int utf32;
#define UtfType LgUtfForm
class GrEngine;
} // gr

#include "GrResult.h"

#include "VwGraphics.h"

#if WIN32
#include "AfDef.h"
#include "AfColorTable.h"
#include "AfGfx.h"
#endif

#endif //!FWCOMPONENTS_H
