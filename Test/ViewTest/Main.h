/*----------------------------------------------------------------------------------------------
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Main.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Main header file for the CellarTst DLL.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef VIEWTEST_H
#define VIEWTEST_H 1

#define NO_EXCEPTIONS 1
#include "Common.h"

#define HVO long
#define PropTag int
#define kdzptInch 72 // Review JohnT: is there a better place for this?
#define kdzmpInch 72000 // elsewhere?

/*************************************************************************************
	Interfaces.
*************************************************************************************/
#include "..\\..\\Output\\test\\TestHarnessTlb.h"
#include "FwKernelTlb.h"
#include "LanguageTlb.h"
#include "TestBase.h"
#include "ViewsTlb.h"

// conceptual model kflid constants.
typedef enum NotebookModuleDefns
{
	#define CMCG_SQL_ENUM 1
	#include "StText.sqh"
	//#include "Notebk.sqh"
	#undef CMCG_SQL_ENUM
} NotebookModuleDefns;


/*************************************************************************************
	Implementations.
*************************************************************************************/
#pragma warning(disable: 4663) // automatically excluding while importing.
#pragma warning(disable: 4018) // automatically excluding while importing.
#pragma warning(disable: 4245) // automatically excluding while importing.
#pragma warning(disable: 4127) // automatically excluding while importing.
#pragma warning(disable: 4244) // automatically excluding while importing.
#pragma warning(disable: 4146) // automatically excluding while importing.
#include <fstream>

#define TESTHARNESS
#define BASELINE
#include "..\\..\\Output\\Test\\TestHarnessTlb.h"
#include "..\\SilTestSite.h"

#include "VwTestRootSite.h"
#include "VwGraphics.h"
#include "VwBaseDataAccess.h"
#include "VwBaseVc.h"
#include "TestStVc.h"
#include "VwCacheDa.h"
// The following headers are for macro reading
#include <sstream>
#include "MacroBase.h"
#include "TestVwRoot.h"
#include "AfDef.h"
#include "AfColorTable.h"
#include "AfGfx.h"

#include "ViewTest.h"


/*************************************************************************************
	Test Harness defines.
*************************************************************************************/
#define TESTPROGID "SIL.Views.Test" // Occurs in test\progids.txt

class __declspec(uuid("143BB781-407B-11d4-9273-00400543A57F")) TestBase;
#define CLSID_Test __uuidof(TestBase)

#endif //!VIEWTEST_H
