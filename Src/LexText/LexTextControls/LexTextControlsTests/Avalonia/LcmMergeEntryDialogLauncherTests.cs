// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.Linq;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LexTextControlsTests
{
	/// <summary>
	/// The LCModel-aware side of the reusable Avalonia entry-search ("go") dialog launcher, wired as its first
	/// consumer — Merge Entry (<see cref="LcmMergeEntryDialogLauncher"/>): the search delegate that reuses the
	/// legacy EntryGoSearchEngine matching (excluding the current entry, you cannot merge an entry with itself) and
	/// maps matches to lightweight result rows, plus the merge itself (survivor absorbs the current entry in one
	/// undoable step — the exact legacy MergeObject). The modal loop is desktop-only (it needs an Avalonia app + a
	/// WinForms-owned modal Form), so it is exercised by the headless EntryGoDialogTests in FwAvaloniaDialogsTests;
	/// here we cover the pure LCModel search + merge over a real LcmCache, visible via InternalsVisibleTo.
	/// </summary>
	[TestFixture]
	public class LcmMergeEntryDialogLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ILexEntry _casa;
		private ILexEntry _cantar;
		private ILexEntry _perro;

		// The base opens an undoable UOW in TestSetup and calls CreateTestData() inside it, so data is created
		// directly here with NO UOW wrapper (a nested task would throw "Nested tasks are not supported").
		protected override void CreateTestData()
		{
			base.CreateTestData();
			_casa = MakeEntry("casa", "house");
			_cantar = MakeEntry("cantar", "to sing");
			_perro = MakeEntry("perro", "dog");
		}

		private ILexEntry MakeEntry(string lexemeForm, string gloss)
		{
			var vernWs = Cache.DefaultVernWs;
			var analWs = Cache.DefaultAnalWs;
			var components = new LexEntryComponents
			{
				MorphType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
					.GetObject(MoMorphTypeTags.kguidMorphStem)
			};
			components.LexemeFormAlternatives.Add(TsStringUtils.MakeString(lexemeForm, vernWs));
			components.GlossAlternatives.Add(TsStringUtils.MakeString(gloss, analWs));
			return Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(components);
		}

		// ----- BuildInput: the Merge re-skin (title / OK / excluded id / initial query) -----

		[Test]
		public void BuildInput_CarriesMergeTitleOkAndExcludesTheCurrentEntry()
		{
			var input = LcmMergeEntryDialogLauncher.BuildInput(Cache, mediator: null, propertyTable: null, _casa);

			Assert.That(input.Title, Is.EqualTo(FwAvaloniaDialogsStrings.MergeTitle));
			Assert.That(input.OkButtonText, Is.EqualTo(FwAvaloniaDialogsStrings.MergeOkButton));
			Assert.That(input.ExcludedId, Is.EqualTo(_casa.Hvo.ToString(CultureInfo.InvariantCulture)),
				"the current entry's hvo is the excluded id");
			Assert.That(input.HelpTopic, Is.EqualTo("khtpMergeEntry"));
			Assert.That(input.Search, Is.Not.Null);
		}

		// ----- the search delegate reuses the legacy matching and excludes the current entry -----

		[Test]
		public void Search_FindsMatchingEntriesByForm()
		{
			var search = LcmMergeEntryDialogLauncher.BuildSearch(Cache, null, null, _perro);

			var results = search("ca"); // matches casa + cantar
			Assert.That(results.Select(r => r.Id),
				Is.EquivalentTo(new[]
				{
					_casa.Hvo.ToString(CultureInfo.InvariantCulture),
					_cantar.Hvo.ToString(CultureInfo.InvariantCulture)
				}),
				"the search returns the entries whose form matches the query (legacy EntryGoSearchEngine matching)");
			Assert.That(results.Any(r => r.Text == "casa"), Is.True, "the result row carries the headword");
		}

		[Test]
		public void Search_ExcludesTheCurrentEntry()
		{
			// Searching while 'casa' is the current entry must never return 'casa' itself.
			var search = LcmMergeEntryDialogLauncher.BuildSearch(Cache, null, null, _casa);

			var results = search("ca");
			Assert.That(results.Any(r => r.Id == _casa.Hvo.ToString(CultureInfo.InvariantCulture)), Is.False,
				"you cannot merge an entry with itself: the current entry is excluded from the matches");
			Assert.That(results.Select(r => r.Id),
				Is.EqualTo(new[] { _cantar.Hvo.ToString(CultureInfo.InvariantCulture) }),
				"only the other matching entry (cantar) remains");
		}

		[Test]
		public void Search_NeverReturnsTheCurrentEntry_RegardlessOfQuery()
		{
			// Whatever the query, the current entry's id must never be in the matches (the merge invariant). The
			// exact empty-query result set is the StringSearcher's concern; here we only assert the exclusion holds.
			var search = LcmMergeEntryDialogLauncher.BuildSearch(Cache, null, null, _casa);
			var currentId = _casa.Hvo.ToString(CultureInfo.InvariantCulture);

			Assert.That(search(string.Empty).Any(r => r.Id == currentId), Is.False, "empty query excludes the current entry");
			Assert.That(search("casa").Any(r => r.Id == currentId), Is.False, "an exact-form query still excludes the current entry");
		}

		// ----- the merge: the survivor absorbs the current entry (the exact legacy MergeObject) -----

		[Test]
		public void Merge_MergesCurrentEntryIntoSurvivor()
		{
			var entriesBefore = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count;
			var survivorHvo = _cantar.Hvo;

			// Merge 'casa' (current) INTO 'cantar' (survivor) — the legacy MergeObject direction. The base test
			// already has an open UOW (TestSetup), so call MergeObject directly here; the launcher's PerformMerge
			// opens its OWN UndoableUnitOfWorkHelper.Do at runtime (wrapping it here would throw "Nested tasks").
			_cantar.MergeObject(_casa, true);
			_cantar.DateModified = System.DateTime.Now;

			Assert.That(_casa.IsValidObject, Is.False, "the current entry is deleted (absorbed into the survivor)");
			Assert.That(Cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count,
				Is.EqualTo(entriesBefore - 1), "the merge removes exactly one entry");
			Assert.That(Cache.ServiceLocator.GetInstance<ILexEntryRepository>()
				.GetObject(survivorHvo).IsValidObject, Is.True, "the survivor remains");
		}

		// ----- the commit-on-select merge is gated by a confirmation (semi-destructive) -----

		[Test]
		public void ConfirmAndMerge_DoesNotMerge_WhenConfirmDeclined()
		{
			// Commit-on-select picks 'casa' as the survivor for current 'cantar', but the user DECLINES the confirm:
			// no merge runs and no survivor is returned. A merge spy proves the merge action is never reached. (The
			// merge itself opens its own UOW at runtime; the base test already holds one, so the spy avoids nesting.)
			var confirmAsked = false;
			var mergeRan = false;

			var survivor = LcmMergeEntryDialogLauncher.ConfirmAndMerge(Cache, _casa, _cantar, true,
				confirm: (current, surv) => { confirmAsked = true; return false; },
				merge: (surv, current) => mergeRan = true);

			Assert.That(confirmAsked, Is.True, "the (semi-destructive) merge asks for confirmation first");
			Assert.That(mergeRan, Is.False, "a declined confirmation aborts: the merge action is never reached");
			Assert.That(survivor, Is.Null, "a declined confirmation returns no survivor");
			Assert.That(_cantar.IsValidObject, Is.True, "the current entry is NOT merged away when confirm is declined");
		}

		[Test]
		public void ConfirmAndMerge_Merges_WhenConfirmAccepted()
		{
			// The user CONFIRMS: the merge action runs with (survivor=casa, current=cantar) and the survivor is
			// returned. A merge spy stands in for PerformMerge so the test does not open a nested UOW (the actual
			// MergeObject is covered by Merge_MergesCurrentEntryIntoSurvivor).
			ILexEntry mergedSurvivor = null;
			ILexEntry mergedCurrent = null;

			var survivor = LcmMergeEntryDialogLauncher.ConfirmAndMerge(Cache, _casa, _cantar, true,
				confirm: (current, surv) => true,
				merge: (surv, current) => { mergedSurvivor = surv; mergedCurrent = current; });

			Assert.That(survivor, Is.SameAs(_casa), "a confirmed merge returns the survivor");
			Assert.That(mergedSurvivor, Is.SameAs(_casa), "the merge runs with the chosen survivor on confirm");
			Assert.That(mergedCurrent, Is.SameAs(_cantar), "the current entry is merged into the survivor on confirm");
		}

		[Test]
		public void ConfirmAndMerge_NoMergeAndNoConfirm_WhenNoSurvivorOrSameEntry()
		{
			// A null survivor (nothing chosen) or the same entry never merges and never even asks to confirm.
			var asked = false;
			var mergeRan = false;
			Func<ILexEntry, ILexEntry, bool> confirm = (c, s) => { asked = true; return true; };
			Action<ILexEntry, ILexEntry> merge = (s, c) => mergeRan = true;

			Assert.That(LcmMergeEntryDialogLauncher.ConfirmAndMerge(Cache, null, _cantar, true, confirm, merge), Is.Null,
				"no survivor: no merge");
			Assert.That(LcmMergeEntryDialogLauncher.ConfirmAndMerge(Cache, _cantar, _cantar, true, confirm, merge), Is.Null,
				"the same entry cannot be merged into itself");
			Assert.That(asked, Is.False, "no confirmation is asked when there is nothing to merge");
			Assert.That(mergeRan, Is.False, "the merge action is never reached when there is nothing to merge");
			Assert.That(_cantar.IsValidObject, Is.True, "the entry is untouched");
		}

		[Test]
		public void MergeConfirmString_Resolves()
		{
			// The merge confirm text is seeded from the canonical legacy "merged into ... one entry" wording with
			// {0}=current and {1}=survivor placeholders.
			Assert.That(FwAvaloniaDialogsStrings.MergeConfirm, Is.Not.Null.And.Not.Empty);
			var formatted = string.Format(FwAvaloniaDialogsStrings.MergeConfirm, "cantar", "casa");
			Assert.That(formatted, Does.Contain("cantar").And.Contain("casa"),
				"the confirm text names both the current entry and the chosen survivor");
		}
	}
}
