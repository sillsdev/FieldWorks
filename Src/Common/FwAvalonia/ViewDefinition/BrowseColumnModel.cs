// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// One available (configurable) browse column, LCModel-free, as the owned column model and the
	/// Configure-Columns dialog see it: a stable <see cref="Key"/> identity (the column source's
	/// layout-token), a display <see cref="Label"/>, and the <see cref="HasWritingSystemOption"/> flag
	/// (P2; inert in P1). The xWorks host projects the LCModel-aware <c>BrowseColumnInfo</c> catalog into
	/// these so the model/dialog/store never see the cache or XML specs.
	/// </summary>
	public sealed class BrowseColumnChoice
	{
		public BrowseColumnChoice(string key, string label, bool hasWritingSystemOption = false)
		{
			Key = key ?? throw new ArgumentNullException(nameof(key));
			Label = label ?? string.Empty;
			HasWritingSystemOption = hasWritingSystemOption;
		}

		public string Key { get; }
		public string Label { get; }
		public bool HasWritingSystemOption { get; }
	}

	/// <summary>One shown column in the owned model: its catalog <see cref="Key"/> plus an optional pixel width.</summary>
	public sealed class BrowseColumnEntry
	{
		public BrowseColumnEntry(string key, double? width = null)
		{
			Key = key ?? throw new ArgumentNullException(nameof(key));
			Width = width;
		}

		public string Key { get; }

		/// <summary>The persisted per-column pixel width, or null for "use the default even split".</summary>
		public double? Width { get; set; }
	}

	/// <summary>
	/// The LCModel-free, owned column model behind the Avalonia browse Configure-Columns feature — the
	/// counterpart of the legacy ColumnConfigureDialog's (available, current) pair plus per-column widths.
	/// It holds the full available catalog and the ordered SHOWN list (key + optional width), and exposes
	/// the legacy mutations: <see cref="Add"/>, <see cref="Remove"/> (refusing the last column),
	/// <see cref="MoveUp"/>/<see cref="MoveDown"/>. The xWorks host builds it from the column source's
	/// catalog + the persisted store, edits it through the dialog, then maps the shown KEYS back to the
	/// live viewer's columns (<c>InstallColumnsByKey</c>) and re-projects the Avalonia surface. Pure data:
	/// no Avalonia, no LCModel, so it unit-tests with plain catalogs.
	/// </summary>
	public sealed class BrowseColumnModel
	{
		private readonly List<BrowseColumnChoice> _available;
		private readonly List<BrowseColumnEntry> _shown;

		/// <summary>
		/// Builds the model from the available catalog and the ordered shown entries. Shown entries whose key
		/// is not in the catalog are dropped (a stale persisted column the catalog no longer offers); the
		/// shown order is otherwise preserved. If nothing valid remains shown, the model is left empty and the
		/// caller is expected to seed a shipped default.
		/// </summary>
		public BrowseColumnModel(IReadOnlyList<BrowseColumnChoice> available, IReadOnlyList<BrowseColumnEntry> shown)
		{
			if (available == null) throw new ArgumentNullException(nameof(available));
			_available = available.ToList();
			var catalogKeys = new HashSet<string>(_available.Select(c => c.Key), StringComparer.Ordinal);
			_shown = (shown ?? new List<BrowseColumnEntry>())
				.Where(e => e != null && catalogKeys.Contains(e.Key))
				.Select(e => new BrowseColumnEntry(e.Key, e.Width))
				.ToList();
		}

		/// <summary>The full catalog of columns that could be shown (the dialog's "available" source).</summary>
		public IReadOnlyList<BrowseColumnChoice> Available => _available;

		/// <summary>The ordered shown columns (key + optional width) — the dialog's "shown" list and what persists.</summary>
		public IReadOnlyList<BrowseColumnEntry> Shown => _shown;

		/// <summary>The ordered shown column keys (the form the viewer install path and persistence consume).</summary>
		public IReadOnlyList<string> ShownKeys => _shown.Select(e => e.Key).ToList();

		/// <summary>Whether <paramref name="key"/> is currently shown.</summary>
		public bool IsShown(string key) => _shown.Any(e => string.Equals(e.Key, key, StringComparison.Ordinal));

		/// <summary>The catalog choice for a key, or null when the catalog doesn't offer it.</summary>
		public BrowseColumnChoice ChoiceFor(string key)
			=> _available.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.Ordinal));

		/// <summary>
		/// Adds the catalog column <paramref name="key"/> to the end of the shown list. No-op when the key is
		/// not in the catalog or is already shown (the dialog also blocks an exact-duplicate add). Returns
		/// true when the shown list changed.
		/// </summary>
		public bool Add(string key)
		{
			if (string.IsNullOrEmpty(key) || IsShown(key)
				|| !_available.Any(c => string.Equals(c.Key, key, StringComparison.Ordinal)))
				return false;
			_shown.Add(new BrowseColumnEntry(key));
			return true;
		}

		/// <summary>
		/// Removes the shown column <paramref name="key"/>. Refuses to remove the LAST shown column (a browse
		/// always needs at least one column — the legacy "ksBrowseNeedsAColumn" guard). Returns true when the
		/// shown list changed.
		/// </summary>
		public bool Remove(string key)
		{
			if (_shown.Count <= 1)
				return false;
			var index = IndexOf(key);
			if (index < 0)
				return false;
			_shown.RemoveAt(index);
			return true;
		}

		/// <summary>Moves the shown column up one position (toward the front). Returns true when it moved.</summary>
		public bool MoveUp(string key)
		{
			var index = IndexOf(key);
			if (index <= 0)
				return false;
			Swap(index, index - 1);
			return true;
		}

		/// <summary>Moves the shown column down one position (toward the back). Returns true when it moved.</summary>
		public bool MoveDown(string key)
		{
			var index = IndexOf(key);
			if (index < 0 || index >= _shown.Count - 1)
				return false;
			Swap(index, index + 1);
			return true;
		}

		/// <summary>Records the persisted pixel width for a shown column (no-op when the key is not shown).</summary>
		public void SetWidth(string key, double width)
		{
			var index = IndexOf(key);
			if (index >= 0)
				_shown[index].Width = width;
		}

		/// <summary>The persisted width of a shown column, or null when none/unknown.</summary>
		public double? WidthOf(string key)
		{
			var index = IndexOf(key);
			return index >= 0 ? _shown[index].Width : null;
		}

		private int IndexOf(string key)
			=> _shown.FindIndex(e => string.Equals(e.Key, key, StringComparison.Ordinal));

		private void Swap(int a, int b)
		{
			var tmp = _shown[a];
			_shown[a] = _shown[b];
			_shown[b] = tmp;
		}
	}
}
