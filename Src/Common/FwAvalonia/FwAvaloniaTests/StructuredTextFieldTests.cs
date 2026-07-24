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
	/// §19a — the owned multi-paragraph structured-text (StText) editor (<see cref="FwStructuredTextField"/>),
	/// the managed replacement for the legacy StTextSlice RootSite editor. These pin the editable behavior:
	/// one editor row per paragraph; a paragraph text edit stages through the paragraph seam; Enter inserts a
	/// paragraph; Backspace in an empty paragraph deletes it; the per-paragraph style picker applies a style;
	/// an ORC/lossy paragraph stays read-only. A PNG per stage is emitted for subjective review, paired with
	/// the AssertNoCrowding tripwire (paragraphs must render as distinct, dense, non-clipped rows).
	/// </summary>
	[TestFixture]
	public class StructuredTextFieldTests
	{
		private static RegionParagraph Para(string text, string style = null)
			=> new RegionParagraph(
				RegionRichTextEditAlgorithms.FromRuns(text ?? string.Empty,
					string.IsNullOrEmpty(text)
						? Array.Empty<RegionTextRun>()
						: new[] { new RegionTextRun(text, "en") }),
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

		[AvaloniaTest]
		public void ReadOnlyBaseline_RendersOneRowPerParagraph_NoEditContext()
		{
			var field = Field(new List<RegionParagraph> { Para("First paragraph."), Para("Second paragraph.") });
			var (control, window) = Show(field, editContext: null);

			DialogSnapshot.Capture(window, "Region-StText-01-readonly-baseline");
			DialogLayoutAssert.AssertNoCrowding(control);

			var boxes = Boxes(control);
			Assert.That(boxes, Has.Count.EqualTo(2), "one editor row per paragraph");
			Assert.That(boxes.All(b => b.IsReadOnly), Is.True, "with no edit context every paragraph is read-only");
			Assert.That(boxes.Select(b => b.Text), Is.EqualTo(new[] { "First paragraph.", "Second paragraph." }));
		}

		[AvaloniaTest]
		public void Editable_RendersEditableRows_AndStagesAParagraphTextEdit()
		{
			var field = Field(new List<RegionParagraph> { Para("First paragraph."), Para("Second paragraph.") });
			var context = new FakeRegionEditContext();
			var (control, window) = Show(field, context);

			DialogSnapshot.Capture(window, "Region-StText-02-editable");
			DialogLayoutAssert.AssertNoCrowding(control);

			var boxes = Boxes(control);
			Assert.That(boxes.All(b => !b.IsReadOnly), Is.True, "with an edit context every editable paragraph is editable");

			// Edit the first paragraph: the keystroke stages through the paragraph-text seam.
			boxes[0].Text = "First paragraph, edited.";
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphTextEdits, Has.Count.EqualTo(1), "the text edit stages once");
			Assert.That(context.ParagraphTextEdits[0].Index, Is.EqualTo(0), "it stages against paragraph 0");
			Assert.That(context.ParagraphTextEdits[0].Value.PlainText, Is.EqualTo("First paragraph, edited."));
		}

		[AvaloniaTest]
		public void MultiParagraph_RendersEachParagraphDistinctly()
		{
			var field = Field(new List<RegionParagraph>
			{
				Para("Alpha."), Para("Beta."), Para("Gamma.")
			});
			var context = new FakeRegionEditContext();
			var (control, window) = Show(field, context);

			DialogSnapshot.Capture(window, "Region-StText-03-multi-paragraph");
			DialogLayoutAssert.AssertNoCrowding(control);

			Assert.That(Boxes(control), Has.Count.EqualTo(3), "three distinct paragraph rows");
		}

		[AvaloniaTest]
		public void EnterAtParagraph_StagesAnInsertAfterIt()
		{
			var field = Field(new List<RegionParagraph> { Para("Only paragraph.") });
			var context = new FakeRegionEditContext();
			var (control, _) = Show(field, context, gestureCompleted: () => { });

			var box = Boxes(control)[0];
			box.Focus();
			Dispatcher.UIThread.RunJobs();
			box.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Enter
			});
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphInserts, Has.Count.EqualTo(1), "Enter inserts a paragraph");
			Assert.That(context.ParagraphInserts[0].AfterIndex, Is.EqualTo(0), "inserted after paragraph 0");
		}

		[AvaloniaTest]
		public void BackspaceInEmptyParagraph_StagesADelete_WhenMoreThanOneRemains()
		{
			var field = Field(new List<RegionParagraph> { Para("Keep this."), Para(string.Empty) });
			var context = new FakeRegionEditContext();
			var (control, _) = Show(field, context, gestureCompleted: () => { });

			var emptyBox = Boxes(control)[1];
			emptyBox.Focus();
			emptyBox.CaretIndex = 0;
			Dispatcher.UIThread.RunJobs();
			emptyBox.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Back
			});
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphDeletes, Has.Count.EqualTo(1), "Backspace in an empty paragraph deletes it");
			Assert.That(context.ParagraphDeletes[0].Index, Is.EqualTo(1), "deletes paragraph 1");
		}

		[AvaloniaTest]
		public void OnlyParagraph_HasNoDeleteAffordance_AndBackspaceDoesNotDelete()
		{
			var field = Field(new List<RegionParagraph> { Para(string.Empty) });
			var context = new FakeRegionEditContext();
			var (control, _) = Show(field, context, gestureCompleted: () => { });

			// No delete button on the only paragraph (the StText always keeps one).
			var deleteButton = control.GetVisualDescendants().OfType<Button>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "Discussion.Para.0.Delete");
			Assert.That(deleteButton, Is.Null, "the only paragraph cannot be deleted");

			var box = Boxes(control)[0];
			box.Focus();
			box.CaretIndex = 0;
			Dispatcher.UIThread.RunJobs();
			box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Back });
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphDeletes, Is.Empty, "Backspace never deletes the last remaining paragraph");
		}

		[AvaloniaTest]
		public void ParagraphStylePicker_AppliesAStyle()
		{
			var field = Field(new List<RegionParagraph> { Para("Styled paragraph.") },
				paragraphStyles: new[] { "Block Quote", "Numbered List" });
			var context = new FakeRegionEditContext();
			var (control, window) = Show(field, context, gestureCompleted: () => { });

			DialogSnapshot.Capture(window, "Region-StText-04-paragraph-style");
			DialogLayoutAssert.AssertNoCrowding(control);

			var styleButton = control.GetVisualDescendants().OfType<Button>()
				.First(b => AutomationProperties.GetAutomationId(b) == "Discussion.Para.0.Style");

			// Open the picker (its flyout is hosted in a popup, not a visual descendant) and commit a
			// style. The option set leads with a "Default" clear entry (index 0), then the supplied
			// styles: "Block Quote" (1), "Numbered List" (2).
			var flyout = (Flyout)styleButton.Flyout;
			flyout.ShowAt(styleButton);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			picker.OptionsList.SelectedIndex = 1; // "Block Quote"
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphStyleEdits, Has.Count.EqualTo(1), "the style gesture stages once");
			Assert.That(context.ParagraphStyleEdits[0].Index, Is.EqualTo(0));
			Assert.That(context.ParagraphStyleEdits[0].Style, Is.EqualTo("Block Quote"));
		}

		[AvaloniaTest]
		public void OrcParagraph_StaysReadOnly_AndEditableNeighborStaysEditable()
		{
			// §19c.3: a lossy/ORC paragraph is held read-only and preserved; the editable paragraph next
			// to it is still fully editable.
			var field = Field(new List<RegionParagraph> { Para("Editable."), LossyPara("Has unsupported formatting.") });
			var context = new FakeRegionEditContext();
			var (control, _) = Show(field, context);

			var boxes = Boxes(control);
			Assert.That(boxes, Has.Count.EqualTo(2));
			Assert.That(boxes[0].IsReadOnly, Is.False, "the editable paragraph stays editable");
			Assert.That(boxes[1].IsReadOnly, Is.True, "the ORC/lossy paragraph stays read-only (§19c.3)");

			// Editing the lossy paragraph's box is impossible (read-only); editing the good one stages.
			boxes[0].Text = "Editable, changed.";
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.ParagraphTextEdits, Has.Count.EqualTo(1));
			Assert.That(context.ParagraphTextEdits[0].Index, Is.EqualTo(0));
		}

		[AvaloniaTest]
		public void Dispose_DetachesEveryWiredHandler()
		{
			var field = Field(new List<RegionParagraph> { Para("Alpha."), Para("Beta.") },
				paragraphStyles: new[] { "Block Quote" });
			var context = new FakeRegionEditContext();
			var (control, _) = Show(field, context, gestureCompleted: () => { });

			Assert.That(control.AttachedHandlerCount, Is.GreaterThan(0), "the editable rows wired handlers");
			control.Dispose();
			Assert.That(control.AttachedHandlerCount, Is.Zero, "Dispose detaches every handler");
			control.Dispose(); // idempotent
		}
	}
}
