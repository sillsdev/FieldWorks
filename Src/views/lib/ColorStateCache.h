/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2026 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef COLORSTATECACHE_INCLUDED
#define COLORSTATECACHE_INCLUDED

class ColorStateCache
{
public:
	ColorStateCache();

	void Invalidate();
	bool ApplyIfNeeded(HDC hdc, COLORREF clrForeNeeded, COLORREF clrBackNeeded, int nBkModeNeeded);

private:
	COLORREF m_clrForeCache;
	COLORREF m_clrBackCache;
	int m_nBkModeCache;
	bool m_fColorCacheValid;
};

#endif // COLORSTATECACHE_INCLUDED
