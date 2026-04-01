/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 2026 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "main.h"
#pragma hdrstop

#include "FontHandleCache.h"

#include <vector_i.cpp>
template Vector<HFONT>; // VecHfont;

FontHandleCache::FontHandleCache()
{
	m_cfceUsed = 0;
	memset(m_rgfce, 0, sizeof(m_rgfce));
}

HFONT FontHandleCache::FindCachedFont(const LgCharRenderProps * pchrp) const
{
	const int cbFontOffset = (int)offsetof(LgCharRenderProps, ttvBold);
	const int cbFontSize = isizeof(LgCharRenderProps) - cbFontOffset;

	for (int i = 0; i < m_cfceUsed; i++)
	{
		if (m_rgfce[i].fUsed &&
			memcmp(((byte *)pchrp) + cbFontOffset,
				((byte *)&m_rgfce[i].chrp) + cbFontOffset,
				cbFontSize) == 0)
		{
			return m_rgfce[i].hfont;
		}
	}
	return NULL;
}

void FontHandleCache::AddFontToCache(HFONT hfont, const LgCharRenderProps * pchrp,
	HFONT hfontActive, TryDeleteFontProc pfnTryDelete, void * pDeleteContext)
{
	TryDeleteDeferredFonts(hfontActive, pfnTryDelete, pDeleteContext);

	if (m_cfceUsed >= kcFontCacheMax)
	{
		int iEvict = 0;
		HFONT hfontEvicted = m_rgfce[iEvict].hfont;
		if (hfontEvicted == hfontActive)
		{
			Assert(false);
			for (int i = 1; i < m_cfceUsed; ++i)
			{
				if (m_rgfce[i].hfont && m_rgfce[i].hfont != hfontActive)
				{
					iEvict = i;
					hfontEvicted = m_rgfce[iEvict].hfont;
					break;
				}
			}
		}
		if (hfontEvicted == hfontActive)
		{
			Assert(false);
			return;
		}
		if (hfontEvicted)
		{
			bool fDeleted = pfnTryDelete ? pfnTryDelete(hfontEvicted, pDeleteContext) : true;
			if (!fDeleted)
				QueueFontForDeferredDelete(hfontEvicted);
		}
		if (iEvict < m_cfceUsed - 1)
		{
			memmove(&m_rgfce[iEvict], &m_rgfce[iEvict + 1],
				(m_cfceUsed - iEvict - 1) * sizeof(FontCacheEntry));
		}
		m_cfceUsed--;
	}

	m_rgfce[m_cfceUsed].hfont = hfont;
	m_rgfce[m_cfceUsed].chrp = *pchrp;
	m_rgfce[m_cfceUsed].fUsed = true;
	m_cfceUsed++;
}

void FontHandleCache::QueueFontForDeferredDelete(HFONT hfont)
{
	if (!hfont)
		return;

	for (int i = 0; i < m_vhfontDeferredDelete.Size(); ++i)
	{
		if (m_vhfontDeferredDelete[i] == hfont)
			return;
	}

	m_vhfontDeferredDelete.Push(hfont);
}

void FontHandleCache::TryDeleteDeferredFonts(HFONT hfontActive, TryDeleteFontProc pfnTryDelete,
	void * pDeleteContext)
{
	for (int i = m_vhfontDeferredDelete.Size() - 1; i >= 0; --i)
	{
		HFONT hfont = m_vhfontDeferredDelete[i];
		if (!hfont)
		{
			m_vhfontDeferredDelete.Delete(i);
			continue;
		}
		if (hfont == hfontActive)
			continue;

		bool fDeleted = pfnTryDelete ? pfnTryDelete(hfont, pDeleteContext) : true;
		if (fDeleted)
			m_vhfontDeferredDelete.Delete(i);
	}
}

void FontHandleCache::Clear(HFONT hfontActive, TryDeleteFontProc pfnTryDelete, void * pDeleteContext)
{
	for (int i = 0; i < m_cfceUsed; i++)
	{
		if (m_rgfce[i].hfont)
		{
			bool fDeleted = pfnTryDelete ? pfnTryDelete(m_rgfce[i].hfont, pDeleteContext) : true;
			if (!fDeleted)
				QueueFontForDeferredDelete(m_rgfce[i].hfont);
			m_rgfce[i].hfont = NULL;
		}
		m_rgfce[i].fUsed = false;
	}
	m_cfceUsed = 0;

	TryDeleteDeferredFonts(hfontActive, pfnTryDelete, pDeleteContext);
	for (int i = 0; i < m_vhfontDeferredDelete.Size(); ++i)
	{
		HFONT hfont = m_vhfontDeferredDelete[i];
		if (!hfont || hfont == hfontActive)
			continue;
		if (pfnTryDelete)
			pfnTryDelete(hfont, pDeleteContext);
	}
	m_vhfontDeferredDelete.Delete(0, m_vhfontDeferredDelete.Size());
}

int FontHandleCache::CacheCount() const
{
	return m_cfceUsed;
}

int FontHandleCache::DeferredDeleteCount() const
{
	return m_vhfontDeferredDelete.Size();
}

bool FontHandleCache::IsDeferredDeleteQueued(HFONT hfont) const
{
	for (int i = 0; i < m_vhfontDeferredDelete.Size(); ++i)
	{
		if (m_vhfontDeferredDelete[i] == hfont)
			return true;
	}
	return false;
}
