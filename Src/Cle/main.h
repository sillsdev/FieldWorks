/*----------------------------------------------------------------------------------------------
Copyright 2000, 2004, SIL International. All rights reserved.

File: main.h (for Choices List Editor)
Responsibility: John Thomson
Last reviewed: never

Description:
	Main header file for the List Editor EXE.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef CLE_INC_H
#define CLE_INC_H 1

/***********************************************************************************************
	Constants
***********************************************************************************************/
#define kdzptInch 72 // ENHANCE JohnT: is there a better place for this?
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
#include "AfStylesDlgRes.h"
#include "FilPgSetDlgRes.h"
#include "PossChsrDlgRes.h"
#include "CleNewProjRes.h"
#include "CleLstNotFndDlgRes.h"
#include "AfPrjNotFndDlgRes.h"

/***********************************************************************************************
	Generic header files.
***********************************************************************************************/
#include "Common.h"

#if 0 // Now in AfCore.h
/***********************************************************************************************
	Interface headers.
***********************************************************************************************/
#include "FwKernelTlb.h"
#include "DbAccessTlb.h"
#include "LanguageTlb.h"
#include "ViewsTlb.h"

// Special view defines (in Views IDL, but somehow don't make it into generated header)
#define HVO long
#define PropTag int

/***********************************************************************************************
	Additional generic headers.
***********************************************************************************************/
#include "GenData.h"

#endif // stuff now in AfCore

#include "CleTlb.h"

/***********************************************************************************************
	Additional AppCore headers.
***********************************************************************************************/
#include "AfCore.h"
#include "AfFwTool.h"

/***********************************************************************************************
	Additional data entry headers.
***********************************************************************************************/
#include "AfDeCore.h"

/***********************************************************************************************
	Additional view library headers.
***********************************************************************************************/
#include "VwOleDbDa.h"
#include "VwCustomVc.h"

/***********************************************************************************************
	Additional common COM dialog headers.
***********************************************************************************************/
#include "..\CommonCOMDlgs\OpenFWProjectDlg.h"

typedef enum ListEditorModuleDefns
{
	#define CMCG_SQL_ENUM 1
	#include "Ling.sqh" // Need kflidPartOfSpeech, and eventually others.
	#undef CMCG_SQL_ENUM
} ListEditorModuleDefns;

/***********************************************************************************************
	Code headers
***********************************************************************************************/
class CleMainWnd;
class CleCustDocVc;
typedef GenSmartPtr<CleCustDocVc> CleCustDocVcPtr;

#include "FilPgSetDlg.h"
#include "CleTlsOptDlg.h"
#include "CleLstNotFndDlg.h"
#include "CleDeSplitChild.h"
#include "Cle.h"
#include "CleCustDocVc.h"
#include "CleCustomExport.h"

#endif // !CLE_INC_H
