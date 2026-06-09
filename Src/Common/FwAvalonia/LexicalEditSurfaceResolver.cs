// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

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

		/// <summary>The Avalonia proof-of-concept surface (flag-gated).</summary>
		Avalonia
	}

	/// <summary>
	/// Pure-logic resolver for the two-adapter feature flag described in
	/// lexical-edit-avalonia-poc-spike. Default is WinForms; Avalonia is selected by a persisted
	/// `UIMode = New` preference or by an explicit override used in tests.
	/// This type has no Avalonia dependency so it can be unit tested without a UI runtime.
	/// </summary>
	public static class LexicalEditSurfaceResolver
	{
		/// <summary>Property/app-setting key storing the preferred lexical-edit UI mode.</summary>
		public const string UIModePropertyName = "UIMode";
		public const string LegacyUIMode = "Legacy";
		public const string NewUIMode = "New";

		/// <summary>
		/// Resolves the surface to use. Resolution order: an explicit <paramref name="overrideEnabled"/>
		/// wins; otherwise the persisted <paramref name="uiMode"/> user preference is used.
		/// </summary>
		/// <param name="overrideEnabled">Optional strong override (PropertyTable/registry).</param>
		/// <param name="uiMode">Persisted user preference (`Legacy` or `New`).</param>
		public static LexicalEditSurface Resolve(
			bool? overrideEnabled = null,
			string uiMode = null)
		{
			if (overrideEnabled.HasValue)
			{
				return overrideEnabled.Value ? LexicalEditSurface.Avalonia : LexicalEditSurface.WinForms;
			}

			return string.Equals(uiMode, NewUIMode, StringComparison.OrdinalIgnoreCase)
				? LexicalEditSurface.Avalonia
				: LexicalEditSurface.WinForms;
		}

		public static string ToUIModeValue(LexicalEditSurface surface)
			=> surface == LexicalEditSurface.Avalonia ? NewUIMode : LegacyUIMode;
	}
}
