/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2026 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTVIEWCACHES_H_INCLUDED
#define TESTVIEWCACHES_H_INCLUDED

#pragma once

#include "testViews.h"
#include "ColorStateCache.h"
#include "FontHandleCache.h"

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
}

#endif // TESTVIEWCACHES_H_INCLUDED
