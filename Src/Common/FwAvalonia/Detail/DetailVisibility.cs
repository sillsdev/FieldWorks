// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>The contiguous field range a collapsible header owns (see <see
	/// cref="DetailVisibility"/>).</summary>
	public struct DetailHeaderRange
	{
		public DetailHeaderRange(int headerIndex, int start, int end)
		{
			HeaderIndex = headerIndex;
			Start = start;
			End = end;
		}

		/// <summary>Index of the header field within the field list.</summary>
		public int HeaderIndex { get; }

		/// <summary>First index (inclusive) owned by the header.</summary>
		public int Start { get; }

		/// <summary>Index just past the last field (exclusive) owned by the header.</summary>
		public int End { get; }
	}

	/// <summary>
	/// Pure, model-level computation of collapsible-header ownership and field visibility,
	/// derived from the field list and expansion state alone. Needing no controls, it answers
	/// for rows that have none, such as rows a collapsed header hides or an items panel has not
	/// realized.
	/// </summary>
	public static class DetailVisibility
	{
		/// <summary>
		/// The ownership range of every collapsible header in <paramref name="fields"/>, in field
		/// order. A header whose range would be empty (no following field at a greater indent) is
		/// omitted, matching the legacy rule that an empty section is not treated as collapsible.
		/// </summary>
		public static IReadOnlyList<DetailHeaderRange> GetCollapsibleRanges(IReadOnlyList<DetailField> fields)
		{
			if (fields == null)
				return Array.Empty<DetailHeaderRange>();

			var ranges = new List<DetailHeaderRange>();
			for (var i = 0; i < fields.Count; i++)
			{
				var field = fields[i];
				if (field.Kind != DetailFieldKind.Header || !field.IsCollapsible)
					continue;

				var start = i + 1;
				var end = start;
				while (end < fields.Count && fields[end].Indent > field.Indent)
					end++;
				if (end == start)
					continue;

				ranges.Add(new DetailHeaderRange(i, start, end));
			}
			return ranges;
		}

		/// <summary>
		/// Per-field visibility: a field is visible iff every collapsible header whose range owns
		/// it is expanded. Ranges nest, so a field owned by a collapsed ancestor stays hidden
		/// even
		/// when a nearer ancestor is expanded (the legacy SliceTreeNode behavior). A header's own
		/// visibility depends only on its ancestors' state, never its own.
		/// </summary>
		/// <param name="getExpansionState">
		/// Returns the recorded expansion state for a header's
		/// <see cref="DetailField.ExpansionKey"/>, or null when none is recorded (falls back to
		/// <see cref="DetailField.IsInitiallyExpanded"/>).
		/// May itself be null, meaning no state is ever recorded.
		/// </param>
		public static IReadOnlyList<bool> ComputeVisibility(
			IReadOnlyList<DetailField> fields,
			Func<string, bool?> getExpansionState)
		{
			if (fields == null)
				return Array.Empty<bool>();

			var visible = new bool[fields.Count];
			for (var i = 0; i < visible.Length; i++)
				visible[i] = true;

			foreach (var range in GetCollapsibleRanges(fields))
			{
				if (IsExpanded(fields[range.HeaderIndex], getExpansionState))
					continue;
				for (var i = range.Start; i < range.End; i++)
					visible[i] = false;
			}
			return visible;
		}

		/// <summary>
		/// The collapsed headers in <paramref name="current"/> that hide a row belonging to an
		/// object no row of <paramref name="previous"/> belonged to, in field order. Expanding
		/// them shows an object that was just added to the record. A row hidden by any header
		/// <paramref name="mustStayCollapsed"/> accepts contributes none of its headers, since
		/// that row stays hidden either way. Expansion state is read as in
		/// <see cref="ComputeVisibility"/>; a null <paramref name="mustStayCollapsed"/> keeps
		/// no header collapsed.
		/// </summary>
		public static IReadOnlyList<DetailField> GetHeadersHidingNewObjects(
			IReadOnlyList<DetailField> previous,
			IReadOnlyList<DetailField> current,
			Func<string, bool?> getExpansionState,
			Func<DetailField, bool> mustStayCollapsed = null)
		{
			if (previous == null || current == null)
				return Array.Empty<DetailField>();

			var known = new HashSet<int>();
			foreach (var field in previous)
				known.Add(field.ObjectHvo);

			var collapsed = new List<DetailHeaderRange>();
			foreach (var range in GetCollapsibleRanges(current))
			{
				if (!IsExpanded(current[range.HeaderIndex], getExpansionState))
					collapsed.Add(range);
			}

			var revealed = new SortedSet<int>();
			for (var i = 0; i < current.Count; i++)
			{
				var hvo = current[i].ObjectHvo;
				if (hvo == 0 || known.Contains(hvo))
					continue;
				var row = i;
				var hiding = collapsed.Where(range => range.Start <= row && row < range.End).ToList();
				if (mustStayCollapsed != null
					&& hiding.Any(range => mustStayCollapsed(current[range.HeaderIndex])))
				{
					continue;
				}
				foreach (var range in hiding)
					revealed.Add(range.HeaderIndex);
			}
			return revealed.Select(index => current[index]).ToList();
		}

		private static bool IsExpanded(DetailField header, Func<string, bool?> getExpansionState)
			=> getExpansionState?.Invoke(header.ExpansionKey) ?? header.IsInitiallyExpanded;

		/// <summary>The subsequence of <paramref name="fields"/> that <see
		/// cref="ComputeVisibility"/> keeps visible.</summary>
		public static IReadOnlyList<DetailField> GetVisibleFields(
			IReadOnlyList<DetailField> fields,
			Func<string, bool?> getExpansionState)
		{
			if (fields == null)
				return Array.Empty<DetailField>();

			var visibility = ComputeVisibility(fields, getExpansionState);
			var result = new List<DetailField>();
			for (var i = 0; i < fields.Count; i++)
			{
				if (visibility[i])
					result.Add(fields[i]);
			}
			return result;
		}
	}
}
