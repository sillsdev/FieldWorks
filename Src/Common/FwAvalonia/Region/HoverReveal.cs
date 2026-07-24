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
using SIL.FieldWorks.Common.FwAvalonia;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// A field editor whose row decorations include hover-revealed affordances (the chooser's settings
	/// gear, the reference vector's separator bars and "+" launcher). The region view reads this
	/// to widen the hover surface to the WHOLE row (label + editor) — presentation only, no behavior.
	/// </summary>
	public interface IHoverAffordanceProvider
	{
		/// <summary>The controls revealed on row hover; empty when the field has none.</summary>
		IReadOnlyList<Control> HoverAffordances { get; }
	}

	/// <summary>
	/// Modern hover-reveal presentation for secondary affordances: the affordances start hidden by
	/// OPACITY (they stay in layout — rows never reflow — and stay in the UIA tree, focusable),
	/// fade in (~120ms) while the pointer is over any hover source or any affordance (entering
	/// the gear itself must not flicker it away), and fade out when the pointer leaves them all.
	/// Keyboard access: an affordance gaining focus (Tab) also reveals; losing focus hides again
	/// unless the pointer is over. Pure presentation — no flyout, staging, or automation-id changes.
	/// </summary>
	public static class HoverReveal
	{
		/// <summary>The opacity fade duration (the "modern feel" transition).</summary>
		internal static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(120);

		// The reveal registration of an affordance: stamped the first time the affordance is
		// attached, looked up (and merged into) by every later Attach. The property is the
		// idempotence anchor — without it each Attach call would stack an independent handler
		// set with its own watched list, and the groups would fight over the opacity.
		private static readonly AttachedProperty<RevealGroup> RevealGroupProperty =
			AvaloniaProperty.RegisterAttached<Control, RevealGroup>("HoverRevealGroup", typeof(HoverReveal));

		/// <summary>
		/// Wires <paramref name="affordances"/> to reveal while the pointer is over any of
		/// <paramref name="hoverSources"/> (or over an affordance itself) and hide otherwise.
		/// Idempotent per affordance: attaching again (the view widening the hover surface to the
		/// row after the control wired itself) merges into the existing registration — one handler
		/// set, one watched list — instead of stacking a second independent one.
		/// </summary>
		public static void Attach(IReadOnlyList<Control> hoverSources, IReadOnlyList<Control> affordances)
		{
			var targets = (affordances ?? Array.Empty<Control>()).Where(a => a != null).Distinct().ToList();
			if (targets.Count == 0)
				return;
			var sources = (hoverSources ?? Array.Empty<Control>()).Where(s => s != null).Distinct().ToList();

			// Resolve the registration this call lands in: the first already-registered target's
			// group wins; targets registered in OTHER groups merge into it (an Attach spanning
			// previously separate registrations unifies them — they reveal together from then on).
			RevealGroup group = null;
			foreach (var affordance in targets)
			{
				var existing = affordance.GetValue(RevealGroupProperty);
				if (existing == null)
					continue;
				if (group == null)
					group = existing;
				else
					existing.MergeInto(group);
			}
			if (group == null)
				group = new RevealGroup();

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

				if (group.ContainsTarget(affordance))
				{
					// Already wired (possibly under a since-merged group): just re-point the stamp.
					affordance.SetValue(RevealGroupProperty, group);
					continue;
				}

				group.AddTarget(affordance);
				affordance.SetValue(RevealGroupProperty, group);
				// The affordances are hover sources too: moving onto the gear keeps it revealed.
				group.Watch(affordance);
				// Accessibility: opacity-hidden affordances stay focusable, so Tab reveals them.
				// The handlers resolve the group at fire time, so they survive later merges.
				affordance.GotFocus += (s, e) => group.RevealAll();
				affordance.LostFocus += (s, e) => group.Update();
			}

			foreach (var source in sources)
				group.Watch(source);

			// Initial/merged state: hidden unless something is already hovered or focused.
			group.Update();
		}

		// One reveal state machine per merged registration: the targets that reveal together and
		// the controls whose hover drives them. Merging leaves a forwarding pointer behind, so
		// handlers subscribed against an absorbed group keep working against the merged one.
		private sealed class RevealGroup
		{
			private RevealGroup _mergedInto;
			private readonly List<Control> _targets = new List<Control>();
			private readonly List<Control> _watched = new List<Control>();

			private RevealGroup Resolve()
			{
				var group = this;
				while (group._mergedInto != null)
					group = group._mergedInto;
				return group;
			}

			public bool ContainsTarget(Control affordance) => Resolve()._targets.Contains(affordance);

			public void AddTarget(Control affordance) => Resolve()._targets.Add(affordance);

			/// <summary>Watches a hover source, subscribing its pointer handlers exactly once.</summary>
			public void Watch(Control control)
			{
				var group = Resolve();
				if (group._watched.Contains(control))
					return;
				group._watched.Add(control);
				control.PointerEntered += (s, e) => Update();
				control.PointerExited += (s, e) => Update();
			}

			public void RevealAll() => SetRevealed(Resolve()._targets, true);

			public void Update()
			{
				var group = Resolve();
				var reveal = group._watched.Any(c => c.IsPointerOver) || group._targets.Any(a => a.IsFocused);
				SetRevealed(group._targets, reveal);
			}

			/// <summary>Folds this registration into <paramref name="other"/> (no-op when equal).</summary>
			public void MergeInto(RevealGroup other)
			{
				var source = Resolve();
				var target = other.Resolve();
				if (source == target)
					return;
				foreach (var affordance in source._targets)
				{
					if (!target._targets.Contains(affordance))
						target._targets.Add(affordance);
				}
				foreach (var watched in source._watched)
				{
					if (!target._watched.Contains(watched))
						target._watched.Add(watched);
				}
				source._targets.Clear();
				source._watched.Clear();
				source._mergedInto = target;
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
	/// The shared hover-affordance icon factory: every field whose value has a supporting list/dialog
	/// (chooser, reference vector, dialog launcher) draws the IDENTICAL settings-gear icon from
	/// this one factory, so the affordance reads the same across all rows.
	/// </summary>
	internal static class RegionChrome
	{
		// A real cog drawn as geometry (circle + teeth + hub hole, even-odd fill), not a text/emoji
		// glyph: 8 teeth on a 24-unit canvas rendered at ~14px in the muted ws-abbreviation hue.
		private static readonly Geometry GearGeometry = CreateGearGeometry();

		// A vertical ellipsis ("⋮", the "kebab" field-menu glyph): three stacked dots on the same
		// 24-unit canvas, drawn as model EllipseGeometry (no stream context) so it renders in the
		// headless unit tests that build these controls with no Avalonia platform loaded.
		private static readonly Geometry KebabGeometry = CreateKebabGeometry();

		/// <summary>The "⋮" field-options glyph, in the muted affordance hue.</summary>
		internal static Control CreateKebabIcon()
			=> new Avalonia.Controls.Shapes.Path
			{
				Data = KebabGeometry,
				Fill = FwAvaloniaDensity.WsAbbrevBrush,
				Width = 14,
				Height = 14,
				Stretch = Stretch.Uniform,
				VerticalAlignment = VerticalAlignment.Center
			};

		/// <summary>A flat (transparent, borderless) button carrying the "⋮" glyph as its face.</summary>
		internal static Button CreateKebabButton()
			=> new Button
			{
				Content = CreateKebabIcon(),
				Padding = new Thickness(2, 0, 2, 0),
				MinHeight = 0,
				MinWidth = 0,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				VerticalAlignment = VerticalAlignment.Top
			};

		private static Geometry CreateKebabGeometry()
		{
			const double cx = 12, radius = 1.7;
			var dots = new GeometryGroup();
			foreach (var cy in new[] { 5.0, 12.0, 19.0 })
				dots.Children.Add(new EllipseGeometry { Center = new Point(cx, cy), RadiusX = radius, RadiusY = radius });
			return dots;
		}

		/// <summary>The gear icon itself, for hosts that carry it inside their own click surface.</summary>
		internal static Control CreateGearIcon()
			=> new Avalonia.Controls.Shapes.Path
			{
				Data = GearGeometry,
				Fill = FwAvaloniaDensity.WsAbbrevBrush,
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
