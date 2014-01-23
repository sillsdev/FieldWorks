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
#if WIN32
	m_hmboxacc = NewObj BoxAccessorMap;
#endif
}

ViewsGlobals::~ViewsGlobals()
{
#if WIN32
	delete m_hmboxacc;
#endif
}

#if WIN32
// Storage for static members
BoxAccessorMap *ViewsGlobals::m_hmboxacc;
#endif

ILgCollatingEnginePtr ViewsGlobals::s_qcoleng;
