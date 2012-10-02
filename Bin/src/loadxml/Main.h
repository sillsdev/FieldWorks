/*----------------------------------------------------------------------------------------------
Copyright 2001 by SIL International. All rights reserved.

File: Main.h
Responsibility: Steve McConnel
Last reviewed:

----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef MAIN_H
#define MAIN_H

#define UNICODE

#undef NO_EXCEPTIONS
#include "common.h"

/***********************************************************************************************
	Interfaces.
***********************************************************************************************/
#include "FwKernelTlb.h"
#include "FwCellarTlb.h"

/***********************************************************************************************
	Implementations.
***********************************************************************************************/
#include "FwXmlData.h"

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c mkload.bat "
// End:

#endif /*MAIN_H*/
