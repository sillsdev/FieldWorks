/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2010 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

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
