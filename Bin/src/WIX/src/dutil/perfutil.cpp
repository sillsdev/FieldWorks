//-------------------------------------------------------------------------------------------------
// <copyright file="perfutil.cpp" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
//    Performance helper funtions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

static BOOL vfHighPerformanceCounter = TRUE;   // assume the system has a high performance counter
static double vdFrequency = 1;


/********************************************************************
 PerfInitialize - initializes internal static variables

********************************************************************/
extern "C" void DAPI PerfInitialize(
	)
{
	LARGE_INTEGER liFrequency = { 0 };

	//
	// check for high perf counter
	//
	if (!::QueryPerformanceFrequency(&liFrequency))
	{
		vfHighPerformanceCounter = FALSE;
		vdFrequency = 1000;  // ticks are measured in milliseconds
	}
	else
		vdFrequency = static_cast<double>(liFrequency.QuadPart);
}


/********************************************************************
 PerfClickTime - resets the clicker, or returns elapsed time since last call

 NOTE: if pliElapsed is NULL, resets the elapsed time
	   if pliElapsed is not NULL, returns perf number since last call to PerfClickTime()
********************************************************************/
extern "C" void DAPI PerfClickTime(
	__in LARGE_INTEGER* pliElapsed
	)
{
	static LARGE_INTEGER liStart = { 0 };
	LARGE_INTEGER* pli = pliElapsed;

	if (!pli)  // if elapsed time time was not requested, reset the start time
		pli = &liStart;

	if (vfHighPerformanceCounter)
		::QueryPerformanceCounter(pli);
	else
		pli->QuadPart = ::GetTickCount();

	if (pliElapsed)
		pliElapsed->QuadPart -= liStart.QuadPart;
}


/********************************************************************
 PerfConvertToSeconds - converts perf number to seconds

********************************************************************/
extern "C" double DAPI PerfConvertToSeconds(
	__in LARGE_INTEGER* pli
	)
{
	Assert(0 < vdFrequency);
	return pli->QuadPart / vdFrequency;
}
