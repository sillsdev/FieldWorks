// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Point-of-use resolution for the shared FwAvaloniaTheme token dictionaries
	/// (Src/Common/FwAvaloniaTheme/Tokens/), merged into Application.Resources by both
	/// FwAvaloniaApp
	/// and PreviewHostApp. Deliberately never cached in a static field: every call resolves fresh
	/// via
	/// <see cref="Application.Current"/>, so lookups happen after the Avalonia Application has
	/// actually started rather than at type-load/static-field-init time (when Application.Current
	/// is
	/// still null under beforefieldinit semantics).
	///
	/// PRECONDITION: every Require* method below throws (see <see cref="Require"/>) rather than
	/// returning a default when called before the Avalonia Application has started -- callers
	/// (e.g. <see cref="FwAvaloniaDensity"/>) must only run from code that executes after
	/// initialization.
	/// </summary>
	internal static class FwThemeResources
	{
		/// <summary>Resolves a required numeric token (e.g. FwSurfaceFontSize).</summary>
		public static double RequireDouble(string key) => (double)Require(key);

		/// <summary>Resolves a required brush token (e.g. FwLabelBrush).</summary>
		public static IBrush RequireBrush(string key) => (IBrush)Require(key);

		/// <summary>Resolves a required <see cref="Thickness"/> token (e.g.
		/// DataTree.SliceMargin).</summary>
		public static Thickness RequireThickness(string key) => (Thickness)Require(key);

		/// <summary>Resolves a required <see cref="CornerRadius"/> token (e.g.
		/// DataTree.PickerCornerRadius).</summary>
		public static CornerRadius RequireCornerRadius(string key) => (CornerRadius)Require(key);

		/// <summary>No fallback default on a miss: both hosts merge the token dictionaries
		/// unconditionally at Initialize(), so a missing key means the wiring is broken and must
		/// fail loudly rather than silently substitute a hardcoded value.</summary>
		private static object Require(string key)
		{
			var app = Application.Current;
			if (app != null && app.TryGetResource(key, app.ActualThemeVariant, out var value))
				return value;
			throw new InvalidOperationException(
				"FwAvaloniaTheme resource '" + key + "' was not found. FwAvaloniaApp/PreviewHostApp " +
				"must merge the FwAvaloniaTheme token dictionaries into Application.Resources first.");
		}
	}
}
