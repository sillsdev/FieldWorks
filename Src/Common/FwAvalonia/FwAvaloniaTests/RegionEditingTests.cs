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
		public readonly List<(string Field, string Key)> OptionEdits = new List<(string, string)>();
		public IReadOnlyList<string> ValidateResult = new List<string>();
		public int CommitCount;
		public int CancelCount;

		public bool IsOpen => TextEdits.Count + OptionEdits.Count > 0 && CommitCount == 0 && CancelCount == 0;

		public bool TrySetText(LexicalEditRegionField field, string ws, string value)
		{
			TextEdits.Add((field.Field, ws, value));
			return true;
		}

		public bool TrySetOption(LexicalEditRegionField field, string optionKey)
		{
			OptionEdits.Add((field.Field, optionKey));
			return true;
		}

		public IReadOnlyList<string> Validate() => ValidateResult;

		public void Commit() => CommitCount++;

		public void Cancel() => CancelCount++;
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

		private static T Find<T>(LexicalEditRegionView view, string automationId) where T : Control
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
		public void ChooserChange_StagesOptionKeyThroughTheEditContext()
		{
			var (view, context, _) = ShowEditable();
			var chooser = Find<FwChooserField>(view, "MorphTypeChooser");
			Assert.That(chooser, Is.Not.Null, "the chooser renders as the owned flyout field");
			Assert.That(chooser.Content, Is.EqualTo("stem"), "shows the current selection");

			var options = (ListBox)((Flyout)chooser.Flyout).Content;
			options.SelectedItem = "suffix";
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.OptionEdits, Has.Count.EqualTo(1));
			Assert.That(context.OptionEdits[0], Is.EqualTo(("MorphType", "g2")));
			Assert.That(chooser.SelectedKey, Is.EqualTo("g2"), "the staged selection becomes current");
			Assert.That(chooser.Content, Is.EqualTo("suffix"));
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

			var options = (ListBox)((Flyout)chooser.Flyout).Content;
			options.SelectedIndex = 1;
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

	/// <summary>Task 6.11: the product-facing strings resolve from embedded .resx resources.</summary>
	[TestFixture]
	public class FwAvaloniaStringsTests
	{
		[Test]
		public void AllStrings_ResolveFromResources()
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
