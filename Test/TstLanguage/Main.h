/*----------------------------------------------------------------------------------------------
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Main.h
Responsibility: Lars Huttar
Last reviewed: Not yet.
Description:
	Main header file for the TstLanguage DLL.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef TSTLANGUAGE_H
#define TSTLANGUAGE_H 1

#define NO_EXCEPTIONS 1
#include "Common.h"

#include <wchar.h>
/*************************************************************************************
	Interfaces. We use the lingserv objects to facilitate the tests.
*************************************************************************************/
#include "TestHarnessTlb.h"
#include "FwKernelTlb.h"
// #include "TextServTlb.h"
// #include "CellarServTlb.h"
#include "LanguageTlb.h"

/*************************************************************************************
	Implementations.
*************************************************************************************/

// #include "FWUtils.h"

/*************************************************************************************
	Test Harness defines.
*************************************************************************************/
#include "TestBase.h"
#define TESTPROGID "SIL.TstLanguage.Test"

class __declspec(uuid("F1D766F1-962E-11d3-BC1A-0000C0943099")) TestBase;
#define CLSID_Test __uuidof(TestBase)

#endif //!TSTLANGUAGE_H
