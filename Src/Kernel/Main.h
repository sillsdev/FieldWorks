/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Main.h
Responsibility: Jeff Gayle
Last reviewed:

	Main header file for the text services dll.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef Main_H
#define Main_H 1

#include "Common.h"

//#define kwsLim 0xfffffff9
#define CMCG_SQL_DEFNS 1
#include "..\Cellar\lib\CmTypes.h"
#undef CMCG_SQL_DEFNS


/***********************************************************************************************
	Interfaces.
***********************************************************************************************/
#include "FwKernelTlb.h"

/***********************************************************************************************
	Implementations.
***********************************************************************************************/
#include "TsString.h"
#include "TsTextProps.h"
#include "TsStrFactory.h"
#include "TsPropsFactory.h"
#include "TextServ.h"
#include "TsMultiStr.h"
#include "ActionHandler.h"
#include "FwStyledText.h"
#include "WriteXml.h"		// From AppCore.
#include "IcuCleanupManager.h"

// for parsing XML files
#include "..\cellar\FwXml.h"

#endif // !Main_H
