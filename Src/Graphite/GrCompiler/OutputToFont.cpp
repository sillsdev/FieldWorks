/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: OutputToFont.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Methods to write the tables in the TT font file.
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
static DWORD PadLong(DWORD ul); //
static unsigned long CalcCheckSum(const void  * pluTable, size_t cluSize);
static int CompareDirEntries(const void *,  const void *); // compare fn for qsort()


/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

/***********************************************************************************************
	Methods
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Copy all tables from src file to dst file and add our custom tables.
	Adding: Silf, Gloc, Glat, Feat, and possibly Sile.

	If we are outputting a separate control file, we are basically copying the minimal font,
	making a few changes (to name, cmap, and OS/2 tables), and adding 5 tables. Otherwise
	we are copying the original source font, making a few changes (to the name table), and
	adding 4 tables.

	Return 0 on success, non-zero otherwise.
	TODO AlanW: Output Sild table (table with debug strings).
----------------------------------------------------------------------------------------------*/
int GrcManager::OutputToFont(char * pchSrcFileName, char * pchDstFileName,
	utf16 * pchDstFontFamily, utf16 * pchSrcFontFamily)
{
	const int kcExtraTables = 5; // count of tables to add to font
	std::ifstream strmSrc(pchSrcFileName, std::ios::binary);
	GrcBinaryStream bstrmDst(pchDstFileName); // this deletes pre-existing file

	sfnt_OffsetTable OffsetTableSrc;
	sfnt_DirectoryEntry * pDirEntrySrc;
	uint16 cTableSrc;

	sfnt_OffsetTable OffsetTableMin;
	sfnt_DirectoryEntry * pDirEntryMin;
	uint16 cTableMin;

	uint16 cTableCopy;
	std::ifstream * pstrmCopy;
	sfnt_DirectoryEntry * pDirEntryCopy;

	sfnt_OffsetTable * pOffsetTableOut;
	int cTableOut;

	int iTableCmapSrc = -1;
	int iTableOS2Src = -1;
	int iTableHeadSrc = -1;
	unsigned int luMasterChecksumSrc;
	unsigned int rgnCreateTime[2];
	unsigned int rgnModifyTime[2];

	// read offset table and directory entries
	strmSrc.read((char *)&OffsetTableSrc, OFFSETTABLESIZE);
	if (swapl(OffsetTableSrc.version) != ONEFIX)
		return 1;
	cTableSrc = swapw(OffsetTableSrc.numOffsets);
	if (!(pDirEntrySrc = new sfnt_DirectoryEntry[cTableSrc + kcExtraTables])) // room for extras
		return 2;
	int i;
	for (i = 0; i < cTableSrc; i++)
	{
		strmSrc.read((char *)(pDirEntrySrc + i), sizeof(sfnt_DirectoryEntry));

		if (tag_CharToIndexMap == SFNT_SWAPTAG(pDirEntrySrc[i].tag))
			iTableCmapSrc = i;
		else if (tag_OS_2 == SFNT_SWAPTAG(pDirEntrySrc[i].tag))
			iTableOS2Src = i;
		else if (tag_FontHeader == SFNT_SWAPTAG(pDirEntrySrc[i].tag))
			iTableHeadSrc = i;
	};
	Assert(iTableCmapSrc >= 0);
	Assert(iTableOS2Src >= 0);
	Assert(iTableHeadSrc >= 0);

	// do the same for the minimal control file
	char * rgchEnvVarName = "GDLPP_PREFS";
	char * rgchMinPath = getenv(rgchEnvVarName);
	StrAnsi staMinFile(rgchMinPath);
	if (staMinFile.Length() > 2)
	{
		if (staMinFile.Length() > 0 && staMinFile[0] == '-' && staMinFile[1] == 'I')
			staMinFile = staMinFile.Right(staMinFile.Length() - 2);
		if (staMinFile[staMinFile.Length() - 1] != '\\')
			staMinFile.Append("\\", 1);
		staMinFile.Append("mingr.ttf");
	}
	else
	{
		// Can't find the minimal font; creating the stream will fail.
		staMinFile = "_bogus_.ttf";
	}
	std::ifstream strmMin(staMinFile.Chars(), std::ios::binary);
	if (SeparateControlFile())
	{
		strmMin.read((char *)&OffsetTableMin, OFFSETTABLESIZE);
		if (swapl(OffsetTableMin.version) != ONEFIX)
			return 3;
		cTableMin = swapw(OffsetTableMin.numOffsets);
		if (!(pDirEntryMin = new sfnt_DirectoryEntry[cTableMin]))
			return 4;
		for (i = 0; i < cTableMin; i++)
		{
			strmMin.read((char *)(pDirEntryMin + i), sizeof(sfnt_DirectoryEntry));
		};

		// Read the master checksum and dates from the source font head table.
		uint8 * pbTableHeadSrc;
		int cbSizeSrc = swapl(pDirEntrySrc[iTableHeadSrc].length);
		int ibOffsetSrc = swapl(pDirEntrySrc[iTableHeadSrc].offset);
		if (!(pbTableHeadSrc = new uint8[cbSizeSrc]))
			return 5;
		strmSrc.seekg(ibOffsetSrc);
		strmSrc.read((char *)pbTableHeadSrc, cbSizeSrc);
		luMasterChecksumSrc = TtfUtil::HeadTableCheckSum(pbTableHeadSrc);
		TtfUtil::HeadTableCreateTime(pbTableHeadSrc, &rgnCreateTime[0], &rgnCreateTime[1]);
		TtfUtil::HeadTableModifyTime(pbTableHeadSrc, &rgnModifyTime[0], &rgnModifyTime[1]);
		delete[] pbTableHeadSrc;

		cTableOut = cTableMin + kcExtraTables + 1; // Six extra tables: Sile, Silf, Feat, Gloc, Glat, Sill
		// This is the file we're really copying.
		pOffsetTableOut = &OffsetTableMin;
		cTableCopy = cTableMin;
		pstrmCopy = &strmMin;
		pDirEntryCopy = pDirEntryMin;
	}
	else
	{
		//	We'll output a copy of the base font, glyphs and all.
		cTableOut = cTableSrc + kcExtraTables; // Five extra tables: Silf, Feat, Gloc, Glat, Sill
		cTableMin = cTableSrc;
		pOffsetTableOut = &OffsetTableSrc;
		cTableCopy = cTableSrc;
		pstrmCopy = &strmSrc;
		pDirEntryCopy = pDirEntrySrc;
	}

	sfnt_DirectoryEntry * pDirEntryOut;
	if (!(pDirEntryOut = new sfnt_DirectoryEntry[cTableOut]))
		return 6;
	memcpy(pDirEntryOut, pDirEntryCopy, cTableSrc * sizeof(sfnt_DirectoryEntry));

	// offset table: adjust for extra tables and output
	int nP2, nLog;
	uint16 suTmp;
	BinarySearchConstants(cTableOut, &nP2, &nLog);
	pOffsetTableOut->numOffsets = swapw(cTableOut);
	suTmp = (uint16)(nP2 << 4);
	pOffsetTableOut->searchRange = swapw(suTmp);
	suTmp = (uint16)nLog;
	pOffsetTableOut->entrySelector = swapw(suTmp);
	suTmp = (cTableOut << 4) - (uint16)(nP2 << 4);
	pOffsetTableOut->rangeShift = swapw(suTmp);
	// write offset table (version & search constants)
	bstrmDst.Write((char *)pOffsetTableOut, OFFSETTABLESIZE);

	// copy tables from input stream to output stream
	long ibTableOffset, ibHeadOffset, cbHeadSize;
	int iNameTbl;
	int iCmapTbl = -1;
	int iOS2Tbl = -1;
	uint8 * pbTable;
	ibTableOffset = PadLong(OFFSETTABLESIZE + cTableOut * sizeof(sfnt_DirectoryEntry));
	bstrmDst.SetPosition(ibTableOffset);
	for (i = 0; i < cTableCopy; i++)
	{
		// read table
		uint32 ibOffset, cbSize;
		cbSize = swapl(pDirEntryCopy[i].length); // dir entries are word aligned already
		ibOffset = swapl(pDirEntryCopy[i].offset);
		if (!(pbTable = new uint8[cbSize]))
			return 7;
		pstrmCopy->seekg(ibOffset);
		pstrmCopy->read((char *)pbTable, cbSize);

		// merge the OS/2 tables from the minimal and source fonts
		if (SeparateControlFile() && tag_OS_2 == SFNT_SWAPTAG(pDirEntryCopy[i].tag))
		{
			iOS2Tbl = i;
			uint8 * pbTableSrc;
			int cbSizeSrc = swapl(pDirEntrySrc[iTableOS2Src].length);
			int ibOffsetSrc = swapl(pDirEntrySrc[iTableOS2Src].offset);
			if (!(pbTableSrc = new uint8[cbSizeSrc]))
				return 8;
			strmSrc.seekg(ibOffsetSrc);
			strmSrc.read((char *)pbTableSrc, cbSizeSrc);

			bstrmDst.SetPosition(ibTableOffset);
			if (!OutputOS2Table(pbTableSrc, cbSizeSrc, pbTable, cbSize, &bstrmDst, &cbSize))
				return 9;
			pDirEntryOut[i].length = swapl(cbSize);

			delete[] pbTableSrc;
		}
		// generate a new cmap
		else if (SeparateControlFile() && tag_CharToIndexMap == SFNT_SWAPTAG(pDirEntryCopy[i].tag))
		{
			iCmapTbl = i;
			uint8 * pbTableSrc;
			int cbSizeSrc = swapl(pDirEntrySrc[iTableCmapSrc].length);
			int ibOffsetSrc = swapl(pDirEntrySrc[iTableCmapSrc].offset);
			if (!(pbTableSrc = new uint8[cbSizeSrc]))
				return 10;
			strmSrc.seekg(ibOffsetSrc);
			strmSrc.read((char *)pbTableSrc, cbSizeSrc);

			bstrmDst.SetPosition(ibTableOffset);
			OutputCmapTable(pbTableSrc, cbSizeSrc, &bstrmDst, &cbSize);
			pDirEntryOut[i].length = swapl(cbSize);

			delete[] pbTableSrc;
		}
		else
		{
			// add the feature names and change the font name in the name table
			if (tag_NamingTable == SFNT_SWAPTAG(pDirEntryCopy[i].tag))
			{
				iNameTbl = i;
				if (!AddFeatsModFamily(&pbTable, &cbSize, pchDstFontFamily))
					return 11;
				pDirEntryOut[i].length = swapl(cbSize);
			}

			// remember offset and size of head table to adjust the file checksum later
			if (tag_FontHeader == SFNT_SWAPTAG(pDirEntryCopy[i].tag))
			{
				ibHeadOffset = ibTableOffset;
				cbHeadSize = cbSize;
			}

			// write table
			bstrmDst.SetPosition(ibTableOffset); // assumes seeking past EOF will fill with zeroes
			bstrmDst.Write((char *)pbTable, cbSize);
		}

		// adjust table's directory entry, offset will change since table directory is bigger
		pDirEntryOut[i].offset = swapl(ibTableOffset);

		ibTableOffset = bstrmDst.SeekPadLong(ibTableOffset + cbSize);
		delete [] pbTable;
	}

	// output our tables - Output*() methods handle 4 byte table alignment
	// create directory entries for our tables
	// can't set checksum because can't read table from GrcBinaryStream class (write only)
	uint32 ibOffset,ibOffset2;
	uint32 cbSize, cbSize2;
	int nCurTable = cTableMin;
	bstrmDst.SetPosition(ibTableOffset);

	// Gloc
	OutputGlatAndGloc(&bstrmDst, (int *)&ibOffset, (int *)&cbSize,
		(int *)&ibOffset2, (int *)&cbSize2);
	pDirEntryOut[nCurTable].tag = SFNT_SWAPTAG(tag_Gloc);
	pDirEntryOut[nCurTable].length = swapl(cbSize);
	pDirEntryOut[nCurTable].offset = swapl(ibOffset);
	pDirEntryOut[nCurTable++].checkSum = 0L; // place holder

	// Glat
	pDirEntryOut[nCurTable].tag = SFNT_SWAPTAG(tag_Glat);
	pDirEntryOut[nCurTable].length = swapl(cbSize2);
	pDirEntryOut[nCurTable].offset = swapl(ibOffset2);
	pDirEntryOut[nCurTable++].checkSum = 0L; // place holder

	// Silf
	OutputSilfTable(&bstrmDst, (int *)&ibOffset, (int *)&cbSize);
	pDirEntryOut[nCurTable].tag = SFNT_SWAPTAG(tag_Silf);
	pDirEntryOut[nCurTable].length = swapl(cbSize);
	pDirEntryOut[nCurTable].offset = swapl(ibOffset);
	pDirEntryOut[nCurTable++].checkSum = 0L; // place holder

	// Feat
	OutputFeatTable(&bstrmDst, (int *)&ibOffset, (int *)&cbSize);
	pDirEntryOut[nCurTable].tag = SFNT_SWAPTAG(tag_Feat);
	pDirEntryOut[nCurTable].length = swapl(cbSize);
	pDirEntryOut[nCurTable].offset = swapl(ibOffset);
	pDirEntryOut[nCurTable++].checkSum = 0L; // place holder

	// Sill
	OutputSillTable(&bstrmDst, (int *)&ibOffset, (int *)&cbSize);
	pDirEntryOut[nCurTable].tag = SFNT_SWAPTAG(tag_Sill);
	pDirEntryOut[nCurTable].length = swapl(cbSize);
	pDirEntryOut[nCurTable].offset = swapl(ibOffset);
	pDirEntryOut[nCurTable++].checkSum = 0L; // place holder

	if (SeparateControlFile())
	{
		// Sile
		OutputSileTable(&bstrmDst,
			pchSrcFontFamily, pchSrcFileName,
			luMasterChecksumSrc, rgnCreateTime, rgnModifyTime,
			(int *)&ibOffset, (int *)&cbSize);
		pDirEntryOut[nCurTable].tag = SFNT_SWAPTAG(tag_Sile);
		pDirEntryOut[nCurTable].length = swapl(cbSize);
		pDirEntryOut[nCurTable].offset = swapl(ibOffset);
		pDirEntryOut[nCurTable++].checkSum = 0L; // place holder
	}

	Assert(nCurTable == cTableOut);

	// fix up file checksums in table directory for our tables and any we modified
	uint32 luCheckSum;
	for (i = 0; i < cTableOut; i++)
	{
		if (i >= cTableCopy // one of our new Graphite tables
			|| i == iNameTbl || i == iOS2Tbl || i == iCmapTbl)
		{ // iterate over our tables
			cbSize = PadLong(swapl(pDirEntryOut[i].length)); // find table size (tables are padded)
			if (!(pbTable = new uint8[cbSize]))
				return 12;
			bstrmDst.seekg(swapl(pDirEntryOut[i].offset));
			bstrmDst.read((char *)pbTable, cbSize);
			luCheckSum = CalcCheckSum(pbTable, cbSize);
			pDirEntryOut[i].checkSum = swapl(luCheckSum);
			delete [] pbTable;
		}
	}

	// calc file checksum
	luCheckSum = 0;
	for (i = 0; i < cTableOut; i++)
	{
		luCheckSum += swapl(pDirEntryOut[i].checkSum);
	}
	luCheckSum += CalcCheckSum(pDirEntryOut, sizeof(sfnt_DirectoryEntry) * cTableOut);
	luCheckSum += CalcCheckSum(pOffsetTableOut, OFFSETTABLESIZE);
	luCheckSum = 0xB1B0AFBA - luCheckSum; // adjust checksum for head table

	// sort directory entries
	::qsort((void *)pDirEntryOut, cTableOut, sizeof(sfnt_DirectoryEntry), CompareDirEntries);

	// write directory entries
	bstrmDst.seekp(OFFSETTABLESIZE);
	for (i = 0; i < cTableOut; i++)
	{
		bstrmDst.write((char *)(pDirEntryOut + i), sizeof(sfnt_DirectoryEntry));
	}

	// write adjusted checksum to head table
	bstrmDst.seekg(ibHeadOffset);
	if (!(pbTable = new uint8[cbHeadSize]))
		return 13;
	bstrmDst.read((char *)pbTable, cbHeadSize); // read the head table
	((sfnt_FontHeader *)pbTable)->checkSumAdjustment = swapl(luCheckSum);
	bstrmDst.seekp(ibHeadOffset);
	bstrmDst.write((char *)pbTable, cbHeadSize); // overwrite the head table
	delete [] pbTable;

	delete [] pDirEntrySrc;
	if (SeparateControlFile())
		delete [] pDirEntryMin;
	delete[] pDirEntryOut;

	return 0;
}

/*----------------------------------------------------------------------------------------------
	Create a new name table with the strings associated with features and their settings in it.
	Also modify the font name. This method deletes ppNameTable's data and sets the pointer to
	the new name table. cbNameTbl is updated with the new size.
	pchFamilyName is the new font name. It will be stored as the font family name and
	as part of the full font name (which includes subfamily too if this is not regular).

	The new name table may be structured differently since any name table records which point
	to the same string will be adjusted to point to different strings (which are copies of one
	another). This produces a simpler algorithm since one string can be copied for each
	record. This issue was discovered close to release so large changes were rejected.
----------------------------------------------------------------------------------------------*/
bool GrcManager::AddFeatsModFamily(uint8 ** ppNameTbl, uint32 * pcbNameTbl, uint16 * pchFamilyName)
{
	// interpret name table header
	sfnt_NamingTable * pTbl = (sfnt_NamingTable *)(*ppNameTbl);
	uint16 cRecords = swapw(pTbl->count);
	uint16 ibStrOffset = swapw(pTbl->stringOffset);
	sfnt_NameRecord * pRecord = (sfnt_NameRecord *)(pTbl + 1);

	int iFamilyRecord, iSubFamilyRecord, iFullRecord;
	// indexes for location in name table where strings will be added
	// and for where largest name table id is stored (in relevant range)
	int iPlatEncMin, iPlatEncLim, iMaxNameId;
	int cbNames; // count of bytes needed to hold all name strings

	uint16 ibSubFamilyOffset, cbSubFamily;
	bool f31NameFound = false;

	// find name table entries for family name and full font name
	// first look for Microsoft Unicode table
	//   platform id = 3, encoding id = 1 below I call this the 3 1 table
	// even if pchFamilyName is null we need to set f31NameFound and iMaxNameId
	if (FindNameTblEntries(pRecord, cRecords, plat_MS, 1, LG_USENG,
		&iFamilyRecord, &iSubFamilyRecord, &iFullRecord,
		&iPlatEncMin, &iPlatEncLim, &iMaxNameId, &cbNames))
	{
		f31NameFound = true;
	}
	else
	{ // next look for Microsoft symbol font table (3 0 table)
		// Using this subtable could cause problems if strings' lead bytes are supposed to be
		// 0xF0 since the name table entries we are adding use the lead byte from
		// MultiByteToWideChar() which are probably 0x00, but this is unlikely to be a problem.
		if (!FindNameTblEntries(pRecord, cRecords, plat_MS, 0, LG_USENG,
			&iFamilyRecord, &iSubFamilyRecord, &iFullRecord,
			&iPlatEncMin, &iPlatEncLim, &iMaxNameId, &cbNames))
		{
			return false;
		}
	}
	ibSubFamilyOffset = swapw(pRecord[iSubFamilyRecord].offset) + ibStrOffset;
	cbSubFamily = swapw(pRecord[iSubFamilyRecord].length);

	// convert the family name and full name to wide char
	// these counts do NOT include null terminator
	uint16 cchwFamilyName = 0;
	uint16 cchwFullName = 0;
	uint16 * pchwFamilyName = NULL;
	uint16 * pchwFullName = NULL;

	// the current method must release the space allocated in this call for
	// pchwFamilyName and pchwFullName. these two pointers could point to the same string.
	// if pchFamilyName is null, no space is allocated.
	if (!BuildFontNames(pchFamilyName, (uint8 *)pTbl + ibSubFamilyOffset, cbSubFamily,
		&pchwFamilyName, &cchwFamilyName, &pchwFullName, &cchwFullName))
		return  false;

	// assign name table ids to the features and settings
	// and get vectors containing the external names, language ids, and name table ids
	//   for all features and settings
	// 256 - 32767 are allowable
	int nNameTblId = swapw(pRecord[iMaxNameId].nameID) + 1;
	nNameTblId = max(nNameTblId, 256);
	if (m_nNameTblStart != -1 && nNameTblId > m_nNameTblStart)
	{
		char rgch[20];
		itoa(nNameTblId, rgch, 10);
		g_errorList.AddWarning(5501, NULL,
			"Feature strings must start at ", rgch, " in the name table");
	}
	nNameTblId = max(nNameTblId, m_nNameTblStart);

	Vector<StrUni> vstuExtNames;
	Vector<uint16> vsuLangIds;
	Vector<uint16> vsuNameTblIds;

	if (!AssignFeatTableNameIds(nNameTblId, &vstuExtNames, &vsuLangIds, &vsuNameTblIds))
	{
		g_errorList.AddError(5101, NULL,
			"Insufficient space available for feature strings in name table.");
		return false; // checks for legal values
	}

	// determine size increases for new strings to add
	unsigned int cbNewTbl;
	int cNewRecords;
	uint8 *pNewTbl;

	cbNewTbl = sizeof(sfnt_NamingTable) + cRecords * sizeof(sfnt_NameRecord) + cbNames;
	Assert(cbNewTbl >= *pcbNameTbl);
	cNewRecords = vstuExtNames.Size();
	cbNewTbl += cNewRecords * sizeof(sfnt_NameRecord); // directory entries
	for (int iExtNms = 0; iExtNms < cNewRecords; iExtNms++)
	{
		cbNewTbl += vstuExtNames[iExtNms].Length() * sizeof(utf16); // string storage
	}

	if (pchFamilyName)
	{
		// determine size adjustments for font name changes
		// subtract old names
		cbNewTbl -= swapw(pRecord[iFamilyRecord].length);
		cbNewTbl -= swapw(pRecord[iFullRecord].length);
		// add new names
		cbNewTbl += cchwFamilyName * sizeof(utf16);
		cbNewTbl += cchwFullName * sizeof(utf16);
	}

	// create the new name table
	pNewTbl = new uint8[cbNewTbl];

	// copy directory entries and strings from source to destination
	// modify font name and add feature names
	if (!AddFeatsModFamilyAux((uint8 *)pTbl, *pcbNameTbl, pNewTbl, cbNewTbl,
		&vstuExtNames, &vsuLangIds, &vsuNameTblIds,
		iFamilyRecord, iFullRecord, iPlatEncMin, iPlatEncLim, f31NameFound,
		pchwFamilyName, cchwFamilyName, pchwFullName, cchwFullName))
		return false;

	// set the output args & clean up
	*pcbNameTbl = cbNewTbl;
	delete [] *ppNameTbl;
	*ppNameTbl = pNewTbl;

	if (pchwFamilyName)
		delete [] pchwFamilyName;
	if (pchwFullName != NULL && pchwFullName != pchwFamilyName)
		delete [] pchwFullName;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Find the indexes for the name table directory entries for the family name, subfamily name,
	and full font name in the range indicated by suPlatformId, suEncodingId, and suLangId.
	Also find the beginning and limiting index for the Platform and Writing system range
	and find the index for the largest name id in that range (regardless of Lang).
	Find how many bytes are needed to hold all name strings.
----------------------------------------------------------------------------------------------*/
bool GrcManager::FindNameTblEntries(void * pNameTblRecord, int cNameTblRecords,
	uint16 suPlatformId, uint16 suEncodingId, uint16 suLangId,
	int * piFamily, int * piSubFamily, int * piFullName,
	int * piPlatEncMin, int * piPlatEncLim, int * piMaxNameId, int * pcbNames)
{
	int iPlatEncMin = -1;
	int iPlatEncMax = -1;
	int iMaxNameId = -1;
	int nMaxNameId = -1;
	int cbNames = 0;
	bool fNamesFound = false;
	sfnt_NameRecord * pRecord = (sfnt_NameRecord *)(pNameTblRecord);

	for (int i = 0; i < cNameTblRecords; i++)
	{
		cbNames += swapw(pRecord->length);

		if (swapw(pRecord->platformID) == suPlatformId &&
			swapw(pRecord->specificID) == suEncodingId)
		{
			if (iPlatEncMin == -1)
			{
				iPlatEncMin = i;
			}
			iPlatEncMax = i;
			if (nMaxNameId < swapw(pRecord->nameID))
			{
				nMaxNameId = swapw(pRecord->nameID);
				iMaxNameId = i;
			}
			if (swapw(pRecord->languageID) == suLangId)
			{
				fNamesFound = true;
				if (swapw(pRecord->nameID) == name_Family)
				{
					*piFamily = i;
				}
				if (swapw(pRecord->nameID) == name_Subfamily)
				{
					*piSubFamily = i;
				}
				if (swapw(pRecord->nameID) == name_FullName)
				{
					*piFullName = i;
				}
			}
		}
		pRecord++;
	}

	if (fNamesFound)
	{
		*piPlatEncMin = iPlatEncMin;
		*piPlatEncLim = iPlatEncMax + 1;
		*piMaxNameId = iMaxNameId;
	}

	*pcbNames = cbNames;

	return fNamesFound;
}

/*----------------------------------------------------------------------------------------------
	Convert pchFamilyName into the form required for the name table (Unicode Big Endian).
	Also create the full font name based on the family and subfamily names. If the subfamily
	is 'regular' then the full name is the same as the font name, otherwise the subfamily
	name must be appended to the family name.
	pchFamilyName can be null. ppchwFamilyName and ppchwFullName can point to same string if
	the names are the same.
	This method allocates space for the string(s) using new[]. The caller must free this space
	using delete[].
	The counts in pcchwFamilyName and pcchwFullName should NOT include the a trailing null.
	pSubFamily points to the string as stored in the name table (Unicode big endian).
----------------------------------------------------------------------------------------------*/
bool GrcManager::BuildFontNames(uint16 * pchFamilyName, uint8 * pSubFamily, uint16 cbSubFamily,
			   uint16 ** ppchwFamilyName, uint16 * pcchwFamilyName,
			   uint16 ** ppchwFullName, uint16 * pcchwFullName)
{
	// convert the family name to wide char
	uint16 cchwFamilyName, cchwFullName;
	uint16 * pchwFamilyName, * pchwFullName;

	if (!pchFamilyName)
	{
		return true;
	}

	Assert(*ppchwFamilyName == NULL);
	Assert(*ppchwFullName == NULL);

	//cchwFamilyName = ::MultiByteToWideChar(CP_ACP, 0, pchFamilyName, -1, NULL, 0);
	//pchwFamilyName = new uint16[cchwFamilyName]; // cchwFamily includes null terminator
	//if (!pchwFamilyName)
	//	return false;
	//::MultiByteToWideChar(CP_ACP, 0, pchFamilyName, -1, pchwFamilyName, cchwFamilyName);
	//cchwFamilyName--; // eliminate null terminator from count
	//if (!TtfUtil::SwapWString(pchwFamilyName, cchwFamilyName)) // convert to big endian chars
	//	return false;

	cchwFamilyName = utf16len(pchFamilyName);
	pchwFamilyName = new uint16[cchwFamilyName + 1];
	if (!pchwFamilyName)
		return false;
	utf16cpy(pchwFamilyName, pchFamilyName);
	pchwFamilyName[cchwFamilyName] = 0;
	if (!TtfUtil::SwapWString(pchwFamilyName, cchwFamilyName)) // convert to big endian chars
		return false;

	// check for Regular subfamily
	uint16 rgchwReg[7]; // 7 - number of chars in "Regular"
	utf16ncpy(rgchwReg, "Regular", 7);
	TtfUtil::SwapWString(rgchwReg, 7);
	// can't use case insensitive compare since Unicode values are big endian now
	bool fRegular = utf16ncmp((utf16 *)(pSubFamily), rgchwReg, 7) == 0;

	// build the full font name
	if (fRegular)
	{ // regular does not include the subfamily in the full font name
		pchwFullName = pchwFamilyName;
		cchwFullName = cchwFamilyName;
	}
	else
	{ // other styles do include subfamily in the full font name
		cchwFullName = cchwFamilyName + cbSubFamily / sizeof(utf16) + 1; // 1 - room for space
		pchwFullName = new uint16[cchwFullName + 1];
		if (!pchwFullName)
			return false;
		utf16ncpy(pchwFullName, pchwFamilyName, cchwFamilyName);
		pchwFullName[cchwFamilyName] = swapw(0x0020); // big endian Unicode space
		utf16ncpy(pchwFullName + cchwFamilyName + 1, (utf16 *)(pSubFamily),
			cbSubFamily / sizeof(utf16));
		pchwFullName[cchwFullName] = 0;
	}

	*ppchwFamilyName = pchwFamilyName;
	*pcchwFamilyName = cchwFamilyName;
	*ppchwFullName = pchwFullName;
	*pcchwFullName = cchwFullName;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Add feature and setting names to the name table. Also modify the family and full font names
	if pchwFamilyName is not null.
	The method also copies names from pNameTbl into pNewTbl. The feature data is in the three
	vectors passed in which must parallel each other. The don't need to be sorted.
----------------------------------------------------------------------------------------------*/
bool GrcManager::AddFeatsModFamilyAux(uint8 * pNameTbl, uint32 cbNameTbl,
	uint8 * pNewTbl, uint32 cbNewTbl,
	Vector<StrUni> * pvstuExtNames, Vector<uint16> * pvsuLangIds, Vector<uint16> * pvsuNameTblIds,
	int iFamilyRecord, int iFullRecord, int iPlatEncMin, int iPlatEncLim, bool f31Name,
	uint16 * pchwFamilyName, uint16 cchwFamilyName,
	uint16 * pchwFullName, uint16 cchwFullName)
{
	// struct used to store data from directory entries that need to be sorted
	struct NameTblEntry
	{
		uint16 suLangId;
		uint16 suNameId;
		uint16 cbStr;
		uint8 * pbStr;
		static int Compare(const void * ptr1, const void * ptr2)
		{
			NameTblEntry * p1 = (NameTblEntry *) ptr1;
			NameTblEntry * p2 = (NameTblEntry *) ptr2;

			if (p1->suLangId != p2->suLangId)
				return p1->suLangId - p2->suLangId;
			else
				return p1->suNameId - p2->suNameId;
		}
	};

	sfnt_NamingTable * pTbl = (sfnt_NamingTable *)(pNameTbl);
	uint16 cRecords = swapw(pTbl->count);
	uint16 ibStrOffset = swapw(pTbl->stringOffset);
	sfnt_NameRecord * pRecord = (sfnt_NameRecord *)(pTbl + 1);

	int cNewRecords = pvstuExtNames->Size();

	int ibNewStrOffset, ibNewStr, cbStr;
	sfnt_NameRecord * pNewRecord;
	uint8 * pStr, * pNewStr;

	// name table header
	((sfnt_NamingTable *)pNewTbl)->format = pTbl->format;
	((sfnt_NamingTable *)pNewTbl)->count = swapw(cRecords + cNewRecords);
	ibNewStrOffset = sizeof(sfnt_NamingTable) +
		(cRecords + cNewRecords) * sizeof(sfnt_NameRecord);
	((sfnt_NamingTable *)pNewTbl)->stringOffset = swapw(ibNewStrOffset);

	pNewRecord = (sfnt_NameRecord *)(pNewTbl + sizeof(sfnt_NamingTable));
	ibNewStr = 0;

	// copy directory entries and strings prior to 3 1 (or 3 0) section
	int iRecord;
	for (iRecord = 0; iRecord < iPlatEncMin; iRecord++)
	{
		pNewRecord[iRecord].platformID = pRecord[iRecord].platformID;
		pNewRecord[iRecord].specificID = pRecord[iRecord].specificID;
		pNewRecord[iRecord].languageID = pRecord[iRecord].languageID;
		pNewRecord[iRecord].nameID = pRecord[iRecord].nameID;
		pNewRecord[iRecord].length = pRecord[iRecord].length;

		cbStr = swapw(pRecord[iRecord].length);
		pStr = (uint8 *)pTbl + ibStrOffset + swapw(pRecord[iRecord].offset);
		pNewStr = pNewTbl + ibNewStrOffset + ibNewStr;
		pNewRecord[iRecord].offset = swapw(ibNewStr);
		memcpy(pNewStr, pStr, cbStr);
		ibNewStr += cbStr;
	}

	// copy 3 1 (or 3 0) part of name table into list of entries
	int cEntries = iPlatEncLim - iPlatEncMin + cNewRecords; // also room for feat names
	NameTblEntry * pEntry = new NameTblEntry[cEntries];
	if (!pEntry)
		return false;
	NameTblEntry * p;
	for (iRecord = iPlatEncMin; iRecord < iPlatEncLim; iRecord++)
	{
		p = pEntry + (iRecord - iPlatEncMin);
		// byte swap these for comparison with features
		p->suLangId = swapw(pRecord[iRecord].languageID);
		p->suNameId = swapw(pRecord[iRecord].nameID);
		p->cbStr = swapw(pRecord[iRecord].length);

		if (!(p->pbStr = new uint8[p->cbStr])) return false;
		pStr = (uint8 *)pTbl + ibStrOffset + swapw(pRecord[iRecord].offset);
		memcpy(p->pbStr, pStr, p->cbStr);
	}

	// modify family and full font names if needed
	if (pchwFamilyName)
	{
		p = pEntry + (iFamilyRecord - iPlatEncMin);
		delete [] p->pbStr;
		p->cbStr = cchwFamilyName * sizeof(utf16);
		if (!(p->pbStr = new uint8[p->cbStr])) return false;
		memcpy(p->pbStr, pchwFamilyName, p->cbStr);

		p = pEntry + (iFullRecord - iPlatEncMin);
		delete [] p->pbStr;
		p->cbStr = cchwFullName * sizeof(utf16);
		if (!(p->pbStr = new uint8[p->cbStr])) return false;
		memcpy(p->pbStr, pchwFullName, p->cbStr);
	}

	// add feature string data to list of entries
	for (iRecord = 0; iRecord < cNewRecords; iRecord++)
	{
		p = pEntry + (iRecord + iPlatEncLim - iPlatEncMin);
		p->suLangId = (*pvsuLangIds)[iRecord];
		p->suNameId = (*pvsuNameTblIds)[iRecord];
		p->cbStr = (*pvstuExtNames)[iRecord].Length() * sizeof(utf16);

		if (!(p->pbStr = new uint8[p->cbStr])) return false;
		memcpy(p->pbStr, (*pvstuExtNames)[iRecord], p->cbStr);
		TtfUtil::SwapWString(p->pbStr, p->cbStr / sizeof(utf16)); // make big endian
	}

	// sort list of entries
	::qsort(pEntry, cEntries, sizeof(NameTblEntry), NameTblEntry::Compare);

	// output list of entries to new name table
	for (iRecord = 0; iRecord < cEntries; iRecord++)
	{
		pNewRecord[iPlatEncMin + iRecord].platformID = swapw(plat_MS);
		pNewRecord[iPlatEncMin + iRecord].specificID = swapw(f31Name ? 1 : 0);
		pNewRecord[iPlatEncMin + iRecord].languageID = swapw(pEntry[iRecord].suLangId);
		pNewRecord[iPlatEncMin + iRecord].nameID = swapw(pEntry[iRecord].suNameId);
		pNewRecord[iPlatEncMin + iRecord].length = swapw(pEntry[iRecord].cbStr);

		cbStr = pEntry[iRecord].cbStr;
		pStr = pEntry[iRecord].pbStr;
		pNewStr = pNewTbl + ibNewStrOffset + ibNewStr;
		pNewRecord[iPlatEncMin + iRecord].offset = swapw(ibNewStr);
		memcpy(pNewStr, pStr, cbStr);
		ibNewStr += cbStr;
	}

	// copy remaining name table entries after 3 1 (or 3 0) section
	for (iRecord = iPlatEncLim; iRecord < cRecords; iRecord++)
	{
		int iNewRecord = iPlatEncMin + cEntries - iPlatEncLim;
		pNewRecord[iNewRecord + iRecord].platformID = pRecord[iRecord].platformID;
		pNewRecord[iNewRecord + iRecord].specificID = pRecord[iRecord].specificID;
		pNewRecord[iNewRecord + iRecord].languageID = pRecord[iRecord].languageID;
		pNewRecord[iNewRecord + iRecord].nameID = pRecord[iRecord].nameID;
		pNewRecord[iNewRecord + iRecord].length = pRecord[iRecord].length;

		cbStr = swapw(pRecord[iRecord].length);
		pStr = (uint8 *)pTbl + ibStrOffset + swapw(pRecord[iRecord].offset);
		pNewStr = pNewTbl + ibNewStrOffset + ibNewStr;
		pNewRecord[iNewRecord + iRecord].offset = swapw(ibNewStr);
		memcpy(pNewStr, pStr, cbStr);
		ibNewStr += cbStr;
	}

	Assert(pNewTbl + cbNewTbl == pNewStr + cbStr);

	// clean up
	for (iRecord = 0; iRecord < cEntries; iRecord++)
	{
		delete [] pEntry[iRecord].pbStr;
	}
	delete [] pEntry;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Write the OS/2 table to the control file, using the table from the minimal font but
	copying key fields from the source font.

		0-1		USHORT	version	= 0x0003
		2-3		SHORT	xAvgCharWidth
		4-5		USHORT	usWeightClass
		6-7		USHORT	usWidthClass
		8-9		USHORT	fsType
		10-29	SHORT	various offsets and sizes [10]
		30-31	SHORT	sFamilyClass
		32-41	BYTE	panose[10]
		42-57	ULONG	ulUnicodeRange
		58-61	CHAR	achVendID[4]
		62-63	USHORT	fsSelection
		64-65	USHORT	usFirstCharIndex
		66-67	USHORT	usLastCharIndex
		...plus more stuff we don't care about
----------------------------------------------------------------------------------------------*/
bool GrcManager::OutputOS2Table(uint8 * pOs2TblSrc, uint32 cbOs2TblSrc,
	uint8 * pOs2TblMin, uint32 cbOs2TblMin,
	GrcBinaryStream * pbstrm, uint32 * pcbSizeRet)
{
	Assert(cbOs2TblSrc >= 68); // number of bytes we may need to fiddle with
	Assert(cbOs2TblMin >= 68);
	uint8 * prgOs2TblNew = new uint8[cbOs2TblMin];
	if (!prgOs2TblNew)
		return false;

	// Initialize with minimal font table.
	memcpy(prgOs2TblNew, pOs2TblMin, isizeof(uint8) * cbOs2TblMin);

	// Overwrite key fields with information from the source font.

	// weight class: 16 bits (2 bytes)
	memcpy(prgOs2TblNew + 4, pOs2TblSrc + 4, isizeof(uint16));

	// fsType: 16 bits (2 bytes)
	memcpy(prgOs2TblNew + 8, pOs2TblSrc + 8, isizeof(uint16));

	// family class: 16 bits (2 bytes)
	memcpy(prgOs2TblNew + 30, pOs2TblSrc + 30, isizeof(uint16));

	// char range: 4 32-bit values (16 bytes)
	memcpy(prgOs2TblNew + 42, pOs2TblSrc + 42, isizeof(uint8)*4 * 4);

	// max Unicode value: 16 bits (2 bytes)
	memcpy(prgOs2TblNew + 64, pOs2TblSrc + 64, isizeof(uint16));

	// min Unicode value: 16 bits (2 bytes)
	memcpy(prgOs2TblNew + 66, pOs2TblSrc + 66, isizeof(uint16));

	// Write the result onto the output stream.
	pbstrm->Write((char *)prgOs2TblNew, cbOs2TblMin);
	*pcbSizeRet = cbOs2TblMin;

	delete[] prgOs2TblNew;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Write the cmap table to the control file. We are outputting a copy of the cmap in the
	source file, with every supported character pointing at some bogus glyph (ie, square box).
----------------------------------------------------------------------------------------------*/
bool GrcManager::OutputCmapTable(uint8 * pCmapTblSrc, uint32 cbCmapTblSrc,
	GrcBinaryStream * pbstrm, uint32 * pcbSizeRet)
{
	long lPosStart = pbstrm->Position();

	//	See if we can get the USC-4 subtable.
	void * pCmap_3_10 = TtfUtil::FindCmapSubtable(pCmapTblSrc, 3, 10);

	//	Regardless, we should be able to find a 16-bit table.
	//	First try UTF-16 table: platform 3 encoding 1
	int nEncID = 1;
	void * pCmap_3_1 = TtfUtil::FindCmapSubtable(pCmapTblSrc, 3, 1);
	if (pCmap_3_1 == NULL)
	{
		// Try the platform 3 encoding 0 table instead (symbol font)
		pCmap_3_1 = TtfUtil::FindCmapSubtable(pCmapTblSrc, 3, 0);
		if (pCmap_3_1)
			nEncID = 0;
	}

	// Note that even if we found the 3-10 table, we're supposed to store a 3-1 table
	// as well for backward-compatibility;
	// see http://www.microsoft.com/typography/OTSPEC/cmap.htm#language

	// version
	pbstrm->WriteShort(0);

	// number of subtables
	long lPosTableCnt = pbstrm->Position();
	if (pCmap_3_10)
		pbstrm->WriteShort(2);	// both formats
	else
		pbstrm->WriteShort(1);	// just 16-bit

	// subtable info
	pbstrm->WriteShort(3);		// platform ID
	pbstrm->WriteShort(nEncID);	// encoding ID: 1 or 0
	long lPosOffset31 = pbstrm->Position();
	pbstrm->WriteInt(0);	// offset

	long lPos310Dir = pbstrm->Position();
	long lPosOffset310;
	if (pCmap_3_10)
	{
		pbstrm->WriteShort(3);	// platform ID
		pbstrm->WriteShort(10);	// encoding ID
		lPosOffset310 = pbstrm->Position();
		pbstrm->WriteInt(0);	// offset: fill in later
	}
	else
	{
		// Just in case we find out we have to output a 3-10 table after all, leave
		// space for it in the directory.
		pbstrm->WriteShort(0);
		pbstrm->WriteShort(0);
		pbstrm->WriteInt(0);
	}

	int ibOffset31 = pbstrm->Position() - lPosStart;
	bool fNeed310;
	OutputCmap31Table(
		(pCmap_3_1) ? pCmap_3_1 : pCmap_3_10,
		pbstrm, (pCmap_3_1 == NULL), &fNeed310);
	// fill in the offset
	long lPosEnd = pbstrm->Position();
	pbstrm->SetPosition(lPosOffset31);
	pbstrm->WriteInt(ibOffset31);

	if (fNeed310)
	{
		// We'll have to output a 3-10 table after all. Go back and put information
		// about it in the directory.
		pbstrm->SetPosition(lPosTableCnt);
		pbstrm->WriteShort(2);	// both formats

		pbstrm->SetPosition(lPos310Dir);
		pbstrm->WriteShort(3);	// platform ID
		pbstrm->WriteShort(10);	// encoding ID
		lPosOffset310 = pbstrm->Position();
		pbstrm->WriteInt(0);	// offset: fill in later
	}

	pbstrm->SetPosition(lPosEnd);

	if (pCmap_3_10 || fNeed310)
	{
		int ibOffset310 = pbstrm->Position() - lPosStart;
		OutputCmap310Table((pCmap_3_10) ? pCmap_3_10 : pCmap_3_1,
			pbstrm, (pCmap_3_10 == NULL));
		// fill in the offset
		lPosEnd = pbstrm->Position();
		pbstrm->SetPosition(lPosOffset310);
		pbstrm->WriteInt(ibOffset310);
		pbstrm->SetPosition(lPosEnd);
	}

	*pcbSizeRet = lPosEnd - lPosStart;
	return true;
}

/*----------------------------------------------------------------------------------------------
	Write the format 4.0 cmap (platform 3 encoding 1) subtable to the control file.
	Make every glyph point at the square box.
	Return the number of bytes in the table.
	Generally it is also reading from a 3-1 table as well, but if we don't have one, we
	may need to generate a 3-1 table from a 3-10 table (format 12).

	We can't output a 4.0 format to include more than 8190 [= (65535 - 14) / 8)]
	codepoints, because the length field is only 16 bits, and because of the lack of
	compression we use up 8 bytes per codepoint! So if we have more than that number, we
	have to truncate the 3-1 table, and also make sure to output a 3-10 table which can be
	quite a bit longer.
----------------------------------------------------------------------------------------------*/
int GrcManager::OutputCmap31Table(void * pCmapSubTblSrc,
	GrcBinaryStream * pbstrm, bool fFrom310, bool * pfNeed310)
{
	long lPosStart = pbstrm->Position();

	int cch = 0;	// number of codepoints in the map

	//	format
	pbstrm->WriteShort(4);

	//	length: fill in later
	int lPosLen = pbstrm->Position();
	pbstrm->WriteShort(0);

	//	language (irrelevant except on Macintosh)
	pbstrm->WriteShort(0);

	//	search variables: fill in later
	int lPosSearch = pbstrm->Position();
	pbstrm->WriteShort(0);
	pbstrm->WriteShort(0);
	pbstrm->WriteShort(0);
	pbstrm->WriteShort(0);

	//	Generate the list of supported characters.

	utf16 rgchChars[65535];
	Vector<int> vichGroupStart;
	//Vector<int> vcchGroupSize;
	int cGroups = 0;
	bool fTruncated = false;
	int key = 0; // for optimizing next-codepoint lookup
	int ch = (fFrom310) ?
		TtfUtil::Cmap310NextCodepoint(pCmapSubTblSrc, 0, &key) :
		TtfUtil::Cmap31NextCodepoint(pCmapSubTblSrc, 0, &key);
	while (ch < 0xFFFF)
	{
		rgchChars[cch] = ch;
		if (cch == 0 || ch != rgchChars[cch - 1] + 1)
		{
			vichGroupStart.Push(cch);
			cGroups++;
		}

		cch++;
		ch = (fFrom310) ?
			TtfUtil::Cmap310NextCodepoint(pCmapSubTblSrc, ch, &key) :
			TtfUtil::Cmap31NextCodepoint(pCmapSubTblSrc, ch, &key);

		// If we're over the limit of what the subtable can handle, truncate.
		int cbNeeded = 14	// header
			+ (cGroups * 8)	// group information
			+ (cch * 2)		// glyphIdArray
			+ 10;			// final FFFF
		if (cbNeeded >= 0xFFFF) // 65535 is limit on subtable length
		{
			if (vichGroupStart[cGroups - 1] == cch - 1)
			{
				vichGroupStart.Pop();
				cGroups--;
			}
			cch--;
			ch = 0xFFFF;
			fTruncated = true;
		}
	}
	rgchChars[cch] = 0xFFFF;  // this must always be last
	vichGroupStart.Push(cch);
	cGroups++;
	cch++;

	//	Output the ranges of characters.

	//	End codes
	int iGroup;
	for (iGroup = 0; iGroup < cGroups - 1; iGroup++)
		pbstrm->WriteShort(rgchChars[vichGroupStart[iGroup+1] - 1]);
	pbstrm->WriteShort(0xFFFF);

	//	reserved pad
	pbstrm->WriteShort(0);

	//	Start codes
	for (iGroup = 0; iGroup < cGroups; iGroup++)
		pbstrm->WriteShort(rgchChars[vichGroupStart[iGroup]]);

	//	id delta: not used; pad with zeros
	for (iGroup = 0; iGroup < cGroups; iGroup++)
		pbstrm->WriteShort(0);

	//	id range array: each element holds the offset (in bytes) from the element
	//	itself down to the corresponding glyphIdArray element (this works because
	//	glyphIdArray immediately follows idRangeArray).
	int cchSoFar = 0;
	for (iGroup = 0; iGroup < cGroups - 1; iGroup++)
	{
		int nStart = rgchChars[vichGroupStart[iGroup]];
		int nEnd = rgchChars[vichGroupStart[iGroup+1] - 1];

		int nRangeOffset = (cGroups - iGroup + cchSoFar) * 2; // *2 for bytes, not wchars
		pbstrm->WriteShort(nRangeOffset);

		cchSoFar += (nEnd - nStart + 1);
	}
	pbstrm->WriteShort((1 + cchSoFar) * 2);
	cchSoFar++;

	//	glyphIdArray: all characters point to our special square box,
	//	except for the space character.
	for (int ich = 0; ich < cch; ich++)
	{
		if (rgchChars[ich] == kchwSpace)
			pbstrm->WriteShort(2);
		else if (rgchChars[ich] == 0xFFFF) // invalid
			pbstrm->WriteShort(0);
		else
			pbstrm->WriteShort(3);
	}

	long lPosEnd = pbstrm->Position();

	//	Fill in the length.
	int cb = lPosEnd - lPosStart;
	pbstrm->SetPosition(lPosLen);
	pbstrm->WriteShort(cb);

	//	Fill in the search variables; note that these are byte-based and so are multiplied by 2,
	//	unlike what is done in the Graphite tables.
	int nPowerOf2, nLog;
	BinarySearchConstants(cGroups, &nPowerOf2, &nLog);
	pbstrm->SetPosition(lPosSearch);
	pbstrm->WriteShort(cGroups << 1);	// * 2
	pbstrm->WriteShort(nPowerOf2 << 1); // * 2
	pbstrm->WriteShort(nLog);
	pbstrm->WriteShort((cGroups - nPowerOf2) << 1);  // * 2

	pbstrm->SetPosition(lPosEnd);

	if (fTruncated)
	{
		g_errorList.AddWarning(5502, NULL,
			"cmap format 4 subtable truncated--does not match format 12 subtable");
	}

	*pfNeed310 = fTruncated;

	return cb;
}

#if 0
// Old approach that uses one segment per codepoint.
int GrcManager::OutputCmap31Table(void * pCmapSubTblSrc,
	GrcBinaryStream * pbstrm, bool fFrom310, bool * pfNeed310)
{
	//	Maximum number of codepoints this table can hold, given the lack of compression
	//	we can do.
	const int cchMax = 8188; // = ((65535 - 14) / 8) - 2

	long lPosStart = pbstrm->Position();

	int cch = 0;	// number of items in the map

	//	format
	pbstrm->WriteShort(4);

	//	length: fill in later
	int lPosLen = pbstrm->Position();
	pbstrm->WriteShort(0);

	//	language (irrelevant except on Macintosh)
	pbstrm->WriteShort(0);

	//	search variables: fill in later
	int lPosSearch = pbstrm->Position();
	pbstrm->WriteShort(0);
	pbstrm->WriteShort(0);
	pbstrm->WriteShort(0);
	pbstrm->WriteShort(0);

	//	Generate the list of supported characters.

	utf16 rgchChars[65535];
	int key = 0; // for optimizing next-codepoint lookup
	int ch = (fFrom310) ?
		TtfUtil::Cmap310NextCodepoint(pCmapSubTblSrc, 0, &key) :
		TtfUtil::Cmap31NextCodepoint(pCmapSubTblSrc, 0, &key);
	while (ch < 0xFFFF)
	{
		rgchChars[cch] = ch;
		cch++;
		ch = (fFrom310) ?
			TtfUtil::Cmap310NextCodepoint(pCmapSubTblSrc, ch, &key) :
			TtfUtil::Cmap31NextCodepoint(pCmapSubTblSrc, ch, &key);
	}
	rgchChars[cch] = 0xFFFF;  // this must always be last
	cch++;

	//	Only output the maximum we can handle; see comment above.
	int cchOutput;
	if (cch > cchMax)
	{
		cchOutput = cchMax;
		//	If we're truncating this table, we need a 3-10 table that can
		//	handle it all properly.
		*pfNeed310 = true;
		rgchChars[cchOutput - 1] = 0xFFFF; // must be the last character
	}
	else
	{
		cchOutput = cch;
		*pfNeed310 = false;
		Assert(rgchChars[cchOutput - 1] == 0xFFFF);
	}

	//	Output the ranges of characters. Each character is in its own range.

	//	End codes
	int ich;
	for (ich = 0; ich < cchOutput; ich++)
		pbstrm->WriteShort(rgchChars[ich]);

	//	reserved pad
	pbstrm->WriteShort(0);

	//	Start codes
	for (ich = 0; ich < cchOutput; ich++)
		pbstrm->WriteShort(rgchChars[ich]);

	//	id delta: set so that all the mappings point to glyph 0
	for (ich = 0; ich < cchOutput; ich++)
	{
		if (rgchChars[ich] == kchwSpace)
			pbstrm->WriteShort((rgchChars[ich] * - 1) + 2); // space is glyph #2; TODO: make this match minimal font
		else
			pbstrm->WriteShort(rgchChars[ich] * -1);
	}

	//	id range array: not used
	for (ich = 0; ich < cchOutput; ich++)
		pbstrm->WriteShort(0);

	long lPosEnd = pbstrm->Position();

	//	Fill in the length.
	int cb = lPosEnd - lPosStart;
	pbstrm->SetPosition(lPosLen);
	pbstrm->WriteShort(cb);

	//	Fill in the search variables; note that these are byte-based and so are multiplied by 2,
	//	unlike what is done in the Graphite tables.
	int nPowerOf2, nLog;
	BinarySearchConstants(cchOutput, &nPowerOf2, &nLog);
	pbstrm->SetPosition(lPosSearch);
	pbstrm->WriteShort(cchOutput << 1);	// * 2
	pbstrm->WriteShort(nPowerOf2 << 1); // * 2
	pbstrm->WriteShort(nLog);
	pbstrm->WriteShort((cchOutput - nPowerOf2) << 1);  // * 2

	pbstrm->SetPosition(lPosEnd);

	return cb;
}
#endif

/*----------------------------------------------------------------------------------------------
	Write the format 12 (platform 3 encoding 10) cmap subtable to the control file.
	Make every glyph point at the square box. Return the number of bytes in the table.

	Normally we will be generating the 3-10 table from the original 3-10 table. But in the
	case of a source font with a very large number of glyphs but no 3-10 table, we will
	generate it from the 3-1 table.
----------------------------------------------------------------------------------------------*/
int GrcManager::OutputCmap310Table(void * pCmapSubTblSrc, GrcBinaryStream * pbstrm,
	bool fFrom31)
{
	long lPosStart = pbstrm->Position();

	int cch = 0;	// number of items in the map

	//	format
	pbstrm->WriteShort(12);

	//	reserved
	pbstrm->WriteShort(0);

	//	length: fill in later
	int lPosLen = pbstrm->Position();
	pbstrm->WriteInt(0);

	//	language (irrelevant except on Macintosh)
	pbstrm->WriteInt(0);

	//	number of groups: fill in later
	int lPosNum = pbstrm->Position();
	pbstrm->WriteInt(0);

	//	Output the ranges of characters. Each character is in its own group.

	unsigned int chLast = (fFrom31) ? 0xFFFF : 0x10FFFF;

	int key = 0; // for optimizing next-codepoint lookup
	unsigned int ch = (fFrom31) ?
		TtfUtil::Cmap31NextCodepoint(pCmapSubTblSrc, 0, &key) :
		TtfUtil::Cmap310NextCodepoint(pCmapSubTblSrc, 0, &key);
	while (ch != chLast)
	{
		//	start code
		pbstrm->WriteInt(ch);
		//	end code
		pbstrm->WriteInt(ch);
		//	glyph id
		if (ch == kchwSpace)
			pbstrm->WriteInt(2); // space is glyph #2; TODO: make this match minimal font
		else
			pbstrm->WriteInt(0);

		cch++;

		ch = (fFrom31) ?
			TtfUtil::Cmap31NextCodepoint(pCmapSubTblSrc, ch, &key) :
			TtfUtil::Cmap310NextCodepoint(pCmapSubTblSrc, ch, &key);
	}

	int lPosEnd = pbstrm->Position();

	//	Fill in the length.
	int cb = lPosEnd - lPosStart;
	pbstrm->SetPosition(lPosLen);
	pbstrm->WriteInt(cb);

	//	Fill in the number of groups.
	pbstrm->SetPosition(lPosNum);
	pbstrm->WriteInt(cch);

	pbstrm->SetPosition(lPosEnd);

	return cb;
}

/*----------------------------------------------------------------------------------------------
	Write the Sile table onto the stream.
----------------------------------------------------------------------------------------------*/
void GrcManager::OutputSileTable(GrcBinaryStream * pbstrm,
	 utf16 * pchSrcFontFamily, char * pchSrcFileName, long luMasterChecksum,
	 unsigned int * pnCreateTime, unsigned int * pnModifyTime,
	 int * pibOffset, int * pcbSize)
{
	*pibOffset = pbstrm->Position();

	// version
	pbstrm->WriteInt(VersionForTable(ktiSile));

	// master check sum from source font
	pbstrm->WriteInt(luMasterChecksum);

	// create and modify times of source font
	pbstrm->WriteInt(pnCreateTime[0]);
	pbstrm->WriteInt(pnCreateTime[1]);
	pbstrm->WriteInt(pnModifyTime[0]);
	pbstrm->WriteInt(pnModifyTime[1]);

	// source font family name
	int cchFontFamily = utf16len(pchSrcFontFamily);
	pbstrm->WriteShort(cchFontFamily);
	for (int ich = 0; ich < cchFontFamily; ich++)
		pbstrm->WriteShort(pchSrcFontFamily[ich]);
	pbstrm->WriteByte(0);

	// source font file
	int cchFile = strlen(pchSrcFileName);
	pbstrm->WriteShort(cchFile);
	pbstrm->Write(pchSrcFileName, cchFile);
	pbstrm->WriteByte(0);

	// handle size and padding
	long lPos = pbstrm->Position();
	*pcbSize = lPos - *pibOffset;
	pbstrm->SeekPadLong(lPos);
}

/*----------------------------------------------------------------------------------------------
	Write the Gloc and Glat tables onto the stream.
----------------------------------------------------------------------------------------------*/
void GrcManager::OutputGlatAndGloc(GrcBinaryStream * pbstrm,
	int * pnGlocOffset, int * pnGlocSize, int * pnGlatOffset, int * pnGlatSize)
{
	int * prgibGlyphOffsets = new int[m_cwGlyphIDs + 1];
	memset(prgibGlyphOffsets, 0, isizeof(int)  * (m_cwGlyphIDs + 1));

	Symbol psymBw = m_psymtbl->FindSymbol("breakweight");
	int nAttrIdBw = psymBw->InternalID();
	//Symbol psymJStr = m_psymtbl->FindSymbol(GrcStructName("justify", "0", "stretch"));
	Symbol psymJStr = m_psymtbl->FindSymbol(GrcStructName("justify", "stretch"));
	int nAttrIdJStr = psymJStr->InternalID();

	//	Output the Glat table, recording the offsets in the array.

	*pnGlatOffset = pbstrm->Position();

	pbstrm->WriteInt(VersionForTable(ktiGlat));	// version number
	int cbOutput = 4;	// first glyph starts after the version number

	int wGlyphID;
	for (wGlyphID = 0; wGlyphID < m_cwGlyphIDs; wGlyphID++)
	{
		prgibGlyphOffsets[wGlyphID] = cbOutput;

		//	Convert breakweight values depending on the table version to output.
		ConvertBwForVersion(wGlyphID, nAttrIdBw);

		//	Split any large stretch values into two 16-bit words.
		SplitLargeStretchValue(wGlyphID, nAttrIdJStr);

		int nAttrIDMin = 0;
		while (nAttrIDMin < m_vpsymGlyphAttrs.Size())
		{
			int nValue;

			//	Skip undefined and zero-valued attributes.
			while (nAttrIDMin < m_vpsymGlyphAttrs.Size() &&
				FinalAttrValue(wGlyphID, nAttrIDMin) == 0)
			{
				nAttrIDMin++;
			}

			int nAttrIDLim = nAttrIDMin;
			Vector<int> vnValues;
			while (nAttrIDLim < m_vpsymGlyphAttrs.Size() &&
				(nAttrIDLim - nAttrIDMin + 1) < 256 &&
				((nValue = FinalAttrValue(wGlyphID, nAttrIDLim)) != 0))
			{
				nAttrIDLim++;
				//	TODO SharonC: Test that the value fits in 16 bits.
				vnValues.Push(nValue);
			}

			//	We've found a run of non-zero attributes. Output them.

			Assert(nAttrIDLim - nAttrIDMin == vnValues.Size());
			Assert(nAttrIDLim - nAttrIDMin < 256);
			if (nAttrIDLim > nAttrIDMin)
			{
				pbstrm->WriteByte(nAttrIDMin);
				pbstrm->WriteByte(nAttrIDLim - nAttrIDMin);
				for (int i = 0; i < vnValues.Size(); i++)
					pbstrm->WriteShort(vnValues[i]);
				cbOutput += (vnValues.Size() * 2) + 2;
			}

			nAttrIDMin = nAttrIDLim;
		}
	}

	//	Final offset to give total length.
	prgibGlyphOffsets[m_cwGlyphIDs] = cbOutput;

	// handle size and padding
	int nTmp = pbstrm->Position();
	*pnGlatSize = nTmp - *pnGlatOffset;
	pbstrm->SeekPadLong(nTmp);

	//	Output the Gloc table.
	*pnGlocOffset = pbstrm->Position();

	pbstrm->WriteInt(VersionForTable(ktiGloc));	// version number

	//	flags
	utf16 wFlags = 0;
	bool fNeedLongFormat = (cbOutput >= 0x0000FFFF);
	bool fAttrNames = false;
	if (fNeedLongFormat)
		wFlags |= 0x0001;
	if (fAttrNames)
		wFlags |= 0x0002;
	pbstrm->WriteShort(wFlags);

	//	number of attributes
	pbstrm->WriteShort(m_vpsymGlyphAttrs.Size());

	//	offsets
	for (wGlyphID = 0; wGlyphID <= m_cwGlyphIDs; wGlyphID++)
	{
		if (fNeedLongFormat)
			pbstrm->WriteInt(prgibGlyphOffsets[wGlyphID]);
		else
			pbstrm->WriteShort(prgibGlyphOffsets[wGlyphID]);

		// Just in case, since incrementing 0xFFFF will produce zero.
		if (wGlyphID == 0xFFFF)
			break;
	}

	// handle size and padding
	nTmp = pbstrm->Position();
	*pnGlocSize = nTmp - *pnGlocOffset;
	pbstrm->SeekPadLong(nTmp);

	//	debug names
	Assert(!fAttrNames);

	delete[] prgibGlyphOffsets;
}


/*----------------------------------------------------------------------------------------------
	Return the final attribute value, resolved to an integer.
----------------------------------------------------------------------------------------------*/
int GrcManager::FinalAttrValue(utf16 wGlyphID, int nAttrID)
{
	if (nAttrID < m_cpsymComponents)
	{
		Assert(!m_pgax->Defined(wGlyphID, nAttrID));

		if (!m_plclist->FindComponentFor(wGlyphID, nAttrID))
			return 0;
		else
			return m_cpsymComponents + (kFieldsPerComponent * nAttrID);
	}
	else
	{
		if (!m_pgax->Defined(wGlyphID, nAttrID))
			return 0;
		GdlExpression * pexpValue = m_pgax->GetExpression(wGlyphID, nAttrID);
		if (pexpValue == NULL)
			return 0;
		int nValue;
		bool f = pexpValue->ResolveToInteger(&nValue, false);
		Assert(f);
		if (!f)
			return 0;
		return nValue;
	}
}

/*----------------------------------------------------------------------------------------------
	Convert the breakweight value to the one appropriate for the version of the table
	being generated.
----------------------------------------------------------------------------------------------*/
void GrcManager::ConvertBwForVersion(int wGlyphID, int nAttrIdBw)
{
	int lb = FinalAttrValue(wGlyphID, nAttrIdBw);
	int lbOut;
	if (VersionForTable(ktiSilf) < 0x00020000)
	{
		switch(lb)
		{
		//case klbWsBreak:		lbOut = klbv1WordBreak; break;
		case klbWordBreak:		lbOut = klbv1WordBreak; break;
		case klbLetterBreak:	lbOut = klbv1LetterBreak; break;
		case klbClipBreak:		lbOut = klbv1ClipBreak; break;
		default:
			lbOut = lb;
		}
	}
	else
	{
		switch(lb)
		{
		//case klbWsBreak:		lbOut = klbv2WsBreak; break;
		case klbWordBreak:		lbOut = klbv2WordBreak; break;
		case klbLetterBreak:	lbOut = klbv2LetterBreak; break;
		case klbClipBreak:		lbOut = klbv2ClipBreak; break;
		default:
			lbOut = lb;
		}
	}
	if (lb != lbOut)
	{
		GdlExpression * pexpOld;
		int nPR;
		int munitPR;
		bool fOverride, fShadow;
		GrpLineAndFile lnf;
		m_pgax->Get(wGlyphID, nAttrIdBw,
			&pexpOld, &nPR, &munitPR, &fOverride, &fShadow, &lnf);

		GdlExpression * pexpNew = new GdlNumericExpression(lbOut);
		m_vpexpModified.Push(pexpNew);
		pexpNew->CopyLineAndFile(*pexpOld);

		m_pgax->Set(wGlyphID, nAttrIdBw,
			pexpNew, nPR, munitPR, true, false, lnf);
	}
}

/*----------------------------------------------------------------------------------------------
	Convert the specification version number requested by the user into the actual
	version for the given table.
----------------------------------------------------------------------------------------------*/
int GrcManager::VersionForTable(int ti)
{
	return VersionForTable(ti, FontTableVersion());
}

int GrcManager::VersionForTable(int ti, int fxdSpecVersion)
{
	switch (ti)
	{
	case ktiGloc:
	case ktiGlat:
		return 0x00010000;
	case ktiFeat:
		return m_fxdFeatVersion;
	case ktiSile:
		return 0x00010000;
	case ktiSill:
		return 0x00010000;
	case ktiSilf:
		if (fxdSpecVersion == 0x00010000)
			return 0x00010000;
		else if (fxdSpecVersion == 0x00020000)
			return 0x00020000;
		else
			// No version specified, or an invalid version:
			return kfxdCompilerVersion;
	default:
		Assert(false);
	}
	return fxdSpecVersion;
}

/*----------------------------------------------------------------------------------------------
	Return the version of the action commands to use for the rules.
----------------------------------------------------------------------------------------------*/
int GrcManager::VersionForRules()
{
	return VersionForTable(ktiSilf); // somebody these may be different
}

/*----------------------------------------------------------------------------------------------
	Split any really large stretch values into two 16-bit words. Store the high word in the
	stretchHW attribute. These are guaranteed to follow the list of stretch attributes (see
	GrcSymbolTable::AssignInternalGlyphAttrIDs).
----------------------------------------------------------------------------------------------*/
void GrcManager::SplitLargeStretchValue(int wGlyphID, int nAttrIdJStr)
{
	int cJLevels = NumJustLevels();

	for (int nJLev = 0; nJLev < cJLevels; nJLev++)
	{
		unsigned int nStretch = FinalAttrValue(wGlyphID, nAttrIdJStr + nJLev);
		if (nStretch > 0x0000FFFF)
		{
			unsigned int nStretchLW = nStretch & 0x0000FFFF;
			unsigned int nStretchHW = (nStretch & 0xFFFF0000) >> 16;

			int nAttrIdJStrHW = nAttrIdJStr + cJLevels;

			GdlExpression * pexpOld;
			int nPR;
			int munitPR;
			bool fOverride, fShadow;
			GrpLineAndFile lnf;
			m_pgax->Get(wGlyphID, nAttrIdJStr,
				&pexpOld, &nPR, &munitPR, &fOverride, &fShadow, &lnf);

			GdlExpression * pexpNew = new GdlNumericExpression(nStretchLW);
			m_vpexpModified.Push(pexpNew);
			pexpNew->CopyLineAndFile(*pexpOld);
			m_pgax->Set(wGlyphID, nAttrIdJStr,
				pexpNew, nPR, munitPR, true, false, lnf);

			pexpNew = new GdlNumericExpression(nStretchHW);
			m_vpexpModified.Push(pexpNew);
			pexpNew->CopyLineAndFile(*pexpOld);
			m_pgax->Set(wGlyphID, nAttrIdJStrHW,
				pexpNew, nPR, munitPR, true, false, lnf);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Write the Silf table and related subtables to the stream.
----------------------------------------------------------------------------------------------*/
void GrcManager::OutputSilfTable(GrcBinaryStream * pbstrm, int * pnSilfOffset, int * pnSilfSize)
{
	*pnSilfOffset = pbstrm->Position();

	int fxdVersion = VersionForTable(ktiSilf);

	//	version number
	pbstrm->WriteInt(fxdVersion);

	//	compiler version
	if (fxdVersion >= 0x00030000)
		pbstrm->WriteInt(kfxdCompilerVersion);

	//	number of sub-tables
	pbstrm->WriteShort(1);
	if (fxdVersion >= 0x00030000)
	{
		//	reserved - pad bytes
		pbstrm->WriteShort(0);
		//	offset of zeroth table relative to the start of this table
		pbstrm->WriteInt(isizeof(int) * 3 + isizeof(utf16) * 2);
	}
	else if (fxdVersion >= 0x00020000)
	{
		//	reserved - pad bytes
		pbstrm->WriteShort(0);
		//	offset of zeroth table relative to the start of this table
		pbstrm->WriteInt(isizeof(int) * 2 + isizeof(utf16) * 2);
	}
	else
		//	offset of zeroth table relative to the start of this table
		pbstrm->WriteInt(isizeof(int) + isizeof(utf16) + isizeof(int));

	//	Sub-table

	long lTableStartSub = pbstrm->Position();

	//	rule version - right now this is the same as the table version
	if (fxdVersion >= 0x00030000)
		pbstrm->WriteInt(fxdVersion);

	long lOffsetsPos = pbstrm->Position();
	if (fxdVersion >= 0x00030000)
	{
		// Place holders for offsets to passes and pseudo-glyphs.
		pbstrm->WriteShort(0);
		pbstrm->WriteShort(0);
	}

	//	maximum valid glyph ID
	pbstrm->WriteShort(m_cwGlyphIDs - 1);

	//	extra ascent
	GdlNumericExpression * pexp = m_prndr->ExtraAscent();
	if (pexp)
		pbstrm->WriteShort(pexp->Value());
	else
		pbstrm->WriteShort(0);
	//	extra descent
	pexp = m_prndr->ExtraDescent();
	if (pexp)
		pbstrm->WriteShort(pexp->Value());
	else
		pbstrm->WriteShort(0);

	//	number of passes
	int cpass, cpassLB, cpassSub, cpassPos, cpassJust, ipassBidi;
	m_prndr->CountPasses(&cpass, &cpassLB, &cpassSub, &cpassJust, &cpassPos, &ipassBidi);
	pbstrm->WriteByte(cpass);
	//	first substitution pass
	pbstrm->WriteByte(cpassLB);
	//	first positioning pass
	pbstrm->WriteByte(cpassLB + cpassSub + cpassJust);
	//	first justification pass
	if (fxdVersion < 0x00020000)
		pbstrm->WriteByte(cpass);
	else
		pbstrm->WriteByte(cpassLB + cpassSub);
	//	index of bidi pass
	pbstrm->WriteByte(ipassBidi);

	//	line-break flag
	pbstrm->WriteByte(m_prndr->LineBreakFlag());
	//	ranges for cross-line-boundary contextualization
	pbstrm->WriteByte(m_prndr->PreXlbContext());
	pbstrm->WriteByte(m_prndr->PostXlbContext());

	//	the fake glyph attribute that is used to store the actual glyph ID for pseudo-glyphs
	Symbol psym = m_psymtbl->FindSymbol("*actualForPseudo*");
	pbstrm->WriteByte(psym->InternalID());
	//	breakweight attribute ID
	psym = m_psymtbl->FindSymbol("breakweight");
	pbstrm->WriteByte(psym->InternalID());
	//	directionality attribute ID
	psym = m_psymtbl->FindSymbol("directionality");
	pbstrm->WriteByte(psym->InternalID());
	if (fxdVersion >= 0x00020000)
	{
		// reserved (pad bytes)
		pbstrm->WriteByte(0);
		pbstrm->WriteByte(0);

		if (m_fBasicJust)
		{
			pbstrm->WriteByte(0);	// number of levels
		}
		else
		{
			//	number of levels
			pbstrm->WriteByte(1);
			//	justify.0.stretch attribute ID
			GrcStructName xnsJStretch("justify", "stretch");
			psym = m_psymtbl->FindSymbol(xnsJStretch);
			Assert(psym);
			pbstrm->WriteByte(psym->InternalID());
			//	justify.0.shrink attribute ID
			GrcStructName xnsJShrink("justify", "shrink");
			psym = m_psymtbl->FindSymbol(xnsJShrink);
			Assert(psym);
			pbstrm->WriteByte(psym->InternalID());
			//	justify.0.step attribute ID
			GrcStructName xnsJStep("justify", "step");
			psym = m_psymtbl->FindSymbol(xnsJStep);
			Assert(psym);
			pbstrm->WriteByte(psym->InternalID());
			//	justify.0.weight attribute ID
			GrcStructName xnsJWeight("justify", "weight");
			psym = m_psymtbl->FindSymbol(xnsJWeight);
			//Assert(psym);
			pbstrm->WriteByte(psym ? psym->InternalID() : -1);
			//	runto
			pbstrm->WriteByte(0);
			//	reserved (pad bytes)
			pbstrm->WriteByte(0);
			pbstrm->WriteByte(0);
			pbstrm->WriteByte(0);
		}
	}

	//	number of initial attributes that represent ligature components
	pbstrm->WriteShort(m_cpsymComponents);

	//	number of user-defined slot attributes
	pbstrm->WriteByte(m_prndr->NumUserDefn());
	//	max number of ligature components
	pbstrm->WriteByte(m_prndr->NumLigComponents());

	//	directions supported
	byte  grfsdc = m_prndr->ScriptDirections();
	grfsdc = (grfsdc == 0) ? kfsdcHorizLtr : grfsdc;	// supply default--left-to-right
	pbstrm->WriteByte(grfsdc);
	//	reserved (pad bytes)
	pbstrm->WriteByte(0);
	pbstrm->WriteByte(0);
	pbstrm->WriteByte(0);

	if (fxdVersion >= 0x00020000)
	{
		//	reserved (pad byte)
		pbstrm->WriteByte(0);
		//	number of critical features
		pbstrm->WriteByte(0);
		//	TODO: include list of critical features
		//m_prndr->OutputCriticalFeatures(pbstrm, m_vCriticalFeatures);

		//	reserved (pad byte)
		pbstrm->WriteByte(0);
	}

	//	number of scripts supported
	int cScriptTags = m_prndr->NumScriptTags();
	pbstrm->WriteByte(cScriptTags);
	//	array of script tags
	int i;
	for (i = 0; i < cScriptTags; i++)
		pbstrm->WriteInt(m_prndr->ScriptTag(i));

	//	line break glyph ID
	pbstrm->WriteShort(m_wLineBreak);

	//	array of offsets to passes, relative to the start of this subtable--fill in later;
	//	for now, write (cpass + 1) place holders
	long lPassOffsetsPos = pbstrm->Position();
	long nPassOffset = lPassOffsetsPos - lTableStartSub;
	for (i = 0; i <= cpass; i++)
		pbstrm->WriteInt(0);

	//	number of pseudo mappings and search constants
	long nPseudoOffset = pbstrm->Position() - lTableStartSub;
	int n = m_vwPseudoForUnicode.Size();
	int nPowerOf2, nLog;
	BinarySearchConstants(n, &nPowerOf2, &nLog);
	pbstrm->WriteShort(n);
	pbstrm->WriteShort(nPowerOf2);
	pbstrm->WriteShort(nLog);
	pbstrm->WriteShort(n - nPowerOf2);

	//	array of unicode-to-pseudo mappings
	for (i = 0; i < m_vwPseudoForUnicode.Size(); i++)
	{
		if (fxdVersion < 0x00020000)
			pbstrm->WriteShort(m_vnUnicodeForPseudo[i]);
		else
			pbstrm->WriteInt(m_vnUnicodeForPseudo[i]);
		pbstrm->WriteShort(m_vwPseudoForUnicode[i]);
	}

	//	replacement classes
	m_prndr->OutputReplacementClasses(m_vpglfcReplcmtClasses, m_cpglfcLinear, pbstrm);

	//	passes
	Vector<int> vnPassOffsets;
	m_prndr->OutputPasses(this, pbstrm, lTableStartSub, vnPassOffsets);
	Assert(vnPassOffsets.Size() == cpass + 1);

	//	Now go back and fill in the offsets.

	long lSavePos = pbstrm->Position();

	if (fxdVersion >= 0x00030000)
	{
		pbstrm->SetPosition(lOffsetsPos);
		pbstrm->WriteShort(nPassOffset);
		pbstrm->WriteShort(nPseudoOffset);
	}

	pbstrm->SetPosition(lPassOffsetsPos);
	for (i = 0; i < vnPassOffsets.Size(); i++)
		pbstrm->WriteInt(vnPassOffsets[i]);

	pbstrm->SetPosition(lSavePos);

	// handle size and padding
	*pnSilfSize = lSavePos - *pnSilfOffset;
	pbstrm->SeekPadLong(lSavePos);
}


/*----------------------------------------------------------------------------------------------
	Write the list of replacement clases to the output stream. First write the classes that
	can be written in linear format (output classes), and then write the ones that are ordered
	by glyph ID and need a map to get the index. Note that some classes may serve as both
	input and output classes and are written twice.
	Arguments:
		vpglfc			- list of replacement classes (previously generated)
		cpgflcLinear	- number of classes that can be in linear format
		pbstrm			- output stream
----------------------------------------------------------------------------------------------*/
void GdlRenderer::OutputReplacementClasses(
	Vector<GdlGlyphClassDefn *> & vpglfcReplcmt, int cpglfcLinear,
	GrcBinaryStream * pbstrm)
{
	long lClassMapStart = pbstrm->Position();

	//	number of classes
	pbstrm->WriteShort(vpglfcReplcmt.Size());
	//	number that can be in linear format
	pbstrm->WriteShort(cpglfcLinear);

	//	offsets to classes--fill in later; for now output (# classes + 1) place holders
	Vector<int> vnClassOffsets;
	long lOffsetPos = pbstrm->Position();
	int ipglfc;
	for (ipglfc = 0; ipglfc <= vpglfcReplcmt.Size(); ipglfc++)
		pbstrm->WriteShort(0);

	//	linear classes (output)
	int cTmp = 0;
	for (ipglfc = 0; ipglfc < cpglfcLinear; ipglfc++)
	{
		GdlGlyphClassDefn * pglfc = vpglfcReplcmt[ipglfc];

		vnClassOffsets.Push(pbstrm->Position() - lClassMapStart);

		Assert(pglfc->ReplcmtOutputClass() || pglfc->GlyphIDCount() <= 1);
		//Assert(pglfc->ReplcmtOutputID() == cTmp);

		Vector<utf16> vwGlyphs;
		pglfc->GenerateOutputGlyphList(vwGlyphs);
		//	number of items and search constants
		//int n = vwGlyphs.Size();
		//int nPowerOf2, nLog;
		//BinarySearchConstants(n, &nPowerOf2, &nLog);
		//pbstrm->WriteShort(n);
		//pbstrm->WriteShort(nPowerOf2);
		//pbstrm->WriteShort(nLog);
		//pbstrm->WriteShort(n - nPowerOf2);
		//	glyph list
		for (int iw = 0; iw < vwGlyphs.Size(); iw++)
			pbstrm->WriteShort(vwGlyphs[iw]);

		cTmp++;
	}

	//	indexed classes (input)
	for (ipglfc = cpglfcLinear; ipglfc < vpglfcReplcmt.Size(); ipglfc++)
	{
		GdlGlyphClassDefn * pglfc = vpglfcReplcmt[ipglfc];

		vnClassOffsets.Push(pbstrm->Position() - lClassMapStart);

		Assert(pglfc->ReplcmtInputClass());
		Assert(pglfc->ReplcmtInputID() == cTmp);

		Vector<utf16> vwGlyphs;
		Vector<int> vnIndices;
		pglfc->GenerateInputGlyphList(vwGlyphs, vnIndices);
		//	number of items and search constants
		int n = vwGlyphs.Size();
		int nPowerOf2, nLog;
		BinarySearchConstants(n, &nPowerOf2, &nLog);
		pbstrm->WriteShort(n);
		pbstrm->WriteShort(nPowerOf2);
		pbstrm->WriteShort(nLog);
		pbstrm->WriteShort(n - nPowerOf2);
		//	glyph list
		for (int iw = 0; iw < vwGlyphs.Size(); iw++)
		{
			pbstrm->WriteShort(vwGlyphs[iw]);
			pbstrm->WriteShort(vnIndices[iw]);
		}

		cTmp++;
	}

	//	final offset giving length of block
	vnClassOffsets.Push(pbstrm->Position() - lClassMapStart);

	//	Now go back and fill in the offsets.
	long lSavePos = pbstrm->Position();

	pbstrm->SetPosition(lOffsetPos);
	for (int i = 0; i < vnClassOffsets.Size(); i++)
		pbstrm->WriteShort(vnClassOffsets[i]);

	pbstrm->SetPosition(lSavePos);
}


/*----------------------------------------------------------------------------------------------
	Generate a list of all the glyphs in the class, ordered by index (ie, ordered as listed
	in the program).
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::GenerateOutputGlyphList(Vector<utf16> & vwGlyphs)
{
	AddGlyphsToUnsortedList(vwGlyphs);
}


/*----------------------------------------------------------------------------------------------
	Add all the glyphs to the list in the order they were defined.
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::AddGlyphsToUnsortedList(Vector<utf16> & vwGlyphs)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
	{
		m_vpglfdMembers[iglfd]->AddGlyphsToUnsortedList(vwGlyphs);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphDefn::AddGlyphsToUnsortedList(Vector<utf16> & vwGlyphs)
{
	for (int iw = 0; iw < m_vwGlyphIDs.Size(); iw++)
	{
		vwGlyphs.Push(m_vwGlyphIDs[iw]);
	}
}


/*----------------------------------------------------------------------------------------------
	Generate a list of all the glyphs in the class, ordered by glyph ID. These will be
	output in linear format.
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::GenerateInputGlyphList(Vector<utf16> & vwGlyphs, Vector<int> & vnIndices)
{
	AddGlyphsToSortedList(vwGlyphs, vnIndices);
}


/*----------------------------------------------------------------------------------------------
	Add all the glyphs to the list, keeping the list sorted.
----------------------------------------------------------------------------------------------*/
void GdlGlyphClassDefn::AddGlyphsToSortedList(Vector<utf16> & vwGlyphs, Vector<int> & vnIndices)
{
	for (int iglfd = 0; iglfd < m_vpglfdMembers.Size(); iglfd++)
	{
		m_vpglfdMembers[iglfd]->AddGlyphsToSortedList(vwGlyphs, vnIndices);
	}
}

/*--------------------------------------------------------------------------------------------*/
void GdlGlyphDefn::AddGlyphsToSortedList(Vector<utf16> & vwGlyphs, Vector<int> & vnIndices)
{
	Assert(vwGlyphs.Size() == vnIndices.Size());

	for (int iw = 0; iw < m_vwGlyphIDs.Size(); iw++)
	{
		int nNextIndex = vwGlyphs.Size();

		utf16 w = m_vwGlyphIDs[iw];
		if (vwGlyphs.Size() == 0 ||
			w > *(vwGlyphs.Top()))	// common case
		{
			vwGlyphs.Push(w);
			vnIndices.Push(nNextIndex);
		}
		else
		{
			int iLow = 0;
			int iHigh = vwGlyphs.Size();

			while (iHigh - iLow > 1)
			{
				int iMid = (iHigh + iLow) >> 1;	// divide by 2
				if (w == vwGlyphs[iMid])
				{
					iLow = iMid;
					iHigh = iMid + 1;
				}
				else if (w < vwGlyphs[iMid])
					iHigh = iMid;
				else
					iLow = iMid;
			}

			if (w <= vwGlyphs[iLow])
			{
				vwGlyphs.Insert(iLow, w);
				vnIndices.Insert(iLow, nNextIndex);
			}
			else
			{
				Assert(iHigh == vwGlyphs.Size() || w < vwGlyphs[iHigh]);
				vwGlyphs.Insert(iLow + 1, w);
				vnIndices.Insert(iLow + 1, nNextIndex);
			}
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Count the passes for output to the font table.
----------------------------------------------------------------------------------------------*/
void GdlRenderer::CountPasses(int * pcpass, int * pcpassLB, int * pcpassSub,
	int * pcpassJust, int * pcpassPos, int * pipassBidi)
{
	GdlRuleTable * prultbl;

	if ((prultbl = FindRuleTable("linebreak")) != NULL)
		*pcpassLB = prultbl->CountPasses();
	else
		*pcpassLB = 0;

	if ((prultbl = FindRuleTable("substitution")) != NULL)
		*pcpassSub = prultbl->CountPasses();
	else
		*pcpassSub = 0;

	if ((prultbl = FindRuleTable("justification")) != NULL)
		*pcpassJust = prultbl->CountPasses();
	else
		*pcpassJust = 0;

	if ((prultbl = FindRuleTable("positioning")) != NULL)
		*pcpassPos = prultbl->CountPasses();
	else
		*pcpassPos = 0;

	*pcpass = *pcpassLB + *pcpassSub + *pcpassJust + *pcpassPos;

	if (m_fBidi)
		*pipassBidi = *pcpassLB + *pcpassSub;
	else
		*pipassBidi = -1;
}


/*--------------------------------------------------------------------------------------------*/
int GdlRuleTable::CountPasses()
{
	int cRet = 0;

	for (int ipass = 0; ipass < m_vppass.Size(); ++ipass)
	{
		if (m_vppass[ipass]->HasRules())
			cRet++;
	}
	return cRet;
}


/*----------------------------------------------------------------------------------------------
	Output the passes to the stream.
----------------------------------------------------------------------------------------------*/
void GdlRenderer::OutputPasses(GrcManager * pcman, GrcBinaryStream * pbstrm, long lTableStart,
	Vector<int> & vnOffsets)
{
	GdlRuleTable * prultbl;

	if ((prultbl = FindRuleTable("linebreak")) != NULL)
		prultbl->OutputPasses(pcman, pbstrm, lTableStart, vnOffsets);

	if ((prultbl = FindRuleTable("substitution")) != NULL)
		prultbl->OutputPasses(pcman, pbstrm, lTableStart, vnOffsets);

	if ((prultbl = FindRuleTable("justification")) != NULL)
		prultbl->OutputPasses(pcman, pbstrm, lTableStart, vnOffsets);

	if ((prultbl = FindRuleTable("positioning")) != NULL)
		prultbl->OutputPasses(pcman, pbstrm, lTableStart, vnOffsets);

	//	Push one more offset so the last pass can figure its length.
	vnOffsets.Push(pbstrm->Position() - lTableStart);
}


/*--------------------------------------------------------------------------------------------*/
void GdlRuleTable::OutputPasses(GrcManager * pcman, GrcBinaryStream * pbstrm, long lTableStart,
	Vector<int> & vnOffsets)
{
	for (int ipass = 0; ipass < m_vppass.Size(); ++ipass)
	{
		if (m_vppass[ipass]->HasRules())
		{
			vnOffsets.Push(pbstrm->Position() - lTableStart);
			m_vppass[ipass]->OutputPass(pcman, pbstrm, lTableStart);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Output the contents of the pass to the stream.
----------------------------------------------------------------------------------------------*/
void GdlPass::OutputPass(GrcManager * pcman, GrcBinaryStream * pbstrm, int lTableStart)
{
	long lPassStart = pbstrm->Position();

	int fxdSilfVersion = pcman->VersionForTable(ktiSilf);
	int fxdRuleVersion = fxdSilfVersion; // someday these may be different

	int nOffsetToPConstraint;
	long lOffsetToPConstraintPos;

	int nOffsetToConstraint;
	long lOffsetToConstraintPos;

	int nOffsetToAction;
	long lOffsetToActionPos;

	int nOffsetToDebugArrays;
	long lOffsetToDebugArraysPos;

	//	flags; TODO SharonC: figure out what should be in here
	pbstrm->WriteByte(0);
	//	MaxRuleLoop
	pbstrm->WriteByte(m_nMaxRuleLoop);
	//	max rule context
	pbstrm->WriteByte(m_nMaxRuleContext);
	//	MaxBackup
	pbstrm->WriteByte(m_nMaxBackup);
	//	number of rules
	pbstrm->WriteShort(m_vprule.Size());

	long lFsmOffsetPos = pbstrm->Position();
	if (fxdSilfVersion >= 0x00020000)
	{
		// offset to row information, or (<=v2) reserved
		pbstrm->WriteShort(0);
		// pass constraint byte count--fill in later
		lOffsetToPConstraintPos = pbstrm->Position();
		pbstrm->WriteInt(0);
	}

	//	offset to rule constraint code--fill in later
	lOffsetToConstraintPos = pbstrm->Position();
	pbstrm->WriteInt(0);
	//	offset to action code--fill in later
	lOffsetToActionPos = pbstrm->Position();
	pbstrm->WriteInt(0);
	//	offset to debug strings--fill in later
	lOffsetToDebugArraysPos = pbstrm->Position();
	pbstrm->WriteInt(0);

	long nFsmOffset = pbstrm->Position() - lPassStart;

	//	number of FSM rows
	pbstrm->WriteShort(NumStates());
	//	number of transitional states
	pbstrm->WriteShort(NumTransitionalStates());
	//	number of success states
	pbstrm->WriteShort(NumSuccessStates());
	//	number of columns
	pbstrm->WriteShort(m_pfsm->NumberOfColumns());
	//	number of glyph sub-ranges
	int n = TotalNumGlyphSubRanges();
	pbstrm->WriteShort(n);
	//	glyph sub-range search constants
	int nPowerOf2, nLog;
	BinarySearchConstants(n, &nPowerOf2, &nLog);
	pbstrm->WriteShort(nPowerOf2);
	pbstrm->WriteShort(nLog);
	pbstrm->WriteShort(n - nPowerOf2);

	//	glyph sub-ranges: for each glyph, find the machine class that includes it, if any,
	//	and output the range.
	utf16 w = 0;
	while (w < pcman->NumGlyphs())
	{
		for (int i = 0; i < m_vpfsmc.Size(); i++)
		{
			//	If this machine class includes this glyph, output the range on the stream
			//	and return the last glyph in the range. Otherwise return 0xFFFF.
			utf16 wLast = m_vpfsmc[i]->OutputRange(w, pbstrm);
			if (wLast != 0xFFFF)
			{
				w = wLast;
				break;
			}
		}
		w++;
	}

	//	rule list and offsets
	Vector<int> vnOffsets;
	Vector<int> vnRuleList;
	GenerateRuleMaps(vnOffsets, vnRuleList);
	int i;
	for (i = 0; i < vnOffsets.Size(); i++)
		pbstrm->WriteShort(vnOffsets[i]);
	for (i = 0; i < vnRuleList.Size(); i++)
		pbstrm->WriteShort(vnRuleList[i]);

	//	minRulePreContext
	pbstrm->WriteByte(m_critMinPreContext);
	//	maxRulePreContext
	pbstrm->WriteByte(m_critMaxPreContext);
	//	start states
	Assert(m_critMaxPreContext - m_critMinPreContext + 1 == m_vrowStartStates.Size());
	Assert(m_vrowStartStates[0] == 0);
	for (i = 0; i < m_vrowStartStates.Size(); i++)
		pbstrm->WriteShort(m_vrowStartStates[i]);

	//	rule sort keys
	for (i = 0; i < m_vprule.Size(); i++)
	{
		pbstrm->WriteShort(m_vprule[i]->SortKey());
	}

	//	pre-context item counts
	for (i = 0; i < m_vprule.Size(); i++)
	{
		pbstrm->WriteByte(m_vprule[i]->NumberOfPreModContextItems());
	}

	//	action and constraint offsets--fill in later;
	//	for now, write (# rules + 1) place holders
	Vector<int> vnActionOffsets;
	Vector<int> vnConstraintOffsets;
	long lCodeOffsets = pbstrm->Position();
	if (fxdSilfVersion >= 0x00020000)
	{
		// reserved - pad byte
		pbstrm->WriteByte(0);
		lCodeOffsets = pbstrm->Position();
		// pass constraint byte count
		pbstrm->WriteShort(0);
	}
	for (i = 0; i <= m_vprule.Size(); i++)
	{
		pbstrm->WriteShort(0);
		pbstrm->WriteShort(0);
	}

	//	transition table for states
	OutputFsmTable(pbstrm);

	//	constraint and action code

	int ib;

	int cbPassConstraint;
	if (fxdSilfVersion >= 0x00020000)
	{
		// reserved - pad byte
		pbstrm->WriteByte(0);

		nOffsetToPConstraint = pbstrm->Position() - lTableStart;
		Vector<byte> vbPassConstr;
		this->GenerateEngineCode(pcman, fxdRuleVersion, vbPassConstr);
		for (ib = 0; ib < vbPassConstr.Size(); ib++)
			pbstrm->WriteByte(vbPassConstr[ib]);
		cbPassConstraint = vbPassConstr.Size();
	}
	else
	{
		nOffsetToPConstraint = pbstrm->Position() - lTableStart;
		cbPassConstraint = 0;
	}


	//	ENHANCE: divide GenerateEngineCode into two methods.

	nOffsetToConstraint = pbstrm->Position() - lTableStart;

	//	Output a dummy byte just to keep any constraint from having zero as its offset,
	//	because we are using zero as a indicator that there are no constraints.
	pbstrm->WriteByte(0);

	Vector<byte> vbConstraints;
	Vector<byte> vbActions;
	int irule;
	for (irule = 0; irule < m_vprule.Size(); irule++)
	{
		vbConstraints.Clear();
		m_vprule[irule]->GenerateEngineCode(pcman, fxdRuleVersion, vbActions, vbConstraints);
		if (vbConstraints.Size() == 0)
			vnConstraintOffsets.Push(0);
		else
		{
			vnConstraintOffsets.Push(pbstrm->Position() - nOffsetToConstraint - lTableStart);
			for (ib = 0; ib < vbConstraints.Size(); ib++)
				pbstrm->WriteByte(vbConstraints[ib]);
		}
	}
	vnConstraintOffsets.Push(pbstrm->Position() - nOffsetToConstraint - lTableStart);

	nOffsetToAction = pbstrm->Position() - lTableStart;

	for (irule = 0; irule < m_vprule.Size(); irule++)
	{
		vbActions.Clear();
		vnActionOffsets.Push(pbstrm->Position() - nOffsetToAction - lTableStart);
		m_vprule[irule]->GenerateEngineCode(pcman, fxdRuleVersion, vbActions, vbConstraints);
		for (int ib = 0; ib < vbActions.Size(); ib++)
			pbstrm->WriteByte(vbActions[ib]);
	}
	vnActionOffsets.Push(pbstrm->Position() - nOffsetToAction - lTableStart);

	Assert(vnConstraintOffsets.Size() == m_vprule.Size() + 1);
	Assert(vnActionOffsets.Size() == m_vprule.Size() + 1);

	//	TODO: output debugger strings
	nOffsetToDebugArrays = 0;	// pbstrm->Position() - lTableStart;

	//	Now go back and fill in the offsets.

	long lSavePos = pbstrm->Position();

	if (fxdSilfVersion >= 0x00020000)
	{
		pbstrm->SetPosition(lOffsetToPConstraintPos);
		pbstrm->WriteInt(nOffsetToPConstraint);
	}
	pbstrm->SetPosition(lOffsetToConstraintPos);
	pbstrm->WriteInt(nOffsetToConstraint);
	pbstrm->SetPosition(lOffsetToActionPos);
	pbstrm->WriteInt(nOffsetToAction);
	pbstrm->SetPosition(lOffsetToDebugArraysPos);
	pbstrm->WriteInt(nOffsetToDebugArrays);

	pbstrm->SetPosition(lCodeOffsets);
	if (fxdSilfVersion >= 0x00020000)
		pbstrm->WriteShort(cbPassConstraint);
	for (i = 0; i < vnConstraintOffsets.Size(); i++)
		pbstrm->WriteShort(vnConstraintOffsets[i]);
	for (i = 0; i < vnActionOffsets.Size(); i++)
		pbstrm->WriteShort(vnActionOffsets[i]);

	if (fxdSilfVersion >= 0x00030000)
	{
		pbstrm->SetPosition(lFsmOffsetPos);
		pbstrm->WriteShort(nFsmOffset);
	}

	pbstrm->SetPosition(lSavePos);
}


/*----------------------------------------------------------------------------------------------
	If this machine class includes the given glyph ID, output the range on the stream
	and return the last glyph in the range; otherwise return 0xFFFF. Note that
	due to the way this method is used, we can assme the given glyph ID is the first glyph
	in the range.
----------------------------------------------------------------------------------------------*/
utf16 FsmMachineClass::OutputRange(utf16 wGlyphID, GrcBinaryStream * pbstrm)
{
	for (int iMin = 0; iMin < m_wGlyphs.Size(); iMin++)
	{
		if (m_wGlyphs[iMin] == wGlyphID)
		{
			//	This machine class includes the glyph. Search for the end of the range of
			//	contiguous glyphs.
			int iLim = iMin + 1;
			while (iLim < m_wGlyphs.Size() && m_wGlyphs[iLim] == m_wGlyphs[iLim - 1] + 1)
				iLim++;
			//	Write to the stream;
			pbstrm->WriteShort(m_wGlyphs[iMin]);
			pbstrm->WriteShort(m_wGlyphs[iLim - 1]);
			pbstrm->WriteShort(m_ifsmcColumn);
			return m_wGlyphs[iLim - 1];		// last glyph in range
		}
		else if (m_wGlyphs[iMin] > wGlyphID)
			return 0xFFFF;

	}
	return 0xFFFF;
}


/*----------------------------------------------------------------------------------------------
	Generate the lists of rule maps for states in the FSM. The second argment are the list
	of rules, the first argument are the offsets into the list for each success state.
	For instance, if you have the following success states:
		s3:  r1, r2, r5
		s4:  r0
		s5:  r3, r4
		s6:  r6
	you'll get the following:
		vnOffsets               vnRuleLists
			0						1
			3						2
			4						5
			6						0
			7						3
									4
									6
	(States 0 - 2 are omitted from the lists; only success states are included.)
----------------------------------------------------------------------------------------------*/
void GdlPass::GenerateRuleMaps(Vector<int> & vnOffsets, Vector<int> & vnRuleList)
{
	int ifsLim = m_vifsFinalToWork.Size();
	for (int ifs = 0; ifs < ifsLim; ifs++)
	{
		FsmState * pfstate = m_pfsm->StateAt(m_vifsFinalToWork[ifs]);

		Assert(!pfstate->HasBeenMerged());

		if (pfstate->NumberOfRulesSucceeded() > 0)
		{
			vnOffsets.Push(vnRuleList.Size());
			//	Make a sorted list of all the rule indices (this allows the rules to be
			//	tried in the order that they appeared in the source file).
			Vector<int> virule;
			for (Set<int>::iterator itset = pfstate->m_setiruleSuccess.Begin();
				itset != pfstate->m_setiruleSuccess.End();
				++itset)
			{
				for (int iirule = 0; iirule <= virule.Size(); iirule++)
				{
					if (iirule == virule.Size())
					{
						virule.Push(*itset);
						break;
					}
					else if (*itset < virule[iirule])
					{
						virule.Insert(iirule, *itset);
						break;
					}
				}
			}
			//	Now put them into the vector.
			for (int iirule = 0; iirule < virule.Size(); iirule++)
				vnRuleList.Push(virule[iirule]);
		}
		else
			//	All non-success states should be together at the beginning of the table.
			Assert(vnRuleList.Size() == 0);
	}

	//	Push a final offset, so that the last state can figure its length.
	vnOffsets.Push(vnRuleList.Size());
}


/*----------------------------------------------------------------------------------------------
	Output the transition table itself.
----------------------------------------------------------------------------------------------*/
void GdlPass::OutputFsmTable(GrcBinaryStream * pbstrm)
{
	int cfsmc = m_pfsm->NumberOfColumns();
	int ifsLim = m_vifsFinalToWork.Size();
	for (int ifs = 0; ifs < ifsLim; ifs++)
	{
		FsmState * pfstate = m_pfsm->StateAt(m_vifsFinalToWork[ifs]);

		Assert(!pfstate->HasBeenMerged());

		if (pfstate->AllCellsEmpty())
		{
			//	We've hit the end of the transitional states--quit.
			break;
		}

		for (int ifsmc = 0; ifsmc < cfsmc; ifsmc++)
		{
			int ifsmcValue = pfstate->CellValue(ifsmc);
			if (m_pfsm->RawStateAt(ifsmcValue)->HasBeenMerged())
				ifsmcValue = m_pfsm->RawStateAt(ifsmcValue)->MergedState()->WorkIndex();
			ifsmcValue = m_vifsWorkToFinal[ifsmcValue];

			//	Optimize: do some fancy footwork on the state number.
			pbstrm->WriteShort(ifsmcValue);
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Convenient wrapper to call the same method in GdlRenderer.
----------------------------------------------------------------------------------------------*/
bool GrcManager::AssignFeatTableNameIds(utf16 wFirstNameId, Vector<StrUni> * pvstuExtNames,
		Vector<utf16> * pvwLangIds, Vector<utf16> * pvwNameTblIds)
{
	return m_prndr->AssignFeatTableNameIds(wFirstNameId, pvstuExtNames, pvwLangIds,
		pvwNameTblIds);
}

/*----------------------------------------------------------------------------------------------
	Assign name table ids to each feature and all settings. The ids will be assigned
	sequentially beginning at wFirstNameId. Note that each string does NOT get its own id.
	A given feature can have several strings (names) each with a differnt lang id
	but all get the same name id.
	Return three vectors which each contain one element for each feature and setting.
	The vectors are parallel. Elements numbered n  contains the name string, lang id, and
	name table id for a given feature or setting.
----------------------------------------------------------------------------------------------*/
bool GdlRenderer::AssignFeatTableNameIds(utf16 wFirstNameId, Vector<StrUni> * pvstuExtNames,
		Vector<utf16> * pvwLangIds, Vector<utf16> * pvwNameTblIds)
{
	if (wFirstNameId > 32767) return false; // max allowed value
	utf16 wNameTblId = wFirstNameId;
	for (int ifeat = 0; ifeat < m_vpfeat.Size(); ifeat++)
	{
		wNameTblId = m_vpfeat[ifeat]->SetNameTblIds(wNameTblId);
		if (wNameTblId > 32767)
			return false;
		m_vpfeat[ifeat]->NameTblInfo(pvstuExtNames, pvwLangIds, pvwNameTblIds);
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Write the "Feat" table and related subtables to the stream.
----------------------------------------------------------------------------------------------*/
void GrcManager::OutputFeatTable(GrcBinaryStream * pbstrm, int * pnFeatOffset, int * pnFeatSize)
{
	*pnFeatOffset = pbstrm->Position();

	//	version number
	int fxdFeatVersion = VersionForTable(ktiFeat);
	pbstrm->WriteInt(fxdFeatVersion);

	m_prndr->OutputFeatTable(pbstrm, *pnFeatOffset, fxdFeatVersion);

	// handle size and padding
	int nTmp = pbstrm->Position();
	*pnFeatSize = nTmp - *pnFeatOffset;
	pbstrm->SeekPadLong(nTmp);
}

/*---------------------------------------------------------------------------------------------*/

void GdlRenderer::OutputFeatTable(GrcBinaryStream * pbstrm, long lTableStart,
	int fxdVersion)
{
	Vector<int> vnOffsets;
	Vector<long> vlOffsetPos;

	//	number of features
	pbstrm->WriteShort(m_vpfeat.Size());

	//	reserved
	pbstrm->WriteShort(0);
	pbstrm->WriteInt(0);

	int ifeat;
	for (ifeat = 0; ifeat < m_vpfeat.Size(); ifeat++)
	{
		//	feature id
		if (fxdVersion >= 0x00020000)
			pbstrm->WriteInt(m_vpfeat[ifeat]->ID());
		else
			pbstrm->WriteShort(m_vpfeat[ifeat]->ID());

		//	number of settings
		pbstrm->WriteShort(m_vpfeat[ifeat]->NumberOfSettings());

		if (fxdVersion >= 0x00020000)
			pbstrm->WriteShort(0); // pad bytes

		//	offset to setting--fill in later
		vlOffsetPos.Push(pbstrm->Position());
		pbstrm->WriteInt(0);

		//	flags
		pbstrm->WriteShort(0x8000);	// all our features are mutually exclusive

		//	name index for feature name
		pbstrm->WriteShort(m_vpfeat[ifeat]->NameTblId());
	}

	for (ifeat = 0; ifeat < m_vpfeat.Size(); ifeat++)
	{
		vnOffsets.Push(pbstrm->Position() - lTableStart);
		m_vpfeat[ifeat]->OutputSettings(pbstrm);
	}

	Assert(vnOffsets.Size() == m_vpfeat.Size());

	//	Now fill in the offsets.

	long lSavePos = pbstrm->Position();

	for (ifeat = 0; ifeat < vnOffsets.Size(); ifeat++)
	{
		pbstrm->SetPosition(vlOffsetPos[ifeat]);
		pbstrm->WriteInt(vnOffsets[ifeat]);
	}

	pbstrm->SetPosition(lSavePos);
}

/*---------------------------------------------------------------------------------------------*/

void GdlFeatureDefn::OutputSettings(GrcBinaryStream * pbstrm)
{
	//	first output the default setting
	if (m_pfsetDefault)
	{
		pbstrm->WriteShort(m_pfsetDefault->Value());
		pbstrm->WriteShort(m_pfsetDefault->NameTblId());	// name index
	}
	else if (m_vpfset.Size() == 0)
	{
		//	no settings (eg, 'lang' feature); write 0 as the default
		pbstrm->WriteShort(0);
		pbstrm->WriteShort(32767); // no name index - output largest legal value
	}

	for (int ifset = 0; ifset < m_vpfset.Size(); ifset++)
	{
		if (m_vpfset[ifset] != m_pfsetDefault)
		{
			pbstrm->WriteShort(m_vpfset[ifset]->Value());
			pbstrm->WriteShort(m_vpfset[ifset]->NameTblId());	// name index
		}
	}
}


/*----------------------------------------------------------------------------------------------
	Write the "Sill" table and related subtables to the stream.
----------------------------------------------------------------------------------------------*/
void GrcManager::OutputSillTable(GrcBinaryStream * pbstrm, int * pnSillOffset, int * pnSillSize)
{
	*pnSillOffset = pbstrm->Position();

	//	version number
	pbstrm->WriteInt(VersionForTable(ktiSill));

	m_prndr->OutputSillTable(pbstrm, *pnSillOffset);

	// handle size and padding
	int nTmp = pbstrm->Position();
	*pnSillSize = nTmp - *pnSillOffset;
	pbstrm->SeekPadLong(nTmp);
}

/*---------------------------------------------------------------------------------------------*/

void GdlRenderer::OutputSillTable(GrcBinaryStream * pbstrm, long lTableStart)
{
	// Note: if the format of the Sill table changes, the CheckLanguageFeatureSize method
	// needs to be changed to match.

	Vector<int> vnOffsets;
	Vector<long> vlOffsetPos;

	//	search constants
	int n = m_vplang.Size();
	int nPowerOf2, nLog;
	BinarySearchConstants(n, &nPowerOf2, &nLog);
	pbstrm->WriteShort(n); // number of languages
	pbstrm->WriteShort(nPowerOf2);
	pbstrm->WriteShort(nLog);
	pbstrm->WriteShort(n - nPowerOf2);

	int ilang;
	for (ilang = 0; ilang < m_vplang.Size(); ilang++)
	{
		//	language ID
		unsigned int nCode = m_vplang[ilang]->Code();
		char rgchCode[4];
		memcpy(rgchCode, &nCode, 4);
		pbstrm->Write(rgchCode, 4);

		//	number of settings
		pbstrm->WriteShort(m_vplang[ilang]->NumberOfSettings());

		//	offset to setting--fill in later
		vlOffsetPos.Push(pbstrm->Position());
		pbstrm->WriteShort(0);
	}
	// Extra bogus entry to make it easy to find length of last.
	pbstrm->WriteInt(0x80808080);
	pbstrm->WriteShort(0);
	vlOffsetPos.Push(pbstrm->Position());
	pbstrm->WriteShort(0);

	for (ilang = 0; ilang < m_vplang.Size(); ilang++)
	{
		vnOffsets.Push(pbstrm->Position() - lTableStart);
		m_vplang[ilang]->OutputSettings(pbstrm);
	}
	vnOffsets.Push(pbstrm->Position() - lTableStart); // offset of bogus entry gives length of last real one

	Assert(vnOffsets.Size() == m_vplang.Size() + 1);

	//	Now fill in the offsets.

	long lSavePos = pbstrm->Position();

	for (ilang = 0; ilang < vnOffsets.Size(); ilang++)
	{
		pbstrm->SetPosition(vlOffsetPos[ilang]);
		pbstrm->WriteShort(vnOffsets[ilang]);
	}

	pbstrm->SetPosition(lSavePos);
}

/*---------------------------------------------------------------------------------------------*/

void GdlLanguageDefn::OutputSettings(GrcBinaryStream * pbstrm)
{
	Assert(m_vpfeat.Size() == m_vnFset.Size());
	for (int ifset = 0; ifset < m_vpfset.Size(); ifset++)
	{
		pbstrm->WriteInt(m_vpfeat[ifset]->ID());	// feature ID
		pbstrm->WriteShort(m_vnFset[ifset]);		// value
		pbstrm->WriteShort(0);						// pad
	}
}

/*----------------------------------------------------------------------------------------------
	Calculate the standard search constants for the given number n:
		- max power of 2 < n
		- log-base-2(the number above)
----------------------------------------------------------------------------------------------*/
void BinarySearchConstants(int n, int * pnPowerOf2, int * pnLog)
{
	Assert(n >= 0);

	if (n == 0)
	{
		*pnPowerOf2 = 0;
		*pnLog = 0;
		return;
	}

	*pnPowerOf2 = 1;
	*pnLog = 0;

	while ((*pnPowerOf2 << 1) <= n)
	{
		*pnPowerOf2 = *pnPowerOf2 << 1;
		*pnLog = *pnLog + 1;
	}
}


/*----------------------------------------------------------------------------------------------
	Write a byte to the output stream.
----------------------------------------------------------------------------------------------*/
void GrcBinaryStream::WriteByte(int iOutput)
{
	Assert(((iOutput & 0xFFFFFF00) == 0xFFFFFF00) ||
		((iOutput & 0xFFFFFF00) == 0x00000000));

	char b = iOutput & 0x000000FF;
	write(&b, 1);
}

/*----------------------------------------------------------------------------------------------
	Write a short integer to the output stream.
----------------------------------------------------------------------------------------------*/
void GrcBinaryStream::WriteShort(int iOutput)
{
	Assert(((iOutput & 0xFFFF0000) == 0xFFFF0000) ||
		((iOutput & 0xFFFF0000) == 0x00000000));

	//	big-endian format:
	char b1 = (iOutput & 0x000000FF);
	char b2 = (iOutput & 0x0000FF00) >> 8;
	write(&b2, 1);
	write(&b1, 1);
}

/*----------------------------------------------------------------------------------------------
	Write a standard integer to the output stream.
----------------------------------------------------------------------------------------------*/
void GrcBinaryStream::WriteInt(int iOutput)
{
	//	big-endian format:
	char b1 = (iOutput & 0x000000FF);
	char b2 = (iOutput & 0x0000FF00) >> 8;
	char b3 = (iOutput & 0x00FF0000) >> 16;
	char b4 = (iOutput & 0xFF000000) >> 24;

	write(&b4, 1);
	write(&b3, 1);
	write(&b2, 1);
	write(&b1, 1);
}

/*----------------------------------------------------------------------------------------------
	Seek to ibOffset then add zero padding for long alignment.
	Return padded location.
----------------------------------------------------------------------------------------------*/
long GrcBinaryStream::SeekPadLong(long ibOffset)
{
	int cPad = (ibOffset + 3 & ~3) - ibOffset;
	seekp(ibOffset);
	if (cPad)
		write("\0\0\0", cPad);
	return tellp();
}

/*----------------------------------------------------------------------------------------------
	Write a byte to the output stream.
----------------------------------------------------------------------------------------------*/
void GrcSubStream::WriteByte(int iOutput)
{
	Assert(((iOutput & 0xFFFFFF00) == 0xFFFFFF00) ||
		((iOutput & 0xFFFFFF00) == 0x00000000));

	char b = iOutput & 0x000000FF;
	m_strm.write(&b, 1);
}

/*----------------------------------------------------------------------------------------------
	Write a short integer to the output stream.
----------------------------------------------------------------------------------------------*/
void GrcSubStream::WriteShort(int iOutput)
{
	Assert(((iOutput & 0xFFFF0000) == 0xFFFF0000) ||
		((iOutput & 0xFFFF0000) == 0x00000000));

	//	big-endian format:
	char b1 = (iOutput & 0x000000FF);
	char b2 = (iOutput & 0x0000FF00) >> 8;
	m_strm.write(&b2, 1);
	m_strm.write(&b1, 1);
}

/*----------------------------------------------------------------------------------------------
	Write a standard integer to the output stream.
----------------------------------------------------------------------------------------------*/
void GrcSubStream::WriteInt(int iOutput)
{
	//	big-endian format:
	char b1 = (iOutput & 0x000000FF);
	char b2 = (iOutput & 0x0000FF00) >> 8;
	char b3 = (iOutput & 0x00FF0000) >> 16;
	char b4 = (iOutput & 0xFF000000) >> 24;

	m_strm.write(&b4, 1);
	m_strm.write(&b3, 1);
	m_strm.write(&b2, 1);
	m_strm.write(&b1, 1);
}

/*----------------------------------------------------------------------------------------------
	Adjust argument for 4 byte padding.
----------------------------------------------------------------------------------------------*/
DWORD PadLong(DWORD ul)
{
	return ul + 3 & ~3;
}

/*----------------------------------------------------------------------------------------------
	Compare two table directory entries. Used by qsort().
----------------------------------------------------------------------------------------------*/
int CompareDirEntries(const void * ptr1, const void * ptr2)
{
	long lTmp1 = swapl(((sfnt_DirectoryEntry *)ptr1)->tag);
	long lTmp2 = swapl(((sfnt_DirectoryEntry *)ptr2)->tag);
	return (lTmp1 - lTmp2);
}

/*----------------------------------------------------------------------------------------------
	Calculate a checksum. cluSize is the byte count of the table pointed at by pluTable.
	The table must be padded to a length that is a multiple of four. cluSize includes the
	padding. The table is treated as a sequence of longs which are summed together.
----------------------------------------------------------------------------------------------*/

unsigned long CalcCheckSum(const void * pluTable, size_t cluSize)
{
	Assert(!(cluSize & 0x00000003));
	unsigned long  luCheckSum = 0;
	const DWORD *        element = static_cast<const DWORD *>(pluTable);
	const DWORD *const   end = element + cluSize / sizeof(DWORD);
	for (;element != end; ++element)
		luCheckSum += swapl(*element);

	return luCheckSum;
}

/***********************************************************************************************
	Debuggers
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Test the serialization process.
----------------------------------------------------------------------------------------------*/
void GrcManager::DebugOutput()
{
	GrcBinaryStream bstrm("testfont.ttt");

	bstrm.WriteInt(0);
	bstrm.WriteInt(0);
	bstrm.WriteInt(0);
	bstrm.WriteInt(0);

	int nGlocOffset, nGlocSize;
	int nGlatOffset, nGlatSize;
	int nSilOffset, nSilSize;
	int nFeatOffset, nFeatSize;

	OutputGlatAndGloc(&bstrm, &nGlocOffset, &nGlocSize, &nGlatOffset, &nGlatSize);
	OutputSilfTable(&bstrm, &nSilOffset, &nSilSize);
	OutputFeatTable(&bstrm, &nFeatOffset, &nFeatSize);

	long lSavePos = bstrm.Position();

	bstrm.SetPosition(0);
	bstrm.WriteInt(nSilOffset);
	bstrm.WriteInt(nGlocOffset);
	bstrm.WriteInt(nGlatOffset);
	bstrm.WriteInt(nFeatOffset);

	bstrm.SetPosition(lSavePos);
}
