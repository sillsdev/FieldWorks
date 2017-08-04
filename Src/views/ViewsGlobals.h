/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 2010-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: ViewsGlobals.h
Responsibility: Calgary
Last reviewed:

	Contains what should be the only static or global objects (not scalers) that
	exist in the Views module.
	This was done to control the order of construction and (mainly) destruction).
	see FWNX-177.
-------------------------------------------------------------------------------*//*:End Ignore*/

#if _MSC_VER
#pragma once
#endif
#ifndef ViewsGlobals_H
#define ViewsGlobals_H 1

#include "lib/TsTextProps.h"
#include "lib/TsStrFactory.h"
#include "lib/TextServ.h"
#ifdef DEBUG
#include "lib/DebugReport.h"
#endif

#ifdef WIN32
#include "VwAccessRoot.h"
#endif

class ViewsGlobals
{
public:
	ViewsGlobals();
	~ViewsGlobals();

#ifdef WIN32
	// Originally from VwAccessRoot.h
	static BoxAccessorMap *m_hmboxacc;
#endif

	// Originally from VwOverlay.cpp
	static ILgCollatingEnginePtr s_qcoleng;

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
