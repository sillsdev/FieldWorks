// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 2.2) — T1 for the chooser options projection: the phonological
	/// inventory (phonemes, natural classes, boundary markers) becomes <see cref="RegionChoiceOption"/>s
	/// whose keys decode back to <see cref="RuleCellSpec"/> via the shared codec.
	/// </summary>
	[TestFixture]
	public class RuleFormulaOptionsTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void BuildCellOptions_ProjectsPhonemesNaturalClassesAndBoundaries_WithDecodableKeys()
		{
			IPhPhoneme p = null;
			IPhNCSegments vowel = null;
			IPhBdryMarker bdry = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Add(
					Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				p = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(p);
				p.Name.SetVernacularDefaultWritingSystem("p");
				vowel = Cache.ServiceLocator.GetInstance<IPhNCSegmentsFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(vowel);
				vowel.Abbreviation.SetAnalysisDefaultWritingSystem("V");
				bdry = Cache.ServiceLocator.GetInstance<IPhBdryMarkerFactory>()
					.Create(LangProjectTags.kguidPhRuleWordBdry, Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0]);
				bdry.Name.SetVernacularDefaultWritingSystem("#");
			});

			var options = RuleFormulaOptions.BuildCellOptions(Cache);

			var byKey = options.ToDictionary(o => o.Key, o => o);
			Assert.That(byKey.ContainsKey("P:" + p.Guid), "phoneme option present with codec key");
			Assert.That(byKey["P:" + p.Guid].Name, Is.EqualTo("p"));
			Assert.That(byKey.ContainsKey("N:" + vowel.Guid), "natural-class option present");
			Assert.That(byKey["N:" + vowel.Guid].Name, Is.EqualTo("V"));
			Assert.That(byKey.ContainsKey("B:" + bdry.Guid), "boundary option present");

			// Every key decodes back to a spec the handler can apply.
			foreach (var option in options)
				Assert.That(RuleCellSpec.FromOptionKey(option.Key), Is.Not.Null, $"key '{option.Key}' decodes");
		}
	}
}
