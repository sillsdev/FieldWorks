// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.VisualTree;

namespace FwAvaloniaTests
{
	/// <summary>Shared helpers for asserting about the visual tree in headless tests.</summary>
	internal static class VisualTreeTestExtensions
	{
		// GetVisualDescendants() walks into control templates (theme primitives like Semi's
		// RepeatButton/reveal-ToggleButton); TemplatedParent is null only for controls we
		// authored.
		public static IEnumerable<T> AuthoredDescendants<T>(this Visual root) where T : Visual =>
			root.GetVisualDescendants().OfType<T>().Where(v => v.TemplatedParent == null);

		// DataTree rebuilds the Form's Items from the visible-field subsequence on every toggle,
		// so a control reference held across a toggle is stale -- always re-query the live tree
		// rather than caching one.
		public static bool HasDescendant<T>(this Visual root, Func<T, bool> predicate) where T : Visual =>
			root.GetVisualDescendants().OfType<T>().Any(predicate);
	}
}
