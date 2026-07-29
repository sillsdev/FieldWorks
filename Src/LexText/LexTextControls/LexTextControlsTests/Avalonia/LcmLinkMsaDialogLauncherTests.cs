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
	/// The dependent auxiliary selection of the Link-MSA consumer (<see cref="LcmLinkMsaDialogLauncher"/>): like the
	/// legacy LinkMSADlg combo, a multi-MSA entry offers ALL its MSAs (in <c>MorphoSyntaxAnalysesOC</c> order, keyed
	/// by Guid, displayed by <c>InterlinearName</c>) and the CHOSEN one — not the first — is applied. Driven through
	/// the launcher's internal Build/Resolve seams over a real LcmCache (InternalsVisibleTo), like
	/// <see cref="EntryGoDialogLauncherTests"/>; the modal loop is covered by the headless FwAvaloniaDialogsTests.
	/// </summary>
	[TestFixture]
	public class LcmLinkMsaDialogLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ILexEntry _casa;

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
			components.LexemeFormAlternatives.Add(TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
			components.GlossAlternatives.Add(TsStringUtils.MakeString("house", Cache.DefaultAnalWs));
			_casa = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(components);
			// A SECOND MSA so the entry is multi-MSA (the factory already created one stem MSA for the sense).
			_casa.MorphoSyntaxAnalysesOC.Add(Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create());
		}

		private string Hvo(ICmObject obj) => obj.Hvo.ToString(CultureInfo.InvariantCulture);

		[Test]
		public void BuildInput_CarriesTheAuxiliarySpec()
		{
			var input = LcmLinkMsaDialogLauncher.BuildInput(Cache, null, null, _casa);

			Assert.That(input.AuxiliaryLabel, Is.EqualTo(FwAvaloniaDialogsStrings.LinkMsaGrammaticalInfoLabel),
				"the picker label is the legacy 'Grammatical Info.' wording");
			Assert.That(input.AuxiliaryOptions, Is.Not.Null, "the MSA resolver makes the dialog two-stage");
		}

		[Test]
		public void GetMsaOptions_OffersAllMsasInLegacyOrder()
		{
			var msas = _casa.MorphoSyntaxAnalysesOC.ToList();
			Assert.That(msas.Count, Is.GreaterThanOrEqualTo(2), "the fixture entry is multi-MSA");

			var options = LcmLinkMsaDialogLauncher.GetMsaOptions(Cache, Hvo(_casa));

			Assert.That(options.Select(o => o.Key), Is.EqualTo(msas.Select(m => m.Guid.ToString())),
				"one option per MSA, keyed by Guid, in the legacy combo's MorphoSyntaxAnalysesOC order");
			Assert.That(options.Select(o => o.Text), Is.EqualTo(msas.Select(m => m.InterlinearName)),
				"each option displays the MSA's InterlinearName (the legacy LMsa.ToString)");
		}

		[Test]
		public void GetMsaOptions_UnknownEntry_ReturnsEmpty()
		{
			Assert.That(LcmLinkMsaDialogLauncher.GetMsaOptions(Cache, "0"), Is.Empty);
		}

		[Test]
		public void ResolveSelectedMsa_AppliesTheChosenMsa_NotTheFirst()
		{
			var second = _casa.MorphoSyntaxAnalysesOC.Skip(1).First();

			var resolved = LcmLinkMsaDialogLauncher.ResolveSelectedMsa(Cache, Hvo(_casa), second.Guid.ToString());

			Assert.That(resolved, Is.SameAs(second), "the CHOSEN (non-first) MSA is applied");
			Assert.That(resolved, Is.Not.SameAs(_casa.MorphoSyntaxAnalysesOC.First()),
				"proof the pick beats the old first-MSA behavior");
		}

		[Test]
		public void ResolveSelectedMsa_NoKey_FallsBackToTheFirstMsa()
		{
			var resolved = LcmLinkMsaDialogLauncher.ResolveSelectedMsa(Cache, Hvo(_casa));
			Assert.That(resolved, Is.SameAs(_casa.MorphoSyntaxAnalysesOC.First()),
				"a missing key falls back to the legacy combo's default (first) selection");
		}
	}
}
