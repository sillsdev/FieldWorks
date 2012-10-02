/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: main.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Main header file for the WorldPad EXE.
-------------------------------------------------------------------------------*//*:End Ignore*/
#if WIN32
#pragma once
#endif
#ifndef NOTEBK_H
#define NOTEBK_H 1

/***********************************************************************************************
	Constants
***********************************************************************************************/
#define kdzptInch 72 // ENHANCE JohnT: is there a better place for this?
// #define kdzmpInch 72000 // elsewhere? (currently in TextFmt.h)


/***********************************************************************************************
	Resource headers
***********************************************************************************************/
#if WIN32
#include "res/resource.h"
#include "FmtGenDlgRes.h"
#include "FmtParaDlgRes.h"
#include "FmtBdrDlgRes.h"
#include "FmtFntDlgRes.h"
#include "FmtBulNumDlgRes.h"
#include "AfStylesDlgRes.h"
#include "FilPgSetDlgRes.h"
#include "AfPrjNotFndDlgRes.h"
#endif

/***********************************************************************************************
	Generic header files.
***********************************************************************************************/
#if WIN32
#include "Common.h"
#endif
#include "WorldPadTlb.h"

#if WIN32
// Some redefines of the COM interfaces that we implemented only on Linux
#define ISimpleMainWnd WpMainWnd
#define ISimpleChildWnd WpChildWnd
#define ISimpleStylesheet WpStylesheet
#define ISimpleStylesheetPtr WpStylesheetPtr
#endif // WIN32

/***********************************************************************************************
	Additional AppCore headers.
***********************************************************************************************/
#define NO_DATABASE_SUPPORT
#if WIN32
#include "AfCore.h"
#include "AfFwTool.h"
#include "UtilView.h"
#endif

/***********************************************************************************************
	Additional view library headers. -- included in AfCore.h above
***********************************************************************************************/
//#include "UtilView.h"
//#include "VwBaseDataAccess.h"
//#include "VwCacheDa.h"
//#include "VwUndo.h"
//#include "VwBaseVc.h"

#if 0 // Currently in AfCore.h...maybe not forever?
/***********************************************************************************************
	Conceptual model headers.
***********************************************************************************************/
#include "FwCellarTlb.h" // ENHANCE JohnT: is this needed?
// This is normally done in an IDH, but we don't have one for the notebook yet, and we do
// need the flid and similar constants defined there.
typedef enum NotebookModuleDefns
{
	#define CMCG_SQL_ENUM 1
	#include "Notebk.sqh"
	#undef CMCG_SQL_ENUM
} NotebookModuleDefns;
#endif  // 0

/***********************************************************************************************
	Code headers
***********************************************************************************************/
#if !WIN32
#include "ISimpleChildWnd.h"
#endif
#include "WpWrSysDlg.h"
#include "WpOptionsDlg.h"
#include "WpDocDlg.h"
#include "WpStylesheet.h"
#include "WpDa.h"
#include "StVc.h"
#include "WorldPad.h"
#include "../Graphite/GrEngine/ITraceControl.h"
namespace gr {
typedef unsigned char utf8;
typedef unsigned short int utf16;
typedef unsigned long int utf32;
#define UtfType LgUtfForm
}
#include "../Graphite/lib/GrUtil.h"
#include "ITextSource.h"
#include "IGrJustifier.h"
#include "FwGr.h"

//#include <direct.h>
#include <io.h>
#include "xmlparse.h"
#include <msxml2.h>

#endif // !NOTEBK_H
