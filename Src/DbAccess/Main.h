/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: Main.h
Responsibility:
Last reviewed:

	Main header file for the DbAccess component.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef DBACCESS_H
#define DBACCESS_H 1

#define NO_EXCEPTIONS 1
#include "Common.h" // Most of generic
#include "wn95scm.h"

#define CMCG_SQL_DEFNS
	#include "CmTypes.h"	// e.g., kcptGuid
#undef CMCG_SQL_DEFNS

/*************************************************************************************
	Interfaces.
*************************************************************************************/
//#include "DbAccessTlb.h"
#include "FwKernelTlb.h"	// includes interfaces for DbAccess

/*************************************************************************************
	Implementations.
*************************************************************************************/
#include "OleDbEncap.h"
#include "DbAdmin.h"

/*************************************************************************************
	Resources.
*************************************************************************************/
#include "resource.h"

// for parsing XML files
#include "xmlparse.h"

#endif //!DBACCESS_H
