/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Main.h
Responsibility: Randy Regnier
Last reviewed: Not yet.

Description:
	Main header file for the views component.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef COMDLGS_H
#define COMDLGS_H 1

#define NO_EXCEPTIONS 1
#define NO_AFMAINWINDOW 1	// Controls compilation of DbStringCrawler.cpp
#include "common.h"

//:>**********************************************************************************
//:>	Resource headers.
//:>**********************************************************************************
#include "OpenProjDlgRes.h"
#include "TeStylesDlgRes.h"
#include "TeFmtGenDlgRes.h"
#include "ImagesSmallIdx.h"
#include "LangProjPropDlgRes.h"
#include "RnAnthroListRes.h"

//:>**********************************************************************************
//:>	Forward declarations
//:>**********************************************************************************
class OpenProjDlg;
class CleOpenProjDlg;
class OpenFWProjectDlg;

//:>**********************************************************************************
//:>	Classes we have to include before we can do typedefs
//:>**********************************************************************************
#include "HashMap.h"
#include "Vector.h"

//:>**********************************************************************************
//:>	Other classes we have to include.
//:>**********************************************************************************

//:>**********************************************************************************
//:>	Additional AppCore headers.
//:>**********************************************************************************
// Note that ..\AppCore\AfLib\main.h #includes "AfCore.h", which #includes "ViewsTlb.h", which
// effectively #includes "CmnFwDlgsTlb.h" via ..\views\ViewsTlb.idl
#include "..\AppCore\AfLib\main.h"

//:>**********************************************************************************
//:>	Interfaces.
//:>**********************************************************************************

/***********************************************************************************************
	Additional common COM dialog headers.
***********************************************************************************************/

//:>**********************************************************************************
//:>	Types and constants used in COMDLGS subsystem
//:>**********************************************************************************

//typedef ComVector<IVwPropertyStore> VwPropsVec; // Hungarian vqvps

//:>**********************************************************************************
//:>	Implementations.
//:>**********************************************************************************
#include "OpenProjDlg.h"
#include "OpenFWProjectDlg.h"
#include "FwStylesDlg.h"
#include "TeStylesDlg.h"
#include "FwExportDlg.h"
#include "AfExportDlg.h"
#include "AfExportRes.h"
#include "FwDbMergeWrtSys.h"
#include "FwDbMergeStyles.h"
#include "RnAnthroListDlg.h"
#include "FwCheckAnthroList.h"

#endif //!COMDLGS_H
