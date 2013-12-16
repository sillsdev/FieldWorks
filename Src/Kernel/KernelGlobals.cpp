/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 2010-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
File: KernelGlobals.cpp
Responsibility: Calgary
Last reviewed:

	Contains what should be the only static or global objects (not scalers) that
	exist in the Kernel module.
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "Main.h"

// Nothing should directly reference this.
static KernelGlobals g_kernel;

KernelGlobals::KernelGlobals()
{
	g_tph = NewObj TsPropsHolder;

	g_vptsg = NewObj TsgVec;

	g_strf = NewObj TsStrFact;

	g_tsh = NewObj TsStrHolder;
}

KernelGlobals::~KernelGlobals()
{
	delete g_tsh;
	g_tsh = NULL;

	delete g_strf;
	g_strf = NULL;

	delete g_vptsg;
	g_vptsg = NULL;

	delete g_tph;
	g_tph = NULL;
}

// Storage for static members
TsPropsHolder *KernelGlobals::g_tph;
TsStrHolder *KernelGlobals::g_tsh;

// There's a single global instance of the ITsStringFactory.
TsStrFact *KernelGlobals::g_strf;

// Originally from TextServ.cpp
TsgVec *KernelGlobals::g_vptsg;

#ifdef DEBUG
// Originally from DebugReport.cpp
IDebugReportSinkPtr KernelGlobals::s_qReportSink = NULL;
#endif
