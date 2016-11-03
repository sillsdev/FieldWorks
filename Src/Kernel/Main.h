/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Main.h
Responsibility: Jeff Gayle
Last reviewed:

	Main header file for the text services dll.
-------------------------------------------------------------------------------*//*:End Ignore*/
#if _MSC_VER
#pragma once
#endif
#ifndef Main_H
#define Main_H 1

#include "common.h"

//#define kwsLim 0xfffffff9
#include "CellarConstants.h"

#define NO_EXCEPTIONS 1
#if WIN32
#include <algorithm>
#include <mlang.h>
#endif

#if !WIN32
#include <OleStringLiteral.h>
#include "BasicTypes.h"
#include <memory>
#endif

#include <algorithm>
#include <vector>

// We want to use std::min and std::max(for portability), so we have to undef min/max which
// are defined in WinDef.h
#undef min
#undef max
using namespace std;

/***********************************************************************************************
	Interfaces.
***********************************************************************************************/
#include "FwKernelTlb.h"

/***********************************************************************************************
	Implementations.
***********************************************************************************************/
#include "LangResource.h"

using namespace fwutil;	// Rect and Point classes

#include "KernelGlobals.h"
#include "TsString.h"
#include "TsTextProps.h"
#include "TsStrFactory.h"
#include "TsPropsFactory.h"
#include "TextServ.h"
#include "ActionHandler.h"
// Engines
#include "LgLineBreaker.h"
class RomRenderEngine;
DEFINE_COM_PTR(RomRenderEngine);
class UniscribeEngine;
DEFINE_COM_PTR(UniscribeEngine);
class GraphiteEngine;
DEFINE_COM_PTR(GraphiteEngine);
#include "RomRenderSegment.h"
#include "RomRenderEngine.h"
#if !WIN32
#include "UniscribeLinux.h"
#endif
#include "UniscribeSegment.h"
#include "UniscribeEngine.h"
#include "GraphiteSegment.h"
#include "GraphiteEngine.h"

// Other tools
#include "FwStyledText.h"
#include "StringToNumHelpers.h"
#include "xmlparse.h"

#if WIN32
// for parsing XML files; in this DLL, we want the parser to work with wide characters,
// since we always parse BSTRs.
#define XML_UNICODE_WCHAR_T
#else
// XML_UNICODE_WCHAR_T causes XML_Char to be wchar_t
#ifdef XML_UNICODE
	#error "Don't define XML_UNICODE as this causes XML_CHAR to be UTF-16 which expat on Linux can't handle"
#endif
#endif
#include "StringToNumHelpers.h"
#include "../Cellar/FwXml.h"

#endif // !Main_H
