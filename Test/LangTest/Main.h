/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: Main.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Main header file for the CellarTst DLL.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef LANGTEST_H
#define LANGTEST_H 1

#define NO_EXCEPTIONS 1
#include "Common.h"

/*************************************************************************************
	Interfaces. We use the lingserv objects to facilitate the tests.
*************************************************************************************/
#include "TestHarnessTlb.h"
#include "FwKernelTlb.h"
#include "DbAccessTlb.h"
#include "LanguageTlb.h"
#include "TestBase.h"
#include "LangTest.h"
#include "WriteXml.h"


/*************************************************************************************
	Implementations.
*************************************************************************************/


/*************************************************************************************
	Test Harness defines.
*************************************************************************************/
#define TESTPROGID "SIL.Language.Test" // Occurs in test\progids.txt

class __declspec(uuid("FC1C0D0F-0483-11d3-8078-0000C0FB81B5")) TestBase;
#define CLSID_Test __uuidof(TestBase)

#endif //!LANGTEST_H
