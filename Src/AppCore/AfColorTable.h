/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: AfColorTable.h
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	Code for dealing with colors in a FieldWorks application. This includes:
	1. A Color Table, defining 40 colors,

	For the Color Table, we define a global table of type ColorTable, which we expect to
	then be used throughout the application. Each ColorDefn in the color table consists
	of an ID (defined in AfDef.h), an RGB value, and a string (defined in
	the resource file) to be used in the UI for describing the color to the user.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef AFCOLORTABLE_H
#define AFCOLORTABLE_H
#pragma once

/*----------------------------------------------------------------------------------------------
	RGB Values contained in the color table, and available for use throughout the application.
----------------------------------------------------------------------------------------------*/
#define kclrBlack			RGB(0x00, 0x00, 0x00) //   0,   0,   0
#define kclrBrown			RGB(0x90, 0x2F, 0x00) // 144,  47,   0
#define kclrDarkOliveGreen	RGB(0x2F, 0x2F, 0x00) //  47,  47,   0
#define kclrDarkGreen		RGB(0x00, 0x2F, 0x00) //   0,  47,   0
#define kclrDarkTeal		RGB(0x00, 0x2F, 0x60) //   0,  47,  96
#define kclrDarkBlue		RGB(0x00, 0x00, 0x7F) //   0,   0, 127
#define kclrIndigo			RGB(0x2F, 0x2F, 0x90) //  47,  47, 144
#define kclrDarkGray		RGB(0x2F, 0x2F, 0x2F) //  47,  47,  47
#define kclrDarkRed			RGB(0x7F, 0x00, 0x00) // 127,   0,   0
#define kclrOrange			RGB(0xFF, 0x60, 0x00) // 255,  96,   0
#define kclrDarkYellow		RGB(0x7F, 0x7F, 0x00) // 127, 127,   0
#define kclrGreen			RGB(0x00, 0xC0, 0x00) //   0, 192,   0
#define kclrTeal			RGB(0x00, 0x7F, 0x7F) //   0, 127, 127
#define kclrBlue			RGB(0x00, 0x00, 0xFF) //   0,   0, 255
#define kclrBlueGray		RGB(0x60, 0x60, 0x90) //  96,  96, 144
#define kclrGray40			RGB(0x7F, 0x7F, 0x7F) // 127, 127, 127
#define kclrRed				RGB(0xFF, 0x00, 0x00) // 255,   0,   0
#define kclrLightOrange		RGB(0xFF, 0x90, 0x00) // 255, 144,   0
#define kclrLime			RGB(0x90, 0xC0, 0x00) // 144, 192,   0
#define kclrSeaGreen		RGB(0x2F, 0x90, 0x60) //  47, 144,  96
#define kclrAqua			RGB(0x2F, 0xC0, 0xC0) //  47, 192, 192
#define kclrLightBlue		RGB(0x2F, 0x60, 0xFF) //  47,  96, 255
#define kclrViolet			RGB(0x7F, 0x00, 0x7F) // 127,   0, 127
#define kclrGray50			RGB(0x90, 0x90, 0x90) // 144, 144, 144
#define kclrPink			RGB(0xFF, 0x00, 0xFF) // 255,   0, 255
#define kclrGold			RGB(0xFF, 0xC0, 0x00) // 255, 192,   0
#define kclrYellow			RGB(0xFF, 0xFF, 0x00) // 255, 255,   0
#define kclrBrightGreen		RGB(0x00, 0xFF, 0x00) //   0, 255,   0
#define kclrTurquoise		RGB(0x00, 0xFF, 0xFF) //   0, 255, 255
#define kclrSkyBlue			RGB(0x00, 0xC0, 0xFF) //   0, 192, 255
#define kclrPlum			RGB(0x90, 0x2F, 0x60) // 144,  47,  96
#define kclrLightGray		RGB(0xC0, 0xC0, 0xC0) // 192, 192, 192
#define kclrRose			RGB(0xFF, 0x90, 0xC0) // 255, 144, 192
#define kclrTan				RGB(0xFF, 0xC0, 0x90) // 255, 192, 144
#define kclrLightYellow		RGB(0xFF, 0xFF, 0x90) // 255, 255, 144
#define kclrPaleGreen		RGB(0xCF, 0xFF, 0xCF) // 207, 255, 207
#define kclrPaleTurquoise	RGB(0xC0, 0xFF, 0xFF) // 192, 255, 255
#define kclrPaleBlue		RGB(0x90, 0xC0, 0xFF) // 144, 192, 255
#define kclrLavender		RGB(0xC0, 0x90, 0xFF) // 192, 144, 255
#define kclrWhite			RGB(0xFF, 0xFF, 0xFF) // 255, 255, 255

/*----------------------------------------------------------------------------------------------
	The ColorTable, by which we work with colors in the FieldWorks applications. Exactly one
	of these is defined in the cpp file, and should be used as an extern where needed.
----------------------------------------------------------------------------------------------*/

class ColorTable
{
public:
	// Construction
	ColorTable();
	~ColorTable();

	// Returns the number of colors in the table
	int Size();

	// Returns the RGB value corresponding to the index into the color table.
	COLORREF GetColor(int iColor);

	// Returns the resource index of the color corresponding to the index into the color table.
	int GetColorRid(int iColor);

	// Returns the index in the table of the given RGB value, or -1 if not found.
	int GetIndexFromColor(COLORREF clr);

	// Map this table's colors onto the system palette for quality drawing
	HPALETTE RealizePalette(HDC hdc);

protected:
	// Hungarian: cdn
	struct ColorDefn
	{
		int m_stidName; // The ID.
		COLORREF m_clr; // The RGB value.
	};
	static ColorDefn s_rgcdn[]; // Array of color definitions.

	// Palette capable of displaying the color table.
	HPALETTE m_hpal;

};

extern ColorTable g_ct;

#endif // !AFCOLORTABLE_H
