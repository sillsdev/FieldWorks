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
		public void ChooserChange_StagesOptionKeyThroughTheEditContext()
		{
			var (view, context, _) = ShowEditable();
			var chooser = Find<FwChooserField>(view, "MorphTypeChooser");
			Assert.That(chooser, Is.Not.Null, "the chooser renders as the owned flyout field");
			Assert.That(chooser.ValueText, Is.EqualTo("stem"), "shows the current selection");

			var options = (ListBox)((Flyout)chooser.Flyout).Content;
			options.SelectedItem = "suffix";
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

			var flyoutPanel = (StackPanel)((Flyout)addButton.Flyout).Content;
			var searchBox = flyoutPanel.Children.OfType<TextBox>().Single();
			Assert.That(AutomationProperties.GetAutomationId(searchBox), Is.EqualTo("Components.Search"));
			var results = flyoutPanel.Children.OfType<ListBox>().Single();
			Assert.That(results.ItemsSource, Is.Null,
				"nothing is enumerated before the user types — lexicons search, lists enumerate");

			searchBox.Text = "ca";
			Dispatcher.UIThread.RunJobs();
			Assert.That(queries, Does.Contain("ca"), "typing drives the field's search delegate");
			var shown = ((IEnumerable<RegionChoiceOption>)results.ItemsSource).ToList();
			Assert.That(shown.Select(o => o.Key), Is.EqualTo(new[] { "e-casa", "e-cantar" }));

			results.SelectedIndex = 1;
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.ReferenceAdds, Has.Count.EqualTo(1));
			Assert.That(context.ReferenceAdds[0], Is.EqualTo(("ComponentLexemes", "e-cantar")),
				"selecting a search result stages the result's key through TryAddReferenceItem");
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
			var list = (ListBox)((Flyout)addButton.Flyout).Content;

			list.SelectedIndex = 1; // "Pocket"
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.ReferenceAdds, Has.Count.EqualTo(1));
			Assert.That(context.ReferenceAdds[0], Is.EqualTo(("PublishIn", "p2")));
			Assert.That(gestures, Is.EqualTo(1), "a successful add completes the gesture exactly once");

			context.ReferenceGestureResult = false;
			list.SelectedIndex = 0;
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
	/// GAP 1 / B7 — list-editor jump links in the gear flyouts: a chooser or reference-vector row
	/// whose layout carries <c>chooserLink</c> metadata shows the legacy "Edit the … list" link
	/// below the options (the ReallySimpleListChooser LinkLabel); clicking it closes the flyout and
	/// raises the host's <see cref="RegionLinkRequest"/> callback, which dispatches the legacy
	/// mediator FollowLink jump.
	/// GAP 2 — the legacy slice tree-node menu button: a text row with a layout <c>menu=</c>
	/// binding (the Lexeme Form's mnuDataTree-LexemeForm) gets the SAME hover-revealed gear, and
	/// clicking it raises the slice-menu request a label right-click raises.
	/// </summary>
	[TestFixture]
	public class RegionChooserLinkAndSliceMenuGearTests
	{
		private static IReadOnlyList<Button> LinkButtons(Control flyoutContent, string automationId)
			=> ((StackPanel)flyoutContent).Children.OfType<Button>()
				.Where(b => (AutomationProperties.GetAutomationId(b) ?? "")
					.StartsWith(automationId + ".Link.", StringComparison.Ordinal))
				.ToList();

		[AvaloniaTest]
		public void ChooserFlyout_ShowsTheJumpLink_BelowTheOptions_AndClickRaisesTheLinkRequest()
		{
			var field = new LexicalEditRegionField("MoForm/x/#0", "Morph Type", "MorphType", null,
				RegionFieldKind.Chooser, EditorClassification.Known, "MorphTypeChooser", null,
				SurfaceRouting.Inherit, null,
				new List<RegionChoiceOption> { new RegionChoiceOption("g1", "stem") }, "g1",
				chooserLinks: new List<RegionChooserLink>
				{
					new RegionChooserLink("Edit the Publications list", "publicationsEdit")
				});
			var requests = new List<RegionLinkRequest>();
			var context = new FakeRegionEditContext();
			var chooser = new FwChooserField(field, "MorphTypeChooser", context, requests.Add);
			var window = new Window { Content = chooser, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var content = (StackPanel)((Flyout)chooser.Flyout).Content;
			Assert.That(content.Children.OfType<ListBox>().Count(), Is.EqualTo(1),
				"the options list still fronts the flyout");
			Assert.That(content.Children.OfType<Border>().Count(), Is.EqualTo(1),
				"a separator rule divides options from the link lane");
			var links = LinkButtons(content, "MorphTypeChooser");
			Assert.That(links, Has.Count.EqualTo(1));
			Assert.That(AutomationProperties.GetName(links[0]), Is.EqualTo("Edit the Publications list"));

			links[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(requests, Has.Count.EqualTo(1), "the link click raises the host callback");
			Assert.That(requests[0].Field, Is.SameAs(field));
			Assert.That(requests[0].Link.Tool, Is.EqualTo("publicationsEdit"));
			Assert.That(requests[0].Link.Label, Is.EqualTo("Edit the Publications list"));
			Assert.That(context.OptionEdits, Is.Empty, "a jump is not an edit");
		}

		[AvaloniaTest]
		public void VectorFlyout_ShowsTheJumpLink_AndClickRaisesTheLinkRequest()
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
			var content = (StackPanel)((Flyout)addButton.Flyout).Content;
			var links = LinkButtons(content, "PublishIn");
			Assert.That(links, Has.Count.EqualTo(1), "the vector gear/+ flyout carries the jump link too");

			links[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(requests, Has.Count.EqualTo(1));
			Assert.That(requests[0].Link.Tool, Is.EqualTo("publicationsEdit"));
			Assert.That(requests[0].Link.TargetGuid, Is.Null,
				"no target — legacy m_guidLink stays Guid.Empty for the Publications link");
		}

		[AvaloniaTest]
		public void ChooserWithoutLinksOrCallback_KeepsTheBareOptionsList()
		{
			var field = new LexicalEditRegionField("MoForm/x/#0", "Morph Type", "MorphType", null,
				RegionFieldKind.Chooser, EditorClassification.Known, "PlainChooser", null,
				SurfaceRouting.Inherit, null,
				new List<RegionChoiceOption> { new RegionChoiceOption("g1", "stem") }, "g1");
			var chooser = new FwChooserField(field, "PlainChooser", new FakeRegionEditContext(),
				request => { });

			Assert.That(((Flyout)chooser.Flyout).Content, Is.TypeOf<ListBox>(),
				"no links: the flyout content stays the bare options list");
		}

		// GAP 2: the legacy Lexeme Form slice's button is its slice TREE-NODE MENU
		// (MoForm-Detail-AsLexemeForm binds menu="mnuDataTree-LexemeForm": Show in Concordance,
		// Swap with Allomorph, Convert to Affix Process/Allomorph) — NOT a chooser launcher; the
		// morph-type chooser w/ swap gate lives on the child Morph Type row, which already has a
		// gear. So a text row with a menu binding gets the same hover-revealed gear raising the
		// SAME slice-menu request a label right-click raises — data-driven from `menu=`.
		[AvaloniaTest]
		public void TextRowWithSliceMenuBinding_GetsTheHoverGear_AndClickRaisesTheSliceMenuRequest()
		{
			var field = new LexicalEditRegionField("MoStemAllomorph/AsLexemeFormBasic/#0", "Lexeme Form",
				"Form", null, RegionFieldKind.Text, EditorClassification.Known, "LexemeFormRow", null,
				SurfaceRouting.Inherit,
				new List<RegionWsValue>
				{
					new RegionWsValue("vern", "casa"),
					new RegionWsValue("ipa", "kasa") // the gear rides the FIRST row only
				},
				null, null, isEditable: true, indent: 0, menuId: "mnuDataTree-LexemeForm");
			var requests = new List<RegionMenuRequest>();
			var text = new FwMultiWsTextField(field, "LexemeFormRow", new FakeRegionEditContext(),
				null, requests.Add);
			var window = new Window { Content = text, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var gears = text.GetVisualDescendants().OfType<Button>()
				.Where(b => AutomationProperties.GetAutomationId(b) == "LexemeFormRow.Settings")
				.ToList();
			Assert.That(gears, Has.Count.EqualTo(1), "one gear, on the first alternative's row");
			var gear = gears[0];
			Assert.That(((IHoverAffordanceProvider)text).HoverAffordances, Does.Contain(gear),
				"the gear is hover-revealed chrome, like the chooser's");
			Assert.That(gear.Opacity, Is.EqualTo(0d), "starts hidden until row hover");
			Assert.That(AutomationProperties.GetName(gear), Does.Contain("Lexeme Form"));

			gear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(requests, Has.Count.EqualTo(1), "the gear raises the host menu bridge");
			Assert.That(requests[0].Kind, Is.EqualTo(RegionMenuKind.SliceMenu),
				"same lane as a label right-click — the host shows mnuDataTree-LexemeForm");
			Assert.That(requests[0].Field.MenuId, Is.EqualTo("mnuDataTree-LexemeForm"));
		}

		[AvaloniaTest]
		public void TextRowWithoutMenuBinding_OrWithoutTheHostBridge_HasNoGear()
		{
			var noMenu = new LexicalEditRegionField("x/#0", "Citation Form", "CitationForm", null,
				RegionFieldKind.Text, EditorClassification.Known, "NoMenuRow", null, SurfaceRouting.Inherit,
				new List<RegionWsValue> { new RegionWsValue("vern", "casa") }, null, null);
			var withoutMenu = new FwMultiWsTextField(noMenu, "NoMenuRow", new FakeRegionEditContext(),
				null, request => { });
			Assert.That(((IHoverAffordanceProvider)withoutMenu).HoverAffordances, Is.Empty,
				"no menu binding, no gear");

			var withMenu = new LexicalEditRegionField("x/#1", "Lexeme Form", "Form", null,
				RegionFieldKind.Text, EditorClassification.Known, "NoBridgeRow", null, SurfaceRouting.Inherit,
				new List<RegionWsValue> { new RegionWsValue("vern", "casa") }, null, null,
				isEditable: true, indent: 0, menuId: "mnuDataTree-LexemeForm");
			var withoutBridge = new FwMultiWsTextField(withMenu, "NoBridgeRow",
				new FakeRegionEditContext(), null);
			Assert.That(((IHoverAffordanceProvider)withoutBridge).HoverAffordances, Is.Empty,
				"no host menu bridge, no gear (it could do nothing)");
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
