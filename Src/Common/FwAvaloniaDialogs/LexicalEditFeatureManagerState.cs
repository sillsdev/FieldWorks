// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The product-supplied state for the "Manage Individual Features" dialog, carried across the
	/// FwAvaloniaDialogs (Avalonia, LCModel-free) &#8596; product (LexText) boundary — mirrors
	/// <see cref="OptionsState"/>'s DTO convention. The product edge builds <see cref="Groups"/> from
	/// <c>LexicalEditFeatureCatalog</c> plus the persisted disabled-tools set; the dialog edits each
	/// row's <see cref="FeatureOption.Enabled"/> directly through its two-way checkbox binding (no
	/// separate apply step needed, same as <see cref="OptionsState"/>'s Plugins list).
	/// </summary>
	public sealed class LexicalEditFeatureManagerState
	{
		public IReadOnlyList<FeatureGroupOption> Groups { get; set; } = new List<FeatureGroupOption>();
	}

	/// <summary>One named group of manageable tool surfaces (e.g. "Dialogs (lexical entry)").</summary>
	public sealed class FeatureGroupOption
	{
		public FeatureGroupOption(string name, IReadOnlyList<FeatureOption> features)
		{
			Name = name;
			Features = features;
		}

		public string Name { get; }
		public IReadOnlyList<FeatureOption> Features { get; }
	}

	/// <summary>
	/// One manageable tool surface row. <see cref="ObservableObject"/> (not a plain POCO like
	/// <c>PluginOption</c>) because <see cref="IsVisible"/> must push live updates to the bound view as
	/// the user types in the dialog's search box; <see cref="Enabled"/> rides the same two-way
	/// checkbox-binding convention <c>PluginOption.Installed</c> uses.
	/// </summary>
	public sealed partial class FeatureOption : ObservableObject
	{
		private readonly string _searchText;

		public FeatureOption(string toolName, string displayName, string description, bool enabled)
		{
			ToolName = toolName;
			DisplayName = displayName;
			Description = description;
			_enabled = enabled;
			_searchText = (displayName + " " + description).ToLowerInvariant();
		}

		public string ToolName { get; }
		public string DisplayName { get; }
		public string Description { get; }

		[ObservableProperty] private bool _enabled;
		[ObservableProperty] private bool _isVisible = true;

		/// <summary>True when <paramref name="term"/> (already trimmed/lowercased) is empty or found in the display name/description.</summary>
		public bool Matches(string term) => term.Length == 0 || _searchText.Contains(term);
	}
}
