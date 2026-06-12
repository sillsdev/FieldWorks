// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Poc;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaTests
{
	/// <summary>
	/// The ONE compact filterable option picker (FwOptionPicker) behind every option dropdown:
	/// a selection-filter panel (filter box auto-focused on open, watermarked with the search
	/// prompt) over a VIRTUALIZED list capped at the density token height, item spacing pinned
	/// to the compact legacy values (never the Fluent defaults), hierarchy preserved by Depth
	/// indent. Typing filters live (case-insensitive contains for static options; the host
	/// search delegate for search-backed pickers); Down/Up move the highlight, Enter commits
	/// the highlighted option (first match by default), Escape dismisses, click commits.
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

		private static void RaiseKey(Control target, Key key)
		{
			target.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = key,
				Source = target
			});
			Dispatcher.UIThread.RunJobs();
		}

		private static IReadOnlyList<RegionChoiceOption> Items(FwOptionPicker picker)
			=> (picker.OptionsList.ItemsSource as IEnumerable<RegionChoiceOption>)?.ToList()
				?? new List<RegionChoiceOption>();

		[AvaloniaTest]
		public void OpensWithFilterFocused_Watermarked_AndAllOptionsListed()
		{
			var (picker, _, _, _) = ShowStatic();

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
			var (picker, _, committed, _) = ShowStatic();

			RaiseKey(picker.FilterBox, Key.Down);
			RaiseKey(picker.FilterBox, Key.Down);
			Assert.That(picker.OptionsList.SelectedIndex, Is.EqualTo(2), "Down moves the highlight");
			RaiseKey(picker.FilterBox, Key.Up);
			Assert.That(picker.OptionsList.SelectedIndex, Is.EqualTo(1), "Up moves it back");

			RaiseKey(picker.FilterBox, Key.Enter);
			Assert.That(committed.Single().Key, Is.EqualTo("u-sky"), "Enter commits the highlighted option");
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
			Assert.That(container.Padding, Is.EqualTo(PocDensity.OptionItemPadding),
				"item padding mirrors the legacy WinForms menu spacing (~6,2), not Fluent");
			Assert.That(container.MinHeight, Is.EqualTo(0d), "no Fluent minimum row height");
		}

		[AvaloniaTest]
		public void List_IsVirtualized_AndHeightCapped_SoOffScreenContentScrolls()
		{
			var (picker, window, _, _) = ShowStatic();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			Assert.That(picker.OptionsList.MaxHeight, Is.EqualTo(PocDensity.OptionListMaxHeight),
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

			Assert.That(picker.OptionsList.ItemsSource, Is.Null,
				"lexicons search, lists enumerate: nothing materializes before the user types");

			window.KeyTextInput("ca");
			Assert.That(queries, Does.Contain("ca"), "typing forwards the query to the host search");
			Assert.That(Items(picker).Select(o => o.Key), Is.EqualTo(new[] { "e-casa", "e-cantar" }));

			RaiseKey(picker.FilterBox, Key.Enter);
			Assert.That(committed.Single().Key, Is.EqualTo("e-casa"),
				"Enter commits the first search result by default");
		}
	}
}
