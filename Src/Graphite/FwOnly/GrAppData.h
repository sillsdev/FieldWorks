/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrAppData.h
Responsibility: Sharon Correll
Last reviewed: not yet

Description:
	FieldWorks-only version of data structures and data types that are needed by applications
	that use the Graphite engine.

	Right now this is the ITextSource.h file.
----------------------------------------------------------------------------------------------*/

#include "GrResult.h"
typedef unsigned short int utf16;	// UTF16 encoded unicode codepoints.
typedef unsigned int featid;		// font feature IDs
typedef unsigned int lgid;			// language ID (for access feature UI strings)
typedef unsigned int toffset;		// text-source index

typedef struct {		// ISO-639-3 language code (for mapping onto features)
	char rgch[4];
} isocode;

// Ideally we should include GrPlatform.h or GrData.h here, but then we get weird errors with
// min and max (since GrPlatform.h fiddles with those definitions)
