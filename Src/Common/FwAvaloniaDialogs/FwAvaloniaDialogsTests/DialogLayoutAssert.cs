// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using NUnit.Framework;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The headless geometry tripwire for dialog layout. After a dialog body is shown and laid out
	/// headlessly, <see cref="AssertNoCrowding"/> walks the realized visual tree and fails if it finds the
	/// "no border around the words / text crowding the edges" defect class:
	///   * a visible text-bearing control (TextBlock / TextBox / text ContentPresenter) with a zero-area bounds,
	///   * two sibling controls whose bounds overlap,
	///   * a child whose bounds butt against its parent container edge (inset below the spacing token), or
	///   * a PART_*Host border with no effective border thickness.
	/// Deterministic; no real windows (relies only on the laid-out Bounds).
	/// </summary>
	public static class DialogLayoutAssert
	{
		/// <summary>The minimum edge inset (in px) a child must keep from its padded parent's content edge.</summary>
		public const double MinEdgeInset = 0.5;

		public static void AssertNoCrowding(Control root)
		{
			Assert.That(root, Is.Not.Null, "AssertNoCrowding needs a realized control");

			var all = root.GetVisualDescendants().OfType<Control>().ToList();

			AssertTextNotZeroArea(all);
			AssertSiblingsDoNotOverlap(root);
			AssertHostBordersHaveAFrame(all);
			AssertChildrenAreInsetFromPaddedBorders(all);
			AssertDialogRootHasWindowPadding(root, all);
		}

		// ----- (1) no visible text-bearing control has a zero-area bounds -----

		private static void AssertTextNotZeroArea(IEnumerable<Control> all)
		{
			// Authored text controls only: a control's template internals (e.g. a CheckBox's content
			// presenter) can legitimately measure to zero in some states; the dialog-authored TextBlock/TextBox
			// are what must have real area.
			foreach (var c in all.Where(IsTextBearing).Where(c => !IsTemplateGenerated(c)).Where(IsEffectivelyVisible))
			{
				var b = c.Bounds;
				Assert.That(b.Width, Is.GreaterThan(0),
					$"text-bearing {Describe(c)} has zero width (crowded out of layout)");
				Assert.That(b.Height, Is.GreaterThan(0),
					$"text-bearing {Describe(c)} has zero height (crowded out of layout)");
			}
		}

		// ----- (2) sibling controls' bounds do not overlap -----

		private static void AssertSiblingsDoNotOverlap(Control root)
		{
			var panels = root.GetVisualDescendants().OfType<Panel>();
			if (root is Panel rootPanel)
				panels = new[] { rootPanel }.Concat(panels);
			foreach (var parent in panels)
			{
				// Only compare the AUTHORED layout: a Panel we wrote (not one generated inside a control's
				// template) arranging children we wrote. A control's templated internals (e.g. a TextBox's
				// PART_BorderElement over its background Border) legitimately stack, so skip any panel or child
				// that belongs to a control template (TemplatedParent != null). This keeps the check focused on
				// the dialog's own layout — where the crowding defect actually appears.
				if (IsTemplateGenerated(parent))
					continue;
				var kids = parent.GetVisualChildren().OfType<Control>()
					.Where(k => !IsTemplateGenerated(k))
					.Where(IsEffectivelyVisible)
					.Where(k => k.Bounds.Width > 0 && k.Bounds.Height > 0)
					.ToList();
				for (var i = 0; i < kids.Count; i++)
				{
					for (var j = i + 1; j < kids.Count; j++)
					{
						// Skip a pair where either side is a SPLITTER. A GridSplitter (or a Border/control
						// whose Name/AutomationId contains "Splitter", e.g. the browse column-splitter) is a drag
						// handle that by design sits ON a column boundary and overlaps its neighbors — that is
						// expected splitter behavior, not the content-overlap defect this tripwire hunts for.
						if (IsSplitterChrome(kids[i]) || IsSplitterChrome(kids[j]))
							continue;
						var a = kids[i].Bounds;
						var b = kids[j].Bounds;
						Assert.That(Overlaps(a, b), Is.False,
							$"sibling controls overlap: {Describe(kids[i])} {a} intersects {Describe(kids[j])} {b}");
					}
				}
			}
		}

		// ----- (3) each PART_*Host border has a nonzero effective border thickness -----

		private static void AssertHostBordersHaveAFrame(IEnumerable<Control> all)
		{
			foreach (var border in all.OfType<Border>())
			{
				var id = AutomationProperties.GetAutomationId(border) ?? string.Empty;
				var name = border.Name ?? string.Empty;
				var isHost = name.StartsWith("PART_") && name.EndsWith("Host");
				if (!isHost)
					continue;
				var t = border.BorderThickness;
				Assert.That(t.Left + t.Top + t.Right + t.Bottom, Is.GreaterThan(0),
					$"PART_*Host border '{name}' (id '{id}') has a zero BorderThickness (no frame around the embedded control)");
			}
		}

		// ----- (4) a Border's child is inset by the Border's padding (its embedded control gets breathing
		// room). Restricted to Decorator (Border) parents because there the Child's Bounds are directly in the
		// Border's content space, so the padding inset is geometrically observable — unlike a templated
		// ContentControl whose padding is consumed by an internal presenter. -----

		private static void AssertChildrenAreInsetFromPaddedBorders(IEnumerable<Control> all)
		{
			foreach (var border in all.OfType<Decorator>())
			{
				var padding = border.Padding;
				if (padding.Left <= 0 && padding.Top <= 0)
					continue;
				if (border.Bounds.Width <= 0 || border.Bounds.Height <= 0)
					continue;
				if (!(border.Child is Control child) || !IsEffectivelyVisible(child))
					continue;
				if (child.Bounds.Width <= 0 || child.Bounds.Height <= 0)
					continue;

				var cb = child.Bounds; // child bounds are in the border's content coordinate space
				if (padding.Left > 0)
					Assert.That(cb.X, Is.GreaterThanOrEqualTo(padding.Left - MinEdgeInset),
						$"{Describe(child)} butts the left edge of padded {Describe(border)} (inset {cb.X} < pad {padding.Left})");
				if (padding.Top > 0)
					Assert.That(cb.Y, Is.GreaterThanOrEqualTo(padding.Top - MinEdgeInset),
						$"{Describe(child)} butts the top edge of padded {Describe(border)} (inset {cb.Y} < pad {padding.Top})");
			}
		}

		// ----- (5) the dialog root carries a nonzero window padding (the structural anti-crowding guarantee:
		// content never butts the dialog frame even if a view omits its own margin). Only enforced when a root is
		// actually present (a `fwDialogRoot` UserControl) so the helper still works on ad-hoc test roots. -----

		private static void AssertDialogRootHasWindowPadding(Control root, IEnumerable<Control> all)
		{
			var dialogRoots = all.OfType<UserControl>()
				.Where(uc => uc.Classes.Contains("fwDialogRoot"))
				.ToList();
			if (root is UserControl r && r.Classes.Contains("fwDialogRoot"))
				dialogRoots.Add(r);
			foreach (var uc in dialogRoots.Distinct())
			{
				var p = uc.Padding;
				Assert.That(p.Left + p.Top + p.Right + p.Bottom, Is.GreaterThan(0),
					$"dialog root {Describe(uc)} (.fwDialogRoot) must carry a nonzero window padding so content does not crowd the dialog frame");
			}
		}

		// ----- helpers -----

		private static bool IsTextBearing(Control c)
		{
			switch (c)
			{
				case TextBlock tb:
					return !string.IsNullOrEmpty(tb.Text);
				case TextBox box:
					return true;
				case TextPresenter tp:
					return !string.IsNullOrEmpty(tp.Text);
				default:
					return false;
			}
		}

		// A visual that the XAML/control template generated (its TemplatedParent is set), as opposed to a
		// control the dialog author placed directly in the logical tree.
		private static bool IsTemplateGenerated(StyledElement e) => e.TemplatedParent != null;

		// A splitter (a GridSplitter, or any control whose Name / AutomationId contains "Splitter")
		// is a drag handle that by design straddles a column boundary and overlaps its neighbors. It is
		// expected splitter behavior, not the content-overlap defect, so the sibling-overlap check skips it.
		private static bool IsSplitterChrome(Control c)
			=> c is GridSplitter
				|| (c.Name ?? string.Empty).IndexOf("Splitter", System.StringComparison.Ordinal) >= 0
				|| (AutomationProperties.GetAutomationId(c) ?? string.Empty)
					.IndexOf("Splitter", System.StringComparison.Ordinal) >= 0;

		private static bool IsEffectivelyVisible(Control c)
		{
			for (Visual v = c; v != null; v = v.GetVisualParent())
			{
				if (v is Control ctrl && !ctrl.IsVisible)
					return false;
			}
			return true;
		}

		private static bool Overlaps(Rect a, Rect b)
		{
			var x = System.Math.Max(a.X, b.X);
			var y = System.Math.Max(a.Y, b.Y);
			var right = System.Math.Min(a.Right, b.Right);
			var bottom = System.Math.Min(a.Bottom, b.Bottom);
			// A shared edge (zero-area touch) is not an overlap; require positive intersection area.
			const double eps = 0.01;
			return right - x > eps && bottom - y > eps;
		}

		private static string Describe(Control c)
		{
			var id = AutomationProperties.GetAutomationId(c);
			var label = !string.IsNullOrEmpty(id) ? $"#{id}" :
				!string.IsNullOrEmpty(c.Name) ? $"#{c.Name}" : c.GetType().Name;
			return $"{c.GetType().Name}({label})";
		}
	}
}
