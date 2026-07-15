// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaTests.VisualChecks;
using FwAvaloniaDialogsTests;

namespace FwAvaloniaTests
{
	/// <summary>
	/// §19c (T1/T3/T5) — rich-text DEPTH on the owned structured-text (StText) editor: the per-paragraph
	/// run-level character-style picker and the per-run writing-system retag picker (the SAME pattern
	/// FwMultiWsTextField has, staging through TrySetParagraphText), plus the inline-display-on-blur /
	/// editable-TextBox-on-focus per-run font swap (a multi-run paragraph renders a read-along TextBlock
	/// with per-run fonts when not focused). LCModel-free: a recording fake context + a host-supplied
	/// per-WS font map.
	/// </summary>
	[TestFixture]
	public class StructuredTextRichDepthTests
	{
		private static RegionParagraph MultiRunPara()
			=> new RegionParagraph(RegionRichTextEditAlgorithms.FromRuns("dog cat",
				new[]
				{
					new RegionTextRun("do", "en"),
					new RegionTextRun("g", "en", namedStyle: "Emphasis"),
					new RegionTextRun(" cat", "fr")
				}));

		private static LexicalEditRegionField Field(IReadOnlyList<RegionParagraph> paragraphs,
			IReadOnlyList<string> charStyles = null,
			IReadOnlyList<RegionWritingSystemOption> writingSystems = null,
			IReadOnlyDictionary<string, RegionRunFont> fontMap = null)
		{
			var field = new LexicalEditRegionField(
				stableId: "LexEntry/Discussion@1", label: "Discussion", field: "Discussion",
				writingSystem: null, kind: RegionFieldKind.StructuredText,
				editorClassification: EditorClassification.Known, automationId: "Discussion",
				localizationKey: null, routing: SurfaceRouting.Product, values: null, options: null,
				selectedOptionKey: null, isEditable: true, paragraphs: paragraphs);
			if (charStyles != null)
				field.AvailableNamedStyles = charStyles;
			if (writingSystems != null)
				field.AvailableWritingSystems = writingSystems;
			if (fontMap != null)
				field.WritingSystemFonts = fontMap;
			return field;
		}

		private static (FwStructuredTextField Field, Window Window) Show(LexicalEditRegionField field,
			IRegionEditContext editContext)
		{
			var control = new FwStructuredTextField(field, field.AutomationId, editContext, null, () => { });
			var window = new Window { Content = control, Width = 460, Height = 240 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			return (control, window);
		}

		private static T Find<T>(Control root, string automationId) where T : Control
			=> root.GetVisualDescendants().OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == automationId);

		// ---- character-style picker on a paragraph row ----

		[AvaloniaTest]
		public void CharStylePicker_AppliesAStyleOverTheSelectedSpan_StagingParagraphText()
		{
			var field = Field(new List<RegionParagraph> { MultiRunPara() },
				charStyles: new[] { "Strong", "Subtle Emphasis" });
			var context = new FakeRegionEditContext();
			var (control, window) = Show(field, context);

			DialogSnapshot.Capture(window, "Region-StTextDepth-01-charstyle");
			DialogLayoutAssert.AssertNoCrowding(control);

			var box = Find<TextBox>(control, "Discussion.Para.0");
			Assert.That(box, Is.Not.Null);
			var styleButton = Find<Button>(control, "Discussion.Para.0.CharStyle");
			Assert.That(styleButton, Is.Not.Null, "an editable styleable paragraph exposes the char-style affordance");

			box.SelectionStart = 0; // select "do"
			box.SelectionEnd = 2;
			Dispatcher.UIThread.RunJobs();

			var flyout = (Flyout)styleButton.Flyout;
			flyout.ShowAt(styleButton);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			picker.OptionsList.SelectedIndex = 1; // "Strong" (index 0 is Default/clear)
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphTextEdits, Has.Count.EqualTo(1),
				"the char-style gesture stages through the paragraph-text seam");
			Assert.That(context.ParagraphTextEdits[0].Index, Is.EqualTo(0));
			var rich = context.ParagraphTextEdits[0].Value;
			Assert.That(rich.PlainText, Is.EqualTo("dog cat"), "styling never changes the plain text");
			var styled = string.Concat(rich.Runs.Where(r => r.NamedStyle == "Strong").Select(r => r.Text));
			Assert.That(styled, Is.EqualTo("do"), "exactly the selected span carries the new style");
		}

		// 19i.2 (regression guard): the span-applying trigger buttons MUST be non-focusable. If a trigger
		// takes focus, clicking it blurs the editor TextBox, Avalonia collapses the selection to the caret on
		// LostFocus, and the gesture (style / WS retag / paragraph style) snapshots an EMPTY span and stages
		// nothing — the bug the prior tests masked by setting SelectionStart/End and calling the flyout
		// directly (never a real focus-stealing click). Asserting Focusable==false is the invariant that
		// can't be routed around.
		[AvaloniaTest]
		public void RichTextTriggerButtons_AreNonFocusable_SoTheSelectionSurvivesOpeningTheirFlyout()
		{
			var field = Field(new List<RegionParagraph> { MultiRunPara() },
				charStyles: new[] { "Strong" },
				writingSystems: new[] { new RegionWritingSystemOption("fr", "French") });
			field.AvailableParagraphStyles = new[] { "Heading", "Block Quotation" };
			var (control, _) = Show(field, new FakeRegionEditContext());

			foreach (var id in new[]
			{
				"Discussion.Para.0.CharStyle",      // char-style span picker
				"Discussion.Para.0.WritingSystem",  // per-run WS retag picker
				"Discussion.Para.0.Style"           // paragraph-style picker
			})
			{
				var button = Find<Button>(control, id);
				Assert.That(button, Is.Not.Null, $"{id} affordance is present");
				Assert.That(button.Focusable, Is.False,
					$"{id} must not steal focus from the editor (else LostFocus collapses the selection → no-op)");
			}
		}

		[AvaloniaTest]
		public void CharStyleAffordance_Absent_WhenNoAvailableStyles()
		{
			var field = Field(new List<RegionParagraph> { MultiRunPara() } /* no char styles */);
			var (control, _) = Show(field, new FakeRegionEditContext());
			Assert.That(Find<Button>(control, "Discussion.Para.0.CharStyle"), Is.Null,
				"a paragraph with no available character styles shows no char-style picker");
		}

		[AvaloniaTest]
		public void CharStyleAffordance_HasAutomationIdAndAccessibleName()
		{
			var field = Field(new List<RegionParagraph> { MultiRunPara() }, charStyles: new[] { "Strong" });
			var (control, _) = Show(field, new FakeRegionEditContext());
			var styleButton = Find<Button>(control, "Discussion.Para.0.CharStyle");
			Assert.That(styleButton, Is.Not.Null);
			Assert.That(AutomationProperties.GetName(styleButton), Is.EqualTo(FwAvaloniaStrings.CharacterStyle));
		}

		// ---- writing-system retag picker on a paragraph row ----

		[AvaloniaTest]
		public void WsRetagPicker_RetagsTheSelectedSpan_StagingParagraphText()
		{
			var field = Field(new List<RegionParagraph> { MultiRunPara() },
				writingSystems: new[]
				{
					new RegionWritingSystemOption("fr", "French"),
					new RegionWritingSystemOption("de", "German")
				});
			var context = new FakeRegionEditContext();
			var (control, window) = Show(field, context);

			DialogSnapshot.Capture(window, "Region-StTextDepth-02-wsretag");
			DialogLayoutAssert.AssertNoCrowding(control);

			var box = Find<TextBox>(control, "Discussion.Para.0");
			var wsButton = Find<Button>(control, "Discussion.Para.0.WritingSystem");
			Assert.That(wsButton, Is.Not.Null, "an editable retaggable paragraph exposes the ws affordance");

			box.SelectionStart = 0; // select "do"
			box.SelectionEnd = 2;
			Dispatcher.UIThread.RunJobs();

			var flyout = (Flyout)wsButton.Flyout;
			flyout.ShowAt(wsButton);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			picker.OptionsList.SelectedIndex = 1; // German -> de
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ParagraphTextEdits, Has.Count.EqualTo(1));
			var rich = context.ParagraphTextEdits[0].Value;
			Assert.That(rich.PlainText, Is.EqualTo("dog cat"), "retag never changes the plain text");
			var retagged = string.Concat(rich.Runs.Where(r => r.WritingSystemTag == "de").Select(r => r.Text));
			Assert.That(retagged, Is.EqualTo("do"), "exactly the selected span carries the new ws");
		}

		[AvaloniaTest]
		public void WsRetagAffordance_Absent_WhenNoAvailableWritingSystems()
		{
			var field = Field(new List<RegionParagraph> { MultiRunPara() } /* no ws */);
			var (control, _) = Show(field, new FakeRegionEditContext());
			Assert.That(Find<Button>(control, "Discussion.Para.0.WritingSystem"), Is.Null,
				"a paragraph with no available writing systems shows no ws picker");
		}

		// ---- per-run font display swap ----

		[AvaloniaTest]
		public void MultiRunParagraph_RendersPerRunFontDisplay_WhenUnfocused()
		{
			var fonts = new Dictionary<string, RegionRunFont>
			{
				["en"] = new RegionRunFont("Charis SIL"),
				["fr"] = new RegionRunFont("Times New Roman")
			};
			var field = Field(new List<RegionParagraph> { MultiRunPara() }, fontMap: fonts);
			var context = new FakeRegionEditContext();
			var (control, window) = Show(field, context);

			DialogSnapshot.Capture(window, "Region-StTextDepth-03-perrun-font-display");
			DialogLayoutAssert.AssertNoCrowding(control);

			// A read-along display layer renders for the multi-run paragraph, with per-run Run inlines
			// carrying distinct fonts; the editable TextBox is present but hidden until focus.
			var display = Find<TextBlock>(control, "Discussion.Para.0.Display");
			Assert.That(display, Is.Not.Null, "a differing-run paragraph renders the per-run font display");
			var runs = display.Inlines.OfType<Run>().ToList();
			Assert.That(runs, Has.Count.GreaterThanOrEqualTo(2), "one inline per text run");
			var fontNames = runs.Select(r => r.FontFamily?.Name).Distinct().ToList();
			Assert.That(fontNames, Has.Some.EqualTo("Charis SIL"));
			Assert.That(fontNames, Has.Some.EqualTo("Times New Roman"),
				"the fr run carries its own font from the host map");
		}

		[AvaloniaTest]
		public void FocusSwapsToEditableTextBox_BlurSwapsBackToDisplay()
		{
			var fonts = new Dictionary<string, RegionRunFont>
			{
				["en"] = new RegionRunFont("Charis SIL"),
				["fr"] = new RegionRunFont("Times New Roman")
			};
			var field = Field(new List<RegionParagraph> { MultiRunPara() }, fontMap: fonts);
			var control = new FwStructuredTextField(field, field.AutomationId, new FakeRegionEditContext(),
				null, () => { });
			// A sibling focusable button so blur has somewhere to go (the structured field panel itself is
			// not focusable, so focusing it would not blur the box in headless).
			var blurTarget = new Button { Content = "elsewhere" };
			var window = new Window
			{
				Content = new StackPanel { Children = { control, blurTarget } },
				Width = 460,
				Height = 260
			};
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			var box = Find<TextBox>(control, "Discussion.Para.0");
			var display = Find<TextBlock>(control, "Discussion.Para.0.Display");
			Assert.That(display.IsVisible, Is.True, "unfocused: the display layer shows");
			Assert.That(box.IsVisible, Is.False, "unfocused: the editable box is collapsed out of layout");

			// A pointer press on the display swaps in the editable box and focuses it (click-to-edit).
			display.RaiseEvent(new PointerPressedEventArgs(display,
				new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true),
				display, default, 0,
				new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
				KeyModifiers.None));
			Dispatcher.UIThread.RunJobs();
			Assert.That(box.IsVisible, Is.True, "the press swaps in the editable box");
			Assert.That(display.IsVisible, Is.False, "and swaps the display out");

			// Blur by moving focus to the sibling button.
			blurTarget.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(display.IsVisible, Is.True, "blur swaps the display layer back in");
			Assert.That(box.IsVisible, Is.False, "and collapses the editable box");
		}

		[AvaloniaTest]
		public void SingleRunUniformParagraph_DoesNotBuildTheDisplayLayer()
		{
			// A uniform single-run paragraph needs no per-run font display: the plain TextBox is enough.
			var para = new RegionParagraph(RegionRichTextEditAlgorithms.FromRuns("plain",
				new[] { new RegionTextRun("plain", "en") }));
			var fonts = new Dictionary<string, RegionRunFont> { ["en"] = new RegionRunFont("Charis SIL") };
			var field = Field(new List<RegionParagraph> { para }, fontMap: fonts);
			var (control, _) = Show(field, new FakeRegionEditContext());

			Assert.That(Find<TextBlock>(control, "Discussion.Para.0.Display"), Is.Null,
				"a uniform single-run paragraph builds no per-run font display");
			Assert.That(Find<TextBox>(control, "Discussion.Para.0").IsVisible, Is.True,
				"the editable box shows directly");
		}
	}
}
