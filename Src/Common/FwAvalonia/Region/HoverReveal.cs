// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SIL.FieldWorks.Common.FwAvalonia.Poc;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// A field editor whose chrome includes hover-revealed affordances (the chooser's settings
	/// gear, the reference vector's separator bars and "+" launcher). The region view reads this
	/// to widen the hover surface to the WHOLE row (label + editor) — chrome only, no behavior.
	/// </summary>
	public interface IHoverAffordanceProvider
	{
		/// <summary>The controls revealed on row hover; empty when the field has none.</summary>
		IReadOnlyList<Control> HoverAffordances { get; }
	}

	/// <summary>
	/// Modern hover-reveal chrome for secondary affordances: the affordances start hidden by
	/// OPACITY (they stay in layout — rows never reflow — and stay in the UIA tree, focusable),
	/// fade in (~120ms) while the pointer is over any hover source or any affordance (entering
	/// the gear itself must not flicker it away), and fade out when the pointer leaves them all.
	/// Keyboard access: an affordance gaining focus (Tab) also reveals; losing focus hides again
	/// unless the pointer is over. Pure chrome — no flyout, staging, or automation-id changes.
	/// </summary>
	public static class HoverReveal
	{
		/// <summary>The opacity fade duration (the "modern feel" transition).</summary>
		internal static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(120);

		/// <summary>
		/// Wires <paramref name="affordances"/> to reveal while the pointer is over any of
		/// <paramref name="hoverSources"/> (or over an affordance itself) and hide otherwise.
		/// Idempotent per affordance: attaching again (the view widening the hover surface to the
		/// row after the control wired itself) only adds the new sources.
		/// </summary>
		public static void Attach(IReadOnlyList<Control> hoverSources, IReadOnlyList<Control> affordances)
		{
			var targets = (affordances ?? Array.Empty<Control>()).Where(a => a != null).Distinct().ToList();
			if (targets.Count == 0)
				return;
			var sources = (hoverSources ?? Array.Empty<Control>()).Where(s => s != null).Distinct().ToList();

			foreach (var affordance in targets)
			{
				// One opacity transition per affordance, even across repeated Attach calls.
				var transitions = affordance.Transitions ?? (affordance.Transitions = new Transitions());
				if (!transitions.OfType<DoubleTransition>().Any(t => t.Property == Visual.OpacityProperty))
				{
					transitions.Add(new DoubleTransition
					{
						Property = Visual.OpacityProperty,
						Duration = FadeDuration
					});
				}
			}
			SetRevealed(targets, false);

			// The affordances are hover sources too: moving onto the gear keeps it revealed.
			var watched = sources.Concat(targets).Distinct().ToList();

			void Update()
			{
				var reveal = watched.Any(c => c.IsPointerOver) || targets.Any(a => a.IsFocused);
				SetRevealed(targets, reveal);
			}

			foreach (var control in watched)
			{
				control.PointerEntered += (s, e) => Update();
				control.PointerExited += (s, e) => Update();
			}

			foreach (var affordance in targets)
			{
				// Accessibility: opacity-hidden affordances stay focusable, so Tab reveals them.
				affordance.GotFocus += (s, e) => SetRevealed(targets, true);
				affordance.LostFocus += (s, e) => Update();
			}
		}

		/// <summary>Applies the revealed/hidden state: opacity plus hit-test visibility.</summary>
		internal static void SetRevealed(IEnumerable<Control> affordances, bool revealed)
		{
			foreach (var affordance in affordances)
			{
				affordance.Opacity = revealed ? 1d : 0d;
				affordance.IsHitTestVisible = revealed;
			}
		}
	}

	/// <summary>
	/// The shared hover-affordance chrome: every field whose value has a supporting list/dialog
	/// (chooser, reference vector, dialog launcher) draws the IDENTICAL settings-gear icon from
	/// this one factory, so the affordance reads the same across all rows.
	/// </summary>
	internal static class RegionChrome
	{
		// A real cog drawn as geometry (circle + teeth + hub hole, even-odd fill), not a text/emoji
		// glyph: 8 teeth on a 24-unit canvas rendered at ~14px in the muted ws-abbreviation hue.
		private static readonly Geometry GearGeometry = CreateGearGeometry();

		/// <summary>The gear icon itself, for hosts that carry it inside their own click surface.</summary>
		internal static Control CreateGearIcon()
			=> new Avalonia.Controls.Shapes.Path
			{
				Data = GearGeometry,
				Fill = PocDensity.WsAbbrevBrush,
				Width = 14,
				Height = 14,
				Stretch = Stretch.Uniform,
				VerticalAlignment = VerticalAlignment.Center
			};

		/// <summary>A flat (transparent, borderless) button carrying the gear icon as its face.</summary>
		internal static Button CreateGearButton()
			=> new Button
			{
				Content = CreateGearIcon(),
				Padding = new Thickness(4, 0, 4, 0),
				MinHeight = 0,
				MinWidth = 0,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				VerticalAlignment = VerticalAlignment.Center
			};

		// Built from MODEL segments (PathGeometry/ArcSegment/LineSegment), NOT StreamGeometry.Open:
		// opening a stream context demands the IPlatformRenderInterface, and xWorks hosts construct
		// these controls in plain unit tests with no Avalonia platform loaded — model geometry only
		// touches the platform when actually rendered.
		private static Geometry CreateGearGeometry()
		{
			const int teeth = 8;
			const double cx = 12, cy = 12;
			const double tipRadius = 11, bodyRadius = 8, holeRadius = 3.6;
			var step = Math.PI * 2 / teeth;
			Point Polar(double radius, double angle)
				=> new Point(cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle));
			PathSegment Line(Point point) => new LineSegment { Point = point };
			PathSegment Arc(Point point, double radius) => new ArcSegment
			{
				Point = point,
				Size = new Size(radius, radius),
				SweepDirection = SweepDirection.Clockwise
			};

			// Toothed ring: per tooth, rise from the body circle to the tip, across, and back,
			// then an arc along the body to the next tooth.
			var ring = new PathFigure
			{
				StartPoint = Polar(bodyRadius, -step * 0.28),
				IsClosed = true,
				IsFilled = true,
				Segments = new PathSegments()
			};
			for (var i = 0; i < teeth; i++)
			{
				var a = i * step;
				if (i > 0)
					ring.Segments.Add(Arc(Polar(bodyRadius, a - step * 0.28), bodyRadius));
				ring.Segments.Add(Line(Polar(tipRadius, a - step * 0.14)));
				ring.Segments.Add(Arc(Polar(tipRadius, a + step * 0.14), tipRadius));
				ring.Segments.Add(Line(Polar(bodyRadius, a + step * 0.28)));
			}
			ring.Segments.Add(Arc(Polar(bodyRadius, -step * 0.28), bodyRadius));

			// Hub hole (even-odd makes it a cut-out).
			var hub = new PathFigure
			{
				StartPoint = new Point(cx + holeRadius, cy),
				IsClosed = true,
				IsFilled = true,
				Segments = new PathSegments
				{
					Arc(new Point(cx - holeRadius, cy), holeRadius),
					Arc(new Point(cx + holeRadius, cy), holeRadius)
				}
			};

			return new PathGeometry
			{
				FillRule = FillRule.EvenOdd,
				Figures = new PathFigures { ring, hub }
			};
		}
	}
}
