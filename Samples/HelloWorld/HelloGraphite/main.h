/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: main.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Main header file for the HelloGraphite.exe, a simple window that shows some text possibly
	rendered with Graphite.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef HGMAIN_H
#define HGMAIN_H 1

/***********************************************************************************************
	Resource headers
***********************************************************************************************/
#include "resource.h"


/***********************************************************************************************
	Generic header files.
***********************************************************************************************/
#include "Common.h"

/***********************************************************************************************
	Interface headers.
***********************************************************************************************/
#include "FwKernelTlb.h"
#include "LanguageTlb.h"
#include "ViewsTlb.h"

/***********************************************************************************************
	Additional AppCore headers.
***********************************************************************************************/
#include "AfCore.h"

#include "HelloGraphite.h"

#include "..\..\..\Src\Graphite\GrEngine\ITraceControl.h"
#include "..\..\..\Src\Graphite\lib\GrUtil.h"

class __declspec(uuid("F0C462A1-3258-11d4-9273-00400543A57C")) GrEngine;
#define CLSID_GrEngine __uuidof(GrEngine)

#endif // !HGMAIN_H
