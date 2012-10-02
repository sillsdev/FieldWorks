/*  */
/*
 *  TT.H  --  Header file TrueType font structures and declarations
 *
 * Includes Apple supplied headers, then adds definitions they forgot.
 * Modified 4/25/2000 by Alan Ward
 * Modified 3/27/2003 by Alan Ward
 */


#ifndef TT_H
#define TT_H

// #ifdef __GNUC__
// #include "config.h"
// #endif

#ifndef WORDS_BIGENDIAN	/* only define PC_OS for little endian OS */
#define PC_OS           /* For TrueType header files from Apple */
#endif
#define UNNAMED_UNION   /* Microsoft C supports annonymous unions */

// Turn off compiler's default struct alignment so structs exactly match TTF file format
#pragma pack(push,enter_push,1) // Alan Ward
#include "TtFscdefs.h"
#include "TtSfnt_en.h"
#include "TtSfnt.h"

// use typedef for some missing types so they can be enclosed in a namespace and
// to be compatible with TfFscdefs.h, etc
typedef uint16 uFWord;
typedef int16 FWord;
typedef int16 F2DOT14;

/* Certain glyphs in the output font are fixed: */

#define MISSINGGLYPH    0
#define EMPTYGLYPH      1
#define SPACEGLYPH      2


/* The Apple-supplied header files didn't have definitions for entire
   cmap subtables, so here is the one I need */
// use uint16 instead of USHORT - see above AW

// MS documentation uses endCount and startCount instead of endCode and startCode,
// but the latter terms are used by Apple and make more sense to me. - SJC

typedef struct {
	uint16      format;
	uint16      length;
	uint16      version; // 21 Mar 2002 spec shows this as language
	uint16      segCountX2;
	uint16      searchRange;
	uint16      entrySelector;
	uint16      rangeShift;
	uint16      endCode[1];
	/* Start of remaining arrays need to be calculated:
	uint16      reserved[1]     = endCode       + segCount;
	uint16      startCode[]     = reserved      + 1;
	uint16      idDelta[]       = startCount    + segCount;
	uint16      idRangeOffset[] = idDelta       + segCount;
	uint16      glyphIdArray[]  = idRangeOffset + segCount;
	*/
} sfnt_Cmap4;

/* Add the sftn_Cmap12 structs to handle cmap format added for UCS-4 - AW */

typedef struct {
	uint32		startCharCode;
	uint32		endCharCode;
	uint32		startGlyphID;
} sfnt_Cmap12Group;

typedef struct {
	uint16		format;
	uint16		reserved;
	uint32		length;
	uint32		language;
	uint32		nGroups;
	sfnt_Cmap12Group	groupArray[1]; /* groupArray[nGroups] */
} sfnt_Cmap12;
#define SIZEOFCMAP12	16

// Add this typedef to parse the header for a glyf entry
typedef struct{
	int16 numberOfContours;
	FWord xMin;
	FWord yMin;
	FWord xMax;
	FWord yMax;
	uint16 endPtsOfContours[1];
} sfnt_GlyfHdr;

#pragma pack(pop,enter_push)

#endif
