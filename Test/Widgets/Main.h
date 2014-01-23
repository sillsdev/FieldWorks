/*----------------------------------------------------------------------------------------------
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Main.h
Responsibility: Ken Zook
Last reviewed:
	Main header file for the SimpleTest EXE
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef SDKWIDGETS_H
#define SDKWIDGETS_H 1

#include "Common.h"

#if 0 //stuff now in AfCore.h
/*************************************************************************************
	Interfaces. We use the FieldWorks objects to facilitate the tests.
*************************************************************************************/
#include "FwKernelTlb.h"
#include "LanguageTlb.h"
#include "ViewsTlb.h"
#include "DbAccessTlb.h"
// Special view defines (in Views IDL, but somehow don't make it into generated header)
#define HVO long
#define PropTag int


/***********************************************************************************************
	This is a bunch of messy stuff we need because AfCore.h now includes CustViewDa.h,
	which has not yet been sufficiently decoupled from a particular conceptual model.
***********************************************************************************************/
#include "GenData.h" // Needed, perhaps temporarily, because AfCore currently includes
					 // VwRsOdbc.h.
// This is normally done in an IDH, but we don't have one for the notebook yet, and we do
// need the flid and similar constants defined there. -- until we decouple this info
// from CustViewDa.h
typedef enum NotebookModuleDefns
{
	#define CMCG_SQL_ENUM 1
	#include "StText.sqh"
	#include "Notebk.sqh"
	#include "LangProj.sqh"
	#undef CMCG_SQL_ENUM
} NotebookModuleDefns;
#include "FwCellarTlb.h"


// TODO DarrellZ: Get rid of this. It was just used to fix a compile error. AfVwWnd.h refers
// to this constant, which should be fixed to point to an AF constant, not an RN constant.
#define kridRnTBarFmtg 1
#endif

/***********************************************************************************************
	Additional generic headers.
***********************************************************************************************/
#include "AfCore.h"

#if 0 // stuff now in AfCore, or obsolete altogether.
#include "AfVwWnd.h" // Review JohnT: should this be in AfCore.h?
#include "UiColor.h"
#include "TextProps.h"
#include "TextEdit.h"
#include "TextFmt.h"
#include "GenData.h"
#endif


/*************************************************************************************
	Implementations.
*************************************************************************************/
#include "resource.h"
#include <stdio.h>

#endif //!SDKWIDGETS_H
