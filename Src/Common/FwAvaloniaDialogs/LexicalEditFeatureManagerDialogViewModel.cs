// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// One group's header (name + Select All / Deselect All) plus the rows it owns. Select All/Deselect
	/// All only touch currently-visible rows, so a filtered search doesn't silently flip hidden items.
	/// </summary>
	public sealed partial class FeatureGroupViewModel : ObservableObject
	{
		public FeatureGroupViewModel(FeatureGroupOption group)
		{
			Name = group.Name;
			Features = group.Features;
		}

		public string Name { get; }
		public IReadOnlyList<FeatureOption> Features { get; }

		[ObservableProperty] private bool _isVisible = true;

		[RelayCommand]
		private void SelectAll() => SetAllVisible(true);

		[RelayCommand]
		private void DeselectAll() => SetAllVisible(false);

		private void SetAllVisible(bool enabled)
		{
			foreach (var feature in Features)
				if (feature.IsVisible)
					feature.Enabled = enabled;
		}

		/// <summary>Applies a lowercased/trimmed search term to every row and hides the group entirely when nothing matches.</summary>
		public void ApplyFilter(string term)
		{
			var anyVisible = false;
			foreach (var feature in Features)
			{
				var visible = feature.Matches(term);
				feature.IsVisible = visible;
				anyVisible |= visible;
			}
			IsVisible = anyVisible;
		}
	}

	/// <summary>
	/// View-model for the "Manage Individual Features" dialog (PR #964 review follow-up; replaces the
	/// WinForms <c>LexicalEditFeatureManagerDlg</c>, whose hand-rolled <see cref="System.Windows.Forms.FlowLayoutPanel"/>
	/// + absolute-positioned child <see cref="System.Windows.Forms.Panel"/> rows corrupted their own layout
	/// on a checkbox click). Lets a user opt individual New-UI tool surfaces back out (the master
	/// UIMode=New switch defaults every catalog tool on), grouped and searchable by name/description.
	/// Edits a product-supplied <see cref="LexicalEditFeatureManagerState"/> so the Avalonia layer stays
	/// LCModel-free; each row's <see cref="FeatureOption.Enabled"/> is written through its two-way
	/// checkbox binding directly onto the state (mirrors <c>OptionsDialogViewModel</c>'s Plugins
	/// convention), so there is no separate <c>ApplyChanges</c> override.
	/// </summary>
	public sealed partial class LexicalEditFeatureManagerDialogViewModel : DialogViewModelBase
	{
		public LexicalEditFeatureManagerDialogViewModel() : this(new LexicalEditFeatureManagerState())
		{
		}

		public LexicalEditFeatureManagerDialogViewModel(LexicalEditFeatureManagerState state)
		{
			Groups = new ObservableCollection<FeatureGroupViewModel>(
				(state?.Groups ?? Array.Empty<FeatureGroupOption>()).Select(g => new FeatureGroupViewModel(g)));
		}

		public ObservableCollection<FeatureGroupViewModel> Groups { get; }

		[ObservableProperty] private string _searchText = string.Empty;

		partial void OnSearchTextChanged(string value)
		{
			var term = (value ?? string.Empty).Trim().ToLowerInvariant();
			foreach (var group in Groups)
				group.ApplyFilter(term);
		}
	}
}
