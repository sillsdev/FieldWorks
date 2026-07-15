// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the PNG harness
using FwAvaloniaDialogsTests;        // DialogLayoutAssert — the shared geometry tripwire

namespace FwAvaloniaTests
{
	/// <summary>
	/// The ONE compact filterable option picker (FwOptionPicker) behind every Avalonia select-from-
	/// list surface: an AutoCompleteBox-based selector whose embedded search box auto-focuses on
	/// open, whose popup list stays virtualized/capped at the density token height, and whose item
	/// spacing stays pinned to the compact legacy values (never the Fluent defaults) while preserving
	/// possibility-list hierarchy by Depth indent. Static options filter by contains; search-backed
	/// pickers populate through the host delegate; Down/Up/Enter/Escape follow the stock selector
	/// path while the wrapper preserves FieldWorks commit/dismiss semantics.
	/// </summary>
	[TestFixture]
	public class FwOptionPickerTests
	{
		private static IReadOnlyList<RegionChoiceOption> Tree() => new List<RegionChoiceOption>
		{
			new RegionChoiceOption("u", "Universe", 0),
			new RegionChoiceOption("u-sky", "Sky", 1),
			new RegionChoiceOption("u-weather", "Weather", 1),
			new RegionChoiceOption("p", "Person", 0)
		};

		private static (FwOptionPicker picker, Window window, List<RegionChoiceOption> committed,
			int dismissed) ShowStatic()
		{
			var picker = new FwOptionPicker(Tree(), null, "Domains");
			var committed = new List<RegionChoiceOption>();
			picker.OptionCommitted += committed.Add;
			var dismissed = 0;
			picker.Dismissed += (s, e) => dismissed++;
			var window = new Window { Content = picker, Width = 400, Height = 420 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			return (picker, window, committed, dismissed);
		}

		private static (FwOptionPicker picker, Window window, List<RegionChoiceOption> committed) ShowStaticWithUnavailable(
			params string[] unavailableKeys)
		{
			var picker = new FwOptionPicker(Tree(), null, "Domains", unavailableKeys);
			var committed = new List<RegionChoiceOption>();
			picker.OptionCommitted += committed.Add;
			var window = new Window { Content = picker, Width = 400, Height = 420 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			return (picker, window, committed);
		}

		private static void RaiseKey(Control target, Key key, bool handled = false)
		{
			target.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = key,
				Source = target,
				Handled = handled
			});
			Dispatcher.UIThread.RunJobs();
		}

		private static IReadOnlyList<RegionChoiceOption> Items(FwOptionPicker picker)
			=> picker.CurrentItems;

		[AvaloniaTest]
		public void OptionsRenderInline_InsideThePickerItself_NoSecondFloatingDropdown()
		{
			// The thick grey border + flaky arrow keys came from AutoCompleteBox opening a SECOND
			// popup (PART_SuggestionsContainer) nested inside the host flyout. The picker must show
			// its filter box AND its options list inside its OWN visual tree — the host flyout is
			// the only popup — so there is no separate grey-chromed dropdown surface to fight.
			var (picker, _, _, _) = ShowStatic();
			picker.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			Assert.That(picker.GetVisualDescendants().OfType<AutoCompleteBox>(), Is.Empty,
				"the picker no longer hosts an AutoCompleteBox (and its nested grey dropdown popup)");
			Assert.That(picker.GetVisualDescendants().Contains(picker.FilterBox), Is.True,
				"the filter box is part of the picker's own (single-popup) visual tree");
			Assert.That(picker.GetVisualDescendants().Contains(picker.OptionsList), Is.True,
				"the options list renders INLINE under the filter box — not in a second floating popup");
		}

		[AvaloniaTest]
		public void OpensWithFilterFocused_Watermarked_AndAllOptionsListed()
		{
			var (picker, window, _, _) = ShowStatic();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			DialogSnapshot.Capture(window, "FwOptionPicker-01-initial");
			DialogLayoutAssert.AssertNoCrowding(picker);

			Assert.That(picker.FilterBox.IsFocused, Is.True,
				"the filter box auto-focuses when the picker attaches (flyout open)");
			Assert.That(picker.FilterBox.Watermark, Is.EqualTo(FwAvaloniaStrings.SearchPrompt),
				"the existing search prompt watermarks the filter");
			Assert.That(AutomationProperties.GetAutomationId(picker.FilterBox), Is.EqualTo("Domains.Search"));
			Assert.That(AutomationProperties.GetAutomationId(picker.OptionsList), Is.EqualTo("Domains.Options"));
			Assert.That(Items(picker).Select(o => o.Key), Is.EqualTo(new[] { "u", "u-sky", "u-weather", "p" }),
				"static options enumerate up front, in list order");
			Assert.That(picker.OptionsList.SelectedIndex, Is.EqualTo(0), "the first match is highlighted");
		}

		[AvaloniaTest]
		public void Typing_FiltersLive_CaseInsensitiveContains()
		{
			var (picker, window, _, _) = ShowStatic();

			window.KeyTextInput("EaTh"); // headless text input into the focused filter box
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			DialogSnapshot.Capture(window, "FwOptionPicker-02-filtered-search");
			DialogLayoutAssert.AssertNoCrowding(picker);

			Assert.That(picker.FilterBox.Text, Is.EqualTo("EaTh"));
			Assert.That(Items(picker).Select(o => o.Name), Is.EqualTo(new[] { "Weather" }),
				"a case-insensitive CONTAINS filter, not prefix-only");
			Assert.That(picker.OptionsList.SelectedIndex, Is.EqualTo(0),
				"the first (only) match is highlighted, ready for Enter");

			picker.FilterBox.Text = string.Empty;
			Dispatcher.UIThread.RunJobs();
			Assert.That(Items(picker), Has.Count.EqualTo(4), "clearing the filter restores all options");
		}

		[AvaloniaTest]
		public void Enter_CommitsTheHighlightedOption_DefaultingToTheFirstMatch()
		{
			var (picker, window, committed, _) = ShowStatic();

			window.KeyTextInput("sky");
			RaiseKey(picker.FilterBox, Key.Enter);

			Assert.That(committed, Has.Count.EqualTo(1), "Enter commits");
			Assert.That(committed[0].Key, Is.EqualTo("u-sky"), "the first match was highlighted by default");
		}

		[AvaloniaTest]
		public void DownAndUp_MoveTheHighlight_AndEnterCommitsIt()
		{
			var (picker, window, committed, _) = ShowStatic();

			RaiseKey(picker.FilterBox, Key.Down);
			RaiseKey(picker.FilterBox, Key.Down);
			Assert.That(picker.OptionsList.SelectedIndex, Is.EqualTo(2), "Down moves the highlight");
			RaiseKey(picker.FilterBox, Key.Up);
			Assert.That(picker.OptionsList.SelectedIndex, Is.EqualTo(1), "Up moves it back");

			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture(window, "FwOptionPicker-05-selected");
			DialogLayoutAssert.AssertNoCrowding(picker);

			RaiseKey(picker.FilterBox, Key.Enter);
			Assert.That(committed.Single().Key, Is.EqualTo("u-sky"), "Enter commits the highlighted option");
		}

		[AvaloniaTest]
		public void RealHeadlessKeyPresses_OnTheFocusedFilterBox_MoveAndCommitSelections()
		{
			var (picker, window, committed, _) = ShowStatic();

			window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
			Assert.That(picker.OptionsList.SelectedIndex, Is.EqualTo(1),
				"a real Down keypress on the focused filter box should move the selector highlight");

			window.KeyTextInput("s");
			Assert.That(Items(picker).Select(o => o.Key), Is.EqualTo(new[] { "u", "u-sky", "p" }),
				"typing through the headless input path should update the filtered result set");

			window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
			Assert.That(picker.OptionsList.SelectedIndex, Is.EqualTo(1),
				"Down should still move through the filtered result set on the real key path");

			window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
			Assert.That(committed.Select(o => o.Key), Is.EqualTo(new[] { "u-sky" }),
				"Enter should commit the highlighted filtered option on the real headless key path");
		}

		[AvaloniaTest]
		public void HandledArrowKeys_FromTheFilterBox_StillMoveTheFilteredSelection()
		{
			var (picker, window, committed, _) = ShowStatic();

			window.KeyTextInput("s");
			Assert.That(Items(picker).Select(o => o.Key), Is.EqualTo(new[] { "u", "u-sky", "p" }),
				"filtering leaves the matching subset in list order");

			RaiseKey(picker.FilterBox, Key.Down, handled: true);
			Assert.That(picker.OptionsList.SelectedIndex, Is.EqualTo(1),
				"Down should still advance even when the TextBox already handled the key");

			RaiseKey(picker.FilterBox, Key.Enter, handled: true);
			Assert.That(committed.Select(o => o.Key), Is.EqualTo(new[] { "u-sky" }),
				"Enter should commit the highlighted filtered option through the same handled event path");
		}

		[AvaloniaTest]
		public void Escape_RaisesDismissed_WithoutCommitting()
		{
			var picker = new FwOptionPicker(Tree(), null, "Domains");
			var committed = new List<RegionChoiceOption>();
			picker.OptionCommitted += committed.Add;
			var dismissed = 0;
			picker.Dismissed += (s, e) => dismissed++;
			var window = new Window { Content = picker, Width = 400, Height = 420 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			RaiseKey(picker.FilterBox, Key.Escape);

			Assert.That(dismissed, Is.EqualTo(1), "Escape dismisses (the host hides its flyout)");
			Assert.That(committed, Is.Empty, "Escape never commits");
		}

		[AvaloniaTest]
		public void HierarchyIndent_IsPreserved_ThroughTheDepthMargin()
		{
			var (picker, window, _, _) = ShowStatic();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			DialogSnapshot.Capture(window, "FwOptionPicker-03-depth-indented");
			DialogLayoutAssert.AssertNoCrowding(picker);

			var texts = picker.OptionsList.GetVisualDescendants().OfType<TextBlock>()
				.Where(t => Tree().Any(o => o.Name == t.Text))
				.ToDictionary(t => t.Text, t => t.Margin.Left);
			Assert.That(texts["Universe"], Is.EqualTo(0), "top-level options sit flush");
			Assert.That(texts["Sky"], Is.EqualTo(14), "depth-1 options indent one level (B8 hierarchy)");
			Assert.That(texts["Weather"], Is.EqualTo(14));
		}

		[AvaloniaTest]
		public void ItemDensity_IsPinnedCompact_NotTheFluentDefaults()
		{
			var (picker, window, _, _) = ShowStatic();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			var container = picker.OptionsList.ContainerFromIndex(0) as ListBoxItem;
			Assert.That(container, Is.Not.Null, "the first option's container is realized");
			Assert.That(container.Padding, Is.EqualTo(FwAvaloniaDensity.OptionItemPadding),
				"item padding mirrors the legacy WinForms menu spacing (~6,2), not Fluent");
			Assert.That(container.MinHeight, Is.EqualTo(0d), "no Fluent minimum row height");
		}

		[AvaloniaTest]
		public void List_IsVirtualized_AndHeightCapped_SoOffScreenContentScrolls()
		{
			var (picker, window, _, _) = ShowStatic();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			Assert.That(picker.OptionsList.MaxHeight, Is.EqualTo(FwAvaloniaDensity.OptionListMaxHeight),
				"the list caps at the density token (~320) so long lists scroll");
			Assert.That(picker.OptionsList.GetVisualDescendants().OfType<VirtualizingStackPanel>().Any(),
				Is.True, "the items panel is a VirtualizingStackPanel — the ~1800-node semantic" +
				" domain list must not realize every row");
		}

		[AvaloniaTest]
		public void SearchBackedPicker_EnumeratesNothingUpFront_AndForwardsTheQuery()
		{
			var queries = new List<string>();
			var lexicon = new List<RegionChoiceOption>
			{
				new RegionChoiceOption("e-casa", "casa"),
				new RegionChoiceOption("e-cantar", "cantar")
			};
			var picker = new FwOptionPicker(null,
				q =>
				{
					queries.Add(q);
					return lexicon.Where(o => o.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase)).ToList();
				},
				"Components");
			var committed = new List<RegionChoiceOption>();
			picker.OptionCommitted += committed.Add;
			var window = new Window { Content = picker, Width = 400, Height = 420 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(picker.CurrentItems, Is.Empty,
				"lexicons search, lists enumerate: nothing materializes before the user types");

			window.KeyTextInput("ca");
			Assert.That(queries, Does.Contain("ca"), "typing forwards the query to the host search");
			Assert.That(Items(picker).Select(o => o.Key), Is.EqualTo(new[] { "e-casa", "e-cantar" }));

			RaiseKey(picker.FilterBox, Key.Enter);
			Assert.That(committed.Single().Key, Is.EqualTo("e-casa"),
				"Enter commits the first search result by default");
		}

		[AvaloniaTest]
		public void UnavailableOptions_AreGreyedOut_AndSkippedByDefaultSelectionAndCommit()
		{
			var (picker, window, committed) = ShowStaticWithUnavailable("u", "u-sky");
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			DialogSnapshot.Capture(window, "FwOptionPicker-04-unavailable-grayed");
			DialogLayoutAssert.AssertNoCrowding(picker);

			Assert.That(picker.OptionsList.SelectedIndex, Is.EqualTo(2),
				"default selection skips options that are already selected elsewhere");

			var texts = picker.OptionsList.GetVisualDescendants().OfType<TextBlock>()
				.Where(t => Tree().Any(o => o.Name == t.Text))
				.ToDictionary(t => t.Text, t => t);
			Assert.That(texts["Universe"].Opacity, Is.LessThan(1.0), "already-selected options are visually muted");
			Assert.That(texts["Sky"].Opacity, Is.LessThan(1.0));
			Assert.That(texts["Weather"].Opacity, Is.EqualTo(1.0).Within(0.01));

			RaiseKey(picker.FilterBox, Key.Up);
			Assert.That(picker.OptionsList.SelectedIndex, Is.EqualTo(2),
				"keyboard navigation does not land on unavailable choices");

			RaiseKey(picker.FilterBox, Key.Enter);
			Assert.That(committed.Select(o => o.Key), Is.EqualTo(new[] { "u-weather" }),
				"commit ignores unavailable options and uses the first enabled choice");
		}

		[AvaloniaTest]
		public void PointerRelease_OnTheListScrollbar_DoesNotCommit()
		{
			// Enough options to overflow the capped list, so the scrollbar is a real part of
			// the gesture surface.
			var options = Enumerable.Range(0, 60)
				.Select(i => new RegionChoiceOption("k" + i, "Option " + i))
				.ToList();
			var picker = new FwOptionPicker(options, null, "Domains");
			var committed = new List<RegionChoiceOption>();
			picker.OptionCommitted += committed.Add;
			var window = new Window { Content = picker, Width = 400, Height = 420 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			var scrollBar = picker.OptionsList.GetVisualDescendants().OfType<ScrollBar>()
				.FirstOrDefault(b => b.Orientation == Avalonia.Layout.Orientation.Vertical);
			Assert.That(scrollBar, Is.Not.Null, "the list template carries a vertical scrollbar");

			RaiseRelease(scrollBar, window);

			Assert.That(committed, Is.Empty,
				"a release landing on the scrollbar (not an option row) must not commit the highlight");
		}

		[AvaloniaTest]
		public void PointerRelease_OnAnOptionRow_StillCommits()
		{
			var (picker, window, committed, _) = ShowStatic();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			picker.OptionsList.SelectedIndex = 2; // the press selects; the release completes
			var container = picker.OptionsList.ContainerFromIndex(2);
			Assert.That(container, Is.Not.Null, "the option's container is realized");

			RaiseRelease(container, window);

			Assert.That(committed.Select(o => o.Key), Is.EqualTo(new[] { "u-weather" }),
				"a release that lands on an option row commits the highlighted option");
		}

		// ===== Multi-select mode (the legacy multi-check chooser) =====

		private static (FwOptionPicker picker, Window window, List<IReadOnlyList<RegionChoiceOption>> batches)
			ShowMultiSelect(IReadOnlyList<RegionChoiceOption> options = null,
				Func<string, IReadOnlyList<RegionChoiceOption>> search = null)
		{
			var picker = new FwOptionPicker(options ?? Tree(), search, "Domains", null, multiSelect: true);
			var batches = new List<IReadOnlyList<RegionChoiceOption>>();
			picker.OptionsCommitted += batches.Add;
			var window = new Window { Content = picker, Width = 400, Height = 420 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			return (picker, window, batches);
		}

		private static Button AddButton(FwOptionPicker picker)
			=> picker.GetVisualDescendants().OfType<Button>()
				.Single(b => AutomationProperties.GetAutomationId(b) == "Domains.AddSelected");

		[AvaloniaTest]
		public void MultiSelect_RowCommit_ChecksRatherThanCommitting_AndAddCommitsTheWholeSet()
		{
			var (picker, window, batches) = ShowMultiSelect();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			Assert.That(picker.IsMultiSelect, Is.True);
			var add = AddButton(picker);
			Assert.That(add.IsEnabled, Is.False, "nothing checked yet: Add is disabled");

			// Check two rows: Enter on the highlight toggles, then highlight another and Enter.
			picker.OptionsList.SelectedIndex = 1; // "Sky"
			RaiseKey(picker.FilterBox, Key.Enter);
			Assert.That(batches, Is.Empty, "a row commit in multi-select mode does NOT commit immediately");
			Assert.That(picker.CheckedKeys, Is.EqualTo(new[] { "u-sky" }));
			Assert.That(add.IsEnabled, Is.True, "a checked item enables Add");

			picker.OptionsList.SelectedIndex = 3; // "Person"
			RaiseKey(picker.FilterBox, Key.Enter);
			Assert.That(picker.CheckedKeys, Is.EqualTo(new[] { "u-sky", "p" }), "checks accumulate in order");

			add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(batches, Has.Count.EqualTo(1), "Add commits the whole set in ONE batch");
			Assert.That(batches[0].Select(o => o.Key), Is.EqualTo(new[] { "u-sky", "p" }),
				"the batch carries every checked item, in check order");
			Assert.That(picker.CheckedKeys, Is.Empty, "the set clears after commit so a re-open starts fresh");
		}

		[AvaloniaTest]
		public void MultiSelect_ShiftRange_ChecksTheContiguousRangeFromTheAnchor()
		{
			var (picker, window, _) = ShowMultiSelect();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			// Plain-toggle row 1 ("Sky") — this becomes the range anchor.
			picker.OptionsList.SelectedIndex = 1;
			RaiseKey(picker.FilterBox, Key.Enter);
			var anchorKey = picker.CheckedKeys.Single();

			// Shift-toggle row 3 ("Person"): the whole visible range 1..3 is checked, anchor included.
			picker.OptionsList.SelectedIndex = 3;
			var targetKey = ((RegionChoiceOption)picker.OptionsList.SelectedItem).Key;
			picker.ToggleHighlightedRange();
			Dispatcher.UIThread.RunJobs();

			Assert.That(picker.CheckedKeys, Has.Count.EqualTo(3), "shift-range checks the contiguous range 1..3");
			Assert.That(picker.CheckedKeys, Does.Contain(anchorKey).And.Contain(targetKey));

			// Shift-toggle back to row 1 from the SAME anchor clears the range (target now unchecks).
			picker.OptionsList.SelectedIndex = 3;
			picker.ToggleHighlightedRange();
			Dispatcher.UIThread.RunJobs();
			Assert.That(picker.CheckedKeys, Is.Empty, "re-ranging to a checked target clears the range");
		}

		[AvaloniaTest]
		public void MultiSelect_RowCommit_Twice_TogglesTheCheckOff()
		{
			var (picker, window, _) = ShowMultiSelect();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			picker.OptionsList.SelectedIndex = 1;
			RaiseKey(picker.FilterBox, Key.Enter); // check
			Assert.That(picker.CheckedKeys, Is.EqualTo(new[] { "u-sky" }));
			RaiseKey(picker.FilterBox, Key.Enter); // uncheck
			Assert.That(picker.CheckedKeys, Is.Empty, "committing a checked row again unchecks it");
			Assert.That(AddButton(picker).IsEnabled, Is.False, "nothing checked: Add disabled again");
		}

		[AvaloniaTest]
		public void MultiSelect_ChecksPersistAcrossSearchReQueries_AndCommitTogether()
		{
			var lexicon = new List<RegionChoiceOption>
			{
				new RegionChoiceOption("e-casa", "casa"),
				new RegionChoiceOption("e-cantar", "cantar"),
				new RegionChoiceOption("e-perro", "perro")
			};
			Func<string, IReadOnlyList<RegionChoiceOption>> search = q =>
				lexicon.Where(o => o.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase)).ToList();
			var (picker, window, batches) = ShowMultiSelect(search: search);
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			// First query: check "casa".
			picker.FilterBox.Text = "ca";
			Dispatcher.UIThread.RunJobs();
			picker.OptionsList.SelectedIndex = 0; // casa
			RaiseKey(picker.FilterBox, Key.Enter);
			Assert.That(picker.CheckedKeys, Is.EqualTo(new[] { "e-casa" }));

			// Second query (different results): check "perro". The casa check must persist even
			// though casa is no longer in the current result set.
			picker.FilterBox.Text = "pe";
			Dispatcher.UIThread.RunJobs();
			Assert.That(picker.CurrentItems.Select(o => o.Key), Is.EqualTo(new[] { "e-perro" }));
			picker.OptionsList.SelectedIndex = 0; // perro
			RaiseKey(picker.FilterBox, Key.Enter);
			Assert.That(picker.CheckedKeys, Is.EqualTo(new[] { "e-casa", "e-perro" }),
				"a check made under an earlier query survives a later re-query");

			AddButton(picker).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(batches[0].Select(o => o.Key), Is.EqualTo(new[] { "e-casa", "e-perro" }),
				"the batch resolves keys that scrolled out of the current result set");
		}

		[AvaloniaTest]
		public void SingleSelect_Default_HasNoAddButton_AndCommitsImmediately()
		{
			var (picker, window, committed, _) = ShowStatic();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			Assert.That(picker.IsMultiSelect, Is.False);
			Assert.That(picker.GetVisualDescendants().OfType<Button>()
				.Any(b => AutomationProperties.GetAutomationId(b) == "Domains.AddSelected"), Is.False,
				"single-select mode has no Add button");
			Assert.That(picker.GetVisualDescendants().OfType<CheckBox>(), Is.Empty,
				"single-select mode renders no row checkboxes");

			picker.OptionsList.SelectedIndex = 1;
			RaiseKey(picker.FilterBox, Key.Enter);
			Assert.That(committed.Select(o => o.Key), Is.EqualTo(new[] { "u-sky" }),
				"single-select commits the one highlighted item immediately (unchanged)");
		}

		[AvaloniaTest]
		public void MultiSelect_RendersACheckboxPerRow()
		{
			var (picker, window, _) = ShowMultiSelect();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			var checks = picker.OptionsList.GetVisualDescendants().OfType<CheckBox>().ToList();
			Assert.That(checks, Is.Not.Empty, "each multi-select row carries a leading checkbox");
			Assert.That(checks.All(c => c.IsChecked != true), Is.True, "nothing is checked initially");
		}

		// ===== Dropdown (collapsed) mode (the MorphType picker) =====

		private static (FwOptionPicker picker, Window window, List<RegionChoiceOption> committed) ShowDropdown(
			IReadOnlyList<RegionChoiceOption> options = null)
		{
			var picker = new FwOptionPicker(options ?? Tree(), null, "Domains", dropdown: true);
			var committed = new List<RegionChoiceOption>();
			picker.OptionCommitted += committed.Add;
			var window = new Window { Content = picker, Width = 400, Height = 420 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			return (picker, window, committed);
		}

		[AvaloniaTest]
		public void Dropdown_CollapsedByDefault_ShowsSelection_PopupClosed_NoFilterFocus()
		{
			var (picker, window, _) = ShowDropdown();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			Assert.That(picker.IsDropdown, Is.True, "the picker was built in dropdown mode");
			Assert.That(picker.IsDropdownOpen, Is.False, "the option list popup is collapsed by default");
			Assert.That(picker.FilterBox.IsFocused, Is.False,
				"a collapsed dropdown does NOT auto-focus the filter (unlike the inline flyout)");
			Assert.That(picker.DropdownText, Is.EqualTo("Universe"),
				"the collapsed box shows the current (first enabled) selection");
			// The toggle box must be reachable by automation id (the host/test contract).
			var toggle = picker.GetVisualDescendants().OfType<ToggleButton>()
				.SingleOrDefault(b => AutomationProperties.GetAutomationId(b) == "Domains.Dropdown");
			Assert.That(toggle, Is.Not.Null, "the collapsed dropdown exposes a stable toggle automation id");
		}

		[AvaloniaTest]
		public void Dropdown_Open_ShowsTheOptionListOnTop_FilterFocused()
		{
			var (picker, window, _) = ShowDropdown();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			picker.OpenDropdown();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			Assert.That(picker.IsDropdownOpen, Is.True, "clicking/opening the box pops the option list up");
			// The list + filter render in the popup's own top-level (on top), realized from the option set.
			Assert.That(picker.OptionsList.GetVisualDescendants().OfType<TextBlock>()
				.Any(t => t.Text == "Universe"), Is.True, "the option list renders when the popup is open");
			Assert.That(picker.FilterBox.IsFocused, Is.True, "opening focuses the filter, so typing filters live");
		}

		[AvaloniaTest]
		public void Dropdown_Pick_CommitsAndCollapsesShowingTheNewSelection()
		{
			var (picker, window, committed) = ShowDropdown();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			picker.OpenDropdown();
			Dispatcher.UIThread.RunJobs();

			picker.OptionsList.SelectedIndex = 3; // "Person"
			picker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(committed.Select(o => o.Key), Is.EqualTo(new[] { "p" }),
				"picking a row commits the highlighted option (single-select semantics, unchanged)");
			Assert.That(picker.IsDropdownOpen, Is.False, "a pick collapses the dropdown back");
			Assert.That(picker.DropdownText, Is.EqualTo("Person"),
				"the collapsed box now shows the newly chosen value");
		}

		[AvaloniaTest]
		public void Dropdown_ExternalSelectionMove_UpdatesTheCollapsedLabel()
		{
			// The VM's derive-on-type reselection sets OptionsList.SelectedIndex directly; the collapsed
			// label must follow it even while the popup is closed.
			var (picker, _, _) = ShowDropdown();
			Dispatcher.UIThread.RunJobs();
			Assert.That(picker.DropdownText, Is.EqualTo("Universe"));

			picker.OptionsList.SelectedIndex = 3; // "Person"
			Dispatcher.UIThread.RunJobs();

			Assert.That(picker.DropdownText, Is.EqualTo("Person"),
				"an external SelectedIndex move (the VM reselection) updates the collapsed label");
		}

		[AvaloniaTest]
		public void Dropdown_Escape_ClosesWithoutCommitting()
		{
			var (picker, window, committed) = ShowDropdown();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			picker.OpenDropdown();
			Dispatcher.UIThread.RunJobs();
			Assert.That(picker.IsDropdownOpen, Is.True);

			RaiseKey(picker.FilterBox, Key.Escape);

			Assert.That(picker.IsDropdownOpen, Is.False, "Escape collapses the dropdown");
			Assert.That(committed, Is.Empty, "Escape never commits");
		}

		[AvaloniaTest]
		public void InlineMode_HasNoDropdownToggle_AndIsNotDropdown()
		{
			// Proof the existing (default) consumers are UNCHANGED: no collapsed toggle, IsDropdown false,
			// the filter+list still render inline in the picker's own tree.
			var (picker, window, _, _) = ShowStatic();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			Assert.That(picker.IsDropdown, Is.False, "the default picker is inline, not dropdown");
			Assert.That(picker.GetVisualDescendants().OfType<ToggleButton>(), Is.Empty,
				"inline mode renders no collapsed dropdown toggle");
			Assert.That(picker.GetVisualDescendants().Contains(picker.FilterBox), Is.True,
				"the filter box still renders inline under the picker (unchanged)");
		}

		// A pointer release routed from a SPECIFIC template part: the commit guard keys off where
		// the release landed (e.Source), which headless window clicks cannot steer onto the
		// scrollbar deterministically.
		private static void RaiseRelease(Control source, Window window)
		{
			source.RaiseEvent(new PointerReleasedEventArgs(source,
				new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true),
				window, default, 0,
				new PointerPointProperties(RawInputModifiers.None,
					PointerUpdateKind.LeftButtonReleased),
				KeyModifiers.None, MouseButton.Left));
			Dispatcher.UIThread.RunJobs();
		}
	}
}
