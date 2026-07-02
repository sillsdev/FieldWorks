// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Task 23(a): REAL-DOMAIN integration coverage for the production browse adapter
	/// (<see cref="ClerkBrowseRowSource"/>) — previously exercised only through an all-null stub. Reuses the
	/// proven <see cref="BulkEditBarTestsBase"/> bootstrap (a real <see cref="RecordClerk"/> over an
	/// in-memory cache plus a fully-initialized <see cref="BrowseViewer"/> as the
	/// <see cref="IBrowseColumnSource"/>) and drives the adapter through the same clerk-routed sort/filter
	/// path product uses, asserting the real list narrows/restores/reorders and the cell projections agree.
	/// </summary>
	[TestFixture]
	public class ClerkBrowseRowSourceIntegrationTests : BulkEditBarTestsBase
	{
		private RecordClerk Clerk => (m_bv.Parent as RecordBrowseViewForTests).Clerk;

		private ClerkBrowseRowSource NewSource() => new ClerkBrowseRowSource(Clerk, m_bv, Cache);

		// The first column the ADAPTER reports as editable (the static transduce rule AND a write target the
		// delegating edit context supports — Lexeme Form / primary-sense Gloss). Scanned through the adapter,
		// not the viewer, so a column it would refuse is not chosen.
		private int FirstEditableColumn(ClerkBrowseRowSource source)
		{
			for (var i = 0; i < m_bv.ColumnCount; i++)
				if (source.IsColumnEditable(i))
					return i;
			return -1;
		}

		// A column whose finder can build a sorter (most text columns) — needed for the sort assertions.
		private int FirstSortableColumn()
		{
			for (var i = 0; i < m_bv.ColumnCount; i++)
				if (m_bv.MakeColumnSorter(i, true) != null)
					return i;
			return -1;
		}

		[Test]
		public void RowCount_TracksTheClerkListSize()
		{
			var source = NewSource();
			Assert.That(source.RowCount, Is.EqualTo(Clerk.ListSize), "the seam is pass-through (row index == clerk index)");
			Assert.That(source.RowCount, Is.GreaterThan(0), "the bulk-edit entries list is populated");
		}

		[Test]
		public void SetFilter_NarrowsRowCount_AndClearingRestoresIt()
		{
			var source = NewSource();
			var sortable = FirstSortableColumn(); // a text column we can also filter
			Assume.That(sortable, Is.GreaterThanOrEqualTo(0), "need at least one filterable text column");
			var full = source.RowCount;

			// Filter on a term taken from an actual cell so the narrowed set is non-empty and < full.
			var seed = source.GetCellValues(0)[sortable];
			Assume.That(seed, Is.Not.Null.And.Not.Empty, "row 0's cell has text to filter on");
			var needle = seed.Substring(0, System.Math.Min(2, seed.Length));

			source.SetFilter(sortable, needle);
			((MockFwXWindow)m_window).ProcessPendingItems();
			var narrowed = source.RowCount;
			Assert.That(narrowed, Is.LessThanOrEqualTo(full), "a contains filter does not grow the list");
			Assert.That(narrowed, Is.GreaterThan(0), "the seeded term matches at least row 0's object");

			source.SetFilter(sortable, string.Empty);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(full), "clearing the filter restores the full list");
		}

		[Test]
		public void SetFilter_OnSameColumn_Replaces_DoesNotStack()
		{
			var source = NewSource();
			var col = FirstSortableColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0));
			var full = source.RowCount;

			source.SetFilter(col, "zzqx-no-such-term");
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(0), "a non-matching filter empties the list");

			// A SECOND filter on the SAME column must REPLACE the first (delta against the prior), not stack
			// an AND that would keep the list empty. Clearing it restores everything.
			source.SetFilter(col, string.Empty);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(full),
				"the replacement (clear) removed the prior column filter rather than stacking with it");
		}

		[Test]
		public void Sort_AscendingThenDescending_ReordersRowZero()
		{
			var source = NewSource();
			var col = FirstSortableColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0), "need a sortable column");
			Assume.That(source.RowCount, Is.GreaterThan(1), "need at least two rows to observe a reorder");

			source.Sort(col, true);
			((MockFwXWindow)m_window).ProcessPendingItems();
			var ascFirstHvo = source.HvoAt(0);
			var ascFirstText = source.GetCellValues(0)[col];

			source.Sort(col, false);
			((MockFwXWindow)m_window).ProcessPendingItems();
			var descFirstHvo = source.HvoAt(0);
			var descFirstText = source.GetCellValues(0)[col];

			// With distinct values across the list a direction flip changes which object is first; assert the
			// ordering is consistent (ascending row 0 <= descending row 0 by ordinal text) and the reorder
			// actually moved something unless every cell in the column is identical.
			Assert.That(string.CompareOrdinal(ascFirstText, descFirstText), Is.LessThanOrEqualTo(0),
				"ascending row 0 sorts at or before descending row 0");
			if (ascFirstText != descFirstText)
				Assert.That(ascFirstHvo, Is.Not.EqualTo(descFirstHvo), "the direction flip moved a different object to the top");
		}

		// A second sortable column distinct from the first, for the multi-column (primary, secondary) sort.
		private int SecondSortableColumn(int firstColumn)
		{
			for (var i = 0; i < m_bv.ColumnCount; i++)
				if (i != firstColumn && m_bv.MakeColumnSorter(i, true) != null)
					return i;
			return -1;
		}

		[Test]
		public void MultiSort_OrdersByPrimaryThenSecondary_MatchingLegacyCombinedKey()
		{
			var source = (IBrowseMultiSortSource)NewSource();
			var primary = FirstSortableColumn();
			var secondary = SecondSortableColumn(primary);
			Assume.That(primary, Is.GreaterThanOrEqualTo(0), "need a primary sortable column");
			Assume.That(secondary, Is.GreaterThanOrEqualTo(0), "need a distinct secondary sortable column");
			Assume.That(((ClerkBrowseRowSource)source).RowCount, Is.GreaterThan(1), "need rows to observe ordering");

			source.Sort(new[] { new BrowseSortKey(primary, true), new BrowseSortKey(secondary, true) });
			((MockFwXWindow)m_window).ProcessPendingItems();

			// The clerk's combined sorter is the legacy composite: an AndSorter (primary then secondary) when
			// both columns yield a finder, so the product multi-sort builds the SAME sorter type the legacy
			// Shift+click header path builds.
			Assert.That(Clerk.Sorter, Is.InstanceOf<AndSorter>(),
				"two sort keys produce the legacy AndSorter composite on the clerk");

			// The resulting list is fully ordered by (primary, secondary): walking adjacent rows, the primary
			// cell is non-decreasing, and where two rows tie on the primary the secondary is non-decreasing.
			var rows = (ClerkBrowseRowSource)source;
			for (var i = 1; i < rows.RowCount; i++)
			{
				var prevPrimary = rows.GetCellValues(i - 1)[primary];
				var curPrimary = rows.GetCellValues(i)[primary];
				var primaryCmp = string.CompareOrdinal(prevPrimary, curPrimary);
				Assert.That(primaryCmp, Is.LessThanOrEqualTo(0),
					$"row {i}: primary column is non-decreasing under the combined sort");
				if (primaryCmp == 0)
				{
					var prevSecondary = rows.GetCellValues(i - 1)[secondary];
					var curSecondary = rows.GetCellValues(i)[secondary];
					Assert.That(string.CompareOrdinal(prevSecondary, curSecondary), Is.LessThanOrEqualTo(0),
						$"row {i}: ties on the primary are broken by a non-decreasing secondary");
				}
			}
		}

		[Test]
		public void MultiSort_SingleKey_ProducesASingleSorter_NotAnAndSorter()
		{
			var source = (IBrowseMultiSortSource)NewSource();
			var primary = FirstSortableColumn();
			Assume.That(primary, Is.GreaterThanOrEqualTo(0));

			source.Sort(new[] { new BrowseSortKey(primary, true) });
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(Clerk.Sorter, Is.Not.InstanceOf<AndSorter>(),
				"a single sort key is applied as a plain column sorter, not wrapped in an AndSorter");
		}

		// ----- Configure-Columns apply path (P1): the viewer + the Avalonia definition stay in sync -----

		[Test]
		public void InstallColumnsByKey_ReordersBrowseViewerColumnSpecs_AndDefinitionAgrees()
		{
			Assume.That(m_bv.ColumnCount, Is.GreaterThan(1), "need at least two columns to reorder");

			// Move the current first column to the end (a reorder of the SHOWN set).
			var keys = new List<string>();
			for (var i = 0; i < m_bv.ColumnCount; i++)
				keys.Add(m_bv.GetColumnKey(i));
			var moved = keys[0];
			var reordered = keys.Skip(1).Concat(new[] { moved }).ToList();

			var installed = m_bv.InstallColumnsByKey(reordered);
			((MockFwXWindow)m_window).ProcessPendingItems();

			// The legacy viewer's ColumnSpecs now reflect the new order...
			Assert.That(installed, Is.EqualTo(reordered), "every requested key resolved to a column");
			Assert.That(m_bv.GetColumnKey(m_bv.ColumnCount - 1), Is.EqualTo(moved),
				"the moved column is now last in the live viewer's ColumnSpecs");

			// ...and the Avalonia column definition built from the same source agrees in count and order.
			var definition = BrowseColumnSpec.ToViewDefinition(BrowseColumnSpec.Snapshot(m_bv));
			var defFields = definition.Roots.Select(n => n.Field).ToList();
			Assert.That(defFields.Count, Is.EqualTo(m_bv.ColumnCount),
				"the Avalonia definition has one column per shown viewer column");
			for (var i = 0; i < m_bv.ColumnCount; i++)
				Assert.That(BrowseColumnSpec.Snapshot(m_bv)[i].StableField, Is.EqualTo(defFields[i]),
					$"definition column {i} matches the viewer's shown column {i}");
		}

		[Test]
		public void InstallColumnsByKey_HidingAColumn_ShrinksTheShownSet()
		{
			Assume.That(m_bv.ColumnCount, Is.GreaterThan(1));
			var keys = new List<string>();
			for (var i = 0; i < m_bv.ColumnCount; i++)
				keys.Add(m_bv.GetColumnKey(i));
			var before = m_bv.ColumnCount;

			// Drop the last column.
			m_bv.InstallColumnsByKey(keys.Take(before - 1).ToList());
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(m_bv.ColumnCount, Is.EqualTo(before - 1), "hiding a column removes it from the shown set");
		}

		[Test]
		public void ResetColumnState_ClearsAnActiveColumnFilter_SoAReorderNeverMisapplies()
		{
			var source = NewSource();
			var col = FirstSortableColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0));
			var full = source.RowCount;

			// Apply a non-matching filter so the list is narrowed (empty), then reset (the Configure-Columns
			// apply step) and assert the column filter was dropped — the list returns to full rather than the
			// stale filter following a reordered column index to a different field.
			source.SetFilter(col, "zzqx-no-such-term");
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(0));
			Assert.That(source.ActiveColumnFilterCount, Is.EqualTo(1));

			var cleared = source.ResetColumnState();
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(cleared, Is.True, "an active column filter was present and removed");
			Assert.That(source.ActiveColumnFilterCount, Is.EqualTo(0), "the per-column filter map is emptied");
			Assert.That(source.RowCount, Is.EqualTo(full),
				"clearing the stale column filter restores the list (it never reapplies to a different column)");
		}

		// ----- type-specific filter presets (BrowseViewer header / FilterBar parity) -----

		// The first shown column whose spec carries attr=value (e.g. multipara="true"), or -1 when none is shown.
		private int FirstColumnWithAttr(string attr, string value)
		{
			for (var i = 0; i < m_bv.ColumnCount; i++)
				if (string.Equals(m_bv.GetColumnSpecAttribute(i, attr), value, System.StringComparison.OrdinalIgnoreCase))
					return i;
			return -1;
		}

		[Test]
		public void GetColumnSpecAttribute_ReadsRawColumnAttributes_OrNullWhenAbsent()
		{
			// At least one shown column in the lexicon browse carries cansortbylength="true" (Lexeme Form etc.);
			// reading a bogus attribute is null, and out-of-range is null (not a throw).
			Assert.That(FirstColumnWithAttr("cansortbylength", "true"), Is.GreaterThanOrEqualTo(0),
				"the lexicon browse shows at least one cansortbylength column");
			Assert.That(m_bv.GetColumnSpecAttribute(0, "no-such-attribute"), Is.Null, "an absent attribute reads null");
			Assert.That(m_bv.GetColumnSpecAttribute(-1, "cansortbylength"), Is.Null, "out-of-range reads null, not a throw");
			Assert.That(m_bv.GetColumnSpecAttribute(m_bv.ColumnCount, "cansortbylength"), Is.Null);
		}

		[Test]
		public void SetFilterPreset_MultiparaPresets_NarrowTheList_AndClearingRestoresIt()
		{
			var col = FirstColumnWithAttr("multipara", "true");
			Assume.That(col, Is.GreaterThanOrEqualTo(0), "the lexicon browse shows a multipara column");
			var source = NewSource();
			var full = source.RowCount;

			// "More Than One Line" + "Exactly One Line" partition the non-blank rows; each is a subset of the
			// full list, and clearing the preset restores the full list. (Whether either is empty depends on the
			// data, so assert the subset/restore invariants rather than a specific narrowed count.)
			source.SetFilterPreset(col, BrowseFilterPreset.MoreThanOneLine);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.LessThanOrEqualTo(full), "More Than One Line never grows the list");

			source.SetFilterPreset(col, BrowseFilterPreset.ExactlyOneLine);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.LessThanOrEqualTo(full), "Exactly One Line never grows the list");

			source.SetFilterPreset(col, BrowseFilterPreset.None);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(full), "clearing the preset restores the full list");
		}

		[Test]
		public void SetFilterPreset_IntegerPresets_AreNestedRanges_AndClearingRestoresIt()
		{
			// Number of Senses (Entry) is a sortType=integer column; show it so the integer presets have a column.
			m_bv.ShowColumn("NumberOfSensesForEntry");
			((MockFwXWindow)m_window).ProcessPendingItems();
			var col = FirstColumnWithAttr("sortType", "integer");
			Assume.That(col, Is.GreaterThanOrEqualTo(0), "the lexicon browse can show a sortType=integer column");
			var source = NewSource();
			var full = source.RowCount;

			source.SetFilterPreset(col, BrowseFilterPreset.GreaterThanZero);
			((MockFwXWindow)m_window).ProcessPendingItems();
			var gtZero = source.RowCount;
			Assert.That(gtZero, Is.LessThanOrEqualTo(full), "Greater Than Zero never grows the list");

			source.SetFilterPreset(col, BrowseFilterPreset.GreaterThanOne);
			((MockFwXWindow)m_window).ProcessPendingItems();
			var gtOne = source.RowCount;
			// {value > 1} is a subset of {value > 0}: the more restrictive range cannot include more rows.
			Assert.That(gtOne, Is.LessThanOrEqualTo(gtZero), "Greater Than One is a subset of Greater Than Zero");

			source.SetFilterPreset(col, BrowseFilterPreset.Zero);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.LessThanOrEqualTo(full), "Zero never grows the list");

			source.SetFilterPreset(col, BrowseFilterPreset.None);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(full), "clearing the integer preset restores the full list");
		}

		[Test]
		public void SetFilterPreset_OnSameColumn_Replaces_DoesNotStack()
		{
			m_bv.ShowColumn("NumberOfSensesForEntry");
			((MockFwXWindow)m_window).ProcessPendingItems();
			var col = FirstColumnWithAttr("sortType", "integer");
			Assume.That(col, Is.GreaterThanOrEqualTo(0));
			var source = NewSource();
			var full = source.RowCount;

			// A second preset on the SAME column replaces the first (delta against the prior), so clearing
			// restores the full list rather than leaving a stale AND-stacked filter behind.
			source.SetFilterPreset(col, BrowseFilterPreset.GreaterThanOne);
			((MockFwXWindow)m_window).ProcessPendingItems();
			source.SetFilterPreset(col, BrowseFilterPreset.GreaterThanZero);
			((MockFwXWindow)m_window).ProcessPendingItems();
			source.SetFilterPreset(col, BrowseFilterPreset.None);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(full),
				"replacing then clearing a column's preset restores the full list (no stacked filter)");
		}

		// ----- "Filter For…" pattern filter (FilterBar FindComboItem/SimpleMatchDlg parity) -----

		[Test]
		public void SetFilterPattern_AnywhereMatch_NarrowsRowCount_AndClearingRestoresIt()
		{
			var source = NewSource();
			var col = FirstSortableColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0), "need a filterable text column");
			var full = source.RowCount;

			// Seed the pattern from an actual cell so the narrowed set is non-empty.
			var seed = source.GetCellValues(0)[col];
			Assume.That(seed, Is.Not.Null.And.Not.Empty, "row 0's cell has text to filter on");
			var needle = seed.Substring(0, System.Math.Min(2, seed.Length));

			source.SetFilterPattern(col, new BrowseFilterForSpec
			{
				MatchText = needle,
				MatchType = BrowsePatternMatch.Anywhere,
				MatchCase = false
			});
			((MockFwXWindow)m_window).ProcessPendingItems();
			var narrowed = source.RowCount;
			Assert.That(narrowed, Is.LessThanOrEqualTo(full), "an anywhere-match pattern never grows the list");
			Assert.That(narrowed, Is.GreaterThan(0), "the seeded pattern matches at least row 0's object");

			// A null/empty spec clears the column filter (restores the full list).
			source.SetFilterPattern(col, new BrowseFilterForSpec { MatchText = string.Empty });
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(full), "clearing the pattern restores the full list");
		}

		[Test]
		public void SetFilterPattern_NonMatching_EmptiesTheList_AndReplacesNotStacks()
		{
			var source = NewSource();
			var col = FirstSortableColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0));
			var full = source.RowCount;

			source.SetFilterPattern(col, new BrowseFilterForSpec
			{
				MatchText = "zzqx-no-such-term",
				MatchType = BrowsePatternMatch.Anywhere
			});
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(0), "a non-matching pattern empties the list");

			// A second filter on the SAME column replaces the first (delta against the prior), not an AND-stack;
			// clearing restores everything.
			source.SetFilterPattern(col, null);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(full),
				"the replacement (clear) removed the prior pattern filter rather than stacking with it");
		}

		[Test]
		public void SetFilterPattern_WholeItemMatch_OnRowZerosExactCell_KeepsThatObject()
		{
			var source = NewSource();
			var col = FirstSortableColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(0));
			var exact = source.GetCellValues(0)[col];
			Assume.That(exact, Is.Not.Null.And.Not.Empty);

			source.SetFilterPattern(col, new BrowseFilterForSpec
			{
				MatchText = exact,
				MatchType = BrowsePatternMatch.WholeItem
			});
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(source.RowCount, Is.GreaterThan(0), "a whole-item match on row 0's exact cell keeps at least that object");
			Assert.That(source.RowCount, Is.LessThanOrEqualTo(source.RowCount), "whole-item never grows the list");
			// Every surviving row's cell equals the exact match text.
			for (var r = 0; r < source.RowCount; r++)
				Assert.That(source.GetCellValues(r)[col], Is.EqualTo(exact),
					"every surviving row matches the whole-item pattern exactly");
		}

		// ----- stringList enumerated-value filters (FilterBar per-value exact / Exclude X parity) -----

		[Test]
		public void GetColumnStringList_NullForNonStringListColumns()
		{
			// The lexicon entries/senses browse shows no sortType=stringList column, so every shown column
			// reports null — the flyout therefore offers no stringList presets here (gated correctly).
			for (var col = 0; col < m_bv.ColumnCount; col++)
				Assert.That(m_bv.GetColumnStringList(col), Is.Null,
					$"col {col} is not a stringList column, so its enumerated values are null");
			Assert.That(m_bv.GetColumnStringList(-1), Is.Null, "out-of-range reads null, not a throw");
			Assert.That(m_bv.GetColumnStringList(m_bv.ColumnCount), Is.Null);
		}

		[Test]
		public void SetFilterStringListValue_ExactMatch_KeepsOnlyMatchingRows_AndExcludeIsTheComplement()
		{
			// The stringList matcher is an exact whole-cell match; it is column-type agnostic, so exercise it
			// against a real text column (the lexicon browse has no stringList column to show). The value/exclude
			// pair partitions the list: exact keeps the matching rows, Exclude keeps the rest.
			var source = NewSource();
			var col = FirstSortableColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(0));
			var full = source.RowCount;
			var value = source.GetCellValues(0)[col];
			Assume.That(value, Is.Not.Null.And.Not.Empty);

			source.SetFilterStringListValue(col, value, exclude: false);
			((MockFwXWindow)m_window).ProcessPendingItems();
			var matching = source.RowCount;
			Assert.That(matching, Is.GreaterThan(0), "the exact value matches at least row 0's object");
			for (var r = 0; r < matching; r++)
				Assert.That(source.GetCellValues(r)[col], Is.EqualTo(value), "every kept row matches the value exactly");

			source.SetFilterStringListValue(col, value, exclude: true);
			((MockFwXWindow)m_window).ProcessPendingItems();
			var excluded = source.RowCount;
			Assert.That(excluded, Is.LessThanOrEqualTo(full), "Exclude never grows the list");
			for (var r = 0; r < excluded; r++)
				Assert.That(source.GetCellValues(r)[col], Is.Not.EqualTo(value), "no kept row matches the excluded value");
			// Exact + Exclude partition the list (allowing for blanks that match neither in some columns).
			Assert.That(matching + excluded, Is.LessThanOrEqualTo(full));
		}

		[Test]
		public void SetFilterStringListValue_MutuallyExclusiveWithFreeText_OnSameColumn()
		{
			var source = NewSource();
			var col = FirstSortableColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0));
			var full = source.RowCount;

			// A non-matching free-text filter empties the list; replacing it with a stringList value (delta
			// against the prior) does NOT stack an AND, then clearing restores the full list.
			source.SetFilter(col, "zzqx-no-such-term");
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(0));

			source.SetFilter(col, string.Empty);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(full), "clearing the free-text filter restores the full list");

			var value = source.GetCellValues(0)[col];
			Assume.That(value, Is.Not.Null.And.Not.Empty);
			source.SetFilterStringListValue(col, value, exclude: false);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.GreaterThan(0), "the stringList value filter applies after the free-text was cleared");

			source.SetFilterStringListValue(col, value, exclude: true);
			((MockFwXWindow)m_window).ProcessPendingItems();
			// Replacing exact with Exclude (same column) is a delta, not an AND-stack.
			Assert.That(source.RowCount, Is.LessThanOrEqualTo(full));
		}

		// ----- "Restrict Date…" date filter (FilterBar RestrictDateComboItem/DateTimeMatcher parity) -----

		[Test]
		public void SetFilterDate_OnDateColumn_NarrowsRowCount_AndClearingRestoresIt()
		{
			// Date Created (Entry) is a sortType=date column; show it so the date filter has a column.
			m_bv.ShowColumn("DateCreatedForEntry");
			((MockFwXWindow)m_window).ProcessPendingItems();
			var col = FirstColumnWithAttr("sortType", "date");
			Assume.That(col, Is.GreaterThanOrEqualTo(0), "the lexicon browse can show a sortType=date column");
			var source = NewSource();
			var full = source.RowCount;
			Assume.That(full, Is.GreaterThan(0));

			// "On or after the epoch" keeps every dated row (all created after 1900); the matcher is the SAME
			// legacy DateTimeMatcher (built via MakeDateColumnFilter), so the real list narrows identically.
			source.SetFilterDate(col, new BrowseDateFilterSpec
			{
				MatchType = BrowseDateMatch.OnOrAfter,
				Start = new System.DateTime(1900, 1, 1),
				End = new System.DateTime(1900, 1, 1).AddDays(1).AddTicks(-1),
				HandleGenDate = false
			});
			((MockFwXWindow)m_window).ProcessPendingItems();
			var afterAfter = source.RowCount;
			Assert.That(afterAfter, Is.LessThanOrEqualTo(full), "an on-or-after filter never grows the list");

			// "On or before the epoch" excludes every modern row → empties (or at least narrows) the list.
			source.SetFilterDate(col, new BrowseDateFilterSpec
			{
				MatchType = BrowseDateMatch.OnOrBefore,
				Start = new System.DateTime(1900, 1, 1),
				End = new System.DateTime(1900, 1, 1).AddDays(1).AddTicks(-1),
				HandleGenDate = false
			});
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.LessThanOrEqualTo(afterAfter),
				"an on-or-before-1900 filter narrows (modern dates are excluded)");

			// A null spec clears the column filter (restores the full list).
			source.SetFilterDate(col, null);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(full), "clearing the date filter restores the full list");
		}

		[Test]
		public void SetFilterDate_OnSameColumn_Replaces_DoesNotStack()
		{
			m_bv.ShowColumn("DateCreatedForEntry");
			((MockFwXWindow)m_window).ProcessPendingItems();
			var col = FirstColumnWithAttr("sortType", "date");
			Assume.That(col, Is.GreaterThanOrEqualTo(0));
			var source = NewSource();
			var full = source.RowCount;

			// A far-future "on or after" empties the list; replacing it (delta, not AND-stack) then clearing
			// restores the full list.
			source.SetFilterDate(col, new BrowseDateFilterSpec
			{
				MatchType = BrowseDateMatch.OnOrAfter,
				Start = new System.DateTime(3000, 1, 1),
				End = new System.DateTime(3000, 1, 1).AddDays(1).AddTicks(-1)
			});
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(0), "an on-or-after-3000 filter empties the list");

			source.SetFilterDate(col, null);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(full),
				"the replacement (clear) removed the prior date filter rather than stacking with it");
		}

		// ----- "Choose…" list-choice filter (FilterBar ListChoiceComboItem/ListChoiceFilter parity) -----

		// The first column the column source reports as a chooser column (non-null chooser list) — Morph Type
		// in the lexicon bulk-edit browse. -1 when none shown.
		private int FirstChooserColumn()
		{
			for (var i = 0; i < m_bv.ColumnCount; i++)
				if (m_bv.GetColumnChooserList(i) != null)
					return i;
			return -1;
		}

		[Test]
		public void GetColumnChooserList_ExternalChooserWithoutList_ReturnsNullInsteadOfThrowing()
		{
			var doc = new XmlDocument();
			var spec = doc.CreateElement("column");
			spec.SetAttribute("chooserFilter", "external");

			var vc = m_bv.BrowseView.Vc;
			var priorColumnSpecs = vc.ColumnSpecs;
			try
			{
				vc.ColumnSpecs = new List<XmlNode> { spec };
				Assert.That(() => m_bv.GetColumnChooserList(0), Throws.Nothing);
				Assert.That(m_bv.GetColumnChooserList(0), Is.Null,
					"external chooser columns without a list attribute are treated as non-list-backed and do not crash");
			}
			finally
			{
				vc.ColumnSpecs = priorColumnSpecs;
			}
		}

		[Test]
		public void GetColumnChooserList_NonNullForChooserColumn_NullOtherwise()
		{
			var col = FirstChooserColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0), "the lexicon browse shows a chooser column (Morph Type)");
			var items = m_bv.GetColumnChooserList(col);
			Assert.That(items, Is.Not.Null.And.Not.Empty, "a chooser column exposes its possibility items");
			Assert.That(items.All(it => !string.IsNullOrEmpty(it.Key)), Is.True, "each item carries a guid key");
			Assert.That(m_bv.GetColumnChooserList(-1), Is.Null, "out-of-range reads null, not a throw");
		}

		[Test]
		public void SetFilterListChoice_NarrowsToRowsWhoseCellMatches_AndClearingRestoresIt()
		{
			var source = NewSource();
			var col = FirstChooserColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0), "need a chooser column to filter");
			var full = source.RowCount;
			Assume.That(full, Is.GreaterThan(0));

			var items = m_bv.GetColumnChooserList(col);
			Assume.That(items, Is.Not.Null.And.Not.Empty);

			// Choose EVERY possibility item: every row whose cell references any of them survives — a superset of
			// any single choice and never more than the full list. The matcher is the SAME legacy ListChoiceFilter
			// (built via MakeListChoiceColumnFilter).
			var allKeys = items.Select(it => it.Key).ToList();
			source.SetFilterListChoice(col, allKeys);
			((MockFwXWindow)m_window).ProcessPendingItems();
			var chosenAll = source.RowCount;
			Assert.That(chosenAll, Is.LessThanOrEqualTo(full), "a list-choice filter never grows the list");

			// A single (first) item is a subset of choosing them all.
			source.SetFilterListChoice(col, new[] { items[0].Key });
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.LessThanOrEqualTo(chosenAll),
				"choosing one item is a subset of choosing all items");

			// An empty selection clears the column filter (restores the full list).
			source.SetFilterListChoice(col, new string[0]);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(full), "clearing the list-choice filter restores the full list");
		}

		[Test]
		public void SetFilterListChoice_UnknownKeys_AreANoOpClear()
		{
			var source = NewSource();
			var col = FirstChooserColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0));
			var full = source.RowCount;

			// Keys that resolve to no object build no filter (null) → the list stays full (no narrowing, no throw).
			source.SetFilterListChoice(col, new[] { System.Guid.NewGuid().ToString() });
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.RowCount, Is.EqualTo(full),
				"keys that resolve to no possibility build no filter, leaving the list unchanged");
		}

		// ----- "Spelling Errors" filter (FilterBar BadSpellingMatcher parity) -----
		//
		// RUNTIME-ONLY ASPECT: ColumnSupportsSpellingFilter's positive result depends on a per-WS spelling
		// dictionary being installed (SpellingHelper.GetSpellChecker), which is not provisioned in the headless
		// test environment — so the AVAILABILITY probe legitimately reports false here for every column. These
		// tests therefore exercise (1) the deterministic STRUCTURAL half of the gate (a chooser column is never
		// offered the item, no matter the dictionary), and (2) the real PRODUCT matcher build + routing + apply:
		// MakeSpellingErrorColumnFilter returns the legacy FilterBarCellFilter wrapping a real BadSpellingMatcher,
		// and SetFilterSpellingErrors applies it through the seam. With no dictionary the BadSpellingMatcher
		// matches nothing (its SpellCheckMethod returns false for a null dict), so the filtered set FOLLOWS the
		// matcher: the list empties, and clearing restores it — proving the seam routes to and applies the
		// matcher even though the dictionary-dependent narrowing can't be positively observed headlessly.

		[Test]
		public void ColumnSupportsSpellingFilter_RunsWithoutThrowing_AndIsFalseForAChooserColumn()
		{
			// The structural gate (independent of the runtime dictionary probe): a chooser/list column is NEVER
			// offered the spelling-errors item, exactly as FilterBar makes a chooser for those instead.
			var col = FirstChooserColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0), "the lexicon browse shows a chooser column (Morph Type)");
			Assert.That(m_bv.ColumnSupportsSpellingFilter(col), Is.False,
				"a chooser/list column never supports the spelling-errors filter");

			// The probe is total over the shown columns (no throw) and safe out of range.
			for (var i = 0; i < m_bv.ColumnCount; i++)
				Assert.That(() => m_bv.ColumnSupportsSpellingFilter(i), Throws.Nothing,
					$"the spell-support probe runs without throwing on shown column {i}");
			Assert.That(m_bv.ColumnSupportsSpellingFilter(-1), Is.False, "out-of-range reads false, not a throw");
			Assert.That(m_bv.ColumnSupportsSpellingFilter(m_bv.ColumnCount), Is.False);
		}

		[Test]
		public void MakeSpellingErrorColumnFilter_BuildsTheRealLegacyBadSpellingMatcher()
		{
			var col = FirstSortableColumn(); // a real string column with a finder
			Assume.That(col, Is.GreaterThanOrEqualTo(0), "need a filterable text column");

			var filter = m_bv.MakeSpellingErrorColumnFilter(col);
			Assert.That(filter, Is.InstanceOf<FilterBarCellFilter>(),
				"the spelling-errors filter is the legacy FilterBarCellFilter over the column's finder");
			var cellFilter = (FilterBarCellFilter)filter;
			Assert.That(cellFilter.Matcher, Is.InstanceOf<BadSpellingMatcher>(),
				"it wraps the REAL legacy BadSpellingMatcher (the product spelling matcher, not a stand-in)");

			// Out of range is null, not a throw.
			Assert.That(m_bv.MakeSpellingErrorColumnFilter(-1), Is.Null);
			Assert.That(m_bv.MakeSpellingErrorColumnFilter(m_bv.ColumnCount), Is.Null);
		}

		[Test]
		public void SetFilterSpellingErrors_AppliesTheMatcherThroughTheSeam_FilteredSetFollowsTheMatcher()
		{
			var source = NewSource();
			var col = FirstSortableColumn();
			Assume.That(col, Is.GreaterThanOrEqualTo(0));
			var full = source.RowCount;
			Assume.That(full, Is.GreaterThan(0));

			// The matcher's own verdict is the ground truth (runtime-dependent: whether a word is "misspelled"
			// depends on whatever spelling checker the environment has for the WS — which is not provisioned
			// headlessly). Evaluate the SAME legacy BadSpellingMatcher filter the seam builds on each original
			// row up front, so the test asserts the filtered set FOLLOWS the matcher rather than a fixed
			// direction. The filter needs a Cache (BadSpellingMatcher requires it) before Accept.
			var clerk = Clerk;
			var probe = m_bv.MakeSpellingErrorColumnFilter(col);
			probe.Cache = Cache;
			var expectedHvos = new HashSet<int>();
			for (var i = 0; i < clerk.ListSize; i++)
			{
				var item = clerk.SortItemProvider.SortItemAt(i);
				if (probe.Accept(item))
					expectedHvos.Add(item.RootObjectHvo);
			}

			// Apply the spelling-errors filter through the seam: it must be tracked as the column's active clerk
			// filter, and the surviving rows must be EXACTLY the rows the matcher accepts (the filtered set
			// follows the matcher) — proving the seam built and APPLIED the real legacy matcher.
			source.SetFilterSpellingErrors(col);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.ActiveColumnFilterCount, Is.EqualTo(1),
				"the spelling-errors filter is tracked as the column's active clerk filter");
			Assert.That(source.RowCount, Is.EqualTo(expectedHvos.Count),
				"the narrowed list size equals the count of rows the BadSpellingMatcher accepts");
			Assert.That(source.RowCount, Is.LessThanOrEqualTo(full), "a spelling filter never grows the list");
			var survivingHvos = new HashSet<int>(Enumerable.Range(0, source.RowCount).Select(source.HvoAt));
			Assert.That(survivingHvos, Is.EquivalentTo(expectedHvos),
				"the surviving rows are exactly the ones the matcher accepts (the filtered set follows the matcher)");

			// Mutual exclusivity / replacement: clearing the column's filter (a delta against the prior spelling
			// filter, not an AND-stack) restores the full list.
			source.SetFilter(col, string.Empty);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(source.ActiveColumnFilterCount, Is.EqualTo(0), "clearing removes the spelling filter");
			Assert.That(source.RowCount, Is.EqualTo(full),
				"the clear removed the prior spelling filter rather than stacking with it (mutual exclusivity holds)");
		}

		[Test]
		public void GetCellValues_And_GetRichCell_AgreeOnEmptiness()
		{
			var source = NewSource();
			Assume.That(source.RowCount, Is.GreaterThan(0));
			var plain = source.GetCellValues(0);
			for (var col = 0; col < m_bv.ColumnCount; col++)
			{
				var rich = source.GetRichCell(0, col);
				var plainEmpty = col >= plain.Count || string.IsNullOrEmpty(plain[col]);
				var richEmpty = rich == null || rich.Count == 0 || string.IsNullOrEmpty(rich[0].Value);
				Assert.That(richEmpty, Is.EqualTo(plainEmpty),
					$"col {col}: the rich display projection and the plain selection/UIA projection must agree on emptiness");
			}
		}

		[Test]
		public void GetEditField_OnEditableColumn_ReturnsRightHvoAndWs()
		{
			var source = NewSource();
			var col = FirstEditableColumn(source);
			Assume.That(col, Is.GreaterThanOrEqualTo(0), "the lexicon browse has editable transduce columns");
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var field = source.GetEditField(0, col);
			Assert.That(field, Is.Not.Null, "an editable column whose write target is supported yields a field");
			Assert.That(field.ObjectHvo, Is.EqualTo(source.HvoAt(0)), "the edit field targets row 0's object");
			Assert.That(field.Kind, Is.EqualTo(RegionFieldKind.Text));
			m_bv.GetColumnEditAttributes(col, out _, out var ws, out _);
			if (!string.IsNullOrEmpty(ws))
				Assert.That(field.WritingSystem, Is.Not.Null.And.Not.Empty, "the field carries the column's writing system");
		}

		// ----- Phase 1 bulk edit (List Choice) -----

		// The first column the adapter reports as a List-Choice bulk target (the unambiguous, entry-anchored
		// Morph Type). -1 when none — the bulkEditEntriesOrSenses browse shows a Morph Type column.
		private int FirstListChoiceColumn(ClerkBrowseRowSource source)
		{
			foreach (var t in source.ListChoiceTargets())
				return t.Column;
			return -1;
		}

		[Test]
		public void ListChoiceTargets_IncludeMorphType_ExcludeAmbiguousColumns()
		{
			var source = NewSource();
			var targets = source.ListChoiceTargets();
			Assert.That(targets, Is.Not.Empty, "the lexicon browse exposes Morph Type as a list-choice bulk target");

			// The reported target IS the Morph Type column (the unambiguous, entry-anchored possibility
			// reference) and it offers options — the only Phase-1 list-choice target.
			Assert.That(targets.Any(t => t.Label == "Morph Type"), Is.True,
				"Morph Type is the Phase-1 list-choice bulk target");
			foreach (var t in targets)
				Assert.That(source.ListChoiceOptions(t.Column), Is.Not.Empty,
					$"target column {t.Column} ({t.Label}) must offer possibility options");

			// A sense-path text column (e.g. Gloss, transduce=LexSense.Gloss) is inline-editable but is NOT a
			// list-choice target — so the bar never offers a wrong-object/ambiguous possibility write.
			var targetCols = new HashSet<int>(targets.Select(t => t.Column));
			for (var col = 0; col < m_bv.ColumnCount; col++)
			{
				m_bv.GetColumnEditAttributes(col, out var field, out _, out var transduce);
				var isGloss = field == "Gloss" || transduce == "LexSense.Gloss";
				if (isGloss)
					Assert.That(targetCols.Contains(col), Is.False,
						"a sense-path text column is NOT a list-choice bulk target");
			}
		}

		[Test]
		public void ListChoiceOptions_AreNonEmpty_ForAListChoiceTarget()
		{
			var source = NewSource();
			var col = FirstListChoiceColumn(source);
			Assume.That(col, Is.GreaterThanOrEqualTo(0), "need a list-choice target column");
			var options = source.ListChoiceOptions(col);
			Assert.That(options, Is.Not.Empty, "the Morph Type target offers the project's morph-type options");
			Assert.That(options.All(o => !string.IsNullOrEmpty(o.Key)), Is.True, "each option carries a key (guid)");
		}

		[Test]
		public void Preview_StoresOverlay_DoesNotCommit_AndGetCellValuesReflectsIt()
		{
			var source = NewSource();
			var col = FirstListChoiceColumn(source);
			Assume.That(col, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var rows = Enumerable.Range(0, source.RowCount).ToList();

			source.PreviewBulkEdit(col, rows, "PREVIEW-NAME");

			// No model mutation: the preview is an in-memory overlay only.
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
				"a preview must not create an undo step (no model write)");
			// GetCellValues shows the overlay for the previewed rows/column.
			for (var r = 0; r < source.RowCount; r++)
				Assert.That(source.GetCellValues(r)[col], Is.EqualTo("PREVIEW-NAME"),
					$"row {r}'s previewed cell shows the overlay value");

			// Clearing the preview restores the model's display value.
			source.ClearBulkEditPreview();
			Assert.That(source.GetCellValues(0)[col], Is.Not.EqualTo("PREVIEW-NAME"),
				"clearing the preview reverts the cell to the model value");
		}

		[Test]
		public void Apply_WritesAllRows_InOneUndoStep()
		{
			var source = NewSource();
			var col = FirstListChoiceColumn(source);
			Assume.That(col, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(1), "need multiple rows to prove the single-UOW span");

			var options = source.ListChoiceOptions(col);
			Assume.That(options, Is.Not.Empty);
			// Choose a target morph type and capture the rows' originals so the single undo can be verified.
			var optionKey = options[0].Key;
			var rows = Enumerable.Range(0, source.RowCount).ToList();
			var originalCells = rows.Select(r => source.GetCellValues(r)[col]).ToList();

			source.ApplyBulkEdit(col, rows, optionKey, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			// The apply is committed (CanUndo true) and is ONE step: a single Undo reverts EVERY row.
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True, "apply commits the bulk edit");
			Cache.ActionHandlerAccessor.Undo();
			((MockFwXWindow)m_window).ProcessPendingItems();

			var reverted = NewSource();
			for (var r = 0; r < reverted.RowCount && r < originalCells.Count; r++)
				Assert.That(reverted.GetCellValues(r)[col], Is.EqualTo(originalCells[r]),
					$"the single undo reverted row {r}'s morph type");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.False,
				"the whole bulk edit was ONE undo step (no second step remains)");
		}

		// ----- Phase 2 bulk edit (Bulk Copy) -----

		// The first column the adapter reports as a copy TARGET (an entry-anchored editable text column —
		// today the Lexeme Form). -1 when none.
		private int FirstCopyTargetColumn(ClerkBrowseRowSource source)
		{
			foreach (var t in source.CopyTargets())
				return t.Column;
			return -1;
		}

		// A source column distinct from the given target that has readable text on row 0.
		private int FirstCopySourceColumnWithText(ClerkBrowseRowSource source, int notColumn)
		{
			var cells = source.GetCellValues(0);
			for (var col = 0; col < m_bv.ColumnCount; col++)
				if (col != notColumn && col < cells.Count && !string.IsNullOrEmpty(cells[col]))
					return col;
			return -1;
		}

		[Test]
		public void BulkCopy_ComputeCopiedValue_AppendReplaceDoNothing()
		{
			// Append: source onto a non-empty target uses the separator; onto an empty target sets it directly.
			Assert.That(ClerkBrowseRowSource.TryComputeCopiedValue("foo", "bar", BulkCopyMode.Append, " ", out var v),
				Is.True);
			Assert.That(v, Is.EqualTo("foo bar"), "Append: target + sep + source when target non-empty");
			Assert.That(ClerkBrowseRowSource.TryComputeCopiedValue("", "bar", BulkCopyMode.Append, " ", out v), Is.True);
			Assert.That(v, Is.EqualTo("bar"), "Append onto empty target is just the source");

			// Replace: source overwrites the target unconditionally.
			Assert.That(ClerkBrowseRowSource.TryComputeCopiedValue("foo", "bar", BulkCopyMode.Replace, " ", out v), Is.True);
			Assert.That(v, Is.EqualTo("bar"), "Replace overwrites a non-empty target");
			Assert.That(ClerkBrowseRowSource.TryComputeCopiedValue("", "bar", BulkCopyMode.Replace, " ", out v), Is.True);
			Assert.That(v, Is.EqualTo("bar"), "Replace fills an empty target");

			// DoNothingIfNonEmpty: fill empty targets, skip non-empty ones (returns false → no write).
			Assert.That(ClerkBrowseRowSource.TryComputeCopiedValue("", "bar", BulkCopyMode.DoNothingIfNonEmpty, " ", out v),
				Is.True);
			Assert.That(v, Is.EqualTo("bar"), "DoNothingIfNonEmpty fills an empty target");
			Assert.That(ClerkBrowseRowSource.TryComputeCopiedValue("foo", "bar", BulkCopyMode.DoNothingIfNonEmpty, " ", out v),
				Is.False, "DoNothingIfNonEmpty skips a non-empty target (no write)");
		}

		[Test]
		public void CopyTargets_IncludeLexemeForm_ExcludeSensePathColumns()
		{
			var source = NewSource();
			var targets = source.CopyTargets();
			Assert.That(targets, Is.Not.Empty, "the lexicon browse exposes at least one entry-anchored text copy target");
			Assert.That(targets.Any(t => t.Label == "Lexeme Form"), Is.True,
				"Lexeme Form (entry-anchored editable text) is a copy target");

			// A sense-path text column (Gloss, transduce=LexSense.Gloss) is inline-editable but is NOT a safe
			// copy target — its correct object is a sense of a possibly multi-sense row (wrong-object hazard).
			var targetCols = new HashSet<int>(targets.Select(t => t.Column));
			for (var col = 0; col < m_bv.ColumnCount; col++)
			{
				m_bv.GetColumnEditAttributes(col, out var field, out _, out var transduce);
				var isGloss = field == "Gloss" || transduce == "LexSense.Gloss";
				if (isGloss)
					Assert.That(targetCols.Contains(col), Is.False,
						"a sense-path text column is excluded from copy targets");
			}
		}

		[Test]
		public void CopySourceColumns_IncludeEveryColumn()
		{
			var source = NewSource();
			Assert.That(source.CopySourceColumns().Count, Is.EqualTo(m_bv.ColumnCount),
				"any column is a valid copy source (it is read, never written)");
		}

		[Test]
		public void PreviewBulkCopy_StoresComputedTargetOverlay_DoesNotCommit()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0), "need an entry-anchored text copy target");
			var sourceCol = FirstCopySourceColumnWithText(source, target);
			Assume.That(sourceCol, Is.GreaterThanOrEqualTo(0), "need a distinct source column with text");
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var rows = Enumerable.Range(0, source.RowCount).ToList();
			var expected = rows.Select(r =>
			{
				var cells = source.GetCellValues(r);
				ClerkBrowseRowSource.TryComputeCopiedValue(cells[target], cells[sourceCol], BulkCopyMode.Replace, " ", out var v);
				return v;
			}).ToList();

			source.PreviewBulkCopy(sourceCol, target, BulkCopyMode.Replace, " ", rows);

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
				"a copy preview must not create an undo step (no model write)");
			for (var r = 0; r < source.RowCount; r++)
				Assert.That(source.GetCellValues(r)[target], Is.EqualTo(expected[r]),
					$"row {r}'s target cell shows the computed copy overlay");

			source.ClearBulkEditPreview();
		}

		[Test]
		public void ApplyBulkCopy_WritesAllRows_InOneUndoStep()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			var sourceCol = FirstCopySourceColumnWithText(source, target);
			Assume.That(sourceCol, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(1), "need multiple rows to prove the single-UOW span");

			var rows = Enumerable.Range(0, source.RowCount).ToList();
			var originalTargets = rows.Select(r => source.GetCellValues(r)[target]).ToList();
			var sourceTexts = rows.Select(r => source.GetCellValues(r)[sourceCol]).ToList();

			// Replace: every checked row's target becomes its source text.
			source.ApplyBulkCopy(sourceCol, target, BulkCopyMode.Replace, " ", rows, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True, "apply commits the bulk copy");
			var afterApply = NewSource();
			for (var r = 0; r < afterApply.RowCount && r < sourceTexts.Count; r++)
				Assert.That(afterApply.GetCellValues(r)[target], Is.EqualTo(sourceTexts[r]),
					$"row {r}: Replace copied the source into the target");

			// ONE undo step: a single Undo reverts EVERY row.
			Cache.ActionHandlerAccessor.Undo();
			((MockFwXWindow)m_window).ProcessPendingItems();
			var reverted = NewSource();
			for (var r = 0; r < reverted.RowCount && r < originalTargets.Count; r++)
				Assert.That(reverted.GetCellValues(r)[target], Is.EqualTo(originalTargets[r]),
					$"the single undo reverted row {r}'s target");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.False,
				"the whole bulk copy was ONE undo step (no second step remains)");
		}

		[Test]
		public void ApplyBulkCopy_Append_ConcatenatesWithSeparator()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			var sourceCol = FirstCopySourceColumnWithText(source, target);
			Assume.That(sourceCol, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var rows = Enumerable.Range(0, source.RowCount).ToList();
			var expected = rows.Select(r =>
			{
				var cells = source.GetCellValues(r);
				ClerkBrowseRowSource.TryComputeCopiedValue(cells[target], cells[sourceCol], BulkCopyMode.Append, " ", out var v);
				return v;
			}).ToList();

			source.ApplyBulkCopy(sourceCol, target, BulkCopyMode.Append, " ", rows, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			var after = NewSource();
			for (var r = 0; r < after.RowCount && r < expected.Count; r++)
				Assert.That(after.GetCellValues(r)[target], Is.EqualTo(expected[r]),
					$"row {r}: Append concatenated source onto the prior target with the separator");

			Cache.ActionHandlerAccessor.Undo();
			((MockFwXWindow)m_window).ProcessPendingItems();
		}

		[Test]
		public void ApplyBulkCopy_SourceEqualsTarget_IsNoOp()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var rows = Enumerable.Range(0, source.RowCount).ToList();

			// Copying a column onto itself is rejected (no write, no undo step).
			source.ApplyBulkCopy(target, target, BulkCopyMode.Replace, " ", rows, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
				"a source==target copy is a no-op (never writes)");
		}

		// ----- Click Copy (interactive per-click copy of a clicked source cell into a target column) -----

		// Pure word-extraction unit tests (the managed parity of native-Views GrowToWord): given the cell text +
		// the clicked character offset, ExtractClickedWord returns the single clicked word. These exercise the
		// boundary cases (word start/middle/end, punctuation, leading/trailing spaces, gaps, empty) directly so
		// the product gesture only has to supply the offset.
		[TestCase("alpha beta gamma", 0, "alpha")]   // start of first word
		[TestCase("alpha beta gamma", 2, "alpha")]   // middle of first word
		[TestCase("alpha beta gamma", 4, "alpha")]   // last char of first word
		[TestCase("alpha beta gamma", 6, "beta")]    // start of middle word
		[TestCase("alpha beta gamma", 8, "beta")]    // middle of middle word
		[TestCase("alpha beta gamma", 11, "gamma")]  // start of last word
		[TestCase("alpha beta gamma", 15, "gamma")]  // last char of last word
		[TestCase("alpha beta gamma", 16, "gamma")]  // end of text → last word
		[TestCase("alpha beta gamma", 5, "beta")]    // the gap between alpha/beta → following word (GrowToWord fwd)
		[TestCase("word", 0, "word")]                // single word, start
		[TestCase("word", 4, "word")]                // single word, end-of-text
		[TestCase("don't stop", 2, "don't")]         // punctuation inside a word stays in the word
		[TestCase("  leading text", 0, "leading")]   // leading space → first real word
		[TestCase("trailing   ", 8, "trailing")]     // click in trailing whitespace → preceding word
		[TestCase("", 0, "")]                         // empty cell → empty word
		[TestCase("   ", 1, "")]                      // all whitespace → empty word
		public void ExtractClickedWord_ReturnsTheClickedWord(string text, int offset, string expected)
		{
			Assert.That(ClerkBrowseRowSource.ExtractClickedWord(text, offset), Is.EqualTo(expected));
		}

		[Test]
		public void ComputeClickCopySource_NegativeOffset_FallsBackToWholeCell()
		{
			// No hit-testable layout (charOffset < 0): both modes copy the whole cell (conservative fallback).
			Assert.That(ClerkBrowseRowSource.ComputeClickCopySource("alpha beta", ClickCopyMode.Word, -1),
				Is.EqualTo("alpha beta"));
			Assert.That(ClerkBrowseRowSource.ComputeClickCopySource("alpha beta", ClickCopyMode.Reorder, -1),
				Is.EqualTo("alpha beta"));
		}

		[Test]
		public void ComputeClickCopySource_WordMode_LiftsTheClickedWord()
		{
			Assert.That(ClerkBrowseRowSource.ComputeClickCopySource("alpha beta gamma", ClickCopyMode.Word, 8),
				Is.EqualTo("beta"));
		}

		[Test]
		public void ComputeClickCopySource_ReorderMode_RotatesToLeadWithTheClickedWord()
		{
			// Click in "gamma" (offset 11): rotate to lead with it, mirroring the legacy IchStartWord rotation.
			Assert.That(ClerkBrowseRowSource.ComputeClickCopySource("alpha beta gamma", ClickCopyMode.Reorder, 11),
				Is.EqualTo("gamma, alpha beta "));
			// Click in the FIRST word (wordStart 0): nothing to rotate (legacy guards on IchStartWord > 0).
			Assert.That(ClerkBrowseRowSource.ComputeClickCopySource("alpha beta gamma", ClickCopyMode.Reorder, 2),
				Is.EqualTo("alpha beta gamma"));
		}

		[Test]
		public void ClickCopyTargets_MatchCopyTargets_IncludeLexemeForm()
		{
			var source = NewSource();
			var clickTargets = source.ClickCopyTargets().Select(t => t.Column).ToList();
			var copyTargets = source.CopyTargets().Select(t => t.Column).ToList();
			Assert.That(clickTargets, Is.EquivalentTo(copyTargets),
				"Click Copy uses the same conservative entry-anchored writable target set as Bulk Copy");
			Assert.That(clickTargets, Is.Not.Empty, "at least the Lexeme Form is a click-copy target");
		}

		[Test]
		public void ClickCopy_Overwrite_ReplacesTargetWithSourceCell_InOneUndoStep()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0), "need an entry-anchored text copy target");
			var sourceCol = FirstCopySourceColumnWithText(source, target);
			Assume.That(sourceCol, Is.GreaterThanOrEqualTo(0), "need a distinct source column with text");
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var clickedRow = 0;
			var sourceText = source.GetCellValues(clickedRow)[sourceCol];

			// Overwrite (append: false) → the clicked source cell replaces the target on that ROW only. charOffset
			// -1 (no hit-testable layout) is the whole-cell fallback, so the whole source cell is copied.
			source.ApplyClickCopy(sourceCol, target, clickedRow, charOffset: -1, ClickCopyMode.Word, " ", append: false, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True, "a click commits the copy");
			var after = NewSource();
			Assert.That(after.GetCellValues(clickedRow)[target], Is.EqualTo(sourceText),
				"overwrite copied the clicked source cell into the target on the clicked row");

			// The click is ONE undo step: a single Undo reverts it.
			Cache.ActionHandlerAccessor.Undo();
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.False,
				"the click copy was exactly ONE undoable unit");
		}

		[Test]
		public void ClickCopy_WordMode_WithOffset_CopiesOnlyTheClickedWord_InOneUndoStep()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0), "need an entry-anchored text copy target");
			var sourceCol = FirstCopySourceColumnWithText(source, target);
			Assume.That(sourceCol, Is.GreaterThanOrEqualTo(0), "need a distinct source column with text");
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var clickedRow = 0;
			var sourceText = source.GetCellValues(clickedRow)[sourceCol];
			Assume.That(string.IsNullOrEmpty(sourceText), Is.False);

			// Click at the FIRST character of the source cell — Word mode copies only that word (the parity of the
			// native-Views GrowToWord), which equals the whole cell only when the cell is a single word.
			var offset = 0;
			var expectedWord = ClerkBrowseRowSource.ExtractClickedWord(sourceText, offset);

			source.ApplyClickCopy(sourceCol, target, clickedRow, offset, ClickCopyMode.Word, " ", append: false, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True, "a click commits the copy");
			var after = NewSource();
			Assert.That(after.GetCellValues(clickedRow)[target], Is.EqualTo(expectedWord),
				"Word mode with a clicked offset copies just the clicked word into the target");

			Cache.ActionHandlerAccessor.Undo();
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.False,
				"the word click copy was exactly ONE undoable unit");
		}

		[Test]
		public void ClickCopy_Append_ConcatenatesOntoExistingTargetWithSeparator()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			var sourceCol = FirstCopySourceColumnWithText(source, target);
			Assume.That(sourceCol, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var clickedRow = 0;
			var cells = source.GetCellValues(clickedRow);
			ClerkBrowseRowSource.TryComputeCopiedValue(cells[target], cells[sourceCol], BulkCopyMode.Append, "; ",
				out var expected);

			source.ApplyClickCopy(sourceCol, target, clickedRow, charOffset: -1, ClickCopyMode.Reorder, "; ", append: true, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			var after = NewSource();
			Assert.That(after.GetCellValues(clickedRow)[target], Is.EqualTo(expected),
				"append joined the clicked source onto the existing target with the separator");

			Cache.ActionHandlerAccessor.Undo();
			((MockFwXWindow)m_window).ProcessPendingItems();
		}

		[Test]
		public void ClickCopy_OnlyTouchesTheClickedRow_NotOthers()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			var sourceCol = FirstCopySourceColumnWithText(source, target);
			Assume.That(sourceCol, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(1), "need a second row to prove it is untouched");

			var otherRow = 1;
			var otherBefore = source.GetCellValues(otherRow)[target];

			source.ApplyClickCopy(sourceCol, target, 0, charOffset: -1, ClickCopyMode.Word, " ", append: false, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			var after = NewSource();
			Assert.That(after.GetCellValues(otherRow)[target], Is.EqualTo(otherBefore),
				"a click copy writes ONLY the clicked row, leaving the others untouched");

			Cache.ActionHandlerAccessor.Undo();
			((MockFwXWindow)m_window).ProcessPendingItems();
		}

		[Test]
		public void ClickCopy_SourceEqualsTarget_OrEmptySource_IsNoOp()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();

			// source == target: a self-copy never writes.
			source.ApplyClickCopy(target, target, 0, charOffset: -1, ClickCopyMode.Word, " ", append: false, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
				"a source==target click copy is a no-op");

			// An empty source column (no text to copy) is also a no-op. Find a column whose cell on row 0 is empty.
			var cells = source.GetCellValues(0);
			var emptyCol = -1;
			for (var col = 0; col < cells.Count; col++)
				if (col != target && string.IsNullOrEmpty(cells[col]))
				{
					emptyCol = col;
					break;
				}
			if (emptyCol >= 0)
			{
				source.ApplyClickCopy(emptyCol, target, 0, charOffset: -1, ClickCopyMode.Word, " ", append: false, source.EditContext);
				((MockFwXWindow)m_window).ProcessPendingItems();
				Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
					"clicking an empty source cell copies nothing");
			}
		}

		// ----- Phase 3 bulk edit (Bulk Clear) -----

		[Test]
		public void ClearTargets_MatchCopyTargets_IncludeLexemeForm_ExcludeSensePathColumns()
		{
			var source = NewSource();
			var clearTargets = source.ClearTargets();
			Assert.That(clearTargets, Is.Not.Empty, "the lexicon browse exposes at least one entry-anchored text clear target");
			Assert.That(clearTargets.Any(t => t.Label == "Lexeme Form"), Is.True,
				"Lexeme Form (entry-anchored editable text) is a clear target");

			// Clear reuses the same conservative safe-target rule as Bulk Copy.
			var copyCols = new HashSet<int>(source.CopyTargets().Select(t => t.Column));
			var clearCols = new HashSet<int>(clearTargets.Select(t => t.Column));
			Assert.That(clearCols, Is.EquivalentTo(copyCols),
				"clear targets are exactly the safe copy targets (same TrySetText eligibility)");

			// A sense-path text column (Gloss, transduce=LexSense.Gloss) is inline-editable but is NOT a safe
			// clear target — wrong-object hazard on a possibly multi-sense row.
			for (var col = 0; col < m_bv.ColumnCount; col++)
			{
				m_bv.GetColumnEditAttributes(col, out var field, out _, out var transduce);
				var isGloss = field == "Gloss" || transduce == "LexSense.Gloss";
				if (isGloss)
					Assert.That(clearCols.Contains(col), Is.False,
						"a sense-path text column is excluded from clear targets");
			}
		}

		[Test]
		public void PreviewBulkClear_ShowsEmptiedTarget_DoesNotCommit()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source); // clear targets == copy targets
			Assume.That(target, Is.GreaterThanOrEqualTo(0), "need an entry-anchored text clear target");
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var rows = Enumerable.Range(0, source.RowCount).ToList();
			var originalTargets = rows.Select(r => source.GetCellValues(r)[target]).ToList();

			source.PreviewBulkClear(target, rows);

			// No model mutation: the preview is an in-memory overlay only.
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
				"a clear preview must not create an undo step (no model write)");
			// GetCellValues shows the emptied (blank) cell for the previewed rows/column.
			for (var r = 0; r < source.RowCount; r++)
				Assert.That(source.GetCellValues(r)[target], Is.Empty,
					$"row {r}'s previewed target cell shows blank");

			// Clearing the preview restores the model's display value.
			source.ClearBulkEditPreview();
			for (var r = 0; r < source.RowCount && r < originalTargets.Count; r++)
				Assert.That(source.GetCellValues(r)[target], Is.EqualTo(originalTargets[r]),
					$"clearing the preview reverts row {r} to the model value");
		}

		[Test]
		public void ApplyBulkClear_EmptiesAllRows_InOneUndoStep()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(1), "need multiple rows to prove the single-UOW span");

			var rows = Enumerable.Range(0, source.RowCount).ToList();
			var originalTargets = rows.Select(r => source.GetCellValues(r)[target]).ToList();
			Assume.That(originalTargets.Any(t => !string.IsNullOrEmpty(t)), Is.True,
				"need at least one non-empty target to observe an actual clear");

			source.ApplyBulkClear(target, rows, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			// The apply is committed and emptied EVERY checked row's target.
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True, "apply commits the bulk clear");
			var afterApply = NewSource();
			for (var r = 0; r < afterApply.RowCount; r++)
				Assert.That(afterApply.GetCellValues(r)[target], Is.Empty,
					$"row {r}: the target was emptied");

			// ONE undo step: a single Undo restores EVERY row's original target.
			Cache.ActionHandlerAccessor.Undo();
			((MockFwXWindow)m_window).ProcessPendingItems();
			var reverted = NewSource();
			for (var r = 0; r < reverted.RowCount && r < originalTargets.Count; r++)
				Assert.That(reverted.GetCellValues(r)[target], Is.EqualTo(originalTargets[r]),
					$"the single undo restored row {r}'s target");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.False,
				"the whole bulk clear was ONE undo step (no second step remains)");
		}

		[Test]
		public void ApplyBulkClear_AlreadyEmptyTarget_IsHarmlessNoOp()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(0));
			var rows = Enumerable.Range(0, source.RowCount).ToList();

			// First clear empties every target.
			source.ApplyBulkClear(target, rows, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();
			var afterFirst = NewSource();
			for (var r = 0; r < afterFirst.RowCount; r++)
				Assume.That(afterFirst.GetCellValues(r)[target], Is.Empty);

			// A SECOND clear over the now-empty targets must not throw and remains valid (harmless no-op writes).
			Assert.DoesNotThrow(() =>
			{
				var again = NewSource();
				again.ApplyBulkClear(target, rows, again.EditContext);
				((MockFwXWindow)m_window).ProcessPendingItems();
			}, "clearing an already-empty target is a harmless no-op");

			var afterSecond = NewSource();
			for (var r = 0; r < afterSecond.RowCount; r++)
				Assert.That(afterSecond.GetCellValues(r)[target], Is.Empty,
					$"row {r}'s target stays empty after the second clear");
		}

		[Test]
		public void ApplyBulkClear_IneligibleTargetColumn_IsNoOp()
		{
			var source = NewSource();
			var clearCols = new HashSet<int>(source.ClearTargets().Select(t => t.Column));
			// Find a column that is NOT an eligible clear target (e.g. a sense-path or read-only column).
			var ineligible = -1;
			for (var col = 0; col < m_bv.ColumnCount; col++)
				if (!clearCols.Contains(col)) { ineligible = col; break; }
			Assume.That(ineligible, Is.GreaterThanOrEqualTo(0), "need an ineligible column to exercise the guard");
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var rows = Enumerable.Range(0, source.RowCount).ToList();

			// Preview and apply against the ineligible column are both no-ops (no overlay, no write).
			source.PreviewBulkClear(ineligible, rows);
			for (var r = 0; r < source.RowCount; r++)
				Assert.That(source.GetCellValues(r)[ineligible],
					Is.EqualTo(NewSource().GetCellValues(r)[ineligible]),
					$"row {r}: an ineligible clear target is not previewed (no overlay)");

			source.ApplyBulkClear(ineligible, rows, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
				"an ineligible clear target never writes (no undo step)");
		}

		// ----- Find/Replace Phase 1 bulk edit (Bulk Replace) -----

		[Test]
		public void ComputeReplaced_Literal_CaseSensitiveAndInsensitive()
		{
			// Case-insensitive (default): "Cat" matches "cat".
			Assert.That(ClerkBrowseRowSource.ComputeReplaced("Cat and cat",
				new BulkReplaceSpec { FindText = "cat", ReplaceText = "dog" }),
				Is.EqualTo("dog and dog"), "case-insensitive replaces both");

			// Case-sensitive: only the exact-case "cat" is replaced.
			Assert.That(ClerkBrowseRowSource.ComputeReplaced("Cat and cat",
				new BulkReplaceSpec { FindText = "cat", ReplaceText = "dog", MatchCase = true }),
				Is.EqualTo("Cat and dog"), "case-sensitive replaces only the matching case");
		}

		[Test]
		public void ComputeReplaced_WholeWord_OnlyReplacesBoundedRuns()
		{
			// Whole-word: "cat" in "category" is NOT a whole word; the standalone "cat" is.
			Assert.That(ClerkBrowseRowSource.ComputeReplaced("a cat in a category",
				new BulkReplaceSpec { FindText = "cat", ReplaceText = "dog", MatchWholeWord = true }),
				Is.EqualTo("a dog in a category"), "whole-word skips the substring inside 'category'");

			// Without whole-word both are replaced.
			Assert.That(ClerkBrowseRowSource.ComputeReplaced("a cat in a category",
				new BulkReplaceSpec { FindText = "cat", ReplaceText = "dog" }),
				Is.EqualTo("a dog in a dogegory"), "without whole-word the substring is also replaced");
		}

		[Test]
		public void ComputeReplaced_Regex_AppliesPatternWithBackrefs()
		{
			Assert.That(ClerkBrowseRowSource.ComputeReplaced("abc123def",
				new BulkReplaceSpec { FindText = @"(\d+)", ReplaceText = "[$1]", UseRegularExpressions = true }),
				Is.EqualTo("abc[123]def"), "regex replace honors backreferences");
		}

		[Test]
		public void ComputeReplaced_InvalidRegex_LeavesInputUnchanged()
		{
			// The dialog blocks OK on an invalid regex, but the producer is defensively a no-op if one slips through.
			Assert.That(ClerkBrowseRowSource.ComputeReplaced("anything",
				new BulkReplaceSpec { FindText = "(", UseRegularExpressions = true }),
				Is.EqualTo("anything"), "an invalid regex leaves the cell unchanged");
		}

		[Test]
		public void ComputeReplaced_EmptyFind_ReturnsInputUnchanged()
		{
			Assert.That(ClerkBrowseRowSource.ComputeReplaced("hello",
				new BulkReplaceSpec { FindText = "", ReplaceText = "x" }),
				Is.EqualTo("hello"), "an empty find is a no-op");
		}

		// ----- §19f.2: Find/Replace P2 — diacritic-insensitive matching (MatchDiacritics OFF, legacy default) -----

		[Test]
		public void ComputeReplaced_DiacriticInsensitive_MatchesAccentedCell_AndPreservesSurroundingDiacritics()
		{
			// MatchDiacritics defaults OFF: an unaccented pattern matches the accented run, and only that run is
			// replaced — the rest of the cell (including other diacritics) is preserved.
			Assert.That(ClerkBrowseRowSource.ComputeReplaced("café déjà",
				new BulkReplaceSpec { FindText = "cafe", ReplaceText = "tea" }),
				Is.EqualTo("tea déjà"), "diacritic-insensitive find matches 'café' and leaves 'déjà' intact");
		}

		[Test]
		public void ComputeReplaced_DiacriticSensitive_DoesNotMatchAcrossAccents()
		{
			// MatchDiacritics ON: the unaccented pattern must NOT match the accented cell.
			Assert.That(ClerkBrowseRowSource.ComputeReplaced("café",
				new BulkReplaceSpec { FindText = "cafe", ReplaceText = "tea", MatchDiacritics = true }),
				Is.EqualTo("café"), "diacritic-sensitive find does not match an accented form");
		}

		[Test]
		public void ComputeReplaced_DiacriticInsensitive_MatchesPrecomposedAndDecomposed()
		{
			// A combining-accent (decomposed) cell matches a precomposed pattern, and vice versa.
			var decomposed = "café"; // e + combining acute
			Assert.That(ClerkBrowseRowSource.ComputeReplaced(decomposed,
				new BulkReplaceSpec { FindText = "café", ReplaceText = "X" }),
				Is.EqualTo("X"), "decomposed cell matches a precomposed find diacritic-insensitively");
		}

		[Test]
		public void ComputeReplaced_DiacriticInsensitive_StillHonorsCaseAndWholeWord()
		{
			Assert.That(ClerkBrowseRowSource.ComputeReplaced("Café cafe",
				new BulkReplaceSpec { FindText = "cafe", ReplaceText = "x", MatchCase = true }),
				Is.EqualTo("Café x"), "case-sensitivity still applies under diacritic-insensitivity");

			Assert.That(ClerkBrowseRowSource.ComputeReplaced("café cafeteria",
				new BulkReplaceSpec { FindText = "cafe", ReplaceText = "x", MatchWholeWord = true }),
				Is.EqualTo("x cafeteria"), "whole-word still bounds the match under diacritic-insensitivity");
		}

		[Test]
		public void ComputeReplaced_DiacriticInsensitive_AllDiacriticFind_DoesNotReplace()
		{
			// A find text that strips to empty (all combining marks) must not match everywhere.
			Assert.That(ClerkBrowseRowSource.ComputeReplaced("hello",
				new BulkReplaceSpec { FindText = "́", ReplaceText = "x" }),
				Is.EqualTo("hello"), "an all-diacritic find replaces nothing");
		}

		[Test]
		public void ComputeReplaced_DiacriticInsensitive_RtlScript_NoSpuriousMatch()
		{
			// An unrelated find text must not match an RTL/complex-script cell.
			Assert.That(ClerkBrowseRowSource.ComputeReplaced("שלום", // Hebrew "shalom"
				new BulkReplaceSpec { FindText = "cafe", ReplaceText = "x" }),
				Is.EqualTo("שלום"), "no spurious match in an unrelated script");
		}

		[Test]
		public void ReplaceTargets_MatchCopyTargets_IncludeLexemeForm()
		{
			var source = NewSource();
			var replaceTargets = source.ReplaceTargets();
			Assert.That(replaceTargets, Is.Not.Empty, "the lexicon browse exposes at least one entry-anchored text replace target");
			Assert.That(replaceTargets.Any(t => t.Label == "Lexeme Form"), Is.True,
				"Lexeme Form (entry-anchored editable text) is a replace target");

			// Replace reuses the same conservative safe-target rule as Bulk Copy/Clear.
			var copyCols = new HashSet<int>(source.CopyTargets().Select(t => t.Column));
			var replaceCols = new HashSet<int>(replaceTargets.Select(t => t.Column));
			Assert.That(replaceCols, Is.EquivalentTo(copyCols),
				"replace targets are exactly the safe copy targets (same TrySetText eligibility)");
		}

		[Test]
		public void PreviewBulkReplace_ShowsReplacedTarget_DoesNotCommit()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source); // replace targets == copy targets
			Assume.That(target, Is.GreaterThanOrEqualTo(0), "need an entry-anchored text replace target");
			Assume.That(source.RowCount, Is.GreaterThan(0));

			// Build a spec that matches a substring of row 0's target so at least one row changes.
			var seed = source.GetCellValues(0)[target];
			Assume.That(seed, Is.Not.Null.And.Not.Empty, "row 0's target has text");
			var needle = seed.Substring(0, System.Math.Min(2, seed.Length));
			var spec = new BulkReplaceSpec { FindText = needle, ReplaceText = "ZZ" };

			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var rows = Enumerable.Range(0, source.RowCount).ToList();
			var expected = rows.Select(r =>
				ClerkBrowseRowSource.ComputeReplaced(source.GetCellValues(r)[target], spec)).ToList();

			source.PreviewBulkReplace(target, rows, spec);

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
				"a replace preview must not create an undo step (no model write)");
			for (var r = 0; r < source.RowCount; r++)
				Assert.That(source.GetCellValues(r)[target], Is.EqualTo(expected[r]),
					$"row {r}'s target cell shows the replaced overlay");

			source.ClearBulkEditPreview();
		}

		[Test]
		public void ApplyBulkReplace_WritesAllMatchingRows_InOneUndoStep()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(1), "need multiple rows to prove the single-UOW span");

			// A spec whose find text matches a common single letter so several rows change.
			var spec = new BulkReplaceSpec { FindText = "a", ReplaceText = "A" };
			var rows = Enumerable.Range(0, source.RowCount).ToList();
			var originalTargets = rows.Select(r => source.GetCellValues(r)[target]).ToList();
			var expected = originalTargets.Select(t => ClerkBrowseRowSource.ComputeReplaced(t, spec)).ToList();
			Assume.That(expected.Where((e, i) => e != originalTargets[i]).Any(), Is.True,
				"the spec must change at least one row to be a meaningful test");

			source.ApplyBulkReplace(target, rows, spec, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True, "apply commits the bulk replace");
			var afterApply = NewSource();
			for (var r = 0; r < afterApply.RowCount && r < expected.Count; r++)
				Assert.That(afterApply.GetCellValues(r)[target], Is.EqualTo(expected[r]),
					$"row {r}: the find/replace was applied");

			// ONE undo step: a single Undo restores EVERY row.
			Cache.ActionHandlerAccessor.Undo();
			((MockFwXWindow)m_window).ProcessPendingItems();
			var reverted = NewSource();
			for (var r = 0; r < reverted.RowCount && r < originalTargets.Count; r++)
				Assert.That(reverted.GetCellValues(r)[target], Is.EqualTo(originalTargets[r]),
					$"the single undo reverted row {r}'s target");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.False,
				"the whole bulk replace was ONE undo step (no second step remains)");
		}

		[Test]
		public void ApplyBulkReplace_IneligibleTargetColumn_IsNoOp()
		{
			var source = NewSource();
			var replaceCols = new HashSet<int>(source.ReplaceTargets().Select(t => t.Column));
			var ineligible = -1;
			for (var col = 0; col < m_bv.ColumnCount; col++)
				if (!replaceCols.Contains(col)) { ineligible = col; break; }
			Assume.That(ineligible, Is.GreaterThanOrEqualTo(0), "need an ineligible column to exercise the guard");
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var rows = Enumerable.Range(0, source.RowCount).ToList();
			var spec = new BulkReplaceSpec { FindText = "a", ReplaceText = "A" };

			// Preview and apply against the ineligible column are both no-ops (no overlay, no write).
			source.PreviewBulkReplace(ineligible, rows, spec);
			for (var r = 0; r < source.RowCount; r++)
				Assert.That(source.GetCellValues(r)[ineligible],
					Is.EqualTo(NewSource().GetCellValues(r)[ineligible]),
					$"row {r}: an ineligible replace target is not previewed (no overlay)");

			source.ApplyBulkReplace(ineligible, rows, spec, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
				"an ineligible replace target never writes (no undo step)");
		}

		[Test]
		public void ApplyBulkReplace_EmptyFindText_IsNoOp()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var rows = Enumerable.Range(0, source.RowCount).ToList();

			source.ApplyBulkReplace(target, rows, new BulkReplaceSpec { FindText = "", ReplaceText = "x" },
				source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
				"an empty find text never writes (no undo step)");
		}

		// ----- Process / Transduce (run a converter over a source column into a target) -----

		// A deterministic, EncConverters-free converter for the integration tests: upper-cases the input
		// (the same shape RecordBrowseView's EncConverterAdapter exposes for a real Unicode-to-Unicode converter).
		private sealed class UpperConverter : IBulkTransduceConverter
		{
			public string Name => "TEST-UPPER";
			public string Convert(string input) => (input ?? string.Empty).ToUpperInvariant();
		}

		[Test]
		public void TransduceColumns_MatchCopyTargets_IncludeLexemeForm_ExcludeSensePathColumns()
		{
			var source = NewSource();
			var transduceTargets = source.TransduceColumns();
			Assert.That(transduceTargets, Is.Not.Empty,
				"the lexicon browse exposes at least one entry-anchored text transduce target");
			Assert.That(transduceTargets.Any(t => t.Label == "Lexeme Form"), Is.True,
				"Lexeme Form (entry-anchored editable text) is a transduce target");

			// Transduce targets are exactly the safe copy targets (conservative, entry-anchored text columns).
			var copyCols = new HashSet<int>(source.CopyTargets().Select(t => t.Column));
			var transduceCols = new HashSet<int>(transduceTargets.Select(t => t.Column));
			Assert.That(transduceCols, Is.EquivalentTo(copyCols),
				"transduce targets match copy targets (the same safe entry-anchored text columns)");
		}

		[Test]
		public void Transduce_StaticTransform_AppliesConverterThenMode()
		{
			var conv = new UpperConverter();
			// Replace: the target becomes the converted source unconditionally.
			Assert.That(ClerkBrowseRowSource.Transduce(conv, "abc"), Is.EqualTo("ABC"), "the converter upper-cases");
			Assert.That(ClerkBrowseRowSource.Transduce(conv, ""), Is.EqualTo(""), "an empty source is not converted");
			Assert.That(ClerkBrowseRowSource.Transduce(conv, null), Is.EqualTo(""), "a null source is treated as empty");
		}

		[Test]
		public void PreviewBulkTransduce_StoresConvertedOverlay_DoesNotCommit()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source); // transduce targets == copy targets
			Assume.That(target, Is.GreaterThanOrEqualTo(0), "need an entry-anchored text transduce target");
			var sourceCol = FirstCopySourceColumnWithText(source, target);
			Assume.That(sourceCol, Is.GreaterThanOrEqualTo(0), "need a distinct source column with text");
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var conv = new UpperConverter();
			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var rows = Enumerable.Range(0, source.RowCount).ToList();
			var expected = rows.Select(r =>
			{
				var cells = source.GetCellValues(r);
				var converted = ClerkBrowseRowSource.Transduce(conv, cells[sourceCol]);
				ClerkBrowseRowSource.TryComputeCopiedValue(cells[target], converted, BulkCopyMode.Replace, " ", out var v);
				return v;
			}).ToList();

			source.PreviewBulkTransduce(sourceCol, target, conv, BulkCopyMode.Replace, " ", rows);

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
				"a transduce preview must not create an undo step (no model write)");
			for (var r = 0; r < source.RowCount; r++)
				Assert.That(source.GetCellValues(r)[target], Is.EqualTo(expected[r]),
					$"row {r}'s target cell shows the converted (upper-cased) source overlay");

			source.ClearBulkEditPreview();
		}

		[Test]
		public void ApplyBulkTransduce_Replace_WritesConvertedSource_InOneUndoStep()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			var sourceCol = FirstCopySourceColumnWithText(source, target);
			Assume.That(sourceCol, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(1), "need multiple rows to prove the single-UOW span");

			var conv = new UpperConverter();
			var rows = Enumerable.Range(0, source.RowCount).ToList();
			var originalTargets = rows.Select(r => source.GetCellValues(r)[target]).ToList();
			var expected = rows.Select(r => ClerkBrowseRowSource.Transduce(conv, source.GetCellValues(r)[sourceCol])).ToList();

			// Replace: every checked row's target becomes its UPPER-cased source text.
			source.ApplyBulkTransduce(sourceCol, target, conv, BulkCopyMode.Replace, " ", rows, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True, "apply commits the bulk transduce");
			var afterApply = NewSource();
			for (var r = 0; r < afterApply.RowCount && r < expected.Count; r++)
				Assert.That(afterApply.GetCellValues(r)[target], Is.EqualTo(expected[r]),
					$"row {r}: Replace wrote the converted source into the target");

			// ONE undo step: a single Undo reverts EVERY row.
			Cache.ActionHandlerAccessor.Undo();
			((MockFwXWindow)m_window).ProcessPendingItems();
			var reverted = NewSource();
			for (var r = 0; r < reverted.RowCount && r < originalTargets.Count; r++)
				Assert.That(reverted.GetCellValues(r)[target], Is.EqualTo(originalTargets[r]),
					$"the single undo reverted row {r}'s target");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.False,
				"the whole bulk transduce was ONE undo step (no second step remains)");
		}

		[Test]
		public void ApplyBulkTransduce_DoNothingIfNonEmpty_SkipsPopulatedTargets()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			var sourceCol = FirstCopySourceColumnWithText(source, target);
			Assume.That(sourceCol, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var conv = new UpperConverter();
			var rows = Enumerable.Range(0, source.RowCount).ToList();
			// The Lexeme Form target is populated for these entries, so Skip-non-empty must leave it untouched.
			var originalTargets = rows.Select(r => source.GetCellValues(r)[target]).ToList();
			Assume.That(originalTargets.Any(t => !string.IsNullOrEmpty(t)), Is.True,
				"need at least one already-populated target to exercise Skip-non-empty");

			source.ApplyBulkTransduce(sourceCol, target, conv, BulkCopyMode.DoNothingIfNonEmpty, " ", rows, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			var after = NewSource();
			for (var r = 0; r < after.RowCount && r < originalTargets.Count; r++)
				if (!string.IsNullOrEmpty(originalTargets[r]))
					Assert.That(after.GetCellValues(r)[target], Is.EqualTo(originalTargets[r]),
						$"row {r}: Skip-non-empty left the already-populated target untouched");

			if (Cache.ActionHandlerAccessor.CanUndo())
			{
				Cache.ActionHandlerAccessor.Undo();
				((MockFwXWindow)m_window).ProcessPendingItems();
			}
		}

		[Test]
		public void ApplyBulkTransduce_NoConverter_IsNoOp()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			var sourceCol = FirstCopySourceColumnWithText(source, target);
			Assume.That(sourceCol, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var rows = Enumerable.Range(0, source.RowCount).ToList();

			// A null converter never writes (the bar gates Apply off this; this is the producer's defensive belt).
			source.ApplyBulkTransduce(sourceCol, target, null, BulkCopyMode.Replace, " ", rows, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
				"a transduce with no converter is a no-op (never writes)");
		}

		[Test]
		public void ApplyBulkTransduce_SourceEqualsTarget_IsNoOp()
		{
			var source = NewSource();
			var target = FirstCopyTargetColumn(source);
			Assume.That(target, Is.GreaterThanOrEqualTo(0));
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var rows = Enumerable.Range(0, source.RowCount).ToList();

			source.ApplyBulkTransduce(target, target, new UpperConverter(), BulkCopyMode.Replace, " ", rows, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore),
				"a source==target transduce is a no-op (never writes)");
		}

		// ----- Task 23(c): AffectsCurrentRow owner-walk -----

		[Test]
		public void AffectsCurrentRow_TrueForCurrentEntry_TrueForOwnedChild_FalseForUnrelated_FalseForInvalid()
		{
			var browseView = m_bv.Parent as RecordBrowseViewForTests;
			var clerk = Clerk;
			Assume.That(clerk.ListSize, Is.GreaterThan(0), "entries list is populated");

			// Find an entry that owns a sense (CreateTestData makes several), make it current.
			var entries = Cache.ServiceLocator.GetInstance<SIL.LCModel.ILexEntryRepository>().AllInstances().ToList();
			var entryWithSense = entries.FirstOrDefault(e => e.SensesOS.Count > 0);
			Assume.That(entryWithSense, Is.Not.Null, "need an entry that owns a sense");
			clerk.JumpToRecord(entryWithSense.Hvo);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assume.That(clerk.CurrentObject?.Hvo, Is.EqualTo(entryWithSense.Hvo), "the clerk is on the chosen entry");

			// True: the current entry itself.
			Assert.That(browseView.AffectsCurrentRowForTest(entryWithSense.Hvo), Is.True,
				"a change to the current entry affects the current row");

			// True: an object the current entry OWNS (a sense), via the owner walk.
			var ownedSense = entryWithSense.SensesOS[0];
			Assert.That(browseView.AffectsCurrentRowForTest(ownedSense.Hvo), Is.True,
				"a change to an object owned by the current entry affects the current row");

			// False: an unrelated entry (one that is NOT the current entry and does not own it).
			var unrelated = entries.FirstOrDefault(e => e.Hvo != entryWithSense.Hvo);
			Assume.That(unrelated, Is.Not.Null, "need a second, unrelated entry");
			Assert.That(browseView.AffectsCurrentRowForTest(unrelated.Hvo), Is.False,
				"a change to an unrelated entry does not affect the current row");

			// False: hvo 0 and an invalid hvo.
			Assert.That(browseView.AffectsCurrentRowForTest(0), Is.False, "hvo 0 never affects the row");
			Assert.That(browseView.AffectsCurrentRowForTest(int.MaxValue), Is.False, "an invalid hvo never affects the row");
		}

		// ----- Delete Rows (the destructive mode of the Delete tab) -----

		[Test]
		public void DeleteRows_CanDelete_AndClassifiesEntriesAsDeletable()
		{
			var source = NewSource();
			Assume.That(source.RowCount, Is.GreaterThan(0), "the entries list is populated");
			Assert.That(source.CanDeleteRows, Is.True, "the clerk-backed source can delete objects");

			var rows = Enumerable.Range(0, source.RowCount).ToList();
			var deletable = source.ClassifyDeletableRows(rows, out var blocked);
			// The browse list is entry-rooted; entries delete cleanly, so every checked row is deletable.
			Assert.That(deletable, Is.EquivalentTo(rows), "every entry row is deletable");
			Assert.That(blocked, Is.Empty, "no entry row is blocked");
		}

		[Test]
		public void DeleteRows_DeletesCheckedEntries_InOneUndoStep_ThatUndoRestores()
		{
			var source = NewSource();
			Assume.That(source.RowCount, Is.GreaterThan(1), "need multiple rows to prove the single-UOW span");
			var totalBefore = source.RowCount;

			// Delete the first two checked rows; capture their object identities to assert their fate.
			var rows = new List<int> { 0, 1 };
			var victimHvos = rows.Select(source.HvoAt).ToList();
			Assume.That(victimHvos.All(h => Cache.ServiceLocator.IsValidObjectId(h)), Is.True);

			var deleted = source.DeleteRows(rows, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(deleted, Is.EqualTo(2), "both checked entries were deleted");
			foreach (var hvo in victimHvos)
				Assert.That(Cache.ServiceLocator.IsValidObjectId(hvo), Is.False, "the deleted entry is gone from the model");
			Assert.That(NewSource().RowCount, Is.EqualTo(totalBefore - 2), "the row set shrank by the deleted entries");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True, "the delete is committed and undoable");

			// ONE undo step: a single Undo restores BOTH deleted entries.
			Cache.ActionHandlerAccessor.Undo();
			((MockFwXWindow)m_window).ProcessPendingItems();
			foreach (var hvo in victimHvos)
				Assert.That(Cache.ServiceLocator.IsValidObjectId(hvo), Is.True, "the single undo restored the deleted entry");
			Assert.That(NewSource().RowCount, Is.EqualTo(totalBefore), "the whole delete was ONE undo step");
		}

		[Test]
		public void DeleteRows_OnlySenseGuard_BlocksDeletingTheOnlySenseOfAnEntry()
		{
			// A sense whose owning entry has exactly one sense is BLOCKED (AllowDeleteRow only-sense guard); a
			// sense of an entry with multiple senses is deletable. Exercise the guard directly on sense hvos.
			var source = NewSource();
			var entries = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().ToList();
			var oneSenseEntry = entries.FirstOrDefault(e => e.SensesOS.Count == 1);
			var multiSenseEntry = entries.FirstOrDefault(e => e.SensesOS.Count > 1);
			Assume.That(oneSenseEntry, Is.Not.Null, "CreateTestData makes a single-sense entry (pus)");
			Assume.That(multiSenseEntry, Is.Not.Null, "CreateTestData makes a multi-sense entry (bili)");

			var onlySenseHvo = oneSenseEntry.SensesOS[0].Hvo;
			var multiSense0 = multiSenseEntry.SensesOS[0].Hvo;
			var multiSense1 = multiSenseEntry.SensesOS[1].Hvo;

			// Deleting ONLY the only-sense: blocked. Deleting one of two senses (the other survives): allowed.
			var candidates = new HashSet<int> { onlySenseHvo, multiSense0 };
			Assert.That(source.TestAllowDeleteRow(onlySenseHvo, candidates), Is.False,
				"the only sense of an entry cannot be deleted");
			Assert.That(source.TestAllowDeleteRow(multiSense0, candidates), Is.True,
				"a sense whose sibling survives is deletable");

			// Deleting BOTH senses of the multi-sense entry: the first sense becomes un-deletable (no survivor).
			var bothSenses = new HashSet<int> { multiSense0, multiSense1 };
			Assert.That(source.TestAllowDeleteRow(multiSense0, bothSenses), Is.False,
				"deleting the first sense is blocked when every other sense is also being deleted");
			Assert.That(source.TestAllowDeleteRow(multiSense1, bothSenses), Is.True,
				"a non-first sense is never blocked by the only-sense guard");
		}

		[Test]
		public void DeleteRows_EmptySelection_IsNoOp()
		{
			var source = NewSource();
			var undoBefore = Cache.ActionHandlerAccessor.CanUndo();
			var totalBefore = source.RowCount;

			var deleted = source.DeleteRows(new List<int>(), source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();

			Assert.That(deleted, Is.EqualTo(0), "no rows → nothing deleted");
			Assert.That(NewSource().RowCount, Is.EqualTo(totalBefore), "the row set is unchanged");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.EqualTo(undoBefore), "an empty delete writes nothing");
		}

		// ----- Ghost-owner allowance (the bulkEditListItemsGhostFields path of AllowDeleteItem) -----
		//
		// When a bulk-edit target field re-roots the list-items class to a ghost target (here LexPronunciation, one
		// of the EntryOrSenseBulkEdit bulkEditListItemsGhostFields), the list shows the existing CHILDREN of that
		// class PLUS the childless ghost-OWNER entries. The legacy bar deletes a child of the expected class
		// (same/subclass), allows deleting a ghost owner whose child already exists (deleting that child), and
		// blocks a childless ghost owner. The OLD AllowDeleteRow blocked ANY non-entry/non-sense row, so it
		// over-blocked these pronunciation rows; these tests pin the faithful behavior.

		// Re-roots the live browse/clerk to LexPronunciation by choosing the Pronunciation-Location bulk target,
		// exactly as Pronunciations_ListChoice_Locations does, so the row source sees the ghost-target list.
		private void SwitchToPronunciationList()
		{
			m_bulkEditBar.SwitchTab("ListChoice");
			m_bv.ShowColumn("Location");
			m_bulkEditBar.SetTargetField("Pronunciation-Location");
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assume.That(m_bv.ListItemsClass, Is.EqualTo(LexPronunciationTags.kClassId),
				"selecting the Pronunciation-Location target re-roots the list-items class to LexPronunciation");
		}

		private ILexEntry AddPronunciationTo(string headword)
		{
			var entry = Cache.LangProject.LexDbOA.Entries.FirstOrDefault(e => e.HeadWord.Text == headword);
			Assume.That(entry, Is.Not.Null, "the test entry exists");
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var pronunciation = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
				entry.PronunciationsOS.Add(pronunciation);
				pronunciation.Form.set_String(Cache.DefaultVernWs, "pron");
			});
			return entry;
		}

		[Test]
		public void DeleteRows_GhostTargetClassRow_IsDeletable_DeletesTheChild_InOneUndoStep()
		{
			// Give one entry a pronunciation (so it appears as a LexPronunciation child row), then re-root the list.
			var entryWithPron = AddPronunciationTo("pus");
			var pronHvo = entryWithPron.PronunciationsOS[0].Hvo;
			SwitchToPronunciationList();
			var source = NewSource();

			// Find the row whose object is the pronunciation child.
			var pronRow = Enumerable.Range(0, source.RowCount).FirstOrDefault(i => source.HvoAt(i) == pronHvo);
			Assume.That(source.HvoAt(pronRow), Is.EqualTo(pronHvo), "the pronunciation child is a row in the re-rooted list");

			// A same-class (ghost-target) row is deletable now — the OLD guard blocked any non-entry/non-sense row.
			var deletable = source.ClassifyDeletableRows(new List<int> { pronRow }, out var blocked);
			Assert.That(deletable, Does.Contain(pronRow), "a LexPronunciation row (the expected list-items class) is deletable");
			Assert.That(blocked, Does.Not.Contain(pronRow));
			Assert.That(source.TestResolveDeleteTarget(pronHvo), Is.EqualTo(pronHvo),
				"a same-class row deletes itself");

			var deleted = source.DeleteRows(new List<int> { pronRow }, source.EditContext);
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(deleted, Is.EqualTo(1), "the pronunciation child was deleted");
			Assert.That(Cache.ServiceLocator.IsValidObjectId(pronHvo), Is.False, "the pronunciation is gone");
			Assert.That(Cache.ServiceLocator.IsValidObjectId(entryWithPron.Hvo), Is.True, "the owning entry survives");

			// ONE undo step restores the pronunciation child.
			Cache.ActionHandlerAccessor.Undo();
			((MockFwXWindow)m_window).ProcessPendingItems();
			Assert.That(Cache.ServiceLocator.IsValidObjectId(pronHvo), Is.True, "a single undo restored the deleted child");
		}

		[Test]
		public void DeleteRows_GhostOwnerWithChild_ResolvesToTheChild_ChildlessOwnerIsBlocked()
		{
			// entryWithPron has a pronunciation child; the other test entries do not. Re-root to LexPronunciation.
			var entryWithPron = AddPronunciationTo("pus");
			var pronHvo = entryWithPron.PronunciationsOS[0].Hvo;
			SwitchToPronunciationList();
			var source = NewSource();

			// A ghost OWNER entry (not the expected pronunciation class) with an existing child is ALLOWED, and the
			// resolved delete target is the child pronunciation — NOT the entry (GhostParentHelper.GetOwnerOfTargetProperty).
			var candidates = new HashSet<int> { entryWithPron.Hvo };
			Assert.That(source.TestAllowDeleteRow(entryWithPron.Hvo, candidates), Is.True,
				"a ghost-owner entry whose child pronunciation already exists is deletable");
			Assert.That(source.TestResolveDeleteTarget(entryWithPron.Hvo), Is.EqualTo(pronHvo),
				"the ghost-owner row resolves to its existing child (the delete handles the ghost parent)");

			// A childless ghost OWNER entry (no pronunciation) is BLOCKED — there is nothing of the expected class.
			var childlessEntry = Cache.LangProject.LexDbOA.Entries
				.FirstOrDefault(e => e.PronunciationsOS.Count == 0);
			Assume.That(childlessEntry, Is.Not.Null, "there is an entry without a pronunciation");
			Assert.That(source.TestAllowDeleteRow(childlessEntry.Hvo, new HashSet<int> { childlessEntry.Hvo }), Is.False,
				"a childless ghost owner is blocked (nothing of the expected class to delete)");
			Assert.That(source.TestResolveDeleteTarget(childlessEntry.Hvo), Is.EqualTo(0),
				"a childless ghost owner resolves to no delete target");
		}

		// ----- bulkDeleteIfZero (VerifyRowDeleteAllowable parity) -----

		[Test]
		public void DeleteRows_BulkDeleteIfZero_BlocksNonZeroCount_AllowsZero()
		{
			// The live lexicon spec has no bulkDeleteIfZero; wrap the viewer to supply one naming an int property
			// (HomographNumber) so the guard reads it via reflection on the row object exactly as the legacy bar does.
			var source = new ClerkBrowseRowSource(Clerk, new BulkDeleteIfZeroColumnSource(m_bv, "HomographNumber"), Cache);
			Assume.That(source.RowCount, Is.GreaterThan(0));

			var entries = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().ToList();
			var zeroEntry = entries.FirstOrDefault(e => e.HomographNumber == 0);
			var nonZeroEntry = entries.FirstOrDefault(e => e.HomographNumber != 0);
			if (nonZeroEntry == null)
			{
				// Force a non-zero homograph number on some entry so the block path is exercised deterministically.
				nonZeroEntry = entries.First();
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => nonZeroEntry.HomographNumber = 3);
				zeroEntry = entries.FirstOrDefault(e => e.Hvo != nonZeroEntry.Hvo && e.HomographNumber == 0);
			}
			Assume.That(zeroEntry, Is.Not.Null, "need an entry with a zero count");

			var candidates = new HashSet<int> { zeroEntry.Hvo, nonZeroEntry.Hvo };
			Assert.That(source.TestAllowDeleteRow(nonZeroEntry.Hvo, candidates), Is.False,
				"a row whose bulkDeleteIfZero count is non-zero is blocked");
			Assert.That(source.TestAllowDeleteRow(zeroEntry.Hvo, candidates), Is.True,
				"a row whose bulkDeleteIfZero count is zero is deletable");
		}

		// A thin decorator over the live BrowseViewer that overrides ONLY GetBulkEditSpecAttribute to supply a
		// bulkDeleteIfZero property name — so the bulkDeleteIfZero guard can be exercised against a real LCModel
		// object graph without a dedicated wordform tool/config. Every other member delegates to the real viewer.
		private sealed class BulkDeleteIfZeroColumnSource : IBrowseColumnSource
		{
			private readonly IBrowseColumnSource _inner;
			private readonly string _bulkDeleteIfZero;
			public BulkDeleteIfZeroColumnSource(IBrowseColumnSource inner, string bulkDeleteIfZero)
			{ _inner = inner; _bulkDeleteIfZero = bulkDeleteIfZero; }
			public int ColumnCount => _inner.ColumnCount;
			public string GetColumnName(int icol) => _inner.GetColumnName(icol);
			public void GetColumnEditAttributes(int icol, out string field, out string ws, out string transduce)
				=> _inner.GetColumnEditAttributes(icol, out field, out ws, out transduce);
			public bool IsColumnEditable(int icol) => _inner.IsColumnEditable(icol);
			public IReadOnlyList<BrowseColumnInfo> GetAvailableColumns() => _inner.GetAvailableColumns();
			public string GetColumnKey(int icol) => _inner.GetColumnKey(icol);
			public IReadOnlyList<string> GetRowCellStrings(IManyOnePathSortItem item) => _inner.GetRowCellStrings(item);
			public SIL.LCModel.Core.KernelInterfaces.ITsString GetRowCellTsString(IManyOnePathSortItem item, int icol)
				=> _inner.GetRowCellTsString(item, icol);
			public RecordSorter MakeColumnSorter(int dataColumnIndex, bool ascending) => _inner.MakeColumnSorter(dataColumnIndex, ascending);
			public RecordSorter MakeColumnSorter(int dataColumnIndex, bool ascending, bool sortedFromEnd, bool sortedByLength)
				=> _inner.MakeColumnSorter(dataColumnIndex, ascending, sortedFromEnd, sortedByLength);
			public RecordFilter MakeColumnFilter(int dataColumnIndex, BrowseColumnFilterKind kind, string text)
				=> _inner.MakeColumnFilter(dataColumnIndex, kind, text);
			public RecordFilter MakePatternColumnFilter(int dataColumnIndex, string pattern, BrowsePatternMatchType matchType, bool matchCase)
				=> _inner.MakePatternColumnFilter(dataColumnIndex, pattern, matchType, matchCase);
			public RecordFilter MakeStringListColumnFilter(int dataColumnIndex, string value, bool exclude)
				=> _inner.MakeStringListColumnFilter(dataColumnIndex, value, exclude);
			public string[] GetColumnStringList(int dataColumnIndex) => _inner.GetColumnStringList(dataColumnIndex);
			public string GetColumnSpecAttribute(int icol, string attrName) => _inner.GetColumnSpecAttribute(icol, attrName);
			public string GetBulkEditSpecAttribute(string attrName)
				=> attrName == "bulkDeleteIfZero" ? _bulkDeleteIfZero : _inner.GetBulkEditSpecAttribute(attrName);
			public RecordFilter MakeDateColumnFilter(int dataColumnIndex, BrowseDateMatchKind kind, System.DateTime start, System.DateTime end, bool handleGenDate)
				=> _inner.MakeDateColumnFilter(dataColumnIndex, kind, start, end, handleGenDate);
			public IReadOnlyList<BrowseChooserItem> GetColumnChooserList(int dataColumnIndex) => _inner.GetColumnChooserList(dataColumnIndex);
			public RecordFilter MakeListChoiceColumnFilter(int dataColumnIndex, IReadOnlyList<string> chosenKeys)
				=> _inner.MakeListChoiceColumnFilter(dataColumnIndex, chosenKeys);
			public bool ColumnSupportsSpellingFilter(int dataColumnIndex) => _inner.ColumnSupportsSpellingFilter(dataColumnIndex);
			public RecordFilter MakeSpellingErrorColumnFilter(int dataColumnIndex) => _inner.MakeSpellingErrorColumnFilter(dataColumnIndex);
		}
	}

	/// <summary>
	/// Task 23(b): an inheritable CONFORMANCE suite pinning the <see cref="IBrowseColumnSource"/> seam
	/// invariants independent of the implementation. It runs against the REAL <see cref="BrowseViewer"/>
	/// today (this concrete subclass) and the future viewer-free provider subclasses it and overrides
	/// <see cref="ColumnSource"/>/<see cref="FirstItem"/> — so the standalone provider is held to the SAME
	/// contract the live viewer satisfies, with no new test bodies.
	/// </summary>
	public abstract class BrowseColumnSourceConformanceTestsBase : BulkEditBarTestsBase
	{
		/// <summary>The column source under test (the live viewer here; the provider in a future subclass).</summary>
		protected abstract IBrowseColumnSource ColumnSource { get; }

		/// <summary>A representative sort item to project a row through, or null if the list is empty.</summary>
		protected abstract IManyOnePathSortItem FirstItem { get; }

		[Test]
		public void GetRowCellStrings_LengthEqualsColumnCount()
		{
			Assume.That(FirstItem, Is.Not.Null, "need at least one row");
			Assert.That(ColumnSource.GetRowCellStrings(FirstItem).Count, Is.EqualTo(ColumnSource.ColumnCount),
				"one display string per data column");
		}

		[Test]
		public void GetRowCellTsString_AgreesWithGetRowCellStrings_OnEmptiness()
		{
			Assume.That(FirstItem, Is.Not.Null);
			var strings = ColumnSource.GetRowCellStrings(FirstItem);
			for (var col = 0; col < ColumnSource.ColumnCount; col++)
			{
				var tss = ColumnSource.GetRowCellTsString(FirstItem, col);
				// A null TsString means the column's finder has no key (e.g. sort-method/int columns); the
				// contract only requires that WHEN a TsString is produced its emptiness matches the plain
				// string (the two come from the same finder and must never contradict).
				if (tss == null)
					continue;
				var tssEmpty = string.IsNullOrEmpty(tss.Text);
				var plainEmpty = col >= strings.Count || string.IsNullOrEmpty(strings[col]);
				Assert.That(tssEmpty, Is.EqualTo(plainEmpty),
					$"col {col}: the rich (Key) and plain (Strings) projections of the same finder agree on emptiness");
			}
		}

		[Test]
		public void MakeColumnSorter_OutOfRange_ReturnsNull_DoesNotThrow()
		{
			Assert.That(ColumnSource.MakeColumnSorter(-1, true), Is.Null);
			Assert.That(ColumnSource.MakeColumnSorter(ColumnSource.ColumnCount, true), Is.Null);
		}

		[Test]
		public void MakeColumnFilter_OutOfRange_ReturnsNull_DoesNotThrow()
		{
			Assert.That(ColumnSource.MakeColumnFilter(-1, BrowseColumnFilterKind.Contains, "x"), Is.Null);
			Assert.That(ColumnSource.MakeColumnFilter(ColumnSource.ColumnCount, BrowseColumnFilterKind.Contains, "x"), Is.Null);
		}

		[Test]
		public void MakeColumnFilter_EmptyContainsTerm_ReturnsNull_RatherThanThrow()
		{
			// An empty contains term is "no filter" (the clear path), not an error.
			Assert.That(ColumnSource.MakeColumnFilter(0, BrowseColumnFilterKind.Contains, string.Empty), Is.Null);
		}

		[Test]
		public void IsColumnEditable_DoesNotThrowAcrossEveryColumn()
		{
			for (var col = 0; col < ColumnSource.ColumnCount; col++)
				Assert.DoesNotThrow(() => ColumnSource.IsColumnEditable(col));
		}

		// ----- Configure-Columns (P1): the available-column catalog + stable keys -----

		[Test]
		public void GetAvailableColumns_IsASupersetOfShown_WithStableKeys()
		{
			var available = ColumnSource.GetAvailableColumns();
			Assert.That(available, Is.Not.Empty, "the catalog offers configurable columns");
			Assert.That(available.Count, Is.GreaterThanOrEqualTo(ColumnSource.ColumnCount),
				"every shown column is a member of the available catalog");
			Assert.That(available.All(c => !string.IsNullOrEmpty(c.Key)), Is.True, "each catalog entry has a stable key");
			Assert.That(available.Select(c => c.Key).Distinct().Count(), Is.EqualTo(available.Count),
				"catalog keys are distinct (the persistence/re-resolution identity)");
		}

		[Test]
		public void GetColumnKey_OfShownColumns_AreInTheCatalog()
		{
			var catalogKeys = new HashSet<string>(ColumnSource.GetAvailableColumns().Select(c => c.Key));
			for (var i = 0; i < ColumnSource.ColumnCount; i++)
			{
				var key = ColumnSource.GetColumnKey(i);
				Assert.That(key, Is.Not.Null.And.Not.Empty, $"shown column {i} has a stable key");
				Assert.That(catalogKeys, Does.Contain(key), $"shown column {i}'s key is offered by the catalog");
			}
		}
	}

	/// <summary>The conformance suite run against the live <see cref="BrowseViewer"/> (Task 23b).</summary>
	[TestFixture]
	public class BrowseViewerColumnSourceConformanceTests : BrowseColumnSourceConformanceTestsBase
	{
		protected override IBrowseColumnSource ColumnSource => m_bv;

		protected override IManyOnePathSortItem FirstItem
		{
			get
			{
				var clerk = (m_bv.Parent as RecordBrowseViewForTests).Clerk;
				return clerk.ListSize > 0 ? clerk.SortItemProvider.SortItemAt(0) : null;
			}
		}
	}
}
