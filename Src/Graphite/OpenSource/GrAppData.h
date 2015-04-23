/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrAppData.h
Responsibility: Sharon Correll
Last reviewed: not yet

Description:
	Data structures need by applications that use the Graphite engine.
----------------------------------------------------------------------------------------------*/


#ifndef GRAPPDATA_INCLUDED
#define GRAPPDATA_INCLUDED

#include "GrData.h"

typedef unsigned int featid;		// font feature IDs
typedef unsigned int lgid;			// language ID (for access feature UI strings)
typedef unsigned int toffset;		// text-source index

typedef struct {		// ISO-639-3 language code (for mapping onto features)
	char rgch[4];
} isocode;

#endif // GRAPPDATA_INCLUDED
