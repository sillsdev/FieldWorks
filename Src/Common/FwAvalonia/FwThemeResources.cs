// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Point-of-use resolution for the shared FwAvaloniaTheme token dictionaries. Never caches:
	/// each call resolves through <see cref="Application.Current"/>, so a lookup cannot run at
	/// type-load time, when that property is still null under beforefieldinit semantics.
	/// </summary>
	/// <exception cref="System.InvalidOperationException">A key is absent, or the Avalonia
	/// Application has not started. Throwing is the design: a missing token must not silently
	/// become a default value that looks like a deliberate one.</exception>
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
