/*----------------------------------------------------------------------------------------------
Copyright 2002, SIL International. All rights reserved.

File: Main.h
Responsibility: Alistair Imrie
Last reviewed: Never

	Main header file for the DbServices component.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef __DBSERVICES_H
#define __DBSERVICES_H

#include "..\generic\Common.h" // Most of generic
#include "..\AppCore\AfLib\main.h"


/*************************************************************************************
	Interfaces.
*************************************************************************************/
#include "DbServicesTlb.h"

/*************************************************************************************
	Implementations.
*************************************************************************************/
#include "Resource.h"
#include "Remote.h"
#include "Disconnect.h"
#include "Backup.h"
#include "ZipInvoke.h"


/*************************************************************************************
	Resources.
*************************************************************************************/
#include "resource.h"

#endif // __DBSERVICES_H
