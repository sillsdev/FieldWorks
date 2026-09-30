// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// The single shared New-mode gate. Lives in FwUtils, with no Avalonia-referencing types anywhere in
	/// this class, so checking the gate never causes the CLR to load the Avalonia assemblies: legacy-mode
	/// call sites must pair this with a [MethodImpl(NoInlining)] helper holding their Avalonia branch.
	/// Default is off: New requires both the FW_AVALONIA opt-in and a "New" mode value, and
	/// null, blank, or any unrecognized value means Legacy.
	/// </summary>
	public static class UIModeGates
	{
		/// <summary>Environment variable a user sets to opt in to choosing the New UI.</summary>
		public const string SwitchingEnabledVariable = "FW_AVALONIA";

		/// <summary>
		/// True only when <paramref name="currentUiMode"/> is exactly "New" (case-insensitive). Judges the
		/// value alone and does not consult <see cref="SwitchingEnabledVariable"/>, so it suits values that
		/// were already gated when the persisted setting was read (the PropertyTable "UIMode" property);
		/// a persisted setting read straight from disk needs <see cref="ShouldUseAvaloniaUIFromSettings"/>.
		/// </summary>
		public static bool ShouldUseAvaloniaUI(string currentUiMode) =>
			string.Equals(currentUiMode, "New", StringComparison.OrdinalIgnoreCase);

		/// <summary>
		/// True when the persisted UI-mode setting selects the New UI AND the user has opted in via
		/// <see cref="SwitchingEnabledVariable"/>. Without the opt-in a persisted "New" is ignored, which
		/// leaves the value untouched on disk so it takes effect again once the variable is set.
		/// </summary>
		public static bool ShouldUseAvaloniaUIFromSettings(string persistedUiMode) =>
			IsSwitchingEnabled() && ShouldUseAvaloniaUI(persistedUiMode);

		/// <summary>Reads <see cref="SwitchingEnabledVariable"/> from the current process environment.</summary>
		public static bool IsSwitchingEnabled() =>
			IsSwitchingEnabled(Environment.GetEnvironmentVariable(SwitchingEnabledVariable));

		/// <summary>
		/// True for any <paramref name="variableValue"/> except null, blank, "0", "false", and "off"
		/// (case-insensitive), so the variable can be spelled "1", "true", or "yes" and still turns
		/// switching on, while "0" reliably turns it back off.
		/// </summary>
		internal static bool IsSwitchingEnabled(string variableValue)
		{
			if (string.IsNullOrWhiteSpace(variableValue))
				return false;

			var trimmed = variableValue.Trim();
			return !string.Equals(trimmed, "0", StringComparison.OrdinalIgnoreCase) &&
				!string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) &&
				!string.Equals(trimmed, "off", StringComparison.OrdinalIgnoreCase);
		}
	}
}
