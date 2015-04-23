/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrStructs.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Cross-platform structure definitions.
----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GRSTRUCTS_INCLUDED
#define GRSTRUCTS_INCLUDED

namespace gr
{

//:End Ignore

typedef struct tagGrCharProps
{
	unsigned long clrFore;
	unsigned long clrBack;
	int dympOffset;
	int ws;
	int ows;
	byte fWsRtl;
	int nDirDepth;
	byte ssv;
	byte ttvBold;
	byte ttvItalic;
	int dympHeight;
	wchar_t szFaceName[ 32 ];
	wchar_t szFontVar[ 64 ];
} GrCharProps;

// Used to pass feature information among the Graphite engine and the application.
typedef struct tagFeatureSetting
{
	int id;
	int value;
} FeatureSetting;

/*****
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
****/

} // namespace gr


#endif // !STRUCTS_INCLUDED
