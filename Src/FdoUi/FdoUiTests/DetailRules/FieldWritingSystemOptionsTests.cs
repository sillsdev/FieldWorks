// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The writing systems a multi-writing-system field offers, shows, and lets the user
	/// switch off. The distinction that matters is offered-versus-shown: a writing system the
	/// project has not checked is offered by the menu but not shown until it is chosen.
	/// </summary>
	[TestFixture]
	public class FieldWritingSystemOptionsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private const string AllAnalysis = "all analysis";
		private const string AllVernacular = "all vernacular";

		private WritingSystemFieldSpec Analysis(string optionalWs = null,
			bool forceIncludeEnglish = false)
			=> WritingSystemFieldSpec.FromLayout(0, AllAnalysis, optionalWs, forceIncludeEnglish);

		private static IReadOnlyList<string> Ids(IEnumerable<CoreWritingSystemDefinition> systems)
			=> systems.Select(ws => ws.Id).ToList();

		// An analysis writing system the project has NOT checked: active, so the menu offers it,
		// but absent from the shown set until the user picks it.
		private CoreWritingSystemDefinition AddUncheckedAnalysisWs(string id)
		{
			// The fixture already runs each test inside a unit of work; a nested one throws.
			Cache.ServiceLocator.WritingSystemManager.GetOrSet(id, out var ws);
			// Active but NOT current: the project knows it, the user has not checked it.
			Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(ws);
			return ws;
		}

		[Test]
		public void Options_OfferAnUncheckedWritingSystem_ThatDefaultShownDoesNot()
		{
			var offered = AddUncheckedAnalysisWs("de");

			var options = Ids(FieldWritingSystemOptions.Options(Cache, Analysis()));
			var shown = Ids(FieldWritingSystemOptions.DefaultShown(Cache, Analysis()));

			Assert.That(options, Does.Contain(offered.Id),
				"the menu offers an active writing system the project has not checked");
			Assert.That(shown, Does.Not.Contain(offered.Id),
				"but the field does not show it until the user chooses it");
			Assert.That(shown, Is.SubsetOf(options), "everything shown must be offerable");
		}

		[Test]
		public void Options_AddTheOptionalSpecsWritingSystems_AfterItsOwn()
		{
			var withoutOptional = Ids(FieldWritingSystemOptions.Options(Cache, Analysis()));

			var withOptional = Ids(FieldWritingSystemOptions.Options(Cache, Analysis(AllVernacular)));

			Assert.That(withOptional.Take(withoutOptional.Count), Is.EqualTo(withoutOptional),
				"the field's own writing systems come first, in their own order");
			Assert.That(withOptional, Is.SupersetOf(withoutOptional));
			Assert.That(withOptional, Does.Contain(Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id),
				"the optional spec's writing systems are offered too");
		}

		[Test]
		public void Options_OfTheOptionalSpec_AreNotShownByDefault()
		{
			var vernacular = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;

			var shown = Ids(FieldWritingSystemOptions.DefaultShown(Cache, Analysis(AllVernacular)));

			Assert.That(shown, Does.Not.Contain(vernacular),
				"the optional spec widens what can be chosen, never what is shown unasked");
		}

		[Test]
		public void Options_OfAPartWithoutAnOptionalSpec_AddNothing()
		{
			// The no-magic-set guard earns its keep here: the query treats an unrecognized
			// set as the analysis one, so without it every part lacking optionalWs would
			// offer the analysis writing systems.
			var vernacularField = WritingSystemFieldSpec.FromLayout(0, AllVernacular, null, false);
			var analysisWs = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;

			var options = Ids(FieldWritingSystemOptions.Options(Cache, vernacularField));

			Assert.That(options, Does.Not.Contain(analysisWs),
				"a null optional spec must widen the options by nothing at all");
			Assert.That(options,
				Is.EqualTo(Ids(FieldWritingSystemOptions.Options(Cache,
					WritingSystemFieldSpec.FromLayout(0, AllVernacular, null, false)))));
		}

		[Test]
		public void Options_OfASpecNamingNoMagicSet_AreEmpty()
		{
			// Deliberately unlike the composer's render-side resolver, which falls back to the
			// analysis set: a field whose spec names no set has no writing systems to offer.
			var spec = WritingSystemFieldSpec.FromLayout(0, "not a magic writing system set",
				null, false);

			Assert.That(FieldWritingSystemOptions.Options(Cache, spec), Is.Empty);
			Assert.That(FieldWritingSystemOptions.DefaultShown(Cache, spec), Is.Empty);
			Assert.That(FieldWritingSystemOptions.Menu(Cache, spec, null), Is.Empty);
		}

		[Test]
		public void Shown_IsTheStoredSelection_InOptionOrder()
		{
			var second = AddUncheckedAnalysisWs("de");
			var first = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;

			// Stored with the re-checked one appended, as a menu toggle leaves it.
			var shown = Ids(FieldWritingSystemOptions.Shown(Cache, Analysis(),
				new[] { second.Id, first.Id }));

			Assert.That(shown, Is.EqualTo(new[] { first.Id, second.Id }),
				"the field renders its selection in option order, whatever order it was stored in");
		}

		[Test]
		public void Shown_DropsAStoredIdThatIsNoLongerAnOption()
		{
			var real = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;

			var shown = Ids(FieldWritingSystemOptions.Shown(Cache, Analysis(),
				new[] { "zzz-no-such-ws", real.Id }));

			Assert.That(shown, Is.EqualTo(new[] { real.Id }), "a stale id must not survive");
		}

		[Test]
		public void Shown_FallsBackToTheDefault_WhenNothingStoredSurvives()
		{
			var expected = Ids(FieldWritingSystemOptions.DefaultShown(Cache, Analysis()));

			Assert.That(Ids(FieldWritingSystemOptions.Shown(Cache, Analysis(), null)),
				Is.EqualTo(expected), "no stored selection shows the default set");
			Assert.That(Ids(FieldWritingSystemOptions.Shown(Cache, Analysis(), new string[0])),
				Is.EqualTo(expected));
			Assert.That(Ids(FieldWritingSystemOptions.Shown(Cache, Analysis(), new[] { "zzz-no-such-ws" })),
				Is.EqualTo(expected), "an entirely stale selection must not blank the field");
		}

		[Test]
		public void Menu_ChecksWhatIsShown_AndOffersWhatIsNot()
		{
			var offeredOnly = AddUncheckedAnalysisWs("de");
			var analysis = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;

			var menu = FieldWritingSystemOptions.Menu(Cache, Analysis(), new[] { analysis.Id });

			Assert.That(Ids(menu.Select(m => m.WritingSystem)),
				Is.EqualTo(Ids(FieldWritingSystemOptions.Options(Cache, Analysis()))),
				"the menu lists every option, in option order");
			Assert.That(menu.Single(m => m.Id == analysis.Id).IsChecked, Is.True);
			Assert.That(menu.Single(m => m.Id == offeredOnly.Id).IsChecked, Is.False);
			Assert.That(menu.Single(m => m.Id == analysis.Id).Label,
				Is.EqualTo(analysis.DisplayLabel), "the menu text is the writing system's label");
		}

		[Test]
		public void Menu_CannotUncheckTheLastShownWritingSystem()
		{
			var second = AddUncheckedAnalysisWs("de");
			var first = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;

			var one = FieldWritingSystemOptions.Menu(Cache, Analysis(), new[] { first.Id });
			Assert.That(one.Single(m => m.Id == first.Id).CanUncheck, Is.False,
				"the only shown writing system cannot be switched off, so the field is never blank");
			Assert.That(one.Single(m => m.Id == second.Id).CanUncheck, Is.True,
				"an unshown one is always available to switch on");

			var two = FieldWritingSystemOptions.Menu(Cache, Analysis(), new[] { first.Id, second.Id });
			Assert.That(two.Where(m => m.IsChecked).Select(m => m.CanUncheck), Is.All.True,
				"with two shown, either may be switched off");
		}

		[Test]
		public void EveryEntryPoint_RejectsAMissingCacheOrSpec()
		{
			Assert.That(() => FieldWritingSystemOptions.Options(null, Analysis()),
				Throws.ArgumentNullException);
			Assert.That(() => FieldWritingSystemOptions.Options(Cache, null),
				Throws.ArgumentNullException);
			Assert.That(() => FieldWritingSystemOptions.DefaultShown(null, Analysis()),
				Throws.ArgumentNullException);
			Assert.That(() => FieldWritingSystemOptions.Shown(Cache, null, null),
				Throws.ArgumentNullException);
			Assert.That(() => FieldWritingSystemOptions.Menu(null, Analysis(), null),
				Throws.ArgumentNullException);
			Assert.That(() => new WritingSystemMenuOption(null, false, false),
				Throws.ArgumentNullException);
		}
	}
}
