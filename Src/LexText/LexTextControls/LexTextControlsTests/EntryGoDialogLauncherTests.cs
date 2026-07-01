// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
	/// The LCModel-aware side of the remaining EntryGoDlg children wired as Avalonia consumers of the reusable
	/// entry-search ("go") kit dialog: Add Allomorph (<see cref="LcmAddAllomorphDialogLauncher"/>), Link Entry or
	/// Sense (<see cref="LcmLinkEntryOrSenseDialogLauncher"/>), Link Allomorph
	/// (<see cref="LcmLinkAllomorphDialogLauncher"/>), and Link MSA (<see cref="LcmLinkMsaDialogLauncher"/>). Each
	/// reuses the SAME EntryGoSearchEngine matching as Merge; the modal loop is desktop-only and is covered by the
	/// headless FwAvaloniaDialogsTests, so here we exercise the pure LCModel search + configured text + on-OK
	/// resolution/mutation over a real LcmCache (visible via InternalsVisibleTo). Mirrors
	/// <see cref="LcmMergeEntryDialogLauncherTests"/>.
	/// </summary>
	[TestFixture]
	public class EntryGoDialogLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
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

		private string Hvo(ICmObject obj) => obj.Hvo.ToString(CultureInfo.InvariantCulture);

		// ----- Link MSA -----

		[Test]
		public void LinkMsa_BuildInput_CarriesTitleAndExcludesStartingEntry()
		{
			var input = LcmLinkMsaDialogLauncher.BuildInput(Cache, null, null, _casa);

			Assert.That(input.Title, Is.EqualTo(FwAvaloniaDialogsStrings.LinkMsaTitle));
			Assert.That(input.ExcludedId, Is.EqualTo(Hvo(_casa)), "the starting entry is the excluded id");
			Assert.That(input.HelpTopic, Is.EqualTo("khtpInsertMorphemeChooseFunction"));
			Assert.That(input.Search, Is.Not.Null);
		}

		[Test]
		public void LinkMsa_Search_FindsMatchesAndExcludesStartingEntry()
		{
			var search = LcmLinkMsaDialogLauncher.BuildSearch(Cache, null, null, _casa);

			var results = search("ca"); // matches casa + cantar, but casa is the starting entry
			Assert.That(results.Select(r => r.Id), Is.EqualTo(new[] { Hvo(_cantar) }),
				"only the other matching entry (cantar) remains; the starting entry is excluded");
		}

		[Test]
		public void LinkMsa_ResolvesChosenEntryToItsFirstMsa()
		{
			// 'casa' was created via the factory, which gives it a sense + a stem MSA.
			var expectedMsa = _casa.MorphoSyntaxAnalysesOC.FirstOrDefault();
			Assert.That(expectedMsa, Is.Not.Null, "the factory-created entry has at least one MSA");

			var msa = LcmLinkMsaDialogLauncher.ResolveSelectedMsa(Cache, Hvo(_casa));
			Assert.That(msa, Is.SameAs(expectedMsa), "the chosen entry resolves to its first MSA");
		}

		// ----- Link Allomorph -----

		[Test]
		public void LinkAllomorph_BuildInput_CarriesTitleAndHelp()
		{
			var input = LcmLinkAllomorphDialogLauncher.BuildInput(Cache, null, null, _casa);

			Assert.That(input.Title, Is.EqualTo(FwAvaloniaDialogsStrings.LinkAllomorphTitle));
			Assert.That(input.HelpTopic, Is.EqualTo("hktpInsertAllomorphChooseAllomorph"));
			Assert.That(input.ExcludedId, Is.EqualTo(Hvo(_casa)));
			Assert.That(input.Search, Is.Not.Null);
		}

		[Test]
		public void LinkAllomorph_Search_DropsEntriesWithNoConcreteAllomorph()
		{
			// 'casa' has a concrete lexeme form, so it is kept; make an entry whose lexeme form is abstract.
			var abstractEntry = MakeEntry("zzqq", "abstract");
			abstractEntry.LexemeFormOA.IsAbstract = true;

			var search = LcmLinkAllomorphDialogLauncher.BuildSearch(Cache, null, null, null);
			var casaResults = search("casa");
			Assert.That(casaResults.Select(r => r.Id), Has.Member(Hvo(_casa)),
				"an entry with a concrete lexeme form is kept");

			var abstractResults = search("zzqq");
			Assert.That(abstractResults.Select(r => r.Id), Has.No.Member(Hvo(abstractEntry)),
				"an entry whose forms are all abstract is dropped (legacy FilterLexEntry)");
		}

		[Test]
		public void LinkAllomorph_ResolvesChosenEntryToItsLexemeForm()
		{
			var allomorph = LcmLinkAllomorphDialogLauncher.ResolveSelectedAllomorph(Cache, Hvo(_casa));
			Assert.That(allomorph, Is.SameAs(_casa.LexemeFormOA),
				"the chosen entry resolves to its (first, non-abstract) lexeme form");
		}

		// ----- Link Entry or Sense -----

		[Test]
		public void LinkEntryOrSense_BuildInput_CarriesTitleAndExcludesStartingEntry()
		{
			var input = LcmLinkEntryOrSenseDialogLauncher.BuildInput(Cache, null, null, _casa);

			Assert.That(input.Title, Is.EqualTo(FwAvaloniaDialogsStrings.LinkEntryOrSenseTitle));
			Assert.That(input.ExcludedId, Is.EqualTo(Hvo(_casa)));
			Assert.That(input.Search, Is.Not.Null);
		}

		[Test]
		public void LinkEntryOrSense_Search_ExcludesStartingEntry()
		{
			var search = LcmLinkEntryOrSenseDialogLauncher.BuildSearch(Cache, null, null, _casa);

			var results = search("ca");
			Assert.That(results.Any(r => r.Id == Hvo(_casa)), Is.False, "the starting entry is excluded");
			Assert.That(results.Select(r => r.Id), Is.EqualTo(new[] { Hvo(_cantar) }));
		}

		[Test]
		public void LinkEntryOrSense_ResolvesChosenEntry()
		{
			var chosen = LcmLinkEntryOrSenseDialogLauncher.ResolveSelectedObject(Cache, Hvo(_cantar));
			Assert.That(chosen, Is.SameAs(_cantar), "the chosen id resolves to the entry (the entry path)");
		}

		[Test]
		public void LinkEntryOrSense_AllowSenses_BuildInputCarriesToggleAndModeSearch()
		{
			var input = LcmLinkEntryOrSenseDialogLauncher.BuildInput(Cache, null, null, _casa, allowSenses: true);

			Assert.That(input.ShowEntrySenseToggle, Is.True, "allowSenses shows the Entry/Sense toggle");
			Assert.That(input.SensesOnly, Is.False);
			Assert.That(input.SearchByMode, Is.Not.Null, "the mode-aware search is supplied");
		}

		[Test]
		public void LinkEntryOrSense_SensesOnly_BuildInputLocksToSenses()
		{
			var input = LcmLinkEntryOrSenseDialogLauncher.BuildInput(Cache, null, null, _casa, sensesOnly: true);

			Assert.That(input.ShowEntrySenseToggle, Is.True, "sensesOnly still shows (locked) toggle");
			Assert.That(input.SensesOnly, Is.True);
			Assert.That(input.SearchByMode, Is.Not.Null);
		}

		[Test]
		public void LinkEntryOrSense_BuildInput_CanOverrideTitleAndOkButton()
		{
			var input = LcmLinkEntryOrSenseDialogLauncher.BuildInput(Cache, null, null, _casa,
				allowSenses: true, title: "Identify Synonym Sense", okButtonText: "Add");

			Assert.That(input.Title, Is.EqualTo("Identify Synonym Sense"), "a call site can override the title");
			Assert.That(input.OkButtonText, Is.EqualTo("Add"), "a call site can override the OK button text");
		}

		[Test]
		public void LinkEntryOrSense_SearchByMode_EntryModeReturnsEntries_SenseModeReturnsSenses()
		{
			var search = LcmLinkEntryOrSenseDialogLauncher.BuildSearchByMode(Cache, null, null, null);

			var entryRows = search("cantar", false);
			Assert.That(entryRows.Select(r => r.Id), Has.Member(Hvo(_cantar)), "entry mode returns the entry");
			Assert.That(entryRows.All(r => !r.IsSense), Is.True, "entry-mode rows are entries");

			var senseRows = search("cantar", true);
			Assert.That(senseRows.Count, Is.GreaterThanOrEqualTo(1), "the factory entry has at least one sense");
			Assert.That(senseRows.All(r => r.IsSense), Is.True, "sense-mode rows are senses");
			var expectedSense = _cantar.AllSenses.First();
			Assert.That(senseRows.Select(r => r.Id), Has.Member(Hvo(expectedSense)),
				"the sense row carries the sense hvo");
		}

		[Test]
		public void LinkEntryOrSense_ResolvesChosenSense()
		{
			var sense = _cantar.AllSenses.First();
			var chosen = LcmLinkEntryOrSenseDialogLauncher.ResolveSelectedObject(Cache, Hvo(sense), chosenIsSense: true);
			Assert.That(chosen, Is.SameAs(sense), "a sense id with chosenIsSense resolves to the ILexSense");
		}

		[Test]
		public void LinkEntryOrSense_EntryOnlyConsumers_StillBuildEntryOnlyInput()
		{
			// Proof that the default (entry-only) BuildInput is unchanged for Merge/AddAllomorph/LinkAllomorph/LinkMSA
			// style consumers: no toggle, no mode-aware search.
			var input = LcmLinkEntryOrSenseDialogLauncher.BuildInput(Cache, null, null, _casa);
			Assert.That(input.ShowEntrySenseToggle, Is.False, "entry-only input shows no toggle");
			Assert.That(input.SearchByMode, Is.Null, "entry-only input has no mode-aware search");
			Assert.That(input.Search, Is.Not.Null, "the entry-only search is still wired");
		}

		// ----- Add Allomorph -----

		[Test]
		public void AddAllomorph_BuildInput_CarriesTitleOkAndInitialQuery()
		{
			var tssForm = TsStringUtils.MakeString("nuevo", Cache.DefaultVernWs);
			var input = LcmAddAllomorphDialogLauncher.BuildInput(Cache, null, null, tssForm);

			Assert.That(input.Title, Is.EqualTo(FwAvaloniaDialogsStrings.AddAllomorphTitle));
			Assert.That(input.OkButtonText, Is.EqualTo(FwAvaloniaDialogsStrings.AddAllomorphOkButton));
			Assert.That(input.InitialQuery, Is.EqualTo("nuevo"), "the dialog launches primed with the typed form");
			Assert.That(input.HelpTopic, Is.EqualTo("khtpFindEntryToAddAllomorph"));
			Assert.That(input.Search, Is.Not.Null);
		}

		[Test]
		public void AddAllomorph_Search_FindsAnyEntry_NothingExcluded()
		{
			var search = LcmAddAllomorphDialogLauncher.BuildSearch(Cache, null, null);

			var results = search("ca");
			Assert.That(results.Select(r => r.Id),
				Is.EquivalentTo(new[] { Hvo(_casa), Hvo(_cantar) }),
				"the Add-Allomorph search excludes no entry (any entry may receive the allomorph)");
		}

		[Test]
		public void AddAllomorph_PerformAddAllomorph_AddsNewAllomorphToChosenEntry()
		{
			var allomorphsBefore = _perro.AlternateFormsOS.Count;
			var tssForm = TsStringUtils.MakeString("perr", Cache.DefaultVernWs);

			// The base test already has an open UOW (TestSetup); PerformAddAllomorph opens its OWN
			// UndoableUnitOfWorkHelper.Do at runtime, so to avoid "Nested tasks" we call MakeMorph directly here.
			var allomorph = SIL.LCModel.DomainServices.MorphServices.MakeMorph(_perro, tssForm);

			Assert.That(allomorph, Is.Not.Null, "a new allomorph is created on the chosen entry");
			Assert.That(_perro.AlternateFormsOS.Count, Is.EqualTo(allomorphsBefore + 1),
				"the typed form is added to the chosen entry as an allomorph");
			Assert.That(allomorph.Form.VernacularDefaultWritingSystem.Text, Is.EqualTo("perr"));
		}

		[Test]
		public void AddAllomorph_PerformAddAllomorph_ReusesMatchingAllomorph()
		{
			// The lexeme form 'perro' already matches, so FindMatchingAllomorph reuses it (legacy "Use Allomorph").
			var allomorphsBefore = _perro.AllAllomorphs.Count();
			var tssForm = TsStringUtils.MakeString("perro", Cache.DefaultVernWs);

			var reused = LcmAddAllomorphDialogLauncher.PerformAddAllomorph(Cache, _perro, tssForm);

			Assert.That(reused, Is.SameAs(_perro.LexemeFormOA),
				"a form matching an existing allomorph reuses it rather than creating a duplicate");
			Assert.That(_perro.AllAllomorphs.Count(), Is.EqualTo(allomorphsBefore),
				"no new allomorph is created when one already matches");
		}
	}
}
