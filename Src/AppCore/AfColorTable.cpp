/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfColorTable.cpp
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	This supports palette color information.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

#define MAX_COLORS 100 // Used in defining a palette.


/***********************************************************************************************
	Color Table Data and Implementation
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Definition of the colors we're using in FieldWorks (a subset of the millions available.)
	The defines are in the resources file.
	Note that knNinch is also a valid color, but is deliberately not in the table: when the
	color is knNinch, we show no button depressed. There is no way to select Ninch.
----------------------------------------------------------------------------------------------*/
ColorTable::ColorDefn ColorTable::s_rgcdn[] =
{
	{ kstidUnspecified,     (COLORREF)kclrTransparent },
	{ kstidBlack,			kclrBlack },
	{ kstidBrown,			kclrBrown },
	{ kstidDarkOliveGreen,	kclrDarkOliveGreen },
	{ kstidDarkGreen,		kclrDarkGreen },
	{ kstidDarkTeal,		kclrDarkTeal },
	{ kstidDarkBlue,		kclrDarkBlue },
	{ kstidIndigo,			kclrIndigo },
	{ kstidDarkGray,		kclrDarkGray },
	{ kstidDarkRed,			kclrDarkRed },
	{ kstidOrange,			kclrOrange },
	{ kstidDarkYellow,		kclrDarkYellow },
	{ kstidGreen,			kclrGreen },
	{ kstidTeal,			kclrTeal },
	{ kstidBlue,			kclrBlue },
	{ kstidBlueGray,		kclrBlueGray },
	{ kstidGray40,			kclrGray40 },
	{ kstidRed,				kclrRed },
	{ kstidLightOrange,		kclrLightOrange },
	{ kstidLime,			kclrLime },
	{ kstidSeaGreen,		kclrSeaGreen },
	{ kstidAqua,			kclrAqua },
	{ kstidLightBlue,		kclrLightBlue },
	{ kstidViolet,			kclrViolet },
	{ kstidGray50,			kclrGray50 },
	{ kstidPink,			kclrPink },
	{ kstidGold,			kclrGold },
	{ kstidYellow,			kclrYellow },
	{ kstidBrightGreen,		kclrBrightGreen },
	{ kstidTurquoise,		kclrTurquoise },
	{ kstidSkyBlue,			kclrSkyBlue },
	{ kstidPlum,			kclrPlum },
	{ kstidLightGray,		kclrLightGray },
	{ kstidRose,			kclrRose },
	{ kstidTan,				kclrTan },
	{ kstidLightYellow,		kclrLightYellow },
	{ kstidPaleGreen,		kclrPaleGreen },
	{ kstidPaleTurquoise,	kclrPaleTurquoise },
	{ kstidPaleBlue,		kclrPaleBlue },
	{ kstidLavender,		kclrLavender },
	{ kstidWhite,			kclrWhite },
};

/*----------------------------------------------------------------------------------------------
	The one (and only) color table, used throughout the application where we are interested
	in colors.
----------------------------------------------------------------------------------------------*/
ColorTable g_ct;


/*----------------------------------------------------------------------------------------------
	Constructor - create the color palette that is capable of displaying all of the colors
	in this table.
----------------------------------------------------------------------------------------------*/
ColorTable::ColorTable()
{
	struct
	{
		LOGPALETTE lp;
		PALETTEENTRY rgpe[MAX_COLORS];
	} pal;

	LOGPALETTE * plp = (LOGPALETTE *)&pal;
	plp->palVersion = 0x300;
	plp->palNumEntries = (WORD)Size();
	for (int i = Size() - 1; i > 0; )
	{
		COLORREF cr = GetColor(i);
		i--;
		plp->palPalEntry[i].peRed	= GetRValue(cr);
		plp->palPalEntry[i].peGreen = GetGValue(cr);
		plp->palPalEntry[i].peBlue	= GetBValue(cr);
		plp->palPalEntry[i].peFlags = 0;
	}
	m_hpal = CreatePalette(plp);
}


/*----------------------------------------------------------------------------------------------
	Destructor - removes the palette from Windows system memory.
----------------------------------------------------------------------------------------------*/
ColorTable::~ColorTable()
{
	if (m_hpal)
	{
		BOOL fSuccess;
		fSuccess = DeleteObject(m_hpal);
		Assert(fSuccess);
		m_hpal = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	Returns the number of ColorDefn's in the table.
----------------------------------------------------------------------------------------------*/
int ColorTable::Size()
{
	return SizeOfArray(s_rgcdn);
}


/*----------------------------------------------------------------------------------------------
	Returns the RGB value corresponding to the index into the color table.
----------------------------------------------------------------------------------------------*/
COLORREF ColorTable::GetColor(int icdn)
{
	Assert((uint)icdn < (uint)Size());
	return s_rgcdn[icdn].m_clr;
}


/*----------------------------------------------------------------------------------------------
	Returns the string resource id corresponding to the index into the color table.
----------------------------------------------------------------------------------------------*/
int ColorTable::GetColorRid(int iColor)
{
	if (iColor == -1)
		return kstidUnknownColor;
	Assert(iColor < Size());
	return s_rgcdn[iColor].m_stidName;
}


/*----------------------------------------------------------------------------------------------
	Given an RGB value, returns the index of that value within the color table, or -1 if not
	found in the table.
----------------------------------------------------------------------------------------------*/
int ColorTable::GetIndexFromColor(COLORREF clr)
{
	int icdn;
	for (icdn = Size(); --icdn >= 0; )
	{
		if (s_rgcdn[icdn].m_clr == clr)
			break;
	}

	return icdn;
}


/*----------------------------------------------------------------------------------------------
	Maps entries from this color table's logical palette to the system palette.

	Returns:
		The old palette (provided the device is capable of realizing the palette) or NULL.
----------------------------------------------------------------------------------------------*/
HPALETTE ColorTable::RealizePalette(HDC hdc)
{
	HPALETTE hpalOld = NULL;
	int iCaps = ::GetDeviceCaps(hdc, RASTERCAPS);
	if (iCaps & RC_PALETTE)
	{
		// Normally for old hardware (probably pre 1998) that
		// typically have color depth that is less than 16 bits (i.e. 8 bit).
		hpalOld = ::SelectPalette(hdc, m_hpal, FALSE);
		Assert(hpalOld);
		UINT cnt; //number of entries in the logical palette mapped to the system palette.
		cnt = ::RealizePalette(hdc);
		Assert(cnt != GDI_ERROR);
	}
	return hpalOld;
}