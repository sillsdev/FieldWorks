/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrcFont.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Implementation the interaction between the TTF file and the compiler.
	Normally uses type names from TTF doc, except public interface uses utf16 for Unicode ids.
-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/
#include "main.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

/***********************************************************************************************
	Forward declarations
***********************************************************************************************/

/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/


/***********************************************************************************************
	Methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
GrcFont::GrcFont(char * pchFontFile) :
	m_pFile(NULL),
	m_pCmap(NULL), m_cCmap(0L),
	m_pGlyf(NULL), m_cGlyf(0L),
	m_pHead(NULL), m_cHead(0L),
	m_pHhea(NULL), m_cHhea(0L),
	m_pHmtx(NULL), m_cHmtx(0L),
	m_pLoca(NULL), m_cLoca(0L),
	m_pMaxp(NULL), m_cMaxp(0L),
	m_pOs2(NULL) , m_cOs2(0L),
	m_pPost(NULL), m_cPost(0L),
	m_pName(NULL), m_cName(0L),
	m_pCmap_3_1(NULL), m_pCmap_3_10(NULL),
	m_nMaxGlyfId(-1)
{
	AssertPtr(pchFontFile);
	int cch = pchFontFile ? strlen(pchFontFile) + 1: 0;
	if (cch)
	{
		m_pchFileName =  new char[cch];
		strcpy(m_pchFileName, pchFontFile); // strcpy_s(m_pchFileName, cch, pchFontFile)
	}
	else
	{
		m_pchFileName = NULL;
		std::cout << "Font file name is null.\n";
	}
	m_fDebug = false;
	return;
}

GrcFont::GrcFont(bool fDebug) :
	m_pFile(NULL),
	m_pCmap(NULL), m_cCmap(0L),
	m_pGlyf(NULL), m_cGlyf(0L),
	m_pHead(NULL), m_cHead(0L),
	m_pHhea(NULL), m_cHhea(0L),
	m_pHmtx(NULL), m_cHmtx(0L),
	m_pLoca(NULL), m_cLoca(0L),
	m_pPost(NULL), m_cPost(0L),
	m_pMaxp(NULL), m_cMaxp(0L),
	m_pOs2(NULL) , m_cOs2(0L),
	m_pName(NULL), m_cName(0L),
	m_pCmap_3_1(NULL), m_pCmap_3_10(NULL),
	m_nMaxGlyfId(-1)
{
	Assert(fDebug);
	m_fDebug = fDebug;
	m_pchFileName = NULL;
}

/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
GrcFont::~GrcFont()
{
	CloseFile();
	if (m_pchFileName)
	{
		delete [] m_pchFileName;
		m_pchFileName = NULL;
	}
	if (m_pCmap)
		delete [] m_pCmap;
	if (m_pGlyf)
		delete [] m_pGlyf;
	if (m_pHead)
		delete [] m_pHead;
	if (m_pHhea)
		delete [] m_pHhea;
	if (m_pHmtx)
		delete [] m_pHmtx;
	if (m_pLoca)
		delete [] m_pLoca;
	if (m_pMaxp)
		delete [] m_pMaxp;
	if (m_pOs2)
		delete [] m_pOs2;
	if (m_pPost)
		delete [] m_pPost;
	if (m_pName)
		delete [] m_pName;
}

/*----------------------------------------------------------------------------------------------
	Call after object constructed. Does most of the real initialization of the object such
	as reading the needed TTF tables and scanning the cmap for collisions.
	Needed so an error code can be returned. Must be called ONLY once BEFORE any other
	methods are called.
	Returns 0 if successful. Otherwise non-zero (see below for error numbers)
----------------------------------------------------------------------------------------------*/
int GrcFont::Init(GrcManager * pcman)
{
	Assert(!m_fDebug);
	Assert(m_pFile == NULL); // should only call once

	if (!OpenFile()) // open the TTF file passed in the ctor
	{
		g_errorList.AddError(107, NULL,
			"Could not open font file");
		return 1;
	}

	// read the offset table
	long lnOffset, lnSize;
	byte * pHdr, * pTableDir;
	if (!TtfUtil::GetHeaderInfo(lnOffset, lnSize))
		return 2;
	if (!ReadData(&pHdr, lnOffset, lnSize))
	{
		g_errorList.AddError(108, NULL,
			"Error reading font offset table.");
		return 3;
	}
	if (!TtfUtil::CheckHeader(pHdr))
	{
		g_errorList.AddError(109, NULL,
			"Font file has bad offset table");
		return 4;
	}

	if (!TtfUtil::GetTableDirInfo(pHdr, lnOffset, lnSize))
		return 5;
	if (!ReadData(&pTableDir, lnOffset, lnSize))
	{
		g_errorList.AddError(110, NULL,
			"Error reading font Table Directory.");
		return 6;
	}

	if (IsGraphiteFont(pHdr, pTableDir))
	{
		g_errorList.AddError(111, NULL,
			"Font already has Graphite table(s) present.");
		return 7;
	}

	// cmap
	if (!ReadTable(ktiCmap, pHdr, pTableDir, &m_pCmap, &m_cCmap))
		return 10;

	// Find cmap subtable we need.
	// Start with UTF-32 table platform 3 encoding 10
	m_pCmap_3_10 = TtfUtil::FindCmapSubtable(m_pCmap, 3, 10);

	// Regardless, we should also have a UTF-16 table. First try platform 3 encoding 1.
	m_pCmap_3_1 = TtfUtil::FindCmapSubtable(m_pCmap, 3, 1);
	if (m_pCmap_3_1 == NULL)
	{
		// try the platform 3 encoding 0 table instead
		m_pCmap_3_1 = TtfUtil::FindCmapSubtable(m_pCmap, 3, 0);
	}

	if (m_pCmap_3_10 == NULL && m_pCmap_3_1 == NULL)
	{
		g_errorList.AddError(112, NULL,
			"Microsoft Unicode cmap could not be found.");
		return 11;
	}
	if (m_pCmap_3_10)
	{
		if (!TtfUtil::CheckCmap310Subtable(m_pCmap_3_10))
		{
			g_errorList.AddError(113, NULL, "cmap platform 3 encoding 10 subtable is invalid.");
			return 12;
		}
	}
	if (m_pCmap_3_1)
	{
		if (!TtfUtil::CheckCmap31Subtable(m_pCmap_3_1))
		{
			if (m_pCmap_3_10) // we can survive
				g_errorList.AddWarning(506, NULL, "cmap platform 3 encoding 1 subtable is invalid.");
			else
				g_errorList.AddError(114, NULL, "cmap platform 3 encoding 1 subtable is invalid.");
			return 13;
		}
	}
	else if (m_pCmap_3_10)
	{
		// All fonts are supposed to have a 16-bit table.
		g_errorList.AddWarning(507, NULL, "cmap platform 3 encoding 1 subtable not found");
	}

	// glyf
	if (!ReadTable(ktiGlyf, pHdr, pTableDir, &m_pGlyf, &m_cGlyf))
		return 20;

	// head
	if (!ReadTable(ktiHead, pHdr, pTableDir, &m_pHead, &m_cHead))
		return 21;

	// hhea
	if (!ReadTable(ktiHhea, pHdr, pTableDir, &m_pHhea, &m_cHhea))
		return 2;

	// hmtx
	if (!ReadTable(ktiHmtx, pHdr, pTableDir, &m_pHmtx, &m_cHmtx))
		return 23;

	// loca
	if (!ReadTable(ktiLoca, pHdr, pTableDir, &m_pLoca, &m_cLoca))
		return 24;

	// maxp
	if (!ReadTable(ktiMaxp, pHdr, pTableDir, &m_pMaxp, &m_cMaxp))
		return 25;

	// OS/2
	if (!ReadTable(ktiOs2, pHdr, pTableDir, &m_pOs2, &m_cOs2))
		return 26;

	// post
	if (!ReadTable(ktiPost, pHdr, pTableDir, &m_pPost, &m_cPost))
		return 27;

	// name
	if (!ReadTable(ktiName, pHdr, pTableDir, &m_pName, &m_cName))
		return 28;

	delete [] pTableDir;
	delete [] pHdr;

	// scan the cmap for colliding Unicode values & find largest glyph id
	if (!ScanGlyfIds())
		return 30;

	if (AnySupplementaryPlaneChars() && pcman->FontTableVersion() <= 0x00010000)
	{
		g_errorList.AddError(115, NULL,
			"Supplementary plane characters are not supported in version 1.0");
	}

	return 0;
}

/*----------------------------------------------------------------------------------------------
	Get the font name out of the name table
----------------------------------------------------------------------------------------------*/
void GrcFont::GetFontFamilyName(utf16 * rgchwName, int cchMax)
{
	long lOffset = -1;
	long lSize = -1;

	// Alan Ward says don't bother looking for the 3-10 version, even if we have a 3-10
	// cmap subtable.

	if (!TtfUtil::Get31EngFamilyInfo(m_pName, lOffset, lSize))
	{
		if (!TtfUtil::Get30EngFamilyInfo(m_pName, lOffset, lSize))
		{
			return; // couldn't find it
		}
	}

	int cchw = (lSize / isizeof(utf16)) + 1;
	cchw = min(cchw, cchMax);
	memcpy(rgchwName, (byte *)m_pName + lOffset, lSize);
	rgchwName[cchw - 1] = 0;  // zero terminate
	TtfUtil::SwapWString(rgchwName, cchw - 1);
}

/*----------------------------------------------------------------------------------------------
	Get the first unused glyph ID that can be used for pseudo glyphs.
----------------------------------------------------------------------------------------------*/
utf16 GrcFont::FirstFreeGlyph()
{
#ifdef _DEBUG
	if (m_fDebug)
		//return 0x2200;
		return 0x0200;	// iucdemo
#endif

	Assert(m_pFile); // insure Init() called first
	Assert(m_nMaxGlyfId != -1);  // insure ScanGlyfIds() called
	Assert(m_pMaxp);
	Assert(m_pHead);

	size_t suMaxGlyf = 	m_nMaxGlyfId + 1; // assuming glyf ids start at zero

	size_t suTest = TtfUtil::GlyphCount(m_pMaxp);
	suMaxGlyf = max(suMaxGlyf, suTest);

	suTest = TtfUtil::LocaGlyphCount(m_cLoca, m_pHead); // could return -1
	suMaxGlyf = max(suMaxGlyf, suTest);

	return (utf16)suMaxGlyf;
}


/*----------------------------------------------------------------------------------------------
	Fill in the arrays with the pairs of ambiguous unicode values and assigned glyph IDs.
	Return the number found.
----------------------------------------------------------------------------------------------*/
int GrcFont::AutoPseudos(Vector<unsigned int> & vnUnicode, Vector<utf16> & vwGlyphID)
{
#ifdef _DEBUG
	if (m_fDebug)
		return 0;
#endif

	Assert(m_pFile); // insure Init() called
	Assert(m_nMaxGlyfId != -1); // insure ScanGlyfIds() called
	Assert(m_pCmap_3_10 || m_pCmap_3_1);

	// Fill the vectors with Unicode values that have duplicate glyph IDs and with the
	// glyph IDs that parallel the Unicode values.
	int nSize = m_vnCollisions.Size();
	for (int i = 0; i < nSize; i++)
	{
		unsigned int nUnicode = m_vnCollisions[i];
		vnUnicode.Push(nUnicode);
		utf16 wchGlyph = GlyphFromCmap(nUnicode, NULL);
		vwGlyphID.Push(wchGlyph);
	}
	return nSize;
}

/*----------------------------------------------------------------------------------------------
	Get the contents of the cmap. The array is assumed to be of adequate size.
----------------------------------------------------------------------------------------------*/
void GrcFont::GetGlyphsFromCmap(utf16 * rgchwUniToGlyphID)
{
	Assert(m_pFile); // insure Init() called
	Assert(m_nMaxGlyfId != -1); // insure ScanGlyfIds() called
	Assert(m_pCmap_3_10 || m_pCmap_3_1);

	GrcFont::iterator fit;
	int iUni;
	for (iUni = 0, fit = this->Begin(); fit != this->End(); ++iUni, ++fit)
	{
		unsigned int nUni = *fit;
		utf16 gid = GlyphFromCmap(nUni, NULL);
		rgchwUniToGlyphID[iUni] = gid;
	}
}

/*----------------------------------------------------------------------------------------------
	Use the given codepage to convert the codepoint to a Unicode value.
	Review: Is there ever a one-to-many mapping? Should we support codepages not installed
		on system?
----------------------------------------------------------------------------------------------*/
unsigned int GrcFont::UnicodeFromCodePage(utf16 wCodePage, utf16 wCodePoint, GdlObject * pgdlobj)
{
	utf16 wUnicode; // should never return supplementary plane characters!
	if (!MultiByteToWideChar(wCodePage, 0, (char *)&wCodePoint, 1, (LPWSTR)&wUnicode, 1))
	{
		g_errorList.AddWarning(508, pgdlobj, "Failed to convert CodePoint to Unicode");
		return 0; // calling method provides error message
	}
	return (int)wUnicode;
}

/*----------------------------------------------------------------------------------------------
	Convert the given unicode character to a glyph ID via the cmap. Return 0 if the
	character was not present in the cmap.
----------------------------------------------------------------------------------------------*/
utf16 GrcFont::GlyphFromCmap(unsigned int nUnicode, GdlObject * pgdlobj)
{
#ifdef _DEBUG
	if (m_fDebug)
		return nUnicode;
#endif

	Assert(m_pFile); // insure Init() called
	Assert(m_pCmap_3_10 || m_pCmap_3_1);

	if (m_pCmap_3_10)
	{
		return TtfUtil::Cmap310Lookup(m_pCmap_3_10, nUnicode);
	}
	else
	{
		return TtfUtil::Cmap31Lookup(m_pCmap_3_1, nUnicode);
	}
}

/*----------------------------------------------------------------------------------------------
	Convert the given postscript name to a glyph ID.
	Return 0 if the postscript name is invalid.
----------------------------------------------------------------------------------------------*/
utf16 GrcFont::GlyphFromPostscript(StrAnsi staPostscriptName, GdlObject * pgdlobj, bool fError)
{
	Assert(m_pFile);
	Assert(m_pPost);
	Assert(m_pMaxp);

	int nGlyphId = TtfUtil::PostLookup(m_pPost, m_cPost, m_pMaxp, staPostscriptName);
	if (nGlyphId >= 0)
		return nGlyphId;

	// calling method outputs invalid postscript name
	if (fError)
	{
		if (nGlyphId == -1)
			g_errorList.AddError(116, pgdlobj, "Postscript name not found");
		if (nGlyphId == -2)
			g_errorList.AddError(117, pgdlobj, "No Postscript name data in font");
		if (nGlyphId < -2)
			g_errorList.AddError(118, pgdlobj, "Postscript name lookup error");
	}
	return 0;

}

/*----------------------------------------------------------------------------------------------
	Given the number of a path used to define the glyph, answer the point number of the
	first point on the path. Normally this path will only have one point.
	Return -1 if the path is invalid.
	TODO AlanW: Still need to handle multi-level composite glyphs.
----------------------------------------------------------------------------------------------*/
int GrcFont::ConvertGPathToGPoint(utf16 wGlyphID, int nPathNumber, GdlObject * pgdlobj)
{
#ifdef _DEBUG
	if (m_fDebug)
		return nPathNumber;
#endif

	Vector<int> vnEndPt;
	int cContours, nEndPt;

	if (GetGlyfContours(wGlyphID, &vnEndPt))
	{
		cContours = vnEndPt.Size();
		if (nPathNumber < cContours)
		{
			if (nPathNumber == 0)
				return 0;

			nEndPt = vnEndPt[nPathNumber - 1] + 1;
			return nEndPt;
		}
	}

	char rgch[20];
	itoa(nPathNumber, rgch, 10);
	g_errorList.AddError(119, pgdlobj,
		"Cannot find point number for path number ",
		rgch,
		" in glyph 0x",
		GdlGlyphDefn::GlyphIDString(wGlyphID));
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Convert a scaled number to an absolute number of font design units.
----------------------------------------------------------------------------------------------*/
int GrcFont::ScaledToAbsolute(int nValue, int mScale)
{
	Assert(m_fDebug || m_pFile);

	if (mScale == kmunitNone || mScale == kmunitUnscaled)
		return nValue;
	else
	{
		float n = (((float)nValue * DesignUnits()) / (float)mScale) + (float)0.5;
		return (int)n;
	}
}

/*----------------------------------------------------------------------------------------------
	Return the design units used in the font, ie, the global scaling factor.
----------------------------------------------------------------------------------------------*/
int GrcFont::DesignUnits()
{
#ifdef _DEBUG
	if (m_fDebug)
		return 100;	// fabdemo, iucdemo
		//return 1000; // attachment pts test
#endif

	Assert(m_pFile);
	Assert(m_pHead);
	return (TtfUtil::DesignUnits(m_pHead));
}

/*----------------------------------------------------------------------------------------------
	Return the metric (in terms of the font's design units) for the given glyph.
	Returns a zero for all BB metrics on a white space glyph.
	Return INT_MIN or INT_MAX on error.
	Review: does USE_MY_METRICS flag in a composist glyph override the hmtx table?
----------------------------------------------------------------------------------------------*/
int GrcFont::GetGlyphMetric(utf16 wGlyphID, GlyphMetric gmet, GdlObject * pgdlobj)
{
	Assert(m_pFile);
	Assert(m_pLoca);
	Assert(m_pGlyf);
	Assert(m_pOs2);
	Assert(m_pHmtx);
	Assert(m_pHhea);

	switch(gmet)
	{
	case kgmetAscent:
		return TtfUtil::FontAscent(m_pOs2);
	case kgmetDescent:
		return TtfUtil::FontDescent(m_pOs2);
	case kgmetAdvHeight: // TODO AlanW (SharonC): eventually the vmtx table will be needed?
		return 0;
	}

	int nAdvWid, nLsb;
	if (gmet == kgmetAdvWidth || gmet == kgmetLsb || gmet == kgmetRsb)
	{
		if (!TtfUtil::HorMetrics(wGlyphID, m_pHmtx, m_cHhea, m_pHhea, nLsb, nAdvWid))
		{
			g_errorList.AddError(120, pgdlobj,
				"Unable to get horizontal metrics for glyph 0x",
				GdlGlyphDefn::GlyphIDString(wGlyphID));
			return INT_MIN;
		}

		switch(gmet)
		{
		case kgmetAdvWidth:
			return nAdvWid;
		case kgmetLsb: // Review: should we return xMin instead?
			return nLsb;
		// handle Rsb below
		}
	}

	if (TtfUtil::IsSpace(wGlyphID, m_pLoca, m_cLoca, m_pHead))
	{
		if (gmet == kgmetRsb)
			return nAdvWid; // for space. RSB same as adv width to agree with compiler

		g_errorList.AddWarning(509, pgdlobj,
			"Requesting bounding box metric for white space glyph 0x",
			GdlGlyphDefn::GlyphIDString(wGlyphID),
			"; 0 will be used");
		return 0; // for space, all BB metrics are zero
	}

	// Find bounding box metrics.
	/*
	Normally would use TtfUtil::GlyfBox but attachment points alter the metrics as follows.
	Normally the min and max values stored for each glyph also reflect the visual
	appearance of the glyph. When attachment points are added the min and max values
	may increase but the appearance of the glyph (the black part) will not change since
	attachment points aren't visible. We want the metrics returned here to match the
	appearance of the the glyph and not be the min and max values with attachment points.
	The GDL author will be familiar with former which is also more intuitive to use.
	The font could be adjusted so attachment points don't affect min and max values
	but that would add a step to the font production process and may produce
	unexpected side effects.
	The bounding box metrics returned from Windows (GetGlyphMetrics) disregard the
	min and max values for each glyph and match the visual appearance. Presumably this
	is related to the way the Windows rasterizer discards attachment points. This means
	the Graphite engine should obtain the same metrics as are returned here.
	Attachment points in this context are points that are alone on their own
	contour (or path), so they can easily be referenced by path number (GPath in GDL).
	*/

	Vector<int> vnEndPt, vnX, vnY;
	Vector<bool> vfOnCurve;

	int xMin = INT_MAX;
	int yMin = INT_MAX;
	int xMax = INT_MIN;
	int yMax = INT_MIN;

	if (GetGlyfPts(wGlyphID, &vnEndPt, &vnX, &vnY, &vfOnCurve))
	{
		int nFirstPt = 0;
		int nSecondPt = -1;	// so nFirstPt initialized to zero in loop below
		int cEndPoints = vnEndPt.Size();
		int i, j;
		for (i = 0; i < cEndPoints; i++)
		{
			nFirstPt = nSecondPt + 1;
			nSecondPt = vnEndPt[i];
			if (nSecondPt - nFirstPt) // throw out point on contour with single point
			{
				for (j = nFirstPt; j <= nSecondPt; j++)
				{
					xMin = Min(xMin, vnX[j]);
					yMin = Min(yMin, vnY[j]);
					xMax = Max(xMax, vnX[j]);
					yMax = Max(yMax, vnY[j]);
				}
			}
		}

		switch(gmet)
		{
		case kgmetBbTop:
			return yMax;
		case kgmetBbBottom:
			return yMin;
		case kgmetBbLeft:
			return xMin;
		case kgmetBbRight:
			return xMax;
		case kgmetBbWidth:
			return xMax - xMin;
		case kgmetBbHeight:
			return yMax - yMin;
		case kgmetRsb:
			return nAdvWid - nLsb - (xMax - xMin);
		}
	}

	g_errorList.AddError(121, pgdlobj,
		"Unable to get bounding box for glyph 0x",
		GdlGlyphDefn::GlyphIDString(wGlyphID));
	return INT_MIN;
}

/*----------------------------------------------------------------------------------------------
	Determine if a given point is the only point in its contour. If this is true, then the
	Engine will NOT be able to determine the coordinates for the point so the compiler should
	do that (with GetXYAtPoint).
----------------------------------------------------------------------------------------------*/
bool GrcFont::IsPointAlone(utf16 wGlyphID, int nPointNumber, GdlObject * pgdlobj)
{
	// these must be declared before goto statements
	Vector<int> vnEndPt;
	int i, cContours;

	if (GetGlyfContours(wGlyphID, &vnEndPt))
	{ // find point's contour
		cContours = vnEndPt.Size();
		for (i = 0; i < cContours; i++)
		{
			if (vnEndPt[i] >= nPointNumber)
			{ // found the contour
				if (i > 0)
				{
					if (vnEndPt[i] - vnEndPt[i - 1] > 1)
						return false;
					else
						return true;
				}
				else // i == 0
				{
					if (vnEndPt[i] > 0)
						return false;
					else
						return true;
				}
			}
		}
	}

	// if we reach here, point doesn't exist in glyph so give error
	char rgch[20];
	itoa(nPointNumber, rgch, 10);
	g_errorList.AddError(122, pgdlobj,
		"Cannot find contour for point number ",
		rgch,
		" in glyph 0x",
		GdlGlyphDefn::GlyphIDString(wGlyphID));
	return true; // safest fallback value
}

/*----------------------------------------------------------------------------------------------
	Find the coordinates for a point on a glyph.
	Return true if successful, otherwise false
----------------------------------------------------------------------------------------------*/
int GrcFont::GetXYAtPoint(utf16 wGlyphID, int nPointNumber, int * mX, int * mY,
						  GdlObject * pgdlobj)
{
	Vector<int> vnEndPt, vnX, vnY;
	Vector<bool> vfOnCurve;

	if (GetGlyfPts(wGlyphID, &vnEndPt, &vnX, &vnY, &vfOnCurve))
	{
		if (nPointNumber < vnX.Size())
		{
			*mX = vnX[nPointNumber];
			*mY = vnY[nPointNumber];
			return true;
		}
	}

	char rgch[20];
	itoa(nPointNumber, rgch, 10);
	g_errorList.AddError(123, pgdlobj,
		"Cannot find coordinates for point number ",
		rgch,
		" in glyph 0x",
		GdlGlyphDefn::GlyphIDString(wGlyphID));
	return false;
}

/*----------------------------------------------------------------------------------------------
	Try to find an actual on-curve point that is within mPointRadius units of the given
	x- and y-coordinates. If found, return the point number, otherwise return -1.
----------------------------------------------------------------------------------------------*/
int GrcFont::GetPointAtXY(utf16 wGlyphID, int mX, int mY, int mPointRadius, GdlObject * pgdlobj)
{
	Vector<int> vnEndPt, vnX, vnY;
	Vector<bool> vfOnCurve;

	const int mRadiusSqr = mPointRadius * mPointRadius;
	int dnX, dnY;
	int nDistanceSqr = -1;
	int iPtClosest = -1;
	int nDistanceSqrClosest = INT_MAX;
	int i, cPoints;

	if (GetGlyfPts(wGlyphID, &vnEndPt, &vnX, &vnY, &vfOnCurve))
	{
		// search through points for the closest one that is within mPointRadius of mX and mY
		cPoints = vnX.Size();
		for (i = 0; i < cPoints; i++)
		{
			dnX = vnX[i] - mX;
			dnY = vnY[i] - mY;
			// for a 2048 em, this should max at around 8 million so it will fit on a signed integer
			nDistanceSqr = dnX * dnX + dnY * dnY;
			if (nDistanceSqr <= mRadiusSqr && nDistanceSqr < nDistanceSqrClosest)
			{
				iPtClosest = i;
				nDistanceSqrClosest = nDistanceSqr;
			}
		}

		return iPtClosest; // initialized to -1
	}

	char rgch1[20];
	char rgch2[20];
	itoa(mX, rgch1, 10);
	itoa(mY, rgch2, 10);
	g_errorList.AddWarning(510, pgdlobj,
		"Cannot find point number for coordinates (",
		rgch1, ", ", rgch2,
		") in glyph 0x",
		GdlGlyphDefn::GlyphIDString(wGlyphID));

	//	For testing:
//	if (mX == 0 && mY == 0)
//		return 10;
//	else if (mX == 500 && mY == 500 && mPointRadius == 1000)
//		return 11;
//	else if (mX == 1000 && mY == 1000)
//		return 12;

	return -1;
}

/***********************************************************************************************
	Protected utility functions
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Open the file for this object.
	Return true if successful, otherwise false.
----------------------------------------------------------------------------------------------*/
int GrcFont::OpenFile()
{
	m_pFile = fopen(m_pchFileName, "rb");
	if (!m_pFile)
	{
		g_errorList.AddError(124, NULL,
			"Unable to open font file: ",
			m_pchFileName);
		return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Close the file for this object.
	Return true if successful, otherwise false.
----------------------------------------------------------------------------------------------*/
int GrcFont::CloseFile()
{
	if (m_pFile)
		return fclose(m_pFile) ? false : true;
	return false;
}

/*--------------------------------1--------------------------------------------------------------
	Read a data from the font storing the data in ppData.
	Return true if successful, otherwise false.
----------------------------------------------------------------------------------------------*/
int GrcFont::ReadData(byte ** ppData, long lnOffset, long lnSize)
{
	*ppData = new byte[lnSize];
	if (!*ppData)
	{
		g_errorList.AddError(125, NULL,
			"Memory failure: could not allocate ppData array while reading font file");
		return false;
	}
	if (fseek(m_pFile, lnOffset, SEEK_SET))
	{
		g_errorList.AddError(126, NULL,
			"Could not seek to correct place in font file");
		return false;
	}
	if (fread(*ppData, lnSize, 1, m_pFile) != 1)
	{
		g_errorList.AddError(127, NULL,
			"Could not read requested data from font file");
		return false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Read a table from the font storing its data in ppTable and its size in plnSize
	Return true if successful, otherwise false. On false, also generate an error message.
----------------------------------------------------------------------------------------------*/
int GrcFont::ReadTable(TableId ktiTableId, void * pHdr, void * pTableDir,
					   byte ** ppTable, long * plnSize)
{
	long lnOffset, lnSize;

	if (!TtfUtil::GetTableInfo(ktiTableId, pHdr, pTableDir, lnOffset, lnSize))
		goto error;
	if (!ReadData(ppTable, lnOffset, lnSize))
		goto error;
	*plnSize = lnSize;
	if (!TtfUtil::CheckTable(ktiTableId, *ppTable, lnSize))
		goto error;

	return true;

error:
	if (*ppTable)
		delete [] *ppTable;
	*ppTable = NULL;
	*plnSize = 0;
	long lnTableTag = TtfUtil::TableIdTag(ktiTableId);
	char chTableTag[5];
	strncpy(chTableTag, (char *)&lnTableTag, 4);
	chTableTag[4] = '\0';

	if (!lnTableTag)
	{
		g_errorList.AddError(128, NULL,
			"Error reading table: ",
			chTableTag);
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	  Scan the TTF table directory looking for Graphite tables.
	  Return true if one is found. False otherwise.
----------------------------------------------------------------------------------------------*/
bool GrcFont::IsGraphiteFont(void * pHdr, void * pTableDir)
{
	long lnOffset, lnSize;

	if (TtfUtil::GetTableInfo(ktiSilf, pHdr, pTableDir, lnOffset, lnSize))
		return true;
	if (TtfUtil::GetTableInfo(ktiGloc, pHdr, pTableDir, lnOffset, lnSize))
		return true;
	if (TtfUtil::GetTableInfo(ktiGlat, pHdr, pTableDir, lnOffset, lnSize))
		return true;
	if (TtfUtil::GetTableInfo(ktiFeat, pHdr, pTableDir, lnOffset, lnSize))
		return true;

	return false;
}
/*----------------------------------------------------------------------------------------------
	Scan through the cmap finding any Unicode ids that have the same Glyph id. Store
	these colliding Unicode ids in  m_vwchCollions. Also store the largest Glyph id
	in m_nMaxGlyfId.
	Return true is successful, false otherwise.
----------------------------------------------------------------------------------------------*/
int GrcFont::ScanGlyfIds(void)
{
	Assert(m_pCmap_3_10 || m_pCmap_3_1);
	if (m_nMaxGlyfId != -1) // return if called more than once
		return true;

	// determine ranges of Unicode codepoints supported by this font
	// TODO: is there a more efficient way to do this?

	unsigned int nUni;
	bool fInUsedRange = false;
	m_cnUnicode = 0;
	for (nUni = 0; nUni <= 0x0010FFFD; nUni++)
	{
		utf16 gid;
		if (nUni == 0x0000FFFE || nUni == 0x0000FFFF) // invalid
			gid = 0;
		else
			gid = GlyphFromCmap(nUni, NULL);

		if (gid != 0 && !fInUsedRange)
		{
			// Record the beginning of the range
			m_vnMinUnicode.Push(nUni);
			fInUsedRange = true;
		}
		else if (gid == 0 && fInUsedRange)
		{
			// Record the end of the range.
			m_vnLimUnicode.Push(nUni);
			fInUsedRange = false;
		}
		if (gid != 0)
			m_cnUnicode++;
	}
	if (fInUsedRange)
		m_vnLimUnicode.Push(nUni);
	Assert(m_vnLimUnicode.Size() == m_vnMinUnicode.Size());

	// create array indexed by glyf id containing unicode codepoints
	unsigned int *prgnUsed = new unsigned int[0x10000];
	if (!prgnUsed)
	{
		g_errorList.AddError(129, NULL,
			"Memory failure: could not allocate prgUsed array when scanning glyph ids");
		return false;
	}
	// initialize all elements to zero
	for (int ig = 0; ig < 0xFFFF; ig++)
		prgnUsed[ig] = 0;
	m_nMaxGlyfId = 0;

	// get glyf id for all unicode codepoints and look for collisions
	GrcFont::iterator fit;
	GrcFont::iterator fitEnd = this->End();
	for (fit = this->Begin(); fit != fitEnd; ++fit)
	{
		nUni = *fit;
		Assert(nUni != 0x0000FFFE);  // invalid
		Assert(nUni != 0x0000FFFF);  // invalid
		Assert(nUni != 0x0010FFFE);  // invalid
		Assert(nUni != 0x0010FFFF);  // invalid

		utf16 gid = GlyphFromCmap(nUni, NULL); // find glyf id
		if (gid == 0) // 0 - normally is default glyf id
		{
			Assert(false); // iterators only cover codepoints with valid glyph IDs
			continue;
		}
		m_nMaxGlyfId = max(m_nMaxGlyfId, static_cast<int>(gid)); // track largest glyf id
		if (!prgnUsed[gid]) // if this glyf id not seen yet
		{
			prgnUsed[gid] = nUni; // store current unicode id
		}
		else
		{
			// track Unicode ids that collide
			int nPrevUni = prgnUsed[gid];
			if (nPrevUni != 0x0000FFFF)
			{
				// first collision - need to store both Unicode ids in array and current id
				m_vnCollisions.Push(nPrevUni);
				// indicate one collision occurred, don't want to store array id if another
				// collision occurs; 0xFFFF is an illegal unicode value
				prgnUsed[gid] = 0x0000FFFF;
			}
			m_vnCollisions.Push(nUni); // store current unicode id always
		}
	}
	delete [] prgnUsed;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Get point data for a given glyph.
	pvnEndPt - indexes where contours (or paths) end
	Return true is successful. False otherwise.
----------------------------------------------------------------------------------------------*/
int GrcFont::GetGlyfContours(utf16 wGlyphID, Vector<int> * pvnEndPt)
{
	Assert(m_pFile);
	Assert(m_pGlyf);
	Assert(m_pLoca);
	Assert(m_pHead);

	size_t cContours;

// these checks are actually done by GlyfContourCount
// they're only needed if a specific error msg needs to be generated
#if 0
	if (TtfUtil::IsSpace(wGlyphID, m_pLoca, m_cLoca, m_pHead))
		return false;

	if (TtfUtil::IsDeepComposite(wGlyphID, m_pGlyf, m_pLoca, m_cLoca, m_pHead))
		return false;
#endif

	if (!TtfUtil::GlyfContourCount(int(wGlyphID), (void *)m_pGlyf, (void *)m_pLoca,
		size_t(m_cLoca), (void *)m_pHead, cContours))
	{
		return false;
	}

	pvnEndPt->Resize(cContours);

	if (!TtfUtil::GlyfContourEndPoints(wGlyphID, m_pGlyf, m_pLoca, m_cLoca, m_pHead,
					pvnEndPt->Begin(), cContours))
		return false;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Get point data for a given glyph.
	pvnX, pvnY - X and Y coordinates
	pvnEndPt - indexes in above where contours (or paths) end
	pvfOnCurve - flag indicating if parallel coordinate is on curve or off
	Return true is successful. False otherwise.
----------------------------------------------------------------------------------------------*/
int GrcFont::GetGlyfPts(utf16 wGlyphID, Vector<int> * pvnEndPt,
						 Vector<int> * pvnX, Vector<int> * pvnY, Vector<bool> * pvfOnCurve)
{
	Assert(m_pFile);
	Assert(m_pGlyf);
	Assert(m_pLoca);
	Assert(m_pHead);

	size_t cContours;
	int cPoints;

// these checks are actually done by GlyfContourCount
// they're only needed if a specific error msg needs to be generated
#if 0
	if (TtfUtil::IsSpace(wGlyphID, m_pLoca, m_cLoca, m_pHead))
		return false;

	if (TtfUtil::IsDeepComposite(wGlyphID, m_pGlyf, m_pLoca, m_cLoca, m_pHead))
		return false;
#endif

	if (!TtfUtil::GlyfContourCount(wGlyphID, m_pGlyf, m_pLoca, m_cLoca, m_pHead, cContours))
		return false;

	pvnEndPt->Resize(cContours);

	if (!TtfUtil::GlyfContourEndPoints(wGlyphID, m_pGlyf, m_pLoca, m_cLoca, m_pHead,
					pvnEndPt->Begin(), cContours))
		return false;

	cPoints = (*pvnEndPt)[cContours - 1] + 1;
	pvnX->Resize(cPoints);
	pvnY->Resize(cPoints);
	pvfOnCurve->Resize(cPoints);

	if (!TtfUtil::GlyfPoints(wGlyphID, m_pGlyf, m_pLoca, m_cLoca, m_pHead, pvnEndPt->Begin(),
		cContours, pvnX->Begin(), pvnY->Begin(), pvfOnCurve->Begin(), cPoints))
		return false;

	return true;
}
