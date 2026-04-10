#pragma once
#ifndef LAYOUTCACHE_INCLUDED
#define LAYOUTCACHE_INCLUDED

class TextAnalysisEntry
{
public:
	TextAnalysisEntry() :
		m_pts(NULL),
		m_ichMin(0),
		m_cch(0),
		m_ws(0),
		m_fWsRtl(false),
		m_fTextIsNfc(true),
		m_cchNfc(0),
		m_citem(0)
	{
	}

	bool Covers(IVwTextSource * pts, int ichMin, int cch, int ws, bool fWsRtl) const
	{
		return m_pts == pts && m_ichMin == ichMin && m_cch >= cch && m_ws == ws && m_fWsRtl == fWsRtl;
	}

	int RequestedNfcLength(int cchRequested) const
	{
		if (cchRequested <= 0)
			return 0;
		if (m_fTextIsNfc)
			return cchRequested;
		if (m_vichOrigToNfc.Size() == 0)
			return cchRequested;
		if (cchRequested >= m_vichOrigToNfc.Size())
			return m_vichOrigToNfc[m_vichOrigToNfc.Size() - 1];
		return m_vichOrigToNfc[cchRequested];
	}

	int OffsetInNfc(int ich, int ichBase) const
	{
		Assert(ich >= ichBase);
		if (m_fTextIsNfc)
			return ich - ichBase;
		int ichRelative = ich - ichBase;
		if (ichRelative <= 0)
			return 0;
		if (m_vichOrigToNfc.Size() == 0)
			return ichRelative;
		if (ichRelative >= m_vichOrigToNfc.Size())
			return m_vichOrigToNfc[m_vichOrigToNfc.Size() - 1];
		return m_vichOrigToNfc[ichRelative];
	}

	int OffsetToOrig(int ich, int ichBase) const
	{
		if (m_fTextIsNfc)
			return ich + ichBase;
		if (ich <= 0)
			return ichBase;
		if (m_vichNfcToOrig.Size() == 0)
			return ich + ichBase;
		if (ich >= m_vichNfcToOrig.Size())
			return m_cch + ichBase;
		return m_vichNfcToOrig[ich] + ichBase;
	}

	void CopyScriptItemsTo(Vector<SCRIPT_ITEM> & vscri, int & citem) const
	{
		citem = m_citem;
		int cscri = m_vscri.Size();
		if (vscri.Size() < cscri)
			vscri.Resize(cscri);
		if (cscri > 0)
			::memcpy(vscri.Begin(), const_cast<Vector<SCRIPT_ITEM> &>(m_vscri).Begin(),
				cscri * isizeof(SCRIPT_ITEM));
	}

public:
	IVwTextSource * m_pts;
	int m_ichMin;
	int m_cch;
	int m_ws;
	bool m_fWsRtl;
	bool m_fTextIsNfc;
	int m_cchNfc;
	int m_citem;
	Vector<OLECHAR> m_vchNfc;
	Vector<SCRIPT_ITEM> m_vscri;
	Vector<int> m_vichOrigToNfc;
	Vector<int> m_vichNfcToOrig;
};

class ShapeRunEntry
{
public:
	ShapeRunEntry() :
		m_hfont(NULL),
		m_cch(0),
		m_cglyph(0),
		m_dxdWidth(0),
		m_fScriptPlaceFailed(false)
	{
		::ZeroMemory(&m_sa, sizeof(m_sa));
	}

	bool Matches(const OLECHAR * prgch, int cch, HFONT hfont, const SCRIPT_ANALYSIS & sa)
	{
		if (m_hfont != hfont || m_cch != cch)
			return false;
		if (::memcmp(&m_sa, &sa, sizeof(SCRIPT_ANALYSIS)) != 0)
			return false;
		if (cch == 0)
			return true;
		return ::memcmp(m_vch.Begin(), prgch, cch * isizeof(OLECHAR)) == 0;
	}

	HFONT m_hfont;
	SCRIPT_ANALYSIS m_sa;
	int m_cch;
	int m_cglyph;
	int m_dxdWidth;
	bool m_fScriptPlaceFailed;
	Vector<OLECHAR> m_vch;
	Vector<WORD> m_vglyph;
	Vector<SCRIPT_VISATTR> m_vsva;
	Vector<int> m_vadvance;
	Vector<int> m_vcst;
	Vector<GOFFSET> m_voff;
	Vector<WORD> m_vcluster;
};

class TextAnalysisCache
{
public:
	TextAnalysisCache(int cEntriesMax = 16) :
		m_cEntriesMax(cEntriesMax),
		m_ientryReplace(0),
		m_cHit(0),
		m_cMiss(0),
		m_cEvict(0),
		m_msCompute(0)
	{
	}

	void Reset()
	{
		m_ventry.Delete(0, m_ventry.Size());
		m_ientryReplace = 0;
		m_cHit = 0;
		m_cMiss = 0;
		m_cEvict = 0;
		m_msCompute = 0;
	}

	TextAnalysisEntry * Find(IVwTextSource * pts, int ichMin, int cch, int ws, bool fWsRtl)
	{
		TextAnalysisEntry * pbest = NULL;
		int cchBest = INT_MAX;
		for (int ientry = 0; ientry < m_ventry.Size(); ++ientry)
		{
			TextAnalysisEntry & entry = m_ventry[ientry];
			if (!entry.Covers(pts, ichMin, cch, ws, fWsRtl))
				continue;
			if (entry.m_cch < cchBest)
			{
				pbest = &entry;
				cchBest = entry.m_cch;
			}
		}
		if (pbest)
			++m_cHit;
		else
			++m_cMiss;
		return pbest;
	}

	TextAnalysisEntry * Store(IVwTextSource * pts, int ichMin, int cch, int ws, bool fWsRtl,
		const OLECHAR * prgchNfc, int cchNfc, bool fTextIsNfc, const SCRIPT_ITEM * prgscri,
		int citem, const Vector<int> * pvichOrigToNfc, const Vector<int> * pvichNfcToOrig)
	{
		TextAnalysisEntry * pentry = NULL;
		for (int ientry = 0; ientry < m_ventry.Size(); ++ientry)
		{
			TextAnalysisEntry & entry = m_ventry[ientry];
			if (entry.m_pts == pts && entry.m_ichMin == ichMin && entry.m_cch == cch &&
				entry.m_ws == ws && entry.m_fWsRtl == fWsRtl)
			{
				pentry = &entry;
				break;
			}
		}

		if (!pentry)
		{
			if (m_ventry.Size() < m_cEntriesMax)
			{
				TextAnalysisEntry entry;
				m_ventry.Push(entry);
				pentry = &m_ventry[m_ventry.Size() - 1];
			}
			else
			{
				pentry = &m_ventry[m_ientryReplace];
				m_ientryReplace = (m_ientryReplace + 1) % m_cEntriesMax;
				++m_cEvict;
			}
		}

		pentry->m_pts = pts;
		pentry->m_ichMin = ichMin;
		pentry->m_cch = cch;
		pentry->m_ws = ws;
		pentry->m_fWsRtl = fWsRtl;
		pentry->m_fTextIsNfc = fTextIsNfc;
		pentry->m_cchNfc = cchNfc;
		pentry->m_citem = citem;

		pentry->m_vchNfc.Resize(cchNfc);
		if (cchNfc > 0)
			::memcpy(pentry->m_vchNfc.Begin(), prgchNfc, cchNfc * isizeof(OLECHAR));

		int cscri = citem + 1;
		if (cscri < 2)
			cscri = 2;
		pentry->m_vscri.Resize(cscri);
		if (cscri > 0)
			::memcpy(pentry->m_vscri.Begin(), prgscri, cscri * isizeof(SCRIPT_ITEM));

		if (pvichOrigToNfc)
		{
			pentry->m_vichOrigToNfc.Resize(pvichOrigToNfc->Size());
			for (int i = 0; i < pvichOrigToNfc->Size(); ++i)
				pentry->m_vichOrigToNfc[i] = (*pvichOrigToNfc)[i];
		}
		else
			pentry->m_vichOrigToNfc.Delete(0, pentry->m_vichOrigToNfc.Size());

		if (pvichNfcToOrig)
		{
			pentry->m_vichNfcToOrig.Resize(pvichNfcToOrig->Size());
			for (int i = 0; i < pvichNfcToOrig->Size(); ++i)
				pentry->m_vichNfcToOrig[i] = (*pvichNfcToOrig)[i];
		}
		else
			pentry->m_vichNfcToOrig.Delete(0, pentry->m_vichNfcToOrig.Size());

		return pentry;
	}

	int HitCount() const { return m_cHit; }
	int MissCount() const { return m_cMiss; }
	int EvictionCount() const { return m_cEvict; }
	int RequestCount() const { return m_cHit + m_cMiss; }
	DWORD ComputeMs() const { return m_msCompute; }
	void AddComputeMs(DWORD ms) { m_msCompute += ms; }

private:
	Vector<TextAnalysisEntry> m_ventry;
	int m_cEntriesMax;
	int m_ientryReplace;
	int m_cHit;
	int m_cMiss;
	int m_cEvict;
	DWORD m_msCompute;
};

class ShapeRunCache
{
public:
	ShapeRunCache(int cEntriesMax = 32) :
		m_cEntriesMax(cEntriesMax),
		m_ientryReplace(0),
		m_cHit(0),
		m_cMiss(0),
		m_cEvict(0),
		m_msCompute(0)
	{
	}

	void Reset()
	{
		m_ventry.Delete(0, m_ventry.Size());
		m_ientryReplace = 0;
		m_cHit = 0;
		m_cMiss = 0;
		m_cEvict = 0;
		m_msCompute = 0;
	}

	ShapeRunEntry * Find(const OLECHAR * prgch, int cch, HFONT hfont, const SCRIPT_ANALYSIS & sa)
	{
		for (int ientry = 0; ientry < m_ventry.Size(); ++ientry)
		{
			ShapeRunEntry & entry = m_ventry[ientry];
			if (entry.Matches(prgch, cch, hfont, sa))
			{
				++m_cHit;
				return &entry;
			}
		}
		++m_cMiss;
		return NULL;
	}

	ShapeRunEntry * Store(const OLECHAR * prgch, int cch, HFONT hfont, const SCRIPT_ANALYSIS & sa,
		const WORD * prgGlyph, const SCRIPT_VISATTR * prgsva, const int * prgAdvance,
		const int * prgcst, const GOFFSET * prgoff, const WORD * prgCluster, int cglyph,
		int dxdWidth, bool fScriptPlaceFailed)
	{
		ShapeRunEntry * pentry = NULL;
		for (int ientry = 0; ientry < m_ventry.Size(); ++ientry)
		{
			ShapeRunEntry & entry = m_ventry[ientry];
			if (entry.Matches(prgch, cch, hfont, sa))
			{
				pentry = &entry;
				break;
			}
		}

		if (!pentry)
		{
			if (m_ventry.Size() < m_cEntriesMax)
			{
				ShapeRunEntry entry;
				m_ventry.Push(entry);
				pentry = &m_ventry[m_ventry.Size() - 1];
			}
			else
			{
				pentry = &m_ventry[m_ientryReplace];
				m_ientryReplace = (m_ientryReplace + 1) % m_cEntriesMax;
				++m_cEvict;
			}
		}

		pentry->m_hfont = hfont;
		pentry->m_sa = sa;
		pentry->m_cch = cch;
		pentry->m_cglyph = cglyph;
		pentry->m_dxdWidth = dxdWidth;
		pentry->m_fScriptPlaceFailed = fScriptPlaceFailed;

		pentry->m_vch.Resize(cch);
		if (cch > 0)
			::memcpy(pentry->m_vch.Begin(), prgch, cch * isizeof(OLECHAR));

		pentry->m_vglyph.Resize(cglyph);
		pentry->m_vsva.Resize(cglyph);
		pentry->m_vadvance.Resize(cglyph);
		pentry->m_vcst.Resize(cglyph);
		pentry->m_voff.Resize(cglyph);
		if (cglyph > 0)
		{
			::memcpy(pentry->m_vglyph.Begin(), prgGlyph, cglyph * isizeof(WORD));
			::memcpy(pentry->m_vsva.Begin(), prgsva, cglyph * isizeof(SCRIPT_VISATTR));
			::memcpy(pentry->m_vadvance.Begin(), prgAdvance, cglyph * isizeof(int));
			::memcpy(pentry->m_vcst.Begin(), prgcst, cglyph * isizeof(int));
			::memcpy(pentry->m_voff.Begin(), prgoff, cglyph * isizeof(GOFFSET));
		}

		pentry->m_vcluster.Resize(cch);
		if (cch > 0)
			::memcpy(pentry->m_vcluster.Begin(), prgCluster, cch * isizeof(WORD));

		return pentry;
	}

	int HitCount() const { return m_cHit; }
	int MissCount() const { return m_cMiss; }
	int EvictionCount() const { return m_cEvict; }
	int RequestCount() const { return m_cHit + m_cMiss; }
	DWORD ComputeMs() const { return m_msCompute; }
	void AddComputeMs(DWORD ms) { m_msCompute += ms; }

private:
	Vector<ShapeRunEntry> m_ventry;
	int m_cEntriesMax;
	int m_ientryReplace;
	int m_cHit;
	int m_cMiss;
	int m_cEvict;
	DWORD m_msCompute;
};

class LayoutPassCache
{
public:
	LayoutPassCache() : m_analysisCache(16), m_shapeRunCache(32)
	{
	}

	void Reset()
	{
		m_analysisCache.Reset();
		m_shapeRunCache.Reset();
	}

	TextAnalysisCache & AnalysisCache()
	{
		return m_analysisCache;
	}

	ShapeRunCache & ShapeCache()
	{
		return m_shapeRunCache;
	}

private:
	TextAnalysisCache m_analysisCache;
	ShapeRunCache m_shapeRunCache;
};

extern __declspec(thread) LayoutPassCache * g_pCurrentLayoutPassCache;

inline bool IsPerfFlagEnabled(const wchar_t * pszName)
{
	wchar_t rgchValue[16] = {0};
	DWORD cchValue = ::GetEnvironmentVariableW(pszName, rgchValue, _countof(rgchValue));
	if (cchValue == 0)
		return true;
	return _wcsicmp(rgchValue, L"0") != 0 && _wcsicmp(rgchValue, L"false") != 0 &&
		_wcsicmp(rgchValue, L"off") != 0;
}

inline bool IsPath1ShapeCacheEnabled()
{
	static int s_nEnabled = -1;
	if (s_nEnabled < 0)
		s_nEnabled = IsPerfFlagEnabled(L"FW_PERF_P125_PATH1") ? 1 : 0;
	return s_nEnabled == 1;
}

inline bool IsPath2AnalysisCacheEnabled()
{
	static int s_nEnabled = -1;
	if (s_nEnabled < 0)
		s_nEnabled = IsPerfFlagEnabled(L"FW_PERF_P125_PATH2") ? 1 : 0;
	return s_nEnabled == 1;
}

inline LayoutPassCache * GetCurrentLayoutPassCache()
{
	return g_pCurrentLayoutPassCache;
}

inline LayoutPassCache * SetCurrentLayoutPassCache(LayoutPassCache * pLayoutPassCache)
{
	LayoutPassCache * pPrev = g_pCurrentLayoutPassCache;
	g_pCurrentLayoutPassCache = pLayoutPassCache;
	return pPrev;
}

#endif // LAYOUTCACHE_INCLUDED