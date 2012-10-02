/*----------------------------------------------------------------------------------------------
Copyright 2001, SIL International. All rights reserved.

File: main.h
Responsibility: John Thomson
Last reviewed: never

Description:
	Main header file for the Research Notebook EXE.
----------------------------------------------------------------------------------------------*/
#pragma once
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
#include "resource.h"
#include "FmtGenDlgRes.h"
#include "FmtParaDlgRes.h"
#include "FmtBdrDlgRes.h"
#include "FmtFntDlgRes.h"
#include "FmtBulNumDlgRes.h"
#include "AfStylesDlgRes.h"
#include "FilPgSetDlgRes.h"
#include "MiscDlgsRes.h"
#include "TlsListsDlgRes.h"
#include "TlsStatsDlgRes.h"
#include "TlsOptDlgRes.h"
#include "PossChsrDlgRes.h"
#include "AfTagOverlayRes.h"
#include "RnImportRes.h"
#include "RnNewProjRes.h"
#include "FmtWrtSysRes.h"
#include "AfPrjNotFndDlgRes.h"
#include "RecMainWndRes.h"

/***********************************************************************************************
	Generic header files.
***********************************************************************************************/
#include "Common.h"

#include "NoteBkTlb.h"

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

/***********************************************************************************************
	Code headers
***********************************************************************************************/
class RnDocSplitChild;
typedef GenSmartPtr<RnDocSplitChild> RnDocSplitChildPtr;
class RnCustDocVc;
typedef GenSmartPtr<RnCustDocVc> RnCustDocVcPtr;
class RnCustBrowseVc;
typedef GenSmartPtr<RnCustBrowseVc> RnCustBrowseVcPtr;

#include "RnTlsOptDlg.h"
#include "RnDeSplitChild.h"
#include "NoteBk.h"
#include "RnCustDocVc.h"
#include "RnCustBrowseVc.h"
#include "RnImportDlg.h"
#include "RnCustomExport.h"
#include "RnDeFeRoleParts.h"
#include "RnDocSplitChild.h"
#include "RnBrowseSplitChild.h"

#endif // !NOTEBK_H
