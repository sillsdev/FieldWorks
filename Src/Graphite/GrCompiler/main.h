/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Main.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Header files to include in the Graphite compiler.
-------------------------------------------------------------------------------*//*:End Ignore*/

/*************************************************************************************
	Main.header file for the Language component.
*************************************************************************************/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GRCOMPILER_H
#define GRCOMPILER_H 1

// It's okay to use functions that were declared deprecated by VS 2005:
#define _CRT_SECURE_NO_DEPRECATE
#pragma warning(disable: 4996) // warning: function was declared deprecated

#define NO_EXCEPTIONS 1

#ifdef GR_FW
#include "Common.h"
#include <algorithm>
using std::max;
using std::min;
#include "GrPlatform.h"
#else
#include "GrCommon.h"
#include "GrPlatform.h"
#endif

using namespace gr;
/*************************************************************************************
	Interfaces.
*************************************************************************************/
#ifdef GR_FW
#include "FwKernelTlb.h"	// includes DbAccess and Language DLL interfaces
// To handle reading from the Unicode char props database, and breakweight enumeration
//#include "DbAccessTlb.h"
//#include "LanguageTlb.h"

#else
#include "LgCharPropsStub.h"

#endif


/*************************************************************************************
	Implementations.
*************************************************************************************/
#include <fstream>
#include <iostream>
#ifdef _WIN32
#include <crtdbg.h>
#endif

#ifdef GR_FW
#include "Vector.h"
#include "UtilString.h"
#include "HashMap.h"
#include "Set.h"
#else
#include "UtilVector.h"
#include "UtilString.h"
#include "UtilHashMap.h"
#include "UtilSet.h"
#include "UtilInt.h"
#endif

class GrcErrorList;
class GrcManager;
class GdlFeatureDefn;
class GdlLanguageDefn;
class GdlGlyphClassDefn;
class GdlRuleItem;
class FsmTable;
class FsmState;
class FsmMachineClass;

extern GrcErrorList g_errorList;
extern GrcManager g_cman;

///#include "Grp.h"
#include "GrpLineAndFile.hpp"
#include "Antlr/AST.hpp"

#include "constants.h"
#include "GdlObject.h"
#include "GrcErrorList.h"
#include "GrcBinaryStream.h"
#include "StrAnsiHash.h"
#include "GrcSymTable.h"
#include "TtfUtil.h" //must occur before GrcFont.h
#include "Tt.h"
#include "GrcFont.h"
#include "GdlExpression.h"
#include "GdlFeatures.h"
#include "GdlNameDefn.h"
#include "GrcGlyphAttrMatrix.h"
#include "GrcMasterTable.h"
#include "GdlRule.h"
#include "Fsm.h"
#include "GdlTablePass.h"
#include "GdlGlyphClassDefn.h"
#include "GdlGlyphDefn.h"
#include "GdlRenderer.h"
#include "GrcEnv.h"
#include "GrcManager.h"

/*************************************************************************************
	Functions.
*************************************************************************************/
void HandleCompilerOptions(char *);
void BinarySearchConstants(int n, int * pnPowerOf2, int * pnLog);
void GenerateOutputFontFileName(char * pchFontFile, char * ppchOutputFont);
void GenerateOutputControlFileName(char * pchFontFile, char * pchOutputFont);
void GenerateOutputControlFontFamily(utf16 * pchFontFile, utf16 * pchOutputFont);
bool LooksLikeFontFamily(char * pchFile);
StrAnsi VersionString(int fxdVersion);

/*************************************************************************************
	Test Harness defines.
*************************************************************************************/
#if 0
#define TESTPROGID "SIL.WRCompiler.Test"					//occurs in test\progids.txt
	class __declspec(uuid("2192B661-143D-11d3-9273-00400543A57C")) TestBase;
#define CLSID_Test __uuidof(TestBase)
#endif


#endif //!WRCOMPILER_H
