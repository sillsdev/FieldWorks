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
	/// Fails closed: null, blank, or any unrecognized value means Legacy.
	/// </summary>
	public static class UIModeGates
	{
		/// <summary>True only when the UI mode setting is exactly "New" (case-insensitive).</summary>
		public static bool ShouldUseAvaloniaUI(string currentUiMode) =>
			string.Equals(currentUiMode, "New", StringComparison.OrdinalIgnoreCase);
	}
}
