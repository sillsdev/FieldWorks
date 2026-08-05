// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Pure-logic resolver for the two-adapter feature flag that selects which
	/// <see cref="UIFramework"/> renders the current tool. Default is Legacy; Avalonia is selected
	/// by a persisted `UIMode = New` preference or by an explicit override used in tests.
	/// This type has no Avalonia dependency so it can be unit tested without a UI runtime.
	/// </summary>
	public static class UIFrameworkResolver
	{
		// Tool support now comes from an app-wide registry rather than a hardcoded array. The
		// default registry is seeded with the tools that ship with Avalonia support, so the static
		// convenience methods below keep their exact original behavior.
		private static readonly UIFrameworkRegistry DefaultRegistry =
			UIFrameworkRegistry.CreateDefault();

		/// <summary>Property/app-setting key storing the preferred lexical-edit UI mode.</summary>
		public const string UIModePropertyName = "UIMode";
		public const string LegacyUIMode = "Legacy";
		public const string NewUIMode = "New";

		/// <summary>
		/// Normalizes a persisted UI-mode value to exactly <see cref="NewUIMode"/> or
		/// <see cref="LegacyUIMode"/>: only a case-insensitive "New" selects New; null, blank, or any
		/// other value fails closed to Legacy. The single normalization shared by the settings seeding
		/// (FwXWindow) and both Options dialogs.
		/// </summary>
		public static string NormalizeUIMode(string uiMode) =>
			string.Equals(uiMode, NewUIMode, StringComparison.OrdinalIgnoreCase) ? NewUIMode : LegacyUIMode;

		/// <summary>
		/// Property/app-setting key storing the user's per-tool opt-outs from the New UI mode. Value is a
		/// comma-separated tool-name list; empty/blank means every catalog tool is enabled — the master
		/// UIMode=New switch's "everything on" default. No dialog edits it; it is set out of band.
		/// </summary>
		public const string UIModeDisabledToolsPropertyName = "UIModeDisabledTools";

		/// <summary>Parses the persisted comma-separated disabled-tools value into a lookup set.</summary>
		public static HashSet<string> ParseDisabledTools(string disabledToolsCsv)
		{
			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrWhiteSpace(disabledToolsCsv))
				return result;

			foreach (var name in disabledToolsCsv.Split(','))
			{
				var trimmed = name.Trim();
				if (trimmed.Length > 0)
					result.Add(trimmed);
			}
			return result;
		}

		/// <summary>Serializes a disabled-tools set back to the persisted comma-separated form.</summary>
		public static string SerializeDisabledTools(IEnumerable<string> disabledToolNames)
			=> string.Join(",", disabledToolNames ?? Enumerable.Empty<string>());

		/// <summary>True when <paramref name="toolName"/> is present (case-insensitive) in the disabled-tools value.</summary>
		public static bool IsToolDisabledByUser(string disabledToolsCsv, string toolName)
			=> !string.IsNullOrWhiteSpace(toolName) && ParseDisabledTools(disabledToolsCsv).Contains(toolName);

		/// <summary>
		/// Resolves the UI framework to use. Resolution order: an explicit <paramref name="overrideEnabled"/>
		/// wins; otherwise the persisted <paramref name="uiMode"/> user preference is used.
		/// </summary>
		/// <param name="overrideEnabled">Optional strong override (PropertyTable/registry).</param>
		/// <param name="uiMode">Persisted user preference (`Legacy` or `New`).</param>
		public static UIFramework Resolve(
			bool? overrideEnabled = null,
			string uiMode = null,
			string currentToolName = null)
			=> Resolve(DefaultRegistry, overrideEnabled, uiMode, currentToolName);

		/// <summary>
		/// Registry-aware resolution: tool support comes from <paramref name="registry"/> rather
		/// than a hardcoded list, so a host can register additional tools without editing this type. A null
		/// registry uses the shipped default. Same precedence as the static overload: tool gate first, then
		/// explicit override, then the persisted UI-mode preference.
		/// </summary>
		public static UIFramework Resolve(
			UIFrameworkRegistry registry,
			bool? overrideEnabled = null,
			string uiMode = null,
			string currentToolName = null)
		{
			registry = registry ?? DefaultRegistry;
			return ResolveFromPreference(registry.SupportsAvalonia(currentToolName), overrideEnabled, uiMode);
		}

		// The single framework-precedence implementation behind the edit gate: a closed tool gate is
		// always Legacy; otherwise an explicit override wins; otherwise the persisted UI-mode
		// preference decides.
		private static UIFramework ResolveFromPreference(bool toolGateOpen, bool? overrideEnabled, string uiMode)
		{
			if (!toolGateOpen)
				return UIFramework.Legacy;

			if (overrideEnabled.HasValue)
				return overrideEnabled.Value ? UIFramework.Avalonia : UIFramework.Legacy;

			return string.Equals(uiMode, NewUIMode, StringComparison.OrdinalIgnoreCase)
				? UIFramework.Avalonia
				: UIFramework.Legacy;
		}

		public static string ToUIModeValue(UIFramework framework)
			=> framework == UIFramework.Avalonia ? NewUIMode : LegacyUIMode;

		public static bool SupportsAvaloniaForTool(string currentToolName)
			=> DefaultRegistry.SupportsAvalonia(currentToolName);
	}
}
