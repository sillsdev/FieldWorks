// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LexTextControlsTests
{
	/// <summary>
	/// The type-mismatch confirmation of the Add-Allomorph consumer (<see cref="LcmAddAllomorphDialogLauncher"/>):
	/// like the legacy <c>CreateAllomorphTypeMismatchDlg</c> flow, when the morpheme type deduced from the typed
	/// form's punctuation disagrees with the chosen entry's existing forms the user is asked first — No adds
	/// nothing, Yes ensures an appropriate MSA and creates the allomorph — and no prompt appears when the types
	/// match. Driven through the launcher's internal seams (a stub confirmation delegate) over a real LcmCache
	/// (InternalsVisibleTo), without any modal.
	/// </summary>
	[TestFixture]
	public class LcmAddAllomorphDialogLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ILexEntry _perro;

		// The base opens an undoable UOW in TestSetup and calls CreateTestData() inside it, so data is created
		// directly here with NO UOW wrapper (a nested task would throw "Nested tasks are not supported").
		protected override void CreateTestData()
		{
			base.CreateTestData();
			var components = new LexEntryComponents
			{
				MorphType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
					.GetObject(MoMorphTypeTags.kguidMorphStem)
			};
			components.LexemeFormAlternatives.Add(TsStringUtils.MakeString("perro", Cache.DefaultVernWs));
			components.GlossAlternatives.Add(TsStringUtils.MakeString("dog", Cache.DefaultAnalWs));
			_perro = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(components);
		}

		[Test]
		public void HasTypeMismatch_TrueForASuffixFormOnAStemEntry()
		{
			// "-s" deduces a suffix; the stem entry has no type-compatible allomorph => mismatch.
			var mismatch = LcmAddAllomorphDialogLauncher.HasTypeMismatch(Cache, _perro,
				TsStringUtils.MakeString("-s", Cache.DefaultVernWs), out var inferredType, out var strippedForm);

			Assert.That(mismatch, Is.True, "a suffix-marked form mismatches a stem-only entry");
			Assert.That(inferredType, Is.Not.Null);
			Assert.That(inferredType.Guid.ToString(), Is.EqualTo(MoMorphTypeTags.kMorphSuffix),
				"the type is deduced from the form's punctuation");
			Assert.That(strippedForm, Is.EqualTo("s"), "the morpheme markers are stripped for the messages");
		}

		[Test]
		public void HasTypeMismatch_FalseWhenTheTypesMatch()
		{
			// An unmarked form deduces a stem, which the entry's stem lexeme form satisfies.
			var mismatch = LcmAddAllomorphDialogLauncher.HasTypeMismatch(Cache, _perro,
				TsStringUtils.MakeString("perr", Cache.DefaultVernWs), out _, out _);
			Assert.That(mismatch, Is.False, "a stem form on a stem entry is consistent — no prompt");
		}

		[Test]
		public void Mismatch_TriggersTheConfirmationSeam_NoDeclinesWithNoChange()
		{
			var prompts = new List<(string Warning, string Question)>();
			var allomorphsBefore = _perro.AllAllomorphs.Count();
			var msasBefore = _perro.MorphoSyntaxAnalysesOC.Count;

			var result = LcmAddAllomorphDialogLauncher.PerformAddAllomorph(Cache, _perro,
				TsStringUtils.MakeString("-s", Cache.DefaultVernWs),
				(warning, question) => { prompts.Add((warning, question)); return false; });

			Assert.That(prompts, Has.Count.EqualTo(1), "the mismatch asks the confirmation seam exactly once");
			Assert.That(prompts[0].Warning, Does.Contain("perro"),
				"the warning names the entry (the legacy 'selected lexical entry (X) is a Y')");
			Assert.That(prompts[0].Question, Does.Contain("s"),
				"the question names the typed form (the legacy 'add the Z allomorph (F)?')");
			Assert.That(result, Is.Null, "No adds nothing (the legacy DialogResult.No outcome)");
			Assert.That(_perro.AllAllomorphs.Count(), Is.EqualTo(allomorphsBefore), "no allomorph is created");
			Assert.That(_perro.MorphoSyntaxAnalysesOC.Count, Is.EqualTo(msasBefore), "no MSA is created");
		}

		[Test]
		public void Mismatch_ConfirmedYes_EnsuresMsaAndCreatesTheAllomorph()
		{
			// PerformAddAllomorph opens its OWN undoable step on the Yes path, so end the base's open task first
			// (the LcmCreateFeatureLauncherTests pattern).
			m_actionHandler.EndUndoTask();
			var allomorphsBefore = _perro.AllAllomorphs.Count();
			Assert.That(_perro.MorphoSyntaxAnalysesOC.OfType<IMoUnclassifiedAffixMsa>().Any(), Is.False,
				"the stem entry starts without an unclassified-affix MSA");

			var created = LcmAddAllomorphDialogLauncher.PerformAddAllomorph(Cache, _perro,
				TsStringUtils.MakeString("-s", Cache.DefaultVernWs), (warning, question) => true);

			Assert.That(created, Is.Not.Null, "Yes creates the allomorph (the legacy DialogResult.Yes outcome)");
			Assert.That(_perro.AllAllomorphs.Count(), Is.EqualTo(allomorphsBefore + 1));
			Assert.That(created.MorphTypeRA.Guid.ToString(), Is.EqualTo(MoMorphTypeTags.kMorphSuffix),
				"the created form carries the deduced (suffix) morph type");
			Assert.That(_perro.MorphoSyntaxAnalysesOC.OfType<IMoUnclassifiedAffixMsa>().Any(), Is.True,
				"an appropriate MSA is ensured before the affix allomorph is created (the legacy Yes tail)");
		}

		[Test]
		public void NoMismatch_NeverAsks_AndReusesAMatchingAllomorph()
		{
			var asked = false;
			var allomorphsBefore = _perro.AllAllomorphs.Count();

			var reused = LcmAddAllomorphDialogLauncher.PerformAddAllomorph(Cache, _perro,
				TsStringUtils.MakeString("perro", Cache.DefaultVernWs),
				(warning, question) => { asked = true; return false; });

			Assert.That(asked, Is.False, "matching types: the confirmation seam is never invoked");
			Assert.That(reused, Is.SameAs(_perro.LexemeFormOA),
				"the matching allomorph is reused (the legacy 'Use Allomorph' path)");
			Assert.That(_perro.AllAllomorphs.Count(), Is.EqualTo(allomorphsBefore), "nothing new is created");
		}

		[Test]
		public void EnsureMsaForMorphType_StemType_AddsAStemMsaOnlyWhenMissing()
		{
			var stemType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
				.GetObject(MoMorphTypeTags.kguidMorphStem);
			var msasBefore = _perro.MorphoSyntaxAnalysesOC.Count;
			Assert.That(_perro.MorphoSyntaxAnalysesOC.OfType<IMoStemMsa>().Any(), Is.True,
				"the factory-created entry already carries a stem MSA");

			LcmAddAllomorphDialogLauncher.EnsureMsaForMorphType(Cache, _perro, stemType);

			Assert.That(_perro.MorphoSyntaxAnalysesOC.Count, Is.EqualTo(msasBefore),
				"an already-satisfied entry gets no duplicate MSA");
		}

		[Test]
		public void EnsureMsaForMorphType_AffixType_AddsAnUnclassifiedAffixMsaWhenMissing()
		{
			var suffixType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
				.GetObject(MoMorphTypeTags.kguidMorphSuffix);
			Assert.That(_perro.MorphoSyntaxAnalysesOC.OfType<IMoUnclassifiedAffixMsa>().Any(), Is.False);

			LcmAddAllomorphDialogLauncher.EnsureMsaForMorphType(Cache, _perro, suffixType);

			Assert.That(_perro.MorphoSyntaxAnalysesOC.OfType<IMoUnclassifiedAffixMsa>().Any(), Is.True,
				"an affix morph type ensures an unclassified-affix MSA (the legacy default branch)");
		}
	}
}
