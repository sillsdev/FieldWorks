/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2010 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

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
