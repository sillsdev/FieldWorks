// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>Records edit-context traffic so view editing behavior can be asserted without LCModel.</summary>
	internal sealed class FakeRegionEditContext : IRegionEditContext
	{
		public readonly List<(string Field, string Ws, string Value)> TextEdits = new List<(string, string, string)>();
		public readonly List<(string Field, string Ws, RegionRichTextValue Value)> RichTextEdits
			= new List<(string, string, RegionRichTextValue)>();
		public readonly List<(string Field, string Key)> OptionEdits = new List<(string, string)>();
		public readonly List<(string Field, string Key)> ReferenceAdds = new List<(string, string)>();
		public readonly List<(string Field, string Key)> ReferenceRemoves = new List<(string, string)>();

		/// <summary>What the next reference add/remove stage reports (false = failed stage).</summary>
		public bool ReferenceGestureResult = true;

		public bool TryAddReferenceItem(LexicalEditRegionField field, string optionKey)
		{
			ReferenceAdds.Add((field.Field, optionKey));
			return ReferenceGestureResult;
		}

		public bool TryRemoveReferenceItem(LexicalEditRegionField field, string optionKey)
		{
			ReferenceRemoves.Add((field.Field, optionKey));
			return ReferenceGestureResult;
		}
		public IReadOnlyList<string> ValidateResult = new List<string>();
		public int CommitCount;
		public int CancelCount;

		/// <summary>The text edits actually CAPTURED by a Commit (those staged since the last commit/cancel
		/// boundary) — models "commit captures staged, cancel discards" so tests can assert WHICH value was
		/// committed, not merely that a commit happened.</summary>
		public readonly List<(string Field, string Ws, string Value)> CommittedTextEdits
			= new List<(string, string, string)>();
		private int _stagedBoundary;

		public bool IsOpen => TextEdits.Count + OptionEdits.Count > 0 && CommitCount == 0 && CancelCount == 0;

		public bool TrySetText(LexicalEditRegionField field, string ws, string value)
		{
			TextEdits.Add((field.Field, ws, value));
			return true;
		}

		public bool TrySetRichText(LexicalEditRegionField field, string ws, RegionRichTextValue value)
		{
			RichTextEdits.Add((field.Field, ws, value));
			return true;
		}

		/// <summary>What the next option stage reports (false = rejected, e.g. an unparseable date).</summary>
		public bool OptionResult = true;

		public bool TrySetOption(LexicalEditRegionField field, string optionKey)
		{
			OptionEdits.Add((field.Field, optionKey));
			return OptionResult;
		}

		public IReadOnlyList<string> Validate() => ValidateResult;

		public void Commit()
		{
			// Capture everything staged since the last boundary — that is what this commit "writes".
			for (var i = _stagedBoundary; i < TextEdits.Count; i++)
				CommittedTextEdits.Add(TextEdits[i]);
			_stagedBoundary = TextEdits.Count;
			CommitCount++;
		}

		public void Cancel()
		{
			// Discard everything staged since the last boundary — a cancelled session writes nothing.
			_stagedBoundary = TextEdits.Count;
			CancelCount++;
		}
	}

	/// <summary>
	/// Tasks 6.8/6.10/6.6: the region view drives editing through the edit-context seam — staging on
	/// text/option change, validation-gated Save, Cancel rollback — with stable automation ids.
	/// </summary>
	[TestFixture]
	public class RegionEditingViewTests
	{
		private static ViewDefinitionModel SampleDefinition() => new ViewDefinitionModel(
			"LexEntry", "identity", "detail",
			new List<ViewNode>
			{
				new ViewNode("LexEntry/identity/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: SurfaceRouting.Product),
				new ViewNode("LexEntry/identity/#1", ViewNodeKind.Field, "Morph Type", null, "MorphType", "morphtypeatomicreference",
					EditorClassification.Known, null, ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "MorphTypeChooser", routing: SurfaceRouting.Product)
			},
			new List<ViewDiagnostic>());

		private sealed class EditingValueProvider : IRegionValueProvider
		{
			public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
				=> fieldNode.Field == "Form"
					? new List<RegionWsValue> { new RegionWsValue("vern", "casa") }
					: (IReadOnlyList<RegionWsValue>)new List<RegionWsValue>();

			public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode)
				=> new List<RegionChoiceOption> { new RegionChoiceOption("g1", "stem"), new RegionChoiceOption("g2", "suffix") };

			public string GetSelectedOptionKey(ViewNode fieldNode) => "g1";
		}

		private sealed class RichEditingValueProvider : IRegionValueProvider
		{
			public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
				=> new List<RegionWsValue>
				{
					new RegionWsValue("vern", "dog", wsTag: "qaa-x-rich",
						richText: RegionRichTextEditAlgorithms.FromRuns("dog",
							new[]
							{
								new RegionTextRun("do", "qaa-x-rich"),
								new RegionTextRun("g", "qaa-x-rich", namedStyle: "Emphasis")
							}))
				};

			public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode)
				=> new List<RegionChoiceOption>();

			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		// A value the run-replay would corrupt on edit: the run-model itself carries only supported
		// props, but the source TsString had a property the model does not round-trip (e.g. a colour),
		// so the product edge flagged the projection lossy. Such a value must render read-only.
		private sealed class LossyEditingValueProvider : IRegionValueProvider
		{
			public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
				=> new List<RegionWsValue>
				{
					new RegionWsValue("vern", "coloured", wsTag: "qaa-x-rich",
						richText: new RegionRichTextValue("coloured",
							new[] { new RegionTextRun("coloured", "qaa-x-rich") },
							richXml: "<Str/>", requiresRichEditor: true, lossyProperties: true))
				};

			public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode)
				=> new List<RegionChoiceOption>();

			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		private static (LexicalEditRegionView view, FakeRegionEditContext context, Window window) ShowEditable()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new EditingValueProvider());
			var context = new FakeRegionEditContext();
			var view = new LexicalEditRegionView(model, context);
			var window = new Window { Content = view, Width = 500, Height = 260 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (view, context, window);
		}

		private static (LexicalEditRegionView view, FakeRegionEditContext context, Window window,
			InMemoryFwClipboard clipboard) ShowRichEditable()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new RichEditingValueProvider());
			var context = new FakeRegionEditContext();
			var clipboard = new InMemoryFwClipboard();
			var view = new LexicalEditRegionView(model, context, clipboard: clipboard);
			var window = new Window { Content = view, Width = 500, Height = 260 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (view, context, window, clipboard);
		}

		private static T Find<T>(Control view, string automationId) where T : Control
			=> view.GetVisualDescendants().OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == automationId);

		[AvaloniaTest]
		public void TextChange_StagesThroughTheEditContext()
		{
			var (view, context, _) = ShowEditable();
			var box = Find<TextBox>(view, "LexemeFormEditor.vern");
			Assert.That(box, Is.Not.Null);
			Assert.That(box.IsReadOnly, Is.False, "an edit context makes the field writable");
			Assert.That(context.TextEdits, Is.Empty, "construction must not stage");

			box.Text = "perro";
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.TextEdits, Has.Count.EqualTo(1));
			Assert.That(context.TextEdits[0], Is.EqualTo(("Form", "vern", "perro")));
		}

		[AvaloniaTest]
		public void RichTextChange_StagesThroughTheRichEditContext_AndPreservesRunMetadata()
		{
			var (view, context, _, _) = ShowRichEditable();
			var box = Find<TextBox>(view, "LexemeFormEditor.qaa-x-rich");
			Assert.That(box, Is.Not.Null);
			Assert.That(context.RichTextEdits, Is.Empty, "construction must not stage");

			box.Text = "dug";
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.TextEdits, Is.Empty, "rich rows stage through the rich-text seam, not the plain-text setter");
			Assert.That(context.RichTextEdits, Has.Count.EqualTo(1));
			var rich = context.RichTextEdits[0].Value;
			Assert.That(rich.PlainText, Is.EqualTo("dug"));
			Assert.That(rich.Runs.Select(r => r.Text), Is.EqualTo(new[] { "du", "g" }));
			Assert.That(rich.Runs[1].NamedStyle, Is.EqualTo("Emphasis"),
				"the unchanged trailing run keeps its style metadata");
		}

		// DATA-SAFETY (Phase 1, test a — rendering lane): a value flagged lossy (a run carries a
		// TsString property the model does not round-trip) renders a READ-ONLY editor with the
		// not-editable-here tooltip, even though an edit context is supplied — so a keystroke can
		// never silently drop the property. The matching model/composer assertions live in xWorks's
		// LexicalEditRegionEditingTests.Compose_RunWithUnsupportedProperty_ComposesReadOnly_*.
		[AvaloniaTest]
		public void LossyValue_RendersReadOnly_WithTooltip()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new LossyEditingValueProvider());
			var context = new FakeRegionEditContext();
			var view = new LexicalEditRegionView(model, context);
			var window = new Window { Content = view, Width = 500, Height = 260 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var box = Find<TextBox>(view, "LexemeFormEditor.qaa-x-rich");
			Assert.That(box, Is.Not.Null);
			Assert.That(box.IsReadOnly, Is.True,
				"a lossy value stays read-only even with an edit context, so a keystroke cannot drop the property");
			Assert.That(ToolTip.GetTip(box), Is.EqualTo(FwAvaloniaStrings.EmbeddedObjectReadOnly),
				"the read-only lossy value surfaces the not-editable-here tooltip");
			Assert.That(context.TextEdits, Is.Empty);
			Assert.That(context.RichTextEdits, Is.Empty);
		}

		[AvaloniaTest]
		public void RichTextCopy_UsesTheSharedClipboardPayload()
		{
			var (view, _, _, clipboard) = ShowRichEditable();
			var box = Find<TextBox>(view, "LexemeFormEditor.qaa-x-rich");
			box.SelectionStart = 0;
			box.SelectionEnd = box.Text.Length;
			Dispatcher.UIThread.RunJobs();

			var flyout = box.ContextFlyout as MenuFlyout;
			var copyItem = flyout?.Items.OfType<MenuItem>().Single();
			Assert.That(copyItem, Is.Not.Null);
			copyItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			var payload = clipboard.GetText();
			Assert.That(payload, Is.Not.Null);
			Assert.That(payload.PlainText, Is.EqualTo("dog"));
			Assert.That(payload.RichText, Is.Not.Null);
			Assert.That(payload.RichText.Runs[1].NamedStyle, Is.EqualTo("Emphasis"));
		}

		[AvaloniaTest]
		public void ChooserFlyout_StripsTheHeavyGreyPresenterChrome_SoThePickerOwnsTheOnlyBorder()
		{
			// The "thick grey border" was the default Fluent FlyoutPresenter (its grey padding +
			// border) wrapping the picker. The option flyout must zero that chrome so the picker's
			// own thin border is the single visible boundary.
			var (view, _, _) = ShowEditable();
			var chooser = Find<FwChooserField>(view, "MorphTypeChooser");

			var flyout = (Flyout)chooser.Flyout;
			flyout.ShowAt(chooser);
			Dispatcher.UIThread.RunJobs();

			var picker = (FwOptionPicker)flyout.Content;
			var presenter = picker.GetVisualAncestors().OfType<FlyoutPresenter>().FirstOrDefault();
			Assert.That(presenter, Is.Not.Null, "the picker is hosted inside a flyout presenter");
			Assert.That(presenter.Padding, Is.EqualTo(new Thickness(0)),
				"no thick grey padding wraps the picker — the heavy presenter chrome is stripped");
			Assert.That(presenter.BorderThickness, Is.EqualTo(new Thickness(0)),
				"no heavy grey presenter border — the picker draws the single clean border");
		}

		[AvaloniaTest]
		public void ChooserChange_StagesOptionKeyThroughTheEditContext()
		{
			var (view, context, _) = ShowEditable();
			var chooser = Find<FwChooserField>(view, "MorphTypeChooser");
			Assert.That(chooser, Is.Not.Null, "the chooser renders as the owned flyout field");
			Assert.That(chooser.ValueText, Is.EqualTo("stem"), "shows the current selection");

			var flyout = (Flyout)chooser.Flyout;
			flyout.ShowAt(chooser);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			picker.OptionsList.SelectedIndex = 1; // "suffix"
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.OptionEdits, Has.Count.EqualTo(1));
			Assert.That(context.OptionEdits[0], Is.EqualTo(("MorphType", "g2")));
			Assert.That(chooser.SelectedKey, Is.EqualTo("g2"), "the staged selection becomes current");
			Assert.That(chooser.ValueText, Is.EqualTo("suffix"));
		}

		// 14.4 — autosave: the legacy view saves as you go, so a staged session commits the moment
		// an editor loses focus; there are no Save/Cancel buttons.
		[AvaloniaTest]
		public void AutoSave_OnFocusLoss_WhenClean_CommitsOnce_AndRaisesEditCompleted()
		{
			var (view, context, _) = ShowEditable();
			var completed = 0;
			view.EditCompleted += (s, e) => completed++;
			var box = Find<TextBox>(view, "LexemeFormEditor.vern");

			box.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.CommitCount, Is.EqualTo(0), "no open session, nothing to autosave");

			box.Text = "perro"; // stage: opens the session
			Dispatcher.UIThread.RunJobs();
			box.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.CommitCount, Is.EqualTo(1), "focus loss commits the open session");
			Assert.That(context.CancelCount, Is.EqualTo(0));
			Assert.That(completed, Is.EqualTo(1));
		}

		[AvaloniaTest]
		public void AutoSave_WithValidationErrors_ShowsThemInline_AndDoesNotCommit()
		{
			var (view, context, _) = ShowEditable();
			context.ValidateResult = new List<string> { "A Lexeme Form is required." };
			var box = Find<TextBox>(view, "LexemeFormEditor.vern");

			box.Text = "";
			Dispatcher.UIThread.RunJobs();
			box.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.CommitCount, Is.EqualTo(0), "validation errors must block the autosave");
			var errors = Find<TextBlock>(view, "RegionEditor.ValidationErrors");
			Assert.That(errors.IsVisible, Is.True, "a blocked autosave is never silent");
			Assert.That(errors.Text, Does.Contain("required"));
		}

		[AvaloniaTest]
		public void Escape_CancelsTheSession_AndRaisesEditCompleted()
		{
			var (view, context, _) = ShowEditable();
			var completed = 0;
			view.EditCompleted += (s, e) => completed++;

			view.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Escape,
				Source = view
			});
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.CancelCount, Is.EqualTo(1));
			Assert.That(context.CommitCount, Is.EqualTo(0));
			Assert.That(completed, Is.EqualTo(1));
		}

		[AvaloniaTest]
		public void EditMode_HasNoSaveCancelButtons_BecauseItAutoSaves()
		{
			var (view, _, _) = ShowEditable();
			Assert.That(Find<Button>(view, "RegionEditor.Save"), Is.Null, "14.4: legacy has no Save button");
			Assert.That(Find<Button>(view, "RegionEditor.Cancel"), Is.Null);
		}

		// Finding-4: chooser options can share a display name (e.g. identically named list items);
		// selection must map back by INDEX, never by name, or the wrong option's key is staged.
		[AvaloniaTest]
		public void Chooser_DuplicateDisplayNames_StagesTheOptionAtTheSelectedIndex()
		{
			var field = new LexicalEditRegionField("LexEntry/x/#0", "Morph Type", "MorphType", null,
				RegionFieldKind.Chooser, EditorClassification.Known, "DupChooser", null, SurfaceRouting.Inherit,
				null,
				new List<RegionChoiceOption>
				{
					new RegionChoiceOption("g1", "stem"),
					new RegionChoiceOption("g2", "stem") // same display name, different key
				},
				"g1");
			var context = new FakeRegionEditContext();
			var chooser = new FwChooserField(field, "DupChooser", context);
			var window = new Window { Content = chooser, Width = 300, Height = 120 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var flyout = (Flyout)chooser.Flyout;
			flyout.ShowAt(chooser);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			picker.OptionsList.SelectedIndex = 1;
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.OptionEdits, Has.Count.EqualTo(1),
				"selecting the second of two same-named options must stage");
			Assert.That(context.OptionEdits[0], Is.EqualTo(("MorphType", "g2")),
				"the staged key is the selected option's key, not the first name match");
			Assert.That(chooser.SelectedKey, Is.EqualTo("g2"));
		}

		// Finding-3 (view side): edits address the writing system by its unique IETF tag
		// (RegionWsValue.WsTag); the user-editable abbreviation is only a fallback for tag-less
		// rows (tests/fakes using aliases like "vern").
		[AvaloniaTest]
		public void TextField_StagesEditsByWsTag_FallingBackToAbbrevWithoutOne()
		{
			var field = new LexicalEditRegionField("LexEntry/x/#1", "Form", "Form", null,
				RegionFieldKind.Text, EditorClassification.Known, "TagField", null, SurfaceRouting.Inherit,
				new List<RegionWsValue>
				{
					new RegionWsValue("du", "uno", wsTag: "qaa-x-one"),
					new RegionWsValue("du", "dos") // duplicate abbreviation, no tag
				}, null, null);
			var context = new FakeRegionEditContext();
			var fieldControl = new FwMultiWsTextField(field, "TagField", context, null);
			var window = new Window { Content = fieldControl, Width = 300, Height = 120 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var boxes = fieldControl.GetVisualDescendants().OfType<TextBox>().ToList();
			Assert.That(boxes, Has.Count.EqualTo(2));
			boxes[0].Text = "uno!";
			boxes[1].Text = "dos!";
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.TextEdits, Has.Count.EqualTo(2));
			Assert.That(context.TextEdits[0], Is.EqualTo(("Form", "qaa-x-one", "uno!")),
				"the row with a tag stages by the unique IETF tag");
			Assert.That(context.TextEdits[1], Is.EqualTo(("Form", "du", "dos!")),
				"tag-less rows keep the abbreviation alias");

			// Review round 2: the per-row automation id (RegionFocusMemory's focus-restore key) must
			// be unique too, so it uses the same tag-preferred key as edits — abbreviations collide.
			Assert.That(AutomationProperties.GetAutomationId(boxes[0]), Is.EqualTo("TagField.qaa-x-one"),
				"a tagged row's automation id keys on the unique IETF tag, not the collidable abbreviation");
			Assert.That(AutomationProperties.GetAutomationId(boxes[1]), Is.EqualTo("TagField.du"),
				"tag-less rows keep the abbreviation-suffixed id");
		}

		// ITEM 3 (voice/sound writing systems): a voice/audio (IsVoice) alternative composes as a
		// read-only audio-placeholder value. The view must render it as a READ-ONLY box showing the
		// placeholder (with the audio tooltip), never a blank editable box that would corrupt the
		// recording on edit.
		[AvaloniaTest]
		public void AudioValue_RendersReadOnlyPlaceholder_NotAnEmptyEditableBox()
		{
			var field = new LexicalEditRegionField("LexEntry/x/#audio", "Pronunciation", "Pronunciation",
				null, RegionFieldKind.Text, EditorClassification.Known, "AudioField", null,
				SurfaceRouting.Inherit,
				new List<RegionWsValue>
				{
					new RegionWsValue("aud", FwAvaloniaStrings.AudioRecordingReadOnly, wsTag: "qaa-Zxxx-x-audio",
						isAudio: true)
				}, null, null, isEditable: false);
			var context = new FakeRegionEditContext();
			var fieldControl = new FwMultiWsTextField(field, "AudioField", context, null);
			var window = new Window { Content = fieldControl, Width = 300, Height = 120 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var box = fieldControl.GetVisualDescendants().OfType<TextBox>().Single();
			Assert.That(box.IsReadOnly, Is.True,
				"a voice/audio alternative is read-only (no editable box to corrupt the recording)");
			Assert.That(box.Text, Is.EqualTo(FwAvaloniaStrings.AudioRecordingReadOnly),
				"the audio placeholder is shown so the data is visible and diagnosable, not blank");
			Assert.That(ToolTip.GetTip(box), Is.EqualTo(FwAvaloniaStrings.AudioRecordingReadOnly),
				"the tooltip tells the user audio is edited in the classic view");

			// Typing must not stage an edit (the row is read-only).
			box.Text = "tampered";
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.TextEdits, Is.Empty, "a read-only audio row never stages a text edit");
		}

		// 14.1 — the ghost add-prompt is a watermark: it disappears when the user clicks in, and
		// comes back only if they leave without typing.
		[AvaloniaTest]
		public void GhostRow_WatermarkClearsOnFocus_AndRestoresWhenLeftEmpty()
		{
			var ghost = new LexicalEditRegionField("LexEntry/Normal/#0/ghost", "Lexeme Form",
				"LexemeForm", null, RegionFieldKind.Text, EditorClassification.Known, "GhostRow", null,
				SurfaceRouting.Inherit,
				new List<RegionWsValue> { new RegionWsValue("vern", "") }, null, null,
				isEditable: true, indent: 0, ghostPrompt: "Click here to add Lexeme Form");
			var model = new LexicalEditRegionModel("LexEntry", "Normal",
				new List<LexicalEditRegionField> { ghost }, new List<ViewDiagnostic>());
			var view = new LexicalEditRegionView(model, new FakeRegionEditContext());
			var window = new Window { Content = view, Width = 480, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var box = view.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(box.Watermark, Is.EqualTo("Click here to add Lexeme Form"));
			Assert.That(box.Text ?? "", Is.Empty, "the prompt is a watermark, never field content");

			box.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(box.Watermark, Is.Empty, "clicking in makes the prompt disappear");

			box.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(box.Watermark, Is.EqualTo("Click here to add Lexeme Form"),
				"leaving without typing restores the prompt");
		}

		// winforms-free-lexeme-editor.md D3 — a search-backed reference vector (SearchOptions
		// non-null): the add slot opens a SEARCH flyout (type-ahead TextBox + virtualized results
		// list) instead of materializing a full options list; selecting a result stages through
		// TryAddReferenceItem; the separator-bar affordance and item remove behavior are unchanged.
		[AvaloniaTest]
		public void SearchBackedReferenceVector_SearchFlyout_RendersAndStagesSelectedResult()
		{
			var queries = new List<string>();
			var lexicon = new List<RegionChoiceOption>
			{
				new RegionChoiceOption("e-casa", "casa"),
				new RegionChoiceOption("e-cantar", "cantar"),
				new RegionChoiceOption("e-perro", "perro")
			};
			var field = new LexicalEditRegionField("LexEntryRef/x/#0", "Components", "ComponentLexemes",
				null, RegionFieldKind.ReferenceVector, EditorClassification.Known, "Components", null,
				SurfaceRouting.Inherit, null, null, null, isEditable: true, indent: 0,
				items: new List<RegionChoiceOption> { new RegionChoiceOption("e-burro", "burro") },
				searchOptions: query =>
				{
					queries.Add(query);
					return lexicon.Where(o => o.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
						.ToList();
				});
			var context = new FakeRegionEditContext();
			var vector = new FwReferenceVectorField(field, "Components", context);
			var window = new Window { Content = vector, Width = 480, Height = 240 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			// The 6.3 affordances are unchanged: item text + trailing separator bar + add launcher.
			Assert.That(Find<TextBlock>(vector, "Components.Item.e-burro"), Is.Not.Null);
			Assert.That(vector.Children.OfType<Border>().Count(), Is.GreaterThanOrEqualTo(1),
				"the separator-bar affordance stays");
			var addButton = vector.GetVisualDescendants().OfType<Button>()
				.Single(b => AutomationProperties.GetAutomationId(b) == "Components.Add");

			var flyout = (Flyout)addButton.Flyout;
			flyout.ShowAt(addButton);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			var searchBox = picker.FilterBox;
			Assert.That(AutomationProperties.GetAutomationId(searchBox), Is.EqualTo("Components.Search"));
			var results = picker.OptionsList;
			Assert.That(AutomationProperties.GetAutomationId(results), Is.EqualTo("Components.Options"));
			Assert.That(picker.CurrentItems, Is.Empty,
				"nothing is enumerated before the user types — lexicons search, lists enumerate");

			searchBox.Text = "ca";
			Dispatcher.UIThread.RunJobs();
			Assert.That(queries, Does.Contain("ca"), "typing drives the field's search delegate");
			var shown = picker.CurrentItems;
			Assert.That(shown.Select(o => o.Key), Is.EqualTo(new[] { "e-casa", "e-cantar" }));

			// The vector add slot is multi-select: highlight + CommitHighlighted CHECKS the row; the
			// Add button (CommitChecked) commits the checked set. A single-item check commits one.
			results.SelectedIndex = 1;
			picker.CommitHighlighted();
			Assert.That(picker.CheckedKeys, Is.EqualTo(new[] { "e-cantar" }),
				"committing a row in multi-select mode checks it rather than committing immediately");
			picker.CommitChecked();
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.ReferenceAdds, Has.Count.EqualTo(1));
			Assert.That(context.ReferenceAdds[0], Is.EqualTo(("ComponentLexemes", "e-cantar")),
				"committing the checked set stages the result's key through TryAddReferenceItem");
		}

		[AvaloniaTest]
		public void SearchBackedReferenceVector_ItemRemove_StillStagesThroughTheContext()
		{
			var field = new LexicalEditRegionField("LexEntryRef/x/#0", "Components", "ComponentLexemes",
				null, RegionFieldKind.ReferenceVector, EditorClassification.Known, "Components2", null,
				SurfaceRouting.Inherit, null, null, null, isEditable: true, indent: 0,
				items: new List<RegionChoiceOption> { new RegionChoiceOption("e-burro", "burro") },
				searchOptions: query => new List<RegionChoiceOption>());
			var context = new FakeRegionEditContext();
			var vector = new FwReferenceVectorField(field, "Components2", context);
			var window = new Window { Content = vector, Width = 480, Height = 240 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var item = Find<TextBlock>(vector, "Components2.Item.e-burro");
			var removeItem = (MenuItem)((MenuFlyout)item.ContextFlyout).Items[0];
			removeItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ReferenceRemoves, Has.Count.EqualTo(1));
			Assert.That(context.ReferenceRemoves[0], Is.EqualTo(("ComponentLexemes", "e-burro")),
				"the remove behavior is unchanged for search-backed vectors");
		}

		private static LexicalEditRegionField PublishInField() => new LexicalEditRegionField(
			"LexEntry/x/#9", "Publish Entry In", "PublishIn", null,
			RegionFieldKind.ReferenceVector, EditorClassification.Known, "PublishIn", null,
			SurfaceRouting.Inherit, null,
			new List<RegionChoiceOption>
			{
				new RegionChoiceOption("p1", "Main Dictionary"),
				new RegionChoiceOption("p2", "Pocket")
			},
			null, isEditable: true, indent: 0,
			items: new List<RegionChoiceOption> { new RegionChoiceOption("p1", "Main Dictionary") });

		private static (FwReferenceVectorField vector, FakeRegionEditContext context) ShowVector(
			Action gestureCompleted)
		{
			var context = new FakeRegionEditContext();
			var vector = new FwReferenceVectorField(PublishInField(), "PublishIn", context, gestureCompleted);
			var window = new Window { Content = vector, Width = 480, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (vector, context);
		}

		// Bug "removing Publish In items not working" (a): item TextBlocks must hit-test their
		// WHOLE box (14.2: a null background only hit-tests the glyph ink), or the right-click
		// Remove flyout only opens when the pointer happens to be over a letter.
		[AvaloniaTest]
		public void VectorItemText_HasATransparentBackground_SoTheWholeItemTakesTheRightClick()
		{
			var (vector, _) = ShowVector(null);
			var item = Find<TextBlock>(vector, "PublishIn.Item.p1");
			Assert.That(item.Background, Is.Not.Null,
				"14.2: a null background only hit-tests the glyphs — right-click would miss between letters");
		}

		// Bug "removing Publish In items not working" (b): a successful remove stage completes the
		// gesture — the callback (which the view wires to its commit/re-show) fires exactly once.
		[AvaloniaTest]
		public void ReferenceRemove_Success_StagesAndFiresTheGestureCallbackOnce()
		{
			var gestures = 0;
			var (vector, context) = ShowVector(() => gestures++);

			var item = Find<TextBlock>(vector, "PublishIn.Item.p1");
			var removeItem = (MenuItem)((MenuFlyout)item.ContextFlyout).Items[0];
			removeItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ReferenceRemoves, Has.Count.EqualTo(1));
			Assert.That(context.ReferenceRemoves[0], Is.EqualTo(("PublishIn", "p1")));
			Assert.That(gestures, Is.EqualTo(1), "a successful remove completes the gesture exactly once");
		}

		[AvaloniaTest]
		public void ReferenceRemove_Failure_DoesNotFireTheGestureCallback()
		{
			var gestures = 0;
			var (vector, context) = ShowVector(() => gestures++);
			context.ReferenceGestureResult = false;

			var item = Find<TextBlock>(vector, "PublishIn.Item.p1");
			var removeItem = (MenuItem)((MenuFlyout)item.ContextFlyout).Items[0];
			removeItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ReferenceRemoves, Has.Count.EqualTo(1), "the stage was attempted");
			Assert.That(gestures, Is.EqualTo(0), "an unsuccessful stage must NOT complete the gesture");
		}

		[AvaloniaTest]
		public void ReferenceAdd_Success_FiresTheGestureCallbackOnce_AndFailureDoesNot()
		{
			var gestures = 0;
			var (vector, context) = ShowVector(() => gestures++);
			var addButton = vector.GetVisualDescendants().OfType<Button>()
				.Single(b => AutomationProperties.GetAutomationId(b) == "PublishIn.Add");
			var flyout = (Flyout)addButton.Flyout;
			flyout.ShowAt(addButton);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;

			picker.OptionsList.SelectedIndex = 1; // "Pocket"
			picker.CommitHighlighted(); // checks the row
			picker.CommitChecked();     // Add button: commits the checked set as one batch
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.ReferenceAdds, Has.Count.EqualTo(1));
			Assert.That(context.ReferenceAdds[0], Is.EqualTo(("PublishIn", "p2")));
			Assert.That(gestures, Is.EqualTo(1), "a successful add completes the gesture exactly once");

			// Re-open the flyout for a fresh picker (the previous batch hid it and cleared focus); a
			// failed stage must not complete the gesture.
			context.ReferenceGestureResult = false;
			flyout.ShowAt(addButton);
			Dispatcher.UIThread.RunJobs();
			var picker2 = (FwOptionPicker)flyout.Content;
			picker2.OptionsList.SelectedIndex = 1;
			picker2.CommitHighlighted();
			picker2.CommitChecked();
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.ReferenceAdds, Has.Count.EqualTo(2), "the second stage was attempted");
			Assert.That(gestures, Is.EqualTo(1), "a failed add must NOT complete the gesture");
		}

		// The view-level wiring of the same fix: the gesture callback runs the view's own
		// validation-gated OnSave, so a remove commits immediately and EditCompleted triggers the
		// host re-show that rebuilds Items from domain truth (no waiting for a later focus loss).
		[AvaloniaTest]
		public void ReferenceGesture_InTheView_CommitsImmediately_AndRaisesEditCompleted()
		{
			var model = new LexicalEditRegionModel("LexEntry", "test",
				new List<LexicalEditRegionField> { PublishInField() }, new List<ViewDiagnostic>());
			var context = new FakeRegionEditContext();
			var view = new LexicalEditRegionView(model, context);
			var window = new Window { Content = view, Width = 500, Height = 260 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			var completed = 0;
			view.EditCompleted += (s, e) => completed++;

			var item = Find<TextBlock>(view, "PublishIn.Item.p1");
			var removeItem = (MenuItem)((MenuFlyout)item.ContextFlyout).Items[0];
			removeItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.ReferenceRemoves, Has.Count.EqualTo(1));
			Assert.That(context.CommitCount, Is.EqualTo(1), "the remove gesture commits immediately");
			Assert.That(context.CancelCount, Is.EqualTo(0));
			Assert.That(completed, Is.EqualTo(1), "EditCompleted drives the host re-show");
		}

		[AvaloniaTest]
		public void ReferenceGesture_InTheView_WithValidationErrors_BlocksTheCommitVisibly()
		{
			var model = new LexicalEditRegionModel("LexEntry", "test",
				new List<LexicalEditRegionField> { PublishInField() }, new List<ViewDiagnostic>());
			var context = new FakeRegionEditContext
			{
				ValidateResult = new List<string> { "A Lexeme Form is required." }
			};
			var view = new LexicalEditRegionView(model, context);
			var window = new Window { Content = view, Width = 500, Height = 260 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var item = Find<TextBlock>(view, "PublishIn.Item.p1");
			var removeItem = (MenuItem)((MenuFlyout)item.ContextFlyout).Items[0];
			removeItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.CommitCount, Is.EqualTo(0), "the gesture commit stays validation-gated");
			Assert.That(Find<TextBlock>(view, "RegionEditor.ValidationErrors").IsVisible, Is.True,
				"a blocked gesture commit is never silent");
		}

		[AvaloniaTest]
		public void WithoutEditContext_ViewIsReadOnlyDisplay_WithNoFooter()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new EditingValueProvider());
			var view = new LexicalEditRegionView(model);
			var window = new Window { Content = view, Width = 500, Height = 260 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(Find<TextBox>(view, "LexemeFormEditor.vern").IsReadOnly, Is.True);
			Assert.That(Find<Button>(view, "RegionEditor.Save"), Is.Null, "display mode has no Save/Cancel footer");
		}

		// ---- Phase 2: character formatting (Ctrl+B/I/U) over a TextBox selection ----

		private static void RaiseCtrlKey(TextBox box, Key key)
		{
			box.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = key,
				KeyModifiers = KeyModifiers.Control,
				Source = box
			});
		}

		// Phase 2 test (b): Ctrl+B on a TextBox selection stages a TrySetRichText whose runs carry
		// bold over EXACTLY the selected span, leaving the rest of the value plain.
		[AvaloniaTest]
		public void CtrlB_OnSelection_StagesBoldOverExactlyTheSelectedSpan()
		{
			var (view, context, _, _) = ShowRichEditable();
			var box = Find<TextBox>(view, "LexemeFormEditor.qaa-x-rich");
			Assert.That(box.Text, Is.EqualTo("dog"));
			Assert.That(context.RichTextEdits, Is.Empty, "construction must not stage");

			box.SelectionStart = 0; // select "do"
			box.SelectionEnd = 2;
			Dispatcher.UIThread.RunJobs();
			RaiseCtrlKey(box, Key.B);
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.TextEdits, Is.Empty, "formatting stages through the rich-text seam");
			Assert.That(context.RichTextEdits, Has.Count.EqualTo(1));
			var rich = context.RichTextEdits[0].Value;
			Assert.That(rich.PlainText, Is.EqualTo("dog"), "formatting never changes the plain text");

			// Reconstruct which characters are bold from the run model.
			var boldText = string.Concat(rich.Runs.Where(r => r.Bold).Select(r => r.Text));
			var plainText = string.Concat(rich.Runs.Where(r => !r.Bold).Select(r => r.Text));
			Assert.That(boldText, Is.EqualTo("do"), "exactly the selected span is bold");
			Assert.That(plainText, Is.EqualTo("g"), "the unselected tail stays plain");
			Assert.That(rich.RichXml, Is.Null,
				"the formatted value drops RichXml so ToTsString re-emits the new bold via run-replay");
		}

		// Phase 2 test (c): a second Ctrl+B over the same span toggles bold back OFF.
		[AvaloniaTest]
		public void CtrlB_Twice_TogglesBoldOff()
		{
			var (view, context, _, _) = ShowRichEditable();
			var box = Find<TextBox>(view, "LexemeFormEditor.qaa-x-rich");
			box.SelectionStart = 0;
			box.SelectionEnd = 2;
			Dispatcher.UIThread.RunJobs();

			RaiseCtrlKey(box, Key.B);
			Dispatcher.UIThread.RunJobs();
			RaiseCtrlKey(box, Key.B);
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits, Has.Count.EqualTo(2), "each gesture stages once");
			var rich = context.RichTextEdits[1].Value;
			Assert.That(rich.Runs.Any(r => r.Bold), Is.False, "the second gesture clears the bold it set");
			Assert.That(rich.PlainText, Is.EqualTo("dog"));
		}

		// Phase 2 test (d): a lossy / read-only value never allows formatting (the whole rich-edit
		// block is gated off, so no handler is even wired and nothing stages).
		[AvaloniaTest]
		public void CtrlB_OnLossyValue_DoesNotStageFormatting()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new LossyEditingValueProvider());
			var context = new FakeRegionEditContext();
			var view = new LexicalEditRegionView(model, context);
			var window = new Window { Content = view, Width = 500, Height = 260 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var box = Find<TextBox>(view, "LexemeFormEditor.qaa-x-rich");
			Assert.That(box.IsReadOnly, Is.True);
			box.SelectionStart = 0;
			box.SelectionEnd = box.Text.Length;
			Dispatcher.UIThread.RunJobs();
			RaiseCtrlKey(box, Key.B);
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits, Is.Empty, "a lossy value is never reformatted");
			Assert.That(context.TextEdits, Is.Empty);
		}

		// Phase 2: a collapsed caret (no selection) is a no-op (no pending-format-for-caret in Phase 2).
		[AvaloniaTest]
		public void CtrlB_WithNoSelection_StagesNothing()
		{
			var (view, context, _, _) = ShowRichEditable();
			var box = Find<TextBox>(view, "LexemeFormEditor.qaa-x-rich");
			box.SelectionStart = 1;
			box.SelectionEnd = 1;
			box.CaretIndex = 1;
			Dispatcher.UIThread.RunJobs();
			RaiseCtrlKey(box, Key.B);
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits, Is.Empty, "a collapsed caret has no span to format");
		}

		// ---- Phase 3: named character style picker over a TextBox selection ----

		// A field carrying a rich value plus the project's available character styles, so the per-WS
		// style picker affordance is built.
		private static LexicalEditRegionField StyleableField(params string[] styles)
		{
			var field = new LexicalEditRegionField("LexEntry/x/#0", "Bibliography", "Bibliography", null,
				RegionFieldKind.Text, EditorClassification.Known, "BibEditor", null, SurfaceRouting.Inherit,
				new List<RegionWsValue>
				{
					new RegionWsValue("anal", "dog", wsTag: "qaa-x-rich",
						richText: RegionRichTextEditAlgorithms.FromRuns("dog",
							new[]
							{
								new RegionTextRun("do", "qaa-x-rich"),
								new RegionTextRun("g", "qaa-x-rich", namedStyle: "Emphasis")
							}))
				},
				null, null, isEditable: true, indent: 0)
			{
				AvailableNamedStyles = styles
			};
			return field;
		}

		private static (FwMultiWsTextField control, FakeRegionEditContext context, Window window)
			ShowStyleable(LexicalEditRegionField field)
		{
			var context = new FakeRegionEditContext();
			var control = new FwMultiWsTextField(field, "BibEditor", context, null);
			var window = new Window { Content = control, Width = 480, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (control, context, window);
		}

		// Phase 3 test (b): picking a style for a selection stages a TrySetRichText whose covered runs
		// carry the style; the rest of the value is untouched.
		[AvaloniaTest]
		public void StylePicker_PickingAStyle_StagesItOverTheSelectedSpan()
		{
			var (control, context, _) = ShowStyleable(StyleableField("Strong", "Subtle Emphasis"));
			var box = Find<TextBox>(control, "BibEditor.qaa-x-rich");
			Assert.That(box, Is.Not.Null);
			Assert.That(context.RichTextEdits, Is.Empty, "construction must not stage");

			var styleButton = Find<Button>(control, "BibEditor.qaa-x-rich.Style");
			Assert.That(styleButton, Is.Not.Null, "an editable styleable row exposes the style affordance");

			box.SelectionStart = 0; // select "do"
			box.SelectionEnd = 2;
			Dispatcher.UIThread.RunJobs();

			var flyout = (Flyout)styleButton.Flyout;
			flyout.ShowAt(styleButton);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			// Options: [0]=Default(no style), [1]=Strong, [2]=Subtle Emphasis.
			picker.OptionsList.SelectedIndex = 1; // "Strong"
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.TextEdits, Is.Empty, "styles stage through the rich-text seam");
			Assert.That(context.RichTextEdits, Has.Count.EqualTo(1));
			var rich = context.RichTextEdits[0].Value;
			Assert.That(rich.PlainText, Is.EqualTo("dog"), "styling never changes the plain text");
			var styledText = string.Concat(rich.Runs.Where(r => r.NamedStyle == "Strong").Select(r => r.Text));
			Assert.That(styledText, Is.EqualTo("do"), "exactly the selected span carries the new style");
			Assert.That(rich.Runs.First(r => r.Text == "g").NamedStyle, Is.EqualTo("Emphasis"),
				"the unselected tail keeps its own style");
			Assert.That(rich.RichXml, Is.Null,
				"the restyled value drops RichXml so ToTsString re-emits the new style via run-replay");
		}

		// Phase 3 test (b, clear): the leading "Default (no style)" entry clears the style over the span.
		[AvaloniaTest]
		public void StylePicker_DefaultEntry_ClearsTheStyleOverTheSelectedSpan()
		{
			var (control, context, _) = ShowStyleable(StyleableField("Strong"));
			var box = Find<TextBox>(control, "BibEditor.qaa-x-rich");

			box.SelectionStart = 2; // select the styled "g"
			box.SelectionEnd = 3;
			Dispatcher.UIThread.RunJobs();

			var styleButton = Find<Button>(control, "BibEditor.qaa-x-rich.Style");
			var flyout = (Flyout)styleButton.Flyout;
			flyout.ShowAt(styleButton);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			picker.OptionsList.SelectedIndex = 0; // "Default (no style)" -> clears
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits, Has.Count.EqualTo(1));
			var rich = context.RichTextEdits[0].Value;
			Assert.That(rich.Runs.Any(r => !string.IsNullOrEmpty(r.NamedStyle)), Is.False,
				"the Default entry cleared the named style over the selection");
		}

		// Phase 3 test (b, no-op): with no selection the picker commit stages nothing.
		[AvaloniaTest]
		public void StylePicker_WithNoSelection_StagesNothing()
		{
			var (control, context, _) = ShowStyleable(StyleableField("Strong"));
			var box = Find<TextBox>(control, "BibEditor.qaa-x-rich");
			box.SelectionStart = 1;
			box.SelectionEnd = 1;
			box.CaretIndex = 1;
			Dispatcher.UIThread.RunJobs();

			var styleButton = Find<Button>(control, "BibEditor.qaa-x-rich.Style");
			var flyout = (Flyout)styleButton.Flyout;
			flyout.ShowAt(styleButton);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			picker.OptionsList.SelectedIndex = 1;
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits, Is.Empty, "a collapsed caret has no span to style");
		}

		// Phase 3 test (b, gating): a row with no available styles exposes NO style affordance.
		[AvaloniaTest]
		public void StyleAffordance_Absent_WhenNoAvailableStyles()
		{
			var (control, _, _) = ShowStyleable(StyleableField(/* none */));
			Assert.That(Find<Button>(control, "BibEditor.qaa-x-rich.Style"), Is.Null,
				"a field with no available character styles shows no style picker");
		}

		// Phase 3 test (b, gating): a lossy / read-only value exposes NO style affordance (the whole
		// editable block is gated off), so styling can never be attempted.
		[AvaloniaTest]
		public void StyleAffordance_Absent_OnLossyReadOnlyValue()
		{
			var field = new LexicalEditRegionField("LexEntry/x/#0", "Bibliography", "Bibliography", null,
				RegionFieldKind.Text, EditorClassification.Known, "BibEditor", null, SurfaceRouting.Inherit,
				new List<RegionWsValue>
				{
					new RegionWsValue("anal", "coloured", wsTag: "qaa-x-rich",
						richText: new RegionRichTextValue("coloured",
							new[] { new RegionTextRun("coloured", "qaa-x-rich") },
							richXml: "<Str/>", requiresRichEditor: true, lossyProperties: true))
				},
				null, null, isEditable: true, indent: 0)
			{
				AvailableNamedStyles = new[] { "Strong" }
			};
			var (control, context, _) = ShowStyleable(field);
			var box = Find<TextBox>(control, "BibEditor.qaa-x-rich");
			Assert.That(box.IsReadOnly, Is.True, "a lossy value is read-only");
			Assert.That(Find<Button>(control, "BibEditor.qaa-x-rich.Style"), Is.Null,
				"a lossy/read-only value exposes no style affordance");
			Assert.That(context.RichTextEdits, Is.Empty);
		}

		// Phase 3 test (b, automation/accessibility): the affordance carries a stable automation id and
		// accessible name.
		[AvaloniaTest]
		public void StyleAffordance_HasAutomationIdAndAccessibleName()
		{
			var (control, _, _) = ShowStyleable(StyleableField("Strong"));
			var styleButton = Find<Button>(control, "BibEditor.qaa-x-rich.Style");
			Assert.That(styleButton, Is.Not.Null);
			Assert.That(AutomationProperties.GetName(styleButton), Is.EqualTo(FwAvaloniaStrings.CharacterStyle));
		}
	}

	/// <summary>
	/// Phase 4: the per-run writing-system retag picker over a TextBox selection. An editable, non-lossy
	/// row carrying the project's available writing systems exposes a "Writing System" affordance whose
	/// FwOptionPicker commit retags the covered runs through TrySetRichText (same seam as Ctrl+B/I/U and
	/// the style picker), leaving the rest of the value untouched. No clear entry: a run always carries a
	/// writing system.
	/// </summary>
	[TestFixture]
	public class RegionWritingSystemPickerTests
	{
		// A field carrying a rich value plus the project's available writing systems, so the per-WS retag
		// picker affordance is built. The lane's own ws is "qaa-x-rich".
		private static LexicalEditRegionField RetaggableField(params (string Tag, string Name)[] systems)
		{
			var field = new LexicalEditRegionField("LexEntry/x/#0", "Bibliography", "Bibliography", null,
				RegionFieldKind.Text, EditorClassification.Known, "BibEditor", null, SurfaceRouting.Inherit,
				new List<RegionWsValue>
				{
					new RegionWsValue("anal", "dog", wsTag: "qaa-x-rich",
						richText: RegionRichTextEditAlgorithms.FromRuns("dog",
							new[]
							{
								new RegionTextRun("do", "qaa-x-rich"),
								new RegionTextRun("g", "qaa-x-other")
							}))
				},
				null, null, isEditable: true, indent: 0)
			{
				AvailableWritingSystems = systems
					.Select(s => new RegionWritingSystemOption(s.Tag, s.Name)).ToList()
			};
			return field;
		}

		private static (FwMultiWsTextField control, FakeRegionEditContext context, Window window)
			ShowRetaggable(params (string Tag, string Name)[] systems)
		{
			var field = RetaggableField(systems);
			var context = new FakeRegionEditContext();
			var control = new FwMultiWsTextField(field, "BibEditor", context, null);
			var window = new Window { Content = control, Width = 480, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (control, context, window);
		}

		private static T Find<T>(Control root, string automationId) where T : Control
			=> root.GetVisualDescendants().OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == automationId);

		[AvaloniaTest]
		public void WritingSystemPicker_PickingAWs_RetagsItOverTheSelectedSpan()
		{
			var (control, context, _) = ShowRetaggable(("fr", "French"), ("de", "German"));
			var box = Find<TextBox>(control, "BibEditor.qaa-x-rich");
			Assert.That(box, Is.Not.Null);
			Assert.That(context.RichTextEdits, Is.Empty, "construction must not stage");

			var wsButton = Find<Button>(control, "BibEditor.qaa-x-rich.WritingSystem");
			Assert.That(wsButton, Is.Not.Null, "an editable retaggable row exposes the ws affordance");

			box.SelectionStart = 0; // select "do"
			box.SelectionEnd = 2;
			Dispatcher.UIThread.RunJobs();

			var flyout = (Flyout)wsButton.Flyout;
			flyout.ShowAt(wsButton);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			// Options: [0]=French(fr), [1]=German(de).
			picker.OptionsList.SelectedIndex = 1; // German -> de
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.TextEdits, Is.Empty, "ws retag stages through the rich-text seam");
			Assert.That(context.RichTextEdits, Has.Count.EqualTo(1));
			var rich = context.RichTextEdits[0].Value;
			Assert.That(rich.PlainText, Is.EqualTo("dog"), "retag never changes the plain text");
			var retagged = string.Concat(rich.Runs.Where(r => r.WritingSystemTag == "de").Select(r => r.Text));
			Assert.That(retagged, Is.EqualTo("do"), "exactly the selected span carries the new ws");
			Assert.That(rich.Runs.First(r => r.Text == "g").WritingSystemTag, Is.EqualTo("qaa-x-other"),
				"the unselected tail keeps its own ws");
			Assert.That(rich.RichXml, Is.Null,
				"the retagged value drops RichXml so ToTsString re-emits the new ws via run-replay");
		}

		[AvaloniaTest]
		public void WritingSystemPicker_WithNoSelection_StagesNothing()
		{
			var (control, context, _) = ShowRetaggable(("fr", "French"));
			var box = Find<TextBox>(control, "BibEditor.qaa-x-rich");
			box.SelectionStart = 1;
			box.SelectionEnd = 1;
			box.CaretIndex = 1;
			Dispatcher.UIThread.RunJobs();

			var wsButton = Find<Button>(control, "BibEditor.qaa-x-rich.WritingSystem");
			var flyout = (Flyout)wsButton.Flyout;
			flyout.ShowAt(wsButton);
			Dispatcher.UIThread.RunJobs();
			var picker = (FwOptionPicker)flyout.Content;
			picker.OptionsList.SelectedIndex = 0;
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.RichTextEdits, Is.Empty, "a collapsed caret has no span to retag");
		}

		[AvaloniaTest]
		public void WritingSystemAffordance_Absent_WhenNoAvailableWritingSystems()
		{
			var (control, _, _) = ShowRetaggable(/* none */);
			Assert.That(Find<Button>(control, "BibEditor.qaa-x-rich.WritingSystem"), Is.Null,
				"a field with no available writing systems shows no ws picker");
		}

		[AvaloniaTest]
		public void WritingSystemAffordance_HasAutomationIdAndAccessibleName()
		{
			var (control, _, _) = ShowRetaggable(("fr", "French"));
			var wsButton = Find<Button>(control, "BibEditor.qaa-x-rich.WritingSystem");
			Assert.That(wsButton, Is.Not.Null);
			Assert.That(AutomationProperties.GetName(wsButton), Is.EqualTo(FwAvaloniaStrings.WritingSystem));
		}
	}

	/// <summary>Task 3.14: the framework-neutral record-key payload round-trips and rejects garbage.</summary>
	[TestFixture]
	public class FwRecordKeyPayloadTests
	{
		[Test]
		public void Serialize_TryParse_RoundTrips()
		{
			var guid = Guid.NewGuid();
			var serialized = new FwRecordKeyPayload(guid).Serialize();
			Assert.That(serialized, Does.StartWith("fwrecord:"));
			Assert.That(FwRecordKeyPayload.TryParse(serialized, out var parsed), Is.True);
			Assert.That(parsed.ObjectGuid, Is.EqualTo(guid));
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("fwrecord:")]
		[TestCase("fwrecord:not-a-guid")]
		[TestCase("fwrecord:00000000-0000-0000-0000-000000000000")]
		[TestCase("record:6f9619ff-8b86-d011-b42d-00cf4fc964ff")]
		public void TryParse_RejectsNonKeys(string input)
		{
			Assert.That(FwRecordKeyPayload.TryParse(input, out _), Is.False);
		}
	}

	/// <summary>
	/// GEAR = CONFIGURE (the B7 rework): a chooser or reference-vector row whose supporting list
	/// resolved a list-editor target (a goto <see cref="RegionChooserLink"/>) draws the gear, and
	/// clicking it DIRECTLY raises the host's <see cref="RegionLinkRequest"/> — no flyout, no
	/// context menu. Option flyouts (single-select chooser click, vector "+") are OPTIONS ONLY:
	/// they contain zero link items. Rows without a resolvable list editor draw no gear; text
	/// rows NEVER draw one (the Lexeme Form slice menu reverted to right-click only).
	/// </summary>
	[TestFixture]
	public class RegionConfigureGearTests
	{
		private static LexicalEditRegionField LinkedChooserField() => new LexicalEditRegionField(
			"MoForm/x/#0", "Morph Type", "MorphType", null,
			RegionFieldKind.Chooser, EditorClassification.Known, "MorphTypeChooser", null,
			SurfaceRouting.Inherit, null,
			new List<RegionChoiceOption> { new RegionChoiceOption("g1", "stem") }, "g1",
			chooserLinks: new List<RegionChooserLink>
			{
				new RegionChooserLink("Edit the Morpheme Types list", "morphTypeEdit"),
				new RegionChooserLink("Edit something else", "otherEdit") // first goto wins
			});

		[AvaloniaTest]
		public void ChooserGear_Click_DispatchesTheFirstResolvedJump_NoFlyoutNoMenu()
		{
			var field = LinkedChooserField();
			var requests = new List<RegionLinkRequest>();
			var context = new FakeRegionEditContext();
			var chooser = new FwChooserField(field, "MorphTypeChooser", context, requests.Add);
			var window = new Window { Content = chooser, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var gear = chooser.GetVisualDescendants().OfType<Button>()
				.Single(b => AutomationProperties.GetAutomationId(b) == "MorphTypeChooser.Settings");
			Assert.That(gear.Flyout, Is.Null, "the gear carries no flyout");
			Assert.That(gear.ContextFlyout, Is.Null, "the gear carries no context menu");
			Assert.That(AutomationProperties.GetName(gear), Does.Contain("Morph Type"));

			gear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(requests, Has.Count.EqualTo(1), "the gear click raises RegionLinkRequest directly");
			Assert.That(requests[0].Field, Is.SameAs(field));
			Assert.That(requests[0].Link.Tool, Is.EqualTo("morphTypeEdit"), "the FIRST goto link wins");
			Assert.That(((Flyout)chooser.Flyout).IsOpen, Is.False, "no flyout opened from the gear");
			Assert.That(context.OptionEdits, Is.Empty, "a configure jump is not an edit");
		}

		[AvaloniaTest]
		public void ChooserFlyout_IsOptionsOnly_ZeroLinkItems()
		{
			var chooser = new FwChooserField(LinkedChooserField(), "MorphTypeChooser",
				new FakeRegionEditContext(), request => { });

			var picker = (FwOptionPicker)((Flyout)chooser.Flyout).Content;
			Assert.That(picker.GetVisualDescendants().OfType<AutoCompleteBox>(), Is.Empty,
				"the options flyout is the shared inline filter+list picker — no nested selector control");
			Assert.That(picker.GetVisualDescendants().Contains(picker.OptionsList), Is.True,
				"the options render inline in the one compact filterable picker — ZERO link items");
			Assert.That(AutomationProperties.GetAutomationId(picker.FilterBox), Is.EqualTo("MorphTypeChooser.Search"));
			Assert.That(AutomationProperties.GetAutomationId(picker.OptionsList), Is.EqualTo("MorphTypeChooser.Options"));
		}

		[AvaloniaTest]
		public void VectorGear_Click_DispatchesTheJumpDirectly_AndTheAddFlyoutHasNoLinks()
		{
			var field = new LexicalEditRegionField("LexEntry/x/#1", "Publish Entry In", "PublishIn", null,
				RegionFieldKind.ReferenceVector, EditorClassification.Known, "PublishIn", null,
				SurfaceRouting.Inherit, null,
				new List<RegionChoiceOption> { new RegionChoiceOption("p1", "Main Dictionary") },
				null, isEditable: true, indent: 0,
				items: new List<RegionChoiceOption>(),
				chooserLinks: new List<RegionChooserLink>
				{
					new RegionChooserLink("Edit the Publications list", "publicationsEdit")
				});
			var requests = new List<RegionLinkRequest>();
			var vector = new FwReferenceVectorField(field, "PublishIn", new FakeRegionEditContext(),
				null, requests.Add);
			var window = new Window { Content = vector, Width = 480, Height = 240 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var addButton = vector.GetVisualDescendants().OfType<Button>()
				.Single(b => AutomationProperties.GetAutomationId(b) == "PublishIn.Add");
			Assert.That(((Flyout)addButton.Flyout).Content, Is.TypeOf<FwOptionPicker>(),
				"the + opens the one compact picker — options only, no link lane");

			var gear = vector.GetVisualDescendants().OfType<Button>()
				.Single(b => AutomationProperties.GetAutomationId(b) == "PublishIn.Settings");
			Assert.That(gear.Flyout, Is.Null, "the vector gear has no flyout either");

			gear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Link.Tool, Is.EqualTo("publicationsEdit"));
			Assert.That(requests[0].Link.TargetGuid, Is.Null,
				"no target — legacy m_guidLink stays Guid.Empty for the Publications link");
		}

		[AvaloniaTest]
		public void RowsWithoutAResolvableListEditor_HaveNoGear()
		{
			// No links: no gear, even with a host callback.
			var noLinks = new LexicalEditRegionField("MoForm/x/#0", "Morph Type", "MorphType", null,
				RegionFieldKind.Chooser, EditorClassification.Known, "PlainChooser", null,
				SurfaceRouting.Inherit, null,
				new List<RegionChoiceOption> { new RegionChoiceOption("g1", "stem") }, "g1");
			var chooser = new FwChooserField(noLinks, "PlainChooser", new FakeRegionEditContext(),
				request => { });
			Assert.That(chooser.HoverAffordances, Is.Empty, "no resolvable list editor, no gear");
			Assert.That(((Flyout)chooser.Flyout).Content, Is.TypeOf<FwOptionPicker>(),
				"the options picker still opens from the value click");

			// Links but no host callback (nothing to dispatch through): no gear either.
			var noCallback = new FwChooserField(LinkedChooserField(), "NoCallbackChooser",
				new FakeRegionEditContext());
			Assert.That(noCallback.HoverAffordances, Is.Empty, "no host bridge, no gear");

			// A vector without links: bars + "+" only — no Settings button at all.
			var vectorField = new LexicalEditRegionField("LexEntry/x/#1", "Publish Entry In",
				"PublishIn", null, RegionFieldKind.ReferenceVector, EditorClassification.Known,
				"PlainVector", null, SurfaceRouting.Inherit, null,
				new List<RegionChoiceOption> { new RegionChoiceOption("p1", "Main Dictionary") },
				null, isEditable: true, indent: 0, items: new List<RegionChoiceOption>());
			var vector = new FwReferenceVectorField(vectorField, "PlainVector",
				new FakeRegionEditContext(), null, request => { });
			Assert.That(vector.Children.OfType<Button>()
					.Any(b => AutomationProperties.GetAutomationId(b) == "PlainVector.Settings"),
				Is.False, "no resolvable list editor, no vector gear");
		}

		// REVERT (gears never open context menus): the Lexeme Form text row draws NO gear; its
		// slice menu (menu="mnuDataTree-LexemeForm") stays on right-click only — the label lane in
		// the region view (RegionMenuTests) and the in-string lane below are unchanged.
		[AvaloniaTest]
		public void TextRows_NeverDrawAGear_TheSliceMenuStaysOnRightClickOnly()
		{
			var field = new LexicalEditRegionField("MoStemAllomorph/AsLexemeFormBasic/#0", "Lexeme Form",
				"Form", null, RegionFieldKind.Text, EditorClassification.Known, "LexemeFormRow", null,
				SurfaceRouting.Inherit,
				new List<RegionWsValue>
				{
					new RegionWsValue("vern", "casa"),
					new RegionWsValue("ipa", "kasa")
				},
				null, null, isEditable: true, indent: 0, menuId: "mnuDataTree-LexemeForm");
			var requests = new List<RegionMenuRequest>();
			var text = new FwMultiWsTextField(field, "LexemeFormRow", new FakeRegionEditContext(),
				null, requests.Add);
			var window = new Window { Content = text, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(text.GetVisualDescendants().OfType<Button>()
					.Any(b => AutomationProperties.GetAutomationId(b) == "LexemeFormRow.Settings"),
				Is.False, "the Lexeme Form slice-menu gear is reverted — text rows draw no gear");
			Assert.That(((IHoverAffordanceProvider)text).HoverAffordances, Is.Empty,
				"text rows expose no hover-revealed chrome");
			Assert.That(requests, Is.Empty, "nothing dispatched a menu request on construction");
		}
	}

	/// <summary>Task 6.11: the product-facing strings resolve through the Avalonia localization accessor.</summary>
	[TestFixture]
	public class FwAvaloniaStringsTests
	{
		[Test]
		public void AllStrings_ResolveFromLocalizationAccessor()
		{
			Assert.That(FwAvaloniaStrings.NoEntrySelected, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaStrings.EntryTypeUnsupported, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaStrings.UnsupportedEditor, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaStrings.Save, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaStrings.Cancel, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaStrings.UndoEditEntry, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaStrings.RedoEditEntry, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaStrings.LexemeFormRequired, Is.Not.Null.And.Not.Empty);
		}
	}
}
