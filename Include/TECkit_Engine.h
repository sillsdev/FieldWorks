/*------------------------------------------------------------------------
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TECkit_Engine.h
Responsibility: Jonathan Kew
Last reviewed: Not yet.

Description:
	Public API to the TECkit conversion engine.
-------------------------------------------------------------------------*/

/*
	TECkit_Engine.h

	Public API to the TECkit encoding conversion library.

	Jonathan Kew	22-Dec-2001
					14-May-2002		added WINAPI to function declarations
					 5-Jul-2002		corrected placement of WINAPI to keep MS compiler happy
*/

#ifndef __TECkit_Engine_H__
#define __TECkit_Engine_H__

#include "TECkit_Common.h"

#define	kCurrentTECkitVersion	0x00020000	/* 16.16 version number */

/* formFlags bits for normalization; if none are set, then this side of the mapping is normalization-form-agnostic on input, and may generate an unspecified mixture */
#define kFlags_ExpectsNFC		0x00000001	/* expects fully composed text (NC) */
#define kFlags_ExpectsNFD		0x00000002	/* expects fully decomposed text (NCD) */
#define kFlags_GeneratesNFC		0x00000004	/* generates fully composed text (NC) */
#define kFlags_GeneratesNFD		0x00000008	/* generates fully decomposed text (NCD) */

/* if VisualOrder is set, this side of the mapping deals with visual-order rather than logical-order text (only relevant for bidi scripts) */
#define kFlags_VisualOrder		0x00008000	/* visual rather than logical order */

/* if Unicode is set, the encoding is Unicode on this side of the mapping */
#define kFlags_Unicode			0x00010000	/* this is Unicode rather than a byte encoding */

/* required names */
#define kNameID_LHS_Name		0		/* "source" or LHS encoding name, e.g. "SIL-EEG_URDU-2001" */
#define kNameID_RHS_Name		1		/* "destination" or RHS encoding name, e.g. "UNICODE-3-1" */
#define kNameID_LHS_Description	2		/* source encoding description, e.g. "SIL East Eurasia Group Extended Urdu (Mac OS)" */
#define kNameID_RHS_Description	3		/* destination description, e.g. "Unicode 3.1" */
/* additional recommended names (parallel to UTR-22) */
#define kNameID_Version			4		/* "1.0b1" */
#define kNameID_Contact			5		/* "mailto:jonathan_kew@sil.org" */
#define kNameID_RegAuthority	6		/* "SIL International" */
#define kNameID_RegName			7		/* "Greek (Galatia)" */
#define kNameID_Copyright		8		/* "(c)2002 SIL International" */
/* additional name IDs may be defined in the future */

/*
	encoding form constants for TECkit_CreateConverter
*/
#define	kForm_EncodingFormMask		0x000F
#define kForm_Unspecified			0	/* invalid as argument to TECkit_CreateConverter */
#define	kForm_Bytes					1
#define kForm_UTF8					2
#define kForm_UTF16BE				3
#define kForm_UTF16LE				4
#define kForm_UTF32BE				5
#define kForm_UTF32LE				6

#define	kForm_NormalizationMask		0x0F00
#define	kForm_NFC					0x0100
#define	kForm_NFD					0x0200

/*
	end of text value for TECkit_DataSource functions to return
*/
#define	kEndOfText					0xffffffffUL

/*
	A converter object is an opaque pointer
*/
typedef struct Opaque_TECkit_Converter*		TECkit_Converter;

#if defined(__cplusplus)
extern "C" {
#endif

#ifdef _WIN32
/* MS compiler has predefined _WIN32, so assume Windows target  */
#include <windef.h>
#else
/* not the MS compiler, so try Metrowerks' platform macros */
#if __dest_os == __win32_os
#include <windef.h>
#else
#define WINAPI
#define CALLBACK
#endif
#endif

/*
	Create a converter object from a compiled mapping
*/
TECkit_Status
WINAPI
TECkit_CreateConverter(
	Byte*				mapping,
	UInt32				mappingSize,
	Byte				mapForward,
	UInt16				sourceForm,
	UInt16				targetForm,
	TECkit_Converter*	converter);

/*
	Dispose of a converter object
*/
TECkit_Status
WINAPI
TECkit_DisposeConverter(
	TECkit_Converter	converter);

/*
	Read a name record or the flags from a converter object
*/
TECkit_Status
WINAPI
TECkit_GetConverterName(
	TECkit_Converter	converter,
	UInt16				nameID,
	Byte*				nameBuffer,
	UInt32				bufferSize,
	UInt32*				nameLength);

TECkit_Status
WINAPI
TECkit_GetConverterFlags(
	TECkit_Converter	converter,
	UInt32*				sourceFlags,
	UInt32*				targetFlags);

/*
	Reset a converter object, forgetting any buffered context/state
*/
TECkit_Status
WINAPI
TECkit_ResetConverter(
	TECkit_Converter	converter);

/*
	Convert text from a buffer in memory
*/
TECkit_Status
WINAPI
TECkit_ConvertBuffer(
	TECkit_Converter	converter,
	const Byte*			inBuffer,
	UInt32				inLength,
	UInt32*				inUsed,
	Byte*				outBuffer,
	UInt32				outLength,
	UInt32*				outUsed,
	Byte				inputIsComplete);

/*
	Flush any buffered text from a converter object
	(at end of input, if inputIsComplete flag not set for ConvertBuffer)
*/
TECkit_Status
WINAPI
TECkit_Flush(
	TECkit_Converter	converter,
	Byte*				outBuffer,
	UInt32				outLength,
	UInt32*				outUsed);

/*
	Read name and flags directly from a compiled mapping, before making a converter object
*/
TECkit_Status
WINAPI
TECkit_GetMappingName(
	Byte*				mapping,
	UInt32				mappingSize,
	UInt16				nameID,
	Byte*				nameBuffer,
	UInt32				bufferSize,
	UInt32*				nameLength);

TECkit_Status
WINAPI
TECkit_GetMappingFlags(
	Byte*				mapping,
	UInt32				mappingSize,
	UInt32*				lhsFlags,
	UInt32*				rhsFlags);

/*
	Return the version number of the TECkit library
*/
UInt32
WINAPI
TECkit_GetVersion();

#if 0

/*
	Set up to convert from a data source function
	(this API is not yet implemented!)
*/
typedef	UInt32 (CALLBACK *TECkit_DataSource)(void* userData);

TECkit_Status
WINAPI
TECkit_BeginConversion(
	TECkit_Converter	converter,
	TECkit_DataSource	source);

TECkit_Status
WINAPI
TECkit_GetCharacter(
	TECkit_Converter	converter,
	UInt32*				outChar);

TECkit_Status
WINAPI
TECkit_EndConversion(
	TECkit_Converter	converter);

#endif

#if defined(__cplusplus)
}	/* extern "C" */
#endif

#endif /* __TECkit_Engine_H__ */
