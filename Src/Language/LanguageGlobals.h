/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2010 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LanguageGlobals.h
Responsibility: Calgary
Last reviewed:

	Contains what should be the only static or global objects (not scalers) that
	exist in the Language module.
	This was done to control the order of construction and (mainly) destruction).
	see FWNX-177.
-------------------------------------------------------------------------------*//*:End Ignore*/

#if _MSC_VER
#pragma once
#endif
#ifndef LanguageGlobals_H
#define LanguageGlobals_H 1

#include "LgFontManager.h"
#include "LgTextServices.h"

class LanguageGlobals
{
public:
	LanguageGlobals();
	~LanguageGlobals();

	// Originally from LgFontManager.h
	static LgFontManager g_fm; // Global LgFontManager.

	// Originally from LgTextServices.cpp
	static LgTextServices g_lts;
};

#endif