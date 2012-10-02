/*----------------------------------------------------------------------------------------------
Copyright 2002, SIL International. All rights reserved.

File: Main.h
Responsibility: Alistair Imrie
Last reviewed: Never

	Main header file for the MigrateData component.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef MIGRATEDATA_H
#define MIGRATEDATA_H 1

#define NO_EXCEPTIONS 1
#define NO_AFMAINWINDOW 1	// Controls compilation of DbStringCrawler.cpp
#include "..\generic\Common.h"	// Most of generic
#include "..\AppCore\AfLib\main.h"
#include "oledb.h"

#define CMCG_SQL_DEFNS
#include "..\cellar\lib\CmTypes.h"
#undef CMCG_SQL_DEFNS
#define CMCG_SQL_ENUM 1
typedef enum DataMigrationDefns
{
	#include "Ling.sqh"
};
#undef CMCG_SQL_ENUM


/*************************************************************************************
	Interfaces.
*************************************************************************************/
#include "FwKernelTlb.h"	// includes DbAccess, must come before MigrateDataTlb.h
#include "MigrateDataTlb.h"

/*************************************************************************************
	Implementations.
*************************************************************************************/
#include "MigrateData.h"

/*************************************************************************************
	Resources.
*************************************************************************************/
#include "resource.h"

#endif //!MIGRATEDATA_H
