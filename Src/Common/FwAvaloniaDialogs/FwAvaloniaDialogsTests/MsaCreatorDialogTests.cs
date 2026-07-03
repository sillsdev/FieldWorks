// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the per-stage PNG harness (linked in via the csproj)
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The reusable "Create New Grammatical Info." dialog (MSA-port Stage 5): the Avalonia replacement for the
	/// legacy MsaCreatorDlg in New-UI mode. The dialog is essentially the LCModel-free FwMsaGroupBox hosted over the
	/// entry's read-only context (lexical entry + senses); it seeds the box from the existing MSA / morph type, has
	/// NO OK gate (like the legacy dialog), and snapshots the box's FwSandboxMsa on OK. Runtime proof on a realized
	/// headless surface, with per-stage PNGs for subjective visual review.
	/// </summary>
	[TestFixture]
	public class MsaCreatorDialogTests
	{
		private static readonly IReadOnlyList<FwPosNode> PosNodes = new List<FwPosNode>
		{
			new FwPosNode("g-noun", "Noun", 0),
			new FwPosNode("g-verb", "Verb", 0)
		};

		private static readonly IReadOnlyList<FwInflectionSlot> Slots = new List<FwInflectionSlot>
		{
			new FwInflectionSlot("s-tense", "Tense"),
			new FwInflectionSlot("s-number", "Number")
		};

		private static MsaCreatorDialogInput BasicInput(FwMsaType msaType = FwMsaType.Stem,
			string mainPosId = null, string secondaryPosId = null, string slotId = null, string senses = null) =>
			new MsaCreatorDialogInput
			{
				Title = "Create New Grammatical Info.",
				LexicalEntry = "cantar",
				Senses = senses,
				PosNodes = PosNodes,
				InitialMsaType = msaType,
				InitialMainPosId = mainPosId,
				InitialSecondaryPosId = secondaryPosId,
				InitialSlotId = slotId,
				SlotsForPos = _ => Slots
			};

		private static (MsaCreatorDialogView view, MsaCreatorDialogViewModel vm) Show(
			MsaCreatorDialogInput input, string stageName = "MsaCreator-01-initial")
		{
			var vm = new MsaCreatorDialogViewModel(input);
			var view = new MsaCreatorDialogView { DataContext = vm };
			AvaloniaDialogTestHarness.Realize(view, 500, 320, stageName, forceRenderTick: true);
			return (view, vm);
		}

		private static void Capture(Control view, string stageName)
			=> AvaloniaDialogTestHarness.Recapture(view, stageName);

		// ----- the box renders, mounted, no OK gate -----

		[AvaloniaTest]
		public void Stem_RendersMainPosOnly_AndOkIsAlwaysEnabled()
		{
			var (view, vm) = Show(BasicInput(FwMsaType.Stem));
			Assert.That(view.GetVisualDescendants().Contains(vm.MsaGroupBox), Is.True,
				"the MSA group box is mounted inside the dialog");
			Assert.That(vm.MsaGroupBox.MainCatPanel.IsVisible, Is.True, "stem shows the Main POS");
			Assert.That(vm.MsaGroupBox.AffixTypePanel.IsVisible, Is.False, "stem hides the affix-type picker");
			Assert.That(vm.OkCommand.CanExecute(null), Is.True, "the dialog has no OK gate (like MsaCreatorDlg)");
		}

		// ----- seeding from an existing MSA (the legacy edit path) -----

		[AvaloniaTest]
		public void SeedsFromExistingStemMsa_AndSnapshotsItOnOk()
		{
			var (view, vm) = Show(BasicInput(FwMsaType.Stem, mainPosId: "g-noun",
				senses: "house, home"), "MsaCreator-02-seeded-stem");

			Assert.That(vm.MsaGroupBox.MsaType, Is.EqualTo(FwMsaType.Stem));
			Assert.That(vm.MsaGroupBox.MainPosId, Is.EqualTo("g-noun"), "the box seeds the existing main POS");
			Assert.That(vm.HasSenses, Is.True, "the senses summary row shows when populated (the edit path)");

			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.Msa.MsaType, Is.EqualTo(FwMsaType.Stem));
			Assert.That(vm.Result.Msa.MainPosId, Is.EqualTo("g-noun"));
		}

		[AvaloniaTest]
		public void SeedsFromExistingInflectionalMsa_WithSlot()
		{
			var (view, vm) = Show(BasicInput(FwMsaType.Inflectional, mainPosId: "g-verb", slotId: "s-tense"),
				"MsaCreator-03-seeded-inflectional");

			Assert.That(vm.MsaGroupBox.MsaType, Is.EqualTo(FwMsaType.Inflectional));
			Assert.That(vm.MsaGroupBox.MainPosId, Is.EqualTo("g-verb"));
			Assert.That(vm.MsaGroupBox.SlotId, Is.EqualTo("s-tense"), "the seeded slot resolves after the slot feed");
			Assert.That(vm.MsaGroupBox.SlotCombo.IsVisible, Is.True);

			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.Msa.MsaType, Is.EqualTo(FwMsaType.Inflectional));
			Assert.That(vm.Result.Msa.SlotId, Is.EqualTo("s-tense"));
		}

		[AvaloniaTest]
		public void SeedsFromExistingDerivationalMsa_WithSecondaryPos()
		{
			var (view, vm) = Show(BasicInput(FwMsaType.Derivational, mainPosId: "g-verb", secondaryPosId: "g-noun"),
				"MsaCreator-04-seeded-derivational");

			Assert.That(vm.MsaGroupBox.MsaType, Is.EqualTo(FwMsaType.Derivational));
			Assert.That(vm.MsaGroupBox.SecondaryPosChooser.IsVisible, Is.True);

			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.Msa.MsaType, Is.EqualTo(FwMsaType.Derivational));
			Assert.That(vm.Result.Msa.MainPosId, Is.EqualTo("g-verb"));
			Assert.That(vm.Result.Msa.SecondaryPosId, Is.EqualTo("g-noun"));
		}

		// ----- affix type reconfigures the box live -----

		[AvaloniaTest]
		public void AffixTypeChange_ReconfiguresTheBoxLive()
		{
			var (view, vm) = Show(BasicInput(FwMsaType.Unclassified), "MsaCreator-05-unclassified");
			Assert.That(vm.MsaGroupBox.AffixTypePanel.IsVisible, Is.True);

			vm.MsaGroupBox.AffixTypeCombo.SelectedIndex = 2; // Derivational
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			Capture(view, "MsaCreator-06-derivational");

			Assert.That(vm.MsaGroupBox.MsaType, Is.EqualTo(FwMsaType.Derivational));
			Assert.That(vm.MsaGroupBox.SecondaryPosChooser.IsVisible, Is.True,
				"changing the affix type reconfigures the box live (HandleComboMSATypesChange parity)");
		}

		// ----- create-new-POS wiring -----

		[AvaloniaTest]
		public void Msa_CreateNewPosRequest_FromMainChooser_RaisesVmEventWithMainTarget()
		{
			var (_, vm) = Show(BasicInput());
			FwPosTarget? target = null;
			vm.CreateNewPosRequested += t => target = t;

			vm.MsaGroupBox.MainPosChooser.RaiseCreateNew();
			Assert.That(target, Is.EqualTo(FwPosTarget.Main));
		}

		[AvaloniaTest]
		public void Msa_AcceptCreatedPos_RefreshesAndSelectsInRequestingChooser()
		{
			var (_, vm) = Show(BasicInput());
			var created = new FwPosNode("g-adj", "Adjective", 0);
			var refreshed = new List<FwPosNode>(PosNodes) { created };

			vm.AcceptCreatedPos(FwPosTarget.Main, created, refreshed);
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.MsaGroupBox.MainPosId, Is.EqualTo("g-adj"));
		}

		// ----- localization / close contract -----

		[Test]
		public void Strings_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.MsaCreatorTitle, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.MsaCreatorLexicalEntryLabel, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.MsaCreatorGrammaticalInfoLabel, Is.Not.Null.And.Not.Empty);
		}

		[AvaloniaTest] // the VM ctor builds owned Avalonia controls — must run on the UI thread
		public void CancelCommand_ClosesWithoutAccepting()
		{
			var vm = new MsaCreatorDialogViewModel(BasicInput());
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.CancelCommand.Execute(null);

			Assert.That(vm.Accepted, Is.False);
			Assert.That(closed, Is.False);
			Assert.That(vm.Result, Is.Null, "Cancel never snapshots a result");
		}
	}
}
