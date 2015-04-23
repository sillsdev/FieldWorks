/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2000-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TtfUtil.cpp
Responsibility: Alan Ward
Last reviewed: Not yet.

Description:
	Implements the methods for TtfUtil class. This file should remain portable to any C++
	environment by only using standard C++ and the TTF structurs defined in Tt.h.
	AW 2011-10-21: Add support for multi-level composite glyphs. Contribution by Alexey Kryukov.
-------------------------------------------------------------------------------*//*:End Ignore*/


/***********************************************************************************************
	Include files
***********************************************************************************************/
// Headers to use for portability to non FieldWorks app
#include "TtfUtil.h"
#include "Tt.h"
#include "string.h" // NULL
#include "limits.h" // INT_MAX, INT_MIN


/***********************************************************************************************
	Forward declarations
***********************************************************************************************/
using namespace gr;

/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/
// max number of components allowed in composite glyphs
const int TtfUtil::kMaxGlyphComponents = 8;

// see bottom of file for standard Postscript glyph names stored in:
// const int TtfUtil::kcPostNames;
// const char * TtfUtil::rgPostName[];

/***********************************************************************************************
	Methods
***********************************************************************************************/

/* Note on error processing: The code guards against bad glyph ids being used to look up data
in open ended tables (loca, hmtx). If the glyph id comes from a cmap this shouldn't happen
but it seems prudent to check for user errors here. The code does assume that data obtained
from the TTF file is valid otherwise (though the CheckTable method seeks to check for
obvious problems that might accompany a change in table versions). For example an invalid
offset in the loca table which could exceed the size of the glyf table is NOT trapped.
Likewise if numberOf_LongHorMetrics in the hhea table is wrong, this will NOT be trapped,
which could cause a lookup in the hmtx table to exceed the table length. Of course, TTF tables
that are completely corrupt will cause unpredictable results. */

/* Note on composite glyphs: It is unclear how to build composite glyphs in some cases,
so this code represents Alan's best guess until test cases can be found. See notes on
the high-level GlyfPoints method. */

/*----------------------------------------------------------------------------------------------
	Get offset and size of the offset table needed to find table directory
	Return true if success, false otherwise
	lSize excludes any table directory entries
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GetHeaderInfo(long & lOffset, long & lSize)
{
	lOffset = 0;
	lSize = OFFSETTABLESIZE;
	if (OFFSETTABLESIZE != sizeof (sfnt_OffsetTable) - sizeof (sfnt_DirectoryEntry) )
		return false;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Check the offset table for expected data
	Return true if success, false otherwise
----------------------------------------------------------------------------------------------*/
bool TtfUtil::CheckHeader(const void * pHdr)
{
	const sfnt_OffsetTable * pOffsetTable = reinterpret_cast<const sfnt_OffsetTable *>(pHdr);
	if (swapls(pOffsetTable->version) != ONEFIX)
		return false;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Get offset and size of the table directory
	Return true if successful, false otherwise
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GetTableDirInfo(const void * pHdr, long & lOffset, long & lSize)
{
	const sfnt_OffsetTable * pOffsetTable = reinterpret_cast<const sfnt_OffsetTable *>(pHdr);
	lOffset = OFFSETTABLESIZE;
	lSize = swapw(pOffsetTable->numOffsets) * sizeof(sfnt_DirectoryEntry);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Get offset and size of the specified table
	Return true if successful, false otherwise. On false, offset and size will be INT_MAX
	Technically lOffset and lSize should be unsigned but its unlikely their values will
		exceed 2^31.
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GetTableInfo(TableId ktiTableId, const void * pHdr, const void * pTableDir,
						   long & lOffset, long & lSize)
{
	fontTableId32 lTableTag = TableIdTag(ktiTableId);
	if (!lTableTag)
	{
		lOffset = INT_MAX;
		lSize = INT_MAX;
		return false;
	}

	const sfnt_OffsetTable * pOffsetTable = reinterpret_cast<const sfnt_OffsetTable *>(pHdr);
	const sfnt_DirectoryEntry * pDir = reinterpret_cast<const sfnt_DirectoryEntry *>(pTableDir);

	for (int i = 0; i < swapw(pOffsetTable->numOffsets) && i < 40; i++) // 40 - safe guard
	{
		if (SFNT_SWAPTAG(pDir[i].tag) == lTableTag)
		{
			lOffset = swapl(pDir[i].offset);
			lSize = swapl(pDir[i].length);
			return true;
		}
	}

	lOffset = INT_MAX;
	lSize = INT_MAX;
	return false;
}

/*----------------------------------------------------------------------------------------------
	Check the specified table. Tests depend on the table type.
	Return true if successful, false otherwise
----------------------------------------------------------------------------------------------*/
bool TtfUtil::CheckTable(TableId ktiTableId, const void * pTable, size_t lTableSize)
{
	switch(ktiTableId)
	{
	case ktiCmap: // cmap
	{
		const sfnt_char2IndexDirectory * pCmap =
			reinterpret_cast<const sfnt_char2IndexDirectory *>(pTable);
		if (swapw(pCmap->version) == 0)
			return true;
		else
			return false;
	}

	case ktiHead: // head
	{
		const sfnt_FontHeader * pHead =
			reinterpret_cast<const sfnt_FontHeader *>(pTable);
		if (swapls(pHead->version) == ONEFIX &&
			swapl(pHead->magicNumber) == SFNT_MAGIC &&
			swapws(pHead->glyphDataFormat) == GLYPH_DATA_FORMAT &&
			(swapws(pHead->indexToLocFormat) == SHORT_INDEX_TO_LOC_FORMAT ||
				swapws(pHead->indexToLocFormat) == LONG_INDEX_TO_LOC_FORMAT) &&
			sizeof(sfnt_FontHeader) <= lTableSize)
			return true;
		else
			return false;
	}

	case ktiPost: // post
	{
		const sfnt_PostScriptInfo * pPost =
			reinterpret_cast<const sfnt_PostScriptInfo *>(pTable);
		Fixed format = swapls(pPost->version);
		if (format == 0x00010000 || format == 0x00020000 ||
			format == 0x00030000 || format == 0x00027FFF)
			return true;
		else
			return false;
	}

	case ktiHhea: // hhea
	{
		const sfnt_HorizontalHeader * pHhea =
			reinterpret_cast<const sfnt_HorizontalHeader *>(pTable);
		if (swapls(pHhea->version) == ONEFIX &&
			swapws(pHhea->metricDataFormat) == 0 &&
			sizeof (sfnt_HorizontalHeader) <= lTableSize)
			return true;
		else
			return false;
	}

	case ktiMaxp: // maxp
	{
		const sfnt_maxProfileTable * pMaxp =
			reinterpret_cast<const sfnt_maxProfileTable *>(pTable);
		if (swapls(pMaxp->version) == ONEFIX &&
			sizeof(sfnt_maxProfileTable) <= lTableSize)
			return true;
		else
			return false;
	}

	case ktiOs2: // OS/2
	{
		const sfnt_OS2 * pOs2 = reinterpret_cast<const sfnt_OS2 *>(pTable);
		if (swapw(pOs2->Version) == 0)
		{ // OS/2 table version 1 size
			if (sizeof(sfnt_OS2) - sizeof(uint32) * 2 - sizeof(int16) * 2 - sizeof(uint16) * 3 <= lTableSize)
				return true;
		}
		else if (swapw(pOs2->Version) == 1)
		{ // OS/2 table version 2 size
			if (sizeof(sfnt_OS2) - sizeof(int16) * 2 - sizeof(uint16) * 3 <= lTableSize)
				return true;
		}
		else if (swapw(pOs2->Version) == 2)
		{ // OS/2 table version 3 size
			if (sizeof(sfnt_OS2) <= lTableSize)
				return true;
		}
		else if (swapw(pOs2->Version) == 3)
		{ // OS/2 table version 4 size - version 4 changed the meaning of some fields which we don't use
			if (sizeof(sfnt_OS2) <= lTableSize)
				return true;
		}
		else
			return false;
	}

	case ktiName:
	{
		const sfnt_NamingTable * pName = reinterpret_cast<const sfnt_NamingTable *>(pTable);
		if (swapw(pName->format) == 0)
			return true;
		else
			return false;
	}

	default:
		break;
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Return the number of glyphs in the font. Should never be less than zero.
----------------------------------------------------------------------------------------------*/
int TtfUtil::GlyphCount(const void * pMaxp)
{
	const sfnt_maxProfileTable * pTable =
			reinterpret_cast<const sfnt_maxProfileTable *>(pMaxp);
	return swapw(pTable->numGlyphs);
}

/*----------------------------------------------------------------------------------------------
	Return the maximum number of components for any composite glyph in the font
----------------------------------------------------------------------------------------------*/
int TtfUtil::MaxCompositeComponentCount(const void * pMaxp)
{
	const sfnt_maxProfileTable * pTable =
			reinterpret_cast<const sfnt_maxProfileTable *>(pMaxp);
	return swapw(pTable->maxComponentElements);
}

/*----------------------------------------------------------------------------------------------
	Composite glyphs can be composed of glyphs that are themselves composites.
	This method returns the maximum number of levels like this for any glyph in the font.
	A non-composite glyph has a level of 1
----------------------------------------------------------------------------------------------*/
int TtfUtil::MaxCompositeLevelCount(const void * pMaxp)
{
	const sfnt_maxProfileTable * pTable =
			reinterpret_cast<const sfnt_maxProfileTable *>(pMaxp);
	return swapw(pTable->maxComponentDepth);
}

/*----------------------------------------------------------------------------------------------
	Return the number of glyphs in the font according to a differt source.
	Should never be less than zero. Return -1 on failure.
----------------------------------------------------------------------------------------------*/
int TtfUtil::LocaGlyphCount(long lLocaSize, const void * pHead)
{

	const sfnt_FontHeader * pTable = reinterpret_cast<const sfnt_FontHeader *>(pHead);

	if (swapws(pTable->indexToLocFormat) == SHORT_INDEX_TO_LOC_FORMAT)
	// loca entries are two bytes and have been divided by two
		return (lLocaSize >> 1) - 1;

	if (swapws(pTable->indexToLocFormat) == LONG_INDEX_TO_LOC_FORMAT)
	 // loca entries are four bytes
		return (lLocaSize >> 2) - 1;

	return -1;
}

/*----------------------------------------------------------------------------------------------
	Return the design units the font is designed with
----------------------------------------------------------------------------------------------*/
int TtfUtil::DesignUnits(const void * pHead)
{
	const sfnt_FontHeader * pTable =
			reinterpret_cast<const sfnt_FontHeader *>(pHead);

	return swapw(pTable->unitsPerEm);
}

/*----------------------------------------------------------------------------------------------
	Return the checksum from the head table, which serves as a unique identifer for the font.
----------------------------------------------------------------------------------------------*/
int TtfUtil::HeadTableCheckSum(const void * pHead)
{
	const sfnt_FontHeader * pTable =
			reinterpret_cast<const sfnt_FontHeader *>(pHead);

	return swapls(pTable->checkSumAdjustment);
}

/*----------------------------------------------------------------------------------------------
	Return the create time from the head table. This consists of a 64-bit integer, which
	we return here as two 32-bit integers.
----------------------------------------------------------------------------------------------*/
void TtfUtil::HeadTableCreateTime(const void * pHead,
	unsigned int * pnDateBC, unsigned int * pnDateAD)
{
	const sfnt_FontHeader * pTable =
			reinterpret_cast<const sfnt_FontHeader *>(pHead);

	*pnDateBC = swapl(pTable->created.bc);
	*pnDateAD = swapl(pTable->created.ad);
}

/*----------------------------------------------------------------------------------------------
	Return the modify time from the head table.This consists of a 64-bit integer, which
	we return here as two 32-bit integers.
----------------------------------------------------------------------------------------------*/
void TtfUtil::HeadTableModifyTime(const void * pHead,
	unsigned int * pnDateBC, unsigned int *pnDateAD)
{
	const sfnt_FontHeader * pTable =
			reinterpret_cast<const sfnt_FontHeader *>(pHead);

	*pnDateBC = swapl(pTable->modified.bc);
	*pnDateAD = swapl(pTable->modified.ad);
}

/*----------------------------------------------------------------------------------------------
	Return true if the font is italic.
----------------------------------------------------------------------------------------------*/
bool TtfUtil::IsItalic(const void * pHead)
{
	const sfnt_FontHeader * pTable =
			reinterpret_cast<const sfnt_FontHeader *>(pHead);

	return ((swapw(pTable->macStyle) & 0x00000002) != 0);
}

/*----------------------------------------------------------------------------------------------
	Return the ascent for the font
----------------------------------------------------------------------------------------------*/
int TtfUtil::FontAscent(const void * pOs2)
{
	const sfnt_OS2 * pTable = reinterpret_cast<const sfnt_OS2 *>(pOs2);

	return swapw(pTable->usWinAscent);
}

/*----------------------------------------------------------------------------------------------
	Return the descent for the font
----------------------------------------------------------------------------------------------*/
int TtfUtil::FontDescent(const void * pOs2)
{
	const sfnt_OS2 * pTable = reinterpret_cast<const sfnt_OS2 *>(pOs2);

	return swapw(pTable->usWinDescent);
}

/*----------------------------------------------------------------------------------------------
	Get the bold and italic style bits.
	Return true if successful. false otherwise.
	In addition to checking the OS/2 table, one could also check
		the head table's macStyle field (overridden by the OS/2 table on Win)
		the sub-family name in the name table (though this can contain oblique, dark, etc too)
----------------------------------------------------------------------------------------------*/
bool TtfUtil::FontOs2Style(const void *pOs2, bool & fBold, bool & fItalic)
{
	const sfnt_OS2 * pTable = reinterpret_cast<const sfnt_OS2 *>(pOs2);

	fBold = (swapw(pTable->usSelection) & OS2_BOLD) != 0;
	fItalic = (swapw(pTable->usSelection) & OS2_ITALIC) != 0;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Method for searching name table.
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GetNameInfo(const void * pName, int nPlatformId, int nEncodingId,
		int nLangId, int nNameId, long & lOffset, long & lSize)
{
	lOffset = INT_MIN;
	lSize = INT_MIN;

	const sfnt_NamingTable * pTable = reinterpret_cast<const sfnt_NamingTable *>(pName);
	uint16 cRecord = swapw(pTable->count);
	uint16 nRecordOffset = swapw(pTable->stringOffset);
	const sfnt_NameRecord * pRecord = reinterpret_cast<const sfnt_NameRecord *>(pTable + 1);

	for (int i = 0; i < cRecord; i++)
	{
		if (swapw(pRecord->platformID) == nPlatformId &&
			swapw(pRecord->specificID) == nEncodingId &&
			swapw(pRecord->languageID) == nLangId &&
			swapw(pRecord->nameID) == nNameId)
		{
			lOffset = swapw(pRecord->offset) + nRecordOffset;
			lSize = swapw(pRecord->length);
			return true;
		}
		pRecord++;
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Return all the lang-IDs that have data for the given name-IDs. Assume that there is room
	in the return array (langIdList) for 128 items. The purpose of this method is to return
	a list of all possible lang-IDs.
----------------------------------------------------------------------------------------------*/
int TtfUtil::GetLangsForNames(const void * pName, int nPlatformId, int nEncodingId,
		int * nameIdList, int cNameIds, short * langIdList)
{
	const sfnt_NamingTable * pTable = reinterpret_cast<const sfnt_NamingTable *>(pName);
	uint16 cRecord = swapw(pTable->count);
	uint16 nRecordOffset = swapw(pTable->stringOffset);
	const sfnt_NameRecord * pRecord = reinterpret_cast<const sfnt_NameRecord *>(pTable + 1);

	int cLangIds = 0;
	for (int i = 0; i < cRecord; i++)
	{
		if (swapw(pRecord->platformID) == nPlatformId &&
			swapw(pRecord->specificID) == nEncodingId)
		{
			bool fNameFound = false;
			int nLangId = swapw(pRecord->languageID);
			int nNameId = swapw(pRecord->nameID);
			for (int i = 0; i < cNameIds; i++)
			{
				if (nNameId == nameIdList[i])
				{
					fNameFound = true;
					break;
				}
			}
			if (fNameFound)
			{
				// Add it if it's not there.
				int ilang;
				for (ilang = 0; ilang < cLangIds; ilang++)
					if (langIdList[ilang] == nLangId)
						break;
				if (ilang >= cLangIds)
				{
					langIdList[cLangIds] = nLangId;
					cLangIds++;
				}
				if (cLangIds == 128)
					return cLangIds;
			}
		}
		pRecord++;
	}

	return cLangIds;
}

/*----------------------------------------------------------------------------------------------
	Get the offset and size of the font family name in English for the MS Platform with Unicode
	writing system. The offset is within the pName data. The string is double byte with MSB first.
----------------------------------------------------------------------------------------------*/
bool TtfUtil::Get31EngFamilyInfo(const void * pName, long & lOffset, long & lSize)
{
	return GetNameInfo(pName, plat_MS, 1, 1033, name_Family, lOffset, lSize);
}

/*----------------------------------------------------------------------------------------------
	Get the offset and size of the full font name in English for the MS Platform with Unicode
	writing system. The offset is within the pName data. The string is double byte with MSB first.
----------------------------------------------------------------------------------------------*/
bool TtfUtil::Get31EngFullFontInfo(const void * pName, long & lOffset, long & lSize)
{
	return GetNameInfo(pName, plat_MS, 1, 1033, name_FullName, lOffset, lSize);
}

/*----------------------------------------------------------------------------------------------
	Get the offset and size of the font family name in English for the MS Platform with Symbol
	writing system. The offset is within the pName data. The string is double byte with MSB first.
----------------------------------------------------------------------------------------------*/
bool TtfUtil::Get30EngFamilyInfo(const void * pName, long & lOffset, long & lSize)
{
	return GetNameInfo(pName, plat_MS, 0, 1033, name_Family, lOffset, lSize);
}

/*----------------------------------------------------------------------------------------------
	Get the offset and size of the full font name in English for the MS Platform with Symbol
	writing system. The offset is within the pName data. The string is double byte with MSB first.
----------------------------------------------------------------------------------------------*/
bool TtfUtil::Get30EngFullFontInfo(const void * pName, long & lOffset, long & lSize)
{
	return GetNameInfo(pName, plat_MS, 0, 1033, name_FullName, lOffset, lSize);
}

/*----------------------------------------------------------------------------------------------
	Return the Glyph ID for a given Postscript name. This method finds the first glyph which
	matches the requested Postscript name. Ideally every glyph should have a unique Postscript
	name (except for special names such as .notdef), but this is not always true.
	On failure return value less than zero.
	   -1 - table search failed
	   -2 - format 3 table (no Postscript glyph info)
	   -3 - other failures
----------------------------------------------------------------------------------------------*/
int TtfUtil::PostLookup(const void * pPost, long lPostSize, const void * pMaxp,
						const char * pPostName)
{
	const sfnt_PostScriptInfo * pTable = reinterpret_cast<const sfnt_PostScriptInfo *>(pPost);
	Fixed format = swapls(pTable->version);

	if (format == 0x00030000)
	{ // format 3 - no Postscript glyph info in font
		return -2;
	}

	// search for given Postscript name among the standard names
	int iPostName = -1; // index in standard names
	for (int i = 0; i < TtfUtil::kcPostNames; i++)
	{
		if (!strcmp(pPostName, TtfUtil::rgPostName[i]))
		{
			iPostName = i;
			break;
		}
	}

	if (format == 0x00010000)
	{ // format 1 - use standard Postscript names
		return iPostName;
	}

	if (format == 0x00027FFF)
	{ // format 2.5 (TTF spec is unclear how a fraction is encoded and I can find no examples.)
		if (iPostName == -1)
			return -1;

		int cnGlyphs = TtfUtil::GlyphCount(pMaxp);
		for (int nGlyphId = 0; nGlyphId < cnGlyphs && nGlyphId < TtfUtil::kcPostNames;
				nGlyphId++)
		{ // glyphNameIndex25 contains bytes so no byte swapping needed
		  // search for first glyph id that uses the standard name
			if (nGlyphId + pTable->glyphNameIndex25[nGlyphId] == iPostName)
				return nGlyphId;
		}
	}

	if (format == 0x00020000)
	{ // format 2
		int cnGlyphs = swapw(pTable->numberGlyphs);

		if (iPostName != -1)
		{ // did match a standard name, look for first glyph id mapped to that name
			for (int nGlyphId = 0; nGlyphId < cnGlyphs; nGlyphId++)
			{
				if (swapw(pTable->glyphNameIndex[nGlyphId]) == iPostName)
					return nGlyphId;
			}
			return -1; // no glyph with this standard name
		}

		else
		{ // did not match a standard name, search font specific names
			int nStrSizeGoal = strlen(pPostName);
			const char * pFirstGlyphName = reinterpret_cast<const char *>(
				&pTable->glyphNameIndex[0] + cnGlyphs);
			const char * pGlyphName = pFirstGlyphName;
			int iInNames = 0; // index in font specific names
			bool fFound = false;
			while (pGlyphName - reinterpret_cast<const char *>(pTable) < lPostSize && !fFound)
			{ // search Pascal strings for first matching name
				int nStringSize = static_cast<int>(*pGlyphName);
				if (nStrSizeGoal != nStringSize ||
					strncmp(pGlyphName + 1, pPostName, nStringSize))
				{ // did not match
					++iInNames;
					pGlyphName += nStringSize + 1;
				}
				else
				{ // did match
					fFound = true;
				}
			}
			if (!fFound)
				return -1; // no font specific name matches request

			iInNames += TtfUtil::kcPostNames;
			for (int nGlyphId = 0; nGlyphId < cnGlyphs; nGlyphId++)
			{ // search for first glyph id that maps to the found string index
				if (swapw(pTable->glyphNameIndex[nGlyphId]) == iInNames)
					return nGlyphId;
			}
			return -1; // no glyph mapped to this index (very strange)
		}
	}

	return -3;
}

/*----------------------------------------------------------------------------------------------
	Convert a Unicode character string from big endian (MSB first, Motorola) format to little
	endian (LSB first, Intel) format.
	nSize is the number of Unicode characters in the string. It should not include any
	terminating null. If nSize is 0, it is assumed the string is null terminated. nSize
	defaults to 0.
	Return true if successful, false otherwise.
----------------------------------------------------------------------------------------------*/
bool TtfUtil::SwapWString(void * pWStr, int nSize /* = 0 */)
{
	if (pWStr == NULL)
		return false;

	utf16 * pStr = (utf16 *)pWStr;

	if (nSize == 0)
		nSize = utf16len(pStr);
	if (nSize < 0)
		return false;
	if (nSize == 0)
		return true;

	for (int i = 0; i < nSize; i++)
	{ // swap the wide characters in the string
		pStr[i] = (utf16)swapw((uint16)pStr[i]);
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the left-side bearing and and advance width based on the given tables and Glyph ID
	Return true if successful, false otherwise. On false, one or both value could be INT_MIN
----------------------------------------------------------------------------------------------*/
bool TtfUtil::HorMetrics(int nGlyphId, const void * pHmtx, long lHmtxSize, const void * pHhea,
						 int & nLsb, int & nAdvWid)
{
	if (nGlyphId < 0)
	{ // guard against bad glyph id, also see below
		nAdvWid = INT_MIN;
		nLsb = INT_MIN;
		return false;
	}

	const sfnt_HorizontalMetrics * phmtx =
		reinterpret_cast<const sfnt_HorizontalMetrics *>(pHmtx);

	const sfnt_HorizontalHeader * phhea =
		reinterpret_cast<const sfnt_HorizontalHeader *>(pHhea);

	int cLongHorMetrics = swapw(phhea->numberOf_LongHorMetrics);
	if (nGlyphId < cLongHorMetrics)
	{	// glyph id is acceptable
		nAdvWid = swapw(phmtx[nGlyphId].advanceWidth);
		nLsb = swapws(phmtx[nGlyphId].leftSideBearing);
	}
	else
	{
		nAdvWid = swapw(phmtx[cLongHorMetrics - 1].advanceWidth);

		// guard against bad glyph id
		long lLsbOffset = sizeof(sfnt_HorizontalMetrics) * cLongHorMetrics +
			sizeof(int16) * (nGlyphId - cLongHorMetrics); // offset in bytes
		if (lLsbOffset + 1 >= lHmtxSize) // + 1 because entries are two bytes wide
		{
			nLsb = INT_MIN;
			return false;
		}
		const int16 * pLsb = reinterpret_cast<const int16 *>(phmtx) +
			lLsbOffset / sizeof(int16);
		nLsb = swapws(*pLsb);
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Return standard TTF tag for the given TableId enumeration constant
	If requested ktiTableId doesn't exist, return 0;
----------------------------------------------------------------------------------------------*/
fontTableId32 TtfUtil::TableIdTag(TableId ktiTableId)
{
	switch(ktiTableId)
	{
	case ktiCmap:	return tag_CharToIndexMap;
	case ktiCvt:	return tag_ControlValue;
	case ktiCryp:	return tag_Encryption;
	case ktiHead: 	return tag_FontHeader;
	case ktiFpgm:	return tag_FontProgram;
	case ktiGdir:	return tag_GlyphDirectory;
	case ktiGlyf:	return tag_GlyphData;
	case ktiHdmx:	return tag_HoriDeviceMetrics;
	case ktiHhea:	return tag_HoriHeader;
	case ktiHmtx:	return tag_HorizontalMetrics;
	case ktiLoca:	return tag_IndexToLoc;
	case ktiKern:	return tag_Kerning;
	case ktiLtsh:	return tag_LSTH;
	case ktiMaxp:	return tag_MaxProfile;
	case ktiName:	return tag_NamingTable;
	case ktiOs2:	return tag_OS_2;
	case ktiPost:	return tag_Postscript;
	case ktiPrep:	return tag_PreProgram;
	case ktiFeat:	return tag_Feat;
	case ktiGlat:	return tag_Glat;
	case ktiGloc:	return tag_Gloc;
	case ktiSilf:	return tag_Silf;
	case ktiSile:	return tag_Sile;
	case ktiSill:	return tag_Sill;
	default:		return 0;
	}
}
/*----------------------------------------------------------------------------------------------
	Return a pointer to the requested cmap subtable. By default find the Microsoft Unicode
	subtable. Pass nEncoding as -1 to find first table that matches only nPlatformId.
	Return NULL if the subtable cannot be found.
----------------------------------------------------------------------------------------------*/
void * TtfUtil::FindCmapSubtable(const void * pCmap,
								 int nPlatformId, /* =3 */
								 int nEncodingId) /* = 1 */
{
	const sfnt_char2IndexDirectory * pTable =
		reinterpret_cast<const sfnt_char2IndexDirectory *>(pCmap);

	uint16 csuPlatforms = swapw(pTable->numTables);
	for (int i = 0; i < csuPlatforms; i++)
	{
		if (swapw(pTable->platform[i].platformID) == nPlatformId &&
			(nEncodingId == -1 || swapw(pTable->platform[i].specificID) == nEncodingId))
		{
			const void * pRtn = reinterpret_cast<const uint8 *>(pCmap) +
				swapl(pTable->platform[i].offset);
			return const_cast<void *>(pRtn);
		}
	}

	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Check the Microsoft Unicode subtable for expected values
----------------------------------------------------------------------------------------------*/
bool TtfUtil::CheckCmap31Subtable(const void * pCmap31)
{
	const sfnt_mappingTable * pTable = reinterpret_cast<const sfnt_mappingTable *>(pCmap31);
	// Bob H says ome freeware TT fonts have version 1 (eg, CALIGULA.TTF)
	// so don't check subtable version. 21 Mar 2002 spec changes version to language.
	if (// swapw(pTable->version) == 0 &&
		swapw(pTable->format) == 4)
		return true;

	return false;
}

/*----------------------------------------------------------------------------------------------
	Return the Glyph ID for the given Unicode ID in the Microsoft Unicode subtable.
	(Actually this code only depends on subtable being format 4.)
	Return 0 if the Unicode ID is not in the subtable.
----------------------------------------------------------------------------------------------*/
int TtfUtil::Cmap31Lookup(const void * pCmap31, int nUnicodeId)
{
	const sfnt_Cmap4 * pTable = reinterpret_cast<const sfnt_Cmap4 *>(pCmap31);

	uint16 nSeg = swapw(pTable->segCountX2) >> 1;

	uint16 n;
	const uint16 * pLeft, * pMid;
	uint16 cMid, chStart, chEnd;

	// Binary search of the endCode[] array
	pLeft = &(pTable->endCode[0]);
	n = nSeg;
	while (n > 0)
	{
		cMid = n >> 1;           // Pick an element in the middle
		pMid = pLeft + cMid;
		chEnd = swapw(*pMid);
		if (nUnicodeId <= chEnd)
		{
			if (cMid == 0 || nUnicodeId > swapw(pMid[-1]))
				break;          // Must be this seg or none!
			n = cMid;            // Continue on left side, omitting mid point
		}
		else
		{
			pLeft = pMid + 1;    // Continue on right side, omitting mid point
			n -= (cMid + 1);
		}
	}

	if (!n)
		return 0;

	// Ok, we're down to one segment and pMid points to the endCode element
	// Either this is it or none is.

	chStart = swapw(*(pMid += nSeg + 1));
	if (chEnd >= nUnicodeId && nUnicodeId >= chStart)
	{
		// Found correct segment. Find Glyph Id
		int16 idDelta = swapws(*(pMid += nSeg));
		uint16 idRangeOffset = swapw(*(pMid += nSeg));

		if (idRangeOffset == 0)
			return (uint16)(idDelta + nUnicodeId); // must use modulus 2^16

		// Look up value in glyphIdArray
		int nGlyphId = swapw(*(pMid + (nUnicodeId - chStart) + (idRangeOffset >> 1)));
		// If this value is 0, return 0. Else add the idDelta
		return nGlyphId ? nGlyphId + idDelta : 0;
	}

	return 0;
}

/*----------------------------------------------------------------------------------------------
	Return the next Unicode value in the cmap. Pass 0 to obtain the first item.
	Returns 0xFFFF as the last item.
	pRangeKey is an optional key that is used to optimize the search; its value is the range
	in which the character is found.
----------------------------------------------------------------------------------------------*/
int TtfUtil::Cmap31NextCodepoint(const void *pCmap31, unsigned int nUnicodeId, int * pRangeKey)
{
	const sfnt_Cmap4 * pTable = reinterpret_cast<const sfnt_Cmap4 *>(pCmap31);

	uint16 nRange = swapw(pTable->segCountX2) >> 1;

	uint32 nUnicodePrev = (uint32)nUnicodeId;

	const uint16 * pStartCode = &(pTable->endCode[0])
		+ nRange // length of end code array
		+ 1;   // reserved word

	if (nUnicodePrev == 0)
	{
		// return the first codepoint.
		if (pRangeKey)
			*pRangeKey = 0;
		return swapw(pStartCode[0]);
	}
	else if (nUnicodePrev >= 0xFFFF)
	{
		if (pRangeKey)
			*pRangeKey = nRange - 1;
		return 0xFFFF;
	}

	int iRange = (pRangeKey) ? *pRangeKey : 0;
	// Just in case we have a bad key:
	while (iRange > 0 && swapw(pStartCode[iRange]) > nUnicodePrev)
		iRange--;
	while (swapw(pTable->endCode[iRange]) < nUnicodePrev)
		iRange++;

	// Now iRange is the range containing nUnicodePrev.
	unsigned int nStartCode = swapw(pStartCode[iRange]);
	unsigned int nEndCode = swapw(pTable->endCode[iRange]);

	if (nStartCode > nUnicodePrev)
		// Oops, nUnicodePrev is not in the cmap! Adjust so we get a reasonable
		// answer this time around.
		nUnicodePrev = nStartCode - 1;

	if (nEndCode > nUnicodePrev)
	{
		// Next is in the same range; it is the next successive codepoint.
		if (pRangeKey)
			*pRangeKey = iRange;
		return nUnicodePrev + 1;
	}

	// Otherwise the next codepoint is the first one in the next range.
	// There is guaranteed to be a next range because there must be one that
	// ends with 0xFFFF.
	if (pRangeKey)
		*pRangeKey = iRange + 1;
	return swapw(pStartCode[iRange + 1]);
}

/*----------------------------------------------------------------------------------------------
	Check the Microsoft UCS-4 subtable for expected values
----------------------------------------------------------------------------------------------*/
bool TtfUtil::CheckCmap310Subtable(const void *pCmap310)
{
	const sfnt_mappingTable * pTable = reinterpret_cast<const sfnt_mappingTable *>(pCmap310);
	if (swapw(pTable->format) == 12)
		return true;

	return false;
}

/*----------------------------------------------------------------------------------------------
	Return the Glyph ID for the given Unicode ID in the Microsoft UCS-4 subtable.
	(Actually this code only depends on subtable being format 12.)
	Return 0 if the Unicode ID is not in the subtable.
----------------------------------------------------------------------------------------------*/
int TtfUtil::Cmap310Lookup(const void * pCmap310, unsigned int uUnicodeId)
{
	const sfnt_Cmap12 * pTable = reinterpret_cast<const sfnt_Cmap12 *>(pCmap310);

	//uint32 uLength = swapl(pTable->length); //could use to test for premature end of table
	uint32 ucGroups = swapl(pTable->nGroups);

	for (unsigned int i = 0; i < ucGroups; i++)
	{
		uint32 uStartCode = swapl(pTable->groupArray[i].startCharCode);
		uint32 uEndCode = swapl(pTable->groupArray[i].endCharCode);
		if (uUnicodeId >= uStartCode && uUnicodeId <= uEndCode)
		{
			uint32 uDiff = uUnicodeId - uStartCode;
			uint32 uStartGid = swapl(pTable->groupArray[i].startGlyphID);
			return uStartGid + uDiff;
		}
	}

	return 0;
}

/*----------------------------------------------------------------------------------------------
	Return the next Unicode value in the cmap. Pass 0 to obtain the first item.
	Returns 0x10FFFF as the last item.
	pRangeKey is an optional key that is used to optimize the search; its value is the range
	in which the character is found.
----------------------------------------------------------------------------------------------*/
int TtfUtil::Cmap310NextCodepoint(const void *pCmap310, unsigned int nUnicodeId, int * pRangeKey)
{
	const sfnt_Cmap12 * pTable = reinterpret_cast<const sfnt_Cmap12 *>(pCmap310);

	int nRange = swapl(pTable->nGroups);

	uint32 nUnicodePrev = (uint32)nUnicodeId;

	if (nUnicodePrev == 0)
	{
		// return the first codepoint.
		if (pRangeKey)
			*pRangeKey = 0;
		return swapl(pTable->groupArray[0].startCharCode);
	}
	else if (nUnicodePrev >= 0x10FFFF)
	{
		if (pRangeKey)
			*pRangeKey = nRange;
		return 0x10FFFF;
	}

	int iRange = (pRangeKey) ? *pRangeKey : 0;
	// Just in case we have a bad key:
	while (iRange > 0 && swapl(pTable->groupArray[iRange].startCharCode) > nUnicodePrev)
		iRange--;
	while (swapl(pTable->groupArray[iRange].endCharCode) < nUnicodePrev)
		iRange++;

	// Now iRange is the range containing nUnicodePrev.

	unsigned int nStartCode = swapl(pTable->groupArray[iRange].startCharCode);
	unsigned int nEndCode = swapl(pTable->groupArray[iRange].endCharCode);

	if (nStartCode > nUnicodePrev)
		// Oops, nUnicodePrev is not in the cmap! Adjust so we get a reasonable
		// answer this time around.
		nUnicodePrev = nStartCode - 1;

	if (nEndCode > nUnicodePrev)
	{
		// Next is in the same range; it is the next successive codepoint.
		if (pRangeKey)
			*pRangeKey = iRange;
		return nUnicodePrev + 1;
	}

	// Otherwise the next codepoint is the first one in the next range, or 10FFFF if we're done.
	if (pRangeKey)
		*pRangeKey = iRange + 1;
	return (iRange + 1 >= nRange) ? 0x10FFFF : swapl(pTable->groupArray[iRange + 1].startCharCode);
}

/*----------------------------------------------------------------------------------------------
	Return the offset stored in the loca table for the given Glyph ID.
	(This offset is into the glyf table.)
	Return -1 if the lookup failed.
	Technically this method should return an unsigned long but it is unlikely the offset will
		exceed 2^31.
----------------------------------------------------------------------------------------------*/
long TtfUtil::LocaLookup(int nGlyphId, const void * pLoca, long lLocaSize,
								  const void * pHead)
{
	if (nGlyphId < 0)
		return -1;

	const sfnt_FontHeader * pTable = reinterpret_cast<const sfnt_FontHeader *>(pHead);

	// CheckTable verifies the indexToLocFormat is valid
	if (swapws(pTable->indexToLocFormat) == SHORT_INDEX_TO_LOC_FORMAT)
	{ // loca entries are two bytes and have been divided by two
		if (nGlyphId < (lLocaSize >> 1) - 1)
		{
			const uint16 * pTable = reinterpret_cast<const uint16 *>(pLoca);
			return (swapw(pTable[nGlyphId]) << 1);
		}
	}

	if (swapws(pTable->indexToLocFormat) == LONG_INDEX_TO_LOC_FORMAT)
	{ // loca entries are four bytes
		if (nGlyphId < (lLocaSize >> 2) - 1)
		{
			const uint32 * pTable = reinterpret_cast<const uint32 *>(pLoca);
			return swapl(pTable[nGlyphId]);
		}
	}

	// only get here if glyph id was bad
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Return a pointer into the glyf table based on the given offset (from LocaLookup).
	Return NULL on error.
----------------------------------------------------------------------------------------------*/
void * TtfUtil::GlyfLookup(const void * pGlyf, long nGlyfOffset)
{
	if (nGlyfOffset < 0)
		return NULL; // catches -1 offset returned by LocaLookup on error

	const uint8 * pByte = reinterpret_cast<const uint8 *>(pGlyf);
	return const_cast<uint8 *>(pByte + nGlyfOffset);
}

/*----------------------------------------------------------------------------------------------
	Get the bounding box coordinates for a simple glyf entry (non-composite)
	Return true if successful, false otherwise
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GlyfBox(const void * pSimpleGlyf, int & xMin, int & yMin,
					  int & xMax, int & yMax)
{
	const sfnt_GlyfHdr * pGlyph = reinterpret_cast<const sfnt_GlyfHdr *>(pSimpleGlyf);

	xMin = swapws(pGlyph->xMin);
	yMin = swapws(pGlyph->yMin);
	xMax = swapws(pGlyph->xMax);
	yMax = swapws(pGlyph->yMax);

	return true;
}

/*----------------------------------------------------------------------------------------------
	Return the number of contours for a simple glyf entry (non-composite)
	Returning -1 means this is a composite glyph
----------------------------------------------------------------------------------------------*/
int TtfUtil::GlyfContourCount(const void * pSimpleGlyf)
{
	const sfnt_GlyfHdr * pGlyph = reinterpret_cast<const sfnt_GlyfHdr *>(pSimpleGlyf);
	return swapws(pGlyph->numberOfContours); // -1 means composite glyph
}

/*----------------------------------------------------------------------------------------------
	Get the point numbers for the end points of the glyph contours for a simple
	glyf entry (non-composite).
	cnPointsTotal - count of contours from GlyfContourCount(); (same as number of end points)
	prgnContourEndPoints - should point to a buffer large enough to hold cnPoints integers
	cnPoints - count of points placed in above range
	Return true if successful, false otherwise.
		False could indicate a multi-level composite glyphs.
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GlyfContourEndPoints(const void * pSimpleGlyf, int * prgnContourEndPoint,
	int cnPointsTotal, size_t & cnPoints)
{
	const sfnt_GlyfHdr * pGlyph = reinterpret_cast<const sfnt_GlyfHdr *>(pSimpleGlyf);

	int cContours = swapws(pGlyph->numberOfContours);
	if (cContours < 0)
		return false; // this method isn't supposed handle composite glyphs

	for (int i = 0; i < cContours && i < cnPointsTotal; i++)
	{
		prgnContourEndPoint[i] = swapw(pGlyph->endPtsOfContours[i]);
	}

	cnPoints = cContours;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the points for a simple glyf entry (non-composite)
	cnPointsTotal - count of points from largest end point obtained from GlyfContourEndPoints
	prgnX & prgnY - should point to buffers large enough to hold cnPointsTotal integers
		The ranges are parallel so that coordinates for point(n) are found at offset n in both
		ranges. This is raw point data with relative coordinates.
	prgbFlag - should point to a buffer a large enough to hold cnPointsTotal bytes
		This range is parallel to the prgnX & prgnY
	cnPoints - count of points placed in above ranges
	Return true if successful, false otherwise.
		False could indicate a composite glyph
	TODO: implement
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GlyfPoints(const void * pSimpleGlyf, int * prgnX, int * prgnY,
	char * prgbFlag, int cnPointsTotal, size_t & cnPoints)
{
	const sfnt_GlyfHdr * pGlyph = reinterpret_cast<const sfnt_GlyfHdr *>(pSimpleGlyf);
	int cContours = swapws(pGlyph->numberOfContours);
	// return false for composite glyph
	if (cContours <= 0)
		return false;
	int cPts = swapw(pGlyph->endPtsOfContours[cContours - 1]) + 1;
	if (cPts > cnPointsTotal)
		return false;

	// skip over bounding box data & point to byte count of instructions (hints)
	const uint8 * pbGlyph = reinterpret_cast<const uint8 *>
												(&pGlyph->endPtsOfContours[cContours]);
	// skip over hints & point to first flag
	int cbHints = swapw(*(uint16 *)pbGlyph);
	pbGlyph += sizeof(uint16);
	pbGlyph += cbHints;

	// load flags & point to first x coordinate
	int iFlag = 0;
	while (iFlag < cPts)
	{
		if (!(*pbGlyph & REPEAT_FLAGS))
		{ // flag isn't repeated
			prgbFlag[iFlag] = (char)*pbGlyph;
			pbGlyph++;
			iFlag++;
		}
		else
		{ // flag is repeated; count specified by next byte
			char chFlag = (char)*pbGlyph;
			pbGlyph++;
			int cFlags = (int)*pbGlyph;
			pbGlyph++;
			prgbFlag[iFlag] = chFlag;
			iFlag++;
			for (int i = 0; i < cFlags; i++)
			{
				prgbFlag[iFlag + i] = chFlag;
			}
			iFlag += cFlags;
		}
	}
	if (iFlag != cPts)
		return false;

	// load x coordinates
	iFlag = 0;
	while (iFlag < cPts)
	{
		if (prgbFlag[iFlag] & XSHORT)
		{
			prgnX[iFlag] = *pbGlyph;
			if (!(prgbFlag[iFlag] & SHORT_X_IS_POS))
			{
				prgnX[iFlag] = -prgnX[iFlag];
			}
			pbGlyph++;
		}
		else
		{
			if (prgbFlag[iFlag] & NEXT_X_IS_ZERO)
			{
				prgnX[iFlag] = 0;
				// do NOT increment pbGlyph
			}
			else
			{
				prgnX[iFlag] = swapws(*(int16 *)pbGlyph);
				pbGlyph += sizeof(int16);
			}
		}
		iFlag++;
	}

	// load y coordinates
	iFlag = 0;
	while (iFlag < cPts)
	{
		if (prgbFlag[iFlag] & YSHORT)
		{
			prgnY[iFlag] = *pbGlyph;
			if (!(prgbFlag[iFlag] & SHORT_Y_IS_POS))
			{
				prgnY[iFlag] = -prgnY[iFlag];
			}
			pbGlyph++;
		}
		else
		{
			if (prgbFlag[iFlag] & NEXT_Y_IS_ZERO)
			{
				prgnY[iFlag] = 0;
				// do NOT increment pbGlyph
			}
			else
			{
				prgnY[iFlag] = swapws(*(int16 *)pbGlyph);
				pbGlyph += sizeof(int16);
			}
		}
		iFlag++;
	}

	cnPoints = cPts;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Fill prgnCompId with the component Glyph IDs from pSimpleGlyf.
	Client must allocate space before calling.
	pSimpleGlyf - assumed to point to a composite glyph
	cCompIdTotal - the number of elements in prgnCompId
	cCompId  - the total number of Glyph IDs stored in prgnCompId
	Return true if successful, false otherwise
		False could indicate a non-composite glyph or the input array was not big enough
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GetComponentGlyphIds(const void * pSimpleGlyf, int * prgnCompId,
									int cnCompIdTotal, int & cnCompId)
{
	if (GlyfContourCount(pSimpleGlyf) >= 0)
		return false;

	const sfnt_GlyfHdr * pGlyph = reinterpret_cast<const sfnt_GlyfHdr *>(pSimpleGlyf);
	// for a composite glyph, the special data begins here
	const uint8 * pbGlyph = reinterpret_cast<const uint8 *>(&pGlyph->endPtsOfContours[0]);

	uint16 GlyphFlags;
	int iCurrentComp = 0;
	do
	{
		GlyphFlags = swapw(*((uint16 *)pbGlyph));
		pbGlyph += sizeof(uint16);
		prgnCompId[iCurrentComp++] = swapw(*((uint16 *)pbGlyph));
		pbGlyph += sizeof(uint16);
		if (iCurrentComp >= cnCompIdTotal)
			return false;
		int nOffset = 0;
		nOffset += GlyphFlags & ARG_1_AND_2_ARE_WORDS ? 4 : 2;
		nOffset += GlyphFlags & WE_HAVE_A_SCALE ? 2 : 0;
		nOffset += GlyphFlags & WE_HAVE_AN_X_AND_Y_SCALE  ? 4 : 0;
		nOffset += GlyphFlags & WE_HAVE_A_TWO_BY_TWO  ? 8 :  0;
		pbGlyph += nOffset;
	} while (GlyphFlags & MORE_COMPONENTS);

	cnCompId = iCurrentComp;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Return info on how a component glyph is to be placed
	pSimpleGlyph - assumed to point to a composite glyph
	nCompId - glyph id for component of interest
	bOffset - if true, a & b are the x & y offsets for this component
			  if false, b is the point on this component that is attaching to point a on the
				preceding glyph
	Return true if successful, false otherwise
		False could indicate a non-composite glyph or that component wasn't found
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GetComponentPlacement(const void * pSimpleGlyf, int nCompId,
									bool fOffset, int & a, int & b)
{
	if (GlyfContourCount(pSimpleGlyf) >= 0)
		return false;

	const sfnt_GlyfHdr * pGlyph = reinterpret_cast<const sfnt_GlyfHdr *>(pSimpleGlyf);
	// for a composite glyph, the special data begins here
	const uint8 * pbGlyph = reinterpret_cast<const uint8 *>(&pGlyph->endPtsOfContours[0]);

	uint16 GlyphFlags;
	do
	{
		GlyphFlags = swapw(*((uint16 *)pbGlyph));
		pbGlyph += sizeof(uint16);
		if (swapw(*((uint16 *)pbGlyph)) == nCompId)
		{
			pbGlyph += sizeof(uint16); // skip over glyph id of component
			fOffset = (GlyphFlags & ARGS_ARE_XY_VALUES) == ARGS_ARE_XY_VALUES;

			if (GlyphFlags & ARG_1_AND_2_ARE_WORDS)
			{
				a = swapws(*(int16 *)pbGlyph);
				pbGlyph += sizeof(int16);
				b = swapws(*(int16 *)pbGlyph);
				pbGlyph += sizeof(int16);
			}
			else
			{ // args are signed bytes
				a = *pbGlyph++;
				b = *pbGlyph++;
			}
			return true;
		}
		pbGlyph += sizeof(uint16); // skip over glyph id of component
		int nOffset = 0;
		nOffset += GlyphFlags & ARG_1_AND_2_ARE_WORDS ? 4 : 2;
		nOffset += GlyphFlags & WE_HAVE_A_SCALE ? 2 : 0;
		nOffset += GlyphFlags & WE_HAVE_AN_X_AND_Y_SCALE  ? 4 : 0;
		nOffset += GlyphFlags & WE_HAVE_A_TWO_BY_TWO  ? 8 :  0;
		pbGlyph += nOffset;
	} while (GlyphFlags & MORE_COMPONENTS);

	// didn't find requested component
	fOffset = true;
	a = 0;
	b = 0;
	return false;
}

/*----------------------------------------------------------------------------------------------
	Return info on how a component glyph is to be transformed
	pSimpleGlyph - assumed to point to a composite glyph
	nCompId - glyph id for component of interest
	flt11, flt11, flt11, flt11 - a 2x2 matrix giving the transform
	bTransOffset - whether to transform the offset from above method
		The spec is unclear about the meaning of this flag
		Currently - initialize to true for MS rasterizer and false for Mac rasterizer, then
			on return it will indicate whether transform should apply to offset (MSDN CD 10/99)
	Return true if successful, false otherwise
		False could indicate a non-composite glyph or that component wasn't found
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GetComponentTransform(const void * pSimpleGlyf, int nCompId,
									float & flt11, float & flt12, float & flt21, float & flt22,
									bool & fTransOffset)
{
	if (GlyfContourCount(pSimpleGlyf) >= 0)
		return false;

	const sfnt_GlyfHdr * pGlyph = reinterpret_cast<const sfnt_GlyfHdr *>(pSimpleGlyf);
	// for a composite glyph, the special data begins here
	const uint8 * pbGlyph = reinterpret_cast<const uint8 *>(&pGlyph->endPtsOfContours[0]);

	uint16 GlyphFlags;
	do
	{
		GlyphFlags = swapw(*((uint16 *)pbGlyph));
		pbGlyph += sizeof(uint16);
		if (swapw(*((uint16 *)pbGlyph)) == nCompId)
		{
			pbGlyph += sizeof(uint16); // skip over glyph id of component
			pbGlyph += GlyphFlags & ARG_1_AND_2_ARE_WORDS ? 4 : 2; // skip over placement data

			if (fTransOffset) // MS rasterizer
				fTransOffset = !(GlyphFlags & UNSCALED_COMPONENT_OFFSET);
			else // Apple rasterizer
				fTransOffset = (GlyphFlags & SCALED_COMPONENT_OFFSET) != 0;

			if (GlyphFlags & WE_HAVE_A_SCALE)
			{
				flt11 = F2Dot14(swapws(*(uint16 *)pbGlyph));
				pbGlyph += sizeof(uint16);
				flt12 = 0;
				flt21 = 0;
				flt22 = flt11;
			}
			else if (GlyphFlags & WE_HAVE_AN_X_AND_Y_SCALE)
			{
				flt11 = F2Dot14(swapws(*(uint16 *)pbGlyph));
				pbGlyph += sizeof(uint16);
				flt12 = 0;
				flt21 = 0;
				flt22 = F2Dot14(swapws(*(uint16 *)pbGlyph));
				pbGlyph += sizeof(uint16);
			}
			else if (GlyphFlags & WE_HAVE_A_TWO_BY_TWO)
			{
				flt11 = F2Dot14(swapws(*(uint16 *)pbGlyph));
				pbGlyph += sizeof(uint16);
				flt12 = F2Dot14(swapws(*(uint16 *)pbGlyph));
				pbGlyph += sizeof(uint16);
				flt21 = F2Dot14(swapws(*(uint16 *)pbGlyph));
				pbGlyph += sizeof(uint16);
				flt22 = F2Dot14(swapws(*(uint16 *)pbGlyph));
				pbGlyph += sizeof(uint16);
			}
			else
			{ // identity transform
				flt11 = 1.0;
				flt12 = 0.0;
				flt21 = 0.0;
				flt22 = 1.0;
			}
			return true;
		}
		pbGlyph += sizeof(uint16); // skip over glyph id of component
		int nOffset = 0;
		nOffset += GlyphFlags & ARG_1_AND_2_ARE_WORDS ? 4 : 2;
		nOffset += GlyphFlags & WE_HAVE_A_SCALE ? 2 : 0;
		nOffset += GlyphFlags & WE_HAVE_AN_X_AND_Y_SCALE  ? 4 : 0;
		nOffset += GlyphFlags & WE_HAVE_A_TWO_BY_TWO  ? 8 :  0;
		pbGlyph += nOffset;
	} while (GlyphFlags & MORE_COMPONENTS);

	// didn't find requested component
	fTransOffset = false;
	flt11 = 1;
	flt12 = 0;
	flt21 = 0;
	flt22 = 1;
	return false;
}

/*----------------------------------------------------------------------------------------------
	Return a pointer into the glyf table based on the given tables and Glyph ID
	Since this method doesn't check for spaces, it is good to call IsSpace before using it.
	Return NULL on error
----------------------------------------------------------------------------------------------*/
void * TtfUtil::GlyfLookup(int nGlyphId, const void * pGlyf, const void * pLoca,
						   long lLocaSize, const void * pHead)
{
	long lGlyfOffset = LocaLookup(nGlyphId, pLoca, lLocaSize, pHead);
	void * pSimpleGlyf = GlyfLookup(pGlyf, lGlyfOffset); // invalid loca offset returns null
	return pSimpleGlyf;
}

/*----------------------------------------------------------------------------------------------
	Determine if a particular Glyph ID has any data in the glyf table. If it is white space,
	there will be no glyf data, though there will be metric data in hmtx, etc.
----------------------------------------------------------------------------------------------*/
bool TtfUtil::IsSpace(int nGlyphId, const void * pLoca, long lLocaSize, const void * pHead)
{
	long lGlyfOffset = LocaLookup(nGlyphId, pLoca, lLocaSize, pHead);
	if (lGlyfOffset < 0)
		return false; // if the Glyph ID is invalid, other code will have to catch it

	// the +1 should always work because there is a sentinel value at the end of the loca table
	long lNextGlyfOffset = LocaLookup(nGlyphId + 1, pLoca, lLocaSize, pHead);
	if (lNextGlyfOffset < 0)
		return false;

	return (lNextGlyfOffset - lGlyfOffset) ? false : true;
}

/*----------------------------------------------------------------------------------------------
	Determine if a particular Glyph ID is a multi-level composite.
	This is probably not needed since support for multi-level composites has been added.
----------------------------------------------------------------------------------------------*/
bool TtfUtil::IsDeepComposite(int nGlyphId, const void * pGlyf, const void * pLoca,
							 long lLocaSize, const void * pHead)
{
	if (IsSpace(nGlyphId, pLoca, lLocaSize, pHead)) {return false;}

	void * pSimpleGlyf = GlyfLookup(nGlyphId, pGlyf, pLoca, lLocaSize, pHead);
	if (pSimpleGlyf == NULL)
		return false; // no way to really indicate an error occured here

	if (GlyfContourCount(pSimpleGlyf) >= 0)
		return false;

	int rgnCompId[kMaxGlyphComponents]; // assumes only a limited number of glyph components
	int cCompIdTotal = kMaxGlyphComponents;
	int cCompId = 0;

	if (!GetComponentGlyphIds(pSimpleGlyf, rgnCompId, cCompIdTotal, cCompId))
		return false;

	for (int i = 0; i < cCompId; i++)
	{
		void * pSimpleGlyf = GlyfLookup(rgnCompId[i], pGlyf, pLoca, lLocaSize, pHead);
		if (pSimpleGlyf == NULL) {return false;}

		if (GlyfContourCount(pSimpleGlyf) < 0)
			return true;
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Get the bounding box coordinates based on the given tables and Glyph ID
	Handles both simple and composite glyphs.
	Return true if successful, false otherwise. On false, all point values will be INT_MIN
		False may indicate a white space glyph
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GlyfBox(int nGlyphId, const void * pGlyf, const void * pLoca, long lLocaSize,
					  const void * pHead, int & xMin, int & yMin, int & xMax, int & yMax)
{
	xMin = yMin = xMax = yMax = INT_MIN;

	if (IsSpace(nGlyphId, pLoca, lLocaSize, pHead)) {return false;}

	void * pSimpleGlyf = GlyfLookup(nGlyphId, pGlyf, pLoca, lLocaSize, pHead);
	if (pSimpleGlyf == NULL) {return false;}

	return GlyfBox(pSimpleGlyf, xMin, yMin, xMax, yMax);
}

/*----------------------------------------------------------------------------------------------
	Get the number of contours based on the given tables and Glyph ID.
	Handles both simple and composite glyphs.
	Return true if successful, false otherwise. On false, cnContours will be INT_MIN
		False may indicate a white space glyph (or component).
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GlyfContourCount(int nGlyphId, const void * pGlyf, const void * pLoca,
	size_t lLocaSize, const void * pHead, size_t & cnContours)
{
	cnContours = INT_MIN;

	if (IsSpace(nGlyphId, pLoca, lLocaSize, pHead)) {return false;}

	void * pSimpleGlyf = GlyfLookup(nGlyphId, pGlyf, pLoca, lLocaSize, pHead);
	if (pSimpleGlyf == NULL) {return false;}

	int cRtnContours = GlyfContourCount(pSimpleGlyf);
	if (cRtnContours >= 0)
	{
		cnContours = cRtnContours;
		return true;
	}

	//handle composite glyphs

	int rgnCompId[kMaxGlyphComponents]; // assumes only a limted number of glyph components
	int cCompIdTotal = kMaxGlyphComponents;
	int cCompId = 0;

	if (!GetComponentGlyphIds(pSimpleGlyf, rgnCompId, cCompIdTotal, cCompId))
		return false;

	cRtnContours = 0;
	int cTmp = 0;
	for (int i = 0; i < cCompId; i++)
	{
		if (IsSpace(rgnCompId[i], pLoca, lLocaSize, pHead)) {return false;}
		pSimpleGlyf = GlyfLookup(static_cast<gr::gid16>(rgnCompId[i]), pGlyf,
			pLoca, lLocaSize, pHead);
		if (pSimpleGlyf == NULL) {return false;}

		if ((cTmp = GlyfContourCount(pSimpleGlyf)) < 0)
		{
			size_t cNest = 0;
			if (!GlyfContourCount(static_cast<gr::gid16>(rgnCompId[i]), pGlyf, pLoca, lLocaSize,
					pHead, 	cNest))
				return false;
			cTmp = (int) cNest;
		}
		cRtnContours += cTmp;
	}

	cnContours = cRtnContours;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the point numbers for the end points of the glyph contours based on the given tables
	and Glyph ID.
	Handles both simple and composite glyphs.
	cnPoints - count of contours from GlyfContourCount (same as number of end points)
	prgnContourEndPoints - should point to a buffer large enough to hold cnPoints integers
	Return true if successful, false otherwise. On false, all end points are INT_MIN
		False may indicate a white space glyph (or component).
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GlyfContourEndPoints(int nGlyphId, const void * pGlyf, const void * pLoca,
	long lLocaSize, const void * pHead, int * prgnContourEndPoint, size_t & cnPoints)
{
	int i;
	for (i = 0; i < (int)cnPoints; i++)
		prgnContourEndPoint[i] = INT_MIN;

	if (IsSpace(nGlyphId, pLoca, lLocaSize, pHead)) {return false;}

	void * pSimpleGlyf = GlyfLookup(nGlyphId, pGlyf, pLoca, lLocaSize, pHead);
	if (pSimpleGlyf == NULL) {return false;}

	int cContours = GlyfContourCount(pSimpleGlyf);
	size_t cActualPts = 0;
	if (cContours > 0)
		return GlyfContourEndPoints(pSimpleGlyf, prgnContourEndPoint, cnPoints, cActualPts);

	// handle composite glyphs

	int rgnCompId[kMaxGlyphComponents]; // assumes no glyph will be made of more than 8 components
	int cCompIdTotal = kMaxGlyphComponents;
	int cCompId = 0;

	if (!GetComponentGlyphIds(pSimpleGlyf, rgnCompId, cCompIdTotal, cCompId))
		return false;

	int * prgnCurrentEndPoint = prgnContourEndPoint;
	size_t cCurrentPoints = cnPoints;
	int nPrevPt = 0;
	for (i = 0; i < cCompId; i++)
	{
		if (IsSpace(rgnCompId[i], pLoca, lLocaSize, pHead)) {return false;}
		pSimpleGlyf = GlyfLookup(rgnCompId[i], pGlyf, pLoca, lLocaSize, pHead);
		if (pSimpleGlyf == NULL) {return false;}

		if (!GlyfContourEndPoints(pSimpleGlyf, prgnCurrentEndPoint, cCurrentPoints, cActualPts))
		{
			size_t cNestedPts = cCurrentPoints;
			if (!GlyfContourEndPoints(static_cast<gr::gid16>(rgnCompId[i]), pGlyf, pLoca, lLocaSize,
					pHead, prgnCurrentEndPoint, cNestedPts))
				return false;
			cActualPts = cCurrentPoints - cNestedPts;
		}
		// points in composite are numbered sequentially as components are added
		//  must adjust end point numbers for new point numbers
		for (size_t j = 0; j < cActualPts; j++)
			prgnCurrentEndPoint[j] += nPrevPt;
		nPrevPt = prgnCurrentEndPoint[cActualPts - 1] + 1;

		prgnCurrentEndPoint += cActualPts;
		cCurrentPoints -= cActualPts;
	}

	cnPoints = cCurrentPoints;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the points for a glyph based on the given tables and Glyph ID
	Handles both simple and composite glyphs.
	cnPoints - count of points from largest end point obtained from GlyfContourEndPoints
	prgnX & prgnY - should point to buffers large enough to hold cnPoints integers
		The ranges are parallel so that coordinates for point(n) are found at offset n in
		both ranges. These points are in absolute coordinates.
	prgfOnCurve - should point to a buffer a large enough to hold cnPoints bytes (bool)
		This range is parallel to the prgnX & prgnY
	Return true if successful, false otherwise. On false, all points may be INT_MIN
		False may indicate a white space glyph (or component), or a corrupt font
	// TODO: doesn't support composite glyphs whose components are themselves components
		It's not clear from the TTF spec when the transforms should be applied. Should the
		transform be done before or after attachment point calcs? (current code - before)
		Should the transform be applied to other offsets? (currently - no; however commented
		out code is	in place so that if UNSCALED_COMPONENT_OFFSET on the MS rasterizer is
		clear (typical) then yes, and if SCALED_COMPONENT_OFFSET on the Apple rasterizer is
		clear (typical?) then no). See GetComponentTransform.
		It's also unclear where point numbering with attachment poinst starts
		(currently - first point number is relative to whole glyph, second point number is
		relative to current glyph).
----------------------------------------------------------------------------------------------*/
bool TtfUtil::GlyfPoints(int nGlyphId, const void * pGlyf, const void * pLoca,
	long lLocaSize, const void * pHead, const int * prgnContourEndPoint,
	int cnEndPoints, int * prgnX, int * prgnY, bool * prgfOnCurve, size_t & cnPoints)
{
	int i;
	for (i = 0; i < (int)cnPoints; i++)
	{
		prgnX[i] = INT_MIN;
		prgnY[i] = INT_MIN;
	}

	if (IsSpace(nGlyphId, pLoca, lLocaSize, pHead)) {return false;}

	void * pSimpleGlyf = GlyfLookup(nGlyphId, pGlyf, pLoca, lLocaSize, pHead);
	if (pSimpleGlyf == NULL) {return false;}

	int cContours = GlyfContourCount(pSimpleGlyf);
	size_t cActualPts;
	if (cContours > 0)
	{
		if (!GlyfPoints(pSimpleGlyf, prgnX, prgnY, (char *)prgfOnCurve, cnPoints, cActualPts))
			return false;
		CalcAbsolutePoints(prgnX, prgnY, cnPoints);
		SimplifyFlags((char *)prgfOnCurve, cnPoints);
		return true;
	}

	// handle composite glyphs
	int rgnCompId[kMaxGlyphComponents]; // assumes no glyph will be made of more than 8 components
	int cCompIdTotal = kMaxGlyphComponents;
	int cCompId = 0;

	// this will fail if there are more components than there is room for
	if (!GetComponentGlyphIds(pSimpleGlyf, rgnCompId, cCompIdTotal, cCompId))
		return false;

	int * prgnCurrentX = prgnX;
	int * prgnCurrentY = prgnY;
	char * prgbCurrentFlag = (char *)prgfOnCurve; // converting bool to char should be safe
	int cCurrentPoints = cnPoints;
	bool fOffset = true, fTransOff = true;
	int a, b;
	float flt11, flt12, flt21, flt22;
	// int * prgnPrevX = prgnX; // in case first att pt number relative to preceding glyph
	// int * prgnPrevY = prgnY;
	for (i = 0; i < cCompId; i++)
	{
		if (IsSpace(rgnCompId[i], pLoca, lLocaSize, pHead)) {return false;}
		void * pCompGlyf = GlyfLookup(static_cast<gr::gid16>(rgnCompId[i]), pGlyf,
			pLoca, lLocaSize, pHead);
		if (pCompGlyf == NULL) {return false;}

		if (!GlyfPoints(pCompGlyf, prgnCurrentX, prgnCurrentY, prgbCurrentFlag,
				cCurrentPoints, cActualPts))
		{
			size_t cNestedPts = cCurrentPoints;
			if (!GlyfPoints(static_cast<gr::gid16>(rgnCompId[i]), pGlyf, pLoca, lLocaSize, pHead,
				prgnContourEndPoint, cnEndPoints, prgnCurrentX, prgnCurrentY, (bool *)prgbCurrentFlag,
				cNestedPts))
			{
				return false;
			}
			cActualPts = cCurrentPoints - cNestedPts;
		}
		else
		{
			// convert points to absolute coordinates
			// do before transform and attachment point placement are applied
			CalcAbsolutePoints(prgnCurrentX, prgnCurrentY, cActualPts);
		}

		if (!GetComponentPlacement(pSimpleGlyf, rgnCompId[i], fOffset, a, b))
			return false;
		if (!GetComponentTransform(pSimpleGlyf, rgnCompId[i],
			flt11, flt12, flt21, flt22, fTransOff))
			return false;
		bool fIdTrans = flt11 == 1.0 && flt12 == 0.0 && flt21 == 0.0 && flt22 == 1.0;

		// apply transform - see main method note above
		// do before attachment point calcs
		if (!fIdTrans)
			for (size_t j = 0; j < cActualPts; j++)
			{
				int x = prgnCurrentX[j]; // store before transform applied
				int y = prgnCurrentY[j];
				prgnCurrentX[j] = (int)(x * flt11 + y * flt12);
				prgnCurrentY[j] = (int)(x * flt21 + y * flt22);
			}

		// apply placement - see main method note above
		int nXOff, nYOff;
		if (fOffset) // explicit x & y offsets
		{
			/* ignore fTransOff for now
			if (fTransOff && !fIdTrans)
			{ 	// transform x & y offsets
				nXOff = (int)(a * flt11 + b * flt12);
				nYOff = (int)(a * flt21 + b * flt22);
			}
			else */
			{ // don't transform offset
				nXOff = a;
				nYOff = b;
			}
		}
		else  // attachment points
		{	// in case first point is relative to preceding glyph and second relative to current
			// nXOff = prgnPrevX[a] - prgnCurrentX[b];
			// nYOff = prgnPrevY[a] - prgnCurrentY[b];
			// first point number relative to whole composite, second relative to current glyph
			nXOff = prgnX[a] - prgnCurrentX[b];
			nYOff = prgnY[a] - prgnCurrentY[b];
		}
		for (size_t j = 0; j < cActualPts; j++)
		{
			prgnCurrentX[j] += nXOff;
			prgnCurrentY[j] += nYOff;
		}

		// prgnPrevX = prgnCurrentX;
		// prgnPrevY = prgnCurrentY;
		prgnCurrentX += cActualPts;
		prgnCurrentY += cActualPts;
		prgbCurrentFlag += cActualPts;
		cCurrentPoints -= cActualPts;
	}

	SimplifyFlags((char *)prgfOnCurve, cnPoints);

	cnPoints = cCurrentPoints;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Simplify the meaning of flags to just indicate whether point is on-curve or off-curve
---------------------------------------------------------------------------------------------*/
bool TtfUtil::SimplifyFlags(char * prgbFlags, int cnPoints)
{
	for (int i = 0; i < cnPoints; i++)
	{
		if (prgbFlags[i] & ONCURVE)
			prgbFlags[i] = true;
		else
			prgbFlags[i] = false;
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Convert relative point coordinates to absolute coordinates
	Points are stored in the font such that they are offsets from one another except for the
		first point of a glyph.
---------------------------------------------------------------------------------------------*/
bool TtfUtil::CalcAbsolutePoints(int * prgnX, int * prgnY, int cnPoints)
{
	int nX = prgnX[0];
	int nY = prgnY[0];
	for (int i = 1; i < cnPoints; i++)
	{
		prgnX[i] += nX;
		nX = prgnX[i];
		prgnY[i] += nY;
		nY = prgnY[i];
	}

	return true;
}

/*----------------------------------------------------------------------------------------------
	Table of standard Postscript glyph names. From Martin Hosken. Disagress with ttfdump.exe
---------------------------------------------------------------------------------------------*/
const int TtfUtil::kcPostNames = 258;

const char * TtfUtil::rgPostName[TtfUtil::kcPostNames] = {
	".notdef", ".null", "nonmarkingreturn", "space", "exclam", "quotedbl", "numbersign",
	"dollar", "percent", "ampersand", "quotesingle", "parenleft",
	"parenright", "asterisk", "plus", "comma", "hyphen", "period", "slash",
	"zero", "one", "two", "three", "four", "five", "six", "seven", "eight",
	"nine", "colon", "semicolon", "less", "equal", "greater", "question",
	"at", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
	"N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
	"bracketleft", "backslash", "bracketright", "asciicircum",
	"underscore", "grave", "a", "b", "c", "d", "e", "f", "g", "h", "i",
	"j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w",
	"x", "y", "z", "braceleft", "bar", "braceright", "asciitilde",
	"Adieresis", "Aring", "Ccedilla", "Eacute", "Ntilde", "Odieresis",
	"Udieresis", "aacute", "agrave", "acircumflex", "adieresis", "atilde",
	"aring", "ccedilla", "eacute", "egrave", "ecircumflex", "edieresis",
	"iacute", "igrave", "icircumflex", "idieresis", "ntilde", "oacute",
	"ograve", "ocircumflex", "odieresis", "otilde", "uacute", "ugrave",
	"ucircumflex", "udieresis", "dagger", "degree", "cent", "sterling",
	"section", "bullet", "paragraph", "germandbls", "registered",
	"copyright", "trademark", "acute", "dieresis", "notequal", "AE",
	"Oslash", "infinity", "plusminus", "lessequal", "greaterequal", "yen",
	"mu", "partialdiff", "summation", "product", "pi", "integral",
	"ordfeminine", "ordmasculine", "Omega", "ae", "oslash", "questiondown",
	"exclamdown", "logicalnot", "radical", "florin", "approxequal",
	"Delta", "guillemotleft", "guillemotright", "ellipsis", "nonbreakingspace",
	"Agrave", "Atilde", "Otilde", "OE", "oe", "endash", "emdash",
	"quotedblleft", "quotedblright", "quoteleft", "quoteright", "divide",
	"lozenge", "ydieresis", "Ydieresis", "fraction", "currency",
	"guilsinglleft", "guilsinglright", "fi", "fl", "daggerdbl", "periodcentered",
	"quotesinglbase", "quotedblbase", "perthousand", "Acircumflex",
	"Ecircumflex", "Aacute", "Edieresis", "Egrave", "Iacute",
	"Icircumflex", "Idieresis", "Igrave", "Oacute", "Ocircumflex",
	"apple", "Ograve", "Uacute", "Ucircumflex", "Ugrave", "dotlessi",
	"circumflex", "tilde", "macron", "breve", "dotaccent", "ring",
	"cedilla", "hungarumlaut", "ogonek", "caron", "Lslash", "lslash",
	"Scaron", "scaron", "Zcaron", "zcaron", "brokenbar", "Eth", "eth",
	"Yacute", "yacute", "Thorn", "thorn", "minus", "multiply",
	"onesuperior", "twosuperior", "threesuperior", "onehalf", "onequarter",
	"threequarters", "franc", "Gbreve", "gbreve", "Idotaccent", "Scedilla",
	"scedilla", "Cacute", "cacute", "Ccaron", "ccaron",
	"dcroat" };

/*----------------------------------------------------------------------------------------------
	Return the length of the 'name' table in bytes.
	Currently used.
---------------------------------------------------------------------------------------------*/
#if 0
size_t NameTableLength(const gr::byte * pTable)
{
	gr::byte * pb = (const_cast<gr::byte *>(pTable)) + 2; // skip format
	size_t cRecords = *pb++ << 8; cRecords += *pb++;
	int dbStringOffset0 = (*pb++) << 8; dbStringOffset0 += *pb++;
	int dbMaxStringOffset = 0;
	for (size_t irec = 0; irec < cRecords; irec++)
	{
		int nPlatform = (*pb++) << 8; nPlatform += *pb++;
		int nEncoding = (*pb++) << 8; nEncoding += *pb++;
		int nLanguage = (*pb++) << 8; nLanguage += *pb++;
		int nName = (*pb++) << 8; nName += *pb++;
		int cbStringLen = (*pb++) << 8; cbStringLen += *pb++;
		int dbStringOffset = (*pb++) << 8; dbStringOffset += *pb++;
		if (dbMaxStringOffset < dbStringOffset + cbStringLen)
			dbMaxStringOffset = dbStringOffset + cbStringLen;
	}
	return dbStringOffset0 + dbMaxStringOffset;
}
#endif
