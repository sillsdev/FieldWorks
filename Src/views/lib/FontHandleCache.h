/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2026 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FONTHANDLECACHE_INCLUDED
#define FONTHANDLECACHE_INCLUDED

class FontHandleCache
{
public:
	typedef bool (*TryDeleteFontProc)(HFONT hfont, void * pContext);
	static const int kcFontCacheMax = 8;

	FontHandleCache();

	HFONT FindCachedFont(const LgCharRenderProps * pchrp) const;
	void AddFontToCache(HFONT hfont, const LgCharRenderProps * pchrp,
		HFONT hfontActive, TryDeleteFontProc pfnTryDelete, void * pDeleteContext);
	void TryDeleteDeferredFonts(HFONT hfontActive, TryDeleteFontProc pfnTryDelete,
		void * pDeleteContext);
	void Clear(HFONT hfontActive, TryDeleteFontProc pfnTryDelete, void * pDeleteContext);

	int CacheCount() const;
	int DeferredDeleteCount() const;
	bool IsDeferredDeleteQueued(HFONT hfont) const;

private:
	struct FontCacheEntry
	{
		HFONT hfont;
		LgCharRenderProps chrp;
		bool fUsed;
	};

	typedef Vector<HFONT> VecHfont;
	FontCacheEntry m_rgfce[kcFontCacheMax];
	int m_cfceUsed;
	VecHfont m_vhfontDeferredDelete;

	void QueueFontForDeferredDelete(HFONT hfont);
};

#endif // FONTHANDLECACHE_INCLUDED
