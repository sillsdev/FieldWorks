/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TtfUtil.h
Responsibility: Alan Ward
Last reviewed: Not yet.

Description:
	Utility class for handling TrueType font files.
----------------------------------------------------------------------------------------------*/

#ifdef _MSC_VER
#pragma once
#endif
#ifndef TTFUTIL_H
#define TTFUTIL_H

#include <cstddef>

#include "GrPlatform.h"

// Enumeration used to specify a table in a TTF file
enum TableId
{
	ktiCmap, ktiCvt, ktiCryp, ktiHead, ktiFpgm, ktiGdir, ktiGlyf,
	ktiHdmx, ktiHhea, ktiHmtx, ktiLoca, ktiKern, ktiLtsh, ktiMaxp,
	ktiName, ktiOs2, ktiPost, ktiPrep, ktiFeat, ktiGlat, ktiGloc,
	ktiSilf, ktiSile, ktiSill,
	ktiLast /*This gives the enum length - it is not a real table*/
};

/*----------------------------------------------------------------------------------------------
	Class providing utility methods to parse a TrueType font file (TTF).
	Callling application handles all file input and memory allocation.
	Assumes minimal knowledge of TTF file format.
----------------------------------------------------------------------------------------------*/
class TtfUtil
{
public:
	static const int kMaxGlyphComponents;
	static const int kcPostNames;
	static const char * rgPostName[];
	////////////////////////////////// tools to find & check TTF tables
	static bool GetHeaderInfo(long & lOffset, long & lSize);
	static bool CheckHeader(const void * pHdr);
	static bool GetTableDirInfo(const void * pHdr, long & lOffset, long & lSize);
	static bool GetTableInfo(TableId ktiTableId, const void * pHdr, const void * pTableDir,
		long & lOffset, long & lSize);
	static bool CheckTable(TableId ktiTableId, const void * pTable, size_t lTableSize);

	////////////////////////////////// simple font wide info
	static int GlyphCount(const void * pMaxp);
	static int MaxCompositeComponentCount(const void * pMaxp);
	static int MaxCompositeLevelCount(const void * pMaxp);
	static int LocaGlyphCount(long lLocaSize, const void * pHead);
	static int DesignUnits(const void * pHead);
	static int HeadTableCheckSum(const void * pHead);
	static void HeadTableCreateTime(const void * pHead, unsigned int * pnDateBC, unsigned int * pnDateAD);
	static void HeadTableModifyTime(const void * pHead, unsigned int * pnDateBC, unsigned int * pnDateAD);
	static bool IsItalic(const void * pHead);
	static int FontAscent(const void * pOs2);
	static int FontDescent(const void * pOs2);
	static bool FontOs2Style(const void *pOs2, bool & fBold, bool & fItalic);
	static bool Get31EngFamilyInfo(const void * pName, long & lOffset, long & lSize);
	static bool Get31EngFullFontInfo(const void * pName, long & lOffset, long & lSize);
	static bool Get30EngFamilyInfo(const void * pName, long & lOffset, long & lSize);
	static bool Get30EngFullFontInfo(const void * pName, long & lOffset, long & lSize);
	static int PostLookup(const void * pPost, long lPostSize, const void * pMaxp,
		const char * pPostName);

	////////////////////////////////// utility methods helpful for name table
	static bool GetNameInfo(const void * pName, int nPlatformId, int nEncodingId,
		int nLangId, int nNameId, long & lOffset, long & lSize);
	//size_t NameTableLength(const gr::byte * pTable);
	static int GetLangsForNames(const void * pName, int nPlatformId, int nEncodingId,
		int * nameIdList, int cNameIds, short * langIdList);
	static bool SwapWString(void * pWStr, int nSize = 0);

	////////////////////////////////// cmap lookup tools
	static void * FindCmapSubtable(const void * pCmap, int nPlatformId = 3,
		int nEncodingId = 1);
	static bool CheckCmap31Subtable(const void * pCmap31);
	static int Cmap31Lookup(const void * pCmap31, int nUnicodeId);
	static int Cmap31NextCodepoint(const void *pCmap31, unsigned int nUnicodeId,
		int * pRangeKey = 0);
	static bool CheckCmap310Subtable(const void *pCmap310);
	static int Cmap310Lookup(const void * pCmap310, unsigned int uUnicodeId);
	static int Cmap310NextCodepoint(const void *pCmap310, unsigned int nUnicodeId,
		int * pRangeKey = 0);

	///////////////////////////////// horizontal metric data for a glyph
	static bool HorMetrics(int nGlyphId, const void * pHmtx, long lHmtxSize,
		const void * pHhea, int & nLsb, int & nAdvWid);

	///////////////////////////////// convert our TableId enum to standard TTF tags
	static gr::fontTableId32 TableIdTag(TableId ktiTableId);

	////////////////////////////////// primitives for loca and glyf lookup
	static long LocaLookup(int nGlyphId, const void * pLoca, long lLocaSize,
		const void * pHead);
	static void * GlyfLookup(const void * pGlyf, long lGlyfOffset);

	////////////////////////////////// primitves for simple glyph data
	static bool GlyfBox(const void * pSimpleGlyf, int & xMin, int & yMin,
		int & xMax, int & yMax);

	static int GlyfContourCount(const void * pSimpleGlyf);
	static bool GlyfContourEndPoints(const void * pSimpleGlyf, int * prgnContourEndPoint,
		int cnPointsTotal, size_t & cnPoints);
	static bool GlyfPoints(const void * pSimpleGlyf, int * prgnX, int * prgnY,
		char * prgbFlag, int cnPointsTotal, size_t & cnPoints);

	// primitive to find the glyph ids in a composite glyph
	static bool GetComponentGlyphIds(const void * pSimpleGlyf, int * prgnCompId,
		int cnCompIdTotal, int & cnCompId);
	// primitive to find the placement data for a component in a composite glyph
	static bool GetComponentPlacement(const void * pSimpleGlyf, int nCompId,
		bool fOffset, int & a, int & b);
	// primitive to find the transform data for a component in a composite glyph
	static bool GetComponentTransform(const void * pSimpleGlyf, int nCompId,
		float & flt11, float & flt12, float & flt21, float & flt22, bool & fTransOffset);

	////////////////////////////////// operate on composite or simple glyph (auto glyf lookup)
	static void * GlyfLookup(int nGlyphId, const void * pGlyf, const void * pLoca,
		long lLocaSize, const void * pHead); // primitive used by below methods

	// below are primary user methods for handling glyf data
	static bool IsSpace(int nGlyphId, const void * pLoca, long lLocaSize, const void * pHead);
	static bool IsDeepComposite(int nGlyphId, const void * pGlyf, const void * pLoca,
		long lLocaSize, const void * pHead);

	static bool GlyfBox(int nGlyphId, const void * pGlyf, const void * pLoca, long lLocaSize,
		const void * pHead, int & xMin, int & yMin, int & xMax, int & yMax);
	static bool GlyfContourCount(int nGlyphId, const void * pGlyf, const void * pLoca,
		size_t lLocaSize, const void *pHead, size_t & cnContours);
	static bool GlyfContourEndPoints(int nGlyphId, const void * pGlyf, const void * pLoca,
		long lLocaSize,	const void * pHead, int * prgnContourEndPoint, size_t & cnPoints);
	static bool GlyfPoints(int nGlyphId, const void * pGlyf, const void * pLoca,
		long lLocaSize, const void * pHead, const int * prgnContourEndPoint, int cnEndPoints,
		int * prgnX, int * prgnY, bool * prgfOnCurve, size_t & cnPoints);

	// utitily method used by high-level GlyfPoints
	static bool SimplifyFlags(char * prgbFlags, int cnPoints);
	static bool CalcAbsolutePoints(int * prgnX, int * prgnY, int cnPoints);
};

#endif
