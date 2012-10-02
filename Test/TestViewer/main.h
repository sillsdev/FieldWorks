/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: main.h
Responsibility: John Thomson
Last reviewed: never

Description:
	Main header file for the TestViewer EXE.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef NOTEBK_H
#define NOTEBK_H 1

/***********************************************************************************************
	Constants
***********************************************************************************************/
#define kdzptInch 72 // Review JohnT: is there a better place for this?
// #define kdzmpInch 72000 // elsewhere? (currently in TextFmt.h)


/***********************************************************************************************
	Resource headers
***********************************************************************************************/
#include "resource.h"
#include "FmtGenDlgRes.h"
#include "FmtParaDlgRes.h"
#include "FmtBdrDlgRes.h"
#include "FmtFntDlgRes.h"
#include "FmtBulNumDlgRes.h"
#include "FmtStylesDlgRes.h"
#include "FilPgSetDlgRes.h"

/***********************************************************************************************
	Generic header files.
***********************************************************************************************/
#include "Common.h"

// Should probably be added to Common.h. RN includes as part of PossChsrDlg.h, but that is
// not right because it is used elsewhere.
#include <HtmlHelp.h>

/***********************************************************************************************
	Interface headers.
***********************************************************************************************/
#include "FwKernelTlb.h"
#include "LanguageTlb.h"
#include "ViewsTlb.h"
// Special view defines (in Views IDL, but somehow don't make it into generated header)
#define HVO long
#define PropTag int

/***********************************************************************************************
// RN gets these from the obsolete TextEdit.h. We should move them to somewhere shareable,
// probably TextServe.idh.
***********************************************************************************************/
const kdzmpInch = 72000;

/*************************************************************************************
	Warnings to turn off.
*************************************************************************************/
#pragma warning(disable: 4065) // Switch statement contains default but no case.
#pragma warning(disable: 4355) // 'this' used in base member initializer list.
#pragma warning(disable: 4786) // identifier truncated in debug info.
#pragma warning(disable: 4290) // exception specification ignored.

#pragma warning(disable: 4192) // automatically excluding while importing.
#pragma warning(disable: 4663) // automatically excluding while importing.
#pragma warning(disable: 4018) // automatically excluding while importing.
#pragma warning(disable: 4245) // automatically excluding while importing.
#pragma warning(disable: 4127) // automatically excluding while importing.
#pragma warning(disable: 4146) // automatically excluding while importing.
#pragma warning(disable: 4124) // automatically excluding while importing.
#pragma warning(disable: 4244) // automatically excluding while importing.

/***********************************************************************************************
	Additional AppCore headers.
***********************************************************************************************/
#define BASELINE
#define MAX_LENGTH	256
using namespace std;
#include <fstream>
#include <sstream>

#include <sql.h>
#include <sqltypes.h>
#include "sqldb.h"
#include "utilview.h"

#include "..\\..\\Output\\Test\\TestHarnessTlb.h"
#include "VwGraphics.h"

#define NO_DATABASE_SUPPORT
#include "AfCore.h"
#include "AfVwWnd.h" // Review JohnT: should this be in AfCore.h?
/***********************************************************************************************
	Additional view library headers.
***********************************************************************************************/
#include "VwBaseDataAccess.h"
#include "VwCacheDa.h"
#include "VwBaseVc.h"

// TestVwRoot Include files
#include "SilTestSite.h"
#include "MacroBase.h"
#include "TestVwRoot.h"


/***********************************************************************************************
	Conceptual model headers.
***********************************************************************************************/
#include "FwCellarTlb.h" // Review JohnT: is this needed?

/***********************************************************************************************
	Code headers
***********************************************************************************************/
#include "UiColor.h"
#include "RnDialog.h"
#include "FmtBdrDlg.h"
#include "FmtFntDlg.h"
#include "FmtParaDlg.h"
#include "FmtBulNumDlg.h"
#include "FmtGenDlg.h"
#include "FilPgSetDlg.h"
#include "WpDa.h"
#include "StVc.h"
#include "TestScriptDlgRes.h"		// Dialog box test action
#include "TestScriptDlg.h"
#include "TestViewer.h"
// #include "..\Graphite\GrEngine\ITraceControl.h"
// #include "..\Graphite\lib\GrUtil.h"

// class __declspec(uuid("F0C462A1-3258-11d4-9273-00400543A57C")) GrEngine;
// #define CLSID_GrEngine __uuidof(GrEngine)

#endif // !NOTEBK_H
