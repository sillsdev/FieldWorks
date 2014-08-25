/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2010-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UniscribeLinux.cpp
Responsibility: Linux Team.
Last reviewed: Not yet.

Description: A minimal implementation of Uniscribe functions used by the FW uniscribe renderer.

-------------------------------------------------------------------------------*//*:End Ignore*/

#if WIN32
#error "UniscribeLinux.cpp should not be compiled on Windows"
#endif

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)


#include <cairomm/context.h>
#include <cairomm/surface.h>
#include <pango/pangocairo.h>
#include <pango/pango.h>
#include "UnicodeString8.h"

PangoFontMap* GetFontMap()
{
	static PangoFontMap* map = NULL;
	if (map == NULL)
		map = pango_cairo_font_map_get_default();

	return map;
}

struct ScriptCacheImplementation
{
	ScriptCacheImplementation() : m_pangoContext(NULL), m_vwGraphics(NULL)
	{
	}

	PangoContext* m_pangoContext;
	IVwGraphicsWin32 * m_vwGraphics;

};

void SetCachesVwGraphics(SCRIPT_CACHE *context, IVwGraphicsWin32* pvg)
{
	if (*context == NULL)
		*context = new ScriptCacheImplementation();

	ScriptCacheImplementation * cache = reinterpret_cast<ScriptCacheImplementation*>(*context);
	cache->m_vwGraphics = pvg;
}

/// Free upto a max of cGlyphs or upto first NULL.
void FreeGlyphs(WORD *pwGlyphs, int cGlyphs)
{
	PangoGlyphString ** glyphString = reinterpret_cast<PangoGlyphString**>(pwGlyphs);
	for(int i = 0 ; i < cGlyphs; ++i)
	{
		if (glyphString[i] == NULL)
			break;
		pango_glyph_string_free(glyphString[i]);
	}
}

PangoContext* GetPangoContext(SCRIPT_CACHE *context)
{
	if (*context == NULL)
		*context = new ScriptCacheImplementation();

	ScriptCacheImplementation * cache = reinterpret_cast<ScriptCacheImplementation*>(*context);

	if (cache->m_pangoContext == NULL)
		cache->m_pangoContext = pango_font_map_create_context(GetFontMap());

	return cache->m_pangoContext;
}

// TODO: refactor these helper functions to reduce code duplication and multiple calls to pangoItemize.

// Helper function used in the implementation of ScriptShape
HRESULT PangoCharsToGlyph(SCRIPT_CACHE *psc, const char * chars, int cInChars, int cMaxItems, PangoGlyphString **pGlpyhs, int * pcItems)
{
	ScriptCacheImplementation * cache = reinterpret_cast<ScriptCacheImplementation*>(*psc);
	PangoContext * pangoContext;
	cache->m_vwGraphics->GetTextStyleContext(reinterpret_cast<HDC*>(&pangoContext));

	PangoAttrList * attributes_list = pango_attr_list_new();
	GList * items = pango_itemize(pangoContext, chars, 0, cInChars, attributes_list, NULL);

	int length = g_list_length(items);

	int glyphsCount = 0;
	pGlpyhs[0] = NULL;
	for(int i = 0; i < length; ++i)
	{
		PangoItem* item = static_cast<PangoItem*>(g_list_nth_data(items, i));

		PangoGlyphString * ptrPangoGlyphString = pango_glyph_string_new();
		pango_shape(chars + item->offset, item->length, &item->analysis, ptrPangoGlyphString);
		glyphsCount += ptrPangoGlyphString->num_glyphs;

		if (glyphsCount > cMaxItems)
		{
			pango_glyph_string_free(ptrPangoGlyphString);
			FreeGlyphs(reinterpret_cast<WORD*>(pGlpyhs), cMaxItems);
			pango_item_free(item);
			pango_attr_list_unref(attributes_list);
			g_list_free(items);
			return E_OUTOFMEMORY;
		}

		pGlpyhs[i] = ptrPangoGlyphString;
		if (i < (cMaxItems - 1))
			pGlpyhs[i + 1] = NULL; // null term the list (used for deleting etc.)
		pango_item_free(item);
	}

	pango_attr_list_unref(attributes_list);
	g_list_free(items);

	*pcItems = glyphsCount;
	return S_OK;
}

// Helper function used in the implementation of ScriptItemize
HRESULT PangoItemize(const char * chars, int cInChars, int cMaxItems, SCRIPT_ITEM *pItems, int * pcItems)
{
	SCRIPT_CACHE context = NULL;

	PangoAttrList * attributes_list = pango_attr_list_new();
	GList * items = pango_itemize(GetPangoContext(&context), chars, 0, cInChars, attributes_list, NULL);

	*pcItems = g_list_length(items);

	if (*pcItems >= cMaxItems)
	{
		pango_attr_list_unref(attributes_list);
		g_list_free(items);
		return E_OUTOFMEMORY;
	}

	for(int i = 0; i < *pcItems; ++i)
	{
		PangoItem* item = static_cast<PangoItem*>(g_list_nth_data(items, i));
		pItems[i].iCharPos = item->offset;
		pItems[i].a.fRTL = item->analysis.level == 1;
		pItems[i].a.fLayoutRTL = pItems[i].a.fRTL;
		// TODO: set other fields in SCRIPT_ANALYSIS as needed.

		pango_item_free(item);
	}

	pango_attr_list_unref(attributes_list);
	g_list_free(items);

	return S_OK;
}


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
  /*__out*/    int *pcGlyphs)
{
	if (cMaxGlyphs < cChars)
		return E_OUTOFMEMORY;

	// TODO-Linux: make this more accurate.
	psva->uJustification = 2;
	psva->fClusterStart = 1;
	psva->fDiacritic = 0;
	psva->fZeroWidth = 0;
	psva->fReserved = 0;
	psva->fShapeReserved = 0;

	UnicodeString8 utf8(pwcChars, cChars);
	PangoCharsToGlyph(psc, utf8.c_str(), utf8.size(), cMaxGlyphs, reinterpret_cast<PangoGlyphString**>(pwOutGlyphs), pcGlyphs);

	for(int i = 0; i < cChars; ++i)
	{
		if (psa->fRTL)
			pwLogClust[i] = cChars - i + 1;
		else
			pwLogClust[i] = i;
	}

	return S_OK;
}

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
)
{
	ScriptCacheImplementation * cache = reinterpret_cast<ScriptCacheImplementation*>(*psc);
	int width, height;
	pABC->abcA = 0;
	pABC->abcB = 0;
	pABC->abcC = 0;

	PangoGlyphString ** glyphStrings = reinterpret_cast<PangoGlyphString**>(const_cast<WORD*>(pwGlyphs));
	int advanceIndex = 0;
	int totalWidth = 0;
	for(int i = 0; i < cGlyphs; ++i)
	{
		if (glyphStrings[i] == NULL)
			break;
		for(int j = 0; j < glyphStrings[i]->num_glyphs; ++j)
		{
			piAdvance[advanceIndex] = glyphStrings[i]->glyphs[j].geometry.width;
			totalWidth += piAdvance[advanceIndex];
			advanceIndex++;
		}
	}

	 pABC->abcB = PANGO_PIXELS(totalWidth);

	if (pGoffset)
	{
		// TODO Linux: implement
		pGoffset->du = 2;
		pGoffset->dv = 2;
	}

	return S_OK;
}

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
)
{
	// TODO-Linux: Implement (if we use this)
	return S_OK;
}

HRESULT ScriptCPtoXLeftToRight(
  /*__in*/   int iCP,
  /*__in*/   BOOL fTrailing,
  /*__in*/   int cChars,
  /*__in*/   int cGlyphs,
  /*__in*/   const WORD *pwLogClust,
  /*__in*/   const SCRIPT_VISATTR *psva,
  /*__in*/   const int *piAdvance,
  /*__in*/   const SCRIPT_ANALYSIS *psa,
  /*__out*/  int *piX
)
{
	// TODO: this implementation doesn't use pwLogClust
	// which means it doesn't handle scripts whose clusters are not single Glyphs

	// loop over cGlyphs to find X pos.
	*piX = 0;
	for(int i = 0; i < MIN(cGlyphs, iCP + (fTrailing ? 1 : 0)); ++i)
		*piX += piAdvance[i];

	*piX = PANGO_PIXELS(*piX);

	return S_OK;
}

HRESULT ScriptCPtoXRightToLeft(
  /*__in*/   int iCP,
  /*__in*/   BOOL fTrailing,
  /*__in*/   int cChars,
  /*__in*/   int cGlyphs,
  /*__in*/   const WORD *pwLogClust,
  /*__in*/   const SCRIPT_VISATTR *psva,
  /*__in*/   const int *piAdvance,
  /*__in*/   const SCRIPT_ANALYSIS *psa,
  /*__out*/  int *piX
)
{
	// TODO: this implementation doesn't use pwLogClust
	// which means it doesn't handle scripts whose clusters are not single Glyphs

	// loop over cGlyphs backwards to find X pos.
	*piX = 0;
	for(int i = cGlyphs - 1; i >= MAX(0, iCP + (fTrailing ? 1 : 0)); --i)
		*piX += piAdvance[i];

	*piX = PANGO_PIXELS(*piX);

	return S_OK;
}

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
)
{
	bool ltr = !psa->fRTL;

	if (ltr)
		return ScriptCPtoXLeftToRight(iCP, fTrailing, cChars, cGlyphs, pwLogClust, psva, piAdvance, psa, piX);
	else
		return ScriptCPtoXRightToLeft(iCP, fTrailing, cChars, cGlyphs, pwLogClust, psva, piAdvance, psa, piX);
}

HRESULT ScriptXtoCPLeftToRight(
  /*__in*/   int iX,
  /*__in*/   int cChars,
  /*__in*/   int cGlyphs,
  /*__in*/   const WORD *pwLogClust,
  /*__in*/   const SCRIPT_VISATTR *psva,
  /*__in*/   const int *piAdvance,
  /*__in*/   const SCRIPT_ANALYSIS *psa,
  /*__out*/  int *piCP,
  /*__out*/  int *piTrailing
)
{
	// TODO: this implementation doesn't use pwLogClust
	// which means it doesn't handle scripts whose clusters are not single Glyphs

	int totalRunWidth = 0;
	int pos = 0;
	int index = 0;

	// is iX isn't before run
	if  (iX < 0)
	{
		*piCP = -1;
		*piTrailing = 1;
		return S_OK;
	}

	for (index = 0; index < cGlyphs; ++index)
		totalRunWidth += piAdvance[index];

	totalRunWidth = PANGO_PIXELS(totalRunWidth);

	// is iX isn't after run
	if  (iX >= totalRunWidth)
	{
		*piCP = cChars;
		*piTrailing = 0;
		return S_OK;
	}

	// loop until pos in run is greater than or equal to iX
	for (index = 0; index < cGlyphs && pos < iX; ++index)
		pos += PANGO_PIXELS(piAdvance[index]);

	// trailing or leading edge?
	if  (pos - iX > PANGO_PIXELS(piAdvance[index]/2))
		*piTrailing = 0;
	else
		*piTrailing = 1;

	*piCP = index - 1;

	return S_OK;
}

HRESULT ScriptXtoCPRightToLeft(
  /*__in*/   int iX,
  /*__in*/   int cChars,
  /*__in*/   int cGlyphs,
  /*__in*/   const WORD *pwLogClust,
  /*__in*/   const SCRIPT_VISATTR *psva,
  /*__in*/   const int *piAdvance,
  /*__in*/   const SCRIPT_ANALYSIS *psa,
  /*__out*/  int *piCP,
  /*__out*/  int *piTrailing
)
{
	// TODO: this implementation doesn't use pwLogClust
	// which means it doesn't handle scripts whose clusters are not single Glyphs

	int totalRunWidth = 0;
	int pos = 0;
	int index = 0;

	// is iX isn't before run
	if  (iX < 0)
	{
		*piCP = cChars;
		*piTrailing = 0;
		return S_OK;
	}

	for (index = 0; index < cGlyphs; ++index)
		totalRunWidth += piAdvance[index];

	totalRunWidth = PANGO_PIXELS(totalRunWidth);

	// is iX after run
	if  (iX >= totalRunWidth)
	{
		*piCP = -1;
		*piTrailing = 1;
		return S_OK;
	}

	// loop until pos in run is greater than or equal to iX
	for (index = cGlyphs - 1; index >= 0 && pos < iX; --index)
		pos += PANGO_PIXELS(piAdvance[index]);

	// trailing or leading edge?
	if  (pos - iX > PANGO_PIXELS(piAdvance[index]/2))
		*piTrailing = 0;
	else
		*piTrailing = 1;

	*piCP = index + 1;

	Assert(*piCP >= 0);

	return S_OK;
}

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
)
{
	bool ltr = !psa->fRTL;

	if (ltr)
		return ScriptXtoCPLeftToRight(iX, cChars, cGlyphs, pwLogClust, psva, piAdvance, psa, piCP, piTrailing);
	else
		return ScriptXtoCPRightToLeft(iX, cChars, cGlyphs, pwLogClust, psva, piAdvance, psa, piCP, piTrailing);
}

HRESULT ScriptItemize(
  /*__in*/   const WCHAR *pwcInChars,
  /*__in*/   int cInChars,
  /*__in*/   int cMaxItems,
  /*__in*/   const SCRIPT_CONTROL *psControl,
  /*__in*/   const SCRIPT_STATE *psState,
  /*__out*/  SCRIPT_ITEM *pItems,
  /*__out*/  int *pcItems
)
{
	UnicodeString8 utf8(pwcInChars, cInChars);
	HRESULT hr = PangoItemize(utf8.c_str(), utf8.size(), cMaxItems, pItems, pcItems);
	if (hr != S_OK)
		return hr;

	// we seem to need to add an extra item to mark then end of the set of items.

	if (*pcItems + 1 > cMaxItems)
		return E_OUTOFMEMORY;

	pItems[*pcItems].iCharPos = cInChars;
	pItems[*pcItems].a.fRTL = false;
	pItems[*pcItems].a.eScript = 0;
	(*pcItems)++;

	return S_OK;
}

HRESULT ScriptFreeCache(
  /*__inout*/  SCRIPT_CACHE *psc
)
{
	if (*psc == NULL)
		return S_OK;

	delete reinterpret_cast<ScriptCacheImplementation*>(*psc);
	*psc = NULL;
	return S_OK;
}

HRESULT ScriptCacheGetHeight(
  /*__in*/     HDC hdc,
  /*__inout*/  SCRIPT_CACHE *psc,
  /*__out*/    long *tmHeight
)
{
	// TODO-Linux: Implement
	*tmHeight = 45;
	return S_OK;
}

HRESULT ScriptGetLogicalWidths(
  /*__in*/   const SCRIPT_ANALYSIS *psa,
  /*__in*/   int cChars,
  /*__in*/   int cGlyphs,
  /*__in*/   const int *piGlyphWidth,
  /*__in*/   const WORD *pwLogClust,
  /*__in*/   const SCRIPT_VISATTR *psva,
  /*__out*/  int *piDx
)
{
	// TODO-Linux: Possibly do a proper implementation.
	// Currently Assuming Logical Width are the same as GlyphWidth

	for (int i = 0 ; i < cGlyphs; ++i)
		piDx[i] =  PANGO_PIXELS(piGlyphWidth[i]);
	return S_OK;
}

HRESULT ScriptBreak(
  /*__in*/   const WCHAR *pwcChars,
  /*__in*/   int cChars,
  /*__in*/   const SCRIPT_ANALYSIS *psa,
  /*__out*/  SCRIPT_LOGATTR *psla
)
{
	// a very basic implementation of ScriptBreak that only looks for whitespace.
	for (int i = 0 ; i < cChars; ++i)
	{
		psla[i].fWhiteSpace = iswspace(pwcChars[i]);
	}

	return S_OK;
}

int GetDeviceCaps(
  /*__in*/  HDC hdc,
  /*__in*/  int nIndex
)
{
	return DT_RASDISPLAY;
}
