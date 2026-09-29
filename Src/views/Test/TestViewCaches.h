/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2026 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTVIEWCACHES_H_INCLUDED
#define TESTVIEWCACHES_H_INCLUDED

#pragma once

#include "testViews.h"
#include "RenderEngineTestBase.h"
#include "ColorStateCache.h"
#include "FontHandleCache.h"
#include "LayoutCache.h"
#include "NfcOffsetMap.h"

namespace TestViews
{
	class TestColorStateCache : public unitpp::suite
	{
		HDC m_hdc;
		ColorStateCache m_cache;

		void testApplyOnFirstUse()
		{
			bool fApplied = m_cache.ApplyIfNeeded(m_hdc, RGB(1, 2, 3), RGB(4, 5, 6), TRANSPARENT);
			unitpp::assert_true("First color apply should update", fApplied);
		}

		void testNoApplyWhenUnchanged()
		{
			m_cache.ApplyIfNeeded(m_hdc, RGB(10, 20, 30), RGB(40, 50, 60), OPAQUE);
			bool fApplied = m_cache.ApplyIfNeeded(m_hdc, RGB(10, 20, 30), RGB(40, 50, 60), OPAQUE);
			unitpp::assert_true("Repeated identical color apply should be skipped", !fApplied);
		}

		void testApplyWhenChanged()
		{
			m_cache.ApplyIfNeeded(m_hdc, RGB(7, 8, 9), RGB(11, 12, 13), TRANSPARENT);
			bool fApplied = m_cache.ApplyIfNeeded(m_hdc, RGB(17, 18, 19), RGB(11, 12, 13), TRANSPARENT);
			unitpp::assert_true("Changed foreground color should update", fApplied);
		}

		void testInvalidateForcesApply()
		{
			m_cache.ApplyIfNeeded(m_hdc, RGB(1, 1, 1), RGB(2, 2, 2), OPAQUE);
			m_cache.Invalidate();
			bool fApplied = m_cache.ApplyIfNeeded(m_hdc, RGB(1, 1, 1), RGB(2, 2, 2), OPAQUE);
			unitpp::assert_true("Invalidate should force next apply", fApplied);
		}

	public:
		TestColorStateCache();
		virtual void Setup()
		{
			m_hdc = GetTestDC();
			m_cache.Invalidate();
		}
		virtual void Teardown()
		{
			if (m_hdc)
				ReleaseTestDC(m_hdc);
			m_hdc = NULL;
		}
	};

	class TestFontHandleCache : public unitpp::suite
	{
		struct DeleteTracker
		{
			Set<HFONT> m_failing;
			Vector<HFONT> m_deleted;
		};

		FontHandleCache m_cache;

		static bool TryDeleteForTest(HFONT hfont, void * pContext)
		{
			DeleteTracker * pTracker = reinterpret_cast<DeleteTracker *>(pContext);
			if (pTracker->m_failing.IsMember(hfont))
				return false;
			pTracker->m_deleted.Push(hfont);
			return true;
		}

		LgCharRenderProps MakeProps(int n) const
		{
			LgCharRenderProps chrp;
			memset(&chrp, 0, sizeof(chrp));
			chrp.ttvBold = (n & 1) ? kttvForceOn : kttvOff;
			chrp.ttvItalic = (n & 2) ? kttvForceOn : kttvOff;
			chrp.dympHeight = 10000 + (n * 10);
			swprintf_s(chrp.szFaceName, L"CacheFont_%d", n);
			return chrp;
		}

		void FillToCacheMax(DeleteTracker & tracker)
		{
			for (int i = 0; i < FontHandleCache::kcFontCacheMax; ++i)
			{
				HFONT hfont = reinterpret_cast<HFONT>(static_cast<uintptr_t>(100 + i));
				LgCharRenderProps chrp = MakeProps(i);
				m_cache.AddFontToCache(hfont, &chrp, NULL, TryDeleteForTest, &tracker);
			}
		}

		void testFindCachedFont()
		{
			DeleteTracker tracker;
			HFONT hfont = reinterpret_cast<HFONT>(static_cast<uintptr_t>(200));
			LgCharRenderProps chrp = MakeProps(1);
			m_cache.AddFontToCache(hfont, &chrp, NULL, TryDeleteForTest, &tracker);
			HFONT hfontFound = m_cache.FindCachedFont(&chrp);
			unitpp::assert_eq("FindCachedFont should return added handle", hfont, hfontFound);
		}

		void testEvictionDeletesOldest()
		{
			DeleteTracker tracker;
			FillToCacheMax(tracker);
			HFONT hfontNewest = reinterpret_cast<HFONT>(static_cast<uintptr_t>(999));
			LgCharRenderProps chrp = MakeProps(99);
			m_cache.AddFontToCache(hfontNewest, &chrp, NULL, TryDeleteForTest, &tracker);

			unitpp::assert_eq("Cache size should stay bounded", FontHandleCache::kcFontCacheMax,
				m_cache.CacheCount());
			unitpp::assert_true("Oldest entry should be deleted on eviction",
				tracker.m_deleted.Size() >= 1 &&
				tracker.m_deleted[tracker.m_deleted.Size() - 1] == reinterpret_cast<HFONT>(static_cast<uintptr_t>(100)));
		}

		void testFailedDeleteIsDeferredAndRetried()
		{
			DeleteTracker tracker;
			HFONT hfontVictim = reinterpret_cast<HFONT>(static_cast<uintptr_t>(100));
			tracker.m_failing.Insert(hfontVictim);
			FillToCacheMax(tracker);

			HFONT hfontNewest = reinterpret_cast<HFONT>(static_cast<uintptr_t>(1000));
			LgCharRenderProps chrpNewest = MakeProps(100);
			m_cache.AddFontToCache(hfontNewest, &chrpNewest, NULL, TryDeleteForTest, &tracker);

			unitpp::assert_eq("Failed delete should queue one deferred font", 1,
				m_cache.DeferredDeleteCount());
			unitpp::assert_true("Victim should be in deferred queue",
				m_cache.IsDeferredDeleteQueued(hfontVictim));

			tracker.m_failing.Delete(hfontVictim);
			m_cache.TryDeleteDeferredFonts(NULL, TryDeleteForTest, &tracker);
			unitpp::assert_eq("Deferred queue should drain after successful retry", 0,
				m_cache.DeferredDeleteCount());
		}

		void testDeferredDeleteSkipsActiveFont()
		{
			DeleteTracker tracker;
			HFONT hfontVictim = reinterpret_cast<HFONT>(static_cast<uintptr_t>(100));
			tracker.m_failing.Insert(hfontVictim);
			FillToCacheMax(tracker);

			HFONT hfontNewest = reinterpret_cast<HFONT>(static_cast<uintptr_t>(1001));
			LgCharRenderProps chrpNewest = MakeProps(101);
			m_cache.AddFontToCache(hfontNewest, &chrpNewest, NULL, TryDeleteForTest, &tracker);
			tracker.m_failing.Delete(hfontVictim);

			m_cache.TryDeleteDeferredFonts(hfontVictim, TryDeleteForTest, &tracker);
			unitpp::assert_eq("Active deferred font should not be deleted", 1,
				m_cache.DeferredDeleteCount());

			m_cache.TryDeleteDeferredFonts(NULL, TryDeleteForTest, &tracker);
			unitpp::assert_eq("Deferred queue should delete when no longer active", 0,
				m_cache.DeferredDeleteCount());
		}

	public:
		TestFontHandleCache();
		virtual void Setup()
		{
			m_cache = FontHandleCache();
		}
	};

	class TestShapeRunCache : public unitpp::suite
	{
		void testFontFeaturesArePartOfCacheKey()
		{
			ShapeRunCache cache;
			SCRIPT_ANALYSIS sa;
			ZeroMemory(&sa, sizeof(sa));
			sa.eScript = 1;

			const OLECHAR rgch[] = L"office";
			const int cch = 6;
			const OLECHAR rgchFeatureOn[] = L"liga=1";
			const OLECHAR rgchFeatureOnCopy[] = L"liga=1";
			const OLECHAR rgchFeatureOff[] = L"liga=0";
			HFONT hfont = reinterpret_cast<HFONT>(static_cast<uintptr_t>(0x1234));

			WORD prgGlyph[] = {1, 2, 3};
			SCRIPT_VISATTR prgsva[3];
			ZeroMemory(prgsva, sizeof(prgsva));
			int prgAdvance[] = {5, 5, 5};
			int prgcst[] = {0, 0, 0};
			GOFFSET prgoff[3];
			ZeroMemory(prgoff, sizeof(prgoff));
			WORD prgCluster[] = {0, 1, 2, 2, 2, 2};

			cache.Store(rgch, cch, hfont, sa, rgchFeatureOn, prgGlyph, prgsva,
				prgAdvance, prgcst, prgoff, prgCluster, 3, 15, false);

			unitpp::assert_true("same feature contents should hit shape cache",
				cache.Find(rgch, cch, hfont, sa, rgchFeatureOnCopy) != NULL);
			unitpp::assert_true("different feature contents should miss shape cache",
				cache.Find(rgch, cch, hfont, sa, rgchFeatureOff) == NULL);
			unitpp::assert_true("missing feature contents should miss feature-specific shape cache entry",
				cache.Find(rgch, cch, hfont, sa, NULL) == NULL);
		}

	public:
		TestShapeRunCache();
	};

	class TestTextAnalysisCache : public unitpp::suite
	{
		static IVwTextSource * FakeSource(int n)
		{
			return reinterpret_cast<IVwTextSource *>(static_cast<uintptr_t>(0x1000 + n));
		}

		void StoreTenChars(TextAnalysisCache & cache, IVwTextSource * pts, int ichMin, int ws)
		{
			SCRIPT_ITEM rgscri[2];
			ZeroMemory(rgscri, sizeof(rgscri));
			rgscri[1].iCharPos = 10;
			cache.Store(pts, ichMin, 10, ws, false, L"abcdefghij", 10, true, rgscri, 1);
		}

		void testStoredRangeIsFound()
		{
			TextAnalysisCache cache;
			StoreTenChars(cache, FakeSource(1), 0, 1);
			TextAnalysisEntry * pentry = cache.Find(FakeSource(1), 0, 10, 1, false);
			unitpp::assert_true("an identical request should hit", pentry != NULL);
			unitpp::assert_eq("the hit should carry the stored text length", 10,
				pentry->m_vchText.Size());
			unitpp::assert_true("the hit should carry the stored text",
				::memcmp(pentry->m_vchText.Begin(), L"abcdefghij", 10 * isizeof(OLECHAR)) == 0);
			unitpp::assert_eq("the hit should carry the stored item count", 1, pentry->m_citem);
		}

		void testShorterRequestFromSameStartIsCovered()
		{
			TextAnalysisCache cache;
			StoreTenChars(cache, FakeSource(1), 0, 1);
			unitpp::assert_true("a shorter request from the same start should hit",
				cache.Find(FakeSource(1), 0, 4, 1, false) != NULL);
			unitpp::assert_true("a longer request from the same start should miss",
				cache.Find(FakeSource(1), 0, 11, 1, false) == NULL);
		}

		void testKeyMismatchMisses()
		{
			TextAnalysisCache cache;
			StoreTenChars(cache, FakeSource(1), 0, 1);
			unitpp::assert_true("a different start offset should miss",
				cache.Find(FakeSource(1), 1, 4, 1, false) == NULL);
			unitpp::assert_true("a different text source should miss",
				cache.Find(FakeSource(2), 0, 4, 1, false) == NULL);
			unitpp::assert_true("a different writing system should miss",
				cache.Find(FakeSource(1), 0, 4, 2, false) == NULL);
			unitpp::assert_true("a different direction should miss",
				cache.Find(FakeSource(1), 0, 4, 1, true) == NULL);
		}

		void testEvictionIsBounded()
		{
			TextAnalysisCache cache(2);
			StoreTenChars(cache, FakeSource(1), 0, 1);
			StoreTenChars(cache, FakeSource(2), 0, 1);
			StoreTenChars(cache, FakeSource(3), 0, 1);
			unitpp::assert_eq("storing past capacity should evict", 1, cache.EvictionCount());
			unitpp::assert_true("the oldest entry should be the one evicted",
				cache.Find(FakeSource(1), 0, 10, 1, false) == NULL);
			unitpp::assert_true("the newest entry should survive",
				cache.Find(FakeSource(3), 0, 10, 1, false) != NULL);
		}

	public:
		TestTextAnalysisCache();
	};

	class TestNfcOffsetMap : public unitpp::suite
	{
		// NFC length of the text in [ichMin, ichLim), normalized from scratch: the definition
		// the map must reproduce.
		static int OracleNfcLength(const StrUni & stu, int ichMin, int ichLim)
		{
			StrUni stuRange(stu.Chars() + ichMin, ichLim - ichMin);
			StrUtil::NormalizeStrUni(stuRange, UNORM_NFC);
			return stuRange.Length();
		}

		void VerifyAgainstOracle(const wchar_t * pszText, const char * pszLabel)
		{
			StrUni stu(pszText);
			int cch = stu.Length();
			TxtSrc ts(pszText, g_qwsf);
			IVwTextSourcePtr qts;
			ts.QueryInterface(IID_IVwTextSource, (void **)&qts);
			NfcOffsetMap map;
			map.Reset(qts);

			StrAnsi staMsg;
			int cBoundaries = 0;
			for (int ichBase = 0; ichBase <= cch; ++ichBase)
			{
				int ichNfc;
				if (!map.IsBoundary(ichBase))
				{
					staMsg.Format("%s: base %d is not a boundary so the map must decline",
						pszLabel, ichBase);
					unitpp::assert_true(staMsg.Chars(), !map.TryOffsetInNfc(cch, ichBase, &ichNfc));
					continue;
				}
				++cBoundaries;
				for (int ich = ichBase; ich <= cch; ++ich)
				{
					int cchExpected = OracleNfcLength(stu, ichBase, ich);
					staMsg.Format("%s: OffsetInNfc(%d, %d) must answer", pszLabel, ich, ichBase);
					unitpp::assert_true(staMsg.Chars(), map.TryOffsetInNfc(ich, ichBase, &ichNfc));
					staMsg.Format("%s: OffsetInNfc(%d, %d)", pszLabel, ich, ichBase);
					unitpp::assert_eq(staMsg.Chars(), cchExpected, ichNfc);
				}
				int cchNfcAll = OracleNfcLength(stu, ichBase, cch);
				for (int ichNfcReq = 0; ichNfcReq <= cchNfcAll + 2; ++ichNfcReq)
				{
					int ichExpected = ichBase;
					for (int ich = ichBase; ich <= cch; ++ich)
					{
						if (OracleNfcLength(stu, ichBase, ich) <= ichNfcReq)
							ichExpected = ich;
					}
					int ichOrig;
					staMsg.Format("%s: OffsetToOrig(%d, %d) must answer", pszLabel, ichNfcReq, ichBase);
					unitpp::assert_true(staMsg.Chars(),
						map.TryOffsetToOrig(ichNfcReq, ichBase, &ichOrig));
					staMsg.Format("%s: OffsetToOrig(%d, %d)", pszLabel, ichNfcReq, ichBase);
					unitpp::assert_eq(staMsg.Chars(), ichExpected, ichOrig);
				}
			}
			staMsg.Format("%s: text start and end are always boundaries", pszLabel);
			unitpp::assert_true(staMsg.Chars(), cBoundaries >= (cch == 0 ? 1 : 2));
		}

		void testMatchesFromScratchNormalization()
		{
			VerifyAgainstOracle(L"plain ascii text, no marks", "ascii only");
			VerifyAgainstOracle(L"cafe\u0301 de\u0301ja\u0300 vu no\u0308el", "decomposed latin");
			VerifyAgainstOracle(L"caf\u00E9 d\u00E9j\u00E0 vu", "precomposed latin");
			VerifyAgainstOracle(L"a\u0301\u0327b a\u0327\u0301c", "non-canonical mark order");
			VerifyAgainstOracle(L"\u0301\u0300abc", "marks with no base");
			VerifyAgainstOracle(L"\u0628\u064E\u0651 \u0644\u0651\u064E", "arabic harakat");
			VerifyAgainstOracle(L"\u1112\u1161\u11AB \u1100\u1161", "hangul jamo");
			VerifyAgainstOracle(L"x\U0001D400\u0301y\U0001D401", "surrogate pairs");
			VerifyAgainstOracle(L"a\u0344b\u0344", "lengthening mark");
			VerifyAgainstOracle(L"\u212Bngstro\u0308m \u2126", "singleton decomposition");
			VerifyAgainstOracle(L"The \u0628\u064E caf\u00E9 e\u0301\u1112\u1161\u11AB\U0001D400\u0344!", "mixed everything");
		}

		void testCombiningMarkAndLowSurrogateAreNotBoundaries()
		{
			TxtSrc ts(L"e\u0301\U0001D400", g_qwsf);
			IVwTextSourcePtr qts;
			ts.QueryInterface(IID_IVwTextSource, (void **)&qts);
			NfcOffsetMap map;
			map.Reset(qts);
			unitpp::assert_true("start is a boundary", map.IsBoundary(0));
			unitpp::assert_true("before a combining mark is not a boundary", !map.IsBoundary(1));
			unitpp::assert_true("before a high surrogate is a boundary", map.IsBoundary(2));
			unitpp::assert_true("between surrogates is not a boundary", !map.IsBoundary(3));
			unitpp::assert_true("end is a boundary", map.IsBoundary(4));
		}

		void testResetRebindsToAnotherSource()
		{
			TxtSrc ts1(L"ab", g_qwsf);
			TxtSrc ts2(L"e\u0301e\u0301", g_qwsf);
			IVwTextSourcePtr qts1;
			IVwTextSourcePtr qts2;
			ts1.QueryInterface(IID_IVwTextSource, (void **)&qts1);
			ts2.QueryInterface(IID_IVwTextSource, (void **)&qts2);
			NfcOffsetMap map;
			map.Reset(qts1);
			unitpp::assert_eq("two ascii characters", 2, map.NfcLengthOfPrefix(2));
			map.Reset(qts2);
			unitpp::assert_eq("rebinding discards the old table", 2, map.NfcLengthOfPrefix(4));
		}

	public:
		TestNfcOffsetMap();
		virtual void Setup()
		{
			CreateTestWritingSystemFactory();
		}
		virtual void Teardown()
		{
			CloseTestWritingSystemFactory();
		}
	};
}

#endif // TESTVIEWCACHES_H_INCLUDED
