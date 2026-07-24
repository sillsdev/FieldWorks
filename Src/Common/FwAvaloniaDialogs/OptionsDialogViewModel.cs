// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the Tools → Options dialog. Edits a product-supplied <see cref="OptionsState"/>
	/// (the real settings bus, populated/applied at the LexText edge) so the Avalonia layer stays
	/// LCModel-free. Covers the four legacy tabs — General (UI language, Lexical Edit UI mode,
	/// auto-open), Plugins, Privacy, Updates. CommunityToolkit.Mvvm generates the observable properties
	/// and <c>OkCommand</c>/<c>CancelCommand</c>; <see cref="IDialogViewModel.CloseRequested"/> closes
	/// the hosting modal window. On OK the edited values are written back into the state for the edge to apply.
	/// </summary>
	public partial class OptionsDialogViewModel : DialogViewModelBase
	{
		private readonly OptionsState _state;

		// General
		[ObservableProperty] private NamedOption _selectedUiLanguage;
		[ObservableProperty] [NotifyPropertyChangedFor(nameof(ManageFeaturesVisible))] private NamedOption _selectedUiMode;
		[ObservableProperty] private bool _autoOpenLastProject;
		// Pending per-feature disable set (CSV of tool names), edited by "Manage Individual Features" and
		// written back to the state on OK. Mirrors LexOptionsDlg.m_pendingUiModeDisabledTools.
		[ObservableProperty] private string _uiModeDisabledTools;
		// Privacy
		[ObservableProperty] private bool _okToPingBasicUsageData;
		// Updates
		[ObservableProperty] private bool _autoUpdate;
		[ObservableProperty] private NamedOption _selectedUpdateChannel;
		// Which tab is shown; two-way bound to the TabControl. Defaults to the first tab; callers can open
		// the dialog on a later tab via the initialTab ctor parameter (used by parity screenshots / deep links).
		[ObservableProperty] private int _selectedTabIndex;

		public OptionsDialogViewModel() : this(new OptionsState())
		{
		}

		/// <param name="initialTab">
		/// Zero-based tab to show on open (0=General, 1=Plugins, 2=Privacy, 3=Updates). Out-of-range values
		/// are clamped. Note: if the Updates tab is hidden (<see cref="UpdatesTabVisible"/> is false) index 3
		/// has no visible tab to select.
		/// </param>
		public OptionsDialogViewModel(OptionsState state, int initialTab = 0)
		{
			_state = state ?? new OptionsState();

			UiLanguages = _state.AvailableUiLanguages;
			SelectedUiLanguage = Match(UiLanguages, _state.UiLanguage);

			UiModes = new[]
			{
				new NamedOption(LegacyMode, FwAvaloniaDialogsStrings.UiModeLegacy),
				new NamedOption(NewMode, FwAvaloniaDialogsStrings.UiModeNew)
			};
			SelectedUiMode = Match(UiModes, _state.UiMode) ?? UiModes[0];
			UiModeDisabledTools = _state.UIModeDisabledTools ?? string.Empty;

			AutoOpenLastProject = _state.AutoOpenLastProject;
			OkToPingBasicUsageData = _state.OkToPingBasicUsageData;

			UpdatesTabVisible = _state.UpdatesTabVisible;
			AutoUpdate = _state.AutoUpdate;
			UpdateChannels = _state.AvailableChannels;
			SelectedUpdateChannel = Match(UpdateChannels, _state.UpdateChannel);

			PluginsAvailable = _state.PluginsAvailable;
			Plugins = _state.Plugins;

			// Open on the requested tab (clamped to the four tab indices).
			SelectedTabIndex = Math.Min(Math.Max(initialTab, 0), 3);
		}

		private const string LegacyMode = "Legacy";
		private const string NewMode = "New";

		public IReadOnlyList<NamedOption> UiLanguages { get; }
		public IReadOnlyList<NamedOption> UiModes { get; }
		public IReadOnlyList<NamedOption> UpdateChannels { get; }
		public IList<PluginOption> Plugins { get; }

		public bool UpdatesTabVisible { get; }
		public bool PluginsAvailable { get; }

		/// <summary>The selected channel's description, shown beneath the channel combo.</summary>
		public string SelectedChannelDescription => SelectedUpdateChannel?.Description ?? string.Empty;

		partial void OnSelectedUpdateChannelChanged(NamedOption value)
			=> OnPropertyChanged(nameof(SelectedChannelDescription));

		/// <summary>
		/// The "Manage Individual Features..." button is only meaningful when the master switch is New — there
		/// is nothing to manage while Legacy is selected, so it stays hidden. Mirrors the WinForms
		/// <c>LexOptionsDlg.UpdateManageFeaturesButtonVisibility</c>.
		/// </summary>
		public bool ManageFeaturesVisible =>
			SelectedUiMode != null && string.Equals(SelectedUiMode.Code, NewMode, StringComparison.Ordinal);

		/// <summary>
		/// Opens the "Manage Individual Features" dialog (via the product-supplied <see cref="OptionsState.ManageFeatures"/>
		/// callback) seeded with the pending disabled-tools set, and keeps the edited result pending until OK.
		/// Mirrors the WinForms <c>LexOptionsDlg.m_manageFeaturesButton_Click</c>. A no-op with no callback
		/// (bare-bones/test contexts).
		/// </summary>
		[RelayCommand]
		private void ManageFeatures()
		{
			var edited = _state.ManageFeatures?.Invoke(UiModeDisabledTools ?? string.Empty);
			if (edited != null)
				UiModeDisabledTools = edited;
		}

		private static NamedOption Match(IEnumerable<NamedOption> options, string code) =>
			options?.FirstOrDefault(o => string.Equals(o.Code, code, StringComparison.Ordinal));

		/// <summary>
		/// Writes the edited values back into the product-supplied <see cref="OptionsState"/> (the
		/// "ApplyTo(state)" convention). Invoked by the base OK command before the dialog closes.
		/// </summary>
		protected override void ApplyChanges()
		{
			_state.UiLanguage = SelectedUiLanguage?.Code ?? _state.UiLanguage;
			_state.UiMode = SelectedUiMode?.Code ?? _state.UiMode;
			_state.UIModeDisabledTools = UiModeDisabledTools ?? _state.UIModeDisabledTools;
			_state.AutoOpenLastProject = AutoOpenLastProject;
			_state.OkToPingBasicUsageData = OkToPingBasicUsageData;
			_state.AutoUpdate = AutoUpdate;
			_state.UpdateChannel = SelectedUpdateChannel?.Code ?? _state.UpdateChannel;
			// Plugin Installed flags are written through their two-way checkbox bindings directly on the state.
		}
	}
}
