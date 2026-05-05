/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 2026 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "main.h"
#pragma hdrstop

#include "ColorStateCache.h"

ColorStateCache::ColorStateCache()
{
	Invalidate();
}

void ColorStateCache::Invalidate()
{
	m_clrForeCache = CLR_INVALID;
	m_clrBackCache = CLR_INVALID;
	m_nBkModeCache = -1;
	m_fColorCacheValid = false;
}

bool ColorStateCache::ApplyIfNeeded(HDC hdc, COLORREF clrForeNeeded, COLORREF clrBackNeeded,
	int nBkModeNeeded)
{
	if (!m_fColorCacheValid
		|| clrForeNeeded != m_clrForeCache
		|| clrBackNeeded != m_clrBackCache
		|| nBkModeNeeded != m_nBkModeCache)
	{
		SmartPalette spal(hdc);
		bool fOK = (AfGfx::SetTextColor(hdc, clrForeNeeded) != CLR_INVALID);
		fOK = fOK && (AfGfx::SetBkColor(hdc, clrBackNeeded) != CLR_INVALID);
		fOK = fOK && ::SetBkMode(hdc, nBkModeNeeded);
		(void)fOK;

		m_clrForeCache = clrForeNeeded;
		m_clrBackCache = clrBackNeeded;
		m_nBkModeCache = nBkModeNeeded;
		m_fColorCacheValid = true;
		return true;
	}

	return false;
}
