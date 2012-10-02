/*----------------------------------------------------------------------------------------------
Copyright 1999, SIL International. All rights reserved.

File: Main.h (for UniConvert)
Responsibility: Darrell Zook
Last reviewed: Never

	Main header file for UniConvert.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef Main_H
#define Main_H 1

#define NO_EXCEPTIONS 1
#include "Common.h"


#define BUFFER_SIZE 4096
#define ERROR_LIMIT 1000
#define NUM_COMBOS	  5
#define HISTORY_COUNT 10



/***********************************************************************************************
	Interfaces.
***********************************************************************************************/

#include "ConvertStringTlb.h"

/***********************************************************************************************
	Implementations.
***********************************************************************************************/

#include "Windows.h"
#include "ConvertProcess.h"
#include "ConvertCallback.h"
#include "MainDlg.h"
#include "UniConvert.h"
#include "resource.h"
#include <commctrl.h>
#include <time.h>
#include <Shlobj.h>
#include <stdio.h>


#endif // !Main_H
