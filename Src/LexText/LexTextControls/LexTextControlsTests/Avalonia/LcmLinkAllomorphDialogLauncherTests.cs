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
	/// The dependent auxiliary selection of the Link-Allomorph consumer
	/// (<see cref="LcmLinkAllomorphDialogLauncher"/>): like the legacy LinkAllomorphDlg combo, a multi-form entry
	/// offers its NON-abstract forms (lexeme form first, then alternates in order, keyed by Guid) and the CHOSEN one
	/// — not the first — is applied. Driven through the launcher's internal Build/Resolve seams over a real LcmCache
	/// (InternalsVisibleTo), like <see cref="EntryGoDialogLauncherTests"/>.
	/// </summary>
	[TestFixture]
	public class LcmLinkAllomorphDialogLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ILexEntry _casa;
		private IMoForm _alternate;
		private IMoForm _abstractAlternate;

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
			// A concrete alternate plus an ABSTRACT alternate (which the legacy combo excludes).
			_alternate = MorphServices.MakeMorph(_casa, TsStringUtils.MakeString("casas", Cache.DefaultVernWs));
			_abstractAlternate = MorphServices.MakeMorph(_casa, TsStringUtils.MakeString("cas", Cache.DefaultVernWs));
			_abstractAlternate.IsAbstract = true;
		}

		private string Hvo(ICmObject obj) => obj.Hvo.ToString(CultureInfo.InvariantCulture);

		[Test]
		public void BuildInput_CarriesTheAuxiliarySpec()
		{
			var input = LcmLinkAllomorphDialogLauncher.BuildInput(Cache, null, null, _casa);

			Assert.That(input.AuxiliaryLabel, Is.EqualTo(FwAvaloniaDialogsStrings.LinkAllomorphAllomorphLabel),
				"the picker label is the legacy 'Allomorph' wording");
			Assert.That(input.AuxiliaryOptions, Is.Not.Null, "the allomorph resolver makes the dialog two-stage");
		}

		[Test]
		public void GetAllomorphOptions_OffersNonAbstractForms_LexemeFormFirst()
		{
			var options = LcmLinkAllomorphDialogLauncher.GetAllomorphOptions(Cache, Hvo(_casa));

			Assert.That(options.Select(o => o.Key),
				Is.EqualTo(new[] { _casa.LexemeFormOA.Guid.ToString(), _alternate.Guid.ToString() }),
				"the legacy combo order: lexeme form first, then the non-abstract alternates");
			Assert.That(options.Select(o => o.Key), Has.No.Member(_abstractAlternate.Guid.ToString()),
				"abstract forms never appear (the legacy non-abstract rule)");
			Assert.That(options.Select(o => o.Text), Is.EqualTo(new[] { "casa", "casas" }),
				"each option displays the form's best vernacular text");
		}

		[Test]
		public void GetAllomorphOptions_UnknownEntry_ReturnsEmpty()
		{
			Assert.That(LcmLinkAllomorphDialogLauncher.GetAllomorphOptions(Cache, "0"), Is.Empty);
		}

		[Test]
		public void ResolveSelectedAllomorph_AppliesTheChosenForm_NotTheFirst()
		{
			var resolved = LcmLinkAllomorphDialogLauncher.ResolveSelectedAllomorph(Cache, Hvo(_casa),
				_alternate.Guid.ToString());

			Assert.That(resolved, Is.SameAs(_alternate), "the CHOSEN (non-first) form is applied");
			Assert.That(resolved, Is.Not.SameAs(_casa.LexemeFormOA),
				"proof the pick beats the old first-form behavior");
		}

		[Test]
		public void ResolveSelectedAllomorph_NoKey_FallsBackToTheLexemeForm()
		{
			var resolved = LcmLinkAllomorphDialogLauncher.ResolveSelectedAllomorph(Cache, Hvo(_casa));
			Assert.That(resolved, Is.SameAs(_casa.LexemeFormOA),
				"a missing key falls back to the legacy combo's default (first) selection");
		}

		[Test]
		public void ResolveSelectedAllomorph_AbstractFormKey_FallsBackToTheFirstConcreteForm()
		{
			// An abstract form is never offered, so its key must not resolve to it either (defensive).
			var resolved = LcmLinkAllomorphDialogLauncher.ResolveSelectedAllomorph(Cache, Hvo(_casa),
				_abstractAlternate.Guid.ToString());
			Assert.That(resolved, Is.SameAs(_casa.LexemeFormOA),
				"a key outside the offered options falls back to the first concrete form");
		}
	}
}
