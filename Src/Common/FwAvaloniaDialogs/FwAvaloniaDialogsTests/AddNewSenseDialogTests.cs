// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
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
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The reusable Add New Sense dialog (MSA-port Stage 5): the Avalonia replacement for the legacy AddNewSenseDlg
	/// in New-UI mode. The view-model hosts a read-only citation form, an editable gloss FwMultiWsTextField (staged
	/// into an in-memory edit context), and the LCModel-free FwMsaGroupBox; it gates OK on a non-empty gloss
	/// (ksFillInGloss parity) and snapshots the per-WS gloss + the box's FwSandboxMsa on OK. Runtime proof on a
	/// realized headless surface, with per-stage PNGs for subjective visual review.
	/// </summary>
	[TestFixture]
	public class AddNewSenseDialogTests
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

		private static LexicalEditRegionField TextField(string name, string automationId, params string[] wsTags)
		{
			var values = wsTags.Select(tag => new RegionWsValue(tag, string.Empty, wsTag: tag)).ToList();
			return new LexicalEditRegionField(name, name, name, null, RegionFieldKind.Text,
				default(EditorClassification), automationId, name, default(SurfaceRouting),
				values, new List<RegionChoiceOption>(), null, isEditable: true);
		}

		private static AddNewSenseDialogInput BasicInput(FwMsaType msaType = FwMsaType.Stem) =>
			new AddNewSenseDialogInput
			{
				CitationForm = "casa",
				Gloss = TextField("Gloss", "AddNewSense.Gloss", "en", "fr"),
				PosNodes = PosNodes,
				InitialMsaType = msaType,
				SlotsForPos = _ => Slots
			};

		private static (AddNewSenseDialogView view, AddNewSenseDialogViewModel vm) Show(
			AddNewSenseDialogInput input, string stageName = "AddNewSense-01-initial")
		{
			var vm = new AddNewSenseDialogViewModel(input);
			var view = new AddNewSenseDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 500, Height = 360 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture(window, stageName);
			DialogLayoutAssert.AssertNoCrowding(view);
			return (view, vm);
		}

		private static void Capture(Control view, string stageName)
		{
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture((Window)view.GetVisualRoot(), stageName);
		}

		private static TextBox GlossBox(AddNewSenseDialogViewModel vm, string wsTag)
			=> vm.GlossField.GetVisualDescendants().OfType<TextBox>()
				.First(b => Avalonia.Automation.AutomationProperties.GetAutomationId(b)
					== "AddNewSense.Gloss." + wsTag);

		// ----- OK gating: empty gloss blocks OK, typing enables it (legacy ksFillInGloss) -----

		[AvaloniaTest]
		public void EmptyGloss_BlocksOk()
		{
			var (_, vm) = Show(BasicInput(), "AddNewSense-03-invalid-empty");
			Assert.That(vm.IsValid, Is.False, "an empty gloss gates OK off (ksFillInGloss parity)");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False);
			Assert.That(vm.ValidationErrors, Is.Not.Empty);
		}

		[AvaloniaTest]
		public void TypingAGloss_EnablesOk()
		{
			var (view, vm) = Show(BasicInput());
			GlossBox(vm, "en").Text = "house";
			Capture(view, "AddNewSense-02-populated");

			Assert.That(vm.IsValid, Is.True, "a non-empty gloss clears the OK gate");
			Assert.That(vm.OkCommand.CanExecute(null), Is.True);
		}

		// ----- payload carries gloss + FwSandboxMsa -----

		[AvaloniaTest]
		public void ApplyChanges_SnapshotsGlossAndMsa()
		{
			var (view, vm) = Show(BasicInput());
			GlossBox(vm, "en").Text = "house";
			GlossBox(vm, "fr").Text = "maison";
			Dispatcher.UIThread.RunJobs();
			vm.MsaGroupBox.MainPosId = "g-noun";
			Capture(view, "AddNewSense-02-populated-all");

			vm.OkCommand.Execute(null);

			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.Result.GlossByWs["en"], Is.EqualTo("house"));
			Assert.That(vm.Result.GlossByWs["fr"], Is.EqualTo("maison"));
			Assert.That(vm.Result.Msa, Is.Not.Null, "the chosen MSA is snapshotted on OK");
			Assert.That(vm.Result.Msa.MsaType, Is.EqualTo(FwMsaType.Stem));
			Assert.That(vm.Result.Msa.MainPosId, Is.EqualTo("g-noun"),
				"the chosen main POS flows into the payload's FwSandboxMsa");
		}

		[AvaloniaTest]
		public void ApplyChanges_DropsEmptyAlternatives()
		{
			var (_, vm) = Show(BasicInput());
			GlossBox(vm, "en").Text = "house"; // leave "fr" empty
			Dispatcher.UIThread.RunJobs();

			vm.OkCommand.Execute(null);

			Assert.That(vm.Result.GlossByWs.ContainsKey("en"), Is.True);
			Assert.That(vm.Result.GlossByWs.ContainsKey("fr"), Is.False, "empty alternatives are dropped");
		}

		// ----- morph type / affix type reconfigures the MSA box -----

		[AvaloniaTest]
		public void Msa_StemConfig_ShowsMainPosOnly()
		{
			var (view, vm) = Show(BasicInput(FwMsaType.Stem), "AddNewSense-04-msa-stem");
			Assert.That(view.GetVisualDescendants().Contains(vm.MsaGroupBox), Is.True,
				"the MSA group box is mounted inside the dialog");
			Assert.That(vm.MsaGroupBox.MsaType, Is.EqualTo(FwMsaType.Stem));
			Assert.That(vm.MsaGroupBox.MainCatPanel.IsVisible, Is.True, "stem shows the Main POS");
			Assert.That(vm.MsaGroupBox.AffixTypePanel.IsVisible, Is.False, "stem hides the affix-type picker");
			Assert.That(vm.MsaGroupBox.SlotsPanel.IsVisible, Is.False, "stem hides the slots panel");
		}

		[AvaloniaTest]
		public void Msa_AffixTypeChange_ReconfiguresTheBoxLive_AndFlowsToPayload()
		{
			// Seeded as an affix (Unclassified): the affix-type picker shows. Refine to Inflectional -> slot combo
			// appears; pick a POS + slot -> the payload carries them.
			var (view, vm) = Show(BasicInput(FwMsaType.Unclassified), "AddNewSense-05-msa-unclassified");
			GlossBox(vm, "en").Text = "house";
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.MsaGroupBox.AffixTypePanel.IsVisible, Is.True, "an affix type shows the affix-type picker");

			vm.MsaGroupBox.AffixTypeCombo.SelectedIndex = 1; // Inflectional
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			Capture(view, "AddNewSense-06-msa-inflectional");

			Assert.That(vm.MsaGroupBox.MsaType, Is.EqualTo(FwMsaType.Inflectional),
				"changing the affix type reconfigures the box live (HandleComboMSATypesChange parity)");
			Assert.That(vm.MsaGroupBox.SlotCombo.IsVisible, Is.True, "inflectional shows the Slot combo");

			vm.MsaGroupBox.MainPosId = "g-verb";
			vm.MsaGroupBox.SlotId = "s-tense";
			vm.OkCommand.Execute(null);

			Assert.That(vm.Result.Msa.MsaType, Is.EqualTo(FwMsaType.Inflectional));
			Assert.That(vm.Result.Msa.MainPosId, Is.EqualTo("g-verb"));
			Assert.That(vm.Result.Msa.SlotId, Is.EqualTo("s-tense"),
				"the inflectional slot pick flows into the payload's FwSandboxMsa");
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

			Assert.That(vm.MsaGroupBox.MainPosId, Is.EqualTo("g-adj"),
				"the requesting (main) chooser selects the newly created POS");
		}

		// ----- the owned controls are mounted -----

		[AvaloniaTest]
		public void OwnedControls_AreHostedInsideTheView()
		{
			var (view, vm) = Show(BasicInput());
			var descendants = view.GetVisualDescendants().ToList();
			Assert.That(descendants.Contains(vm.GlossField), Is.True, "the gloss field is mounted");
			Assert.That(descendants.Contains(vm.MsaGroupBox), Is.True, "the MSA group box is mounted");
		}

		// ----- localization / close contract -----

		[Test]
		public void Strings_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.AddNewSenseTitle, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.AddNewSenseGlossLabel, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.AddNewSenseFillInGloss, Is.Not.Null.And.Not.Empty);
		}

		[AvaloniaTest] // the VM ctor builds owned Avalonia controls — must run on the UI thread
		public void CancelCommand_ClosesWithoutAccepting()
		{
			var vm = new AddNewSenseDialogViewModel(BasicInput());
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.CancelCommand.Execute(null);

			Assert.That(vm.Accepted, Is.False);
			Assert.That(closed, Is.False);
			Assert.That(vm.Result, Is.Null, "Cancel never snapshots a result");
		}
	}
}
