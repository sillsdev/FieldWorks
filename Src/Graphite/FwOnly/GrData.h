/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrData.h
Responsibility: Sharon Correll
Last reviewed: not yet

Description:
	FieldWorks-only version of data structures and data types.
----------------------------------------------------------------------------------------------*/

#include "FwKernelTlb.h"	// includes interfaces from DbAccess and Language DLLs.
//#include "DbAccessTlb.h"
//#include "LanguageTlb.h"
#include "ViewsTlb.h"
#include "ITraceControl.h"

#define LineBrk LgLineBreak
#define TrWsHandling LgTrailingWsHandling
#define SegEnd LgEndSegmentType
#define UtfType LgUtfForm

// Just define these here, since FieldWorks doesn't really need them.
enum tagFlushMode
{
	kflushAuto		= 0,
	kflushManual	= kflushAuto + 1
};

namespace gr
{

typedef unsigned char      data8;
typedef unsigned short int data16;	// generic 16-bit data
typedef unsigned int       data32;	// generic 32-bit data
typedef signed char        sdata8;
typedef signed short int   sdata16;	// generic 16-bit data
typedef signed int         sdata32;	// generic 32-bit data


// Note that these classes are based on floating point values, different from the standard
// FW classes.

struct Point
{
	float x;
	float y;

	Point()
	{
		x = y = 0;
	}

	Point(POINT & p)
	{
		x = (float)p.x;
		y = (float)p.y;
	}
};


struct Rect
{
	float top;
	float bottom;
	float left;
	float right;

	Rect()
	{
		top = bottom = left = right = 0;
	};

	Rect(RECT & r)
	{
		top = (float)r.top;
		bottom = (float)r.bottom;
		left = (float)r.left;
		right = (float)r.right;
	};
};

} // namespace gr