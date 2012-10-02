/*----------------------------------------------------------------------------------------------
File: FwAppVersion.h
Description:
	Defines application and compatible database version constants.
	This file eventually will be edited automatically (by a yet to be created utility) and
	will not need to be edited by hand, however, for now, we do have to edit it by hand.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef FWAPPVERSION_INCLUDED
#define FWAPPVERSION_INCLUDED 1

// We are not using the following three variables, because we are using kndbVersion in AfCore.h
// This file could be removed later.
const int knApplicationVersion = 500;
const int knDbVerCompatEarliest = 500;
const int knDbVerCompatLastKnown = 500;

#endif // FWAPPVERSION_INCLUDED
