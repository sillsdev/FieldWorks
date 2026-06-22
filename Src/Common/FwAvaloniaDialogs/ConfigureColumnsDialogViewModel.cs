// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// One column the Configure-Columns dialog lists, LCModel-free: a stable <see cref="Key"/> identity (the
	/// column source's layout token, persisted) and a display <see cref="Label"/>. The same shape is used for
	/// both the available catalog and the shown list, so the dialog binds two <see cref="ColumnChoiceItem"/>
	/// lists and reports the resulting ordered shown keys on OK.
	/// </summary>
	public sealed class ColumnChoiceItem
	{
		public ColumnChoiceItem(string key, string label)
		{
			Key = key;
			Label = label ?? string.Empty;
		}

		public string Key { get; }
		public string Label { get; }

		// Shown in the list boxes; the key stays the load-bearing identity.
		public override string ToString() => Label;
	}

	/// <summary>
	/// View-model for the Avalonia browse Configure-Columns dialog (P1: show/hide/reorder) — the
	/// LCModel-free MVVM counterpart of the legacy <c>ColumnConfigureDialog</c>'s two-list editor. It holds
	/// the available catalog and an editable, ordered SHOWN list, and exposes Add / Remove / MoveUp /
	/// MoveDown commands with the legacy guards: an exact-duplicate add is refused, and the LAST shown
	/// column cannot be removed (the "a browse needs a column" rule). OK snapshots the shown KEYS into
	/// <see cref="ResultKeys"/>; the LCModel-aware host (RecordBrowseView) maps those back to the live
	/// viewer's columns and re-projects the Avalonia surface. Cancel leaves the host's model untouched.
	/// </summary>
	public partial class ConfigureColumnsDialogViewModel : DialogViewModelBase
	{
		[ObservableProperty] private ColumnChoiceItem _selectedAvailable;
		[ObservableProperty] private ColumnChoiceItem _selectedShown;

		public ConfigureColumnsDialogViewModel(IReadOnlyList<ColumnChoiceItem> available,
			IReadOnlyList<string> shownKeys)
		{
			Available = new ObservableCollection<ColumnChoiceItem>(available ?? new List<ColumnChoiceItem>());
			var byKey = Available.ToDictionary(c => c.Key, c => c);
			Shown = new ObservableCollection<ColumnChoiceItem>();
			if (shownKeys != null)
			{
				foreach (var key in shownKeys)
					if (byKey.TryGetValue(key, out var item))
						Shown.Add(item);
			}
		}

		/// <summary>The available catalog (the "Available columns" list source).</summary>
		public ObservableCollection<ColumnChoiceItem> Available { get; }

		/// <summary>The editable, ordered shown columns (the "Columns shown" list source).</summary>
		public ObservableCollection<ColumnChoiceItem> Shown { get; }

		/// <summary>The ordered shown column keys snapshot written on OK; null until OK runs.</summary>
		public IReadOnlyList<string> ResultKeys { get; private set; }

		// ----- commands -----

		[RelayCommand]
		private void Add()
		{
			var item = SelectedAvailable;
			// Refuse an exact-duplicate add (the column is already shown), matching the legacy dialog.
			if (item == null || Shown.Any(s => s.Key == item.Key))
				return;
			Shown.Add(item);
			SelectedShown = item;
		}

		[RelayCommand]
		private void Remove()
		{
			var item = SelectedShown;
			// Refuse to remove the last shown column (a browse must keep at least one).
			if (item == null || Shown.Count <= 1)
				return;
			var index = Shown.IndexOf(item);
			Shown.Remove(item);
			if (Shown.Count > 0)
				SelectedShown = Shown[index < Shown.Count ? index : Shown.Count - 1];
		}

		[RelayCommand]
		private void MoveUp()
		{
			var index = SelectedShown == null ? -1 : Shown.IndexOf(SelectedShown);
			if (index <= 0)
				return;
			Shown.Move(index, index - 1);
		}

		[RelayCommand]
		private void MoveDown()
		{
			var index = SelectedShown == null ? -1 : Shown.IndexOf(SelectedShown);
			if (index < 0 || index >= Shown.Count - 1)
				return;
			Shown.Move(index, index + 1);
		}

		/// <summary>Whether removing the currently-selected shown column is allowed (kept above the last-column guard).</summary>
		public bool CanRemoveSelected => SelectedShown != null && Shown.Count > 1;

		/// <summary>Snapshots the ordered shown keys into <see cref="ResultKeys"/> on OK.</summary>
		protected override void ApplyChanges()
		{
			ResultKeys = Shown.Select(s => s.Key).ToList();
		}
	}
}
