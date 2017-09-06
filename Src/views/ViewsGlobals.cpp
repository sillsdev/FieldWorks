/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 2010-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: ViewsGlobals.cpp
Responsibility: Calgary
Last reviewed:

	Contains what should be the only static or global objects (not scalers) that
	exist in the Views module.
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "Main.h"

// Nothing should directly reference this.
static ViewsGlobals g_views;

ViewsGlobals::ViewsGlobals()
{
#if defined(WIN32) || defined(WIN64)
	m_hmboxacc = NewObj BoxAccessorMap;
#endif

	g_tph = NewObj TsPropsHolder;

	g_vptsg = NewObj TsgVec;

	g_strf = NewObj TsStrFact;

	g_tsh = NewObj TsStrHolder;
}

ViewsGlobals::~ViewsGlobals()
{
#if defined(WIN32) || defined(WIN64)
	delete m_hmboxacc;
#endif

	delete g_tsh;
	g_tsh = NULL;

	delete g_strf;
	g_strf = NULL;

	delete g_vptsg;
	g_vptsg = NULL;

	delete g_tph;
	g_tph = NULL;
}

#if defined(WIN32) || defined(WIN64)
// Storage for static members
BoxAccessorMap *ViewsGlobals::m_hmboxacc;
#endif

ILgCollatingEnginePtr ViewsGlobals::s_qcoleng;

// Storage for static members
TsPropsHolder *ViewsGlobals::g_tph;
TsStrHolder *ViewsGlobals::g_tsh;

// There's a single global instance of the ITsStringFactory.
TsStrFact *ViewsGlobals::g_strf;

// Originally from TextServ.cpp
TsgVec *ViewsGlobals::g_vptsg;

#ifdef DEBUG
// Originally from DebugReport.cpp
IDebugReportSinkPtr ViewsGlobals::s_qReportSink = NULL;
#endif
