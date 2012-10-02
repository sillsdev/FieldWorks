/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: Main.h
Responsibility: Steve McConnel (was Shon Katzenberger)
Last reviewed:

	Main header file for the Cellar DLL.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef Main_H
#define Main_H 1

#include "Common.h"

/***********************************************************************************************
	Interfaces.
***********************************************************************************************/
#include "FwKernelTlb.h"	// for TsString, also provides LgWritingSystemFactory[Builder]
#include "FwCellarTlb.h"

#include "FwCellarRes.h"

/***********************************************************************************************
	Implementations.
***********************************************************************************************/
#include "FwXmlData.h"
#include "WriteXml.h"
#include "FwStyledText.h"

#endif // !Main_H
