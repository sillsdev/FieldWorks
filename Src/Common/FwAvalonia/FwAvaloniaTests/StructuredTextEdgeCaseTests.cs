// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the PNG harness
using FwAvaloniaDialogsTests;        // DialogLayoutAssert — the shared geometry tripwire

namespace FwAvaloniaTests
{
	/// <summary>
	/// §19a T3 — edge cases for the owned multi-paragraph structured-text (StText) editor
	/// (<see cref="FwStructuredTextField"/>), derived from the sttext-test-research note: an empty
	/// StText (zero / one paragraph), the only-paragraph-cannot-delete invariant, RTL + complex-script
	/// (Khmer) content round-tripping, rapid interleaved insert/delete, ORC/lossy interleaving, and
	/// clear-style → Normal mapping. These pin the corners the happy-path unit tests don't reach. The
	/// view side stays LCModel-free (a recording fake context); the matching real-LCModel round-trip
	/// assertions live in StructuredTextAdapterTests / StructuredTextWorkflowTests.
	/// </summary>
	[TestFixture]
	public class StructuredTextEdgeCaseTests
	{
		private static RegionParagraph Para(string text, string style = null, string ws = "en")
			=> new RegionParagraph(
				RegionRichTextEditAlgorithms.FromRuns(text ?? string.Empty,
					string.IsNullOrEmpty(text)
						? Array.Empty<RegionTextRun>()
						: new[] { new RegionTextRun(text, ws) }),
				style);

		// An ORC/lossy paragraph: a value flagged lossy is held read-only (§19c.3).
		private static RegionParagraph LossyPara(string text)
		{
			var rich = new RegionRichTextValue(text, new[] { new RegionTextRun(text, "en") },
				richXml: null, requiresRichEditor: true, canEditRichText: true, lossyProperties: true);
			return new RegionParagraph(rich);
		}

		private static LexicalEditRegionField Field(IReadOnlyList<RegionParagraph> paragraphs,
			bool isEditable = true, IReadOnlyList<string> paragraphStyles = null)
		{
			var field = new LexicalEditRegionField(
				stableId: "LexEntry/Discussion@1", label: "Discussion", field: "Discussion",
				writingSystem: null, kind: RegionFieldKind.StructuredText,
				editorClassification: EditorClassification.Known, automationId: "Discussion",
				localizationKey: null, routing: SurfaceRouting.Product, values: null, options: null,
				selectedOptionKey: null, isEditable: isEditable, paragraphs: paragraphs);
			if (paragraphStyles != null)
				field.AvailableParagraphStyles = paragraphStyles;
			return field;
		}

		private static (FwStructuredTextField Field, Window Window) Show(LexicalEditRegionField field,
			IRegionEditContext editContext = null, Action gestureCompleted = null)
		{
			var control = new FwStructuredTextField(field, field.AutomationId, editContext, null, gestureCompleted);
			var window = new Window { Content = control, Width = 420, Height = 220 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			return (control, window);
		}

		private static IReadOnlyList<TextBox> Boxes(FwStructuredTextField control)
			=> control.GetVisualDescendants().OfType<TextBox>().ToList();

		private static Button DeleteButton(FwStructuredTextField control, int index)
			=> control.GetVisualDescendants().OfType<Button>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "Discussion.Para." + index + ".Delete");

		// ---- C1 / C2 / B5: empty StText (zero / one paragraph) ----

		[AvaloniaTest]
		public void EmptyStText_ShowsOneEditableRow_NoCrash()
		{
			// The composer can hand an empty paragraph list for a not-yet-materialized StText; the editor
			// must still show ONE empty editable row (StTextSlice.OnEnter's create-on-edit lane).
			var field = Field(new List<RegionParagraph>());
			var context = new FakeRegionEditContext();
			var (control, window) = Show(field, context, gestureCompleted: () => { });

			DialogSnapshot.Capture(window, "Region-StText-05-empty-single-row");
			DialogLayoutAssert.AssertNoCrowding(control);

			var boxes = Boxes(control);
			Assert.That(boxes, Has.Count.EqualTo(1), "an empty StText shows exactly one row so the user can type");
			Assert.That(boxes[0].IsReadOnly, Is.False, "the lone empty row is editable (materializes on first keystroke)");
			Assert.That(DeleteButton(control, 0), Is.Null, "the only row carries no delete affordance");
		}

		[AvaloniaTest]
		public void EmptyStText_FirstKeystrokeMaterializesParagraph_StagesAtIndex0()
		{
			var field = Field(new List<RegionParagraph>());
			var context = new FakeRegionEditContext();
			var (control, _) = Show(field, context, gestureCompleted: () => { });

			// Typing into the lone empty row stages a text edit against paragraph index 0 — the seam the
			// composer's text setter turns into "create paragraphs up to the index" against a null StText.
			Boxes(control)[0].Text = "first words";
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphTextEdits, Has.Count.EqualTo(1));
			Assert.That(context.ParagraphTextEdits[0].Index, Is.EqualTo(0));
			Assert.That(context.ParagraphTextEdits[0].Value.PlainText, Is.EqualTo("first words"));
		}

		// ---- C2 / C3 / B6: the only paragraph cannot be deleted ----

		[AvaloniaTest]
		public void OnlyParagraph_CannotBeDeleted_ByButtonOrBackspace()
		{
			var field = Field(new List<RegionParagraph> { Para(string.Empty) });
			var context = new FakeRegionEditContext();
			var (control, _) = Show(field, context, gestureCompleted: () => { });

			Assert.That(DeleteButton(control, 0), Is.Null, "no delete affordance on the only paragraph");

			var box = Boxes(control)[0];
			box.Focus();
			box.CaretIndex = 0;
			Dispatcher.UIThread.RunJobs();
			box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Back });
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphDeletes, Is.Empty, "Backspace never deletes the last remaining paragraph");
		}

		// ---- C4 / B8: RTL + complex-script (Khmer) content stages and round-trips ----

		[AvaloniaTest]
		public void RtlAndComplexScript_ParagraphStagesAndRoundTrips_LosslessRuns()
		{
			// An RTL Arabic paragraph and a Khmer complex-script paragraph. Editing each must stage the
			// edited plain text WITHOUT reordering/normalizing the script, and preserve the run's ws tag.
			const string arabic = "العربية";   // RTL
			const string khmer = "ភាសាខ្មែរ";   // complex script with subscript consonants
			var field = Field(new List<RegionParagraph>
			{
				Para(arabic, ws: "ar"),
				Para(khmer, ws: "km")
			});
			var context = new FakeRegionEditContext();
			var (control, window) = Show(field, context);

			DialogSnapshot.Capture(window, "Region-StText-06-rtl-khmer");
			DialogLayoutAssert.AssertNoCrowding(control);

			var boxes = Boxes(control);
			Assert.That(boxes, Has.Count.EqualTo(2));
			Assert.That(boxes[0].Text, Is.EqualTo(arabic), "RTL content renders verbatim, not reordered");
			Assert.That(boxes[1].Text, Is.EqualTo(khmer), "complex-script content renders verbatim");

			// Edit the Khmer paragraph: append a syllable. The staged value preserves the exact characters
			// and the km ws on the unchanged head run (run-replay around the edit).
			var edited = khmer + "ៗ";
			boxes[1].Text = edited;
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphTextEdits, Has.Count.EqualTo(1));
			var staged = context.ParagraphTextEdits[0].Value;
			Assert.That(context.ParagraphTextEdits[0].Index, Is.EqualTo(1));
			Assert.That(staged.PlainText, Is.EqualTo(edited), "the complex-script edit stages losslessly");
			Assert.That(staged.Runs.Any(r => r.WritingSystemTag == "km"), Is.True,
				"the preserved run keeps its Khmer writing-system tag through the edit");
		}

		// ---- C5: rapid interleaved insert/delete do not crash or orphan an undo step ----

		[AvaloniaTest]
		public void RapidInterleavedInsertDelete_DoNotCrashOrOrphanUndo()
		{
			// Each structural gesture completes immediately (the gestureCompleted callback the host wires
			// to its one validation-gated commit + re-show). Interleaving them rapidly must remain
			// one-completed-gesture-per-action — no missed or doubled completion (which would orphan undo).
			var field = Field(new List<RegionParagraph> { Para("Alpha."), Para("Beta."), Para("Gamma.") });
			var context = new FakeRegionEditContext();
			var gestures = 0;
			var (control, _) = Show(field, context, gestureCompleted: () => gestures++);

			Button AddButton(int i) => control.GetVisualDescendants().OfType<Button>()
				.First(b => AutomationProperties.GetAutomationId(b) == "Discussion.Para." + i + ".Add");

			// Insert after 0, delete 2, insert after 1, delete 1 — fired back to back with no re-show
			// between (the snapshot list is unchanged in this headless surface; we assert seam traffic).
			AddButton(0).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			DeleteButton(control, 2).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			AddButton(1).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			DeleteButton(control, 1).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphInserts, Has.Count.EqualTo(2), "both inserts staged");
			Assert.That(context.ParagraphDeletes, Has.Count.EqualTo(2), "both deletes staged");
			Assert.That(gestures, Is.EqualTo(4),
				"each successful structural gesture completes exactly once — no orphaned/doubled commit");
		}

		[AvaloniaTest]
		public void StructuralGesture_ThatFailsToStage_DoesNotCompleteTheGesture()
		{
			// If the seam rejects a gesture (e.g. the edit-context guard), the host commit must NOT fire,
			// so a rejected action can never leave a stray empty undo step.
			var field = Field(new List<RegionParagraph> { Para("Alpha."), Para("Beta.") });
			var context = new FakeRegionEditContext { ParagraphGestureResult = false };
			var gestures = 0;
			var (control, _) = Show(field, context, gestureCompleted: () => gestures++);

			control.GetVisualDescendants().OfType<Button>()
				.First(b => AutomationProperties.GetAutomationId(b) == "Discussion.Para.0.Add")
				.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphInserts, Has.Count.EqualTo(1), "the stage was attempted");
			Assert.That(gestures, Is.EqualTo(0), "a rejected gesture must not complete (no orphaned undo step)");
		}

		// ---- C7 / B9: ORC/lossy paragraph interleaved with editable paragraphs ----

		[AvaloniaTest]
		public void OrcParagraph_StaysReadOnly_WhileEditableParagraphsStillEdit()
		{
			// §19c.3: a lossy/ORC paragraph between two editable ones stays read-only and preserved; the
			// editable neighbors still edit and stage normally.
			var field = Field(new List<RegionParagraph>
			{
				Para("Editable head."),
				LossyPara("Has an embedded object."),
				Para("Editable tail.")
			});
			var context = new FakeRegionEditContext();
			var (control, window) = Show(field, context);

			DialogSnapshot.Capture(window, "Region-StText-07-orc-interleaved");
			DialogLayoutAssert.AssertNoCrowding(control);

			var boxes = Boxes(control);
			Assert.That(boxes, Has.Count.EqualTo(3));
			Assert.That(boxes[0].IsReadOnly, Is.False, "head paragraph editable");
			Assert.That(boxes[1].IsReadOnly, Is.True, "ORC/lossy paragraph held read-only (§19c.3)");
			Assert.That(ToolTip.GetTip(boxes[1]), Is.EqualTo(FwAvaloniaStrings.EmbeddedObjectReadOnly),
				"the ORC paragraph surfaces the not-editable-here tooltip");
			Assert.That(boxes[2].IsReadOnly, Is.False, "tail paragraph editable");

			// Edit both editable paragraphs — each stages against its own index; the ORC one never stages.
			boxes[0].Text = "Editable head, changed.";
			boxes[2].Text = "Editable tail, changed.";
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphTextEdits.Select(e => e.Index), Is.EqualTo(new[] { 0, 2 }),
				"only the editable paragraphs stage; the ORC paragraph (index 1) never does");
		}

		// ---- C8 / B4: clear-style maps to the Default picker entry (Normal on the LCModel side) ----

		[AvaloniaTest]
		public void ClearStyle_PicksDefaultEntry_StagesNullStyle()
		{
			// The picker leads with a "Default" entry (index 0) that CLEARS the style — staging a null
			// style name. The composer's setter maps that null to StyleServices.NormalStyleName (asserted
			// in StructuredTextAdapterTests.ParagraphStyle_AppliedAndCleared_OneUndoStep); here we pin that
			// the view stages the CLEAR (null) when Default is chosen on a currently-styled paragraph.
			var field = Field(new List<RegionParagraph> { Para("Styled.", style: "Block Quote") },
				paragraphStyles: new[] { "Block Quote", "Numbered List" });
			var context = new FakeRegionEditContext();
			var (control, _) = Show(field, context, gestureCompleted: () => { });

			var styleButton = control.GetVisualDescendants().OfType<Button>()
				.First(b => AutomationProperties.GetAutomationId(b) == "Discussion.Para.0.Style");
			var flyout = (Flyout)styleButton.Flyout;
			flyout.ShowAt(styleButton);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			picker.OptionsList.SelectedIndex = 0; // "Default" -> clears
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphStyleEdits, Has.Count.EqualTo(1));
			Assert.That(context.ParagraphStyleEdits[0].Index, Is.EqualTo(0));
			Assert.That(context.ParagraphStyleEdits[0].Style, Is.Null,
				"the Default entry stages a null style — the composer maps null to the Normal paragraph style");
		}
	}
}
