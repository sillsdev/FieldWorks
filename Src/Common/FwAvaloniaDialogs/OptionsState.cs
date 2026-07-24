// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The real Tools → Options settings, carried across the FwAvaloniaDialogs (Avalonia, LCModel-free)
	/// ↔ product (LexText) boundary. The product edge populates this from the live settings bus
	/// (PropertyTable / FwApplicationSettings / registry / writing-system manager / plugin manager),
	/// the dialog view-model edits it, and the product edge applies it on OK — so the Avalonia layer
	/// never references LCModel or the PropertyTable. Mirrors the four tabs of the legacy
	/// <c>LexOptionsDlg</c>: General, Plugins, Privacy, Updates.
	/// </summary>
	public sealed class OptionsState
	{
		// --- General: user-interface language ---
		public IReadOnlyList<NamedOption> AvailableUiLanguages { get; set; } = new List<NamedOption>();
		public string UiLanguage { get; set; }

		// --- General: Lexical Edit UI mode (Legacy / New) ---
		public string UiMode { get; set; } = "Legacy";

		/// <summary>
		/// The per-feature disable set (a CSV of tool names) edited by the "Manage Individual Features"
		/// dialog and applied on OK — mirrors <c>FwApplicationSettings.UIModeDisabledTools</c> and the
		/// WinForms <c>LexOptionsDlg.m_pendingUiModeDisabledTools</c>. Only meaningful in New mode.
		/// </summary>
		public string UIModeDisabledTools { get; set; } = string.Empty;

		/// <summary>
		/// Optional product callback that opens the "Manage Individual Features" dialog seeded with the
		/// given disabled-tools CSV and returns the edited CSV (or the same value if the user cancelled).
		/// Set by the product launcher (which owns the owner window + the feature catalog), so the Avalonia
		/// layer stays LCModel-free and never shows the nested dialog itself. Null in bare-bones/test contexts.
		/// </summary>
		public Func<string, string> ManageFeatures { get; set; }

		// --- General: auto-open last project ---
		public bool AutoOpenLastProject { get; set; }

		// --- Privacy ---
		public bool OkToPingBasicUsageData { get; set; }

		// --- Updates (Windows only) ---
		public bool UpdatesTabVisible { get; set; }
		public bool AutoUpdate { get; set; }
		public string UpdateChannel { get; set; }
		public IReadOnlyList<NamedOption> AvailableChannels { get; set; } = new List<NamedOption>();

		// --- Plugins ---
		public bool PluginsAvailable { get; set; }
		public IList<PluginOption> Plugins { get; set; } = new List<PluginOption>();
	}

	/// <summary>A code/display(/description) option for a combo (UI language, update channel, UI mode).</summary>
	public sealed class NamedOption
	{
		public NamedOption(string code, string display, string description = null)
		{
			Code = code;
			Display = display;
			Description = description;
		}

		public string Code { get; }
		public string Display { get; }
		public string Description { get; }

		// Combo items show Display.
		public override string ToString() => Display;
	}

	/// <summary>One installable plugin row; <see cref="Installed"/> is toggled by the user, diffed against
	/// <see cref="WasInstalled"/> by the product applier to install/uninstall.</summary>
	public sealed class PluginOption
	{
		public PluginOption(string name, string description, bool installed)
		{
			Name = name;
			Description = description;
			Installed = installed;
			WasInstalled = installed;
		}

		public string Name { get; }
		public string Description { get; }
		public bool Installed { get; set; }
		public bool WasInstalled { get; }
	}
}
