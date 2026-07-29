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
	/// The LCModel-aware side of the reusable Avalonia entry-search ("go") dialog launcher, wired as the New-UI
	/// replacement for the plain "Go to Lexical Entry" command (<see cref="LcmGoToEntryDialogLauncher"/>, the
	/// New-mode path of <c>GoLinkEntryDlgListener.OnGotoLexEntry</c>): the search delegate that reuses the legacy
	/// EntryGoSearchEngine matching (with NO excluded entry — unlike Merge/Link, a plain "go" has no starting
	/// entry) and resolves the chosen id back to the live entry, with no model mutation. The modal loop is
	/// desktop-only (it needs an Avalonia app + a WinForms-owned modal Form), so it is exercised by the headless
	/// EntryGoDialogTests in FwAvaloniaDialogsTests; here we cover the pure LCModel search + resolve over a real
	/// LcmCache, visible via InternalsVisibleTo.
	/// </summary>
	[TestFixture]
	public class LcmGoToEntryDialogLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
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

		// ----- BuildInput: the plain "go" re-skin (title / prompt / help, no excluded id) -----

		[Test]
		public void BuildInput_CarriesFindLexEntryTitleAndNoExcludedId()
		{
			var input = LcmGoToEntryDialogLauncher.BuildInput(Cache, mediator: null, propertyTable: null);

			Assert.That(input.Title, Is.EqualTo(LexTextControls.ksFindLexEntry));
			Assert.That(input.SearchPrompt, Is.EqualTo(FwAvaloniaDialogsStrings.EntryGoResultsLabel));
			Assert.That(input.HelpTopic, Is.EqualTo("khtpFindLexicalEntry"));
			Assert.That(input.ExcludedId, Is.Null, "a plain go-to-entry has no starting entry to exclude");
			Assert.That(input.Search, Is.Not.Null);
		}

		// ----- the search delegate reuses the legacy matching, with NO exclusion -----

		[Test]
		public void Search_FindsMatchingEntriesByForm()
		{
			var search = LcmGoToEntryDialogLauncher.BuildSearch(Cache, null, null);

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
		public void Search_DoesNotExcludeAnyEntry()
		{
			// Unlike Merge/Link, the plain go-to-entry dialog has no starting entry, so nothing is excluded.
			var search = LcmGoToEntryDialogLauncher.BuildSearch(Cache, null, null);

			var results = search("perro");
			Assert.That(results.Select(r => r.Id), Is.EqualTo(new[] { _perro.Hvo.ToString(CultureInfo.InvariantCulture) }),
				"the matching entry is returned; nothing is filtered out");
		}

		// ----- resolving the chosen id back to the live entry (no model mutation) -----

		[Test]
		public void Show_ResolvesTheChosenEntry_ViaEntryGoLauncherShared()
		{
			// LcmGoToEntryDialogLauncher.Apply resolves via EntryGoLauncherShared.ResolveEntry; exercise that
			// resolution directly against a real cache (the modal loop itself is covered by the headless
			// EntryGoDialogTests).
			var resolved = EntryGoLauncherShared.ResolveEntry(Cache, _casa.Hvo.ToString(CultureInfo.InvariantCulture));
			Assert.That(resolved, Is.SameAs(_casa), "a chosen entry id resolves back to the live entry");
		}

		[Test]
		public void Show_NullOrUnparsableId_ResolvesToNull()
		{
			Assert.That(EntryGoLauncherShared.ResolveEntry(Cache, null), Is.Null);
			Assert.That(EntryGoLauncherShared.ResolveEntry(Cache, "not-an-id"), Is.Null);
		}

		// ----- Show's null-cache guard runs BEFORE the launcher is constructed or any modal is touched -----

		[Test]
		public void Show_NullCache_ThrowsArgumentNullException_BeforeConstructingLauncherOrShowingAModal()
		{
			// LcmGoToEntryDialogLauncher.Show checks "cache == null" first thing, before `new
			// LcmGoToEntryDialogLauncher(...)` / Run / AvaloniaDialogHost.ShowModal ever execute, so this guard is
			// reachable and provable without any Avalonia/WinForms modal machinery. If this guard regressed (e.g.
			// were deleted or reordered after the launcher construction/Run call), GoLinkEntryDlgListener.OnGotoLexEntry
			// callers with a null cache would instead crash with a cryptic NullReferenceException deep inside
			// BuildState/BuildInput rather than a clear, named ArgumentNullException.
			var ex = Assert.Throws<ArgumentNullException>(() =>
				LcmGoToEntryDialogLauncher.Show(null, mediator: null, propertyTable: null, owner: null));
			Assert.That(ex.ParamName, Is.EqualTo("cache"));
		}
	}
}
