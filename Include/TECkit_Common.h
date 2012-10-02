/*------------------------------------------------------------------------
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TECkit_Common.h
Responsibility: Jonathan Kew
Last reviewed: Not yet.

Description:
	Public definitions used by TECkit engine and compiler
-------------------------------------------------------------------------*/

#ifndef __TECkit_Common_H__
#define __TECkit_Common_H__

#ifndef MAC_TYPES	/* these are all predefined if using a Mac prefix */
typedef unsigned char			UInt8;
typedef unsigned short			UInt16;
typedef unsigned long			UInt32;
typedef UInt8					Byte;
typedef Byte*					BytePtr;
typedef UInt16					UniChar;

typedef char*					Ptr;
typedef Byte*					TextPtr;
#endif

/*
	all public functions return a status code
*/
typedef long					TECkit_Status;

/*
	possible TECkit_Status return values
*/
#define	kStatus_NoError				0	/* this is usually the desired result! */

/* positive values are informational status values */
#define kStatus_OutputBufferFull	1	/* ConvertBuffer or Flush: output buffer full, so not all input was processed */
#define kStatus_NeedMoreInput		2	/* ConvertBuffer: processed all input data, ready for next chunk */

/* negative values are errors */
#define kStatus_InvalidForm			-1	/* inForm or outForm parameter doesn't match mapping (bytes/Unicode mismatch) */
#define kStatus_ConverterBusy		-2	/* can't initiate a conversion, as the converter is already in the midst of an operation */
#define kStatus_InvalidConverter	-3	/* converter object is corrupted (or not really a TECkit_Converter at all) */
#define kStatus_InvalidMapping		-4	/* compiled mapping data is not recognizable */
#define kStatus_BadMappingVersion	-5	/* compiled mapping is not a version we can handle */
#define kStatus_Exception			-6	/* an internal error has occurred */
#define kStatus_NameNotFound		-7	/* couldn't find the requested name in the compiled mapping */
#define kStatus_IncompleteChar		-8	/* bad input data (lone surrogate, incomplete UTF8 sequence) */
#define kStatus_CompilationFailed	-9	/* mapping compilation failed (syntax errors, etc) */
#define kStatus_OutOfMemory			-10	/* unable to allocate required memory */

#endif
