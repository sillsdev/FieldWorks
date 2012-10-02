/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2010 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: KernelGlobals.h
Responsibility: Calgary
Last reviewed:

	Contains what should be the only static or global objects (not scalers) that
	exist in the Kernel module.
	This was done to control the order of construction and (mainly) destruction).
	see FWNX-177.
-------------------------------------------------------------------------------*//*:End Ignore*/

#if _MSC_VER
#pragma once
#endif
#ifndef Globals_H
#define Globals_H 1


#include "Main.h"
#include "TsTextProps.h"
#include "TsStrFactory.h"
#include "TextServ.h"
#include "DebugReport.h"

class KernelGlobals
{
public:
	KernelGlobals();
	~KernelGlobals();

	// Originally from TsTextProps.cpp
	static TsPropsHolder *g_tph;
	static TsStrHolder *g_tsh;

	// Originally from TsStrFactory.cpp
	static TsStrFact *g_strf;

	// Originally from TextServ.h
	// This keeps a list of all the TSGs allocated for all threads.
	// It is needed because DetachThread is not called when the library is closed
	// or the process stops, only ProcessDetach; and there is no way for that function
	// to get access to the TLS slots for other threads.
	// All code that uses this must enter the critical section g_crs.
	static TsgVec *g_vptsg;

#ifdef DEBUG
	// Originally from DebugReport.h
	static IDebugReportSinkPtr s_qReportSink;
#endif

};

#endif
