/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2026 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: NfcOffsetMap.h
Responsibility:
Last reviewed: Not yet.

Description:
	Translates offsets between a text source and its NFC-normalized form without
	renormalizing a prefix of the text on every request (LT-22674).
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef NFCOFFSETMAP_INCLUDED
#define NFCOFFSETMAP_INCLUDED

/*----------------------------------------------------------------------------------------------
	Offset translation for one text source between its own (typically NFD) offsets and offsets
	into its NFC-normalized form.

	An offset into the NFC form of a range is defined as the NFC length of that range, exactly
	as normalizing the range from scratch would give. The map reproduces that definition while
	normalizing each character at most once: it splits the text where ICU says normalization
	cannot cross a boundary, records the NFC length of the text before each boundary, and
	normalizes only the tail from the nearest boundary when a request lands inside a chunk.
	Requests whose base offset is not a boundary cannot be answered from the table and report
	failure, so the caller can fall back to normalizing the range directly.

	The map is bound to one text source whose content does not change while the map is in
	use; Reset rebinds it.
----------------------------------------------------------------------------------------------*/
class NfcOffsetMap
{
public:
	NfcOffsetMap() : m_pts(NULL), m_cchText(0), m_fBuilt(false)
	{
	}

	// Binds the map to a text source, discarding any table built for a different one.
	void Reset(IVwTextSource * pts)
	{
		if (pts == m_pts && m_fBuilt)
			return;
		Clear();
		m_pts = pts;
	}

	// Unbinds the map and discards its table.
	void Clear()
	{
		m_pts = NULL;
		m_cchText = 0;
		m_fBuilt = false;
		m_vchText.Clear();
		m_vichBoundary.Clear();
		m_vcchNfcBefore.Clear();
	}

	// True when normalization of the text before ich is independent of the text from ich on.
	// The start and end of the text are boundaries; the middle of a surrogate pair is not.
	bool IsBoundary(int ich)
	{
		if (!Build() || ich < 0 || ich > m_cchText)
			return false;
		if (ich == 0 || ich == m_cchText)
			return true;
		return BoundaryIndexAt(ich) >= 0;
	}

	// NFC length of the text from ichBase to ich. Fails unless ichBase is a boundary.
	bool TryOffsetInNfc(int ich, int ichBase, int * pichNfc)
	{
		*pichNfc = 0;
		if (!Build() || ichBase < 0 || ich < ichBase || ich > m_cchText || !IsBoundary(ichBase))
			return false;
		*pichNfc = NfcLengthOfPrefix(ich) - NfcLengthOfPrefix(ichBase);
		return true;
	}

	// The longest range starting at ichBase whose NFC form is at most ichNfc characters long,
	// reported as the offset of its end. Fails unless ichBase is a boundary.
	bool TryOffsetToOrig(int ichNfc, int ichBase, int * pichOrig)
	{
		*pichOrig = ichBase;
		if (!Build() || ichBase < 0 || ichBase > m_cchText || ichNfc < 0 || !IsBoundary(ichBase))
			return false;
		int cchNfcBase = NfcLengthOfPrefix(ichBase);
		// Find the last boundary at or after ichBase whose prefix length still fits, then
		// walk forward one character at a time inside that chunk.
		int iLow = BoundaryIndexAtOrBefore(ichBase);
		int iHigh = m_vichBoundary.Size() - 1;
		while (iLow < iHigh)
		{
			int iMid = iLow + (iHigh - iLow + 1) / 2;
			if (m_vcchNfcBefore[iMid] - cchNfcBase <= ichNfc)
				iLow = iMid;
			else
				iHigh = iMid - 1;
		}
		int ich = max(m_vichBoundary[iLow], ichBase);
		int ichChunkLim = iLow + 1 < m_vichBoundary.Size() ? m_vichBoundary[iLow + 1] : m_cchText;
		while (ich < ichChunkLim && NfcLengthOfPrefix(ich + 1) - cchNfcBase <= ichNfc)
			++ich;
		*pichOrig = ich;
		return true;
	}

	// NFC length of the text before ich, computed from the nearest boundary at or before it.
	int NfcLengthOfPrefix(int ich)
	{
		if (!Build() || ich <= 0)
			return 0;
		Assert(ich <= m_cchText);
		int iBoundary = BoundaryIndexAtOrBefore(ich);
		int ichBoundary = m_vichBoundary[iBoundary];
		int cchNfc = m_vcchNfcBefore[iBoundary];
		if (ich > ichBoundary)
			cchNfc += NfcLength(ichBoundary, ich);
		return cchNfc;
	}

private:
	// Fetches the whole text and records every normalization boundary with the NFC length
	// of the text before it. Chunks between boundaries are normalized independently.
	bool Build()
	{
		if (m_fBuilt)
			return true;
		if (!m_pts)
			return false;
		CheckHr(m_pts->get_Length(&m_cchText));
		m_vchText.Resize(m_cchText);
		if (m_cchText > 0)
			CheckHr(m_pts->Fetch(0, m_cchText, m_vchText.Begin()));

		const icu::Normalizer2 * pnorm = SilUtil::GetIcuNormalizer(UNORM_NFC);
		m_vichBoundary.Push(0);
		m_vcchNfcBefore.Push(0);
		int ichChunk = 0;
		int cchNfc = 0;
		int ich = 0;
		while (ich < m_cchText)
		{
			int ichNext = ich;
			UChar32 ch;
			U16_NEXT(m_vchText.Begin(), ichNext, m_cchText, ch);
			if (ich > 0 && pnorm->hasBoundaryBefore(ch))
			{
				cchNfc += NfcLength(ichChunk, ich);
				m_vichBoundary.Push(ich);
				m_vcchNfcBefore.Push(cchNfc);
				ichChunk = ich;
			}
			ich = ichNext;
		}
		m_fBuilt = true;
		return true;
	}

	// NFC length of the text in [ichMin, ichLim).
	int NfcLength(int ichMin, int ichLim)
	{
		if (ichLim <= ichMin)
			return 0;
		StrUni stu(m_vchText.Begin() + ichMin, ichLim - ichMin);
		StrUtil::NormalizeStrUni(stu, UNORM_NFC);
		return stu.Length();
	}

	// Index of the last recorded boundary at or before ich; the table always holds 0.
	int BoundaryIndexAtOrBefore(int ich)
	{
		int iLow = 0;
		int iHigh = m_vichBoundary.Size() - 1;
		while (iLow < iHigh)
		{
			int iMid = iLow + (iHigh - iLow + 1) / 2;
			if (m_vichBoundary[iMid] <= ich)
				iLow = iMid;
			else
				iHigh = iMid - 1;
		}
		return iLow;
	}

	// Index of the boundary recorded exactly at ich, or -1.
	int BoundaryIndexAt(int ich)
	{
		int i = BoundaryIndexAtOrBefore(ich);
		return m_vichBoundary[i] == ich ? i : -1;
	}

	IVwTextSource * m_pts;
	int m_cchText;
	bool m_fBuilt;
	Vector<OLECHAR> m_vchText;
	Vector<int> m_vichBoundary;
	Vector<int> m_vcchNfcBefore;
};

#endif // NFCOFFSETMAP_INCLUDED
