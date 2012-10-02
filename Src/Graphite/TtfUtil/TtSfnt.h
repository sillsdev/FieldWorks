/*
	Original File:       sfnt.h

	Copyright:  c 1988-1990 by Apple Computer, Inc., all rights reserved.

	Modified 11/24/92 by Bob Hallissy: Add last 5 entries to OS/2 table
	Modified 4/25/2000 by Alan Ward
	Modified 9/4/2001 by Alan Ward: Add more entries to OS/2 table
	Modified 5/6/2003 by Alan Ward: Comment on OS/2 table for version > 2

*/

#ifndef SFNT_ENUMS
#include "TtSfnt_en.h"
#endif

typedef struct {
	uint32 bc;
	uint32 ad;
} BigDate;

typedef struct {
	sfnt_TableTag   tag;
	uint32          checkSum;
	uint32          offset;
	uint32          length;
} sfnt_DirectoryEntry;

/*
 *  The search fields limits numOffsets to 4096.
 */
typedef struct {
	int32 version;                  /* 0x10000 (1.0) */
	uint16 numOffsets;              /* number of tables */
	uint16 searchRange;             /* (max2 <= numOffsets)*16 */
	uint16 entrySelector;           /* log2 (max2 <= numOffsets) */
	uint16 rangeShift;              /* numOffsets*16-searchRange*/
	sfnt_DirectoryEntry table[1];   /* table[numOffsets] */
} sfnt_OffsetTable;
#define OFFSETTABLESIZE     12  /* not including any entries */

/*
 *  for the flags field
 */
#define Y_POS_SPECS_BASELINE            0x0001
#define X_POS_SPECS_LSB                 0x0002
#define HINTS_USE_POINTSIZE             0x0004
#define USE_INTEGER_SCALING             0x0008

#define SFNT_MAGIC 0x5F0F3CF5

#define SHORT_INDEX_TO_LOC_FORMAT       0
#define LONG_INDEX_TO_LOC_FORMAT        1
#define GLYPH_DATA_FORMAT               0

typedef struct {
	Fixed       version;            /* for this table, set to 1.0 */
	Fixed       fontRevision;       /* For Font Manufacturer */
	uint32      checkSumAdjustment;
	uint32      magicNumber;        /* signature, should always be 0x5F0F3CF5  == MAGIC */
	uint16      flags;
	uint16      unitsPerEm;         /* Specifies how many in Font Units we have per EM */

	BigDate     created;
	BigDate     modified;

	/** This is the font wide bounding box in ideal space
 (baselines and metrics are NOT worked into these numbers) **/
	FUnit       xMin;
	FUnit       yMin;
	FUnit       xMax;
	FUnit       yMax;

	uint16      macStyle;               /* macintosh style word */
	uint16      lowestRecPPEM;          /* lowest recommended pixels per Em */

	/* 0: fully mixed directional glyphs, 1: only strongly L->R or T->B glyphs,
	   -1: only strongly R->L or B->T glyphs, 2: like 1 but also contains neutrals,
	   -2: like -1 but also contains neutrals */
	int16       fontDirectionHint;

	int16       indexToLocFormat;
	int16       glyphDataFormat;
} sfnt_FontHeader;

typedef struct {
	Fixed       version;                /* for this table, set to 1.0 */

	FUnit       yAscender;
	FUnit       yDescender;
	FUnit       yLineGap;       /* Recommended linespacing = ascender - descender + linegap */
	uFUnit      advanceWidthMax;
	FUnit       minLeftSideBearing;
	FUnit       minRightSideBearing;
	FUnit       xMaxExtent; /* Max of (LSBi + (XMAXi - XMINi)), i loops through all glyphs */

	int16       horizontalCaretSlopeNumerator;
	int16       horizontalCaretSlopeDenominator;

	uint16      reserved0;
	uint16      reserved1;
	uint16      reserved2;
	uint16      reserved3;
	uint16      reserved4;

	int16       metricDataFormat;           /* set to 0 for current format */
	uint16      numberOf_LongHorMetrics;    /* if format == 0 */
} sfnt_HorizontalHeader;

typedef struct {
	Fixed       version;                /* for this table, set to 1.0 */
	uint16      numGlyphs;
	uint16      maxPoints;              /* in an individual glyph */
	uint16      maxContours;            /* in an individual glyph */
	uint16      maxCompositePoints;     /* in an composite glyph */
	uint16      maxCompositeContours;   /* in an composite glyph */
	uint16      maxElements;            /* set to 2, or 1 if no twilightzone points */
	uint16      maxTwilightPoints;      /* max points in element zero */
	uint16      maxStorage;             /* max number of storage locations */
	uint16      maxFunctionDefs;        /* max number of FDEFs in any preprogram */
	uint16      maxInstructionDefs;     /* max number of IDEFs in any preprogram */
	uint16      maxStackElements;       /* max number of stack elements for any individual glyph */
	uint16      maxSizeOfInstructions;  /* max size in bytes for any individual glyph */
	uint16      maxComponentElements;   /* number of glyphs referenced at top level */
	uint16      maxComponentDepth;      /* levels of recursion, 1 for simple components */
} sfnt_maxProfileTable;


typedef struct {
	uint16      advanceWidth;
	int16       leftSideBearing;
} sfnt_HorizontalMetrics;

/*
 *  CVT is just a bunch of int16s
 */
typedef int16 sfnt_ControlValue;

/*
 *  Char2Index structures, including platform IDs
 */
typedef struct {
	uint16  format;
	uint16  length;
	uint16  version; // 21 Mar 2002 spec shows this as language
} sfnt_mappingTable;

typedef struct {
	uint16  platformID;
	uint16  specificID;
	uint32  offset;
} sfnt_platformEntry;

typedef struct {
	uint16  version;
	uint16  numTables;
	sfnt_platformEntry platform[1]; /* platform[numTables] */
} sfnt_char2IndexDirectory;
#define SIZEOFCHAR2INDEXDIR     4

typedef struct {
	uint16 platformID;
	uint16 specificID;
	uint16 languageID;
	uint16 nameID;
	uint16 length;
	uint16 offset;
} sfnt_NameRecord;

typedef struct {
	uint16 format;
	uint16 count;
	uint16 stringOffset;
/*  sfnt_NameRecord[count]  */
} sfnt_NamingTable;


#define DEVEXTRA    2   /* size + max */
/*
 *  Each record is n+2 bytes, padded to long word alignment.
 *  First byte is ppem, second is maxWidth, rest are widths for each glyph
 */
typedef struct {
	int16               version;
	int16               numRecords;
	int32               recordSize;
	/* Byte widths[numGlyphs+2] * numRecords */
} sfnt_DeviceMetrics;

#ifdef UNNAMED_UNION            /* Anonymous unions are supported */
#define postScriptNameIndices   /* by some C implementations,  */
#endif                          /* but they are not portable. */

typedef struct {
	Fixed   version;                /* 1.0 */
	Fixed   italicAngle;
	FUnit   underlinePosition;
	FUnit   underlineThickness;
	int16   isFixedPitch;
	int16   pad;
	uint32  minMemType42;
	uint32  maxMemType42;
	uint32  minMemType1;
	uint32  maxMemType1;

	uint16  numberGlyphs;
	union
	{
	  uint16  glyphNameIndex[1];   /* version == 2.0 */
	  int8    glyphNameIndex25[1]; /* version == 2.5 */
	} postScriptNameIndices;
} sfnt_PostScriptInfo;

#ifdef postScriptNameIndices
#undef postScriptNameIndices
#endif

// masks to examine usSelection - AW Feb 2001
#define OS2_BOLD		((uint16)0x0020)
#define OS2_ITALIC		((uint16)0x0001)

typedef struct {
	uint16  Version;
	int16   xAvgCharWidth;
	uint16  usWeightClass;
	uint16  usWidthClass;
	int16   fsType;
	int16   ySubscriptXSize;
	int16   ySubscriptYSize;
	int16   ySubscriptXOffset;
	int16   ySubscriptYOffset;
	int16   ySuperScriptXSize;
	int16   ySuperScriptYSize;
	int16   ySuperScriptXOffset;
	int16   ySuperScriptYOffset;
	int16   yStrikeOutSize;
	int16   yStrikeOutPosition;
	int16   sFamilyClass;
	uint8   Panose [10];
	// uint32  ulCharRange [4];   Original definition. Now know it to be:
	uint32  ulUnicodeRange [4]; /* Added by Bob Hallissy */
	char    achVendID [4];
	uint16  usSelection;
	uint16  usFirstChar;
	uint16  usLastChar;
	uint16  sTypoAscender;      /* Added by Bob Hallissy */
	uint16  sTypoDescender;
	uint16  sTypoLineGap;
	uint16  usWinAscent;
	uint16  usWinDescent;
	uint32  ulCodePageRange [2];/* Added by Bob Hallissy; only present if Version > 0 */
	int16	sxHeight;		/* sxHeight - usMaxContext added by Alan Ward; only present if Version > 1 */
	int16	sCapHeight;     /* Alan W - Version > 2 changed the meaning of some fields but not table layout */
	uint16	usDefaultChar;
	uint16	usBreakChar;
	uint16	usMaxContext;
} sfnt_OS2;


/*
 * UNPACKING Constants
*/
#define ONCURVE             0x01
#define XSHORT              0x02
#define YSHORT              0x04
#define REPEAT_FLAGS        0x08 /* repeat flag n times */
/* IF XSHORT */
#define SHORT_X_IS_POS      0x10 /* the short vector is positive */
/* ELSE */
#define NEXT_X_IS_ZERO      0x10 /* the relative x coordinate is zero */
/* ENDIF */
/* IF YSHORT */
#define SHORT_Y_IS_POS      0x20 /* the short vector is positive */
/* ELSE */
#define NEXT_Y_IS_ZERO      0x20 /* the relative y coordinate is zero */
/* ENDIF */
/* 0x40 & 0x80              RESERVED
** Set to Zero
**
*/

/*
 * Composite glyph constants
 */
#define COMPONENTCTRCOUNT           -1      /* ctrCount == -1 for composite */
#define ARG_1_AND_2_ARE_WORDS       0x0001  /* if set args are words otherwise they are bytes */
#define ARGS_ARE_XY_VALUES          0x0002  /* if set args are xy values, otherwise they are points */
#define ROUND_XY_TO_GRID            0x0004  /* for the xy values if above is true */
#define WE_HAVE_A_SCALE             0x0008  /* Sx = Sy, otherwise scale == 1.0 */
#define NON_OVERLAPPING             0x0010  /* set to same value for all components */
#define MORE_COMPONENTS             0x0020  /* indicates at least one more glyph after this one */
#define WE_HAVE_AN_X_AND_Y_SCALE    0x0040  /* Sx, Sy */
#define WE_HAVE_A_TWO_BY_TWO        0x0080  /* t00, t01, t10, t11 */
#define WE_HAVE_INSTRUCTIONS        0x0100  /* instructions follow */
#define OVERLAPPING					0x0400	/* Components overlap */
// below added on 6/30/00 from MSDN CD (10/99 version)
#define SCALED_COMPONENT_OFFSET		0x0800	/* x & y offsets are scaled in Apple rasterizer */
#define UNSCALED_COMPONENT_OFFSET	0x1000	/* x & y offsets are unscaled in MS rasterize */

/*
 *  Private enums for tables used by the scaler.  See sfnt_Classify
 */
typedef enum {
	sfnt_fontHeader,
	sfnt_horiHeader,
	sfnt_indexToLoc,
	sfnt_maxProfile,
	sfnt_controlValue,
	sfnt_preProgram,
	sfnt_glyphData,
	sfnt_horizontalMetrics,
	sfnt_charToIndexMap,
	sfnt_fontProgram,
#ifdef PC_OS
	sfnt_Postscript,
	sfnt_HoriDeviceMetrics,
	sfnt_LinearThreeShold,
	sfnt_Names,
	sfnt_OS_2,
#endif
#ifdef FSCFG_USE_GLYPH_DIRECTORY
	sfnt_GlyphDirectory,
#endif
	sfnt_NUMTABLEINDEX
} sfnt_tableIndex;
