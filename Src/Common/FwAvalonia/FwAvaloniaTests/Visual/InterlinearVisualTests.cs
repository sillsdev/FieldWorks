// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using FwAvaloniaDialogsTests; // DialogLayoutAssert

namespace FwAvaloniaTests.VisualChecks
{
	/// <summary>
	/// avalonia-interlinear-editor (task 2.3) — T5 visual stages and T3 edge cases for the read-only
	/// interlinear control. Each is captured as a Skia PNG for subjective alignment/crowding review and run
	/// through the shared <see cref="DialogLayoutAssert"/> crowding tripwire. All LCModel-free: the
	/// <see cref="InterlinearAnalysisModel"/> DTOs are built directly (the projection is tested over a real
	/// cache in xWorksTests). Edges: empty analysis (bare wordform), a multi-analysis wordform, and an
	/// RTL/complex-script morpheme run.
	/// </summary>
	[TestFixture]
	public class InterlinearVisualTests
	{
		private static InterlinearBundle Bundle(string morph, string gloss, string gram, string lexEntry = null)
			=> new InterlinearBundle(morph, gloss, gram, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
				lexEntry ?? morph);

		private static InterlinearAnalysisModel Kapula()
			=> new InterlinearAnalysisModel("kapula", new[]
			{
				new InterlinearLine("kapula", new[]
				{
					Bundle("ka-", "1Sg.Subj", "pfx", "ka-"),
					Bundle("pul", "see", "v", "pula"),
					Bundle("-a", "Indic", "sfx", "-a")
				}, Guid.NewGuid())
			}, Guid.NewGuid());

		[AvaloniaTest]
		public void ReadOnlyInterlinear_AlignsColumns()
		{
			var control = new InterlinearRegionEditor(Kapula());
			DialogSnapshot.Capture(control, "Interlinear-01-readonly", width: 460, height: 180);
			DialogLayoutAssert.AssertNoCrowding(control);

			// All three lines plus the wordform render their text (4 rows × 3 columns + wordform).
			var texts = control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
			Assert.That(texts, Does.Contain("kapula"));
			Assert.That(texts, Does.Contain("pul"));
			Assert.That(texts, Does.Contain("see"));
			Assert.That(texts, Does.Contain("Indic"));
		}

		[AvaloniaTest]
		public void EmptyAnalysis_RendersTheBareWordform()
		{
			var model = new InterlinearAnalysisModel("kapula", Array.Empty<InterlinearLine>(), Guid.NewGuid());
			var control = new InterlinearRegionEditor(model);

			DialogSnapshot.Capture(control, "Interlinear-02-bare-wordform", width: 420, height: 120);
			DialogLayoutAssert.AssertNoCrowding(control);

			Assert.That(model.HasAnalysis, Is.False);
			var texts = control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
			Assert.That(texts, Does.Contain("kapula"), "the bare wordform still shows");
		}

		[AvaloniaTest]
		public void MultiAnalysisWordform_RendersOneLinePerAnalysis()
		{
			var model = new InterlinearAnalysisModel("kapula", new[]
			{
				new InterlinearLine("kapula", new[] { Bundle("ka-", "1Sg", "pfx"), Bundle("pula", "rain", "n") },
					Guid.NewGuid()),
				new InterlinearLine("kapula", new[] { Bundle("kapula", "downpour", "n") }, Guid.NewGuid())
			}, Guid.NewGuid());
			var control = new InterlinearRegionEditor(model);

			DialogSnapshot.Capture(control, "Interlinear-03-multi-analysis", width: 460, height: 280);
			DialogLayoutAssert.AssertNoCrowding(control);

			var texts = control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
			Assert.That(texts, Does.Contain("rain"));
			Assert.That(texts, Does.Contain("downpour"), "the second analysis line renders too");
		}

		[AvaloniaTest]
		public void EditableMode_RendersSenseAndMsaChoosers_AndInvokesTheCallbacks()
		{
			// W-5 (task 3.1): when the adapter supplies edit choices, both the gloss (sense) and
			// grammatical-info (MSA) cells render editable choosers (buttons opening an FwOptionPicker)
			// instead of static TextBlocks. The control stays LCModel-free — it gets RegionChoiceOption DTOs
			// + callbacks. (The morph line stays read-only — PARITY: re-segmentation is deferred.)
			var model = Kapula();
			string chosenSenseKey = null;
			string chosenMsaKey = null;
			string chosenMorphKey = null;
			var senseKey = Guid.NewGuid().ToString();
			var msaKey = Guid.NewGuid().ToString();
			var morphKey = Guid.NewGuid().ToString();
			Func<int, int, InterlinearBundleEditChoices> editChoices = (line, bundle) =>
				new InterlinearBundleEditChoices(
					new[] { new RegionChoiceOption(senseKey, "rain"), new RegionChoiceOption(Guid.NewGuid().ToString(), "storm") },
					key => chosenSenseKey = key,
					new[] { new RegionChoiceOption(msaKey, "n"), new RegionChoiceOption(Guid.NewGuid().ToString(), "v") },
					key => chosenMsaKey = key,
					new[] { new RegionChoiceOption(morphKey, "pula₁"), new RegionChoiceOption(Guid.NewGuid().ToString(), "pula₂") },
					key => chosenMorphKey = key);

			var control = new InterlinearRegionEditor(model, "InterlinearEditor", editChoices: editChoices);
			Assert.That(control.IsEditable, Is.True);

			DialogSnapshot.Capture(control, "Interlinear-05-editable", width: 560, height: 220);
			DialogLayoutAssert.AssertNoCrowding(control);

			// Three chooser buttons per bundle (lex-entry + sense + MSA) × 3 bundles in Kapula = 9.
			var buttons = control.GetVisualDescendants().OfType<Button>().ToList();
			Assert.That(buttons, Has.Count.EqualTo(9),
				"an editable lex-entry + gloss + grammatical-info chooser per bundle (morph line stays read-only)");

			// Each chooser routes its chosen key to the right adapter callback.
			CommitFirstOption(buttons.First(b => AutomationId(b) == "InterlinearEditor.Entry0"));
			Assert.That(chosenMorphKey, Is.EqualTo(morphKey), "picking a different entry routes its morph key");

			CommitFirstOption(buttons.First(b => AutomationId(b) == "InterlinearEditor.Gloss0"));
			Assert.That(chosenSenseKey, Is.EqualTo(senseKey), "picking a sense routes its key to the adapter");

			CommitFirstOption(buttons.First(b => AutomationId(b) == "InterlinearEditor.Gram0"));
			Assert.That(chosenMsaKey, Is.EqualTo(msaKey), "picking a grammatical-info/MSA routes its key to the adapter");
		}

		private static string AutomationId(Control c) => Avalonia.Automation.AutomationProperties.GetAutomationId(c);

		private static void CommitFirstOption(Button chooserButton)
		{
			var picker = (chooserButton.Flyout as Flyout)?.Content as FwOptionPicker;
			Assert.That(picker, Is.Not.Null, "the chooser button opens an FwOptionPicker flyout");
			picker.OptionsList.SelectedIndex = 0;
			picker.CommitHighlighted();
		}

		[AvaloniaTest]
		public void RtlComplexScript_RendersRightToLeft_WithoutCrowding()
		{
			// A right-to-left vernacular run (Arabic-script morphemes) with analysis glosses.
			var model = new InterlinearAnalysisModel("كتبها", new[]
			{
				new InterlinearLine("كتبها", new[]
				{
					Bundle("كتب", "write", "v"),
					Bundle("ها", "3Sg.Fem.Obj", "sfx")
				}, Guid.NewGuid())
			}, Guid.NewGuid());
			var control = new InterlinearRegionEditor(model, "InterlinearEditor", rightToLeft: true);

			DialogSnapshot.Capture(control, "Interlinear-04-rtl-complex", width: 460, height: 180);
			DialogLayoutAssert.AssertNoCrowding(control);

			Assert.That(control.FlowDirection, Is.EqualTo(Avalonia.Media.FlowDirection.RightToLeft));
			var texts = control.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
			Assert.That(texts, Does.Contain("write"));
		}
	}
}
