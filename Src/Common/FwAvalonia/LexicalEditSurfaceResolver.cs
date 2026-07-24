// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Which implementation renders the Lexical Edit surface. WinForms is the safe default;
	/// Avalonia is the proof-of-concept path selected only when the feature flag is enabled.
	/// </summary>
	public enum LexicalEditSurface
	{
		/// <summary>The existing WinForms DataTree/Slice surface (default).</summary>
		WinForms,

		/// <summary>The Avalonia surface (flag-gated).</summary>
		Avalonia
	}

	/// <summary>
	/// Pure-logic resolver for the two-adapter feature flag that selects the active lexical-edit
	/// surface. Default is WinForms; Avalonia is selected by a persisted `UIMode = New` preference
	/// or by an explicit override used in tests.
	/// This type has no Avalonia dependency so it can be unit tested without a UI runtime.
	/// </summary>
	public static class LexicalEditSurfaceResolver
	{
		// Stage 2.2: tool support now comes from an app-wide registry rather than a hardcoded array. The
		// default registry is seeded with the tools that ship with Avalonia support, so the static
		// convenience methods below keep their exact original behavior.
		private static readonly LexicalEditSurfaceRegistry DefaultRegistry =
			LexicalEditSurfaceRegistry.CreateDefault();

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
		/// Property/app-setting key storing the user's per-tool opt-outs from the New UI mode (the
		/// "Manage Individual Features" dialog). Value is a comma-separated tool-name list; empty/blank
		/// means every catalog tool is enabled — the master UIMode=New switch's "everything on" default.
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
		/// Resolves the surface to use. Resolution order: an explicit <paramref name="overrideEnabled"/>
		/// wins; otherwise the persisted <paramref name="uiMode"/> user preference is used.
		/// </summary>
		/// <param name="overrideEnabled">Optional strong override (PropertyTable/registry).</param>
		/// <param name="uiMode">Persisted user preference (`Legacy` or `New`).</param>
		public static LexicalEditSurface Resolve(
			bool? overrideEnabled = null,
			string uiMode = null,
			string currentToolName = null)
			=> Resolve(DefaultRegistry, overrideEnabled, uiMode, currentToolName);

		/// <summary>
		/// Registry-aware resolution (Stage 2.2): tool support comes from <paramref name="registry"/> rather
		/// than a hardcoded list, so a host can register additional tools without editing this type. A null
		/// registry uses the shipped default. Same precedence as the static overload: tool gate first, then
		/// explicit override, then the persisted UI-mode preference.
		/// </summary>
		public static LexicalEditSurface Resolve(
			LexicalEditSurfaceRegistry registry,
			bool? overrideEnabled = null,
			string uiMode = null,
			string currentToolName = null)
		{
			registry = registry ?? DefaultRegistry;
			return ResolveFromPreference(registry.SupportsAvalonia(currentToolName), overrideEnabled, uiMode);
		}

		// The single surface-precedence implementation behind the edit gate: a closed tool gate is
		// always WinForms; otherwise an explicit override wins; otherwise the persisted UI-mode
		// preference decides.
		private static LexicalEditSurface ResolveFromPreference(bool toolGateOpen, bool? overrideEnabled, string uiMode)
		{
			if (!toolGateOpen)
				return LexicalEditSurface.WinForms;

			if (overrideEnabled.HasValue)
				return overrideEnabled.Value ? LexicalEditSurface.Avalonia : LexicalEditSurface.WinForms;

			return string.Equals(uiMode, NewUIMode, StringComparison.OrdinalIgnoreCase)
				? LexicalEditSurface.Avalonia
				: LexicalEditSurface.WinForms;
		}

		public static string ToUIModeValue(LexicalEditSurface surface)
			=> surface == LexicalEditSurface.Avalonia ? NewUIMode : LegacyUIMode;

		public static bool SupportsAvaloniaForTool(string currentToolName)
			=> DefaultRegistry.SupportsAvalonia(currentToolName);
	}
}
