// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Header context-menu sort toggles ("Sort From End" / "Sort By Length") and the type-specific filter
	/// presets (multipara / YesNo / integer) — the WinForms BrowseViewer header + FilterBar parity increment.
	/// Drives the owned <see cref="LexicalBrowseView"/> through its real header/filter UI, asserting the
	/// toggles are present + checkable + re-sort and that each preset is offered for the right column type and
	/// routes to the source with the expected <see cref="BrowseFilterPreset"/>.
	/// </summary>
	[TestFixture]
	public class LexicalBrowseSortFilterPresetTests
	{
		// A source that supports single + multi sort, blank-aware + type-specific presets, and exposes per-column
		// spec attributes (the column-metadata seam) keyed by column index. It records the sort keys and presets
		// it last received so tests can assert the toggle state and preset routing flowed through correctly.
		private sealed class MetaRowSource : IBrowseRowSource, IBrowseMultiSortSource, IBrowseFilterPresetSource,
			IBrowseColumnMetadataSource
		{
			private readonly List<string[]> _all;
			private readonly Dictionary<int, Dictionary<string, string>> _attrs;
			private readonly Dictionary<int, string[]> _stringLists;
			private readonly Dictionary<int, IReadOnlyList<RegionChoiceOption>> _chooserLists;
			public IReadOnlyList<BrowseSortKey> LastSortKeys;
			public int LastPresetColumn = -1;
			public BrowseFilterPreset LastPreset = BrowseFilterPreset.None;
			public string LastFilterText;
			public BrowseFilterForSpec LastPattern;
			public int LastPatternColumn = -1;
			public string LastStringListValue;
			public bool LastStringListExclude;
			public int LastStringListColumn = -1;
			public BrowseDateFilterSpec LastDateSpec;
			public int LastDateColumn = -1;
			public IReadOnlyList<string> LastListChoiceKeys;
			public int LastListChoiceColumn = -1;

			public MetaRowSource(List<string[]> all, Dictionary<int, Dictionary<string, string>> attrs,
				Dictionary<int, string[]> stringLists = null,
				Dictionary<int, IReadOnlyList<RegionChoiceOption>> chooserLists = null)
			{
				_all = all;
				_attrs = attrs;
				_stringLists = stringLists;
				_chooserLists = chooserLists;
			}

			public int RowCount => _all.Count;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => _all[rowIndex];
			public int HvoAt(int rowIndex) => rowIndex + 1;

			public void Sort(int columnIndex, bool ascending)
				=> LastSortKeys = new[] { new BrowseSortKey(columnIndex, ascending) };
			public void Sort(IReadOnlyList<BrowseSortKey> keys) => LastSortKeys = keys.ToList();

			public void SetFilter(int columnIndex, string text) => LastFilterText = text;
			public void SetFilterPreset(int columnIndex, BrowseFilterPreset preset)
			{
				LastPresetColumn = columnIndex;
				LastPreset = preset;
			}

			public void SetFilterPattern(int columnIndex, BrowseFilterForSpec spec)
			{
				LastPatternColumn = columnIndex;
				LastPattern = spec;
			}

			public void SetFilterStringListValue(int columnIndex, string value, bool exclude)
			{
				LastStringListColumn = columnIndex;
				LastStringListValue = value;
				LastStringListExclude = exclude;
			}

			public void SetFilterDate(int columnIndex, BrowseDateFilterSpec spec)
			{
				LastDateColumn = columnIndex;
				LastDateSpec = spec;
			}

			public void SetFilterListChoice(int columnIndex, IReadOnlyList<string> chosenKeys)
			{
				LastListChoiceColumn = columnIndex;
				LastListChoiceKeys = chosenKeys;
			}

			public string GetColumnSpecAttribute(int columnIndex, string attrName)
			{
				return _attrs != null && _attrs.TryGetValue(columnIndex, out var byName)
					&& byName.TryGetValue(attrName, out var value)
					? value
					: null;
			}

			public string[] GetColumnStringList(int columnIndex)
			{
				return _stringLists != null && _stringLists.TryGetValue(columnIndex, out var values)
					? values
					: null;
			}

			public IReadOnlyList<RegionChoiceOption> GetColumnChooserList(int columnIndex)
			{
				return _chooserLists != null && _chooserLists.TryGetValue(columnIndex, out var values)
					? values
					: null;
			}
		}

		private static ViewDefinitionModel ThreeColumns() => new ViewDefinitionModel(
			"LexEntry", "browse", "browse",
			new List<ViewNode>
			{
				new ViewNode("b/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "string",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null),
				new ViewNode("b/#1", ViewNodeKind.Field, "Definition", null, "Definition", "string",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null),
				new ViewNode("b/#2", ViewNodeKind.Field, "Sense Count", null, "SenseCount", "int",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null)
			},
			new List<ViewDiagnostic>());

		// Column 0 (Form): cansortbylength="true"; Column 1 (Definition): multipara="true";
		// Column 2 (Sense Count): sortType="integer". A YesNo column is added per-test as needed.
		private static MetaRowSource MakeSource() => new MetaRowSource(
			new List<string[]>
			{
				new[] { "cat", "a feline", "2" },
				new[] { "dog", "a canine", "1" }
			},
			new Dictionary<int, Dictionary<string, string>>
			{
				[0] = new Dictionary<string, string> { ["cansortbylength"] = "true" },
				[1] = new Dictionary<string, string> { ["multipara"] = "true" },
				[2] = new Dictionary<string, string> { ["sortType"] = "integer" }
			});

		private static LexicalBrowseView Show(IBrowseRowSource source)
		{
			var view = new LexicalBrowseView(ThreeColumns(), source);
			var window = new Window { Content = view, Width = 600, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return view;
		}

		private static MenuItem MenuItemById(IEnumerable<MenuItem> items, string automationId)
			=> items.First(m => AutomationProperties.GetAutomationId(m) == automationId);

		private static void Click(MenuItem item) => item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

		// ----- Sort From End -----

		[AvaloniaTest]
		public void SortFromEnd_IsPresentAndCheckable_OnASortableColumn()
		{
			var view = Show(MakeSource());
			var menu = view.HeaderContextMenuFor("Form");
			Assert.That(menu, Is.Not.Null);
			var item = menu.Items.OfType<MenuItem>()
				.FirstOrDefault(m => AutomationProperties.GetAutomationId(m) == "BrowseHeaderSortFromEnd.Form");
			Assert.That(item, Is.Not.Null, "Sort From End is offered for a sortable column");
			Assert.That(item.ToggleType, Is.EqualTo(MenuItemToggleType.CheckBox), "the toggle is checkable");
			Assert.That(item.IsChecked, Is.False, "starts unchecked");
		}

		[AvaloniaTest]
		public void ToggleSortFromEnd_FlipsState_AndReSortsWithTheFlag()
		{
			var source = MakeSource();
			var view = Show(source);

			view.ToggleSortedFromEnd(0);
			Assert.That(view.IsSortedFromEnd(0), Is.True, "toggling turns the flag on");
			Assert.That(source.LastSortKeys, Is.Not.Null.And.Not.Empty, "toggling re-sorts");
			Assert.That(source.LastSortKeys[0].Column, Is.EqualTo(0));
			Assert.That(source.LastSortKeys[0].SortedFromEnd, Is.True, "the flag flows into the sort key");

			// A freshly-rebuilt menu reflects the new checked state.
			var menu = view.HeaderContextMenuFor("Form");
			var item = menu.Items.OfType<MenuItem>()
				.First(m => AutomationProperties.GetAutomationId(m) == "BrowseHeaderSortFromEnd.Form");
			Assert.That(item.IsChecked, Is.True, "the rebuilt menu shows the toggle checked");
		}

		[AvaloniaTest]
		public void ToggleSortFromEnd_Twice_ReturnsToOriginalOrder()
		{
			var source = MakeSource();
			var view = Show(source);

			view.ToggleSortedFromEnd(0);
			Assert.That(view.IsSortedFromEnd(0), Is.True);
			view.ToggleSortedFromEnd(0);
			Assert.That(view.IsSortedFromEnd(0), Is.False, "toggling twice returns to the original (off) state");
			Assert.That(source.LastSortKeys[0].SortedFromEnd, Is.False, "the last re-sort carries the cleared flag");
		}

		// ----- Sort By Length -----

		[AvaloniaTest]
		public void SortByLength_OnlyShownWhenCanSortByLengthTrue()
		{
			var view = Show(MakeSource());

			// Column 0 (Form) carries cansortbylength="true" → the toggle is offered.
			var formMenu = view.HeaderContextMenuFor("Form");
			Assert.That(formMenu.Items.OfType<MenuItem>()
					.Any(m => AutomationProperties.GetAutomationId(m) == "BrowseHeaderSortByLength.Form"),
				Is.True, "Sort By Length is offered when cansortbylength=true");

			// Column 1 (Definition) does NOT carry the attribute → the toggle is absent.
			var defMenu = view.HeaderContextMenuFor("Definition");
			Assert.That(defMenu.Items.OfType<MenuItem>()
					.Any(m => AutomationProperties.GetAutomationId(m) == "BrowseHeaderSortByLength.Definition"),
				Is.False, "Sort By Length is hidden when the column cannot sort by length");
		}

		[AvaloniaTest]
		public void ToggleSortByLength_FlipsState_AndReSortsWithTheFlag()
		{
			var source = MakeSource();
			var view = Show(source);

			view.ToggleSortedByLength(0);
			Assert.That(view.IsSortedByLength(0), Is.True);
			Assert.That(source.LastSortKeys[0].SortedByLength, Is.True, "the by-length flag flows into the sort key");
		}

		// ----- type-specific filter presets -----

		private static IEnumerable<MenuItem> PresetItemsFor(LexicalBrowseView view, string field)
			=> view.PresetFlyoutFor(field).Items.OfType<MenuItem>();

		[AvaloniaTest]
		public void MultiparaPresets_OfferedOnlyForMultiparaColumn_AndRouteToTheRightPresets()
		{
			var source = MakeSource();
			var view = Show(source);

			// Definition (col 1) carries multipara="true".
			var defItems = PresetItemsFor(view, "Definition").ToList();
			var more = MenuItemById(defItems, "BrowseFilterPresetItem.MoreThanOneLine.Definition");
			var exactly = MenuItemById(defItems, "BrowseFilterPresetItem.ExactlyOneLine.Definition");

			// Form (col 0) is NOT multipara → no line presets.
			var formItems = PresetItemsFor(view, "Form").ToList();
			Assert.That(formItems.Any(m => (AutomationProperties.GetAutomationId(m) ?? "").Contains("MoreThanOneLine")),
				Is.False, "the multipara presets are not offered on a non-multipara column");

			Click(more);
			Assert.That(source.LastPresetColumn, Is.EqualTo(1));
			Assert.That(source.LastPreset, Is.EqualTo(BrowseFilterPreset.MoreThanOneLine));

			Click(exactly);
			Assert.That(source.LastPreset, Is.EqualTo(BrowseFilterPreset.ExactlyOneLine));
		}

		[AvaloniaTest]
		public void IntegerPresets_OfferedOnlyForIntegerColumn_AndRouteToTheRightPresets()
		{
			var source = MakeSource();
			var view = Show(source);

			// Sense Count (col 2) carries sortType="integer".
			var items = PresetItemsFor(view, "SenseCount").ToList();
			var zero = MenuItemById(items, "BrowseFilterPresetItem.Zero.SenseCount");
			var gtZero = MenuItemById(items, "BrowseFilterPresetItem.GreaterThanZero.SenseCount");
			var gtOne = MenuItemById(items, "BrowseFilterPresetItem.GreaterThanOne.SenseCount");

			// Form (col 0) is not an integer column.
			var formItems = PresetItemsFor(view, "Form").ToList();
			Assert.That(formItems.Any(m => (AutomationProperties.GetAutomationId(m) ?? "").Contains("GreaterThanZero")),
				Is.False, "the integer presets are not offered on a non-integer column");

			Click(zero);
			Assert.That(source.LastPreset, Is.EqualTo(BrowseFilterPreset.Zero));
			Click(gtZero);
			Assert.That(source.LastPreset, Is.EqualTo(BrowseFilterPreset.GreaterThanZero));
			Click(gtOne);
			Assert.That(source.LastPreset, Is.EqualTo(BrowseFilterPreset.GreaterThanOne));
		}

		[AvaloniaTest]
		public void YesNoPresets_OfferedOnlyForYesNoColumn()
		{
			// Re-tag column 0 as a YesNo column for this test.
			var source = new MetaRowSource(
				new List<string[]> { new[] { "yes", "x", "1" }, new[] { "no", "y", "0" } },
				new Dictionary<int, Dictionary<string, string>>
				{
					[0] = new Dictionary<string, string> { ["sortType"] = "YesNo" }
				});
			var view = Show(source);

			var items = PresetItemsFor(view, "Form").ToList();
			Assert.That(items.Any(m => AutomationProperties.GetAutomationId(m) == "BrowseFilterPresetItem.Yes.Form"), Is.True);
			Assert.That(items.Any(m => AutomationProperties.GetAutomationId(m) == "BrowseFilterPresetItem.No.Form"), Is.True);

			Click(MenuItemById(items, "BrowseFilterPresetItem.No.Form"));
			Assert.That(source.LastPreset, Is.EqualTo(BrowseFilterPreset.No));

			// The blank-aware presets remain universal even on a typed column.
			Assert.That(items.Any(m => AutomationProperties.GetAutomationId(m) == "BrowseFilterPresetItem.Blanks.Form"), Is.True);
		}

		[AvaloniaTest]
		public void Preset_OnNonApplicableColumn_OffersOnlyTheUniversalBlankAwareSet()
		{
			// A source with NO type attributes on any column: only Show All / Blanks / Non-blanks are offered.
			var source = new MetaRowSource(
				new List<string[]> { new[] { "a", "b", "c" } },
				new Dictionary<int, Dictionary<string, string>>());
			var view = Show(source);

			var ids = PresetItemsFor(view, "Form").Select(AutomationProperties.GetAutomationId).ToList();
			Assert.That(ids, Is.EquivalentTo(new[]
			{
				"BrowseFilterPresetItem.None.Form",
				"BrowseFilterPresetItem.Blanks.Form",
				"BrowseFilterPresetItem.NonBlanks.Form",
				// "Filter For…" is universal (FilterBar offers it on every column).
				"BrowseFilterForItem.Form"
			}), "an untyped column offers the universal blank-aware presets plus Filter For…");
		}

		// ----- "Filter For…" (universal pattern dialog) -----

		[AvaloniaTest]
		public void FilterFor_IsOfferedOnEveryColumn_Regardless_OfType()
		{
			var view = Show(MakeSource());
			foreach (var field in new[] { "Form", "Definition", "SenseCount" })
			{
				var items = PresetItemsFor(view, field).ToList();
				Assert.That(items.Any(m => AutomationProperties.GetAutomationId(m) == "BrowseFilterForItem." + field),
					Is.True, $"Filter For… is offered on the {field} column");
			}
		}

		[AvaloniaTest]
		public void FilterFor_Click_RaisesFilterForRequested_WithTheColumnIndex()
		{
			var view = Show(MakeSource());
			var raisedColumn = -1;
			view.FilterForRequested += (_, col) => raisedColumn = col;

			var item = MenuItemById(PresetItemsFor(view, "Definition").ToList(), "BrowseFilterForItem.Definition");
			Click(item);
			Assert.That(raisedColumn, Is.EqualTo(1), "Filter For… raises the request with the clicked column index");
		}

		[AvaloniaTest]
		public void ApplyFilterPattern_RoutesTheSpecToTheSource()
		{
			var source = MakeSource();
			var view = Show(source);

			var spec = new BrowseFilterForSpec
			{
				MatchText = "ca",
				MatchType = BrowsePatternMatch.AtStart,
				MatchCase = true
			};
			view.ApplyFilterPattern(0, spec);

			Assert.That(source.LastPatternColumn, Is.EqualTo(0));
			Assert.That(source.LastPattern, Is.SameAs(spec));
			Assert.That(source.LastPattern.MatchText, Is.EqualTo("ca"));
			Assert.That(source.LastPattern.MatchType, Is.EqualTo(BrowsePatternMatch.AtStart));
			Assert.That(source.LastPattern.MatchCase, Is.True);
		}

		// ----- stringList enumerated-value presets -----

		// Column 0 (Form) is a stringList of THREE values, so "Exclude X" variants are also offered.
		private static MetaRowSource MakeStringListSource() => new MetaRowSource(
			new List<string[]>
			{
				new[] { "Noun", "x", "1" },
				new[] { "Verb", "y", "2" },
				new[] { "Adjective", "z", "3" }
			},
			new Dictionary<int, Dictionary<string, string>>
			{
				[0] = new Dictionary<string, string> { ["sortType"] = "stringList" }
			},
			new Dictionary<int, string[]>
			{
				[0] = new[] { "Noun", "Verb", "Adjective" }
			});

		[AvaloniaTest]
		public void StringListPresets_OfferedOnlyForStringListColumn_OnePerValue_PlusExcludeWhenMoreThanTwo()
		{
			var view = Show(MakeStringListSource());

			var ids = PresetItemsFor(view, "Form").Select(AutomationProperties.GetAutomationId).ToList();
			// One exact-match preset per value...
			Assert.That(ids, Does.Contain("BrowseFilterStringListItem.Noun.Form"));
			Assert.That(ids, Does.Contain("BrowseFilterStringListItem.Verb.Form"));
			Assert.That(ids, Does.Contain("BrowseFilterStringListItem.Adjective.Form"));
			// ...and an Exclude variant per value (list has >2 values).
			Assert.That(ids, Does.Contain("BrowseFilterStringListItem.Exclude.Noun.Form"));
			Assert.That(ids, Does.Contain("BrowseFilterStringListItem.Exclude.Verb.Form"));
			Assert.That(ids, Does.Contain("BrowseFilterStringListItem.Exclude.Adjective.Form"));

			// A non-stringList column offers no stringList presets.
			var defIds = PresetItemsFor(view, "Definition").Select(AutomationProperties.GetAutomationId).ToList();
			Assert.That(defIds.Any(id => (id ?? "").StartsWith("BrowseFilterStringListItem.")), Is.False,
				"stringList presets are absent on a non-stringList column");
		}

		[AvaloniaTest]
		public void StringListPresets_NoExcludeVariants_WhenTwoOrFewerValues()
		{
			var source = new MetaRowSource(
				new List<string[]> { new[] { "Yes", "a", "1" }, new[] { "No", "b", "2" } },
				new Dictionary<int, Dictionary<string, string>>
				{
					[0] = new Dictionary<string, string> { ["sortType"] = "stringList" }
				},
				new Dictionary<int, string[]> { [0] = new[] { "Yes", "No" } });
			var view = Show(source);

			var ids = PresetItemsFor(view, "Form").Select(AutomationProperties.GetAutomationId).ToList();
			Assert.That(ids, Does.Contain("BrowseFilterStringListItem.Yes.Form"));
			Assert.That(ids, Does.Contain("BrowseFilterStringListItem.No.Form"));
			Assert.That(ids.Any(id => (id ?? "").StartsWith("BrowseFilterStringListItem.Exclude.")), Is.False,
				"no Exclude variants when the list has 2 or fewer values");
		}

		[AvaloniaTest]
		public void StringListPreset_Click_RoutesValueAndExcludeFlag()
		{
			var source = MakeStringListSource();
			var view = Show(source);
			var items = PresetItemsFor(view, "Form").ToList();

			Click(MenuItemById(items, "BrowseFilterStringListItem.Verb.Form"));
			Assert.That(source.LastStringListColumn, Is.EqualTo(0));
			Assert.That(source.LastStringListValue, Is.EqualTo("Verb"));
			Assert.That(source.LastStringListExclude, Is.False);

			Click(MenuItemById(items, "BrowseFilterStringListItem.Exclude.Adjective.Form"));
			Assert.That(source.LastStringListValue, Is.EqualTo("Adjective"));
			Assert.That(source.LastStringListExclude, Is.True);
		}

		// ----- "Restrict Date…" (date/genDate column dialog) -----

		// Column 0 (Form) re-tagged sortType=date; Column 1 (Definition) sortType=genDate.
		private static MetaRowSource MakeDateSource() => new MetaRowSource(
			new List<string[]>
			{
				new[] { "2020-01-01", "1999", "1" },
				new[] { "2021-06-15", "2001", "2" }
			},
			new Dictionary<int, Dictionary<string, string>>
			{
				[0] = new Dictionary<string, string> { ["sortType"] = "date" },
				[1] = new Dictionary<string, string> { ["sortType"] = "genDate" }
			});

		[AvaloniaTest]
		public void RestrictDate_OfferedOnlyForDateAndGenDateColumns()
		{
			var view = Show(MakeDateSource());

			// Form (date) and Definition (genDate) both offer Restrict Date…
			Assert.That(PresetItemsFor(view, "Form").Any(m =>
				AutomationProperties.GetAutomationId(m) == "BrowseFilterRestrictDateItem.Form"), Is.True,
				"Restrict Date… is offered on a sortType=date column");
			Assert.That(PresetItemsFor(view, "Definition").Any(m =>
				AutomationProperties.GetAutomationId(m) == "BrowseFilterRestrictDateItem.Definition"), Is.True,
				"Restrict Date… is offered on a sortType=genDate column");

			// Sense Count (integer) does NOT offer Restrict Date…
			Assert.That(PresetItemsFor(view, "SenseCount").Any(m =>
				(AutomationProperties.GetAutomationId(m) ?? "").Contains("RestrictDate")), Is.False,
				"Restrict Date… is absent on a non-date column");
		}

		[AvaloniaTest]
		public void RestrictDate_Click_RaisesRestrictDateRequested_WithTheColumnIndex()
		{
			var view = Show(MakeDateSource());
			var raisedColumn = -1;
			view.RestrictDateRequested += (_, col) => raisedColumn = col;

			Click(MenuItemById(PresetItemsFor(view, "Definition").ToList(), "BrowseFilterRestrictDateItem.Definition"));
			Assert.That(raisedColumn, Is.EqualTo(1), "Restrict Date… raises the request with the clicked column index");
		}

		[AvaloniaTest]
		public void ApplyFilterDate_RoutesTheSpecToTheSource()
		{
			var source = MakeDateSource();
			var view = Show(source);

			var spec = new BrowseDateFilterSpec
			{
				MatchType = BrowseDateMatch.Between,
				Start = new System.DateTime(2020, 1, 1),
				End = new System.DateTime(2020, 12, 31),
				HandleGenDate = true
			};
			view.ApplyFilterDate(1, spec);

			Assert.That(source.LastDateColumn, Is.EqualTo(1));
			Assert.That(source.LastDateSpec, Is.SameAs(spec));
			Assert.That(source.LastDateSpec.MatchType, Is.EqualTo(BrowseDateMatch.Between));
			Assert.That(source.LastDateSpec.HandleGenDate, Is.True);
		}

		// ----- "Choose…" (list-choice chooser) -----

		// Column 0 (Form) is a chooser column (carries a non-null chooser list through the metadata seam).
		private static MetaRowSource MakeChooserSource() => new MetaRowSource(
			new List<string[]>
			{
				new[] { "Noun", "x", "1" },
				new[] { "Verb", "y", "2" }
			},
			new Dictionary<int, Dictionary<string, string>>
			{
				[0] = new Dictionary<string, string> { ["bulkEdit"] = "atomicFlatListItem" }
			},
			chooserLists: new Dictionary<int, IReadOnlyList<RegionChoiceOption>>
			{
				[0] = new[]
				{
					new RegionChoiceOption("guid-noun", "Noun"),
					new RegionChoiceOption("guid-verb", "Verb")
				}
			});

		[AvaloniaTest]
		public void Choose_OfferedOnlyForChooserColumns()
		{
			var view = Show(MakeChooserSource());

			Assert.That(PresetItemsFor(view, "Form").Any(m =>
				AutomationProperties.GetAutomationId(m) == "BrowseFilterChooseItem.Form"), Is.True,
				"Choose… is offered on a chooser (bulkEdit) column");

			// Definition has no chooser list → no Choose… entry.
			Assert.That(PresetItemsFor(view, "Definition").Any(m =>
				(AutomationProperties.GetAutomationId(m) ?? "").Contains("ChooseItem")), Is.False,
				"Choose… is absent on a non-chooser column");
		}

		[AvaloniaTest]
		public void Choose_Click_RaisesChooseListRequested_WithTheColumnIndex()
		{
			var view = Show(MakeChooserSource());
			var raisedColumn = -1;
			view.ChooseListRequested += (_, col) => raisedColumn = col;

			Click(MenuItemById(PresetItemsFor(view, "Form").ToList(), "BrowseFilterChooseItem.Form"));
			Assert.That(raisedColumn, Is.EqualTo(0), "Choose… raises the request with the clicked column index");
		}

		[AvaloniaTest]
		public void ApplyFilterListChoice_RoutesTheChosenKeysToTheSource()
		{
			var source = MakeChooserSource();
			var view = Show(source);

			view.ApplyFilterListChoice(0, new[] { "guid-noun", "guid-verb" });

			Assert.That(source.LastListChoiceColumn, Is.EqualTo(0));
			Assert.That(source.LastListChoiceKeys, Is.EquivalentTo(new[] { "guid-noun", "guid-verb" }));
		}

		[AvaloniaTest]
		public void DateAndChoose_BothAbsent_OnAPlainTextColumn()
		{
			// A source with no date/chooser attributes: neither advanced entry appears.
			var source = new MetaRowSource(
				new List<string[]> { new[] { "a", "b", "c" } },
				new Dictionary<int, Dictionary<string, string>>());
			var view = Show(source);

			var ids = PresetItemsFor(view, "Form").Select(AutomationProperties.GetAutomationId).ToList();
			Assert.That(ids.Any(id => (id ?? "").Contains("RestrictDate")), Is.False,
				"Restrict Date… is absent on a plain text column");
			Assert.That(ids.Any(id => (id ?? "").Contains("ChooseItem")), Is.False,
				"Choose… is absent on a plain text column");
		}
	}
}
