/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2010 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LanguageGlobals.cpp
Responsibility: Calgary
Last reviewed:

	Contains what should be the only static or global objects (not scalers) that
	exist in the Language module.
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "Main.h"

// Nothing should directly reference this.
static LanguageGlobals g_language;

LanguageGlobals::LanguageGlobals()
{

}

LanguageGlobals::~LanguageGlobals()
{

}

// The single global instance of the LgFontManager.
LgFontManager LanguageGlobals::g_fm; // Global LgFontManager.

// The single global instance of the LgTextServices.
LgTextServices LanguageGlobals::g_lts;