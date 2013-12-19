/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2010 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UniscribeLinux.h
Responsibility: Linux Team.
Last reviewed: Not yet.

Description: A minimal implementation of Uniscribe functions used by the FW uniscribe renderer.
-------------------------------------------------------------------------------*//*:End Ignore*/

#pragma once

#if WIN32
#error "UniscribeLinux.h should not be included on Windows"
#endif

//:>********************************************************************************************
//:>	   Uniscribe structs and types declarations
//:>********************************************************************************************

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd374046(VS.85).aspx
typedef struct tag_SCRIPT_VISATTR {
  WORD uJustification  :4;
  WORD fClusterStart  :1;
  WORD fDiacritic  :1;
  WORD fZeroWidth  :1;
  WORD fReserved  :1;
  WORD fShapeReserved  :8;
} SCRIPT_VISATTR;

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd318141(VS.85).aspx
typedef struct tagGOFFSET {
  LONG du;
  LONG dv;
} GOFFSET;

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd374043(VS.85).aspx
typedef struct tag_SCRIPT_STATE {
  WORD uBidiLevel  :5;
  WORD fOverrideDirection  :1;
  WORD fInhibitSymSwap  :1;
  WORD fCharShape  :1;
  WORD fDigitSubstitute  :1;
  WORD fInhibitLigate  :1;
  WORD fDisplayZWG  :1;
  WORD fArabicNumContext  :1;
  WORD fGcpClusters  :1;
  WORD fReserved  :1;
  WORD fEngineReserved  :2;
} SCRIPT_STATE;

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd368797(v=VS.85).aspx
typedef struct tag_SCRIPT_ANALYSIS {
  WORD         eScript  :10;
  WORD         fRTL  :1;
  WORD         fLayoutRTL  :1;
  WORD         fLinkBefore  :1;
  WORD         fLinkAfter  :1;
  WORD         fLogicalOrder  :1;
  WORD         fNoGlyphIndex  :1;
  SCRIPT_STATE s;
} SCRIPT_ANALYSIS;

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd374039(VS.85).aspx
typedef struct tag_SCRIPT_ITEM {
  int             iCharPos;
  SCRIPT_ANALYSIS a;
} SCRIPT_ITEM;

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd374041(VS.85).aspx
typedef struct tag_SCRIPT_LOGATTR {
  BYTE fSoftBreak  :1;
  BYTE fWhiteSpace  :1;
  BYTE fCharStop  :1;
  BYTE fWordStop  :1;
  BYTE fInvalid  :1;
  BYTE fReserved  :3;
} SCRIPT_LOGATTR;

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/ee491527.aspx
typedef void *SCRIPT_CACHE;

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd368800(VS.85).aspx
typedef struct tag_SCRIPT_CONTROL {
  DWORD uDefaultLanguage  :16;
  DWORD fContextDigits  :1;
  DWORD fInvertPreBoundDir  :1;
  DWORD fInvertPostBoundDir  :1;
  DWORD fLinkStringBefore  :1;
  DWORD fLinkStringAfter  :1;
  DWORD fNeutralOverride  :1;
  DWORD fNumericOverride  :1;
  DWORD fLegacyBidiClass  :1;
  DWORD fMergeNeutralItems  :1;
  DWORD fReserved  :7;
} SCRIPT_CONTROL;

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd162454(VS.85).aspx
typedef struct _ABC {
  int  abcA;
  UINT abcB;
  int  abcC;
} ABC, *PABC;

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd374040(VS.85).aspx
typedef enum tag_SCRIPT_JUSTIFY  {
  SCRIPT_JUSTIFY_NONE             = 0,
  SCRIPT_JUSTIFY_ARABIC_BLANK     = 1,
  SCRIPT_JUSTIFY_CHARACTER        = 2,
  SCRIPT_JUSTIFY_RESERVED1        = 3,
  SCRIPT_JUSTIFY_BLANK            = 4,
  SCRIPT_JUSTIFY_RESERVED2        = 5,
  SCRIPT_JUSTIFY_RESERVED3        = 6,
  SCRIPT_JUSTIFY_ARABIC_NORMAL    = 7,
  SCRIPT_JUSTIFY_ARABIC_KASHIDA   = 8,
  SCRIPT_JUSTIFY_ARABIC_ALEF      = 9,
  SCRIPT_JUSTIFY_ARABIC_HA        = 10,
  SCRIPT_JUSTIFY_ARABIC_RA        = 11,
  SCRIPT_JUSTIFY_ARABIC_BA        = 12,
  SCRIPT_JUSTIFY_ARABIC_BARA      = 13,
  SCRIPT_JUSTIFY_ARABIC_SEEN      = 14,
  SCRIPT_JUSTIFY_ARABIC_SEEN_M    = 15
} SCRIPT_JUSTIFY;

#define USP_E_SCRIPT_NOT_IN_FONT 0x80040200

#define SCRIPT_UNDEFINED 0

#define DT_RASDISPLAY 1

#define TECHNOLOGY 1

//:>********************************************************************************************
//:>	   Helper functions/class not part of the Uniscribe API.
//:>********************************************************************************************

void SetCachesVwGraphics(SCRIPT_CACHE *context, IVwGraphicsWin32* pvg);

void FreeGlyphs(WORD *pwGlyphs, int cGlyphs);

//:>********************************************************************************************
//:>	   Uniscribe functions
//:>********************************************************************************************

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd368564(VS.85).aspx
HRESULT ScriptShape(
  /*__in*/     HDC hdc,
  /*__inout*/  SCRIPT_CACHE *psc,
  /*__in*/     const WCHAR *pwcChars,
  /*__in*/     int cChars,
  /*__in*/     int cMaxGlyphs,
  /*__inout*/  SCRIPT_ANALYSIS *psa,
  /*__out*/    WORD *pwOutGlyphs,
  /*__out*/    WORD *pwLogClust,
  /*__out*/    SCRIPT_VISATTR *psva,
  /*__out*/    int *pcGlyphs);

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd368560(VS.85).aspx
HRESULT ScriptPlace(
  /*__in*/     HDC hdc,
  /*__inout*/  SCRIPT_CACHE *psc,
  /*__in*/     const WORD *pwGlyphs,
  /*__in*/     int cGlyphs,
  /*__in*/     const SCRIPT_VISATTR *psva,
  /*__inout*/  SCRIPT_ANALYSIS *psa,
  /*__out*/    int *piAdvance,
  /*__out*/    GOFFSET *pGoffset,
  /*__out*/    ABC *pABC
);

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd368795(VS.85).aspx
HRESULT ScriptTextOut(
  /*__in*/     const HDC hdc,
  /*__inout*/  SCRIPT_CACHE *psc,
  /*__in*/     int x,
  /*__in*/     int y,
  /*__in*/     UINT fuOptions,
  /*__in*/     const RECT *lprc,
  /*__in*/     const SCRIPT_ANALYSIS *psa,
  /*__in*/     const WCHAR *pwcReserved,
  /*__in*/     int iReserved,
  /*__in*/     const WORD *pwGlyphs,
  /*__in*/     int cGlyphs,
  /*__in*/     const int *piAdvance,
  /*__in*/     const int *piJustify,
  /*__in*/     const GOFFSET *pGoffset
);

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd319120(VS.85).aspx
HRESULT ScriptCPtoX(
  /*__in*/   int iCP,
  /*__in*/   BOOL fTrailing,
  /*__in*/   int cChars,
  /*__in*/   int cGlyphs,
  /*__in*/   const WORD *pwLogClust,
  /*__in*/   const SCRIPT_VISATTR *psva,
  /*__in*/   const int *piAdvance,
  /*__in*/   const SCRIPT_ANALYSIS *psa,
  /*__out*/  int *piX
);

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd368796(VS.85).aspx
HRESULT ScriptXtoCP(
  /*__in*/   int iX,
  /*__in*/   int cChars,
  /*__in*/   int cGlyphs,
  /*__in*/   const WORD *pwLogClust,
  /*__in*/   const SCRIPT_VISATTR *psva,
  /*__in*/   const int *piAdvance,
  /*__in*/   const SCRIPT_ANALYSIS *psa,
  /*__out*/  int *piCP,
  /*__out*/  int *piTrailing
);

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd368556(VS.85).aspx
HRESULT ScriptItemize(
  /*__in*/   const WCHAR *pwcInChars,
  /*__in*/   int cInChars,
  /*__in*/   int cMaxItems,
  /*__in*/   const SCRIPT_CONTROL *psControl,
  /*__in*/   const SCRIPT_STATE *psState,
  /*__out*/  SCRIPT_ITEM *pItems,
  /*__out*/  int *pcItems
);

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd319121(VS.85).aspx
HRESULT ScriptFreeCache(
  /*__inout*/  SCRIPT_CACHE *psc
);

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd319119(VS.85).aspx
HRESULT ScriptCacheGetHeight(
  /*__in*/     HDC hdc,
  /*__inout*/  SCRIPT_CACHE *psc,
  /*__out*/    long *tmHeight
);

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd368552(VS.85).aspx
HRESULT ScriptGetLogicalWidths(
  /*__in*/   const SCRIPT_ANALYSIS *psa,
  /*__in*/   int cChars,
  /*__in*/   int cGlyphs,
  /*__in*/   const int *piGlyphWidth,
  /*__in*/   const WORD *pwLogClust,
  /*__in*/   const SCRIPT_VISATTR *psva,
  /*__out*/  int *piDx
);

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd319118(VS.85).aspx
HRESULT ScriptBreak(
  /*__in*/   const WCHAR *pwcChars,
  /*__in*/   int cChars,
  /*__in*/   const SCRIPT_ANALYSIS *psa,
  /*__out*/  SCRIPT_LOGATTR *psla
);

// defined as described by msdn: http://msdn.microsoft.com/en-us/library/dd144877(VS.85).aspx
int GetDeviceCaps(
  /*__in*/  HDC hdc,
  /*__in*/  int nIndex
);
