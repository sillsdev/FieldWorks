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

#if WIN32
#include "VwAccessRoot.h"
#endif

class ViewsGlobals
{
public:
	ViewsGlobals();
	~ViewsGlobals();

#if WIN32
	// Originally from VwAccessRoot.h
	static BoxAccessorMap *m_hmboxacc;
#endif

	// Originally from VwOverlay.cpp
	static ILgCollatingEnginePtr s_qcoleng;
};

#endif
