// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The writing systems a sense's Reversal Entries field offers and shows. Unlike a
	/// multi-writing-system field, it shows a writing system the project has not checked
	/// until the user switches it off.
	/// </summary>
	[TestFixture]
	public class ReversalEntryWritingSystemsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private static IReadOnlyList<string> Ids(IEnumerable<CoreWritingSystemDefinition> systems)
			=> systems.Select(ws => ws.Id).ToList();

		private CoreWritingSystemDefinition First
			=> Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;

		// An analysis writing system the project has NOT checked.
		private CoreWritingSystemDefinition AddUncheckedAnalysisWs(string id)
		{
			// The fixture already runs each test inside a unit of work; a nested one throws.
			Cache.ServiceLocator.WritingSystemManager.GetOrSet(id, out var ws);
			Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(ws);
			return ws;
		}

		[Test]
		public void Options_AreEveryAnalysisWritingSystem_CheckedOrNot()
		{
			var offered = AddUncheckedAnalysisWs("de");

			var options = Ids(ReversalEntryWritingSystems.Options(Cache));

			Assert.That(options, Is.EqualTo(Ids(Cache.LanguageProject.AnalysisWritingSystems)));
			Assert.That(options, Does.Contain(offered.Id),
				"a writing system the project has not checked is still offered");
		}

		[Test]
		public void Shown_WithNothingStored_IsEveryOption()
		{
			var offered = AddUncheckedAnalysisWs("de");
			var options = Ids(ReversalEntryWritingSystems.Options(Cache));

			Assert.That(Ids(ReversalEntryWritingSystems.Shown(Cache, null)), Is.EqualTo(options));
			Assert.That(Ids(ReversalEntryWritingSystems.Shown(Cache, new string[0])), Is.EqualTo(options));
			Assert.That(Ids(ReversalEntryWritingSystems.Shown(Cache, null)), Does.Contain(offered.Id),
				"an entry in a writing system the project has not checked must still be seen");
		}

		[Test]
		public void Shown_IsTheStoredSelection_InOptionOrder()
		{
			var second = AddUncheckedAnalysisWs("de");
			var third = AddUncheckedAnalysisWs("es");

			var shown = Ids(ReversalEntryWritingSystems.Shown(Cache, new[] { third.Id, First.Id }));

			Assert.That(shown, Is.EqualTo(new[] { First.Id, third.Id }),
				"the selection shows in option order, whatever order it was stored in");
			Assert.That(shown, Does.Not.Contain(second.Id));
		}

		[Test]
		public void Shown_FallsBackToEveryOption_WhenNothingStoredSurvives()
		{
			AddUncheckedAnalysisWs("de");

			Assert.That(Ids(ReversalEntryWritingSystems.Shown(Cache, new[] { "zzz-no-such-ws" })),
				Is.EqualTo(Ids(ReversalEntryWritingSystems.Options(Cache))),
				"an entirely stale selection must not blank the field");
		}

		[Test]
		public void Menu_ChecksEveryOption_UntilTheUserChooses()
		{
			AddUncheckedAnalysisWs("de");

			var menu = ReversalEntryWritingSystems.Menu(Cache, null);

			Assert.That(Ids(menu.Select(m => m.WritingSystem)),
				Is.EqualTo(Ids(ReversalEntryWritingSystems.Options(Cache))),
				"the menu lists every option, in option order");
			Assert.That(menu.Select(m => m.IsChecked), Is.All.True);
			Assert.That(menu.Select(m => m.CanUncheck), Is.All.True,
				"with more than one shown, any may be switched off");
		}

		[Test]
		public void Menu_CannotUncheckTheLastShownWritingSystem()
		{
			var second = AddUncheckedAnalysisWs("de");

			var menu = ReversalEntryWritingSystems.Menu(Cache, new[] { First.Id });

			Assert.That(menu.Single(m => m.Id == First.Id).IsChecked, Is.True);
			Assert.That(menu.Single(m => m.Id == First.Id).CanUncheck, Is.False,
				"the only shown writing system cannot be switched off");
			Assert.That(menu.Single(m => m.Id == second.Id).IsChecked, Is.False);
			Assert.That(menu.Single(m => m.Id == second.Id).CanUncheck, Is.True,
				"an unshown one is always available to switch on");
		}

		[Test]
		public void EveryEntryPoint_RejectsAMissingCache()
		{
			Assert.That(() => ReversalEntryWritingSystems.Options(null), Throws.ArgumentNullException);
			Assert.That(() => ReversalEntryWritingSystems.Shown(null, null), Throws.ArgumentNullException);
			Assert.That(() => ReversalEntryWritingSystems.Menu(null, null), Throws.ArgumentNullException);
		}
	}
}
