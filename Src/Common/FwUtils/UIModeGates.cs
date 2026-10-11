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

		/// <summary>
		/// True when <see cref="SwitchingEnabledVariable"/> is set in the current process
		/// environment to a value that opts in, which is anything except blank, "0",
		/// "false", and "off".
		/// </summary>
		public static bool IsSwitchingEnabled() =>
			EnvironmentVariables.IsTrue(SwitchingEnabledVariable);
	}
}
